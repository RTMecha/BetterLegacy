﻿using BetterLegacy.Core;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Menus;
using HarmonyLib;
using LSFunctions;
using System.IO;
using UnityEngine;
using YamlDotNet.Serialization;

namespace BetterLegacy.Patchers
{
    [HarmonyPatch(typeof(InterfaceLoader))]
    public class InterfaceLoaderPatch
    {
        [HarmonyPatch(nameof(InterfaceLoader.Start))]
        [HarmonyPrefix]
        static bool InterfaceLoaderPrefix(InterfaceLoader __instance)
        {
            string text = "";
            string fileName = "";
            if (string.IsNullOrEmpty(__instance.file))
            {
                text = SaveManager.inst.CurrentStoryLevel.BeatmapJson.text;

                CoreHelper.UpdateDiscordStatus("Level: " + SaveManager.inst.CurrentStoryLevel.SongName, "Playing Story", "arcade");
            }
            else
            {
                if (__instance.isYAML)
                {
                    string text2 = (Resources.Load("terminal/" + __instance.location + "/" + __instance.file) as TextAsset).text;
                    object graph = new DeserializerBuilder().Build().Deserialize(new StringReader(text2));
                    text = new SerializerBuilder().JsonCompatible().Build().Serialize(graph);
                    LSText.CopyToClipboard(text);
                }
                else if (__instance.gameObject.scene.name == "Main Menu" && (RTFile.FileExists(RTFile.ApplicationDirectory + "beatmaps/menus/main/menu.lsm") || RTFile.FileExists(RTFile.ApplicationDirectory + "beatmaps/menus/main.lsm")))
                {
                    fileName = "Main Menu";
                    if (RTFile.FileExists(RTFile.ApplicationDirectory + "beatmaps/menus/main.lsm"))
                    {
                        text = FileManager.inst.LoadJSONFileRaw(RTFile.ApplicationDirectory + "beatmaps/menus/main.lsm");
                        MenuManager.currentInterface = "beatmaps/menus/main.lsm";
                    }
                    else if (RTFile.FileExists(RTFile.ApplicationDirectory + "beatmaps/menus/main/menu.lsm"))
                    {
                        text = FileManager.inst.LoadJSONFileRaw(RTFile.ApplicationDirectory + "beatmaps/menus/main/menu.lsm");
                        MenuManager.currentInterface = "beatmaps/menus/main/menu.lsm";
                    }
                }
                else if (__instance.gameObject.scene.name == "Game" && (RTFile.FileExists(RTFile.ApplicationDirectory + "beatmaps/menus/pause/menu.lsm") || RTFile.FileExists(RTFile.ApplicationDirectory + "beatmaps/menus/pause.lsm")))
                {
                    if (RTFile.FileExists(RTFile.ApplicationDirectory + "beatmaps/menus/pause.lsm"))
                    {
                        text = FileManager.inst.LoadJSONFileRaw(RTFile.ApplicationDirectory + "beatmaps/menus/pause.lsm");
                        MenuManager.currentInterface = "beatmaps/menus/pause.lsm";
                    }
                    else if (RTFile.FileExists(RTFile.ApplicationDirectory + "beatmaps/menus/pause/menu.lsm"))
                    {
                        text = FileManager.inst.LoadJSONFileRaw(RTFile.ApplicationDirectory + "beatmaps/menus/pause/menu.lsm");
                        MenuManager.currentInterface = "beatmaps/menus/pause/menu.lsm";
                    }
                }
                else if (__instance.gameObject.scene.name == "Interface" && RTFile.FileExists(RTFile.ApplicationDirectory + "beatmaps/menus/story_mode.lsm"))
                {
                    fileName = "Interface";
                    if (RTFile.FileExists(RTFile.ApplicationDirectory + "beatmaps/menus/story_mode.lsm"))
                    {
                        text = FileManager.inst.LoadJSONFileRaw(RTFile.ApplicationDirectory + "beatmaps/menus/story_mode.lsm");
                        MenuManager.currentInterface = "beatmaps/menus/story_mode.lsm";
                    }
                }
                else
                {
                    text = (Resources.Load("terminal/" + __instance.location + "/" + __instance.file) as TextAsset).text;
                }

                CoreHelper.UpdateDiscordStatus($"Navigating {fileName}", "In Menu", "menu");
            }

            if (CoreHelper.CurrentSceneType == SceneType.Editor || !MenuManager.fromPageLevel || string.IsNullOrEmpty(MenuManager.prevBranch))
            {
                MenuManager.inst.loadingFromInterfaceLoader = true;
                __instance.terminal.GetComponent<InterfaceController>().ParseLilScript(text);
            }

            if (CoreHelper.CurrentSceneType == SceneType.Interface)
                MenuManager.fromPageLevel = false;

            InputDataManager.inst.playersCanJoin = __instance.playersCanJoin;
            return false;
        }

    }
}
