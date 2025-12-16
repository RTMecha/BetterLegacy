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
    public class BloomKeyframeDialog : KeyframeDialog
    {
        public BloomKeyframeDialog() : base(EventLibrary.Indexes.BLOOM) { }

        public InputFieldStorage IntensityField { get; set; }

        public InputFieldStorage DiffusionField { get; set; }

        public InputFieldStorage ThresholdField { get; set; }

        public InputFieldStorage AnamorphicRatioField { get; set; }

        public List<Toggle> ColorToggles { get; set; }

        public Vector3InputFieldStorage HSVFields { get; set; }

        public override void Init()
        {
            base.Init();

            IntensityField = GameObject.transform.Find("bloom/x").gameObject.GetOrAddComponent<InputFieldStorage>();
            IntensityField.Assign();

            new LabelsElement("Diffusion").Init(EditorElement.InitSettings.Default.Parent(GameObject.transform));
            var diffusion = EditorPrefabHolder.Instance.LayoutInputField.Duplicate(GameObject.transform, "diffusion");
            DiffusionField = diffusion.transform.Find("x").gameObject.GetOrAddComponent<InputFieldStorage>();
            DiffusionField.inputField.image.rectTransform.sizeDelta = new Vector2(317f, 32f);

            new LabelsElement("Threshold").Init(EditorElement.InitSettings.Default.Parent(GameObject.transform));
            var threshold = EditorPrefabHolder.Instance.LayoutInputField.Duplicate(GameObject.transform, "threshold");
            ThresholdField = threshold.transform.Find("x").gameObject.GetOrAddComponent<InputFieldStorage>();
            ThresholdField.inputField.image.rectTransform.sizeDelta = new Vector2(317f, 32f);

            new LabelsElement("Anamorphic Ratio").Init(EditorElement.InitSettings.Default.Parent(GameObject.transform));
            var anamorphicRatio = EditorPrefabHolder.Instance.LayoutInputField.Duplicate(GameObject.transform, "anamorphic ratio");
            AnamorphicRatioField = anamorphicRatio.transform.Find("x").gameObject.GetOrAddComponent<InputFieldStorage>();
            AnamorphicRatioField.inputField.image.rectTransform.sizeDelta = new Vector2(317f, 32f);

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
            EditorThemeManager.ApplyInputField(DiffusionField);
            EditorThemeManager.ApplyInputField(ThresholdField);
            EditorThemeManager.ApplyInputField(AnamorphicRatioField);
            EditorThemeManager.ApplyInputField(HSVFields);
        }

        public override void Render()
        {
            base.Render();

            var currentKeyframe = RTEventEditor.inst.CurrentSelectedKeyframe;

            //Bloom Intensity
            SetFloatInputField(IntensityField, 0, max: 1280f, allowNegative: false);

            var dialogTmp = GameObject.transform;
            EditorHelper.SetComplexity(dialogTmp.Find("diffusion").GetPreviousSibling()?.gameObject, "event/bloom_diffusion", Complexity.Normal);
            EditorHelper.SetComplexity(dialogTmp.Find("diffusion").gameObject, "event/bloom_diffusion", Complexity.Normal);
            EditorHelper.SetComplexity(dialogTmp.Find("threshold").GetPreviousSibling()?.gameObject, "event/bloom_threshold", Complexity.Advanced);
            EditorHelper.SetComplexity(dialogTmp.Find("threshold").gameObject, "event/bloom_threshold", Complexity.Advanced);
            EditorHelper.SetComplexity(dialogTmp.Find("anamorphic ratio").GetPreviousSibling()?.gameObject, "event/bloom_anamorphic_ratio", Complexity.Advanced);
            EditorHelper.SetComplexity(dialogTmp.Find("anamorphic ratio").gameObject, "event/bloom_anamorphic_ratio", Complexity.Advanced);
            EditorHelper.SetComplexity(dialogTmp.Find("colors").GetPreviousSibling()?.gameObject, "event/bloom_colors", Complexity.Normal);
            EditorHelper.SetComplexity(dialogTmp.Find("colors").gameObject, "event/bloom_colors", Complexity.Normal);
            EditorHelper.SetComplexity(dialogTmp.Find("colorshift").GetPreviousSibling()?.gameObject, "event/bloom_hsv", Complexity.Advanced);
            EditorHelper.SetComplexity(dialogTmp.Find("colorshift").gameObject, "event/bloom_hsv", Complexity.Advanced);

            // Bloom Diffusion
            SetFloatInputField(DiffusionField, 1, min: 1f, max: float.PositiveInfinity, allowNegative: false);

            // Bloom Threshold
            SetFloatInputField(ThresholdField, 2, min: 0f, max: 1.4f, allowNegative: false);

            // Bloom Anamorphic Ratio
            SetFloatInputField(AnamorphicRatioField, 3, min: -1f, max: 1f);

            // Bloom Color
            SetListColor((int)currentKeyframe.values[4], 4, ColorToggles, Color.white, Color.black, -1, 5, 6, 7);

            // Bloom Color Shift
            SetFloatInputField(HSVFields.x, 5, onValueChanged: _val =>
            {
                if (EditorConfig.Instance.ShowModifiedColors.Value)
                    SetListColor((int)currentKeyframe.values[4], 4, ColorToggles, Color.white, Color.black, -1, 5, 6, 7);
            });
            SetFloatInputField(HSVFields.y, 6, onValueChanged: _val =>
            {
                if (EditorConfig.Instance.ShowModifiedColors.Value)
                    SetListColor((int)currentKeyframe.values[4], 4, ColorToggles, Color.white, Color.black, -1, 5, 6, 7);
            });
            SetFloatInputField(HSVFields.z, 7, onValueChanged: _val =>
            {
                if (EditorConfig.Instance.ShowModifiedColors.Value)
                    SetListColor((int)currentKeyframe.values[4], 4, ColorToggles, Color.white, Color.black, -1, 5, 6, 7);
            });
        }
    }
}
