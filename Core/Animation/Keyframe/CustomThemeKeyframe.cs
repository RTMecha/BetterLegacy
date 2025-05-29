using System.Collections.Generic;

using UnityEngine;

using BetterLegacy.Core.Helpers;

namespace BetterLegacy.Core.Animation.Keyframe
{
    /// <summary>
    /// A keyframe that animates a (theme, customizable) color value.
    /// </summary>
    public struct CustomThemeKeyframe : IKeyframe<Color>
    {
        public CustomThemeKeyframe(float time, int colorSource, int colorSlot, float opacity, float hue, float saturation, float value, EaseFunction ease, bool invertOpacity)
        {
            Time = time;
            this.colorSource = colorSource;
            this.colorSlot = colorSlot;
            this.opacity = opacity;
            this.hue = hue;
            this.saturation = saturation;
            this.value = value;
            Ease = ease;
            Active = false;
            this.invertOpacity = invertOpacity;
            Value = Color.white;
            TotalValue = Color.white;
            Relative = false;
        }

        public CustomThemeKeyframe(float time, int colorSlot, float opacity, float hue, float saturation, float value, EaseFunction ease, bool invertOpacity) : this(time, 0, colorSlot, opacity, hue, saturation, value, ease, invertOpacity) { }

        public CustomThemeKeyframe(float time, int colorSlot, float opacity, float hue, float saturation, float value, EaseFunction ease) : this(time, colorSlot, opacity, hue, saturation, value, ease, true) { }

        #region Values

        public bool Active { get; set; }

        public float Time { get; set; }
        public EaseFunction Ease { get; set; }
        public Color Value { get; set; }
        public Color TotalValue { get; set; }
        public bool Relative { get; set; }

        /// <summary>
        /// Source of the colors.
        /// </summary>
        public int colorSource;

        /// <summary>
        /// Slot index of the color to get from the current theme.
        /// </summary>
        public int colorSlot;

        /// <summary>
        /// Opacity of the returned color.
        /// </summary>
        public float opacity;

        /// <summary>
        /// Hue of HSV color.
        /// </summary>
        public float hue;

        /// <summary>
        /// Saturation of HSV color.
        /// </summary>
        public float saturation;

        /// <summary>
        /// Value of HSV color.
        /// </summary>
        public float value;

        /// <summary>
        /// If opacity should be inverted.
        /// </summary>
        public bool invertOpacity;

        List<Color> Theme => colorSource switch
        {
            0 => CoreHelper.CurrentBeatmapTheme.objectColors,
            1 => CoreHelper.CurrentBeatmapTheme.backgroundColors,
            2 => CoreHelper.CurrentBeatmapTheme.effectColors,
            _ => null,
        };

        #endregion

        #region Methods

        public void Start(IKeyframe<Color> prev, Color value, float time) => Active = true;

        public void Stop() => Active = false;

        public void SetEase(EaseFunction ease) => Ease = ease;

        public void SetValue(Color value) { }

        public Color GetValue()
        {
            var colorSlots = Theme;
            var color = colorSlots[colorSlot];
            var opacity = this.opacity;
            if (invertOpacity)
                opacity = -(opacity - 1f);

            return RTColors.FadeColor(
                    RTColors.ChangeColorHSV(color,
                        hue,
                        saturation,
                        value),
                    opacity);
        }

        public Color Interpolate(IKeyframe<Color> other, float time) => RTMath.Lerp(GetValue(), other.GetValue(), other.Ease(time));

        #endregion
    }
}
