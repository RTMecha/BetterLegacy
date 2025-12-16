using UnityEngine;

using BetterLegacy.Core;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Prefabs;
using BetterLegacy.Editor.Managers;

namespace BetterLegacy.Editor.Data.Dialogs
{
    public class LensKeyframeDialog : KeyframeDialog
    {
        public LensKeyframeDialog() : base(EventLibrary.Indexes.LENS) { }

        public InputFieldStorage IntensityField { get; set; }

        public Vector2InputFieldStorage CenterFields { get; set; }

        public Vector2InputFieldStorage IntensityFields { get; set; }

        public InputFieldStorage ScaleField { get; set; }

        public override void Init()
        {
            base.Init();

            IntensityField = GameObject.transform.Find("lens/x").gameObject.GetOrAddComponent<InputFieldStorage>();
            IntensityField.Assign();

            new LabelsElement(HorizontalOrVerticalLayoutValues.Horizontal.ChildControlHeight(false), "Center X", "Center Y").Init(EditorElement.InitSettings.Default.Parent(GameObject.transform));
            var center = EditorPrefabHolder.Instance.Vector2InputFields.Duplicate(GameObject.transform, "center");
            CenterFields = center.GetOrAddComponent<Vector2InputFieldStorage>();

            new LabelsElement(HorizontalOrVerticalLayoutValues.Horizontal.ChildControlHeight(false), "Intensity X", "Intensity Y").Init(EditorElement.InitSettings.Default.Parent(GameObject.transform));
            var intensity = EditorPrefabHolder.Instance.Vector2InputFields.Duplicate(GameObject.transform, "intensity");
            IntensityFields = intensity.GetOrAddComponent<Vector2InputFieldStorage>();

            new LabelsElement("Scale").Init(EditorElement.InitSettings.Default.Parent(GameObject.transform));
            var scale = EditorPrefabHolder.Instance.LayoutInputField.Duplicate(GameObject.transform, "scale");
            ScaleField = scale.transform.Find("x").gameObject.GetOrAddComponent<InputFieldStorage>();
            ScaleField.inputField.image.rectTransform.sizeDelta = new Vector2(317f, 32f);

            EditorThemeManager.ApplyInputField(IntensityField);
            EditorThemeManager.ApplyInputField(CenterFields);
            EditorThemeManager.ApplyInputField(IntensityFields);
            EditorThemeManager.ApplyInputField(ScaleField);
        }

        public override void Render()
        {
            base.Render();

            // Lens Intensity
            SetFloatInputField(IntensityField, 0, 1f, 10f, -100f, 100f);

            var dialogTmp = GameObject.transform;

            EditorHelper.SetComplexity(dialogTmp.Find("center").GetPreviousSibling()?.gameObject, "event/lens_center", Complexity.Advanced);
            EditorHelper.SetComplexity(dialogTmp.Find("center").gameObject, "event/lens_center", Complexity.Advanced);
            EditorHelper.SetComplexity(dialogTmp.Find("intensity").GetPreviousSibling()?.gameObject, "event/lens_intensity_xy", Complexity.Advanced);
            EditorHelper.SetComplexity(dialogTmp.Find("intensity").gameObject, "event/lens_intensity_xy", Complexity.Advanced);
            EditorHelper.SetComplexity(dialogTmp.Find("scale").GetPreviousSibling()?.gameObject, "event/lens_scale", Complexity.Advanced);
            EditorHelper.SetComplexity(dialogTmp.Find("scale").gameObject, "event/lens_scale", Complexity.Advanced);

            // Lens Center X / Y
            SetVector2InputField(CenterFields, 1, 2);

            // Lens Intensity X / Y
            SetVector2InputField(IntensityFields, 3, 4);

            // Lens Scale
            SetFloatInputField(ScaleField, 5, 0.1f, 10f, 0.001f, float.PositiveInfinity, allowNegative: false);
        }
    }
}
