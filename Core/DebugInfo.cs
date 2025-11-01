using System;

using UnityEngine;

using TMPro;

using BetterLegacy.Configs;
using BetterLegacy.Core.Components;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Helpers;

namespace BetterLegacy.Core.Managers
{
    public static class DebugInfo
    {
        public static void TogglePlay() => (AudioManager.inst.CurrentAudioSource.isPlaying ? (Action)AudioManager.inst.CurrentAudioSource.Pause : AudioManager.inst.CurrentAudioSource.Play).Invoke();

        public static int BeatmapObjectAliveCount() => !GameData.Current ? 0 : GameData.Current.beatmapObjects.FindAll(x => x.objectType != BeatmapObject.ObjectType.Empty && x.Alive).Count;

        public static void LogColor(Color color) => Debug.Log($"[<color=#{RTColors.ColorToHex(color)}>▓▓▓▓▓▓▓▓▓▓▓▓▓</color>]");
        public static void LogColor(string color) => Debug.Log($"[<color={color}>▓▓▓▓▓▓▓▓▓▓▓▓▓</color>]");

        public static void LogClassNames()
        {
            // Mod Class Names
            var str = $"\n{LegacyPlugin.className}";

            str += RTVideoManager.managerSettings.ClassName;

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

        public static TextMeshProUGUI Info { get; set; }

        public static DraggableUI InfoSelection { get; set; }

        public static bool init;

        public static UICanvas canvas;

        public static void Init()
        {
            if (init)
                return;

            canvas = UIManager.GenerateUICanvas("Debug Info Canvas", null, true, 13000);

            var info = new GameObject("Info");
            info.transform.SetParent(canvas.GameObject.transform);
            info.transform.localScale = Vector3.one;

            var infoRT = info.AddComponent<RectTransform>();

            Info = info.AddComponent<TextMeshProUGUI>();
            try
            {
                Info.font = FontManager.inst.allFontAssets["Inconsolata Variable"];
            }
            catch (Exception ex)
            {
                CoreHelper.LogException(ex);
            }
            Info.fontSize = 32;

            UIManager.SetRectTransform(Info.rectTransform, CoreConfig.Instance.DebugPosition.Value, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 1f), new Vector2(800f, 200f));

            InfoSelection = info.AddComponent<DraggableUI>();
            InfoSelection.mode = DraggableUI.DragMode.RequiredDrag;
            InfoSelection.draggingAction = vector2 => CoreConfig.Instance.DebugPosition.Value = vector2;
            InfoSelection.target = Info.rectTransform;

            init = true;
        }

        public static void Update()
        {
            if (!init)
                return;

            Info.gameObject.SetActive(CoreConfig.Instance.DebugInfo.Value && CoreHelper.InGame);

            if (!CoreConfig.Instance.DebugInfo.Value && !CoreHelper.InGame && !CoreHelper.Playing && !CoreHelper.Reversing)
                return;

            if (!InfoSelection.dragging)
                Info.transform.position = CoreConfig.Instance.DebugPosition.Value;

            if (CoreHelper.InGame && !CoreConfig.Instance.DebugShowOnlyFPS.Value)
            {
                Info.text = $"<b>FPS:</b> {LegacyPlugin.FPSCounter.Text}<br>" +
                            $"<b>Beatmap Objects Alive:</b> {BeatmapObjectAliveCount()} / {(!GameData.Current ? 0 : GameData.Current.beatmapObjects.Count)}<br>" +
                            $"<b>Main Camera Position: {Camera.main.transform.position}<br>" +
                            $"<b>Main Camera Zoom: {Camera.main.orthographicSize}<br>" +
                            $"<b>Main Camera Rotation: {Camera.main.transform.rotation.eulerAngles}<br>" +
                            $"<b>BG Camera Position: {EventManager.inst.camPer.transform.position}<br>" +
                            $"<b>BG Camera Rotation: {EventManager.inst.camPer.transform.rotation.eulerAngles}<br>";
            }
            else
                Info.text = $"<b>FPS:</b> {LegacyPlugin.FPSCounter.Text}";

            if (canvas != null)
                canvas.Canvas.scaleFactor = CoreHelper.ScreenScale;
        }
    }
}
