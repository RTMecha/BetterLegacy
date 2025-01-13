using BetterLegacy.Core;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Data.Level;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using BetterLegacy.Core.Prefabs;
using BetterLegacy.Editor.Data;
using BetterLegacy.Editor.Data.Dialogs;
using Crosstales.FB;
using LSFunctions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnityEngine;
using UnityEngine.UI;

namespace BetterLegacy.Editor.Managers
{
    public class LevelTemplateEditor : MonoBehaviour
    {
        #region Init

        public static LevelTemplateEditor inst;

        public static void Init() => Creator.NewGameObject(nameof(LevelTemplateEditor), EditorManager.inst.transform.parent).AddComponent<LevelTemplateEditor>();

        void Awake()
        {
            inst = this;
            GenerateUI();

            try
            {
                Dialog = new EditorDialog(EditorDialog.LEVEL_TEMPLATE_SELECTOR);
                Dialog.Init();
            }
            catch (Exception ex)
            {
                CoreHelper.LogException(ex);
            } // init dialog
        }

        void GenerateUI()
        {
            var editorDialogObject = Instantiate(EditorManager.inst.GetDialog("Multi Keyframe Editor (Object)").Dialog.gameObject);
            var editorDialogTransform = editorDialogObject.transform;
            editorDialogObject.name = "NewLevelTemplateDialog";
            editorDialogObject.layer = 5;
            editorDialogTransform.SetParent(EditorManager.inst.dialogs);
            editorDialogTransform.localScale = Vector3.one;
            editorDialogTransform.position = new Vector3(1537.5f, 714.945f, 0f) * EditorManager.inst.ScreenScale;
            editorDialogTransform.AsRT().sizeDelta = new Vector2(0f, 32f);

            EditorThemeManager.AddGraphic(editorDialogObject.GetComponent<Image>(), ThemeGroup.Background_1);

            var editorDialogTitle = editorDialogTransform.GetChild(0);
            var editorDialogTitleImage = editorDialogTitle.GetComponent<Image>();
            var editorDialogTitleText = editorDialogTitle.GetChild(0).GetComponent<Text>();
            editorDialogTitleText.text = "- New Level Template -";

            EditorThemeManager.AddGraphic(editorDialogTitleImage, ThemeGroup.Add);
            EditorThemeManager.AddGraphic(editorDialogTitleText, ThemeGroup.Add_Text);

            var editorDialogSpacer = editorDialogTransform.GetChild(1);
            editorDialogSpacer.AsRT().sizeDelta = new Vector2(765f, 54f);

            Destroy(editorDialogTransform.GetChild(2).gameObject);

            EditorHelper.AddEditorDialog("New Level Template Dialog", editorDialogObject);

            var scrollView = EditorPrefabHolder.Instance.ScrollView.Duplicate(editorDialogTransform, "Scroll View");
            newLevelTemplateContent = scrollView.transform.Find("Viewport/Content");

            var scrollViewLE = scrollView.AddComponent<LayoutElement>();
            scrollViewLE.ignoreLayout = true;

            scrollView.transform.AsRT().anchoredPosition = new Vector2(392.5f, 280f);
            scrollView.transform.AsRT().sizeDelta = new Vector2(735f, 542f);

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

            CoreHelper.StartCoroutine(AlephNetwork.DownloadImageTexture($"{RTFile.ApplicationDirectory}{RTFile.BepInExAssetsPath}default_template.png", texture2D => newLevelTemplateBaseSprite = SpriteHelper.CreateSprite(texture2D)));

            var delete = EditorPrefabHolder.Instance.DeleteButton.Duplicate(newLevelTemplatePrefab.transform, "Delete");
            RectValues.Default.AnchoredPosition(335f, 75f).SizeDelta(32f, 32f).AssignToRectTransform(delete.transform.AsRT());

            #endregion

            var gameObject = Creator.NewUIObject("create", editorDialogTransform, 2);
            gameObject.transform.AsRT().sizeDelta = new Vector2(765f, 32f);

            var createLevelTemplateButton = EditorPrefabHolder.Instance.Function2Button.Duplicate(gameObject.transform, "create");
            UIManager.SetRectTransform(createLevelTemplateButton.transform.AsRT(), new Vector2(200f, 42f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(322f, 32f));
            var createLevelTemplateButtonStorage = createLevelTemplateButton.GetComponent<FunctionButtonStorage>();
            createLevelTemplateButtonStorage.text.text = "Create a new template";
            createLevelTemplateButtonStorage.button.onClick.ClearAll();
            createLevelTemplateButtonStorage.button.onClick.AddListener(() =>
            {
                choosingLevelTemplate = true;
                RTEditor.inst.OpenLevelPopup.Open();
                EditorManager.inst.RenderOpenBeatmapPopup();

                EditorManager.inst.DisplayNotification("Choose a level to create a template from.", 4f, EditorManager.NotificationType.Info);
            });

            var gameObject2 = Creator.NewUIObject("name", editorDialogTransform, 3);
            gameObject2.transform.AsRT().sizeDelta = new Vector2(765f, 32f);

            nameInput = EditorPrefabHolder.Instance.NumberInputField.GetComponent<InputFieldStorage>().inputField.gameObject.Duplicate(gameObject2.transform, "name").GetComponent<InputField>();
            nameInput.onValueChanged.ClearAll();
            nameInput.text = "New Level Template";
            RectValues.Default.AnchoredPosition(160f, 42f).SizeDelta(400f, 32f).AssignToRectTransform(nameInput.image.rectTransform);

            var gameObject3 = Creator.NewUIObject("preview", editorDialogTransform, 4);
            gameObject3.transform.AsRT().sizeDelta = new Vector2(765f, 32f);

            var preview = Creator.NewUIObject("preview", gameObject3.transform);
            var previewImage = preview.AddComponent<Image>();
            RectValues.Default.AnchoredPosition(-200f, 76f).SizeDelta(240f, 135f).AssignToRectTransform(previewImage.rectTransform);

            var choosePreviewButton = EditorPrefabHolder.Instance.Function2Button.Duplicate(gameObject3.transform, "choose");
            UIManager.SetRectTransform(choosePreviewButton.transform.AsRT(), new Vector2(200f, 42f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(322f, 32f));
            var choosePreviewButtonStorage = choosePreviewButton.GetComponent<FunctionButtonStorage>();
            choosePreviewButtonStorage.text.text = "Select a preview";
            choosePreviewButtonStorage.button.onClick.ClearAll();
            choosePreviewButtonStorage.button.onClick.AddListener(() =>
            {
                RTEditor.inst.ShowWarningPopup("Select a file browser.", () =>
                {
                    string text = FileBrowser.OpenSingleFile("Select a preview image to use!", RTFile.ApplicationDirectory, FileFormat.PNG.ToName());
                    if (!string.IsNullOrEmpty(text))
                    {
                        var sprite = SpriteHelper.LoadSprite(text);

                        if (sprite.texture.width != 480 || sprite.texture.height != 270)
                        {
                            EditorManager.inst.DisplayNotification("Preview image resolution must be 480p x 270p", 3f, EditorManager.NotificationType.Warning);
                            RTEditor.inst.HideWarningPopup();
                            return;
                        }

                        currentTemplateSprite = sprite;
                        previewImage.sprite = currentTemplateSprite;
                    }
                    RTEditor.inst.HideWarningPopup();
                }, () =>
                {
                    RTEditor.inst.BrowserPopup.Open();
                    RTFileBrowser.inst.UpdateBrowserFile(new string[] { FileFormat.PNG.Dot() }, onSelectFile: _val =>
                    {
                        if (string.IsNullOrEmpty(_val))
                            return;

                        RTEditor.inst.BrowserPopup.Close();
                        var sprite = SpriteHelper.LoadSprite(_val);

                        if (sprite.texture.width != 480 || sprite.texture.height != 270)
                        {
                            EditorManager.inst.DisplayNotification("Preview image resolution must be 480p x 270p", 3f, EditorManager.NotificationType.Warning);
                            return;
                        }

                        currentTemplateSprite = sprite;
                        previewImage.sprite = currentTemplateSprite;
                    });
                    RTEditor.inst.HideWarningPopup();
                }, "System Browser", "Editor Browser");
            });
        }

        #endregion

        #region Values

        public EditorDialog Dialog { get; set; }

        public bool choosingLevelTemplate;
        public int currentLevelTemplate = -1;

        public Transform newLevelTemplateContent;
        public GameObject newLevelTemplatePrefab;
        public Sprite newLevelTemplateBaseSprite;
        public InputField nameInput;
        public Sprite currentTemplateSprite;

        public LevelTemplate defaultLevelTemplate = new LevelTemplate("Default");

        public List<LevelTemplate> LevelTemplates { get; set; } = new List<LevelTemplate>();

        public LevelTemplate CurrentTemplate => currentLevelTemplate >= 0 && currentLevelTemplate < LevelTemplates.Count ? LevelTemplates[currentLevelTemplate] : defaultLevelTemplate;

        #endregion

        #region Methods

        /// <summary>
        /// Creates a new level template from an existing level.
        /// </summary>
        /// <param name="file">Level folder to use.</param>
        public void CreateTemplate(string file)
        {
            if (string.IsNullOrEmpty(nameInput.text))
            {
                EditorManager.inst.DisplayNotification($"Level template name is empty. Name it something unique via the input field in the Level Template editor.", 3f, EditorManager.NotificationType.Error);
                return;
            }

            if (string.IsNullOrEmpty(file))
            {
                EditorManager.inst.DisplayNotification($"Path is empty.", 3f, EditorManager.NotificationType.Error);
                return;
            }

            file = file.Remove("/level.lsb");

            var levelLSB = RTFile.CombinePaths(file, Level.LEVEL_LSB);
            if (!RTFile.FileExists(levelLSB))
            {
                EditorManager.inst.DisplayNotification($"Level file doesn't exist.", 3f, EditorManager.NotificationType.Error);
                return;
            }

            RTEditor.inst.OpenLevelPopup.Close();

            RTEditor.inst.ShowWarningPopup("Are you sure you want to make a new level template?", () =>
            {
                choosingLevelTemplate = false;

                var copyTo = RTFile.CombinePaths(RTFile.ApplicationDirectory, "beatmaps/templates", RTFile.ValidateDirectory(nameInput.text));

                RTFile.CreateDirectory(copyTo);
                RTFile.CopyFile(levelLSB, RTFile.CombinePaths(copyTo, Level.LEVEL_LSB));

                if (currentTemplateSprite)
                    currentTemplateSprite.Save(RTFile.CombinePaths(copyTo, $"preview{FileFormat.PNG.Dot()}"));

                RefreshNewLevelTemplates();
                RTEditor.inst.HideWarningPopup();
            }, () =>
            {
                RTEditor.inst.OpenLevelPopup.Open();
                RTEditor.inst.HideWarningPopup();
            });
        }

        public void RefreshNewLevelTemplates()
        {
            LevelTemplates.Clear();
            LSHelpers.DeleteChildren(newLevelTemplateContent);
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

        public void SelectTemplate(int index)
        {
            currentLevelTemplate = index;
            EditorManager.inst.DisplayNotification($"Set level template to {CurrentTemplate.name} [{currentLevelTemplate}]", 2f, EditorManager.NotificationType.Success);

            RenderSelected();
        }

        public void RenderSelected()
        {
            for (int i = 0; i < LevelTemplates.Count; i++)
                LevelTemplates[i].RenderTitle();
        }

        #endregion
    }
}
