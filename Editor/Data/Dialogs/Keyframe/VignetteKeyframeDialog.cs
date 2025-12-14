using UnityEngine.UI;

using BetterLegacy.Core;
using BetterLegacy.Core.Prefabs;
using BetterLegacy.Core.Runtime.Events;

namespace BetterLegacy.Editor.Data.Dialogs
{
    public class VignetteKeyframeDialog : KeyframeDialog
    {
        public VignetteKeyframeDialog() : base(EventEngine.VIGNETTE) { }

        public InputFieldStorage IntensityField { get; set; }

        public InputFieldStorage SmoothnessField { get; set; }

        public Toggle RoundedToggle { get; set; }

        public InputFieldStorage RoundnessField { get; set; }

        public Vector2InputFieldStorage CenterFields { get; set; }

        public Vector3InputFieldStorage HSVFields { get; set; }

        public override void Init()
        {
            base.Init();

            IntensityField = GameObject.transform.Find("intensity").gameObject.GetOrAddComponent<InputFieldStorage>();
            IntensityField.Assign();

            SmoothnessField = GameObject.transform.Find("smoothness").gameObject.GetOrAddComponent<InputFieldStorage>();
            SmoothnessField.Assign();

            RoundedToggle = GameObject.transform.Find("roundness/rounded").GetComponent<Toggle>();

            RoundnessField = GameObject.transform.Find("roundness").gameObject.GetOrAddComponent<InputFieldStorage>();
            RoundnessField.Assign();

            CenterFields = GameObject.transform.Find("position").gameObject.GetOrAddComponent<Vector2InputFieldStorage>();
            CenterFields.Assign();

            HSVFields = GameObject.transform.Find("colorshift").gameObject.GetOrAddComponent<Vector3InputFieldStorage>();
            HSVFields.Assign();
        }
    }
}
