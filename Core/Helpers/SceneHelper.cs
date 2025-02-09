using BetterLegacy.Configs;
using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Managers;
using BetterLegacy.Example;
using BetterLegacy.Menus;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace BetterLegacy.Core.Helpers
{
    /// <summary>
    /// Unity Scene helper class.
    /// </summary>
    public static class SceneHelper
    {
        #region Properties

        /// <summary>
        /// The current scene PA is in.
        /// </summary>
        public static string CurrentScene { get; set; } = "Main Menu";

        /// <summary>
        /// The current type of scene PA is in.
        /// </summary>
        public static SceneType CurrentSceneType => CurrentScene == "Editor" ? SceneType.Editor : CurrentScene == "Game" ? SceneType.Game : SceneType.Interface;

        /// <summary>
        /// The previous scene that was loaded.
        /// </summary>
        public static string PreviousScene { get; set; }

        /// <summary>
        /// True if a scene is loading, otherwise false.
        /// </summary>
        public static bool Loading { get; set; }

        /// <summary>
        /// Action to run when a scene is finished loading.
        /// </summary>
        public static Action<string> OnSceneLoad { get; set; }

        #endregion

        #region Wrapper Methods

        /// <summary>
        /// Loads the Editor scene with a progress screen.
        /// </summary>
        public static void LoadEditorWithProgress() => LoadScene(SceneName.Editor);

        /// <summary>
        /// Loads the Game scene with a progress screen.
        /// </summary>
        public static void LoadGameWithProgress() => LoadScene(SceneName.Game);

        /// <summary>
        /// Loads the Input Select scene.
        /// </summary>
        public static void LoadInputSelect() => LoadScene(SceneName.Input_Select, false);

        /// <summary>
        /// Loads the Input Select scene with a custom inputs selected function.
        /// </summary>
        /// <param name="onInputsSelected">Function to run when the user wants to continue past the Input Select menu.</param>
        public static void LoadInputSelect(Action onInputsSelected)
        {
            LevelManager.OnInputsSelected = onInputsSelected;
            LoadInputSelect();
        }

        /// <summary>
        /// Loads the Interface scene.
        /// </summary>
        public static void LoadInterfaceScene() => LoadScene(SceneName.Interface);

        /// <summary>
        /// Loads the Editor scene.
        /// </summary>
        public static void LoadEditor() => LoadScene(SceneName.Editor, false);

        /// <summary>
        /// Loads the Game scene.
        /// </summary>
        public static void LoadGame() => LoadScene(SceneName.Game, false);

        /// <summary>
        /// Loads a scene matching the <see cref="SceneName"/> enum value.
        /// </summary>
        /// <param name="sceneName">Scene to load.</param>
        /// <param name="onSceneLoad">Sets an action to occur when the scene is done loading.</param>
        /// <param name="showLoading">If the progress screen should display.</param>
        public static void LoadScene(SceneName sceneName, Action<string> onSceneLoad, bool showLoading = true)
        {
            OnSceneLoad = onSceneLoad;
            LoadScene(sceneName, showLoading);
        }

        /// <summary>
        /// Loads a scene matching the <see cref="SceneName"/> enum value.
        /// </summary>
        /// <param name="sceneName">Scene to load.</param>
        /// <param name="showLoading">If the progress screen should display.</param>
        public static void LoadScene(SceneName sceneName, bool showLoading = true) => SceneManager.inst.LoadScene(sceneName.ToName(), showLoading);

        public static IEnumerator ILoadScene(SceneName sceneName, bool showLoading = true)
        {
            SceneManager.inst.currentLevel = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            yield return CoreHelper.StartCoroutine(ILoadScene(sceneName.ToName(), showLoading));
        }

        #endregion

        #region Base Methods

        /// <summary>
        /// Sets the loading screen active / inactive.
        /// </summary>
        /// <param name="active">Active state to set to the loading screen.</param>
        public static void SetActive(bool active)
        {
            SceneManager.inst.background.SetActive(active);
            SceneManager.inst.icon.SetActive(active);

            if (!active && loadingImage)
                loadingImage.gameObject.SetActive(false);
        }

        /// <summary>
        /// Updates the loading screen progress.
        /// </summary>
        /// <param name="progress"></param>
        public static void UpdateProgress(float progress)
        {
            try
            {
                if (!loadingText && SceneManager.inst.icon)
                    loadingText = SceneManager.inst.icon.GetComponent<Text>();

                if (!loadingImage && !SceneManager.inst.canvas.transform.Find("loading sprite"))
                    loadingImage = Creator.NewUIObject("loading sprite", SceneManager.inst.canvas.transform).AddComponent<Image>();
                else if (SceneManager.inst.canvas.transform.Find("loading sprite"))
                    loadingImage = SceneManager.inst.canvas.transform.Find("loading sprite").GetComponent<Image>();

                var loadingDisplayType = CoreConfig.Instance.LoadingDisplayType.Value;
                if (loadingImage)
                {
                    var imageActive = loadingDisplayType == LoadingDisplayType.Doggo || loadingDisplayType == LoadingDisplayType.Waveform;
                    loadingImage.gameObject.SetActive(imageActive);
                    if (imageActive)
                        loadingImage.color = InterfaceManager.inst.CurrentTheme.GetObjColor(6);
                }
                if (loadingText && loadingText.isActiveAndEnabled)
                {
                    loadingText.color = InterfaceManager.inst.CurrentTheme.GetObjColor(6);
                    loadingText.text = loadingDisplayType switch
                    {
                        LoadingDisplayType.Bar => $"<b>Loading [ {RTString.ConvertBar("▓", progress)} ]</b>",
                        LoadingDisplayType.EqualsBar => $"<b>Loading [ {RTString.ConvertBar("=", progress)} ]</b>",
                        LoadingDisplayType.Percentage => $"<b>Loading {progress.ToString("F0")}%</b>",
                        _ => string.Empty
                    };
                }
            }
            catch
            {

            }

        }

        /// <summary>
        /// Scene loader coroutine.
        /// </summary>
        /// <param name="level">Scene to load.</param>
        /// <param name="showLoading">If the progress screen should display.</param>
        public static IEnumerator ILoadScene(string level, bool showLoading = true)
        {
            ExampleManager.onSceneLoad?.Invoke(level);
            
            PreviousScene = CurrentScene;
            CurrentScene = level;

            CoreHelper.Log($"Set Scene\nType: {CurrentSceneType}\nName: {level}");

            startLoadTime = Time.time;
            AudioManager.inst.SetPitch(1f);
            Loading = true;
            SetActive(showLoading);

            if (showLoading)
            {
                try
                {
                    CoreHelper.StartCoroutine(LoopSprite(0.1f));
                    if (SceneManager.inst.background.TryGetComponent(out Image image) && InterfaceManager.inst && InterfaceManager.inst.CurrentTheme is BeatmapTheme beatmapTheme)
                        image.color = beatmapTheme.backgroundColor;
                }
                catch (Exception ex)
                {
                    CoreHelper.LogException(ex);
                }
            }

            var async = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(level);
            async.allowSceneActivation = false;
            if (showLoading)
                UpdateProgress(0f);

            while (!async.isDone)
            {
                if (showLoading)
                    UpdateProgress(async.progress * 100f);

                if (async.progress >= 0.9f)
                {
                    yield return new WaitForSecondsRealtime(0.1f);
                    async.allowSceneActivation = true;
                }

                yield return null;
            }

            if (!CoreHelper.InGame)
            {
                SetActive(false);
                Loading = false;
                InvokeSceneLoad(level);
                yield break;
            }

            yield return new WaitForSecondsRealtime(0.1f);
            while (CoreHelper.InEditor && EditorManager.inst.loading || CoreHelper.Loading || CoreHelper.Parsing)
            {
                SetActive(true);
                yield return new WaitForEndOfFrame();
            }
            SetActive(false);

            Loading = false;

            InvokeSceneLoad(level);

            yield break;
        }

        static IEnumerator LoopSprite(float delay)
        {
            var loadingDisplayType = CoreConfig.Instance.LoadingDisplayType.Value;
            int num = 0;
            while (Loading && (loadingDisplayType == LoadingDisplayType.Waveform || loadingDisplayType == LoadingDisplayType.Doggo) && loadingImage.isActiveAndEnabled)
            {
                loadingImage.sprite = SceneManager.inst.loadingTextures[num];
                yield return new WaitForSeconds(delay);
                num++;
                if (num >= SceneManager.inst.loadingTextures.Length)
                    num = 0;
            }
        }

        static void InvokeSceneLoad(string level)
        {
            OnSceneLoad?.Invoke(level);
            OnSceneLoad = null;
        }

        #endregion

        #region Fields

        public static Text loadingText;
        public static Image loadingImage;
        public static float startLoadTime;

        #endregion
    }
}
