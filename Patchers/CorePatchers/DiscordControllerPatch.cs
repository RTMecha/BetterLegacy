using BetterLegacy.Configs;
using HarmonyLib;
using UnityEngine;

namespace BetterLegacy.Patchers
{
    [HarmonyPatch(typeof(DiscordController))]
    public class DiscordControllerPatch : MonoBehaviour
    {
        [HarmonyPatch(nameof(DiscordController.Awake))]
        [HarmonyPrefix]
        static void AwakePrefix(DiscordController __instance) => __instance.applicationId = CoreConfig.Instance.DiscordRichPresenceID.Value;

        [HarmonyPatch(nameof(DiscordController.Awake))]
        [HarmonyPostfix]
        static void AwakePostfix(DiscordController __instance) => __instance.OnArtChange("pa_logo_white"); // fixes the logo being incorrect
    
        [HarmonyPatch(nameof(DiscordController.OnArtChange))]
        [HarmonyPrefix]
        static bool OnArtChangePrefix(DiscordController __instance, string _art)
        {
            __instance.presence.largeImageKey = _art;
            return false;
        }

        [HarmonyPatch(nameof(DiscordController.OnIconChange))]
        [HarmonyPrefix]
        static bool OnIconChangePrefix(DiscordController __instance, string _icon)
        {
            __instance.presence.smallImageKey = _icon;
            return false;
        }

        [HarmonyPatch(nameof(DiscordController.OnStateChange))]
        [HarmonyPrefix]
        static bool OnStateChangePrefix(DiscordController __instance, string _state)
        {
            __instance.presence.state = _state;
            DiscordRpc.UpdatePresence(__instance.presence);
            return false;
        }

        [HarmonyPatch(nameof(DiscordController.OnDetailsChange))]
        [HarmonyPrefix]
        static bool OnDetailsChangePrefix(DiscordController __instance, string _details)
        {
            __instance.presence.details = _details;
            DiscordRpc.UpdatePresence(__instance.presence);
            return false;
        }
    }
}
