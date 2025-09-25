using System.Linq;

using UnityEngine;

using HarmonyLib;

using BetterLegacy.Companion.Entity;
using BetterLegacy.Core;
using BetterLegacy.Core.Helpers;

namespace BetterLegacy.Patchers
{
    [HarmonyPatch(typeof(SoundLibrary))]
    public class SoundLibraryPatch
    {
        public static SoundLibrary Instance => AudioManager.inst.library;
        static SoundLibrary inst;

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

            var ostFilePath = RTFile.GetAsset($"builtin/ost{FileFormat.ASSET.Dot()}");

            if (RTFile.FileExists(ostFilePath))
            {
                var ost = AssetBundle.LoadFromFile(ostFilePath);

                foreach (var asset in ost.GetAllAssetNames())
                {
                    var assetName = asset.Remove("assets/ost/");
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
            }

            foreach (var soundGroup in __instance.soundGroups)
                __instance.soundClips[soundGroup.soundID] = soundGroup.group;

            var quickSounds = Resources.LoadAll<AudioClip>("terminal/quick-sounds");
            if (quickSounds != null)
                foreach (var audioClip in quickSounds)
                    __instance.soundClips["qe_" + audioClip.name] = new AudioClip[] { audioClip };

            var ogg = FileFormat.OGG.Dot();
            AddSound(AssetPack.GetFile($"companion/model/example speak{ogg}"), DefaultSounds.example_speak);
            AddSound($"{SFXPath}anna speak{ogg}", DefaultSounds.anna_speak);
            AddSound($"{SFXPath}hal speak{ogg}", DefaultSounds.hal_speak);
            AddSound($"{SFXPath}para speak{ogg}", DefaultSounds.para_speak);
            AddSound($"{SFXPath}t speak{ogg}", DefaultSounds.t_speak);
            AddSound($"{SFXPath}menuflip{ogg}", DefaultSounds.menuflip);
            AddSound($"{SFXPath}Record Scratch{ogg}", DefaultSounds.record_scratch);
            AddSound($"{SFXPath}hit2{ogg}", DefaultSounds.HurtPlayer2);
            AddSound($"{SFXPath}hit3{ogg}", DefaultSounds.HurtPlayer3);
            AddSound($"{SFXPath}HealPlayer{ogg}", DefaultSounds.HealPlayer);
            AddSound($"{SFXPath}shoot{ogg}", DefaultSounds.shoot);
            AddSound($"{SFXPath}pop{ogg}", DefaultSounds.pop);

            var menuMusic = __instance.musicGroups[1];
            var audioClips = menuMusic.music;
            menuMusic.music = new AudioClip[audioClips.Length - 1];
            for (int i = 1; i < audioClips.Length; i++)
                menuMusic.music[i - 1] = audioClips[i];

            foreach (var musicGroup in __instance.musicGroups)
            {
                if (musicGroup.music.Length > 1 && !musicGroup.alwaysRandom) // not alwaysRandom is apparently ACTUALLY RANDOM???
                    __instance.musicClipsRandomIndex[musicGroup.musicID] = Random.Range(0, musicGroup.music.Length);
                __instance.musicClips[musicGroup.musicID] = musicGroup.music;
            }

            SetSound($"{SFXPath}click cut{ogg}", DefaultSounds.UpDown, DefaultSounds.LeftRight);
            SetSound($"{SFXPath}optionexit{ogg}", DefaultSounds.Block);
            SetSound($"{SFXPath}SpawnPlayer{ogg}", DefaultSounds.SpawnPlayer);

            return false;
        }

        static void SetSound(string path, params DefaultSounds[] defaultSounds)
        {
            inst.StartCoroutine(AlephNetwork.DownloadAudioClip($"file://{path}", AudioType.OGGVORBIS, audioClip =>
            {
                foreach (var defaultSound in defaultSounds)
                    inst.soundClips[defaultSound.ToString()][0] = audioClip;
            }));
        }

        static void AddSound(string path, DefaultSounds defaultSound)
        {
            var id = defaultSound.ToString();
            CoreHelper.Log($"Adding sound: {id} {path}");
            if (RTFile.FileExists(path))
                CoroutineHelper.StartCoroutine(AlephNetwork.DownloadAudioClip($"file://{path}", RTFile.GetAudioType(path), audioClip =>
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

            __result = musicClips[__instance.musicClipsRandomIndex.TryGetValue(__0, out int randomIndex) ? randomIndex : Random.Range(0, musicClips.Length)];
            return false;
        }

        static string GetMusicName(string input) => input switch
        {
            "air_-_truepianoskg.ogg" => "AiR - Truepianoskg",
            "dread.ogg" => "Dread",
            "in the distance.ogg" => "F-777 - In the Distance",
            "io.ogg" => "meganeko - IO",
            "jukio -kozilek- kallio - distance.ogg" => "Jukio -Kozilek- Kallio - Distance",
            "shirobon - infinity - 06 little calculations.ogg" => "Shirobon - Little Calculations",
            "shirobon - infinity - 10 reflections.ogg" => "Shirobon - Reflections",
            "shirobon - rebirth - 01 fracture.ogg" => "Shirobon - Fracture",
            "shirobon - rebirth - 04 xilioh.ogg" => "Shirobon - Xilioh",
            _ => input
        };

        static string GetGroupName(string input) => input switch
        {
            "air_-_truepianoskg.ogg" => "truepianoskg",
            "dread.ogg" => "dread",
            "in the distance.ogg" => "in_the_distance",
            "io.ogg" => "io",
            "jukio -kozilek- kallio - distance.ogg" => "jukio_distance",
            "shirobon - infinity - 06 little calculations.ogg" => "little_calculations",
            "shirobon - infinity - 10 reflections.ogg" => "reflections",
            "shirobon - rebirth - 01 fracture.ogg" => "fracture",
            "shirobon - rebirth - 04 xilioh.ogg" => "xilioh",
            _ => input
        };
    }
}
