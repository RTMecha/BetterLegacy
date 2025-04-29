using BetterLegacy.Core.Data;
using BetterLegacy.Core.Data.Beatmap;

namespace BetterLegacy.Core.Runtime.Objects
{
    public class RTBackgroundObject : Exists, IRTObject
    {
        public float StartTime { get; set; }
        public float KillTime { get; set; }

        public BackgroundObject backgroundObject;

        public void Clear()
        {

        }

        public void SetActive(bool active)
        {

        }

        public void Interpolate(float time)
        {

        }
    }
}
