using HarmonyLib;

using BetterLegacy.Core.Managers;

namespace BetterLegacy.Patchers
{
    [HarmonyPatch(typeof(SteamWrapper.Achievements))]
    public class SteamWrapperAchievementsPatch
    {
        [HarmonyPatch(nameof(SteamWrapper.Achievements.SetAchievement))]
        [HarmonyPrefix]
        static bool SetAchievementPrefix(string __0)
        {
            if (!RTSteamManager.inst || !RTSteamManager.inst.Initialized)
                return false;

            RTSteamManager.inst.steamUser.SetAchievement(__0);
            return false;
        }

        [HarmonyPatch(nameof(SteamWrapper.Achievements.GetAchievement))]
        [HarmonyPrefix]
        static bool GetAchievementPrefix(ref bool __result, string __0)
        {
            __result = RTSteamManager.inst.steamUser.GetAchievement(__0);
            return false;
        }

        [HarmonyPatch(nameof(SteamWrapper.Achievements.ClearAchievement))]
        [HarmonyPrefix]
        static bool ClearAchievementPrefix(string __0)
        {
            if (!RTSteamManager.inst || !RTSteamManager.inst.Initialized)
                return false;

            RTSteamManager.inst.steamUser.ClearAchievement(__0);
            return false;
        }
    }
}
