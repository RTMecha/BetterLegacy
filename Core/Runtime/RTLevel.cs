using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

using LSFunctions;

using BetterLegacy.Configs;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using BetterLegacy.Core.Runtime.Objects;
using BetterLegacy.Core.Runtime.Objects.Visual;
using BetterLegacy.Editor.Components;

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

        public RTLevel() { }

        public static string className = "[<color=#FF26C5>Updater</color>] \n";

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

        public bool Initialized => engine && converter;

        /// <summary>
        /// Initializes the runtime level.
        /// </summary>
        public static void Init()
        {
            Current?.Clear();
            Current = new RTLevel();
            Current.InternalInit();
        }

        void InternalInit()
        {
            Debug.Log($"{className}Loading level");

            previousAudioTime = 0.0f;
            audioTimeVelocity = 0.0f;

            // Sets a new seed or uses the current one.
            RandomHelper.UpdateSeed();

            var gameData = GameData.Current;

            // Removing and reinserting prefabs.
            gameData.beatmapObjects.RemoveAll(x => x.fromPrefab);
            for (int i = 0; i < GameData.Current.prefabObjects.Count; i++)
                AddPrefabToLevel(GameData.Current.prefabObjects[i], false);

            // Convert GameData to LevelObjects
            converter = new ObjectConverter(gameData);
            IEnumerable<IRTObject> runtimeObjects = converter.ToRuntimeObjects();

            objects = runtimeObjects.ToList();
            engine = new ObjectEngine(Objects);

            IEnumerable<IRTObject> runtimeModifiers = converter.ToRuntimeModifiers();

            modifiers = runtimeModifiers.ToList();
            objectModifiersEngine = new ObjectEngine(Modifiers);

            Debug.Log($"{className}Loaded {objects.Count} objects (original: {gameData.beatmapObjects.Count})");
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

            OnEventsTick(); // events need to update first
            OnBeatmapObjectsTick(); // objects update second
            OnObjectModifiersTick(); // modifiers update third
            OnBackgroundObjectsTick(); // bgs update fourth
            OnBackgroundModifiersTick(); // bg modifiers update fifth
        }

        /// <summary>
        /// Clears the runtime levels' data.
        /// </summary>
        public void Clear()
        {
            Debug.Log($"{className}Cleaning up level");

            engine = null;
            objectModifiersEngine = null;
        }

        #endregion

        #region Samples

        /// <summary>
        /// Samples of the current audio.
        /// </summary>
        public float[] samples = new float[256];

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

        void OnEventsTick()
        {
            Arcade.Managers.RTEventManager.OnLevelTick();
        }

        #endregion

        #region Objects

        #region Sequences

        /// <summary>
        /// Recalculate object states.
        /// </summary>
        public void RecalculateObjectStates() => engine?.objectSpawner?.RecalculateObjectStates();

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
                beatmapObject.cachedSequences = null;

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

        public ObjectEngine engine;
        public ObjectConverter converter;

        public IReadOnlyList<IRTObject> Objects => objects.AsReadOnly();

        public List<IRTObject> objects;

        void OnBeatmapObjectsTick()
        {
            if (!UseNewUpdateMethod)
            {
                var time = AudioManager.inst.CurrentAudioSource.time;
                CurrentTime = time;
                engine?.Update(time);
                return;
            }

            var currentAudioTime = AudioManager.inst.CurrentAudioSource.time;
            var smoothedTime = Mathf.SmoothDamp(previousAudioTime, currentAudioTime, ref audioTimeVelocity, 1.0f / 50.0f);
            CurrentTime = smoothedTime;
            engine?.Update(smoothedTime);
            previousAudioTime = smoothedTime;
        }

        /// <summary>
        /// Updates a Beatmap Object.
        /// </summary>
        /// <param name="beatmapObject">Beatmap Objec to update.</param>
        /// <param name="recache">If sequences should be recached.</param>
        /// <param name="update">If the object itself should be updated.</param>
        /// <param name="reinsert">If the runtime object should be reinserted.</param>
        public void UpdateObject(BeatmapObject beatmapObject, bool recache = true, bool update = true, bool reinsert = true, bool recursive = true, bool recalculate = true)
        {
            if (!engine)
                return;

            var objectSpawner = engine.objectSpawner;

            if (objects == null || !converter)
                return;

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
                objectSpawner.RecalculateObjectStates();
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
            var levelObject = beatmapObject.runtimeObject;

            switch (context)
            {
                case ObjectContext.RENDERING: {
                        if (!levelObject)
                            break;

                        if (levelObject.visualObject is SolidObject solidObject)
                            solidObject.UpdateRendering((int)beatmapObject.gradientType, (int)beatmapObject.renderLayerType, false, beatmapObject.gradientScale, beatmapObject.gradientRotation);
                        else
                            levelObject.visualObject.SetRenderType((int)beatmapObject.renderLayerType);

                        break;
                    } // Material
                case ObjectContext.OBJECT_TYPE: {
                        // object was empty
                        if (!levelObject)
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
                            if (levelObject.visualObject)
                                levelObject.visualObject.opacity = beatmapObject.objectType == BeatmapObject.ObjectType.Helper ? 0.35f : 1.0f;

                            if (levelObject.visualObject is SolidObject solidObject)
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
                        if (!levelObject)
                        {
                            if (sort)
                                Sort();
                            break;
                        }

                        if (!engine || !engine.objectSpawner)
                            break;

                        var spawner = engine.objectSpawner;

                        levelObject.StartTime = beatmapObject.StartTime;
                        levelObject.KillTime = beatmapObject.StartTime + beatmapObject.SpawnDuration;

                        if (sort)
                            Sort();

                        levelObject.SetActive(beatmapObject.Alive);

                        foreach (var levelParent in levelObject.parentObjects)
                            levelParent.timeOffset = levelParent.beatmapObject.StartTime;

                        break;
                    } // StartTime
                case ObjectContext.AUTOKILL: {
                        if (!levelObject || !engine || !engine.objectSpawner)
                            break;

                        levelObject.KillTime = beatmapObject.StartTime + beatmapObject.SpawnDuration;

                        if (!sort)
                            break;

                        var spawner = engine.objectSpawner;

                        spawner.deactivateList.Sort((a, b) => a.KillTime.CompareTo(b.KillTime));
                        spawner.RecalculateObjectStates();

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

                        if (!levelObject)
                            break;

                        foreach (var levelParent in levelObject.parentObjects)
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
                        UpdateParentChain(beatmapObject, levelObject);
                        break;
                    }
                case ObjectContext.VISUAL_OFFSET: {
                        if (!levelObject)
                            break;

                        levelObject.depth = beatmapObject.Depth;
                        if (levelObject.visualObject)
                            levelObject.visualObject.SetOrigin(new Vector3(beatmapObject.origin.x, beatmapObject.origin.y, beatmapObject.Depth * 0.1f));

                        break;
                    } // Origin & Depth
                case ObjectContext.SHAPE: {
                        UpdateVisualObject(beatmapObject, levelObject);

                        break;
                    } // Shape
                case ObjectContext.POLYGONS: {
                        UpdateVisualObject(beatmapObject, levelObject);

                        break;
                    } // Polygons
                case ObjectContext.IMAGE: {
                        if (levelObject && levelObject.visualObject is ImageObject imageObject)
                            imageObject.UpdateImage(beatmapObject.text, GameData.Current.assets.GetSprite(beatmapObject.text));

                        break;
                    } // Image
                case ObjectContext.TEXT: {
                        if (levelObject && levelObject.visualObject is TextObject textObject)
                        {
                            textObject.text = beatmapObject.text;
                            textObject.SetText(textObject.text);
                        }

                        break;
                    } // Text
                case ObjectContext.KEYFRAMES: {
                        if (!levelObject)
                        {
                            RecacheSequences(beatmapObject);
                            break;
                        }

                        levelObject.KillTime = beatmapObject.StartTime + beatmapObject.SpawnDuration;
                        RecacheSequences(beatmapObject, true, true);

                        break;
                    } // Keyframes
                case ObjectContext.MODIFIERS: {
                        var runtimeModifiers = beatmapObject.runtimeModifiers;

                        if (runtimeModifiers)
                        {
                            objectModifiersEngine?.objectSpawner?.RemoveObject(runtimeModifiers, false);
                            modifiers.Remove(runtimeModifiers);

                            runtimeModifiers = null;
                        }

                        beatmapObject.runtimeModifiers = null;
                        beatmapObject.runtimeModifiers =
                            new RTModifiers<BeatmapObject>(
                                beatmapObject.modifiers, beatmapObject.orderModifiers,
                                beatmapObject.ignoreLifespan ? 0f : beatmapObject.StartTime,
                                beatmapObject.ignoreLifespan ? SoundManager.inst.MusicLength : beatmapObject.StartTime + beatmapObject.SpawnDuration
                            );

                        modifiers.Add(beatmapObject.runtimeModifiers);
                        objectModifiersEngine?.objectSpawner?.InsertObject(beatmapObject.runtimeModifiers, false);

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

                engine?.objectSpawner?.RemoveObject(runtimeObject, false);
                objects.Remove(runtimeObject);

                runtimeObject.parentObjects.Clear();

                runtimeObject = null;
                top = null;
                beatmapObject.runtimeObject = null;
            }

            var runtimeModifiers = beatmapObject.runtimeModifiers;

            if (runtimeModifiers)
            {
                objectModifiersEngine?.objectSpawner?.RemoveObject(runtimeModifiers, false);
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
                engine?.objectSpawner?.InsertObject(iRuntimeObject, false);
            }

            var iRuntimeModifiers = converter.ToIRuntimeModifiers(beatmapObject);
            if (iRuntimeModifiers != null)
            {
                modifiers.Add(iRuntimeModifiers);
                objectModifiersEngine?.objectSpawner?.InsertObject(iRuntimeModifiers, false);
            }
        }

        void UpdateParentChain(BeatmapObject beatmapObject, RTBeatmapObject levelObject = null)
        {
            string id = beatmapObject.id;
            var beatmapObjects = GameData.Current.beatmapObjects;
            for (int i = 0; i < beatmapObjects.Count; i++)
            {
                var bm = beatmapObjects[i];
                if (bm.Parent == id)
                    UpdateParentChain(bm, bm.runtimeObject);
            }

            if (!levelObject)
                return;

            var baseObject = levelObject.visualObject.gameObject.transform.parent;
            baseObject.SetParent(levelObject.top);

            for (int i = 1; i < levelObject.parentObjects.Count; i++)
                CoreHelper.Destroy(levelObject.parentObjects[i].gameObject);

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

            top.SetParent(levelObject.top);
            top.localScale = Vector3.one;

            if (lastParent)
                baseObject.SetParent(lastParent);

            levelObject.parentObjects = parentObjects;

            var pc = beatmapObject.GetParentChain();

            if (pc != null && !pc.IsEmpty())
            {
                var beatmapParent = pc[pc.Count - 1];

                levelObject.cameraParent = beatmapParent.Parent == BeatmapObject.CAMERA_PARENT;

                levelObject.positionParent = beatmapParent.GetParentType(0);
                levelObject.scaleParent = beatmapParent.GetParentType(1);
                levelObject.rotationParent = beatmapParent.GetParentType(2);

                levelObject.positionParentOffset = beatmapParent.parallaxSettings[0];
                levelObject.scaleParentOffset = beatmapParent.parallaxSettings[1];
                levelObject.rotationParentOffset = beatmapParent.parallaxSettings[2];
            }
        }

        void UpdateVisualObject(BeatmapObject beatmapObject, RTBeatmapObject levelObject)
        {
            if (!levelObject)
                return;

            var parent = levelObject.parentObjects[0].transform?.parent;

            CoreHelper.Destroy(levelObject.parentObjects[0].transform.gameObject);
            levelObject.visualObject?.Clear();
            levelObject.visualObject = null;

            var shape = Mathf.Clamp(beatmapObject.shape, 0, ObjectManager.inst.objectPrefabs.Count - 1);
            var shapeOption = Mathf.Clamp(beatmapObject.shapeOption, 0, ObjectManager.inst.objectPrefabs[shape].options.Count - 1);
            var shapeType = (ShapeType)shape;

            GameObject baseObject = UnityObject.Instantiate(ObjectManager.inst.objectPrefabs[shape].options[shapeOption], parent ? parent.transform : ObjectManager.inst.objectParent.transform);

            baseObject.transform.localScale = Vector3.one;

            var visualObject = baseObject.transform.GetChild(0).gameObject;
            if (beatmapObject.ShapeType != ShapeType.Text || !beatmapObject.autoTextAlign)
                visualObject.transform.localPosition = new Vector3(beatmapObject.origin.x, beatmapObject.origin.y, beatmapObject.Depth * 0.1f);
            visualObject.name = "Visual [ " + beatmapObject.name + " ]";

            levelObject.parentObjects[0] = converter.InitLevelParentObject(beatmapObject, baseObject);

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
                var obj = visualObject.AddComponent<SelectObject>();
                obj.SetObject(beatmapObject);
                beatmapObject.selector = obj;

                UnityObject.Destroy(visualObject.GetComponent<SelectObjectInEditor>());
            }

            visual.colorSequence = beatmapObject.cachedSequences.ColorSequence;
            visual.secondaryColorSequence = beatmapObject.cachedSequences.SecondaryColorSequence;

            levelObject.visualObject = visual;
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
        }

        #region Modifiers

        public ObjectEngine objectModifiersEngine;

        public IReadOnlyList<IRTObject> Modifiers => modifiers.AsReadOnly();

        public List<IRTObject> modifiers;

        void OnObjectModifiersTick()
        {
            if (!GameData.Current || !CoreHelper.Playing)
                return;

            objectModifiersEngine?.Update(CurrentTime);

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

        // todo: implement start and autokill values to BG objects
        public ObjectSpawner bgSpawner;

        void OnBackgroundObjectsTick()
        {
            var ldm = CoreConfig.Instance.LDM.Value;
            if (CoreConfig.Instance.ShowBackgroundObjects.Value && (CoreHelper.Playing || LevelManager.LevelEnded && ArcadeHelper.ReplayLevel) && BackgroundManager.inst?.backgroundParent?.gameObject)
            {
                // idk if there's a better solution for this
                sampleLow = samples.Skip(0).Take(56).Average((float a) => a) * 1000f;
                sampleMid = samples.Skip(56).Take(100).Average((float a) => a) * 3000f;
                sampleHigh = samples.Skip(156).Take(100).Average((float a) => a) * 6000f;

                var beatmapTheme = CoreHelper.CurrentBeatmapTheme;

                for (int bg = 0; bg < GameData.Current.backgroundObjects.Count; bg++)
                {
                    var backgroundObject = GameData.Current.backgroundObjects[bg];

                    var gameObject = backgroundObject.BaseObject;

                    if (backgroundObject.active && gameObject && gameObject.activeSelf != backgroundObject.Enabled)
                        gameObject.SetActive(backgroundObject.Enabled);

                    if (!backgroundObject.active || !backgroundObject.Enabled || !gameObject)
                        continue;

                    Color mainColor =
                        CoreHelper.ChangeColorHSV(beatmapTheme.GetBGColor(backgroundObject.color), backgroundObject.hue, backgroundObject.saturation, backgroundObject.value);

                    var reactive = backgroundObject.IsReactive;

                    if (reactive)
                        mainColor =
                            RTMath.Lerp(mainColor,
                                CoreHelper.ChangeColorHSV(
                                    beatmapTheme.GetBGColor(backgroundObject.reactiveCol),
                                    backgroundObject.hue,
                                    backgroundObject.saturation,
                                    backgroundObject.value),
                                GetSample(backgroundObject.reactiveColSample, backgroundObject.reactiveColIntensity));

                    mainColor.a = 1f;

                    var fadeColor =
                        CoreHelper.ChangeColorHSV(beatmapTheme.GetBGColor(backgroundObject.fadeColor), backgroundObject.fadeHue, backgroundObject.fadeSaturation, backgroundObject.fadeValue);

                    if (CoreHelper.ColorMatch(fadeColor, beatmapTheme.backgroundColor, 0.05f))
                        fadeColor = ThemeManager.inst.bgColorToLerp;
                    fadeColor.a = 1f;

                    int layer = backgroundObject.iterations - backgroundObject.depth;
                    if (ldm && backgroundObject.renderers.Count > 0)
                    {
                        backgroundObject.renderers[0].material.color = mainColor;
                        if (backgroundObject.renderers.Count > 1 && backgroundObject.renderers[1].gameObject.activeSelf)
                        {
                            for (int i = 1; i < backgroundObject.renderers.Count; i++)
                                backgroundObject.renderers[i].gameObject.SetActive(false);
                        }
                    }
                    else
                        backgroundObject.renderers.ForLoop((renderer, i) =>
                        {
                            if (i == 0)
                            {
                                renderer.material.color = mainColor;
                                return;
                            }

                            if (!renderer.gameObject.activeSelf)
                                renderer.gameObject.SetActive(true);

                            float t = 1f / layer * i;

                            renderer.material.color = Color.Lerp(Color.Lerp(mainColor, fadeColor, t), fadeColor, t);
                        });

                    if (!reactive)
                    {
                        backgroundObject.reactiveSize = Vector2.zero;

                        gameObject.transform.localPosition = new Vector3(backgroundObject.pos.x, backgroundObject.pos.y, 32f + backgroundObject.depth * 10f + backgroundObject.zposition) + backgroundObject.positionOffset;
                        gameObject.transform.localScale = new Vector3(backgroundObject.scale.x, backgroundObject.scale.y, backgroundObject.zscale) + backgroundObject.scaleOffset;
                        gameObject.transform.localRotation = Quaternion.Euler(new Vector3(backgroundObject.rotation.x, backgroundObject.rotation.y, backgroundObject.rot) + backgroundObject.rotationOffset);
                        continue;
                    }

                    backgroundObject.reactiveSize = backgroundObject.reactiveType switch
                    {
                        BackgroundObject.ReactiveType.Bass => new Vector2(sampleLow, sampleLow) * backgroundObject.reactiveScale,
                        BackgroundObject.ReactiveType.Mids => new Vector2(sampleMid, sampleMid) * backgroundObject.reactiveScale,
                        BackgroundObject.ReactiveType.Treble => new Vector2(sampleHigh, sampleHigh) * backgroundObject.reactiveScale,
                        BackgroundObject.ReactiveType.Custom => new Vector2(GetSample(backgroundObject.reactiveScaSamples.x, backgroundObject.reactiveScaIntensity.x), GetSample(backgroundObject.reactiveScaSamples.y, backgroundObject.reactiveScaIntensity.y)) * backgroundObject.reactiveScale,
                        _ => Vector2.zero,
                    };

                    if (backgroundObject.reactiveType != BackgroundObject.ReactiveType.Custom)
                    {
                        gameObject.transform.localPosition =
                            new Vector3(backgroundObject.pos.x,
                            backgroundObject.pos.y,
                            32f + backgroundObject.depth * 10f + backgroundObject.zposition) + backgroundObject.positionOffset;
                        gameObject.transform.localScale =
                            new Vector3(backgroundObject.scale.x, backgroundObject.scale.y, backgroundObject.zscale) +
                            new Vector3(backgroundObject.reactiveSize.x, backgroundObject.reactiveSize.y, 0f) + backgroundObject.scaleOffset;
                        gameObject.transform.localRotation =
                            Quaternion.Euler(new Vector3(backgroundObject.rotation.x, backgroundObject.rotation.y, backgroundObject.rot) + backgroundObject.rotationOffset);

                        continue;
                    }

                    float x = GetSample(backgroundObject.reactivePosSamples.x, backgroundObject.reactivePosIntensity.x);
                    float y = GetSample(backgroundObject.reactivePosSamples.y, backgroundObject.reactivePosIntensity.y);
                    float z = GetSample(backgroundObject.reactiveZSample, backgroundObject.reactiveZIntensity);

                    float rot = GetSample(backgroundObject.reactiveRotSample, backgroundObject.reactiveRotIntensity);

                    gameObject.transform.localPosition =
                        new Vector3(backgroundObject.pos.x + x,
                        backgroundObject.pos.y + y,
                        32f + backgroundObject.depth * 10f + z + backgroundObject.zposition) + backgroundObject.positionOffset;
                    gameObject.transform.localScale =
                        new Vector3(backgroundObject.scale.x, backgroundObject.scale.y, backgroundObject.zscale) +
                        new Vector3(backgroundObject.reactiveSize.x, backgroundObject.reactiveSize.y, 0f) + backgroundObject.scaleOffset;
                    gameObject.transform.localRotation = Quaternion.Euler(
                        new Vector3(backgroundObject.rotation.x, backgroundObject.rotation.y,
                        backgroundObject.rot + rot) + backgroundObject.rotationOffset);
                }
            }
        }

        #region Modifiers

        void OnBackgroundModifiersTick()
        {
            if (!CoreConfig.Instance.ShowBackgroundObjects.Value || !CoreHelper.Playing || !GameData.Current || GameData.Current.backgroundObjects == null)
                return;

            var list = GameData.Current.backgroundObjects;

            for (int i = 0; i < list.Count; i++)
            {
                var backgroundObject = list[i];

                if (backgroundObject.modifiers.Count <= 0)
                    continue;

                for (int j = 0; j < backgroundObject.modifiers.Count; j++)
                {
                    var modifiers = backgroundObject.modifiers[j];

                    if (backgroundObject.orderModifiers)
                        ModifiersHelper.RunModifiersLoop(modifiers);
                    else
                        ModifiersHelper.RunModifiersAll(modifiers);
                }
            }
        }

        /// <summary>
        /// Updates all BackgroundObjects.
        /// </summary>
        public void UpdateBackgroundObjects()
        {
            foreach (var backgroundObject in GameData.Current.backgroundObjects)
                CreateBackgroundObject(backgroundObject);
        }

        /// <summary>
        /// Creates the GameObjects for the BackgroundObject.
        /// </summary>
        /// <param name="backgroundObject">BG Object to create.</param>
        /// <returns>The generated Background Object.</returns>
        public GameObject CreateBackgroundObject(BackgroundObject backgroundObject)
        {
            if (!CoreConfig.Instance.ShowBackgroundObjects.Value || !backgroundObject.active)
                return null;

            DestroyBackgroundObject(backgroundObject);

            var gameObject = BackgroundManager.inst.backgroundPrefab.Duplicate(BackgroundManager.inst.backgroundParent, backgroundObject.name);
            gameObject.layer = 9;
            gameObject.transform.localPosition = new Vector3(backgroundObject.pos.x, backgroundObject.pos.y, 32f + backgroundObject.depth * 10f);
            gameObject.transform.localScale = new Vector3(backgroundObject.scale.x, backgroundObject.scale.y, backgroundObject.zscale);
            gameObject.transform.localRotation = Quaternion.Euler(new Vector3(backgroundObject.rotation.x, backgroundObject.rotation.y, backgroundObject.rot));

            var renderer = gameObject.GetComponent<Renderer>();
            renderer.material = LegacyResources.objectMaterial;

            CoreHelper.Destroy(gameObject.GetComponent<SelectBackgroundInEditor>());
            CoreHelper.Destroy(gameObject.GetComponent<BoxCollider>());

            backgroundObject.gameObjects.Clear();
            backgroundObject.transforms.Clear();
            backgroundObject.renderers.Clear();

            backgroundObject.gameObjects.Add(gameObject);
            backgroundObject.transforms.Add(gameObject.transform);
            backgroundObject.renderers.Add(renderer);

            if (backgroundObject.drawFade)
            {
                int depth = backgroundObject.iterations;

                for (int i = 1; i < depth - backgroundObject.depth; i++)
                {
                    var gameObject2 = BackgroundManager.inst.backgroundFadePrefab.Duplicate(gameObject.transform, $"{backgroundObject.name} Fade [{i}]");

                    gameObject2.transform.localPosition = new Vector3(0f, 0f, i);
                    gameObject2.transform.localScale = Vector3.one;
                    gameObject2.transform.localRotation = Quaternion.Euler(Vector3.zero);
                    gameObject2.layer = 9;

                    var renderer2 = gameObject2.GetComponent<Renderer>();
                    renderer2.material = LegacyResources.objectMaterial;

                    backgroundObject.gameObjects.Add(gameObject2);
                    backgroundObject.transforms.Add(gameObject2.transform);
                    backgroundObject.renderers.Add(renderer2);
                }
            }

            backgroundObject.UpdateShape();

            return gameObject;
        }

        /// <summary>
        /// Updates the GameObjects for the BackgroundObject.
        /// </summary>
        /// <param name="backgroundObject">BG Object to update.</param>
        public void UpdateBackgroundObject(BackgroundObject backgroundObject)
        {
            DestroyBackgroundObject(backgroundObject);
            CreateBackgroundObject(backgroundObject);
        }

        /// <summary>
        /// Destroys and clears the BackgroundObject.
        /// </summary>
        /// <param name="backgroundObject">BG Object to clear.</param>
        public void DestroyBackgroundObject(BackgroundObject backgroundObject)
        {
            var gameObject = backgroundObject.BaseObject;
            if (gameObject)
                CoreHelper.Destroy(gameObject);
            backgroundObject.gameObjects.Clear();
            backgroundObject.transforms.Clear();
            backgroundObject.renderers.Clear();
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

            gameData.beatmapObjects.RemoveAll(x => x.prefabInstanceID == prefabObject.id);

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
                        foreach (var beatmapObject in GameData.Current.beatmapObjects.FindAll(x => x.fromPrefab && x.prefabInstanceID == prefabObject.id))
                        {
                            if (!beatmapObject.runtimeObject || !beatmapObject.runtimeObject.visualObject || !beatmapObject.runtimeObject.top)
                                continue;

                            var top = beatmapObject.runtimeObject.top;

                            bool hasPosX = prefabObject.events.Count > 0 && prefabObject.events[0] != null && prefabObject.events[0].values.Length > 0;
                            bool hasPosY = prefabObject.events.Count > 0 && prefabObject.events[0] != null && prefabObject.events[0].values.Length > 1;

                            bool hasScaX = prefabObject.events.Count > 1 && prefabObject.events[1] != null && prefabObject.events[1].values.Length > 0;
                            bool hasScaY = prefabObject.events.Count > 1 && prefabObject.events[1] != null && prefabObject.events[1].values.Length > 1;

                            bool hasRot = prefabObject.events.Count > 2 && prefabObject.events[2] != null && prefabObject.events[2].values.Length > 0;

                            var pos = new Vector3(hasPosX ? prefabObject.events[0].values[0] : 0f, hasPosY ? prefabObject.events[0].values[1] : 0f, 0f);
                            var sca = new Vector3(hasScaX ? prefabObject.events[1].values[0] : 1f, hasScaY ? prefabObject.events[1].values[1] : 1f, 1f);
                            var rot = Quaternion.Euler(0f, 0f, hasRot ? prefabObject.events[2].values[0] : 0f);

                            try
                            {
                                if (prefabObject.events[0].random != 0)
                                    pos = RandomHelper.KeyframeRandomizer.RandomizeVector2Keyframe(prefabObject.events[0]);
                                if (prefabObject.events[1].random != 0)
                                    sca = RandomHelper.KeyframeRandomizer.RandomizeVector2Keyframe(prefabObject.events[1]);
                                if (prefabObject.events[2].random != 0)
                                    rot = Quaternion.Euler(0f, 0f, RandomHelper.KeyframeRandomizer.RandomizeFloatKeyframe(prefabObject.events[2]));
                            }
                            catch (Exception ex)
                            {
                                Debug.LogError($"{className}Prefab Randomization error.\n{ex}");
                            }

                            beatmapObject.runtimeObject.prefabOffsetPosition = pos;
                            beatmapObject.runtimeObject.prefabOffsetScale = sca.x != 0f && sca.y != 0f ? sca : Vector3.one;
                            beatmapObject.runtimeObject.prefabOffsetRotation = rot.eulerAngles;

                            if (!hasPosX)
                                Debug.LogError($"{className}PrefabObject does not have Postion X in its' eventValues.\nPossible causes:");
                            if (!hasPosY)
                                Debug.LogError($"{className}PrefabObject does not have Postion Y in its' eventValues.");
                            if (!hasScaX)
                                Debug.LogError($"{className}PrefabObject does not have Scale X in its' eventValues.");
                            if (!hasScaY)
                                Debug.LogError($"{className}PrefabObject does not have Scale Y in its' eventValues.");
                            if (!hasRot)
                                Debug.LogError($"{className}PrefabObject does not have Rotation in its' eventValues.");
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
                                if (prefab.beatmapObjects.TryFind(x => x.id == beatmapObject.originalID, out BeatmapObject original))
                                {
                                    beatmapObject.StartTime = prefabObject.StartTime + prefab.offset + ((original.StartTime + timeToAdd) / prefabObject.Speed);

                                    if (lower == PrefabContext.SPEED)
                                    {
                                        for (int j = 0; j < beatmapObject.events.Count; j++)
                                            for (int k = 0; k < beatmapObject.events[j].Count; k++)
                                                beatmapObject.events[i][k].time = original.events[i][k].time / prefabObject.Speed;

                                        UpdateObject(beatmapObject, ObjectContext.KEYFRAMES);
                                    }

                                    var levelObject = beatmapObject.runtimeObject;

                                    // Update Start Time
                                    if (levelObject)
                                    {
                                        levelObject.StartTime = beatmapObject.StartTime;
                                        levelObject.KillTime = beatmapObject.StartTime + beatmapObject.SpawnDuration;

                                        levelObject.SetActive(beatmapObject.Alive);

                                        for (int j = 0; j < levelObject.parentObjects.Count; j++)
                                        {
                                            var levelParent = levelObject.parentObjects[j];
                                            var parent = levelParent.beatmapObject;

                                            levelParent.timeOffset = parent.StartTime;
                                        }
                                    }
                                }
                            }

                            timeToAdd += t;
                        }

                        if (!sort || !Initialized)
                            break;

                        var spawner = engine.objectSpawner;

                        if (!spawner)
                            break;

                        spawner.activateList.Sort((a, b) => a.StartTime.CompareTo(b.StartTime));
                        spawner.deactivateList.Sort((a, b) => a.KillTime.CompareTo(b.KillTime));
                        spawner.RecalculateObjectStates();

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
                            var beatmapObject = prefabObject.expandedObjects[i];
                            if (!beatmapObject.fromPrefabBase)
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
            // Checks if prefab exists.
            bool prefabExists = GameData.Current.prefabs.FindIndex(x => x.id == prefabObject.prefabID) != -1;
            if (string.IsNullOrEmpty(prefabObject.prefabID) || !prefabExists)
            {
                GameData.Current.prefabObjects.RemoveAll(x => x.prefabID == prefabObject.prefabID);
                return;
            }

            float t = 1f;

            if (prefabObject.RepeatOffsetTime != 0f)
                t = prefabObject.RepeatOffsetTime;

            float timeToAdd = 0f;

            var prefab = prefabObject.GetPrefab();
            if (prefabObject.expandedObjects == null)
                prefabObject.expandedObjects = new List<BeatmapObject>();
            prefabObject.expandedObjects.Clear();
            var notParented = new List<BeatmapObject>();
            if (prefab.beatmapObjects.Count > 0)
                for (int i = 0; i < prefabObject.RepeatCount + 1; i++)
                {
                    var objectIDs = new List<IDPair>();
                    for (int j = 0; j < prefab.beatmapObjects.Count; j++)
                        objectIDs.Add(new IDPair(prefab.beatmapObjects[j].id));

                    string prefabObjectID = prefabObject.id;
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
                        beatmapObjectCopy.prefabInstanceID = prefabObjectID;

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

                        if (prefabObject.autoKillType != PrefabObject.AutoKillType.Regular && prefabObject.StartTime + prefab.offset + beatmapObjectCopy.SpawnDuration > prefabObject.autoKillOffset)
                        {
                            beatmapObjectCopy.autoKillType = AutoKillType.SongTime;
                            beatmapObjectCopy.autoKillOffset = prefabObject.autoKillType == PrefabObject.AutoKillType.StartTimeOffset ? prefabObject.StartTime + prefab.offset + prefabObject.autoKillOffset : prefabObject.autoKillOffset;
                        }

                        if (beatmapObjectCopy.shape == 6 && !string.IsNullOrEmpty(beatmapObjectCopy.text) && prefab.assets.sprites.TryFind(x => x.name == beatmapObjectCopy.text, out SpriteAsset spriteAsset))
                            GameData.Current.assets.sprites.Add(spriteAsset.Copy());

                        beatmapObjectCopy.prefabID = prefabObject.prefabID;

                        beatmapObjectCopy.originalID = beatmapObject.id;
                        GameData.Current.beatmapObjects.Add(beatmapObjectCopy);
                        prefabObject.expandedObjects.Add(beatmapObjectCopy);

                        if (string.IsNullOrEmpty(beatmapObjectCopy.Parent) || beatmapObjectCopy.Parent == BeatmapObject.CAMERA_PARENT || GameData.Current.beatmapObjects.FindIndex(x => x.id == beatmapObject.Parent) != -1) // prevent updating of parented objects since updating is recursive.
                            notParented.Add(beatmapObjectCopy);

                        num++;
                    }

                    timeToAdd += t;
                }

            if (update)
                foreach (var bm in notParented.Count > 0 ? notParented : prefabObject.expandedObjects)
                    UpdateObject(bm, recalculate: recalculate);

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
        }

        #endregion

        #region Misc

        /// <summary>
        /// Sorts all the spawnable objects by start and kill time.
        /// </summary>
        public void Sort()
        {
            if (!Initialized)
                return;

            var spawner = engine.objectSpawner;

            if (!spawner)
                return;

            spawner.activateList.Sort((a, b) => a.StartTime.CompareTo(b.StartTime));
            spawner.deactivateList.Sort((a, b) => a.KillTime.CompareTo(b.KillTime));
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
