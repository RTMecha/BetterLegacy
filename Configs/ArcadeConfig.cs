using BepInEx.Configuration;
using BetterLegacy.Arcade;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using BetterLegacy.Core.Managers.Networking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace BetterLegacy.Configs
{
    public class ArcadeConfig : BaseConfig
    {
        public static ArcadeConfig Instance { get; set; }
        public override ConfigFile Config { get; set; }

        public ArcadeConfig(ConfigFile config) : base(config)
        {
            Instance = this;
            Config = config;

            CurrentLevelMode = Config.Bind("Level", "Level Mode", 0, "If a modes.lsms exists in the arcade level folder that you're loading, it will list other level modes (think easy mode, cutscene mode, hard mode, etc). The value in this config is for choosing which mode gets loaded. 0 is the default level.lsb.");

            UseNewArcadeUI = Config.Bind("Arcade", "Use New UI", true, "If the arcade should use the new UI or not. The old UI should always be accessible if you want to use it.");

            TabsRoundedness = Config.Bind("Arcade", "Tabs Roundness", 1, new ConfigDescription("How rounded the tabs at the top of the Arcade UI are. (New UI Only)", new AcceptableValueRange<int>(0, 5)));

            LoadingBackRoundness = Config.Bind("Arcade", "Loading Back Roundness", 2, new ConfigDescription("How rounded the loading screens' back is.", new AcceptableValueRange<int>(0, 5)));
            LoadingIconRoundness = Config.Bind("Arcade", "Loading Icon Roundness", 1, new ConfigDescription("How rounded the loading screens' icon is", new AcceptableValueRange<int>(0, 5)));
            LoadingBarRoundness = Config.Bind("Arcade", "Loading Bar Roundness", 1, new ConfigDescription("How rounded the loading screens' loading bar is", new AcceptableValueRange<int>(0, 5)));

            LocalLevelsRoundness = Config.Bind("Arcade", "Local Levels Roundness", 1, new ConfigDescription("How rounded the levels are. (New UI Only)", new AcceptableValueRange<int>(0, 5)));
            LocalLevelsIconRoundness = Config.Bind("Arcade", "Local Levels Icon Roundness", 0, new ConfigDescription("How rounded the levels' icon are. (New UI Only)", new AcceptableValueRange<int>(0, 5)));

            SteamLevelsRoundness = Config.Bind("Arcade", "Steam Levels Roundness", 1, new ConfigDescription("How rounded the levels are. (New UI Only)", new AcceptableValueRange<int>(0, 5)));
            SteamLevelsIconRoundness = Config.Bind("Arcade", "Steam Levels Icon Roundness", 0, new ConfigDescription("How rounded the levels' icon are. (New UI Only)", new AcceptableValueRange<int>(0, 5)));

            MiscRounded = Config.Bind("Arcade", "Misc Rounded", true, "If the some random elements should be rounded in the UI. (New UI Only)");

            OnlyShowShineOnSelected = Config.Bind("Arcade", "Only Show Shine on Selected", true, "If the SS rank shine should only show on the current selected level with an SS rank or on all levels with an SS rank.");
            ShineSpeed = Config.Bind("Arcade", "SS Rank Shine Speed", 0.7f, new ConfigDescription("How fast the shine goes by.", new AcceptableValueRange<float>(0.1f, 3f)));
            ShineMaxDelay = Config.Bind("Arcade", "SS Rank Shine Max Delay", 0.6f, new ConfigDescription("The max time the shine delays.", new AcceptableValueRange<float>(0.1f, 3f)));
            ShineMinDelay = Config.Bind("Arcade", "SS Rank Shine Min Delay", 0.2f, new ConfigDescription("The min time the shine delays.", new AcceptableValueRange<float>(0.1f, 3f)));
            ShineColor = Config.Bind("Arcade", "SS Rank Shine Color", new Color(1f, 0.933f, 0.345f, 1f), "The color of the shine.");

            PageFieldRoundness = Config.Bind("Arcade", "Page Field Roundness", 1, new ConfigDescription("How rounded the Page Input Field is. (New UI Only)", new AcceptableValueRange<int>(0, 5)));

            PlayLevelMenuButtonsRoundness = Config.Bind("Arcade", "Play Level Menu Buttons Roundness", 1, new ConfigDescription("How rounded the Play Menu Buttons are. (New UI Only)", new AcceptableValueRange<int>(0, 5)));

            PlayLevelMenuIconRoundness = Config.Bind("Arcade", "Play Level Menu Icon Roundness", 2, new ConfigDescription("How rounded the Play Menu Buttons are. (New UI Only)", new AcceptableValueRange<int>(0, 5)));

            LocalLevelOrderby = Config.Bind("Arcade Sorting", "Local Orderby", LevelSort.Cover, "How the level list is ordered.");
            LocalLevelAscend = Config.Bind("Arcade Sorting", "Local Ascend", true, "If the level order should be up or down.");

            SteamLevelOrderby = Config.Bind("Arcade Sorting", "Steam Orderby", LevelSort.Cover, "How the level list is ordered.");
            SteamLevelAscend = Config.Bind("Arcade Sorting", "Steam Ascend", true, "If the level order should be up or down.");

            LocalLevelsPath = Config.Bind("Level", "Arcade Path in Beatmaps", "arcade", "The location of your local arcade folder.");

            OpenOnlineLevelAfterDownload = Config.Bind("Arcade", "Open After Download", true, "If the Play Level Menu should open once the level has finished downloading.");

            LoadSteamLevels = Config.Bind("Arcade", "Load Steam Levels After Local Loaded", true, "If subscribed Steam levels should load after the local levels have loaded.");
            QueuePlaysLevel = Config.Bind("Arcade", "Play First Queued", false, "If enabled, the game will immediately load into the first queued level, otherwise it will open it in the Play Level Menu.");
            ShuffleQueueAmount = Config.Bind("Arcade", "Shuffle Queue Amount", 5, new ConfigDescription("How many levels should be added to the Queue.", new AcceptableValueRange<int>(1, 50)));

            LevelManager.CurrentLevelMode = CurrentLevelMode.Value;
            LevelManager.Path = LocalLevelsPath.Value;

            SetupSettingChanged();
        }

        public ConfigEntry<int> CurrentLevelMode { get; set; }
        public ConfigEntry<bool> UseNewArcadeUI { get; set; }

        public ConfigEntry<int> TabsRoundedness { get; set; }

        public ConfigEntry<int> PlayLevelMenuButtonsRoundness { get; set; }
        public ConfigEntry<int> PlayLevelMenuIconRoundness { get; set; }

        public ConfigEntry<int> LocalLevelsRoundness { get; set; }
        public ConfigEntry<int> LocalLevelsIconRoundness { get; set; }

        public ConfigEntry<int> SteamLevelsRoundness { get; set; }
        public ConfigEntry<int> SteamLevelsIconRoundness { get; set; }

        public ConfigEntry<bool> MiscRounded { get; set; }

        public ConfigEntry<int> PageFieldRoundness { get; set; }
        public ConfigEntry<int> LoadingBackRoundness { get; set; }
        public ConfigEntry<int> LoadingIconRoundness { get; set; }
        public ConfigEntry<int> LoadingBarRoundness { get; set; }

        public ConfigEntry<string> LocalLevelsPath { get; set; }
        public ConfigEntry<bool> OpenOnlineLevelAfterDownload { get; set; }
        public ConfigEntry<bool> LoadSteamLevels { get; set; }
        public ConfigEntry<int> ShuffleQueueAmount { get; set; }
        public ConfigEntry<bool> QueuePlaysLevel { get; set; }

        #region Sorting

        public ConfigEntry<bool> LocalLevelAscend { get; set; }
        public ConfigEntry<LevelSort> LocalLevelOrderby { get; set; }

        public ConfigEntry<bool> SteamLevelAscend { get; set; }
        public ConfigEntry<LevelSort> SteamLevelOrderby { get; set; }

        #endregion

        #region Shine Config

        public ConfigEntry<bool> OnlyShowShineOnSelected { get; set; }
        public ConfigEntry<float> ShineSpeed { get; set; }
        public ConfigEntry<float> ShineMaxDelay { get; set; }
        public ConfigEntry<float> ShineMinDelay { get; set; }
        public ConfigEntry<Color> ShineColor { get; set; }

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
    }
}
