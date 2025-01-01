using BetterLegacy.Configs;
using BetterLegacy.Core;
using BetterLegacy.Core.Components;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Prefabs;
using BetterLegacy.Editor.Components;
using BetterLegacy.Editor.Managers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BetterLegacy.Editor.Data
{
    /// <summary>
    /// Object for storing theme panel data. Used for the themes in the theme keyframe.
    /// </summary>
    public class ThemePanel
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

            var gameObjectFolder = EditorManager.inst.folderButtonPrefab.Duplicate(RTThemeEditor.inst.themeKeyframeContent, $"Folder [{Path.GetFileName(directory)}]", index + 2);
            var folderButtonStorageFolder = gameObjectFolder.GetComponent<FunctionButtonStorage>();
            var folderButtonFunctionFolder = gameObjectFolder.AddComponent<FolderButtonFunction>();

            var hoverUIFolder = gameObjectFolder.AddComponent<HoverUI>();
            hoverUIFolder.size = EditorConfig.Instance.OpenLevelButtonHoverSize.Value;
            hoverUIFolder.animatePos = false;
            hoverUIFolder.animateSca = true;

            folderButtonStorageFolder.button.onClick.ClearAll();

            GameObject = gameObjectFolder;
            FilePath = directory;
            isFolder = true;
            Name = folderButtonStorageFolder.text;
            FolderButton = folderButtonFunctionFolder;

            EditorThemeManager.ApplySelectable(folderButtonStorageFolder.button, ThemeGroup.List_Button_2);
            EditorThemeManager.ApplyGraphic(folderButtonStorageFolder.text, ThemeGroup.List_Button_2_Text);

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

            gameObject = EventEditor.inst.ThemePanel.Duplicate(RTThemeEditor.inst.themeKeyframeContent, "theme-panel", index + 2);

            var storage = gameObject.GetComponent<ThemePanelStorage>();

            GameObject = gameObject;
            UseButton = storage.button;
            ContextClickable = gameObject.AddComponent<ContextClickable>();
            EditButton = storage.edit;
            DeleteButton = storage.delete;
            Name = storage.text;
            BaseImage = storage.baseImage;

            Theme = beatmapTheme;
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
            RenderName();

            if (isFolder)
            {
                var directory = FilePath;
                var path = RTFile.ReplaceSlash(directory);
                Name.text = Path.GetFileName(directory);
                FolderButton.onClick = eventData =>
                {
                    if (!path.Contains(RTFile.ApplicationDirectory + "beatmaps/"))
                    {
                        EditorManager.inst.DisplayNotification($"Path does not contain the proper directory.", 2f, EditorManager.NotificationType.Warning);
                        return;
                    }

                    if (eventData.button == PointerEventData.InputButton.Right)
                    {
                        RTEditor.inst.ShowContextMenu(300f,
                            new ButtonFunction("Open folder", () =>
                            {
                                RTEditor.inst.themePathField.text = path.Replace(RTFile.ApplicationDirectory.Replace("\\", "/") + "beatmaps/", "");
                                RTEditor.inst.UpdateThemePath(false);
                            }),
                            new ButtonFunction("Create folder", () => RTEditor.inst.ShowFolderCreator($"{RTFile.ApplicationDirectory}{RTEditor.themeListPath}", () => { RTEditor.inst.UpdateThemePath(true); RTEditor.inst.HideNameEditor(); })),
                            new ButtonFunction("Create theme", () => RTThemeEditor.inst.RenderThemeEditor()),
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
                            }));

                        return;
                    }

                    RTEditor.inst.themePathField.text = path.Replace(RTFile.ApplicationDirectory.Replace("\\", "/") + "beatmaps/", "");
                    RTEditor.inst.UpdateThemePath(false);
                };

                return;
            }

            if (!string.IsNullOrEmpty(Theme.filePath))
                FilePath = RTFile.ReplaceSlash(Theme.filePath);

            for (int j = 0; j < Colors.Count; j++)
                Colors[j].color = Theme.GetObjColor(j);

            UseButton.onClick.ClearAll();
            UseButton.onClick.AddListener(() =>
            {
                if (isDuplicate)
                {
                    var array = DataManager.inst.CustomBeatmapThemes.Where(x => x.id == Theme.id).Select(x => x.name).ToArray();
                    var str = RTString.ArrayToString(array);

                    EditorManager.inst.DisplayNotification($"Unable to use Theme [{Theme.name}] due to conflicting themes: {str}.", 2f * array.Length, EditorManager.NotificationType.Error);
                    return;
                }

                if (RTEventEditor.inst.SelectedKeyframes.Count > 1 && RTEventEditor.inst.SelectedKeyframes.All(x => RTEventEditor.inst.SelectedKeyframes.Min(y => y.Type) == x.Type))
                {
                    foreach (var timelineObject in RTEventEditor.inst.SelectedKeyframes)
                        timelineObject.GetData<EventKeyframe>().eventValues[0] = Parser.TryParse(Theme.id, 0);
                }
                else
                    RTEventEditor.inst.CurrentSelectedKeyframe.eventValues[0] = Parser.TryParse(Theme.id, 0);

                EventManager.inst.updateEvents();
                EventEditor.inst.RenderThemePreview(RTThemeEditor.inst.themeKeyframe);
            });

            ContextClickable.onClick = eventData =>
            {
                if (eventData.button != PointerEventData.InputButton.Right)
                    return;

                RTEditor.inst.ShowContextMenu(400f,
                    new ButtonFunction("Use", () =>
                    {
                        if (isDuplicate)
                        {
                            var array = DataManager.inst.CustomBeatmapThemes.Where(x => x.id == Theme.id).Select(x => x.name).ToArray();
                            var str = RTString.ArrayToString(array);

                            EditorManager.inst.DisplayNotification($"Unable to use Theme [{Theme.name}] due to conflicting themes: {str}.", 2f * array.Length, EditorManager.NotificationType.Error);
                            return;
                        }

                        if (RTEventEditor.inst.SelectedKeyframes.Count > 1 && RTEventEditor.inst.SelectedKeyframes.All(x => RTEventEditor.inst.SelectedKeyframes.Min(y => y.Type) == x.Type))
                        {
                            foreach (var timelineObject in RTEventEditor.inst.SelectedKeyframes)
                                timelineObject.GetData<EventKeyframe>().eventValues[0] = Parser.TryParse(Theme.id, 0);
                        }
                        else
                            RTEventEditor.inst.CurrentSelectedKeyframe.eventValues[0] = Parser.TryParse(Theme.id, 0);

                        EventManager.inst.updateEvents();
                        EventEditor.inst.RenderThemePreview(RTThemeEditor.inst.themeKeyframe);
                    }),
                    new ButtonFunction("Edit", () => RTThemeEditor.inst.RenderThemeEditor(Parser.TryParse(Theme.id, 0))),
                    new ButtonFunction("Convert to VG", () => RTThemeEditor.inst.ConvertTheme(Theme)),
                    new ButtonFunction(true),
                    new ButtonFunction("Create folder", () => RTEditor.inst.ShowFolderCreator($"{RTFile.ApplicationDirectory}{RTEditor.themeListPath}", () => { RTEditor.inst.UpdateThemePath(true); RTEditor.inst.HideNameEditor(); })),
                    new ButtonFunction("Create theme", () => RTThemeEditor.inst.RenderThemeEditor()),
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
                            RTThemeEditor.inst.DeleteThemeDelegate(Theme);
                        else
                            EditorManager.inst.DisplayNotification("Cannot delete a default theme!", 2f, EditorManager.NotificationType.Warning);
                    }),
                    new ButtonFunction(true),
                    new ButtonFunction("Shuffle ID", () => RTThemeEditor.inst.ShuffleThemeID(Theme))
                    );
            };

            EditButton.onClick.ClearAll();
            EditButton.onClick.AddListener(() => RTThemeEditor.inst.RenderThemeEditor(Parser.TryParse(Theme.id, 0)));

            DeleteButton.onClick.ClearAll();
            DeleteButton.interactable = !isDefault;
            if (!isDefault)
                DeleteButton.onClick.AddListener(() => RTThemeEditor.inst.DeleteThemeDelegate(Theme));
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

        /// <summary>
        /// Sets the theme panel active state.
        /// </summary>
        /// <param name="active">Active state to set.</param>
        public void SetActive(bool active)
        {
            if (GameObject)
                GameObject.SetActive(active);
        }

        #endregion
    }
}
