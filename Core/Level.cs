using BetterLegacy.Configs;
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

            if (CoreConfig.Instance.PrioritizeVG.Value && RTFile.FileExists(GetFile(METADATA_VGM)))
                metadata = MetaData.ParseVG(JSON.Parse(RTFile.ReadFromFile(GetFile(METADATA_VGM))));
            else if (RTFile.FileExists(GetFile(METADATA_LSB)))
                metadata = MetaData.Parse(JSON.Parse(RTFile.ReadFromFile(GetFile(METADATA_LSB))), false);
            else if (RTFile.FileExists(GetFile(METADATA_VGM)))
                metadata = MetaData.ParseVG(JSON.Parse(RTFile.ReadFromFile(GetFile(METADATA_VGM))));

            if (loadIcon)
                icon = RTFile.FileExists(GetFile("level.jpg")) ? SpriteHelper.LoadSprite(GetFile("level.jpg")) : RTFile.FileExists(GetFile("cover.jpg")) ? SpriteHelper.LoadSprite(GetFile("cover.jpg")) : SteamWorkshop.inst.defaultSteamImageSprite;

            UpdateDefaults();
        }

        public Level(string path, MetaData metadata)
        {
            this.path = path;

            this.metadata = metadata;

            icon = RTFile.FileExists(GetFile("level.jpg")) ? SpriteHelper.LoadSprite(GetFile("level.jpg")) : RTFile.FileExists(GetFile("cover.jpg")) ? SpriteHelper.LoadSprite(GetFile("cover.jpg")) : SteamWorkshop.inst.defaultSteamImageSprite;

            UpdateDefaults();
        }

        #region Fields

        public SteamworksFacepunch.Ugc.Item steamItem;
        public bool isSteamLevel;
        public bool steamLevelInit;

        public string path;

        public Sprite icon;

        public AudioClip music;

        public string id;

        public MetaData metadata;

        public int currentMode = 0;
        public LevelManager.PlayerData playerData;

        public bool fromCollection;

        public bool isStory;

        #endregion

        #region Properties

        public bool Locked => metadata != null && metadata.requireUnlock && (playerData == null || !playerData.Unlocked);

        public string[] LevelModes { get; set; }
        public string CurrentFile => LevelModes[Mathf.Clamp(LevelManager.CurrentLevelMode, 0, LevelModes.Length - 1)];

        public bool IsVG => CurrentFile.Contains(".vgd");
        public bool VGExists => RTFile.FileExists(GetFile(LEVEL_VGD)) && RTFile.FileExists(GetFile(METADATA_VGM));

        public bool InvalidID => string.IsNullOrEmpty(id) || id == "0" || id == "-1";

        #endregion

        #region Constants

        /// <summary>
        /// The cover file in Legacy.
        /// </summary>
        public const string LEVEL_JPG = "level.jpg";
        /// <summary>
        /// The cover file in Alpha.
        /// </summary>
        public const string COVER_JPG = "cover.jpg";

        /// <summary>
        /// The metadata file in Legacy.
        /// </summary>
        public const string METADATA_LSB = "metadata.lsb";
        /// <summary>
        /// The metadata file in Alpha.
        /// </summary>
        public const string METADATA_VGM = "metadata.vgm";

        /// <summary>
        /// The level file in Legacy.
        /// </summary>
        public const string LEVEL_LSB = "level.lsb";
        /// <summary>
        /// The level file in Alpha.
        /// </summary>
        public const string LEVEL_VGD = "level.vgd";

        /// <summary>
        /// The OGG audio file in Alpha.
        /// </summary>
        public const string AUDIO_OGG = "audio.ogg";
        /// <summary>
        /// The WAV audio file in Alpha.
        /// </summary>
        public const string AUDIO_WAV = "audio.wav";
        /// <summary>
        /// The MP3 audio file in Alpha.
        /// </summary>
        public const string AUDIO_MP3 = "audio.mp3";

        /// <summary>
        /// The OGG audio file in Legacy.
        /// </summary>
        public const string LEVEL_OGG = "level.ogg";
        /// <summary>
        /// The WAV audio file in Legacy.
        /// </summary>
        public const string LEVEL_WAV = "level.wav";
        /// <summary>
        /// The MP3 audio file in Legacy.
        /// </summary>
        public const string LEVEL_MP3 = "level.mp3";

        #endregion

        #region Methods

        /// <summary>
        /// Combines the file name with the levels' path.
        /// </summary>
        /// <param name="fileName">File to combine.</param>
        /// <returns>Returns a combined path.</returns>
        public string GetFile(string fileName) => RTFile.CombinePaths(path, fileName);

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

            if (RTFile.FileExists(GetFile("modes.lsms")))
            {
                var jn = JSON.Parse(RTFile.ReadFromFile(GetFile("modes.lsms")));
                LevelModes = new string[jn["paths"].Count + 1];
                LevelModes[0] = CoreConfig.Instance.PrioritizeVG.Value && RTFile.FileExists(GetFile(LEVEL_VGD)) ? LEVEL_VGD : LEVEL_LSB;
                for (int i = 1; i < jn["paths"].Count + 1; i++)
                    LevelModes[i] = jn["paths"][i - 1];
            }
            else
                LevelModes = new string[1] { CoreConfig.Instance.PrioritizeVG.Value && RTFile.FileExists(GetFile(LEVEL_VGD)) ? LEVEL_VGD : LEVEL_LSB, };
        }

        public void LoadAudioClip()
        {
            if (music)
                return;

            if (RTFile.FileExists(GetFile(LEVEL_OGG)))
            {
                CoreHelper.StartCoroutineAsync(AlephNetworkManager.DownloadAudioClip("file://" + GetFile(LEVEL_OGG), AudioType.OGGVORBIS, delegate (AudioClip audioClip)
                {
                    music = audioClip;
                }));
            }
            else if (RTFile.FileExists(GetFile(LEVEL_WAV)))
            {
                CoreHelper.StartCoroutineAsync(AlephNetworkManager.DownloadAudioClip("file://" + GetFile(LEVEL_WAV), AudioType.WAV, delegate (AudioClip audioClip)
                {
                    music = audioClip;
                }));
            }
            else if (RTFile.FileExists(GetFile(LEVEL_MP3)))
            {
                music = LSFunctions.LSAudio.CreateAudioClipUsingMP3File(GetFile(LEVEL_MP3));
            }
            else if (RTFile.FileExists(GetFile(AUDIO_OGG)))
            {
                CoreHelper.StartCoroutineAsync(AlephNetworkManager.DownloadAudioClip("file://" + GetFile(AUDIO_OGG), AudioType.OGGVORBIS, delegate (AudioClip audioClip)
                {
                    music = audioClip;
                }));
            }
            else if (RTFile.FileExists(GetFile(AUDIO_WAV)))
            {
                CoreHelper.StartCoroutineAsync(AlephNetworkManager.DownloadAudioClip("file://" + GetFile(AUDIO_WAV), AudioType.WAV, delegate (AudioClip audioClip)
                {
                    music = audioClip;
                }));
            }
            else if (RTFile.FileExists(GetFile(AUDIO_MP3)))
            {
                music = LSFunctions.LSAudio.CreateAudioClipUsingMP3File(GetFile(AUDIO_MP3));
            }
        }

        public IEnumerator LoadAudioClipRoutine(Action onComplete = null)
        {
            if (music)
                yield break;

            if (RTFile.FileExists(GetFile(LEVEL_OGG)))
            {
                yield return CoreHelper.StartCoroutineAsync(AlephNetworkManager.DownloadAudioClip("file://" + GetFile(LEVEL_OGG), AudioType.OGGVORBIS, delegate (AudioClip audioClip)
                {
                    music = audioClip;
                }));
            }
            else if (RTFile.FileExists(GetFile(LEVEL_WAV)))
            {
                yield return CoreHelper.StartCoroutineAsync(AlephNetworkManager.DownloadAudioClip("file://" + GetFile(LEVEL_WAV), AudioType.WAV, delegate (AudioClip audioClip)
                {
                    music = audioClip;
                }));
            }
            else if (RTFile.FileExists(GetFile(LEVEL_MP3)))
            {
                yield return music = LSFunctions.LSAudio.CreateAudioClipUsingMP3File(GetFile(LEVEL_MP3));
            }
            else if (RTFile.FileExists(GetFile(AUDIO_OGG)))
            {
                yield return CoreHelper.StartCoroutineAsync(AlephNetworkManager.DownloadAudioClip("file://" + GetFile(AUDIO_OGG), AudioType.OGGVORBIS, delegate (AudioClip audioClip)
                {
                    music = audioClip;
                }));
            }
            else if (RTFile.FileExists(GetFile(AUDIO_WAV)))
            {
                yield return CoreHelper.StartCoroutineAsync(AlephNetworkManager.DownloadAudioClip("file://" + GetFile(AUDIO_WAV), AudioType.WAV, delegate (AudioClip audioClip)
                {
                    music = audioClip;
                }));
            }
            else if (RTFile.FileExists(GetFile(AUDIO_MP3)))
            {
                yield return music = LSFunctions.LSAudio.CreateAudioClipUsingMP3File(GetFile(AUDIO_MP3));
            }

            onComplete?.Invoke();
        }

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
            RTFile.FileExists(RTFile.CombinePaths(folder, AUDIO_OGG)) || RTFile.FileExists(RTFile.CombinePaths(folder, AUDIO_WAV)) || RTFile.FileExists(RTFile.CombinePaths(folder, AUDIO_MP3)) ||
            RTFile.FileExists(RTFile.CombinePaths(folder, LEVEL_OGG)) || RTFile.FileExists(RTFile.CombinePaths(folder, LEVEL_WAV)) || RTFile.FileExists(RTFile.CombinePaths(folder, LEVEL_MP3));

        /// <summary>
        /// Checks if the level has metadata. Includes LS and VG formats.
        /// </summary>
        /// <param name="folder">The folder to check. Must end with a /.</param>
        /// <returns>True if metadata exists, otherwise false.</returns>
        public static bool VerifyMetadata(string folder) => RTFile.FileExists(RTFile.CombinePaths(folder, "metadata.vgm")) || RTFile.FileExists(RTFile.CombinePaths(folder, "metadata.lsb"));

        /// <summary>
        /// Checks if the level has level data. Includes LS and VG formats.
        /// </summary>
        /// <param name="folder">The folder to check. Must end with a /.</param>
        /// <returns>True if level data exists, otherwise false.</returns>
        public static bool VerifyLevel(string folder) => RTFile.FileExists(RTFile.CombinePaths(folder, "level.vgd")) || RTFile.FileExists(RTFile.CombinePaths(folder, "level.lsb"));

        public override string ToString() => $"{Path.GetFileName(Path.GetDirectoryName(path))} - {id} - {(metadata == null || metadata.song == null ? "" : metadata.song.title)}";

        #endregion
    }
}
