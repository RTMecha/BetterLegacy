using UnityEngine;

using BetterLegacy.Core.Managers;

namespace BetterLegacy.Core.Animation.Keyframe
{
    /// <summary>
    /// A keyframe that animates a float value.
    /// </summary>
    public struct StaticFloatKeyframe : IKeyframe<float>, IHomingKeyframe, IHomingFloatKeyframe
    {
        public bool Active { get; set; }

        public float Time { get; set; }
        public EaseFunction Ease { get; set; }
        public float Value { get; set; }

        public Sequence<Vector3> PositionSequence { get; set; }

        public Vector3 Target { get; set; }

        public float Angle { get; set; }

        public StaticFloatKeyframe(float time, float value, EaseFunction ease, Sequence<Vector3> positionSequence)
        {
            Time = time;
            Value = value;
            Ease = ease;
            Active = false;
            Target = Vector3.zero;
            PositionSequence = positionSequence;
            Angle = 0f;
        }

        public void Start(float time)
        {
            Active = true;
            var player = this.GetPlayer(time);
            if (player)
                Target = player.transform.position;

            Angle = -RTMath.VectorAngle(PositionSequence.Interpolate(time), Target);
        }

        public void Stop() => Active = false;

        public Vector3 GetPosition() => PositionSequence.Value;

        public Vector3 GetPosition(float time) => PositionSequence.Interpolate(time);

        public void SetEase(EaseFunction ease) => Ease = ease;

        public void SetValue(float value) => Value = value;

        public float GetValue() => Angle + Value;

        public float Interpolate(IKeyframe<float> other, float time) => RTMath.Lerp(GetValue(), other.GetValue(), other.Ease(time));
    }
}
