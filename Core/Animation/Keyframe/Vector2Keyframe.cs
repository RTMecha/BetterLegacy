using UnityEngine;

namespace BetterLegacy.Core.Animation.Keyframe
{
    /// <summary>
    /// A keyframe that animates a Vector2 value.
    /// </summary>
    public struct Vector2Keyframe : IKeyframe<Vector2>
    {
        public Vector2Keyframe(float time, Vector2 value, EaseFunction ease, bool relative = false)
        {
            Time = time;
            Value = value;
            Ease = ease;
            Active = false;
            TotalValue = Vector2.zero;
            Relative = relative;
        }

        #region Values

        public bool Active { get; set; }
        public float Time { get; set; }
        public EaseFunction Ease { get; set; }
        public Vector2 Value { get; set; }
        public Vector2 TotalValue { get; set; }
        public bool Relative { get; set; }

        #endregion

        #region Methods

        public void Start(IKeyframe<Vector2> prev, Vector2 value, float time) => Active = true;

        public void Stop() => Active = false;

        public void SetEase(EaseFunction ease) => Ease = ease;

        public void SetValue(Vector2 value) => Value = value;

        public Vector2 GetValue() => Value;

        public Vector2 Interpolate(IKeyframe<Vector2> other, float time) => RTMath.Lerp(GetValue(), other.GetValue(), other.Ease(time));

        #endregion
    }
}

