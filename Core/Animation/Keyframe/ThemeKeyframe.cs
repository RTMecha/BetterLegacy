﻿using System.Collections.Generic;

using UnityEngine;

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

        public Color TotalValue { get; set; }
        public bool Relative { get; set; }

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
            TotalValue = Color.white;
            Relative = false;
        }

        public void Start(IKeyframe<Color> prev, Color value, float time) => Active = true;

        public void Stop() => Active = false;

        public void SetEase(EaseFunction ease) => Ease = ease;

        public void SetValue(Color value) { }

        public Color GetValue() => RTColors.FadeColor(
                    RTColors.ChangeColorHSV(Theme[colorSlot],
                        hue,
                        saturation,
                        brightness),
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
                                RTMath.Lerp(brightness, themeKeyframe.brightness, ease)),
                            -(RTMath.Lerp(opacity, themeKeyframe.opacity, ease) - 1f));
            }

            // if other is not a ThemeKeyframe (e.g. ColorKeyframe), just interpolate the color values.
            return RTMath.Lerp(GetValue(), other.GetValue(), other.Ease(time));
        }
    }
}
