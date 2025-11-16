using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;

using BetterLegacy.Core;
using BetterLegacy.Core.Components;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Prefabs;
using BetterLegacy.Editor.Managers;

namespace BetterLegacy.Editor.Data.Dialogs
{
    public class AnimationEditorDialog : EditorDialog, IAnimationDialog
    {
        public AnimationEditorDialog() : base() { }

        #region Animation Values

        public RectTransform Content { get; set; }

        #region Top Properties

        public Button ReturnButton { get; set; }
        public RectTransform IDBase { get; set; }
        public Text IDText { get; set; }

        public InputField ReferenceField { get; set; }

        #endregion

        #region Info

        public InputField NameField { get; set; }

        public InputField DescriptionField { get; set; }

        #endregion

        #region Start Time

        public InputFieldStorage StartTimeField { get; set; }

        #endregion

        #region Animation States

        public Toggle AnimatePositionToggle { get; set; }
        public Toggle AnimateScaleToggle { get; set; }
        public Toggle AnimateRotationToggle { get; set; }
        public Toggle AnimateColorToggle { get; set; }
        public Toggle TransitionToggle { get; set; }

        #endregion

        #endregion

        #region Keyframe Editors

        /// <summary>
        /// The currently open object keyframe editor.
        /// </summary>
        public KeyframeDialog CurrentKeyframeDialog { get; set; }

        /// <summary>
        /// A list containing all the event keyframe editors.
        /// </summary>
        public List<KeyframeDialog> keyframeDialogs = new List<KeyframeDialog>();
        public List<KeyframeDialog> KeyframeDialogs { get => keyframeDialogs; set => keyframeDialogs = value; }

        public List<Toggle> startColorToggles = new List<Toggle>();
        public List<Toggle> endColorToggles = new List<Toggle>();

        public KeyframeTimeline Timeline { get; set; }

        #endregion

        #region Methods

        public override void Init()
        {
            if (init)
                return;

            base.Init();

            var editorDialogObject = ObjectEditor.inst.Dialog.GameObject.Duplicate(EditorManager.inst.dialogs, "AnimationEditor");
            editorDialogObject.transform.AsRT().sizeDelta = new Vector2(0f, 32f);
            CoreHelper.Destroy(editorDialogObject.GetComponent<Clickable>());

            EditorHelper.AddEditorDialog(ANIMATION_EDITOR_DIALOG, editorDialogObject);

            InitDialog(ANIMATION_EDITOR_DIALOG);

            #region Setup

            var data = GameObject.transform.Find("data").AsRT();
            Content = data.Find("left/Scroll View/Viewport/Content").AsRT();

            var panel = data.Find("left").GetChild(0);
            panel.Find("bg").GetComponent<Image>().color = new Color(1f, 0.24f, 0.24f);
            panel.Find("text").GetComponent<Text>().text = "- Animation Editor -";

            CoreHelper.DestroyChildren(Content);

            var returnButton = EditorPrefabHolder.Instance.Function2Button.Duplicate(Content, "return");
            var returnButtonStorage = returnButton.GetComponent<FunctionButtonStorage>();
            ReturnButton = returnButtonStorage.button;
            returnButtonStorage.Text = "Return";
            EditorThemeManager.AddSelectable(ReturnButton, ThemeGroup.Function_2);
            EditorThemeManager.AddGraphic(returnButtonStorage.label, ThemeGroup.Function_2_Text);

            SetupID();
            SetupReference();
            SetupName();
            SetupDescription();
            SetupStartTime();
            SetupStates();

            var keyframeDialogsParent = GameObject.transform.Find("data/right");
            var colorDialog = keyframeDialogsParent.Find("color");
            var endColor = colorDialog.Find("gradient_color");

            startColorToggles.Clear();
            for (int i = 1; i <= 18; i++)
                startColorToggles.Add(colorDialog.Find("color/" + i).GetComponent<Toggle>());

            endColorToggles.Clear();
            for (int i = 0; i < endColor.transform.childCount; i++)
                endColorToggles.Add(endColor.transform.GetChild(i).GetComponent<Toggle>());

            for (int i = 0; i < 5; i++)
            {
                var keyframeDialog = new KeyframeDialog(i);
                keyframeDialog.GameObject = keyframeDialogsParent.GetChild(i).gameObject;
                keyframeDialog.isMulti = i == 4;
                keyframeDialog.isObjectKeyframe = true;
                keyframeDialog.Init();
                if (keyframeDialog.RandomToggles != null)
                    keyframeDialog.RandomToggles.ForLoopReverse((toggle, index) =>
                    {
                        if (toggle.name != "homing-static" && toggle.name != "homing-dynamic")
                            return;

                        CoreHelper.Delete(toggle);
                        keyframeDialog.RandomToggles.RemoveAt(index);
                    });
                keyframeDialogs.Add(keyframeDialog);
            }

            Timeline = new KeyframeTimeline();
            Timeline.startColorsReference = startColorToggles;
            Timeline.endColorsReference = endColorToggles;
            Timeline.Init(this);

            #endregion
        }

        void SetupID()
        {
            var id = EditorPrefabHolder.Instance.Labels.Duplicate(Content, "id");
            IDBase = id.transform.AsRT();
            EditorHelper.SetComplexity(id, Complexity.Normal);

            IDBase.sizeDelta = new Vector2(515, 32f);
            IDBase.GetChild(0).AsRT().sizeDelta = new Vector2(226f, 32f);

            IDText = IDBase.GetChild(0).GetComponent<Text>();
            IDText.fontSize = 18;
            IDText.text = "ID:";
            IDText.alignment = TextAnchor.MiddleLeft;
            IDText.horizontalOverflow = HorizontalWrapMode.Overflow;

            var image = id.AddComponent<Image>();
            EditorThemeManager.AddGraphic(image, ThemeGroup.Background_2, true);
            EditorThemeManager.AddLightText(IDText);
        }

        void SetupReference()
        {
            new Labels(Labels.InitSettings.Default.Parent(Content), "Reference");
            var reference = EditorPrefabHolder.Instance.StringInputField.Duplicate(Content, "reference");
            reference.transform.AsRT().sizeDelta = new Vector2(0f, 32f);
            ReferenceField = reference.GetComponent<InputField>();
            EditorThemeManager.AddInputField(ReferenceField);
            TooltipHelper.AssignTooltip(reference, "Animation Reference ID");
        }

        void SetupName()
        {
            new Labels(Labels.InitSettings.Default.Parent(Content), "Name");
            var name = EditorPrefabHolder.Instance.StringInputField.Duplicate(Content, "name");
            name.transform.AsRT().sizeDelta = new Vector2(0f, 32f);
            NameField = name.GetComponent<InputField>();
            EditorThemeManager.AddInputField(NameField);
        }

        void SetupDescription()
        {
            new Labels(Labels.InitSettings.Default.Parent(Content), "Description");
            var description = EditorPrefabHolder.Instance.StringInputField.Duplicate(Content, "desc");
            description.transform.AsRT().sizeDelta = new Vector2(0f, 200f);
            DescriptionField = description.GetComponent<InputField>();
            DescriptionField.lineType = InputField.LineType.MultiLineNewline;
            DescriptionField.textComponent.alignment = TextAnchor.UpperLeft;
            DescriptionField.GetPlaceholderText().alignment = TextAnchor.UpperLeft;
            EditorThemeManager.AddInputField(DescriptionField);
        }

        void SetupStartTime()
        {
            new Labels(Labels.InitSettings.Default.Parent(Content), "Start Time");
            var startTime = EditorPrefabHolder.Instance.NumberInputField.Duplicate(Content, "start time");
            startTime.transform.AsRT().sizeDelta = new Vector2(0f, 32f);
            StartTimeField = startTime.GetComponent<InputFieldStorage>();
            EditorThemeManager.AddInputField(StartTimeField);
        }

        void SetupStates()
        {
            new Labels(Labels.InitSettings.Default.Parent(Content), "Animation States");

            var animatePosition = EditorPrefabHolder.Instance.ToggleButton.Duplicate(Content, "animate pos");
            var animatePositionStorage = animatePosition.GetComponent<ToggleButtonStorage>();
            AnimatePositionToggle = animatePositionStorage.toggle;
            animatePositionStorage.label.text = "Animate Position";
            EditorThemeManager.AddToggle(AnimatePositionToggle, graphic: animatePositionStorage.label);

            var animateScale = EditorPrefabHolder.Instance.ToggleButton.Duplicate(Content, "animate sca");
            var animateScaleStorage = animateScale.GetComponent<ToggleButtonStorage>();
            AnimateScaleToggle = animateScaleStorage.toggle;
            animateScaleStorage.label.text = "Animate Scale";
            EditorThemeManager.AddToggle(AnimateScaleToggle, graphic: animateScaleStorage.label);

            var animateRotation = EditorPrefabHolder.Instance.ToggleButton.Duplicate(Content, "animate rot");
            var animateRotationStorage = animateRotation.GetComponent<ToggleButtonStorage>();
            AnimateRotationToggle = animateRotationStorage.toggle;
            animateRotationStorage.label.text = "Animate Rotation";
            EditorThemeManager.AddToggle(AnimateRotationToggle, graphic: animateRotationStorage.label);

            var animateColor = EditorPrefabHolder.Instance.ToggleButton.Duplicate(Content, "animate col");
            var animateColorStorage = animateColor.GetComponent<ToggleButtonStorage>();
            AnimateColorToggle = animateColorStorage.toggle;
            animateColorStorage.label.text = "Animate Color";
            EditorThemeManager.AddToggle(AnimateColorToggle, graphic: animateColorStorage.label);

            var transition = EditorPrefabHolder.Instance.ToggleButton.Duplicate(Content, "transition");
            var transitionStorage = transition.GetComponent<ToggleButtonStorage>();
            TransitionToggle = transitionStorage.toggle;
            transitionStorage.label.text = "Transition Override";
            EditorThemeManager.AddToggle(TransitionToggle, graphic: transitionStorage.label);
            TooltipHelper.AssignTooltip(transition, "Animation Transition");
        }

        #endregion
    }
}
