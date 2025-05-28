using System.IO;

using UnityEngine;
using UnityEngine.UI;

using BetterLegacy.Core;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Data.Level;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Prefabs;
using BetterLegacy.Editor.Managers;

namespace BetterLegacy.Editor.Data
{
    /// <summary>
    /// Handles a level template that new levels are based on.
    /// </summary>
    public class LevelTemplate : Exists
    {
        public LevelTemplate() { }

        public LevelTemplate(string name) => this.name = name;

        #region Properties

        /// <summary>
        /// The template game object.
        /// </summary>
        public GameObject GameObject { get; set; }

        /// <summary>
        /// The name UI of the level template.
        /// </summary>
        public Text Title { get; set; }

        /// <summary>
        /// The preview image of the level template.
        /// </summary>
        public Image PreviewImage { get; set; }

        /// <summary>
        /// The delete button of the level template.
        /// </summary>
        public DeleteButtonStorage DeleteButton { get; set; }

        /// <summary>
        /// Directory that contains the level template files.
        /// </summary>
        public string Directory { get; set; }

        #endregion

        #region Fields

        /// <summary>
        /// Path to level template file.
        /// </summary>
        public string path;

        /// <summary>
        /// The loaded preview thumbnail.
        /// </summary>
        public Sprite preview;

        /// <summary>
        /// Name of the level template.
        /// </summary>
        public string name;

        /// <summary>
        /// Index of the level template.
        /// </summary>
        public int index;

        #endregion

        #region Methods

        /// <summary>
        /// Initializes the level template UI.
        /// </summary>
        public void Init(int index, string directory)
        {
            Directory = directory;
            this.index = index;
            if (!string.IsNullOrEmpty(directory))
                name = Path.GetFileName(RTFile.RemoveEndSlash(Directory));

            var gameObject = GameObject;
            if (gameObject)
                CoreHelper.Destroy(gameObject);

            gameObject = LevelTemplateEditor.inst.newLevelTemplatePrefab.Duplicate(LevelTemplateEditor.inst.Dialog.Content);
            var previewBase = gameObject.transform.Find("Preview Base");

            GameObject = gameObject;
            PreviewImage = previewBase.Find("Preview").GetComponent<Image>();
            Title = gameObject.transform.Find("Title").GetComponent<Text>();
            DeleteButton = gameObject.transform.Find("Delete").GetComponent<DeleteButtonStorage>();

            var isDefault = string.IsNullOrEmpty(directory);
            DeleteButton.gameObject.SetActive(!isDefault);
            if (!isDefault)
            {
                DeleteButton.button.onClick.NewListener(() =>
                {
                    RTEditor.inst.ShowWarningPopup("Are you sure you want to delete this template? This is permanent!", () =>
                    {
                        RTFile.DeleteDirectory(Directory);
                        LevelTemplateEditor.inst.RenderLevelTemplates();
                        EditorManager.inst.DisplayNotification("Successfully deleted the template.", 2f, EditorManager.NotificationType.Success);
                        RTEditor.inst.HideWarningPopup();
                    }, RTEditor.inst.HideWarningPopup);
                });
            }

            var button = gameObject.GetComponent<Button>();
            button.onClick.NewListener(SelectTemplate);

            EditorThemeManager.ApplySelectable(button, ThemeGroup.List_Button_1);
            EditorThemeManager.ApplyGraphic(previewBase.GetComponent<Image>(), ThemeGroup.Null, true);
            EditorThemeManager.ApplyLightText(Title);

            EditorThemeManager.ApplyGraphic(DeleteButton.baseImage, ThemeGroup.Delete);
            EditorThemeManager.ApplyGraphic(DeleteButton.image, ThemeGroup.Delete_Text);

            Render();
        }

        /// <summary>
        /// Renders the whole level template UI.
        /// </summary>
        public void Render()
        {
            RenderTitle();
            RenderPreview();
        }

        /// <summary>
        /// Renders the level templates' title and selection status.
        /// </summary>
        public void RenderTitle() => Title.text = $"{name}{(LevelTemplateEditor.inst.currentLevelTemplate == index ? " [SELECTED]" : "")}";

        /// <summary>
        /// Renders the level templates' preview.
        /// </summary>
        public void RenderPreview() => RenderPreview(RTFile.FileExists(RTFile.CombinePaths(Directory, "preview.png")) ? SpriteHelper.LoadSprite(RTFile.CombinePaths(Directory, "preview.png")) : null);

        /// <summary>
        /// Renders the level templates' preview.
        /// </summary>
        /// <param name="preview">Preview to render.</param>
        public void RenderPreview(Sprite preview)
        {
            var noPreview = !preview;

            PreviewImage.color = new Color(1f, 1f, 1f, noPreview ? 0.1f : 1f);
            PreviewImage.sprite = noPreview ? LevelTemplateEditor.inst.newLevelTemplateBaseSprite : preview;
            GameObject.transform.Find("No Preview").gameObject.SetActive(noPreview);
        }

        /// <summary>
        /// Selects this level template as the current.
        /// </summary>
        public void SelectTemplate() => LevelTemplateEditor.inst.SelectTemplate(index);

        /// <summary>
        /// Loads the level templates' game data.
        /// </summary>
        /// <returns>Returns a loaded game data from the template.</returns>
        public GameData GetGameData() =>
            !RTFile.FileExists(RTFile.CombinePaths(Directory, Level.LEVEL_LSB)) ?
                EditorLevelManager.inst.CreateBaseBeatmap() :
                GameData.ReadFromFile(RTFile.CombinePaths(Directory, Level.LEVEL_LSB), ArrhythmiaType.LS);

        #endregion
    }
}
