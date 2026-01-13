using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

using BetterLegacy.Configs;
using BetterLegacy.Core;
using BetterLegacy.Core.Components;
using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Prefabs;
using BetterLegacy.Editor.Components;
using BetterLegacy.Editor.Managers;

namespace BetterLegacy.Editor.Data.Elements
{
    /// <summary>
    /// Object for storing theme panel data. Used for the themes in the theme keyframe.
    /// </summary>
    public class ThemePanel : EditorPanel<BeatmapTheme>, ISelectable
    {
        #region Constructors

        public ThemePanel() { }

        public ThemePanel(ObjectSource source, int index, bool isDuplicate = false)
        {
            Source = source;
            this.index = index;
            this.isDuplicate = isDuplicate;
        }

        public ThemePanel(int index, bool isDuplicate = false)
        {
            this.index = index;
            this.isDuplicate = isDuplicate;
        }

        #endregion

        #region Values

        #region UI

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
        /// The icon of the theme panel.
        /// </summary>
        public Image IconImage { get; set; }

        #endregion

        #region Data

        public ObjectSource Source { get; set; }

        /// <summary>
        /// The original ID from the theme.
        /// </summary>
        public string OriginalID { get; set; }

        public override string DisplayName => isFolder ? System.IO.Path.GetFileName(Path) : Source switch
        {
            ObjectSource.Internal => Item?.name,
            ObjectSource.External => !Item ? string.Empty : $"{Item.name} [ ID: {Item.id} ]",
            _ => string.Empty
        };

        const string TOOLTIP_COLOR_BAR = "▓";

        /// <summary>
        /// If the theme is a default theme (e.g. PA Machine, PA Anarchy, etc)
        /// </summary>
        public bool isDefault;

        /// <summary>
        /// If the theme is a duplicate (or a theme with the ID already exists)
        /// </summary>
        public bool isDuplicate;

        public bool Selected { get; set; }

        #endregion

        #endregion

        #region Functions

        public override void Init(string directory)
        {
            var gameObject = GameObject;
            if (gameObject)
                CoreHelper.Destroy(gameObject);

            var gameObjectFolder = EditorManager.inst.spriteFolderButtonPrefab.Duplicate(RTThemeEditor.inst.Popup.Content, $"Folder [{System.IO.Path.GetFileName(directory)}]");
            gameObjectFolder.transform.AsRT().sizeDelta = new Vector2(600f, 32f);
            var folderButtonStorageFolder = gameObjectFolder.GetComponent<SpriteFunctionButtonStorage>();
            var folderButtonFunctionFolder = gameObjectFolder.AddComponent<FolderButtonFunction>();

            var hoverUIFolder = gameObjectFolder.AddComponent<HoverUI>();
            hoverUIFolder.size = EditorConfig.Instance.OpenLevelButtonHoverSize.Value;
            hoverUIFolder.animatePos = false;
            hoverUIFolder.animateSca = true;

            folderButtonStorageFolder.OnClick.ClearAll();

            GameObject = gameObjectFolder;
            Path = directory;
            isFolder = true;
            Label = folderButtonStorageFolder.label;
            Button = folderButtonFunctionFolder;
            IconImage = folderButtonStorageFolder.image;

            EditorThemeManager.ApplySelectable(folderButtonStorageFolder.button, ThemeGroup.List_Button_2);
            EditorThemeManager.ApplyGraphic(folderButtonStorageFolder.label, ThemeGroup.List_Button_2_Text);

            Render();
            SetActive(false);
        }

        public override void Init(BeatmapTheme beatmapTheme)
        {
            var gameObject = GameObject;
            if (gameObject)
                CoreHelper.Destroy(gameObject);

            switch (Source)
            {
                case ObjectSource.Internal: {
                        gameObject = EventEditor.inst.ThemePanel.Duplicate(RTThemeEditor.inst.Dialog.Content, "theme-panel", index + 1);

                        var storage = gameObject.GetComponent<ThemePanelStorage>();

                        GameObject = gameObject;
                        UseButton = storage.button;
                        Button = gameObject.AddComponent<FolderButtonFunction>();
                        EditButton = storage.edit;
                        DeleteButton = storage.delete;
                        Label = storage.text;
                        BaseImage = storage.baseImage;

                        Item = beatmapTheme;
                        Item.themePanel = this;
                        isDefault = beatmapTheme.isDefault;
                        OriginalID = beatmapTheme.id;

                        Colors.Add(storage.color1);
                        Colors.Add(storage.color2);
                        Colors.Add(storage.color3);
                        Colors.Add(storage.color4);

                        EditorThemeManager.ApplyGraphic(BaseImage, ThemeGroup.List_Button_2_Normal, true);
                        EditorThemeManager.ApplyGraphic(UseButton.image, ThemeGroup.Null, true);
                        EditorThemeManager.ApplyGraphic(EditButton.image, ThemeGroup.List_Button_2_Text);
                        EditorThemeManager.ApplyGraphic(Label, ThemeGroup.List_Button_2_Text);
                        EditorThemeManager.ApplySelectable(DeleteButton, ThemeGroup.Delete_Keyframe_Button, false);

                        TooltipHelper.AssignTooltip(gameObject, "Internal Theme List Button");

                        break;
                    }
                case ObjectSource.External: {
                        var name = beatmapTheme.name;
                        gameObject = RTThemeEditor.inst.themePopupPanelPrefab.Duplicate(RTThemeEditor.inst.Popup.Content, name);
                        gameObject.transform.AsRT().sizeDelta = new Vector2(600f, 362f);
                        gameObject.transform.localScale = Vector3.one;

                        var viewThemeStorage = gameObject.GetComponent<ViewThemePanelStorage>();
                        viewThemeStorage.text.text = $"{name} [ ID: {beatmapTheme.id} ]";

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

                        var button = gameObject.GetComponent<Button>();

                        var convert = viewThemeStorage.convertButton;
                        var convertStorage = convert.GetComponent<FunctionButtonStorage>();
                        convert.onClick.NewListener(() => RTThemeEditor.inst.ConvertTheme(beatmapTheme));

                        var delete = viewThemeStorage.deleteButton;
                        delete.OnClick.NewListener(() => RTThemeEditor.inst.DeleteTheme(this));

                        EditorThemeManager.ApplyLightText(viewThemeStorage.baseColorsText);
                        EditorThemeManager.ApplyLightText(viewThemeStorage.playerColorsText);
                        EditorThemeManager.ApplyLightText(viewThemeStorage.objectColorsText);
                        EditorThemeManager.ApplyLightText(viewThemeStorage.backgroundColorsText);
                        EditorThemeManager.ApplyLightText(viewThemeStorage.effectColorsText);

                        EditorThemeManager.ApplySelectable(button, ThemeGroup.List_Button_1);
                        EditorThemeManager.ApplyLightText(viewThemeStorage.text);
                        EditorThemeManager.ApplySelectable(convert, ThemeGroup.Function_2);
                        EditorThemeManager.ApplyGraphic(convertStorage.label, ThemeGroup.Function_2_Text);
                        EditorThemeManager.ApplyDeleteButton(delete);

                        button.onClick.ClearAll();

                        convert.gameObject.SetActive(true);

                        GameObject = gameObject;
                        Button = gameObject.GetOrAddComponent<FolderButtonFunction>();
                        Label = viewThemeStorage.text;

                        Item = beatmapTheme;
                        Item.themePanel = this;
                        isDefault = beatmapTheme.isDefault;
                        OriginalID = beatmapTheme.id;

                        TooltipHelper.AssignTooltip(gameObject, "External Theme List Button");

                        break;
                    }
            }

            Render();
            SetActive(false);
        }

        public override void Render()
        {
            RenderIcon();
            RenderLabel();
            RenderTooltip();

            if (isFolder)
            {
                var directory = Path;
                var path = RTFile.ReplaceSlash(directory);
                Label.text = System.IO.Path.GetFileName(directory);
                Button.onClick = eventData =>
                {
                    if (!path.Contains(RTEditor.inst.BeatmapsPath + "/"))
                    {
                        EditorManager.inst.DisplayNotification($"Path does not contain the proper directory.", 2f, EditorManager.NotificationType.Warning);
                        return;
                    }

                    if (eventData.button == PointerEventData.InputButton.Right)
                    {
                        EditorContextMenu.inst.ShowContextMenu(EditorContextMenu.GetFolderPanelFunctions(this, RenderIcon,
                            onOpenFolder: () =>
                            {
                                RTThemeEditor.inst.Popup.PathField.text = path.Remove(RTEditor.inst.BeatmapsPath + "/");
                                RTEditor.inst.UpdateThemePath(false);
                            },
                            onFolderUpdate: () => RTEditor.inst.UpdateThemePath(true),
                            paste: RTThemeEditor.inst.PasteTheme));
                        return;
                    }

                    RTThemeEditor.inst.Popup.PathField.text = path.Remove(RTEditor.inst.BeatmapsPath + "/");
                    RTEditor.inst.UpdateThemePath(false);
                };

                return;
            }

            if (!string.IsNullOrEmpty(Item.filePath))
                Path = RTFile.ReplaceSlash(Item.filePath);

            if (UseButton)
                UseButton.onClick.NewListener(Use);
            Button.onClick = pointerEventData =>
            {
                if (pointerEventData.button != PointerEventData.InputButton.Right)
                {
                    Use();
                    return;
                }

                switch (Source)
                {
                    case ObjectSource.Internal: {
                        EditorContextMenu.inst.ShowContextMenu(
                            new ButtonElement("Use", Use),
                            new ButtonElement("Edit", () => RTThemeEditor.inst.RenderThemeEditor(Item)),
                            new ButtonElement("Export", () => RTThemeEditor.inst.ExportTheme(Item), "Internal Theme Export"),
                            new ButtonElement("Convert to VG", () => RTThemeEditor.inst.ConvertTheme(Item)),
                            new SpacerElement(),
                            new ButtonElement("Create theme", RTThemeEditor.inst.RenderThemeEditor),
                            new SpacerElement(),
                            new ButtonElement("Delete", () =>
                            {
                                if (!isDefault)
                                    RTThemeEditor.inst.DeleteTheme(this);
                                else
                                    EditorManager.inst.DisplayNotification("Cannot delete a default theme!", 2f, EditorManager.NotificationType.Warning);
                            }),
                            new ButtonElement("Clear Themes", RTThemeEditor.inst.ClearInternalThemes),
                            new ButtonElement("Remove Unused Themes", RTThemeEditor.inst.RemoveUnusedThemes, "Internal Remove Unused Themes"),
                            new SpacerElement(),
                            new ButtonElement("Shuffle ID", () => RTThemeEditor.inst.ShuffleThemeID(Item)),
                            new SpacerElement(),
                            new ButtonElement("Add to Prefab", () =>
                            {
                                RTPrefabEditor.inst.OpenPopup();
                                RTPrefabEditor.inst.onSelectPrefab = prefabPanel =>
                                {
                                    if (!Item || !prefabPanel.Item)
                                    {
                                        EditorManager.inst.DisplayNotification($"Failed to add the theme to a prefab due to a null error.", 2f, EditorManager.NotificationType.Error);
                                        CoreHelper.Log($"Theme is null: {!Item}\n" +
                                            $"Prefab is null: {!prefabPanel.Item}");
                                        return;
                                    }

                                    if (prefabPanel.Item.AddTheme(Item))
                                    {
                                        if (prefabPanel.IsExternal)
                                            RTPrefabEditor.inst.UpdatePrefabFile(prefabPanel);
                                        EditorManager.inst.DisplayNotification($"Added theme {Item} to the prefab.", 2f, EditorManager.NotificationType.Success);
                                    }
                                    else
                                    {
                                        RTEditor.inst.ShowWarningPopup("Theme already exists in the Prefab! Do you wish to overwrite it?",
                                            onConfirm: () =>
                                            {
                                                prefabPanel.Item.OverwriteTheme(Item);
                                                if (prefabPanel.IsExternal)
                                                    RTPrefabEditor.inst.UpdatePrefabFile(prefabPanel);
                                                EditorManager.inst.DisplayNotification($"Updated theme {Item} in the prefab.", 2f, EditorManager.NotificationType.Success);
                                            },
                                            confirm: "Overwrite",
                                            cancel: "Cancel");
                                    }
                                };
                            })
                            );
                            break;
                        }
                    case ObjectSource.External: {
                        EditorContextMenu.inst.ShowContextMenu(
                            new ButtonElement("Import", Use, "External Theme Import"),
                            new ButtonElement("Update", () =>
                            {
                                if (!GameData.Current.UpdateTheme(Item))
                                    EditorManager.inst.DisplayNotification($"No theme was found to update!", 2f, EditorManager.NotificationType.Warning);
                                else
                                    RTThemeEditor.inst.LoadInternalThemes();
                            }, "External Theme Update"),
                            new ButtonElement("Convert to VG", () => RTThemeEditor.inst.ConvertTheme(Item)),
                            new SpacerElement(),
                            new ButtonElement("Create folder", () => RTEditor.inst.ShowFolderCreator(RTFile.CombinePaths(RTEditor.inst.BeatmapsPath, RTEditor.inst.ThemePath), () => { RTEditor.inst.UpdateThemePath(true); RTEditor.inst.HideNameEditor(); })),
                            new SpacerElement(),
                            new ButtonElement("Cut", () =>
                            {
                                if (isDefault)
                                {
                                    EditorManager.inst.DisplayNotification($"Cannot cut a default theme!", 1.5f, EditorManager.NotificationType.Warning);
                                    return;
                                }

                                RTThemeEditor.inst.shouldCutTheme = true;
                                RTThemeEditor.inst.copiedThemePath = Item.filePath;
                                EditorManager.inst.DisplayNotification($"Cut {Item.name}!", 1.5f, EditorManager.NotificationType.Success);
                                CoreHelper.Log($"Cut theme: {RTThemeEditor.inst.copiedThemePath}");
                            }),
                            new ButtonElement("Copy", () =>
                            {
                                if (isDefault)
                                {
                                    EditorManager.inst.DisplayNotification($"Cannot copy a default theme!", 1.5f, EditorManager.NotificationType.Warning);
                                    return;
                                }

                                RTThemeEditor.inst.shouldCutTheme = false;
                                RTThemeEditor.inst.copiedThemePath = Item.filePath;
                                EditorManager.inst.DisplayNotification($"Copied {Item.name}!", 1.5f, EditorManager.NotificationType.Success);
                                CoreHelper.Log($"Copied theme: {RTThemeEditor.inst.copiedThemePath}");
                            }),
                            new ButtonElement("Paste", RTThemeEditor.inst.PasteTheme),
                            new ButtonElement("Delete", () =>
                            {
                                if (!isDefault)
                                    RTThemeEditor.inst.DeleteTheme(this);
                                else
                                    EditorManager.inst.DisplayNotification("Cannot delete a default theme!", 2f, EditorManager.NotificationType.Warning);
                            }),
                            new SpacerElement(),
                            new ButtonElement("Shuffle ID", () => RTThemeEditor.inst.ShuffleThemeID(Item))
                            );
                            break;
                        }
                }
            };

            if (Source == ObjectSource.External)
                return;

            RenderColors();

            EditButton.onClick.NewListener(() => RTThemeEditor.inst.RenderThemeEditor(Item));
            DeleteButton.interactable = !isDefault;
            DeleteButton.onClick.NewListener(() => RTThemeEditor.inst.DeleteTheme(this));
        }

        /// <summary>
        /// Renders the folder button's icon.
        /// </summary>
        public void RenderIcon()
        {
            if (!isFolder || !IconImage)
                return;

            IconImage.sprite = RTFile.FileExists(RTFile.CombinePaths(Path, $"folder_icon{FileFormat.PNG.Dot()}")) ?
                SpriteHelper.LoadSprite(RTFile.CombinePaths(Path, $"folder_icon{FileFormat.PNG.Dot()}")) :
                EditorSprites.OpenSprite;
        }

        public override void RenderLabel(string name) => Label.text = name;

        public override void RenderTooltip()
        {
            if (isFolder)
            {
                GetFolderTooltip();
                return;
            }

            try
            {
                var sb = new StringBuilder();
                sb.AppendLine("Background Color:");
                sb.AppendLine($"<#{RTColors.ColorToHexOptional(Item.backgroundColor)}>{TOOLTIP_COLOR_BAR}</color>");

                sb.AppendLine("GUI Color | GUI Accent Color");
                sb.AppendLine($"<#{RTColors.ColorToHexOptional(Item.guiColor)}>{TOOLTIP_COLOR_BAR}</color> " + $"<#{RTColors.ColorToHexOptional(Item.guiAccentColor)}>{TOOLTIP_COLOR_BAR}</color>");

                sb.AppendLine("Player Colors");
                var playerColors = string.Empty;
                for (int i = 0; i < Mathf.Clamp(Item.playerColors.Count, 0, 4); i++)
                    playerColors += $"<#{RTColors.ColorToHexOptional(Item.GetPlayerColor(i))}>{TOOLTIP_COLOR_BAR}</color>";
                sb.AppendLine(playerColors);

                sb.AppendLine("Beatmap Object Colors:");
                var objectColors = string.Empty;
                for (int i = 0; i < Mathf.Clamp(Item.objectColors.Count, 0, 18); i++)
                    objectColors += $"<#{RTColors.ColorToHexOptional(Item.GetObjColor(i))}>{TOOLTIP_COLOR_BAR}</color>";
                sb.AppendLine(objectColors);

                sb.AppendLine("BG Object Colors:");
                var bgColors = string.Empty;
                for (int i = 0; i < Mathf.Clamp(Item.backgroundColors.Count, 0, 9); i++)
                    bgColors += $"<#{RTColors.ColorToHexOptional(Item.GetBGColor(i))}>{TOOLTIP_COLOR_BAR}</color>";
                sb.AppendLine(bgColors);

                sb.AppendLine("Effect Colors:");
                var fxColors = string.Empty;
                for (int i = 0; i < Mathf.Clamp(Item.effectColors.Count, 0, 18); i++)
                    fxColors += $"<#{RTColors.ColorToHexOptional(Item.GetFXColor(i))}>{TOOLTIP_COLOR_BAR}</color>";
                sb.AppendLine(fxColors);

                TooltipHelper.AddHoverTooltip(GameObject, Item.name, sb.ToString(), clear: true);
            }
            catch (System.Exception ex)
            {
                CoreHelper.LogError($"Had an exception with trying to add info to the {nameof(ThemePanel)}.\nGameObject: {GameObject}\nFilePath: {Path}\nException: {ex}");
            }
        }

        /// <summary>
        /// Renders the theme panel colors.
        /// </summary>
        public void RenderColors()
        {
            if (Source == ObjectSource.External)
                return;

            for (int j = 0; j < Colors.Count; j++)
                Colors[j].color = Item.GetObjColor(j);
        }

        /// <summary>
        /// Renders a color of the theme panel.
        /// </summary>
        /// <param name="i">Color index.</param>
        public void RenderColor(int i) => RenderColor(i, Item.GetObjColor(i));

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
        /// Uses the theme for the current selected theme keyframe.
        /// </summary>
        public void Use()
        {
            switch (Source)
            {
                case ObjectSource.Internal: {
                        if (isDuplicate)
                        {
                            var array = RTThemeEditor.inst.InternalThemePanels.Where(x => x.Item && x.Item.id == Item.id).Select(x => x.Item.name).ToArray();
                            var str = RTString.ArrayToString(array);

                            EditorManager.inst.DisplayNotification($"Unable to use Theme [{Item.name}] due to conflicting themes: {str}.", 2f * array.Length, EditorManager.NotificationType.Error);
                            return;
                        }

                        RTEventEditor.inst.SetKeyframeValue(0, Parser.TryParse(Item.id, 0));
                        RTThemeEditor.inst.RenderThemePreview();

                        break;
                    }
                case ObjectSource.External: {
                        RTThemeEditor.inst.ImportTheme(Item);
                        break;
                    }
            }
        }

        public override int GetIndex() => Source switch
        {
            ObjectSource.Internal => GameData.Current.beatmapThemes.FindIndex(x => x.id == Item.id),
            ObjectSource.External => index,
            _ => -1,
        };

        public override string ToString() => isFolder ? Name : Item?.ToString();

        #endregion
    }
}
