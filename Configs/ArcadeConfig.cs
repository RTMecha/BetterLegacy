using UnityEngine;

using BetterLegacy.Arcade.Interfaces;
using BetterLegacy.Core.Managers;
using BetterLegacy.Core.Managers.Networking;

namespace BetterLegacy.Configs
{
    /// <summary>
    /// Arcade Config for PA Legacy. Based on the ArcadiaCustoms mod.
    /// </summary>
    public class ArcadeConfig : BaseConfig
    {
        public static ArcadeConfig Instance { get; set; }

        public ArcadeConfig() : base(nameof(ArcadeConfig)) // Set config name via base("")
        {
            Instance = this;
            BindSettings();

            LevelManager.CurrentLevelMode = CurrentLevelMode.Value;
            LevelManager.Path = LocalLevelsPath.Value;

            SetupSettingChanged();
        }

        public override string TabName => "Arcade";
        public override Color TabColor => new Color(1f, 0.143f, 0.22f, 1f);
        public override string TabDesc => "The Arcade menus and levels.";

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

        /// <summary>
        /// The yield instruction used for spacing out levels being loaded in the Arcade. Some options will load faster but freeze the game, while others load slower but allow you to see them loading in the menus.
        /// </summary>
        public Setting<YieldType> LoadYieldMode { get; set; }

        /// <summary>
        /// If folders containing levels should appear.
        /// </summary>
        public Setting<bool> LoadFolders { get; set; }

        #endregion

        #region UI

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
        /// If searching should automatically run when sort, ascend, etc is changed.
        /// </summary>
        public Setting<bool> AutoSearch { get; set; }

        /// <summary>
        /// How the level list is ordered.
        /// </summary>
        public Setting<LevelSort> LocalLevelOrderby { get; set; }

        /// <summary>
        /// If the level order should be up or down.
        /// </summary>
        public Setting<bool> LocalLevelAscend { get; set; }

        /// <summary>
        /// How the online level list is ordered.
        /// </summary>
        public Setting<OnlineLevelSort> OnlineLevelOrderby { get; set; }

        /// <summary>
        /// If the online level order should be up or down.
        /// </summary>
        public Setting<bool> OnlineLevelAscend { get; set; }

        /// <summary>
        /// How the online level collection list is ordered.
        /// </summary>
        public Setting<OnlineLevelCollectionSort> OnlineLevelCollectionOrderby { get; set; }

        /// <summary>
        /// If the online level collection order should be up or down.
        /// </summary>
        public Setting<bool> OnlineLevelCollectionAscend { get; set; }

        /// <summary>
        /// How the Steam level list is ordered.
        /// </summary>
        public Setting<LevelSort> SteamLevelOrderby { get; set; }

        /// <summary>
        /// If the Steam level order should be up or down.
        /// </summary>
        public Setting<bool> SteamLevelAscend { get; set; }

        /// <summary>
        /// How the Steam Workshop is ordered.
        /// </summary>
        public Setting<QuerySort> SteamWorkshopOrderby { get; set; }

        /// <summary>
        /// If items from friends should only appear.
        /// </summary>
        public Setting<bool> SteamWorkshopFriendsOnly { get; set; }

        /// <summary>
        /// If items from followed users should only appear.
        /// </summary>
        public Setting<bool> SteamWorkshopFollowingOnly { get; set; }

        /// <summary>
        /// If favorited items should only appear.
        /// </summary>
        public Setting<bool> SteamWorkshopFavoritedOnly { get; set; }

        #endregion

        #endregion

        /// <summary>
        /// Bind the individual settings of the config.
        /// </summary>
        public override void BindSettings()
        {
            Load();

            #region Level

            CurrentLevelMode = Bind(this, LEVEL, "Level Mode", 0, "If a modes.lsms exists in the arcade level folder that you're loading, it will list other level modes (think easy mode, cutscene mode, hard mode, etc). The value in this config is for choosing which mode gets loaded. 0 is the default level.lsb or level.vgd.");
            OpenOnlineLevelAfterDownload = Bind(this, "Level", "Open After Download", true, "If the Play Level Menu should open once the level has finished downloading.");
            LocalLevelsPath = Bind(this, LEVEL, "Arcade Path in Beatmaps", "arcade", "The location of your local arcade folder.");
            LoadSteamLevels = Bind(this, LEVEL, "Load Steam Levels After Local Loaded", true, "If subscribed Steam levels should load after the local levels have loaded.");
            QueuePlaysLevel = Bind(this, LEVEL, "Play First Queued", false, "If enabled, the game will immediately load into the first queued level, otherwise it will open it in the Play Level Menu.");
            ShuffleQueueAmount = Bind(this, LEVEL, "Shuffle Queue Amount", 5, "How many levels should be added to the Queue.", 1, 50);
            LoadYieldMode = BindEnum(this, LEVEL, "Load Yield Mode", YieldType.FixedUpdate, "The yield instruction used for spacing out levels being loaded in the Arcade. Some options will load faster but freeze the game, while others load slower but allow you to see them loading in the menus.");
            LoadFolders = Bind(this, LEVEL, "Load Folders", false, "If folders containing levels should appear.");

            #endregion

            #region UI

            LoadingBackRoundness = Bind(this, UI, "Loading Back Roundness", 2, "The roundness of the loading screens' back.", 0, 5);
            LoadingIconRoundness = Bind(this, UI, "Loading Icon Roundness", 1, "The roundness of the loading screens' icon.", 0, 5);
            LoadingBarRoundness = Bind(this, UI, "Loading Bar Roundness", 1, "The roundness of the loading screens' loading bar.", 0, 5);

            OnlyShowShineOnSelected = Bind(this, UI, "Only Show Shine on Selected", true, "If the SS rank shine should only show on the current selected level with an SS rank or on all levels with an SS rank.");
            ShineSpeed = Bind(this, UI, "SS Rank Shine Speed", 0.7f, "How fast the shine goes by.", 0.1f, 3f);
            ShineMaxDelay = Bind(this, UI, "SS Rank Shine Max Delay", 0.6f, "The max time the shine delays.", 0.1f, 3f);
            ShineMinDelay = Bind(this, UI, "SS Rank Shine Min Delay", 0.2f, "The min time the shine delays.", 0.1f, 3f);
            ShineColor = Bind(this, UI, "SS Rank Shine Color", new Color(1f, 0.933f, 0.345f, 1f), "The color of the shine.");

            #endregion

            #region Sorting

            AutoSearch = Bind(this, SORTING, "Auto Search", true, "If searching should automatically run when sort, ascend, etc is changed.");

            LocalLevelOrderby = BindEnum(this, SORTING, "Local Orderby", LevelSort.Cover, "How the level list is ordered.");
            LocalLevelAscend = Bind(this, SORTING, "Local Ascend", true, "If the level order should be up or down.");

            OnlineLevelOrderby = BindEnum(this, SORTING, "Online Level Orderby", OnlineLevelSort.DatePublished, "How the online level list is ordered.");
            OnlineLevelAscend = Bind(this, SORTING, "Online Level Ascend", true, "If the online level order should be up or down.");

            OnlineLevelCollectionOrderby = BindEnum(this, SORTING, "Online Level Collection Orderby", OnlineLevelCollectionSort.DatePublished, "How the online level collection list is ordered.");
            OnlineLevelCollectionAscend = Bind(this, SORTING, "Online Level Collection Ascend", true, "If the online level collection order should be up or down.");

            SteamLevelOrderby = BindEnum(this, SORTING, "Steam Orderby", LevelSort.Cover, "How the Steam level list is ordered.");
            SteamLevelAscend = Bind(this, SORTING, "Steam Ascend", true, "If the Steam level order should be up or down.");

            SteamWorkshopOrderby = BindEnum(this, SORTING, "Steam Workshop Orderby", QuerySort.None, "How the Steam Workshop search should be sorted.");
            SteamWorkshopFriendsOnly = Bind(this, SORTING, "Steam Workshop Friends Only", false, "If items from friends should only appear.");
            SteamWorkshopFollowingOnly = Bind(this, SORTING, "Steam Workshop Following Only", false, "If items from followed users should only appear.");
            SteamWorkshopFavoritedOnly = Bind(this, SORTING, "Steam Workshop Favorited Only", false, "If favorited items should only appear.");

            #endregion

            Save();
        }

        #region Settings Changed

        public override void SetupSettingChanged()
        {
            CurrentLevelMode.SettingChanged += CurrentLevelModeChanged;

            LocalLevelOrderby.SettingChanged += LocalLevelSortChanged;
            LocalLevelAscend.SettingChanged += LocalLevelSortChanged;

            SteamLevelOrderby.SettingChanged += SteamLevelSortChanged;
            SteamLevelAscend.SettingChanged += SteamLevelSortChanged;

            SteamWorkshopOrderby.SettingChanged += SteamLevelSortChanged;
            SteamWorkshopFriendsOnly.SettingChanged += SteamLevelSortChanged;
            SteamWorkshopFollowingOnly.SettingChanged += SteamLevelSortChanged;
            SteamWorkshopFavoritedOnly.SettingChanged += SteamLevelSortChanged;

            LocalLevelsPath.SettingChanged += LocalLevelsPathChanged;
        }

        void SteamLevelSortChanged()
        {
            SteamWorkshopManager.inst.Levels = LevelManager.SortLevels(SteamWorkshopManager.inst.Levels, SteamLevelOrderby.Value, SteamLevelAscend.Value);

            if (ArcadeMenu.Current && ArcadeMenu.CurrentTab == ArcadeMenu.Tab.Steam)
            {
                if (ArcadeMenu.ViewOnline)
                {
                    ArcadeMenu.Current.SetOnlineSteamLevelsPage(0);
                    return;
                }

                ArcadeMenu.Pages[(int)ArcadeMenu.Tab.Steam] = 0;
                ArcadeMenu.Current.RefreshSubscribedSteamLevels(true, true);
            }
        }

        void LocalLevelSortChanged()
        {
            LevelManager.Sort(LocalLevelOrderby.Value, LocalLevelAscend.Value);

            if (ArcadeMenu.Current && ArcadeMenu.CurrentTab == ArcadeMenu.Tab.Local)
            {
                ArcadeMenu.Pages[(int)ArcadeMenu.Tab.Local] = 0;
                ArcadeMenu.Current.RefreshLocalLevels(true);
            }
        }

        void CurrentLevelModeChanged() => LevelManager.CurrentLevelMode = CurrentLevelMode.Value;

        void LocalLevelsPathChanged() => LevelManager.Path = LocalLevelsPath.Value;

        #endregion

        #region Sections

        public const string LEVEL = "Level";
        public const string UI = "UI";
        public const string SORTING = "Sorting";

        #endregion
    }
}
