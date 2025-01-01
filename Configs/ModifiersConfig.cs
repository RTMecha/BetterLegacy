namespace BetterLegacy.Configs
{
    /// <summary>
    /// Modifiers Config for PA Legacy. Based on the ObjectModifiers mod.
    /// </summary>
    public class ModifiersConfig : BaseConfig
    {
        public static ModifiersConfig Instance { get; set; }

        public ModifiersConfig() : base(nameof(ModifiersConfig)) // Set config name via base("")
        {
            Instance = this;
            BindSettings();

            SetupSettingChanged();
        }

        #region Settings

        /// <summary>
        /// Any modifiers with the "loadLevel" function will load the level whilst in the editor. This is only to prevent the loss of progress.
        /// </summary>
        public Setting<bool> EditorLoadLevel { get; set; }

        /// <summary>
        /// The current level will have a backup saved before a level is loaded using a loadLevel modifier or before the game has been quit.
        /// </summary>
        public Setting<bool> EditorSavesBeforeLoad { get; set; }

        #endregion

        /// <summary>
        /// Bind the individual settings of the config.
        /// </summary>
        public override void BindSettings()
        {
            Load();

            EditorLoadLevel = Bind(this, "Editing", "Modifier Loads Level in Editor", true, "Any modifiers with the \"loadLevel\" function will load the level whilst in the editor. This is only to prevent the loss of progress.");
            EditorSavesBeforeLoad = Bind(this, "Editing", "Saves Before Load", true, "The current level will have a backup saved before a level is loaded using a loadLevel modifier or before the game has been quit.");

            Save();
        }

        #region Settings Changed

        public override void SetupSettingChanged()
        {

        }

        #endregion
    }
}
