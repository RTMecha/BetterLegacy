using UnityEngine;
using BetterLegacy.Core;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Helpers;

namespace BetterLegacy.Core.Animation.Keyframe
{
    /// <summary>
    /// A keyframe that animates a color value.
    /// </summary>
    public struct ColorKeyframe : IKeyframe<Color>
    {
        public bool Active { get; set; }

        public float Time { get; set; }
        public EaseFunction Ease { get; set; }
        public Color Value { get; set; }

        public ColorKeyframe(float time, Color value, EaseFunction ease)
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
            var second = (ColorKeyframe)other;
            return RTMath.Lerp(Value, second.Value, second.Ease(time));
        }
    }
}
