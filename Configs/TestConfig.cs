namespace BetterLegacy.Configs
{
    public class TestConfig : BaseConfig
    {
        public static TestConfig Instance { get; set; }

        public TestConfig() : base("Test") // Set config name via base("")
        {
            Instance = this;
            BindSettings();

            SetupSettingChanged();
        }

        #region Settings

        public Setting<int> SomeSetting { get; set; }

        #endregion

        /// <summary>
        /// Bind the individual settings of the config.
        /// </summary>
        public override void BindSettings()
        {
            Load();
            // Binding settings go in the middle here.
            Save();
        }

        #region Settings Changed

        public override void SetupSettingChanged()
        {

        }

        #endregion
    }
}
