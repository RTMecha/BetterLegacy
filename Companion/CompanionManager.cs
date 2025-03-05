using BetterLegacy.Core;
using BetterLegacy.Core.Helpers;
using UnityEngine;

using BetterLegacy.Companion.Entity;
using BetterLegacy.Core.Components;
using BetterLegacy.Configs;

namespace BetterLegacy.Companion
{
    /// <summary>
    /// Manager class for handling the companion (Example).
    /// </summary>
    public class CompanionManager : MonoBehaviour
    {
        #region Init

        public static string className = "[<color=#3F59FC>CompanionManager</color>]\n";

        /// <summary>
        /// The <see cref="CompanionManager"/> global instance reference.
        /// </summary>
        public static CompanionManager inst;

        /// <summary>
        /// Initializes <see cref="CompanionManager"/>.
        /// </summary>
        public static void Init() => Creator.NewGameObject(nameof(CompanionManager), SystemManager.inst.transform).AddComponent<CompanionManager>();

        void Awake()
        {
            inst = this;
            animationController = gameObject.AddComponent<AnimationController>();
        }

        void Update() => Example.Current?.Tick();

        #endregion

        /// <summary>
        /// Logs a message if Example logs are enabled.
        /// </summary>
        /// <param name="msg">Message.</param>
        public static void Log(string msg)
        {
            if (ExampleConfig.Instance.LogsEnabled.Value)
                Debug.Log($"{className}{msg}");
        }

        /// <summary>
        /// Logs a warning message if Example logs are enabled.
        /// </summary>
        /// <param name="msg">Message.</param>
        public static void LogWarning(string msg)
        {
            if (ExampleConfig.Instance.LogsEnabled.Value)
                Debug.LogWarning($"{className}{msg}");
        }

        /// <summary>
        /// Logs an error message if Example logs are enabled.
        /// </summary>
        /// <param name="msg">Message.</param>
        public static void LogError(string msg)
        {
            if (ExampleConfig.Instance.LogsEnabled.Value)
                Debug.LogError($"{className}{msg}");
        }

        /// <summary>
        /// Local animation controller for Example.
        /// </summary>
        public AnimationController animationController;

        /// <summary>
        /// If the music is playing, if true then Example has a chance to start dancing.
        /// </summary>
        public static bool MusicPlaying => CoreHelper.InEditor && EditorManager.inst.hasLoadedLevel && AudioManager.inst.CurrentAudioSource.isPlaying;
        /// <summary>
        /// How far the face should go when facing something.
        /// </summary>
        public const float FACE_LOOK_MULTIPLIER = 0.006f;
        /// <summary>
        /// How far the face elements should go when facing something.
        /// </summary>
        public const float FACE_X_MULTIPLIER = 5f;
        /// <summary>
        /// How often the pupils naturally look around slightly.
        /// </summary>
        public const float PUPILS_LOOK_RATE = 3f;
        /// <summary>
        /// How often the eyes should blink.
        /// </summary>
        public const float BLINK_RATE = 5f;
    }
}
