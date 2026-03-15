using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

using UnityEngine;

using LSFunctions;

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
            isInterface = RTFile.FileIsFormat(path, FileFormat.LSI);

            try
            {
                if (CoreConfig.Instance.PrioritizeVG.Value && RTFile.FileExists(GetFile(METADATA_VGM)))
                    metadata = MetaData.ParseVG(JSON.Parse(RTFile.ReadFromFile(GetFile(METADATA_VGM))));
                else if (RTFile.FileExists(GetFile(METADATA_LSB)))
                    metadata = MetaData.Parse(JSON.Parse(RTFile.ReadFromFile(GetFile(METADATA_LSB))));
                else if (RTFile.FileExists(GetFile(METADATA_VGM)))
                    metadata = MetaData.ParseVG(JSON.Parse(RTFile.ReadFromFile(GetFile(METADATA_VGM))));
            }
            catch (Exception ex)
            {
                throw new MetaDataException($"Could not parse the metadata of \'{Path.GetFileName(path)}\'.\nInner exception: {ex}");
            }

            if (loadIcon)
                LoadCover();

            UpdateDefaults();
        }

        public Level(string path, MetaData metadata, bool loadIcon = true)
        {
            this.path = path;
            isInterface = RTFile.FileIsFormat(path, FileFormat.LSI);

            this.metadata = metadata;

            if (loadIcon)
                LoadCover();

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

        public AudioClip previewAudio;

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

        /// <summary>
        /// If the level is an interface.
        /// </summary>
        public bool isInterface;

        #endregion

        /// <summary>
        /// Gets the level paths' folder name.
        /// </summary>
        public string FolderName => string.IsNullOrEmpty(path) ? path : Path.GetFileName(RTFile.RemoveEndSlash(path));

        /// <summary>
        /// Gets the current locked state of the level. Returns true if <see cref="MetaData.requireUnlock"/> is on and the level was not unlocked, otherwise returns false.
        /// </summary>
        public bool Locked => metadata && metadata.requireUnlock && (!saveData || !saveData.Unlocked);

        /// <summary>
        /// The current selected level file.
        /// </summary>
        public string CurrentFile => !string.IsNullOrEmpty(currentFile) ? currentFile : metadata && metadata.package && metadata.package.GetLevel(metadata.package.mainLevel) is PackageMetaData.File file ? file.fileName : Level.LEVEL_LSB;

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

        #region Functions

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
            if (!metadata)
                return;

            if (!string.IsNullOrEmpty(metadata.arcadeID) && metadata.arcadeID != "-1")
                id = metadata.arcadeID;
            else
                id = metadata.beatmap.workshopID.ToString();
        }

        /// <summary>
        /// Loads the levels' cover art.
        /// </summary>
        public void LoadCover()
        {
            if (metadata && metadata.package && metadata.package.GetImage(metadata.package.mainCover) is PackageMetaData.File file)
            {
                icon = RTFile.FileExists(GetFile(file.fileName)) ?
                    SpriteHelper.LoadSprite(GetFile(file.fileName)) :
                    LegacyPlugin.AtanPlaceholder;
            }
            else
                icon =
                    RTFile.FileExists(GetFile(LEVEL_JPG)) ? SpriteHelper.LoadSprite(GetFile(LEVEL_JPG)) :
                    RTFile.FileExists(GetFile(COVER_JPG)) ? SpriteHelper.LoadSprite(GetFile(COVER_JPG)) :
                    LegacyPlugin.AtanPlaceholder;

            if (metadata && metadata.package && metadata.package.GetImage(metadata.package.mainLockedCover) is PackageMetaData.File lockedFile)
                lockedIcon = RTFile.FileExists(GetFile(lockedFile.fileName)) ?
                    SpriteHelper.LoadSprite(GetFile(lockedFile.fileName)) :
                    LegacyPlugin.AtanPlaceholder;
            else
                lockedIcon = RTFile.FileExists(GetFile(LOCKED_JPG)) ? SpriteHelper.LoadSprite(GetFile(LOCKED_JPG)) : null;
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

            tracks.Clear();
            if (!metadata)
            {
                var coroutine = LoadAudioClipFileRoutine("level", audioClip => music = audioClip);
                if (coroutine != null)
                    yield return coroutine;
                coroutine = LoadAudioClipFileRoutine("audio", audioClip => music = audioClip);
                if (coroutine != null)
                    yield return coroutine;
                tracks["audio"] = music;
                yield break;
            }

            for (int i = 0; i < metadata.package.files.Count; i++)
            {
                var file = metadata.package.files[i];
                if (!file.IsAudio)
                    continue;

                var coroutine = LoadAudioClipFileRoutine(file.fileName, audioClip => tracks.TryAdd(file.id, audioClip));
                if (coroutine != null)
                    yield return coroutine;
            }

            if (tracks.TryGetValue(metadata.package.mainAudio, out AudioClip audioClip))
                music = audioClip;
            else
            {
                CoreHelper.Log($"Level [{this}] had to load the main audio the default way.");
                var coroutine = LoadAudioClipFileRoutine("level", audioClip => music = audioClip);
                if (coroutine != null)
                    yield return coroutine;
                coroutine = LoadAudioClipFileRoutine("audio", audioClip => music = audioClip);
                if (coroutine != null)
                    yield return coroutine;
                tracks["audio"] = music;
            }

            if (tracks.TryGetValue(metadata.package.mainPreviewAudio, out AudioClip previewAudioClip))
                previewAudio = previewAudioClip;
            
            onComplete?.Invoke();
        }

        Coroutine LoadAudioClipFileRoutine(string file, Action<AudioClip> callback)
        {
            file = Path.GetFileNameWithoutExtension(file);
            if (RTFile.FileExists(GetFile(file + FileFormat.OGG.Dot())))
                return CoroutineHelper.StartCoroutine(AlephNetwork.DownloadAudioClip("file://" + GetFile(file + FileFormat.OGG.Dot()), AudioType.OGGVORBIS, callback));
            if (RTFile.FileExists(GetFile(file + FileFormat.WAV.Dot())))
                return CoroutineHelper.StartCoroutine(AlephNetwork.DownloadAudioClip("file://" + GetFile(file + FileFormat.WAV.Dot()), AudioType.WAV, callback));
            if (RTFile.FileExists(GetFile(file + FileFormat.MP3.Dot())))
                callback?.Invoke(LSAudio.CreateAudioClipUsingMP3File(GetFile(file + FileFormat.MP3.Dot())));
            return null;
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
        /// Gets the preview audio that should play.
        /// </summary>
        /// <returns>Returns <see cref="previewAudio"/> if it is not null, otherwise returns <see cref="music"/>.</returns>
        public AudioClip GetPreviewAudio() => previewAudio ? previewAudio : music;

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

        public override string ToString() => $"{Path.GetFileName(RTFile.RemoveEndSlash(path))} - {id} - {(!metadata || !metadata.song ? string.Empty : metadata.song.title)}";

        #endregion
    }
}
