using BetterLegacy.Core;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers.Networking;
using SimpleJSON;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnityEngine;
using UnityEngine.Video;

namespace BetterLegacy.Story
{
    /// <summary>
    /// Stores data to be used for playing levels in the story mode. Level does not need to have a full path, it can be purely an asset.
    /// </summary>
    public class StoryLevel : Level
    {
        public StoryLevel() : base()
        {
            isStory = true;
        }

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

        public static IEnumerator LoadFromAsset(string path, Action<StoryLevel> result)
        {
            CoreHelper.Log($"Loading level from {System.IO.Path.GetFileName(path)}");

            if (!RTFile.FileExists(path))
                yield break;

            AssetBundle assets = null;
            yield return CoreHelper.StartCoroutineAsync(AlephNetworkManager.DownloadAssetBundle($"file://{path}", assetBundle =>
            {
                assets = assetBundle;
                assetBundle = null;
            }));

            if (assets == null)
            {
                CoreHelper.LogError($"Assets is null.");
                yield break;
            }

            var icon = assets.LoadAsset<Sprite>($"cover.jpg");
            var song = assets.LoadAsset<AudioClip>($"song.ogg");
            var levelJSON = assets.LoadAsset<TextAsset>($"level.json");
            var metadataJSON = assets.LoadAsset<TextAsset>($"metadata.json");
            var players = assets.LoadAsset<TextAsset>($"players.json");

            if (!song)
            {
                assets.Unload(true);
                yield break;
            }

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
                videoClip = assets.Contains($"bg.mp4") ? assets.LoadAsset<VideoClip>($"bg.mp4") : null,
            };

            assets.Unload(false);
            assets = null;
            result?.Invoke(storyLevel);
        }
    }
}
