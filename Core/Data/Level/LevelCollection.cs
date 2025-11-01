using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using UnityEngine;

using SimpleJSON;
using SteamworksFacepunch.Ugc;

using BetterLegacy.Arcade.Interfaces;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using BetterLegacy.Editor.Data.Elements;
using BetterLegacy.Menus;
using BetterLegacy.Menus.UI.Interfaces;

namespace BetterLegacy.Core.Data.Level
{
    /// <summary>
    /// Stores multiple levels in a specific order. Good for stories.
    /// </summary>
    public class LevelCollection : Exists, IUploadable
    {
        public LevelCollection() => id = PAObjectBase.GetNumberID();

        /// <summary>
        /// Constructs a level collection from a pre-existing queue. This allows the creation of collections in the Arcade menu.
        /// </summary>
        /// <param name="levels">Level queue.</param>
        public LevelCollection(List<Level> levels) : this()
        {
            this.levels = levels;
            for (int i = 0; i < levels.Count; i++)
            {
                var levelInfo = LevelInfo.FromLevel(levels[i]);
                levelInfo.collection = this;
                levelInformation.Add(levelInfo);
            }
        }

        #region Values

        public DifficultyType Difficulty { get => difficulty; set => difficulty = value; }

        /// <summary>
        /// Gets the level that the player first enters when clicking Play.<br>Level is either a hub level or the first in the collection.</br>
        /// </summary>
        public Level EntryLevel => this[EntryLevelIndex];

        /// <summary>
        /// Gets the levels' index that the player first enters when clicking Play.<br>Level is either a hub level or the first in the collection.</br>
        /// </summary>
        public int EntryLevelIndex
        {
            get
            {
                if (levelInformation.InRange(this.entryLevelIndex))
                    return this.entryLevelIndex;

                int entryLevelIndex = levels.FindIndex(x => x && x.metadata != null && x.metadata.isHubLevel && (!x.metadata.requireUnlock || x.saveData && x.saveData.Unlocked));

                if (entryLevelIndex < 0)
                    entryLevelIndex = 0;

                return entryLevelIndex;
            }
        }

        /// <summary>
        /// Total amount of levels in the collection.
        /// </summary>
        public int Count => levels.Count;

        /// <summary>
        /// Gets the level collection paths' folder name.
        /// </summary>
        public string FolderName => string.IsNullOrEmpty(path) ? path : Path.GetFileName(RTFile.RemoveEndSlash(path));

        /// <summary>
        /// Identification number of the collection.
        /// </summary>
        public string id = string.Empty;

        /// <summary>
        /// Name of the collection.
        /// </summary>
        public string name = string.Empty;

        /// <summary>
        /// Description of the collection.
        /// </summary>
        public string description = string.Empty;

        /// <summary>
        /// Creator of the collection / levels within.
        /// </summary>
        public string creator = string.Empty;

        /// <summary>
        /// General difficutly of the collection.
        /// </summary>
        public int difficulty;

        /// <summary>
        /// Full path of the collection. Must end with a "/".
        /// </summary>
        public string path = string.Empty;

        /// <summary>
        /// Icon of the collection.
        /// </summary>
        public Sprite icon;

        /// <summary>
        /// Banner of the collection. To be used for full level screen.
        /// </summary>
        public Sprite banner;

        /// <summary>
        /// Audio to play when viewing the collection.
        /// </summary>
        public AudioClip previewAudio;

        /// <summary>
        /// First level to enter.
        /// </summary>
        public int entryLevelIndex = -1;

        /// <summary>
        /// All levels the collection contains.
        /// </summary>
        public List<Level> levels = new List<Level>();

        /// <summary>
        /// A list of levels that exist in the level collection file, regardless of whether a level was loaded or not.
        /// </summary>
        public List<LevelInfo> levelInformation = new List<LevelInfo>();

        /// <summary>
        /// Saved player data.
        /// </summary>
        public SaveCollectionData saveData;

        /// <summary>
        /// Achievements to be loaded for a collection.
        /// </summary>
        public List<Achievement> achievements = new List<Achievement>();

        /// <summary>
        /// If the collection has no level collection data but contains levels.
        /// </summary>
        public bool isFolder;

        /// <summary>
        /// If level collection is loaded from the editor.
        /// </summary>
        public bool isEditor;

        /// <summary>
        /// The editor level collection panel.
        /// </summary>
        public LevelCollectionPanel editorPanel;

        /// <summary>
        /// If progression is allowed in zen mode.
        /// </summary>
        public bool allowZenProgression;

        #region Server

        /// <summary>
        /// Server ID of the collection.
        /// </summary>
        public string serverID = string.Empty;

        public string ServerID { get => serverID; set => serverID = value; }

        public string UploaderName { get; set; }

        public string UploaderID { get; set; }

        public List<ServerUser> Uploaders { get; set; } = new List<ServerUser>();

        public ServerVisibility Visibility { get; set; }

        public string Changelog { get; set; }

        /// <summary>
        /// Tags used to identify the collection.
        /// </summary>
        public List<string> tags = new List<string>();
        public List<string> ArcadeTags { get => tags; set => tags = value; }

        public string ObjectVersion { get; set; }

        public string dateCreated = string.Empty;
        public string dateEdited = DateTime.Now.ToString(LegacyPlugin.DATE_TIME_FORMAT);
        public string datePublished = string.Empty;
        public int versionNumber;

        public string DatePublished { get => datePublished; set => datePublished = value; }

        public int VersionNumber { get => versionNumber; set => versionNumber = value; }

        #endregion

        #endregion

        #region Constants

        /// <summary>
        /// The collection icon file.
        /// </summary>
        public const string ICON_PNG = "icon.png";
        /// <summary>
        /// The collection icon file.
        /// </summary>
        public const string ICON_JPG = "icon.jpg";

        /// <summary>
        /// The collection banner file.
        /// </summary>
        public const string BANNER_PNG = "banner.png";
        /// <summary>
        /// The collection banner file.
        /// </summary>
        public const string BANNER_JPG = "banner.jpg";

        /// <summary>
        /// The collection file.
        /// </summary>
        public const string COLLECTION_LSCO = "collection.lsco";
        /// <summary>
        /// The collection preview audio file.
        /// </summary>
        public const string PREVIEW_OGG = "preview.ogg";

        #endregion

        #region Indexers

        public Level this[int index]
        {
            get => levels.GetAtOrDefault(index, null);
            set
            {
                if (levels.InRange(index))
                    levels[index] = value;
            }
        }

        public Level this[string id]
        {
            get => levels.Find(x => x.id == id);
            set
            {
                var index = levels.FindIndex(x => x.id == id);
                levels[index] = value;
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Parses a level collection. Levels can be loaded either via path, arcade ID or workshop ID. Ensure this runs after Arcade and/or Steam levels have loaded.
        /// </summary>
        /// <param name="path">Path to the level collection.</param>
        /// <param name="jn">JSON to parse.</param>
        /// <param name="loadLevels">If actual levels should be loaded.</param>
        /// <returns>Returns a parsed level collection.</returns>
        public static LevelCollection Parse(string path, JSONNode jn, bool loadLevels = true, bool loadIcons = true)
        {
            var collection = new LevelCollection();
            collection.id = jn["id"] ?? PAObjectBase.GetNumberID();
            collection.name = jn["name"] ?? string.Empty;
            collection.creator = jn["creator"] ?? string.Empty;
            collection.description = jn["desc"] ?? string.Empty;
            collection.difficulty = jn["difficulty"].AsInt;
            collection.path = path;
            if (jn["entry_level_index"] != null)
                collection.entryLevelIndex = jn["entry_level_index"].AsInt;
            if (jn["allow_zen_progression"] != null)
                collection.allowZenProgression = jn["allow_zen_progression"].AsBool;

            if (!string.IsNullOrEmpty(jn["date_edited"]))
                collection.dateEdited = jn["date_edited"];
            if (!string.IsNullOrEmpty(jn["date_created"]))
                collection.dateCreated = jn["date_created"];
            else
                collection.dateCreated = DateTime.Now.ToString(LegacyPlugin.DATE_TIME_FORMAT);
            if (!string.IsNullOrEmpty(jn["date_published"]))
                collection.datePublished = jn["date_published"];
            if (jn["version_number"] != null)
                collection.versionNumber = jn["version_number"].AsInt;

            collection.ReadUploadableJSON(jn);

            for (int i = 0; i < jn["levels"].Count; i++)
            {
                var jnLevel = jn["levels"][i];
                var levelInfo = LevelInfo.Parse(jnLevel, i);
                levelInfo.collection = collection;
                collection.levelInformation.Add(levelInfo);

                if (loadLevels)
                    collection.LoadLevel(levelInfo);
            }

            if (loadIcons)
                collection.UpdateIcons();

            return collection;
        }

        /// <summary>
        /// Loads all levels from the level information.
        /// </summary>
        public void LoadLevels()
        {
            for (int i = 0; i < levelInformation.Count; i++)
                LoadLevel(levelInformation[i]);
        }

        /// <summary>
        /// Loads a level from level information.
        /// </summary>
        /// <param name="levelInfo">Level information to get the level from.</param>
        public void LoadLevel(LevelInfo levelInfo)
        {
            var basePath = this.path;
            var path =/* CoreHelper.InEditor ? levelInfo.editorPath : */levelInfo.path;

            // load via path
            if (path != null && (RTFile.FileExists(RTFile.CombinePaths(basePath, path, Level.LEVEL_LSB)) || RTFile.FileExists(RTFile.CombinePaths(basePath, path, Level.LEVEL_VGD))))
            {
                var levelFolder = RTFile.CombinePaths(basePath, path);

                MetaData metadata = null;

                if (RTFile.FileExists(RTFile.CombinePaths(levelFolder, Level.METADATA_VGM)))
                    metadata = MetaData.ParseVG(JSON.Parse(RTFile.ReadFromFile(RTFile.CombinePaths(levelFolder, Level.METADATA_VGM))));
                else if (RTFile.FileExists(RTFile.CombinePaths(levelFolder, Level.METADATA_LSB)))
                    metadata = MetaData.Parse(JSON.Parse(RTFile.ReadFromFile(RTFile.CombinePaths(levelFolder, Level.METADATA_LSB))));

                if (!metadata)
                    return;

                metadata.VerifyID(levelFolder);
                levelInfo.level = NewCollectionLevel(levelFolder, this);
            }

            // load via arcade ID
            else if (levelInfo.arcadeID != null && LevelManager.Levels.TryFind(x => x.id == levelInfo.arcadeID, out Level arcadeLevel))
                levelInfo.level = NewCollectionLevel(arcadeLevel.path, this);

            // load via workshop ID
            else if (levelInfo.workshopID != null && RTSteamManager.inst.Levels.TryFind(x => x.id == levelInfo.workshopID, out Level steamLevel))
                levelInfo.level = NewCollectionLevel(steamLevel.path, this);

            if (levelInfo.level)
                levelInfo.Overwrite(levelInfo.level);

            levels.Add(levelInfo.level);
        }

        static Level NewCollectionLevel(string path, LevelCollection levelCollection) => new Level(path) { fromCollection = true, levelCollection = levelCollection };

        /// <summary>
        /// Downloads a level if it doesn't exist.
        /// </summary>
        /// <param name="levelInfo">Level info to obtain a level from.</param>
        public void DownloadLevel(LevelInfo levelInfo, Action<Level> onDownload = null) => DownloadLevel(this, levelInfo, onDownload);

        /// <summary>
        /// Downloads a level if it doesn't exist.
        /// </summary>
        /// <param name="levelInfo">Level info to obtain a level from.</param>
        public static void DownloadLevel(LevelCollection collection, LevelInfo levelInfo, Action<Level> onDownload = null)
        {
            Level level;
            if (!string.IsNullOrEmpty(levelInfo.arcadeID) && LevelManager.Levels.TryFind(x => x.id == levelInfo.arcadeID, out level))
            {
                levelInfo.level = level;
                onDownload?.Invoke(level);
                CoreHelper.Log($"Level {level.id} already exists!");
                return;
            }

            if (!string.IsNullOrEmpty(levelInfo.workshopID))
            {
                if (!RTSteamManager.inst.Initialized)
                {
                    CoreHelper.Log($"Steam was not initialized. Please open Steam.");
                    ArcadeHelper.QuitToArcade();
                    return;
                }

                if (RTSteamManager.inst.Levels.TryFind(x => x.id == levelInfo.workshopID, out level))
                {
                    level = NewCollectionLevel(level.path, collection);
                    levelInfo.Overwrite(level);
                    if (collection)
                        collection[levelInfo.index] = level;
                    levelInfo.level = level;

                    InterfaceManager.inst.CloseMenus();
                    onDownload?.Invoke(level);
                    CoreHelper.Log($"Level {level.id} already exists!");
                    return;
                }

                ConfirmMenu.Init("Level does not exist in your subscribed Steam items. Do you want to subscribe to the level?", () =>
                {
                    CoroutineHelper.StartCoroutine(SubscribeToSteamLevel(collection, levelInfo, onDownload, ArcadeHelper.QuitToArcade));
                }, ArcadeHelper.QuitToArcade);
            }
            else if (!string.IsNullOrEmpty(levelInfo.serverID))
            {
                CoroutineHelper.StartCoroutine(AlephNetwork.DownloadJSONFile($"{AlephNetwork.ArcadeServerURL}api/level/{levelInfo.serverID}", json =>
                {
                    ConfirmMenu.Init("Level does not exist in your level list. Do you want to download it off the Arcade server?", () =>
                    {
                        var jn = JSON.Parse(json);
                        if (jn is JSONObject jsonObject)
                        {
                            if (CoreHelper.InGame)
                            {
                                AlephNetwork.DownloadLevel(jsonObject, level =>
                                {
                                    level = NewCollectionLevel(level.path, collection);
                                    levelInfo.Overwrite(level);
                                    if (collection)
                                        collection[levelInfo.index] = level;
                                    InterfaceManager.inst.CloseMenus();
                                    onDownload?.Invoke(level);
                                }, onError => ArcadeHelper.QuitToArcade());
                                return;
                            }

                            InterfaceManager.inst.CloseMenus();
                            CoroutineHelper.StartCoroutine(AlephNetwork.DownloadBytes($"{AlephNetwork.LevelCoverURL}{levelInfo.serverID}{FileFormat.JPG.Dot()}", bytes =>
                            {
                                var sprite = SpriteHelper.LoadSprite(bytes);
                                ArcadeMenu.OnlineLevelIcons[levelInfo.serverID] = sprite;

                                DownloadLevelMenu.Init(jsonObject);
                                DownloadLevelMenu.Current.onDownloadComplete = level =>
                                {
                                    level = NewCollectionLevel(level.path, collection);
                                    levelInfo.Overwrite(level);
                                    if (collection)
                                        collection[levelInfo.index] = level;

                                    InterfaceManager.inst.CloseMenus();
                                    if (onDownload != null)
                                    {
                                        onDownload(level);
                                        return;
                                    }

                                    PlayLevelMenu.Init(level);
                                };
                            }, onError =>
                            {
                                ArcadeMenu.OnlineLevelIcons[levelInfo.serverID] = LegacyPlugin.AtanPlaceholder;

                                DownloadLevelMenu.Init(jsonObject);
                                DownloadLevelMenu.Current.onDownloadComplete = level =>
                                {
                                    level = NewCollectionLevel(level.path, collection);
                                    levelInfo.Overwrite(level);
                                    if (collection)
                                        collection[levelInfo.index] = level;

                                    InterfaceManager.inst.CloseMenus();
                                    if (onDownload != null)
                                    {
                                        onDownload(level);
                                        return;
                                    }

                                    PlayLevelMenu.Init(level);
                                };
                            }));
                        }
                    }, ArcadeHelper.QuitToArcade);
                }, (string onError, long responseCode, string errorMsg) => ArcadeHelper.QuitToArcade()));
            }
        }

        static IEnumerator SubscribeToSteamLevel(LevelCollection collection, LevelInfo levelInfo, Action<Level> onDownload = null, Action onFail = null)
        {
            var workshopID = RTSteamManager.GetWorkshopID(levelInfo.workshopID);
            if (workshopID.Value == 0)
            {
                InterfaceManager.inst.CloseMenus();
                if (CoreHelper.InGame)
                {
                    ArcadeHelper.QuitToArcade();
                    yield break;
                }
                onFail?.Invoke();
                yield break;
            }

            CoreHelper.Log($"Updating {workshopID}");
            yield return RTSteamManager.GetItem(workshopID, item =>
            {
                CoreHelper.Log($"Got item: {item}");
                CoroutineHelper.StartCoroutineAsync(AlephNetwork.DownloadBytes(item.PreviewImageUrl, bytes =>
                {
                    LegacyPlugin.MainTick += () =>
                    {
                        var sprite = SpriteHelper.LoadSprite(bytes);
                        ArcadeMenu.OnlineSteamLevelIcons[levelInfo.workshopID] = sprite;
                        InitSteamItem(collection, levelInfo, onDownload, item);
                    };
                }, onError =>
                {
                    LegacyPlugin.MainTick += () =>
                    {
                        var sprite = LegacyPlugin.AtanPlaceholder;
                        ArcadeMenu.OnlineSteamLevelIcons[levelInfo.workshopID] = sprite;
                        InitSteamItem(collection, levelInfo, onDownload, item);
                    };
                }));

            }, onFail);
        }

        static void InitSteamItem(LevelCollection collection, LevelInfo levelInfo, Action<Level> onDownload, Item item)
        {
            CoreHelper.Log($"Init Steam Item: {item}");
            if (CoreHelper.InGame)
            {
                CoroutineHelper.StartCoroutine(RTSteamManager.inst.ToggleSubscribedState(item, level =>
                {
                    level = NewCollectionLevel(level.path, collection);
                    levelInfo.Overwrite(level);
                    if (collection)
                        collection[levelInfo.index] = level;

                    InterfaceManager.inst.CloseMenus();
                    onDownload?.Invoke(level);
                }));
                return;
            }

            InterfaceManager.inst.CloseMenus();
            SteamLevelMenu.Init(item);
            SteamLevelMenu.Current.onSubscribedLevel = level =>
            {
                level = NewCollectionLevel(level.path, collection);
                levelInfo.Overwrite(level);
                if (collection)
                    collection[levelInfo.index] = level;

                InterfaceManager.inst.CloseMenus();
                if (onDownload != null)
                {
                    onDownload(level);
                    return;
                }

                PlayLevelMenu.Init(level);
            };
        }

        /// <summary>
        /// Loads the collections' achievements.
        /// </summary>
        public void LoadAchievements()
        {
            var achievementsPath = RTFile.CombinePaths(path, Level.ACHIEVEMENTS_LSA);
            if (!RTFile.FileExists(achievementsPath))
                return;

            var jn = JSON.Parse(RTFile.ReadFromFile(achievementsPath));
            for (int i = 0; i < jn["achievements"].Count; i++)
            {
                var achievement = Achievement.Parse(jn["achievements"][i]);
                if (jn["achievements"][i]["icon_path"] != null)
                    achievement.CheckIconPath(RTFile.CombinePaths(path, jn["achievements"][i]["icon_path"]));
                if (jn["achievements"][i]["locked_icon_path"] != null)
                    achievement.CheckIconPath(RTFile.CombinePaths(path, jn["achievements"][i]["locked_icon_path"]));
                achievements.Add(achievement);
            }
        }

        /// <summary>
        /// Updates the icons of the collection.
        /// </summary>
        public void UpdateIcons()
        {
            icon =
                RTFile.FileExists(RTFile.CombinePaths(path, ICON_PNG)) ? SpriteHelper.LoadSprite(RTFile.CombinePaths(path, ICON_PNG)) :
                RTFile.FileExists(RTFile.CombinePaths(path, ICON_JPG)) ? SpriteHelper.LoadSprite(RTFile.CombinePaths(path, ICON_JPG)) : LegacyPlugin.AtanPlaceholder;
            banner =
                RTFile.FileExists(RTFile.CombinePaths(path, BANNER_PNG)) ? SpriteHelper.LoadSprite(RTFile.CombinePaths(path, BANNER_PNG)) :
                RTFile.FileExists(RTFile.CombinePaths(path, BANNER_JPG)) ? SpriteHelper.LoadSprite(RTFile.CombinePaths(path, BANNER_JPG)) : LegacyPlugin.AtanPlaceholder;
        }

        /// <summary>
        /// Moves a levels' order.
        /// </summary>
        /// <param name="id">ID of a level to move.</param>
        /// <param name="moveTo">Index to move to.</param>
        public void Move(string id, int moveTo)
        {
            levels.Move(x => x.id == id, moveTo);
            levelInformation.Move(x => x.id == id, moveTo);
            levelInformation[moveTo].index = moveTo;
        }

        /// <summary>
        /// Saves the level collection.
        /// </summary>
        /// <param name="saveIcons">If icons should be saved.</param>
        /// <param name="jpg">If icons should be saved as JPG.</param>
        public void Save(bool saveIcons = true, bool jpg = true)
        {
            var jn = Parser.NewJSONObject();

            jn["id"] = id ?? PAObjectBase.GetNumberID();
            if (!string.IsNullOrEmpty(name))
                jn["name"] = name;
            if (!string.IsNullOrEmpty(creator))
                jn["creator"] = creator;
            if (!string.IsNullOrEmpty(description))
                jn["desc"] = description;
            if (difficulty != 0)
                jn["difficulty"] = difficulty;
            if (levelInformation.InRange(entryLevelIndex))
                jn["entry_level_index"] = entryLevelIndex;
            if (allowZenProgression)
                jn["allow_zen_progression"] = allowZenProgression;

            jn["date_created"] = dateCreated;
            jn["date_edited"] = DateTime.Now.ToString(LegacyPlugin.DATE_TIME_FORMAT);
            if (!string.IsNullOrEmpty(datePublished))
                jn["date_published"] = datePublished;
            if (versionNumber != 0)
                jn["version_number"] = versionNumber;

            this.WriteUploadableJSON(jn);

            for (int i = 0; i < levelInformation.Count; i++)
                jn["levels"][i] = levelInformation[i].ToJSON();

            if (saveIcons)
                SaveIcons(jpg);

            RTFile.CreateDirectory(path);

            RTFile.WriteToFile(RTFile.CombinePaths(path, COLLECTION_LSCO), jn.ToString(3));
        }

        /// <summary>
        /// Saves the level collections images.
        /// </summary>
        /// <param name="jpg">If icons should be saved as JPG.</param>
        public void SaveIcons(bool jpg = true)
        {
            if (icon)
                SpriteHelper.SaveSprite(icon, RTFile.CombinePaths(path, jpg ? ICON_JPG : ICON_PNG));
            if (banner)
                SpriteHelper.SaveSprite(banner, RTFile.CombinePaths(path, jpg ? BANNER_JPG : BANNER_PNG));
        }

        /// <summary>
        /// Adds a <see cref="Level"/> to the level collection.
        /// </summary>
        /// <param name="level">Level to add.</param>
        public void AddLevel(Level level)
        {
            if (!levels.IsEmpty() && levels.Any(x => x && x.id == level.id)) // don't want to have duplicate levels
                return;

            var actualLevel = NewCollectionLevel(level.path, this);
            var levelInfo = LevelInfo.FromLevel(actualLevel);
            levelInfo.collection = this;
            levelInfo.index = levelInformation.Count;
            var id = levelInfo.id;
            actualLevel.id = id;
            if (actualLevel.metadata)
                actualLevel.metadata.arcadeID = id;

            levels.Add(actualLevel);
            levelInformation.Add(levelInfo);
        }

        /// <summary>
        /// Adds a <see cref="Level"/> to the level collection and copies its folder to the level collection folder.
        /// </summary>
        /// <param name="level">Level to add.</param>
        public void AddLevelToFolder(Level level)
        {
            if (!levels.IsEmpty() && levels.Any(x => x && x.id == level.id)) // don't want to have duplicate levels
                return;

            var path = RTFile.RemoveEndSlash(level.path);
            var folderName = Path.GetFileName(path);
            var levelPath = RTFile.CombinePaths(this.path, folderName);

            var files = Directory.GetFiles(level.path, "*", SearchOption.AllDirectories);
            for (int i = 0; i < files.Length; i++)
            {
                var file = files[i];
                var copyToPath = RTFile.ReplaceSlash(file).Replace(level.path, levelPath);
                RTFile.CreateDirectory(Path.GetDirectoryName(copyToPath));
                RTFile.CopyFile(file, copyToPath);
            }

            var actualLevel = NewCollectionLevel(levelPath, this);
            var levelInfo = LevelInfo.FromLevel(actualLevel);
            levelInfo.collection = this;
            levelInfo.index = levelInformation.Count;
            var id = levelInfo.id;
            actualLevel.id = id;
            if (actualLevel.metadata)
                actualLevel.metadata.arcadeID = id;
            levelInfo.path = folderName;

            levels.Add(actualLevel);
            levelInformation.Add(levelInfo);
        }

        /// <summary>
        /// Removes a <see cref="Level"/> from the level collection and deletes its folder.
        /// </summary>
        /// <param name="level">Level to remove.</param>
        public void RemoveLevelFromFolder(Level level)
        {
            if (!levels.TryFind(x => x.id == level.id, out Level actualLevel))
                return;

            RTFile.DeleteDirectory(RTFile.RemoveEndSlash(actualLevel.path));

            levels.RemoveAll(x => x.id == level.id);
            levelInformation.RemoveAll(x => x.id == level.id);

            for (int i = 0; i < levelInformation.Count; i++)
                levelInformation[i].index = i;
        }

        public void Remove(LevelInfo levelInfo, bool deleteFiles = true)
        {
            if (deleteFiles)
            {
                if (!string.IsNullOrEmpty(levelInfo.path))
                    RTFile.DeleteDirectory(RTFile.CombinePaths(path, levelInfo.path));
                if (!string.IsNullOrEmpty(levelInfo.editorPath))
                    RTFile.DeleteDirectory(RTFile.CombinePaths(path, levelInfo.editorPath));
            }
            Remove(levelInfo.id);
        }

        public void Remove(string id)
        {
            levels.RemoveAll(x => x && x.id == id);
            levelInformation.RemoveAll(x => x && x.id == id);

            for (int i = 0; i < levelInformation.Count; i++)
                levelInformation[i].index = i;
        }

        /// <summary>
        /// Checks if all files required to load a level collection exist.
        /// </summary>
        /// <param name="folder">The folder to check. Must end with a /.</param>
        /// <returns>Returns true if all files are validated, otherwise false.</returns>
        public static bool Verify(string folder) => VerifyLevel(folder);

        /// <summary>
        /// Checks if all necessary level collection files exist in a folder, and outputs a level collection.
        /// </summary>
        /// <param name="folder">Folder to check.</param>
        /// <param name="loadIcons">If the icons should be loaded.</param>
        /// <param name="level">The output level collection.</param>
        /// <returns>Returns true if a level collection was found, otherwise returns false.</returns>
        public static bool TryVerify(string folder, bool loadIcons, out LevelCollection level)
        {
            var verify = Verify(folder);
            level = verify ? Parse(folder, JSON.Parse(RTFile.ReadFromFile(RTFile.CombinePaths(folder, COLLECTION_LSCO))), loadIcons: loadIcons) : null;
            return verify;
        }

        /// <summary>
        /// Checks if the level collection has level collection data.
        /// </summary>
        /// <param name="folder">The folder to check.</param>
        /// <returns>Returns true if level data exists, otherwise false.</returns>
        public static bool VerifyLevel(string folder) => RTFile.FileExists(RTFile.CombinePaths(folder, COLLECTION_LSCO));

        /// <summary>
        /// Gets the rank of the collection.
        /// </summary>
        /// <returns>Returns the rank.</returns>
        public Rank GetRank()
        {
            var hits = -1;
            for (int i = 0; i < levelInformation.Count; i++)
            {
                var levelInfo = levelInformation[i];

                var saveData = LevelManager.GetSaveData(levelInfo.id);
                var levelHits = saveData?.Hits ?? -1;
                if (levelHits > 0)
                {
                    if (hits < -1)
                        hits = 0;

                    hits += levelHits;
                }
            }
            return LevelManager.GetLevelRank(hits);
        }

        #endregion
    }
}
