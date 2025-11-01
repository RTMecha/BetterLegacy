using UnityEngine;

using HarmonyLib;

using BetterLegacy.Core.Managers;

namespace BetterLegacy.Patchers
{
    [HarmonyPatch(typeof(SteamManager))]
    public class SteamManagerPatch : MonoBehaviour
    {
        [HarmonyPatch(nameof(SteamManager.Awake))]
        [HarmonyPostfix]
        static void AwakePostfix() => RTSteamManager.Init();
    }
}
