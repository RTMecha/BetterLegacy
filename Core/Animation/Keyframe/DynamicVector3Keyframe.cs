using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using System.Linq;

using UnityEngine;

namespace BetterLegacy.Core.Animation.Keyframe
{
    public struct DynamicVector3Keyframe : IKeyframe<Vector3>
    {
        public bool Active { get; set; }

        public float Time { get; set; }
        public EaseFunction Ease { get; set; }
        public Vector3 Value { get; set; }
        public Vector3 OriginalValue { get; set; }
        public AxisMode Axis { get; set; }

        public float Delay { get; set; }
        public float MinRange { get; set; }
        public float MaxRange { get; set; }
        public bool Flee { get; set; }

        public Transform Player
        {
            get
            {
                var player = PlayerManager.GetClosestPlayer(Value);
                if (player.Player)
                    return player.Player.transform.Find("Player");
                return null;
            }
        }

        public DynamicVector3Keyframe(float time, Vector3 value, EaseFunction ease, float delay, float min, float max, bool flee, AxisMode axisMode)
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
            Axis = axisMode;
        }

        public void Start()
        {
            Value = OriginalValue;
        }

        public Vector3 Interpolate(IKeyframe<Vector3> other, float time)
        {
            //var secondValue = other is DynamicVector3Keyframe keyframe ? keyframe.Value : ((Vector3Keyframe)other).Value;
            //var secondEase = other is DynamicVector3Keyframe keyframe1 ? keyframe1.Ease(time) : ((Vector3Keyframe)other).Ease(time);

            var value = other is Vector3Keyframe vector3Keyframe ? vector3Keyframe.Value : other is DynamicVector3Keyframe dynamicVector3Keyframe ? dynamicVector3Keyframe.Value : other is StaticVector3Keyframe staticVector3Keyframe ? staticVector3Keyframe.Value : Vector3.zero;
            var ease = other is Vector3Keyframe vector3Keyframe1 ? vector3Keyframe1.Ease(time) : other is DynamicVector3Keyframe dynamicVector3Keyframe1 ? dynamicVector3Keyframe1.Ease(time) : other is StaticVector3Keyframe staticVector3Keyframe1 ? staticVector3Keyframe1.Ease(time) : 0f;

            var delayOther = other is DynamicVector3Keyframe keyframe2 ? keyframe2.Delay : -1f;

            //return RTMath.Lerp(Value, new Vector3(Player?.localPosition.x ?? 0f, Player?.localPosition.y ?? 0f, 0f) + value, ease);

            var vector = Player?.localPosition ?? Vector3.zero;

            float pitch = CoreHelper.ForwardPitch;

            float p = UnityEngine.Time.deltaTime * pitch;

            float po = 1f - Mathf.Pow(1f - Mathf.Clamp(delayOther < 0f ? Delay : RTMath.Lerp(Delay, delayOther, ease), 0.001f, 1f), p);
            if ((MinRange == 0f && MaxRange == 0f || MinRange > MaxRange || Vector2.Distance(vector, Value) > MinRange && Vector2.Distance(vector, Value) < MaxRange) && Axis == AxisMode.Both)
                Value += Flee ? -(vector - Value) * po : (vector - Value) * po;

            if ((MinRange == 0f && MaxRange == 0f || MinRange > MaxRange || Vector2.Distance(vector.X(), Value.X()) > MinRange && Vector2.Distance(vector.X(), Value.X()) < MaxRange) && Axis == AxisMode.XOnly)
            {
                var x = Value;
                x.x += Flee ? -(vector.x - Value.x) * po : (vector.x - Value.x) * po;
                Value = x;
            }

            if ((MinRange == 0f && MaxRange == 0f || MinRange > MaxRange || Vector2.Distance(vector.Y(), Value.Y()) > MinRange && Vector2.Distance(vector.Y(), Value.Y()) < MaxRange) && Axis == AxisMode.YOnly)
            {
                var x = Value;
                x.y += Flee ? -(vector.y - Value.y) * po : (vector.y - Value.y) * po;
                Value = x;
            }

            //return RTMath.Lerp(Value + OriginalValue, value, ease);
            return Value;
        }
    }
}
