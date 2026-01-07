using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

using LSFunctions;

using SimpleJSON;
using Crosstales.FB;

using BetterLegacy.Configs;
using BetterLegacy.Core;
using BetterLegacy.Core.Components;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Data.Level;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Prefabs;
using BetterLegacy.Editor.Components;
using BetterLegacy.Editor.Managers;

namespace BetterLegacy.Editor.Data.Elements
{
    /// <summary>
    /// Object for storing level panel data.
    /// </summary>
    public class LevelPanel : EditorPanel<Level>
    {
        public LevelPanel() { }

        #region Values

        #region UI

        /// <summary>
        /// The icon of the level panel.
        /// </summary>
        public Image IconImage { get; set; }

        /// <summary>
        /// The delete button of the level panel.
        /// </summary>
        public DeleteButtonStorage DeleteButton { get; set; }

        /// <summary>
        /// The UI that displays that the level is selected.
        /// </summary>
        public GameObject SelectedUI { get; set; }

        public GameObject Progress { get; set; }

        #endregion

        #region Data

        public LevelInfo Info { get; set; }

        public EditorInfo EditorInfo { get; set; }

        public override float FocusSize => EditorConfig.Instance.OpenLevelButtonHoverSize.Value;

        public override string DisplayName
        {
            get
            {
                if (isFolder)
                    return Name;

                if (Info)
                    return $"/{Info.arcadeID} - {Info.name}";

                var metadata = Item.metadata;
                return string.Format(labelFormat,
                    LSText.ClampString(Name, labelFolderNameMax),
                    LSText.ClampString(metadata.song.title, labelSongTitleMax),
                    LSText.ClampString(metadata.artist.name, labelArtistNameMax),
                    LSText.ClampString(metadata.creator.name, labelCreatorNameMax),
                    metadata.song.Difficulty.DisplayName,
                    LSText.ClampString(metadata.song.description, labelDescriptionMax),
                    LSText.ClampString(metadata.beatmap.dateEdited, labelDateMax),
                    LSText.ClampString(metadata.beatmap.dateCreated, labelDateMax),
                    LSText.ClampString(metadata.beatmap.datePublished, labelDateMax));
            }
        }

        bool selected;
        /// <summary>
        /// If the level is selected.
        /// </summary>
        public bool Selected
        {
            get => selected;
            set
            {
                selected = value;
                RenderSelected(value);
            }
        }

        /// <summary>
        /// Names of all the level difficulties
        /// </summary>
        public static string[] difficultyNames = new string[]
        {
            "easy",
            "normal",
            "hard",
            "expert",
            "expert+",
            "master",
            "animation",
            "Unknown difficulty",
        };

        #endregion

        #region Asset Pack

        public static RectValues baseRect = new RectValues(Vector2.zero, new Vector2(0f, 1f), new Vector2(0f, 1f), RectValues.CenterPivot, new Vector2(584f, 32f));

        public static RectValues labelRect = RectValues.FullAnchored.AnchoredPosition(32f, 0f).SizeDelta(-12f, -8f);

        public static RectValues iconRect = RectValues.Default.AnchoredPosition(-276f, 0f).SizeDelta(26f, 26f);

        public static RectValues deleteRect = new RectValues(Vector2.zero, Vector2.one, new Vector2(1f, 0f), new Vector2(1f, 0.5f), new Vector2(32f, 0f));

        public static RectValues progressRect = new RectValues(Vector2.zero, Vector2.one, Vector2.one, Vector2.one, new Vector2(44f, 20f));

        public static RectValues checkmarkRect = new RectValues(Vector2.zero, Vector2.one, Vector2.one, Vector2.one, new Vector2(32f, 32f));

        public static string labelFormat = "/{0} : {1} by {2}";

        public static TextAnchor labelAlignment = TextAnchor.MiddleLeft;

        public static HorizontalWrapMode labelHorizontalWrap = HorizontalWrapMode.Wrap;

        public static VerticalWrapMode labelVerticalWrap = VerticalWrapMode.Truncate;

        public static int labelFontSize = 20;

        public static int labelFolderNameMax = 16;

        public static int labelSongTitleMax = 22;

        public static int labelArtistNameMax = 16;

        public static int labelCreatorNameMax = 16;

        public static int labelDescriptionMax = 16;

        public static int labelDateMax = 16;

        #endregion

        #endregion

        #region Methods

        public override void Init(string directory)
        {
            Path = directory;
            isFolder = true;

            var gameObject = GameObject;
            if (gameObject)
                CoreHelper.Destroy(gameObject);

            gameObject = EditorManager.inst.folderButtonPrefab.Duplicate(EditorLevelManager.inst.OpenLevelPopup.Content, $"Folder [{Name}]");
            baseRect.AssignToRectTransform(gameObject.transform.AsRT());
            GameObject = gameObject;

            Button = gameObject.AddComponent<FolderButtonFunction>();

            HoverFocus = gameObject.AddComponent<HoverUI>();
            HoverFocus.size = EditorConfig.Instance.OpenLevelButtonHoverSize.Value;
            HoverFocus.animatePos = false;
            HoverFocus.animateSca = true;

            var folderButtonStorage = gameObject.GetComponent<FunctionButtonStorage>();
            Label = folderButtonStorage.label;
            Label.enabled = true;
            labelRect.AssignToRectTransform(Label.rectTransform);
            folderButtonStorage.OnClick.ClearAll();

            var iconBase = Creator.NewUIObject("icon base", gameObject.transform);
            var iconBaseImage = iconBase.AddComponent<Image>();
            iconBase.AddComponent<Mask>().showMaskGraphic = false;
            iconRect.AssignToRectTransform(iconBaseImage.rectTransform);
            EditorThemeManager.ApplyGraphic(iconBaseImage, ThemeGroup.Null, true);

            var icon = Creator.NewUIObject("icon", iconBase.transform);
            RectValues.FullAnchored.AssignToRectTransform(icon.transform.AsRT());
            var iconImage = icon.AddComponent<Image>();
            iconImage.sprite = EditorSprites.OpenSprite;
            IconImage = iconImage;

            EditorThemeManager.ApplySelectable(folderButtonStorage.button, ThemeGroup.List_Button_1);
            EditorThemeManager.ApplyLightText(folderButtonStorage.label);

            Render();
        }

        public override void Init(Level level)
        {
            Item = level;
            Path = level.path;
            Info = level.collectionInfo;
            level.editorLevelPanel = this;
            level.isEditor = true;

            LoadEditorInfo();

            var gameObject = GameObject;
            if (gameObject)
                CoreHelper.Destroy(gameObject);

            gameObject = EditorManager.inst.folderButtonPrefab.Duplicate(EditorLevelManager.inst.OpenLevelPopup.Content, $"Folder [{Name}]");
            baseRect.AssignToRectTransform(gameObject.transform.AsRT());
            GameObject = gameObject;
            var folderButtonFunction = gameObject.AddComponent<FolderButtonFunction>();

            HoverFocus = gameObject.AddComponent<HoverUI>();
            HoverFocus.animatePos = false;
            HoverFocus.animateSca = true;

            var folderButtonStorage = gameObject.GetComponent<FunctionButtonStorage>();
            Label = folderButtonStorage.label;
            Label.enabled = true;
            labelRect.AssignToRectTransform(Label.rectTransform);
            folderButtonStorage.OnClick.ClearAll();
            Button = folderButtonFunction;
            EditorThemeManager.ApplySelectable(folderButtonStorage.button, ThemeGroup.List_Button_1);
            EditorThemeManager.ApplyLightText(folderButtonStorage.label);

            var iconBase = Creator.NewUIObject("icon base", gameObject.transform);
            var iconBaseImage = iconBase.AddComponent<Image>();
            iconBase.AddComponent<Mask>().showMaskGraphic = false;
            iconRect.AssignToRectTransform(iconBaseImage.rectTransform);
            EditorThemeManager.ApplyGraphic(iconBaseImage, ThemeGroup.Null, true);

            var icon = Creator.NewUIObject("icon", iconBase.transform);
            RectValues.FullAnchored.AssignToRectTransform(icon.transform.AsRT());

            var iconImage = icon.AddComponent<Image>();
            iconImage.sprite = Item.icon;
            IconImage = iconImage;

            var delete = EditorPrefabHolder.Instance.DeleteButton.Duplicate(gameObject.transform, "delete");

            var deleteStorage = delete.GetComponent<DeleteButtonStorage>();
            DeleteButton = deleteStorage;

            deleteRect.AssignToRectTransform(delete.transform.AsRT());

            EditorThemeManager.ApplyGraphic(deleteStorage.baseImage, ThemeGroup.Delete, true, roundedSide: SpriteHelper.RoundedSide.W);
            EditorThemeManager.ApplyGraphic(deleteStorage.image, ThemeGroup.Delete_Text, false, roundedSide: SpriteHelper.RoundedSide.W);
            delete.SetActive(EditorConfig.Instance.OpenLevelShowDeleteButton.Value);

            SelectedUI = Creator.NewUIObject("selected", gameObject.transform);
            SelectedUI.SetActive(false);
            var selectedImage = SelectedUI.AddComponent<Image>();
            selectedImage.color = LSColors.HexToColorAlpha("0088FF25");

            RectValues.FullAnchored.AssignToRectTransform(selectedImage.rectTransform);

            Render();
            RenderProgress();
        }

        public void Init(LevelInfo levelInfo)
        {
            Info = levelInfo;
            Path = levelInfo.path;

            var gameObject = GameObject;
            if (gameObject)
                CoreHelper.Destroy(gameObject);

            gameObject = EditorManager.inst.folderButtonPrefab.Duplicate(EditorLevelManager.inst.OpenLevelPopup.Content, $"Folder [{Name}]");
            baseRect.AssignToRectTransform(gameObject.transform.AsRT());
            GameObject = gameObject;
            var folderButtonFunction = gameObject.AddComponent<FolderButtonFunction>();

            HoverFocus = gameObject.AddComponent<HoverUI>();
            HoverFocus.animatePos = false;
            HoverFocus.animateSca = true;

            var folderButtonStorage = gameObject.GetComponent<FunctionButtonStorage>();
            Label = folderButtonStorage.label;
            Label.enabled = true;
            labelRect.AssignToRectTransform(Label.rectTransform);
            folderButtonStorage.OnClick.ClearAll();
            Button = folderButtonFunction;
            EditorThemeManager.ApplySelectable(folderButtonStorage.button, ThemeGroup.List_Button_1);
            EditorThemeManager.ApplyLightText(folderButtonStorage.label);

            var iconBase = Creator.NewUIObject("icon base", gameObject.transform);
            var iconBaseImage = iconBase.AddComponent<Image>();
            iconBase.AddComponent<Mask>().showMaskGraphic = false;
            iconRect.AssignToRectTransform(iconBaseImage.rectTransform);
            EditorThemeManager.ApplyGraphic(iconBaseImage, ThemeGroup.Null, true);

            var icon = Creator.NewUIObject("icon", iconBase.transform);
            RectValues.FullAnchored.AssignToRectTransform(icon.transform.AsRT());
            var iconImage = icon.AddComponent<Image>();
            iconImage.sprite = Info?.icon ?? Info?.level?.icon ?? LegacyPlugin.AtanPlaceholder;
            IconImage = iconImage;

            var delete = EditorPrefabHolder.Instance.DeleteButton.Duplicate(gameObject.transform, "delete");

            var deleteStorage = delete.GetComponent<DeleteButtonStorage>();
            DeleteButton = deleteStorage;

            deleteRect.AssignToRectTransform(delete.transform.AsRT());

            new RectValues(Vector2.zero, Vector2.one, new Vector2(1f, 0f), new Vector2(1f, 0.5f), new Vector2(32f, 0f)).AssignToRectTransform(delete.transform.AsRT());

            EditorThemeManager.ApplyGraphic(deleteStorage.baseImage, ThemeGroup.Delete, true, roundedSide: SpriteHelper.RoundedSide.W);
            EditorThemeManager.ApplyGraphic(deleteStorage.image, ThemeGroup.Delete_Text, false, roundedSide: SpriteHelper.RoundedSide.W);
            delete.SetActive(true);

            SelectedUI = Creator.NewUIObject("selected", gameObject.transform);
            SelectedUI.SetActive(false);
            var selectedImage = SelectedUI.AddComponent<Image>();
            selectedImage.color = LSColors.HexToColorAlpha("0088FF25");

            RectValues.FullAnchored.AssignToRectTransform(selectedImage.rectTransform);

            Render();
        }

        public override void Render()
        {
            if (GameObject)
                baseRect.AssignToRectTransform(GameObject.transform.AsRT());

            RenderIcon();
            RenderLabel();
            RenderHover();
            RenderTooltip();
            UpdateFunction();
            UpdateDeleteFunction();
        }

        /// <summary>
        /// Renders the level panel icon.
        /// </summary>
        public void RenderIcon()
        {
            if (isFolder)
                return;

            RenderIcon(Item?.icon ?? Info?.level?.icon ?? LegacyPlugin.AtanPlaceholder);
        }

        /// <summary>
        /// Renders the level panel icon.
        /// </summary>
        /// <param name="icon">Icon of the level panel.</param>
        public void RenderIcon(Sprite icon)
        {
            if (IconImage)
                IconImage.sprite = icon;
        }

        public override void RenderLabel(string text)
        {
            Label.text = text;

            Label.alignment = labelAlignment;
            Label.horizontalOverflow = labelHorizontalWrap;
            Label.verticalOverflow = labelVerticalWrap;
            Label.fontSize = labelFontSize;
        }

        public override void RenderTooltip()
        {
            TooltipHelper.AssignTooltip(GameObject, "Level Panel", 3f);

            if (isFolder)
            {
                GetFolderTooltip();
                return;
            }

            if (!Item)
            {
                var info = GetLevelInfo();
                if (!info)
                    return;

                TooltipHelper.AddHoverTooltip(GameObject, $"{info.songTitle} by {info.creator}",
                    $"<br>Folder: {info.path}<br>" +
                    $"<br>Folder: {info.path}<br>" +
                    $"Editor Folder: {info.editorPath}<br>" +
                    $"ID: {info.id}<br>" +
                    $"Find Arcade ID: {info.arcadeID}" +
                    $"Find Server ID: {info.serverID}" +
                    $"Find Workshop ID: {info.workshopID}");
                return;
            }

            var metadata = Item.metadata;

            TooltipHelper.AddHoverTooltip(GameObject, "<#" + LSColors.ColorToHex(metadata.song.Difficulty.Color) + ">" + metadata.artist.name + " - " + metadata.song.title,
                $"</color><br>Folder: {Item.FolderName}<br>Date Edited: {metadata.beatmap.dateEdited}<br>Date Created: {metadata.beatmap.dateCreated}<br>Description: {metadata.song.description}");
        }

        /// <summary>
        /// Renders the levels' progress.
        /// </summary>
        public void RenderProgress()
        {
            CoreHelper.Delete(Progress);
            if (!EditorInfo)
                return;

            if (EditorInfo.IsComplete)
            {
                var checkmark = Creator.NewUIObject("check", GameObject.transform);
                checkmarkRect.AssignToRectTransform(checkmark.transform.AsRT());
                var checkmarkImage = checkmark.AddComponent<Image>();
                checkmarkImage.sprite = EditorSprites.CheckmarkSprite;
                EditorThemeManager.ApplyGraphic(checkmarkImage, ThemeGroup.Light_Text);
                Progress = checkmark;
            }
            else
            {
                var labelElement = new LabelElement($"{RTString.Percentage(EditorInfo.Progress, 100f)}%");
                labelElement.Init(EditorElement.InitSettings.Default.Parent(GameObject.transform).Rect(progressRect));
                Progress = labelElement.GameObject;
            }
        }

        /// <summary>
        /// Updates the level panels' main function.
        /// </summary>
        public void UpdateFunction()
        {
            if (isFolder)
            {
                var path = Path;
                Button.onClick = eventData =>
                {
                    if (!path.Contains(RTEditor.inst.BeatmapsPath + "/"))
                    {
                        EditorManager.inst.DisplayNotification($"Path does not contain the proper directory.", 2f, EditorManager.NotificationType.Warning);
                        return;
                    }

                    if (eventData.button == PointerEventData.InputButton.Right)
                    {
                        EditorContextMenu.inst.ShowContextMenu(
                            new ButtonElement("Open folder", () =>
                            {
                                EditorLevelManager.inst.OpenLevelPopup.PathField.text = path.Remove(RTEditor.inst.BeatmapsPath + "/");
                                RTEditor.inst.UpdateEditorPath(false);
                            }, "Level Panel Open Folder"),
                            new ButtonElement("Create folder", () => RTEditor.inst.ShowFolderCreator(RTFile.GetDirectory(path), EndFolderCreation), "Level Panel Create Folder"),
                            new ButtonElement("Create level", EditorManager.inst.OpenNewLevelPopup),
                            new SpacerElement(),
                            new ButtonElement("Rename", () => RTEditor.inst.ShowNameEditor("Folder Renamer", "Folder name", "Rename", () =>
                            {
                                RTFile.MoveDirectory(path, path.Replace(Name, RTFile.ValidateDirectory(RTEditor.inst.folderCreatorName.text)).Replace("\\", "/"));

                                EditorLevelManager.inst.LoadLevels();
                                RTEditor.inst.HideNameEditor();
                            }), "Level Panel Rename Folder"),
                            new ButtonElement("Paste", EditorLevelManager.inst.PasteLevel, "Level Panel Paste"),
                            new ButtonElement("Delete", () => RTEditor.inst.ShowWarningPopup("Are you <b>100%</b> sure you want to delete this folder? This <b>CANNOT</b> be undone! Always make sure you have backups.", () =>
                            {
                                RTFile.DeleteDirectory(path);
                                RTEditor.inst.UpdateEditorPath(true);
                                EditorManager.inst.DisplayNotification("Deleted folder!", 2f, EditorManager.NotificationType.Success);
                            }), "Level Panel Delete"),
                            new SpacerElement(),
                            new ButtonElement("ZIP Folder", () => EditorLevelManager.inst.ZipLevel(this), "Level Panel ZIP"),
                            new ButtonElement("Copy Path", () => LSText.CopyToClipboard(path), "Level Panel Copy Folder"),
                            new ButtonElement("Copy Relative Path", () => LSText.CopyToClipboard(path.Remove(RTEditor.inst.BeatmapsPath + "/")), "Level Panel Copy Folder"),
                            new ButtonElement("Open in File Explorer", () => RTFile.OpenInFileBrowser.Open(path), "Level Panel Open Explorer"),
                            new ButtonElement("Open List in File Explorer", RTEditor.inst.OpenLevelListFolder, "Level List Open Explorer"),
                            new SpacerElement(),
                            new ButtonElement($"Select Icon ({RTFileBrowser.SYSTEM_BROWSER})", () =>
                            {
                                string imageFile = FileBrowser.OpenSingleFile("Select an image!", RTEditor.inst.BasePath, new string[] { "png" });
                                if (string.IsNullOrEmpty(imageFile))
                                    return;

                                RTFile.CopyFile(imageFile, RTFile.CombinePaths(path, $"folder_icon{FileFormat.PNG.Dot()}"));
                                RenderIcon();
                            }),
                            new ButtonElement($"Select Icon ({RTFileBrowser.EDITOR_BROWSER})", () =>
                            {
                                RTFileBrowser.inst.Popup.Open();
                                RTFileBrowser.inst.UpdateBrowserFile(new string[] { FileFormat.PNG.Dot() }, imageFile =>
                                {
                                    if (string.IsNullOrEmpty(imageFile))
                                        return;

                                    RTFileBrowser.inst.Popup.Close();

                                    RTFile.CopyFile(imageFile, RTFile.CombinePaths(path, $"folder_icon{FileFormat.PNG.Dot()}"));
                                    RenderIcon();
                                });
                            }),
                            new ButtonElement("Clear Icon", () => RTEditor.inst.ShowWarningPopup("Are you sure you want to clear the folder icon? This will delete the icon file.", () =>
                            {
                                RTFile.DeleteFile(RTFile.CombinePaths(path, $"folder_icon{FileFormat.PNG.Dot()}"));
                                RenderIcon();
                                EditorManager.inst.DisplayNotification("Deleted icon!", 1.5f, EditorManager.NotificationType.Success);
                            })),
                            new SpacerElement(),
                            new ButtonElement("Create Info File", () =>
                            {
                                var filePath = RTFile.CombinePaths(path, $"folder_info{FileFormat.JSON.Dot()}");
                                if (RTFile.FileExists(filePath))
                                {
                                    EditorManager.inst.DisplayNotification($"Info file already exists!", 2f, EditorManager.NotificationType.Warning);
                                    return;
                                }

                                RTTextEditor.inst.SetEditor("This is the default description.", val => { }, "Create", () =>
                                {
                                    var jn = Parser.NewJSONObject();
                                    jn["desc"] = RTTextEditor.inst.Text;
                                    infoJN = jn;
                                    RTFile.WriteToFile(filePath, jn.ToString());
                                    RenderTooltip();
                                    RTTextEditor.inst.Popup.Close();

                                    EditorManager.inst.DisplayNotification("Created info file!", 1.5f, EditorManager.NotificationType.Success);
                                });
                            }),
                            new ButtonElement("Edit Info File", () =>
                            {
                                var filePath = RTFile.CombinePaths(path, $"folder_info{FileFormat.JSON.Dot()}");

                                if (!RTFile.FileExists(filePath))
                                    return;

                                RTTextEditor.inst.SetEditor("This is the default description.", val => { }, "Done", () =>
                                {
                                    var jn = JSON.Parse("{}");
                                    jn["desc"] = RTTextEditor.inst.Text;
                                    infoJN = jn;
                                    RTFile.WriteToFile(filePath, jn.ToString());
                                    RenderTooltip();
                                    RTTextEditor.inst.Popup.Close();
                                });
                            }),
                            new ButtonElement("Update Info", () =>
                            {
                                infoJN = null;
                                RenderTooltip();
                                RenderIcon();
                            }),
                            new ButtonElement("Clear Info File", () => RTEditor.inst.ShowWarningPopup("Are you sure you want to delete the info file?", () =>
                            {
                                RTFile.DeleteFile(RTFile.CombinePaths(path, $"folder_info{FileFormat.JSON.Dot()}"));
                                infoJN = null;
                                RenderTooltip();
                                EditorManager.inst.DisplayNotification("Deleted info file!", 1.5f, EditorManager.NotificationType.Success);
                            })),
                            new SpacerElement(),
                            new ButtonElement("Create Collection", () =>
                            {
                                EditorManager.inst.DisplayNotification($"todo", 2f, EditorManager.NotificationType.Error);
                            }));

                        return;
                    }

                    EditorLevelManager.inst.OpenLevelPopup.PathField.text = path.Remove(RTEditor.inst.BeatmapsPath + "/");
                    RTEditor.inst.UpdateEditorPath(false);
                };
                return;
            }

            Button.onClick = eventData =>
            {
                if (LevelTemplateEditor.inst.choosingLevelTemplate && Item)
                {
                    LevelTemplateEditor.inst.CreateTemplate(Item.path);

                    return;
                }

                var selectedLevels = EditorLevelManager.inst.SelectedLevels;
                var currentLevelCollection = EditorLevelManager.inst.CurrentLevelCollection ?? EditorLevelManager.inst.OpenLevelCollection;

                var info = GetLevelInfo();

                if (eventData.button == PointerEventData.InputButton.Right)
                {
                    var list = new List<EditorElement>();

                    if (!selectedLevels.IsEmpty())
                    {
                        list = new List<EditorElement>()
                        {
                            new ButtonElement("Combine", () =>
                            {
                                RTEditor.inst.ShowNameEditor("Combiner", "Combined Level name", "Combine", () =>
                                {
                                    EditorLevelManager.inst.Combine(RTEditor.inst.folderCreatorName.text, EditorLevelManager.inst.SelectedLevels, EditorLevelManager.inst.LoadLevels);
                                });
                            }),
                            new SpacerElement(),
                            new ButtonElement("Create Collection", () =>
                            {
                                EditorManager.inst.DisplayNotification($"Not implemented yet.", 2f, EditorManager.NotificationType.Warning);
                            }),
                            new ButtonElement("Add to Collection", () =>
                            {
                                EditorManager.inst.DisplayNotification($"Not implemented yet.", 2f, EditorManager.NotificationType.Warning);
                            }),
                        };

                        if (currentLevelCollection && info)
                            list.Add(new SpacerElement());
                    }
                    else if (Item)
                    {
                        list = new List<EditorElement>()
                        {
                            new ButtonElement("Open", () =>
                            {
                                EditorLevelManager.inst.LoadLevel(this);
                                EditorLevelManager.inst.OpenLevelPopup.Close();
                            }, "Level Panel Open"),
                            new ButtonElement("Show Autosaves", () => EditorLevelManager.inst.OpenAutosaveList(this), "Level Panel Show Autosaves"),
                            new ButtonElement("Convert to VG", () => EditorLevelManager.inst.ConvertLevel(this), "Convert Level VG"),
                            new SpacerElement(),
                            new ButtonElement("Create folder", () => RTEditor.inst.ShowFolderCreator(RTFile.CombinePaths(RTEditor.inst.BeatmapsPath, RTEditor.inst.EditorPath), EndFolderCreation), "Level Panel Create Folder"),
                            new ButtonElement("Create template", () => LevelTemplateEditor.inst.CreateTemplate(Item.path), "Level Panel Create Template"),
                            new ButtonElement("Create level", EditorManager.inst.OpenNewLevelPopup, "Level Panel Create Level"),
                            new ButtonElement("Create backup", () => EditorLevelManager.inst.SaveBackup(this), "Level Panel Create Backup"),
                            new SpacerElement(),
                            new ButtonElement("Rename", () => RTEditor.inst.ShowNameEditor("Level Renamer", "Level name", "Rename", () =>
                            {
                                var currentPath = Item.path;

                                var destination = RTFile.ReplaceSlash(Item.path.Replace(Item.FolderName, RTFile.ValidateDirectory(RTEditor.inst.folderCreatorName.text)));
                                RTFile.MoveDirectory(Item.path, destination);
                                Item.metadata.beatmap.name = RTEditor.inst.folderCreatorName.text;
                                RTFile.WriteToFile(RTFile.CombinePaths(destination, Level.METADATA_LSB), Item.metadata.ToJSON().ToString());

                                if (currentPath == EditorLevelManager.inst.CurrentLevel.path)
                                {
                                    RTEditor.inst.SetCurrentPath(destination);
                                    // set new path in case the current level is a separate instance.
                                    EditorLevelManager.inst.CurrentLevel.path = destination;
                                }

                                Item.path = destination;

                                EditorLevelManager.inst.LoadLevels();
                                RTEditor.inst.HideNameEditor();
                            }), "Level Panel Rename Level"),
                            new ButtonElement("Cut", () =>
                            {
                                EditorLevelManager.inst.shouldCutLevel = true;
                                EditorLevelManager.inst.copiedLevelPath = Item.path;
                                EditorManager.inst.DisplayNotification($"Cut {Item.FolderName}!", 1.5f, EditorManager.NotificationType.Success);
                                CoreHelper.Log($"Cut level: {EditorLevelManager.inst.copiedLevelPath}");
                            }, "Level Panel Cut"),
                            new ButtonElement("Copy", () =>
                            {
                                EditorLevelManager.inst.shouldCutLevel = false;
                                EditorLevelManager.inst.copiedLevelPath = Item.path;
                                EditorManager.inst.DisplayNotification($"Copied {Item.FolderName}!", 1.5f, EditorManager.NotificationType.Success);
                                CoreHelper.Log($"Copied level: {EditorLevelManager.inst.copiedLevelPath}");
                            }, "Level Panel Copy"),
                            new ButtonElement("Paste", EditorLevelManager.inst.PasteLevel, "Level Panel Paste"),
                            new ButtonElement("Delete", () => Delete()),
                            new SpacerElement(),
                            new ButtonElement("Copy Arcade ID", () =>
                            {
                                var metadata = Item.metadata;
                                if (string.IsNullOrEmpty(metadata.arcadeID) || metadata.arcadeID == "0")
                                {
                                    EditorManager.inst.DisplayNotification($"Level does not have an ID assigned to it yet. Open the level, save it and try again.", 3.3f, EditorManager.NotificationType.Warning);
                                    return;
                                }

                                LSText.CopyToClipboard(metadata.arcadeID);
                                EditorManager.inst.DisplayNotification($"Copied Arcade ID ({metadata.arcadeID}) to your clipboard.", 2f, EditorManager.NotificationType.Success);
                            }, "Copy Arcade ID"),
                            new ButtonElement("Copy Server ID", () =>
                            {
                                var metadata = Item.metadata;
                                if (string.IsNullOrEmpty(metadata.serverID) || metadata.serverID == "0")
                                {
                                    EditorManager.inst.DisplayNotification($"Your level needs to be uploaded to the arcade server before you can copy the server ID.", 3.5f, EditorManager.NotificationType.Warning);
                                    return;
                                }

                                LSText.CopyToClipboard(metadata.serverID);
                                EditorManager.inst.DisplayNotification($"Copied Server ID ({metadata.serverID}) to your clipboard.", 2f, EditorManager.NotificationType.Success);
                            }, "Copy Server ID"),
                            new SpacerElement(),
                            new ButtonElement("ZIP Level", () => EditorLevelManager.inst.ZipLevel(this), "Level Panel ZIP"),
                            new ButtonElement("Copy Path", () => LSText.CopyToClipboard(Item.path), "Level Panel Copy Folder"),
                            new ButtonElement("Copy Relative Path", () => LSText.CopyToClipboard(Item.path.Remove(RTEditor.inst.BeatmapsPath + "/")), "Level Panel Copy Folder"),
                            new ButtonElement("Open in File Explorer", () => RTFile.OpenInFileBrowser.Open(Item.path), "Level Panel Open Explorer"),
                            new ButtonElement("Open List in File Explorer", RTEditor.inst.OpenLevelListFolder, "Level List Open Explorer"),
                            new SpacerElement(),
                            new ButtonElement("Add File to Collection", () =>
                            {
                                EditorLevelManager.inst.LevelCollectionPopup.Open();
                                EditorLevelManager.inst.RenderLevelCollections();
                                EditorLevelManager.inst.onLevelCollectionSelected = (levelCollectionPanel, pointerEventData) =>
                                {
                                    if (!levelCollectionPanel || !levelCollectionPanel.Item)
                                        return;

                                    levelCollectionPanel.Item.AddLevelToFolder(Item);
                                    levelCollectionPanel.Item.Save();
                                    EditorLevelManager.inst.LoadLevels();
                                    EditorManager.inst.DisplayNotification($"Added the level to the level collection [ {levelCollectionPanel.Item.name} ]", 3f, EditorManager.NotificationType.Success);
                                };
                            }),
                            new ButtonElement("Add Ref to Collection", () =>
                            {
                                EditorLevelManager.inst.LevelCollectionPopup.Open();
                                EditorLevelManager.inst.RenderLevelCollections();
                                EditorLevelManager.inst.onLevelCollectionSelected = (levelCollectionPanel, pointerEventData) =>
                                {
                                    if (!levelCollectionPanel || !levelCollectionPanel.Item)
                                        return;

                                    levelCollectionPanel.Item.AddLevel(Item);
                                    levelCollectionPanel.Item.Save();
                                    EditorLevelManager.inst.LoadLevels();
                                    EditorManager.inst.DisplayNotification($"Added the level to the level collection [ {levelCollectionPanel.Item.name} ]", 3f, EditorManager.NotificationType.Success);
                                };
                            }),
                        };

                        if (currentLevelCollection && info)
                            list.Add(new SpacerElement());
                    }

                    if (currentLevelCollection && info)
                    {
                        list.Add(new ButtonElement("Edit Info", () => EditorLevelManager.inst.OpenLevelInfoEditor(info, Item ? () =>
                        {
                            EditorLevelManager.inst.LoadLevel(this);
                            EditorLevelManager.inst.OpenLevelPopup.Close();
                        } : null, Item ? "Open" : null)));
                        list.Add(new ButtonElement("Remove from Collection", () =>
                        {
                            RTEditor.inst.ShowWarningPopup("Are you sure you want to remove the level from the current collection?", () =>
                            {
                                if (Item)
                                    currentLevelCollection.RemoveLevelFromFolder(Item);
                                else
                                    currentLevelCollection.Remove(info);
                                currentLevelCollection.Save();

                                EditorLevelManager.inst.LoadLevels();
                            });
                        }));
                        list.Add(new ButtonElement("Move to Start", () =>
                        {
                            var info = GetLevelInfo();
                            if (!info)
                                return;

                            if (info.index == 0)
                            {
                                EditorManager.inst.DisplayNotification($"The level is already at the start!", 2f, EditorManager.NotificationType.Warning);
                                return;
                            }

                            currentLevelCollection.Move(info.id, 0);
                            currentLevelCollection.Save();
                            EditorLevelManager.inst.LoadLevels();
                        }));
                        list.Add(new ButtonElement("Move Earlier", () =>
                        {
                            var info = GetLevelInfo();
                            if (!info)
                                return;

                            if (info.index - 1 < 0)
                            {
                                EditorManager.inst.DisplayNotification($"Cannot move the level earlier than the start!", 2f, EditorManager.NotificationType.Warning);
                                return;
                            }

                            currentLevelCollection.Move(info.id, info.index - 1);
                            currentLevelCollection.Save();
                            EditorLevelManager.inst.LoadLevels();
                        }));
                        list.Add(new ButtonElement("Move Later", () =>
                        {
                            var info = GetLevelInfo();
                            if (!info)
                                return;

                            var index = info.index;
                            if (info.index + 1 >= currentLevelCollection.levelInformation.Count)
                            {
                                EditorManager.inst.DisplayNotification($"Cannot move the level later than the end!", 2f, EditorManager.NotificationType.Warning);
                                return;
                            }

                            currentLevelCollection.Move(info.id, info.index + 1);
                            currentLevelCollection.Save();
                            EditorLevelManager.inst.LoadLevels();
                        }));
                        list.Add(new ButtonElement("Move to End", () =>
                        {
                            var info = GetLevelInfo();
                            if (!info)
                                return;

                            if (info.index == currentLevelCollection.Count - 1)
                            {
                                EditorManager.inst.DisplayNotification($"The level is already at the end!", 2f, EditorManager.NotificationType.Warning);
                                return;
                            }

                            currentLevelCollection.Move(info.id, currentLevelCollection.Count - 1);
                            currentLevelCollection.Save();
                            EditorLevelManager.inst.LoadLevels();
                        }));
                    }

                    EditorContextMenu.inst.ShowContextMenu(list);

                    return;
                }

                if (InputDataManager.inst.editorActions.MultiSelect.IsPressed)
                {
                    Selected = !Selected;
                    return;
                }

                if (!selectedLevels.IsEmpty())
                {
                    Selected = false;
                    return;
                }

                if (currentLevelCollection && info)
                {
                    if (EditorConfig.Instance.LoadCollectionLevelDirectly.Value && Item)
                    {
                        EditorLevelManager.inst.LoadLevel(this);
                        EditorLevelManager.inst.OpenLevelPopup.Close();
                        return;
                    }
                    EditorLevelManager.inst.OpenLevelInfoEditor(info, Item ? () =>
                    {
                        EditorLevelManager.inst.LoadLevel(this);
                        EditorLevelManager.inst.OpenLevelPopup.Close();
                    } : null, Item ? "Open" : null);
                    return;
                }

                if (!Item)
                {
                    EditorManager.inst.DisplayNotification($"No level was found.", 2f, EditorManager.NotificationType.Error);
                    return;
                }

                EditorLevelManager.inst.LoadLevel(this);
                EditorLevelManager.inst.OpenLevelPopup.Close();
            };
        }

        /// <summary>
        /// Updates the level panels' deletion function
        /// </summary>
        public void UpdateDeleteFunction()
        {
            if (!DeleteButton)
                return;

            var active = GetLevelInfo() || EditorConfig.Instance.OpenLevelShowDeleteButton.Value;

            DeleteButton.gameObject.SetActive(active);

            if (!active)
                return;

            DeleteButton.OnClick.NewListener(() => Delete(true));
        }

        public void Delete(bool recycle = false)
        {
            var info = GetLevelInfo();
            if (info)
            {
                RTEditor.inst.ShowWarningPopup("Are you sure you want to remove this level from the current collection? This cannot be undone!", () =>
                {
                    var currentLevelCollection = EditorLevelManager.inst.CurrentLevelCollection ?? EditorLevelManager.inst.OpenLevelCollection;
                    currentLevelCollection.Remove(info);
                    currentLevelCollection.Save();
                    EditorLevelManager.inst.LoadLevels();
                });
                return;
            }

            if (!Item)
                return;

            RTEditor.inst.ShowWarningPopup("Are you sure you want to delete this level? (It will be moved to a recycling folder)", () =>
            {
                if (recycle)
                    EditorLevelManager.inst.RecycleLevel(this);
                else
                {
                    RTFile.DeleteDirectory(Item.path);
                    EditorLevelManager.inst.LoadLevels();
                }
                EditorManager.inst.DisplayNotification("Deleted level!", 2f, EditorManager.NotificationType.Success);
            });
        }

        /// <summary>
        /// Sets the icon for the level panel and the level itself.
        /// </summary>
        /// <param name="icon">Icon to set.</param>
        public void SetIcon(Sprite icon)
        {
            if (Item)
                Item.icon = icon;
            RenderIcon(icon);
        }

        /// <summary>
        /// Renders the selected state of the level panel.
        /// </summary>
        /// <param name="selected">If the level panel is selected.</param>
        public void RenderSelected(bool selected)
        {
            if (SelectedUI)
                SelectedUI.SetActive(selected);
        }

        /// <summary>
        /// Loads the levels' icon.
        /// </summary>
        /// <param name="file">Image file to load.</param>
        /// <param name="onLoad">Action to run when the image is loaded.</param>
        /// <returns>Returns a generated coroutine.</returns>
        public Coroutine LoadImageCoroutine(string file, Action<LevelPanel> onLoad = null) => CoroutineHelper.StartCoroutine(AlephNetwork.DownloadImageTexture($"file://{RTFile.CombinePaths(Path, file)}", cover =>
        {
            if (!cover)
            {
                SetDefaultIcon();
                onLoad?.Invoke(this);
                return;
            }

            SetIcon(SpriteHelper.CreateSprite(cover));
            onLoad?.Invoke(this);
        }, (errorMsg, handlerText) =>
        {
            SetDefaultIcon();
            onLoad?.Invoke(this);
        }));

        /// <summary>
        /// Sets the default icon.
        /// </summary>
        public void SetDefaultIcon() => SetIcon(isFolder ? EditorSprites.OpenSprite : LegacyPlugin.AtanPlaceholder);

        /// <summary>
        /// Loads the level's editor info.
        /// </summary>
        public void LoadEditorInfo()
        {
            try
            {
                if (RTFile.FileExists(RTFile.CombinePaths(Path, Level.EDITOR_LSE)))
                    EditorInfo = RTFile.CreateFromFile<EditorInfo>(RTFile.CombinePaths(Path, Level.EDITOR_LSE));
            }
            catch
            {

            }
        }

        void EndFolderCreation()
        {
            EditorLevelManager.inst.LoadLevels();
            RTEditor.inst.HideNameEditor();
        }

        public LevelInfo GetLevelInfo() => Item?.collectionInfo ?? Info;

        public override string ToString() => isFolder ? Name : Item?.ToString();

        #endregion
    }
}
