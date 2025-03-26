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
            yield return CoreHelper.StartCoroutineAsync(AlephNetwork.DownloadAssetBundle($"file://{path}", assetBundle =>
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
            var icon = assets.LoadAsset<Sprite>($"cover{FileFormat.JPG.Dot()}");
            var song = assets.LoadAsset<AudioClip>($"song{FileFormat.OGG.Dot()}");
            var levelJSON = assets.LoadAsset<TextAsset>($"level{FileFormat.JSON.Dot()}");
            var metadataJSON = assets.LoadAsset<TextAsset>($"metadata{FileFormat.JSON.Dot()}");
            var players = assets.LoadAsset<TextAsset>($"players{FileFormat.JSON.Dot()}");

            var metadata = MetaData.Parse(JSON.Parse(metadataJSON.text), false);
            var storyLevel = new StoryLevel
            {
                id = metadata?.arcadeID,
                name = metadata?.beatmap?.name,
                icon = icon,
                music = song,
                json = levelJSON.text,
                metadata = metadata,
                jsonPlayers = players.text,
                videoClip = assets.Contains($"bg{FileFormat.MP4.Dot()}") ? assets.LoadAsset<VideoClip>($"bg{FileFormat.MP4.Dot()}") : null,
            };

            return storyLevel;
        }

        /// <summary>
        /// Loads the story levels' game data.
        /// </summary>
        /// <returns>Returns the loaded game data.</returns>
        public override GameData LoadGameData() => GameData.Parse(JSON.Parse(json));
    }
}
