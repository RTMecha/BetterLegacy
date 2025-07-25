﻿using System.IO;
using System.Linq;

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
using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Prefabs;
using BetterLegacy.Editor.Components;
using BetterLegacy.Editor.Managers;

namespace BetterLegacy.Editor.Data
{
    /// <summary>
    /// Object for storing prefab panel data.
    /// </summary>
    public class PrefabPanel : Exists
    {
        public PrefabPanel() { }

        public PrefabPanel(int index) => this.index = index;

        public PrefabPanel(PrefabDialog dialog, int index) : this(index) => Dialog = dialog;

        #region Properties

        #region UI

        /// <summary>
        /// The prefab panel game object.
        /// </summary>
        public GameObject GameObject { get; set; }

        /// <summary>
        /// The button for the prefab panel.
        /// </summary>
        public Button Button { get; set; }

        /// <summary>
        /// The delete button for the prefab panel.
        /// </summary>
        public Button DeleteButton { get; set; }

        /// <summary>
        /// The name text of the prefab panel.
        /// </summary>
        public Text Name { get; set; }

        /// <summary>
        /// The type text of the prefab panel.
        /// </summary>
        public Text TypeText { get; set; }

        /// <summary>
        /// The base image for the prefab type of the prefab panel.
        /// </summary>
        public Image TypeImage { get; set; }

        /// <summary>
        /// The icon for the prefab type of the prefab panel.
        /// </summary>
        public Image TypeIcon { get; set; }

        public HoverUI HoverUI { get; set; }

        #endregion

        #region Data

        /// <summary>
        /// What dialog this prefab panel is located in.
        /// </summary>
        public PrefabDialog Dialog { get; set; }

        /// <summary>
        /// If the prefab panel is external.
        /// </summary>
        public bool IsExternal => Dialog == PrefabDialog.External;

        /// <summary>
        /// The prefab reference.
        /// </summary>
        public Prefab Prefab { get; set; }

        /// <summary>
        /// The file path to the prefab, if it is external.
        /// </summary>
        public string FilePath { get; set; }

        /// <summary>
        /// The icon of the prefab panel.
        /// </summary>
        public Image IconImage { get; set; }

        #endregion

        #endregion

        #region Fields

        /// <summary>
        /// If the prefab panel is a folder button instead.
        /// </summary>
        public bool isFolder;

        /// <summary>
        /// Index of the prefab panel.
        /// </summary>
        public int index;

        JSONNode infoJN;

        #endregion

        #region Methods

        /// <summary>
        /// Initializes the prefab panel as a folder.
        /// </summary>
        /// <param name="directory">Directory to set to the theme panel.</param>
        public void Init(string directory)
        {
            var path = RTFile.ReplaceSlash(directory);
            var fileName = Path.GetFileName(directory);

            var gameObjectFolder = EditorManager.inst.spriteFolderButtonPrefab.Duplicate(PrefabEditor.inst.externalContent, $"Folder [{fileName}]");
            var folderButtonStorageFolder = gameObjectFolder.GetComponent<SpriteFunctionButtonStorage>();
            var folderButtonFunctionFolder = gameObjectFolder.AddComponent<FolderButtonFunction>();

            var hover = gameObjectFolder.AddComponent<HoverUI>();
            hover.animatePos = false;
            hover.animateSca = true;

            folderButtonStorageFolder.button.onClick.ClearAll();
            folderButtonFunctionFolder.onClick = eventData =>
            {
                if (!path.Contains(RTEditor.inst.BeatmapsPath + "/"))
                {
                    EditorManager.inst.DisplayNotification($"Path does not contain the proper directory.", 2f, EditorManager.NotificationType.Warning);
                    return;
                }

                if (eventData.button == PointerEventData.InputButton.Right)
                {
                    EditorContextMenu.inst.ShowContextMenu(
                        new ButtonFunction("Open folder", () =>
                        {
                            RTEditor.inst.prefabPathField.text = path.Replace(RTEditor.inst.BeatmapsPath + "/", "");
                            RTEditor.inst.UpdatePrefabPath(false);
                        }),
                        new ButtonFunction("Create folder", () => RTEditor.inst.ShowFolderCreator(RTFile.CombinePaths(RTEditor.inst.BeatmapsPath, RTEditor.inst.PrefabPath), () => { RTEditor.inst.UpdatePrefabPath(true); RTEditor.inst.HideNameEditor(); })),
                        new ButtonFunction(true),
                        new ButtonFunction("Paste Prefab", RTPrefabEditor.inst.PastePrefab),
                        new ButtonFunction("Delete", () => RTEditor.inst.ShowWarningPopup("Are you <b>100%</b> sure you want to delete this folder? This <b>CANNOT</b> be undone! Always make sure you have backups.", () =>
                        {
                            RTFile.DeleteDirectory(path);
                            RTEditor.inst.UpdatePrefabPath(true);
                            EditorManager.inst.DisplayNotification("Deleted folder!", 2f, EditorManager.NotificationType.Success);
                            RTEditor.inst.HideWarningPopup();
                        }, RTEditor.inst.HideWarningPopup)),
                        new ButtonFunction(true),
                        new ButtonFunction($"Select Icon ({RTEditor.SYSTEM_BROWSER})", () =>
                        {
                            string imageFile = FileBrowser.OpenSingleFile("Select an image!", RTEditor.inst.BasePath, new string[] { "png" });
                            if (string.IsNullOrEmpty(imageFile))
                                return;

                            RTFile.CopyFile(imageFile, RTFile.CombinePaths(path, $"folder_icon{FileFormat.PNG.Dot()}"));
                            RenderIcon();
                        }),
                        new ButtonFunction($"Select Icon ({RTEditor.EDITOR_BROWSER})", () =>
                        {
                            RTEditor.inst.BrowserPopup.Open();
                            RTFileBrowser.inst.UpdateBrowserFile(new string[] { FileFormat.PNG.Dot() }, imageFile =>
                            {
                                if (string.IsNullOrEmpty(imageFile))
                                    return;

                                RTEditor.inst.BrowserPopup.Close();

                                RTFile.CopyFile(imageFile, RTFile.CombinePaths(path, $"folder_icon{FileFormat.PNG.Dot()}"));
                                RenderIcon();
                            });
                        }),
                        new ButtonFunction("Clear Icon", () => RTEditor.inst.ShowWarningPopup("Are you sure you want to clear the folder icon? This will delete the icon file.", () =>
                        {
                            RTEditor.inst.HideWarningPopup();
                            RTFile.DeleteFile(RTFile.CombinePaths(path, $"folder_icon{FileFormat.PNG.Dot()}"));
                            RenderIcon();
                            EditorManager.inst.DisplayNotification("Deleted icon!", 1.5f, EditorManager.NotificationType.Success);
                        }, RTEditor.inst.HideWarningPopup)),
                        new ButtonFunction(true),
                        new ButtonFunction("Create Info File", () =>
                        {
                            var filePath = RTFile.CombinePaths(path, $"folder_info{FileFormat.JSON.Dot()}");
                            if (RTFile.FileExists(filePath))
                            {
                                EditorManager.inst.DisplayNotification($"Info file already exists!", 2f, EditorManager.NotificationType.Warning);
                                return;
                            }

                            RTTextEditor.inst.SetEditor("This is the default description.", val => { }, "Create", () =>
                            {
                                var jn = JSON.Parse("{}");
                                jn["desc"] = RTTextEditor.inst.Text;
                                infoJN = jn;
                                RTFile.WriteToFile(filePath, jn.ToString());
                                RenderTooltip();
                                RTTextEditor.inst.Popup.Close();

                                EditorManager.inst.DisplayNotification("Created info file!", 1.5f, EditorManager.NotificationType.Success);
                            });
                        }),
                        new ButtonFunction("Edit Info File", () =>
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
                        new ButtonFunction("Update Info", () =>
                        {
                            infoJN = null;
                            RenderTooltip();
                            RenderIcon();
                        }),
                        new ButtonFunction("Clear Info File", () => RTEditor.inst.ShowWarningPopup("Are you sure you want to delete the info file?", () =>
                        {
                            RTFile.DeleteFile(RTFile.CombinePaths(path, $"folder_info{FileFormat.JSON.Dot()}"));
                            infoJN = null;
                            RenderTooltip();
                            RTEditor.inst.HideWarningPopup();
                            EditorManager.inst.DisplayNotification("Deleted info file!", 1.5f, EditorManager.NotificationType.Success);
                        }, RTEditor.inst.HideWarningPopup))
                        );

                    return;
                }

                RTEditor.inst.prefabPathField.text = path.Replace(RTEditor.inst.BeatmapsPath + "/", "");
                RTEditor.inst.UpdatePrefabPath(false);
            };

            EditorThemeManager.ApplySelectable(folderButtonStorageFolder.button, ThemeGroup.List_Button_1);
            EditorThemeManager.ApplyLightText(folderButtonStorageFolder.label);

            GameObject = gameObjectFolder;
            Name = folderButtonStorageFolder.label;
            HoverUI = hover;
            FilePath = directory;
            Dialog = PrefabDialog.External;
            isFolder = true;
            IconImage = folderButtonStorageFolder.image;

            Render();
        }

        /// <summary>
        /// Initializes the prefab panel as an actual prefab.
        /// </summary>
        /// <param name="updateCurrentPrefab">If the current quick prefab should be set instead of importing.</param>
        public void Init(Prefab prefab, bool updateCurrentPrefab = false)
        {
            var gameObject = GameObject;
            if (gameObject)
                CoreHelper.Destroy(gameObject);

            Prefab = prefab;
            var dialog = Dialog;

            gameObject = PrefabEditor.inst.AddPrefab.Duplicate(RTEditor.inst.PrefabPopups.GetPopup(dialog).Content);

            var hover = gameObject.AddComponent<HoverUI>();
            hover.animateSca = true;
            hover.animatePos = false;

            var storage = gameObject.GetComponent<PrefabPanelStorage>();

            var name = storage.nameText;
            var typeName = storage.typeNameText;
            var typeImage = storage.typeImage;
            var typeImageShade = storage.typeImageShade;
            var typeIconImage = storage.typeIconImage;
            var addPrefabObject = storage.button;
            var delete = storage.deleteButton;

            EditorThemeManager.ApplySelectable(addPrefabObject, ThemeGroup.List_Button_1);
            EditorThemeManager.ApplyLightText(name);
            EditorThemeManager.ApplyLightText(typeName);
            EditorThemeManager.ApplyGraphic(delete.image, ThemeGroup.Delete, true);
            EditorThemeManager.ApplyGraphic(delete.transform.GetChild(0).GetComponent<Image>(), ThemeGroup.Delete_Text);
            EditorThemeManager.ApplyGraphic(typeImage, ThemeGroup.Null, true);
            EditorThemeManager.ApplyGraphic(typeImageShade, ThemeGroup.Null, true);

            TooltipHelper.AssignTooltip(gameObject, $"{dialog} Prefab List Button", 3.2f);

            addPrefabObject.onClick.ClearAll();
            delete.onClick.ClearAll();

            GameObject = gameObject;
            HoverUI = hover;
            Button = addPrefabObject;
            DeleteButton = delete;
            Name = name;
            TypeText = typeName;
            TypeImage = typeImage;
            TypeIcon = typeIconImage;
            FilePath = prefab.filePath;

            Render(updateCurrentPrefab);
            SetActive(RTPrefabEditor.inst.ContainsName(Prefab, dialog));
        }

        /// <summary>
        /// Renders the whole prefab panel.
        /// </summary>
        /// <param name="updateCurrentPrefab">If the current quick prefab should be set instead of importing.</param>
        public void Render(bool updateCurrentPrefab = false)
        {
            var gameObject = GameObject;
            if (!gameObject)
                return;

            RenderHoverUI();

            RenderIcon();
            RenderName();

            if (isFolder)
            {
                RenderTooltip();
                return;
            }

            var prefab = Prefab;
            var prefabType = prefab.GetPrefabType();

            RenderPrefabType(prefabType);
            RenderTooltip(prefab, prefabType);
            RenderDeleteButton();
            UpdateFunction(updateCurrentPrefab);
        }

        /// <summary>
        /// Renders the folder button's icon.
        /// </summary>
        public void RenderIcon()
        {
            if (!isFolder || !IconImage)
                return;

            IconImage.sprite = RTFile.FileExists(RTFile.CombinePaths(FilePath, $"folder_icon{FileFormat.PNG.Dot()}")) ?
                SpriteHelper.LoadSprite(RTFile.CombinePaths(FilePath, $"folder_icon{FileFormat.PNG.Dot()}")) :
                EditorSprites.OpenSprite;
        }

        /// <summary>
        /// Renders the prefab panel name.
        /// </summary>
        public void RenderName() => RenderName(isFolder ? Path.GetFileName(FilePath) : Prefab.name);

        /// <summary>
        /// Renders the prefab panel name.
        /// </summary>
        /// <param name="name">Name of the prefab.</param>
        public void RenderName(string name)
        {
            Name.text = name;
            var isExternal = IsExternal;
            Name.horizontalOverflow = isExternal ? EditorConfig.Instance.PrefabExternalNameHorizontalWrap.Value : EditorConfig.Instance.PrefabInternalNameHorizontalWrap.Value;
            Name.verticalOverflow = isExternal ? EditorConfig.Instance.PrefabExternalNameVerticalWrap.Value : EditorConfig.Instance.PrefabInternalNameVerticalWrap.Value;
            Name.fontSize = isExternal ? EditorConfig.Instance.PrefabExternalNameFontSize.Value : EditorConfig.Instance.PrefabInternalNameFontSize.Value;
        }

        /// <summary>
        /// Renders the prefab panel hover component.
        /// </summary>
        public void RenderHoverUI() => HoverUI.size = EditorConfig.Instance.PrefabButtonHoverSize.Value;

        /// <summary>
        /// Renders the prefab panel hover component.
        /// </summary>
        /// <param name="size">Size to grow when hovered.</param>
        public void RenderHoverUI(float size) => HoverUI.size = size;

        /// <summary>
        /// Renders the prefab panel prefab type.
        /// </summary>
        public void RenderPrefabType() => RenderPrefabType(Prefab.GetPrefabType());

        /// <summary>
        /// Renders the prefab panel prefab type.
        /// </summary>
        /// <param name="prefabType">Type of the prefab.</param>
        public void RenderPrefabType(PrefabType prefabType) => RenderPrefabType(prefabType.name, prefabType.color, prefabType.icon);

        /// <summary>
        /// Renders the prefab panel prefab type.
        /// </summary>
        /// <param name="name">Name of the prefab type.</param>
        /// <param name="color">Color of the prefab type.</param>
        /// <param name="icon">Icon of the prefab type.</param>
        public void RenderPrefabType(string name, Color color, Sprite icon)
        {
            TypeText.text = name;
            TypeImage.color = color;
            TypeIcon.sprite = icon;

            var isExternal = IsExternal;
            TypeText.horizontalOverflow = isExternal ? EditorConfig.Instance.PrefabExternalTypeHorizontalWrap.Value : EditorConfig.Instance.PrefabInternalTypeHorizontalWrap.Value;
            TypeText.verticalOverflow = isExternal ? EditorConfig.Instance.PrefabExternalTypeVerticalWrap.Value : EditorConfig.Instance.PrefabInternalTypeVerticalWrap.Value;
            TypeText.fontSize = isExternal ? EditorConfig.Instance.PrefabExternalTypeFontSize.Value : EditorConfig.Instance.PrefabInternalTypeFontSize.Value;
        }

        /// <summary>
        /// Renders the prefab panel tooltip.
        /// </summary>
        public void RenderTooltip()
        {
            if (isFolder)
            {
                try
                {
                    if (infoJN == null)
                        GetInfo();

                    if (infoJN != null && !string.IsNullOrEmpty(infoJN["desc"]))
                        TooltipHelper.AddHoverTooltip(GameObject, $"Folder - {Path.GetFileName(FilePath)}", infoJN["desc"], clear: true);
                    else
                        TooltipHelper.AddHoverTooltip(GameObject, "Folder", Path.GetFileName(FilePath), clear: true);
                }
                catch (System.Exception ex)
                {
                    CoreHelper.LogError($"Had an exception with trying to add info to the {nameof(PrefabPanel)}.\nGameObject: {GameObject}\nFilePath: {FilePath}\nException: {ex}");
                }

                return;
            }

            RenderTooltip(Prefab, Prefab.GetPrefabType());
        }

        /// <summary>
        /// Renders the prefab panel tooltip.
        /// </summary>
        /// <param name="prefab">Prefab reference.</param>
        /// <param name="prefabType">Prefab type reference.</param>
        public void RenderTooltip(Prefab prefab, PrefabType prefabType)
        {
            TooltipHelper.AddHoverTooltip(GameObject,
                "<#" + LSColors.ColorToHex(prefabType.color) + ">" + prefab.name + "</color>",
                "Offset: " + prefab.offset +
                "<br>Type: " + prefabType.name +
                "<br>Count: " + prefab.beatmapObjects.Count +
                "<br>Description: " + prefab.description, clear: true);
        }

        /// <summary>
        /// Updates the prefab panels' function.
        /// </summary>
        /// <param name="updateCurrentPrefab">If the current quick prefab should be set instead of importing.</param>
        public void UpdateFunction(bool updateCurrentPrefab = false)
        {
            var prefab = Prefab;
            switch (Dialog)
            {
                case PrefabDialog.Internal: {
                        var clickable = GameObject.AddComponent<ContextClickable>();
                        clickable.onClick = eventData =>
                        {
                            if (RTEditor.inst.prefabPickerEnabled)
                            {
                                var prefabInstanceID = PAObjectBase.GetStringID();
                                if (RTEditor.inst.selectingMultiple)
                                {
                                    foreach (var timelineObject in EditorTimeline.inst.SelectedObjects)
                                    {
                                        if (!timelineObject.TryGetPrefabable(out IPrefabable prefabable))
                                            return;

                                        prefabable.PrefabID = prefab.id;
                                        prefabable.PrefabInstanceID = prefabInstanceID;
                                        EditorTimeline.inst.RenderTimelineObject(timelineObject);
                                    }
                                }
                                else if (EditorTimeline.inst.CurrentSelection.TryGetPrefabable(out IPrefabable singlePrefabable))
                                {
                                    singlePrefabable.PrefabID = prefab.id;
                                    singlePrefabable.PrefabInstanceID = prefabInstanceID;
                                    EditorTimeline.inst.RenderTimelineObject(EditorTimeline.inst.CurrentSelection);

                                    if (EditorTimeline.inst.CurrentSelection.isBeatmapObject)
                                        ObjectEditor.inst.OpenDialog(EditorTimeline.inst.CurrentSelection.GetData<BeatmapObject>());
                                    if (EditorTimeline.inst.CurrentSelection.isBackgroundObject)
                                        RTBackgroundEditor.inst.OpenDialog(EditorTimeline.inst.CurrentSelection.GetData<BackgroundObject>());
                                }

                                RTEditor.inst.prefabPickerEnabled = false;

                                return;
                            }

                            if (updateCurrentPrefab)
                            {
                                RTPrefabEditor.inst.UpdateCurrentPrefab(prefab);
                                CoroutineHelper.StartCoroutine(RTPrefabEditor.inst.RefreshInternalPrefabs());
                                return;
                            }

                            if (eventData.button == PointerEventData.InputButton.Right)
                            {
                                EditorContextMenu.inst.ShowContextMenu(
                                    new ButtonFunction("Add to Level", () =>
                                    {
                                        RTPrefabEditor.inst.AddPrefabObjectToLevel(prefab);
                                        RTEditor.inst.PrefabPopups.Close();
                                    }),
                                    new ButtonFunction("Create Prefab", () =>
                                    {
                                        PrefabEditor.inst.OpenDialog();
                                        RTPrefabEditor.inst.createInternal = true;
                                    }),
                                    new ButtonFunction("Assign to Quick Prefab", () =>
                                    {
                                        RTPrefabEditor.inst.UpdateCurrentPrefab(prefab);
                                        CoroutineHelper.StartCoroutine(RTPrefabEditor.inst.RefreshInternalPrefabs());
                                    }),
                                    new ButtonFunction(true),
                                    new ButtonFunction("Edit", () =>
                                    {
                                        RTPrefabEditor.inst.PrefabExternalEditor.Open();
                                        RTPrefabEditor.inst.RenderPrefabExternalDialog(this);
                                    }),
                                    new ButtonFunction("Delete", () => RTEditor.inst.ShowWarningPopup("Are you sure you want to delete this prefab? (This is permanent!)", () =>
                                    {
                                        RTPrefabEditor.inst.DeleteInternalPrefab(Prefab);
                                        RTEditor.inst.HideWarningPopup();
                                    }, RTEditor.inst.HideWarningPopup))
                                    );
                                return;
                            }

                            RTPrefabEditor.inst.AddPrefabObjectToLevel(prefab);
                            RTEditor.inst.PrefabPopups.Close();
                        };
                        break;
                    }
                case PrefabDialog.External: {
                        var clickable = GameObject.AddComponent<ContextClickable>();
                        clickable.onClick = eventData =>
                        {
                            if (RTEditor.inst.prefabPickerEnabled)
                                RTEditor.inst.prefabPickerEnabled = false;

                            if (RTPrefabEditor.inst.savingToPrefab && RTPrefabEditor.inst.prefabToSaveFrom != null)
                            {
                                RTPrefabEditor.inst.savingToPrefab = false;

                                var prefabToSaveTo = Prefab;

                                prefabToSaveTo.beatmapObjects = RTPrefabEditor.inst.prefabToSaveFrom.beatmapObjects.Clone();
                                prefabToSaveTo.prefabObjects = RTPrefabEditor.inst.prefabToSaveFrom.prefabObjects.Clone();
                                prefabToSaveTo.offset = RTPrefabEditor.inst.prefabToSaveFrom.offset;
                                prefabToSaveTo.type = RTPrefabEditor.inst.prefabToSaveFrom.type;
                                prefabToSaveTo.typeID = RTPrefabEditor.inst.prefabToSaveFrom.typeID;

                                var prefabType = prefabToSaveTo.GetPrefabType();

                                RenderName();
                                RenderPrefabType(prefabType);
                                RenderTooltip(prefab, prefabType);

                                RTFile.WriteToFile(prefabToSaveTo.filePath, prefabToSaveTo.ToJSON().ToString());

                                RTEditor.inst.PrefabPopups.Close();

                                RTPrefabEditor.inst.prefabToSaveFrom = null;

                                EditorManager.inst.DisplayNotification("Applied all changes to External Prefab.", 2f, EditorManager.NotificationType.Success);

                                return;
                            }

                            if (eventData.button == PointerEventData.InputButton.Right)
                            {
                                EditorContextMenu.inst.ShowContextMenu(
                                    new ButtonFunction("Import", () => RTPrefabEditor.inst.ImportPrefabIntoLevel(Prefab)),
                                    new ButtonFunction("Convert to VG", () => RTPrefabEditor.inst.ConvertPrefab(Prefab)),
                                    new ButtonFunction("Open", () =>
                                    {
                                        RTPrefabEditor.inst.PrefabExternalEditor.Open();
                                        RTPrefabEditor.inst.RenderPrefabExternalDialog(this);
                                    }),
                                    new ButtonFunction(true),
                                    new ButtonFunction("Create folder", () => RTEditor.inst.ShowFolderCreator(RTFile.CombinePaths(RTEditor.inst.BeatmapsPath, RTEditor.inst.PrefabPath), () => { RTEditor.inst.UpdatePrefabPath(true); RTEditor.inst.HideNameEditor(); })),
                                    new ButtonFunction("Create Prefab", () =>
                                    {
                                        PrefabEditor.inst.OpenDialog();
                                        RTPrefabEditor.inst.createInternal = false;
                                    }),
                                    new ButtonFunction(true),
                                    new ButtonFunction("Cut", () =>
                                    {
                                        RTPrefabEditor.inst.shouldCutPrefab = true;
                                        RTPrefabEditor.inst.copiedPrefabPath = FilePath;
                                        EditorManager.inst.DisplayNotification($"Cut {prefab.name}!", 1.5f, EditorManager.NotificationType.Success);
                                        CoreHelper.Log($"Cut prefab: {RTPrefabEditor.inst.copiedPrefabPath}");
                                    }),
                                    new ButtonFunction("Copy", () =>
                                    {
                                        RTPrefabEditor.inst.shouldCutPrefab = false;
                                        RTPrefabEditor.inst.copiedPrefabPath = FilePath;
                                        EditorManager.inst.DisplayNotification($"Copied {prefab.name}!", 1.5f, EditorManager.NotificationType.Success);
                                        CoreHelper.Log($"Copied prefab: {RTPrefabEditor.inst.copiedPrefabPath}");
                                    }),
                                    new ButtonFunction("Paste", RTPrefabEditor.inst.PastePrefab),
                                    new ButtonFunction("Delete", () => RTEditor.inst.ShowWarningPopup("Are you sure you want to delete this prefab? (This is permanent!)", () =>
                                    {
                                        RTPrefabEditor.inst.DeleteExternalPrefab(this);
                                        RTEditor.inst.HideWarningPopup();
                                    }, RTEditor.inst.HideWarningPopup))
                                    );

                                return;
                            }

                            if (!RTPrefabEditor.ImportPrefabsDirectly)
                            {
                                RTPrefabEditor.inst.PrefabExternalEditor.Open();
                                RTPrefabEditor.inst.RenderPrefabExternalDialog(this);
                            }
                            else
                                RTPrefabEditor.inst.ImportPrefabIntoLevel(Prefab);
                        };
                        break;
                    }
            }
        }

        /// <summary>
        /// Renders the prefab panel delete button.
        /// </summary>
        public void RenderDeleteButton()
        {
            var deleteButton = DeleteButton;
            var deleteRT = deleteButton.transform.AsRT();
            var isExternal = IsExternal;
            deleteRT.anchoredPosition = isExternal ? EditorConfig.Instance.PrefabExternalDeleteButtonPos.Value : EditorConfig.Instance.PrefabInternalDeleteButtonPos.Value;
            deleteRT.sizeDelta = isExternal ? EditorConfig.Instance.PrefabExternalDeleteButtonSca.Value : EditorConfig.Instance.PrefabInternalDeleteButtonSca.Value;

            deleteButton.onClick.NewListener(() => RTEditor.inst.ShowWarningPopup("Are you sure you want to delete this prefab? (This is permanent!)", () =>
            {
                switch (Dialog)
                {
                    case PrefabDialog.Internal: {
                            RTPrefabEditor.inst.DeleteInternalPrefab(Prefab);
                            break;
                        }
                    case PrefabDialog.External: {
                            RTPrefabEditor.inst.DeleteExternalPrefab(this);
                            break;
                        }
                }
                RTEditor.inst.HideWarningPopup();
            }, RTEditor.inst.HideWarningPopup));
        }

        /// <summary>
        /// Sets the prefab panel active state.
        /// </summary>
        /// <param name="active">Active state to set.</param>
        public void SetActive(bool active)
        {
            var gameObject = GameObject;
            if (gameObject)
                gameObject.SetActive(active);
        }

        /// <summary>
        /// Gets the info file.
        /// </summary>
        public void GetInfo()
        {
            if (RTFile.TryReadFromFile(RTFile.CombinePaths(FilePath, $"folder_info{FileFormat.JSON.Dot()}"), out string file))
                infoJN = JSON.Parse(file);
        }

        #endregion
    }
}
