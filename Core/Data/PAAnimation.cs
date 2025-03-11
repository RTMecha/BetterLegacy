using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using LSFunctions;

using SimpleJSON;

using BetterLegacy.Editor.Managers;
using BetterLegacy.Core.Animation;
using BetterLegacy.Core.Animation.Keyframe;
using BetterLegacy.Core.Optimization;
using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Data.Player;
using BetterLegacy.Core.Optimization.Objects;

namespace BetterLegacy.Core.Data
{
    // TODO: figure out how to get this working properly
    public class PAAnimation : Exists
    {
        public PAAnimation()
        {
            id = LSText.randomNumString(16);
            markers = new List<Marker>();

            positionKeyframes.Add(EventKeyframe.DefaultPositionKeyframe);
            scaleKeyframes.Add(EventKeyframe.DefaultScaleKeyframe);
            rotationKeyframes.Add(EventKeyframe.DefaultRotationKeyframe);
            colorKeyframes.Add(EventKeyframe.DefaultColorKeyframe);
        }

        public PAAnimation(string name, string desc)
        {
            id = LSText.randomNumString(16);
            this.name = name;
            this.desc = desc;
            StartTime = 0f;
            markers = new List<Marker>();

            positionKeyframes.Add(EventKeyframe.DefaultPositionKeyframe);
            scaleKeyframes.Add(EventKeyframe.DefaultScaleKeyframe);
            rotationKeyframes.Add(EventKeyframe.DefaultRotationKeyframe);
            colorKeyframes.Add(EventKeyframe.DefaultColorKeyframe);
        }
        
        public PAAnimation(string name, string desc, float startTime)
        {
            id = LSText.randomNumString(16);
            this.name = name;
            this.desc = desc;
            StartTime = startTime;
            markers = new List<Marker>();

            positionKeyframes.Add(EventKeyframe.DefaultPositionKeyframe);
            scaleKeyframes.Add(EventKeyframe.DefaultScaleKeyframe);
            rotationKeyframes.Add(EventKeyframe.DefaultRotationKeyframe);
            colorKeyframes.Add(EventKeyframe.DefaultColorKeyframe);
        }
        
        #region Fields

        /// <summary>
        /// ID of the animation.
        /// </summary>
        public string id;
        /// <summary>
        /// Name of the animation.
        /// </summary>
        public string name = "Anim";
        /// <summary>
        /// Description of the animation.
        /// </summary>
        public string desc = "This is the default description!";
        float startTime;
        /// <summary>
        /// Time the animation starts at.
        /// </summary>
        public float StartTime { get => Mathf.Clamp(startTime, 0f, float.MaxValue); set => startTime = Mathf.Clamp(value, 0f, float.MaxValue); }
        /// <summary>
        /// Markers to render in the <see cref="AnimationEditor"/>
        /// </summary>
        public List<Marker> markers;

        /// <summary>
        /// ID used for comparing objects with their associated animations.
        /// </summary>
        public string ReferenceID { get; set; }

        public List<EventKeyframe> positionKeyframes = new List<EventKeyframe>();
        public List<EventKeyframe> scaleKeyframes = new List<EventKeyframe>();
        public List<EventKeyframe> rotationKeyframes = new List<EventKeyframe>();
        public List<EventKeyframe> colorKeyframes = new List<EventKeyframe>();

        /// <summary>
        /// If this is on, the values of the first keyframes will be replaced with the transition values.
        /// </summary>
        public bool transition;

        public bool animatePosition = true;
        public bool animateScale = true;
        public bool animateRotation = true;
        public bool animateColor;

        #endregion

        #region Methods

        public float GetLength()
        {
            var objectsLength = GetLength(0) + GetLength(1) + GetLength(2) + GetLength(3);
            var markersLength = markers.IsEmpty() ? 0f : markers.Max(x => x.time);

            return objectsLength > markersLength ? objectsLength : markersLength;
        }

        public float GetLength(int type) => type switch
        {
            0 => positionKeyframes.IsEmpty() ? 0f : positionKeyframes.Max(x => x.time),
            1 => scaleKeyframes.IsEmpty() ? 0f : scaleKeyframes.Max(x => x.time),
            2 => rotationKeyframes.IsEmpty() ? 0f : rotationKeyframes.Max(x => x.time),
            3 => colorKeyframes.IsEmpty() ? 0f : colorKeyframes.Max(x => x.time),
            _ => 0f,
        };

        public static PAAnimation DeepCopy(PAAnimation orig, bool newID = true) => new PAAnimation
        {
            id = newID ? LSText.randomNumString(16) : orig.id,
            name = orig.name,
            desc = orig.desc,
            markers = orig.markers.Select(x => Marker.DeepCopy(x)).ToList(),
            ReferenceID = orig.ReferenceID,
            StartTime = orig.StartTime,

            positionKeyframes = orig.positionKeyframes.Select(x => EventKeyframe.DeepCopy(x)).ToList(),
            scaleKeyframes = orig.scaleKeyframes.Select(x => EventKeyframe.DeepCopy(x)).ToList(),
            rotationKeyframes = orig.rotationKeyframes.Select(x => EventKeyframe.DeepCopy(x)).ToList(),
            colorKeyframes = orig.colorKeyframes.Select(x => EventKeyframe.DeepCopy(x)).ToList(),

            animatePosition = orig.animatePosition,
            animateScale = orig.animateScale,
            animateRotation = orig.animateRotation,
            animateColor = orig.animateColor,
            transition = orig.transition,
        };

        public static PAAnimation Parse(JSONNode jn)
        {
            var animation = new PAAnimation(jn["name"], jn["desc"], jn["st"].AsFloat)
            {
                id = jn["id"] ?? LSText.randomNumString(16),
            };

            for (int i = 0; i < jn["markers"].Count; i++)
                animation.markers.Add(Marker.Parse(jn["markers"][i]));

            if (!string.IsNullOrEmpty(jn["ref_id"]))
                animation.ReferenceID = jn["ref_id"];

            if (jn["tr"] != null)
                animation.transition = jn["tr"].AsBool;

            if (jn["anim"]["pos"] != null)
                animation.animatePosition = jn["anim"]["pos"].AsBool;
            if (jn["anim"]["sca"] != null)
                animation.animateScale = jn["anim"]["sca"].AsBool;
            if (jn["anim"]["rot"] != null)
                animation.animateRotation = jn["anim"]["rot"].AsBool;
            if (jn["anim"]["col"] != null)
                animation.animateColor = jn["anim"]["col"].AsBool;

            if (jn["pos"] != null)
            {
                animation.positionKeyframes.Clear();
                for (int i = 0; i < jn["pos"].Count; i++)
                    animation.positionKeyframes.Add(EventKeyframe.Parse(jn["pos"][i], 0, 3));
            }
            if (jn["sca"] != null)
            {
                animation.scaleKeyframes.Clear();
                for (int i = 0; i < jn["sca"].Count; i++)
                    animation.scaleKeyframes.Add(EventKeyframe.Parse(jn["sca"][i], 0, 2));
            }
            if (jn["rot"] != null)
            {
                animation.rotationKeyframes.Clear();
                for (int i = 0; i < jn["rot"].Count; i++)
                    animation.rotationKeyframes.Add(EventKeyframe.Parse(jn["rot"][i], 0, 1));
            }
            if (jn["col"] != null)
            {
                animation.colorKeyframes.Clear();
                for (int i = 0; i < jn["col"].Count; i++)
                    animation.colorKeyframes.Add(EventKeyframe.Parse(jn["col"][i], 0, 5));
            }

            return animation;
        }

        public JSONNode ToJSON()
        {
            var jn = JSON.Parse("{}");
            jn["id"] = id;
            jn["name"] = name;
            jn["st"] = StartTime.ToString();
            for (int i = 0; i < markers.Count; i++)
                jn["markers"][i] = markers[i].ToJSON();

            if (!string.IsNullOrEmpty(ReferenceID))
                jn["ref_id"] = ReferenceID;

            if (!animatePosition)
                jn["anim"]["pos"] = animatePosition;
            if (!animateScale)
                jn["anim"]["sca"] = animateScale;
            if (!animateRotation)
                jn["anim"]["rot"] = animateRotation;
            if (animateColor)
                jn["anim"]["col"] = animateColor;

            for (int i = 0; i < positionKeyframes.Count; i++)
                jn["pos"][i] = (positionKeyframes[i] as EventKeyframe).ToJSON();
            for (int i = 0; i < scaleKeyframes.Count; i++)
                jn["sca"][i] = (scaleKeyframes[i] as EventKeyframe).ToJSON();
            for (int i = 0; i < rotationKeyframes.Count; i++)
                jn["rot"][i] = (rotationKeyframes[i] as EventKeyframe).ToJSON();
            for (int i = 0; i < colorKeyframes.Count; i++)
                jn["col"][i] = (colorKeyframes[i] as EventKeyframe).ToJSON();

            return jn;
        }

        public RTAnimation ToRTAnimation(Transform transform) => ToRTAnimation(transform, Vector3.zero, Vector2.one, 0f);

        public RTAnimation ToRTAnimation(Transform transform, Vector3 positionOffset, Vector2 scaleOffset, float rotationOffset)
        {
            var runtimeAnim = new RTAnimation("PA Animation");

            if (!transform)
                return runtimeAnim;

            if (animatePosition)
                for (int i = 0; i < positionKeyframes.Count; i++)
                {
                    var positionKeyframes = ObjectConverter.GetVector3Keyframes(this.positionKeyframes, ObjectConverter.DefaultVector3Keyframe);

                    if (transition)
                        positionKeyframes[0].SetValue(transform.localPosition);

                    runtimeAnim.animationHandlers.Add(new AnimationHandler<Vector3>(positionKeyframes, vector =>
                    {
                        if (transform)
                            transform.localPosition = vector + positionOffset;
                    }, interpolateOnComplete: true));
                }
            if (animateScale)
                for (int i = 0; i < scaleKeyframes.Count; i++)
                {
                    var scaleKeyframes = ObjectConverter.GetVector2Keyframes(this.scaleKeyframes, ObjectConverter.DefaultVector2Keyframe);

                    if (transition)
                        scaleKeyframes[0].SetValue(transform.localScale);

                    runtimeAnim.animationHandlers.Add(new AnimationHandler<Vector2>(scaleKeyframes, vector =>
                    {
                        if (transform)
                            transform.localScale = new Vector3(scaleOffset.x * vector.x, scaleOffset.y * vector.y, 1f);
                    }, interpolateOnComplete: true));
                }
            if (animateRotation)
                for (int i = 0; i < rotationKeyframes.Count; i++)
                {
                    var rotationKeyframes = ObjectConverter.GetFloatKeyframes(this.rotationKeyframes, 0, ObjectConverter.DefaultFloatKeyframe);

                    if (transition)
                        rotationKeyframes[0].SetValue(transform.localEulerAngles.z);

                    runtimeAnim.animationHandlers.Add(new AnimationHandler<float>(rotationKeyframes, x =>
                    {
                        if (transform)
                            transform.localEulerAngles = (new Vector3(0f, 0f, rotationOffset + x));
                    }, interpolateOnComplete: true));
                }

            return runtimeAnim;
        }

        #endregion
    }
}
