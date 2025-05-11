using UnityEngine;

using BetterLegacy.Core.Managers;

namespace BetterLegacy.Core.Animation.Keyframe
{
    public struct StaticVector3Keyframe : IKeyframe<Vector3>, IHomingKeyframe
    {
        public bool Active { get; set; }

        public float Time { get; set; }
        public EaseFunction Ease { get; set; }
        public Vector3 Value { get; set; }
        public AxisMode Axis { get; set; }

        public Vector3 Target { get; set; }

        public StaticVector3Keyframe(float time, Vector3 value, EaseFunction ease, AxisMode axisMode)
        {
            Time = time;
            Value = value;
            Ease = ease;
            Active = false;
            Target = value;
            Axis = axisMode;
        }

        public void Start(float time)
        {
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

        public Vector3 GetValue() => Target + Value;

        public Vector3 Interpolate(IKeyframe<Vector3> other, float time) => RTMath.Lerp(GetValue(), other.GetValue(), other.Ease(time));
    }
}
