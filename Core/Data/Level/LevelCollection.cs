using BetterLegacy.Arcade;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Data.Level;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using BetterLegacy.Core.Managers.Networking;
using BetterLegacy.Menus;
using BetterLegacy.Menus.UI.Interfaces;
using LSFunctions;
using SimpleJSON;
using SteamworksFacepunch.Ugc;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace BetterLegacy.Core.Data.Level
{
    /// <summary>
    /// Stores multiple levels in a specific order. Good for stories.
    /// </summary>
    public class LevelCollection : Exists
    {
        public LevelCollection() => id = LSText.randomNumString(16);

        /// <summary>
        /// Constructs a level collection from a pre-existing queue. This allows the creation of collections in the Arcade menu.
        /// </summary>
        /// <param name="levels">Level queue.</param>
        public LevelCollection(List<Level> levels) : this()
        {
            this.levels = levels;
            for (int i = 0; i < levels.Count; i++)
                levelInformation.Add(LevelInfo.FromLevel(levels[i]));
        }

        #region Fields

        /// <summary>
        /// Identification number of the collection.
        /// </summary>
        public string id;

        /// <summary>
        /// Server ID of the collection.
        /// </summary>
        public string serverID;

        /// <summary>
        /// Name of the collection.
        /// </summary>
        public string name;

        /// <summary>
        /// Description of the collection.
        /// </summary>
        public string description;

        /// <summary>
        /// Creator of the collection / levels within.
        /// </summary>
        public string creator;

        /// <summary>
        /// Tags used to identify the collection.
        /// </summary>
        public string[] tags;

        /// <summary>
        /// Full path of the collection. Must end with a "/".
        /// </summary>
        public string path;

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
        /// All levels the collection contains.
        /// </summary>
        public List<Level> levels = new List<Level>();

        /// <summary>
        /// A list of levels that exist in the level collection file, regardless of whether a level was loaded or not.
        /// </summary>
        public List<LevelInfo> levelInformation = new List<LevelInfo>();

        /// <summary>
        /// Achievements to be loaded for a collection.
        /// </summary>
        public List<Achievement> achievements = new List<Achievement>();

        #endregion

        #region Properties

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
                int entryLevelIndex = levels.FindIndex(x => x.metadata != null && x.metadata.isHubLevel && (!x.metadata.requireUnlock || x.playerData != null && x.playerData.Unlocked));

                if (entryLevelIndex < 0)
                    entryLevelIndex = 0;

                return entryLevelIndex;
            }
        }

        /// <summary>
        /// Total amount of levels in the collection.
        /// </summary>
        public int Count => levels.Count;

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
            get => levels[index];
            set => levels[index] = value;
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
        public static LevelCollection Parse(string path, JSONNode jn, bool loadLevels = true)
        {
            var collection = new LevelCollection();
            collection.id = jn["id"];
            collection.serverID = jn["server_id"];
            collection.name = jn["name"];
            collection.creator = jn["creator"];
            collection.description = jn["desc"];
            collection.path = path;

            if (jn["tags"] != null)
            {
                collection.tags = new string[jn["tags"].Count];
                for (int i = 0; i < jn["tags"].Count; i++)
                    collection.tags[i] = jn["tags"][i];
            }

            for (int i = 0; i < jn["levels"].Count; i++)
            {
                var jnLevel = jn["levels"][i];
                var levelInfo = LevelInfo.Parse(jnLevel, i);
                collection.levelInformation.Add(levelInfo);

                if (!loadLevels)
                    continue;

                var jnPath = jnLevel["path"];

                // load via path
                if (jnPath != null && (RTFile.FileExists(RTFile.CombinePaths(path, jnPath, Level.LEVEL_LSB)) || RTFile.FileExists(RTFile.CombinePaths(path, jnPath, Level.LEVEL_VGD))))
                {
                    var levelFolder = RTFile.CombinePaths(path, jnPath);

                    MetaData metadata = null;

                    if (RTFile.FileExists(RTFile.CombinePaths(levelFolder, Level.METADATA_VGM)))
                        metadata = MetaData.ParseVG(JSON.Parse(RTFile.ReadFromFile(RTFile.CombinePaths(levelFolder, Level.METADATA_VGM))));
                    else if (RTFile.FileExists(RTFile.CombinePaths(levelFolder, Level.METADATA_LSB)))
                        metadata = MetaData.Parse(JSON.Parse(RTFile.ReadFromFile(RTFile.CombinePaths(levelFolder, Level.METADATA_LSB))), false);

                    if (!metadata)
                        continue;

                    if ((string.IsNullOrEmpty(metadata.arcadeID) || metadata.arcadeID.Contains("-") /* < don't want negative IDs */ || metadata.arcadeID == "0"))
                    {
                        metadata.arcadeID = LSText.randomNumString(16);
                        var metadataJN = metadata.ToJSON();
                        RTFile.WriteToFile(RTFile.CombinePaths(levelFolder, Level.METADATA_LSB), metadataJN.ToString(3));
                    }

                    levelInfo.level = NewCollectionLevel(levelFolder);
                }

                // load via arcade ID
                else if (jnLevel["arcade_id"] != null && LevelManager.Levels.TryFind(x => x.id == jnLevel["arcade_id"], out Level arcadeLevel))
                    levelInfo.level = NewCollectionLevel(arcadeLevel.path);

                // load via workshop ID
                else if (jnLevel["workshop_id"] != null && SteamWorkshopManager.inst.Levels.TryFind(x => x.id == jnLevel["workshop_id"], out Level steamLevel))
                    levelInfo.level = NewCollectionLevel(steamLevel.path);

                if (levelInfo.level)
                    levelInfo.Overwrite(levelInfo.level);

                collection.levels.Add(levelInfo.level);
            }

            collection.UpdateIcons();

            return collection;
        }

        static Level NewCollectionLevel(string path) => new Level(path) { fromCollection = true };

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
                if (!SteamWorkshopManager.inst.Initialized)
                {
                    CoreHelper.Log($"Steam was not initialized. Please open Steam.");
                    ArcadeHelper.QuitToArcade();
                    return;
                }

                if (SteamWorkshopManager.inst.Levels.TryFind(x => x.id == levelInfo.workshopID, out level))
                {
                    level = NewCollectionLevel(level.path);
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
                    CoreHelper.StartCoroutine(SubscribeToSteamLevel(collection, levelInfo, onDownload, ArcadeHelper.QuitToArcade));
                }, ArcadeHelper.QuitToArcade);
            }
            else if (!string.IsNullOrEmpty(levelInfo.serverID))
            {
                CoreHelper.StartCoroutine(AlephNetwork.DownloadJSONFile($"{AlephNetwork.ARCADE_SERVER_URL}api/level/{levelInfo.serverID}", json =>
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
                                    level = NewCollectionLevel(level.path);
                                    levelInfo.Overwrite(level);
                                    if (collection)
                                        collection[levelInfo.index] = level;
                                    InterfaceManager.inst.CloseMenus();
                                    onDownload?.Invoke(level);
                                }, onError => ArcadeHelper.QuitToArcade());
                                return;
                            }

                            InterfaceManager.inst.CloseMenus();
                            CoreHelper.StartCoroutine(AlephNetwork.DownloadBytes($"{ArcadeMenu.CoverURL}{levelInfo.serverID}{FileFormat.JPG.Dot()}", bytes =>
                            {
                                var sprite = SpriteHelper.LoadSprite(bytes);
                                ArcadeMenu.OnlineLevelIcons[levelInfo.serverID] = sprite;

                                DownloadLevelMenu.Init(jsonObject);
                                DownloadLevelMenu.Current.onDownloadComplete = level =>
                                {
                                    level = NewCollectionLevel(level.path);
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
                                    level = NewCollectionLevel(level.path);
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
                }, onError => ArcadeHelper.QuitToArcade()));
            }
        }

        static IEnumerator SubscribeToSteamLevel(LevelCollection collection, LevelInfo levelInfo, Action<Level> onDownload = null, Action onFail = null)
        {
            var workshopID = SteamWorkshopManager.GetWorkshopID(levelInfo.workshopID);
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
            yield return SteamWorkshopManager.GetItem(workshopID, item =>
            {
                CoreHelper.Log($"Got item: {item}");
                CoreHelper.StartCoroutineAsync(AlephNetwork.DownloadBytes(item.PreviewImageUrl, bytes =>
                {
                    CoreHelper.ReturnToUnity(() =>
                    {
                        var sprite = SpriteHelper.LoadSprite(bytes);
                        ArcadeMenu.OnlineSteamLevelIcons[levelInfo.workshopID] = sprite;
                        InitSteamItem(collection, levelInfo, onDownload, item);
                    });
                }, onError =>
                {
                    CoreHelper.ReturnToUnity(() =>
                    {
                        var sprite = SteamWorkshop.inst.defaultSteamImageSprite;
                        ArcadeMenu.OnlineSteamLevelIcons[levelInfo.workshopID] = sprite;
                        InitSteamItem(collection, levelInfo, onDownload, item);
                    });
                }));

            }, onFail);
        }

        static void InitSteamItem(LevelCollection collection, LevelInfo levelInfo, Action<Level> onDownload, Item item)
        {
            CoreHelper.Log($"Init Steam Item: {item}");
            if (CoreHelper.InGame)
            {
                CoreHelper.StartCoroutine(SteamWorkshopManager.inst.ToggleSubscribedState(item, level =>
                {
                    level = NewCollectionLevel(level.path);
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
                level = NewCollectionLevel(level.path);
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
            if (RTFile.FileExists(achievementsPath))
            {
                var ach = JSON.Parse(RTFile.ReadFromFile(achievementsPath));
                for (int i = 0; i < ach["achievements"].Count; i++)
                    achievements.Add(Achievement.Parse(ach["achievements"][i]));
            }
        }

        /// <summary>
        /// Updates the icons of the collection.
        /// </summary>
        public void UpdateIcons()
        {
            icon = RTFile.FileExists(RTFile.CombinePaths(path, ICON_PNG)) ? SpriteHelper.LoadSprite(RTFile.CombinePaths(path, ICON_PNG)) : SpriteHelper.LoadSprite(RTFile.CombinePaths(path, ICON_JPG));
            banner = RTFile.FileExists(RTFile.CombinePaths(path, BANNER_PNG)) ? SpriteHelper.LoadSprite(RTFile.CombinePaths(path, BANNER_PNG)) : SpriteHelper.LoadSprite(RTFile.CombinePaths(path, BANNER_JPG));
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
            var jn = JSON.Parse("{}");

            jn["id"] = id;
            jn["server_id"] = serverID;
            jn["name"] = name;
            jn["creator"] = creator;
            jn["desc"] = name;

            if (tags != null)
                for (int i = 0; i < tags.Length; i++)
                    jn["tags"][i] = tags[i];

            for (int i = 0; i < levelInformation.Count; i++)
                jn["levels"][i] = levelInformation[i].ToJSON();

            if (saveIcons)
                SaveIcons(jpg);

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
        /// Adds a <see cref="Level"/> to the level collection and copies its folder to the level collection folder.
        /// </summary>
        /// <param name="level">Level to add.</param>
        public void AddLevelToFolder(Level level, bool add = false)
        {
            if (levels.Any(x => x.id == level.id)) // don't want to have duplicate levels
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

            var actualLevel = new Level(levelPath);
            var levelInfo = LevelInfo.FromLevel(actualLevel);
            levelInfo.index = levelInformation.Count;
            var id = levelInfo.id;
            actualLevel.id = id;
            if (actualLevel.metadata)
                actualLevel.metadata.arcadeID = id;

            if (add)
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

        #endregion
    }
}
