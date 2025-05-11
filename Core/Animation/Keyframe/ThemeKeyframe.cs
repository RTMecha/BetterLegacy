using System.Collections.Generic;

using UnityEngine;

using LSFunctions;

using BetterLegacy.Core.Helpers;

namespace BetterLegacy.Core.Animation.Keyframe
{
    /// <summary>
    /// A keyframe that animates a (theme) color value.
    /// </summary>
    public struct ThemeKeyframe : IKeyframe<Color>
    {
        public bool Active { get; set; }

        public float Time { get; set; }
        public EaseFunction Ease { get; set; }

        public int colorSlot;
        public float opacity;
        public float hue;
        public float saturation;
        public float brightness;

        List<Color> Theme => CoreHelper.CurrentBeatmapTheme.objectColors;

        public ThemeKeyframe(float time, int value, float opacity, float hue, float saturation, float brightness, EaseFunction ease)
        {
            Time = time;
            colorSlot = value;
            this.opacity = opacity;
            this.hue = hue;
            this.saturation = saturation;
            this.brightness = brightness;
            Ease = ease;
            Active = false;
        }

        public void Start(float time) => Active = true;

        public void Stop() => Active = false;

        public void SetEase(EaseFunction ease) => Ease = ease;

        public void SetValue(Color value) { }

        public Color GetValue() => LSColors.fadeColor(
                    RTColors.ChangeColorHSV(Theme[colorSlot],
                        hue,
                        saturation,
                        brightness),
                    -(opacity - 1f));

        public Color Interpolate(IKeyframe<Color> other, float time) => RTMath.Lerp(GetValue(), other.GetValue(), other.Ease(time));
    }
}
