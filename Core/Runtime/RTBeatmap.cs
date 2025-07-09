using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using SimpleJSON;

using BetterLegacy.Arcade.Managers;
using BetterLegacy.Configs;
using BetterLegacy.Core.Animation;
using BetterLegacy.Core.Animation.Keyframe;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Data.Level;
using BetterLegacy.Core.Data.Player;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using BetterLegacy.Core.Managers.Networking;
using BetterLegacy.Menus;
using BetterLegacy.Menus.UI.Interfaces;

namespace BetterLegacy.Core.Runtime
{
    /// <summary>
    /// Caches the current beatmap game states. (E.G. usually stuff that should only be assigned on level start, such as game speed, challenge mode, etc.)
    /// <br></br>Can also include other stuff that doesn't fit in <see cref="RTLevel"/>, such as hit data and general runtime stuff.
    /// </summary>
    public class RTBeatmap : Exists
    {
        /// <summary>
        /// The current runtime beatmap.
        /// </summary>
        public static RTBeatmap Current { get; set; } = new RTBeatmap();

        /// <summary>
        /// Cached challenge mode.
        /// </summary>
        public ChallengeMode challengeMode = ChallengeMode.Normal;

        /// <summary>
        /// Cached game speed.
        /// </summary>
        public GameSpeed gameSpeed = GameSpeed.X1_0;

        /// <summary>
        /// Resets the current beatmap cache.
        /// </summary>
        public void Reset(bool apply = true)
        {
            hits.Clear();
            deaths.Clear();
            boosts.Clear();

            if (!apply)
                return;

            challengeMode = CoreConfig.Instance.ChallengeModeSetting.Value;
            gameSpeed = CoreConfig.Instance.GameSpeedSetting.Value;

            lives = challengeMode.Lives;
        }

        /// <summary>
        /// Total time a user has been in a level.
        /// </summary>
        public RTTimer levelTimer;

        /// <summary>
        /// Amount of time the game has been paused.
        /// </summary>
        public RTTimer pausedTimer;

        /// <summary>
        /// Used for the no music achievement.
        /// </summary>
        public int CurrentMusicVolume { get; set; }

        /// <summary>
        /// Pauses the runtime.
        /// </summary>
        public void Pause()
        {
            if (!CoreHelper.InGame)
                return;

            AudioManager.inst.CurrentAudioSource.Pause();
            InputDataManager.inst.SetAllControllerRumble(0f);
            GameManager.inst.gameState = GameManager.State.Paused;

            pausedTimer.Reset();
        }

        /// <summary>
        /// Resumes the runtime.
        /// </summary>
        public void Resume()
        {
            if (!CoreHelper.InGame)
                return;

            AudioManager.inst.CurrentAudioSource.UnPause();
            GameManager.inst.gameState = GameManager.State.Playing;

            // remove time spent in pause menu from total timer
            levelTimer.offset -= pausedTimer.time;
        }

        #region End Level

        /// <summary>
        /// Function to run when the level ends in the Arcade.
        /// </summary>
        public EndLevelFunction endLevelFunc;

        /// <summary>
        /// End level function data.
        /// </summary>
        public string endLevelData;

        /// <summary>
        /// If level progress should be updated.
        /// </summary>
        public bool endLevelUpdateProgress = true;
        
        /// <summary>
        /// Runs the custom end level function.
        /// </summary>
        public void EndOfLevel()
        {
            ArcadeHelper.endedLevel = true;

            try
            {
                if (endLevelUpdateProgress)
                    LevelManager.UpdateCurrentLevelProgress();

                switch (endLevelFunc)
                {
                    case EndLevelFunction.EndLevelMenu: {
                            if (!EndLevelMenu.Current)
                                EndLevelMenu.Init();

                            break;
                        }
                    case EndLevelFunction.QuitToArcade: {
                            ArcadeHelper.QuitToArcade();

                            break;
                        }
                    case EndLevelFunction.ReturnToHub: {
                            LevelManager.Play(LevelManager.Hub);

                            break;
                        }
                    case EndLevelFunction.ReturnToPrevious: {
                            LevelManager.Play(LevelManager.PreviousLevel);

                            break;
                        }
                    case EndLevelFunction.ContinueCollection: {
                            var metadata = LevelManager.CurrentLevel.metadata;
                            var nextLevel = LevelManager.NextLevelInCollection;
                            if (LevelManager.CurrentLevelCollection && (metadata.song.DifficultyType == DifficultyType.Animation || nextLevel && nextLevel.saveData && nextLevel.saveData.Unlocked || !RTBeatmap.Current.challengeMode.Invincible || LevelManager.currentLevelIndex + 1 != LevelManager.CurrentLevelCollection.Count) || !LevelManager.IsNextEndOfQueue)
                            {
                                if (nextLevel)
                                    CoreHelper.Log($"Selecting next Arcade level in collection [{LevelManager.currentLevelIndex + 2} / {LevelManager.CurrentLevelCollection.Count}]");
                                else
                                    CoreHelper.Log($"Selecting next Arcade level in queue [{LevelManager.currentQueueIndex + 2} / {LevelManager.ArcadeQueue.Count}]");

                                ArcadeHelper.NextLevel();
                                break;
                            }

                            ArcadeHelper.QuitToArcade();

                            break;
                        }
                    case EndLevelFunction.LoadLevel: {
                            if (string.IsNullOrEmpty(endLevelData))
                                break;

                            if (LevelManager.Levels.TryFind(x => x.id == endLevelData, out Level level))
                                LevelManager.Play(level);
                            else if (SteamWorkshopManager.inst.Levels.TryFind(x => x.id == endLevelData, out Level steamLevel))
                                LevelManager.Play(steamLevel);

                            break;
                        }
                    case EndLevelFunction.LoadLevelInCollection: {
                            if (string.IsNullOrEmpty(endLevelData) || !LevelManager.CurrentLevelCollection)
                                break;

                            if (LevelManager.CurrentLevelCollection.levels.TryFind(x => x.id == endLevelData, out Level level))
                                LevelManager.Play(level);
                            else if (LevelManager.CurrentLevelCollection.levelInformation.TryFind(x => x.id == endLevelData, out LevelInfo levelInfo))
                                LevelManager.CurrentLevelCollection.DownloadLevel(levelInfo, LevelManager.Play);

                            break;
                        }
                    case EndLevelFunction.ParseInterface: {
                            if (CoreHelper.IsEditing) // don't want interfaces to load in editor
                            {
                                EditorManager.inst.DisplayNotification($"Cannot load interface in the editor!", 1f, EditorManager.NotificationType.Warning);
                                return;
                            }

                            var path = RTFile.CombinePaths(RTFile.BasePath, endLevelData + FileFormat.LSI.Dot());

                            if (!RTFile.FileExists(path))
                            {
                                CoreHelper.LogError($"Interface with file name: \"{endLevelData}\" does not exist.");
                                return;
                            }

                            InterfaceManager.inst.ParseInterface(path);

                            InterfaceManager.inst.MainDirectory = RTFile.BasePath;

                            Pause();
                            ArcadeHelper.endedLevel = false;

                            break;
                        }
                    case EndLevelFunction.Loop: {
                            GameManager.inst.gameState = GameManager.State.Playing;
                            AudioManager.inst.SetMusicTime(0f);

                            Time.timeScale = 1f;
                            InputDataManager.inst.SetAllControllerRumble(0f);

                            LevelManager.LevelEnded = false;
                            break;
                        }
                    case EndLevelFunction.Restart: {
                            GameManager.inst.gameState = GameManager.State.Playing;
                            ArcadeHelper.RestartLevel();
                            AudioManager.inst.CurrentAudioSource.Play();
                            break;
                        }
                }
            }
            catch (Exception ex)
            {
                CoreHelper.LogError($"End Level Func: {endLevelFunc}\nEnd Level String: {endLevelData}\nException: {ex}");
                // boot to main menu if level ending issues occur.
                SceneHelper.LoadScene(SceneName.Main_Menu);
            }

            ResetEndLevelVariables();
        }

        /// <summary>
        /// Resets the end level function.
        /// </summary>
        public void ResetEndLevelVariables()
        {
            endLevelFunc = 0;
            endLevelData = null;
            endLevelUpdateProgress = true;
        }

        #endregion

        #region Checkpoints

        /// <summary>
        /// The currently activated checkpoint.
        /// </summary>
        public Checkpoint ActiveCheckpoint { get; set; }

        int nextCheckpointIndex;

        /// <summary>
        /// Updates the checkpoint conditions.
        /// </summary>
        public void UpdateCheckpoints()
        {
            var checkpoints = GameData.Current?.data?.checkpoints;
            if (checkpoints != null && checkpoints.InRange(nextCheckpointIndex) && AudioManager.inst.CurrentAudioSource.time > (double)checkpoints[nextCheckpointIndex].time && CoreHelper.InEditorPreview)
                SetCheckpoint(nextCheckpointIndex);
        }

        /// <summary>
        /// Creates a new checkpoint and sets it as the currently active checkpoint. Used for modifiers.
        /// </summary>
        /// <param name="time">Time of the checkpoint to rewind to when reversing to it.</param>
        /// <param name="position">Position to spawn the players at.</param>
        public void SetCheckpoint(float time, Vector2 position) => SetCheckpoint(new Checkpoint("Modifier Checkpoint", Mathf.Clamp(time, 0f, AudioManager.inst.CurrentAudioSource.clip.length), position));

        /// <summary>
        /// Sets the currently active checkpoint based on an index.
        /// </summary>
        /// <param name="index">Index of the checkpoint.</param>
        public void SetCheckpoint(int index)
        {
            var checkpoints = GameData.Current?.data?.checkpoints;
            if (checkpoints == null || !checkpoints.InRange(index))
                return;

            CoreHelper.Log($"Set checkpoint: {index}");
            SetCheckpoint(checkpoints[index], index + 1);
        }

        /// <summary>
        /// Sets the currently active checkpoint based on an index.
        /// </summary>
        /// <param name="checkpoint">Checkpoint to set active.</param>
        /// <param name="nextIndex">Index of the next checkpoint to activate. If left at -1, it will not update the next index.</param>
        public void SetCheckpoint(Checkpoint checkpoint, int nextIndex = -1)
        {
            ActiveCheckpoint = checkpoint;
            if (nextIndex >= 0)
                nextCheckpointIndex = nextIndex;

            if (checkpoint.heal)
                PlayerManager.Players.ForLoop(customPlayer => customPlayer.ResetHealth());

            if (checkpoint.respawn)
                PlayerManager.SpawnPlayers(ActiveCheckpoint);

            CoroutineHelper.StartCoroutine(RTGameManager.inst.IPlayCheckpointAnimation());
        }

        /// <summary>
        /// Resets the active checkpoint.
        /// </summary>
        /// <param name="baseOnTime">If true, reset to last checkpoint. Otherwise, reset to first.</param>
        public void ResetCheckpoint(bool baseOnTime = false)
        {
            var checkpoints = GameData.Current?.data?.checkpoints;
            if (checkpoints == null || (CoreHelper.InEditor && !EditorManager.inst.hasLoadedLevel))
                return;

            CoreHelper.Log($"Reset Checkpoints | Based on time: {baseOnTime}");
            int index = 0;
            if (baseOnTime)
                index = GameData.Current.data.GetLastCheckpointIndex();

            ActiveCheckpoint = checkpoints[index];
            nextCheckpointIndex = index + 1;
        }

        /// <summary>
        /// Reverses to the active checkpoint.
        /// </summary>
        public void ReverseToCheckpoint() => CoroutineHelper.StartCoroutine(IReverseToCheckpoint());

        IEnumerator IReverseToCheckpoint()
        {
            if (GameManager.inst.isReversing)
                yield break;

            GameManager.inst.isReversing = true;

            var checkpoint = ActiveCheckpoint ?? GameData.Current.data.GetLastCheckpoint();

            if (GameData.Current.data.level.reverse && checkpoint.reverse)
            {
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

                animation.onComplete = () => AnimationManager.inst.Remove(animation.id);

                AnimationManager.inst.Play(animation);
                SoundManager.inst.PlaySound(DefaultSounds.rewind);
            }

            yield return CoroutineHelper.Seconds(2f);

            float time = Mathf.Clamp(checkpoint.time + 0.01f, 0.1f, AudioManager.inst.CurrentAudioSource.clip.length);
            if (!CoreHelper.InEditor && challengeMode.Lives > 0)
            {
                time = 0.1f;
                lives = challengeMode.Lives;
            }

            if (checkpoint.setTime)
                AudioManager.inst.SetMusicTime(time);

            GameManager.inst.gameState = GameManager.State.Playing;

            AudioManager.inst.CurrentAudioSource.Play();
            AudioManager.inst.SetPitch(1f); // resets the pitch offset

            GameManager.inst.UpdateEventSequenceTime();
            GameManager.inst.isReversing = false;

            yield return CoroutineHelper.Seconds(0.1f);

            PlayerManager.SpawnPlayers(checkpoint);

            checkpoint = null;

            yield break;
        }

        #endregion

        #region Game State Checks

        /// <summary>
        /// Players take damage but lose health and don't die.
        /// </summary>
        public bool IsPractice => challengeMode == ChallengeMode.Practice;

        /// <summary>
        /// Players take damage and can die if health hits zero.
        /// </summary>
        public bool IsNormal => challengeMode == ChallengeMode.Normal;

        /// <summary>
        /// Players take damage and only have 1 life. When they die, restart the level.
        /// </summary>
        public bool Is1Life => challengeMode == ChallengeMode.OneLife;

        /// <summary>
        /// Players take damage and only have 1 health. When they die, restart the level.
        /// </summary>
        public bool IsNoHit => challengeMode == ChallengeMode.OneHit;

        /// <summary>
        /// If the player is invincible.
        /// </summary>
        public bool Invincible => CoreHelper.IsEditing || !challengeMode.Damageable;

        /// <summary>
        /// The current pitch setting.
        /// </summary>
        public float Pitch => CoreHelper.InEditor || CoreHelper.InStory ? 1f : gameSpeed.Pitch;

        #endregion

        #region Player Conditions

        /// <summary>
        /// Amount of lives left until the level restarts.
        /// </summary>
        public int lives = -1;

        /// <summary>
        /// If the player is out of lives.
        /// </summary>
        public bool OutOfLives => lives <= 0;

        /// <summary>
        /// Data points representing the times the players got hit.
        /// </summary>
        public List<PlayerDataPoint> hits = new List<PlayerDataPoint>();

        /// <summary>
        /// Data points representing the times the players died.
        /// </summary>
        public List<PlayerDataPoint> deaths = new List<PlayerDataPoint>();

        /// <summary>
        /// Data points representing the times the players boosted.
        /// </summary>
        public List<PlayerDataPoint> boosts = new List<PlayerDataPoint>();

        /// <summary>
        /// If a player has been hit on a tick.
        /// </summary>
        public bool playerHit = false;

        /// <summary>
        /// If a player has died on a tick.
        /// </summary>
        public bool playerDied = false;

        #endregion
    }
}
