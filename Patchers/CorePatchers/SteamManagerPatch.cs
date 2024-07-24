using BetterLegacy.Core.Managers.Networking;
using HarmonyLib;

using UnityEngine;

namespace BetterLegacy.Patchers
{
    [HarmonyPatch(typeof(SteamManager))]
    public class SteamManagerPatch : MonoBehaviour
    {
        [HarmonyPatch(nameof(SteamManager.Awake))]
        [HarmonyPostfix]
        static void AwakePostfix(SteamManager __instance) => SteamWorkshopManager.Init(__instance);
    }
}
