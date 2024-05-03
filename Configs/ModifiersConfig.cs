using BepInEx.Configuration;

namespace BetterLegacy.Configs
{
    /// <summary>
    /// Modifiers Config for PA Legacy. Based on the ObjectModifiers mod.
    /// </summary>
    public class ModifiersConfig : BaseConfig
    {
        public static ModifiersConfig Instance { get; set; }

        public override ConfigFile Config { get; set; }

        public ModifiersConfig(ConfigFile config) : base(config)
        {
            Instance = this;
            Config = config;

            EditorLoadLevel = Config.Bind("Modifiers - Editing", "Modifier Loads Level in Editor", true, "Any modifiers with the \"loadLevel\" function will load the level whilst in the editor. This is only to prevent the loss of progress.");
            EditorSavesBeforeLoad = Config.Bind("Modifiers - Editing", "Saves Before Load", true, "The current level will have a backup saved before a level is loaded using a loadLevel modifier or before the game has been quit.");

            SetupSettingChanged();
        }

        /// <summary>
        /// Any modifiers with the "loadLevel" function will load the level whilst in the editor. This is only to prevent the loss of progress.
        /// </summary>
        public ConfigEntry<bool> EditorLoadLevel { get; set; }

        /// <summary>
        /// The current level will have a backup saved before a level is loaded using a loadLevel modifier or before the game has been quit.
        /// </summary>
        public ConfigEntry<bool> EditorSavesBeforeLoad { get; set; }

        public override void SetupSettingChanged()
        {

        }

        public override string ToString() => "Modifiers Config";
    }
}
