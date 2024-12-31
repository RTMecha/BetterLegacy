using BetterLegacy.Components.Player;
using BetterLegacy.Configs;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Data.Level;
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

using Version = BetterLegacy.Core.Data.Version;

namespace BetterLegacy.Core.Managers
{
    public class LevelManager : MonoBehaviour
    {
        #region Init

        /// <summary>
        /// The <see cref="LevelManager"/> global instance reference.
        /// </summary>
        public static LevelManager inst;

        /// <summary>
        /// Manager class name.
        /// </summary>
        public static string className = "[<color=#7F00FF>LevelManager</color>] \n";

        /// <summary>
        /// Initializes <see cref="LevelManager"/>.
        /// </summary>
        public static void Init() => Creator.NewGameObject(nameof(LevelManager), SystemManager.inst.transform).AddComponent<LevelManager>();

        void Awake()
        {
            inst = this;
            Levels = new List<Level>();
            ArcadeQueue = new List<Level>();
            LevelCollections = new List<LevelCollection>();

            LoadProgress();
        }

        void Update()
        {
            if (CoreHelper.InEditor && EditorManager.inst.isEditing)
                BoostCount = 0;
        }

        #endregion

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

        #region Loading & Sorting

        /// <summary>
        /// Loads the game scene and plays a level.
        /// </summary>
        /// <param name="level">The level to play.</param>
        public static void Play(Level level)
        {
            if (level)
                CoreHelper.StartCoroutine(IPlay(level));
        }

        /// <summary>
        /// Loads the game scene and plays a level.
        /// </summary>
        /// <param name="level">The level to play.</param>
        public static IEnumerator IPlay(Level level)
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
                if (ProjectArrhythmia.RequireUpdate(level.metadata.beatmap.game_version))
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

            PlayerManager.LoadLocalModels();
            PlayerManager.ValidatePlayers();
            PlayerManager.AssignPlayerModels();

            PlayerManager.allowController = PlayerConfig.Instance.AllowControllerIfSinglePlayer.Value && PlayerManager.IsSingleplayer;

            RTPlayer.GameMode = GameMode.Regular;

            GameStorageManager.inst.PlayIntro();
            PlayerManager.SpawnPlayersOnStart();

            RTPlayer.SetGameDataProperties();

            EventManager.inst?.updateEvents();
            RTEventManager.inst?.SetResetOffsets();

            BackgroundManager.inst.UpdateBackgrounds();
            yield return inst.StartCoroutine(Updater.IUpdateObjects(true));

            CursorManager.inst.HideCursor();

            Debug.Log($"{className}Done!");

            GameManager.inst.gameState = GameManager.State.Playing;
            AudioManager.inst.SetMusicTime(0f);

            LoadingFromHere = false;

            ResetTransition();
            CurrentMusicVolume = CoreConfig.Instance.MusicVol.Value;
            AchievementManager.inst.CheckLevelBeginAchievements();
        }

        /// <summary>
        /// Loads a level from anywhere. For example: LevelManager.Load("E:/4.1.16/beatmaps/story/Apocrypha/level.lsb");
        /// </summary>
        /// <param name="path">Path to the level. Can either be an asset file or a normal level format.</param>
        public static void Load(string path)
        {
            if (!RTFile.FileExists(path))
            {
                Debug.LogError($"{className}Couldn't load level from {path} as it doesn't exist.");
                return;
            }

            Debug.Log($"{className}Loading level from {path}");

            OnLevelEnd = ArcadeHelper.EndOfLevel;

            if (path.EndsWith(FileFormat.ASSET.Dot()))
            {
                CoreHelper.StartCoroutine(StoryLevel.LoadFromAsset(path, Play));
                return;
            }

            Play(new Level(path.Replace(Level.LEVEL_LSB, "").Replace(Level.LEVEL_VGD, "")));
        }

        /// <summary>
        /// Resets the level transition values to the defaults.
        /// </summary>
        public static void ResetTransition()
        {
            //CoreHelper.Log($"Song Fade Transition: {songFadeTransition}\nDo Intro Fade: {GameStorageManager.doIntroFade}");
            songFadeTransition = 0.5f;
            GameStorageManager.doIntroFade = true;
        }

        /// <summary>
        /// Clears any left over data.
        /// </summary>
        public static void Clear()
        {
            DG.Tweening.DOTween.Clear();
            try
            {
                Updater.UpdateObjects(false);
            }
            catch (Exception ex)
            {
                CoreHelper.LogException(ex);
            } // try cleanup
            GameData.Current = null;
            GameData.Current = new GameData();
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
                    CurrentLevelCollection.previewAudio = null;
                }
                CurrentLevelCollection = null;

                if (CurrentLevel && CurrentLevel.music)
                {
                    CurrentLevel.music.UnloadAudioData();
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

                    if (collection.previewAudio)
                    {
                        collection.previewAudio.UnloadAudioData();
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
                    level.playerData = null;
                    if (level.achievements != null)
                        level.achievements.Clear();

                    if (level.music)
                    {
                        level.music.UnloadAudioData();
                        level.music = null;
                    }

                    Levels[i] = null;
                }
                Levels.Clear();
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

        #region Player Data

        /// <summary>
        /// Finds and sets the levels' player data.
        /// </summary>
        /// <param name="level">Level to assign to.</param>
        public static void AssignPlayerData(Level level)
        {
            if (!level || !Saves.TryFind(x => x.ID == level.id, out PlayerData playerData))
                return;

            level.playerData = playerData;
            playerData.LevelName = level.metadata?.beatmap?.name;
        }

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
                if (NextLevelInCollection && CurrentLevel.metadata && CurrentLevel.metadata.song.LevelDifficulty == LevelDifficulty.Animation)
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
                currentLevel.playerData = new PlayerData(currentLevel);
            if (currentLevel && currentLevel.playerData)
                currentLevel.playerData.LevelName = currentLevel.metadata?.beatmap?.name; // update level name

            if (update)
            {
                CoreHelper.Log($"Updating save data\n" +
                    $"New Player Data = {makeNewPlayerData}\n" +
                    $"Deaths [OLD = {currentLevel.playerData.Deaths} > NEW = {GameManager.inst.deaths.Count}]\n" +
                    $"Hits: [OLD = {currentLevel.playerData.Hits} > NEW = {GameManager.inst.hits.Count}]\n" +
                    $"Boosts: [OLD = {currentLevel.playerData.Boosts} > NEW = {BoostCount}]");

                currentLevel.playerData.Update(GameManager.inst.deaths.Count, GameManager.inst.hits.Count, BoostCount, true);
            }

            if (currentLevel.metadata && currentLevel.metadata.unlockAfterCompletion && (currentLevel.metadata.song.LevelDifficulty == LevelDifficulty.Animation || !PlayerManager.IsZenMode && !PlayerManager.IsPractice))
                currentLevel.playerData.Unlocked = true;

            if (Saves.TryFindIndex(x => x.ID == currentLevel.id, out int saveIndex))
                Saves[saveIndex] = currentLevel.playerData;
            else
                Saves.Add(currentLevel.playerData);

            if (levels.TryFind(x => x.id == currentLevel.id, out Level level))
                level.playerData = currentLevel.playerData;

            SaveProgress();

        }

        /// <summary>
        /// Saves the current player save data.
        /// </summary>
        public static void SaveProgress()
        {
            var jn = JSON.Parse("{}");
            int num = 0;
            for (int i = 0; i < Saves.Count; i++)
            {
                var save = Saves[i];
                if (string.IsNullOrEmpty(save.ID) || save.ID == "-1" || save.ID == "0")
                    continue;

                jn["lvl"][num] = save.ToJSON();
                num++;
            }
            jn["played_count"] = PlayedLevelCount.ToString();

            var profilePath = RTFile.ApplicationDirectory + "profile";
            RTFile.CreateDirectory(profilePath);

            var json = jn.ToString();
            RTFile.WriteToFile(RTFile.CombinePaths(profilePath, $"arcade_saves{FileFormat.LSS.Dot()}"), json);
        }

        /// <summary>
        /// Loads the current player save data.
        /// </summary>
        public static void LoadProgress()
        {
            Saves.Clear();

            var profilePath = RTFile.ApplicationDirectory + "profile";
            JSONNode jn;
            if (!RTFile.FileExists(RTFile.CombinePaths(profilePath, $"arcade_saves{FileFormat.LSS.Dot()}")))
            {
                var modded = RTFile.FileExists(RTFile.CombinePaths(profilePath, "saves.les"));
                var path = modded ? RTFile.CombinePaths(profilePath, "saves.les") : RTFile.ApplicationDirectory + $"settings/saves{FileFormat.LSS.Dot()}";
                jn = JSON.Parse(LSEncryption.DecryptText(RTFile.ReadFromFile(path), SaveManager.inst.encryptionKey));

                // parse old saves and then create a new save.
                if (!modded)
                {
                    for (int i = 0; i < jn["arcade"].Count; i++)
                        Saves.Add(PlayerData.ParseVanilla(jn["arcade"][i]));

                    SaveProgress();
                    return;
                }
            }
            else
                jn = JSON.Parse(RTFile.ReadFromFile(RTFile.CombinePaths(profilePath, $"arcade_saves{FileFormat.LSS.Dot()}")));

            for (int i = 0; i < jn["lvl"].Count; i++)
                Saves.Add(PlayerData.Parse(jn["lvl"][i]));

            if (!string.IsNullOrEmpty(jn["played_count"]))
                PlayedLevelCount = jn["played_count"].AsInt;
        }

        /// <summary>
        /// Removes player data.
        /// </summary>
        /// <param name="id">ID of the player data to remove.</param>
        public static void RemovePlayerData(string id)
        {
            Saves.RemoveAll(x => x.ID == id);
            SaveProgress();
        }

        /// <summary>
        /// Gets a levels' player data.
        /// </summary>
        /// <param name="id">ID of the player data to get.</param>
        /// <returns>Returns the player data of the level.</returns>
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

        #endregion
    }
}
