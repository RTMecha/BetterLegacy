using UnityEngine;

namespace BetterLegacy.Core.Managers.Settings
{
    /// <summary>
    /// Manager settings for <see cref="SteamLobbyManager"/>.
    /// </summary>
    public class SteamLobbyManagerSettings : ManagerSettings
    {
        public SteamLobbyManagerSettings() { }

        public override string ClassName => "[<color=#E81E62>SteamLobby</color>] \n";
        public override Color Color => RTColors.HexToColor("1A1899");
    }
}
