using BepInEx.Configuration;
using BetterLegacy.Menus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace BetterLegacy.Configs
{
    public class MenuConfig : BaseConfig
    {
        public static MenuConfig Instance { get; set; }
        public override ConfigFile Config { get; set; }

        public MenuConfig(ConfigFile config) : base(config)
        {
            Instance = this;
            Config = config;

            PlayCustomMusic = Config.Bind("Music", "Play Custom Music", true, "Allows you to load any number of songs from settings/menus.");
            MusicLoadMode = Config.Bind("Music", "Load Directory", MenuMusicLoadMode.Settings, "Where the music loads from. Settings path: Project Arrhythmia/settings/menus.");
            MusicIndex = Config.Bind("Music", "File Index", -1, "If number is less than 0 or higher than the song file count, it will play a random song. Otherwise it will use the specified index.");
            MusicGlobalPath = Config.Bind("Music", "Global Path", "C:/", "Set this path to whatever path you want if you're using Global Load Directory.");

            ReloadMainMenu = Config.Bind("Menu", "Reload Main Menu key", KeyCode.F5, "The key to reload the main menu for easy reloading of modified menu file.");

            prevPlayCustomMusic = PlayCustomMusic.Value;
            prevMusicLoadMode = MusicLoadMode.Value;
            prevMusicIndex = MusicIndex.Value;

            SetupSettingChanged();
        }

        public ConfigEntry<bool> PlayCustomMusic { get; set; }
        public ConfigEntry<MenuMusicLoadMode> MusicLoadMode { get; set; }
        public ConfigEntry<int> MusicIndex { get; set; }

        public ConfigEntry<string> MusicGlobalPath { get; set; }

        public ConfigEntry<KeyCode> ReloadMainMenu { get; set; }

        static bool prevPlayCustomMusic;
        static MenuMusicLoadMode prevMusicLoadMode;
        static int prevMusicIndex;

        public override void SetupSettingChanged()
        {
            Config.SettingChanged += new EventHandler<SettingChangedEventArgs>(UpdateSettings);
        }

        void UpdateSettings(object sender, EventArgs e)
        {
            if (EditorManager.inst == null && ArcadeManager.inst != null && ArcadeManager.inst.ic != null && (prevPlayCustomMusic != PlayCustomMusic.Value || prevMusicLoadMode != MusicLoadMode.Value || prevMusicIndex != MusicIndex.Value))
            {
                prevPlayCustomMusic = PlayCustomMusic.Value;
                prevMusicLoadMode = MusicLoadMode.Value;
                prevMusicIndex = MusicIndex.Value;

                MenuManager.inst.PlayMusic(ArcadeManager.inst.ic);
            }
        }
    }
}
