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
    /// <summary>
    /// Events Config for PA Legacy. Based on the EventsCore mod.
    /// </summary>
    public class EventsConfig : BaseConfig
    {
        public static EventsConfig Instance { get; set; }

        public override ConfigFile Config { get; set; }

        public EventsConfig(ConfigFile config) : base(config)
        {
            Instance = this;
            Config = config;

            #region General

            ShowFX = Config.Bind("Events - General", "Show Effects", true, "If disabled, effects like chroma, bloom, etc will be disabled.");
            ShakeAffectsController = Config.Bind("Events - General", "Shake Affects Controller", true, "If the shake event affects the controller rumble.");
            ShakeEventMode = Config.Bind("Events - General", "Shake Mode", ShakeType.Original, "Original is for the original shake method, while Catalyst is for the new shake method.");

            #endregion

            #region Camera

            EditorCamEnabled = Config.Bind("Events - Camera", "Editor Camera Offset", false, "Enabling this will disable all regular Camera events (move, zoom, etc) and allow you to move the camera around freely. WASD to move, + and - to zoom and numpad 4 / numpad 6 to rotate.");
            EditorCamSpeed = Config.Bind("Events - Camera", "Editor Camera Speed", 1f, "How fast the editor camera moves.");
            EditorCamToggle = Config.Bind("Events - Camera", "Editor Camera Toggle Key", KeyCode.F3, "Press this key to toggle the Editor Camera on or off.");
            EditorCamSlowSpeed = Config.Bind("Events - Camera", "Editor Camera Slow Speed", 0.5f, "How slow the editor camera is when left trigger is held.");
            EditorCamFastSpeed = Config.Bind("Events - Camera", "Editor Camera Fast Speed", 2f, "How fast the editor camera is when right trigger is held.");
            EditorCamUseKeys = Config.Bind("Events - Camera", "Editor Camera Use Keys", true, "If the editor camera can use your keyboard or not.");
            EditorCamResetValues = Config.Bind("Events - Camera", "Editor Camera Reset Values", true, "If the offset values should reset when the editor camera is disabled.");

            #endregion

            #region Game

            ShowGUI = Config.Bind("Events - Game", "Players & GUI Active", true, "Sets the players and GUI elements active / inactive.");
            ShowGUIToggle = Config.Bind("Events - Game", "Players & GUI Toggle Key", KeyCode.F9, "Press this key to toggle the players / GUI on or off.");
            ShowIntro = Config.Bind("Events - Game", "Show Intro", true, "Sets the Intro GUI active state while it's on-screen.");

            #endregion

            SetupSettingChanged();
        }

        #region Configs

        #region General

        /// <summary>
        /// If disabled, effects like chroma, bloom, etc will be disabled.
        /// </summary>
        public ConfigEntry<bool> ShowFX { get; set; }

        /// <summary>
        /// If the shake event affects the controller rumble.
        /// </summary>
        public ConfigEntry<bool> ShakeAffectsController { get; set; }

        /// <summary>
        /// Original is for the original shake method, while Catalyst is for the new shake method.
        /// </summary>
        public ConfigEntry<ShakeType> ShakeEventMode { get; set; }

        #endregion

        #region Camera

        /// <summary>
        /// Enabling this will disable all regular Camera events (move, zoom, etc) and allow you to move the camera around freely. WASD to move, + and - to zoom and numpad 4 / numpad 6 to rotate.
        /// </summary>
        public ConfigEntry<bool> EditorCamEnabled { get; set; }

        /// <summary>
        /// How fast the editor camera moves.
        /// </summary>
        public ConfigEntry<float> EditorCamSpeed { get; set; }

        /// <summary>
        /// Press this key to toggle the Editor Camera on or off.
        /// </summary>
        public ConfigEntry<KeyCode> EditorCamToggle { get; set; }

        /// <summary>
        /// How slow the editor camera is when left trigger is held.
        /// </summary>
        public ConfigEntry<float> EditorCamSlowSpeed { get; set; }

        /// <summary>
        /// How fast the editor camera is when right trigger is held.
        /// </summary>
        public ConfigEntry<float> EditorCamFastSpeed { get; set; }

        /// <summary>
        /// If the editor camera can use your keyboard or not.
        /// </summary>
        public ConfigEntry<bool> EditorCamUseKeys { get; set; }

        /// <summary>
        /// If the offset values should reset when the editor camera is disabled.
        /// </summary>
        public ConfigEntry<bool> EditorCamResetValues { get; set; }

        #endregion

        #region Game

        /// <summary>
        /// Sets the players and GUI elements active / inactive.
        /// </summary>
        public ConfigEntry<bool> ShowGUI { get; set; }

        /// <summary>
        /// Press this key to toggle the players / GUI on or off.
        /// </summary>
        public ConfigEntry<KeyCode> ShowGUIToggle { get; set; }

        /// <summary>
        /// Sets the Intro GUI active state while it's on-screen.
        /// </summary>
        public ConfigEntry<bool> ShowIntro { get; set; }

        #endregion

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
