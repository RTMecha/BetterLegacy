using UnityEngine;

using BetterLegacy.Core.Managers.Settings;

namespace BetterLegacy.Editor.Managers.Settings
{
    public class RTBackgroundEditorSettings : ManagerSettings
    {
        public RTBackgroundEditorSettings() { }

        public override Transform Parent => EditorManager.inst.transform.parent.Find("BackgroundEditor");
        public override bool IsComponent => true;
    }
}
