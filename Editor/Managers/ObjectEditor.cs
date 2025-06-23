using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

using LSFunctions;

using TMPro;
using SimpleJSON;
using Crosstales.FB;

using BetterLegacy.Companion.Data.Parameters;
using BetterLegacy.Companion.Entity;
using BetterLegacy.Configs;
using BetterLegacy.Core;
using BetterLegacy.Core.Animation;
using BetterLegacy.Core.Animation.Keyframe;
using BetterLegacy.Core.Components;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using BetterLegacy.Core.Runtime;
using BetterLegacy.Core.Prefabs;
using BetterLegacy.Editor.Data;
using BetterLegacy.Editor.Data.Dialogs;

namespace BetterLegacy.Editor.Managers
{
    public class ObjectEditor : MonoBehaviour
    {
        #region Init

        public static ObjectEditor inst;

        public static void Init() => ObjEditor.inst.gameObject.AddComponent<ObjectEditor>();

        void Awake()
        {
            inst = this;

            try
            {
                Dialog = new ObjectEditorDialog();
                Dialog.Init();

                Dialog.TimelinePosScrollbar.onValueChanged.NewListener(_val =>
                {
                    Dialog.Timeline.GetComponent<ScrollRect>().horizontalNormalizedPosition = _val;
                    if (EditorTimeline.inst.CurrentSelection.isBeatmapObject)
                        EditorTimeline.inst.CurrentSelection.TimelinePosition = _val;
                });

                var idRight = ObjEditor.inst.objTimelineContent.parent.Find("id/right");
                for (int i = 0; i < ObjEditor.inst.TimelineParents.Count; i++)
                {
                    var type = i;
                    var entry = TriggerHelper.CreateEntry(EventTriggerType.PointerUp, eventData =>
                    {
                        if (((PointerEventData)eventData).button != PointerEventData.InputButton.Right)
                            return;

                        var timeTmp = MouseTimelineCalc();

                        var beatmapObject = EditorTimeline.inst.CurrentSelection.GetData<BeatmapObject>();

                        var eventKeyfame = CreateEventKeyframe(beatmapObject, timeTmp, type, beatmapObject.events[type].FindLast(x => x.time <= timeTmp), false);
                        UpdateKeyframeOrder(beatmapObject);

                        RenderKeyframes(beatmapObject);

                        var keyframe = beatmapObject.events[type].FindLastIndex(x => x.id == eventKeyfame.id);
                        if (keyframe < 0)
                            keyframe = 0;

                        SetCurrentKeyframe(beatmapObject, type, keyframe, false, InputDataManager.inst.editorActions.MultiSelect.IsPressed);
                        ResizeKeyframeTimeline(beatmapObject);

                        RenderObjectKeyframesDialog(beatmapObject);
                        RenderMarkers(beatmapObject);

                        // Keyframes affect both physical object and timeline object.
                        EditorTimeline.inst.RenderTimelineObject(EditorTimeline.inst.GetTimelineObject(beatmapObject));
                        if (UpdateObjects)
                            RTLevel.Current?.UpdateObject(beatmapObject, RTLevel.ObjectContext.KEYFRAMES);
                    });

                    var comp = ObjEditor.inst.TimelineParents[type].GetComponent<EventTrigger>();
                    comp.triggers.RemoveAll(x => x.eventID == EventTriggerType.PointerUp);
                    comp.triggers.Add(entry);

                    EditorThemeManager.AddGraphic(idRight.GetChild(type).GetComponent<Image>(), EditorTheme.GetGroup($"Object Keyframe Color {type + 1}"));
                }

                ObjEditor.inst.objTimelineSlider.onValueChanged.ClearAll();
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

                TriggerHelper.AddEventTriggers(Dialog.TimelinePosScrollbar.gameObject, TriggerHelper.CreateEntry(EventTriggerType.Scroll, baseEventData =>
                {
                    var pointerEventData = (PointerEventData)baseEventData;

                    var scrollBar = Dialog.TimelinePosScrollbar;
                    float multiply = Input.GetKey(KeyCode.LeftAlt) ? 0.1f : Input.GetKey(KeyCode.LeftControl) ? 10f : 1f;

                    scrollBar.value = pointerEventData.scrollDelta.y > 0f ? scrollBar.value + (0.005f * multiply) : pointerEventData.scrollDelta.y < 0f ? scrollBar.value - (0.005f * multiply) : 0f;
                }));

            }
            catch (Exception ex)
            {
                CoreHelper.LogException(ex);
            } // init dialog

            try
            {
                AnimationEditor.Init();
            }
            catch (Exception ex)
            {
                CoreHelper.LogException(ex);
            }

            ApplyConfig();

            try
            {
                LoadObjectTemplates();
            }
            catch (Exception ex)
            {
                CoreHelper.LogException(ex);
            } // load object templates

            LoadGlobalCopy();
        }

        public void ApplyConfig()
        {
            ObjEditor.inst.SelectedColor = EditorConfig.Instance.ObjectSelectionColor.Value;
            ObjEditor.inst.ObjectLengthOffset = EditorConfig.Instance.KeyframeEndLengthOffset.Value;
        }

        #endregion

        #region Variables

        public ObjectEditorDialog Dialog { get; set; }

        public bool movingTimeline;
        public Vector2 cachedTimelinePos;

        public GameObject shapeButtonPrefab;

        public Prefab copy;

        public List<TimelineKeyframe> copiedObjectKeyframes = new List<TimelineKeyframe>();

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

        #region Timeline

        /// <summary>
        /// Sets the Object Keyframe timeline position.
        /// </summary>
        /// <param name="position">The position to set the timeline scroll.</param>
        public void SetTimelinePosition(float position) => SetTimeline(ObjEditor.inst.zoomFloat, position);

        /// <summary>
        /// Sets the Object Keyframe timeline zoom.
        /// </summary>
        /// <param name="zoom">The zoom to set to the timeline.</param>
        public void SetTimelineZoom(float zoom)
        {
            var beatmapObject = EditorTimeline.inst.CurrentSelection.GetData<BeatmapObject>();
            float timelineCalc = ObjEditor.inst.objTimelineSlider.value;
            if (AudioManager.inst.CurrentAudioSource.clip)
            {
                float time = -beatmapObject.StartTime + AudioManager.inst.CurrentAudioSource.time;
                float objectLifeLength = beatmapObject.GetObjectLifeLength(ObjEditor.inst.ObjectLengthOffset);

                timelineCalc = time / objectLifeLength;
            }

            SetTimeline(zoom, timelineCalc);
        }

        /// <summary>
        /// Sets the Object Keyframe timeline zoom and position.
        /// </summary>
        /// <param name="zoom">The amount to zoom in.</param>
        /// <param name="position">The position to set the timeline scroll. If the value is less that 0, it will automatically calculate the position to match the audio time.</param>
        /// <param name="render">If the timeline should render.</param>
        public void SetTimeline(float zoom, float position, bool render = true)
        {
            float prevZoom = ObjEditor.inst.zoomFloat;
            ObjEditor.inst.zoomFloat = Mathf.Clamp01(zoom);
            ObjEditor.inst.zoomVal =
                LSMath.InterpolateOverCurve(ObjEditor.inst.ZoomCurve, ObjEditor.inst.zoomBounds.x, ObjEditor.inst.zoomBounds.y, ObjEditor.inst.zoomFloat);

            var beatmapObject = EditorTimeline.inst.CurrentSelection.GetData<BeatmapObject>();
            EditorTimeline.inst.CurrentSelection.Zoom = ObjEditor.inst.zoomFloat;
            EditorTimeline.inst.CurrentSelection.TimelinePosition = position;

            if (render)
            {
                ResizeKeyframeTimeline(beatmapObject);
                RenderKeyframes(beatmapObject);
                RenderMarkerPositions(beatmapObject);
            }

            Dialog.TimelinePosScrollbar.SetValueWithoutNotify(position);
            Dialog.TimelinePosScrollbar.onValueChanged.NewListener(SetTimelineScroll);
            SetTimelineScroll(position);

            ObjEditor.inst.zoomSlider.onValueChanged.ClearAll();
            ObjEditor.inst.zoomSlider.value = ObjEditor.inst.zoomFloat;
            ObjEditor.inst.zoomSlider.onValueChanged.AddListener(_val =>
            {
                ObjEditor.inst.Zoom = _val;
                EditorTimeline.inst.CurrentSelection.Zoom = Mathf.Clamp01(_val);
            });
        }

        void SetTimelineScroll(float scroll)
        {
            Dialog.Timeline.GetComponent<ScrollRect>().horizontalNormalizedPosition = scroll;
            EditorTimeline.inst.CurrentSelection.TimelinePosition = scroll;
        }

        public static float TimeTimelineCalc(float _time) => _time * 14f * ObjEditor.inst.zoomVal + 5f;

        public static float MouseTimelineCalc()
        {
            float num = Screen.width * ((1155f - Mathf.Abs(ObjEditor.inst.timelineScroll.transform.AsRT().anchoredPosition.x) + 7f) / 1920f);
            float screenScale = 1f / (Screen.width / 1920f);
            float mouseX = Input.mousePosition.x < num ? num : Input.mousePosition.x;

            return (mouseX - num) / ObjEditor.inst.Zoom / 14f * screenScale;
        }

        void HandleTimelineDrag()
        {
            if (Input.GetMouseButtonUp((int)PointerEventData.InputButton.Middle))
                movingTimeline = false;

            if (!movingTimeline)
                return;

            var vector = Input.mousePosition * CoreHelper.ScreenScaleInverse;
            float multiply = 12f / ObjEditor.inst.Zoom;
            SetTimelinePosition(cachedTimelinePos.x + -(((vector.x - ObjEditor.inst.DragStartPos.x) / Screen.width) * multiply));
        }

        public void StartTimelineDrag()
        {
            cachedTimelinePos = new Vector2(Dialog.TimelinePosScrollbar.value, 0f);
            movingTimeline = true;
        }

        #endregion

        #region Dragging

        void Update()
        {
            Dialog?.ModifiersDialog?.Tick();

            if (!ObjEditor.inst.changingTime && EditorTimeline.inst.CurrentSelection && EditorTimeline.inst.CurrentSelection.isBeatmapObject)
            {
                // Sets new audio time using the Object Keyframe timeline cursor.
                ObjEditor.inst.newTime = Mathf.Clamp(AudioManager.inst.CurrentAudioSource.time,
                    EditorTimeline.inst.CurrentSelection.Time,
                    EditorTimeline.inst.CurrentSelection.Time + EditorTimeline.inst.CurrentSelection.GetData<BeatmapObject>().GetObjectLifeLength(ObjEditor.inst.ObjectLengthOffset));
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
            HandleTimelineDrag();
        }

        void HandleObjectsDrag()
        {
            if (!ObjEditor.inst.beatmapObjectsDrag)
                return;

            var musicLength = SoundManager.inst.MusicLength;
            var selectedObjects = EditorTimeline.inst.SelectedObjects;

            if (InputDataManager.inst.editorActions.MultiSelect.IsPressed)
            {
                int binOffset = 14 - Mathf.RoundToInt((float)((Input.mousePosition.y - 25) * EditorManager.inst.ScreenScaleInverse / 20)) + ObjEditor.inst.mouseOffsetYForDrag;

                bool hasChanged = false;

                foreach (var timelineObject in selectedObjects)
                {
                    if (timelineObject.Locked)
                        continue;

                    int binCalc = EditorTimeline.inst.CalculateMaxBin(binOffset + timelineObject.binOffset);

                    if (timelineObject.Bin != binCalc)
                        hasChanged = true;

                    timelineObject.Bin = binCalc;
                    timelineObject.RenderPosLength();
                    if (timelineObject.isBeatmapObject && selectedObjects.Count == 1)
                        RenderBin(timelineObject.GetData<BeatmapObject>());
                    if (timelineObject.isPrefabObject && selectedObjects.Count == 1)
                        RTPrefabEditor.inst.RenderPrefabObjectBin(timelineObject.GetData<PrefabObject>());
                    if (timelineObject.isBackgroundObject && selectedObjects.Count == 1)
                        RTBackgroundEditor.inst.RenderBin(timelineObject.GetData<BackgroundObject>());
                }

                if (RTEditor.inst.dragBinOffset != binOffset && !selectedObjects.All(x => x.Locked))
                {
                    if (hasChanged && RTEditor.DraggingPlaysSound)
                        SoundManager.inst.PlaySound(DefaultSounds.UpDown, 0.4f, 0.6f);

                    RTEditor.inst.dragBinOffset = binOffset;
                }

                return;
            }

            float timeOffset = EditorTimeline.inst.GetTimelineTime(RTEditor.inst.editorInfo.bpmSnapActive && EditorConfig.Instance.BPMSnapsObjects.Value) + ObjEditor.inst.mouseOffsetXForDrag;
            if (EditorConfig.Instance.ClampedTimelineDrag.Value)
                timeOffset = Mathf.Clamp(timeOffset, 0f, musicLength);
            timeOffset = Mathf.Round(timeOffset * 1000f) / 1000f;

            if (RTEditor.inst.dragOffset != timeOffset && !EditorTimeline.inst.SelectedObjects.All(x => x.Locked))
            {
                if (RTEditor.DraggingPlaysSound && (RTEditor.inst.editorInfo.bpmSnapActive || !RTEditor.DraggingPlaysSoundBPM))
                    SoundManager.inst.PlaySound(DefaultSounds.LeftRight, RTEditor.inst.editorInfo.bpmSnapActive ? 0.6f : 0.1f, 0.7f);

                RTEditor.inst.dragOffset = timeOffset;
            }

            foreach (var timelineObject in selectedObjects)
            {
                if (timelineObject.Locked)
                    continue;

                var time = timeOffset + timelineObject.timeOffset;
                if (EditorConfig.Instance.ClampedTimelineDrag.Value)
                    time = Mathf.Clamp(time, 0f, musicLength);

                timelineObject.Time = time;

                timelineObject.RenderPosLength();
                
                switch (timelineObject.TimelineReference)
                {
                    case TimelineObject.TimelineReferenceType.BeatmapObject: {
                            var beatmapObject = timelineObject.GetData<BeatmapObject>();

                            var runtimeObject = beatmapObject.runtimeObject;

                            if (runtimeObject)
                            {
                                runtimeObject.StartTime = beatmapObject.StartTime;
                                runtimeObject.KillTime = beatmapObject.StartTime + beatmapObject.SpawnDuration;

                                runtimeObject.SetActive(beatmapObject.Alive);

                                for (int i = 0; i < runtimeObject.parentObjects.Count; i++)
                                {
                                    var levelParent = runtimeObject.parentObjects[i];
                                    var parent = levelParent.beatmapObject;

                                    levelParent.timeOffset = parent.StartTime;
                                }
                            }

                            if (selectedObjects.Count == 1)
                            {
                                RenderStartTime(beatmapObject);
                                ResizeKeyframeTimeline(beatmapObject);
                                RenderMarkerPositions(beatmapObject);
                            }
                            break;
                        }
                    case TimelineObject.TimelineReferenceType.PrefabObject: {
                            var prefabObject = timelineObject.GetData<PrefabObject>();
                            RTPrefabEditor.inst.RenderPrefabObjectStartTime(prefabObject);
                            RTLevel.Current?.UpdatePrefab(prefabObject, RTLevel.PrefabContext.TIME, false);
                            break;
                        }
                    case TimelineObject.TimelineReferenceType.BackgroundObject: {
                            var backgroundObject = timelineObject.GetData<BackgroundObject>();

                            var runtimeObject = backgroundObject.runtimeObject;

                            if (runtimeObject)
                            {
                                runtimeObject.StartTime = backgroundObject.StartTime;
                                runtimeObject.KillTime = backgroundObject.StartTime + backgroundObject.SpawnDuration;

                                runtimeObject.SetActive(backgroundObject.Alive);
                            }

                            if (selectedObjects.Count == 1)
                                RTBackgroundEditor.inst.RenderStartTime(backgroundObject);
                            break;
                        }
                }
            }

            RTLevel.Current?.Sort();
            RTLevel.Current?.backgroundEngine?.spawner?.RecalculateObjectStates();

            if (EditorConfig.Instance.UpdateHomingKeyframesDrag.Value && RTLevel.Current)
                System.Threading.Tasks.Task.Run(RTLevel.Current.UpdateHomingKeyframes);
        }

        void HandleKeyframesDrag()
        {
            if (!ObjEditor.inst.timelineKeyframesDrag || !EditorTimeline.inst.CurrentSelection.isBeatmapObject)
                return;

            var beatmapObject = EditorTimeline.inst.CurrentSelection.GetData<BeatmapObject>();

            var snap = EditorConfig.Instance.BPMSnapsObjectKeyframes.Value;
            var timelineCalc = MouseTimelineCalc();
            var selected = EditorTimeline.inst.CurrentSelection.InternalTimelineObjects.Where(x => x.Selected);
            var startTime = beatmapObject.StartTime;

            float timeOffset = Mathf.Round(Mathf.Clamp(timelineCalc + ObjEditor.inst.mouseOffsetXForKeyframeDrag, 0f, AudioManager.inst.CurrentAudioSource.clip.length) * 1000f) / 1000f;
            if (RTEditor.inst.editorInfo.bpmSnapActive && EditorConfig.Instance.BPMSnapsObjectKeyframes.Value && !Input.GetKey(KeyCode.LeftAlt))
                timeOffset = -(startTime - RTEditor.SnapToBPM(startTime + timeOffset));

            bool changed = false;
            foreach (var timelineKeyframe in selected)
            {
                if (timelineKeyframe.Index == 0 || timelineKeyframe.Locked)
                    continue;

                float calc = Mathf.Clamp(timeOffset + timelineKeyframe.timeOffset, 0f, beatmapObject.GetObjectLifeLength(ObjEditor.inst.ObjectLengthOffset));

                timelineKeyframe.eventKeyframe.time = calc;

                timelineKeyframe.GameObject.transform.AsRT().anchoredPosition = new Vector2(TimeTimelineCalc(startTime), 0f);

                timelineKeyframe.Render();
                changed = true;
            }

            RTLevel.Current?.UpdateObject(beatmapObject, RTLevel.ObjectContext.KEYFRAMES);
            RTLevel.Current?.UpdateObject(beatmapObject, RTLevel.ObjectContext.AUTOKILL);
            RenderObjectKeyframesDialog(beatmapObject);
            ResizeKeyframeTimeline(beatmapObject);

            RenderMarkerPositions(beatmapObject);

            foreach (var timelineObject in EditorTimeline.inst.SelectedBeatmapObjects)
                EditorTimeline.inst.RenderTimelineObject(timelineObject);

            if (changed && !selected.All(x => x.Locked) && RTEditor.inst.dragOffset != timelineCalc + ObjEditor.inst.mouseOffsetXForDrag)
            {
                if (RTEditor.DraggingPlaysSound && (RTEditor.inst.editorInfo.bpmSnapActive && snap || !RTEditor.DraggingPlaysSoundBPM))
                    SoundManager.inst.PlaySound(DefaultSounds.LeftRight, RTEditor.inst.editorInfo.bpmSnapActive && snap ? 0.6f : 0.1f, 0.8f);

                RTEditor.inst.dragOffset = timelineCalc + ObjEditor.inst.mouseOffsetXForDrag;
            }
        }

        #endregion

        #region Deleting

        public IEnumerator DeleteKeyframes()
        {
            if (EditorTimeline.inst.CurrentSelection.isBeatmapObject)
                yield return DeleteKeyframes(EditorTimeline.inst.CurrentSelection.GetData<BeatmapObject>());
            yield break;
        }

        public IEnumerator DeleteKeyframes(BeatmapObject beatmapObject)
        {
            var bmTimelineObject = EditorTimeline.inst.GetTimelineObject(beatmapObject);

            var list = bmTimelineObject.InternalTimelineObjects.Where(x => x.Selected).ToList();
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
                    strs.Add(timelineObject.eventKeyframe.id);
            }

            for (int i = 0; i < beatmapObject.events.Count; i++)
                beatmapObject.events[i].RemoveAll(x => strs.Contains(x.id));

            bmTimelineObject.InternalTimelineObjects.Where(x => x.Selected).ToList().ForEach(x => Destroy(x.GameObject));
            bmTimelineObject.InternalTimelineObjects.RemoveAll(x => x.Selected);

            EditorTimeline.inst.RenderTimelineObject(bmTimelineObject);
            RTLevel.Current?.UpdateObject(beatmapObject, RTLevel.ObjectContext.KEYFRAMES);

            if (beatmapObject.autoKillType == AutoKillType.LastKeyframe || beatmapObject.autoKillType == AutoKillType.LastKeyframeOffset)
                RTLevel.Current?.UpdateObject(beatmapObject, RTLevel.ObjectContext.AUTOKILL);

            RenderKeyframes(beatmapObject);

            if (count == 1 || allOfTheSameType)
                SetCurrentKeyframe(beatmapObject, type, Mathf.Clamp(index - 1, 0, beatmapObject.events[type].Count - 1));
            else
                SetCurrentKeyframe(beatmapObject, type, 0);

            ResizeKeyframeTimeline(beatmapObject);
            RenderMarkers(beatmapObject);

            EditorManager.inst.DisplayNotification("Deleted Object Keyframes [ " + count + " ]", 2f, EditorManager.NotificationType.Success);

            yield break;
        }

        #endregion

        #region Copy / Paste

        /// <summary>
        /// Loads the globally copied file.
        /// </summary>
        public void LoadGlobalCopy()
        {
            try
            {
                var prefabFilePath = RTFile.CombinePaths(Application.persistentDataPath, $"copied_objects{FileFormat.LSP.Dot()}");
                if (!RTFile.FileExists(prefabFilePath))
                    return;

                var jn = JSON.Parse(RTFile.ReadFromFile(prefabFilePath));
                copy = Prefab.Parse(jn);
                ObjEditor.inst.hasCopiedObject = true;
            }
            catch (Exception ex)
            {
                CoreHelper.LogError($"Could not load global copied objects.\n{ex}");
            } // load global copy
        }

        public void CopyObjects()
        {
            var selected = EditorTimeline.inst.SelectedObjects;

            float start = 0f;
            if (EditorConfig.Instance.PasteOffset.Value)
                start = -AudioManager.inst.CurrentAudioSource.time + selected.Min(x => x.Time);

            var copy = new Prefab("copied prefab", 0, start,
                selected.Where(x => x.isBeatmapObject).Select(x => x.GetData<BeatmapObject>()).ToList(),
                selected.Where(x => x.isPrefabObject).Select(x => x.GetData<PrefabObject>()).ToList(),
                null,
                selected.Where(x => x.isBackgroundObject).Select(x => x.GetData<BackgroundObject>()).ToList());

            copy.description = "Take me wherever you go!";
            this.copy = copy;
            ObjEditor.inst.hasCopiedObject = true;

            if (EditorConfig.Instance.CopyPasteGlobal.Value && RTFile.DirectoryExists(Application.persistentDataPath))
                RTFile.WriteToFile(RTFile.CombinePaths(Application.persistentDataPath, $"copied_objects{FileFormat.LSP.Dot()}"), copy.ToJSON().ToString());
        }

        public void PasteObject() => PasteObject(0f);

        public void PasteObject(float offsetTime) => PasteObject(offsetTime, false);

        public void PasteObject(float offsetTime, bool regen) => PasteObject(offsetTime, false, regen);

        public void PasteObject(float offsetTime, bool dup, bool regen)
        {
            if (!ObjEditor.inst.hasCopiedObject || !copy || (copy.prefabObjects.IsEmpty() && copy.beatmapObjects.IsEmpty() && copy.backgroundObjects.IsEmpty()))
            {
                EditorManager.inst.DisplayNotification("No copied object yet!", 1f, EditorManager.NotificationType.Error, false);
                return;
            }

            EditorTimeline.inst.DeselectAllObjects();
            EditorManager.inst.DisplayNotification("Pasting objects.", 1f, EditorManager.NotificationType.Success);

            new PrefabExpander(copy)
                .Select()
                .Offset(offsetTime)
                .OffsetToCurrentTime(!dup)
                .Regen(regen)
                .AddBin(dup)
                .Expand();
        }

        public void CopyAllSelectedEvents(BeatmapObject beatmapObject)
        {
            copiedObjectKeyframes.Clear();
            UpdateKeyframeOrder(beatmapObject);

            var bmTimelineObject = EditorTimeline.inst.GetTimelineObject(beatmapObject);

            float num = bmTimelineObject.InternalTimelineObjects.Where(x => x.Selected).Min(x => x.Time);

            foreach (var timelineObject in bmTimelineObject.InternalTimelineObjects.Where(x => x.Selected))
            {
                int type = timelineObject.Type;
                int index = timelineObject.Index;
                var eventKeyframe = beatmapObject.events[type][index].Copy();
                eventKeyframe.time -= num;

                copiedObjectKeyframes.Add(new TimelineKeyframe(eventKeyframe) { Type = type, Index = index, isObjectKeyframe = true });
            }
        }

        public void PasteKeyframes(bool setTime = true)
        {
            if (EditorTimeline.inst.CurrentSelection.isBeatmapObject)
                PasteKeyframes(EditorTimeline.inst.CurrentSelection.GetData<BeatmapObject>(), setTime);
        }

        public void PasteKeyframes(BeatmapObject beatmapObject, bool setTime = true) => PasteKeyframes(beatmapObject, copiedObjectKeyframes, setTime);

        public void PasteKeyframes(BeatmapObject beatmapObject, List<TimelineKeyframe> kfs, bool setTime = true)
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
            RenderMarkers(beatmapObject);

            if (EditorConfig.Instance.SelectPasted.Value)
            {
                var timelineObject = EditorTimeline.inst.GetTimelineObject(beatmapObject);
                foreach (var kf in timelineObject.InternalTimelineObjects)
                    kf.Selected = ids.Contains(kf.ID);
            }

            RenderObjectKeyframesDialog(beatmapObject);
            EditorTimeline.inst.RenderTimelineObject(EditorTimeline.inst.GetTimelineObject(beatmapObject));

            if (UpdateObjects)
            {
                RTLevel.Current?.UpdateObject(beatmapObject, RTLevel.ObjectContext.KEYFRAMES);
                RTLevel.Current?.UpdateObject(beatmapObject, RTLevel.ObjectContext.AUTOKILL);
            }
        }

        public EventKeyframe PasteKF(BeatmapObject beatmapObject, TimelineKeyframe timelineKeyframe, bool setTime = true)
        {
            var eventKeyframe = timelineKeyframe.eventKeyframe.Copy();

            var time = EditorManager.inst.CurrentAudioPos;
            if (RTEditor.inst.editorInfo.bpmSnapActive && EditorConfig.Instance.BPMSnapsPasted.Value)
                time = RTEditor.SnapToBPM(time);

            if (!setTime)
                return eventKeyframe;

            eventKeyframe.time = time - beatmapObject.StartTime + eventKeyframe.time;
            if (eventKeyframe.time <= 0f)
                eventKeyframe.time = 0.001f;

            return eventKeyframe;
        }

        public EventKeyframe GetCopiedData(int type) => type switch
        {
            0 => CopiedPositionData,
            1 => CopiedScaleData,
            2 => CopiedRotationData,
            3 => CopiedColorData,
            _ => null,
        };

        public void CopyData(int type, EventKeyframe kf)
        {
            switch (type)
            {
                case 0: CopiedPositionData = kf.Copy();
                    break;
                case 1: CopiedScaleData = kf.Copy();
                    break;
                case 2: CopiedRotationData = kf.Copy();
                    break;
                case 3: CopiedColorData = kf.Copy();
                    break;
            }
        }

        public void SetCopiedData(int type, EventKeyframe kf) => SetData(kf, GetCopiedData(type));

        public void SetData(EventKeyframe kf, EventKeyframe copiedData)
        {
            if (copiedData == null)
                return;

            kf.curve = copiedData.curve;
            kf.values = copiedData.values.Copy();
            kf.randomValues = copiedData.randomValues.Copy();
            kf.random = copiedData.random;
            kf.relative = copiedData.relative;
        }

        public void PasteKeyframeData(int type, IEnumerable<TimelineKeyframe> selected, BeatmapObject beatmapObject)
        {
            var copiedData = GetCopiedData(type);
            var name = type switch
            {
                0 => "Position",
                1 => "Scale",
                2 => "Rotation",
                3 => "Color",
                _ => "Null",
            };

            if (copiedData == null)
            {
                EditorManager.inst.DisplayNotification($"{name} keyframe data not copied yet.", 2f, EditorManager.NotificationType.Error);
                return;
            }

            foreach (var timelineObject in selected)
            {
                if (timelineObject.Type == type)
                    SetData(timelineObject.eventKeyframe, copiedData);
            }

            RenderKeyframes(beatmapObject);
            RenderObjectKeyframesDialog(beatmapObject);
            RTLevel.Current?.UpdateObject(beatmapObject, RTLevel.ObjectContext.KEYFRAMES);
            EditorManager.inst.DisplayNotification($"Pasted {name.ToLower()} keyframe data to current selected keyframe.", 2f, EditorManager.NotificationType.Success);
        }

        #endregion

        #region Create New Objects

        /// <summary>
        /// List of extra options used to create objects.
        /// </summary>
        public List<ObjectOption> objectOptions = new List<ObjectOption>()
        {
            new ObjectOption("Normal", "A regular square object that hits the player.", null),
            new ObjectOption("Helper", "A regular square object that is transparent and doesn't hit the player. This can be used to warn players of an attack.", timelineObject =>
            {
                var bm = timelineObject.GetData<BeatmapObject>();
                bm.objectType = BeatmapObject.ObjectType.Helper;
                bm.name = nameof(BeatmapObject.ObjectType.Helper);
            }),
            new ObjectOption("Decoration", "A regular square object that is opaque and doesn't hit the player.", timelineObject =>
            {
                var bm = timelineObject.GetData<BeatmapObject>();
                bm.objectType = BeatmapObject.ObjectType.Decoration;
                bm.name = nameof(BeatmapObject.ObjectType.Decoration);
            }),
            new ObjectOption("Solid", "A regular square object that doesn't allow the player to passh through.", timelineObject =>
            {
                var bm = timelineObject.GetData<BeatmapObject>();
                bm.objectType = BeatmapObject.ObjectType.Solid;
                bm.name = nameof(BeatmapObject.ObjectType.Solid);
            }),
            new ObjectOption("Alpha Helper", "A regular square object that is transparent and doesn't hit the player. This can be used to warn players of an attack.", timelineObject =>
            {
                var bm = timelineObject.GetData<BeatmapObject>();
                bm.objectType = BeatmapObject.ObjectType.Decoration;
                bm.name = nameof(BeatmapObject.ObjectType.Helper);
                bm.events[3][0].values[1] = 0.65f;
            }),
            new ObjectOption("Empty Hitbox", "A square object that is invisible but still has a collision and can hit the player.", timelineObject =>
            {
                var bm = timelineObject.GetData<BeatmapObject>();
                bm.objectType = BeatmapObject.ObjectType.Normal;
                bm.name = "Collision";
                bm.events[3][0].values[1] = 1f;
            }),
            new ObjectOption("Empty Solid", "A square object that is invisible but still has a collision and prevents the player from passing through.", timelineObject =>
            {
                var bm = timelineObject.GetData<BeatmapObject>();
                bm.objectType = BeatmapObject.ObjectType.Solid;
                bm.name = "Collision";
                bm.events[3][0].values[1] = 1f;
            }),
            new ObjectOption("Text", "A text object that can be used for dialogue.", timelineObject =>
            {
                var bm = timelineObject.GetData<BeatmapObject>();
                bm.objectType = BeatmapObject.ObjectType.Decoration;
                bm.name = "Text";
                bm.text = "A text object that can be used for dialogue.";
                bm.shape = 4;
                bm.shapeOption = 0;
            }),
            new ObjectOption("Text Sequence", "A text object that can be used for dialogue. Includes a textSequence modifier.", timelineObject =>
            {
                var bm = timelineObject.GetData<BeatmapObject>();
                bm.objectType = BeatmapObject.ObjectType.Decoration;
                bm.name = "Text";
                bm.text = "A text object that can be used for dialogue. Includes a textSequence modifier.";
                bm.shape = 4;
                bm.shapeOption = 0;
                if (ModifiersManager.defaultBeatmapObjectModifiers.TryFind(x => x.Name == "textSequence", out ModifierBase defaultModifier) && defaultModifier is Modifier<BeatmapObject> modifier)
                    bm.modifiers.Add(modifier.Copy(true, bm));
            }),
        };

        /// <summary>
        /// List of custom object templates.
        /// </summary>
        public List<ObjectOption> customObjectOptions = new List<ObjectOption>();

        /// <summary>
        /// Loads the custom object templates list.
        /// </summary>
        public void LoadObjectTemplates()
        {
            var filePath = RTFile.CombinePaths(RTEditor.inst.BeatmapsPath, $"create_object_templates{FileFormat.JSON.Dot()}");
            if (!RTFile.FileExists(filePath))
                return;

            customObjectOptions.Clear();
            var jn = JSON.Parse(RTFile.ReadFromFile(filePath));

            for (int i = 0; i < jn["objects"].Count; i++)
            {
                var data = jn["data"];
                customObjectOptions.Add(new ObjectOption(jn["name"], jn["desc"], timelineObject => timelineObject.GetData<BeatmapObject>().ReadJSON(data)));
            }
        }

        /// <summary>
        /// Adds a Beatmap Object to the custom object templates.
        /// </summary>
        /// <param name="beatmapObject">Object to create a template of.</param>
        /// <param name="name">Name of the template.</param>
        /// <param name="desc">Description of the template.</param>
        public void AddObjectTemplate(BeatmapObject beatmapObject, string name, string desc)
        {
            var filePath = RTFile.CombinePaths(RTEditor.inst.BeatmapsPath, $"create_object_templates{FileFormat.JSON.Dot()}");
            var jn = !RTFile.FileExists(filePath) ? Parser.NewJSONObject() : JSON.Parse(RTFile.ReadFromFile(filePath));

            var jnObject = Parser.NewJSONObject();
            jnObject["name"] = name;
            jnObject["desc"] = desc;
            jnObject["data"] = beatmapObject.ToJSON();

            jn["objects"][jn["objects"].Count] = jnObject;

            RTFile.WriteToFile(filePath, jn.ToString());
        }

        /// <summary>
        /// Shows extra object templates.
        /// </summary>
        public void ShowObjectTemplates()
        {
            RTEditor.inst.ObjectTemplatePopup.Open();
            RTEditor.inst.ObjectTemplatePopup.UpdateSearchFunction(RefreshObjectTemplates);
            RefreshObjectTemplates(RTEditor.inst.ObjectTemplatePopup.SearchField.text);
        }

        /// <summary>
        /// Refreshes the list of extra object templates.
        /// </summary>
        /// <param name="search">The search term.</param>
        public void RefreshObjectTemplates(string search)
        {
            RTEditor.inst.ObjectTemplatePopup.ClearContent();
            var objectOptions = customObjectOptions.IsEmpty() ? this.objectOptions : this.objectOptions.Union(customObjectOptions).ToList();
            for (int i = 0; i < objectOptions.Count; i++)
            {
                if (!RTString.SearchString(search, objectOptions[i].name))
                    continue;

                var name = objectOptions[i].name;
                var hint = objectOptions[i].hint;

                var gameObject = EditorManager.inst.folderButtonPrefab.Duplicate(RTEditor.inst.ObjectTemplatePopup.Content, "Function");

                gameObject.AddComponent<HoverTooltip>().tooltipLangauges.Add(new HoverTooltip.Tooltip { desc = name, hint = hint });

                var button = gameObject.GetComponent<Button>();
                button.onClick.ClearAll();
                button.onClick.AddListener(objectOptions[i].Create);

                EditorThemeManager.ApplySelectable(button, ThemeGroup.List_Button_1);
                var text = gameObject.transform.GetChild(0).GetComponent<Text>();
                text.text = name;
                EditorThemeManager.ApplyLightText(text);
            }
        }

        /// <summary>
        /// Creates a new Beatmap Object and generates a Timeline Object for it.
        /// </summary>
        /// <param name="select">If the Timeline Object should be selected.</param>
        /// <returns>Returns the generated Timeline Object.</returns>
        public TimelineObject CreateNewDefaultObject(bool select = true)
        {
            if (!EditorManager.inst.hasLoadedLevel)
            {
                EditorManager.inst.DisplayNotification("Can't add objects to level until a level has been loaded!", 2f, EditorManager.NotificationType.Error);
                return null;
            }

            var beatmapObject = CreateNewBeatmapObject(AudioManager.inst.CurrentAudioSource.time);
            beatmapObject.autoKillType = AutoKillType.LastKeyframeOffset;
            beatmapObject.autoKillOffset = 5f;
            beatmapObject.orderModifiers = EditorConfig.Instance.CreateObjectModifierOrderDefault.Value;

            beatmapObject.parentType = EditorConfig.Instance.CreateObjectsScaleParentDefault.Value ? "111" : "101";

            if (EditorTimeline.inst.layerType == EditorTimeline.LayerType.Events)
                EditorTimeline.inst.SetLayer(beatmapObject.editorData.Layer, EditorTimeline.LayerType.Objects);

            GameData.Current.beatmapObjects.Add(beatmapObject);

            var timelineObject = EditorTimeline.inst.GetTimelineObject(beatmapObject);

            AudioManager.inst.SetMusicTime(AllowTimeExactlyAtStart ? AudioManager.inst.CurrentAudioSource.time : AudioManager.inst.CurrentAudioSource.time + 0.001f);

            if (select)
                EditorTimeline.inst.SetCurrentObject(timelineObject);

            return timelineObject;
        }

        /// <summary>
        /// Creates a new Beatmap Object with the default start keyframes.
        /// </summary>
        /// <param name="time">Time to create the object at.</param>
        /// <returns>Returns a new Beatmap Object.</returns>
        public BeatmapObject CreateNewBeatmapObject(float time)
        {
            var beatmapObject = new BeatmapObject(time);

            if (!Seasons.IsAprilFools)
                beatmapObject.editorData.Layer = EditorTimeline.inst.Layer;

            beatmapObject.events[0].Add(EventKeyframe.DefaultPositionKeyframe);
            beatmapObject.events[1].Add(EventKeyframe.DefaultScaleKeyframe);
            beatmapObject.events[2].Add(EventKeyframe.DefaultRotationKeyframe);
            beatmapObject.events[3].Add(EventKeyframe.DefaultColorKeyframe);

            return beatmapObject;
        }

        /// <summary>
        /// Creates a new beatmap object.
        /// </summary>
        /// <param name="action">Action to apply to the timeline object.</param>
        /// <param name="select">If the object should be selected.</param>
        /// <param name="setHistory">If undo / redo history should be set.</param>
        public void CreateNewObject(Action<TimelineObject> action = null, bool select = true, bool setHistory = true, bool recalculate = true, bool openDialog = true, bool exampleNotice = true)
        {
            var timelineObject = CreateNewDefaultObject(select);

            var bm = timelineObject.GetData<BeatmapObject>();
            if (EditorConfig.Instance.CreateObjectsatCameraCenter.Value)
            {
                var pos = EventManager.inst.cam.transform.position;

                bm.events[0][0].values[0] = pos.x;
                bm.events[0][0].values[1] = pos.y;
            }

            action?.Invoke(timelineObject);
            RTLevel.Current?.UpdateObject(bm, recalculate: recalculate);
            EditorTimeline.inst.RenderTimelineObject(timelineObject);
            EditorTimeline.inst.UpdateTransformIndex();

            if (openDialog)
                OpenDialog(bm);

            if (exampleNotice)
                Example.Current?.brain?.Notice(ExampleBrain.Notices.NEW_OBJECT, new BeatmapObjectNoticeParameters(bm, true));

            if (setHistory)
                EditorManager.inst.history.Add(new History.Command("Create New Object", () => CreateNewObject(action, select, false), () => EditorTimeline.inst.DeleteObject(timelineObject)));
        }

        public void CreateNewNormalObject(bool select = true, bool setHistory = true)
        {
            var timelineObject = CreateNewDefaultObject(select);

            var bm = timelineObject.GetData<BeatmapObject>();
            if (EditorConfig.Instance.CreateObjectsatCameraCenter.Value)
            {
                var pos = EventManager.inst.cam.transform.position;

                bm.events[0][0].values[0] = pos.x;
                bm.events[0][0].values[1] = pos.y;
            }

            RTLevel.Current?.UpdateObject(bm);
            EditorTimeline.inst.RenderTimelineObject(timelineObject);
            EditorTimeline.inst.UpdateTransformIndex();
            OpenDialog(bm);

            Example.Current?.brain?.Notice(ExampleBrain.Notices.NEW_OBJECT, new BeatmapObjectNoticeParameters(bm));

            if (!setHistory)
                return;

            EditorManager.inst.history.Add(new History.Command("Create New Normal Object", () => CreateNewNormalObject(select, false), () => EditorTimeline.inst.DeleteObject(timelineObject)));
        }

        public void CreateNewCircleObject(bool select = true, bool setHistory = true)
        {
            var timelineObject = CreateNewDefaultObject(select);

            var bm = timelineObject.GetData<BeatmapObject>();
            bm.shape = 1;
            bm.shapeOption = 0;
            bm.name = Seasons.IsAprilFools ? "<font=Arrhythmia>bro" : "circle";

            if (EditorConfig.Instance.CreateObjectsatCameraCenter.Value)
            {
                var pos = EventManager.inst.cam.transform.position;

                bm.events[0][0].values[0] = pos.x;
                bm.events[0][0].values[1] = pos.y;
            }

            RTLevel.Current?.UpdateObject(bm);
            EditorTimeline.inst.RenderTimelineObject(timelineObject);
            EditorTimeline.inst.UpdateTransformIndex();
            OpenDialog(bm);

            Example.Current?.brain?.Notice(ExampleBrain.Notices.NEW_OBJECT, new BeatmapObjectNoticeParameters(bm));

            if (!setHistory)
                return;

            EditorManager.inst.history.Add(new History.Command("Create New Normal Circle Object", () => CreateNewCircleObject(select, false), () => EditorTimeline.inst.DeleteObject(timelineObject)));
        }

        public void CreateNewTriangleObject(bool select = true, bool setHistory = true)
        {
            var timelineObject = CreateNewDefaultObject(select);

            var bm = timelineObject.GetData<BeatmapObject>();
            bm.shape = 2;
            bm.shapeOption = 0;
            bm.name = Seasons.IsAprilFools ? "baracuda <i>beat plays</i>" : "triangle";

            if (EditorConfig.Instance.CreateObjectsatCameraCenter.Value)
            {
                var pos = EventManager.inst.cam.transform.position;

                bm.events[0][0].values[0] = pos.x;
                bm.events[0][0].values[1] = pos.y;
            }

            RTLevel.Current?.UpdateObject(bm);
            EditorTimeline.inst.RenderTimelineObject(timelineObject);
            EditorTimeline.inst.UpdateTransformIndex();
            OpenDialog(bm);

            Example.Current?.brain?.Notice(ExampleBrain.Notices.NEW_OBJECT, new BeatmapObjectNoticeParameters(bm));

            if (!setHistory)
                return;

            EditorManager.inst.history.Add(new History.Command("Create New Normal Triangle Object", () => CreateNewTriangleObject(select, false), () => EditorTimeline.inst.DeleteObject(timelineObject)));
        }

        public void CreateNewTextObject(bool select = true, bool setHistory = true)
        {
            var timelineObject = CreateNewDefaultObject(select);

            var bm = timelineObject.GetData<BeatmapObject>();
            bm.shape = 4;
            bm.shapeOption = 0;
            bm.text = Seasons.IsAprilFools ? "Never gonna give you up<br>" +
                                            "Never gonna let you down<br>" +
                                            "Never gonna run around and desert you<br>" +
                                            "Never gonna make you cry<br>" +
                                            "Never gonna say goodbye<br>" +
                                            "Never gonna tell a lie and hurt you" : "text";
            bm.name = Seasons.IsAprilFools ? "Don't look at my text" : "text";
            bm.objectType = BeatmapObject.ObjectType.Decoration;
            if (Seasons.IsAprilFools)
                bm.StartTime += 1f;

            if (EditorConfig.Instance.CreateObjectsatCameraCenter.Value)
            {
                var pos = EventManager.inst.cam.transform.position;

                bm.events[0][0].values[0] = pos.x;
                bm.events[0][0].values[1] = pos.y;
            }

            RTLevel.Current?.UpdateObject(bm);
            EditorTimeline.inst.RenderTimelineObject(timelineObject);
            EditorTimeline.inst.UpdateTransformIndex();

            if (!Seasons.IsAprilFools)
                OpenDialog(bm);

            Example.Current?.brain?.Notice(ExampleBrain.Notices.NEW_OBJECT, new BeatmapObjectNoticeParameters(bm));

            if (!setHistory)
                return;

            EditorManager.inst.history.Add(new History.Command("Create New Normal Text Object", () => CreateNewTextObject(select, false), () => EditorTimeline.inst.DeleteObject(timelineObject)));
        }

        public void CreateNewHexagonObject(bool select = true, bool setHistory = true)
        {
            var timelineObject = CreateNewDefaultObject(select);

            var bm = timelineObject.GetData<BeatmapObject>();
            bm.shape = 5;
            bm.shapeOption = 0;
            bm.name = Seasons.IsAprilFools ? "super" : "hexagon";

            if (EditorConfig.Instance.CreateObjectsatCameraCenter.Value)
            {
                var pos = EventManager.inst.cam.transform.position;

                bm.events[0][0].values[0] = pos.x;
                bm.events[0][0].values[1] = pos.y;
            }

            RTLevel.Current?.UpdateObject(bm);
            EditorTimeline.inst.RenderTimelineObject(timelineObject);
            EditorTimeline.inst.UpdateTransformIndex();
            OpenDialog(bm);

            Example.Current?.brain?.Notice(ExampleBrain.Notices.NEW_OBJECT, new BeatmapObjectNoticeParameters(bm));

            if (!setHistory)
                return;

            EditorManager.inst.history.Add(new History.Command("Create New Normal Hexagon Object", () => CreateNewHexagonObject(select, false), () => EditorTimeline.inst.DeleteObject(timelineObject)));
        }

        public void CreateNewHelperObject(bool select = true, bool setHistory = true)
        {
            var timelineObject = CreateNewDefaultObject(select);

            var bm = timelineObject.GetData<BeatmapObject>();
            bm.name = Seasons.IsAprilFools ? "totally not deprecated object" : "helper";
            bm.objectType = Seasons.IsAprilFools ? BeatmapObject.ObjectType.Decoration : BeatmapObject.ObjectType.Helper;
            if (Seasons.IsAprilFools)
                bm.events[3][0].values[1] = 0.65f;

            if (EditorConfig.Instance.CreateObjectsatCameraCenter.Value)
            {
                var pos = EventManager.inst.cam.transform.position;

                bm.events[0][0].values[0] = pos.x;
                bm.events[0][0].values[1] = pos.y;
            }

            RTLevel.Current?.UpdateObject(bm);
            EditorTimeline.inst.RenderTimelineObject(timelineObject);
            EditorTimeline.inst.UpdateTransformIndex();
            OpenDialog(bm);

            Example.Current?.brain?.Notice(ExampleBrain.Notices.NEW_OBJECT, new BeatmapObjectNoticeParameters(bm));

            if (!setHistory)
                return;

            EditorManager.inst.history.Add(new History.Command("Create New Helper Object", () => CreateNewHelperObject(select, false), () => EditorTimeline.inst.DeleteObject(timelineObject)));
        }

        public void CreateNewDecorationObject(bool select = true, bool setHistory = true)
        {
            var timelineObject = CreateNewDefaultObject(select);

            var bm = timelineObject.GetData<BeatmapObject>();
            bm.name = "decoration";
            if (!Seasons.IsAprilFools)
                bm.objectType = BeatmapObject.ObjectType.Decoration;

            if (EditorConfig.Instance.CreateObjectsatCameraCenter.Value)
            {
                var pos = EventManager.inst.cam.transform.position;

                bm.events[0][0].values[0] = pos.x;
                bm.events[0][0].values[1] = pos.y;
            }

            RTLevel.Current?.UpdateObject(bm);
            EditorTimeline.inst.RenderTimelineObject(timelineObject);
            EditorTimeline.inst.UpdateTransformIndex();
            OpenDialog(bm);

            Example.Current?.brain?.Notice(ExampleBrain.Notices.NEW_OBJECT, new BeatmapObjectNoticeParameters(bm));

            if (!setHistory)
                return;

            EditorManager.inst.history.Add(new History.Command("Create New Decoration Object", () => CreateNewDecorationObject(select, false), () => EditorTimeline.inst.DeleteObject(timelineObject)));
        }

        public void CreateNewEmptyObject(bool select = true, bool setHistory = true)
        {
            var timelineObject = CreateNewDefaultObject(select);

            var bm = timelineObject.GetData<BeatmapObject>();
            bm.name = "empty";
            if (!Seasons.IsAprilFools)
                bm.objectType = BeatmapObject.ObjectType.Empty;

            if (EditorConfig.Instance.CreateObjectsatCameraCenter.Value)
            {
                var pos = EventManager.inst.cam.transform.position;

                bm.events[0][0].values[0] = pos.x;
                bm.events[0][0].values[1] = pos.y + (Seasons.IsAprilFools ? 999f : 0f);
            }

            RTLevel.Current?.UpdateObject(bm);
            EditorTimeline.inst.RenderTimelineObject(timelineObject);
            EditorTimeline.inst.UpdateTransformIndex();
            OpenDialog(bm);

            Example.Current?.brain?.Notice(ExampleBrain.Notices.NEW_OBJECT, new BeatmapObjectNoticeParameters(bm));

            if (!setHistory)
                return;

            EditorManager.inst.history.Add(new History.Command("Create New Empty Object", () => CreateNewEmptyObject(select, false), () => EditorTimeline.inst.DeleteObject(timelineObject)));
        }

        public void CreateNewNoAutokillObject(bool select = true, bool setHistory = true)
        {
            var timelineObject = CreateNewDefaultObject(select);

            var bm = timelineObject.GetData<BeatmapObject>();
            bm.name = Seasons.IsAprilFools ? "dead" : "no autokill";
            bm.autoKillType = AutoKillType.NoAutokill;

            if (EditorConfig.Instance.CreateObjectsatCameraCenter.Value)
            {
                var pos = EventManager.inst.cam.transform.position;

                bm.events[0][0].values[0] = pos.x;
                bm.events[0][0].values[1] = pos.y;
            }

            RTLevel.Current?.UpdateObject(bm);
            EditorTimeline.inst.RenderTimelineObject(timelineObject);
            EditorTimeline.inst.UpdateTransformIndex();
            OpenDialog(bm);

            Example.Current?.brain?.Notice(ExampleBrain.Notices.NEW_OBJECT, new BeatmapObjectNoticeParameters(bm));

            if (!setHistory)
                return;

            EditorManager.inst.history.Add(new History.Command("Create New No Autokill Object", () => CreateNewNoAutokillObject(select, false), () => EditorTimeline.inst.DeleteObject(timelineObject)));
        }

        /// <summary>
        /// Creates a sequence of image objects.
        /// </summary>
        /// <param name="directory">Directory that contains images.</param>
        /// <param name="fps">FPS of the image sequence.</param>
        public void CreateImageSequence(string directory, int fps)
        {
            if (RTFile.DirectoryExists(directory))
                CreateImageSequence(Directory.GetFiles(directory), fps);
        }

        /// <summary>
        /// Creates a sequence of image objects.
        /// </summary>
        /// <param name="files">Files to create an image sequence from.</param>
        /// <param name="fps">FPS of the image sequence.</param>
        public void CreateImageSequence(string[] files, int fps)
        {
            if (!EditorManager.inst.hasLoadedLevel)
            {
                EditorManager.inst.DisplayNotification("Cannot create an image sequence wihtout a level loaded.", 4f, EditorManager.NotificationType.Error);
                return;
            }

            EditorManager.inst.DisplayNotification("Creating image sequence. Please wait...", 3f, EditorManager.NotificationType.Warning);

            TimelineObject parentObject = null;
            string parentID = string.Empty;
            var time = AudioManager.inst.CurrentAudioSource.time;
            int frame = 0;
            var sw = CoreHelper.StartNewStopwatch();
            for (int i = 0; i < files.Length; i++)
            {
                var file = files[i];
                if (!RTFile.FileIsFormat(file, FileFormat.PNG, FileFormat.JPG))
                    continue;

                if (!parentObject)
                    CreateNewObject(timelineObject =>
                    {
                        var beatmapObject = timelineObject.GetData<BeatmapObject>();
                        beatmapObject.name = "P_Sequence Parent";
                        beatmapObject.StartTime = time;
                        beatmapObject.objectType = BeatmapObject.ObjectType.Empty;
                        parentID = beatmapObject.id;
                        parentObject = timelineObject;
                    }, false, true, false, false, false);

                float t = 1f / fps;

                CreateNewObject(timelineObject =>
                {
                    var beatmapObject = timelineObject.GetData<BeatmapObject>();
                    beatmapObject.name = $"{frame} frame";
                    beatmapObject.parentType = "111";
                    beatmapObject.Parent = parentID;
                    beatmapObject.StartTime = time;
                    beatmapObject.ShapeType = ShapeType.Image;
                    beatmapObject.autoKillOffset = t;
                    beatmapObject.autoKillType = AutoKillType.FixedTime;
                    beatmapObject.editorData.Bin = 1;
                    SelectImage(file, beatmapObject, false, false);
                }, false, true, false, false, false);

                time += t;
                frame++;
            }

            RTLevel.Current?.RecalculateObjectStates();

            if (parentObject)
                EditorTimeline.inst.SetCurrentObject(parentObject);

            CoreHelper.StopAndLogStopwatch(sw);
            EditorManager.inst.DisplayNotification($"Created image sequence! Took {sw.Elapsed}", 3f, EditorManager.NotificationType.Warning);
        }

        #endregion

        #region Selection

        public IEnumerator GroupSelectKeyframes(bool _add = true)
        {
            if (!EditorTimeline.inst.CurrentSelection.isBeatmapObject)
                yield break;

            var list = EditorTimeline.inst.CurrentSelection.InternalTimelineObjects;

            if (!_add)
                list.ForEach(x => x.Selected = false);

            list.Where(x => RTMath.RectTransformToScreenSpace(ObjEditor.inst.SelectionBoxImage.rectTransform)
            .Overlaps(RTMath.RectTransformToScreenSpace(x.Image.rectTransform))).ToList().ForEach(timelineObject =>
            {
                timelineObject.Selected = true;
                timelineObject.timeOffset = 0f;
                ObjEditor.inst.currentKeyframeKind = timelineObject.Type;
                ObjEditor.inst.currentKeyframe = timelineObject.Index;
            });

            var bm = EditorTimeline.inst.CurrentSelection.GetData<BeatmapObject>();
            RenderObjectKeyframesDialog(bm);
            RenderKeyframes(bm);

            yield break;
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
            var bmTimelineObject = EditorTimeline.inst.GetTimelineObject(beatmapObject);

            if (!ObjEditor.inst.timelineKeyframesDrag)
            {
                Debug.Log($"{ObjEditor.inst.className}Setting Current Keyframe: {type}, {index}");
                if (!_shift && bmTimelineObject.InternalTimelineObjects.Count > 0)
                    bmTimelineObject.InternalTimelineObjects.ForEach(timelineObject => { timelineObject.Selected = false; });

                var kf = GetKeyframe(beatmapObject, type, index);

                kf.Selected = !_shift || !kf.Selected;
            }

            DataManager.inst.UpdateSettingInt("EditorObjKeyframeKind", type);
            DataManager.inst.UpdateSettingInt("EditorObjKeyframe", index);
            ObjEditor.inst.currentKeyframeKind = type;
            ObjEditor.inst.currentKeyframe = index;

            if (_bringTo)
            {
                float value = beatmapObject.events[ObjEditor.inst.currentKeyframeKind][ObjEditor.inst.currentKeyframe].time + beatmapObject.StartTime;

                value = Mathf.Clamp(value, AllowTimeExactlyAtStart ? beatmapObject.StartTime + 0.001f : beatmapObject.StartTime, beatmapObject.StartTime + beatmapObject.GetObjectLifeLength());

                AudioManager.inst.SetMusicTime(Mathf.Clamp(value, 0f, AudioManager.inst.CurrentAudioSource.clip.length));
                AudioManager.inst.CurrentAudioSource.Pause();
                EditorManager.inst.UpdatePlayButton();
            }

            RenderObjectKeyframesDialog(beatmapObject);
        }

        public EventKeyframe CreateEventKeyframe(BeatmapObject beatmapObject, float time, int type, EventKeyframe previousKeyframe, bool openDialog)
        {
            var eventKeyframe = previousKeyframe.Copy();
            var t = RTEditor.inst.editorInfo.bpmSnapActive && EditorConfig.Instance.BPMSnapsKeyframes.Value ? -(beatmapObject.StartTime - RTEditor.SnapToBPM(beatmapObject.StartTime + time)) : time;
            eventKeyframe.time = t;

            if (eventKeyframe.relative)
                for (int i = 0; i < eventKeyframe.values.Length; i++)
                    eventKeyframe.values[i] = 0f;

            if (type == 0) // position type has 4 random values.
                eventKeyframe.SetRandomValues(eventKeyframe.GetRandomValue(0), eventKeyframe.GetRandomValue(1), eventKeyframe.GetRandomValue(2), eventKeyframe.GetRandomValue(3));
            else
                eventKeyframe.SetRandomValues(eventKeyframe.GetRandomValue(0), eventKeyframe.GetRandomValue(1), eventKeyframe.GetRandomValue(2));

            eventKeyframe.locked = false;

            beatmapObject.events[type].Add(eventKeyframe);

            EditorTimeline.inst.RenderTimelineObject(EditorTimeline.inst.GetTimelineObject(beatmapObject));
            RTLevel.Current?.UpdateObject(beatmapObject, RTLevel.ObjectContext.AUTOKILL);
            if (openDialog)
            {
                ResizeKeyframeTimeline(beatmapObject);
                RenderObjectKeyframesDialog(beatmapObject);
            }
            return eventKeyframe;
        }

        #endregion

        #region Render Dialog

        public static bool UpdateObjects => true;

        public static bool HideVisualElementsWhenObjectIsEmpty { get; set; }

        /// <summary>
        /// Opens the Object Editor dialog.
        /// </summary>
        /// <param name="beatmapObject">The Beatmap Object to edit.</param>
        public void OpenDialog(BeatmapObject beatmapObject)
        {
            if (!EditorManager.inst.hasLoadedLevel)
            {
                EditorManager.inst.DisplayNotification("Open a level first before trying to select an object.", 2f, EditorManager.NotificationType.Error);
                return;
            }

            if (!beatmapObject || string.IsNullOrEmpty(beatmapObject.id))
            {
                EditorManager.inst.DisplayNotification("Cannot edit non-object!", 2f, EditorManager.NotificationType.Error);
                return;
            }

            EditorManager.inst.ClearPopups();

            if (!Dialog)
            {
                EditorManager.inst.DisplayNotification("Object Editor Dialog is null. Please report this to RTMecha.", 4f, EditorManager.NotificationType.Error);
                return;
            }

            Dialog.Open();

            if (EditorTimeline.inst.CurrentSelection.ID != beatmapObject.id)
                for (int i = 0; i < ObjEditor.inst.TimelineParents.Count; i++)
                    LSHelpers.DeleteChildren(ObjEditor.inst.TimelineParents[i], true);

            RenderDialog(beatmapObject);
        }

        /// <summary>
        /// Refreshes the Object Editor to the specified BeatmapObject, allowing for any object to be edited from anywhere.
        /// </summary>
        /// <param name="beatmapObject">The BeatmapObject to render the editor for.</param>
        public void RenderDialog(BeatmapObject beatmapObject)
        {
            if (!EditorManager.inst.hasLoadedLevel || string.IsNullOrEmpty(beatmapObject.id))
                return;

            EditorTimeline.inst.CurrentSelection = EditorTimeline.inst.GetTimelineObject(beatmapObject);
            EditorTimeline.inst.CurrentSelection.Selected = true;

            RenderID(beatmapObject);
            RenderLDM(beatmapObject);
            RenderName(beatmapObject);
            RenderTags(beatmapObject);
            RenderObjectType(beatmapObject);

            RenderStartTime(beatmapObject);
            RenderAutokill(beatmapObject);

            RenderParent(beatmapObject);

            RenderOrigin(beatmapObject);
            RenderGradient(beatmapObject);
            RenderShape(beatmapObject);
            RenderDepth(beatmapObject);

            RenderLayers(beatmapObject);
            RenderBin(beatmapObject);

            try
            {
                RenderIndex(beatmapObject);
            }
            catch (Exception ex)
            {
                CoreHelper.LogException(ex);
            }

            RenderEditorColors(beatmapObject);

            RenderGameObjectInspector(beatmapObject);
            RenderPrefabReference(beatmapObject);

            SetTimeline(EditorTimeline.inst.CurrentSelection.Zoom, EditorTimeline.inst.CurrentSelection.TimelinePosition);

            RenderObjectKeyframesDialog(beatmapObject);

            try
            {
                RenderMarkers(beatmapObject);
            }
            catch (Exception ex)
            {
                CoreHelper.LogError($"Error {ex}");
            }

            CoroutineHelper.StartCoroutine(Dialog.ModifiersDialog.RenderModifiers(beatmapObject));
        }

        /// <summary>
        /// Renders the ID Text.
        /// </summary>
        /// <param name="beatmapObject">The Beatmap Object to set.</param>
        public void RenderID(BeatmapObject beatmapObject)
        {
            Dialog.IDBase.gameObject.SetActive(RTEditor.NotSimple);
            if (!RTEditor.NotSimple)
                return;

            Dialog.IDText.text = $"ID: {beatmapObject.id}";

            var clickable = Dialog.IDBase.gameObject.GetOrAddComponent<Clickable>();

            clickable.onClick = pointerEventData =>
            {
                EditorManager.inst.DisplayNotification($"Copied ID from {beatmapObject.name}!", 2f, EditorManager.NotificationType.Success);
                LSText.CopyToClipboard(beatmapObject.id);
            };
        }

        /// <summary>
        /// Renders the LDM Toggle.
        /// </summary>
        /// <param name="beatmapObject">The Beatmap Object to set.</param>
        public void RenderLDM(BeatmapObject beatmapObject)
        {
            Dialog.LDMLabel.gameObject.SetActive(RTEditor.ShowModdedUI);
            Dialog.LDMToggle.gameObject.SetActive(RTEditor.ShowModdedUI);

            if (!RTEditor.ShowModdedUI)
                return;

            Dialog.LDMToggle.SetIsOnWithoutNotify(beatmapObject.LDM);
            Dialog.LDMToggle.onValueChanged.NewListener(_val =>
            {
                beatmapObject.LDM = _val;
                RTLevel.Current?.UpdateObject(beatmapObject);
            });
        }

        /// <summary>
        /// Renders the Name InputField.
        /// </summary>
        /// <param name="beatmapObject">The Beatmap Object to set.</param>
        public void RenderName(BeatmapObject beatmapObject)
        {
            // Allows for left / right flipping.
            TriggerHelper.InversableField(Dialog.NameField, InputFieldSwapper.Type.String);
            EditorHelper.AddInputFieldContextMenu(Dialog.NameField);

            Dialog.NameField.SetTextWithoutNotify(beatmapObject.name);
            Dialog.NameField.onValueChanged.NewListener(_val =>
            {
                beatmapObject.name = _val;

                // Since name has no effect on the physical object, we will only need to update the timeline object.
                EditorTimeline.inst.RenderTimelineObject(EditorTimeline.inst.GetTimelineObject(beatmapObject));
            });
        }

        /// <summary>
        /// Renders the Tags list.
        /// </summary>
        /// <param name="beatmapObject">The Beatmap Object to set.</param>
        public void RenderTags(BeatmapObject beatmapObject)
        {
            var tagsScrollView = Dialog.TagsScrollView;
            tagsScrollView.parent.GetChild(tagsScrollView.GetSiblingIndex() - 1).gameObject.SetActive(RTEditor.ShowModdedUI);
            tagsScrollView.gameObject.SetActive(RTEditor.ShowModdedUI);

            LSHelpers.DeleteChildren(Dialog.TagsContent);

            if (!RTEditor.ShowModdedUI)
                return;

            int num = 0;
            foreach (var tag in beatmapObject.tags)
            {
                int index = num;
                var gameObject = EditorPrefabHolder.Instance.Tag.Duplicate(Dialog.TagsContent, index.ToString());
                gameObject.transform.localScale = Vector3.one;
                var input = gameObject.transform.Find("Input").GetComponent<InputField>();
                input.SetTextWithoutNotify(tag);
                input.onValueChanged.NewListener(_val => beatmapObject.tags[index] = _val);

                var deleteStorage = gameObject.transform.Find("Delete").GetComponent<DeleteButtonStorage>();
                deleteStorage.button.onClick.NewListener(() =>
                {
                    beatmapObject.tags.RemoveAt(index);
                    RenderTags(beatmapObject);
                });

                EditorHelper.AddInputFieldContextMenu(input);
                TriggerHelper.InversableField(input, InputFieldSwapper.Type.String);

                EditorThemeManager.ApplyGraphic(gameObject.GetComponent<Image>(), ThemeGroup.Input_Field, true);

                EditorThemeManager.ApplyInputField(input);

                EditorThemeManager.ApplyGraphic(deleteStorage.baseImage, ThemeGroup.Delete, true);
                EditorThemeManager.ApplyGraphic(deleteStorage.image, ThemeGroup.Delete_Text);

                num++;
            }

            var add = PrefabEditor.inst.CreatePrefab.Duplicate(Dialog.TagsContent, "Add");
            add.transform.localScale = Vector3.one;
            var addText = add.transform.Find("Text").GetComponent<Text>();
            addText.text = "Add Tag";
            var addButton = add.GetComponent<Button>();
            addButton.onClick.NewListener(() =>
            {
                beatmapObject.tags.Add("New Tag");
                RenderTags(beatmapObject);
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
            Dialog.ObjectTypeDropdown.options =
                EditorConfig.Instance.EditorComplexity.Value == Complexity.Advanced ?
                    CoreHelper.StringToOptionData("Normal", "Helper", "Decoration", "Empty", "Solid") :
                    CoreHelper.StringToOptionData("Normal", "Helper", "Decoration", "Empty"); // don't show solid object type 

            Dialog.ObjectTypeDropdown.SetValueWithoutNotify(Mathf.Clamp((int)beatmapObject.objectType, 0, Dialog.ObjectTypeDropdown.options.Count - 1));
            Dialog.ObjectTypeDropdown.onValueChanged.NewListener(_val =>
            {
                beatmapObject.objectType = (BeatmapObject.ObjectType)_val;
                RenderGameObjectInspector(beatmapObject);
                // ObjectType affects both physical object and timeline object.
                EditorTimeline.inst.RenderTimelineObject(EditorTimeline.inst.GetTimelineObject(beatmapObject));
                if (UpdateObjects)
                    RTLevel.Current?.UpdateObject(beatmapObject, RTLevel.ObjectContext.OBJECT_TYPE);

                RenderDialog(beatmapObject);
            });
        }

        /// <summary>
        /// Renders all StartTime UI.
        /// </summary>
        /// <param name="beatmapObject">The Beatmap Object to set.</param>
        public void RenderStartTime(BeatmapObject beatmapObject)
        {
            var startTimeField = Dialog.StartTimeField;

            startTimeField.lockToggle.SetIsOnWithoutNotify(beatmapObject.editorData.locked);
            startTimeField.lockToggle.onValueChanged.NewListener(_val =>
            {
                beatmapObject.editorData.locked = _val;

                // Since locking has no effect on the physical object, we will only need to update the timeline object.
                EditorTimeline.inst.RenderTimelineObject(EditorTimeline.inst.GetTimelineObject(beatmapObject));
            });

            startTimeField.inputField.SetTextWithoutNotify(beatmapObject.StartTime.ToString());
            startTimeField.inputField.onValueChanged.NewListener(_val =>
            {
                if (float.TryParse(_val, out float num))
                {
                    if (EditorConfig.Instance.ClampedTimelineDrag.Value)
                        num = Mathf.Clamp(num, 0f, AudioManager.inst.CurrentAudioSource.clip.length);
                    beatmapObject.StartTime = num;

                    // StartTime affects both physical object and timeline object.
                    EditorTimeline.inst.RenderTimelineObject(EditorTimeline.inst.GetTimelineObject(beatmapObject));
                    if (UpdateObjects)
                        RTLevel.Current?.UpdateObject(beatmapObject, RTLevel.ObjectContext.START_TIME);

                    beatmapObject.modifiers.ForEach(modifier =>
                    {
                        modifier.Inactive?.Invoke(modifier, null);
                        modifier.Result = default;
                    });

                    ResizeKeyframeTimeline(beatmapObject);
                    RenderMarkers(beatmapObject);
                }
            });

            TriggerHelper.AddEventTriggers(Dialog.StartTimeField.gameObject, TriggerHelper.ScrollDelta(startTimeField.inputField));

            startTimeField.leftGreaterButton.onClick.NewListener(() =>
            {
                float moveTime = beatmapObject.StartTime - 1f;
                moveTime = Mathf.Clamp(moveTime, 0f, AudioManager.inst.CurrentAudioSource.clip.length);
                startTimeField.inputField.text = moveTime.ToString();

                // StartTime affects both physical object and timeline object.
                EditorTimeline.inst.RenderTimelineObject(EditorTimeline.inst.GetTimelineObject(beatmapObject));
                if (UpdateObjects)
                    RTLevel.Current?.UpdateObject(beatmapObject, RTLevel.ObjectContext.START_TIME);

                beatmapObject.modifiers.ForEach(modifier =>
                {
                    modifier.Inactive?.Invoke(modifier, null);
                    modifier.Result = default;
                });

                ResizeKeyframeTimeline(beatmapObject);
                RenderMarkers(beatmapObject);
            });
            startTimeField.leftButton.onClick.NewListener(() =>
            {
                float moveTime = beatmapObject.StartTime - 0.1f;
                moveTime = Mathf.Clamp(moveTime, 0f, AudioManager.inst.CurrentAudioSource.clip.length);
                startTimeField.inputField.text = moveTime.ToString();

                // StartTime affects both physical object and timeline object.
                EditorTimeline.inst.RenderTimelineObject(EditorTimeline.inst.GetTimelineObject(beatmapObject));
                if (UpdateObjects)
                    RTLevel.Current?.UpdateObject(beatmapObject, RTLevel.ObjectContext.START_TIME);

                beatmapObject.modifiers.ForEach(modifier =>
                {
                    modifier.Inactive?.Invoke(modifier, null);
                    modifier.Result = default;
                });

                ResizeKeyframeTimeline(beatmapObject);
                RenderMarkers(beatmapObject);
            });
            startTimeField.middleButton.onClick.NewListener(() =>
            {
                startTimeField.inputField.text = EditorManager.inst.CurrentAudioPos.ToString();

                // StartTime affects both physical object and timeline object.
                EditorTimeline.inst.RenderTimelineObject(EditorTimeline.inst.GetTimelineObject(beatmapObject));
                if (UpdateObjects)
                    RTLevel.Current?.UpdateObject(beatmapObject, RTLevel.ObjectContext.START_TIME);

                beatmapObject.modifiers.ForEach(modifier =>
                {
                    modifier.Inactive?.Invoke(modifier, null);
                    modifier.Result = default;
                });

                ResizeKeyframeTimeline(beatmapObject);
                RenderMarkers(beatmapObject);
            });
            startTimeField.rightButton.onClick.NewListener(() =>
            {
                float moveTime = beatmapObject.StartTime + 0.1f;
                moveTime = Mathf.Clamp(moveTime, 0f, AudioManager.inst.CurrentAudioSource.clip.length);
                startTimeField.inputField.text = moveTime.ToString();

                // StartTime affects both physical object and timeline object.
                EditorTimeline.inst.RenderTimelineObject(EditorTimeline.inst.GetTimelineObject(beatmapObject));
                if (UpdateObjects)
                    RTLevel.Current?.UpdateObject(beatmapObject, RTLevel.ObjectContext.START_TIME);

                beatmapObject.modifiers.ForEach(modifier =>
                {
                    modifier.Inactive?.Invoke(modifier, null);
                    modifier.Result = default;
                });

                ResizeKeyframeTimeline(beatmapObject);
                RenderMarkers(beatmapObject);
            });
            startTimeField.rightGreaterButton.onClick.NewListener(() =>
            {
                float moveTime = beatmapObject.StartTime + 1f;
                moveTime = Mathf.Clamp(moveTime, 0f, AudioManager.inst.CurrentAudioSource.clip.length);
                startTimeField.inputField.text = moveTime.ToString();

                // StartTime affects both physical object and timeline object.
                EditorTimeline.inst.RenderTimelineObject(EditorTimeline.inst.GetTimelineObject(beatmapObject));
                if (UpdateObjects)
                    RTLevel.Current?.UpdateObject(beatmapObject, RTLevel.ObjectContext.START_TIME);

                beatmapObject.modifiers.ForEach(modifier =>
                {
                    modifier.Inactive?.Invoke(modifier, null);
                    modifier.Result = default;
                });

                ResizeKeyframeTimeline(beatmapObject);
                RenderMarkers(beatmapObject);
            });
        }

        /// <summary>
        /// Renders all Autokill UI.
        /// </summary>
        /// <param name="beatmapObject">The Beatmap Object to set.</param>
        public void RenderAutokill(BeatmapObject beatmapObject)
        {
            Dialog.AutokillDropdown.SetValueWithoutNotify((int)beatmapObject.autoKillType);
            Dialog.AutokillDropdown.onValueChanged.NewListener(_val =>
            {
                beatmapObject.autoKillType = (AutoKillType)_val;
                // AutoKillType affects both physical object and timeline object.
                EditorTimeline.inst.RenderTimelineObject(EditorTimeline.inst.GetTimelineObject(beatmapObject));
                if (UpdateObjects)
                    RTLevel.Current?.UpdateObject(beatmapObject, RTLevel.ObjectContext.AUTOKILL);
                ResizeKeyframeTimeline(beatmapObject);
                RenderAutokill(beatmapObject);
                RenderMarkers(beatmapObject);
            });

            if (beatmapObject.autoKillType == AutoKillType.FixedTime ||
                beatmapObject.autoKillType == AutoKillType.SongTime ||
                beatmapObject.autoKillType == AutoKillType.LastKeyframeOffset)
            {
                Dialog.AutokillField.gameObject.SetActive(true);

                Dialog.AutokillField.SetTextWithoutNotify(beatmapObject.autoKillOffset.ToString());
                Dialog.AutokillField.onValueChanged.NewListener(_val =>
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
                        EditorTimeline.inst.RenderTimelineObject(EditorTimeline.inst.GetTimelineObject(beatmapObject));
                        if (UpdateObjects)
                            RTLevel.Current?.UpdateObject(beatmapObject, RTLevel.ObjectContext.AUTOKILL);

                        beatmapObject.modifiers.ForEach(modifier =>
                        {
                            modifier.Inactive?.Invoke(modifier, null);
                            modifier.Result = default;
                        });

                        ResizeKeyframeTimeline(beatmapObject);
                        RenderMarkers(beatmapObject);
                    }
                });

                Dialog.AutokillSetButton.gameObject.SetActive(true);
                Dialog.AutokillSetButton.onClick.NewListener(() =>
                {
                    float num = 0f;

                    if (beatmapObject.autoKillType == AutoKillType.SongTime)
                        num = AudioManager.inst.CurrentAudioSource.time;
                    else num = AudioManager.inst.CurrentAudioSource.time - beatmapObject.StartTime;

                    if (num < 0f)
                        num = 0f;

                    Dialog.AutokillField.text = num.ToString();
                });

                // Add Scrolling for easy changing of values.
                TriggerHelper.AddEventTriggers(Dialog.AutokillField.gameObject, TriggerHelper.ScrollDelta(Dialog.AutokillField, 0.1f, 10f, 0f, float.PositiveInfinity));
            }
            else
            {
                Dialog.AutokillField.gameObject.SetActive(false);
                Dialog.AutokillField.onValueChanged.ClearAll();
                Dialog.AutokillSetButton.gameObject.SetActive(false);
                Dialog.AutokillSetButton.onClick.ClearAll();
            }

            Dialog.CollapseToggle.SetIsOnWithoutNotify(beatmapObject.editorData.collapse);
            Dialog.CollapseToggle.onValueChanged.NewListener(_val =>
            {
                beatmapObject.editorData.collapse = _val;

                // Since autokill collapse has no affect on the physical object, we will only need to update the timeline object.
                EditorTimeline.inst.RenderTimelineObject(EditorTimeline.inst.GetTimelineObject(beatmapObject));
            });
        }

        /// <summary>
        /// Renders all Parent UI.
        /// </summary>
        /// <param name="beatmapObject">The Beatmap Object to set.</param>
        public void RenderParent(BeatmapObject beatmapObject)
        {
            string parent = beatmapObject.Parent;
            
            Dialog.ParentButton.transform.AsRT().sizeDelta = new Vector2(!string.IsNullOrEmpty(parent) ? 201f : 241f, 32f);

            Dialog.ParentSearchButton.onClick.NewListener(ShowParentSearch);
            var parentSearchContextMenu = Dialog.ParentSearchButton.gameObject.GetOrAddComponent<ContextClickable>();
            parentSearchContextMenu.onClick = eventData =>
            {
                if (eventData.button != PointerEventData.InputButton.Right)
                    return;

                EditorContextMenu.inst.ShowContextMenu(
                    new ButtonFunction("Open Parent Popup", () => ShowParentSearch(EditorTimeline.inst.GetTimelineObject(beatmapObject))),
                    new ButtonFunction("Parent to Camera", () =>
                    {
                        beatmapObject.Parent = BeatmapObject.CAMERA_PARENT;
                        RTLevel.Current?.UpdateObject(beatmapObject, RTLevel.ObjectContext.PARENT_CHAIN);
                        RenderParent(beatmapObject);
                    })
                    );
            };

            Dialog.ParentPickerButton.onClick.NewListener(() => RTEditor.inst.parentPickerEnabled = true);

            Dialog.ParentClearButton.gameObject.SetActive(!string.IsNullOrEmpty(parent));

            Dialog.ParentSettingsParent.transform.AsRT().sizeDelta = new Vector2(351f, RTEditor.ShowModdedUI ? 152f : 112f);

            var parentContextMenu = Dialog.ParentButton.gameObject.GetOrAddComponent<ContextClickable>();
            parentContextMenu.onClick = eventData =>
            {
                if (eventData.button != PointerEventData.InputButton.Right)
                    return;

                var list = new List<ButtonFunction>();

                if (!string.IsNullOrEmpty(beatmapObject.Parent))
                {
                    var parentChain = beatmapObject.GetParentChain();
                    if (parentChain.Count > 0)
                        list.Add(new ButtonFunction("View Parent Chain", () =>
                        {
                            ShowObjectSearch(x => EditorTimeline.inst.SetCurrentObject(EditorTimeline.inst.GetTimelineObject(x), Input.GetKey(KeyCode.LeftControl)), beatmapObjects: parentChain);
                        }));
                }

                if (GameData.Current.beatmapObjects.TryFindAll(x => x.Parent == beatmapObject.id, out List<BeatmapObject> findAll))
                {
                    var childTree = beatmapObject.GetChildTree();
                    if (childTree.Count > 0)
                        list.Add(new ButtonFunction("View Child Tree", () =>
                        {
                            ShowObjectSearch(x => EditorTimeline.inst.SetCurrentObject(EditorTimeline.inst.GetTimelineObject(x), Input.GetKey(KeyCode.LeftControl)), beatmapObjects: childTree);
                        }));
                }

                EditorContextMenu.inst.ShowContextMenu(list);
            };

            if (string.IsNullOrEmpty(parent))
            {
                Dialog.ParentButton.button.interactable = false;
                Dialog.ParentMoreButton.interactable = false;
                Dialog.ParentSettingsParent.gameObject.SetActive(false);
                Dialog.ParentButton.label.text = "No Parent Object";

                Dialog.ParentInfo.tooltipLangauges[0].hint = string.IsNullOrEmpty(parent) ? "Object not parented." : "No parent found.";
                Dialog.ParentButton.button.onClick.ClearAll();
                Dialog.ParentMoreButton.onClick.ClearAll();
                Dialog.ParentClearButton.onClick.ClearAll();

                return;
            }

            string p = null;

            if (GameData.Current.beatmapObjects.TryFindIndex(x => x.id == parent, out int pa))
            {
                p = GameData.Current.beatmapObjects[pa].name;
                Dialog.ParentInfo.tooltipLangauges[0].hint = string.Format("Parent chain count: [{0}]\n(Inclusive)", beatmapObject.GetParentChain().Count);
            }
            else if (parent == BeatmapObject.CAMERA_PARENT)
            {
                p = "[CAMERA]";
                Dialog.ParentInfo.tooltipLangauges[0].hint = "Object parented to the camera.";
            }

            Dialog.ParentButton.button.interactable = p != null;
            Dialog.ParentMoreButton.interactable = p != null;

            Dialog.ParentSettingsParent.gameObject.SetActive(p != null && ObjEditor.inst.advancedParent);

            Dialog.ParentClearButton.onClick.NewListener(() =>
            {
                if (beatmapObject.customParent != null)
                {
                    beatmapObject.customParent = null;
                    EditorManager.inst.DisplayNotification("Removed custom parent!", 1.5f, EditorManager.NotificationType.Success);
                }
                else
                    beatmapObject.Parent = string.Empty;

                // Since parent has no affect on the timeline object, we will only need to update the physical object.
                if (UpdateObjects)
                    RTLevel.Current?.UpdateObject(beatmapObject, RTLevel.ObjectContext.PARENT_CHAIN);

                RenderParent(beatmapObject);
            });

            if (p == null)
            {
                Dialog.ParentButton.label.text = "No Parent Object";
                Dialog.ParentInfo.tooltipLangauges[0].hint = string.IsNullOrEmpty(parent) ? "Object not parented." : "No parent found.";
                Dialog.ParentButton.button.onClick.ClearAll();
                Dialog.ParentMoreButton.onClick.ClearAll();

                return;
            }

            Dialog.ParentButton.label.text = p;

            Dialog.ParentButton.button.onClick.NewListener(() =>
            {
                if (GameData.Current.beatmapObjects.Find(x => x.id == parent) != null &&
                    parent != BeatmapObject.CAMERA_PARENT &&
                    EditorTimeline.inst.timelineObjects.TryFind(x => x.ID == parent, out TimelineObject timelineObject))

                    EditorTimeline.inst.SetCurrentObject(timelineObject);
                else if (parent == BeatmapObject.CAMERA_PARENT)
                {
                    EditorTimeline.inst.SetLayer(EditorTimeline.LayerType.Events);
                    EventEditor.inst.SetCurrentEvent(0, GameData.Current.ClosestEventKeyframe(0));
                }
            });

            Dialog.ParentMoreButton.onClick.NewListener(() =>
            {
                ObjEditor.inst.advancedParent = !ObjEditor.inst.advancedParent;
                Dialog.ParentSettingsParent.gameObject.SetActive(ObjEditor.inst.advancedParent);
            });
            Dialog.ParentSettingsParent.gameObject.SetActive(ObjEditor.inst.advancedParent);

            Dialog.ParentDesyncToggle.gameObject.SetActive(RTEditor.ShowModdedUI);
            if (RTEditor.ShowModdedUI)
            {
                Dialog.ParentDesyncToggle.isOn = beatmapObject.desync;
                Dialog.ParentDesyncToggle.onValueChanged.NewListener(_val =>
                {
                    beatmapObject.desync = _val;
                    RTLevel.Current?.UpdateObject(beatmapObject, RTLevel.ObjectContext.PARENT_CHAIN);
                });
            }

            for (int i = 0; i < Dialog.ParentSettings.Count; i++)
            {
                var parentSetting = Dialog.ParentSettings[i];

                var index = i;

                // Parent Type
                parentSetting.activeToggle.SetIsOnWithoutNotify(beatmapObject.GetParentType(i));
                parentSetting.activeToggle.onValueChanged.NewListener(_val =>
                {
                    beatmapObject.SetParentType(index, _val);

                    // Since updating parent type has no affect on the timeline object, we will only need to update the physical object.
                    if (UpdateObjects)
                        RTLevel.Current?.UpdateObject(beatmapObject, RTLevel.ObjectContext.PARENT_CHAIN);
                });

                // Parent Offset
                var lel = parentSetting.offsetField.GetComponent<LayoutElement>();
                lel.minWidth = RTEditor.ShowModdedUI ? 64f : 128f;
                lel.preferredWidth = RTEditor.ShowModdedUI ? 64f : 128f;
                parentSetting.offsetField.SetTextWithoutNotify(beatmapObject.GetParentOffset(i).ToString());
                parentSetting.offsetField.onValueChanged.NewListener(_val =>
                {
                    if (float.TryParse(_val, out float num))
                    {
                        beatmapObject.SetParentOffset(index, num);

                        // Since updating parent type has no affect on the timeline object, we will only need to update the physical object.
                        if (UpdateObjects)
                            RTLevel.Current?.UpdateObject(beatmapObject, RTLevel.ObjectContext.PARENT_CHAIN);
                    }
                });

                TriggerHelper.AddEventTriggers(parentSetting.offsetField.gameObject, TriggerHelper.ScrollDelta(parentSetting.offsetField));

                parentSetting.additiveToggle.onValueChanged.ClearAll();
                parentSetting.parallaxField.onValueChanged.ClearAll();
                parentSetting.additiveToggle.gameObject.SetActive(RTEditor.ShowModdedUI);
                parentSetting.parallaxField.gameObject.SetActive(RTEditor.ShowModdedUI);

                if (!RTEditor.ShowModdedUI)
                    continue;

                parentSetting.additiveToggle.SetIsOnWithoutNotify(beatmapObject.GetParentAdditive(i));
                parentSetting.additiveToggle.onValueChanged.AddListener(_val =>
                {
                    beatmapObject.SetParentAdditive(index, _val);
                    if (UpdateObjects)
                        RTLevel.Current?.UpdateObject(beatmapObject, RTLevel.ObjectContext.PARENT_CHAIN);
                });
                parentSetting.parallaxField.SetTextWithoutNotify(beatmapObject.parallaxSettings[index].ToString());
                parentSetting.parallaxField.onValueChanged.NewListener(_val =>
                {
                    if (float.TryParse(_val, out float num))
                    {
                        beatmapObject.parallaxSettings[index] = num;

                        // Since updating parent type has no affect on the timeline object, we will only need to update the physical object.
                        if (UpdateObjects)
                            RTLevel.Current?.UpdateObject(beatmapObject, RTLevel.ObjectContext.PARENT_CHAIN);
                    }
                });

                TriggerHelper.AddEventTriggers(parentSetting.parallaxField.gameObject, TriggerHelper.ScrollDelta(parentSetting.parallaxField));
            }
        }

        /// <summary>
        /// Renders the Origin InputFields.
        /// </summary>
        /// <param name="beatmapObject">The Beatmap Object to set.</param>
        public void RenderOrigin(BeatmapObject beatmapObject)
        {
            var active = !HideVisualElementsWhenObjectIsEmpty || beatmapObject.objectType != BeatmapObject.ObjectType.Empty;

            var originTF = Dialog.OriginParent;
            originTF.parent.GetChild(originTF.GetSiblingIndex() - 1).gameObject.SetActive(active);
            originTF.gameObject.SetActive(active);

            if (!active)
                return;

            // Reimplemented origin toggles for Simple Editor Complexity.
            float[] originDefaultPositions = new float[] { 0f, -0.5f, 0f, 0.5f };
            for (int i = 1; i <= 3; i++)
            {
                int index = i;
                var toggle = Dialog.OriginXToggles[i - 1];
                toggle.SetIsOnWithoutNotify(beatmapObject.origin.x == originDefaultPositions[i]);
                toggle.onValueChanged.NewListener(_val =>
                {
                    if (!_val)
                        return;

                    switch (index)
                    {
                        case 1: {
                                beatmapObject.origin.x = -0.5f;

                                // Since origin has no affect on the timeline object, we will only need to update the physical object.
                                if (UpdateObjects)
                                    RTLevel.Current?.UpdateObject(beatmapObject, RTLevel.ObjectContext.VISUAL_OFFSET);
                                break;
                            }
                        case 2: {
                                beatmapObject.origin.x = 0f;

                                // Since origin has no affect on the timeline object, we will only need to update the physical object.
                                if (UpdateObjects)
                                    RTLevel.Current?.UpdateObject(beatmapObject, RTLevel.ObjectContext.VISUAL_OFFSET);
                                break;
                            }
                        case 3: {
                                beatmapObject.origin.x = 0.5f;

                                // Since origin has no affect on the timeline object, we will only need to update the physical object.
                                if (UpdateObjects)
                                    RTLevel.Current?.UpdateObject(beatmapObject, RTLevel.ObjectContext.VISUAL_OFFSET);
                                break;
                            }
                    }
                });

                var originContextMenu = toggle.gameObject.GetOrAddComponent<ContextClickable>();

                originContextMenu.onClick = eventData =>
                {
                    if (eventData.button != PointerEventData.InputButton.Right)
                        return;

                    OriginContextMenu(beatmapObject);
                };
            }
            for (int i = 1; i <= 3; i++)
            {
                int index = i;
                var toggle = Dialog.OriginYToggles[i - 1];
                toggle.SetIsOnWithoutNotify(beatmapObject.origin.y == originDefaultPositions[i]);
                toggle.onValueChanged.NewListener(_val =>
                {
                    if (!_val)
                        return;

                    switch (index)
                    {
                        case 1: {
                                beatmapObject.origin.y = -0.5f;

                                // Since origin has no affect on the timeline object, we will only need to update the physical object.
                                if (UpdateObjects)
                                    RTLevel.Current?.UpdateObject(beatmapObject, RTLevel.ObjectContext.VISUAL_OFFSET);
                                break;
                            }
                        case 2: {
                                beatmapObject.origin.y = 0f;

                                // Since origin has no affect on the timeline object, we will only need to update the physical object.
                                if (UpdateObjects)
                                    RTLevel.Current?.UpdateObject(beatmapObject, RTLevel.ObjectContext.VISUAL_OFFSET);
                                break;
                            }
                        case 3: {
                                beatmapObject.origin.y = 0.5f;

                                // Since origin has no affect on the timeline object, we will only need to update the physical object.
                                if (UpdateObjects)
                                    RTLevel.Current?.UpdateObject(beatmapObject, RTLevel.ObjectContext.VISUAL_OFFSET);
                                break;
                            }
                    }
                });

                var originContextMenu = toggle.gameObject.GetOrAddComponent<ContextClickable>();

                originContextMenu.onClick = eventData =>
                {
                    if (eventData.button != PointerEventData.InputButton.Right)
                        return;

                    OriginContextMenu(beatmapObject);
                };
            }

            Dialog.OriginXField.inputField.SetTextWithoutNotify(beatmapObject.origin.x.ToString());
            Dialog.OriginXField.inputField.onValueChanged.NewListener(_val =>
            {
                if (float.TryParse(_val, out float num))
                {
                    beatmapObject.origin.x = num;

                    // Since origin has no affect on the timeline object, we will only need to update the physical object.
                    if (UpdateObjects)
                        RTLevel.Current?.UpdateObject(beatmapObject, RTLevel.ObjectContext.VISUAL_OFFSET);
                }
            });

            Dialog.OriginYField.inputField.SetTextWithoutNotify(beatmapObject.origin.y.ToString());
            Dialog.OriginYField.inputField.onValueChanged.NewListener(_val =>
            {
                if (float.TryParse(_val, out float num))
                {
                    beatmapObject.origin.y = num;

                    // Since origin has no affect on the timeline object, we will only need to update the physical object.
                    if (UpdateObjects)
                        RTLevel.Current?.UpdateObject(beatmapObject, RTLevel.ObjectContext.VISUAL_OFFSET);
                }
            });

            TriggerHelper.IncreaseDecreaseButtons(Dialog.OriginXField);
            TriggerHelper.IncreaseDecreaseButtons(Dialog.OriginYField);

            TriggerHelper.AddEventTriggers(Dialog.OriginXField.inputField.gameObject, TriggerHelper.ScrollDelta(Dialog.OriginXField.inputField, multi: true), TriggerHelper.ScrollDeltaVector2(Dialog.OriginXField.inputField, Dialog.OriginYField.inputField, 0.1f, 10f));
            TriggerHelper.AddEventTriggers(Dialog.OriginYField.inputField.gameObject, TriggerHelper.ScrollDelta(Dialog.OriginYField.inputField, multi: true), TriggerHelper.ScrollDeltaVector2(Dialog.OriginXField.inputField, Dialog.OriginYField.inputField, 0.1f, 10f));

            var originXContextMenu = Dialog.OriginXField.inputField.gameObject.GetOrAddComponent<ContextClickable>();

            originXContextMenu.onClick = eventData =>
            {
                if (eventData.button != PointerEventData.InputButton.Right)
                    return;

                OriginContextMenu(beatmapObject);
            };

            var originYContextMenu = Dialog.OriginYField.inputField.gameObject.GetOrAddComponent<ContextClickable>();

            originYContextMenu.onClick = eventData =>
            {
                if (eventData.button != PointerEventData.InputButton.Right)
                    return;

                OriginContextMenu(beatmapObject);
            };
        }

        void OriginContextMenu(BeatmapObject beatmapObject)
        {
            EditorContextMenu.inst.ShowContextMenu(
                new ButtonFunction("Center", () =>
                {
                    beatmapObject.origin = Vector2.zero;
                    // Since origin has no affect on the timeline object, we will only need to update the physical object.
                    if (UpdateObjects)
                        RTLevel.Current?.UpdateObject(beatmapObject, RTLevel.ObjectContext.VISUAL_OFFSET);
                    RenderOrigin(beatmapObject);
                }),
                new ButtonFunction("Top", () =>
                {
                    beatmapObject.origin.y = -0.5f;
                    // Since origin has no affect on the timeline object, we will only need to update the physical object.
                    if (UpdateObjects)
                        RTLevel.Current?.UpdateObject(beatmapObject, RTLevel.ObjectContext.VISUAL_OFFSET);
                    RenderOrigin(beatmapObject);
                }),
                new ButtonFunction("Bottom", () =>
                {
                    beatmapObject.origin.y = 0.5f;
                    if (UpdateObjects)
                        RTLevel.Current?.UpdateObject(beatmapObject, RTLevel.ObjectContext.VISUAL_OFFSET);
                    RenderOrigin(beatmapObject);
                }),
                new ButtonFunction("Left", () =>
                {
                    beatmapObject.origin.x = -0.5f;
                    if (UpdateObjects)
                        RTLevel.Current?.UpdateObject(beatmapObject, RTLevel.ObjectContext.VISUAL_OFFSET);
                    RenderOrigin(beatmapObject);
                }),
                new ButtonFunction("Right", () =>
                {
                    beatmapObject.origin.x = 0.5f;
                    if (UpdateObjects)
                        RTLevel.Current?.UpdateObject(beatmapObject, RTLevel.ObjectContext.VISUAL_OFFSET);
                    RenderOrigin(beatmapObject);
                }),
                new ButtonFunction("Top (Triangle)", () =>
                {
                    beatmapObject.origin.y = BeatmapObject.TRIANGLE_TOP_OFFSET;
                    if (UpdateObjects)
                        RTLevel.Current?.UpdateObject(beatmapObject, RTLevel.ObjectContext.VISUAL_OFFSET);
                    RenderOrigin(beatmapObject);
                }),
                new ButtonFunction("Bottom (Triangle)", () =>
                {
                    beatmapObject.origin.y = BeatmapObject.TRIANGLE_BOTTOM_OFFSET;
                    if (UpdateObjects)
                        RTLevel.Current?.UpdateObject(beatmapObject, RTLevel.ObjectContext.VISUAL_OFFSET);
                    RenderOrigin(beatmapObject);
                }),
                new ButtonFunction("Left (Triangle)", () =>
                {
                    beatmapObject.origin.x = -BeatmapObject.TRIANGLE_HORIZONTAL_OFFSET;
                    if (UpdateObjects)
                        RTLevel.Current?.UpdateObject(beatmapObject, RTLevel.ObjectContext.VISUAL_OFFSET);
                    RenderOrigin(beatmapObject);
                }),
                new ButtonFunction("Right (Triangle)", () =>
                {
                    beatmapObject.origin.x = BeatmapObject.TRIANGLE_HORIZONTAL_OFFSET;
                    if (UpdateObjects)
                        RTLevel.Current?.UpdateObject(beatmapObject, RTLevel.ObjectContext.VISUAL_OFFSET);
                    RenderOrigin(beatmapObject);
                })
                );
        }

        /// <summary>
        /// Renders the Gradient ToggleGroup.
        /// </summary>
        /// <param name="beatmapObject">The BeatmapObject to set.</param>
        public void RenderGradient(BeatmapObject beatmapObject)
        {
            var active = (!HideVisualElementsWhenObjectIsEmpty || beatmapObject.objectType != BeatmapObject.ObjectType.Empty) && RTEditor.NotSimple;
            var gradientScaleActive = beatmapObject.gradientType != GradientType.Normal;
            var gradientRotationActive = beatmapObject.gradientType == GradientType.LeftLinear || beatmapObject.gradientType == GradientType.RightLinear;

            Dialog.GradientShapesLabel.transform.parent.gameObject.SetActive(active);
            Dialog.GradientParent.gameObject.SetActive(active);
            Dialog.GradientScale.gameObject.SetActive(active && gradientScaleActive);
            Dialog.GradientRotation.gameObject.SetActive(active && gradientRotationActive);

            if (!active)
                return;

            Dialog.GradientShapesLabel.text = RTEditor.NotSimple ? "Gradient / Shape" : "Shape";

            for (int i = 0; i < Dialog.GradientToggles.Count; i++)
            {
                var index = i;
                var toggle = Dialog.GradientToggles[i];
                toggle.onValueChanged.ClearAll();
                toggle.isOn = index == (int)beatmapObject.gradientType;
                toggle.onValueChanged.AddListener(_val =>
                {
                    beatmapObject.gradientType = (GradientType)index;
                    var incompatibleGradient = beatmapObject.gradientType != GradientType.Normal && beatmapObject.IsSpecialShape;

                    if (incompatibleGradient)
                    {
                        beatmapObject.Shape = 0;
                        beatmapObject.ShapeOption = 0;
                        RenderShape(beatmapObject);
                    }

                    if (!RTEditor.ShowModdedUI)
                    {
                        for (int i = 0; i < beatmapObject.events[3].Count; i++)
                            beatmapObject.events[3][i].values[6] = 10f;
                    }

                    // Since shape has no affect on the timeline object, we will only need to update the physical object.
                    if (UpdateObjects)
                        RTLevel.Current?.UpdateObject(beatmapObject, incompatibleGradient ? RTLevel.ObjectContext.SHAPE : RTLevel.ObjectContext.RENDERING);

                    RenderGradient(beatmapObject);
                    inst.RenderObjectKeyframesDialog(beatmapObject);
                });
            }

            Dialog.GradientScale.inputField.onValueChanged.ClearAll();
            if (gradientScaleActive)
            {
                Dialog.GradientScale.inputField.text = beatmapObject.gradientScale.ToString();
                Dialog.GradientScale.inputField.onValueChanged.AddListener(_val =>
                {
                    if (float.TryParse(_val, out float num))
                    {
                        beatmapObject.gradientScale = num;
                        RTLevel.Current?.UpdateObject(beatmapObject, RTLevel.ObjectContext.RENDERING);
                    }
                });

                TriggerHelper.IncreaseDecreaseButtons(Dialog.GradientScale);
                TriggerHelper.AddEventTriggers(Dialog.GradientScale.inputField.gameObject, TriggerHelper.ScrollDelta(Dialog.GradientScale.inputField));
                TriggerHelper.InversableField(Dialog.GradientScale);
            }

            Dialog.GradientRotation.inputField.onValueChanged.ClearAll();
            if (gradientRotationActive)
            {
                Dialog.GradientRotation.inputField.text = beatmapObject.gradientRotation.ToString();
                Dialog.GradientRotation.inputField.onValueChanged.AddListener(_val =>
                {
                    if (float.TryParse(_val, out float num))
                    {
                        beatmapObject.gradientRotation = num;
                        RTLevel.Current?.UpdateObject(beatmapObject, RTLevel.ObjectContext.RENDERING);
                    }
                });

                TriggerHelper.IncreaseDecreaseButtons(Dialog.GradientRotation, 15f, 3f);
                TriggerHelper.AddEventTriggers(Dialog.GradientRotation.inputField.gameObject, TriggerHelper.ScrollDelta(Dialog.GradientRotation.inputField, 15f, 3f));
                TriggerHelper.InversableField(Dialog.GradientRotation);
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

        /// <summary>
        /// Renders the Shape ToggleGroup.
        /// </summary>
        /// <param name="beatmapObject">The BeatmapObject to set.</param>
        public void RenderShape(BeatmapObject beatmapObject)
        {
            var shape = Dialog.ShapeTypesParent;
            var shapeSettings = Dialog.ShapeOptionsParent;

            var active = !HideVisualElementsWhenObjectIsEmpty || beatmapObject.objectType != BeatmapObject.ObjectType.Empty;
            Dialog.ShapeTypesParent.gameObject.SetActive(active);
            Dialog.ShapeOptionsParent.gameObject.SetActive(active);

            if (!active)
                return;

            LSHelpers.SetActiveChildren(shapeSettings, false);

            if (beatmapObject.Shape >= shapeSettings.childCount)
            {
                Debug.Log($"{ObjEditor.inst.className}Somehow, the object ended up being at a higher shape than normal.");
                beatmapObject.Shape = shapeSettings.childCount - 1;
                // Since shape has no affect on the timeline object, we will only need to update the physical object.
                if (UpdateObjects)
                    RTLevel.Current?.UpdateObject(beatmapObject, RTLevel.ObjectContext.SHAPE);

                RenderShape(beatmapObject);
                return;
            }

            shapeSettings.AsRT().sizeDelta = new Vector2(351f, beatmapObject.ShapeType == ShapeType.Text ? 74f : 32f);
            shapeSettings.GetChild(4).AsRT().sizeDelta = new Vector2(351f, beatmapObject.ShapeType == ShapeType.Text ? 74f : 32f);
            // 351 164 = polygon
            shapeSettings.GetChild(beatmapObject.Shape).gameObject.SetActive(true);

            int num = 0;
            foreach (var toggle in Dialog.ShapeToggles)
            {
                int index = num;
                toggle.SetIsOnWithoutNotify(beatmapObject.Shape == index);
                toggle.gameObject.SetActive(RTEditor.ShowModdedUI || index < Shape.unmoddedMaxShapes.Length);

                if (RTEditor.ShowModdedUI || index < Shape.unmoddedMaxShapes.Length)
                    toggle.onValueChanged.NewListener(_val =>
                    {
                        beatmapObject.Shape = index;
                        beatmapObject.ShapeOption = 0;

                        if (beatmapObject.gradientType != GradientType.Normal && (index == 4 || index == 6))
                            beatmapObject.Shape = 0;

                        if (beatmapObject.ShapeType == ShapeType.Polygon && EditorConfig.Instance.AutoPolygonRadius.Value)
                            beatmapObject.polygonShape.Radius = beatmapObject.polygonShape.GetAutoRadius();

                        // Since shape has no affect on the timeline object, we will only need to update the physical object.
                        if (UpdateObjects)
                            RTLevel.Current?.UpdateObject(beatmapObject, RTLevel.ObjectContext.SHAPE);

                        RenderShape(beatmapObject);
                    });

                num++;
            }

            switch (beatmapObject.ShapeType)
            {
                case ShapeType.Text: {
                        shapeSettings.AsRT().sizeDelta = new Vector2(351f, 74f);
                        shapeSettings.GetChild(4).AsRT().sizeDelta = new Vector2(351f, 74f);

                        var textIF = shapeSettings.Find("5").GetComponent<InputField>();
                        textIF.textComponent.alignment = TextAnchor.UpperLeft;
                        textIF.GetPlaceholderText().alignment = TextAnchor.UpperLeft;
                        textIF.GetPlaceholderText().text = "Enter text...";
                        textIF.lineType = InputField.LineType.MultiLineNewline;

                        textIF.SetTextWithoutNotify(beatmapObject.text);
                        textIF.onValueChanged.NewListener(_val =>
                        {
                            beatmapObject.text = _val;

                            // Since text has no affect on the timeline object, we will only need to update the physical object.
                            if (UpdateObjects)
                                RTLevel.Current?.UpdateObject(beatmapObject, RTLevel.ObjectContext.TEXT);
                        });

                        var textContextClickable = textIF.gameObject.GetOrAddComponent<ContextClickable>();
                        textContextClickable.onClick = eventData =>
                        {
                            if (eventData.button != PointerEventData.InputButton.Right)
                                return;

                            EditorContextMenu.inst.ShowContextMenu(
                                new ButtonFunction($"Open Text Editor", () => RTTextEditor.inst.SetInputField(textIF)),
                                new ButtonFunction(true),
                                new ButtonFunction($"Insert a Font", () => RTEditor.inst.ShowFontSelector(font => textIF.text = font + textIF.text)),
                                new ButtonFunction($"Add a Font", () => RTEditor.inst.ShowFontSelector(font => textIF.text += font)),
                                new ButtonFunction(true),
                                new ButtonFunction($"Clear Formatting", () =>
                                {
                                    RTEditor.inst.ShowWarningPopup("Are you sure you want to clear the fomratting of this text? This cannot be undone!", () =>
                                    {
                                        textIF.text = Regex.Replace(beatmapObject.text, @"<(.*?)>", string.Empty);
                                        RTEditor.inst.HideWarningPopup();
                                    }, RTEditor.inst.HideWarningPopup);
                                }),
                                new ButtonFunction($"Force Modded Formatting", () =>
                                {
                                    var formatText = "formatText";
                                    if (beatmapObject.modifiers.Has(x => x.Name == formatText))
                                        return;

                                    if (ModifiersManager.defaultBeatmapObjectModifiers.TryFind(x => x.Name == formatText, out ModifierBase defaultModifier) && defaultModifier is Modifier<BeatmapObject> modifier)
                                    {
                                        beatmapObject.modifiers.Add(modifier.Copy(true, beatmapObject));
                                        CoroutineHelper.StartCoroutine(Dialog.ModifiersDialog.RenderModifiers(beatmapObject));
                                    }
                                }),
                                new ButtonFunction(true),
                                new ButtonFunction($"Auto Align: [{beatmapObject.autoTextAlign}]", () =>
                                {
                                    beatmapObject.autoTextAlign = !beatmapObject.autoTextAlign;
                                    RTLevel.Current?.UpdateObject(beatmapObject, RTLevel.ObjectContext.SHAPE);
                                }),
                                new ButtonFunction("Align Left", () => textIF.text = "<align=left>" + textIF.text),
                                new ButtonFunction("Align Center", () => textIF.text = "<align=center>" + textIF.text),
                                new ButtonFunction("Align Right", () => textIF.text = "<align=right>" + textIF.text)
                                );
                        };

                        break;
                    }
                case ShapeType.Image: {
                        shapeSettings.AsRT().sizeDelta = new Vector2(351f, 32f);

                        var select = shapeSettings.Find("7/select").GetComponent<Button>();
                        select.onClick.ClearAll();
                        var selectContextClickable = select.gameObject.GetOrAddComponent<ContextClickable>();
                        selectContextClickable.onClick = eventData =>
                        {
                            if (eventData.button == PointerEventData.InputButton.Right)
                            {
                                EditorContextMenu.inst.ShowContextMenu(
                                    new ButtonFunction($"Use {RTEditor.SYSTEM_BROWSER}", () => OpenImageSelector(beatmapObject)),
                                    new ButtonFunction($"Use {RTEditor.EDITOR_BROWSER}", () =>
                                    {
                                        var editorPath = RTFile.RemoveEndSlash(EditorLevelManager.inst.CurrentLevel.path);
                                        RTEditor.inst.BrowserPopup.Open();
                                        RTFileBrowser.inst.UpdateBrowserFile(new string[] { FileFormat.PNG.Dot(), FileFormat.JPG.Dot() }, file =>
                                        {
                                            SelectImage(file, beatmapObject);
                                            RTEditor.inst.BrowserPopup.Close();
                                        });
                                    }),
                                    new ButtonFunction($"Store & Use {RTEditor.SYSTEM_BROWSER}", () => OpenImageSelector(beatmapObject, copyFile: false, storeImage: true)),
                                    new ButtonFunction($"Store & Use {RTEditor.EDITOR_BROWSER}", () =>
                                    {
                                        var editorPath = RTFile.RemoveEndSlash(EditorLevelManager.inst.CurrentLevel.path);
                                        RTEditor.inst.BrowserPopup.Open();
                                        RTFileBrowser.inst.UpdateBrowserFile(new string[] { FileFormat.PNG.Dot(), FileFormat.JPG.Dot() }, file =>
                                        {
                                            SelectImage(file, beatmapObject, copyFile: false, storeImage: true);
                                            RTEditor.inst.BrowserPopup.Close();
                                        });
                                    }),
                                    new ButtonFunction(true),
                                    new ButtonFunction("Remove Image", () =>
                                    {
                                        beatmapObject.text = string.Empty;

                                        // Since setting image has no affect on the timeline object, we will only need to update the physical object.
                                        if (UpdateObjects)
                                            RTLevel.Current?.UpdateObject(beatmapObject, RTLevel.ObjectContext.SHAPE);

                                        RenderShape(beatmapObject);
                                    }),
                                    new ButtonFunction("Delete Image", () => RTEditor.inst.ShowWarningPopup("Are you sure you want to delete the image and remove it from the image object?", () =>
                                    {
                                        RTFile.DeleteFile(RTFile.CombinePaths(EditorLevelManager.inst.CurrentLevel.path, beatmapObject.text));

                                        beatmapObject.text = string.Empty;

                                // Since setting image has no affect on the timeline object, we will only need to update the physical object.
                                if (UpdateObjects)
                                            RTLevel.Current?.UpdateObject(beatmapObject, RTLevel.ObjectContext.SHAPE);

                                        RenderShape(beatmapObject);
                                    }, RTEditor.inst.HideWarningPopup))
                                    );
                                return;
                            }
                            OpenImageSelector(beatmapObject);
                        };
                        shapeSettings.Find("7/text").GetComponent<Text>().text = string.IsNullOrEmpty(beatmapObject.text) ? "No image selected" : beatmapObject.text;

                        // Stores / Removes Image Data for transfering of Image Objects between levels.
                        var dataText = shapeSettings.Find("7/set/Text").GetComponent<Text>();
                        dataText.text = !GameData.Current.assets.sprites.Has(x => x.name == beatmapObject.text) ? "Store Data" : "Clear Data";
                        var set = shapeSettings.Find("7/set").GetComponent<Button>();
                        set.onClick.NewListener(() =>
                        {
                            var regex = new Regex(@"img\((.*?)\)");
                            var match = regex.Match(beatmapObject.text);

                            var path = match.Success ? RTFile.CombinePaths(RTFile.BasePath, match.Groups[1].ToString()) : RTFile.CombinePaths(RTFile.BasePath, beatmapObject.text);

                            if (!GameData.Current.assets.sprites.Has(x => x.name == beatmapObject.text))
                                StoreImage(beatmapObject, path);
                            else
                            {
                                GameData.Current.assets.RemoveSprite(beatmapObject.text);
                                if (!RTFile.FileExists(path))
                                    beatmapObject.text = string.Empty;
                            }

                            RTLevel.Current?.UpdateObject(beatmapObject, RTLevel.ObjectContext.IMAGE);

                            RenderShape(beatmapObject);
                        });

                        break;
                    }
                case ShapeType.Polygon: {
                        shapeSettings.AsRT().sizeDelta = new Vector2(351f, 276f);

                        var radius = shapeSettings.Find("10/radius").gameObject.GetComponent<InputFieldStorage>();
                        radius.inputField.onValueChanged.ClearAll();
                        radius.inputField.text = beatmapObject.polygonShape.Radius.ToString();
                        radius.SetInteractible(!EditorConfig.Instance.AutoPolygonRadius.Value);
                        if (!EditorConfig.Instance.AutoPolygonRadius.Value)
                        {
                            radius.inputField.onValueChanged.AddListener(_val =>
                            {
                                if (float.TryParse(_val, out float num))
                                {
                                    num = Mathf.Clamp(num, 0.1f, 10f);
                                    beatmapObject.polygonShape.Radius = num;
                                    RTLevel.Current?.UpdateObject(beatmapObject, RTLevel.ObjectContext.POLYGONS);
                                }
                            });

                            TriggerHelper.IncreaseDecreaseButtons(radius, min: 0.1f, max: 10f);
                            TriggerHelper.AddEventTriggers(radius.inputField.gameObject, TriggerHelper.ScrollDelta(radius.inputField, min: 0.1f, max: 10f));
                        }

                        var contextMenu = radius.inputField.gameObject.GetOrAddComponent<ContextClickable>();
                        contextMenu.onClick = eventData =>
                        {
                            if (eventData.button != PointerEventData.InputButton.Right)
                                return;

                            var buttonFunctions = new List<ButtonFunction>()
                            {
                                new ButtonFunction($"Auto Assign Radius [{(EditorConfig.Instance.AutoPolygonRadius.Value ? "On" : "Off")}]", () =>
                                {
                                    EditorConfig.Instance.AutoPolygonRadius.Value = !EditorConfig.Instance.AutoPolygonRadius.Value;
                                    RenderShape(beatmapObject);
                                })
                            };

                            if (!EditorConfig.Instance.AutoPolygonRadius.Value)
                            {
                                buttonFunctions.Add(new ButtonFunction("Set to Triangle Radius", () =>
                                {
                                    beatmapObject.polygonShape.Radius = PolygonShape.TRIANGLE_RADIUS;
                                    RTLevel.Current?.UpdateObject(beatmapObject, RTLevel.ObjectContext.POLYGONS);
                                }));
                                buttonFunctions.Add(new ButtonFunction("Set to Square Radius", () =>
                                {
                                    beatmapObject.polygonShape.Radius = PolygonShape.SQUARE_RADIUS;
                                    RTLevel.Current?.UpdateObject(beatmapObject, RTLevel.ObjectContext.POLYGONS);
                                }));
                                buttonFunctions.Add(new ButtonFunction("Set to Normal Radius", () =>
                                {
                                    beatmapObject.polygonShape.Radius = PolygonShape.NORMAL_RADIUS;
                                    RTLevel.Current?.UpdateObject(beatmapObject, RTLevel.ObjectContext.POLYGONS);
                                }));
                            }

                            EditorContextMenu.inst.ShowContextMenu(buttonFunctions);
                        };

                        var sides = shapeSettings.Find("10/sides").gameObject.GetComponent<InputFieldStorage>();
                        sides.inputField.SetTextWithoutNotify(beatmapObject.polygonShape.Sides.ToString());
                        sides.inputField.onValueChanged.NewListener(_val =>
                        {
                            if (int.TryParse(_val, out int num))
                            {
                                num = Mathf.Clamp(num, 3, 32);
                                beatmapObject.polygonShape.Sides = num;
                                if (EditorConfig.Instance.AutoPolygonRadius.Value)
                                {
                                    beatmapObject.polygonShape.Radius = beatmapObject.polygonShape.GetAutoRadius();
                                    radius.inputField.SetTextWithoutNotify(beatmapObject.polygonShape.Radius.ToString());
                                }
                                RTLevel.Current?.UpdateObject(beatmapObject, RTLevel.ObjectContext.POLYGONS);
                            }
                        });

                        TriggerHelper.IncreaseDecreaseButtonsInt(sides, min: 3, max: 32);
                        TriggerHelper.AddEventTriggers(sides.inputField.gameObject, TriggerHelper.ScrollDeltaInt(sides.inputField, min: 3, max: 32));
                        
                        var roundness = shapeSettings.Find("10/roundness").gameObject.GetComponent<InputFieldStorage>();
                        roundness.inputField.SetTextWithoutNotify(beatmapObject.polygonShape.Roundness.ToString());
                        roundness.inputField.onValueChanged.NewListener(_val =>
                        {
                            if (float.TryParse(_val, out float num))
                            {
                                num = Mathf.Clamp(num, 0f, 1f);
                                beatmapObject.polygonShape.Roundness = num;
                                RTLevel.Current?.UpdateObject(beatmapObject, RTLevel.ObjectContext.POLYGONS);
                            }
                        });

                        TriggerHelper.IncreaseDecreaseButtons(roundness, max: 1f);
                        TriggerHelper.AddEventTriggers(roundness.inputField.gameObject, TriggerHelper.ScrollDelta(roundness.inputField, max: 1f));

                        var thickness = shapeSettings.Find("10/thickness").gameObject.GetComponent<InputFieldStorage>();
                        thickness.inputField.SetTextWithoutNotify(beatmapObject.polygonShape.Thickness.ToString());
                        thickness.inputField.onValueChanged.NewListener(_val =>
                        {
                            if (float.TryParse(_val, out float num))
                            {
                                num = Mathf.Clamp(num, 0f, 1f);
                                beatmapObject.polygonShape.Thickness = num;
                                RTLevel.Current?.UpdateObject(beatmapObject, RTLevel.ObjectContext.POLYGONS);
                            }
                        });

                        TriggerHelper.IncreaseDecreaseButtons(thickness, max: 1f);
                        TriggerHelper.AddEventTriggers(thickness.inputField.gameObject, TriggerHelper.ScrollDelta(thickness.inputField, max: 1f));
                        
                        var thicknessOffsetX = shapeSettings.Find("10/thickness offset/x").gameObject.GetComponent<InputFieldStorage>();
                        thicknessOffsetX.inputField.SetTextWithoutNotify(beatmapObject.polygonShape.ThicknessOffset.x.ToString());
                        thicknessOffsetX.inputField.onValueChanged.NewListener(_val =>
                        {
                            if (float.TryParse(_val, out float num))
                            {
                                beatmapObject.polygonShape.ThicknessOffset = new Vector2(num, beatmapObject.polygonShape.ThicknessOffset.y);
                                RTLevel.Current?.UpdateObject(beatmapObject, RTLevel.ObjectContext.POLYGONS);
                            }
                        });

                        TriggerHelper.IncreaseDecreaseButtons(thicknessOffsetX);
                        TriggerHelper.AddEventTriggers(thicknessOffsetX.inputField.gameObject, TriggerHelper.ScrollDelta(thicknessOffsetX.inputField));
                        
                        var thicknessOffsetY = shapeSettings.Find("10/thickness offset/y").gameObject.GetComponent<InputFieldStorage>();
                        thicknessOffsetY.inputField.SetTextWithoutNotify(beatmapObject.polygonShape.ThicknessOffset.y.ToString());
                        thicknessOffsetY.inputField.onValueChanged.NewListener(_val =>
                        {
                            if (float.TryParse(_val, out float num))
                            {
                                beatmapObject.polygonShape.ThicknessOffset = new Vector2(beatmapObject.polygonShape.ThicknessOffset.x, num);
                                RTLevel.Current?.UpdateObject(beatmapObject, RTLevel.ObjectContext.POLYGONS);
                            }
                        });

                        TriggerHelper.IncreaseDecreaseButtons(thicknessOffsetY);
                        TriggerHelper.AddEventTriggers(thicknessOffsetY.inputField.gameObject, TriggerHelper.ScrollDelta(thicknessOffsetY.inputField));
                        
                        var thicknessScaleX = shapeSettings.Find("10/thickness scale/x").gameObject.GetComponent<InputFieldStorage>();
                        thicknessScaleX.inputField.SetTextWithoutNotify(beatmapObject.polygonShape.ThicknessScale.x.ToString());
                        thicknessScaleX.inputField.onValueChanged.NewListener(_val =>
                        {
                            if (float.TryParse(_val, out float num))
                            {
                                beatmapObject.polygonShape.ThicknessScale = new Vector2(num, beatmapObject.polygonShape.ThicknessScale.y);
                                RTLevel.Current?.UpdateObject(beatmapObject, RTLevel.ObjectContext.POLYGONS);
                            }
                        });

                        TriggerHelper.IncreaseDecreaseButtons(thicknessScaleX);
                        TriggerHelper.AddEventTriggers(thicknessScaleX.inputField.gameObject, TriggerHelper.ScrollDelta(thicknessScaleX.inputField));
                        
                        var thicknessScaleY = shapeSettings.Find("10/thickness scale/y").gameObject.GetComponent<InputFieldStorage>();
                        thicknessScaleY.inputField.SetTextWithoutNotify(beatmapObject.polygonShape.ThicknessScale.y.ToString());
                        thicknessScaleY.inputField.onValueChanged.NewListener(_val =>
                        {
                            if (float.TryParse(_val, out float num))
                            {
                                beatmapObject.polygonShape.ThicknessScale = new Vector2(beatmapObject.polygonShape.ThicknessScale.x, num);
                                RTLevel.Current?.UpdateObject(beatmapObject, RTLevel.ObjectContext.POLYGONS);
                            }
                        });

                        TriggerHelper.IncreaseDecreaseButtons(thicknessScaleY);
                        TriggerHelper.AddEventTriggers(thicknessScaleY.inputField.gameObject, TriggerHelper.ScrollDelta(thicknessScaleY.inputField));

                        var slices = shapeSettings.Find("10/slices").gameObject.GetComponent<InputFieldStorage>();
                        slices.inputField.SetTextWithoutNotify(beatmapObject.polygonShape.Slices.ToString());
                        slices.inputField.onValueChanged.NewListener(_val =>
                        {
                            if (int.TryParse(_val, out int num))
                            {
                                num = Mathf.Clamp(num, 1, 32);
                                beatmapObject.polygonShape.Slices = num;
                                RTLevel.Current?.UpdateObject(beatmapObject, RTLevel.ObjectContext.POLYGONS);
                            }
                        });

                        TriggerHelper.IncreaseDecreaseButtonsInt(slices, min: 1, max: 32);
                        TriggerHelper.AddEventTriggers(slices.inputField.gameObject, TriggerHelper.ScrollDeltaInt(slices.inputField, min: 1, max: 32));
                        
                        var rotation = shapeSettings.Find("10/rotation").gameObject.GetComponent<InputFieldStorage>();
                        rotation.inputField.SetTextWithoutNotify(beatmapObject.polygonShape.Angle.ToString());
                        rotation.inputField.onValueChanged.NewListener(_val =>
                        {
                            if (float.TryParse(_val, out float num))
                            {
                                beatmapObject.polygonShape.Angle = num;
                                RTLevel.Current?.UpdateObject(beatmapObject, RTLevel.ObjectContext.POLYGONS);
                            }
                        });

                        TriggerHelper.IncreaseDecreaseButtons(rotation, 15f, 3f);
                        TriggerHelper.AddEventTriggers(rotation.inputField.gameObject, TriggerHelper.ScrollDelta(rotation.inputField, 15f, 3f));

                        break;
                    }
                default: {
                        shapeSettings.AsRT().sizeDelta = new Vector2(351f, 32f);
                        shapeSettings.GetChild(4).AsRT().sizeDelta = new Vector2(351f, 32f);

                        num = 0;
                        foreach (var toggle in Dialog.ShapeOptionToggles[beatmapObject.Shape])
                        {
                            int index = num;
                            toggle.SetIsOnWithoutNotify(beatmapObject.shapeOption == index);
                            toggle.gameObject.SetActive(RTEditor.ShowModdedUI || index < Shape.unmoddedMaxShapes[beatmapObject.Shape]);

                            if (RTEditor.ShowModdedUI || index < Shape.unmoddedMaxShapes[beatmapObject.Shape])
                                toggle.onValueChanged.NewListener(_val =>
                                {
                                    beatmapObject.ShapeOption = index;

                                    // Since shape has no affect on the timeline object, we will only need to update the physical object.
                                    if (UpdateObjects)
                                        RTLevel.Current?.UpdateObject(beatmapObject, RTLevel.ObjectContext.SHAPE);

                                    RenderShape(beatmapObject);
                                });

                            num++;
                        }

                        break;
                    }
            }
        }

        void SetDepthSlider(BeatmapObject beatmapObject, int value, InputField inputField, Slider slider)
        {
            if (!RTEditor.ShowModdedUI)
                value = Mathf.Clamp(value, EditorConfig.Instance.RenderDepthRange.Value.y, EditorConfig.Instance.RenderDepthRange.Value.x);

            beatmapObject.Depth = value;

            slider.SetValueWithoutNotify(value);
            slider.onValueChanged.NewListener(_val => SetDepthInputField(beatmapObject, ((int)_val).ToString(), inputField, slider));

            // Since depth has no affect on the timeline object, we will only need to update the physical object.
            if (UpdateObjects)
                RTLevel.Current?.UpdateObject(beatmapObject, RTLevel.ObjectContext.VISUAL_OFFSET);
        }

        void SetDepthInputField(BeatmapObject beatmapObject, string value, InputField inputField, Slider slider)
        {
            if (!int.TryParse(value, out int num))
                return;

            if (!RTEditor.ShowModdedUI)
                num = Mathf.Clamp(num, EditorConfig.Instance.RenderDepthRange.Value.y, EditorConfig.Instance.RenderDepthRange.Value.x);

            beatmapObject.Depth = num;

            inputField.SetTextWithoutNotify(num.ToString());
            inputField.onValueChanged.NewListener(_val =>
            {
                if (int.TryParse(_val, out int numb))
                    SetDepthSlider(beatmapObject, numb, inputField, slider);
            });

            // Since depth has no affect on the timeline object, we will only need to update the physical object.
            if (UpdateObjects)
                RTLevel.Current?.UpdateObject(beatmapObject, RTLevel.ObjectContext.VISUAL_OFFSET);
        }

        /// <summary>
        /// Renders the Depth InputField and Slider.
        /// </summary>
        /// <param name="beatmapObject">The BeatmapObject to set.</param>
        public void RenderDepth(BeatmapObject beatmapObject)
        {
            var active = !HideVisualElementsWhenObjectIsEmpty || beatmapObject.objectType != BeatmapObject.ObjectType.Empty;

            Dialog.DepthParent.gameObject.SetActive(active);

            var depthTf = Dialog.DepthField.transform.parent;
            depthTf.parent.GetChild(depthTf.GetSiblingIndex() - 1).gameObject.SetActive(active);
            depthTf.gameObject.SetActive(RTEditor.NotSimple && active);
            Dialog.DepthSlider.transform.AsRT().sizeDelta = new Vector2(RTEditor.NotSimple ? 352f : 292f, 32f);

            var renderTypeTF = Dialog.RenderTypeDropdown.transform;
            renderTypeTF.parent.GetChild(renderTypeTF.GetSiblingIndex() - 1).gameObject.SetActive(active && RTEditor.ShowModdedUI);
            renderTypeTF.gameObject.SetActive(active && RTEditor.ShowModdedUI);

            if (!active)
                return;

            Dialog.DepthField.inputField.SetTextWithoutNotify(beatmapObject.Depth.ToString());
            Dialog.DepthField.inputField.onValueChanged.NewListener(_val =>
            {
                if (int.TryParse(_val, out int num))
                    SetDepthSlider(beatmapObject, num, Dialog.DepthField.inputField, Dialog.DepthSlider);
            });

            var max = EditorConfig.Instance.EditorComplexity.Value == Complexity.Simple ? 30 : EditorConfig.Instance.RenderDepthRange.Value.x;
            var min = EditorConfig.Instance.EditorComplexity.Value == Complexity.Simple ? 0 : EditorConfig.Instance.RenderDepthRange.Value.y;

            Dialog.DepthSlider.maxValue = max;
            Dialog.DepthSlider.minValue = min;

            Dialog.DepthSlider.SetValueWithoutNotify(beatmapObject.Depth);
            Dialog.DepthSlider.onValueChanged.NewListener(_val => SetDepthInputField(beatmapObject, _val.ToString(), Dialog.DepthField.inputField, Dialog.DepthSlider));

            if (RTEditor.ShowModdedUI)
            {
                max = 0;
                min = 0;
            }

            TriggerHelper.IncreaseDecreaseButtonsInt(Dialog.DepthField, -1, min, max);
            TriggerHelper.AddEventTriggers(Dialog.DepthField.inputField.gameObject, TriggerHelper.ScrollDeltaInt(Dialog.DepthField.inputField, 1, min, max));
            TriggerHelper.IncreaseDecreaseButtonsInt(Dialog.DepthField.inputField, -1, min, max, Dialog.DepthParent);

            // allow negative flipping
            if (min < 0)
                TriggerHelper.InversableField(Dialog.DepthField);
            else if (Dialog.DepthField.fieldSwapper)
                CoreHelper.Destroy(Dialog.DepthField.fieldSwapper);

            Dialog.RenderTypeDropdown.SetValueWithoutNotify((int)beatmapObject.renderLayerType);
            Dialog.RenderTypeDropdown.onValueChanged.NewListener(_val =>
            {
                beatmapObject.renderLayerType = (BeatmapObject.RenderLayerType)_val;
                RTLevel.Current?.UpdateObject(beatmapObject, RTLevel.ObjectContext.RENDERING);
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

            if (Dialog.UnityExplorerLabel)
                Dialog.UnityExplorerLabel.transform.parent.gameObject.SetActive(RTEditor.ShowModdedUI);

            if (Dialog.InspectBeatmapObjectButton)
            {
                Dialog.InspectBeatmapObjectButton.gameObject.SetActive(RTEditor.ShowModdedUI);
                Dialog.InspectBeatmapObjectButton.button.onClick.NewListener(() => ModCompatibility.Inspect(beatmapObject));
            }

            if (Dialog.InspectLevelObjectButton)
            {
                bool active = beatmapObject.runtimeObject && RTEditor.ShowModdedUI;
                Dialog.InspectLevelObjectButton.gameObject.SetActive(active);
                Dialog.InspectLevelObjectButton.button.onClick.NewListener(() => ModCompatibility.Inspect(beatmapObject.runtimeObject));
            }

            if (Dialog.InspectTimelineObjectButton)
            {
                Dialog.InspectTimelineObjectButton.gameObject.SetActive(RTEditor.ShowModdedUI);
                Dialog.InspectTimelineObjectButton.button.onClick.NewListener(() => ModCompatibility.Inspect(EditorTimeline.inst.GetTimelineObject(beatmapObject)));
            }
        }

        /// <summary>
        /// Renders the Layers InputField.
        /// </summary>
        /// <param name="beatmapObject">The BeatmapObject to set.</param>
        public void RenderLayers(BeatmapObject beatmapObject)
        {
            Dialog.EditorLayerField.gameObject.SetActive(RTEditor.NotSimple);

            if (RTEditor.NotSimple)
            {
                Dialog.EditorLayerField.SetTextWithoutNotify((beatmapObject.editorData.Layer + 1).ToString());
                Dialog.EditorLayerField.image.color = EditorTimeline.GetLayerColor(beatmapObject.editorData.Layer);
                Dialog.EditorLayerField.onValueChanged.NewListener(_val =>
                {
                    if (int.TryParse(_val, out int num))
                    {
                        num = Mathf.Clamp(num - 1, 0, int.MaxValue);
                        beatmapObject.editorData.Layer = num;
                        EditorTimeline.inst.RenderTimelineObject(EditorTimeline.inst.GetTimelineObject(beatmapObject));
                        RenderLayers(beatmapObject);
                    }
                });

                if (Dialog.EditorLayerField.gameObject)
                    TriggerHelper.AddEventTriggers(Dialog.EditorLayerField.gameObject, TriggerHelper.ScrollDeltaInt(Dialog.EditorLayerField, 1, 1, int.MaxValue));

                var editorLayerContextMenu = Dialog.EditorLayerField.gameObject.GetOrAddComponent<ContextClickable>();
                editorLayerContextMenu.onClick = eventData =>
                {
                    if (eventData.button != PointerEventData.InputButton.Right)
                        return;

                    EditorContextMenu.inst.ShowContextMenu(
                        new ButtonFunction("Go to Editor Layer", () => EditorTimeline.inst.SetLayer(beatmapObject.editorData.Layer, EditorTimeline.LayerType.Objects))
                        );
                };
            }

            if (Dialog.EditorLayerToggles == null)
                return;

            Dialog.EditorSettingsParent.Find("layer").gameObject.SetActive(!RTEditor.NotSimple);

            if (RTEditor.NotSimple)
                return;

            for (int i = 0; i < Dialog.EditorLayerToggles.Length; i++)
            {
                var index = i;
                var toggle = Dialog.EditorLayerToggles[i];
                toggle.SetIsOnWithoutNotify(index == beatmapObject.editorData.Layer);
                toggle.onValueChanged.NewListener(_val =>
                {
                    beatmapObject.editorData.Layer = index;
                    EditorTimeline.inst.RenderTimelineObject(EditorTimeline.inst.GetTimelineObject(beatmapObject));
                    RenderLayers(beatmapObject);
                });
            }
        }

        /// <summary>
        /// Renders the Bin Slider.
        /// </summary>
        /// <param name="beatmapObject">The BeatmapObject to set.</param>
        public void RenderBin(BeatmapObject beatmapObject)
        {
            Dialog.BinSlider.onValueChanged.ClearAll();
            Dialog.BinSlider.maxValue = EditorTimeline.inst.BinCount;
            Dialog.BinSlider.value = beatmapObject.editorData.Bin;
            Dialog.BinSlider.onValueChanged.AddListener(_val =>
            {
                beatmapObject.editorData.Bin = Mathf.Clamp((int)_val, 0, EditorTimeline.inst.BinCount);

                // Since bin has no effect on the physical object, we will only need to update the timeline object.
                EditorTimeline.inst.RenderTimelineObject(EditorTimeline.inst.GetTimelineObject(beatmapObject));
            });
        }

        /// <summary>
        /// Renders the Index field.
        /// </summary>
        /// <param name="beatmapObject">The BeatmapObject to set.</param>
        public void RenderIndex(BeatmapObject beatmapObject)
        {
            if (!Dialog.EditorIndexField)
                return;

            Dialog.Content.Find("indexer_label").gameObject.SetActive(RTEditor.ShowModdedUI);
            Dialog.EditorIndexField.gameObject.SetActive(RTEditor.ShowModdedUI);

            if (!RTEditor.ShowModdedUI)
                return;

            var currentIndex = GameData.Current.beatmapObjects.FindIndex(x => x.id == beatmapObject.id);
            Dialog.EditorIndexField.inputField.onEndEdit.ClearAll();
            Dialog.EditorIndexField.inputField.onValueChanged.ClearAll();
            Dialog.EditorIndexField.inputField.text = currentIndex.ToString();
            Dialog.EditorIndexField.inputField.onEndEdit.AddListener(_val =>
            {
                if (currentIndex < 0)
                {
                    EditorManager.inst.DisplayNotification($"Object is not in the Beatmap Object list.", 2f, EditorManager.NotificationType.Error);
                    return;
                }

                if (int.TryParse(_val, out int index))
                {
                    index = Mathf.Clamp(index, 0, GameData.Current.beatmapObjects.Count - 1);
                    if (currentIndex == index)
                        return;

                    GameData.Current.beatmapObjects.Move(currentIndex, index);
                    EditorTimeline.inst.UpdateTransformIndex();
                    RenderIndex(beatmapObject);
                }
            });

            Dialog.EditorIndexField.leftGreaterButton.onClick.NewListener(() =>
            {
                var index = GameData.Current.beatmapObjects.FindIndex(x => x == beatmapObject);
                if (index <= 0)
                {
                    EditorManager.inst.DisplayNotification("Could not move object back since it's already at the start.", 3f, EditorManager.NotificationType.Error);
                    return;
                }

                GameData.Current.beatmapObjects.Move(index, 0);
                EditorTimeline.inst.UpdateTransformIndex();
                RenderIndex(beatmapObject);
            });
            Dialog.EditorIndexField.leftButton.onClick.NewListener(() =>
            {
                var index = GameData.Current.beatmapObjects.FindIndex(x => x == beatmapObject);
                if (index <= 0)
                {
                    EditorManager.inst.DisplayNotification("Could not move object back since it's already at the start.", 3f, EditorManager.NotificationType.Error);
                    return;
                }

                GameData.Current.beatmapObjects.Move(index, index - 1);
                EditorTimeline.inst.UpdateTransformIndex();
                RenderIndex(beatmapObject);
            });
            Dialog.EditorIndexField.rightButton.onClick.NewListener(() =>
            {
                var index = GameData.Current.beatmapObjects.FindIndex(x => x == beatmapObject);
                if (index >= GameData.Current.beatmapObjects.Count - 1)
                {
                    EditorManager.inst.DisplayNotification("Could not move object forwards since it's already at the end.", 3f, EditorManager.NotificationType.Error);
                    return;
                }

                GameData.Current.beatmapObjects.Move(index, index + 1);
                EditorTimeline.inst.UpdateTransformIndex();
                RenderIndex(beatmapObject);
            });
            Dialog.EditorIndexField.rightGreaterButton.onClick.NewListener(() =>
            {
                var index = GameData.Current.beatmapObjects.FindIndex(x => x == beatmapObject);
                if (index >= GameData.Current.beatmapObjects.Count - 1)
                {
                    EditorManager.inst.DisplayNotification("Could not move object forwards since it's already at the end.", 3f, EditorManager.NotificationType.Error);
                    return;
                }

                GameData.Current.beatmapObjects.Move(index, GameData.Current.beatmapObjects.Count - 1);
                EditorTimeline.inst.UpdateTransformIndex();
                RenderIndex(beatmapObject);
            });

            TriggerHelper.AddEventTriggers(Dialog.EditorIndexField.gameObject, TriggerHelper.CreateEntry(EventTriggerType.Scroll, eventData =>
            {
                var pointerEventData = (PointerEventData)eventData;

                if (!int.TryParse(Dialog.EditorIndexField.inputField.text, out int index))
                    return;

                if (pointerEventData.scrollDelta.y < 0f)
                    index -= (Input.GetKey(EditorConfig.Instance.ScrollwheelLargeAmountKey.Value) ? 10 : 1);
                if (pointerEventData.scrollDelta.y > 0f)
                    index += (Input.GetKey(EditorConfig.Instance.ScrollwheelLargeAmountKey.Value) ? 10 : 1);

                if (index < 0)
                {
                    EditorManager.inst.DisplayNotification("Could not move object back since it's already at the start.", 3f, EditorManager.NotificationType.Error);
                    return;
                }
                if (index > GameData.Current.beatmapObjects.Count - 1)
                {
                    EditorManager.inst.DisplayNotification("Could not move object forwards since it's already at the end.", 3f, EditorManager.NotificationType.Error);
                    return;
                }

                GameData.Current.beatmapObjects.Move(currentIndex, index);
                EditorTimeline.inst.UpdateTransformIndex();
                RenderIndex(beatmapObject);
            }));

            var contextMenu = Dialog.EditorIndexField.inputField.gameObject.GetOrAddComponent<ContextClickable>();
            contextMenu.onClick = pointerEventData =>
            {
                if (pointerEventData.button != PointerEventData.InputButton.Right)
                    return;

                EditorContextMenu.inst.ShowContextMenu(
                    new ButtonFunction("Select Previous", () =>
                    {
                        if (currentIndex <= 0)
                        {
                            EditorManager.inst.DisplayNotification($"There are no previous objects to select.", 2f, EditorManager.NotificationType.Error);
                            return;
                        }

                        var prevObject = GameData.Current.beatmapObjects[currentIndex - 1];

                        if (!prevObject)
                            return;

                        var timelineObject = EditorTimeline.inst.GetTimelineObject(prevObject);

                        if (timelineObject)
                            EditorTimeline.inst.SetCurrentObject(timelineObject, EditorConfig.Instance.BringToSelection.Value);
                    }),
                    new ButtonFunction("Select Previous", () =>
                    {
                        if (currentIndex >= GameData.Current.beatmapObjects.Count - 1)
                        {
                            EditorManager.inst.DisplayNotification($"There are no previous objects to select.", 2f, EditorManager.NotificationType.Error);
                            return;
                        }

                        var nextObject = GameData.Current.beatmapObjects[currentIndex + 1];

                        if (!nextObject)
                            return;

                        var timelineObject = EditorTimeline.inst.GetTimelineObject(nextObject);

                        if (timelineObject)
                            EditorTimeline.inst.SetCurrentObject(timelineObject, EditorConfig.Instance.BringToSelection.Value);
                    }),
                    new ButtonFunction(true),
                    new ButtonFunction("Select First", () =>
                    {
                        if (GameData.Current.beatmapObjects.IsEmpty())
                        {
                            EditorManager.inst.DisplayNotification($"There are no Beatmap Objects!", 3f, EditorManager.NotificationType.Warning);
                            return;
                        }

                        var prevObject = GameData.Current.beatmapObjects.First();

                        if (!prevObject)
                            return;

                        var timelineObject = EditorTimeline.inst.GetTimelineObject(prevObject);

                        if (timelineObject)
                            EditorTimeline.inst.SetCurrentObject(timelineObject, EditorConfig.Instance.BringToSelection.Value);
                    }),
                    new ButtonFunction("Select Last", () =>
                    {
                        if (GameData.Current.beatmapObjects.IsEmpty())
                        {
                            EditorManager.inst.DisplayNotification($"There are no Beatmap Objects!", 3f, EditorManager.NotificationType.Warning);
                            return;
                        }

                        var nextObject = GameData.Current.beatmapObjects.Last();

                        if (!nextObject)
                            return;

                        var timelineObject = EditorTimeline.inst.GetTimelineObject(nextObject);

                        if (timelineObject)
                            EditorTimeline.inst.SetCurrentObject(timelineObject, EditorConfig.Instance.BringToSelection.Value);
                    }));
            };
        }

        /// <summary>
        /// Renders the Editor Colors.
        /// </summary>
        /// <param name="beatmapObject">The BeatmapObject to set.</param>
        public void RenderEditorColors(BeatmapObject beatmapObject)
        {
            Dialog.BaseColorField.SetTextWithoutNotify(beatmapObject.editorData.color);
            Dialog.BaseColorField.onValueChanged.NewListener(_val =>
            {
                beatmapObject.editorData.color = _val;
                beatmapObject.timelineObject?.RenderVisibleState(false);
            });
            var baseColorContextMenu = Dialog.BaseColorField.gameObject.GetOrAddComponent<ContextClickable>();
            baseColorContextMenu.onClick = pointerEventData =>
            {
                if (pointerEventData.button == PointerEventData.InputButton.Right)
                    beatmapObject.timelineObject?.ShowColorContextMenu(Dialog.BaseColorField, beatmapObject.editorData.color);
            };

            Dialog.SelectColorField.SetTextWithoutNotify(beatmapObject.editorData.selectedColor);
            Dialog.SelectColorField.onValueChanged.NewListener(_val =>
            {
                beatmapObject.editorData.selectedColor = _val;
                beatmapObject.timelineObject?.RenderVisibleState(false);
            });
            var selectColorContextMenu = Dialog.SelectColorField.gameObject.GetOrAddComponent<ContextClickable>();
            selectColorContextMenu.onClick = pointerEventData =>
            {
                if (pointerEventData.button == PointerEventData.InputButton.Right)
                    beatmapObject.timelineObject?.ShowColorContextMenu(Dialog.SelectColorField, beatmapObject.editorData.selectedColor);
            };

            Dialog.TextColorField.SetTextWithoutNotify(beatmapObject.editorData.textColor);
            Dialog.TextColorField.onValueChanged.NewListener(_val =>
            {
                beatmapObject.editorData.textColor = _val;
                beatmapObject.timelineObject?.RenderText(beatmapObject.name);
            });
            var textColorContextMenu = Dialog.TextColorField.gameObject.GetOrAddComponent<ContextClickable>();
            textColorContextMenu.onClick = pointerEventData =>
            {
                if (pointerEventData.button == PointerEventData.InputButton.Right)
                    beatmapObject.timelineObject?.ShowColorContextMenu(Dialog.TextColorField, beatmapObject.editorData.textColor);
            };

            Dialog.MarkColorField.SetTextWithoutNotify(beatmapObject.editorData.markColor);
            Dialog.MarkColorField.onValueChanged.NewListener(_val =>
            {
                beatmapObject.editorData.markColor = _val;
                beatmapObject.timelineObject?.RenderText(beatmapObject.name);
            });
            var markColorContextMenu = Dialog.MarkColorField.gameObject.GetOrAddComponent<ContextClickable>();
            markColorContextMenu.onClick = pointerEventData =>
            {
                if (pointerEventData.button == PointerEventData.InputButton.Right)
                    beatmapObject.timelineObject?.ShowColorContextMenu(Dialog.MarkColorField, beatmapObject.editorData.markColor);
            };
        }

        /// <summary>
        /// Renders the Prefab references.
        /// </summary>
        /// <param name="beatmapObject">The BeatmapObject to set.</param>
        public void RenderPrefabReference(BeatmapObject beatmapObject)
        {
            bool fromPrefab = !string.IsNullOrEmpty(beatmapObject.prefabID);
            Dialog.CollapsePrefabLabel.SetActive(fromPrefab);
            Dialog.CollapsePrefabButton.gameObject.SetActive(fromPrefab);
            Dialog.CollapsePrefabButton.button.onClick.ClearAll();

            var prefab = beatmapObject.GetPrefab();
            Dialog.PrefabName.gameObject.SetActive(prefab);
            if (prefab)
                Dialog.PrefabNameText.text = $"[ <b>{prefab.name}</b> ]";

            var collapsePrefabContextMenu = Dialog.CollapsePrefabButton.button.gameObject.GetOrAddComponent<ContextClickable>();
            collapsePrefabContextMenu.onClick = pointerEventData =>
            {
                if (pointerEventData.button != PointerEventData.InputButton.Right)
                {
                    if (EditorConfig.Instance.ShowCollapsePrefabWarning.Value)
                    {
                        RTEditor.inst.ShowWarningPopup("Are you sure you want to collapse this Prefab group and save the changes to the Internal Prefab?", () =>
                        {
                            RTPrefabEditor.inst.Collapse(beatmapObject, beatmapObject.editorData);
                            RTEditor.inst.HideWarningPopup();
                        }, RTEditor.inst.HideWarningPopup);

                        return;
                    }

                    RTPrefabEditor.inst.Collapse(beatmapObject, beatmapObject.editorData);
                    return;
                }

                EditorContextMenu.inst.ShowContextMenu(
                    new ButtonFunction("Apply", () => RTPrefabEditor.inst.Collapse(beatmapObject, beatmapObject.editorData)),
                    new ButtonFunction("Create New", () => RTPrefabEditor.inst.Collapse(beatmapObject, beatmapObject.editorData, true))
                    );
            };

            Dialog.AssignPrefabButton.button.onClick.NewListener(() =>
            {
                RTEditor.inst.selectingMultiple = false;
                RTEditor.inst.prefabPickerEnabled = true;
            });

            Dialog.RemovePrefabButton.button.onClick.NewListener(() =>
            {
                beatmapObject.RemovePrefabReference();
                EditorTimeline.inst.RenderTimelineObject(EditorTimeline.inst.GetTimelineObject(beatmapObject));
                OpenDialog(beatmapObject);
            });
        }

        void KeyframeHandler(int type, int valueIndex, IEnumerable<TimelineKeyframe> selected, TimelineKeyframe firstKF, BeatmapObject beatmapObject)
        {
            var isSingle = selected.Count() == 1;
            var dialog = Dialog.keyframeDialogs[type];
            var inputFieldStorage = dialog.EventValueFields[valueIndex];

            TriggerHelper.InversableField(inputFieldStorage);

            if (!inputFieldStorage.eventTrigger)
                inputFieldStorage.eventTrigger = inputFieldStorage.gameObject.AddComponent<EventTrigger>();

            inputFieldStorage.eventTrigger.triggers.Clear();

            var contextMenu = inputFieldStorage.inputField.gameObject.GetOrAddComponent<ContextClickable>();
            contextMenu.onClick = eventData =>
            {
                if (eventData.button != PointerEventData.InputButton.Right)
                    return;

                EditorContextMenu.inst.ShowContextMenu(
                    new ButtonFunction("Reset Value", () =>
                    {
                        switch (type)
                        {
                            case 0: {
                                    inputFieldStorage.inputField.text = "0";
                                    break;
                                }
                            case 1: {
                                    inputFieldStorage.inputField.text = "1";
                                    break;
                                }
                            case 2: {
                                    inputFieldStorage.inputField.text = "0";
                                    break;
                                }
                        }
                    }));
            };

            switch (type)
            {
                case 0: {
                        inputFieldStorage.eventTrigger.triggers.Add(TriggerHelper.ScrollDelta(inputFieldStorage.inputField, EditorConfig.Instance.ObjectPositionScroll.Value, EditorConfig.Instance.ObjectPositionScrollMultiply.Value, multi: true));
                        inputFieldStorage.eventTrigger.triggers.Add(TriggerHelper.ScrollDeltaVector2(dialog.EventValueFields[0].inputField, dialog.EventValueFields[1].inputField, EditorConfig.Instance.ObjectPositionScroll.Value, EditorConfig.Instance.ObjectPositionScrollMultiply.Value));
                        break;
                    }
                case 1: {
                        inputFieldStorage.eventTrigger.triggers.Add(TriggerHelper.ScrollDelta(inputFieldStorage.inputField, EditorConfig.Instance.ObjectScaleScroll.Value, EditorConfig.Instance.ObjectScaleScrollMultiply.Value, multi: true));
                        inputFieldStorage.eventTrigger.triggers.Add(TriggerHelper.ScrollDeltaVector2(dialog.EventValueFields[0].inputField, dialog.EventValueFields[1].inputField, EditorConfig.Instance.ObjectScaleScroll.Value, EditorConfig.Instance.ObjectScaleScrollMultiply.Value));
                        break;
                    }
                case 2: {
                        inputFieldStorage.eventTrigger.triggers.Add(TriggerHelper.ScrollDelta(inputFieldStorage.inputField, EditorConfig.Instance.ObjectRotationScroll.Value, EditorConfig.Instance.ObjectRotationScrollMultiply.Value));
                        break;
                    }
            }

            inputFieldStorage.inputField.characterValidation = InputField.CharacterValidation.None;
            inputFieldStorage.inputField.contentType = InputField.ContentType.Standard;
            inputFieldStorage.inputField.keyboardType = TouchScreenKeyboardType.Default;

            inputFieldStorage.inputField.SetTextWithoutNotify(isSingle ? firstKF.eventKeyframe.values[valueIndex].ToString() : type == 2 ? "15" : "1");
            inputFieldStorage.inputField.onValueChanged.NewListener(_val =>
            {
                if (isSingle && float.TryParse(_val, out float num))
                {
                    firstKF.eventKeyframe.values[valueIndex] = num;

                    // Since keyframe value has no affect on the timeline object, we will only need to update the physical object.
                    if (UpdateObjects)
                        RTLevel.Current?.UpdateObject(beatmapObject, RTLevel.ObjectContext.KEYFRAMES);
                }
            });
            inputFieldStorage.inputField.onEndEdit.NewListener(_val =>
            {
                if (!isSingle)
                    return;

                var variables = new Dictionary<string, float>
                {
                    { "eventTime", firstKF.eventKeyframe.time },
                    { "currentValue", firstKF.eventKeyframe.values[valueIndex] }
                };

                if (!float.TryParse(_val, out float n) && RTMath.TryParse(_val, firstKF.eventKeyframe.values[valueIndex], variables, out float calc))
                    inputFieldStorage.inputField.text = calc.ToString();
            });

            inputFieldStorage.leftButton.gameObject.SetActive(isSingle);
            inputFieldStorage.rightButton.gameObject.SetActive(isSingle);
            if (isSingle)
                TriggerHelper.IncreaseDecreaseButtons(inputFieldStorage, type == 2 ? 15f : 0.1f, type == 2 ? 3f : 10f);

            if (inputFieldStorage.addButton)
            {
                inputFieldStorage.addButton.onClick.ClearAll();
                inputFieldStorage.addButton.gameObject.SetActive(!isSingle);
                if (!isSingle)
                    inputFieldStorage.addButton.onClick.AddListener(() =>
                    {
                        if (float.TryParse(inputFieldStorage.inputField.text, out float x))
                        {
                            foreach (var keyframe in selected)
                                keyframe.eventKeyframe.values[valueIndex] += x;

                            // Since keyframe value has no affect on the timeline object, we will only need to update the physical object.
                            if (UpdateObjects)
                                RTLevel.Current?.UpdateObject(beatmapObject, RTLevel.ObjectContext.KEYFRAMES);
                        }
                        else
                        {
                            var variables = new Dictionary<string, float>
                            {
                                { "eventTime", firstKF.eventKeyframe.time },
                                { "currentValue", firstKF.eventKeyframe.values[valueIndex] }
                            };

                            if (RTMath.TryParse(inputFieldStorage.inputField.text, firstKF.eventKeyframe.values[valueIndex], variables, out float calc))
                                foreach (var keyframe in selected)
                                    keyframe.eventKeyframe.values[valueIndex] += calc;
                        }
                    });
            }
            if (inputFieldStorage.subButton)
            {
                inputFieldStorage.subButton.onClick.ClearAll();
                inputFieldStorage.subButton.gameObject.SetActive(!isSingle);
                if (!isSingle)
                    inputFieldStorage.subButton.onClick.AddListener(() =>
                    {
                        if (float.TryParse(inputFieldStorage.inputField.text, out float x))
                        {
                            foreach (var keyframe in selected)
                                keyframe.eventKeyframe.values[valueIndex] -= x;

                            // Since keyframe value has no affect on the timeline object, we will only need to update the physical object.
                            if (UpdateObjects)
                                RTLevel.Current?.UpdateObject(beatmapObject, RTLevel.ObjectContext.KEYFRAMES);
                        }
                        else
                        {
                            var variables = new Dictionary<string, float>
                            {
                                { "eventTime", firstKF.eventKeyframe.time },
                                { "currentValue", firstKF.eventKeyframe.values[valueIndex] }
                            };

                            if (RTMath.TryParse(inputFieldStorage.inputField.text, firstKF.eventKeyframe.values[valueIndex], variables, out float calc))
                                foreach (var keyframe in selected)
                                    keyframe.eventKeyframe.values[valueIndex] -= calc;
                        }
                    });
            }
            if (inputFieldStorage.middleButton)
            {
                inputFieldStorage.middleButton.onClick.ClearAll();
                inputFieldStorage.middleButton.gameObject.SetActive(!isSingle);
                if (!isSingle)
                    inputFieldStorage.middleButton.onClick.AddListener(() =>
                    {
                        if (float.TryParse(inputFieldStorage.inputField.text, out float x))
                        {
                            foreach (var keyframe in selected)
                                keyframe.eventKeyframe.values[valueIndex] = x;

                            // Since keyframe value has no affect on the timeline object, we will only need to update the physical object.
                            if (UpdateObjects)
                                RTLevel.Current?.UpdateObject(beatmapObject, RTLevel.ObjectContext.KEYFRAMES);
                        }
                        else
                        {
                            var variables = new Dictionary<string, float>
                            {
                                { "eventTime", firstKF.eventKeyframe.time },
                                { "currentValue", firstKF.eventKeyframe.values[valueIndex] }
                            };

                            if (RTMath.TryParse(inputFieldStorage.inputField.text, firstKF.eventKeyframe.values[valueIndex], variables, out float calc))
                                foreach (var keyframe in selected)
                                    keyframe.eventKeyframe.values[valueIndex] = calc;
                        }
                    });
            }

            inputFieldStorage.GetComponent<HorizontalLayoutGroup>().spacing = isSingle ? 8f : 0f;
        }

        void UpdateKeyframeRandomDialog(int type, int randomType)
        {
            var dialog = Dialog.keyframeDialogs[type];
            var kfdialog = dialog.GameObject.transform;

            if (dialog.RandomAxisDropdown)
                dialog.RandomAxisDropdown.gameObject.SetActive(RTEditor.ShowModdedUI && (randomType == 5 || randomType == 6));

            dialog.RandomEventValueLabels.SetActive(randomType != 0 && randomType != 5);
            dialog.RandomEventValueParent.SetActive(randomType != 0 && randomType != 5);
            dialog.RandomEventValueLabels.transform.GetChild(0).GetComponent<Text>().text = (randomType == 4) ? "Random Scale Min" : randomType == 6 ? "Minimum Range" : "Random X";
            dialog.RandomEventValueLabels.transform.GetChild(1).gameObject.SetActive(type != 2 || randomType == 6);
            dialog.RandomEventValueLabels.transform.GetChild(1).GetComponent<Text>().text = (randomType == 4) ? "Random Scale Max" : randomType == 6 ? "Maximum Range" : "Random Y";
            dialog.RandomIntervalField.gameObject.SetActive(randomType != 0 && randomType != 3 && randomType != 5);
            kfdialog.Find("r_label/interval").gameObject.SetActive(randomType != 0 && randomType != 3 && randomType != 5);

            if (dialog.FleeToggle)
            {
                var active = RTEditor.ShowModdedUI && randomType == 6;
                dialog.FleeToggle.gameObject.SetActive(active);
                if (active)
                    dialog.FleeToggle.label.text = type != 2 ? "Flee" : "Turn";
            }

            dialog.RandomEventValueParent.transform.GetChild(1).gameObject.SetActive(type != 2 || randomType == 6);

            dialog.RandomEventValueParent.transform.GetChild(0).GetChild(0).AsRT().sizeDelta = new Vector2(type != 2 || randomType == 6 ? 117 : 317f, 32f);
            dialog.RandomEventValueParent.transform.GetChild(1).GetChild(0).AsRT().sizeDelta = new Vector2(type != 2 || randomType == 6 ? 117 : 317f, 32f);

            if (randomType != 0 && randomType != 3 && randomType != 5)
                kfdialog.Find("r_label/interval").GetComponent<Text>().text = randomType == 6 ? "Delay" : "Random Interval";
        }

        void KeyframeRandomHandler(int type, IEnumerable<TimelineKeyframe> selected, TimelineKeyframe firstKF, BeatmapObject beatmapObject)
        {
            var dialog = Dialog.keyframeDialogs[type];

            int random = firstKF.eventKeyframe.random;

            for (int n = 0; n <= (type == 0 ? 5 : type == 2 ? 4 : 3); n++)
            {
                // We skip the 2nd random type for compatibility with old PA levels (for some reason).
                int buttonTmp = (n >= 2 && (type != 2 || n < 3)) ? (n + 1) : (n > 2 && type == 2) ? n + 2 : n;

                var active = buttonTmp != 5 && buttonTmp != 6 || RTEditor.ShowModdedUI;

                var toggle = dialog.RandomToggles[n];
                toggle.gameObject.SetActive(active);

                if (!active)
                    continue;

                toggle.SetIsOnWithoutNotify(random == buttonTmp);
                toggle.onValueChanged.NewListener(_val =>
                {
                    foreach (var keyframe in selected.Select(x => x.eventKeyframe))
                        keyframe.random = buttonTmp;

                    // Since keyframe value has no affect on the timeline object, we will only need to update the physical object.
                    if (UpdateObjects)
                        RTLevel.Current?.UpdateObject(beatmapObject, RTLevel.ObjectContext.KEYFRAMES);

                    UpdateKeyframeRandomDialog(type, buttonTmp);
                    KeyframeRandomHandler(type, selected, firstKF, beatmapObject);
                });
            }

            UpdateKeyframeRandomDialog(type, random);

            if (dialog.RandomAxisDropdown)
            {
                var active = (random == 5 || random == 6) && RTEditor.ShowModdedUI;
                dialog.RandomAxisDropdown.gameObject.SetActive(active);
                dialog.RandomAxisDropdown.onValueChanged.ClearAll();
                if (active)
                {
                    if (firstKF.eventKeyframe.randomValues.Length < 4)
                    {
                        var keyframe = firstKF.eventKeyframe;
                        keyframe.SetRandomValues(keyframe.randomValues[0], keyframe.randomValues[1], keyframe.randomValues[2], 0f);
                    }

                    dialog.RandomAxisDropdown.value = Mathf.Clamp((int)firstKF.eventKeyframe.randomValues[3], 0, 3);
                    dialog.RandomAxisDropdown.onValueChanged.AddListener(_val =>
                    {
                        foreach (var keyframe in selected.Select(x => x.eventKeyframe))
                            keyframe.SetRandomValues(keyframe.randomValues[0], keyframe.randomValues[1], keyframe.randomValues[2], _val);
                        RTLevel.Current?.UpdateObject(beatmapObject, RTLevel.ObjectContext.KEYFRAMES);
                    });
                }
            }

            if (dialog.FleeToggle)
            {
                var active = random == 6 && RTEditor.ShowModdedUI;
                dialog.FleeToggle.gameObject.SetActive(active);
                dialog.FleeToggle.toggle.onValueChanged.ClearAll();
                if (active)
                {
                    dialog.FleeToggle.toggle.isOn = firstKF.eventKeyframe.flee;
                    dialog.FleeToggle.toggle.onValueChanged.AddListener(_val =>
                    {
                        foreach (var keyframe in selected.Select(x => x.eventKeyframe))
                            keyframe.flee = _val;
                        RTLevel.Current?.UpdateObject(beatmapObject, RTLevel.ObjectContext.KEYFRAMES);
                    });
                }
            }

            if (!dialog.RandomIntervalField)
            {
                CoreHelper.LogError($"Random Interval Field is null.");
                return;
            }

            float num = 0f;
            if (firstKF.eventKeyframe.randomValues.Length > 2)
                num = firstKF.eventKeyframe.randomValues[2];

            dialog.RandomIntervalField.NewValueChangedListener(num.ToString(), _val =>
            {
                if (float.TryParse(_val, out float num))
                {
                    foreach (var keyframe in selected.Select(x => x.eventKeyframe))
                        keyframe.randomValues[2] = num;

                    // Since keyframe value has no affect on the timeline object, we will only need to update the physical object.
                    if (UpdateObjects)
                        RTLevel.Current?.UpdateObject(beatmapObject, RTLevel.ObjectContext.KEYFRAMES);
                }
            });

            TriggerHelper.InversableField(dialog.RandomIntervalField);

            TriggerHelper.AddEventTriggers(dialog.RandomIntervalField.gameObject, TriggerHelper.ScrollDelta(dialog.RandomIntervalField, max: float.MaxValue));
        }

        void KeyframeRandomValueHandler(int type, int valueIndex, IEnumerable<TimelineKeyframe> selected, TimelineKeyframe firstKF, BeatmapObject beatmapObject)
        {
            var dialog = Dialog.keyframeDialogs[type];
            var kfdialog = dialog.GameObject.transform;

            var random = firstKF.eventKeyframe.random;

            var inputFieldStorage = dialog.RandomEventValueFields[valueIndex];

            inputFieldStorage.inputField.characterValidation = InputField.CharacterValidation.None;
            inputFieldStorage.inputField.contentType = InputField.ContentType.Standard;
            inputFieldStorage.inputField.keyboardType = TouchScreenKeyboardType.Default;
            inputFieldStorage.inputField.SetTextWithoutNotify(selected.Count() == 1 ? firstKF.eventKeyframe.randomValues[valueIndex].ToString() : type == 2 ? "15" : "1");
            inputFieldStorage.inputField.onValueChanged.NewListener(_val =>
            {
                if (float.TryParse(_val, out float num) && selected.Count() == 1)
                {
                    firstKF.eventKeyframe.randomValues[valueIndex] = num;

                    // Since keyframe value has no affect on the timeline object, we will only need to update the physical object.
                    if (UpdateObjects)
                        RTLevel.Current?.UpdateObject(beatmapObject, RTLevel.ObjectContext.KEYFRAMES);
                }
            });

            inputFieldStorage.leftButton.onClick.NewListener(() =>
            {
                if (float.TryParse(inputFieldStorage.inputField.text, out float x))
                {
                    if (selected.Count() == 1)
                    {
                        inputFieldStorage.inputField.text = (x - (type == 2 ? 15f : 1f)).ToString();
                        return;
                    }

                    foreach (var keyframe in selected)
                        keyframe.eventKeyframe.randomValues[valueIndex] -= x;

                    // Since keyframe value has no affect on the timeline object, we will only need to update the physical object.
                    if (UpdateObjects)
                        RTLevel.Current?.UpdateObject(beatmapObject, RTLevel.ObjectContext.KEYFRAMES);
                }
            });

            inputFieldStorage.rightButton.onClick.NewListener(() =>
            {
                if (float.TryParse(inputFieldStorage.inputField.text, out float x))
                {
                    if (selected.Count() == 1)
                    {
                        inputFieldStorage.inputField.text = (x + (type == 2 ? 15f : 1f)).ToString();
                        return;
                    }

                    foreach (var keyframe in selected)
                        keyframe.eventKeyframe.randomValues[valueIndex] += x;

                    // Since keyframe value has no affect on the timeline object, we will only need to update the physical object.
                    if (UpdateObjects)
                        RTLevel.Current?.UpdateObject(beatmapObject, RTLevel.ObjectContext.KEYFRAMES);
                }
            });

            TriggerHelper.AddEventTriggers(inputFieldStorage.gameObject,
                TriggerHelper.ScrollDelta(inputFieldStorage.inputField, type == 2 && random != 6 ? 15f : 0.1f, type == 2 && random != 6 ? 3f : 10f, multi: true),
                TriggerHelper.ScrollDeltaVector2(inputFieldStorage.inputField, dialog.RandomEventValueFields[1].inputField, type == 2 && random != 6 ? 15f : 0.1f, type == 2 && random != 6 ? 3f : 10f));

            TriggerHelper.InversableField(inputFieldStorage);
        }

        void ColorKeyframeHandler(int valueIndex, List<Toggle> colorButtons, IEnumerable<TimelineKeyframe> selected, TimelineKeyframe firstKF, BeatmapObject beatmapObject)
        {
            bool showModifiedColors = EditorConfig.Instance.ShowModifiedColors.Value;
            var eventTime = firstKF.eventKeyframe.time;
            int index = 0;
            foreach (var toggle in colorButtons)
            {
                int tmpIndex = index;

                toggle.gameObject.SetActive(RTEditor.ShowModdedUI || tmpIndex < 9);

                toggle.SetIsOnWithoutNotify(index == firstKF.eventKeyframe.values[valueIndex]);
                toggle.onValueChanged.NewListener(_val => SetKeyframeColor(beatmapObject, valueIndex, tmpIndex, colorButtons, selected));

                if (showModifiedColors)
                {
                    var color = CoreHelper.CurrentBeatmapTheme.GetObjColor(tmpIndex);

                    float hueNum = beatmapObject.Interpolate(3, valueIndex + 2, eventTime);
                    float satNum = beatmapObject.Interpolate(3, valueIndex + 3, eventTime);
                    float valNum = beatmapObject.Interpolate(3, valueIndex + 4, eventTime);

                    toggle.image.color = RTColors.ChangeColorHSV(color, hueNum, satNum, valNum);
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
        }

        /// <summary>
        /// Renders the keyframe editors.
        /// </summary>
        /// <param name="beatmapObject">The BeatmapObject to set.</param>
        public void RenderObjectKeyframesDialog(BeatmapObject beatmapObject)
        {
            var selected = beatmapObject.timelineObject.InternalTimelineObjects.Where(x => x.Selected);
            var count = selected.Count();

            if (count < 1)
            {
                Dialog.CloseKeyframeDialogs();
                return;
            }

            if (!(count == 1 || selected.All(x => x.Type == selected.Min(y => y.Type))))
            {
                Dialog.OpenKeyframeDialog(4);

                try
                {
                    var multiDialog = Dialog.keyframeDialogs[4].GameObject.transform;
                    var time = multiDialog.Find("time/time/time").GetComponent<InputField>();
                    time.onValueChanged.ClearAll();
                    if (time.text == "100.000")
                        time.text = "10";

                    var setTime = multiDialog.Find("time/time").GetChild(3).GetComponent<Button>();
                    setTime.onClick.NewListener(() =>
                    {
                        if (float.TryParse(time.text, out float num))
                        {
                            if (num < 0f)
                                num = 0f;

                            if (EditorConfig.Instance.RoundToNearest.Value)
                                num = RTMath.RoundToNearestDecimal(num, 3);

                            foreach (var kf in selected.Where(x => x.Index != 0))
                                kf.Time = num;


                            RenderKeyframes(beatmapObject);

                            // Keyframe Time affects both physical object and timeline object.
                            EditorTimeline.inst.RenderTimelineObject(EditorTimeline.inst.GetTimelineObject(beatmapObject));
                            if (UpdateObjects)
                                RTLevel.Current?.UpdateObject(beatmapObject, RTLevel.ObjectContext.KEYFRAMES);

                            ResizeKeyframeTimeline(beatmapObject);
                            RenderMarkers(beatmapObject);
                        }
                    });

                    var decreaseTimeGreat = multiDialog.Find("time/time/<<").GetComponent<Button>();
                    var decreaseTime = multiDialog.Find("time/time/<").GetComponent<Button>();
                    var increaseTimeGreat = multiDialog.Find("time/time/>>").GetComponent<Button>();
                    var increaseTime = multiDialog.Find("time/time/>").GetComponent<Button>();

                    decreaseTime.onClick.NewListener(() =>
                    {
                        if (float.TryParse(time.text, out float num))
                        {
                            if (num < 0f)
                                num = 0f;

                            if (EditorConfig.Instance.RoundToNearest.Value)
                                num = RTMath.RoundToNearestDecimal(num, 3);

                            foreach (var kf in selected.Where(x => x.Index != 0))
                                kf.Time = Mathf.Clamp(kf.Time - num, 0f, float.MaxValue);

                            RenderKeyframes(beatmapObject);

                            // Keyframe Time affects both physical object and timeline object.
                            EditorTimeline.inst.RenderTimelineObject(EditorTimeline.inst.GetTimelineObject(beatmapObject));
                            if (UpdateObjects)
                                RTLevel.Current?.UpdateObject(beatmapObject, RTLevel.ObjectContext.KEYFRAMES);

                            ResizeKeyframeTimeline(beatmapObject);
                            RenderMarkers(beatmapObject);
                        }
                    });

                    increaseTime.onClick.NewListener(() =>
                    {
                        if (float.TryParse(time.text, out float num))
                        {
                            if (num < 0f)
                                num = 0f;

                            if (EditorConfig.Instance.RoundToNearest.Value)
                                num = RTMath.RoundToNearestDecimal(num, 3);

                            foreach (var kf in selected.Where(x => x.Index != 0))
                                kf.Time = Mathf.Clamp(kf.Time + num, 0f, float.MaxValue);

                            RenderKeyframes(beatmapObject);

                            // Keyframe Time affects both physical object and timeline object.
                            EditorTimeline.inst.RenderTimelineObject(EditorTimeline.inst.GetTimelineObject(beatmapObject));
                            if (UpdateObjects)
                                RTLevel.Current?.UpdateObject(beatmapObject, RTLevel.ObjectContext.KEYFRAMES);

                            ResizeKeyframeTimeline(beatmapObject);
                            RenderMarkers(beatmapObject);
                        }
                    });

                    decreaseTimeGreat.onClick.NewListener(() =>
                    {
                        if (float.TryParse(time.text, out float num))
                        {
                            if (num < 0f)
                                num = 0f;

                            if (EditorConfig.Instance.RoundToNearest.Value)
                                num = RTMath.RoundToNearestDecimal(num, 3);

                            foreach (var kf in selected.Where(x => x.Index != 0))
                                kf.Time = Mathf.Clamp(kf.Time - (num * 10f), 0f, float.MaxValue);

                            RenderKeyframes(beatmapObject);

                            // Keyframe Time affects both physical object and timeline object.
                            EditorTimeline.inst.RenderTimelineObject(EditorTimeline.inst.GetTimelineObject(beatmapObject));
                            if (UpdateObjects)
                                RTLevel.Current?.UpdateObject(beatmapObject, RTLevel.ObjectContext.KEYFRAMES);

                            ResizeKeyframeTimeline(beatmapObject);
                            RenderMarkers(beatmapObject);
                        }
                    });

                    increaseTimeGreat.onClick.NewListener(() =>
                    {
                        if (float.TryParse(time.text, out float num))
                        {
                            if (num < 0f)
                                num = 0f;

                            if (EditorConfig.Instance.RoundToNearest.Value)
                                num = RTMath.RoundToNearestDecimal(num, 3);

                            foreach (var kf in selected.Where(x => x.Index != 0))
                                kf.Time = Mathf.Clamp(kf.Time + (num * 10f), 0f, float.MaxValue);

                            RenderKeyframes(beatmapObject);

                            // Keyframe Time affects both physical object and timeline object.
                            EditorTimeline.inst.RenderTimelineObject(EditorTimeline.inst.GetTimelineObject(beatmapObject));
                            if (UpdateObjects)
                                RTLevel.Current?.UpdateObject(beatmapObject, RTLevel.ObjectContext.KEYFRAMES);

                            ResizeKeyframeTimeline(beatmapObject);
                            RenderMarkers(beatmapObject);
                        }
                    });

                    TriggerHelper.AddEventTriggers(time.gameObject, TriggerHelper.ScrollDelta(time));

                    var curvesMulti = multiDialog.Find("curves/curves").GetComponent<Dropdown>();
                    var curvesMultiApplyButton = multiDialog.Find("curves/apply").GetComponent<Button>();
                    curvesMulti.onValueChanged.ClearAll();
                    curvesMultiApplyButton.onClick.NewListener(() =>
                    {
                        var anim = (Easing)curvesMulti.value;
                        foreach (var keyframe in selected)
                        {
                            if (keyframe.Index != 0)
                                keyframe.eventKeyframe.curve = anim;
                        }

                        // Since keyframe curve has no affect on the timeline object, we will only need to update the physical object.
                        if (UpdateObjects)
                            RTLevel.Current?.UpdateObject(beatmapObject, RTLevel.ObjectContext.KEYFRAMES);
                        RenderKeyframes(beatmapObject);
                    });

                    var valueIndex = multiDialog.Find("value base/value index/input").GetComponent<InputField>();
                    if (!int.TryParse(valueIndex.text, out int a))
                        valueIndex.SetTextWithoutNotify("0");
                    valueIndex.onValueChanged.NewListener(_val =>
                    {
                        if (!int.TryParse(_val, out int n))
                            valueIndex.text = "0";
                    });

                    TriggerHelper.IncreaseDecreaseButtonsInt(valueIndex, t: valueIndex.transform.parent);
                    TriggerHelper.AddEventTriggers(valueIndex.gameObject, TriggerHelper.ScrollDeltaInt(valueIndex));

                    var value = multiDialog.Find("value base/value/input").GetComponent<InputField>();
                    value.onValueChanged.NewListener(_val =>
                    {
                        if (!float.TryParse(_val, out float n))
                            value.text = "0";
                    });

                    var setValue = value.transform.parent.GetChild(2).GetComponent<Button>();
                    setValue.onClick.NewListener(() =>
                    {
                        if (float.TryParse(value.text, out float num))
                        {
                            foreach (var kf in selected)
                            {
                                var keyframe = kf.eventKeyframe;

                                var index = Parser.TryParse(valueIndex.text, 0);

                                index = Mathf.Clamp(index, 0, keyframe.values.Length - 1);
                                if (index >= 0 && index < keyframe.values.Length)
                                    keyframe.values[index] = kf.Type == 3 ? Mathf.Clamp((int)num, 0, CoreHelper.CurrentBeatmapTheme.objectColors.Count - 1) : num;
                            }

                            if (UpdateObjects)
                                RTLevel.Current?.UpdateObject(beatmapObject, RTLevel.ObjectContext.KEYFRAMES);
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
            var isFirst = firstKF.Index == 0;

            CoreHelper.Log($"Selected Keyframe:\nID - {firstKF.ID}\nType: {firstKF.Type}\nIndex {firstKF.Index}");

            Dialog.OpenKeyframeDialog(type);

            ObjEditor.inst.currentKeyframeKind = type;
            ObjEditor.inst.currentKeyframe = firstKF.Index;

            var dialog = Dialog.keyframeDialogs[type];
            var kfdialog = dialog.GameObject.transform;

            dialog.EventTimeField.SetInteractible(!isFirst);

            dialog.JumpToStartButton.interactable = !isFirst;
            dialog.JumpToStartButton.onClick.NewListener(() => SetCurrentKeyframe(beatmapObject, 0, true));

            dialog.JumpToPrevButton.interactable = selected.Count() == 1 && firstKF.Index != 0;
            dialog.JumpToPrevButton.onClick.NewListener(() => SetCurrentKeyframe(beatmapObject, firstKF.Index - 1, true));

            dialog.KeyframeIndexer.text = firstKF.Index == 0 ? "S" : firstKF.Index == beatmapObject.events[firstKF.Type].Count - 1 ? "E" : firstKF.Index.ToString();

            dialog.JumpToNextButton.interactable = selected.Count() == 1 && firstKF.Index < beatmapObject.events[type].Count - 1;
            dialog.JumpToNextButton.onClick.NewListener(() => SetCurrentKeyframe(beatmapObject, firstKF.Index + 1, true));

            dialog.JumpToLastButton.interactable = selected.Count() == 1 && firstKF.Index < beatmapObject.events[type].Count - 1;
            dialog.JumpToLastButton.onClick.NewListener(() => SetCurrentKeyframe(beatmapObject, beatmapObject.events[type].Count - 1, true));

            dialog.CopyButton.button.onClick.NewListener(() =>
            {
                CopyData(firstKF.Type, firstKF.eventKeyframe);
                EditorManager.inst.DisplayNotification("Copied keyframe data!", 2f, EditorManager.NotificationType.Success);
            });

            dialog.PasteButton.button.onClick.NewListener(() => PasteKeyframeData(type, selected, beatmapObject));

            dialog.DeleteButton.button.onClick.NewListener(DeleteKeyframes(beatmapObject).Start);

            dialog.EventTimeField.eventTrigger.triggers.Clear();
            if (count == 1 && firstKF.Index != 0 || count > 1)
                dialog.EventTimeField.eventTrigger.triggers.Add(TriggerHelper.ScrollDelta(dialog.EventTimeField.inputField));

            dialog.EventTimeField.inputField.SetTextWithoutNotify(count == 1 ? firstKF.Time.ToString() : "1");
            dialog.EventTimeField.inputField.onValueChanged.NewListener(_val =>
            {
                if (float.TryParse(_val, out float num) && !ObjEditor.inst.timelineKeyframesDrag && selected.Count() == 1)
                {
                    if (num < 0f)
                        num = 0f;

                    if (EditorConfig.Instance.RoundToNearest.Value)
                        num = RTMath.RoundToNearestDecimal(num, 3);

                    firstKF.Time = num;

                    RenderKeyframes(beatmapObject);

                    // Keyframe Time affects both physical object and timeline object.
                    EditorTimeline.inst.RenderTimelineObject(EditorTimeline.inst.GetTimelineObject(beatmapObject));
                    if (UpdateObjects)
                        RTLevel.Current?.UpdateObject(beatmapObject, RTLevel.ObjectContext.KEYFRAMES);

                    ResizeKeyframeTimeline(beatmapObject);
                    RenderMarkers(beatmapObject);
                }
            });

            if (count == 1)
                TriggerHelper.IncreaseDecreaseButtons(dialog.EventTimeField.inputField, t: dialog.EventTimeField.transform);
            else
            {
                dialog.EventTimeField.leftButton.onClick.NewListener(() =>
                {
                    if (float.TryParse(dialog.EventTimeField.inputField.text, out float result))
                    {
                        var num = Input.GetKey(KeyCode.LeftAlt) ? 0.1f / 10f : Input.GetKey(KeyCode.LeftControl) ? 0.1f * 10f : 0.1f;
                        result -= num;

                        if (count == 1)
                        {
                            dialog.EventTimeField.inputField.text = result.ToString();
                            return;
                        }

                        foreach (var keyframe in selected)
                            keyframe.Time = Mathf.Clamp(keyframe.Time - num, 0.001f, float.PositiveInfinity);

                        RenderKeyframes(beatmapObject);

                        // Keyframe Time affects both physical object and timeline object.
                        EditorTimeline.inst.RenderTimelineObject(EditorTimeline.inst.GetTimelineObject(beatmapObject));
                        if (UpdateObjects)
                            RTLevel.Current?.UpdateObject(beatmapObject, RTLevel.ObjectContext.KEYFRAMES);

                        ResizeKeyframeTimeline(beatmapObject);
                        RenderMarkers(beatmapObject);
                    }
                });

                dialog.EventTimeField.rightButton.onClick.NewListener(() =>
                {
                    if (float.TryParse(dialog.EventTimeField.inputField.text, out float result))
                    {
                        var num = Input.GetKey(KeyCode.LeftAlt) ? 0.1f / 10f : Input.GetKey(KeyCode.LeftControl) ? 0.1f * 10f : 0.1f;
                        result += num;

                        if (count == 1)
                        {
                            dialog.EventTimeField.inputField.text = result.ToString();
                            return;
                        }

                        foreach (var keyframe in selected)
                            keyframe.Time = Mathf.Clamp(keyframe.Time + num, 0.001f, float.PositiveInfinity);

                        RenderKeyframes(beatmapObject);

                        // Keyframe Time affects both physical object and timeline object.
                        EditorTimeline.inst.RenderTimelineObject(EditorTimeline.inst.GetTimelineObject(beatmapObject));
                        if (UpdateObjects)
                            RTLevel.Current?.UpdateObject(beatmapObject, RTLevel.ObjectContext.KEYFRAMES);

                        ResizeKeyframeTimeline(beatmapObject);
                        RenderMarkers(beatmapObject);
                    }
                });

                dialog.EventTimeField.leftGreaterButton.onClick.NewListener(() =>
                {
                    if (float.TryParse(dialog.EventTimeField.inputField.text, out float result))
                    {
                        var num = (Input.GetKey(KeyCode.LeftAlt) ? 0.1f / 10f : Input.GetKey(KeyCode.LeftControl) ? 0.1f * 10f : 0.1f) * 10f;
                        result -= num;

                        if (count == 1)
                        {
                            dialog.EventTimeField.inputField.text = result.ToString();
                            return;
                        }

                        foreach (var keyframe in selected)
                            keyframe.Time = Mathf.Clamp(keyframe.Time - num, 0.001f, float.PositiveInfinity);

                        RenderKeyframes(beatmapObject);

                        // Keyframe Time affects both physical object and timeline object.
                        EditorTimeline.inst.RenderTimelineObject(EditorTimeline.inst.GetTimelineObject(beatmapObject));
                        if (UpdateObjects)
                            RTLevel.Current?.UpdateObject(beatmapObject, RTLevel.ObjectContext.KEYFRAMES);

                        ResizeKeyframeTimeline(beatmapObject);
                        RenderMarkers(beatmapObject);
                    }
                });

                dialog.EventTimeField.rightGreaterButton.onClick.NewListener(() =>
                {
                    if (float.TryParse(dialog.EventTimeField.inputField.text, out float result))
                    {
                        var num = (Input.GetKey(KeyCode.LeftAlt) ? 0.1f / 10f : Input.GetKey(KeyCode.LeftControl) ? 0.1f * 10f : 0.1f) * 10f;
                        result += num;

                        if (count == 1)
                        {
                            dialog.EventTimeField.inputField.text = result.ToString();
                            return;
                        }

                        foreach (var keyframe in selected)
                            keyframe.Time = Mathf.Clamp(keyframe.Time + num, 0.001f, float.PositiveInfinity);

                        RenderKeyframes(beatmapObject);

                        // Keyframe Time affects both physical object and timeline object.
                        EditorTimeline.inst.RenderTimelineObject(EditorTimeline.inst.GetTimelineObject(beatmapObject));
                        if (UpdateObjects)
                            RTLevel.Current?.UpdateObject(beatmapObject, RTLevel.ObjectContext.KEYFRAMES);

                        ResizeKeyframeTimeline(beatmapObject);
                        RenderMarkers(beatmapObject);
                    }
                });
            }

            dialog.CurvesLabel.SetActive(count == 1 && firstKF.Index != 0 || count > 1);
            dialog.CurvesDropdown.gameObject.SetActive(count == 1 && firstKF.Index != 0 || count > 1);
            dialog.CurvesDropdown.SetValueWithoutNotify((int)firstKF.eventKeyframe.curve);
            dialog.CurvesDropdown.onValueChanged.NewListener(_val =>
            {
                var anim = (Easing)_val;
                foreach (var keyframe in selected)
                {
                    if (keyframe.Index != 0)
                        keyframe.eventKeyframe.curve = anim;
                }

                // Since keyframe curve has no affect on the timeline object, we will only need to update the physical object.
                if (UpdateObjects)
                    RTLevel.Current?.UpdateObject(beatmapObject, RTLevel.ObjectContext.KEYFRAMES);
                RenderKeyframes(beatmapObject);
            });

            switch (type)
            {
                case 0: {
                        for (int i = 0; i < 3; i++)
                            KeyframeHandler(type, i, selected, firstKF, beatmapObject);

                        KeyframeRandomHandler(type, selected, firstKF, beatmapObject);
                        for (int i = 0; i < 2; i++)
                            KeyframeRandomValueHandler(type, i, selected, firstKF, beatmapObject);

                        break;
                    }
                case 1: {
                        for (int i = 0; i < 2; i++)
                            KeyframeHandler(type, i, selected, firstKF, beatmapObject);

                        KeyframeRandomHandler(type, selected, firstKF, beatmapObject);
                        for (int i = 0; i < 2; i++)
                            KeyframeRandomValueHandler(type, i, selected, firstKF, beatmapObject);

                        break;
                    }
                case 2: {
                        KeyframeHandler(type, 0, selected, firstKF, beatmapObject);

                        KeyframeRandomHandler(type, selected, firstKF, beatmapObject);
                        for (int i = 0; i < 2; i++)
                            KeyframeRandomValueHandler(type, i, selected, firstKF, beatmapObject);

                        break;
                    }
                case 3: {
                        ColorKeyframeHandler(0, ObjEditor.inst.colorButtons, selected, firstKF, beatmapObject);

                        bool showModifiedColors = EditorConfig.Instance.ShowModifiedColors.Value;
                        var eventTime = firstKF.eventKeyframe.time;
                        int index = 0;
                        foreach (var toggle in ObjEditor.inst.colorButtons)
                        {
                            int tmpIndex = index;

                            toggle.gameObject.SetActive(RTEditor.ShowModdedUI || tmpIndex < 9);

                            toggle.SetIsOnWithoutNotify(index == firstKF.eventKeyframe.values[0]);
                            toggle.onValueChanged.NewListener(_val => SetKeyframeColor(beatmapObject, 0, tmpIndex, ObjEditor.inst.colorButtons, selected));

                            if (showModifiedColors)
                            {
                                var color = CoreHelper.CurrentBeatmapTheme.GetObjColor(tmpIndex);

                                float hueNum = beatmapObject.Interpolate(type, 2, eventTime);
                                float satNum = beatmapObject.Interpolate(type, 3, eventTime);
                                float valNum = beatmapObject.Interpolate(type, 4, eventTime);

                                toggle.image.color = RTColors.ChangeColorHSV(color, hueNum, satNum, valNum);
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

                        var random = firstKF.eventKeyframe.random;

                        kfdialog.Find("opacity_label").gameObject.SetActive(RTEditor.NotSimple);
                        kfdialog.Find("opacity").gameObject.SetActive(RTEditor.NotSimple);
                        kfdialog.Find("opacity/collision").gameObject.SetActive(RTEditor.ShowModdedUI);

                        kfdialog.Find("huesatval_label").gameObject.SetActive(RTEditor.ShowModdedUI);
                        kfdialog.Find("huesatval").gameObject.SetActive(RTEditor.ShowModdedUI);

                        var showGradient = RTEditor.NotSimple && beatmapObject.gradientType != GradientType.Normal;

                        kfdialog.Find("color_label").GetChild(0).GetComponent<Text>().text = showGradient ? "Start Color" : "Color";
                        kfdialog.Find("opacity_label").GetChild(0).GetComponent<Text>().text = showGradient ? "Start Opacity" : "Opacity";
                        kfdialog.Find("huesatval_label").GetChild(0).GetComponent<Text>().text = showGradient ? "Start Hue" : "Hue";
                        kfdialog.Find("huesatval_label").GetChild(1).GetComponent<Text>().text = showGradient ? "Start Sat" : "Saturation";
                        kfdialog.Find("huesatval_label").GetChild(2).GetComponent<Text>().text = showGradient ? "Start Val" : "Value";

                        kfdialog.Find("gradient_color_label").gameObject.SetActive(showGradient);
                        kfdialog.Find("gradient_color").gameObject.SetActive(showGradient);
                        kfdialog.Find("gradient_opacity_label").gameObject.SetActive(showGradient && RTEditor.ShowModdedUI);
                        kfdialog.Find("gradient_opacity").gameObject.SetActive(showGradient && RTEditor.ShowModdedUI);
                        kfdialog.Find("gradient_huesatval_label").gameObject.SetActive(showGradient && RTEditor.ShowModdedUI);
                        kfdialog.Find("gradient_huesatval").gameObject.SetActive(showGradient && RTEditor.ShowModdedUI);

                        kfdialog.Find("color").AsRT().sizeDelta = new Vector2(366f, RTEditor.ShowModdedUI ? 78f : 32f);
                        kfdialog.Find("gradient_color").AsRT().sizeDelta = new Vector2(366f, RTEditor.ShowModdedUI ? 78f : 32f);

                        if (!RTEditor.NotSimple)
                            break;

                        var opacity = kfdialog.Find("opacity/x").GetComponent<InputField>();

                        opacity.SetTextWithoutNotify((-firstKF.eventKeyframe.values[1] + 1).ToString());
                        opacity.onValueChanged.NewListener(_val =>
                        {
                            if (float.TryParse(_val, out float num))
                            {
                                var value = Mathf.Clamp(-num + 1, 0f, 1f);
                                foreach (var keyframe in selected.Select(x => x.eventKeyframe))
                                {
                                    keyframe.values[1] = value;
                                    if (!RTEditor.ShowModdedUI)
                                        keyframe.values[6] = 10f;
                                }

                                // Since keyframe value has no affect on the timeline object, we will only need to update the physical object.
                                if (UpdateObjects)
                                    RTLevel.Current?.UpdateObject(beatmapObject, RTLevel.ObjectContext.KEYFRAMES);
                            }
                        });
                        opacity.onEndEdit.NewListener(_val =>
                        {
                            if (RTMath.TryParse(_val, (-firstKF.eventKeyframe.values[1] + 1), out float num))
                            {
                                var value = Mathf.Clamp(-num + 1, 0f, 1f);
                                foreach (var keyframe in selected.Select(x => x.eventKeyframe))
                                {
                                    keyframe.values[1] = value;
                                    if (!RTEditor.ShowModdedUI)
                                        keyframe.values[6] = 10f;
                                }

                                // Since keyframe value has no affect on the timeline object, we will only need to update the physical object.
                                if (UpdateObjects)
                                    RTLevel.Current?.UpdateObject(beatmapObject, RTLevel.ObjectContext.KEYFRAMES);
                            }
                        });

                        TriggerHelper.AddEventTriggers(kfdialog.Find("opacity").gameObject, TriggerHelper.ScrollDelta(opacity, 0.1f, 10f, 0f, 1f));

                        TriggerHelper.IncreaseDecreaseButtons(opacity);

                        ColorKeyframeHandler(5, gradientColorButtons, selected, firstKF, beatmapObject);

                        if (!RTEditor.ShowModdedUI)
                            break;

                        var collision = kfdialog.Find("opacity/collision").GetComponent<Toggle>();
                        collision.SetIsOnWithoutNotify(beatmapObject.opacityCollision);
                        collision.onValueChanged.NewListener(_val =>
                        {
                            beatmapObject.opacityCollision = _val;
                            // Since keyframe value has no affect on the timeline object, we will only need to update the physical object.
                            if (UpdateObjects)
                                RTLevel.Current?.UpdateObject(beatmapObject, RTLevel.ObjectContext.OBJECT_TYPE);
                        });

                        var gradientOpacity = kfdialog.Find("gradient_opacity/x").GetComponent<InputField>();

                        gradientOpacity.SetTextWithoutNotify((-firstKF.eventKeyframe.values[6] + 1).ToString());
                        gradientOpacity.onValueChanged.NewListener(_val =>
                        {
                            if (float.TryParse(_val, out float n))
                            {
                                foreach (var keyframe in selected.Select(x => x.eventKeyframe))
                                    keyframe.values[6] = Mathf.Clamp(-n + 1, 0f, 1f);

                                // Since keyframe value has no affect on the timeline object, we will only need to update the physical object.
                                if (UpdateObjects)
                                    RTLevel.Current?.UpdateObject(beatmapObject, RTLevel.ObjectContext.KEYFRAMES);
                            }
                        });
                        gradientOpacity.onEndEdit.NewListener(_val =>
                        {
                            if (RTMath.TryParse(_val, (-firstKF.eventKeyframe.values[6] + 1), out float n))
                            {
                                foreach (var keyframe in selected.Select(x => x.eventKeyframe))
                                    keyframe.values[6] = Mathf.Clamp(-n + 1, 0f, 1f);

                                // Since keyframe value has no affect on the timeline object, we will only need to update the physical object.
                                if (UpdateObjects)
                                    RTLevel.Current?.UpdateObject(beatmapObject, RTLevel.ObjectContext.KEYFRAMES);
                            }
                        });

                        TriggerHelper.AddEventTriggers(kfdialog.Find("gradient_opacity").gameObject, TriggerHelper.ScrollDelta(gradientOpacity, 0.1f, 10f, 0f, 1f));

                        TriggerHelper.IncreaseDecreaseButtons(gradientOpacity);

                        // Start
                        {
                            var hue = kfdialog.Find("huesatval/x").GetComponent<InputField>();

                            hue.SetTextWithoutNotify(firstKF.eventKeyframe.values[2].ToString());
                            hue.onValueChanged.NewListener(_val =>
                            {
                                if (float.TryParse(_val, out float n))
                                {
                                    foreach (var keyframe in selected.Select(x => x.eventKeyframe))
                                        keyframe.values[2] = n;

                                    // Since keyframe value has no affect on the timeline object, we will only need to update the physical object.
                                    if (UpdateObjects)
                                        RTLevel.Current?.UpdateObject(beatmapObject, RTLevel.ObjectContext.KEYFRAMES);
                                }
                                ColorKeyframeHandler(0, ObjEditor.inst.colorButtons, selected, firstKF, beatmapObject);
                            });
                            hue.onEndEdit.NewListener(_val =>
                            {
                                if (RTMath.TryParse(_val, firstKF.eventKeyframe.values[2], out float n))
                                {
                                    foreach (var keyframe in selected.Select(x => x.eventKeyframe))
                                        keyframe.values[2] = n;

                                    // Since keyframe value has no affect on the timeline object, we will only need to update the physical object.
                                    if (UpdateObjects)
                                        RTLevel.Current?.UpdateObject(beatmapObject, RTLevel.ObjectContext.KEYFRAMES);
                                }
                                ColorKeyframeHandler(0, ObjEditor.inst.colorButtons, selected, firstKF, beatmapObject);
                            });

                            Destroy(kfdialog.transform.Find("huesatval").GetComponent<EventTrigger>());

                            TriggerHelper.AddEventTriggers(hue.gameObject, TriggerHelper.ScrollDelta(hue));
                            TriggerHelper.IncreaseDecreaseButtons(hue);

                            var sat = kfdialog.Find("huesatval/y").GetComponent<InputField>();

                            sat.SetTextWithoutNotify(firstKF.eventKeyframe.values[3].ToString());
                            sat.onValueChanged.NewListener(_val =>
                            {
                                if (float.TryParse(_val, out float n))
                                {
                                    foreach (var keyframe in selected.Select(x => x.eventKeyframe))
                                        keyframe.values[3] = n;

                                    // Since keyframe value has no affect on the timeline object, we will only need to update the physical object.
                                    if (UpdateObjects)
                                        RTLevel.Current?.UpdateObject(beatmapObject, RTLevel.ObjectContext.KEYFRAMES);
                                }
                                ColorKeyframeHandler(0, ObjEditor.inst.colorButtons, selected, firstKF, beatmapObject);
                            });
                            sat.onEndEdit.NewListener(_val =>
                            {
                                if (RTMath.TryParse(_val, firstKF.eventKeyframe.values[3], out float n))
                                {
                                    foreach (var keyframe in selected.Select(x => x.eventKeyframe))
                                        keyframe.values[3] = n;

                                    // Since keyframe value has no affect on the timeline object, we will only need to update the physical object.
                                    if (UpdateObjects)
                                        RTLevel.Current?.UpdateObject(beatmapObject, RTLevel.ObjectContext.KEYFRAMES);
                                }
                                ColorKeyframeHandler(0, ObjEditor.inst.colorButtons, selected, firstKF, beatmapObject);
                            });

                            TriggerHelper.AddEventTriggers(sat.gameObject, TriggerHelper.ScrollDelta(sat));
                            TriggerHelper.IncreaseDecreaseButtons(sat);

                            var val = kfdialog.Find("huesatval/z").GetComponent<InputField>();

                            val.SetTextWithoutNotify(firstKF.eventKeyframe.values[4].ToString());
                            val.onValueChanged.NewListener(_val =>
                            {
                                if (float.TryParse(_val, out float n))
                                {
                                    foreach (var keyframe in selected.Select(x => x.eventKeyframe))
                                        keyframe.values[4] = n;

                                    // Since keyframe value has no affect on the timeline object, we will only need to update the physical object.
                                    if (UpdateObjects)
                                        RTLevel.Current?.UpdateObject(beatmapObject, RTLevel.ObjectContext.KEYFRAMES);
                                }
                                ColorKeyframeHandler(0, ObjEditor.inst.colorButtons, selected, firstKF, beatmapObject);
                            });
                            val.onEndEdit.NewListener(_val =>
                            {
                                if (RTMath.TryParse(_val, firstKF.eventKeyframe.values[4], out float n))
                                {
                                    foreach (var keyframe in selected.Select(x => x.eventKeyframe))
                                        keyframe.values[4] = n;

                                    // Since keyframe value has no affect on the timeline object, we will only need to update the physical object.
                                    if (UpdateObjects)
                                        RTLevel.Current?.UpdateObject(beatmapObject, RTLevel.ObjectContext.KEYFRAMES);
                                }
                                ColorKeyframeHandler(0, ObjEditor.inst.colorButtons, selected, firstKF, beatmapObject);
                            });

                            TriggerHelper.AddEventTriggers(val.gameObject, TriggerHelper.ScrollDelta(val));
                            TriggerHelper.IncreaseDecreaseButtons(val);
                        }
                        
                        // End
                        {
                            var hue = kfdialog.Find("gradient_huesatval/x").GetComponent<InputField>();

                            hue.SetTextWithoutNotify(firstKF.eventKeyframe.values[7].ToString());
                            hue.onValueChanged.NewListener(_val =>
                            {
                                if (float.TryParse(_val, out float n))
                                {
                                    foreach (var keyframe in selected.Select(x => x.eventKeyframe))
                                        keyframe.values[7] = n;

                                    // Since keyframe value has no affect on the timeline object, we will only need to update the physical object.
                                    if (UpdateObjects)
                                        RTLevel.Current?.UpdateObject(beatmapObject, RTLevel.ObjectContext.KEYFRAMES);
                                }
                                ColorKeyframeHandler(5, gradientColorButtons, selected, firstKF, beatmapObject);
                            });
                            hue.onEndEdit.NewListener(_val =>
                            {
                                if (RTMath.TryParse(_val, firstKF.eventKeyframe.values[7], out float n))
                                {
                                    foreach (var keyframe in selected.Select(x => x.eventKeyframe))
                                        keyframe.values[7] = n;

                                    // Since keyframe value has no affect on the timeline object, we will only need to update the physical object.
                                    if (UpdateObjects)
                                        RTLevel.Current?.UpdateObject(beatmapObject, RTLevel.ObjectContext.KEYFRAMES);
                                }
                                ColorKeyframeHandler(5, gradientColorButtons, selected, firstKF, beatmapObject);
                            });

                            Destroy(kfdialog.transform.Find("gradient_huesatval").GetComponent<EventTrigger>());

                            TriggerHelper.AddEventTriggers(hue.gameObject, TriggerHelper.ScrollDelta(hue));
                            TriggerHelper.IncreaseDecreaseButtons(hue);

                            var sat = kfdialog.Find("gradient_huesatval/y").GetComponent<InputField>();

                            sat.SetTextWithoutNotify(firstKF.eventKeyframe.values[8].ToString());
                            sat.onValueChanged.NewListener(_val =>
                            {
                                if (float.TryParse(_val, out float n))
                                {
                                    foreach (var keyframe in selected.Select(x => x.eventKeyframe))
                                        keyframe.values[8] = n;

                                    // Since keyframe value has no affect on the timeline object, we will only need to update the physical object.
                                    if (UpdateObjects)
                                        RTLevel.Current?.UpdateObject(beatmapObject, RTLevel.ObjectContext.KEYFRAMES);
                                }
                                ColorKeyframeHandler(5, gradientColorButtons, selected, firstKF, beatmapObject);
                            });
                            sat.onEndEdit.NewListener(_val =>
                            {
                                if (RTMath.TryParse(_val, firstKF.eventKeyframe.values[8], out float n))
                                {
                                    foreach (var keyframe in selected.Select(x => x.eventKeyframe))
                                        keyframe.values[8] = n;

                                    // Since keyframe value has no affect on the timeline object, we will only need to update the physical object.
                                    if (UpdateObjects)
                                        RTLevel.Current?.UpdateObject(beatmapObject, RTLevel.ObjectContext.KEYFRAMES);
                                }
                                ColorKeyframeHandler(5, gradientColorButtons, selected, firstKF, beatmapObject);
                            });

                            TriggerHelper.AddEventTriggers(sat.gameObject, TriggerHelper.ScrollDelta(sat));
                            TriggerHelper.IncreaseDecreaseButtons(sat);

                            var val = kfdialog.Find("gradient_huesatval/z").GetComponent<InputField>();

                            val.SetTextWithoutNotify(firstKF.eventKeyframe.values[9].ToString());
                            val.onValueChanged.NewListener(_val =>
                            {
                                if (float.TryParse(_val, out float n))
                                {
                                    foreach (var keyframe in selected.Select(x => x.eventKeyframe))
                                        keyframe.values[9] = n;

                                    // Since keyframe value has no affect on the timeline object, we will only need to update the physical object.
                                    if (UpdateObjects)
                                        RTLevel.Current?.UpdateObject(beatmapObject, RTLevel.ObjectContext.KEYFRAMES);
                                }
                                ColorKeyframeHandler(5, gradientColorButtons, selected, firstKF, beatmapObject);
                            });
                            val.onEndEdit.NewListener(_val =>
                            {
                                if (RTMath.TryParse(_val, firstKF.eventKeyframe.values[9], out float n))
                                {
                                    foreach (var keyframe in selected.Select(x => x.eventKeyframe))
                                        keyframe.values[9] = n;

                                    // Since keyframe value has no affect on the timeline object, we will only need to update the physical object.
                                    if (UpdateObjects)
                                        RTLevel.Current?.UpdateObject(beatmapObject, RTLevel.ObjectContext.KEYFRAMES);
                                }
                                ColorKeyframeHandler(5, gradientColorButtons, selected, firstKF, beatmapObject);
                            });

                            TriggerHelper.AddEventTriggers(val.gameObject, TriggerHelper.ScrollDelta(val));
                            TriggerHelper.IncreaseDecreaseButtons(val);
                        }

                        break;
                    }
            }

            if (!dialog.RelativeToggle)
                return;

            RTEditor.SetActive(dialog.RelativeToggle.gameObject, RTEditor.ShowModdedUI);
            if (RTEditor.ShowModdedUI)
            {
                dialog.RelativeToggle.toggle.SetIsOnWithoutNotify(firstKF.eventKeyframe.relative);
                dialog.RelativeToggle.toggle.onValueChanged.NewListener(_val =>
                {
                    foreach (var keyframe in selected.Select(x => x.eventKeyframe))
                        keyframe.relative = _val;

                    // Since keyframe value has no affect on the timeline object, we will only need to update the physical object.
                    if (UpdateObjects)
                        RTLevel.Current?.UpdateObject(beatmapObject, RTLevel.ObjectContext.KEYFRAMES);
                });
            }
        }

        List<TimelineMarker> timelineMarkers = new List<TimelineMarker>();
        bool renderedMarkers;

        /// <summary>
        /// Renders the Markers in the object timeline.
        /// </summary>
        /// <param name="beatmapObject">The BeatmapObject to set.</param>
        public void RenderMarkers(BeatmapObject beatmapObject)
        {
            if (renderedMarkers)
            {
                LSHelpers.DeleteChildren(Dialog.Markers);
                timelineMarkers.Clear();
                renderedMarkers = false;
            }

            if (!EditorConfig.Instance.ShowMarkersInObjectEditor.Value)
                return;

            var length = beatmapObject.GetObjectLifeLength(ObjEditor.inst.ObjectLengthOffset);
            for (int i = 0; i < GameData.Current.data.markers.Count; i++)
            {
                var marker = GameData.Current.data.markers[i];
                if (marker.time < beatmapObject.StartTime || marker.time > beatmapObject.StartTime + length)
                    continue;

                var timelineMarker = new TimelineMarker();
                timelineMarker.Marker = marker;
                int index = i;

                var gameObject = MarkerEditor.inst.markerPrefab.Duplicate(Dialog.Markers, $"Marker {index}");

                timelineMarker.Index = index;
                timelineMarker.GameObject = gameObject;
                timelineMarker.RectTransform = gameObject.transform.AsRT();
                timelineMarker.Handle = gameObject.GetComponent<Image>();
                timelineMarker.Line = gameObject.transform.Find("line").GetComponent<Image>();
                timelineMarker.Text = gameObject.GetComponentInChildren<Text>();
                timelineMarker.HoverTooltip = gameObject.GetComponent<HoverTooltip>();

                var markerColor = timelineMarker.Color;

                timelineMarker.GameObject.SetActive(true);
                timelineMarker.RenderPosition(marker.time - beatmapObject.StartTime, ObjEditor.inst.Zoom * 14f, 0f);
                timelineMarker.RenderTooltip(markerColor);
                timelineMarker.RenderName();
                timelineMarker.RenderTextWidth(EditorConfig.Instance.ObjectMarkerTextWidth.Value);
                timelineMarker.RenderColor(markerColor, EditorConfig.Instance.ObjectMarkerLineColor.Value);
                timelineMarker.RenderLine(EditorConfig.Instance.ObjectMarkerLineDotted.Value);
                timelineMarker.RenderLineWidth(EditorConfig.Instance.ObjectMarkerLineWidth.Value);

                EditorThemeManager.ApplyLightText(timelineMarker.Text);

                TriggerHelper.AddEventTriggers(gameObject, TriggerHelper.CreateEntry(EventTriggerType.PointerClick, eventData =>
                {
                    var pointerEventData = (PointerEventData)eventData;

                    if (!marker.timelineMarker)
                        return;

                    switch (pointerEventData.button)
                    {
                        case PointerEventData.InputButton.Left: {
                                AudioManager.inst.SetMusicTimeWithDelay(Mathf.Clamp(marker.time, 0f, AudioManager.inst.CurrentAudioSource.clip.length), 0.05f);
                                break;
                            }
                        case PointerEventData.InputButton.Right: {
                                RTMarkerEditor.inst.ShowMarkerContextMenu(marker.timelineMarker);
                                break;
                            }
                        case PointerEventData.InputButton.Middle: {
                                if (EditorConfig.Instance.MarkerDragButton.Value == PointerEventData.InputButton.Middle)
                                    return;

                                AudioManager.inst.SetMusicTime(Mathf.Clamp(marker.time, 0f, AudioManager.inst.CurrentAudioSource.clip.length));
                                break;
                            }
                    }
                }));

                timelineMarkers.Add(timelineMarker);
                renderedMarkers = true;
            }
        }

        /// <summary>
        /// Renders the Markers in the object timeline.
        /// </summary>
        /// <param name="beatmapObject">The BeatmapObject to set.</param>
        public void RenderMarkerPositions(BeatmapObject beatmapObject)
        {
            if (!renderedMarkers)
                return;

            for (int i = 0; i < timelineMarkers.Count; i++)
                timelineMarkers[i].RenderPosition(timelineMarkers[i].Marker.time - beatmapObject.StartTime, ObjEditor.inst.Zoom * 14f, 0f);
        }

        public void OpenImageSelector(BeatmapObject beatmapObject, bool copyFile = true, bool storeImage = false)
        {
            var editorPath = RTFile.RemoveEndSlash(EditorLevelManager.inst.CurrentLevel.path);
            string jpgFile = FileBrowser.OpenSingleFile("Select an image!", editorPath, new string[] { "png", "jpg" });
            SelectImage(jpgFile, beatmapObject, copyFile: copyFile, storeImage: storeImage);
        }

        public void StoreImage(BeatmapObject beatmapObject, string file)
        {
            if (RTFile.FileExists(file))
            {
                var imageData = File.ReadAllBytes(file);

                var texture2d = new Texture2D(2, 2, TextureFormat.ARGB32, false);
                texture2d.LoadImage(imageData);

                texture2d.wrapMode = TextureWrapMode.Clamp;
                texture2d.filterMode = FilterMode.Point;
                texture2d.Apply();

                GameData.Current.assets.AddSprite(beatmapObject.text, SpriteHelper.CreateSprite(texture2d));
            }
            else
            {
                var imageData = LegacyPlugin.PALogoSprite.texture.EncodeToPNG();

                var texture2d = new Texture2D(2, 2, TextureFormat.ARGB32, false);
                texture2d.LoadImage(imageData);

                texture2d.wrapMode = TextureWrapMode.Clamp;
                texture2d.filterMode = FilterMode.Point;
                texture2d.Apply();

                GameData.Current.assets.AddSprite(beatmapObject.text, SpriteHelper.CreateSprite(texture2d));
            }
        }

        void SelectImage(string file, BeatmapObject beatmapObject, bool renderEditor = true, bool updateObject = true, bool copyFile = true, bool storeImage = false)
        {
            var editorPath = RTFile.RemoveEndSlash(EditorLevelManager.inst.CurrentLevel.path);
            RTFile.CreateDirectory(RTFile.CombinePaths(editorPath, "images"));

            file = RTFile.ReplaceSlash(file);
            CoreHelper.Log($"Selected file: {file}");
            if (!RTFile.FileExists(file))
                return;
            
            string jpgFileLocation = RTFile.CombinePaths(editorPath, "images", Path.GetFileName(file));

            if (copyFile && (EditorConfig.Instance.OverwriteImportedImages.Value || !RTFile.FileExists(jpgFileLocation)) && !file.Contains(editorPath))
                RTFile.CopyFile(file, jpgFileLocation);

            beatmapObject.text = jpgFileLocation.Remove(editorPath + "/");

            if (storeImage)
                StoreImage(beatmapObject, file);

            // Since setting image has no affect on the timeline object, we will only need to update the physical object.
            if (updateObject && UpdateObjects)
                RTLevel.Current?.UpdateObject(beatmapObject, RTLevel.ObjectContext.IMAGE);

            if (renderEditor)
                RenderShape(beatmapObject);
        }

        #endregion

        #region Object Search

        public string objectSearchTerm = string.Empty;

        /// <summary>
        /// Shows a list of <see cref="BeatmapObject"/>s in the level.
        /// </summary>
        /// <param name="onSelect">Function to run when a button is clicked.</param>
        /// <param name="clearParent">If the Clear Parents button should render.</param>
        /// <param name="beatmapObjects">List of <see cref="BeatmapObject"/> to render.</param>
        public void ShowObjectSearch(Action<BeatmapObject> onSelect, bool clearParent = false, List<BeatmapObject> beatmapObjects = null)
        {
            RTEditor.inst.ObjectSearchPopup.Open();
            RefreshObjectSearch(onSelect, clearParent, beatmapObjects);
        }

        /// <summary>
        /// Refreshes the list of <see cref="BeatmapObject"/>s in the level.
        /// </summary>
        /// <param name="onSelect">Function to run when a button is clicked.</param>
        /// <param name="clearParent">If the Clear Parents button should render.</param>
        /// <param name="beatmapObjects">List of <see cref="BeatmapObject"/> to render.</param>
        public void RefreshObjectSearch(Action<BeatmapObject> onSelect, bool clearParent = false, List<BeatmapObject> beatmapObjects = null)
        {
            RTEditor.inst.ObjectSearchPopup.SearchField.onValueChanged.ClearAll();
            RTEditor.inst.ObjectSearchPopup.SearchField.onValueChanged.AddListener(_val =>
            {
                objectSearchTerm = _val;
                RefreshObjectSearch(onSelect, clearParent, beatmapObjects);
            });

            RTEditor.inst.ObjectSearchPopup.ClearContent();

            if (clearParent)
            {
                var buttonPrefab = EditorManager.inst.spriteFolderButtonPrefab.Duplicate(RTEditor.inst.ObjectSearchPopup.Content, "Clear Parents");
                var buttonText = buttonPrefab.transform.GetChild(0).GetComponent<Text>();
                buttonText.text = "Clear Parents";

                var button = buttonPrefab.GetComponent<Button>();
                button.onClick.NewListener(() =>
                {
                    foreach (var bm in EditorTimeline.inst.SelectedObjects.Where(x => x.isBeatmapObject).Select(x => x.GetData<BeatmapObject>()))
                    {
                        bm.Parent = "";
                        RTLevel.Current?.UpdateObject(bm, RTLevel.ObjectContext.PARENT_CHAIN);
                    }
                });

                var image = buttonPrefab.transform.Find("Image").GetComponent<Image>();
                image.color = Color.red;
                image.sprite = EditorSprites.CloseSprite;

                EditorThemeManager.ApplySelectable(button, ThemeGroup.List_Button_1);
                EditorThemeManager.ApplyLightText(buttonText);
            }

            if (beatmapObjects == null)
                beatmapObjects = GameData.Current.beatmapObjects;

            var list = beatmapObjects.FindAll(x => !x.fromPrefab);
            int num = 0;
            foreach (var beatmapObject in list)
            {
                var regex = new Regex(@"\[([0-9])\]");
                var match = regex.Match(objectSearchTerm);

                if (RTString.SearchString(objectSearchTerm, beatmapObject.name) ||
                    match.Success && int.TryParse(match.Groups[1].ToString(), out int index) && index < beatmapObjects.Count && num == index ||
                    beatmapObject.id == objectSearchTerm)
                {
                    string nm = $"[{(list.IndexOf(beatmapObject) + 1).ToString("0000")}/{list.Count.ToString("0000")} - {beatmapObject.id}] : {beatmapObject.name}";
                    var buttonPrefab = EditorManager.inst.spriteFolderButtonPrefab.Duplicate(RTEditor.inst.ObjectSearchPopup.Content, nm);
                    var buttonText = buttonPrefab.transform.GetChild(0).GetComponent<Text>();
                    buttonText.text = nm;

                    var button = buttonPrefab.GetComponent<Button>();
                    button.onClick.NewListener(() => onSelect?.Invoke(beatmapObject));

                    var image = buttonPrefab.transform.Find("Image").GetComponent<Image>();
                    image.color = RTEditor.GetObjectColor(beatmapObject, false);

                    var shape = Mathf.Clamp(beatmapObject.shape, 0, ShapeManager.inst.Shapes2D.Count - 1);
                    var shapeOption = Mathf.Clamp(beatmapObject.shapeOption, 0, ShapeManager.inst.Shapes2D[shape].Count - 1);

                    image.sprite = ShapeManager.inst.Shapes2D[shape][shapeOption].icon;

                    EditorThemeManager.ApplySelectable(button, ThemeGroup.List_Button_1);
                    EditorThemeManager.ApplyLightText(buttonText);

                    #region Info

                    var levelObject = beatmapObject.runtimeObject;

                    if (!levelObject || !levelObject.visualObject || !levelObject.visualObject.gameObject)
                    {
                        num++;
                        continue;
                    }

                    var transform = levelObject.visualObject.gameObject.transform;

                    string parent = "";
                    if (!string.IsNullOrEmpty(beatmapObject.Parent))
                        parent = "<br>P: " + beatmapObject.Parent + " (" + beatmapObject.parentType + ")";
                    else
                        parent = "<br>P: No Parent" + " (" + beatmapObject.parentType + ")";

                    string text = "";
                    if (beatmapObject.shape != 4 || beatmapObject.shape != 6)
                        text = "<br>S: " + CoreHelper.GetShape(beatmapObject.shape, beatmapObject.shapeOption) +
                            "<br>T: " + beatmapObject.text;
                    if (beatmapObject.shape == 4)
                        text = "<br>S: Text" +
                            "<br>T: " + beatmapObject.text;
                    if (beatmapObject.shape == 6)
                        text = "<br>S: Image" +
                            "<br>T: " + beatmapObject.text;

                    string ptr = "";
                    if (!string.IsNullOrEmpty(beatmapObject.prefabID) && !string.IsNullOrEmpty(beatmapObject.prefabInstanceID))
                        ptr = "<br><#" + RTColors.ColorToHex(beatmapObject.GetPrefab().GetPrefabType().color) + ">PID: " + beatmapObject.prefabID + " | PIID: " + beatmapObject.prefabInstanceID + "</color>";
                    else
                        ptr = "<br>Not from prefab";

                    var desc = "N/ST: " + beatmapObject.name + " [ " + beatmapObject.StartTime + " ]";
                    var hint = "ID: {" + beatmapObject.id + "}" +
                        parent +
                        "<br>Alive: " + beatmapObject.Alive.ToString() +
                        "<br>Origin: {X: " + beatmapObject.origin.x + ", Y: " + beatmapObject.origin.y + "}" +
                        text +
                        "<br>Depth: " + beatmapObject.Depth +
                        "<br>ED: {L: " + beatmapObject.editorData.Layer + ", B: " + beatmapObject.editorData.Bin + "}" +
                        "<br>POS: {X: " + transform.position.x + ", Y: " + transform.position.y + "}" +
                        "<br>SCA: {X: " + transform.localScale.x + ", Y: " + transform.localScale.y + "}" +
                        "<br>ROT: " + transform.eulerAngles.z +
                        "<br>COL: " + "<#" + RTColors.ColorToHex(RTEditor.GetObjectColor(beatmapObject, false)) + ">" + "█ <b>#" + RTColors.ColorToHex(RTEditor.GetObjectColor(beatmapObject, true)) + "</b></color>" +
                        ptr;

                    TooltipHelper.AddHoverTooltip(buttonPrefab, desc, hint);

                    #endregion
                }

                num++;
            }
        }

        /// <summary>
        /// Shows the parent search.
        /// </summary>
        public void ShowParentSearch() => ShowParentSearch(EditorTimeline.inst.CurrentSelection);

        /// <summary>
        /// Shows the parent search.
        /// </summary>
        /// <param name="timelineObject">The object to parent.</param>
        public void ShowParentSearch(TimelineObject timelineObject)
        {
            RTEditor.inst.ParentSelectorPopup.Open();
            RefreshParentSearch(timelineObject);
        }

        /// <summary>
        /// Refrehes the parent search.
        /// </summary>
        /// <param name="timelineObject">The object to parent.</param>
        public void RefreshParentSearch(TimelineObject timelineObject)
        {
            RTEditor.inst.ParentSelectorPopup.ClearContent();

            var noParent = EditorManager.inst.folderButtonPrefab.Duplicate(RTEditor.inst.ParentSelectorPopup.Content, "No Parent");
            noParent.transform.localScale = Vector3.one;
            var noParentText = noParent.transform.GetChild(0).GetComponent<Text>();
            noParentText.text = "No Parent";
            var noParentButton = noParent.GetComponent<Button>();
            noParentButton.onClick.NewListener(() =>
            {
                var list = EditorTimeline.inst.SelectedObjects;
                foreach (var timelineObject in list)
                {
                    if (timelineObject.isPrefabObject)
                    {
                        var prefabObject = timelineObject.GetData<PrefabObject>();
                        prefabObject.parent = "";
                        RTLevel.Current?.UpdatePrefab(prefabObject, RTLevel.PrefabContext.PARENT, false);
                        RTPrefabEditor.inst.RenderPrefabObjectDialog(prefabObject);
                    }
                    if (timelineObject.isBeatmapObject)
                    {
                        var beatmapObject = timelineObject.GetData<BeatmapObject>();
                        beatmapObject.Parent = "";
                        RTLevel.Current?.UpdateObject(beatmapObject, RTLevel.ObjectContext.PARENT_CHAIN);
                    }
                }

                RTLevel.Current?.RecalculateObjectStates();
                RTEditor.inst.ParentSelectorPopup.Close();
                if (list.Count == 1 && timelineObject.isBeatmapObject)
                    RenderDialog(timelineObject.GetData<BeatmapObject>());
                if (list.Count == 1 && timelineObject.isPrefabObject)
                    RTPrefabEditor.inst.RenderPrefabObjectDialog(timelineObject.GetData<PrefabObject>());
            });

            EditorThemeManager.ApplySelectable(noParentButton, ThemeGroup.List_Button_1);
            EditorThemeManager.ApplyLightText(noParentText);

            if (RTString.SearchString(EditorManager.inst.parentSearch, "camera"))
            {
                var cam = EditorManager.inst.folderButtonPrefab.Duplicate(RTEditor.inst.ParentSelectorPopup.Content, "Camera");
                var camText = cam.transform.GetChild(0).GetComponent<Text>();
                var camButton = cam.GetComponent<Button>();

                camText.text = "Camera";
                camButton.onClick.NewListener(() =>
                {
                    var list = EditorTimeline.inst.SelectedObjects;
                    foreach (var timelineObject in list)
                    {
                        if (timelineObject.isPrefabObject)
                        {
                            var prefabObject = timelineObject.GetData<PrefabObject>();
                            prefabObject.parent = BeatmapObject.CAMERA_PARENT;
                            RTLevel.Current?.UpdatePrefab(prefabObject, RTLevel.PrefabContext.PARENT, false);
                        }
                        if (timelineObject.isBeatmapObject)
                        {
                            var bm = timelineObject.GetData<BeatmapObject>();
                            bm.Parent = BeatmapObject.CAMERA_PARENT;
                            RTLevel.Current?.UpdateObject(bm, RTLevel.ObjectContext.PARENT_CHAIN);
                        }
                    }

                    RTLevel.Current?.RecalculateObjectStates();
                    RTEditor.inst.ParentSelectorPopup.Close();
                    if (list.Count == 1 && timelineObject.isBeatmapObject)
                        RenderDialog(timelineObject.GetData<BeatmapObject>());
                    if (list.Count == 1 && timelineObject.isPrefabObject)
                        RTPrefabEditor.inst.RenderPrefabObjectDialog(timelineObject.GetData<PrefabObject>());
                });

                EditorThemeManager.ApplySelectable(camButton, ThemeGroup.List_Button_1);
                EditorThemeManager.ApplyLightText(camText);
            }

            foreach (var obj in GameData.Current.beatmapObjects)
            {
                if (obj.fromPrefab)
                    continue;

                int index = GameData.Current.beatmapObjects.IndexOf(obj);

                if (!RTString.SearchString(EditorManager.inst.parentSearch, obj.name + " " + index.ToString("0000")) || obj.id == timelineObject.ID ||
                    !timelineObject.isPrefabObject && !timelineObject.GetData<BeatmapObject>().CanParent(obj))
                    continue;

                string s = $"{obj.name} {index.ToString("0000")}";
                var objectToParent = EditorManager.inst.folderButtonPrefab.Duplicate(RTEditor.inst.ParentSelectorPopup.Content, s);
                var objectToParentText = objectToParent.transform.GetChild(0).GetComponent<Text>();
                var objectToParentButton = objectToParent.GetComponent<Button>();

                objectToParentText.text = s;
                objectToParentButton.onClick.ClearAll();
                objectToParentButton.onClick.AddListener(() =>
                {
                    string id = obj.id;

                    var list = EditorTimeline.inst.SelectedObjects;
                    foreach (var timelineObject in list)
                    {
                        if (timelineObject.isPrefabObject)
                        {
                            var prefabObject = timelineObject.GetData<PrefabObject>();
                            prefabObject.parent = id;
                            RTLevel.Current?.UpdatePrefab(prefabObject, RTLevel.PrefabContext.PARENT, false);
                        }
                        if (timelineObject.isBeatmapObject)
                            timelineObject.GetData<BeatmapObject>().SetParent(obj);
                    }

                    RTLevel.Current?.RecalculateObjectStates();

                    RTEditor.inst.ParentSelectorPopup.Close();

                    if (list.Count == 1 && timelineObject.isPrefabObject)
                        RTPrefabEditor.inst.RenderPrefabObjectParent(timelineObject.GetData<PrefabObject>());

                    Debug.Log($"{EditorManager.inst.className}Set Parent ID: {id}");
                });

                EditorThemeManager.ApplySelectable(objectToParentButton, ThemeGroup.List_Button_1);
                EditorThemeManager.ApplyLightText(objectToParentText);
            }
        }

        #endregion

        #region Keyframe Handlers

        public GameObject keyframeEnd;

        public static bool AllowTimeExactlyAtStart => false;
        public void ResizeKeyframeTimeline(BeatmapObject beatmapObject)
        {
            // ObjEditor.inst.ObjectLengthOffset is the offset from the last keyframe. Could allow for more timeline space.
            float objectLifeLength = beatmapObject.GetObjectLifeLength();
            float x = TimeTimelineCalc(objectLifeLength + ObjEditor.inst.ObjectLengthOffset);

            ObjEditor.inst.objTimelineContent.AsRT().sizeDelta = new Vector2(x, 0f);
            ObjEditor.inst.objTimelineGrid.AsRT().sizeDelta = new Vector2(x, 122f);

            // Whether the value should clamp at 0.001 over StartTime or not.
            ObjEditor.inst.objTimelineSlider.minValue = AllowTimeExactlyAtStart ? beatmapObject.StartTime : beatmapObject.StartTime + 0.001f;
            ObjEditor.inst.objTimelineSlider.maxValue = beatmapObject.StartTime + objectLifeLength + ObjEditor.inst.ObjectLengthOffset;

            if (!keyframeEnd)
            {
                ObjEditor.inst.objTimelineGrid.DeleteChildren();
                keyframeEnd = ObjEditor.inst.KeyframeEndPrefab.Duplicate(ObjEditor.inst.objTimelineGrid, "end keyframe");
            }

            var rectTransform = keyframeEnd.transform.AsRT();
            rectTransform.sizeDelta = new Vector2(4f, 122f);
            rectTransform.anchoredPosition = new Vector2(objectLifeLength * ObjEditor.inst.Zoom * 14f, 0f);
        }

        public void ClearKeyframes(BeatmapObject beatmapObject)
        {
            var timelineObject = EditorTimeline.inst.GetTimelineObject(beatmapObject);

            foreach (var kf in timelineObject.InternalTimelineObjects)
                Destroy(kf.GameObject);
        }

        public TimelineKeyframe GetKeyframe(BeatmapObject beatmapObject, int type, int index)
        {
            var bmTimelineObject = EditorTimeline.inst.GetTimelineObject(beatmapObject);

            var kf = bmTimelineObject.InternalTimelineObjects.Find(x => x.Type == type && x.Index == index);

            if (!kf)
                kf = bmTimelineObject.InternalTimelineObjects.Find(x => x.ID == beatmapObject.events[type][index].id);

            if (!kf)
            {
                kf = CreateKeyframe(beatmapObject, type, index);
                bmTimelineObject.InternalTimelineObjects.Add(kf);
            }

            if (!kf.GameObject)
                kf.Init(true);

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
                    var kf = beatmapObject.timelineObject.InternalTimelineObjects.Find(x => x.ID == keyframe.id);
                    if (!kf)
                    {
                        kf = CreateKeyframe(beatmapObject, i, j);
                        beatmapObject.timelineObject.InternalTimelineObjects.Add(kf);
                    }

                    if (!kf.GameObject)
                        kf.Init();

                    kf.Render();
                }
            }
        }

        public TimelineKeyframe CreateKeyframe(BeatmapObject beatmapObject, int type, int index)
        {
            var eventKeyframe = beatmapObject.events[type][index];

            var kf = new TimelineKeyframe(eventKeyframe, beatmapObject)
            {
                Type = type,
                Index = index,
            };

            eventKeyframe.timelineKeyframe = kf;
            kf.Init();

            return kf;
        }

        public void RenderKeyframes(BeatmapObject beatmapObject)
        {
            for (int i = 0; i < beatmapObject.events.Count; i++)
            {
                for (int j = 0; j < beatmapObject.events[i].Count; j++)
                {
                    var kf = GetKeyframe(beatmapObject, i, j);

                    kf.Render();
                }
            }

            var timelineObject = EditorTimeline.inst.GetTimelineObject(beatmapObject);
            if (timelineObject.InternalTimelineObjects.Count > 0 && timelineObject.InternalTimelineObjects.Where(x => x.Selected).Count() == 0)
            {
                if (EditorConfig.Instance.RememberLastKeyframeType.Value && timelineObject.InternalTimelineObjects.TryFind(x => x.Type == ObjEditor.inst.currentKeyframeKind, out TimelineKeyframe kf))
                    kf.Selected = true;
                else
                    timelineObject.InternalTimelineObjects[0].Selected = true;
            }

            if (timelineObject.InternalTimelineObjects.Count >= 1000)
                AchievementManager.inst.UnlockAchievement("holy_keyframes");
        }

        public void RenderKeyframe(BeatmapObject beatmapObject, TimelineKeyframe timelineObject)
        {
            if (beatmapObject.events[timelineObject.Type].TryFindIndex(x => x.id == timelineObject.ID, out int kfIndex))
                timelineObject.Index = kfIndex;

            var eventKeyframe = timelineObject.eventKeyframe;
            timelineObject.RenderSprite(beatmapObject.events[timelineObject.Type]);
            timelineObject.RenderPos();
            timelineObject.RenderIcons();
        }

        public void UpdateKeyframeOrder(BeatmapObject beatmapObject)
        {
            for (int i = 0; i < beatmapObject.events.Count; i++)
            {
                beatmapObject.events[i] = (from x in beatmapObject.events[i]
                                           orderby x.time
                                           select x).ToList();
            }

            RenderKeyframes(beatmapObject);
        }

        public static string IntToAxis(int num) => num switch
        {
            0 => "x",
            1 => "y",
            2 => "z",
            _ => string.Empty,
        };

        public static string IntToType(int num) => num switch
        {
            0 => "pos",
            1 => "sca",
            2 => "rot",
            3 => "col",
            _ => string.Empty,
        };

        public static string IntToTypeName(int num) => num switch
        {
            0 => "Position",
            1 => "Scale",
            2 => "Rotation",
            3 => "Color",
            4 => "Multi",
            _ => string.Empty,
        };

        #endregion

        #region Set Values

        public void SetKeyframeColor(BeatmapObject beatmapObject, int index, int value, List<Toggle> colorButtons, IEnumerable<TimelineKeyframe> selected)
        {
            foreach (var keyframe in selected.Select(x => x.eventKeyframe))
            {
                keyframe.values[index] = value;
                if (!RTEditor.ShowModdedUI)
                    keyframe.values[6] = 10f; // set behaviour to alpha's default if editor complexity is not set to advanced.
            }

            // Since keyframe color has no affect on the timeline object, we will only need to update the physical object.
            if (UpdateObjects)
                RTLevel.Current?.UpdateObject(beatmapObject, RTLevel.ObjectContext.KEYFRAMES);

            int num = 0;
            foreach (var toggle in colorButtons)
            {
                int tmpIndex = num;
                toggle.SetIsOnWithoutNotify(num == value);
                toggle.onValueChanged.NewListener(_val => SetKeyframeColor(beatmapObject, index, tmpIndex, colorButtons, selected));
                num++;
            }
        }

        #endregion
    }
}
