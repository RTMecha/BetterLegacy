using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

using DG.Tweening;
using ILMath;

using BetterLegacy.Arcade.Managers;
using BetterLegacy.Configs;
using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Data.Modifiers;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using BetterLegacy.Core.Runtime.Events;
using BetterLegacy.Core.Runtime.Objects;
using BetterLegacy.Core.Threading;

namespace BetterLegacy.Core.Runtime
{
    public delegate void RuntimeObjectNotifier(IRTObject runtimeObject);

    /// <summary>
    /// Short for Runtime Level. Controls all objects, events and modifiers in the level at runtime.
    /// </summary>
    public class RTLevel : RTLevelBase
    {
        #region Core

        public RTLevel()
        {
            eventEngine = new EventEngine();

            threadedTickRunner = new TickRunner(true);
            threadedTickRunner.onTick = () =>
            {
                sampleLow = samples.Skip(0).Take(56).Average((float a) => a) * 1000f;
                sampleMid = samples.Skip(56).Take(100).Average((float a) => a) * 3000f;
                sampleHigh = samples.Skip(156).Take(100).Average((float a) => a) * 6000f;
            };
            loop = new ModifierLoop(GameData.Current, new Dictionary<string, string>());
        }

        /// <summary>
        /// Class name for logging.
        /// </summary>
        public static string className = "[<color=#FF26C5>RTLevel</color>] \n";

        /// <summary>
        /// The current runtime level.
        /// </summary>
        public static RTLevel Current { get; set; }

        public override Transform Parent => ObjectManager.inst && ObjectManager.inst.objectParent ? ObjectManager.inst.objectParent.transform : GameObject.Find("GameObjects")?.transform;

        public override float FixedTime => AudioManager.inst.CurrentAudioSource.time;

        /// <summary>
        /// Layer where 2D objects are.
        /// </summary>
        public const int FOREGROUND_LAYER = 8;
        /// <summary>
        /// Layer where 3D objects are.
        /// </summary>
        public const int BACKGROUND_LAYER = 9;
        /// <summary>
        /// Layer that doesn't get affected by post process effects and appears above other layers.
        /// </summary>
        public const int UI_LAYER = 11;

        /// <summary>
        /// Performs heavy calculations on a separate tick thread.
        /// </summary>
        public TickRunner threadedTickRunner;

        public EvaluationContext evaluationContext;

        ModifierLoop loop;

        /// <summary>
        /// Initializes the runtime level.
        /// </summary>
        public static void Init()
        {
            Current?.Clear();
            Current = new RTLevel();
            Current.Load();
        }

        /// <summary>
        /// Updates everything and reinitializes the engine.
        /// </summary>
        /// <param name="restart">If the engine should restart or not.</param>
        public static void Reinit(bool restart = true)
        {
            // We check if LevelProcessor has been invoked and if the level should restart.
            if (!Current && restart)
            {
                Init();
                return;
            }

            // If it is not null then we continue.
            ResetOffsets();

            if (Current)
            {
                for (int i = 0; i < Current.objects.Count; i++)
                    Current.objects[i].Clear();
                Current.objects.Clear();

                for (int i = 0; i < Current.bgObjects.Count; i++)
                    Current.bgObjects[i].Clear();
                Current.bgObjects.Clear();

                for (int i = 0; i < Current.modifiers.Count; i++)
                    Current.modifiers[i].Clear();
                Current.modifiers.Clear();

                for (int i = 0; i < Current.bgModifiers.Count; i++)
                    Current.bgModifiers[i].Clear();
                Current.bgModifiers.Clear();

                for (int i = 0; i < Current.backgroundLayers.Count; i++)
                    Current.backgroundLayers[i].Clear();
                Current.backgroundLayers.Clear();
            }

            // End and restart.
            if (restart)
                Init();
            else
            {
                Current?.Clear();
                Current = null;
            }
        }

        /// <summary>
        /// Updates everything and reinitializes the engine.
        /// </summary>
        /// <param name="restart">If the engine should restart or not.</param>
        /// <param name="resetOffsets">If the offsets should reset.</param>
        public static IEnumerator IReinit(bool restart = true, bool resetOffsets = false)
        {
            // We check if LevelProcessor has been invoked and if the level should restart.
            if (!Current && restart)
            {
                Init();
                yield break;
            }

            if (resetOffsets)
                ResetOffsets();

            if (Current)
            {
                for (int i = 0; i < Current.objects.Count; i++)
                    Current.objects[i].Clear();
                Current.objects.Clear();

                for (int i = 0; i < Current.bgObjects.Count; i++)
                    Current.bgObjects[i].Clear();
                Current.bgObjects.Clear();

                for (int i = 0; i < Current.modifiers.Count; i++)
                    Current.modifiers[i].Clear();
                Current.modifiers.Clear();

                for (int i = 0; i < Current.bgModifiers.Count; i++)
                    Current.bgModifiers[i].Clear();
                Current.bgModifiers.Clear();

                for (int i = 0; i < Current.backgroundLayers.Count; i++)
                    Current.backgroundLayers[i].Clear();
                Current.backgroundLayers.Clear();
            }

            // End and restart.
            if (restart)
                Init();
            else
            {
                Current?.Clear();
                Current = null;
            }

            yield break;
        }

        public override void Load()
        {
            Debug.Log($"{className}Loading level");

            previousAudioTime = 0.0f;
            audioTimeVelocity = 0.0f;

            // Sets a new seed or uses the current one.
            RandomHelper.UpdateSeed();

            evaluationContext = EvaluationContext.CreateDefault();

            var gameData = GameData.Current;

            // Removing and reinserting prefabs.
            gameData.beatmapObjects.RemoveAll(x => x.fromPrefab);
            gameData.backgroundObjects.RemoveAll(x => x.fromPrefab);

            Load(gameData);

            Debug.Log($"{className}Loaded {objects.Count} objects (original: {gameData.beatmapObjects.Count})");
        }

        public override void Tick()
        {
            var logTick = !CoreHelper.IsUsingInputField && Input.GetKeyDown(KeyCode.I);
            System.Diagnostics.Stopwatch sw = logTick ? CoreHelper.StartNewStopwatch() : null;

            AudioManager.inst.CurrentAudioSource.GetSpectrumData(samples, 0, FFTWindow.Rectangular);

            if (!CoreConfig.Instance.UseNewUpdateMethod.Value)
                CurrentTime = FixedTime;
            else
            {
                var smoothedTime = Mathf.SmoothDamp(previousAudioTime, FixedTime, ref audioTimeVelocity, 1.0f / 50.0f);
                CurrentTime = smoothedTime;
                previousAudioTime = smoothedTime;
            }

            if (logTick)
                Log($"Start pre-tick at: {sw.Elapsed}");

            try
            {
                RTMath.RegisterVariableFunctions(evaluationContext);
            }
            catch
            {

            }

            PreTick();

            try
            {
                if (logTick)
                    Log($"Start modifier tick at: {sw.Elapsed}");

                // gamedata modifiers update first
                if (GameData.Current && !GameData.Current.modifiers.IsEmpty())
                {
                    if (loop.reference == null)
                        loop.reference = GameData.Current;
                    loop.ValidateDictionary();
                    ModifiersHelper.RunModifiersLoop(GameData.Current.modifiers, loop);
                }

                OnObjectModifiersTick(); // modifiers update second
                OnBackgroundModifiersTick(); // bg modifiers update third
            }
            catch (Exception ex)
            {
                Debug.LogError($"Had an exception with modifier tick. Exception: {ex}");
            }

            if (logTick)
                Log($"Start event tick at: {sw.Elapsed}");

            OnEventsTick(); // events update fourth

            if (logTick)
                Log($"Start object tick at: {sw.Elapsed}");

            OnBeatmapObjectsTick(); // objects update fifth

            if (logTick)
                Log($"Start BG object tick at: {sw.Elapsed}");

            OnBackgroundObjectsTick(); // bgs update sixth

            if (logTick)
                Log($"Start prefab modifier tick at: {sw.Elapsed}");

            OnPrefabModifiersTick(); // prefab modifiers update seventh

            if (logTick)
                Log($"Start prefab object tick at: {sw.Elapsed}");

            OnPrefabObjectsTick(); // prefab objects update last


            if (logTick)
                Log($"Reset beatmap tick cache tick at: {sw.Elapsed}");

            // reset player cache
            if (RTBeatmap.Current)
            {
                RTBeatmap.Current.playerHit = false;
                RTBeatmap.Current.playerDied = false;
                RTBeatmap.Current.LevelStarted = false;
            }

            try
            {
                var level = LevelManager.CurrentLevel;
                if (CoreHelper.InEditor)
                    level = Editor.Managers.EditorLevelManager.inst?.CurrentLevel;
                level?.saveData?.UpdateState();
                LevelManager.CurrentLevelCollection?.saveData?.UpdateState();
            }
            catch
            {

            }

            if (logTick)
                Log($"Start post-tick at: {sw.Elapsed}");
            sw?.Stop();
            sw = null;

            PostTick();
            ScheduleTick();
        }

        public override void Clear()
        {
            Debug.Log($"{className}Cleaning up level");

            base.Clear();

            eventEngine = null;

            threadedTickRunner?.Dispose();
            threadedTickRunner = null;
        }

        /// <summary>
        /// Sets the game's current seed and updates all animations accordingly.
        /// </summary>
        /// <param name="seed">The seed to set.</param>
        public void InitSeed(int seed) => InitSeed(seed.ToString());

        /// <summary>
        /// Sets the game's current seed and updates all animations accordingly.
        /// </summary>
        /// <param name="seed">The seed to set.</param>
        public void InitSeed(string seed)
        {
            RandomHelper.SetSeed(seed);
            RecacheAllSequences();
        }

        /// <summary>
        /// Updates the game's current seed and updates all animations accordingly.
        /// </summary>
        public void InitSeed()
        {
            RandomHelper.UpdateSeed();
            RecacheAllSequences();
        }

        static void Log(string message) => Debug.Log($"{className}{message}");

        #endregion

        #region Samples

        /// <summary>
        /// Max amount of samples in audio.
        /// </summary>
        public const int MAX_SAMPLES = 256;

        /// <summary>
        /// Samples of the current audio.
        /// </summary>
        public float[] samples = new float[MAX_SAMPLES];

        /// <summary>
        /// Bass audio sample.
        /// </summary>
        public float sampleLow;

        /// <summary>
        /// Mids audio sample.
        /// </summary>
        public float sampleMid;

        /// <summary>
        /// High audio sample.
        /// </summary>
        public float sampleHigh;

        /// <summary>
        /// Gets a sample by a sample index and multiplies it by intensity.
        /// </summary>
        /// <param name="sample">Sample index to get.</param>
        /// <param name="intensity">Intensity to multiply the sample by.</param>
        /// <returns>Returns a sample.</returns>
        public float GetSample(int sample, float intensity) => samples[Mathf.Clamp(sample, 0, samples.Length - 1)] * intensity;

        #endregion

        #region Events

        /// <summary>
        /// Event time engine. Handles event interpolation.
        /// </summary>
        public EventEngine eventEngine;

        public virtual void OnEventsTick()
        {
            if (GameManager.inst.introMain && AudioManager.inst.CurrentAudioSource.time < 15f)
                GameManager.inst.introMain.SetActive(EventsConfig.Instance.ShowIntro.Value);

            if (EventManager.inst)
            {
                if (CoreConfig.Instance.ControllerRumble.Value && EventsConfig.Instance.ShakeAffectsController.Value)
                    InputDataManager.inst.SetAllControllerRumble(EventManager.inst.shakeMultiplier);

                if (EventManager.inst.shakeSequence == null && EventsConfig.Instance.ShakeEventMode.Value == ShakeType.Original)
                {
                    EventManager.inst.shakeSequence = DOTween.Sequence();

                    float strength = 3f;
                    int vibrato = 10;
                    float randomness = 90f;
                    EventManager.inst.shakeSequence.Insert(0f, DOTween.Shake(() => Vector3.zero, eventEngine.OverrideShake, AudioManager.inst.CurrentAudioSource.clip.length, strength, vibrato, randomness, true, false));
                }
            }

            eventEngine?.UpdateEditorCamera();
            eventEngine?.Update(CurrentTime);
            eventEngine?.Render();
        }

        public static bool LogUpdateEvents { get; set; } = true;

        // todo: implement if I ever end up caching events via sequences
        /// <summary>
        /// Updates a specific event.
        /// </summary>
        /// <param name="currentEvent">Type of event to update.</param>
        public virtual void UpdateEvents(int currentEvent)
        {
            if (LogUpdateEvents)
                CoreHelper.Log($"Updating event: {currentEvent}");

            EventManager.inst.eventSequence?.Kill();
            EventManager.inst.eventSequence = null;
            switch (currentEvent)
            {
                case EventEngine.SHAKE: {
                        eventEngine?.SetupShake();
                        EventManager.inst.shakeSequence?.Kill();
                        EventManager.inst.shakeSequence = null;
                        break;
                    }
                case EventEngine.THEME: {
                        EventManager.inst.themeSequence?.Kill();
                        EventManager.inst.themeSequence = null;
                        break;
                    }
            }

            if (!GameData.Current)
                return;

            GameData.Current.events[currentEvent] = GameData.Current.events[currentEvent].OrderBy(x => x.time).ToList();
            //GameData.Current.events[currentEvent].Sort((a, b) => a.time.CompareTo(b.time));

            if (CoreHelper.InEditor)
                GameData.Current.events[currentEvent].ForLoop((eventKeyframe, index) => eventKeyframe.timelineKeyframe.Index = index);
        }

        /// <summary>
        /// Updates all events.
        /// </summary>
        public virtual void UpdateEvents()
        {
            if (LogUpdateEvents)
                CoreHelper.Log("Updating all events");

            eventEngine?.SetupShake();
            EventManager.inst.eventSequence?.Kill();
            EventManager.inst.shakeSequence?.Kill();
            EventManager.inst.themeSequence?.Kill();
            EventManager.inst.eventSequence = null;
            EventManager.inst.shakeSequence = null;
            EventManager.inst.themeSequence = null;
            DOTween.Kill(false);

            if (!GameData.Current)
                return;

            for (int i = 0; i < GameData.Current.events.Count; i++)
            {
                GameData.Current.events[i] = GameData.Current.events[i].OrderBy(x => x.time).ToList();
                //GameData.Current.events[i].Sort((a, b) => a.time.CompareTo(b.time));

                if (CoreHelper.InEditor)
                    GameData.Current.events[i].ForLoop((eventKeyframe, index) => eventKeyframe.timelineKeyframe.Index = index);
            }
        }

        /// <summary>
        /// List of runtime cameras.
        /// </summary>
        public static class Cameras
        {
            /// <summary>
            /// Gets the list of cameras.
            /// </summary>
            /// <returns>Returns a list of game cameras.</returns>
            public static List<Camera> GetCameras() => new List<Camera>
            {
                BG,
                FG,
                UI,
            };

            /// <summary>
            /// Background layer camera.
            /// </summary>
            public static Camera BG => EventManager.inst ? EventManager.inst.camPer : null;
            /// <summary>
            /// Foreground layer camera.
            /// </summary>
            public static Camera FG => EventManager.inst ? EventManager.inst.cam : null;
            /// <summary>
            /// User Interface layer camera.
            /// </summary>
            public static Camera UI => RTEventManager.inst ? RTEventManager.inst.uiCam : null;
        }

        #endregion
    }
}
