﻿using BepInEx.Configuration;
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

        public CoreConfig() : base("Core") // Set config name via base("")
        {
            Instance = this;
            BindSettings();

            defaultSettings.Add("FullScreen", Fullscreen);
            defaultSettings.Add("Resolution", Resolution);
            defaultSettings.Add("MasterVolume", MasterVol);
            defaultSettings.Add("MusicVolume", MusicVol);
            defaultSettings.Add("EffectsVolume", SFXVol);
            defaultSettings.Add("ControllerVibrate", ControllerRumble);

            Updater.UseNewUpdateMethod = UseNewUpdateMethod.Value;

            SetupSettingChanged();
        }

        public Dictionary<string, BaseSetting> defaultSettings = new Dictionary<string, BaseSetting>();

        #region Configs

        #region Default Settings

        public Setting<KeyCode> FullscreenKey { get; set; }

        /// <summary>
        /// If game window should cover the entire screen or not.
        /// </summary>
        public Setting<bool> Fullscreen { get; set; }

        /// <summary>
        /// The size of the game window in pixels.
        /// </summary>
        public Setting<Resolutions> Resolution { get; set; }

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

        #region Game

        /// <summary>
        /// The player will not move while an InputField is being used with this off.
        /// </summary>
        public Setting<bool> AllowControlsInputField { get; set; }

        /// <summary>
        /// Possibly releases the fixed framerate of the game.
        /// </summary>
        public Setting<bool> UseNewUpdateMethod { get; set; }

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
        /// Increases the clip panes to a very high amount, allowing for object render depth to go really high or really low. Off is the unmodded setting.
        /// </summary>
        public Setting<bool> IncreasedClipPlanes { get; set; }

        /// <summary>
        /// If on, the old video BG feature returns, though somewhat buggy. Requires a bg.mp4 or bg.mov file to exist in the level folder.
        /// </summary>
        public Setting<bool> EnableVideoBackground { get; set; }

        /// <summary>
        /// If custom written code should evaluate. Turn this on if you're sure the level you're using isn't going to mess anything up with a code Modifier or custom player code.
        /// </summary>
        public Setting<bool> EvaluateCode { get; set; }

        /// <summary>
        /// When completing a level, having this on will replay the level with no players in the background of the end screen.
        /// </summary>
        public Setting<bool> ReplayLevel { get; set; }

        /// <summary>
        /// Due to LS file formats also being in level folders with VG formats, VG format will need to be prioritized, though you can turn this off if a VG level isn't working and it has a level.lsb file.
        /// </summary>
        public Setting<bool> PrioritizeVG { get; set; }

        /// <summary>
        /// The size of the in-game interface blur.
        /// </summary>
        public Setting<float> InterfaceBlurSize { get; set; }

        /// <summary>
        /// The color of the in-game interface blur.
        /// </summary>
        public Setting<Color> InterfaceBlurColor { get; set; }

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

        #endregion

        #region Level

        /// <summary>
        /// If on, reactive color will lerp from base color to reactive color. Otherwise, the reactive color will be added to the base color.
        /// </summary>
        public Setting<bool> BGReactiveLerp { get; set; }

        /// <summary>
        /// If enabled, any objects with "LDM" (Low Detail Mode) toggled on will not be rendered.
        /// </summary>
        public Setting<bool> LDM { get; set; }

        #endregion

        #region Discord

        /// <summary>
        /// If level name is shown in your Discord status.
        /// </summary>
        public Setting<bool> DiscordShowLevel { get; set; }

        /// <summary>
        /// Only change if you already have your own custom Discord status setup.
        /// </summary>
        public Setting<string> DiscordRichPresenceID { get; set; }

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

        #endregion

        #endregion

        /// <summary>
        /// Bind the individual settings of the config.
        /// </summary>
        public override void BindSettings()
        {
            Load();

            #region Settings

            FullscreenKey = BindEnum(this, "Settings", "Fullscreen Key", KeyCode.F11, "The key to toggle fullscreen.");
            Fullscreen = Bind(this, "Settings", "Fullscreen", false, "If game window should cover the entire screen or not.");
            Resolution = BindEnum(this, "Settings", "Resolution", Resolutions.p720, "The size of the game window in pixels.");
            MasterVol = Bind(this, "Settings", "Volume Master", 8, "Total volume.", 0, 9);
            MusicVol = Bind(this, "Settings", "Volume Music", 9, "Music volume.", 0, 9);
            SFXVol = Bind(this, "Settings", "Volume SFX", 9, "SFX volume.", 0, 9);
            Language = BindEnum(this, "Settings", "Language", BetterLegacy.Language.English, "The language the game is in.");
            ControllerRumble = Bind(this, "Settings", "Controller Vibrate", true, "If the controllers should vibrate.");

            #endregion

            #region Game

            AllowControlsInputField = Bind(this, "Game", "Allow Controls While Using InputField", true, "The player will not move while an InputField is being used with this off.");
            OpenConfigKey = BindEnum(this, "Game", "Open Config Key", KeyCode.F12, "The key to press to open the Config Manager.");
            AntiAliasing = Bind(this, "Game", "Anti-Aliasing", true, "If anti-aliasing is on or not.");
            RunInBackground = Bind(this, "Game", "Run In Background", true, "If you want the game to continue playing when minimized.");
            IncreasedClipPlanes = Bind(this, "Game", "Increase Camera Clip Planes", true, "Increases the clip panes to a very high amount, allowing for object render depth to go really high or really low. Off is the unmodded setting.");
            EvaluateCode = Bind(this, "Game", "Evaluate Custom Code", false, "If custom written code should evaluate. Turn this on if you're sure the level you're using isn't going to mess anything up with a code Modifier or custom player code.");

            InterfaceBlurSize = Bind(this, "Game", "Interface Blur Size", 3f, "The size of the in-game interface blur.");
            InterfaceBlurColor = Bind(this, "Game", "Interface Blur Color", new Color(0.4f, 0.4f, 0.4f), "The color of the in-game interface blur.");

            #endregion

            #region User

            DisplayName = Bind(this, "User", "Display Name", "Player", "Sets the username to show in levels and menus.");

            #endregion

            #region File

            ScreenshotsPath = Bind(this, "File", "Screenshot Path", "screenshots", "The path to save screenshots to.");
            ScreenshotKey = BindEnum(this, "File", "Screenshot Key", KeyCode.F2, "The key to press to take a screenshot.");
            OpenPAFolder = BindEnum(this, "File", "Open Project Arrhythmia Folder", KeyCode.F4, "Opens the folder containing the Project Arrhythmia application and all files related to it.");
            OpenPAPersistentFolder = BindEnum(this, "File", "Open LocalLow Folder", KeyCode.F5, "Opens the data folder all instances of PA share containing the log files and global editor data.");

            #endregion

            #region Level

            BGReactiveLerp = Bind(this, "Level", "Reactive Color Lerp", true, "If on, reactive color will lerp from base color to reactive color. Otherwise, the reactive color will be added to the base color.");
            LDM = Bind(this, "Level", "Low Detail Mode", false, "If enabled, any objects with \"LDM\" (Low Detail Mode) toggled on will not be rendered.");
            EnableVideoBackground = Bind(this, "Level", "Video Backgrounds", true, "If on, the old video BG feature returns, though somewhat buggy. Requires a bg.mp4 or bg.mov file to exist in the level folder.");
            UseNewUpdateMethod = Bind(this, "Level", "Use New Update Method", true, "Possibly releases the fixed framerate of the game.");
            ReplayLevel = Bind(this, "Level", "Replay Level in Background After Completion", true, "When completing a level, having this on will replay the level with no players in the background of the end screen.");
            PrioritizeVG = Bind(this, "Level", "Priotize VG format", true, "Due to LS file formats also being in level folders with VG formats, VG format will need to be prioritized, though you can turn this off if a VG level isn't working and it has a level.lsb file.");

            #endregion

            #region Discord

            DiscordShowLevel = Bind(this, "Discord", "Show Level Status", true, "If level name is shown in your Discord status.");
            DiscordRichPresenceID = Bind(this, "Discord", "Status ID (READ DESC)", "1176264603374735420", "Only change if you already have your own custom Discord status setup.");

            #endregion

            #region Debugging

            DebugsOn = Bind(this, "Debugging", "Enabled", true, "If disabled, turns all Unity debug logs off. Might boost performance.");
            DebugInfo = Bind(this, "Debugging", "Show Debug Info", false, "Shows a helpful info overlay with some information about the current gamestate.");
            DebugInfoStartup = Bind(this, "Debugging", "Create Debug Info", false, "If the Debug Info menu should be created on game start. Requires restart to have this option take affect.");
            DebugInfoToggleKey = BindEnum(this, "Debugging", "Show Debug Info Toggle Key", KeyCode.F6, "Shows a helpful info overlay with some information about the current gamestate.");
            DebugPosition = Bind(this, "Debugging", "Debug Info Position", new Vector2(-960f, 540f), "The position the Debug Info menu is at.");
            NotifyREPL = Bind(this, "Debugging", "Notify REPL", false, "If in editor, code ran will have their results be notified.");

            #endregion

            Save();
        }

        #region Settings Changed

        public override void SetupSettingChanged()
        {
            SettingChanged += UpdateSettings;
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
            DebugInfoStartup.SettingChanged += DebugInfoChanged;
        }

        void DebugInfoChanged() => RTDebugger.Init();

        void InterfaceBlurChanged()
        {
            if (GameStorageManager.inst && GameStorageManager.inst.guiBlur)
            {
                GameStorageManager.inst.guiBlur.material.SetFloat("_Size", InterfaceBlurSize.Value);
                GameStorageManager.inst.guiBlur.material.color = InterfaceBlurColor.Value;
            }
        }

        void DiscordChanged()
        {
            CoreHelper.UpdateDiscordStatus(CoreHelper.discordLevel, CoreHelper.discordDetails, CoreHelper.discordIcon, CoreHelper.discordArt);
        }

        void LDMChanged()
        {
            if (!EditorManager.inst)
                return;

            var list = GameData.Current.BeatmapObjects.Where(x => x.LDM).ToList();
            for (int i = 0; i < list.Count; i++)
            {
                Updater.UpdateProcessor(list[i]);
            }
        }

        void DefaultSettingsChanged()
        {
            CoreHelper.UpdateValue(prevFullscreen, Fullscreen.Value, SetFullscreen);
            CoreHelper.UpdateValue(prevResolution, Resolution.Value, SetResolution);
            CoreHelper.UpdateValue(prevMasterVol, MasterVol.Value, SetMasterVol);
            CoreHelper.UpdateValue(prevMusicVol, MusicVol.Value, SetMusicVol);
            CoreHelper.UpdateValue(prevSFXVol, SFXVol.Value, SetSFXVol);
            CoreHelper.UpdateValue(prevControllerRumble, ControllerRumble.Value, SetControllerRumble);
            CoreHelper.UpdateValue(prevLanguage, Language.Value, SetLanguage);
        }

        void DisplayNameChanged()
        {
            DataManager.inst.UpdateSettingString("s_display_name", DisplayName.Value);

            LegacyPlugin.player.sprName = DisplayName.Value;

            if (SteamWrapper.inst)
                SteamWrapper.inst.user.displayName = DisplayName.Value;

            if (EditorManager.inst)
                EditorManager.inst.SetCreatorName(DisplayName.Value);

            LegacyPlugin.SaveProfile();
        }

        void UseNewUpdateMethodChanged() => Updater.UseNewUpdateMethod = UseNewUpdateMethod.Value;

        static void UpdateSettings()
        {
            Debug.unityLogger.logEnabled = Instance.DebugsOn.Value;

            CoreHelper.SetCameraRenderDistance();
            CoreHelper.SetAntiAliasing();

            if (RTVideoManager.inst && RTVideoManager.inst.didntPlay && Instance.EnableVideoBackground.Value)
                RTVideoManager.inst.Play(RTVideoManager.inst.currentURL, RTVideoManager.inst.currentAlpha);

            LegacyPlugin.SaveProfile();
        }

        #endregion

    }

}
