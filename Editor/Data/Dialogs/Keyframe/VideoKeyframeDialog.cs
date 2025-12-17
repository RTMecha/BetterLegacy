using UnityEngine;
using UnityEngine.UI;

using BetterLegacy.Core;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Prefabs;
using BetterLegacy.Editor.Managers;

namespace BetterLegacy.Editor.Data.Dialogs
{
    public class VideoKeyframeDialog : KeyframeDialog
    {
        public VideoKeyframeDialog(int type, bool doRenderType) : base(type) => this.doRenderType = doRenderType;

        public bool doRenderType;

        public Vector3InputFieldStorage PositionFields { get; set; }

        public Vector3InputFieldStorage ScaleFields { get; set; }

        public Vector3InputFieldStorage RotationFields { get; set; }

        public Dropdown RenderTypeDropdown { get; set; }

        public override void Init()
        {
            CreateNew(EventEditor.inst.dialogRight);

            base.Init();

            new LabelsElement(HorizontalOrVerticalLayoutValues.Horizontal.ChildControlHeight(false), "Position X", "Position Y", "Position Z").Init(EditorElement.InitSettings.Default.Parent(GameObject.transform));
            var position = EditorPrefabHolder.Instance.Vector3InputFields.Duplicate(GameObject.transform, "position");
            PositionFields = position.GetComponent<Vector3InputFieldStorage>();
            PositionFields.Assign();
            PositionFields.SetSize(new Vector2(122f, 32f), new Vector2(60f, 32f));

            new LabelsElement(HorizontalOrVerticalLayoutValues.Horizontal.ChildControlHeight(false), "Scale X", "Scale Y", "Scale Z").Init(EditorElement.InitSettings.Default.Parent(GameObject.transform));
            var scale = EditorPrefabHolder.Instance.Vector3InputFields.Duplicate(GameObject.transform, "scale");
            ScaleFields = scale.GetComponent<Vector3InputFieldStorage>();
            ScaleFields.Assign();
            ScaleFields.SetSize(new Vector2(122f, 32f), new Vector2(60f, 32f));

            new LabelsElement(HorizontalOrVerticalLayoutValues.Horizontal.ChildControlHeight(false), "Rotation X", "Rotation Y", "Rotation Z").Init(EditorElement.InitSettings.Default.Parent(GameObject.transform));
            var rotation = EditorPrefabHolder.Instance.Vector3InputFields.Duplicate(GameObject.transform, "rotation");
            RotationFields = rotation.GetComponent<Vector3InputFieldStorage>();
            RotationFields.Assign();
            RotationFields.SetSize(new Vector2(122f, 32f), new Vector2(60f, 32f));

            if (doRenderType)
            {
                new LabelsElement("Render Type").Init(EditorElement.InitSettings.Default.Parent(GameObject.transform));

                var renderType = EditorPrefabHolder.Instance.Dropdown.Duplicate(GameObject.transform, "rendertype");
                RenderTypeDropdown = renderType.GetComponent<Dropdown>();
                RenderTypeDropdown.options = CoreHelper.StringToOptionData("Background", "Foreground");
            }

            EditorThemeManager.ApplyInputField(PositionFields);
            EditorThemeManager.ApplyInputField(ScaleFields);
            EditorThemeManager.ApplyInputField(RotationFields);
            EditorThemeManager.ApplyDropdown(RenderTypeDropdown);
        }

        public override void Render()
        {
            base.Render();

            // Position
            SetFloatInputField(PositionFields.x, 0);
            SetFloatInputField(PositionFields.y, 1);
            SetFloatInputField(PositionFields.z, 2);

            // Scale
            SetVector2InputField(ScaleFields, 3, 4);
            SetFloatInputField(ScaleFields.z, 5);

            // Rotation
            SetFloatInputField(RotationFields.x, 6, 15f, 3f);
            SetFloatInputField(RotationFields.y, 7, 15f, 3f);
            SetFloatInputField(RotationFields.z, 8, 15f, 3f);

            if (!RenderTypeDropdown)
                return;

            // Render Type
            RenderTypeDropdown.SetValueWithoutNotify((int)RTEventEditor.inst.CurrentSelectedKeyframe.values[9]);
            RenderTypeDropdown.onValueChanged.NewListener(_val => SetKeyframeValue(9, _val));
        }
    }
}
