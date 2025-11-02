using UnityEngine;

using BetterLegacy.Core.Managers.Settings;

namespace BetterLegacy.Editor.Managers.Settings
{
    public class RTCheckpointEditorSettings : ManagerSettings
    {
        public RTCheckpointEditorSettings() { }

        public override Transform Parent => EditorManager.inst.transform.parent.Find("CheckpointEditor");
        public override bool IsComponent => true;
    }
}
