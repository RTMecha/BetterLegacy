using BepInEx.Configuration;

namespace BetterLegacy.Configs
{
    public class ModifiersConfig : BaseConfig
    {
        public static ModifiersConfig Instance { get; set; }

        public override ConfigFile Config { get; set; }

        public ModifiersConfig(ConfigFile config) : base(config)
        {
            Instance = this;
            Config = config;

            EditorLoadLevel = Config.Bind("Editor", "Modifier Loads Level", false, "Any modifiers with the \"loadLevel\" function will load the level whilst in the editor. This is only to prevent the loss of progress.");
            EditorSavesBeforeLoad = Config.Bind("Editor", "Saves Before Load", false, "The current level will have a backup saved before a level is loaded using a loadLevel modifier or before the game has been quit.");

            SetupSettingChanged();
        }

        public ConfigEntry<bool> EditorLoadLevel { get; set; }
        public ConfigEntry<bool> EditorSavesBeforeLoad { get; set; }

        public override void SetupSettingChanged()
        {

        }

        public override string ToString() => "Modifiers Config";
    }
}
