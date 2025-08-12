using System.Collections.Generic;
using System.Linq;

using UnityEngine;

using LSFunctions;

using SimpleJSON;

using BetterLegacy.Core.Animation;
using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Runtime.Objects;
using BetterLegacy.Editor.Data.Timeline;
using BetterLegacy.Editor.Managers;

namespace BetterLegacy.Core.Data
{
    public class PAAnimation : PAObject<PAAnimation>, IAnimatable
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

        public PAAnimation(string name, string description)
        {
            id = LSText.randomNumString(16);
            this.name = name;
            this.description = description;
            StartTime = 0f;
            markers = new List<Marker>();

            positionKeyframes.Add(EventKeyframe.DefaultPositionKeyframe);
            scaleKeyframes.Add(EventKeyframe.DefaultScaleKeyframe);
            rotationKeyframes.Add(EventKeyframe.DefaultRotationKeyframe);
            colorKeyframes.Add(EventKeyframe.DefaultColorKeyframe);
        }
        
        public PAAnimation(string name, string description, float startTime)
        {
            id = LSText.randomNumString(16);
            this.name = name;
            this.description = description;
            StartTime = startTime;
            markers = new List<Marker>();

            positionKeyframes.Add(EventKeyframe.DefaultPositionKeyframe);
            scaleKeyframes.Add(EventKeyframe.DefaultScaleKeyframe);
            rotationKeyframes.Add(EventKeyframe.DefaultRotationKeyframe);
            colorKeyframes.Add(EventKeyframe.DefaultColorKeyframe);
        }

        #region Values

        public string ID { get => id; set => id = value; }

        /// <summary>
        /// Name of the animation.
        /// </summary>
        public string name = "Anim";

        /// <summary>
        /// Description of the animation.
        /// </summary>
        public string description = "This is the default description!";

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

        public List<List<EventKeyframe>> Events => new List<List<EventKeyframe>>
        {
            positionKeyframes,
            scaleKeyframes,
            rotationKeyframes,
            colorKeyframes,
        };

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

        #region Editor

        public ObjectEditorData EditorData { get; set; } = new ObjectEditorData();

        public List<TimelineKeyframe> TimelineKeyframes { get; set; } = new List<TimelineKeyframe>();

        #endregion

        #endregion

        #region Methods

        public float GetLength(bool markers = false)
        {
            var animLength = 0f;
            for (int i = 0; i < 4; i++)
            {
                var length = GetLength(i);
                if (length > animLength)
                    animLength = length;
            }

            if (!markers)
                return animLength;

            var markersLength = this.markers.IsEmpty() ? 0f : this.markers.Max(x => x.time);

            return animLength > markersLength ? animLength : markersLength;
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
            description = orig.description;
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

            if (!EditorData)
                EditorData = new ObjectEditorData();
            EditorData.CopyData(orig.EditorData);
        }

        public override void ReadJSON(JSONNode jn)
        {
            id = jn["id"] ?? LSText.randomNumString(16);
            name = jn["name"] ?? string.Empty;
            description = jn["desc"] ?? string.Empty;
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

            var d = EventKeyframe.DefaultPositionKeyframe;
            ReadKeyframesJSON(jn["pos"] ?? jn["e"]["pos"], positionKeyframes, 0, 3, 4, d.values, d.randomValues);
            d = EventKeyframe.DefaultScaleKeyframe;
            ReadKeyframesJSON(jn["sca"] ?? jn["e"]["sca"], scaleKeyframes, 1, 2, 4, d.values, d.randomValues);
            d = EventKeyframe.DefaultRotationKeyframe;
            ReadKeyframesJSON(jn["rot"] ?? jn["e"]["rot"], rotationKeyframes, 2, 1, 4, d.values, d.randomValues, true);
            d = EventKeyframe.DefaultColorKeyframe;
            ReadKeyframesJSON(jn["col"] ?? jn["e"]["col"], colorKeyframes, 3, 10, 3, d.values, d.randomValues);

            if (jn["ed"] != null)
                EditorData = ObjectEditorData.Parse(jn["ed"]);
        }

        void ReadKeyframesJSON(JSONNode jn, List<EventKeyframe> eventKeyframes, int type, int valueCount, int randomValueCount, float[] origValues, float[] origRandomValues, bool defaultRelative = false)
        {
            if (jn == null)
                return;

            eventKeyframes.Clear();
            for (int i = 0; i < jn.Count; i++)
                eventKeyframes.Add(EventKeyframe.Parse(jn[i], type, valueCount, randomValueCount, origValues, origRandomValues, defaultRelative));
        }

        public override JSONNode ToJSON()
        {
            var jn = Parser.NewJSONObject();
            jn["id"] = id;
            jn["name"] = name;
            if (!string.IsNullOrEmpty(description))
                jn["desc"] = description;

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

            if (EditorData && EditorData.ShouldSerialize)
                jn["ed"] = EditorData.ToJSON();

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

        public List<EventKeyframe> GetEventKeyframes(int type) => type switch
        {
            0 => positionKeyframes,
            1 => scaleKeyframes,
            2 => rotationKeyframes,
            3 => colorKeyframes,
            _ => null,
        };

        int SearchKeyframe(int type, float time)
        {
            int low = 0;
            var keyframes = GetEventKeyframes(type);
            int high = keyframes.Count - 1;

            while (low <= high)
            {
                int mid = (low + high) / 2;
                float midTime = keyframes[mid].time;

                if (time < midTime)
                    high = mid - 1;
                else if (time > midTime)
                    low = mid + 1;
                else
                    return mid;
            }

            return low - 1;
        }

        public float Interpolate(int type, int valueIndex, float time)
        {
            var list = GetEventKeyframes(type);
            if (list == null)
                return 0f;

            var prevKFIndex = RTMath.Clamp(SearchKeyframe(type, time), 0, list.Count - 1);
            var nextKFIndex = RTMath.Clamp(prevKFIndex + 1, 0, list.Count - 1);

            var prevKF = list[prevKFIndex];
            var nextKF = list[nextKFIndex];

            valueIndex = Mathf.Clamp(valueIndex, 0, list[0].values.Length);

            if (prevKF.values.Length <= valueIndex)
                return 0f;

            if (time <= 0f)
                return prevKF.values[valueIndex];

            var total = 0f;
            var prevtotal = 0f;
            if (prevKF.relative || nextKF.relative)
            {
                for (int k = 0; k < nextKFIndex; k++)
                {
                    if (list[k + 1].relative)
                        total += list[k].values[valueIndex];
                    else
                        total = 0f;

                    if (list[k].relative)
                        prevtotal += list[k].values[valueIndex];
                    else
                        prevtotal = 0f;
                }
            }

            var next = nextKF.relative ? total + nextKF.values[valueIndex] : nextKF.values[valueIndex];
            var prev = prevKF.relative || nextKF.relative ? prevtotal : prevKF.values[valueIndex];

            bool isLerper = type != 3 || valueIndex != 0 || valueIndex != 5;

            if (float.IsNaN(prev) || !isLerper)
                prev = 0f;

            if (float.IsNaN(next))
                next = 0f;

            if (!isLerper)
                next = 1f;

            if (prevKFIndex == nextKFIndex)
                return next;

            var x = RTMath.Lerp(prev, next, Ease.GetEaseFunction(nextKF.curve.ToString())(RTMath.InverseLerp(prevKF.time, nextKF.time, Mathf.Clamp(time, 0f, nextKF.time))));

            if (float.IsNaN(x) || float.IsInfinity(x))
                x = next;

            return x;
        }

        public float Interpolate(EventKeyframe prevKeyframe, EventKeyframe nextKeyframe, int type, int valueIndex, float time)
        {
            var list = GetEventKeyframes(type);
            if (list == null)
                return 0f;

            valueIndex = Mathf.Clamp(valueIndex, 0, prevKeyframe.values.Length);

            if (prevKeyframe.values.Length <= valueIndex)
                return 0f;

            var total = 0f;
            var prevtotal = 0f;
            if (prevKeyframe.relative || nextKeyframe.relative)
            {
                for (int k = 0; k < list.Count; k++)
                {
                    if (time >= list[k].time)
                        break;

                    if (list[k + 1].relative)
                        total += list[k].values[valueIndex];
                    else
                        total = 0f;

                    if (list[k].relative)
                        prevtotal += list[k].values[valueIndex];
                    else
                        prevtotal = 0f;
                }
            }

            var next = nextKeyframe.relative ? total + nextKeyframe.values[valueIndex] : nextKeyframe.values[valueIndex];
            var prev = prevKeyframe.relative || nextKeyframe.relative ? prevtotal : prevKeyframe.values[valueIndex];

            bool isLerper = type != 3 || valueIndex != 0;

            if (float.IsNaN(prev) || !isLerper)
                prev = 0f;

            if (float.IsNaN(next))
                next = 0f;

            if (!isLerper)
                next = 1f;

            if (prevKeyframe == nextKeyframe)
                return next;

            var x = RTMath.Lerp(prev, next, Ease.GetEaseFunction(nextKeyframe.curve.ToString())(RTMath.InverseLerp(prevKeyframe.time, nextKeyframe.time, Mathf.Clamp(time, 0f, nextKeyframe.time))));

            if (float.IsNaN(x) || float.IsInfinity(x))
                x = next;

            return x;
        }

        public void SortKeyframes() => SortKeyframes(Events);

        public void SortKeyframes(List<List<EventKeyframe>> events)
        {
            for (int i = 0; i < events.Count; i++)
                SortKeyframes(i);
        }

        public void SortKeyframes(int type) => SortKeyframes(type switch
        {
            0 => positionKeyframes,
            1 => scaleKeyframes,
            2 => rotationKeyframes,
            3 => colorKeyframes,
            _ => null,
        });

        public void SortKeyframes(List<EventKeyframe> eventKeyframes) => eventKeyframes.Sort((a, b) => a.time.CompareTo(b.time));

        #endregion
    }
}
