using BetterLegacy.Core;
using BetterLegacy.Core.Prefabs;
using BetterLegacy.Core.Runtime.Events;

namespace BetterLegacy.Editor.Data.Dialogs
{
    public class LensKeyframeDialog : KeyframeDialog
    {
        public LensKeyframeDialog() : base(EventEngine.LENS) { }

        public InputFieldStorage IntensityField { get; set; }

        public Vector2InputFieldStorage CenterFields { get; set; }

        public Vector2InputFieldStorage IntensityFields { get; set; }

        public InputFieldStorage ScaleField { get; set; }

        public override void Init()
        {
            base.Init();

            IntensityField = GameObject.transform.Find("lens/x").gameObject.GetOrAddComponent<InputFieldStorage>();
            IntensityField.Assign();

            CenterFields = GameObject.transform.Find("center").gameObject.GetOrAddComponent<Vector2InputFieldStorage>();
            CenterFields.Assign();

            IntensityFields = GameObject.transform.Find("intensity").gameObject.GetOrAddComponent<Vector2InputFieldStorage>();
            IntensityFields.Assign();

            ScaleField = GameObject.transform.Find("scale/x").gameObject.GetOrAddComponent<InputFieldStorage>();
            ScaleField.Assign();
        }
    }
}
