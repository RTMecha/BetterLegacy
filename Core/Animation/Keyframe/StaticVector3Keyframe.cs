using BetterLegacy.Core.Managers;
using System.Linq;

using UnityEngine;

namespace BetterLegacy.Core.Animation.Keyframe
{
    public struct StaticVector3Keyframe : IKeyframe<Vector3>
    {
        public bool Active { get; set; }

        public float Time { get; set; }
        public EaseFunction Ease { get; set; }
        public Vector3 Value { get; set; }
        public IKeyframe<Vector3> PreviousKeyframe { get; set; }
        public AxisMode Axis { get; set; }

        public Transform Player
        {
            get
            {
                var player = PlayerManager.GetClosestPlayer(Value);
                if (player && player.Player)
                    return player.Player.transform.Find("Player");
                return null;
            }
        }

        public Vector2 Target { get; set; }

        public StaticVector3Keyframe(float time, Vector3 value, EaseFunction ease, IKeyframe<Vector3> previousKeyframe, AxisMode axisMode)
        {
            Time = time;
            Value = value;
            Ease = ease;
            Active = false;
            Target = value;
            PreviousKeyframe = previousKeyframe;
            Axis = axisMode;
        }

        public void Start()
        {
            if (Player)
            {
                var pos = Player.transform.position;
                Target = Axis == AxisMode.Both ? pos : Axis == AxisMode.XOnly ? new Vector3(pos.x, 0f) : Axis == AxisMode.YOnly ? new Vector3(0f, pos.y) : Vector3.zero;
            }
        }

        public void Stop()
        {
            Active = false;
        }

        public void SetEase(EaseFunction ease)
        {
            Ease = ease;
        }

        public Vector3 Interpolate(IKeyframe<Vector3> other, float time)
        {
            //var secondValue = other is StaticVector3Keyframe keyframe ? keyframe.Value : other is DynamicVector3Keyframe dynamicKeyframe ? dynamicKeyframe.Value : ((Vector3Keyframe)other).Value;
            //var secondEase = other is StaticVector3Keyframe keyframe1 ? keyframe1.Ease(time) : other is DynamicVector3Keyframe dynamickeyframe1 ? dynamickeyframe1.Ease(time) : ((Vector3Keyframe)other).Ease(time);

            var value = other is Vector3Keyframe vector3Keyframe ? vector3Keyframe.Value : other is DynamicVector3Keyframe dynamicVector3Keyframe ? dynamicVector3Keyframe.Value : other is StaticVector3Keyframe staticVector3Keyframe ? staticVector3Keyframe.Value : Vector3.zero;
            var ease = other is Vector3Keyframe vector3Keyframe1 ? vector3Keyframe1.Ease(time) : other is DynamicVector3Keyframe dynamicVector3Keyframe1 ? dynamicVector3Keyframe1.Ease(time) : other is StaticVector3Keyframe staticVector3Keyframe1 ? staticVector3Keyframe1.Ease(time) : 0f;

            var prevtarget = PreviousKeyframe != null && PreviousKeyframe is StaticVector3Keyframe ? ((StaticVector3Keyframe)PreviousKeyframe).Target :
                PreviousKeyframe != null && PreviousKeyframe is DynamicVector3Keyframe ? ((DynamicVector3Keyframe)PreviousKeyframe).Value : Vector2.zero;

            return RTMath.Lerp(new Vector3(prevtarget.x, prevtarget.y, 0f) + Value, new Vector3(Target.x, Target.y, 0f) + value, ease);
        }
    }
}
