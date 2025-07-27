using System.Collections.Generic;
using System.Linq;

using UnityEngine;

using LSFunctions;

using SimpleJSON;

using BetterLegacy.Core.Animation;
using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Runtime.Objects;
using BetterLegacy.Editor.Managers;

namespace BetterLegacy.Core.Data
{
    // TODO: figure out how to get this working properly
    public class PAAnimation : PAObject<PAAnimation>
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

        public override void CopyData(PAAnimation orig, bool newID = true)
        {
            id = newID ? LSText.randomNumString(16) : orig.id;
            name = orig.name;
            desc = orig.desc;
            markers = orig.markers.Select(x => x.Copy()).ToList();
            ReferenceID = orig.ReferenceID;
            StartTime = orig.StartTime;

            positionKeyframes = orig.positionKeyframes.Select(x => x.Copy()).ToList();
            scaleKeyframes = orig.scaleKeyframes.Select(x => x.Copy()).ToList();
            rotationKeyframes = orig.rotationKeyframes.Select(x => x.Copy()).ToList();
            colorKeyframes = orig.colorKeyframes.Select(x => x.Copy()).ToList();

            animatePosition = orig.animatePosition;
            animateScale = orig.animateScale;
            animateRotation = orig.animateRotation;
            animateColor = orig.animateColor;
            transition = orig.transition;
        }

        public override void ReadJSON(JSONNode jn)
        {
            id = jn["id"] ?? LSText.randomNumString(16);
            name = jn["name"];
            desc = jn["desc"];
            StartTime = jn["st"].AsFloat;
            markers = new List<Marker>();
            positionKeyframes = new List<EventKeyframe>();
            positionKeyframes.Add(EventKeyframe.DefaultPositionKeyframe);
            scaleKeyframes = new List<EventKeyframe>();
            scaleKeyframes.Add(EventKeyframe.DefaultScaleKeyframe);
            rotationKeyframes = new List<EventKeyframe>();
            rotationKeyframes.Add(EventKeyframe.DefaultRotationKeyframe);
            colorKeyframes = new List<EventKeyframe>();
            colorKeyframes.Add(EventKeyframe.DefaultColorKeyframe);

            for (int i = 0; i < jn["markers"].Count; i++)
                markers.Add(Marker.Parse(jn["markers"][i]));

            if (!string.IsNullOrEmpty(jn["ref_id"]))
                ReferenceID = jn["ref_id"];

            if (jn["tr"] != null)
                transition = jn["tr"].AsBool;

            if (jn["anim"]["pos"] != null)
                animatePosition = jn["anim"]["pos"].AsBool;
            if (jn["anim"]["sca"] != null)
                animateScale = jn["anim"]["sca"].AsBool;
            if (jn["anim"]["rot"] != null)
                animateRotation = jn["anim"]["rot"].AsBool;
            if (jn["anim"]["col"] != null)
                animateColor = jn["anim"]["col"].AsBool;

            ReadKeyframesJSON(jn["pos"] ?? jn["e"]["pos"], positionKeyframes, 0, 3);
            ReadKeyframesJSON(jn["sca"] ?? jn["e"]["sca"], scaleKeyframes, 1, 2);
            ReadKeyframesJSON(jn["rot"] ?? jn["e"]["rot"], rotationKeyframes, 2, 1);
            ReadKeyframesJSON(jn["col"] ?? jn["e"]["col"], colorKeyframes, 3, 5);
        }

        void ReadKeyframesJSON(JSONNode jn, List<EventKeyframe> eventKeyframes, int type, int valueCount, bool defaultRelative = false)
        {
            if (jn == null)
                return;

            eventKeyframes.Clear();
            for (int i = 0; i < jn.Count; i++)
                eventKeyframes.Add(EventKeyframe.Parse(jn[i], type, valueCount, defaultRelative));
        }

        public override JSONNode ToJSON()
        {
            var jn = Parser.NewJSONObject();
            jn["id"] = id;
            jn["name"] = name;
            jn["st"] = StartTime;
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
                jn["pos"][i] = positionKeyframes[i].ToJSON();
            for (int i = 0; i < scaleKeyframes.Count; i++)
                jn["sca"][i] = scaleKeyframes[i].ToJSON();
            for (int i = 0; i < rotationKeyframes.Count; i++)
                jn["rot"][i] = rotationKeyframes[i].ToJSON(true);
            for (int i = 0; i < colorKeyframes.Count; i++)
                jn["col"][i] = colorKeyframes[i].ToJSON();

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
                    var positionKeyframes = ObjectConverter.GetVector3Keyframes(this, this.positionKeyframes, ObjectConverter.DefaultVector3Keyframe);

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
                    var scaleKeyframes = ObjectConverter.GetVector2Keyframes(this, this.scaleKeyframes, ObjectConverter.DefaultVector2Keyframe);

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
                    var rotationKeyframes = ObjectConverter.GetFloatKeyframes(this, this.rotationKeyframes, 0, ObjectConverter.DefaultFloatKeyframe);

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
