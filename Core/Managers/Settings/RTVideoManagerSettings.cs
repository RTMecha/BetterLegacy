using UnityEngine;

namespace BetterLegacy.Core.Managers.Settings
{
    /// <summary>
    /// Manager settings for <see cref="RTVideoManager"/>.
    /// </summary>
    public class RTVideoManagerSettings : ManagerSettings
    {
        public RTVideoManagerSettings() { }

        public override string ClassName => "[<color=#E65100>RTVideoManager</color>] \n";
        public override Color Color => RTColors.HexToColor("E65100");
    }
}
