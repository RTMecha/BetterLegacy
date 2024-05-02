using BepInEx.Configuration;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using BetterLegacy.Core.Optimization;
using LSFunctions;
using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace BetterLegacy.Configs
{
    public class CoreConfig : BaseConfig
    {
        public static CoreConfig Instance { get; set; }

        public override ConfigFile Config { get; set; }

        public CoreConfig(ConfigFile config) : base(config)
        {
            Instance = this;
			Config = config;

			DebugsOn = Config.Bind("Debugging", "Enabled", true, "If disabled, turns all Unity debug logs off. Might boost performance.");
			DebugInfo = Config.Bind("Debugging", "Show Debug Info", false, "Shows a helpful info overlay with some information about the current gamestate.");
			DebugInfoStartup = Config.Bind("Debugging", "Create Debug Info", false, "If the Debug Info menu should be created on game start. Requires restart to have this option take affect.");
			DebugInfoToggleKey = Config.Bind("Debugging", "Show Debug Info Toggle Key", KeyCode.F6, "Shows a helpful info overlay with some information about the current gamestate.");
			NotifyREPL = Config.Bind("Debugging", "Notify REPL", false, "If in editor, code ran will have their results be notified.");

			AllowControlsInputField = Config.Bind("Game", "Allow Controls While Using InputField", true, "If you have this off, the player will not move while an InputField is being used.");
			UseNewUpdateMethod = Config.Bind("Game", "Use New Update Method", true, "Possibly releases the fixed framerate of the game.");
			ScreenshotsPath = Config.Bind("Game", "Screenshot Path", "screenshots", "The path to save screenshots to.");
			ScreenshotKey = Config.Bind("Game", "Screenshot Key", KeyCode.F2, "The key to press to take a screenshot.");
			AntiAliasing = Config.Bind("Game", "Anti-Aliasing", true, "If antialiasing is on or not.");
			RunInBackground = Config.Bind("Game", "Run In Background", true, "If you want the game to continue playing when minimized.");
			IncreasedClipPlanes = Config.Bind("Game", "Camera Clip Planes", true, "Increases the clip panes to a very high amount, allowing for object render depth to go really high or really low.");
			EnableVideoBackground = Config.Bind("Game", "Video Backgrounds", false, "If on, the old video BG feature returns, though somewhat buggy. Requires a bg.mp4 file to exist in the level folder.");
			EvaluateCode = Config.Bind("Game", "Evaluate Custom Code", false, "If custom written code should evaluate. Turn this on if you're sure the level you're using isn't going to mess anything up with a code Modifier or custom player code.");
			ReplayLevel = Config.Bind("Game", "Replay Level in Background After Completion", true, "When completing a level, having this on will replay the level with no players in the background of the end screen.");
			PrioritizeVG = Config.Bind("Game", "Priotize VG format", true, "Due to LS file formats also being in level folders with VG formats, VG format will need to be prioritized, though you can turn this off if a VG level isn't working and it has a level.lsb file.");

			InterfaceBlurSize = Config.Bind("Game", "Interface Blur Size", 3f, "The size of the in-game interface blur.");
			InterfaceBlurColor = Config.Bind("Game", "Interface Blur Color", new Color(0.4f, 0.4f, 0.4f), "The color of the in-game interface blur.");

			DisplayName = Config.Bind("User", "Display Name", "Player", "Sets the username to show in levels and menus.");

			OpenPAFolder = Config.Bind("File", "Open Project Arrhythmia Folder", KeyCode.F4, "Opens the folder containing the Project Arrhythmia application and all files related to it.");
			OpenPAPersistentFolder = Config.Bind("File", "Open LocalLow Folder", KeyCode.F5, "Opens the data folder all instances of PA share containing the log files and copied prefab (if you have EditorManagement installed)");

			Fullscreen = Config.Bind("Settings", "Fullscreen", false);
			Resolution = Config.Bind("Settings", "Resolution", Resolutions.p720);
			MasterVol = Config.Bind("Settings", "Volume Master", 8, new ConfigDescription("Total volume.", new AcceptableValueRange<int>(0, 9)));
			MusicVol = Config.Bind("Settings", "Volume Music", 9, new ConfigDescription("Music volume.", new AcceptableValueRange<int>(0, 9)));
			SFXVol = Config.Bind("Settings", "Volume SFX", 9, new ConfigDescription("SFX volume.", new AcceptableValueRange<int>(0, 9)));
			Language = Config.Bind("Settings", "Language", BetterLegacy.Language.English, "This is currently here for testing purposes. This version of the game has not been translated yet.");
			ControllerRumble = Config.Bind("Settings", "Controller Vibrate", true, "If the controllers should vibrate or not.");

			BGReactiveLerp = Config.Bind("Level Backgrounds", "Reactive Color Lerp", true, "If on, reactive color will lerp from base color to reactive color. Otherwise, the reactive color will be added to the base color.");

			LDM = Config.Bind("Level", "Low Detail Mode", false, "If enabled, any objects with \"LDM\" on will not be rendered.");
			DiscordShowLevel = Config.Bind("Discord", "Show Level Status", true, "Level name is shown.");
			DiscordRichPresenceID = Config.Bind("Discord", "Status ID (READ DESC)", "1176264603374735420", "Only change if you already have your own custom Discord app status setup.");

			Updater.UseNewUpdateMethod = UseNewUpdateMethod.Value;

			SetupSettingChanged();
		}

		#region Configs

		public ConfigEntry<KeyCode> OpenPAFolder { get; set; }
		public ConfigEntry<KeyCode> OpenPAPersistentFolder { get; set; }

		public ConfigEntry<bool> DebugsOn { get; set; }
		public ConfigEntry<bool> AllowControlsInputField { get; set; }
		public ConfigEntry<bool> IncreasedClipPlanes { get; set; }
		public ConfigEntry<string> DisplayName { get; set; }

		public ConfigEntry<bool> DebugInfo { get; set; }
		public ConfigEntry<bool> DebugInfoStartup { get; set; }
		public ConfigEntry<KeyCode> DebugInfoToggleKey { get; set; }
		public ConfigEntry<bool> NotifyREPL { get; set; }

		public ConfigEntry<bool> BGReactiveLerp { get; set; }

		public ConfigEntry<bool> LDM { get; set; }

		public ConfigEntry<bool> AntiAliasing { get; set; }

		public ConfigEntry<KeyCode> ScreenshotKey { get; set; }

		public ConfigEntry<string> ScreenshotsPath { get; set; }
		public ConfigEntry<bool> UseNewUpdateMethod { get; set; }

		public ConfigEntry<bool> DiscordShowLevel { get; set; }

		public ConfigEntry<bool> EnableVideoBackground { get; set; }
		public ConfigEntry<bool> RunInBackground { get; set; }

		public ConfigEntry<bool> EvaluateCode { get; set; }
		public ConfigEntry<bool> ReplayLevel { get; set; }
		public ConfigEntry<bool> PrioritizeVG { get; set; }

		public ConfigEntry<string> DiscordRichPresenceID { get; set; }
		public ConfigEntry<float> InterfaceBlurSize { get; set; }
		public ConfigEntry<Color> InterfaceBlurColor { get; set; }

		#endregion

		#region Default Settings

		public ConfigEntry<bool> Fullscreen { get; set; }

		public ConfigEntry<Resolutions> Resolution { get; set; }

		public ConfigEntry<int> MasterVol { get; set; }

		public ConfigEntry<int> MusicVol { get; set; }

		public ConfigEntry<int> SFXVol { get; set; }

		public ConfigEntry<Language> Language { get; set; }

		public ConfigEntry<bool> ControllerRumble { get; set; }

		void SetFullscreen(bool value)
		{
			prevFullscreen = Fullscreen.Value;

			DataManager.inst.UpdateSettingBool("FullScreen", value);
			SaveManager.inst.ApplyVideoSettings();
			SaveManager.inst.UpdateSettingsFile(false);
		}

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

		void SetMasterVol(int value)
		{
			prevMasterVol = MasterVol.Value;

			DataManager.inst.UpdateSettingInt("MasterVolume", value);

			SaveManager.inst.UpdateSettingsFile(false);
		}

		void SetMusicVol(int value)
		{
			prevMusicVol = MusicVol.Value;

			DataManager.inst.UpdateSettingInt("MusicVolume", value);

			SaveManager.inst.UpdateSettingsFile(false);
		}

		void SetSFXVol(int value)
		{
			prevSFXVol = SFXVol.Value;

			DataManager.inst.UpdateSettingInt("EffectsVolume", value);

			SaveManager.inst.UpdateSettingsFile(false);
		}

		void SetLanguage(Language value)
		{
			prevLanguage = Language.Value;

			DataManager.inst.UpdateSettingInt("Language_i", (int)value);

			SaveManager.inst.UpdateSettingsFile(false);
		}

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
			if (EditorManager.inst)
			{
				var list = GameData.Current.BeatmapObjects.Where(x => x.LDM).ToList();
				for (int i = 0; i < list.Count; i++)
				{
					Updater.UpdateProcessor(list[i]);
				}
			}
		}

		void DefaultSettingsChanged(object sender, EventArgs e)
		{
			CoreHelper.UpdateValue(prevFullscreen, Fullscreen.Value, SetFullscreen);
			CoreHelper.UpdateValue(prevMasterVol, MasterVol.Value, SetMasterVol);
			CoreHelper.UpdateValue(prevMusicVol, MusicVol.Value, SetMusicVol);
			CoreHelper.UpdateValue(prevSFXVol, SFXVol.Value, SetSFXVol);
			CoreHelper.UpdateValue(prevControllerRumble, ControllerRumble.Value, SetControllerRumble);

			if (prevResolution != Resolution.Value)
				SetResolution(Resolution.Value);

			if (prevLanguage != Language.Value)
				SetLanguage(Language.Value);
		}

		void DisplayNameChanged(object sender, EventArgs e)
		{
			DataManager.inst.UpdateSettingString("s_display_name", DisplayName.Value);

			LegacyPlugin.player.sprName = DisplayName.Value;

			if (SteamWrapper.inst != null)
				SteamWrapper.inst.user.displayName = DisplayName.Value;

			EditorManager.inst?.SetCreatorName(DisplayName.Value);

			LegacyPlugin.SaveProfile();
		}

		void UseNewUpdateMethodChanged(object sender, EventArgs e)
		{
			Updater.UseNewUpdateMethod = UseNewUpdateMethod.Value;
		}

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
