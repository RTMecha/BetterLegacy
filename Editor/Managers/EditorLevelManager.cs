﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using UnityEngine;
using UnityEngine.EventSystems;

using LSFunctions;

using CielaSpike;
using SimpleJSON;

using BetterLegacy.Companion.Entity;
using BetterLegacy.Configs;
using BetterLegacy.Core;
using BetterLegacy.Core.Components;
using BetterLegacy.Core.Components.Player;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Data.Level;
using BetterLegacy.Core.Data.Player;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using BetterLegacy.Core.Prefabs;
using BetterLegacy.Core.Runtime;
using BetterLegacy.Editor.Components;
using BetterLegacy.Editor.Data;
using BetterLegacy.Editor.Data.Dialogs;

namespace BetterLegacy.Editor.Managers
{
    /// <summary>
    /// Manages levels loaded in the editor and general functions to do with a level in the editor.
    /// </summary>
    public class EditorLevelManager : MonoBehaviour
    {
        #region Init

        /// <summary>
        /// The <see cref="EditorLevelManager"/> global instance reference.
        /// </summary>
        public static EditorLevelManager inst;

        /// <summary>
        /// Initializes <see cref="EditorLevelManager"/>.
        /// </summary>
        public static void Init() => EditorManager.inst.gameObject.AddComponent<EditorLevelManager>();

        void Awake() => inst = this;

        void Update() => autosaveTimer.Update();

        #endregion

        #region Values

        /// <summary>
        /// The default level name.
        /// </summary>
        public const string DEFAULT_LEVEL_NAME = "New Awesome Beatmap";
        /// <summary>
        /// The default artist.
        /// </summary>
        public const string DEFAULT_ARTIST = "Kaixo";
        /// <summary>
        /// The default song title.
        /// </summary>
        public const string DEFAULT_SONG_TITLE = "Intertia";
        /// <summary>
        /// The default level difficulty.
        /// </summary>
        public const int DEFAULT_DIFFICULTY = 2;

        /// <summary>
        /// The currently loaded level.
        /// </summary>
        public Level CurrentLevel { get; set; }

        /// <summary>
        /// Loaded editor levels.
        /// </summary>
        public List<LevelPanel> LevelPanels { get; set; } = new List<LevelPanel>();

        /// <summary>
        /// Selected editor levels.
        /// </summary>
        public List<LevelPanel> SelectedLevels => LevelPanels.FindAll(x => x.Selected);

        /// <summary>
        /// If the current level is a newly created one.
        /// </summary>
        public bool fromNewLevel;

        /// <summary>
        /// Settings used for creating new levels.
        /// </summary>
        public NewLevelSettings newLevelSettings = new NewLevelSettings(
            audioPath: string.Empty,
            levelName: DEFAULT_LEVEL_NAME,
            songArtist: DEFAULT_ARTIST,
            songTitle: DEFAULT_SONG_TITLE,
            difficulty: DEFAULT_DIFFICULTY
            );

        /// <summary>
        /// If the level should cut.
        /// </summary>
        public bool shouldCutLevel;

        /// <summary>
        /// Path to the copied level.
        /// </summary>
        public string copiedLevelPath;

        /// <summary>
        /// If the editor is currently loading a level.
        /// </summary>
        public bool loadingLevel;

        /// <summary>
        /// If the editor is currently autosaving.
        /// </summary>
        public bool autosaving;

        /// <summary>
        /// Amount of time that has passed since the last autosave.
        /// </summary>
        public RTTimer autosaveTimer;

        #endregion

        #region Methods

        /// <summary>
        /// Pastes the copied / cut level.
        /// </summary>
        public void PasteLevel()
        {
            if (string.IsNullOrEmpty(copiedLevelPath))
            {
                EditorManager.inst.DisplayNotification("No level has been copied yet!", 2f, EditorManager.NotificationType.Error);
                return;
            }

            if (!RTFile.DirectoryExists(copiedLevelPath))
            {
                EditorManager.inst.DisplayNotification("Copied level no longer exists.", 2f, EditorManager.NotificationType.Error);
                return;
            }

            var folderName = Path.GetFileName(copiedLevelPath);
            var directory = RTFile.GetDirectory(copiedLevelPath);
            var editorPath = RTFile.CombinePaths(RTEditor.inst.BeatmapsPath, RTEditor.inst.EditorPath);

            if (shouldCutLevel)
            {
                if (RTFile.DirectoryExists(copiedLevelPath.Replace(directory, editorPath)))
                {
                    EditorManager.inst.DisplayNotification($"Level with the name \"{folderName}\" already exists in this location.", 2f, EditorManager.NotificationType.Warning);
                    return;
                }

                Directory.Move(copiedLevelPath, copiedLevelPath.Replace(directory, editorPath));
                LoadLevels();
                EditorManager.inst.DisplayNotification($"Succesfully moved {folderName}!", 2f, EditorManager.NotificationType.Success);

                return;
            }

            var result = copiedLevelPath;
            int num = 0;
            while (Directory.Exists(result.Replace(directory, editorPath)))
            {
                result = $"{copiedLevelPath} [{num}]";
                num++;
            }

            var files = Directory.GetFiles(copiedLevelPath, "*", SearchOption.AllDirectories);
            for (int i = 0; i < files.Length; i++)
            {
                var file = files[i].Replace("\\", "/").Replace(copiedLevelPath, result).Replace(directory, editorPath);
                var copyToDirectory = Path.GetDirectoryName(file);
                RTFile.CreateDirectory(copyToDirectory);

                RTFile.CopyFile(files[i], file);
            }

            LoadLevels();
            EditorManager.inst.DisplayNotification($"Succesfully pasted {folderName}!", 2f, EditorManager.NotificationType.Success);
        }

        /// <summary>
        /// Loads editor levels from the current editor folder.
        /// </summary>
        public void LoadLevels() => CoroutineHelper.StartCoroutine(ILoadLevels());

        /// <summary>
        /// Loads editor levels from the current editor folder.
        /// </summary>
        public IEnumerator ILoadLevels()
        {
            LevelPanels.Clear();

            RTEditor.inst.OpenLevelPopup.ClearContent();

            var list = new List<Coroutine>();
            var files = Directory.GetDirectories(RTFile.CombinePaths(RTEditor.inst.BeatmapsPath, RTEditor.inst.EditorPath));

            // Back
            if (EditorConfig.Instance.ShowFoldersInLevelList.Value && RTFile.GetDirectory(RTFile.CombinePaths(RTEditor.inst.BeatmapsPath, RTEditor.inst.EditorPath)) != RTEditor.inst.BeatmapsPath)
            {
                var gameObjectFolder = EditorManager.inst.folderButtonPrefab.Duplicate(RTEditor.inst.OpenLevelPopup.Content, "back");
                var folderButtonStorageFolder = gameObjectFolder.GetComponent<FunctionButtonStorage>();
                var folderButtonFunctionFolder = gameObjectFolder.AddComponent<FolderButtonFunction>();

                var hoverUIFolder = gameObjectFolder.AddComponent<HoverUI>();
                hoverUIFolder.size = EditorConfig.Instance.OpenLevelButtonHoverSize.Value;
                hoverUIFolder.animatePos = false;
                hoverUIFolder.animateSca = true;

                folderButtonStorageFolder.label.text = "< Up a folder";

                folderButtonStorageFolder.label.horizontalOverflow = EditorConfig.Instance.OpenLevelTextHorizontalWrap.Value;
                folderButtonStorageFolder.label.verticalOverflow = EditorConfig.Instance.OpenLevelTextVerticalWrap.Value;
                folderButtonStorageFolder.label.fontSize = EditorConfig.Instance.OpenLevelTextFontSize.Value;

                folderButtonStorageFolder.button.onClick.ClearAll();
                folderButtonFunctionFolder.onClick = eventData =>
                {
                    if (eventData.button == PointerEventData.InputButton.Right)
                    {
                        EditorContextMenu.inst.ShowContextMenu(
                            new ButtonFunction("Create folder", () => RTEditor.inst.ShowFolderCreator(RTFile.CombinePaths(RTEditor.inst.BeatmapsPath, RTEditor.inst.EditorPath), () => { LoadLevels(); RTEditor.inst.HideNameEditor(); })),
                            new ButtonFunction("Create level", EditorManager.inst.OpenNewLevelPopup),
                            new ButtonFunction("Paste", PasteLevel),
                            new ButtonFunction("Open List in File Explorer", RTEditor.inst.OpenLevelListFolder));

                        return;
                    }

                    if (RTEditor.inst.editorPathField.text == RTEditor.inst.EditorPath)
                    {
                        RTEditor.inst.editorPathField.text = RTFile.GetDirectory(RTFile.CombinePaths(RTEditor.inst.BeatmapsPath, RTEditor.inst.EditorPath)).Replace(RTEditor.inst.BeatmapsPath + "/", "");
                        RTEditor.inst.UpdateEditorPath(false);
                    }
                };

                EditorThemeManager.ApplySelectable(folderButtonStorageFolder.button, ThemeGroup.List_Button_1);
                EditorThemeManager.ApplyLightText(folderButtonStorageFolder.label);
            }

            foreach (var file in files)
            {
                var path = RTFile.ReplaceSlash(file);

                var levelPanel = new LevelPanel();

                if (!Level.TryVerify(path, false, out Level level))
                {
                    if (!EditorConfig.Instance.ShowFoldersInLevelList.Value)
                        continue;

                    levelPanel.Init(path);
                    LevelPanels.Add(levelPanel);

                    list.Add(levelPanel.LoadImageCoroutine($"folder_icon{FileFormat.PNG.Dot()}"));

                    continue;
                }

                levelPanel.Init(level);

                if (RTFile.FileExists(RTFile.CombinePaths(path, Level.LEVEL_JPG)))
                    list.Add(levelPanel.LoadImageCoroutine(Level.LEVEL_JPG, LevelPanels.Add));
                else if (RTFile.FileExists(RTFile.CombinePaths(path, Level.COVER_JPG)))
                    list.Add(levelPanel.LoadImageCoroutine(Level.COVER_JPG, LevelPanels.Add));
                else
                {
                    levelPanel.SetDefaultIcon();
                    LevelPanels.Add(levelPanel);
                }
            }

            if (list.Count >= 1)
                yield return StartCoroutine(LSHelpers.WaitForMultipleCoroutines(list, OpenLevelPopupOnFinish));
            else
                OpenLevelPopupOnFinish();

            CoreHelper.Log($"Finished loading editor levels.");

            yield break;
        }

        /// <summary>
        /// Loads a level in the editor.
        /// </summary>
        /// <param name="levelPanel">Level to load and edit.</param>
        public void LoadLevel(LevelPanel levelPanel) => CoroutineHelper.StartCoroutine(ILoadLevel(levelPanel.Level));

        /// <summary>
        /// Loads a level in the editor.
        /// </summary>
        /// <param name="level">Level to load and edit.</param>
        public void LoadLevel(Level level) => CoroutineHelper.StartCoroutine(ILoadLevel(level));

        /// <summary>
        /// Loads a level in the editor.
        /// </summary>
        /// <param name="level">Level to load and edit.</param>
        public IEnumerator ILoadLevel(Level level)
        {
            loadingLevel = true;

            CurrentLevel = level;
            var fullPath = RTFile.RemoveEndSlash(level.path);
            var currentFile = level.CurrentFile;
            level.currentFile = null; // reset since autosave loading should be temporary.

            var sw = CoreHelper.StartNewStopwatch();

            RTPlayer.GameMode = GameMode.Regular;

            string code = RTFile.CombinePaths(fullPath, $"EditorLoad{FileFormat.CS.Dot()}");
            if (RTFile.FileExists(code))
            {
                var str = RTFile.ReadFromFile(code);
                if (RTCode.Validate(str))
                    yield return StartCoroutine(RTCode.IEvaluate(str));
            }

            EditorTimeline.inst.SetLayer(0, EditorTimeline.LayerType.Objects);

            WindowController.ResetTitle();

            RandomHelper.UpdateSeed();

            CoreHelper.Log("Clearing data...");

            for (int i = 0; i < EditorTimeline.inst.timelineObjects.Count; i++)
            {
                var timelineObject = EditorTimeline.inst.timelineObjects[i];
                if (timelineObject.GameObject)
                    Destroy(timelineObject.GameObject);

                if (timelineObject.InternalTimelineObjects != null)
                    for (int j = 0; j < timelineObject.InternalTimelineObjects.Count; j++)
                    {
                        var kf = timelineObject.InternalTimelineObjects[j];
                        if (kf.GameObject)
                            Destroy(kf.GameObject);
                    }
                EditorTimeline.inst.timelineObjects[i] = null;
            }
            EditorTimeline.inst.timelineObjects.Clear();

            for (int i = 0; i < EditorTimeline.inst.timelineKeyframes.Count; i++)
            {
                var timelineObject = EditorTimeline.inst.timelineKeyframes[i];
                if (timelineObject.GameObject)
                    Destroy(timelineObject.GameObject);
                EditorTimeline.inst.timelineKeyframes[i] = null;
            }
            EditorTimeline.inst.timelineKeyframes.Clear();

            for (int i = 0; i < ObjEditor.inst.TimelineParents.Count; i++)
                LSHelpers.DeleteChildren(ObjEditor.inst.TimelineParents[i]);

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

            // We stop and play the doggo bop animation in case the user has looked at the settings dialog.
            EditorManager.inst.CancelInvoke("LoadingIconUpdate");
            EditorManager.inst.InvokeRepeating("LoadingIconUpdate", 0f, 0.05f);

            CoreHelper.Log($"Done. Time taken: {sw.Elapsed}");

            var name = Path.GetFileName(fullPath);

            EditorManager.inst.currentLoadedLevel = name;
            EditorManager.inst.SetPitch(1f);

            EditorManager.inst.timelineScrollRectBar.value = 0f;
            GameManager.inst.gameState = GameManager.State.Loading;

            EditorManager.inst.ClearPopups();
            EditorDialog.CurrentDialog?.Close();
            RTEditor.inst.InfoPopup.Open();

            if (EditorManager.inst.hasLoadedLevel && EditorConfig.Instance.BackupPreviousLoadedLevel.Value && RTFile.FileExists(GameManager.inst.path))
            {
                CoreHelper.Log("Backing up previous level...");

                RTEditor.inst.InfoPopup.SetInfo($"Backing up previous level [ {Path.GetFileName(RTFile.RemoveEndSlash(GameManager.inst.path))} ]");

                GameData.Current.SaveData(RTFile.CombinePaths(RTFile.BasePath, $"level-open-backup{FileFormat.LSB.Dot()}"));

                CoreHelper.Log($"Done. Time taken: {sw.Elapsed}");
            }

            CoreHelper.Log("Loading data...");

            RTEditor.inst.InfoPopup.SetInfo($"Loading Level Data for [ {name} ]");

            CoreHelper.Log($"Loading {currentFile}...");

            GameManager.inst.path = RTFile.CombinePaths(fullPath, Level.LEVEL_LSB);
            RTFile.BasePath = RTFile.AppendEndSlash(fullPath);
            GameManager.inst.levelName = name;
            RTEditor.inst.InfoPopup.SetInfo($"Loading Level Music for [ {name} ]\n\nIf this is taking more than a minute or two check if the song file (.ogg / .wav / .mp3) is corrupt. If not, then something went really wrong.");

            yield return CoroutineHelper.StartCoroutine(level.LoadAudioClipRoutine());

            CoreHelper.Log($"Done. Time taken: {sw.Elapsed}");

            CoreHelper.Log("Setting up Video...");
            yield return StartCoroutine(RTVideoManager.inst.Setup(fullPath));
            CoreHelper.Log($"Done. Time taken: {sw.Elapsed}");

            GameManager.inst.gameState = GameManager.State.Parsing;
            CoreHelper.Log("Parsing data...");
            RTEditor.inst.InfoPopup.SetInfo($"Parsing Level Data for [ {name} ]");
            string rawJSON = RTFile.ReadFromFile(RTFile.CombinePaths(fullPath, currentFile));
            if (string.IsNullOrEmpty(rawJSON))
            {
                RTEditor.inst.InfoPopup.SetInfo($"{currentFile} is empty or corrupt.");
                EditorManager.inst.DisplayNotification("Level could not load.", 3f, EditorManager.NotificationType.Error);

                yield break;
            }

            try
            {
                MetaData.Current = null;
                MetaData.Current = level.metadata;

                if (MetaData.Current.arcadeID == null || MetaData.Current.arcadeID == "0" || MetaData.Current.arcadeID == "-1")
                    MetaData.Current.arcadeID = LSText.randomNumString(16);

                if (ProjectArrhythmia.RequireUpdate(MetaData.Current.beatmap.gameVersion))
                    rawJSON = LevelManager.UpdateBeatmap(rawJSON, MetaData.Current.beatmap.gameVersion);

                GameData.Current = null;
                GameData.Current = level.IsVG ?
                    GameData.ParseVG(JSON.Parse(rawJSON), MetaData.Current.Version) :
                    GameData.Parse(JSON.Parse(rawJSON));
            }
            catch (Exception ex)
            {
                RTEditor.inst.InfoPopup.SetInfo($"Something went wrong when parsing the level data. Press the open log folder key ({CoreConfig.Instance.OpenPAPersistentFolder.Value}) and send the Player.log file to Mecha.");

                EditorManager.inst.DisplayNotification("Level could not load.", 3f, EditorManager.NotificationType.Error);

                CoreHelper.LogError($"Level loading caught an error: {ex}");

                yield break;
            }

            // preload audio clips
            if (GameData.Current && GameData.Current.assets)
                for (int i = 0; i < GameData.Current.assets.sounds.Count; i++)
                {
                    var soundAsset = GameData.Current.assets.sounds[i];
                    if (!soundAsset.audio)
                        yield return CoroutineHelper.StartCoroutine(soundAsset.LoadAudioClip());
                }

            CoreHelper.Log($"Done. Time taken: {sw.Elapsed}");

            if (RTEditor.inst.PreviewCover != null && RTEditor.inst.PreviewCover.gameObject)
                RTEditor.inst.PreviewCover.gameObject.SetActive(false);

            CoreHelper.Log("Playing level music...");
            RTEditor.inst.InfoPopup.SetInfo($"Playing Music for [ {name} ]\n\nIf it doesn't, then something went wrong!");
            SetCurrentAudio(level.music);
            CoreHelper.Log($"Done. Time taken: {sw.Elapsed}");

            if (EditorConfig.Instance.WaveformGenerate.Value)
            {
                CoreHelper.Log("Assigning waveform textures...");
                RTEditor.inst.InfoPopup.SetInfo($"Assigning Waveform Textures for [ {name} ]");
                StartCoroutine(EditorTimeline.inst.AssignTimelineTexture(level.music));
                CoreHelper.Log($"Done. Time taken: {sw.Elapsed}");
            }
            else
            {
                CoreHelper.Log("Skipping waveform textures...");
                RTEditor.inst.InfoPopup.SetInfo($"Skipping Waveform Textures for [ {name} ]");
                EditorTimeline.inst.SetTimelineSprite(null);
                CoreHelper.Log($"Done. Time taken: {sw.Elapsed}");
            }

            CoreHelper.Log("Updating timeline...");
            RTEditor.inst.InfoPopup.SetInfo($"Updating Timeline for [ {name} ]");
            EditorTimeline.inst.UpdateTimelineSizes();
            GameManager.inst.UpdateTimeline();
            RTMetaDataEditor.inst.RenderDialog();
            CoreHelper.Log($"Done. Time taken: {sw.Elapsed}");

            CheckpointEditor.inst.CreateGhostCheckpoints();

            RTEditor.inst.InfoPopup.SetInfo($"Updating states for [ {name} ]");
            if (CoreConfig.Instance.DiscordTimestampUpdatesPerLevel.Value)
                DiscordController.inst.presence.startTimestamp = SteamworksFacepunch.Epoch.Current;
            CoreHelper.UpdateDiscordStatus($"Editing: {MetaData.Current.song.title}", "In Editor", "editor");

            CoreHelper.Log("Spawning players...");
            try
            {
                PlayersData.Load(level.GetFile(Level.PLAYERS_LSB));
                PlayerManager.SpawnPlayersOnStart();
            }
            catch (Exception ex)
            {
                CoreHelper.LogException(ex);
            }

            RTPlayer.SetGameDataProperties();
            CoreHelper.Log($"Done. Time taken: {sw.Elapsed}");

            CoreHelper.Log("Updating objects...");
            StartCoroutine(RTLevel.IReinit());
            CoreHelper.Log($"Done. Time taken: {sw.Elapsed}");

            CoreHelper.Log("Updating timeline objects...");
            EventEditor.inst.CreateEventObjects();

            RTMarkerEditor.inst.CreateMarkers();
            RTMarkerEditor.inst.markerLooping = false;
            RTMarkerEditor.inst.markerLoopBegin = null;
            RTMarkerEditor.inst.markerLoopEnd = null;

            GameData.Current.UpdateUsedThemes();

            CoreHelper.Log($"Done. Time taken: {sw.Elapsed}");

            CoreHelper.Log("Creating timeline objects...");
            RTEditor.inst.InfoPopup.SetInfo($"Setting first object of [ {name} ]");
            EditorTimeline.inst.ClearTimelineObjects();
            EditorTimeline.inst.timelineObjects = EditorTimeline.inst.ToTimelineObjects().ToList();
            CoreHelper.Log($"Done. Time taken: {sw.Elapsed}");

            CheckpointEditor.inst.SetCurrentCheckpoint(0);

            RTEditor.inst.InfoPopup.SetInfo("Done!");
            RTEditor.inst.InfoPopup.Close();
            EditorManager.inst.CancelInvoke("LoadingIconUpdate");

            RTBeatmap.Current.ResetCheckpoint();
            GameManager.inst.gameState = GameManager.State.Playing;

            EditorManager.inst.DisplayNotification($"{name} Level loaded", 2f, EditorManager.NotificationType.Success);
            EditorManager.inst.UpdatePlayButton();
            EditorManager.inst.hasLoadedLevel = true;

            SetAutosave();

            TriggerHelper.AddEventTriggers(RTEditor.inst.timeField.gameObject, TriggerHelper.ScrollDelta(RTEditor.inst.timeField, max: AudioManager.inst.CurrentAudioSource.clip.length));

            // Load Settings like timeline position, editor layer, bpm active, etc
            RTEditor.inst.LoadSettings();
            EditorTimeline.inst.RenderTimeline();
            EditorTimeline.inst.RenderBins();

            if (EditorConfig.Instance.LevelPausesOnStart.Value)
            {
                AudioManager.inst.CurrentAudioSource.Pause();
                EditorManager.inst.UpdatePlayButton();
            }

            Example.Current?.brain?.Notice(ExampleBrain.Notices.LOADED_LEVEL);

            loadingLevel = false;
            fromNewLevel = false;

            rawJSON = null;

            CoreHelper.StopAndLogStopwatch(sw, $"Finished loading {name}");
            sw = null;

            yield break;
        }

        /// <summary>
        /// Saves the current level.
        /// </summary>
        public void SaveLevel()
        {
            if (!EditorManager.inst.hasLoadedLevel)
            {
                EditorManager.inst.DisplayNotification("Beatmap can't be saved until you load a level.", 5f, EditorManager.NotificationType.Error);
                return;
            }

            if (EditorManager.inst.savingBeatmap)
            {
                EditorManager.inst.DisplayNotification("Attempting to save beatmap already, please wait!", 2f, EditorManager.NotificationType.Error);
                return;
            }

            RTFile.CopyFile(RTFile.CombinePaths(RTFile.BasePath, Level.LEVEL_LSB), RTFile.CombinePaths(RTFile.BasePath, $"level-previous{FileFormat.LSB.Dot()}"));

            MetaData.Current.WriteToFile(RTFile.CombinePaths(RTFile.BasePath, Level.METADATA_LSB));
            CoroutineHelper.StartCoroutine(SaveData());
            CoroutineHelper.StartCoroutine(SavePlayers());
            RTEditor.inst.SaveSettings();

            return;
        }

        /// <summary>
        /// Saves the current level.
        /// </summary>
        public IEnumerator SaveData()
        {
            EditorManager.inst.DisplayNotification("Saving Beatmap!", 1f, EditorManager.NotificationType.Warning);
            EditorManager.inst.savingBeatmap = true;

            var gameData = GameData.Current;
            if (gameData.data && gameData.data.level)
                gameData.data.level.modVersion = LegacyPlugin.ModVersion.ToString();

            if (EditorConfig.Instance.SaveAsync.Value)
                yield return CoroutineHelper.StartCoroutineAsync(gameData.ISaveData(CurrentLevel.GetFile(Level.LEVEL_LSB)));
            else
                yield return CoroutineHelper.StartCoroutine(gameData.ISaveData(CurrentLevel.GetFile(Level.LEVEL_LSB)));

            yield return CoroutineHelper.Seconds(0.5f);

            EditorManager.inst.DisplayNotification("Saved Beatmap!", 2f, EditorManager.NotificationType.Success);
            EditorManager.inst.savingBeatmap = false;

            Example.Current?.brain?.Notice(ExampleBrain.Notices.EDITOR_SAVED_LEVEL);

            yield break;
        }

        /// <summary>
        /// Saves the current players data.
        /// </summary>
        public IEnumerator SavePlayers()
        {
            EditorManager.inst.DisplayNotification("Saving Player Models...", 1f, EditorManager.NotificationType.Warning);

            if (EditorConfig.Instance.SaveAsync.Value)
                yield return CoroutineHelper.StartCoroutineAsync(CoroutineHelper.DoAction(() => RTFile.WriteToFile(CurrentLevel.GetFile(Level.PLAYERS_LSB), PlayersData.Current.ToJSON().ToString())));
            else
                RTFile.WriteToFile(CurrentLevel.GetFile(Level.PLAYERS_LSB), PlayersData.Current.ToJSON().ToString());

            PlayersData.Save();

            yield return CoroutineHelper.Seconds(0.5f);
            EditorManager.inst.DisplayNotification("Saved Player Models!", 1f, EditorManager.NotificationType.Success);
        }

        /// <summary>
        /// Sets the current audio the editor should use.
        /// </summary>
        /// <param name="audioClip">Audio to set.</param>
        public void SetCurrentAudio(AudioClip audioClip) => AudioManager.inst.PlayMusic(null, audioClip, true, 0f, true);

        /// <summary>
        /// Restarts the autosave loop.
        /// </summary>
        public void SetAutosave()
        {
            var autosavesDirectory = RTFile.CombinePaths(RTFile.BasePath, "autosaves");
            RTFile.CreateDirectory(autosavesDirectory);
            var files = Directory.GetFiles(autosavesDirectory, $"autosave_{FileFormat.LSB.ToPattern()}", SearchOption.TopDirectoryOnly);

            EditorManager.inst.autosaves.Clear();
            EditorManager.inst.autosaves.AddRange(files);

            EditorManager.inst.CancelInvoke(nameof(AutosaveLevel));
            CancelInvoke(nameof(AutosaveLevel));
            InvokeRepeating(nameof(AutosaveLevel), EditorConfig.Instance.AutosaveLoopTime.Value, EditorConfig.Instance.AutosaveLoopTime.Value);
        }

        /// <summary>
        /// Autosaves the level.
        /// </summary>
        public void AutosaveLevel()
        {
            if (EditorManager.inst.loading)
                return;

            autosaving = true;

            if (!EditorManager.inst.hasLoadedLevel)
            {
                EditorManager.inst.DisplayNotification("Beatmap can't autosave until you load a level.", 3f, EditorManager.NotificationType.Error);
                autosaving = false;
                return;
            }

            if (EditorManager.inst.savingBeatmap)
            {
                EditorManager.inst.DisplayNotification("Already attempting to save the beatmap!", 2f, EditorManager.NotificationType.Error);
                autosaving = false;
                return;
            }

            var autosavesDirectory = RTFile.CombinePaths(RTFile.BasePath, "autosaves");
            var autosavePath = RTFile.CombinePaths(autosavesDirectory, $"autosave_{DateTime.Now.ToString("yyyy-MM-dd_HH.mm.ss")}{FileFormat.LSB.Dot()}");

            RTFile.CreateDirectory(autosavesDirectory);

            EditorManager.inst.DisplayNotification("Autosaving backup...", 2f, EditorManager.NotificationType.Warning);

            EditorManager.inst.autosaves.Add(autosavePath);

            while (EditorManager.inst.autosaves.Count > EditorConfig.Instance.AutosaveLimit.Value)
            {
                var first = EditorManager.inst.autosaves[0];
                if (RTFile.FileExists(first))
                    File.Delete(first);

                EditorManager.inst.autosaves.RemoveAt(0);
            }

            GameData.Current?.SaveData(autosavePath);

            EditorManager.inst.DisplayNotification("Autosaved backup!", 2f, EditorManager.NotificationType.Success);

            Example.Current?.brain?.Notice(ExampleBrain.Notices.EDITOR_AUTOSAVED);

            autosaveTimer.Reset();
            autosaving = false;
        }

        /// <summary>
        /// Saves a backup of the current level.
        /// </summary>
        public void SaveBackup()
        {
            if (EditorManager.inst.loading)
                return;

            autosaving = true;

            if (!EditorManager.inst.hasLoadedLevel)
            {
                EditorManager.inst.DisplayNotification("Beatmap can't autosave until you load a level.", 3f, EditorManager.NotificationType.Error);
                autosaving = false;
                return;
            }

            if (EditorManager.inst.savingBeatmap)
            {
                EditorManager.inst.DisplayNotification("Already attempting to save the beatmap!", 2f, EditorManager.NotificationType.Error);
                autosaving = false;
                return;
            }

            var autosavesDirectory = RTFile.CombinePaths(RTFile.BasePath, "autosaves");
            var autosavePath = RTFile.CombinePaths(autosavesDirectory, $"backup_{DateTime.Now.ToString("yyyy-MM-dd_HH.mm.ss")}{FileFormat.LSB.Dot()}");

            RTFile.CreateDirectory(autosavesDirectory);

            EditorManager.inst.DisplayNotification("Saving backup...", 2f, EditorManager.NotificationType.Warning);

            GameData.Current?.SaveData(autosavePath);

            EditorManager.inst.DisplayNotification("Autosaved backup!", 2f, EditorManager.NotificationType.Success);

            autosaveTimer.Reset();
            autosaving = false;
        }

        /// <summary>
        /// Saves a backup of a level.
        /// </summary>
        /// <param name="levelPanel">Level to save the backup of.</param>
        public void SaveBackup(LevelPanel levelPanel)
        {
            if (EditorManager.inst.loading)
                return;

            autosaving = true;

            if (EditorManager.inst.savingBeatmap)
            {
                EditorManager.inst.DisplayNotification("Already attempting to save the beatmap!", 2f, EditorManager.NotificationType.Error);
                autosaving = false;
                return;
            }

            var autosavesDirectory = RTFile.CombinePaths(levelPanel.FolderPath, "autosaves");
            var autosavePath = RTFile.CombinePaths(autosavesDirectory, $"backup_{DateTime.Now.ToString("yyyy-MM-dd_HH.mm.ss")}{FileFormat.LSB.Dot()}");

            RTFile.CreateDirectory(autosavesDirectory);

            EditorManager.inst.DisplayNotification("Saving backup...", 2f, EditorManager.NotificationType.Warning);

            RTFile.CopyFile(levelPanel.Level.GetFile(levelPanel.Level.CurrentFile), autosavePath);

            EditorManager.inst.DisplayNotification("Saved backup!", 2f, EditorManager.NotificationType.Success);

            autosaving = false;
        }

        /// <summary>
        /// Creates a new level and loads it.
        /// </summary>
        public void CreateNewLevel() => CreateNewLevel(() => LevelTemplateEditor.inst.CurrentTemplate.GetGameData(), newLevelSettings);

        /// <summary>
        /// Creates a new level and loads it.
        /// </summary>
        /// <param name="func">Custom GameData to create and assign to the new level.</param>
        /// <param name="newLevelSettings">Settings to apply to the new level.</param>
        public void CreateNewLevel(Func<GameData> func, NewLevelSettings newLevelSettings)
        {
            var newAudioPath = newLevelSettings.audioPath;
            var newLevelName = newLevelSettings.levelName;
            var newSongArtist = newLevelSettings.songArtist;
            var newSongTitle = newLevelSettings.songTitle;
            var newLevelDifficulty = newLevelSettings.difficulty;

            if (string.IsNullOrEmpty(newAudioPath))
            {
                EditorManager.inst.DisplayNotification("The file path is empty.", 2f, EditorManager.NotificationType.Error);
                return;
            }

            if (!RTFile.FileExists(newAudioPath))
            {
                EditorManager.inst.DisplayNotification("The file you are trying to load doesn't appear to exist.", 2f, EditorManager.NotificationType.Error);
                return;
            }

            var audioFormat = RTFile.GetFileFormat(newAudioPath);
            if (!RTFile.ValidAudio(audioFormat))
            {
                EditorManager.inst.DisplayNotification($"The file you are trying to load doesn't appear to be a song file.\nDetected format: {audioFormat}", 6f, EditorManager.NotificationType.Error);
                return;
            }

            bool setNew = false;
            int num = 0;
            string p = RTFile.CombinePaths(RTEditor.inst.BeatmapsPath, RTEditor.inst.EditorPath, newLevelName);
            while (RTFile.DirectoryExists(p))
            {
                p = RTFile.CombinePaths(RTEditor.inst.BeatmapsPath, RTEditor.inst.EditorPath, newLevelName) + " - " + num.ToString();
                num += 1;
                setNew = true;

            }
            if (setNew)
                newLevelName += " - " + num.ToString();

            var path = RTFile.CombinePaths(RTEditor.inst.BeatmapsPath, RTEditor.inst.EditorPath, newLevelName);

            if (RTFile.DirectoryExists(path))
            {
                EditorManager.inst.DisplayNotification($"The level you are trying to create already exists.\nName: {Path.GetFileName(path)}", 4f, EditorManager.NotificationType.Error);
                return;
            }

            var gameData = func?.Invoke();

            if (!gameData)
                return;

            Directory.CreateDirectory(path);

            RTFile.CopyFile(newAudioPath, RTFile.CombinePaths(path, $"level{audioFormat.Dot()}"));

            gameData.SaveData(RTFile.CombinePaths(path, Level.LEVEL_LSB));
            var metaData = new MetaData();
            metaData.beatmap.gameVersion = ProjectArrhythmia.GameVersion.ToString();
            metaData.arcadeID = LSText.randomNumString(16);
            metaData.artist.name = newSongArtist;
            metaData.artist.link = newSongArtist;
            metaData.song.title = newSongTitle;
            metaData.song.difficulty = newLevelDifficulty;
            metaData.uploaderName = SteamWrapper.inst.user.displayName;
            metaData.creator.name = SteamWrapper.inst.user.displayName;
            metaData.creator.steamID = SteamWrapper.inst.user.id;
            metaData.beatmap.name = newLevelName;

            MetaData.Current = metaData;

            fromNewLevel = true;
            metaData.WriteToFile(RTFile.CombinePaths(path, Level.METADATA_LSB));

            var levelPanel = new LevelPanel();
            levelPanel.Init(new Level(path));
            LevelPanels.Add(levelPanel);
            LoadLevel(levelPanel);
            RTEditor.inst.NewLevelPopup.Close();
        }

        /// <summary>
        /// Creates the default beatmap.
        /// </summary>
        /// <returns>Returns the base beatmap.</returns>
        public GameData CreateBaseBeatmap()
        {
            var gameData = new GameData();
            gameData.data = new BeatmapData();
            gameData.data.level = new LevelData()
            {
                limitPlayer = false,
            };
            gameData.data.checkpoints.Add(Checkpoint.Default);

            if (gameData.events == null)
                gameData.events = new List<List<EventKeyframe>>();
            gameData.events.Clear();
            GameData.ClampEventListValues(gameData.events);

            for (int i = 0; i < (Seasons.IsAprilFools ? 45 : 25); i++)
            {
                var backgroundObject = new BackgroundObject();
                backgroundObject.name = "bg - " + i;
                if (UnityEngine.Random.value > 0.5f)
                    backgroundObject.scale = new Vector2(UnityEngine.Random.Range(2, 8), UnityEngine.Random.Range(2, 8));
                else
                {
                    float num = UnityEngine.Random.Range(2, 6);
                    backgroundObject.scale = new Vector2(num, num);
                }
                backgroundObject.pos = new Vector2(UnityEngine.Random.Range(-48, 48), UnityEngine.Random.Range(-32, 32));
                backgroundObject.color = UnityEngine.Random.Range(1, 6);
                backgroundObject.depth = UnityEngine.Random.Range(0, 6);

                if (UnityEngine.Random.value > 0.5f)
                {
                    backgroundObject.reactiveType = (BackgroundObject.ReactiveType)UnityEngine.Random.Range(1, 5);
                    backgroundObject.reactiveScale = UnityEngine.Random.Range(0.01f, 0.04f);
                }

                if (Seasons.IsAprilFools)
                {
                    var randomShape = UnityEngine.Random.Range(0, ShapeManager.inst.Shapes3D.Count - 1);
                    if (randomShape != 4 && randomShape != 6)
                        backgroundObject.shape = randomShape;
                }

                gameData.backgroundObjects.Add(backgroundObject);
            }

            var beatmapObject = ObjectEditor.inst.CreateNewBeatmapObject(0.5f);
            beatmapObject.events[0].Add(new EventKeyframe(4f, new float[3] { 10f, 0f, 0f }, new float[3]));
            if (Seasons.IsAprilFools)
                beatmapObject.events[2].Add(new EventKeyframe(12f, new float[1] { 360000f }, new float[3]));

            beatmapObject.name = Seasons.IsAprilFools ? "trololololo" : RTEditor.DEFAULT_OBJECT_NAME;
            beatmapObject.autoKillType = AutoKillType.LastKeyframeOffset;
            beatmapObject.autoKillOffset = 4f;
            beatmapObject.editorData.Layer = 0;
            gameData.beatmapObjects.Add(beatmapObject);

            return gameData;
        }

        void OpenLevelPopupOnFinish()
        {
            if (!EditorConfig.Instance.OpenNewLevelCreatorIfNoLevels.Value || LevelPanels.Count > 0)
            {
                RTEditor.inst.OpenLevelPopup.Open();
                EditorManager.inst.RenderOpenBeatmapPopup();
            }
            else
                EditorManager.inst.OpenNewLevelPopup();
        }

        /// <summary>
        /// Refreshes the search and sort of the editor levels.
        /// </summary>
        public void RenderLevels() => CoroutineHelper.StartCoroutine(IRenderLevels());

        /// <summary>
        /// Refreshes the search and sort of the editor levels.
        /// </summary>
        public IEnumerator IRenderLevels()
        {
            CoreHelper.Log($"Level Search: {EditorManager.inst.openFileSearch}\nLevel Sort: { RTEditor.inst.levelAscend} - { RTEditor.inst.levelSort}");

            var levelPanels = RTEditor.inst.levelSort switch
            {
                LevelSort.Cover => LevelPanels.Order(x => x.Level && x.Level.icon != SteamWorkshop.inst.defaultSteamImageSprite, !RTEditor.inst.levelAscend),
                LevelSort.Artist => LevelPanels.Order(x => x.Level?.metadata?.artist?.name ?? string.Empty, !RTEditor.inst.levelAscend),
                LevelSort.Creator => LevelPanels.Order(x => x.Level?.metadata?.creator?.name ?? string.Empty, !RTEditor.inst.levelAscend),
                LevelSort.File => LevelPanels.Order(x => x.FolderPath, !RTEditor.inst.levelAscend),
                LevelSort.Title => LevelPanels.Order(x => x.Level?.metadata?.song?.title ?? string.Empty, !RTEditor.inst.levelAscend),
                LevelSort.Difficulty => LevelPanels.Order(x => x.Level?.metadata?.song?.difficulty ?? 0, !RTEditor.inst.levelAscend),
                LevelSort.DateEdited => LevelPanels.Order(x => x.Level?.metadata?.beatmap?.dateEdited ?? string.Empty, !RTEditor.inst.levelAscend),
                LevelSort.DateCreated => LevelPanels.Order(x => x.Level?.metadata?.beatmap?.dateCreated ?? string.Empty, !RTEditor.inst.levelAscend),
                LevelSort.DatePublished => LevelPanels.Order(x => x.Level?.metadata?.beatmap?.datePublished ?? string.Empty, !RTEditor.inst.levelAscend),
                _ => LevelPanels,
            };

            levelPanels = levelPanels.Order(x => x.isFolder, true); // folders should always be at the top.

            int num = 0;
            foreach (var editorWrapper in levelPanels)
            {
                var folder = editorWrapper.FolderPath;
                var metadata = editorWrapper.Level?.metadata;

                editorWrapper.SetActive(editorWrapper.isFolder && RTString.SearchString(EditorManager.inst.openFileSearch, Path.GetFileName(folder)) ||
                    !editorWrapper.isFolder && (RTString.SearchString(EditorManager.inst.openFileSearch, Path.GetFileName(folder)) ||
                        metadata == null || metadata != null &&
                        (RTString.SearchString(EditorManager.inst.openFileSearch, metadata.song.title) ||
                        RTString.SearchString(EditorManager.inst.openFileSearch, metadata.artist.name) ||
                        RTString.SearchString(EditorManager.inst.openFileSearch, metadata.creator.name) ||
                        RTString.SearchString(EditorManager.inst.openFileSearch, metadata.song.description) ||
                        RTString.SearchString(EditorManager.inst.openFileSearch, LevelPanel.difficultyNames[Mathf.Clamp(metadata.song.difficulty, 0, LevelPanel.difficultyNames.Length - 1)]))));

                editorWrapper.GameObject.transform.SetSiblingIndex(num);
                num++;
            }

            var content = RTEditor.inst.OpenLevelPopup.Content;

            if (content.Find("back"))
            {
                yield return null;
                if (content.Find("back"))
                    content.Find("back").SetAsFirstSibling();
            }

            yield break;
        }

        /// <summary>
        /// Refreshes the editor file browsing.
        /// </summary>
        public void RefreshFileBrowserLevels() => RTFileBrowser.inst.UpdateBrowserFile(FileFormat.LSB.Dot(), "level", x => LoadLevel(new Level(RTFile.ReplaceSlash(x).Remove("/" + Level.LEVEL_LSB))));

        /// <summary>
        /// Refreshes the autosaves and backups of a level.
        /// </summary>
        /// <param name="levelPanel">Level to get autosaves from.</param>
        public void RefreshAutosaveList(LevelPanel levelPanel)
        {
            RTEditor.inst.AutosavePopup.SearchField.onValueChanged.NewListener(_val => RefreshAutosaveList(levelPanel));

            RTEditor.inst.AutosavePopup.ClearContent();

            if (levelPanel.isFolder)
            {
                EditorManager.inst.DisplayNotification("Folders can't have autosaves / backups.", 1.5f, EditorManager.NotificationType.Warning);
                return;
            }

            var files =
                Directory.GetFiles(levelPanel.FolderPath, $"autosave_{FileFormat.LSB.ToPattern()}", SearchOption.AllDirectories)
                .Union(Directory.GetFiles(levelPanel.FolderPath, $"backup_{FileFormat.LSB.ToPattern()}", SearchOption.AllDirectories));

            foreach (var file in files)
            {
                if (!RTString.SearchString(RTEditor.inst.AutosavePopup.SearchTerm, Path.GetFileName(file)))
                    continue;

                var path = RTFile.ReplaceSlash(file);
                var gameObject = EditorManager.inst.folderButtonPrefab.Duplicate(RTEditor.inst.AutosavePopup.Content, $"Folder [{Path.GetFileName(file)}]");
                var folderButtonStorage = gameObject.GetComponent<FunctionButtonStorage>();
                folderButtonStorage.button.onClick.ClearAll();

                string tmpFile = path;

                var contextMenu = gameObject.AddComponent<FolderButtonFunction>();
                contextMenu.onClick = eventData =>
                {
                    switch (eventData.button)
                    {
                        // just realized I could collapse the switch case blocks like this
                        case PointerEventData.InputButton.Left: {
                                levelPanel.Level.currentFile = tmpFile.Remove(RTFile.AppendEndSlash(levelPanel.FolderPath));

                                LoadLevel(levelPanel);
                                RTEditor.inst.OpenLevelPopup.Close();
                                break;
                            }
                        case PointerEventData.InputButton.Right: {
                                EditorContextMenu.inst.ShowContextMenu(
                                    new ButtonFunction("Open", () =>
                                    {
                                        levelPanel.Level.currentFile = tmpFile.Remove(RTFile.AppendEndSlash(levelPanel.FolderPath));

                                        LoadLevel(levelPanel);
                                        RTEditor.inst.OpenLevelPopup.Close();
                                    }),
                                    new ButtonFunction("Toggle Backup State", () =>
                                    {
                                        var fi = new FileInfo(tmpFile);

                                        tmpFile = tmpFile.Contains("autosave_") ? tmpFile.Replace("autosave_", "backup_") : tmpFile.Replace("backup_", "autosave_");

                                        if (fi.Exists)
                                            fi.MoveTo(tmpFile);

                                        RefreshAutosaveList(levelPanel);
                                    }, "Autosave Toggle Backup State"),
                                    new ButtonFunction("Delete", () =>
                                    {
                                        RTEditor.inst.ShowWarningPopup("Are you sure you want to delete this autosave? This is permanent!", () =>
                                        {
                                            RTFile.DeleteFile(tmpFile);
                                            RefreshAutosaveList(levelPanel);
                                        }, RTEditor.inst.HideWarningPopup);
                                    })
                                    );
                                break;
                            }
                    }
                };

                var hoverUI = gameObject.AddComponent<HoverUI>();
                hoverUI.size = EditorConfig.Instance.OpenLevelButtonHoverSize.Value;
                hoverUI.animatePos = false;
                hoverUI.animateSca = true;

                folderButtonStorage.label.text = Path.GetFileName(file);

                var backup = EditorPrefabHolder.Instance.Function1Button.Duplicate(gameObject.transform, "backup");
                var backupHolder = backup.GetComponent<FunctionButtonStorage>();
                backup.transform.localScale = Vector3.one;
                UIManager.SetRectTransform(backup.transform.AsRT(), new Vector2(450f, 0f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(80f, 28f));
                backupHolder.label.text = "Backup";
                backupHolder.button.onClick.NewListener(() =>
                {
                    var fi = new FileInfo(tmpFile);

                    tmpFile = tmpFile.Contains("autosave_") ? tmpFile.Replace("autosave_", "backup_") : tmpFile.Replace("backup_", "autosave_");

                    if (fi.Exists)
                        fi.MoveTo(tmpFile);

                    RefreshAutosaveList(levelPanel);
                });

                TooltipHelper.AssignTooltip(backup, "Autosave Toggle Backup State");

                EditorThemeManager.ApplySelectable(folderButtonStorage.button, ThemeGroup.List_Button_1);
                EditorThemeManager.ApplyGraphic(backupHolder.button.image, ThemeGroup.Function_1, true);
                EditorThemeManager.ApplyGraphic(backupHolder.label, ThemeGroup.Function_1_Text);
            }
        }

        /// <summary>
        /// Converts a level to VG format and outputs it to the exports folder.
        /// </summary>
        /// <param name="levelPanel">Level to convert to VG.</param>
        public void ConvertLevel(LevelPanel levelPanel)
        {
            if (levelPanel.isFolder)
                return;

            var currentPath = levelPanel.FolderPath;
            var fileName = levelPanel.Level.FolderName;

            var exportPath = EditorConfig.Instance.ConvertLevelLSToVGExportPath.Value;

            if (string.IsNullOrEmpty(exportPath))
            {
                var output = RTFile.CombinePaths(RTFile.ApplicationDirectory, RTEditor.DEFAULT_EXPORTS_PATH);
                RTFile.CreateDirectory(output);
                exportPath = output + "/";
            }

            exportPath = RTFile.AppendEndSlash(exportPath);

            if (!RTFile.DirectoryExists(Path.GetDirectoryName(exportPath)))
            {
                EditorManager.inst.DisplayNotification("Directory does not exist.", 2f, EditorManager.NotificationType.Error);
                return;
            }

            var gamedata = GameData.ReadFromFile(RTFile.CombinePaths(currentPath, Level.LEVEL_LSB), ArrhythmiaType.LS);
            var metadata = MetaData.ReadFromFile(RTFile.CombinePaths(currentPath, Level.METADATA_LSB), ArrhythmiaType.LS);

            var vgd = gamedata.ToJSONVG();

            var vgm = metadata.ToJSONVG();

            var path = exportPath + fileName;

            RTFile.CreateDirectory(path);

            RTFile.CopyFile(RTFile.CombinePaths(currentPath, Level.LEVEL_OGG), RTFile.CombinePaths(path, Level.AUDIO_OGG));
            RTFile.CopyFile(RTFile.CombinePaths(currentPath, Level.LEVEL_WAV), RTFile.CombinePaths(path, Level.AUDIO_WAV));
            RTFile.CopyFile(RTFile.CombinePaths(currentPath, Level.LEVEL_MP3), RTFile.CombinePaths(path, Level.AUDIO_MP3));

            RTFile.CopyFile(RTFile.CombinePaths(currentPath, Level.LEVEL_JPG), RTFile.CombinePaths(path, Level.COVER_JPG));

            try
            {
                RTFile.WriteToFile(RTFile.CombinePaths(path, Level.METADATA_VGM), vgm.ToString());
            }
            catch (Exception ex)
            {
                Debug.LogError($"{MetadataEditor.inst.className}Convert to VG error (MetaData) {ex}");
            }

            try
            {

                RTFile.WriteToFile(RTFile.CombinePaths(path, Level.LEVEL_VGD), vgd.ToString());
            }
            catch (Exception ex)
            {
                Debug.LogError($"{MetadataEditor.inst.className}Convert to VG error (GameData) {ex}");
            }

            EditorManager.inst.DisplayNotification($"Converted Level \"{fileName}\" from LS format to VG format and saved to {Path.GetFileName(path)}.", 4f,
                EditorManager.NotificationType.Success);

            AchievementManager.inst.UnlockAchievement("time_machine");
        }

        /// <summary>
        /// Zips a level folder.
        /// </summary>
        /// <param name="levelPanel">Level to zip.</param>
        public void ZipLevel(LevelPanel levelPanel)
        {
            EditorManager.inst.DisplayNotification($"Zipping {Path.GetFileName(RTFile.RemoveEndSlash(levelPanel.FolderPath))}...", 2f, EditorManager.NotificationType.Warning);

            IZipLevel(levelPanel).StartAsync();
        }

        /// <summary>
        /// Zips a level folder.
        /// </summary>
        /// <param name="levelPanel">Level to zip.</param>
        public IEnumerator IZipLevel(LevelPanel levelPanel)
        {
            var currentPath = levelPanel.FolderPath;
            bool failed;
            var zipPath = RTFile.RemoveEndSlash(currentPath) + FileFormat.ZIP.Dot();
            try
            {
                RTFile.DeleteFile(zipPath);

                System.IO.Compression.ZipFile.CreateFromDirectory(currentPath, zipPath);

                failed = false;
            }
            catch (Exception ex)
            {
                failed = true;
                CoreHelper.LogException(ex);
            }

            yield return Ninja.JumpToUnity;
            if (failed)
                EditorManager.inst.DisplayNotification($"Had an error with zipping the folder. Check the logs!", 2f, EditorManager.NotificationType.Error);
            else
                EditorManager.inst.DisplayNotification($"Successfully zipped the folder to {Path.GetFileName(zipPath)}!", 2f, EditorManager.NotificationType.Success);
            yield break;
        }

        /// <summary>
        /// Moves the folder to a recycling folder.
        /// </summary>
        /// <param name="level">Level to remove.</param>
        public void RecycleLevel(LevelPanel levelPanel)
        {
            var folderPath = levelPanel.FolderPath;
            var recyclingPath = RTFile.CombinePaths(RTFile.ApplicationDirectory, "recycling");
            RTFile.CreateDirectory(recyclingPath);
            RTFile.MoveDirectory(folderPath, RTFile.CombinePaths(recyclingPath, Path.GetFileName(folderPath)));
            LoadLevels();
        }

        #region Story Development

        string storyLevelsCompilerPath = "C:/Users/Mecha/Documents/Project Arrhythmia/Unity/BetterLegacyEditor/Assets/Story Levels";
        string storyLevelsVersionControlPath = "C:/Users/Mecha/Documents/Project Arrhythmia/BetterLegacy.Story";

        public void ToStoryLevel() => ToStoryLevel(RTEditor.inst.editorInfo);

        public void ToStoryLevel(EditorInfo editorInfo)
        {
            try
            {
                if (!editorInfo.isStory)
                    return;

                string cutsceneDestination = string.Empty;
                if (editorInfo.cutsceneDestination != Story.CutsceneDestination.Level)
                    cutsceneDestination = editorInfo.cutsceneDestination.ToString().ToLower() + "_cutscene";
                int cutscene = 0;
                if (editorInfo.cutscene >= 0)
                    cutscene = editorInfo.cutscene;

                ToStoryLevel(editorInfo.storyChapter, editorInfo.storyLevel, cutsceneDestination, cutscene);
            }
            catch { }
        }

        public void ToStoryLevel(int chapter, int level, string type = "", int cutscene = 0)
        {
            var path = RTFile.BasePath;
            var doc = $"doc{RTString.ToStoryNumber(chapter)}";
            var saveTo = RTFile.CombinePaths(storyLevelsCompilerPath, doc, $"{doc}_{RTString.ToStoryNumber(level)}{(string.IsNullOrEmpty(type) ? string.Empty : "_" + type + RTString.ToStoryNumber(cutscene))}");

            RTFile.CreateDirectory(saveTo);
            RTFile.CopyFile(RTFile.CombinePaths(path, Level.LEVEL_LSB), RTFile.CombinePaths(saveTo, $"level{FileFormat.JSON.Dot()}"));
            RTFile.CopyFile(RTFile.CombinePaths(path, Level.METADATA_LSB), RTFile.CombinePaths(saveTo, $"metadata{FileFormat.JSON.Dot()}"));
            RTFile.CopyFile(RTFile.CombinePaths(path, Level.PLAYERS_LSB), RTFile.CombinePaths(saveTo, $"players{FileFormat.JSON.Dot()}"));
            RTFile.CopyFile(RTFile.CombinePaths(path, Level.LEVEL_OGG), RTFile.CombinePaths(saveTo, $"song{FileFormat.OGG.Dot()}"));
            RTFile.CopyFile(RTFile.CombinePaths(path, Level.LEVEL_JPG), RTFile.CombinePaths(saveTo, Level.COVER_JPG));

            EditorManager.inst.DisplayNotification("Saved the level to the story level compiler.", 2f, EditorManager.NotificationType.Success);
        }

        public void ToStoryVersionControl() => ToStoryVersionControl(RTEditor.inst.editorInfo);

        public void ToStoryVersionControl(EditorInfo editorInfo)
        {
            try
            {
                if (!editorInfo.isStory)
                    return;

                string cutsceneDestination = string.Empty;
                if (editorInfo.cutsceneDestination != Story.CutsceneDestination.Level)
                    cutsceneDestination = editorInfo.cutsceneDestination.ToString().ToLower();
                int cutscene = 0;
                if (editorInfo.cutscene >= 0)
                    cutscene = editorInfo.cutscene;

                ToStoryVersionControl(editorInfo.storyChapter, editorInfo.storyLevel, cutsceneDestination, cutscene);
            }
            catch { }
        }

        public void ToStoryVersionControl(int chapter, int level, string type, int cutscene)
        {
            var path = RTFile.BasePath;
            var doc = $"doc{RTString.ToStoryNumber(chapter)}";
            var saveTo = RTFile.CombinePaths(storyLevelsVersionControlPath, doc, $"{doc}_{RTString.ToStoryNumber(level)}{(string.IsNullOrEmpty(type) ? "" : "_" + type + RTString.ToStoryNumber(cutscene))} - {EditorLevelManager.inst.CurrentLevel.FolderName}");

            RTFile.CreateDirectory(saveTo);
            RTFile.CopyFile(RTFile.CombinePaths(path, Level.LEVEL_LSB), RTFile.CombinePaths(saveTo, Level.LEVEL_LSB));
            RTFile.CopyFile(RTFile.CombinePaths(path, Level.METADATA_LSB), RTFile.CombinePaths(saveTo, Level.METADATA_LSB));
            RTFile.CopyFile(RTFile.CombinePaths(path, Level.PLAYERS_LSB), RTFile.CombinePaths(saveTo, Level.PLAYERS_LSB));
            RTFile.CopyFile(RTFile.CombinePaths(path, Level.EDITOR_LSE), RTFile.CombinePaths(saveTo, Level.EDITOR_LSE));
            // don't copy audio files
            //RTFile.CopyFile(RTFile.CombinePaths(path, Level.LEVEL_OGG), RTFile.CombinePaths(saveTo, $"song{FileFormat.OGG.Dot()}"));
            RTFile.CopyFile(RTFile.CombinePaths(path, Level.LEVEL_JPG), RTFile.CombinePaths(saveTo, Level.LEVEL_JPG));

            EditorManager.inst.DisplayNotification("Saved the level to the story level version control.", 2f, EditorManager.NotificationType.Success);
        }

        #endregion

        #endregion
    }
}
