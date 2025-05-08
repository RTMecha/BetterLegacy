using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

using LSFunctions;

using DG.Tweening;

using BetterLegacy.Configs;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using BetterLegacy.Core.Runtime.Events;
using BetterLegacy.Core.Runtime.Objects;
using BetterLegacy.Core.Runtime.Objects.Visual;
using BetterLegacy.Core.Threading;
using BetterLegacy.Editor.Components;
using BetterLegacy.Editor.Managers;

using UnityObject = UnityEngine.Object;

namespace BetterLegacy.Core.Runtime
{
    public delegate void RuntimeObjectNotifier(IRTObject runtimeObject);

    /// <summary>
    /// Short for Runtime Level. Controls all objects, events and modifiers in the level.
    /// </summary>
    public class RTLevel : Exists
    {
        #region Core

        public RTLevel()
        {
            Debug.Log($"{className}Loading level");

            previousAudioTime = 0.0f;
            audioTimeVelocity = 0.0f;

            // Sets a new seed or uses the current one.
            RandomHelper.UpdateSeed();

            var gameData = GameData.Current;

            eventEngine = new EventEngine();

            // Removing and reinserting prefabs.
            gameData.beatmapObjects.RemoveAll(x => x.fromPrefab);
            gameData.backgroundObjects.RemoveAll(x => x.fromPrefab);
            for (int i = 0; i < gameData.prefabObjects.Count; i++)
                AddPrefabToLevel(gameData.prefabObjects[i], false);

            // Convert GameData to LevelObjects
            converter = new ObjectConverter(gameData);
            IEnumerable<IRTObject> runtimeObjects = converter.ToRuntimeObjects();

            objects = runtimeObjects.ToList();
            objectEngine = new ObjectEngine(Objects);

            IEnumerable<IRTObject> runtimeModifiers = converter.ToRuntimeModifiers();

            modifiers = runtimeModifiers.ToList();
            objectModifiersEngine = new ObjectEngine(Modifiers);

            IEnumerable<IRTObject> runtimeBGObjects = converter.ToRuntimeBGObjects();

            bgObjects = runtimeBGObjects.ToList();
            backgroundEngine = new ObjectEngine(BGObjects);

            IEnumerable<IRTObject> runtimeBGModifiers = converter.ToBGRuntimeModifiers();

            bgModifiers = runtimeBGModifiers.ToList();
            bgModifiersEngine = new ObjectEngine(BGModifiers);

            Debug.Log($"{className}Loaded {objects.Count} objects (original: {gameData.beatmapObjects.Count})");

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

        float previousAudioTime;
        float audioTimeVelocity;

        /// <summary>
        /// The current runtime level.
        /// </summary>
        public static RTLevel Current { get; set; }

        /// <summary>
        /// If the smooth method should be used for time updating.
        /// </summary>
        public static bool UseNewUpdateMethod { get; set; }

        /// <summary>
        /// The current time the objects are interpolating to.
        /// </summary>
        public float CurrentTime { get; set; }

        /// <summary>
        /// Fixed level time.
        /// </summary>
        public float FixedTime => AudioManager.inst.CurrentAudioSource.time;

        /// <summary>
        /// The current room to render. If the room number is 0, all objects are active regardless of room number.
        /// </summary>
        public int CurrentRoom { get; set; }

        /// <summary>
        /// Performs heavy calculations on a separate tick thread.
        /// </summary>
        public TickRunner threadedTickRunner;

        /// <summary>
        /// Queue of actions to run before the tick starts.
        /// </summary>
        public Queue<Action> preTick = new Queue<Action>();

        /// <summary>
        /// Queue of actions to run after the tick ends.
        /// </summary>
        public Queue<Action> postTick = new Queue<Action>();

        /// <summary>
        /// Initializes the runtime level.
        /// </summary>
        public static void Init()
        {
            Current?.Clear();
            Current = new RTLevel();
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
            }

            // Delete all the "GameObjects" children.
            LSHelpers.DeleteChildren(GameObject.Find("GameObjects").transform);

            // End and restart.
            if (restart)
                Init();
            else
                Current?.Clear();
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
            }

            // Delete all the "GameObjects" children.
            LSHelpers.DeleteChildren(GameObject.Find("GameObjects").transform);

            // End and restart.
            if (restart)
                Init();
            else
                Current?.Clear();

            yield break;
        }

        /// <summary>
        /// Ticks the runtime level.
        /// </summary>
        public void Tick()
        {
            AudioManager.inst.CurrentAudioSource.GetSpectrumData(samples, 0, FFTWindow.Rectangular);

            if (!UseNewUpdateMethod)
                CurrentTime = FixedTime;
            else
            {
                var smoothedTime = Mathf.SmoothDamp(previousAudioTime, FixedTime, ref audioTimeVelocity, 1.0f / 50.0f);
                CurrentTime = smoothedTime;
                previousAudioTime = smoothedTime;
            }

            while (preTick != null && !preTick.IsEmpty())
                preTick.Dequeue()?.Invoke();

            try
            {
                // gamedata modifiers update first
                if (GameData.Current && !GameData.Current.modifiers.IsEmpty())
                    ModifiersHelper.RunModifiersLoop(GameData.Current.modifiers, true, new Dictionary<string, string>());

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

            while (postTick != null && !postTick.IsEmpty())
                postTick.Dequeue()?.Invoke();
        }

        /// <summary>
        /// Clears the runtime levels' data.
        /// </summary>
        public void Clear()
        {
            Debug.Log($"{className}Cleaning up level");

            eventEngine = null;
            objectEngine = null;
            objectModifiersEngine = null;
            backgroundEngine = null;
            bgModifiersEngine = null;

            threadedTickRunner?.Dispose();
            threadedTickRunner = null;

            preTick.Clear();
            postTick.Clear();
        }

        /// <summary>
        /// Recalculate object states.
        /// </summary>
        public void RecalculateObjectStates()
        {
            objectEngine?.spawner?.RecalculateObjectStates();
            objectModifiersEngine?.spawner?.RecalculateObjectStates();
            backgroundEngine?.spawner?.RecalculateObjectStates();
            bgModifiersEngine?.spawner?.RecalculateObjectStates();
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

        void OnEventsTick()
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
        public void UpdateEvents(int currentEvent)
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
        public void UpdateEvents()
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

        #region Objects

        #region Sequences

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

        /// <summary>
        /// Updates every objects' animations.
        /// </summary>
        public void RecacheAllSequences()
        {
            if (!converter || !GameData.Current)
                return;

            var beatmapObjects = GameData.Current.beatmapObjects;
            for (int i = 0; i < beatmapObjects.Count; i++)
                UpdateCachedSequence(beatmapObjects[i]);
        }

        /// <summary>
        /// Updates objects' cached sequences without reinitialization.
        /// </summary>
        /// <param name="beatmapObject">Object to update.</param>
        public void UpdateCachedSequence(BeatmapObject beatmapObject) => converter.UpdateCachedSequence(beatmapObject, beatmapObject.cachedSequences);

        /// <summary>
        /// Removes or updates an objects' sequences.
        /// </summary>
        /// <param name="beatmapObject">Object to update.</param>
        /// <param name="reinsert">If the sequence should be reinserted or not.</param>
        /// <param name="updateParents">If the LevelObjects' parents should be updated.</param>
        /// <param name="recursive">If the method should run recursively.</param>
        public void RecacheSequences(BeatmapObject beatmapObject, bool reinsert = true, bool updateParents = true, bool recursive = true)
        {
            if (!reinsert)
            {
                // Recursive recaching.
                if (recursive)
                {
                    var beatmapObjects = GameData.Current.beatmapObjects;
                    for (int i = 0; i < beatmapObjects.Count; i++)
                    {
                        var bm = beatmapObjects[i];
                        if (bm.Parent == beatmapObject.id)
                            RecacheSequences(bm, reinsert, updateParents, recursive);
                    }
                }

                return;
            }

            var collection = converter.CacheSequence(beatmapObject);

            // Recursive recaching.
            if (recursive)
            {
                var beatmapObjects = GameData.Current.beatmapObjects;
                for (int i = 0; i < beatmapObjects.Count; i++)
                {
                    var bm = beatmapObjects[i];
                    if (bm.Parent == beatmapObject.id)
                        RecacheSequences(bm, reinsert, updateParents, recursive);
                }
            }

            var levelObject = beatmapObject.runtimeObject;

            if (!levelObject)
                return;

            if (levelObject.visualObject)
            {
                levelObject.visualObject.colorSequence = collection.ColorSequence;
                levelObject.visualObject.secondaryColorSequence = collection.SecondaryColorSequence;
            }

            if (updateParents)
                foreach (var levelParent in levelObject.parentObjects)
                {
                    var cachedSequences = levelParent.beatmapObject.cachedSequences;
                    if (cachedSequences)
                    {
                        levelParent.positionSequence = cachedSequences.PositionSequence;
                        levelParent.scaleSequence = cachedSequences.ScaleSequence;
                        levelParent.rotationSequence = cachedSequences.RotationSequence;
                    }
                }
        }

        /// <summary>
        /// Stops all homing keyframes and resets them to their base values.
        /// </summary>
        public void UpdateHomingKeyframes()
        {
            for (int i = 0; i < GameData.Current.beatmapObjects.Count; i++)
            {
                var cachedSequences = GameData.Current.beatmapObjects[i].cachedSequences;
                for (int j = 0; j < cachedSequences.PositionSequence.keyframes.Length; j++)
                    cachedSequences.PositionSequence.keyframes[j].Stop();
                for (int j = 0; j < cachedSequences.RotationSequence.keyframes.Length; j++)
                    cachedSequences.RotationSequence.keyframes[j].Stop();
                for (int j = 0; j < cachedSequences.ColorSequence.keyframes.Length; j++)
                    cachedSequences.ColorSequence.keyframes[j].Stop();
            }
        }

        #endregion

        /// <summary>
        /// Object time engine. Handles object spawning and interpolation.
        /// </summary>
        public ObjectEngine objectEngine;

        /// <summary>
        /// Object conversion system. Handles converting data objects to runtime objects.
        /// </summary>
        public ObjectConverter converter;

        /// <summary>
        /// Readonly collection of runtime objects.
        /// </summary>
        public IReadOnlyList<IRTObject> Objects => objects?.AsReadOnly();

        /// <summary>
        /// List of runtime objects.
        /// </summary>
        public List<IRTObject> objects;

        void OnBeatmapObjectsTick() => objectEngine?.Update(CurrentTime);

        /// <summary>
        /// Updates a Beatmap Object.
        /// </summary>
        /// <param name="beatmapObject">Beatmap Objec to update.</param>
        /// <param name="recache">If sequences should be recached.</param>
        /// <param name="update">If the object itself should be updated.</param>
        /// <param name="reinsert">If the runtime object should be reinserted.</param>
        /// <param name="recursive">If updating should be recursive.</param>
        /// <param name="recalculate">If the engine should recalculate.</param>
        public void UpdateObject(BeatmapObject beatmapObject, bool recache = true, bool update = true, bool reinsert = true, bool recursive = true, bool recalculate = true)
        {
            if (!reinsert)
            {
                recache = true;
                update = true;
            }

            if (recache)
                RecacheSequences(beatmapObject, reinsert, recursive: recursive);

            if (update)
                ReinitObject(beatmapObject, reinsert, recursive);

            if (recalculate)
                RecalculateObjectStates();
        }

        /// <summary>
        /// Updates a specific value.
        /// </summary>
        /// <param name="beatmapObject">The BeatmapObject to update.</param>
        /// <param name="context">The specific context to update under.</param>
        /// <param name="sort">If the objects should be recalculated depending on the context.</param>
        public void UpdateObject(BeatmapObject beatmapObject, string context, bool sort = true)
        {
            context = context.ToLower().Replace(" ", "").Replace("_", "");
            var runtimeObject = beatmapObject.runtimeObject;

            switch (context)
            {
                case ObjectContext.RENDERING: {
                        if (!runtimeObject)
                        {
                            RecacheSequences(beatmapObject);
                            break;
                        }

                        if (runtimeObject.visualObject is SolidObject solidObject)
                            solidObject.UpdateRendering((int)beatmapObject.gradientType, (int)beatmapObject.renderLayerType, false, beatmapObject.gradientScale, beatmapObject.gradientRotation);
                        else
                            runtimeObject.visualObject.SetRenderType((int)beatmapObject.renderLayerType);
                        RecacheSequences(beatmapObject);

                        break;
                    } // Material
                case ObjectContext.OBJECT_TYPE: {
                        // object was empty
                        if (!runtimeObject)
                        {
                            if (beatmapObject.objectType == BeatmapObject.ObjectType.Empty) // object is still empty
                                break;

                            UpdateObject(beatmapObject);

                            break;
                        }

                        // object was non-empty (normal, helper, etc)

                        // object is still non-empty
                        if (beatmapObject.objectType != BeatmapObject.ObjectType.Empty)
                        {
                            if (runtimeObject.visualObject)
                                runtimeObject.visualObject.opacity = beatmapObject.objectType == BeatmapObject.ObjectType.Helper ? 0.35f : 1.0f;

                            if (runtimeObject.visualObject is SolidObject solidObject)
                            {
                                bool deco = beatmapObject.objectType == BeatmapObject.ObjectType.Helper ||
                                                    beatmapObject.objectType == BeatmapObject.ObjectType.Decoration;

                                bool solid = beatmapObject.objectType == BeatmapObject.ObjectType.Solid;

                                solidObject.UpdateCollider(deco, solid, beatmapObject.opacityCollision);
                            }

                            break;
                        }

                        // object is now empty.
                        UpdateObject(beatmapObject);

                        break;
                    } // ObjectType
                case ObjectContext.START_TIME: {
                        if (beatmapObject.runtimeModifiers)
                        {
                            beatmapObject.runtimeModifiers.orderMatters = beatmapObject.orderModifiers;
                            beatmapObject.runtimeModifiers.StartTime = beatmapObject.ignoreLifespan ? 0f : beatmapObject.StartTime;
                            beatmapObject.runtimeModifiers.KillTime = beatmapObject.ignoreLifespan ? SoundManager.inst.MusicLength : beatmapObject.StartTime + beatmapObject.SpawnDuration;
                            beatmapObject.runtimeModifiers.SetActive(beatmapObject.ModifiersActive);
                        }

                        if (!runtimeObject)
                        {
                            if (sort)
                                Sort();
                            break;
                        }

                        runtimeObject.StartTime = beatmapObject.StartTime;
                        runtimeObject.KillTime = beatmapObject.StartTime + beatmapObject.SpawnDuration;

                        if (sort)
                            Sort();

                        runtimeObject.SetActive(beatmapObject.Alive);

                        foreach (var levelParent in runtimeObject.parentObjects)
                            levelParent.timeOffset = levelParent.beatmapObject.StartTime;

                        break;
                    } // StartTime
                case ObjectContext.AUTOKILL: {
                        if (!runtimeObject)
                            break;

                        runtimeObject.KillTime = beatmapObject.StartTime + beatmapObject.SpawnDuration;

                        if (!sort)
                            break;

                        objectEngine?.spawner?.deactivateList?.Sort((a, b) => a.KillTime.CompareTo(b.KillTime));
                        objectEngine?.spawner?.RecalculateObjectStates();
                        
                        objectModifiersEngine?.spawner?.deactivateList?.Sort((a, b) => a.KillTime.CompareTo(b.KillTime));
                        objectModifiersEngine?.spawner?.RecalculateObjectStates();

                        break;
                    } // Autokill
                case ObjectContext.PARENT: {
                        var parentChain = beatmapObject.GetParentChain();
                        if (beatmapObject.Parent == BeatmapObject.CAMERA_PARENT || parentChain.Count > 1 && parentChain[parentChain.Count - 1].Parent == BeatmapObject.CAMERA_PARENT)
                        {
                            var beatmapParent = parentChain.Count > 1 && parentChain[parentChain.Count - 1].Parent == BeatmapObject.CAMERA_PARENT ? parentChain[parentChain.Count - 1] : beatmapObject;

                            var childTree = beatmapObject.GetChildTree();
                            for (int i = 0; i < childTree.Count; i++)
                            {
                                var child = childTree[i];
                                var childLevelObject = child.runtimeObject;

                                childLevelObject.cameraParent = beatmapParent.Parent == BeatmapObject.CAMERA_PARENT;

                                childLevelObject.positionParent = beatmapParent.GetParentType(0);
                                childLevelObject.scaleParent = beatmapParent.GetParentType(1);
                                childLevelObject.rotationParent = beatmapParent.GetParentType(2);

                                childLevelObject.positionParentOffset = beatmapParent.parallaxSettings[0];
                                childLevelObject.scaleParentOffset = beatmapParent.parallaxSettings[1];
                                childLevelObject.rotationParentOffset = beatmapParent.parallaxSettings[2];
                            }
                        }
                        else
                            UpdateObject(beatmapObject);

                        break;
                    } // Parent
                case ObjectContext.PARENT_SETTING: {
                        var parentChain = beatmapObject.GetParentChain();
                        if (beatmapObject.Parent == BeatmapObject.CAMERA_PARENT || parentChain.Count > 1 && parentChain[parentChain.Count - 1].Parent == BeatmapObject.CAMERA_PARENT)
                        {
                            var beatmapParent = parentChain.Count > 1 && parentChain[parentChain.Count - 1].Parent == BeatmapObject.CAMERA_PARENT ? parentChain[parentChain.Count - 1] : beatmapObject;

                            var childTree = beatmapObject.GetChildTree();
                            for (int i = 0; i < childTree.Count; i++)
                            {
                                var child = childTree[i];
                                var childLevelObject = child.runtimeObject;

                                childLevelObject.cameraParent = beatmapParent.Parent == BeatmapObject.CAMERA_PARENT;

                                childLevelObject.positionParent = beatmapParent.GetParentType(0);
                                childLevelObject.scaleParent = beatmapParent.GetParentType(1);
                                childLevelObject.rotationParent = beatmapParent.GetParentType(2);

                                childLevelObject.positionParentOffset = beatmapParent.parallaxSettings[0];
                                childLevelObject.scaleParentOffset = beatmapParent.parallaxSettings[1];
                                childLevelObject.rotationParentOffset = beatmapParent.parallaxSettings[2];
                            }
                        }

                        if (!runtimeObject)
                            break;

                        foreach (var levelParent in runtimeObject.parentObjects)
                        {
                            if (GameData.Current.beatmapObjects.TryFind(x => x.id == levelParent.id, out BeatmapObject parent))
                            {
                                levelParent.parentAnimatePosition = parent.GetParentType(0);
                                levelParent.parentAnimateScale = parent.GetParentType(1);
                                levelParent.parentAnimateRotation = parent.GetParentType(2);

                                levelParent.parentOffsetPosition = parent.GetParentOffset(0);
                                levelParent.parentOffsetScale = parent.GetParentOffset(1);
                                levelParent.parentOffsetRotation = parent.GetParentOffset(2);
                            }
                        }

                        break;
                    }
                case ObjectContext.PARENT_CHAIN: {
                        UpdateParentChain(beatmapObject, runtimeObject);
                        break;
                    }
                case ObjectContext.VISUAL_OFFSET: {
                        if (!runtimeObject)
                            break;

                        runtimeObject.depth = beatmapObject.Depth;
                        if (runtimeObject.visualObject)
                            runtimeObject.visualObject.SetOrigin(new Vector3(beatmapObject.origin.x, beatmapObject.origin.y, beatmapObject.Depth * 0.1f));

                        break;
                    } // Origin & Depth
                case ObjectContext.SHAPE: {
                        UpdateVisualObject(beatmapObject, runtimeObject);

                        break;
                    } // Shape
                case ObjectContext.POLYGONS: {
                        UpdateVisualObject(beatmapObject, runtimeObject);

                        break;
                    } // Polygons
                case ObjectContext.IMAGE: {
                        if (runtimeObject && runtimeObject.visualObject is ImageObject imageObject)
                            imageObject.UpdateImage(beatmapObject.text, GameData.Current.assets.GetSprite(beatmapObject.text));

                        break;
                    } // Image
                case ObjectContext.TEXT: {
                        if (runtimeObject && runtimeObject.visualObject is TextObject textObject)
                        {
                            textObject.text = beatmapObject.text;
                            textObject.SetText(textObject.text);
                        }

                        break;
                    } // Text
                case ObjectContext.KEYFRAMES: {
                        if (!runtimeObject)
                        {
                            RecacheSequences(beatmapObject);
                            break;
                        }

                        runtimeObject.KillTime = beatmapObject.StartTime + beatmapObject.SpawnDuration;
                        RecacheSequences(beatmapObject);

                        break;
                    } // Keyframes
                case ObjectContext.MODIFIERS: {
                        var runtimeModifiers = beatmapObject.runtimeModifiers;

                        if (runtimeModifiers)
                        {
                            objectModifiersEngine?.spawner?.RemoveObject(runtimeModifiers, false);
                            modifiers.Remove(runtimeModifiers);

                            runtimeModifiers = null;
                            beatmapObject.runtimeModifiers = null;
                        }

                        var iRuntimeModifiers = converter.ToIRuntimeModifiers(beatmapObject);
                        if (iRuntimeModifiers != null)
                        {
                            modifiers.Add(iRuntimeModifiers);
                            objectModifiersEngine?.spawner?.InsertObject(iRuntimeModifiers, false);
                        }

                        if (sort)
                            objectModifiersEngine?.spawner?.RecalculateObjectStates();

                        break;
                    }
                case ObjectContext.SELECTABLE: {
                        if (!beatmapObject.runtimeObject || beatmapObject.editorData.selectable == beatmapObject.selector)
                            break;

                        if (beatmapObject.selector)
                        {
                            CoreHelper.Destroy(beatmapObject.selector);
                            break;
                        }

                        var obj = beatmapObject.runtimeObject.visualObject.gameObject.AddComponent<SelectObject>();
                        obj.SetObject(beatmapObject);
                        beatmapObject.selector = obj;

                        break;
                    }
                case ObjectContext.HIDE: {
                        if (!beatmapObject.runtimeObject || beatmapObject.runtimeObject.parentObjects.IsEmpty() || !beatmapObject.runtimeObject.parentObjects[0].gameObject)
                            break;

                        beatmapObject.runtimeObject.parentObjects[0].gameObject.SetActive(!beatmapObject.editorData.hidden);

                        break;
                    }
            }
        }

        /// <summary>
        /// Removes and recreates the object if it still exists.
        /// </summary>
        /// <param name="beatmapObject">Beatmap Object to update.</param>
        /// <param name="objects">Runtime object list.</param>
        /// <param name="converter">Object converter.</param>
        /// <param name="spawner">Object spawner.</param>
        /// <param name="reinsert">If the object should be reinserted.</param>
        /// <param name="recursive">If the updating should be recursive.</param>
        public void ReinitObject(BeatmapObject beatmapObject, bool reinsert = true, bool recursive = true)
        {
            string id = beatmapObject.id;

            beatmapObject.reactivePositionOffset = Vector3.zero;
            beatmapObject.reactiveScaleOffset = Vector3.zero;
            beatmapObject.reactiveRotationOffset = 0f;
            beatmapObject.positionOffset = Vector3.zero;
            beatmapObject.scaleOffset = Vector3.zero;
            beatmapObject.rotationOffset = Vector3.zero;

            // Recursing updating.
            if (recursive)
            {
                var beatmapObjects = GameData.Current.beatmapObjects;
                for (int i = 0; i < beatmapObjects.Count; i++)
                {
                    var bm = beatmapObjects[i];
                    if (bm.Parent == id)
                        ReinitObject(bm, reinsert, recursive);
                }
            }

            var runtimeObject = beatmapObject.runtimeObject;

            if (runtimeObject)
            {
                var top = runtimeObject.top;
                if (top)
                    UnityObject.Destroy(top.gameObject);

                objectEngine?.spawner?.RemoveObject(runtimeObject, false);
                objects.Remove(runtimeObject);

                runtimeObject.parentObjects.Clear();

                runtimeObject = null;
                top = null;
                beatmapObject.runtimeObject = null;
            }

            var runtimeModifiers = beatmapObject.runtimeModifiers;

            if (runtimeModifiers)
            {
                objectModifiersEngine?.spawner?.RemoveObject(runtimeModifiers, false);
                modifiers.Remove(runtimeModifiers);

                runtimeModifiers = null;
                beatmapObject.runtimeModifiers = null;
            }

            // If the object should be reinserted and it is not null then we reinsert the object.
            if (!reinsert || !beatmapObject)
                return;

            // Convert object to ILevelObject.
            var iRuntimeObject = converter.ToIRuntimeObject(beatmapObject);
            if (iRuntimeObject != null)
            {
                objects.Add(iRuntimeObject);
                objectEngine?.spawner?.InsertObject(iRuntimeObject, false);
            }

            var iRuntimeModifiers = converter.ToIRuntimeModifiers(beatmapObject);
            if (iRuntimeModifiers != null)
            {
                modifiers.Add(iRuntimeModifiers);
                objectModifiersEngine?.spawner?.InsertObject(iRuntimeModifiers, false);
            }
        }

        void UpdateParentChain(BeatmapObject beatmapObject, RTBeatmapObject runtimeObject = null)
        {
            string id = beatmapObject.id;
            var beatmapObjects = GameData.Current.beatmapObjects;
            for (int i = 0; i < beatmapObjects.Count; i++)
            {
                var bm = beatmapObjects[i];
                if (bm.Parent == id)
                    UpdateParentChain(bm, bm.runtimeObject);
            }

            if (!runtimeObject)
                return;

            var baseObject = runtimeObject.visualObject.gameObject.transform.parent;
            baseObject.SetParent(runtimeObject.top);

            for (int i = 1; i < runtimeObject.parentObjects.Count; i++)
                CoreHelper.Destroy(runtimeObject.parentObjects[i].gameObject);

            var parentObjects = new List<ParentObject>();

            if (!string.IsNullOrEmpty(beatmapObject.Parent) && GameData.Current.beatmapObjects.TryFind(x => x.id == beatmapObject.Parent, out BeatmapObject beatmapObjectParent))
                converter.InitParentChain(beatmapObjectParent, parentObjects);

            var lastParent = !parentObjects.IsEmpty() && parentObjects[0] && parentObjects[0].transform ?
                parentObjects[0].transform : null;

            var p = converter.InitLevelParentObject(beatmapObject, baseObject.gameObject);
            if (!parentObjects.IsEmpty())
                parentObjects.Insert(0, p);
            else
                parentObjects.Add(p);

            var top = !parentObjects.IsEmpty() && parentObjects[parentObjects.Count - 1] && parentObjects[parentObjects.Count - 1].transform ?
                parentObjects[parentObjects.Count - 1].transform : baseObject.transform;

            top.SetParent(runtimeObject.top);
            top.localScale = Vector3.one;

            if (lastParent)
                baseObject.SetParent(lastParent);

            runtimeObject.parentObjects = parentObjects;

            var pc = beatmapObject.GetParentChain();

            if (pc == null || pc.IsEmpty())
                return;

            var beatmapParent = pc[pc.Count - 1];

            runtimeObject.cameraParent = beatmapParent.Parent == BeatmapObject.CAMERA_PARENT;

            runtimeObject.positionParent = beatmapParent.GetParentType(0);
            runtimeObject.scaleParent = beatmapParent.GetParentType(1);
            runtimeObject.rotationParent = beatmapParent.GetParentType(2);

            runtimeObject.positionParentOffset = beatmapParent.parallaxSettings[0];
            runtimeObject.scaleParentOffset = beatmapParent.parallaxSettings[1];
            runtimeObject.rotationParentOffset = beatmapParent.parallaxSettings[2];
        }

        void UpdateVisualObject(BeatmapObject beatmapObject, RTBeatmapObject runtimeObject)
        {
            if (!runtimeObject)
                return;

            var parent = runtimeObject.parentObjects[0].transform?.parent;

            CoreHelper.Destroy(runtimeObject.parentObjects[0].transform.gameObject);
            runtimeObject.visualObject?.Clear();
            runtimeObject.visualObject = null;

            var shape = Mathf.Clamp(beatmapObject.shape, 0, ObjectManager.inst.objectPrefabs.Count - 1);
            var shapeOption = Mathf.Clamp(beatmapObject.shapeOption, 0, ObjectManager.inst.objectPrefabs[shape].options.Count - 1);
            var shapeType = (ShapeType)shape;

            GameObject baseObject = UnityObject.Instantiate(ObjectManager.inst.objectPrefabs[shape].options[shapeOption], parent ? parent.transform : ObjectManager.inst.objectParent.transform);

            baseObject.transform.localScale = Vector3.one;

            var visualObject = baseObject.transform.GetChild(0).gameObject;
            if (beatmapObject.ShapeType != ShapeType.Text || !beatmapObject.autoTextAlign)
                visualObject.transform.localPosition = new Vector3(beatmapObject.origin.x, beatmapObject.origin.y, beatmapObject.Depth * 0.1f);
            visualObject.name = "Visual [ " + beatmapObject.name + " ]";

            runtimeObject.parentObjects[0] = converter.InitLevelParentObject(beatmapObject, baseObject);

            baseObject.name = beatmapObject.name;

            baseObject.SetActive(true);
            visualObject.SetActive(true);

            // Init visual object wrapper
            float opacity = beatmapObject.objectType == BeatmapObject.ObjectType.Helper ? 0.35f : 1.0f;
            bool hasCollider = beatmapObject.objectType == BeatmapObject.ObjectType.Helper ||
                               beatmapObject.objectType == BeatmapObject.ObjectType.Decoration;

            bool isSolid = beatmapObject.objectType == BeatmapObject.ObjectType.Solid;

            VisualObject visual = shapeType switch
            {
                ShapeType.Text => new TextObject(visualObject, opacity, beatmapObject.text, beatmapObject.autoTextAlign, TextObject.GetAlignment(beatmapObject.origin), (int)beatmapObject.renderLayerType),
                ShapeType.Image => new ImageObject(visualObject, opacity, beatmapObject.text, (int)beatmapObject.renderLayerType, GameData.Current.assets.GetSprite(beatmapObject.text)),
                ShapeType.Polygon => new PolygonObject(visualObject, opacity, hasCollider, isSolid, (int)beatmapObject.renderLayerType, beatmapObject.opacityCollision, (int)beatmapObject.gradientType, beatmapObject.gradientScale, beatmapObject.gradientRotation, beatmapObject.polygonShapeSettings),
                _ => new SolidObject(visualObject, opacity, hasCollider, isSolid, (int)beatmapObject.renderLayerType, beatmapObject.opacityCollision, (int)beatmapObject.gradientType, beatmapObject.gradientScale, beatmapObject.gradientRotation),
            };

            if (CoreHelper.InEditor)
            {
                runtimeObject.parentObjects[0].gameObject.SetActive(!beatmapObject.editorData.hidden);

                if (beatmapObject.editorData.selectable)
                {
                    var obj = visualObject.AddComponent<SelectObject>();
                    obj.SetObject(beatmapObject);
                    beatmapObject.selector = obj;
                }

                UnityObject.Destroy(visualObject.GetComponent<SelectObjectInEditor>());
            }

            visual.colorSequence = beatmapObject.cachedSequences.ColorSequence;
            visual.secondaryColorSequence = beatmapObject.cachedSequences.SecondaryColorSequence;

            runtimeObject.visualObject = visual;
        }

        /// <summary>
        /// Library of values to update a <see cref="BeatmapObject"/>.
        /// </summary>
        public static class ObjectContext
        {
            public const string START_TIME = "starttime";
            public const string RENDERING = "rendering";
            public const string OBJECT_TYPE = "objecttype";
            public const string AUTOKILL = "autokill";
            public const string PARENT = "parent";
            public const string PARENT_SETTING = "parentsetting";
            public const string PARENT_CHAIN = "parentchain";
            public const string VISUAL_OFFSET = "visualoffset";
            public const string SHAPE = "shape";
            public const string TEXT = "text";
            public const string POLYGONS = "polygons";
            public const string IMAGE = "image";
            public const string KEYFRAMES = "keyframes";
            public const string MODIFIERS = "modifiers";
            public const string SELECTABLE = "selectable";
            public const string HIDE = "hide";
        }

        #region Modifiers

        /// <summary>
        /// Modifiers time engine. Handles active / inactive modifiers efficiently.
        /// </summary>
        public ObjectEngine objectModifiersEngine;

        /// <summary>
        /// Readonly collection of runtime modifiers.
        /// </summary>
        public IReadOnlyList<IRTObject> Modifiers => modifiers?.AsReadOnly();

        /// <summary>
        /// List of runtime modifiers.
        /// </summary>
        public List<IRTObject> modifiers;

        void OnObjectModifiersTick()
        {
            if (!GameData.Current || !CoreHelper.Playing)
                return;

            objectModifiersEngine?.Update(FixedTime);

            foreach (var audioSource in ModifiersManager.audioSources)
            {
                try
                {
                    if (GameData.Current.beatmapObjects.Find(x => x.id == audioSource.Key) == null || !GameData.Current.beatmapObjects.Find(x => x.id == audioSource.Key).Alive)
                        ModifiersManager.queuedAudioToDelete.Add(audioSource);
                }
                catch
                {

                }
            }

            if (ModifiersManager.queuedAudioToDelete.IsEmpty())
                return;

            foreach (var audio in ModifiersManager.queuedAudioToDelete)
                ModifiersManager.DeleteKey(audio.Key, audio.Value);
            ModifiersManager.queuedAudioToDelete.Clear();
        }

        #endregion

        #endregion

        #region Background Objects

        /// <summary>
        /// Background Objects time engine. Handles BG object spawning and interpolation.
        /// </summary>
        public ObjectEngine backgroundEngine;

        /// <summary>
        /// Readonly collection of runtime BG objects.
        /// </summary>
        public IReadOnlyList<IRTObject> BGObjects => bgObjects?.AsReadOnly();

        /// <summary>
        /// List of runtime BG objects.
        /// </summary>
        public List<IRTObject> bgObjects;

        void OnBackgroundObjectsTick()
        {
            if (CoreConfig.Instance.ShowBackgroundObjects.Value && (CoreHelper.Playing || LevelManager.LevelEnded && ArcadeHelper.ReplayLevel) && BackgroundManager.inst?.backgroundParent?.gameObject)
                backgroundEngine?.Update(CurrentTime);
        }

        /// <summary>
        /// Updates all BackgroundObjects.
        /// </summary>
        public void UpdateBackgroundObjects()
        {
            foreach (var backgroundObject in GameData.Current.backgroundObjects)
                UpdateBackgroundObject(backgroundObject);
        }

        /// <summary>
        /// Updates a Background Object.
        /// </summary>
        /// <param name="backgroundObject">Beatmap Objec to update.</param>
        /// <param name="reinsert">If the runtime object should be reinserted.</param>
        /// <param name="recalculate">If the engine should recalculate.</param>
        public void UpdateBackgroundObject(BackgroundObject backgroundObject, bool reinsert = true, bool recalculate = true)
        {
            ReinitObject(backgroundObject, reinsert);

            if (recalculate)
                backgroundEngine?.spawner?.RecalculateObjectStates();
        }

        public void UpdateBackgroundObject(BackgroundObject backgroundObject, string context, bool sort = true)
        {
            context = context.ToLower().Replace(" ", "").Replace("_", "");
            var runtimeObject = backgroundObject.runtimeObject;

            switch (context)
            {
                case BackgroundObjectContext.START_TIME: {
                        if (backgroundObject.runtimeModifiers)
                        {
                            backgroundObject.runtimeModifiers.orderMatters = backgroundObject.orderModifiers;
                            backgroundObject.runtimeModifiers.StartTime = backgroundObject.ignoreLifespan ? 0f : backgroundObject.StartTime;
                            backgroundObject.runtimeModifiers.KillTime = backgroundObject.ignoreLifespan ? SoundManager.inst.MusicLength : backgroundObject.StartTime + backgroundObject.SpawnDuration;
                            backgroundObject.runtimeModifiers.SetActive(backgroundObject.ModifiersActive);
                        }

                        if (!runtimeObject)
                        {
                            if (sort)
                                Sort();
                            break;
                        }

                        runtimeObject.StartTime = backgroundObject.StartTime;
                        runtimeObject.KillTime = backgroundObject.StartTime + backgroundObject.SpawnDuration;

                        if (sort)
                            Sort();

                        runtimeObject.SetActive(backgroundObject.Alive);

                        break;
                    }
                case BackgroundObjectContext.AUTOKILL: {
                        if (!runtimeObject)
                            break;

                        runtimeObject.KillTime = backgroundObject.StartTime + backgroundObject.SpawnDuration;

                        if (!sort)
                            break;

                        backgroundEngine?.spawner?.deactivateList?.Sort((a, b) => a.KillTime.CompareTo(b.KillTime));
                        backgroundEngine?.spawner?.RecalculateObjectStates();

                        bgModifiersEngine?.spawner?.deactivateList?.Sort((a, b) => a.KillTime.CompareTo(b.KillTime));
                        bgModifiersEngine?.spawner?.RecalculateObjectStates();

                        break;
                    }
                case BackgroundObjectContext.MODIFIERS: {
                        var runtimeModifiers = backgroundObject.runtimeModifiers;

                        if (runtimeModifiers)
                        {
                            bgModifiersEngine?.spawner?.RemoveObject(runtimeModifiers, false);
                            bgModifiers.Remove(runtimeModifiers);

                            runtimeModifiers = null;
                            backgroundObject.runtimeModifiers = null;
                        }

                        var iRuntimeModifiers = converter.ToIRuntimeModifiers(backgroundObject);
                        if (iRuntimeModifiers != null)
                        {
                            bgModifiers.Add(iRuntimeModifiers);
                            bgModifiersEngine?.spawner?.InsertObject(iRuntimeModifiers, false);
                        }

                        if (sort)
                            bgModifiersEngine?.spawner?.RecalculateObjectStates();

                        break;
                    }
                case BackgroundObjectContext.HIDE: {
                        if (backgroundObject.runtimeObject)
                            backgroundObject.runtimeObject.hidden = backgroundObject.editorData.hidden;

                        break;
                    }
            }
        }

        /// <summary>
        /// Removes and recreates the object if it still exists.
        /// </summary>
        /// <param name="backgroundObject">Background Object to update.</param>
        /// <param name="reinsert">If the object should be reinserted.</param>
        /// <param name="recursive">If the updating should be recursive.</param>
        public void ReinitObject(BackgroundObject backgroundObject, bool reinsert = true)
        {
            backgroundObject.positionOffset = Vector3.zero;
            backgroundObject.scaleOffset = Vector3.zero;
            backgroundObject.rotationOffset = Vector3.zero;

            var runtimeObject = backgroundObject.runtimeObject;

            if (runtimeObject)
            {
                runtimeObject.Clear();
                backgroundEngine?.spawner?.RemoveObject(runtimeObject, false);
                bgObjects.Remove(runtimeObject);

                runtimeObject = null;
                backgroundObject.runtimeObject = null;
            }

            var runtimeModifiers = backgroundObject.runtimeModifiers;

            if (runtimeModifiers)
            {
                bgModifiersEngine?.spawner?.RemoveObject(runtimeModifiers, false);
                bgModifiers.Remove(runtimeModifiers);

                runtimeModifiers = null;
                backgroundObject.runtimeModifiers = null;
            }

            // If the object should be reinserted and it is not null then we reinsert the object.
            if (!reinsert || !backgroundObject)
                return;

            // Convert object to ILevelObject.
            var iRuntimeBGObject = converter.ToIRuntimeBGObject(backgroundObject);
            if (iRuntimeBGObject != null)
            {
                bgObjects.Add(iRuntimeBGObject);
                backgroundEngine?.spawner?.InsertObject(iRuntimeBGObject, false);
            }

            var iRuntimeBGModifiers = converter.ToIRuntimeModifiers(backgroundObject);
            if (iRuntimeBGModifiers != null)
            {
                bgModifiers.Add(iRuntimeBGModifiers);
                bgModifiersEngine?.spawner?.InsertObject(iRuntimeBGModifiers, false);
            }
        }

        /// <summary>
        /// Library of values to update a <see cref="BackgroundObject"/>.
        /// </summary>
        public static class BackgroundObjectContext
        {
            public const string START_TIME = "starttime";
            public const string AUTOKILL = "autokill";
            public const string MODIFIERS = "modifiers";
            public const string HIDE = "hide";
        }

        #region Modifiers

        /// <summary>
        /// Modifiers time engine. Handles active / inactive modifiers efficiently.
        /// </summary>
        public ObjectEngine bgModifiersEngine;

        /// <summary>
        /// Readonly collection of runtime modifiers.
        /// </summary>
        public IReadOnlyList<IRTObject> BGModifiers => bgModifiers?.AsReadOnly();

        /// <summary>
        /// List of runtime modifiers.
        /// </summary>
        public List<IRTObject> bgModifiers;

        void OnBackgroundModifiersTick()
        {
            if (!CoreConfig.Instance.ShowBackgroundObjects.Value || !CoreHelper.Playing)
                return;

            bgModifiersEngine?.Update(CurrentTime);
        }

        #endregion

        #endregion

        #region Prefabs

        /// <summary>
        /// Updates a Prefab Object.
        /// </summary>
        /// <param name="prefabObject">The Prefab Object to update.</param>
        /// <param name="reinsert">If the object should be updated or removed.</param>
        public void UpdatePrefab(PrefabObject prefabObject, bool reinsert = true, bool recalculate = true)
        {
            var gameData = GameData.Current;

            for (int i = 0; i < gameData.beatmapObjects.Count; i++)
            {
                var beatmapObject = gameData.beatmapObjects[i];
                if (string.IsNullOrEmpty(beatmapObject.Parent) && beatmapObject.prefabInstanceID == prefabObject.id)
                    UpdateObject(beatmapObject, reinsert: false, recalculate: recalculate);
            }
            
            for (int i = 0; i < gameData.backgroundObjects.Count; i++)
            {
                var backgroundObject = gameData.backgroundObjects[i];
                if (backgroundObject.prefabInstanceID == prefabObject.id)
                    UpdateBackgroundObject(backgroundObject, reinsert: false, recalculate: recalculate);
            }

            gameData.beatmapObjects.RemoveAll(x => x.prefabInstanceID == prefabObject.id);
            gameData.backgroundObjects.RemoveAll(x => x.prefabInstanceID == prefabObject.id);

            if (reinsert)
                AddPrefabToLevel(prefabObject, recalculate: recalculate);
        }

        /// <summary>
        /// Updates a singled out value of a Prefab Object.
        /// </summary>
        /// <param name="prefabObject">The Prefab Object to update.</param>
        /// <param name="context">The context to update.</param>
        public void UpdatePrefab(PrefabObject prefabObject, string context, bool sort = true)
        {
            string lower = context.ToLower().Replace(" ", "").Replace("_", "");
            switch (lower)
            {
                case PrefabContext.TRANSFORM_OFFSET: {
                        var transform = prefabObject.GetTransformOffset();

                        foreach (var prefabable in prefabObject.expandedObjects)
                        {
                            if (prefabable.GetRuntimeObject() is IPrefabOffset prefabOffset)
                            {
                                prefabOffset.PrefabOffsetPosition = transform.position;
                                prefabOffset.PrefabOffsetScale = new Vector3(transform.scale.x, transform.scale.y, 1f);
                                prefabOffset.PrefabOffsetRotation = new Vector3(0f, 0f, transform.rotation);
                            }
                        }

                        break;
                    }
                case PrefabContext.TIME:
                case PrefabContext.SPEED: {
                        float t = 1f;

                        if (prefabObject.RepeatOffsetTime != 0f)
                            t = prefabObject.RepeatOffsetTime;

                        float timeToAdd = 0f;

                        var prefab = prefabObject.GetPrefab();

                        for (int i = 0; i < prefabObject.RepeatCount + 1; i++)
                        {
                            foreach (var beatmapObject in GameData.Current.beatmapObjects.FindAll(x => x.fromPrefab && x.prefabInstanceID == prefabObject.id))
                            {
                                if (!prefab.beatmapObjects.TryFind(x => x.id == beatmapObject.originalID, out BeatmapObject original))
                                    continue;

                                beatmapObject.StartTime = prefabObject.StartTime + prefab.offset + ((original.StartTime + timeToAdd) / prefabObject.Speed);

                                if (lower == PrefabContext.SPEED)
                                {
                                    for (int j = 0; j < beatmapObject.events.Count; j++)
                                        for (int k = 0; k < beatmapObject.events[j].Count; k++)
                                            beatmapObject.events[i][k].time = original.events[i][k].time / prefabObject.Speed;

                                    UpdateObject(beatmapObject, ObjectContext.KEYFRAMES);
                                }

                                var runtimeObject = beatmapObject.runtimeObject;

                                // Update Start Time
                                if (!runtimeObject)
                                    continue;

                                runtimeObject.StartTime = beatmapObject.StartTime;
                                runtimeObject.KillTime = beatmapObject.StartTime + beatmapObject.SpawnDuration;

                                runtimeObject.SetActive(beatmapObject.Alive);

                                for (int j = 0; j < runtimeObject.parentObjects.Count; j++)
                                {
                                    var levelParent = runtimeObject.parentObjects[j];
                                    var parent = levelParent.beatmapObject;

                                    levelParent.timeOffset = parent.StartTime;
                                }
                            }

                            foreach (var backgroundObject in GameData.Current.backgroundObjects.FindAll(x => x.fromPrefab && x.prefabInstanceID == prefabObject.id))
                            {
                                if (!prefab.backgroundObjects.TryFind(x => x.id == backgroundObject.originalID, out BackgroundObject original))
                                    continue;

                                backgroundObject.StartTime = prefabObject.StartTime + prefab.offset + ((original.StartTime + timeToAdd) / prefabObject.Speed);

                                var runtimeObject = backgroundObject.runtimeObject;

                                // Update Start Time
                                if (!runtimeObject)
                                    continue;

                                runtimeObject.StartTime = backgroundObject.StartTime;
                                runtimeObject.KillTime = backgroundObject.StartTime + backgroundObject.SpawnDuration;

                                runtimeObject.SetActive(backgroundObject.Alive);
                            }

                            timeToAdd += t;
                        }

                        if (!sort)
                            break;

                        objectEngine?.spawner?.activateList?.Sort((a, b) => a.StartTime.CompareTo(b.StartTime));
                        objectEngine?.spawner?.deactivateList?.Sort((a, b) => a.KillTime.CompareTo(b.KillTime));
                        objectEngine?.spawner?.RecalculateObjectStates();

                        objectModifiersEngine?.spawner?.activateList?.Sort((a, b) => a.StartTime.CompareTo(b.StartTime));
                        objectModifiersEngine?.spawner?.deactivateList?.Sort((a, b) => a.KillTime.CompareTo(b.KillTime));
                        objectModifiersEngine?.spawner?.RecalculateObjectStates();
                        
                        backgroundEngine?.spawner?.activateList?.Sort((a, b) => a.StartTime.CompareTo(b.StartTime));
                        backgroundEngine?.spawner?.deactivateList?.Sort((a, b) => a.KillTime.CompareTo(b.KillTime));
                        backgroundEngine?.spawner?.RecalculateObjectStates();

                        bgModifiersEngine?.spawner?.activateList?.Sort((a, b) => a.StartTime.CompareTo(b.StartTime));
                        bgModifiersEngine?.spawner?.deactivateList?.Sort((a, b) => a.KillTime.CompareTo(b.KillTime));
                        bgModifiersEngine?.spawner?.RecalculateObjectStates();

                        break;
                    }
                case PrefabContext.AUTOKILL: {
                        UpdatePrefab(prefabObject);

                        // only issue with this rn is when objects already have autokill applied to them via the prefab object.

                        //if (prefabObject.autoKillType == PrefabObject.AutoKillType.Regular)
                        //{
                        //    UpdatePrefab(prefabObject);
                        //    break;
                        //}

                        //var prefab = prefabObject.GetPrefab();
                        //var time = prefabObject.StartTime + (prefab?.offset ?? 0f);

                        //for (int i = 0; i < prefabObject.expandedObjects.Count; i++)
                        //{
                        //    var beatmapObject = prefabObject.expandedObjects[i];

                        //    if (time + beatmapObject.SpawnDuration > prefabObject.autoKillOffset)
                        //    {
                        //        beatmapObject.autoKillType = BeatmapObject.AutoKillType.SongTime;
                        //        beatmapObject.autoKillOffset = prefabObject.autoKillType == PrefabObject.AutoKillType.StartTimeOffset ? time + prefabObject.autoKillOffset : prefabObject.autoKillOffset;
                        //    }

                        //    UpdateObject(beatmapObject, ObjectContext.START_TIME);
                        //}

                        break;
                    }
                case PrefabContext.PARENT: {
                        for (int i = 0; i < prefabObject.expandedObjects.Count; i++)
                        {
                            var beatmapObject = prefabObject.expandedObjects[i] as BeatmapObject;
                            if (!beatmapObject || !beatmapObject.fromPrefabBase)
                                continue;

                            beatmapObject.Parent = prefabObject.parent;
                            beatmapObject.parentType = prefabObject.parentType;
                            beatmapObject.parentOffsets = prefabObject.parentOffsets;
                            beatmapObject.parentAdditive = prefabObject.parentAdditive;
                            beatmapObject.parallaxSettings = prefabObject.parentParallax;
                            beatmapObject.desync = prefabObject.desync;

                            UpdateObject(beatmapObject, ObjectContext.PARENT_CHAIN);
                        }
                        break;
                    }
                case PrefabContext.REPEAT: {
                        UpdatePrefab(prefabObject);
                        break;
                    }
                case PrefabContext.HIDE: {
                        foreach (var expanded in prefabObject.expandedObjects)
                        {
                            if (expanded is BeatmapObject beatmapObject)
                            {
                                beatmapObject.editorData.hidden = prefabObject.editorData.hidden;
                                UpdateObject(beatmapObject, ObjectContext.HIDE);
                            }
                            if (expanded is BackgroundObject backgroundObject)
                            {
                                backgroundObject.editorData.hidden = prefabObject.editorData.hidden;
                                UpdateBackgroundObject(backgroundObject, BackgroundObjectContext.HIDE);
                            }
                        }

                        break;
                    }
                case PrefabContext.SELECTABLE: {
                        foreach (var expanded in prefabObject.expandedObjects)
                        {
                            if (expanded is BeatmapObject beatmapObject)
                            {
                                beatmapObject.editorData.selectable = prefabObject.editorData.selectable;
                                UpdateObject(beatmapObject, ObjectContext.SELECTABLE);
                            }
                        }
                        break;
                    }
            }
        }

        /// <summary>
        /// Applies all Beatmap Objects stored in the Prefab Object's Prefab to the level.
        /// </summary>
        /// <param name="prefabObject">Prefab Object to add to the level</param>
        /// <param name="update">If the object should be updated.</param>
        /// <param name="recalculate">If the engine should be recalculated.</param>
        public void AddPrefabToLevel(PrefabObject prefabObject, bool update = true, bool recalculate = true)
        {
            var prefab = prefabObject.GetPrefab();
            if (!prefab)
            {
                GameData.Current.prefabObjects.RemoveAll(x => x.prefabID == prefabObject.prefabID);
                return;
            }

            if (prefab.beatmapObjects.IsEmpty() && prefab.backgroundObjects.IsEmpty())
                return;

            float t = 1f;

            if (prefabObject.RepeatOffsetTime != 0f)
                t = prefabObject.RepeatOffsetTime;

            float timeToAdd = 0f;

            if (prefabObject.expandedObjects == null)
                prefabObject.expandedObjects = new List<IPrefabable>();
            prefabObject.expandedObjects.Clear();
            var notParented = new List<BeatmapObject>();
            for (int i = 0; i < prefabObject.RepeatCount + 1; i++)
            {
                var objectIDs = new List<IDPair>();
                for (int j = 0; j < prefab.beatmapObjects.Count; j++)
                    objectIDs.Add(new IDPair(prefab.beatmapObjects[j].id));

                int num = 0;
                foreach (var beatmapObject in prefab.beatmapObjects)
                {
                    var beatmapObjectCopy = beatmapObject.Copy(false);
                    try
                    {
                        beatmapObjectCopy.id = objectIDs[num].newID;

                        if (!string.IsNullOrEmpty(beatmapObject.Parent) && objectIDs.TryFind(x => x.oldID == beatmapObject.Parent, out IDPair idPair))
                            beatmapObjectCopy.Parent = idPair.newID;
                        else if (!string.IsNullOrEmpty(beatmapObject.Parent) && GameData.Current.beatmapObjects.FindIndex(x => x.id == beatmapObject.Parent) == -1 && beatmapObject.Parent != "CAMERA_PARENT")
                            beatmapObjectCopy.Parent = "";
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"{className}Failed to set object ID.\n{ex}");
                    }

                    beatmapObjectCopy.fromPrefabBase = string.IsNullOrEmpty(beatmapObjectCopy.Parent);
                    if (beatmapObjectCopy.fromPrefabBase && !string.IsNullOrEmpty(prefabObject.parent))
                    {
                        beatmapObjectCopy.Parent = prefabObject.parent;
                        beatmapObjectCopy.parentType = prefabObject.parentType;
                        beatmapObjectCopy.parentOffsets = prefabObject.parentOffsets;
                        beatmapObjectCopy.parentAdditive = prefabObject.parentAdditive;
                        beatmapObjectCopy.parallaxSettings = prefabObject.parentParallax;
                        beatmapObjectCopy.desync = prefabObject.desync;
                    }

                    beatmapObjectCopy.fromPrefab = true;
                    beatmapObjectCopy.SetPrefabReference(prefabObject);

                    beatmapObjectCopy.StartTime = prefabObject.StartTime + prefab.offset + ((beatmapObjectCopy.StartTime + timeToAdd) / prefabObject.Speed);

                    try
                    {
                        if (beatmapObjectCopy.events.Count > 0 && prefabObject.Speed != 1f)
                            for (int j = 0; j < beatmapObjectCopy.events.Count; j++)
                                beatmapObjectCopy.events[j].ForEach(x => x.time /= prefabObject.Speed);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"{className}Failed to set event speed.\n{ex}");
                    }

                    if (prefabObject.autoKillType != PrefabAutoKillType.Regular && prefabObject.StartTime + prefab.offset + beatmapObjectCopy.SpawnDuration > prefabObject.autoKillOffset)
                    {
                        beatmapObjectCopy.autoKillType = AutoKillType.SongTime;
                        beatmapObjectCopy.autoKillOffset = prefabObject.autoKillType == PrefabAutoKillType.StartTimeOffset ? prefabObject.StartTime + prefab.offset + prefabObject.autoKillOffset : prefabObject.autoKillOffset;
                    }

                    if (beatmapObjectCopy.shape == 6 && !string.IsNullOrEmpty(beatmapObjectCopy.text) && prefab.assets.sprites.TryFind(x => x.name == beatmapObjectCopy.text, out SpriteAsset spriteAsset))
                        GameData.Current.assets.sprites.Add(spriteAsset.Copy());

                    beatmapObjectCopy.editorData.hidden = prefabObject.editorData.hidden;
                    beatmapObjectCopy.editorData.selectable = prefabObject.editorData.selectable;

                    beatmapObjectCopy.originalID = beatmapObject.id;
                    GameData.Current.beatmapObjects.Add(beatmapObjectCopy);
                    prefabObject.expandedObjects.Add(beatmapObjectCopy);

                    if (string.IsNullOrEmpty(beatmapObjectCopy.Parent) || beatmapObjectCopy.Parent == BeatmapObject.CAMERA_PARENT || GameData.Current.beatmapObjects.FindIndex(x => x.id == beatmapObject.Parent) != -1) // prevent updating of parented objects since updating is recursive.
                        notParented.Add(beatmapObjectCopy);

                    num++;
                }

                foreach (var backgroundObject in prefab.backgroundObjects)
                {
                    var backgroundObjectCopy = backgroundObject.Copy(false);

                    backgroundObjectCopy.fromPrefab = true;
                    backgroundObjectCopy.SetPrefabReference(prefabObject);

                    backgroundObjectCopy.StartTime = prefabObject.StartTime + prefab.offset + ((backgroundObjectCopy.StartTime + timeToAdd) / prefabObject.Speed);

                    if (prefabObject.autoKillType != PrefabAutoKillType.Regular && prefabObject.StartTime + prefab.offset + backgroundObjectCopy.SpawnDuration > prefabObject.autoKillOffset)
                    {
                        backgroundObjectCopy.autoKillType = AutoKillType.SongTime;
                        backgroundObjectCopy.autoKillOffset = prefabObject.autoKillType == PrefabAutoKillType.StartTimeOffset ? prefabObject.StartTime + prefab.offset + prefabObject.autoKillOffset : prefabObject.autoKillOffset;
                    }

                    if (backgroundObjectCopy.shape == 6 && !string.IsNullOrEmpty(backgroundObjectCopy.text) && prefab.assets.sprites.TryFind(x => x.name == backgroundObjectCopy.text, out SpriteAsset spriteAsset))
                        GameData.Current.assets.sprites.Add(spriteAsset.Copy());

                    backgroundObjectCopy.editorData.hidden = prefabObject.editorData.hidden;
                    backgroundObjectCopy.editorData.selectable = prefabObject.editorData.selectable;

                    backgroundObjectCopy.originalID = backgroundObject.id;
                    GameData.Current.backgroundObjects.Add(backgroundObjectCopy);
                    prefabObject.expandedObjects.Add(backgroundObjectCopy);
                }

                timeToAdd += t;
            }

            if (update)
            {
                foreach (var beatmapObject in notParented.Count > 0 ? notParented : prefabObject.expandedObjects.Where(x => x is BeatmapObject).Select(x => x as BeatmapObject))
                    UpdateObject(beatmapObject, recalculate: recalculate);
                foreach (var backgroundObject in prefabObject.expandedObjects.Where(x => x is BackgroundObject).Select(x => x as BackgroundObject))
                    UpdateBackgroundObject(backgroundObject, recalculate: recalculate);
            }

            notParented.Clear();
            notParented = null;
        }

        /// <summary>
        /// Library of values to update a <see cref="PrefabObject"/>.
        /// </summary>
        public static class PrefabContext
        {
            public const string TRANSFORM_OFFSET = "transformoffset";
            public const string TIME = "time";
            public const string SPEED = "speed";
            public const string AUTOKILL = "autokill";
            public const string PARENT = "parent";
            public const string REPEAT = "repeat";
            public const string HIDE = "hide";
            public const string SELECTABLE = "selectable";
        }

        #endregion

        #region Misc

        /// <summary>
        /// Sorts all the spawnable objects by start and kill time.
        /// </summary>
        public void Sort()
        {
            objectEngine?.spawner?.activateList?.Sort((a, b) => a.StartTime.CompareTo(b.StartTime));
            objectEngine?.spawner?.deactivateList?.Sort((a, b) => a.KillTime.CompareTo(b.KillTime));
            objectModifiersEngine?.spawner?.activateList?.Sort((a, b) => a.StartTime.CompareTo(b.StartTime));
            objectModifiersEngine?.spawner?.deactivateList?.Sort((a, b) => a.KillTime.CompareTo(b.KillTime));
            backgroundEngine?.spawner?.activateList?.Sort((a, b) => a.StartTime.CompareTo(b.StartTime));
            backgroundEngine?.spawner?.deactivateList?.Sort((a, b) => a.KillTime.CompareTo(b.KillTime));
            bgModifiersEngine?.spawner?.activateList?.Sort((a, b) => a.StartTime.CompareTo(b.StartTime));
            bgModifiersEngine?.spawner?.deactivateList?.Sort((a, b) => a.KillTime.CompareTo(b.KillTime));
            RecalculateObjectStates();
        }

        /// <summary>
        /// Resets the transform offsets of all the objects.
        /// </summary>
        static void ResetOffsets()
        {
            if (!GameData.Current)
                return;

            foreach (var beatmapObject in GameData.Current.beatmapObjects)
            {
                beatmapObject.ResetOffsets();

                beatmapObject.customParent = null;
            }

            foreach (var backgroundObject in GameData.Current.backgroundObjects)
            {
                backgroundObject.positionOffset = Vector3.zero;
                backgroundObject.scaleOffset = Vector3.zero;
                backgroundObject.rotationOffset = Vector3.zero;
            }
        }

        #endregion
    }
}
