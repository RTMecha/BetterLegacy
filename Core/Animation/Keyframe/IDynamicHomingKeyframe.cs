using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BetterLegacy.Core.Animation.Keyframe
{
    public interface IDynamicHomingKeyframe
    {
        public float MinRange { get; set; }

        public float MaxRange { get; set; }

        public float Delay { get; set; }

        public float CalculateDelay();
    }
}
