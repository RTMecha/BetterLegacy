using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using UnityEngine;

using LSFunctions;

using SimpleJSON;

using BetterLegacy.Configs;
using BetterLegacy.Core;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Data.Level;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using BetterLegacy.Menus;
using BetterLegacy.Menus.UI.Elements;
using BetterLegacy.Menus.UI.Interfaces;

namespace BetterLegacy.Arcade.Interfaces
{
    /// <summary>
    /// Interface for loading levels and level collections.
    /// </summary>
    public class LoadLevelsInterface : BaseInterface
    {
        #region Constructors
        
        public LoadLevelsInterface()
        {
            name = "Loading Levels";

            elements.Add(new MenuImage
            {
                id = "0",
                name = "BG",
                rect = RectValues.Default.SizeDelta(800f, 600f),
                color = 6,
                opacity = 0.1f,
                length = 0f,
                wait = false,
            });

            icon = new MenuImage
            {
                id = "1",
                name = "Icon",
                rect = RectValues.Default.AnchoredPosition(0f, 130f).SizeDelta(256f, 256f),
                overrideColor = Color.white,
                useOverrideColor = true,
                length = 0f,
                wait = false,
                rounded = 0,
            };
            elements.Add(icon);

            title = new MenuText
            {
                id = LSText.randomNumString(16),
                name = "Title",
                alignment = TMPro.TextAlignmentOptions.Center,
                rect = new RectValues(new Vector2(0f, -40f), Vector2.one, Vector2.zero, new Vector2(0f, 0.5f), new Vector2(32f, 32f)),
                length = 0f,
                wait = false,
                interpolateText = false,
                hideBG = true,
                textColor = 6,
                opacity = 1f,
            };
            elements.Add(title);

            elements.Add(new MenuImage
            {
                id = "2",
                name = "Progress Base",
                rect = RectValues.Default.AnchoredPosition(-300f, -140f).Pivot(0f, 0.5f).SizeDelta(600f, 32f),
                mask = true,
                color = 6,
                opacity = 0.1f,
                length = 0f,
                wait = false,
            });

            progressBar = new MenuImage
            {
                id = "3",
                name = "Progress",
                parent = "2",
                rect = RectValues.Default.AnchoredPosition(-300f, 0f).Pivot(0f, 0.5f).SizeDelta(0f, 32f),
                color = 6,
                opacity = 1f,
                length = 0f,
                wait = false,
            };
            elements.Add(progressBar);

            exitFunc = Exit;
        }

        #endregion

        #region Values

        /// <summary>
        /// The current <see cref="LoadLevelsInterface"/>.
        /// </summary>
        public static LoadLevelsInterface Current { get; set; }

        MenuText title;
        MenuImage icon;
        MenuImage progressBar;

        int totalLevelCount;

        /// <summary>
        /// If the levels and level collections are currently loading.
        /// </summary>
        public static bool currentlyLoading;

        /// <summary>
        /// If loading has been cancelled.
        /// </summary>
        public bool cancelled;

        #endregion

        #region Functions

        /// <summary>
        /// Initializes <see cref="LoadLevelsInterface"/> with the default directory, end function and load settings.
        /// </summary>
        public static void Init() => Init(ArcadeHelper.OnLoadingEnd().Start);

        /// <summary>
        /// Initializes <see cref="LoadLevelsInterface"/> with a set directory and default end function and load settings.
        /// </summary>
        /// <param name="levelsDirectory">Directory to load from.</param>
        public static void Init(string levelsDirectory) => Init(levelsDirectory, ArcadeHelper.OnLoadingEnd().Start);

        /// <summary>
        /// Initializes <see cref="LoadLevelsInterface"/> with a set end function and default directory and load settings.
        /// </summary>
        /// <param name="onLoadingEnd">Function to run when loading has ended.</param>
        public static void Init(Action onLoadingEnd) => Init(RTFile.CombinePaths(RTFile.ApplicationDirectory, LevelManager.ListPath), onLoadingEnd);

        /// <summary>
        /// Initializes <see cref="LoadLevelsInterface"/> with a set directory and end function and default load settings.
        /// </summary>
        /// <param name="levelsDirectory">Directory to load from.</param>
        /// <param name="onLoadingEnd">Function to run when loading has ended.</param>
        public static void Init(string levelsDirectory, Action onLoadingEnd)
        {
            InterfaceManager.inst.CloseMenus();
            Current = new LoadLevelsInterface();
            InterfaceManager.inst.CurrentInterface = Current;
            Current.StartGeneration();
            CoroutineHelper.StartCoroutine(Current.GetLevelList(levelsDirectory, true, ArcadeConfig.Instance.LoadSteamLevels.Value, onLoadingEnd));
        }

        /// <summary>
        /// Initializes <see cref="LoadLevelsInterface"/> with a set directory and default end function and only loads local levels.
        /// </summary>
        public static void InitLocal() => InitLocal(RTFile.CombinePaths(RTFile.ApplicationDirectory, LevelManager.ListPath));

        /// <summary>
        /// Initializes <see cref="LoadLevelsInterface"/> with a set directory and default end function and only loads local levels.
        /// </summary>
        /// <param name="levelsDirectory">Directory to load from.</param>
        public static void InitLocal(string levelsDirectory)
        {
            InterfaceManager.inst.CloseMenus();
            Current = new LoadLevelsInterface();
            InterfaceManager.inst.CurrentInterface = Current;
            Current.StartGeneration();
            CoroutineHelper.StartCoroutine(Current.GetLevelList(levelsDirectory, true, false, ArcadeHelper.OnLoadingEnd().Start));
        }

        /// <summary>
        /// Initializes <see cref="LoadLevelsInterface"/> with a set directory and default end function and only loads Steam levels.
        /// </summary>
        public static void InitSteam()
        {
            InterfaceManager.inst.CloseMenus();
            Current = new LoadLevelsInterface();
            InterfaceManager.inst.CurrentInterface = Current;
            Current.StartGeneration();
            CoroutineHelper.StartCoroutine(Current.GetLevelList(string.Empty, false, true, ArcadeHelper.OnLoadingEnd().Start));
        }

        /// <summary>
        /// Loads the Arcade & Steam levels.
        /// </summary>
        /// <param name="onLoadingEnd">Function to run when loading ends.</param>
        public IEnumerator GetLevelList(string levelsDirectory, bool loadLocal, bool loadSteam, Action onLoadingEnd = null)
        {
            float delay = 0f;
            if (currentlyLoading)
            {
                onLoadingEnd?.Invoke();
                yield break;
            }

            var sw = CoreHelper.StartNewStopwatch();
            currentlyLoading = true;
            ArcadeHelper.fromLevel = false;
            ArcadeManager.inst.skippedLoad = false;
            ArcadeManager.inst.forcedSkip = false;
            LevelManager.IsArcade = true;

            var loadYieldMode = ArcadeConfig.Instance.LoadYieldMode.Value;

            var levelCollections = new Queue<string>();

            if (loadLocal)
            {
                RTFile.CreateDirectory(levelsDirectory);

                var directories = Directory.GetDirectories(levelsDirectory, "*", SearchOption.TopDirectoryOnly);

                totalLevelCount = directories.Length;

                LevelManager.Levels.Clear();
                LevelManager.ArcadeQueue.Clear();
                LevelManager.LevelCollections.Clear();
                LevelManager.CurrentLevel = null;
                LevelManager.CurrentLevelCollection = null;
                LevelManager.LoadProgress();

                for (int i = 0; i < directories.Length; i++)
                {
                    var folder = directories[i];

                    if (cancelled)
                    {
                        SceneHelper.LoadScene(SceneName.Main_Menu, false);
                        currentlyLoading = false;
                        LevelManager.ClearData();
                        yield break;
                    }

                    var path = RTFile.ReplaceSlash(folder);
                    var name = Path.GetFileName(path);

                    if (loadYieldMode != YieldType.None)
                        yield return CoroutineHelper.GetYieldInstruction(loadYieldMode, ref delay);

                    if (RTFile.FileExists(RTFile.CombinePaths(path, LevelCollection.COLLECTION_LSCO)))
                    {
                        levelCollections.Enqueue(path);
                        continue;
                    }

                    if (!RTFile.FileExists(RTFile.CombinePaths(path, Level.METADATA_VGM)) && !RTFile.FileExists(RTFile.CombinePaths(path, Level.METADATA_LSB)))
                    {
                        levelCollections.Enqueue(path);
                        continue;
                    }

                    MetaData metadata = null;

                    try
                    {
                        if (RTFile.FileExists(RTFile.CombinePaths(path, Level.METADATA_VGM)))
                            metadata = MetaData.ParseVG(JSON.Parse(RTFile.ReadFromFile(RTFile.CombinePaths(path, Level.METADATA_VGM))));
                        else if (RTFile.FileExists(RTFile.CombinePaths(path, Level.METADATA_LSB)))
                            metadata = MetaData.Parse(JSON.Parse(RTFile.ReadFromFile(RTFile.CombinePaths(path, RTFile.CombinePaths(path, Level.METADATA_LSB)))));
                    }
                    catch (Exception ex)
                    {
                        CoreHelper.LogError($"Could not load metadata of {name} due to the exception: {ex}");
                        UpdateInfo(LegacyPlugin.AtanPlaceholder, $"<color=$FF0000>Failed to load metadata of {name}</color>", i, true);
                        continue;
                    }

                    if (!metadata)
                    {
                        UpdateInfo(LegacyPlugin.AtanPlaceholder, $"<color=$FF0000>No metadata in {name}</color>", i, true);
                        continue;
                    }

                    if (!RTFile.FileExists(RTFile.CombinePaths(path, Level.LEVEL_OGG)) && !RTFile.FileExists(RTFile.CombinePaths(path, Level.LEVEL_WAV)) && !RTFile.FileExists(RTFile.CombinePaths(path, Level.LEVEL_MP3))
                        && !RTFile.FileExists(RTFile.CombinePaths(path, Level.AUDIO_OGG)) && !RTFile.FileExists(RTFile.CombinePaths(path, Level.AUDIO_WAV)) && !RTFile.FileExists(RTFile.CombinePaths(path, Level.AUDIO_MP3)))
                    {
                        UpdateInfo(LegacyPlugin.AtanPlaceholder, $"<color=$FF0000>No song in {name}</color>", i, true);
                        continue;
                    }

                    if (!RTFile.FileExists(RTFile.CombinePaths(path, Level.LEVEL_LSB)) && !RTFile.FileExists(RTFile.CombinePaths(path, Level.LEVEL_VGD)))
                    {
                        UpdateInfo(LegacyPlugin.AtanPlaceholder, $"<color=$FF0000>No song in {name}</color>", i, true);
                        continue;
                    }

                    try
                    {
                        metadata.VerifyID(path);
                        var level = new Level(path, metadata);

                        LevelManager.AssignSaveData(level);

                        UpdateInfo(level.icon, $"Loading {name}", i);

                        LevelManager.Levels.Add(level);
                    }
                    catch (Exception ex)
                    {
                        CoreHelper.LogError($"Could not load {name} due to the exception: {ex}");
                        UpdateInfo(LegacyPlugin.AtanPlaceholder, $"<color=$FF0000>Failed to load {name}</color>", i, true);
                        continue;
                    }
                }

                CoreHelper.Log($"Finished loading Arcade levels at {sw.Elapsed}");
            }

            if (loadSteam)
            {
                yield return CoroutineHelper.StartCoroutine(RTSteamManager.inst.GetSubscribedItems((Level level, int i) =>
                {
                    totalLevelCount = (int)RTSteamManager.inst.LevelCount;
                    UpdateInfo(level.icon, $"Steam: Loading {Path.GetFileName(RTFile.RemoveEndSlash(level.path))}", i);
                }));

                if (!currentlyLoading)
                {
                    onLoadingEnd?.Invoke();
                    yield break;
                }

                CoreHelper.Log($"Finished loading Steam levels at {sw.Elapsed}");
            }

            LevelManager.Sort(ArcadeConfig.Instance.LocalLevelOrderby.Value, ArcadeConfig.Instance.LocalLevelAscend.Value);

            RTSteamManager.inst.Levels = LevelManager.SortLevels(RTSteamManager.inst.Levels, ArcadeConfig.Instance.SteamLevelOrderby.Value, ArcadeConfig.Instance.SteamLevelAscend.Value);

            if (loadLocal)
            {
                int collectionIndex = 0;
                totalLevelCount = levelCollections.Count;
                while (!levelCollections.IsEmpty())
                {
                    var path = levelCollections.Dequeue();
                    var name = Path.GetFileName(path);

                    LevelCollection levelCollection = null;

                    if (!RTFile.FileExists(RTFile.CombinePaths(path, LevelCollection.COLLECTION_LSCO)))
                    {
                        if (!ArcadeConfig.Instance.LoadFolders.Value)
                        {
                            collectionIndex++;
                            continue;
                        }
                        levelCollection = new LevelCollection() { path = path, isFolder = true, icon = LegacyPlugin.AtanPlaceholder, name = name, id = PAObjectBase.GetNumberID(), };
                    }
                    else
                        levelCollection = LevelCollection.Parse(path, JSON.Parse(RTFile.ReadFromFile(RTFile.CombinePaths(path, LevelCollection.COLLECTION_LSCO))));

                    LevelManager.AssignSaveData(levelCollection);

                    LevelManager.LevelCollections.Add(levelCollection);
                    UpdateInfo(levelCollection.icon, $"Loading {name}", collectionIndex);
                    collectionIndex++;
                }
            }

            sw.Stop();
            CoreHelper.Log($"Total levels: {LevelManager.Levels.Union(RTSteamManager.inst.Levels).Count()}\nTime taken: {sw.Elapsed}");
            sw = null;

            currentlyLoading = false;

            onLoadingEnd?.Invoke();

            yield break;
        }

        /// <summary>
        /// Updates the loading info.
        /// </summary>
        /// <param name="sprite">Sprite icon to display.</param>
        /// <param name="status">Status of loading to display.</param>
        /// <param name="num">Progress of loading.</param>
        /// <param name="logError">If the status should be logged as an error.</param>
        public void UpdateInfo(Sprite sprite, string status, int num, bool logError = false)
        {
            float e = num / (float)totalLevelCount;

            if (progressBar && progressBar.image)
                progressBar.image.rectTransform.sizeDelta = new Vector2(600f * e, 32f);

            if (icon && icon.image)
                icon.image.sprite = sprite;

            if (title && title.textUI)
            {
                title.textUI.maxVisibleCharacters = 9999;
                title.textUI.text = "<size=30>" + LSText.ClampString(status, 52);
            }

            if (logError)
                CoreHelper.LogError(status);
        }

        /// <summary>
        /// Closes the interface.
        /// </summary>
        public void Exit() => cancelled = true;

        #endregion
    }
}
