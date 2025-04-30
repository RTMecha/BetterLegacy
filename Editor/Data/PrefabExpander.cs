using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using LSFunctions;

using BetterLegacy.Configs;
using BetterLegacy.Core;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using BetterLegacy.Core.Runtime;
using BetterLegacy.Editor.Managers;

namespace BetterLegacy.Editor.Data
{
    /// <summary>
    /// Helper struct for expanding prefabs.
    /// </summary>
    public struct PrefabExpander
    {
        public PrefabExpander(PrefabObject prefabObject, Prefab prefab, bool select, float offset, bool offsetToCurrentTime, bool regen, bool retainID, bool addBin)
        {
            this.prefabObject = prefabObject;
            this.prefab = prefab;
            this.select = select;
            this.offset = offset;
            this.offsetToCurrentTime = offsetToCurrentTime;
            this.regen = regen;
            this.retainID = retainID;
            this.addBin = addBin;
        }

        public PrefabExpander(PrefabObject prefabObject, Prefab prefab) : this(prefabObject, prefab, false, 0f, false, false, false, false) { }

        public PrefabExpander(PrefabObject prefabObject) : this(prefabObject, prefabObject.GetPrefab()) { }

        public PrefabExpander(Prefab prefab) : this(null, prefab) { }

        public PrefabExpander Select(bool select = true)
        {
            this.select = select;
            return this;
        }
        
        public PrefabExpander Offset(float offset = 0f)
        {
            this.offset = offset;
            return this;
        }

        public PrefabExpander OffsetToCurrentTime(bool offsetToCurrentTime = true)
        {
            this.offsetToCurrentTime = offsetToCurrentTime;
            return this;
        }

        public PrefabExpander Regen(bool regen = true)
        {
            this.regen = regen;
            return this;
        }
        
        public PrefabExpander RetainID(bool retainID = true)
        {
            this.retainID = retainID;
            return this;
        }
        
        public PrefabExpander AddBin(bool addBin = true)
        {
            this.addBin = addBin;
            return this;
        }

        public PrefabObject prefabObject;
        public Prefab prefab;

        public bool select;
        public float offset;
        public bool offsetToCurrentTime;
        public bool regen;
        public bool retainID;
        public bool addBin;

        /// <summary>
        /// Expands the current <see cref="prefab"/>.
        /// </summary>
        public void Expand() => CoroutineHelper.StartCoroutine(IExpand());

        /// <summary>
        /// Expands the current <see cref="prefab"/>.
        /// </summary>
        public IEnumerator IExpand()
        {
            if (!prefab)
            {
                CoreHelper.LogError($"Prefab is null!");
                yield break;
            }

            CoreHelper.Log($"Placing prefab with {prefab.beatmapObjects.Count} objects and {prefab.prefabObjects.Count} prefabs");

            float audioTime = AudioManager.inst.CurrentAudioSource.time;

            if (CoreHelper.InEditor)
            {
                if (EditorConfig.Instance.BPMSnapsPasted.Value && RTEditor.inst.editorInfo.bpmSnapActive)
                    audioTime = RTEditor.SnapToBPM(audioTime);

                if (EditorTimeline.inst.layerType == EditorTimeline.LayerType.Events)
                    EditorTimeline.inst.SetLayer(EditorTimeline.LayerType.Objects);

                if (EditorTimeline.inst.CurrentSelection.isBeatmapObject && prefab.beatmapObjects.Count > 0)
                    ObjectEditor.inst.ClearKeyframes(EditorTimeline.inst.CurrentSelection.GetData<BeatmapObject>());

                if (prefab.beatmapObjects.Count > 1 || prefab.prefabObjects.Count > 1)
                    EditorManager.inst.ClearPopups();
            }

            var sw = CoreHelper.StartNewStopwatch();

            var objectIDs = new List<IDPair>();
            for (int j = 0; j < prefab.beatmapObjects.Count; j++)
                objectIDs.Add(new IDPair(prefab.beatmapObjects[j].id));

            var pastedObjects = new List<BeatmapObject>();
            var unparentedPastedObjects = new List<BeatmapObject>();

            for (int i = 0; i < prefab.beatmapObjects.Count; i++)
            {
                var beatmapObject = prefab.beatmapObjects[i];

                var beatmapObjectCopy = beatmapObject.Copy(false);

                if (!retainID)
                    beatmapObjectCopy.id = objectIDs[i].newID;

                if (!retainID && !string.IsNullOrEmpty(beatmapObject.Parent) && objectIDs.TryFind(x => x.oldID == beatmapObject.Parent, out IDPair idPair))
                    beatmapObjectCopy.Parent = idPair.newID;
                else if (!retainID && !string.IsNullOrEmpty(beatmapObject.Parent) && GameData.Current.beatmapObjects.FindIndex(x => x.id == beatmapObject.Parent) == -1 && beatmapObjectCopy.Parent != BeatmapObject.CAMERA_PARENT)
                    beatmapObjectCopy.Parent = "";

                if (regen)
                    beatmapObjectCopy.RemovePrefabReference();
                else if (prefabObject)
                    beatmapObjectCopy.SetPrefabReference(prefabObject);
                else
                {
                    beatmapObjectCopy.prefabID = beatmapObject.prefabID;
                    beatmapObjectCopy.prefabInstanceID = beatmapObject.prefabInstanceID;
                }

                beatmapObjectCopy.fromPrefab = false;

                if (prefabObject)
                    beatmapObjectCopy.StartTime += prefabObject.StartTime + prefab.offset;
                else
                    beatmapObjectCopy.StartTime += offsetToCurrentTime ? audioTime + prefab.offset : offset;

                if (addBin)
                    ++beatmapObjectCopy.editorData.Bin;

                if (beatmapObjectCopy.shape == 6 && !string.IsNullOrEmpty(beatmapObjectCopy.text) && prefab.assets.sprites.TryFind(x => x.name == beatmapObjectCopy.text, out SpriteAsset spriteAsset))
                    GameData.Current.assets.sprites.Add(spriteAsset.Copy());

                beatmapObjectCopy.editorData.Layer = EditorTimeline.inst.Layer;
                GameData.Current.beatmapObjects.Add(beatmapObjectCopy);

                if (string.IsNullOrEmpty(beatmapObject.Parent) || beatmapObjectCopy.Parent == BeatmapObject.CAMERA_PARENT || GameData.Current.beatmapObjects.FindIndex(x => x.id == beatmapObject.Parent) != -1) // prevent updating of parented objects since updating is recursive.
                    unparentedPastedObjects.Add(beatmapObjectCopy);
                pastedObjects.Add(beatmapObjectCopy);

                if (!CoreHelper.InEditor)
                    continue;

                var timelineObject = new TimelineObject(beatmapObjectCopy);

                timelineObject.Selected = true;
                EditorTimeline.inst.CurrentSelection = timelineObject;

                EditorTimeline.inst.RenderTimelineObject(timelineObject);
            }

            var list = unparentedPastedObjects.Count > 0 ? unparentedPastedObjects : pastedObjects;
            for (int i = 0; i < list.Count; i++)
                RTLevel.Current?.UpdateObject(list[i], recalculate: false);

            unparentedPastedObjects.Clear();
            unparentedPastedObjects = null;
            pastedObjects.Clear();
            pastedObjects = null;

            var ids = new List<string>();
            for (int i = 0; i < prefab.prefabObjects.Count; i++)
                ids.Add(LSText.randomString(16));

            for (int i = 0; i < prefab.prefabObjects.Count; i++)
            {
                var prefabObject = prefab.prefabObjects[i];

                var prefabObjectCopy = prefabObject.Copy(false);
                prefabObjectCopy.id = ids[i];
                prefabObjectCopy.prefabID = prefabObject.prefabID;

                if (this.prefabObject)
                    prefabObjectCopy.StartTime += this.prefabObject.StartTime + prefab.offset;
                else
                    prefabObjectCopy.StartTime += offsetToCurrentTime ? audioTime + prefab.offset : offset;

                if (offset != 0.0)
                    ++prefabObjectCopy.editorData.Bin;

                prefabObjectCopy.editorData.Layer = EditorTimeline.inst.Layer;

                GameData.Current.prefabObjects.Add(prefabObjectCopy);

                RTLevel.Current?.AddPrefabToLevel(prefabObjectCopy, recalculate: false);

                if (!CoreHelper.InEditor)
                    continue;

                var timelineObject = new TimelineObject(prefabObjectCopy);

                timelineObject.Selected = true;
                EditorTimeline.inst.CurrentSelection = timelineObject;

                EditorTimeline.inst.RenderTimelineObject(timelineObject);
            }

            var elapsed = sw.Elapsed;

            RTLevel.Current?.RecalculateObjectStates();

            CoreHelper.StopAndLogStopwatch(sw);

            if (!CoreHelper.InEditor)
            {
                sw = null;
                yield break;
            }

            if (prefabObject)
                EditorManager.inst.DisplayNotification($"Expanded Prefab Object {prefab.name} in {elapsed}!.", 2f, EditorManager.NotificationType.Success);
            else
            {
                string stri = "object";
                if (prefab.beatmapObjects.Count == 1)
                    stri = prefab.beatmapObjects[0].name;
                if (prefab.beatmapObjects.Count > 1)
                    stri = prefab.name;

                EditorManager.inst.DisplayNotification(
                    $"Pasted Beatmap Object{(prefab.beatmapObjects.Count == 1 ? "" : "s")} [ {stri} ] {(regen ? "" : $"and kept Prefab Instance ID")} in {elapsed}!",
                    2f, EditorManager.NotificationType.Success);
            }

            sw = null;

            if (!select)
                yield break;

            if (prefab.beatmapObjects.Count > 1 || prefab.prefabObjects.Count > 1)
                MultiObjectEditor.inst.Dialog.Open();
            else if (EditorTimeline.inst.CurrentSelection.isBeatmapObject)
                ObjectEditor.inst.OpenDialog(EditorTimeline.inst.CurrentSelection.GetData<BeatmapObject>());
            else if (EditorTimeline.inst.CurrentSelection.isPrefabObject)
                RTPrefabEditor.inst.OpenPrefabObjectDialog(EditorTimeline.inst.CurrentSelection.GetData<PrefabObject>());
            yield break;
        }
    }
}
