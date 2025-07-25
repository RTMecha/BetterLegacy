﻿using System;
using System.Collections.Generic;
using System.IO;

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
using BetterLegacy.Core.Managers;
using BetterLegacy.Core.Prefabs;
using BetterLegacy.Editor.Components;
using BetterLegacy.Editor.Managers;

namespace BetterLegacy.Editor.Data
{
    /// <summary>
    /// Object for storing level panel data.
    /// </summary>
    public class LevelPanel : Exists
    {
        public LevelPanel() { }

        #region Properties

        #region UI

        /// <summary>
        /// The level panel game object.
        /// </summary>
        public GameObject GameObject { get; set; }

        /// <summary>
        /// Game object used for the level combiner.
        /// </summary>
        public GameObject CombinerGameObject { get; set; }

        /// <summary>
        /// The hover scale component of the level panel.
        /// </summary>
        public HoverUI Hover { get; set; }

        /// <summary>
        /// The title of the level panel.
        /// </summary>
        public Text LevelTitle { get; set; }

        /// <summary>
        /// The icon of the level panel.
        /// </summary>
        public Image IconImage { get; set; }

        /// <summary>
        /// The button of the level panel.
        /// </summary>
        public FolderButtonFunction Button { get; set; }

        /// <summary>
        /// The delete button of the level panel.
        /// </summary>
        public DeleteButtonStorage DeleteButton { get; set; }

        /// <summary>
        /// The UI that displays that the level is selected.
        /// </summary>
        public GameObject SelectedUI { get; set; }

        #endregion

        #region Data

        /// <summary>
        /// The level reference.
        /// </summary>
        public Level Level { get; set; }

        /// <summary>
        /// Folder name of the level panel.
        /// </summary>
        public string Name => Path.GetFileName(RTFile.RemoveEndSlash(FolderPath));

        /// <summary>
        /// Direct path to the level panels' folder.
        /// </summary>
        public string FolderPath { get; set; }

        /// <summary>
        /// Formats the name to display.
        /// </summary>
        public string NameFormat
        {
            get
            {
                var metadata = Level.metadata;
                return string.Format(EditorConfig.Instance.OpenLevelTextFormatting.Value,
                    LSText.ClampString(Name, EditorConfig.Instance.OpenLevelFolderNameMax.Value),
                    LSText.ClampString(metadata.song.title, EditorConfig.Instance.OpenLevelSongNameMax.Value),
                    LSText.ClampString(metadata.artist.name, EditorConfig.Instance.OpenLevelArtistNameMax.Value),
                    LSText.ClampString(metadata.creator.name, EditorConfig.Instance.OpenLevelCreatorNameMax.Value),
                    metadata.song.difficulty,
                    LSText.ClampString(metadata.song.description, EditorConfig.Instance.OpenLevelDescriptionMax.Value),
                    LSText.ClampString(metadata.beatmap.dateEdited, EditorConfig.Instance.OpenLevelDateMax.Value),
                    LSText.ClampString(metadata.beatmap.dateCreated, EditorConfig.Instance.OpenLevelDateMax.Value),
                    LSText.ClampString(metadata.beatmap.datePublished, EditorConfig.Instance.OpenLevelDateMax.Value));
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

        #endregion

        #endregion

        #region Fields

        /// <summary>
        /// If the level is selected in the editor.
        /// </summary>
        public bool combinerSelected;

        /// <summary>
        /// If the level panel is a folder button instead.
        /// </summary>
        public bool isFolder;

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

        JSONNode infoJN;

        #endregion

        #region Methods

        /// <summary>
        /// Initializes the level panel as a folder.
        /// </summary>
        public void Init(string directory)
        {
            FolderPath = directory;
            isFolder = true;

            var gameObject = GameObject;
            if (gameObject)
                CoreHelper.Destroy(gameObject);

            gameObject = EditorManager.inst.spriteFolderButtonPrefab.Duplicate(RTEditor.inst.OpenLevelPopup.Content, $"Folder [{Name}]");
            GameObject = gameObject;

            Button = gameObject.AddComponent<FolderButtonFunction>();

            Hover = gameObject.AddComponent<HoverUI>();
            Hover.size = EditorConfig.Instance.OpenLevelButtonHoverSize.Value;
            Hover.animatePos = false;
            Hover.animateSca = true;

            var folderButtonStorage = gameObject.GetComponent<SpriteFunctionButtonStorage>();
            LevelTitle = folderButtonStorage.label;
            LevelTitle.enabled = true;
            folderButtonStorage.button.onClick.ClearAll();
            IconImage = folderButtonStorage.image;

            EditorThemeManager.ApplySelectable(folderButtonStorage.button, ThemeGroup.List_Button_1);
            EditorThemeManager.ApplyLightText(folderButtonStorage.label);

            Render();
        }

        /// <summary>
        /// Initializes the level panel as an actual level.
        /// </summary>
        public void Init(Level level)
        {
            Level = level;
            FolderPath = level.path;
            level.editorLevelPanel = this;
            level.isEditor = true;

            var gameObject = GameObject;
            if (gameObject)
                CoreHelper.Destroy(gameObject);

            gameObject = EditorManager.inst.folderButtonPrefab.Duplicate(RTEditor.inst.OpenLevelPopup.Content, $"Folder [{Name}]");
            GameObject = gameObject;
            var folderButtonFunction = gameObject.AddComponent<FolderButtonFunction>();

            Hover = gameObject.AddComponent<HoverUI>();
            Hover.animatePos = false;
            Hover.animateSca = true;

            var folderButtonStorage = gameObject.GetComponent<FunctionButtonStorage>();
            LevelTitle = folderButtonStorage.label;
            LevelTitle.enabled = true;
            folderButtonStorage.button.onClick.ClearAll();
            Button = folderButtonFunction;
            EditorThemeManager.ApplySelectable(folderButtonStorage.button, ThemeGroup.List_Button_1);
            EditorThemeManager.ApplyLightText(folderButtonStorage.label);

            var iconBase = Creator.NewUIObject("icon base", gameObject.transform);
            var iconBaseImage = iconBase.AddComponent<Image>();
            iconBase.AddComponent<Mask>().showMaskGraphic = false;
            iconBase.transform.AsRT().anchoredPosition = EditorConfig.Instance.OpenLevelCoverPosition.Value;
            iconBase.transform.AsRT().sizeDelta = EditorConfig.Instance.OpenLevelCoverScale.Value;
            EditorThemeManager.ApplyGraphic(iconBaseImage, ThemeGroup.Null, true);

            var icon = Creator.NewUIObject("icon", iconBase.transform);
            icon.transform.AsRT().anchoredPosition = Vector3.zero;
            icon.transform.AsRT().sizeDelta = EditorConfig.Instance.OpenLevelCoverScale.Value;

            var iconImage = icon.AddComponent<Image>();
            iconImage.sprite = Level.icon;
            IconImage = iconImage;

            var delete = EditorPrefabHolder.Instance.DeleteButton.Duplicate(gameObject.transform, "delete");

            var deleteStorage = delete.GetComponent<DeleteButtonStorage>();
            DeleteButton = deleteStorage;

            delete.transform.AsRT().anchoredPosition = new Vector2(-5f, 0f);

            EditorThemeManager.ApplyGraphic(deleteStorage.baseImage, ThemeGroup.Delete, true, roundedSide: SpriteHelper.RoundedSide.W);
            EditorThemeManager.ApplyGraphic(deleteStorage.image, ThemeGroup.Delete_Text, true, roundedSide: SpriteHelper.RoundedSide.W);
            delete.SetActive(EditorConfig.Instance.OpenLevelShowDeleteButton.Value);

            SelectedUI = Creator.NewUIObject("selected", gameObject.transform);
            SelectedUI.SetActive(false);
            var selectedImage = SelectedUI.AddComponent<Image>();
            selectedImage.color = LSColors.HexToColorAlpha("0088FF25");

            RectValues.FullAnchored.AssignToRectTransform(selectedImage.rectTransform);

            Render();
        }

        /// <summary>
        /// Initializes the level panel in the level combiner.
        /// </summary>
        public void InitLevelCombiner()
        {
            if (isFolder)
                return;

            var folder = FolderPath;
            var metadata = Level.metadata;

            if (CombinerGameObject)
                CoreHelper.Destroy(CombinerGameObject, true);

            var gameObjectBase = Creator.NewUIObject($"Folder [{Name}]", LevelCombiner.editorDialogContent);
            CombinerGameObject = gameObjectBase;

            var image = gameObjectBase.AddComponent<Image>();

            EditorThemeManager.ApplyGraphic(image, ThemeGroup.Function_1, true);

            CombinerGameObject.transform.AsRT().sizeDelta = new Vector2(750f, 42f);

            var gameObject = EditorManager.inst.folderButtonPrefab.Duplicate(CombinerGameObject.transform, "Button");
            UIManager.SetRectTransform(gameObject.transform.AsRT(), Vector2.zero, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(740f, 32f));
            var folderButtonStorage = gameObject.GetComponent<FunctionButtonStorage>();

            var hoverUI = gameObject.AddComponent<HoverUI>();
            hoverUI.size = EditorConfig.Instance.OpenLevelButtonHoverSize.Value;
            hoverUI.animatePos = false;
            hoverUI.animateSca = true;

            folderButtonStorage.label.text = NameFormat;

            folderButtonStorage.label.horizontalOverflow = EditorConfig.Instance.OpenLevelTextHorizontalWrap.Value;
            folderButtonStorage.label.verticalOverflow = EditorConfig.Instance.OpenLevelTextVerticalWrap.Value;
            folderButtonStorage.label.fontSize = EditorConfig.Instance.OpenLevelTextFontSize.Value;

            gameObject.AddComponent<HoverTooltip>().tooltipLangauges.Add(new HoverTooltip.Tooltip
            {
                desc = "<#" + LSColors.ColorToHex(metadata.song.DifficultyType.Color) + ">" + metadata.artist.name + " - " + metadata.song.title,
                hint = "</color>" + metadata.song.description
            });

            folderButtonStorage.button.onClick.AddListener(() =>
            {
                combinerSelected = !combinerSelected;
                image.enabled = combinerSelected;
            });
            image.enabled = combinerSelected;

            var icon = Creator.NewUIObject("icon", gameObject.transform);
            icon.layer = 5;
            var iconImage = icon.AddComponent<Image>();

            var iconPosition = EditorConfig.Instance.OpenLevelCoverPosition.Value;
            iconPosition.x += -75f;
            icon.transform.AsRT().anchoredPosition = iconPosition;
            icon.transform.AsRT().sizeDelta = EditorConfig.Instance.OpenLevelCoverScale.Value;

            iconImage.sprite = Level.icon ?? SteamWorkshop.inst.defaultSteamImageSprite;

            EditorThemeManager.ApplySelectable(folderButtonStorage.button, ThemeGroup.List_Button_1);
            EditorThemeManager.ApplyLightText(folderButtonStorage.label);

            string difficultyName = difficultyNames[Mathf.Clamp(metadata.song.difficulty, 0, difficultyNames.Length - 1)];

            CombinerSetActive(RTString.SearchString(LevelCombiner.searchTerm, Path.GetFileName(folder), metadata.song.title, metadata.artist.name, metadata.creator.name, metadata.song.description, difficultyName));
        }

        /// <summary>
        /// Renders the whole level panel.
        /// </summary>
        public void Render()
        {
            RenderIcon();
            RenderTitle();
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

            RenderIcon(Level?.icon);
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

        /// <summary>
        /// Renders the level panel title.
        /// </summary>
        public void RenderTitle()
        {
            if (isFolder)
            {
                RenderTitle(Name);
                return;
            }

            RenderTitle(NameFormat);
        }

        /// <summary>
        /// Renders the level panel title.
        /// </summary>
        /// <param name="text">Title of the level panel.</param>
        public void RenderTitle(string text)
        {
            LevelTitle.text = text;

            LevelTitle.horizontalOverflow = EditorConfig.Instance.OpenLevelTextHorizontalWrap.Value;
            LevelTitle.verticalOverflow = EditorConfig.Instance.OpenLevelTextVerticalWrap.Value;
            LevelTitle.fontSize = EditorConfig.Instance.OpenLevelTextFontSize.Value;
        }

        /// <summary>
        /// Renders the level panel hover component.
        /// </summary>
        public void RenderHover() => RenderHover(EditorConfig.Instance.OpenLevelButtonHoverSize.Value);

        /// <summary>
        /// Renders the level panel hover component.
        /// </summary>
        /// <param name="size">Size to grow when hovered.</param>
        public void RenderHover(float size) => Hover.size = size;

        /// <summary>
        /// Renders the level panel tooltips.
        /// </summary>
        public void RenderTooltip()
        {
            TooltipHelper.AssignTooltip(GameObject, "Level Panel", 3f);

            if (isFolder)
            {
                if (infoJN == null)
                    GetInfo();

                if (infoJN != null && !string.IsNullOrEmpty(infoJN["desc"]))
                    TooltipHelper.AddHoverTooltip(GameObject, $"Folder - {Name}", infoJN["desc"], clear: true);
                else
                    TooltipHelper.AddHoverTooltip(GameObject, "Folder", Name, clear: true);

                return;
            }

            var metadata = Level.metadata;

            TooltipHelper.AddHoverTooltip(GameObject, "<#" + LSColors.ColorToHex(metadata.song.DifficultyType.Color) + ">" + metadata.artist.name + " - " + metadata.song.title,
                $"</color><br>Folder: {Level.FolderName}<br>Date Edited: {metadata.beatmap.dateEdited}<br>Date Created: {metadata.beatmap.dateCreated}<br>Description: {metadata.song.description}");
        }

        /// <summary>
        /// Updates the level panels' main function.
        /// </summary>
        public void UpdateFunction()
        {
            if (isFolder)
            {
                var path = FolderPath;
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
                            new ButtonFunction("Open folder", () =>
                            {
                                RTEditor.inst.editorPathField.text = path.Replace(RTEditor.inst.BeatmapsPath + "/", "");
                                RTEditor.inst.UpdateEditorPath(false);
                            }, "Level Panel Open Folder"),
                            new ButtonFunction("Create folder", () => RTEditor.inst.ShowFolderCreator(RTFile.CombinePaths(RTEditor.inst.BeatmapsPath, RTEditor.inst.EditorPath), EndFolderCreation), "Level Panel Create Folder"),
                            new ButtonFunction("Create level", EditorManager.inst.OpenNewLevelPopup),
                            new ButtonFunction(true),
                            new ButtonFunction("Rename", () => RTEditor.inst.ShowNameEditor("Folder Renamer", "Folder name", "Rename", () =>
                            {
                                RTFile.MoveDirectory(path, path.Replace(Name, RTFile.ValidateDirectory(RTEditor.inst.folderCreatorName.text)).Replace("\\", "/"));

                                EditorLevelManager.inst.LoadLevels();
                                RTEditor.inst.HideNameEditor();
                            }), "Level Panel Rename Folder"),
                            new ButtonFunction("Paste", EditorLevelManager.inst.PasteLevel, "Level Panel Paste"),
                            new ButtonFunction("Delete", () =>
                            {
                                RTEditor.inst.ShowWarningPopup("Are you <b>100%</b> sure you want to delete this folder? This <b>CANNOT</b> be undone! Always make sure you have backups.", () =>
                                {
                                    RTFile.DeleteDirectory(path);
                                    RTEditor.inst.UpdateEditorPath(true);
                                    EditorManager.inst.DisplayNotification("Deleted folder!", 2f, EditorManager.NotificationType.Success);
                                    RTEditor.inst.HideWarningPopup();
                                }, RTEditor.inst.HideWarningPopup);
                            }, "Level Panel Delete"),
                            new ButtonFunction(true),
                            new ButtonFunction("ZIP Folder", () => EditorLevelManager.inst.ZipLevel(this), "Level Panel ZIP"),
                            new ButtonFunction("Copy Path", () => LSText.CopyToClipboard(path), "Level Panel Copy Folder"),
                            new ButtonFunction("Open in File Explorer", () => RTFile.OpenInFileBrowser.Open(path), "Level Panel Open Explorer"),
                            new ButtonFunction("Open List in File Explorer", RTEditor.inst.OpenLevelListFolder, "Level List Open Explorer"),
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
                            new ButtonFunction("Clear Icon", () =>
                            {
                                RTEditor.inst.ShowWarningPopup("Are you sure you want to clear the folder icon? This will delete the icon file.", () =>
                                {
                                    RTEditor.inst.HideWarningPopup();
                                    RTFile.DeleteFile(RTFile.CombinePaths(path, $"folder_icon{FileFormat.PNG.Dot()}"));
                                    RenderIcon();
                                    EditorManager.inst.DisplayNotification("Deleted icon!", 1.5f, EditorManager.NotificationType.Success);
                                }, RTEditor.inst.HideWarningPopup);
                            }),
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
                            new ButtonFunction("Clear Info File", () =>
                            {
                                RTEditor.inst.ShowWarningPopup("Are you sure you want to delete the info file?", () =>
                                {
                                    RTFile.DeleteFile(RTFile.CombinePaths(path, $"folder_info{FileFormat.JSON.Dot()}"));
                                    infoJN = null;
                                    RenderTooltip();
                                    RTEditor.inst.HideWarningPopup();
                                    EditorManager.inst.DisplayNotification("Deleted info file!", 1.5f, EditorManager.NotificationType.Success);
                                }, RTEditor.inst.HideWarningPopup);
                            }));

                        return;
                    }

                    RTEditor.inst.editorPathField.text = path.Replace(RTEditor.inst.BeatmapsPath + "/", "");
                    RTEditor.inst.UpdateEditorPath(false);
                };
                return;
            }

            Button.onClick = eventData =>
            {
                if (LevelTemplateEditor.inst.choosingLevelTemplate)
                {
                    LevelTemplateEditor.inst.CreateTemplate(Level.path);

                    return;
                }

                var selectedLevels = EditorLevelManager.inst.SelectedLevels;

                if (eventData.button == PointerEventData.InputButton.Right)
                {
                    var list = new List<ButtonFunction>();

                    if (!selectedLevels.IsEmpty())
                        list = new List<ButtonFunction>()
                        {
                            new ButtonFunction("Combine", () =>
                            {
                                RTEditor.inst.ShowNameEditor("Combiner", "Combined Level name", "Combine", () =>
                                {
                                    LevelCombiner.inst.Combine(RTEditor.inst.folderCreatorName.text, EditorLevelManager.inst.SelectedLevels, EditorLevelManager.inst.LoadLevels);
                                });
                            }),
                            new ButtonFunction(true),
                            new ButtonFunction("Create Collection", () =>
                            {
                                EditorManager.inst.DisplayNotification($"Not implemented yet.", 2f, EditorManager.NotificationType.Warning);
                            }),
                            new ButtonFunction("Add to Collection", () =>
                            {
                                EditorManager.inst.DisplayNotification($"Not implemented yet.", 2f, EditorManager.NotificationType.Warning);
                            }),
                        };
                    else
                        list = new List<ButtonFunction>()
                        {
                            new ButtonFunction("Open", () =>
                            {
                                EditorLevelManager.inst.LoadLevel(this);
                                RTEditor.inst.OpenLevelPopup.Close();
                            }, "Level Panel Open"),
                            new ButtonFunction("Show Autosaves", () =>
                            {
                                RTEditor.inst.AutosavePopup.Open();
                                EditorLevelManager.inst.RefreshAutosaveList(this);
                            }, "Level Panel Show Autosaves"),
                            new ButtonFunction("Convert to VG", () => EditorLevelManager.inst.ConvertLevel(this), "Convert Level VG"),
                            new ButtonFunction(true),
                            new ButtonFunction("Create folder", () => RTEditor.inst.ShowFolderCreator(RTFile.CombinePaths(RTEditor.inst.BeatmapsPath, RTEditor.inst.EditorPath), EndFolderCreation), "Level Panel Create Folder"),
                            new ButtonFunction("Create template", () => LevelTemplateEditor.inst.CreateTemplate(Level.path), "Level Panel Create Template"),
                            new ButtonFunction("Create level", EditorManager.inst.OpenNewLevelPopup, "Level Panel Create Level"),
                            new ButtonFunction("Create backup", () => EditorLevelManager.inst.SaveBackup(this), "Level Panel Create Backup"),
                            new ButtonFunction(true),
                            new ButtonFunction("Rename", () => RTEditor.inst.ShowNameEditor("Level Renamer", "Level name", "Rename", () =>
                            {
                                var destination = RTFile.ReplaceSlash(Level.path.Replace(Level.FolderName, RTFile.ValidateDirectory(RTEditor.inst.folderCreatorName.text)));
                                RTFile.MoveDirectory(Level.path, destination);
                                Level.metadata.beatmap.name = RTEditor.inst.folderCreatorName.text;
                                RTFile.WriteToFile(RTFile.CombinePaths(destination, Level.METADATA_LSB), Level.metadata.ToJSON().ToString());

                                EditorLevelManager.inst.LoadLevels();
                                RTEditor.inst.HideNameEditor();
                            }), "Level Panel Rename Level"),
                            new ButtonFunction("Cut", () =>
                            {
                                EditorLevelManager.inst.shouldCutLevel = true;
                                EditorLevelManager.inst.copiedLevelPath = Level.path;
                                EditorManager.inst.DisplayNotification($"Cut {Level.FolderName}!", 1.5f, EditorManager.NotificationType.Success);
                                CoreHelper.Log($"Cut level: {EditorLevelManager.inst.copiedLevelPath}");
                            }, "Level Panel Cut"),
                            new ButtonFunction("Copy", () =>
                            {
                                EditorLevelManager.inst.shouldCutLevel = false;
                                EditorLevelManager.inst.copiedLevelPath = Level.path;
                                EditorManager.inst.DisplayNotification($"Copied {Level.FolderName}!", 1.5f, EditorManager.NotificationType.Success);
                                CoreHelper.Log($"Copied level: {EditorLevelManager.inst.copiedLevelPath}");
                            }, "Level Panel Copy"),
                            new ButtonFunction("Paste", EditorLevelManager.inst.PasteLevel, "Level Panel Paste"),
                            new ButtonFunction("Delete", () =>
                            {
                                RTEditor.inst.ShowWarningPopup("Are you sure you want to delete this level? This CANNOT be undone!", () =>
                                {
                                    RTFile.DeleteDirectory(Level.path);
                                    EditorLevelManager.inst.LoadLevels();
                                    EditorManager.inst.DisplayNotification("Deleted level!", 2f, EditorManager.NotificationType.Success);
                                    RTEditor.inst.HideWarningPopup();
                                }, RTEditor.inst.HideWarningPopup);
                            }),
                            new ButtonFunction(true),
                            new ButtonFunction("Copy Arcade ID", () =>
                            {
                                var metadata = Level.metadata;
                                if (string.IsNullOrEmpty(metadata.arcadeID) || metadata.arcadeID == "0")
                                {
                                    EditorManager.inst.DisplayNotification($"Level does not have an ID assigned to it yet. Open the level, save it and try again.", 3.3f, EditorManager.NotificationType.Warning);
                                    return;
                                }

                                LSText.CopyToClipboard(metadata.arcadeID);
                                EditorManager.inst.DisplayNotification($"Copied Arcade ID ({metadata.arcadeID}) to your clipboard.", 2f, EditorManager.NotificationType.Success);
                            }, "Copy Arcade ID"),
                            new ButtonFunction("Copy Server ID", () =>
                            {
                                var metadata = Level.metadata;
                                if (string.IsNullOrEmpty(metadata.serverID) || metadata.serverID == "0")
                                {
                                    EditorManager.inst.DisplayNotification($"Your level needs to be uploaded to the arcade server before you can copy the server ID.", 3.5f, EditorManager.NotificationType.Warning);
                                    return;
                                }

                                LSText.CopyToClipboard(metadata.serverID);
                                EditorManager.inst.DisplayNotification($"Copied Server ID ({metadata.serverID}) to your clipboard.", 2f, EditorManager.NotificationType.Success);
                            }, "Copy Server ID"),
                            new ButtonFunction(true),
                            new ButtonFunction("ZIP Level", () => EditorLevelManager.inst.ZipLevel(this), "Level Panel ZIP"),
                            new ButtonFunction("Copy Path", () => LSText.CopyToClipboard(Level.path), "Level Panel Copy Folder"),
                            new ButtonFunction("Open in File Explorer", () => RTFile.OpenInFileBrowser.Open(Level.path), "Level Panel Open Explorer"),
                            new ButtonFunction("Open List in File Explorer", RTEditor.inst.OpenLevelListFolder, "Level List Open Explorer"),
                        };

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

                EditorLevelManager.inst.LoadLevel(this);
                RTEditor.inst.OpenLevelPopup.Close();
            };
        }

        /// <summary>
        /// Updates the level panels' deletion function
        /// </summary>
        public void UpdateDeleteFunction()
        {
            if (!DeleteButton)
                return;

            var active = EditorConfig.Instance.OpenLevelShowDeleteButton.Value;

            DeleteButton.gameObject.SetActive(active);

            if (!active)
                return;

            DeleteButton.button.onClick.NewListener(() =>
            {
                RTEditor.inst.ShowWarningPopup("Are you sure you want to delete this level? (It will be moved to a recycling folder)", () =>
                {
                    EditorLevelManager.inst.RecycleLevel(this);
                    EditorManager.inst.DisplayNotification("Deleted level!", 2f, EditorManager.NotificationType.Success);
                    RTEditor.inst.HideWarningPopup();
                }, RTEditor.inst.HideWarningPopup);
            });
        }

        /// <summary>
        /// Sets the icon for the level panel and the level itself.
        /// </summary>
        /// <param name="icon">Icon to set.</param>
        public void SetIcon(Sprite icon)
        {
            if (Level)
                Level.icon = icon;
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
        /// Sets the level panel active state.
        /// </summary>
        /// <param name="active">Active state to set.</param>
        public void SetActive(bool active)
        {
            if (GameObject)
                GameObject.SetActive(active);
        }

        /// <summary>
        /// Sets the level panel (combiner object) active state.
        /// </summary>
        /// <param name="active">Active state to set.</param>
        public void CombinerSetActive(bool active)
        {
            if (CombinerGameObject)
                CombinerGameObject.SetActive(active);
        }

        /// <summary>
        /// Loads the levels' icon.
        /// </summary>
        /// <param name="file">Image file to load.</param>
        /// <param name="onLoad">Action to run when the image is loaded.</param>
        /// <returns>Returns a generated coroutine.</returns>
        public Coroutine LoadImageCoroutine(string file, Action<LevelPanel> onLoad = null) => CoroutineHelper.StartCoroutine(AlephNetwork.DownloadImageTexture($"file://{RTFile.CombinePaths(FolderPath, file)}", cover =>
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
        public void SetDefaultIcon() => SetIcon(isFolder ? EditorSprites.OpenSprite : SteamWorkshop.inst.defaultSteamImageSprite);

        void EndFolderCreation()
        {
            EditorLevelManager.inst.LoadLevels();
            RTEditor.inst.HideNameEditor();
        }

        /// <summary>
        /// Gets the info file.
        /// </summary>
        public void GetInfo()
        {
            if (RTFile.TryReadFromFile(RTFile.CombinePaths(FolderPath, $"folder_info{FileFormat.JSON.Dot()}"), out string file))
                infoJN = JSON.Parse(file);
        }

        public override string ToString() => isFolder ? Path.GetFileName(FolderPath) : Level?.ToString();

        #endregion
    }
}
