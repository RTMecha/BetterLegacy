using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

using LSFunctions;

using SimpleJSON;

using BetterLegacy.Arcade.Managers;
using BetterLegacy.Configs;
using BetterLegacy.Core.Components.Player;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Data.Level;
using BetterLegacy.Core.Data.Player;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers.Settings;
using BetterLegacy.Core.Runtime;
using BetterLegacy.Story;

using Version = BetterLegacy.Core.Data.Version;

namespace BetterLegacy.Core.Managers
{
    /// <summary>
    /// Manages <see cref="Level"/>, <see cref="LevelCollection"/> and Arcade player data.
    /// </summary>
    public class LevelManager : BaseManager<LevelManager, LevelManagerSettings>
    {
        #region Values

        #region Path

        public static string Path
        {
            get => path;
            set => path = value;
        }

        static string path = "arcade";
        public static string ListPath => $"beatmaps/{Path}";
        public static string ListSlash => $"beatmaps/{Path}/";

        #endregion

        #region Levels & Level Collections

        /// <summary>
        /// Whether the scene after Input Select should be the Arcade scene or the Interface scene.
        /// </summary>
        public static bool IsArcade { get; set; } = true;

        /// <summary>
        /// If the level has ended.
        /// </summary>
        public static bool LevelEnded { get; set; }

        /// <summary>
        /// If the game is currently loading a level.
        /// </summary>
        public static bool LoadingFromHere { get; set; }

        /// <summary>
        /// Which level mode is going to load.
        /// </summary>
        public static int CurrentLevelMode { get; set; }

        /// <summary>
        /// The level that is currently being played if the game is in the arcade.
        /// </summary>
        public static Level CurrentLevel { get; set; }

        /// <summary>
        /// The current hub level to return to.
        /// </summary>
        public static Level Hub { get; set; }

        /// <summary>
        /// The level that was played before the current.
        /// </summary>
        public static Level PreviousLevel { get; set; }

        /// <summary>
        /// Local levels from within the arcade folder.
        /// </summary>
        public static List<Level> Levels { get; set; }

        /// <summary>
        /// The level collection that is currently open.
        /// </summary>
        public static LevelCollection CurrentLevelCollection { get; set; }

        /// <summary>
        /// Index of the current level in <see cref="CurrentLevelCollection"/>. Used for collection progression.
        /// </summary>
        public static int currentLevelIndex;

        /// <summary>
        /// The next level in the current Arcade Queue.
        /// </summary>
        public static Level NextLevel => ArcadeQueue.InRange(currentQueueIndex) ? ArcadeQueue[currentQueueIndex] : null;

        /// <summary>
        /// The next level to play in the level collection.
        /// </summary>
        public static Level NextLevelInCollection =>
            CurrentLevelCollection && CurrentLevelCollection.Count > currentLevelIndex + 1 ?
                CurrentLevelCollection[currentLevelIndex + 1] : null;

        /// <summary>
        /// Local collections from the arcade folder.
        /// </summary>
        public static List<LevelCollection> LevelCollections { get; set; }

        /// <summary>
        /// Levels added to the queue.
        /// </summary>
        public static List<Level> ArcadeQueue { get; set; }

        public static bool HasQueue => ArcadeQueue != null && ArcadeQueue.Count > 0;

        /// <summary>
        /// If <see cref="currentQueueIndex"/> is at the end of the Arcade Queue list.
        /// </summary>
        public static bool IsNextEndOfQueue => ArcadeQueue.Count <= 1 || currentQueueIndex + 1 >= ArcadeQueue.Count;

        /// <summary>
        /// If <see cref="currentQueueIndex"/> is at the end of the Arcade Queue list.
        /// </summary>
        public static bool IsEndOfQueue => ArcadeQueue.Count <= 1 || currentQueueIndex >= ArcadeQueue.Count;

        /// <summary>
        /// The current index in <see cref="ArcadeQueue"/>
        /// </summary>
        public static int currentQueueIndex;

        /// <summary>
        /// What should happen when an in-game level starts.
        /// </summary>
        public static Action<Level> OnLevelStart { get; set; }

        /// <summary>
        /// What should happen when an in-game level ends.
        /// </summary>
        public static Action OnLevelEnd { get; set; }

        /// <summary>
        /// How many times levels have been played.
        /// </summary>
        public static int PlayedLevelCount { get; set; }

        /// <summary>
        /// Used for customizing fade transition between levels.
        /// </summary>
        public static float songFadeTransition = 0.5f;

        #endregion

        #region Saves

        /// <summary>
        /// Arcade saves list.
        /// </summary>
        public static List<SaveData> Saves { get; set; } = new List<SaveData>();

        /// <summary>
        /// Arcade saves list.
        /// </summary>
        public static List<SaveCollectionData> CollectionSaves { get; set; } = new List<SaveCollectionData>();

        /// <summary>
        /// The current save file for the Arcade.
        /// </summary>
        public static string CurrentSaveFile { get; set; } = $"arcade_saves{FileFormat.LSS.Dot()}";

        /// <summary>
        /// Maximum amount of data points for the End Level Menu.
        /// </summary>
        public const int DATA_POINT_MAX = 24;

        #endregion

        #endregion

        #region Functions

        public override void OnInit()
        {
            Levels = new List<Level>();
            ArcadeQueue = new List<Level>();
            LevelCollections = new List<LevelCollection>();

            LoadProgress();
        }

        #region Loading & Sorting

        /// <summary>
        /// Loads the game scene and plays a level.
        /// </summary>
        /// <param name="level">The level to play.</param>
        public static void Play(Level level)
        {
            if (level)
                CoroutineHelper.StartCoroutine(IPlay(level));
        }

        /// <summary>
        /// Loads the game scene and plays a level.
        /// </summary>
        /// <param name="level">The level to play.</param>
        /// <param name="onLevelEnd">Function to run when the level ends.</param>
        public static void Play(Level level, Action onLevelEnd)
        {
            if (level)
            {
                OnLevelEnd = onLevelEnd;
                CoroutineHelper.StartCoroutine(IPlay(level));
            }
        }

        /// <summary>
        /// Loads the game scene and plays a level.
        /// </summary>
        /// <param name="level">The level to play.</param>
        public static IEnumerator IPlay(Level level)
        {
            Log($"Start playing level:\n{level}\nIs Story: {level.isStory}");

            LoadingFromHere = true;
            LevelEnded = false;

            AssignSaveData(level);

            PreviousLevel = CurrentLevel;
            CurrentLevel = level;

            if (level.metadata && level.metadata.isHubLevel)
                Hub = level;

            #region Init

            RandomHelper.UpdateSeed();

            Log($"Updating scene.");

            bool inGame = ProjectArrhythmia.State.InGame;
            if (!inGame || ProjectArrhythmia.State.InEditor)
            {
                Log($"Switching to Game scene.");
                SceneHelper.LoadGameWithProgress();
            }

            bool logged = false;
            while (ProjectArrhythmia.State.InEditor || !ProjectArrhythmia.State.InGame || !ShapeManager.inst.loadedShapes)
            {
                if (!logged)
                {
                    logged = true;
                    if (ProjectArrhythmia.State.InEditor)
                        Log($"Have to switch to the game scene from the editor.");
                    if (!ProjectArrhythmia.State.InGame)
                        Log($"Have to switch to the game scene.");
                    if (!ShapeManager.inst.loadedShapes)
                        Log($"Shapes haven't initialized yet.");
                }

                yield return null;
            }

            Log($"Resetting window states.");
            ProjectArrhythmia.Window.ResetPosition();
            ProjectArrhythmia.Window.ApplySettings();
            ProjectArrhythmia.Window.ResetTitle();

            if (BackgroundManager.inst)
                LSHelpers.DeleteChildren(BackgroundManager.inst.backgroundParent);

            RTBeatmap.Current.levelTimer.offset = 0f;
            RTBeatmap.Current.levelTimer.Reset();
            RTBeatmap.Current.boosts.Clear();
            RTBeatmap.Current.hits.Clear();
            RTBeatmap.Current.deaths.Clear();

            if (level.isStory && GetLevelRank(level) != Rank.SS)
            {
                RTBeatmap.Current.challengeMode = ChallengeMode.Normal;
                RTBeatmap.Current.gameSpeed = GameSpeed.X1_0;
                RTBeatmap.Current.lives = -1;
            }
            else
            {
                RTBeatmap.Current.challengeMode = CoreConfig.Instance.ChallengeModeSetting.Value;
                RTBeatmap.Current.gameSpeed = CoreConfig.Instance.GameSpeedSetting.Value;
                RTBeatmap.Current.lives = RTBeatmap.Current.challengeMode.Lives;
            }

            #endregion

            #region Parsing

            Log($"Parsing level...");

            GameManager.inst.gameState = GameManager.State.Parsing;

            var storyLevel = level as StoryLevel;

            ProjectArrhythmia.State.InStory = level.isStory;

            MetaData.Current = level.metadata;
            if (level.isInterface)
            {
                RTFile.BasePath = RTFile.AppendEndSlash(level.path);
                Menus.InterfaceManager.inst.ParseInterface(level.path);
                GameManager.inst.gameState = GameManager.State.Playing;
                yield break;
            }
            else
            {
                SetCurrentGameData(level);
                if (GameData.Current && GameData.Current.data && GameData.Current.data.level)
                    RTBeatmap.Current.respawnImmediately = GameData.Current.data.level.respawnImmediately;
            }

            if (level.IsVG)
                AchievementManager.inst.UnlockAchievement("time_traveler");

            ThemeManager.inst.UpdateAllThemes();

            Log($"Setting paths...");
            GameManager.inst.currentLevelName = level.metadata.beatmap.name;
            RTFile.BasePath = RTFile.AppendEndSlash(level.path);

            #endregion

            #region States

            Log($"Updating states...");

            if (IsArcade || !ProjectArrhythmia.State.InStory)
                DiscordHelper.UpdateDiscordStatus($"Level: {level.metadata.beatmap.name}",
                    "In Arcade",
                    "arcade");
            else
                DiscordHelper.UpdateDiscordStatus(
                    $"DOC{RTString.ToStoryNumber(StoryManager.inst.currentPlayingChapterIndex)}-{RTString.ToStoryNumber(StoryManager.inst.currentPlayingLevelSequenceIndex)}: {level.metadata.beatmap.name}",
                    "In Story",
                    "arcade");

            if (CoreConfig.Instance.DiscordTimestampUpdatesPerLevel.Value)
                DiscordController.inst.presence.startTimestamp = SteamworksFacepunch.Epoch.Current;

            while (!GameManager.inst.introTitle && !GameManager.inst.introArtist)
                yield return null;

            GameManager.inst.introTitle.text = level.metadata.song.title;
            GameManager.inst.introArtist.text = level.metadata.artist.name;

            RTBeatmap.Current.LevelStarted = true;

            #endregion

            #region Music

            Log($"Loading music...\nMusic is null: {!level.music}");

            if (!level.music)
                yield return CoroutineHelper.StartCoroutine(level.LoadAudioClipRoutine());

            Log($"Playing music... music state: {level.music}");

            while (!level.music)
                yield return null;

            SetCurrentAudio(level.music);
            GameManager.inst.songLength = level.music.length;

            // preload audio clips
            if (GameData.Current && GameData.Current.assets)
                yield return CoroutineHelper.StartCoroutine(GameData.Current.assets.LoadSounds());

            #endregion

            #region Camera

            Log($"Setting Camera...");

            if (!storyLevel)
                yield return RTVideoManager.inst.Setup(level.path);
            else if (storyLevel.videoClip)
                RTVideoManager.inst.Play(storyLevel.videoClip);

            RTGameManager.inst.SetCameraArea(new Rect(0f, 0f, 1f, 1f));

            #endregion

            #region Checkpoints

            Log($"Updating checkpoints...");

            GameManager.inst.UpdateTimeline();
            RTBeatmap.Current.ResetCheckpoint();

            #endregion

            #region Spawning

            Log($"Spawning...");

            if (!storyLevel)
                PlayersData.Load(level.GetFile(Level.PLAYERS_LSB));
            else
                PlayersData.LoadJSON(!string.IsNullOrEmpty(storyLevel.jsonPlayers) ? JSON.Parse(storyLevel.jsonPlayers) : new JSONNull());

            PlayerManager.ValidatePlayers();
            PlayerManager.AssignPlayerModels();

            RTPlayer.GameMode = GameMode.Regular;

            RTGameManager.inst.PlayIntro();
            PlayerManager.SpawnPlayersOnStart();

            RTPlayer.SetGameDataProperties();

            yield return inst.StartCoroutine(RTLevel.IReinit());

            CursorManager.inst.HideCursor();

            if (RTBeatmap.Current.shouldResetEndFuncOnStart)
                RTBeatmap.Current.ResetEndLevelVariables();
            RTBeatmap.Current.shouldResetEndFuncOnStart = true;

            #endregion

            #region Done

            Log($"Done!");

            GameManager.inst.gameState = GameManager.State.Playing;
            AudioManager.inst.SetMusicTime(GameData.Current.data.level.LevelStartOffset);

            LoadingFromHere = false;

            ResetTransition();
            RTBeatmap.Current.CurrentMusicVolume = CoreConfig.Instance.MusicVol.Value;
            AchievementManager.inst.CheckLevelBeginAchievements();

            OnLevelStart?.Invoke(level);
            OnLevelStart = null;

            LegacyPlugin.AddRecentArcadeLevel(level);

            #endregion
        }

        /// <summary>
        /// Loads a level from anywhere. For example: LevelManager.Load("E:/4.1.16/beatmaps/story/Apocrypha/level.lsb");
        /// </summary>
        /// <param name="path">Path to the level. Can either be an asset file or a normal level format.</param>
        public static void Load(string path)
        {
            if (!RTFile.FileExists(path))
            {
                LogError($"Couldn't load level from {path} as it doesn't exist.");
                return;
            }

            Log($"Loading level from {path}");

            OnLevelEnd = RTBeatmap.Current.EndOfLevel;

            if (path.EndsWith(FileFormat.ASSET.Dot()))
            {
                CoroutineHelper.StartCoroutine(StoryLevel.LoadFromAsset(path, Play));
                return;
            }

            Play(new Level(path.Remove(Level.LEVEL_LSB).Remove(Level.LEVEL_VGD)));
        }

        /// <summary>
        /// Sets the current <see cref="MetaData"/>.
        /// </summary>
        /// <param name="metaData">Meta data to set.</param>
        public static void SetCurrentMetaData(MetaData metaData)
        {
            try
            {
                MetaData.Current = null;
                MetaData.Current = metaData;

                if (MetaData.Current.arcadeID == null || MetaData.Current.arcadeID == "0" || MetaData.Current.arcadeID == "-1")
                    MetaData.Current.arcadeID = LSText.randomNumString(16);
            }
            catch
            {
                throw new MetaDataException("Failed to set meta data.");
            }
        }

        /// <summary>
        /// Sets the current <see cref="GameData"/>.
        /// </summary>
        /// <param name="level">Level to load the game data from.</param>
        public static void SetCurrentGameData(Level level)
        {
            GameData.Current = null;
            GameData.Current = level.LoadGameData();
        }

        /// <summary>
        /// Sets the current audio the game should use.
        /// </summary>
        /// <param name="audioClip">Audio to set.</param>
        public static void SetCurrentAudio(AudioClip audioClip) => AudioManager.inst.PlayMusic(null, audioClip, true, ProjectArrhythmia.State.InEditor ? 0f : songFadeTransition, false);
        
        /// <summary>
        /// Clears all objects before loading a level.
        /// </summary>
        public static void ClearObjects()
        {
            if (GameData.Current)
            {
                try
                {
                    for (int i = 0; i < GameData.Current.assets.sounds.Count; i++)
                    {
                        CoreHelper.Destroy(GameData.Current.assets.sounds[i].audio);
                        GameData.Current.assets.sounds[i].audio = null;
                    }
                    GameData.Current.assets.sounds.Clear();
                }
                catch (Exception ex)
                {
                    CoreHelper.LogError($"Failed to clear sound assets due to the exception: {ex}");
                }
            }

            RTLevel.Reinit(false);
        }

        /// <summary>
        /// Resets the level transition values to the defaults.
        /// </summary>
        public static void ResetTransition()
        {
            songFadeTransition = 0.5f;
            RTGameManager.doIntroFade = true;
        }

        /// <summary>
        /// Clears any left over data.
        /// </summary>
        public static void Clear()
        {
            DG.Tweening.DOTween.Clear();
            try
            {
                RTLevel.Reinit(false);
            }
            catch (Exception ex)
            {
                CoreHelper.LogException(ex);
            } // try cleanup
            GameData.Current?.Clear();
            GameData.Current = null;
            InputDataManager.inst.SetAllControllerRumble(0f);
        }

        /// <summary>
        /// Clears all level data to save on memory.
        /// </summary>
        public static void ClearData()
        {
            try
            {
                if (CurrentLevelCollection && CurrentLevelCollection.previewAudio)
                {
                    CurrentLevelCollection.previewAudio.UnloadAudioData();
                    CoreHelper.Destroy(CurrentLevelCollection.previewAudio);
                    CurrentLevelCollection.previewAudio = null;
                }
                CurrentLevelCollection = null;

                if (CurrentLevel && CurrentLevel.music)
                {
                    CurrentLevel.music.UnloadAudioData();
                    CoreHelper.Destroy(CurrentLevel.music);
                    CurrentLevel.music = null;
                }
                CurrentLevel = null;

                for (int i = 0; i < LevelCollections.Count; i++)
                {
                    var collection = LevelCollections[i];

                    if (!collection)
                        continue;

                    collection.levelInformation.Clear();
                    collection.levels.Clear();

                    collection.icon = null;
                    collection.banner = null;

                    if (collection.previewAudio)
                    {
                        collection.previewAudio.UnloadAudioData();
                        CoreHelper.Destroy(collection.previewAudio);
                        collection.previewAudio = null;
                    }

                    LevelCollections[i] = null;
                }
                LevelCollections.Clear();

                for (int i = 0; i < Levels.Count; i++)
                {
                    var level = Levels[i];

                    if (!level)
                        continue;

                    level.metadata = null;
                    level.icon = null;
                    level.saveData = null;
                    if (level.achievements != null)
                        level.achievements.Clear();

                    if (level.music)
                    {
                        level.music.UnloadAudioData();
                        CoreHelper.Destroy(level.music);
                        level.music = null;
                    }

                    Levels[i] = null;
                }
                Levels.Clear();

                OnLevelStart = null;
                OnLevelEnd = null;

                Arcade.Interfaces.ArcadeMenu.OnlineLevelIcons.Clear();
                Arcade.Interfaces.ArcadeMenu.OnlineSteamLevelIcons.Clear();
            }
            catch (Exception ex)
            {
                CoreHelper.LogException(ex);
            }
        }

        /// <summary>
        /// Sorts a Level list by a specific order and ascending / descending.
        /// </summary>
        /// <param name="levels">The Level list to sort.</param>
        /// <param name="sort">How the Level list should be ordered by.</param>
        /// <param name="ascend">Whether the list should ascend of descend.</param>
        /// <returns>Returns a sorted Level list.</returns>
        public static List<Level> SortLevels(List<Level> levels, LevelSort sort, bool ascend) => sort switch
        {
            LevelSort.Cover => levels.Order(x => x.icon != LegacyPlugin.AtanPlaceholder, !ascend),
            LevelSort.Artist => levels.Order(x => x.metadata.artist.name, !ascend),
            LevelSort.Creator => levels.Order(x => x.metadata.creator.name, !ascend),
            LevelSort.File => levels.Order(x => System.IO.Path.GetFileName(x.path), !ascend),
            LevelSort.Title => levels.Order(x => x.metadata.song.title, !ascend),
            LevelSort.Difficulty => levels.Order(x => x.metadata.song.difficulty, !ascend),
            LevelSort.DateEdited => levels.Order(x => x.metadata.beatmap.dateEdited, !ascend),
            LevelSort.DateCreated => levels.Order(x => x.metadata.beatmap.dateCreated, !ascend),
            LevelSort.DatePublished => levels.Order(x => x.metadata.beatmap.datePublished, !ascend),
            LevelSort.Ranking => levels.Order(x =>
            {
                var saveData = GetSaveData(x.id);
                return saveData ? saveData.Hits : int.MaxValue;
            }, !ascend),
            _ => levels,
        };

        /// <summary>
        /// Sorts <see cref="Levels"/> by a specific order and ascending / descending.
        /// </summary>
        /// <param name="sort">How the Level list should be ordered by.</param>
        /// <param name="ascend">Whether the list should ascend of descend.</param>
        public static void Sort(LevelSort sort, bool ascend) => Levels = SortLevels(Levels, sort, ascend);

        /// <summary>
        /// Updates the Beatmap JSON depending on version.
        /// </summary>
        /// <param name="json">The Beatmap JSON to update.</param>
        /// <param name="ver">The PA version the Beatmap was made in.</param>
        /// <returns>Updated Beatmap JSON.</returns>
        public static string UpdateBeatmap(string json, string ver) => UpdateBeatmap(json, new Version(ver));

        /// <summary>
        /// Updates the Beatmap JSON depending on version.
        /// </summary>
        /// <param name="json">The Beatmap JSON to update.</param>
        /// <param name="version">The PA version the Beatmap was made in.</param>
        /// <returns>Updated Beatmap JSON.</returns>
        public static string UpdateBeatmap(string json, Version version)
        {
            CoreHelper.Log($"[ -- Updating Beatmap! -- ] - [{version}]");

            // 3.7.26
            if (version.Major <= 3 && version.Minor <= 7 && version.Patch <= 26)
            {
                Log("value_x -> x & value_y -> y");
                json = json.Replace("\"value_x\"", "\"x\"");
                json = json.Replace("\"value_y\"", "\"y\"");
            }

            // 3.7.42
            if (version.Major <= 3 && version.Minor <= 7 && version.Patch <= 42)
            {
                Log("text 4 -> 5");
                json = json.Replace("\"shape\": \"4\"", "\"shape\": \"5\"");
            }

            // 3.8.15
            if (version.Major <= 3 && version.Minor <= 8 && version.Patch <= 15)
                Log("Add parent relationship if none");

            // 3.8.25
            if (version.Major <= 3 && version.Minor <= 8 && version.Patch <= 25)
            {
                Log("background_objects -> bg_objects");
                json = json.Replace("\"background_objects\"", "\"bg_objects\"");
                Log("reactive_settings -> r_set");
                json = json.Replace("\"reactive_settings\"", "\"r_set\"");
            }

            // 3.8.48
            if (version.Major <= 3 && version.Minor <= 8 && version.Patch <= 48)
            {
                Log("is_random -> r");
                json = json.Replace("\"is_random\":\"False\"", "\"r\":\"0\"").Replace("\"is_random\":\"True\"", "\"r\":\"1\"");
                json = json.Replace("\"is_random\": \"False\"", "\"r\": \"0\"").Replace("\"is_random\": \"True\"", "\"r\": \"1\"");
                Log("origin -> o");
                json = json.Replace("\"origin\"", "\"o\"");
                Log("time -> t");
                json = json.Replace("\"time\"", "\"t\"");
                Log("start_time -> st");
                json = json.Replace("\"start_time\"", "\"st\"");
                Log("editor_data -> ed");
                json = json.Replace("\"editor_data\"", "\"ed\"");
                Log("value_random_x -> rx");
                json = json.Replace("\"value_random_x\"", "\"rx\"");
                Log("value_random_y -> ry");
                json = json.Replace("\"value_random_y\"", "\"ry\"");
                Log("value_z -> z");
                json = json.Replace("\"value_z\"", "\"z\"").Replace("\"value_z2\"", "\"z2\"");
                Log("curve_type -> ct");
                json = json.Replace("\"curve_type\"", "\"ct\"");
                Log("p_type -> pt");
                json = json.Replace("\"p_type\"", "\"pt\"");
                Log("parent -> p");
                json = json.Replace("\"parent\"", "\"p\"");
                Log("helper -> h");
                json = json.Replace("\"helper\"", "\"h\"");
                Log("depth -> d");
                json = json.Replace("\"depth\"", "\"d\"");
                Log("prefab_id -> pid");
                json = json.Replace("\"prefab_id\"", "\"pid\"");
                Log("prefab_inst_id -> piid");
                json = json.Replace("\"prefab_inst_id\"", "\"piid\"");
                Log("shape_option -> so");
                json = json.Replace("\"shape_option\"", "\"so\"");
            }

            return json;
        }

        /// <summary>
        /// Forces the level to end.
        /// </summary>
        public static void EndLevel()
        {
            GameManager.inst.gameState = GameManager.State.Finish;

            Time.timeScale = 1f;
            InputDataManager.inst.SetAllControllerRumble(0f);

            LevelEnded = true;
            OnLevelEnd?.Invoke();
        }

        #endregion

        #region Save Data

        /// <summary>
        /// Finds and sets the levels' save data.
        /// </summary>
        /// <param name="level">Level to assign to.</param>
        public static void AssignSaveData(Level level)
        {
            if (!level)
                return;

            if (Saves.TryFind(x => x.ID == level.id, out SaveData saveData))
            {
                level.saveData = saveData;
                saveData.LevelName = level.metadata?.beatmap?.name;
            }
            else
            {
                level.saveData = new SaveData(level);
                Saves.Add(level.saveData);
            }

            level.LoadAchievements();
        }

        public static void AssignSaveData(LevelCollection levelCollection)
        {
            if (!levelCollection)
                return;

            if (CollectionSaves.TryFind(x => x.ID == levelCollection.id, out SaveCollectionData saveData))
            {
                levelCollection.saveData = saveData;
                saveData.LevelCollectionName = levelCollection.name;
            }
            else
            {
                levelCollection.saveData = new SaveCollectionData(levelCollection);
                CollectionSaves.Add(levelCollection.saveData);
            }

            levelCollection.LoadAchievements();
        }

        /// <summary>
        /// Updates the players played level data.
        /// </summary>
        public static void UpdateCurrentLevelProgress()
        {
            if (CurrentLevelCollection && CurrentLevelCollection.saveData)
            {
                if (currentLevelIndex + 1 >= CurrentLevelCollection.Count)
                {
                    CurrentLevelCollection.saveData.Completed = true;
                    CurrentLevelCollection.saveData.PlayedTimes++;
                }
                CurrentLevelCollection.saveData.LastPlayed = DateTime.Now;
            }

            if (!IsArcade || !CurrentLevel && !NextLevelInCollection)
                return;

            CoreHelper.Log($"Setting Player Data");

            PlayedLevelCount++;

            if (Saves.Where(x => x.Completed).Count() >= 10)
                AchievementManager.inst.UnlockAchievement("ten_levels");
            if (Saves.Where(x => x.Completed).Count() >= 50)
                AchievementManager.inst.UnlockAchievement("fifty_levels");
            if (Saves.Where(x => x.Completed).Count() >= 100)
                AchievementManager.inst.UnlockAchievement("one_hundred_levels");

            if (RTBeatmap.Current.challengeMode.Invincible)
            {
                SaveProgress();
                return;
            }

            var levels = CurrentLevelCollection ? CurrentLevelCollection.levels : Levels;

            if (NextLevelInCollection)
                SetLevelData(levels, NextLevelInCollection, false);
            SetLevelData(levels, CurrentLevel, true);
        }

        static void SetLevelData(List<Level> levels, Level currentLevel, bool update)
        {
            if (!currentLevel)
                return;

            bool makeNewSaveData = false;
            if (!currentLevel.saveData)
            {
                makeNewSaveData = true;
                currentLevel.saveData = new SaveData(currentLevel);
            }

            currentLevel.saveData.LevelName = currentLevel.metadata?.beatmap?.name; // update level name
            currentLevel.saveData.LastPlayed = DateTime.Now;

            if (update && currentLevel.metadata && currentLevel.metadata.song.Difficulty == DifficultyType.Animation) // animation levels shouldn't update rank data
            {
                currentLevel.saveData.Completed = true;
                currentLevel.saveData.PlayedTimes++;

                try
                {
                    if (AudioManager.inst.CurrentAudioSource.clip)
                    {
                        var length = AudioManager.inst.CurrentAudioSource.clip.length;
                        if (currentLevel.saveData.LevelLength != length)
                            currentLevel.saveData.LevelLength = length;

                        float calc = AudioManager.inst.CurrentAudioSource.time / length * 100f;

                        if (currentLevel.saveData.Percentage < calc)
                            currentLevel.saveData.Percentage = calc;

                        currentLevel.saveData.TimeInLevel = RTBeatmap.Current.levelTimer.time;
                    }
                }
                catch (Exception ex)
                {
                    CoreHelper.LogException(ex);
                }
            }
            else if (update)
            {
                CoreHelper.Log($"Updating save data\n" +
                    $"New Player Data = {makeNewSaveData}\n" +
                    $"Deaths [OLD = {currentLevel.saveData.Deaths} > NEW = {RTBeatmap.Current.deaths.Count}]\n" +
                    $"Hits: [OLD = {currentLevel.saveData.Hits} > NEW = {RTBeatmap.Current.hits.Count}]\n" +
                    $"Boosts: [OLD = {currentLevel.saveData.Boosts} > NEW = {RTBeatmap.Current.boosts.Count}]");

                currentLevel.saveData.Update(RTBeatmap.Current.deaths.Count, RTBeatmap.Current.hits.Count, RTBeatmap.Current.boosts.Count, true);
                currentLevel.saveData.PlayedTimes++;
            }

            if (currentLevel.metadata && currentLevel.metadata.unlockAfterCompletion)
                currentLevel.saveData.Unlocked = true;

            if (Saves.TryFindIndex(x => x.ID == currentLevel.id, out int saveIndex))
                Saves[saveIndex] = currentLevel.saveData;
            else
                Saves.Add(currentLevel.saveData);

            if (levels.TryFind(x => x.id == currentLevel.id, out Level level))
                level.saveData = currentLevel.saveData;
            currentLevel.LoadAchievements();

            SaveProgress();
        }

        /// <summary>
        /// Saves the current player save data.
        /// </summary>
        public static void SaveProgress()
        {
            var jn = Parser.NewJSONObject();
            int num = 0;
            for (int i = 0; i < Saves.Count; i++)
            {
                var save = Saves[i];
                if (!save.ShouldSerialize)
                    continue;

                jn["lvl"][num] = save.ToJSON();
                num++;
            }
            num = 0;
            for (int i = 0; i < CollectionSaves.Count; i++)
            {
                var save = CollectionSaves[i];
                if (!save.ShouldSerialize)
                    continue;

                jn["lvl_col"][num] = save.ToJSON();
                num++;
            }
            jn["played_count"] = PlayedLevelCount;

            var profilePath = RTFile.ApplicationDirectory + "profile";
            RTFile.CreateDirectory(profilePath);

            var json = jn.ToString();
            RTFile.WriteToFile(RTFile.CombinePaths(profilePath, CurrentSaveFile), json);
        }

        /// <summary>
        /// Loads the current player save data.
        /// </summary>
        public static void LoadProgress()
        {
            Saves.Clear();
            CollectionSaves.Clear();

            var profilePath = RTFile.ApplicationDirectory + "profile";
            JSONNode jn;
            if (!RTFile.FileExists(RTFile.CombinePaths(profilePath, CurrentSaveFile)))
            {
                var modded = RTFile.FileExists(RTFile.CombinePaths(profilePath, "saves.les"));
                var path = modded ? RTFile.CombinePaths(profilePath, "saves.les") : RTFile.ApplicationDirectory + $"settings/saves{FileFormat.LSS.Dot()}";
                if (!RTFile.FileExists(path))
                    return;

                jn = JSON.Parse(LSEncryption.DecryptText(RTFile.ReadFromFile(path), SaveManager.inst.encryptionKey));

                // parse old saves and then create a new save.
                if (!modded)
                {
                    for (int i = 0; i < jn["arcade"].Count; i++)
                        Saves.Add(SaveData.ParseVanilla(jn["arcade"][i]));

                    SaveProgress();
                    return;
                }
            }
            else
                jn = JSON.Parse(RTFile.ReadFromFile(RTFile.CombinePaths(profilePath, CurrentSaveFile)));

            if (jn == null)
                return;

            for (int i = 0; i < jn["lvl"].Count; i++)
                Saves.Add(SaveData.Parse(jn["lvl"][i]));
            if (jn["lvl_col"] != null)
                for (int i = 0; i < jn["lvl_col"].Count; i++)
                    CollectionSaves.Add(SaveCollectionData.Parse(jn["lvl_col"][i]));

            if (jn["played_count"] != null)
                PlayedLevelCount = jn["played_count"].AsInt;
        }

        /// <summary>
        /// Removes player data.
        /// </summary>
        /// <param name="id">ID of the player data to remove.</param>
        public static void RemoveSaveData(string id)
        {
            Saves.Remove(x => x.ID == id);
            SaveProgress();
        }

        /// <summary>
        /// Gets a levels' player data.
        /// </summary>
        /// <param name="id">ID of the player data to get.</param>
        /// <returns>Returns the player data of the level.</returns>
        public static SaveData GetSaveData(string id) => Saves.Find(x => x.ID == id);

        /// <summary>
        /// Gets the normalized amount of hits.
        /// </summary>
        /// <param name="hits">Hits to normalize.</param>
        /// <returns>Returns an array representing the normalized hits.</returns>
        public static int[] GetHitsNormalized(List<PlayerDataPoint> hits)
        {
            int[] hitsNormalized = new int[DATA_POINT_MAX + 1];
            foreach (var playerDataPoint in hits)
            {
                int num5 = (int)RTMath.SuperLerp(0f, AudioManager.inst.CurrentAudioSource.clip.length, 0f, (float)DATA_POINT_MAX, playerDataPoint.time);
                hitsNormalized[num5]++;
            }

            return hitsNormalized;
        }

        /// <summary>
        /// The regular level rank calculation method.
        /// </summary>
        /// <param name="hits">Hits player data list.</param>
        /// <returns>A calculated rank from hits.</returns>
        public static Rank GetLevelRank(List<PlayerDataPoint> hits)
        {
            if (ProjectArrhythmia.State.InEditor)
                return Rank.EditorRank;

            if (!ProjectArrhythmia.State.InStory && RTBeatmap.Current.challengeMode.Invincible)
                return Rank.Null;

            var hitsNormalized = GetHitsNormalized(hits);
            var ranks = Rank.Null.GetValues();
            return Rank.Null.TryGetValue(x => hitsNormalized.Sum() >= x.MinHits && hitsNormalized.Sum() <= x.MaxHits, out Rank rankType) ? rankType : Rank.Null;
        }

        /// <summary>
        /// Gets a level rank by hit count.
        /// </summary>
        /// <param name="hits">Hit count.</param>
        /// <returns>A calculated rank from the amount of hits.</returns>
        public static Rank GetLevelRank(int hits)
            => ProjectArrhythmia.State.InEditor ? Rank.EditorRank : Rank.Null.TryGetValue(x => hits >= x.MinHits && hits <= x.MaxHits, out Rank rankType) ? rankType : Rank.Null;

        /// <summary>
        /// Gets a levels' rank.
        /// </summary>
        /// <param name="level">Level to get a rank from.</param>
        /// <returns>A levels' stored rank.</returns>
        public static Rank GetLevelRank(Level level) => GetLevelRank(level?.saveData?.Hits ?? -1);

        /// <summary>
        /// Gets a levels' rank.
        /// </summary>
        /// <param name="playerData">PlayerData to get a rank from.</param>
        /// <returns>A levels' stored rank.</returns>
        public static Rank GetLevelRank(SaveData playerData) => GetLevelRank(playerData?.Hits ?? -1);

        /// <summary>
        /// Calculates the players' accuracy in a level.
        /// </summary>
        /// <param name="hits">Amount of hits.</param>
        /// <param name="length">Total length of the level contributes to the accuracy.</param>
        /// <returns>Returns a calculated accuracy.</returns>
        public static float CalculateAccuracy(int hits, float length) => 100f / ((hits / (length / 10f)) + 1f);

        #endregion

        #endregion
    }
}
