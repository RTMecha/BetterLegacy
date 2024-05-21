﻿using BepInEx.Configuration;
using BetterLegacy.Core;
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
                JSON[setting.Section][setting.Key]["value"] = setting.BoxedValue.ToString().Replace(":", "{{colon}}");

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
                    setting.BoxedValue = new Vector2(Parser.TryParse(JSON[section][key]["value"]["x"], defaultVector2.x), Parser.TryParse(JSON[section][key]["value"]["y"], defaultVector2.y));
                else if (defaultValue is Vector2Int defaultVector2Int)
                    setting.BoxedValue = new Vector2(Parser.TryParse(JSON[section][key]["value"]["x"], defaultVector2Int.x), Parser.TryParse(JSON[section][key]["value"]["y"], defaultVector2Int.y));
                else if (defaultValue is Vector3 defaultVector3)
                    setting.BoxedValue = new Vector3(Parser.TryParse(JSON[section][key]["value"]["x"], defaultVector3.x), Parser.TryParse(JSON[section][key]["value"]["y"], defaultVector3.y), Parser.TryParse(JSON[section][key]["value"]["z"], defaultVector3.z));
                else if (defaultValue is Vector3Int defaultVector3Int)
                    setting.BoxedValue = new Vector3(Parser.TryParse(JSON[section][key]["value"]["x"], defaultVector3Int.x), Parser.TryParse(JSON[section][key]["value"]["y"], defaultVector3Int.y), Parser.TryParse(JSON[section][key]["value"]["z"], defaultVector3Int.z));
                else if (defaultValue is Color defaultColor)
                    setting.BoxedValue = JSON[section][key]["value"].ToString().Length == 8 ? LSColors.HexToColorAlpha(JSON[section][key]["value"]) : JSON[section][key]["value"].ToString().Length == 6 ? LSColors.HexToColor(JSON[section][key]["value"]) : defaultColor;
                else if (defaultValue is string defaultString)
                    setting.BoxedValue = ((string)JSON[section][key]["value"]).Replace("{{colon}}", ":");

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
