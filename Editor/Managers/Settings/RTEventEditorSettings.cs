using UnityEngine;

using BetterLegacy.Core.Managers.Settings;

namespace BetterLegacy.Editor.Managers.Settings
{
    public class RTEventEditorSettings : ManagerSettings
    {
        public RTEventEditorSettings() { }

        public override Transform Parent => EventEditor.inst.transform;
        public override bool IsComponent => true;
    }
}
