﻿using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

using SimpleJSON;
using Crosstales.FB;

using BetterLegacy.Configs;
using BetterLegacy.Core;
using BetterLegacy.Core.Components;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using BetterLegacy.Core.Prefabs;
using BetterLegacy.Core.Runtime;
using BetterLegacy.Editor.Components;
using BetterLegacy.Editor.Managers;

namespace BetterLegacy.Editor.Data
{
    /// <summary>
    /// Object for storing theme panel data. Used for the themes in the theme keyframe.
    /// </summary>
    public class ThemePanel : Exists
    {
        public ThemePanel() { }

        public ThemePanel(int index) => this.index = index;

        #region Properties

        #region UI

        /// <summary>
        /// The theme panel game object.
        /// </summary>
        public GameObject GameObject { get; set; }

        /// <summary>
        /// The name text of the theme panel.
        /// </summary>
        public Text Name { get; set; }

        /// <summary>
        /// The colors list of the theme panel.
        /// </summary>
        public List<Image> Colors { get; set; } = new List<Image>();

        /// <summary>
        /// Context menu clickable component.
        /// </summary>
        public ContextClickable ContextClickable { get; set; }

        /// <summary>
        /// The button to use the theme.
        /// </summary>
        public Button UseButton { get; set; }

        /// <summary>
        /// The button to edit the theme.
        /// </summary>
        public Button EditButton { get; set; }

        /// <summary>
        /// The button to delete the theme.
        /// </summary>
        public Button DeleteButton { get; set; }

        /// <summary>
        /// The base image.
        /// </summary>
        public Image BaseImage { get; set; }

        /// <summary>
        /// The folder button function.
        /// </summary>
        public FolderButtonFunction FolderButton { get; set; }

        /// <summary>
        /// The icon of the theme panel.
        /// </summary>
        public Image IconImage { get; set; }

        #endregion

        #region Data

        /// <summary>
        /// The theme reference.
        /// </summary>
        public BeatmapTheme Theme { get; set; }

        /// <summary>
        /// The file path to the theme.
        /// </summary>
        public string FilePath { get; set; }

        /// <summary>
        /// The original ID from the theme.
        /// </summary>
        public string OriginalID { get; set; }

        #endregion

        #endregion

        #region Fields

        const string TOOLTIP_COLOR_BAR = "▓";

        /// <summary>
        /// If the theme is a default theme (e.g. PA Machine, PA Anarchy, etc)
        /// </summary>
        public bool isDefault;

        /// <summary>
        /// If the theme is a duplicate (or a theme with the ID already exists)
        /// </summary>
        public bool isDuplicate;

        /// <summary>
        /// If the theme panel is a folder button instead.
        /// </summary>
        public bool isFolder;

        /// <summary>
        /// Index of the theme panel.
        /// </summary>
        public int index;

        JSONNode infoJN;

        #endregion

        #region Methods

        /// <summary>
        /// Initializes the theme panel as a folder.
        /// </summary>
        /// <param name="directory">Directory to set to the theme panel.</param>
        public void Init(string directory)
        {
            var gameObject = GameObject;
            if (gameObject)
                CoreHelper.Destroy(gameObject);

            var gameObjectFolder = EditorManager.inst.spriteFolderButtonPrefab.Duplicate(RTThemeEditor.inst.Dialog.Content, $"Folder [{Path.GetFileName(directory)}]", index + 2);
            var folderButtonStorageFolder = gameObjectFolder.GetComponent<SpriteFunctionButtonStorage>();
            var folderButtonFunctionFolder = gameObjectFolder.AddComponent<FolderButtonFunction>();

            var hoverUIFolder = gameObjectFolder.AddComponent<HoverUI>();
            hoverUIFolder.size = EditorConfig.Instance.OpenLevelButtonHoverSize.Value;
            hoverUIFolder.animatePos = false;
            hoverUIFolder.animateSca = true;

            folderButtonStorageFolder.button.onClick.ClearAll();

            GameObject = gameObjectFolder;
            FilePath = directory;
            isFolder = true;
            Name = folderButtonStorageFolder.label;
            FolderButton = folderButtonFunctionFolder;
            IconImage = folderButtonStorageFolder.image;

            EditorThemeManager.ApplySelectable(folderButtonStorageFolder.button, ThemeGroup.List_Button_2);
            EditorThemeManager.ApplyGraphic(folderButtonStorageFolder.label, ThemeGroup.List_Button_2_Text);

            Render();
            SetActive(false);
        }

        /// <summary>
        /// Initializes the theme panel as a beatmap theme panel.
        /// </summary>
        /// <param name="beatmapTheme"><see cref="BeatmapTheme"/> reference.</param>
        /// <param name="defaultTheme">If the theme is a default theme.</param>
        /// <param name="duplicate">If the theme is a duplicate.</param>
        public void Init(BeatmapTheme beatmapTheme, bool defaultTheme = false, bool duplicate = false)
        {
            var gameObject = GameObject;
            if (gameObject)
                CoreHelper.Destroy(gameObject);

            gameObject = EventEditor.inst.ThemePanel.Duplicate(RTThemeEditor.inst.Dialog.Content, "theme-panel", index + 2);

            var storage = gameObject.GetComponent<ThemePanelStorage>();

            GameObject = gameObject;
            UseButton = storage.button;
            ContextClickable = gameObject.AddComponent<ContextClickable>();
            EditButton = storage.edit;
            DeleteButton = storage.delete;
            Name = storage.text;
            BaseImage = storage.baseImage;

            Theme = beatmapTheme;
            Theme.themePanel = this;
            isDefault = defaultTheme;
            isDuplicate = duplicate;
            OriginalID = beatmapTheme.id;

            Colors.Add(storage.color1);
            Colors.Add(storage.color2);
            Colors.Add(storage.color3);
            Colors.Add(storage.color4);

            EditorThemeManager.ApplyGraphic(BaseImage, ThemeGroup.List_Button_2_Normal, true);
            EditorThemeManager.ApplyGraphic(UseButton.image, ThemeGroup.Null, true);
            EditorThemeManager.ApplyGraphic(EditButton.image, ThemeGroup.List_Button_2_Text);
            EditorThemeManager.ApplyGraphic(Name, ThemeGroup.List_Button_2_Text);
            EditorThemeManager.ApplySelectable(DeleteButton, ThemeGroup.Delete_Keyframe_Button, false);

            Render();
            SetActive(false);
        }

        /// <summary>
        /// Renders the whole theme panel.
        /// </summary>
        public void Render()
        {
            RenderIcon();
            RenderName();
            RenderTooltip();

            if (isFolder)
            {
                var directory = FilePath;
                var path = RTFile.ReplaceSlash(directory);
                Name.text = Path.GetFileName(directory);
                FolderButton.onClick = eventData =>
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
                                RTEditor.inst.themePathField.text = path.Replace(RTEditor.inst.BeatmapsPath + "/", "");
                                RTEditor.inst.UpdateThemePath(false);
                            }),
                            new ButtonFunction("Create folder", () => RTEditor.inst.ShowFolderCreator(RTFile.CombinePaths(RTEditor.inst.BeatmapsPath, RTEditor.inst.ThemePath), () => { RTEditor.inst.UpdateThemePath(true); RTEditor.inst.HideNameEditor(); })),
                            new ButtonFunction("Create theme", RTThemeEditor.inst.RenderThemeEditor),
                            new ButtonFunction(true),
                            new ButtonFunction("Paste", RTThemeEditor.inst.PasteTheme),
                            new ButtonFunction("Delete", () =>
                            {
                                RTEditor.inst.ShowWarningPopup("Are you <b>100%</b> sure you want to delete this folder? This <b>CANNOT</b> be undone! Always make sure you have backups.", () =>
                                {
                                    RTFile.DeleteDirectory(path);
                                    RTEditor.inst.UpdateThemePath(true);
                                    EditorManager.inst.DisplayNotification("Deleted folder!", 2f, EditorManager.NotificationType.Success);
                                    RTEditor.inst.HideWarningPopup();
                                }, RTEditor.inst.HideWarningPopup);
                            }),
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

                    RTEditor.inst.themePathField.text = path.Replace(RTEditor.inst.BeatmapsPath + "/", "");
                    RTEditor.inst.UpdateThemePath(false);
                };

                return;
            }

            if (!string.IsNullOrEmpty(Theme.filePath))
                FilePath = RTFile.ReplaceSlash(Theme.filePath);

            RenderColors();

            UseButton.onClick.ClearAll();
            UseButton.onClick.AddListener(Use);

            ContextClickable.onClick = pointerEventData =>
            {
                if (pointerEventData.button != PointerEventData.InputButton.Right)
                {
                    Use();
                    return;
                }

                EditorContextMenu.inst.ShowContextMenu(
                    new ButtonFunction("Use", Use),
                    new ButtonFunction("Edit", () => RTThemeEditor.inst.RenderThemeEditor(Theme)),
                    new ButtonFunction("Convert to VG", () => RTThemeEditor.inst.ConvertTheme(Theme)),
                    new ButtonFunction(true),
                    new ButtonFunction("Create folder", () => RTEditor.inst.ShowFolderCreator(RTFile.CombinePaths(RTEditor.inst.BeatmapsPath, RTEditor.inst.ThemePath), () => { RTEditor.inst.UpdateThemePath(true); RTEditor.inst.HideNameEditor(); })),
                    new ButtonFunction("Create theme", RTThemeEditor.inst.RenderThemeEditor),
                    new ButtonFunction(true),
                    new ButtonFunction("Cut", () =>
                    {
                        if (isDuplicate)
                        {
                            EditorManager.inst.DisplayNotification($"Cannot cut a default theme!", 1.5f, EditorManager.NotificationType.Warning);
                            return;
                        }

                        RTThemeEditor.inst.shouldCutTheme = true;
                        RTThemeEditor.inst.copiedThemePath = Theme.filePath;
                        EditorManager.inst.DisplayNotification($"Cut {Theme.name}!", 1.5f, EditorManager.NotificationType.Success);
                        CoreHelper.Log($"Cut theme: {RTThemeEditor.inst.copiedThemePath}");
                    }),
                    new ButtonFunction("Copy", () =>
                    {
                        if (isDuplicate)
                        {
                            EditorManager.inst.DisplayNotification($"Cannot copy a default theme!", 1.5f, EditorManager.NotificationType.Warning);
                            return;
                        }

                        RTThemeEditor.inst.shouldCutTheme = false;
                        RTThemeEditor.inst.copiedThemePath = Theme.filePath;
                        EditorManager.inst.DisplayNotification($"Copied {Theme.name}!", 1.5f, EditorManager.NotificationType.Success);
                        CoreHelper.Log($"Copied theme: {RTThemeEditor.inst.copiedThemePath}");
                    }),
                    new ButtonFunction("Paste", RTThemeEditor.inst.PasteTheme),
                    new ButtonFunction("Delete", () =>
                    {
                        if (!isDuplicate)
                            RTThemeEditor.inst.DeleteTheme(Theme);
                        else
                            EditorManager.inst.DisplayNotification("Cannot delete a default theme!", 2f, EditorManager.NotificationType.Warning);
                    }),
                    new ButtonFunction(true),
                    new ButtonFunction("Shuffle ID", () => RTThemeEditor.inst.ShuffleThemeID(Theme))
                    );
            };

            EditButton.onClick.ClearAll();
            EditButton.onClick.AddListener(() => RTThemeEditor.inst.RenderThemeEditor(Theme));

            DeleteButton.onClick.ClearAll();
            DeleteButton.interactable = !isDefault;
            if (!isDefault)
                DeleteButton.onClick.AddListener(() => RTThemeEditor.inst.DeleteTheme(Theme));
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
        /// Renders the theme panel name.
        /// </summary>
        public void RenderName() => RenderName(isFolder ? Path.GetFileName(FilePath) : Theme?.name);

        /// <summary>
        /// Renders the theme panel name.
        /// </summary>
        /// <param name="name">Name of the theme.</param>
        public void RenderName(string name) => Name.text = name;

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
                    CoreHelper.LogError($"Had an exception with trying to add info to the {nameof(ThemePanel)}.\nGameObject: {GameObject}\nFilePath: {FilePath}\nException: {ex}");
                }

                return;
            }

            try
            {
                var sb = new StringBuilder();
                sb.AppendLine("Background Color:");
                sb.AppendLine($"<#{RTColors.ColorToHexOptional(Theme.backgroundColor)}>{TOOLTIP_COLOR_BAR}</color>");

                sb.AppendLine("GUI Color | GUI Accent Color");
                sb.AppendLine($"<#{RTColors.ColorToHexOptional(Theme.guiColor)}>{TOOLTIP_COLOR_BAR}</color> " + $"<#{RTColors.ColorToHexOptional(Theme.guiAccentColor)}>{TOOLTIP_COLOR_BAR}</color>");

                sb.AppendLine("Player Colors");
                var playerColors = string.Empty;
                for (int i = 0; i < Mathf.Clamp(Theme.playerColors.Count, 0, 4); i++)
                    playerColors += $"<#{RTColors.ColorToHexOptional(Theme.GetPlayerColor(i))}>{TOOLTIP_COLOR_BAR}</color>";
                sb.AppendLine(playerColors);

                sb.AppendLine("Beatmap Object Colors:");
                var objectColors = string.Empty;
                for (int i = 0; i < Mathf.Clamp(Theme.objectColors.Count, 0, 18); i++)
                    objectColors += $"<#{RTColors.ColorToHexOptional(Theme.GetObjColor(i))}>{TOOLTIP_COLOR_BAR}</color>";
                sb.AppendLine(objectColors);

                sb.AppendLine("BG Object Colors:");
                var bgColors = string.Empty;
                for (int i = 0; i < Mathf.Clamp(Theme.backgroundColors.Count, 0, 9); i++)
                    bgColors += $"<#{RTColors.ColorToHexOptional(Theme.GetBGColor(i))}>{TOOLTIP_COLOR_BAR}</color>";
                sb.AppendLine(bgColors);

                sb.AppendLine("Effect Colors:");
                var fxColors = string.Empty;
                for (int i = 0; i < Mathf.Clamp(Theme.effectColors.Count, 0, 18); i++)
                    fxColors += $"<#{RTColors.ColorToHexOptional(Theme.GetFXColor(i))}>{TOOLTIP_COLOR_BAR}</color>";
                sb.AppendLine(fxColors);

                TooltipHelper.AddHoverTooltip(GameObject, Theme.name, sb.ToString(), clear: true);
            }
            catch (System.Exception ex)
            {
                CoreHelper.LogError($"Had an exception with trying to add info to the {nameof(ThemePanel)}.\nGameObject: {GameObject}\nFilePath: {FilePath}\nException: {ex}");
            }
        }

        /// <summary>
        /// Renders the theme panel colors.
        /// </summary>
        public void RenderColors()
        {
            for (int j = 0; j < Colors.Count; j++)
                Colors[j].color = Theme.GetObjColor(j);
        }

        /// <summary>
        /// Renders a color of the theme panel.
        /// </summary>
        /// <param name="i">Color index.</param>
        public void RenderColor(int i) => RenderColor(i, Theme.GetObjColor(i));

        /// <summary>
        /// Renders a color of the theme panel.
        /// </summary>
        /// <param name="i">Color index.</param>
        /// <param name="color">Color to set.</param>
        public void RenderColor(int i, Color color) => GetColorSlot(i).color = color;

        /// <summary>
        /// Gets an image from the theme panel.
        /// </summary>
        /// <param name="i">Index of the color slot.</param>
        /// <returns>Returns an image representing a color slot.</returns>
        public Image GetColorSlot(int i) => Colors[Mathf.Clamp(i, 0, Colors.Count - 1)];

        /// <summary>
        /// Sets the theme panel active state.
        /// </summary>
        /// <param name="active">Active state to set.</param>
        public void SetActive(bool active)
        {
            if (GameObject)
                GameObject.SetActive(active);
        }

        /// <summary>
        /// Gets the info file.
        /// </summary>
        public void GetInfo()
        {
            if (RTFile.TryReadFromFile(RTFile.CombinePaths(FilePath, $"folder_info{FileFormat.JSON.Dot()}"), out string file))
                infoJN = JSON.Parse(file);
        }

        /// <summary>
        /// Uses the theme for the current selected theme keyframe.
        /// </summary>
        public void Use()
        {
            if (isDuplicate)
            {
                var array = ThemeManager.inst.CustomThemes.Where(x => x.id == Theme.id).Select(x => x.name).ToArray();
                var str = RTString.ArrayToString(array);

                EditorManager.inst.DisplayNotification($"Unable to use Theme [{Theme.name}] due to conflicting themes: {str}.", 2f * array.Length, EditorManager.NotificationType.Error);
                return;
            }

            if (RTEventEditor.inst.SelectedKeyframes.Count > 1 && RTEventEditor.inst.SelectedKeyframes.All(x => RTEventEditor.inst.SelectedKeyframes.Min(y => y.Type) == x.Type))
            {
                foreach (var timelineObject in RTEventEditor.inst.SelectedKeyframes)
                    timelineObject.eventKeyframe.values[0] = Parser.TryParse(Theme.id, 0);
            }
            else
                RTEventEditor.inst.CurrentSelectedKeyframe.values[0] = Parser.TryParse(Theme.id, 0);

            RTLevel.Current?.UpdateEvents(4);
            RTThemeEditor.inst.RenderThemePreview();

            GameData.Current.UpdateUsedThemes();
        }

        public override string ToString() => isFolder ? Path.GetFileName(FilePath) : Theme?.ToString();

        #endregion
    }
}
