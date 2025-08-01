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
    /// <summary>
    /// Represents the base of a runtime.
    /// </summary>
    public abstract class RTLevelBase : Exists
    {
        #region Core

        public RTLevelBase() { }

        public float previousAudioTime;
        public float audioTimeVelocity;

        /// <summary>
        /// The current time the objects are interpolating to.
        /// </summary>
        public float CurrentTime { get; set; }

        /// <summary>
        /// Parent of the runtime.
        /// </summary>
        public abstract Transform Parent { get; }

        /// <summary>
        /// Fixed level time.
        /// </summary>
        public abstract float FixedTime { get; }

        /// <summary>
        /// Loads the runtime.
        /// </summary>
        public abstract void Load();

        /// <summary>
        /// Loads the runtime from a package.
        /// </summary>
        /// <param name="beatmap">Package to convert to runtime.</param>
        /// <param name="checkPrefab">If objects should be checked if they're from a prefab.</param>
        public virtual void Load(IBeatmap beatmap, bool checkPrefab = true)
        {
            converter = new ObjectConverter(this);
            var beatmapObjects = !checkPrefab ? beatmap.BeatmapObjects : beatmap.BeatmapObjects.Where(x => !x.FromPrefab).ToList();
            var prefabObjects = !checkPrefab ? beatmap.PrefabObjects : beatmap.PrefabObjects.Where(x => !x.FromPrefab).ToList();
            var backgroundLayers = !checkPrefab ? beatmap.BackgroundLayers : beatmap.BackgroundLayers.Where(x => !x.FromPrefab).ToList();
            var backgroundObjects = !checkPrefab ? beatmap.BackgroundObjects : beatmap.BackgroundObjects.Where(x => !x.FromPrefab);

            for (int i = 0; i < beatmapObjects.Count; i++)
                converter.CacheSequence(beatmapObjects[i]);

            IEnumerable<IRTObject> runtimePrefabObjects = converter.ToRuntimePrefabObjects(prefabObjects);

            this.prefabObjects = runtimePrefabObjects.ToList();
            prefabEngine = new ObjectEngine(PrefabObjects);

            IEnumerable<IRTObject> runtimePrefabModifiers = converter.ToRuntimePrefabModifiers(prefabObjects);

            prefabModifiers = runtimePrefabModifiers.ToList();
            prefabModifiersEngine = new ObjectEngine(PrefabModifiers);

            IEnumerable<IRTObject> runtimeObjects = converter.ToRuntimeObjects(beatmapObjects);

            objects = runtimeObjects.ToList();
            objectEngine = new ObjectEngine(Objects);

            IEnumerable<IRTObject> runtimeModifiers = converter.ToRuntimeModifiers(beatmapObjects);

            modifiers = runtimeModifiers.ToList();
            objectModifiersEngine = new ObjectEngine(Modifiers);

            IEnumerable<BackgroundLayerObject> backgroundLayerObjects = converter.ToBackgroundLayerObjects(backgroundLayers);
            this.backgroundLayers = backgroundLayerObjects.ToList();

            IEnumerable<IRTObject> runtimeBGObjects = converter.ToRuntimeBGObjects(backgroundObjects);

            bgObjects = runtimeBGObjects.ToList();
            backgroundEngine = new ObjectEngine(BGObjects);

            IEnumerable<IRTObject> runtimeBGModifiers = converter.ToRuntimeBGModifiers(backgroundObjects);

            bgModifiers = runtimeBGModifiers.ToList();
            bgModifiersEngine = new ObjectEngine(BGModifiers);
        }

        /// <summary>
        /// Queue of actions to run before the tick starts.
        /// </summary>
        public Queue<Action> preTick = new Queue<Action>();

        /// <summary>
        /// Queue of actions to run after the tick ends.
        /// </summary>
        public Queue<Action> postTick = new Queue<Action>();

        /// <summary>
        /// Ticks the runtime level.
        /// </summary>
        public virtual void Tick()
        {
            PreTick();

            try
            {
                OnObjectModifiersTick(); // modifiers update second
                OnBackgroundModifiersTick(); // bg modifiers update third
            }
            catch (Exception ex)
            {
                Debug.LogError($"Had an exception with modifier tick. Exception: {ex}");
            }

            OnBeatmapObjectsTick(); // objects update fourth
            OnBackgroundObjectsTick(); // bgs update fifth

            PostTick();
        }

        /// <summary>
        /// Runs pre-tick queue.
        /// </summary>
        public void PreTick()
        {
            while (preTick != null && !preTick.IsEmpty())
                preTick.Dequeue()?.Invoke();
        }

        /// <summary>
        /// Runs post-tick queue.
        /// </summary>
        public void PostTick()
        {
            while (postTick != null && !postTick.IsEmpty())
                postTick.Dequeue()?.Invoke();
        }

        /// <summary>
        /// Clears the runtime levels' data.
        /// </summary>
        public virtual void Clear()
        {
            var parent = Parent;
            if (parent)
                LSHelpers.DeleteChildren(parent);

            objectEngine = null;
            objectModifiersEngine = null;
            backgroundEngine = null;
            bgModifiersEngine = null;

            preTick.Clear();
            postTick.Clear();
        }

        /// <summary>
        /// Recalculate object states.
        /// </summary>
        public virtual void RecalculateObjectStates()
        {
            objectEngine?.spawner?.RecalculateObjectStates();
            objectModifiersEngine?.spawner?.RecalculateObjectStates();
            backgroundEngine?.spawner?.RecalculateObjectStates();
            bgModifiersEngine?.spawner?.RecalculateObjectStates();
            prefabEngine?.spawner?.RecalculateObjectStates();
            prefabModifiersEngine?.spawner?.RecalculateObjectStates();
        }

        #endregion

        #region Objects

        #region Sequences

        /// <summary>
        /// Updates every objects' animations.
        /// </summary>
        public virtual void RecacheAllSequences()
        {
            if (!converter || !GameData.Current)
                return;

            // currently this desyncs all animations... idk why
            for (int i = 0; i < GameData.Current.beatmapObjects.Count; i++)
                RecacheSequences(GameData.Current.beatmapObjects[i], recursive: false);
            for (int i = 0; i < GameData.Current.prefabObjects.Count; i++)
                UpdatePrefab(GameData.Current.prefabObjects[i], PrefabObjectContext.TRANSFORM_OFFSET);
        }

        /// <summary>
        /// Removes or updates an objects' sequences.
        /// </summary>
        /// <param name="beatmapObject">Object to update.</param>
        /// <param name="reinsert">If the sequence should be reinserted or not.</param>
        /// <param name="updateParents">If the LevelObjects' parents should be updated.</param>
        /// <param name="recursive">If the method should run recursively.</param>
        public virtual void RecacheSequences(BeatmapObject beatmapObject, bool reinsert = true, bool updateParents = true, bool recursive = true)
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

            var runtimeObject = beatmapObject.runtimeObject;

            if (!runtimeObject)
                return;

            if (runtimeObject.visualObject)
            {
                runtimeObject.visualObject.colorSequence = collection.ColorSequence;
                runtimeObject.visualObject.secondaryColorSequence = collection.SecondaryColorSequence;
            }

            if (updateParents)
                foreach (var parentObject in runtimeObject.parentObjects)
                {
                    var cachedSequences = parentObject.beatmapObject.cachedSequences;
                    if (cachedSequences)
                    {
                        parentObject.positionSequence = cachedSequences.PositionSequence;
                        parentObject.scaleSequence = cachedSequences.ScaleSequence;
                        parentObject.rotationSequence = cachedSequences.RotationSequence;
                    }
                }
        }

        /// <summary>
        /// Stops all homing keyframes and resets them to their base values.
        /// </summary>
        public virtual void UpdateHomingKeyframes()
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

        public virtual void OnBeatmapObjectsTick() => objectEngine?.Update(CurrentTime);

        /// <summary>
        /// Updates a Beatmap Object.
        /// </summary>
        /// <param name="beatmapObject">Beatmap Objec to update.</param>
        /// <param name="recache">If sequences should be recached.</param>
        /// <param name="update">If the object itself should be updated.</param>
        /// <param name="reinsert">If the runtime object should be reinserted.</param>
        /// <param name="recursive">If updating should be recursive.</param>
        /// <param name="recalculate">If the engine should recalculate.</param>
        public virtual void UpdateObject(BeatmapObject beatmapObject, bool recache = true, bool update = true, bool reinsert = true, bool recursive = true, bool recalculate = true)
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
        public virtual void UpdateObject(BeatmapObject beatmapObject, string context, bool sort = true)
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
                        if (beatmapObject.runtimeModifiers)
                        {
                            beatmapObject.runtimeModifiers.orderMatters = beatmapObject.orderModifiers;
                            beatmapObject.runtimeModifiers.StartTime = beatmapObject.ignoreLifespan ? 0f : beatmapObject.StartTime;
                            beatmapObject.runtimeModifiers.KillTime = beatmapObject.ignoreLifespan ? SoundManager.inst.MusicLength : beatmapObject.StartTime + beatmapObject.SpawnDuration;
                            beatmapObject.runtimeModifiers.SetActive(beatmapObject.ModifiersActive);
                        }

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

                                childLevelObject.CameraParent = beatmapParent.Parent == BeatmapObject.CAMERA_PARENT;

                                childLevelObject.PositionParent = beatmapParent.GetParentType(0);
                                childLevelObject.ScaleParent = beatmapParent.GetParentType(1);
                                childLevelObject.RotationParent = beatmapParent.GetParentType(2);

                                childLevelObject.PositionParentOffset = beatmapParent.parallaxSettings[0];
                                childLevelObject.ScaleParentOffset = beatmapParent.parallaxSettings[1];
                                childLevelObject.RotationParentOffset = beatmapParent.parallaxSettings[2];
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

                                childLevelObject.CameraParent = beatmapParent.Parent == BeatmapObject.CAMERA_PARENT;

                                childLevelObject.PositionParent = beatmapParent.GetParentType(0);
                                childLevelObject.ScaleParent = beatmapParent.GetParentType(1);
                                childLevelObject.RotationParent = beatmapParent.GetParentType(2);

                                childLevelObject.PositionParentOffset = beatmapParent.parallaxSettings[0];
                                childLevelObject.ScaleParentOffset = beatmapParent.parallaxSettings[1];
                                childLevelObject.RotationParentOffset = beatmapParent.parallaxSettings[2];
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

                        runtimeObject.Depth = beatmapObject.Depth;
                        if (runtimeObject.visualObject)
                            runtimeObject.visualObject.SetOrigin(new Vector3(beatmapObject.origin.x, beatmapObject.origin.y, beatmapObject.Depth * 0.1f));

                        break;
                    } // Origin & Depth
                case ObjectContext.SHAPE: {
                        UpdateVisualObject(beatmapObject, runtimeObject);

                        break;
                    } // Shape
                case ObjectContext.POLYGONS: {
                        if (runtimeObject.visualObject is PolygonObject polygonObject)
                            polygonObject.UpdatePolygon(beatmapObject.polygonShape);
                        else
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
                        if (beatmapObject.runtimeModifiers)
                        {
                            beatmapObject.runtimeModifiers.orderMatters = beatmapObject.orderModifiers;
                            beatmapObject.runtimeModifiers.StartTime = beatmapObject.ignoreLifespan ? 0f : beatmapObject.StartTime;
                            beatmapObject.runtimeModifiers.KillTime = beatmapObject.ignoreLifespan ? SoundManager.inst.MusicLength : beatmapObject.StartTime + beatmapObject.SpawnDuration;
                            beatmapObject.runtimeModifiers.SetActive(beatmapObject.ModifiersActive);
                        }

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
                            runtimeModifiers.modifiers.ForLoop(modifier =>
                            {
                                modifier.Inactive?.Invoke(modifier, beatmapObject, null);
                                modifier.Result = null;
                            });

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
                        if (!beatmapObject.runtimeObject || beatmapObject.runtimeObject.parentObjects.IsEmpty() || !beatmapObject.runtimeObject.visualObject || !beatmapObject.runtimeObject.visualObject.gameObject)
                            break;

                        beatmapObject.runtimeObject.visualObject.gameObject.SetActive(!beatmapObject.editorData.hidden);

                        break;
                    }
            }
        }

        /// <summary>
        /// Removes a Beatmap Object from the runtime.
        /// </summary>
        /// <param name="beatmapObject">Beatmap Object to remove.</param>
        public void RemoveObject(BeatmapObject beatmapObject)
        {
            var runtimeObject = beatmapObject.runtimeObject;

            if (runtimeObject)
            {
                var top = runtimeObject.Parent;
                CoreHelper.Delete(top);
                runtimeObject.Parent = null;

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
                runtimeModifiers.modifiers.ForLoop(modifier =>
                {
                    modifier.Inactive?.Invoke(modifier, beatmapObject, null);
                    modifier.Result = null;
                });

                objectModifiersEngine?.spawner?.RemoveObject(runtimeModifiers, false);
                modifiers.Remove(runtimeModifiers);

                runtimeModifiers = null;
                beatmapObject.runtimeModifiers = null;
            }
        }

        /// <summary>
        /// Adds a Beatmap Object to the runtime.
        /// </summary>
        /// <param name="beatmapObject">Beatmap Object to add.</param>
        public void AddObject(BeatmapObject beatmapObject)
        {
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

        /// <summary>
        /// Removes and recreates the object if it still exists.
        /// </summary>
        /// <param name="beatmapObject">Beatmap Object to update.</param>
        /// <param name="objects">Runtime object list.</param>
        /// <param name="converter">Object converter.</param>
        /// <param name="spawner">Object spawner.</param>
        /// <param name="reinsert">If the object should be reinserted.</param>
        /// <param name="recursive">If the updating should be recursive.</param>
        public virtual void ReinitObject(BeatmapObject beatmapObject, bool reinsert = true, bool recursive = true)
        {
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
                    if (bm.Parent == beatmapObject.id)
                        ReinitObject(bm, reinsert, recursive);
                }
            }

            RemoveObject(beatmapObject);

            // If the object should be reinserted.
            if (reinsert)
                AddObject(beatmapObject);
        }

        public virtual void UpdateParentChain(BeatmapObject beatmapObject, RTBeatmapObject runtimeObject = null)
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
            baseObject.SetParent(runtimeObject.Parent);

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

            top.SetParent(runtimeObject.Parent);
            top.localScale = Vector3.one;

            if (lastParent)
                baseObject.SetParent(lastParent);

            runtimeObject.parentObjects = parentObjects;

            var pc = beatmapObject.GetParentChain();

            if (pc == null || pc.IsEmpty())
                return;

            var beatmapParent = pc[pc.Count - 1];

            runtimeObject.CameraParent = beatmapParent.Parent == BeatmapObject.CAMERA_PARENT;

            runtimeObject.PositionParent = beatmapParent.GetParentType(0);
            runtimeObject.ScaleParent = beatmapParent.GetParentType(1);
            runtimeObject.RotationParent = beatmapParent.GetParentType(2);

            runtimeObject.PositionParentOffset = beatmapParent.parallaxSettings[0];
            runtimeObject.ScaleParentOffset = beatmapParent.parallaxSettings[1];
            runtimeObject.RotationParentOffset = beatmapParent.parallaxSettings[2];
        }

        public virtual void UpdateVisualObject(BeatmapObject beatmapObject, RTBeatmapObject runtimeObject)
        {
            if (!runtimeObject)
                return;

            var parent = runtimeObject.parentObjects[0].transform?.parent;

            CoreHelper.Destroy(runtimeObject.parentObjects[0].transform.gameObject);
            runtimeObject.visualObject?.Clear();
            runtimeObject.visualObject = null;

            var shape = Mathf.Clamp(beatmapObject.Shape, 0, ObjectManager.inst.objectPrefabs.Count - 1);
            var shapeOption = Mathf.Clamp(beatmapObject.ShapeOption, 0, ObjectManager.inst.objectPrefabs[shape].options.Count - 1);
            var shapeType = (ShapeType)shape;

            GameObject baseObject = UnityObject.Instantiate(ObjectManager.inst.objectPrefabs[shape].options[shapeOption], parent ? parent.transform : Parent);

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
                ShapeType.Polygon => new PolygonObject(visualObject, opacity, hasCollider, isSolid, (int)beatmapObject.renderLayerType, beatmapObject.opacityCollision, (int)beatmapObject.gradientType, beatmapObject.gradientScale, beatmapObject.gradientRotation, beatmapObject.polygonShape),
                _ => new SolidObject(visualObject, opacity, hasCollider, isSolid, (int)beatmapObject.renderLayerType, beatmapObject.opacityCollision, (int)beatmapObject.gradientType, beatmapObject.gradientScale, beatmapObject.gradientRotation),
            };

            if (CoreHelper.InEditor)
            {
                visualObject.SetActive(!beatmapObject.editorData.hidden);

                if (beatmapObject.editorData.selectable)
                {
                    var obj = visualObject.AddComponent<SelectObject>();
                    obj.SetObject(beatmapObject);
                    beatmapObject.selector = obj;
                }
            }

            UnityObject.Destroy(visualObject.GetComponent<SelectObjectInEditor>());

            visual.colorSequence = beatmapObject.cachedSequences.ColorSequence;
            visual.secondaryColorSequence = beatmapObject.cachedSequences.SecondaryColorSequence;

            runtimeObject.visualObject = visual;
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

        public virtual void OnObjectModifiersTick()
        {
            if (GameData.Current && CoreHelper.Sequencing)
                objectModifiersEngine?.Update(FixedTime);
        }

        #endregion

        #endregion

        #region Background Objects

        /// <summary>
        /// Parents for Background Objects to use.
        /// </summary>
        public List<BackgroundLayerObject> backgroundLayers = new List<BackgroundLayerObject>();

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

        public virtual void OnBackgroundObjectsTick()
        {
            if (CoreConfig.Instance.ShowBackgroundObjects.Value && (CoreHelper.Playing || LevelManager.LevelEnded && ArcadeHelper.ReplayLevel))
                backgroundEngine?.Update(CurrentTime);
        }

        /// <summary>
        /// Updates all BackgroundLayers.
        /// </summary>
        public virtual void UpdateBackgroundLayers()
        {
            foreach (var backgroundLayer in GameData.Current.backgroundLayers)
                ReinitObject(backgroundLayer);
        }

        public virtual void ReinitObject(BackgroundLayer backgroundLayer, bool reinsert = true)
        {
            var runtimeObject = backgroundLayer.runtimeObject;

            if (runtimeObject)
            {
                runtimeObject.Clear();
                backgroundLayers.Remove(runtimeObject);
                backgroundLayer.runtimeObject = null;
                runtimeObject = null;
            }

            if (!reinsert)
                return;

            runtimeObject = converter.ToBackgroundLayerObject(backgroundLayer);
            if (runtimeObject != null)
                backgroundLayers.Add(runtimeObject);
        }

        /// <summary>
        /// Updates all BackgroundObjects.
        /// </summary>
        public virtual void UpdateBackgroundObjects()
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
        public virtual void UpdateBackgroundObject(BackgroundObject backgroundObject, bool reinsert = true, bool recalculate = true)
        {
            ReinitObject(backgroundObject, reinsert);

            if (recalculate)
                backgroundEngine?.spawner?.RecalculateObjectStates();
        }

        public virtual void UpdateBackgroundObject(BackgroundObject backgroundObject, string context, bool sort = true)
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
        /// Removes a Background Object from the runtime.
        /// </summary>
        /// <param name="backgroundObject">Background Object to remove.</param>
        public void RemoveBackgroundObject(BackgroundObject backgroundObject)
        {
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
        }

        /// <summary>
        /// Adds a Background Object to the runtime.
        /// </summary>
        /// <param name="backgroundObject">Background Object to add.</param>
        public void AddBackgroundObject(BackgroundObject backgroundObject)
        {
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
        /// Removes and recreates the object if it still exists.
        /// </summary>
        /// <param name="backgroundObject">Background Object to update.</param>
        /// <param name="reinsert">If the object should be reinserted.</param>
        public virtual void ReinitObject(BackgroundObject backgroundObject, bool reinsert = true)
        {
            backgroundObject.positionOffset = Vector3.zero;
            backgroundObject.scaleOffset = Vector3.zero;
            backgroundObject.rotationOffset = Vector3.zero;

            RemoveBackgroundObject(backgroundObject);

            if (reinsert)
                AddBackgroundObject(backgroundObject);
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

        public virtual void OnBackgroundModifiersTick()
        {
            if (CoreConfig.Instance.ShowBackgroundObjects.Value && CoreHelper.Sequencing)
                bgModifiersEngine?.Update(FixedTime);
        }

        #endregion

        #endregion

        #region Prefabs

        /// <summary>
        /// Prefab Objects time engine. Handles Prefab object spawning and interpolation.
        /// </summary>
        public ObjectEngine prefabEngine;

        /// <summary>
        /// Readonly collection of runtime Prefab objects.
        /// </summary>
        public IReadOnlyList<IRTObject> PrefabObjects => prefabObjects?.AsReadOnly();

        /// <summary>
        /// List of runtime Prefab objects.
        /// </summary>
        public List<IRTObject> prefabObjects;

        public virtual void OnPrefabObjectsTick() => prefabEngine?.Update(CurrentTime);

        /// <summary>
        /// Updates a Prefab Object.
        /// </summary>
        /// <param name="prefabObject">The Prefab Object to update.</param>
        /// <param name="reinsert">If the object should be updated or removed.</param>
        public void UpdatePrefab(PrefabObject prefabObject, bool reinsert = true, bool recalculate = true)
        {
            ReinitPrefab(prefabObject, reinsert);

            if (!recalculate)
                return;

            objectEngine?.spawner?.RecalculateObjectStates();
            objectModifiersEngine?.spawner?.RecalculateObjectStates();
            prefabEngine?.spawner?.RecalculateObjectStates();
            prefabModifiersEngine?.spawner?.RecalculateObjectStates();
        }

        /// <summary>
        /// Updates a singled out value of a Prefab Object.
        /// </summary>
        /// <param name="prefabObject">The Prefab Object to update.</param>
        /// <param name="context">The context to update.</param>
        public virtual void UpdatePrefab(PrefabObject prefabObject, string context, bool sort = true)
        {
            var runtimePrefabObject = prefabObject.runtimeObject;
            if (!runtimePrefabObject)
                UpdatePrefab(prefabObject);
            runtimePrefabObject = prefabObject.runtimeObject;

            string lower = context.ToLower().Replace(" ", "").Replace("_", "");
            switch (lower)
            {
                case PrefabObjectContext.TRANSFORM_OFFSET: {
                        prefabObject.cachedTransform = null;
                        var transform = prefabObject.GetTransformOffset();

                        runtimePrefabObject.Position = transform.position;
                        runtimePrefabObject.Scale = new Vector3(transform.scale.x, transform.scale.y, 1f);
                        runtimePrefabObject.Rotation = new Vector3(0f, 0f, transform.rotation);

                        break;
                    }
                case PrefabObjectContext.TIME: {
                        prefabEngine?.spawner?.RecalculateObjectStates();
                        prefabModifiersEngine?.spawner?.RecalculateObjectStates();
                        break;
                    }
                case PrefabObjectContext.AUTOKILL: {
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
                case PrefabObjectContext.PARENT: {
                        for (int i = 0; i < prefabObject.expandedObjects.Count; i++)
                        {
                            var beatmapObject = prefabObject.expandedObjects[i] as BeatmapObject;
                            if (!beatmapObject || !beatmapObject.fromPrefabBase)
                                continue;

                            beatmapObject.Parent = prefabObject.Parent;
                            beatmapObject.parentType = prefabObject.parentType;
                            beatmapObject.parentOffsets = prefabObject.parentOffsets;
                            beatmapObject.parentAdditive = prefabObject.parentAdditive;
                            beatmapObject.parallaxSettings = prefabObject.parentParallax;
                            beatmapObject.desync = prefabObject.desync;

                            UpdateObject(beatmapObject, ObjectContext.PARENT_CHAIN);
                        }
                        break;
                    }
                case PrefabObjectContext.REPEAT: {
                        UpdatePrefab(prefabObject);
                        break;
                    }
                case PrefabObjectContext.HIDE: {
                        runtimePrefabObject.UpdateActive();

                        break;
                    }
                case PrefabObjectContext.SELECTABLE: {
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
                case PrefabObjectContext.MODIFIERS: {
                        var runtimeModifiers = prefabObject.runtimeModifiers;

                        if (runtimeModifiers)
                        {
                            prefabModifiersEngine?.spawner?.RemoveObject(runtimeModifiers, false);
                            prefabModifiers.Remove(runtimeModifiers);

                            runtimeModifiers = null;
                            prefabObject.runtimeModifiers = null;
                        }

                        var iRuntimeModifiers = converter.ToIRuntimeModifiers(runtimePrefabObject.Prefab, prefabObject);
                        if (iRuntimeModifiers != null)
                        {
                            prefabModifiers.Add(iRuntimeModifiers);
                            prefabModifiersEngine?.spawner?.InsertObject(iRuntimeModifiers, false);
                        }

                        if (sort)
                            prefabModifiersEngine?.spawner?.RecalculateObjectStates();

                        break;
                    }
            }
        }

        /// <summary>
        /// Loads all Prefab Objects to runtime.
        /// </summary>
        public virtual IEnumerator IAddPrefabObjectsToLevel()
        {
            var gameData = GameData.Current;
            for (int i = 0; i < gameData.prefabObjects.Count; i++)
            {
                if (i != 0)
                    yield return null;
                UpdatePrefab(gameData.prefabObjects[i]);
            }
        }

        /// <summary>
        /// Removes a Prefab Object from the runtime.
        /// </summary>
        /// <param name="prefabObject">Prefab Object to remove.</param>
        public void RemovePrefab(PrefabObject prefabObject)
        {
            var runtimeObject = prefabObject.runtimeObject;

            if (runtimeObject)
            {
                var spawner = runtimeObject.Spawner;
                if (spawner)
                {
                    foreach (var beatmapObject in spawner.BeatmapObjects)
                        runtimeObject.UpdateObject(beatmapObject, recursive: false, reinsert: false, recalculate: false);

                    foreach (var backgroundObject in spawner.BackgroundObjects)
                        runtimeObject.UpdateBackgroundObject(backgroundObject, reinsert: false, recalculate: false);

                    foreach (var backgroundLayer in spawner.BackgroundLayers)
                        runtimeObject.ReinitObject(backgroundLayer, false);

                    foreach (var subPrefabObject in spawner.PrefabObjects)
                        runtimeObject.RemovePrefab(subPrefabObject);
                }

                prefabEngine?.spawner?.RemoveObject(runtimeObject, false);
                prefabObjects.Remove(runtimeObject);
                runtimeObject.Clear();

                runtimeObject = null;
                prefabObject.runtimeObject = null;
            }

            var runtimeModifiers = prefabObject.runtimeModifiers;

            if (runtimeModifiers)
            {
                prefabModifiersEngine?.spawner?.RemoveObject(runtimeModifiers, false);
                prefabModifiers.Remove(runtimeModifiers);

                runtimeModifiers = null;
                prefabObject.runtimeModifiers = null;
            }
        }

        /// <summary>
        /// Adds a Prefab Object to the runtime.
        /// </summary>
        /// <param name="prefabObject">Prefab Object to add.</param>
        public void AddPrefab(PrefabObject prefabObject)
        {
            var prefab = prefabObject.GetPrefab();
            if (!prefab)
                return;

            var iRuntimePrefabObject = converter.ToIRuntimePrefabObject(prefab, prefabObject);
            if (iRuntimePrefabObject != null)
            {
                prefabObjects.Add(iRuntimePrefabObject);
                prefabEngine?.spawner?.InsertObject(iRuntimePrefabObject, false);
            }

            var iRuntimePrefabModifiers = converter.ToIRuntimeModifiers(prefab, prefabObject);
            if (iRuntimePrefabModifiers != null)
            {
                prefabModifiers.Add(iRuntimePrefabModifiers);
                prefabModifiersEngine?.spawner?.InsertObject(iRuntimePrefabModifiers, false);
            }
        }

        /// <summary>
        /// Removes and recreates the object if it still exists.
        /// </summary>
        /// <param name="prefabObject">Prefab Object to update.</param>
        /// <param name="reinsert">If the object should be reinserted.</param>
        public virtual void ReinitPrefab(PrefabObject prefabObject, bool reinsert = true)
        {
            prefabObject.positionOffset = Vector3.zero;
            prefabObject.scaleOffset = Vector3.zero;
            prefabObject.rotationOffset = Vector3.zero;

            RemovePrefab(prefabObject);

            GameData.Current.beatmapObjects.RemoveAll(x => x.PrefabInstanceID == prefabObject.id);
            GameData.Current.backgroundLayers.RemoveAll(x => x.PrefabInstanceID == prefabObject.id);
            GameData.Current.backgroundObjects.RemoveAll(x => x.PrefabInstanceID == prefabObject.id);
            GameData.Current.prefabObjects.RemoveAll(x => x.PrefabInstanceID == prefabObject.id);
            GameData.Current.prefabs.RemoveAll(x => x.PrefabInstanceID == prefabObject.id);

            if (reinsert)
                AddPrefab(prefabObject);
        }

        #region Modifiers

        /// <summary>
        /// Modifiers time engine. Handles active / inactive modifiers efficiently.
        /// </summary>
        public ObjectEngine prefabModifiersEngine;

        /// <summary>
        /// Readonly collection of runtime modifiers.
        /// </summary>
        public IReadOnlyList<IRTObject> PrefabModifiers => prefabModifiers?.AsReadOnly();

        /// <summary>
        /// List of runtime modifiers.
        /// </summary>
        public List<IRTObject> prefabModifiers;

        public virtual void OnPrefabModifiersTick()
        {
            if (CoreHelper.Sequencing)
                prefabModifiersEngine?.Update(FixedTime);
        }

        #endregion

        #endregion

        #region Misc

        /// <summary>
        /// Sorts all the spawnable objects by start and kill time.
        /// </summary>
        public virtual void Sort()
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
        public static void ResetOffsets()
        {
            if (!GameData.Current)
                return;

            foreach (var beatmapObject in GameData.Current.beatmapObjects)
            {
                beatmapObject.ResetOffsets();

                beatmapObject.customShape = -1;
                beatmapObject.customShapeOption = -1;
                beatmapObject.customParent = null;
            }

            foreach (var backgroundObject in GameData.Current.backgroundObjects)
            {
                backgroundObject.ResetOffsets();

                backgroundObject.customShape = -1;
                backgroundObject.customShapeOption = -1;
            }
        }

        #endregion
    }
}
