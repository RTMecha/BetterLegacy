using BetterLegacy.Core.Managers;
using HarmonyLib;
using System.Collections;
using UnityEngine;

namespace BetterLegacy.Patchers
{
    [HarmonyPatch(typeof(AudioManager))]
    public class AudioManagerPatch
    {
        [HarmonyPatch(nameof(AudioManager.Update))]
        [HarmonyPrefix]
        static bool UpdatePrefix(AudioManager __instance)
        {
            float masterVol = (float)DataManager.inst.GetSettingInt("MasterVolume", 9) / 9f;

            if (RTEventManager.inst != null)
                __instance.masterVol = masterVol * RTEventManager.inst.audioVolume;
            else
                __instance.masterVol = masterVol;

            __instance.musicVol = (float)DataManager.inst.GetSettingInt("MusicVolume", 9) / 9f * __instance.masterVol;
            __instance.sfxVol = (float)DataManager.inst.GetSettingInt("EffectsVolume", 9) / 9f * __instance.masterVol;
            if (!__instance.isFading)
            {
                __instance.musicSources[__instance.activeMusicSourceIndex].volume = __instance.musicVol;
            }
            __instance.musicSources[__instance.activeMusicSourceIndex].pitch = __instance.pitch;

            return false;
        }

        [HarmonyPatch(nameof(AudioManager.SetPitch))]
        [HarmonyPrefix]
        static bool SetPitchPrefix(AudioManager __instance, float __0)
        {
            if (RTEventManager.inst)
                RTEventManager.inst.pitchOffset = __0;
            else
                __instance.pitch = __0;

            return false;
        }

        [HarmonyPatch(nameof(AudioManager.SetMusicTime))]
        [HarmonyPrefix]
        static bool SetMusicTimePrefix(AudioManager __instance, float __0)
        {
            if (__instance.CurrentAudioSource.clip == null)
                return false;
            
            __instance.CurrentAudioSource.time = Mathf.Clamp(__0, 0f, AudioManager.inst.CurrentAudioSource.clip.length);
            return false;
        }

        [HarmonyPatch(nameof(AudioManager.animateMusicFade))]
        [HarmonyPrefix]
        static bool animateMusicFadePrefix(AudioManager __instance, ref IEnumerator __result, float __0)
        {
            __result = AnimateMusicFade(__instance, __0);
            return false;
        }

        static IEnumerator AnimateMusicFade(AudioManager __instance, float duration)
        {
            __instance.isFading = true;
            float percent = 0f;
            while (percent < 1f)
            {
                percent += Time.deltaTime * 1f / duration;
                __instance.musicSources[__instance.activeMusicSourceIndex].volume = Mathf.Lerp(0f, __instance.musicVol, percent);
                __instance.musicSources[1 - __instance.activeMusicSourceIndex].volume = Mathf.Lerp(__instance.musicVol, 0f, percent);
                yield return null;
            }
            __instance.isFading = false;
            __instance.musicSources[1 - __instance.activeMusicSourceIndex].clip = null; // clear clip from memory
            yield break;
        }
    }
}