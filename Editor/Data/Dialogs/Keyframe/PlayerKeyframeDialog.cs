using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;

using BetterLegacy.Configs;
using BetterLegacy.Core;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Managers;
using BetterLegacy.Core.Prefabs;
using BetterLegacy.Editor.Managers;

namespace BetterLegacy.Editor.Data.Dialogs
{
    public class PlayerKeyframeDialog : KeyframeDialog
    {
        public PlayerKeyframeDialog() : base(EventLibrary.Indexes.PLAYER) { }

        public Toggle ActiveToggle { get; set; }

        public Toggle MoveToggle { get; set; }

        public Vector2InputFieldStorage PositionFields { get; set; }

        public InputFieldStorage RotationField { get; set; }

        public Toggle OutOfBoundsToggle { get; set; }

        public List<Toggle> ColorToggles { get; set; }

        public InputFieldStorage OpacityField { get; set; }

        public InputFieldStorage HueField { get; set; }

        public InputFieldStorage SaturationField { get; set; }

        public InputFieldStorage ValueField { get; set; }

        public override void Init()
        {
            CreateNew(EventEditor.inst.dialogRight);

            base.Init();

            new LabelsElement("Players Active").Init(EditorElement.InitSettings.Default.Parent(GameObject.transform));
            var active = EditorPrefabHolder.Instance.ToggleButton.Duplicate(GameObject.transform, "active");
            var activeToggle = active.GetComponent<ToggleButtonStorage>();
            activeToggle.Text = "Active";
            ActiveToggle = activeToggle.toggle;

            new LabelsElement("Can Move").Init(EditorElement.InitSettings.Default.Parent(GameObject.transform));
            var move = EditorPrefabHolder.Instance.ToggleButton.Duplicate(GameObject.transform, "move");
            var moveToggle = move.GetComponent<ToggleButtonStorage>();
            moveToggle.Text = "Moveable";
            MoveToggle = moveToggle.toggle;

            new LabelsElement("Can Exit Bounds").Init(EditorElement.InitSettings.Default.Parent(GameObject.transform));
            var outOfBounds = EditorPrefabHolder.Instance.ToggleButton.Duplicate(GameObject.transform, "oob");
            var outOfBoundsToggle = outOfBounds.GetComponent<ToggleButtonStorage>();
            outOfBoundsToggle.Text = "Out of Bounds";
            OutOfBoundsToggle = outOfBoundsToggle.toggle;

            new LabelsElement("Position X", "Position Y").Init(EditorElement.InitSettings.Default.Parent(GameObject.transform));
            var position = EditorPrefabHolder.Instance.Vector2InputFields.Duplicate(GameObject.transform, "position");
            var positionFields = position.GetComponent<Vector2InputFieldStorage>();
            PositionFields = positionFields;

            new LabelsElement("Rotation").Init(EditorElement.InitSettings.Default.Parent(GameObject.transform));
            var rotation = EditorPrefabHolder.Instance.LayoutInputField.Duplicate(GameObject.transform, "rotation");
            RotationField = rotation.transform.Find("x").gameObject.GetOrAddComponent<InputFieldStorage>();
            RotationField.inputField.image.rectTransform.sizeDelta = new Vector2(317f, 32f);

            new LabelsElement("Tail Color").Init(EditorElement.InitSettings.Default.Parent(GameObject.transform));
            var colors = new ColorGroupElement(19, new Vector2(366f, 64f), new Vector2(32f, 32f), new Vector2(5f, 5f));
            colors.Init(EditorElement.InitSettings.Default.Parent(GameObject.transform).Name("colors"));
            ColorToggles = colors.toggles;

            new LabelsElement(HorizontalOrVerticalLayoutValues.Horizontal.ChildControlHeight(false), "Opacity", "Hue", "Sat", "Val").Init(EditorElement.InitSettings.Default.Parent(GameObject.transform));
            var colorshift = EditorPrefabHolder.Instance.Vector4InputFields.Duplicate(GameObject.transform, "colorshift");
            var colorshiftFields = colorshift.GetComponent<Vector4InputFieldStorage>();
            colorshiftFields.SetSize(new Vector2(80f, 32f), new Vector2(50f, 32f));
            colorshiftFields.SetIndividualSpacing(4f);
            OpacityField = colorshiftFields.x;
            HueField = colorshiftFields.y;
            SaturationField = colorshiftFields.z;
            ValueField = colorshiftFields.w;

            EditorThemeManager.ApplyToggle(ActiveToggle);
            EditorThemeManager.ApplyToggle(MoveToggle);
            EditorThemeManager.ApplyToggle(OutOfBoundsToggle);
            EditorThemeManager.ApplyInputField(PositionFields);
            EditorThemeManager.ApplyInputField(RotationField);
            EditorThemeManager.ApplyInputField(OpacityField);
            EditorThemeManager.ApplyInputField(HueField);
            EditorThemeManager.ApplyInputField(SaturationField);
            EditorThemeManager.ApplyInputField(ValueField);
        }

        public override void Render()
        {
            base.Render();

            var currentKeyframe = RTEventEditor.inst.CurrentSelectedKeyframe;

            SetToggle(ActiveToggle, 0, 0, 1);
            SetToggle(MoveToggle, 1, 0, 1);
            SetVector2InputField(PositionFields, 2, 3);
            SetFloatInputField(RotationField, 4, 15f, 3f);
            SetToggle(OutOfBoundsToggle, 5, 1, 0);

            // Timeline Color
            SetListColor((int)currentKeyframe.values[6], 6, ColorToggles, ThemeManager.inst.Current.guiAccentColor, 7, 8, 9, 10);

            // Timeline Color Shift
            SetFloatInputField(OpacityField, 7, max: 1f);
            SetFloatInputField(HueField, 8, onValueChanged: _val =>
            {
                if (EditorConfig.Instance.ShowModifiedColors.Value)
                    SetListColor((int)currentKeyframe.values[6], 6, ColorToggles, ThemeManager.inst.Current.guiAccentColor, Color.black, 7, 8, 9, 10);
            });
            SetFloatInputField(SaturationField, 9, onValueChanged: _val =>
            {
                if (EditorConfig.Instance.ShowModifiedColors.Value)
                    SetListColor((int)currentKeyframe.values[6], 6, ColorToggles, ThemeManager.inst.Current.guiAccentColor, Color.black, 7, 8, 9, 10);
            });
            SetFloatInputField(ValueField, 10, onValueChanged: _val =>
            {
                if (EditorConfig.Instance.ShowModifiedColors.Value)
                    SetListColor((int)currentKeyframe.values[6], 6, ColorToggles, ThemeManager.inst.Current.guiAccentColor, Color.black, 7, 8, 9, 10);
            });
        }
    }
}
