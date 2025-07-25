using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

using DG.Tweening;

using BetterLegacy.Configs;
using BetterLegacy.Core.Data.Beatmap;
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

            var gameData = GameData.Current;

            // Removing and reinserting prefabs.
            gameData.beatmapObjects.RemoveAll(x => x.fromPrefab);
            gameData.backgroundObjects.RemoveAll(x => x.fromPrefab);

            Load(gameData);

            Debug.Log($"{className}Loaded {objects.Count} objects (original: {gameData.beatmapObjects.Count})");
        }

        public override void Tick()
        {
            AudioManager.inst.CurrentAudioSource.GetSpectrumData(samples, 0, FFTWindow.Rectangular);

            if (!CoreConfig.Instance.UseNewUpdateMethod.Value)
                CurrentTime = FixedTime;
            else
            {
                var smoothedTime = Mathf.SmoothDamp(previousAudioTime, FixedTime, ref audioTimeVelocity, 1.0f / 50.0f);
                CurrentTime = smoothedTime;
                previousAudioTime = smoothedTime;
            }

            PreTick();

            try
            {
                // gamedata modifiers update first
                if (GameData.Current && !GameData.Current.modifiers.IsEmpty())
                    ModifiersHelper.RunModifiersLoop(GameData.Current.modifiers, GameData.Current, new Dictionary<string, string>());

                OnObjectModifiersTick(); // modifiers update second
                OnBackgroundModifiersTick(); // bg modifiers update third
            }
            catch (Exception ex)
            {
                Debug.LogError($"Had an exception with modifier tick. Exception: {ex}");
            }

            OnEventsTick(); // events update fourth
            OnBeatmapObjectsTick(); // objects update fifth
            OnBackgroundObjectsTick(); // bgs update sixth
            OnPrefabModifiersTick(); // prefab modifiers update seventh
            OnPrefabObjectsTick(); // prefab objects update last

            //for (int i = 0; i < GameData.Current.prefabObjects.Count; i++)
            //    GameData.Current.prefabObjects[i].runtimeObject?.Tick();

            // reset player cache
            if (RTBeatmap.Current)
            {
                RTBeatmap.Current.playerHit = false;
                RTBeatmap.Current.playerDied = false;
                RTBeatmap.Current.LevelStarted = false;
            }

            PostTick();
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
            //RecacheAllSequences();
            // todo: figure out how randomization is handled for single object updating.
            // should it not update randomization? If so, that means the object can properly be updated and will not affect other objects.
            // if it should update randomization, that means parent objects also need to be updated. but also, how will randomization work if the ID is the same as before? is randomization based on something else?
        }

        /// <summary>
        /// Updates the game's current seed and updates all animations accordingly.
        /// </summary>
        public void InitSeed()
        {
            RandomHelper.UpdateSeed();
            //RecacheAllSequences();
        }

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

            if (EventManager.inst && (CoreHelper.Playing || CoreHelper.Reversing || LevelManager.LevelEnded))
            {
                if (CoreConfig.Instance.ControllerRumble.Value && EventsConfig.Instance.ShakeAffectsController.Value)
                    InputDataManager.inst.SetAllControllerRumble(EventManager.inst.shakeMultiplier);

                if (EventManager.inst.eventSequence == null)
                    EventManager.inst.eventSequence = DOTween.Sequence();
                if (EventManager.inst.themeSequence == null)
                    EventManager.inst.themeSequence = DOTween.Sequence();
                if (EventManager.inst.shakeSequence == null && EventsConfig.Instance.ShakeEventMode.Value == ShakeType.Original)
                {
                    EventManager.inst.shakeSequence = DOTween.Sequence();

                    float strength = 3f;
                    int vibrato = 10;
                    float randomness = 90f;
                    EventManager.inst.shakeSequence.Insert(0f, DOTween.Shake(() => Vector3.zero, delegate (Vector3 x)
                    {
                        EventManager.inst.shakeVector = x;
                    }, AudioManager.inst.CurrentAudioSource.clip.length, strength, vibrato, randomness, true, false));
                }
            }

            eventEngine?.UpdateEditorCamera();
            eventEngine?.Update(CurrentTime);
            eventEngine?.Render();
        }

        // todo: implement if I ever end up caching events via sequences
        /// <summary>
        /// Updates a specific event.
        /// </summary>
        /// <param name="currentEvent">Type of event to update.</param>
        public virtual void UpdateEvents(int currentEvent)
        {
            eventEngine?.SetupShake();
            EventManager.inst.eventSequence.Kill();
            EventManager.inst.shakeSequence.Kill();
            EventManager.inst.themeSequence.Kill();
            EventManager.inst.eventSequence = null;
            EventManager.inst.shakeSequence = null;
            EventManager.inst.themeSequence = null;

            if (!GameData.Current)
                return;

            GameData.Current.events[currentEvent] = GameData.Current.events[currentEvent].OrderBy(x => x.time).ToList();

            if (CoreHelper.InEditor)
                GameData.Current.events[currentEvent].ForLoop((eventKeyframe, index) => eventKeyframe.timelineKeyframe.Index = index);
        }

        /// <summary>
        /// Updates all events.
        /// </summary>
        public virtual void UpdateEvents()
        {
            eventEngine?.SetupShake();
            EventManager.inst.eventSequence.Kill();
            EventManager.inst.shakeSequence.Kill();
            EventManager.inst.themeSequence.Kill();
            EventManager.inst.eventSequence = null;
            EventManager.inst.shakeSequence = null;
            EventManager.inst.themeSequence = null;
            DOTween.Kill(false);

            if (!GameData.Current)
                return;

            for (int i = 0; i < GameData.Current.events.Count; i++)
            {
                GameData.Current.events[i] = GameData.Current.events[i].OrderBy(x => x.time).ToList();

                if (CoreHelper.InEditor)
                    GameData.Current.events[i].ForLoop((eventKeyframe, index) => eventKeyframe.timelineKeyframe.Index = index);
            }
        }

        #endregion
    }
}
