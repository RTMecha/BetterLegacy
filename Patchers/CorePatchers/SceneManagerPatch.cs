using System.Collections;

using UnityEngine;
using UnityEngine.UI;

using HarmonyLib;

using BetterLegacy.Configs;
using BetterLegacy.Core;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;

namespace BetterLegacy.Patchers
{
    [HarmonyPatch(typeof(SceneManager))]
    public class SceneManagerPatch
    {
        public static SceneManager Instance => SceneManager.inst;

        [HarmonyPatch(nameof(SceneManager.Awake))]
        [HarmonyPostfix]
        static void AwakePostfix(SceneManager __instance)
        {
            SceneHelper.loadingText = __instance.icon.GetComponent<Text>();

            if (!SceneHelper.loadingImage && !__instance.canvas.transform.Find("loading sprite"))
            {
                SceneHelper.loadingImage = Creator.NewUIObject("loading sprite", __instance.canvas.transform).AddComponent<Image>();
                UIManager.SetRectTransform(SceneHelper.loadingImage.rectTransform, new Vector2(-172f, 80f), Vector2.right, Vector2.right, new Vector2(0.5f, 0.5f), new Vector2(100f, 100f));
                if (SceneHelper.loadingImage)
                    SceneHelper.loadingImage.gameObject.SetActive(false);
            }
            else if (__instance.canvas.transform.Find("loading sprite"))
            {
                SceneHelper.loadingImage = __instance.canvas.transform.Find("loading sprite").GetComponent<Image>();
                if (SceneHelper.loadingImage)
                    SceneHelper.loadingImage.gameObject.SetActive(false);
            }
        }

        [HarmonyPatch(nameof(SceneManager.Update))]
        [HarmonyPostfix]
        static void UpdatePostfix()
        {
            try
            {
                var loadingDisplayType = CoreConfig.Instance.LoadingDisplayType.Value;
                if (SceneHelper.Loading && (loadingDisplayType == LoadingDisplayType.Waveform || loadingDisplayType == LoadingDisplayType.Doggo) && SceneHelper.loadingImage.isActiveAndEnabled)
                {
                    var time = Time.time - SceneHelper.startLoadTime;
                    var t = time % Instance.loadingTextures.Length;
                    SceneHelper.loadingImage.sprite = Instance.loadingTextures[(int)t];
                }
            }
            catch
            {

            }
        }

        [HarmonyPatch(nameof(SceneManager.UpdateSpinner))]
        [HarmonyPrefix]
        static bool UpdateSpinnerPrefix(string _value)
        {
            try
            {
                if (SceneHelper.loadingText == null && Instance.icon)
                    SceneHelper.loadingText = Instance.icon.GetComponent<Text>();

                if (SceneHelper.loadingText.isActiveAndEnabled)
                    SceneHelper.loadingText.text = $"<b>Loading : {_value}%</b>";
            }
            catch
            {

            }

            return false;
        }

        [HarmonyPatch(nameof(SceneManager.DisplayLoadingScreen))]
        [HarmonyPrefix]
        static bool DisplayLoadingScreenPrefix(ref IEnumerator __result, string __0, bool __1 = true)
        {
            __result = SceneHelper.ILoadScene(__0, __1);
            return false;
        }
    }
}