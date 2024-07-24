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

namespace BetterLegacy.Editor.Managers
{
    public class AnimationEditor : MonoBehaviour
    {
        public static AnimationEditor inst;

        public static void Init() => Creator.NewGameObject("AnimationEditor", EditorManager.inst.transform.parent).AddComponent<AnimationEditor>();

        void Awake()
        {
            inst = this;

            StartCoroutine(GenerateUI());
        }

        public IEnumerator GenerateUI()
        {
            var dialog = EditorManager.inst.GetDialog("Object Editor").Dialog.gameObject.Duplicate(EditorManager.inst.dialogs, "AnimationEditor");
            dialog.transform.AsRT().sizeDelta = new Vector2(0f, 32f);
            dialog.gameObject.SetActive(false);
            dialogData = dialog.transform.Find("data").AsRT();
            left = dialogData.Find("left").AsRT();
            right = dialogData.Find("right").AsRT();
            content = left.Find("Scroll View/Viewport/Content").AsRT();

            var panel = left.GetChild(0);
            panel.Find("bg").GetComponent<Image>().color = new Color(1f, 0.24f, 0.24f);
            panel.Find("text").GetComponent<Text>().text = "- Animation Editor -";

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

            var desc = content.Find("name").gameObject.Duplicate(content, "desc", 3);
            desc.transform.AsRT().sizeDelta = new Vector2(351f, 74f);
            desc.transform.Find("name").AsRT().sizeDelta = new Vector2(351f, 74f);
            desc.transform.Find("name/Text").GetComponent<Text>().alignment = TextAnchor.UpperLeft;
            desc.transform.Find("name/Placeholder").GetComponent<Text>().alignment = TextAnchor.UpperLeft;
            desc.transform.Find("name").GetComponent<InputField>().lineType = InputField.LineType.MultiLineNewline;

            var descLabel = content.Find("label").gameObject.Duplicate(content, "label", 3);
            descLabel.transform.GetChild(0).GetComponent<Text>().text = "Description";

            content.Find("editor/layers").GetComponent<ContrastColors>().Init(content.Find("editor/layers").GetChild(0).GetComponent<Text>(), content.Find("editor/layers").GetComponent<Image>());

            yield break;
        }

        public RectTransform dialogData;
        public RectTransform left;
        public RectTransform right;
        public RectTransform content;
        public RectTransform timeline;
        public RectTransform timelineLeft;
        public RectTransform timelineRight;

        public InputField layerInputField;

        public List<TimelineObject> timelineObjects = new List<TimelineObject>();

        public RectTransform timelineParent;

        float currentTime;
        public float CurrentTime { get => Mathf.Clamp(currentTime, 0f, float.MaxValue); set => currentTime = Mathf.Clamp(value, 0f, float.MaxValue); }

        int layer;
        public int Layer { get => Mathf.Clamp(layer, 0, int.MaxValue); set => layer = Mathf.Clamp(value, 0, int.MaxValue); }

        public int CurrentObject { get; set; }

        public static string NoEventLabel => "??? (No event yet)";

        public PAAnimation testAnimation;

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
            var id = content.Find("id/text").GetComponent<Text>();
            id.text = $"ID: {animation.id}";

            var clickable = content.Find("id").gameObject.GetComponent<Clickable>() ?? content.Find("id").gameObject.AddComponent<Clickable>();

            clickable.onClick = delegate (PointerEventData pointerEventData)
            {
                EditorManager.inst.DisplayNotification($"Copied ID from {animation.name}!", 2f, EditorManager.NotificationType.Success);
                LSText.CopyToClipboard(animation.id);
            };

            var name = content.Find("name/name").GetComponent<InputField>();
            name.onValueChanged.ClearAll();
            name.text = animation.name;
            name.onValueChanged.AddListener(delegate (string _val)
            {
                animation.name = _val;
            });
            
            var desc = content.Find("desc/name").GetComponent<InputField>();
            desc.onValueChanged.ClearAll();
            desc.text = animation.desc;
            desc.onValueChanged.AddListener(delegate (string _val)
            {
                animation.desc = _val;
            });
            
            var time = content.Find("time/time").GetComponent<InputField>();
            time.onValueChanged.ClearAll();
            time.text = animation.StartTime.ToString();
            time.onValueChanged.AddListener(delegate (string _val)
            {
                if (float.TryParse(_val, out float result))
                    animation.StartTime = result;
            });

            TriggerHelper.IncreaseDecreaseButtons(time, max: float.MaxValue, t: content.Find("time"));
            TriggerHelper.AddEventTriggerParams(content.Find("time").gameObject, TriggerHelper.ScrollDelta(time, max: float.MaxValue));

            var layers = content.Find("editor/layers").GetComponent<InputField>();
            var layersImage = content.Find("editor/layers").GetComponent<Image>();

            layersImage.color = EditorManager.inst.layerColors[Mathf.Clamp(Layer, 0, EditorManager.inst.layerColors.Count - 1)];
            layers.onValueChanged.ClearAll();
            layers.text = Layer.ToString();
            layers.onValueChanged.AddListener(delegate (string _val)
            {
                if (int.TryParse(_val, out int result))
                {
                    Layer = result;
                    layersImage.color = EditorManager.inst.layerColors[Mathf.Clamp(Layer, 0, EditorManager.inst.layerColors.Count - 1)];

                    RenderBins(animation);
                }
            });

            TriggerHelper.AddEventTriggerParams(layers.gameObject, TriggerHelper.ScrollDeltaInt(layers, max: int.MaxValue));

            RenderBins(animation);
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

        public void RenderKeyframe(int type, EventKeyframe eventKeyframe)
        {

        }

        public void RenderMarker()
        {

        }
    }
}
