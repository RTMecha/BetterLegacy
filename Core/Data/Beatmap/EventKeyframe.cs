using System.Collections.Generic;

using UnityEngine;

using LSFunctions;

using SimpleJSON;

using BetterLegacy.Editor.Data.Timeline;

namespace BetterLegacy.Core.Data.Beatmap
{
    public class EventKeyframe : PAObject<EventKeyframe>
    {
        public EventKeyframe()
        {
            id = LSText.randomNumString(8);
        }

        public EventKeyframe(float time) : this() => this.time = time;

        public EventKeyframe(float[] values, float[] randomValues, int random = 0) : this()
        {
            this.random = random;
            SetValues(values);
            SetRandomValues(randomValues);
        }

        public EventKeyframe(float time, float[] values, string curve) : this()
        {
            this.time = time;
            SetValues(values);
            this.curve = Parser.TryParse(curve, Easing.Linear);
        }

        public EventKeyframe(float time, float[] values, float[] randomValues, int random = 0) : this()
        {
            this.time = time;
            this.random = random;
            SetValues(values);
            SetRandomValues(randomValues);
        }

        public EventKeyframe(float time, float value, string curve) : this(time, new float[1] { value }, curve) { }
        public EventKeyframe(float time, Vector2 value, string curve) : this(time, new float[2] { value.x, value.y }, curve) { }
        public EventKeyframe(float time, Vector3 value, string curve) : this(time, new float[3] { value.x, value.y, value.z }, curve) { }

        public static EventKeyframe DefaultPositionKeyframe => new EventKeyframe(0f, new float[3], new float[4]);
        public static EventKeyframe DefaultScaleKeyframe => new EventKeyframe(0f, new float[2] { 1f, 1f }, new float[4]);
        public static EventKeyframe DefaultRotationKeyframe => new EventKeyframe(0f, new float[1], new float[4]) { relative = true };
        public static EventKeyframe DefaultColorKeyframe => new EventKeyframe(0f, new float[10], new float[3]);

        #region Values

        public float[] values = new float[2];

        public float[] randomValues = new float[3];

        public string[] stringValues;

        public int random;

        public RandomType RandomType => (RandomType)random;

        public bool relative;

        public bool flee;

        public HomingPriority homingPriority;

        public int playerIndex;

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

        /// <summary>
        /// Copies data from another EventKeyframe.
        /// </summary>
        /// <param name="orig">Original object to copy data from.</param>
        public override void CopyData(EventKeyframe orig, bool newID = true)
        {
            id = newID ? LSText.randomNumString(8) : orig.id;
            curve = orig.curve;
            time = orig.time;
            values = orig.values.Copy();
            randomValues = orig.randomValues.Copy();
            random = orig.random;
            flee = orig.flee;
            relative = orig.relative;
            locked = orig.locked;
        }

        public static EventKeyframe Parse(JSONNode jn, int type, int valueCount, int randomValueCount, float[] origValues, float[] origRandomValues, bool defaultRelative = false)
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
                eventValues.Add(origValues[eventValues.Count]);
            while (eventValues.Count > valueCount)
                eventValues.RemoveAt(eventValues.Count - 1);

            eventKeyframe.SetValues(eventValues.ToArray());

            var eventRandomValues = new List<float>();
            for (int i = 0; i < raxis.Length; i++)
                if (jn[raxis[i]] != null)
                    eventRandomValues.Add(jn[raxis[i]].AsFloat);

            while (eventRandomValues.Count < randomValueCount)
                eventRandomValues.Add(origRandomValues[eventRandomValues.Count]);
            while (eventRandomValues.Count > randomValueCount)
                eventRandomValues.RemoveAt(eventRandomValues.Count - 1);

            eventKeyframe.SetRandomValues(eventRandomValues.ToArray());

            if (jn["str"] != null)
            {
                eventKeyframe.stringValues = new string[jn["str"].Count];
                for (int i = 0; i < jn["str"].Count; i++)
                    eventKeyframe.stringValues[i] = jn["str"][i];
            }

            eventKeyframe.random = jn["r"].AsInt;

            eventKeyframe.relative = jn["rel"] != null ? jn["rel"].AsBool : defaultRelative;
            eventKeyframe.flee = jn["flee"].AsBool;

            eventKeyframe.locked = jn["l"].AsBool;

            return eventKeyframe;
        }

        public JSONNode ToJSON(bool defaultRelative = false, int maxValuesToSave = -1)
        {
            JSONNode jn = Parser.NewJSONObject();
            jn["t"] = time;

            for (int i = 0; i < values.Length; i++)
            {
                if (maxValuesToSave != -1 && i >= maxValuesToSave)
                    break;

                jn[axis[i]] = values[i];
            }

            if (curve != Easing.Linear)
                jn["ct"] = curve.ToString();

            if (random != 0)
            {
                jn["r"] = random;
                for (int i = 0; i < randomValues.Length; i++)
                    jn[raxis[i]] = randomValues[i];
            }

            if (stringValues != null)
            {
                for (int i = 0; i < stringValues.Length; i++)
                    jn["str"][i] = stringValues[i];
            }

            if (relative != defaultRelative)
                jn["rel"] = relative;

            if (flee)
                jn["flee"] = flee;
            if (homingPriority != HomingPriority.Closest)
                jn["hop"] = (int)homingPriority;
            if (playerIndex != 0)
                jn["pindex"] = playerIndex;

            if (locked)
                jn["l"] = locked;

            return jn;
        }

        /// <summary>
        /// Set an EventKeyframe's easing via an integer. If the number is within the range of the list, then the ease is set.
        /// </summary>
        /// <param name="eventKeyframe">The EventKeyframe instance</param>
        /// <param name="ease">The ease to set to the keyframe</param>
        public void SetCurve(int ease) => curve = (Easing)Mathf.Clamp(ease, 0, System.Enum.GetNames(typeof(Easing)).Length - 1);

        /// <summary>
        /// Set an EventKeyframe's easing via a string. If the AnimationList contains the specified string, then the ease is set.
        /// </summary>
        /// <param name="eventKeyframe">The EventKeyframe instance</param>
        /// <param name="ease">The ease to set to the keyframe</param>
        public void SetCurve(string ease) => curve = Parser.TryParse(ease, Easing.Linear);

        public void SetValues(params float[] vals)
        {
            if (vals == null)
                return;

            values = new float[vals.Length];
            for (int i = 0; i < vals.Length; i++)
                values[i] = vals[i];
        }

        public void SetRandomValues(params float[] vals)
        {
            if (vals == null)
                return;

            randomValues = new float[vals.Length];
            for (int i = 0; i < vals.Length; i++)
                randomValues[i] = vals[i];
        }

        public void SetStringValues(params string[] vals)
        {
            if (vals == null)
                return;

            stringValues = new string[vals.Length];
            for (int i = 0; i < vals.Length; i++)
                stringValues[i] = vals[i];
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

        public void SetStringValue(int index, string val)
        {
            if (stringValues.InRange(index))
                stringValues[index] = val;
        }

        public float GetValue(int index, float defaultValue = 0f) => values.TryGetAt(index, out float result) ? result : defaultValue;

        public float GetRandomValue(int index, float defaultValue = 0f) => randomValues.TryGetAt(index, out float result) ? result : defaultValue;

        public string GetStringValue(int index, string defaultValue = null) => stringValues.TryGetAt(index, out string result) ? result : defaultValue;

        public bool IsHoming() => random == 5 || random == 6;

        public Dictionary<string, float> GetKeyframeVariables(int index) => new Dictionary<string, float>
        {
            { "eventTime", time },
            { "currentValue", values[index] }
        };
        
        public Dictionary<string, float> GetKeyframeVariables(int xindex, int yindex) => new Dictionary<string, float>
        {
            { "eventTime", time },
            { "currentValueX", values[xindex] },
            { "currentValueY", values[yindex] }
        };

        #endregion

        #region Operators

        public override string ToString()
        {
            string strs = string.Empty;
            for (int i = 0; i < values.Length; i++)
            {
                strs += $"{values[i]}";
                if (i != values.Length - 1)
                    strs += ", ";
            }

            return $"{index}, {type}: {strs}";
        }

        public override bool Equals(object obj) => obj is EventKeyframe eventKeyframe && id == eventKeyframe.id;

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
