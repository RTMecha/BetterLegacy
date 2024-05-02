using System;
using System.Linq;

using UnityEngine;
using UnityEngine.UI;
using LSFunctions;
using TMPro;
using BetterLegacy.Components;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Configs;
using BetterLegacy.Core.Data;

namespace BetterLegacy.Core.Managers
{
    public static class RTDebugger
    {
        public static void TogglePlay() => (AudioManager.inst.CurrentAudioSource.isPlaying ? (Action)AudioManager.inst.CurrentAudioSource.Pause : AudioManager.inst.CurrentAudioSource.Play).Invoke();

        public static int BeatmapObjectAliveCount() => GameData.Current.BeatmapObjects.Where(x => x.objectType != BeatmapObject.ObjectType.Empty && x.TimeWithinLifespan()).Count();

        public static void LogColor(Color color) => Debug.Log($"[<color=#{CoreHelper.ColorToHex(color)}>▓▓▓▓▓▓▓▓▓▓▓▓▓</color>]");
        public static void LogColor(string color) => Debug.Log($"[<color={color}>▓▓▓▓▓▓▓▓▓▓▓▓▓</color>]");

        public static void LogClassNames()
        {
            // Mod Class Names
            var str = $"\n{LegacyPlugin.className}";

            str += RTVideoManager.className;

            // Base Class Names
            str += DataManager.inst.className;
            str += SceneManager.inst.className;
            str += FileManager.className;
            str += DiscordController.inst.className;
            str += SteamWrapper.className;
            str += AudioManager.inst.className;
            str += AudioManager.inst.library.className;
            str += InputDataManager.className;
            str += SaveManager.inst.className;

            str += GameManager.inst.className;

            // Editor Class Names
            str += EditorManager.inst.className;
            str += ObjEditor.inst.className;
            str += PrefabEditor.inst.className;
            str += EventEditor.inst.className;
            str += BackgroundEditor.inst.className;
            str += CheckpointEditor.inst.className;
            str += SettingEditor.inst.className;
            str += MetadataEditor.inst.className;

            Debug.Log(str);
        }

        public static FPSCounter FPS { get; set; }

        public static TextMeshProUGUI Info { get; set; }

        public static bool init = false;

        public static void Init()
        {
            var inter = new GameObject("Debug Info");
            UnityEngine.Object.DontDestroyOnLoad(inter);
            inter.transform.localScale = Vector3.one * CoreHelper.ScreenScale;
            var interfaceRT = inter.AddComponent<RectTransform>();
            interfaceRT.anchoredPosition = new Vector2(960f, 540f);
            interfaceRT.sizeDelta = new Vector2(1920f, 1080f);
            interfaceRT.pivot = new Vector2(0.5f, 0.5f);
            interfaceRT.anchorMin = Vector2.zero;
            interfaceRT.anchorMax = Vector2.zero;

            var canvas = inter.AddComponent<Canvas>();
            canvas.additionalShaderChannels = AdditionalCanvasShaderChannels.None;
            canvas.additionalShaderChannels = AdditionalCanvasShaderChannels.TexCoord1;
            canvas.additionalShaderChannels = AdditionalCanvasShaderChannels.Tangent;
            canvas.additionalShaderChannels = AdditionalCanvasShaderChannels.Normal;
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.scaleFactor = CoreHelper.ScreenScale;
            canvas.sortingOrder = 13000;

            var canvasScaler = inter.AddComponent<CanvasScaler>();
            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasScaler.referenceResolution = new Vector2(Screen.width, Screen.height);

            inter.AddComponent<GraphicRaycaster>();

            var info = new GameObject("Info");
            info.transform.SetParent(interfaceRT);
            info.transform.localScale = Vector3.one;

            var infoRT = info.AddComponent<RectTransform>();

            Info = info.AddComponent<TextMeshProUGUI>();
            Info.font = FontManager.inst.allFontAssets["Inconsolata Variable"];
            Info.fontSize = 32;

            FPS = info.AddComponent<FPSCounter>();

            UIManager.SetRectTransform(Info.rectTransform, new Vector2(-960f, 540f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 1f), new Vector2(800f, 200f));

            init = true;
        }

        public static void Update()
        {
            if (!init)
                return;

            Info.gameObject.SetActive(CoreConfig.Instance.DebugInfo.Value && GameManager.inst);

            if (CoreConfig.Instance.DebugInfo.Value && GameManager.inst && GameManager.inst.gameState == GameManager.State.Playing)
                Info.text = $"<b>FPS:</b> {FPS.Text}<br>" +
                            $"<b>Beatmap Objects Alive:</b> {BeatmapObjectAliveCount()} / {GameData.Current.beatmapObjects.Count}<br>" +
                            $"<b>Main Camera Position: {Camera.main.transform.position}<br>" +
                            $"<b>Main Camera Zoom: {Camera.main.orthographicSize}<br>" +
                            $"<b>Main Camera Rotation: {Camera.main.transform.rotation.eulerAngles}<br>" +
                            $"<b>BG Camera Position: {EventManager.inst.camPer.transform.position}<br>" +
                            $"<b>BG Camera Rotation: {EventManager.inst.camPer.transform.rotation.eulerAngles}<br>";
        }
    }
}
