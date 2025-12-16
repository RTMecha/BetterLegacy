using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;

using BetterLegacy.Configs;
using BetterLegacy.Core;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Prefabs;
using BetterLegacy.Editor.Managers;

namespace BetterLegacy.Editor.Data.Dialogs
{
    public class VignetteKeyframeDialog : KeyframeDialog
    {
        public VignetteKeyframeDialog() : base(EventLibrary.Indexes.VIGNETTE) { }

        public InputFieldStorage IntensityField { get; set; }

        public InputFieldStorage SmoothnessField { get; set; }

        public Toggle RoundedToggle { get; set; }

        public InputFieldStorage RoundnessField { get; set; }

        public Vector2InputFieldStorage CenterFields { get; set; }

        public List<Toggle> ColorToggles { get; set; }

        public Vector3InputFieldStorage HSVFields { get; set; }

        public override void Init()
        {
            base.Init();

            // remove unused
            CoreHelper.Delete(GameObject.transform.GetChild(18)); // random empty label (wtf?)
            CoreHelper.Delete(GameObject.transform.GetChild(9)); // original color slots
            CoreHelper.Delete(GameObject.transform.GetChild(8)); // original color slots label

            IntensityField = GameObject.transform.Find("intensity").gameObject.GetOrAddComponent<InputFieldStorage>();
            IntensityField.Assign();

            SmoothnessField = GameObject.transform.Find("smoothness").gameObject.GetOrAddComponent<InputFieldStorage>();
            SmoothnessField.Assign();

            RoundedToggle = GameObject.transform.Find("roundness/rounded").GetComponent<Toggle>();

            RoundnessField = GameObject.transform.Find("roundness").gameObject.GetOrAddComponent<InputFieldStorage>();
            RoundnessField.Assign();

            CenterFields = GameObject.transform.Find("position").gameObject.GetOrAddComponent<Vector2InputFieldStorage>();
            CenterFields.Assign();

            new LabelsElement("Colors").Init(EditorElement.InitSettings.Default.Parent(GameObject.transform));
            var colors = new ColorGroupElement(19, new Vector2(366f, 64f), new Vector2(32f, 32f), new Vector2(5f, 5f));
            colors.Init(EditorElement.InitSettings.Default.Parent(GameObject.transform).Name("colors"));
            ColorToggles = colors.toggles;

            new LabelsElement(HorizontalOrVerticalLayoutValues.Horizontal.ChildControlHeight(false), "Hue", "Sat", "Val").Init(EditorElement.InitSettings.Default.Parent(GameObject.transform));
            var colorshift = EditorPrefabHolder.Instance.Vector3InputFields.Duplicate(GameObject.transform, "colorshift");

            HSVFields = colorshift.GetOrAddComponent<Vector3InputFieldStorage>();
            HSVFields.Assign();
            HSVFields.SetSize(new Vector2(122f, 32f), new Vector2(60f, 32f));

            EditorThemeManager.ApplyInputField(IntensityField);
            EditorThemeManager.ApplyInputField(SmoothnessField);
            EditorThemeManager.ApplyToggle(RoundedToggle);
            EditorThemeManager.ApplyInputField(RoundnessField);
            EditorThemeManager.ApplyInputField(CenterFields);
            EditorThemeManager.ApplyInputField(HSVFields);
        }

        public override void Render()
        {
            base.Render();

            var currentKeyframe = RTEventEditor.inst.CurrentSelectedKeyframe;

            // Vignette Intensity
            SetFloatInputField(IntensityField, 0, max: float.MaxValue, allowNegative: false);

            // Vignette Smoothness
            SetFloatInputField(SmoothnessField, 1);

            // Vignette Rounded
            SetToggle(RoundedToggle, 2, 1, 0);

            // Vignette Roundness
            SetFloatInputField(RoundnessField, 3, 0.01f, 10f, float.MinValue, 1.2f);

            // Vignette Center
            SetVector2InputField(CenterFields, 4, 5);

            var dialogTmp = GameObject.transform;
            EditorHelper.SetComplexity(dialogTmp.Find("colors").GetPreviousSibling()?.gameObject, "event/vignette_colors", Complexity.Normal);
            EditorHelper.SetComplexity(dialogTmp.Find("colors").gameObject, "event/vignette_colors", Complexity.Normal);
            EditorHelper.SetComplexity(dialogTmp.Find("colorshift").GetPreviousSibling()?.gameObject, "event/vignette_hsv", Complexity.Advanced);
            EditorHelper.SetComplexity(dialogTmp.Find("colorshift").gameObject, "event/vignette_hsv", Complexity.Advanced);

            // Vignette Color
            SetListColor((int)currentKeyframe.values[6], 6, ColorToggles, Color.black, Color.black, -1, 7, 8, 9);
            // Vignette Color Shift
            SetFloatInputField(HSVFields.x, 7, onValueChanged: _val =>
            {
                if (EditorConfig.Instance.ShowModifiedColors.Value)
                    SetListColor((int)currentKeyframe.values[6], 6, ColorToggles, Color.black, Color.black, -1, 7, 8, 9);
            });
            SetFloatInputField(HSVFields.y, 8, onValueChanged: _val =>
            {
                if (EditorConfig.Instance.ShowModifiedColors.Value)
                    SetListColor((int)currentKeyframe.values[6], 6, ColorToggles, Color.black, Color.black, -1, 7, 8, 9);
            });
            SetFloatInputField(HSVFields.z, 9, onValueChanged: _val =>
            {
                if (EditorConfig.Instance.ShowModifiedColors.Value)
                    SetListColor((int)currentKeyframe.values[6], 6, ColorToggles, Color.black, Color.black, -1, 7, 8, 9);
            });
        }
    }
}
