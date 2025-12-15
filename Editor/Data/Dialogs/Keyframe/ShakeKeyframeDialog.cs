using UnityEngine;

using BetterLegacy.Core;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Prefabs;
using BetterLegacy.Core.Runtime.Events;
using BetterLegacy.Editor.Managers;

namespace BetterLegacy.Editor.Data.Dialogs
{
    public class ShakeKeyframeDialog : KeyframeDialog
    {
        public ShakeKeyframeDialog() : base(EventEngine.SHAKE) { }

        public InputFieldStorage IntensityField { get; set; }

        public Vector2InputFieldStorage DirectionFields { get; set; }

        public InputFieldStorage InterpolationField { get; set; }

        public InputFieldStorage SpeedField { get; set; }

        public override void Init()
        {
            base.Init();

            IntensityField = GameObject.transform.Find("shake/x").gameObject.GetOrAddComponent<InputFieldStorage>();
            IntensityField.Assign();

            new LabelsElement(HorizontalOrVerticalLayoutValues.Horizontal.ChildControlHeight(false), "Direction X", "Direction Y").Init(EditorElement.InitSettings.Default.Parent(GameObject.transform));
            var direction = EditorPrefabHolder.Instance.Vector2InputFields.Duplicate(GameObject.transform, "direction");
            DirectionFields = direction.GetOrAddComponent<Vector2InputFieldStorage>();

            new LabelsElement(HorizontalOrVerticalLayoutValues.Horizontal.ChildControlHeight(false), new LabelElement("(Requires Catalyst Shake)")).Init(EditorElement.InitSettings.Default.Parent(GameObject.transform).Name("notice-label"));

            new LabelsElement("Smoothness").Init(EditorElement.InitSettings.Default.Parent(GameObject.transform));
            var interpolation = EditorPrefabHolder.Instance.LayoutInputField.Duplicate(GameObject.transform, "interpolation");
            InterpolationField = interpolation.transform.Find("x").gameObject.GetOrAddComponent<InputFieldStorage>();
            InterpolationField.inputField.image.rectTransform.sizeDelta = new Vector2(317f, 32f);

            new LabelsElement("Speed").Init(EditorElement.InitSettings.Default.Parent(GameObject.transform));
            var speed = EditorPrefabHolder.Instance.LayoutInputField.Duplicate(GameObject.transform, "speed");
            SpeedField = speed.transform.Find("x").gameObject.GetOrAddComponent<InputFieldStorage>();
            SpeedField.inputField.image.rectTransform.sizeDelta = new Vector2(317f, 32f);

            EditorThemeManager.ApplyInputField(IntensityField);
            EditorThemeManager.ApplyInputField(DirectionFields);
            EditorThemeManager.ApplyInputField(InterpolationField);
            EditorThemeManager.ApplyInputField(SpeedField);
        }

        public override void Render()
        {
            base.Render();

            SetFloatInputField(IntensityField, 0, min: 0f, max: 10f, allowNegative: false);

            var dialogTmp = GameObject.transform;
            EditorHelper.SetComplexity(dialogTmp.Find("direction").GetPreviousSibling()?.gameObject, "event/shake_direction", Complexity.Normal);
            EditorHelper.SetComplexity(dialogTmp.Find("direction").gameObject, "event/shake_direction", Complexity.Normal);
            EditorHelper.SetComplexity(dialogTmp.Find("notice-label").gameObject, "event/shake_notice", Complexity.Advanced);
            EditorHelper.SetComplexity(dialogTmp.Find("interpolation").GetPreviousSibling()?.gameObject, "event/shake_interpolation", Complexity.Advanced);
            EditorHelper.SetComplexity(dialogTmp.Find("interpolation").gameObject, "event/shake_interpolation", Complexity.Advanced);
            EditorHelper.SetComplexity(dialogTmp.Find("speed").GetPreviousSibling()?.gameObject, "event/shake_speed", Complexity.Advanced);
            EditorHelper.SetComplexity(dialogTmp.Find("speed").gameObject, "event/shake_speed", Complexity.Advanced);

            SetVector2InputField(DirectionFields, 1, 2, -10f, 10f);
            SetFloatInputField(InterpolationField, 3, max: 999f, allowNegative: false);
            SetFloatInputField(SpeedField, 4, min: 0.001f, max: 9999f, allowNegative: false);
        }
    }
}
