using UnityEngine;

namespace BetterLegacy.Core.Managers.Settings
{
    /// <summary>
    /// Manager settings for <see cref="FontManager"/>.
    /// </summary>
    public class FontManagerSettings : ManagerSettings
    {
        public FontManagerSettings() { }

        public override string ClassName => "[<color=#A100FF>FontManager</color>] \n";
        public override Color Color => RTColors.HexToColor("A100FF");
    }
}
