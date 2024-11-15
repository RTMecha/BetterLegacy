﻿using BetterLegacy.Core;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers.Networking;
using HarmonyLib;
using System.Linq;
using UnityEngine;

namespace BetterLegacy.Patchers
{
    [HarmonyPatch(typeof(SoundLibrary))]
    public class SoundLibraryPatch
    {
        public static SoundLibrary Instance => AudioManager.inst.library;

        public static string SFXPath => $"{RTFile.ApplicationDirectory}{RTFile.BepInExAssetsPath}SFX/";

        static bool ran = false;

        [HarmonyPatch(nameof(SoundLibrary.Awake))]
        [HarmonyPrefix]
        static bool AwakePrefix(SoundLibrary __instance)
        {
            if (ran)
                return false; // only run once due to Awake being run every time a scene is loaded.
            ran = true;

            var ost = AssetBundle.LoadFromFile($"{RTFile.ApplicationDirectory}{RTFile.BepInExAssetsPath}ost.asset");

            foreach (var asset in ost.GetAllAssetNames())
            {
                var assetName = asset.Replace("assets/ost/", "");
                var music = ost.LoadAsset<AudioClip>(assetName);

                if (music == null)
                {
                    Debug.LogError($"{__instance.className}The music ({assetName}) does not exist in the asset bundle for some reason.");
                    continue;
                }

                var name = GetMusicName(assetName);
                var groupName = GetGroupName(assetName);

                music.name = name;

                __instance.musicClips[groupName] = new AudioClip[] { music };
            }
            ost.Unload(false);

            foreach (var soundGroup in __instance.soundGroups)
                __instance.soundClips[soundGroup.soundID] = soundGroup.group;

            var quickSounds = Resources.LoadAll<AudioClip>("terminal/quick-sounds");
            if (quickSounds != null)
                foreach (var audioClip in quickSounds)
                    __instance.soundClips["qe_" + audioClip.name] = new AudioClip[] { audioClip };

            AddSound(Example.ExampleManager.SpeakPath, "example_speak");
            AddSound($"{SFXPath}anna speak.ogg", "anna_speak");
            AddSound($"{SFXPath}hal speak.ogg", "hal_speak");

            foreach (var musicGroup in __instance.musicGroups)
            {
                if (musicGroup.music.Length > 1 && !musicGroup.alwaysRandom) // not alwaysRandom is apparently ACTUALLY RANDOM???
                    __instance.musicClipsRandomIndex[musicGroup.musicID] = Random.Range(0, musicGroup.music.Length);
                __instance.musicClips[musicGroup.musicID] = musicGroup.music;
            }

            __instance.StartCoroutine(AlephNetworkManager.DownloadAudioClip($"file://{SFXPath}click cut.ogg", AudioType.OGGVORBIS, audioClip =>
            {
                __instance.soundClips["UpDown"][0] = audioClip;
                __instance.soundClips["LeftRight"][0] = audioClip;
            }));
            __instance.StartCoroutine(AlephNetworkManager.DownloadAudioClip($"file://{SFXPath}optionexit.ogg", AudioType.OGGVORBIS, audioClip =>
            {
                __instance.soundClips["Block"][0] = audioClip;
            }));

            return false;
        }

        static void AddSound(string path, string id)
        {
            CoreHelper.Log($"Adding sound: {id} {path}");
            if (RTFile.FileExists(path))
                CoreHelper.StartCoroutine(AlephNetworkManager.DownloadAudioClip($"file://{path}", RTFile.GetAudioType(path), audioClip =>
                {
                    audioClip.name = id;
                    var soundGroup = new SoundLibrary.SoundGroup { group = new AudioClip[] { audioClip }, soundID = id };
                    Instance.soundGroups = Instance.soundGroups.AddItem(soundGroup).ToArray();
                    Instance.soundClips[soundGroup.soundID] = soundGroup.group;
                }));

        }

        [HarmonyPatch(nameof(SoundLibrary.GetClipFromName))]
        [HarmonyPrefix]
        static bool GetClipFromNamePrefix(SoundLibrary __instance, ref AudioClip __result, string __0)
        {
            // Optimized method by calling TryGetValue instead of ContainsKey.
            if (__instance.soundClips.TryGetValue(__0, out AudioClip[] soundClips))
            {
                __result = soundClips[Random.Range(0, soundClips.Length)];
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
            if (!__instance.musicClips.TryGetValue(__0, out AudioClip[] musicClips))
            {
                Debug.LogError($"{__instance.className}Music Clip not found -> [{__0}]");
                __result = null;
                return false;
            }

            if (__instance.musicClipsRandomIndex.TryGetValue(__0, out int randomIndex))
                return musicClips[randomIndex];

            return musicClips[Random.Range(0, musicClips.Length)];
        }

        static string GetMusicName(string input) => input switch
        {
            "air_-_truepianoskg.ogg" => "AiR - Truepianoskg",
            "dread.ogg" => "Dread",
            "in the distance.ogg" => "F-777 - In the Distance",
            "io.ogg" => "meganeko - IO",
            "jukio -kozilek- kallio - distance.ogg" => "Jukio -Kozilek- Kallio - Distance",
            _ => input
        };

        static string GetGroupName(string input) => input switch
        {
            "air_-_truepianoskg.ogg" => "truepianoskg",
            "dread.ogg" => "dread",
            "in the distance.ogg" => "in_the_distance",
            "io.ogg" => "io",
            "jukio -kozilek- kallio - distance.ogg" => "jukio_distance",
            _ => input
        };
    }
}
