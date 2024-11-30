using BetterLegacy.Configs;
using BetterLegacy.Core.Helpers;
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

        [HarmonyPatch(nameof(DiscordController.Start))]
        [HarmonyPostfix]
        static void StartPostfix(DiscordController __instance)
        {
            __instance.presence.largeImageText = "Using the BetterLegacy mod";
            __instance.presence.startTimestamp = 1;
            DiscordRpc.UpdatePresence(__instance.presence);
        }

        [HarmonyPatch(nameof(DiscordController.OnArtChange))]
        [HarmonyPrefix]
        static bool OnArtChangePrefix(DiscordController __instance, string _art)
        {
            __instance.presence.largeImageKey = _art;
            DiscordRpc.UpdatePresence(__instance.presence);
            return false;
        }

        [HarmonyPatch(nameof(DiscordController.OnIconChange))]
        [HarmonyPrefix]
        static bool OnIconChangePrefix(DiscordController __instance, string _icon)
        {
            __instance.presence.smallImageKey = _icon;
            DiscordRpc.UpdatePresence(__instance.presence);
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

        [HarmonyPatch(nameof(DiscordController.ReadyCallback))]
        [HarmonyPrefix]
        static bool ReadyCallbackPrefix(DiscordController __instance)
        {
            __instance.callbackCalls++;
            __instance.Initialized = true;
            Debug.Log($"{__instance.className}Discord: ready");
            CoreHelper.UpdateDiscordStatus(CoreHelper.discordLevel, CoreHelper.discordDetails, CoreHelper.discordIcon, CoreHelper.discordArt);
            __instance.onConnect.Invoke();
            return false;
        }
    }
}
