using UnityEngine;

using LSFunctions;

namespace BetterLegacy.Core
{
    public static class RTColors
    {
        public static Color ChangeColorHSV(Color color, float hue, float sat, float val)
        {
            double num;
            double saturation;
            double value;
            LSColors.ColorToHSV(color, out num, out saturation, out value);
            return LSColors.ColorFromHSV(num + hue, saturation + sat, value + val);
        }

        public static Color defaultBloomColor = Color.white;
        public static Color defaultVignetteColor = Color.black;
        public static Color defaultGradientColor1 = new Color(0f, 0.8f, 0.56f, 0.5f);
        public static Color defaultGradientColor2 = new Color(0.81f, 0.37f, 1f, 0.5f);
        public static Color defaultDangerColor = new Color(0.66f, 0f, 0f);
    }
}
