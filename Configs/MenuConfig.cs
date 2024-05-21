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

        public MenuConfig() : base("Menu") // Set config name via base("")
        {
            Instance = this;
            BindSettings();

            SetupSettingChanged();
        }

        #region Settings

        #region General

        /// <summary>
        /// The key to reload the main menu for easy reloading of modified menu file.
        /// </summary>
        public Setting<KeyCode> ReloadMainMenu { get; set; }

        /// <summary>
        /// The key to load the Page Editor.
        /// </summary>
        public Setting<KeyCode> LoadPageEditor { get; set; }

        /// <summary>
        /// The key to select the first menu button. This is for cases where menu selection disappears.
        /// </summary>
        public Setting<KeyCode> SelectFirstButton { get; set; }

        #endregion

        #region Music

        /// <summary>
        /// If a custom song should play instead of the normal internal menu music.
        /// </summary>
        public Setting<bool> PlayCustomMusic { get; set; }

        /// <summary>
        /// Where the music loads from. Settings path: Project Arrhythmia/settings/menus.
        /// </summary>
        public Setting<MenuMusicLoadMode> MusicLoadMode { get; set; }

        /// <summary>
        /// If number is less than 0 or higher than the song file count, it will play a random song. Otherwise it will use the specified index.
        /// </summary>
        public Setting<int> MusicIndex { get; set; }

        /// <summary>
        /// Set this path to whatever path you want if you're using Global Load Directory.
        /// </summary>
        public Setting<string> MusicGlobalPath { get; set; }

        #endregion

        #endregion

        /// <summary>
        /// Bind the individual settings of the config.
        /// </summary>
        public override void BindSettings()
        {
            Load();

            ReloadMainMenu = BindEnum(this, "General", "Reload Main Menu key", KeyCode.F6, "The key to reload the main menu for easy reloading of modified menu file.");
            LoadPageEditor = BindEnum(this, "General", "Load Page Editor key", KeyCode.F10, "The key to load the Page Editor.");
            SelectFirstButton = BindEnum(this, "General", "Select First Button", KeyCode.G, "The key to select the first menu button. This is for cases where menu selection disappears.");

            PlayCustomMusic = Bind(this, "Music", "Play Custom Music", true, "If a custom song should play instead of the normal internal menu music.");
            MusicLoadMode = BindEnum(this, "Music", "Load Directory", MenuMusicLoadMode.Settings, "Where the music loads from. Settings path: Project Arrhythmia/settings/menus.");
            MusicIndex = Bind(this, "Music", "File Index", -1, "If number is less than 0 or higher than the song file count, it will play a random song. Otherwise it will use the specified index.");
            MusicGlobalPath = Bind(this, "Music", "Global Path", "C:/", "Set this path to whatever path you want if you're using Global Load Directory.");

            Save();
        }

        #region Settings Changed

        public override void SetupSettingChanged()
        {
            PlayCustomMusic.SettingChanged += MusicChanged;
            MusicLoadMode.SettingChanged += MusicChanged;
            MusicIndex.SettingChanged += MusicChanged;
            MusicGlobalPath.SettingChanged += MusicChanged;
        }

        void MusicChanged()
        {
            if (!EditorManager.inst && MenuManager.inst.ic)
                MenuManager.inst.PlayMusic();
        }

        #endregion
    }
}
