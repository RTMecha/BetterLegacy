using System.Collections;

using UnityEngine;
using UnityEngine.UI;

using HarmonyLib;

using BetterLegacy.Arcade.Managers;
using BetterLegacy.Companion.Entity;
using BetterLegacy.Core;
using BetterLegacy.Core.Components;
using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using BetterLegacy.Core.Runtime;
using BetterLegacy.Menus.UI.Interfaces;

namespace BetterLegacy.Patchers
{
    [HarmonyPatch(typeof(GameManager))]
    public class GameManagerPatch : MonoBehaviour
    {
        public static GameManager Instance { get => GameManager.inst; set => GameManager.inst = value; }

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

            SceneHelper.LoadedGame = true;
        }

        [HarmonyPatch(nameof(GameManager.Start))]
        [HarmonyPrefix]
        static bool StartPrefix()
        {
            Instance.gameState = GameManager.State.Loading;
            InputDataManager.inst.PlayerPrefabs = Instance.PlayerPrefabs;
            InputDataManager.inst.playersCanJoin = false;
            Instance.playerGUI.SetActive(true);
            Instance.menuUI.GetComponentInChildren<Image>().enabled = false;
            Instance.initialPlayerCount = PlayerManager.Players.Count;
            InputDataManager.playerDisconnectedEvent += Instance.PlayerDisconnected;
            InputDataManager.playerReconnectedEvent += Instance.PlayerReconnected;

            CoreHelper.SetCameraRenderDistance();
            CoreHelper.SetAntiAliasing();
            var beatmapTheme = GameManager.inst.LiveTheme;
            ThemeManager.inst.Current = new BeatmapTheme
            {
                id = beatmapTheme.id,
                name = beatmapTheme.name,
                backgroundColor = beatmapTheme.backgroundColor,
                guiAccentColor = beatmapTheme.guiColor,
                guiColor = beatmapTheme.guiColor,
                playerColors = beatmapTheme.playerColors,
                objectColors = RTColors.NewColorList(18),
                backgroundColors = beatmapTheme.backgroundColors,
                effectColors = RTColors.NewColorList(18),
            };

            PlayerManager.SetupImages(Instance);

            Instance.gameObject.AddComponent<RTGameManager>();

            ArcadeHelper.fromLevel = true;
            return false;
        }

        [HarmonyPatch(nameof(GameManager.Update))]
        [HarmonyPrefix]
        static bool UpdatePrefix(GameManager __instance)
        {
            if (!LevelManager.LevelEnded)
                RTBeatmap.Current.levelTimer.Update();

            if (CoreHelper.Paused)
                RTBeatmap.Current.pausedTimer.Update();

            if (!CoreHelper.IsUsingInputField && InputDataManager.inst.menuActions.Cancel.WasPressed && CoreHelper.Paused && !LevelManager.LevelEnded && PauseMenu.Current && !PauseMenu.Current.generating)
                PauseMenu.UnPause();

            if (CoreHelper.Playing)
            {
                if (!CoreHelper.IsUsingInputField && !CoreHelper.InEditor)
                {
                    bool shouldPause = false;
                    foreach (var player in PlayerManager.Players)
                        if (player.RuntimePlayer && player.RuntimePlayer.Actions.Pause.WasPressed)
                            shouldPause = true;

                    if (shouldPause)
                        PauseMenu.Pause();
                }

                RTBeatmap.Current.UpdateCheckpoints();
            }

            switch (__instance.gameState)
            {
                case GameManager.State.Reversing: {
                        if (!__instance.isReversing)
                            RTBeatmap.Current.ReverseToCheckpoint();
                        break;
                    }
                case GameManager.State.Playing: {
                        CheckLevelEnd();
                        break;
                    }
                case GameManager.State.Finish: {
                        CheckReplay();
                        break;
                    }
            }

            if (CoreHelper.Playing || CoreHelper.Reversing)
                __instance.UpdateEventSequenceTime();

            __instance.prevAudioTime = AudioManager.inst.CurrentAudioSource.time;

            return false;
        }

        static void CheckLevelEnd()
        {
            if (AudioManager.inst.CurrentAudioSource.clip && !CoreHelper.InEditor && ArcadeHelper.SongEnded && !LevelManager.LevelEnded)
            {
                CoreHelper.Log($"Level has ended!\n" +
                    $"Game State: {Instance.gameState}\n" +
                    $"Song Ended: {ArcadeHelper.SongEnded}\n" +
                    $"Song Pitch: {AudioManager.inst.CurrentAudioSource.pitch}\n" +
                    $"Song Time: {AudioManager.inst.CurrentAudioSource.time}\n" +
                    $"Song Length: {GameManager.inst.songLength}\n" +
                    $"End Offset: {GameData.Current?.data?.level?.LevelEndOffset ?? 0.1f}\n" +
                    $"End Point: {GameManager.inst.songLength - GameData.Current?.data?.level?.LevelEndOffset ?? 0.1f}");
                LevelManager.EndLevel();
            }
        }

        static void CheckReplay()
        {
            if (AudioManager.inst.CurrentAudioSource.clip && !CoreHelper.InEditor && ArcadeHelper.SongEnded && ArcadeHelper.ReplayLevel && LevelManager.LevelEnded)
                AudioManager.inst.SetMusicTime(GameData.Current.data.level.LevelStartOffset);
        }

        [HarmonyPatch(nameof(GameManager.FixedUpdate))]
        [HarmonyPrefix]
        static bool FixedUpdatePrefix()
        {
            if (DataManager.inst && GameData.Current && GameData.Current.data && GameData.Current.data.checkpoints != null &&
                !GameData.Current.data.checkpoints.IsEmpty() && (CoreHelper.Playing || CoreHelper.Reversing))
            {
                if (!CoreHelper.Reversing)
                    Instance.UpcomingCheckpointIndex = GameData.Current.data.checkpoints.FindLastIndex(x => x.time < AudioManager.inst.CurrentAudioSource.time);

                if (Instance.timeline && AudioManager.inst.CurrentAudioSource.clip)
                {
                    float num = RTBeatmap.Current.GetTimelineOffset(AudioManager.inst.CurrentAudioSource.time);
                    if (Instance.timeline.transform.Find("Base/position"))
                        Instance.timeline.transform.Find("Base/position").AsRT().anchoredPosition = new Vector2(num, 0f);
                    else
                        Instance.UpdateTimeline();
                }
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
            __result = RTBeatmap.Current.Pitch;
            return false;
        }

        [HarmonyPatch(nameof(GameManager.UpdateTheme))]
        [HarmonyPrefix]
        static bool UpdateThemePrefix()
        {
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

            Instance.menuUI.GetComponentInChildren<Image>().enabled = true;
            RTBeatmap.Current?.Pause();
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

            Instance.menuUI.GetComponentInChildren<Image>().enabled = false;
            RTBeatmap.Current?.Resume();

            return false;
        }

        [HarmonyPatch(nameof(GameManager.UpdateTimeline))]
        [HarmonyPrefix]
        static bool UpdateTimelinePrefix()
        {
            if (RTGameManager.inst)
                RTGameManager.inst.UpdateTimeline();
            return false;
        }

        [HarmonyPatch(nameof(GameManager.ResetCheckpoints))]
        [HarmonyPrefix]
        static bool ResetCheckpointsPrefix(bool __0)
        {
            RTBeatmap.Current.ResetCheckpoint(__0);
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
