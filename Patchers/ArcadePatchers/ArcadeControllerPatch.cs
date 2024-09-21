
using BetterLegacy.Core;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using HarmonyLib;

namespace BetterLegacy.Patchers
{
    [HarmonyPatch(typeof(ArcadeController))]
    public class ArcadeControllerPatch
    {
        [HarmonyPatch(nameof(ArcadeController.Start))]
        [HarmonyPrefix]
        static bool StartPrefix(ArcadeController __instance)
        {
            CoreHelper.Log("Trying to generate new arcade UI...");

            if (ArcadeHelper.buttonPrefab == null)
            {
                ArcadeHelper.buttonPrefab = __instance.ic.ButtonPrefab.Duplicate(null);
                UnityEngine.Object.DontDestroyOnLoad(ArcadeHelper.buttonPrefab);
            }

            InputDataManager.inst.playersCanJoin = false;
            ArcadeHelper.ReloadMenu();

            return false;
        }
    }
}
