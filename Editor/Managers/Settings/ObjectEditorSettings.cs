using UnityEngine;

using BetterLegacy.Core.Managers.Settings;

namespace BetterLegacy.Editor.Managers.Settings
{
    public class ObjectEditorSettings : ManagerSettings
    {
        public ObjectEditorSettings() { }

        public override Transform Parent => ObjEditor.inst.transform;
        public override bool IsComponent => true;
    }
}
