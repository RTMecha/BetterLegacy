using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

using BetterLegacy.Core;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers.Networking;
using System.IO.Compression;
using BetterLegacy.Core.Data;

namespace BetterLegacy.Story
{
    public class StoryManager : MonoBehaviour
    {
        public static StoryManager inst;

        public static string StoryAssetsPath => $"{RTFile.ApplicationDirectory}{RTFile.BepInExAssetsPath}Story/";
        public static string StoryAssetsURL => $"{AlephNetworkManager.ArcadeServerURL}api/story/download";

        public static AssetBundle covers = null;
        public static AssetBundle levels = null;
        public static AssetBundle songs = null;

        public static bool Loaded { get; set; }

        public static bool HasFiles => RTFile.FileExists($"{StoryAssetsPath}covers.asset") && RTFile.FileExists($"{StoryAssetsPath}levels.asset") && RTFile.FileExists($"{StoryAssetsPath}ost.asset");

        public bool AssetBundlesLoaded => covers != null && levels != null && songs != null;

        public static void Init() => new GameObject(nameof(StoryManager), typeof(StoryManager)).transform.SetParent(SystemManager.inst.transform);

        void Awake() => inst = this;

        public void Clear(bool unloadAllLoadedObjects = true)
        {
            if (covers)
                covers.Unload(unloadAllLoadedObjects);
            covers = null;
            if (levels)
                levels.Unload(unloadAllLoadedObjects);
            levels = null;
            if (songs)
                songs.Unload(unloadAllLoadedObjects);
            songs = null;

            Loaded = false;
        }

        public List<StoryLevel> storyLevels = new List<StoryLevel>();

        public static StoryLevel CurrentStoryLevel { get; set; }

        public IEnumerator Download(System.Action onComplete = null)
        {
            yield return CoreHelper.StartCoroutineAsync(AlephNetworkManager.DownloadBytes(StoryAssetsURL, (float percentage) => { }, (byte[] bytes) =>
            {
                var directory = $"{RTFile.ApplicationDirectory}{RTFile.BepInExAssetsPath}Story/test";
                var zip = $"{directory}/story.zip";

                if (!RTFile.DirectoryExists(directory))
                    Directory.CreateDirectory(directory);

                File.WriteAllBytes(zip, bytes);

                ZipFile.ExtractToDirectory(zip, directory);

                File.Delete(zip);
            }, (string onError) => { }));

            onComplete?.Invoke();

            yield break;
        }

        public IEnumerator Play(string name)
        {
            StoryLevel storyLevel = LoadLevel(name);

            MetaData.Current = MetaData.Parse(storyLevel.jsonMetadata);
            GameData.Current = GameData.Parse(storyLevel.json);

            AudioManager.inst.PlayMusic(null, storyLevel.song);

            yield break;
        }

        public StoryLevel LoadLevel(string name)
        {
            if (storyLevels.Has(x => x.name == name))
                return storyLevels.Find(x => x.name == name);

            var icon = covers.LoadAsset<Sprite>($"{name}.jpg");
            var song = songs.LoadAsset<AudioClip>($"{name}.ogg");
            var level = levels.LoadAsset<TextAsset>($"{name}level.json");
            var metadata = levels.LoadAsset<TextAsset>($"{name}metadata.json");
            var players = levels.LoadAsset<TextAsset>($"{name}players.json");

            var storyLevel = new StoryLevel
            {
                name = name,
                icon = icon,
                song = song,
                json = level.text,
                jsonMetadata = metadata.text,
                jsonPlayers = players.text,
            };

            storyLevels.Add(storyLevel);

            return storyLevel;
        }

        public void Load()
        {
            StartCoroutine(ILoad());
        }

        public IEnumerator ILoad()
        {
            if (!HasFiles)
            {
                if (AssetBundlesLoaded)
                    Clear();

                Loaded = false;
                yield break;
            }

            yield return CoreHelper.StartCoroutineAsync(AlephNetworkManager.DownloadAssetBundle($"file://{StoryAssetsPath}covers.asset", (AssetBundle assetBundle) =>
            {
                covers = assetBundle;
            }));

            yield return CoreHelper.StartCoroutineAsync(AlephNetworkManager.DownloadAssetBundle($"file://{StoryAssetsPath}levels.asset", (AssetBundle assetBundle) =>
            {
                levels = assetBundle;
            }));

            yield return CoreHelper.StartCoroutineAsync(AlephNetworkManager.DownloadAssetBundle($"file://{StoryAssetsPath}ost.asset", (AssetBundle assetBundle) =>
            {
                songs = assetBundle;
            }));

            //var allAssetNames = covers.GetAllAssetNames();

            //for (int i = 0; i < allAssetNames.Length; i++)
            //{
            //    var fileName = Path.GetFileName(allAssetNames[i]);

            //    var name = fileName.Replace(".jpg", "");

            //    var icon = covers.LoadAsset<Sprite>(fileName);
            //    var song = songs.LoadAsset<AudioClip>($"{name}.ogg");
            //    var level = levels.LoadAsset<TextAsset>($"{name}level.json");
            //    var metadata = levels.LoadAsset<TextAsset>($"{name}metadata.json");
            //    var players = levels.LoadAsset<TextAsset>($"{name}players.json");

            //    var storyLevel = new StoryLevel
            //    {
            //        name = name,
            //        icon = icon,
            //        song = song,
            //        json = level.text,
            //        jsonMetadata = metadata.text,
            //        jsonPlayers = players.text,
            //    };

            //    storyLevels.Add(storyLevel);
            //}

            Loaded = true;
        }
    }
}