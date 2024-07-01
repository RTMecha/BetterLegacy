using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnityEngine;
using UnityEngine.UI;

using BetterLegacy.Core.Data;
using BetterLegacy.Core;

using BaseMarker = DataManager.GameData.BeatmapData.Marker;
using BetterLegacy.Core.Helpers;

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

            content.Find("name").gameObject.Duplicate(content, "desc", 3);
            var descLabel = content.Find("label").gameObject.Duplicate(content, "label", 3);
            descLabel.transform.GetChild(0).GetComponent<Text>().text = "Description";

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

        public static string NoEventLabel => "??? (No event yet)";

        public PAAnimation testAnimation;

        public void Test()
        {
            if (testAnimation == null)
                testAnimation = new PAAnimation("Test", "Test description", 0f, new List<List<EventKeyframe>>
                {
                    new List<EventKeyframe>
                    {
                        new EventKeyframe(0f, new float[] { 0f, 0f, 0f }, new float[] { 0f, 0f, 0f }),
                    }, // Test bin
                    new List<EventKeyframe>
                    {
                        new EventKeyframe(0f, new float[] { 0f, 0f, 0f }, new float[] { 0f, 0f, 0f }),
                    } // Scale
                }, new string[]
                {
                    "Test bin",
                    "Scale",
                }, new List<BaseMarker>
                {
                    new BaseMarker(true, "Test marker", "Test description for a marker", 0, 2f)
                });
            RenderEditor(testAnimation);
        }

        public void RenderEditor(PAAnimation animation)
        {
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

            layers.onValueChanged.ClearAll();
            layers.text = Layer.ToString();
            layers.onValueChanged.AddListener(delegate (string _val)
            {
                if (int.TryParse(_val, out int result))
                {
                    Layer = result;

                    RenderBins(animation.binNames);
                }
            });

            TriggerHelper.AddEventTriggerParams(layers.gameObject, TriggerHelper.ScrollDeltaInt(layers));

            RenderBins(animation.binNames);
        }

        public void RenderBins(params string[] keyframeNames)
        {
            var layer = Layer + 1;

            for (int i = 0; i < keyframeNames.Length; i++)
            {
                var child = i % 4;
                var num = Mathf.Clamp(layer * 4, 0, layer * 4);

                if (child >= timelineLeft.childCount)
                    return;

                var text = timelineLeft.GetChild(child).GetComponent<Text>();

                if (i >= num - 4 && i < num)
                    text.text = keyframeNames[i];
                else if (i < num)
                    text.text = layer == 69 ? "lol" : layer == 555 ? "Hahaha" : NoEventLabel;
                else
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
