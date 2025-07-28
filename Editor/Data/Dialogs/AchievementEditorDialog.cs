using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        public Transform DifficultyParent { get; set; }

        public List<Toggle> DifficultyToggles { get; set; } = new List<Toggle>();

        public ToggleButtonStorage HiddenToggle { get; set; }

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

            EditorThemeManager.AddGraphic(dialog.GetComponent<Image>(), ThemeGroup.Background_1);
            EditorThemeManager.AddGraphic(Right.GetComponent<Image>(), ThemeGroup.Background_3);

            var list = Right.Find("checkpoints");
            list.name = "achievements";
            Content = list.Find("viewport/content");

            SearchField = Right.Find("search").GetComponent<InputField>();
            SearchField.GetPlaceholderText().text = "Search for achievement...";
            EditorThemeManager.AddInputField(SearchField, ThemeGroup.Search_Field_2);

            ContentScrollbar = list.Find("Scrollbar Vertical").GetComponent<Scrollbar>();
            EditorThemeManager.AddScrollbar(ContentScrollbar, scrollbarGroup: ThemeGroup.Scrollbar_2, handleGroup: ThemeGroup.Scrollbar_2_Handle);

            CoreHelper.DestroyChildren(Left);

            var newLeft = Left.gameObject.Duplicate(Left, "left");
            LeftContent = newLeft.transform.AsRT();

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
            EditorThemeManager.AddGraphic(image, ThemeGroup.Background_2, true);

            new Labels(Labels.InitSettings.Default.Parent(LeftContent), "Name");
            var name = EditorPrefabHolder.Instance.StringInputField.Duplicate(LeftContent, "name");
            NameField = name.GetComponent<InputField>();
            NameField.GetPlaceholderText().text = "Set name...";
            EditorThemeManager.AddInputField(NameField);
            
            new Labels(Labels.InitSettings.Default.Parent(LeftContent), "Description");
            var description = EditorPrefabHolder.Instance.StringInputField.Duplicate(LeftContent, "desc");
            DescriptionField = description.GetComponent<InputField>();
            DescriptionField.GetPlaceholderText().text = "Set description...";
            EditorThemeManager.AddInputField(DescriptionField);

            new Labels(Labels.InitSettings.Default.Parent(LeftContent), "Icon");
            var iconBase = Creator.NewUIObject("icon base", LeftContent);
            var icon = Creator.NewUIObject("icon", iconBase.transform);
            IconImage = icon.AddComponent<Image>();
            RectValues.LeftAnchored.SizeDelta(100f, 100f).AssignToRectTransform(IconImage.rectTransform);

            var selectIcon = EditorPrefabHolder.Instance.Function2Button.Duplicate(iconBase.transform, "select");
            SelectIconButton = selectIcon.GetComponent<FunctionButtonStorage>();
            SelectIconButton.label.text = "Select Icon";
            EditorThemeManager.AddSelectable(SelectIconButton.button, ThemeGroup.Function_2);
            EditorThemeManager.AddGraphic(SelectIconButton.label, ThemeGroup.Function_2_Text);
            RectValues.Default.AnchoredPosition(75f, 0f).SizeDelta(200f, 32f).AssignToRectTransform(selectIcon.transform.AsRT());

            new Labels(Labels.InitSettings.Default.Parent(LeftContent), new Label("Achievement hidden until unlocked") { horizontalWrap = HorizontalWrapMode.Overflow });
            var hidden = EditorPrefabHolder.Instance.ToggleButton.Duplicate(LeftContent, "hidden");
            HiddenToggle = hidden.GetComponent<ToggleButtonStorage>();
            HiddenToggle.label.text = "Hidden";
            EditorThemeManager.AddToggle(HiddenToggle.toggle, graphic: HiddenToggle.label);

            new Labels(Labels.InitSettings.Default.Parent(LeftContent), new Label("Difficulty"));
            var difficultyParent = Creator.NewUIObject("difficulty", LeftContent);
            DifficultyParent = difficultyParent.transform;
            var difficultyLayout = difficultyParent.AddComponent<GridLayoutGroup>();
            difficultyLayout.cellSize = new Vector2(84f, 32f);
            difficultyLayout.spacing = new Vector2(4f, 4f);

            new Labels(Labels.InitSettings.Default.Parent(LeftContent), new Label("Show Achievement notification") { horizontalWrap = HorizontalWrapMode.Overflow });
            var preview = EditorPrefabHolder.Instance.Function2Button.Duplicate(LeftContent, "preview");
            PreviewButton = preview.GetComponent<FunctionButtonStorage>();
            PreviewButton.label.text = "Preview";
            EditorThemeManager.AddSelectable(PreviewButton.button, ThemeGroup.Function_2);
            EditorThemeManager.AddGraphic(PreviewButton.label, ThemeGroup.Function_2_Text);

            #endregion
        }
    }
}
