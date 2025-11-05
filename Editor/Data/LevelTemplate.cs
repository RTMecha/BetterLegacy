using System.Collections.Generic;
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
using SimpleJSON;

namespace BetterLegacy.Editor.Data
{
    /// <summary>
    /// Handles a level template that new levels are based on.
    /// </summary>
    public class LevelTemplate : Exists
    {
        public LevelTemplate() { }

        public LevelTemplate(string name) => this.name = name;

        #region Values

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

        /// <summary>
        /// Metadata of the level template.
        /// </summary>
        public LevelTemplateInfo info;

        #endregion

        #region Functions

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
                CoreHelper.Delete(gameObject);

            gameObject = LevelTemplateEditor.inst.newLevelTemplatePrefab.Duplicate(LevelTemplateEditor.inst.Dialog.Content);
            var previewBase = gameObject.transform.Find("Preview Base");

            GameObject = gameObject;
            PreviewImage = previewBase.Find("Preview").GetComponent<Image>();
            Title = gameObject.transform.Find("Title").GetComponent<Text>();
            DeleteButton = gameObject.transform.Find("Delete").GetComponent<DeleteButtonStorage>();

            var isDefault = string.IsNullOrEmpty(directory);
            DeleteButton.gameObject.SetActive(!isDefault);
            if (!isDefault)
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

            var button = gameObject.GetComponent<Button>();
            button.onClick.NewListener(SelectTemplate);

            EditorThemeManager.ApplySelectable(button, ThemeGroup.List_Button_1);
            EditorThemeManager.ApplyGraphic(previewBase.GetComponent<Image>(), ThemeGroup.Null, true);
            EditorThemeManager.ApplyLightText(Title);

            EditorThemeManager.ApplyGraphic(DeleteButton.baseImage, ThemeGroup.Delete);
            EditorThemeManager.ApplyGraphic(DeleteButton.image, ThemeGroup.Delete_Text);

            try
            {
                if (RTFile.TryReadFromFile(RTFile.CombinePaths(Directory, "info.lsb"), out string metadata))
                    info = LevelTemplateInfo.Parse(JSON.Parse(metadata));
            }
            catch (System.Exception ex)
            {
                CoreHelper.LogError($"Could not load template due to the exception: {ex}");
            }

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
        public void RenderTitle() => Title.text = $"{name}{(LevelTemplateEditor.inst.currentLevelTemplate == index ? " [SELECTED]" : string.Empty)}";

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

    public class LevelTemplateInfo : PAObject<LevelTemplateInfo>, IUploadable, IFile
    {
        #region Values

        public string name;
        public string description;

        #region Server

        public string ServerID { get; set; }

        public string UploaderName { get; set; }

        public string UploaderID { get; set; }

        public List<ServerUser> Uploaders { get; set; } = new List<ServerUser>();

        public ServerVisibility Visibility { get; set; }

        public string Changelog { get; set; }

        public List<string> ArcadeTags { get; set; } = new List<string>();

        public string ObjectVersion { get; set; }

        public string DatePublished { get; set; }

        public int VersionNumber { get; set; }

        #endregion

        public FileFormat FileFormat => FileFormat.LSB;

        #endregion

        #region Functions

        /// <summary>
        /// Gets the name of the file.
        /// </summary>
        /// <returns>Returns "info.lsb".</returns>
        public string GetFileName() => "info" + FileFormat.Dot();

        public void ReadFromFile(string path)
        {
            if (string.IsNullOrEmpty(path))
                return;

            if (!path.EndsWith(FileFormat.Dot()))
                path = path += FileFormat.Dot();

            var file = RTFile.ReadFromFile(path);
            if (string.IsNullOrEmpty(file))
                return;

            ReadJSON(JSON.Parse(file));
        }

        public void WriteToFile(string path)
        {
            if (string.IsNullOrEmpty(path))
                return;

            var jn = ToJSON();
            RTFile.WriteToFile(path, jn.ToString());
        }

        public override void CopyData(LevelTemplateInfo orig, bool newID = true)
        {
            name = orig.name;
            description = orig.description;
            this.CopyUploadableData(orig);
        }

        public override void ReadJSON(JSONNode jn)
        {
            if (jn["name"] != null)
                name = jn["name"];
            if (jn["desc"] != null)
                description = jn["desc"];
            this.ReadUploadableJSON(jn);
        }

        public override JSONNode ToJSON()
        {
            var jn = Parser.NewJSONObject();

            if (!string.IsNullOrEmpty(name))
                jn["name"] = name;
            if (!string.IsNullOrEmpty(description))
                jn["desc"] = description;
            this.WriteUploadableJSON(jn);

            return jn;
        }

        #endregion
    }
}
