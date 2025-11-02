using UnityEngine;

using BetterLegacy.Core.Managers.Settings;

namespace BetterLegacy.Editor.Managers.Settings
{
    public class RTEditorSettings : ManagerSettings
    {
        public RTEditorSettings() { }

        public override Transform Parent => EditorManager.inst.transform;
        public override bool IsComponent => true;
    }
}
