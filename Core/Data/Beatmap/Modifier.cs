using System;
using System.Collections.Generic;

using SimpleJSON;

using BetterLegacy.Core.Data.Player;
using BetterLegacy.Core.Helpers;

namespace BetterLegacy.Core.Data.Beatmap
{
    public class Modifier : PAObject<Modifier>
    {
        public Modifier() { }

        public Modifier(string name) : this()
        {
            commands = new List<string> { name };
        }

        public Modifier(Type type, string name, bool constant, params string[] values)
        {
            commands = new List<string> { name };
            this.type = type;
            this.constant = constant;
            value = values == null || values.IsEmpty() ? string.Empty : values[0];
            for (int i = 1; i < values.Length; i++)
                commands.Add(values[i]);
        }
        
        public Modifier(ModifierCompatibility compatibility, Type type, string name, bool constant, params string[] values) : this(type, name, constant, values)
        {
            this.compatibility = compatibility;
        }
        
        public Modifier(Type type, string name, bool constant,
            Action<Modifier, IModifierReference, Dictionary<string, string>> action,
            Func<Modifier, IModifierReference, Dictionary<string, string>, bool> trigger,
            Action<Modifier, IModifierReference, Dictionary<string, string>> inactive,
            params string[] values)
        {
            commands = new List<string> { name };
            this.type = type;
            this.constant = constant;
            value = values == null || values.IsEmpty() ? string.Empty : values[0];
            for (int i = 1; i < values.Length; i++)
                commands.Add(values[i]);

            Action = action;
            Trigger = trigger;
            Inactive = Inactive;
        }

        public Modifier(Type type, string name, bool constant,
            Action<Modifier, IModifierReference, Dictionary<string, string>> action,
            Action<Modifier, IModifierReference, Dictionary<string, string>> inactive,
            params string[] values) : this(type, name, constant, action, null, inactive, values) { }

        public Modifier(Type type, string name, bool constant,
            Func<Modifier, IModifierReference, Dictionary<string, string>, bool> trigger,
            Action<Modifier, IModifierReference, Dictionary<string, string>> inactive,
            params string[] values) : this(type, name, constant, null, trigger, inactive, values) { }

        public Modifier(ModifierCompatibility compatibility, Type type, string name, bool constant,
            Action<Modifier, IModifierReference, Dictionary<string, string>> action,
            Action<Modifier, IModifierReference, Dictionary<string, string>> inactive,
            params string[] values) : this(type, name, constant, action, null, inactive, values)
        {
            this.compatibility = compatibility;
        }

        public Modifier(ModifierCompatibility compatibility, Type type, string name, bool constant,
            Func<Modifier, IModifierReference, Dictionary<string, string>, bool> trigger,
            Action<Modifier, IModifierReference, Dictionary<string, string>> inactive,
            params string[] values) : this(type, name, constant, null, trigger, inactive, values)
        {
            this.compatibility = compatibility;
        }

        public Modifier(Type type, string name, bool constant,
            Action<Modifier, IModifierReference, Dictionary<string, string>> action,
            params string[] values) : this(type, name, constant, action, null, null, values) { }

        public Modifier(Type type, string name, bool constant,
            Func<Modifier, IModifierReference, Dictionary<string, string>, bool> trigger,
            params string[] values) : this(type, name, constant, null, trigger, null, values) { }

        public Modifier(ModifierCompatibility compatibility, Type type, string name, bool constant,
            Action<Modifier, IModifierReference, Dictionary<string, string>> action,
            params string[] values) : this(type, name, constant, action, null, null, values)
        {
            this.compatibility = compatibility;
        }

        public Modifier(ModifierCompatibility compatibility, Type type, string name, bool constant,
            Func<Modifier, IModifierReference, Dictionary<string, string>, bool> trigger,
            params string[] values) : this(type, name, constant, null, trigger, null, values)
        {
            this.compatibility = compatibility;
        }

        #region Values

        /// <summary>
        /// Compatability of the modifier between object types.
        /// </summary>
        public ModifierCompatibility compatibility = ModifierCompatibility.AllCompatible;

        /// <summary>
        /// Action to run per-tick.
        /// </summary>
        public Action<Modifier, IModifierReference, Dictionary<string, string>> Action { get; set; }

        /// <summary>
        /// Trigger to check if other modifiers should run.
        /// </summary>
        public Func<Modifier, IModifierReference, Dictionary<string, string>, bool> Trigger { get; set; }

        /// <summary>
        /// Inactive state.
        /// </summary>
        public Action<Modifier, IModifierReference, Dictionary<string, string>> Inactive { get; set; }

        /// <summary>
        /// Name of the modifier.
        /// </summary>
        public string Name => commands != null && !commands.IsEmpty() ? commands[0] : "Invalid Modifier";

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
        /// If the modifier should be collapsed in the editor.
        /// </summary>
        public bool collapse = false;

        /// <summary>
        /// Function type of the modifier.
        /// </summary>
        public Type type = Type.Action;

        /// <summary>
        /// Main value of the modifier.
        /// </summary>
        public string value;

        /// <summary>
        /// Extra values.
        /// </summary>
        public List<string> commands = new List<string> { "" };

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

        #endregion

        #region Debug

        public static bool tryCatch;

        #endregion

        #endregion

        #region Methods

        public override void CopyData(Modifier orig, bool newID = true)
        {
            id = newID ? GetNumberID() : orig.id;
            type = orig.type;
            commands = orig.commands.Clone();
            value = orig.value;
            not = orig.not;
            elseIf = orig.elseIf;
            constant = orig.constant;
            triggerCount = orig.triggerCount;

            prefabInstanceOnly = orig.prefabInstanceOnly;
            groupAlive = orig.groupAlive;
            subPrefab = orig.subPrefab;

            collapse = orig.collapse;

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

            collapse = jn["collapse"].AsBool;

            commands.Clear();
            if (jn["name"] != null)
            {
                commands.Add(jn["name"]);

                if (jn["values"] != null)
                {
                    value = jn["values"][0];
                    for (int i = 1; i < jn["values"].Count; i++)
                        commands.Add(jn["values"][i]);
                }

                return;
            }

            for (int i = 0; i < jn["commands"].Count; i++)
                commands.Add(((string)jn["commands"][i]).Replace("{{colon}}", ":"));

            value = string.IsNullOrEmpty(jn["value"]) ? string.Empty : jn["value"];
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

            for (int i = 0; i < commands.Count; i++)
                jn["values"][i] = GetValue(i);

            jn["const"] = constant;

            if (prefabInstanceOnly)
                jn["po"] = prefabInstanceOnly;
            if (groupAlive)
                jn["ga"] = groupAlive;
            if (subPrefab)
                jn["sub"] = subPrefab;

            if (collapse)
                jn["collapse"] = collapse;

            return jn;
        }

        public void VerifyModifier(List<Modifier> modifiers)
        {
            if (commands.IsEmpty())
                return;

            if (modifiers != null && modifiers.TryFind(x => x.Name == Name && x.type == type, out Modifier defaultModifier))
            {
                compatibility = defaultModifier.compatibility;

                int num = commands.Count;
                while (commands.Count < defaultModifier.commands.Count)
                {
                    commands.Add(defaultModifier.commands[num]);
                    num++;
                }
            }
        }

        #region Run

        public void RunAction(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (!tryCatch)
            {
                Action?.Invoke(modifier, reference, variables);
                return;
            }
            try
            {
                Action?.Invoke(modifier, reference, variables);
            }
            catch (Exception ex)
            {
                CoreHelper.LogError($"Encountered an exception with the modifier: {Name}.\nException: {ex}");
            }
        }
        
        public bool RunTrigger(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (!tryCatch)
                return Trigger?.Invoke(modifier, reference, variables) == true;
            try
            {
                return Trigger?.Invoke(modifier, reference, variables) == true;
            }
            catch (Exception ex)
            {
                CoreHelper.LogError($"Encountered an exception with the modifier: {Name}.\nException: {ex}");
                return false;
            }
        }

        public void RunInactive(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (!tryCatch)
            {
                Inactive?.Invoke(modifier, reference, variables);
                return;
            }
            try
            {
                Inactive?.Invoke(modifier, reference, variables);
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
            if (index > 0 && !commands.InRange(index))
                return string.Empty;

            var result = index == 0 ? value : commands[index];

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
            if (!commands.InRange(index))
                return defaultValue;

            return Parser.TryParse(GetValue(index, variables), defaultValue);
        }

        public float GetFloat(int index, float defaultValue, Dictionary<string, string> variables = null)
        {
            if (!commands.InRange(index))
                return defaultValue;

            return Parser.TryParse(GetValue(index, variables), defaultValue);
        }

        public int GetInt(int index, int defaultValue, Dictionary<string, string> variables = null)
        {
            if (!commands.InRange(index))
                return defaultValue;

            return Parser.TryParse(GetValue(index, variables), defaultValue);
        }

        public string GetString(int index, string defaultValue, Dictionary<string, string> variables = null)
        {
            if (!commands.InRange(index))
                return defaultValue;

            return GetValue(index, variables);
        }

        public void SetValue(int index, string value)
        {
            if (index == 0)
                this.value = value;
            else if (index < commands.Count)
                commands[index] = value;
            else
                commands.Add(value);
        }

        #endregion

        public override string ToString() => Name;

        #endregion
    }
}
