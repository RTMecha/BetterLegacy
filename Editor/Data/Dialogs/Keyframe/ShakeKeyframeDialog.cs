using BetterLegacy.Core;
using BetterLegacy.Core.Prefabs;
using BetterLegacy.Core.Runtime.Events;

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

            DirectionFields = GameObject.transform.Find("direction").gameObject.GetOrAddComponent<Vector2InputFieldStorage>();
            DirectionFields.Assign();

            InterpolationField = GameObject.transform.Find("interpolation/x").gameObject.GetOrAddComponent<InputFieldStorage>();
            InterpolationField.Assign();

            SpeedField = GameObject.transform.Find("speed/x").gameObject.GetOrAddComponent<InputFieldStorage>();
            SpeedField.Assign();
        }
    }
}
