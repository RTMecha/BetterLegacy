using System.Collections.Generic;

using UnityEngine;

using LSFunctions;

using SimpleJSON;

using BetterLegacy.Core.Data.Network;
using BetterLegacy.Editor.Data.Timeline;

namespace BetterLegacy.Core.Data.Beatmap
{
    public class EventKeyframe : PAObject<EventKeyframe>, IPacket
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

        #region Defaults

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

        #endregion

        #region JSON Names

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

        #endregion

        #region Value Arrays

        /// <summary>
        /// Values of the event keyframe.
        /// </summary>
        public float[] values = new float[2];

        /// <summary>
        /// Random values of the event keyframe if <see cref="RandomType"/> is not <see cref="RandomType.None"/>
        /// </summary>
        public float[] randomValues = new float[3];

        /// <summary>
        /// String values of the event keyframe.
        /// </summary>
        public string[] stringValues;

        #endregion

        #region Settings

        /// <summary>
        /// Random type.
        /// </summary>
        public int random;

        /// <summary>
        /// Randomization type.
        /// </summary>
        public RandomType RandomType => (RandomType)random;

        /// <summary>
        /// If the event keyframe is relative / additive. With this on, the previous keyframe values are added onto the current one.
        /// </summary>
        public bool relative;

        /// <summary>
        /// If the object should flee from the player rather than follow, only if <see cref="RandomType"/> is <see cref="RandomType.HomingStatic"/> or <see cref="RandomType.HomingDynamic"/>.
        /// </summary>
        public bool flee;

        /// <summary>
        /// Target priority of the event keyframe if <see cref="RandomType"/> is <see cref="RandomType.HomingStatic"/> or <see cref="RandomType.HomingDynamic"/>.
        /// </summary>
        public HomingPriority homingPriority;

        /// <summary>
        /// Player index to target if <see cref="RandomType"/> is <see cref="RandomType.HomingStatic"/> or <see cref="RandomType.HomingDynamic"/> and <see cref="homingPriority"/> is <see cref="HomingPriority.Index"/>.
        /// </summary>
        public int playerIndex;

        /// <summary>
        /// If the event keyframe time is locked and cannot be changed in the editor.
        /// </summary>
        public bool locked;

        #endregion

        #region Timing

        /// <summary>
        /// Event keyframe time.
        /// </summary>
        public float time;

        /// <summary>
        /// Ease / curve type.
        /// </summary>
        public Easing curve = Easing.Linear;

        #endregion

        #region Reference

        /// <summary>
        /// Type of the event keyframe.
        /// </summary>
        public int type;

        /// <summary>
        /// Index of the event keyframe.
        /// </summary>
        public int index;

        /// <summary>
        /// Timeline keyframe reference for the editor.
        /// </summary>
        public TimelineKeyframe timelineKeyframe;

        #endregion

        #endregion

        #region Functions

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

        public void ReadPacket(NetworkReader reader)
        {
            type = reader.ReadInt32();
            index = reader.ReadInt32();

            time = reader.ReadSingle();
            values = reader.ReadSingleArray();
            curve = (Easing)reader.ReadUInt16();
            random = reader.ReadByte();
            if (RandomType != RandomType.None)
                randomValues = reader.ReadSingleArray();
            if (IsHoming())
            {
                flee = reader.ReadBoolean();
                homingPriority = (HomingPriority)reader.ReadByte();
                if (homingPriority == HomingPriority.Index)
                    playerIndex = reader.ReadInt32();
            }
            var hasStringValues = reader.ReadBoolean();
            if (hasStringValues)
                stringValues = reader.ReadStringArray();
            relative = reader.ReadBoolean();
            if (ProjectArrhythmia.State.InEditor)
                locked = reader.ReadBoolean();
        }

        public void WritePacket(NetworkWriter writer)
        {
            writer.Write(type);
            writer.Write(index);
            writer.Write(time);
            writer.Write(values);
            writer.Write((ushort)curve);
            writer.Write((byte)random);
            if (RandomType != RandomType.None)
                writer.Write(randomValues);
            if (IsHoming())
            {
                writer.Write(flee);
                writer.Write((byte)homingPriority);
                if (homingPriority == HomingPriority.Index)
                    writer.Write(index);
            }
            var hasStringValues = stringValues != null;
            writer.Write(hasStringValues);
            if (hasStringValues)
                writer.Write(stringValues);
            writer.Write(relative);
            if (ProjectArrhythmia.State.InEditor)
                writer.Write(locked);
        }

        /// <summary>
        /// Set an EventKeyframe's easing via an integer. If the number is within the range of the list, then the ease is set.
        /// </summary>
        /// <param name="eventKeyframe">The EventKeyframe instance</param>
        /// <param name="ease">The ease to set to the keyframe</param>
        public void SetCurve(int ease) => curve = (Easing)Mathf.Clamp(ease, 0, System.Enum.GetNames(typeof(Easing)).Length - 1);

        /// <summary>
        /// Set the keyframes's easing via a string.
        /// </summary>
        /// <param name="ease">The ease to set to the keyframe.</param>
        public void SetCurve(string ease) => curve = Parser.TryParse(ease, Easing.Linear);

        /// <summary>
        /// Sets the keyframes' values.
        /// </summary>
        /// <param name="vals">Values to set.</param>
        public void SetValues(params float[] vals)
        {
            if (vals == null)
                return;

            values = new float[vals.Length];
            for (int i = 0; i < vals.Length; i++)
                values[i] = vals[i];
        }

        /// <summary>
        /// Sets the keyframes' random values.
        /// </summary>
        /// <param name="vals">Random values to set.</param>
        public void SetRandomValues(params float[] vals)
        {
            if (vals == null)
                return;

            randomValues = new float[vals.Length];
            for (int i = 0; i < vals.Length; i++)
                randomValues[i] = vals[i];
        }

        /// <summary>
        /// Sets the keyframes' string values.
        /// </summary>
        /// <param name="vals">String values to set.</param>
        public void SetStringValues(params string[] vals)
        {
            if (vals == null)
                return;

            stringValues = new string[vals.Length];
            for (int i = 0; i < vals.Length; i++)
                stringValues[i] = vals[i];
        }

        /// <summary>
        /// Sets a value of the keyframe.
        /// </summary>
        /// <param name="index">Index of the value to set.</param>
        /// <param name="val">Value to set.</param>
        public void SetValue(int index, float val)
        {
            if (values.InRange(index))
                values[index] = val;
        }

        /// <summary>
        /// Sets a random value of the keyframe.
        /// </summary>
        /// <param name="index">Index of the random value to set.</param>
        /// <param name="val">Random value to set.</param>
        public void SetRandomValue(int index, float val)
        {
            if (randomValues.InRange(index))
                randomValues[index] = val;
        }

        /// <summary>
        /// Sets a string value of the keyframe.
        /// </summary>
        /// <param name="index">Index of the string value to set.</param>
        /// <param name="val">String value to set.</param>
        public void SetStringValue(int index, string val)
        {
            if (stringValues.InRange(index))
                stringValues[index] = val;
        }

        /// <summary>
        /// Gets a value from the keyframe.
        /// </summary>
        /// <param name="index">Index of the value to get.</param>
        /// <param name="defaultValue">Default value to return if no value exists at the <paramref name="index"/>.</param>
        /// <returns>Returns the value if <paramref name="index"/> is in the range of the <see cref="values"/> array, otherwise returns <paramref name="defaultValue"/>.</returns>
        public float GetValue(int index, float defaultValue = 0f) => values.TryGetAt(index, out float result) ? result : defaultValue;

        /// <summary>
        /// Gets a random value from the keyframe.
        /// </summary>
        /// <param name="index">Index of the random value to get.</param>
        /// <param name="defaultValue">Default value to return if no value exists at the <paramref name="index"/>.</param>
        /// <returns>Returns the value if <paramref name="index"/> is in the range of the <see cref="randomValues"/> array, otherwise returns <paramref name="defaultValue"/>.</returns>
        public float GetRandomValue(int index, float defaultValue = 0f) => randomValues.TryGetAt(index, out float result) ? result : defaultValue;

        /// <summary>
        /// Gets a string value from the keyframe.
        /// </summary>
        /// <param name="index">Index of the string value to get.</param>
        /// <param name="defaultValue">Default value to return if no value exists at the <paramref name="index"/>.</param>
        /// <returns>Returns the value if <paramref name="index"/> is in the range of the <see cref="stringValues"/> array, otherwise returns <paramref name="defaultValue"/>.</returns>
        public string GetStringValue(int index, string defaultValue = null) => stringValues.TryGetAt(index, out string result) ? result : defaultValue;

        /// <summary>
        /// Checks if the event keyframe is a homing type.
        /// </summary>
        /// <returns>Returns <see langword="true"/> if <see cref="RandomType"/> is <see cref="RandomType.HomingStatic"/> or <see cref="RandomType.HomingDynamic"/>, otherwise returns <see langword="false"/>.</returns>
        public bool IsHoming() => RandomType == RandomType.HomingStatic || RandomType == RandomType.HomingDynamic;

        /// <summary>
        /// Gets keyframe variables for math evaluation.
        /// </summary>
        /// <param name="index">Index of the value.</param>
        /// <returns>Returns a dictionary containing event keyframe variables.</returns>
        public Dictionary<string, float> GetKeyframeVariables(int index) => new Dictionary<string, float>
        {
            { "eventTime", time },
            { "currentValue", values[index] }
        };

        /// <summary>
        /// Gets keyframe variables for math evaluation.
        /// </summary>
        /// <param name="xindex">Index of the X value.</param>
        /// <param name="yindex">Index of the Y value.</param>
        /// <returns>Returns a dictionary containing event keyframe variables.</returns>
        public Dictionary<string, float> GetKeyframeVariables(int xindex, int yindex) => new Dictionary<string, float>
        {
            { "eventTime", time },
            { "currentValueX", values[xindex] },
            { "currentValueY", values[yindex] }
        };

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
    }
}
