using UnityEngine;

using BetterLegacy.Core.Managers.Settings;

namespace BetterLegacy.Editor.Managers.Settings
{
    public class RTThemeEditorSettings : ManagerSettings
    {
        public RTThemeEditorSettings() { }

        public override Transform Parent => ThemeEditor.inst.transform;
        public override bool IsComponent => true;
    }
}
