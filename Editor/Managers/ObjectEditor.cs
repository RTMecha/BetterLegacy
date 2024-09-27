using BetterLegacy.Components;
using BetterLegacy.Components.Editor;
using BetterLegacy.Configs;
using BetterLegacy.Core;
using BetterLegacy.Core.Animation;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using BetterLegacy.Core.Optimization;
using BetterLegacy.Core.Optimization.Objects;
using BetterLegacy.Core.Prefabs;
using BetterLegacy.Example;
using Crosstales.FB;
using HarmonyLib;
using LSFunctions;
using SimpleJSON;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using AutoKillType = DataManager.GameData.BeatmapObject.AutoKillType;
using BaseEventKeyframe = DataManager.GameData.EventKeyframe;
using ObjectType = BetterLegacy.Core.Data.BeatmapObject.ObjectType;

namespace BetterLegacy.Editor.Managers
{
    public class ObjectEditor : MonoBehaviour
    {
        public static ObjectEditor inst;

        public static void Init(ObjEditor objEditor) => objEditor?.gameObject?.AddComponent<ObjectEditor>();

        void Awake()
        {
            inst = this;

            timelinePosScrollbar = ObjEditor.inst.objTimelineContent.parent.parent.GetComponent<ScrollRect>().horizontalScrollbar;
            timelinePosScrollbar.onValueChanged.AddListener(_val =>
            {
                if (CurrentSelection.IsBeatmapObject)
                    CurrentSelection.TimelinePosition = _val;
            });

            var idRight = ObjEditor.inst.objTimelineContent.parent.Find("id/right");
            for (int i = 0; i < ObjEditor.inst.TimelineParents.Count; i++)
            {
                int tmpIndex = i;
                var entry = TriggerHelper.CreateEntry(EventTriggerType.PointerUp, eventData =>
                {
                    if (((PointerEventData)eventData).button != PointerEventData.InputButton.Right)
                        return;

                    float timeTmp = MouseTimelineCalc();

                    var beatmapObject = CurrentSelection.GetData<BeatmapObject>();

                    int index = beatmapObject.events[tmpIndex].FindLastIndex(x => x.eventTime <= timeTmp);
                    var eventKeyfame = AddEvent(beatmapObject, timeTmp, tmpIndex, (EventKeyframe)beatmapObject.events[tmpIndex][index], false);
                    UpdateKeyframeOrder(beatmapObject);

                    RenderKeyframes(beatmapObject);

                    int keyframe = beatmapObject.events[tmpIndex].FindLastIndex(x => x.eventTime == eventKeyfame.eventTime);
                    if (keyframe < 0)
                        keyframe = 0;

                    SetCurrentKeyframe(beatmapObject, tmpIndex, keyframe, false, InputDataManager.inst.editorActions.MultiSelect.IsPressed);
                    ResizeKeyframeTimeline(beatmapObject);

                    RenderObjectKeyframesDialog(beatmapObject);

                    // Keyframes affect both physical object and timeline object.
                    RenderTimelineObject(GetTimelineObject(beatmapObject));
                    if (UpdateObjects)
                        Updater.UpdateObject(beatmapObject, "Keyframes");
                });

                var comp = ObjEditor.inst.TimelineParents[tmpIndex].GetComponent<EventTrigger>();
                comp.triggers.RemoveAll(x => x.eventID == EventTriggerType.PointerUp);
                comp.triggers.Add(entry);

                EditorThemeManager.AddGraphic(idRight.GetChild(i).GetComponent<Image>(), EditorThemeManager.EditorTheme.GetGroup($"Object Keyframe Color {i + 1}"));
            }

            ObjEditor.inst.objTimelineSlider.onValueChanged.RemoveAllListeners();
            ObjEditor.inst.objTimelineSlider.onValueChanged.AddListener(_val =>
            {
                if (!ObjEditor.inst.changingTime)
                    return;
                ObjEditor.inst.newTime = _val;
                AudioManager.inst.SetMusicTime(Mathf.Clamp(_val, 0f, AudioManager.inst.CurrentAudioSource.clip.length));
            });

            var objectKeyframeTimelineEventTrigger = ObjEditor.inst.objTimelineContent.parent.parent.parent.GetComponent<EventTrigger>();
            ObjEditor.inst.objTimelineContent.GetComponent<EventTrigger>().triggers.AddRange(objectKeyframeTimelineEventTrigger.triggers);
            objectKeyframeTimelineEventTrigger.triggers.Clear();

            TriggerHelper.AddEventTriggers(timelinePosScrollbar.gameObject, TriggerHelper.CreateEntry(EventTriggerType.Scroll, baseEventData =>
            {
                var pointerEventData = (PointerEventData)baseEventData;

                var scrollBar = timelinePosScrollbar;
                float multiply = Input.GetKey(KeyCode.LeftAlt) ? 0.1f : Input.GetKey(KeyCode.LeftControl) ? 10f : 1f;

                scrollBar.value = pointerEventData.scrollDelta.y > 0f ? scrollBar.value + (0.005f * multiply) : pointerEventData.scrollDelta.y < 0f ? scrollBar.value - (0.005f * multiply) : 0f;
            }));

            try
            {
                if (!RTFile.FileExists(Application.persistentDataPath + "/copied_objects.lsp"))
                    return;

                var jn = JSON.Parse(RTFile.ReadFromFile(Application.persistentDataPath + "/copied_objects.lsp"));
                ObjEditor.inst.beatmapObjCopy = Prefab.Parse(jn);
                ObjEditor.inst.hasCopiedObject = true;
            }
            catch (Exception ex)
            {
                CoreHelper.LogError($"Could not load global copied objects.\n{ex}");
            }
        }

        #region Variables

        public Scrollbar timelinePosScrollbar;
        public GameObject shapeButtonPrefab;

        public TimelineObject CurrentSelection { get; set; } = new TimelineObject(null);

        public List<TimelineObject> SelectedObjects => RTEditor.inst.timelineObjects.FindAll(x => x.Selected && !x.IsEventKeyframe);
        public List<TimelineObject> SelectedBeatmapObjects => RTEditor.inst.TimelineBeatmapObjects.FindAll(x => x.Selected);
        public List<TimelineObject> SelectedPrefabObjects => RTEditor.inst.TimelinePrefabObjects.FindAll(x => x.Selected);

        public int SelectedObjectCount => SelectedObjects.Count;

        public List<TimelineObject> copiedObjectKeyframes = new List<TimelineObject>();

        public EventKeyframe CopiedPositionData { get; set; }
        public EventKeyframe CopiedScaleData { get; set; }
        public EventKeyframe CopiedRotationData { get; set; }
        public EventKeyframe CopiedColorData { get; set; }

        public List<Toggle> gradientColorButtons = new List<Toggle>();

        public bool colorShifted;

        public static bool RenderPrefabTypeIcon { get; set; }

        public static float TimelineObjectHoverSize { get; set; }

        public static float TimelineCollapseLength { get; set; }

        #endregion

        /// <summary>
        /// Sets the Object Keyframe timeline zoom and position.
        /// </summary>
        /// <param name="zoom">The amount to zoom in.</param>
        /// <param name="position">The position to set the timeline scroll. If the value is less that 0, it will automatically calculate the position to match the audio time.</param>
        /// <param name="render">If the timeline should render.</param>
        public void SetTimeline(float zoom, float position = -1f, bool render = true, bool log = true)
        {
            float prevZoom = ObjEditor.inst.zoomFloat;
            ObjEditor.inst.zoomFloat = Mathf.Clamp01(zoom);
            ObjEditor.inst.zoomVal =
                LSMath.InterpolateOverCurve(ObjEditor.inst.ZoomCurve, ObjEditor.inst.zoomBounds.x, ObjEditor.inst.zoomBounds.y, ObjEditor.inst.zoomFloat);

            var beatmapObject = CurrentSelection.GetData<BeatmapObject>();
            CurrentSelection.Zoom = ObjEditor.inst.zoomFloat;

            if (render)
            {
                ResizeKeyframeTimeline(beatmapObject);
                RenderKeyframes(beatmapObject);
            }

            CoreHelper.StartCoroutine(SetTimelinePosition(beatmapObject, position));

            ObjEditor.inst.zoomSlider.onValueChanged.ClearAll();
            ObjEditor.inst.zoomSlider.value = ObjEditor.inst.zoomFloat;
            ObjEditor.inst.zoomSlider.onValueChanged.AddListener(_val =>
            {
                ObjEditor.inst.Zoom = _val;
                CurrentSelection.Zoom = Mathf.Clamp01(_val);
            });

            if (log)
                CoreHelper.Log($"SET OBJECT ZOOM\n" +
                    $"ZoomFloat: {ObjEditor.inst.zoomFloat}\n" +
                    $"ZoomVal: {ObjEditor.inst.zoomVal}\n" +
                    $"ZoomBounds: {ObjEditor.inst.zoomBounds}\n" +
                    $"Timeline Position: {timelinePosScrollbar.value}");
        }

        IEnumerator SetTimelinePosition(BeatmapObject beatmapObject, float position = 0f)
        {
            yield return new WaitForFixedUpdate();
            float timelineCalc = ObjEditor.inst.objTimelineSlider.value;
            if (AudioManager.inst.CurrentAudioSource.clip != null)
            {
                float time = -beatmapObject.StartTime + AudioManager.inst.CurrentAudioSource.time;
                float objectLifeLength = beatmapObject.GetObjectLifeLength(ObjEditor.inst.ObjectLengthOffset);

                timelineCalc = time / objectLifeLength;
            }

            timelinePosScrollbar.value =
                position >= 0f ? position : timelineCalc;
        }

        public static float TimeTimelineCalc(float _time) => _time * 14f * ObjEditor.inst.zoomVal + 5f;

        public static float MouseTimelineCalc()
        {
            float num = Screen.width * ((1155f - Mathf.Abs(ObjEditor.inst.timelineScroll.transform.AsRT().anchoredPosition.x) + 7f) / 1920f);
            float screenScale = 1f / (Screen.width / 1920f);
            float mouseX = Input.mousePosition.x < num ? num : Input.mousePosition.x;

            return (mouseX - num) / ObjEditor.inst.Zoom / 14f * screenScale;
        }

        #region Dragging

        void Update()
        {
            if (!ObjEditor.inst.changingTime && CurrentSelection && CurrentSelection.IsBeatmapObject)
            {
                // Sets new audio time using the Object Keyframe timeline cursor.
                ObjEditor.inst.newTime = Mathf.Clamp(EditorManager.inst.CurrentAudioPos,
                    CurrentSelection.Time,
                    CurrentSelection.Time + CurrentSelection.GetData<BeatmapObject>().GetObjectLifeLength(ObjEditor.inst.ObjectLengthOffset));
                ObjEditor.inst.objTimelineSlider.value = ObjEditor.inst.newTime;
            }

            if (Input.GetMouseButtonUp(0))
            {
                ObjEditor.inst.beatmapObjectsDrag = false;
                ObjEditor.inst.timelineKeyframesDrag = false;
                RTEditor.inst.dragOffset = -1f;
                RTEditor.inst.dragBinOffset = -100;
            }

            HandleObjectsDrag();
            HandleKeyframesDrag();
        }

        void HandleObjectsDrag()
        {
            if (!ObjEditor.inst.beatmapObjectsDrag)
                return;

            if (InputDataManager.inst.editorActions.MultiSelect.IsPressed)
            {
                int binOffset = 14 - Mathf.RoundToInt((float)((Input.mousePosition.y - 25) * EditorManager.inst.ScreenScaleInverse / 20)) + ObjEditor.inst.mouseOffsetYForDrag;

                bool hasChanged = false;

                foreach (var timelineObject in SelectedObjects)
                {
                    if (timelineObject.Locked)
                        continue;

                    int binCalc = Mathf.Clamp(binOffset + timelineObject.binOffset, 0, 14);

                    if (timelineObject.Bin != binCalc)
                        hasChanged = true;

                    timelineObject.Bin = binCalc;
                    RenderTimelineObjectPosition(timelineObject);
                    if (timelineObject.IsBeatmapObject && SelectedObjects.Count == 1)
                        RenderBin(timelineObject.GetData<BeatmapObject>());
                }

                if (RTEditor.inst.dragBinOffset != binOffset && !SelectedObjects.All(x => x.Locked))
                {
                    if (hasChanged && RTEditor.DraggingPlaysSound)
                        SoundManager.inst.PlaySound("UpDown", 0.4f, 0.6f);

                    RTEditor.inst.dragBinOffset = binOffset;
                }

                return;
            }

            float timeOffset = Mathf.Round(Mathf.Clamp(EditorManager.inst.GetTimelineTime() + ObjEditor.inst.mouseOffsetXForDrag,
                0f, AudioManager.inst.CurrentAudioSource.clip.length) * 1000f) / 1000f;

            if (RTEditor.inst.dragOffset != timeOffset && !SelectedObjects.All(x => x.Locked))
            {
                if (RTEditor.DraggingPlaysSound && (SettingEditor.inst.SnapActive || !RTEditor.DraggingPlaysSoundBPM))
                    SoundManager.inst.PlaySound("LeftRight", SettingEditor.inst.SnapActive ? 0.6f : 0.1f, 0.7f);

                RTEditor.inst.dragOffset = timeOffset;
            }

            if (!Updater.levelProcessor || !Updater.levelProcessor.engine || Updater.levelProcessor.engine.objectSpawner == null)
                return;

            var spawner = Updater.levelProcessor.engine.objectSpawner;

            foreach (var timelineObject in SelectedObjects)
            {
                if (timelineObject.Locked)
                    continue;

                timelineObject.Time = Mathf.Clamp(timeOffset + timelineObject.timeOffset, 0f, AudioManager.inst.CurrentAudioSource.clip.length);

                RenderTimelineObjectPosition(timelineObject);

                if (timelineObject.IsPrefabObject)
                {
                    var prefabObject = timelineObject.GetData<PrefabObject>();
                    RTPrefabEditor.inst.RenderPrefabObjectDialog(prefabObject);
                    Updater.UpdatePrefab(prefabObject, "Start Time");
                    continue;
                }

                var beatmapObject = timelineObject.GetData<BeatmapObject>();

                if (Updater.TryGetObject(beatmapObject, out LevelObject levelObject))
                {
                    levelObject.StartTime = beatmapObject.StartTime;
                    levelObject.KillTime = beatmapObject.StartTime + beatmapObject.GetObjectLifeLength(0.0f, true);

                    levelObject.SetActive(beatmapObject.Alive);

                    for (int i = 0; i < levelObject.parentObjects.Count; i++)
                    {
                        var levelParent = levelObject.parentObjects[i];
                        var parent = levelParent.BeatmapObject;

                        levelParent.timeOffset = parent.StartTime;
                    }
                }

                if (SelectedObjectCount == 1)
                {
                    RenderStartTime(beatmapObject);
                    ResizeKeyframeTimeline(beatmapObject);
                }
            }

            spawner.activateList.Sort((a, b) => a.StartTime.CompareTo(b.StartTime));
            spawner.deactivateList.Sort((a, b) => a.KillTime.CompareTo(b.KillTime));
            spawner.RecalculateObjectStates();

            if (EditorConfig.Instance.UpdateHomingKeyframesDrag.Value)
                System.Threading.Tasks.Task.Run(Updater.UpdateHomingKeyframes);
        }

        void HandleKeyframesDrag()
        {
            if (!ObjEditor.inst.timelineKeyframesDrag || !CurrentSelection.IsBeatmapObject)
                return;

            var beatmapObject = CurrentSelection.GetData<BeatmapObject>();

            var snap = EditorConfig.Instance.BPMSnapsKeyframes.Value;
            var timelineCalc = MouseTimelineCalc();
            var selected = CurrentSelection.InternalSelections.Where(x => x.Selected);
            var startTime = beatmapObject.StartTime;

            foreach (var timelineObject in selected)
            {
                if (timelineObject.Index == 0 || timelineObject.Locked)
                    continue;

                float calc = Mathf.Clamp(
                    Mathf.Round(Mathf.Clamp(timelineCalc + timelineObject.timeOffset + ObjEditor.inst.mouseOffsetXForKeyframeDrag, 0f, AudioManager.inst.CurrentAudioSource.clip.length) * 1000f) / 1000f,
                    0f, beatmapObject.GetObjectLifeLength(ObjEditor.inst.ObjectLengthOffset));

                float st = beatmapObject.StartTime;

                st = SettingEditor.inst.SnapActive && snap && !Input.GetKey(KeyCode.LeftAlt) ? -(st - RTEditor.SnapToBPM(st + calc)) : calc;

                beatmapObject.events[timelineObject.Type][timelineObject.Index].eventTime = st;

                ((RectTransform)timelineObject.GameObject.transform).anchoredPosition = new Vector2(TimeTimelineCalc(st), 0f);

                RenderKeyframe(beatmapObject, timelineObject);
            }

            Updater.UpdateObject(beatmapObject, "Keyframes");
            Updater.UpdateObject(beatmapObject, "Autokill");
            RenderObjectKeyframesDialog(beatmapObject);
            ResizeKeyframeTimeline(beatmapObject);

            foreach (var timelineObject in SelectedBeatmapObjects)
                RenderTimelineObject(timelineObject);

            if (!selected.All(x => x.Locked) && RTEditor.inst.dragOffset != timelineCalc + ObjEditor.inst.mouseOffsetXForDrag)
            {
                if (RTEditor.DraggingPlaysSound && (SettingEditor.inst.SnapActive && snap || !RTEditor.DraggingPlaysSoundBPM))
                    SoundManager.inst.PlaySound("LeftRight", SettingEditor.inst.SnapActive && snap ? 0.6f : 0.1f, 0.8f);

                RTEditor.inst.dragOffset = timelineCalc + ObjEditor.inst.mouseOffsetXForDrag;
            }
        }

        #endregion

        #region Deleting

        public IEnumerator DeleteObjects(bool _set = true)
        {
            var list = SelectedObjects;
            int count = SelectedObjectCount;

            var gameData = GameData.Current;
            if (count == gameData.beatmapObjects.FindAll(x => !x.fromPrefab).Count + gameData.prefabObjects.Count)
            {
                yield break;
            }

            int min = list.Min(x => x.Index) - 1;

            var beatmapObjects = list.FindAll(x => x.IsBeatmapObject).Select(x => x.GetData<BeatmapObject>()).ToList();
            var beatmapObjectIDs = new List<string>();
            var prefabObjectIDs = new List<string>();

            beatmapObjectIDs.AddRange(list.FindAll(x => x.IsBeatmapObject).Select(x => x.ID));
            prefabObjectIDs.AddRange(list.FindAll(x => x.IsPrefabObject).Select(x => x.ID));

            if (beatmapObjectIDs.Count == gameData.beatmapObjects.FindAll(x => !x.fromPrefab).Count)
            {
                yield break;
            }

            if (prefabObjectIDs.Count > 0)
                list.FindAll(x => x.IsPrefabObject)
                    .Select(x => x.GetData<PrefabObject>()).ToList()
                    .ForEach(x => beatmapObjectIDs
                        .AddRange(GameData.Current.beatmapObjects
                            .Where(c => c.prefabInstanceID == x.ID)
                        .Select(c => c.id)));

            gameData.beatmapObjects.FindAll(x => beatmapObjectIDs.Contains(x.id)).ForEach(x => Updater.UpdateObject(x, reinsert: false, recalculate: false));
            gameData.beatmapObjects.FindAll(x => prefabObjectIDs.Contains(x.prefabInstanceID)).ForEach(x => Updater.UpdateObject(x, reinsert: false, recalculate: false));

            gameData.beatmapObjects.RemoveAll(x => beatmapObjectIDs.Contains(x.id));
            gameData.beatmapObjects.RemoveAll(x => prefabObjectIDs.Contains(x.prefabInstanceID));
            gameData.prefabObjects.RemoveAll(x => prefabObjectIDs.Contains(x.ID));

            Updater.levelProcessor?.engine?.objectSpawner?.RecalculateObjectStates();

            RTEditor.inst.timelineObjects.FindAll(x => beatmapObjectIDs.Contains(x.ID) || prefabObjectIDs.Contains(x.ID)).ForEach(x => Destroy(x.GameObject));
            RTEditor.inst.timelineObjects.RemoveAll(x => beatmapObjectIDs.Contains(x.ID) || prefabObjectIDs.Contains(x.ID));

            SetCurrentObject(RTEditor.inst.timelineObjects[Mathf.Clamp(min, 0, RTEditor.inst.timelineObjects.Count - 1)]);

            EditorManager.inst.DisplayNotification($"Deleted Beatmap Objects [ {count} ]", 1f, EditorManager.NotificationType.Success);
            yield break;
        }

        public IEnumerator DeleteObject(TimelineObject timelineObject, bool _set = true)
        {
            int index = timelineObject.Index;

            RTEditor.inst.RemoveTimelineObject(timelineObject);

            if (timelineObject.IsBeatmapObject)
            {
                var beatmapObject = timelineObject.GetData<BeatmapObject>();

                if (GameData.Current.beatmapObjects.Count > 1)
                {
                    Updater.UpdateObject(beatmapObject, reinsert: false, recalculate: false);
                    string id = beatmapObject.id;

                    index = GameData.Current.beatmapObjects.FindIndex(x => x.id == id);

                    GameData.Current.beatmapObjects.RemoveAt(index);

                    foreach (var bm in GameData.Current.beatmapObjects)
                    {
                        if (bm.parent == id)
                        {
                            bm.parent = "";

                            Updater.UpdateObject(bm, recalculate: false);
                        }
                    }

                    Updater.levelProcessor?.engine?.objectSpawner?.RecalculateObjectStates();
                }
                else
                    EditorManager.inst.DisplayNotification("Can't delete only object", 2f, EditorManager.NotificationType.Error);
            }
            else if (timelineObject.IsPrefabObject)
            {
                var prefabObject = timelineObject.GetData<PrefabObject>();

                Updater.UpdatePrefab(prefabObject, false);

                string id = prefabObject.ID;

                index = GameData.Current.prefabObjects.FindIndex(x => x.ID == id);
                GameData.Current.prefabObjects.RemoveAt(index);
            }

            if (_set && RTEditor.inst.timelineObjects.Count > 0)
                SetCurrentObject(RTEditor.inst.timelineObjects[Mathf.Clamp(index - 1, 0, RTEditor.inst.timelineObjects.Count - 1)]);

            yield break;
        }

        public IEnumerator DeleteKeyframes()
        {
            if (CurrentSelection.IsBeatmapObject)
                yield return DeleteKeyframes(CurrentSelection.GetData<BeatmapObject>());
            yield break;
        }

        public IEnumerator DeleteKeyframes(BeatmapObject beatmapObject)
        {
            var bmTimelineObject = GetTimelineObject(beatmapObject);

            var list = bmTimelineObject.InternalSelections.Where(x => x.Selected).ToList();
            int count = list.Where(x => x.Index != 0).Count();

            if (count < 1)
            {
                EditorManager.inst.DisplayNotification($"No Object keyframes to delete.", 2f, EditorManager.NotificationType.Warning);
                yield break;
            }

            int index = list.Min(x => x.Index);
            int type = list.Min(x => x.Type);
            bool allOfTheSameType = list.All(x => x.Type == list.Min(y => y.Type));

            EditorManager.inst.DisplayNotification($"Deleting Object Keyframes [ {count} ]", 0.2f, EditorManager.NotificationType.Success);

            UpdateKeyframeOrder(beatmapObject);

            var strs = new List<string>();
            foreach (var timelineObject in list)
            {
                if (timelineObject.Index != 0)
                    strs.Add(timelineObject.GetData<EventKeyframe>().id);
            }

            for (int i = 0; i < beatmapObject.events.Count; i++)
            {
                beatmapObject.events[i].RemoveAll(x => strs.Contains(((EventKeyframe)x).id));
            }

            bmTimelineObject.InternalSelections.Where(x => x.Selected).ToList().ForEach(x => Destroy(x.GameObject));
            bmTimelineObject.InternalSelections.RemoveAll(x => x.Selected);

            RenderTimelineObject(bmTimelineObject);
            Updater.UpdateObject(beatmapObject, "Keyframes");

            if (beatmapObject.autoKillType == AutoKillType.LastKeyframe || beatmapObject.autoKillType == AutoKillType.LastKeyframeOffset)
                Updater.UpdateObject(beatmapObject, "Autokill");

            RenderKeyframes(beatmapObject);

            if (count == 1 || allOfTheSameType)
                SetCurrentKeyframe(beatmapObject, type, Mathf.Clamp(index - 1, 0, beatmapObject.events[type].Count - 1));
            else
                SetCurrentKeyframe(beatmapObject, type, 0);

            ResizeKeyframeTimeline(beatmapObject);

            EditorManager.inst.DisplayNotification("Deleted Object Keyframes [ " + count + " ]", 2f, EditorManager.NotificationType.Success);

            yield break;
        }

        public void DeleteKeyframe(BeatmapObject beatmapObject, TimelineObject timelineObject)
        {
            if (timelineObject.Index != 0)
            {
                Debug.Log($"{ObjEditor.inst.className}Deleting keyframe: ({timelineObject.Type}, {timelineObject.Index})");
                beatmapObject.events[timelineObject.Type].RemoveAt(timelineObject.Index);

                Destroy(timelineObject.GameObject);

                RenderTimelineObject(GetTimelineObject(beatmapObject));
                if (UpdateObjects)
                    Updater.UpdateObject(beatmapObject, "Keyframes");
                return;
            }
            EditorManager.inst.DisplayNotification("Can't delete first Keyframe", 2f, EditorManager.NotificationType.Error, false);
        }

        #endregion

        #region Copy / Paste

        public void CopyAllSelectedEvents(BeatmapObject beatmapObject)
        {
            copiedObjectKeyframes.Clear();
            UpdateKeyframeOrder(beatmapObject);

            var bmTimelineObject = GetTimelineObject(beatmapObject);

            float num = bmTimelineObject.InternalSelections.Where(x => x.Selected).Min(x => x.Time);

            foreach (var timelineObject in bmTimelineObject.InternalSelections.Where(x => x.Selected))
            {
                int type = timelineObject.Type;
                int index = timelineObject.Index;
                var eventKeyframe = EventKeyframe.DeepCopy((EventKeyframe)beatmapObject.events[type][index]);
                eventKeyframe.eventTime -= num;

                copiedObjectKeyframes.Add(new TimelineObject(eventKeyframe) { Type = type, Index = index, isObjectKeyframe = true });
            }
        }

        public void PasteKeyframes(BeatmapObject beatmapObject, bool setTime = true) => PasteKeyframes(beatmapObject, copiedObjectKeyframes, setTime);

        public void PasteKeyframes(BeatmapObject beatmapObject, List<TimelineObject> kfs, bool setTime = true)
        {
            if (kfs.Count <= 0)
            {
                Debug.LogError($"{ObjEditor.inst.className}No copied event yet!");
                return;
            }

            var ids = new List<string>();
            for (int i = 0; i < beatmapObject.events.Count; i++)
                beatmapObject.events[i].AddRange(kfs.Where(x => x.Type == i).Select(x =>
                {
                    var kf = PasteKF(beatmapObject, x, setTime);
                    ids.Add(kf.id);
                    return kf;
                }));

            ResizeKeyframeTimeline(beatmapObject);
            UpdateKeyframeOrder(beatmapObject);
            RenderKeyframes(beatmapObject);

            if (EditorConfig.Instance.SelectPasted.Value)
            {
                var timelineObject = GetTimelineObject(beatmapObject);
                foreach (var kf in timelineObject.InternalSelections)
                    kf.Selected = ids.Contains(kf.ID);
            }

            RenderObjectKeyframesDialog(beatmapObject);
            RenderTimelineObject(GetTimelineObject(beatmapObject));

            if (UpdateObjects)
            {
                Updater.UpdateObject(beatmapObject, "Keyframes");
                Updater.UpdateObject(beatmapObject, "Autokill");
            }
        }

        public EventKeyframe PasteKF(BeatmapObject beatmapObject, TimelineObject timelineObject, bool setTime = true)
        {
            var eventKeyframe = EventKeyframe.DeepCopy(timelineObject.GetData<EventKeyframe>());

            var time = EditorManager.inst.CurrentAudioPos;
            if (SettingEditor.inst.SnapActive)
                time = RTEditor.SnapToBPM(time);

            if (!setTime)
                return eventKeyframe;

            eventKeyframe.eventTime = time - beatmapObject.StartTime + eventKeyframe.eventTime;
            if (eventKeyframe.eventTime <= 0f)
                eventKeyframe.eventTime = 0.001f;

            return eventKeyframe;
        }

        public void PasteObject(float _offsetTime = 0f, bool _regen = true)
        {
            if (!ObjEditor.inst.hasCopiedObject || ObjEditor.inst.beatmapObjCopy == null || (ObjEditor.inst.beatmapObjCopy.prefabObjects.Count <= 0 && ObjEditor.inst.beatmapObjCopy.objects.Count <= 0))
            {
                EditorManager.inst.DisplayNotification("No copied object yet!", 1f, EditorManager.NotificationType.Error, false);
                return;
            }

            DeselectAllObjects();
            EditorManager.inst.DisplayNotification("Pasting objects, please wait.", 1f, EditorManager.NotificationType.Success);

            StartCoroutine(AddPrefabExpandedToLevel((Prefab)ObjEditor.inst.beatmapObjCopy, true, _offsetTime, false, _regen));
        }

        public EventKeyframe GetCopiedData(int type) => type switch
        {
            0 => CopiedPositionData,
            1 => CopiedScaleData,
            2 => CopiedRotationData,
            3 => CopiedColorData,
            _ => null,
        };

        #endregion

        #region Prefabs

        /// <summary>
        /// Expands a prefab into the level.
        /// </summary>
        /// <param name="prefab"></param>
        /// <param name="select"></param>
        /// <param name="offset"></param>
        /// <param name="undone"></param>
        /// <param name="regen"></param>
        /// <param name="dictionary"></param>
        /// <returns></returns>
        public IEnumerator AddPrefabExpandedToLevel(Prefab prefab, bool select = false, float offset = 0f, bool undone = false, bool regen = false, bool retainID = false)
        {
            RTEditor.inst.ienumRunning = true;
            float delay = 0f;
            float audioTime = EditorManager.inst.CurrentAudioPos;
            CoreHelper.Log($"Placing prefab with {prefab.objects.Count} objects and {prefab.prefabObjects.Count} prefabs");

            if (RTEditor.inst.layerType == RTEditor.LayerType.Events)
                RTEditor.inst.SetLayer(RTEditor.LayerType.Objects);

            if (CurrentSelection.IsBeatmapObject && prefab.objects.Count > 0)
                ClearKeyframes(CurrentSelection.GetData<BeatmapObject>());

            if (prefab.objects.Count > 1 || prefab.prefabObjects.Count > 1)
                EditorManager.inst.ClearDialogs();

            var sw = CoreHelper.StartNewStopwatch();

            var pasteObjectsYieldType = EditorConfig.Instance.PasteObjectsYieldMode.Value;
            var updatePastedObjectsYieldType = EditorConfig.Instance.UpdatePastedObjectsYieldMode.Value;

            //Objects
            {
                var objectIDs = new List<IDPair>();
                for (int j = 0; j < prefab.objects.Count; j++)
                    objectIDs.Add(new IDPair(prefab.objects[j].id, LSText.randomString(16)));

                var pastedObjects = new List<BeatmapObject>();
                var unparentedPastedObjects = new List<BeatmapObject>();
                for (int i = 0; i < prefab.objects.Count; i++)
                {
                    var beatmapObject = prefab.objects[i];
                    if (i > 0 && pasteObjectsYieldType != YieldType.None)
                        yield return CoreHelper.GetYieldInstruction(pasteObjectsYieldType, ref delay);

                    var beatmapObjectCopy = BeatmapObject.DeepCopy((BeatmapObject)beatmapObject, false);

                    if (!retainID)
                        beatmapObjectCopy.id = objectIDs[i].newID;

                    if (!retainID && !string.IsNullOrEmpty(beatmapObject.parent) && objectIDs.TryFind(x => x.oldID == beatmapObject.parent, out IDPair idPair))
                        beatmapObjectCopy.parent = idPair.newID;
                    else if (!retainID && !string.IsNullOrEmpty(beatmapObject.parent) && GameData.Current.beatmapObjects.FindIndex(x => x.id == beatmapObject.parent) == -1 && beatmapObjectCopy.parent != "CAMERA_PARENT")
                        beatmapObjectCopy.parent = "";

                    beatmapObjectCopy.prefabID = beatmapObject.prefabID;
                    if (regen)
                    {
                        beatmapObjectCopy.prefabID = "";
                        beatmapObjectCopy.prefabInstanceID = "";
                    }
                    else
                        beatmapObjectCopy.prefabInstanceID = beatmapObject.prefabInstanceID;

                    beatmapObjectCopy.fromPrefab = false;

                    beatmapObjectCopy.StartTime += offset == 0.0 ? undone ? prefab.Offset : audioTime + prefab.Offset : offset;
                    if (offset != 0.0)
                        ++beatmapObjectCopy.editorData.Bin;

                    if (beatmapObjectCopy.shape == 6 && !string.IsNullOrEmpty(beatmapObjectCopy.text) && prefab.SpriteAssets.TryGetValue(beatmapObjectCopy.text, out Sprite sprite))
                        AssetManager.SpriteAssets[beatmapObjectCopy.text] = sprite;

                    beatmapObjectCopy.editorData.layer = RTEditor.inst.Layer;
                    GameData.Current.beatmapObjects.Add(beatmapObjectCopy);
                    if (Updater.levelProcessor && Updater.levelProcessor.converter != null)
                        Updater.levelProcessor.converter.beatmapObjects[beatmapObjectCopy.id] = beatmapObjectCopy;

                    if (string.IsNullOrEmpty(beatmapObject.parent) || beatmapObjectCopy.parent == "CAMERA_PARENT" || GameData.Current.beatmapObjects.FindIndex(x => x.id == beatmapObject.parent) != -1) // prevent updating of parented objects since updating is recursive.
                        unparentedPastedObjects.Add(beatmapObjectCopy);
                    pastedObjects.Add(beatmapObjectCopy);

                    var timelineObject = new TimelineObject(beatmapObjectCopy);

                    timelineObject.Selected = true;
                    CurrentSelection = timelineObject;

                    RenderTimelineObject(timelineObject);
                }

                var list = unparentedPastedObjects.Count > 0 ? unparentedPastedObjects : pastedObjects;
                delay = 0f;
                for (int i = 0; i < list.Count; i++)
                {
                    if (i > 0 && updatePastedObjectsYieldType != YieldType.None)
                        yield return CoreHelper.GetYieldInstruction(updatePastedObjectsYieldType, ref delay);
                    Updater.UpdateObject(list[i], recalculate: false);
                }

                unparentedPastedObjects.Clear();
                unparentedPastedObjects = null;
                pastedObjects.Clear();
                pastedObjects = null;
            }

            //Prefabs
            {
                var ids = new List<string>();
                for (int i = 0; i < prefab.prefabObjects.Count; i++)
                    ids.Add(LSText.randomString(16));

                delay = 0f;
                for (int i = 0; i < prefab.prefabObjects.Count; i++)
                {
                    var prefabObject = prefab.prefabObjects[i];
                    if (i > 0 && pasteObjectsYieldType != YieldType.None)
                        yield return CoreHelper.GetYieldInstruction(pasteObjectsYieldType, ref delay);

                    var prefabObjectCopy = PrefabObject.DeepCopy((PrefabObject)prefabObject, false);
                    prefabObjectCopy.ID = ids[i];
                    prefabObjectCopy.prefabID = prefabObject.prefabID;

                    prefabObjectCopy.StartTime += offset == 0.0 ? undone ? prefab.Offset : audioTime + prefab.Offset : offset;
                    if (offset != 0.0)
                        ++prefabObjectCopy.editorData.Bin;

                    prefabObjectCopy.editorData.layer = RTEditor.inst.Layer;

                    GameData.Current.prefabObjects.Add(prefabObjectCopy);

                    var timelineObject = new TimelineObject(prefabObjectCopy);

                    timelineObject.Selected = true;
                    CurrentSelection = timelineObject;

                    RenderTimelineObject(timelineObject);

                    Updater.AddPrefabToLevel(prefabObjectCopy, recalculate: false);
                }
            }

            CoreHelper.StopAndLogStopwatch(sw);

            Updater.levelProcessor?.engine?.objectSpawner?.RecalculateObjectStates();

            string stri = "object";
            if (prefab.objects.Count == 1)
                stri = prefab.objects[0].name;
            if (prefab.objects.Count > 1)
                stri = prefab.Name;

            EditorManager.inst.DisplayNotification(
                $"Pasted Beatmap Object{(prefab.objects.Count == 1 ? "" : "s")} [ {stri} ] {(regen ? "" : $"and kept Prefab Instance ID")} in {sw.Elapsed}!",
                5f, EditorManager.NotificationType.Success);

            if (select)
            {
                if (prefab.objects.Count > 1 || prefab.prefabObjects.Count > 1)
                    EditorManager.inst.ShowDialog("Multi Object Editor", false);
                else if (CurrentSelection.IsBeatmapObject)
                    OpenDialog(CurrentSelection.GetData<BeatmapObject>());
                else if (CurrentSelection.IsPrefabObject)
                    PrefabEditor.inst.OpenPrefabDialog();
            }

            RTEditor.inst.ienumRunning = false;
            yield break;
        }

        #endregion

        #region Create New Objects

        public static bool SetToCenterCam => EditorConfig.Instance.CreateObjectsatCameraCenter.Value;

        public void CreateNewNormalObject(bool _select = true, bool setHistory = true)
        {
            var timelineObject = CreateNewDefaultObject(_select);

            var bm = timelineObject.GetData<BeatmapObject>();
            if (SetToCenterCam)
            {
                var pos = EventManager.inst.cam.transform.position;

                bm.events[0][0].eventValues[0] = pos.x;
                bm.events[0][0].eventValues[1] = pos.y;
            }

            Updater.UpdateObject(bm);
            RenderTimelineObject(timelineObject);
            OpenDialog(bm);

            if (setHistory)
            {
                EditorManager.inst.history.Add(new History.Command("Create New Normal Object", delegate ()
                {
                    CreateNewNormalObject(_select, false);
                }, delegate ()
                {
                    inst.StartCoroutine(DeleteObject(timelineObject));
                }), false);
            }
        }

        public void CreateNewCircleObject(bool _select = true, bool setHistory = true)
        {
            var timelineObject = CreateNewDefaultObject(_select);

            var bm = timelineObject.GetData<BeatmapObject>();
            bm.shape = 1;
            bm.shapeOption = 0;
            bm.name = CoreHelper.AprilFools ? "<font=Arrhythmia>bro" : "circle";

            if (SetToCenterCam)
            {
                var pos = EventManager.inst.cam.transform.position;

                bm.events[0][0].eventValues[0] = pos.x;
                bm.events[0][0].eventValues[1] = pos.y;
            }

            Updater.UpdateObject(bm);
            RenderTimelineObject(timelineObject);
            OpenDialog(bm);

            if (setHistory)
            {
                EditorManager.inst.history.Add(new History.Command("Create New Normal Circle Object", delegate ()
                {
                    CreateNewCircleObject(_select, false);
                }, delegate ()
                {
                    inst.StartCoroutine(DeleteObject(timelineObject));
                }), false);
            }
        }

        public void CreateNewTriangleObject(bool _select = true, bool setHistory = true)
        {
            var timelineObject = CreateNewDefaultObject(_select);

            var bm = timelineObject.GetData<BeatmapObject>();
            bm.shape = 2;
            bm.shapeOption = 0;
            bm.name = CoreHelper.AprilFools ? "baracuda <i>beat plays</i>" : "triangle";

            if (SetToCenterCam)
            {
                var pos = EventManager.inst.cam.transform.position;

                bm.events[0][0].eventValues[0] = pos.x;
                bm.events[0][0].eventValues[1] = pos.y;
            }

            Updater.UpdateObject(bm);
            RenderTimelineObject(timelineObject);
            OpenDialog(bm);

            if (setHistory)
            {
                EditorManager.inst.history.Add(new History.Command("Create New Normal Triangle Object", delegate ()
                {
                    CreateNewTriangleObject(_select, false);
                }, delegate ()
                {
                    inst.StartCoroutine(DeleteObject(timelineObject));
                }), false);
            }
        }

        public void CreateNewTextObject(bool _select = true, bool setHistory = true)
        {
            var timelineObject = CreateNewDefaultObject(_select);

            var bm = timelineObject.GetData<BeatmapObject>();
            bm.shape = 4;
            bm.shapeOption = 0;
            bm.text = CoreHelper.AprilFools ? "Never gonna give you up<br>" +
                                            "Never gonna let you down<br>" +
                                            "Never gonna run around and desert you<br>" +
                                            "Never gonna make you cry<br>" +
                                            "Never gonna say goodbye<br>" +
                                            "Never gonna tell a lie and hurt you" : "text";
            bm.name = CoreHelper.AprilFools ? "Don't look at my text" : "text";
            bm.objectType = ObjectType.Decoration;
            if (CoreHelper.AprilFools)
                bm.StartTime += 1f;

            if (SetToCenterCam)
            {
                var pos = EventManager.inst.cam.transform.position;

                bm.events[0][0].eventValues[0] = pos.x;
                bm.events[0][0].eventValues[1] = pos.y;
            }

            Updater.UpdateObject(bm);
            RenderTimelineObject(timelineObject);

            if (!CoreHelper.AprilFools)
                OpenDialog(bm);

            if (setHistory)
            {
                EditorManager.inst.history.Add(new History.Command("Create New Normal Text Object", delegate ()
                {
                    CreateNewTextObject(_select, false);
                }, delegate ()
                {
                    inst.StartCoroutine(DeleteObject(timelineObject));
                }), false);
            }
        }

        public void CreateNewHexagonObject(bool _select = true, bool setHistory = true)
        {
            var timelineObject = CreateNewDefaultObject(_select);

            var bm = timelineObject.GetData<BeatmapObject>();
            bm.shape = 5;
            bm.shapeOption = 0;
            bm.name = CoreHelper.AprilFools ? "super" : "hexagon";

            if (SetToCenterCam)
            {
                var pos = EventManager.inst.cam.transform.position;

                bm.events[0][0].eventValues[0] = pos.x;
                bm.events[0][0].eventValues[1] = pos.y;
            }

            Updater.UpdateObject(bm);
            RenderTimelineObject(timelineObject);
            OpenDialog(bm);

            if (setHistory)
            {
                EditorManager.inst.history.Add(new History.Command("Create New Normal Hexagon Object", delegate ()
                {
                    CreateNewHexagonObject(_select, false);
                }, delegate ()
                {
                    inst.StartCoroutine(DeleteObject(timelineObject));
                }), false);
            }
        }

        public void CreateNewHelperObject(bool _select = true, bool setHistory = true)
        {
            var timelineObject = CreateNewDefaultObject(_select);

            var bm = timelineObject.GetData<BeatmapObject>();
            bm.name = CoreHelper.AprilFools ? "totally not deprecated object" : "helper";
            bm.objectType = CoreHelper.AprilFools ? ObjectType.Decoration : ObjectType.Helper;
            if (CoreHelper.AprilFools)
                bm.events[3][0].eventValues[1] = 0.65f;

            if (SetToCenterCam)
            {
                var pos = EventManager.inst.cam.transform.position;

                bm.events[0][0].eventValues[0] = pos.x;
                bm.events[0][0].eventValues[1] = pos.y;
            }

            Updater.UpdateObject(bm);
            RenderTimelineObject(timelineObject);
            OpenDialog(bm);

            if (setHistory)
            {
                EditorManager.inst.history.Add(new History.Command("Create New Helper Object", delegate ()
                {
                    CreateNewHelperObject(_select, false);
                }, delegate ()
                {
                    inst.StartCoroutine(DeleteObject(timelineObject));
                }), false);
            }
        }

        public void CreateNewDecorationObject(bool _select = true, bool setHistory = true)
        {
            var timelineObject = CreateNewDefaultObject(_select);

            var bm = timelineObject.GetData<BeatmapObject>();
            bm.name = "decoration";
            if (!CoreHelper.AprilFools)
                bm.objectType = ObjectType.Decoration;

            if (SetToCenterCam)
            {
                var pos = EventManager.inst.cam.transform.position;

                bm.events[0][0].eventValues[0] = pos.x;
                bm.events[0][0].eventValues[1] = pos.y;
            }

            Updater.UpdateObject(bm);
            RenderTimelineObject(timelineObject);
            OpenDialog(bm);

            if (setHistory)
            {
                EditorManager.inst.history.Add(new History.Command("Create New Decoration Object", delegate ()
                {
                    CreateNewDecorationObject(_select, false);
                }, delegate ()
                {
                    inst.StartCoroutine(DeleteObject(timelineObject));
                }), false);
            }
        }

        public void CreateNewEmptyObject(bool _select = true, bool setHistory = true)
        {
            var timelineObject = CreateNewDefaultObject(_select);

            var bm = timelineObject.GetData<BeatmapObject>();
            bm.name = "empty";
            if (!CoreHelper.AprilFools)
                bm.objectType = ObjectType.Empty;

            if (SetToCenterCam)
            {
                var pos = EventManager.inst.cam.transform.position;

                bm.events[0][0].eventValues[0] = pos.x;
                bm.events[0][0].eventValues[1] = pos.y + (CoreHelper.AprilFools ? 999f : 0f);
            }

            Updater.UpdateObject(bm);
            RenderTimelineObject(timelineObject);
            OpenDialog(bm);

            if (setHistory)
            {
                EditorManager.inst.history.Add(new History.Command("Create New Empty Object", delegate ()
                {
                    CreateNewEmptyObject(_select, false);
                }, delegate ()
                {
                    inst.StartCoroutine(DeleteObject(timelineObject));
                }), false);
            }
        }

        public void CreateNewNoAutokillObject(bool _select = true, bool setHistory = true)
        {
            var timelineObject = CreateNewDefaultObject(_select);

            var bm = timelineObject.GetData<BeatmapObject>();
            bm.name = CoreHelper.AprilFools ? "dead" : "no autokill";
            bm.autoKillType = AutoKillType.OldStyleNoAutokill;

            if (SetToCenterCam)
            {
                var pos = EventManager.inst.cam.transform.position;

                bm.events[0][0].eventValues[0] = pos.x;
                bm.events[0][0].eventValues[1] = pos.y;
            }

            Updater.UpdateObject(bm);
            RenderTimelineObject(timelineObject);
            OpenDialog(bm);

            if (setHistory)
            {
                EditorManager.inst.history.Add(new History.Command("Create New No Autokill Object", delegate ()
                {
                    CreateNewNoAutokillObject(_select, false);
                }, delegate ()
                {
                    inst.StartCoroutine(DeleteObject(timelineObject));
                }), false);
            }
        }

        public TimelineObject CreateNewDefaultObject(bool _select = true)
        {
            if (!EditorManager.inst.hasLoadedLevel)
            {
                EditorManager.inst.DisplayNotification("Can't add objects to level until a level has been loaded!", 2f, EditorManager.NotificationType.Error);
                return null;
            }

            var list = new List<List<BaseEventKeyframe>>
            {
                new List<BaseEventKeyframe>(),
                new List<BaseEventKeyframe>(),
                new List<BaseEventKeyframe>(),
                new List<BaseEventKeyframe>()
            };
            // Position
            list[0].Add(new EventKeyframe(0f, new float[3]
            {
                0f,
                0f,
                0f,
            }, new float[4], 0));
            // Scale
            list[1].Add(new EventKeyframe(0f, new float[]
            {
                1f,
                1f
            }, new float[3], 0));
            // Rotation
            list[2].Add(new EventKeyframe(0f, new float[1], new float[3], 0));
            ((EventKeyframe)list[2][0]).relative = true;
            // Color
            list[3].Add(new EventKeyframe(0f, new float[10]
            {
                0f, // start color slot
                0f, // start opacity
                0f, // start hue
                0f, // start saturation
                0f, // start value
                1f, // end color slot
                0f, // end opacity
                0f, // end hue
                0f, // end saturation
                0f, // end value
            }, new float[4], 0));

            var beatmapObject = new BeatmapObject(true, AudioManager.inst.CurrentAudioSource.time, "", 0, "", list);
            beatmapObject.id = LSText.randomString(16);
            beatmapObject.autoKillType = AutoKillType.LastKeyframeOffset;
            beatmapObject.autoKillOffset = 5f;

            if (!CoreHelper.AprilFools)
                beatmapObject.editorData.layer = RTEditor.inst.Layer;
            beatmapObject.parentType = EditorConfig.Instance.CreateObjectsScaleParentDefault.Value ? "111" : "101";

            if (RTEditor.inst.layerType == RTEditor.LayerType.Events)
                RTEditor.inst.SetLayer(RTEditor.LayerType.Objects);

            int num = GameData.Current.beatmapObjects.FindIndex(x => x.fromPrefab);
            if (num == -1)
                GameData.Current.beatmapObjects.Add(beatmapObject);
            else
                GameData.Current.beatmapObjects.Insert(num, beatmapObject);

            var timelineObject = new TimelineObject(beatmapObject);

            AudioManager.inst.SetMusicTime(AllowTimeExactlyAtStart ? AudioManager.inst.CurrentAudioSource.time : AudioManager.inst.CurrentAudioSource.time + 0.001f);

            if (_select)
                SetCurrentObject(timelineObject);

            if (ExampleManager.inst && ExampleManager.inst.Visible && RandomHelper.PercentChance(20))
                ExampleManager.inst.SayDialogue("CreateObject");

            return timelineObject;
        }

        public static BeatmapObject CreateNewBeatmapObject(float _time, bool _add = true)
        {
            var beatmapObject = new BeatmapObject();
            beatmapObject.id = LSText.randomString(16);
            beatmapObject.StartTime = _time;

            if (!CoreHelper.AprilFools)
                beatmapObject.editorData.layer = RTEditor.inst.Layer;

            var positionKeyframe = new EventKeyframe();
            positionKeyframe.eventTime = 0f;
            positionKeyframe.SetEventValues(new float[3]);
            positionKeyframe.SetEventRandomValues(new float[4]);

            var scaleKeyframe = new EventKeyframe();
            scaleKeyframe.eventTime = 0f;
            scaleKeyframe.SetEventValues(new float[]
            {
                1f,
                1f
            });

            var rotationKeyframe = new EventKeyframe();
            rotationKeyframe.eventTime = 0f;
            rotationKeyframe.relative = true;
            rotationKeyframe.SetEventValues(new float[1]);

            var colorKeyframe = new EventKeyframe();
            colorKeyframe.eventTime = 0f;
            colorKeyframe.SetEventValues(new float[]
            {
                0f,
                0f,
                0f,
                0f,
                0f,
                0f,
                0f,
                0f,
                0f,
                0f
            });
            colorKeyframe.SetEventRandomValues(0f, 0f, 0f, 0f);

            beatmapObject.events[0].Add(positionKeyframe);
            beatmapObject.events[1].Add(scaleKeyframe);
            beatmapObject.events[2].Add(rotationKeyframe);
            beatmapObject.events[3].Add(colorKeyframe);

            if (_add)
            {
                GameData.Current.beatmapObjects.Add(beatmapObject);

                if (inst)
                {
                    var timelineObject = new TimelineObject(beatmapObject);

                    inst.RenderTimelineObject(timelineObject);
                    Updater.UpdateObject(beatmapObject);
                    inst.SetCurrentObject(timelineObject);
                }
            }
            return beatmapObject;
        }

        #endregion

        #region Selection

        public IEnumerator GroupSelectObjects(bool _add = true)
        {
            if (!_add)
                DeselectAllObjects();

            var list = RTEditor.inst.timelineObjects;
            list.Where(x => x.Layer == RTEditor.inst.Layer && RTMath.RectTransformToScreenSpace(EditorManager.inst.SelectionBoxImage.rectTransform)
            .Overlaps(RTMath.RectTransformToScreenSpace(x.Image.rectTransform))).ToList().ForEach(delegate (TimelineObject x)
            {
                x.Selected = true;
                x.timeOffset = 0f;
                x.binOffset = 0;
            });

            if (SelectedObjectCount > 1)
            {
                EditorManager.inst.ClearDialogs();
                EditorManager.inst.ShowDialog("Multi Object Editor", false);
            }

            if (SelectedObjectCount <= 0)
                CheckpointEditor.inst.SetCurrentCheckpoint(0);

            EditorManager.inst.DisplayNotification($"Selection includes {SelectedObjectCount} objects!", 1f, EditorManager.NotificationType.Success);
            yield break;
        }

        public IEnumerator GroupSelectKeyframes(bool _add = true)
        {
            if (!CurrentSelection.IsBeatmapObject)
                yield break;

            var list = CurrentSelection.InternalSelections;

            if (!_add)
                list.ForEach(x => x.Selected = false);

            list.Where(x => RTMath.RectTransformToScreenSpace(ObjEditor.inst.SelectionBoxImage.rectTransform)
            .Overlaps(RTMath.RectTransformToScreenSpace(x.Image.rectTransform))).ToList().ForEach(delegate (TimelineObject x)
            {
                x.Selected = true;
                x.timeOffset = 0f;
                ObjEditor.inst.currentKeyframeKind = x.Type;
                ObjEditor.inst.currentKeyframe = x.Index;
            });

            var bm = CurrentSelection.GetData<BeatmapObject>();
            RenderObjectKeyframesDialog(bm);
            RenderKeyframes(bm);

            yield break;
        }

        public void DeselectAllObjects()
        {
            foreach (var timelineObject in SelectedObjects)
                timelineObject.Selected = false;
        }

        public void AddSelectedObject(TimelineObject timelineObject)
        {
            if (SelectedObjectCount + 1 > 1)
            {
                EditorManager.inst.ClearDialogs();

                var first = SelectedObjects[0];
                timelineObject.Selected = !timelineObject.Selected;
                if (SelectedObjectCount == 0 || SelectedObjectCount == 1)
                {
                    SetCurrentObject(SelectedObjectCount == 1 ? SelectedObjects[0] : first);
                    return;
                }

                EditorManager.inst.ShowDialog("Multi Object Editor", false);

                RenderTimelineObject(timelineObject);

                return;
            }

            SetCurrentObject(timelineObject);
        }

        public void SetCurrentObject(TimelineObject timelineObject, bool bringTo = false, bool openDialog = true)
        {
            if (!timelineObject.verified && !RTEditor.inst.timelineObjects.Has(x => x.ID == timelineObject.ID))
            {
                RenderTimelineObject(timelineObject);
            }

            if (CurrentSelection.IsBeatmapObject && CurrentSelection.ID != timelineObject.ID)
                for (int i = 0; i < ObjEditor.inst.TimelineParents.Count; i++)
                {
                    LSHelpers.DeleteChildren(ObjEditor.inst.TimelineParents[i]);
                }

            DeselectAllObjects();

            timelineObject.Selected = true;
            CurrentSelection = timelineObject;

            if (!string.IsNullOrEmpty(timelineObject.ID) && openDialog)
            {
                if (timelineObject.IsBeatmapObject)
                    OpenDialog(timelineObject.GetData<BeatmapObject>());
                if (timelineObject.IsPrefabObject)
                    PrefabEditor.inst.OpenPrefabDialog();
            }

            if (bringTo)
            {
                AudioManager.inst.SetMusicTime(timelineObject.Time);
                RTEditor.inst.SetLayer(timelineObject.Layer, RTEditor.LayerType.Objects);
            }
        }

        public void SetCurrentKeyframe(BeatmapObject beatmapObject, int _keyframe, bool _bringTo = false) => SetCurrentKeyframe(beatmapObject, ObjEditor.inst.currentKeyframeKind, _keyframe, _bringTo, false);

        public void AddCurrentKeyframe(BeatmapObject beatmapObject, int _add, bool _bringTo = false)
        {
            SetCurrentKeyframe(beatmapObject,
                ObjEditor.inst.currentKeyframeKind,
                Mathf.Clamp(ObjEditor.inst.currentKeyframe + _add == int.MaxValue ? 1000000 : _add, 0, beatmapObject.events[ObjEditor.inst.currentKeyframeKind].Count - 1),
                _bringTo);
        }

        public void SetCurrentKeyframe(BeatmapObject beatmapObject, int type, int index, bool _bringTo = false, bool _shift = false)
        {
            var bmTimelineObject = GetTimelineObject(beatmapObject);

            if (!ObjEditor.inst.timelineKeyframesDrag)
            {
                Debug.Log($"{ObjEditor.inst.className}Setting Current Keyframe: {type}, {index}");
                if (!_shift && bmTimelineObject.InternalSelections.Count > 0)
                    bmTimelineObject.InternalSelections.ForEach(delegate (TimelineObject x) { x.Selected = false; });

                var kf = GetKeyframe(beatmapObject, type, index);

                kf.Selected = !_shift || !kf.Selected;
            }

            DataManager.inst.UpdateSettingInt("EditorObjKeyframeKind", type);
            DataManager.inst.UpdateSettingInt("EditorObjKeyframe", index);
            ObjEditor.inst.currentKeyframeKind = type;
            ObjEditor.inst.currentKeyframe = index;

            if (_bringTo)
            {
                float value = beatmapObject.events[ObjEditor.inst.currentKeyframeKind][ObjEditor.inst.currentKeyframe].eventTime + beatmapObject.StartTime;

                value = Mathf.Clamp(value, AllowTimeExactlyAtStart ? beatmapObject.StartTime + 0.001f : beatmapObject.StartTime, beatmapObject.StartTime + beatmapObject.GetObjectLifeLength());

                AudioManager.inst.SetMusicTime(Mathf.Clamp(value, 0f, AudioManager.inst.CurrentAudioSource.clip.length));
                AudioManager.inst.CurrentAudioSource.Pause();
                EditorManager.inst.UpdatePlayButton();
            }

            RenderObjectKeyframesDialog(beatmapObject);
        }

        public EventKeyframe AddEvent(BeatmapObject beatmapObject, float time, int type, EventKeyframe _keyframe, bool openDialog)
        {
            var eventKeyframe = EventKeyframe.DeepCopy(_keyframe);
            var t = SettingEditor.inst.SnapActive && EditorConfig.Instance.BPMSnapsKeyframes.Value ? -(beatmapObject.StartTime - RTEditor.SnapToBPM(beatmapObject.StartTime + time)) : time;
            eventKeyframe.eventTime = t;

            if (eventKeyframe.relative)
                for (int i = 0; i < eventKeyframe.eventValues.Length; i++)
                    eventKeyframe.eventValues[i] = 0f;

            eventKeyframe.locked = false;

            beatmapObject.events[type].Add(eventKeyframe);

            RenderTimelineObject(GetTimelineObject(beatmapObject));
            Updater.UpdateObject(beatmapObject, "Autokill");
            if (openDialog)
            {
                ResizeKeyframeTimeline(beatmapObject);
                RenderObjectKeyframesDialog(beatmapObject);
            }
            return eventKeyframe;
        }

        #endregion

        #region Timeline Objects

        /// <summary>
        /// Finds the timeline object with the associated BeatmapObject ID.
        /// </summary>
        /// <param name="beatmapObject"></param>
        /// <returns>Returns either the related TimelineObject or a new TimelineObject if one doesn't exist for whatever reason.</returns>
        public TimelineObject GetTimelineObject(BeatmapObject beatmapObject)
        {
            if (beatmapObject.fromPrefab && RTEditor.inst.timelineObjects.TryFind(x => x.IsPrefabObject && x.ID == beatmapObject.prefabInstanceID, out TimelineObject timelineObject))
                return timelineObject;

            if (!beatmapObject.timelineObject)
                beatmapObject.timelineObject = new TimelineObject(beatmapObject);

            return beatmapObject.timelineObject;
        }

        public GameObject RenderTimelineObject(TimelineObject timelineObject, bool ignoreLayer = true)
        {
            GameObject gameObject = null;

            if (!timelineObject.verified && !RTEditor.inst.timelineObjects.Has(x => x.ID == timelineObject.ID))
            {
                timelineObject.verified = true;
                RTEditor.inst.timelineObjects.Add(timelineObject);
            }

            gameObject = !timelineObject.GameObject ? CreateTimelineObject(timelineObject) : timelineObject.GameObject;

            if (ignoreLayer || RTEditor.inst.Layer == timelineObject.Layer)
            {
                bool locked = false;
                bool collapsed = false;
                int bin = 0;
                string name = "object name";
                float startTime = 0f;
                float offset = 0f;

                string nullName = "";

                var image = timelineObject.Image;

                var color = ObjEditor.inst.NormalColor;

                Prefab prefab = timelineObject.ObjectType switch
                {
                    TimelineObject.TimelineObjectType.BeatmapObject => timelineObject.GetData<BeatmapObject>().Prefab,
                    TimelineObject.TimelineObjectType.PrefabObject => timelineObject.GetData<PrefabObject>().Prefab,
                    _ => null,
                };

                var prefabExists = prefab != null;

                if (timelineObject.IsBeatmapObject)
                {
                    var beatmapObject = timelineObject.GetData<BeatmapObject>();
                    beatmapObject.timelineObject = timelineObject;

                    locked = beatmapObject.editorData.locked;
                    collapsed = beatmapObject.editorData.collapse;
                    bin = beatmapObject.editorData.Bin;
                    name = beatmapObject.name;
                    startTime = beatmapObject.StartTime;
                    offset = beatmapObject.GetObjectLifeLength(_takeCollapseIntoConsideration: true);

                    image.type = GetObjectTypePattern(beatmapObject.objectType);
                    image.sprite = GetObjectTypeSprite(beatmapObject.objectType);

                    if (prefabExists)
                        color = prefab.PrefabType.Color;
                    else
                    {
                        beatmapObject.prefabID = null;
                        beatmapObject.prefabInstanceID = null;
                    }
                }

                if (timelineObject.IsPrefabObject)
                {
                    var prefabObject = timelineObject.GetData<PrefabObject>();

                    locked = prefabObject.editorData.locked;
                    collapsed = prefabObject.editorData.collapse;
                    bin = prefabObject.editorData.Bin;
                    name = prefab.Name;
                    startTime = prefabObject.StartTime + prefab.Offset;
                    offset = prefabObject.GetPrefabLifeLength(true);
                    image.type = Image.Type.Simple;
                    image.sprite = null;

                    var prefabType = prefab.PrefabType;

                    color = prefabType.Color;
                    nullName = prefabType.Name;
                }

                if (timelineObject.Text)
                {
                    var textMeshNoob = timelineObject.Text; // ha! take that tmp
                    textMeshNoob.text = (!string.IsNullOrEmpty(name)) ? string.Format("<mark=#000000aa>{0}</mark>", name) : nullName;
                    textMeshNoob.color = LSColors.white;
                }

                gameObject.transform.Find("icons/lock").gameObject.SetActive(locked);
                gameObject.transform.Find("icons/dots").gameObject.SetActive(collapsed);
                var typeIcon = gameObject.transform.Find("icons/type").gameObject;

                var renderTypeIcon = prefabExists && RenderPrefabTypeIcon;
                typeIcon.SetActive(renderTypeIcon);
                if (renderTypeIcon)
                    gameObject.transform.Find("icons/type/type").GetComponent<Image>().sprite = prefab.PrefabType.icon;

                float zoom = EditorManager.inst.Zoom;

                offset = offset <= TimelineCollapseLength ? TimelineCollapseLength * zoom : offset * zoom;

                var rectTransform = gameObject.transform.AsRT();
                rectTransform.sizeDelta = new Vector2(offset, 20f);
                rectTransform.anchoredPosition = new Vector2(startTime * zoom, (-20 * Mathf.Clamp(bin, 0, 14)));
                if (timelineObject.Hover)
                    timelineObject.Hover.size = TimelineObjectHoverSize;
                gameObject.SetActive(RTEditor.inst.Layer == timelineObject.Layer);
            }

            return gameObject;
        }

        public void RenderTimelineObjects()
        {
            foreach (var timelineObject in RTEditor.inst.timelineObjects.FindAll(x => !x.IsEventKeyframe))
                RenderTimelineObject(timelineObject);
        }

        public void RenderTimelineObjectPosition(TimelineObject timelineObject)
        {
            float offset = 0f;
            float timeOffset = 0f;

            if (timelineObject.IsBeatmapObject)
            {
                var beatmapObject = timelineObject.GetData<BeatmapObject>();
                offset = beatmapObject.GetObjectLifeLength(_takeCollapseIntoConsideration: true);
            }

            if (timelineObject.IsPrefabObject)
            {
                var prefabObject = timelineObject.GetData<PrefabObject>();
                var prefab = GameData.Current.prefabs.Find(x => x.ID == prefabObject.prefabID);

                offset = prefabObject.GetPrefabLifeLength(true);
                timeOffset = prefab.Offset;
            }

            float zoom = EditorManager.inst.Zoom;

            offset = offset <= TimelineCollapseLength ? TimelineCollapseLength * zoom : offset * zoom;

            var rectTransform = timelineObject.GameObject.transform.AsRT();
            rectTransform.sizeDelta = new Vector2(offset, 20f);
            rectTransform.anchoredPosition = new Vector2((timelineObject.Time + timeOffset) * zoom, (-20 * Mathf.Clamp(timelineObject.Bin, 0, 14)));
            if (timelineObject.Hover)
                timelineObject.Hover.size = TimelineObjectHoverSize;
        }

        public void RenderTimelineObjectsPositions()
        {
            foreach (var timelineObject in RTEditor.inst.timelineObjects.FindAll(x => !x.IsEventKeyframe && RTEditor.inst.Layer == x.Layer))
            {
                RenderTimelineObjectPosition(timelineObject);
            }
        }

        public GameObject CreateTimelineObject(TimelineObject timelineObject)
        {
            GameObject gameObject = null;

            if (!timelineObject.verified && !RTEditor.inst.timelineObjects.Has(x => x.ID == timelineObject.ID))
            {
                timelineObject.verified = true;
                RTEditor.inst.timelineObjects.Add(timelineObject);
            }

            if (timelineObject.GameObject)
                Destroy(timelineObject.GameObject);

            gameObject = ObjEditor.inst.timelineObjectPrefab.Duplicate(EditorManager.inst.timeline.transform, "timeline object");
            var storage = gameObject.GetComponent<TimelineObjectStorage>();

            timelineObject.Hover = storage.hoverUI;
            timelineObject.GameObject = gameObject;
            timelineObject.Image = storage.image;
            timelineObject.Text = storage.text;

            storage.eventTrigger.triggers.Clear();
            storage.eventTrigger.triggers.Add(TriggerHelper.CreateBeatmapObjectTrigger(timelineObject));
            storage.eventTrigger.triggers.Add(TriggerHelper.CreateBeatmapObjectStartDragTrigger(timelineObject));
            storage.eventTrigger.triggers.Add(TriggerHelper.CreateBeatmapObjectEndDragTrigger(timelineObject));

            timelineObject.Update();

            return gameObject;
        }

        public IEnumerator ICreateTimelineObjects()
        {
            if (RTEditor.inst.timelineObjects.Count > 0)
                RTEditor.inst.timelineObjects.ForEach(x => Destroy(x.GameObject));

            RTEditor.inst.timelineObjects.Clear();

            for (int i = 0; i < GameData.Current.beatmapObjects.Count; i++)
            {
                var beatmapObject = GameData.Current.beatmapObjects[i];
                if (!string.IsNullOrEmpty(beatmapObject.id) && !beatmapObject.fromPrefab)
                {
                    var timelineObject = GetTimelineObject(beatmapObject);
                    CreateTimelineObject(timelineObject);
                    RenderTimelineObject(timelineObject);
                }
            }

            for (int i = 0; i < GameData.Current.prefabObjects.Count; i++)
            {
                var prefabObject = GameData.Current.prefabObjects[i];
                if (!string.IsNullOrEmpty(prefabObject.ID))
                {
                    var timelineObject = RTPrefabEditor.inst.GetTimelineObject(prefabObject);
                    CreateTimelineObject(timelineObject);
                    RenderTimelineObject(timelineObject);
                }
            }

            yield break;
        }

        public void CreateTimelineObjects()
        {
            if (RTEditor.inst.timelineObjects.Count > 0)
                RTEditor.inst.timelineObjects.ForEach(x => Destroy(x.GameObject));

            RTEditor.inst.timelineObjects.Clear();

            for (int i = 0; i < GameData.Current.beatmapObjects.Count; i++)
            {
                var beatmapObject = GameData.Current.beatmapObjects[i];
                if (!string.IsNullOrEmpty(beatmapObject.id) && !beatmapObject.fromPrefab)
                {
                    var timelineObject = GetTimelineObject(beatmapObject);
                    CreateTimelineObject(timelineObject);
                    RenderTimelineObject(timelineObject);
                }
            }

            for (int i = 0; i < GameData.Current.prefabObjects.Count; i++)
            {
                var prefabObject = GameData.Current.prefabObjects[i];
                if (!string.IsNullOrEmpty(prefabObject.ID))
                {
                    var timelineObject = RTPrefabEditor.inst.GetTimelineObject(prefabObject);
                    CreateTimelineObject(timelineObject);
                    RenderTimelineObject(timelineObject);
                }
            }
        }

        public Sprite GetObjectTypeSprite(ObjectType objectType)
            => objectType == ObjectType.Helper ? ObjEditor.inst.HelperSprite :
            objectType == ObjectType.Decoration ? ObjEditor.inst.DecorationSprite :
            objectType == ObjectType.Empty ? ObjEditor.inst.EmptySprite : null;

        public Image.Type GetObjectTypePattern(ObjectType objectType)
            => objectType == ObjectType.Helper || objectType == ObjectType.Decoration || objectType == ObjectType.Empty ? Image.Type.Tiled : Image.Type.Simple;

        #endregion

        #region RefreshObjectGUI

        public static bool UpdateObjects => true;

        Dictionary<string, object> objectUIElements;
        public Dictionary<string, object> ObjectUIElements
        {
            get
            {
                if (objectUIElements == null || objectUIElements.Count == 0 || objectUIElements.Any(x => x.Value == null))
                {
                    var objEditor = ObjEditor.inst;
                    var tfv = objEditor.ObjectView.transform;

                    if (objectUIElements == null)
                        objectUIElements = new Dictionary<string, object>();
                    objectUIElements.Clear();

                    objectUIElements.Add("ID Base", tfv.Find("id"));
                    objectUIElements.Add("ID Text", tfv.Find("id/text").GetComponent<Text>());
                    objectUIElements.Add("LDM Toggle", tfv.Find("id/ldm/toggle").GetComponent<Toggle>());

                    objectUIElements.Add("Name IF", tfv.Find("name/name").GetComponent<InputField>());
                    objectUIElements.Add("Object Type DD", tfv.Find("name/object-type").GetComponent<Dropdown>());
                    objectUIElements.Add("Tags Content", tfv.Find("Tags Scroll View/Viewport/Content").transform);

                    objectUIElements.Add("Start Time ET", tfv.Find("time").GetComponent<EventTrigger>());
                    objectUIElements.Add("Start Time IF", tfv.Find("time/time").GetComponent<InputField>());
                    objectUIElements.Add("Start Time Lock", tfv.Find("time/lock")?.GetComponent<Toggle>());
                    objectUIElements.Add("Start Time <<", tfv.Find("time/<<").GetComponent<Button>());
                    objectUIElements.Add("Start Time <", tfv.Find("time/<").GetComponent<Button>());
                    objectUIElements.Add("Start Time |", tfv.Find("time/|").GetComponent<Button>());
                    objectUIElements.Add("Start Time >", tfv.Find("time/>").GetComponent<Button>());
                    objectUIElements.Add("Start Time >>", tfv.Find("time/>>").GetComponent<Button>());

                    objectUIElements.Add("Autokill TOD DD", tfv.Find("autokill/tod-dropdown").GetComponent<Dropdown>());
                    objectUIElements.Add("Autokill TOD IF", tfv.Find("autokill/tod-value").GetComponent<InputField>());
                    objectUIElements.Add("Autokill TOD Value", tfv.Find("autokill/tod-value"));
                    objectUIElements.Add("Autokill TOD Set", tfv.Find("autokill/|"));
                    objectUIElements.Add("Autokill TOD Set B", tfv.Find("autokill/|").GetComponent<Button>());
                    objectUIElements.Add("Autokill Collapse", tfv.Find("autokill/collapse").GetComponent<Toggle>());

                    objectUIElements.Add("Parent Name", tfv.Find("parent/text/text").GetComponent<Text>());
                    objectUIElements.Add("Parent Select", tfv.Find("parent/text").GetComponent<Button>());
                    objectUIElements.Add("Parent Info", tfv.Find("parent/text").GetComponent<HoverTooltip>());
                    objectUIElements.Add("Parent More B", tfv.Find("parent/more").GetComponent<Button>());
                    objectUIElements.Add("Parent More", tfv.Find("parent_more"));
                    objectUIElements.Add("Parent Spawn Once", tfv.Find("parent_more/spawn_once").GetComponent<Toggle>());
                    objectUIElements.Add("Parent Search Open", tfv.Find("parent/parent").GetComponent<Button>());
                    objectUIElements.Add("Parent Clear", tfv.Find("parent/clear parent").GetComponent<Button>());
                    objectUIElements.Add("Parent Picker", tfv.Find("parent/parent picker").GetComponent<Button>());

                    objectUIElements.Add("Parent Offset 1", tfv.Find("parent_more/pos_row"));
                    objectUIElements.Add("Parent Offset 2", tfv.Find("parent_more/sca_row"));
                    objectUIElements.Add("Parent Offset 3", tfv.Find("parent_more/rot_row"));

                    objectUIElements.Add("Origin", tfv.Find("origin"));
                    objectUIElements.Add("Origin X IF", tfv.Find("origin/x").GetComponent<InputField>());
                    objectUIElements.Add("Origin Y IF", tfv.Find("origin/y").GetComponent<InputField>());

                    objectUIElements.Add("Gradient", tfv.Find("gradienttype"));
                    objectUIElements.Add("Shape", tfv.Find("shape"));
                    objectUIElements.Add("Shape Settings", tfv.Find("shapesettings"));

                    objectUIElements.Add("Depth", tfv.Find("depth"));
                    objectUIElements.Add("Depth T", tfv.Find("depth input"));
                    objectUIElements.Add("Depth Slider", tfv.Find("depth/depth").GetComponent<Slider>());
                    objectUIElements.Add("Depth IF", tfv.Find("depth input/depth")?.GetComponent<InputField>());
                    objectUIElements.Add("Depth <", tfv.Find("depth input/depth/<")?.GetComponent<Button>());
                    objectUIElements.Add("Depth >", tfv.Find("depth input/depth/>")?.GetComponent<Button>());
                    objectUIElements.Add("Render Type T", tfv.Find("rendertype"));
                    objectUIElements.Add("Render Type", tfv.Find("rendertype")?.GetComponent<Dropdown>());

                    objectUIElements.Add("Bin Slider", tfv.Find("editor/bin").GetComponent<Slider>());
                    objectUIElements.Add("Layers IF", tfv.Find("editor/layers")?.GetComponent<InputField>());
                    objectUIElements.Add("Layers Image", tfv.Find("editor/layers")?.GetComponent<Image>());

                    objectUIElements.Add("Collapse Label", tfv.Find("collapselabel").gameObject);
                    objectUIElements.Add("Collapse Prefab", tfv.Find("applyprefab").gameObject);
                }

                return objectUIElements;
            }
            set => objectUIElements = value;
        }

        public static bool HideVisualElementsWhenObjectIsEmpty { get; set; }

        /// <summary>
        /// Opens the Object Editor dialog.
        /// </summary>
        /// <param name="beatmapObject">The Beatmap Object to edit.</param>
        public void OpenDialog(BeatmapObject beatmapObject)
        {
            if (!CurrentSelection.IsBeatmapObject)
            {
                EditorManager.inst.DisplayNotification("Cannot edit non-object!", 2f, EditorManager.NotificationType.Error);
                return;
            }

            if (EditorManager.inst.ActiveDialogs.Count > 2 || !EditorManager.inst.ActiveDialogs.Has(x => x.Name == "Object Editor")) // Only need to clear the dialogs if object editor isn't the only active dialog.
            {
                EditorManager.inst.ClearDialogs();
                EditorManager.inst.ShowDialog("Object Editor");
            }

            if (CurrentSelection.ID != beatmapObject.id)
                for (int i = 0; i < ObjEditor.inst.TimelineParents.Count; i++)
                    LSHelpers.DeleteChildren(ObjEditor.inst.TimelineParents[i]);

            StartCoroutine(RefreshObjectGUI(beatmapObject));
        }

        /// <summary>
        /// Refreshes the Object Editor to the specified BeatmapObject, allowing for any object to be edited from anywhere.
        /// </summary>
        /// <param name="beatmapObject">The BeatmapObject to set.</param>
        /// <returns></returns>
        public static IEnumerator RefreshObjectGUI(BeatmapObject beatmapObject)
        {
            if (!EditorManager.inst.hasLoadedLevel || string.IsNullOrEmpty(beatmapObject.id))
                yield break;

            inst.CurrentSelection = inst.GetTimelineObject(beatmapObject);
            inst.CurrentSelection.Selected = true;

            inst.RenderIDLDM(beatmapObject);
            inst.RenderName(beatmapObject);
            inst.RenderObjectType(beatmapObject);

            inst.RenderStartTime(beatmapObject);
            inst.RenderAutokill(beatmapObject);

            inst.RenderParent(beatmapObject);

            inst.RenderEmpty(beatmapObject);

            if (!HideVisualElementsWhenObjectIsEmpty || beatmapObject.objectType != ObjectType.Empty)
            {
                inst.RenderOrigin(beatmapObject);
                inst.RenderGradient(beatmapObject);
                inst.RenderShape(beatmapObject);
                inst.RenderDepth(beatmapObject);
            }

            inst.RenderLayers(beatmapObject);
            inst.RenderBin(beatmapObject);

            inst.RenderGameObjectInspector(beatmapObject);

            bool fromPrefab = !string.IsNullOrEmpty(beatmapObject.prefabID);
            ((GameObject)inst.ObjectUIElements["Collapse Label"]).SetActive(fromPrefab);
            ((GameObject)inst.ObjectUIElements["Collapse Prefab"]).SetActive(fromPrefab);

            inst.SetTimeline(inst.CurrentSelection.Zoom, inst.CurrentSelection.TimelinePosition);

            inst.RenderObjectKeyframesDialog(beatmapObject);

            try
            {
                if (EditorConfig.Instance.ShowMarkersInObjectEditor.Value)
                    inst.RenderMarkers(beatmapObject);
            }
            catch (Exception ex)
            {
                CoreHelper.LogError($"Error {ex}");
            }

            if (ObjectModifiersEditor.inst)
                inst.StartCoroutine(ObjectModifiersEditor.inst.RenderModifiers(beatmapObject));

            yield break;
        }

        /// <summary>
        /// Sets specific GUI elements active / inactive depending on settings.
        /// </summary>
        /// <param name="beatmapObject">The Beatmap Object to set.</param>
        public void RenderEmpty(BeatmapObject beatmapObject)
        {
            var active = !HideVisualElementsWhenObjectIsEmpty || beatmapObject.objectType != ObjectType.Empty;
            var shapeTF = (Transform)ObjectUIElements["Shape"];
            var shapesLabel = shapeTF.parent.GetChild(shapeTF.GetSiblingIndex() - 2);
            var shapeTFPActive = shapesLabel.gameObject.activeSelf;
            shapeTF.parent.GetChild(shapeTF.GetSiblingIndex() - 2).gameObject.SetActive(active);
            shapeTF.gameObject.SetActive(active);

            try
            {
                shapesLabel.GetChild(0).GetComponent<Text>().text = RTEditor.NotSimple ? "Gradient / Shape" : "Shape";
            }
            catch (Exception ex)
            {
                CoreHelper.LogException(ex);
            }
            ((Transform)ObjectUIElements["Gradient"]).gameObject.SetActive(active && RTEditor.NotSimple);

            ((Transform)ObjectUIElements["Shape Settings"]).gameObject.SetActive(active);
            ((Transform)ObjectUIElements["Depth"]).gameObject.SetActive(active);
            var depthTf = (Transform)ObjectUIElements["Depth T"];
            depthTf.parent.GetChild(depthTf.GetSiblingIndex() - 1).gameObject.SetActive(active);
            depthTf.gameObject.SetActive(RTEditor.NotSimple && active);
            ((Slider)ObjectUIElements["Depth Slider"]).transform.AsRT().sizeDelta = new Vector2(RTEditor.NotSimple ? 352f : 292f, 32f);

            var renderTypeTF = (Transform)ObjectUIElements["Render Type T"];
            renderTypeTF.parent.GetChild(renderTypeTF.GetSiblingIndex() - 1).gameObject.SetActive(active && RTEditor.ShowModdedUI);
            renderTypeTF.gameObject.SetActive(active && RTEditor.ShowModdedUI);

            var originTF = (Transform)ObjectUIElements["Origin"];
            originTF.parent.GetChild(originTF.GetSiblingIndex() - 1).gameObject.SetActive(active);
            originTF.gameObject.SetActive(active);

            var tagsParent = ObjEditor.inst.ObjectView.transform.Find("Tags Scroll View");
            tagsParent.parent.GetChild(tagsParent.GetSiblingIndex() - 1).gameObject.SetActive(RTEditor.ShowModdedUI);
            bool tagsActive = tagsParent.gameObject.activeSelf;
            tagsParent.gameObject.SetActive(RTEditor.ShowModdedUI);

            ((Transform)ObjectUIElements["ID Base"]).Find("ldm").gameObject.SetActive(RTEditor.ShowModdedUI);

            ObjEditor.inst.ObjectView.transform.Find("int_variable").gameObject.SetActive(RTEditor.ShowModdedUI);
            ObjEditor.inst.ObjectView.transform.Find("ignore life").gameObject.SetActive(RTEditor.ShowModdedUI);

            var activeModifiers = ObjEditor.inst.ObjectView.transform.Find("active").gameObject;

            if (!RTEditor.ShowModdedUI)
                activeModifiers.GetComponent<Toggle>().isOn = false;

            activeModifiers.SetActive(RTEditor.ShowModdedUI);

            if (active && !shapeTFPActive)
            {
                RenderOrigin(beatmapObject);
                RenderShape(beatmapObject);
                RenderDepth(beatmapObject);
            }

            if (RTEditor.ShowModdedUI && !tagsActive)
            {
                RenderIDLDM(beatmapObject);
                RenderName(beatmapObject);
            }
        }

        /// <summary>
        /// Renders the ID Text and LDM Toggle.
        /// </summary>
        /// <param name="beatmapObject">The Beatmap Object to set.</param>
        public void RenderIDLDM(BeatmapObject beatmapObject)
        {
            var idText = (Text)ObjectUIElements["ID Text"];
            idText.text = $"ID: {beatmapObject.id}";

            var gameObject = idText.transform.parent.gameObject;

            var clickable = gameObject.GetComponent<Clickable>() ?? gameObject.AddComponent<Clickable>();

            clickable.onClick = pointerEventData =>
            {
                EditorManager.inst.DisplayNotification($"Copied ID from {beatmapObject.name}!", 2f, EditorManager.NotificationType.Success);
                LSText.CopyToClipboard(beatmapObject.id);
            };

            var ldmToggle = (Toggle)ObjectUIElements["LDM Toggle"];
            ldmToggle.onValueChanged.ClearAll();
            ldmToggle.isOn = beatmapObject.LDM;
            ldmToggle.onValueChanged.AddListener(_val =>
            {
                beatmapObject.LDM = _val;
                Updater.UpdateObject(beatmapObject);
            });
        }

        /// <summary>
        /// Renders the Name InputField.
        /// </summary>
        /// <param name="beatmapObject">The Beatmap Object to set.</param>
        public void RenderName(BeatmapObject beatmapObject)
        {
            var name = (InputField)ObjectUIElements["Name IF"];

            // Allows for left / right flipping.
            if (!name.GetComponent<InputFieldSwapper>() && name.gameObject)
            {
                var t = name.gameObject.AddComponent<InputFieldSwapper>();
                t.Init(name, InputFieldSwapper.Type.String);
            }

            EditorHelper.AddInputFieldContextMenu(name);

            name.onValueChanged.ClearAll();
            name.text = beatmapObject.name;
            name.onValueChanged.AddListener(_val =>
            {
                beatmapObject.name = _val;

                // Since name has no effect on the physical object, we will only need to update the timeline object.
                RenderTimelineObject(GetTimelineObject(beatmapObject));
            });

            var tagsParent = (Transform)ObjectUIElements["Tags Content"];

            if (!RTEditor.ShowModdedUI)
                return;

            LSHelpers.DeleteChildren(tagsParent);

            int num = 0;
            foreach (var tag in beatmapObject.tags)
            {
                int index = num;
                var gameObject = RTEditor.inst.tagPrefab.Duplicate(tagsParent, index.ToString());
                gameObject.transform.localScale = Vector3.one;
                var input = gameObject.transform.Find("Input").GetComponent<InputField>();
                input.onValueChanged.ClearAll();
                input.text = tag;
                input.onValueChanged.AddListener(_val => { beatmapObject.tags[index] = _val; });

                var inputFieldSwapper = gameObject.AddComponent<InputFieldSwapper>();
                inputFieldSwapper.Init(input, InputFieldSwapper.Type.String);

                var deleteStorage = gameObject.transform.Find("Delete").GetComponent<DeleteButtonStorage>();
                deleteStorage.button.onClick.ClearAll();
                deleteStorage.button.onClick.AddListener(() =>
                {
                    beatmapObject.tags.RemoveAt(index);
                    RenderName(beatmapObject);
                });

                EditorHelper.AddInputFieldContextMenu(input);

                EditorThemeManager.ApplyGraphic(gameObject.GetComponent<Image>(), ThemeGroup.Input_Field, true);

                EditorThemeManager.ApplyInputField(input);

                EditorThemeManager.ApplyGraphic(deleteStorage.baseImage, ThemeGroup.Delete, true);
                EditorThemeManager.ApplyGraphic(deleteStorage.image, ThemeGroup.Delete_Text);

                num++;
            }

            var add = PrefabEditor.inst.CreatePrefab.Duplicate(tagsParent, "Add");
            add.transform.localScale = Vector3.one;
            var addText = add.transform.Find("Text").GetComponent<Text>();
            addText.text = "Add Tag";
            var addButton = add.GetComponent<Button>();
            addButton.onClick.ClearAll();
            addButton.onClick.AddListener(() =>
            {
                beatmapObject.tags.Add("New Tag");
                RenderName(beatmapObject);
            });

            EditorThemeManager.ApplyGraphic(addButton.image, ThemeGroup.Add, true);
            EditorThemeManager.ApplyGraphic(addText, ThemeGroup.Add_Text, true);
        }

        /// <summary>
        /// Renders the ObjectType Dropdown.
        /// </summary>
        /// <param name="beatmapObject">The Beatmap Object to set.</param>
        public void RenderObjectType(BeatmapObject beatmapObject)
        {
            var objType = (Dropdown)ObjectUIElements["Object Type DD"];

            objType.options =
                EditorConfig.Instance.EditorComplexity.Value == Complexity.Advanced ?
                    CoreHelper.StringToOptionData("Normal", "Helper", "Decoration", "Empty", "Solid") :
                    CoreHelper.StringToOptionData("Normal", "Helper", "Decoration", "Empty"); // don't show solid object type 
            objType.onValueChanged.ClearAll();
            objType.value = Mathf.Clamp((int)beatmapObject.objectType, 0, objType.options.Count - 1);
            objType.onValueChanged.AddListener(_val =>
            {
                beatmapObject.objectType = (ObjectType)_val;
                RenderGameObjectInspector(beatmapObject);
                // ObjectType affects both physical object and timeline object.
                RenderTimelineObject(GetTimelineObject(beatmapObject));
                if (UpdateObjects)
                    Updater.UpdateObject(beatmapObject);

                RenderEmpty(beatmapObject);
            });
        }

        /// <summary>
        /// Renders all StartTime UI.
        /// </summary>
        /// <param name="beatmapObject">The Beatmap Object to set.</param>
        public void RenderStartTime(BeatmapObject beatmapObject)
        {
            var time = (EventTrigger)ObjectUIElements["Start Time ET"];
            var timeIF = (InputField)ObjectUIElements["Start Time IF"];
            var locker = (Toggle)ObjectUIElements["Start Time Lock"];
            var timeJumpLargeLeft = (Button)ObjectUIElements["Start Time <<"];
            var timeJumpLeft = (Button)ObjectUIElements["Start Time <"];
            var setStartToTime = (Button)ObjectUIElements["Start Time |"];
            var timeJumpRight = (Button)ObjectUIElements["Start Time >"];
            var timeJumpLargeRight = (Button)ObjectUIElements["Start Time >>"];

            locker.onValueChanged.ClearAll();
            locker.isOn = beatmapObject.editorData.locked;
            locker.onValueChanged.AddListener(_val =>
            {
                beatmapObject.editorData.locked = _val;

                // Since locking has no effect on the physical object, we will only need to update the timeline object.
                RenderTimelineObject(GetTimelineObject(beatmapObject));
            });

            timeIF.onValueChanged.ClearAll();
            timeIF.text = beatmapObject.StartTime.ToString();
            timeIF.onValueChanged.AddListener(_val =>
            {
                if (float.TryParse(_val, out float num))
                {
                    beatmapObject.StartTime = Mathf.Clamp(num, 0f, AudioManager.inst.CurrentAudioSource.clip.length);

                    ResizeKeyframeTimeline(beatmapObject);

                    // StartTime affects both physical object and timeline object.
                    RenderTimelineObject(GetTimelineObject(beatmapObject));
                    if (UpdateObjects)
                        Updater.UpdateObject(beatmapObject, "StartTime");
                }
            });

            time.triggers.Clear();
            time.triggers.Add(TriggerHelper.ScrollDelta(timeIF, max: AudioManager.inst.CurrentAudioSource.clip.length));

            timeJumpLargeLeft.onClick.ClearAll();
            timeJumpLargeLeft.interactable = (beatmapObject.StartTime > 0f);
            timeJumpLargeLeft.onClick.AddListener(() =>
            {
                float moveTime = beatmapObject.StartTime - 1f;
                moveTime = Mathf.Clamp(moveTime, 0f, AudioManager.inst.CurrentAudioSource.clip.length);
                timeIF.text = moveTime.ToString();

                ResizeKeyframeTimeline(beatmapObject);

                // StartTime affects both physical object and timeline object.
                RenderTimelineObject(GetTimelineObject(beatmapObject));
                if (UpdateObjects)
                    Updater.UpdateObject(beatmapObject, "StartTime");

                ResizeKeyframeTimeline(beatmapObject);
            });

            timeJumpLeft.onClick.ClearAll();
            timeJumpLeft.interactable = (beatmapObject.StartTime > 0f);
            timeJumpLeft.onClick.AddListener(() =>
            {
                float moveTime = beatmapObject.StartTime - 0.1f;
                moveTime = Mathf.Clamp(moveTime, 0f, AudioManager.inst.CurrentAudioSource.clip.length);
                timeIF.text = moveTime.ToString();

                ResizeKeyframeTimeline(beatmapObject);

                // StartTime affects both physical object and timeline object.
                RenderTimelineObject(GetTimelineObject(beatmapObject));
                if (UpdateObjects)
                    Updater.UpdateObject(beatmapObject, "StartTime");

                ResizeKeyframeTimeline(beatmapObject);
            });

            setStartToTime.onClick.ClearAll();
            setStartToTime.onClick.AddListener(() =>
            {
                timeIF.text = EditorManager.inst.CurrentAudioPos.ToString();

                ResizeKeyframeTimeline(beatmapObject);

                // StartTime affects both physical object and timeline object.
                RenderTimelineObject(GetTimelineObject(beatmapObject));
                if (UpdateObjects)
                    Updater.UpdateObject(beatmapObject, "StartTime");

                ResizeKeyframeTimeline(beatmapObject);
            });

            timeJumpRight.onClick.ClearAll();
            timeJumpRight.onClick.AddListener(() =>
            {
                float moveTime = beatmapObject.StartTime + 0.1f;
                moveTime = Mathf.Clamp(moveTime, 0f, AudioManager.inst.CurrentAudioSource.clip.length);
                timeIF.text = moveTime.ToString();

                ResizeKeyframeTimeline(beatmapObject);

                // StartTime affects both physical object and timeline object.
                RenderTimelineObject(GetTimelineObject(beatmapObject));
                if (UpdateObjects)
                    Updater.UpdateObject(beatmapObject, "StartTime");

                ResizeKeyframeTimeline(beatmapObject);
            });

            timeJumpLargeRight.onClick.ClearAll();
            timeJumpLargeRight.onClick.AddListener(() =>
            {
                float moveTime = beatmapObject.StartTime + 1f;
                moveTime = Mathf.Clamp(moveTime, 0f, AudioManager.inst.CurrentAudioSource.clip.length);
                timeIF.text = moveTime.ToString();

                ResizeKeyframeTimeline(beatmapObject);

                // StartTime affects both physical object and timeline object.
                RenderTimelineObject(GetTimelineObject(beatmapObject));
                if (UpdateObjects)
                    Updater.UpdateObject(beatmapObject, "StartTime");

                ResizeKeyframeTimeline(beatmapObject);
            });
        }

        /// <summary>
        /// Renders all Autokill UI.
        /// </summary>
        /// <param name="beatmapObject">The Beatmap Object to set.</param>
        public void RenderAutokill(BeatmapObject beatmapObject)
        {
            var akType = (Dropdown)ObjectUIElements["Autokill TOD DD"];
            akType.onValueChanged.ClearAll();
            akType.value = (int)beatmapObject.autoKillType;
            akType.onValueChanged.AddListener(_val =>
            {
                beatmapObject.autoKillType = (AutoKillType)_val;
                // AutoKillType affects both physical object and timeline object.
                RenderTimelineObject(GetTimelineObject(beatmapObject));
                if (UpdateObjects)
                    Updater.UpdateObject(beatmapObject, "Autokill");
                ResizeKeyframeTimeline(beatmapObject);
                RenderAutokill(beatmapObject);

            });

            var todValue = (Transform)ObjectUIElements["Autokill TOD Value"];
            var akOffset = todValue.GetComponent<InputField>();
            var akset = (Transform)ObjectUIElements["Autokill TOD Set"];
            var aksetButt = (Button)ObjectUIElements["Autokill TOD Set B"];

            if (beatmapObject.autoKillType == AutoKillType.FixedTime ||
                beatmapObject.autoKillType == AutoKillType.SongTime ||
                beatmapObject.autoKillType == AutoKillType.LastKeyframeOffset)
            {
                todValue.gameObject.SetActive(true);

                akOffset.onValueChanged.ClearAll();
                akOffset.text = beatmapObject.autoKillOffset.ToString();
                akOffset.onValueChanged.AddListener(_val =>
                {
                    if (float.TryParse(_val, out float num))
                    {
                        if (beatmapObject.autoKillType == AutoKillType.SongTime)
                        {
                            float startTime = beatmapObject.StartTime;
                            if (num < startTime)
                                num = startTime + 0.1f;
                        }

                        if (num < 0f)
                            num = 0f;

                        beatmapObject.autoKillOffset = num;

                        // AutoKillType affects both physical object and timeline object.
                        RenderTimelineObject(GetTimelineObject(beatmapObject));
                        if (UpdateObjects)
                            Updater.UpdateObject(beatmapObject, "Autokill");
                        ResizeKeyframeTimeline(beatmapObject);
                    }
                });

                akset.gameObject.SetActive(true);
                aksetButt.onClick.ClearAll();
                aksetButt.onClick.AddListener(() =>
                {
                    float num = 0f;

                    if (beatmapObject.autoKillType == AutoKillType.SongTime)
                        num = AudioManager.inst.CurrentAudioSource.time;
                    else num = AudioManager.inst.CurrentAudioSource.time - beatmapObject.StartTime;

                    if (num < 0f)
                        num = 0f;

                    akOffset.text = num.ToString();
                });

                // Add Scrolling for easy changing of values.
                TriggerHelper.AddEventTriggers(todValue.gameObject, TriggerHelper.ScrollDelta(akOffset, 0.1f, 10f, 0f, float.PositiveInfinity));
            }
            else
            {
                todValue.gameObject.SetActive(false);
                akOffset.onValueChanged.ClearAll();
                akset.gameObject.SetActive(false);
                aksetButt.onClick.ClearAll();
            }

            var collapse = (Toggle)ObjectUIElements["Autokill Collapse"];

            collapse.onValueChanged.ClearAll();
            collapse.isOn = beatmapObject.editorData.collapse;
            collapse.onValueChanged.AddListener(_val =>
            {
                beatmapObject.editorData.collapse = _val;

                // Since autokill collapse has no affect on the physical object, we will only need to update the timeline object.
                RenderTimelineObject(GetTimelineObject(beatmapObject));
            });
        }

        /// <summary>
        /// Renders all Parent UI.
        /// </summary>
        /// <param name="beatmapObject">The Beatmap Object to set.</param>
        public void RenderParent(BeatmapObject beatmapObject)
        {
            string parent = beatmapObject.parent;

            var parentTextText = (Text)ObjectUIElements["Parent Name"];
            var parentText = (Button)ObjectUIElements["Parent Select"];
            var parentMore = (Button)ObjectUIElements["Parent More B"];
            var parent_more = (Transform)ObjectUIElements["Parent More"];
            var parentParent = (Button)ObjectUIElements["Parent Search Open"];
            var parentClear = (Button)ObjectUIElements["Parent Clear"];
            var parentPicker = (Button)ObjectUIElements["Parent Picker"];

            parentText.transform.AsRT().sizeDelta = new Vector2(!string.IsNullOrEmpty(parent) ? 201f : 241f, 32f);

            parentParent.onClick.ClearAll();
            parentParent.onClick.AddListener(EditorManager.inst.OpenParentPopup);

            parentClear.onClick.ClearAll();

            parentPicker.onClick.ClearAll();
            parentPicker.onClick.AddListener(() => { RTEditor.inst.parentPickerEnabled = true; });

            parentClear.gameObject.SetActive(!string.IsNullOrEmpty(parent));

            parent_more.AsRT().sizeDelta = new Vector2(351f, RTEditor.ShowModdedUI ? 152f : 112f);

            if (string.IsNullOrEmpty(parent))
            {
                parentText.interactable = false;
                parentMore.interactable = false;
                parent_more.gameObject.SetActive(false);
                parentTextText.text = "No Parent Object";

                ((HoverTooltip)ObjectUIElements["Parent Info"]).tooltipLangauges[0].hint = string.IsNullOrEmpty(parent) ? "Object not parented." : "No parent found.";
                parentText.onClick.ClearAll();
                parentMore.onClick.ClearAll();

                return;
            }

            string p = null;

            if (GameData.Current.beatmapObjects.TryFindIndex(x => x.id == parent, out int pa))
            {
                p = GameData.Current.beatmapObjects[pa].name;
                ((HoverTooltip)ObjectUIElements["Parent Info"]).tooltipLangauges[0].hint = string.Format("Parent chain count: [{0}]\n(Inclusive)", beatmapObject.GetParentChain().Count);
            }
            else if (parent == "CAMERA_PARENT")
            {
                p = "[CAMERA]";
                ((HoverTooltip)ObjectUIElements["Parent Info"]).tooltipLangauges[0].hint = "Object parented to the camera.";
            }

            parentText.interactable = p != null;
            parentMore.interactable = p != null;

            parent_more.gameObject.SetActive(p != null && ObjEditor.inst.advancedParent);

            parentClear.onClick.AddListener(() =>
            {
                beatmapObject.parent = "";

                // Since parent has no affect on the timeline object, we will only need to update the physical object.
                if (UpdateObjects)
                    Updater.UpdateObject(beatmapObject, "Parent");

                RenderParent(beatmapObject);
            });

            if (p == null)
            {
                parentTextText.text = "No Parent Object";
                ((HoverTooltip)ObjectUIElements["Parent Info"]).tooltipLangauges[0].hint = string.IsNullOrEmpty(parent) ? "Object not parented." : "No parent found.";
                parentText.onClick.ClearAll();
                parentMore.onClick.ClearAll();

                return;
            }

            parentTextText.text = p;

            parentText.onClick.ClearAll();
            parentText.onClick.AddListener(() =>
            {
                if (GameData.Current.beatmapObjects.Find(x => x.id == parent) != null &&
                parent != "CAMERA_PARENT" &&
                RTEditor.inst.timelineObjects.TryFind(x => x.ID == parent, out TimelineObject timelineObject))
                    SetCurrentObject(timelineObject);
                else if (parent == "CAMERA_PARENT")
                {
                    RTEditor.inst.SetLayer(RTEditor.LayerType.Events);
                    EventEditor.inst.SetCurrentEvent(0, CoreHelper.ClosestEventKeyframe(0));
                }
            });

            parentMore.onClick.ClearAll();
            parentMore.onClick.AddListener(() =>
            {
                ObjEditor.inst.advancedParent = !ObjEditor.inst.advancedParent;
                parent_more.gameObject.SetActive(ObjEditor.inst.advancedParent);
            });
            parent_more.gameObject.SetActive(ObjEditor.inst.advancedParent);

            var spawnOnce = (Toggle)ObjectUIElements["Parent Spawn Once"];
            spawnOnce.onValueChanged.ClearAll();
            spawnOnce.gameObject.SetActive(RTEditor.ShowModdedUI);
            if (RTEditor.ShowModdedUI)
            {
                spawnOnce.isOn = beatmapObject.desync;
                spawnOnce.onValueChanged.AddListener(_val =>
                {
                    beatmapObject.desync = _val;
                    Updater.UpdateObject(beatmapObject);
                });
            }

            for (int i = 0; i < 3; i++)
            {
                var _p = (Transform)ObjectUIElements[$"Parent Offset {i + 1}"];

                var parentOffset = beatmapObject.getParentOffset(i);

                var index = i;

                // Parent Type
                var tog = _p.GetChild(2).GetComponent<Toggle>();
                tog.onValueChanged.ClearAll();
                tog.isOn = beatmapObject.GetParentType(i);
                tog.onValueChanged.AddListener(_val =>
                {
                    beatmapObject.SetParentType(index, _val);

                    // Since updating parent type has no affect on the timeline object, we will only need to update the physical object.
                    if (UpdateObjects && !string.IsNullOrEmpty(beatmapObject.parent) && beatmapObject.parent != "CAMERA_PARENT")
                        Updater.UpdateObject(beatmapObject.Parent);
                    else if (UpdateObjects && beatmapObject.parent == "CAMERA_PARENT")
                        Updater.UpdateObject(beatmapObject);
                });

                // Parent Offset
                var pif = _p.GetChild(3).GetComponent<InputField>();
                var lel = _p.GetChild(3).GetComponent<LayoutElement>();
                lel.minWidth = RTEditor.ShowModdedUI ? 64f : 128f;
                lel.preferredWidth = RTEditor.ShowModdedUI ? 64f : 128f;
                pif.onValueChanged.ClearAll();
                pif.text = parentOffset.ToString();
                pif.onValueChanged.AddListener(_val =>
                {
                    if (float.TryParse(_val, out float num))
                    {
                        beatmapObject.SetParentOffset(index, num);

                        // Since updating parent type has no affect on the timeline object, we will only need to update the physical object.
                        if (UpdateObjects && !string.IsNullOrEmpty(beatmapObject.parent) && beatmapObject.parent != "CAMERA_PARENT")
                            Updater.UpdateObject(beatmapObject.Parent);
                        else if (UpdateObjects && beatmapObject.parent == "CAMERA_PARENT")
                            Updater.UpdateObject(beatmapObject);
                    }
                });

                TriggerHelper.AddEventTriggers(pif.gameObject, TriggerHelper.ScrollDelta(pif));

                var additive = _p.GetChild(4).GetComponent<Toggle>();
                additive.onValueChanged.ClearAll();
                additive.gameObject.SetActive(RTEditor.ShowModdedUI);
                var parallax = _p.GetChild(5).GetComponent<InputField>();
                parallax.onValueChanged.ClearAll();
                parallax.gameObject.SetActive(RTEditor.ShowModdedUI);

                if (!RTEditor.ShowModdedUI)
                    continue;

                additive.isOn = beatmapObject.parentAdditive[i] == '1';
                additive.onValueChanged.AddListener(_val =>
                {
                    beatmapObject.SetParentAdditive(index, _val);
                    if (UpdateObjects)
                        Updater.UpdateObject(beatmapObject);
                });
                parallax.text = beatmapObject.parallaxSettings[index].ToString();
                parallax.onValueChanged.AddListener(_val =>
                {
                    if (float.TryParse(_val, out float num))
                    {
                        beatmapObject.parallaxSettings[index] = num;

                            // Since updating parent type has no affect on the timeline object, we will only need to update the physical object.
                            if (UpdateObjects)
                            Updater.UpdateObject(beatmapObject);
                    }
                });

                TriggerHelper.AddEventTriggers(parallax.gameObject, TriggerHelper.ScrollDelta(parallax));
            }
        }

        /// <summary>
        /// Renders the Origin InputFields.
        /// </summary>
        /// <param name="beatmapObject">The Beatmap Object to set.</param>
        public void RenderOrigin(BeatmapObject beatmapObject)
        {
            // Reimplemented origin toggles for Simple Editor Complexity.
            float[] originDefaultPositions = new float[] { 0f, -0.5f, 0f, 0.5f };
            for (int i = 1; i <= 3; i++)
            {
                int index = i;
                var toggle = ObjEditor.inst.ObjectView.transform.Find("origin/origin-x/" + i).GetComponent<Toggle>();
                toggle.onValueChanged.ClearAll();
                toggle.isOn = beatmapObject.origin.x == originDefaultPositions[i];
                toggle.onValueChanged.AddListener(_val =>
                {
                    if (!_val)
                        return;

                    switch (index)
                    {
                        case 1:
                            beatmapObject.origin.x = -0.5f;

                            // Since origin has no affect on the timeline object, we will only need to update the physical object.
                            if (UpdateObjects)
                                Updater.UpdateObject(beatmapObject, "Origin");
                            return;
                        case 2:
                            beatmapObject.origin.x = 0f;

                            // Since origin has no affect on the timeline object, we will only need to update the physical object.
                            if (UpdateObjects)
                                Updater.UpdateObject(beatmapObject, "Origin");
                            return;
                        case 3:
                            beatmapObject.origin.x = 0.5f;

                            // Since origin has no affect on the timeline object, we will only need to update the physical object.
                            if (UpdateObjects)
                                Updater.UpdateObject(beatmapObject, "Origin");
                            break;
                        default:
                            return;
                    }
                });
            }
            for (int i = 1; i <= 3; i++)
            {
                int index = i;
                var toggle = ObjEditor.inst.ObjectView.transform.Find("origin/origin-y/" + i).GetComponent<Toggle>();
                toggle.onValueChanged.ClearAll();
                toggle.isOn = beatmapObject.origin.y == originDefaultPositions[i];
                toggle.onValueChanged.AddListener(_val =>
                {
                    if (!_val)
                        return;

                    switch (index)
                    {
                        case 1:
                            beatmapObject.origin.y = -0.5f;

                            // Since origin has no affect on the timeline object, we will only need to update the physical object.
                            if (UpdateObjects)
                                Updater.UpdateObject(beatmapObject, "Origin");
                            return;
                        case 2:
                            beatmapObject.origin.y = 0f;

                            // Since origin has no affect on the timeline object, we will only need to update the physical object.
                            if (UpdateObjects)
                                Updater.UpdateObject(beatmapObject, "Origin");
                            return;
                        case 3:
                            beatmapObject.origin.y = 0.5f;

                            // Since origin has no affect on the timeline object, we will only need to update the physical object.
                            if (UpdateObjects)
                                Updater.UpdateObject(beatmapObject, "Origin");
                            break;
                        default:
                            return;
                    }
                });
            }

            var oxIF = (InputField)ObjectUIElements["Origin X IF"];

            if (!oxIF.gameObject.GetComponent<InputFieldSwapper>())
            {
                var ifh = oxIF.gameObject.AddComponent<InputFieldSwapper>();
                ifh.Init(oxIF, InputFieldSwapper.Type.Num);
            }

            oxIF.onValueChanged.RemoveAllListeners();
            oxIF.text = beatmapObject.origin.x.ToString();
            oxIF.onValueChanged.AddListener(_val =>
            {
                if (float.TryParse(_val, out float num))
                {
                    beatmapObject.origin.x = num;

                    // Since origin has no affect on the timeline object, we will only need to update the physical object.
                    if (UpdateObjects)
                        Updater.UpdateObject(beatmapObject, "Origin");
                }
            });

            var oyIF = (InputField)ObjectUIElements["Origin Y IF"];

            if (!oyIF.gameObject.GetComponent<InputFieldSwapper>())
            {
                var ifh = oyIF.gameObject.AddComponent<InputFieldSwapper>();
                ifh.Init(oyIF, InputFieldSwapper.Type.Num);
            }

            oyIF.onValueChanged.RemoveAllListeners();
            oyIF.text = beatmapObject.origin.y.ToString();
            oyIF.onValueChanged.AddListener(_val =>
            {
                if (float.TryParse(_val, out float num))
                {
                    beatmapObject.origin.y = num;

                    // Since origin has no affect on the timeline object, we will only need to update the physical object.
                    if (UpdateObjects)
                        Updater.UpdateObject(beatmapObject, "Origin");
                }
            });

            TriggerHelper.IncreaseDecreaseButtons(oxIF, 0.1f, 10f);
            TriggerHelper.IncreaseDecreaseButtons(oyIF, 0.1f, 10f);

            TriggerHelper.AddEventTriggers(oxIF.gameObject, TriggerHelper.ScrollDelta(oxIF, multi: true), TriggerHelper.ScrollDeltaVector2(oxIF, oyIF, 0.1f, 10f));
            TriggerHelper.AddEventTriggers(oyIF.gameObject, TriggerHelper.ScrollDelta(oyIF, multi: true), TriggerHelper.ScrollDeltaVector2(oxIF, oyIF, 0.1f, 10f));
        }

        public void RenderGradient(BeatmapObject beatmapObject)
        {
            var gradient = (Transform)ObjectUIElements["Gradient"];
            for (int i = 0; i < gradient.childCount; i++)
            {
                var index = i;
                var toggle = gradient.GetChild(i).GetComponent<Toggle>();
                toggle.onValueChanged.ClearAll();
                toggle.isOn = index == (int)beatmapObject.gradientType;
                toggle.onValueChanged.AddListener(_val =>
                {
                    beatmapObject.gradientType = (BeatmapObject.GradientType)index;

                    if (beatmapObject.gradientType != BeatmapObject.GradientType.Normal && (beatmapObject.shape == 4 || beatmapObject.shape == 6 || beatmapObject.shape == 10))
                    {
                        beatmapObject.shape = 0;
                        beatmapObject.shapeOption = 0;
                        RenderShape(beatmapObject);
                    }

                    if (!RTEditor.ShowModdedUI)
                    {
                        for (int i = 0; i < beatmapObject.events[3].Count; i++)
                            beatmapObject.events[3][i].eventValues[6] = 10f;
                    }

                    // Since shape has no affect on the timeline object, we will only need to update the physical object.
                    if (UpdateObjects)
                        Updater.UpdateObject(beatmapObject);

                    RenderGradient(beatmapObject);
                    inst.RenderObjectKeyframesDialog(beatmapObject);
                });
            }
        }

        /// <summary>
        /// Ensures a toggle list ends with a non-toggle game object.
        /// </summary>
        /// <param name="parent">The parent for the end non-toggle.</param>
        public void LastGameObject(Transform parent)
        {
            var gameObject = new GameObject("GameObject");
            gameObject.transform.SetParent(parent);
            gameObject.transform.localScale = Vector3.one;

            var rectTransform = gameObject.AddComponent<RectTransform>();

            rectTransform.anchorMax = new Vector2(0f, 0f);
            rectTransform.anchorMin = new Vector2(0f, 0f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.sizeDelta = new Vector2(0f, 32f);

            var layoutElement = gameObject.AddComponent<LayoutElement>();
            layoutElement.layoutPriority = 1;
            layoutElement.preferredWidth = 1000f;
        }

        bool updatedShapes = false;
        public List<Toggle> shapeToggles = new List<Toggle>();
        public List<List<Toggle>> shapeOptionToggles = new List<List<Toggle>>();
        public static int[] UnmoddedShapeCounts => new int[]
        {
            3,
            9,
            4,
            2,
            1,
            6
        };

        /// <summary>
        /// Renders the Shape ToggleGroup.
        /// </summary>
        /// <param name="beatmapObject">The BeatmapObject to set.</param>
        public void RenderShape(BeatmapObject beatmapObject)
        {
            var shape = (Transform)ObjectUIElements["Shape"];
            var shapeSettings = (Transform)ObjectUIElements["Shape Settings"];

            var shapeGLG = shape.GetComponent<GridLayoutGroup>();
            shapeGLG.constraint = GridLayoutGroup.Constraint.FixedRowCount;
            shapeGLG.constraintCount = 1;
            shapeGLG.spacing = new Vector2(7.6f, 0f);

            if (!updatedShapes)
            {
                // Initial removing
                DestroyImmediate(shape.GetComponent<ToggleGroup>());

                var toDestroy = new List<GameObject>();

                for (int i = 0; i < shape.childCount; i++)
                {
                    toDestroy.Add(shape.GetChild(i).gameObject);
                }

                for (int i = 0; i < shapeSettings.childCount; i++)
                {
                    if (i != 4 && i != 6)
                        for (int j = 0; j < shapeSettings.GetChild(i).childCount; j++)
                        {
                            toDestroy.Add(shapeSettings.GetChild(i).GetChild(j).gameObject);
                        }
                }

                foreach (var obj in toDestroy)
                    DestroyImmediate(obj);

                toDestroy = null;

                for (int i = 0; i < ShapeManager.inst.Shapes2D.Count; i++)
                {
                    var obj = shapeButtonPrefab.Duplicate(shape, (i + 1).ToString(), i);
                    if (obj.transform.Find("Image") && obj.transform.Find("Image").gameObject.TryGetComponent(out Image image))
                    {
                        image.sprite = ShapeManager.inst.Shapes2D[i][0].Icon;
                        EditorThemeManager.ApplyGraphic(image, ThemeGroup.Toggle_1_Check);
                    }

                    if (!obj.GetComponent<HoverUI>())
                    {
                        var hoverUI = obj.AddComponent<HoverUI>();
                        hoverUI.animatePos = false;
                        hoverUI.animateSca = true;
                        hoverUI.size = 1.1f;
                    }

                    var shapeToggle = obj.GetComponent<Toggle>();
                    EditorThemeManager.ApplyToggle(shapeToggle, ThemeGroup.Background_1);

                    shapeToggles.Add(shapeToggle);

                    shapeOptionToggles.Add(new List<Toggle>());

                    if (i != 4 && i != 6)
                    {
                        if (!shapeSettings.Find((i + 1).ToString()))
                        {
                            var sh = shapeSettings.Find("6").gameObject.Duplicate(shapeSettings, (i + 1).ToString());
                            LSHelpers.DeleteChildren(sh.transform, true);

                            var d = new List<GameObject>();
                            for (int j = 0; j < sh.transform.childCount; j++)
                            {
                                d.Add(sh.transform.GetChild(j).gameObject);
                            }
                            foreach (var go in d)
                                DestroyImmediate(go);
                            d.Clear();
                            d = null;
                        }

                        var so = shapeSettings.Find((i + 1).ToString());

                        var rect = (RectTransform)so;
                        if (!so.GetComponent<ScrollRect>())
                        {
                            var scroll = so.gameObject.AddComponent<ScrollRect>();
                            so.gameObject.AddComponent<Mask>();
                            var ad = so.gameObject.AddComponent<Image>();

                            scroll.horizontal = true;
                            scroll.vertical = false;
                            scroll.content = rect;
                            scroll.viewport = rect;
                            ad.color = new Color(1f, 1f, 1f, 0.01f);
                        }

                        for (int j = 0; j < ShapeManager.inst.Shapes2D[i].Count; j++)
                        {
                            var opt = shapeButtonPrefab.Duplicate(shapeSettings.GetChild(i), (j + 1).ToString(), j);
                            if (opt.transform.Find("Image") && opt.transform.Find("Image").gameObject.TryGetComponent(out Image image1))
                            {
                                image1.sprite = ShapeManager.inst.Shapes2D[i][j].Icon;
                                EditorThemeManager.ApplyGraphic(image1, ThemeGroup.Toggle_1_Check);
                            }

                            if (!opt.GetComponent<HoverUI>())
                            {
                                var hoverUI = opt.AddComponent<HoverUI>();
                                hoverUI.animatePos = false;
                                hoverUI.animateSca = true;
                                hoverUI.size = 1.1f;
                            }

                            var shapeOptionToggle = opt.GetComponent<Toggle>();
                            EditorThemeManager.ApplyToggle(shapeOptionToggle, ThemeGroup.Background_1);

                            shapeOptionToggles[i].Add(shapeOptionToggle);

                            var layoutElement = opt.AddComponent<LayoutElement>();
                            layoutElement.layoutPriority = 1;
                            layoutElement.minWidth = 32f;

                            ((RectTransform)opt.transform).sizeDelta = new Vector2(32f, 32f);

                            if (!opt.GetComponent<HoverUI>())
                            {
                                var he = opt.AddComponent<HoverUI>();
                                he.animatePos = false;
                                he.animateSca = true;
                                he.size = 1.1f;
                            }
                        }

                        LastGameObject(shapeSettings.GetChild(i));
                    }
                }

                if (ObjectManager.inst.objectPrefabs.Count > 9)
                {
                    var playerSprite = SpriteHelper.LoadSprite(RTFile.ApplicationDirectory + "BepInEx/plugins/Assets/editor_gui_player.png");
                    int i = shape.childCount;
                    var obj = shapeButtonPrefab.Duplicate(shape, (i + 1).ToString());
                    if (obj.transform.Find("Image") && obj.transform.Find("Image").gameObject.TryGetComponent(out Image image))
                    {
                        image.sprite = playerSprite;
                        EditorThemeManager.ApplyGraphic(image, ThemeGroup.Toggle_1_Check);
                    }

                    var so = shapeSettings.Find((i + 1).ToString());

                    if (!so)
                    {
                        so = shapeSettings.Find("6").gameObject.Duplicate(shapeSettings, (i + 1).ToString()).transform;
                        LSHelpers.DeleteChildren(so, true);

                        var d = new List<GameObject>();
                        for (int j = 0; j < so.transform.childCount; j++)
                        {
                            d.Add(so.transform.GetChild(j).gameObject);
                        }
                        foreach (var go in d)
                            DestroyImmediate(go);
                        d.Clear();
                        d = null;
                    }

                    var rect = (RectTransform)so;
                    if (!so.GetComponent<ScrollRect>())
                    {
                        var scroll = so.gameObject.AddComponent<ScrollRect>();
                        so.gameObject.AddComponent<Mask>();
                        var ad = so.gameObject.AddComponent<Image>();

                        scroll.horizontal = true;
                        scroll.vertical = false;
                        scroll.content = rect;
                        scroll.viewport = rect;
                        ad.color = new Color(1f, 1f, 1f, 0.01f);
                    }

                    var shapeToggle = obj.GetComponent<Toggle>();
                    shapeToggles.Add(shapeToggle);
                    EditorThemeManager.ApplyToggle(shapeToggle, ThemeGroup.Background_1);

                    shapeOptionToggles.Add(new List<Toggle>());

                    for (int j = 0; j < ObjectManager.inst.objectPrefabs[9].options.Count; j++)
                    {
                        var opt = shapeButtonPrefab.Duplicate(shapeSettings.GetChild(i), (j + 1).ToString(), j);
                        if (opt.transform.Find("Image") && opt.transform.Find("Image").gameObject.TryGetComponent(out Image image1))
                        {
                            image1.sprite = playerSprite;
                            EditorThemeManager.ApplyGraphic(image1, ThemeGroup.Toggle_1_Check);
                        }

                        var shapeOptionToggle = opt.GetComponent<Toggle>();
                        EditorThemeManager.ApplyToggle(shapeOptionToggle, ThemeGroup.Background_1);

                        shapeOptionToggles[i].Add(shapeOptionToggle);

                        var layoutElement = opt.AddComponent<LayoutElement>();
                        layoutElement.layoutPriority = 1;
                        layoutElement.minWidth = 32f;

                        ((RectTransform)opt.transform).sizeDelta = new Vector2(32f, 32f);

                        if (!opt.GetComponent<HoverUI>())
                        {
                            var he = opt.AddComponent<HoverUI>();
                            he.animatePos = false;
                            he.animateSca = true;
                            he.size = 1.1f;
                        }
                    }

                    LastGameObject(shapeSettings.GetChild(i));
                }

                updatedShapes = true;
            }

            LSHelpers.SetActiveChildren(shapeSettings, false);

            if (beatmapObject.shape >= shapeSettings.childCount)
            {
                Debug.Log($"{ObjEditor.inst.className}Somehow, the object ended up being at a higher shape than normal.");
                beatmapObject.shape = shapeSettings.childCount - 1;
                // Since shape has no affect on the timeline object, we will only need to update the physical object.
                if (UpdateObjects)
                    Updater.UpdateObject(beatmapObject, "Shape");

                RenderShape(beatmapObject);
            }

            if (beatmapObject.shape == 4)
            {
                shapeSettings.AsRT().sizeDelta = new Vector2(351f, 74f);
                var child = shapeSettings.GetChild(4);
                child.AsRT().sizeDelta = new Vector2(351f, 74f);
                child.Find("Text").GetComponent<Text>().alignment = TextAnchor.UpperLeft;
                child.Find("Placeholder").GetComponent<Text>().alignment = TextAnchor.UpperLeft;
                child.GetComponent<InputField>().lineType = InputField.LineType.MultiLineNewline;
            }
            else
            {
                shapeSettings.AsRT().sizeDelta = new Vector2(351f, 32f);
                shapeSettings.GetChild(4).AsRT().sizeDelta = new Vector2(351f, 32f);
            }

            shapeSettings.GetChild(beatmapObject.shape).gameObject.SetActive(true);

            int num = 0;
            foreach (var toggle in shapeToggles)
            {
                int index = num;
                toggle.onValueChanged.ClearAll();
                toggle.isOn = beatmapObject.shape == index;
                toggle.gameObject.SetActive(RTEditor.ShowModdedUI || index < UnmoddedShapeCounts.Length);

                if (RTEditor.ShowModdedUI || index < UnmoddedShapeCounts.Length)
                    toggle.onValueChanged.AddListener(_val =>
                    {
                        beatmapObject.shape = index;
                        beatmapObject.shapeOption = 0;
                        //beatmapObject.text = "";

                        if (beatmapObject.gradientType != BeatmapObject.GradientType.Normal && (index == 4 || index == 6 || index == 10))
                        {
                            beatmapObject.shape = 0;
                        }

                        // Since shape has no affect on the timeline object, we will only need to update the physical object.
                        if (UpdateObjects)
                            Updater.UpdateObject(beatmapObject, "Shape");

                        RenderShape(beatmapObject);
                    });


                num++;
            }

            if (beatmapObject.shape != 4 && beatmapObject.shape != 6)
            {
                num = 0;
                foreach (var toggle in shapeOptionToggles[beatmapObject.shape])
                {
                    int index = num;
                    toggle.onValueChanged.ClearAll();
                    toggle.isOn = beatmapObject.shapeOption == index;
                    toggle.gameObject.SetActive(RTEditor.ShowModdedUI || index < UnmoddedShapeCounts[beatmapObject.shape]);

                    if (RTEditor.ShowModdedUI || index < UnmoddedShapeCounts[beatmapObject.shape])
                        toggle.onValueChanged.AddListener(_val =>
                        {
                            beatmapObject.shapeOption = index;

                            // Since shape has no affect on the timeline object, we will only need to update the physical object.
                            if (UpdateObjects)
                                Updater.UpdateObject(beatmapObject, "Shape");

                            RenderShape(beatmapObject);
                        });

                    num++;
                }
            }
            else if (beatmapObject.shape == 4)
            {
                var textIF = shapeSettings.Find("5").GetComponent<InputField>();
                textIF.onValueChanged.ClearAll();
                textIF.text = beatmapObject.text;
                textIF.onValueChanged.AddListener(_val =>
                {
                    beatmapObject.text = _val;

                    // Since text has no affect on the timeline object, we will only need to update the physical object.
                    if (UpdateObjects)
                        Updater.UpdateObject(beatmapObject, "Shape");
                });

                if (!textIF.transform.Find("edit"))
                {
                    var button = EditorPrefabHolder.Instance.DeleteButton.Duplicate(textIF.transform, "edit");
                    var buttonStorage = button.GetComponent<DeleteButtonStorage>();
                    buttonStorage.image.sprite = KeybindManager.inst.editSprite;
                    EditorThemeManager.ApplySelectable(buttonStorage.button, ThemeGroup.Function_2);
                    EditorThemeManager.ApplyGraphic(buttonStorage.image, ThemeGroup.Function_2_Text);
                    buttonStorage.button.onClick.ClearAll();
                    buttonStorage.button.onClick.AddListener(() => { TextEditor.inst.SetInputField(textIF); });
                    UIManager.SetRectTransform(buttonStorage.baseImage.rectTransform, new Vector2(160f, 24f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(22f, 22f));
                    EditorHelper.SetComplexity(button, Complexity.Advanced);
                }
            }
            else if (beatmapObject.shape == 6)
            {
                var select = shapeSettings.Find("7/select").GetComponent<Button>();
                select.onClick.ClearAll();
                select.onClick.AddListener(() => { OpenImageSelector(beatmapObject); });
                shapeSettings.Find("7/text").GetComponent<Text>().text = string.IsNullOrEmpty(beatmapObject.text) ? "No image selected" : beatmapObject.text;

                // Sets Image Data for transfering of Image Objects between levels.
                var dataText = shapeSettings.Find("7/set/Text").GetComponent<Text>();
                dataText.text = !AssetManager.SpriteAssets.ContainsKey(beatmapObject.text) ? "Set Data" : "Clear Data";
                var set = shapeSettings.Find("7/set").GetComponent<Button>();
                set.onClick.ClearAll();
                set.onClick.AddListener(() =>
                {
                    var assetExists = AssetManager.SpriteAssets.ContainsKey(beatmapObject.text);
                    if (!assetExists)
                    {
                        var regex = new Regex(@"img\((.*?)\)");
                        var match = regex.Match(beatmapObject.text);

                        var path = match.Success ? RTFile.BasePath + match.Groups[1].ToString() : RTFile.BasePath + beatmapObject.text;

                        if (RTFile.FileExists(path))
                        {
                            var imageData = File.ReadAllBytes(path);

                            var texture2d = new Texture2D(2, 2, TextureFormat.ARGB32, false);
                            texture2d.LoadImage(imageData);

                            texture2d.wrapMode = TextureWrapMode.Clamp;
                            texture2d.filterMode = FilterMode.Point;
                            texture2d.Apply();

                            AssetManager.SpriteAssets.Add(beatmapObject.text, SpriteHelper.CreateSprite(texture2d));
                        }
                        else
                        {
                            var imageData = ArcadeManager.inst.defaultImage.texture.EncodeToPNG();

                            var texture2d = new Texture2D(2, 2, TextureFormat.ARGB32, false);
                            texture2d.LoadImage(imageData);

                            texture2d.wrapMode = TextureWrapMode.Clamp;
                            texture2d.filterMode = FilterMode.Point;
                            texture2d.Apply();

                            AssetManager.SpriteAssets.Add(beatmapObject.text, SpriteHelper.CreateSprite(texture2d));
                        }

                        Updater.UpdateObject(beatmapObject);
                    }
                    else
                    {
                        AssetManager.SpriteAssets.Remove(beatmapObject.text);

                        Updater.UpdateObject(beatmapObject);
                    }

                    dataText.text = !assetExists ? "Set Data" : "Clear Data";
                });
            }
        }

        public void SetDepthSlider(BeatmapObject beatmapObject, float _value, InputField inputField, Slider slider)
        {
            var num = (int)_value;

            beatmapObject.Depth = num;

            slider.onValueChanged.RemoveAllListeners();
            slider.value = num;
            slider.onValueChanged.AddListener(_val => { SetDepthInputField(beatmapObject, ((int)_val).ToString(), inputField, slider); });

            // Since depth has no affect on the timeline object, we will only need to update the physical object.
            if (UpdateObjects)
                Updater.UpdateObject(beatmapObject, "Depth");
        }

        public void SetDepthInputField(BeatmapObject beatmapObject, string _value, InputField inputField, Slider slider)
        {
            var num = int.Parse(_value);

            beatmapObject.Depth = num;

            inputField.onValueChanged.RemoveAllListeners();
            inputField.text = num.ToString();
            inputField.onValueChanged.AddListener(_val =>
            {
                if (int.TryParse(_val, out int numb))
                    SetDepthSlider(beatmapObject, numb, inputField, slider);
            });

            // Since depth has no affect on the timeline object, we will only need to update the physical object.
            if (UpdateObjects)
                Updater.UpdateObject(beatmapObject, "Depth");
        }

        /// <summary>
        /// Renders the Depth InputField and Slider.
        /// </summary>
        /// <param name="beatmapObject">The BeatmapObject to set.</param>
        public void RenderDepth(BeatmapObject beatmapObject)
        {
            var depthSlider = (Slider)ObjectUIElements["Depth Slider"];
            var depthText = (InputField)ObjectUIElements["Depth IF"];

            if (!depthText.GetComponent<InputFieldSwapper>())
            {
                var ifh = depthText.gameObject.AddComponent<InputFieldSwapper>();
                ifh.Init(depthText, InputFieldSwapper.Type.Num);
            }

            depthText.onValueChanged.ClearAll();
            depthText.text = beatmapObject.Depth.ToString();

            depthText.onValueChanged.AddListener(_val =>
            {
                if (int.TryParse(_val, out int num))
                    SetDepthSlider(beatmapObject, num, depthText, depthSlider);
            });

            depthSlider.maxValue = EditorConfig.Instance.RenderDepthRange.Value.x;
            depthSlider.minValue = EditorConfig.Instance.RenderDepthRange.Value.y;

            depthSlider.onValueChanged.ClearAll();
            depthSlider.value = beatmapObject.Depth;
            depthSlider.onValueChanged.AddListener(_val => { SetDepthInputField(beatmapObject, _val.ToString(), depthText, depthSlider); });

            TriggerHelper.IncreaseDecreaseButtonsInt(depthText, -1);
            TriggerHelper.AddEventTriggers(depthText.gameObject, TriggerHelper.ScrollDeltaInt(depthText, 1));

            TriggerHelper.IncreaseDecreaseButtonsInt(depthText, -1, t: ObjEditor.inst.ObjectView.transform.Find("depth"));

            var renderType = (Dropdown)ObjectUIElements["Render Type"];
            renderType.onValueChanged.ClearAll();
            renderType.value = beatmapObject.background ? 1 : 0;
            renderType.onValueChanged.AddListener(_val =>
            {
                beatmapObject.background = _val == 1;
                if (Updater.TryGetObject(beatmapObject, out LevelObject levelObject) && levelObject.visualObject != null && levelObject.visualObject.GameObject)
                    levelObject.visualObject.GameObject.layer = beatmapObject.background ? 9 : 8;
            });
        }

        /// <summary>
        /// Creates and Renders the UnityExplorer GameObject Inspector.
        /// </summary>
        /// <param name="beatmapObject">The BeatmapObject to get.</param>
        public void RenderGameObjectInspector(BeatmapObject beatmapObject)
        {
            if (!ModCompatibility.UnityExplorerInstalled)
                return;

            var tfv = ObjEditor.inst.ObjectView.transform;

            var inspector = AccessTools.TypeByName("UnityExplorer.InspectorManager");
            var uiManager = AccessTools.TypeByName("UnityExplorer.UI.UIManager");

            if (inspector != null && !tfv.Find("inspect"))
            {
                var label = tfv.ChildList().First(x => x.name == "label").gameObject.Duplicate(tfv, "unity explorer label");
                var index = tfv.Find("editor").GetSiblingIndex() + 1;
                label.transform.SetSiblingIndex(index);

                Destroy(label.transform.GetChild(1).gameObject);
                var labelText = label.transform.GetChild(0).GetComponent<Text>();
                labelText.text = "Unity Explorer";
                EditorThemeManager.AddLightText(labelText);

                var inspect = EditorPrefabHolder.Instance.Function2Button.Duplicate(tfv);
                inspect.SetActive(true);
                inspect.transform.SetSiblingIndex(index + 1);
                inspect.name = "inspectbeatmapobject";

                var inspectText = inspect.transform.GetChild(0).GetComponent<Text>();
                inspectText.text = "Inspect BeatmapObject";

                var inspectGameObject = EditorPrefabHolder.Instance.Function2Button.Duplicate(tfv);
                inspectGameObject.SetActive(true);
                inspectGameObject.transform.SetSiblingIndex(index + 2);
                inspectGameObject.name = "inspect";

                var inspectGameObjectText = inspectGameObject.transform.GetChild(0).GetComponent<Text>();
                inspectGameObjectText.text = "Inspect LevelObject";

                var inspectButton = inspect.GetComponent<Button>();
                var inspectGameObjectButton = inspectGameObject.GetComponent<Button>();

                Destroy(inspect.GetComponent<Animator>());
                inspectButton.transition = Selectable.Transition.ColorTint;
                EditorThemeManager.AddSelectable(inspectButton, ThemeGroup.Function_2);
                EditorThemeManager.AddGraphic(inspectText, ThemeGroup.Function_2_Text);

                Destroy(inspectGameObject.GetComponent<Animator>());
                inspectGameObjectButton.transition = Selectable.Transition.ColorTint;
                EditorThemeManager.AddSelectable(inspectGameObjectButton, ThemeGroup.Function_2);
                EditorThemeManager.AddGraphic(inspectGameObjectText, ThemeGroup.Function_2_Text);
            }

            if (tfv.TryFind("unity explorer label", out Transform unityExplorerLabel))
                unityExplorerLabel.gameObject.SetActive(RTEditor.ShowModdedUI);

            if (tfv.Find("inspect"))
            {
                bool active = Updater.TryGetObject(beatmapObject, out LevelObject levelObject) && RTEditor.ShowModdedUI;
                tfv.Find("inspect").gameObject.SetActive(active);
                var inspectButton = tfv.Find("inspect").GetComponent<Button>();
                inspectButton.onClick.ClearAll();
                if (active)
                    inspectButton.onClick.AddListener(() => { ModCompatibility.Inspect(levelObject); });
            }

            if (tfv.Find("inspectbeatmapobject"))
            {
                var inspectButton = tfv.Find("inspectbeatmapobject").GetComponent<Button>();
                inspectButton.gameObject.SetActive(RTEditor.ShowModdedUI);
                inspectButton.onClick.ClearAll();
                if (RTEditor.ShowModdedUI)
                    inspectButton.onClick.AddListener(() => { ModCompatibility.Inspect(beatmapObject); });
            }
        }

        /// <summary>
        /// Renders the Layers InputField.
        /// </summary>
        /// <param name="beatmapObject">The BeatmapObject to set.</param>
        public void RenderLayers(BeatmapObject beatmapObject)
        {
            var editorLayersIF = (InputField)ObjectUIElements["Layers IF"];
            var editorLayersImage = (Image)ObjectUIElements["Layers Image"];

            editorLayersIF.onValueChanged.ClearAll();
            editorLayersIF.text = (beatmapObject.editorData.layer + 1).ToString();
            editorLayersImage.color = RTEditor.GetLayerColor(beatmapObject.editorData.layer);
            editorLayersIF.onValueChanged.AddListener(_val =>
            {
                if (int.TryParse(_val, out int num))
                {
                    num = Mathf.Clamp(num - 1, 0, int.MaxValue);
                    beatmapObject.editorData.layer = num;

                    // Since layers have no effect on the physical object, we will only need to update the timeline object.
                    RenderTimelineObject(GetTimelineObject(beatmapObject));

                    //editorLayersImage.color = RTEditor.GetLayerColor(beatmapObject.editorData.Layer);
                    RenderLayers(beatmapObject);
                }
            });

            if (editorLayersIF.gameObject)
                TriggerHelper.AddEventTriggers(editorLayersIF.gameObject, TriggerHelper.ScrollDeltaInt(editorLayersIF, 1, 1, int.MaxValue));
        }

        /// <summary>
        /// Renders the Bin Slider.
        /// </summary>
        /// <param name="beatmapObject">The BeatmapObject to set.</param>
        public void RenderBin(BeatmapObject beatmapObject)
        {
            var editorBin = (Slider)ObjectUIElements["Bin Slider"];
            editorBin.onValueChanged.ClearAll();
            editorBin.value = beatmapObject.editorData.Bin;
            editorBin.onValueChanged.AddListener(_val =>
            {
                beatmapObject.editorData.Bin = Mathf.Clamp((int)_val, 0, 14);

                // Since bin has no effect on the physical object, we will only need to update the timeline object.
                RenderTimelineObject(GetTimelineObject(beatmapObject));
            });
        }

        void KeyframeHandler(Transform kfdialog, int type, IEnumerable<TimelineObject> selected, TimelineObject firstKF, BeatmapObject beatmapObject, string typeName, int i, string valueType)
        {
            var valueBase = kfdialog.Find(typeName);
            var value = valueBase.Find(valueType);

            if (!value)
            {
                CoreHelper.LogError($"Value {valueType} is null.");
                return;
            }

            var valueEventTrigger = typeName != "rotation" ? value.GetComponent<EventTrigger>() : kfdialog.GetChild(9).GetComponent<EventTrigger>();

            var valueInputField = value.GetComponent<InputField>();
            var valueButtonLeft = value.Find("<").GetComponent<Button>();
            var valueButtonRight = value.Find(">").GetComponent<Button>();

            if (!value.GetComponent<InputFieldSwapper>())
            {
                var ifh = value.gameObject.AddComponent<InputFieldSwapper>();
                ifh.Init(valueInputField, InputFieldSwapper.Type.Num);
            }

            valueEventTrigger.triggers.Clear();

            switch (type)
            {
                case 0:
                    {
                        valueEventTrigger.triggers.Add(TriggerHelper.ScrollDelta(valueInputField, EditorConfig.Instance.ObjectPositionScroll.Value, EditorConfig.Instance.ObjectPositionScrollMultiply.Value, multi: true));
                        valueEventTrigger.triggers.Add(TriggerHelper.ScrollDeltaVector2(kfdialog.GetChild(9).GetChild(0).GetComponent<InputField>(), kfdialog.GetChild(9).GetChild(1).GetComponent<InputField>(), EditorConfig.Instance.ObjectPositionScroll.Value, EditorConfig.Instance.ObjectPositionScrollMultiply.Value));
                        break;
                    }
                case 1:
                    {
                        valueEventTrigger.triggers.Add(TriggerHelper.ScrollDelta(valueInputField, EditorConfig.Instance.ObjectScaleScroll.Value, EditorConfig.Instance.ObjectScaleScrollMultiply.Value, multi: true));
                        valueEventTrigger.triggers.Add(TriggerHelper.ScrollDeltaVector2(kfdialog.GetChild(9).GetChild(0).GetComponent<InputField>(), kfdialog.GetChild(9).GetChild(1).GetComponent<InputField>(), EditorConfig.Instance.ObjectScaleScroll.Value, EditorConfig.Instance.ObjectScaleScrollMultiply.Value));
                        break;
                    }
                case 2:
                    {
                        valueEventTrigger.triggers.Add(TriggerHelper.ScrollDelta(valueInputField, EditorConfig.Instance.ObjectRotationScroll.Value, EditorConfig.Instance.ObjectRotationScrollMultiply.Value));
                        break;
                    }
            }

            int current = i;

            valueInputField.characterValidation = InputField.CharacterValidation.None;
            valueInputField.contentType = InputField.ContentType.Standard;
            valueInputField.keyboardType = TouchScreenKeyboardType.Default;

            valueInputField.onEndEdit.ClearAll();
            valueInputField.onValueChanged.ClearAll();
            valueInputField.text = selected.Count() == 1 ? firstKF.GetData<EventKeyframe>().eventValues[i].ToString() : typeName == "rotation" ? "15" : "1";
            valueInputField.onValueChanged.AddListener(_val =>
            {
                if (float.TryParse(_val, out float num) && selected.Count() == 1)
                {

                    firstKF.GetData<EventKeyframe>().eventValues[current] = num;

                    // Since keyframe value has no affect on the timeline object, we will only need to update the physical object.
                    if (UpdateObjects)
                        Updater.UpdateObject(beatmapObject, "Keyframes");
                }
            });
            valueInputField.onEndEdit.AddListener(_val =>
            {
                if (!float.TryParse(_val, out float n) && RTMath.TryEvaluate(_val.Replace("eventTime", firstKF.GetData<EventKeyframe>().eventTime.ToString()), firstKF.GetData<EventKeyframe>().eventValues[current], out float calc))
                    valueInputField.text = calc.ToString();
            });

            valueButtonLeft.onClick.ClearAll();
            valueButtonLeft.onClick.AddListener(() =>
            {
                if (float.TryParse(valueInputField.text, out float x))
                {
                    if (selected.Count() == 1)
                    {
                        valueInputField.text = (x - (typeName == "rotation" ? 5f : 1f)).ToString();
                        return;
                    }

                    foreach (var keyframe in selected)
                        keyframe.GetData<EventKeyframe>().eventValues[current] -= x;

                    // Since keyframe value has no affect on the timeline object, we will only need to update the physical object.
                    if (UpdateObjects)
                        Updater.UpdateObject(beatmapObject, "Keyframes");
                }
            });

            valueButtonRight.onClick.ClearAll();
            valueButtonRight.onClick.AddListener(() =>
            {
                if (float.TryParse(valueInputField.text, out float x))
                {
                    if (selected.Count() == 1)
                    {
                        valueInputField.text = (x + (typeName == "rotation" ? 5f : 1f)).ToString();
                        return;
                    }

                    foreach (var keyframe in selected)
                        keyframe.GetData<EventKeyframe>().eventValues[current] += x;

                    // Since keyframe value has no affect on the timeline object, we will only need to update the physical object.
                    if (UpdateObjects)
                        Updater.UpdateObject(beatmapObject, "Keyframes");
                }
            });
        }

        void UpdateKeyframeRandomDialog(Transform kfdialog, Transform randomValueLabel, Transform randomValue, int type,
            IEnumerable<TimelineObject> selected, TimelineObject firstKF, BeatmapObject beatmapObject, string typeName, int randomType)
        {
            if (kfdialog.Find("r_axis"))
                kfdialog.Find("r_axis").gameObject.SetActive(RTEditor.ShowModdedUI && (randomType == 5 || randomType == 6));

            randomValueLabel.gameObject.SetActive(randomType != 0 && randomType != 5);
            randomValue.gameObject.SetActive(randomType != 0 && randomType != 5);
            randomValueLabel.GetChild(0).GetComponent<Text>().text = (randomType == 4) ? "Random Scale Min" : randomType == 6 ? "Minimum Range" : "Random X";
            randomValueLabel.GetChild(1).gameObject.SetActive(type != 2 || randomType == 6);
            randomValueLabel.GetChild(1).GetComponent<Text>().text = (randomType == 4) ? "Random Scale Max" : randomType == 6 ? "Maximum Range" : "Random Y";
            kfdialog.Find("random/interval-input").gameObject.SetActive(randomType != 0 && randomType != 3 && randomType != 5);
            kfdialog.Find("r_label/interval").gameObject.SetActive(randomType != 0 && randomType != 3 && randomType != 5);

            if (kfdialog.Find("relative-label"))
            {
                kfdialog.Find("relative-label").gameObject.SetActive(RTEditor.ShowModdedUI);
                if (RTEditor.ShowModdedUI)
                {
                    kfdialog.Find("relative-label").GetChild(0).GetComponent<Text>().text =
                        randomType == 6 && type != 2 ? "Object Flees from Player" : randomType == 6 ? "Object Turns Away from Player" : "Value Additive";
                    kfdialog.Find("relative").GetChild(1).GetComponent<Text>().text =
                        randomType == 6 && type != 2 ? "Flee" : randomType == 6 ? "Turn Away" : "Relative";
                }
            }

            randomValue.GetChild(1).gameObject.SetActive(type != 2 || randomType == 6);

            randomValue.GetChild(0).GetChild(0).AsRT().sizeDelta = new Vector2(type != 2 || randomType == 6 ? 117 : 317f, 32f);
            randomValue.GetChild(1).GetChild(0).AsRT().sizeDelta = new Vector2(type != 2 || randomType == 6 ? 117 : 317f, 32f);

            if (randomType != 0 && randomType != 3 && randomType != 5)
                kfdialog.Find("r_label/interval").GetComponent<Text>().text = randomType == 6 ? "Speed" : "Random Interval";
        }

        void KeyframeRandomHandler(Transform kfdialog, int type, IEnumerable<TimelineObject> selected, TimelineObject firstKF, BeatmapObject beatmapObject, string typeName)
        {
            var randomValueLabel = kfdialog.Find($"r_{typeName}_label");
            var randomValue = kfdialog.Find($"r_{typeName}");

            int random = firstKF.GetData<EventKeyframe>().random;

            if (kfdialog.Find("r_axis") && kfdialog.Find("r_axis").gameObject.TryGetComponent(out Dropdown rAxis))
            {
                rAxis.gameObject.SetActive(random == 5 || random == 6);
                rAxis.onValueChanged.ClearAll();
                rAxis.value = Mathf.Clamp((int)firstKF.GetData<EventKeyframe>().eventRandomValues[3], 0, 3);
                rAxis.onValueChanged.AddListener(_val =>
                {
                    foreach (var keyframe in selected.Select(x => x.GetData<EventKeyframe>()))
                        keyframe.eventRandomValues[3] = _val;
                    Updater.UpdateObject(beatmapObject, "Keyframes");
                });
            }

            for (int n = 0; n <= (type == 0 ? 5 : type == 2 ? 4 : 3); n++)
            {
                // We skip the 2nd random type for compatibility with old PA levels.
                int buttonTmp = (n >= 2 && (type != 2 || n < 3)) ? (n + 1) : (n > 2 && type == 2) ? n + 2 : n;

                var randomToggles = kfdialog.Find("random");

                randomToggles.GetChild(n).gameObject.SetActive(buttonTmp != 5 && buttonTmp != 6 || RTEditor.ShowModdedUI);

                if (buttonTmp != 5 && buttonTmp != 6 || RTEditor.ShowModdedUI)
                {
                    var toggle = randomToggles.GetChild(n).GetComponent<Toggle>();
                    toggle.onValueChanged.ClearAll();
                    toggle.isOn = random == buttonTmp;
                    toggle.onValueChanged.AddListener(_val =>
                    {
                        if (_val)
                        {
                            foreach (var keyframe in selected.Select(x => x.GetData<EventKeyframe>()))
                                keyframe.random = buttonTmp;

                            // Since keyframe value has no affect on the timeline object, we will only need to update the physical object.
                            if (UpdateObjects)
                                Updater.UpdateObject(beatmapObject, "Keyframes");
                        }

                        UpdateKeyframeRandomDialog(kfdialog, randomValueLabel, randomValue, type, selected, firstKF, beatmapObject, typeName, buttonTmp);
                    });
                    if (!toggle.GetComponent<HoverUI>())
                    {
                        var hoverUI = toggle.gameObject.AddComponent<HoverUI>();
                        hoverUI.animatePos = false;
                        hoverUI.animateSca = true;
                        hoverUI.size = 1.1f;
                    }
                }
            }

            UpdateKeyframeRandomDialog(kfdialog, randomValueLabel, randomValue, type, selected, firstKF, beatmapObject, typeName, random);

            float num = 0f;
            if (firstKF.GetData<EventKeyframe>().eventRandomValues.Length > 2)
                num = firstKF.GetData<EventKeyframe>().eventRandomValues[2];

            var randomInterval = kfdialog.Find("random/interval-input");
            var randomIntervalIF = randomInterval.GetComponent<InputField>();
            randomIntervalIF.NewValueChangedListener(num.ToString(), _val =>
            {
                if (float.TryParse(_val, out float num))
                {
                    foreach (var keyframe in selected.Select(x => x.GetData<EventKeyframe>()))
                        keyframe.eventRandomValues[2] = num;

                    // Since keyframe value has no affect on the timeline object, we will only need to update the physical object.
                    if (UpdateObjects)
                        Updater.UpdateObject(beatmapObject, "Keyframes");
                }
            });

            TriggerHelper.AddEventTriggers(randomIntervalIF.gameObject,
                TriggerHelper.ScrollDelta(randomIntervalIF, 0.01f));

            if (!randomInterval.GetComponent<InputFieldSwapper>())
            {
                var ifh = randomInterval.gameObject.AddComponent<InputFieldSwapper>();
                ifh.Init(randomIntervalIF, InputFieldSwapper.Type.Num);
            }

            TriggerHelper.AddEventTriggers(randomInterval.gameObject, TriggerHelper.ScrollDelta(randomIntervalIF, max: random == 6 ? 1f : 0f));
        }

        void KeyframeRandomValueHandler(Transform kfdialog, int type, IEnumerable<TimelineObject> selected, TimelineObject firstKF, BeatmapObject beatmapObject, string typeName, int i, string valueType)
        {
            var randomValueLabel = kfdialog.Find($"r_{typeName}_label");
            var randomValueBase = kfdialog.Find($"r_{typeName}");

            if (!randomValueBase)
            {
                CoreHelper.LogError($"Value {valueType} (Base) is null.");
                return;
            }

            var randomValue = randomValueBase.Find(valueType);

            if (!randomValue)
            {
                CoreHelper.LogError($"Value {valueType} is null.");
                return;
            }

            var random = firstKF.GetData<EventKeyframe>().random;

            var valueButtonLeft = randomValue.Find("<").GetComponent<Button>();
            var valueButtonRight = randomValue.Find(">").GetComponent<Button>();

            var randomValueInputField = randomValue.GetComponent<InputField>();

            randomValueInputField.characterValidation = InputField.CharacterValidation.None;
            randomValueInputField.contentType = InputField.ContentType.Standard;
            randomValueInputField.keyboardType = TouchScreenKeyboardType.Default;
            randomValueInputField.onValueChanged.ClearAll();
            randomValueInputField.text = selected.Count() == 1 ? firstKF.GetData<EventKeyframe>().eventRandomValues[i].ToString() : typeName == "rotation" ? "15" : "1";
            randomValueInputField.onValueChanged.AddListener(_val =>
            {
                if (float.TryParse(_val, out float num) && selected.Count() == 1)
                {
                    firstKF.GetData<EventKeyframe>().eventRandomValues[i] = num;

                    // Since keyframe value has no affect on the timeline object, we will only need to update the physical object.
                    if (UpdateObjects)
                        Updater.UpdateObject(beatmapObject, "Keyframes");
                }
            });

            valueButtonLeft.onClick.ClearAll();
            valueButtonLeft.onClick.AddListener(() =>
            {
                if (float.TryParse(randomValueInputField.text, out float x))
                {
                    if (selected.Count() == 1)
                    {
                        randomValueInputField.text = (x - (typeName == "rotation" ? 15f : 1f)).ToString();
                        return;
                    }

                    foreach (var keyframe in selected)
                        keyframe.GetData<EventKeyframe>().eventRandomValues[i] -= x;

                    // Since keyframe value has no affect on the timeline object, we will only need to update the physical object.
                    if (UpdateObjects)
                        Updater.UpdateObject(beatmapObject, "Keyframes");
                }
            });

            valueButtonRight.onClick.ClearAll();
            valueButtonRight.onClick.AddListener(() =>
            {
                if (float.TryParse(randomValueInputField.text, out float x))
                {
                    if (selected.Count() == 1)
                    {
                        randomValueInputField.text = (x + (typeName == "rotation" ? 15f : 1f)).ToString();
                        return;
                    }

                    foreach (var keyframe in selected)
                        keyframe.GetData<EventKeyframe>().eventRandomValues[i] += x;

                    // Since keyframe value has no affect on the timeline object, we will only need to update the physical object.
                    if (UpdateObjects)
                        Updater.UpdateObject(beatmapObject, "Keyframes");
                }
            });

            TriggerHelper.AddEventTriggers(randomValue.gameObject,
                TriggerHelper.ScrollDelta(randomValueInputField, type == 2 && random != 6 ? 15f : 0.1f, type == 2 && random != 6 ? 3f : 10f, multi: true),
                TriggerHelper.ScrollDeltaVector2(randomValueInputField, randomValueBase.GetChild(1).GetComponent<InputField>(), type == 2 && random != 6 ? 15f : 0.1f, type == 2 && random != 6 ? 3f : 10f));

            if (!randomValue.GetComponent<InputFieldSwapper>())
            {
                var ifh = randomValue.gameObject.AddComponent<InputFieldSwapper>();
                ifh.Init(randomValueInputField, InputFieldSwapper.Type.Num);
            }
        }

        public void PasteKeyframeData(EventKeyframe copiedData, IEnumerable<TimelineObject> selected, BeatmapObject beatmapObject, string name)
        {
            if (copiedData == null)
            {
                EditorManager.inst.DisplayNotification($"{name} keyframe data not copied yet.", 2f, EditorManager.NotificationType.Error);
                return;
            }

            foreach (var timelineObject in selected)
            {
                var kf = timelineObject.GetData<EventKeyframe>();
                kf.curveType = copiedData.curveType;
                kf.eventValues = copiedData.eventValues.Copy();
                kf.eventRandomValues = copiedData.eventRandomValues.Copy();
                kf.random = copiedData.random;
                kf.relative = copiedData.relative;
            }

            RenderKeyframes(beatmapObject);
            RenderObjectKeyframesDialog(beatmapObject);
            Updater.UpdateObject(beatmapObject, "Keyframes");
            EditorManager.inst.DisplayNotification($"Pasted {name.ToLower()} keyframe data to current selected keyframe.", 2f, EditorManager.NotificationType.Success);
        }

        public void RenderObjectKeyframesDialog(BeatmapObject beatmapObject)
        {
            var selected = beatmapObject.timelineObject.InternalSelections.Where(x => x.Selected);

            for (int i = 0; i < ObjEditor.inst.KeyframeDialogs.Count; i++)
                ObjEditor.inst.KeyframeDialogs[i].SetActive(false);

            if (selected.Count() < 1)
            {
                return;
            }

            if (!(selected.Count() == 1 || selected.All(x => x.Type == selected.Min(y => y.Type))))
            {
                ObjEditor.inst.KeyframeDialogs[4].SetActive(true);

                try
                {
                    var dialog = ObjEditor.inst.KeyframeDialogs[4].transform;
                    var time = dialog.Find("time/time/time").GetComponent<InputField>();
                    time.onValueChanged.ClearAll();
                    if (time.text == "100.000")
                        time.text = "10";

                    var setTime = dialog.Find("time/time").GetChild(3).GetComponent<Button>();
                    setTime.onClick.ClearAll();
                    setTime.onClick.AddListener(() =>
                    {
                        if (float.TryParse(time.text, out float num))
                        {
                            if (num < 0f)
                                num = 0f;

                            if (EditorConfig.Instance.RoundToNearest.Value)
                                num = RTMath.RoundToNearestDecimal(num, 3);

                            foreach (var kf in selected.Where(x => x.Index != 0))
                                kf.Time = num;

                            ResizeKeyframeTimeline(beatmapObject);

                            RenderKeyframes(beatmapObject);

                            // Keyframe Time affects both physical object and timeline object.
                            RenderTimelineObject(GetTimelineObject(beatmapObject));
                            if (UpdateObjects)
                                Updater.UpdateObject(beatmapObject, "Keyframes");
                        }
                    });

                    var decreaseTimeGreat = dialog.Find("time/time/<<").GetComponent<Button>();
                    var decreaseTime = dialog.Find("time/time/<").GetComponent<Button>();
                    var increaseTimeGreat = dialog.Find("time/time/>>").GetComponent<Button>();
                    var increaseTime = dialog.Find("time/time/>").GetComponent<Button>();

                    decreaseTime.onClick.ClearAll();
                    decreaseTime.onClick.AddListener(() =>
                    {
                        if (float.TryParse(time.text, out float num))
                        {
                            if (num < 0f)
                                num = 0f;

                            if (EditorConfig.Instance.RoundToNearest.Value)
                                num = RTMath.RoundToNearestDecimal(num, 3);

                            foreach (var kf in selected.Where(x => x.Index != 0))
                                kf.Time = Mathf.Clamp(kf.Time - num, 0f, float.MaxValue);

                            ResizeKeyframeTimeline(beatmapObject);

                            RenderKeyframes(beatmapObject);

                            // Keyframe Time affects both physical object and timeline object.
                            RenderTimelineObject(GetTimelineObject(beatmapObject));
                            if (UpdateObjects)
                                Updater.UpdateObject(beatmapObject, "Keyframes");
                        }
                    });

                    increaseTime.onClick.ClearAll();
                    increaseTime.onClick.AddListener(() =>
                    {
                        if (float.TryParse(time.text, out float num))
                        {
                            if (num < 0f)
                                num = 0f;

                            if (EditorConfig.Instance.RoundToNearest.Value)
                                num = RTMath.RoundToNearestDecimal(num, 3);

                            foreach (var kf in selected.Where(x => x.Index != 0))
                                kf.Time = Mathf.Clamp(kf.Time + num, 0f, float.MaxValue);

                            ResizeKeyframeTimeline(beatmapObject);

                            RenderKeyframes(beatmapObject);

                            // Keyframe Time affects both physical object and timeline object.
                            RenderTimelineObject(GetTimelineObject(beatmapObject));
                            if (UpdateObjects)
                                Updater.UpdateObject(beatmapObject, "Keyframes");
                        }
                    });

                    decreaseTimeGreat.onClick.ClearAll();
                    decreaseTimeGreat.onClick.AddListener(() =>
                    {
                        if (float.TryParse(time.text, out float num))
                        {
                            if (num < 0f)
                                num = 0f;

                            if (EditorConfig.Instance.RoundToNearest.Value)
                                num = RTMath.RoundToNearestDecimal(num, 3);

                            foreach (var kf in selected.Where(x => x.Index != 0))
                                kf.Time = Mathf.Clamp(kf.Time - (num * 10f), 0f, float.MaxValue);

                            ResizeKeyframeTimeline(beatmapObject);

                            RenderKeyframes(beatmapObject);

                            // Keyframe Time affects both physical object and timeline object.
                            RenderTimelineObject(GetTimelineObject(beatmapObject));
                            if (UpdateObjects)
                                Updater.UpdateObject(beatmapObject, "Keyframes");
                        }
                    });

                    increaseTimeGreat.onClick.ClearAll();
                    increaseTimeGreat.onClick.AddListener(() =>
                    {
                        if (float.TryParse(time.text, out float num))
                        {
                            if (num < 0f)
                                num = 0f;

                            if (EditorConfig.Instance.RoundToNearest.Value)
                                num = RTMath.RoundToNearestDecimal(num, 3);

                            foreach (var kf in selected.Where(x => x.Index != 0))
                                kf.Time = Mathf.Clamp(kf.Time + (num * 10f), 0f, float.MaxValue);

                            ResizeKeyframeTimeline(beatmapObject);

                            RenderKeyframes(beatmapObject);

                            // Keyframe Time affects both physical object and timeline object.
                            RenderTimelineObject(GetTimelineObject(beatmapObject));
                            if (UpdateObjects)
                                Updater.UpdateObject(beatmapObject, "Keyframes");
                        }
                    });

                    TriggerHelper.AddEventTriggers(time.gameObject, TriggerHelper.ScrollDelta(time));

                    var curvesMulti = dialog.Find("curves/curves").GetComponent<Dropdown>();
                    curvesMulti.onValueChanged.ClearAll();
                    curvesMulti.onValueChanged.AddListener(_val =>
                    {
                        if (!DataManager.inst.AnimationListDictionary.TryGetValue(_val, out DataManager.LSAnimation anim))
                            return;

                        foreach (var keyframe in selected.Where(x => x.Index != 0).Select(x => x.GetData<EventKeyframe>()))
                            keyframe.curveType = anim;

                        ResizeKeyframeTimeline(beatmapObject);

                        RenderKeyframes(beatmapObject);

                        // Keyframe Time affects both physical object and timeline object.
                        RenderTimelineObject(GetTimelineObject(beatmapObject));
                        if (UpdateObjects)
                            Updater.UpdateObject(beatmapObject, "Keyframes");
                    });

                    var valueIndex = dialog.Find("value base/value index").GetComponent<InputField>();
                    valueIndex.onValueChanged.ClearAll();
                    if (valueIndex.text == "25.0")
                        valueIndex.text = "0";
                    valueIndex.onValueChanged.AddListener(_val =>
                    {
                        if (!int.TryParse(_val, out int n))
                            valueIndex.text = "0";
                    });

                    TriggerHelper.IncreaseDecreaseButtonsInt(valueIndex);
                    TriggerHelper.AddEventTriggers(valueIndex.gameObject, TriggerHelper.ScrollDeltaInt(valueIndex));

                    var value = dialog.Find("value base/value/input").GetComponent<InputField>();
                    value.onValueChanged.ClearAll();
                    value.onValueChanged.AddListener(_val =>
                    {
                        if (!float.TryParse(_val, out float n))
                            value.text = "0";
                    });

                    var setValue = value.transform.parent.GetChild(2).GetComponent<Button>();
                    setValue.onClick.ClearAll();
                    setValue.onClick.AddListener(() =>
                    {
                        if (float.TryParse(value.text, out float num))
                        {
                            foreach (var kf in selected)
                            {
                                var keyframe = kf.GetData<EventKeyframe>();

                                var index = Parser.TryParse(valueIndex.text, 0);

                                index = Mathf.Clamp(index, 0, keyframe.eventValues.Length - 1);
                                if (index >= 0 && index < keyframe.eventValues.Length)
                                    keyframe.eventValues[index] = kf.Type == 3 ? Mathf.Clamp((int)num, 0, CoreHelper.CurrentBeatmapTheme.objectColors.Count - 1) : num;
                            }

                            if (UpdateObjects)
                                Updater.UpdateObject(beatmapObject, "Keyframes");
                        }
                    });

                    TriggerHelper.IncreaseDecreaseButtons(value, t: value.transform.parent);
                    TriggerHelper.AddEventTriggers(value.gameObject, TriggerHelper.ScrollDelta(value));

                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                }

                return;
            }

            var firstKF = selected.ElementAt(0);
            var type = firstKF.Type;

            CoreHelper.Log($"Selected Keyframe:\nID - {firstKF.ID}\nType: {firstKF.Type}\nIndex {firstKF.Index}");

            ObjEditor.inst.KeyframeDialogs[type].SetActive(true);

            ObjEditor.inst.currentKeyframeKind = type;
            ObjEditor.inst.currentKeyframe = firstKF.Index;

            var kfdialog = ObjEditor.inst.KeyframeDialogs[type].transform;

            var timeDecreaseGreat = kfdialog.Find("time/<<").GetComponent<Button>();
            var timeDecrease = kfdialog.Find("time/<").GetComponent<Button>();
            var timeIncrease = kfdialog.Find("time/>").GetComponent<Button>();
            var timeIncreaseGreat = kfdialog.Find("time/>>").GetComponent<Button>();
            var timeSet = kfdialog.Find("time/time").GetComponent<InputField>();

            timeDecreaseGreat.interactable = firstKF.Index != 0;
            timeDecrease.interactable = firstKF.Index != 0;
            timeIncrease.interactable = firstKF.Index != 0;
            timeIncreaseGreat.interactable = firstKF.Index != 0;
            timeSet.interactable = firstKF.Index != 0;

            var superLeft = kfdialog.Find("edit/<<").GetComponent<Button>();

            superLeft.onClick.ClearAll();
            superLeft.interactable = firstKF.Index != 0;
            superLeft.onClick.AddListener(() => { SetCurrentKeyframe(beatmapObject, 0, true); });

            var left = kfdialog.Find("edit/<").GetComponent<Button>();

            left.onClick.ClearAll();
            left.interactable = selected.Count() == 1 && firstKF.Index != 0;
            left.onClick.AddListener(() => { SetCurrentKeyframe(beatmapObject, firstKF.Index - 1, true); });

            kfdialog.Find("edit/|").GetComponentInChildren<Text>().text = firstKF.Index == 0 ? "S" : firstKF.Index == beatmapObject.events[firstKF.Type].Count - 1 ? "E" : firstKF.Index.ToString();

            var right = kfdialog.Find("edit/>").GetComponent<Button>();

            right.onClick.ClearAll();
            right.interactable = selected.Count() == 1 && firstKF.Index < beatmapObject.events[type].Count - 1;
            right.onClick.AddListener(() => { SetCurrentKeyframe(beatmapObject, firstKF.Index + 1, true); });

            var superRight = kfdialog.Find("edit/>>").GetComponent<Button>();

            superRight.onClick.ClearAll();
            superRight.interactable = selected.Count() == 1 && firstKF.Index < beatmapObject.events[type].Count - 1;
            superRight.onClick.AddListener(() => { SetCurrentKeyframe(beatmapObject, beatmapObject.events[type].Count - 1, true); });

            var copy = kfdialog.Find("edit/copy").GetComponent<Button>();
            copy.onClick.ClearAll();
            copy.onClick.AddListener(() =>
            {
                switch (type)
                {
                    case 0:
                        CopiedPositionData = EventKeyframe.DeepCopy(firstKF.GetData<EventKeyframe>());
                        break;
                    case 1:
                        CopiedScaleData = EventKeyframe.DeepCopy(firstKF.GetData<EventKeyframe>());
                        break;
                    case 2:
                        CopiedRotationData = EventKeyframe.DeepCopy(firstKF.GetData<EventKeyframe>());
                        break;
                    case 3:
                        CopiedColorData = EventKeyframe.DeepCopy(firstKF.GetData<EventKeyframe>());
                        break;
                }
                EditorManager.inst.DisplayNotification("Copied keyframe data!", 2f, EditorManager.NotificationType.Success);
            });

            var paste = kfdialog.Find("edit/paste").GetComponent<Button>();
            paste.onClick.ClearAll();
            paste.onClick.AddListener(() =>
            {
                switch (type)
                {
                    case 0:
                        PasteKeyframeData(CopiedPositionData, selected, beatmapObject, "Position");
                        break;
                    case 1:
                        PasteKeyframeData(CopiedScaleData, selected, beatmapObject, "Scale");
                        break;
                    case 2:
                        PasteKeyframeData(CopiedRotationData, selected, beatmapObject, "Rotation");
                        break;
                    case 3:
                        PasteKeyframeData(CopiedColorData, selected, beatmapObject, "Color");
                        break;
                }
            });

            var deleteKey = kfdialog.Find("edit/del").GetComponent<Button>();

            deleteKey.onClick.ClearAll();
            deleteKey.onClick.AddListener(() => { StartCoroutine(DeleteKeyframes(beatmapObject)); });

            var tet = kfdialog.Find("time").GetComponent<EventTrigger>();
            var tif = kfdialog.Find("time/time").GetComponent<InputField>();

            tet.triggers.Clear();
            if (selected.Count() == 1 && firstKF.Index != 0 || selected.Count() > 1)
                tet.triggers.Add(TriggerHelper.ScrollDelta(tif));

            tif.onValueChanged.ClearAll();
            tif.text = selected.Count() == 1 ? firstKF.Time.ToString() : "1";
            tif.onValueChanged.AddListener(_val =>
            {
                if (float.TryParse(_val, out float num) && !ObjEditor.inst.timelineKeyframesDrag && selected.Count() == 1)
                {
                    if (num < 0f)
                        num = 0f;

                    if (EditorConfig.Instance.RoundToNearest.Value)
                        num = RTMath.RoundToNearestDecimal(num, 3);

                    firstKF.Time = num;

                    ResizeKeyframeTimeline(beatmapObject);

                    RenderKeyframes(beatmapObject);

                    // Keyframe Time affects both physical object and timeline object.
                    RenderTimelineObject(GetTimelineObject(beatmapObject));
                    if (UpdateObjects)
                        Updater.UpdateObject(beatmapObject, "Keyframes");
                }
            });

            if (selected.Count() == 1)
                TriggerHelper.IncreaseDecreaseButtons(tif, t: kfdialog.Find("time"));
            else
            {
                var btR = kfdialog.Find("time/<").GetComponent<Button>();
                var btL = kfdialog.Find("time/>").GetComponent<Button>();
                var btGR = kfdialog.Find("time/<<").GetComponent<Button>();
                var btGL = kfdialog.Find("time/>>").GetComponent<Button>();

                btR.onClick.ClearAll();
                btR.onClick.AddListener(() =>
                {
                    if (float.TryParse(tif.text, out float result))
                    {
                        var num = Input.GetKey(KeyCode.LeftAlt) ? 0.1f / 10f : Input.GetKey(KeyCode.LeftControl) ? 0.1f * 10f : 0.1f;
                        result -= num;

                        if (selected.Count() == 1)
                        {
                            tif.text = result.ToString();
                            return;
                        }

                        foreach (var keyframe in selected)
                            keyframe.Time = Mathf.Clamp(keyframe.Time - num, 0.001f, float.PositiveInfinity);
                    }
                });

                btL.onClick.ClearAll();
                btL.onClick.AddListener(() =>
                {
                    if (float.TryParse(tif.text, out float result))
                    {
                        var num = Input.GetKey(KeyCode.LeftAlt) ? 0.1f / 10f : Input.GetKey(KeyCode.LeftControl) ? 0.1f * 10f : 0.1f;
                        result += num;

                        if (selected.Count() == 1)
                        {
                            tif.text = result.ToString();
                            return;
                        }

                        foreach (var keyframe in selected)
                            keyframe.Time = Mathf.Clamp(keyframe.Time + num, 0.001f, float.PositiveInfinity);
                    }
                });

                btGR.onClick.ClearAll();
                btGR.onClick.AddListener(() =>
                {
                    if (float.TryParse(tif.text, out float result))
                    {
                        var num = (Input.GetKey(KeyCode.LeftAlt) ? 0.1f / 10f : Input.GetKey(KeyCode.LeftControl) ? 0.1f * 10f : 0.1f) * 10f;
                        result -= num;

                        if (selected.Count() == 1)
                        {
                            tif.text = result.ToString();
                            return;
                        }

                        foreach (var keyframe in selected)
                            keyframe.Time = Mathf.Clamp(keyframe.Time - num, 0.001f, float.PositiveInfinity);
                    }
                });

                btGL.onClick.ClearAll();
                btGL.onClick.AddListener(() =>
                {
                    if (float.TryParse(tif.text, out float result))
                    {
                        var num = (Input.GetKey(KeyCode.LeftAlt) ? 0.1f / 10f : Input.GetKey(KeyCode.LeftControl) ? 0.1f * 10f : 0.1f) * 10f;
                        result += num;

                        if (selected.Count() == 1)
                        {
                            tif.text = result.ToString();
                            return;
                        }

                        foreach (var keyframe in selected)
                            keyframe.Time = Mathf.Clamp(keyframe.Time + num, 0.001f, float.PositiveInfinity);
                    }
                });
            }

            kfdialog.Find("curves_label").gameObject.SetActive(selected.Count() == 1 && firstKF.Index != 0 || selected.Count() > 1);
            kfdialog.Find("curves").gameObject.SetActive(selected.Count() == 1 && firstKF.Index != 0 || selected.Count() > 1);
            var curves = kfdialog.Find("curves").GetComponent<Dropdown>();
            curves.onValueChanged.ClearAll();

            if (DataManager.inst.AnimationListDictionaryBack.TryGetValue(firstKF.GetData<EventKeyframe>().curveType, out int animIndex))
                curves.value = animIndex;

            curves.onValueChanged.AddListener(_val =>
            {
                if (!DataManager.inst.AnimationListDictionary.TryGetValue(_val, out DataManager.LSAnimation anim))
                    return;

                foreach (var keyframe in selected.Select(x => x.GetData<EventKeyframe>()))
                    keyframe.curveType = anim;

                // Since keyframe curve has no affect on the timeline object, we will only need to update the physical object.
                if (UpdateObjects)
                    Updater.UpdateObject(beatmapObject, "Keyframes");
                RenderKeyframes(beatmapObject);
            });

            switch (type)
            {
                case 0:
                    {
                        KeyframeHandler(kfdialog, type, selected, firstKF, beatmapObject, "position", 0, "x");
                        KeyframeHandler(kfdialog, type, selected, firstKF, beatmapObject, "position", 1, "y");
                        KeyframeHandler(kfdialog, type, selected, firstKF, beatmapObject, "position", 2, "z");

                        KeyframeRandomHandler(kfdialog, type, selected, firstKF, beatmapObject, "position");
                        KeyframeRandomValueHandler(kfdialog, type, selected, firstKF, beatmapObject, "position", 0, "x");
                        KeyframeRandomValueHandler(kfdialog, type, selected, firstKF, beatmapObject, "position", 1, "y");

                        break;
                    }
                case 1:
                    {
                        KeyframeHandler(kfdialog, type, selected, firstKF, beatmapObject, "scale", 0, "x");
                        KeyframeHandler(kfdialog, type, selected, firstKF, beatmapObject, "scale", 1, "y");

                        KeyframeRandomHandler(kfdialog, type, selected, firstKF, beatmapObject, "scale");
                        KeyframeRandomValueHandler(kfdialog, type, selected, firstKF, beatmapObject, "scale", 0, "x");
                        KeyframeRandomValueHandler(kfdialog, type, selected, firstKF, beatmapObject, "scale", 1, "y");

                        break;
                    }
                case 2:
                    {
                        KeyframeHandler(kfdialog, type, selected, firstKF, beatmapObject, "rotation", 0, "x");

                        KeyframeRandomHandler(kfdialog, type, selected, firstKF, beatmapObject, "rotation");
                        KeyframeRandomValueHandler(kfdialog, type, selected, firstKF, beatmapObject, "rotation", 0, "x");
                        KeyframeRandomValueHandler(kfdialog, type, selected, firstKF, beatmapObject, "rotation", 1, "y");

                        break;
                    }
                case 3:
                    {
                        bool showModifiedColors = EditorConfig.Instance.ShowModifiedColors.Value;
                        var eventTime = firstKF.GetData<EventKeyframe>().eventTime;
                        int index = 0;
                        foreach (var toggle in ObjEditor.inst.colorButtons)
                        {
                            int tmpIndex = index;

                            toggle.gameObject.SetActive(RTEditor.ShowModdedUI || tmpIndex < 9);

                            toggle.onValueChanged.ClearAll();
                            if (RTEditor.ShowModdedUI || tmpIndex < 9)
                            {
                                toggle.isOn = index == firstKF.GetData<EventKeyframe>().eventValues[0];
                                toggle.onValueChanged.AddListener(_val => { SetKeyframeColor(beatmapObject, 0, tmpIndex, ObjEditor.inst.colorButtons, selected); });
                            }

                            if (showModifiedColors)
                            {
                                var color = CoreHelper.CurrentBeatmapTheme.GetObjColor(tmpIndex);

                                float hueNum = beatmapObject.Interpolate(eventTime, type, 2);
                                float satNum = beatmapObject.Interpolate(eventTime, type, 3);
                                float valNum = beatmapObject.Interpolate(eventTime, type, 4);

                                toggle.image.color = CoreHelper.ChangeColorHSV(color, hueNum, satNum, valNum);
                            }
                            else
                                toggle.image.color = CoreHelper.CurrentBeatmapTheme.GetObjColor(tmpIndex);

                            if (!toggle.GetComponent<HoverUI>())
                            {
                                var hoverUI = toggle.gameObject.AddComponent<HoverUI>();
                                hoverUI.animatePos = false;
                                hoverUI.animateSca = true;
                                hoverUI.size = 1.1f;
                            }
                            index++;
                        }

                        var random = firstKF.GetData<EventKeyframe>().random;

                        kfdialog.Find("opacity_label").gameObject.SetActive(RTEditor.NotSimple);
                        kfdialog.Find("opacity").gameObject.SetActive(RTEditor.NotSimple);
                        kfdialog.Find("opacity/collision").gameObject.SetActive(RTEditor.ShowModdedUI);

                        kfdialog.Find("huesatval_label").gameObject.SetActive(RTEditor.ShowModdedUI);
                        kfdialog.Find("huesatval").gameObject.SetActive(RTEditor.ShowModdedUI);

                        var showGradient = RTEditor.NotSimple && beatmapObject.gradientType != BeatmapObject.GradientType.Normal;

                        kfdialog.Find("color_label").GetChild(0).GetComponent<Text>().text = showGradient ? "Start Color" : "Color";
                        kfdialog.Find("opacity_label").GetChild(0).GetComponent<Text>().text = showGradient ? "Start Opacity" : "Opacity";
                        kfdialog.Find("huesatval_label").GetChild(0).GetComponent<Text>().text = showGradient ? "Start Hue" : "Hue";
                        kfdialog.Find("huesatval_label").GetChild(1).GetComponent<Text>().text = showGradient ? "Start Saturation" : "Saturation";
                        kfdialog.Find("huesatval_label").GetChild(2).GetComponent<Text>().text = showGradient ? "Start Value" : "Value";

                        kfdialog.Find("gradient_color_label").gameObject.SetActive(showGradient);
                        kfdialog.Find("gradient_color").gameObject.SetActive(showGradient);
                        kfdialog.Find("gradient_opacity_label").gameObject.SetActive(showGradient && RTEditor.ShowModdedUI);
                        kfdialog.Find("gradient_opacity").gameObject.SetActive(showGradient && RTEditor.ShowModdedUI);
                        kfdialog.Find("gradient_huesatval_label").gameObject.SetActive(showGradient && RTEditor.ShowModdedUI);
                        kfdialog.Find("gradient_huesatval").gameObject.SetActive(showGradient && RTEditor.ShowModdedUI);

                        kfdialog.Find("color").AsRT().sizeDelta = new Vector2(366f, RTEditor.ShowModdedUI ? 78f : 32f);
                        kfdialog.Find("gradient_color").AsRT().sizeDelta = new Vector2(366f, RTEditor.ShowModdedUI ? 78f : 32f);

                        if (gradientColorButtons.Count == 0)
                        {
                            for (int i = 0; i < kfdialog.Find("gradient_color").childCount; i++)
                            {
                                gradientColorButtons.Add(kfdialog.Find("gradient_color").GetChild(i).GetComponent<Toggle>());
                            }
                        }

                        if (!RTEditor.NotSimple)
                            break;

                        var opacity = kfdialog.Find("opacity/x").GetComponent<InputField>();

                        opacity.onValueChanged.RemoveAllListeners();
                        opacity.text = (-firstKF.GetData<EventKeyframe>().eventValues[1] + 1).ToString();
                        opacity.onValueChanged.AddListener(_val =>
                        {
                            if (float.TryParse(_val, out float n))
                            {
                                var value = Mathf.Clamp(-n + 1, 0f, 1f);
                                foreach (var keyframe in selected.Select(x => x.GetData<EventKeyframe>()))
                                {
                                    keyframe.eventValues[1] = value;
                                    if (!RTEditor.ShowModdedUI)
                                        keyframe.eventValues[6] = 10f;
                                }

                                // Since keyframe value has no affect on the timeline object, we will only need to update the physical object.
                                if (UpdateObjects)
                                    Updater.UpdateObject(beatmapObject, "Keyframes");
                            }
                        });

                        TriggerHelper.AddEventTriggers(kfdialog.Find("opacity").gameObject, TriggerHelper.ScrollDelta(opacity, 0.1f, 10f, 0f, 1f));

                        TriggerHelper.IncreaseDecreaseButtons(opacity);

                        index = 0;
                        foreach (var toggle in gradientColorButtons)
                        {
                            int tmpIndex = index;

                            toggle.gameObject.SetActive(RTEditor.ShowModdedUI || tmpIndex < 9);

                            toggle.onValueChanged.ClearAll();
                            if (RTEditor.ShowModdedUI || tmpIndex < 9)
                            {
                                toggle.isOn = index == firstKF.GetData<EventKeyframe>().eventValues[5];
                                toggle.onValueChanged.AddListener(_val => { SetKeyframeColor(beatmapObject, 5, tmpIndex, gradientColorButtons, selected); });
                            }

                            if (showModifiedColors)
                            {
                                var color = CoreHelper.CurrentBeatmapTheme.GetObjColor(tmpIndex);

                                float hueNum = beatmapObject.Interpolate(eventTime, type, 7);
                                float satNum = beatmapObject.Interpolate(eventTime, type, 8);
                                float valNum = beatmapObject.Interpolate(eventTime, type, 9);

                                toggle.image.color = CoreHelper.ChangeColorHSV(color, hueNum, satNum, valNum);
                            }
                            else
                                toggle.image.color = CoreHelper.CurrentBeatmapTheme.GetObjColor(tmpIndex);

                            if (!toggle.GetComponent<HoverUI>())
                            {
                                var hoverUI = toggle.gameObject.AddComponent<HoverUI>();
                                hoverUI.animatePos = false;
                                hoverUI.animateSca = true;
                                hoverUI.size = 1.1f;
                            }
                            index++;
                        }

                        if (!RTEditor.ShowModdedUI)
                            break;

                        var collision = kfdialog.Find("opacity/collision").GetComponent<Toggle>();
                        collision.onValueChanged.ClearAll();
                        collision.isOn = beatmapObject.opacityCollision;
                        collision.onValueChanged.AddListener(_val =>
                        {
                            beatmapObject.opacityCollision = _val;
                            // Since keyframe value has no affect on the timeline object, we will only need to update the physical object.
                            if (UpdateObjects)
                                Updater.UpdateObject(beatmapObject);
                        });

                        var gradientOpacity = kfdialog.Find("gradient_opacity/x").GetComponent<InputField>();

                        gradientOpacity.onValueChanged.RemoveAllListeners();
                        gradientOpacity.text = (-firstKF.GetData<EventKeyframe>().eventValues[6] + 1).ToString();
                        gradientOpacity.onValueChanged.AddListener(_val =>
                        {
                            if (float.TryParse(_val, out float n))
                            {
                                foreach (var keyframe in selected.Select(x => x.GetData<EventKeyframe>()))
                                    keyframe.eventValues[6] = Mathf.Clamp(-n + 1, 0f, 1f);

                                // Since keyframe value has no affect on the timeline object, we will only need to update the physical object.
                                if (UpdateObjects)
                                    Updater.UpdateObject(beatmapObject, "Keyframes");
                            }
                        });

                        TriggerHelper.AddEventTriggers(kfdialog.Find("gradient_opacity").gameObject, TriggerHelper.ScrollDelta(gradientOpacity, 0.1f, 10f, 0f, 1f));

                        TriggerHelper.IncreaseDecreaseButtons(gradientOpacity);

                        // Start
                        {
                            var hue = kfdialog.Find("huesatval/x").GetComponent<InputField>();

                            hue.onValueChanged.RemoveAllListeners();
                            hue.text = firstKF.GetData<EventKeyframe>().eventValues[2].ToString();
                            hue.onValueChanged.AddListener(_val =>
                            {
                                if (float.TryParse(_val, out float n))
                                {
                                    firstKF.GetData<EventKeyframe>().eventValues[2] = n;

                                    // Since keyframe value has no affect on the timeline object, we will only need to update the physical object.
                                    if (UpdateObjects)
                                        Updater.UpdateObject(beatmapObject, "Keyframes");
                                }
                            });

                            Destroy(kfdialog.transform.Find("huesatval").GetComponent<EventTrigger>());

                            TriggerHelper.AddEventTriggers(hue.gameObject, TriggerHelper.ScrollDelta(hue));
                            TriggerHelper.IncreaseDecreaseButtons(hue);

                            var sat = kfdialog.Find("huesatval/y").GetComponent<InputField>();

                            sat.onValueChanged.RemoveAllListeners();
                            sat.text = firstKF.GetData<EventKeyframe>().eventValues[3].ToString();
                            sat.onValueChanged.AddListener(_val =>
                            {
                                if (float.TryParse(_val, out float n))
                                {
                                    firstKF.GetData<EventKeyframe>().eventValues[3] = n;

                                    // Since keyframe value has no affect on the timeline object, we will only need to update the physical object.
                                    if (UpdateObjects)
                                        Updater.UpdateObject(beatmapObject, "Keyframes");
                                }
                            });

                            TriggerHelper.AddEventTriggers(sat.gameObject, TriggerHelper.ScrollDelta(sat));
                            TriggerHelper.IncreaseDecreaseButtons(sat);

                            var val = kfdialog.Find("huesatval/z").GetComponent<InputField>();

                            val.onValueChanged.RemoveAllListeners();
                            val.text = firstKF.GetData<EventKeyframe>().eventValues[4].ToString();
                            val.onValueChanged.AddListener(_val =>
                            {
                                if (float.TryParse(_val, out float n))
                                {
                                    firstKF.GetData<EventKeyframe>().eventValues[4] = n;

                                    // Since keyframe value has no affect on the timeline object, we will only need to update the physical object.
                                    if (UpdateObjects)
                                        Updater.UpdateObject(beatmapObject, "Keyframes");
                                }
                            });

                            TriggerHelper.AddEventTriggers(val.gameObject, TriggerHelper.ScrollDelta(val));
                            TriggerHelper.IncreaseDecreaseButtons(val);
                        }
                        
                        // End
                        {
                            var hue = kfdialog.Find("gradient_huesatval/x").GetComponent<InputField>();

                            hue.onValueChanged.RemoveAllListeners();
                            hue.text = firstKF.GetData<EventKeyframe>().eventValues[7].ToString();
                            hue.onValueChanged.AddListener(_val =>
                            {
                                if (float.TryParse(_val, out float n))
                                {
                                    firstKF.GetData<EventKeyframe>().eventValues[7] = n;

                                    // Since keyframe value has no affect on the timeline object, we will only need to update the physical object.
                                    if (UpdateObjects)
                                        Updater.UpdateObject(beatmapObject, "Keyframes");
                                }
                            });

                            Destroy(kfdialog.transform.Find("gradient_huesatval").GetComponent<EventTrigger>());

                            TriggerHelper.AddEventTriggers(hue.gameObject, TriggerHelper.ScrollDelta(hue));
                            TriggerHelper.IncreaseDecreaseButtons(hue);

                            var sat = kfdialog.Find("gradient_huesatval/y").GetComponent<InputField>();

                            sat.onValueChanged.RemoveAllListeners();
                            sat.text = firstKF.GetData<EventKeyframe>().eventValues[8].ToString();
                            sat.onValueChanged.AddListener(_val =>
                            {
                                if (float.TryParse(_val, out float n))
                                {
                                    firstKF.GetData<EventKeyframe>().eventValues[8] = n;

                                    // Since keyframe value has no affect on the timeline object, we will only need to update the physical object.
                                    if (UpdateObjects)
                                        Updater.UpdateObject(beatmapObject, "Keyframes");
                                }
                            });

                            TriggerHelper.AddEventTriggers(sat.gameObject, TriggerHelper.ScrollDelta(sat));
                            TriggerHelper.IncreaseDecreaseButtons(sat);

                            var val = kfdialog.Find("gradient_huesatval/z").GetComponent<InputField>();

                            val.onValueChanged.RemoveAllListeners();
                            val.text = firstKF.GetData<EventKeyframe>().eventValues[9].ToString();
                            val.onValueChanged.AddListener(_val =>
                            {
                                if (float.TryParse(_val, out float n))
                                {
                                    firstKF.GetData<EventKeyframe>().eventValues[9] = n;

                                    // Since keyframe value has no affect on the timeline object, we will only need to update the physical object.
                                    if (UpdateObjects)
                                        Updater.UpdateObject(beatmapObject, "Keyframes");
                                }
                            });

                            TriggerHelper.AddEventTriggers(val.gameObject, TriggerHelper.ScrollDelta(val));
                            TriggerHelper.IncreaseDecreaseButtons(val);
                        }

                        break;
                    }
            }

            var relativeBase = kfdialog.Find("relative");

            if (!relativeBase)
                return;

            RTEditor.SetActive(relativeBase.gameObject, RTEditor.ShowModdedUI);
            if (RTEditor.ShowModdedUI)
            {
                var relative = relativeBase.GetComponent<Toggle>();
                relative.onValueChanged.ClearAll();
                relative.isOn = firstKF.GetData<EventKeyframe>().relative;
                relative.onValueChanged.AddListener(_val =>
                {
                    foreach (var keyframe in selected.Select(x => x.GetData<EventKeyframe>()))
                        keyframe.relative = _val;

                    // Since keyframe value has no affect on the timeline object, we will only need to update the physical object.
                    if (UpdateObjects)
                        Updater.UpdateObject(beatmapObject, "Keyframes");
                });
            }
        }

        public void RenderMarkers(BeatmapObject beatmapObject)
        {
            var parent = ObjEditor.inst.objTimelineSlider.transform.Find("Markers");

            var dottedLine = ObjEditor.inst.KeyframeEndPrefab.GetComponent<Image>().sprite;
            LSHelpers.DeleteChildren(parent);

            for (int i = 0; i < GameData.Current.beatmapData.markers.Count; i++)
            {
                var marker = (Marker)GameData.Current.beatmapData.markers[i];
                var length = beatmapObject.GetObjectLifeLength(ObjEditor.inst.ObjectLengthOffset);
                if (marker.time < beatmapObject.StartTime || marker.time > beatmapObject.StartTime + length)
                    continue;
                int index = i;

                var gameObject = MarkerEditor.inst.markerPrefab.Duplicate(parent, $"Marker {index}");
                var pos = (marker.time - beatmapObject.StartTime) / length;
                UIManager.SetRectTransform(gameObject.transform.AsRT(), new Vector2(0f, -12f), new Vector2(pos, 1f), new Vector2(pos, 1f), new Vector2(0.5f, 1f), new Vector2(12f, 12f));

                gameObject.GetComponent<Image>().color = MarkerEditor.inst.markerColors[Mathf.Clamp(marker.color, 0, MarkerEditor.inst.markerColors.Count - 1)];
                gameObject.GetComponentInChildren<Text>().text = marker.name;
                var line = gameObject.transform.Find("line").GetComponent<Image>();
                line.rectTransform.sizeDelta = new Vector2(5f, 301f);
                line.sprite = dottedLine;
                line.type = Image.Type.Tiled;

                TriggerHelper.AddEventTriggers(gameObject, TriggerHelper.CreateEntry(EventTriggerType.PointerClick, eventData =>
                {
                    var pointerEventData = (PointerEventData)eventData;

                    if (pointerEventData.button == PointerEventData.InputButton.Left && RTMarkerEditor.inst.timelineMarkers.TryFind(x => x.Marker.id == marker.id, out TimelineMarker timelineMarker))
                    {
                        RTMarkerEditor.inst.SetCurrentMarker(timelineMarker);
                        AudioManager.inst.SetMusicTimeWithDelay(Mathf.Clamp(timelineMarker.Marker.time, 0f, AudioManager.inst.CurrentAudioSource.clip.length), 0.05f);
                    }

                    if (pointerEventData.button == PointerEventData.InputButton.Right)
                        RTMarkerEditor.inst.DeleteMarker(index);

                    if (pointerEventData.button == PointerEventData.InputButton.Middle)
                        AudioManager.inst.SetMusicTime(marker.time);
                }));
            }
        }

        public void OpenImageSelector(BeatmapObject beatmapObject)
        {
            var editorPath = RTFile.ApplicationDirectory + RTEditor.editorListSlash + EditorManager.inst.currentLoadedLevel;
            string jpgFile = FileBrowser.OpenSingleFile("Select an image!", editorPath, new string[] { "png", "jpg" });
            CoreHelper.Log($"Selected file: {jpgFile}");
            if (!string.IsNullOrEmpty(jpgFile))
            {
                string jpgFileLocation = editorPath + "/" + Path.GetFileName(jpgFile);
                CoreHelper.Log($"jpgFileLocation: {jpgFileLocation}");

                var levelPath = jpgFile.Replace("\\", "/").Replace(editorPath + "/", "");
                CoreHelper.Log($"levelPath: {levelPath}");

                if (!RTFile.FileExists(jpgFileLocation) && !jpgFile.Replace("\\", "/").Contains(editorPath))
                {
                    File.Copy(jpgFile, jpgFileLocation);
                    CoreHelper.Log($"Copied file to : {jpgFileLocation}");
                }
                else
                    jpgFileLocation = editorPath + "/" + levelPath;

                CoreHelper.Log($"jpgFileLocation: {jpgFileLocation}");
                beatmapObject.text = jpgFileLocation.Replace(jpgFileLocation.Substring(0, jpgFileLocation.LastIndexOf('/') + 1), "");

                // Since setting image has no affect on the timeline object, we will only need to update the physical object.
                if (UpdateObjects)
                    Updater.UpdateObject(beatmapObject, "Shape");

                RenderShape(beatmapObject);
            }
        }

        #endregion

        #region Keyframe Handlers

        public GameObject keyframeEnd;

        public static bool AllowTimeExactlyAtStart => false;
        public void ResizeKeyframeTimeline(BeatmapObject beatmapObject)
        {
            // ObjEditor.inst.ObjectLengthOffset is the offset from the last keyframe. Could allow for more timeline space.
            float objectLifeLength = beatmapObject.GetObjectLifeLength(ObjEditor.inst.ObjectLengthOffset);
            float x = ObjEditor.inst.posCalc(objectLifeLength);

            ObjEditor.inst.objTimelineContent.AsRT().sizeDelta = new Vector2(x, 0f);
            ObjEditor.inst.objTimelineGrid.AsRT().sizeDelta = new Vector2(x, 122f);

            // Whether the value should clamp at 0.001 over StartTime or not.
            ObjEditor.inst.objTimelineSlider.minValue = AllowTimeExactlyAtStart ? beatmapObject.StartTime : beatmapObject.StartTime + 0.001f;
            ObjEditor.inst.objTimelineSlider.maxValue = beatmapObject.StartTime + objectLifeLength;

            if (!keyframeEnd)
            {
                ObjEditor.inst.objTimelineGrid.DeleteChildren();
                keyframeEnd = ObjEditor.inst.KeyframeEndPrefab.Duplicate(ObjEditor.inst.objTimelineGrid, "end keyframe");
            }

            var rectTransform = keyframeEnd.transform.AsRT();
            rectTransform.sizeDelta = new Vector2(4f, 122f);
            rectTransform.anchoredPosition = new Vector2(beatmapObject.GetObjectLifeLength() * ObjEditor.inst.Zoom * 14f, 0f);
        }

        public void ClearKeyframes(BeatmapObject beatmapObject)
        {
            var timelineObject = GetTimelineObject(beatmapObject);

            foreach (var kf in timelineObject.InternalSelections)
                Destroy(kf.GameObject);
        }

        public TimelineObject GetKeyframe(BeatmapObject beatmapObject, int type, int index)
        {
            var bmTimelineObject = GetTimelineObject(beatmapObject);

            var kf = bmTimelineObject.InternalSelections.Find(x => x.Type == type && x.Index == index);

            if (!kf)
                kf = bmTimelineObject.InternalSelections.Find(x => x.ID == (beatmapObject.events[type][index] as EventKeyframe).id);

            if (!kf)
            {
                kf = CreateKeyframe(beatmapObject, type, index);
                bmTimelineObject.InternalSelections.Add(kf);
            }

            if (!kf.GameObject)
            {
                kf.GameObject = KeyframeObject(beatmapObject, kf);
                kf.Image = kf.GameObject.transform.GetChild(0).GetComponent<Image>();
                kf.Update();
            }

            return kf;
        }

        public void CreateKeyframes(BeatmapObject beatmapObject)
        {
            ClearKeyframes(beatmapObject);

            if (!beatmapObject.timelineObject)
                return;

            for (int i = 0; i < beatmapObject.events.Count; i++)
            {
                if (beatmapObject.events[i].Count <= 0)
                    return;

                for (int j = 0; j < beatmapObject.events[i].Count; j++)
                {
                    var keyframe = (EventKeyframe)beatmapObject.events[i][j];
                    var kf = beatmapObject.timelineObject.InternalSelections.Find(x => x.ID == keyframe.id);
                    if (!kf)
                    {
                        kf = CreateKeyframe(beatmapObject, i, j);
                        beatmapObject.timelineObject.InternalSelections.Add(kf);
                    }

                    if (!kf.GameObject)
                    {
                        kf.GameObject = KeyframeObject(beatmapObject, kf);
                        kf.Image = kf.GameObject.transform.GetChild(0).GetComponent<Image>();
                        kf.Update();
                    }

                    RenderKeyframe(beatmapObject, kf);
                }
            }
        }

        public TimelineObject CreateKeyframe(BeatmapObject beatmapObject, int type, int index)
        {
            var eventKeyframe = beatmapObject.events[type][index];

            var kf = new TimelineObject(eventKeyframe)
            {
                Type = type,
                Index = index,
                isObjectKeyframe = true
            };

            kf.GameObject = KeyframeObject(beatmapObject, kf);
            kf.Image = kf.GameObject.transform.GetChild(0).GetComponent<Image>();
            kf.Update();

            return kf;
        }

        public GameObject KeyframeObject(BeatmapObject beatmapObject, TimelineObject kf)
        {
            var gameObject = ObjEditor.inst.objTimelinePrefab.Duplicate(ObjEditor.inst.TimelineParents[kf.Type], $"{IntToType(kf.Type)}_{kf.Index}");

            var button = gameObject.GetComponent<Button>();
            button.onClick.ClearAll();
            button.onClick.AddListener(() =>
            {
                if (!Input.GetMouseButtonDown(2))
                    SetCurrentKeyframe(beatmapObject, kf.Type, kf.Index, false, InputDataManager.inst.editorActions.MultiSelect.IsPressed);
            });

            TriggerHelper.AddEventTriggers(gameObject,
                TriggerHelper.CreateKeyframeStartDragTrigger(beatmapObject, kf),
                TriggerHelper.CreateKeyframeEndDragTrigger(beatmapObject, kf),
                TriggerHelper.CreateKeyframeSelectTrigger(beatmapObject, kf));

            return gameObject;
        }

        public void RenderKeyframes(BeatmapObject beatmapObject)
        {
            for (int i = 0; i < beatmapObject.events.Count; i++)
            {
                for (int j = 0; j < beatmapObject.events[i].Count; j++)
                {
                    var kf = GetKeyframe(beatmapObject, i, j);

                    RenderKeyframe(beatmapObject, kf);
                }
            }

            var timelineObject = GetTimelineObject(beatmapObject);
            if (timelineObject.InternalSelections.Count > 0 && timelineObject.InternalSelections.Where(x => x.Selected).Count() == 0)
            {
                if (EditorConfig.Instance.RememberLastKeyframeType.Value && timelineObject.InternalSelections.TryFind(x => x.Type == ObjEditor.inst.currentKeyframeKind, out TimelineObject kf))
                    kf.Selected = true;
                else
                    timelineObject.InternalSelections[0].Selected = true;
            }

            if (timelineObject.InternalSelections.Count >= 1000)
                AchievementManager.inst.UnlockAchievement("holy_keyframes");
        }

        public void RenderKeyframe(BeatmapObject beatmapObject, TimelineObject timelineObject)
        {
            if (beatmapObject.events[timelineObject.Type].TryFindIndex(x => (x as EventKeyframe).id == timelineObject.ID, out int kfIndex))
                timelineObject.Index = kfIndex;

            var eventKeyframe = timelineObject.GetData<EventKeyframe>();
            timelineObject.Image.sprite =
                                RTEditor.GetKeyframeIcon(eventKeyframe.curveType,
                                beatmapObject.events[timelineObject.Type].Count > timelineObject.Index + 1 ?
                                beatmapObject.events[timelineObject.Type][timelineObject.Index + 1].curveType : DataManager.inst.AnimationList[0]);

            float x = ObjEditor.inst.posCalc(eventKeyframe.eventTime);

            var rectTransform = (RectTransform)timelineObject.GameObject.transform;
            rectTransform.sizeDelta = new Vector2(14f, 25f);
            rectTransform.anchoredPosition = new Vector2(x, 0f);

            var locked = timelineObject.GameObject.transform.Find("lock");
            if (locked)
                locked.gameObject.SetActive(timelineObject.Locked);
        }

        public void UpdateKeyframeOrder(BeatmapObject beatmapObject)
        {
            for (int i = 0; i < beatmapObject.events.Count; i++)
            {
                beatmapObject.events[i] = (from x in beatmapObject.events[i]
                                           orderby x.eventTime
                                           select x).ToList();
            }

            RenderKeyframes(beatmapObject);
        }

        public static string IntToAxis(int num)
        {
            switch (num)
            {
                case 0: return "x";
                case 1: return "y";
                case 2: return "z";
                case 3: return "w";
                default: throw new Exception("Axis out of dimensional range.");
            }
        }

        public static string IntToType(int num)
        {
            switch (num)
            {
                case 0: return "pos";
                case 1: return "sca";
                case 2: return "rot";
                case 3: return "col";
                default: throw new Exception($"No recognized Keyframe Type at {num}.");
            }
        }

        #endregion

        #region Set Values

        public void SetKeyframeColor(BeatmapObject beatmapObject, int index, int value, List<Toggle> colorButtons, IEnumerable<TimelineObject> selected)
        {
            foreach (var keyframe in selected.Select(x => x.GetData<EventKeyframe>()))
            {
                keyframe.eventValues[index] = value;
                if (!RTEditor.ShowModdedUI)
                    keyframe.eventValues[6] = 10f; // set behaviour to alpha's default if editor complexity is not set to advanced.
            }

            // Since keyframe color has no affect on the timeline object, we will only need to update the physical object.
            if (UpdateObjects)
                Updater.UpdateObject(beatmapObject, "Keyframes");

            int num = 0;
            foreach (var toggle in colorButtons)
            {
                int tmpIndex = num;
                toggle.onValueChanged.ClearAll();
                toggle.isOn = num == value;
                toggle.onValueChanged.AddListener(_val => { SetKeyframeColor(beatmapObject, index, tmpIndex, colorButtons, selected); });
                num++;
            }
        }

        public void SetKeyframeRandomColorTarget(BeatmapObject beatmapObject, int index, int value, Toggle[] toggles)
        {
            beatmapObject.events[3][ObjEditor.inst.currentKeyframe].eventRandomValues[index] = (float)value;

            // Since keyframe color has no affect on the timeline object, we will only need to update the physical object.
            Updater.UpdateObject(beatmapObject, "Keyframes");

            int num = 0;
            foreach (var toggle in toggles)
            {
                int tmpIndex = num;
                toggle.onValueChanged.ClearAll();
                toggle.isOn = num == value;
                toggle.onValueChanged.AddListener(_val => { SetKeyframeRandomColorTarget(beatmapObject, index, tmpIndex, toggles); });
                num++;
            }
        }

        #endregion
    }
}
