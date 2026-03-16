using UnityEngine;

namespace BetterLegacy.Core.Animation.Keyframe
{
    /// <summary>
    /// A keyframe that targets the player and animates a float value.
    /// </summary>
    public struct StaticRotationKeyframe : IKeyframe<Vector3>, IHomingKeyframe, IHomingFloatKeyframe
    {
        public StaticRotationKeyframe(float time, Vector3 value, EaseFunction ease, Sequence<Vector3> positionSequence, bool relative, HomingPriority priority, int playerIndex)
        {
            Time = time;
            Value = value;
            Ease = ease;
            Active = false;
            Target = Vector3.zero;
            PositionSequence = positionSequence;
            Angle = 0f;
            TotalValue = Vector3.zero;
            Relative = relative;
            Priority = priority;
            PlayerIndex = playerIndex;
        }

        #region Values

        public bool Active { get; set; }
        public float Time { get; set; }
        public EaseFunction Ease { get; set; }
        public Vector3 Value { get; set; }
        public Vector3 TotalValue { get; set; }
        public bool Relative { get; set; }
        public Sequence<Vector3> PositionSequence { get; set; }
        public Vector3 Target { get; set; }
        public HomingPriority Priority { get; set; }
        public int PlayerIndex { get; set; }
        public float Angle { get; set; }

        #endregion

        #region Functions

        public void Start(IKeyframe<Vector3> prev, Vector3 value, float time)
        {
            TotalValue = Relative ? prev is IHomingKeyframe ? prev.GetValue() : prev.TotalValue : Vector3.zero;
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

        public void SetValue(Vector3 value) => Value = value;

        public Vector3 GetValue() => new Vector3(0f, 0f, Angle) + Value + TotalValue;

        public Vector3 Interpolate(IKeyframe<Vector3> other, float time) => RTMath.Lerp(GetValue(), other.GetValue(), other.Ease(time));

        #endregion
    }
}
