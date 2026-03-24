using BetterLegacy.Core;
using BetterLegacy.Core.Prefabs;
using BetterLegacy.Editor.Managers;

namespace BetterLegacy.Editor.Data.Dialogs
{
    public class DatamoshKeyframeDialog : KeyframeDialog
    {
        public DatamoshKeyframeDialog() : base(EventLibrary.Indexes.DATAMOSH) { }

        public InputFieldStorage BlockSizeField { get; set; }

        public InputFieldStorage EntropyField { get; set; }

        public InputFieldStorage NoiseContrastField { get; set; }

        public InputFieldStorage VelocityScaleField { get; set; }

        public InputFieldStorage DiffusionField { get; set; }

        public override void Init()
        {
            CreateNew(EventEditor.inst.dialogRight);

            base.Init();

            new LabelsElement("Block Size").Init(EditorElement.InitSettings.Default.Parent(GameObject.transform));
            var blockSize = EditorPrefabHolder.Instance.LayoutInputField.Duplicate(GameObject.transform, "colordrift");

            new LabelsElement("Entropy").Init(EditorElement.InitSettings.Default.Parent(GameObject.transform));
            var entropy = EditorPrefabHolder.Instance.LayoutInputField.Duplicate(GameObject.transform, "horizontalshake");

            new LabelsElement("Noise Contrast").Init(EditorElement.InitSettings.Default.Parent(GameObject.transform));
            var noiseContrast = EditorPrefabHolder.Instance.LayoutInputField.Duplicate(GameObject.transform, "scanlinejitter");

            new LabelsElement("Velocity Scale").Init(EditorElement.InitSettings.Default.Parent(GameObject.transform));
            var velocityScale = EditorPrefabHolder.Instance.LayoutInputField.Duplicate(GameObject.transform, "verticaljump");
            
            new LabelsElement("Diffusion").Init(EditorElement.InitSettings.Default.Parent(GameObject.transform));
            var diffusion = EditorPrefabHolder.Instance.LayoutInputField.Duplicate(GameObject.transform, "verticaljump");

            BlockSizeField = blockSize.transform.Find("x").gameObject.GetOrAddComponent<InputFieldStorage>();
            BlockSizeField.Assign();

            EntropyField = entropy.transform.Find("x").gameObject.GetOrAddComponent<InputFieldStorage>();
            EntropyField.Assign();

            NoiseContrastField = noiseContrast.transform.Find("x").gameObject.GetOrAddComponent<InputFieldStorage>();
            NoiseContrastField.Assign();

            VelocityScaleField = velocityScale.transform.Find("x").gameObject.GetOrAddComponent<InputFieldStorage>();
            VelocityScaleField.Assign();

            DiffusionField = diffusion.transform.Find("x").gameObject.GetOrAddComponent<InputFieldStorage>();
            DiffusionField.Assign();

            EditorThemeManager.ApplyInputField(BlockSizeField);
            EditorThemeManager.ApplyInputField(EntropyField);
            EditorThemeManager.ApplyInputField(NoiseContrastField);
            EditorThemeManager.ApplyInputField(VelocityScaleField);
            EditorThemeManager.ApplyInputField(DiffusionField);
        }

        public override void Render()
        {
            base.Render();

            SetIntInputField(BlockSizeField, 0, min: 4, max: int.MaxValue, allowNegative: false);
            SetFloatInputField(EntropyField, 1, max: 1f, allowNegative: false);
            SetFloatInputField(NoiseContrastField, 2, min: 0.5f, max: 4.0f, allowNegative: false);
            SetFloatInputField(VelocityScaleField, 3, max: 2f, allowNegative: false);
            SetFloatInputField(DiffusionField, 4, max: 2f, allowNegative: false);
        }
    }
}
