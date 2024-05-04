using BetterLegacy.Configs;
using HarmonyLib;
using UnityEngine;

namespace BetterLegacy.Patchers
{
    [HarmonyPatch(typeof(DiscordController))]
    public class DiscordControllerPatch : MonoBehaviour
    {
        [HarmonyPatch("Awake")]
        [HarmonyPrefix]
        static void AwakePrefix(DiscordController __instance)
        {
            __instance.applicationId = CoreConfig.Instance.DiscordRichPresenceID.Value;
        }

        [HarmonyPatch("Awake")]
        [HarmonyPostfix]
        static void AwakePostfix(DiscordController __instance)
        {
            __instance.OnArtChange("pa_logo_white");
        }

        [HarmonyPatch("OnArtChange")]
        [HarmonyPrefix]
        static bool OnArtChangePrefix(DiscordController __instance, string _art)
        {
            __instance.presence.largeImageKey = _art;
            return false;
        }

        [HarmonyPatch("OnIconChange")]
        [HarmonyPrefix]
        static bool OnIconChangePrefix(DiscordController __instance, string _icon)
        {
            __instance.presence.smallImageKey = _icon;
            return false;
        }

        [HarmonyPatch("OnStateChange")]
        [HarmonyPrefix]
        static bool OnStateChangePrefix(DiscordController __instance, string _state)
        {
            __instance.presence.state = _state;
            DiscordRpc.UpdatePresence(__instance.presence);
            return false;
        }

        [HarmonyPatch("OnDetailsChange")]
        [HarmonyPrefix]
        static bool OnDetailsChangePrefix(DiscordController __instance, string _details)
        {
            __instance.presence.details = _details;
            DiscordRpc.UpdatePresence(__instance.presence);
            return false;
        }
    }
}
