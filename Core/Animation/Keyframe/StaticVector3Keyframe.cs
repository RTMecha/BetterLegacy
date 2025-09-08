using UnityEngine;

namespace BetterLegacy.Core.Animation.Keyframe
{
    /// <summary>
    /// A keyframe that targets the player and animates a Vector3 value.
    /// </summary>
    public struct StaticVector3Keyframe : IKeyframe<Vector3>, IHomingKeyframe, IHomingVector3Keyframe
    {
        public StaticVector3Keyframe(float time, Vector3 value, EaseFunction ease, AxisMode axisMode, bool relative, HomingPriority priority, int playerIndex)
        {
            Time = time;
            Value = value;
            Ease = ease;
            Active = false;
            Target = value;
            Axis = axisMode;
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
        public AxisMode Axis { get; set; }
        public bool Relative { get; set; }
        public Vector3 Target { get; set; }
        public HomingPriority Priority { get; set; }
        public int PlayerIndex { get; set; }

        #endregion

        #region Methods

        public void Start(IKeyframe<Vector3> prev, Vector3 value, float time)
        {
            TotalValue = Relative ? prev is IHomingKeyframe ? prev.GetValue() : prev.TotalValue : Vector3.zero;
            Active = true;
            var player = this.GetPlayer();
            if (player)
                Target = Axis switch
                {
                    AxisMode.Both => player.transform.position,
                    AxisMode.XOnly => new Vector3(player.transform.position.x, 0f),
                    AxisMode.YOnly => new Vector3(0f, player.transform.position.y),
                    _ => Vector3.zero,
                };
        }

        public void Stop() => Active = false;

        public Vector3 GetPosition() => GetValue();

        public Vector3 GetPosition(float time) => GetValue();

        public void SetEase(EaseFunction ease) => Ease = ease;

        public void SetValue(Vector3 value) => Value = value;

        public Vector3 GetValue() => Target + Value + TotalValue;

        public Vector3 Interpolate(IKeyframe<Vector3> other, float time) => RTMath.Lerp(GetValue(), other.GetValue(), other.Ease(time));

        #endregion
    }
}
