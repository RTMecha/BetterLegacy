using UnityEngine;

using BetterLegacy.Core;
using BetterLegacy.Core.Prefabs;
using BetterLegacy.Editor.Managers;

namespace BetterLegacy.Editor.Data.Dialogs
{
    public class ScanlinesKeyframeDialog : KeyframeDialog
    {
        public ScanlinesKeyframeDialog() : base(EventLibrary.Indexes.SCANLINES) { }

        public InputFieldStorage IntensityField { get; set; }

        public InputFieldStorage AmountField { get; set; }

        public InputFieldStorage SpeedField { get; set; }

        public override void Init()
        {
            CreateNew(EventEditor.inst.dialogRight);

            base.Init();

            new LabelsElement("Intensity").Init(EditorElement.InitSettings.Default.Parent(GameObject.transform));
            var intensity = EditorPrefabHolder.Instance.LayoutInputField.Duplicate(GameObject.transform, "intensity");
            IntensityField = intensity.transform.Find("x").gameObject.GetOrAddComponent<InputFieldStorage>();
            IntensityField.inputField.image.rectTransform.sizeDelta = new Vector2(317f, 32f);

            new LabelsElement("Amount Horizontal").Init(EditorElement.InitSettings.Default.Parent(GameObject.transform));
            var amount = EditorPrefabHolder.Instance.LayoutInputField.Duplicate(GameObject.transform, "amount");
            AmountField = amount.transform.Find("x").gameObject.GetOrAddComponent<InputFieldStorage>();
            AmountField.inputField.image.rectTransform.sizeDelta = new Vector2(317f, 32f);

            new LabelsElement("Speed").Init(EditorElement.InitSettings.Default.Parent(GameObject.transform));
            var speed = EditorPrefabHolder.Instance.LayoutInputField.Duplicate(GameObject.transform, "speed");
            SpeedField = speed.transform.Find("x").gameObject.GetOrAddComponent<InputFieldStorage>();
            SpeedField.inputField.image.rectTransform.sizeDelta = new Vector2(317f, 32f);

            EditorThemeManager.ApplyInputField(IntensityField);
            EditorThemeManager.ApplyInputField(AmountField);
            EditorThemeManager.ApplyInputField(SpeedField);
        }

        public override void Render()
        {
            base.Render();

            SetFloatInputField(IntensityField, 0);
            SetFloatInputField(AmountField, 1);
            SetFloatInputField(SpeedField, 2);
        }
    }
}
