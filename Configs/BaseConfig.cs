using System;
using System.Collections.Generic;

using UnityEngine;

using LSFunctions;

using SimpleJSON;

using BetterLegacy.Core;
using BetterLegacy.Core.Helpers;

namespace BetterLegacy.Configs
{
    /// <summary>
    /// The base for all configs BetterLegacy uses.
    /// </summary>
    public abstract class BaseConfig
    {
        public BaseConfig(string name) => Name = name;

        #region Properties

        /// <summary>
        /// Settings within the config.
        /// </summary>
        public List<BaseSetting> Settings { get; set; } = new List<BaseSetting>();

        /// <summary>
        /// Writeable JSON for the config.
        /// </summary>
        public JSONNode JSON { get; set; }

        /// <summary>
        /// Name of the config.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Full path to the config.
        /// </summary>
        public string FullPath => RTFile.CombinePaths(RTFile.ApplicationDirectory, "profile", RTFile.FormatLegacyFileName(Name).Replace("config", "") + FileFormat.LSC.Dot());

        /// <summary>
        /// Name of the tab in the config manager UI.
        /// </summary>
        public abstract string TabName { get; }

        /// <summary>
        /// Color of the tab in the config manager UI.
        /// </summary>
        public abstract Color TabColor { get; }

        /// <summary>
        /// Description of the tab in the config manager UI.
        /// </summary>
        public abstract string TabDesc { get; }

        /// <summary>
        /// Function to run when a setting is changed.
        /// </summary>
        public Action SettingChanged { get; set; }

        #endregion

        #region Methods

        #region Read / Write

        /// <summary>
        /// Loads the config JSON.
        /// </summary>
        public void Load() => JSON = SimpleJSON.JSON.Parse(RTFile.FileExists(FullPath) ? RTFile.ReadFromFile(FullPath) : "{}");

        /// <summary>
        /// Saves the config JSON.
        /// </summary>
        public void Save() => RTFile.WriteToFile(FullPath, JSON.ToString());

        /// <summary>
        /// Overwrites the current JSON, in case there are any old settings that need to be removed.
        /// </summary>
        public void OverwriteJSON()
        {
            JSON = SimpleJSON.JSON.Parse("{}");
            var settings = Settings;
            for (int i = 0; i < settings.Count; i++)
            {
                var setting = settings[i];
                WriteToJSON(setting.Section, setting.Key, setting.BoxedValue);
            }
        }

        /// <summary>
        /// Writes to the config JSON.
        /// </summary>
        /// <param name="section">Section of the setting.</param>
        /// <param name="key">Key of the setting.</param>
        /// <param name="value">Value to write.</param>
        public void WriteToJSON(string section, string key, object value)
        {
            if (value is Color color)
                JSON[section][key]["value"] = RTColors.ColorToHex(color);
            else if (value is Vector2 vector2)
                JSON[section][key]["value"] = vector2.ToJSON();
            else if (value is Vector3 vector3)
                JSON[section][key]["value"] = vector3.ToJSON();
            else if (value is Vector2Int vector2Int)
                JSON[section][key]["value"] = vector2Int.ToJSON();
            else if (value is Vector3Int vector3Int)
                JSON[section][key]["value"] = vector3Int.ToJSON();
            else
                JSON[section][key]["value"] = value.ToString().Replace(":", "{{colon}}");
        }

        #endregion

        #region Bind

        /// <summary>
        /// Binds a setting to the JSON file.
        /// </summary>
        /// <typeparam name="T">The type of <see cref="Setting{T}"/>.</typeparam>
        /// <param name="config">The config instance.</param>
        /// <param name="section">Section of the setting.</param>
        /// <param name="key">Key of the setting.</param>
        /// <param name="defaultValue">Default value of the setting.</param>
        /// <param name="description">Description of the setting.</param>
        /// <param name="minValue">Minimum value the setting can have.</param>
        /// <param name="maxValue">Maximum value the setting can have.</param>
        /// <param name="settingChanged">Function to run when setting is changed.</param>
        /// <returns>Returns a bound setting.</returns>
        public Setting<T> Bind<T>(BaseConfig config, string section, string key, T defaultValue, string description, T minValue = default, T maxValue = default, Action settingChanged = null)
        {
            var setting = new Setting<T>(section, key, defaultValue, description, minValue, maxValue, settingChanged);

            if (JSON[section][key] == null)
            {
                WriteToJSON(section, key, defaultValue);

                setting.Value = defaultValue;
                setting.fireSettingChanged = true;
            }
            else
            {
                if (defaultValue is bool defaultBool)
                    setting.BoxedValue = Parser.TryParse(JSON[section][key]["value"], defaultBool);
                else if (defaultValue is int defaultInt)
                    setting.BoxedValue = Parser.TryParse(JSON[section][key]["value"], defaultInt);
                else if (defaultValue is float defaultFloat)
                    setting.BoxedValue = Parser.TryParse(JSON[section][key]["value"], defaultFloat);
                else if (defaultValue is Vector2 defaultVector2)
                    setting.BoxedValue = Parser.TryParse(JSON[section][key]["value"], defaultVector2);
                else if (defaultValue is Vector2Int defaultVector2Int)
                    setting.BoxedValue = Parser.TryParse(JSON[section][key]["value"], defaultVector2Int);
                else if (defaultValue is Vector3 defaultVector3)
                    setting.BoxedValue = Parser.TryParse(JSON[section][key]["value"], defaultVector3);
                else if (defaultValue is Vector3Int defaultVector3Int)
                    setting.BoxedValue = Parser.TryParse(JSON[section][key]["value"], defaultVector3Int);
                else if (defaultValue is Color defaultColor)
                    setting.BoxedValue = ((string)JSON[section][key]["value"]).Length == 8 ? LSColors.HexToColorAlpha(JSON[section][key]["value"]) : ((string)JSON[section][key]["value"]).Length == 6 ? LSColors.HexToColor(JSON[section][key]["value"]) : defaultColor;
                else if (defaultValue is string)
                    setting.BoxedValue = ((string)JSON[section][key]["value"]).Replace("{{colon}}", ":");

                setting.fireSettingChanged = true;
            }

            setting.Config = config;
            Settings.Add(setting);

            return setting;
        }

        /// <summary>
        /// Binds a setting to the JSON file.
        /// </summary>
        /// <typeparam name="T">The type of <see cref="Setting{T}"/>.</typeparam>
        /// <param name="config">The config instance.</param>
        /// <param name="section">Section of the setting.</param>
        /// <param name="key">Key of the setting.</param>
        /// <param name="defaultValue">Default value of the setting.</param>
        /// <param name="description">Description of the setting.</param>
        /// <param name="settingChanged">Function to run when setting is changed.</param>
        /// <returns>Returns a bound setting.</returns>
        public Setting<T> BindEnum<T>(BaseConfig config, string section, string key, T defaultValue, string description, Action settingChanged = null) where T : struct
        {
            var setting = new Setting<T>(section, key, defaultValue, description, default, default, settingChanged);

            if (JSON[section][key] == null)
            {
                JSON[section][key]["value"] = defaultValue.ToString();
                setting.Value = defaultValue;
                setting.fireSettingChanged = true;
            }
            else
            {
                setting.BoxedValue = Parser.TryParse(JSON[section][key]["value"], defaultValue);

                setting.fireSettingChanged = true;
            }

            setting.Config = config;
            Settings.Add(setting);

            return setting;
        }

        /// <summary>
        /// Binds the settings to the JSON file.
        /// </summary>
        public abstract void BindSettings();

        #endregion

        #region Setting Changed

        /// <summary>
        /// Runs when a setting is changed.
        /// </summary>
        /// <param name="instance">The setting.</param>
        public void OnSettingChanged(object instance)
        {
            if (instance is BaseSetting setting)
                WriteToJSON(setting.Section, setting.Key, setting.BoxedValue);

            SettingChanged?.Invoke();

            Save();
        }

        /// <summary>
        /// Setting changed setup.
        /// </summary>
        public abstract void SetupSettingChanged();

        #endregion

        public override string ToString() => Name;

        #endregion
    }

}
