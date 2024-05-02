using BepInEx.Configuration;
using BetterLegacy.Example;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace BetterLegacy.Configs
{
    public class ExampleConfig : BaseConfig
    {
        public static ExampleConfig Instance { get; set; }
        public override ConfigFile Config { get; set; }

        public ExampleConfig(ConfigFile config) : base(config)
        {
            Instance = this;
            Config = config;

            ExampleSpawns = Config.Bind("Spawning", "Enabled", true);
            ExampleVisibility = Config.Bind("Visibility", "Amount", 0.5f);
            ExampleVisible = Config.Bind("Visibility", "Enabled", false);
            ExampleVisiblityToggle = Config.Bind("Visibility", "Toggle KeyCode", KeyCode.O);

            EnabledInArcade = Config.Bind("Visibility", "In Arcade", false, "Includes Editor Preview.");
            EnabledInEditor = Config.Bind("Visibility", "In Editor", true);
            EnabledInMenus = Config.Bind("Visibility", "In Menus", false);

            SetupSettingChanged();
        }

        #region Configs

        public ConfigEntry<bool> ExampleSpawns { get; set; }
        public ConfigEntry<float> ExampleVisibility { get; set; }
        public ConfigEntry<bool> ExampleVisible { get; set; }

        public ConfigEntry<KeyCode> ExampleVisiblityToggle { get; set; }

        public ConfigEntry<bool> EnabledInEditor { get; set; }
        public ConfigEntry<bool> EnabledInMenus { get; set; }
        public ConfigEntry<bool> EnabledInArcade { get; set; }

        #endregion

        public override void SetupSettingChanged()
        {
            Config.SettingChanged += new EventHandler<SettingChangedEventArgs>(UpdateSettings);
        }

        void UpdateSettings(object sender, EventArgs e)
        {
            if (!ExampleManager.inst && ExampleSpawns.Value)
                ExampleManager.Init();
        }
    }
}
