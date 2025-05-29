using System.Collections.Generic;

using UnityEngine;

using BetterLegacy.Core.Helpers;

namespace BetterLegacy.Core.Animation.Keyframe
{
    /// <summary>
    /// A keyframe that animates a (theme) color value.
    /// </summary>
    public struct ThemeKeyframe : IKeyframe<Color>
    {
        public ThemeKeyframe(float time, int colorSlot, float opacity, float hue, float saturation, float value, EaseFunction ease)
        {
            Time = time;
            this.colorSlot = colorSlot;
            this.opacity = opacity;
            this.hue = hue;
            this.saturation = saturation;
            this.value = value;
            Ease = ease;
            Active = false;
            Value = Color.white;
            TotalValue = Color.white;
            Relative = false;
        }

        #region Values

        public bool Active { get; set; }
        public float Time { get; set; }
        public EaseFunction Ease { get; set; }
        public Color Value { get; set; }
        public Color TotalValue { get; set; }
        public bool Relative { get; set; }

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

        List<Color> Theme => CoreHelper.CurrentBeatmapTheme.objectColors;

        #endregion

        #region Methods

        public void Start(IKeyframe<Color> prev, Color value, float time) => Active = true;

        public void Stop() => Active = false;

        public void SetEase(EaseFunction ease) => Ease = ease;

        public void SetValue(Color value) { }

        public Color GetValue() => RTColors.FadeColor(
                    RTColors.ChangeColorHSV(Theme[colorSlot],
                        hue,
                        saturation,
                        value),
                    -(opacity - 1f));

        public Color Interpolate(IKeyframe<Color> other, float time)
        {
            // interpolate HSV values so the color values properly transition.
            if (other is ThemeKeyframe themeKeyframe)
            {
                var ease = other.Ease(time);

                return RTColors.FadeColor(
                            RTColors.ChangeColorHSV(RTMath.Lerp(Theme[colorSlot], Theme[themeKeyframe.colorSlot], ease),
                                RTMath.Lerp(hue, themeKeyframe.hue, ease),
                                RTMath.Lerp(saturation, themeKeyframe.saturation, ease),
                                RTMath.Lerp(value, themeKeyframe.value, ease)),
                            -(RTMath.Lerp(opacity, themeKeyframe.opacity, ease) - 1f));
            }

            // if other is not a ThemeKeyframe (e.g. ColorKeyframe), just interpolate the color values.
            return RTMath.Lerp(GetValue(), other.GetValue(), other.Ease(time));
        }

        #endregion
    }
}
