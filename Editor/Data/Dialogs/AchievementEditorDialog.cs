using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;

using LSFunctions;

using BetterLegacy.Core;
using BetterLegacy.Core.Components;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Prefabs;
using BetterLegacy.Editor.Managers;

namespace BetterLegacy.Editor.Data.Dialogs
{
    public class AchievementEditorDialog : EditorDialog, IContentUI
    {
        public AchievementEditorDialog() : base() { }

        #region Left

        public RectTransform Left { get; set; }
        public RectTransform LeftContent { get; set; }

        public RectTransform IDBase { get; set; }
        public Text IDText { get; set; }
        public InputField NameField { get; set; }

        public InputField DescriptionField { get; set; }

        public Image IconImage { get; set; }

        public FunctionButtonStorage SelectIconButton { get; set; }

        public Image LockedIconImage { get; set; }

        public FunctionButtonStorage SelectLockedIconButton { get; set; }
        public FunctionButtonStorage RemoveLockedIconButton { get; set; }

        public Transform DifficultyParent { get; set; }

        public List<Toggle> DifficultyToggles { get; set; } = new List<Toggle>();

        public ToggleButtonStorage HiddenToggle { get; set; }
        public InputField HintField { get; set; }
        public ToggleButtonStorage SharedToggle { get; set; }

        public FunctionButtonStorage PreviewButton { get; set; }

        #endregion

        #region Right

        public RectTransform Right { get; set; }

        public InputField SearchField { get; set; }

        public Transform Content { get; set; }

        public GridLayoutGroup Grid { get; set; }

        public Scrollbar ContentScrollbar { get; set; }

        public string SearchTerm { get => SearchField.text; set => SearchField.text = value; }

        public void ClearContent() => LSHelpers.DeleteChildren(Content);

        #endregion

        public override void Init()
        {
            if (init)
                return;

            base.Init();

            var editorDialogObject = RTCheckpointEditor.inst.Dialog.GameObject.Duplicate(EditorManager.inst.dialogs, "AchievementEditorDialog");
            editorDialogObject.transform.AsRT().sizeDelta = new Vector2(0f, 32f);
            CoreHelper.Destroy(editorDialogObject.GetComponent<Clickable>());

            EditorHelper.AddEditorDialog(ACHIEVEMENT_EDITOR_DIALOG, editorDialogObject);

            InitDialog(ACHIEVEMENT_EDITOR_DIALOG);

            #region Setup

            var dialog = GameObject.transform;

            Left = dialog.Find("data/left").AsRT();
            Right = dialog.Find("data/right").AsRT();

            var titleBar = dialog.Find("titlebar");
            var titleBarLeft = titleBar.Find("left");
            titleBarLeft.Find("bg").GetComponent<Image>().color = RTColors.HexToColor("4935FF");
            titleBarLeft.Find("title").GetComponent<Text>().text = "- Current Achievement Props -";
            var titleBarRight = titleBar.Find("right");
            titleBarRight.Find("bg").GetComponent<Image>().color = RTColors.HexToColor("3A25F7");
            titleBarRight.Find("title").GetComponent<Text>().text = "- Achievement List -";

            EditorThemeManager.ApplyGraphic(dialog.GetComponent<Image>(), ThemeGroup.Background_1);
            EditorThemeManager.ApplyGraphic(Right.GetComponent<Image>(), ThemeGroup.Background_3);

            var list = Right.Find("checkpoints");
            list.name = "achievements";
            Content = list.Find("viewport/content");

            SearchField = Right.Find("search").GetComponent<InputField>();
            SearchField.GetPlaceholderText().text = "Search for achievement...";
            EditorThemeManager.ApplyInputField(SearchField, ThemeGroup.Search_Field_2);

            ContentScrollbar = list.Find("Scrollbar Vertical").GetComponent<Scrollbar>();
            EditorThemeManager.ApplyScrollbar(ContentScrollbar, scrollbarGroup: ThemeGroup.Scrollbar_2, handleGroup: ThemeGroup.Scrollbar_2_Handle);

            CoreHelper.DestroyChildren(Left);

            var scrollView = EditorPrefabHolder.Instance.ScrollView.Duplicate(Left, "Scroll View");
            LeftContent = scrollView.transform.Find("Viewport/Content").AsRT();

            scrollView.transform.AsRT().anchoredPosition = new Vector2(392.5f, 320f);
            scrollView.transform.AsRT().sizeDelta = new Vector2(735f, 638f);

            LeftContent.GetComponent<VerticalLayoutGroup>().spacing = 8f;

            var id = EditorPrefabHolder.Instance.Labels.Duplicate(LeftContent, "id");

            IDBase = id.transform.AsRT();
            IDText = IDBase.Find("text").GetComponent<Text>();

            IDBase.sizeDelta = new Vector2(515, 32f);
            IDText.rectTransform.sizeDelta = new Vector2(226f, 32f);

            IDText.fontSize = 18;
            IDText.text = "ID:";
            IDText.alignment = TextAnchor.MiddleLeft;
            IDText.horizontalOverflow = HorizontalWrapMode.Overflow;

            var image = id.AddComponent<Image>();
            EditorThemeManager.ApplyGraphic(image, ThemeGroup.Background_2, true);

            new Labels(Labels.InitSettings.Default.Parent(LeftContent), "Name");
            var name = EditorPrefabHolder.Instance.StringInputField.Duplicate(LeftContent, "name");
            NameField = name.GetComponent<InputField>();
            NameField.GetPlaceholderText().text = "Set name...";
            EditorThemeManager.ApplyInputField(NameField);
            
            new Labels(Labels.InitSettings.Default.Parent(LeftContent), "Description");
            var description = EditorPrefabHolder.Instance.StringInputField.Duplicate(LeftContent, "desc");
            DescriptionField = description.GetComponent<InputField>();
            DescriptionField.GetPlaceholderText().text = "Set description...";
            EditorThemeManager.ApplyInputField(DescriptionField);

            new Labels(Labels.InitSettings.Default.Parent(LeftContent), "Icon");
            var iconBase = Creator.NewUIObject("icon base", LeftContent);
            var icon = Creator.NewUIObject("icon", iconBase.transform);
            IconImage = icon.AddComponent<Image>();
            RectValues.LeftAnchored.SizeDelta(100f, 100f).AssignToRectTransform(IconImage.rectTransform);

            var selectIcon = EditorPrefabHolder.Instance.Function2Button.Duplicate(iconBase.transform, "select");
            SelectIconButton = selectIcon.GetComponent<FunctionButtonStorage>();
            SelectIconButton.Text = "Select Icon";
            EditorThemeManager.ApplySelectable(SelectIconButton.button, ThemeGroup.Function_2);
            EditorThemeManager.ApplyGraphic(SelectIconButton.label, ThemeGroup.Function_2_Text);
            RectValues.Default.AnchoredPosition(75f, 0f).SizeDelta(200f, 32f).AssignToRectTransform(selectIcon.transform.AsRT());
            
            new Labels(Labels.InitSettings.Default.Parent(LeftContent), "Locked Icon");
            var lockedIconBase = Creator.NewUIObject("icon base", LeftContent);
            var lockedIcon = Creator.NewUIObject("icon", lockedIconBase.transform);
            LockedIconImage = lockedIcon.AddComponent<Image>();
            RectValues.LeftAnchored.SizeDelta(100f, 100f).AssignToRectTransform(LockedIconImage.rectTransform);

            var selectLockedIcon = EditorPrefabHolder.Instance.Function2Button.Duplicate(lockedIconBase.transform, "select");
            SelectLockedIconButton = selectLockedIcon.GetComponent<FunctionButtonStorage>();
            SelectLockedIconButton.Text = "Select Locked Icon";
            EditorThemeManager.ApplySelectable(SelectLockedIconButton.button, ThemeGroup.Function_2);
            EditorThemeManager.ApplyGraphic(SelectLockedIconButton.label, ThemeGroup.Function_2_Text);
            RectValues.Default.AnchoredPosition(75f, 20f).SizeDelta(200f, 32f).AssignToRectTransform(selectLockedIcon.transform.AsRT());
            
            var removeLockedIcon = EditorPrefabHolder.Instance.Function2Button.Duplicate(lockedIconBase.transform, "remove");
            RemoveLockedIconButton = removeLockedIcon.GetComponent<FunctionButtonStorage>();
            RemoveLockedIconButton.Text = "Remove Locked Icon";
            EditorThemeManager.ApplySelectable(RemoveLockedIconButton.button, ThemeGroup.Function_2);
            EditorThemeManager.ApplyGraphic(RemoveLockedIconButton.label, ThemeGroup.Function_2_Text);
            RectValues.Default.AnchoredPosition(75f, -20f).SizeDelta(200f, 32f).AssignToRectTransform(removeLockedIcon.transform.AsRT());

            new Labels(Labels.InitSettings.Default.Parent(LeftContent), new Label("Achievement hidden until unlocked") { horizontalWrap = HorizontalWrapMode.Overflow });
            var hidden = EditorPrefabHolder.Instance.ToggleButton.Duplicate(LeftContent, "hidden");
            HiddenToggle = hidden.GetComponent<ToggleButtonStorage>();
            HiddenToggle.Text = "Hidden";
            EditorThemeManager.ApplyToggle(HiddenToggle.toggle, graphic: HiddenToggle.label);

            new Labels(Labels.InitSettings.Default.Parent(LeftContent), "Hint");
            var hint = EditorPrefabHolder.Instance.StringInputField.Duplicate(LeftContent, "hint");
            HintField = hint.GetComponent<InputField>();
            HintField.GetPlaceholderText().text = "Set hint (empty for default)";
            EditorThemeManager.ApplyInputField(HintField);

            new Labels(Labels.InitSettings.Default.Parent(LeftContent), new Label("Unlocked state is shared everywhere") { horizontalWrap = HorizontalWrapMode.Overflow });
            var shared = EditorPrefabHolder.Instance.ToggleButton.Duplicate(LeftContent, "shared");
            SharedToggle = shared.GetComponent<ToggleButtonStorage>();
            SharedToggle.Text = "Shared";
            EditorThemeManager.ApplyToggle(SharedToggle.toggle, graphic: SharedToggle.label);

            new Labels(Labels.InitSettings.Default.Parent(LeftContent), new Label("Difficulty"));
            var difficultyParent = Creator.NewUIObject("difficulty", LeftContent);
            DifficultyParent = difficultyParent.transform;
            var difficultyLayout = difficultyParent.AddComponent<GridLayoutGroup>();
            difficultyLayout.cellSize = new Vector2(84f, 32f);
            difficultyLayout.spacing = new Vector2(4f, 4f);

            new Labels(Labels.InitSettings.Default.Parent(LeftContent), new Label("Show Achievement notification") { horizontalWrap = HorizontalWrapMode.Overflow });
            var preview = EditorPrefabHolder.Instance.Function2Button.Duplicate(LeftContent, "preview");
            PreviewButton = preview.GetComponent<FunctionButtonStorage>();
            PreviewButton.Text = "Preview";
            EditorThemeManager.ApplySelectable(PreviewButton.button, ThemeGroup.Function_2);
            EditorThemeManager.ApplyGraphic(PreviewButton.label, ThemeGroup.Function_2_Text);

            #endregion
        }
    }
}
