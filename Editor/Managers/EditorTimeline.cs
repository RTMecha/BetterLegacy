﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using UnityEngine;
using UnityEngine.UI;

using LSFunctions;

using CielaSpike;

using BetterLegacy.Companion.Entity;
using BetterLegacy.Configs;
using BetterLegacy.Core;
using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using BetterLegacy.Core.Optimization;
using BetterLegacy.Editor.Components;
using BetterLegacy.Editor.Data;

using ObjectType = BetterLegacy.Core.Data.Beatmap.BeatmapObject.ObjectType;

namespace BetterLegacy.Editor.Managers
{
    public class EditorTimeline : MonoBehaviour
    {
        #region Init

        public static EditorTimeline inst;

        public static void Init() => EditorManager.inst.gameObject.AddComponent<EditorTimeline>();

        void Awake() => inst = this;

        void Update()
        {
            if (Input.GetMouseButtonUp((int)UnityEngine.EventSystems.PointerEventData.InputButton.Middle))
                movingTimeline = false;

            if (!movingTimeline)
                return;

            var vector = Input.mousePosition * CoreHelper.ScreenScaleInverse;
            float multiply = 12f / EditorManager.inst.Zoom;
            SetTimelinePosition(cachedTimelinePos.x + -(((vector.x - EditorManager.inst.DragStartPos.x) / Screen.width) * multiply));
            SetBinScroll(Mathf.Clamp(cachedTimelinePos.y + ((vector.y - EditorManager.inst.DragStartPos.y) / Screen.height), 0f, 1f));
        }

        #endregion

        #region Timeline

        public bool movingTimeline;

        public Slider timelineSlider;

        public Image timelineSliderHandle;
        public Image timelineSliderRuler;
        public Image keyframeTimelineSliderHandle;
        public Image keyframeTimelineSliderRuler;

        public bool isOverMainTimeline;
        public bool changingTime;
        public float newTime;

        public Transform wholeTimeline;

        public Vector2 cachedTimelinePos;

        /// <summary>
        /// Renders the timeline.
        /// </summary>
        public void RenderTimeline()
        {
            if (layerType == LayerType.Events)
                EventEditor.inst.RenderEventObjects();
            else
                RenderTimelineObjectsPositions();

            CheckpointEditor.inst.RenderCheckpoints();
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
        public void SetTimelineZoom(float zoom) => SetTimeline(zoom, AudioManager.inst.CurrentAudioSource.clip == null ? 0f : (EditorConfig.Instance.UseMouseAsZoomPoint.Value ? GetTimelineTime(false) : AudioManager.inst.CurrentAudioSource.time) / AudioManager.inst.CurrentAudioSource.clip.length);

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

                CoroutineHelper.StartCoroutine(ISetTimelinePosition(position));

                EditorManager.inst.zoomSlider.onValueChanged.ClearAll();
                EditorManager.inst.zoomSlider.value = EditorManager.inst.zoomFloat;
                EditorManager.inst.zoomSlider.onValueChanged.AddListener(_val => EditorManager.inst.Zoom = _val);
            }
            catch (Exception ex)
            {
                CoreHelper.LogError($"Had an error with setting zoom. Exception: {ex}");
            }
        }

        // i have no idea why the timeline scrollbar doesn't like to be set in the frame the zoom is also set in.
        IEnumerator ISetTimelinePosition(float position)
        {
            yield return CoroutineHelper.FixedUpdate;
            EditorManager.inst.timelineScrollRectBar.value = position;
        }

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

            keyframeTimelineSliderHandle.color = EditorConfig.Instance.KeyframeCursorColor.Value;
            keyframeTimelineSliderRuler.color = EditorConfig.Instance.KeyframeCursorColor.Value;
        }

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
            var scrollRects = EditorManager.inst.timelineScrollRect.gameObject.GetComponents<ScrollRect>();
            for (int i = 0; i < scrollRects.Length; i++)
                scrollRects[i].movementType = movementType;
            EditorManager.inst.markerTimeline.transform.parent.GetComponent<ScrollRect>().movementType = movementType;
            EditorManager.inst.timelineSlider.transform.parent.GetComponent<ScrollRect>().movementType = movementType;
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

        public IEnumerator GroupSelectObjects(bool _add = true)
        {
            if (!_add)
                DeselectAllObjects();

            var list = timelineObjects;
            list.Where(x => x.Layer == Layer && RTMath.RectTransformToScreenSpace(EditorManager.inst.SelectionBoxImage.rectTransform)
            .Overlaps(RTMath.RectTransformToScreenSpace(x.Image.rectTransform))).ToList().ForEach(timelineObject =>
            {
                timelineObject.Selected = true;
                timelineObject.timeOffset = 0f;
                timelineObject.binOffset = 0;
            });

            if (SelectedObjectCount > 1)
            {
                EditorManager.inst.ClearPopups();
                MultiObjectEditor.inst.Dialog.Open();
            }

            if (SelectedObjectCount <= 0)
                CheckpointEditor.inst.SetCurrentCheckpoint(0);

            EditorManager.inst.DisplayNotification($"Selection includes {SelectedObjectCount} objects!", 1f, EditorManager.NotificationType.Success);
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
                EditorManager.inst.ClearPopups();

                var first = SelectedObjects[0];
                timelineObject.Selected = !timelineObject.Selected;
                if (SelectedObjectCount == 0 || SelectedObjectCount == 1)
                {
                    SetCurrentObject(SelectedObjectCount == 1 ? SelectedObjects[0] : first);
                    return;
                }

                MultiObjectEditor.inst.Dialog.Open();

                RenderTimelineObject(timelineObject);

                return;
            }

            SetCurrentObject(timelineObject);
        }

        public void SetCurrentObject(TimelineObject timelineObject, bool bringTo = false, bool openDialog = true)
        {
            if (!timelineObject.verified && !timelineObjects.Has(x => x.ID == timelineObject.ID))
                RenderTimelineObject(timelineObject);

            if (CurrentSelection.isBeatmapObject && CurrentSelection.ID != timelineObject.ID)
                for (int i = 0; i < ObjEditor.inst.TimelineParents.Count; i++)
                    LSHelpers.DeleteChildren(ObjEditor.inst.TimelineParents[i]);

            DeselectAllObjects();

            timelineObject.Selected = true;
            CurrentSelection = timelineObject;

            if (!string.IsNullOrEmpty(timelineObject.ID) && openDialog)
            {
                if (timelineObject.isBeatmapObject)
                    ObjectEditor.inst.OpenDialog(timelineObject.GetData<BeatmapObject>());
                if (timelineObject.isPrefabObject)
                    PrefabEditor.inst.OpenPrefabDialog();
            }

            if (bringTo)
            {
                AudioManager.inst.SetMusicTime(timelineObject.Time);
                SetLayer(timelineObject.Layer, LayerType.Objects);
            }
        }

        /// <summary>
        /// Removes and destroys the timeline object.
        /// </summary>
        /// <param name="timelineObject">Timeline object to remove.</param>
        public void RemoveTimelineObject(TimelineObject timelineObject)
        {
            if (timelineObjects.TryFindIndex(x => x.ID == timelineObject.ID, out int a))
            {
                Destroy(timelineObject.GameObject);
                timelineObjects.RemoveAt(a);
            }
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
            for (int i = 0; i < timelineObjects.Count; i++)
                timelineObjects[i].RenderVisibleState();

            if (CurrentSelection && CurrentSelection.isBeatmapObject && CurrentSelection.InternalTimelineObjects.Count > 0)
                for (int i = 0; i < CurrentSelection.InternalTimelineObjects.Count; i++)
                    CurrentSelection.InternalTimelineObjects[i].RenderVisibleState();

            for (int i = 0; i < timelineKeyframes.Count; i++)
                timelineKeyframes[i].RenderVisibleState();
        }

        /// <summary>
        /// Finds the timeline object with the associated BeatmapObject ID.
        /// </summary>
        /// <param name="beatmapObject"></param>
        /// <returns>Returns either the related TimelineObject or a new TimelineObject if one doesn't exist for whatever reason.</returns>
        public TimelineObject GetTimelineObject(BeatmapObject beatmapObject)
        {
            if (beatmapObject.fromPrefab && timelineObjects.TryFind(x => x.isPrefabObject && x.ID == beatmapObject.prefabInstanceID, out TimelineObject timelineObject))
                return timelineObject;

            if (!beatmapObject.timelineObject)
                beatmapObject.timelineObject = new TimelineObject(beatmapObject);

            return beatmapObject.timelineObject;
        }

        /// <summary>
        /// Finds the timeline object with the associated PrefabObject ID.
        /// </summary>
        /// <param name="prefabObject"></param>
        /// <returns>Returns either the related TimelineObject or a new TimelineObject if one doesn't exist for whatever reason.</returns>
        public TimelineObject GetTimelineObject(PrefabObject prefabObject)
        {
            if (!prefabObject.timelineObject)
                prefabObject.timelineObject = new TimelineObject(prefabObject);

            return prefabObject.timelineObject;
        }

        public void RenderTimelineObject(TimelineObject timelineObject)
        {
            if (!timelineObject.GameObject)
            {
                timelineObject.AddToList();
                timelineObject.Init();
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
            foreach (var timelineObject in timelineObjects)
            {
                if (timelineObject.IsCurrentLayer)
                    timelineObject.RenderPosLength();
            }
        }

        public IEnumerator ICreateTimelineObjects()
        {
            if (timelineObjects.Count > 0)
                timelineObjects.ForEach(x => Destroy(x.GameObject));
            timelineObjects.Clear();

            for (int i = 0; i < GameData.Current.beatmapObjects.Count; i++)
            {
                var beatmapObject = GameData.Current.beatmapObjects[i];
                if (!string.IsNullOrEmpty(beatmapObject.id) && !beatmapObject.fromPrefab)
                {
                    var timelineObject = GetTimelineObject(beatmapObject);
                    timelineObject.AddToList(true);
                    timelineObject.Init(true);
                }
            }

            for (int i = 0; i < GameData.Current.prefabObjects.Count; i++)
            {
                var prefabObject = GameData.Current.prefabObjects[i];
                if (!string.IsNullOrEmpty(prefabObject.id))
                {
                    var timelineObject = GetTimelineObject(prefabObject);
                    timelineObject.AddToList(true);
                    timelineObject.Init(true);
                }
            }

            yield break;
        }

        public void CreateTimelineObjects()
        {
            if (timelineObjects.Count > 0)
                timelineObjects.ForEach(x => Destroy(x.GameObject));
            timelineObjects.Clear();

            for (int i = 0; i < GameData.Current.beatmapObjects.Count; i++)
            {
                var beatmapObject = GameData.Current.beatmapObjects[i];
                if (!string.IsNullOrEmpty(beatmapObject.id) && !beatmapObject.fromPrefab)
                {
                    var timelineObject = GetTimelineObject(beatmapObject);
                    timelineObject.AddToList(true);
                    timelineObject.Init(true);
                }
            }

            for (int i = 0; i < GameData.Current.prefabObjects.Count; i++)
            {
                var prefabObject = GameData.Current.prefabObjects[i];
                if (!string.IsNullOrEmpty(prefabObject.id))
                {
                    var timelineObject = GetTimelineObject(prefabObject);
                    timelineObject.AddToList(true);
                    timelineObject.Init(true);
                }
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

            for (int i = 0; i < GameData.Current.prefabObjects.Count; i++)
            {
                var prefabObject = GameData.Current.prefabObjects[i];
                if (prefabObject.fromModifier)
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
            if (!timelineObject || timelineObject.isBeatmapObject && timelineObject.GetData<BeatmapObject>().fromPrefab && timelineObject.GetData<BeatmapObject>().TryGetPrefabObject(out PrefabObject result) && result.fromModifier)
                return;

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
            if (RTEditor.inst.prefabPickerEnabled && timelineObject.isBeatmapObject)
            {
                var beatmapObject = timelineObject.GetData<BeatmapObject>();
                if (string.IsNullOrEmpty(beatmapObject.prefabInstanceID))
                {
                    EditorManager.inst.DisplayNotification("Object is not assigned to a prefab!", 2f, EditorManager.NotificationType.Error);
                    return;
                }

                if (RTEditor.inst.selectingMultiple)
                {
                    foreach (var otherTimelineObject in SelectedObjects.Where(x => x.isBeatmapObject))
                    {
                        var otherBeatmapObject = otherTimelineObject.GetData<BeatmapObject>();
                        otherBeatmapObject.SetPrefabReference(beatmapObject);
                        RenderTimelineObject(otherTimelineObject);
                    }
                }
                else if (CurrentSelection.isBeatmapObject)
                {
                    var currentBeatmapObject = CurrentSelection.GetData<BeatmapObject>();
                    currentBeatmapObject.SetPrefabReference(beatmapObject);
                    RenderTimelineObject(CurrentSelection);
                    ObjectEditor.inst.OpenDialog(currentBeatmapObject);
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
                        var otherBeatmapObject = otherTimelineObject.GetData<BeatmapObject>();

                        otherBeatmapObject.prefabID = prefabObject.prefabID;
                        otherBeatmapObject.prefabInstanceID = prefabInstanceID;
                        RenderTimelineObject(otherTimelineObject);
                    }
                }
                else if (CurrentSelection.isBeatmapObject)
                {
                    var currentBeatmapObject = CurrentSelection.GetData<BeatmapObject>();

                    currentBeatmapObject.prefabID = prefabObject.prefabID;
                    currentBeatmapObject.prefabInstanceID = prefabInstanceID;
                    RenderTimelineObject(CurrentSelection);
                    ObjectEditor.inst.OpenDialog(currentBeatmapObject);
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
                            prefabObject.parent = timelineObject.ID;
                            Updater.UpdatePrefab(prefabObject, Updater.PrefabContext.PARENT, false);
                            RTPrefabEditor.inst.RenderPrefabObjectDialog(prefabObject);

                            success = true;
                            continue;
                        }
                        success = otherTimelineObject.GetData<BeatmapObject>().TrySetParent(timelineObject.GetData<BeatmapObject>());
                    }
                    Updater.RecalculateObjectStates();

                    if (!success)
                        EditorManager.inst.DisplayNotification("Cannot set parent to child / self!", 1f, EditorManager.NotificationType.Warning);
                    else
                        RTEditor.inst.parentPickerEnabled = false;

                    return;
                }

                if (CurrentSelection.isPrefabObject)
                {
                    var prefabObject = CurrentSelection.GetData<PrefabObject>();
                    prefabObject.parent = timelineObject.ID;
                    Updater.UpdatePrefab(prefabObject, Updater.PrefabContext.PARENT);
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

        /// <summary>
        /// Updates the timelines' waveform texture.
        /// </summary>
        public IEnumerator AssignTimelineTexture(bool forceReload = false)
        {
            var config = EditorConfig.Instance;
            var path = RTFile.CombinePaths(RTFile.BasePath, $"waveform-{config.WaveformMode.Value.ToString().ToLower()}{FileFormat.PNG.Dot()}");
            var settingsPath = RTFile.CombinePaths(RTFile.ApplicationDirectory, $"settings/waveform-{config.WaveformMode.Value.ToString().ToLower()}{FileFormat.PNG.Dot()}");

            SetTimelineSprite(null);

            if ((!EditorManager.inst.hasLoadedLevel && !EditorManager.inst.loading && !RTFile.FileExists(settingsPath) ||
                !RTFile.FileExists(path)) && !config.WaveformRerender.Value || config.WaveformRerender.Value || forceReload)
            {
                var clip = AudioManager.inst.CurrentAudioSource.clip;
                int num = Mathf.Clamp((int)clip.length * 48, 100, 15000);
                Texture2D waveform = null;

                yield return config.WaveformMode.Value switch
                {
                    WaveformType.Split => CoroutineHelper.StartCoroutineAsync(Legacy(clip, num, 300, config.WaveformBGColor.Value, config.WaveformTopColor.Value, config.WaveformBottomColor.Value, _tex => waveform = _tex)),
                    WaveformType.Centered => CoroutineHelper.StartCoroutineAsync(Beta(clip, num, 300, config.WaveformBGColor.Value, config.WaveformTopColor.Value, _tex => waveform = _tex)),
                    WaveformType.Bottom => CoroutineHelper.StartCoroutineAsync(Modern(clip, num, 300, config.WaveformBGColor.Value, config.WaveformTopColor.Value, _tex => waveform = _tex)),
                    WaveformType.SplitDetailed => CoroutineHelper.StartCoroutineAsync(LegacyFast(clip, num, 300, config.WaveformBGColor.Value, config.WaveformTopColor.Value, config.WaveformBottomColor.Value, _tex => waveform = _tex)),
                    WaveformType.CenteredDetailed => CoroutineHelper.StartCoroutineAsync(BetaFast(clip, num, 300, config.WaveformBGColor.Value, config.WaveformTopColor.Value, _tex => waveform = _tex)),
                    WaveformType.BottomDetailed => CoroutineHelper.StartCoroutineAsync(ModernFast(clip, num, 300, config.WaveformBGColor.Value, config.WaveformTopColor.Value, _tex => waveform = _tex)),
                    _ => null,
                };

                SetTimelineSprite(Sprite.Create(waveform, new Rect(0f, 0f, num, 300f), new Vector2(0.5f, 0.5f), 100f));

                if (config.WaveformSaves.Value)
                    CoroutineHelper.StartCoroutineAsync(SaveWaveform());
            }
            else
            {
                CoroutineHelper.StartCoroutineAsync(AlephNetwork.DownloadImageTexture("file://" + (!EditorManager.inst.hasLoadedLevel && !EditorManager.inst.loading ?
                settingsPath :
                path), texture2D => SetTimelineSprite(SpriteHelper.CreateSprite(texture2D))));
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
            timelineImage.sprite = sprite;
            timelineOverlayImage.sprite = timelineImage.sprite;
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

                    texture2D.SetPixel(x, y, texture2D.GetPixel(x, y) == top ? CoreHelper.MixColors(top, bottom) : bottom);
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
                    tex.SetPixel(x, y, tex.GetPixel(x, y) == colTop ? CoreHelper.MixColors(colTop, colBot) : colBot);
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

        // todo: look into improving this? is it possible to fix the issues with zooming in too close causing the grid to break and some issues with the grid going further than it should.
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

            //var clipLength = AudioManager.inst.CurrentAudioSource.clip.length;

            //float x = RTEditor.inst.editorInfo.bpm / 60f;

            //var closer = 40f * x;
            //var close = 20f * x;
            //var unrender = 6f * x;

            //var bpm = EditorManager.inst.Zoom > closer ? RTEditor.inst.editorInfo.bpm : EditorManager.inst.Zoom > close ? RTEditor.inst.editorInfo.bpm / 2f : RTEditor.inst.editorInfo.bpm / 4f;
            //var snapDivisions = RTEditor.inst.editorInfo.timeSignature * 2f;
            //if (timelineGridRenderer && EditorManager.inst.Zoom > unrender)
            //{
            //    timelineGridRenderer.enabled = false;
            //    timelineGridRenderer.gridCellSize.x = ((int)bpm / (int)snapDivisions) * (int)clipLength;
            //    timelineGridRenderer.gridSize.x = clipLength * bpm / (snapDivisions * 1.875f);
            //    timelineGridRenderer.enabled = true;
            //}
            //else if (timelineGridRenderer)
            //    timelineGridRenderer.enabled = false;

            if (timelineGridRenderer)
            {
                timelineGridRenderer.enabled = true;
                var col = EditorConfig.Instance.TimelineGridColor.Value;
                timelineGridRenderer.color = LSColors.fadeColor(col, col.a * RTMath.Clamp(RTMath.InverseLerp(4f, 128f, EditorManager.inst.Zoom), 0f, 1f));

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

        /// <summary>
        /// List of editor layers the user has pinned in a level.
        /// </summary>
        public List<PinnedEditorLayer> pinnedEditorLayers = new List<PinnedEditorLayer>();

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
        public static Color GetLayerColor(int layer)
        {
            if (inst.pinnedEditorLayers.TryFind(x => x.layer == layer, out PinnedEditorLayer pinnedEditorLayer))
                return pinnedEditorLayer.color;

            return layer >= 0 && layer < EditorManager.inst.layerColors.Count ? EditorManager.inst.layerColors[layer] : Color.white;
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
            timelineOverlayImage.color = GetLayerColor(layer);
            RTEditor.inst.editorLayerImage.color = GetLayerColor(layer);

            RTEditor.inst.editorLayerField.onValueChanged.ClearAll();
            RTEditor.inst.editorLayerField.text = GetLayerString(layer);
            RTEditor.inst.editorLayerField.onValueChanged.AddListener(_val =>
            {
                if (int.TryParse(_val, out int num))
                    SetLayer(Mathf.Clamp(num - 1, 0, int.MaxValue));
            });

            RTEditor.inst.eventLayerToggle.onValueChanged.ClearAll();
            RTEditor.inst.eventLayerToggle.isOn = layerType == LayerType.Events;
            RTEditor.inst.eventLayerToggle.onValueChanged.AddListener(_val => SetLayer(_val ? LayerType.Events : LayerType.Objects));

            RTEventEditor.inst.SetEventActive(layerType == LayerType.Events);

            if (prevLayer != layer || prevLayerType != layerType)
            {
                UpdateTimelineObjects();
                switch (layerType)
                {
                    case LayerType.Objects: {
                            RenderBins();
                            RenderTimelineObjectsPositions();

                            if (prevLayerType != layerType)
                                CheckpointEditor.inst.CreateGhostCheckpoints();

                            ClampTimeline(false);

                            break;
                        }
                    case LayerType.Events: {
                            SetBinScroll(0f);
                            RenderBins(); // makes sure the bins look normal on the event layer
                            ShowBinControls(false);

                            if (EditorManager.inst.timelineScrollRectBar.value < 0f)
                                EditorManager.inst.timelineScrollRectBar.value = 0f;

                            RTEventEditor.inst.RenderEventObjects();
                            CheckpointEditor.inst.CreateCheckpoints();

                            RTEventEditor.inst.RenderLayerBins();

                            ClampTimeline(true);

                            break;
                        }
                }
            }

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

        #endregion

        #endregion
    }
}
