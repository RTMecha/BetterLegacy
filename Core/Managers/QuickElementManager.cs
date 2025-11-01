using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using UnityEngine;

using TMPro;
using SimpleJSON;

using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers.Settings;

namespace BetterLegacy.Core.Managers
{
    /// <summary>
    /// This class manages the QuickElements you see in the PA menus. It can create, save and load new QuickElements.
    /// </summary>
    public class QuickElementManager : BaseManager<QuickElementManager, ManagerSettings>
    {
        #region Values

        public static Dictionary<string, QuickElement> AllQuickElements
        {
            get
            {
                allQuickElements = quickElements.Union(customQuickElements).ToDictionary(x => x.Key, x => x.Value);

                return allQuickElements;
            }
            set
            {
                allQuickElements = value;
            }
        }

        static Dictionary<string, QuickElement> allQuickElements = new Dictionary<string, QuickElement>();
        public static Dictionary<string, QuickElement> customQuickElements = new Dictionary<string, QuickElement>();
        public static Dictionary<string, QuickElement> quickElements = new Dictionary<string, QuickElement>();

        #endregion

        #region Functions

        public override void OnInit()
        {
            var resources = Resources.LoadAll<QuickElement>("terminal/quick-elements");
            if (resources != null)
            {
                foreach (var quickElement in resources)
                    quickElements.TryAdd(quickElement.name, quickElement);

                if (quickElements.TryGetValue("loading_bar_1", out QuickElement loadingBar1))
                    loadingBar1.effects.Add(new QuickElement.Effect { data = new List<string> { "loop" }, name = "loop", });
            }

            if (!quickElements.ContainsKey("blink_loop"))
            {
                var blinkLoop = ScriptableObject.CreateInstance<QuickElement>();
                blinkLoop.name = "blink_loop";
                blinkLoop.keyframes = new List<QuickElement.Keyframe>
                {
                    new QuickElement.Keyframe
                    {
                        text = "(._.)",
                        time = 1f
                    },
                    new QuickElement.Keyframe
                    {
                        text = "(-_-)",
                        time = 0.1f
                    },
                    new QuickElement.Keyframe
                    {
                        text = "(._.)",
                        time = 0.1f
                    },
                    new QuickElement.Keyframe
                    {
                        text = "(-_-)",
                        time = 0.1f
                    },
                    new QuickElement.Keyframe
                    {
                        text = "(._.)",
                        time = 1f
                    },
                };
                blinkLoop.effects = new List<QuickElement.Effect>
                {
                    new QuickElement.Effect
                    {
                        name = "loop",
                        data = new List<string> { "loop" },
                    }
                };

                quickElements.Add("blink_loop", blinkLoop);
            }

            if (RTFile.FileExists(RTFile.GetAsset($"builtin/default_quick_elements{FileFormat.JSON.Dot()}")))
            {
                var jn = JSON.Parse(RTFile.ReadFromFile(RTFile.GetAsset($"builtin/default_quick_elements{FileFormat.JSON.Dot()}")));
                for (int i = 0; i < jn["quick_elements"].Count; i++)
                {
                    var quickElement = Parse(jn["quick_elements"][i]);
                    quickElements[quickElement.name] = quickElement;
                }
            }

            CoroutineHelper.StartCoroutine(LoadExternalQuickElements());
        }

        public static void CreateNewQuickElement(string name)
        {
            if (AllQuickElements.ContainsKey(name))
                return;

            var quickElement = ScriptableObject.CreateInstance<QuickElement>();
            quickElement.name = name;

            quickElement.keyframes = new List<QuickElement.Keyframe>();
            quickElement.effects = new List<QuickElement.Effect>();

            var kf1 = new QuickElement.Keyframe();
            kf1.text = "._.";
            kf1.time = 1f;

            quickElement.keyframes.Add(kf1);

            var kf2 = new QuickElement.Keyframe();
            kf2.text = "-_-";
            kf2.time = 0.1f;

            quickElement.keyframes.Add(kf2);

            var kf3 = new QuickElement.Keyframe();
            kf3.text = "._.";
            kf3.time = 1f;

            quickElement.keyframes.Add(kf3);

            var loop = new QuickElement.Effect();
            loop.name = "loop";
            loop.data = new List<string>();
            loop.data.Add("loop");

            quickElement.effects.Add(loop);

            customQuickElements.Add(name, quickElement);
        }

        public static string ConvertQuickElement(string element, float t) => AllQuickElements.TryGetValue(element, out QuickElement quickElement) ? ConvertQuickElement(quickElement, t) : "";

        public static string ConvertQuickElement(QuickElement quickElement, float t)
        {
            if (quickElement == null || quickElement.keyframes == null || quickElement.keyframes.IsEmpty())
                return "";

            var times = new List<float>();
            var texts = new List<string>();

            float totalTime = 0f;
            foreach (var kf in quickElement.keyframes)
            {
                texts.Add(kf.text);
                times.Add(totalTime);
                totalTime += kf.time;
            }

            var looping = quickElement.effects != null && quickElement.effects.Find(x => !x.data.IsEmpty() && x.name == "loop") != null;
            var index = times.FindIndex(x => x > (looping ? t % totalTime : t)) - 1;
            if (t < 0f)
                index = 0;

            return index >= 0 && texts.Count > index ? texts[index] : texts.Count > 0 ? texts[texts.Count - 1] : "error";
        }

        public static string ConvertQuickElement(BeatmapObject beatmapObject, string element) => ConvertQuickElement(element, AudioManager.inst.CurrentAudioSource.time - beatmapObject.StartTime);

        public static void SaveExternalQuickElements()
        {
            var directory = RTFile.ApplicationDirectory + "beatmaps/quickelements";
            RTFile.CreateDirectory(directory);

            foreach (var quickElementPair in customQuickElements)
            {
                if (quickElements.ContainsKey(quickElementPair.Key))
                    continue;

                var quickElement = quickElementPair.Value;
                var jn = JSON.Parse("{}");

                jn["name"] = quickElement.name;

                for (int i = 0; i < quickElement.keyframes.Count; i++)
                {
                    jn["keys"][i]["text"] = quickElement.keyframes[i].text;
                    jn["keys"][i]["time"] = quickElement.keyframes[i].time.ToString();
                }

                for (int i = 0; i < quickElement.effects.Count; i++)
                {
                    jn["effects"][i]["name"] = quickElement.effects[i].name;
                    for (int j = 0; j < quickElement.effects[i].data.Count; j++)
                        jn["effects"][i]["data"][j] = quickElement.effects[i].data[j];
                }

                RTFile.WriteToFile(RTFile.CombinePaths(directory, RTFile.FormatLegacyFileName(quickElement.name) + FileFormat.LSQE.Dot()), jn.ToString(3));
            }
        }

        public static IEnumerator LoadExternalQuickElements()
        {
            var directory = RTFile.ApplicationDirectory + "beatmaps/quickelements";
            if (!RTFile.DirectoryExists(directory))
                yield break;

            var files = Directory.GetFiles(directory, FileFormat.LSQE.ToPattern());
            foreach (var file in files)
            {
                var jn = JSON.Parse(RTFile.ReadFromFile(file));

                var quickElement = Parse(jn);

                if (!AllQuickElements.ContainsKey(quickElement.name))
                    customQuickElements.Add(quickElement.name, quickElement);
            }

            yield break;
        }

        public static QuickElement Parse(JSONNode jn)
        {
            var quickElement = ScriptableObject.CreateInstance<QuickElement>();

            if (!string.IsNullOrEmpty(jn["name"]))
                quickElement.name = jn["name"];

            try
            {
                ((ScriptableObject)quickElement).name = quickElement.name;
            }
            catch (System.Exception ex)
            {
                Helpers.CoreHelper.LogError($"Error: {ex}");
            }

            quickElement.keyframes = new List<QuickElement.Keyframe>();
            quickElement.effects = new List<QuickElement.Effect>();

            if (jn["keys"] != null)
            {
                for (int i = 0; i < jn["keys"].Count; i++)
                {
                    var keyframe = new QuickElement.Keyframe();
                    keyframe.text = jn["keys"][i]["text"];

                    keyframe.time = 1f;
                    if (float.TryParse(jn["keys"][i]["time"], out float result))
                        keyframe.time = result;

                    quickElement.keyframes.Add(keyframe);
                }
            }
            else
            {
                var keyframe = new QuickElement.Keyframe();
                keyframe.text = "null";
                keyframe.time = 1f;

                quickElement.keyframes.Add(keyframe);
            }

            if (jn["effects"] != null)
            {
                for (int i = 0; i < jn["effects"].Count; i++)
                {
                    var effect = new QuickElement.Effect();
                    effect.name = jn["effects"][i]["name"];
                    effect.data = new List<string>();
                    for (int j = 0; j < jn["effects"][i]["data"].Count; j++)
                        effect.data.Add(jn["effects"][i]["data"][j]);

                    quickElement.effects.Add(effect);
                }
            }

            return quickElement;
        }

        public static IEnumerator PlayQuickElement(TextMeshPro tmp, QuickElement quickElement)
        {
            string replaceStr = quickElement.keyframes[0].text;
            var currentKeyframe = quickElement.keyframes[0];

            for (int i = 0; i < quickElement.keyframes.Count; i++)
            {
                string newText = tmp.text;
                yield return CoroutineHelper.Seconds(currentKeyframe.time);

                currentKeyframe = quickElement.keyframes[i];
                newText = newText.Replace(replaceStr, currentKeyframe.text);
                if (tmp.text.Contains(replaceStr))
                    tmp.SetText(newText);

                replaceStr = quickElement.keyframes[i].text;
                newText = null;
            }

            yield break;
        }

        public static IEnumerator UpdateQuickElement(TextMeshProUGUI tmp, List<QuickElement.Keyframe> keyframes, int _instance)
        {
            string replaceStr = keyframes[0].text;
            QuickElement.Keyframe currentKeyframe = keyframes[0];
            int i = 0;
            while (tmp.text.Contains(replaceStr))
            {
                string newText = tmp.text;
                yield return CoroutineHelper.Seconds(currentKeyframe.time);
                int num;
                for (int inst = 0; inst <= _instance; inst = num + 1)
                {
                    yield return CoroutineHelper.EndOfFrame;
                    num = inst;
                }
                num = i;
                i = num + 1;
                if (i > keyframes.Count - 1)
                    i = 0;
                currentKeyframe = keyframes[i];
                newText = newText.Replace(replaceStr, currentKeyframe.text);
                if (tmp.text.Contains(replaceStr))
                {
                    tmp.SetText(newText);
                }
                replaceStr = keyframes[i].text;
                newText = null;
            }
            yield break;
        }

        #endregion
    }
}
