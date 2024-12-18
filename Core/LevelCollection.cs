﻿using BetterLegacy.Core.Data;
using BetterLegacy.Core.Managers;
using BetterLegacy.Core.Managers.Networking;
using LSFunctions;
using SimpleJSON;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace BetterLegacy.Core
{
    /// <summary>
    /// Stores multiple levels in a specific order. Good for stories.
    /// </summary>
    public class LevelCollection : Exists
    {
        public LevelCollection() { id = LSText.randomNumString(16); }

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
                collection.levelInformation.Add(LevelInfo.Parse(jnLevel, i));

                if (!loadLevels)
                    continue;

                var jnPath = jnLevel["path"];

                // parse via path
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

                    collection.AddJSON(jnLevel, NewCollectionLevel(levelFolder));
                }

                // load via arcade ID
                else if (jnLevel["arcade_id"] != null && LevelManager.Levels.TryFind(x => x.id == jnLevel["arcade_id"], out Level arcadeLevel))
                    collection.AddJSON(jnLevel, NewCollectionLevel(arcadeLevel.path));

                // load via workshop ID
                else if (jnLevel["workshop_id"] != null && SteamWorkshopManager.inst.Levels.TryFind(x => x.id == jnLevel["workshop_id"], out Level steamLevel))
                    collection.AddJSON(jnLevel, NewCollectionLevel(steamLevel.path));

                // no level was found, so add null
                else
                    collection.levels.Add(null);
            }

            collection.UpdateIcons();

            return collection;
        }

        void AddJSON(JSONNode jn, Level level)
        {
            if (level.metadata)
            {
                level.metadata = MetaData.DeepCopy(level.metadata);
                level.metadata.arcadeID = jn["id"];
                if (jn["require_unlock"] != null)
                    level.metadata.requireUnlock = jn["require_unlock"];
            }

            // overwrites the original level ID so it doesn't conflict with the same level outside the collection.
            // So when the player plays the level inside the collection, it isn't already ranked.
            level.id = jn["id"];

            if (LevelManager.Saves.TryFind(x => x.ID == level.id, out LevelManager.PlayerData playerData))
                level.playerData = playerData;

            levels.Add(level);
        }

        static Level NewCollectionLevel(string path) => new Level(path) { fromCollection = true };

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

        /// <summary>
        /// Class used for obtaining info about a level even if the player doesn't have the level loaded in their arcade.
        /// </summary>
        public class LevelInfo
        {
            #region Fields

            /// <summary>
            /// Index of the level in the <see cref="LevelCollection.levels"/>.
            /// </summary>
            public int index;
            /// <summary>
            /// Unique ID of the level. Changes <see cref="arcadeID"/> to this when loading the level from the collection, so it does not conflict with the same level outside the collection.
            /// </summary>
            public string id;

            /// <summary>
            /// Path to the level in the level collection folder, if it's located there.
            /// </summary>
            public string path;
            /// <summary>
            /// Path to the level in the editor. Used for editing the level collection.
            /// </summary>
            public string editorPath;

            /// <summary>
            /// Title of the song the level uses.
            /// </summary>
            public string songTitle;
            /// <summary>
            /// Human-readable name of the level.
            /// </summary>
            public string name;
            /// <summary>
            /// Creator of the level.
            /// </summary>
            public string creator;

            /// <summary>
            /// Arcade ID reference.
            /// </summary>
            public string arcadeID;
            /// <summary>
            /// Server ID reference. Used for downloading the level off the Arcade server. if the player does not have it.
            /// </summary>
            public string serverID;
            /// <summary>
            /// Steam Workshop ID reference. Used for subscribing to and downloading the level off the Steam Workshop if the player does not have it.
            /// </summary>
            public string workshopID;

            /// <summary>
            /// If the level should be hidden in the level collection list.
            /// </summary>
            public bool hidden;
            /// <summary>
            /// If the level requires unlocking / completion in order to access the level in the level collection list.
            /// </summary>
            public bool requireUnlock;

            #endregion

            #region Methods

            /// <summary>
            /// Parses a levels' information from the level collection file.
            /// </summary>
            /// <param name="jn">JSON to parse.</param>
            /// <param name="index">Index of the level.</param>
            /// <returns>Returns a parsed <see cref="LevelInfo"/>.</returns>
            public static LevelInfo Parse(JSONNode jn, int index) => new LevelInfo
            {
                index = index,
                id = jn["id"],

                path = jn["path"],
                editorPath = jn["editor_path"],

                name = jn["name"],
                creator = jn["creator"],
                songTitle = jn["song_title"],

                arcadeID = jn["arcade_id"],
                serverID = jn["server_id"],
                workshopID = jn["workshop_id"],

                hidden = jn["hidden"].AsBool,
                requireUnlock = jn["require_unlock"].AsBool,
            };

            /// <summary>
            /// Writes the <see cref="LevelInfo"/> to a JSON.
            /// </summary>
            /// <returns>Returns a JSON representing the <see cref="LevelInfo"/>.</returns>
            public JSONNode ToJSON()
            {
                var jn = JSON.Parse("{}");

                jn["id"] = id;

                if (!string.IsNullOrEmpty(path))
                    jn["path"] = path;
                if (!string.IsNullOrEmpty(editorPath))
                    jn["editor_path"] = editorPath;

                if (!string.IsNullOrEmpty(name))
                    jn["name"] = name;
                if (!string.IsNullOrEmpty(creator))
                    jn["creator"] = creator;
                if (!string.IsNullOrEmpty(songTitle))
                    jn["song_title"] = songTitle;

                if (!string.IsNullOrEmpty(arcadeID))
                    jn["arcade_id"] = arcadeID;
                if (!string.IsNullOrEmpty(serverID))
                    jn["server_id"] = serverID;
                if (!string.IsNullOrEmpty(workshopID))
                    jn["workshop_id"] = workshopID;

                if (hidden)
                    jn["hidden"] = hidden.ToString();
                if (requireUnlock)
                    jn["require_unlock"] = requireUnlock.ToString();

                return jn;
            }

            /// <summary>
            /// Creates a <see cref="LevelInfo"/> from a <see cref="Level"/>.
            /// </summary>
            /// <param name="level"><see cref="Level"/> to reference.</param>
            /// <returns>Returns a <see cref="LevelInfo"/> based on the <see cref="Level"/>.</returns>
            public static LevelInfo FromLevel(Level level) => new LevelInfo
            {
                id = LSText.randomNumString(16),

                name = level.metadata?.beatmap?.name,
                creator = level.metadata?.creator?.steam_name,
                songTitle = level.metadata?.song?.title,

                arcadeID = level.metadata?.arcadeID,
                serverID = level.metadata?.serverID,
                workshopID = level.metadata?.beatmap?.beatmap_id,

                hidden = false,
                requireUnlock = level.metadata.requireUnlock,
            };

            #endregion
        }
    }
}
