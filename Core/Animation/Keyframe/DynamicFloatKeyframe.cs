using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using System.Linq;

using UnityEngine;

namespace BetterLegacy.Core.Animation.Keyframe
{
    public struct DynamicFloatKeyframe : IKeyframe<float>
    {
        public bool Active { get; set; }

        public float Time { get; set; }
        public EaseFunction Ease { get; set; }
        public float Value { get; set; }
        public float OriginalValue { get; set; }

        public float Delay { get; set; }
        public float MinRange { get; set; }
        public float MaxRange { get; set; }
        public bool Flee { get; set; }

        public float Angle { get; set; }
        public float Angle360 { get; set; }
        public float AngleDegrees { get; set; }

        public Vector3 Target { get; set; }

        public Sequence<Vector3> PositionSequence { get; set; }
        public Vector3 Position { get; set; }

        public Transform Player
        {
            get
            {
                var player = PlayerManager.GetClosestPlayer(PositionSequence.Value);
                if (player.Player)
                    return player.Player.transform.Find("Player");
                return null;
            }
        }

        public DynamicFloatKeyframe(float time, float value, EaseFunction ease, float delay, float min, float max, bool flee, Sequence<Vector3> positionSequence)
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
        }

        public void Start()
        {
            Value = OriginalValue;
            Target = Player?.localPosition ?? Vector3.zero;
            Angle360 = 0f;
            Angle = 0f;
            AngleDegrees = 0f;
            PlayerSide = Side.Undetermined;
            Position = PositionSequence.Value;

            var vector = Player?.localPosition ?? Vector3.zero;
            var angle = -RTMath.VectorAngle(PositionSequence.Value, Flee ? vector - PositionSequence.Value : vector);

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

        public void Stop()
        {
            Active = false;
        }

        public Side PlayerSide { get; set; }

        public enum Side
        {
            Undetermined,
            Left,
            Right
        }

        public float Interpolate(IKeyframe<float> other, float time)
        {
            var secondValue = other is DynamicFloatKeyframe keyframe ? keyframe.Value : other is StaticFloatKeyframe staticKeyframe ? staticKeyframe.Value : ((FloatKeyframe)other).Value;
            var secondEase = other is DynamicFloatKeyframe keyframe1 ? keyframe1.Ease(time) : other is StaticFloatKeyframe staticKeyframe1 ? staticKeyframe1.Ease(time) : ((FloatKeyframe)other).Ease(time);
            var delayOther = other is DynamicFloatKeyframe keyframe2 ? keyframe2.Delay : -1f;

            var vector = Player?.localPosition ?? Vector3.zero;
            var angle = -RTMath.VectorAngle(PositionSequence.Value, Flee ? vector - PositionSequence.Value : vector);

            // Calculation for rotation looping so it doesn't flip around with a lower delay.

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
            Target = Player?.localPosition ?? Vector3.zero;
            Position = PositionSequence.Value;

            float pitch = CoreHelper.ForwardPitch;

            float p = UnityEngine.Time.deltaTime * pitch;

            float po = 1f - Mathf.Pow(1f - Mathf.Clamp(delayOther < 0f ? Delay : RTMath.Lerp(Delay, delayOther, secondEase), 0.001f, 1f), p);

            if (MinRange == 0f && MaxRange == 0f || Vector2.Distance(vector, PositionSequence.Value) > MinRange && Vector2.Distance(vector, PositionSequence.Value) < MaxRange)
                Value += (Angle - Value) * po;

            //return RTMath.Lerp(Value + OriginalValue, secondValue, secondEase);
            return Value;
        }
    }
}
