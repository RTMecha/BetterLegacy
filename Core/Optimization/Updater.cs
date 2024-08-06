using BetterLegacy.Core.Data;
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
    public class Updater
    {
        public static string className = "[<color=#FF26C5>Updater</color>] \n";

        public static LevelProcessor levelProcessor;

        static float previousAudioTime;
        static float audioTimeVelocity;

        public static float[] samples = new float[256];

        public static bool Active => levelProcessor && levelProcessor.level;

        public static bool HasObject(BaseBeatmapObject beatmapObject) => Active && (LevelObject)levelProcessor.level.objects.Find(x => x.ID == beatmapObject.id);

        public static bool TryGetObject(BaseBeatmapObject beatmapObject, out LevelObject levelObject)
        {
            if (beatmapObject is BeatmapObject && (beatmapObject as BeatmapObject).levelObject)
            {
                levelObject = (beatmapObject as BeatmapObject).levelObject;
                return true;
            }

            if (HasObject(beatmapObject))
            {
                levelObject = (LevelObject)levelProcessor.level.objects.Find(x => x.ID == beatmapObject.id);
                return true;
            }

            levelObject = null;
            return false;
        }

        /// <summary>
        /// Initializes the level.
        /// </summary>
        public static void OnLevelStart()
        {
            Debug.Log($"{className}Loading level");

            previousAudioTime = 0.0f;
            audioTimeVelocity = 0.0f;

            // Removing and reinserting prefabs.
            DataManager.inst.gameData.beatmapObjects.RemoveAll(x => x.fromPrefab);
            for (int i = 0; i < DataManager.inst.gameData.prefabObjects.Count; i++)
                AddPrefabToLevel(DataManager.inst.gameData.prefabObjects[i], false);

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

        /// <summary>
        /// Updates a Beatmap Object.
        /// </summary>
        /// <param name="beatmapObject"></param>
        /// <param name="recache"></param>
        /// <param name="update"></param>
        /// <param name="reinsert"></param>
        public static void UpdateProcessor(BaseBeatmapObject beatmapObject, bool recache = true, bool update = true, bool reinsert = true)
        {
            var lp = levelProcessor;
            if (lp != null)
            {
                var level = levelProcessor.level;
                var converter = levelProcessor.converter;
                var engine = levelProcessor.engine;
                var objectSpawner = engine.objectSpawner;

                if (level != null && converter != null)
                {
                    var objects = level.objects;

                    if (!reinsert)
                    {
                        recache = true;
                        update = true;
                    }

                    if (recache)
                        CoreHelper.StartCoroutine(RecacheSequences(beatmapObject, converter, reinsert));
                    if (update)
                        CoreHelper.StartCoroutine(UpdateObjects(beatmapObject, level, objects, converter, objectSpawner, reinsert));
                }
            }
        }

        public static void Sort()
        {
            if (!levelProcessor || !levelProcessor.engine || levelProcessor.engine.objectSpawner == null)
                return;

            var spawner = levelProcessor.engine.objectSpawner;

            spawner.activateList.Sort((a, b) => a.StartTime.CompareTo(b.StartTime));
            spawner.deactivateList.Sort((a, b) => a.KillTime.CompareTo(b.KillTime));
            spawner.RecalculateObjectStates();
        }

        /// <summary>
        /// Updates a specific value.
        /// </summary>
        /// <param name="beatmapObject">The BeatmapObject to update.</param>
        /// <param name="context">The specific context to update under.</param>
        public static void UpdateProcessor(BaseBeatmapObject beatmapObject, string context)
        {
            if (TryGetObject(beatmapObject, out LevelObject levelObject))
            {
                switch (context.ToLower().Replace(" ", "").Replace("_", ""))
                {
                    case "objecttype":
                        {
                            UpdateProcessor(beatmapObject);
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

                            levelObject.SetActive(beatmapObject.TimeWithinLifespan());

                            foreach (var levelParent in levelObject.parentObjects)
                                levelParent.TimeOffset = levelParent.BeatmapObject.StartTime;

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
                                    var child = DataManager.inst.gameData.beatmapObjects.Find(x => x.id == id);
                                    if (TryGetObject(child, out LevelObject childLevelObject))
                                    {
                                        childLevelObject.cameraParent = beatmapParent.parent == "CAMERA_PARENT";

                                        childLevelObject.positionParent = beatmapParent.GetParentType(0);
                                        childLevelObject.scaleParent = beatmapParent.GetParentType(1);
                                        childLevelObject.rotationParent = beatmapParent.GetParentType(2);

                                        childLevelObject.positionParentOffset = ((BeatmapObject)beatmapParent).parallaxSettings[0];
                                        childLevelObject.scaleParentOffset = ((BeatmapObject)beatmapParent).parallaxSettings[1];
                                        childLevelObject.rotationParentOffset = ((BeatmapObject)beatmapParent).parallaxSettings[2];
                                    }
                                }
                            }
                            else
                            {
                                UpdateProcessor(beatmapObject);
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
                                    var child = DataManager.inst.gameData.beatmapObjects.Find(x => x.id == id);
                                    if (TryGetObject(child, out LevelObject childLevelObject))
                                    {
                                        childLevelObject.cameraParent = beatmapParent.parent == "CAMERA_PARENT";

                                        childLevelObject.positionParent = beatmapParent.GetParentType(0);
                                        childLevelObject.scaleParent = beatmapParent.GetParentType(1);
                                        childLevelObject.rotationParent = beatmapParent.GetParentType(2);

                                        childLevelObject.positionParentOffset = ((BeatmapObject)beatmapParent).parallaxSettings[0];
                                        childLevelObject.scaleParentOffset = ((BeatmapObject)beatmapParent).parallaxSettings[1];
                                        childLevelObject.rotationParentOffset = ((BeatmapObject)beatmapParent).parallaxSettings[2];
                                    }
                                }
                            }

                            foreach (var levelParent in levelObject.parentObjects)
                            {
                                if (DataManager.inst.gameData.beatmapObjects.TryFind(x => x.id == levelParent.ID, out BaseBeatmapObject parent))
                                {
                                    levelParent.ParentAnimatePosition = parent.GetParentType(0);
                                    levelParent.ParentAnimateScale = parent.GetParentType(1);
                                    levelParent.ParentAnimateRotation = parent.GetParentType(2);

                                    levelParent.ParentOffsetPosition = parent.getParentOffset(0);
                                    levelParent.ParentOffsetScale = parent.getParentOffset(1);
                                    levelParent.ParentOffsetRotation = parent.getParentOffset(2);
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
                                UpdateProcessor(beatmapObject);

                            //else if (ShapeManager.GetShape(beatmapObject.shape, beatmapObject.shapeOption).mesh != null)
                            //    levelObject.visualObject.GameObject.GetComponent<MeshFilter>().mesh = ShapeManager.GetShape(beatmapObject.shape, beatmapObject.shapeOption).mesh;

                            break;
                        } // Shape
                    case "text":
                        {
                            if (levelObject.visualObject != null && levelObject.visualObject is Objects.Visual.TextObject)
                                (levelObject.visualObject as Objects.Visual.TextObject).TextMeshPro.text = beatmapObject.text;
                            break;
                        } // Text
                    case "keyframe":
                    case "keyframes":
                        {
                            levelObject.KillTime = beatmapObject.StartTime + beatmapObject.GetObjectLifeLength(0.0f, true);
                            CoreHelper.StartCoroutine(RecacheSequences(beatmapObject, levelProcessor.converter, true, true));

                            break;
                        } // Keyframes
                }
            }
            else if (context.ToLower() == "keyframe" || context.ToLower() == "keyframes")
            {
                CoreHelper.StartCoroutine(RecacheSequences(beatmapObject, levelProcessor.converter, true, true));
            }
            else if (context.ToLower() == "starttime" || context.ToLower() == "time")
            {
                if (levelProcessor && levelProcessor.engine && levelProcessor.engine.objectSpawner != null)
                {
                    var spawner = levelProcessor.engine.objectSpawner;

                    spawner.activateList.Sort((a, b) => a.StartTime.CompareTo(b.StartTime));
                    spawner.deactivateList.Sort((a, b) => a.KillTime.CompareTo(b.KillTime));
                    spawner.RecalculateObjectStates();
                }
            }
        }

        /// <summary>
        /// Updates a Prefab Object.
        /// </summary>
        /// <param name="prefabObject">The Prefab Object to update.</param>
        /// <param name="reinsert">If the object should be updated or removed.</param>
        public static void UpdatePrefab(BasePrefabObject prefabObject, bool reinsert = true)
        {
            DataManager.inst.gameData.beatmapObjects.Where(x => x.fromPrefab && x.prefabInstanceID == prefabObject.ID).ToList().ForEach(x => UpdateProcessor(x, reinsert: false));

            DataManager.inst.gameData.beatmapObjects.RemoveAll(x => x.prefabInstanceID == prefabObject.ID);

            if (reinsert)
                CoreHelper.StartCoroutine(IAddPrefabToLevel(prefabObject));
        }

        /// <summary>
        /// Updates a singled out value of a Prefab Object.
        /// </summary>
        /// <param name="prefabObject">The Prefab Object to update.</param>
        /// <param name="context">The context to update.</param>
        public static void UpdatePrefab(BasePrefabObject prefabObject, string context)
        {
            string lower = context.ToLower().Replace(" ", "").Replace("_", "");
            switch (lower)
            {
                case "offset":
                case "transformoffset":
                    {
                        foreach (var beatmapObject in DataManager.inst.gameData.beatmapObjects.Where(x => x.fromPrefab && x.prefabInstanceID == prefabObject.ID && x is BeatmapObject).Select(x => x as BeatmapObject))
                        {
                            if (beatmapObject.levelObject && beatmapObject.levelObject.visualObject != null && beatmapObject.levelObject.visualObject.Top)
                            {
                                var top = beatmapObject.levelObject.visualObject.Top;

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

                        var moddedPrefab = (PrefabObject)prefabObject;

                        if (prefabObject.RepeatOffsetTime != 0f)
                            t = prefabObject.RepeatOffsetTime;

                        float timeToAdd = 0f;

                        var prefab = DataManager.inst.gameData.prefabs.Find(x => x.ID == prefabObject.prefabID);

                        for (int i = 0; i < prefabObject.RepeatCount + 1; i++)
                        {
                            foreach (var beatmapObject in DataManager.inst.gameData.beatmapObjects.Where(x => x.fromPrefab && x.prefabInstanceID == prefabObject.ID))
                            {
                                if (prefab.objects.TryFind(x => x.id == ((BeatmapObject)beatmapObject).originalID, out BaseBeatmapObject original))
                                {
                                    beatmapObject.StartTime = prefabObject.StartTime + prefab.Offset + ((original.StartTime + timeToAdd) / Mathf.Clamp(moddedPrefab.speed, 0.01f, MaxFastSpeed));

                                    if (lower == "speed")
                                    {
                                        for (int j = 0; j < beatmapObject.events.Count; j++)
                                        {
                                            for (int k = 0; k < beatmapObject.events[j].Count; k++)
                                            {
                                                beatmapObject.events[i][k].eventTime = original.events[i][k].eventTime / Mathf.Clamp(moddedPrefab.speed, 0.01f, MaxFastSpeed);
                                            }
                                        }

                                        UpdateProcessor(beatmapObject, "Keyframes");
                                    }

                                    UpdateProcessor(beatmapObject, "Start Time");
                                }
                            }

                            timeToAdd += t;
                        }

                        break;
                    }
                case "autokill":
                    {
                        var pr = prefabObject as PrefabObject;

                        foreach (var beatmapObject in DataManager.inst.gameData.beatmapObjects.Where(x => x.fromPrefab && x.prefabInstanceID == prefabObject.ID))
                        {
                            if (pr.autoKillType != PrefabObject.AutoKillType.Regular && prefabObject.StartTime + pr.Prefab?.Offset + beatmapObject.GetObjectLifeLength(_oldStyle: true) > pr.autoKillOffset)
                            {
                                beatmapObject.autoKillType = ObjectAutoKillType.SongTime;
                                beatmapObject.autoKillOffset = pr.autoKillType == PrefabObject.AutoKillType.StartTimeOffset ? prefabObject.StartTime + pr.Prefab?.Offset ?? 0f + pr.autoKillOffset : pr.autoKillOffset;
                            }

                            if (pr.autoKillType == PrefabObject.AutoKillType.Regular)
                            {
                                UpdatePrefab(prefabObject);
                            }

                            UpdateProcessor(beatmapObject, "Start Time");
                        }

                        break;
                    }
            }
        }

        public static IEnumerator IUpdatePrefab(BasePrefabObject prefabObject, string context)
        {
            string lower = context.ToLower().Replace(" ", "").Replace("_", "");
            switch (lower)
            {
                case "offset":
                case "transformoffset":
                    {
                        foreach (var beatmapObject in DataManager.inst.gameData.beatmapObjects.Where(x => x.fromPrefab && x.prefabInstanceID == prefabObject.ID && x is BeatmapObject).Select(x => x as BeatmapObject))
                        {
                            if (beatmapObject.levelObject && beatmapObject.levelObject.visualObject != null && beatmapObject.levelObject.visualObject.Top)
                            {
                                var top = beatmapObject.levelObject.visualObject.Top;

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

                        var moddedPrefab = (PrefabObject)prefabObject;

                        if (prefabObject.RepeatOffsetTime != 0f)
                            t = prefabObject.RepeatOffsetTime;

                        float timeToAdd = 0f;

                        var prefab = DataManager.inst.gameData.prefabs.Find(x => x.ID == prefabObject.prefabID);

                        for (int i = 0; i < prefabObject.RepeatCount + 1; i++)
                        {
                            foreach (var beatmapObject in DataManager.inst.gameData.beatmapObjects.Where(x => x.fromPrefab && x.prefabInstanceID == prefabObject.ID))
                            {
                                if (prefab.objects.TryFind(x => x.id == ((BeatmapObject)beatmapObject).originalID, out BaseBeatmapObject original))
                                {
                                    beatmapObject.StartTime = prefabObject.StartTime + prefab.Offset + ((original.StartTime + timeToAdd) / Mathf.Clamp(moddedPrefab.speed, 0.01f, MaxFastSpeed));

                                    if (lower == "speed")
                                    {
                                        for (int j = 0; j < beatmapObject.events.Count; j++)
                                        {
                                            for (int k = 0; k < beatmapObject.events[j].Count; k++)
                                            {
                                                beatmapObject.events[i][k].eventTime = original.events[i][k].eventTime / Mathf.Clamp(moddedPrefab.speed, 0.01f, MaxFastSpeed);
                                            }
                                        }

                                        UpdateProcessor(beatmapObject, "Keyframes");
                                    }

                                    UpdateProcessor(beatmapObject, "Start Time");
                                }
                            }

                            timeToAdd += t;
                        }

                        break;
                    }
                case "autokill":
                    {
                        var pr = prefabObject as PrefabObject;

                        foreach (var beatmapObject in DataManager.inst.gameData.beatmapObjects.Where(x => x.fromPrefab && x.prefabInstanceID == prefabObject.ID))
                        {
                            if (pr.autoKillType != PrefabObject.AutoKillType.Regular && prefabObject.StartTime + pr.Prefab?.Offset + beatmapObject.GetObjectLifeLength(_oldStyle: true) > pr.autoKillOffset)
                            {
                                beatmapObject.autoKillType = ObjectAutoKillType.SongTime;
                                beatmapObject.autoKillOffset = pr.autoKillType == PrefabObject.AutoKillType.StartTimeOffset ? prefabObject.StartTime + pr.Prefab?.Offset ?? 0f + pr.autoKillOffset : pr.autoKillOffset;
                            }

                            if (pr.autoKillType == PrefabObject.AutoKillType.Regular)
                            {
                                UpdatePrefab(prefabObject);
                            }

                            UpdateProcessor(beatmapObject, "Start Time");
                        }

                        break;
                    }
            }

            yield break;
        }

        public static float MaxFastSpeed => 1000f;

        /// <summary>
        /// Applies all Beatmap Objects stored in the Prefab Object's Prefab to the level.
        /// </summary>
        /// <param name="basePrefabObject"></param>
        /// <param name="update"></param>
        public static void AddPrefabToLevel(BasePrefabObject basePrefabObject, bool update = true)
        {
            var prefabObject = (PrefabObject)basePrefabObject;

            // Checks if prefab exists.
            bool prefabExists = DataManager.inst.gameData.prefabs.FindIndex(x => x.ID == basePrefabObject.prefabID) != -1;
            if (string.IsNullOrEmpty(basePrefabObject.prefabID) || !prefabExists)
            {
                DataManager.inst.gameData.prefabObjects.RemoveAll(x => x.prefabID == basePrefabObject.prefabID);
                return;
            }

            float t = 1f;

            if (basePrefabObject.RepeatOffsetTime != 0f)
                t = basePrefabObject.RepeatOffsetTime;

            float timeToAdd = 0f;

            var prefab = (Prefab)DataManager.inst.gameData.prefabs.Find(x => x.ID == basePrefabObject.prefabID);

            var list = new List<BeatmapObject>();
            if (prefab.objects.Count > 0)
                for (int i = 0; i < basePrefabObject.RepeatCount + 1; i++)
                {
                    var ids = prefab.objects.ToDictionary(x => x.id, x => LSFunctions.LSText.randomString(16));

                    string iD = basePrefabObject.ID;
                    foreach (var beatmapObj in prefab.objects)
                    {
                        var beatmapObject = BeatmapObject.DeepCopy((BeatmapObject)beatmapObj, false);
                        try
                        {
                            if (ids.ContainsKey(beatmapObj.id))
                                beatmapObject.id = ids[beatmapObj.id];

                            if (ids.ContainsKey(beatmapObj.parent))
                                beatmapObject.parent = ids[beatmapObj.parent];
                            else if (DataManager.inst.gameData.beatmapObjects.FindIndex(x => x.id == beatmapObj.parent) == -1)
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
                        beatmapObject.prefabInstanceID = iD;

                        beatmapObject.StartTime = basePrefabObject.StartTime + prefab.Offset + ((beatmapObject.StartTime + timeToAdd) / Mathf.Clamp(prefabObject.speed, 0.01f, MaxFastSpeed));

                        try
                        {
                            if (beatmapObject.events.Count > 0 && prefabObject.speed != 1f)
                                for (int j = 0; j < beatmapObject.events.Count; j++)
                                {
                                    beatmapObject.events[j].ForEach(x => x.eventTime /= Mathf.Clamp(prefabObject.speed, 0.01f, MaxFastSpeed));
                                }
                        }
                        catch (System.Exception ex)
                        {
                            Debug.LogError($"{className}Failed to set event speed.\n{ex}");
                        }

                        if (prefabObject.autoKillType != PrefabObject.AutoKillType.Regular && basePrefabObject.StartTime + prefab.Offset + beatmapObject.GetObjectLifeLength(_oldStyle: true) > prefabObject.autoKillOffset)
                        {
                            beatmapObject.autoKillType = ObjectAutoKillType.SongTime;
                            beatmapObject.autoKillOffset = prefabObject.autoKillType == PrefabObject.AutoKillType.StartTimeOffset ? prefabObject.StartTime + prefab.Offset + prefabObject.autoKillOffset : prefabObject.autoKillOffset;
                        }

                        if (!Managers.AssetManager.SpriteAssets.ContainsKey(beatmapObject.text) && prefab.SpriteAssets.ContainsKey(beatmapObject.text))
                        {
                            Managers.AssetManager.SpriteAssets.Add(beatmapObject.text, prefab.SpriteAssets[beatmapObject.text]);
                        }

                        beatmapObject.prefabID = basePrefabObject.prefabID;

                        beatmapObject.originalID = beatmapObj.id;
                        DataManager.inst.gameData.beatmapObjects.Add(beatmapObject);
                        if (levelProcessor && levelProcessor.converter != null && !levelProcessor.converter.beatmapObjects.ContainsKey(beatmapObject.id))
                            levelProcessor.converter.beatmapObjects.Add(beatmapObject.id, beatmapObject);
                        list.Add(beatmapObject);
                    }

                    timeToAdd += t;
                }

            if (update)
                foreach (var bm in list)
                {
                    UpdateProcessor(bm);
                }
            list.Clear();
            list = null;
        }

        /// <summary>
        /// Applies all Beatmap Objects stored in the Prefab Object's Prefab to the level.
        /// </summary>
        /// <param name="basePrefabObject"></param>
        /// <param name="update"></param>
        public static IEnumerator IAddPrefabToLevel(BasePrefabObject basePrefabObject, bool update = true)
        {
            var prefabObject = (PrefabObject)basePrefabObject;

            // Checks if prefab exists.
            bool prefabExists = DataManager.inst.gameData.prefabs.FindIndex(x => x.ID == basePrefabObject.prefabID) != -1;
            if (string.IsNullOrEmpty(basePrefabObject.prefabID) || !prefabExists)
            {
                DataManager.inst.gameData.prefabObjects.RemoveAll(x => x.prefabID == basePrefabObject.prefabID);
                yield break;
            }

            float t = 1f;

            if (basePrefabObject.RepeatOffsetTime != 0f)
                t = basePrefabObject.RepeatOffsetTime;

            float timeToAdd = 0f;

            var prefab = (Prefab)DataManager.inst.gameData.prefabs.Find(x => x.ID == basePrefabObject.prefabID);

            var list = new List<BeatmapObject>();
            if (prefab.objects.Count > 0)
                for (int i = 0; i < basePrefabObject.RepeatCount + 1; i++)
                {
                    var ids = prefab.objects.ToDictionary(x => x.id, x => LSFunctions.LSText.randomString(16));

                    string iD = basePrefabObject.ID;
                    foreach (var beatmapObj in prefab.objects)
                    {
                        var beatmapObject = BeatmapObject.DeepCopy((BeatmapObject)beatmapObj, false);
                        try
                        {
                            if (ids.ContainsKey(beatmapObj.id))
                                beatmapObject.id = ids[beatmapObj.id];

                            if (ids.ContainsKey(beatmapObj.parent))
                                beatmapObject.parent = ids[beatmapObj.parent];
                            else if (DataManager.inst.gameData.beatmapObjects.FindIndex(x => x.id == beatmapObj.parent) == -1)
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
                        beatmapObject.prefabInstanceID = iD;

                        beatmapObject.StartTime = basePrefabObject.StartTime + prefab.Offset + ((beatmapObject.StartTime + timeToAdd) / Mathf.Clamp(prefabObject.speed, 0.01f, MaxFastSpeed));

                        try
                        {
                            if (beatmapObject.events.Count > 0 && prefabObject.speed != 1f)
                                for (int j = 0; j < beatmapObject.events.Count; j++)
                                {
                                    beatmapObject.events[j].ForEach(x => x.eventTime /= Mathf.Clamp(prefabObject.speed, 0.01f, MaxFastSpeed));
                                }
                        }
                        catch (System.Exception ex)
                        {
                            Debug.LogError($"{className}Failed to set event speed.\n{ex}");
                        }

                        if (prefabObject.autoKillType != PrefabObject.AutoKillType.Regular && basePrefabObject.StartTime + prefab.Offset + beatmapObject.GetObjectLifeLength(_oldStyle: true) > prefabObject.autoKillOffset)
                        {
                            beatmapObject.autoKillType = ObjectAutoKillType.SongTime;
                            beatmapObject.autoKillOffset = prefabObject.autoKillType == PrefabObject.AutoKillType.StartTimeOffset ? prefabObject.StartTime + prefab.Offset + prefabObject.autoKillOffset : prefabObject.autoKillOffset;
                        }

                        if (!Managers.AssetManager.SpriteAssets.ContainsKey(beatmapObject.text) && prefab.SpriteAssets.ContainsKey(beatmapObject.text))
                        {
                            Managers.AssetManager.SpriteAssets.Add(beatmapObject.text, prefab.SpriteAssets[beatmapObject.text]);
                        }

                        beatmapObject.prefabID = basePrefabObject.prefabID;

                        beatmapObject.originalID = beatmapObj.id;
                        DataManager.inst.gameData.beatmapObjects.Add(beatmapObject);
                        if (levelProcessor && levelProcessor.converter != null && !levelProcessor.converter.beatmapObjects.ContainsKey(beatmapObject.id))
                            levelProcessor.converter.beatmapObjects.Add(beatmapObject.id, beatmapObject);
                        list.Add(beatmapObject);
                    }

                    timeToAdd += t;
                }

            if (update)
                foreach (var bm in list)
                {
                    UpdateProcessor(bm);
                }

            list.Clear();
            list = null;
            yield break;
        }

        /// <summary>
        /// Recaches all the keyframe sequences related to the BeatmapObject.
        /// </summary>
        /// <param name="baseBeatmapObject"></param>
        /// <param name="converter"></param>
        /// <param name="reinsert"></param>
        /// <returns></returns>
        public static IEnumerator RecacheSequences(BaseBeatmapObject baseBeatmapObject, ObjectConverter converter, bool reinsert = true, bool updateParents = false)
        {
            if (converter.cachedSequences.ContainsKey(baseBeatmapObject.id))
            {
                converter.cachedSequences[baseBeatmapObject.id] = null;
                converter.cachedSequences.Remove(baseBeatmapObject.id);
            }

            // Recursive recaching.
            foreach (var beatmapObject in DataManager.inst.gameData.beatmapObjects)
            {
                if (beatmapObject.parent == baseBeatmapObject.id)
                    CoreHelper.StartCoroutine(RecacheSequences(beatmapObject, converter, reinsert, updateParents));
            }

            if (reinsert)
            {
                yield return CoreHelper.StartCoroutine(converter.CacheSequence((BeatmapObject)baseBeatmapObject));

                if (TryGetObject(baseBeatmapObject, out LevelObject levelObject))
                {
                    if (converter.cachedSequences.ContainsKey(baseBeatmapObject.id))
                        levelObject.SetSequences(
                            converter.cachedSequences[baseBeatmapObject.id].ColorSequence,
                            converter.cachedSequences[baseBeatmapObject.id].OpacitySequence,
                            converter.cachedSequences[baseBeatmapObject.id].HueSequence,
                            converter.cachedSequences[baseBeatmapObject.id].SaturationSequence,
                            converter.cachedSequences[baseBeatmapObject.id].ValueSequence);

                    if (updateParents)
                        foreach (var levelParent in levelObject.parentObjects)
                        {
                            if (converter.cachedSequences.ContainsKey(levelParent.ID))
                            {
                                var cachedSequences = converter.cachedSequences[levelParent.ID];
                                levelParent.Position3DSequence = cachedSequences.Position3DSequence;
                                levelParent.ScaleSequence = cachedSequences.ScaleSequence;
                                levelParent.RotationSequence = cachedSequences.RotationSequence;
                            }
                        }
                }
            }

            yield break;
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
        public static IEnumerator UpdateObjects(BaseBeatmapObject baseBeatmapObject, LevelStorage level, List<ILevelObject> objects, ObjectConverter converter, ObjectSpawner spawner, bool reinsert = true)
        {
            string id = baseBeatmapObject.id;

            if (baseBeatmapObject is BeatmapObject modObject)
            {
                modObject.reactivePositionOffset = Vector3.zero;
                modObject.reactiveScaleOffset = Vector3.zero;
                modObject.reactiveRotationOffset = 0f;
                modObject.positionOffset = Vector3.zero;
                modObject.scaleOffset = Vector3.zero;
                modObject.rotationOffset = Vector3.zero;
            }

            // Recursing updating.
            foreach (var beatmapObject in DataManager.inst.gameData.beatmapObjects)
            {
                if (beatmapObject.parent == id)
                    CoreHelper.StartCoroutine(UpdateObjects(beatmapObject, level, objects, converter, spawner, reinsert));
            }

            // Get ILevelObject related to BeatmapObject.
            var iLevelObject = objects == null || objects.Count < 1 ? null : objects.Find(x => x.ID == id);

            // If ILevelObject is not null, then start destroying.
            if (iLevelObject != null)
            {
                var visualObject = ((LevelObject)iLevelObject).visualObject;

                var top = visualObject.Top;

                // Remove GameObject.
                spawner.RemoveObject(iLevelObject);
                objects.Remove(iLevelObject);
                Object.Destroy(top.gameObject);
                ((LevelObject)iLevelObject).parentObjects.Clear();

                // Remove BeatmapObject from converter.
                converter.beatmapObjects.Remove(id);

                iLevelObject = null;
            }

            // If the object should be reinserted and it is not null then we reinsert the object.
            if (reinsert && baseBeatmapObject != null)
            {
                // It's important that the beatmapObjects Dictionary has a reference to the object.
                if (!converter.beatmapObjects.ContainsKey(baseBeatmapObject.id))
                    converter.beatmapObjects.Add(baseBeatmapObject.id, (BeatmapObject)baseBeatmapObject);

                // Convert object to ILevelObject.
                var ilevelObj = converter.ToILevelObject((BeatmapObject)baseBeatmapObject);
                if (ilevelObj != null)
                    level.InsertObject(ilevelObj);
            }

            yield break;
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

        static void ResetOffsets()
        {
            if (DataManager.inst.gameData is not GameData)
                return;

            foreach (var bm in GameData.Current.beatmapObjects)
            {
                if (bm is BeatmapObject modObject)
                {
                    modObject.reactivePositionOffset = Vector3.zero;
                    modObject.reactiveScaleOffset = Vector3.zero;
                    modObject.reactiveRotationOffset = 0f;
                    modObject.positionOffset = Vector3.zero;
                    modObject.scaleOffset = Vector3.zero;
                    modObject.rotationOffset = Vector3.zero;
                }
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

    }
}