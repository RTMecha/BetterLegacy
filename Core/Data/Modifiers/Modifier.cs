using System;
using System.Collections.Generic;

using SimpleJSON;

using BetterLegacy.Core.Data.Network;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Editor.Data.Elements;

namespace BetterLegacy.Core.Data.Modifiers
{
    public class Modifier : PAObject<Modifier>, IPacket
    {
        #region Constructors

        public Modifier() { }

        public Modifier(string name) : this() => this.name = name;

        public Modifier(Type type, string name, bool constant, params string[] values)
        {
            this.name = name;
            this.type = type;
            this.constant = constant;
            for (int i = 0; i < values.Length; i++)
                this.values.Add(values[i]);
        }
        
        public Modifier(ModifierCompatibility compatibility, Type type, string name, bool constant, params string[] values) : this(type, name, constant, values) => this.compatibility = compatibility;

        public Modifier(Type type, string name, bool constant,
            Action<Modifier, ModifierLoop> action,
            Func<Modifier, ModifierLoop, bool> trigger,
            Action<Modifier, ModifierLoop> inactive,
            params string[] values)
        {
            this.name = name;
            this.type = type;
            this.constant = constant;
            for (int i = 0; i < values.Length; i++)
                this.values.Add(values[i]);

            Action = action;
            Trigger = trigger;
            Inactive = inactive;
        }

        public Modifier(Type type, string name, bool constant,
            Action<Modifier, ModifierLoop> action,
            Action<Modifier, ModifierLoop> inactive,
            params string[] values) : this(type, name, constant, action, null, inactive, values) { }

        public Modifier(Type type, string name, bool constant,
            Func<Modifier, ModifierLoop, bool> trigger,
            Action<Modifier, ModifierLoop> inactive,
            params string[] values) : this(type, name, constant, null, trigger, inactive, values) { }

        public Modifier(ModifierCompatibility compatibility, Type type, string name, bool constant,
            Action<Modifier, ModifierLoop> action,
            Action<Modifier, ModifierLoop> inactive,
            params string[] values) : this(type, name, constant, action, null, inactive, values) => this.compatibility = compatibility;

        public Modifier(ModifierCompatibility compatibility, Type type, string name, bool constant,
            Func<Modifier, ModifierLoop, bool> trigger,
            Action<Modifier, ModifierLoop> inactive,
            params string[] values) : this(type, name, constant, null, trigger, inactive, values) => this.compatibility = compatibility;

        public Modifier(Type type, string name, bool constant,
            Action<Modifier, ModifierLoop> action,
            params string[] values) : this(type, name, constant, action, null, null, values) { }

        public Modifier(Type type, string name, bool constant,
            Func<Modifier, ModifierLoop, bool> trigger,
            params string[] values) : this(type, name, constant, null, trigger, null, values) { }

        public Modifier(ModifierCompatibility compatibility, Type type, string name, bool constant,
            Action<Modifier, ModifierLoop> action,
            params string[] values) : this(type, name, constant, action, null, null, values) => this.compatibility = compatibility;

        public Modifier(ModifierCompatibility compatibility, Type type, string name, bool constant,
            Func<Modifier, ModifierLoop, bool> trigger,
            params string[] values) : this(type, name, constant, null, trigger, null, values) => this.compatibility = compatibility;

        #endregion

        #region Values

        /// <summary>
        /// Compatability of the modifier between object types.
        /// </summary>
        public ModifierCompatibility compatibility = ModifierCompatibility.AllCompatible;

        /// <summary>
        /// Action to run per-tick.
        /// </summary>
        public Action<Modifier, ModifierLoop> Action { get; set; }

        /// <summary>
        /// Trigger to check if other modifiers should run.
        /// </summary>
        public Func<Modifier, ModifierLoop, bool> Trigger { get; set; }

        /// <summary>
        /// Inactive state.
        /// </summary>
        public Action<Modifier, ModifierLoop> Inactive { get; set; }

        public string name;
        /// <summary>
        /// Name of the modifier.
        /// </summary>
        public string Name => !string.IsNullOrEmpty(name) ? name : "Invalid Modifier";

        /// <summary>
        /// Function type.
        /// </summary>
        public enum Type { Trigger, Action }

        #region Settings

        /// <summary>
        /// If the modifier has been verified yet.
        /// </summary>
        public bool verified = false;

        /// <summary>
        /// If true, the modifier will run per-frame, otherwise it will only run once.
        /// </summary>
        public bool constant = true;

        /// <summary>
        /// If the modifier group functions should only target objects in the prefab the object was spawned from, if it is from a prefab.
        /// </summary>
        public bool prefabInstanceOnly = false;

        /// <summary>
        /// If the modifier group functions should only target alive objects.
        /// </summary>
        public bool groupAlive = false;

        /// <summary>
        /// If the modifier group functions should only target objects inside of the Prefab object, if the modifier is in one.
        /// </summary>
        public bool subPrefab = false;

        /// <summary>
        /// Function type of the modifier.
        /// </summary>
        public Type type = Type.Action;

        /// <summary>
        /// Values of the modifier.
        /// </summary>
        public List<string> values = new List<string>();

        #endregion

        #region Trigger Settings

        /// <summary>
        /// If the trigger result is the opposite.
        /// </summary>
        public bool not = false;

        /// <summary>
        /// If the trigger can check regardless of previous triggers.
        /// </summary>
        public bool elseIf;

        /// <summary>
        /// Max amount of times the modifier can run.
        /// </summary>
        public int triggerCount;

        #endregion

        #region Run States

        /// <summary>
        /// Result of the modifier.
        /// </summary>
        public object Result { get; set; }

        /// <summary>
        /// Timer result of the modifier.
        /// </summary>
        public float ResultTimer { get; set; }

        /// <summary>
        /// If the modifier is currently running.
        /// </summary>
        public bool running = false;

        /// <summary>
        /// If the modifier has triggered if the type is <see cref="Type.Trigger"/>.
        /// </summary>
        public bool triggered = false;

        /// <summary>
        /// If the modifier was activated once.
        /// </summary>
        public bool active = false;

        /// <summary>
        /// If the <see cref="ResultTimer"/> was set.
        /// </summary>
        public bool setTimer = false;

        /// <summary>
        /// How many times the modifier has ran.
        /// </summary>
        public int runCount = 0;

        /// <summary>
        /// If the modifier should run.
        /// </summary>
        public bool enabled = true;

        #endregion

        #region Editor

        /// <summary>
        /// If the modifier should be collapsed in the editor.
        /// </summary>
        public bool collapse = false;

        /// <summary>
        /// Name to display for the modifier in the editor.
        /// </summary>
        public string DisplayName => !string.IsNullOrEmpty(customName) ? customName : Name;

        /// <summary>
        /// Custom name to display for the modifier in the editor.
        /// </summary>
        public string customName = string.Empty;

        /// <summary>
        /// Description to display for the modifier in the editor.
        /// </summary>
        public string description = string.Empty;

        /// <summary>
        /// Modifier card reference.
        /// </summary>
        public ModifierCard card;

        #endregion

        #region Debug

        public static bool tryCatch;

        #endregion

        #endregion

        #region Functions

        public override void CopyData(Modifier orig, bool newID = true)
        {
            id = newID ? GetNumberID() : orig.id;
            name = orig.name;
            type = orig.type;
            values = new List<string>(orig.values);
            not = orig.not;
            elseIf = orig.elseIf;
            constant = orig.constant;
            triggerCount = orig.triggerCount;
            enabled = orig.enabled;

            prefabInstanceOnly = orig.prefabInstanceOnly;
            groupAlive = orig.groupAlive;
            subPrefab = orig.subPrefab;

            collapse = orig.collapse;
            customName = orig.customName;
            description = orig.description;

            Action = orig.Action;
            Trigger = orig.Trigger;
            Inactive = orig.Inactive;
        }

        public override void ReadJSON(JSONNode jn)
        {
            type = (Type)jn["type"].AsInt;

            if (type == Type.Trigger)
            {
                not = jn["not"].AsBool;
                elseIf = jn["else"].AsBool;
            }

            triggerCount = jn["count"].AsInt;
            constant = jn["const"].AsBool;
            prefabInstanceOnly = jn["po"].AsBool;
            groupAlive = jn["ga"].AsBool;
            subPrefab = jn["sub"].AsBool;
            if (jn["en"] != null)
                enabled = jn["en"].AsBool;

            collapse = jn["collapse"].AsBool;
            customName = jn["cn"] ?? string.Empty;
            description = jn["desc"] ?? string.Empty;

            values.Clear();
            if (jn["name"] != null)
            {
                name = jn["name"];

                if (jn["values"] != null)
                    for (int i = 0; i < jn["values"].Count; i++)
                        values.Add(jn["values"][i]);

                return;
            }

            name = jn["commands"].Count > 0 ? jn["commands"][0] : string.Empty;

            values.Add(string.IsNullOrEmpty("value") ? string.Empty : jn["value"]);
            for (int i = 1; i < jn["commands"].Count; i++)
                values.Add(((string)jn["commands"][i]).Replace("{{colon}}", ":"));
        }

        public override JSONNode ToJSON()
        {
            var jn = Parser.NewJSONObject();

            jn["type"] = (int)type;

            if (not)
                jn["not"] = not;

            if (elseIf)
                jn["else"] = elseIf;

            if (triggerCount > 0)
                jn["count"] = triggerCount;

            jn["name"] = Name;

            for (int i = 0; i < values.Count; i++)
                jn["values"][i] = GetValue(i);

            jn["const"] = constant;

            if (prefabInstanceOnly)
                jn["po"] = prefabInstanceOnly;
            if (groupAlive)
                jn["ga"] = groupAlive;
            if (subPrefab)
                jn["sub"] = subPrefab;
            if (!enabled)
                jn["en"] = enabled;

            if (collapse)
                jn["collapse"] = collapse;
            if (!string.IsNullOrEmpty(customName))
                jn["cn"] = customName;
            if (!string.IsNullOrEmpty(description))
                jn["desc"] = description;

            return jn;
        }

        public void ReadPacket(NetworkReader reader)
        {
            id = reader.ReadString();
            type = (Type)reader.ReadUInt16();
            if (type == Type.Trigger)
            {
                not = reader.ReadBoolean();
                elseIf = reader.ReadBoolean();
            }
            triggerCount = reader.ReadInt32();
            constant = reader.ReadBoolean();
            prefabInstanceOnly = reader.ReadBoolean();
            groupAlive = reader.ReadBoolean();
            subPrefab = reader.ReadBoolean();
            enabled = reader.ReadBoolean();

            if (ProjectArrhythmia.State.InEditor)
            {
                collapse = reader.ReadBoolean();
                customName = reader.ReadString();
                description = reader.ReadString();
            }

            name = reader.ReadString();
            var count = reader.ReadInt32();
            values.Clear();
            for (int i = 0; i < count; i++)
                values.Add(reader.ReadString());
        }

        public void WritePacket(NetworkWriter writer)
        {
            writer.Write(id ?? string.Empty);
            writer.Write((ushort)type);
            if (type == Type.Trigger)
            {
                writer.Write(not);
                writer.Write(elseIf);
            }
            writer.Write(triggerCount);
            writer.Write(constant);
            writer.Write(prefabInstanceOnly);
            writer.Write(groupAlive);
            writer.Write(subPrefab);
            writer.Write(enabled);

            if (ProjectArrhythmia.State.InEditor)
            {
                writer.Write(collapse);
                writer.Write(customName ?? string.Empty);
                writer.Write(description ?? string.Empty);
            }

            writer.Write(name ?? string.Empty);
            writer.Write(values.Count);
            for (int i = 0; i < values.Count; i++)
                writer.Write(values[i] ?? string.Empty);
        }

        public void VerifyModifier(List<Modifier> modifiers)
        {
            if (modifiers != null && modifiers.TryFind(x => x.Name == Name && x.type == type, out Modifier defaultModifier))
            {
                compatibility = defaultModifier.compatibility;

                int num = values.Count;
                while (values.Count < defaultModifier.values.Count)
                {
                    values.Add(defaultModifier.values[num]);
                    num++;
                }
            }
        }

        #region Run

        public void RunAction(Modifier modifier, ModifierLoop modifierLoop)
        {
            if (!tryCatch)
            {
                Action?.Invoke(modifier, modifierLoop);
                return;
            }
            try
            {
                Action?.Invoke(modifier, modifierLoop);
            }
            catch (Exception ex)
            {
                CoreHelper.LogError($"Encountered an exception with the modifier: {Name}.\nException: {ex}");
            }
        }
        
        public bool RunTrigger(Modifier modifier, ModifierLoop modifierLoop)
        {
            if (!tryCatch)
                return Trigger?.Invoke(modifier, modifierLoop) == true;
            try
            {
                return Trigger?.Invoke(modifier, modifierLoop) == true;
            }
            catch (Exception ex)
            {
                CoreHelper.LogError($"Encountered an exception with the modifier: {Name}.\nException: {ex}");
                return false;
            }
        }

        public void RunInactive(Modifier modifier, ModifierLoop modifierLoop)
        {
            if (!tryCatch)
            {
                Inactive?.Invoke(modifier, modifierLoop);
                return;
            }
            try
            {
                Inactive?.Invoke(modifier, modifierLoop);
            }
            catch (Exception ex)
            {
                CoreHelper.LogError($"Encountered an exception with the modifier: {Name}.\nException: {ex}");
            }
        }
        
        public void RunInactive(Modifier modifier, IModifierReference reference)
        {
            if (!tryCatch)
            {
                Inactive?.Invoke(modifier, new ModifierLoop(reference, null));
                return;
            }
            try
            {
                Inactive?.Invoke(modifier, new ModifierLoop(reference, null));
            }
            catch (Exception ex)
            {
                CoreHelper.LogError($"Encountered an exception with the modifier: {Name}.\nException: {ex}");
            }
        }
        
        public void RunInactive(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (!tryCatch)
            {
                Inactive?.Invoke(modifier, new ModifierLoop(reference, variables));
                return;
            }
            try
            {
                Inactive?.Invoke(modifier, new ModifierLoop(reference, variables));
            }
            catch (Exception ex)
            {
                CoreHelper.LogError($"Encountered an exception with the modifier: {Name}.\nException: {ex}");
            }
        }

        #endregion

        #region Result

        /// <summary>
        /// Gets the cached result of the modifier.
        /// </summary>
        /// <typeparam name="T">Type of the result.</typeparam>
        /// <returns>Returns the modifiers' cache.</returns>
        public T GetResult<T>() => (T)Result;

        /// <summary>
        /// Tries to get the cached result of the modifier.
        /// </summary>
        /// <typeparam name="T">Type of the result.</typeparam>
        /// <param name="result">Output cache.</param>
        /// <returns>Returns true if the result matches the type, otherwise returns false.</returns>
        public bool TryGetResult<T>(out T result)
        {
            if (Result is T r)
            {
                result = r;
                return true;
            }
            result = default;
            return false;
        }

        /// <summary>
        /// Gets the modifiers' cache.
        /// </summary>
        /// <typeparam name="T">Type of the result</typeparam>
        /// <param name="get">Get cache function.</param>
        /// <returns>Returns the modifiers' cache if the type matches, otherwise uses <paramref name="get"/> to get the cache and store it.</returns>
        public T GetResultOrDefault<T>(Func<T> get)
        {
            if (!TryGetResult(out T obj))
            {
                obj = get();
                Result = obj;
            }
            return obj;
        }

        /// <summary>
        /// If the modifier has a result.
        /// </summary>
        /// <returns>Returns true if the modifier has a result, otherwise returns false.</returns>
        public bool HasResult() => Result != null;

        #endregion

        #region Values

        /// <summary>
        /// Reference value indexes here since I was pretty stupid with how I implemented the value system initially.
        /// </summary>
        /// <param name="index">Index of the value.</param>
        /// <returns>Returns a value.</returns>
        public string GetValue(int index, Dictionary<string, string> variables = null)
        {
            if (index > 0 && !values.InRange(index))
                return string.Empty;

            var result = values[index];

            if (variables != null && variables.TryGetValue(result, out string variable))
                return variable;

            return result;
        }

        public T GetValue<T>(int index, T defaultValue)
        {
            var type = typeof(T);
            if (type == typeof(bool))
                return (T)(object)GetBool(index, (bool)(object)defaultValue);
            if (type == typeof(float))
                return (T)(object)GetFloat(index, (float)(object)defaultValue);
            if (type == typeof(int))
                return (T)(object)GetInt(index, (int)(object)defaultValue);
            if (type == typeof(string))
                return (T)(object)GetString(index, (string)(object)defaultValue);
            return default;
        }

        public bool GetBool(int index, bool defaultValue, Dictionary<string, string> variables = null)
        {
            if (!values.InRange(index))
                return defaultValue;

            return Parser.TryParse(GetValue(index, variables), defaultValue);
        }

        public float GetFloat(int index, float defaultValue, Dictionary<string, string> variables = null)
        {
            if (!values.InRange(index))
                return defaultValue;

            return Parser.TryParse(GetValue(index, variables), defaultValue);
        }

        public int GetInt(int index, int defaultValue, Dictionary<string, string> variables = null)
        {
            if (!values.InRange(index))
                return defaultValue;

            return Parser.TryParse(GetValue(index, variables), defaultValue);
        }

        public string GetString(int index, string defaultValue, Dictionary<string, string> variables = null)
        {
            if (!values.InRange(index))
                return defaultValue;

            return GetValue(index, variables);
        }

        public void SetValue(int index, string value)
        {
            if (index < values.Count)
                values[index] = value;
            else
                values.Add(value);
        }

        #endregion

        public override string ToString() => Name;

        #endregion
    }
}
