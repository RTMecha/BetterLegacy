using System.Collections.Generic;

using BetterLegacy.Core.Data.Player;

namespace BetterLegacy.Core.Data.Beatmap
{
    public class ModifierBase : PAObject<ModifierBase>
    {
        public ModifierBase() => id = GetNumberID();

        /// <summary>
        /// Name of the modifier.
        /// </summary>
        public string Name => commands != null && !commands.IsEmpty() ? commands[0] : "Invalid Modifier";

        /// <summary>
        /// Function type.
        /// </summary>
        public enum Type { Trigger, Action }

        #region Modifier Values

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
        public bool groupAlive;

        /// <summary>
        /// If the modifier should be collapsed in the editor.
        /// </summary>
        public bool collapse;

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

        public bool hasChanged;
        public bool running = false;
        public bool triggered = false;
        public bool active = false;
        public bool setTimer = false;
        public int runCount;

        #endregion

        #region Methods

        public static ModifierReferenceType GetReferenceType<T>()
        {
            var type = typeof(T);
            if (type == typeof(BeatmapObject))
                return ModifierReferenceType.BeatmapObject;
            else if (type == typeof(BackgroundObject))
                return ModifierReferenceType.BackgroundObject;
            else if (type == typeof(PAPlayer))
                return ModifierReferenceType.PAPlayer;
            else if (type == typeof(GameData))
                return ModifierReferenceType.GameData;
            return ModifierReferenceType.Null;
        }

        public void VerifyModifier(List<ModifierBase> modifiers)
        {
            if (commands.IsEmpty())
                return;

            if (modifiers != null && modifiers.TryFind(x => x.Name == Name && x.type == type, out ModifierBase defaultModifier))
            {
                int num = commands.Count;
                while (commands.Count < defaultModifier.commands.Count)
                {
                    commands.Add(defaultModifier.commands[num]);
                    num++;
                }
            }
        }

        public override void CopyData(ModifierBase orig, bool newID = true)
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

            collapse = orig.collapse;
        }

        public T GetResult<T>() => (T)Result;

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

        public bool HasResult() => Result != null;

        /// <summary>
        /// Reference value indexes here since I was pretty stupid with how I implemented the value system initially.
        /// </summary>
        /// <param name="index">Index of the value.</param>
        /// <returns>Returns a value.</returns>
        public string GetValue(int index, Dictionary<string, string> variables = null)
        {
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
    }
}
