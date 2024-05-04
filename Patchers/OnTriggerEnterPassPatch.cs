
using HarmonyLib;
using UnityEngine;

namespace BetterLegacy.Patchers
{
    [HarmonyPatch(typeof(OnTriggerEnterPass))]
    public class OnTriggerEnterPassPatch : MonoBehaviour
    {
        [HarmonyPatch("Start")]
        [HarmonyPrefix]
        static bool TriggerPassPrefix(OnTriggerEnterPass __instance)
        {
            return false;
        }

        [HarmonyPatch("OnTriggerEnter2D")]
        [HarmonyPrefix]
        static bool OnTriggerEnter2DPrefix(OnTriggerEnterPass __instance, Collider2D __0)
        {
            return false;
        }

        [HarmonyPatch("OnTriggerEnter")]
        [HarmonyPrefix]
        static bool OnTriggerEnterPrefix(OnTriggerEnterPass __instance, Collider __0)
        {
            return false;
        }

        [HarmonyPatch("OnTriggerStay2D")]
        [HarmonyPrefix]
        static bool OnTriggerStay2DPrefix(OnTriggerEnterPass __instance, Collider2D __0)
        {
            return false;
        }

        [HarmonyPatch("OnTriggerStay")]
        [HarmonyPrefix]
        static bool OnTriggerStayPrefix(OnTriggerEnterPass __instance, Collider __0)
        {
            return false;
        }
    }
}
