using UnityEngine;

using BetterLegacy.Core;
using BetterLegacy.Core.Prefabs;
using BetterLegacy.Editor.Managers;

namespace BetterLegacy.Editor.Data.Dialogs
{
    public class ShockwaveKeyframeDialog : KeyframeDialog
    {
        public ShockwaveKeyframeDialog() : base(EventLibrary.Indexes.SHOCKWAVE) { }

        public InputFieldStorage IntensityField { get; set; }

        public InputFieldStorage RingAmount { get; set; }

        public Vector2InputFieldStorage CenterFields { get; set; }

        public Vector2InputFieldStorage ScaleFields { get; set; }

        public InputFieldStorage RotationField { get; set; }

        public InputFieldStorage WarpField { get; set; }

        public InputFieldStorage ElapsedField { get; set; }

        public override void Init()
        {
            CreateNew(EventEditor.inst.dialogRight, "shockwave");

            base.Init();

            new LabelsElement("Intensity").Init(EditorElement.InitSettings.Default.Parent(GameObject.transform));
            var intensity = EditorPrefabHolder.Instance.LayoutInputField.Duplicate(GameObject.transform, "intensity");
            IntensityField = intensity.transform.Find("x").gameObject.GetOrAddComponent<InputFieldStorage>();
            IntensityField.inputField.image.rectTransform.sizeDelta = new Vector2(317f, 32f);

            new LabelsElement("Ring Amount").Init(EditorElement.InitSettings.Default.Parent(GameObject.transform));
            var ringAmount = EditorPrefabHolder.Instance.LayoutInputField.Duplicate(GameObject.transform, "ring amount");
            RingAmount = ringAmount.transform.Find("x").gameObject.GetOrAddComponent<InputFieldStorage>();
            RingAmount.inputField.image.rectTransform.sizeDelta = new Vector2(317f, 32f);

            new LabelsElement("Center X", "Center Y").Init(EditorElement.InitSettings.Default.Parent(GameObject.transform));
            var center = EditorPrefabHolder.Instance.Vector2InputFields.Duplicate(GameObject.transform, "center");
            CenterFields = center.GetComponent<Vector2InputFieldStorage>();
            
            new LabelsElement("Scale X", "Scale Y").Init(EditorElement.InitSettings.Default.Parent(GameObject.transform));
            var scale = EditorPrefabHolder.Instance.Vector2InputFields.Duplicate(GameObject.transform, "scale");
            ScaleFields = scale.GetComponent<Vector2InputFieldStorage>();

            new LabelsElement("Rotation").Init(EditorElement.InitSettings.Default.Parent(GameObject.transform));
            var rotation = EditorPrefabHolder.Instance.LayoutInputField.Duplicate(GameObject.transform, "rotation");
            RotationField = rotation.transform.Find("x").gameObject.GetOrAddComponent<InputFieldStorage>();
            RotationField.inputField.image.rectTransform.sizeDelta = new Vector2(317f, 32f);
            
            new LabelsElement("Warp").Init(EditorElement.InitSettings.Default.Parent(GameObject.transform));
            var warp = EditorPrefabHolder.Instance.LayoutInputField.Duplicate(GameObject.transform, "warp");
            WarpField = warp.transform.Find("x").gameObject.GetOrAddComponent<InputFieldStorage>();
            WarpField.inputField.image.rectTransform.sizeDelta = new Vector2(317f, 32f);
            
            new LabelsElement("Elapsed").Init(EditorElement.InitSettings.Default.Parent(GameObject.transform));
            var elapsed = EditorPrefabHolder.Instance.LayoutInputField.Duplicate(GameObject.transform, "elapsed");
            ElapsedField = elapsed.transform.Find("x").gameObject.GetOrAddComponent<InputFieldStorage>();
            ElapsedField.inputField.image.rectTransform.sizeDelta = new Vector2(317f, 32f);

            EditorThemeManager.ApplyInputField(IntensityField);
            EditorThemeManager.ApplyInputField(RingAmount);
            EditorThemeManager.ApplyInputField(CenterFields);
            EditorThemeManager.ApplyInputField(ScaleFields);
            EditorThemeManager.ApplyInputField(RotationField);
            EditorThemeManager.ApplyInputField(WarpField);
            EditorThemeManager.ApplyInputField(ElapsedField);
        }

        public override void Render()
        {
            base.Render();

            SetFloatInputField(IntensityField, 0);
            SetFloatInputField(RingAmount, 1);
            SetVector2InputField(CenterFields, 2, 3);
            SetVector2InputField(ScaleFields, 4, 5);
            SetFloatInputField(RotationField, 6);
            SetFloatInputField(WarpField, 7);
            SetFloatInputField(ElapsedField, 8);
        }
    }
}
