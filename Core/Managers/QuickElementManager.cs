using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using UnityEngine;

using TMPro;
using SimpleJSON;

using BetterLegacy.Core.Data;
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

        /// <summary>
        /// Dictionary of quick elements.
        /// </summary>
        public Dictionary<string, Data.QuickElement> quickElements = new Dictionary<string, Data.QuickElement>();

        #endregion

        #region Functions

        public override void OnInit()
        {
            LoadAssetPack(AssetPack.BuiltIn);

            for (int i = 0; i < AssetPack.AssetPacks.Count; i++)
            {
                var assetPack = AssetPack.AssetPacks[i];
                var settings = assetPack.GetSettings();
                if (settings && !settings.enabled)
                    continue;

                LoadAssetPack(assetPack);
            }
        }

        void LoadAssetPack(AssetPack assetPack)
        {
            if (!assetPack.HasDirectory("core/quick_elements"))
                return;

            var directory = assetPack.GetPath("core/quick_elements");
            var files = Directory.GetFiles(directory);
            for (int i = 0; i < files.Length; i++)
            {
                try
                {
                    var quickElement = Data.QuickElement.Parse(JSON.Parse(RTFile.ReadFromFile(files[i])));
                    if (string.IsNullOrEmpty(quickElement.name))
                        continue;
                    quickElements[quickElement.name] = quickElement;
                }
                catch (System.Exception ex)
                {
                    CoreHelper.LogException(ex);
                }
            }
        }

        /// <summary>
        /// Interpolates a quick element.
        /// </summary>
        /// <param name="element">Key of the quick element.</param>
        /// <param name="t">Elapsed time.</param>
        /// <returns>Returns the keyframed text.</returns>
        public string Interpolate(string element, float t) => quickElements.TryGetValue(element, out Data.QuickElement quickElement) ? Interpolate(quickElement, t) : string.Empty;

        /// <summary>
        /// Interpolates a quick element.
        /// </summary>
        /// <param name="quickElement">Quick element reference.</param>
        /// <param name="t">Elapsed time.</param>
        /// <returns>Returns the keyframed text.</returns>
        public string Interpolate(Data.QuickElement quickElement, float t)
        {
            if (quickElement == null || quickElement.keyframes == null || quickElement.keyframes.IsEmpty())
                return string.Empty;

            var times = new List<float>();
            var texts = new List<string>();

            float totalTime = 0f;
            foreach (var kf in quickElement.keyframes)
            {
                texts.Add(kf.text);
                times.Add(totalTime);
                totalTime += kf.time;
            }

            var looping = quickElement.loop;
            var index = times.FindIndex(x => x > (looping ? t % totalTime : t)) - 1;
            if (t < 0f)
                index = 0;

            return index >= 0 && texts.Count > index ? texts[index] : texts.Count > 0 ? texts[texts.Count - 1] : "error";
        }

        /// <summary>
        /// Interpolates a quick element based on <see cref="BeatmapObject.StartTime"/> and current audio time.
        /// </summary>
        /// <param name="beatmapObject">Beatmap object reference.</param>
        /// <param name="element">Key of the quick element.</param>
        /// <returns>Returns the keyframed text.</returns>
        public string Interpolate(BeatmapObject beatmapObject, string element) => Interpolate(element, AudioManager.inst.CurrentAudioSource.time - beatmapObject.StartTime);

        public static IEnumerator PlayQuickElement(TextMeshPro tmp, Data.QuickElement quickElement)
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

        public static IEnumerator UpdateQuickElement(TextMeshProUGUI tmp, List<TextKeyframe> keyframes, int _instance)
        {
            string replaceStr = keyframes[0].text;
            var currentKeyframe = keyframes[0];
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
