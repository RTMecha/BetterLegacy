using System.Collections.Generic;

using UnityEngine;

using LSFunctions;

using SimpleJSON;

using BetterLegacy.Editor.Data.Timeline;

namespace BetterLegacy.Core.Data.Beatmap
{
    /// <summary>
    /// Represents the data of a keyframe that can animate an object with any amount of values.
    /// </summary>
    public class EventKeyframe : PAObject<EventKeyframe>
    {
        #region Constructors

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

        #endregion

        #region Values

        #region Global

        /// <summary>
        /// The default position keyframe.
        /// </summary>
        public static EventKeyframe DefaultPositionKeyframe => new EventKeyframe(0f, new float[3], new float[4]);
        /// <summary>
        /// The default scale keyframe.
        /// </summary>
        public static EventKeyframe DefaultScaleKeyframe => new EventKeyframe(0f, new float[2] { 1f, 1f }, new float[4]);
        /// <summary>
        /// The default rotation keyframe.
        /// </summary>
        public static EventKeyframe DefaultRotationKeyframe => new EventKeyframe(0f, new float[1], new float[4]) { relative = true };
        /// <summary>
        /// The default color keyframe.
        /// </summary>
        public static EventKeyframe DefaultColorKeyframe => new EventKeyframe(0f, new float[10], new float[3]);

        /// <summary>
        /// JSON value names.
        /// </summary>
        public static readonly string[] axis = new string[]
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

        /// <summary>
        /// JSON random value names.
        /// </summary>
        public static readonly string[] raxis = new string[]
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

        #endregion

        /// <summary>
        /// Keyframe values.
        /// </summary>
        public float[] values = new float[2];

        /// <summary>
        /// Keyframe random values, used when <see cref="random"/> is not 0.
        /// </summary>
        public float[] randomValues = new float[3];

        /// <summary>
        /// Keyframe string values, used for cases where strings are needed.
        /// </summary>
        public string[] stringValues;

        /// <summary>
        /// Keyframe random type.
        /// </summary>
        public int random;

        /// <summary>
        /// Keyframe random type.
        /// </summary>
        public RandomType RandomType => (RandomType)random;

        /// <summary>
        /// If the keyframe should add all previous keyframe values to itself in runtime.
        /// </summary>
        public bool relative;

        #region Homing

        /// <summary>
        /// If <see cref="IsHoming"/> is true and the keyframe should have the object flee rather than follow.
        /// </summary>
        public bool flee;

        /// <summary>
        /// Homing target priority.
        /// </summary>
        public HomingPriority homingPriority;

        /// <summary>
        /// Player index to target if <see cref="homingPriority"/> is <see cref="HomingPriority.Index"/>.
        /// </summary>
        public int playerIndex;

        #endregion

        #region Timing

        /// <summary>
        /// Keyframe time.
        /// </summary>
        public float time;

        /// <summary>
        /// Keyframe curve / easing type.
        /// </summary>
        public Easing curve = Easing.Linear;

        /// <summary>
        /// If the keyframes' time is locked.
        /// </summary>
        public bool locked;

        #endregion

        #region Reference

        /// <summary>
        /// Reference of the keyframe in an editor timeline.
        /// </summary>
        public TimelineKeyframe timelineKeyframe;

        #endregion

        #endregion

        #region Functions

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
            if (orig.stringValues != null)
                stringValues = orig.stringValues.Copy();
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
        /// Set a the easing via an integer.
        /// </summary>
        /// <param name="ease">The ease to set to the keyframe.</param>
        public void SetCurve(int ease) => curve = (Easing)Mathf.Clamp(ease, 0, System.Enum.GetNames(typeof(Easing)).Length - 1);

        /// <summary>
        /// Set the easing via a string.
        /// </summary>
        /// <param name="ease">The ease to set to the keyframe.</param>
        public void SetCurve(string ease) => curve = Parser.TryParse(ease, Easing.Linear);

        /// <summary>
        /// Sets the <see cref="values"/> array.
        /// </summary>
        /// <param name="values">Values to set.</param>
        public void SetValues(params float[] values)
        {
            if (values == null)
                return;

            this.values = new float[values.Length];
            for (int i = 0; i < values.Length; i++)
                this.values[i] = values[i];
        }

        /// <summary>
        /// Sets the <see cref="randomValues"/> array.
        /// </summary>
        /// <param name="values">Values to set.</param>
        public void SetRandomValues(params float[] values)
        {
            if (values == null)
                return;

            randomValues = new float[values.Length];
            for (int i = 0; i < values.Length; i++)
                randomValues[i] = values[i];
        }

        /// <summary>
        /// Sets the <see cref="stringValues"/> array.
        /// </summary>
        /// <param name="values">Values to set.</param>
        public void SetStringValues(params string[] values)
        {
            if (values == null)
                return;

            stringValues = new string[values.Length];
            for (int i = 0; i < values.Length; i++)
                stringValues[i] = values[i];
        }

        /// <summary>
        /// Sets a value at an index.
        /// </summary>
        /// <param name="index">Index of the value to set.</param>
        /// <param name="value">Value to set.</param>
        public void SetValue(int index, float value)
        {
            if (values.InRange(index))
                values[index] = value;
        }

        /// <summary>
        /// Sets a random value at an index.
        /// </summary>
        /// <param name="index">Index of the value to set.</param>
        /// <param name="value">Value to set.</param>
        public void SetRandomValue(int index, float value)
        {
            if (randomValues.InRange(index))
                randomValues[index] = value;
        }

        /// <summary>
        /// Sets a string value at an index.
        /// </summary>
        /// <param name="index">Index of the value to set.</param>
        /// <param name="value">Value to set.</param>
        public void SetStringValue(int index, string value)
        {
            if (stringValues == null)
                stringValues = new string[index + 1];

            if (stringValues.InRange(index))
                stringValues[index] = value;
            else
            {
                var sv = new string[index + 1];
                System.Array.Copy(stringValues, sv, stringValues.Length);
                sv[index] = value;
                stringValues = sv;
            }
        }

        /// <summary>
        /// Gets a value at an index.
        /// </summary>
        /// <param name="index">Index of the value to get.</param>
        /// <param name="defaultValue">Default value to return if the index is out of the range of the array.</param>
        /// <returns>Returns the specified entry in <see cref="values"/> if <paramref name="index"/> is in the range of the array, otherwise returns <paramref name="defaultValue"/>.</returns>
        public float GetValue(int index, float defaultValue = 0f) => values.TryGetAt(index, out float result) ? result : defaultValue;

        /// <summary>
        /// Gets a random value at an index.
        /// </summary>
        /// <param name="index">Index of the value to get.</param>
        /// <param name="defaultValue">Default value to return if the index is out of the range of the array.</param>
        /// <returns>Returns the specified entry in <see cref="randomValues"/> if <paramref name="index"/> is in the range of the array, otherwise returns <paramref name="defaultValue"/>.</returns>
        public float GetRandomValue(int index, float defaultValue = 0f) => randomValues.TryGetAt(index, out float result) ? result : defaultValue;

        /// <summary>
        /// Gets a string value at an index.
        /// </summary>
        /// <param name="index">Index of the value to get.</param>
        /// <param name="defaultValue">Default value to return if the index is out of the range of the array.</param>
        /// <returns>Returns the specified entry in <see cref="stringValues"/> if <paramref name="index"/> is in the range of the array, otherwise returns <paramref name="defaultValue"/>.</returns>
        public string GetStringValue(int index, string defaultValue = null) => stringValues != null && stringValues.TryGetAt(index, out string result) ? result : defaultValue;

        /// <summary>
        /// Checks if the keyframe is a homing keyframe.
        /// </summary>
        /// <returns>Returns true if <see cref="random"/> is 5 or 6.</returns>
        public bool IsHoming() => random == 5 || random == 6;

        /// <summary>
        /// Converts the color keyframe's color data to hex color.
        /// </summary>
        /// <param name="isGradient">If the reference object is a gradient.</param>
        public void ConvertToHexColor(bool isGradient)
        {
            random = 1;

            var startColorSlot = (int)values[0];
            var startOpacity = -(values[1] - 1f);
            var startHue = values[2];
            var startSat = values[3];
            var startVal = values[4];

            if (isGradient)
            {
                var endColorSlot = (int)values[5];
                var endOpacity = -(values[6] - 1f);
                var endHue = values[7];
                var endSat = values[8];
                var endVal = values[9];

                SetStringValues(
                    RTColors.ColorToHexOptional(RTColors.FadeColor(RTColors.ChangeColorHSV(Helpers.CoreHelper.CurrentBeatmapTheme.GetObjColor(startColorSlot), startHue, startSat, startVal), startOpacity)),
                    RTColors.ColorToHexOptional(RTColors.FadeColor(RTColors.ChangeColorHSV(Helpers.CoreHelper.CurrentBeatmapTheme.GetObjColor(endColorSlot), endHue, endSat, endVal), endOpacity)));
            }
            else
            {
                SetStringValues(
                    RTColors.ColorToHexOptional(RTColors.FadeColor(RTColors.ChangeColorHSV(Helpers.CoreHelper.CurrentBeatmapTheme.GetObjColor(startColorSlot), startHue, startSat, startVal), startOpacity)),
                    RTColors.WHITE_HEX_CODE);
            }
        }

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

        public override string ToString()
        {
            string strs = $"{time} - {curve}: ";
            for (int i = 0; i < values.Length; i++)
            {
                strs += $"{values[i]}";
                if (i != values.Length - 1)
                    strs += ", ";
            }
            return strs;
        }

        public override bool Equals(object obj) => obj is EventKeyframe eventKeyframe && id == eventKeyframe.id;

        public override int GetHashCode() => base.GetHashCode();

        #endregion
    }
}
