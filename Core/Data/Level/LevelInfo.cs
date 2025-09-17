using UnityEngine;

using SimpleJSON;

using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;

namespace BetterLegacy.Core.Data.Level
{
    /// <summary>
    /// Class used for obtaining info about a level even if the player doesn't have the level loaded in their arcade.
    /// </summary>
    public class LevelInfo : Exists
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

        #region Reference

        /// <summary>
        /// The level reference.
        /// </summary>
        public Level level;

        /// <summary>
        /// The level collection reference.
        /// </summary>
        public LevelCollection collection;

        /// <summary>
        /// Index of the level in the <see cref="levels"/>.
        /// </summary>
        public int index = 0;
        /// <summary>
        /// Unique ID of the level. Changes <see cref="arcadeID"/> to this when loading the level from the collection, so it does not conflict with the same level outside the collection.
        /// </summary>
        public string id = string.Empty;

        /// <summary>
        /// Path to the level in the level collection folder, if it's located there.
        /// </summary>
        public string path = string.Empty;
        /// <summary>
        /// Path to the level in the editor. Used for editing the level collection.
        /// </summary>
        public string editorPath = string.Empty;

        /// <summary>
        /// Artist of the song the level uses.
        /// </summary>
        public string songArtist = string.Empty;
        /// <summary>
        /// Title of the song the level uses.
        /// </summary>
        public string songTitle = string.Empty;
        /// <summary>
        /// Human-readable name of the level.
        /// </summary>
        public string name = string.Empty;
        /// <summary>
        /// Creator of the level.
        /// </summary>
        public string creator = string.Empty;
        /// <summary>
        /// Difficulty of the level.
        /// </summary>
        public int difficulty = 0;
        /// <summary>
        /// Difficulty of the level.
        /// </summary>
        public DifficultyType DifficultyType { get => difficulty; set => difficulty = value; }

        /// <summary>
        /// Icon of the level to display if the level does not exist.
        /// </summary>
        public Sprite icon;
        /// <summary>
        /// Icon of the level to display if the level is locked.
        /// </summary>
        public Sprite lockedIcon;

        /// <summary>
        /// Arcade ID reference.
        /// </summary>
        public string arcadeID = string.Empty;
        /// <summary>
        /// Server ID reference. Used for downloading the level off the Arcade server. if the player does not have it.
        /// </summary>
        public string serverID = string.Empty;
        /// <summary>
        /// Steam Workshop ID reference. Used for subscribing to and downloading the level off the Steam Workshop if the player does not have it.
        /// </summary>
        public string workshopID = string.Empty;

        #endregion

        #region Overwrite

        /// <summary>
        /// If the level requires unlocking / completion in order to access the level in the level collection list.
        /// </summary>
        public bool requireUnlock;

        /// <summary>
        /// If true, overwrites <see cref="requireUnlock"/>.
        /// </summary>
        public bool overwriteRequireUnlock;

        /// <summary>
        /// If the level unlocks after completion.
        /// </summary>
        public bool unlockAfterCompletion = true;

        /// <summary>
        /// If true, overwrites <see cref="unlockAfterCompletion"/>.
        /// </summary>
        public bool overwriteUnlockAfterCompletion;

        #endregion

        /// <summary>
        /// If the level should be hidden in the level collection list.
        /// </summary>
        public bool hidden;

        /// <summary>
        /// If the level should show after it has been unlocked.
        /// </summary>
        public bool showAfterUnlock;

        /// <summary>
        /// If the level should be skipped when progressing. This is for levels that you don't want played during the normal collection playthrough, and instead want to load it via a modifier.
        /// </summary>
        public bool skip;

        #endregion

        #region Methods

        /// <summary>
        /// Overwrites some values of the level.
        /// </summary>
        /// <param name="level">Level to overwrite the data of.</param>
        public void Overwrite(Level level)
        {
            if (level.metadata)
            {
                level.metadata = level.metadata.Copy(false);
                if (!string.IsNullOrEmpty(name))
                    level.metadata.beatmap.name = name;
                level.metadata.arcadeID = id;
                if (overwriteRequireUnlock)
                    level.metadata.requireUnlock = requireUnlock;
                if (overwriteUnlockAfterCompletion)
                    level.metadata.unlockAfterCompletion = unlockAfterCompletion;
            }

            // overwrites the original level ID so it doesn't conflict with the same level outside the collection.
            // So when the player plays the level inside the collection, it isn't already ranked.
            level.id = id;

            LevelManager.AssignSaveData(level);

            level.collectionInfo = this;
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
            id = jn.GetValueOrDefault("id", PAObjectBase.GetNumberID()),

            path = jn.GetValueOrDefault("path", string.Empty),
            editorPath = jn.GetValueOrDefault("editor_path", string.Empty),

            name = jn["name"],
            creator = jn.GetValueOrDefault("creator", string.Empty),
            songArtist = jn.GetValueOrDefault("song_artist", string.Empty),
            songTitle = jn.GetValueOrDefault("song_title", string.Empty),
            difficulty = jn.GetValueOrDefault("difficulty", string.Empty),

            arcadeID = jn.GetValueOrDefault("arcade_id", string.Empty),
            serverID = jn.GetValueOrDefault("server_id", string.Empty),
            workshopID = jn.GetValueOrDefault("workshop_id", string.Empty),

            hidden = jn["hidden"].AsBool,
            showAfterUnlock = jn["show_after_unlock"].AsBool,
            skip = jn["skip"].AsBool,

            overwriteRequireUnlock = jn["require_unlock"] != null,
            requireUnlock = jn["require_unlock"].AsBool,
            overwriteUnlockAfterCompletion = jn["unlock_complete"] != null,
            unlockAfterCompletion = jn["unlock_complete"].AsBool,

            icon = jn["icon"] != null ? SpriteHelper.StringToSprite(jn["icon"]) : null,
            lockedIcon = jn["locked_icon"] != null ? SpriteHelper.StringToSprite(jn["locked_icon"]) : null,
        };

        /// <summary>
        /// Writes the <see cref="LevelInfo"/> to a JSON.
        /// </summary>
        /// <returns>Returns a JSON representing the <see cref="LevelInfo"/>.</returns>
        public JSONNode ToJSON()
        {
            var jn = Parser.NewJSONObject();

            jn["id"] = id;

            if (!string.IsNullOrEmpty(path))
                jn["path"] = path;
            if (!string.IsNullOrEmpty(editorPath))
                jn["editor_path"] = editorPath;

            if (!string.IsNullOrEmpty(name))
                jn["name"] = name;
            if (!string.IsNullOrEmpty(creator))
                jn["creator"] = creator;
            if (!string.IsNullOrEmpty(songArtist))
                jn["song_artist"] = songArtist;
            if (!string.IsNullOrEmpty(songTitle))
                jn["song_title"] = songTitle;
            jn["difficulty"] = difficulty;

            if (!string.IsNullOrEmpty(arcadeID))
                jn["arcade_id"] = arcadeID;
            if (!string.IsNullOrEmpty(serverID))
                jn["server_id"] = serverID;
            if (!string.IsNullOrEmpty(workshopID))
                jn["workshop_id"] = workshopID;

            if (hidden)
                jn["hidden"] = hidden;
            if (showAfterUnlock)
                jn["show_after_unlock"] = showAfterUnlock;
            if (skip)
                jn["skip"] = skip;

            if (overwriteRequireUnlock)
                jn["require_unlock"] = requireUnlock;
            if (overwriteUnlockAfterCompletion)
                jn["unlock_complete"] = unlockAfterCompletion;

            if (icon)
                jn["icon"] = SpriteHelper.SpriteToString(icon);
            if (lockedIcon)
                jn["locked_icon"] = SpriteHelper.SpriteToString(lockedIcon);

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
            id = PAObjectBase.GetNumberID(),

            name = level.metadata?.beatmap?.name ?? string.Empty,
            creator = level.metadata?.creator?.name ?? string.Empty,
            songTitle = level.metadata?.song?.title ?? string.Empty,
            songArtist = level.metadata?.artist?.name ?? string.Empty,
            difficulty = level.metadata?.song?.difficulty ?? 0,
            icon = level.icon,
            lockedIcon = level.lockedIcon,

            arcadeID = level.metadata?.arcadeID ?? string.Empty,
            serverID = level.metadata?.serverID ?? string.Empty,
            workshopID = level.metadata?.beatmap?.workshopID.ToString() ?? string.Empty,

            hidden = false,
            requireUnlock = level.metadata?.requireUnlock ?? false,
            unlockAfterCompletion = level.metadata?.unlockAfterCompletion ?? true,
        };

        #endregion
    }
}
