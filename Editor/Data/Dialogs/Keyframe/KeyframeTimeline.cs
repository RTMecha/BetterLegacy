using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

using LSFunctions;

using BetterLegacy.Configs;
using BetterLegacy.Core;
using BetterLegacy.Core.Components;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using BetterLegacy.Core.Runtime;
using BetterLegacy.Editor.Data.Timeline;
using BetterLegacy.Editor.Managers;

namespace BetterLegacy.Editor.Data.Dialogs
{
    /// <summary>
    /// Represents a keyframe timeline part of an editor dialog.
    /// </summary>
    public class KeyframeTimeline : Exists
    {
        #region Init
        
        /// <summary>
        /// Initializes the keyframe timeline.
        /// </summary>
        /// <param name="animationDialog">The main dialog.</param>
        public void Init(IAnimationDialog animationDialog)
        {
            AllTimelines.Add(this);

            Dialog = animationDialog;
            Parent = animationDialog.GameObject.transform.Find("timeline").AsRT();
            ScrollView = Parent.Find("Scroll View").AsRT();
            Viewport = ScrollView.Find("Viewport").AsRT();
            Content = Viewport.Find("Content").AsRT();

            var beginDragTrigger = TriggerHelper.CreateEntry(EventTriggerType.BeginDrag, eventData =>
            {
                var pointerEventData = (PointerEventData)eventData;
                ObjEditor.inst.DragStartPos = pointerEventData.position * EditorManager.inst.ScreenScaleInverse;
                if (pointerEventData.button == PointerEventData.InputButton.Middle)
                {
                    StartTimelineDrag();
                    return;
                }

                ObjEditor.inst.SelectionBoxImage.gameObject.SetActive(true);
                ObjEditor.inst.SelectionRect = default;
            });
            var dragTrigger = TriggerHelper.CreateEntry(EventTriggerType.Drag, eventData =>
            {
                if (movingTimeline)
                    return;

                var vector = ((PointerEventData)eventData).position * EditorManager.inst.ScreenScaleInverse;

                ObjEditor.inst.SelectionRect.xMin = vector.x < ObjEditor.inst.DragStartPos.x ? vector.x : ObjEditor.inst.DragStartPos.x;
                ObjEditor.inst.SelectionRect.xMax = vector.x < ObjEditor.inst.DragStartPos.x ? ObjEditor.inst.DragStartPos.x : vector.x;
                ObjEditor.inst.SelectionRect.yMin = vector.y < ObjEditor.inst.DragStartPos.y ? vector.y : ObjEditor.inst.DragStartPos.y;
                ObjEditor.inst.SelectionRect.yMax = vector.y < ObjEditor.inst.DragStartPos.y ? ObjEditor.inst.DragStartPos.y : vector.y;

                ObjEditor.inst.SelectionBoxImage.rectTransform.offsetMin = ObjEditor.inst.SelectionRect.min;
                ObjEditor.inst.SelectionBoxImage.rectTransform.offsetMax = ObjEditor.inst.SelectionRect.max;
            });
            var endDragTrigger = TriggerHelper.CreateEntry(EventTriggerType.EndDrag, eventData =>
            {
                var pointerEventData = (PointerEventData)eventData;
                ObjEditor.inst.DragEndPos = pointerEventData.position;
                ObjEditor.inst.SelectionBoxImage.gameObject.SetActive(false);

                if (movingTimeline)
                {
                    movingTimeline = false;
                    return;
                }

                CoroutineHelper.StartCoroutine(GroupSelectKeyframes(CurrentObject, Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)));
            });

            SelectionArea.Add(Content.Find("position").gameObject);
            SelectionArea.Add(Content.Find("scale").gameObject);
            SelectionArea.Add(Content.Find("rotation").gameObject);
            SelectionArea.Add(Content.Find("color").gameObject);
            SelectionArea.Add(Content.Find("dragselection").gameObject);
            foreach (var gameObject in SelectionArea)
                TriggerHelper.AddEventTriggers(gameObject, beginDragTrigger, dragTrigger, endDragTrigger);

            KeyframeParents.Add(Content.Find("position").AsRT());
            KeyframeParents.Add(Content.Find("scale").AsRT());
            KeyframeParents.Add(Content.Find("rotation").AsRT());
            KeyframeParents.Add(Content.Find("color").AsRT());

            var idRight = Viewport.Find("id/right");
            for (int i = 0; i < KeyframeParents.Count; i++)
            {
                var type = i;
                var entry = TriggerHelper.CreateEntry(EventTriggerType.PointerUp, eventData =>
                {
                    if (((PointerEventData)eventData).button == PointerEventData.InputButton.Right)
                        CreateKeyframe(type);
                });

                var eventTrigger = KeyframeParents[type].GetComponent<EventTrigger>();
                eventTrigger.triggers.RemoveAll(x => x.eventID == EventTriggerType.PointerUp);
                eventTrigger.triggers.Add(entry);

                EditorThemeManager.ApplyGraphic(idRight.GetChild(type).GetComponent<Image>(), EditorTheme.GetGroup($"Object Keyframe Color {type + 1}"));
            }

            TimelineLeft = Viewport.Find("id/left").AsRT();
            TimelineRight = Viewport.Find("id/right").AsRT();

            CoreHelper.Delete(TimelineLeft.Find("position"));
            CoreHelper.Delete(TimelineLeft.Find("scale"));
            CoreHelper.Delete(TimelineLeft.Find("rotation"));
            CoreHelper.Delete(TimelineLeft.Find("color"));

            Cursor = Viewport.Find("Content/time_slider").GetComponent<Slider>();
            PosScrollbar = ScrollView.GetComponent<ScrollRect>().horizontalScrollbar;
            TimelineGrid = Viewport.Find("Content/grid").AsRT();
            ZoomSlider = ScrollView.Find("zoom-panel/Slider").GetComponent<Slider>();

            TriggerHelper.AddEventTriggers(Content.gameObject,
                TriggerHelper.CreateEntry(EventTriggerType.PointerEnter,
                    eventData => MouseOver = true),
                TriggerHelper.CreateEntry(EventTriggerType.PointerExit,
                    eventData => MouseOver = false));

            Markers = Cursor.transform.Find("Markers").AsRT();

            Parent.GetComponent<EventTrigger>().triggers.Clear();

            TriggerHelper.AddEventTriggers(PosScrollbar.gameObject, TriggerHelper.CreateEntry(EventTriggerType.Scroll, baseEventData =>
            {
                var pointerEventData = (PointerEventData)baseEventData;

                var scrollBar = PosScrollbar;
                float multiply = Input.GetKey(KeyCode.LeftAlt) ? 0.1f : Input.GetKey(KeyCode.LeftControl) ? 10f : 1f;

                scrollBar.value = pointerEventData.scrollDelta.y > 0f ? scrollBar.value + (0.005f * multiply) : pointerEventData.scrollDelta.y < 0f ? scrollBar.value - (0.005f * multiply) : 0f;
            }));

            Cursor.onValueChanged.NewListener(_val =>
            {
                if (!changingTime)
                    return;

                time = _val;
                if (setTime)
                    AudioManager.inst.SetMusicTime(Mathf.Clamp(_val, 0f, AudioManager.inst.CurrentAudioSource.clip.length));
            });

            TriggerHelper.AddEventTriggers(Cursor.gameObject,
                TriggerHelper.CreateEntry(EventTriggerType.PointerDown, eventData =>
                {
                    changingTime = true;
                    time = Cursor.value;
                    if (setTime)
                        AudioManager.inst.SetMusicTime(Mathf.Clamp(Cursor.value, 0f, AudioManager.inst.CurrentAudioSource.clip.length));
                }),
                TriggerHelper.CreateEntry(EventTriggerType.PointerUp, eventData =>
                {
                    changingTime = false;
                    Cursor.SetValueWithoutNotify(time);
                }));

            try
            {
                cursorHandle = Cursor.transform.GetChild(1).GetChild(0).GetComponent<Image>();
                cursorRuler = cursorHandle.transform.GetChild(0).GetComponent<Image>();
            }
            catch (Exception ex)
            {
                CoreHelper.LogException(ex);
            }

            #region Editor Theme

            EditorThemeManager.ApplyScrollbar(ScrollView.Find("Scrollbar Horizontal").GetComponent<Scrollbar>(),
                scrollbarGroup: ThemeGroup.Timeline_Scrollbar_Base, handleGroup: ThemeGroup.Timeline_Scrollbar, canSetScrollbarRounded: false);
            EditorThemeManager.ApplyGraphic(Cursor.transform.Find("Background").GetComponent<Image>(), ThemeGroup.Timeline_Time_Scrollbar);
            EditorThemeManager.ApplyGraphic(ScrollView.GetComponent<Image>(), ThemeGroup.Background_1);

            var zoomSliderBack = Viewport.parent.Find("zoom back");
            var zoomSliderBase = ZoomSlider.transform.parent;

            EditorThemeManager.ApplyGraphic(zoomSliderBack.GetComponent<Image>(), ThemeGroup.Timeline_Scrollbar_Base);
            EditorThemeManager.ApplyGraphic(zoomSliderBase.GetComponent<Image>(), ThemeGroup.Background_1, true);
            EditorThemeManager.ApplyGraphic(zoomSliderBase.transform.GetChild(0).GetComponent<Image>(), ThemeGroup.Slider_2);
            EditorThemeManager.ApplyGraphic(zoomSliderBase.transform.GetChild(2).GetComponent<Image>(), ThemeGroup.Slider_2);
            EditorThemeManager.ApplyGraphic(ZoomSlider.transform.Find("Background").GetComponent<Image>(), ThemeGroup.Slider_2, true);
            EditorThemeManager.ApplyGraphic(ZoomSlider.transform.Find("Fill Area/Fill").GetComponent<Image>(), ThemeGroup.Slider_2, true);
            EditorThemeManager.ApplyGraphic(ZoomSlider.image, ThemeGroup.Slider_2_Handle, true);

            #endregion
        }

        void CreateKeyframe(int type)
        {
            var timeTmp = MouseTimelineCalc();

            var animatable = CurrentObject;
            if (animatable == null)
                return;

            var beatmapObject = animatable as BeatmapObject;
            var keyframes = animatable.GetEventKeyframes(type);

            var eventKeyfame = CreateEventKeyframe(animatable, timeTmp, type, keyframes.FindLast(x => x.time <= timeTmp), false);
            UpdateKeyframeOrder(animatable);
            RenderKeyframes(animatable);

            var keyframe = keyframes.FindLastIndex(x => x.id == eventKeyfame.id);
            if (keyframe < 0)
                keyframe = 0;

            SetCurrentKeyframe(animatable, type, keyframe, false, InputDataManager.inst.editorActions.MultiSelect.IsPressed);
            ResizeKeyframeTimeline(animatable);

            RenderDialog(animatable);
            RenderMarkers(animatable);

            if (!beatmapObject)
                return;

            // Keyframes affect both physical object and timeline object.
            EditorTimeline.inst.RenderTimelineObject(EditorTimeline.inst.GetTimelineObject(beatmapObject));
            RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.KEYFRAMES);
        }

        /// <summary>
        /// Ticks the keyframe timeline.
        /// </summary>
        public void Tick()
        {
            if (!changingTime && setTime && EditorTimeline.inst.CurrentSelection && EditorTimeline.inst.CurrentSelection.isBeatmapObject)
            {
                // Sets new audio time using the Object Keyframe timeline cursor.
                time = Mathf.Clamp(AudioManager.inst.CurrentAudioSource.time,
                    EditorTimeline.inst.CurrentSelection.Time,
                    EditorTimeline.inst.CurrentSelection.Time + EditorTimeline.inst.CurrentSelection.GetData<BeatmapObject>().GetObjectLifeLength(ObjEditor.inst.ObjectLengthOffset));
                Cursor.SetValueWithoutNotify(time);
            }

            try
            {
                float multiply = Input.GetKey(KeyCode.LeftControl) ? 2f : Input.GetKey(KeyCode.LeftShift) ? 0.1f : 1f;
                if (Input.GetKey(EditorConfig.Instance.BinControlKey.Value))
                    return;

                if (Dialog.IsCurrent && MouseOver && !CoreHelper.IsUsingInputField && !EditorTimeline.inst.isOverMainTimeline)
                {
                    if (InputDataManager.inst.editorActions.ZoomIn.WasPressed)
                        ObjEditor.inst.Zoom = ObjEditor.inst.zoomFloat + EditorConfig.Instance.KeyframeZoomAmount.Value * multiply;
                    if (InputDataManager.inst.editorActions.ZoomOut.WasPressed)
                        ObjEditor.inst.Zoom = ObjEditor.inst.zoomFloat - EditorConfig.Instance.KeyframeZoomAmount.Value * multiply;
                }
            }
            catch
            {

            }

            if (Input.GetMouseButtonUp(0))
                draggingKeyframes = false;

            HandleKeyframesDrag();
            HandleTimelineDrag();
        }

        #endregion

        #region Values

        /// <summary>
        /// The currently active keyframe timeline.
        /// </summary>
        public static KeyframeTimeline CurrentTimeline { get; set; }

        /// <summary>
        /// All registered keyframe timelines.
        /// </summary>
        public static List<KeyframeTimeline> AllTimelines { get; set; } = new List<KeyframeTimeline>();

        /// <summary>
        /// Main dialog the timeline is a part of.
        /// </summary>
        public IAnimationDialog Dialog { get; set; }

        /// <summary>
        /// The currently selected object.
        /// </summary>
        public IAnimatable CurrentObject { get; set; }

        #region UI

        public RectTransform TimelineLeft { get; set; }
        public RectTransform TimelineRight { get; set; }
        public RectTransform TimelineGrid { get; set; }

        public List<GameObject> SelectionArea { get; set; } = new List<GameObject>();

        public RectTransform Parent { get; set; }
        public RectTransform ScrollView { get; set; }
        public RectTransform Viewport { get; set; }
        public RectTransform Content { get; set; }

        public List<RectTransform> KeyframeParents { get; set; } = new List<RectTransform>();

        public Transform Markers { get; set; }

        public Slider ZoomSlider { get; set; }
        public Slider Cursor { get; set; }
        public Scrollbar PosScrollbar { get; set; }

        public Image cursorHandle;
        public Image cursorRuler;

        public List<TimelineKeyframe> RenderedKeyframes { get; set; } = new List<TimelineKeyframe>();

        #endregion

        #region Keyframe Editor

        public List<Toggle> startColorsReference;
        public List<Toggle> endColorsReference;

        public int currentKeyframeType;
        public int currentKeyframeIndex;

        public static List<TimelineKeyframe> copiedObjectKeyframes = new List<TimelineKeyframe>();

        public EventKeyframe CopiedPositionData { get; set; }
        public EventKeyframe CopiedScaleData { get; set; }
        public EventKeyframe CopiedRotationData { get; set; }
        public EventKeyframe CopiedColorData { get; set; }

        #endregion

        #region Timeline States

        public bool movingTimeline;
        public Vector2 cachedTimelinePos;

        /// <summary>
        /// If the mouse cursor is over this timeline.
        /// </summary>
        public bool MouseOver { get; set; }

        public bool changingTime;

        public float time;

        public bool setTime;

        public bool draggingKeyframes;

        #endregion

        #endregion

        #region Methods

        #region Deleting

        public IEnumerator DeleteKeyframes(IAnimatable animatable)
        {
            var list = animatable.TimelineKeyframes.Where(x => x.Selected).ToList();
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

            UpdateKeyframeOrder(animatable);

            var strs = new List<string>();
            foreach (var timelineObject in list)
            {
                if (timelineObject.Index != 0)
                    strs.Add(timelineObject.eventKeyframe.id);
            }

            var events = animatable.Events;
            for (int i = 0; i < events.Count; i++)
                events[i].RemoveAll(x => strs.Contains(x.id));

            animatable.TimelineKeyframes.ForLoopReverse((timelineKeyframe, index) =>
            {
                if (!timelineKeyframe.Selected)
                    return;

                CoreHelper.Delete(timelineKeyframe.GameObject);
                animatable.TimelineKeyframes.RemoveAt(index);
            });

            if (animatable is BeatmapObject beatmapObject)
            {
                EditorTimeline.inst.RenderTimelineObject(EditorTimeline.inst.GetTimelineObject(beatmapObject));
                RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.KEYFRAMES);

                if (beatmapObject.autoKillType == AutoKillType.LastKeyframe || beatmapObject.autoKillType == AutoKillType.LastKeyframeOffset)
                    RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.AUTOKILL);
            }

            RenderKeyframes(animatable);

            if (count == 1 || allOfTheSameType)
                SetCurrentKeyframe(animatable, type, Mathf.Clamp(index - 1, 0, events[type].Count - 1));
            else
                SetCurrentKeyframe(animatable, type, 0);

            ResizeKeyframeTimeline(animatable);
            RenderMarkers(animatable);

            EditorManager.inst.DisplayNotification("Deleted Object Keyframes [ " + count + " ]", 2f, EditorManager.NotificationType.Success);

            yield break;
        }

        #endregion

        #region Copy / Paste

        public void CopyAllSelectedEvents(IAnimatable animatable)
        {
            copiedObjectKeyframes.Clear();
            UpdateKeyframeOrder(animatable);

            float num = animatable.TimelineKeyframes.Where(x => x.Selected).Min(x => x.Time);
            var events = animatable.Events;

            foreach (var timelineObject in animatable.TimelineKeyframes.Where(x => x.Selected))
            {
                int type = timelineObject.Type;
                int index = timelineObject.Index;
                var eventKeyframe = events[type][index].Copy();
                eventKeyframe.time -= num;

                copiedObjectKeyframes.Add(new TimelineKeyframe(eventKeyframe) { Type = type, Index = index, isObjectKeyframe = true });
            }
        }

        public static void CopyAllKeyframes(IAnimatable animatable)
        {
            copiedObjectKeyframes.Clear();

            var events = animatable.Events;
            for (int i = 0; i < events.Count; i++)
            {
                for (int j = 0; j < events[i].Count; j++)
                {
                    var eventKeyframe = events[i][j];
                    copiedObjectKeyframes.Add(new TimelineKeyframe(eventKeyframe) { Type = i, Index = j, isObjectKeyframe = true });
                }
            }
        }

        public List<TimelineKeyframe> PasteKeyframes(IAnimatable animatable, bool setTime = true) => PasteKeyframes(animatable, copiedObjectKeyframes, setTime);

        public List<TimelineKeyframe> PasteKeyframes(IAnimatable animatable, List<TimelineKeyframe> kfs, bool setTime = true)
        {
            if (kfs.Count <= 0)
            {
                Debug.LogError($"{ObjEditor.inst.className}No copied event yet!");
                return null;
            }

            var pastedKeyframes = new List<TimelineKeyframe>();
            var eventKeyframes = new List<EventKeyframe>();

            var events = animatable.Events;
            for (int i = 0; i < events.Count; i++)
            {
                events[i].AddRange(kfs.Where(x => x.Type == i).Select(x =>
                {
                    var kf = PasteKF(animatable, x, setTime);
                    eventKeyframes.Add(kf);
                    return kf;
                }));
                events[i].Sort((a, b) => a.time.CompareTo(b.time));
            }

            ResizeKeyframeTimeline(animatable);
            UpdateKeyframeOrder(animatable);
            RenderKeyframes(animatable);
            RenderMarkers(animatable);

            if (EditorConfig.Instance.SelectPasted.Value)
                foreach (var kf in animatable.TimelineKeyframes)
                    kf.Selected = false;

            foreach (var eventKeyframe in eventKeyframes)
            {
                if (!eventKeyframe.timelineKeyframe)
                    continue;

                if (EditorConfig.Instance.SelectPasted.Value)
                    eventKeyframe.timelineKeyframe.Selected = true;
                pastedKeyframes.Add(eventKeyframe.timelineKeyframe);
            }

            RenderDialog(animatable);

            if (animatable is BeatmapObject beatmapObject)
            {
                EditorTimeline.inst.RenderTimelineObject(EditorTimeline.inst.GetTimelineObject(beatmapObject));
                RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.KEYFRAMES);
                RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.AUTOKILL);
            }
            return pastedKeyframes;
        }

        public EventKeyframe PasteKF(IAnimatable animatable, TimelineKeyframe timelineKeyframe, bool setTime = true)
        {
            var eventKeyframe = timelineKeyframe.eventKeyframe.Copy();

            var time = 0f;
            if (animatable is BeatmapObject)
            {
                time = RTLevel.Current.FixedTime;
                if (RTEditor.inst.editorInfo.bpmSnapActive && EditorConfig.Instance.BPMSnapsPasted.Value)
                    time = RTEditor.SnapToBPM(time);
                time -= animatable.StartTime;
            }

            if (animatable.EditorData.miscDisplayValues.TryGetValue(IntToType(timelineKeyframe.Type) + "/force_relative", out float forceRelative))
                eventKeyframe.relative = forceRelative switch
                {
                    1f => true,
                    _ => false,
                };

            if (!setTime)
                return eventKeyframe;

            eventKeyframe.time = time + eventKeyframe.time;
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
                case 0:
                    CopiedPositionData = kf.Copy();
                    break;
                case 1:
                    CopiedScaleData = kf.Copy();
                    break;
                case 2:
                    CopiedRotationData = kf.Copy();
                    break;
                case 3:
                    CopiedColorData = kf.Copy();
                    break;
            }
        }

        public void SetCopiedData(int type, EventKeyframe eventKeyframe, IAnimatable animatable)
        {
            SetData(eventKeyframe, GetCopiedData(type));
            if (animatable.EditorData.miscDisplayValues.TryGetValue(IntToType(type) + "/force_relative", out float forceRelative))
                eventKeyframe.relative = forceRelative switch
                {
                    1f => true,
                    _ => false,
                };
        }

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

        public void PasteKeyframeData(int type, IEnumerable<TimelineKeyframe> selected, IAnimatable animatable)
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

            foreach (var timelineKeyframe in selected)
            {
                if (timelineKeyframe.Type != type)
                    continue;

                SetData(timelineKeyframe.eventKeyframe, copiedData);
                if (animatable.EditorData.miscDisplayValues.TryGetValue(IntToType(timelineKeyframe.Type) + "/force_relative", out float forceRelative))
                    timelineKeyframe.eventKeyframe.relative = forceRelative switch
                    {
                        1f => true,
                        _ => false,
                    };
            }

            RenderKeyframes(animatable);
            RenderDialog(animatable);
            if (animatable is BeatmapObject beatmapObject)
                RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.KEYFRAMES);
            EditorManager.inst.DisplayNotification($"Pasted {name.ToLower()} keyframe data to current selected keyframe.", 2f, EditorManager.NotificationType.Success);
        }

        #endregion

        #region Selection

        public IEnumerator GroupSelectKeyframes(IAnimatable animatable, bool _add = true)
        {
            if (animatable == null)
                yield break;

            var list = animatable.TimelineKeyframes;

            if (!_add)
                list.ForEach(x => x.Selected = false);

            list.Where(x => RTMath.RectTransformToScreenSpace(ObjEditor.inst.SelectionBoxImage.rectTransform)
            .Overlaps(RTMath.RectTransformToScreenSpace(x.Image.rectTransform))).ToList().ForEach(timelineObject =>
            {
                timelineObject.Selected = true;
                timelineObject.timeOffset = 0f;
                currentKeyframeType = timelineObject.Type;
                currentKeyframeIndex = timelineObject.Index;
            });

            RenderDialog(animatable);
            RenderKeyframes(animatable);

            yield break;
        }

        public void SetCurrentKeyframe(IAnimatable animatable, int _keyframe, bool _bringTo = false) => SetCurrentKeyframe(animatable, currentKeyframeType, _keyframe, _bringTo, false);

        public void AddCurrentKeyframe(IAnimatable animatable, int _add, bool _bringTo = false)
        {
            SetCurrentKeyframe(animatable,
                currentKeyframeType,
                Mathf.Clamp(currentKeyframeIndex + _add == int.MaxValue ? 1000000 : _add, 0, animatable.GetEventKeyframes(currentKeyframeType).Count - 1),
                _bringTo);
        }

        public void SetCurrentKeyframe(IAnimatable animatable, int type, int index, bool _bringTo = false, bool _shift = false)
        {
            if (!draggingKeyframes)
            {
                Debug.Log($"{ObjEditor.inst.className}Setting Current Keyframe: {type}, {index}");
                if (!_shift && animatable.TimelineKeyframes.Count > 0)
                    animatable.TimelineKeyframes.ForEach(timelineObject => timelineObject.Selected = false);

                var kf = GetKeyframe(animatable, type, index);

                kf.Selected = !_shift || !kf.Selected;
            }

            currentKeyframeType = type;
            currentKeyframeIndex = index;

            if (_bringTo)
            {
                float value = animatable.GetEventKeyframes(currentKeyframeType)[currentKeyframeIndex].time + animatable.StartTime;

                if (animatable is BeatmapObject beatmapObject)
                {
                    value = Mathf.Clamp(value, AllowTimeExactlyAtStart ? beatmapObject.StartTime + 0.001f : beatmapObject.StartTime, beatmapObject.StartTime + beatmapObject.GetObjectLifeLength());

                    AudioManager.inst.SetMusicTime(Mathf.Clamp(value, 0f, AudioManager.inst.CurrentAudioSource.clip.length));
                    AudioManager.inst.CurrentAudioSource.Pause();
                    EditorManager.inst.UpdatePlayButton();
                }
            }

            RenderDialog(animatable);
        }

        public EventKeyframe CreateEventKeyframe(IAnimatable animatable, float time, int type, EventKeyframe previousKeyframe, bool openDialog)
        {
            var eventKeyframe = previousKeyframe.Copy();
            var t = RTEditor.inst.editorInfo.bpmSnapActive && EditorConfig.Instance.BPMSnapsCreated.Value && EditorConfig.Instance.BPMSnapsKeyframes.Value ? -(animatable.StartTime - RTEditor.SnapToBPM(animatable.StartTime + time)) : time;
            eventKeyframe.time = t;

            if (eventKeyframe.relative)
                for (int i = 0; i < eventKeyframe.values.Length; i++)
                    eventKeyframe.values[i] = 0f;

            if (type == 0) // position type has 4 random values.
                eventKeyframe.SetRandomValues(eventKeyframe.GetRandomValue(0), eventKeyframe.GetRandomValue(1), eventKeyframe.GetRandomValue(2), eventKeyframe.GetRandomValue(3));
            else
                eventKeyframe.SetRandomValues(eventKeyframe.GetRandomValue(0), eventKeyframe.GetRandomValue(1), eventKeyframe.GetRandomValue(2));

            eventKeyframe.locked = false;

            var events = animatable.GetEventKeyframes(type);
            var index = events.FindIndex(x => x.id == previousKeyframe.id);
            events.Insert(index + 1, eventKeyframe);

            if (animatable is BeatmapObject beatmapObject)
            {
                EditorTimeline.inst.RenderTimelineObject(EditorTimeline.inst.GetTimelineObject(beatmapObject));
                RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.AUTOKILL);
            }
            if (openDialog)
            {
                ResizeKeyframeTimeline(animatable);
                RenderDialog(animatable);
            }
            return eventKeyframe;
        }

        public void HandleKeyframesDrag()
        {
            if (!draggingKeyframes)
                return;

            var animatable = CurrentObject;
            if (animatable == null)
                return;

            var beatmapObject = animatable as BeatmapObject;

            var timelineCalc = MouseTimelineCalc();
            var selected = animatable.TimelineKeyframes.Where(x => x.Selected);
            var startTime = 0f;
            float length = float.MaxValue;

            float timeOffset = timelineCalc + ObjEditor.inst.mouseOffsetXForKeyframeDrag;
            if (beatmapObject)
            {
                startTime = beatmapObject.StartTime;
                length = beatmapObject.GetObjectLifeLength(ObjEditor.inst.ObjectLengthOffset);
                timeOffset = Mathf.Round(Mathf.Clamp(timeOffset, 0f, AudioManager.inst.CurrentAudioSource.clip.length) * 1000f) / 1000f;
                if (RTEditor.inst.editorInfo.bpmSnapActive && EditorConfig.Instance.BPMSnapsObjectKeyframes.Value && !Input.GetKey(KeyCode.LeftAlt))
                    timeOffset = -(startTime - RTEditor.SnapToBPM(startTime + timeOffset));
            }
            else
                timeOffset = Mathf.Round(timeOffset * 1000f) / 1000f;

            bool changed = false;
            foreach (var timelineKeyframe in selected)
            {
                if (timelineKeyframe.Index == 0 || timelineKeyframe.Locked)
                    continue;

                float calc = Mathf.Clamp(timeOffset + timelineKeyframe.timeOffset, 0f, length);

                timelineKeyframe.Time = calc;
                timelineKeyframe.Render();
                changed = true;
            }

            if (beatmapObject)
            {
                RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.KEYFRAMES);
                RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.AUTOKILL);
            }

            RenderDialog(animatable);
            ResizeKeyframeTimeline(animatable);
            RenderMarkerPositions(animatable);

            foreach (var timelineObject in EditorTimeline.inst.SelectedBeatmapObjects)
                EditorTimeline.inst.RenderTimelineObject(timelineObject);

            if (changed && !Input.GetKey(KeyCode.LeftAlt) && !selected.All(x => x.Locked) && RTEditor.inst.dragOffset != timelineCalc + ObjEditor.inst.mouseOffsetXForDrag)
            {
                if (RTEditor.DraggingPlaysSound && (RTEditor.inst.editorInfo.bpmSnapActive && EditorConfig.Instance.BPMSnapsObjectKeyframes.Value || !RTEditor.DraggingPlaysSoundBPM))
                    SoundManager.inst.PlaySound(DefaultSounds.LeftRight, RTEditor.inst.editorInfo.bpmSnapActive && EditorConfig.Instance.BPMSnapsObjectKeyframes.Value ? 0.6f : 0.1f, 0.8f);

                RTEditor.inst.dragOffset = timelineCalc + ObjEditor.inst.mouseOffsetXForDrag;
            }
        }

        /// <summary>
        /// Gets a keyframe coordinate of the currently selected keyframe.
        /// </summary>
        /// <returns>Returns a <see cref="KeyframeCoord"/>.</returns>
        public KeyframeCoord GetSelectionCoord() => new KeyframeCoord(currentKeyframeType, currentKeyframeIndex);

        #endregion

        #region Timeline

        /// <summary>
        /// Sets the Object Keyframe timeline position.
        /// </summary>
        /// <param name="position">The position to set the timeline scroll.</param>
        public void SetTimelinePosition(float position) => SetTimeline(CurrentObject, ObjEditor.inst.zoomFloat, position);

        /// <summary>
        /// Sets the Object Keyframe timeline zoom.
        /// </summary>
        /// <param name="zoom">The zoom to set to the timeline.</param>
        public void SetTimelineZoom(float zoom)
        {
            var beatmapObject = CurrentObject as BeatmapObject;
            float timelineCalc = Cursor.value;
            if (EditorConfig.Instance.UseMouseAsZoomPoint.Value)
                timelineCalc = MouseTimelineCalc() / Cursor.maxValue;
            else if (AudioManager.inst.CurrentAudioSource.clip && beatmapObject)
            {
                float time = -beatmapObject.StartTime + AudioManager.inst.CurrentAudioSource.time;
                float objectLifeLength = beatmapObject.GetObjectLifeLength(ObjEditor.inst.ObjectLengthOffset);

                timelineCalc = time / objectLifeLength;
            }

            SetTimeline(CurrentObject, zoom, timelineCalc);
        }

        /// <summary>
        /// Sets the Object Keyframe timeline zoom and position.
        /// </summary>
        /// <param name="zoom">The amount to zoom in.</param>
        /// <param name="position">The position to set the timeline scroll. If the value is less that 0, it will automatically calculate the position to match the audio time.</param>
        /// <param name="render">If the timeline should render.</param>
        public void SetTimeline(IAnimatable animatable, float zoom, float position, bool render = true)
        {
            float prevZoom = ObjEditor.inst.zoomFloat;
            ObjEditor.inst.zoomFloat = Mathf.Clamp01(zoom);
            ObjEditor.inst.zoomVal =
                LSMath.InterpolateOverCurve(ObjEditor.inst.ZoomCurve, ObjEditor.inst.zoomBounds.x, ObjEditor.inst.zoomBounds.y, ObjEditor.inst.zoomFloat);

            if (ObjectEditor.inst.Dialog.IsCurrent)
            {
                EditorTimeline.inst.CurrentSelection.Zoom = ObjEditor.inst.zoomFloat;
                EditorTimeline.inst.CurrentSelection.TimelinePosition = position;
            }

            if (render && animatable != null)
            {
                ResizeKeyframeTimeline(animatable);
                RenderKeyframes(animatable);
                RenderMarkerPositions(animatable);
            }

            PosScrollbar.SetValueWithoutNotify(position);
            PosScrollbar.onValueChanged.NewListener(SetTimelineScroll);
            SetTimelineScroll(position);

            ZoomSlider.SetValueWithoutNotify(ObjEditor.inst.zoomFloat);
            ZoomSlider.onValueChanged.NewListener(_val =>
            {
                ObjEditor.inst.Zoom = _val;
                if (ObjectEditor.inst.Dialog.IsCurrent)
                    EditorTimeline.inst.CurrentSelection.Zoom = Mathf.Clamp01(_val);
            });
        }

        void SetTimelineScroll(float scroll)
        {
            ScrollView.GetComponent<ScrollRect>().horizontalNormalizedPosition = scroll;
            if (ObjectEditor.inst.Dialog.IsCurrent)
                EditorTimeline.inst.CurrentSelection.TimelinePosition = scroll;
        }

        public static float TimeTimelineCalc(float _time) => _time * 14f * ObjEditor.inst.zoomVal + 5f;

        public float MouseTimelineCalc()
        {
            float num = Screen.width * ((1155f - Mathf.Abs(Content.anchoredPosition.x) + 7f) / 1920f);
            float screenScale = 1f / (Screen.width / 1920f);
            float mouseX = Input.mousePosition.x < num ? num : Input.mousePosition.x;

            return (mouseX - num) / ObjEditor.inst.Zoom / 14f * screenScale;
        }

        public void HandleTimelineDrag()
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
            cachedTimelinePos = new Vector2(PosScrollbar.value, 0f);
            movingTimeline = true;
        }

        #endregion

        #region Editor
        
        public void RenderDialog(IAnimatable animatable)
        {
            SetCursorColor(EditorConfig.Instance.KeyframeCursorColor.Value);

            CurrentTimeline = this;
            CurrentObject = animatable;

            var selected = animatable.TimelineKeyframes.Where(x => x.Selected);
            var count = selected.Count();

            if (count < 1)
            {
                Dialog.CloseKeyframeDialogs();
                return;
            }

            var beatmapObject = animatable as BeatmapObject;

            if (!(count == 1 || selected.All(x => x.Type == selected.Min(y => y.Type))))
            {
                Dialog.OpenKeyframeDialog(4);

                try
                {
                    var multiDialog = Dialog.KeyframeDialogs[4].GameObject.transform;
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

                            RenderKeyframes(animatable);

                            // Keyframe Time affects both physical object and timeline object.
                            if (animatable is BeatmapObject beatmapObject)
                            {
                                EditorTimeline.inst.RenderTimelineObject(EditorTimeline.inst.GetTimelineObject(beatmapObject));
                                RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.KEYFRAMES);
                            }

                            ResizeKeyframeTimeline(animatable);
                            RenderMarkers(animatable);
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

                            RenderKeyframes(animatable);

                            // Keyframe Time affects both physical object and timeline object.
                            if (animatable is BeatmapObject beatmapObject)
                            {
                                EditorTimeline.inst.RenderTimelineObject(EditorTimeline.inst.GetTimelineObject(beatmapObject));
                                RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.KEYFRAMES);
                            }

                            ResizeKeyframeTimeline(animatable);
                            RenderMarkers(animatable);
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

                            RenderKeyframes(animatable);

                            // Keyframe Time affects both physical object and timeline object.
                            if (animatable is BeatmapObject beatmapObject)
                            {
                                EditorTimeline.inst.RenderTimelineObject(EditorTimeline.inst.GetTimelineObject(beatmapObject));
                                RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.KEYFRAMES);
                            }

                            ResizeKeyframeTimeline(animatable);
                            RenderMarkers(animatable);
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

                            RenderKeyframes(animatable);

                            // Keyframe Time affects both physical object and timeline object.
                            if (animatable is BeatmapObject beatmapObject)
                            {
                                EditorTimeline.inst.RenderTimelineObject(EditorTimeline.inst.GetTimelineObject(beatmapObject));
                                RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.KEYFRAMES);
                            }

                            ResizeKeyframeTimeline(animatable);
                            RenderMarkers(animatable);
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

                            RenderKeyframes(animatable);

                            // Keyframe Time affects both physical object and timeline object.
                            if (animatable is BeatmapObject beatmapObject)
                            {
                                EditorTimeline.inst.RenderTimelineObject(EditorTimeline.inst.GetTimelineObject(beatmapObject));
                                RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.KEYFRAMES);
                            }

                            ResizeKeyframeTimeline(animatable);
                            RenderMarkers(animatable);
                        }
                    });

                    TriggerHelper.AddEventTriggers(time.gameObject, TriggerHelper.ScrollDelta(time));

                    var curvesMulti = multiDialog.Find("curves/curves").GetComponent<Dropdown>();
                    var curvesMultiApplyButton = multiDialog.Find("curves/apply").GetComponent<Button>();
                    curvesMulti.onValueChanged.ClearAll();
                    curvesMultiApplyButton.onClick.NewListener(() =>
                    {
                        var anim = RTEditor.inst.GetEasing(curvesMulti.value);
                        foreach (var keyframe in selected)
                        {
                            if (keyframe.Index != 0)
                                keyframe.eventKeyframe.curve = anim;
                        }

                        RenderKeyframes(animatable);

                        // Keyframe Time affects both physical object and timeline object.
                        if (animatable is BeatmapObject beatmapObject)
                            RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.KEYFRAMES);

                        RenderMarkers(animatable);
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

                            // Keyframe Time affects both physical object and timeline object.
                            if (animatable is BeatmapObject beatmapObject)
                                RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.KEYFRAMES);
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
            var events = animatable.GetEventKeyframes(type);

            CoreHelper.Log($"Selected Keyframe:\nID - {firstKF.ID}\nType: {firstKF.Type}\nIndex {firstKF.Index}");

            Dialog.OpenKeyframeDialog(type);

            currentKeyframeType = type;
            currentKeyframeIndex = firstKF.Index;

            var dialog = Dialog.KeyframeDialogs[type];
            var kfdialog = dialog.GameObject.transform;

            dialog.EventTimeField.SetInteractible(!isFirst);

            dialog.JumpToStartButton.interactable = !isFirst;
            dialog.JumpToStartButton.onClick.NewListener(() => SetCurrentKeyframe(animatable, 0, true));

            dialog.JumpToPrevButton.interactable = selected.Count() == 1 && firstKF.Index != 0;
            dialog.JumpToPrevButton.onClick.NewListener(() => SetCurrentKeyframe(animatable, firstKF.Index - 1, true));

            dialog.KeyframeIndexer.text = firstKF.Index == 0 ? "S" : firstKF.Index == events.Count - 1 ? "E" : firstKF.Index.ToString();

            dialog.JumpToNextButton.interactable = selected.Count() == 1 && firstKF.Index < events.Count - 1;
            dialog.JumpToNextButton.onClick.NewListener(() => SetCurrentKeyframe(animatable, firstKF.Index + 1, true));

            dialog.JumpToLastButton.interactable = selected.Count() == 1 && firstKF.Index < events.Count - 1;
            dialog.JumpToLastButton.onClick.NewListener(() => SetCurrentKeyframe(animatable, events.Count - 1, true));

            dialog.CopyButton.OnClick.NewListener(() =>
            {
                CopyData(firstKF.Type, firstKF.eventKeyframe);
                EditorManager.inst.DisplayNotification("Copied keyframe data!", 2f, EditorManager.NotificationType.Success);
            });

            dialog.PasteButton.OnClick.NewListener(() => PasteKeyframeData(type, selected, animatable));

            dialog.DeleteButton.OnClick.NewListener(DeleteKeyframes(animatable).Start);

            dialog.EventTimeField.eventTrigger.triggers.Clear();
            if (count == 1 && firstKF.Index != 0 || count > 1)
                dialog.EventTimeField.eventTrigger.triggers.Add(TriggerHelper.ScrollDelta(dialog.EventTimeField.inputField));

            dialog.EventTimeField.SetTextWithoutNotify(count == 1 ? firstKF.Time.ToString() : "1");
            dialog.EventTimeField.OnValueChanged.NewListener(_val =>
            {
                if (float.TryParse(_val, out float num) && !draggingKeyframes && selected.Count() == 1)
                {
                    if (num < 0f)
                        num = 0f;

                    if (EditorConfig.Instance.RoundToNearest.Value)
                        num = RTMath.RoundToNearestDecimal(num, 3);

                    firstKF.Time = num;

                    RenderKeyframes(animatable);

                    // Keyframe Time affects both physical object and timeline object.
                    if (animatable is BeatmapObject beatmapObject)
                    {
                        EditorTimeline.inst.RenderTimelineObject(EditorTimeline.inst.GetTimelineObject(beatmapObject));
                        RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.KEYFRAMES);
                    }

                    ResizeKeyframeTimeline(animatable);
                    RenderMarkers(animatable);
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

                        RenderKeyframes(animatable);

                        // Keyframe Time affects both physical object and timeline object.
                        if (animatable is BeatmapObject beatmapObject)
                        {
                            EditorTimeline.inst.RenderTimelineObject(EditorTimeline.inst.GetTimelineObject(beatmapObject));
                            RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.KEYFRAMES);
                        }

                        ResizeKeyframeTimeline(animatable);
                        RenderMarkers(animatable);
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

                        RenderKeyframes(animatable);

                        // Keyframe Time affects both physical object and timeline object.
                        if (animatable is BeatmapObject beatmapObject)
                        {
                            EditorTimeline.inst.RenderTimelineObject(EditorTimeline.inst.GetTimelineObject(beatmapObject));
                            RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.KEYFRAMES);
                        }

                        ResizeKeyframeTimeline(animatable);
                        RenderMarkers(animatable);
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

                        RenderKeyframes(animatable);

                        // Keyframe Time affects both physical object and timeline object.
                        if (animatable is BeatmapObject beatmapObject)
                        {
                            EditorTimeline.inst.RenderTimelineObject(EditorTimeline.inst.GetTimelineObject(beatmapObject));
                            RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.KEYFRAMES);
                        }

                        ResizeKeyframeTimeline(animatable);
                        RenderMarkers(animatable);
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

                        RenderKeyframes(animatable);

                        // Keyframe Time affects both physical object and timeline object.
                        if (animatable is BeatmapObject beatmapObject)
                        {
                            EditorTimeline.inst.RenderTimelineObject(EditorTimeline.inst.GetTimelineObject(beatmapObject));
                            RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.KEYFRAMES);
                        }

                        ResizeKeyframeTimeline(animatable);
                        RenderMarkers(animatable);
                    }
                });
            }

            dialog.CurvesLabel.SetActive(count == 1 && firstKF.Index != 0 || count > 1);
            dialog.CurvesDropdown.gameObject.SetActive(count == 1 && firstKF.Index != 0 || count > 1);
            dialog.CurvesDropdown.SetValueWithoutNotify(RTEditor.inst.GetEaseIndex(firstKF.eventKeyframe.curve.ToString()));
            dialog.CurvesDropdown.onValueChanged.NewListener(_val =>
            {
                var anim = RTEditor.inst.GetEasing(_val);
                foreach (var keyframe in selected)
                {
                    if (keyframe.Index != 0)
                        keyframe.eventKeyframe.curve = anim;
                }

                RenderKeyframes(animatable);

                // Keyframe Time affects both physical object and timeline object.
                if (animatable is BeatmapObject beatmapObject)
                    RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.KEYFRAMES);
            });
            TriggerHelper.AddEventTriggers(dialog.CurvesDropdown.gameObject, TriggerHelper.ScrollDelta(dialog.CurvesDropdown));

            switch (type)
            {
                case 0: {
                        for (int i = 0; i < dialog.EventValueElements.Count; i++)
                            dialog.EventValueElements[i].Render(type, i, selected, firstKF, animatable);

                        KeyframeRandomHandler(type, selected, firstKF, animatable);
                        for (int i = 0; i < 2; i++)
                            KeyframeRandomValueHandler(type, i, selected, firstKF, animatable);

                        break;
                    }
                case 1: {
                        for (int i = 0; i < dialog.EventValueElements.Count; i++)
                            dialog.EventValueElements[i].Render(type, i, selected, firstKF, animatable);

                        KeyframeRandomHandler(type, selected, firstKF, animatable);
                        for (int i = 0; i < 2; i++)
                            KeyframeRandomValueHandler(type, i, selected, firstKF, animatable);

                        break;
                    }
                case 2: {
                        dialog.EventValueElements[0].Render(type, 0, selected, firstKF, animatable);

                        KeyframeRandomHandler(type, selected, firstKF, animatable);
                        for (int i = 0; i < 2; i++)
                            KeyframeRandomValueHandler(type, i, selected, firstKF, animatable);

                        break;
                    }
                case 3: {
                        FullColorKeyframeHandler(selected, firstKF, animatable, beatmapObject, kfdialog);
                        break;
                    }
            }

            RenderRelative(type, selected, firstKF, animatable, beatmapObject, dialog);

            dialog.GameObject.transform.parent.gameObject.GetOrAddComponent<Button>();
            if (type != 3)
                EditorContextMenu.AddContextMenu(dialog.GameObject.transform.parent.gameObject,
                    new ButtonElement("Show Random", () =>
                    {
                        animatable.EditorData.miscDisplayValues.Remove(IntToType(type) + "/random_active");
                        RenderDialog(animatable);
                    }),
                    new ButtonElement("Show Relative Toggle", () =>
                    {
                        animatable.EditorData.miscDisplayValues.Remove(IntToType(type) + "/relative_active");
                        RenderDialog(animatable);
                    }),
                    new SpacerElement(),
                    new ButtonElement("Remove Keyframe Settings", () =>
                    {
                        animatable.EditorData.miscDisplayValues.Remove(IntToType(type) + "/random_active");
                        animatable.EditorData.miscDisplayValues.Remove(IntToType(type) + "/relative_active");
                        animatable.EditorData.miscDisplayValues.Remove(IntToType(type) + "/lock_relative");
                        animatable.EditorData.miscDisplayValues.Remove(IntToType(type) + "/force_relative");
                        RenderDialog(animatable);
                    }));
            else
                EditorContextMenu.AddContextMenu(dialog.GameObject.transform.parent.gameObject);
        }

        void KeyframeRandomHandler(int type, IEnumerable<TimelineKeyframe> selected, TimelineKeyframe firstKF, IAnimatable animatable)
        {
            var dialog = Dialog.KeyframeDialogs[type];
            if (dialog.RandomToggles == null)
                return;

            var randomActive = !animatable.EditorData.miscDisplayValues.TryGetValue(IntToType(type) + "/random_active", out float randomActiveF) || randomActiveF == 1f;
            CoreHelper.SetGameObjectActive(dialog.RandomTogglesParent.GetPreviousSibling()?.gameObject, randomActive);
            dialog.RandomTogglesParent.gameObject.SetActive(randomActive);
            int random = firstKF.eventKeyframe.random;

            dialog.RandomEventValueLabels.SetActive(randomActive && random != 0 && random != 5);
            dialog.RandomEventValueParent.SetActive(randomActive && random != 0 && random != 5);
            dialog.RandomEventValueLabels.transform.GetChild(0).GetComponent<Text>().text = (random == 4) ? "Random Scale Min" : random == 6 ? "Minimum Range" : "Random X";
            dialog.RandomEventValueLabels.transform.GetChild(1).gameObject.SetActive(type != 2 || random == 6);
            dialog.RandomEventValueLabels.transform.GetChild(1).GetComponent<Text>().text = (random == 4) ? "Random Scale Max" : random == 6 ? "Maximum Range" : "Random Y";
            dialog.RandomIntervalField.gameObject.SetActive(random != 0 && random != 3 && random != 5);
            dialog.GameObject.transform.Find("r_label/interval").gameObject.SetActive(random != 0 && random != 3 && random != 5);

            dialog.RandomEventValueParent.transform.GetChild(1).gameObject.SetActive(type != 2 || random == 6);

            dialog.RandomEventValueParent.transform.GetChild(0).GetChild(0).AsRT().sizeDelta = new Vector2(type != 2 || random == 6 ? 117 : 317f, 32f);
            dialog.RandomEventValueParent.transform.GetChild(1).GetChild(0).AsRT().sizeDelta = new Vector2(type != 2 || random == 6 ? 117 : 317f, 32f);

            if (random != 0 && random != 3 && random != 5)
                dialog.GameObject.transform.Find("r_label/interval").GetComponent<Text>().text = random == 6 ? "Delay" : "Random Interval";

            var complexityPrefix = type switch
            {
                0 => "position",
                1 => "scale",
                2 => "rotation",
                _ => "keyframe",
            };

            for (int n = 0; n <= (type == 0 ? 5 : type == 2 ? 4 : 3); n++)
            {
                // We skip the 2nd random type for compatibility with old PA levels (for some reason).
                int buttonTmp = (n >= 2 && (type != 2 || n < 3)) ? (n + 1) : (n > 2 && type == 2) ? n + 2 : n;

                if (n >= dialog.RandomToggles.Count)
                    continue;

                var toggle = dialog.RandomToggles[n];

                if (buttonTmp == 5 || buttonTmp == 6)
                    EditorHelper.SetComplexity(toggle.gameObject,
                        EditorHelper.GetComplexity(random switch
                        {
                            5 => complexityPrefix + "/homing_type_static",
                            6 => complexityPrefix + "/homing_type_dynamic",
                            _ => string.Empty,
                        }, Complexity.Advanced));

                toggle.SetIsOnWithoutNotify(random == buttonTmp);
                toggle.onValueChanged.NewListener(_val =>
                {
                    foreach (var keyframe in selected.Select(x => x.eventKeyframe))
                        keyframe.random = buttonTmp;

                    // Since keyframe value has no affect on the timeline object, we will only need to update the physical object.
                    if (animatable is BeatmapObject beatmapObject && ObjectEditor.UpdateObjects)
                        RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.KEYFRAMES);

                    KeyframeRandomHandler(type, selected, firstKF, animatable);
                });

                EditorContextMenu.AddContextMenu(toggle.gameObject,
                    new ButtonElement("Hide Random", () =>
                    {
                        animatable.EditorData.miscDisplayValues[IntToType(type) + "/random_active"] = 0f;
                        RenderDialog(animatable);
                    }));
            }

            if (dialog.RandomAxisDropdown)
            {
                EditorHelper.SetComplexity(dialog.RandomAxisDropdown.gameObject, EditorHelper.GetComplexity(type switch
                {
                    0 => "position_keyframe/homing_axis",
                    2 => "rotation_keyframe/homing_axis",
                    _ => "keyframe/homing_axis",
                }, Complexity.Advanced), visible: () => randomActive && (random == 5 || random == 6));
                if (firstKF.eventKeyframe.randomValues.Length < 4)
                {
                    var keyframe = firstKF.eventKeyframe;
                    keyframe.SetRandomValues(keyframe.randomValues[0], keyframe.randomValues[1], keyframe.randomValues[2], 0f);
                }

                dialog.RandomAxisDropdown.SetValueWithoutNotify(Mathf.Clamp((int)firstKF.eventKeyframe.randomValues[3], 0, 3));
                dialog.RandomAxisDropdown.onValueChanged.NewListener(_val =>
                {
                    foreach (var keyframe in selected.Select(x => x.eventKeyframe))
                        keyframe.SetRandomValues(keyframe.randomValues[0], keyframe.randomValues[1], keyframe.randomValues[2], _val);
                    if (animatable is BeatmapObject beatmapObject)
                        RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.KEYFRAMES);
                });
            }

            if (dialog.FleeToggle)
            {
                EditorHelper.SetComplexity(dialog.FleeToggle.gameObject, EditorHelper.GetComplexity(type switch
                {
                    0 => "position_keyframe/homing_flee",
                    2 => "rotation_keyframe/homing_flee",
                    _ => "keyframe/homing_flee",
                }, Complexity.Advanced), visible: () => randomActive && random == 6);
                dialog.FleeToggle.toggle.SetIsOnWithoutNotify(firstKF.eventKeyframe.flee);
                dialog.FleeToggle.toggle.onValueChanged.NewListener(_val =>
                {
                    foreach (var keyframe in selected.Select(x => x.eventKeyframe))
                        keyframe.flee = _val;
                    if (animatable is BeatmapObject beatmapObject)
                        RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.KEYFRAMES);
                });
            }

            if (!dialog.RandomIntervalField)
            {
                CoreHelper.LogError($"Random Interval Field is null.");
                return;
            }

            float num = 0f;
            if (firstKF.eventKeyframe.randomValues.Length > 2)
                num = firstKF.eventKeyframe.randomValues[2];

            dialog.RandomIntervalField.SetTextWithoutNotify(num.ToString());
            dialog.RandomIntervalField.onValueChanged.NewListener(_val =>
            {
                if (float.TryParse(_val, out float num))
                {
                    foreach (var keyframe in selected.Select(x => x.eventKeyframe))
                        keyframe.randomValues[2] = num;

                    // Since keyframe value has no affect on the timeline object, we will only need to update the physical object.
                    if (animatable is BeatmapObject beatmapObject)
                        RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.KEYFRAMES);
                }
            });

            TriggerHelper.InversableField(dialog.RandomIntervalField);

            TriggerHelper.AddEventTriggers(dialog.RandomIntervalField.gameObject, TriggerHelper.ScrollDelta(dialog.RandomIntervalField, max: float.MaxValue));
        }

        void KeyframeRandomValueHandler(int type, int valueIndex, IEnumerable<TimelineKeyframe> selected, TimelineKeyframe firstKF, IAnimatable animatable)
        {
            var dialog = Dialog.KeyframeDialogs[type];
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
                    if (animatable is BeatmapObject beatmapObject)
                        RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.KEYFRAMES);
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
                    if (animatable is BeatmapObject beatmapObject)
                        RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.KEYFRAMES);
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
                    if (animatable is BeatmapObject beatmapObject)
                        RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.KEYFRAMES);
                }
            });

            if (type != 2)
            {
                TriggerHelper.AddEventTriggers(inputFieldStorage.gameObject,
                    TriggerHelper.ScrollDelta(inputFieldStorage.inputField, multi: true),
                    TriggerHelper.ScrollDeltaVector2(dialog.RandomEventValueFields[0].inputField, dialog.RandomEventValueFields[1].inputField));
            }
            else
            {
                TriggerHelper.AddEventTriggers(inputFieldStorage.gameObject,
                    TriggerHelper.ScrollDelta(inputFieldStorage.inputField, random != 6 ? 15f : 0.1f, random != 6 ? 3f : 10f, multi: true));
            }

            TriggerHelper.InversableField(inputFieldStorage);
        }

        void ColorKeyframeHandler(int valueIndex, List<Toggle> colorButtons, IEnumerable<TimelineKeyframe> selected, TimelineKeyframe firstKF, IAnimatable animatable)
        {
            bool showModifiedColors = EditorConfig.Instance.ShowModifiedColors.Value;
            var eventTime = firstKF.eventKeyframe.time;
            int index = 0;
            foreach (var toggle in colorButtons)
            {
                int tmpIndex = index;

                toggle.gameObject.SetActive((RTEditor.ShowModdedUI || tmpIndex < 9) && firstKF.eventKeyframe.values.Length > valueIndex);

                if (firstKF.eventKeyframe.values.Length <= valueIndex)
                    continue;

                toggle.SetIsOnWithoutNotify(index == firstKF.eventKeyframe.values[valueIndex]);
                toggle.onValueChanged.NewListener(_val => SetKeyframeColor(animatable, valueIndex, tmpIndex, colorButtons, selected));

                if (showModifiedColors)
                {
                    var color = CoreHelper.CurrentBeatmapTheme.GetObjColor(tmpIndex);

                    float hueNum = animatable.Interpolate(3, valueIndex + 2, eventTime);
                    float satNum = animatable.Interpolate(3, valueIndex + 3, eventTime);
                    float valNum = animatable.Interpolate(3, valueIndex + 4, eventTime);

                    toggle.image.color = RTColors.ChangeColorHSV(color, hueNum, satNum, valNum);
                }
                else
                    toggle.image.color = CoreHelper.CurrentBeatmapTheme.GetObjColor(tmpIndex);

                EditorContextMenu.AddContextMenu(toggle.gameObject,
                    getEditorElements: () => new List<EditorElement>
                    {
                        new ButtonElement("Use", () => toggle.isOn = true),
                        ButtonElement.ToggleButton("Show Modified Colors", () => EditorConfig.Instance.ShowModifiedColors.Value, () => EditorConfig.Instance.ShowModifiedColors.Value = !EditorConfig.Instance.ShowModifiedColors.Value),
                        new ButtonElement("Copy Hex Color", () => LSText.CopyToClipboard(RTColors.ColorToHexOptional(CoreHelper.CurrentBeatmapTheme.GetObjColor(tmpIndex)))),
                        new ButtonElement("Copy Modified Hex Color", () =>
                        {
                            var color = CoreHelper.CurrentBeatmapTheme.GetObjColor(tmpIndex);

                            float hueNum = animatable.Interpolate(3, valueIndex + 2, eventTime);
                            float satNum = animatable.Interpolate(3, valueIndex + 3, eventTime);
                            float valNum = animatable.Interpolate(3, valueIndex + 4, eventTime);

                            LSText.CopyToClipboard(RTColors.ColorToHexOptional(RTColors.ChangeColorHSV(color, hueNum, satNum, valNum)));
                        })
                    });

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

        void SetKeyframeColor(IAnimatable animatable, int index, int value, List<Toggle> colorButtons, IEnumerable<TimelineKeyframe> selected)
        {
            var beatmapObject = animatable as BeatmapObject;
            foreach (var keyframe in selected.Select(x => x.eventKeyframe))
            {
                keyframe.values[index] = value;
                if (!RTEditor.ShowModdedUI && beatmapObject)
                    keyframe.values[6] = 10f; // set behaviour to alpha's default if editor complexity is not set to advanced.
            }

            // Since keyframe color has no affect on the timeline object, we will only need to update the physical object.
            if (beatmapObject)
                RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.KEYFRAMES);

            int num = 0;
            foreach (var toggle in colorButtons)
            {
                int tmpIndex = num;
                toggle.SetIsOnWithoutNotify(num == value);
                toggle.onValueChanged.NewListener(_val => SetKeyframeColor(animatable, index, tmpIndex, colorButtons, selected));
                num++;
            }
        }

        void FullColorKeyframeHandler(IEnumerable<TimelineKeyframe> selected, TimelineKeyframe firstKF, IAnimatable animatable, BeatmapObject beatmapObject, Transform kfdialog)
        {
            ColorKeyframeHandler(0, startColorsReference, selected, firstKF, animatable);

            var opacityObj = kfdialog.Find("opacity").gameObject;
            var collision = kfdialog.Find("opacity/collision").GetComponent<Toggle>();
            var hsvObj = kfdialog.Find("huesatval").gameObject;
            EditorHelper.SetComplexity(kfdialog.Find("opacity_label").gameObject, "color_keyframe/opacity", Complexity.Normal);
            EditorHelper.SetComplexity(opacityObj, "color_keyframe/opacity", Complexity.Normal);
            EditorHelper.SetComplexity(collision.gameObject, "color_keyframe/opacity_collision", Complexity.Normal, visible: () => beatmapObject);

            EditorHelper.SetComplexity(kfdialog.Find("huesatval_label").gameObject, "color_keyframe/hsv", Complexity.Advanced);
            EditorHelper.SetComplexity(hsvObj, "color_keyframe/hsv", Complexity.Advanced);

            var showGradient = EditorHelper.CheckComplexity(EditorHelper.GetComplexity("beatmapobject/gradient", Complexity.Normal)) && beatmapObject && beatmapObject.gradientType != GradientType.Normal;

            kfdialog.Find("color_label").GetChild(0).GetComponent<Text>().text = showGradient ? "Start Color" : "Color";
            kfdialog.Find("opacity_label").GetChild(0).GetComponent<Text>().text = showGradient ? "Start Opacity" : "Opacity";
            kfdialog.Find("huesatval_label").GetChild(0).GetComponent<Text>().text = showGradient ? "Start Hue" : "Hue";
            kfdialog.Find("huesatval_label").GetChild(1).GetComponent<Text>().text = showGradient ? "Start Sat" : "Saturation";
            kfdialog.Find("huesatval_label").GetChild(2).GetComponent<Text>().text = showGradient ? "Start Val" : "Value";

            var gradientColorToggles = kfdialog.Find("gradient_color").gameObject;
            var gradientOpacityObj = kfdialog.Find("gradient_opacity").gameObject;
            var gradientHSVObj = kfdialog.Find("gradient_huesatval").gameObject;
            gradientColorToggles.SetActive(showGradient);
            kfdialog.Find("gradient_color").gameObject.SetActive(showGradient);
            EditorHelper.SetComplexity(kfdialog.Find("gradient_color_label").gameObject, "color_keyframe/gradient", Complexity.Normal, visible: () => showGradient);
            EditorHelper.SetComplexity(kfdialog.Find("gradient_opacity_label").gameObject, "color_keyframe/gradient_opacity", Complexity.Advanced, visible: () => showGradient);
            EditorHelper.SetComplexity(gradientOpacityObj, "color_keyframe/gradient_opacity", Complexity.Advanced, visible: () => showGradient);
            EditorHelper.SetComplexity(kfdialog.Find("gradient_huesatval_label").gameObject, "color_keyframe/gradient_hsv", Complexity.Advanced, visible: () => showGradient);
            EditorHelper.SetComplexity(gradientHSVObj, "color_keyframe/gradient_hsv", Complexity.Advanced, visible: () => showGradient);

            kfdialog.Find("color").AsRT().sizeDelta = new Vector2(366f, RTEditor.ShowModdedUI ? 78f : 32f);
            kfdialog.Find("gradient_color").AsRT().sizeDelta = new Vector2(366f, RTEditor.ShowModdedUI ? 78f : 32f);

            if (opacityObj.activeSelf)
            {
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
                        if (beatmapObject)
                            RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.KEYFRAMES);
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
                        if (beatmapObject)
                            RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.KEYFRAMES);
                    }
                });

                TriggerHelper.AddEventTriggers(kfdialog.Find("opacity").gameObject, TriggerHelper.ScrollDelta(opacity, 0.1f, 10f, 0f, 1f));

                TriggerHelper.IncreaseDecreaseButtons(opacity);

                EditorContextMenu.AddContextMenu(opacity.gameObject,
                    new ButtonElement("Reset Value", () =>
                    {
                        foreach (var keyframe in selected.Select(x => x.eventKeyframe))
                        {
                            keyframe.values[1] = 0f;
                            if (!RTEditor.ShowModdedUI)
                                keyframe.values[6] = 10f;
                        }

                        // Since keyframe value has no affect on the timeline object, we will only need to update the physical object.
                        if (beatmapObject)
                            RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.KEYFRAMES);

                        FullColorKeyframeHandler(selected, firstKF, animatable, beatmapObject, kfdialog);
                    }),
                    new ButtonElement("Set to Helper Opacity", () =>
                    {
                        foreach (var keyframe in selected.Select(x => x.eventKeyframe))
                        {
                            keyframe.values[1] = -(BeatmapObject.HELPER_OPACITY - 1f);
                            if (!RTEditor.ShowModdedUI)
                                keyframe.values[6] = 10f;
                        }

                        // Since keyframe value has no affect on the timeline object, we will only need to update the physical object.
                        if (beatmapObject)
                            RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.KEYFRAMES);

                        FullColorKeyframeHandler(selected, firstKF, animatable, beatmapObject, kfdialog);
                    }));
            }

            if (gradientColorToggles.activeSelf)
                ColorKeyframeHandler(5, endColorsReference, selected, firstKF, animatable);

            if (beatmapObject && collision.gameObject.activeSelf)
            {
                collision.SetIsOnWithoutNotify(beatmapObject.opacityCollision);
                collision.onValueChanged.NewListener(_val =>
                {
                    beatmapObject.opacityCollision = _val;
                    // Since keyframe value has no affect on the timeline object, we will only need to update the physical object.
                    if (beatmapObject)
                        RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.OBJECT_TYPE);
                });
            }

            if (gradientOpacityObj.activeSelf)
            {
                var gradientOpacity = kfdialog.Find("gradient_opacity/x").GetComponent<InputField>();

                gradientOpacity.SetTextWithoutNotify((-firstKF.eventKeyframe.values[6] + 1).ToString());
                gradientOpacity.onValueChanged.NewListener(_val =>
                {
                    if (float.TryParse(_val, out float n))
                    {
                        foreach (var keyframe in selected.Select(x => x.eventKeyframe))
                            keyframe.values[6] = Mathf.Clamp(-n + 1, 0f, 1f);

                        // Since keyframe value has no affect on the timeline object, we will only need to update the physical object.
                        if (beatmapObject)
                            RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.KEYFRAMES);
                    }
                });
                gradientOpacity.onEndEdit.NewListener(_val =>
                {
                    if (RTMath.TryParse(_val, (-firstKF.eventKeyframe.values[6] + 1), out float n))
                    {
                        foreach (var keyframe in selected.Select(x => x.eventKeyframe))
                            keyframe.values[6] = Mathf.Clamp(-n + 1, 0f, 1f);

                        // Since keyframe value has no affect on the timeline object, we will only need to update the physical object.
                        if (beatmapObject)
                            RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.KEYFRAMES);
                    }
                });

                TriggerHelper.AddEventTriggers(kfdialog.Find("gradient_opacity").gameObject, TriggerHelper.ScrollDelta(gradientOpacity, 0.1f, 10f, 0f, 1f));

                TriggerHelper.IncreaseDecreaseButtons(gradientOpacity);

                EditorContextMenu.AddContextMenu(gradientOpacity.gameObject,
                    new ButtonElement("Reset Value", () =>
                    {
                        foreach (var keyframe in selected.Select(x => x.eventKeyframe))
                            keyframe.values[6] = 0f;

                        // Since keyframe value has no affect on the timeline object, we will only need to update the physical object.
                        if (beatmapObject)
                            RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.KEYFRAMES);

                        FullColorKeyframeHandler(selected, firstKF, animatable, beatmapObject, kfdialog);
                    }),
                    new ButtonElement("Set to Helper Opacity", () =>
                    {
                        foreach (var keyframe in selected.Select(x => x.eventKeyframe))
                            keyframe.values[6] = -(BeatmapObject.HELPER_OPACITY - 1f);

                        // Since keyframe value has no affect on the timeline object, we will only need to update the physical object.
                        if (beatmapObject)
                            RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.KEYFRAMES);

                        FullColorKeyframeHandler(selected, firstKF, animatable, beatmapObject, kfdialog);
                    }));
            }

            // Start
            if (hsvObj.activeSelf)
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
                        if (beatmapObject)
                            RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.KEYFRAMES);
                    }
                    ColorKeyframeHandler(0, startColorsReference, selected, firstKF, animatable);
                });
                hue.onEndEdit.NewListener(_val =>
                {
                    if (RTMath.TryParse(_val, firstKF.eventKeyframe.values[2], out float n))
                    {
                        foreach (var keyframe in selected.Select(x => x.eventKeyframe))
                            keyframe.values[2] = n;

                        // Since keyframe value has no affect on the timeline object, we will only need to update the physical object.
                        if (beatmapObject)
                            RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.KEYFRAMES);
                    }
                    ColorKeyframeHandler(0, startColorsReference, selected, firstKF, animatable);
                });

                CoreHelper.Destroy(kfdialog.transform.Find("huesatval").GetComponent<EventTrigger>());

                TriggerHelper.AddEventTriggers(hue.gameObject, TriggerHelper.ScrollDelta(hue));
                TriggerHelper.IncreaseDecreaseButtons(hue);

                EditorContextMenu.AddContextMenu(hue.gameObject,
                    new ButtonElement("Reset Value", () =>
                    {
                        foreach (var keyframe in selected.Select(x => x.eventKeyframe))
                            keyframe.values[2] = 0f;

                        // Since keyframe value has no affect on the timeline object, we will only need to update the physical object.
                        if (beatmapObject)
                            RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.KEYFRAMES);
                        FullColorKeyframeHandler(selected, firstKF, animatable, beatmapObject, kfdialog);
                    }),
                    new ButtonElement("Match End Hue", () =>
                    {
                        if (!beatmapObject || beatmapObject.gradientType == GradientType.Normal)
                        {
                            EditorManager.inst.DisplayNotification("Object is not a gradient!", 2f, EditorManager.NotificationType.Warning);
                            return;
                        }

                        foreach (var keyframe in selected.Select(x => x.eventKeyframe))
                            keyframe.values[2] = keyframe.values[7];

                        // Since keyframe value has no affect on the timeline object, we will only need to update the physical object.
                        if (beatmapObject)
                            RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.KEYFRAMES);
                        FullColorKeyframeHandler(selected, firstKF, animatable, beatmapObject, kfdialog);
                    }));

                var sat = kfdialog.Find("huesatval/y").GetComponent<InputField>();

                sat.SetTextWithoutNotify(firstKF.eventKeyframe.values[3].ToString());
                sat.onValueChanged.NewListener(_val =>
                {
                    if (float.TryParse(_val, out float n))
                    {
                        foreach (var keyframe in selected.Select(x => x.eventKeyframe))
                            keyframe.values[3] = n;

                        // Since keyframe value has no affect on the timeline object, we will only need to update the physical object.
                        if (beatmapObject)
                            RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.KEYFRAMES);
                    }
                    ColorKeyframeHandler(0, startColorsReference, selected, firstKF, animatable);
                });
                sat.onEndEdit.NewListener(_val =>
                {
                    if (RTMath.TryParse(_val, firstKF.eventKeyframe.values[3], out float n))
                    {
                        foreach (var keyframe in selected.Select(x => x.eventKeyframe))
                            keyframe.values[3] = n;

                        // Since keyframe value has no affect on the timeline object, we will only need to update the physical object.
                        if (beatmapObject)
                            RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.KEYFRAMES);
                    }
                    ColorKeyframeHandler(0, startColorsReference, selected, firstKF, animatable);
                });

                TriggerHelper.AddEventTriggers(sat.gameObject, TriggerHelper.ScrollDelta(sat));
                TriggerHelper.IncreaseDecreaseButtons(sat);

                EditorContextMenu.AddContextMenu(sat.gameObject,
                    new ButtonElement("Reset Value", () =>
                    {
                        foreach (var keyframe in selected.Select(x => x.eventKeyframe))
                            keyframe.values[3] = 0f;

                        // Since keyframe value has no affect on the timeline object, we will only need to update the physical object.
                        if (beatmapObject)
                            RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.KEYFRAMES);
                        FullColorKeyframeHandler(selected, firstKF, animatable, beatmapObject, kfdialog);
                    }),
                    new ButtonElement("Match End Sat", () =>
                    {
                        if (!beatmapObject || beatmapObject.gradientType == GradientType.Normal)
                        {
                            EditorManager.inst.DisplayNotification("Object is not a gradient!", 2f, EditorManager.NotificationType.Warning);
                            return;
                        }

                        foreach (var keyframe in selected.Select(x => x.eventKeyframe))
                            keyframe.values[3] = keyframe.values[8];

                        // Since keyframe value has no affect on the timeline object, we will only need to update the physical object.
                        if (beatmapObject)
                            RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.KEYFRAMES);
                        FullColorKeyframeHandler(selected, firstKF, animatable, beatmapObject, kfdialog);
                    }));

                var val = kfdialog.Find("huesatval/z").GetComponent<InputField>();

                val.SetTextWithoutNotify(firstKF.eventKeyframe.values[4].ToString());
                val.onValueChanged.NewListener(_val =>
                {
                    if (float.TryParse(_val, out float n))
                    {
                        foreach (var keyframe in selected.Select(x => x.eventKeyframe))
                            keyframe.values[4] = n;

                        // Since keyframe value has no affect on the timeline object, we will only need to update the physical object.
                        if (beatmapObject)
                            RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.KEYFRAMES);
                    }
                    ColorKeyframeHandler(0, startColorsReference, selected, firstKF, animatable);
                });
                val.onEndEdit.NewListener(_val =>
                {
                    if (RTMath.TryParse(_val, firstKF.eventKeyframe.values[4], out float n))
                    {
                        foreach (var keyframe in selected.Select(x => x.eventKeyframe))
                            keyframe.values[4] = n;

                        // Since keyframe value has no affect on the timeline object, we will only need to update the physical object.
                        if (beatmapObject)
                            RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.KEYFRAMES);
                    }
                    ColorKeyframeHandler(0, startColorsReference, selected, firstKF, animatable);
                });

                TriggerHelper.AddEventTriggers(val.gameObject, TriggerHelper.ScrollDelta(val));
                TriggerHelper.IncreaseDecreaseButtons(val);

                EditorContextMenu.AddContextMenu(val.gameObject,
                    new ButtonElement("Reset Value", () =>
                    {
                        foreach (var keyframe in selected.Select(x => x.eventKeyframe))
                            keyframe.values[4] = 0f;

                        // Since keyframe value has no affect on the timeline object, we will only need to update the physical object.
                        if (beatmapObject)
                            RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.KEYFRAMES);
                        FullColorKeyframeHandler(selected, firstKF, animatable, beatmapObject, kfdialog);
                    }),
                    new ButtonElement("Match End Val", () =>
                    {
                        if (!beatmapObject || beatmapObject.gradientType == GradientType.Normal)
                        {
                            EditorManager.inst.DisplayNotification("Object is not a gradient!", 2f, EditorManager.NotificationType.Warning);
                            return;
                        }

                        foreach (var keyframe in selected.Select(x => x.eventKeyframe))
                            keyframe.values[4] = keyframe.values[9];

                        // Since keyframe value has no affect on the timeline object, we will only need to update the physical object.
                        if (beatmapObject)
                            RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.KEYFRAMES);
                        FullColorKeyframeHandler(selected, firstKF, animatable, beatmapObject, kfdialog);
                    }));
            }

            // End
            if (gradientHSVObj.activeSelf && endColorsReference != null)
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
                        if (beatmapObject)
                            RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.KEYFRAMES);
                    }
                    ColorKeyframeHandler(5, endColorsReference, selected, firstKF, animatable);
                });
                hue.onEndEdit.NewListener(_val =>
                {
                    if (RTMath.TryParse(_val, firstKF.eventKeyframe.values[7], out float n))
                    {
                        foreach (var keyframe in selected.Select(x => x.eventKeyframe))
                            keyframe.values[7] = n;

                        // Since keyframe value has no affect on the timeline object, we will only need to update the physical object.
                        if (beatmapObject)
                            RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.KEYFRAMES);
                    }
                    ColorKeyframeHandler(5, endColorsReference, selected, firstKF, animatable);
                });

                CoreHelper.Destroy(kfdialog.transform.Find("gradient_huesatval").GetComponent<EventTrigger>());

                TriggerHelper.AddEventTriggers(hue.gameObject, TriggerHelper.ScrollDelta(hue));
                TriggerHelper.IncreaseDecreaseButtons(hue);

                EditorContextMenu.AddContextMenu(hue.gameObject,
                    new ButtonElement("Reset Value", () =>
                    {
                        foreach (var keyframe in selected.Select(x => x.eventKeyframe))
                            keyframe.values[7] = 0f;

                        // Since keyframe value has no affect on the timeline object, we will only need to update the physical object.
                        if (beatmapObject)
                            RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.KEYFRAMES);
                        FullColorKeyframeHandler(selected, firstKF, animatable, beatmapObject, kfdialog);
                    }),
                    new ButtonElement("Match Start Hue", () =>
                    {
                        foreach (var keyframe in selected.Select(x => x.eventKeyframe))
                            keyframe.values[7] = keyframe.values[2];

                        // Since keyframe value has no affect on the timeline object, we will only need to update the physical object.
                        if (beatmapObject)
                            RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.KEYFRAMES);
                        FullColorKeyframeHandler(selected, firstKF, animatable, beatmapObject, kfdialog);
                    }));

                var sat = kfdialog.Find("gradient_huesatval/y").GetComponent<InputField>();

                sat.SetTextWithoutNotify(firstKF.eventKeyframe.values[8].ToString());
                sat.onValueChanged.NewListener(_val =>
                {
                    if (float.TryParse(_val, out float n))
                    {
                        foreach (var keyframe in selected.Select(x => x.eventKeyframe))
                            keyframe.values[8] = n;

                        // Since keyframe value has no affect on the timeline object, we will only need to update the physical object.
                        if (beatmapObject)
                            RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.KEYFRAMES);
                    }
                    ColorKeyframeHandler(5, endColorsReference, selected, firstKF, animatable);
                });
                sat.onEndEdit.NewListener(_val =>
                {
                    if (RTMath.TryParse(_val, firstKF.eventKeyframe.values[8], out float n))
                    {
                        foreach (var keyframe in selected.Select(x => x.eventKeyframe))
                            keyframe.values[8] = n;

                        // Since keyframe value has no affect on the timeline object, we will only need to update the physical object.
                        if (beatmapObject)
                            RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.KEYFRAMES);
                    }
                    ColorKeyframeHandler(5, endColorsReference, selected, firstKF, animatable);
                });

                TriggerHelper.AddEventTriggers(sat.gameObject, TriggerHelper.ScrollDelta(sat));
                TriggerHelper.IncreaseDecreaseButtons(sat);

                EditorContextMenu.AddContextMenu(sat.gameObject,
                    new ButtonElement("Reset Value", () =>
                    {
                        foreach (var keyframe in selected.Select(x => x.eventKeyframe))
                            keyframe.values[8] = 0f;

                        // Since keyframe value has no affect on the timeline object, we will only need to update the physical object.
                        if (beatmapObject)
                            RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.KEYFRAMES);
                        FullColorKeyframeHandler(selected, firstKF, animatable, beatmapObject, kfdialog);
                    }),
                    new ButtonElement("Match Start Sat", () =>
                    {
                        foreach (var keyframe in selected.Select(x => x.eventKeyframe))
                            keyframe.values[8] = keyframe.values[3];

                        // Since keyframe value has no affect on the timeline object, we will only need to update the physical object.
                        if (beatmapObject)
                            RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.KEYFRAMES);
                        FullColorKeyframeHandler(selected, firstKF, animatable, beatmapObject, kfdialog);
                    }));

                var val = kfdialog.Find("gradient_huesatval/z").GetComponent<InputField>();

                val.SetTextWithoutNotify(firstKF.eventKeyframe.values[9].ToString());
                val.onValueChanged.NewListener(_val =>
                {
                    if (float.TryParse(_val, out float n))
                    {
                        foreach (var keyframe in selected.Select(x => x.eventKeyframe))
                            keyframe.values[9] = n;

                        // Since keyframe value has no affect on the timeline object, we will only need to update the physical object.
                        if (beatmapObject)
                            RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.KEYFRAMES);
                    }
                    ColorKeyframeHandler(5, endColorsReference, selected, firstKF, animatable);
                });
                val.onEndEdit.NewListener(_val =>
                {
                    if (RTMath.TryParse(_val, firstKF.eventKeyframe.values[9], out float n))
                    {
                        foreach (var keyframe in selected.Select(x => x.eventKeyframe))
                            keyframe.values[9] = n;

                        // Since keyframe value has no affect on the timeline object, we will only need to update the physical object.
                        if (beatmapObject)
                            RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.KEYFRAMES);
                    }
                    ColorKeyframeHandler(5, endColorsReference, selected, firstKF, animatable);
                });

                TriggerHelper.AddEventTriggers(val.gameObject, TriggerHelper.ScrollDelta(val));
                TriggerHelper.IncreaseDecreaseButtons(val);

                EditorContextMenu.AddContextMenu(val.gameObject,
                    new ButtonElement("Reset Value", () =>
                    {
                        foreach (var keyframe in selected.Select(x => x.eventKeyframe))
                            keyframe.values[9] = 0f;

                        // Since keyframe value has no affect on the timeline object, we will only need to update the physical object.
                        if (beatmapObject)
                            RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.KEYFRAMES);
                        FullColorKeyframeHandler(selected, firstKF, animatable, beatmapObject, kfdialog);
                    }),
                    new ButtonElement("Match Start Sat", () =>
                    {
                        foreach (var keyframe in selected.Select(x => x.eventKeyframe))
                            keyframe.values[9] = keyframe.values[4];

                        // Since keyframe value has no affect on the timeline object, we will only need to update the physical object.
                        if (beatmapObject)
                            RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.KEYFRAMES);
                        FullColorKeyframeHandler(selected, firstKF, animatable, beatmapObject, kfdialog);
                    }));
            }
        }

        void RenderRelative(int type, IEnumerable<TimelineKeyframe> selected, TimelineKeyframe firstKF, IAnimatable animatable, BeatmapObject beatmapObject, KeyframeDialog dialog)
        {
            if (!dialog.RelativeToggle)
                return;

            var relativeToggleComplexity = EditorHelper.GetComplexity(type switch
            {
                0 => "position_keyframe/relative",
                1 => "scale_keyframe/relative",
                2 => "rotation_keyframe/relative",
                _ => "keyframe/relative"
            }, type switch
            {
                2 => Complexity.Normal,
                _ => Complexity.Advanced,
            });
            EditorHelper.SetComplexity(dialog.RelativeToggle.transform.GetPreviousSibling()?.gameObject, relativeToggleComplexity,
                visible: () => !animatable.EditorData.miscDisplayValues.TryGetValue(IntToType(type) + "/relative_active", out float val) || val == 1f);
            EditorHelper.SetComplexity(dialog.RelativeToggle.gameObject, relativeToggleComplexity,
                visible: () => !animatable.EditorData.miscDisplayValues.TryGetValue(IntToType(type) + "/relative_active", out float val) || val == 1f);
            dialog.RelativeToggle.Interactable = !animatable.EditorData.miscDisplayValues.TryGetValue(IntToType(type) + "/lock_relative", out float relativeLock) || relativeLock != 1f;
            dialog.RelativeToggle.SetIsOnWithoutNotify(firstKF.eventKeyframe.relative);
            dialog.RelativeToggle.OnValueChanged.NewListener(_val =>
            {
                if (animatable.EditorData.miscDisplayValues.TryGetValue(IntToType(type) + "/force_relative", out float forceRelative))
                    _val = forceRelative switch
                    {
                        1f => true,
                        _ => false,
                    };

                foreach (var keyframe in selected.Select(x => x.eventKeyframe))
                    keyframe.relative = _val;

                // Since keyframe value has no affect on the timeline object, we will only need to update the physical object.
                if (beatmapObject)
                    RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.KEYFRAMES);
            });

            EditorContextMenu.AddContextMenu(dialog.RelativeToggle.gameObject,
                new ButtonElement("Hide Relative Toggle", () =>
                {
                    animatable.EditorData.miscDisplayValues[IntToType(type) + "/relative_active"] = 0f;
                    RenderDialog(animatable);
                }),
                new ButtonElement(dialog.RelativeToggle.Interactable ? "Lock Relative Toggle" : "Unlock Relative Toggle", () =>
                {
                    animatable.EditorData.miscDisplayValues[IntToType(type) + "/lock_relative"] = dialog.RelativeToggle.Interactable ? 1f : 0f;
                    RenderRelative(type, selected, firstKF, animatable, beatmapObject, dialog);
                }),
                new ButtonElement("Force Relative On", () =>
                {
                    animatable.EditorData.miscDisplayValues[IntToType(type) + "/force_relative"] = 1f;
                    RenderRelative(type, selected, firstKF, animatable, beatmapObject, dialog);
                }),
                new ButtonElement("Force Relative Off", () =>
                {
                    animatable.EditorData.miscDisplayValues[IntToType(type) + "/force_relative"] = 0f;
                    RenderRelative(type, selected, firstKF, animatable, beatmapObject, dialog);
                }),
                new ButtonElement("Don't Force Relative", () =>
                {
                    animatable.EditorData.miscDisplayValues.Remove(IntToType(type) + "/force_relative");
                    RenderRelative(type, selected, firstKF, animatable, beatmapObject, dialog);
                }, shouldGenerate: () => animatable.EditorData.miscDisplayValues.ContainsKey(IntToType(type) + "/force_relative")),
                new SpacerElement(),
                new ButtonElement("Remove Relative Settings", () =>
                {
                    animatable.EditorData.miscDisplayValues.Remove(IntToType(type) + "/relative_active");
                    animatable.EditorData.miscDisplayValues.Remove(IntToType(type) + "/lock_relative");
                    animatable.EditorData.miscDisplayValues.Remove(IntToType(type) + "/force_relative");
                    RenderRelative(type, selected, firstKF, animatable, beatmapObject, dialog);
                }));
        }

        List<TimelineMarker> timelineMarkers = new List<TimelineMarker>();
        bool renderedMarkers;

        public List<Marker> GetMarkers(IAnimatable animatable)
        {
            if (animatable is PAAnimation animation)
                return animation.markers;
            return GameData.Current.data.markers;
        }

        public void RenderMarkers(IAnimatable animatable)
        {
            if (renderedMarkers)
            {
                LSHelpers.DeleteChildren(Markers);
                timelineMarkers.Clear();
                renderedMarkers = false;
            }

            if (!EditorConfig.Instance.ShowMarkersInObjectEditor.Value)
                return;

            var beatmapObject = animatable as BeatmapObject;
            var markers = GetMarkers(animatable);
            var length = beatmapObject ? beatmapObject.GetObjectLifeLength(ObjEditor.inst.ObjectLengthOffset) : 0f;
            for (int i = 0; i < markers.Count; i++)
            {
                var marker = markers[i];
                if (beatmapObject && (marker.time < beatmapObject.StartTime || marker.time > beatmapObject.StartTime + length))
                    continue;

                var timelineMarker = new TimelineMarker();
                timelineMarker.Marker = marker;
                int index = i;

                var gameObject = MarkerEditor.inst.markerPrefab.Duplicate(Markers, $"Marker {index}");

                timelineMarker.Index = index;
                timelineMarker.GameObject = gameObject;
                timelineMarker.RectTransform = gameObject.transform.AsRT();
                timelineMarker.Handle = gameObject.GetComponent<Image>();
                timelineMarker.Line = gameObject.transform.Find("line").GetComponent<Image>();
                timelineMarker.Text = gameObject.GetComponentInChildren<Text>();
                timelineMarker.HoverTooltip = gameObject.GetComponent<HoverTooltip>();

                var markerColor = timelineMarker.Color;

                timelineMarker.GameObject.SetActive(true);
                timelineMarker.RenderPosition(marker.time - (beatmapObject ? beatmapObject.StartTime : 0f), ObjEditor.inst.Zoom * 14f, 0f);
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

        public void RenderMarkerPositions(IAnimatable animatable)
        {
            if (!renderedMarkers)
                return;

            var beatmapObject = animatable as BeatmapObject;
            for (int i = 0; i < timelineMarkers.Count; i++)
                timelineMarkers[i].RenderPosition(timelineMarkers[i].Marker.time - (beatmapObject ? beatmapObject.StartTime : 0f), ObjEditor.inst.Zoom * 14f, 0f);
        }

        public void SetCursorColor(Color color)
        {
            if (cursorHandle)
                cursorHandle.color = color;
            if (cursorRuler)
                cursorRuler.color = color;
        }

        #endregion

        #region Render

        public GameObject keyframeEnd;

        public static bool AllowTimeExactlyAtStart => false;
        public void ResizeKeyframeTimeline(IAnimatable animatable)
        {
            CurrentTimeline = this;
            CurrentObject = animatable;

            // ObjEditor.inst.ObjectLengthOffset is the offset from the last keyframe. Could allow for more timeline space.
            float objectLifeLength = animatable.AnimLength;
            float x = TimeTimelineCalc(objectLifeLength + ObjEditor.inst.ObjectLengthOffset);

            Content.AsRT().sizeDelta = new Vector2(x, 0f);
            TimelineGrid.AsRT().sizeDelta = new Vector2(x, 122f);

            // Whether the value should clamp at 0.001 over StartTime or not.
            Cursor.minValue = AllowTimeExactlyAtStart ? animatable.StartTime : animatable.StartTime + 0.001f;
            Cursor.maxValue = animatable.StartTime + objectLifeLength + ObjEditor.inst.ObjectLengthOffset;

            if (!keyframeEnd)
            {
                TimelineGrid.DeleteChildren();
                keyframeEnd = ObjEditor.inst.KeyframeEndPrefab.Duplicate(TimelineGrid, "end keyframe");
            }

            var rectTransform = keyframeEnd.transform.AsRT();
            rectTransform.sizeDelta = new Vector2(4f, 122f);
            rectTransform.anchoredPosition = new Vector2(objectLifeLength * ObjEditor.inst.Zoom * 14f, 0f);
        }

        public void UpdateRenderedKeyframes(IAnimatable animatable) => RenderedKeyframes = new List<TimelineKeyframe>(animatable.TimelineKeyframes);

        public void ClearKeyframes()
        {
            RenderedKeyframes.ForLoop(timelineKeyframe => CoreHelper.Delete(timelineKeyframe.GameObject));
            RenderedKeyframes.Clear();
        }

        public TimelineKeyframe GetKeyframe(IAnimatable animatable, int type, int index)
        {
            var kf = animatable.TimelineKeyframes.Find(x => x.Type == type && x.Index == index);

            var events = animatable.GetEventKeyframes(type);
            if (!kf)
                kf = animatable.TimelineKeyframes.Find(x => x.ID == events[index].id);

            if (!kf)
            {
                kf = CreateKeyframe(animatable, type, index);
                animatable.TimelineKeyframes.Add(kf);
            }

            if (!kf.GameObject)
                kf.Init(true);

            return kf;
        }

        public void CreateKeyframes(IAnimatable animatable)
        {
            CurrentTimeline = this;
            CurrentObject = animatable;

            ClearKeyframes();

            var events = animatable.Events;
            for (int i = 0; i < events.Count; i++)
            {
                if (events[i].Count <= 0)
                    return;

                for (int j = 0; j < events[i].Count; j++)
                {
                    var keyframe = events[i][j];
                    var timelineKeyframe = animatable.TimelineKeyframes.Find(x => x.ID == keyframe.id);
                    if (!timelineKeyframe)
                    {
                        timelineKeyframe = CreateKeyframe(animatable, i, j);
                        animatable.TimelineKeyframes.Add(timelineKeyframe);
                    }

                    if (!timelineKeyframe.GameObject)
                        timelineKeyframe.Init();

                    timelineKeyframe.Render();
                }
            }
        }

        public TimelineKeyframe CreateKeyframe(IAnimatable animatable, int type, int index)
        {
            var eventKeyframe = animatable.GetEventKeyframes(type)[index];

            var timelineKeyframe = new TimelineKeyframe(eventKeyframe, animatable, this)
            {
                Type = type,
                Index = index,
            };

            eventKeyframe.timelineKeyframe = timelineKeyframe;
            timelineKeyframe.Init();

            return timelineKeyframe;
        }

        public void RenderKeyframes(IAnimatable animatable)
        {
            CurrentTimeline = this;
            if (animatable != CurrentObject)
            {
                RenderedKeyframes.ForLoop(timelineKeyframe => CoreHelper.Delete(timelineKeyframe.GameObject));
                RenderedKeyframes.Clear();
            }

            CurrentObject = animatable;

            var events = animatable.Events;
            for (int i = 0; i < events.Count; i++)
            {
                for (int j = 0; j < events[i].Count; j++)
                {
                    var timelineKeyframe = GetKeyframe(animatable, i, j);

                    timelineKeyframe.Render();
                }
            }

            if (animatable.TimelineKeyframes.Count > 0 && animatable.TimelineKeyframes.Where(x => x.Selected).Count() == 0)
            {
                if (EditorConfig.Instance.RememberLastKeyframeType.Value && animatable.TimelineKeyframes.TryFind(x => x.Type == currentKeyframeType, out TimelineKeyframe kf))
                    kf.Selected = true;
                else
                    animatable.TimelineKeyframes[0].Selected = true;
            }

            if (animatable.TimelineKeyframes.Count >= 1000)
                AchievementManager.inst.UnlockAchievement("holy_keyframes");

            UpdateRenderedKeyframes(animatable);
        }

        public void RenderKeyframe(IAnimatable animatable, TimelineKeyframe timelineKeyframe)
        {
            var events = animatable.GetEventKeyframes(timelineKeyframe.Type);
            if (events.TryFindIndex(x => x.id == timelineKeyframe.ID, out int kfIndex))
                timelineKeyframe.Index = kfIndex;

            var eventKeyframe = timelineKeyframe.eventKeyframe;
            timelineKeyframe.RenderSprite(events);
            timelineKeyframe.RenderPos();
            timelineKeyframe.RenderIcons();
        }

        public void UpdateKeyframeOrder(IAnimatable animatable)
        {
            animatable.SortKeyframes();
            RenderKeyframes(animatable);
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

        #endregion
    }
}
