using UnityEngine;

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
        public Color TotalValue { get; set; }
        public bool Relative { get; set; }

        public ColorKeyframe(float time, Color value, EaseFunction ease)
        {
            Time = time;
            Value = value;
            Ease = ease;
            Active = false;
            TotalValue = Color.white;
            Relative = false;
        }

        public void Start(IKeyframe<Color> prev, Color value, float time) => Active = true;

        public void Stop() => Active = false;

        public void SetEase(EaseFunction ease) => Ease = ease;

        public void SetValue(Color value) => Value = value;

        public Color GetValue() => Value;

        public Color Interpolate(IKeyframe<Color> other, float time) => RTMath.Lerp(GetValue(), other.GetValue(), other.Ease(time));
    }
}
