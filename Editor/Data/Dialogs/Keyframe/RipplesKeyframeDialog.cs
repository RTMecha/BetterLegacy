using UnityEngine;
using UnityEngine.UI;

using BetterLegacy.Core;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Prefabs;
using BetterLegacy.Editor.Managers;

namespace BetterLegacy.Editor.Data.Dialogs
{
    public class RipplesKeyframeDialog : KeyframeDialog
    {
        public RipplesKeyframeDialog() : base(EventLibrary.Indexes.RIPPLES) { }

        public InputFieldStorage StrengthField { get; set; }

        public InputFieldStorage SpeedField { get; set; }

        public InputFieldStorage DistanceField { get; set; }

        public Vector2InputFieldStorage SizeFields { get; set; }

        public Dropdown ModeDropdown { get; set; }

        public override void Init()
        {
            CreateNew(EventEditor.inst.dialogRight, "ripples");

            base.Init();

            new LabelsElement("Strength").Init(EditorElement.InitSettings.Default.Parent(GameObject.transform));
            var strength = EditorPrefabHolder.Instance.LayoutInputField.Duplicate(GameObject.transform, "strength");
            StrengthField = strength.transform.Find("x").gameObject.GetOrAddComponent<InputFieldStorage>();
            StrengthField.inputField.image.rectTransform.sizeDelta = new Vector2(317f, 32f);

            new LabelsElement("Speed").Init(EditorElement.InitSettings.Default.Parent(GameObject.transform));
            var speed = EditorPrefabHolder.Instance.LayoutInputField.Duplicate(GameObject.transform, "speed");
            SpeedField = speed.transform.Find("x").gameObject.GetOrAddComponent<InputFieldStorage>();
            SpeedField.inputField.image.rectTransform.sizeDelta = new Vector2(317f, 32f);

            new LabelsElement("Distance").Init(EditorElement.InitSettings.Default.Parent(GameObject.transform));
            var distance = EditorPrefabHolder.Instance.LayoutInputField.Duplicate(GameObject.transform, "distance");
            DistanceField = distance.transform.Find("x").gameObject.GetOrAddComponent<InputFieldStorage>();
            DistanceField.inputField.image.rectTransform.sizeDelta = new Vector2(317f, 32f);

            new LabelsElement("Height", "Width").Init(EditorElement.InitSettings.Default.Parent(GameObject.transform));
            var size = EditorPrefabHolder.Instance.Vector2InputFields.Duplicate(GameObject.transform, "size");
            SizeFields = size.GetComponent<Vector2InputFieldStorage>();

            new LabelsElement("Mode").Init(EditorElement.InitSettings.Default.Parent(GameObject.transform));
            ModeDropdown = EditorPrefabHolder.Instance.Dropdown.Duplicate(GameObject.transform, "mode").GetComponent<Dropdown>();
            ModeDropdown.options = CoreHelper.StringToOptionData("Radial", "Omni-Directional");

            EditorThemeManager.ApplyInputField(StrengthField);
            EditorThemeManager.ApplyInputField(SpeedField);
            EditorThemeManager.ApplyInputField(DistanceField);
            EditorThemeManager.ApplyInputField(SizeFields);
            EditorThemeManager.ApplyDropdown(ModeDropdown);
        }

        public override void Render()
        {
            base.Render();

            SetFloatInputField(StrengthField, 0);
            SetFloatInputField(SpeedField, 1);
            SetFloatInputField(DistanceField, 2, 0.1f, 10f, 0.001f, float.PositiveInfinity);
            SetVector2InputField(SizeFields, 3, 4);

            ModeDropdown.SetValueWithoutNotify((int)RTEventEditor.inst.CurrentSelectedKeyframe.values[5]);
            ModeDropdown.onValueChanged.NewListener(_val => SetKeyframeValue(5, _val));
        }
    }
}
