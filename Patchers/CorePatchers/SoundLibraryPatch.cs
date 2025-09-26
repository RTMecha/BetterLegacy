using UnityEngine;

using HarmonyLib;

using BetterLegacy.Core;
using BetterLegacy.Core.Data;

namespace BetterLegacy.Patchers
{
    [HarmonyPatch(typeof(SoundLibrary))]
    public class SoundLibraryPatch
    {
        public static SoundLibrary inst;

        public static string SFXPath => RTFile.GetAsset($"core/sfx/");

        static bool ran = false;

        [HarmonyPatch(nameof(SoundLibrary.Awake))]
        [HarmonyPrefix]
        static bool AwakePrefix(SoundLibrary __instance)
        {
            inst = __instance;
            if (ran)
                return false; // only run once due to Awake being run every time a scene is loaded.
            ran = true;

            //foreach (var soundGroup in __instance.soundGroups)
            //    __instance.soundClips[soundGroup.soundID] = soundGroup.group;

            //var quickSounds = Resources.LoadAll<AudioClip>("terminal/quick-sounds");
            //if (quickSounds != null)
            //    foreach (var audioClip in quickSounds)
            //        __instance.soundClips["qe_" + audioClip.name] = new AudioClip[] { audioClip };

            LegacyResources.LoadSounds();
            LegacyResources.LoadMusic();

            return false;
        }
        
        [HarmonyPatch(nameof(SoundLibrary.GetClipFromName))]
        [HarmonyPrefix]
        static bool GetClipFromNamePrefix(SoundLibrary __instance, ref AudioClip __result, string __0)
        {
            if (LegacyResources.soundClips.TryFind(x => x.id == __0, out SoundGroup soundGroup))
            {
                __result = soundGroup.GetClip();
                return false;
            }

            Debug.LogError($"{__instance.className}Sound Clip not found -> [{__0}]");
            __result = null;
            return false;
        }

        [HarmonyPatch(nameof(SoundLibrary.GetMusicFromName))]
        [HarmonyPrefix]
        static bool GetMusicFromNamePrefix(SoundLibrary __instance, ref AudioClip __result, string __0)
        {
            // Optimized method by calling TryGetValue instead of ContainsKey.
            if (LegacyResources.musicClips.TryFind(x => x.id == __0, out MusicGroup musicGroup))
            {
                __result = musicGroup.GetClip();
                return false;
            }

            Debug.LogError($"{__instance.className}Music Clip not found -> [{__0}]");
            __result = null;
            return false;
        }
    }
}
