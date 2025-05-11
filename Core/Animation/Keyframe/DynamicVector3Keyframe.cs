using UnityEngine;

using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;

namespace BetterLegacy.Core.Animation.Keyframe
{
    public struct DynamicVector3Keyframe : IKeyframe<Vector3>, IHomingKeyframe
    {
        public bool Active { get; set; }

        public float Time { get; set; }
        public EaseFunction Ease { get; set; }
        public Vector3 Value { get; set; }
        public Vector3 OriginalValue { get; set; }
        public AxisMode Axis { get; set; }
        public Vector3 TotalValue { get; set; }
        public bool Relative { get; set; }

        public float Delay { get; set; }
        public float MinRange { get; set; }
        public float MaxRange { get; set; }
        public bool Flee { get; set; }

        public Vector3 Target { get; set; }

        public DynamicVector3Keyframe(float time, Vector3 value, EaseFunction ease, float delay, float min, float max, bool flee, AxisMode axisMode, bool relative)
        {
            Time = time;
            Value = value;
            OriginalValue = value;
            Ease = ease;
            Active = false;
            Delay = delay;
            MinRange = min;
            MaxRange = max;
            Flee = flee;
            Target = Vector3.zero;
            Axis = axisMode;
            TotalValue = Vector3.zero;
            Relative = relative;
        }

        public void Start(IKeyframe<Vector3> prev, Vector3 value, float time)
        {
            Active = true;
            Value = OriginalValue;
        }

        public void Stop() => Active = false;

        public Vector3 GetPosition() => Value;

        public Vector3 GetPosition(float time) => Value;

        public void SetEase(EaseFunction ease) => Ease = ease;

        public void SetValue(Vector3 value) => Value = value;

        public Vector3 GetValue()
        {
            var player = this.GetPlayer();
            var vector = player?.localPosition ?? Vector3.zero;
            var delay = UnityEngine.Time.deltaTime * CoreHelper.ForwardPitch * Delay;

            if ((MinRange == 0f && MaxRange == 0f || MinRange > MaxRange || Vector2.Distance(vector, Value) > MinRange && Vector2.Distance(vector, Value) < MaxRange) && Axis == AxisMode.Both)
                Value += Flee ? -(vector - Value) * delay : (vector - Value) * delay;

            if ((MinRange == 0f && MaxRange == 0f || MinRange > MaxRange || RTMath.Distance(vector.x, Value.x) > MinRange && RTMath.Distance(vector.x, Value.x) < MaxRange) && Axis == AxisMode.XOnly)
            {
                var x = Value;
                x.x += Flee ? -(vector.x - Value.x) * delay : (vector.x - Value.x) * delay;
                Value = x;
            }

            if ((MinRange == 0f && MaxRange == 0f || MinRange > MaxRange || RTMath.Distance(vector.y, Value.y) > MinRange && RTMath.Distance(vector.y, Value.y) < MaxRange) && Axis == AxisMode.YOnly)
            {
                var x = Value;
                x.y += Flee ? -(vector.y - Value.y) * delay : (vector.y - Value.y) * delay;
                Value = x;
            }

            return Value;
        }

        public Vector3 Interpolate(IKeyframe<Vector3> other, float time) => RTMath.Lerp(GetValue(), other.GetValue(), other.Ease(time));
    }
}
