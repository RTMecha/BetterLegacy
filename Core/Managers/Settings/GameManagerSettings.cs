using UnityEngine;

namespace BetterLegacy.Core.Managers.Settings
{
    /// <summary>
    /// Represents the settings a game manager is initialized with.
    /// </summary>
    public class GameManagerSettings : ManagerSettings
    {
        public GameManagerSettings() { }

        public override Transform Parent => GameManager.inst.transform.parent;
    }
}
