using SimpleJSON;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using BeatmapObject = DataManager.GameData.BeatmapObject;

namespace BetterLegacy.Core.Managers
{
    /// <summary>
    /// This class manages the QuickElements you see in the PA menus. It can create, save and load new QuickElements.
    /// </summary>
    public class QuickElementManager : MonoBehaviour
    {
        public static QuickElementManager inst;

        void Awake()
        {
            inst = this;

            if (Resources.LoadAll<QuickElement>("terminal/quick-elements") != null)
            {
                foreach (QuickElement quickElement in Resources.LoadAll<QuickElement>("terminal/quick-elements"))
                {
                    if (!quickElements.ContainsKey(quickElement.name))
                        quickElements.Add(quickElement.name, quickElement);
                }
            }

            inst.StartCoroutine(LoadExternalQuickElements());
        }

        public static Dictionary<string, QuickElement> AllQuickElements
        {
            get
            {
                foreach (var qe in quickElements)
                {
                    if (!allQuickElements.ContainsKey(qe.Key))
                    {
                        allQuickElements.Add(qe.Key, qe.Value);
                    }
                }
                
                foreach (var qe in customQuickElements)
                {
                    if (!allQuickElements.ContainsKey(qe.Key))
                    {
                        allQuickElements.Add(qe.Key, qe.Value);
                    }
                }

                return allQuickElements;
            }
            set
            {
                allQuickElements = value;
            }
        }

        private static Dictionary<string, QuickElement> allQuickElements = new Dictionary<string, QuickElement>();
        public static Dictionary<string, QuickElement> customQuickElements = new Dictionary<string, QuickElement>();
        public static Dictionary<string, QuickElement> quickElements = new Dictionary<string, QuickElement>();

        public static void CreateNewQuickElement(string name)
        {
            if (!AllQuickElements.ContainsKey(name))
            {
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
        }

        public static string ConvertQuickElement(BeatmapObject beatmapObject, string element)
        {
            if (AllQuickElements.ContainsKey(element) && AllQuickElements[element].keyframes.Count > 0)
            {
                var quickElement = AllQuickElements[element];

                if (quickElement.effects != null && quickElement.effects.Find(x => x.data.Count > 0 && x.name == "loop") != null)
                {
                    var times = new List<float>();
                    var texts = new List<string>();

                    float totaltime = 0f;
                    foreach (var kf in quickElement.keyframes)
                    {
                        texts.Add(kf.text);
                        times.Add(totaltime);
                        totaltime += kf.time;
                    }

                    var currentTime = AudioManager.inst.CurrentAudioSource.time - beatmapObject.StartTime;
                    var index = times.FindIndex(x => x > currentTime % totaltime) - 1;

                    if (index >= 0 && texts.Count > index)
                        return texts[index];
                    else if (texts.Count > 0)
                        return texts[texts.Count - 1];
                    else
                        return "error";
                }
                else
                {
                    var times = new List<float>();
                    var texts = new List<string>();

                    float totaltime = 0f;
                    foreach (var kf in quickElement.keyframes)
                    {
                        texts.Add(kf.text);
                        times.Add(totaltime);
                        totaltime += kf.time;
                    }

                    var currentTime = AudioManager.inst.CurrentAudioSource.time - beatmapObject.StartTime;
                    var index = times.FindIndex(x => x > currentTime) - 1;

                    if (index >= 0 && texts.Count > index)
                        return texts[index];
                    else if (texts.Count > 0)
                        return texts[texts.Count - 1];
                    else
                        return "error";
                }
            }

            return "";
        }

        public static void SaveExternalQuickElements()
        {
            if (!RTFile.DirectoryExists(RTFile.ApplicationDirectory + "beatmaps/quickelements"))
            {
                Directory.CreateDirectory(RTFile.ApplicationDirectory + "beatmaps/quickelements");
            }

            foreach (var quickElement in customQuickElements)
            {
                if (!quickElements.ContainsKey(quickElement.Key))
                {
                    var jn = JSON.Parse("{}");

                    jn["name"] = quickElement.Value.name;

                    for (int i = 0; i < quickElement.Value.keyframes.Count; i++)
                    {
                        jn["keys"][i]["text"] = quickElement.Value.keyframes[i].text;
                        jn["keys"][i]["time"] = quickElement.Value.keyframes[i].time.ToString();
                    }

                    for (int i = 0; i < quickElement.Value.effects.Count; i++)
                    {
                        jn["effects"][i]["name"] = quickElement.Value.effects[i].name;
                        for (int j = 0; j < quickElement.Value.effects[i].data.Count; j++)
                        {
                            jn["effects"][i]["data"][j] = quickElement.Value.effects[i].data[j];
                        }
                    }

                    RTFile.WriteToFile("beatmaps/quickelements/" + quickElement.Value.name + ".lsqe", jn.ToString(3));
                }
            }
        }

        public static IEnumerator LoadExternalQuickElements()
        {
            if (RTFile.DirectoryExists(RTFile.ApplicationDirectory + "beatmaps/quickelements"))
            {
                var files = Directory.GetFiles(RTFile.ApplicationDirectory + "beatmaps/quickelements", "*.lsqe");
                foreach (var file in files)
                {
                    var json = FileManager.inst.LoadJSONFileRaw(file);
                    var jn = JSON.Parse(json);

                    var quickElement = ScriptableObject.CreateInstance<QuickElement>();

                    quickElement.name = Path.GetFileName(file).Replace(".lsqe", "");
                    if (!string.IsNullOrEmpty(jn["name"]))
                    {
                        quickElement.name = jn["name"];
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
                            {
                                keyframe.time = result;
                            }

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
                            {
                                effect.data.Add(jn["effects"][i]["data"][j]);
                            }

                            quickElement.effects.Add(effect);
                        }
                    }

                    if (!AllQuickElements.ContainsKey(quickElement.name))
                        customQuickElements.Add(quickElement.name, quickElement);
                }
            }

            yield break;
        }

        public static IEnumerator PlayQuickElement(TextMeshPro tmp, QuickElement quickElement)
        {
            string replaceStr = quickElement.keyframes[0].text;
            var currentKeyframe = quickElement.keyframes[0];

            for (int i = 0; i < quickElement.keyframes.Count; i++)
            {
                string newText = tmp.text;
                yield return new WaitForSeconds(currentKeyframe.time);

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
                yield return new WaitForSeconds(currentKeyframe.time);
                int num;
                for (int inst = 0; inst <= _instance; inst = num + 1)
                {
                    yield return new WaitForEndOfFrame();
                    num = inst;
                }
                num = i;
                i = num + 1;
                if (i > keyframes.Count - 1)
                {
                    i = 0;
                }
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
    }
}
