using BetterLegacy.Core.Managers;
using HarmonyLib;
using System;

namespace BetterLegacy.Patchers
{
    [HarmonyPatch(typeof(EventManager))]
    class EventManagerPatch
    {
        [HarmonyPatch("Update")]
        [HarmonyPrefix]
        static bool EventManagerUpdatePrefix()
        {
            return false;
        }

        [HarmonyPatch("LateUpdate")]
        [HarmonyPrefix]
        static bool EventManagerLateUpdatePrefix()
        {
            return false;
        }

        [HarmonyPatch(typeof(EventManager), "updateShake")]
        [HarmonyPrefix]
        static bool EventManagerShakePrefix()
        {
            RTEventManager.inst.updateShake();
            return false;
        }

        [HarmonyPatch(typeof(EventManager), "updateEvents", new[] { typeof(int) })]
        [HarmonyPrefix]
        static bool EventManagerUpdateEventsPrefix1(int __0)
        {
            RTEventManager.inst.updateEvents(__0);
            return false;
        }

        [HarmonyPatch(typeof(EventManager), "updateEvents", new Type[] { })]
        [HarmonyPrefix]
        static bool EventManagerUpdateEventsPrefix2(EventManager __instance)
        {
            __instance.StartCoroutine(RTEventManager.inst.updateEvents());

            return false;
        }

    }
}
