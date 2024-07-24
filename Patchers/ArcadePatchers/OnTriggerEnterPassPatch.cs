
using HarmonyLib;
using UnityEngine;

namespace BetterLegacy.Patchers
{
    [HarmonyPatch(typeof(OnTriggerEnterPass))]
    public class OnTriggerEnterPassPatch
    {
        [HarmonyPatch(nameof(OnTriggerEnterPass.Start))]
        [HarmonyPrefix]
        static bool TriggerPassPrefix() => false;

        [HarmonyPatch(nameof(OnTriggerEnterPass.OnTriggerEnter2D))]
        [HarmonyPrefix]
        static bool OnTriggerEnter2DPrefix() => false;

        [HarmonyPatch(nameof(OnTriggerEnterPass.OnTriggerEnter))]
        [HarmonyPrefix]
        static bool OnTriggerEnterPrefix() => false;

        [HarmonyPatch(nameof(OnTriggerEnterPass.OnTriggerStay2D))]
        [HarmonyPrefix]
        static bool OnTriggerStay2DPrefix() => false;

        [HarmonyPatch(nameof(OnTriggerEnterPass.OnTriggerStay))]
        [HarmonyPrefix]
        static bool OnTriggerStayPrefix() => false;
    }
}
