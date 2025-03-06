using BetterLegacy.Arcade.Managers;
using BetterLegacy.Configs;
using BetterLegacy.Core;
using BetterLegacy.Core.Animation;
using BetterLegacy.Core.Animation.Keyframe;
using BetterLegacy.Core.Components;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Data.Player;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using BetterLegacy.Editor.Managers;
using BetterLegacy.Companion;
using BetterLegacy.Menus;
using BetterLegacy.Menus.UI.Interfaces;
using HarmonyLib;
using LSFunctions;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using BetterLegacy.Companion.Entity;

namespace BetterLegacy.Patchers
{
    [HarmonyPatch(typeof(GameManager))]
    public class GameManagerPatch : MonoBehaviour
    {
        public static GameManager Instance { get => GameManager.inst; set => GameManager.inst = value; }

        #region Variables

        public static Color bgColorToLerp;
        public static Color timelineColorToLerp;

        #endregion

        [HarmonyPatch(nameof(GameManager.Awake))]
        [HarmonyPrefix]
        static void AwakePrefix(GameManager __instance)
        {
            CoreHelper.Log($"Current scene type: {SceneHelper.CurrentSceneType}\nCurrent scene name: {SceneHelper.CurrentScene}");
            CoreHelper.LogInit(__instance.className);

            if (!GameObject.Find("Game Systems/EffectsManager").GetComponent<RTEffectsManager>())
                GameObject.Find("Game Systems/EffectsManager").AddComponent<RTEffectsManager>();

            if (!GameObject.Find("Game Systems/EventManager").GetComponent<RTEventManager>())
                GameObject.Find("Game Systems/EventManager").AddComponent<RTEventManager>();

            var camBase = new GameObject("Camera Base");

            EventManager.inst.camParentTop.SetParent(camBase.transform);

            RTEventManager.inst.delayTracker = camBase.AddComponent<EventDelayTracker>();

            Example.Current?.brain?.Notice(ExampleBrain.Notices.GAME_START);

            Camera.main.transparencySortMode = TransparencySortMode.CustomAxis;
        }

        [HarmonyPatch(nameof(GameManager.Start))]
        [HarmonyPrefix]
        static bool StartPrefix()
        {
            AudioManager.inst.SetMusicTime(0f);
            Instance.gameState = GameManager.State.Loading;
            InputDataManager.inst.PlayerPrefabs = Instance.PlayerPrefabs;
            InputDataManager.inst.playersCanJoin = false;
            Instance.playerGUI.SetActive(true);
            Instance.menuUI.GetComponentInChildren<Image>().enabled = false;
            Instance.initialPlayerCount = InputDataManager.inst.players.Count;
            InputDataManager.playerDisconnectedEvent += Instance.PlayerDisconnected;
            InputDataManager.playerReconnectedEvent += Instance.PlayerReconnected;

            CoreHelper.SetCameraRenderDistance();
            CoreHelper.SetAntiAliasing();
            var beatmapTheme = GameManager.inst.LiveTheme;
            GameManager.inst.LiveTheme = new BeatmapTheme
            {
                id = beatmapTheme.id,
                name = beatmapTheme.name,
                expanded = beatmapTheme.expanded,
                backgroundColor = beatmapTheme.backgroundColor,
                guiAccentColor = beatmapTheme.guiColor,
                guiColor = beatmapTheme.guiColor,
                playerColors = beatmapTheme.playerColors,
                objectColors = CoreHelper.NewColorList(18),
                backgroundColors = beatmapTheme.backgroundColors,
                effectColors = CoreHelper.NewColorList(18),
            };

            PlayerManager.SetupImages(Instance);

            Instance.gameObject.AddComponent<RTGameManager>();

            ArcadeHelper.fromLevel = true;

            LevelManager.timeInLevelOffset = Time.time;
            LevelManager.timeInLevel = 0f;
            return false;
        }

        [HarmonyPatch(nameof(GameManager.Update))]
        [HarmonyPrefix]
        static bool UpdatePrefix(GameManager __instance)
        {
            if (!LevelManager.LevelEnded)
                LevelManager.timeInLevel = Time.time - LevelManager.timeInLevelOffset;

            if (!CoreHelper.IsUsingInputField && InputDataManager.inst.menuActions.Cancel.WasPressed && CoreHelper.Paused && !LevelManager.LevelEnded && PauseMenu.Current && !PauseMenu.Current.generating)
                PauseMenu.UnPause();

            if (CoreHelper.Playing)
            {
                for (int i = 0; i < GameData.Current.levelModifiers.Count; i++)
                    GameData.Current.levelModifiers[i].Activate();

                if (!CoreHelper.IsUsingInputField && !CoreHelper.InEditor)
                {
                    bool shouldPause = false;
                    foreach (var player in PlayerManager.Players)
                        if (player.Player && player.Player.Actions.Pause.WasPressed)
                            shouldPause = true;

                    if (shouldPause)
                        PauseMenu.Pause();
                }

                RTGameManager.inst.UpdateCheckpoints();
            }

            if (CoreHelper.Reversing && !__instance.isReversing)
                RTGameManager.inst.ReverseToCheckpoint();
            else if (CoreHelper.Playing)
                CheckLevelEnd();
            else if (CoreHelper.Finished)
                CheckReplay();

            if (CoreHelper.Playing || CoreHelper.Reversing)
                __instance.UpdateEventSequenceTime();

            __instance.prevAudioTime = AudioManager.inst.CurrentAudioSource.time;

            return false;
        }

        static void CheckLevelEnd()
        {
            if (AudioManager.inst.CurrentAudioSource.clip && !CoreHelper.InEditor && ArcadeHelper.SongEnded && !LevelManager.LevelEnded)
                LevelManager.EndLevel();
        }

        static void CheckReplay()
        {
            if (AudioManager.inst.CurrentAudioSource.clip && !CoreHelper.InEditor && ArcadeHelper.SongEnded && ArcadeHelper.ReplayLevel && LevelManager.LevelEnded)
                AudioManager.inst.SetMusicTime(0f);
        }

        [HarmonyPatch(nameof(GameManager.FixedUpdate))]
        [HarmonyPrefix]
        static bool FixedUpdatePrefix()
        {
            if (DataManager.inst && GameData.IsValid && GameData.Current.beatmapData != null && GameData.Current.beatmapData.checkpoints != null &&
                GameData.Current.beatmapData.checkpoints.Count > 0 && (CoreHelper.Playing || CoreHelper.Reversing))
            {
                if (!CoreHelper.Reversing)
                {
                    Instance.UpcomingCheckpointIndex = GameData.Current.beatmapData.checkpoints.FindLastIndex(x => x.time < AudioManager.inst.CurrentAudioSource.time);
                    if (Instance.UpcomingCheckpointIndex > 0)
                        Instance.UpcomingCheckpoint = GameData.Current.beatmapData.checkpoints[Instance.UpcomingCheckpointIndex];
                }
                if (Instance.timeline && AudioManager.inst.CurrentAudioSource.clip != null)
                {
                    float num = AudioManager.inst.CurrentAudioSource.time * 400f / AudioManager.inst.CurrentAudioSource.clip.length;
                    if (Instance.timeline.transform.Find("Base/position"))
                        Instance.timeline.transform.Find("Base/position").AsRT().anchoredPosition = new Vector2(num, 0f);
                    else
                        Instance.UpdateTimeline();
                }

                //if (!CoreHelper.Reversing)
                //    Instance.lastCheckpointState = GameData.Current.beatmapData.GetWhichCheckpointBasedOnTime(AudioManager.inst.CurrentAudioSource.time);
            }
            Instance.playerGUI.SetActive(CoreHelper.InEditorPreview);
            return false;
        }

        [HarmonyPatch(nameof(GameManager.LoadLevelCurrent))]
        [HarmonyPrefix]
        static bool LoadLevelCurrentPrefix() => false;

        [HarmonyPatch(nameof(GameManager.getPitch))]
        [HarmonyPrefix]
        static bool getPitch(ref float __result)
        {
            __result = CoreHelper.Pitch;
            return false;
        }

        [HarmonyPatch(nameof(GameManager.UpdateTheme))]
        [HarmonyPrefix]
        static bool UpdateThemePrefix(GameManager __instance)
        {
            BackgroundManagerPatch.bgColorToLerp = bgColorToLerp;

            if (RTGameManager.inst && EventManager.inst)
            {
                EventManager.inst.camPer.backgroundColor = bgColorToLerp;
                if (RTGameManager.inst.checkpointImages.Count > 0)
                    foreach (var image in RTGameManager.inst.checkpointImages)
                        image.color = timelineColorToLerp;

                RTGameManager.inst.timelinePlayer.color = timelineColorToLerp;
                RTGameManager.inst.timelineLeftCap.color = timelineColorToLerp;
                RTGameManager.inst.timelineRightCap.color = timelineColorToLerp;
                RTGameManager.inst.timelineLine.color = timelineColorToLerp;
            }

            if (!CoreHelper.InEditor && AudioManager.inst.CurrentAudioSource.time < 15f)
            {
                bool introActive = GameData.IsValid && GameData.Current.beatmapData != null && GameData.Current.beatmapData.levelData is LevelData levelData && !levelData.showIntro;

                __instance.introTitle.gameObject.SetActive(introActive);
                __instance.introArtist.gameObject.SetActive(introActive);
                if (introActive && __instance.introTitle.color != timelineColorToLerp)
                    __instance.introTitle.color = timelineColorToLerp;
                if (introActive && __instance.introArtist.color != timelineColorToLerp)
                    __instance.introArtist.color = timelineColorToLerp;
            }

            if (__instance.guiImages.Length > 0)
                foreach (var image in __instance.guiImages)
                    image.color = timelineColorToLerp;

            if (!__instance.menuUI || !__instance.menuUI.activeInHierarchy)
                return false;

            var componentsInChildren2 = __instance.menuUI.GetComponentsInChildren<TextMeshProUGUI>();
            for (int i = 0; i < componentsInChildren2.Length; i++)
                componentsInChildren2[i].color = CoreHelper.InvertColorHue(CoreHelper.InvertColorValue(bgColorToLerp));

            return false;
        }

        [HarmonyPatch(nameof(GameManager.GoToNextLevelLoop))]
        [HarmonyPrefix]
        static bool GoToNextLevelLoopPrefix(ref IEnumerator __result)
        {
            __result = GoToNextLevelLoop();
            return false;
        }

        static IEnumerator GoToNextLevelLoop()
        {
            LevelManager.EndLevel();
            yield break;
        }

        [HarmonyPatch(nameof(GameManager.SpawnPlayers))]
        [HarmonyPrefix]
        static bool SpawnPlayersPrefix(Vector3 __0)
        {
            PlayerManager.SpawnPlayers(__0);
            return false;
        }

        [HarmonyPatch(nameof(GameManager.Pause))]
        [HarmonyPrefix]
        static bool PausePrefix()
        {
            if (!CoreHelper.Playing)
                return false;

            MenuManager.inst.ic.SwitchBranch("main");
            Instance.menuUI.GetComponentInChildren<Image>().enabled = true;
            AudioManager.inst.CurrentAudioSource.Pause();
            InputDataManager.inst.SetAllControllerRumble(0f);
            Instance.gameState = GameManager.State.Paused;
            ArcadeHelper.endedLevel = false;

            return false;
        }

        [HarmonyPatch(nameof(GameManager.UnPause))]
        [HarmonyPrefix]
        static bool UnPausePrefix()
        {
            if (!CoreHelper.Paused)
                return false;
            CursorManager.inst.HideCursor();
            MenuManager.inst.ic.SwitchBranch("empty");
            Instance.menuUI.GetComponentInChildren<Image>().enabled = false;
            AudioManager.inst.CurrentAudioSource.UnPause();
            Instance.gameState = GameManager.State.Playing;

            return false;
        }

        [HarmonyPatch(nameof(GameManager.UpdateTimeline))]
        [HarmonyPrefix]
        static bool UpdateTimelinePrefix()
        {
            if (RTEditor.inst)
                RTEditor.inst.UpdateTimeline();

            if (!Instance.timeline || !AudioManager.inst.CurrentAudioSource.clip || GameData.Current.beatmapData == null)
                return false;

            if (RTGameManager.inst)
                RTGameManager.inst.checkpointImages.Clear();
            var parent = Instance.timeline.transform.Find("elements");
            LSHelpers.DeleteChildren(parent);
            foreach (var checkpoint in GameData.Current.beatmapData.checkpoints)
            {
                if (checkpoint.time <= 0.5f)
                    continue;

                var gameObject = Instance.checkpointPrefab.Duplicate(parent, $"Checkpoint [{checkpoint.name}] - [{checkpoint.time}]");
                float num = checkpoint.time * 400f / AudioManager.inst.CurrentAudioSource.clip.length;
                gameObject.transform.AsRT().anchoredPosition = new Vector2(num, 0f);
                if (RTGameManager.inst)
                    RTGameManager.inst.checkpointImages.Add(gameObject.GetComponent<Image>());
            }

            return false;
        }

        [HarmonyPatch(nameof(GameManager.ResetCheckpoints))]
        [HarmonyPrefix]
        static bool ResetCheckpointsPrefix(bool __0)
        {
            RTGameManager.inst.ResetCheckpoint(__0);
            return false;
        }

        [HarmonyPatch(nameof(GameManager.PlayCheckpointAnimation))]
        [HarmonyPrefix]
        static bool PlayCheckpointAnimationPrefix(ref IEnumerator __result, int __0)
        {
            __result = RTGameManager.inst.IPlayCheckpointAnimation();
            return false;
        }
    }
}
