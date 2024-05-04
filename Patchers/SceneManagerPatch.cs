using BetterLegacy.Core.Helpers;
using BetterLegacy.Example;
using HarmonyLib;
using LSFunctions;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace BetterLegacy.Patchers
{
    [HarmonyPatch(typeof(SceneManager))]
    public class SceneManagerPatch
    {
        public static SceneManager Instance => SceneManager.inst;

        public static bool loading;
        public static Text loadingText;
        [HarmonyPatch("Awake")]
        [HarmonyPrefix]
        static void AwakePrefix(SceneManager __instance)
        {
            loadingText = __instance.icon.GetComponent<Text>();
        }

        [HarmonyPatch("UpdateSpinner")]
        [HarmonyPrefix]
        static bool UpdateSpinnerPrefix(string _value)
        {
            try
            {
                if (loadingText.isActiveAndEnabled)
                    loadingText.text = $"<b>Loading : {_value}</b>";
            }
            catch
            {

            }

            return false;
        }

        [HarmonyPatch("DisplayLoadingScreen")]
        [HarmonyPrefix]
        static void DisplayLoadingScreenPrefix(string __0)
        {
            ExampleManager.onSceneLoad?.Invoke(__0);
            CoreHelper.CurrentSceneType = __0 == "Editor" ? SceneType.Editor : __0 == "Game" ? SceneType.Game : SceneType.Interface;
            CoreHelper.Log($"Set Scene\nType: {CoreHelper.CurrentSceneType}\nName: {__0}");
            loading = true;
        }

        [HarmonyPatch("DisplayLoadingScreen")]
        [HarmonyPostfix]
        static void DisplayLoadingScreenPostfix() => loading = false;

        [HarmonyPatch("DisplayLoadingScreen")]
        [HarmonyPrefix]
        static bool DisplayLoadingScreenPrefix(ref IEnumerator __result, string __0, bool __1 = true)
        {
            ExampleManager.onSceneLoad?.Invoke(__0);

            CoreHelper.CurrentSceneType = __0 == "Editor" ? SceneType.Editor : __0 == "Game" ? SceneType.Game : SceneType.Interface;
            CoreHelper.Log($"Set Scene\nType: {CoreHelper.CurrentSceneType}\nName: {__0}");

            __result = DisplayLoadingScreen(__0, __1);
            return false;
        }

        public static void SetActive(bool active)
        {
            Instance.background.SetActive(active);
            Instance.icon.SetActive(active);
        }

        static IEnumerator DisplayLoadingScreen(string _level, bool _showLoading = true)
        {
            AudioManager.inst.SetPitch(1f);
            loading = true;
            SetActive(_showLoading);

            if (_showLoading)
                Instance.background.GetComponent<Image>().color = LSColors.HexToColor(DataManager.inst.GetSettingEnumValues("UITheme", 0)["bg"]);

            var async = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(_level);
            async.allowSceneActivation = false;
            string text = "0%";
            if (_showLoading)
                Instance.UpdateSpinner(text);

            while (!async.isDone)
            {
                if (async.progress < 0.9f)
                {
                    text = (async.progress * 100f).ToString("F0") + "%";
                    if (_showLoading)
                        Instance.UpdateSpinner(text);
                }
                else
                {
                    if (_showLoading)
                        Instance.UpdateSpinner("100%");
                    yield return new WaitForSecondsRealtime(0.1f);
                    async.allowSceneActivation = true;
                }

                yield return null;
            }

            if (!GameManager.inst)
            {
                SetActive(false);
                loading = false;
                yield break;
            }

            yield return new WaitForSecondsRealtime(0.1f);
            while (EditorManager.inst && EditorManager.inst.loading || GameManager.inst.gameState == GameManager.State.Loading || GameManager.inst.gameState == GameManager.State.Parsing)
            {
                SetActive(true);
                yield return new WaitForEndOfFrame();
            }
            SetActive(false);

            loading = false;

            yield break;
        }
    }
}
