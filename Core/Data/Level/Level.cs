using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

using UnityEngine;

using SimpleJSON;

using BetterLegacy.Configs;
using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using BetterLegacy.Editor.Data.Elements;

namespace BetterLegacy.Core.Data.Level
{
    /// <summary>
    /// Stores data to be used for playing a level in the <see cref="SceneName.Game"/> scene.
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
                metadata = MetaData.Parse(JSON.Parse(RTFile.ReadFromFile(GetFile(METADATA_LSB))));
            else if (RTFile.FileExists(GetFile(METADATA_VGM)))
                metadata = MetaData.ParseVG(JSON.Parse(RTFile.ReadFromFile(GetFile(METADATA_VGM))));

            if (loadIcon)
                icon =
                    RTFile.FileExists(GetFile(LEVEL_JPG)) ? SpriteHelper.LoadSprite(GetFile(LEVEL_JPG)) :
                    RTFile.FileExists(GetFile(COVER_JPG)) ? SpriteHelper.LoadSprite(GetFile(COVER_JPG)) :
                    LegacyPlugin.AtanPlaceholder;

            UpdateDefaults();
        }

        public Level(string path, MetaData metadata, bool loadIcon = true)
        {
            this.path = path;

            this.metadata = metadata;

            if (loadIcon)
            {
                icon =
                    RTFile.FileExists(GetFile(LEVEL_JPG)) ? SpriteHelper.LoadSprite(GetFile(LEVEL_JPG)) :
                    RTFile.FileExists(GetFile(COVER_JPG)) ? SpriteHelper.LoadSprite(GetFile(COVER_JPG)) :
                    LegacyPlugin.AtanPlaceholder;

                lockedIcon = RTFile.FileExists(GetFile(LOCKED_JPG)) ? SpriteHelper.LoadSprite(GetFile(LOCKED_JPG)) : null;
            }    

            UpdateDefaults();
        }

        #region Values

        /// <summary>
        /// Unique Arcade / Steam Workshop ID.
        /// </summary>
        public string id;

        /// <summary>
        /// The path to the level folder.
        /// </summary>
        public string path;

        /// <summary>
        /// The current level file to load.
        /// </summary>
        public string currentFile;

        /// <summary>
        /// Icon of the level.
        /// </summary>
        public Sprite icon;

        /// <summary>
        /// Locked icon of the level.
        /// </summary>
        public Sprite lockedIcon;

        /// <summary>
        /// Song the level plays.
        /// </summary>
        public AudioClip music;

        /// <summary>
        /// List of tracks to load from.
        /// </summary>
        public Dictionary<string, AudioClip> tracks = new Dictionary<string, AudioClip>();

        /// <summary>
        /// MetaData of the level.
        /// </summary>
        public MetaData metadata;

        /// <summary>
        /// Saved player data, used for ranking a level.
        /// </summary>
        public SaveData saveData;

        /// <summary>
        /// Achievements to be loaded for a level.
        /// </summary>
        public List<Achievement> achievements = new List<Achievement>();

        /// <summary>
        /// Reference level collection.
        /// </summary>
        public LevelCollection levelCollection;

        #region Level Context

        /// <summary>
        /// If level is from a collection.
        /// </summary>
        public bool fromCollection;

        /// <summary>
        /// If level is <see cref="Story.StoryLevel"/>.
        /// </summary>
        public bool isStory;

        /// <summary>
        /// If level is loaded from the editor.
        /// </summary>
        public bool isEditor;

        /// <summary>
        /// Steam Workshop item reference.
        /// </summary>
        public SteamworksFacepunch.Ugc.Item? steamItem;

        /// <summary>
        /// If level is from the Steam Workshop.
        /// </summary>
        public bool isSteamLevel;

        /// <summary>
        /// The editor level panel.
        /// </summary>
        public LevelPanel editorLevelPanel;

        /// <summary>
        /// Info of the level for a level collection.
        /// </summary>
        public LevelInfo collectionInfo;

        /// <summary>
        /// If achievements have been loaded.
        /// </summary>
        public bool loadedAchievements;

        #endregion

        /// <summary>
        /// Gets the level paths' folder name.
        /// </summary>
        public string FolderName => string.IsNullOrEmpty(path) ? path : Path.GetFileName(RTFile.RemoveEndSlash(path));

        /// <summary>
        /// Gets the current locked state of the level. Returns true if <see cref="MetaData.requireUnlock"/> is on and the level was not unlocked, otherwise returns false.
        /// </summary>
        public bool Locked => metadata != null && metadata.requireUnlock && (!saveData || !saveData.Unlocked);

        /// <summary>
        /// The level files. (level.lsb, level.vgd, etc)
        /// </summary>
        public string[] LevelFiles { get; set; }
        /// <summary>
        /// The current selected level file.
        /// </summary>
        public string CurrentFile => !string.IsNullOrEmpty(currentFile) ? currentFile : LevelFiles[Mathf.Clamp(LevelManager.CurrentLevelMode, 0, LevelFiles.Length - 1)];

        /// <summary>
        /// The type of Project Arrhythmia the level comes from.
        /// </summary>
        public ArrhythmiaType ArrhythmiaType => isStory ? ArrhythmiaType.LS : RTFile.GetFileFormat(CurrentFile).ToArrhythmiaType();

        /// <summary>
        /// If the current selected file is a VG format.
        /// </summary>
        public bool IsVG => ArrhythmiaType == ArrhythmiaType.VG;

        /// <summary>
        /// If the ID is invalid.
        /// </summary>
        public bool InvalidID => string.IsNullOrEmpty(id) || id == "0" || id == "-1";

        /// <summary>
        /// If the level has no set icon, therefore it has the default icon.
        /// </summary>
        public bool HasNoIcon => icon == LegacyPlugin.AtanPlaceholder;

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
        /// The locked cover file.
        /// </summary>
        public const string LOCKED_JPG = "locked.jpg";

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

        /// <summary>
        /// The Players file in BetterLegacy.
        /// </summary>
        public const string PLAYERS_LSB = "players.lsb";

        /// <summary>
        /// The files index file in BetterLegacy.
        /// </summary>
        public const string FILES_LSF = "files.lsf";

        /// <summary>
        /// The editor file.
        /// </summary>
        public const string EDITOR_LSE = "editor.lse";

        /// <summary>
        /// The achievements list file.
        /// </summary>
        public const string ACHIEVEMENTS_LSA = "achievements.lsa";

        #endregion

        #region Methods

        /// <summary>
        /// Combines the file name with the levels' path.
        /// </summary>
        /// <param name="fileName">File to combine.</param>
        /// <returns>Returns a combined path.</returns>
        public string GetFile(string fileName) => RTFile.CombinePaths(path, fileName);

        /// <summary>
        /// Updates the default values of the level.
        /// </summary>
        public void UpdateDefaults()
        {
            if (metadata)
            {
                if (!string.IsNullOrEmpty(metadata.arcadeID) && metadata.arcadeID != "-1")
                    id = metadata.arcadeID;
                else
                    id = metadata.beatmap.workshopID.ToString();
            }

            var defaultFile = (!CoreHelper.InEditor || !RTFile.FileExists(GetFile(LEVEL_LSB))) && CoreConfig.Instance.PrioritizeVG.Value && RTFile.FileExists(GetFile(LEVEL_VGD)) ? LEVEL_VGD : LEVEL_LSB;

            if (RTFile.FileExists(GetFile(FILES_LSF)))
            {
                var jn = JSON.Parse(RTFile.ReadFromFile(GetFile(FILES_LSF)));
                LevelFiles = new string[jn["paths"].Count + 1];
                LevelFiles[0] = defaultFile;
                for (int i = 1; i < jn["paths"].Count + 1; i++)
                    LevelFiles[i] = jn["paths"][i - 1];
            }
            else
                LevelFiles = new string[1] { defaultFile, };
        }

        /// <summary>
        /// Loads the levels' song.
        /// </summary>
        public void LoadAudioClip()
        {
            if (music)
                return;

            if (RTFile.FileExists(GetFile(LEVEL_OGG)))
                CoroutineHelper.StartCoroutine(AlephNetwork.DownloadAudioClip("file://" + GetFile(LEVEL_OGG), AudioType.OGGVORBIS, audioClip => music = audioClip));
            else if (RTFile.FileExists(GetFile(LEVEL_WAV)))
                CoroutineHelper.StartCoroutine(AlephNetwork.DownloadAudioClip("file://" + GetFile(LEVEL_WAV), AudioType.WAV, audioClip => music = audioClip));
            else if (RTFile.FileExists(GetFile(LEVEL_MP3)))
                music = LSFunctions.LSAudio.CreateAudioClipUsingMP3File(GetFile(LEVEL_MP3));
            else if (RTFile.FileExists(GetFile(AUDIO_OGG)))
                CoroutineHelper.StartCoroutine(AlephNetwork.DownloadAudioClip("file://" + GetFile(AUDIO_OGG), AudioType.OGGVORBIS, audioClip => music = audioClip));
            else if (RTFile.FileExists(GetFile(AUDIO_WAV)))
                CoroutineHelper.StartCoroutine(AlephNetwork.DownloadAudioClip("file://" + GetFile(AUDIO_WAV), AudioType.WAV, audioClip => music = audioClip));
            else if (RTFile.FileExists(GetFile(AUDIO_MP3)))
                music = LSFunctions.LSAudio.CreateAudioClipUsingMP3File(GetFile(AUDIO_MP3));
        }

        /// <summary>
        /// Loads the levels' song.
        /// </summary>
        /// <param name="onComplete">Function to run when loading has completed or if the song was already loaded..</param>
        public IEnumerator LoadAudioClipRoutine(Action onComplete = null)
        {
            if (music)
            {
                onComplete?.Invoke();
                yield break;
            }

            if (RTFile.FileExists(GetFile(LEVEL_OGG)))
                yield return CoroutineHelper.StartCoroutine(AlephNetwork.DownloadAudioClip("file://" + GetFile(LEVEL_OGG), AudioType.OGGVORBIS, audioClip => music = audioClip));
            else if (RTFile.FileExists(GetFile(LEVEL_WAV)))
                yield return CoroutineHelper.StartCoroutine(AlephNetwork.DownloadAudioClip("file://" + GetFile(LEVEL_WAV), AudioType.WAV, audioClip => music = audioClip));
            else if (RTFile.FileExists(GetFile(LEVEL_MP3)))
                music = LSFunctions.LSAudio.CreateAudioClipUsingMP3File(GetFile(LEVEL_MP3));
            else if (RTFile.FileExists(GetFile(AUDIO_OGG)))
                yield return CoroutineHelper.StartCoroutine(AlephNetwork.DownloadAudioClip("file://" + GetFile(AUDIO_OGG), AudioType.OGGVORBIS, audioClip => music = audioClip));
            else if (RTFile.FileExists(GetFile(AUDIO_WAV)))
                yield return CoroutineHelper.StartCoroutine(AlephNetwork.DownloadAudioClip("file://" + GetFile(AUDIO_WAV), AudioType.WAV, audioClip => music = audioClip));
            else if (RTFile.FileExists(GetFile(AUDIO_MP3)))
                music = LSFunctions.LSAudio.CreateAudioClipUsingMP3File(GetFile(AUDIO_MP3));

            onComplete?.Invoke();
        }

        /// <summary>
        /// Loads the levels' game data.
        /// </summary>
        /// <param name="parseThemes">If the theme list should be replaced with the game datas' themes.</param>
        /// <returns>Returns a parsed game data from the level folder.</returns>
        public virtual GameData LoadGameData()
        {
            var path = GetFile(CurrentFile);
            var rawJSON = RTFile.ReadFromFile(path);
            var version = metadata.Version;

            if (ProjectArrhythmia.RequireUpdate(version))
                rawJSON = LevelManager.UpdateBeatmap(rawJSON, version);

            var jn = JSON.Parse(rawJSON);
            return IsVG ? GameData.ParseVG(jn, version) : GameData.Parse(jn);
        }

        /// <summary>
        /// Loads the levels' achievements.
        /// </summary>
        public void LoadAchievements()
        {
            achievements.Clear();
            loadedAchievements = false;
            var achievementsPath = GetFile(ACHIEVEMENTS_LSA);
            if (!RTFile.FileExists(achievementsPath))
                return;

            try
            {
                var jn = JSON.Parse(RTFile.ReadFromFile(achievementsPath));
                for (int i = 0; i < jn["achievements"].Count; i++)
                {
                    var achievement = Achievement.Parse(jn["achievements"][i]);
                    if (jn["achievements"][i]["icon_path"] != null)
                        achievement.CheckIconPath(GetFile(jn["achievements"][i]["icon_path"]));
                    if (jn["achievements"][i]["locked_icon_path"] != null)
                        achievement.CheckIconPath(GetFile(jn["achievements"][i]["locked_icon_path"]));

                    achievement.unlocked =
                        saveData && saveData.UnlockedAchievements != null && saveData.UnlockedAchievements.TryGetValue(achievement.id, out bool unlocked) && unlocked ||
                        AchievementManager.unlockedCustomAchievements.TryGetValue(achievement.id, out bool customUnlocked) && customUnlocked;

                    achievements.Add(achievement);
                }

                loadedAchievements = true;
            }
            catch (Exception ex)
            {
                CoreHelper.LogError($"Couldn't load achievements due to the exception: {ex}");
            }
        }

        /// <summary>
        /// Gets the levels' achievements.
        /// </summary>
        /// <returns>Returns a list of the achievements in the level and the levels collection, if it exists.</returns>
        public List<Achievement> GetAchievements()
        {
            var list = achievements;
            var levelCollection = this.levelCollection ?? LevelManager.CurrentLevelCollection;
            if (levelCollection && levelCollection.achievements != null)
                list.AddRange(levelCollection.achievements);
            return list;
        }

        /// <summary>
        /// Checks if all files required to load a level exist. Includes LS / VG formats.
        /// </summary>
        /// <param name="folder">The folder to check. Must end with a /.</param>
        /// <returns>Returns true if all files are validated, otherwise false.</returns>
        public static bool Verify(string folder) => VerifySong(folder) && VerifyMetadata(folder) && VerifyLevel(folder);

        /// <summary>
        /// Checks if all necessary level files exist in a folder, and outputs a level.
        /// </summary>
        /// <param name="folder">Folder to check.</param>
        /// <param name="loadIcon">If the icon should be loaded.</param>
        /// <param name="level">The output level.</param>
        /// <returns>Returns true if a level was found, otherwise returns false.</returns>
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
        /// <returns>Returns true if a song exists, otherwise false.</returns>
        public static bool VerifySong(string folder) =>
            RTFile.FileExists(RTFile.CombinePaths(folder, AUDIO_OGG)) || RTFile.FileExists(RTFile.CombinePaths(folder, AUDIO_WAV)) || RTFile.FileExists(RTFile.CombinePaths(folder, AUDIO_MP3)) ||
            RTFile.FileExists(RTFile.CombinePaths(folder, LEVEL_OGG)) || RTFile.FileExists(RTFile.CombinePaths(folder, LEVEL_WAV)) || RTFile.FileExists(RTFile.CombinePaths(folder, LEVEL_MP3));

        /// <summary>
        /// Checks if the level has metadata. Includes LS and VG formats.
        /// </summary>
        /// <param name="folder">The folder to check.</param>
        /// <returns>Returns true if metadata exists, otherwise false.</returns>
        public static bool VerifyMetadata(string folder) => RTFile.FileExists(RTFile.CombinePaths(folder, METADATA_VGM)) || RTFile.FileExists(RTFile.CombinePaths(folder, METADATA_LSB));

        /// <summary>
        /// Checks if the level has level data. Includes LS and VG formats.
        /// </summary>
        /// <param name="folder">The folder to check.</param>
        /// <returns>Returns true if level data exists, otherwise false.</returns>
        public static bool VerifyLevel(string folder) => RTFile.FileExists(RTFile.CombinePaths(folder, LEVEL_VGD)) || RTFile.FileExists(RTFile.CombinePaths(folder, LEVEL_LSB));

        public override string ToString() => $"{Path.GetFileName(RTFile.RemoveEndSlash(path))} - {id} - {(metadata == null || metadata.song == null ? "" : metadata.song.title)}";

        #endregion
    }
}
