using UnityEngine;

namespace BetterLegacy.Core.Managers.Settings
{
    /// <summary>
    /// Manager settings for <see cref="LevelManager"/>.
    /// </summary>
    public class LevelManagerSettings : ManagerSettings
    {
        public LevelManagerSettings() { }

        public override string ClassName => "[<color=#7F00FF>LevelManager</color>] \n";
        public override Color Color => RTColors.HexToColor("7F00FF");
    }
}
