using BepInEx.Configuration;
using BetterLegacy.Core;
using BetterLegacy.Core.Helpers;
using LSFunctions;
using SimpleJSON;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace BetterLegacy.Configs
{
    public abstract class BaseConfig
    {
        public BaseConfig(string name)
        {
            Name = name;
        }

        public List<BaseSetting> Settings { get; set; } = new List<BaseSetting>();
        public JSONNode JSON { get; set; }
        public string Name { get; set; }
        public string FullPath => RTFile.ApplicationDirectory + "profile/" + Name.ToLower().Replace(" ", "_") + ".lsc";

        public Action SettingChanged { get; set; }

        public void OnSettingChanged(object instance)
        {
            if (instance is BaseSetting setting)
            {
                if (setting.BoxedValue is Color color)
                    JSON[setting.Section][setting.Key]["value"] = CoreHelper.ColorToHex(color);
                else if (setting.BoxedValue is Vector2 vector2)
                    JSON[setting.Section][setting.Key]["value"] = vector2.ToJSON();
                else if (setting.BoxedValue is Vector3 vector3)
                    JSON[setting.Section][setting.Key]["value"] = vector3.ToJSON();
                else if (setting.BoxedValue is Vector2Int vector2Int)
                    JSON[setting.Section][setting.Key]["value"] = vector2Int.ToJSON();
                else if (setting.BoxedValue is Vector3Int vector3Int)
                    JSON[setting.Section][setting.Key]["value"] = vector3Int.ToJSON();
                else
                    JSON[setting.Section][setting.Key]["value"] = setting.BoxedValue.ToString().Replace(":", "{{colon}}");
            }

            SettingChanged?.Invoke();

            Save();
        }

        public void Load() => JSON = SimpleJSON.JSON.Parse(RTFile.FileExists(FullPath) ? RTFile.ReadFromFile(FullPath) : "{}");

        public void Save() => RTFile.WriteToFile(FullPath, JSON.ToString());

        public abstract void BindSettings();

        public abstract void SetupSettingChanged();

        public Setting<T> Bind<T>(BaseConfig config, string section, string key, T defaultValue, string description, T minValue = default, T maxValue = default, Action settingChanged = null)
        {
            var setting = new Setting<T>(section, key, defaultValue, description, minValue, maxValue, settingChanged);

            if (JSON[section][key] == null)
            {
                if (defaultValue is Color color)
                    JSON[section][key]["value"] = CoreHelper.ColorToHex(color);
                else if (defaultValue is Vector2 vector2)
                    JSON[section][key]["value"] = vector2.ToJSON();
                else if (defaultValue is Vector3 vector3)
                    JSON[section][key]["value"] = vector3.ToJSON();
                else if (defaultValue is Vector2Int vector2Int)
                    JSON[section][key]["value"] = vector2Int.ToJSON();
                else if (defaultValue is Vector3Int vector3Int)
                    JSON[section][key]["value"] = vector3Int.ToJSON();
                else
                    JSON[section][key]["value"] = defaultValue.ToString().Replace(":", "{{colon}}");

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
                else if (defaultValue is string defaultString)
                    setting.BoxedValue = defaultString.Replace("{{colon}}", ":");

                setting.fireSettingChanged = true;
            }

            setting.Config = config;
            Settings.Add(setting);

            return setting;
        }

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

        public override string ToString() => Name;
    }

}
