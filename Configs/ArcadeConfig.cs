using BepInEx.Configuration;
using BetterLegacy.Arcade;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using BetterLegacy.Core.Managers.Networking;
using System;
using UnityEngine;
using UnityEngine.UI;

namespace BetterLegacy.Configs
{
    /// <summary>
    /// Arcade Config for PA Legacy. Based on the ArcadiaCustoms mod.
    /// </summary>
    public class ArcadeConfig : BaseConfig
    {
        public static ArcadeConfig Instance { get; set; }
        public override ConfigFile Config { get; set; }

        public ArcadeConfig(ConfigFile config) : base(config)
        {
            Instance = this;
            Config = config;

            #region Level

            CurrentLevelMode = Config.Bind("Arcade - Level", "Level Mode", 0, "If a modes.lsms exists in the arcade level folder that you're loading, it will list other level modes (think easy mode, cutscene mode, hard mode, etc). The value in this config is for choosing which mode gets loaded. 0 is the default level.lsb or level.vgd.");
            OpenOnlineLevelAfterDownload = Config.Bind("Arcade - Level", "Open After Download", true, "If the Play Level Menu should open once the level has finished downloading.");
            LocalLevelsPath = Config.Bind("Arcade - Level", "Arcade Path in Beatmaps", "arcade", "The location of your local arcade folder.");
            LoadSteamLevels = Config.Bind("Arcade - Level", "Load Steam Levels After Local Loaded", true, "If subscribed Steam levels should load after the local levels have loaded.");
            QueuePlaysLevel = Config.Bind("Arcade - Level", "Play First Queued", false, "If enabled, the game will immediately load into the first queued level, otherwise it will open it in the Play Level Menu.");
            ShuffleQueueAmount = Config.Bind("Arcade - Level", "Shuffle Queue Amount", 5, new ConfigDescription("How many levels should be added to the Queue.", new AcceptableValueRange<int>(1, 50)));

            #endregion

            #region UI

            UseNewArcadeUI = Config.Bind("Arcade - UI", "Use New UI", true, "If the arcade should use the new UI or not. The old modded UI should always be accessible if you want to use it.");

            TabsRoundedness = Config.Bind("Arcade - UI", "Tabs Roundness", 1, new ConfigDescription("The roundness of the tabs at the top of the Arcade UI. (New UI Only)", new AcceptableValueRange<int>(0, 5)));

            LoadingBackRoundness = Config.Bind("Arcade - UI", "Loading Back Roundness", 2, new ConfigDescription("The roundness of the loading screens' back.", new AcceptableValueRange<int>(0, 5)));
            LoadingIconRoundness = Config.Bind("Arcade - UI", "Loading Icon Roundness", 1, new ConfigDescription("The roundness of the loading screens' icon.", new AcceptableValueRange<int>(0, 5)));
            LoadingBarRoundness = Config.Bind("Arcade - UI", "Loading Bar Roundness", 1, new ConfigDescription("The roundness of the loading screens' loading bar.", new AcceptableValueRange<int>(0, 5)));

            LocalLevelsRoundness = Config.Bind("Arcade - UI", "Local Levels Roundness", 1, new ConfigDescription("The roundness of the levels. (New UI Only)", new AcceptableValueRange<int>(0, 5)));
            LocalLevelsIconRoundness = Config.Bind("Arcade - UI", "Local Levels Icon Roundness", 0, new ConfigDescription("The roundness of the levels' icon. (New UI Only)", new AcceptableValueRange<int>(0, 5)));

            SteamLevelsRoundness = Config.Bind("Arcade - UI", "Steam Levels Roundness", 1, new ConfigDescription("The roundness of the Steam levels. (New UI Only)", new AcceptableValueRange<int>(0, 5)));
            SteamLevelsIconRoundness = Config.Bind("Arcade - UI", "Steam Levels Icon Roundness", 0, new ConfigDescription("The roundness of the Steam levels' icon. (New UI Only)", new AcceptableValueRange<int>(0, 5)));

            PageFieldRoundness = Config.Bind("Arcade - UI", "Page Field Roundness", 1, new ConfigDescription("The roundness of the Page Input Field. (New UI Only)", new AcceptableValueRange<int>(0, 5)));

            PlayLevelMenuButtonsRoundness = Config.Bind("Arcade - UI", "Play Level Menu Buttons Roundness", 1, new ConfigDescription("The roundness of the Play Menu Buttons. (New UI Only)", new AcceptableValueRange<int>(0, 5)));

            PlayLevelMenuIconRoundness = Config.Bind("Arcade - UI", "Play Level Menu Icon Roundness", 2, new ConfigDescription("The roundness of the Play Menu icon. (New UI Only)", new AcceptableValueRange<int>(0, 5)));

            MiscRounded = Config.Bind("Arcade", "Misc Rounded - UI", true, "If some random elements should be rounded in the UI. (New UI Only)");

            OnlyShowShineOnSelected = Config.Bind("Arcade - UI", "Only Show Shine on Selected", true, "If the SS rank shine should only show on the current selected level with an SS rank or on all levels with an SS rank.");
            ShineSpeed = Config.Bind("Arcade - UI", "SS Rank Shine Speed", 0.7f, new ConfigDescription("How fast the shine goes by.", new AcceptableValueRange<float>(0.1f, 3f)));
            ShineMaxDelay = Config.Bind("Arcade - UI", "SS Rank Shine Max Delay", 0.6f, new ConfigDescription("The max time the shine delays.", new AcceptableValueRange<float>(0.1f, 3f)));
            ShineMinDelay = Config.Bind("Arcade - UI", "SS Rank Shine Min Delay", 0.2f, new ConfigDescription("The min time the shine delays.", new AcceptableValueRange<float>(0.1f, 3f)));
            ShineColor = Config.Bind("Arcade - UI", "SS Rank Shine Color", new Color(1f, 0.933f, 0.345f, 1f), "The color of the shine.");

            #endregion

            #region Sorting

            LocalLevelOrderby = Config.Bind("Arcade - Sorting", "Local Orderby", LevelSort.Cover, "How the level list is ordered.");
            LocalLevelAscend = Config.Bind("Arcade - Sorting", "Local Ascend", true, "If the level order should be up or down.");

            SteamLevelOrderby = Config.Bind("Arcade - Sorting", "Steam Orderby", LevelSort.Cover, "How the Steam level list is ordered.");
            SteamLevelAscend = Config.Bind("Arcade - Sorting", "Steam Ascend", true, "If the Steam level order should be up or down.");

            #endregion

            LevelManager.CurrentLevelMode = CurrentLevelMode.Value;
            LevelManager.Path = LocalLevelsPath.Value;

            SetupSettingChanged();
        }

        #region Level

        /// <summary>
        /// If a modes.lsms exists in the arcade level folder that you're loading, it will list other level modes (think easy mode, cutscene mode, hard mode, etc). The value in this config is for choosing which mode gets loaded. 0 is the default level.lsb or level.vgd.
        /// </summary>
        public ConfigEntry<int> CurrentLevelMode { get; set; }

        /// <summary>
        /// If the Play Level Menu should open once the level has finished downloading.
        /// </summary>
        public ConfigEntry<bool> OpenOnlineLevelAfterDownload { get; set; }

        /// <summary>
        /// The location of your local arcade folder.
        /// </summary>
        public ConfigEntry<string> LocalLevelsPath { get; set; }

        /// <summary>
        /// If subscribed Steam levels should load after the local levels have loaded.
        /// </summary>
        public ConfigEntry<bool> LoadSteamLevels { get; set; }

        /// <summary>
        /// If enabled, the game will immediately load into the first queued level, otherwise it will open it in the Play Level Menu.
        /// </summary>
        public ConfigEntry<bool> QueuePlaysLevel { get; set; }

        /// <summary>
        /// How many levels should be added to the Queue.
        /// </summary>
        public ConfigEntry<int> ShuffleQueueAmount { get; set; }

        #endregion

        #region UI

        /// <summary>
        /// If the arcade should use the new UI or not. The old modded UI should always be accessible if you want to use it.
        /// </summary>
        public ConfigEntry<bool> UseNewArcadeUI { get; set; }

        /// <summary>
        /// The roundness of the tabs at the top of the Arcade UI. (New UI Only)
        /// </summary>
        public ConfigEntry<int> TabsRoundedness { get; set; }

        /// <summary>
        /// The roundness of the loading screens' back.
        /// </summary>
        public ConfigEntry<int> LoadingBackRoundness { get; set; }

        /// <summary>
        /// The roundness of the loading screens' icon.
        /// </summary>
        public ConfigEntry<int> LoadingIconRoundness { get; set; }

        /// <summary>
        /// The roundness of the loading screens' loading bar.
        /// </summary>
        public ConfigEntry<int> LoadingBarRoundness { get; set; }

        /// <summary>
        /// The roundness of the levels.
        /// </summary>
        public ConfigEntry<int> LocalLevelsRoundness { get; set; }

        /// <summary>
        /// The roundness of the levels' icon.
        /// </summary>
        public ConfigEntry<int> LocalLevelsIconRoundness { get; set; }

        /// <summary>
        /// The roundness of the Steam levels.
        /// </summary>
        public ConfigEntry<int> SteamLevelsRoundness { get; set; }

        /// <summary>
        /// The roundness of the Steam levels' icon.
        /// </summary>
        public ConfigEntry<int> SteamLevelsIconRoundness { get; set; }

        /// <summary>
        /// The roundness of the Page Input Field.
        /// </summary>
        public ConfigEntry<int> PageFieldRoundness { get; set; }

        /// <summary>
        /// The roundness of the Play Menu Buttons.
        /// </summary>
        public ConfigEntry<int> PlayLevelMenuButtonsRoundness { get; set; }

        /// <summary>
        /// The roundness of the Play Menu icon.
        /// </summary>
        public ConfigEntry<int> PlayLevelMenuIconRoundness { get; set; }

        /// <summary>
        /// If some random elements should be rounded in the UI.
        /// </summary>
        public ConfigEntry<bool> MiscRounded { get; set; }

        /// <summary>
        /// If the SS rank shine should only show on the current selected level with an SS rank or on all levels with an SS rank.
        /// </summary>
        public ConfigEntry<bool> OnlyShowShineOnSelected { get; set; }

        /// <summary>
        /// How fast the shine goes by.
        /// </summary>
        public ConfigEntry<float> ShineSpeed { get; set; }

        /// <summary>
        /// The max time the shine delays.
        /// </summary>
        public ConfigEntry<float> ShineMaxDelay { get; set; }

        /// <summary>
        /// The min time the shine delays.
        /// </summary>
        public ConfigEntry<float> ShineMinDelay { get; set; }

        /// <summary>
        /// The color of the shine.
        /// </summary>
        public ConfigEntry<Color> ShineColor { get; set; }

        #endregion

        #region Sorting

        /// <summary>
        /// How the level list is ordered.
        /// </summary>
        public ConfigEntry<LevelSort> LocalLevelOrderby { get; set; }

        /// <summary>
        /// If the level order should be up or down.
        /// </summary>
        public ConfigEntry<bool> LocalLevelAscend { get; set; }

        /// <summary>
        /// How the Steam level list is ordered.
        /// </summary>
        public ConfigEntry<LevelSort> SteamLevelOrderby { get; set; }

        /// <summary>
        /// If the Steam level order should be up or down.
        /// </summary>
        public ConfigEntry<bool> SteamLevelAscend { get; set; }

        #endregion

        public override void SetupSettingChanged()
        {
            CurrentLevelMode.SettingChanged += CurrentLevelModeChanged;

            TabsRoundedness.SettingChanged += TabsRoundnessChanged;

            LocalLevelsRoundness.SettingChanged += LocalLevelPanelsRoundnessChanged;
            LocalLevelsIconRoundness.SettingChanged += LocalLevelPanelsRoundnessChanged;

            SteamLevelsRoundness.SettingChanged += SteamLevelPanelsRoundnessChanged;
            SteamLevelsIconRoundness.SettingChanged += SteamLevelPanelsRoundnessChanged;

            MiscRounded.SettingChanged += MiscRoundedChanged;

            PageFieldRoundness.SettingChanged += MiscRoundedChanged;

            PlayLevelMenuButtonsRoundness.SettingChanged += PlayLevelMenuRoundnessChanged;

            PlayLevelMenuIconRoundness.SettingChanged += PlayLevelMenuRoundnessChanged;

            LocalLevelOrderby.SettingChanged += LocalLevelSortChanged;
            LocalLevelAscend.SettingChanged += LocalLevelSortChanged;

            SteamLevelOrderby.SettingChanged += SteamLevelSortChanged;
            SteamLevelAscend.SettingChanged += SteamLevelSortChanged;

            LocalLevelsPath.SettingChanged += LocalLevelsPathChanged;
        }

        #region Settings Changed

        void SteamLevelSortChanged(object sender, EventArgs e)
        {
            SteamWorkshopManager.inst.Levels = LevelManager.SortLevels(SteamWorkshopManager.inst.Levels, (int)SteamLevelOrderby.Value, SteamLevelAscend.Value);

            if (ArcadeMenuManager.inst && ArcadeMenuManager.inst.CurrentTab == 5 && ArcadeMenuManager.inst.steamViewType == ArcadeMenuManager.SteamViewType.Subscribed)
            {
                ArcadeMenuManager.inst.selected = new Vector2Int(0, 2);
                if (ArcadeMenuManager.inst.steamPageField.text != "0")
                    ArcadeMenuManager.inst.steamPageField.text = "0";
                else
                    CoreHelper.StartCoroutine(ArcadeMenuManager.inst.RefreshSubscribedSteamLevels());
            }
        }

        void LocalLevelSortChanged(object sender, EventArgs e)
        {
            LevelManager.Sort((int)LocalLevelOrderby.Value, LocalLevelAscend.Value);

            if (LevelMenuManager.inst)
            {
                LevelMenuManager.levelFilter = (int)LocalLevelOrderby.Value;
                LevelMenuManager.levelAscend = LocalLevelAscend.Value;

                var toggleClone = LevelMenuManager.levelList.transform.Find("toggle/toggle").GetComponent<Toggle>();
                toggleClone.onValueChanged.RemoveAllListeners();
                toggleClone.isOn = LevelMenuManager.levelAscend;
                toggleClone.onValueChanged.AddListener(delegate (bool _val)
                {
                    LevelMenuManager.levelAscend = _val;
                    LevelMenuManager.Sort();
                    CoreHelper.StartCoroutine(LevelMenuManager.GenerateUIList());
                });

                var dropdownClone = LevelMenuManager.levelList.transform.Find("orderby dropdown").GetComponent<Dropdown>();
                dropdownClone.onValueChanged.RemoveAllListeners();
                dropdownClone.value = LevelMenuManager.levelFilter;
                dropdownClone.onValueChanged.AddListener(delegate (int _val)
                {
                    LevelMenuManager.levelFilter = _val;
                    LevelMenuManager.Sort();
                    CoreHelper.StartCoroutine(LevelMenuManager.GenerateUIList());
                });

                CoreHelper.StartCoroutine(LevelMenuManager.GenerateUIList());
            }

            if (ArcadeMenuManager.inst)
            {
                ArcadeMenuManager.inst.selected = new Vector2Int(0, 2);
                if (ArcadeMenuManager.inst.localPageField.text != "0")
                    ArcadeMenuManager.inst.localPageField.text = "0";
                else
                    CoreHelper.StartCoroutine(ArcadeMenuManager.inst.RefreshLocalLevels());
            }
        }

        void PlayLevelMenuRoundnessChanged(object sender, EventArgs e) => PlayLevelMenuManager.inst?.UpdateRoundness();

        void MiscRoundedChanged(object sender, EventArgs e) => ArcadeMenuManager.inst?.UpdateMiscRoundness();

        void LocalLevelPanelsRoundnessChanged(object sender, EventArgs e) => ArcadeMenuManager.inst?.UpdateLocalLevelsRoundness();

        void SteamLevelPanelsRoundnessChanged(object sender, EventArgs e) => ArcadeMenuManager.inst?.UpdateSteamLevelsRoundness();

        void TabsRoundnessChanged(object sender, EventArgs e) => ArcadeMenuManager.inst?.UpdateTabRoundness();

        void CurrentLevelModeChanged(object sender, EventArgs e)
        {
            LevelManager.CurrentLevelMode = CurrentLevelMode.Value;
        }

        void LocalLevelsPathChanged(object sender, EventArgs e)
        {
            LevelManager.Path = LocalLevelsPath.Value;
        }

        #endregion

        public override string ToString() => "Arcade Config";
    }
}
