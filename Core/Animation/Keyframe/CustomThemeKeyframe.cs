using System.Collections.Generic;

using UnityEngine;

using LSFunctions;

using BetterLegacy.Core.Helpers;

namespace BetterLegacy.Core.Animation.Keyframe
{
    /// <summary>
    /// A keyframe that animates a (theme, customizable) color value.
    /// </summary>
    public struct CustomThemeKeyframe : IKeyframe<Color>
    {
        public bool Active { get; set; }

        public float Time { get; set; }
        public EaseFunction Ease { get; set; }

        public int colorSource;
        public int colorSlot;
        public float opacity;
        public float hue;
        public float saturation;
        public float value;

        public bool invertOpacity;

        List<Color> Theme => colorSource switch
        {
            0 => CoreHelper.CurrentBeatmapTheme.objectColors,
            1 => CoreHelper.CurrentBeatmapTheme.backgroundColors,
            2 => CoreHelper.CurrentBeatmapTheme.effectColors,
            _ => null,
        };

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
        }

        public CustomThemeKeyframe(float time, int colorSlot, float opacity, float hue, float saturation, float value, EaseFunction ease, bool invertOpacity) : this(time, 0, colorSlot, opacity, hue, saturation, value, ease, invertOpacity) { }

        public CustomThemeKeyframe(float time, int colorSlot, float opacity, float hue, float saturation, float value, EaseFunction ease) : this(time, colorSlot, opacity, hue, saturation, value, ease, true) { }
        
        public void Start(float time) => Active = true;

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

            return LSColors.fadeColor(
                    RTColors.ChangeColorHSV(color,
                        hue,
                        saturation,
                        value),
                    opacity);
        }

        public Color Interpolate(IKeyframe<Color> other, float time) => RTMath.Lerp(GetValue(), other.GetValue(), other.Ease(time));
    }
}
