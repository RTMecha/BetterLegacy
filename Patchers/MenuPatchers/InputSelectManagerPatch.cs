using UnityEngine;

using HarmonyLib;

namespace BetterLegacy.Patchers
{
    [HarmonyPatch(typeof(InputSelectManager))]
    public class InputSelectManagerPatch : MonoBehaviour
    {
        [HarmonyPatch(nameof(InputSelectManager.Start))]
        [HarmonyPrefix]
        static bool StartPrefix() => false;

        [HarmonyPatch(nameof(InputSelectManager.Update))]
        [HarmonyPrefix]
        static bool UpdatePrefix() => false;

        [HarmonyPatch(nameof(InputSelectManager.LoadLevel))]
        [HarmonyPrefix]
        static bool LoadLevelPrefix() => false;
    }
}
