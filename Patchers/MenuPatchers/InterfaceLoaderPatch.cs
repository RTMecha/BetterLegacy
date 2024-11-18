using BetterLegacy.Core;
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
            if (__instance.gameObject.scene.name == "Main Menu" || __instance.gameObject.scene.name == "Interface")
            {
                return false;
            }

            string text;
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
                else
                    text = (Resources.Load("terminal/" + __instance.location + "/" + __instance.file) as TextAsset).text;
            }

            __instance.terminal.GetComponent<InterfaceController>().ParseLilScript(text);

            InputDataManager.inst.playersCanJoin = __instance.playersCanJoin;
            return false;
        }
    }
}
