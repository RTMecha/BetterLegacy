using BepInEx.Configuration;
using BetterLegacy.Core;
using BetterLegacy.Core.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace BetterLegacy.Configs
{
    public class EventsConfig : BaseConfig
    {
        public static EventsConfig Instance { get; set; }

        public override ConfigFile Config { get; set; }

        public EventsConfig(ConfigFile config) : base(config)
        {
            Instance = this;
            Config = config;

            EditorCamEnabled = Config.Bind("Camera", "Editor Camera Offset", false, "Enabling this will disable all regular Camera events (move, zoom, etc) and allow you to move the camera around freely. WASD to move, + and - to zoom and numpad 4 / numpad 6 to rotate.");
            EditorCamSpeed = Config.Bind("Camera", "Editor Camera Speed", 1f, "How fast the editor camera moves");
            EditorCamToggle = Config.Bind("Camera", "Editor Camera Toggle Key", KeyCode.F3, "Press this key to toggle the Editor Camera on or off.");
            EditorCamSlowSpeed = Config.Bind("Camera", "Editor Camera Slow Speed", 0.5f, "How slow the editor camera is when left trigger is held.");
            EditorCamFastSpeed = Config.Bind("Camera", "Editor Camera Fast Speed", 2f, "How fast the editor camera is when right trigger is held.");
            EditorCamUseKeys = Config.Bind("Camera", "Editor Camera Use Keys", true, "If the editor camera should use your keyboard or not.");
            EditorCamResetValues = Config.Bind("Camera", "Editor Camera Reset Values", true, "If the offset values should reset when the editor camera is disabled.");

            ShowGUI = Config.Bind("Game", "Players & GUI Active", true, "Sets the players and GUI elements active / inactive.");
            ShowGUIToggle = Config.Bind("Game", "Players & GUI Toggle Key", KeyCode.F9, "Press this key to toggle the players / GUI on or off.");
            ShowIntro = Config.Bind("Game", "Show Intro", true, "Sets whether the Intro GUI is active / inactive.");

            ShowFX = Config.Bind("Events", "Show Effects", true, "If disabled, effects like chroma, bloom, etc will be disabled.");

            ShakeEventMode = Config.Bind("Events", "Shake Mode", ShakeType.Original, "Original is for the original shake method, while Catalyst is for the new shake method.");

            SetupSettingChanged();
        }

        #region Configs

        public ConfigEntry<bool> EditorCamEnabled { get; set; }
        public ConfigEntry<bool> EditorCamResetValues { get; set; }

        public ConfigEntry<float> EditorCamSpeed { get; set; }

        public ConfigEntry<KeyCode> EditorCamToggle { get; set; }

        public ConfigEntry<bool> EditorCamUseKeys { get; set; }
        public ConfigEntry<float> EditorCamSlowSpeed { get; set; }
        public ConfigEntry<float> EditorCamFastSpeed { get; set; }

        public ConfigEntry<bool> ShowGUI { get; set; }

        public ConfigEntry<KeyCode> ShowGUIToggle { get; set; }

        public ConfigEntry<bool> ShowIntro { get; set; }

        public ConfigEntry<bool> ShowFX { get; set; }

        public ConfigEntry<ShakeType> ShakeEventMode { get; set; }

        #endregion

        public override void SetupSettingChanged()
        {
            Config.SettingChanged += new EventHandler<SettingChangedEventArgs>(UpdateSettings);
        }

        void UpdateSettings(object sender, EventArgs e)
        {
            if (EventManager.inst)
                EventManager.inst.updateEvents();
        }

        public override string ToString() => "Events Config";
    }
}
