using BetterLegacy.Arcade;
using BetterLegacy.Configs;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Managers;
using BetterLegacy.Core.Managers.Networking;
using BetterLegacy.Core.Optimization;
using BetterLegacy.Menus;
using BetterLegacy.Menus.UI.Interfaces;
using InControl;
using LSFunctions;
using SimpleJSON;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BetterLegacy.Core.Helpers
{
    public static class ArcadeHelper
    {
        public static bool endedLevel;

        public static void LoadInputSelect()
        {
            LevelManager.Levels.Clear();
            InterfaceManager.inst.CloseMenus();
            SceneHelper.LoadInputSelect();
        }

        public static void ReturnToHub()
        {
            if (LevelManager.Hub == null)
                return;

            LevelManager.LevelEnded = false;
            LevelManager.CurrentLevel = LevelManager.Hub;

            InterfaceManager.inst.CloseMenus();

            SceneHelper.LoadGame();
        }

        public static void FirstLevel()
        {
            if (LevelManager.CurrentLevelCollection != null)
            {
                var prevIndex = LevelManager.currentLevelIndex;
                LevelManager.currentLevelIndex = LevelManager.CurrentLevelCollection.EntryLevelIndex;

                CoreHelper.Log($"Update collection index - Prev: {prevIndex} > Current: {LevelManager.currentLevelIndex}");
                LevelManager.LevelEnded = false;
                LevelManager.CurrentLevel = LevelManager.CurrentLevelCollection[LevelManager.currentLevelIndex];

                InterfaceManager.inst.CloseMenus();

                SceneHelper.LoadGame();

                return;
            }

            if (!LevelManager.HasQueue)
                return;

            var prev = LevelManager.currentQueueIndex;
            LevelManager.currentQueueIndex = 0;

            CoreHelper.Log($"Update queue - Prev: {prev} > Current: {LevelManager.currentQueueIndex}");

            LevelManager.LevelEnded = false;
            LevelManager.CurrentLevel = LevelManager.ArcadeQueue[LevelManager.currentQueueIndex];

            InterfaceManager.inst.CloseMenus();

            SceneHelper.LoadGame();
        }

        public static void NextLevel()
        {
            if (LevelManager.CurrentLevelCollection != null)
            {
                var prevIndex = LevelManager.currentLevelIndex;
                LevelManager.currentLevelIndex++;

                CoreHelper.Log($"Update collection index - Prev: {prevIndex} > Current: {LevelManager.currentLevelIndex}");

                if (LevelManager.currentLevelIndex >= LevelManager.CurrentLevelCollection.Count)
                    return;

                LevelManager.LevelEnded = false;
                LevelManager.CurrentLevel = LevelManager.CurrentLevelCollection[LevelManager.currentLevelIndex];

                InterfaceManager.inst.CloseMenus();

                SceneHelper.LoadGame();

                return;
            }

            var prev = LevelManager.currentQueueIndex;
            LevelManager.currentQueueIndex++;

            CoreHelper.Log($"Update queue - Prev: {prev} > Current: {LevelManager.currentQueueIndex}");

            if (LevelManager.IsEndOfQueue)
                return;

            LevelManager.LevelEnded = false;
            LevelManager.CurrentLevel = LevelManager.ArcadeQueue[LevelManager.currentQueueIndex];

            InterfaceManager.inst.CloseMenus();

            SceneHelper.LoadGame();
        }

        public static void RestartLevel(bool respawnPlayers, Action action)
        {
            if (CoreHelper.InEditor || !CoreHelper.InGame)
                return;

            if (endedLevel)
                LevelManager.LevelEnded = false;

            GameManager.inst.hits.Clear();
            GameManager.inst.deaths.Clear();

            if (respawnPlayers)
                PlayerManager.RespawnPlayers();

            AudioManager.inst.SetMusicTime(0f);
            action?.Invoke();
            endedLevel = false;
        }

        public static void QuitToArcade()
        {
            InterfaceManager.inst.CloseMenus();

            CoreHelper.Log("Quitting to arcade...");
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

            LevelManager.LevelEnded = false;
            LevelManager.Hub = null;

            if (CoreHelper.InEditor)
            {
                ArcadeManager.inst.skippedLoad = false;
                ArcadeManager.inst.forcedSkip = false;
                LevelManager.IsArcade = true;
                SceneHelper.LoadInputSelect();
                return;
            }

            if (!LevelManager.IsArcade)
            {
                SceneHelper.LoadScene(SceneName.Main_Menu);
                return;
            }
            SceneHelper.LoadScene(SceneName.Arcade_Select);
        }

        public static void EndOfLevel()
        {
            endedLevel = true;

            if (EndLevelMenu.Current != null)
                return;

            EndLevelMenu.Init();
        }

        public static bool fromLevel = false;

        public static void DeleteComponents()
        {
            CoreHelper.Destroy(GameObject.Find("Interface"));
            CoreHelper.Destroy(GameObject.Find("EventSystem").GetComponent<InControlInputModule>());
            CoreHelper.Destroy(GameObject.Find("EventSystem").GetComponent<BaseInput>());
            GameObject.Find("EventSystem").AddComponent<StandaloneInputModule>();
            CoreHelper.Destroy(GameObject.Find("Main Camera").GetComponent<InterfaceLoader>());
            CoreHelper.Destroy(GameObject.Find("Main Camera").GetComponent<ArcadeController>());
            CoreHelper.Destroy(GameObject.Find("Main Camera").GetComponent<FlareLayer>());
            CoreHelper.Destroy(GameObject.Find("Main Camera").GetComponent<GUILayer>());
        }

        public static void ReloadMenu()
        {
            if (!fromLevel)
            {
                LoadLevelsManager.Init();
                return;
            }

            DeleteComponents();

            var currentCollection = LevelManager.CurrentLevelCollection;
            if (!currentCollection)
            {
                ArcadeMenu.Init();
                return;
            }

            LevelListMenu.close = () => LevelCollectionMenu.Init(currentCollection);
            LevelListMenu.Init(currentCollection.levels);
        }

        public static bool currentlyLoading = false;
        public static IEnumerator GetLevelList()
        {
            float delay = 0f;
            if (currentlyLoading)
            {
                LoadLevelsManager.inst?.End();
                yield break;
            }

            var sw = CoreHelper.StartNewStopwatch();
            currentlyLoading = true;
            fromLevel = false;
            ArcadeManager.inst.skippedLoad = false;
            ArcadeManager.inst.forcedSkip = false;
            LevelManager.IsArcade = true;

            var levelsDirectory = RTFile.CombinePaths(RTFile.ApplicationDirectory, LevelManager.ListPath);

            if (!RTFile.DirectoryExists(levelsDirectory))
                Directory.CreateDirectory(levelsDirectory);

            var directories = Directory.GetDirectories(levelsDirectory, "*", SearchOption.TopDirectoryOnly);

            if (LoadLevelsManager.inst != null)
                LoadLevelsManager.totalLevelCount = directories.Length;

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

                if (LoadLevelsManager.inst && LoadLevelsManager.inst.cancelled)
                {
                    SceneHelper.LoadInputSelect();
                    currentlyLoading = false;
                    yield break;
                }

                var path = folder.Replace("\\", "/");
                var name = Path.GetFileName(path);

                if (loadYieldMode != YieldType.None)
                    yield return CoreHelper.GetYieldInstruction(loadYieldMode, ref delay);

                if (RTFile.FileExists(RTFile.CombinePaths(path, LevelCollection.COLLECTION_LSCO)))
                {
                    levelCollections.Enqueue(path);
                    continue;
                }

                MetaData metadata = null;

                if (RTFile.FileExists(RTFile.CombinePaths(path, Level.METADATA_VGM)))
                    metadata = MetaData.ParseVG(JSON.Parse(RTFile.ReadFromFile(RTFile.CombinePaths(path, Level.METADATA_VGM))));
                else if (RTFile.FileExists(RTFile.CombinePaths(path, Level.METADATA_LSB)))
                    metadata = MetaData.Parse(JSON.Parse(RTFile.ReadFromFile(RTFile.CombinePaths(path, RTFile.CombinePaths(path, Level.METADATA_LSB)))), false);

                if (metadata == null)
                {
                    if (LoadLevelsManager.inst)
                        LoadLevelsManager.inst.UpdateInfo(SteamWorkshop.inst.defaultSteamImageSprite, $"<color=$FF0000>No metadata in {name}</color>", i, true);

                    continue;
                }

                if (!RTFile.FileExists(RTFile.CombinePaths(path, Level.LEVEL_OGG)) && !RTFile.FileExists(RTFile.CombinePaths(path, Level.LEVEL_WAV)) && !RTFile.FileExists(RTFile.CombinePaths(path, Level.LEVEL_MP3))
                    && !RTFile.FileExists(RTFile.CombinePaths(path, Level.AUDIO_OGG)) && !RTFile.FileExists(RTFile.CombinePaths(path, Level.AUDIO_WAV)) && !RTFile.FileExists(RTFile.CombinePaths(path, Level.AUDIO_MP3)))
                {
                    if (LoadLevelsManager.inst)
                        LoadLevelsManager.inst.UpdateInfo(SteamWorkshop.inst.defaultSteamImageSprite, $"<color=$FF0000>No song in {name}</color>", i, true);

                    continue;
                }

                if (!RTFile.FileExists(RTFile.CombinePaths(path, Level.LEVEL_LSB)) && !RTFile.FileExists(RTFile.CombinePaths(path, Level.LEVEL_VGD)))
                {
                    if (LoadLevelsManager.inst)
                        LoadLevelsManager.inst.UpdateInfo(SteamWorkshop.inst.defaultSteamImageSprite, $"<color=$FF0000>No song in {name}</color>", i, true);

                    continue;
                }

                if ((string.IsNullOrEmpty(metadata.serverID) || metadata.serverID == "-1")
                    && (string.IsNullOrEmpty(metadata.beatmap.beatmap_id) && metadata.beatmap.beatmap_id == "-1" || metadata.beatmap.beatmap_id == "0")
                    && (string.IsNullOrEmpty(metadata.arcadeID) || metadata.arcadeID.Contains("-") /* < don't want negative IDs */ || metadata.arcadeID == "0"))
                {
                    metadata.arcadeID = LSText.randomNumString(16);
                    var metadataJN = metadata.ToJSON();
                    RTFile.WriteToFile(RTFile.CombinePaths(path, Level.METADATA_LSB), metadataJN.ToString(3));
                }

                var level = new Level(path + "/", metadata);

                if (LevelManager.Saves.TryFind(x => x.ID == level.id, out LevelManager.PlayerData playerData))
                    level.playerData = playerData;

                if (LoadLevelsManager.inst)
                    LoadLevelsManager.inst.UpdateInfo(level.icon, $"Loading {name}", i);

                LevelManager.Levels.Add(level);
            }

            if (ArcadeConfig.Instance.LoadSteamLevels.Value)
            {
                yield return CoreHelper.StartCoroutine(SteamWorkshopManager.inst.GetSubscribedItems((Level level, int i) =>
                {
                    if (!LoadLevelsManager.inst)
                        return;

                    LoadLevelsManager.totalLevelCount = (int)SteamWorkshopManager.inst.LevelCount;
                    LoadLevelsManager.inst.UpdateInfo(level.icon, $"Steam: Loading {Path.GetFileName(Path.GetDirectoryName(level.path))}", i);
                }));
            }

            LevelManager.Sort(ArcadeConfig.Instance.LocalLevelOrderby.Value, ArcadeConfig.Instance.LocalLevelAscend.Value);

            SteamWorkshopManager.inst.Levels = LevelManager.SortLevels(SteamWorkshopManager.inst.Levels, ArcadeConfig.Instance.SteamLevelOrderby.Value, ArcadeConfig.Instance.SteamLevelAscend.Value);

            int collectionIndex = 0;
            LoadLevelsManager.totalLevelCount = levelCollections.Count;
            while (levelCollections.Count > 0)
            {
                var path = levelCollections.Dequeue();
                var name = Path.GetFileName(path);

                var levelCollection = LevelCollection.Parse($"{path}/", JSON.Parse(RTFile.ReadFromFile(RTFile.CombinePaths(path, LevelCollection.COLLECTION_LSCO))));
                LevelManager.LevelCollections.Add(levelCollection);
                if (LoadLevelsManager.inst)
                    LoadLevelsManager.inst.UpdateInfo(levelCollection.icon, $"Loading {name}", collectionIndex);
                collectionIndex++;
            }
            
            sw.Stop();
            CoreHelper.Log($"Total levels: {LevelManager.Levels.Union(SteamWorkshopManager.inst.Levels).Count()}\nTime taken: {sw.Elapsed}");

            currentlyLoading = false;

            LoadLevelsManager.inst?.End();
            yield break;
        }

        public static void CopyArcadeQueue()
        {
            var jn = JSON.Parse("{}");

            for (int i = 0; i < LevelManager.ArcadeQueue.Count; i++)
            {
                jn["queue"][i]["id"] = LevelManager.ArcadeQueue[i].id;

                if (LevelManager.ArcadeQueue[i].metadata && LevelManager.ArcadeQueue[i].metadata.beatmap is LevelBeatmap levelBeatmap)
                {
                    if (!string.IsNullOrEmpty(LevelManager.ArcadeQueue[i].metadata.serverID))
                        jn["queue"][i]["server_id"] = LevelManager.ArcadeQueue[i].metadata.serverID;
                    if (!string.IsNullOrEmpty(LevelManager.ArcadeQueue[i].metadata.serverID))
                        jn["queue"][i]["workhsop_id"] = levelBeatmap.beatmap_id;
                    jn["queue"][i]["name"] = levelBeatmap.name;
                }
            }

            LSText.CopyToClipboard(jn.ToString(3));
        }

        public static void PasteArcadeQueue()
        {
            try
            {
                if (!Clipboard.ContainsText())
                    return;

                var text = Clipboard.GetText();

                var jn = JSON.Parse(text);

                if (jn["queue"] == null)
                    return;

                LevelManager.ArcadeQueue.Clear();

                for (int i = 0; i < jn["queue"].Count; i++)
                {
                    var jnQueue = jn["queue"][i];

                    var hasLocal = LevelManager.Levels.TryFindIndex(x => x.id == jnQueue["id"], out int localIndex);
                    var hasSteam = SteamWorkshopManager.inst.Levels.TryFindIndex(x => x.id == jnQueue["id"], out int steamIndex);

                    if ((hasLocal || hasSteam) && !LevelManager.ArcadeQueue.Has(x => x.id == jnQueue["id"]))
                    {
                        var currentLevel = hasSteam ? SteamWorkshopManager.inst.Levels[steamIndex] : LevelManager.Levels[localIndex];

                        LevelManager.ArcadeQueue.Add(currentLevel);
                    }
                    else if (!hasLocal && !hasSteam)
                        CoreHelper.LogError($"Level with ID {jnQueue["id"]} (Name: {jnQueue["name"]}) does not currently exist in your Local folder / Steam subscribed items.\n" +
                            $"Find the level on the server: {jnQueue["server_id"]}\n" +
                            $"or find the level on the Steam Workshop: {jnQueue["workhsop_id"]}");
                }

                if (ArcadeMenu.Current != null)
                    ArcadeMenu.Current.RefreshQueueLevels(true);
            }
            catch (Exception ex)
            {
                CoreHelper.LogError($"Pasted text was probably not in the correct format.\n{ex}");
            }

        }

        public static IEnumerator OnLoadingEnd()
        {
            yield return new WaitForSeconds(0.1f);
            SoundManager.inst.PlaySound(DefaultSounds.loadsound);
            ArcadeMenu.Init();
            yield break;
        }
    }
}
