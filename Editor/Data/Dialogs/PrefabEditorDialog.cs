using UnityEngine;
using UnityEngine.UI;

using LSFunctions;

using BetterLegacy.Core;
using BetterLegacy.Core.Components;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using BetterLegacy.Core.Prefabs;
using BetterLegacy.Editor.Managers;

namespace BetterLegacy.Editor.Data.Dialogs
{
    public class PrefabEditorDialog : EditorDialog
    {
        public PrefabEditorDialog() : base() { }

        public RectTransform Content { get; set; }
        public FunctionButtonStorage TypeButton { get; set; }
        public InputField CreatorField { get; set; }
        public InputField NameField { get; set; }
        public InputField DescriptionField { get; set; }
        public InputField VersionField { get; set; }

        public FunctionButtonStorage ImportPrefabButton { get; set; }

        public FunctionButtonStorage ConvertPrefabButton { get; set; }

        public FunctionButtonStorage UploadPrefabButton { get; set; }

        public override void Init()
        {
            if (init)
                return;

            var editorDialog = EditorPrefabHolder.Instance.Dialog.Duplicate(EditorManager.inst.dialogs, "PrefabEditorDialog");
            var editorDialogStorage = editorDialog.GetComponent<EditorDialogStorage>();
            editorDialog.transform.AsRT().sizeDelta = new Vector2(0f, 32f);

            EditorHelper.AddEditorDialog(PREFAB_EXTERNAL_EDITOR, editorDialog);

            InitDialog(PREFAB_EXTERNAL_EDITOR);

            base.Init();

            #region Generation

            editorDialogStorage.topPanel.color = LSColors.HexToColor("4C4C4C");
            editorDialogStorage.title.text = "- Prefab Editor -";
            editorDialogStorage.title.color = new Color(0.9f, 0.9f, 0.9f, 1f);

            var editorDialogSpacer = editorDialog.transform.GetChild(1);
            editorDialogSpacer.AsRT().sizeDelta = new Vector2(765f, 54f);

            editorDialog.transform.GetChild(1).AsRT().sizeDelta = new Vector2(765f, 24f);

            var main = Creator.NewUIObject("Main", editorDialog.transform);
            main.transform.AsRT().sizeDelta = new Vector2(765f, 668f);

            var scrollView = EditorPrefabHolder.Instance.ScrollView.Duplicate(main.transform, "Scroll View");
            RectValues.Default.SizeDelta(745f, 668f).AssignToRectTransform(scrollView.transform.AsRT());
            Content = scrollView.transform.Find("Viewport/Content").AsRT();

            CoreHelper.Delete(editorDialog.transform.GetChild(2));

            new Labels(Labels.InitSettings.Default.Parent(Content), "Type");

            var prefabType = EditorPrefabHolder.Instance.Function1Button.Duplicate(Content, "Show Type Editor");
            RectValues.Default.SizeDelta(742f, 34f).AssignToRectTransform(prefabType.transform.AsRT());
            TypeButton = prefabType.GetComponent<FunctionButtonStorage>();
            TypeButton.button.onClick.ClearAll();

            prefabType.AddComponent<ContrastColors>().Init(TypeButton.label, TypeButton.button.image);

            new Labels(Labels.InitSettings.Default.Parent(Content), "Creator");

            var creator = EditorPrefabHolder.Instance.DefaultInputField.Duplicate(Content);
            RectValues.Default.SizeDelta(740f, 32f).AssignToRectTransform(creator.transform.AsRT());

            CreatorField = creator.GetComponent<InputField>();
            CreatorField.GetPlaceholderText().text = "Set creator...";
            CreatorField.GetPlaceholderText().color = new Color(0.1961f, 0.1961f, 0.1961f, 0.5f);
            
            new Labels(Labels.InitSettings.Default.Parent(Content), "Name");

            var name = EditorPrefabHolder.Instance.DefaultInputField.Duplicate(Content);
            RectValues.Default.SizeDelta(740f, 32f).AssignToRectTransform(name.transform.AsRT());

            NameField = name.GetComponent<InputField>();
            NameField.lineType = InputField.LineType.MultiLineNewline;
            NameField.GetPlaceholderText().text = "Set name...";
            NameField.GetPlaceholderText().color = new Color(0.1961f, 0.1961f, 0.1961f, 0.5f);

            new Labels(Labels.InitSettings.Default.Parent(Content), "Description");

            var description = EditorPrefabHolder.Instance.DefaultInputField.Duplicate(Content);
            description.transform.localScale = Vector3.one;
            RectValues.Default.SizeDelta(740f, 300f).AssignToRectTransform(description.transform.AsRT());

            DescriptionField = description.GetComponent<InputField>();
            DescriptionField.lineType = InputField.LineType.MultiLineNewline;
            DescriptionField.GetPlaceholderText().text = "Set description...";
            DescriptionField.GetPlaceholderText().color = new Color(0.1961f, 0.1961f, 0.1961f, 0.5f);

            new Labels(Labels.InitSettings.Default.Parent(Content), "Prefab Version");

            var version = EditorPrefabHolder.Instance.DefaultInputField.Duplicate(Content);
            RectValues.Default.SizeDelta(740f, 32f).AssignToRectTransform(version.transform.AsRT());

            VersionField = version.GetComponent<InputField>();
            VersionField.GetPlaceholderText().text = "Set version...";
            VersionField.GetPlaceholderText().color = new Color(0.1961f, 0.1961f, 0.1961f, 0.5f);

            RTEditor.GenerateSpacer("spacer", Content, new Vector2(765f, 8f));

            var buttons = Creator.NewUIObject("buttons", Content);
            buttons.transform.AsRT().sizeDelta = new Vector2(765f, 32f);

            var buttonsHLG = buttons.AddComponent<HorizontalLayoutGroup>();
            buttonsHLG.spacing = 60f;

            var importPrefab = EditorPrefabHolder.Instance.Function2Button.Duplicate(buttons.transform, "import");
            importPrefab.SetActive(true);
            ImportPrefabButton = importPrefab.GetComponent<FunctionButtonStorage>();
            ImportPrefabButton.Text = "Import Prefab";

            var exportToVG = EditorPrefabHolder.Instance.Function2Button.Duplicate(buttons.transform, "export");
            exportToVG.SetActive(true);
            ConvertPrefabButton = exportToVG.GetComponent<FunctionButtonStorage>();
            ConvertPrefabButton.Text = "Convert to VG Format";

            var uploadPrefab = EditorPrefabHolder.Instance.Function1Button.Duplicate(buttons.transform, "upload");
            uploadPrefab.SetActive(false);
            UploadPrefabButton = uploadPrefab.GetComponent<FunctionButtonStorage>();
            UploadPrefabButton.Text = "Upload";

            #endregion

            #region Editor Themes

            EditorThemeManager.AddGraphic(TypeButton.button.image, ThemeGroup.Null, true);

            EditorThemeManager.AddInputField(CreatorField);
            EditorThemeManager.AddInputField(NameField);
            EditorThemeManager.AddInputField(DescriptionField);
            EditorThemeManager.AddInputField(VersionField);

            EditorThemeManager.AddGraphic(editorDialog.GetComponent<Image>(), ThemeGroup.Background_1);
            EditorThemeManager.AddSelectable(ImportPrefabButton.button, ThemeGroup.Function_2);
            EditorThemeManager.AddGraphic(ImportPrefabButton.label, ThemeGroup.Function_2_Text);
            EditorThemeManager.AddSelectable(ConvertPrefabButton.button, ThemeGroup.Function_2);
            EditorThemeManager.AddGraphic(ConvertPrefabButton.label, ThemeGroup.Function_2_Text);
            EditorThemeManager.AddGraphic(UploadPrefabButton.button.image, ThemeGroup.Function_1);
            EditorThemeManager.AddGraphic(UploadPrefabButton.label, ThemeGroup.Function_1_Text);

            #endregion
        }
    }
}
