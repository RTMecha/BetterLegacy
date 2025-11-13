using UnityEngine;

using BetterLegacy.Core;
using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using BetterLegacy.Core.Runtime;

namespace BetterLegacy.Configs
{
    /// <summary>
    /// Core Config for PA Legacy. Based on the RTFunctions mod.
    /// </summary>
    public class CoreConfig : BaseConfig
    {
        public static CoreConfig Instance { get; set; }

        public CoreConfig() : base(nameof(CoreConfig)) // Set config name via base("")
        {
            Instance = this;
            BindSettings();

            SetupSettingChanged();
        }

        public override string TabName => "Core";
        public override Color TabColor => new Color(0.18f, 0.4151f, 1f, 1f);
        public override string TabDesc => "The main systems of PA Legacy.";

        #region Settings

        #region Default Settings

        public Setting<KeyCode> FullscreenKey { get; set; }

        /// <summary>
        /// If game window should cover the entire screen or not.
        /// </summary>
        public Setting<bool> Fullscreen { get; set; }

        /// <summary>
        /// If fullscreen should behave as a maximized window without a border.
        /// </summary>
        public Setting<bool> BorderlessFullscreen { get; set; }

        /// <summary>
        /// The size of the game window in pixels.
        /// </summary>
        public Setting<ResolutionType> Resolution { get; set; }

        /// <summary>
        /// If FPS matches your monitors refresh rate.
        /// </summary>
        public Setting<bool> VSync { get; set; }

        /// <summary>
        /// The amount the FPS is limited to. If the number is -1, it is unlimited.
        /// </summary>
        public Setting<int> FPSLimit { get; set; }

        /// <summary>
        /// Customizes the type of loading bar that is used.
        /// </summary>
        public Setting<LoadingDisplayType> LoadingDisplayType { get; set; }

        /// <summary>
        /// Total volume.
        /// </summary>
        public Setting<int> MasterVol { get; set; }

        /// <summary>
        /// Music volume.
        /// </summary>
        public Setting<int> MusicVol { get; set; }

        /// <summary>
        /// SFX volume.
        /// </summary>
        public Setting<int> SFXVol { get; set; }

        /// <summary>
        /// The language the game is in.
        /// </summary>
        public Setting<Language> Language { get; set; }

        /// <summary>
        /// If the controllers should vibrate.
        /// </summary>
        public Setting<bool> ControllerRumble { get; set; }

        #endregion

        #region Game

        /// <summary>
        /// Forces all physics related things to match your FPS.
        /// </summary>
        public Setting<bool> PhysicsUpdateMatchFramerate { get; set; }

        /// <summary>
        /// The player will not move while an InputField is being used with this off.
        /// </summary>
        public Setting<bool> AllowControlsInputField { get; set; }

        /// <summary>
        /// The path to save screenshots to.
        /// </summary>
        public Setting<string> ScreenshotsPath { get; set; }

        /// <summary>
        /// The key to press to take a screenshot.
        /// </summary>
        public Setting<KeyCode> ScreenshotKey { get; set; }

        /// <summary>
        /// The key to press to open the Config Manager.
        /// </summary>
        public Setting<KeyCode> OpenConfigKey { get; set; }

        /// <summary>
        /// If anti-aliasing is on or not.
        /// </summary>
        public Setting<bool> AntiAliasing { get; set; }

        /// <summary>
        /// If you want the game to continue playing when minimized.
        /// </summary>
        public Setting<bool> RunInBackground { get; set; }

        /// <summary>
        /// If text objects should display custom formatting such as levelRank. Currently very unoptimized, so it's off by default.
        /// </summary>
        public Setting<bool> AllowCustomTextFormatting { get; set; }

        /// <summary>
        /// Increases the clip panes to a very high amount, allowing for object render depth to go really high or really low. Off is the unmodded setting.
        /// </summary>
        public Setting<bool> IncreasedClipPlanes { get; set; }

        /// <summary>
        /// If custom written code should evaluate. Turn this on if you're sure the level you're using isn't going to mess anything up with a code Modifier or custom player code.
        /// </summary>
        public Setting<bool> EvaluateCode { get; set; }

        /// <summary>
        /// If resuming the game starts a countdown. With this off, the game immediately unpauses.
        /// </summary>
        public Setting<bool> PlayPauseCountdown { get; set; }

        /// <summary>
        /// If the pause menu should display a slider that can set the level time. Only displays in zen mode.
        /// </summary>
        public Setting<bool> ShowPauseTimeSlider { get; set; }

        #endregion

        #region User

        /// <summary>
        /// Sets the username to show in levels and menus.
        /// </summary>
        public Setting<string> DisplayName { get; set; }

        #endregion

        #region File

        /// <summary>
        /// Opens the folder containing the Project Arrhythmia application and all files related to it.
        /// </summary>
        public Setting<KeyCode> OpenPAFolder { get; set; }

        /// <summary>
        /// Opens the data folder all instances of PA share containing the log files and global editor data.
        /// </summary>
        public Setting<KeyCode> OpenPAPersistentFolder { get; set; }

        /// <summary>
        /// Where the story mode JSON file is located. Only change this if you know what you're doing.
        /// </summary>
        public Setting<string> StoryFile { get; set; }

        /// <summary>
        /// If the story mode levels are loaded via the editor path or not.
        /// </summary>
        public Setting<bool> StoryEditorMode { get; set; }

        #endregion

        #region Level

        /// <summary>
        /// The current seed randomization a level uses. Leave empty to randomize the seed each time you play a level.
        /// </summary>
        public Setting<string> Seed { get; set; }

        /// <summary>
        /// If the random systems should use the current seed instead of the original random method. If randomization breaks, feel free to turn this setting off.
        /// </summary>
        public Setting<bool> UseSeedBasedRandom { get; set; }

        /// <summary>
        /// If enabled, any objects with "LDM" (Low Detail Mode) toggled on will not be rendered.
        /// </summary>
        public Setting<bool> LDM { get; set; }

        /// <summary>
        /// Custom game pitch.
        /// </summary>
        public Setting<GameSpeed> GameSpeedSetting { get; set; }

        /// <summary>
        /// Custom challenge mode.
        /// </summary>
        public Setting<ChallengeMode> ChallengeModeSetting { get; set; }

        /// <summary>
        /// If enabled, the Background Objects will render. Otherwise, they will be hidden and will boost performance.
        /// </summary>
        public Setting<bool> ShowBackgroundObjects { get; set; }

        /// <summary>
        /// If on, the old video BG feature returns, though somewhat buggy. Requires a bg.mp4 or bg.mov file to exist in the level folder.
        /// </summary>
        public Setting<bool> EnableVideoBackground { get; set; }

        /// <summary>
        /// Possibly releases the fixed framerate of the game.
        /// </summary>
        public Setting<bool> UseNewUpdateMethod { get; set; }

        /// <summary>
        /// When completing a level, having this on will replay the level with no players in the background of the end screen.
        /// </summary>
        public Setting<bool> ReplayLevel { get; set; }

        /// <summary>
        /// Due to LS file formats also being in level folders with VG formats, VG format will need to be prioritized, though you can turn this off if a VG level isn't working and it has a level.lsb file.
        /// </summary>
        public Setting<bool> PrioritizeVG { get; set; }

        /// <summary>
        /// When parsing a level, it will automatically try to apply as many optimizations to itself as possible changing how the level works.
        /// </summary>
        public Setting<bool> ParseOptimizations { get; set; }

        /// <summary>
        /// If the checkpoint sound should play.
        /// </summary>
        public Setting<bool> PlayCheckpointSound { get; set; }
        
        /// <summary>
        /// If the checkpoint animation should play.
        /// </summary>
        public Setting<bool> PlayCheckpointAnimation { get; set; }

        /// <summary>
        /// If recently opened / saved levels in the editor / arcade are saved to a stats.json file. Good for remembering what you did recently.
        /// </summary>
        public Setting<bool> StoreRecentLevels { get; set; }

        #endregion

        #region Discord

        /// <summary>
        /// If level name is shown in your Discord status.
        /// </summary>
        public Setting<bool> DiscordShowLevel { get; set; }

        /// <summary>
        /// If the Discord status timestamp should update every time you load a level.
        /// </summary>
        public Setting<bool> DiscordTimestampUpdatesPerLevel { get; set; }

        /// <summary>
        /// Only change if you already have your own custom Discord status setup.
        /// </summary>
        public Setting<string> DiscordRichPresenceID { get; set; }

        #endregion

        #region Cursor

        /// <summary>
        /// How long the cursor should be visible when temporarily shown.
        /// </summary>
        public Setting<float> CursorVisibleTime { get; set; }
        /// <summary>
        /// If the cursor in the game / editor preview can show when the user moves their mouse.
        /// </summary>
        public Setting<bool> GameCursorCanShow { get; set; }
        /// <summary>
        /// If the cursor in the editor should always be visible and not hide after a few seconds.
        /// </summary>
        public Setting<bool> EditorCursorAlwaysVisible { get; set; }

        #endregion

        #region Debugging

        /// <summary>
        /// If disabled, turns all Unity debug logs off. Might boost performance.
        /// </summary>
        public Setting<bool> DebugsOn { get; set; }

        /// <summary>
        /// Shows a helpful info overlay with some information about the current gamestate.
        /// </summary>
        public Setting<bool> DebugInfo { get; set; }

        /// <summary>
        /// If the Debug Info menu should be created on game start. Requires restart to have this option take affect.
        /// </summary>
        public Setting<bool> DebugInfoStartup { get; set; }

        /// <summary>
        /// If the Debug Info menu should only display FPS.
        /// </summary>
        public Setting<bool> DebugShowOnlyFPS { get; set; }

        /// <summary>
        /// Shows a helpful info overlay with some information about the current gamestate.
        /// </summary>
        public Setting<KeyCode> DebugInfoToggleKey { get; set; }

        /// <summary>
        /// The position the Debug Info menu is at.
        /// </summary>
        public Setting<Vector2> DebugPosition { get; set; }

        /// <summary>
        /// If in editor, code ran will have their results be notified.
        /// </summary>
        public Setting<bool> NotifyREPL { get; set; }

        public Setting<string> ArcadeServerURL { get; set; }

        #endregion

        #endregion

        /// <summary>
        /// Bind the individual settings of the config.
        /// </summary>
        public override void BindSettings()
        {
            Load();

            #region Settings

            OpenConfigKey = BindEnum(this, SETTINGS, "Open Config Key", KeyCode.F12, "The key to press to open the Config Manager.");
            FullscreenKey = BindEnum(this, SETTINGS, "Fullscreen Key", KeyCode.F11, "The key to toggle fullscreen.");
            Fullscreen = Bind(this, SETTINGS, "Fullscreen", false, "If game window should cover the entire screen or not.");
            BorderlessFullscreen = Bind(this, SETTINGS, "Borderless Fullscreen", false, "If fullscreen should behave as a maximized window without a border.");
            Resolution = Bind(this, SETTINGS, "Resolution", ResolutionType.p720, "The size of the game window in pixels.");
            VSync = Bind(this, SETTINGS, "VSync", true, "If FPS matches your monitors refresh rate.");
            FPSLimit = Bind(this, SETTINGS, "FPS Limit", -1, "The amount the FPS is limited to. If the number is -1, it is unlimited.");
            MasterVol = Bind(this, SETTINGS, "Volume Master", 8, "Total volume.", 0, 9);
            MusicVol = Bind(this, SETTINGS, "Volume Music", 9, "Music volume.", 0, 9);
            SFXVol = Bind(this, SETTINGS, "Volume SFX", 9, "SFX volume.", 0, 9);
            Language = BindEnum(this, SETTINGS, "Language", BetterLegacy.Language.English, "The language the game is in.");
            ControllerRumble = Bind(this, SETTINGS, "Controller Vibrate", true, "If the controllers should vibrate.");
            LoadingDisplayType = BindEnum(this, SETTINGS, "Loading Display Type", BetterLegacy.LoadingDisplayType.EqualsBar, "Customizes the type of loading bar that is used.");

            #endregion

            #region Game

            PhysicsUpdateMatchFramerate = Bind(this, GAME, "Physics Update Match Framerate", false, "Forces all physics related things to match your FPS.");
            AllowControlsInputField = Bind(this, GAME, "Allow Controls While Using InputField", true, "The player will not move while an InputField is being used with this off.");
            AntiAliasing = Bind(this, GAME, "Anti-Aliasing", true, "If anti-aliasing is on or not.");
            RunInBackground = Bind(this, GAME, "Run In Background", true, "If you want the game to continue playing when minimized.");
            AllowCustomTextFormatting = Bind(this, "Game", "Allow Custom Text Formatting", false, "If text objects should display custom formatting such as levelRank. Currently very unoptimized, so it's off by default.");
            IncreasedClipPlanes = Bind(this, GAME, "Increase Camera Clip Planes", true, "Increases the clip panes to a very high amount, allowing for object render depth to go really high or really low. Off is the unmodded setting.");
            EvaluateCode = Bind(this, GAME, "Evaluate Custom Code", false, "If custom written code should evaluate. Turn this on if you're sure the level you're using isn't going to mess anything up with a code Modifier or custom player code.");
            PlayPauseCountdown = Bind(this, GAME, "Play Pause Countdown", true, "If resuming the game starts a countdown. With this off, the game immediately unpauses.");
            ShowPauseTimeSlider = Bind(this, GAME, "Show Pause Time Slider", false, "If the pause menu should display a slider that can set the level time. Only displays in zen mode. (only recommended for animation levels)");

            #endregion

            #region User

            DisplayName = Bind(this, USER, "Display Name", "Player", "Sets the username to show in levels and menus.");

            #endregion

            #region File

            ScreenshotsPath = Bind(this, FILE, "Screenshot Path", "screenshots", "The path to save screenshots to.");
            ScreenshotKey = BindEnum(this, FILE, "Screenshot Key", KeyCode.F2, "The key to press to take a screenshot.");
            OpenPAFolder = BindEnum(this, FILE, "Open Project Arrhythmia Folder", KeyCode.F4, "Opens the folder containing the Project Arrhythmia application and all files related to it.");
            OpenPAPersistentFolder = BindEnum(this, FILE, "Open LocalLow Folder", KeyCode.F5, "Opens the data folder all instances of PA share containing the log files and global editor data.");
            StoryFile = Bind(this, FILE, "Story File", "story.json", "Where the story mode JSON file is located. Only change this if you know what you're doing.");
            StoryEditorMode = Bind(this, FILE, "Story Editor Mode", false, "If the story mode levels are loaded via the editor path or not.");

            #endregion

            #region Level

            Seed = Bind(this, LEVEL, "Seed", string.Empty, "The current seed randomization a level uses. Leave empty to randomize the seed each time you play a level.");
            UseSeedBasedRandom = Bind(this, LEVEL, "Use Seed Based Random", true, "If the random systems should use the current seed instead of the original random method. If randomization breaks, feel free to turn this setting off.");
            LDM = Bind(this, LEVEL, "Low Detail Mode", false, "If enabled, any objects with \"LDM\" (Low Detail Mode) toggled on will not be rendered.");
            GameSpeedSetting = Bind(this, LEVEL, "Game Speed", GameSpeed.X1_0, "Custom game pitch.");
            ChallengeModeSetting = Bind(this, LEVEL, "Challenge Mode", ChallengeMode.Normal, "Custom challenge mode that affects gameplay.\n" +
                "<b>Zen</b>: No damage is taken.\n" +
                "<b>Practice</b>: Damage is taken, but health is not subtracted so the Player will not die.\n" +
                "<b>Normal</b>: Damage is taken and health is subtracted.\n" +
                "<b>1 Life</b>: The level restarts when all Players are dead.\n" +
                "<b>1 Hit</b>: The level restarts when any Player takes damage.");
            ShowBackgroundObjects = Bind(this, LEVEL, "Show Background Objects", true, "If enabled, the Background Objects will render. Otherwise, they will be hidden and will boost performance.");
            EnableVideoBackground = Bind(this, LEVEL, "Video Backgrounds", true, "If on, the old video BG feature returns, though somewhat buggy. Requires a bg.mp4 or bg.mov file to exist in the level folder.");
            UseNewUpdateMethod = Bind(this, LEVEL, "Use New Update Method", true, "Possibly releases the fixed framerate of the game.");
            ReplayLevel = Bind(this, LEVEL, "Replay Level in Background After Completion", true, "When completing a level, having this on will replay the level with no players in the background of the end screen.");
            PrioritizeVG = Bind(this, LEVEL, "Priotize VG format", true, "Due to LS file formats also being in level folders with VG formats, VG format will need to be prioritized, though you can turn this off if a VG level isn't working and it has a level.lsb file.");
            ParseOptimizations = Bind(this, LEVEL, "Parse Optimizations", false, "When parsing a level, it will automatically try to apply as many optimizations to itself as possible changing how the level works.");
            PlayCheckpointSound = Bind(this, LEVEL, "Play Checkpoint Sound", true, "If the checkpoint sound should play.");
            PlayCheckpointAnimation = Bind(this, LEVEL, "Play Checkpoint Animation", true, "If the checkpoint animation should play.");
            StoreRecentLevels = Bind(this, LEVEL, "Store Recent Levels", false, "If recently opened / saved levels in the editor / arcade are saved to a stats.json file. Good for remembering what you did recently.");

            #endregion

            #region Discord

            DiscordShowLevel = Bind(this, DISCORD, "Show Level Status", true, "If level name is shown in your Discord status.");
            DiscordTimestampUpdatesPerLevel = Bind(this, DISCORD, "Timestamp Updates Per Level", false, "If the Discord status timestamp should update every time you load a level.");
            DiscordRichPresenceID = Bind(this, DISCORD, "Status ID (READ DESC)", "1176264603374735420", "Only change if you already have your own custom Discord status setup.");

            #endregion

            #region Cursor

            CursorVisibleTime = Bind(this, CURSOR, "Cursor Visible Time", 1f, "How long the cursor should be visible when temporarily shown.", 0f, 60f);
            GameCursorCanShow = Bind(this, CURSOR, "Game Cursor Can Show", false, "If the cursor in the game / editor preview can show when the user moves their mouse.");
            EditorCursorAlwaysVisible = Bind(this, CURSOR, "Editor Cursor Always Visible", true, "If the cursor in the editor should always be visible and not hide after a few seconds.");

            #endregion

            #region Debugging

            DebugsOn = Bind(this, DEBUGGING, "Enabled", true, "If disabled, turns all Unity debug logs off. Might boost performance.");
            DebugInfo = Bind(this, DEBUGGING, "Show Debug Info", false, "Shows a helpful info overlay with some information about the current gamestate.");
            DebugInfoStartup = Bind(this, DEBUGGING, "Create Debug Info", false, "If the Debug Info menu should be created on game start. Requires restart to have this option take affect.");
            DebugInfoToggleKey = BindEnum(this, DEBUGGING, "Show Debug Info Toggle Key", KeyCode.F6, "Shows a helpful info overlay with some information about the current gamestate.");
            DebugShowOnlyFPS = Bind(this, DEBUGGING, "Show Only FPS", true, "If the Debug Info menu should only display FPS.");
            DebugPosition = Bind(this, DEBUGGING, "Debug Info Position", new Vector2(10f, 1080f), "The position the Debug Info menu is at.");
            NotifyREPL = Bind(this, DEBUGGING, "Notify REPL", false, "If in editor, code ran will have their results be notified.");
            ArcadeServerURL = Bind(this, DEBUGGING, "Arcade Server URL", "https://betterlegacy.net/", "The link to the Arcade Server. This is only for debugging purposes and as such is not to be changed unless you are debugging the Arcade server.");

            #endregion

            Save();
        }

        #region Settings Changed

        public override void SetupSettingChanged()
        {
            SettingChanged += UpdateSettings;
            DisplayName.SettingChanged += DisplayNameChanged;
            Fullscreen.SettingChanged += ResolutionChanged;
            BorderlessFullscreen.SettingChanged += ResolutionChanged;
            Resolution.SettingChanged += ResolutionChanged;
            MasterVol.SettingChanged += SFXVolumeChanged;
            MusicVol.SettingChanged += MusicVolumeChanged;
            SFXVol.SettingChanged += SFXVolumeChanged;
            LDM.SettingChanged += LDMChanged;
            ChallengeModeSetting.SettingChanged += ChallengeModeChanged;
            ShowBackgroundObjects.SettingChanged += ShowBackgroundObjectsChanged;
            DiscordShowLevel.SettingChanged += DiscordChanged;
            DiscordRichPresenceID.SettingChanged += DiscordChanged;
            DebugInfoStartup.SettingChanged += DebugInfoChanged;

            VSync.SettingChanged += FPSChanged;
            FPSLimit.SettingChanged += FPSChanged;

            StoryFile.SettingChanged += Story.StoryMode.Init;

            CursorManager.onScreenTime = CursorVisibleTime.Value;
            CursorVisibleTime.SettingChanged += OnCursorChanged;

            Seed.SettingChanged += SeedChanged;
            UseSeedBasedRandom.SettingChanged += SeedChanged;
        }

        void SeedChanged()
        {
            if (CoreHelper.InEditor)
                RTLevel.Current.InitSeed();
        }

        void OnCursorChanged() => CursorManager.onScreenTime = CursorVisibleTime.Value;

        void FPSChanged()
        {
            ProjectArrhythmia.Window.ApplySettings();
        }

        void DebugInfoChanged() => Core.Managers.DebugInfo.Init();

        void DiscordChanged()
        {
            CoreHelper.UpdateValue(DiscordController.inst.applicationId, DiscordRichPresenceID.Value, x =>
            {
                DiscordController.inst.OnDisableDiscord();
                DiscordController.inst.applicationId = x;
                DiscordController.inst.enabled = false;
                DiscordController.inst.enabled = true;
            });
            CoreHelper.UpdateDiscordStatus(CoreHelper.discordLevel, CoreHelper.discordDetails, CoreHelper.discordIcon, CoreHelper.discordArt);
        }

        void LDMChanged()
        {
            if (!CoreHelper.InEditor || !GameData.Current)
                return;

            var list = GameData.Current.beatmapObjects.FindAll(x => x.LDM);
            for (int i = 0; i < list.Count; i++)
                list[i].GetParentRuntime()?.UpdateObject(list[i]);
        }

        void ChallengeModeChanged()
        {
            if (!CoreHelper.InEditor)
                return;

            RTBeatmap.Current?.Reset(EditorConfig.Instance.ApplyGameSettingsInPreviewMode.Value);

            if (RTBeatmap.Current && !EditorConfig.Instance.ApplyGameSettingsInPreviewMode.Value)
            {
                RTBeatmap.Current.challengeMode = ChallengeMode.Normal;
                RTBeatmap.Current.gameSpeed = GameSpeed.X1_0;
            }

            Editor.Managers.RTEditor.inst.UpdatePlayers();
        }

        void ShowBackgroundObjectsChanged() => RTLevel.Current?.UpdateBackgroundObjects();

        void ResolutionChanged() => ProjectArrhythmia.Window.ApplySettings();

        void DisplayNameChanged()
        {
            LegacyPlugin.player.sprName = DisplayName.Value;

            if (SteamWrapper.inst)
                SteamWrapper.inst.user.displayName = DisplayName.Value;

            if (CoreHelper.InEditor)
                EditorManager.inst.SetCreatorName(DisplayName.Value);

            LegacyPlugin.SaveProfile();
        }

        void MusicVolumeChanged() => SoundManager.inst.PlaySound(DefaultSounds.UpDown, (MusicVol.Value / 9f) * (MasterVol.Value / 9f));

        void SFXVolumeChanged() => SoundManager.inst.PlaySound(DefaultSounds.UpDown);

        static void UpdateSettings()
        {
            Debug.unityLogger.logEnabled = Instance.DebugsOn.Value;

            CoreHelper.SetCameraRenderDistance();
            CoreHelper.SetAntiAliasing();

            if (RTVideoManager.inst && RTVideoManager.inst.didntPlay && Instance.EnableVideoBackground.Value)
                RTVideoManager.inst.Play(RTVideoManager.inst.currentURL, RTVideoManager.inst.currentAlpha);
        }

        #endregion

        #region Sections

        public const string SETTINGS = "Settings";
        public const string GAME = "Game";
        public const string USER = "User";
        public const string FILE = "File";
        public const string LEVEL = "Level";
        public const string DISCORD = "Discord";
        public const string CURSOR = "Cursor";
        public const string DEBUGGING = "Debugging";

        #endregion
    }
}
