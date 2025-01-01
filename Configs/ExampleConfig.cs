using UnityEngine;

namespace BetterLegacy.Configs
{
    /// <summary>
    /// Example Config for PA Legacy. Based on the ExampleCompanion mod.
    /// </summary>
    public class ExampleConfig : BaseConfig
    {
        public static ExampleConfig Instance { get; set; }

        public ExampleConfig() : base(nameof(ExampleConfig)) // Set config name via base("")
        {
            Instance = this;
            BindSettings();

            SetupSettingChanged();
        }

        #region Settings

        #region General

        /// <summary>
        /// If Example should spawn.
        /// </summary>
        public Setting<bool> ExampleSpawns { get; set; }

        /// <summary>
        /// If Example should spawn.
        /// </summary>
        public Setting<bool> ExampleSpeaks { get; set; }

        #endregion

        #region Visibility

        /// <summary>
        /// If Example becomes transparent.
        /// </summary>
        public Setting<bool> ExampleVisible { get; set; }

        /// <summary>
        /// The opacity of Example if visibility is turned off.
        /// </summary>
        public Setting<float> ExampleVisibility { get; set; }

        /// <summary>
        /// The key to press to make Example become transparent.
        /// </summary>
        public Setting<KeyCode> ExampleVisiblityToggle { get; set; }

        /// <summary>
        /// If Example is enabled in game. Includes Editor Preview.
        /// </summary>
        public Setting<bool> EnabledInGame { get; set; }

        /// <summary>
        /// If Example is enabled in editor.
        /// </summary>
        public Setting<bool> EnabledInEditor { get; set; }

        /// <summary>
        /// If Example is enabled in menus.
        /// </summary>
        public Setting<bool> EnabledInMenus { get; set; }

        #endregion

        #endregion

        /// <summary>
        /// Bind the individual settings of the config.
        /// </summary>
        public override void BindSettings()
        {
            Load();

            ExampleSpawns = Bind(this, "General", "Spawns", true, "If Example should spawn.");
            ExampleSpeaks = Bind(this, "General", "Speaks", true, "If Example can talk.");

            ExampleVisible = Bind(this, "Visibility", "Set Opacity", false, "If Example becomes transparent.");
            ExampleVisibility = Bind(this, "Visibility", "Amount", 0.5f, "The opacity of Example if visibility is turned off.");
            ExampleVisiblityToggle = Bind(this, "Visibility", "Toggle KeyCode", KeyCode.O, "The key to press to make Example become transparent.");

            EnabledInGame = Bind(this, "Visibility", "In Game", false, "If Example is enabled in game. Includes Editor Preview.");
            EnabledInEditor = Bind(this, "Visibility", "In Editor", true, "If Example is enabled in editor.");
            EnabledInMenus = Bind(this, "Visibility", "In Menus", false, "If Example is enabled in menus.");

            Save();
        }

        #region Settings Changed

        public override void SetupSettingChanged()
        {

        }

        #endregion
    }
}
