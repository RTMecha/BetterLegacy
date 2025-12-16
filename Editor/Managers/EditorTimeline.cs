using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using UnityEngine;
using UnityEngine.UI;

using LSFunctions;

using CielaSpike;

using BetterLegacy.Companion.Data.Parameters;
using BetterLegacy.Companion.Entity;
using BetterLegacy.Configs;
using BetterLegacy.Core;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using BetterLegacy.Core.Runtime;
using BetterLegacy.Editor.Components;
using BetterLegacy.Editor.Data;
using BetterLegacy.Editor.Data.Dialogs;
using BetterLegacy.Editor.Data.Timeline;
using BetterLegacy.Editor.Managers.Settings;

using ObjectType = BetterLegacy.Core.Data.Beatmap.BeatmapObject.ObjectType;

namespace BetterLegacy.Editor.Managers
{
    /// <summary>
    /// Manages the main timeline.
    /// </summary>
    public class EditorTimeline : BaseManager<EditorTimeline, RTEditorSettings>, IEditorLayerUI
    {
        #region Init

        public override void OnInit()
        {
            timelineScrollRects.AddRange(EditorManager.inst.timelineScrollRect.GetComponents<ScrollRect>());
            timelineScrollRects.Add(EditorManager.inst.markerTimeline.transform.parent.GetComponent<ScrollRect>());
            timelineScrollRects.Add(EditorManager.inst.timelineSlider.transform.parent.GetComponent<ScrollRect>());
        }

        public override void OnTick()
        {
            startOffsetDisplay.color = RTColors.FadeColor(startOffsetDisplay.color, offsetOpacity);
            endOffsetDisplay.color = RTColors.FadeColor(endOffsetDisplay.color, offsetOpacity);

            if (Input.GetMouseButtonUp((int)UnityEngine.EventSystems.PointerEventData.InputButton.Middle))
                movingTimeline = false;

            if (!movingTimeline)
                return;

            var vector = Input.mousePosition * CoreHelper.ScreenScaleInverse;
            //float multiply = 12f / EditorManager.inst.Zoom;
            //float multiply = AudioManager.inst.CurrentAudioSource.clip.length / 10f / EditorManager.inst.Zoom;
            float multiply = (EditorManager.inst.zoomFloat * 1000f) / AudioManager.inst.CurrentAudioSource.clip.length / (EditorManager.inst.zoomFloat * 10f);
            SetTimelinePosition(cachedTimelinePos.x + -(((vector.x - EditorManager.inst.DragStartPos.x) / Screen.width) * multiply));
            SetBinScroll(Mathf.Clamp(cachedTimelinePos.y + ((vector.y - EditorManager.inst.DragStartPos.y) / Screen.height), 0f, 1f));
        }

        #endregion

        #region Timeline

        /// <summary>
        /// If the timeline is being dragged.
        /// </summary>
        public bool movingTimeline;

        /// <summary>
        /// The timeline cursor.
        /// </summary>
        public Slider timelineSlider;

        /// <summary>
        /// The handle of the timeline cursor.
        /// </summary>
        public Image timelineSliderHandle;

        /// <summary>
        /// The ruler of the timeline cursor.
        /// </summary>
        public Image timelineSliderRuler;

        /// <summary>
        /// If the mouse cursor is over the timeline.
        /// </summary>
        public bool isOverMainTimeline;

        /// <summary>
        /// If the user is changing the level time.
        /// </summary>
        public bool changingTime;

        /// <summary>
        /// The new time to set.
        /// </summary>
        public float newTime;

        /// <summary>
        /// Entire timeline parent.
        /// </summary>
        public Transform wholeTimeline;

        Vector2 cachedTimelinePos;

        List<ScrollRect> timelineScrollRects = new List<ScrollRect>();

        /// <summary>
        /// UI for displaying <see cref="LevelData.LevelStartOffset"/>.
        /// </summary>
        public Image startOffsetDisplay;

        /// <summary>
        /// UI for displaying <see cref="LevelData.LevelEndOffset"/>.
        /// </summary>
        public Image endOffsetDisplay;

        /// <summary>
        /// Opacity for <see cref="startOffsetDisplay"/> and <see cref="endOffsetDisplay"/>.
        /// </summary>
        public float offsetOpacity = 0.3f;

        /// <summary>
        /// Initializes timeline features.
        /// </summary>
        public void SetupTimeline()
        {
            var startOffsetDisplay = Creator.NewUIObject("Start", wholeTimeline.Find("Timeline/Panel 1"));
            RectValues.LeftAnchored.AnchoredPosition(4f, 0f).AnchorMin(0f, 0f).Pivot(0f, 0.5f).SizeDelta(0f, 0f).AssignToRectTransform(startOffsetDisplay.transform.AsRT());
            this.startOffsetDisplay = startOffsetDisplay.gameObject.AddComponent<Image>();
            EditorThemeManager.ApplyGraphic(this.startOffsetDisplay, ThemeGroup.Light_Text, true, roundedSide: SpriteHelper.RoundedSide.Right);

            var endOffsetDisplay = Creator.NewUIObject("End", wholeTimeline.Find("Timeline/Panel 1"));
            RectValues.LeftAnchored.AnchoredPosition(-4f, 0f).AnchorMax(1f, 1f).AnchorMin(1f, 0f).Pivot(1f, 0.5f).SizeDelta(0f, 0f).AssignToRectTransform(endOffsetDisplay.transform.AsRT());
            this.endOffsetDisplay = endOffsetDisplay.gameObject.AddComponent<Image>();
            EditorThemeManager.ApplyGraphic(this.endOffsetDisplay, ThemeGroup.Light_Text, true, roundedSide: SpriteHelper.RoundedSide.Right);
        }

        /// <summary>
        /// Renders the timeline.
        /// </summary>
        public void RenderTimeline()
        {
            if (layerType == LayerType.Events)
                RTEventEditor.inst.RenderTimelineKeyframes();
            else
                RenderTimelineObjectsPositions();

            RTCheckpointEditor.inst.RenderCheckpoints();
            RTMarkerEditor.inst.RenderMarkers();

            UpdateTimelineSizes();

            SetTimelineGridSize();
        }

        /// <summary>
        /// Sets the main timeline position.
        /// </summary>
        /// <param name="position">The position to set the timeline scroll.</param>
        public void SetTimelinePosition(float position) => SetTimeline(EditorManager.inst.zoomFloat, position);

        /// <summary>
        /// Sets the main timeline zoom.
        /// </summary>
        /// <param name="zoom">The zoom to set to the timeline.</param>
        public void SetTimelineZoom(float zoom)
        {
            float position = 0f;
            if (EditorConfig.Instance.UseMouseAsZoomPoint.Value)
                position = (float)((double)GetTimelineTime(false) / AudioManager.inst.CurrentAudioSource.clip.length);
                //position = Mathf.Clamp((float)((double)GetTimelineTime(false) / AudioManager.inst.CurrentAudioSource.clip.length), 0.05f, 0.95f);
            else if (AudioManager.inst.CurrentAudioSource.clip)
                position = (float)((double)AudioManager.inst.CurrentAudioSource.time / AudioManager.inst.CurrentAudioSource.clip.length);
                //position = Mathf.Clamp((float)((double)AudioManager.inst.CurrentAudioSource.time / AudioManager.inst.CurrentAudioSource.clip.length), 0.05f, 0.95f);

            SetTimeline(zoom, position);
        }

        /// <summary>
        /// Sets the main timeline zoom and position.
        /// </summary>
        /// <param name="zoom">The amount to zoom in.</param>
        /// <param name="position">The position to set the timeline scroll. If the value is less that 0, it will automatically calculate the position to match the audio time.</param>
        /// <param name="render">If the timeline should render.</param>
        public void SetTimeline(float zoom, float position, bool render = true)
        {
            try
            {
                float prevZoom = EditorManager.inst.zoomFloat;
                EditorManager.inst.zoomFloat = Mathf.Clamp01(zoom);
                EditorManager.inst.zoomVal =
                    LSMath.InterpolateOverCurve(EditorManager.inst.ZoomCurve, EditorManager.inst.zoomBounds.x, EditorManager.inst.zoomBounds.y, EditorManager.inst.zoomFloat);

                if (render)
                    RenderTimeline();

                EditorManager.inst.timelineScrollRectBar.onValueChanged.NewListener(SetTimelineScroll);
                CoroutineHelper.PerformAtNextFrame(() => EditorManager.inst.timelineScrollRectBar.value = position); // wtf why does setting the position on the current frame cause it to be inaccurate...

                EditorManager.inst.zoomSlider.SetValueWithoutNotify(EditorManager.inst.zoomFloat);
                EditorManager.inst.zoomSlider.onValueChanged.NewListener(_val => EditorManager.inst.Zoom = _val);

                //CoreHelper.Log($"Zoom: {zoom}\nPosition: {position}");
            }
            catch (Exception ex)
            {
                CoreHelper.LogError($"Had an error with setting zoom. Exception: {ex}");
            }
        }

        void SetTimelineScroll(float scroll) => timelineScrollRects.ForLoop(x => x.horizontalNormalizedPosition = scroll);

        /// <summary>
        /// Calculates the timeline time the mouse cursor is at.
        /// </summary>
        /// <param name="snap">If the return value should be snapped to the BPM.</param>
        /// <returns>Returns a calculated timeline time.</returns>
        public float GetTimelineTime(bool snap)
        {
            float num = Input.mousePosition.x;
            num += Mathf.Abs(EditorManager.inst.timeline.transform.AsRT().position.x);

            return snap && !Input.GetKey(KeyCode.LeftAlt) ?
                RTEditor.SnapToBPM(num * EditorManager.inst.ScreenScaleInverse / EditorManager.inst.Zoom) :
                num * EditorManager.inst.ScreenScaleInverse / EditorManager.inst.Zoom;
        }

        /// <summary>
        /// Calculates the timeline time the mouse cursor is at.
        /// </summary>
        /// <returns>Returns a calculated timeline time.</returns>
        public float GetTimelineTime()
        {
            float num = Input.mousePosition.x;
            num += Mathf.Abs(EditorManager.inst.timeline.transform.AsRT().position.x);

            return RTEditor.inst.editorInfo.bpmSnapActive && !Input.GetKey(KeyCode.LeftAlt) ?
                RTEditor.SnapToBPM(num * EditorManager.inst.ScreenScaleInverse / EditorManager.inst.Zoom) :
                num * EditorManager.inst.ScreenScaleInverse / EditorManager.inst.Zoom;
        }

        /// <summary>
        /// Updates the timeline cursor colors.
        /// </summary>
        public void UpdateTimelineColors()
        {
            timelineSliderHandle.color = EditorConfig.Instance.TimelineCursorColor.Value;
            timelineSliderRuler.color = EditorConfig.Instance.TimelineCursorColor.Value;
        }

        /// <summary>
        /// Updates the time cursor.
        /// </summary>
        public void UpdateTimeChange()
        {
            if (!changingTime && EditorConfig.Instance.DraggingMainCursorFix.Value)
            {
                newTime = Mathf.Clamp(AudioManager.inst.CurrentAudioSource.time, 0f, AudioManager.inst.CurrentAudioSource.clip.length) * EditorManager.inst.Zoom;
                timelineSlider.value = newTime;
            }
            else if (EditorConfig.Instance.DraggingMainCursorFix.Value)
            {
                newTime = timelineSlider.value / EditorManager.inst.Zoom;
                AudioManager.inst.SetMusicTime(Mathf.Clamp(timelineSlider.value / EditorManager.inst.Zoom, 0f, AudioManager.inst.CurrentAudioSource.clip.length));
            }
        }

        /// <summary>
        /// Starts dragging the timeline.
        /// </summary>
        public void StartTimelineDrag()
        {
            cachedTimelinePos = new Vector2(EditorManager.inst.timelineScrollRectBar.value, binSlider.value);
            movingTimeline = true;
        }

        /// <summary>
        /// Prevents the timeline from being navigated outside the normal range.
        /// </summary>
        /// <param name="clamp">If the timeline should clamp.</param>
        public void ClampTimeline(bool clamp)
        {
            var movementType = clamp ? ScrollRect.MovementType.Clamped : ScrollRect.MovementType.Unrestricted;
            for (int i = 0; i < timelineScrollRects.Count; i++)
                timelineScrollRects[i].movementType = movementType;
        }

        /// <summary>
        /// Updates the timeline size.
        /// </summary>
        public void UpdateTimelineSizes()
        {
            if (AudioManager.inst.CurrentAudioSource.clip)
                SetTimelineSizes(AudioManager.inst.CurrentAudioSource.clip.length * EditorManager.inst.Zoom);
        }

        /// <summary>
        /// Sets the timeline size.
        /// </summary>
        /// <param name="size">Size to set.</param>
        public void SetTimelineSizes(float size)
        {
            EditorManager.inst.markerTimeline.transform.AsRT().SetSizeDeltaX(size);
            EditorManager.inst.timeline.transform.AsRT().SetSizeDeltaX(size);
            EditorManager.inst.timelineWaveformOverlay.transform.AsRT().SetSizeDeltaX(size);

            var startOffset = GameData.Current?.data?.level?.LevelStartOffset ?? 0f;
            var endOffset = GameData.Current?.data?.level?.LevelEndOffset ?? 0f;
            startOffsetDisplay.rectTransform.sizeDelta = new Vector2(startOffset * EditorManager.inst.Zoom, 0f);
            endOffsetDisplay.rectTransform.sizeDelta = new Vector2(endOffset * EditorManager.inst.Zoom, 0f);
        }

        #endregion

        #region Timeline Objects

        /// <summary>
        /// The singular currently selected object.
        /// </summary>
        public TimelineObject CurrentSelection { get; set; } = new TimelineObject(null);

        public List<TimelineObject> SelectedObjects => timelineObjects.FindAll(x => x.Selected);
        public List<TimelineObject> SelectedBeatmapObjects => TimelineBeatmapObjects.FindAll(x => x.Selected);
        public List<TimelineObject> SelectedPrefabObjects => TimelinePrefabObjects.FindAll(x => x.Selected);
        public List<TimelineObject> SelectedBackgroundObjects => TimelineBackgroundObjects.FindAll(x => x.Selected);

        public int SelectedObjectCount => SelectedObjects.Count;

        public RectTransform timelineObjectsParent;

        /// <summary>
        /// Function to run when the user selects a timeline object using the picker.
        /// </summary>
        public Action<TimelineObject> onSelectTimelineObject;

        /// <summary>
        /// The list of all timeline objects, excluding event keyframes.
        /// </summary>
        public List<TimelineObject> timelineObjects = new List<TimelineObject>();

        /// <summary>
        /// The list of timeline keyframes.
        /// </summary>
        public List<TimelineKeyframe> timelineKeyframes = new List<TimelineKeyframe>();

        /// <summary>
        /// All timeline objects that are <see cref="BeatmapObject"/>.
        /// </summary>
        public List<TimelineObject> TimelineBeatmapObjects => timelineObjects.Where(x => x.isBeatmapObject).ToList();

        /// <summary>
        /// All timeline objects that are <see cref="PrefabObject"/>.
        /// </summary>
        public List<TimelineObject> TimelinePrefabObjects => timelineObjects.Where(x => x.isPrefabObject).ToList();
        
        /// <summary>
        /// All timeline objects that are <see cref="BackgroundObject"/>.
        /// </summary>
        public List<TimelineObject> TimelineBackgroundObjects => timelineObjects.Where(x => x.isBackgroundObject).ToList();

        /// <summary>
        /// Selects a group of objects based on drag selection.
        /// </summary>
        /// <param name="add">If selection should be added to.</param>
        public IEnumerator GroupSelectObjects(bool add, bool remove)
        {
            if (!add && !remove)
                DeselectAllObjects();

            var list = timelineObjects;
            list.Where(x => x.Layer == Layer && RTMath.RectTransformToScreenSpace(EditorManager.inst.SelectionBoxImage.rectTransform)
            .Overlaps(RTMath.RectTransformToScreenSpace(x.Image.rectTransform))).ToList().ForEach(timelineObject =>
            {
                timelineObject.Selected = !remove;
                timelineObject.timeOffset = 0f;
                timelineObject.binOffset = 0;
            });

            var selectedObjects = SelectedObjects;
            HandleSelection(selectedObjects);
            EditorManager.inst.DisplayNotification($"Selection includes {selectedObjects.Count} objects!", 1f, EditorManager.NotificationType.Success);
            yield break;
        }

        /// <summary>
        /// Handles multiple timeline objects in a list. If the list is empty, select the first checkpoint.
        /// </summary>
        /// <param name="selectedObjects">Selected objects.</param>
        /// <param name="index">Index to select.</param>
        /// <param name="multi">If multi selection should be considered.</param>
        public void HandleSelection(List<TimelineObject> selectedObjects, int index = 0, bool multi = true)
        {
            var selectedCount = selectedObjects.Count;

            if (selectedCount <= 0)
            {
                RTCheckpointEditor.inst.SetCurrentCheckpoint(0);
                return;
            }

            if (multi && selectedCount > 1)
            {
                MultiObjectEditor.inst.Dialog.Open();
                return;
            }

            if (!multi && selectedCount > 0 || selectedCount == 1)
                SetCurrentObject(selectedObjects.GetAt(index));
        }

        /// <summary>
        /// Deselects all timeline objects.
        /// </summary>
        /// <param name="closeDialog">If the current dialog should be closed.</param>
        public void DeselectAllObjects(bool closeDialog = true)
        {
            if (closeDialog)
                EditorDialog.CurrentDialog?.Close();
            foreach (var timelineObject in SelectedObjects)
                timelineObject.Selected = false;
        }

        /// <summary>
        /// Multi selects a timeline object.
        /// </summary>
        /// <param name="timelineObject">Timeline object to select.</param>
        public void AddSelectedObject(TimelineObject timelineObject)
        {
            if (SelectedObjectCount + 1 > 1)
            {
                var first = SelectedObjects[0];
                timelineObject.Selected = !timelineObject.Selected;
                int selectedCount = SelectedObjectCount;
                if (selectedCount == 0 || selectedCount == 1)
                {
                    SetCurrentObject(selectedCount == 1 ? SelectedObjects[0] : first);
                    return;
                }

                MultiObjectEditor.inst.Dialog.Open();

                RenderTimelineObject(timelineObject);

                return;
            }

            SetCurrentObject(timelineObject);
        }

        /// <summary>
        /// Sets the currently selected timeline object.
        /// </summary>
        /// <param name="timelineObject">Timeline object to select.</param>
        /// <param name="bringTo">If audio, layer and bin position should be brought to this object.</param>
        /// <param name="openDialog">If the dialog should be opened.</param>
        public void SetCurrentObject(TimelineObject timelineObject, bool bringTo = false, bool openDialog = true)
        {
            if (!timelineObject.verified && !timelineObjects.Has(x => x.ID == timelineObject.ID))
                RenderTimelineObject(timelineObject);

            if (CurrentSelection.isBeatmapObject && CurrentSelection.ID != timelineObject.ID)
                for (int i = 0; i < ObjEditor.inst.TimelineParents.Count; i++)
                    LSHelpers.DeleteChildren(ObjEditor.inst.TimelineParents[i]);

            DeselectAllObjects(false);

            timelineObject.Selected = true;
            CurrentSelection = timelineObject;

            if (!string.IsNullOrEmpty(timelineObject.ID) && openDialog)
            {
                if (timelineObject.isBeatmapObject)
                    ObjectEditor.inst.OpenDialog(timelineObject.GetData<BeatmapObject>());
                if (timelineObject.isPrefabObject)
                    RTPrefabEditor.inst.OpenPrefabObjectDialog(timelineObject.GetData<PrefabObject>());
                if (timelineObject.isBackgroundObject)
                    RTBackgroundEditor.inst.OpenDialog(timelineObject.GetData<BackgroundObject>());
            }

            if (bringTo)
            {
                AudioManager.inst.SetMusicTime(timelineObject.Time);
                SetLayer(timelineObject.Layer, LayerType.Objects);
                SetBinPosition(timelineObject.Bin);
            }
        }

        /// <summary>
        /// Deletes all selected timeline objects.
        /// </summary>
        public void DeleteObjects()
        {
            var list = SelectedObjects;
            var count = list.Count;
            int minIndex = timelineObjects.ToIndexer().Where(x => x.obj && x.obj.Selected).Min(x => x.index) - 1;
            var objectIDs = list.Where(x => x.isBeatmapObject).Select(x => x.ID);
            var prefabIDs = list.Where(x => x.isPrefabObject).Select(x => x.ID);
            var bgIDs = list.Where(x => x.isBackgroundObject).Select(x => x.ID);

            EditorDialog.CurrentDialog?.Close();
            CoreHelper.Log($"Deleting count: {count}\nSelect index: {minIndex}");

            list.ForLoopReverse(x => DeleteObject(x, false, false, false, false));

            GameData.Current.beatmapObjects.RemoveAll(x => objectIDs.Contains(x.id));
            GameData.Current.prefabObjects.RemoveAll(x => prefabIDs.Contains(x.id));
            GameData.Current.backgroundObjects.RemoveAll(x => bgIDs.Contains(x.id));

            foreach (var beatmapObject in GameData.Current.beatmapObjects)
            {
                if (objectIDs.Contains(beatmapObject.Parent))
                {
                    beatmapObject.Parent = string.Empty;

                    beatmapObject.GetParentRuntime()?.UpdateObject(beatmapObject, ObjectContext.PARENT_CHAIN);
                }
            }

            RTLevel.Current?.RecalculateObjectStates();

            HandleSelection(timelineObjects, minIndex, false);

            if (RandomHelper.PercentChance(ExampleConfig.Instance.DeleteObjectNoticeChance.Value))
                Example.Current?.chatBubble?.SayDialogue(ExampleChatBubble.Dialogues.DELETE_OBJECT, new TimelineObjectsDialogueParameters(list));

            EditorManager.inst.DisplayNotification($"Deleted {count} objects!", 1f, EditorManager.NotificationType.Success);
        }

        /// <summary>
        /// Deletes a timeline object.
        /// </summary>
        /// <param name="timelineObject">Timeline object to delete.</param>
        /// <param name="recalculate">If the object engine should recalculate.</param>
        /// <param name="select">If an earlier object should be selected.</param>
        /// <param name="update">If objects parented to this object should be updated.</param>
        public void DeleteObject(TimelineObject timelineObject, bool recalculate = true, bool select = true, bool update = true, bool remove = true)
        {
            int index = 0;
            if (select)
                index = timelineObjects.IndexOf(timelineObject) - 1;

            if (timelineObject.isBeatmapObject)
            {
                var beatmapObject = timelineObject.GetData<BeatmapObject>();

                if (RTPrefabEditor.inst.quickPrefabTarget && RTPrefabEditor.inst.quickPrefabTarget.id == beatmapObject.id)
                    RTPrefabEditor.inst.quickPrefabTarget = null;

                for (int i = 0; i < beatmapObject.modifiers.Count; i++)
                {
                    var modifier = beatmapObject.modifiers[i];
                    try
                    {
                        modifier.RunInactive(modifier, beatmapObject); // for cases where we want to clear data.
                    }
                    catch (Exception ex)
                    {
                        CoreHelper.LogException(ex);
                    } // allow further objects to be deleted if a modifiers' inactive state throws an error
                }

                if (remove)
                    GameData.Current.beatmapObjects.Remove(x => x.id == beatmapObject.id);

                beatmapObject.GetParentRuntime()?.UpdateObject(beatmapObject, reinsert: false, recursive: false, recalculate: false);

                if (update)
                {
                    foreach (var other in GameData.Current.beatmapObjects)
                    {
                        if (other.Parent == beatmapObject.id)
                        {
                            other.Parent = string.Empty;

                            other.GetParentRuntime()?.UpdateObject(other, ObjectContext.PARENT_CHAIN);
                        }
                    }
                }
            }
            if (timelineObject.isPrefabObject)
            {
                var prefabObject = timelineObject.GetData<PrefabObject>();

                if (remove)
                    GameData.Current.prefabObjects.Remove(x => x.id == prefabObject.id);
                prefabObject.GetParentRuntime()?.UpdatePrefab(prefabObject, false, false);
            }
            if (timelineObject.isBackgroundObject)
            {
                var backgroundObject = timelineObject.GetData<BackgroundObject>();

                if (remove)
                    GameData.Current.backgroundObjects.Remove(x => x.id == backgroundObject.id);
                backgroundObject.GetParentRuntime()?.UpdateBackgroundObject(backgroundObject, false);
                if (RTBackgroundEditor.inst.Dialog.IsCurrent)
                    RTBackgroundEditor.inst.UpdateBackgroundList();
            }

            CoreHelper.Delete(timelineObject.GameObject);
            timelineObjects.Remove(timelineObject);

            if (recalculate)
                RTLevel.Current?.RecalculateObjectStates();

            if (!select)
                return;

            HandleSelection(timelineObjects, index, false);
        }

        /// <summary>
        /// Removes and destroys the timeline object.
        /// </summary>
        /// <param name="timelineObject">Timeline object to remove.</param>
        public void RemoveTimelineObject(TimelineObject timelineObject)
        {
            if (!timelineObject)
                return;

            CoreHelper.Delete(timelineObject.GameObject);
            if (timelineObjects.TryFindIndex(x => x.ID == timelineObject.ID, out int index))
                timelineObjects.RemoveAt(index);
        }

        /// <summary>
        /// Gets a keyframes' sprite based on easing type.
        /// </summary>
        /// <param name="a">The keyframes' own easing.</param>
        /// <param name="b">The next keyframes' easing.</param>
        /// <returns>Returns a sprite based on the animation curve.</returns>
        public static Sprite GetKeyframeIcon(Easing a, Easing b)
            => ObjEditor.inst.KeyframeSprites[a.ToString().Contains("Out") && b.ToString().Contains("In") ? 3 : a.ToString().Contains("Out") ? 2 : b.ToString().Contains("In") ? 1 : 0];

        void UpdateTimelineObjects()
        {
            CoroutineHelper.ProcessLoop(timelineObjects, timelineObject =>
            {
                timelineObject.RenderVisibleState();
                if (timelineObject.IsCurrentLayer)
                    timelineObject.RenderPosLength();
            }, 2000);

            if (CurrentSelection && CurrentSelection.isBeatmapObject && CurrentSelection.GetData<BeatmapObject>().TimelineKeyframes.Count > 0)
                for (int i = 0; i < CurrentSelection.GetData<BeatmapObject>().TimelineKeyframes.Count; i++)
                    CurrentSelection.GetData<BeatmapObject>().TimelineKeyframes[i].RenderVisibleState();

            for (int i = 0; i < timelineKeyframes.Count; i++)
                timelineKeyframes[i].RenderVisibleState();

            for (int i = 0; i < RTMarkerEditor.inst.timelineMarkers.Count; i++)
                RTMarkerEditor.inst.timelineMarkers[i].Render();
        }

        /// <summary>
        /// Gets the timeline object.
        /// </summary>
        /// <param name="editable">Editable Object to get a timeline object from.</param>
        /// <returns>Returns either the related TimelineObject or a new TimelineObject if one doesn't exist for whatever reason.</returns>
        public TimelineObject GetTimelineObject(IEditable editable)
        {
            if (editable is IPrefabable prefabable)
            {
                if (prefabable.FromPrefab && prefabable.TryGetPrefabObject(out PrefabObject prefabObject) && prefabObject.timelineObject)
                    return prefabObject.timelineObject;
                else if (prefabable.FromPrefab)
                    return null;
            }

            if (!editable.TimelineObject)
                editable.TimelineObject = new TimelineObject(editable);

            return editable.TimelineObject;
        }

        public void RenderTimelineObject(TimelineObject timelineObject)
        {
            if (!timelineObject.GameObject)
            {
                timelineObject.AddToList();
                timelineObject.Init(false);
            }

            timelineObject.Render();
        }

        public void RenderTimelineObjects()
        {
            foreach (var timelineObject in timelineObjects)
                RenderTimelineObject(timelineObject);
        }

        public void RenderTimelineObjectsPositions()
        {
            for (int i = 0; i < timelineObjects.Count; i++)
            {
                var timelineObject = timelineObjects[i];
                if (timelineObject.IsCurrentLayer)
                    timelineObject.RenderPosLength();
            }
        }

        public void ClearTimelineObjects()
        {
            if (timelineObjects.Count > 0)
                timelineObjects.ForEach(x => Destroy(x.GameObject));
            timelineObjects.Clear();
        }

        public void InitTimelineObjects()
        {
            ClearTimelineObjects();
            timelineObjects = ToTimelineObjects().ToList();

            //CoroutineHelper.StartCoroutine(IInitTimelineObjects());
        }

        IEnumerator IInitTimelineObjects()
        {
            ClearTimelineObjects();

            if (!GameData.Current)
                yield break;

            var editables = GameData.Current.GetEditablesList();
            int num = 0;
            CoroutineHelper.ProcessLoop(editables, editable =>
            {
                if (!editable.CanRenderInTimeline)
                {
                    num++;
                    return;
                }

                TimelineObject timelineObject = null;

                try
                {
                    timelineObject = GetTimelineObject(editable);
                    timelineObject.verified = true;
                    timelineObject.Init(false);
                    timelineObject.Render();
                    onTimelineObjectCreated?.Invoke(timelineObject, num, editables.Count);
                }
                catch (Exception e)
                {
                    var stringBuilder = new StringBuilder();
                    stringBuilder.AppendLine($"{RTLevel.className}Failed to convert object '{editable.ID}' to {nameof(TimelineObject)}.");
                    stringBuilder.AppendLine($"Exception: {e.Message}");
                    stringBuilder.AppendLine(e.StackTrace);

                    Debug.LogError(stringBuilder.ToString());
                }

                if (timelineObject)
                    timelineObjects.Add(timelineObject);
                num++;
            }, 2000);
            //foreach (var editable in editables)
            //{
            //    if (!editable.CanRenderInTimeline)
            //    {
            //        num++;
            //        continue;
            //    }

            //    TimelineObject timelineObject = null;

            //    try
            //    {
            //        timelineObject = GetTimelineObject(editable);
            //        timelineObject.verified = true;
            //        timelineObject.Init(false);
            //        timelineObject.Render();
            //        onTimelineObjectCreated?.Invoke(timelineObject, num, editables.Count);
            //    }
            //    catch (Exception e)
            //    {
            //        var stringBuilder = new StringBuilder();
            //        stringBuilder.AppendLine($"{RTLevel.className}Failed to convert object '{editable.ID}' to {nameof(TimelineObject)}.");
            //        stringBuilder.AppendLine($"Exception: {e.Message}");
            //        stringBuilder.AppendLine(e.StackTrace);

            //        Debug.LogError(stringBuilder.ToString());
            //    }

            //    if (timelineObject)
            //        timelineObjects.Add(timelineObject);
            //    num++;
            //}

            yield break;
        }

        public Action<TimelineObject, int, int> onTimelineObjectCreated;
        public IEnumerable<TimelineObject> ToTimelineObjects()
        {
            if (!GameData.Current)
                yield break;

            var editables = GameData.Current.GetEditablesList();
            int num = 0;
            foreach (var editable in editables)
            {
                if (!editable.CanRenderInTimeline)
                {
                    num++;
                    continue;
                }

                TimelineObject timelineObject = null;

                try
                {
                    timelineObject = GetTimelineObject(editable);
                    timelineObject.verified = true;
                    timelineObject.Init(false);
                    timelineObject.Render();
                    onTimelineObjectCreated?.Invoke(timelineObject, num, editables.Count);
                }
                catch (Exception e)
                {
                    var stringBuilder = new StringBuilder();
                    stringBuilder.AppendLine($"{RTLevel.className}Failed to convert object '{editable.ID}' to {nameof(TimelineObject)}.");
                    stringBuilder.AppendLine($"Exception: {e.Message}");
                    stringBuilder.AppendLine(e.StackTrace);

                    Debug.LogError(stringBuilder.ToString());
                }

                if (timelineObject)
                    yield return timelineObject;
                num++;
            }
        }

        public void CreateTimelineObjects()
        {
            if (timelineObjects.Count > 0)
                timelineObjects.ForEach(x => Destroy(x.GameObject));
            timelineObjects.Clear();

            if (!GameData.Current)
                return;

            for (int i = 0; i < GameData.Current.beatmapObjects.Count; i++)
            {
                var beatmapObject = GameData.Current.beatmapObjects[i];
                if (string.IsNullOrEmpty(beatmapObject.id) || beatmapObject.fromPrefab)
                    continue;

                var timelineObject = GetTimelineObject(beatmapObject);
                timelineObject.AddToList(true);
                timelineObject.Init();
            }

            for (int i = 0; i < GameData.Current.backgroundObjects.Count; i++)
            {
                var backgroundObject = GameData.Current.backgroundObjects[i];
                if (string.IsNullOrEmpty(backgroundObject.id) || backgroundObject.fromPrefab)
                    continue;

                var timelineObject = GetTimelineObject(backgroundObject);
                timelineObject.AddToList(true);
                timelineObject.Init();
            }

            for (int i = 0; i < GameData.Current.prefabObjects.Count; i++)
            {
                var prefabObject = GameData.Current.prefabObjects[i];
                if (string.IsNullOrEmpty(prefabObject.id) || prefabObject.fromModifier || prefabObject.fromPrefab)
                    continue;

                var timelineObject = GetTimelineObject(prefabObject);
                timelineObject.AddToList(true);
                timelineObject.Init();
            }
        }

        public Sprite GetObjectTypeSprite(ObjectType objectType)
            => objectType == ObjectType.Helper ? ObjEditor.inst.HelperSprite :
            objectType == ObjectType.Decoration ? ObjEditor.inst.DecorationSprite :
            objectType == ObjectType.Empty ? ObjEditor.inst.EmptySprite : null;

        public Image.Type GetObjectTypePattern(ObjectType objectType)
            => objectType == ObjectType.Helper || objectType == ObjectType.Decoration || objectType == ObjectType.Empty ? Image.Type.Tiled : Image.Type.Simple;

        public void UpdateTransformIndex()
        {
            if (!GameData.Current)
                return;

            int siblingIndex = 0;
            for (int i = 0; i < GameData.Current.beatmapObjects.Count; i++)
            {
                var beatmapObject = GameData.Current.beatmapObjects[i];
                if (beatmapObject.fromPrefab)
                    continue;
                var timelineObject = GetTimelineObject(beatmapObject);
                if (!timelineObject || !timelineObject.GameObject)
                    continue;
                timelineObject.GameObject.transform.SetSiblingIndex(siblingIndex);
                siblingIndex++;
            }

            for (int i = 0; i < GameData.Current.backgroundObjects.Count; i++)
            {
                var backgroundObject = GameData.Current.backgroundObjects[i];
                if (backgroundObject.fromPrefab)
                    continue;
                var timelineObject = GetTimelineObject(backgroundObject);
                if (!timelineObject || !timelineObject.GameObject)
                    continue;
                timelineObject.GameObject.transform.SetSiblingIndex(siblingIndex);
                siblingIndex++;
            }

            for (int i = 0; i < GameData.Current.prefabObjects.Count; i++)
            {
                var prefabObject = GameData.Current.prefabObjects[i];
                if (prefabObject.fromModifier || prefabObject.fromPrefab)
                    continue;
                var timelineObject = GetTimelineObject(prefabObject);
                if (!timelineObject || !timelineObject.GameObject)
                    continue;
                timelineObject.GameObject.transform.SetSiblingIndex(siblingIndex);
                siblingIndex++;
            }
        }

        /// <summary>
        /// Handles object selecting and picking.
        /// </summary>
        /// <param name="timelineObject">Timeline object to select.</param>
        public void SelectObject(TimelineObject timelineObject)
        {
            if (!timelineObject)
                return;

            var prefabable = timelineObject.GetData<IPrefabable>();
            if (prefabable != null && prefabable.FromPrefab && prefabable.TryGetPrefabObject(out PrefabObject result) && result.fromModifier)
                return;

            //if (timelineObject.isBeatmapObject)
            //{
            //    var beatmapObject = timelineObject.GetData<BeatmapObject>();
            //    if (beatmapObject.fromPrefab && beatmapObject.TryGetPrefabObject(out PrefabObject result) && result.fromModifier)
            //        return;
            //}
            
            //if (timelineObject.isBackgroundObject)
            //{
            //    var backgroundObject = timelineObject.GetData<BackgroundObject>();
            //    if (backgroundObject.fromPrefab && backgroundObject.TryGetPrefabObject(out PrefabObject result) && result.fromModifier)
            //        return;
            //}
            
            //if (timelineObject.isPrefabObject)
            //{
            //    var prefabObject = timelineObject.GetData<PrefabObject>();
            //    if (prefabObject.fromPrefab && prefabObject.TryGetPrefabObject(out PrefabObject result) && result.fromModifier)
            //        return;
            //}

            if (onSelectTimelineObject != null)
            {
                onSelectTimelineObject(timelineObject);
                onSelectTimelineObject = null;
                return;
            }

            // select object if picker is not currently active.
            if (!RTEditor.inst.parentPickerEnabled && !RTEditor.inst.prefabPickerEnabled)
            {
                if (InputDataManager.inst.editorActions.MultiSelect.IsPressed)
                    AddSelectedObject(timelineObject);
                else
                    SetCurrentObject(timelineObject);

                return;
            }

            var currentSelection = CurrentSelection;
            var selectedObjects = SelectedObjects;

            // assigns the Beatmap Objects' prefab reference to selected objects.
            if (RTEditor.inst.prefabPickerEnabled && !timelineObject.isPrefabObject && prefabable != null)
            {
                if (string.IsNullOrEmpty(prefabable.PrefabInstanceID))
                {
                    EditorManager.inst.DisplayNotification("Object is not assigned to a prefab!", 2f, EditorManager.NotificationType.Error);
                    return;
                }

                if (RTEditor.inst.selectingMultiple)
                {
                    foreach (var otherTimelineObject in SelectedObjects)
                    {
                        otherTimelineObject.AsPrefabable()?.SetPrefabReference(prefabable);
                        RenderTimelineObject(otherTimelineObject);
                    }
                }
                else if (CurrentSelection.TryGetPrefabable(out IPrefabable currentPrefabable))
                {
                    currentPrefabable.SetPrefabReference(prefabable);
                    RenderTimelineObject(CurrentSelection);
                    if (CurrentSelection.isBeatmapObject)
                        ObjectEditor.inst.OpenDialog(CurrentSelection.GetData<BeatmapObject>());
                    if (CurrentSelection.isBackgroundObject)
                        RTBackgroundEditor.inst.OpenDialog(CurrentSelection.GetData<BackgroundObject>());
                    if (CurrentSelection.isPrefabObject)
                        RTPrefabEditor.inst.OpenPrefabObjectDialog(CurrentSelection.GetData<PrefabObject>());
                }

                RTEditor.inst.prefabPickerEnabled = false;

                return;
            }

            // assigns the Prefab Objects' prefab reference to selected objects.
            if (RTEditor.inst.prefabPickerEnabled && timelineObject.isPrefabObject)
            {
                var prefabObject = timelineObject.GetData<PrefabObject>();
                var prefabInstanceID = LSText.randomString(16);

                if (RTEditor.inst.selectingMultiple)
                {
                    foreach (var otherTimelineObject in SelectedObjects.Where(x => x.isBeatmapObject))
                    {
                        if (otherTimelineObject.TryGetPrefabable(out IPrefabable otherPrefabable))
                        {
                            otherPrefabable.PrefabID = prefabObject.prefabID;
                            otherPrefabable.PrefabInstanceID = prefabInstanceID;
                            RenderTimelineObject(otherTimelineObject);
                        }
                    }
                }
                else if (CurrentSelection.TryGetPrefabable(out IPrefabable currentPrefabable))
                {
                    currentPrefabable.PrefabID = prefabObject.prefabID;
                    currentPrefabable.PrefabInstanceID = prefabInstanceID;
                    RenderTimelineObject(CurrentSelection);
                    if (CurrentSelection.isBeatmapObject)
                        ObjectEditor.inst.OpenDialog(CurrentSelection.GetData<BeatmapObject>());
                    if (CurrentSelection.isBackgroundObject)
                        RTBackgroundEditor.inst.OpenDialog(CurrentSelection.GetData<BackgroundObject>());
                    if (CurrentSelection.isPrefabObject)
                        RTPrefabEditor.inst.OpenPrefabObjectDialog(CurrentSelection.GetData<PrefabObject>());
                }

                RTEditor.inst.prefabPickerEnabled = false;

                return;
            }

            // assigns the selected objects' parent.
            if (RTEditor.inst.parentPickerEnabled && timelineObject.isBeatmapObject)
            {
                if (RTEditor.inst.selectingMultiple)
                {
                    bool success = false;
                    foreach (var otherTimelineObject in SelectedObjects)
                    {
                        if (otherTimelineObject.isPrefabObject)
                        {
                            var prefabObject = otherTimelineObject.GetData<PrefabObject>();
                            prefabObject.Parent = timelineObject.ID;
                            prefabObject.GetParentRuntime()?.UpdatePrefab(prefabObject, PrefabObjectContext.PARENT, false);
                            RTPrefabEditor.inst.RenderPrefabObjectDialog(prefabObject);

                            success = true;
                            continue;
                        }
                        if (otherTimelineObject.isBeatmapObject)
                            success = otherTimelineObject.GetData<BeatmapObject>().TrySetParent(timelineObject.GetData<BeatmapObject>());
                    }
                    RTLevel.Current?.RecalculateObjectStates();

                    if (!success)
                        EditorManager.inst.DisplayNotification("Cannot set parent to child / self!", 1f, EditorManager.NotificationType.Warning);
                    else
                        RTEditor.inst.parentPickerEnabled = false;

                    return;
                }

                if (CurrentSelection.isPrefabObject)
                {
                    var prefabObject = CurrentSelection.GetData<PrefabObject>();
                    prefabObject.Parent = timelineObject.ID;
                    prefabObject.GetParentRuntime()?.UpdatePrefab(prefabObject, PrefabObjectContext.PARENT);
                    RTPrefabEditor.inst.RenderPrefabObjectDialog(prefabObject);
                    RTEditor.inst.parentPickerEnabled = false;

                    return;
                }

                var tryParent = CurrentSelection.GetData<BeatmapObject>().TrySetParent(timelineObject.GetData<BeatmapObject>());

                if (!tryParent)
                    EditorManager.inst.DisplayNotification("Cannot set parent to child / self!", 1f, EditorManager.NotificationType.Warning);
                else
                    RTEditor.inst.parentPickerEnabled = false;
            }
        }

        #endregion

        #region Timeline Textures

        public Image timelineImage;
        public Image timelineOverlayImage;
        public GridRenderer timelineGridRenderer;
        Coroutine assignTimelineTextureCoroutine;
        bool stopCoroutine;

        /// <summary>
        /// Updates the timelines' waveform texture.
        /// </summary>
        /// <param name="clip">The audio clip to create a waveform texture from.</param>
        /// <param name="forceReload">If the waveform should re-render regardless of user settings.</param>
        public IEnumerator AssignTimelineTexture(AudioClip clip, bool forceReload = false)
        {
            CoreHelper.Log(
                $"Clip: {clip}\n" +
                $"Has Loaded Level: {EditorManager.inst.hasLoadedLevel}");

            var config = EditorConfig.Instance;
            var path = RTFile.CombinePaths(RTFile.BasePath, $"waveform-{config.WaveformMode.Value.ToString().ToLower()}{FileFormat.PNG.Dot()}");
            var settingsPath = RTFile.CombinePaths(RTFile.ApplicationDirectory, $"settings/waveform-{config.WaveformMode.Value.ToString().ToLower()}{FileFormat.PNG.Dot()}");

            SetTimelineSprite(null);

            if (assignTimelineTextureCoroutine != null)
            {
                CoroutineHelper.StopCoroutine(assignTimelineTextureCoroutine);
                stopCoroutine = true;
                assignTimelineTextureCoroutine = null;
            }

            if (forceReload || config.WaveformRerender.Value || (!EditorManager.inst.hasLoadedLevel && !EditorLevelManager.inst.loadingLevel && !RTFile.FileExists(settingsPath) || !RTFile.FileExists(path)))
            {
                int num = Mathf.Clamp((int)clip.length * 48, 100, 15000);
                Texture2D waveform = null;

                assignTimelineTextureCoroutine = config.WaveformMode.Value switch
                {
                    WaveformType.Split => CoroutineHelper.StartCoroutineAsync(Legacy(clip, num, 300, config.WaveformBGColor.Value, config.WaveformTopColor.Value, config.WaveformBottomColor.Value, _tex => waveform = _tex)),
                    WaveformType.Centered => CoroutineHelper.StartCoroutineAsync(Beta(clip, num, 300, config.WaveformBGColor.Value, config.WaveformTopColor.Value, _tex => waveform = _tex)),
                    WaveformType.Bottom => CoroutineHelper.StartCoroutineAsync(Modern(clip, num, 300, config.WaveformBGColor.Value, config.WaveformTopColor.Value, _tex => waveform = _tex)),
                    WaveformType.SplitDetailed => CoroutineHelper.StartCoroutineAsync(LegacyFast(clip, num, 300, config.WaveformBGColor.Value, config.WaveformTopColor.Value, config.WaveformBottomColor.Value, _tex => waveform = _tex)),
                    WaveformType.CenteredDetailed => CoroutineHelper.StartCoroutineAsync(BetaFast(clip, num, 300, config.WaveformBGColor.Value, config.WaveformTopColor.Value, _tex => waveform = _tex)),
                    WaveformType.BottomDetailed => CoroutineHelper.StartCoroutineAsync(ModernFast(clip, num, 300, config.WaveformBGColor.Value, config.WaveformTopColor.Value, _tex => waveform = _tex)),
                    _ => null,
                };
                yield return assignTimelineTextureCoroutine;
                assignTimelineTextureCoroutine = null;
                if (stopCoroutine)
                {
                    stopCoroutine = false;
                    yield break;
                }    

                SetTimelineSprite(Sprite.Create(waveform, new Rect(0f, 0f, num, 300f), new Vector2(0.5f, 0.5f), 100f));

                if (config.WaveformSaves.Value)
                    CoroutineHelper.StartCoroutineAsync(SaveWaveform());
            }
            else
            {
                assignTimelineTextureCoroutine = CoroutineHelper.StartCoroutineAsync(AlephNetwork.DownloadImageTexture("file://" + (!EditorManager.inst.hasLoadedLevel && !EditorLevelManager.inst.loadingLevel ?
                settingsPath : path), texture2D => SetTimelineSprite(SpriteHelper.CreateSprite(texture2D))));
                yield return assignTimelineTextureCoroutine;
                assignTimelineTextureCoroutine = null;
                if (stopCoroutine)
                {
                    stopCoroutine = false;
                    yield break;
                }
            }

            SetTimelineGridSize();

            yield break;
        }

        /// <summary>
        /// Saves the timelines' current waveform texture.
        /// </summary>
        public IEnumerator SaveWaveform()
        {
            var path = !EditorManager.inst.hasLoadedLevel && !EditorManager.inst.loading ?
                    RTFile.CombinePaths(RTFile.ApplicationDirectory, $"settings/waveform-{EditorConfig.Instance.WaveformMode.Value.ToString().ToLower()}{FileFormat.PNG.Dot()}") :
                    RTFile.CombinePaths(RTFile.BasePath, $"waveform-{EditorConfig.Instance.WaveformMode.Value.ToString().ToLower()}{FileFormat.PNG.Dot()}");
            var bytes = timelineImage.sprite.texture.EncodeToPNG();

            File.WriteAllBytes(path, bytes);

            yield break;
        }

        /// <summary>
        /// Sets the timelines' texture.
        /// </summary>
        /// <param name="sprite">Sprite to set.</param>
        public void SetTimelineSprite(Sprite sprite)
        {
            // destroy previous timeline sprite so it gets unloaded.
            CoreHelper.Destroy(timelineImage.sprite);

            timelineImage.sprite = sprite;
            timelineOverlayImage.sprite = timelineImage.sprite;

            // refresh memory
            CoreHelper.Cleanup();
        }

        /// <summary>
        /// Based on the pre-Legacy waveform where the waveform is in the center of the timeline instead of the edges.
        /// </summary>
        public IEnumerator Beta(AudioClip clip, int textureWidth, int textureHeight, Color background, Color center, Action<Texture2D> action)
        {
            yield return Ninja.JumpToUnity;

            CoreHelper.Log("Generating Beta Waveform");

            #region Setup

            int freq = clip.frequency / 100;
            var texture2D = new Texture2D(textureWidth, textureHeight, EditorConfig.Instance.WaveformTextureFormat.Value, false);

            yield return Ninja.JumpBack;
            var backgroundColors = new Color[texture2D.width * texture2D.height];
            for (int i = 0; i < backgroundColors.Length; i++)
                backgroundColors[i] = background;
            texture2D.SetPixels(backgroundColors);

            var samples = new float[clip.samples * clip.channels];
            clip.GetData(samples, 0);

            float[] waveform = new float[samples.Length / freq];

            #endregion

            #region Calculate

            // calculate texture
            for (int i = 0; i < waveform.Length; i++)
            {
                waveform[i] = 0f;
                for (int j = 0; j < freq; j++)
                    waveform[i] += Mathf.Abs(samples[i * freq + j]);
                waveform[i] /= freq;
            }

            // set pixels
            for (int i = 0; i < waveform.Length - 1; i++)
                for (int pos = 0; pos < textureHeight * waveform[i] + 1f; pos++)
                    texture2D.SetPixel(textureWidth * i / waveform.Length, (int)(textureHeight * (waveform[i] + 1f) / 2f) - pos, center);

            #endregion

            #region Apply

            yield return Ninja.JumpToUnity;
            texture2D.wrapMode = TextureWrapMode.Clamp;
            texture2D.filterMode = FilterMode.Point;
            texture2D.Apply();
            action?.Invoke(texture2D);

            #endregion

            yield break;
        }

        /// <summary>
        /// Based on the regular Legacy waveform where the waveform is on the top and bottom of the timeline.
        /// </summary>
        public IEnumerator Legacy(AudioClip clip, int textureWidth, int textureHeight, Color background, Color top, Color bottom, Action<Texture2D> action)
        {
            yield return Ninja.JumpToUnity;

            CoreHelper.Log("Generating Legacy Waveform");

            #region Setup

            int freq = clip.frequency / 160;
            var texture2D = new Texture2D(textureWidth, textureHeight, EditorConfig.Instance.WaveformTextureFormat.Value, false);

            yield return Ninja.JumpBack;
            var backgroundColors = new Color[texture2D.width * texture2D.height];
            for (int i = 0; i < backgroundColors.Length; i++)
                backgroundColors[i] = background;
            texture2D.SetPixels(backgroundColors);

            var samples = new float[clip.samples * clip.channels];
            clip.GetData(samples, 0);

            var oddSamples = clip.channels > 1 ? samples.Where((float value, int index) => index % 2 != 0).ToArray() : samples;
            var evenSamples = clip.channels > 1 ? samples.Where((float value, int index) => index % 2 == 0).ToArray() : samples;

            var waveform = new float[oddSamples.Length / freq];

            #endregion

            #region Calculate

            // calculate top of texture
            for (int i = 0; i < waveform.Length; i++)
            {
                waveform[i] = 0f;
                for (int j = 0; j < freq; j++)
                    waveform[i] += Mathf.Abs(oddSamples[i * freq + j]);

                waveform[i] /= freq;
                waveform[i] *= 0.85f;
            }

            // set top pixels
            for (int i = 0; i < waveform.Length - 1; i++)
                for (int pos = 0; pos < textureHeight * waveform[i]; pos++)
                    texture2D.SetPixel(textureWidth * i / waveform.Length, (int)(textureHeight * waveform[i]) - pos, top);

            // calculate bottom of texture
            for (int i = 0; i < waveform.Length; i++)
            {
                waveform[i] = 0f;
                for (int n = 0; n < freq; n++)
                    waveform[i] += Mathf.Abs(evenSamples[i * freq + n]);

                waveform[i] /= freq;
                waveform[i] *= 0.85f;
            }

            // set bottom pixels
            for (int i = 0; i < waveform.Length - 1; i++)
            {
                for (int pos = 0; pos < textureHeight * waveform[i]; pos++)
                {
                    int x = textureWidth * i / waveform.Length;
                    int y = (int)evenSamples[i * freq + pos] - pos;

                    texture2D.SetPixel(x, y, texture2D.GetPixel(x, y) == top ? RTColors.MixColors(top, bottom) : bottom);
                }
            }

            #endregion

            #region Apply

            yield return Ninja.JumpToUnity;
            texture2D.wrapMode = TextureWrapMode.Clamp;
            texture2D.filterMode = FilterMode.Point;
            texture2D.Apply();
            action?.Invoke(texture2D);

            #endregion

            yield break;
        }

        /// <summary>
        /// Based on the modern VG / Alpha editor waveform where only one side of the waveform is at the bottom of the timeline.
        /// </summary>
        public IEnumerator Modern(AudioClip clip, int textureWidth, int textureHeight, Color background, Color center, Action<Texture2D> action)
        {
            yield return Ninja.JumpToUnity;

            CoreHelper.Log("Generating Modern Waveform");

            #region Setup

            int freq = clip.frequency / 100;
            var texture2D = new Texture2D(textureWidth, textureHeight, EditorConfig.Instance.WaveformTextureFormat.Value, false);
            yield return Ninja.JumpBack;

            var backgroundColors = new Color[texture2D.width * texture2D.height];
            for (int i = 0; i < backgroundColors.Length; i++)
                backgroundColors[i] = background;
            texture2D.SetPixels(backgroundColors);

            float[] samples = new float[clip.samples * clip.channels];
            clip.GetData(samples, 0);

            float[] waveform = new float[samples.Length / freq];

            #endregion

            #region Calculate

            // calculate texture
            for (int i = 0; i < waveform.Length; i++)
            {
                waveform[i] = 0f;
                for (int k = 0; k < freq; k++)
                    waveform[i] += Mathf.Abs(samples[i * freq + k]);
                waveform[i] /= freq;
            }

            // set pixels
            for (int i = 0; i < waveform.Length - 1; i++)
                for (int pos = 0; pos < textureHeight * waveform[i] + 1f; pos++)
                    texture2D.SetPixel(textureWidth * i / waveform.Length, (int)(textureHeight * (waveform[i] + 1f)) - pos, center);

            #endregion

            #region Apply

            yield return Ninja.JumpToUnity;
            texture2D.wrapMode = TextureWrapMode.Clamp;
            texture2D.filterMode = FilterMode.Point;
            texture2D.Apply();
            action?.Invoke(texture2D);

            #endregion

            yield break;
        }

        /// <summary>
        /// Based on the pre-Legacy waveform where the waveform is in the center of the timeline instead of the edges.<br></br>
        /// Forgot where I got this from, but it appeared to be faster at the time. Now it's just a different aesthetic.
        /// </summary>
        public IEnumerator BetaFast(AudioClip audio, int width, int height, Color background, Color col, Action<Texture2D> action)
        {
            yield return Ninja.JumpToUnity;

            CoreHelper.Log("Generating Beta Waveform (Fast)");

            #region Setup

            var tex = new Texture2D(width, height, EditorConfig.Instance.WaveformTextureFormat.Value, false);
            yield return Ninja.JumpBack;

            float[] samples = new float[audio.samples * audio.channels];
            float[] waveform = new float[width];
            audio.GetData(samples, 0);
            float packSize = ((float)samples.Length / (float)width);

            #endregion

            #region Calculate

            int s = 0;
            for (float i = 0; Mathf.RoundToInt(i) < samples.Length && s < waveform.Length; i += packSize)
            {
                waveform[s] = Mathf.Abs(samples[Mathf.RoundToInt(i)]);
                s++;
            }

            // set background colors
            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                    tex.SetPixel(x, y, background);

            for (int x = 0; x < waveform.Length; x++)
            {
                for (int y = 0; y <= waveform[x] * ((float)height * .75f); y++)
                {
                    tex.SetPixel(x, (height / 2) + y, col);
                    tex.SetPixel(x, (height / 2) - y, col);
                }
            }

            #endregion

            #region Apply

            yield return Ninja.JumpToUnity;
            tex.Apply();
            action?.Invoke(tex);

            #endregion

            yield break;
        }

        /// <summary>
        /// Based on the regular Legacy waveform where the waveform is on the top and bottom of the timeline.<br></br>
        /// Forgot where I got this from, but it appeared to be faster at the time. Now it's just a different aesthetic.
        /// </summary>
        public IEnumerator LegacyFast(AudioClip audio, int width, int height, Color background, Color colTop, Color colBot, Action<Texture2D> action)
        {
            yield return Ninja.JumpToUnity;

            CoreHelper.Log("Generating Legacy Waveform (Fast)");

            #region Setup

            var tex = new Texture2D(width, height, EditorConfig.Instance.WaveformTextureFormat.Value, false);
            yield return Ninja.JumpBack;

            float[] samples = new float[audio.samples * audio.channels];
            float[] waveform = new float[width];
            audio.GetData(samples, 0);
            float packSize = ((float)samples.Length / (float)width);

            #endregion

            #region Calculate

            int s = 0;
            for (float i = 0; Mathf.RoundToInt(i) < samples.Length && s < waveform.Length; i += packSize)
            {
                waveform[s] = Mathf.Abs(samples[Mathf.RoundToInt(i)]);
                s++;
            }

            // set background colors
            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                    tex.SetPixel(x, y, background);

            for (int x = 0; x < waveform.Length; x++)
            {
                for (int y = 0; y <= waveform[x] * ((float)height * .75f); y++)
                {
                    tex.SetPixel(x, height - y, colTop);
                    tex.SetPixel(x, y, tex.GetPixel(x, y) == colTop ? RTColors.MixColors(colTop, colBot) : colBot);
                }
            }

            #endregion

            #region Apply

            yield return Ninja.JumpToUnity;
            tex.Apply();
            action?.Invoke(tex);

            #endregion

            yield break;
        }

        /// <summary>
        /// Based on the modern VG / Alpha editor waveform where only one side of the waveform is at the bottom of the timeline.<br></br>
        /// Forgot where I got this from, but it appeared to be faster at the time. Now it's just a different aesthetic.
        /// </summary>
        public IEnumerator ModernFast(AudioClip audio, int width, int height, Color background, Color col, Action<Texture2D> action)
        {
            yield return Ninja.JumpToUnity;

            CoreHelper.Log("Generating Modern Waveform (Fast)");

            #region Setup

            var tex = new Texture2D(width, height, EditorConfig.Instance.WaveformTextureFormat.Value, false);
            yield return Ninja.JumpBack;

            float[] samples = new float[audio.samples * audio.channels];
            float[] waveform = new float[width];
            audio.GetData(samples, 0);
            float packSize = ((float)samples.Length / (float)width);

            #endregion

            #region Calculate

            int s = 0;
            for (float i = 0; Mathf.RoundToInt(i) < samples.Length && s < waveform.Length; i += packSize)
            {
                waveform[s] = Mathf.Abs(samples[Mathf.RoundToInt(i)]);
                s++;
            }

            // set background colors
            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                    tex.SetPixel(x, y, background);

            for (int x = 0; x < waveform.Length; x++)
                for (int y = 0; y <= waveform[x] * ((float)height * .75f); y++)
                    tex.SetPixel(x, y, col);

            #endregion

            #region Apply

            yield return Ninja.JumpToUnity;
            tex.Apply();
            action?.Invoke(tex);

            #endregion

            yield break;
        }

        /// <summary>
        /// Updates the timeline grids' size.
        /// </summary>
        public void SetTimelineGridSize()
        {
            if (!AudioManager.inst || !AudioManager.inst.CurrentAudioSource || !AudioManager.inst.CurrentAudioSource.clip || !EditorConfig.Instance.TimelineGridEnabled.Value)
            {
                if (timelineGridRenderer)
                    timelineGridRenderer.enabled = false;

                return;
            }

            if (timelineGridRenderer)
            {
                timelineGridRenderer.enabled = true;
                var col = EditorConfig.Instance.TimelineGridColor.Value;
                timelineGridRenderer.color = RTColors.FadeColor(col, col.a * RTMath.Clamp(RTMath.InverseLerp(4f, 128f, EditorManager.inst.Zoom), 0f, 1f));

                timelineGridRenderer.gridCellSize.x = 4000;
                timelineGridRenderer.gridSize.x = (RTEditor.inst.editorInfo.bpm) * (SoundManager.inst.MusicLength / (60f / RTEditor.inst.editorInfo.timeSignature));
                timelineGridRenderer.rectTransform.anchoredPosition = new Vector2(RTEditor.inst.editorInfo.bpmOffset * EditorManager.inst.Zoom, 0f);
                timelineGridRenderer.rectTransform.sizeDelta = new Vector2(SoundManager.inst.MusicLength * EditorManager.inst.Zoom, 0f);
                timelineGridRenderer.SetAllDirty();
            }
        }

        #endregion

        #region Bins & Layers

        #region Layers

        public InputField EditorLayerField { get; set; }
        public RectTransform EditorLayerTogglesParent { get; set; }
        public Toggle[] EditorLayerToggles { get; set; }

        /// <summary>
        /// The current editor layer.
        /// </summary>
        public int Layer
        {
            get => GetLayer(EditorManager.inst.layer);
            set => EditorManager.inst.layer = GetLayer(value);
        }

        /// <summary>
        /// The type of layer to render.
        /// </summary>
        public LayerType layerType;

        public int prevLayer;
        public LayerType prevLayerType;

        /// <summary>
        /// Represents a type of layer to render in the timeline. In the vanilla Project Arrhythmia editor, the objects and events layer are considered a part of the same layer system.
        /// <br><br></br></br>This is used to separate them and cause less issues with objects ending up on the events layer.
        /// </summary>
        public enum LayerType
        {
            /// <summary>
            /// Renders the <see cref="BeatmapObject"/> and <see cref="PrefabObject"/> object layers.
            /// </summary>
            Objects,
            /// <summary>
            /// Renders the <see cref="EventKeyframe"/> layers.
            /// </summary>
            Events
        }

        /// <summary>
        /// Limits the editor layer between 0 and <see cref="int.MaxValue"/>.
        /// </summary>
        /// <param name="layer">Editor layer to limit.</param>
        /// <returns>Returns a clamped editor layer.</returns>
        public static int GetLayer(int layer) => Mathf.Clamp(layer, 0, int.MaxValue);

        /// <summary>
        /// Makes the editor layer human-readable by changing it from zero based to one based.
        /// </summary>
        /// <param name="layer">Editor layer to format.</param>
        /// <returns>Returns a formatted editor layer.</returns>
        public static string GetLayerString(int layer) => (layer + 1).ToString();

        /// <summary>
        /// Gets the editor layer color.
        /// </summary>
        /// <param name="layer">The layer to get the color of.</param>
        /// <returns>Returns an editor layers' color.</returns>
        public static Color GetLayerColor(int layer, LayerType layerType = LayerType.Objects)
        {
            if (RTEditor.inst.editorInfo.pinnedEditorLayers.TryFind(x => x.overrideColor && x.layer == layer && x.layerType == layerType, out PinnedEditorLayer pinnedEditorLayer))
                return pinnedEditorLayer.color;

            return layer >= 0 && layer < EditorManager.inst.layerColors.Count ? EditorManager.inst.layerColors[layer] : Color.white;
        }

        /// <summary>
        /// Renders the layer inputs.
        /// </summary>
        /// <param name="layer">Layer to render.</param>
        /// <param name="layerType">Layer type to render.</param>
        public void RenderLayerInput(int layer, LayerType layerType)
        {
            timelineOverlayImage.color = GetLayerColor(layer, layerType);

            RTEditor.inst.RenderEditorLayer(
                editorLayerUI: this,
                getLayer: () => layer,
                setLayer: _val => SetLayer(_val));

            RTEditor.inst.eventLayerToggle.SetIsOnWithoutNotify(layerType == LayerType.Events);
            RTEditor.inst.eventLayerToggle.onValueChanged.NewListener(_val => SetLayer(_val ? LayerType.Events : LayerType.Objects));

        }

        /// <summary>
        /// Sets the current editor layer.
        /// </summary>
        /// <param name="layerType">The type of layer to set.</param>
        public void SetLayer(LayerType layerType) => SetLayer(0, layerType);

        /// <summary>
        /// Sets the current editor layer.
        /// </summary>
        /// <param name="layer">The layer to set.</param>
        /// <param name="setHistory">If the action should be undoable.</param>
        public void SetLayer(int layer, bool setHistory = true) => SetLayer(layer, layerType, setHistory);

        /// <summary>
        /// Sets the current editor layer.
        /// </summary>
        /// <param name="layer">The layer to set.</param>
        /// <param name="layerType">The type of layer to set.</param>
        /// <param name="setHistory">If the action should be undoable.</param>
        public void SetLayer(int layer, LayerType layerType, bool setHistory = true)
        {
            if (layer == 68)
                AchievementManager.inst.UnlockAchievement("editor_layer_lol");

            if (layer == 554)
                AchievementManager.inst.UnlockAchievement("editor_layer_funny");

            var oldLayer = Layer;
            var oldLayerType = this.layerType;

            Layer = layer;
            this.layerType = layerType;
            RenderLayerInput(layer, layerType);
            RTEventEditor.inst.SetEventTimelineActive(layerType == LayerType.Events);

            if (prevLayer != layer || prevLayerType != layerType)
            {
                UpdateTimelineObjects();

                switch (layerType)
                {
                    case LayerType.Objects: {
                            RenderBins();

                            ClampTimeline(false);

                            break;
                        }
                    case LayerType.Events: {
                            SetBinScroll(0f);
                            RenderBins(); // makes sure the bins look normal on the event layer
                            ShowBinControls(false);

                            if (EditorManager.inst.timelineScrollRectBar.value < 0f)
                                EditorManager.inst.timelineScrollRectBar.value = 0f;

                            RTEventEditor.inst.RenderTimelineKeyframes();
                            RTEventEditor.inst.RenderLayerBins();

                            ClampTimeline(true);

                            break;
                        }
                }
            }

            if (prevLayerType != layerType)
                RTCheckpointEditor.inst.UpdateCheckpointTimeline();

            prevLayerType = layerType;
            prevLayer = layer;

            var tmpLayer = Layer;
            var tmpLayerType = this.layerType;
            if (setHistory)
            {
                EditorManager.inst.history.Add(new History.Command("Change Layer", () =>
                {
                    CoreHelper.Log($"Redone layer: {tmpLayer}");
                    SetLayer(tmpLayer, tmpLayerType, false);
                }, () =>
                {
                    CoreHelper.Log($"Undone layer: {oldLayer}");
                    SetLayer(oldLayer, oldLayerType, false);
                }));
            }
        }

        #endregion

        #region Bins

        /// <summary>
        /// Total max of possible bins.
        /// </summary>
        public const int MAX_BINS = 60;

        /// <summary>
        /// The default bin count.
        /// </summary>
        public const int DEFAULT_BIN_COUNT = 14;

        public Transform bins;
        public GameObject binPrefab;
        public Slider binSlider;

        int binCount = DEFAULT_BIN_COUNT;

        /// <summary>
        /// The amount of bins that should render and max objects to.
        /// </summary>
        public int BinCount { get => Mathf.Clamp(binCount, 0, MAX_BINS); set => binCount = Mathf.Clamp(value, 0, MAX_BINS); }

        /// <summary>
        /// The current scroll amount of the bin.
        /// </summary>
        public float BinScroll { get; set; }

        public void UpdateBinControls()
        {
            if (!binSlider)
                return;

            switch (EditorConfig.Instance.BinControlActiveBehavior.Value)
            {
                case BinSliderControlActive.Always: {
                        ShowBinControls(layerType == LayerType.Objects);
                        break;
                    }
                case BinSliderControlActive.Never: {
                        ShowBinControls(false);
                        break;
                    }
                case BinSliderControlActive.KeyToggled: {
                        if (Input.GetKeyDown(EditorConfig.Instance.BinControlKey.Value))
                            ShowBinControls(!binSlider.gameObject.activeSelf);
                        break;
                    }
                case BinSliderControlActive.KeyHeld: {
                        ShowBinControls(Input.GetKey(EditorConfig.Instance.BinControlKey.Value));
                        break;
                    }
            }
        }

        /// <summary>
        /// Adds a bin (row) to the main editor timeline.
        /// </summary>
        public void AddBin()
        {
            if (!EditorManager.inst.hasLoadedLevel)
            {
                EditorManager.inst.DisplayNotification("Please load a level first before trying to change the bin count.", 2f, EditorManager.NotificationType.Warning);
                return;
            }

            if (layerType == LayerType.Events)
            {
                EditorManager.inst.DisplayNotification("Cannot change the bin count of the event layer.", 2f, EditorManager.NotificationType.Warning);
                return;
            }

            int prevBinCount = BinCount;
            BinCount++;
            if (prevBinCount == BinCount)
                return;

            CoreHelper.Log($"Add bin count: {BinCount}");
            EditorManager.inst.DisplayNotification($"Set bin count to {BinCount}!", 1.5f, EditorManager.NotificationType.Success);
            AchievementManager.inst.UnlockAchievement("more_bins");

            Example.Current?.brain?.Notice(ExampleBrain.Notices.MORE_BINS);

            RenderTimelineObjectsPositions();
            RenderBins();

            if (EditorConfig.Instance.MoveToChangedBin.Value)
                SetBinPosition(BinCount);

            if (EditorConfig.Instance.BinControlsPlaysSounds.Value)
                SoundManager.inst.PlaySound(DefaultSounds.pop, 0.7f, 1.3f + UnityEngine.Random.Range(-0.05f, 0.05f));
        }

        /// <summary>
        /// Removes a bin (row) from the main editor timeline.
        /// </summary>
        public void RemoveBin()
        {
            if (!EditorManager.inst.hasLoadedLevel)
            {
                EditorManager.inst.DisplayNotification("Please load a level first before trying to change the bin count.", 2f, EditorManager.NotificationType.Warning);
                return;
            }

            if (layerType == LayerType.Events)
            {
                EditorManager.inst.DisplayNotification("Cannot change the bin count of the event layer.", 2f, EditorManager.NotificationType.Warning);
                return;
            }

            int prevBinCount = BinCount;
            BinCount--;
            if (prevBinCount == BinCount)
                return;

            CoreHelper.Log($"Remove bin count: {BinCount}");
            EditorManager.inst.DisplayNotification($"Set bin count to {BinCount}!", 1.5f, EditorManager.NotificationType.Success);
            AchievementManager.inst.UnlockAchievement("more_bins");

            Example.Current?.brain?.Notice(ExampleBrain.Notices.MORE_BINS);

            RenderTimelineObjectsPositions();
            RenderBins();

            if (EditorConfig.Instance.MoveToChangedBin.Value)
                SetBinPosition(BinCount);

            if (!EditorConfig.Instance.BinControlsPlaysSounds.Value)
                return;

            float add = UnityEngine.Random.Range(-0.05f, 0.05f);
            SoundManager.inst.PlaySound(DefaultSounds.Block, 0.5f, 1.3f + add);
            SoundManager.inst.PlaySound(DefaultSounds.menuflip, 0.4f, 1.5f + add);
        }

        /// <summary>
        /// Sets the bin (row) count to a specific number.
        /// </summary>
        /// <param name="count">Count to set to the editor bins.</param>
        public void SetBinCount(int count)
        {
            if (!EditorManager.inst.hasLoadedLevel)
            {
                EditorManager.inst.DisplayNotification("Please load a level first before trying to change the bin count.", 2f, EditorManager.NotificationType.Warning);
                return;
            }

            if (layerType == LayerType.Events)
            {
                EditorManager.inst.DisplayNotification("Cannot change the bin count of the event layer.", 2f, EditorManager.NotificationType.Warning);
                return;
            }

            int prevBinCount = BinCount;
            BinCount = count;
            if (prevBinCount == BinCount)
                return;

            CoreHelper.Log($"Set bin count: {BinCount}");
            EditorManager.inst.DisplayNotification($"Set bin count to {BinCount}!", 1.5f, EditorManager.NotificationType.Success);
            AchievementManager.inst.UnlockAchievement("more_bins");

            Example.Current?.brain?.Notice(ExampleBrain.Notices.MORE_BINS);

            RenderTimelineObjectsPositions();
            RenderBins();

            if (EditorConfig.Instance.MoveToChangedBin.Value)
                SetBinPosition(BinCount);

            if (EditorConfig.Instance.BinControlsPlaysSounds.Value)
                SoundManager.inst.PlaySound(DefaultSounds.glitch);
        }

        /// <summary>
        /// Shows / hides the bin slider controls.
        /// </summary>
        /// <param name="enabled">If the bin slider should show.</param>
        public void ShowBinControls(bool enabled)
        {
            if (binSlider)
                binSlider.gameObject.SetActive(enabled);
        }

        /// <summary>
        /// Scrolls the editor bins up exactly by one bin height.
        /// </summary>
        public void ScrollBinsUp() => binSlider.value -= 0.1f / 2.3f;

        /// <summary>
        /// Scrolls the editor bins down exactly by one bin height.
        /// </summary>
        public void ScrollBinsDown() => binSlider.value += 0.1f / 2.3f;

        /// <summary>
        /// Sets the editor bins to a specific bin.
        /// </summary>
        /// <param name="bin">Bin to set.</param>
        public void SetBinPosition(int bin)
        {
            if (bin >= 14)
            {
                var value = ((bin - 14f) * 20f) / 920f;
                CoreHelper.Log($"Set pos: {bin} at: {value}");
                SetBinScroll(value);
            }
        }

        /// <summary>
        /// Sets the slider value for the Bin Control slider.
        /// </summary>
        /// <param name="scroll">Value to set.</param>
        public void SetBinScroll(float scroll) => binSlider.value = layerType == LayerType.Events ? 0f : scroll;

        public void RenderBinPosition()
        {
            //var scroll = Mathf.Lerp(0f, Mathf.Clamp(BinCount - DEFAULT_BIN_COUNT, 0f, MAX_BINS), BinScroll) * 10f;
            // can't figure out how to clamp the slider value to the available bin count

            //var scroll = BinScroll * MAX_BINS * 20f;
            var scroll = (MAX_BINS * 15f + 20f) * BinScroll;
            RenderBinPosition(scroll);
        }

        public void RenderBinPosition(float scroll)
        {
            bins.transform.AsRT().anchoredPosition = new Vector2(0f, scroll);
            timelineObjectsParent.transform.AsRT().anchoredPosition = new Vector2(0f, scroll);
        }

        public void RenderBins()
        {
            RenderBinPosition();
            LSHelpers.DeleteChildren(bins);
            for (int i = 0; i < (layerType == LayerType.Events ? DEFAULT_BIN_COUNT : BinCount) + 1; i++)
            {
                var bin = binPrefab.Duplicate(bins);
                bin.transform.GetChild(0).GetComponent<Image>().enabled = i % 2 == 0;
            }
        }

        public int CalculateMaxBin(int binOffset) => EditorConfig.Instance.BinClampBehavior.Value switch
        {
            BinClamp.Clamp => Mathf.Clamp(binOffset, 0, BinCount),
            BinClamp.Loop => (int) Mathf.Repeat(binOffset, BinCount + 1),
            _ => binOffset,
        };

        #endregion

        #endregion
    }
}
