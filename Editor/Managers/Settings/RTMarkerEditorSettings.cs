using UnityEngine;

using BetterLegacy.Core.Managers.Settings;

namespace BetterLegacy.Editor.Managers.Settings
{
    public class RTMarkerEditorSettings : ManagerSettings
    {
        public RTMarkerEditorSettings() { }

        public override Transform Parent => MarkerEditor.inst.transform;
        public override bool IsComponent => true;
    }
}
