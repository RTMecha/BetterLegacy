﻿using BetterLegacy.Configs;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using BetterLegacy.Core.Managers.Networking;
using SimpleJSON;
using System;
using System.Collections;
using System.IO;
using UnityEngine;

namespace BetterLegacy.Core
{
    /// <summary>
    /// Stores data to be used for playing levels in the arcade. Make sure the path ends in a "/"
    /// </summary>
    public class Level : Exists
    {
        public Level() { }

        public Level(string path, bool loadIcon = true)
        {
            this.path = path;

            if (CoreConfig.Instance.PrioritizeVG.Value && RTFile.FileExists($"{path}metadata.vgm"))
                metadata = MetaData.ParseVG(JSON.Parse(RTFile.ReadFromFile($"{path}metadata.vgm")));
            else if (RTFile.FileExists($"{path}metadata.lsb"))
                metadata = MetaData.Parse(JSON.Parse(RTFile.ReadFromFile($"{path}metadata.lsb")), false);
            else if (RTFile.FileExists($"{path}metadata.vgm"))
                metadata = MetaData.ParseVG(JSON.Parse(RTFile.ReadFromFile($"{path}metadata.vgm")));

            if (loadIcon)
                icon = RTFile.FileExists($"{path}level.jpg") ? SpriteHelper.LoadSprite($"{path}level.jpg") : RTFile.FileExists($"{path}cover.jpg") ? SpriteHelper.LoadSprite($"{path}cover.jpg") : SteamWorkshop.inst.defaultSteamImageSprite;

            UpdateDefaults();
        }

        public Level(string path, MetaData metadata)
        {
            this.path = path;

            this.metadata = metadata;

            icon = RTFile.FileExists($"{path}level.jpg") ? SpriteHelper.LoadSprite($"{path}level.jpg") : RTFile.FileExists($"{path}cover.jpg") ? SpriteHelper.LoadSprite($"{path}cover.jpg") : SteamWorkshop.inst.defaultSteamImageSprite;

            UpdateDefaults();
        }

        public void UpdateDefaults()
        {
            if (metadata)
            {
                if (!string.IsNullOrEmpty(metadata.arcadeID) && metadata.arcadeID != "-1")
                    id = metadata.arcadeID;
                else if (!string.IsNullOrEmpty(metadata.beatmap.beatmap_id) && metadata.beatmap.beatmap_id != "-1")
                    id = metadata.beatmap.beatmap_id;
                else
                    id = "-1";
            }

            if (RTFile.FileExists($"{path}modes.lsms"))
            {
                var jn = JSON.Parse(RTFile.ReadFromFile($"{path}modes.lsms"));
                LevelModes = new string[jn["paths"].Count + 1];
                LevelModes[0] = CoreConfig.Instance.PrioritizeVG.Value && RTFile.FileExists($"{path}level.vgd") ? "level.vgd" : "level.lsb";
                for (int i = 1; i < jn["paths"].Count + 1; i++)
                {
                    LevelModes[i] = jn["paths"][i - 1];
                }
            }
            else
                LevelModes = new string[1]
                {
                    CoreConfig.Instance.PrioritizeVG.Value && RTFile.FileExists($"{path}level.vgd") ? "level.vgd" : "level.lsb",
                };
        }

        public SteamworksFacepunch.Ugc.Item steamItem;
        public bool isSteamLevel;
        public bool steamLevelInit;

        public string path;

        public Sprite icon;

        public AudioClip music;

        public string id;

        public MetaData metadata;

        public int currentMode = 0;
        public string[] LevelModes { get; set; }
        public string CurrentFile => LevelModes[Mathf.Clamp(LevelManager.CurrentLevelMode, 0, LevelModes.Length - 1)];

        public bool Locked => metadata != null && metadata.requireUnlock && (playerData == null || !playerData.Unlocked);

        public void LoadAudioClip()
        {
            if (RTFile.FileExists(path + "level.ogg") && !music)
            {
                CoreHelper.StartCoroutineAsync(AlephNetworkManager.DownloadAudioClip("file://" + path + "level.ogg", AudioType.OGGVORBIS, delegate (AudioClip audioClip)
                {
                    music = audioClip;
                }));
            }
            else if (RTFile.FileExists(path + "level.wav") && !music)
            {
                CoreHelper.StartCoroutineAsync(AlephNetworkManager.DownloadAudioClip("file://" + path + "level.wav", AudioType.WAV, delegate (AudioClip audioClip)
                {
                    music = audioClip;
                }));
            }
            else if (RTFile.FileExists(path + "level.mp3") && !music)
            {
                music = LSFunctions.LSAudio.CreateAudioClipUsingMP3File(path + "level.mp3");
            }
            else if (RTFile.FileExists(path + "audio.ogg") && !music)
            {
                CoreHelper.StartCoroutineAsync(AlephNetworkManager.DownloadAudioClip("file://" + path + "audio.ogg", AudioType.OGGVORBIS, delegate (AudioClip audioClip)
                {
                    music = audioClip;
                }));
            }
            else if (RTFile.FileExists(path + "audio.wav") && !music)
            {
                CoreHelper.StartCoroutineAsync(AlephNetworkManager.DownloadAudioClip("file://" + path + "audio.wav", AudioType.WAV, delegate (AudioClip audioClip)
                {
                    music = audioClip;
                }));
            }
            else if (RTFile.FileExists(path + "audio.mp3") && !music)
            {
                music = LSFunctions.LSAudio.CreateAudioClipUsingMP3File(path + "audio.mp3");
            }
        }

        public IEnumerator LoadAudioClipRoutine(Action onComplete = null)
        {
            if (RTFile.FileExists(path + "level.ogg") && !music)
            {
                yield return CoreHelper.StartCoroutineAsync(AlephNetworkManager.DownloadAudioClip("file://" + path + "level.ogg", AudioType.OGGVORBIS, delegate (AudioClip audioClip)
                {
                    music = audioClip;
                }));
            }
            else if (RTFile.FileExists(path + "level.wav") && !music)
            {
                yield return CoreHelper.StartCoroutineAsync(AlephNetworkManager.DownloadAudioClip("file://" + path + "level.wav", AudioType.WAV, delegate (AudioClip audioClip)
                {
                    music = audioClip;
                }));
            }
            else if (RTFile.FileExists(path + "level.mp3") && !music)
            {
                yield return music = LSFunctions.LSAudio.CreateAudioClipUsingMP3File(path + "level.mp3");
            }
            else if (RTFile.FileExists(path + "audio.ogg") && !music)
            {
                yield return CoreHelper.StartCoroutineAsync(AlephNetworkManager.DownloadAudioClip("file://" + path + "audio.ogg", AudioType.OGGVORBIS, delegate (AudioClip audioClip)
                {
                    music = audioClip;
                }));
            }
            else if (RTFile.FileExists(path + "audio.wav") && !music)
            {
                yield return CoreHelper.StartCoroutineAsync(AlephNetworkManager.DownloadAudioClip("file://" + path + "audio.wav", AudioType.WAV, delegate (AudioClip audioClip)
                {
                    music = audioClip;
                }));
            }
            else if (RTFile.FileExists(path + "audio.mp3") && !music)
            {
                yield return music = LSFunctions.LSAudio.CreateAudioClipUsingMP3File(path + "audio.mp3");
            }

            onComplete?.Invoke();
        }

        public LevelManager.PlayerData playerData;

        public bool fromCollection;

        public bool isStory;

        public bool IsVG => CurrentFile.Contains(".vgd");
        public bool VGExists => RTFile.FileExists($"{path}level.vgd") && RTFile.FileExists($"{path}metadata.vgm");

        public bool InvalidID => string.IsNullOrEmpty(id) || id == "0" || id == "-1";

        public override string ToString() => $"{System.IO.Path.GetFileName(System.IO.Path.GetDirectoryName(path))} - {id} - {(metadata == null || metadata.song == null ? "" : metadata.song.title)}";

        /// <summary>
        /// Checks if all files required to load a level exist. Includes LS / VG formats.
        /// </summary>
        /// <param name="folder">The folder to check. Must end with a /.</param>
        /// <returns>True if all files are validated, otherwise false.</returns>
        public static bool Verify(string folder) => VerifySong(folder) && VerifyMetadata(folder) && VerifyLevel(folder);

        public static bool TryVerify(string folder, bool loadIcon, out Level level)
        {
            var verify = Verify(folder);
            level = verify ? new Level(folder, loadIcon) : null;
            return verify;
        }

        /// <summary>
        /// Checks if the level has a song. Includes all audio types and LS / VG names.
        /// </summary>
        /// <param name="folder">The folder to check. Must end with a /.</param>
        /// <returns>True if a song exists, otherwise false.</returns>
        public static bool VerifySong(string folder) =>
            RTFile.FileExists(folder + "audio.ogg") || RTFile.FileExists(folder + "audio.wav") || RTFile.FileExists(folder + "audio.mp3") ||
            RTFile.FileExists(folder + "level.ogg") || RTFile.FileExists(folder + "level.wav") || RTFile.FileExists(folder + "level.mp3");

        /// <summary>
        /// Checks if the level has metadata. Includes LS and VG formats.
        /// </summary>
        /// <param name="folder">The folder to check. Must end with a /.</param>
        /// <returns>True if metadata exists, otherwise false.</returns>
        public static bool VerifyMetadata(string folder) => RTFile.FileExists(folder + "metadata.vgm") || RTFile.FileExists(folder + "metadata.lsb");

        /// <summary>
        /// Checks if the level has level data. Includes LS and VG formats.
        /// </summary>
        /// <param name="folder">The folder to check. Must end with a /.</param>
        /// <returns>True if level data exists, otherwise false.</returns>
        public static bool VerifyLevel(string folder) => RTFile.FileExists(folder + "level.vgd") || RTFile.FileExists(folder + "level.lsb");
    }
}
