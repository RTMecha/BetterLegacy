﻿using System.Collections.Generic;

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

        #endregion

        public override void Init()
        {
            if (init)
                return;

            base.Init();

            #region Setup

            GameObject.name = "PrefabCreatorDialog";
            EditorThemeManager.AddGraphic(GameObject.GetComponent<Image>(), ThemeGroup.Background_1);

            var prefabEditorData = GameObject.transform.Find("data");
            var scrollView = EditorPrefabHolder.Instance.ScrollView.Duplicate(GameObject.transform, "Scroll View", 1);
            scrollView.transform.AsRT().sizeDelta = new Vector2(765, 600f);
            Content = scrollView.transform.Find("Viewport/Content").AsRT();
            var array = new Transform[prefabEditorData.childCount];
            int num = 0;
            foreach (var child in prefabEditorData.GetChildren())
            {
                array[num] = child;
                num++;
            }
            for (int i = 0; i < array.Length; i++)
                array[i].SetParent(Content);
            CoreHelper.Delete(prefabEditorData);

            CoreHelper.Delete(Content.Find("offset/<").gameObject);
            CoreHelper.Delete(Content.Find("offset/>").gameObject);

            GameObject.transform.Find("spacer").AsRT().sizeDelta = new Vector2(753.49f, 32f);

            NameField = Content.Find("name/input").GetComponent<InputField>();

            OffsetSlider = Content.Find("offset/slider").GetComponent<Slider>();
            OffsetField = Content.Find("offset/input").GetComponent<InputField>();
            OffsetField.characterLimit = 0;

            CoreHelper.Delete(Content.Find("type/types").gameObject);

            var prefabType = EditorPrefabHolder.Instance.Function1Button.gameObject.Duplicate(Content.Find("type"), "Show Type Editor");
            TypeButton = prefabType.GetComponent<FunctionButtonStorage>();

            UIManager.SetRectTransform(prefabType.transform.AsRT(), new Vector2(-370f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 0.5f), new Vector2(652f, 34f));
            TypeButton.button.onClick.ClearAll();

            prefabType.AddComponent<ContrastColors>().Init(TypeButton.label, TypeButton.button.image);
            Content.Find("type").AsRT().sizeDelta = new Vector2(749f, 48f);

            Content.Find("spacer").AsRT().sizeDelta = new Vector2(749f, 32f);

            var descriptionGO = Content.Find("name").gameObject.Duplicate(Content, "description", 4);
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

            var selection = Content.Find("selection");
            EditorHelper.SetComplexity(selection.gameObject, Complexity.Advanced);
            selection.AsRT().sizeDelta = new Vector2(749f, 300f);
            SelectionSearchField = selection.Find("search-box/search").GetComponent<InputField>();

            SelectionContent = selection.Find("mask/content").AsRT();
            var selectionGroup = SelectionContent.GetComponent<GridLayoutGroup>();
            selectionGroup.cellSize = new Vector2(355f, 32f);
            selectionGroup.constraintCount = 2;

            #endregion

            #region Editor Themes

            EditorThemeManager.AddGraphic(Content.Find("title/Panel/icon").GetComponent<Image>(), ThemeGroup.Light_Text);
            EditorThemeManager.AddLightText(Content.Find("title/title").GetComponent<Text>());
            EditorThemeManager.AddLightText(Content.Find("name/title").GetComponent<Text>());
            EditorThemeManager.AddLightText(Content.Find("offset/title").GetComponent<Text>());
            EditorThemeManager.AddLightText(Content.Find("type/title").GetComponent<Text>());

            EditorThemeManager.AddInputField(NameField);
            EditorThemeManager.AddGraphic(OffsetSlider.transform.Find("Background").GetComponent<Image>(), ThemeGroup.Slider_2, true);

            EditorThemeManager.AddGraphic(OffsetSlider.image, ThemeGroup.Slider_2_Handle, true);
            EditorThemeManager.AddInputField(OffsetField);

            EditorThemeManager.AddGraphic(TypeButton.button.image, ThemeGroup.Null, true);

            EditorThemeManager.AddInputField(DescriptionField);

            EditorThemeManager.AddInputField(SelectionSearchField, ThemeGroup.Search_Field_2);

            EditorThemeManager.AddGraphic(selection.GetComponent<Image>(), ThemeGroup.Background_3, true);

            EditorThemeManager.AddGraphic(GameObject.transform.Find("submit/submit").GetComponent<Image>(), ThemeGroup.Add, true);
            EditorThemeManager.AddGraphic(GameObject.transform.Find("submit/submit/Text").GetComponent<Text>(), ThemeGroup.Add_Text);

            var scrollbar = selection.Find("scrollbar");
            EditorThemeManager.AddScrollbar(scrollbar.GetComponent<Scrollbar>(), scrollbar.GetComponent<Image>(), ThemeGroup.Scrollbar_2, ThemeGroup.Scrollbar_2_Handle);

            #endregion
        }

        void SetupTab(string name)
        {
            var timelineObjectsTab = EditorPrefabHolder.Instance.Function1Button.Duplicate(SelectionTabsContent);
            var timelineObjectsTabStorage = timelineObjectsTab.GetComponent<FunctionButtonStorage>();

            timelineObjectsTabStorage.Text = name;
            SelectionTabButtons.Add(timelineObjectsTabStorage.button);

            EditorThemeManager.AddGraphic(timelineObjectsTabStorage.button.image, ThemeGroup.Function_1, true);
            EditorThemeManager.AddGraphic(timelineObjectsTabStorage.label, ThemeGroup.Function_1_Text);
        }

        public void ClearSelectionContent() => LSHelpers.DeleteChildren(SelectionContent);
    }
}
