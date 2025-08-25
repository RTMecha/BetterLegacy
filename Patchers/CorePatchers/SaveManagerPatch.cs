using UnityEngine;

using HarmonyLib;

using SimpleJSON;

using BetterLegacy.Configs;
using BetterLegacy.Core;
using BetterLegacy.Core.Helpers;

namespace BetterLegacy.Patchers
{
    [HarmonyPatch(typeof(SaveManager))]
    public class SaveManagerPatch
    {
        [HarmonyPatch(nameof(SaveManager.Start))]
        [HarmonyPrefix]
        static bool StartPrefix()
        {
            ProjectArrhythmia.Window.ApplySettings();
            return false;
        }

        [HarmonyPatch(nameof(SaveManager.ApplySettingsFile))]
        [HarmonyPrefix]
        static bool ApplySettingsFilePrefix() => false;

        [HarmonyPatch(nameof(SaveManager.ApplyVideoSettings))]
        [HarmonyPrefix]
        static bool ApplyVideoSettingsPrefix()
        {
            ProjectArrhythmia.Window.ApplySettings();
            return false;
        }

        [HarmonyPatch(nameof(SaveManager.OnApplicationQuit))]
        [HarmonyPrefix]
        static bool OnApplicationQuitPrefix()
        {
            DiscordController.inst.OnDisableDiscord();
            Debug.Log("Run Quit Function");
            PlayerPrefs.DeleteAll();
            return false;
        }

        [HarmonyPatch(nameof(SaveManager.LoadSavesFile))]
        [HarmonyPrefix]
        static bool LoadSavesFilePrefix() => false;
    }
}
