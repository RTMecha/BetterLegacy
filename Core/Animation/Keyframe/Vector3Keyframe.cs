using UnityEngine;

namespace BetterLegacy.Core.Animation.Keyframe
{
    /// <summary>
    /// A keyframe that animates a Vector3 value.
    /// </summary>
    public struct Vector3Keyframe : IKeyframe<Vector3>
    {
        public Vector3Keyframe(float time, Vector3 value, EaseFunction ease, bool relative = false)
        {
            Time = time;
            Value = value;
            Ease = ease;
            Active = false;
            TotalValue = Vector3.zero;
            Relative = relative;
        }

        #region Values

        public bool Active { get; set; }
        public float Time { get; set; }
        public EaseFunction Ease { get; set; }
        public Vector3 Value { get; set; }
        public Vector3 TotalValue { get; set; }
        public bool Relative { get; set; }

        #endregion

        #region Methods

        public void Start(IKeyframe<Vector3> prev, Vector3 value, float time)
        {
            // get the relative value of the previous homing keyframe
            TotalValue = Relative ? prev is IHomingKeyframe ? prev.GetValue() : prev.TotalValue : Vector3.zero;
            Active = true;
        }

        public void Stop() => Active = false;

        public void SetEase(EaseFunction ease) => Ease = ease;

        public void SetValue(Vector3 value) => Value = value;

        public Vector3 GetValue() => Value + TotalValue;

        public Vector3 Interpolate(IKeyframe<Vector3> other, float time) => RTMath.Lerp(GetValue(), other.GetValue(), other.Ease(time));

        #endregion
    }
}

