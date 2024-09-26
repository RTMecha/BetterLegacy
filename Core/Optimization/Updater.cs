﻿using BetterLegacy.Core.Data;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Optimization.Level;
using BetterLegacy.Core.Optimization.Objects;
using BetterLegacy.Patchers;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using BaseBeatmapObject = DataManager.GameData.BeatmapObject;
using BasePrefabObject = DataManager.GameData.PrefabObject;
using ObjectAutoKillType = DataManager.GameData.BeatmapObject.AutoKillType;

namespace BetterLegacy.Core.Optimization
{
    /// <summary>
    /// An extensive wrapper for updating Catalyst objects.
    /// </summary>
    public class Updater
    {
        public static string className = "[<color=#FF26C5>Updater</color>] \n";

        public static LevelProcessor levelProcessor;

        static float previousAudioTime;
        static float audioTimeVelocity;

        public static float[] samples = new float[256];

        public static bool Active => levelProcessor && levelProcessor.level;

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

        #region Start / End

        /// <summary>
        /// Initializes the level.
        /// </summary>
        public static void OnLevelStart()
        {
            Debug.Log($"{className}Loading level");

            previousAudioTime = 0.0f;
            audioTimeVelocity = 0.0f;

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

        public static bool UseNewUpdateMethod { get; set; }

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

        #endregion

        #region Initialization

        public static void RecacheSequences(BeatmapObject beatmapObject, ObjectConverter converter, bool reinsert = true, bool updateParents = true, bool recursive = true)
        {
            converter.cachedSequences.Remove(beatmapObject.id);

            if (!reinsert)
            {
                // Recursive recaching.
                if (recursive)
                {
                    var beatmapObjects = GameData.Current.beatmapObjects;
                    for (int i = 0; i < beatmapObjects.Count; i++)
                    {
                        var bm = beatmapObjects[i];
                        if (bm.parent == beatmapObject.id)
                            RecacheSequences(bm, converter, reinsert, updateParents, recursive);
                    }
                }

                return;
            }

            converter.CacheSequence(beatmapObject);

            // Recursive recaching.
            if (recursive)
            {
                var beatmapObjects = GameData.Current.beatmapObjects;
                for (int i = 0; i < beatmapObjects.Count; i++)
                {
                    var bm = beatmapObjects[i];
                    if (bm.parent == beatmapObject.id)
                        RecacheSequences(bm, converter, reinsert, updateParents, recursive);
                }
            }

            if (!TryGetObject(beatmapObject, out LevelObject levelObject))
                return;

            if (converter.cachedSequences.TryGetValue(beatmapObject.id, out ObjectConverter.CachedSequences colorSequences))
            {
                levelObject.colorSequence = colorSequences.ColorSequence;
                levelObject.secondaryColorSequence = colorSequences.SecondaryColorSequence;
            }

            if (updateParents)
                foreach (var levelParent in levelObject.parentObjects)
                {
                    if (converter.cachedSequences.TryGetValue(levelParent.id, out ObjectConverter.CachedSequences cachedSequences))
                    {
                        levelParent.position3DSequence = cachedSequences.Position3DSequence;
                        levelParent.scaleSequence = cachedSequences.ScaleSequence;
                        levelParent.rotationSequence = cachedSequences.RotationSequence;
                    }
                }
        }

        #region Objects

        public static List<BeatmapObject> noParentObjects;

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
        public static void UpdateObject(BeatmapObject beatmapObject, string context)
        {
            context = context.ToLower().Replace(" ", "").Replace("_", "");
            if (TryGetObject(beatmapObject, out LevelObject levelObject))
            {
                switch (context)
                {
                    case "gradienttype": // TODO: find a way to do this better
                    case "objecttype":
                        {
                            UpdateObject(beatmapObject);
                            break;
                        } // ObjectType
                    case "time":
                    case "starttime":
                        {
                            if (!levelProcessor || !levelProcessor.engine || levelProcessor.engine.objectSpawner == null)
                                break;

                            var spawner = levelProcessor.engine.objectSpawner;

                            levelObject.StartTime = beatmapObject.StartTime;
                            levelObject.KillTime = beatmapObject.StartTime + beatmapObject.GetObjectLifeLength(0.0f, true);

                            spawner.activateList.Sort((a, b) => a.StartTime.CompareTo(b.StartTime));
                            spawner.deactivateList.Sort((a, b) => a.KillTime.CompareTo(b.KillTime));
                            spawner.RecalculateObjectStates();

                            levelObject.SetActive(beatmapObject.Alive);

                            foreach (var levelParent in levelObject.parentObjects)
                                levelParent.timeOffset = levelParent.BeatmapObject.StartTime;

                            break;
                        } // StartTime
                    case "autokilltype":
                    case "autokilloffset":
                    case "autokill":
                        {
                            if (!levelProcessor || !levelProcessor.engine || levelProcessor.engine.objectSpawner == null)
                                break;

                            var spawner = levelProcessor.engine.objectSpawner;

                            levelObject.KillTime = beatmapObject.StartTime + beatmapObject.GetObjectLifeLength(0.0f, true);

                            spawner.deactivateList.Sort((a, b) => a.KillTime.CompareTo(b.KillTime));
                            spawner.RecalculateObjectStates();

                            break;
                        } // Autokill
                    case "parent":
                        {
                            var parentChain = beatmapObject.GetParentChain();
                            if (beatmapObject.parent == "CAMERA_PARENT" || parentChain.Count > 1 && parentChain[parentChain.Count - 1].parent == "CAMERA_PARENT")
                            {
                                var beatmapParent = parentChain.Count > 1 && parentChain[parentChain.Count - 1].parent == "CAMERA_PARENT" ? parentChain[parentChain.Count - 1] : beatmapObject;

                                var ids = new List<string>();
                                foreach (var child in beatmapParent.GetChildChain())
                                {
                                    ids.AddRange(child.Where(x => !ids.Contains(x.id)).Select(x => x.id));
                                }

                                foreach (var id in ids)
                                {
                                    var child = GameData.Current.beatmapObjects.Find(x => x.id == id);
                                    if (TryGetObject(child, out LevelObject childLevelObject))
                                    {
                                        childLevelObject.cameraParent = beatmapParent.parent == "CAMERA_PARENT";

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
                    case "parenttype":
                    case "parentoffset":
                        {
                            var parentChain = beatmapObject.GetParentChain();
                            if (beatmapObject.parent == "CAMERA_PARENT" || parentChain.Count > 1 && parentChain[parentChain.Count - 1].parent == "CAMERA_PARENT")
                            {
                                var beatmapParent = parentChain.Count > 1 && parentChain[parentChain.Count - 1].parent == "CAMERA_PARENT" ? parentChain[parentChain.Count - 1] : beatmapObject;

                                var ids = new List<string>();
                                foreach (var child in beatmapParent.GetChildChain())
                                {
                                    ids.AddRange(child.Where(x => !ids.Contains(x.id)).Select(x => x.id));
                                }

                                foreach (var id in ids)
                                {
                                    var child = GameData.Current.beatmapObjects.Find(x => x.id == id);
                                    if (TryGetObject(child, out LevelObject childLevelObject))
                                    {
                                        childLevelObject.cameraParent = beatmapParent.parent == "CAMERA_PARENT";

                                        childLevelObject.positionParent = beatmapParent.GetParentType(0);
                                        childLevelObject.scaleParent = beatmapParent.GetParentType(1);
                                        childLevelObject.rotationParent = beatmapParent.GetParentType(2);

                                        childLevelObject.positionParentOffset = beatmapParent.parallaxSettings[0];
                                        childLevelObject.scaleParentOffset = beatmapParent.parallaxSettings[1];
                                        childLevelObject.rotationParentOffset = beatmapParent.parallaxSettings[2];
                                    }
                                }
                            }

                            foreach (var levelParent in levelObject.parentObjects)
                            {
                                if (GameData.Current.beatmapObjects.TryFind(x => x.id == levelParent.id, out BeatmapObject parent))
                                {
                                    levelParent.parentAnimatePosition = parent.GetParentType(0);
                                    levelParent.parentAnimateScale = parent.GetParentType(1);
                                    levelParent.parentAnimateRotation = parent.GetParentType(2);

                                    levelParent.parentOffsetPosition = parent.getParentOffset(0);
                                    levelParent.parentOffsetScale = parent.getParentOffset(1);
                                    levelParent.parentOffsetRotation = parent.getParentOffset(2);
                                }
                            }

                            break;
                        }
                    case "origin":
                    case "depth":
                    case "renderdepth":
                    case "originoffset":
                        {
                            levelObject.depth = beatmapObject.depth;
                            if (levelObject.visualObject != null && levelObject.visualObject.GameObject)
                                levelObject.visualObject.GameObject.transform.localPosition = new Vector3(beatmapObject.origin.x, beatmapObject.origin.y, beatmapObject.depth * 0.1f);
                            break;
                        } // Origin & Depth
                    case "shape":
                        {
                            //if (beatmapObject.shape == 4 || beatmapObject.shape == 6 || beatmapObject.shape == 9)
                            UpdateObject(beatmapObject);

                            //else if (ShapeManager.GetShape(beatmapObject.shape, beatmapObject.shapeOption).mesh != null)
                            //    levelObject.visualObject.GameObject.GetComponent<MeshFilter>().mesh = ShapeManager.GetShape(beatmapObject.shape, beatmapObject.shapeOption).mesh;

                            break;
                        } // Shape
                    case "text":
                        {
                            if (levelObject.visualObject != null && levelObject.visualObject is Objects.Visual.TextObject)
                                (levelObject.visualObject as Objects.Visual.TextObject).textMeshPro.text = beatmapObject.text;
                            break;
                        } // Text
                    case "keyframe":
                    case "keyframes":
                        {
                            levelObject.KillTime = beatmapObject.StartTime + beatmapObject.GetObjectLifeLength(0.0f, true);
                            RecacheSequences(beatmapObject, levelProcessor.converter, true, true);

                            break;
                        } // Keyframes
                }
            }
            else if (context.ToLower() == "keyframe" || context.ToLower() == "keyframes")
            {
                RecacheSequences(beatmapObject, levelProcessor.converter);
            }
            else if (context == "starttime" || context == "time")
            {
                Sort();
            }
            else if (context == "objecttype")
            {
                UpdateObject(beatmapObject);
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
                    if (bm.parent == id)
                        ReinitObject(bm, level, objects, converter, spawner, reinsert, recursive);
                }
            }

            if (TryGetObject(beatmapObject, out LevelObject levelObject))
            {
                var top = levelObject.top;

                spawner.RemoveObject(levelObject, false);
                objects.Remove(levelObject);
                Object.Destroy(top.gameObject);
                levelObject.parentObjects.Clear();

                converter.beatmapObjects.Remove(id);

                levelObject = null;
                top = null;
            }

            // If the object should be reinserted and it is not null then we reinsert the object.
            if (reinsert && beatmapObject != null)
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
                    ((LevelObject)levelProcessor.level.objects[i]).Clear();

                levelProcessor.level.objects.Clear();
                levelProcessor.converter.beatmapObjects.Clear();

                // Delete all the "GameObjects" children.
                LSFunctions.LSHelpers.DeleteChildren(GameObject.Find("GameObjects").transform);

                // End and restart.
                OnLevelEnd();
                if (restart)
                    OnLevelStart();
            }
        }

        #endregion

        #region Prefabs

        /// <summary>
        /// The max speed of a prefab object.
        /// </summary>
        public static float MaxFastSpeed => 1000f;

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
                if (string.IsNullOrEmpty(beatmapObject.parent) && beatmapObject.prefabInstanceID == prefabObject.ID)
                    UpdateObject(beatmapObject, reinsert: false, recalculate: recalculate);
            }

            gameData.beatmapObjects.RemoveAll(x => x.prefabInstanceID == prefabObject.ID);

            if (reinsert)
                AddPrefabToLevel(prefabObject, recalculate: recalculate);
        }

        /// <summary>
        /// Updates a singled out value of a Prefab Object.
        /// </summary>
        /// <param name="prefabObject">The Prefab Object to update.</param>
        /// <param name="context">The context to update.</param>
        public static void UpdatePrefab(PrefabObject prefabObject, string context)
        {
            string lower = context.ToLower().Replace(" ", "").Replace("_", "");
            switch (lower)
            {
                case "offset":
                case "transformoffset":
                    {
                        foreach (var beatmapObject in GameData.Current.beatmapObjects.FindAll(x => x.fromPrefab && x.prefabInstanceID == prefabObject.ID))
                        {
                            if (beatmapObject.levelObject && beatmapObject.levelObject.visualObject != null && beatmapObject.levelObject.top)
                            {
                                var top = beatmapObject.levelObject.top;

                                bool hasPosX = prefabObject.events.Count > 0 && prefabObject.events[0] != null && prefabObject.events[0].eventValues.Length > 0;
                                bool hasPosY = prefabObject.events.Count > 0 && prefabObject.events[0] != null && prefabObject.events[0].eventValues.Length > 1;

                                bool hasScaX = prefabObject.events.Count > 1 && prefabObject.events[1] != null && prefabObject.events[1].eventValues.Length > 0;
                                bool hasScaY = prefabObject.events.Count > 1 && prefabObject.events[1] != null && prefabObject.events[1].eventValues.Length > 1;

                                bool hasRot = prefabObject.events.Count > 2 && prefabObject.events[2] != null && prefabObject.events[2].eventValues.Length > 0;

                                var pos = new Vector3(hasPosX ? prefabObject.events[0].eventValues[0] : 0f, hasPosY ? prefabObject.events[0].eventValues[1] : 0f, 0f);
                                var sca = new Vector3(hasScaX ? prefabObject.events[1].eventValues[0] : 1f, hasScaY ? prefabObject.events[1].eventValues[1] : 1f, 1f);
                                var rot = Quaternion.Euler(0f, 0f, hasRot ? prefabObject.events[2].eventValues[0] : 0f);

                                try
                                {
                                    if (prefabObject.events[0].random != 0)
                                        pos = ObjectManager.inst.RandomVector2Parser(prefabObject.events[0]);
                                    if (prefabObject.events[1].random != 0)
                                        sca = ObjectManager.inst.RandomVector2Parser(prefabObject.events[1]);
                                    if (prefabObject.events[2].random != 0)
                                        rot = Quaternion.Euler(0f, 0f, ObjectManager.inst.RandomFloatParser(prefabObject.events[2]));
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
                case "time":
                case "starttime":
                case "speed":
                    {
                        float t = 1f;

                        if (prefabObject.RepeatOffsetTime != 0f)
                            t = prefabObject.RepeatOffsetTime;

                        float timeToAdd = 0f;

                        var prefab = GameData.Current.prefabs.Find(x => x.ID == prefabObject.prefabID);

                        for (int i = 0; i < prefabObject.RepeatCount + 1; i++)
                        {
                            foreach (var beatmapObject in GameData.Current.beatmapObjects.FindAll(x => x.fromPrefab && x.prefabInstanceID == prefabObject.ID))
                            {
                                if (prefab.objects.TryFind(x => x.id == beatmapObject.originalID, out BaseBeatmapObject original))
                                {
                                    beatmapObject.StartTime = prefabObject.StartTime + prefab.Offset + ((original.StartTime + timeToAdd) / Mathf.Clamp(prefabObject.speed, 0.01f, MaxFastSpeed));

                                    if (lower == "speed")
                                    {
                                        for (int j = 0; j < beatmapObject.events.Count; j++)
                                        {
                                            for (int k = 0; k < beatmapObject.events[j].Count; k++)
                                            {
                                                beatmapObject.events[i][k].eventTime = original.events[i][k].eventTime / Mathf.Clamp(prefabObject.speed, 0.01f, MaxFastSpeed);
                                            }
                                        }

                                        UpdateObject(beatmapObject, "Keyframes");
                                    }

                                    // Update Start Time
                                    if (TryGetObject(beatmapObject, out LevelObject levelObject))
                                    {
                                        levelObject.StartTime = beatmapObject.StartTime;
                                        levelObject.KillTime = beatmapObject.StartTime + beatmapObject.GetObjectLifeLength(0.0f, true);

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

                        if (!levelProcessor || !levelProcessor.engine || levelProcessor.engine.objectSpawner == null)
                            return;

                        var spawner = levelProcessor.engine.objectSpawner;

                        spawner.activateList.Sort((a, b) => a.StartTime.CompareTo(b.StartTime));
                        spawner.deactivateList.Sort((a, b) => a.KillTime.CompareTo(b.KillTime));
                        spawner.RecalculateObjectStates();

                        break;
                    }
                case "autokill":
                    {
                        foreach (var beatmapObject in GameData.Current.beatmapObjects.Where(x => x.fromPrefab && x.prefabInstanceID == prefabObject.ID))
                        {
                            if (prefabObject.autoKillType != PrefabObject.AutoKillType.Regular && prefabObject.StartTime + prefabObject.Prefab?.Offset + beatmapObject.GetObjectLifeLength(_oldStyle: true) > prefabObject.autoKillOffset)
                            {
                                beatmapObject.autoKillType = ObjectAutoKillType.SongTime;
                                beatmapObject.autoKillOffset = prefabObject.autoKillType == PrefabObject.AutoKillType.StartTimeOffset ? prefabObject.StartTime + prefabObject.Prefab?.Offset ?? 0f + prefabObject.autoKillOffset : prefabObject.autoKillOffset;
                            }

                            if (prefabObject.autoKillType == PrefabObject.AutoKillType.Regular)
                            {
                                UpdatePrefab(prefabObject);
                            }

                            UpdateObject(beatmapObject, "Start Time");
                        }

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
            bool prefabExists = GameData.Current.prefabs.FindIndex(x => x.ID == prefabObject.prefabID) != -1;
            if (string.IsNullOrEmpty(prefabObject.prefabID) || !prefabExists)
            {
                GameData.Current.prefabObjects.RemoveAll(x => x.prefabID == prefabObject.prefabID);
                return;
            }

            float t = 1f;

            if (prefabObject.RepeatOffsetTime != 0f)
                t = prefabObject.RepeatOffsetTime;

            float timeToAdd = 0f;

            var prefab = GameData.Current.prefabs.Find(x => x.ID == prefabObject.prefabID);
            if (prefabObject.expandedObjects == null)
                prefabObject.expandedObjects = new List<BeatmapObject>();
            prefabObject.expandedObjects.Clear();
            var notParented = new List<BeatmapObject>();
            if (prefab.objects.Count > 0)
                for (int i = 0; i < prefabObject.RepeatCount + 1; i++)
                {
                    var objectIDs = new List<KeyValuePair<string, string>>();
                    for (int j = 0; j < prefab.objects.Count; j++)
                        objectIDs.Add(new KeyValuePair<string, string>(prefab.objects[j].id, LSFunctions.LSText.randomString(16)));

                    string prefabObjectID = prefabObject.ID;
                    int num = 0;
                    foreach (var beatmapObj in prefab.objects)
                    {
                        var beatmapObject = BeatmapObject.DeepCopy((BeatmapObject)beatmapObj, false);
                        try
                        {
                            beatmapObject.id = objectIDs[num].Value;

                            if (!string.IsNullOrEmpty(beatmapObj.parent) && objectIDs.TryFind(x => x.Key == beatmapObj.parent, out KeyValuePair<string, string> keyValuePair))
                                beatmapObject.parent = keyValuePair.Value;
                            else if (!string.IsNullOrEmpty(beatmapObj.parent) && GameData.Current.beatmapObjects.FindIndex(x => x.id == beatmapObj.parent) == -1)
                                beatmapObject.parent = "";
                        }
                        catch (System.Exception ex)
                        {
                            Debug.LogError($"{className}Failed to set object ID.\n{ex}");
                        }

                        if (string.IsNullOrEmpty(beatmapObject.parent) && !string.IsNullOrEmpty(prefabObject.parent))
                        {
                            beatmapObject.parent = prefabObject.parent;
                            beatmapObject.parentType = prefabObject.parentType;
                            beatmapObject.parentOffsets = prefabObject.parentOffsets.ToList();
                            beatmapObject.parentAdditive = prefabObject.parentAdditive;
                            beatmapObject.parallaxSettings = prefabObject.parentParallax;
                            beatmapObject.desync = prefabObject.desync;
                        }

                        beatmapObject.active = false;
                        beatmapObject.fromPrefab = true;
                        beatmapObject.prefabInstanceID = prefabObjectID;

                        beatmapObject.StartTime = prefabObject.StartTime + prefab.Offset + ((beatmapObject.StartTime + timeToAdd) / Mathf.Clamp(prefabObject.speed, 0.01f, MaxFastSpeed));

                        try
                        {
                            if (beatmapObject.events.Count > 0 && prefabObject.speed != 1f)
                                for (int j = 0; j < beatmapObject.events.Count; j++)
                                    beatmapObject.events[j].ForEach(x => x.eventTime /= Mathf.Clamp(prefabObject.speed, 0.01f, MaxFastSpeed));
                        }
                        catch (System.Exception ex)
                        {
                            Debug.LogError($"{className}Failed to set event speed.\n{ex}");
                        }

                        if (prefabObject.autoKillType != PrefabObject.AutoKillType.Regular && prefabObject.StartTime + prefab.Offset + beatmapObject.GetObjectLifeLength(_oldStyle: true) > prefabObject.autoKillOffset)
                        {
                            beatmapObject.autoKillType = ObjectAutoKillType.SongTime;
                            beatmapObject.autoKillOffset = prefabObject.autoKillType == PrefabObject.AutoKillType.StartTimeOffset ? prefabObject.StartTime + prefab.Offset + prefabObject.autoKillOffset : prefabObject.autoKillOffset;
                        }

                        if (beatmapObject.shape == 6 && !string.IsNullOrEmpty(beatmapObject.text) && prefab.SpriteAssets.TryGetValue(beatmapObject.text, out Sprite sprite))
                            Managers.AssetManager.SpriteAssets[beatmapObject.text] = sprite;

                        beatmapObject.prefabID = prefabObject.prefabID;

                        beatmapObject.originalID = beatmapObj.id;
                        GameData.Current.beatmapObjects.Add(beatmapObject);
                        prefabObject.expandedObjects.Add(beatmapObject);
                        if (levelProcessor && levelProcessor.converter != null)
                            levelProcessor.converter.beatmapObjects[beatmapObject.id] = beatmapObject;

                        if (string.IsNullOrEmpty(beatmapObject.parent)) // prevent updating of parented objects since updating is recursive.
                            notParented.Add(beatmapObject);

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
                    ((LevelObject)levelProcessor.level.objects[i]).Clear();

                levelProcessor.level.objects.Clear();
                levelProcessor.converter.beatmapObjects.Clear();
            }

            // Delete all the "GameObjects" children.
            LSFunctions.LSHelpers.DeleteChildren(GameObject.Find("GameObjects").transform);

            // End and restart.
            OnLevelEnd();
            if (restart)
                OnLevelStart();

            yield break;
        }

        #endregion

        #region Misc

        public static void Sort()
        {
            if (!levelProcessor || !levelProcessor.engine || levelProcessor.engine.objectSpawner == null)
                return;

            var spawner = levelProcessor.engine.objectSpawner;

            spawner.activateList.Sort((a, b) => a.StartTime.CompareTo(b.StartTime));
            spawner.deactivateList.Sort((a, b) => a.KillTime.CompareTo(b.KillTime));
            spawner.RecalculateObjectStates();
        }

        static void ResetOffsets()
        {
            if (!GameData.IsValid)
                return;

            foreach (var beatmapObject in GameData.Current.beatmapObjects)
            {
                beatmapObject.reactivePositionOffset = Vector3.zero;
                beatmapObject.reactiveScaleOffset = Vector3.zero;
                beatmapObject.reactiveRotationOffset = 0f;
                beatmapObject.positionOffset = Vector3.zero;
                beatmapObject.scaleOffset = Vector3.zero;
                beatmapObject.rotationOffset = Vector3.zero;
            }
        }

        /// <summary>
        /// Creates the GameObjects for the BackgroundObject.
        /// </summary>
        /// <param name="__instance"></param>
        /// <param name="__result"></param>
        /// <param name="__0"></param>
        /// <returns></returns>
        public static GameObject CreateBackgroundObject(BackgroundObject backgroundObject)
        {
            if (!backgroundObject.active)
                return null;

            float scaleZ = backgroundObject.zscale;
            int depth = backgroundObject.depth;

            var gameObject = BackgroundManager.inst.backgroundPrefab.Duplicate(BackgroundManager.inst.backgroundParent, backgroundObject.name);
            gameObject.layer = 9;
            gameObject.transform.localPosition = new Vector3(backgroundObject.pos.x, backgroundObject.pos.y, 32f + backgroundObject.layer * 10f);
            gameObject.transform.localScale = new Vector3(backgroundObject.scale.x, backgroundObject.scale.y, scaleZ);
            gameObject.transform.localRotation = Quaternion.Euler(new Vector3(backgroundObject.rotation.x, backgroundObject.rotation.y, backgroundObject.rot));

            Object.Destroy(gameObject.GetComponent<SelectBackgroundInEditor>());
            Object.Destroy(gameObject.GetComponent<BoxCollider>());
            BackgroundManager.inst.backgroundObjects.Add(gameObject);

            backgroundObject.gameObjects.Clear();
            backgroundObject.transforms.Clear();
            backgroundObject.renderers.Clear();

            backgroundObject.gameObjects.Add(gameObject);
            backgroundObject.transforms.Add(gameObject.transform);
            backgroundObject.renderers.Add(gameObject.GetComponent<Renderer>());

            if (backgroundObject.drawFade)
            {
                for (int i = 1; i < depth - backgroundObject.layer; i++)
                {
                    var gameObject2 = BackgroundManager.inst.backgroundFadePrefab.Duplicate(gameObject.transform, $"{backgroundObject.name} Fade [{i}]");

                    gameObject2.transform.localPosition = new Vector3(0f, 0f, i);
                    gameObject2.transform.localScale = Vector3.one;
                    gameObject2.transform.localRotation = Quaternion.Euler(Vector3.zero);
                    gameObject2.layer = 9;

                    backgroundObject.gameObjects.Add(gameObject2);
                    backgroundObject.transforms.Add(gameObject2.transform);
                    backgroundObject.renderers.Add(gameObject2.GetComponent<Renderer>());
                }
            }

            backgroundObject.SetShape(backgroundObject.shape.Type, backgroundObject.shape.Option);

            return gameObject;
        }

        public static void UpdateHomingKeyframes()
        {
            foreach (var cachedSequence in levelProcessor.converter.cachedSequences.Values)
            {
                for (int i = 0; i < cachedSequence.Position3DSequence.keyframes.Length; i++)
                    cachedSequence.Position3DSequence.keyframes[i].Stop();
                for (int i = 0; i < cachedSequence.RotationSequence.keyframes.Length; i++)
                    cachedSequence.RotationSequence.keyframes[i].Stop();
                for (int i = 0; i < cachedSequence.ColorSequence.keyframes.Length; i++)
                    cachedSequence.ColorSequence.keyframes[i].Stop();
            }
        }

        #endregion

        #endregion
    }
}