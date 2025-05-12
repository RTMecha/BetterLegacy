using UnityEngine;

using BetterLegacy.Core.Helpers;

namespace BetterLegacy.Core.Runtime.Events
{
    /// <summary>
    /// Represents a theme color value.
    /// </summary>
    public struct ThemeColor
    {
        public ThemeColor(int colorSlot, float hue, float saturation, float value, float opacity, int colorSource)
        {
            this.colorSlot = colorSlot;
            this.hue = hue;
            this.saturation = saturation;
            this.value = value;
            this.opacity = opacity;
            this.colorSource = colorSource;
        }

        public ThemeColor(int colorSlot, float hue, float saturation, float value, int colorSource) : this(colorSlot, hue, saturation, value, 1f, colorSource) { }

        /// <summary>
        /// The source of the theme color slots.
        /// </summary>
        public int colorSource;

        /// <summary>
        /// Color slot to get.
        /// </summary>
        public int colorSlot;

        /// <summary>
        /// Hue shift of the color.
        /// </summary>
        public float hue;

        /// <summary>
        /// Saturation of the color.
        /// </summary>
        public float saturation;

        /// <summary>
        /// Value of the color.
        /// </summary>
        public float value;

        /// <summary>
        /// Opacity of the color.
        /// </summary>
        public float opacity;

        /// <summary>
        /// Calculates the color.
        /// </summary>
        /// <returns>Returns the theme color.</returns>
        public Color GetColor()
        {
            var color = colorSource switch
            {
                0 => CoreHelper.CurrentBeatmapTheme.GetObjColor(colorSlot),
                1 => CoreHelper.CurrentBeatmapTheme.GetFXColor(colorSlot),
                2 => CoreHelper.CurrentBeatmapTheme.GetPlayerColor(colorSlot),
                _ => CoreHelper.CurrentBeatmapTheme.GetBGColor(colorSlot),
            };
            color = RTColors.ChangeColorHSV(color, hue, saturation, value);
            color = RTColors.FadeColor(color, opacity);
            return color;
        }
    }
}
