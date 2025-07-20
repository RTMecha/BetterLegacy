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
using BetterLegacy.Core.Managers.Networking;
using BetterLegacy.Menus;
using BetterLegacy.Menus.UI.Elements;
using BetterLegacy.Menus.UI.Interfaces;

namespace BetterLegacy.Arcade.Interfaces
{
    public class LoadLevelsMenu : MenuBase
    {
        public static LoadLevelsMenu Current { get; set; }

        public static void Init() => Init(ArcadeHelper.OnLoadingEnd().Start);

        public static void Init(Action onLoadingEnd)
        {
            InterfaceManager.inst.CloseMenus();
            Current = new LoadLevelsMenu();
            InterfaceManager.inst.CurrentInterface = Current;
            Current.StartGeneration();
            CoroutineHelper.StartCoroutine(Current.GetLevelList(onLoadingEnd));
        }

        public LoadLevelsMenu()
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

        MenuText title;
        MenuImage icon;
        MenuImage progressBar;

        int totalLevelCount;

        public static bool currentlyLoading;
        public bool cancelled;

        /// <summary>
        /// Loads the Arcade & Steam levels.
        /// </summary>
        /// <param name="onLoadingEnd">Function to run when loading ends.</param>
        public IEnumerator GetLevelList(Action onLoadingEnd = null)
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

            var levelsDirectory = RTFile.CombinePaths(RTFile.ApplicationDirectory, LevelManager.ListPath);

            RTFile.CreateDirectory(levelsDirectory);

            var directories = Directory.GetDirectories(levelsDirectory, "*", SearchOption.TopDirectoryOnly);

            totalLevelCount = directories.Length;

            LevelManager.Levels.Clear();
            LevelManager.ArcadeQueue.Clear();
            LevelManager.LevelCollections.Clear();
            LevelManager.CurrentLevel = null;
            LevelManager.CurrentLevelCollection = null;
            LevelManager.LoadProgress();

            var levelCollections = new Queue<string>();

            var loadYieldMode = ArcadeConfig.Instance.LoadYieldMode.Value;

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

                MetaData metadata = null;

                if (RTFile.FileExists(RTFile.CombinePaths(path, Level.METADATA_VGM)))
                    metadata = MetaData.ParseVG(JSON.Parse(RTFile.ReadFromFile(RTFile.CombinePaths(path, Level.METADATA_VGM))));
                else if (RTFile.FileExists(RTFile.CombinePaths(path, Level.METADATA_LSB)))
                    metadata = MetaData.Parse(JSON.Parse(RTFile.ReadFromFile(RTFile.CombinePaths(path, RTFile.CombinePaths(path, Level.METADATA_LSB)))));

                if (!metadata)
                {
                    UpdateInfo(SteamWorkshop.inst.defaultSteamImageSprite, $"<color=$FF0000>No metadata in {name}</color>", i, true);
                    continue;
                }

                if (!RTFile.FileExists(RTFile.CombinePaths(path, Level.LEVEL_OGG)) && !RTFile.FileExists(RTFile.CombinePaths(path, Level.LEVEL_WAV)) && !RTFile.FileExists(RTFile.CombinePaths(path, Level.LEVEL_MP3))
                    && !RTFile.FileExists(RTFile.CombinePaths(path, Level.AUDIO_OGG)) && !RTFile.FileExists(RTFile.CombinePaths(path, Level.AUDIO_WAV)) && !RTFile.FileExists(RTFile.CombinePaths(path, Level.AUDIO_MP3)))
                {
                    UpdateInfo(SteamWorkshop.inst.defaultSteamImageSprite, $"<color=$FF0000>No song in {name}</color>", i, true);
                    continue;
                }

                if (!RTFile.FileExists(RTFile.CombinePaths(path, Level.LEVEL_LSB)) && !RTFile.FileExists(RTFile.CombinePaths(path, Level.LEVEL_VGD)))
                {
                    UpdateInfo(SteamWorkshop.inst.defaultSteamImageSprite, $"<color=$FF0000>No song in {name}</color>", i, true);
                    continue;
                }

                metadata.VerifyID(path);
                var level = new Level(path, metadata);

                LevelManager.AssignSaveData(level);
                
                UpdateInfo(level.icon, $"Loading {name}", i);

                LevelManager.Levels.Add(level);
            }

            CoreHelper.Log($"Finished loading Arcade levels at {sw.Elapsed}");

            if (ArcadeConfig.Instance.LoadSteamLevels.Value)
            {
                yield return CoroutineHelper.StartCoroutine(SteamWorkshopManager.inst.GetSubscribedItems((Level level, int i) =>
                {
                    totalLevelCount = (int)SteamWorkshopManager.inst.LevelCount;
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

            SteamWorkshopManager.inst.Levels = LevelManager.SortLevels(SteamWorkshopManager.inst.Levels, ArcadeConfig.Instance.SteamLevelOrderby.Value, ArcadeConfig.Instance.SteamLevelAscend.Value);

            int collectionIndex = 0;
            totalLevelCount = levelCollections.Count;
            while (levelCollections.Count > 0)
            {
                var path = levelCollections.Dequeue();
                var name = Path.GetFileName(path);

                var levelCollection = LevelCollection.Parse(path, JSON.Parse(RTFile.ReadFromFile(RTFile.CombinePaths(path, LevelCollection.COLLECTION_LSCO))));
                LevelManager.LevelCollections.Add(levelCollection);
                UpdateInfo(levelCollection.icon, $"Loading {name}", collectionIndex);
                collectionIndex++;
            }

            sw.Stop();
            CoreHelper.Log($"Total levels: {LevelManager.Levels.Union(SteamWorkshopManager.inst.Levels).Count()}\nTime taken: {sw.Elapsed}");
            sw = null;

            currentlyLoading = false;

            onLoadingEnd?.Invoke();

            yield break;
        }

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
                CoreHelper.LogError($"{status}");
        }

        public void UpdateInfo(string name, float percentage)
        {
            if (progressBar && progressBar.image)
                progressBar.image.rectTransform.sizeDelta = new Vector2(600f * percentage, 32f);

            if (title && title.textUI)
            {
                title.textUI.maxVisibleCharacters = 9999;
                title.textUI.text = "<size=30>" + LSText.ClampString(name, 52);
            }
        }

        public void Exit()
        {
            cancelled = true;
        }
    }
}
