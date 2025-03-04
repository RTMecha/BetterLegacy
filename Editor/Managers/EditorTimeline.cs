using BetterLegacy.Companion.Entity;
using BetterLegacy.Configs;
using BetterLegacy.Core;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using BetterLegacy.Editor.Components;
using BetterLegacy.Editor.Data;
using CielaSpike;
using LSFunctions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnityEngine;
using UnityEngine.UI;

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
        /// Sets the main timeline position.
        /// </summary>
        /// <param name="position">The position to set the timeline scroll.</param>
        public void SetTimelinePosition(float position) => SetTimeline(EditorManager.inst.zoomFloat, position);

        /// <summary>
        /// Sets the main timeline zoom.
        /// </summary>
        /// <param name="zoom">The zoom to set to the timeline.</param>
        public void SetTimelineZoom(float zoom) => SetTimeline(zoom, AudioManager.inst.CurrentAudioSource.clip == null ? 0f : (EditorConfig.Instance.UseMouseAsZoomPoint.Value ? GetTimelineTime() : AudioManager.inst.CurrentAudioSource.time) / AudioManager.inst.CurrentAudioSource.clip.length);

        /// <summary>
        /// Sets the main timeline zoom and position.
        /// </summary>
        /// <param name="zoom">The amount to zoom in.</param>
        /// <param name="position">The position to set the timeline scroll. If the value is less that 0, it will automatically calculate the position to match the audio time.</param>
        /// <param name="render">If the timeline should render.</param>
        /// <param name="log">If the zoom amount should be logged.</param>
        public void SetTimeline(float zoom, float position, bool render = true)
        {
            try
            {
                float prevZoom = EditorManager.inst.zoomFloat;
                EditorManager.inst.zoomFloat = Mathf.Clamp01(zoom);
                EditorManager.inst.zoomVal =
                    LSMath.InterpolateOverCurve(EditorManager.inst.ZoomCurve, EditorManager.inst.zoomBounds.x, EditorManager.inst.zoomBounds.y, EditorManager.inst.zoomFloat);

                if (render)
                    EditorManager.inst.RenderTimeline();

                CoreHelper.StartCoroutine(ISetTimelinePosition(position));

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
            yield return new WaitForFixedUpdate();
            EditorManager.inst.timelineScrollRectBar.value = position;
        }

        /// <summary>
        /// Calculates the timeline time the mouse cursor is at.
        /// </summary>
        /// <returns>Returns a calculated timeline time.</returns>
        public float GetTimelineTime()
        {
            float num = Input.mousePosition.x;
            num += Mathf.Abs(EditorManager.inst.timeline.transform.AsRT().position.x);

            return SettingEditor.inst.SnapActive && !Input.GetKey(KeyCode.LeftAlt) ?
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
        public static Sprite GetKeyframeIcon(DataManager.LSAnimation a, DataManager.LSAnimation b)
            => ObjEditor.inst.KeyframeSprites[a.Name.Contains("Out") && b.Name.Contains("In") ? 3 : a.Name.Contains("Out") ? 2 : b.Name.Contains("In") ? 1 : 0];

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
                if (!string.IsNullOrEmpty(prefabObject.ID))
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
                if (!string.IsNullOrEmpty(prefabObject.ID))
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

        #endregion

        #region Timeline Textures

        public Image timelineImage;
        public Image timelineOverlayImage;
        public GridRenderer timelineGridRenderer;

        /// <summary>
        /// Updates the timelines' waveform texture.
        /// </summary>
        public IEnumerator AssignTimelineTexture()
        {
            var config = EditorConfig.Instance;
            var path = RTFile.CombinePaths(RTFile.BasePath, $"waveform-{config.WaveformMode.Value.ToString().ToLower()}{FileFormat.PNG.Dot()}");
            var settingsPath = RTFile.CombinePaths(RTFile.ApplicationDirectory, $"settings/waveform-{config.WaveformMode.Value.ToString().ToLower()}{FileFormat.PNG.Dot()}");

            SetTimelineSprite(null);

            if ((!EditorManager.inst.hasLoadedLevel && !EditorManager.inst.loading && !RTFile.FileExists(settingsPath) ||
                !RTFile.FileExists(path)) && !config.WaveformRerender.Value || config.WaveformRerender.Value)
            {
                int num = Mathf.Clamp((int)AudioManager.inst.CurrentAudioSource.clip.length * 48, 100, 15000);
                Texture2D waveform = null;

                switch (config.WaveformMode.Value)
                {
                    case WaveformType.Split: {
                            yield return CoreHelper.StartCoroutineAsync(Legacy(AudioManager.inst.CurrentAudioSource.clip, num, 300, config.WaveformBGColor.Value, config.WaveformTopColor.Value, config.WaveformBottomColor.Value, (Texture2D _tex) => { waveform = _tex; }));
                            break;
                        }
                    case WaveformType.Centered: {
                            yield return CoreHelper.StartCoroutineAsync(Beta(AudioManager.inst.CurrentAudioSource.clip, num, 300, config.WaveformBGColor.Value, config.WaveformTopColor.Value, (Texture2D _tex) => { waveform = _tex; }));
                            break;
                        }
                    case WaveformType.Bottom: {
                            yield return CoreHelper.StartCoroutineAsync(Modern(AudioManager.inst.CurrentAudioSource.clip, num, 300, config.WaveformBGColor.Value, config.WaveformTopColor.Value, (Texture2D _tex) => { waveform = _tex; }));
                            break;
                        }
                    case WaveformType.SplitDetailed: {
                            yield return CoreHelper.StartCoroutineAsync(LegacyFast(AudioManager.inst.CurrentAudioSource.clip, num, 300, config.WaveformBGColor.Value, config.WaveformTopColor.Value, config.WaveformBottomColor.Value, (Texture2D _tex) => { waveform = _tex; }));
                            break;
                        }
                    case WaveformType.CenteredDetailed: {
                            yield return CoreHelper.StartCoroutineAsync(BetaFast(AudioManager.inst.CurrentAudioSource.clip, num, 300, config.WaveformBGColor.Value, config.WaveformTopColor.Value, (Texture2D _tex) => { waveform = _tex; }));
                            break;
                        }
                    case WaveformType.BottomDetailed: {
                            yield return CoreHelper.StartCoroutineAsync(ModernFast(AudioManager.inst.CurrentAudioSource.clip, num, 300, config.WaveformBGColor.Value, config.WaveformTopColor.Value, (Texture2D _tex) => { waveform = _tex; }));
                            break;
                        }
                }

                var waveSprite = Sprite.Create(waveform, new Rect(0f, 0f, num, 300f), new Vector2(0.5f, 0.5f), 100f);
                SetTimelineSprite(waveSprite);

                if (config.WaveformSaves.Value)
                    CoreHelper.StartCoroutineAsync(SaveWaveform());
            }
            else
            {
                CoreHelper.StartCoroutineAsync(AlephNetwork.DownloadImageTexture("file://" + (!EditorManager.inst.hasLoadedLevel && !EditorManager.inst.loading ?
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
        public IEnumerator Beta(AudioClip clip, int textureWidth, int textureHeight, Color background, Color waveform, Action<Texture2D> action)
        {
            yield return Ninja.JumpToUnity;
            CoreHelper.Log("Generating Beta Waveform");
            int num = 100;
            var texture2D = new Texture2D(textureWidth, textureHeight, EditorConfig.Instance.WaveformTextureFormat.Value, false);
            yield return Ninja.JumpBack;

            var array = new Color[texture2D.width * texture2D.height];
            for (int i = 0; i < array.Length; i++)
                array[i] = background;

            texture2D.SetPixels(array);
            num = clip.frequency / num;
            float[] array2 = new float[clip.samples * clip.channels];
            clip.GetData(array2, 0);
            float[] array3 = new float[array2.Length / num];
            for (int j = 0; j < array3.Length; j++)
            {
                array3[j] = 0f;
                for (int k = 0; k < num; k++)
                    array3[j] += Mathf.Abs(array2[j * num + k]);
                array3[j] /= num;
            }
            for (int l = 0; l < array3.Length - 1; l++)
            {
                int num2 = 0;
                while (num2 < textureHeight * array3[l] + 1f)
                {
                    texture2D.SetPixel(textureWidth * l / array3.Length, (int)(textureHeight * (array3[l] + 1f) / 2f) - num2, waveform);
                    num2++;
                }
            }
            yield return Ninja.JumpToUnity;
            texture2D.wrapMode = TextureWrapMode.Clamp;
            texture2D.filterMode = FilterMode.Point;
            texture2D.Apply();
            action(texture2D);
            yield break;
        }

        /// <summary>
        /// Based on the regular Legacy waveform where the waveform is on the top and bottom of the timeline.
        /// </summary>
        public IEnumerator Legacy(AudioClip clip, int textureWidth, int textureHeight, Color background, Color top, Color bottom, Action<Texture2D> action)
        {
            yield return Ninja.JumpToUnity;

            CoreHelper.Log("Generating Legacy Waveform");
            int num = 160;
            num = clip.frequency / num;
            var texture2D = new Texture2D(textureWidth, textureHeight, EditorConfig.Instance.WaveformTextureFormat.Value, false);

            yield return Ninja.JumpBack;
            Color[] array = new Color[texture2D.width * texture2D.height];
            for (int i = 0; i < array.Length; i++)
                array[i] = background;

            texture2D.SetPixels(array);
            float[] array3 = new float[clip.samples];
            float[] array4 = new float[clip.samples];
            float[] array5 = new float[clip.samples * clip.channels];
            clip.GetData(array5, 0);
            if (clip.channels > 1)
            {
                array3 = array5.Where((float value, int index) => index % 2 != 0).ToArray();
                array4 = array5.Where((float value, int index) => index % 2 == 0).ToArray();
            }
            else
            {
                array3 = array5;
                array4 = array5;
            }
            float[] array6 = new float[array3.Length / num];
            for (int j = 0; j < array6.Length; j++)
            {
                array6[j] = 0f;
                for (int k = 0; k < num; k++)
                {
                    array6[j] += Mathf.Abs(array3[j * num + k]);
                }
                array6[j] /= num;
                array6[j] *= 0.85f;
            }
            for (int l = 0; l < array6.Length - 1; l++)
            {
                int num2 = 0;
                while (num2 < textureHeight * array6[l])
                {
                    texture2D.SetPixel(textureWidth * l / array6.Length, (int)(textureHeight * array6[l]) - num2, top);
                    num2++;
                }
            }
            array6 = new float[array4.Length / num];
            for (int m = 0; m < array6.Length; m++)
            {
                array6[m] = 0f;
                for (int n = 0; n < num; n++)
                {
                    array6[m] += Mathf.Abs(array4[m * num + n]);
                }
                array6[m] /= num;
                array6[m] *= 0.85f;
            }
            for (int num3 = 0; num3 < array6.Length - 1; num3++)
            {
                int num4 = 0;
                while (num4 < textureHeight * array6[num3])
                {
                    int x = textureWidth * num3 / array6.Length;
                    int y = (int)array4[num3 * num + num4] - num4;
                    texture2D.SetPixel(x, y, texture2D.GetPixel(x, y) == top ? CoreHelper.MixColors(top, bottom) : bottom);
                    num4++;
                }
            }
            yield return Ninja.JumpToUnity;
            texture2D.wrapMode = TextureWrapMode.Clamp;
            texture2D.filterMode = FilterMode.Point;
            texture2D.Apply();
            action?.Invoke(texture2D);
            yield break;
        }

        /// <summary>
        /// Based on the modern VG / Alpha editor waveform where only one side of the waveform is at the bottom of the timeline.
        /// </summary>
        public IEnumerator Modern(AudioClip clip, int textureWidth, int textureHeight, Color background, Color waveform, Action<Texture2D> action)
        {
            yield return Ninja.JumpToUnity;
            CoreHelper.Log("Generating Modern Waveform");
            int num = 100;
            var texture2D = new Texture2D(textureWidth, textureHeight, EditorConfig.Instance.WaveformTextureFormat.Value, false);
            yield return Ninja.JumpBack;

            var array = new Color[texture2D.width * texture2D.height];
            for (int i = 0; i < array.Length; i++)
                array[i] = background;

            texture2D.SetPixels(array);
            num = clip.frequency / num;
            float[] array2 = new float[clip.samples * clip.channels];
            clip.GetData(array2, 0);
            float[] array3 = new float[array2.Length / num];
            for (int j = 0; j < array3.Length; j++)
            {
                array3[j] = 0f;
                for (int k = 0; k < num; k++)
                    array3[j] += Mathf.Abs(array2[j * num + k]);
                array3[j] /= (float)num;
            }
            for (int l = 0; l < array3.Length - 1; l++)
            {
                int num2 = 0;
                while (num2 < textureHeight * array3[l] + 1f)
                {
                    texture2D.SetPixel(textureWidth * l / array3.Length, (int)(textureHeight * (array3[l] + 1f)) - num2, waveform);
                    num2++;
                }
            }
            yield return Ninja.JumpToUnity;
            texture2D.wrapMode = TextureWrapMode.Clamp;
            texture2D.filterMode = FilterMode.Point;
            texture2D.Apply();
            action(texture2D);
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
            var tex = new Texture2D(width, height, EditorConfig.Instance.WaveformTextureFormat.Value, false);
            yield return Ninja.JumpBack;

            float[] samples = new float[audio.samples * audio.channels];
            float[] waveform = new float[width];
            audio.GetData(samples, 0);
            float packSize = ((float)samples.Length / (float)width);
            int s = 0;
            for (float i = 0; Mathf.RoundToInt(i) < samples.Length && s < waveform.Length; i += packSize)
            {
                waveform[s] = Mathf.Abs(samples[Mathf.RoundToInt(i)]);
                s++;
            }

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
            yield return Ninja.JumpToUnity;
            tex.Apply();

            action?.Invoke(tex);
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
            var tex = new Texture2D(width, height, EditorConfig.Instance.WaveformTextureFormat.Value, false);
            yield return Ninja.JumpBack;

            float[] samples = new float[audio.samples * audio.channels];
            float[] waveform = new float[width];
            audio.GetData(samples, 0);
            float packSize = ((float)samples.Length / (float)width);
            int s = 0;
            for (float i = 0; Mathf.RoundToInt(i) < samples.Length && s < waveform.Length; i += packSize)
            {
                waveform[s] = Mathf.Abs(samples[Mathf.RoundToInt(i)]);
                s++;
            }

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    tex.SetPixel(x, y, background);
                }
            }

            for (int x = 0; x < waveform.Length; x++)
            {
                for (int y = 0; y <= waveform[x] * ((float)height * .75f); y++)
                {
                    tex.SetPixel(x, height - y, colTop);

                    tex.SetPixel(x, y, tex.GetPixel(x, y) == colTop ? CoreHelper.MixColors(colTop, colBot) : colBot);
                }
            }
            yield return Ninja.JumpToUnity;
            tex.Apply();

            action?.Invoke(tex);
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
            var tex = new Texture2D(width, height, EditorConfig.Instance.WaveformTextureFormat.Value, false);
            yield return Ninja.JumpBack;

            float[] samples = new float[audio.samples * audio.channels];
            float[] waveform = new float[width];
            audio.GetData(samples, 0);
            float packSize = ((float)samples.Length / (float)width);
            int s = 0;
            for (float i = 0; Mathf.RoundToInt(i) < samples.Length && s < waveform.Length; i += packSize)
            {
                waveform[s] = Mathf.Abs(samples[Mathf.RoundToInt(i)]);
                s++;
            }

            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                    tex.SetPixel(x, y, background);

            for (int x = 0; x < waveform.Length; x++)
            {
                for (int y = 0; y <= waveform[x] * ((float)height * .75f); y++)
                {
                    tex.SetPixel(x, y, col);
                    //tex.SetPixel(x, (height / 2) - y, col);
                }
            }
            yield return Ninja.JumpToUnity;
            tex.Apply();

            action?.Invoke(tex);
            yield break;
        }

        // todo: look into improving this? is it possible to fix the issues with zooming in too close causing the grid to break and some issues with the grid going further than it should.
        /// <summary>
        /// Updates the timeline grids' size.
        /// </summary>
        public void SetTimelineGridSize()
        {
            if (!AudioManager.inst || !AudioManager.inst.CurrentAudioSource || !AudioManager.inst.CurrentAudioSource.clip)
            {
                if (timelineGridRenderer)
                    timelineGridRenderer.enabled = false;
                return;
            }

            var clipLength = AudioManager.inst.CurrentAudioSource.clip.length;

            float x = SettingEditor.inst.SnapBPM / 60f;

            var closer = 40f * x;
            var close = 20f * x;
            var unrender = 6f * x;

            var bpm = EditorManager.inst.Zoom > closer ? SettingEditor.inst.SnapBPM : EditorManager.inst.Zoom > close ? SettingEditor.inst.SnapBPM / 2f : SettingEditor.inst.SnapBPM / 4f;
            var snapDivisions = EditorConfig.Instance.BPMSnapDivisions.Value * 2f;
            if (timelineGridRenderer && EditorManager.inst.Zoom > unrender && EditorConfig.Instance.TimelineGridEnabled.Value)
            {
                timelineGridRenderer.enabled = false;
                timelineGridRenderer.gridCellSize.x = ((int)bpm / (int)snapDivisions) * (int)clipLength;
                timelineGridRenderer.gridSize.x = clipLength * bpm / (snapDivisions * 1.875f);
                timelineGridRenderer.enabled = true;
            }
            else if (timelineGridRenderer)
                timelineGridRenderer.enabled = false;
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

            if (Example.Current && Example.Current.brain && Example.Current.brain.GetAttribute("SEEN_MORE_BINS").Value == 0.0)
            {
                Example.Current.brain.SetAttribute("SEEN_MORE_BINS", 1.0, MathOperation.Set);
                Example.Current.chatBubble?.Say("Ooh, you found a way to change the bin count! That's awesome!");
            }

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

            if (Example.Current && Example.Current.brain && Example.Current.brain.GetAttribute("SEEN_MORE_BINS").Value == 0.0)
            {
                Example.Current.brain.SetAttribute("SEEN_MORE_BINS", 1.0, MathOperation.Set);
                Example.Current.chatBubble?.Say("Ooh, you found a way to change the bin count! That's awesome!");
            }

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

            if (Example.Current && Example.Current.brain && Example.Current.brain.GetAttribute("SEEN_MORE_BINS").Value == 0.0)
            {
                Example.Current.brain.SetAttribute("SEEN_MORE_BINS", 1.0, MathOperation.Set);
                Example.Current.chatBubble?.Say("Ooh, you found a way to change the bin count! That's awesome!");
            }

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
