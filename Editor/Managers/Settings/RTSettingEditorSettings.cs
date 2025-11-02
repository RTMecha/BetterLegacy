using UnityEngine;

using BetterLegacy.Core.Managers.Settings;

namespace BetterLegacy.Editor.Managers.Settings
{
    public class RTSettingEditorSettings : ManagerSettings
    {
        public RTSettingEditorSettings() { }

        public override Transform Parent => SettingEditor.inst.transform;
        public override bool IsComponent => true;
    }
}
