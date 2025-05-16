using UnityEngine;
using UnityEngine.UI;

using BetterLegacy.Core;
using BetterLegacy.Core.Components;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using BetterLegacy.Core.Prefabs;
using BetterLegacy.Editor.Managers;

namespace BetterLegacy.Editor.Data.Dialogs
{
    public class PrefabCreatorDialog : EditorDialog
    {
        public PrefabCreatorDialog() : base(PREFAB_EDITOR) { }

        #region Values

        public InputField NameField { get; set; }
        public Slider OffsetSlider { get; set; }
        public InputField OffsetField { get; set; }
        public FunctionButtonStorage TypeButton { get; set; }
        public InputField DescriptionField { get; set; }

        #endregion

        public override void Init()
        {
            if (init)
                return;

            base.Init();

            #region Setup

            EditorThemeManager.AddGraphic(PrefabEditor.inst.dialog.gameObject.GetComponent<Image>(), ThemeGroup.Background_1);

            var prefabEditorData = PrefabEditor.inst.dialog.Find("data");

            CoreHelper.Delete(prefabEditorData.Find("offset/<").gameObject);
            CoreHelper.Delete(prefabEditorData.Find("offset/>").gameObject);

            NameField = prefabEditorData.Find("name/input").GetComponent<InputField>();

            OffsetSlider = prefabEditorData.Find("offset/slider").GetComponent<Slider>();
            OffsetField = prefabEditorData.Find("offset/input").GetComponent<InputField>();
            OffsetField.characterLimit = 0;

            CoreHelper.Delete(prefabEditorData.Find("type/types").gameObject);

            var prefabType = EditorPrefabHolder.Instance.Function1Button.gameObject.Duplicate(prefabEditorData.Find("type"), "Show Type Editor");
            TypeButton = prefabType.GetComponent<FunctionButtonStorage>();

            UIManager.SetRectTransform(prefabType.transform.AsRT(), new Vector2(-370f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 0.5f), new Vector2(652f, 34f));
            TypeButton.button.onClick.ClearAll();

            prefabType.AddComponent<ContrastColors>().Init(TypeButton.label, TypeButton.button.image);
            prefabEditorData.Find("type").AsRT().sizeDelta = new Vector2(749f, 48f);

            prefabEditorData.Find("spacer").AsRT().sizeDelta = new Vector2(749f, 32f);

            var descriptionGO = prefabEditorData.Find("name").gameObject.Duplicate(prefabEditorData, "description", 4);
            descriptionGO.transform.AsRT().sizeDelta = new Vector2(749f, 108f);
            var descriptionTitle = descriptionGO.transform.Find("title").GetComponent<Text>();
            descriptionTitle.text = "Desc";
            EditorThemeManager.AddLightText(descriptionTitle);
            DescriptionField = descriptionGO.transform.Find("input").GetComponent<InputField>();
            DescriptionField.GetPlaceholderText().alignment = TextAnchor.UpperLeft;
            DescriptionField.GetPlaceholderText().text = "Enter description...";

            DescriptionField.onValueChanged.ClearAll();
            DescriptionField.lineType = InputField.LineType.MultiLineNewline;
            DescriptionField.characterLimit = 0;
            DescriptionField.characterValidation = InputField.CharacterValidation.None;
            DescriptionField.textComponent.alignment = TextAnchor.UpperLeft;
            DescriptionField.text = string.Empty;

            var selection = prefabEditorData.Find("selection");
            EditorHelper.SetComplexity(selection.gameObject, Complexity.Advanced);
            selection.AsRT().sizeDelta = new Vector2(749f, 300f);
            var search = selection.Find("search-box/search").GetComponent<InputField>();
            search.onValueChanged.NewListener(_val => RTPrefabEditor.inst.ReloadSelectionContent());

            var selectionGroup = selection.Find("mask/content").GetComponent<GridLayoutGroup>();
            selectionGroup.cellSize = new Vector2(172.5f, 32f);
            selectionGroup.constraintCount = 4;

            #endregion

            #region Editor Themes

            EditorThemeManager.AddGraphic(prefabEditorData.Find("title/Panel/icon").GetComponent<Image>(), ThemeGroup.Light_Text);
            EditorThemeManager.AddLightText(prefabEditorData.Find("title/title").GetComponent<Text>());
            EditorThemeManager.AddLightText(prefabEditorData.Find("name/title").GetComponent<Text>());
            EditorThemeManager.AddLightText(prefabEditorData.Find("offset/title").GetComponent<Text>());
            EditorThemeManager.AddLightText(prefabEditorData.Find("type/title").GetComponent<Text>());

            EditorThemeManager.AddInputField(NameField);
            EditorThemeManager.AddGraphic(OffsetSlider.transform.Find("Background").GetComponent<Image>(), ThemeGroup.Slider_2, true);

            EditorThemeManager.AddGraphic(OffsetSlider.image, ThemeGroup.Slider_2_Handle, true);
            EditorThemeManager.AddInputField(OffsetField);

            EditorThemeManager.AddGraphic(TypeButton.button.image, ThemeGroup.Null, true);

            EditorThemeManager.AddInputField(DescriptionField);

            EditorThemeManager.AddInputField(search, ThemeGroup.Search_Field_2);

            EditorThemeManager.AddGraphic(selection.GetComponent<Image>(), ThemeGroup.Background_3, true);

            EditorThemeManager.AddGraphic(PrefabEditor.inst.dialog.Find("submit/submit").GetComponent<Image>(), ThemeGroup.Add, true);
            EditorThemeManager.AddGraphic(PrefabEditor.inst.dialog.Find("submit/submit/Text").GetComponent<Text>(), ThemeGroup.Add_Text);

            var scrollbar = selection.Find("scrollbar");
            EditorThemeManager.AddScrollbar(scrollbar.GetComponent<Scrollbar>(), scrollbar.GetComponent<Image>(), ThemeGroup.Scrollbar_2, ThemeGroup.Scrollbar_2_Handle);

            #endregion
        }
    }
}
