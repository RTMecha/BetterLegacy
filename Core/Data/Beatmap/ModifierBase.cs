using System.Collections.Generic;

namespace BetterLegacy.Core.Data.Beatmap
{
    public class ModifierBase
    {
        public ModifierBase() { }

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

        public bool not = false;

        public bool elseIf;

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

        #endregion

        #region Methods

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
        public string GetValue(int index) => index == 0 ? value : commands[index];

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

        public bool GetBool(int index, bool defaultValue)
        {
            if (!commands.InRange(index))
                return defaultValue;

            return Parser.TryParse(GetValue(index), defaultValue);
        }
        
        public float GetFloat(int index, float defaultValue)
        {
            if (!commands.InRange(index))
                return defaultValue;

            return Parser.TryParse(GetValue(index), defaultValue);
        }
        
        public int GetInt(int index, int defaultValue)
        {
            if (!commands.InRange(index))
                return defaultValue;

            return Parser.TryParse(GetValue(index), defaultValue);
        }

        public string GetString(int index, string defaultValue)
        {
            if (!commands.InRange(index))
                return defaultValue;

            return GetValue(index);
        }

        public void SetValue(int index, string value)
        {
            if (index == 0)
                this.value = value;
            else if (index < commands.Count)
                commands[index] = value;
        }

        #endregion
    }
}
