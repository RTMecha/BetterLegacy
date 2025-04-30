using System;

using HarmonyLib;

using BetterLegacy.Arcade.Managers;
using BetterLegacy.Core;
using BetterLegacy.Core.Runtime;

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
        static bool updateShakePrefix() => false;

        [HarmonyPatch(nameof(EventManager.updateEvents), new[] { typeof(int) })]
        [HarmonyPrefix]
        static bool EventManagerUpdateEventsPrefix1(int __0)
        {
            RTLevel.Current?.UpdateEvents(__0);
            return false;
        }

        [HarmonyPatch(nameof(EventManager.updateEvents), new Type[] { })]
        [HarmonyPrefix]
        static bool EventManagerUpdateEventsPrefix2()
        {
            RTLevel.Current?.UpdateEvents();
            return false;
        }

    }
}
