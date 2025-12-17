using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;

using BetterLegacy.Configs;
using BetterLegacy.Core;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Managers;
using BetterLegacy.Core.Prefabs;
using BetterLegacy.Editor.Managers;

namespace BetterLegacy.Editor.Data.Dialogs
{
    public class BGKeyframeDialog : KeyframeDialog
    {
        public BGKeyframeDialog() : base(EventLibrary.Indexes.BG) { }

        public Toggle ActiveToggle { get; set; }

        public List<Toggle> ColorToggles { get; set; }

        public Vector3InputFieldStorage HSVFields { get; set; }

        public override void Init()
        {
            CreateNew(EventEditor.inst.dialogRight);

            base.Init();

            new LabelsElement(HorizontalOrVerticalLayoutValues.Horizontal.ChildControlHeight(false), "Background Objects Active").Init(EditorElement.InitSettings.Default.Parent(GameObject.transform));
            var active = EditorPrefabHolder.Instance.ToggleButton.Duplicate(GameObject.transform, "active");
            var activeToggle = active.GetComponent<ToggleButtonStorage>();
            activeToggle.Text = "Active";

            new LabelsElement("Colors").Init(EditorElement.InitSettings.Default.Parent(GameObject.transform));
            var colors = new ColorGroupElement(19, new Vector2(366f, 64f), new Vector2(32f, 32f), new Vector2(5f, 5f));
            colors.Init(EditorElement.InitSettings.Default.Parent(GameObject.transform).Name("colors"));
            ColorToggles = colors.toggles;

            new LabelsElement(HorizontalOrVerticalLayoutValues.Horizontal.ChildControlHeight(false), "Hue", "Sat", "Val").Init(EditorElement.InitSettings.Default.Parent(GameObject.transform));
            var colorshift = EditorPrefabHolder.Instance.Vector3InputFields.Duplicate(GameObject.transform, "colorshift");

            ActiveToggle = activeToggle.toggle;

            HSVFields = colorshift.GetOrAddComponent<Vector3InputFieldStorage>();
            HSVFields.Assign();
            HSVFields.SetSize(new Vector2(122f, 32f), new Vector2(60f, 32f));

            EditorThemeManager.ApplyToggle(ActiveToggle);
            EditorThemeManager.ApplyInputField(HSVFields);
        }

        public override void Render()
        {
            base.Render();

            var currentKeyframe = RTEventEditor.inst.CurrentSelectedKeyframe;

            SetListColor((int)currentKeyframe.values[0], 0, ColorToggles, ThemeManager.inst.Current.backgroundColor, Color.black, -1, 2, 3, 4);

            SetToggle(ActiveToggle, 1, 0, 1);

            // BG Color Shift
            SetFloatInputField(HSVFields.x, 2, onValueChanged: _val =>
            {
                if (EditorConfig.Instance.ShowModifiedColors.Value)
                    SetListColor((int)currentKeyframe.values[0], 0, ColorToggles, ThemeManager.inst.Current.backgroundColor, Color.black, -1, 2, 3, 4);
            });
            SetFloatInputField(HSVFields.y, 3, onValueChanged: _val =>
            {
                if (EditorConfig.Instance.ShowModifiedColors.Value)
                    SetListColor((int)currentKeyframe.values[0], 0, ColorToggles, ThemeManager.inst.Current.backgroundColor, Color.black, -1, 2, 3, 4);
            });
            SetFloatInputField(HSVFields.z, 4, onValueChanged: _val =>
            {
                if (EditorConfig.Instance.ShowModifiedColors.Value)
                    SetListColor((int)currentKeyframe.values[0], 0, ColorToggles, ThemeManager.inst.Current.backgroundColor, Color.black, -1, 2, 3, 4);
            });
        }
    }
}
