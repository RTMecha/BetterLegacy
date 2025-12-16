using UnityEngine;

using BetterLegacy.Core;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Prefabs;
using BetterLegacy.Editor.Managers;

namespace BetterLegacy.Editor.Data.Dialogs
{
    public class ColorGradingKeyframeDialog : KeyframeDialog
    {
        public ColorGradingKeyframeDialog() : base(EventLibrary.Indexes.COLORGRADING) { }

        public InputFieldStorage HueshiftField { get; set; }

        public InputFieldStorage ContrastField { get; set; }

        public Vector4InputFieldStorage GammaFields { get; set; }

        public InputFieldStorage SaturationField { get; set; }

        public InputFieldStorage TemperatureField { get; set; }

        public InputFieldStorage TintField { get; set; }

        public override void Init()
        {
            CreateNew(EventEditor.inst.dialogRight, "colorgrading");
            
            base.Init();

            new LabelsElement("Hueshift").Init(EditorElement.InitSettings.Default.Parent(GameObject.transform));
            var hueshift = EditorPrefabHolder.Instance.LayoutInputField.Duplicate(GameObject.transform, "hueshift");
            HueshiftField = hueshift.transform.Find("x").gameObject.GetOrAddComponent<InputFieldStorage>();
            HueshiftField.inputField.image.rectTransform.sizeDelta = new Vector2(317f, 32f);

            new LabelsElement("Contrast").Init(EditorElement.InitSettings.Default.Parent(GameObject.transform));
            var contrast = EditorPrefabHolder.Instance.LayoutInputField.Duplicate(GameObject.transform, "contrast");
            ContrastField = contrast.transform.Find("x").gameObject.GetOrAddComponent<InputFieldStorage>();
            ContrastField.inputField.image.rectTransform.sizeDelta = new Vector2(317f, 32f);

            new LabelsElement("Saturation").Init(EditorElement.InitSettings.Default.Parent(GameObject.transform));
            var saturation = EditorPrefabHolder.Instance.LayoutInputField.Duplicate(GameObject.transform, "saturation");
            SaturationField = saturation.transform.Find("x").gameObject.GetOrAddComponent<InputFieldStorage>();
            SaturationField.inputField.image.rectTransform.sizeDelta = new Vector2(317f, 32f);

            new LabelsElement("Temperature").Init(EditorElement.InitSettings.Default.Parent(GameObject.transform));
            var temperature = EditorPrefabHolder.Instance.LayoutInputField.Duplicate(GameObject.transform, "temperature");
            TemperatureField = temperature.transform.Find("x").gameObject.GetOrAddComponent<InputFieldStorage>();
            TemperatureField.inputField.image.rectTransform.sizeDelta = new Vector2(317f, 32f);

            new LabelsElement("Tint").Init(EditorElement.InitSettings.Default.Parent(GameObject.transform));
            var tint = EditorPrefabHolder.Instance.LayoutInputField.Duplicate(GameObject.transform, "tint");
            TintField = tint.transform.Find("x").gameObject.GetOrAddComponent<InputFieldStorage>();
            TintField.inputField.image.rectTransform.sizeDelta = new Vector2(317f, 32f);

            new LabelsElement(HorizontalOrVerticalLayoutValues.Horizontal.ChildControlHeight(false), "Red", "Green", "Blue", "Global").Init(EditorElement.InitSettings.Default.Parent(GameObject.transform));
            var gamma = EditorPrefabHolder.Instance.Vector4InputFields.Duplicate(GameObject.transform, "gamma");
            GammaFields = gamma.GetComponent<Vector4InputFieldStorage>();
            GammaFields.SetSize(new Vector2(80f, 32f), new Vector2(50f, 32f));
            GammaFields.SetIndividualSpacing(4f);

            EditorThemeManager.ApplyInputField(HueshiftField);
            EditorThemeManager.ApplyInputField(ContrastField);
            EditorThemeManager.ApplyInputField(GammaFields);
            EditorThemeManager.ApplyInputField(SaturationField);
            EditorThemeManager.ApplyInputField(TemperatureField);
            EditorThemeManager.ApplyInputField(TintField);
        }

        public override void Render()
        {
            base.Render();

            // ColorGrading Hueshift
            SetFloatInputField(HueshiftField, 0, 0.1f, 10f);

            // ColorGrading Contrast
            SetFloatInputField(ContrastField, 1, 1f, 10f);

            // ColorGrading Gamma
            SetFloatInputField(GammaFields.x, 2);
            SetFloatInputField(GammaFields.y, 3);
            SetFloatInputField(GammaFields.z, 4);
            SetFloatInputField(GammaFields.w, 5);

            // ColorGrading Saturation
            SetFloatInputField(SaturationField, 6, 1f, 10f);

            // ColorGrading Temperature
            SetFloatInputField(TemperatureField, 7, 1f, 10f);

            // ColorGrading Tint
            SetFloatInputField(TintField, 8, 1f, 10f);
        }
    }
}
