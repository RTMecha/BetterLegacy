using BepInEx.Configuration;

namespace BetterLegacy.Configs
{
    public abstract class BaseConfig
    {
        public abstract ConfigFile Config { get; set; }

        public BaseConfig(ConfigFile config)
        {
            Config = config;
        }

        public abstract void SetupSettingChanged();
    }
}
