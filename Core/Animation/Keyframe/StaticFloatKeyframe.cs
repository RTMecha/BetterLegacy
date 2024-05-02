using BetterLegacy.Core.Managers;
using System.Linq;

using UnityEngine;

namespace BetterLegacy.Core.Animation.Keyframe
{
    /// <summary>
    /// A keyframe that animates a float value.
    /// </summary>
    public struct StaticFloatKeyframe : IKeyframe<float>
    {
        public bool Active { get; set; }

        public float Time { get; set; }
        public EaseFunction Ease { get; set; }
        public float Value { get; set; }
        public IKeyframe<float> PreviousKeyframe { get; set; }

        public Sequence<Vector3> PositionSequence { get; set; }

        public Transform Player
        {
            get
            {
                if (PlayerManager.Players.Count > 0)
                {
                    var value = PositionSequence.Value;
                    var orderedList = PlayerManager.Players
                        .Where(x => x.Player && x.Player.transform.Find("Player"))
                        .OrderBy(x => Vector2.Distance(x.Player.transform.Find("Player").localPosition, value))
                        .ToList();
                    if (orderedList.Count > 0)
                    {
                        var player = orderedList[0];

                        if (player && player.Player)
                        {
                            return player.Player.transform.Find("Player");
                        }
                    }
                    return null;
                }

                return null;
            }
        }

        public Vector2 Target { get; set; }

        public float Angle { get; set; }

        public StaticFloatKeyframe(float time, float value, EaseFunction ease, IKeyframe<float> previousKeyframe, Sequence<Vector3> positionSequence)
        {
            Time = time;
            Value = value;
            Ease = ease;
            Active = false;
            Target = Vector3.zero;
            PreviousKeyframe = previousKeyframe;
            PositionSequence = positionSequence;
            Angle = 0f;
        }

        public void Start()
        {
            if (Player)
                Target = Player.transform.position;
        }

        public float Interpolate(IKeyframe<float> other, float time)
        {
            var value = other is FloatKeyframe vector3Keyframe ? vector3Keyframe.Value : other is DynamicFloatKeyframe dynamicVector3Keyframe ? dynamicVector3Keyframe.Value : other is StaticFloatKeyframe staticVector3Keyframe ? staticVector3Keyframe.Value : 0f;
            var ease = other is FloatKeyframe vector3Keyframe1 ? vector3Keyframe1.Ease(time) : other is DynamicFloatKeyframe dynamicVector3Keyframe1 ? dynamicVector3Keyframe1.Ease(time) : other is StaticFloatKeyframe staticVector3Keyframe1 ? staticVector3Keyframe1.Ease(time) : 0f;

            var prevtarget = PreviousKeyframe != null && PreviousKeyframe is StaticFloatKeyframe ? ((StaticFloatKeyframe)PreviousKeyframe).Angle : 0f;

            Angle = -RTMath.VectorAngle(PositionSequence.Value, Target);
            return RTMath.Lerp(prevtarget + Value, Angle + value, ease);
        }
    }
}
