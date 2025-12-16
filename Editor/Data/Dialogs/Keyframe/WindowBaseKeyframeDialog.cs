using UnityEngine.UI;

using BetterLegacy.Core;
using BetterLegacy.Core.Prefabs;
using BetterLegacy.Editor.Managers;

namespace BetterLegacy.Editor.Data.Dialogs
{
    public class WindowBaseKeyframeDialog : KeyframeDialog
    {
        public WindowBaseKeyframeDialog() : base(EventLibrary.Indexes.WINDOW_BASE) { }

        public Toggle ForceToggle { get; set; }

        public Vector2InputFieldStorage ResolutionFields { get; set; }

        public Toggle AllowPositionToggle { get; set; }

        public override void Init()
        {
            CreateNew(EventEditor.inst.dialogRight);

            base.Init();

            new LabelsElement("Force Resolution").Init(EditorElement.InitSettings.Default.Parent(GameObject.transform));
            var force = EditorPrefabHolder.Instance.ToggleButton.Duplicate(GameObject.transform, "force");
            var forceToggle = force.GetComponent<ToggleButtonStorage>();
            forceToggle.Text = "Force";
            ForceToggle = forceToggle.toggle;

            new LabelsElement("Width", "Height").Init(EditorElement.InitSettings.Default.Parent(GameObject.transform));
            var resolution = EditorPrefabHolder.Instance.Vector2InputFields.Duplicate(GameObject.transform, "resolution");
            ResolutionFields = resolution.GetComponent<Vector2InputFieldStorage>();

            new LabelsElement("Allow Position Events").Init(EditorElement.InitSettings.Default.Parent(GameObject.transform));
            var allow = EditorPrefabHolder.Instance.ToggleButton.Duplicate(GameObject.transform, "allow");
            var allowToggle = allow.GetComponent<ToggleButtonStorage>();
            allowToggle.Text = "Allow";
            AllowPositionToggle = allowToggle.toggle;

            EditorThemeManager.ApplyToggle(ForceToggle);
            EditorThemeManager.ApplyInputField(ResolutionFields);
            EditorThemeManager.ApplyToggle(AllowPositionToggle);
        }

        public override void Render()
        {
            base.Render();

            SetToggle(ForceToggle, 0, 1, 0);
            SetVector2InputField(ResolutionFields, 1, 2, max: int.MaxValue, allowNegative: false);
            SetToggle(AllowPositionToggle, 3, 1, 0);
        }
    }
}
