using HarmonyLib;

using BetterLegacy.Core.Helpers;

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
