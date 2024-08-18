using BetterLegacy.Arcade;
using BetterLegacy.Configs;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers.Networking;
using LSFunctions;
using SimpleJSON;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using UnityEngine;
using CielaSpike;

namespace BetterLegacy.Core.Managers
{
    public class RTArcade : MonoBehaviour
    {
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
            DataManager.inst.UpdateSettingBool("IsArcade", true);

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
