using UnityEngine.UI;

using BetterLegacy.Core;
using BetterLegacy.Core.Prefabs;
using BetterLegacy.Core.Runtime.Events;

namespace BetterLegacy.Editor.Data.Dialogs
{
    public class GrainKeyframeDialog : KeyframeDialog
    {
        public GrainKeyframeDialog() : base(EventEngine.GRAIN) { }

        public InputFieldStorage IntensityField { get; set; }

        public Toggle ColoredToggle { get; set; }

        public InputFieldStorage SizeField { get; set; }

        public override void Init()
        {
            base.Init();

            IntensityField = GameObject.transform.Find("intensity").gameObject.GetOrAddComponent<InputFieldStorage>();
            IntensityField.Assign();

            ColoredToggle = GameObject.transform.Find("colored").GetComponent<Toggle>();

            SizeField = GameObject.transform.Find("size").gameObject.GetOrAddComponent<InputFieldStorage>();
            SizeField.Assign();
        }
    }
}
