using BetterLegacy.Core.Managers;
using HarmonyLib;
using System;

namespace BetterLegacy.Patchers
{
    [HarmonyPatch(typeof(EventManager))]
    public class EventManagerPatch
    {
        [HarmonyPatch(nameof(EventManager.Update))]
        [HarmonyPrefix]
        static bool EventManagerUpdatePrefix() => false;

        [HarmonyPatch(nameof(EventManager.LateUpdate))]
        [HarmonyPrefix]
        static bool EventManagerLateUpdatePrefix() => false;

        [HarmonyPatch(nameof(EventManager.updateShake))]
        [HarmonyPrefix]
        static bool updateShakePrefix()
        {
            RTEventManager.inst.UpdateShake();
            return false;
        }

        [HarmonyPatch(nameof(EventManager.updateEvents), new[] { typeof(int) })]
        [HarmonyPrefix]
        static bool EventManagerUpdateEventsPrefix1(int __0)
        {
            RTEventManager.inst.UpdateEvents(__0);
            return false;
        }

        [HarmonyPatch(nameof(EventManager.updateEvents), new Type[] { })]
        [HarmonyPrefix]
        static bool EventManagerUpdateEventsPrefix2(EventManager __instance)
        {
            __instance.StartCoroutine(RTEventManager.inst.UpdateEvents());

            return false;
        }

    }
}
