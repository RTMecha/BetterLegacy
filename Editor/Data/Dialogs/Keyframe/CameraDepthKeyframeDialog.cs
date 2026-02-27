using UnityEngine;
using UnityEngine.UI;

using BetterLegacy.Core;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Prefabs;
using BetterLegacy.Editor.Managers;

namespace BetterLegacy.Editor.Data.Dialogs
{
    public class CameraDepthKeyframeDialog : KeyframeDialog
    {
        public CameraDepthKeyframeDialog() : base(EventLibrary.Indexes.CAMERA_DEPTH) { }

        public InputFieldStorage DepthField { get; set; }

        public InputFieldStorage BGZoomMultiplyField { get; set; }

        public Toggle GlobalToggle { get; set; }

        public Toggle AlignToggle { get; set; }

        public Toggle FOVAlignToggle { get; set; }

        public InputFieldStorage BGFOVMultiplyField { get; set; }

        public InputFieldStorage BGFOVField { get; set; }

        public Vector3InputFieldStorage PositionFields { get; set; }

        public Vector3InputFieldStorage RotationFields { get; set; }

        public override void Init()
        {
            CreateNew(EventEditor.inst.dialogRight);

            base.Init();

            new LabelsElement("Depth", "Zoom Multiply").Init(EditorElement.InitSettings.Default.Parent(GameObject.transform));
            var vector2Field = EditorPrefabHolder.Instance.Vector2InputFields.Duplicate(GameObject.transform, "position").GetOrAddComponent<Vector2InputFieldStorage>();
            DepthField = vector2Field.x;
            BGZoomMultiplyField = vector2Field.y;

            new LabelsElement("Set Global Position").Init(EditorElement.InitSettings.Default.Parent(GameObject.transform));
            var global = EditorPrefabHolder.Instance.ToggleButton.Duplicate(GameObject.transform, "global");
            var globalToggle = global.GetComponent<ToggleButtonStorage>();
            globalToggle.Text = "Global";

            GlobalToggle = globalToggle.toggle;

            var alignLabels = new LabelsElement("Near Clip Plane", "BG Field of View");
            alignLabels.Init(EditorElement.InitSettings.Default.Parent(GameObject.transform));
            alignLabels.labels[0].GameObject.transform.AsRT().sizeDelta = new Vector2(194f, 20f);
            var alignLayout = new LayoutGroupElement(HorizontalOrVerticalLayoutValues.Horizontal.Spacing(8f));
            alignLayout.Init(EditorElement.InitSettings.Default.Parent(GameObject.transform));

            var align = EditorPrefabHolder.Instance.ToggleButton.Duplicate(alignLayout.GameObject.transform, "align");
            var alignToggle = align.GetComponent<ToggleButtonStorage>();
            alignToggle.Text = "Align";

            AlignToggle = alignToggle.toggle;

            var bgFOVAlign = EditorPrefabHolder.Instance.ToggleButton.Duplicate(alignLayout.GameObject.transform, "alignfov");
            var bgFOVAlignToggle = bgFOVAlign.GetComponent<ToggleButtonStorage>();
            bgFOVAlignToggle.Text = "Align";

            FOVAlignToggle = bgFOVAlignToggle.toggle;

            new LabelsElement("Field of View Multiply").Init(EditorElement.InitSettings.Default.Parent(GameObject.transform));
            var fovMultiply = EditorPrefabHolder.Instance.LayoutInputField.Duplicate(GameObject.transform, "fovmultiply");
            BGFOVMultiplyField = fovMultiply.transform.Find("x").gameObject.GetOrAddComponent<InputFieldStorage>();
            BGFOVMultiplyField.inputField.image.rectTransform.sizeDelta = new Vector2(317f, 32f);

            var fovLabel = new LabelsElement("Field of View (Not aligned)");
            fovLabel.Init(EditorElement.InitSettings.Default.Parent(GameObject.transform));
            fovLabel.labels[0].GameObject.transform.AsRT().sizeDelta = new Vector2(300f, 20f);
            var fov = EditorPrefabHolder.Instance.LayoutInputField.Duplicate(GameObject.transform, "fov");
            BGFOVField = fov.transform.Find("x").gameObject.GetOrAddComponent<InputFieldStorage>();
            BGFOVField.inputField.image.rectTransform.sizeDelta = new Vector2(317f, 32f);

            new LabelsElement(HorizontalOrVerticalLayoutValues.Horizontal.ChildControlHeight(false), "Position X", "Position Y", "Position Z").Init(EditorElement.InitSettings.Default.Parent(GameObject.transform));
            var position = EditorPrefabHolder.Instance.Vector3InputFields.Duplicate(GameObject.transform, "position");
            PositionFields = position.GetComponent<Vector3InputFieldStorage>();
            PositionFields.Assign();
            PositionFields.SetSize(new Vector2(122f, 32f), new Vector2(60f, 32f));

            new LabelsElement(HorizontalOrVerticalLayoutValues.Horizontal.ChildControlHeight(false), "Rotation X", "Rotation Y", "Rotation Z").Init(EditorElement.InitSettings.Default.Parent(GameObject.transform));
            var rotation = EditorPrefabHolder.Instance.Vector3InputFields.Duplicate(GameObject.transform, "rotation");
            RotationFields = rotation.GetComponent<Vector3InputFieldStorage>();
            RotationFields.Assign();
            RotationFields.SetSize(new Vector2(122f, 32f), new Vector2(60f, 32f));

            EditorThemeManager.ApplyInputField(DepthField);
            EditorThemeManager.ApplyInputField(BGZoomMultiplyField);
            EditorThemeManager.ApplyToggle(GlobalToggle);
            EditorThemeManager.ApplyToggle(AlignToggle);
            EditorThemeManager.ApplyToggle(FOVAlignToggle);
            EditorThemeManager.ApplyInputField(BGFOVMultiplyField);
            EditorThemeManager.ApplyInputField(BGFOVField);
            EditorThemeManager.ApplyInputField(PositionFields);
            EditorThemeManager.ApplyInputField(RotationFields);
        }

        public override void Render()
        {
            base.Render();

            SetFloatInputField(DepthField, 0);
            SetFloatInputField(BGZoomMultiplyField, 4);
            SetToggle(GlobalToggle, 2, 0, 1);
            SetToggle(AlignToggle, 3, 1, 0);
            SetToggle(FOVAlignToggle, 5, 1, 0);
            SetFloatInputField(BGFOVMultiplyField, 6);
            SetFloatInputField(BGFOVField, 7);

            // Position
            SetFloatInputField(PositionFields.x, 8);
            SetFloatInputField(PositionFields.y, 9);
            SetFloatInputField(PositionFields.z, 1);

            // Rotation
            SetFloatInputField(RotationFields.x, 10, 15f, 3f);
            SetFloatInputField(RotationFields.y, 11, 15f, 3f);
            SetFloatInputField(RotationFields.z, 12, 15f, 3f);
        }
    }
}
