using UnityEngine;

namespace BetterLegacy.Core.Managers.Settings
{
    /// <summary>
    /// Manager settings for <see cref="SoundManager"/>.
    /// </summary>
    public class SoundManagerSettings : ManagerSettings
    {
        public SoundManagerSettings() { }

        public override Transform Parent => AudioManager.inst.transform;

        public override bool IsComponent => true;
    }
}
