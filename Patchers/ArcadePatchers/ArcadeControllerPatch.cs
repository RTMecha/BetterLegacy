
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
        static bool StartPrefix()
        {
            CoreHelper.Log("Trying to generate new arcade UI...");

            InputDataManager.inst.playersCanJoin = false;
            ArcadeHelper.ReloadMenu();

            return false;
        }
    }
}
