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
using BetterLegacy.Core.Runtime;
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
                Popup = RTEditor.inst.GeneratePagePopup(EditorPopup.THEME_POPUP, "Beatmap Themes", Vector2.zero, new Vector2(600f, 450f), _val =>
                {
                    RenderExternalThemesPopup();
                }, placeholderText: "Search for theme...");

                Popup.Grid.cellSize = new Vector2(600f, 362f);
                Popup.getMaxPageCount = () => ThemeManager.inst.ThemeCount / themesPerPage;

                EditorHelper.AddEditorDropdown("View Themes", "", "View", EditorSprites.SearchSprite, OpenExternalThemesPopup);

                // Prefab
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

                // Prefab
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

                    var useTheme = EditorPrefabHolder.Instance.Function2Button.Duplicate(buttons.transform, "use");
                    var useThemeStorage = useTheme.GetComponent<FunctionButtonStorage>();
                    useTheme.SetActive(false);
                    var useThemeText = useThemeStorage.label;
                    useThemeText.fontSize = 16;
                    useThemeText.text = "Use Theme";

                    viewThemeStorage.useButton = useThemeStorage.button;

                    var exportToVG = EditorPrefabHolder.Instance.Function2Button.Duplicate(buttons.transform, "convert");
                    var exportToVGStorage = exportToVG.GetComponent<FunctionButtonStorage>();
                    exportToVG.SetActive(false);
                    var exportToVGText = exportToVGStorage.label;
                    exportToVGText.fontSize = 16;
                    exportToVGText.text = "Convert to VG Format";

                    viewThemeStorage.convertButton = exportToVGStorage.button;

                    Destroy(themePopupPanelPrefab.GetComponent<Button>());
                }
            }
            catch (Exception ex)
            {
                CoreHelper.LogError($"{ex}");
            }

            Dialog.Editor.AddComponent<ActiveState>().onStateChanged = OnDialog;

            PreviewTheme = ThemeManager.inst.DefaultThemes[0];
            StartCoroutine(LoadThemes());
        }

        #endregion

        #region Values

        public BeatmapTheme PreviewTheme { get; set; }

        public PageContentPopup Popup { get; set; }

        public ThemeKeyframeDialog Dialog { get; set; }

        public bool loadingThemes = false;

        public bool shouldCutTheme;
        public string copiedThemePath;

        public bool themesLoading = false;

        public GameObject themePopupPanelPrefab;

        public static int themesPerPage = 10;

        public static int ThemePreviewColorCount => 4;

        public List<ThemePanel> ThemePanels { get; set; } = new List<ThemePanel>();

        public static int eventThemesPerPage = 30;
        public int CurrentEventThemePage => Dialog.Page + 1;
        public int MinEventTheme => MaxEventTheme - eventThemesPerPage;
        public int MaxEventTheme => CurrentEventThemePage * eventThemesPerPage;
        public int ThemesCount => ThemePanels.FindAll(x => RTString.SearchString(Dialog.SearchTerm, x.isFolder ? Path.GetFileName(x.Path) : x.Item.name)).Count;

        public bool filterUsed;

        #endregion

        #region Methods

        public void OnDialog(bool enabled)
        {
            if (!enabled && EventEditor.inst)
                EventEditor.inst.showTheme = false;
        }

        public IEnumerator LoadThemes(bool refreshGUI = false)
        {
            if (themesLoading)
                yield break;

            themesLoading = true;

            ThemeManager.inst.Clear();

            if (!ThemePanels.IsEmpty())
                ThemePanels.ForEach(x => Destroy(x.GameObject));
            ThemePanels.Clear();

            int themePanelIndex = 0;
            try
            {
                Dialog.themeUpFolderButton.SetActive(RTFile.GetDirectory(RTFile.CombinePaths(RTEditor.inst.BeatmapsPath, RTEditor.inst.ThemePath)) != RTEditor.inst.BeatmapsPath);

                foreach (var directory in Directory.GetDirectories(RTFile.CombinePaths(RTEditor.inst.BeatmapsPath, RTEditor.inst.ThemePath), "*", SearchOption.TopDirectoryOnly))
                {
                    var themePanel = new ThemePanel(themePanelIndex);
                    themePanel.Init(directory);

                    ThemePanels.Add(themePanel);
                    themePanelIndex++;
                }
            }
            catch (Exception ex)
            {
                CoreHelper.LogException(ex);
            }

            foreach (var beatmapTheme in ThemeManager.inst.DefaultThemes)
            {
                var themePanel = new ThemePanel(themePanelIndex);
                themePanel.Init(beatmapTheme);

                ThemePanels.Add(themePanel);

                themePanelIndex++;
            }

            var files = Directory.GetFiles(RTFile.CombinePaths(RTEditor.inst.BeatmapsPath, RTEditor.inst.ThemePath), FileFormat.LST.ToPattern());
            for (int i = 0; i < files.Length; i++)
            {
                var file = files[i];
                var jn = JSON.Parse(RTFile.ReadFromFile(file));
                var beatmapTheme = BeatmapTheme.Parse(jn);
                beatmapTheme.filePath = RTFile.ReplaceSlash(file);

                bool isDuplicate = !ThemeManager.inst.AddTheme(beatmapTheme);

                var themePanel = new ThemePanel(themePanelIndex, isDuplicate);
                themePanel.Init(beatmapTheme);

                ThemePanels.Add(themePanel);

                themePanelIndex++;
            }

            ThemeManager.inst.UpdateAllThemes();
            GameData.Current?.UpdateUsedThemes();

            themesLoading = false;

            if (refreshGUI)
                yield return StartCoroutine(RenderThemeList());

            yield break;
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
            Popup.ClearContent();

            var layer = Popup.Page + 1;

            TriggerHelper.AddEventTriggers(Popup.PageField.inputField.gameObject, TriggerHelper.ScrollDeltaInt(Popup.PageField.inputField, max: ThemeManager.inst.ThemeCount / themesPerPage));

            int num = 0;
            foreach (var beatmapTheme in ThemeManager.inst.AllThemes)
            {
                int max = layer * themesPerPage;

                var name = beatmapTheme.name ?? "theme";
                if (!RTString.SearchString(Popup.SearchTerm, name) || num < max - themesPerPage || num >= max)
                {
                    num++;
                    continue;
                }

                var gameObject = themePopupPanelPrefab.Duplicate(Popup.Content, name);
                gameObject.transform.localScale = Vector3.one;

                var viewThemeStorage = gameObject.GetComponent<ViewThemePanelStorage>();
                viewThemeStorage.text.text = $"{name} [ ID: {beatmapTheme.id} ]";

                EditorThemeManager.ApplyLightText(viewThemeStorage.baseColorsText);
                EditorThemeManager.ApplyLightText(viewThemeStorage.playerColorsText);
                EditorThemeManager.ApplyLightText(viewThemeStorage.objectColorsText);
                EditorThemeManager.ApplyLightText(viewThemeStorage.backgroundColorsText);
                EditorThemeManager.ApplyLightText(viewThemeStorage.effectColorsText);

                for (int i = 0; i < viewThemeStorage.baseColors.Count; i++)
                {
                    viewThemeStorage.baseColors[i].color = i == 0 ? beatmapTheme.backgroundColor : i == 1 ? beatmapTheme.guiAccentColor : beatmapTheme.guiAccentColor;
                    EditorThemeManager.ApplyGraphic(viewThemeStorage.baseColors[i], ThemeGroup.Null, true);
                }

                for (int i = 0; i < viewThemeStorage.playerColors.Count; i++)
                {
                    if (i < beatmapTheme.playerColors.Count)
                    {
                        viewThemeStorage.playerColors[i].color = beatmapTheme.playerColors[i];
                        EditorThemeManager.ApplyGraphic(viewThemeStorage.playerColors[i], ThemeGroup.Null, true);
                    }
                    else
                        viewThemeStorage.playerColors[i].gameObject.SetActive(false);
                }

                for (int i = 0; i < viewThemeStorage.objectColors.Count; i++)
                {
                    if (i < beatmapTheme.objectColors.Count)
                    {
                        viewThemeStorage.objectColors[i].color = beatmapTheme.objectColors[i];
                        EditorThemeManager.ApplyGraphic(viewThemeStorage.objectColors[i], ThemeGroup.Null, true);
                    }
                    else
                        viewThemeStorage.objectColors[i].gameObject.SetActive(false);
                }

                for (int i = 0; i < viewThemeStorage.backgroundColors.Count; i++)
                {
                    if (i < beatmapTheme.backgroundColors.Count)
                    {
                        viewThemeStorage.backgroundColors[i].color = beatmapTheme.backgroundColors[i];
                        EditorThemeManager.ApplyGraphic(viewThemeStorage.backgroundColors[i], ThemeGroup.Null, true);
                    }
                    else
                        viewThemeStorage.backgroundColors[i].gameObject.SetActive(false);
                }

                for (int i = 0; i < viewThemeStorage.effectColors.Count; i++)
                {
                    if (i < beatmapTheme.effectColors.Count)
                    {
                        viewThemeStorage.effectColors[i].color = beatmapTheme.effectColors[i];
                        EditorThemeManager.ApplyGraphic(viewThemeStorage.effectColors[i], ThemeGroup.Null, true);
                    }
                    else
                        viewThemeStorage.effectColors[i].gameObject.SetActive(false);
                }

                var use = viewThemeStorage.useButton;
                var useStorage = use.GetComponent<FunctionButtonStorage>();
                use.onClick.NewListener(() =>
                {
                    if (RTEventEditor.inst.SelectedKeyframes.Count > 1 && RTEventEditor.inst.SelectedKeyframes.All(x => x.Type == 4))
                        foreach (var timelineObject in RTEventEditor.inst.SelectedKeyframes)
                            timelineObject.eventKeyframe.values[0] = Parser.TryParse(beatmapTheme.id, 0);
                    else if (EventEditor.inst.currentEventType == 4)
                        GameData.Current.events[4][EventEditor.inst.currentEvent].values[0] = Parser.TryParse(beatmapTheme.id, 0);
                    else if (GameData.Current.events[4].Count > 0)
                        GameData.Current.events[4].FindLast(x => x.time < AudioManager.inst.CurrentAudioSource.time).values[0] = Parser.TryParse(beatmapTheme.id, 0);

                    RTLevel.Current?.UpdateEvents(4);
                });

                var convert = viewThemeStorage.convertButton;
                var convertStorage = convert.GetComponent<FunctionButtonStorage>();
                convert.onClick.NewListener(() => ConvertTheme(beatmapTheme));

                EditorThemeManager.ApplyGraphic(viewThemeStorage.baseImage, ThemeGroup.List_Button_1_Normal, true);
                EditorThemeManager.ApplyLightText(viewThemeStorage.text);
                EditorThemeManager.ApplySelectable(use, ThemeGroup.Function_2);
                EditorThemeManager.ApplyGraphic(useStorage.label, ThemeGroup.Function_2_Text);
                EditorThemeManager.ApplySelectable(convert, ThemeGroup.Function_2);
                EditorThemeManager.ApplyGraphic(convertStorage.label, ThemeGroup.Function_2_Text);

                use.gameObject.SetActive(true);
                convert.gameObject.SetActive(true);

                num++;
            }
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

        public IEnumerator RenderThemeList()
        {
            if (!GameData.Current)
                yield break;

            if (!loadingThemes && !EventEditor.inst.eventDrag)
            {
                loadingThemes = true;

                int num = 0;
                for (int i = 0; i < ThemePanels.Count; i++)
                {
                    var themePanel = ThemePanels[i];
                    var isFolder = themePanel.isFolder;
                    var searchBool = (!themePanel.isDefault || EditorConfig.Instance.ShowDefaultThemes.Value) &&
                        RTString.SearchString(Dialog.SearchTerm, isFolder ? Path.GetFileName(themePanel.Path) : themePanel.Item.name);

                    if (filterUsed && !isFolder && !GameData.Current.events[4].Any(x => x.values[0] == Parser.TryParse(themePanel.Item.id, -1)))
                        searchBool = false;

                    if (searchBool)
                        num++;

                    if (!themePanel.GameObject)
                    {
                        throw new NullReferenceException($"Theme Panel object was null. Index: {i} Item: {themePanel.Item}");
                        //ThemePanels[i] = isFolder ? SetupThemePanel(themePanel.Path, false, i) : SetupThemePanel(themePanel.Item, Parser.TryParse(themePanel.Item.id, 0) < 10, themePanel.isDuplicate, false, i);
                    }
                    ThemePanels[i].SetActive(num >= MinEventTheme && num < MaxEventTheme && searchBool);
                }

                if (ThemesCount > eventThemesPerPage)
                    TriggerHelper.AddEventTriggers(Dialog.PageField.inputField.gameObject, TriggerHelper.ScrollDeltaInt(Dialog.PageField.inputField, max: ThemesCount / eventThemesPerPage));
                else
                    TriggerHelper.AddEventTriggers(Dialog.PageField.inputField.gameObject);

                loadingThemes = false;
            }

            yield break;
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

        /// <summary>
        /// Saves a theme.
        /// </summary>
        /// <param name="theme">Theme to save.</param>
        public void SaveTheme(BeatmapTheme theme)
        {
            Debug.Log($"{EventEditor.inst.className}Saving {theme.id} ({theme.name}) to File System!");

            RTEditor.inst.DisableThemeWatcher();

            if (string.IsNullOrEmpty(theme.id))
                theme.id = LSText.randomNumString(BeatmapTheme.ID_LENGTH);

            var config = EditorConfig.Instance;

            GameData.SaveOpacityToThemes = config.SavingSavesThemeOpacity.Value;

            var str = config.ThemeSavesIndents.Value ? theme.ToJSON().ToString(3) : theme.ToJSON().ToString();

            var path = RTFile.CombinePaths(RTEditor.inst.BeatmapsPath, RTEditor.inst.ThemePath, $"{RTFile.FormatLegacyFileName(theme.name)}{FileFormat.LST.Dot()}");

            theme.filePath = path;

            RTFile.WriteToFile(path, str);

            EditorManager.inst.DisplayNotification($"Saved theme [{theme.name}]!", 2f, EditorManager.NotificationType.Success);

            RTEditor.inst.EnableThemeWatcher();
        }

        /// <summary>
        /// Deletes a theme.
        /// </summary>
        /// <param name="theme">Theme to delete.</param>
        public void DeleteTheme(BeatmapTheme theme) => RTEditor.inst.ShowWarningPopup("Are you sure you want to delete this theme?", () =>
        {
            RTEditor.inst.DisableThemeWatcher();

            RTFile.DeleteFile(theme.filePath);

            if (ThemePanels.TryFindIndex(x => x.Item != null && x.Item.id == theme.id, out int themePanelIndex))
            {
                var themePanel = ThemePanels[themePanelIndex];
                CoreHelper.Delete(themePanel.GameObject);
                ThemePanels.RemoveAt(themePanelIndex);

                ThemeManager.inst.customThemes.Remove(x => x.id == themePanel.Item.id);
            }

            RTEditor.inst.EnableThemeWatcher();

            CoroutineHelper.StartCoroutine(RenderThemeList());
            RenderThemePreview();
            EventEditor.inst.showTheme = false;
            Dialog.Editor.SetActive(false);
            GameData.Current?.UpdateUsedThemes();

            RTEditor.inst.HideWarningPopup();
        }, RTEditor.inst.HideWarningPopup);

        /// <summary>
        /// Shuffles a theme's ID.
        /// </summary>
        /// <param name="beatmapTheme">Theme to shuffle the ID of.</param>
        public void ShuffleThemeID(BeatmapTheme beatmapTheme) => RTEditor.inst.ShowWarningPopup("Are you sure you want to shuffle the theme ID? Any levels that use this theme will need to have their theme keyframes reassigned.", () =>
        {
            beatmapTheme.id = LSText.randomNumString(BeatmapTheme.ID_LENGTH);
            RTEditor.inst.HideWarningPopup();
        }, RTEditor.inst.HideWarningPopup);

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

            var theme = Dialog.Editor.transform;
            Dialog.Editor.SetActive(true);
            EventEditor.inst.showTheme = true;

            if (!Seasons.IsAprilFools)
            {
                theme.Find("theme").localRotation = Quaternion.Euler(Vector3.zero);
                foreach (Transform child in Dialog.EditorContent)
                    child.localRotation = Quaternion.Euler(Vector3.zero);
            }

            theme.Find("theme_title/Text").GetComponent<Text>().text = !newTheme ? $"- Theme Editor (ID: {beatmapThemeEdit.id}) -" : "- Theme Editor -";

            var isDefaultTheme = beatmapThemeEdit && beatmapThemeEdit.isDefault;

            Dialog.EditorShuffleID.gameObject.SetActive(!newTheme && !isDefaultTheme);
            if (!newTheme && !isDefaultTheme)
                Dialog.EditorShuffleID.onClick.NewListener(() => ShuffleThemeID(PreviewTheme));

            Dialog.EditorNameField.SetTextWithoutNotify(PreviewTheme.name);
            Dialog.EditorNameField.onValueChanged.NewListener(_val => PreviewTheme.name = _val);
            Dialog.EditorCancel.onClick.NewListener(() =>
            {
                EventEditor.inst.showTheme = false;
                theme.gameObject.SetActive(false);
            });

            Dialog.EditorCreateNew.gameObject.SetActive(true);
            Dialog.EditorCreateNew.onClick.NewListener(() =>
            {
                PreviewTheme.id = null;
                SaveTheme(PreviewTheme.Copy());
                EventEditor.inst.StartCoroutine(LoadThemes(true));
                var child = EventEditor.inst.dialogRight.GetChild(EventEditor.inst.currentEventType);
                RenderThemePreview();
                EventEditor.inst.showTheme = false;
                theme.gameObject.SetActive(false);
            });

            Dialog.EditorUpdate.gameObject.SetActive(!isDefaultTheme);
            Dialog.EditorUpdate.onClick.NewListener(() =>
            {
                var beatmapTheme = PreviewTheme.Copy(false);

                if (ThemePanels.TryFind(x => x.Item && x.Item.id == beatmapTheme.id, out ThemePanel themePanel) && RTFile.FileExists(themePanel.Path))
                {
                    CoreHelper.Log($"Deleting original theme...");
                    RTFile.DeleteFile(themePanel.Path);
                }

                SaveTheme(beatmapTheme);
                if (ThemeManager.inst.CustomThemes.TryFindIndex(x => x.id == beatmapTheme.id, out int themeIndex))
                    ThemeManager.inst.CustomThemes[themeIndex] = beatmapTheme;

                ThemeManager.inst.UpdateAllThemes();

                if (themePanel)
                {
                    themePanel.Item = beatmapTheme;
                    themePanel.Render();

                    CoroutineHelper.StartCoroutine(RenderThemeList());
                }

                RenderThemePreview();
                EventEditor.inst.showTheme = false;
                theme.gameObject.SetActive(false);
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
                {
                    beatmapTheme = PreviewTheme.Copy(false);

                    if (ThemePanels.TryFind(x => x.Item && x.Item.id == beatmapTheme.id, out ThemePanel themePanel1) && RTFile.FileExists(themePanel1.Path))
                    {
                        CoreHelper.Log($"Deleting original theme...");
                        RTFile.DeleteFile(themePanel1.Path);
                    }
                }

                SaveTheme(beatmapTheme);
                if (isDefaultTheme)
                    StartCoroutine(LoadThemes(true));

                if (ThemeManager.inst.CustomThemes.TryFindIndex(x => x.id == beatmapTheme.id, out int themeIndex))
                    ThemeManager.inst.CustomThemes[themeIndex] = beatmapTheme;

                ThemeManager.inst.UpdateAllThemes();

                if (ThemePanels.TryFind(x => x.Item && x.Item.id == beatmapTheme.id, out ThemePanel themePanel))
                {
                    if (!isDefaultTheme)
                    {
                        themePanel.Item = beatmapTheme;
                        themePanel.Render();
                        CoroutineHelper.StartCoroutine(RenderThemeList());
                    }

                    themePanel.Use();
                }

                RenderThemePreview();
                EventEditor.inst.showTheme = false;
                theme.gameObject.SetActive(false);
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
