using UnityEngine;

namespace BetterLegacy.Core.Managers.Settings
{
    /// <summary>
    /// Manager settings for <see cref="RTSteamManager"/>.
    /// </summary>
    public class RTSteamManagerSettings : ManagerSettings
    {
        public RTSteamManagerSettings() { }

        public override Transform Parent => SteamManager.Instance ? SteamManager.Instance.transform : SystemManager.inst.transform;

        public override bool IsComponent => SteamManager.Instance;

        public override string ClassName => "[<color=#E81E62>Steam</color>] \n";
        public override Color Color => RTColors.HexToColor("E81E62");
    }
}
