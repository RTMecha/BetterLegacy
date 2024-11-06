using System.Collections.Generic;

namespace BetterLegacy.Core.Data
{
    public class ModifierBase
    {
        public ModifierBase() { }

        public bool verified = false;

        public bool constant = true;

        public bool prefabInstanceOnly = false;

        public enum Type { Trigger, Action }

        public Type type = Type.Action;
        public string value;
        public bool running = false;
        public bool active = false;
        public List<string> commands = new List<string> { "" };
        public string Name => commands != null && commands.Count > 0 ? commands[0] : "Null";

        public bool not = false;

        public object Result { get; set; }
        public float ResultTimer { get; set; }

        public bool hasChanged;
        public bool elseIf;

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

        /// <summary>
        /// Reference value indexes here since I was pretty stupid with how I implemented the value system initially.
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
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
            if (index < 0 || index >= commands.Count)
                return defaultValue;

            return Parser.TryParse(GetValue(index), defaultValue);
        }
        
        public float GetFloat(int index, float defaultValue)
        {
            if (index < 0 || index >= commands.Count)
                return defaultValue;

            return Parser.TryParse(GetValue(index), defaultValue);
        }
        
        public int GetInt(int index, int defaultValue)
        {
            if (index < 0 || index >= commands.Count)
                return defaultValue;

            return Parser.TryParse(GetValue(index), defaultValue);
        }
        
        public string GetString(int index, string defaultValue)
        {
            if (index < 0 || index >= commands.Count)
                return defaultValue;

            return GetValue(index);
        }

        #endregion
    }
}
