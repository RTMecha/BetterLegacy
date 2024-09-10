using BetterLegacy.Core.Helpers;
using LSFunctions;
using System.Collections.Generic;
using UnityEngine;

namespace BetterLegacy.Core.Animation.Keyframe
{
    /// <summary>
    /// A keyframe that animates a (theme) color value.
    /// </summary>
    public struct GradientThemeKeyframe : IKeyframe<KeyValuePair<Color, Color>>
    {
        public bool Active { get; set; }

        public float Time { get; set; }
        public EaseFunction Ease { get; set; }

        public int colorSlot;
        public float opacity;
        public float hue;
        public float saturation;
        public float value;

        public int secondColorSlot;
        public float secondOpacity;
        public float secondHue;
        public float secondSaturation;
        public float secondValue;

        List<Color> Theme => CoreHelper.CurrentBeatmapTheme.objectColors;

        public GradientThemeKeyframe(float time,
            int colorSlot, float opacity, float hue, float saturation, float value,
            int secondColorSlot, float secondOpacity, float secondHue, float secondSaturation, float secondValue, EaseFunction ease)
        {
            Time = time;
            this.colorSlot = colorSlot;
            this.opacity = opacity;
            this.hue = hue;
            this.saturation = saturation;
            this.value = value;
            this.secondColorSlot = secondColorSlot;
            this.secondOpacity = secondOpacity;
            this.secondHue = secondHue;
            this.secondSaturation = secondSaturation;
            this.secondValue = secondValue;
            Ease = ease;
            Active = false;
        }

        public void Start() { }

        public void Stop() => Active = false;

        public void SetEase(EaseFunction ease) => Ease = ease;

        public KeyValuePair<Color, Color> Interpolate(IKeyframe<KeyValuePair<Color, Color>> other, float time)
        {
            var second = (GradientThemeKeyframe)other;
            var ease = second.Ease(time);
            var color = RTMath.Lerp(Theme[colorSlot], Theme[second.colorSlot], ease);
            var secondColor = RTMath.Lerp(Theme[secondColorSlot], Theme[second.secondColorSlot], ease);

            color =
                LSColors.fadeColor(
                    CoreHelper.ChangeColorHSV(color,
                        RTMath.Lerp(hue, second.hue, ease),
                        RTMath.Lerp(saturation, second.saturation, ease),
                        RTMath.Lerp(value, second.value, ease)),
                    RTMath.Lerp(opacity, second.opacity, ease));

            secondColor =
                LSColors.fadeColor(
                    CoreHelper.ChangeColorHSV(secondColor,
                        RTMath.Lerp(secondHue, second.secondHue, ease),
                        RTMath.Lerp(secondSaturation, second.secondSaturation, ease),
                        RTMath.Lerp(secondValue, second.secondValue, ease)),
                    RTMath.Lerp(secondOpacity, second.secondOpacity, ease));

            return new KeyValuePair<Color, Color>(color, secondColor);
        }
    }
}
