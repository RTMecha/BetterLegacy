﻿using BetterLegacy.Core.Managers.Networking;
using HarmonyLib;

namespace BetterLegacy.Patchers
{
    [HarmonyPatch(typeof(SteamWrapper.Achievements))]
    public class SteamWrapperAchievementsPatch
    {
        [HarmonyPatch("SetAchievement")]
        [HarmonyPrefix]
        static bool SetAchievementPrefix(string __0)
        {
            if (!SteamWorkshopManager.inst || !SteamWorkshopManager.inst.Initialized)
                return false;

            SteamWorkshopManager.inst.steamUser.SetAchievement(__0);
            return false;
        }

        [HarmonyPatch("GetAchievement")]
        [HarmonyPrefix]
        static bool GetAchievementPrefix(ref bool __result, string __0)
        {
            __result = SteamWorkshopManager.inst.steamUser.GetAchievement(__0);
            return false;
        }

        [HarmonyPatch("ClearAchievement")]
        [HarmonyPrefix]
        static bool ClearAchievementPrefix(string __0)
        {
            if (!SteamWorkshopManager.inst || !SteamWorkshopManager.inst.Initialized)
                return false;

            SteamWorkshopManager.inst.steamUser.ClearAchievement(__0);
            return false;
        }
    }
}
