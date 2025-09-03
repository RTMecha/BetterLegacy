using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

using LSFunctions;

using SimpleJSON;

using BetterLegacy.Configs;
using BetterLegacy.Core;
using BetterLegacy.Core.Components;
using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using BetterLegacy.Core.Prefabs;
using BetterLegacy.Editor.Components;
using BetterLegacy.Editor.Data;
using BetterLegacy.Editor.Data.Dialogs;
using BetterLegacy.Editor.Data.Elements;
using BetterLegacy.Editor.Data.Popups;

namespace BetterLegacy.Editor.Managers
{
    public class RTThemeEditor : MonoBehaviour
    {
        #region Init

        public static RTThemeEditor inst;

        public static void Init() => ThemeEditor.inst?.gameObject?.AddComponent<RTThemeEditor>();

        void Awake()
        {
            inst = this;

            Dialog = RTEventEditor.inst.Dialog.keyframeDialogs[4] as ThemeKeyframeDialog;

            try
            {
                Popup = RTEditor.inst.GeneratePopup(EditorPopup.THEME_POPUP, "Beatmap Themes", Vector2.zero, new Vector2(600f, 450f), _val =>
                {
                    RenderExternalThemesPopup();
                }, placeholderText: "Search for theme...");

                //Popup.Grid.cellSize = new Vector2(600f, 362f);
                CoreHelper.Destroy(Popup.Grid, true);
                var layoutGroup = Popup.Content.gameObject.AddComponent<VerticalLayoutGroup>();
                layoutGroup.childControlWidth = false;
                layoutGroup.childControlHeight = false;
                layoutGroup.childForceExpandWidth = false;
                layoutGroup.childForceExpandHeight = false;
                layoutGroup.spacing = 8f;

                Popup.getMaxPageCount = () => ExternalThemesCount / themesPerPage;
                Popup.InitPageField();
                Popup.InitReload();
                Popup.InitPath();

                Popup.PathField.SetTextWithoutNotify(RTEditor.inst.ThemePath);
                Popup.PathField.onValueChanged.NewListener(_val => RTEditor.inst.ThemePath = _val);
                Popup.PathField.onEndEdit.NewListener(_val => RTEditor.inst.UpdateThemePath(false));
                var themeClickable = Popup.PathField.gameObject.AddComponent<Clickable>();
                themeClickable.onDown = pointerEventData =>
                {
                    if (pointerEventData.button != PointerEventData.InputButton.Right)
                        return;

                    EditorContextMenu.inst.ShowContextMenu(
                        new ButtonFunction("Set Theme folder", () =>
                        {
                            RTEditor.inst.BrowserPopup.Open();
                            RTFileBrowser.inst.UpdateBrowserFolder(_val =>
                            {
                                if (!_val.Replace("\\", "/").Contains(RTFile.ApplicationDirectory + "beatmaps/"))
                                {
                                    EditorManager.inst.DisplayNotification($"Path does not contain the proper directory.", 2f, EditorManager.NotificationType.Warning);
                                    return;
                                }

                                Popup.PathField.text = _val.Replace("\\", "/").Replace(RTFile.ApplicationDirectory.Replace("\\", "/") + "beatmaps/", "");
                                EditorManager.inst.DisplayNotification($"Set Theme path to {RTEditor.inst.ThemePath}!", 2f, EditorManager.NotificationType.Success);
                                RTEditor.inst.BrowserPopup.Close();
                                RTEditor.inst.UpdateThemePath(false);
                            });
                        }),
                        new ButtonFunction("Open List in File Explorer", RTEditor.inst.OpenThemeListFolder),
                        new ButtonFunction("Set as Default for Level", () =>
                        {
                            RTEditor.inst.editorInfo.themePath = RTEditor.inst.ThemePath;
                            EditorManager.inst.DisplayNotification($"Set current theme folder [ {RTEditor.inst.ThemePath} ] as the default for the level!", 5f, EditorManager.NotificationType.Success);
                        }, "Theme Default Path"),
                        new ButtonFunction("Remove Default", () =>
                        {
                            RTEditor.inst.editorInfo.themePath = null;
                            EditorManager.inst.DisplayNotification($"Removed default theme folder.", 5f, EditorManager.NotificationType.Success);
                        }, "Theme Default Path"));
                };

                Popup.ReloadButton.onClick.NewListener(() => CoroutineHelper.StartCoroutine(LoadThemes()));

                EditorHelper.AddEditorDropdown("View Themes", "", "View", EditorSprites.SearchSprite, OpenExternalThemesPopup);

                // Internal Prefab
                {
                    var gameObject = EventEditor.inst.ThemePanel.Duplicate(transform, "theme-panel");
                    gameObject.AddComponent<Button>();
                    var storage = gameObject.AddComponent<ThemePanelStorage>();

                    var image = gameObject.transform.Find("image");

                    image.gameObject.AddComponent<Mask>().showMaskGraphic = false;

                    var hlg = image.gameObject.AddComponent<HorizontalLayoutGroup>();

                    for (int i = 0; i < ThemePreviewColorCount; i++)
                    {
                        var col = Creator.NewUIObject($"Col{i + 1}", image);

                        switch (i)
                        {
                            case 0: {
                                    storage.color1 = col.AddComponent<Image>();
                                    break;
                                }
                            case 1: {
                                    storage.color2 = col.AddComponent<Image>();
                                    break;
                                }
                            case 2: {
                                    storage.color3 = col.AddComponent<Image>();
                                    break;
                                }
                            case 3: {
                                    storage.color4 = col.AddComponent<Image>();
                                    break;
                                }
                        }
                    }

                    storage.button = image.GetComponent<Button>();
                    storage.baseImage = gameObject.GetComponent<Image>();
                    storage.text = gameObject.transform.Find("text").GetComponent<Text>();
                    storage.edit = gameObject.transform.Find("edit").GetComponent<Button>();
                    storage.delete = gameObject.transform.Find("delete").GetComponent<Button>();

                    EventEditor.inst.ThemePanel = gameObject;
                }

                // External Prefab
                {
                    themePopupPanelPrefab = EditorManager.inst.folderButtonPrefab.Duplicate(transform, $"theme panel");

                    var viewThemeStorage = themePopupPanelPrefab.AddComponent<ViewThemePanelStorage>();

                    var nameText = themePopupPanelPrefab.transform.GetChild(0).GetComponent<Text>();
                    UIManager.SetRectTransform(nameText.rectTransform, new Vector2(2f, 160f), new Vector2(1f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-12f, 32f));
                    nameText.text = "theme";
                    nameText.fontSize = 17;

                    viewThemeStorage.baseImage = themePopupPanelPrefab.GetComponent<Image>();
                    viewThemeStorage.text = nameText;
                    viewThemeStorage.baseColors = new List<Image>();
                    viewThemeStorage.playerColors = new List<Image>();
                    viewThemeStorage.objectColors = new List<Image>();
                    viewThemeStorage.backgroundColors = new List<Image>();
                    viewThemeStorage.effectColors = new List<Image>();

                    // Misc Colors
                    {
                        var objectColorsLabel = themePopupPanelPrefab.transform.GetChild(0).gameObject.Duplicate(themePopupPanelPrefab.transform, "label");
                        objectColorsLabel.transform.AsRT().anchoredPosition = new Vector2(2f, 125f);
                        objectColorsLabel.transform.AsRT().anchorMax = new Vector2(1f, 0.5f);
                        objectColorsLabel.transform.AsRT().anchorMin = new Vector2(0f, 0.5f);
                        objectColorsLabel.transform.AsRT().sizeDelta = new Vector2(-12f, 32f);
                        viewThemeStorage.baseColorsText = objectColorsLabel.GetComponent<Text>();
                        viewThemeStorage.baseColorsText.text = "Background / GUI / Tail Colors";

                        var objectColors = new GameObject("misc colors");
                        objectColors.transform.SetParent(themePopupPanelPrefab.transform);
                        objectColors.transform.localScale = Vector3.one;

                        var objectColorsRT = objectColors.AddComponent<RectTransform>();
                        objectColorsRT.anchoredPosition = new Vector2(13f, 90f);
                        objectColorsRT.sizeDelta = new Vector2(600f, 32f);
                        var objectColorsGLG = objectColors.AddComponent<GridLayoutGroup>();
                        objectColorsGLG.cellSize = new Vector2(28f, 28f);
                        objectColorsGLG.spacing = new Vector2(4f, 4f);

                        for (int i = 0; i < 3; i++)
                        {
                            var colorSlot = new GameObject($"{i + 1}");
                            colorSlot.transform.SetParent(objectColorsRT);
                            colorSlot.transform.localScale = Vector3.one;

                            var colorSlotRT = colorSlot.AddComponent<RectTransform>();
                            var colorSlotImage = colorSlot.AddComponent<Image>();
                            viewThemeStorage.baseColors.Add(colorSlotImage);
                        }
                    }

                    // Player Colors
                    {
                        var objectColorsLabel = themePopupPanelPrefab.transform.GetChild(0).gameObject.Duplicate(themePopupPanelPrefab.transform, "label");
                        objectColorsLabel.transform.AsRT().anchoredPosition = new Vector2(2f, 65f);
                        objectColorsLabel.transform.AsRT().anchorMax = new Vector2(1f, 0.5f);
                        objectColorsLabel.transform.AsRT().anchorMin = new Vector2(0f, 0.5f);
                        objectColorsLabel.transform.AsRT().sizeDelta = new Vector2(-12f, 32f);
                        viewThemeStorage.playerColorsText = objectColorsLabel.GetComponent<Text>();
                        viewThemeStorage.playerColorsText.text = "Player Colors";

                        var objectColors = new GameObject("player colors");
                        objectColors.transform.SetParent(themePopupPanelPrefab.transform);
                        objectColors.transform.localScale = Vector3.one;

                        var objectColorsRT = objectColors.AddComponent<RectTransform>();
                        objectColorsRT.anchoredPosition = new Vector2(13f, 30f);
                        objectColorsRT.sizeDelta = new Vector2(600f, 32f);
                        var objectColorsGLG = objectColors.AddComponent<GridLayoutGroup>();
                        objectColorsGLG.cellSize = new Vector2(28f, 28f);
                        objectColorsGLG.spacing = new Vector2(4f, 4f);

                        for (int i = 0; i < 4; i++)
                        {
                            var colorSlot = new GameObject($"{i + 1}");
                            colorSlot.transform.SetParent(objectColorsRT);
                            colorSlot.transform.localScale = Vector3.one;

                            var colorSlotRT = colorSlot.AddComponent<RectTransform>();
                            var colorSlotImage = colorSlot.AddComponent<Image>();
                            viewThemeStorage.playerColors.Add(colorSlotImage);
                        }
                    }

                    // Object Colors
                    {
                        var objectColorsLabel = themePopupPanelPrefab.transform.GetChild(0).gameObject.Duplicate(themePopupPanelPrefab.transform, "label");
                        objectColorsLabel.transform.AsRT().anchoredPosition = new Vector2(2f, 5f);
                        objectColorsLabel.transform.AsRT().anchorMax = new Vector2(1f, 0.5f);
                        objectColorsLabel.transform.AsRT().anchorMin = new Vector2(0f, 0.5f);
                        objectColorsLabel.transform.AsRT().sizeDelta = new Vector2(-12f, 32f);
                        viewThemeStorage.objectColorsText = objectColorsLabel.GetComponent<Text>();
                        viewThemeStorage.objectColorsText.text = "Object Colors";

                        var objectColors = new GameObject("object colors");
                        objectColors.transform.SetParent(themePopupPanelPrefab.transform);
                        objectColors.transform.localScale = Vector3.one;

                        var objectColorsRT = objectColors.AddComponent<RectTransform>();
                        objectColorsRT.anchoredPosition = new Vector2(13f, -30f);
                        objectColorsRT.sizeDelta = new Vector2(600f, 32f);
                        var objectColorsGLG = objectColors.AddComponent<GridLayoutGroup>();
                        objectColorsGLG.cellSize = new Vector2(28f, 28f);
                        objectColorsGLG.spacing = new Vector2(4f, 4f);

                        for (int i = 0; i < 18; i++)
                        {
                            var colorSlot = new GameObject($"{i + 1}");
                            colorSlot.transform.SetParent(objectColorsRT);
                            colorSlot.transform.localScale = Vector3.one;

                            var colorSlotRT = colorSlot.AddComponent<RectTransform>();
                            var colorSlotImage = colorSlot.AddComponent<Image>();
                            viewThemeStorage.objectColors.Add(colorSlotImage);
                        }
                    }

                    // Background Colors
                    {
                        var objectColorsLabel = themePopupPanelPrefab.transform.GetChild(0).gameObject.Duplicate(themePopupPanelPrefab.transform, "label");
                        objectColorsLabel.transform.AsRT().anchoredPosition = new Vector2(2f, -55f);
                        objectColorsLabel.transform.AsRT().anchorMax = new Vector2(1f, 0.5f);
                        objectColorsLabel.transform.AsRT().anchorMin = new Vector2(0f, 0.5f);
                        objectColorsLabel.transform.AsRT().sizeDelta = new Vector2(-12f, 32f);
                        viewThemeStorage.backgroundColorsText = objectColorsLabel.GetComponent<Text>();
                        viewThemeStorage.backgroundColorsText.text = "Background Colors";

                        var objectColors = new GameObject("background colors");
                        objectColors.transform.SetParent(themePopupPanelPrefab.transform);
                        objectColors.transform.localScale = Vector3.one;

                        var objectColorsRT = objectColors.AddComponent<RectTransform>();
                        objectColorsRT.anchoredPosition = new Vector2(13f, -90f);
                        objectColorsRT.sizeDelta = new Vector2(600f, 32f);
                        var objectColorsGLG = objectColors.AddComponent<GridLayoutGroup>();
                        objectColorsGLG.cellSize = new Vector2(28f, 28f);
                        objectColorsGLG.spacing = new Vector2(4f, 4f);

                        for (int i = 0; i < 9; i++)
                        {
                            var colorSlot = new GameObject($"{i + 1}");
                            colorSlot.transform.SetParent(objectColorsRT);
                            colorSlot.transform.localScale = Vector3.one;

                            var colorSlotRT = colorSlot.AddComponent<RectTransform>();
                            var colorSlotImage = colorSlot.AddComponent<Image>();
                            viewThemeStorage.backgroundColors.Add(colorSlotImage);
                        }
                    }

                    // Effect Colors
                    {
                        var objectColorsLabel = themePopupPanelPrefab.transform.GetChild(0).gameObject.Duplicate(themePopupPanelPrefab.transform, "label");
                        objectColorsLabel.transform.AsRT().anchoredPosition = new Vector2(2f, -115f);
                        objectColorsLabel.transform.AsRT().anchorMax = new Vector2(1f, 0.5f);
                        objectColorsLabel.transform.AsRT().anchorMin = new Vector2(0f, 0.5f);
                        objectColorsLabel.transform.AsRT().sizeDelta = new Vector2(-12f, 32f);
                        viewThemeStorage.effectColorsText = objectColorsLabel.GetComponent<Text>();
                        viewThemeStorage.effectColorsText.text = "Effect Colors";

                        var objectColors = new GameObject("effect colors");
                        objectColors.transform.SetParent(themePopupPanelPrefab.transform);
                        objectColors.transform.localScale = Vector3.one;

                        var objectColorsRT = objectColors.AddComponent<RectTransform>();
                        objectColorsRT.anchoredPosition = new Vector2(13f, -150f);
                        objectColorsRT.sizeDelta = new Vector2(600f, 32f);
                        var objectColorsGLG = objectColors.AddComponent<GridLayoutGroup>();
                        objectColorsGLG.cellSize = new Vector2(28f, 28f);
                        objectColorsGLG.spacing = new Vector2(4f, 4f);

                        for (int i = 0; i < 18; i++)
                        {
                            var colorSlot = new GameObject($"{i + 1}");
                            colorSlot.transform.SetParent(objectColorsRT);
                            colorSlot.transform.localScale = Vector3.one;

                            var colorSlotRT = colorSlot.AddComponent<RectTransform>();
                            var colorSlotImage = colorSlot.AddComponent<Image>();
                            viewThemeStorage.effectColors.Add(colorSlotImage);
                        }
                    }

                    var buttonsBase = new GameObject("buttons base");
                    buttonsBase.transform.SetParent(themePopupPanelPrefab.transform);
                    buttonsBase.transform.localScale = new Vector3(0.8f, 0.8f, 1f);

                    var buttonsBaseRT = buttonsBase.AddComponent<RectTransform>();
                    buttonsBaseRT.anchoredPosition = new Vector2(140f, 160f);
                    buttonsBaseRT.sizeDelta = new Vector2(0f, 0f);

                    var buttons = new GameObject("buttons");
                    buttons.transform.SetParent(buttonsBaseRT);
                    buttons.transform.localScale = Vector3.one;

                    var buttonsHLG = buttons.AddComponent<HorizontalLayoutGroup>();
                    buttonsHLG.spacing = 8f;

                    buttons.transform.AsRT().anchoredPosition = Vector2.zero;
                    buttons.transform.AsRT().sizeDelta = new Vector2(360f, 32f);

                    var exportToVG = EditorPrefabHolder.Instance.Function2Button.Duplicate(buttons.transform, "convert");
                    var exportToVGStorage = exportToVG.GetComponent<FunctionButtonStorage>();
                    exportToVG.SetActive(false);
                    var exportToVGText = exportToVGStorage.label;
                    exportToVGText.fontSize = 16;
                    exportToVGText.text = "Convert to VG Format";

                    viewThemeStorage.convertButton = exportToVGStorage.button;

                    var delete = EditorPrefabHolder.Instance.DeleteButton.Duplicate(buttons.transform, "delete");
                    viewThemeStorage.deleteButton = delete.GetComponent<DeleteButtonStorage>();
                }
            }
            catch (Exception ex)
            {
                CoreHelper.LogError($"{ex}");
            }

            Dialog.Editor.AddComponent<ActiveState>().onStateChanged = OnDialog;

            PreviewTheme = ThemeManager.inst.DefaultThemes[0];
            CoroutineHelper.StartCoroutine(LoadThemes());
        }

        #endregion

        #region Values

        public BeatmapTheme PreviewTheme { get; set; }

        public ContentPopup Popup { get; set; }

        public ThemeKeyframeDialog Dialog { get; set; }

        public bool loadingThemes = false;

        public bool shouldCutTheme;
        public string copiedThemePath;

        public bool themesLoading = false;

        public GameObject themePopupPanelPrefab;

        public static int themesPerPage = 10;

        public static int ThemePreviewColorCount => 4;

        public List<ThemePanel> ExternalThemePanels { get; set; } = new List<ThemePanel>();
        public List<ThemePanel> InternalThemePanels { get; set; } = new List<ThemePanel>();

        public List<string> themeIDs = new List<string>();

        public static int eventThemesPerPage = 30;
        public int ExternalThemesCount => ExternalThemePanels.FindAll(x => RTString.SearchString(Dialog.SearchTerm, x.DisplayName)).Count;
        public int InternalThemesCount => InternalThemePanels.FindAll(x => RTString.SearchString(Dialog.SearchTerm, x.DisplayName)).Count;

        public bool filterUsed;

        public GameObject themeUpFolderButton;

        #endregion

        #region Methods

        public void OnDialog(bool enabled)
        {
            if (!enabled && EventEditor.inst)
                EventEditor.inst.showTheme = false;
        }

        public IEnumerator LoadThemes()
        {
            if (themesLoading)
                yield break;

            themesLoading = true;

            themeIDs.Clear();

            if (!ExternalThemePanels.IsEmpty())
                ExternalThemePanels.ForEach(x => CoreHelper.Delete(x.GameObject));
            ExternalThemePanels.Clear();

            if (!themeUpFolderButton)
            {
                var spacer = Creator.NewUIObject("spacer", Popup.Content, 0);
                spacer.transform.AsRT().sizeDelta = new Vector2(0f, 32f);

                themeUpFolderButton = EditorManager.inst.folderButtonPrefab.Duplicate(Popup.Content, "back", 1);
                themeUpFolderButton.transform.AsRT().sizeDelta = new Vector2(600f, 32f);
                var folderButtonStorageFolder = themeUpFolderButton.GetComponent<FunctionButtonStorage>();
                var folderButtonFunctionFolder = themeUpFolderButton.AddComponent<FolderButtonFunction>();

                var hoverUIFolder = themeUpFolderButton.AddComponent<HoverUI>();
                hoverUIFolder.size = EditorConfig.Instance.OpenLevelButtonHoverSize.Value;
                hoverUIFolder.animatePos = false;
                hoverUIFolder.animateSca = true;

                folderButtonStorageFolder.label.text = "< Up a folder";

                folderButtonStorageFolder.button.onClick.ClearAll();
                folderButtonFunctionFolder.onClick = eventData =>
                {
                    if (eventData.button == PointerEventData.InputButton.Right)
                    {
                        EditorContextMenu.inst.ShowContextMenu(
                            new ButtonFunction("Create folder", () => RTEditor.inst.ShowFolderCreator(RTFile.CombinePaths(RTEditor.inst.BeatmapsPath, RTEditor.inst.ThemePath), () => { RTEditor.inst.UpdateThemePath(true); RTEditor.inst.HideNameEditor(); })),
                            new ButtonFunction("Paste", PasteTheme));

                        return;
                    }

                    if (Popup.PathField.text == RTEditor.inst.ThemePath)
                    {
                        Popup.PathField.text = RTFile.GetDirectory(RTFile.CombinePaths(RTEditor.inst.BeatmapsPath, RTEditor.inst.ThemePath)).Replace(RTEditor.inst.BeatmapsPath + "/", "");
                        RTEditor.inst.UpdateThemePath(false);
                    }
                };

                EditorThemeManager.ApplySelectable(folderButtonStorageFolder.button, ThemeGroup.List_Button_2);
                EditorThemeManager.ApplyGraphic(folderButtonStorageFolder.label, ThemeGroup.List_Button_2_Text);
            }

            int themePanelIndex = 0;
            try
            {
                themeUpFolderButton.SetActive(RTFile.GetDirectory(RTFile.CombinePaths(RTEditor.inst.BeatmapsPath, RTEditor.inst.ThemePath)) != RTEditor.inst.BeatmapsPath);

                foreach (var directory in Directory.GetDirectories(RTFile.CombinePaths(RTEditor.inst.BeatmapsPath, RTEditor.inst.ThemePath), "*", SearchOption.TopDirectoryOnly))
                {
                    var themePanel = new ThemePanel(ObjectSource.External, themePanelIndex);
                    themePanel.Init(directory);

                    ExternalThemePanels.Add(themePanel);
                    themePanelIndex++;
                }
            }
            catch (Exception ex)
            {
                CoreHelper.LogException(ex);
            }

            //foreach (var beatmapTheme in ThemeManager.inst.DefaultThemes)
            //{
            //    var themePanel = new ThemePanel(ObjectSource.External, themePanelIndex);
            //    themePanel.Init(beatmapTheme);

            //    ThemePanels.Add(themePanel);

            //    themePanelIndex++;
            //}

            var files = Directory.GetFiles(RTFile.CombinePaths(RTEditor.inst.BeatmapsPath, RTEditor.inst.ThemePath), FileFormat.LST.ToPattern());
            for (int i = 0; i < files.Length; i++)
            {
                var file = files[i];
                var jn = JSON.Parse(RTFile.ReadFromFile(file));
                var beatmapTheme = BeatmapTheme.Parse(jn);
                beatmapTheme.filePath = RTFile.ReplaceSlash(file);

                var themePanel = new ThemePanel(ObjectSource.External, themePanelIndex);
                themePanel.Init(beatmapTheme);

                ExternalThemePanels.Add(themePanel);

                themePanelIndex++;
            }

            ThemeManager.inst.UpdateAllThemes();

            themesLoading = false;

            if (Popup.IsOpen)
                RenderExternalThemesPopup();

            yield break;
        }

        public void LoadInternalThemes()
        {
            if (!InternalThemePanels.IsEmpty())
                InternalThemePanels.ForEach(x => CoreHelper.Delete(x.GameObject));
            InternalThemePanels.Clear();

            int themePanelIndex = 0;
            foreach (var beatmapTheme in ThemeManager.inst.DefaultThemes)
            {
                var themePanel = new ThemePanel(ObjectSource.Internal, themePanelIndex);
                themePanel.Init(beatmapTheme);

                InternalThemePanels.Add(themePanel);

                themePanelIndex++;
            }

            for (int i = 0; i < GameData.Current.beatmapThemes.Count; i++)
            {
                var beatmapTheme = GameData.Current.beatmapThemes[i];

                var themePanel = new ThemePanel(ObjectSource.Internal, themePanelIndex);
                themePanel.Init(beatmapTheme);

                InternalThemePanels.Add(themePanel);

                themePanelIndex++;
            }

            ThemeManager.inst.UpdateAllThemes();

            if (Dialog.GameObject.activeInHierarchy)
                RenderThemeList();
        }

        public void PasteTheme()
        {
            if (string.IsNullOrEmpty(copiedThemePath))
            {
                EditorManager.inst.DisplayNotification("No theme has been copied yet!", 2f, EditorManager.NotificationType.Error);
                return;
            }

            if (!RTFile.FileExists(copiedThemePath))
            {
                EditorManager.inst.DisplayNotification("Copied theme no longer exists.", 2f, EditorManager.NotificationType.Error);
                return;
            }

            var copiedThemesFolder = RTFile.GetDirectory(copiedThemePath);
            CoreHelper.Log($"Copied Folder: {copiedThemesFolder}");

            if (copiedThemesFolder == RTFile.CombinePaths(RTEditor.inst.BeatmapsPath, RTEditor.inst.ThemePath))
            {
                EditorManager.inst.DisplayNotification("Source and destination are the same.", 2f, EditorManager.NotificationType.Warning);
                return;
            }

            var destination = copiedThemePath.Replace(copiedThemesFolder, RTFile.CombinePaths(RTEditor.inst.BeatmapsPath, RTEditor.inst.ThemePath));
            CoreHelper.Log($"Destination: {destination}");
            if (RTFile.FileExists(destination))
            {
                EditorManager.inst.DisplayNotification("File already exists.", 2f, EditorManager.NotificationType.Warning);
                return;
            }

            if (shouldCutTheme)
            {
                if (RTFile.MoveFile(copiedThemePath, destination))
                    EditorManager.inst.DisplayNotification($"Succesfully moved {Path.GetFileName(destination)}!", 2f, EditorManager.NotificationType.Success);
            }
            else
            {
                if (RTFile.CopyFile(copiedThemePath, destination))
                    EditorManager.inst.DisplayNotification($"Succesfully pasted {Path.GetFileName(destination)}!", 2f, EditorManager.NotificationType.Success);
            }

            RTEditor.inst.UpdateThemePath(true);
        }

        public void OpenExternalThemesPopup()
        {
            Popup.Open();
            RenderExternalThemesPopup();
        }

        public void RenderExternalThemesPopup()
        {
            int index = 0;
            for (int i = 0; i < ExternalThemePanels.Count; i++)
            {
                var themePanel = ExternalThemePanels[i];
                var searchBool = RTString.SearchString(Popup.SearchTerm, themePanel.DisplayName);

                if (searchBool)
                    index++;

                if (!themePanel.GameObject)
                    throw new NullReferenceException($"Theme Panel object was null. Index: {i} Item: {themePanel.Item}");
                themePanel.SetActive(Popup.InPage(index, themesPerPage) && searchBool);
            }

            Popup.RenderPageField();
        }

        public void RenderThemePreview()
        {
            if (EventEditor.inst.currentEventType != 4)
                return;

            var beatmapTheme = ThemeManager.inst.GetTheme((int)RTEventEditor.inst.CurrentSelectedKeyframe.values[0]);

            Dialog.CurrentTitle.text = beatmapTheme.name;

            Dialog.BGColor.color = beatmapTheme.backgroundColor;
            Dialog.GUIColor.color = beatmapTheme.guiColor;

            Dialog.PlayerColors.ForLoop((img, index) => img.color = beatmapTheme.GetPlayerColor(index));
            Dialog.ObjectColors.ForLoop((img, index) => img.color = beatmapTheme.GetObjColor(index));
            Dialog.BGColors.ForLoop((img, index) => img.color = beatmapTheme.GetBGColor(index));
        }

        public void RenderThemeList()
        {
            if (!GameData.Current)
                return;

            if (EventEditor.inst.eventDrag)
                return;

            int index = 0;
            for (int i = 0; i < InternalThemePanels.Count; i++)
            {
                var themePanel = InternalThemePanels[i];
                if (!themePanel.Item)
                    throw new NullReferenceException($"Theme Panel item was null. Index: {i}");

                var searchBool = (!themePanel.isDefault || EditorConfig.Instance.ShowDefaultThemes.Value) && RTString.SearchString(Dialog.SearchTerm, themePanel.DisplayName);

                if (filterUsed && !GameData.Current.events[4].Any(x => x.values[0] == Parser.TryParse(themePanel.Item.id, -1)))
                    searchBool = false;

                if (searchBool)
                    index++;

                if (!themePanel.GameObject)
                    throw new NullReferenceException($"Theme Panel object was null. Index: {i} Item: {themePanel.Item}");
                themePanel.SetActive(Dialog.InPage(index, eventThemesPerPage) && searchBool);
            }

            Dialog.RenderPageField();
        }

        /// <summary>
        /// Converts a theme to the VG format.
        /// </summary>
        /// <param name="beatmapTheme">Theme to convert.</param>
        public void ConvertTheme(BeatmapTheme beatmapTheme)
        {
            var exportPath = EditorConfig.Instance.ConvertThemeLSToVGExportPath.Value;

            if (string.IsNullOrEmpty(exportPath))
            {
                exportPath = RTFile.CombinePaths(RTFile.ApplicationDirectory, RTEditor.DEFAULT_EXPORTS_PATH);
                RTFile.CreateDirectory(exportPath);
            }

            exportPath = RTFile.AppendEndSlash(exportPath);

            if (!RTFile.DirectoryExists(Path.GetDirectoryName(exportPath)))
            {
                EditorManager.inst.DisplayNotification("Directory does not exist.", 2f, EditorManager.NotificationType.Error);
                return;
            }

            var vgjn = beatmapTheme.ToJSONVG();

            var fileName = $"{RTFile.FormatAlphaFileName(beatmapTheme.name)}{FileFormat.VGT.Dot()}";
            RTFile.WriteToFile(RTFile.CombinePaths(exportPath, fileName), vgjn.ToString());

            EditorManager.inst.DisplayNotification($"Converted Theme {beatmapTheme.name} from LS format to VG format and saved to {fileName}!", 4f, EditorManager.NotificationType.Success);

            AchievementManager.inst.UnlockAchievement("time_machine");
        }

        public void SaveTheme(BeatmapTheme theme)
        {
            if (string.IsNullOrEmpty(theme.id))
                theme.id = LSText.randomNumString(BeatmapTheme.ID_LENGTH);

            GameData.Current.OverwriteTheme(theme);
            ThemeManager.inst.UpdateAllThemes();
            EditorManager.inst.DisplayNotification($"Saved theme [{theme.name}]!", 2f, EditorManager.NotificationType.Success);
        }

        public void ExportTheme(BeatmapTheme theme)
        {
            Debug.Log($"{EventEditor.inst.className}Saving {theme.id} ({theme.name}) to File System!");

            RTEditor.inst.DisableThemeWatcher();

            if (string.IsNullOrEmpty(theme.id))
                theme.id = LSText.randomNumString(BeatmapTheme.ID_LENGTH);

            GameData.SaveOpacityToThemes = EditorConfig.Instance.SavingSavesThemeOpacity.Value;

            var str = EditorConfig.Instance.ThemeSavesIndents.Value ? theme.ToJSON().ToString(3) : theme.ToJSON().ToString();

            var path = RTFile.CombinePaths(RTEditor.inst.BeatmapsPath, RTEditor.inst.ThemePath, $"{RTFile.FormatLegacyFileName(theme.name)}{FileFormat.LST.Dot()}");

            theme.filePath = path;

            if (RTFile.FileExists(theme.filePath))
            {
                RTEditor.inst.ShowWarningPopup("File already exists. Do you wish to overwrite it?", () =>
                {
                    RTFile.WriteToFile(path, str);

                    EditorManager.inst.DisplayNotification($"Saved theme [{theme.name}]!", 2f, EditorManager.NotificationType.Success);

                    CoroutineHelper.StartCoroutine(LoadThemes());

                    RTEditor.inst.EnableThemeWatcher();
                    RTEditor.inst.HideWarningPopup();
                }, () =>
                {
                    RTEditor.inst.EnableThemeWatcher();
                    RTEditor.inst.HideWarningPopup();
                });
                return;
            }

            RTFile.WriteToFile(path, str);

            EditorManager.inst.DisplayNotification($"Saved theme [{theme.name}]!", 2f, EditorManager.NotificationType.Success);

            CoroutineHelper.StartCoroutine(LoadThemes());

            RTEditor.inst.EnableThemeWatcher();
        }

        public void DeleteTheme(ThemePanel themePanel) => RTEditor.inst.ShowWarningPopup("Are you sure you want to delete this theme?", () =>
        {
            switch (themePanel.Source)
            {
                case ObjectSource.Internal: {
                        DeleteInternalTheme(themePanel);
                        break;
                    }
                case ObjectSource.External: {
                        DeleteExternalTheme(themePanel);
                        break;
                    }
            }

            RTEditor.inst.HideWarningPopup();
        }, RTEditor.inst.HideWarningPopup);

        public void DeleteInternalTheme(ThemePanel themePanel)
        {
            var index = themePanel.GetIndex();
            if (index < 0)
                return;

            GameData.Current.beatmapThemes.RemoveAt(index);

            CoreHelper.Delete(themePanel.GameObject);
            InternalThemePanels.RemoveAt(themePanel.index);

            UpdateInternalThemeIndexes();

            RenderThemeList();
            RenderThemePreview();
            SetEditor(false);
        }

        public void DeleteExternalTheme(ThemePanel themePanel)
        {
            RTEditor.inst.DisableThemeWatcher();

            RTFile.DeleteFile(themePanel.Path);

            CoreHelper.Delete(themePanel.GameObject);
            ExternalThemePanels.RemoveAt(themePanel.index);

            UpdateExternalThemeIndexes();

            RTEditor.inst.EnableThemeWatcher();

            if (Popup.IsOpen)
                RenderExternalThemesPopup();
        }

        public void ClearInternalThemes() => RTEditor.inst.ShowWarningPopup("Are you sure you want to remove all themes?", () =>
        {
            GameData.Current.beatmapThemes.Clear();

            LoadInternalThemes();
            RTEditor.inst.HideWarningPopup();
        }, RTEditor.inst.HideWarningPopup);

        public void RemoveUnusedThemes() => RTEditor.inst.ShowWarningPopup("Are you sure you want to remove unused themes?", () =>
        {
            GameData.Current.beatmapThemes.ForLoopReverse((beatmapTheme, index) =>
            {
                if (!GameData.Current.ThemeIsUsed(beatmapTheme))
                    GameData.Current.beatmapThemes.RemoveAt(index);
            });

            LoadInternalThemes();
            RTEditor.inst.HideWarningPopup();
        }, RTEditor.inst.HideWarningPopup);

        public void UpdateInternalThemeIndexes()
        {
            for (int i = 0; i < InternalThemePanels.Count; i++)
                InternalThemePanels[i].index = i;
        }
        
        public void UpdateExternalThemeIndexes()
        {
            for (int i = 0; i < ExternalThemePanels.Count; i++)
                ExternalThemePanels[i].index = i;
        }

        /// <summary>
        /// Shuffles a theme's ID.
        /// </summary>
        /// <param name="beatmapTheme">Theme to shuffle the ID of.</param>
        public void ShuffleThemeID(BeatmapTheme beatmapTheme) => RTEditor.inst.ShowWarningPopup("Are you sure you want to shuffle the theme ID? Any levels that use this theme will need to have their theme keyframes reassigned.", () =>
        {
            beatmapTheme.id = LSText.randomNumString(BeatmapTheme.ID_LENGTH);
            RTEditor.inst.HideWarningPopup();
        }, RTEditor.inst.HideWarningPopup);

        public void SetEditor(bool editing)
        {
            EventEditor.inst.showTheme = editing;
            Dialog.Editor.SetActive(editing);
        }

        /// <summary>
        /// Renders the theme editor to create a new theme.
        /// </summary>
        public void RenderThemeEditor() => RenderThemeEditor(null);

        /// <summary>
        /// Renders the theme editor.
        /// </summary>
        /// <param name="beatmapThemeEdit">Theme to edit. If null, creates a new theme.</param>
        public void RenderThemeEditor(BeatmapTheme beatmapThemeEdit)
        {
            CoreHelper.Log($"Editing Theme: {beatmapThemeEdit}");

            var newTheme = !beatmapThemeEdit;
            PreviewTheme = !newTheme ? beatmapThemeEdit.Copy(false) : new BeatmapTheme();
            if (newTheme)
                PreviewTheme.Reset();

            SetEditor(true);

            if (!Seasons.IsAprilFools)
            {
                Dialog.Editor.transform.Find("theme").localRotation = Quaternion.Euler(Vector3.zero);
                foreach (Transform child in Dialog.EditorContent)
                    child.localRotation = Quaternion.Euler(Vector3.zero);
            }

            Dialog.Editor.transform.Find("theme_title/Text").GetComponent<Text>().text = !newTheme ? $"- Theme Editor (ID: {beatmapThemeEdit.id}) -" : "- Theme Editor -";

            var isDefaultTheme = beatmapThemeEdit && beatmapThemeEdit.isDefault;

            Dialog.EditorShuffleID.gameObject.SetActive(!newTheme && !isDefaultTheme);
            if (!newTheme && !isDefaultTheme)
                Dialog.EditorShuffleID.onClick.NewListener(() => ShuffleThemeID(PreviewTheme));

            Dialog.EditorNameField.SetTextWithoutNotify(PreviewTheme.name);
            Dialog.EditorNameField.onValueChanged.NewListener(_val => PreviewTheme.name = _val);
            Dialog.EditorCreatorField.SetTextWithoutNotify(PreviewTheme.creator);
            Dialog.EditorCreatorField.onValueChanged.NewListener(_val => PreviewTheme.creator = _val);

            Dialog.EditorCancel.onClick.NewListener(() => SetEditor(false));

            Dialog.EditorCreateNew.gameObject.SetActive(true);
            Dialog.EditorCreateNew.onClick.NewListener(() =>
            {
                PreviewTheme.id = null;
                SaveTheme(PreviewTheme.Copy());
                LoadInternalThemes();
                RenderThemePreview();
                SetEditor(false);
            });

            Dialog.EditorUpdate.gameObject.SetActive(!isDefaultTheme);
            Dialog.EditorUpdate.onClick.NewListener(() =>
            {
                var beatmapTheme = PreviewTheme.Copy(false);

                SaveTheme(beatmapTheme);

                if (InternalThemePanels.TryFind(x => x.Item && x.Item.id == beatmapTheme.id, out ThemePanel themePanel))
                {
                    themePanel.Item = beatmapTheme;
                    themePanel.Render();

                    RenderThemeList();
                }

                RenderThemePreview();
                SetEditor(false);
            });

            Dialog.EditorSaveUse.onClick.NewListener(() =>
            {
                BeatmapTheme beatmapTheme;
                if (beatmapThemeEdit && beatmapThemeEdit.isDefault)
                {
                    PreviewTheme.id = null;
                    beatmapTheme = PreviewTheme.Copy();
                }
                else
                    beatmapTheme = PreviewTheme.Copy(false);

                SaveTheme(beatmapTheme);

                if (InternalThemePanels.TryFind(x => x.Item && x.Item.id == beatmapTheme.id, out ThemePanel themePanel))
                {
                    if (!isDefaultTheme)
                    {
                        themePanel.Item = beatmapTheme;
                        themePanel.Render();
                        RenderThemeList();
                    }

                    themePanel.Use();
                }

                RenderThemePreview();
                SetEditor(false);
            });

            var bgHex = Dialog.EditorContent.Find("bg/hex").GetComponent<InputField>();
            var bgPreview = Dialog.EditorContent.Find("bg/preview").GetComponent<Image>();
            var bgPreviewET = Dialog.EditorContent.Find("bg/preview").GetComponent<EventTrigger>();
            var bgDropper = Dialog.EditorContent.Find("bg/preview/dropper").GetComponent<Image>();

            bgPreview.color = PreviewTheme.backgroundColor;
            bgHex.SetTextWithoutNotify(LSColors.ColorToHex(PreviewTheme.backgroundColor));
            bgHex.onValueChanged.NewListener(_val =>
            {
                bgPreview.color = _val.Length == 6 ? LSColors.HexToColor(_val) : LSColors.pink500;
                PreviewTheme.backgroundColor = _val.Length == 6 ? LSColors.HexToColor(_val) : LSColors.pink500;

                SetDropper(bgDropper, bgPreview, bgHex, bgPreviewET, PreviewTheme.backgroundColor);
            });

            SetDropper(bgDropper, bgPreview, bgHex, bgPreviewET, PreviewTheme.backgroundColor);

            var guiHex = Dialog.EditorContent.Find("gui/hex").GetComponent<InputField>();
            var guiPreview = Dialog.EditorContent.Find("gui/preview").GetComponent<Image>();
            var guiPreviewET = Dialog.EditorContent.Find("gui/preview").GetComponent<EventTrigger>();
            var guiDropper = Dialog.EditorContent.Find("gui/preview/dropper").GetComponent<Image>();

            guiHex.characterLimit = EditorConfig.Instance.SavingSavesThemeOpacity.Value ? 8 : 6;
            guiHex.characterValidation = InputField.CharacterValidation.None;
            guiHex.contentType = InputField.ContentType.Standard;
            guiPreview.color = PreviewTheme.guiColor;
            guiHex.SetTextWithoutNotify(EditorConfig.Instance.SavingSavesThemeOpacity.Value ? RTColors.ColorToHex(PreviewTheme.guiColor) : LSColors.ColorToHex(PreviewTheme.guiColor));
            guiHex.onValueChanged.NewListener(_val =>
            {
                guiPreview.color = _val.Length == 8 ? LSColors.HexToColorAlpha(_val) : _val.Length == 6 ? LSColors.HexToColor(_val) : LSColors.pink500;
                PreviewTheme.guiColor = _val.Length == 8 ? LSColors.HexToColorAlpha(_val) : _val.Length == 6 ? LSColors.HexToColor(_val) : LSColors.pink500;

                SetDropper(guiDropper, guiPreview, guiHex, guiPreviewET, PreviewTheme.guiColor);
            });

            SetDropper(guiDropper, guiPreview, guiHex, guiPreviewET, PreviewTheme.guiColor);

            var guiaccentHex = Dialog.EditorContent.Find("guiaccent/hex").GetComponent<InputField>();
            var guiaccentPreview = Dialog.EditorContent.Find("guiaccent/preview").GetComponent<Image>();
            var guiaccentPreviewET = Dialog.EditorContent.Find("guiaccent/preview").GetComponent<EventTrigger>();
            var guiaccentDropper = Dialog.EditorContent.Find("guiaccent/preview/dropper").GetComponent<Image>();

            guiaccentHex.characterLimit = EditorConfig.Instance.SavingSavesThemeOpacity.Value ? 8 : 6;
            guiaccentHex.characterValidation = InputField.CharacterValidation.None;
            guiaccentHex.contentType = InputField.ContentType.Standard;
            guiaccentPreview.color = PreviewTheme.guiAccentColor;
            guiaccentHex.SetTextWithoutNotify(EditorConfig.Instance.SavingSavesThemeOpacity.Value ? RTColors.ColorToHex(PreviewTheme.guiAccentColor) : LSColors.ColorToHex(PreviewTheme.guiAccentColor));
            guiaccentHex.onValueChanged.NewListener(_val =>
            {
                guiaccentPreview.color = _val.Length == 8 ? LSColors.HexToColorAlpha(_val) : _val.Length == 6 ? LSColors.HexToColor(_val) : LSColors.pink500;
                PreviewTheme.guiAccentColor = _val.Length == 8 ? LSColors.HexToColorAlpha(_val) : _val.Length == 6 ? LSColors.HexToColor(_val) : LSColors.pink500;

                SetDropper(guiaccentDropper, guiaccentPreview, guiaccentHex, guiaccentPreviewET, PreviewTheme.guiAccentColor);
            });

            SetDropper(guiaccentDropper, guiaccentPreview, guiaccentHex, guiaccentPreviewET, PreviewTheme.guiAccentColor);

            RenderColorList(Dialog.EditorContent, "player", 4, PreviewTheme.playerColors, EditorConfig.Instance.SavingSavesThemeOpacity.Value);

            RenderColorList(Dialog.EditorContent, "object", 18, PreviewTheme.objectColors, EditorConfig.Instance.SavingSavesThemeOpacity.Value);

            RenderColorList(Dialog.EditorContent, "background", 9, PreviewTheme.backgroundColors, false);

            Dialog.EditorContent.Find("effect_label").gameObject.SetActive(RTEditor.ShowModdedUI);
            RenderColorList(Dialog.EditorContent, "effect", 18, PreviewTheme.effectColors);
        }

        void RenderColorList(Transform themeContent, string name, int count, List<Color> colors, bool allowAlpha = true)
        {
            for (int i = 0; i < count; i++)
            {
                if (!themeContent.Find($"{name}{i}"))
                    continue;

                var p = themeContent.Find($"{name}{i}");

                // We have to rotate the element due to the rotation being off in unmodded.
                if (!Seasons.IsAprilFools)
                    p.transform.localRotation = Quaternion.Euler(Vector3.zero);

                bool active = RTEditor.ShowModdedUI || !name.Contains("effect") && i < 9;
                p.gameObject.SetActive(active);

                if (!active)
                    continue;

                var hex = p.Find("hex").GetComponent<InputField>();
                var preview = p.Find("preview").GetComponent<Image>();
                var previewET = p.Find("preview").GetComponent<EventTrigger>();
                var dropper = p.Find("preview").GetChild(0).GetComponent<Image>();

                int indexTmp = i;
                hex.characterLimit = allowAlpha ? 8 : 6;
                hex.characterValidation = InputField.CharacterValidation.None;
                hex.contentType = InputField.ContentType.Standard;
                preview.color = colors[indexTmp];
                hex.SetTextWithoutNotify(allowAlpha ? RTColors.ColorToHex(colors[indexTmp]) : LSColors.ColorToHex(colors[indexTmp]));
                hex.onValueChanged.NewListener(_val =>
                {
                    var color = _val.Length == 8 && allowAlpha ? LSColors.HexToColorAlpha(_val) : _val.Length == 6 ? LSColors.HexToColor(_val) : LSColors.pink500;
                    preview.color = color;
                    colors[indexTmp] = color;

                    SetDropper(dropper, preview, hex, previewET, colors[indexTmp]);
                });

                SetDropper(dropper, preview, hex, previewET, colors[indexTmp]);
            }
        }

        void SetDropper(Image dropper, Image preview, InputField hex, EventTrigger previewET, Color color)
        {
            dropper.color = RTColors.InvertColorHue(RTColors.InvertColorValue(color));
            previewET.triggers.Clear();
            previewET.triggers.Add(TriggerHelper.CreatePreviewClickTrigger(preview, dropper, hex, color));
        }

        #endregion
    }
}
