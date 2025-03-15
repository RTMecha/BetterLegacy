using BetterLegacy.Editor.Data;
using LSFunctions;
using SimpleJSON;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BetterLegacy.Core.Data.Beatmap
{
    public class EventKeyframe : Exists
    {
        public EventKeyframe() { }

        public EventKeyframe(float time) => this.time = time;

        public EventKeyframe(float[] values, float[] randomValues, int random = 0)
        {
            id = LSText.randomNumString(8);
            this.random = random;
            SetEventValues(values);
            SetEventRandomValues(randomValues);
        }

        public EventKeyframe(float time, float[] values, string curve)
        {
            this.time = time;
            SetEventValues(values);
            this.curve = Parser.TryParse(curve, Easing.Linear);
        }

        public EventKeyframe(float time, float[] values, float[] randomValues, int random = 0)
        {
            this.time = time;
            this.random = random;
            SetEventValues(values);
            SetEventRandomValues(randomValues);
        }

        public EventKeyframe(float time, float value, string curve) : this(time, new float[1] { value }, curve) { }
        public EventKeyframe(float time, Vector2 value, string curve) : this(time, new float[2] { value.x, value.y }, curve) { }
        public EventKeyframe(float time, Vector3 value, string curve) : this(time, new float[3] { value.x, value.y, value.z }, curve) { }

        public static EventKeyframe DefaultPositionKeyframe => new EventKeyframe(0f, new float[3], new float[3]);
        public static EventKeyframe DefaultScaleKeyframe => new EventKeyframe(0f, new float[2] { 1f, 1f }, new float[3]);
        public static EventKeyframe DefaultRotationKeyframe => new EventKeyframe(0f, new float[1], new float[3]) { relative = true };
        public static EventKeyframe DefaultColorKeyframe => new EventKeyframe(0f, new float[10], new float[3]);

        #region Values

        public string id = LSText.randomNumString(8);

        public float[] values = new float[2];

        public float[] randomValues = new float[3];

        public int random;

        public bool relative;

        public bool locked;

        #region Timing

        public float time;

        public Easing curve = Easing.Linear;

        #endregion

        #region Reference

        public int type;
        public int index;
        public TimelineKeyframe timelineKeyframe;

        #endregion

        #endregion

        #region Methods

        public static EventKeyframe DeepCopy(EventKeyframe eventKeyframe, bool newID = true) => new EventKeyframe
        {
            id = newID ? LSText.randomNumString(8) : eventKeyframe.id,
            curve = eventKeyframe.curve,
            time = eventKeyframe.time,
            values = eventKeyframe.values.ToList().Clone().ToArray(),
            randomValues = eventKeyframe.randomValues.ToList().Clone().ToArray(),
            random = eventKeyframe.random,
            relative = eventKeyframe.relative,
            locked = eventKeyframe.locked,
        };

        public static EventKeyframe Parse(JSONNode jn, int type, int valueCount, bool defaultRelative = false)
        {
            var eventKeyframe = new EventKeyframe();

            eventKeyframe.time = jn["t"].AsFloat;

            if (jn["ct"] != null)
                eventKeyframe.curve = Parser.TryParse(jn["ct"], Easing.Linear);

            var eventValues = new List<float>();
            for (int i = 0; i < axis.Length; i++)
                if (jn[axis[i]] != null)
                    eventValues.Add(jn[axis[i]].AsFloat);

            while (eventValues.Count < valueCount)
                eventValues.Add(GameData.DefaultKeyframes[type].values[eventValues.Count]);

            while (eventValues.Count > valueCount)
                eventValues.RemoveAt(eventValues.Count - 1);

            eventKeyframe.SetEventValues(eventValues.ToArray());

            var eventRandomValues = new List<float>();
            for (int i = 0; i < raxis.Length; i++)
                if (jn[raxis[i]] != null)
                    eventRandomValues.Add(jn[raxis[i]].AsFloat);

            eventKeyframe.random = jn["r"].AsInt;

            eventKeyframe.relative = !string.IsNullOrEmpty(jn["rel"]) ? jn["rel"].AsBool : defaultRelative;

            eventKeyframe.locked = !string.IsNullOrEmpty(jn["l"]) && jn["l"].AsBool;

            eventKeyframe.SetEventRandomValues(eventRandomValues.ToArray());

            return eventKeyframe;
        }

        public JSONNode ToJSON(bool defaultRelative = false, int maxValuesToSave = -1)
        {
            JSONNode jn = JSON.Parse("{}");
            jn["t"] = time.ToString();

            for (int i = 0; i < values.Length; i++)
            {
                if (maxValuesToSave != -1 && i >= maxValuesToSave)
                    break;

                jn[axis[i]] = values[i].ToString();
            }

            if (curve != Easing.Linear)
                jn["ct"] = curve.ToString();

            if (random != 0)
            {
                jn["r"] = random.ToString();
                for (int i = 0; i < randomValues.Length; i++)
                    jn[raxis[i]] = randomValues[i].ToString();
            }

            if (relative != defaultRelative)
                jn["rel"] = relative.ToString();

            if (locked)
                jn["l"] = locked.ToString();

            return jn;
        }

        /// <summary>
        /// Set an EventKeyframe's easing via an integer. If the number is within the range of the list, then the ease is set.
        /// </summary>
        /// <param name="eventKeyframe">The EventKeyframe instance</param>
        /// <param name="ease">The ease to set to the keyframe</param>
        public void SetCurve(int ease) => curve = (Easing)Mathf.Clamp(ease, 0, DataManager.inst.AnimationList.Count - 1);

        /// <summary>
        /// Set an EventKeyframe's easing via a string. If the AnimationList contains the specified string, then the ease is set.
        /// </summary>
        /// <param name="eventKeyframe">The EventKeyframe instance</param>
        /// <param name="ease">The ease to set to the keyframe</param>
        public void SetCurve(string ease) => curve = Parser.TryParse(ease, Easing.Linear);

        public void SetEventValues(params float[] vals)
        {
            if (vals == null)
                return;

            values = new float[vals.Length];
            for (int i = 0; i < vals.Length; i++)
                values[i] = vals[i];
        }

        public void SetEventRandomValues(params float[] vals)
        {
            if (vals == null)
                return;

            randomValues = new float[vals.Length];
            for (int i = 0; i < vals.Length; i++)
                randomValues[i] = vals[i];
        }

        public void SetValue(int index, float val)
        {
            if (values.InRange(index))
                values[index] = val;
        }
        
        public void SetRandomValue(int index, float val)
        {
            if (randomValues.InRange(index))
                randomValues[index] = val;
        }

        #endregion

        #region Operators

        public override string ToString()
        {
            string strs = "";
            for (int i = 0; i < values.Length; i++)
            {
                strs += $"{values[i]}";
                if (i != values.Length - 1)
                    strs += ", ";
            }

            return $"{index}, {type}: {strs}";
        }

        public override bool Equals(object obj) => obj is EventKeyframe && id == (obj as EventKeyframe).id;

        public override int GetHashCode() => base.GetHashCode();

        #endregion

        static readonly string[] axis = new string[]
        {
            "x",
            "y",
            "z",
            "x2",
            "y2",
            "z2",
            "x3",
            "y3",
            "z3",
            "x4",
            "y4",
            "z4",
            "x5",
            "y5",
            "z5",
        };

        static readonly string[] raxis = new string[]
        {
            "rx",
            "ry",
            "rz",
            "rx2",
            "ry2",
            "rz2",
            "rx3",
            "ry3",
            "rz3",
            "rx4",
            "ry4",
            "rz4",
            "rx5",
            "ry5",
            "rz5",
        };
    }
}
