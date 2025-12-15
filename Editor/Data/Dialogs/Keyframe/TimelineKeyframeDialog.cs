using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;

using BetterLegacy.Configs;
using BetterLegacy.Core;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Managers;
using BetterLegacy.Core.Prefabs;
using BetterLegacy.Core.Runtime.Events;
using BetterLegacy.Editor.Managers;

namespace BetterLegacy.Editor.Data.Dialogs
{
    public class TimelineKeyframeDialog : KeyframeDialog
    {
        public TimelineKeyframeDialog() : base(EventEngine.TIMELINE) { }

        public Toggle ActiveToggle { get; set; }

        public Vector2InputFieldStorage PositionFields { get; set; }

        public Vector2InputFieldStorage ScaleFields { get; set; }

        public InputFieldStorage RotationField { get; set; }

        public List<Toggle> ColorToggles { get; set; }

        public InputFieldStorage OpacityField { get; set; }

        public InputFieldStorage HueField { get; set; }

        public InputFieldStorage SaturationField { get; set; }

        public InputFieldStorage ValueField { get; set; }

        public override void Init()
        {
            CreateNew(EventEditor.inst.dialogRight);

            base.Init();

            new LabelsElement("Timeline Active").Init(EditorElement.InitSettings.Default.Parent(GameObject.transform));
            var active = EditorPrefabHolder.Instance.ToggleButton.Duplicate(GameObject.transform, "active");
            var activeToggle = active.GetComponent<ToggleButtonStorage>();
            activeToggle.Text = "Active";
            ActiveToggle = activeToggle.toggle;

            new LabelsElement("Position X", "Position Y").Init(EditorElement.InitSettings.Default.Parent(GameObject.transform));
            var position = EditorPrefabHolder.Instance.Vector2InputFields.Duplicate(GameObject.transform, "position");
            PositionFields = position.GetComponent<Vector2InputFieldStorage>();
            
            new LabelsElement("Scale X", "Scale Y").Init(EditorElement.InitSettings.Default.Parent(GameObject.transform));
            var scale = EditorPrefabHolder.Instance.Vector2InputFields.Duplicate(GameObject.transform, "scale");
            ScaleFields = scale.GetComponent<Vector2InputFieldStorage>();

            new LabelsElement("Rotation").Init(EditorElement.InitSettings.Default.Parent(GameObject.transform));
            var rotation = EditorPrefabHolder.Instance.LayoutInputField.Duplicate(GameObject.transform, "rotation");
            RotationField = rotation.transform.Find("x").gameObject.GetOrAddComponent<InputFieldStorage>();
            RotationField.inputField.image.rectTransform.sizeDelta = new Vector2(317f, 32f);

            new LabelsElement("Color").Init(EditorElement.InitSettings.Default.Parent(GameObject.transform));
            var colors = new ColorGroupElement(20, new Vector2(366f, 64f), new Vector2(32f, 32f), new Vector2(5f, 5f));
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
            EditorThemeManager.ApplyInputField(PositionFields);
            EditorThemeManager.ApplyInputField(ScaleFields);
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

            // Timeline Active
            SetToggle(ActiveToggle, 0, 0, 1);

            // Timeline Position
            SetVector2InputField(PositionFields, 1, 2);

            // Timeline Scale
            SetVector2InputField(ScaleFields, 3, 4);

            // Timeline Rotation
            SetFloatInputField(RotationField, 5, 15f, 3f);

            // Timeline Color
            SetListColor((int)currentKeyframe.values[6], 6, ColorToggles, ThemeManager.inst.Current.guiColor, Color.black, 7, 8, 9, 10);

            // Timeline Color Shift
            SetFloatInputField(OpacityField, 7, max: 1f);
            SetFloatInputField(HueField, 8, onValueChanged: _val =>
            {
                if (EditorConfig.Instance.ShowModifiedColors.Value)
                    SetListColor((int)currentKeyframe.values[6], 6, ColorToggles, ThemeManager.inst.Current.guiColor, Color.black, 7, 8, 9, 10);
            });
            SetFloatInputField(SaturationField, 9, onValueChanged: _val =>
            {
                if (EditorConfig.Instance.ShowModifiedColors.Value)
                    SetListColor((int)currentKeyframe.values[6], 6, ColorToggles, ThemeManager.inst.Current.guiColor, Color.black, 7, 8, 9, 10);
            });
            SetFloatInputField(ValueField, 10, onValueChanged: _val =>
            {
                if (EditorConfig.Instance.ShowModifiedColors.Value)
                    SetListColor((int)currentKeyframe.values[6], 6, ColorToggles, ThemeManager.inst.Current.guiColor, Color.black, 7, 8, 9, 10);
            });
        }
    }
}
