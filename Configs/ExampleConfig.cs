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
    /// <summary>
    /// Example Config for PA Legacy. Based on the ExampleCompanion mod.
    /// </summary>
    public class ExampleConfig : BaseConfig
    {
        public static ExampleConfig Instance { get; set; }
        public override ConfigFile Config { get; set; }

        public ExampleConfig(ConfigFile config) : base(config)
        {
            Instance = this;
            Config = config;

            ExampleSpawns = Config.Bind("Example - General", "Spawns", true, "If Example should spawn.");
            ExampleSpeaks = Config.Bind("Example - General", "Speaks", true, "If Example can talk.");

            ExampleVisible = Config.Bind("Example - Visibility", "Set Opacity", false, "If Example becomes transparent.");
            ExampleVisibility = Config.Bind("Example - Visibility", "Amount", 0.5f, "The opacity of Example if visibility is turned off.");
            ExampleVisiblityToggle = Config.Bind("Example - Visibility", "Toggle KeyCode", KeyCode.O, "The key to press to make Example become transparent.");

            EnabledInGame = Config.Bind("Example - Visibility", "In Game", false, "If Example is enabled in game. Includes Editor Preview.");
            EnabledInEditor = Config.Bind("Example - Visibility", "In Editor", true, "If Example is enabled in editor.");
            EnabledInMenus = Config.Bind("Example - Visibility", "In Menus", false, "If Example is enabled in menus.");

            SetupSettingChanged();
        }

        #region Configs

        #region General

        /// <summary>
        /// If Example should spawn.
        /// </summary>
        public ConfigEntry<bool> ExampleSpawns { get; set; }

        /// <summary>
        /// If Example should spawn.
        /// </summary>
        public ConfigEntry<bool> ExampleSpeaks { get; set; }

        #endregion

        #region Visibility

        /// <summary>
        /// If Example becomes transparent.
        /// </summary>
        public ConfigEntry<bool> ExampleVisible { get; set; }

        /// <summary>
        /// The opacity of Example if visibility is turned off.
        /// </summary>
        public ConfigEntry<float> ExampleVisibility { get; set; }

        /// <summary>
        /// The key to press to make Example become transparent.
        /// </summary>
        public ConfigEntry<KeyCode> ExampleVisiblityToggle { get; set; }

        /// <summary>
        /// If Example is enabled in game. Includes Editor Preview.
        /// </summary>
        public ConfigEntry<bool> EnabledInGame { get; set; }

        /// <summary>
        /// If Example is enabled in editor.
        /// </summary>
        public ConfigEntry<bool> EnabledInEditor { get; set; }

        /// <summary>
        /// If Example is enabled in menus.
        /// </summary>
        public ConfigEntry<bool> EnabledInMenus { get; set; }

        #endregion

        #endregion

        public override void SetupSettingChanged()
        {
            ExampleSpawns.SettingChanged += ExampleSpawnsChanged;
        }

        void ExampleSpawnsChanged(object sender, EventArgs e)
        {
            if (!ExampleManager.inst && ExampleSpawns.Value)
                ExampleManager.Init();
        }
    }
}
