using BetterLegacy.Arcade.Managers;
using BetterLegacy.Core.Managers;
using HarmonyLib;
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
            __instance.masterVol = (float)DataManager.inst.GetSettingInt("MasterVolume", 9) / 9f;

            __instance.musicVol = (float)DataManager.inst.GetSettingInt("MusicVolume", 9) / 9f * __instance.masterVol * (RTEventManager.inst ? RTEventManager.inst.audioVolume : 1f) * SoundManager.musicVolume;
            __instance.sfxVol = (float)DataManager.inst.GetSettingInt("EffectsVolume", 9) / 9f * __instance.masterVol;
            if (!__instance.isFading)
                __instance.CurrentAudioSource.volume = __instance.musicVol;
            __instance.CurrentAudioSource.pitch = __instance.pitch;
            if (__instance.pitch != prevPitch)
            {
                prevPitch = __instance.pitch;

                try
                {
                    if (RTVideoManager.inst.videoPlayer.enabled)
                        RTVideoManager.inst?.UpdateVideo();
                }
                catch
                {

                }
            }

            return false;
        }

        static float prevPitch = 1f;

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
            
            __instance.CurrentAudioSource.time = Mathf.Clamp(__0, 0f, __instance.CurrentAudioSource.clip.length);

            try
            {
                if (RTVideoManager.inst.videoPlayer.enabled)
                    RTVideoManager.inst?.UpdateVideo();
            }
            catch
            {
                
            }
            return false;
        }
    }
}