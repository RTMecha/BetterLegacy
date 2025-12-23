using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

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
        /// Ticks the runtime beatmap.
        /// </summary>
        public void Tick()
        {
            if (!GameManager.inst)
                return;

            if (!LevelManager.LevelEnded)
                levelTimer.Update();

            if (CoreHelper.Paused)
                pausedTimer.Update();

            if (!CoreHelper.IsUsingInputField && InputDataManager.inst.menuActions.Cancel.WasPressed && CoreHelper.Paused && !LevelManager.LevelEnded && PauseMenu.Current && !PauseMenu.Current.generating)
                PauseMenu.UnPause();

            if (CoreHelper.Playing)
            {
                if (!CoreHelper.IsUsingInputField && !CoreHelper.InEditor)
                {
                    bool shouldPause = false;
                    foreach (var player in PlayerManager.Players)
                        if (player.Input && player.Input.Pause.WasPressed)
                            shouldPause = true;

                    if (shouldPause)
                        PauseMenu.Pause();
                }

                UpdateCheckpoints();
            }

            switch (GameManager.inst.gameState)
            {
                case GameManager.State.Reversing: {
                        if (!GameManager.inst.isReversing)
                            ReverseToCheckpoint();
                        break;
                    }
                case GameManager.State.Playing: {
                        if (AudioManager.inst.CurrentAudioSource.clip && !CoreHelper.InEditor && ArcadeHelper.SongEnded && !LevelManager.LevelEnded)
                        {
                            CoreHelper.Log($"Level has ended!\n" +
                                $"Game State: {GameManager.inst.gameState}\n" +
                                $"Song Ended: {ArcadeHelper.SongEnded}\n" +
                                $"Song Pitch: {AudioManager.inst.CurrentAudioSource.pitch}\n" +
                                $"Song Time: {AudioManager.inst.CurrentAudioSource.time}\n" +
                                $"Song Length: {GameManager.inst.songLength}\n" +
                                $"End Offset: {GameData.Current?.data?.level?.LevelEndOffset ?? 0.1f}\n" +
                                $"End Point: {GameManager.inst.songLength - GameData.Current?.data?.level?.LevelEndOffset ?? 0.1f}");
                            LevelManager.EndLevel();
                        }
                        break;
                    }
                case GameManager.State.Finish: {
                        if (AudioManager.inst.CurrentAudioSource.clip && !CoreHelper.InEditor && ArcadeHelper.SongEnded && ArcadeHelper.ReplayLevel && LevelManager.LevelEnded)
                            AudioManager.inst.SetMusicTime(GameData.Current.data.level.LevelStartOffset);
                        break;
                    }
            }

            if (CoreHelper.Playing || CoreHelper.Reversing)
                GameManager.inst.UpdateEventSequenceTime();

            GameManager.inst.prevAudioTime = AudioManager.inst.CurrentAudioSource.time;
        }

        /// <summary>
        /// Resets the current beatmap cache.
        /// </summary>
        public void Reset(bool apply = true)
        {
            hits.Clear();
            deaths.Clear();
            boosts.Clear();

            if (GameData.Current && GameData.Current.data && GameData.Current.data.level)
                respawnImmediately = GameData.Current.data.level.respawnImmediately;

            if (!apply)
            {
                UpdateLives(challengeMode.Lives);
                return;
            }

            challengeMode = CoreConfig.Instance.ChallengeModeSetting.Value;
            gameSpeed = CoreConfig.Instance.GameSpeedSetting.Value;

            UpdateLives(challengeMode.Lives);
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

        /// <summary>
        /// Gets the position of an element on the game timeline.
        /// </summary>
        /// <param name="t">Time to calculate.</param>
        /// <returns>Returns the position of an element on the game timeline based on the time, the song length and the level start and end offsets.</returns>
        public float GetTimelineOffset(float t) => (t - GameData.Current.data.level.LevelStartOffset) * 400f / (AudioManager.inst.CurrentAudioSource.clip.length - (GameData.Current.data.level.LevelEndOffset + GameData.Current.data.level.LevelStartOffset));

        #region End Level

        /// <summary>
        /// If the end level functions should be reset when the level starts.
        /// </summary>
        public bool shouldResetEndFuncOnStart = true;

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
                if (LevelManager.CurrentLevel && LevelManager.CurrentLevel.saveData)
                    LevelManager.CurrentLevel.saveData.Percentage = 100f;

                if (endLevelUpdateProgress)
                    LevelManager.UpdateCurrentLevelProgress();
                else
                    LevelManager.SaveProgress();

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
                            if (LevelManager.CurrentLevelCollection && (metadata.song.Difficulty == DifficultyType.Animation || nextLevel && nextLevel.saveData && nextLevel.saveData.Unlocked || !RTBeatmap.Current.challengeMode.Invincible || LevelManager.currentLevelIndex + 1 != LevelManager.CurrentLevelCollection.Count) || !LevelManager.IsNextEndOfQueue)
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
                            else if (RTSteamManager.inst.Levels.TryFind(x => x.id == endLevelData, out Level steamLevel))
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
                            AudioManager.inst.SetMusicTime(GameData.Current.data.level.LevelStartOffset);

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
            endLevelFunc = GameData.Current?.data?.level?.endLevelFunc ?? EndLevelFunction.EndLevelMenu;
            endLevelData = GameData.Current?.data?.level?.endLevelData;
            endLevelUpdateProgress = GameData.Current?.data?.level?.endLevelUpdateProgress ?? true;
        }

        #endregion

        #region Checkpoints

        /// <summary>
        /// The currently activated checkpoint.
        /// </summary>
        public Checkpoint ActiveCheckpoint { get; set; }

        public int ActiveCheckpointIndex { get; set; }

        int nextCheckpointIndex;

        /// <summary>
        /// Updates the checkpoint conditions.
        /// </summary>
        public void UpdateCheckpoints()
        {
            var checkpoints = GameData.Current?.data?.checkpoints;
            if (checkpoints != null && checkpoints.InRange(nextCheckpointIndex) && AudioManager.inst.CurrentAudioSource.time > (double)checkpoints[nextCheckpointIndex].time && CoreHelper.InEditorPreview)
                SetCheckpoint(nextCheckpointIndex, false);
        }

        /// <summary>
        /// Sets the currently active checkpoint based on an index.
        /// </summary>
        /// <param name="index">Index of the checkpoint.</param>
        public void SetCheckpoint(int index, bool forceTrigger = true)
        {
            var checkpoints = GameData.Current?.data?.checkpoints;
            if (checkpoints == null || !checkpoints.InRange(index))
                return;

            CoreHelper.Log($"Set checkpoint: {index}");
            SetCheckpoint(checkpoints[index], index, index + 1, forceTrigger);
        }

        /// <summary>
        /// Sets the currently active checkpoint based on an index.
        /// </summary>
        /// <param name="checkpoint">Checkpoint to set active.</param>
        /// <param name="nextIndex">Index of the next checkpoint to activate. If left at -1, it will not update the next index.</param>
        public void SetCheckpoint(Checkpoint checkpoint, int index = -1, int nextIndex = -1, bool forceTrigger = true)
        {
            // set next checkpoint index if it is triggerable
            if (nextIndex >= 0)
            {
                var checkpoints = GameData.Current?.data?.checkpoints;
                while (checkpoints != null && checkpoints.TryGetAt(nextIndex, out Checkpoint nextCheckpoint) && !nextCheckpoint.autoTriggerable)
                    nextIndex++;

                nextCheckpointIndex = nextIndex;
            }

            if (!forceTrigger && !checkpoint.autoTriggerable)
                return;

            ActiveCheckpointIndex = index;
            ActiveCheckpoint = checkpoint;

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
            ActiveCheckpointIndex = index;
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

            CoreHelper.Log("Started reversing the level.");

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

                animation.onComplete = () => RTGameManager.inst.levelAnimationController.Remove(animation.id);
                RTGameManager.inst.levelAnimationController.Play(animation);
                SoundManager.inst.PlaySound(DefaultSounds.rewind);
                yield return CoroutineHelper.Seconds(2f);
            }
            else
                yield return CoroutineHelper.Seconds(1f);

            float time = Mathf.Clamp(checkpoint.time + 0.01f, 0.1f, AudioManager.inst.CurrentAudioSource.clip.length);
            if (!CoreHelper.InEditor && challengeMode.Lives > 0)
            {
                time = GameData.Current.data.level.LevelStartOffset;
                if (OutOfLives)
                    UpdateLives(challengeMode.Lives);
            }

            if (checkpoint.setTime)
                AudioManager.inst.SetMusicTime(time);
            AudioManager.inst.SetPitch(1f); // resets the pitch offset

            GameManager.inst.UpdateEventSequenceTime();

            yield return CoroutineHelper.Seconds(0.1f);

            if (!AudioManager.inst.CurrentAudioSource.isPlaying)
                AudioManager.inst.CurrentAudioSource.Play();

            GameManager.inst.gameState = GameManager.State.Playing;
            GameManager.inst.isReversing = false;

            PlayerManager.SpawnPlayers(checkpoint);

            checkpoint = null;

            yield break;
        }

        #endregion

        #region Game State Checks

        /// <summary>
        /// True if frame the level has started on is the current frame.
        /// </summary>
        public bool LevelStarted { get; set; }

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
        public bool Invincible => CoreHelper.IsEditing || (!challengeMode.Damageable && challengeMode.Invincible);

        /// <summary>
        /// The current pitch setting.
        /// </summary>
        public float Pitch => CoreHelper.InEditor || CoreHelper.InStory ? 1f : gameSpeed.Pitch;

        #endregion

        #region Player Conditions

        /// <summary>
        /// If players should respawn immediately when they die and not wait for other players to die.
        /// </summary>
        public bool respawnImmediately = false;

        /// <summary>
        /// Amount of lives left until the level restarts.
        /// </summary>
        public int lives = -1;

        /// <summary>
        /// If the player is out of lives.
        /// </summary>
        public bool OutOfLives => lives == 0 || PlayerManager.Players.All(x => x is PAPlayer player && player.OutOfLives);

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
        
        /// <summary>
        /// If a player has boosted on a tick.
        /// </summary>
        public bool playerBoosted = false;
        
        /// <summary>
        /// If a player has jumped on a tick.
        /// </summary>
        public bool playerJumped = false;

        public void UpdateLives(int lives)
        {
            this.lives = lives;
            for (int i = 0; i < PlayerManager.Players.Count; i++)
            {
                var player = PlayerManager.Players[i];
                player.lives = lives > 0 ? lives : player.GetControl()?.lives ?? -1;
            }
        }

        #endregion
    }
}
