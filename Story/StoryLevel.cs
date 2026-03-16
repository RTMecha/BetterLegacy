using System;
using System.Collections;

using UnityEngine;
using UnityEngine.Video;

using SimpleJSON;

using BetterLegacy.Core;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Data.Level;
using BetterLegacy.Core.Helpers;

namespace BetterLegacy.Story
{
    /// <summary>
    /// Stores data to be used for playing levels in the story mode. Level does not need to have a full path, it can be purely an asset.
    /// </summary>
    public class StoryLevel : Level
    {
        public StoryLevel() : base() => isStory = true;

        #region Values

        /// <summary>
        /// Name of the story level.
        /// </summary>
        public string name;

        /// <summary>
        /// The full level.lsb JSON.
        /// </summary>
        public string json;

        /// <summary>
        /// The players.lsb JSON.
        /// </summary>
        public string jsonPlayers;

        /// <summary>
        /// Used for any cases where we want to play a VideoClip to showcase the Video BG feature.
        /// </summary>
        public VideoClip videoClip;

        /// <summary>
        /// If the level is from the game files.
        /// </summary>
        public bool isResourcesBeatmap;

        #endregion

        #region Functions

        /// <summary>
        /// Loads a story level from an <see cref="AssetBundle"/>.
        /// </summary>
        /// <param name="path">Path to an <see cref="AssetBundle"/>.</param>
        /// <param name="result">The loaded <see cref="StoryLevel"/>.</param>
        public static IEnumerator LoadFromAsset(string path, Action<StoryLevel> result)
        {
            CoreHelper.Log($"Loading level from {System.IO.Path.GetFileName(path)}");

            if (!RTFile.FileExists(path))
                yield break;

            AssetBundle assets = null;
            yield return CoroutineHelper.StartCoroutineAsync(AlephNetwork.DownloadAssetBundle($"file://{path}", assetBundle =>
            {
                assets = assetBundle;
                assetBundle = null;
            }));

            if (assets == null)
            {
                CoreHelper.LogError($"Assets is null.");
                yield break;
            }

            var storyLevel = FromAsset(assets);
            if (!storyLevel)
            {
                assets.Unload(true);
                assets = null;
                yield break;
            }

            assets.Unload(false);
            assets = null;
            result?.Invoke(storyLevel);
        }

        /// <summary>
        /// Loads a story level from an <see cref="AssetBundle"/>.
        /// </summary>
        /// <param name="assets"><see cref="AssetBundle"/> to get a story level from.</param>
        /// <returns>Returns a story level.</returns>
        public static StoryLevel FromAsset(AssetBundle assets)
        {
            var metadataJSON = assets.LoadAsset<TextAsset>($"metadata{FileFormat.JSON.Dot()}");
            var metadataJN = JSON.Parse(metadataJSON.text);
            var metadata = MetaData.Parse(metadataJN);
            if (metadataJN != null && metadataJN["package"] == null && metadata)
            {
                metadata.package = new PackageMetaData
                {
                    files = new System.Collections.Generic.List<PackageMetaData.File>
                    {
                        new PackageMetaData.File("audio", $"song{FileFormat.OGG.Dot()}"),
                        new PackageMetaData.File("cover", $"cover{FileFormat.JPG.Dot()}"),
                        new PackageMetaData.File("level", $"level{FileFormat.JSON.Dot()}"),
                        new PackageMetaData.File("players", $"players{FileFormat.JSON.Dot()}"),
                    },
                };
            }

            var icon = assets.LoadAsset<Sprite>(metadata.package.GetImage(metadata.package.mainCover)?.fileName ?? "cover.jpg");
            var levelJSON = assets.LoadAsset<TextAsset>(metadata.package.GetLevel(metadata.package.mainLevel)?.fileName ?? "level.json");
            var players = assets.LoadAsset<TextAsset>(metadata.package.GetLevel("players")?.fileName ?? $"players{FileFormat.JSON.Dot()}");

            var storyLevel = new StoryLevel
            {
                id = metadata?.arcadeID,
                name = metadata?.beatmap?.name,
                icon = icon,
                json = levelJSON ? levelJSON.text : null,
                metadata = metadata,
                jsonPlayers = players ? players.text : null,
                videoClip = assets.Contains($"bg{FileFormat.MP4.Dot()}") ? assets.LoadAsset<VideoClip>($"bg{FileFormat.MP4.Dot()}") : null,
            };

            for (int i = 0; i < metadata.package.files.Count; i++)
            {
                var file = metadata.package.files[i];
                if (!file.IsAudio)
                    continue;

                var audioClip = assets.LoadAsset<AudioClip>(file.fileName);
                if (!audioClip)
                    continue;
                storyLevel.tracks.TryAdd(file.id, audioClip);
            }

            if (storyLevel.tracks.TryGetValue(metadata.package.mainAudio, out AudioClip music))
                storyLevel.music = music;
            else
            {
                CoreHelper.Log($"Level [{storyLevel}] had to load the main audio the default way.");
                storyLevel.music = assets.LoadAsset<AudioClip>($"song{FileFormat.OGG.Dot()}");
                storyLevel.tracks["audio"] = storyLevel.music;
            }

            return storyLevel;
        }

        /// <summary>
        /// Loads the story levels' game data.
        /// </summary>
        /// <returns>Returns the loaded game data.</returns>
        public override GameData LoadGameData() => GameData.Parse(JSON.Parse(json));

        #endregion
    }
}
