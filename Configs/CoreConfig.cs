using BepInEx.Configuration;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using BetterLegacy.Core.Optimization;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BetterLegacy.Configs
{
    /// <summary>
    /// Core Config for PA Legacy. Based on the RTFunctions mod.
    /// </summary>
    public class CoreConfig : BaseConfig
    {
        public static CoreConfig Instance { get; set; }

        public override ConfigFile Config { get; set; }

        public Dictionary<string, ConfigEntryBase> defaultSettings = new Dictionary<string, ConfigEntryBase>();

        public CoreConfig(ConfigFile config) : base(config)
        {
            Instance = this;
            Config = config;

            #region Debugging

            DebugsOn = Config.Bind("Core - Debugging", "Enabled", true, "If disabled, turns all Unity debug logs off. Might boost performance.");
            DebugInfo = Config.Bind("Core - Debugging", "Show Debug Info", false, "Shows a helpful info overlay with some information about the current gamestate.");
            DebugInfoStartup = Config.Bind("Core - Debugging", "Create Debug Info", false, "If the Debug Info menu should be created on game start. Requires restart to have this option take affect.");
            DebugInfoToggleKey = Config.Bind("Core - Debugging", "Show Debug Info Toggle Key", KeyCode.F6, "Shows a helpful info overlay with some information about the current gamestate.");
            NotifyREPL = Config.Bind("Core - Debugging", "Notify REPL", false, "If in editor, code ran will have their results be notified.");

            #endregion

            #region Game

            AllowControlsInputField = Config.Bind("Core - Game", "Allow Controls While Using InputField", true, "The player will not move while an InputField is being used with this off.");
            UseNewUpdateMethod = Config.Bind("Core - Game", "Use New Update Method", true, "Possibly releases the fixed framerate of the game.");
            ScreenshotsPath = Config.Bind("Core - Game", "Screenshot Path", "screenshots", "The path to save screenshots to.");
            ScreenshotKey = Config.Bind("Core - Game", "Screenshot Key", KeyCode.F2, "The key to press to take a screenshot.");
            AntiAliasing = Config.Bind("Core - Game", "Anti-Aliasing", true, "If anti-aliasing is on or not.");
            RunInBackground = Config.Bind("Core - Game", "Run In Background", true, "If you want the game to continue playing when minimized.");
            IncreasedClipPlanes = Config.Bind("Core - Game", "Increase Camera Clip Planes", true, "Increases the clip panes to a very high amount, allowing for object render depth to go really high or really low. Off is the unmodded setting.");
            EnableVideoBackground = Config.Bind("Core - Game", "Video Backgrounds", true, "If on, the old video BG feature returns, though somewhat buggy. Requires a bg.mp4 or bg.mov file to exist in the level folder.");
            EvaluateCode = Config.Bind("Core - Game", "Evaluate Custom Code", false, "If custom written code should evaluate. Turn this on if you're sure the level you're using isn't going to mess anything up with a code Modifier or custom player code.");
            ReplayLevel = Config.Bind("Core - Game", "Replay Level in Background After Completion", true, "When completing a level, having this on will replay the level with no players in the background of the end screen.");
            PrioritizeVG = Config.Bind("Core - Game", "Priotize VG format", true, "Due to LS file formats also being in level folders with VG formats, VG format will need to be prioritized, though you can turn this off if a VG level isn't working and it has a level.lsb file.");

            InterfaceBlurSize = Config.Bind("Core - Game", "Interface Blur Size", 3f, "The size of the in-game interface blur.");
            InterfaceBlurColor = Config.Bind("Core - Game", "Interface Blur Color", new Color(0.4f, 0.4f, 0.4f), "The color of the in-game interface blur.");

            #endregion

            #region User

            DisplayName = Config.Bind("Core - User", "Display Name", "Player", "Sets the username to show in levels and menus.");

            #endregion

            #region File

            OpenPAFolder = Config.Bind("Core - File", "Open Project Arrhythmia Folder", KeyCode.F4, "Opens the folder containing the Project Arrhythmia application and all files related to it.");
            OpenPAPersistentFolder = Config.Bind("Core - File", "Open LocalLow Folder", KeyCode.F5, "Opens the data folder all instances of PA share containing the log files and global editor data.");

            #endregion

            #region Level

            BGReactiveLerp = Config.Bind("Core - Level", "Reactive Color Lerp", true, "If on, reactive color will lerp from base color to reactive color. Otherwise, the reactive color will be added to the base color.");
            LDM = Config.Bind("Core - Level", "Low Detail Mode", false, "If enabled, any objects with \"LDM\" (Low Detail Mode) toggled on will not be rendered.");

            #endregion

            #region Discord

            DiscordShowLevel = Config.Bind("Core - Discord", "Show Level Status", true, "If level name is shown in your Discord status.");
            DiscordRichPresenceID = Config.Bind("Core - Discord", "Status ID (READ DESC)", "1176264603374735420", "Only change if you already have your own custom Discord status setup.");

            #endregion

            #region Settings

            Fullscreen = Config.Bind("Core - Settings", "Fullscreen", false, "If game window should cover the entire screen or not.");
            Resolution = Config.Bind("Core - Settings", "Resolution", Resolutions.p720, "The size of the game window in pixels.");
            MasterVol = Config.Bind("Core - Settings", "Volume Master", 8, new ConfigDescription("Total volume.", new AcceptableValueRange<int>(0, 9)));
            MusicVol = Config.Bind("Core - Settings", "Volume Music", 9, new ConfigDescription("Music volume.", new AcceptableValueRange<int>(0, 9)));
            SFXVol = Config.Bind("Core - Settings", "Volume SFX", 9, new ConfigDescription("SFX volume.", new AcceptableValueRange<int>(0, 9)));
            Language = Config.Bind("Core - Settings", "Language", BetterLegacy.Language.English, "The language the game is in.");
            ControllerRumble = Config.Bind("Core - Settings", "Controller Vibrate", true, "If the controllers should vibrate.");

            #endregion

            defaultSettings.Add("FullScreen", Fullscreen);
            defaultSettings.Add("Resolution", Resolution);
            defaultSettings.Add("MasterVolume", MasterVol);
            defaultSettings.Add("MusicVolume", MusicVol);
            defaultSettings.Add("EffectsVolume", SFXVol);
            defaultSettings.Add("ControllerVibrate", ControllerRumble);


            Updater.UseNewUpdateMethod = UseNewUpdateMethod.Value;

            SetupSettingChanged();
        }

        #region Configs

        #region Debugging

        /// <summary>
        /// If disabled, turns all Unity debug logs off. Might boost performance.
        /// </summary>
        public ConfigEntry<bool> DebugsOn { get; set; }

        /// <summary>
        /// Shows a helpful info overlay with some information about the current gamestate.
        /// </summary>
        public ConfigEntry<bool> DebugInfo { get; set; }

        /// <summary>
        /// If the Debug Info menu should be created on game start. Requires restart to have this option take affect.
        /// </summary>
        public ConfigEntry<bool> DebugInfoStartup { get; set; }

        /// <summary>
        /// Shows a helpful info overlay with some information about the current gamestate.
        /// </summary>
        public ConfigEntry<KeyCode> DebugInfoToggleKey { get; set; }

        /// <summary>
        /// If in editor, code ran will have their results be notified.
        /// </summary>
        public ConfigEntry<bool> NotifyREPL { get; set; }

        #endregion

        #region Game

        /// <summary>
        /// The player will not move while an InputField is being used with this off.
        /// </summary>
        public ConfigEntry<bool> AllowControlsInputField { get; set; }

        /// <summary>
        /// Possibly releases the fixed framerate of the game.
        /// </summary>
        public ConfigEntry<bool> UseNewUpdateMethod { get; set; }

        /// <summary>
        /// The path to save screenshots to.
        /// </summary>
        public ConfigEntry<string> ScreenshotsPath { get; set; }

        /// <summary>
        /// The key to press to take a screenshot.
        /// </summary>
        public ConfigEntry<KeyCode> ScreenshotKey { get; set; }

        /// <summary>
        /// If anti-aliasing is on or not.
        /// </summary>
        public ConfigEntry<bool> AntiAliasing { get; set; }

        /// <summary>
        /// If you want the game to continue playing when minimized.
        /// </summary>
        public ConfigEntry<bool> RunInBackground { get; set; }

        /// <summary>
        /// Increases the clip panes to a very high amount, allowing for object render depth to go really high or really low. Off is the unmodded setting.
        /// </summary>
        public ConfigEntry<bool> IncreasedClipPlanes { get; set; }

        /// <summary>
        /// If on, the old video BG feature returns, though somewhat buggy. Requires a bg.mp4 or bg.mov file to exist in the level folder.
        /// </summary>
        public ConfigEntry<bool> EnableVideoBackground { get; set; }

        /// <summary>
        /// If custom written code should evaluate. Turn this on if you're sure the level you're using isn't going to mess anything up with a code Modifier or custom player code.
        /// </summary>
        public ConfigEntry<bool> EvaluateCode { get; set; }

        /// <summary>
        /// When completing a level, having this on will replay the level with no players in the background of the end screen.
        /// </summary>
        public ConfigEntry<bool> ReplayLevel { get; set; }

        /// <summary>
        /// Due to LS file formats also being in level folders with VG formats, VG format will need to be prioritized, though you can turn this off if a VG level isn't working and it has a level.lsb file.
        /// </summary>
        public ConfigEntry<bool> PrioritizeVG { get; set; }

        /// <summary>
        /// The size of the in-game interface blur.
        /// </summary>
        public ConfigEntry<float> InterfaceBlurSize { get; set; }

        /// <summary>
        /// The color of the in-game interface blur.
        /// </summary>
        public ConfigEntry<Color> InterfaceBlurColor { get; set; }

        #endregion

        #region User

        /// <summary>
        /// Sets the username to show in levels and menus.
        /// </summary>
        public ConfigEntry<string> DisplayName { get; set; }

        #endregion

        #region File

        /// <summary>
        /// Opens the folder containing the Project Arrhythmia application and all files related to it.
        /// </summary>
        public ConfigEntry<KeyCode> OpenPAFolder { get; set; }

        /// <summary>
        /// Opens the data folder all instances of PA share containing the log files and global editor data.
        /// </summary>
        public ConfigEntry<KeyCode> OpenPAPersistentFolder { get; set; }

        #endregion

        #region Level

        /// <summary>
        /// If on, reactive color will lerp from base color to reactive color. Otherwise, the reactive color will be added to the base color.
        /// </summary>
        public ConfigEntry<bool> BGReactiveLerp { get; set; }

        /// <summary>
        /// If enabled, any objects with "LDM" (Low Detail Mode) toggled on will not be rendered.
        /// </summary>
        public ConfigEntry<bool> LDM { get; set; }

        #endregion

        #region Discord

        /// <summary>
        /// If level name is shown in your Discord status.
        /// </summary>
        public ConfigEntry<bool> DiscordShowLevel { get; set; }

        /// <summary>
        /// Only change if you already have your own custom Discord status setup.
        /// </summary>
        public ConfigEntry<string> DiscordRichPresenceID { get; set; }

        #endregion

        #region Default Settings

        /// <summary>
        /// If game window should cover the entire screen or not.
        /// </summary>
        public ConfigEntry<bool> Fullscreen { get; set; }

        /// <summary>
        /// The size of the game window in pixels.
        /// </summary>
        public ConfigEntry<Resolutions> Resolution { get; set; }

        /// <summary>
        /// Total volume.
        /// </summary>
        public ConfigEntry<int> MasterVol { get; set; }

        /// <summary>
        /// Music volume.
        /// </summary>
        public ConfigEntry<int> MusicVol { get; set; }

        /// <summary>
        /// SFX volume.
        /// </summary>
        public ConfigEntry<int> SFXVol { get; set; }

        /// <summary>
        /// The language the game is in.
        /// </summary>
        public ConfigEntry<Language> Language { get; set; }

        /// <summary>
        /// If the controllers should vibrate.
        /// </summary>
        public ConfigEntry<bool> ControllerRumble { get; set; }

        /// <summary>
        /// Updates fullscreen.
        /// </summary>
        /// <param name="value">Value to update.</param>
        void SetFullscreen(bool value)
        {
            prevFullscreen = Fullscreen.Value;

            DataManager.inst.UpdateSettingBool("FullScreen", value);
            SaveManager.inst.ApplyVideoSettings();
            SaveManager.inst.UpdateSettingsFile(false);
        }

        /// <summary>
        /// Updates resolution.
        /// </summary>
        /// <param name="value">Value to update.</param>
        void SetResolution(Resolutions value)
        {
            prevResolution = Resolution.Value;

            DataManager.inst.UpdateSettingInt("Resolution_i", (int)value);

            var res = DataManager.inst.resolutions[(int)value];

            DataManager.inst.UpdateSettingFloat("Resolution_x", res.x);
            DataManager.inst.UpdateSettingFloat("Resolution_y", res.y);

            SaveManager.inst.ApplyVideoSettings();
            SaveManager.inst.UpdateSettingsFile(false);
        }

        /// <summary>
        /// Updates master volume.
        /// </summary>
        /// <param name="value">Value to update.</param>
        void SetMasterVol(int value)
        {
            prevMasterVol = MasterVol.Value;

            DataManager.inst.UpdateSettingInt("MasterVolume", value);

            SaveManager.inst.UpdateSettingsFile(false);
        }

        /// <summary>
        /// Updates music volume.
        /// </summary>
        /// <param name="value">Value to update.</param>
        void SetMusicVol(int value)
        {
            prevMusicVol = MusicVol.Value;

            DataManager.inst.UpdateSettingInt("MusicVolume", value);

            SaveManager.inst.UpdateSettingsFile(false);
        }

        /// <summary>
        /// Updates sfx volume.
        /// </summary>
        /// <param name="value">Value to update.</param>
        void SetSFXVol(int value)
        {
            prevSFXVol = SFXVol.Value;

            DataManager.inst.UpdateSettingInt("EffectsVolume", value);

            SaveManager.inst.UpdateSettingsFile(false);
        }

        /// <summary>
        /// Updates language.
        /// </summary>
        /// <param name="value">Value to update.</param>
        void SetLanguage(Language value)
        {
            prevLanguage = Language.Value;

            DataManager.inst.UpdateSettingInt("Language_i", (int)value);

            SaveManager.inst.UpdateSettingsFile(false);
        }

        /// <summary>
        /// Updates controller rumble.
        /// </summary>
        /// <param name="value">Value to update.</param>
        void SetControllerRumble(bool value)
        {
            prevControllerRumble = ControllerRumble.Value;

            DataManager.inst.UpdateSettingBool("ControllerVibrate", value);

            SaveManager.inst.UpdateSettingsFile(false);
        }

        public bool prevFullscreen;

        public Resolutions prevResolution;

        public int prevMasterVol;

        public int prevMusicVol;

        public int prevSFXVol;

        public Language prevLanguage;

        public bool prevControllerRumble;

        #endregion

        #endregion

        public override void SetupSettingChanged()
        {
            UseNewUpdateMethod.SettingChanged += UseNewUpdateMethodChanged;
            InterfaceBlurSize.SettingChanged += InterfaceBlurChanged;
            InterfaceBlurColor.SettingChanged += InterfaceBlurChanged;
            DisplayName.SettingChanged += DisplayNameChanged;
            Fullscreen.SettingChanged += DefaultSettingsChanged;
            Resolution.SettingChanged += DefaultSettingsChanged;
            MasterVol.SettingChanged += DefaultSettingsChanged;
            MusicVol.SettingChanged += DefaultSettingsChanged;
            SFXVol.SettingChanged += DefaultSettingsChanged;
            Language.SettingChanged += DefaultSettingsChanged;
            ControllerRumble.SettingChanged += DefaultSettingsChanged;
            LDM.SettingChanged += LDMChanged;
            DiscordShowLevel.SettingChanged += DiscordChanged;
            Config.SettingChanged += new EventHandler<SettingChangedEventArgs>(UpdateSettings);
        }

        #region Settings Changed

        void InterfaceBlurChanged(object sender, EventArgs e)
        {
            if (GameStorageManager.inst && GameStorageManager.inst.guiBlur)
            {
                GameStorageManager.inst.guiBlur.material.SetFloat("_Size", InterfaceBlurSize.Value);
                GameStorageManager.inst.guiBlur.material.color = InterfaceBlurColor.Value;
            }
        }

        void DiscordChanged(object sender, EventArgs e)
        {
            CoreHelper.UpdateDiscordStatus(CoreHelper.discordLevel, CoreHelper.discordDetails, CoreHelper.discordIcon, CoreHelper.discordArt);
        }

        void LDMChanged(object sender, EventArgs e)
        {
            if (!EditorManager.inst)
                return;

            var list = GameData.Current.BeatmapObjects.Where(x => x.LDM).ToList();
            for (int i = 0; i < list.Count; i++)
            {
                Updater.UpdateProcessor(list[i]);
            }
        }

        void DefaultSettingsChanged(object sender, EventArgs e)
        {
            CoreHelper.UpdateValue(prevFullscreen, Fullscreen.Value, SetFullscreen);
            CoreHelper.UpdateValue(prevResolution, Resolution.Value, SetResolution);
            CoreHelper.UpdateValue(prevMasterVol, MasterVol.Value, SetMasterVol);
            CoreHelper.UpdateValue(prevMusicVol, MusicVol.Value, SetMusicVol);
            CoreHelper.UpdateValue(prevSFXVol, SFXVol.Value, SetSFXVol);
            CoreHelper.UpdateValue(prevControllerRumble, ControllerRumble.Value, SetControllerRumble);
            CoreHelper.UpdateValue(prevLanguage, Language.Value, SetLanguage);
        }

        void DisplayNameChanged(object sender, EventArgs e)
        {
            DataManager.inst.UpdateSettingString("s_display_name", DisplayName.Value);

            LegacyPlugin.player.sprName = DisplayName.Value;

            if (SteamWrapper.inst)
                SteamWrapper.inst.user.displayName = DisplayName.Value;

            if (EditorManager.inst)
                EditorManager.inst.SetCreatorName(DisplayName.Value);

            LegacyPlugin.SaveProfile();
        }

        void UseNewUpdateMethodChanged(object sender, EventArgs e) => Updater.UseNewUpdateMethod = UseNewUpdateMethod.Value;

        static void UpdateSettings(object sender, EventArgs e)
        {
            Debug.unityLogger.logEnabled = Instance.DebugsOn.Value;

            CoreHelper.SetCameraRenderDistance();
            CoreHelper.SetAntiAliasing();

            if (RTVideoManager.inst && RTVideoManager.inst.didntPlay && Instance.EnableVideoBackground.Value)
                RTVideoManager.inst.Play(RTVideoManager.inst.currentURL, RTVideoManager.inst.currentAlpha);

            LegacyPlugin.SaveProfile();
        }

        #endregion

        public override string ToString() => "Editor Config";
    }
}
