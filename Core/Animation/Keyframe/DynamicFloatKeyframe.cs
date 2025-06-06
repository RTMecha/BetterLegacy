﻿using UnityEngine;

using BetterLegacy.Core.Helpers;

namespace BetterLegacy.Core.Animation.Keyframe
{
    public struct DynamicFloatKeyframe : IKeyframe<float>, IHomingKeyframe, IDynamicHomingKeyframe<float>, IHomingFloatKeyframe
    {
        public DynamicFloatKeyframe(float time, float value, EaseFunction ease, float delay, float min, float max, bool flee, Sequence<Vector3> positionSequence, bool relative)
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
            Angle360 = 0f;
            Angle = 0f;
            AngleDegrees = 0f;
            PlayerSide = Side.Undetermined;
            Target = Vector3.zero;
            Position = Vector3.zero;
            PositionSequence = positionSequence;
            TotalValue = 0f;
            Relative = relative;
        }

        #region Values

        public bool Active { get; set; }
        public float Time { get; set; }
        public EaseFunction Ease { get; set; }
        public float Value { get; set; }
        public float OriginalValue { get; set; }
        public float TotalValue { get; set; }
        public bool Relative { get; set; }
        public float Delay { get; set; }
        public float MinRange { get; set; }
        public float MaxRange { get; set; }
        public bool Flee { get; set; }
        public float Angle { get; set; }
        public Vector3 Target { get; set; }
        public Sequence<Vector3> PositionSequence { get; set; }

        /// <summary>
        /// 360 degree offset to rotate.
        /// </summary>
        public float Angle360 { get; set; }

        /// <summary>
        /// The current angle degrees.
        /// </summary>
        public float AngleDegrees { get; set; }

        /// <summary>
        /// The current position to target.
        /// </summary>
        public Vector3 Position { get; set; }

        /// <summary>
        /// Horizontal side the player is on relative to the object.
        /// </summary>
        public Side PlayerSide { get; set; }

        /// <summary>
        /// Horizontal side.
        /// </summary>
        public enum Side
        {
            /// <summary>
            /// The side is undetermined.
            /// </summary>
            Undetermined,
            /// <summary>
            /// The player is to the left of the object.
            /// </summary>
            Left,
            /// <summary>
            /// The player is to the right of the object.
            /// </summary>
            Right,
        }

        #endregion

        #region Methods

        public void Start(IKeyframe<float> prev, float value, float time)
        {
            Active = true;
            if (prev is not IDynamicHomingKeyframe<float>)
                Value = OriginalValue;
            var player = this.GetPlayer(time);
            Target = player?.localPosition ?? Vector3.zero;
            Angle360 = 0f;
            Angle = 0f;
            AngleDegrees = 0f;
            PlayerSide = Side.Undetermined;
            Position = PositionSequence.Interpolate(time);

            //var vector = player?.localPosition ?? Vector3.zero;
            //var angle = -RTMath.VectorAngle(Position, Flee ? vector - Position : vector);

            // Calculation for rotation looping so it doesn't flip around with a lower delay.

            //if (Target.x < PositionSequence.Value.x && vector.y < PositionSequence.Value.y && PlayerSide != Side.Left)
            //{
            //    PlayerSide = Side.Left;
            //}

            //if (Target.x > PositionSequence.Value.x && vector.y < PositionSequence.Value.y && PlayerSide != Side.Right)
            //{
            //    PlayerSide = Side.Right;
            //}
        }

        public void Stop() => Active = false;

        public Vector3 GetPosition() => PositionSequence.Value;

        public Vector3 GetPosition(float time) => PositionSequence.Interpolate(time);

        public void SetEase(EaseFunction ease) => Ease = ease;

        public void SetValue(float value) => Value = value;

        public float GetValue() => GetValue(0f);

        public float GetValue(float ease) => GetValue(null, ease);

        /// <summary>
        /// Gets the value of the dynamic homing keyframe. Interpolates only min & max range and delay.
        /// </summary>
        /// <param name="dynamicHomingKeyframe">Next dynamic homing keyframe. If is null, doesn't interpolate.</param>
        /// <param name="ease">Eased time scale.</param>
        /// <returns>Returns the dynamic homing value.</returns>
        public float GetValue(IDynamicHomingKeyframe<float> dynamicHomingKeyframe, float ease)
        {
            var player = this.GetPlayer();
            var vector = player?.localPosition ?? Vector3.zero;
            var angle = -RTMath.VectorAngle(PositionSequence.Value, Flee ? vector - PositionSequence.Value : vector);

            // Calculation for rotation looping so it doesn't flip around with a lower delay.
            // This code was used in researching how to get the homing rotation to not flip. Sadly, couldn't figure it out fully.

            //if (Target.x < PositionSequence.Value.x && vector.y < PositionSequence.Value.y && PlayerSide != Side.Left)
            //{
            //    PlayerSide = Side.Left;
            //}

            //if (Target.x > PositionSequence.Value.x && vector.y < PositionSequence.Value.y && PlayerSide != Side.Right)
            //{
            //    PlayerSide = Side.Right;
            //}

            // PREVIOUS X LESSER THAN CURRENT X AND CURRENT X LESSER THAN POSITION X AND ANGLE LESSER THAN CALCULATED ANGLE + ANGLE 360
            // PREVIOUS X = 10
            // CURRENT X = 9
            // PREVIOUS X LESSER THAN CURRENT X = TRUE, SO MOVING LEFT

            // PREVIOUS X = 1
            // CURRENT X = -1
            // POSITION X = 0
            // CURRENT X LESSER THAN POSITION X

            // PREVIOUS ANGLE = -350
            // CURRENT ANGLE = -10
            //if (Target.x < vector.x && Target.x < PositionSequence.Value.x && AngleDegrees < angle && vector.y > PositionSequence.Value.y && PlayerSide != Side.Right)
            //{
            //    PlayerSide = Side.Right;
            //    Angle360 -= 360f;
            //}

            // PREVIOUS X GREATER THAN CURRENT X AND CURRENT X LESSER THAN POSITION X AND ANGLE LESSER THAN CALCULATED ANGLE + ANGLE 360
            // PREVIOUS X = 10
            // CURRENT X = 9
            // PREVIOUS X GREATER THAN CURRENT X = TRUE, SO MOVING RIGHT

            // PREVIOUS X = 1
            // CURRENT X = -1
            // POSITION X = 0
            // CURRENT X GREATER THAN POSITION X

            // PREVIOUS ANGLE = -10
            // CURRENT ANGLE = -350

            // CURRENT Y = 4
            // POSITON Y = 5
            // CURRENT Y - POSITION Y = 
            //if (Target.x > vector.x && Target.x > PositionSequence.Value.x && AngleDegrees > angle && vector.y > PositionSequence.Value.y && PlayerSide != Side.Left)
            //{
            //    PlayerSide = Side.Left;
            //    Angle360 += 360f;
            //}

            if ((Target.x < vector.x || Position.x > PositionSequence.Value.x) && vector.x > PositionSequence.Value.x && PlayerSide != Side.Right)
            {
                PlayerSide = Side.Right;
                if (vector.y > PositionSequence.Value.y && AngleDegrees < angle)
                    Angle360 -= 360f;
            }

            if ((Target.x > vector.x || Position.x < PositionSequence.Value.x) && vector.x < PositionSequence.Value.x && PlayerSide != Side.Left)
            {
                PlayerSide = Side.Left;
                if (vector.y > PositionSequence.Value.y && AngleDegrees > angle)
                    Angle360 += 360f;
            }

            AngleDegrees = angle;
            Angle = angle + Angle360;
            Target = player?.localPosition ?? Vector3.zero;
            Position = PositionSequence.Value;

            var minRange = MinRange;
            var maxRange = MaxRange;
            var delay = CalculateDelay();

            if (dynamicHomingKeyframe != null)
            {
                minRange = RTMath.Lerp(minRange, dynamicHomingKeyframe.MinRange, ease);
                maxRange = RTMath.Lerp(maxRange, dynamicHomingKeyframe.MaxRange, ease);
                delay = RTMath.Lerp(delay, dynamicHomingKeyframe.CalculateDelay(), ease);
            }

            if (minRange == 0f && maxRange == 0f || Vector2.Distance(vector, PositionSequence.Value) > minRange && Vector2.Distance(vector, PositionSequence.Value) < maxRange)
                Value += (Angle - Value) * delay;

            return Value;
        }

        public float CalculateDelay() => 1f - Mathf.Pow(Delay, UnityEngine.Time.deltaTime * CoreHelper.ForwardPitch);

        public float Interpolate(IKeyframe<float> other, float time)
        {
            var ease = other.Ease(time);
            if (other is IDynamicHomingKeyframe<float> dynamicHomingKeyframe)
            {
                var value = GetValue(dynamicHomingKeyframe, ease);
                // set the value to the other dynamic homing keyframe so it doesn't snap to 0 when the keyframe starts interpolating.
                other.SetValue(value);
                return value;
            }

            return RTMath.Lerp(GetValue(ease), other.GetValue(), ease);
        }

        #endregion
    }
}
