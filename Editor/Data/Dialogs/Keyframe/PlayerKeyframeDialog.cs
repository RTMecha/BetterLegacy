using UnityEngine;
using UnityEngine.UI;

using BetterLegacy.Core;
using BetterLegacy.Core.Prefabs;
using BetterLegacy.Core.Runtime.Events;
using BetterLegacy.Editor.Managers;

namespace BetterLegacy.Editor.Data.Dialogs
{
    public class PlayerKeyframeDialog : KeyframeDialog
    {
        public PlayerKeyframeDialog() : base(EventEngine.PLAYER) { }

        public Toggle ActiveToggle { get; set; }

        public Toggle MoveToggle { get; set; }

        public Vector2InputFieldStorage PositionFields { get; set; }

        public InputFieldStorage RotationField { get; set; }

        public Toggle OutOfBoundsToggle { get; set; }

        public override void Init()
        {
            CreateNew(EventEditor.inst.dialogRight);

            base.Init();

            new LabelsElement("Players Active").Init(EditorElement.InitSettings.Default.Parent(GameObject.transform));
            var active = EditorPrefabHolder.Instance.ToggleButton.Duplicate(GameObject.transform, "active");
            var activeToggle = active.GetComponent<ToggleButtonStorage>();
            activeToggle.Text = "Active";
            ActiveToggle = activeToggle.toggle;

            new LabelsElement("Can Move").Init(EditorElement.InitSettings.Default.Parent(GameObject.transform));
            var move = EditorPrefabHolder.Instance.ToggleButton.Duplicate(GameObject.transform, "move");
            var moveToggle = move.GetComponent<ToggleButtonStorage>();
            moveToggle.Text = "Moveable";
            MoveToggle = moveToggle.toggle;

            new LabelsElement("Can Exit Bounds").Init(EditorElement.InitSettings.Default.Parent(GameObject.transform));
            var outOfBounds = EditorPrefabHolder.Instance.ToggleButton.Duplicate(GameObject.transform, "oob");
            var outOfBoundsToggle = outOfBounds.GetComponent<ToggleButtonStorage>();
            outOfBoundsToggle.Text = "Out of Bounds";
            OutOfBoundsToggle = outOfBoundsToggle.toggle;

            new LabelsElement("Position X", "Position Y").Init(EditorElement.InitSettings.Default.Parent(GameObject.transform));
            var position = EditorPrefabHolder.Instance.Vector2InputFields.Duplicate(GameObject.transform, "position");
            var positionFields = position.GetComponent<Vector2InputFieldStorage>();
            PositionFields = positionFields;

            new LabelsElement("Rotation").Init(EditorElement.InitSettings.Default.Parent(GameObject.transform));
            var rotation = EditorPrefabHolder.Instance.LayoutInputField.Duplicate(GameObject.transform, "rotation");
            RotationField = rotation.transform.Find("x").gameObject.GetOrAddComponent<InputFieldStorage>();
            RotationField.inputField.image.rectTransform.sizeDelta = new Vector2(317f, 32f);

            EditorThemeManager.ApplyToggle(ActiveToggle);
            EditorThemeManager.ApplyToggle(MoveToggle);
            EditorThemeManager.ApplyToggle(OutOfBoundsToggle);
            EditorThemeManager.ApplyInputField(PositionFields);
            EditorThemeManager.ApplyInputField(RotationField);
        }

        public override void Render()
        {
            base.Render();

            SetToggle(ActiveToggle, 0, 0, 1);
            SetToggle(MoveToggle, 1, 0, 1);
            SetVector2InputField(PositionFields, 2, 3);
            SetFloatInputField(RotationField, 4, 15f, 3f);
            SetToggle(OutOfBoundsToggle, 5, 1, 0);
        }
    }
}
