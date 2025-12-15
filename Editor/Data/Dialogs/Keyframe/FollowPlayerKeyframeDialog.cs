using UnityEngine;
using UnityEngine.UI;

using BetterLegacy.Core;
using BetterLegacy.Core.Prefabs;
using BetterLegacy.Core.Runtime.Events;
using BetterLegacy.Editor.Managers;

namespace BetterLegacy.Editor.Data.Dialogs
{
    public class FollowPlayerKeyframeDialog : KeyframeDialog
    {
        public FollowPlayerKeyframeDialog() : base(EventEngine.FOLLOW_PLAYER) { }

        public Toggle ActiveToggle { get; set; }

        public Toggle MoveToggle { get; set; }

        public Toggle RotateToggle { get; set; }

        public InputFieldStorage SharpnessField { get; set; }

        public InputFieldStorage OffsetField { get; set; }

        public InputFieldStorage LimitLeftField { get; set; }

        public InputFieldStorage LimitRightField { get; set; }
        
        public InputFieldStorage LimitUpField { get; set; }

        public InputFieldStorage LimitDownField { get; set; }

        public InputFieldStorage AnchorField { get; set; }

        public override void Init()
        {
            CreateNew(EventEditor.inst.dialogRight);

            base.Init();

            new LabelsElement("Camera Follows Player").Init(EditorElement.InitSettings.Default.Parent(GameObject.transform));
            var active = EditorPrefabHolder.Instance.ToggleButton.Duplicate(GameObject.transform, "active");
            var activeToggle = active.GetComponent<ToggleButtonStorage>();
            activeToggle.Text = "Active";
            ActiveToggle = activeToggle.toggle;

            new LabelsElement("Move Enabled").Init(EditorElement.InitSettings.Default.Parent(GameObject.transform));
            var move = EditorPrefabHolder.Instance.ToggleButton.Duplicate(GameObject.transform, "move");
            var moveToggle = move.GetComponent<ToggleButtonStorage>();
            moveToggle.Text = "Move";
            MoveToggle = moveToggle.toggle;

            new LabelsElement("Rotate Enabled").Init(EditorElement.InitSettings.Default.Parent(GameObject.transform));
            var rotate = EditorPrefabHolder.Instance.ToggleButton.Duplicate(GameObject.transform, "rotate");
            var rotateToggle = rotate.GetComponent<ToggleButtonStorage>();
            rotateToggle.Text = "Rotate";
            RotateToggle = rotateToggle.toggle;

            new LabelsElement("Sharpness", "Offset").Init(EditorElement.InitSettings.Default.Parent(GameObject.transform));
            var position = EditorPrefabHolder.Instance.Vector2InputFields.Duplicate(GameObject.transform, "position");
            var positionFields = position.GetComponent<Vector2InputFieldStorage>();
            SharpnessField = positionFields.x;
            OffsetField = positionFields.y;

            new LabelsElement("Limit Left", "Limit Right").Init(EditorElement.InitSettings.Default.Parent(GameObject.transform));
            var limitHorizontal = EditorPrefabHolder.Instance.Vector2InputFields.Duplicate(GameObject.transform, "limit horizontal");
            var limitHorizontalFields = limitHorizontal.GetComponent<Vector2InputFieldStorage>();
            LimitLeftField = limitHorizontalFields.x;
            LimitRightField = limitHorizontalFields.y;

            new LabelsElement("Limit Up", "Limit Down").Init(EditorElement.InitSettings.Default.Parent(GameObject.transform));
            var limitVertical = EditorPrefabHolder.Instance.Vector2InputFields.Duplicate(GameObject.transform, "limit vertical");
            var limitVerticalFields = limitVertical.GetComponent<Vector2InputFieldStorage>();
            LimitUpField = limitVerticalFields.x;
            LimitDownField = limitVerticalFields.y;

            new LabelsElement("Anchor").Init(EditorElement.InitSettings.Default.Parent(GameObject.transform));
            var anchor = EditorPrefabHolder.Instance.LayoutInputField.Duplicate(GameObject.transform, "anchor");
            AnchorField = anchor.transform.Find("x").gameObject.GetOrAddComponent<InputFieldStorage>();
            AnchorField.inputField.image.rectTransform.sizeDelta = new Vector2(317f, 32f);

            EditorThemeManager.ApplyToggle(ActiveToggle);
            EditorThemeManager.ApplyToggle(MoveToggle);
            EditorThemeManager.ApplyToggle(RotateToggle);

            EditorThemeManager.ApplyInputField(SharpnessField);
            EditorThemeManager.ApplyInputField(OffsetField);
            EditorThemeManager.ApplyInputField(LimitLeftField);
            EditorThemeManager.ApplyInputField(LimitRightField);
            EditorThemeManager.ApplyInputField(LimitUpField);
            EditorThemeManager.ApplyInputField(LimitDownField);
            EditorThemeManager.ApplyInputField(AnchorField);
        }

        public override void Render()
        {
            base.Render();

            SetToggle(ActiveToggle, 0, 1, 0);
            SetToggle(MoveToggle, 1, 1, 0);
            SetToggle(RotateToggle, 2, 1, 0);
            SetFloatInputField(SharpnessField, 3, 0.1f, 10f, 0.001f, 1f, allowNegative: false);
            SetFloatInputField(OffsetField, 4);
            SetFloatInputField(LimitLeftField, 5);
            SetFloatInputField(LimitRightField, 6);
            SetFloatInputField(LimitUpField, 7);
            SetFloatInputField(LimitDownField, 8);
            SetFloatInputField(AnchorField, 9, 0.1f, 10f);
        }
    }
}
