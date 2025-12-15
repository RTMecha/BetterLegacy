using UnityEngine;
using UnityEngine.UI;

using BetterLegacy.Core;
using BetterLegacy.Core.Prefabs;
using BetterLegacy.Core.Runtime.Events;
using BetterLegacy.Editor.Managers;

namespace BetterLegacy.Editor.Data.Dialogs
{
    public class CameraDepthKeyframeDialog : KeyframeDialog
    {
        public CameraDepthKeyframeDialog() : base(EventEngine.CAMERA_DEPTH) { }

        public InputFieldStorage DepthField { get; set; }

        public InputFieldStorage ZoomField { get; set; }

        public Toggle GlobalToggle { get; set; }

        public Toggle AlignToggle { get; set; }

        public override void Init()
        {
            CreateNew(EventEditor.inst.dialogRight);

            base.Init();

            new LabelsElement("Depth").Init(EditorElement.InitSettings.Default.Parent(GameObject.transform));
            var depth = EditorPrefabHolder.Instance.LayoutInputField.Duplicate(GameObject.transform, "depth");
            DepthField = depth.transform.Find("x").gameObject.GetOrAddComponent<InputFieldStorage>();
            DepthField.inputField.image.rectTransform.sizeDelta = new Vector2(317f, 32f);

            new LabelsElement("Zoom").Init(EditorElement.InitSettings.Default.Parent(GameObject.transform));
            var zoom = EditorPrefabHolder.Instance.LayoutInputField.Duplicate(GameObject.transform, "zoom");
            ZoomField = zoom.transform.Find("x").gameObject.GetOrAddComponent<InputFieldStorage>();
            ZoomField.inputField.image.rectTransform.sizeDelta = new Vector2(317f, 32f);

            new LabelsElement("Set Global Position").Init(EditorElement.InitSettings.Default.Parent(GameObject.transform));
            var global = EditorPrefabHolder.Instance.ToggleButton.Duplicate(GameObject.transform, "global");
            var globalToggle = global.GetComponent<ToggleButtonStorage>();
            globalToggle.Text = "Global";

            GlobalToggle = globalToggle.toggle;

            new LabelsElement("Align Near Clip Plane").Init(EditorElement.InitSettings.Default.Parent(GameObject.transform));
            var align = EditorPrefabHolder.Instance.ToggleButton.Duplicate(GameObject.transform, "align");
            var alignToggle = align.GetComponent<ToggleButtonStorage>();
            alignToggle.Text = "Align";

            AlignToggle = alignToggle.toggle;

            EditorThemeManager.ApplyInputField(DepthField);
            EditorThemeManager.ApplyInputField(ZoomField);
            EditorThemeManager.ApplyToggle(GlobalToggle);
            EditorThemeManager.ApplyToggle(AlignToggle);
        }

        public override void Render()
        {
            base.Render();

            SetFloatInputField(DepthField, 0);
            SetFloatInputField(ZoomField, 1);
            SetToggle(GlobalToggle, 2, 0, 1);
            SetToggle(AlignToggle, 3, 1, 0);
        }
    }
}
