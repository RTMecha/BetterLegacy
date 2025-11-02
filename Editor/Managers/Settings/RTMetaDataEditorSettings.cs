using UnityEngine;

using BetterLegacy.Core.Managers.Settings;

namespace BetterLegacy.Editor.Managers.Settings
{
    public class RTMetaDataEditorSettings : ManagerSettings
    {
        public RTMetaDataEditorSettings() { }

        public override Transform Parent => MetadataEditor.inst.transform;
        public override bool IsComponent => true;
    }
}
