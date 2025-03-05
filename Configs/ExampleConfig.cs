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
        public Setting<bool> ShouldSpawn { get; set; }

        /// <summary>
        /// If Example should spawn.
        /// </summary>
        public Setting<bool> CanSpeak { get; set; }

        /// <summary>
        /// Where Example should spawn.
        /// </summary>
        public Setting<SceneName> SpawnScene { get; set; }

        #endregion

        #region Visibility

        /// <summary>
        /// If Example becomes transparent.
        /// </summary>
        public Setting<bool> IsTransparent { get; set; }

        /// <summary>
        /// The opacity of Example if visibility is turned off.
        /// </summary>
        public Setting<float> TransparencyOpacity { get; set; }

        /// <summary>
        /// The key to press to make Example become transparent.
        /// </summary>
        public Setting<KeyCode> TransparencyKeyToggle { get; set; }

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

        #region Behavior

        public Setting<bool> CanDance { get; set; }

        #endregion

        #endregion

        /// <summary>
        /// Bind the individual settings of the config.
        /// </summary>
        public override void BindSettings()
        {
            Load();

            ShouldSpawn = Bind(this, "General", "Should Spawn", true, "If Example should spawn.");
            CanSpeak = Bind(this, "General", "Speaks", true, "If Example can talk.");
            SpawnScene = BindEnum(this, "General", "Spawn Scene", SceneName.Editor, "Where Example should spawn.");

            IsTransparent = Bind(this, "Visibility", "Is Transparent", false, "If Example becomes transparent.");
            TransparencyOpacity = Bind(this, "Visibility", "Transparency Opacity", 0.5f, "The opacity of Example if visibility is turned off.");
            TransparencyKeyToggle = Bind(this, "Visibility", "Transparency Key Toggle", KeyCode.O, "The key to press to make Example become transparent.");

            EnabledInGame = Bind(this, "Visibility", "In Game", false, "If Example is enabled in game. Includes Editor Preview.");
            EnabledInEditor = Bind(this, "Visibility", "In Editor", true, "If Example is enabled in editor.");
            EnabledInMenus = Bind(this, "Visibility", "In Menus", false, "If Example is enabled in menus.");

            CanDance = Bind(this, "Behavior", "Can Dance", true, "If Example can dance.");

            Save();
        }

        #region Settings Changed

        public override void SetupSettingChanged()
        {

        }

        #endregion
    }
}
