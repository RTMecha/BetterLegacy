using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

using BetterLegacy.Core;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers.Networking;

namespace BetterLegacy.Story
{
    public class StoryManager : MonoBehaviour
    {
        public static string StoryAssetsPath => $"{RTFile.ApplicationDirectory}{RTFile.BepInExAssetsPath}Story/";

        public static AssetBundle covers = null;
        public static AssetBundle levels = null;
        public static AssetBundle songs = null;

        public static bool Loaded { get; set; }

        public static void Init() => new GameObject(nameof(StoryManager), typeof(StoryManager)).transform.SetParent(SystemManager.inst.transform);

        public List<StoryLevel> storyLevels = new List<StoryLevel>();

        public StoryLevel LoadLevel(string name)
        {
            var icon = covers.LoadAsset<Sprite>($"{name}.jpg");
            var song = songs.LoadAsset<AudioClip>($"{name}.ogg");
            var level = levels.LoadAsset<TextAsset>($"{name}level.json");
            var metadata = levels.LoadAsset<TextAsset>($"{name}metadata.json");
            var players = levels.LoadAsset<TextAsset>($"{name}players.json");

            return new StoryLevel
            {
                icon = icon,
                song = song,
                json = level.text,
                jsonMetadata = metadata.text,
                jsonPlayers = players.text,
            };
        }

        public void Load()
        {
            StartCoroutine(ILoad());
        }

        public IEnumerator ILoad()
        {
            yield return CoreHelper.StartCoroutineAsync(AlephNetworkManager.DownloadAssetBundle($"file://{StoryAssetsPath}covers.asset", delegate (AssetBundle assetBundle)
            {
                covers = assetBundle;
            }));

            yield return CoreHelper.StartCoroutineAsync(AlephNetworkManager.DownloadAssetBundle($"file://{StoryAssetsPath}levels.asset", delegate (AssetBundle assetBundle)
            {
                levels = assetBundle;
            }));

            yield return CoreHelper.StartCoroutineAsync(AlephNetworkManager.DownloadAssetBundle($"file://{StoryAssetsPath}ost.asset", delegate (AssetBundle assetBundle)
            {
                songs = assetBundle;
            }));

            var allAssetNames = covers.GetAllAssetNames();

            for (int i = 0; i < allAssetNames.Length; i++)
            {
                var fileName = Path.GetFileName(allAssetNames[i]);

                var name = fileName.Replace(".jpg", "");

                var icon = covers.LoadAsset<Sprite>(fileName);
                var song = songs.LoadAsset<AudioClip>($"{name}.ogg");
                var level = levels.LoadAsset<TextAsset>($"{name}level.json");
                var metadata = levels.LoadAsset<TextAsset>($"{name}metadata.json");
                var players = levels.LoadAsset<TextAsset>($"{name}players.json");

                var storyLevel = new StoryLevel
                {
                    icon = icon,
                    song = song,
                    json = level.text,
                    jsonMetadata = metadata.text,
                    jsonPlayers = players.text,
                };

                storyLevels.Add(storyLevel);
            }
        }
    }
}