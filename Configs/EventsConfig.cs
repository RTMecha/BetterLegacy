using UnityEngine;

using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Runtime;

namespace BetterLegacy.Configs
{
    /// <summary>
    /// Events Config for PA Legacy. Based on the EventsCore mod.
    /// </summary>
    public class EventsConfig : BaseConfig
    {
        public static EventsConfig Instance { get; set; }

        public EventsConfig() : base(nameof(EventsConfig)) // Set config name via base("")
        {
            Instance = this;
            BindSettings();

            SetupSettingChanged();
        }

        public override string TabName => "Events";
        public override Color TabColor => new Color(0.1509f, 0.7096f, 1f, 1f);
        public override string TabDesc => "The effects of PA.";

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

            ShowFX = Bind(this, GENERAL, "Show Effects", true, "If disabled, effects like chroma, bloom, etc will be disabled.");
            ShakeAffectsController = Bind(this, GENERAL, "Shake Affects Controller", true, "If the shake event affects the controller rumble.");
            ShakeEventMode = BindEnum(this, GENERAL, "Shake Mode", ShakeType.Catalyst, "Catalyst shake supports shake smoothness and speed. If you don't like how Catalyst shake looks, you can use the original shake type, just know these features will not be present with it.");

            #endregion

            #region Game

            HideTimeline = Bind(this, GAME, "Hide Timeline", false, "Sets the timeline active / inactive. Only works in editor / zen mode.");
            ShowGUI = Bind(this, GAME, "Players & GUI Active", true, "Sets the players and GUI elements active / inactive. Only works in editor / zen mode.");
            ShowGUIToggle = BindEnum(this, GAME, "Players & GUI Toggle Key", KeyCode.F9, "Press this key to toggle the players / GUI on or off.");
            ShowIntro = Bind(this, GAME, "Show Intro", true, "Sets the Intro GUI active state while it's on-screen. Only works in the arcade.");

            #endregion

            Save();
        }

        #region Settings Changed

        public override void SetupSettingChanged()
        {
            SettingChanged += UpdateSettings;
        }

        void UpdateSettings() => RTLevel.Current?.UpdateEvents();

        #endregion

        #region Sections

        public const string GENERAL = "General";
        public const string CAMERA = "Camera";
        public const string GAME = "Game";

        #endregion
    }
}
