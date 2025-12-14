using BetterLegacy.Core;
using BetterLegacy.Core.Prefabs;
using BetterLegacy.Core.Runtime.Events;

namespace BetterLegacy.Editor.Data.Dialogs
{
    public class BloomKeyframeDialog : KeyframeDialog
    {
        public BloomKeyframeDialog() : base(EventEngine.BLOOM) { }

        public InputFieldStorage IntensityField { get; set; }

        public InputFieldStorage DiffusionField { get; set; }

        public InputFieldStorage ThresholdField { get; set; }

        public InputFieldStorage AnamorphicRatioField { get; set; }

        public Vector3InputFieldStorage HSVFields { get; set; }

        public override void Init()
        {
            base.Init();

            IntensityField = GameObject.transform.Find("bloom/x").gameObject.GetOrAddComponent<InputFieldStorage>();
            IntensityField.Assign();

            DiffusionField = GameObject.transform.Find("diffusion/x").gameObject.GetOrAddComponent<InputFieldStorage>();
            DiffusionField.Assign();

            ThresholdField = GameObject.transform.Find("threshold/x").gameObject.GetOrAddComponent<InputFieldStorage>();
            ThresholdField.Assign();

            AnamorphicRatioField = GameObject.transform.Find("anamorphic ratio/x").gameObject.GetOrAddComponent<InputFieldStorage>();
            AnamorphicRatioField.Assign();

            HSVFields = GameObject.transform.Find("colorshift").gameObject.GetOrAddComponent<Vector3InputFieldStorage>();
            HSVFields.Assign();
        }
    }
}
