using BetterLegacy.Core.Helpers;
using LSFunctions;
using System.Collections.Generic;
using UnityEngine;

namespace BetterLegacy.Core.Animation.Keyframe
{
    /// <summary>
    /// A keyframe that animates a (theme) color value.
    /// </summary>
    public struct NewThemeKeyframe : IKeyframe<Color>
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

        public NewThemeKeyframe(float time, int value, float opacity, float hue, float saturation, float brightness, EaseFunction ease)
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

        public void Start() { }

        public void Stop() => Active = false;

        public void SetEase(EaseFunction ease) => Ease = ease;

        public Color Interpolate(IKeyframe<Color> other, float time)
        {
            var second = (NewThemeKeyframe)other;
            var ease = second.Ease(time);
            var color = RTMath.Lerp(Theme[colorSlot], Theme[second.colorSlot], ease);

            color =
                LSColors.fadeColor(
                    CoreHelper.ChangeColorHSV(color,
                        RTMath.Lerp(hue, second.hue, ease),
                        RTMath.Lerp(saturation, second.saturation, ease),
                        RTMath.Lerp(brightness, second.brightness, ease)),
                    RTMath.Lerp(opacity, second.opacity, ease));

            return color;
        }
    }
}
