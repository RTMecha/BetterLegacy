using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;

using BetterLegacy.Configs;
using BetterLegacy.Core;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Prefabs;
using BetterLegacy.Core.Runtime.Events;
using BetterLegacy.Editor.Managers;

namespace BetterLegacy.Editor.Data.Dialogs
{
    public class GradientKeyframeDialog : KeyframeDialog
    {
        public GradientKeyframeDialog() : base(EventEngine.GRADIENT) { }

        public InputFieldStorage IntensityField { get; set; }

        public InputFieldStorage RotationField { get; set; }

        public List<Toggle> StartColorToggles { get; set; }

        public InputFieldStorage StartOpacityField { get; set; }

        public InputFieldStorage StartHueField { get; set; }

        public InputFieldStorage StartSaturationField { get; set; }

        public InputFieldStorage StartValueField { get; set; }

        public List<Toggle> EndColorToggles { get; set; }

        public InputFieldStorage EndOpacityField { get; set; }

        public InputFieldStorage EndHueField { get; set; }

        public InputFieldStorage EndSaturationField { get; set; }

        public InputFieldStorage EndValueField { get; set; }

        public Dropdown ModeDropdown { get; set; }

        public override void Init()
        {
            CreateNew(EventEditor.inst.dialogRight);

            base.Init();

            new LabelsElement("Intensity", "Rotation").Init(EditorElement.InitSettings.Default.Parent(GameObject.transform));
            var introt = EditorPrefabHolder.Instance.Vector2InputFields.Duplicate(GameObject.transform, "introt");
            var introtFields = introt.GetComponent<Vector2InputFieldStorage>();
            IntensityField = introtFields.x;
            IntensityField.Assign();
            RotationField = introtFields.y;
            RotationField.Assign();

            new LabelsElement("Start Color").Init(EditorElement.InitSettings.Default.Parent(GameObject.transform));
            var startColors = new ColorGroupElement(20, new Vector2(366f, 64f), new Vector2(32f, 32f), new Vector2(5f, 5f));
            startColors.Init(EditorElement.InitSettings.Default.Parent(GameObject.transform).Name("colors1"));
            StartColorToggles = startColors.toggles;

            new LabelsElement(HorizontalOrVerticalLayoutValues.Horizontal.ChildControlHeight(false), "Opacity", "Hue", "Sat", "Val").Init(EditorElement.InitSettings.Default.Parent(GameObject.transform));
            var startColorshift = EditorPrefabHolder.Instance.Vector4InputFields.Duplicate(GameObject.transform, "colorshift1");
            var startColorshiftFields = startColorshift.GetComponent<Vector4InputFieldStorage>();
            startColorshiftFields.SetSize(new Vector2(80f, 32f), new Vector2(50f, 32f));
            startColorshiftFields.SetIndividualSpacing(4f);
            StartOpacityField = startColorshiftFields.x;
            StartHueField = startColorshiftFields.y;
            StartSaturationField = startColorshiftFields.z;
            StartValueField = startColorshiftFields.w;

            new LabelsElement("End Color").Init(EditorElement.InitSettings.Default.Parent(GameObject.transform));
            var endColors = new ColorGroupElement(20, new Vector2(366f, 64f), new Vector2(32f, 32f), new Vector2(5f, 5f));
            endColors.Init(EditorElement.InitSettings.Default.Parent(GameObject.transform).Name("colors2"));
            EndColorToggles = endColors.toggles;

            new LabelsElement(HorizontalOrVerticalLayoutValues.Horizontal.ChildControlHeight(false), "Opacity", "Hue", "Sat", "Val").Init(EditorElement.InitSettings.Default.Parent(GameObject.transform));
            var endColorshift = EditorPrefabHolder.Instance.Vector4InputFields.Duplicate(GameObject.transform, "colorshift2");
            var endColorshiftFields = endColorshift.GetComponent<Vector4InputFieldStorage>();
            endColorshiftFields.SetSize(new Vector2(80f, 32f), new Vector2(50f, 32f));
            endColorshiftFields.SetIndividualSpacing(4f);
            EndOpacityField = endColorshiftFields.x;
            EndHueField = endColorshiftFields.y;
            EndSaturationField = endColorshiftFields.z;
            EndValueField = endColorshiftFields.w;

            new LabelsElement("Mode").Init(EditorElement.InitSettings.Default.Parent(GameObject.transform));
            ModeDropdown = EditorPrefabHolder.Instance.Dropdown.Duplicate(GameObject.transform, "mode").GetComponent<Dropdown>();
            ModeDropdown.options = CoreHelper.StringToOptionData("Linear", "Additive", "Multiply", "Screen");

            EditorThemeManager.ApplyInputField(IntensityField);
            EditorThemeManager.ApplyInputField(RotationField);
            EditorThemeManager.ApplyDropdown(ModeDropdown);
            EditorThemeManager.ApplyInputField(StartOpacityField);
            EditorThemeManager.ApplyInputField(StartHueField);
            EditorThemeManager.ApplyInputField(StartSaturationField);
            EditorThemeManager.ApplyInputField(StartValueField);
            EditorThemeManager.ApplyInputField(EndOpacityField);
            EditorThemeManager.ApplyInputField(EndHueField);
            EditorThemeManager.ApplyInputField(EndSaturationField);
            EditorThemeManager.ApplyInputField(EndValueField);
        }

        public override void Render()
        {
            base.Render();

            var currentKeyframe = RTEventEditor.inst.CurrentSelectedKeyframe;

            // Gradient Intensity / Rotation (Had to put them together due to mode going over the timeline lol)
            SetFloatInputField(IntensityField, 0);
            SetFloatInputField(RotationField, 1);

            // Gradient Color Top
            SetListColor((int)currentKeyframe.values[2], 2, StartColorToggles, new Color(0f, 0.8f, 0.56f, 0.5f), Color.black, 5, 6, 7, 8);

            // Gradient Color Bottom
            SetListColor((int)currentKeyframe.values[3], 3, EndColorToggles, new Color(0.81f, 0.37f, 1f, 0.5f), Color.black, 9, 10, 11, 12);

            // Gradient Mode
            ModeDropdown.SetValueWithoutNotify((int)currentKeyframe.values[4]);
            ModeDropdown.onValueChanged.NewListener(_val => SetKeyframeValue(4, _val));

            // Gradient Top Color Shift
            SetFloatInputField(StartOpacityField, 5, max: 1f);
            SetFloatInputField(StartHueField, 6, onValueChanged: _val =>
            {
                if (EditorConfig.Instance.ShowModifiedColors.Value)
                    SetListColor((int)currentKeyframe.values[2], 2, StartColorToggles, new Color(0f, 0.8f, 0.56f, 0.5f), Color.black, 5, 6, 7, 8);
            });
            SetFloatInputField(StartSaturationField, 7, onValueChanged: _val =>
            {
                if (EditorConfig.Instance.ShowModifiedColors.Value)
                    SetListColor((int)currentKeyframe.values[2], 2, StartColorToggles, new Color(0f, 0.8f, 0.56f, 0.5f), Color.black, 5, 6, 7, 8);
            });
            SetFloatInputField(StartValueField, 8, onValueChanged: _val =>
            {
                if (EditorConfig.Instance.ShowModifiedColors.Value)
                    SetListColor((int)currentKeyframe.values[2], 2, StartColorToggles, new Color(0f, 0.8f, 0.56f, 0.5f), Color.black, 5, 6, 7, 8);
            });

            // Gradient Bottom Color Shift
            SetFloatInputField(EndOpacityField, 9, max: 1f);
            SetFloatInputField(EndHueField, 10, onValueChanged: _val =>
            {
                if (EditorConfig.Instance.ShowModifiedColors.Value)
                    SetListColor((int)currentKeyframe.values[3], 3, EndColorToggles, new Color(0.81f, 0.37f, 1f, 0.5f), Color.black, 9, 10, 11, 12);
            });
            SetFloatInputField(EndSaturationField, 11, onValueChanged: _val =>
            {
                if (EditorConfig.Instance.ShowModifiedColors.Value)
                    SetListColor((int)currentKeyframe.values[3], 3, EndColorToggles, new Color(0.81f, 0.37f, 1f, 0.5f), Color.black, 9, 10, 11, 12);
            });
            SetFloatInputField(EndValueField, 12, onValueChanged: _val =>
            {
                if (EditorConfig.Instance.ShowModifiedColors.Value)
                    SetListColor((int)currentKeyframe.values[3], 3, EndColorToggles, new Color(0.81f, 0.37f, 1f, 0.5f), Color.black, 9, 10, 11, 12);
            });
        }
    }
}
