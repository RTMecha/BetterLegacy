using System;

using UnityEngine;

using HarmonyLib;
using Steamworks;

using BetterLegacy.Core.Managers;

namespace BetterLegacy.Patchers
{
    [HarmonyPatch(typeof(SteamManager))]
    public class SteamManagerPatch
    {
        [HarmonyPatch(nameof(SteamManager.Awake))]
        [HarmonyPrefix]
        static bool AwakePrefix(SteamManager __instance)
        {
            try
            {
                if (SteamManager.s_instance != null)
                {
                    UnityEngine.Object.Destroy(__instance.gameObject);
                    return false;
                }
                SteamManager.s_instance = __instance;
                UnityObject.DontDestroyOnLoad(__instance.gameObject);
            }
            catch
            {
                Debug.LogError("[Steamworks.NET] failed");
            }
            RTSteamManager.Init();
            NetworkManager.Init();
            SteamLobbyManager.Init();
            return false;
        }
    }
}
