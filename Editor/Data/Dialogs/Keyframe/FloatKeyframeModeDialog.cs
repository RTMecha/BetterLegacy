using System.Collections.Generic;

using UnityEngine.UI;

using BetterLegacy.Core;
using BetterLegacy.Core.Prefabs;
using BetterLegacy.Editor.Managers;

namespace BetterLegacy.Editor.Data.Dialogs
{
    public class FloatKeyframeModeDialog : FloatKeyframeDialog
    {
        public FloatKeyframeModeDialog(int type, List<Dropdown.OptionData> options) : base(type) => this.options = options;

        public FloatKeyframeModeDialog(int type, string valueLabel, List<Dropdown.OptionData> options) : base(type, valueLabel) => this.options = options;

        public FloatKeyframeModeDialog(int type, string valueLabel, string modeLabel, List<Dropdown.OptionData> options) : this(type, valueLabel, options) => this.modeLabel = modeLabel;

        public string modeLabel;

        public List<Dropdown.OptionData> options;

        public Dropdown Mode { get; set; }

        public override void Init()
        {
            CreateNew(EventEditor.inst.dialogRight);

            base.Init();

            new LabelsElement(!string.IsNullOrEmpty(modeLabel) ? modeLabel : "Mode").Init(EditorElement.InitSettings.Default.Parent(GameObject.transform));

            Mode = EditorPrefabHolder.Instance.Dropdown.Duplicate(GameObject.transform, "mode").GetComponent<Dropdown>();
            Mode.options = options ?? new List<Dropdown.OptionData>();
            EditorThemeManager.ApplyDropdown(Mode);
        }

        public override void Render()
        {
            base.Render();

            var currentKeyframe = RTEventEditor.inst.CurrentSelectedKeyframe;
            Mode.SetValueWithoutNotify((int)currentKeyframe.values[1]);
            Mode.onValueChanged.NewListener(_val => SetKeyframeValue(1, _val));
        }
    }
}
