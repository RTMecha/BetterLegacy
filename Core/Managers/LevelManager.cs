﻿using BetterLegacy.Components.Player;
using BetterLegacy.Configs;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Optimization;
using BetterLegacy.Story;
using LSFunctions;
using SimpleJSON;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace BetterLegacy.Core.Managers
{
    public class LevelManager : MonoBehaviour
    {
        public static LevelManager inst;
        public static string className = "[<color=#7F00FF>LevelManager</color>] \n";

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

        #region Data

        public static float timeInLevel = 0f;
        public static float timeInLevelOffset = 0f;

        /// <summary>
        /// Used for the no music achievement.
        /// </summary>
        public static int CurrentMusicVolume { get; set; }

        /// <summary>
        /// Action that occurs when a player selects all controller inputs in the Input Select screen and loads the next scene.
        /// </summary>
        public static Action OnInputsSelected { get; set; }

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

        public static LevelCollection CurrentLevelCollection { get; set; }

        public static int currentLevelIndex;

        public static Level NextLevel =>
            CurrentLevelCollection && CurrentLevelCollection.Count > currentLevelIndex ?
                CurrentLevelCollection[currentLevelIndex] :
                ArcadeQueue.Count > currentQueueIndex ?
                    ArcadeQueue[currentQueueIndex] :
                    null;

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
        public static bool IsNextEndOfQueue
            => ArcadeQueue.Count <= 1 || currentQueueIndex + 1 >= ArcadeQueue.Count;

        /// <summary>
        /// If <see cref="currentQueueIndex"/> is at the end of the Arcade Queue list.
        /// </summary>
        public static bool IsEndOfQueue
            => ArcadeQueue.Count <= 1 || currentQueueIndex >= ArcadeQueue.Count;

        /// <summary>
        /// The current index in <see cref="ArcadeQueue"/>
        /// </summary>
        public static int currentQueueIndex;

        /// <summary>
        /// How many times a player has boosted.
        /// </summary>
        public static int BoostCount { get; set; }

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

        /// <summary>
        /// Inits LevelManager.
        /// </summary>
        public static void Init() => Creator.NewGameObject(nameof(LevelManager), SystemManager.inst.transform).AddComponent<LevelManager>();

        void Awake()
        {
            inst = this;
            Levels = new List<Level>();
            ArcadeQueue = new List<Level>();
            LevelCollections = new List<LevelCollection>();

            if (!RTFile.FileExists(RTFile.ApplicationDirectory + "profile/saves.les") && RTFile.FileExists(RTFile.ApplicationDirectory + "settings/save.lss"))
                UpgradeProgress();
            else
                LoadProgress();
        }

        void Update()
        {
            if (CoreHelper.InEditor && EditorManager.inst.isEditing)
                BoostCount = 0;
        }

        #region Loading & Sorting

        /// <summary>
        /// Loads the game scene and plays a level.
        /// </summary>
        /// <param name="level">The level to play.</param>
        /// <returns></returns>
        public static IEnumerator Play(Level level)
        {
            Debug.Log($"{className}Start playing level:\n{level}\nIs Story: {level.isStory}");

            LoadingFromHere = true;
            LevelEnded = false;

            if (level.playerData == null && (level.isStory ? StoryManager.inst.Saves : Saves).TryFind(x => x.ID == level.id, out PlayerData playerData))
                level.playerData = playerData;

            PreviousLevel = CurrentLevel;
            CurrentLevel = level;

            if (level.metadata != null && level.metadata.isHubLevel)
                Hub = level;

            RandomHelper.UpdateSeed();

            Debug.Log($"{className}Switching to Game scene");

            bool inGame = CoreHelper.InGame;
            if (!inGame || CoreHelper.InEditor)
                SceneHelper.LoadGameWithProgress();

            Debug.Log($"{className}Loading music...\nMusic is null: {!level.music}");

            if (!level.music)
                yield return CoreHelper.StartCoroutine(level.LoadAudioClipRoutine());

            Debug.Log($"{className}Waiting...\n" +
                $"In Editor: {CoreHelper.InEditor}\n" +
                $"In Game: {CoreHelper.InGame}\n" +
                $"Loaded Shapes: {ShapeManager.inst.loadedShapes}");
            if (CoreHelper.InEditor || !CoreHelper.InGame || !ShapeManager.inst.loadedShapes)
                while (CoreHelper.InEditor || !CoreHelper.InGame || !ShapeManager.inst.loadedShapes)
                    yield return null;

            Debug.Log($"{className}Resetting Window resolution.");
            if (RTEventManager.windowPositionResolutionChanged)
            {
                RTEventManager.windowPositionResolutionChanged = false;
                WindowController.ResetResolution();
            }

            WindowController.ResetTitle();

            if (BackgroundManager.inst)
                LSHelpers.DeleteChildren(BackgroundManager.inst.backgroundParent);

            Debug.Log($"{className}Parsing level...");

            GameManager.inst.gameState = GameManager.State.Parsing;

            StoryLevel storyLevel = null;
            if (level is StoryLevel storyLevelResult)
                storyLevel = storyLevelResult;

            CoreHelper.InStory = level.isStory;
            if (level.isStory && storyLevel)
                GameData.Current = GameData.Parse(JSON.Parse(UpdateBeatmap(storyLevel.json, level.metadata.beatmap.game_version)));
            else
            {
                var levelMode = level.CurrentFile;
                Debug.Log($"{className}Level Mode: {levelMode}...");

                var rawJSON = RTFile.ReadFromFile(RTFile.CombinePaths(level.path, levelMode));
                if (level.metadata.beatmap.game_version != "4.1.16" && level.metadata.beatmap.game_version != "20.4.4")
                    rawJSON = UpdateBeatmap(rawJSON, level.metadata.beatmap.game_version);

                if (level.IsVG)
                    AchievementManager.inst.UnlockAchievement("time_traveler");

                GameData.Current = level.IsVG ? GameData.ParseVG(JSON.Parse(rawJSON), version: level.metadata.Version) : GameData.Parse(JSONNode.Parse(rawJSON));
            }

            Debug.Log($"{className}Setting paths...");

            DataManager.inst.metaData = level.metadata;
            GameManager.inst.currentLevelName = level.metadata.song.title;
            RTFile.BasePath = RTFile.AppendEndSlash(level.path);

            Debug.Log($"{className}Updating states...");

            if (IsArcade || !CoreHelper.InStory)
                CoreHelper.UpdateDiscordStatus($"Level: {level.metadata.beatmap.name}", "In Arcade", "arcade");
            else
            {
                int chapter = StoryManager.inst.currentPlayingChapterIndex;
                int storyLevelIndex = StoryManager.inst.currentPlayingLevelSequenceIndex;
                CoreHelper.UpdateDiscordStatus($"DOC{(chapter + 1).ToString("00")}-{(storyLevelIndex + 1).ToString("00")}: {level.metadata.beatmap.name}", "In Story", "arcade");
            }

            while (!GameManager.inst.introTitle && !GameManager.inst.introArtist)
                yield return null;

            GameManager.inst.introTitle.text = level.metadata.song.title;
            GameManager.inst.introArtist.text = level.metadata.artist.Name;

            Debug.Log($"{className}Playing music...");

            while (!level.music)
                yield return null;

            AudioManager.inst.PlayMusic(null, level.music, true, songFadeTransition, false);
            AudioManager.inst.SetPitch(CoreHelper.Pitch);
            GameManager.inst.songLength = level.music.length;
            songFadeTransition = 0.5f;

            if (!CurrentLevel.isStory)
                yield return RTVideoManager.inst.Setup(level.path);
            else if (storyLevel && storyLevel.videoClip)
                RTVideoManager.inst.Play(storyLevel.videoClip);

            Debug.Log($"{className}Setting Camera sizes...");

            EventManager.inst.cam.rect = new Rect(0f, 0f, 1f, 1f);
            EventManager.inst.camPer.rect = new Rect(0f, 0f, 1f, 1f);

            Debug.Log($"{className}Updating checkpoints...");

            GameManager.inst.UpdateTimeline();
            GameManager.inst.ResetCheckpoints();

            Debug.Log($"{className}Spawning...");
            BoostCount = 0;
            if (InputDataManager.inst.players.Count == 0)
            {
                var customPlayer = new Data.Player.CustomPlayer(true, 0, null);
                InputDataManager.inst.players.Add(customPlayer);
            }
            else
            {
                for (int i = 0; i < PlayerManager.Players.Count; i++)
                    DestroyImmediate(PlayerManager.Players[i].GameObject);
            }

            PlayerManager.allowController = PlayerConfig.Instance.AllowControllerIfSinglePlayer.Value && InputDataManager.inst.players.Count == 1;

            PlayerManager.LoadLocalModels();

            PlayerManager.AssignPlayerModels();

            RTPlayer.GameMode = GameMode.Regular;

            GameStorageManager.inst.PlayIntro();
            GameManager.inst.SpawnPlayers(GameData.Current.beatmapData.checkpoints[0].pos);

            RTPlayer.SetGameDataProperties();

            EventManager.inst?.updateEvents();
            RTEventManager.inst?.SetResetOffsets();

            BackgroundManager.inst.UpdateBackgrounds();
            yield return inst.StartCoroutine(Updater.IUpdateObjects(true));

            LSHelpers.HideCursor();

            Debug.Log($"{className}Done!");

            GameManager.inst.gameState = GameManager.State.Playing;
            AudioManager.inst.SetMusicTime(0f);

            LoadingFromHere = false;

            CurrentMusicVolume = CoreConfig.Instance.MusicVol.Value;
            AchievementManager.inst.CheckLevelBeginAchievements();
        }

        /// <summary>
        /// Loads a level from anywhere. For example: LevelManager.Load("E:/4.1.16/beatmaps/story/Apocrypha/level.lsb");
        /// </summary>
        /// <param name="path"></param>
        public static void Load(string path, bool setLevelEnd = true)
        {
            if (!RTFile.FileExists(path))
            {
                Debug.LogError($"{className}Couldn't load level from {path} as it doesn't exist.");
                return;
            }

            Debug.Log($"{className}Loading level from {path}");

            if (setLevelEnd)
                OnLevelEnd = () =>
                {
                    Clear();
                    Updater.OnLevelEnd();
                    SceneHelper.LoadScene(SceneName.Main_Menu);
                };

            if (path.EndsWith(FileFormat.ASSET.Dot()))
            {
                CoreHelper.StartCoroutine(StoryLevel.LoadFromAsset(path, storyLevel => CoreHelper.StartCoroutine(Play(storyLevel))));
                return;
            }

            var level = new Level(path.Replace(Level.LEVEL_LSB, "").Replace(Level.LEVEL_VGD, ""));
            CoreHelper.StartCoroutine(Play(level));
        }

        /// <summary>
        /// Clears any left over data.
        /// </summary>
        public static void Clear()
        {
            DG.Tweening.DOTween.Clear();
            GameData.Current = null;
            GameData.Current = new GameData();
            InputDataManager.inst.SetAllControllerRumble(0f);
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
            LevelSort.Cover => levels.Order(x => x.icon != SteamWorkshop.inst.defaultSteamImageSprite, !ascend),
            LevelSort.Artist => levels.Order(x => x.metadata.artist.Name, !ascend),
            LevelSort.Creator => levels.Order(x => x.metadata.creator.steam_name, !ascend),
            LevelSort.File => levels.Order(x => System.IO.Path.GetFileName(x.path), !ascend),
            LevelSort.Title => levels.Order(x => x.metadata.song.title, !ascend),
            LevelSort.Difficulty => levels.Order(x => x.metadata.song.difficulty, !ascend),
            LevelSort.DateEdited => levels.Order(x => x.metadata.beatmap.date_edited, !ascend),
            LevelSort.DateCreated => levels.Order(x => x.metadata.beatmap.date_created, !ascend),
            LevelSort.DatePublished => levels.Order(x => x.metadata.beatmap.date_published, !ascend),
            LevelSort.Ranking => levels.Order(x =>
            {
                var playerData = GetPlayerData(x.id);
                return playerData != null ? playerData.Hits : int.MaxValue;
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
        public static string UpdateBeatmap(string json, string ver)
        {
            CoreHelper.Log($"[ -- Updating Beatmap! -- ] - [{ver}]");

            var version = new Version(ver);

            // 3.7.26
            if (version.Major <= 3 && version.Minor <= 7 && version.Patch <= 26)
            {
                Debug.Log("value_x -> x & value_y -> y");
                json = json.Replace("\"value_x\"", "\"x\"");
                json = json.Replace("\"value_y\"", "\"y\"");
            }

            // 3.7.42
            if (version.Major <= 3 && version.Minor <= 7 && version.Patch <= 42)
            {
                Debug.Log("text 4 -> 5");
                json = json.Replace("\"shape\": \"4\"", "\"shape\": \"5\"");
            }

            // 3.8.15
            if (version.Major <= 3 && version.Minor <= 8 && version.Patch <= 15)
                Debug.Log("Add parent relationship if none");

            // 3.8.25
            if (version.Major <= 3 && version.Minor <= 8 && version.Patch <= 25)
            {
                Debug.Log("background_objects -> bg_objects");
                json = json.Replace("\"background_objects\"", "\"bg_objects\"");
                Debug.Log("reactive_settings -> r_set");
                json = json.Replace("\"reactive_settings\"", "\"r_set\"");
            }

            // 3.8.48
            if (version.Major <= 3 && version.Minor <= 8 && version.Patch <= 48)
            {
                Debug.Log("is_random -> r");
                json = json.Replace("\"is_random\":\"False\"", "\"r\":\"0\"").Replace("\"is_random\":\"True\"", "\"r\":\"1\"");
                json = json.Replace("\"is_random\": \"False\"", "\"r\": \"0\"").Replace("\"is_random\": \"True\"", "\"r\": \"1\"");
                Debug.Log("origin -> o");
                json = json.Replace("\"origin\"", "\"o\"");
                Debug.Log("time -> t");
                json = json.Replace("\"time\"", "\"t\"");
                Debug.Log("start_time -> st");
                json = json.Replace("\"start_time\"", "\"st\"");
                Debug.Log("editor_data -> ed");
                json = json.Replace("\"editor_data\"", "\"ed\"");
                Debug.Log("value_random_x -> rx");
                json = json.Replace("\"value_random_x\"", "\"rx\"");
                Debug.Log("value_random_y -> ry");
                json = json.Replace("\"value_random_y\"", "\"ry\"");
                Debug.Log("value_z -> z");
                json = json.Replace("\"value_z\"", "\"z\"").Replace("\"value_z2\"", "\"z2\"");
                Debug.Log("curve_type -> ct");
                json = json.Replace("\"curve_type\"", "\"ct\"");
                Debug.Log("p_type -> pt");
                json = json.Replace("\"p_type\"", "\"pt\"");
                Debug.Log("parent -> p");
                json = json.Replace("\"parent\"", "\"p\"");
                Debug.Log("helper -> h");
                json = json.Replace("\"helper\"", "\"h\"");
                Debug.Log("depth -> d");
                json = json.Replace("\"depth\"", "\"d\"");
                Debug.Log("prefab_id -> pid");
                json = json.Replace("\"prefab_id\"", "\"pid\"");
                Debug.Log("prefab_inst_id -> piid");
                json = json.Replace("\"prefab_inst_id\"", "\"piid\"");
                Debug.Log("shape_option -> so");
                json = json.Replace("\"shape_option\"", "\"so\"");
            }

            // To fix alpha screwing the font up
            json = json
                .Replace("<font=LiberationSans>", "<font=LiberationSans SDF>").Replace("<font=\"LiberationSans\">", "<font=\"LiberationSans SDF\">")
                .Replace("<font=Inconsolata>", "<font=Inconsolata Variable>").Replace("<font=\"Inconsolata\">", "<font=\"Inconsolata Variable\">")
                .Replace("<font=liberationsans>", "<font=LiberationSans SDF>").Replace("<font=\"liberationsans\">", "<font=\"LiberationSans SDF\">")
                .Replace("<font=inconsolata>", "<font=Inconsolata Variable>").Replace("<font=\"inconsolata\">", "<font=\"Inconsolata Variable\">");

            return json;
        }

        #endregion

        #region Player Data

        public static void UpdateCurrentLevelProgress()
        {
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

            var levels = CurrentLevelCollection ? CurrentLevelCollection.levels : Levels;

            if (PlayerManager.IsZenMode || PlayerManager.IsPractice)
            {
                if (NextLevelInCollection && CurrentLevel.metadata && CurrentLevel.metadata.song.difficulty == 6)
                    SetLevelData(levels, NextLevelInCollection, NextLevelInCollection.playerData == null, false);
                return;
            }

            if (NextLevelInCollection)
                SetLevelData(levels, NextLevelInCollection, NextLevelInCollection.playerData == null, false);
            SetLevelData(levels, CurrentLevel, CurrentLevel.playerData == null, true);
        }

        public static void SetLevelData(List<Level> levels, Level currentLevel, bool makeNewPlayerData, bool update)
        {
            if (makeNewPlayerData)
                currentLevel.playerData = new PlayerData { ID = currentLevel.id, LevelName = currentLevel.metadata?.beatmap?.name, };

            if (update)
            {
                CoreHelper.Log($"Updating save data\n" +
                    $"New Player Data = {makeNewPlayerData}\n" +
                    $"Deaths [OLD = {currentLevel.playerData.Deaths} > NEW = {GameManager.inst.deaths.Count}]\n" +
                    $"Hits: [OLD = {currentLevel.playerData.Hits} > NEW = {GameManager.inst.hits.Count}]\n" +
                    $"Boosts: [OLD = {currentLevel.playerData.Boosts} > NEW = {BoostCount}]");

                currentLevel.playerData.Update(GameManager.inst.deaths.Count, GameManager.inst.hits.Count, BoostCount, true);
            }

            if (currentLevel.metadata && currentLevel.metadata.unlockAfterCompletion)
                currentLevel.playerData.Unlocked = true;

            if (Saves.TryFindIndex(x => x.ID == currentLevel.id, out int saveIndex1))
                Saves[saveIndex1] = currentLevel.playerData;
            else
                Saves.Add(currentLevel.playerData);

            if (levels.TryFind(x => x.id == currentLevel.id, out Level level1))
                level1.playerData = currentLevel.playerData;

            SaveProgress();

        }

        /// <summary>
        /// Updates the unmodded progress file to a separate one for better control.
        /// </summary>
        public static void UpgradeProgress()
        {
            if (!RTFile.FileExists(RTFile.ApplicationDirectory + "profile/saves.les") && RTFile.FileExists(RTFile.ApplicationDirectory + "settings/save.lss"))
            {
                var decryptedJSON = LSEncryption.DecryptText(RTFile.ReadFromFile(RTFile.ApplicationDirectory + "settings/saves.lss"), SaveManager.inst.encryptionKey);

                var jn = JSON.Parse(decryptedJSON);

                for (int i = 0; i < jn["arcade"].Count; i++)
                {
                    var js = jn["arcade"][i];

                    Saves.Add(new PlayerData
                    {
                        ID = js["level_data"]["id"],
                        Completed = js["play_data"]["finished"].AsBool,
                        Hits = js["play_data"]["hits"].AsInt,
                        Deaths = js["play_data"]["deaths"].AsInt,
                    });
                }

                SaveProgress();
            }
        }

        /// <summary>
        /// Saves the current player save data.
        /// </summary>
        public static void SaveProgress()
        {
            var jn = JSON.Parse("{}");
            for (int i = 0; i < Saves.Count; i++)
            {
                jn["lvl"][i] = Saves[i].ToJSON();
            }

            jn["played_count"] = PlayedLevelCount.ToString();

            if (!RTFile.DirectoryExists(RTFile.ApplicationDirectory + "profile"))
                Directory.CreateDirectory(RTFile.ApplicationDirectory + "profile");

            var json = jn.ToString();
            json = LSEncryption.EncryptText(json, SaveManager.inst.encryptionKey);
            RTFile.WriteToFile(RTFile.ApplicationDirectory + "profile/saves.les", json);
        }

        /// <summary>
        /// Loads the current player save data.
        /// </summary>
        public static void LoadProgress()
        {
            if (!RTFile.FileExists(RTFile.ApplicationDirectory + "profile/saves.les"))
                return;

            Saves.Clear();

            string decryptedJSON = LSEncryption.DecryptText(RTFile.ReadFromFile(RTFile.ApplicationDirectory + "profile/saves.les"), SaveManager.inst.encryptionKey);

            var jn = JSON.Parse(decryptedJSON);

            for (int i = 0; i < jn["lvl"].Count; i++)
            {
                Saves.Add(PlayerData.Parse(jn["lvl"][i]));
            }

            if (!string.IsNullOrEmpty(jn["played_count"]))
            {
                PlayedLevelCount = jn["played_count"].AsInt;
            }
        }

        public static PlayerData GetPlayerData(string id) => Saves.Find(x => x.ID == id);

        /// <summary>
        /// The regular level rank calculation method.
        /// </summary>
        /// <param name="hits">Hits player data list.</param>
        /// <returns>A calculated rank from hits.</returns>
        public static DataManager.LevelRank GetLevelRank(List<SaveManager.SaveGroup.Save.PlayerDataPoint> hits)
        {
            if (CoreHelper.InEditor)
                return EditorRank;

            if (!CoreHelper.InStory && (PlayerManager.IsZenMode || PlayerManager.IsPractice))
                return DataManager.inst.levelRanks[0];

            int dataPointMax = 24;
            int[] hitsNormalized = new int[dataPointMax + 1];
            foreach (var playerDataPoint in hits)
            {
                int num5 = (int)RTMath.SuperLerp(0f, AudioManager.inst.CurrentAudioSource.clip.length, 0f, (float)dataPointMax, playerDataPoint.time);
                hitsNormalized[num5]++;
            }

            return DataManager.inst.levelRanks.Find(x => hitsNormalized.Sum() >= x.minHits && hitsNormalized.Sum() <= x.maxHits);
        }

        /// <summary>
        /// Gets a level rank by hit count.
        /// </summary>
        /// <param name="hits">Hit count.</param>
        /// <returns>A calculated rank from the amount of hits.</returns>
        public static DataManager.LevelRank GetLevelRank(int hits)
            => CoreHelper.InEditor ? EditorRank : DataManager.inst.levelRanks.TryFind(x => hits >= x.minHits && hits <= x.maxHits, out DataManager.LevelRank levelRank) ? levelRank : DataManager.inst.levelRanks[0];

        /// <summary>
        /// Gets a levels' rank.
        /// </summary>
        /// <param name="level">Level to get a rank from.</param>
        /// <returns>A levels' stored rank.</returns>
        public static DataManager.LevelRank GetLevelRank(Level level)
            => CoreHelper.InEditor ? EditorRank : level.playerData != null && DataManager.inst.levelRanks.TryFind(LevelRankPredicate(level), out DataManager.LevelRank levelRank) ? levelRank : DataManager.inst.levelRanks[0];

        /// <summary>
        /// Gets a levels' rank.
        /// </summary>
        /// <param name="playerData">PlayerData to get a rank from.</param>
        /// <returns>A levels' stored rank.</returns>
        public static DataManager.LevelRank GetLevelRank(PlayerData playerData)
            => CoreHelper.InEditor ? EditorRank : playerData != null && DataManager.inst.levelRanks.TryFind(x => playerData.Hits >= x.minHits && playerData.Hits <= x.maxHits, out DataManager.LevelRank levelRank) ? levelRank : DataManager.inst.levelRanks[0];

        public static Dictionary<string, int> levelRankIndexes = new Dictionary<string, int>
        {
            { "-", 0 },
            { "SS", 1 },
            { "S", 2 },
            { "A", 3 },
            { "B", 4 },
            { "C", 5 },
            { "D", 6 },
            { "F", 7 },
        };

        public static DataManager.LevelRank EditorRank => DataManager.inst.levelRanks[(int)EditorConfig.Instance.EditorRank.Value];

        public static float CalculateAccuracy(int hits, float length)
            => 100f / ((hits / (length / PlayerManager.AcurracyDivisionAmount)) + 1f);

        public static Predicate<DataManager.LevelRank> LevelRankPredicate(Level level)
             => x => level.playerData != null && level.playerData.Hits >= x.minHits && level.playerData.Hits <= x.maxHits;

        public static List<PlayerData> Saves { get; set; } = new List<PlayerData>();
        public class PlayerData
        {
            public PlayerData() { }

            public string LevelName { get; set; }
            public string ID { get; set; }
            public bool Completed { get; set; }
            public int Hits { get; set; } = -1;
            public int Deaths { get; set; } = -1;
            public int Boosts { get; set; } = -1;
            public int PlayedTimes { get; set; }
            public float TimeInLevel { get; set; }
            public float Percentage { get; set; }
            public float LevelLength { get; set; }
            public bool Unlocked { get; set; }

            public void Update()
            {
                if (Hits > GameManager.inst.hits.Count)
                    Hits = GameManager.inst.hits.Count;

                if (Deaths > GameManager.inst.deaths.Count)
                    Deaths = GameManager.inst.deaths.Count;

                var l = AudioManager.inst.CurrentAudioSource.clip.length;
                if (LevelLength != l)
                    LevelLength = l;

                float calc = AudioManager.inst.CurrentAudioSource.time / AudioManager.inst.CurrentAudioSource.clip.length * 100f;

                if (Percentage < calc)
                    Percentage = calc;
            }

            public void Update(int deaths, int hits, int boosts, bool completed)
            {
                if (Deaths == -1 || Deaths > deaths)
                    Deaths = deaths;
                if (Hits == -1 || Hits > hits)
                    CurrentLevel.playerData.Hits = hits;
                if (Boosts == -1 || Boosts > boosts)
                    CurrentLevel.playerData.Boosts = boosts;
                Completed = completed;
            }

            public static PlayerData Parse(JSONNode jn) => new PlayerData
            {
                LevelName = jn["n"],
                ID = jn["id"],
                Completed = jn["c"].AsBool,
                Hits = jn["h"].AsInt,
                Deaths = jn["d"].AsInt,
                Boosts = jn["b"].AsInt,
                PlayedTimes = jn["pt"].AsInt,
                TimeInLevel = jn["t"].AsFloat,
                Percentage = jn["p"].AsFloat,
                LevelLength = jn["l"].AsFloat,
                Unlocked = jn["u"].AsBool,
            };

            public JSONNode ToJSON()
            {
                var jn = JSON.Parse("{}");
                if (!string.IsNullOrEmpty(LevelName))
                    jn["n"] = LevelName;
                jn["id"] = ID;
                jn["c"] = Completed;
                jn["h"] = Hits;
                jn["d"] = Deaths;
                jn["b"] = Boosts;
                jn["pt"] = PlayedTimes;
                jn["t"] = TimeInLevel;
                jn["p"] = Percentage;
                jn["l"] = LevelLength;
                jn["u"] = Unlocked;
                return jn;
            }

            public override string ToString() => $"{ID} - Hits: {Hits} Deaths: {Deaths}";
        }

        #endregion
    }
}
