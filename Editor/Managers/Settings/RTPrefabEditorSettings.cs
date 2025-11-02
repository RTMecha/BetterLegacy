using UnityEngine;

using BetterLegacy.Core.Managers.Settings;

namespace BetterLegacy.Editor.Managers.Settings
{
    public class RTPrefabEditorSettings : ManagerSettings
    {
        public RTPrefabEditorSettings() { }

        public override Transform Parent => PrefabEditor.inst.transform;
        public override bool IsComponent => true;
    }
}
