using UnityEngine.UI;

using BetterLegacy.Core;
using BetterLegacy.Core.Prefabs;
using BetterLegacy.Core.Runtime.Events;
using BetterLegacy.Editor.Managers;

namespace BetterLegacy.Editor.Data.Dialogs
{
    public class AnalogGlitchKeyframeDialog : KeyframeDialog
    {
        public AnalogGlitchKeyframeDialog() : base(EventEngine.ANALOG_GLITCH) { }

        public Toggle EnabledToggle { get; set; }

        public InputFieldStorage ColorDriftField { get; set; }

        public InputFieldStorage HorizontalShakeField { get; set; }

        public InputFieldStorage ScanLineJitterField { get; set; }

        public InputFieldStorage VerticalJumpField { get; set; }

        public override void Init()
        {
            CreateNew(EventEditor.inst.dialogRight);

            base.Init();

            new LabelsElement("Effect Enabled").Init(EditorElement.InitSettings.Default.Parent(GameObject.transform));
            var enabled = EditorPrefabHolder.Instance.ToggleButton.Duplicate(GameObject.transform, "enabled");
            var enabledToggle = enabled.GetComponent<ToggleButtonStorage>();
            enabledToggle.Text = "Enabled";

            new LabelsElement("Color Drift").Init(EditorElement.InitSettings.Default.Parent(GameObject.transform));
            var colorDrift = EditorPrefabHolder.Instance.LayoutInputField.Duplicate(GameObject.transform, "colordrift");
            
            new LabelsElement("Horizontal Shake").Init(EditorElement.InitSettings.Default.Parent(GameObject.transform));
            var horizontalShake = EditorPrefabHolder.Instance.LayoutInputField.Duplicate(GameObject.transform, "horizontalshake");
            
            new LabelsElement("Scan Line Jitter").Init(EditorElement.InitSettings.Default.Parent(GameObject.transform));
            var scanLineJitter = EditorPrefabHolder.Instance.LayoutInputField.Duplicate(GameObject.transform, "scanlinejitter");
            
            new LabelsElement("Vertical Jump").Init(EditorElement.InitSettings.Default.Parent(GameObject.transform));
            var verticalJump = EditorPrefabHolder.Instance.LayoutInputField.Duplicate(GameObject.transform, "verticaljump");

            EnabledToggle = enabledToggle.toggle;

            ColorDriftField = colorDrift.transform.Find("x").gameObject.GetOrAddComponent<InputFieldStorage>();
            ColorDriftField.Assign();

            HorizontalShakeField = horizontalShake.transform.Find("x").gameObject.GetOrAddComponent<InputFieldStorage>();
            HorizontalShakeField.Assign();

            ScanLineJitterField = scanLineJitter.transform.Find("x").gameObject.GetOrAddComponent<InputFieldStorage>();
            ScanLineJitterField.Assign();

            VerticalJumpField = verticalJump.transform.Find("x").gameObject.GetOrAddComponent<InputFieldStorage>();
            VerticalJumpField.Assign();

            EditorThemeManager.ApplyToggle(EnabledToggle);
            EditorThemeManager.ApplyInputField(ColorDriftField);
            EditorThemeManager.ApplyInputField(HorizontalShakeField);
            EditorThemeManager.ApplyInputField(ScanLineJitterField);
            EditorThemeManager.ApplyInputField(VerticalJumpField);
        }

        public override void Render()
        {
            base.Render();

            SetToggle(EnabledToggle, 0, 1, 0);
            SetFloatInputField(ColorDriftField, 1);
            SetFloatInputField(HorizontalShakeField, 2);
            SetFloatInputField(ScanLineJitterField, 3);
            SetFloatInputField(VerticalJumpField, 4);
        }
    }
}
