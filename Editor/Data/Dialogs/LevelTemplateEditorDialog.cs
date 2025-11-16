using UnityEngine;
using UnityEngine.UI;

using LSFunctions;

using Crosstales.FB;

using BetterLegacy.Core;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Prefabs;
using BetterLegacy.Editor.Managers;

namespace BetterLegacy.Editor.Data.Dialogs
{
    public class LevelTemplateEditorDialog : EditorDialog, IContentUI
    {
        public LevelTemplateEditorDialog() : base() { }

        public InputField SearchField { get; set; }

        public Transform Content { get; set; }

        public GridLayoutGroup Grid { get; set; }

        public Scrollbar ContentScrollbar { get; set; }

        public string SearchTerm { get; set; }

        public void ClearContent() => LSHelpers.DeleteChildren(Content);

        /// <summary>
        /// Create New Level Template button.
        /// </summary>
        public FunctionButtonStorage CreateNewTemplateButton { get; set; }

        /// <summary>
        /// Name editor.
        /// </summary>
        public InputField NameField { get; set; }

        /// <summary>
        /// Choose Preview button.
        /// </summary>
        public FunctionButtonStorage ChoosePreviewButton { get; set; }

        public override void Init()
        {
            if (init)
                return;

            base.Init();

            var editorDialogObject = EditorPrefabHolder.Instance.Dialog.Duplicate(EditorManager.inst.dialogs, "NewLevelTemplateDialog");
            editorDialogObject.transform.AsRT().anchoredPosition = new Vector2(0f, 16f);
            editorDialogObject.transform.AsRT().sizeDelta = new Vector2(0f, 32f);
            var dialogStorage = editorDialogObject.GetComponent<EditorDialogStorage>();

            dialogStorage.title.text = "- Level Templates -";

            EditorThemeManager.AddGraphic(editorDialogObject.GetComponent<Image>(), ThemeGroup.Background_1);

            EditorThemeManager.AddGraphic(dialogStorage.topPanel, ThemeGroup.Add);
            EditorThemeManager.AddGraphic(dialogStorage.title, ThemeGroup.Add_Text);

            var editorDialogSpacer = editorDialogObject.transform.GetChild(1);
            editorDialogSpacer.AsRT().sizeDelta = new Vector2(765f, 54f);

            CoreHelper.Delete(editorDialogObject.transform.GetChild(2).gameObject);

            EditorHelper.AddEditorDialog(LEVEL_TEMPLATE_SELECTOR, editorDialogObject);

            InitDialog(LEVEL_TEMPLATE_SELECTOR);

            var scrollView = EditorPrefabHolder.Instance.ScrollView.Duplicate(editorDialogObject.transform, "Scroll View");
            Content = scrollView.transform.Find("Viewport/Content");

            var scrollViewLE = scrollView.AddComponent<LayoutElement>();
            scrollViewLE.ignoreLayout = true;

            scrollView.transform.AsRT().anchoredPosition = new Vector2(392.5f, 280f);
            scrollView.transform.AsRT().sizeDelta = new Vector2(735f, 542f);

            var gameObject = Creator.NewUIObject("create", editorDialogObject.transform, 2);
            gameObject.transform.AsRT().sizeDelta = new Vector2(765f, 32f);

            var createLevelTemplateButton = EditorPrefabHolder.Instance.Function2Button.Duplicate(gameObject.transform, "create");
            RectValues.Default.AnchoredPosition(200f, 42f).SizeDelta(322f, 32f).AssignToRectTransform(createLevelTemplateButton.transform.AsRT());
            CreateNewTemplateButton = createLevelTemplateButton.GetComponent<FunctionButtonStorage>();
            CreateNewTemplateButton.Text = "Create a new template";
            CreateNewTemplateButton.OnClick.NewListener(() =>
            {
                LevelTemplateEditor.inst.choosingLevelTemplate = true;
                EditorLevelManager.inst.OpenLevelPopup.Open();
                EditorManager.inst.RenderOpenBeatmapPopup();

                EditorManager.inst.DisplayNotification("Choose a level to create a template from.", 4f, EditorManager.NotificationType.Info);
            });
            EditorThemeManager.AddSelectable(CreateNewTemplateButton.button, ThemeGroup.Function_2);
            EditorThemeManager.AddGraphic(CreateNewTemplateButton.label, ThemeGroup.Function_2_Text);

            var gameObject2 = Creator.NewUIObject("name", editorDialogObject.transform, 3);
            gameObject2.transform.AsRT().sizeDelta = new Vector2(765f, 32f);

            NameField = EditorPrefabHolder.Instance.NumberInputField.GetComponent<InputFieldStorage>().inputField.gameObject.Duplicate(gameObject2.transform, "name").GetComponent<InputField>();
            NameField.onValueChanged.ClearAll();
            NameField.text = "New Level Template";
            EditorThemeManager.AddInputField(NameField);
            RectValues.Default.AnchoredPosition(160f, 42f).SizeDelta(400f, 32f).AssignToRectTransform(NameField.image.rectTransform);

            var gameObject3 = Creator.NewUIObject("preview", editorDialogObject.transform, 4);
            gameObject3.transform.AsRT().sizeDelta = new Vector2(765f, 32f);

            var preview = Creator.NewUIObject("preview", gameObject3.transform);
            var previewImage = preview.AddComponent<Image>();
            RectValues.Default.AnchoredPosition(-200f, 76f).SizeDelta(240f, 135f).AssignToRectTransform(previewImage.rectTransform);

            var choosePreviewButton = EditorPrefabHolder.Instance.Function2Button.Duplicate(gameObject3.transform, "choose");
            RectValues.Default.AnchoredPosition(200f, 42f).SizeDelta(322f, 32f).AssignToRectTransform(choosePreviewButton.transform.AsRT());
            ChoosePreviewButton = choosePreviewButton.GetComponent<FunctionButtonStorage>();
            ChoosePreviewButton.Text = "Select a preview";
            ChoosePreviewButton.OnClick.NewListener(() =>
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

                        LevelTemplateEditor.inst.currentTemplateSprite = sprite;
                        previewImage.sprite = LevelTemplateEditor.inst.currentTemplateSprite;
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

                        LevelTemplateEditor.inst.currentTemplateSprite = sprite;
                        previewImage.sprite = LevelTemplateEditor.inst.currentTemplateSprite;
                    });
                    RTEditor.inst.HideWarningPopup();
                }, "System Browser", "Editor Browser");
            });
            EditorThemeManager.AddSelectable(ChoosePreviewButton.button, ThemeGroup.Function_2);
            EditorThemeManager.AddGraphic(ChoosePreviewButton.label, ThemeGroup.Function_2_Text);
        }
    }
}
