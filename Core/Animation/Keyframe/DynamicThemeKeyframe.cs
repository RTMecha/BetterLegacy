using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BetterLegacy.Core.Animation.Keyframe
{
    /// <summary>
    /// A keyframe that animates a (theme) color value.
    /// </summary>
    public struct DynamicThemeKeyframe : IKeyframe<Color>
    {
        public bool Active { get; set; }

        public float Time { get; set; }
        public EaseFunction Ease { get; set; }
        public int Value { get; set; }

        public int Home { get; set; }

        public float Delay { get; set; }
        public float MinRange { get; set; }
        public float MaxRange { get; set; }
        public bool Flee { get; set; }

        List<Color> Theme => CoreHelper.CurrentBeatmapTheme.objectColors;

        public Sequence<Vector3> PositionSequence { get; set; }

        public Color Current { get; set; }

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

        public DynamicThemeKeyframe(float time, int value, EaseFunction ease, float delay, float min, float max, bool flee, int home, Sequence<Vector3> positionSequence)
        {
            Time = time;
            Value = value;
            Ease = ease;
            Active = false;
            Delay = delay;
            MinRange = min;
            MaxRange = max;
            Flee = flee;
            Home = home;
            PositionSequence = positionSequence;

            Current = CoreHelper.CurrentBeatmapTheme.objectColors[value];
        }

        public void Start()
        {

        }

        public void Stop()
        {
            Active = false;
        }

        public Color Interpolate(IKeyframe<Color> other, float time)
        {
            var value = other is ThemeKeyframe vector3Keyframe ? vector3Keyframe.Value : other is DynamicThemeKeyframe dynamicVector3Keyframe ? dynamicVector3Keyframe.Value : 0;
            var ease = other is ThemeKeyframe vector3Keyframe1 ? vector3Keyframe1.Ease(time) : other is DynamicThemeKeyframe dynamicVector3Keyframe1 ? dynamicVector3Keyframe1.Ease(time) : 0f;
            var delayOther = other is DynamicThemeKeyframe keyframe2 ? keyframe2.Delay : -1f;

            var distance = Vector2.Distance(Player.position, PositionSequence.Value);

            float max = MaxRange < 0.01f ? 10f : MaxRange;
            float t = (-(distance + MinRange) + max) / max;

            float pitch = CoreHelper.ForwardPitch;

            float p = UnityEngine.Time.deltaTime * pitch;

            float po = 1f - Mathf.Pow(1f - Mathf.Clamp(delayOther < 0f ? Delay : RTMath.Lerp(Delay, delayOther, ease), 0.001f, 1f), p);

            Current += (RTMath.Lerp(RTMath.Lerp(Theme[Value], Theme[value], ease), Theme[Home], Mathf.Clamp(t, 0f, 1f)) - Current) * po;

            return Current;
        }
    }
}
