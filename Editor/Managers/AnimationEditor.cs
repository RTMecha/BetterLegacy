using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnityEngine;
using UnityEngine.UI;

using LSFunctions;

using BetterLegacy.Core.Data;
using BetterLegacy.Core;

using BaseMarker = DataManager.GameData.BeatmapData.Marker;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Components;
using UnityEngine.EventSystems;
using BetterLegacy.Core.Managers;
using BetterLegacy.Configs;

namespace BetterLegacy.Editor.Managers
{
    public class AnimationEditor : MonoBehaviour
    {
        public static AnimationEditor inst;

        public static void Init() => Creator.NewGameObject(nameof(AnimationEditor), EditorManager.inst.transform.parent).AddComponent<AnimationEditor>();

        void Awake()
        {
            inst = this;

            StartCoroutine(GenerateUI());
        }

        void Update()
        {
            var config = EditorConfig.Instance;
            float multiply = Input.GetKey(KeyCode.LeftControl) ? 2f : Input.GetKey(KeyCode.LeftShift) ? 0.1f : 1f;

            if (dialog.activeInHierarchy
                && isOverTimeline
                && !CoreHelper.IsUsingInputField
                && !RTEditor.inst.isOverMainTimeline)
            {
                if (InputDataManager.inst.editorActions.ZoomIn.WasPressed)
                    Zoom = zoomFloat + config.KeyframeZoomAmount.Value * multiply;
                if (InputDataManager.inst.editorActions.ZoomOut.WasPressed)
                    Zoom = zoomFloat - config.KeyframeZoomAmount.Value * multiply;
            }
        }

        public IEnumerator GenerateUI()
        {
            dialog = EditorManager.inst.GetDialog("Object Editor").Dialog.gameObject.Duplicate(EditorManager.inst.dialogs, "AnimationEditor");
            dialog.transform.AsRT().sizeDelta = new Vector2(0f, 32f);
            dialog.gameObject.SetActive(false);
            dialogData = dialog.transform.Find("data").AsRT();
            left = dialogData.Find("left").AsRT();
            right = dialogData.Find("right").AsRT();
            content = left.Find("Scroll View/Viewport/Content").AsRT();

            var panel = left.GetChild(0);
            panel.Find("bg").GetComponent<Image>().color = new Color(1f, 0.24f, 0.24f);
            panel.Find("text").GetComponent<Text>().text = "- Animation Editor -";

            #region Deleting

            string[] names = new string[]
            {
                "tags_label",
                "Tags Scroll View",
                "autokill",
                "parent",
                "parent_more",
                "origin",
                "shape",
                "shapesettings",
                "spacer",
                "depth",
                "rendertype_label",
                "rendertype",
                "collapselabel",
                "applyprefab",
                "assignlabel",
                "assign",
                "remove",
                "int_variable",
                "ignore life",
                "active",
                "Modifiers Scroll View",
            };
            var list = new List<GameObject>();
            for (int i = 0; i < content.childCount; i++)
            {
                var child = content.GetChild(i);

                if (child.name == "id")
                {
                    list.Add(child.Find("ldm").gameObject);
                }

                if (i == 1)
                {
                    list.Add(child.Find("text (1)").gameObject);
                }

                if (child.name == "name")
                {
                    list.Add(child.Find("object-type").gameObject);
                }

                if (child.name == "time")
                {
                    list.Add(child.Find("lock").gameObject);
                }

                if (child.name == "editor")
                {
                    list.Add(child.Find("bin").gameObject);
                }
                
                if (i == 22)
                {
                    list.Add(child.GetChild(1).gameObject);
                }

                if (names.Contains(child.name) || i == 7|| i == 9 || i == 12 || i == 14 || i == 17)
                {
                    list.Add(child.gameObject);
                }
            }

            timeline = dialog.transform.Find("timeline/Scroll View/Viewport").AsRT();
            timelineLeft = timeline.Find("id/left").AsRT();
            timelineRight = timeline.Find("id/right").AsRT();

            list.Add(timelineLeft.Find("position").gameObject);
            list.Add(timelineLeft.Find("scale").gameObject);
            list.Add(timelineLeft.Find("rotation").gameObject);
            list.Add(timelineLeft.Find("color").gameObject);

            for (int i = 0; i < list.Count; i++)
            {
                Destroy(list[i]);
            }

            #endregion

            var desc = content.Find("name").gameObject.Duplicate(content, "desc", 3);
            desc.transform.AsRT().sizeDelta = new Vector2(351f, 74f);
            desc.transform.Find("name").AsRT().sizeDelta = new Vector2(351f, 74f);
            desc.transform.Find("name/Text").GetComponent<Text>().alignment = TextAnchor.UpperLeft;
            desc.transform.Find("name/Placeholder").GetComponent<Text>().alignment = TextAnchor.UpperLeft;
            desc.transform.Find("name").GetComponent<InputField>().lineType = InputField.LineType.MultiLineNewline;

            var descLabel = content.Find("label").gameObject.Duplicate(content, "label", 3);
            descLabel.transform.GetChild(0).GetComponent<Text>().text = "Description";

            content.Find("editor/layers").GetComponent<ContrastColors>().Init(content.Find("editor/layers").GetChild(0).GetComponent<Text>(), content.Find("editor/layers").GetComponent<Image>());
            timelineSlider = timeline.Find("Content/time_slider").GetComponent<Slider>();
            timelinePosScrollbar = timeline.parent.GetComponent<ScrollRect>().horizontalScrollbar;
            timelineGrid = timeline.Find("Content/grid").AsRT();
            zoomSlider = timeline.parent.Find("zoom-panel/Slider").GetComponent<Slider>();

            TriggerHelper.AddEventTriggers(timeline.Find("Content").gameObject, TriggerHelper.CreateEntry(EventTriggerType.PointerEnter, eventData => { isOverTimeline = true; }), TriggerHelper.CreateEntry(EventTriggerType.PointerExit, eventData => { isOverTimeline = false; }));

            #region Markers

            markers = timeline.Find("Content/time_slider/Markers").AsRT();

            #endregion

            yield break;
        }

        public GameObject dialog;
        public RectTransform dialogData;
        public RectTransform left;
        public RectTransform right;
        public RectTransform content;
        public RectTransform timeline;
        public RectTransform timelineLeft;
        public RectTransform timelineRight;
        public RectTransform timelineGrid;
        public RectTransform markers;

        public Slider zoomSlider;
        public Slider timelineSlider;
        public Scrollbar timelinePosScrollbar;
        public InputField layerInputField;

        public bool isOverTimeline;

        public List<TimelineObject> timelineKeyframes = new List<TimelineObject>();
        public List<TimelineMarker> timelineMarkers = new List<TimelineMarker>();

        public RectTransform timelineParent;

        float currentTime;
        public float CurrentTime { get => Mathf.Clamp(currentTime, 0f, float.MaxValue); set => currentTime = Mathf.Clamp(value, 0f, float.MaxValue); }

        int layer;
        public int Layer { get => Mathf.Clamp(layer, 0, int.MaxValue); set => layer = Mathf.Clamp(value, 0, int.MaxValue); }

        public float Zoom
        {
            get => zoomVal;
            set => SetTimeline(value);
        }

        public float zoomFloat;
        public float zoomVal;

        public int CurrentObject { get; set; }

        public static string NoEventLabel => "??? (No event yet)";

        public PAAnimation testAnimation;

        public PAAnimation CurrentAnimation { get; set; }

        public void Test()
        {
            if (testAnimation == null)
                testAnimation = new PAAnimation("Test", "Test description", 0f, new List<PAAnimation.AnimationObject>
                {
                    new PAAnimation.AnimationObject
                    {
                        animationBins = new List<PAAnimation.AnimationBin>
                        {
                            new PAAnimation.AnimationBin
                            {
                                name = "Test bin",
                                events = new List<EventKeyframe>
                                {
                                    new EventKeyframe(0f, new float[] { 0f, 0f, 0f }, new float[] { 0f, 0f, 0f }),
                                }, // Test bin
                            },
                            new PAAnimation.AnimationBin
                            {
                                name = "Scale Test",
                                events = new List<EventKeyframe>
                                {
                                    new EventKeyframe(0f, new float[] { 0f, 0f, 0f }, new float[] { 0f, 0f, 0f }),
                                }, // Scale Test
                            },
                        }
                    }
                },
                new List<BaseMarker>
                {
                    new Marker("Test marker", "Test description for a marker", 0, 2f)
                });
            RenderEditor(testAnimation);
        }

        public void RenderEditor(PAAnimation animation)
        {
            CurrentAnimation = animation;
            var id = content.Find("id/text").GetComponent<Text>();
            id.text = $"ID: {animation.id}";

            var clickable = content.Find("id").gameObject.GetComponent<Clickable>() ?? content.Find("id").gameObject.AddComponent<Clickable>();

            clickable.onClick = pointerEventData =>
            {
                EditorManager.inst.DisplayNotification($"Copied ID from {animation.name}!", 2f, EditorManager.NotificationType.Success);
                LSText.CopyToClipboard(animation.id);
            };

            var name = content.Find("name/name").GetComponent<InputField>();
            name.onValueChanged.ClearAll();
            name.text = animation.name;
            name.onValueChanged.AddListener(_val => { animation.name = _val; });
            
            var desc = content.Find("desc/name").GetComponent<InputField>();
            desc.onValueChanged.ClearAll();
            desc.text = animation.desc;
            desc.onValueChanged.AddListener(_val => { animation.desc = _val; });
            
            var time = content.Find("time/time").GetComponent<InputField>();
            time.onValueChanged.ClearAll();
            time.text = animation.StartTime.ToString();
            time.onValueChanged.AddListener(_val =>
            {
                if (float.TryParse(_val, out float result))
                    animation.StartTime = result;
            });

            TriggerHelper.IncreaseDecreaseButtons(time, max: float.MaxValue, t: content.Find("time"));
            TriggerHelper.AddEventTriggers(content.Find("time").gameObject, TriggerHelper.ScrollDelta(time, max: float.MaxValue));

            var layers = content.Find("editor/layers").GetComponent<InputField>();
            var layersImage = content.Find("editor/layers").GetComponent<Image>();

            layersImage.color = EditorManager.inst.layerColors[Mathf.Clamp(Layer, 0, EditorManager.inst.layerColors.Count - 1)];
            layers.onValueChanged.ClearAll();
            layers.text = Layer.ToString();
            layers.onValueChanged.AddListener(_val =>
            {
                if (int.TryParse(_val, out int result))
                {
                    Layer = result;
                    layersImage.color = EditorManager.inst.layerColors[Mathf.Clamp(Layer, 0, EditorManager.inst.layerColors.Count - 1)];

                    RenderBins(animation);
                }
            });

            TriggerHelper.AddEventTriggers(layers.gameObject, TriggerHelper.ScrollDeltaInt(layers, max: int.MaxValue));

            RenderBins(animation);
            RenderMarkers(animation);
            ResizeKeyframeTimeline(animation);
        }

        public void RenderBins(PAAnimation animation)
        {
            var layer = Layer + 1;

            for (int i = 0; i < Mathf.Clamp(animation.objects[CurrentObject].animationBins.Count, 4, int.MaxValue); i++)
            {
                var child = i % 4;
                var num = Mathf.Clamp(layer * 4, 0, layer * 4);

                if (child >= timelineLeft.childCount)
                    return;

                var text = timelineLeft.GetChild(child).GetComponent<Text>();

                if (i >= num - 4 && i < num && i < animation.objects[CurrentObject].animationBins.Count)
                    text.text = animation.objects[CurrentObject].animationBins[i].name;
                else if (i < num)
                    text.text = layer == 69 ? "lol" : layer == 555 ? "Hahaha" : NoEventLabel;
            }
        }

        public void RenderMarkers(PAAnimation animation)
        {
            var dottedLine = ObjEditor.inst.KeyframeEndPrefab.GetComponent<Image>().sprite;
            LSHelpers.DeleteChildren(markers);

            timelineMarkers.Clear();

            for (int i = 0; i < animation.markers.Count; i++)
            {
                var marker = (Marker)animation.markers[i];

                if (marker.time < 0f)
                    continue;

                int index = i;

                var gameObject = MarkerEditor.inst.markerPrefab.Duplicate(markers, $"Marker {index}");
                var pos = marker.time;
                UIManager.SetRectTransform(gameObject.transform.AsRT(), new Vector2(0f, -12f), new Vector2(pos, 1f), new Vector2(pos, 1f), new Vector2(0.5f, 1f), new Vector2(12f, 12f));

                var timelineMarker = new TimelineMarker
                {
                    GameObject = gameObject,
                    Handle = gameObject.GetComponent<Image>(),
                    Line = gameObject.transform.Find("line").GetComponent<Image>(),
                    Marker = marker,
                    Index = index,
                    RectTransform = gameObject.transform.AsRT(),
                    Text = gameObject.GetComponentInChildren<Text>(),
                };

                timelineMarker.Handle.color = MarkerEditor.inst.markerColors[Mathf.Clamp(marker.color, 0, MarkerEditor.inst.markerColors.Count - 1)];
                timelineMarker.Text.text = marker.name;
                timelineMarker.Line.rectTransform.sizeDelta = new Vector2(5f, 301f);
                timelineMarker.Line.sprite = dottedLine;
                timelineMarker.Line.type = Image.Type.Tiled;

                TriggerHelper.AddEventTriggers(gameObject, TriggerHelper.CreateEntry(EventTriggerType.PointerClick, eventData =>
                {
                    var pointerEventData = (PointerEventData)eventData;

                    if (pointerEventData.button == PointerEventData.InputButton.Left)
                    {
                        CoreHelper.Log($"Select marker: {index}");
                        //RTMarkerEditor.inst.SetCurrentMarker(timelineMarker);
                        //AudioManager.inst.SetMusicTimeWithDelay(Mathf.Clamp(timelineMarker.Marker.time, 0f, AudioManager.inst.CurrentAudioSource.clip.length), 0.05f);
                    }

                    if (pointerEventData.button == PointerEventData.InputButton.Right)
                        CoreHelper.Log($"Delete marker: {index}");
                        //RTMarkerEditor.inst.DeleteMarker(index);

                    if (pointerEventData.button == PointerEventData.InputButton.Middle)
                        CoreHelper.Log($"Set time to marker: {index}");
                        //AudioManager.inst.SetMusicTime(marker.time);
                }));

                timelineMarkers.Add(timelineMarker);
            }
        }

        /// <summary>
        /// Sets the Object Keyframe timeline zoom and position.
        /// </summary>
        /// <param name="zoom">The amount to zoom in.</param>
        /// <param name="position">The position to set the timeline scroll. If the value is less that 0, it will automatically calculate the position to match the audio time.</param>
        /// <param name="render">If the timeline should render.</param>
        public void SetTimeline(float zoom, float position = -1f, bool render = true, bool log = true)
        {
            float prevZoom = zoomFloat;
            zoomFloat = Mathf.Clamp01(zoom);
            zoomVal =
                LSMath.InterpolateOverCurve(ObjEditor.inst.ZoomCurve, ObjEditor.inst.zoomBounds.x, ObjEditor.inst.zoomBounds.y, zoomFloat);

            if (render)
            {
                ResizeKeyframeTimeline(CurrentAnimation);
                RenderKeyframes(CurrentAnimation);
            }

            float timelineCalc = timelineSlider.value;
            if (AudioManager.inst.CurrentAudioSource.clip != null)
            {
                float time = CurrentTime;
                float objectLifeLength = CurrentAnimation.GetLength(CurrentObject) + ObjEditor.inst.ObjectLengthOffset;

                timelineCalc = time / objectLifeLength;
            }

            timelinePosScrollbar.value =
                position >= 0f ? position : timelineCalc;

            zoomSlider.onValueChanged.ClearAll();
            zoomSlider.value = zoomFloat;
            zoomSlider.onValueChanged.AddListener(_val => { Zoom = _val; });

            if (log)
                CoreHelper.Log($"SET ANIMATION ZOOM\n" +
                    $"ZoomFloat: {zoomFloat}\n" +
                    $"ZoomVal: {zoomVal}\n" +
                    $"ZoomBounds: {ObjEditor.inst.zoomBounds}\n" +
                    $"Timeline Position: {timelinePosScrollbar.value}");
        }

        public float TimeTimelineCalc(float _time) => _time * 14f * zoomVal + 5f;

        public GameObject keyframeEnd;
        public void ResizeKeyframeTimeline(PAAnimation animation)
        {
            // ObjEditor.inst.ObjectLengthOffset is the offset from the last keyframe. Could allow for more timeline space.
            float objectLifeLength = animation.GetLength(CurrentObject) + ObjEditor.inst.ObjectLengthOffset;
            float x = TimeTimelineCalc(objectLifeLength);

            timeline.Find("Content").AsRT().sizeDelta = new Vector2(x, 0f);
            timelineGrid.sizeDelta = new Vector2(x, 122f);

            timelineSlider.minValue = animation.StartTime + 0.001f;
            timelineSlider.maxValue = animation.StartTime + objectLifeLength;

            if (!keyframeEnd)
            {
                timelineGrid.DeleteChildren();
                keyframeEnd = ObjEditor.inst.KeyframeEndPrefab.Duplicate(timelineGrid, "end keyframe");
            }

            var rectTransform = keyframeEnd.transform.AsRT();
            rectTransform.sizeDelta = new Vector2(4f, 122f);
            rectTransform.anchoredPosition = new Vector2(animation.GetLength(CurrentObject) * Zoom * 14f, 0f);
        }

        public void RenderKeyframes(PAAnimation animation)
        {

        }

        public void RenderKeyframe(int type, EventKeyframe eventKeyframe)
        {

        }

        public void RenderMarker()
        {

        }
    }
}
