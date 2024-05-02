using BetterLegacy.Core;
using BetterLegacy.Menus;
using HarmonyLib;
using LSFunctions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using YamlDotNet.Serialization;

namespace BetterLegacy.Patchers
{
    [HarmonyPatch(typeof(InterfaceLoader))]
    public class InterfaceLoaderPatch
    {
        [HarmonyPatch("Start")]
        [HarmonyPrefix]
        static bool InterfaceLoaderPrefix(InterfaceLoader __instance)
        {
            string text = "";
            if (string.IsNullOrEmpty(__instance.file))
            {
                text = SaveManager.inst.CurrentStoryLevel.BeatmapJson.text;
                DiscordController.inst.OnDetailsChange("Playing Story");
                DiscordController.inst.OnStateChange("Level: " + SaveManager.inst.CurrentStoryLevel.SongName);
                DiscordController.inst.OnIconChange("arcade");
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
                    if (RTFile.FileExists(RTFile.ApplicationDirectory + "beatmaps/menus/main.lsm"))
                    {
                        text = FileManager.inst.LoadJSONFileRaw(RTFile.ApplicationDirectory + "beatmaps/menus/main.lsm");
                        MenuManager.prevInterface = "beatmaps/menus/main.lsm";
                    }
                    else if (RTFile.FileExists(RTFile.ApplicationDirectory + "beatmaps/menus/main/menu.lsm"))
                    {
                        text = FileManager.inst.LoadJSONFileRaw(RTFile.ApplicationDirectory + "beatmaps/menus/main/menu.lsm");
                        MenuManager.prevInterface = "beatmaps/menus/main/menu.lsm";
                    }
                }
                else if (__instance.gameObject.scene.name == "Game" && (RTFile.FileExists(RTFile.ApplicationDirectory + "beatmaps/menus/pause/menu.lsm") || RTFile.FileExists(RTFile.ApplicationDirectory + "beatmaps/menus/pause.lsm")))
                {
                    if (RTFile.FileExists(RTFile.ApplicationDirectory + "beatmaps/menus/pause.lsm"))
                    {
                        text = FileManager.inst.LoadJSONFileRaw(RTFile.ApplicationDirectory + "beatmaps/menus/pause.lsm");
                        MenuManager.prevInterface = "beatmaps/menus/pause.lsm";
                    }
                    else if (RTFile.FileExists(RTFile.ApplicationDirectory + "beatmaps/menus/pause/menu.lsm"))
                    {
                        text = FileManager.inst.LoadJSONFileRaw(RTFile.ApplicationDirectory + "beatmaps/menus/pause/menu.lsm");
                        MenuManager.prevInterface = "beatmaps/menus/pause/menu.lsm";
                    }
                }
                else if (__instance.gameObject.scene.name == "Interface" && RTFile.FileExists(RTFile.ApplicationDirectory + "beatmaps/menus/story_mode.lsm"))
                {
                    if (RTFile.FileExists(RTFile.ApplicationDirectory + "beatmaps/menus/story_mode.lsm"))
                    {
                        text = FileManager.inst.LoadJSONFileRaw(RTFile.ApplicationDirectory + "beatmaps/menus/story_mode.lsm");
                        MenuManager.prevInterface = "beatmaps/menus/story_mode.lsm";
                    }
                }
                else
                {
                    text = (Resources.Load("terminal/" + __instance.location + "/" + __instance.file) as TextAsset).text;
                }

                DiscordController.inst.OnDetailsChange("In Menu");
                DiscordController.inst.OnStateChange("");
                DiscordController.inst.OnIconChange("");
            }

            __instance.terminal.GetComponent<InterfaceController>().ParseLilScript(text);
            InputDataManager.inst.playersCanJoin = __instance.playersCanJoin;
            return false;
        }

    }
}
