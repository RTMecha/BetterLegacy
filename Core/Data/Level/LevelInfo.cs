﻿using BetterLegacy.Core.Managers;
using LSFunctions;
using SimpleJSON;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BetterLegacy.Core.Data.Level
{
    /// <summary>
    /// Class used for obtaining info about a level even if the player doesn't have the level loaded in their arcade.
    /// </summary>
    public class LevelInfo
    {
        public LevelInfo() { }

        public LevelInfo(string id, string arcadeID, string serverID, string workshopID, string songTitle, string name)
        {
            this.id = id;
            this.arcadeID = arcadeID;
            this.serverID = serverID;
            this.workshopID = workshopID;
            this.songTitle = songTitle;
            this.name = name;
        }

        #region Fields

        /// <summary>
        /// Index of the level in the <see cref="levels"/>.
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

        /// <summary>
        /// If true, overwrites <see cref="requireUnlock"/>.
        /// </summary>
        public bool overwriteRequireUnlock;

        /// <summary>
        /// The level reference.
        /// </summary>
        public Level level;

        #endregion

        #region Methods

        public void Overwrite(Level level)
        {
            if (level.metadata)
            {
                level.metadata = MetaData.DeepCopy(level.metadata);
                level.metadata.arcadeID = id;
                if (overwriteRequireUnlock)
                    level.metadata.requireUnlock = requireUnlock;
            }

            // overwrites the original level ID so it doesn't conflict with the same level outside the collection.
            // So when the player plays the level inside the collection, it isn't already ranked.
            level.id = id;

            if (LevelManager.Saves.TryFind(x => x.ID == level.id, out PlayerData playerData))
                level.playerData = playerData;
        }

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
            overwriteRequireUnlock = jn["require_unlock"] != null,
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
            if (overwriteRequireUnlock && requireUnlock)
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
            level = level,
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
