﻿using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

using LSFunctions;

using BetterLegacy.Configs;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using BetterLegacy.Core.Optimization.Level;
using BetterLegacy.Core.Optimization.Objects;
using BetterLegacy.Core.Optimization.Objects.Visual;
using BetterLegacy.Editor.Components;

namespace BetterLegacy.Core.Optimization
{
    /// <summary>
    /// An extensive wrapper for updating Catalyst objects.
    /// </summary>
    public class Updater
    {
        public static string className = "[<color=#FF26C5>Updater</color>] \n";

        /// <summary>
        /// Level Processor reference.
        /// </summary>
        public static LevelProcessor levelProcessor;

        static float previousAudioTime;
        static float audioTimeVelocity;

        /// <summary>
        /// Checks if a <see cref="BeatmapObject"/> has a generated <see cref="LevelObject"/> and spits out said <see cref="LevelObject"/>.
        /// </summary>
        /// <param name="beatmapObject"><see cref="BeatmapObject"/> to get a LevelObject from.</param>
        /// <param name="levelObject"><see cref="LevelObject"/> result.</param>
        /// <returns>Returns true if the <see cref="BeatmapObject"/> has a generated <see cref="LevelObject"/>, otherwise returns false.</returns>
        public static bool TryGetObject(BeatmapObject beatmapObject, out LevelObject levelObject)
        {
            if (beatmapObject.levelObject)
            {
                levelObject = beatmapObject.levelObject;
                return true;
            }

            levelObject = null;
            return false;
        }

        #region Samples

        /// <summary>
        /// Samples of the current audio.
        /// </summary>
        public static float[] samples = new float[256];

        public static float sampleLow;
        public static float sampleMid;
        public static float sampleHigh;

        /// <summary>
        /// Gets a sample by a sample index and multiplies it by intensity.
        /// </summary>
        /// <param name="sample">Sample index to get.</param>
        /// <param name="intensity">Intensity to multiply the sample by.</param>
        /// <returns>Returns a sample.</returns>
        public static float GetSample(int sample, float intensity) => samples[Mathf.Clamp(sample, 0, samples.Length - 1)] * intensity;

        #endregion

        #region Start / End

        /// <summary>
        /// Initializes the level.
        /// </summary>
        public static void OnLevelStart()
        {
            Debug.Log($"{className}Loading level");

            previousAudioTime = 0.0f;
            audioTimeVelocity = 0.0f;

            // Sets a new seed or uses the current one.
            RandomHelper.UpdateSeed();

            // Removing and reinserting prefabs.
            GameData.Current.beatmapObjects.RemoveAll(x => x.fromPrefab);
            for (int i = 0; i < GameData.Current.prefabObjects.Count; i++)
                AddPrefabToLevel(GameData.Current.prefabObjects[i], false);
            
            levelProcessor = new LevelProcessor(GameData.Current);
        }

        /// <summary>
        /// Cleans up the level.
        /// </summary>
        public static void OnLevelEnd()
        {
            Debug.Log($"{className}Cleaning up level");

            levelProcessor?.Dispose();
            levelProcessor = null;
        }

        #endregion

        #region Tick Update

        /// <summary>
        /// If the smooth method should be used for time updating.
        /// </summary>
        public static bool UseNewUpdateMethod { get; set; }

        /// <summary>
        /// The current time the objects are interpolating to.
        /// </summary>
        public static float CurrentTime { get; set; }

        /// <summary>
        /// Updates animation system.
        /// </summary>
        public static void OnLevelTick()
        {
            AudioManager.inst.CurrentAudioSource.GetSpectrumData(samples, 0, FFTWindow.Rectangular);

            if (!UseNewUpdateMethod)
            {
                var time = AudioManager.inst.CurrentAudioSource.time;
                CurrentTime = time;
                levelProcessor?.Update(time);
                return;
            }

            var currentAudioTime = AudioManager.inst.CurrentAudioSource.time;
            var smoothedTime = Mathf.SmoothDamp(previousAudioTime, currentAudioTime, ref audioTimeVelocity, 1.0f / 50.0f);
            CurrentTime = smoothedTime;
            levelProcessor?.Update(smoothedTime);
            previousAudioTime = smoothedTime;
        }

        public static void OnBGTick()
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

        #endregion

        #region Initialization

        #region Sequences

        /// <summary>
        /// Recalculate object states.
        /// </summary>
        public static void RecalculateObjectStates() => levelProcessor?.engine?.objectSpawner?.RecalculateObjectStates();

        /// <summary>
        /// Sets the game's current seed and updates all animations accordingly.
        /// </summary>
        /// <param name="seed">The seed to set.</param>
        public static void InitSeed(int seed) => InitSeed(seed.ToString());

        /// <summary>
        /// Sets the game's current seed and updates all animations accordingly.
        /// </summary>
        /// <param name="seed">The seed to set.</param>
        public static void InitSeed(string seed)
        {
            RandomHelper.SetSeed(seed);
            RecacheAllSequences();
        }

        /// <summary>
        /// Updates the game's current seed and updates all animations accordingly.
        /// </summary>
        public static void InitSeed()
        {
            RandomHelper.UpdateSeed();
            RecacheAllSequences();
        }

        /// <summary>
        /// Updates every objects' animations.
        /// </summary>
        public static void RecacheAllSequences()
        {
            if (!levelProcessor || levelProcessor.converter == null || levelProcessor.converter.cachedSequences == null || !GameData.Current)
                return;

            var beatmapObjects = GameData.Current.beatmapObjects;
            for (int i = 0; i < beatmapObjects.Count; i++)
                UpdateCachedSequence(beatmapObjects[i]);
        }

        /// <summary>
        /// Updates objects' cached sequences without reinitialization.
        /// </summary>
        /// <param name="beatmapObject">Object to update.</param>
        public static void UpdateCachedSequence(BeatmapObject beatmapObject)
        {
            if (levelProcessor.converter.cachedSequences.TryGetValue(beatmapObject.id, out ObjectConverter.CachedSequences collection))
                levelProcessor.converter.UpdateCachedSequence(beatmapObject, collection);
        }

        /// <summary>
        /// Removes or updates an objects' sequences.
        /// </summary>
        /// <param name="beatmapObject">Object to update.</param>
        /// <param name="converter"><see cref="ObjectConverter"/> reference.</param>
        /// <param name="reinsert">If the sequence should be reinserted or not.</param>
        /// <param name="updateParents">If the LevelObjects' parents should be updated.</param>
        /// <param name="recursive">If the method should run recursively.</param>
        public static void RecacheSequences(BeatmapObject beatmapObject, ObjectConverter converter, bool reinsert = true, bool updateParents = true, bool recursive = true)
        {
            if (!reinsert)
            {
                converter.cachedSequences.Remove(beatmapObject.id);

                // Recursive recaching.
                if (recursive)
                {
                    var beatmapObjects = GameData.Current.beatmapObjects;
                    for (int i = 0; i < beatmapObjects.Count; i++)
                    {
                        var bm = beatmapObjects[i];
                        if (bm.Parent == beatmapObject.id)
                            RecacheSequences(bm, converter, reinsert, updateParents, recursive);
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
                        RecacheSequences(bm, converter, reinsert, updateParents, recursive);
                }
            }

            if (!TryGetObject(beatmapObject, out LevelObject levelObject))
                return;

            if (levelObject.visualObject)
            {
                levelObject.visualObject.colorSequence = collection.ColorSequence;
                levelObject.visualObject.secondaryColorSequence = collection.SecondaryColorSequence;
            }

            if (updateParents)
                foreach (var levelParent in levelObject.parentObjects)
                {
                    if (converter.cachedSequences.TryGetValue(levelParent.id, out ObjectConverter.CachedSequences cachedSequences))
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
        public static void UpdateHomingKeyframes()
        {
            foreach (var cachedSequence in levelProcessor.converter.cachedSequences.Values)
            {
                for (int i = 0; i < cachedSequence.PositionSequence.keyframes.Length; i++)
                    cachedSequence.PositionSequence.keyframes[i].Stop();
                for (int i = 0; i < cachedSequence.RotationSequence.keyframes.Length; i++)
                    cachedSequence.RotationSequence.keyframes[i].Stop();
                for (int i = 0; i < cachedSequence.ColorSequence.keyframes.Length; i++)
                    cachedSequence.ColorSequence.keyframes[i].Stop();
            }
        }

        #endregion

        #region Objects

        /// <summary>
        /// Updates a Beatmap Object.
        /// </summary>
        /// <param name="beatmapObject"></param>
        /// <param name="recache"></param>
        /// <param name="update"></param>
        /// <param name="reinsert"></param>
        public static void UpdateObject(BeatmapObject beatmapObject, bool recache = true, bool update = true, bool reinsert = true, bool recursive = true, bool recalculate = true)
        {
            if (!levelProcessor)
                return;

            var level = levelProcessor.level;
            var converter = levelProcessor.converter;
            var engine = levelProcessor.engine;
            var objectSpawner = engine.objectSpawner;

            if (level == null || converter == null)
                return;

            var objects = level.objects;

            if (!reinsert)
            {
                recache = true;
                update = true;
            }

            if (recache)
                RecacheSequences(beatmapObject, converter, reinsert, recursive: recursive);

            if (update)
                ReinitObject(beatmapObject, level, objects, converter, objectSpawner, reinsert, recursive);

            if (recalculate)
                objectSpawner.RecalculateObjectStates();
        }

        /// <summary>
        /// Updates a specific value.
        /// </summary>
        /// <param name="beatmapObject">The BeatmapObject to update.</param>
        /// <param name="context">The specific context to update under.</param>
        public static void UpdateObject(BeatmapObject beatmapObject, string context, bool sort = true)
        {
            context = context.ToLower().Replace(" ", "").Replace("_", "");
            var levelObject = beatmapObject.levelObject;

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

                        if (!levelProcessor || !levelProcessor.engine || levelProcessor.engine.objectSpawner == null)
                            break;

                        var spawner = levelProcessor.engine.objectSpawner;

                        levelObject.StartTime = beatmapObject.StartTime;
                        levelObject.KillTime = beatmapObject.StartTime + beatmapObject.SpawnDuration;

                        if (sort)
                            Sort();

                        levelObject.SetActive(beatmapObject.Alive);

                        foreach (var levelParent in levelObject.parentObjects)
                            levelParent.timeOffset = levelParent.BeatmapObject.StartTime;

                        break;
                    } // StartTime

                case ObjectContext.AUTOKILL: {
                        if (!levelObject || !levelProcessor || !levelProcessor.engine || levelProcessor.engine.objectSpawner == null)
                            break;

                        levelObject.KillTime = beatmapObject.StartTime + beatmapObject.SpawnDuration;

                        if (!sort)
                            break;

                        var spawner = levelProcessor.engine.objectSpawner;

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
                                if (TryGetObject(child, out LevelObject childLevelObject))
                                {
                                    childLevelObject.cameraParent = beatmapParent.Parent == BeatmapObject.CAMERA_PARENT;

                                    childLevelObject.positionParent = beatmapParent.GetParentType(0);
                                    childLevelObject.scaleParent = beatmapParent.GetParentType(1);
                                    childLevelObject.rotationParent = beatmapParent.GetParentType(2);

                                    childLevelObject.positionParentOffset = beatmapParent.parallaxSettings[0];
                                    childLevelObject.scaleParentOffset = beatmapParent.parallaxSettings[1];
                                    childLevelObject.rotationParentOffset = beatmapParent.parallaxSettings[2];
                                }
                            }
                        }
                        else
                        {
                            UpdateObject(beatmapObject);
                        }

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
                                if (TryGetObject(child, out LevelObject childLevelObject))
                                {
                                    childLevelObject.cameraParent = beatmapParent.Parent == BeatmapObject.CAMERA_PARENT;

                                    childLevelObject.positionParent = beatmapParent.GetParentType(0);
                                    childLevelObject.scaleParent = beatmapParent.GetParentType(1);
                                    childLevelObject.rotationParent = beatmapParent.GetParentType(2);

                                    childLevelObject.positionParentOffset = beatmapParent.parallaxSettings[0];
                                    childLevelObject.scaleParentOffset = beatmapParent.parallaxSettings[1];
                                    childLevelObject.rotationParentOffset = beatmapParent.parallaxSettings[2];
                                }
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
                            imageObject.UpdateImage(beatmapObject.text, AssetManager.SpriteAssets.TryGetValue(beatmapObject.text, out Sprite spriteAsset) ? spriteAsset : null);

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
                            RecacheSequences(beatmapObject, levelProcessor.converter);
                            break;
                        }

                        levelObject.KillTime = beatmapObject.StartTime + beatmapObject.SpawnDuration;
                        RecacheSequences(beatmapObject, levelProcessor.converter, true, true);

                        break;
                    } // Keyframes
            }
        }

        /// <summary>
        /// Removes and recreates the object if it still exists.
        /// </summary>
        /// <param name="baseBeatmapObject"></param>
        /// <param name="level"></param>
        /// <param name="objects"></param>
        /// <param name="converter"></param>
        /// <param name="spawner"></param>
        /// <param name="reinsert"></param>
        /// <returns></returns>
        public static void ReinitObject(BeatmapObject beatmapObject, LevelStorage level, List<ILevelObject> objects, ObjectConverter converter, ObjectSpawner spawner, bool reinsert = true, bool recursive = true)
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
                        ReinitObject(bm, level, objects, converter, spawner, reinsert, recursive);
                }
            }

            if (TryGetObject(beatmapObject, out LevelObject levelObject))
            {
                var top = levelObject.top;

                spawner.RemoveObject(levelObject, false);
                objects.Remove(levelObject);
                if (top)
                    Object.Destroy(top.gameObject);

                levelObject.parentObjects.Clear();
                converter.beatmapObjects.Remove(id);

                levelObject = null;
                top = null;
            }

            // If the object should be reinserted and it is not null then we reinsert the object.
            if (reinsert && beatmapObject)
            {
                // It's important that the beatmapObjects Dictionary has a reference to the object.
                converter.beatmapObjects[beatmapObject.id] = beatmapObject;

                // Convert object to ILevelObject.
                var ilevelObj = converter.ToILevelObject(beatmapObject);
                if (ilevelObj != null)
                {
                    level.objects.Add(ilevelObj);
                    spawner.InsertObject(ilevelObj, false);
                }
            }
        }

        /// <summary>
        /// Updates everything and reinitializes the engine.
        /// </summary>
        /// <param name="restart">If the engine should restart or not.</param>
        public static void UpdateObjects(bool restart = true)
        {
            // We check if LevelProcessor has been invoked and if the level should restart.
            if (levelProcessor == null && restart)
            {
                OnLevelStart();
                return;
            }

            // If it is not null then we continue.
            if (levelProcessor != null)
            {
                ResetOffsets();

                for (int i = 0; i < levelProcessor.level.objects.Count; i++)
                    levelProcessor.level.objects[i].Clear();

                levelProcessor.level.objects.Clear();
                levelProcessor.converter.beatmapObjects.Clear();

                // Delete all the "GameObjects" children.
                LSHelpers.DeleteChildren(GameObject.Find("GameObjects").transform);

                // End and restart.
                OnLevelEnd();
                if (restart)
                    OnLevelStart();
            }
        }

        static void UpdateParentChain(BeatmapObject beatmapObject, LevelObject levelObject = null)
        {
            string id = beatmapObject.id;
            var beatmapObjects = GameData.Current.beatmapObjects;
            for (int i = 0; i < beatmapObjects.Count; i++)
            {
                var bm = beatmapObjects[i];
                if (bm.Parent == id)
                    UpdateParentChain(bm, bm.levelObject);
            }

            if (!levelObject)
                return;

            var baseObject = levelObject.visualObject.gameObject.transform.parent;
            baseObject.SetParent(levelObject.top);

            for (int i = 1; i < levelObject.parentObjects.Count; i++)
                CoreHelper.Destroy(levelObject.parentObjects[i].gameObject);

            var parentObjects = new List<LevelParentObject>();

            if (!string.IsNullOrEmpty(beatmapObject.Parent) && levelProcessor.converter.beatmapObjects.TryGetValue(beatmapObject.Parent, out BeatmapObject beatmapObjectParent))
                levelProcessor.converter.InitParentChain(beatmapObjectParent, parentObjects);

            var lastParent = !parentObjects.IsEmpty() && parentObjects[0] && parentObjects[0].transform ?
                parentObjects[0].transform : null;

            var p = levelProcessor.converter.InitLevelParentObject(beatmapObject, baseObject.gameObject);
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

        static void UpdateVisualObject(BeatmapObject beatmapObject, LevelObject levelObject)
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

            GameObject baseObject = Object.Instantiate(ObjectManager.inst.objectPrefabs[shape].options[shapeOption], parent ? parent.transform : ObjectManager.inst.objectParent.transform);

            baseObject.transform.localScale = Vector3.one;

            var visualObject = baseObject.transform.GetChild(0).gameObject;
            if (beatmapObject.ShapeType != ShapeType.Text || !beatmapObject.autoTextAlign)
                visualObject.transform.localPosition = new Vector3(beatmapObject.origin.x, beatmapObject.origin.y, beatmapObject.Depth * 0.1f);
            visualObject.name = "Visual [ " + beatmapObject.name + " ]";

            levelObject.parentObjects[0] = levelProcessor.converter.InitLevelParentObject(beatmapObject, baseObject);

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
                ShapeType.Image => new ImageObject(visualObject, opacity, beatmapObject.text, (int)beatmapObject.renderLayerType, AssetManager.SpriteAssets.TryGetValue(beatmapObject.text, out Sprite spriteAsset) ? spriteAsset : null),
                ShapeType.Polygon => new PolygonObject(visualObject, opacity, hasCollider, isSolid, (int)beatmapObject.renderLayerType, beatmapObject.opacityCollision, (int)beatmapObject.gradientType, beatmapObject.gradientScale, beatmapObject.gradientRotation, beatmapObject.polygonShapeSettings),
                _ => new SolidObject(visualObject, opacity, hasCollider, isSolid, (int)beatmapObject.renderLayerType, beatmapObject.opacityCollision, (int)beatmapObject.gradientType, beatmapObject.gradientScale, beatmapObject.gradientRotation),
            };

            if (CoreHelper.InEditor)
            {
                var obj = visualObject.AddComponent<SelectObject>();
                obj.SetObject(beatmapObject);
                beatmapObject.selector = obj;

                Object.Destroy(visualObject.GetComponent<SelectObjectInEditor>());
            }

            var cachedSequence = levelProcessor.converter.cachedSequences[beatmapObject.id];

            visual.colorSequence = cachedSequence.ColorSequence;
            visual.secondaryColorSequence = cachedSequence.SecondaryColorSequence;

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
        }

        #endregion

        #region Prefabs

        /// <summary>
        /// Updates a Prefab Object.
        /// </summary>
        /// <param name="prefabObject">The Prefab Object to update.</param>
        /// <param name="reinsert">If the object should be updated or removed.</param>
        public static void UpdatePrefab(PrefabObject prefabObject, bool reinsert = true, bool recalculate = true)
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
        public static void UpdatePrefab(PrefabObject prefabObject, string context, bool sort = true)
        {
            string lower = context.ToLower().Replace(" ", "").Replace("_", "");
            switch (lower)
            {
                case PrefabContext.TRANSFORM_OFFSET: {
                        foreach (var beatmapObject in GameData.Current.beatmapObjects.FindAll(x => x.fromPrefab && x.prefabInstanceID == prefabObject.id))
                        {
                            if (beatmapObject.levelObject && beatmapObject.levelObject.visualObject != null && beatmapObject.levelObject.top)
                            {
                                var top = beatmapObject.levelObject.top;

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
                                catch (System.Exception ex)
                                {
                                    Debug.LogError($"{className}Prefab Randomization error.\n{ex}");
                                }

                                beatmapObject.levelObject.prefabOffsetPosition = pos;
                                beatmapObject.levelObject.prefabOffsetScale = sca.x != 0f && sca.y != 0f ? sca : Vector3.one;
                                beatmapObject.levelObject.prefabOffsetRotation = rot.eulerAngles;

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

                                    // Update Start Time
                                    if (TryGetObject(beatmapObject, out LevelObject levelObject))
                                    {
                                        levelObject.StartTime = beatmapObject.StartTime;
                                        levelObject.KillTime = beatmapObject.StartTime + beatmapObject.SpawnDuration;

                                        levelObject.SetActive(beatmapObject.Alive);

                                        for (int j = 0; j < levelObject.parentObjects.Count; j++)
                                        {
                                            var levelParent = levelObject.parentObjects[j];
                                            var parent = levelParent.BeatmapObject;

                                            levelParent.timeOffset = parent.StartTime;
                                        }
                                    }
                                }
                            }

                            timeToAdd += t;
                        }

                        if (!sort || !levelProcessor || !levelProcessor.engine || levelProcessor.engine.objectSpawner == null)
                            break;

                        var spawner = levelProcessor.engine.objectSpawner;

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
        /// <param name="basePrefabObject"></param>
        /// <param name="update"></param>
        public static void AddPrefabToLevel(PrefabObject prefabObject, bool update = true, bool recalculate = true)
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
                        catch (System.Exception ex)
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
                        catch (System.Exception ex)
                        {
                            Debug.LogError($"{className}Failed to set event speed.\n{ex}");
                        }

                        if (prefabObject.autoKillType != PrefabObject.AutoKillType.Regular && prefabObject.StartTime + prefab.offset + beatmapObjectCopy.SpawnDuration > prefabObject.autoKillOffset)
                        {
                            beatmapObjectCopy.autoKillType = BeatmapObject.AutoKillType.SongTime;
                            beatmapObjectCopy.autoKillOffset = prefabObject.autoKillType == PrefabObject.AutoKillType.StartTimeOffset ? prefabObject.StartTime + prefab.offset + prefabObject.autoKillOffset : prefabObject.autoKillOffset;
                        }

                        if (beatmapObjectCopy.shape == 6 && !string.IsNullOrEmpty(beatmapObjectCopy.text) && prefab.SpriteAssets.TryGetValue(beatmapObjectCopy.text, out Sprite sprite))
                            Managers.AssetManager.SpriteAssets[beatmapObjectCopy.text] = sprite;

                        beatmapObjectCopy.prefabID = prefabObject.prefabID;

                        beatmapObjectCopy.originalID = beatmapObject.id;
                        GameData.Current.beatmapObjects.Add(beatmapObjectCopy);
                        prefabObject.expandedObjects.Add(beatmapObjectCopy);
                        if (levelProcessor && levelProcessor.converter != null)
                            levelProcessor.converter.beatmapObjects[beatmapObjectCopy.id] = beatmapObjectCopy;

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

        #region Coroutines

        public static IEnumerator IUpdateObjects(bool restart, bool resetOffsets = false)
        {
            // We check if LevelProcessor has been invoked and if the level should restart.
            if (levelProcessor == null && restart)
            {
                OnLevelStart();
                yield break;
            }

            if (resetOffsets)
                ResetOffsets();

            if (levelProcessor != null)
            {
                for (int i = 0; i < levelProcessor.level.objects.Count; i++)
                    levelProcessor.level.objects[i].Clear();

                levelProcessor.level.objects.Clear();
                levelProcessor.converter.beatmapObjects.Clear();
            }

            // Delete all the "GameObjects" children.
            LSHelpers.DeleteChildren(GameObject.Find("GameObjects").transform);

            // End and restart.
            OnLevelEnd();
            if (restart)
                OnLevelStart();

            yield break;
        }

        #endregion

        #region Misc

        /// <summary>
        /// Sorts all the spawnable objects by start and kill time.
        /// </summary>
        public static void Sort()
        {
            if (!levelProcessor || !levelProcessor.engine || levelProcessor.engine.objectSpawner == null)
                return;

            var spawner = levelProcessor.engine.objectSpawner;

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

        /// <summary>
        /// Updates all BackgroundObjects.
        /// </summary>
        public static void UpdateBackgroundObjects()
        {
            foreach (var backgroundObject in GameData.Current.backgroundObjects)
                CreateBackgroundObject(backgroundObject);
        }

        /// <summary>
        /// Creates the GameObjects for the BackgroundObject.
        /// </summary>
        /// <param name="backgroundObject">BG Object to create.</param>
        /// <returns></returns>
        public static GameObject CreateBackgroundObject(BackgroundObject backgroundObject)
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

            Object.Destroy(gameObject.GetComponent<SelectBackgroundInEditor>());
            Object.Destroy(gameObject.GetComponent<BoxCollider>());

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
        public static void UpdateBackgroundObject(BackgroundObject backgroundObject)
        {
            DestroyBackgroundObject(backgroundObject);
            CreateBackgroundObject(backgroundObject);
        }

        /// <summary>
        /// Destroys and clears the BackgroundObject.
        /// </summary>
        /// <param name="backgroundObject">BG Object to clear.</param>
        public static void DestroyBackgroundObject(BackgroundObject backgroundObject)
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
    }
}