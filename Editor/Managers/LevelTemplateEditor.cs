using System;
using System.Collections.Generic;
using System.IO;

using UnityEngine;
using UnityEngine.UI;

using BetterLegacy.Core;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Data.Level;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Prefabs;
using BetterLegacy.Editor.Data;
using BetterLegacy.Editor.Data.Dialogs;

namespace BetterLegacy.Editor.Managers
{
    /// <summary>
    /// Manages Level Templates new levels are created by.
    /// </summary>
    public class LevelTemplateEditor : MonoBehaviour
    {
        #region Init

        /// <summary>
        /// The <see cref="LevelTemplateEditor"/> global instance reference.
        /// </summary>
        public static LevelTemplateEditor inst;

        /// <summary>
        /// Initializes <see cref="LevelTemplateEditor"/>.
        /// </summary>
        public static void Init() => Creator.NewGameObject(nameof(LevelTemplateEditor), EditorManager.inst.transform.parent).AddComponent<LevelTemplateEditor>();

        void Awake()
        {
            inst = this;

            try
            {
                Dialog = new LevelTemplateEditorDialog();
                Dialog.Init();

                #region Prefabs

                newLevelTemplatePrefab = EditorManager.inst.folderButtonPrefab.Duplicate(transform, "Template");

                newLevelTemplatePrefab.transform.AsRT().sizeDelta = new Vector2(734f, 200f);

                var newLevelTemplatePrefabPreviewBase = Creator.NewUIObject("Preview Base", newLevelTemplatePrefab.transform);
                newLevelTemplatePrefabPreviewBase.AddComponent<Image>();
                newLevelTemplatePrefabPreviewBase.AddComponent<Mask>().showMaskGraphic = false;

                newLevelTemplatePrefabPreviewBase.transform.AsRT().anchoredPosition = new Vector2(-200f, 0f);
                newLevelTemplatePrefabPreviewBase.transform.AsRT().sizeDelta = new Vector2(312f, 175.5f);

                var newLevelTemplatePrefabPreview = Creator.NewUIObject("Preview", newLevelTemplatePrefabPreviewBase.transform);
                newLevelTemplatePrefabPreview.AddComponent<Image>();
                RectValues.FullAnchored.AssignToRectTransform(newLevelTemplatePrefabPreview.transform.AsRT());

                var newLevelTemplatePrefabTitle = newLevelTemplatePrefab.transform.GetChild(0);
                newLevelTemplatePrefabTitle.name = "Title";
                newLevelTemplatePrefabTitle.AsRT().anchoredPosition = new Vector2(350f, 0f);
                newLevelTemplatePrefabTitle.AsRT().sizeDelta = new Vector2(32f, 32f);

                var noLevel = newLevelTemplatePrefabTitle.gameObject.Duplicate(newLevelTemplatePrefab.transform, "No Preview");
                noLevel.transform.AsRT().anchoredPosition = new Vector2(-200f, 0f);
                noLevel.transform.AsRT().sizeDelta = new Vector2(32f, 32f);
                var noLevelText = noLevel.GetComponent<Text>();
                noLevelText.alignment = TextAnchor.MiddleCenter;
                noLevelText.fontSize = 20;
                noLevelText.text = "No Preview";
                noLevel.SetActive(false);

                CoroutineHelper.StartCoroutine(AlephNetwork.DownloadImageTexture($"{RTFile.ApplicationDirectory}{RTFile.BepInExAssetsPath}default_template.png", texture2D => newLevelTemplateBaseSprite = SpriteHelper.CreateSprite(texture2D)));

                var delete = EditorPrefabHolder.Instance.DeleteButton.Duplicate(newLevelTemplatePrefab.transform, "Delete");
                RectValues.Default.AnchoredPosition(335f, 75f).SizeDelta(32f, 32f).AssignToRectTransform(delete.transform.AsRT());

                #endregion

            }
            catch (Exception ex)
            {
                CoreHelper.LogException(ex);
            } // init dialog
        }

        #endregion

        #region Values

        /// <summary>
        /// Level Template Editor dialog.
        /// </summary>
        public LevelTemplateEditorDialog Dialog { get; set; }

        /// <summary>
        /// If a new Level Template is being chosen in the Level list.
        /// </summary>
        public bool choosingLevelTemplate;

        /// <summary>
        /// The currently selected level template index. If value is out of the range of <see cref="LevelTemplates"/>, gets <see cref="defaultLevelTemplate"/>.
        /// </summary>
        public int currentLevelTemplate = -1;

        /// <summary>
        /// Level Template panel prefab.
        /// </summary>
        public GameObject newLevelTemplatePrefab;

        /// <summary>
        /// Default template preview.
        /// </summary>
        public Sprite newLevelTemplateBaseSprite;

        /// <summary>
        /// The currently selected template preview.
        /// </summary>
        public Sprite currentTemplateSprite;

        /// <summary>
        /// The default level template.
        /// </summary>
        public LevelTemplate defaultLevelTemplate = new LevelTemplate("Default");

        /// <summary>
        /// List of all custom level templates.
        /// </summary>
        public List<LevelTemplate> LevelTemplates { get; set; } = new List<LevelTemplate>();

        /// <summary>
        /// The currently selected Level Template.
        /// </summary>
        public LevelTemplate CurrentTemplate => LevelTemplates.TryGetAt(currentLevelTemplate, out LevelTemplate levelTemplate) ? levelTemplate : defaultLevelTemplate;

        #endregion

        #region Methods

        /// <summary>
        /// Creates a new level template from an existing level.
        /// </summary>
        /// <param name="file">Level folder to use.</param>
        public void CreateTemplate(string file)
        {
            if (string.IsNullOrEmpty(Dialog.NameField.text))
            {
                EditorManager.inst.DisplayNotification($"Level template name is empty. Name it something unique via the input field in the Level Template editor.", 3f, EditorManager.NotificationType.Error);
                return;
            }

            if (string.IsNullOrEmpty(file))
            {
                EditorManager.inst.DisplayNotification($"Path is empty.", 3f, EditorManager.NotificationType.Error);
                return;
            }

            if (!file.Contains(Level.LEVEL_LSB))
                file = RTFile.CombinePaths(file, Level.LEVEL_LSB);

            if (!RTFile.FileExists(file))
            {
                EditorManager.inst.DisplayNotification($"Level file doesn't exist.", 3f, EditorManager.NotificationType.Error);
                return;
            }

            EditorLevelManager.inst.OpenLevelPopup.Close();

            RTEditor.inst.ShowWarningPopup("Are you sure you want to make a new level template?", () =>
            {
                choosingLevelTemplate = false;

                var copyTo = RTFile.CombinePaths(RTFile.ApplicationDirectory, "beatmaps/templates", RTFile.ValidateDirectory(Dialog.NameField.text));

                RTFile.CreateDirectory(copyTo);
                RTFile.CopyFile(file, RTFile.CombinePaths(copyTo, Level.LEVEL_LSB));

                if (currentTemplateSprite)
                    currentTemplateSprite.Save(RTFile.CombinePaths(copyTo, $"preview{FileFormat.PNG.Dot()}"));

                RenderLevelTemplates();
                RTEditor.inst.HideWarningPopup();
            }, () =>
            {
                EditorLevelManager.inst.OpenLevelPopup.Open();
                RTEditor.inst.HideWarningPopup();
            });
        }

        /// <summary>
        /// Renders the Level Templates list.
        /// </summary>
        public void RenderLevelTemplates()
        {
            LevelTemplates.Clear();
            Dialog.ClearContent();
            defaultLevelTemplate.Init(-1, null);

            var baseDirectory = $"{RTFile.ApplicationDirectory}beatmaps/templates";

            if (!RTFile.DirectoryExists(baseDirectory))
                return;

            int num = 0;
            var directories = Directory.GetDirectories(baseDirectory, "*", SearchOption.TopDirectoryOnly);
            for (int i = 0; i < directories.Length; i++)
            {
                var directory = directories[i];

                if (!RTFile.FileExists(RTFile.CombinePaths(directory, Level.LEVEL_LSB)))
                    continue;

                var template = new LevelTemplate();
                template.Init(i, RTFile.ReplaceSlash(directory));

                LevelTemplates.Add(template);
                num++;
            }

            currentLevelTemplate = Mathf.Clamp(currentLevelTemplate, -1, LevelTemplates.Count - 1);
        }

        /// <summary>
        /// Selects a Level Template at an index.
        /// </summary>
        /// <param name="index">Index of the Level Template to select. If the index is out of range of the LevelTemplates list, selects the default level template.</param>
        public void SelectTemplate(int index)
        {
            currentLevelTemplate = index;
            EditorManager.inst.DisplayNotification($"Set level template to {CurrentTemplate.name} [{currentLevelTemplate}]", 2f, EditorManager.NotificationType.Success);

            RenderSelected();
        }

        /// <summary>
        /// Renders the title of all Level Templates. For rendering which template is selected.
        /// </summary>
        public void RenderSelected()
        {
            for (int i = 0; i < LevelTemplates.Count; i++)
                LevelTemplates[i].RenderTitle();
        }

        #endregion
    }
}
