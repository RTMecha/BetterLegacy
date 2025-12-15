using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;

using BetterLegacy.Configs;
using BetterLegacy.Core;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Prefabs;
using BetterLegacy.Core.Runtime.Events;
using BetterLegacy.Editor.Managers;

namespace BetterLegacy.Editor.Data.Dialogs
{
    public class DangerKeyframeDialog : KeyframeDialog
    {
        public DangerKeyframeDialog() : base(EventEngine.DANGER) { }

        public InputFieldStorage IntensityField { get; set; }

        public InputFieldStorage SizeField { get; set; }

        public List<Toggle> ColorToggles { get; set; }

        public InputFieldStorage OpacityField { get; set; }

        public InputFieldStorage HueField { get; set; }

        public InputFieldStorage SaturationField { get; set; }

        public InputFieldStorage ValueField { get; set; }

        public override void Init()
        {
            CreateNew(EventEditor.inst.dialogRight);

            base.Init();

            new LabelsElement("Intensity").Init(EditorElement.InitSettings.Default.Parent(GameObject.transform));
            var intensity = EditorPrefabHolder.Instance.LayoutInputField.Duplicate(GameObject.transform, "intensity");
            IntensityField = intensity.transform.Find("x").gameObject.GetOrAddComponent<InputFieldStorage>();
            IntensityField.inputField.image.rectTransform.sizeDelta = new Vector2(317f, 32f);

            new LabelsElement("Size").Init(EditorElement.InitSettings.Default.Parent(GameObject.transform));
            var size = EditorPrefabHolder.Instance.LayoutInputField.Duplicate(GameObject.transform, "size");
            SizeField = size.transform.Find("x").gameObject.GetOrAddComponent<InputFieldStorage>();
            SizeField.inputField.image.rectTransform.sizeDelta = new Vector2(317f, 32f);

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

            EditorThemeManager.ApplyInputField(IntensityField);
            EditorThemeManager.ApplyInputField(SizeField);
            EditorThemeManager.ApplyInputField(OpacityField);
            EditorThemeManager.ApplyInputField(HueField);
            EditorThemeManager.ApplyInputField(SaturationField);
            EditorThemeManager.ApplyInputField(ValueField);
        }

        public override void Render()
        {
            base.Render();

            SetFloatInputField(IntensityField, 0);

            SetFloatInputField(SizeField, 1);

            var currentKeyframe = RTEventEditor.inst.CurrentSelectedKeyframe;

            // Danger Color
            SetListColor((int)currentKeyframe.values[2], 2, ColorToggles, new Color(0.66f, 0f, 0f), Color.black, 3, 4, 5, 6);

            // Danger Color Shift
            SetFloatInputField(OpacityField, 3, max: 1f);
            SetFloatInputField(HueField, 4, onValueChanged: _val =>
            {
                if (EditorConfig.Instance.ShowModifiedColors.Value)
                    SetListColor((int)currentKeyframe.values[2], 2, ColorToggles, new Color(0.66f, 0f, 0f), Color.black, 3, 4, 5, 6);
            });
            SetFloatInputField(SaturationField, 5, onValueChanged: _val =>
            {
                if (EditorConfig.Instance.ShowModifiedColors.Value)
                    SetListColor((int)currentKeyframe.values[2], 2, ColorToggles, new Color(0.66f, 0f, 0f), Color.black, 3, 4, 5, 6);
            });
            SetFloatInputField(ValueField, 6, onValueChanged: _val =>
            {
                if (EditorConfig.Instance.ShowModifiedColors.Value)
                    SetListColor((int)currentKeyframe.values[2], 2, ColorToggles, new Color(0.66f, 0f, 0f), Color.black, 3, 4, 5, 6);
            });
        }
    }
}
