﻿using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
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

        public MenuConfig() : base(nameof(MenuConfig)) // Set config name via base("")
        {
            Instance = this;
            BindSettings();

            SetupSettingChanged();
        }

        public override string TabName => "Menus";
        public override Color TabColor => new Color(0.86f, 0.86f, 0.86f, 1f);
        public override string TabDesc => "The interfaces of PA.";

        #region Settings

        #region General

        public Setting<bool> RoundedUI { get; set; }

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

        /// <summary>
        /// The theme of the interface.
        /// </summary>
        public Setting<int> Theme { get; set; }

        /// <summary>
        /// The theme of the new interface.
        /// </summary>
        public Setting<string> InterfaceThemeID { get; set; }

        /// <summary>
        /// If the changelog screen should show on game startup.
        /// </summary>
        public Setting<bool> ShowChangelog { get; set; }

        /// <summary>
        /// If menu effects should be enabled.
        /// </summary>
        public Setting<bool> ShowFX { get; set; }

        /// <summary>
        /// How fast the interface should load normally.
        /// </summary>
        public Setting<float> RegularSpeedMultiplier { get; set; }

        /// <summary>
        /// How fast the interface should load while a submit button is being held.
        /// </summary>
        public Setting<float> SpeedUpSpeedMultiplier { get; set; }

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

        public Setting<bool> PlayInputSelectMusic { get; set; }

        #endregion

        #endregion

        /// <summary>
        /// Bind the individual settings of the config.
        /// </summary>
        public override void BindSettings()
        {
            Load();

            #region General

            ReloadMainMenu = BindEnum(this, GENERAL, "Reload Main Menu key", KeyCode.F6, "The key to reload the main menu for easy reloading of modified menu file.");
            LoadPageEditor = BindEnum(this, GENERAL, "Load Page Editor key", KeyCode.F10, "The key to load the Page Editor.");
            SelectFirstButton = BindEnum(this, GENERAL, "Select First Button", KeyCode.G, "The key to select the first menu button. This is for cases where menu selection disappears.");
            Theme = Bind(this, GENERAL, "Theme", 0, "The theme of the interface.");
            InterfaceThemeID = Bind(this, GENERAL, "Interface Theme ID", "-1", "The theme of the new interface.");
            RoundedUI = Bind(this, GENERAL, "Rounded", false, "If most elements in the interface should have a rounded corner.");
            ShowChangelog = Bind(this, GENERAL, "Show Changelog", true, "If the changelog screen should show on game startup.");
            ShowFX = Bind(this, GENERAL, "Show Effects", true, "If menu effects should be enabled.");
            RegularSpeedMultiplier = Bind(this, GENERAL, "Regular Speed Multiplier", 1f, "How fast the interface should load normally.");
            SpeedUpSpeedMultiplier = Bind(this, GENERAL, "Speed Up Multiplier", 4f, "How fast the interface should load while a submit button is being held.");

            #endregion

            #region Music

            PlayCustomMusic = Bind(this, MUSIC, "Play Custom Music", true, "If a custom song should play instead of the normal internal menu music.");
            MusicLoadMode = BindEnum(this, MUSIC, "Load Directory", MenuMusicLoadMode.Settings, "Where the music loads from. Settings path: Project Arrhythmia/settings/menus.");
            MusicIndex = Bind(this, MUSIC, "File Index", -1, "If number is less than 0 or higher than the song file count, it will play a random song. Otherwise it will use the specified index.");
            MusicGlobalPath = Bind(this, MUSIC, "Global Path", "C:/", "Set this path to whatever path you want if you're using Global Load Directory.");
            PlayInputSelectMusic = Bind(this, MUSIC, "Play Input Select Music", true, "If the Input Select theme music should play.");

            #endregion

            Save();
        }

        #region Settings Changed

        public override void SetupSettingChanged()
        {
            Theme.SettingChanged += ThemeChanged;
            PlayCustomMusic.SettingChanged += MusicChanged;
            MusicLoadMode.SettingChanged += MusicChanged;
            MusicIndex.SettingChanged += MusicChanged;
            MusicGlobalPath.SettingChanged += MusicChanged;
            PlayInputSelectMusic.SettingChanged += InputSelectMusicChanged;
        }

        void ThemeChanged()
        {
            var value = Mathf.Clamp(Theme.Value, 0, DataManager.inst.interfaceSettings["UITheme"].Count - 1);

            DataManager.inst.UpdateSettingEnum("UITheme", value);
            SaveManager.inst.UpdateSettingsFile(false);

            if (MenuManager.inst && MenuManager.inst.ic)
            {
                try
                {
                    MenuManager.inst.ApplyInterfaceTheme();
                }
                catch (Exception ex)
                {
                    CoreHelper.LogError($"Could not update menu theme.\nException: {ex}");
                }
            }
        }

        void MusicChanged()
        {
            if (!CoreHelper.InGame)
                InterfaceManager.inst.PlayMusic();
        }

        void InputSelectMusicChanged()
        {
            if (SceneHelper.CurrentScene != "Input Select")
                return;

            if (!CoreHelper.InGame && !PlayInputSelectMusic.Value)
                InterfaceManager.inst.PlayMusic();
            else if (PlayInputSelectMusic.Value)
                SoundManager.inst.PlayMusic(DefaultMusic.loading);
        }

        #endregion

        #region Sections

        public const string GENERAL = "General";
        public const string MUSIC = "Music";

        #endregion
    }
}
