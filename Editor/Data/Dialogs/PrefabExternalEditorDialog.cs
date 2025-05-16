using UnityEngine;
using UnityEngine.UI;

using LSFunctions;

using BetterLegacy.Core;
using BetterLegacy.Core.Components;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using BetterLegacy.Core.Prefabs;
using BetterLegacy.Editor.Managers;

namespace BetterLegacy.Editor.Data.Dialogs
{
    public class PrefabExternalEditorDialog : EditorDialog
    {
        public PrefabExternalEditorDialog() : base() { }

        public InputField NameField { get; set; }
        public FunctionButtonStorage TypeButton { get; set; }
        public InputField DescriptionField { get; set; }

        public FunctionButtonStorage ImportPrefabButton { get; set; }

        public FunctionButtonStorage ConvertPrefabButton { get; set; }

        public override void Init()
        {
            if (init)
                return;

            var editorDialog = EditorPrefabHolder.Instance.Dialog.Duplicate(EditorManager.inst.dialogs, "PrefabExternalDialog");
            var editorDialogStorage = editorDialog.GetComponent<EditorDialogStorage>();
            editorDialog.transform.AsRT().sizeDelta = new Vector2(0f, 32f);

            EditorHelper.AddEditorDialog(PREFAB_EXTERNAL_EDITOR, editorDialog);

            InitDialog(PREFAB_EXTERNAL_EDITOR);

            base.Init();

            #region Generation

            editorDialogStorage.topPanel.color = LSColors.HexToColor("4C4C4C");
            editorDialogStorage.title.text = "- External Prefab View -";
            editorDialogStorage.title.color = new Color(0.9f, 0.9f, 0.9f, 1f);

            var editorDialogSpacer = editorDialog.transform.GetChild(1);
            editorDialogSpacer.AsRT().sizeDelta = new Vector2(765f, 54f);

            editorDialog.transform.GetChild(1).AsRT().sizeDelta = new Vector2(765f, 24f);

            var labelTypeBase = Creator.NewUIObject("Type Label", editorDialog.transform);

            labelTypeBase.transform.AsRT().sizeDelta = new Vector2(765f, 32f);

            var labelType = editorDialog.transform.GetChild(2);
            labelType.SetParent(labelTypeBase.transform);
            labelType.localPosition = Vector3.zero;
            labelType.localScale = Vector3.one;
            labelType.AsRT().sizeDelta = new Vector2(725f, 32f);
            var labelTypeText = labelType.GetComponent<Text>();
            labelTypeText.text = "Type";
            labelTypeText.alignment = TextAnchor.UpperLeft;

            var prefabTypeBase = Creator.NewUIObject("Prefab Type Base", editorDialog.transform);
            prefabTypeBase.transform.AsRT().sizeDelta = new Vector2(765f, 32f);

            var prefabType = EditorPrefabHolder.Instance.Function1Button.gameObject.Duplicate(prefabTypeBase.transform, "Show Type Editor");
            TypeButton = prefabType.GetComponent<FunctionButtonStorage>();

            UIManager.SetRectTransform(prefabType.transform.AsRT(), new Vector2(-370f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 0.5f), new Vector2(742f, 34f));
            TypeButton.button.onClick.ClearAll();

            prefabType.AddComponent<ContrastColors>().Init(TypeButton.label, TypeButton.button.image);

            RTEditor.GenerateSpacer("spacer2", editorDialog.transform, new Vector2(765f, 24f));

            var labelNameBase = Creator.NewUIObject("Name Label", editorDialog.transform);
            labelNameBase.transform.AsRT().sizeDelta = new Vector2(765f, 32f);

            var labelName = labelType.gameObject.Duplicate(labelNameBase.transform);
            labelName.transform.localPosition = Vector3.zero;
            labelName.transform.localScale = Vector3.one;
            labelName.transform.AsRT().sizeDelta = new Vector2(725f, 32f);
            var labelNameText = labelName.GetComponent<Text>();
            labelNameText.text = "Name";
            labelNameText.alignment = TextAnchor.UpperLeft;

            var nameTextBase1 = Creator.NewUIObject("Text Base 1", editorDialog.transform);
            nameTextBase1.transform.AsRT().sizeDelta = new Vector2(765f, 32f);

            var name = EditorPrefabHolder.Instance.DefaultInputField.Duplicate(nameTextBase1.transform);
            name.transform.localScale = Vector3.one;
            UIManager.SetRectTransform(name.transform.AsRT(), Vector2.zero, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(740f, 32f));

            NameField = name.GetComponent<InputField>();
            NameField.lineType = InputField.LineType.MultiLineNewline;
            NameField.GetPlaceholderText().text = "Set name...";
            NameField.GetPlaceholderText().color = new Color(0.1961f, 0.1961f, 0.1961f, 0.5f);

            RTEditor.GenerateSpacer("spacer3", editorDialog.transform, new Vector2(765f, 4f));

            var labelDescriptionBase = Creator.NewUIObject("Description Label", editorDialog.transform);
            labelDescriptionBase.transform.AsRT().sizeDelta = new Vector2(765f, 32f);

            var labelDescription = labelType.gameObject.Duplicate(labelDescriptionBase.transform);
            labelDescription.transform.localPosition = Vector3.zero;
            labelDescription.transform.localScale = Vector3.one;
            labelDescription.transform.AsRT().sizeDelta = new Vector2(725f, 32f);
            var labelDescriptionText = labelDescription.GetComponent<Text>();
            labelDescriptionText.text = "Description";
            labelDescriptionText.alignment = TextAnchor.UpperLeft;

            var descriptionTextBase1 = Creator.NewUIObject("Text Base 1", editorDialog.transform);
            descriptionTextBase1.transform.AsRT().sizeDelta = new Vector2(765f, 300f);

            var description = EditorPrefabHolder.Instance.DefaultInputField.Duplicate(descriptionTextBase1.transform);
            description.transform.localScale = Vector3.one;
            UIManager.SetRectTransform(description.transform.AsRT(), Vector2.zero, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(740f, 300f));

            DescriptionField = description.GetComponent<InputField>();
            DescriptionField.lineType = InputField.LineType.MultiLineNewline;
            DescriptionField.GetPlaceholderText().text = "Set description...";
            DescriptionField.GetPlaceholderText().color = new Color(0.1961f, 0.1961f, 0.1961f, 0.5f);

            RTEditor.GenerateSpacer("spacer4", editorDialog.transform, new Vector2(765f, 80f));

            var buttonsBase = new GameObject("buttons base");
            buttonsBase.transform.SetParent(editorDialog.transform);
            buttonsBase.transform.localScale = Vector3.one;

            var buttonsBaseRT = buttonsBase.AddComponent<RectTransform>();
            buttonsBaseRT.sizeDelta = new Vector2(765f, 0f);

            var buttons = new GameObject("buttons");
            buttons.transform.SetParent(buttonsBaseRT);
            buttons.transform.localScale = Vector3.one;

            var buttonsHLG = buttons.AddComponent<HorizontalLayoutGroup>();
            buttonsHLG.spacing = 60f;

            buttons.transform.AsRT().sizeDelta = new Vector2(600f, 32f);

            var importPrefab = EditorPrefabHolder.Instance.Function2Button.Duplicate(buttons.transform, "import");
            importPrefab.SetActive(true);
            ImportPrefabButton = importPrefab.GetComponent<FunctionButtonStorage>();
            ImportPrefabButton.label.text = "Import Prefab";

            var exportToVG = EditorPrefabHolder.Instance.Function2Button.Duplicate(buttons.transform, "export");
            exportToVG.SetActive(true);
            ConvertPrefabButton = exportToVG.GetComponent<FunctionButtonStorage>();
            ConvertPrefabButton.label.text = "Convert to VG Format";

            #endregion

            #region Editor Themes

            EditorThemeManager.AddLightText(labelNameText);
            EditorThemeManager.AddInputField(NameField);

            EditorThemeManager.AddLightText(labelTypeText);
            EditorThemeManager.AddGraphic(TypeButton.button.image, ThemeGroup.Null, true);

            EditorThemeManager.AddLightText(labelDescriptionText);
            EditorThemeManager.AddInputField(DescriptionField);

            EditorThemeManager.AddGraphic(editorDialog.GetComponent<Image>(), ThemeGroup.Background_1);
            EditorThemeManager.AddSelectable(ImportPrefabButton.button, ThemeGroup.Function_2);
            EditorThemeManager.AddSelectable(ConvertPrefabButton.button, ThemeGroup.Function_2);
            EditorThemeManager.AddGraphic(ImportPrefabButton.label, ThemeGroup.Function_2_Text);
            EditorThemeManager.AddGraphic(ConvertPrefabButton.label, ThemeGroup.Function_2_Text);

            #endregion
        }
    }
}
