using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

using SimpleJSON;

using BetterLegacy.Core;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Helpers;

using UnityObject = UnityEngine.Object;

namespace BetterLegacy
{
    public class LegacyResources
    {
        public static LegacyResources Instance { get; } = new LegacyResources();

        public static Shader analogGlitchShader;
        public static Material analogGlitchMaterial;
        public static Shader digitalGlitchShader;
        public static Material digitalGlitchMaterial;

        public static void GetKinoGlitch()
        {
            var assetBundle = AssetBundle.LoadFromFile(RTFile.GetAsset($"builtin/effects.asset")); // Get AssetBundle from assets folder.
            analogGlitchMaterial = assetBundle.LoadAsset<Material>("analogglitchmaterial.mat"); // Load asset
            digitalGlitchMaterial = assetBundle.LoadAsset<Material>("digitalglitchmaterial.mat"); // Load asset
            analogGlitchShader = assetBundle.LoadAsset<Shader>("analogglitch.shader"); // Load asset
            digitalGlitchShader = assetBundle.LoadAsset<Shader>("digitalglitch.shader"); // Load asset
        }

        public static MaterialGroup objectMaterial;

        public static MaterialGroup outlineMaterial;

        public static MaterialGroup outlineBehindMaterial;

        public static MaterialGroup editorOutlineMaterial;

        public static MaterialGroup blurMaterial;

        public static MaterialGroup blurColoredMaterial;

        public static Dictionary<string, MaterialGroup> materials = new Dictionary<string, MaterialGroup>();

        static AssetBundle objectMaterialsAssetBundle;

        public class MaterialGroup
        {
            public string key;
            public Shader shader;
            public Material material;

            public static implicit operator Material(MaterialGroup materialGroup) => materialGroup.material;
            public static implicit operator Shader(MaterialGroup materialGroup) => materialGroup.shader;
        }

        public static string GetObjectMaterialKey(bool doubleSided, int gradientType, int blendMode)
        {
            var key = (doubleSided ? "double/" : "single/");

            if (gradientType != 0)
                key += "gradients/";

            if (blendMode != 0)
                key += "blendmodes/";

            key += blendMode switch
            {
                1 => "multiply",
                2 => "add",
                _ => string.Empty
            };

            if (gradientType != 0)
            {
                if (blendMode != 0)
                    key += "_";

                key += gradientType switch
                {
                    1 => "linear",
                    2 => "linear",
                    3 => "radial",
                    4 => "radial",
                    _ => string.Empty,
                };
            }

            if (gradientType == 0 && blendMode == 0)
                key += "object";

            return key;
        }

        public static MaterialGroup GetObjectMaterial(bool doubleSided, int gradientType, int blendMode)
        {
            return materials.TryGetValue(GetObjectMaterialKey(doubleSided, gradientType, blendMode), out MaterialGroup materialGroup) ? materialGroup : objectMaterial;
        }

        public static void GetObjectMaterials()
        {
            objectMaterialsAssetBundle = AssetBundle.LoadFromFile(RTFile.GetAsset($"builtin/objectmaterials.asset")); // Get AssetBundle from assets folder.

            foreach (var a in objectMaterialsAssetBundle.GetAllAssetNames())
            {
                var assetName = System.IO.Path.GetFileName(a);
                var assetKey = a
                    .Remove("assets/shaders/")
                    .Remove("shaders/")
                    .Remove("_double")
                    .Remove(".shader");

                CoreHelper.Log($"Loading object material\n" +
                    $"Asset Path: {a}\n" +
                    $"Asset Name: {System.IO.Path.GetFileName(a)}\n" +
                    $"Asset Key: {assetKey}");

                var materialGroup = CreateMaterialGroup(assetKey, objectMaterialsAssetBundle.LoadAsset<Shader>(assetName));

                switch (assetName)
                {
                    case "object.shader": {
                            objectMaterial = materialGroup;
                            break;
                        }
                    case "outline.shader": {
                            outlineMaterial = materialGroup;
                            break;
                        }
                    case "outline_behind.shader": {
                            outlineBehindMaterial = materialGroup;
                            break;
                        }
                    case "editor_outline.shader": {
                            editorOutlineMaterial = materialGroup;
                            break;
                        }
                    case "blur.shader": {
                            blurMaterial = materialGroup;
                            break;
                        }
                    case "blur_colored.shader": {
                            blurColoredMaterial = materialGroup;
                            break;
                        }
                }

                materials[assetKey] = materialGroup;
            }
        }

        static MaterialGroup CreateMaterialGroup(string assetKey, Shader shader) => new MaterialGroup
        {
            key = assetKey,
            shader = shader,
            material = new Material(shader),
        };

        public static Material canvasImageMask;
        // from https://stackoverflow.com/questions/59138535/unity-ui-image-alpha-overlap
        public static void GetGUIAssets()
        {
            var assetBundle = AssetBundle.LoadFromFile(RTFile.GetAsset("builtin/gui.asset"));
            canvasImageMask = new Material(assetBundle.LoadAsset<Shader>("canvasimagemask.shader"));
            assetBundle.Unload(false);
        }


        public static AssetBundle postProcessResourcesAssetBundle;
        public static PostProcessResources postProcessResources;

        public static void GetEffects()
        {
            postProcessResourcesAssetBundle = AssetBundle.LoadFromFile(RTFile.GetAsset("builtin/effectresources.asset"));
            postProcessResources = postProcessResourcesAssetBundle.LoadAsset<PostProcessResources>("postprocessresources.asset");
        }

        public static Dictionary<Rank, string[]> sayings = new Dictionary<Rank, string[]>()
        {
            { Rank.Null, null },
            { Rank.SS, null },
            { Rank.S, null },
            { Rank.A, null },
            { Rank.B, null },
            { Rank.C, null },
            { Rank.D, null },
            { Rank.F, null },
        };

        public static void GetSayings()
        {
            if (!AssetPack.TryReadFromFile($"core/sayings{FileFormat.JSON.Dot()}", out string file))
                return;

            var sayingsJN = JSON.Parse(file)["sayings"];

            if (sayingsJN == null)
                return;

            var ranks = Rank.Null.GetValues();
            foreach (var rank in ranks)
            {
                if (sayingsJN[rank.Name.ToLower()] != null)
                    sayings[rank] = sayingsJN[rank.Name.ToLower()].Children.Select(x => x.Value).ToArray();
            }
        }

        public static void LoadSounds()
        {
            var library = Patchers.SoundLibraryPatch.inst;

            for (int i = 0; i < soundClips.Count; i++)
            {
                var soundGroup = soundClips[i];
                for (int j = 0; j < soundGroup.Count; j++)
                {
                    var clip = soundGroup[j];
                    if (!clip.custom)
                        continue;

                    clip.clip.UnloadAudioData();
                    CoreHelper.Destroy(clip);
                    soundGroup.group[j] = null;
                }
                soundGroup.group.Clear();
            }
            soundClips.Clear();

            // load original sounds
            for (int i = 0; i < library.soundGroups.Length; i++)
                soundClips.Add(library.soundGroups[i]);

            var sounds = AssetPack.GetArray("core/sounds.json");
            for (int i = 0; i < sounds.Count; i++)
            {
                var sound = sounds[i];
                if (sound["clips"] == null)
                    continue;

                var id = sound["name"];
                CoreHelper.Log($"Loading sound {id}");

                var soundGroupIndex = soundClips.FindIndex(x => x.id == id);
                var soundGroup = soundGroupIndex >= 0 ? soundClips[soundGroupIndex] : new SoundGroup(id);
                if (soundGroupIndex < 0)
                    soundClips.Add(soundGroup);

                for (int j = 0; j < sound["clips"].Count; j++)
                {
                    var clipIndex = j;
                    var c = sound["clips"][j];
                    if (c == null)
                        continue;

                    if (c.IsString)
                    {
                        LoadSound(c, id, clipIndex, soundGroup);
                        continue;
                    }

                    if (c["path"] != null)
                    {
                        LoadSound(c["path"], id, clipIndex, soundGroup);
                        continue;
                    }

                    var orig = soundGroup.orig;
                    var findID = c["id"];
                    if (!string.IsNullOrEmpty(findID))
                        orig = library.soundGroups.First(x => x.soundID == findID);

                    var index = c["index"].AsInt;
                    if (orig != null && orig.group.TryGetAt(index, out AudioClip origClip))
                        soundGroup.group.TryAdd(clipIndex, origClip);
                    continue;
                }
            }
        }

        static void LoadSound(string c, string id, int clipIndex, SoundGroup soundGroup)
        {
            var path = AssetPack.GetFile(c);
            if (RTFile.FileExists(path))
                CoroutineHelper.StartCoroutine(AlephNetwork.DownloadAudioClip($"file://{path}", RTFile.GetAudioType(path), audioClip =>
                {
                    audioClip.name = id;
                    soundGroup.group.TryAdd(clipIndex, new SoundGroup.AudioClipWrapper(audioClip, true));
                }));
        }

        public static void LoadMusic()
        {
            var library = Patchers.SoundLibraryPatch.inst;

            var ostFilePath = RTFile.GetAsset($"builtin/ost{FileFormat.ASSET.Dot()}");
            List<AudioClip> loadedClips = new List<AudioClip>();

            if (RTFile.FileExists(ostFilePath))
            {
                var ost = AssetBundle.LoadFromFile(ostFilePath);

                foreach (var asset in ost.GetAllAssetNames())
                {
                    var assetName = asset.Remove("assets/ost/");
                    var music = ost.LoadAsset<AudioClip>(assetName);

                    if (music == null)
                    {
                        Debug.LogError($"{library.className}The music ({assetName}) does not exist in the asset bundle for some reason.");
                        continue;
                    }

                    var name = GetMusicName(assetName);
                    var groupName = GetGroupName(assetName);

                    music.name = assetName;

                    loadedClips.Add(music);
                }
                ost.Unload(false);
            }

            // removes barrels from main menu music so it can serve as the loading music
            var menuMusic = library.musicGroups[1];
            var audioClips = menuMusic.music;
            menuMusic.music = new AudioClip[audioClips.Length - 1];
            for (int i = 1; i < audioClips.Length; i++)
                menuMusic.music[i - 1] = audioClips[i];

            foreach (var musicGroup in library.musicGroups)
            {
                if (musicGroup.music.Length > 1 && !musicGroup.alwaysRandom) // not alwaysRandom is apparently ACTUALLY RANDOM???
                    library.musicClipsRandomIndex[musicGroup.musicID] = Random.Range(0, musicGroup.music.Length);

                musicClips.Add(musicGroup);
            }

            var songs = AssetPack.GetArray("core/music.json");
            for (int i = 0; i < songs.Count; i++)
            {
                var song = songs[i];
                if (song["clips"] == null)
                    continue;

                var id = song["name"];
                CoreHelper.Log($"Loading sound {id}");

                var musicGroupIndex = musicClips.FindIndex(x => x.id == id);
                var musicGroup = musicGroupIndex >= 0 ? musicClips[musicGroupIndex] : new MusicGroup(id);
                if (musicGroupIndex < 0)
                    musicClips.Add(musicGroup);

                for (int j = 0; j < song["clips"].Count; j++)
                {
                    var clipIndex = j;
                    var c = song["clips"][j];
                    if (c == null)
                        continue;

                    if (c.IsString)
                    {
                        LoadMusic(c, id, clipIndex, musicGroup, loadedClips);
                        continue;
                    }

                    if (c["path"] != null)
                    {
                        LoadMusic(c["path"], id, clipIndex, musicGroup, loadedClips);
                        continue;
                    }

                    var orig = musicGroup.orig;
                    var findID = c["id"];
                    if (!string.IsNullOrEmpty(findID))
                        orig = library.musicGroups.First(x => x.musicID == findID);

                    var index = c["index"].AsInt;
                    if (orig != null && orig.music.TryGetAt(index, out AudioClip origClip))
                        musicGroup.group.TryAdd(clipIndex, origClip);
                    continue;
                }
            }

            loadedClips.Clear();
        }

        static void LoadMusic(string c, string id, int clipIndex, MusicGroup musicGroup, List<AudioClip> loadedClips)
        {
            if (loadedClips.TryFind(x => x.name == c, out AudioClip loadedSong))
                musicGroup.group.TryAdd(clipIndex, loadedSong);
            else
                LoadSound(c, id, clipIndex, musicGroup);
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

        public static List<SoundGroup> soundClips = new List<SoundGroup>();
        public static List<MusicGroup> musicClips = new List<MusicGroup>();
    }
}
