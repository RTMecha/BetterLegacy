using BetterLegacy.Core;
using BetterLegacy.Core.Helpers;
using UnityEngine;

using BetterLegacy.Companion.Entity;
using BetterLegacy.Core.Components;

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

        public static void Log(string msg) => Debug.Log($"{className}{msg}");
        public static void LogWarning(string msg) => Debug.LogWarning($"{className}{msg}");
        public static void LogError(string msg) => Debug.LogError($"{className}{msg}");

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
