using BetterLegacy.Arcade;
using BetterLegacy.Configs;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Managers;
using BetterLegacy.Core.Managers.Networking;
using BetterLegacy.Menus;
using BetterLegacy.Menus.UI.Interfaces;
using LSFunctions;
using SimpleJSON;
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using UnityEngine;
using UnityEngine.UI;

namespace BetterLegacy.Core.Helpers
{
    public static class ArcadeHelper
    {
        public static GameObject buttonPrefab;
        public static bool endedLevel;

        public static void ReturnToHub()
        {
            if (LevelManager.Hub == null)
                return;

            LevelManager.LevelEnded = false;
            LevelManager.CurrentLevel = LevelManager.Hub;

            InterfaceManager.inst.CurrentMenu?.Clear();
            InterfaceManager.inst.CurrentMenu = null;
            PauseMenu.Current = null;
            EndLevelMenu.Current = null;

            SceneManager.inst.LoadScene("Game");
        }

        public static void FirstLevel()
        {
            if (!LevelManager.HasQueue)
                return;

            var prev = LevelManager.currentQueueIndex;
            LevelManager.currentQueueIndex = 0;

            CoreHelper.Log($"Update queue - Prev: {prev} > Current: {LevelManager.currentQueueIndex}");

            LevelManager.LevelEnded = false;
            LevelManager.CurrentLevel = LevelManager.ArcadeQueue[LevelManager.currentQueueIndex];

            InterfaceManager.inst.CurrentMenu?.Clear();
            InterfaceManager.inst.CurrentMenu = null;
            PauseMenu.Current = null;
            EndLevelMenu.Current = null;

            SceneManager.inst.LoadScene("Game");
        }

        public static void NextLevel()
        {
            var prev = LevelManager.currentQueueIndex;
            LevelManager.currentQueueIndex++;

            CoreHelper.Log($"Update queue - Prev: {prev} > Current: {LevelManager.currentQueueIndex}");

            if (LevelManager.IsEndOfQueue)
                return;

            LevelManager.LevelEnded = false;
            LevelManager.CurrentLevel = LevelManager.ArcadeQueue[LevelManager.currentQueueIndex];

            InterfaceManager.inst.CurrentMenu?.Clear();
            InterfaceManager.inst.CurrentMenu = null;
            PauseMenu.Current = null;
            EndLevelMenu.Current = null;

            SceneManager.inst.LoadScene("Game");
        }

        public static void RestartLevel(Action action)
        {
            if (CoreHelper.InEditor)
                return;

            var levelHasEnded = endedLevel;

            if (levelHasEnded)
                LevelManager.LevelEnded = false;

            AudioManager.inst.SetMusicTime(0f);
            GameManager.inst.hits.Clear();
            GameManager.inst.deaths.Clear();
            action?.Invoke();
            endedLevel = false;
        }

        public static void QuitToArcade()
        {
            InterfaceManager.inst.CurrentMenu?.Clear();
            InterfaceManager.inst.CurrentMenu = null;
            PauseMenu.Current = null;
            EndLevelMenu.Current = null;

            CoreHelper.Log("Quitting to arcade...");
            DG.Tweening.DOTween.Clear();
            DataManager.inst.gameData = null;
            DataManager.inst.gameData = new GameData();
            InputDataManager.inst.SetAllControllerRumble(0f);

            LevelManager.LevelEnded = false;
            LevelManager.Hub = null;

            if (CoreHelper.InEditor)
            {
                ArcadeManager.inst.skippedLoad = false;
                ArcadeManager.inst.forcedSkip = false;
                LevelManager.IsArcade = true;
                SceneManager.inst.LoadScene("Input Select");
                return;
            }

            if (!LevelManager.IsArcade)
            {
                SceneManager.inst.LoadScene("Main Menu");
                return;
            }
            SceneManager.inst.LoadScene("Arcade Select");
        }

        public static void EndOfLevel()
        {
            endedLevel = true;

            if (EndLevelMenu.Current != null)
                return;

            EndLevelMenu.Init();
            return;

            //var __instance = GameManager.inst;
            //GameManager.inst.players.SetActive(false);
            //InputDataManager.inst.SetAllControllerRumble(0f);

            //__instance.timeline.gameObject.SetActive(false);
            //__instance.menuUI.GetComponentInChildren<Image>().enabled = true;
            //LSHelpers.ShowCursor();

            //var ic = __instance.menuUI.GetComponent<InterfaceController>();

            //var metadata = LevelManager.CurrentLevel.metadata;

            //if (DataManager.inst.GetSettingBool("IsArcade", false))
            //{
            //    CoreHelper.Log($"Setting Player Data");
            //    int prevHits = LevelManager.CurrentLevel.playerData != null ? LevelManager.CurrentLevel.playerData.Hits : -1;

            //    LevelManager.PlayedLevelCount++;

            //    if (LevelManager.Saves.Where(x => x.Completed).Count() >= 100)
            //    {
            //        SteamWrapper.inst.achievements.SetAchievement("GREAT_TESTER");
            //    }

            //    if (!PlayerManager.IsZenMode && !PlayerManager.IsPractice)
            //    {
            //        if (LevelManager.CurrentLevel.playerData == null)
            //        {
            //            LevelManager.CurrentLevel.playerData = new LevelManager.PlayerData
            //            {
            //                ID = LevelManager.CurrentLevel.id,
            //            };
            //        }

            //        CoreHelper.Log($"Updating save data\n" +
            //            $"Deaths [OLD = {LevelManager.CurrentLevel.playerData.Deaths} > NEW = {__instance.deaths.Count}]\n" +
            //            $"Hits: [OLD = {LevelManager.CurrentLevel.playerData.Hits} > NEW = {__instance.hits.Count}]\n" +
            //            $"Boosts: [OLD = {LevelManager.CurrentLevel.playerData.Boosts} > NEW = {LevelManager.BoostCount}]");

            //        if (LevelManager.CurrentLevel.playerData.Deaths == 0 || LevelManager.CurrentLevel.playerData.Deaths > __instance.deaths.Count)
            //            LevelManager.CurrentLevel.playerData.Deaths = __instance.deaths.Count;
            //        if (LevelManager.CurrentLevel.playerData.Hits == 0 || LevelManager.CurrentLevel.playerData.Hits > __instance.hits.Count)
            //            LevelManager.CurrentLevel.playerData.Hits = __instance.hits.Count;
            //        if (LevelManager.CurrentLevel.playerData.Boosts == 0 || LevelManager.CurrentLevel.playerData.Boosts > LevelManager.BoostCount)
            //            LevelManager.CurrentLevel.playerData.Boosts = LevelManager.BoostCount;
            //        LevelManager.CurrentLevel.playerData.Completed = true;

            //        if (LevelManager.Saves.Has(x => x.ID == LevelManager.CurrentLevel.id))
            //        {
            //            var saveIndex = LevelManager.Saves.FindIndex(x => x.ID == LevelManager.CurrentLevel.id);
            //            LevelManager.Saves[saveIndex] = LevelManager.CurrentLevel.playerData;
            //        }
            //        else
            //            LevelManager.Saves.Add(LevelManager.CurrentLevel.playerData);

            //        if (LevelManager.Levels.TryFind(x => x.id == LevelManager.CurrentLevel.id, out Level level))
            //        {
            //            level.playerData = LevelManager.CurrentLevel.playerData;
            //        }
            //    }

            //    LevelManager.SaveProgress();

            //    CoreHelper.Log($"Setting More Info");
            //    //More Info
            //    {
            //        var moreInfo = ic.interfaceBranches.Find(x => x.name == "end_of_level_more_info");
            //        moreInfo.elements[5] = new InterfaceController.InterfaceElement(InterfaceController.InterfaceElement.Type.Text, "You died a total of " + __instance.deaths.Count + " times.", "end_of_level_more_info");
            //        moreInfo.elements[6] = new InterfaceController.InterfaceElement(InterfaceController.InterfaceElement.Type.Text, "You got hit a total of " + __instance.hits.Count + " times.", "end_of_level_more_info");
            //        moreInfo.elements[7] = new InterfaceController.InterfaceElement(InterfaceController.InterfaceElement.Type.Text, "You boosted a total of " + LevelManager.BoostCount + " times.", "end_of_level_more_info");
            //        moreInfo.elements[8] = new InterfaceController.InterfaceElement(InterfaceController.InterfaceElement.Type.Text, "Total song time: " + AudioManager.inst.CurrentAudioSource.clip.length, "end_of_level_more_info");
            //        moreInfo.elements[9] = new InterfaceController.InterfaceElement(InterfaceController.InterfaceElement.Type.Text, "Time in level: " + LevelManager.timeInLevel, "end_of_level_more_info");
            //    }

            //    int endOfLevelIndex = ic.interfaceBranches.FindIndex(x => x.name == "end_of_level");
            //    int getSongIndex = ic.interfaceBranches.FindIndex(x => x.name == "getsong");

            //    int line = 5;
            //    int dataPointMax = 24;
            //    int[] hitsNormalized = new int[dataPointMax + 1];
            //    foreach (var playerDataPoint in __instance.hits)
            //    {
            //        int num5 = (int)RTMath.SuperLerp(0f, AudioManager.inst.CurrentAudioSource.clip.length, 0f, (float)dataPointMax, playerDataPoint.time);
            //        hitsNormalized[num5]++;
            //    }

            //    CoreHelper.Log($"Setting Level Ranks");
            //    var levelRank = DataManager.inst.levelRanks.Find(x => hitsNormalized.Sum() >= x.minHits && hitsNormalized.Sum() <= x.maxHits);
            //    var newLevelRank = DataManager.inst.levelRanks.Find(x => prevHits >= x.minHits && prevHits <= x.maxHits);

            //    if (PlayerManager.IsZenMode)
            //    {
            //        levelRank = DataManager.inst.levelRanks.Find(x => x.name == "-");
            //        newLevelRank = null;
            //    }

            //    CoreHelper.Log($"Setting Achievements");
            //    if (levelRank.name == "SS")
            //        SteamWrapper.inst.achievements.SetAchievement("SS_RANK");
            //    else if (levelRank.name == "F")
            //        SteamWrapper.inst.achievements.SetAchievement("F_RANK");

            //    CoreHelper.Log($"Setting End UI");
            //    var sayings = LSText.WordWrap(levelRank.sayings[Random.Range(0, levelRank.sayings.Length)], 32);
            //    string easy = LSColors.GetThemeColorHex("easy");
            //    string normal = LSColors.GetThemeColorHex("normal");
            //    string hard = LSColors.GetThemeColorHex("hard");
            //    string expert = LSColors.GetThemeColorHex("expert");

            //    if (CoreConfig.Instance.ReplayLevel.Value)
            //    {
            //        AudioManager.inst.SetMusicTime(0f);
            //        AudioManager.inst.CurrentAudioSource.Play();
            //    }
            //    else
            //    {
            //        AudioManager.inst.SetMusicTime(AudioManager.inst.CurrentAudioSource.clip.length - 0.01f);
            //        AudioManager.inst.CurrentAudioSource.Pause();
            //    }

            //    for (int i = 0; i < 11; i++)
            //    {
            //        string text = "<b>";
            //        for (int j = 0; j < dataPointMax; j++)
            //        {
            //            int sum = hitsNormalized.Take(j + 1).Sum();
            //            int sumLerp = (int)RTMath.SuperLerp(0f, 15f, 0f, (float)11, (float)sum);
            //            string color = sum == 0 ? easy : sum <= 3 ? normal : sum <= 9 ? hard : expert;

            //            for (int k = 0; k < 2; k++)
            //            {
            //                if (sumLerp == i)
            //                {
            //                    text = text + "<color=" + color + "ff>▓</color>";
            //                }
            //                else if (sumLerp > i)
            //                {
            //                    text += "<alpha=#22>▓";
            //                }
            //                else if (sumLerp < i)
            //                {
            //                    text = text + "<color=" + color + "44>▓</color>";
            //                }
            //            }
            //        }
            //        text += "</b>";
            //        if (line == 5)
            //        {
            //            text = "<voffset=0.6em>" + text;

            //            if (prevHits == -1)
            //            {
            //                text += string.Format("       <voffset=0em><size=300%><color=#{0}><b>{1}</b></color>", LSColors.ColorToHex(levelRank.color), levelRank.name);
            //            }
            //            else if (prevHits > __instance.hits.Count && newLevelRank != null)
            //            {
            //                text += string.Format("       <voffset=0em><size=300%><color=#{0}><b>{1}</b></color><size=150%> <voffset=0.325em><b>-></b> <voffset=0em><size=300%><color=#{2}><b>{3}</b></color>", new object[]
            //                {
            //                    LSColors.ColorToHex(newLevelRank.color),
            //                    newLevelRank.name,
            //                    LSColors.ColorToHex(levelRank.color),
            //                    levelRank.name
            //                });
            //            }
            //            else
            //            {
            //                text += string.Format("       <voffset=0em><size=300%><color=#{0}><b>{1}</b></color>", LSColors.ColorToHex(levelRank.color), levelRank.name);
            //            }
            //        }

            //        if (line == 7)
            //        {
            //            text = "<voffset=0.6em>" + text;

            //            text += $"       <voffset=0em><size=300%><color=#{LSColors.ColorToHex(levelRank.color)}><b>{LevelManager.CalculateAccuracy(__instance.hits.Count, AudioManager.inst.CurrentAudioSource.clip.length)}%</b></color>";
            //        }

            //        if (line >= 9 && sayings.Count > line - 9)
            //        {
            //            text = text + "       <alpha=#ff>" + sayings[line - 9];
            //        }

            //        var interfaceElement = new InterfaceController.InterfaceElement(InterfaceController.InterfaceElement.Type.Text, text);
            //        interfaceElement.branch = "end_of_level";
            //        ic.interfaceBranches[endOfLevelIndex].elements[line] = interfaceElement;
            //        line++;
            //    }
            //    var levelSummary = new InterfaceController.InterfaceElement(InterfaceController.InterfaceElement.Type.Text, string.Format("Level Summary - <b>{0}</b> by {1}", metadata.song.title, metadata.artist.Name));
            //    levelSummary.branch = "end_of_level";
            //    ic.interfaceBranches[endOfLevelIndex].elements[2] = levelSummary;

            //    InterfaceController.InterfaceElement buttons = null;
            //    LevelManager.currentQueueIndex += 1;
            //    if (LevelManager.ArcadeQueue.Count > 1 && LevelManager.currentQueueIndex < LevelManager.ArcadeQueue.Count)
            //    {
            //        CoreHelper.Log($"Selecting next Arcade level in queue [{LevelManager.currentQueueIndex + 1} / {LevelManager.ArcadeQueue.Count}]");
            //        LevelManager.CurrentLevel = LevelManager.ArcadeQueue[LevelManager.currentQueueIndex];
            //        buttons = new InterfaceController.InterfaceElement(InterfaceController.InterfaceElement.Type.Buttons, (metadata.artist.getUrl() != null) ? "[NEXT]:next&&[TO ARCADE]:toarcade&&[REPLAY]:replay&&[MORE INFO]:end_of_level_more_info&&[GET SONG]:getsong" : "[NEXT]:next&&[TO ARCADE]:toarcade&&[REPLAY]:replay&&[MORE INFO]:end_of_level_more_info");
            //    }
            //    else
            //    {
            //        buttons = new InterfaceController.InterfaceElement(InterfaceController.InterfaceElement.Type.Buttons, (metadata.artist.getUrl() != null) ? "[TO ARCADE]:toarcade&&[REPLAY]:replay&&[MORE INFO]:end_of_level_more_info&&[GET SONG]:getsong" : "[TO ARCADE]:toarcade&&[REPLAY]:replay&&[MORE INFO]:end_of_level_more_info");
            //    }

            //    buttons.settings.Add("alignment", "center");
            //    buttons.settings.Add("orientation", "grid");
            //    buttons.settings.Add("width", "1");
            //    buttons.settings.Add("grid_h", "5");
            //    buttons.settings.Add("grid_v", "1");
            //    buttons.branch = "end_of_level";
            //    ic.interfaceBranches[endOfLevelIndex].elements[17] = buttons;
            //    var openLink = new InterfaceController.InterfaceElement(InterfaceController.InterfaceElement.Type.Event, "openlink::" + metadata.artist.getUrl());
            //    openLink.branch = "getsong";
            //    ic.interfaceBranches[getSongIndex].elements[0] = openLink;

            //    var interfaceBranch = new InterfaceController.InterfaceBranch("next");
            //    interfaceBranch.elements.Add(new InterfaceController.InterfaceElement(InterfaceController.InterfaceElement.Type.Event, "loadscene::Game::true", "next"));
            //    ic.interfaceBranches.Add(interfaceBranch);

            //    var interfaceBranch2 = new InterfaceController.InterfaceBranch("replay");
            //    interfaceBranch2.elements.Add(new InterfaceController.InterfaceElement(InterfaceController.InterfaceElement.Type.Event, "restartlevel", "replay"));
            //    ic.interfaceBranches.Add(interfaceBranch2);
            //}
            //ic.SwitchBranch("end_of_level");
        }


        public static bool fromLevel = false;

        public static void ReloadMenu()
        {
            if (fromLevel)
            {
                var menu = new GameObject("Arcade Menu System");
                Component component = ArcadeConfig.Instance.UseNewArcadeUI.Value ? menu.AddComponent<ArcadeMenuManager>() : menu.AddComponent<LevelMenuManager>();
            }
            else
            {
                var menu = new GameObject("Load Level System");
                menu.AddComponent<LoadLevelsManager>();
            }
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

            var sw = new System.Diagnostics.Stopwatch();
            sw.Start();
            currentlyLoading = true;
            fromLevel = false;
            ArcadeManager.inst.skippedLoad = false;
            ArcadeManager.inst.forcedSkip = false;
            LevelManager.IsArcade = true;

            if (!RTFile.DirectoryExists(RTFile.ApplicationDirectory + LevelManager.ListPath))
                Directory.CreateDirectory(RTFile.ApplicationDirectory + LevelManager.ListPath);

            var directories = Directory.GetDirectories(RTFile.ApplicationDirectory + LevelManager.ListPath, "*", SearchOption.TopDirectoryOnly);

            if (LoadLevelsManager.inst != null)
                LoadLevelsManager.totalLevelCount = directories.Length;

            LevelManager.Levels.Clear();
            LevelManager.ArcadeQueue.Clear();
            LevelManager.LoadProgress();

            var loadYieldMode = ArcadeConfig.Instance.LoadYieldMode.Value;

            for (int i = 0; i < directories.Length; i++)
            {
                var folder = directories[i];

                if (LoadLevelsManager.inst && LoadLevelsManager.inst.cancelled)
                {
                    SceneManager.inst.LoadScene("Input Select");
                    currentlyLoading = false;
                    yield break;
                }

                var path = folder.Replace("\\", "/");
                var name = Path.GetFileName(path);

                switch (loadYieldMode)
                {
                    case YieldType.Delay:
                        yield return new WaitForSeconds(delay);
                        delay += 0.0001f;
                        break;
                    case YieldType.Null:
                        yield return null;
                        break;
                    case YieldType.EndOfFrame:
                        yield return new WaitForEndOfFrame();
                        break;
                    case YieldType.FixedUpdate:
                        yield return new WaitForFixedUpdate();
                        break;
                }

                MetaData metadata = null;

                if (RTFile.FileExists($"{path}/metadata.vgm"))
                    metadata = MetaData.ParseVG(JSON.Parse(RTFile.ReadFromFile($"{path}/metadata.vgm")));
                else if (RTFile.FileExists($"{path}/metadata.lsb"))
                    metadata = MetaData.Parse(JSON.Parse(RTFile.ReadFromFile($"{path}/metadata.lsb")));

                if (metadata == null)
                {
                    if (LoadLevelsManager.inst)
                        LoadLevelsManager.inst.UpdateInfo(SteamWorkshop.inst.defaultSteamImageSprite, $"<color=$FF0000>No metadata in {name}</color>", i, true);

                    continue;
                }

                if (!RTFile.FileExists($"{path}/level.ogg") && !RTFile.FileExists($"{path}/level.wav") && !RTFile.FileExists($"{path}/level.mp3")
                    && !RTFile.FileExists($"{path}/audio.ogg") && !RTFile.FileExists($"{path}/audio.wav") && !RTFile.FileExists($"{path}/audio.mp3"))
                {
                    if (LoadLevelsManager.inst)
                        LoadLevelsManager.inst.UpdateInfo(SteamWorkshop.inst.defaultSteamImageSprite, $"<color=$FF0000>No song in {name}</color>", i, true);

                    continue;
                }

                if (!RTFile.FileExists($"{path}/level.lsb") && !RTFile.FileExists($"{path}/level.vgd"))
                {
                    if (LoadLevelsManager.inst)
                        LoadLevelsManager.inst.UpdateInfo(SteamWorkshop.inst.defaultSteamImageSprite, $"<color=$FF0000>No song in {name}</color>", i, true);

                    continue;
                }

                if ((string.IsNullOrEmpty(metadata.serverID) || metadata.serverID == "-1")
                    && (string.IsNullOrEmpty(metadata.LevelBeatmap.beatmap_id) && metadata.LevelBeatmap.beatmap_id == "-1" || metadata.LevelBeatmap.beatmap_id == "0")
                    && (string.IsNullOrEmpty(metadata.arcadeID) || metadata.arcadeID == "-1" || metadata.arcadeID == "0"))
                {
                    metadata.arcadeID = LSText.randomNumString(16);
                    var metadataJN = metadata.ToJSON();
                    RTFile.WriteToFile($"{path}/metadata.lsb", metadataJN.ToString(3));
                }

                var level = new Level(path + "/", metadata);

                if (LevelManager.Saves.Has(x => x.ID == level.id))
                    level.playerData = LevelManager.Saves.Find(x => x.ID == level.id);

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

            LevelManager.Sort((int)ArcadeConfig.Instance.LocalLevelOrderby.Value, ArcadeConfig.Instance.LocalLevelAscend.Value);

            SteamWorkshopManager.inst.Levels = LevelManager.SortLevels(SteamWorkshopManager.inst.Levels, (int)ArcadeConfig.Instance.SteamLevelOrderby.Value, ArcadeConfig.Instance.SteamLevelAscend.Value);
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
            }

            LSText.CopyToClipboard(jn.ToString());
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

                    var hasLocal = LevelManager.Levels.Has(x => x.id == jnQueue["id"]);
                    var hasSteam = SteamWorkshopManager.inst.Levels.Has(x => x.id == jnQueue["id"]);

                    if ((hasLocal || hasSteam) && !LevelManager.ArcadeQueue.Has(x => x.id == jnQueue["id"]))
                    {
                        var currentLevel = hasSteam ? SteamWorkshopManager.inst.Levels.Find(x => x.id == jnQueue["id"]) :
                            LevelManager.Levels.Find(x => x.id == jnQueue["id"]);

                        LevelManager.ArcadeQueue.Add(currentLevel);
                    }
                    else if (!hasLocal && !hasSteam)
                    {
                        CoreHelper.LogError($"Level with ID {jnQueue["id"]} does not currently exist in your Local folder / Steam subscribed items.");
                    }
                }

                if (ArcadeMenuManager.inst && ArcadeMenuManager.inst.CurrentTab == 4)
                {
                    if (ArcadeMenuManager.inst.queuePageField.text != "0")
                        ArcadeMenuManager.inst.queuePageField.text = "0";
                    else
                        CoreHelper.StartCoroutine(ArcadeMenuManager.inst.RefreshQueuedLevels());
                }
            }
            catch (Exception ex)
            {
                CoreHelper.LogError($"Pasted text was probably not in the correct format.\n{ex}");
            }

        }

        public static IEnumerator OnLoadingEnd()
        {
            yield return new WaitForSeconds(0.1f);
            AudioManager.inst.PlaySound("loadsound");
            var menu = new GameObject("Arcade Menu System");
            Component component = ArcadeConfig.Instance.UseNewArcadeUI.Value ? menu.AddComponent<ArcadeMenuManager>() : menu.AddComponent<LevelMenuManager>();

            yield break;
        }
    }
}
