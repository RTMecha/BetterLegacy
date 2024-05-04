using BetterLegacy.Core.Helpers;
using System.Collections.Generic;
using UnityEngine;

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
        public int Value { get; set; }

        List<Color> Theme => CoreHelper.CurrentBeatmapTheme.objectColors;

        public ThemeKeyframe(float time, int value, EaseFunction ease)
        {
            Time = time;
            Value = value;
            Ease = ease;
            Active = false;
        }

        public void Start()
        {

        }

        public Color Interpolate(IKeyframe<Color> other, float time)
        {
            var second = (ThemeKeyframe)other;
            return RTMath.Lerp(Theme[Value], Theme[second.Value], second.Ease(time));
        }
    }
}
