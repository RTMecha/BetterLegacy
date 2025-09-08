using UnityEngine;

using BetterLegacy.Core.Helpers;

namespace BetterLegacy.Core.Animation.Keyframe
{
    public struct DynamicVector3Keyframe : IKeyframe<Vector3>, IHomingKeyframe, IDynamicHomingKeyframe<Vector3>, IHomingVector3Keyframe
    {
        public DynamicVector3Keyframe(float time, Vector3 value, EaseFunction ease, float delay, float min, float max, bool flee, AxisMode axisMode, bool relative, HomingPriority priority, int playerIndex)
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
            Priority = priority;
            PlayerIndex = playerIndex;
        }

        #region Values

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
        public HomingPriority Priority { get; set; }
        public int PlayerIndex { get; set; }

        #endregion

        #region Methods

        public void Start(IKeyframe<Vector3> prev, Vector3 value, float time)
        {
            Active = true;
            if (prev is not IDynamicHomingKeyframe<Vector3>)
                Value = OriginalValue;
        }

        public void Stop() => Active = false;

        public Vector3 GetPosition() => Value;

        public Vector3 GetPosition(float time) => Value;

        public void SetEase(EaseFunction ease) => Ease = ease;

        public void SetValue(Vector3 value) => Value = value;

        public Vector3 GetValue() => GetValue(0f);

        public Vector3 GetValue(float ease) => GetValue(null, ease);

        /// <summary>
        /// Gets the value of the dynamic homing keyframe. Interpolates only min & max range and delay.
        /// </summary>
        /// <param name="dynamicHomingKeyframe">Next dynamic homing keyframe. If is null, doesn't interpolate.</param>
        /// <param name="ease">Eased time scale.</param>
        /// <returns>Returns the dynamic homing value.</returns>
        public Vector3 GetValue(IDynamicHomingKeyframe<Vector3> dynamicHomingKeyframe, float ease)
        {
            var player = this.GetPlayer();
            var vector = player?.localPosition ?? Vector3.zero;

            var minRange = MinRange;
            var maxRange = MaxRange;
            var delay = CalculateDelay();

            if (dynamicHomingKeyframe != null)
            {
                minRange = RTMath.Lerp(minRange, dynamicHomingKeyframe.MinRange, ease);
                maxRange = RTMath.Lerp(maxRange, dynamicHomingKeyframe.MaxRange, ease);
                delay = RTMath.Lerp(delay, dynamicHomingKeyframe.CalculateDelay(), ease);
            }

            if ((minRange == 0f && maxRange == 0f || minRange > maxRange || Vector2.Distance(vector, Value) > minRange && Vector2.Distance(vector, Value) < maxRange) && Axis == AxisMode.Both)
                Value += Flee ? -(vector - Value) * delay : (vector - Value) * delay;

            if ((minRange == 0f && maxRange == 0f || minRange > maxRange || RTMath.Distance(vector.x, Value.x) > minRange && RTMath.Distance(vector.x, Value.x) < maxRange) && Axis == AxisMode.XOnly)
            {
                var x = Value;
                x.x += Flee ? -(vector.x - Value.x) * delay : (vector.x - Value.x) * delay;
                Value = x;
            }

            if ((minRange == 0f && maxRange == 0f || minRange > maxRange || RTMath.Distance(vector.y, Value.y) > minRange && RTMath.Distance(vector.y, Value.y) < maxRange) && Axis == AxisMode.YOnly)
            {
                var x = Value;
                x.y += Flee ? -(vector.y - Value.y) * delay : (vector.y - Value.y) * delay;
                Value = x;
            }

            return Value;
        }

        public float CalculateDelay() => 1f - Mathf.Pow(Delay, UnityEngine.Time.deltaTime * CoreHelper.ForwardPitch);

        public Vector3 Interpolate(IKeyframe<Vector3> other, float time)
        {
            var ease = other.Ease(time);
            if (other is IDynamicHomingKeyframe<Vector3> dynamicHomingKeyframe)
            {
                var value = GetValue(dynamicHomingKeyframe, ease);
                // set the value to the other dynamic homing keyframe so it doesn't snap to 0 when the keyframe starts interpolating.
                other.SetValue(value);
                return value;
            }

            return RTMath.Lerp(GetValue(ease), other.GetValue(), other.Ease(time));
        }

        #endregion
    }
}
