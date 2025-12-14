using System.Collections.Generic;

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
    public class PrefabCreatorDialog : EditorDialog
    {
        public PrefabCreatorDialog() : base(PREFAB_EDITOR) { }

        #region Values

        public RectTransform Content { get; set; }
        public InputField NameField { get; set; }
        public Slider OffsetSlider { get; set; }
        public InputField OffsetField { get; set; }
        public FunctionButtonStorage TypeButton { get; set; }
        public InputField DescriptionField { get; set; }

        public RectTransform SelectionTabsContent { get; set; }
        public List<Button> SelectionTabButtons { get; set; } = new List<Button>();
        public InputField SelectionSearchField { get; set; }
        public string SelectionSearchTerm { get => SelectionSearchField.text; set => SelectionSearchField.text = value; }
        public RectTransform SelectionContent { get; set; }

        public RectTransform IconBase { get; set; }
        public Image IconImage { get; set; }

        public Button SelectIconButton { get; set; }
        public Toggle CollapseToggle { get; set; }

        #endregion

        public override void Init()
        {
            if (init)
                return;

            base.Init();

            #region Setup

            GameObject.name = "PrefabCreatorDialog";
            GameObject.AddComponent<ActiveState>().onStateChanged = enabled => CaptureArea.inst.SetActive(enabled);
            EditorThemeManager.ApplyGraphic(GameObject.GetComponent<Image>(), ThemeGroup.Background_1);

            var prefabEditorData = GameObject.transform.Find("data");
            var scrollView = EditorPrefabHolder.Instance.ScrollView.Duplicate(GameObject.transform, "Scroll View", 1);
            scrollView.transform.AsRT().sizeDelta = new Vector2(765, 600f);
            Content = scrollView.transform.Find("Viewport/Content").AsRT();
            prefabEditorData.TransferChildren(Content);
            CoreHelper.Delete(prefabEditorData);

            CoreHelper.Delete(Content.Find("offset/<"));
            CoreHelper.Delete(Content.Find("offset/>"));

            GameObject.transform.Find("spacer").AsRT().sizeDelta = new Vector2(753.49f, 32f);

            NameField = Content.Find("name/input").GetComponent<InputField>();

            OffsetSlider = Content.Find("offset/slider").GetComponent<Slider>();
            OffsetField = Content.Find("offset/input").GetComponent<InputField>();
            OffsetField.characterLimit = 0;

            CoreHelper.Delete(Content.Find("type/types"));

            var prefabType = EditorPrefabHolder.Instance.Function1Button.gameObject.Duplicate(Content.Find("type"), "Show Type Editor");
            TypeButton = prefabType.GetComponent<FunctionButtonStorage>();

            UIManager.SetRectTransform(prefabType.transform.AsRT(), new Vector2(-370f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 0.5f), new Vector2(652f, 34f));
            TypeButton.OnClick.ClearAll();

            prefabType.AddComponent<ContrastColors>().Init(TypeButton.label, TypeButton.button.image);
            Content.Find("type").AsRT().sizeDelta = new Vector2(749f, 48f);

            Content.Find("spacer").AsRT().sizeDelta = new Vector2(749f, 32f);

            var descriptionGO = Content.Find("name").gameObject.Duplicate(Content, "description", 4);
            descriptionGO.transform.AsRT().sizeDelta = new Vector2(749f, 108f);
            var descriptionTitle = descriptionGO.transform.Find("title").GetComponent<Text>();
            descriptionTitle.text = "Desc";
            EditorThemeManager.ApplyLightText(descriptionTitle);
            DescriptionField = descriptionGO.transform.Find("input").GetComponent<InputField>();
            DescriptionField.GetPlaceholderText().alignment = TextAnchor.UpperLeft;
            DescriptionField.GetPlaceholderText().text = "Enter description...";

            DescriptionField.onValueChanged.ClearAll();
            DescriptionField.lineType = InputField.LineType.MultiLineNewline;
            DescriptionField.characterLimit = 0;
            DescriptionField.characterValidation = InputField.CharacterValidation.None;
            DescriptionField.textComponent.alignment = TextAnchor.UpperLeft;
            DescriptionField.text = string.Empty;

            new Labels(Labels.InitSettings.Default.Parent(Content).SiblingIndex(5), "Selection");

            var tabs = Creator.NewUIObject("tabs", Content, 6);
            SelectionTabsContent = tabs.transform.AsRT();
            SelectionTabsContent.sizeDelta = new Vector2(750f, 32f);
            var tabsLayout = tabs.AddComponent<HorizontalLayoutGroup>();
            tabsLayout.childControlHeight = true;
            tabsLayout.childForceExpandHeight = true;
            tabsLayout.childForceExpandWidth = true;
            tabsLayout.childForceExpandHeight = true;
            tabsLayout.spacing = 8f;

            SetupTab("Timeline Objects");
            SetupTab("Beatmap Themes");
            SetupTab("Modifier Blocks");
            SetupTab("Images");

            var selection = Content.Find("selection");
            EditorHelper.SetComplexity(selection.gameObject, Complexity.Advanced);
            selection.AsRT().sizeDelta = new Vector2(749f, 300f);
            SelectionSearchField = selection.Find("search-box/search").GetComponent<InputField>();

            SelectionContent = selection.Find("mask/content").AsRT();
            var selectionGroup = SelectionContent.GetComponent<GridLayoutGroup>();
            selectionGroup.cellSize = new Vector2(355f, 32f);
            selectionGroup.constraintCount = 2;

            #region Icon

            var iconBase = Creator.NewUIObject("icon", Content);
            IconBase = iconBase.transform.AsRT();
            RectValues.Default.SizeDelta(764f, 574f).AssignToRectTransform(IconBase);
            new Labels(Labels.InitSettings.Default.Parent(IconBase).Rect(new RectValues(new Vector2(16f, 0f), new Vector2(0f, 1f), new Vector2(0f, 1f), Vector2.zero, new Vector2(0f, -32f))), new Label("Icon") { fontStyle = FontStyle.Bold, });

            var icon = Creator.NewUIObject("image", IconBase);
            IconImage = icon.AddComponent<Image>();
            icon.AddComponent<Button>();
            new RectValues(new Vector2(16f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(512f, 512f)).AssignToRectTransform(IconImage.rectTransform);

            var selectIcon = EditorPrefabHolder.Instance.Function2Button.Duplicate(IconBase, "select");
            new RectValues(new Vector2(240f, -62f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(150f, 32f)).AssignToRectTransform(selectIcon.transform.AsRT());
            var selectIconStorage = selectIcon.GetComponent<FunctionButtonStorage>();
            SelectIconButton = selectIconStorage.button;
            selectIconStorage.Text = "Browse";

            EditorThemeManager.ApplySelectable(SelectIconButton, ThemeGroup.Function_2);
            EditorThemeManager.ApplyGraphic(selectIconStorage.label, ThemeGroup.Function_2_Text);

            var collapser = EditorPrefabHolder.Instance.CollapseToggle.Duplicate(IconBase, "collapse");
            new RectValues(new Vector2(340f, -62f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(32f, 32f)).AssignToRectTransform(collapser.transform.AsRT());
            CollapseToggle = collapser.GetComponent<Toggle>();

            EditorThemeManager.ApplyToggle(CollapseToggle, ThemeGroup.Background_1);

            for (int i = 0; i < collapser.transform.Find("dots").childCount; i++)
                EditorThemeManager.ApplyGraphic(collapser.transform.Find("dots").GetChild(i).GetComponent<Image>(), ThemeGroup.Dark_Text);

            #endregion

            #endregion

            #region Editor Themes

            EditorThemeManager.ApplyGraphic(Content.Find("title/Panel/icon").GetComponent<Image>(), ThemeGroup.Light_Text);
            EditorThemeManager.ApplyLightText(Content.Find("title/title").GetComponent<Text>());
            EditorThemeManager.ApplyLightText(Content.Find("name/title").GetComponent<Text>());
            EditorThemeManager.ApplyLightText(Content.Find("offset/title").GetComponent<Text>());
            EditorThemeManager.ApplyLightText(Content.Find("type/title").GetComponent<Text>());

            EditorThemeManager.ApplyInputField(NameField);
            EditorThemeManager.ApplyGraphic(OffsetSlider.transform.Find("Background").GetComponent<Image>(), ThemeGroup.Slider_2, true);

            EditorThemeManager.ApplyGraphic(OffsetSlider.image, ThemeGroup.Slider_2_Handle, true);
            EditorThemeManager.ApplyInputField(OffsetField);

            EditorThemeManager.ApplyGraphic(TypeButton.button.image, ThemeGroup.Null, true);

            EditorThemeManager.ApplyInputField(DescriptionField);

            EditorThemeManager.ApplyInputField(SelectionSearchField, ThemeGroup.Search_Field_2);

            EditorThemeManager.ApplyGraphic(selection.GetComponent<Image>(), ThemeGroup.Background_3, true);

            EditorThemeManager.ApplyGraphic(GameObject.transform.Find("submit/submit").GetComponent<Image>(), ThemeGroup.Add, true);
            EditorThemeManager.ApplyGraphic(GameObject.transform.Find("submit/submit/Text").GetComponent<Text>(), ThemeGroup.Add_Text);

            var scrollbar = selection.Find("scrollbar");
            EditorThemeManager.ApplyScrollbar(scrollbar.GetComponent<Scrollbar>(), scrollbar.GetComponent<Image>(), ThemeGroup.Scrollbar_2, ThemeGroup.Scrollbar_2_Handle);

            #endregion
        }

        public void CollapseIcon(bool collapse)
        {
            var size = collapse ? 32f : 512f;
            IconImage.rectTransform.sizeDelta = new Vector2(size, size);
            IconBase.transform.AsRT().sizeDelta = new Vector2(764f, collapse ? 94f : 574f);

            LayoutRebuilder.ForceRebuildLayoutImmediate(Content);
        }

        void SetupTab(string name)
        {
            var timelineObjectsTab = EditorPrefabHolder.Instance.Function1Button.Duplicate(SelectionTabsContent);
            var timelineObjectsTabStorage = timelineObjectsTab.GetComponent<FunctionButtonStorage>();

            timelineObjectsTabStorage.Text = name;
            SelectionTabButtons.Add(timelineObjectsTabStorage.button);

            EditorThemeManager.ApplyGraphic(timelineObjectsTabStorage.button.image, ThemeGroup.Function_1, true);
            EditorThemeManager.ApplyGraphic(timelineObjectsTabStorage.label, ThemeGroup.Function_1_Text);
        }

        public void ClearSelectionContent() => LSHelpers.DeleteChildren(SelectionContent);
    }
}
