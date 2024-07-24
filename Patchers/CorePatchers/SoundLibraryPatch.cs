using BetterLegacy.Core;
using BetterLegacy.Core.Managers.Networking;
using HarmonyLib;
using UnityEngine;

namespace BetterLegacy.Patchers
{
    [HarmonyPatch(typeof(SoundLibrary))]
    public class SoundLibraryPatch : MonoBehaviour
    {
        public static SoundLibrary Instance => AudioManager.inst.library;

        [HarmonyPatch(nameof(SoundLibrary.Awake))]
        [HarmonyPostfix]
        static void AwakePostfix(SoundLibrary __instance)
        {
            __instance.StartCoroutine(AlephNetworkManager.DownloadAudioClip($"file://{RTFile.ApplicationDirectory}{RTFile.BepInExAssetsPath}click cut.ogg", AudioType.OGGVORBIS, delegate (AudioClip audioClip)
            {
                __instance.soundClips["UpDown"][0] = audioClip;
                __instance.soundClips["LeftRight"][0] = audioClip;
            }));
            __instance.StartCoroutine(AlephNetworkManager.DownloadAudioClip($"file://{RTFile.ApplicationDirectory}{RTFile.BepInExAssetsPath}optionexit.ogg", AudioType.OGGVORBIS, delegate (AudioClip audioClip)
            {
                __instance.soundClips["Block"][0] = audioClip;
            }));
        }
    }
}
