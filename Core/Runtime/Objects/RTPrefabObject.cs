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
    public class RTPrefabObject : RTLevelBase, IRTObject, ICustomActivatable
    {
        public RTPrefabObject(Prefab prefab, PrefabObject prefabObject, RTLevelBase parentRuntime)
        {
            Prefab = prefab;
            PrefabObject = prefabObject;
            ParentRuntime = parentRuntime;

            StartTime = prefabObject.StartTime + prefab.offset;
            KillTime = prefabObject.StartTime + prefab.offset + prefabObject.SpawnDuration;

            var gameObject = Creator.NewGameObject(prefab.name, parentRuntime.Parent);
            Parent = gameObject.transform;

            prefabObject.cachedTransform = null;
            var transform = prefabObject.GetTransformOffset();

            Position = transform.position;
            Scale = new Vector3(transform.scale.x, transform.scale.y, 1f);
            Rotation = new Vector3(0f, 0f, transform.rotation);

            UpdateActive();
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

        public RTLevelBase ParentRuntime { get; set; }

        public override Transform Parent { get; }

        public override float FixedTime => UseCustomTime ? CustomTime : GetTime(ParentRuntime.FixedTime);

        /// <summary>
        /// Custom interpolation time.
        /// </summary>
        public float CustomTime { get; set; }

        /// <summary>
        /// If the runtime should use the parent's fixed time.
        /// </summary>
        public bool UseCustomTime { get; set; }

        /// <summary>
        /// If the Runtime Prefab Object is currently active.
        /// </summary>
        public bool IsActive => EngineActive && Active && !PrefabObject.editorData.hidden;
        /// <summary>
        /// If the Runtime Prefab Object is currently active.
        /// </summary>
        public bool EngineActive { get; set; }
        /// <summary>
        /// If the Runtime Prefab Object is currently active. Used for modifiers.
        /// </summary>
        public bool Active { get; set; } = true;

        /// <summary>
        /// Position offset of the Prefab Object.
        /// </summary>
        public Vector3 Position { get; set; }

        /// <summary>
        /// Scale offset of the Prefab Object.
        /// </summary>
        public Vector3 Scale { get; set; }

        /// <summary>
        /// Rotation offset of the Prefab Object.
        /// </summary>
        public Vector3 Rotation { get; set; }

        public override void Load()
        {
            previousAudioTime = 0.0f;
            audioTimeVelocity = 0.0f;

            ApplyPrefab();
            Load(Spawner, false);
        }

        public override void Tick()
        {
            if (!IsActive)
                return;

            CurrentTime = UseCustomTime ? CustomTime : GetTime(ParentRuntime.CurrentTime);

            PreTick();

            try
            {
                OnObjectModifiersTick(); // modifiers update first
                OnBackgroundModifiersTick(); // bg modifiers update second
            }
            catch (Exception ex)
            {
                Debug.LogError($"Had an exception with modifier tick. Exception: {ex}");
            }

            OnBeatmapObjectsTick(); // objects update third
            OnBackgroundObjectsTick(); // bgs update fourth
            OnPrefabModifiersTick(); // prefab modifiers update fifth
            OnPrefabObjectsTick(); // prefab objects update last

            if (Parent)
            {
                Parent.localPosition = Position + PrefabObject.PositionOffset;
                Parent.localScale = Scale + PrefabObject.ScaleOffset;
                Parent.localEulerAngles = Rotation + PrefabObject.RotationOffset;
            }

            PostTick();
        }

        public override void Clear()
        {
            //CoreHelper.Log($"Cleaning up runtime prefab {Prefab}");
            base.Clear();
            CoreHelper.Delete(Parent);
            Spawner?.Clear();
            Spawner = null;
            Prefab = null;
            if (PrefabObject)
                PrefabObject.runtimeObject = null;
            PrefabObject = null;
        }

        public float StartTime { get; set; }

        public float KillTime { get; set; }

        public void SetActive(bool active)
        {
            if (EngineActive == active)
                return;

            EngineActive = active;
            UpdateActive();
        }

        /// <summary>
        /// Sets the active state of the prefab object. Used for modifiers.
        /// </summary>
        /// <param name="active">Active state.</param>
        public void SetCustomActive(bool active)
        {
            if (Active == active)
                return;

            Active = active;
            UpdateActive();
        }

        /// <summary>
        /// Updates the active state of the prefab object.
        /// </summary>
        public void UpdateActive()
        {
            var parent = Parent;
            if (parent)
                parent.gameObject.SetActive(IsActive);
        }

        public void Interpolate(float time) => Tick();
        
        /// <summary>
        /// Applies all Beatmap Objects stored in the Prefab Object's Prefab to the level.
        /// </summary>
        /// <param name="prefabObject">Prefab Object to add to the level</param>
        /// <param name="update">If the object should be updated.</param>
        /// <param name="recalculate">If the engine should be recalculated.</param>
        public void ApplyPrefab()
        {
            var prefabObject = PrefabObject;

            if (!prefabObject)
            {
                CoreHelper.LogError($"Cannot add a null Prefab Object to the level.");
                return;
            }

            prefabObject.positionOffset = Vector3.zero;
            prefabObject.scaleOffset = Vector3.zero;
            prefabObject.rotationOffset = Vector3.zero;

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
                subPrefabCopy.CachedPrefab = Prefab;
                subPrefabCopy.CachedPrefabObject = PrefabObject;

                subPrefabCopy.fromPrefab = true;
                subPrefabCopy.SetPrefabReference(prefabObject);

                subPrefabCopy.originalID = subPrefab.id;
                prefabObject.expandedObjects.Add(subPrefabCopy);
                Spawner.Prefabs.Add(subPrefabCopy);
            }

            for (int i = 0; i < prefabObject.RepeatCount + 1; i++)
            {
                var objectIDs = new List<IDPair>(prefab.beatmapObjects.Count);
                for (int j = 0; j < prefab.beatmapObjects.Count; j++)
                {
                    var beatmapObject = prefab.beatmapObjects[j];
                    //if (CoreConfig.Instance.UseSeedBasedRandom.Value)
                        objectIDs.Add(new IDPair(beatmapObject.id, RandomHelper.RandomString(RandomHelper.GetHash(beatmapObject.id, prefabObject.id, i, RandomHelper.CurrentSeed), 16)));
                    //else
                    //    objectIDs.Add(new IDPair(beatmapObject.id));
                }

                int num = 0;
                foreach (var beatmapObject in prefab.beatmapObjects)
                {
                    var beatmapObjectCopy = beatmapObject.Copy(false);
                    beatmapObjectCopy.CachedPrefab = beatmapObjectCopy.GetPrefab(Spawner.Prefabs);
                    beatmapObjectCopy.CachedPrefabObject = PrefabObject;

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
                    backgroundObjectCopy.CachedPrefabObject = PrefabObject;

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

                objectIDs = new List<IDPair>(prefab.prefabObjects.Count);
                for (int j = 0; j < prefab.prefabObjects.Count; j++)
                {
                    var subPrefabObject = prefab.prefabObjects[j];
                    //if (CoreConfig.Instance.UseSeedBasedRandom.Value)
                        objectIDs.Add(new IDPair(subPrefabObject.id, RandomHelper.RandomString(RandomHelper.GetHash(subPrefabObject.id, prefabObject.id, i, RandomHelper.CurrentSeed), 16)));
                    //else
                    //    objectIDs.Add(new IDPair(subPrefabObject.id));
                }

                num = 0;
                foreach (var subPrefabObject in prefab.prefabObjects)
                {
                    var subPrefabObjectCopy = subPrefabObject.Copy(false);
                    subPrefabObjectCopy.CachedPrefab = subPrefabObjectCopy.GetPrefab(Spawner.Prefabs);
                    subPrefabObjectCopy.CachedPrefabObject = PrefabObject;

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

                    subPrefabObjectCopy.fromPrefabBase = string.IsNullOrEmpty(subPrefabObjectCopy.Parent);
                    if (subPrefabObjectCopy.fromPrefabBase && !string.IsNullOrEmpty(prefabObject.parent))
                    {
                        subPrefabObjectCopy.Parent = prefabObject.parent;
                        subPrefabObjectCopy.parentType = prefabObject.parentType;
                        subPrefabObjectCopy.parentOffsets = prefabObject.parentOffsets;
                        subPrefabObjectCopy.parentAdditive = prefabObject.parentAdditive;
                        subPrefabObjectCopy.parentParallax = prefabObject.parentParallax;
                        subPrefabObjectCopy.desync = prefabObject.desync;
                    }

                    subPrefabObjectCopy.fromPrefab = true;
                    subPrefabObjectCopy.SetPrefabReference(prefabObject);

                    subPrefabObjectCopy.StartTime += timeToAdd;

                    subPrefabObjectCopy.originalID = subPrefabObject.id;
                    GameData.Current.prefabObjects.Add(subPrefabObjectCopy);
                    prefabObject.expandedObjects.Add(subPrefabObjectCopy);
                    Spawner.PrefabObjects.Add(subPrefabObjectCopy);
                    num++;
                }

                timeToAdd += t;
            }
        }

        /// <summary>
        /// Calculates the runtimes' current time.
        /// </summary>
        /// <param name="time">Current time.</param>
        /// <returns>Returns the calculated runtime time.</returns>
        public float GetTime(float time) => (time * PrefabObject.Speed) - ((PrefabObject.StartTime * PrefabObject.Speed) + Prefab.offset);
    }
}
