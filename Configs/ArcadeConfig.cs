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

        public ArcadeConfig() : base("Arcade") // Set config name via base("")
        {
            Instance = this;
            BindSettings();

            LevelManager.CurrentLevelMode = CurrentLevelMode.Value;
            LevelManager.Path = LocalLevelsPath.Value;

            SetupSettingChanged();
        }

        #region Settings

        #region Level

        /// <summary>
        /// If a modes.lsms exists in the arcade level folder that you're loading, it will list other level modes (think easy mode, cutscene mode, hard mode, etc). The value in this config is for choosing which mode gets loaded. 0 is the default level.lsb or level.vgd.
        /// </summary>
        public Setting<int> CurrentLevelMode { get; set; }

        /// <summary>
        /// If the Play Level Menu should open once the level has finished downloading.
        /// </summary>
        public Setting<bool> OpenOnlineLevelAfterDownload { get; set; }

        /// <summary>
        /// The location of your local arcade folder.
        /// </summary>
        public Setting<string> LocalLevelsPath { get; set; }

        /// <summary>
        /// If subscribed Steam levels should load after the local levels have loaded.
        /// </summary>
        public Setting<bool> LoadSteamLevels { get; set; }

        /// <summary>
        /// If enabled, the game will immediately load into the first queued level, otherwise it will open it in the Play Level Menu.
        /// </summary>
        public Setting<bool> QueuePlaysLevel { get; set; }

        /// <summary>
        /// How many levels should be added to the Queue.
        /// </summary>
        public Setting<int> ShuffleQueueAmount { get; set; }

        #endregion

        #region UI

        /// <summary>
        /// If the arcade should use the new UI or not. The old modded UI should always be accessible if you want to use it.
        /// </summary>
        public Setting<bool> UseNewArcadeUI { get; set; }

        /// <summary>
        /// The roundness of the tabs at the top of the Arcade UI. (New UI Only)
        /// </summary>
        public Setting<int> TabsRoundedness { get; set; }

        /// <summary>
        /// The roundness of the loading screens' back.
        /// </summary>
        public Setting<int> LoadingBackRoundness { get; set; }

        /// <summary>
        /// The roundness of the loading screens' icon.
        /// </summary>
        public Setting<int> LoadingIconRoundness { get; set; }

        /// <summary>
        /// The roundness of the loading screens' loading bar.
        /// </summary>
        public Setting<int> LoadingBarRoundness { get; set; }

        /// <summary>
        /// The roundness of the levels.
        /// </summary>
        public Setting<int> LocalLevelsRoundness { get; set; }

        /// <summary>
        /// The roundness of the levels' icon.
        /// </summary>
        public Setting<int> LocalLevelsIconRoundness { get; set; }

        /// <summary>
        /// The roundness of the Steam levels.
        /// </summary>
        public Setting<int> SteamLevelsRoundness { get; set; }

        /// <summary>
        /// The roundness of the Steam levels' icon.
        /// </summary>
        public Setting<int> SteamLevelsIconRoundness { get; set; }

        /// <summary>
        /// The roundness of the Page Input Field.
        /// </summary>
        public Setting<int> PageFieldRoundness { get; set; }

        /// <summary>
        /// The roundness of the Play Menu Buttons.
        /// </summary>
        public Setting<int> PlayLevelMenuButtonsRoundness { get; set; }

        /// <summary>
        /// The roundness of the Play Menu icon.
        /// </summary>
        public Setting<int> PlayLevelMenuIconRoundness { get; set; }

        /// <summary>
        /// If some random elements should be rounded in the UI.
        /// </summary>
        public Setting<bool> MiscRounded { get; set; }

        /// <summary>
        /// If the SS rank shine should only show on the current selected level with an SS rank or on all levels with an SS rank.
        /// </summary>
        public Setting<bool> OnlyShowShineOnSelected { get; set; }

        /// <summary>
        /// How fast the shine goes by.
        /// </summary>
        public Setting<float> ShineSpeed { get; set; }

        /// <summary>
        /// The max time the shine delays.
        /// </summary>
        public Setting<float> ShineMaxDelay { get; set; }

        /// <summary>
        /// The min time the shine delays.
        /// </summary>
        public Setting<float> ShineMinDelay { get; set; }

        /// <summary>
        /// The color of the shine.
        /// </summary>
        public Setting<Color> ShineColor { get; set; }

        #endregion

        #region Sorting

        /// <summary>
        /// How the level list is ordered.
        /// </summary>
        public Setting<LevelSort> LocalLevelOrderby { get; set; }

        /// <summary>
        /// If the level order should be up or down.
        /// </summary>
        public Setting<bool> LocalLevelAscend { get; set; }

        /// <summary>
        /// How the Steam level list is ordered.
        /// </summary>
        public Setting<LevelSort> SteamLevelOrderby { get; set; }

        /// <summary>
        /// If the Steam level order should be up or down.
        /// </summary>
        public Setting<bool> SteamLevelAscend { get; set; }

        #endregion

        #endregion

        /// <summary>
        /// Bind the individual settings of the config.
        /// </summary>
        public override void BindSettings()
        {
            Load();

            #region Level

            CurrentLevelMode = Bind(this, "Level", "Level Mode", 0, "If a modes.lsms exists in the arcade level folder that you're loading, it will list other level modes (think easy mode, cutscene mode, hard mode, etc). The value in this config is for choosing which mode gets loaded. 0 is the default level.lsb or level.vgd.");
            OpenOnlineLevelAfterDownload = Bind(this, "Level", "Open After Download", true, "If the Play Level Menu should open once the level has finished downloading.");
            LocalLevelsPath = Bind(this, "Level", "Arcade Path in Beatmaps", "arcade", "The location of your local arcade folder.");
            LoadSteamLevels = Bind(this, "Level", "Load Steam Levels After Local Loaded", true, "If subscribed Steam levels should load after the local levels have loaded.");
            QueuePlaysLevel = Bind(this, "Level", "Play First Queued", false, "If enabled, the game will immediately load into the first queued level, otherwise it will open it in the Play Level Menu.");
            ShuffleQueueAmount = Bind(this, "Level", "Shuffle Queue Amount", 5, "How many levels should be added to the Queue.", 1, 50);

            #endregion

            #region UI

            UseNewArcadeUI = Bind(this, "UI", "Use New UI", true, "If the arcade should use the new UI or not. The old modded UI should always be accessible if you want to use it.");

            TabsRoundedness = Bind(this, "UI", "Tabs Roundness", 1, "The roundness of the tabs at the top of the Arcade UI. (New UI Only)", 0, 5);

            LoadingBackRoundness = Bind(this, "UI", "Loading Back Roundness", 2, "The roundness of the loading screens' back.", 0, 5);
            LoadingIconRoundness = Bind(this, "UI", "Loading Icon Roundness", 1, "The roundness of the loading screens' icon.", 0, 5);
            LoadingBarRoundness = Bind(this, "UI", "Loading Bar Roundness", 1, "The roundness of the loading screens' loading bar.", 0, 5);

            LocalLevelsRoundness = Bind(this, "UI", "Local Levels Roundness", 1, "The roundness of the levels. (New UI Only)", 0, 5);
            LocalLevelsIconRoundness = Bind(this, "UI", "Local Levels Icon Roundness", 0, "The roundness of the levels' icon. (New UI Only)", 0, 5);

            SteamLevelsRoundness = Bind(this, "UI", "Steam Levels Roundness", 1, "The roundness of the Steam levels. (New UI Only)", 0, 5);
            SteamLevelsIconRoundness = Bind(this, "UI", "Steam Levels Icon Roundness", 0, "The roundness of the Steam levels' icon. (New UI Only)", 0, 5);

            PageFieldRoundness = Bind(this, "UI", "Page Field Roundness", 1, "The roundness of the Page Input Field. (New UI Only)", 0, 5);

            PlayLevelMenuButtonsRoundness = Bind(this, "UI", "Play Level Menu Buttons Roundness", 1, "The roundness of the Play Menu Buttons. (New UI Only)", 0, 5);

            PlayLevelMenuIconRoundness = Bind(this, "UI", "Play Level Menu Icon Roundness", 2, "The roundness of the Play Menu icon. (New UI Only)", 0, 5);

            MiscRounded = Bind(this, "UI", "Misc Rounded", true, "If some random elements should be rounded in the UI. (New UI Only)");

            OnlyShowShineOnSelected = Bind(this, "UI", "Only Show Shine on Selected", true, "If the SS rank shine should only show on the current selected level with an SS rank or on all levels with an SS rank.");
            ShineSpeed = Bind(this, "UI", "SS Rank Shine Speed", 0.7f, "How fast the shine goes by.", 0.1f, 3f);
            ShineMaxDelay = Bind(this, "UI", "SS Rank Shine Max Delay", 0.6f, "The max time the shine delays.", 0.1f, 3f);
            ShineMinDelay = Bind(this, "UI", "SS Rank Shine Min Delay", 0.2f, "The min time the shine delays.", 0.1f, 3f);
            ShineColor = Bind(this, "UI", "SS Rank Shine Color", new Color(1f, 0.933f, 0.345f, 1f), "The color of the shine.");

            #endregion

            #region Sorting

            LocalLevelOrderby = BindEnum(this, "Sorting", "Local Orderby", LevelSort.Cover, "How the level list is ordered.");
            LocalLevelAscend = Bind(this, "Sorting", "Local Ascend", true, "If the level order should be up or down.");

            SteamLevelOrderby = BindEnum(this, "Sorting", "Steam Orderby", LevelSort.Cover, "How the Steam level list is ordered.");
            SteamLevelAscend = Bind(this, "Sorting", "Steam Ascend", true, "If the Steam level order should be up or down.");

            #endregion

            Save();
        }

        #region Settings Changed

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

        void SteamLevelSortChanged()
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

        void LocalLevelSortChanged()
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

        void PlayLevelMenuRoundnessChanged() => PlayLevelMenuManager.inst?.UpdateRoundness();

        void MiscRoundedChanged() => ArcadeMenuManager.inst?.UpdateMiscRoundness();

        void LocalLevelPanelsRoundnessChanged() => ArcadeMenuManager.inst?.UpdateLocalLevelsRoundness();

        void SteamLevelPanelsRoundnessChanged() => ArcadeMenuManager.inst?.UpdateSteamLevelsRoundness();

        void TabsRoundnessChanged() => ArcadeMenuManager.inst?.UpdateTabRoundness();

        void CurrentLevelModeChanged()
        {
            LevelManager.CurrentLevelMode = CurrentLevelMode.Value;
        }

        void LocalLevelsPathChanged()
        {
            LevelManager.Path = LocalLevelsPath.Value;
        }

        #endregion
    }
}
