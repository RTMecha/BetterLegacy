using BepInEx.Configuration;
using System;
using UnityEngine;

namespace BetterLegacy.Configs
{
    /// <summary>
    /// Events Config for PA Legacy. Based on the EventsCore mod.
    /// </summary>
    public class EventsConfig : BaseConfig
    {
        public static EventsConfig Instance { get; set; }

        public EventsConfig() : base("Events") // Set config name via base("")
        {
            Instance = this;
            BindSettings();

            SetupSettingChanged();
        }

        #region Settings

        #region General

        /// <summary>
        /// If disabled, effects like chroma, bloom, etc will be disabled.
        /// </summary>
        public Setting<bool> ShowFX { get; set; }

        /// <summary>
        /// If the shake event affects the controller rumble.
        /// </summary>
        public Setting<bool> ShakeAffectsController { get; set; }

        /// <summary>
        /// Original is for the original shake method, while Catalyst is for the new shake method.
        /// </summary>
        public Setting<ShakeType> ShakeEventMode { get; set; }

        #endregion

        #region Camera

        /// <summary>
        /// Enabling this will disable all regular Camera events (move, zoom, etc) and allow you to move the camera around freely. WASD to move, + and - to zoom and numpad 4 / numpad 6 to rotate.
        /// </summary>
        public Setting<bool> EditorCamEnabled { get; set; }

        /// <summary>
        /// How fast the editor camera moves.
        /// </summary>
        public Setting<float> EditorCamSpeed { get; set; }

        /// <summary>
        /// Press this key to toggle the Editor Camera on or off.
        /// </summary>
        public Setting<KeyCode> EditorCamToggle { get; set; }

        /// <summary>
        /// How slow the editor camera is when left trigger is held.
        /// </summary>
        public Setting<float> EditorCamSlowSpeed { get; set; }

        /// <summary>
        /// How fast the editor camera is when right trigger is held.
        /// </summary>
        public Setting<float> EditorCamFastSpeed { get; set; }

        /// <summary>
        /// If the editor camera can use your keyboard or not.
        /// </summary>
        public Setting<bool> EditorCamUseKeys { get; set; }

        /// <summary>
        /// If the offset values should reset when the editor camera is disabled.
        /// </summary>
        public Setting<bool> EditorCamResetValues { get; set; }

        #endregion

        #region Game

        /// <summary>
        /// Sets the timeline active / inactive.
        /// </summary>
        public Setting<bool> HideTimeline { get; set; }

        /// <summary>
        /// Sets the players and GUI elements active / inactive.
        /// </summary>
        public Setting<bool> ShowGUI { get; set; }

        /// <summary>
        /// Press this key to toggle the players / GUI on or off.
        /// </summary>
        public Setting<KeyCode> ShowGUIToggle { get; set; }

        /// <summary>
        /// Sets the Intro GUI active state while it's on-screen.
        /// </summary>
        public Setting<bool> ShowIntro { get; set; }

        #endregion

        #endregion

        /// <summary>
        /// Bind the individual settings of the config.
        /// </summary>
        public override void BindSettings()
        {
            Load();

            #region General

            ShowFX = Bind(this, "General", "Show Effects", true, "If disabled, effects like chroma, bloom, etc will be disabled.");
            ShakeAffectsController = Bind(this, "General", "Shake Affects Controller", true, "If the shake event affects the controller rumble.");
            ShakeEventMode = BindEnum(this, "General", "Shake Mode", ShakeType.Original, "Original is for the original shake method, while Catalyst is for the new shake method.");

            #endregion

            #region Camera

            EditorCamEnabled = Bind(this, "Camera", "Editor Camera Offset", false, "Enabling this will disable all regular Camera events (move, zoom, etc) and allow you to move the camera around freely. WASD to move, + and - to zoom and numpad 4 / numpad 6 to rotate.");
            EditorCamSpeed = Bind(this, "Camera", "Editor Camera Speed", 1f, "How fast the editor camera moves.");
            EditorCamToggle = BindEnum(this, "Camera", "Editor Camera Toggle Key", KeyCode.F3, "Press this key to toggle the Editor Camera on or off.");
            EditorCamSlowSpeed = Bind(this, "Camera", "Editor Camera Slow Speed", 0.5f, "How slow the editor camera is when left trigger is held.");
            EditorCamFastSpeed = Bind(this, "Camera", "Editor Camera Fast Speed", 2f, "How fast the editor camera is when right trigger is held.");
            EditorCamUseKeys = Bind(this, "Camera", "Editor Camera Use Keys", true, "If the editor camera can use your keyboard or not.");
            EditorCamResetValues = Bind(this, "Camera", "Editor Camera Reset Values", true, "If the offset values should reset when the editor camera is disabled.");

            #endregion

            #region Game

            HideTimeline = Bind(this, "Game", "Hide Timeline", false, "Sets the timeline active / inactive.");
            ShowGUI = Bind(this, "Game", "Players & GUI Active", true, "Sets the players and GUI elements active / inactive.");
            ShowGUIToggle = BindEnum(this, "Game", "Players & GUI Toggle Key", KeyCode.F9, "Press this key to toggle the players / GUI on or off.");
            ShowIntro = Bind(this, "Game", "Show Intro", true, "Sets the Intro GUI active state while it's on-screen.");

            #endregion

            Save();
        }

        #region Settings Changed

        public override void SetupSettingChanged()
        {
            SettingChanged += UpdateSettings;
        }

        void UpdateSettings()
        {
            if (EventManager.inst)
                EventManager.inst.updateEvents();
        }

        #endregion
    }
}
