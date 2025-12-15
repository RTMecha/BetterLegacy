using UnityEngine;

using BetterLegacy.Core;
using BetterLegacy.Core.Prefabs;
using BetterLegacy.Editor.Managers;

namespace BetterLegacy.Editor.Data.Dialogs
{
    public class BlurKeyframeDialog : KeyframeDialog
    {
        public BlurKeyframeDialog(int type, int maxIterations) : base(type) => this.maxIterations = maxIterations;

        public InputFieldStorage IntensityField { get; set; }

        public InputFieldStorage IterationsField { get; set; }

        public string name;

        public int maxIterations;

        public override void Init()
        {
            CreateNew(EventEditor.inst.dialogRight);

            base.Init();

            new LabelsElement("Intensity").Init(EditorElement.InitSettings.Default.Parent(GameObject.transform));
            var intensity = EditorPrefabHolder.Instance.LayoutInputField.Duplicate(GameObject.transform, "intensity");
            IntensityField = intensity.transform.Find("x").gameObject.GetOrAddComponent<InputFieldStorage>();
            IntensityField.inputField.image.rectTransform.sizeDelta = new Vector2(317f, 32f);

            new LabelsElement("Iterations").Init(EditorElement.InitSettings.Default.Parent(GameObject.transform));
            var iterations = EditorPrefabHolder.Instance.LayoutInputField.Duplicate(GameObject.transform, "iterations");
            IterationsField = iterations.transform.Find("x").gameObject.GetOrAddComponent<InputFieldStorage>();
            IterationsField.Assign();
            IterationsField.inputField.image.rectTransform.sizeDelta = new Vector2(317f, 32f);

            EditorThemeManager.ApplyInputField(IntensityField);
            EditorThemeManager.ApplyInputField(IterationsField);
        }

        public override void Render()
        {
            base.Render();

            SetFloatInputField(IntensityField, 0);
            SetIntInputField(IterationsField, 1, 1, 1, maxIterations, false);
        }
    }
}
