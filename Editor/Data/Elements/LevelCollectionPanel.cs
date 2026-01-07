using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

using LSFunctions;

using Crosstales.FB;
using SimpleJSON;

using BetterLegacy.Configs;
using BetterLegacy.Core;
using BetterLegacy.Core.Components;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Data.Level;
using BetterLegacy.Core.Managers;
using BetterLegacy.Core.Prefabs;
using BetterLegacy.Editor.Components;
using BetterLegacy.Editor.Managers;

namespace BetterLegacy.Editor.Data.Elements
{
    public class LevelCollectionPanel : EditorPanel<LevelCollection>
    {
        #region Values

        #region UI

        /// <summary>
        /// The icon of the level panel.
        /// </summary>
        public Image IconImage { get; set; }

        /// <summary>
        /// The UI that displays that the level is selected.
        /// </summary>
        public GameObject SelectedUI { get; set; }

        #endregion

        #region Data

        public override string DisplayName => isFolder ? Name : string.Format(labelFormat,
            LSText.ClampString(Item.name, labelFolderNameMax),
            LSText.ClampString(Item.creator, labelCreatorNameMax),
            Item.Difficulty.DisplayName,
            LSText.ClampString(Item.description, labelDescriptionMax),
            LSText.ClampString(Item.dateEdited, labelDateMax),
            LSText.ClampString(Item.dateCreated, labelDateMax),
            LSText.ClampString(Item.datePublished, labelDateMax));

        public override float FocusSize => EditorConfig.Instance.OpenLevelButtonHoverSize.Value;

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

        #endregion

        #region Asset Pack

        public static RectValues baseRect = new RectValues(Vector2.zero, new Vector2(0f, 1f), new Vector2(0f, 1f), RectValues.CenterPivot, new Vector2(584f, 32f));

        public static RectValues labelRect = RectValues.FullAnchored.AnchoredPosition(32f, 0f).SizeDelta(-12f, -8f);

        public static RectValues iconRect = RectValues.Default.AnchoredPosition(-276f, 0f).SizeDelta(26f, 26f);

        public static RectValues deleteRect = new RectValues(Vector2.zero, Vector2.one, new Vector2(1f, 0f), new Vector2(1f, 0.5f), new Vector2(32f, 0f));

        public static string labelFormat = "/{0}";

        public static TextAnchor labelAlignment = TextAnchor.MiddleLeft;

        public static HorizontalWrapMode labelHorizontalWrap = HorizontalWrapMode.Wrap;

        public static VerticalWrapMode labelVerticalWrap = VerticalWrapMode.Truncate;

        public static int labelFontSize = 20;

        public static int labelFolderNameMax = 40;

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

            gameObject = EditorManager.inst.folderButtonPrefab.Duplicate(EditorLevelManager.inst.LevelCollectionPopup.Content, $"Folder [{Name}]");
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

        public override void Init(LevelCollection levelCollection)
        {
            Item = levelCollection;
            Path = levelCollection.path;
            levelCollection.editorPanel = this;
            levelCollection.isEditor = true;

            var gameObject = GameObject;
            if (gameObject)
                CoreHelper.Destroy(gameObject);

            gameObject = EditorManager.inst.folderButtonPrefab.Duplicate(EditorLevelManager.inst.LevelCollectionPopup.Content, $"Folder [{Name}]");
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

            SelectedUI = Creator.NewUIObject("selected", gameObject.transform);
            SelectedUI.SetActive(false);
            var selectedImage = SelectedUI.AddComponent<Image>();
            selectedImage.color = LSColors.HexToColorAlpha("0088FF25");

            RectValues.FullAnchored.AssignToRectTransform(selectedImage.rectTransform);

            Render();
        }

        public override void Render()
        {
            RenderIcon();
            RenderLabel();
            RenderHover();
            RenderTooltip();
            UpdateFunction();
        }

        /// <summary>
        /// Renders the level collection panel icon.
        /// </summary>
        public void RenderIcon()
        {
            if (isFolder)
                return;

            RenderIcon(Item?.icon);
        }

        /// <summary>
        /// Renders the level collection panel icon.
        /// </summary>
        /// <param name="icon">Icon of the level collection panel.</param>
        public void RenderIcon(Sprite icon)
        {
            if (IconImage)
                IconImage.sprite = icon;
        }

        public override void RenderLabel(string text)
        {
            Label.text = text;

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

            TooltipHelper.AddHoverTooltip(GameObject, "<#" + LSColors.ColorToHex(Item.Difficulty.Color) + ">" + Item.name,
                $"</color><br>Folder: {Item.FolderName}<br>Description: {Item.description}");
        }

        /// <summary>
        /// Updates the level collection panels' main function.
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
                                EditorLevelManager.inst.LevelCollectionPopup.PathField.text = path.Remove(RTEditor.inst.BeatmapsPath + "/");
                                RTEditor.inst.UpdateEditorPath(false);
                            }, "Level Panel Open Folder"),
                            new ButtonElement("Create folder", () => RTEditor.inst.ShowFolderCreator(RTFile.CombinePaths(RTEditor.inst.BeatmapsPath, RTEditor.inst.CollectionsPath), EndFolderCreation), "Level Panel Create Folder"),
                            //new ButtonElement("Create level", EditorManager.inst.OpenNewLevelPopup),
                            new SpacerElement(),
                            new ButtonElement("Rename", () => RTEditor.inst.ShowNameEditor("Folder Renamer", "Folder name", "Rename", () =>
                            {
                                RTFile.MoveDirectory(path, path.Replace(Name, RTFile.ValidateDirectory(RTEditor.inst.folderCreatorName.text)).Replace("\\", "/"));

                                EditorLevelManager.inst.LoadLevelCollections();
                                RTEditor.inst.HideNameEditor();
                            }), "Level Panel Rename Folder"),
                            new ButtonElement("Paste", EditorLevelManager.inst.PasteLevel, "Level Panel Paste"),
                            new ButtonElement("Delete", () => RTEditor.inst.ShowWarningPopup("Are you <b>100%</b> sure you want to delete this folder? This <b>CANNOT</b> be undone! Always make sure you have backups.", () =>
                            {
                                RTFile.DeleteDirectory(path);
                                EditorLevelManager.inst.LoadLevelCollections();
                                EditorManager.inst.DisplayNotification("Deleted folder!", 2f, EditorManager.NotificationType.Success);
                            }), "Level Panel Delete"),
                            new SpacerElement(),
                            //new ButtonElement("ZIP Folder", () => EditorLevelManager.inst.ZipLevel(this), "Level Panel ZIP"),
                            new ButtonElement("Copy Path", () => LSText.CopyToClipboard(path), "Level Panel Copy Folder"),
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

                    EditorLevelManager.inst.LevelCollectionPopup.PathField.text = path.Remove(RTEditor.inst.BeatmapsPath + "/");
                    EditorLevelManager.inst.LoadLevelCollections();
                };
                return;
            }

            Button.onClick = eventData =>
            {
                if (EditorLevelManager.inst.onLevelCollectionSelected != null)
                {
                    EditorLevelManager.inst.onLevelCollectionSelected.Invoke(this, eventData);
                    EditorLevelManager.inst.onLevelCollectionSelected = null;
                    return;
                }

                //if (LevelTemplateEditor.inst.choosingLevelTemplate)
                //{
                //    LevelTemplateEditor.inst.CreateTemplate(Item.path);

                //    return;
                //}

                var selectedLevels = EditorLevelManager.inst.SelectedLevelCollections;

                if (eventData.button == PointerEventData.InputButton.Right)
                {
                    var list = new List<EditorElement>();

                    if (selectedLevels.IsEmpty())
                    {
                        list = new List<EditorElement>()
                        {
                            new ButtonElement("View Levels", () =>
                            {
                                EditorLevelManager.inst.LoadLevelCollection(this);
                            }, "Level Panel Open"),
                            new ButtonElement("Edit", () =>
                            {
                                EditorLevelManager.inst.OpenLevelCollectionEditor(Item);
                            }),
                            new ButtonElement("Copy to Arcade", () =>
                            {
                                RTFile.CopyDirectory(Path, RTFile.CombinePaths(RTFile.ApplicationDirectory, LevelManager.ListPath, System.IO.Path.GetFileName(Path)));
                                EditorManager.inst.DisplayNotification($"Successfully copied the level collection to the Arcade!", 2f, EditorManager.NotificationType.Success);
                            }),
                            //new ButtonElement("Show Autosaves", () =>
                            //{
                            //    RTEditor.inst.AutosavePopup.Open();
                            //    EditorLevelManager.inst.RefreshAutosaveList(this);
                            //}, "Level Panel Show Autosaves"),
                            //new ButtonElement("Convert to VG", () => EditorLevelManager.inst.ConvertLevel(this), "Convert Level VG"),
                            new SpacerElement(),
                            new ButtonElement("Create folder", () => RTEditor.inst.ShowFolderCreator(RTFile.CombinePaths(RTEditor.inst.BeatmapsPath, RTEditor.inst.CollectionsPath), EndFolderCreation), "Level Panel Create Folder"),
                            //new ButtonElement("Create template", () => LevelTemplateEditor.inst.CreateTemplate(Item.path), "Level Panel Create Template"),
                            //new ButtonElement("Create level", EditorManager.inst.OpenNewLevelPopup, "Level Panel Create Level"),
                            //new ButtonElement("Create backup", () => EditorLevelManager.inst.SaveBackup(this), "Level Panel Create Backup"),
                            new SpacerElement(),
                            new ButtonElement("Rename", () => RTEditor.inst.ShowNameEditor("Level Renamer", "Level name", "Rename", () =>
                            {
                                var oldPath = Item.path;
                                var path = Item.path;
                                path = RTFile.GetDirectory(RTFile.RemoveEndSlash(path));
                                path = RTFile.CombinePaths(path, RTFile.ValidateDirectory(RTEditor.inst.folderCreatorName.text));
                                Item.name = RTEditor.inst.folderCreatorName.text;
                                Item.path = path;

                                RenderLabel();
                                RTFile.MoveDirectory(oldPath, path);

                                RTEditor.inst.HideNameEditor();
                            }), "Level Panel Rename Level"),
                            //new ButtonElement("Cut", () =>
                            //{
                            //    EditorLevelManager.inst.shouldCutLevel = true;
                            //    EditorLevelManager.inst.copiedLevelPath = Item.path;
                            //    EditorManager.inst.DisplayNotification($"Cut {Item.FolderName}!", 1.5f, EditorManager.NotificationType.Success);
                            //    CoreHelper.Log($"Cut level: {EditorLevelManager.inst.copiedLevelPath}");
                            //}, "Level Panel Cut"),
                            //new ButtonElement("Copy", () =>
                            //{
                            //    EditorLevelManager.inst.shouldCutLevel = false;
                            //    EditorLevelManager.inst.copiedLevelPath = Item.path;
                            //    EditorManager.inst.DisplayNotification($"Copied {Item.FolderName}!", 1.5f, EditorManager.NotificationType.Success);
                            //    CoreHelper.Log($"Copied level: {EditorLevelManager.inst.copiedLevelPath}");
                            //}, "Level Panel Copy"),
                            //new ButtonElement("Paste", EditorLevelManager.inst.PasteLevel, "Level Panel Paste"),
                            new ButtonElement("Delete", () => RTEditor.inst.ShowWarningPopup("Are you sure you want to delete this level? This CANNOT be undone!", () =>
                            {
                                RTFile.DeleteDirectory(Item.path);
                                EditorLevelManager.inst.LoadLevels();
                                EditorManager.inst.DisplayNotification("Deleted level!", 2f, EditorManager.NotificationType.Success);
                            })),
                            new SpacerElement(),
                            new ButtonElement("Copy Arcade ID", () =>
                            {
                                var id = Item.id;
                                if (string.IsNullOrEmpty(id) || id == "0")
                                {
                                    EditorManager.inst.DisplayNotification($"Level does not have an ID assigned to it yet. Open the level, save it and try again.", 3.3f, EditorManager.NotificationType.Warning);
                                    return;
                                }

                                LSText.CopyToClipboard(id);
                                EditorManager.inst.DisplayNotification($"Copied Arcade ID ({id}) to your clipboard.", 2f, EditorManager.NotificationType.Success);
                            }, "Copy Arcade ID"),
                            new ButtonElement("Copy Server ID", () =>
                            {
                                var serverID = Item.serverID;
                                if (string.IsNullOrEmpty(serverID) || serverID == "0")
                                {
                                    EditorManager.inst.DisplayNotification($"Your level needs to be uploaded to the arcade server before you can copy the server ID.", 3.5f, EditorManager.NotificationType.Warning);
                                    return;
                                }

                                LSText.CopyToClipboard(serverID);
                                EditorManager.inst.DisplayNotification($"Copied Server ID ({serverID}) to your clipboard.", 2f, EditorManager.NotificationType.Success);
                            }, "Copy Server ID"),
                            new SpacerElement(),
                            //new ButtonElement("ZIP Level", () => EditorLevelManager.inst.ZipLevel(this), "Level Panel ZIP"),
                            new ButtonElement("Copy Path", () => LSText.CopyToClipboard(Item.path), "Level Panel Copy Folder"),
                            new ButtonElement("Open in File Explorer", () => RTFile.OpenInFileBrowser.Open(Item.path), "Level Panel Open Explorer"),
                            new ButtonElement("Open List in File Explorer", RTEditor.inst.OpenLevelCollectionListFolder, "Level List Open Explorer"),
                        };
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

                EditorLevelManager.inst.OpenLevelCollectionEditor(Item);
            };
        }

        /// <summary>
        /// Sets the icon for the level collection panel and the level itself.
        /// </summary>
        /// <param name="icon">Icon to set.</param>
        public void SetIcon(Sprite icon)
        {
            if (Item)
                Item.icon = icon;
            RenderIcon(icon);
        }

        /// <summary>
        /// Renders the selected state of the level collection panel.
        /// </summary>
        /// <param name="selected">If the level panel is selected.</param>
        public void RenderSelected(bool selected)
        {
            if (SelectedUI)
                SelectedUI.SetActive(selected);
        }

        /// <summary>
        /// Loads the level collections' icon.
        /// </summary>
        /// <param name="file">Image file to load.</param>
        /// <param name="onLoad">Action to run when the image is loaded.</param>
        /// <returns>Returns a generated coroutine.</returns>
        public Coroutine LoadImageCoroutine(string file, Action<LevelCollectionPanel> onLoad = null) => CoroutineHelper.StartCoroutine(AlephNetwork.DownloadImageTexture($"file://{RTFile.CombinePaths(Path, file)}",
            cover =>
            {
                if (!cover)
                {
                    SetDefaultIcon();
                    onLoad?.Invoke(this);
                    return;
                }

                SetIcon(SpriteHelper.CreateSprite(cover));
                onLoad?.Invoke(this);
            },
            (errorMsg, handlerText) =>
            {
                SetDefaultIcon();
                onLoad?.Invoke(this);
            }));
        
        /// <summary>
        /// Loads the level collections' banner.
        /// </summary>
        /// <param name="file">Image file to load.</param>
        /// <param name="onLoad">Action to run when the image is loaded.</param>
        /// <returns>Returns a generated coroutine.</returns>
        public Coroutine LoadBannerCoroutine(string file, Action<LevelCollectionPanel> onLoad = null) => CoroutineHelper.StartCoroutine(AlephNetwork.DownloadImageTexture($"file://{RTFile.CombinePaths(Path, file)}",
            cover =>
            {
                if (!cover)
                {
                    if (Item)
                        Item.banner = null;
                    onLoad?.Invoke(this);
                    return;
                }
                if (Item)
                    Item.banner = SpriteHelper.CreateSprite(cover);
                onLoad?.Invoke(this);
            },
            (errorMsg, handlerText) =>
            {
                if (Item)
                    Item.banner = null;
                onLoad?.Invoke(this);
            }));

        /// <summary>
        /// Sets the default icon.
        /// </summary>
        public void SetDefaultIcon() => SetIcon(isFolder ? EditorSprites.OpenSprite : LegacyPlugin.AtanPlaceholder);

        void EndFolderCreation()
        {
            EditorLevelManager.inst.LoadLevelCollections();
            RTEditor.inst.HideNameEditor();
        }

        public override string ToString() => isFolder ? Name : Item?.ToString();

        #endregion
    }
}
