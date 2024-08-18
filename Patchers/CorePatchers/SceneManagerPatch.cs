using BetterLegacy.Configs;
using BetterLegacy.Core;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using BetterLegacy.Example;
using HarmonyLib;
using LSFunctions;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

#pragma warning disable Harmony003 // Harmony non-ref patch parameters modified
namespace BetterLegacy.Patchers
{
    [HarmonyPatch(typeof(SceneManager))]
    public class SceneManagerPatch
    {
        public static SceneManager Instance => SceneManager.inst;

        public static bool loading;
        public static Text loadingText;
        public static string previousScene;
        public static Image loadingImage;
        public static float startLoadTime;

        [HarmonyPatch(nameof(SceneManager.Awake))]
        [HarmonyPostfix]
        static void AwakePostfix(SceneManager __instance)
        {
            loadingText = __instance.icon.GetComponent<Text>();

            if (!loadingImage && !__instance.canvas.transform.Find("loading sprite"))
            {
                loadingImage = Creator.NewUIObject("loading sprite", __instance.canvas.transform).AddComponent<Image>();
                UIManager.SetRectTransform(loadingImage.rectTransform, new Vector2(-172f, 80f), Vector2.right, Vector2.right, new Vector2(0.5f, 0.5f), new Vector2(100f, 100f));
                loadingImage?.gameObject?.SetActive(false);
            }
            else if (__instance.canvas.transform.Find("loading sprite"))
            {
                loadingImage = __instance.canvas.transform.Find("loading sprite").GetComponent<Image>();
                loadingImage?.gameObject?.SetActive(false);
            }
        }

        [HarmonyPatch(nameof(SceneManager.Update))]
        [HarmonyPostfix]
        static void UpdatePostfix()
        {
            try
            {
                var loadingDisplayType = CoreConfig.Instance.LoadingDisplayType.Value;
                if (loading && (loadingDisplayType == LoadingDisplayType.Waveform || loadingDisplayType == LoadingDisplayType.Doggo) && loadingImage.isActiveAndEnabled)
                {
                    var time = Time.time - startLoadTime;
                    var t = time % Instance.loadingTextures.Length;
                    loadingImage.sprite = Instance.loadingTextures[(int)t];
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
                if (loadingText == null && Instance.icon)
                    loadingText = Instance.icon.GetComponent<Text>();

                if (loadingText.isActiveAndEnabled)
                    loadingText.text = $"<b>Loading : {_value}%</b>";
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
            ExampleManager.onSceneLoad?.Invoke(__0);

            CoreHelper.CurrentSceneType = __0 == "Editor" ? SceneType.Editor : __0 == "Game" ? SceneType.Game : SceneType.Interface;
            CoreHelper.Log($"Set Scene\nType: {CoreHelper.CurrentSceneType}\nName: {__0}");

            try
            {
                previousScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            }
            catch (System.Exception ex)
            {
                CoreHelper.LogError($"Error: {ex}");
            }

            __result = DisplayLoadingScreen(__0, __1);
            return false;
        }

        public static void SetActive(bool active)
        {
            Instance.background.SetActive(active);
            Instance.icon.SetActive(active);
        }

        public static bool displayLoadingText = true;

        public static void UpdateProgress(float progress)
        {
            try
            {
                if (!loadingText && Instance.icon)
                    loadingText = Instance.icon.GetComponent<Text>();

                if (!loadingImage && !Instance.canvas.transform.Find("loading sprite"))
                    loadingImage = Creator.NewUIObject("loading sprite", Instance.canvas.transform).AddComponent<Image>();
                else if (Instance.canvas.transform.Find("loading sprite"))
                    loadingImage = Instance.canvas.transform.Find("loading sprite").GetComponent<Image>();

                var loadingDisplayType = CoreConfig.Instance.LoadingDisplayType.Value;
                switch (loadingDisplayType)
                {
                    case LoadingDisplayType.Bar:
                        {
                            loadingImage?.gameObject?.SetActive(false);
                            if (loadingText.isActiveAndEnabled)
                                loadingText.text = $"<b>{(displayLoadingText ? "Loading : " : "")}[ {FontManager.TextTranslater.ConvertBar("▓", progress)} ]</b>";
                            break;
                        }
                    case LoadingDisplayType.EqualsBar:
                        {
                            loadingImage?.gameObject?.SetActive(false);
                            if (loadingText.isActiveAndEnabled)
                                loadingText.text = $"<b>{(displayLoadingText ? "Loading : " : "")}[ {FontManager.TextTranslater.ConvertBar("=", progress)} ]</b>";
                            break;
                        }
                    case LoadingDisplayType.Percentage:
                        {
                            loadingImage?.gameObject?.SetActive(false);
                            if (loadingText.isActiveAndEnabled)
                                loadingText.text = $"<b>{(displayLoadingText ? "Loading : " : "")}{progress.ToString("F0")}%</b>";
                            break;
                        }
                    case LoadingDisplayType.Waveform:
                        {
                            loadingImage?.gameObject?.SetActive(true);
                            if (loadingText.isActiveAndEnabled)
                                loadingText.text = "";

                            break;
                        }
                    case LoadingDisplayType.Doggo:
                        {
                            loadingImage?.gameObject?.SetActive(true);
                            if (loadingText.isActiveAndEnabled)
                                loadingText.text = "";

                            break;
                        }
                }
            }
            catch
            {

            }

        }

        static IEnumerator DisplayLoadingScreen(string _level, bool _showLoading = true)
        {
            startLoadTime = Time.time;
            AudioManager.inst.SetPitch(1f);
            loading = true;
            SetActive(_showLoading);

            if (_showLoading)
                Instance.background.GetComponent<Image>().color = LSColors.HexToColor(DataManager.inst.GetSettingEnumValues("UITheme", 0)["bg"]);

            var async = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(_level);
            async.allowSceneActivation = false;
            if (_showLoading)
                UpdateProgress(0f);

            while (!async.isDone)
            {
                if (_showLoading)
                    UpdateProgress(async.progress * 100f);

                if (async.progress >= 0.9f)
                {
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
            while (CoreHelper.InEditor && EditorManager.inst.loading || CoreHelper.Loading || CoreHelper.Parsing)
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

#pragma warning restore Harmony003 // Harmony non-ref patch parameters modified