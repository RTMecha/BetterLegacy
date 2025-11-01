using UnityEngine;

namespace BetterLegacy.Core.Managers.Settings
{
    /// <summary>
    /// Manager settings for <see cref="PlayerManager"/>.
    /// </summary>
    public class PlayerManagerSettings : ManagerSettings
    {
        public PlayerManagerSettings() { }

        public override string ClassName => "[<color=#E57373>PlayerManager</color>] \n";
        public override Color Color => RTColors.HexToColor("E57373");
    }
}
