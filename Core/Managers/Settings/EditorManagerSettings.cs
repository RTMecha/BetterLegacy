using UnityEngine;

namespace BetterLegacy.Core.Managers.Settings
{
    /// <summary>
    /// Represents the settings an editor manager is initialized with.
    /// </summary>
    public class EditorManagerSettings : ManagerSettings
    {
        public EditorManagerSettings() { }

        public override Transform Parent => EditorManager.inst.transform.parent;
    }
}
