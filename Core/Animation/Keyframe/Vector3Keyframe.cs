using UnityEngine;

namespace BetterLegacy.Core.Animation.Keyframe
{
    /// <summary>
    /// A keyframe that animates a Vector3 value.
    /// </summary>
    public struct Vector3Keyframe : IKeyframe<Vector3>
    {
        public bool Active { get; set; }

        public float Time { get; set; }
        public EaseFunction Ease { get; set; }
        public Vector3 Value { get; set; }

        public Vector3Keyframe(float time, Vector3 value, EaseFunction ease)
        {
            Time = time;
            Value = value;
            Ease = ease;
            Active = false;
        }

        public void Start(float time) => Active = true;

        public void Stop() => Active = false;

        public void SetEase(EaseFunction ease) => Ease = ease;

        public void SetValue(Vector3 value) => Value = value;

        public Vector3 GetValue() => Value;

        public Vector3 Interpolate(IKeyframe<Vector3> other, float time) => RTMath.Lerp(GetValue(), other.GetValue(), other.Ease(time));
    }
}

