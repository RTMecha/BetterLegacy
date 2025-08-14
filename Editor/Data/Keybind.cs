using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

using SimpleJSON;

using BetterLegacy.Core;
using BetterLegacy.Core.Data;
using BetterLegacy.Editor.Managers;

namespace BetterLegacy.Editor.Data
{
    public class Keybind : PAObject<Keybind>
    {
        public Keybind() => id = GetNumberID();

        public Keybind(string name, params Key[] keys) : this()
        {
            Name = name;
            this.keys = keys.ToList();
        }

        public Keybind(string name, List<Setting> settings, params Key[] keys) : this(name, keys) => this.settings = settings;

        #region Values

        public List<Setting> settings = new List<Setting>();
        public KeyCode KeyCode { get; set; }
        public KeyCode KeyCodeDown { get; set; }

        public int ActionType
        {
            get => KeybindEditor.inst && KeybindEditor.inst.keybindFunctions.TryFindIndex(x => x.name == Name, out int index) ? index : -1;
            set
            {
                if (!KeybindEditor.inst || !KeybindEditor.inst.keybindFunctions.TryGetAt(value, out KeybindFunction keybindFunction))
                {
                    Name = string.Empty;
                    action = null;
                    settings = new List<Setting>();
                    return;
                }

                Name = keybindFunction.name;
                action = keybindFunction.action;
                settings = new List<Setting>(keybindFunction.settings.Select(x => x.Copy()));
            }
        }

        Action<Keybind> action;

        string name = string.Empty;
        public string Name
        {
            get => name;
            set
            {
                name = value;
                if (string.IsNullOrEmpty(name))
                    return;

                if (!KeybindEditor.inst || !KeybindEditor.inst.keybindFunctions.TryFind(x => x.name == name, out KeybindFunction keybindFunction))
                {
                    action = null;
                    settings = new List<Setting>();
                    return;
                }

                action = keybindFunction.action;
                settings = keybindFunction.settings == null ? new List<Setting>() : new List<Setting>(keybindFunction.settings.Select(x => x.Copy()));
            }
        }

        /// <summary>
        /// List of keys to check.
        /// </summary>
        public List<Key> keys = new List<Key>();

        #endregion

        #region Methods

        public override void CopyData(Keybind orig, bool newID = true)
        {
            id = newID ? GetNumberID() : orig.id;

            Name = orig.Name;
            keys = new List<Key>(orig.keys.Select(x => x.Copy()));
            settings = new List<Setting>(orig.settings.Select(x => x.Copy()));
        }

        public override void ReadJSON(JSONNode jn)
        {
            id = jn["id"];

            Name = jn["name"] ?? string.Empty;
            keys = new List<Key>();
            for (int i = 0; i < jn["keys"].Count; i++)
                keys.Add(Key.Parse(jn["keys"][i]));

            for (int i = 0; i < jn["settings"].Count; i++)
            {
                var setting = Setting.Parse(jn["settings"][i]);
                settings.OverwriteAdd((x, index) => x.key == setting.key, setting);
            }
        }

        public override JSONNode ToJSON()
        {
            var jn = Parser.NewJSONObject();

            jn["id"] = id ?? GetNumberID();
            jn["name"] = Name ?? string.Empty;
            for (int i = 0; i < keys.Count; i++)
                jn["keys"][i] = keys[i].ToJSON();
            for (int i = 0; i < settings.Count; i++)
                jn["settings"][i] = settings[i].ToJSON();

            return jn;
        }

        /// <summary>
        /// Checks if the keybind is able to activate.
        /// </summary>
        /// <returns>Returns true if the keybind can activate, otherwise returns false.</returns>
        public bool Check() => !keys.IsEmpty() && keys.All(x => x.Check());

        /// <summary>
        /// Runs the keybind function.
        /// </summary>
        public void Activate() => action?.Invoke(this);

        /// <summary>
        /// Tries to a setting from the keybind.
        /// </summary>
        /// <param name="key">Key of the setting.</param>
        /// <param name="setting">Setting result.</param>
        /// <returns>Returns true if a setting was found, otherwise returns false.</returns>
        public bool TryGetSetting(string key, out Setting setting) => settings.TryFind(x => x.key == key, out setting);

        /// <summary>
        /// Tries to a settings' value from the keybind.
        /// </summary>
        /// <param name="key">Key of the setting.</param>
        /// <param name="value">Setting result.</param>
        /// <returns>Returns true if a setting was found, otherwise returns false.</returns>
        public bool TryGetSetting(string key, out string value)
        {
            if (settings.TryFind(x => x.key == key, out Setting setting))
            {
                value = setting.value;
                return true;
            }
            value = default;
            return false;
        }

        /// <summary>
        /// Gets a settings' value if the setting exists, otherwise gets a default value.
        /// </summary>
        /// <param name="key">Key of the setting.</param>
        /// <param name="defaultValue">Default value to return if no setting is found.</param>
        /// <returns>Returns the setting value if a setting is found, otherwise returns the default value.</returns>
        public string GetSettingOrDefault(string key, string defaultValue) => TryGetSetting(key, out string value) ? value : defaultValue;

        /// <summary>
        /// Gets a settings' value if the setting exists, otherwise gets a default value.
        /// </summary>
        /// <param name="key">Key of the setting.</param>
        /// <param name="defaultValue">Default value to return if no setting is found.</param>
        /// <returns>Returns the setting value if a setting is found, otherwise returns the default value.</returns>
        public float GetSettingOrDefault(string key, float defaultValue) => Parser.TryParse(GetSettingOrDefault(key, defaultValue.ToString()), defaultValue);

        /// <summary>
        /// Gets a settings' value if the setting exists, otherwise gets a default value.
        /// </summary>
        /// <param name="key">Key of the setting.</param>
        /// <param name="defaultValue">Default value to return if no setting is found.</param>
        /// <returns>Returns the setting value if a setting is found, otherwise returns the default value.</returns>
        public int GetSettingOrDefault(string key, int defaultValue) => Parser.TryParse(GetSettingOrDefault(key, defaultValue.ToString()), defaultValue);

        /// <summary>
        /// Gets a settings' value if the setting exists, otherwise gets a default value.
        /// </summary>
        /// <param name="key">Key of the setting.</param>
        /// <param name="defaultValue">Default value to return if no setting is found.</param>
        /// <returns>Returns the setting value if a setting is found, otherwise returns the default value.</returns>
        public bool GetSettingOrDefault(string key, bool defaultValue) => Parser.TryParse(GetSettingOrDefault(key, defaultValue.ToString()), defaultValue);

        public override string ToString() => Name;

        #endregion

        public class Key : PAObject<Key>
        {
            public Key() { }

            public Key(Type type, KeyCode keyCode)
            {
                InteractType = type;
                KeyCode = keyCode;
            }

            public enum Type
            {
                Down,
                Pressed,
                Up,
                NotPressed
            }

            public Type InteractType { get; set; }
            public KeyCode KeyCode { get; set; }

            public override void CopyData(Key orig, bool newID = true)
            {
                InteractType = orig.InteractType;
                KeyCode = orig.KeyCode;
            }

            public override void ReadJSON(JSONNode jn)
            {
                InteractType = (Type)jn["type"].AsInt;
                KeyCode = (KeyCode)jn["key"].AsInt;
            }

            public override JSONNode ToJSON()
            {
                var jn = Parser.NewJSONObject();

                jn["type"] = (int)InteractType;
                jn["key"] = (int)KeyCode;

                return jn;
            }

            public bool Check() => InteractType switch
            {
                Type.Down => Input.GetKeyDown(KeyCode),
                Type.Pressed => Input.GetKey(KeyCode),
                Type.Up => Input.GetKeyUp(KeyCode),
                Type.NotPressed => !Input.GetKey(KeyCode),
                _ => false,
            };
        }

        public class Setting : PAObject<Setting>
        {
            public Setting() { }

            public Setting(string key, string value)
            {
                this.key = key;
                this.value = value;
            }

            public string key = string.Empty;
            public string value = string.Empty;

            public override void CopyData(Setting orig, bool newID = true)
            {
                key = orig.key;
                value = orig.value;
            }

            public override void ReadJSON(JSONNode jn)
            {
                key = jn["type"];
                value = jn["value"] ?? string.Empty;
            }

            public override JSONNode ToJSON()
            {
                var jn = Parser.NewJSONObject();

                jn["type"] = key;
                jn["value"] = value ?? string.Empty;

                return jn;
            }
        }
    }
}
