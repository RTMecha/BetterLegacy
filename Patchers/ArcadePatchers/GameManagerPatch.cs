using BetterLegacy.Components;
using BetterLegacy.Configs;
using BetterLegacy.Core;
using BetterLegacy.Core.Animation;
using BetterLegacy.Core.Animation.Keyframe;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Data.Player;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using BetterLegacy.Editor.Managers;
using BetterLegacy.Example;
using BetterLegacy.Menus;
using BetterLegacy.Menus.UI.Interfaces;
using HarmonyLib;
using LSFunctions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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
            CoreHelper.Log($"Current scene type: {CoreHelper.CurrentSceneType}\nCurrent scene name: {__instance.gameObject.scene.name}");
            CoreHelper.LogInit(__instance.className);

            if (!GameObject.Find("Game Systems/EffectsManager").GetComponent<RTEffectsManager>())
                GameObject.Find("Game Systems/EffectsManager").AddComponent<RTEffectsManager>();

            if (!GameObject.Find("Game Systems/EventManager").GetComponent<RTEventManager>())
                GameObject.Find("Game Systems/EventManager").AddComponent<RTEventManager>();

            var camBase = new GameObject("Camera Base");

            EventManager.inst.camParentTop.SetParent(camBase.transform);

            RTEventManager.inst.delayTracker = camBase.AddComponent<EventDelayTracker>();

            ExampleManager.onGameAwake?.Invoke(__instance);

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
            if (!CoreHelper.InEditor)
                Instance.LoadLevelCurrent();
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

            Instance.gameObject.AddComponent<GameStorageManager>();

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

            //if (!CoreHelper.IsUsingInputField && CoreHelper.Paused && !LevelManager.LevelEnded && InputDataManager.inst.menuActions.Cancel.WasPressed)
            //{
            //    PauseMenu.UnPause();
            //}

            if (CoreHelper.Playing)
            {
                for (int i = 0; i < GameData.Current.levelModifiers.Count; i++)
                {
                    GameData.Current.levelModifiers[i].Activate();
                }

                if (!CoreHelper.IsUsingInputField && !CoreHelper.InEditor)
                {
                    foreach (var player in PlayerManager.Players)
                    {
                        if (player.Player && player.Player.Actions.Pause.WasPressed)
                            PauseMenu.Pause();
                    }
                }

                if (__instance.checkpointsActivated != null && __instance.checkpointsActivated.Length != 0 &&
                    AudioManager.inst.CurrentAudioSource.time >= (double)__instance.UpcomingCheckpoint.time && !__instance.playingCheckpointAnimation &&
                    __instance.UpcomingCheckpointIndex != -1 && !__instance.checkpointsActivated[__instance.UpcomingCheckpointIndex] &&
                    CoreHelper.InEditorPreview)
                {
                    __instance.playingCheckpointAnimation = true;
                    __instance.SpawnPlayers(__instance.UpcomingCheckpoint.pos);
                    __instance.StartCoroutine(__instance.PlayCheckpointAnimation(__instance.UpcomingCheckpointIndex));
                }
            }

            if (CoreHelper.Reversing && !__instance.isReversing)
            {
                __instance.StartCoroutine(ReverseToCheckpointLoop(__instance));
            }
            else if (CoreHelper.Playing)
            {
                if (AudioManager.inst.CurrentAudioSource.clip && !EditorManager.inst
                    && AudioManager.inst.CurrentAudioSource.time >= __instance.songLength - 0.1f)
                    if (!LevelManager.LevelEnded)
                        __instance.GoToNextLevel();
            }
            else if (CoreHelper.Finished)
            {
                if (AudioManager.inst.CurrentAudioSource.clip && !EditorManager.inst
                    && AudioManager.inst.CurrentAudioSource.time >= __instance.songLength - 0.1f
                    && CoreConfig.Instance.ReplayLevel.Value && LevelManager.LevelEnded)
                    AudioManager.inst.SetMusicTime(0f);
            }

            if (CoreHelper.Playing || CoreHelper.Reversing)
                __instance.UpdateEventSequenceTime();

            if (AudioManager.inst.CurrentAudioSource.clip)
                __instance.prevAudioTime = AudioManager.inst.CurrentAudioSource.time;

            return false;
        }

        public static IEnumerator ReverseToCheckpointLoop(GameManager __instance)
        {
            if (__instance.isReversing)
                yield break;

            __instance.playingCheckpointAnimation = true;
            __instance.isReversing = true;

            int index = DataManager.inst.gameData.beatmapData.checkpoints.FindLastIndex(x => x.time < AudioManager.inst.CurrentAudioSource.time);
            if (index < 0)
                index = 0;

            var checkpoint = DataManager.inst.gameData.beatmapData.checkpoints[index];

            var animation = new RTAnimation("Reverse");
            animation.animationHandlers = new List<AnimationHandlerBase>
            {
                new AnimationHandler<float>(new List<IKeyframe<float>>
                {
                    new FloatKeyframe(0f, AudioManager.inst.CurrentAudioSource.pitch, Ease.Linear),
                    new FloatKeyframe(1f, -1.5f, Ease.CircIn)
                }, x =>
                {
                    if (AudioManager.inst.CurrentAudioSource.time > 1f)
                        AudioManager.inst.SetPitch(x);
                    else
                        AudioManager.inst.SetMusicTime(1f);
                }),
            };

            animation.onComplete = () =>
            {
                AnimationManager.inst.RemoveID(animation.id);
            };

            AnimationManager.inst.Play(animation);

            AudioManager.inst.PlaySound("rewind");

            yield return new WaitForSeconds(2f);

            float time = Mathf.Clamp(checkpoint.time + 0.01f, 0.1f, AudioManager.inst.CurrentAudioSource.clip.length);
            if (EditorManager.inst == null && (DataManager.inst.GetSettingInt("ArcadeDifficulty", 0) == 2 || DataManager.inst.GetSettingInt("ArcadeDifficulty", 0) == 3))
                time = 0.1f;

            AudioManager.inst.SetMusicTime(time);
            __instance.gameState = GameManager.State.Playing;

            AudioManager.inst.CurrentAudioSource.Play();
            AudioManager.inst.SetPitch(__instance.getPitch());

            __instance.UpdateEventSequenceTime();
            __instance.isReversing = false;

            yield return new WaitForSeconds(0.1f);

            __instance.SpawnPlayers(checkpoint.pos);
            __instance.playingCheckpointAnimation = false;
            checkpoint = null;

            yield break;
        }

        [HarmonyPatch(nameof(GameManager.FixedUpdate))]
        [HarmonyPrefix]
        static bool FixedUpdatePrefix()
        {
            if (DataManager.inst && DataManager.inst.gameData != null && DataManager.inst.gameData.beatmapData != null && DataManager.inst.gameData.beatmapData.checkpoints != null &&
                DataManager.inst.gameData.beatmapData.checkpoints.Count > 0 && (CoreHelper.Playing || CoreHelper.Reversing))
            {
                if (!CoreHelper.Reversing)
                {
                    Instance.UpcomingCheckpoint = Instance.GetClosestIndex(DataManager.inst.gameData.beatmapData.checkpoints, AudioManager.inst.CurrentAudioSource.time);
                    Instance.UpcomingCheckpointIndex = DataManager.inst.gameData.beatmapData.checkpoints.FindIndex(x => x == Instance.UpcomingCheckpoint);
                }
                if (Instance.timeline && AudioManager.inst.CurrentAudioSource.clip != null)
                {
                    float num = AudioManager.inst.CurrentAudioSource.time * 400f / AudioManager.inst.CurrentAudioSource.clip.length;
                    if (Instance.timeline.transform.Find("Base/position"))
                        Instance.timeline.transform.Find("Base/position").AsRT().anchoredPosition = new Vector2(num, 0f);
                    else
                        Instance.UpdateTimeline();
                }

                if (!CoreHelper.Reversing)
                    Instance.lastCheckpointState = DataManager.inst.gameData.beatmapData.GetWhichCheckpointBasedOnTime(AudioManager.inst.CurrentAudioSource.time);
            }
            Instance.playerGUI.SetActive(CoreHelper.InEditorPreview);
            return false;
        }

        [HarmonyPatch(nameof(GameManager.LoadLevelCurrent))]
        [HarmonyPrefix]
        static bool LoadLevelCurrentPrefix()
        {
            if (!LevelManager.LoadingFromHere && LevelManager.CurrentLevel)
            {
                LevelManager.LevelEnded = false;
                CoreHelper.StartCoroutine(LevelManager.Play(LevelManager.CurrentLevel));
            }
            return false;
        }

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

            if (GameStorageManager.inst && EventManager.inst)
            {
                EventManager.inst.camPer.backgroundColor = bgColorToLerp;
                if (GameStorageManager.inst.checkpointImages.Count > 0)
                    foreach (var image in GameStorageManager.inst.checkpointImages)
                        image.color = timelineColorToLerp;

                GameStorageManager.inst.timelinePlayer.color = timelineColorToLerp;
                GameStorageManager.inst.timelineLeftCap.color = timelineColorToLerp;
                GameStorageManager.inst.timelineRightCap.color = timelineColorToLerp;
                GameStorageManager.inst.timelineLine.color = timelineColorToLerp;
            }

            if (!CoreHelper.InEditor && AudioManager.inst.CurrentAudioSource.time < 15f)
            {
                if (__instance.introTitle.color != timelineColorToLerp)
                    __instance.introTitle.color = timelineColorToLerp;
                if (__instance.introArtist.color != timelineColorToLerp)
                    __instance.introArtist.color = timelineColorToLerp;
            }
            if (__instance.guiImages.Length > 0)
                foreach (var image in __instance.guiImages)
                    image.color = timelineColorToLerp;

            if (!__instance.menuUI.activeInHierarchy)
                return false;
            var componentsInChildren2 = __instance.menuUI.GetComponentsInChildren<TextMeshProUGUI>();
            for (int i = 0; i < componentsInChildren2.Length; i++)
                componentsInChildren2[i].color = CoreHelper.InvertColorHue(CoreHelper.InvertColorValue(bgColorToLerp));

            return false;
        }

        [HarmonyPatch(nameof(GameManager.GoToNextLevelLoop))]
        [HarmonyPrefix]
        static bool GoToNextLevelLoopPrefix(GameManager __instance, ref IEnumerator __result)
        {
            __result = GoToNextLevelLoop(__instance);
            return false;
        }

        static IEnumerator GoToNextLevelLoop(GameManager __instance)
        {
            // TODO: Implement custom achievements.
            //if (AudioManager.inst.masterVol <= 0f)
            //    SteamWrapper.inst.achievements.SetAchievement("NO_AUDIO");

            __instance.gameState = GameManager.State.Finish;
            Time.timeScale = 1f;
            DG.Tweening.DOTween.Clear();
            InputDataManager.inst.SetAllControllerRumble(0f);

            LevelManager.LevelEnded = true;
            LevelManager.OnLevelEnd?.Invoke();
            yield break;
        }

        [HarmonyPatch(nameof(GameManager.SpawnPlayers))]
        [HarmonyPrefix]
        static bool SpawnPlayersPrefix(Vector3 __0)
        {
            foreach (var customPlayer in InputDataManager.inst.players.Select(x => x as CustomPlayer))
            {
                if (customPlayer.Player == null)
                {
                    PlayerManager.SpawnPlayer(customPlayer, __0);
                    continue;
                }

                CoreHelper.Log($"Player {customPlayer.index} already exists!");
            }
            return false;
        }

        [HarmonyPatch(nameof(GameManager.Pause))]
        [HarmonyPrefix]
        static bool PausePrefix()
        {
            if (!CoreHelper.Playing)
                return false;

            LSHelpers.ShowCursor();
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
            LSHelpers.HideCursor();
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
            RTEditor.inst?.UpdateTimeline();

            if (!Instance.timeline || !AudioManager.inst.CurrentAudioSource.clip || DataManager.inst.gameData.beatmapData == null)
                return false;

            if (GameStorageManager.inst)
                GameStorageManager.inst.checkpointImages.Clear();
            var parent = Instance.timeline.transform.Find("elements");
            LSHelpers.DeleteChildren(parent, true);
            foreach (var checkpoint in DataManager.inst.gameData.beatmapData.checkpoints)
            {
                if (checkpoint.time <= 0.5f)
                    continue;

                var gameObject = Instance.checkpointPrefab.Duplicate(parent, $"Checkpoint [{checkpoint.name}] - [{checkpoint.time}]");
                float num = checkpoint.time * 400f / AudioManager.inst.CurrentAudioSource.clip.length;
                gameObject.transform.AsRT().anchoredPosition = new Vector2(num, 0f);
                if (GameStorageManager.inst)
                    GameStorageManager.inst.checkpointImages.Add(gameObject.GetComponent<Image>());
            }

            return false;
        }
    }
}
