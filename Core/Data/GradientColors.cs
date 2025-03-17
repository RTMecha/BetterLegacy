using UnityEngine;

namespace BetterLegacy.Core.Data
{
    public struct GradientColors
    {
        public GradientColors(Color startColor, Color endColor)
        {
            this.startColor = startColor;
            this.endColor = endColor;
        }

        public Color startColor;
        public Color endColor;
    }
}
