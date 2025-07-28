using UnityEngine;
using UnityEngine.UI;

using LSFunctions;

using BetterLegacy.Core;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Prefabs;
using BetterLegacy.Editor.Managers;

namespace BetterLegacy.Editor.Data.Dialogs
{
    public class CheckpointEditorDialog : EditorDialog, IContentUI, IIndexDialog
    {
        public CheckpointEditorDialog() : base(CHECKPOINT_EDITOR) { }

        #region Left

        public RectTransform Left { get; set; }

        #region Edit

        public Transform Edit { get; set; }
        public Button JumpToStartButton { get; set; }
        public Button JumpToPrevButton { get; set; }
        public Text KeyframeIndexer { get; set; }
        public Button JumpToNextButton { get; set; }
        public Button JumpToLastButton { get; set; }
        public FunctionButtonStorage CopyButton { get; set; }
        public FunctionButtonStorage PasteButton { get; set; }
        public DeleteButtonStorage DeleteButton { get; set; }

        #endregion

        public InputField NameField { get; set; }

        public InputFieldStorage TimeField { get; set; }

        public Vector2InputFieldStorage PositionFields { get; set; }

        public ToggleButtonStorage RespawnToggle { get; set; }
        public ToggleButtonStorage HealToggle { get; set; }
        public ToggleButtonStorage SetTimeToggle { get; set; }
        public ToggleButtonStorage ReverseToggle { get; set; }

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

            #region Setup

            var dialog = GameObject.transform;

            Left = dialog.Find("data/left").AsRT();
            Right = dialog.Find("data/right").AsRT();

            EditorThemeManager.AddGraphic(dialog.GetComponent<Image>(), ThemeGroup.Background_1);
            EditorThemeManager.AddGraphic(Right.GetComponent<Image>(), ThemeGroup.Background_3);

            Content = Right.Find("checkpoints/viewport/content");

            SearchField = Right.Find("search").GetComponent<InputField>();
            EditorThemeManager.AddInputField(SearchField, ThemeGroup.Search_Field_2);

            ContentScrollbar = Right.Find("checkpoints/Scrollbar Vertical").GetComponent<Scrollbar>();
            EditorThemeManager.AddScrollbar(ContentScrollbar, scrollbarGroup: ThemeGroup.Scrollbar_2, handleGroup: ThemeGroup.Scrollbar_2_Handle);

            Edit = Left.Find("edit");
            RTEditor.inst.SetupIndexer(this);

            for (int i = 0; i < Edit.childCount; i++)
            {
                var button = Edit.GetChild(i);
                var buttonComponent = button.GetComponent<Button>();

                if (!buttonComponent)
                    continue;

                if (button.name == "del")
                {
                    var buttonBG = button.GetChild(0).GetComponent<Image>();

                    EditorThemeManager.AddGraphic(buttonBG, ThemeGroup.Delete_Keyframe_BG);

                    EditorThemeManager.AddSelectable(buttonComponent, ThemeGroup.Delete_Keyframe_Button, false);

                    continue;
                }

                CoreHelper.Destroy(button.GetComponent<Animator>());
                buttonComponent.transition = Selectable.Transition.ColorTint;

                EditorThemeManager.AddSelectable(buttonComponent, ThemeGroup.Function_2, false);
            }

            // Labels
            for (int i = 0; i < Left.childCount; i++)
            {
                var label = Left.GetChild(i);

                if (!(label.name == "label" || label.name == "curves_label"))
                    continue;

                for (int j = 0; j < label.childCount; j++)
                    EditorThemeManager.AddLightText(label.GetChild(j).GetComponent<Text>());
            }

            NameField = Left.Find("name").GetComponent<InputField>();
            EditorThemeManager.AddInputField(NameField);
            var time = Left.Find("time");
            TimeField = time.gameObject.AddComponent<InputFieldStorage>();
            TimeField.Assign();
            EditorThemeManager.AddInputField(TimeField);
            for (int i = 1; i < time.childCount; i++)
            {
                var button = time.GetChild(i);
                var buttonComponent = button.GetComponent<Button>();

                CoreHelper.Destroy(button.GetComponent<Animator>());
                buttonComponent.transition = Selectable.Transition.ColorTint;
            }

            var position = Left.Find("position");
            PositionFields = position.gameObject.AddComponent<Vector2InputFieldStorage>();
            PositionFields.Assign();
            EditorThemeManager.AddInputField(PositionFields.x);
            EditorThemeManager.AddInputField(PositionFields.y);
            for (int i = 0; i < position.childCount; i++)
            {
                var child = position.GetChild(i);
                for (int j = 1; j < child.childCount; j++)
                {
                    var button = child.GetChild(j);
                    var buttonComponent = button.GetComponent<Button>();

                    CoreHelper.Destroy(button.GetComponent<Animator>());
                    buttonComponent.transition = Selectable.Transition.ColorTint;

                    EditorThemeManager.AddSelectable(buttonComponent, ThemeGroup.Function_2, false);
                }
            }

            new Labels(Labels.InitSettings.Default.Parent(Left), "Respawn Dead Players");
            var respawn = EditorPrefabHolder.Instance.ToggleButton.Duplicate(Left);
            RespawnToggle = respawn.GetComponent<ToggleButtonStorage>();
            RespawnToggle.label.text = "Respawn";
            EditorThemeManager.AddToggle(RespawnToggle.toggle, graphic: RespawnToggle.label);

            new Labels(Labels.InitSettings.Default.Parent(Left), "Heal Players");
            var heal = EditorPrefabHolder.Instance.ToggleButton.Duplicate(Left);
            HealToggle = heal.GetComponent<ToggleButtonStorage>();
            HealToggle.label.text = "Heal";
            EditorThemeManager.AddToggle(HealToggle.toggle, graphic: HealToggle.label);
            
            new Labels(Labels.InitSettings.Default.Parent(Left), "Set time on reverse");
            var setTime = EditorPrefabHolder.Instance.ToggleButton.Duplicate(Left);
            SetTimeToggle = setTime.GetComponent<ToggleButtonStorage>();
            SetTimeToggle.label.text = "Set Time";
            EditorThemeManager.AddToggle(SetTimeToggle.toggle, graphic: SetTimeToggle.label);
            
            new Labels(Labels.InitSettings.Default.Parent(Left), "Reverse level on death");
            var reverse = EditorPrefabHolder.Instance.ToggleButton.Duplicate(Left);
            ReverseToggle = reverse.GetComponent<ToggleButtonStorage>();
            ReverseToggle.label.text = "Reverse";
            EditorThemeManager.AddToggle(ReverseToggle.toggle, graphic: ReverseToggle.label);

            #endregion
        }
    }
}
