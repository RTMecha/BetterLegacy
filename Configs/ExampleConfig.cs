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

        /// <summary>
        /// If logs about Example should be made.
        /// </summary>
        public Setting<bool> LogsEnabled { get; set; }

        /// <summary>
        /// If logs about Example's startup should be made.
        /// </summary>
        public Setting<bool> LogStartup { get; set; }

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

        public Setting<bool> CanNotice { get; set; }

        public Setting<bool> CanGoToWarningPopup { get; set; }

        public Setting<int> LoadedLevelNoticeChance { get; set; }

        public Setting<int> NewObjectNoticeChance { get; set; }

        public Setting<int> SavedEditorLevelNoticeChance { get; set; }

        public Setting<int> AutosaveNoticeChance { get; set; }

        public Setting<int> PlayerHitNoticeChance { get; set; }
        public Setting<int> PlayerDeathNoticeChance { get; set; }

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
            LogsEnabled = Bind(this, "General", "Logs Enabled", false, "If logs about Example should be made.");
            LogStartup = Bind(this, "General", "Log Startup", false, "If logs about Example's startup should be made.");

            IsTransparent = Bind(this, "Visibility", "Is Transparent", false, "If Example becomes transparent.");
            TransparencyOpacity = Bind(this, "Visibility", "Transparency Opacity", 0.5f, "The opacity of Example if visibility is turned off.");
            TransparencyKeyToggle = Bind(this, "Visibility", "Transparency Key Toggle", KeyCode.O, "The key to press to make Example become transparent.");

            EnabledInGame = Bind(this, "Visibility", "In Game", false, "If Example is enabled in game. Includes Editor Preview.");
            EnabledInEditor = Bind(this, "Visibility", "In Editor", true, "If Example is enabled in editor.");
            EnabledInMenus = Bind(this, "Visibility", "In Menus", false, "If Example is enabled in menus.");

            CanDance = Bind(this, "Behavior", "Can Dance", true, "If Example can dance.");
            CanGoToWarningPopup = Bind(this, "Behavior", "Can Go To Warning Popup", true, "If Example moves to the Warning Popup when it opens.");
            CanNotice = Bind(this, "Behavior", "Can Notice", true, "If Example can notice your actions.");
            LoadedLevelNoticeChance = Bind(this, "Behavior", "Loaded Level Notice Chance", 100, "The percent chance Example says something about you loading a level.", 0, 100);
            NewObjectNoticeChance = Bind(this, "Behavior", "New Object Notice Chance", 20, "The percent chance Example says something about you making a new object.", 0, 100);
            SavedEditorLevelNoticeChance = Bind(this, "Behavior", "Saved Editor Level Notice Chance", 60, "The percent chance Example says something about you saving the current editor level.", 0, 100);
            AutosaveNoticeChance = Bind(this, "Behavior", "Autosave Notice Chance", 40, "The percent chance Example says something about autosave happening.", 0, 100);
            PlayerHitNoticeChance = Bind(this, "Behavior", "Player Hit Notice Chance", 15, "The percent chance Example notices a player getting hit.", 0, 100);
            PlayerDeathNoticeChance = Bind(this, "Behavior", "Player Death Notice Chance", 50, "The percent chance Example notices a player dying.", 0, 100);

            Save();
        }

        #region Settings Changed

        public override void SetupSettingChanged()
        {

        }

        #endregion
    }
}
