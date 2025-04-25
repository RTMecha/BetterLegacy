using HarmonyLib;

namespace BetterLegacy.Patchers
{
    [HarmonyPatch(typeof(InterfaceLoader))]
    public class InterfaceLoaderPatch
    {
        [HarmonyPatch(nameof(InterfaceLoader.Start))]
        [HarmonyPrefix]
        static bool StartPrefix() => false;
    }
}
