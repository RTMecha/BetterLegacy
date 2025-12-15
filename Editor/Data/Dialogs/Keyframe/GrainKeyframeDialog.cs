using UnityEngine.UI;

using BetterLegacy.Core;
using BetterLegacy.Core.Prefabs;
using BetterLegacy.Core.Runtime.Events;
using BetterLegacy.Editor.Managers;

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

            EditorThemeManager.ApplyInputField(IntensityField);
            EditorThemeManager.ApplyToggle(ColoredToggle);
            EditorThemeManager.ApplyInputField(SizeField);
        }

        public override void Render()
        {
            base.Render();

            // Grain Intensity
            SetFloatInputField(IntensityField, 0, 0.1f, 10f, 0f, float.PositiveInfinity, allowNegative: false);

            // Grain Colored
            SetToggle(ColoredToggle, 1, 1, 0);

            // Grain Size
            SetFloatInputField(SizeField, 2, 0.1f, 10f, 0f, float.PositiveInfinity, allowNegative: false);
        }
    }
}
