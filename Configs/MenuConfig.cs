using BepInEx.Configuration;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Menus;
using System;
using UnityEngine;

namespace BetterLegacy.Configs
{
    /// <summary>
    /// Menu Config for PA Legacy. Based on the PageCreator mod.
    /// </summary>
    public class MenuConfig : BaseConfig
    {
        public static MenuConfig Instance { get; set; }
        public override ConfigFile Config { get; set; }

        public MenuConfig(ConfigFile config) : base(config)
        {
            Instance = this;
            Config = config;

            UseNewInterface = Config.Bind("Menu - General", "Use New Interface", true, "If the game should use the new interface. Requires restart to take effect.");
            ReloadMainMenu = Config.Bind("Menu - General", "Reload Main Menu key", KeyCode.F6, "The key to reload the main menu for easy reloading of modified menu file.");
            LoadPageEditor = Config.Bind("Menu - General", "Load Page Editor key", KeyCode.F10, "The key to load the Page Editor.");
            SelectFirstButton = Config.Bind("Menu - General", "Select First Button", KeyCode.G, "The key to select the first menu button. This is for cases where menu selection disappears.");

            PlayCustomMusic = Config.Bind("Menu - Music", "Play Custom Music", true, "If a custom song should play instead of the normal internal menu music.");
            MusicLoadMode = Config.Bind("Menu - Music", "Load Directory", MenuMusicLoadMode.Settings, "Where the music loads from. Settings path: Project Arrhythmia/settings/menus.");
            MusicIndex = Config.Bind("Menu - Music", "File Index", -1, "If number is less than 0 or higher than the song file count, it will play a random song. Otherwise it will use the specified index.");
            MusicGlobalPath = Config.Bind("Menu - Music", "Global Path", "C:/", "Set this path to whatever path you want if you're using Global Load Directory.");

            CoreHelper.UseNewInterface = UseNewInterface.Value;
            SetupSettingChanged();
        }

        #region General

        public ConfigEntry<bool> UseNewInterface { get; set; }

        /// <summary>
        /// The key to reload the main menu for easy reloading of modified menu file.
        /// </summary>
        public ConfigEntry<KeyCode> ReloadMainMenu { get; set; }

        /// <summary>
        /// The key to load the Page Editor.
        /// </summary>
        public ConfigEntry<KeyCode> LoadPageEditor { get; set; }

        /// <summary>
        /// The key to select the first menu button. This is for cases where menu selection disappears.
        /// </summary>
        public ConfigEntry<KeyCode> SelectFirstButton { get; set; }

        #endregion

        #region Music

        /// <summary>
        /// If a custom song should play instead of the normal internal menu music.
        /// </summary>
        public ConfigEntry<bool> PlayCustomMusic { get; set; }

        /// <summary>
        /// Where the music loads from. Settings path: Project Arrhythmia/settings/menus.
        /// </summary>
        public ConfigEntry<MenuMusicLoadMode> MusicLoadMode { get; set; }

        /// <summary>
        /// If number is less than 0 or higher than the song file count, it will play a random song. Otherwise it will use the specified index.
        /// </summary>
        public ConfigEntry<int> MusicIndex { get; set; }

        /// <summary>
        /// Set this path to whatever path you want if you're using Global Load Directory.
        /// </summary>
        public ConfigEntry<string> MusicGlobalPath { get; set; }

        #endregion

        public override void SetupSettingChanged()
        {
            PlayCustomMusic.SettingChanged += MusicChanged;
            MusicLoadMode.SettingChanged += MusicChanged;
            MusicIndex.SettingChanged += MusicChanged;
            MusicGlobalPath.SettingChanged += MusicChanged;
        }

        void MusicChanged(object sender, EventArgs e)
        {
            if (!EditorManager.inst && MenuManager.inst.ic)
                MenuManager.inst.PlayMusic();
        }
    }
}
