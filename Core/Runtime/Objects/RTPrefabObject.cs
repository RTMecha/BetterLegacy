using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

using BetterLegacy.Configs;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Helpers;

namespace BetterLegacy.Core.Runtime.Objects
{
    /// <summary>
    /// Controls spawned objects from a Prefab Object at runtime.
    /// </summary>
    public class RTPrefabObject : RTLevelBase, IRTObject
    {
        #region Core

        public RTPrefabObject(Prefab prefab, PrefabObject prefabObject)
        {
            Prefab = prefab;
            PrefabObject = prefabObject;

            StartTime = prefabObject.StartTime;
            KillTime = prefabObject.SpawnDuration;

            var gameObject = Creator.NewGameObject(prefab.name, RTLevel.Current.Parent);
            Parent = gameObject.transform;

            var transform = prefabObject.GetTransformOffset();

            Parent.localPosition = transform.position;
            Parent.localScale = new Vector3(transform.scale.x, transform.scale.y, 1f);
            Parent.localEulerAngles = new Vector3(0f, 0f, transform.rotation);
        }

        /// <summary>
        /// Prefab package.
        /// </summary>
        public Prefab Prefab { get; set; }

        /// <summary>
        /// Prefab instance.
        /// </summary>
        public PrefabObject PrefabObject { get; set; }

        /// <summary>
        /// Spawner package.
        /// </summary>
        public PrefabSpawner Spawner { get; set; } = new PrefabSpawner();

        public override Transform Parent { get; }

        public override float FixedTime => (AudioManager.inst.CurrentAudioSource.time * PrefabObject.Speed) - ((PrefabObject.StartTime * PrefabObject.Speed) + Prefab.offset);

        public override void Load()
        {
            previousAudioTime = 0.0f;
            audioTimeVelocity = 0.0f;

            AddPrefabToLevel(PrefabObject, false, false);

            converter = new ObjectConverter(GameData.Current, this);
            for (int i = 0; i < Spawner.BeatmapObjects.Count; i++)
                converter.CacheSequence(Spawner.BeatmapObjects[i]);

            IEnumerable<IRTObject> runtimeObjects = converter.ToRuntimeObjects(Spawner.BeatmapObjects);

            objects = runtimeObjects.ToList();
            objectEngine = new ObjectEngine(Objects);

            IEnumerable<IRTObject> runtimeModifiers = converter.ToRuntimeModifiers(Spawner.BeatmapObjects);

            modifiers = runtimeModifiers.ToList();
            objectModifiersEngine = new ObjectEngine(Modifiers);

            IEnumerable<BackgroundLayerObject> backgroundLayerObjects = converter.ToBackgroundLayerObjects(Spawner.BackgroundLayers);
            backgroundLayers = backgroundLayerObjects.ToList();

            IEnumerable<IRTObject> runtimeBGObjects = converter.ToRuntimeBGObjects(Spawner.BackgroundObjects);

            bgObjects = runtimeBGObjects.ToList();
            backgroundEngine = new ObjectEngine(BGObjects);

            IEnumerable<IRTObject> runtimeBGModifiers = converter.ToRuntimeBGModifiers(Spawner.BackgroundObjects);

            bgModifiers = runtimeBGModifiers.ToList();
            bgModifiersEngine = new ObjectEngine(BGModifiers);
        }

        public override void Tick()
        {
            if (!CoreConfig.Instance.UseNewUpdateMethod.Value)
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
                var variables = new Dictionary<string, string>();
                ModifiersHelper.RunModifiersLoop(PrefabObject.Modifiers, PrefabObject, variables);

                for (int i = 0; i < modifiers.Count; i++)
                    if (modifiers[i] is RTModifiers runtimeModifiers)
                        runtimeModifiers.variables.InsertRange(variables);
                
                for (int i = 0; i < bgModifiers.Count; i++)
                    if (bgModifiers[i] is RTModifiers runtimeModifiers)
                        runtimeModifiers.variables.InsertRange(variables);

                OnObjectModifiersTick(); // modifiers update second
                OnBackgroundModifiersTick(); // bg modifiers update third
            }
            catch (Exception ex)
            {
                Debug.LogError($"Had an exception with modifier tick. Exception: {ex}");
            }

            OnBeatmapObjectsTick(); // objects update fourth
            OnBackgroundObjectsTick(); // bgs update fifth

            while (postTick != null && !postTick.IsEmpty())
                postTick.Dequeue()?.Invoke();
        }

        public override void Clear()
        {
            base.Clear();
            CoreHelper.Delete(Parent);
            Spawner?.Clear();
            Spawner = null;
            Prefab = null;
            if (PrefabObject)
                PrefabObject.runtimeObject = null;
            PrefabObject = null;
        }

        public override void RecalculateObjectStates()
        {
            objectEngine?.spawner?.RecalculateObjectStates();
            objectModifiersEngine?.spawner?.RecalculateObjectStates();
            backgroundEngine?.spawner?.RecalculateObjectStates();
            bgModifiersEngine?.spawner?.RecalculateObjectStates();
        }

        public float StartTime { get; set; }

        public float KillTime { get; set; }

        public void SetActive(bool active)
        {
            var parent = Parent;
            if (parent)
                parent.gameObject.SetActive(active);
        }

        public void Interpolate(float time) { }

        #endregion

        #region Prefabs

        public override void UpdatePrefab(PrefabObject prefabObject, bool reinsert = true, bool recalculate = true)
        {
            foreach (var beatmapObject in Spawner.BeatmapObjects)
                UpdateObject(beatmapObject, recursive: false, reinsert: false, recalculate: false);

            foreach (var backgroundObject in Spawner.BackgroundObjects)
                UpdateBackgroundObject(backgroundObject, reinsert: false, recalculate: false);

            foreach (var backgroundLayer in Spawner.BackgroundLayers)
                ReinitObject(backgroundLayer, false);

            foreach (var subPrefabObject in Spawner.PrefabObjects)
                UpdatePrefab(subPrefabObject, false, recalculate: false);

            GameData.Current.beatmapObjects.RemoveAll(x => x.PrefabInstanceID == prefabObject.id);
            GameData.Current.backgroundLayers.RemoveAll(x => x.PrefabInstanceID == prefabObject.id);
            GameData.Current.backgroundObjects.RemoveAll(x => x.PrefabInstanceID == prefabObject.id);
            GameData.Current.prefabObjects.RemoveAll(x => x.PrefabInstanceID == prefabObject.id);
            GameData.Current.prefabs.RemoveAll(x => x.PrefabInstanceID == prefabObject.id);

            if (reinsert)
                AddPrefabToLevel(prefabObject);
            else
            {
                Clear();
                prefabObject.runtimeObject = null;
            }
        }

        public override void UpdatePrefab(PrefabObject prefabObject, string context, bool sort = true)
        {
            string lower = context.ToLower().Replace(" ", "").Replace("_", "");
            switch (lower)
            {
                case PrefabObjectContext.TRANSFORM_OFFSET: {
                        prefabObject.cachedTransform = null;
                        var transform = prefabObject.GetTransformOffset();

                        Parent.localPosition = transform.position;
                        Parent.localScale = new Vector3(transform.scale.x, transform.scale.y, 1f);
                        Parent.localEulerAngles = new Vector3(0f, 0f, transform.rotation);

                        break;
                    }
                case PrefabObjectContext.TIME: {
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

                                beatmapObject.StartTime = original.StartTime + timeToAdd;

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

                                backgroundObject.StartTime = original.StartTime + timeToAdd;

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
                case PrefabObjectContext.REPEAT: {
                        UpdatePrefab(prefabObject);
                        break;
                    }
                case PrefabObjectContext.HIDE: {
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
            }
        }

        public override void AddPrefabToLevel(PrefabObject prefabObject, bool update = true, bool recalculate = true)
        {
            if (!prefabObject)
            {
                CoreHelper.LogError($"Cannot add a null Prefab Object to the level.");
                return;
            }

            var prefab = Prefab;
            if (!prefab)
                prefab = prefabObject.GetPrefab();

            if (!prefab)
            {
                GameData.Current.prefabObjects.RemoveAll(x => x.prefabID == prefabObject.prefabID);
                return;
            }

            if (prefab.beatmapObjects.IsEmpty() && prefab.backgroundObjects.IsEmpty() && prefab.prefabs.IsEmpty() && prefab.prefabObjects.IsEmpty() || prefabObject.expanded)
                return;

            Spawner.Clear();

            float t = 1f;

            if (prefabObject.RepeatOffsetTime != 0f)
                t = prefabObject.RepeatOffsetTime;

            float timeToAdd = 0f;

            if (prefabObject.expandedObjects == null)
                prefabObject.expandedObjects = new List<IPrefabable>();
            prefabObject.expandedObjects.Clear();

            foreach (var subPrefab in prefab.prefabs)
            {
                var subPrefabCopy = subPrefab.Copy(false);

                subPrefabCopy.fromPrefab = true;
                subPrefabCopy.SetPrefabReference(prefabObject);

                subPrefabCopy.originalID = subPrefab.id;
                prefabObject.expandedObjects.Add(subPrefabCopy);
                Spawner.Prefabs.Add(subPrefabCopy);
            }

            for (int i = 0; i < prefabObject.RepeatCount + 1; i++)
            {
                var objectIDs = new List<IDPair>();
                for (int j = 0; j < prefab.beatmapObjects.Count; j++)
                    objectIDs.Add(new IDPair(prefab.beatmapObjects[j].id));

                int num = 0;
                foreach (var beatmapObject in prefab.beatmapObjects)
                {
                    var beatmapObjectCopy = beatmapObject.Copy(false);
                    beatmapObjectCopy.CachedPrefab = beatmapObjectCopy.GetPrefab(Spawner.Prefabs);

                    try
                    {
                        beatmapObjectCopy.id = objectIDs[num].newID;

                        if (!string.IsNullOrEmpty(beatmapObject.Parent) && objectIDs.TryFind(x => x.oldID == beatmapObject.Parent, out IDPair idPair))
                            beatmapObjectCopy.Parent = idPair.newID;
                        else if (!string.IsNullOrEmpty(beatmapObject.Parent) && GameData.Current.beatmapObjects.FindIndex(x => x.id == beatmapObject.Parent) == -1 && beatmapObject.Parent != "CAMERA_PARENT")
                            beatmapObjectCopy.Parent = string.Empty;
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"{RTLevel.className}Failed to set object ID.\n{ex}");
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

                    beatmapObjectCopy.StartTime += timeToAdd;

                    if (prefabObject.autoKillType != PrefabAutoKillType.Regular && ((prefabObject.StartTime * prefabObject.Speed) + prefab.offset) + beatmapObjectCopy.SpawnDuration > prefabObject.autoKillOffset)
                    {
                        beatmapObjectCopy.autoKillType = AutoKillType.SongTime;
                        beatmapObjectCopy.autoKillOffset = prefabObject.autoKillType == PrefabAutoKillType.StartTimeOffset ? ((prefabObject.StartTime * prefabObject.Speed) + prefab.offset) - prefabObject.autoKillOffset : prefabObject.autoKillOffset;
                    }

                    if (beatmapObjectCopy.shape == 6 && !string.IsNullOrEmpty(beatmapObjectCopy.text) && prefab.assets.sprites.TryFind(x => x.name == beatmapObjectCopy.text, out SpriteAsset spriteAsset))
                        GameData.Current.assets.sprites.Add(spriteAsset.Copy());

                    beatmapObjectCopy.editorData.hidden = prefabObject.editorData.hidden;
                    beatmapObjectCopy.editorData.selectable = prefabObject.editorData.selectable;

                    beatmapObjectCopy.originalID = beatmapObject.id;
                    GameData.Current.beatmapObjects.Add(beatmapObjectCopy);
                    prefabObject.expandedObjects.Add(beatmapObjectCopy);
                    Spawner.BeatmapObjects.Add(beatmapObjectCopy);

                    num++;
                }

                foreach (var backgroundObject in prefab.backgroundObjects)
                {
                    var backgroundObjectCopy = backgroundObject.Copy();
                    backgroundObjectCopy.CachedPrefab = backgroundObjectCopy.GetPrefab(Spawner.Prefabs);

                    backgroundObjectCopy.fromPrefab = true;
                    backgroundObjectCopy.SetPrefabReference(prefabObject);

                    backgroundObjectCopy.StartTime += timeToAdd;

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
                    Spawner.BackgroundObjects.Add(backgroundObjectCopy);
                }

                objectIDs.Clear();
                for (int j = 0; j < prefab.prefabObjects.Count; j++)
                    objectIDs.Add(new IDPair(prefab.prefabObjects[j].id));

                foreach (var subPrefabObject in prefab.prefabObjects)
                {
                    var subPrefabObjectCopy = subPrefabObject.Copy(false);
                    subPrefabObjectCopy.CachedPrefab = subPrefabObjectCopy.GetPrefab(Spawner.Prefabs);

                    try
                    {
                        subPrefabObjectCopy.id = objectIDs[num].newID;

                        if (!string.IsNullOrEmpty(subPrefabObject.Parent) && objectIDs.TryFind(x => x.oldID == subPrefabObject.Parent, out IDPair idPair))
                            subPrefabObjectCopy.Parent = idPair.newID;
                        else if (!string.IsNullOrEmpty(subPrefabObject.Parent) && GameData.Current.beatmapObjects.FindIndex(x => x.id == subPrefabObject.Parent) == -1 && subPrefabObject.Parent != "CAMERA_PARENT")
                            subPrefabObjectCopy.Parent = string.Empty;
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"{RTLevel.className}Failed to set object ID.\n{ex}");
                    }

                    subPrefabObjectCopy.fromPrefab = true;
                    subPrefabObjectCopy.SetPrefabReference(prefabObject);

                    subPrefabObjectCopy.StartTime += timeToAdd;

                    subPrefabObjectCopy.originalID = subPrefabObject.id;
                    prefabObject.expandedObjects.Add(subPrefabObjectCopy);
                    Spawner.PrefabObjects.Add(subPrefabObjectCopy);
                }

                timeToAdd += t;
            }

            prefabObject.cachedTransform = null;
            if (update)
            {
                var transform = prefabObject.GetTransformOffset();

                foreach (var beatmapObject in Spawner.BeatmapObjects)
                    UpdateObject(beatmapObject, recursive: false, recalculate: false);
                foreach (var backgroundLayer in Spawner.BackgroundLayers)
                    ReinitObject(backgroundLayer);
                foreach (var backgroundObject in Spawner.BackgroundObjects)
                    UpdateBackgroundObject(backgroundObject, recalculate: false);
                foreach (var subPrefabObject in Spawner.PrefabObjects)
                    RTLevel.Current.UpdatePrefab(subPrefabObject, recalculate: false);

                Parent.localPosition = transform.position;
                Parent.localScale = new Vector3(transform.scale.x, transform.scale.y, 1f);
                Parent.localEulerAngles = new Vector3(0f, 0f, transform.rotation);

                if (recalculate)
                    RecalculateObjectStates();
            }
        }

        #endregion
    }
}
