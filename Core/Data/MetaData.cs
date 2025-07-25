﻿using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

using SimpleJSON;

using BetterLegacy.Core.Helpers;

namespace BetterLegacy.Core.Data
{
    /// <summary>
    /// User readable information of the level.
    /// </summary>
    public class MetaData : PAObject<MetaData>
    {
        public MetaData()
        {
            artist = new ArtistMetaData();
            creator = new CreatorMetaData();
            song = new SongMetaData();
            beatmap = new BeatmapMetaData();
        }

        #region Properties

        /// <summary>
        /// The current MetaData that is being used by the game.
        /// </summary>
        public static MetaData Current { get; set; }

        /// <summary>
        /// Gets the ID of the level.
        /// </summary>
        public string ID => arcadeID;

        /// <summary>
        /// Gets the game version of the metadata.
        /// </summary>
        public Version Version => new Version(beatmap.gameVersion);

        /// <summary>
        /// Gets the mod version of the metadata.
        /// </summary>
        public Version ModVersion => new Version(beatmap.modVersion);

        /// <summary>
        /// Formats the song URL into a correct link format, in cases where the artist name is included in the song link somewhere.
        /// </summary>
        public string SongURL => string.IsNullOrEmpty(song.link) || string.IsNullOrEmpty(artist.link) ? null : AlephNetwork.GetURL(URLSource.Song, song.linkType, song.linkType == 2 ? artist.link + "," + song.link : song.link);

        #endregion

        #region Methods

        public override void CopyData(MetaData orig, bool newID = true)
        {
            artist.CopyData(orig.artist, newID);
            creator.CopyData(orig.creator, newID);
            song.CopyData(orig.song, newID);
            beatmap.CopyData(orig.beatmap, newID);

            serverID = orig.serverID;
            uploaderName = orig.uploaderName;
            uploaderID = orig.uploaderID;
            index = orig.index;
            collectionID = orig.collectionID;
            isHubLevel = orig.isHubLevel;
            requireUnlock = orig.requireUnlock;
            unlockAfterCompletion = orig.unlockAfterCompletion;
            arcadeID = orig.arcadeID;
            nextID = orig.nextID;
            prevID = orig.prevID;
            visibility = orig.visibility;
            changelog = orig.changelog;
            requireVersion = orig.requireVersion;
        }

        /// <summary>
        /// Parses a levels' metadata from a file.
        /// </summary>
        /// <param name="path">File to parse.</param>
        /// <param name="fileType">The type of Project Arrhythmia the file is from.</param>
        /// <param name="setDefaultValues">If defaults should be set when a value is null.</param>
        /// <returns>Returns a parsed <see cref="MetaData"/>.</returns>
        public static MetaData ReadFromFile(string path, ArrhythmiaType fileType) => fileType switch
        {
            ArrhythmiaType.LS => Parse(JSON.Parse(RTFile.ReadFromFile(path))),
            ArrhythmiaType.VG => ParseVG(JSON.Parse(RTFile.ReadFromFile(path))),
            _ => null,
        };

        public override void ReadJSONVG(JSONNode jn, Version version = default)
        {
            artist.ReadJSONVG(jn, version);
            creator.ReadJSONVG(jn, version);
            song.ReadJSONVG(jn, version);
            beatmap.ReadJSONVG(jn, version);
        }

        public override void ReadJSON(JSONNode jn)
        {
            try
            {
                artist.ReadJSON(jn);
                creator.ReadJSON(jn);
                song.ReadJSON(jn);
                beatmap.ReadJSON(jn);

                if (!string.IsNullOrEmpty(jn["server_id"]))
                    serverID = jn["server_id"];
                if (!string.IsNullOrEmpty(jn["arcade_id"]))
                    arcadeID = jn["arcade_id"];

                if (!string.IsNullOrEmpty(jn["storyline"]["prev_level"]))
                    prevID = jn["storyline"]["prev_level"];

                if (!string.IsNullOrEmpty(jn["storyline"]["next_level"]))
                    nextID = jn["storyline"]["next_level"];

                if (!string.IsNullOrEmpty(jn["uploader_name"]))
                    uploaderName = jn["uploader_name"];
                else
                    uploaderName = creator.name;

                if (!string.IsNullOrEmpty(jn["uploader_id"]))
                    uploaderID = jn["uploader_id"];

                if (!string.IsNullOrEmpty(jn["is_hub_level"]))
                    isHubLevel = jn["is_hub_level"].AsBool;

                if (!string.IsNullOrEmpty(jn["require_unlock"]))
                    requireUnlock = jn["require_unlock"].AsBool;

                if (!string.IsNullOrEmpty(jn["unlock_complete"]))
                    unlockAfterCompletion = jn["unlock_complete"].AsBool;

                if (!string.IsNullOrEmpty(jn["visibility"]))
                    visibility = (ServerVisibility)jn["visibility"].AsInt;

                if (!string.IsNullOrEmpty(jn["changelog"]))
                    changelog = jn["changelog"];

                if (!string.IsNullOrEmpty(jn["require_version"]))
                    requireVersion = jn["require_version"].AsBool;

                if (!string.IsNullOrEmpty(jn["version_comparison"]))
                    versionRange = (DataManager.VersionComparison)jn["version_comparison"].AsInt;

                var sayings = jn["sayings"];
                if (sayings != null)
                {
                    var ranks = Rank.Null.GetValues();
                    foreach (var rank in ranks)
                    {
                        if (sayings[rank.Name.ToLower()] != null)
                            customSayings[rank] = sayings[rank.Name.ToLower()].Children.Select(x => x.Value).ToArray();
                    }
                }
            }
            catch
            {
                var artist = new ArtistMetaData("Corrupted", 0, "");
                var creator = new CreatorMetaData(SteamWrapper.inst.user.displayName, SteamWrapper.inst.user.id, "", 0);
                var song = new SongMetaData("Corrupt Metadata", 0, "", 140f, 100f, -1f, -1f, new string[] { "Corrupted" }, 2, "album/full-devoid");
                var beatmap = new BeatmapMetaData("Level Name", "", "", "", ProjectArrhythmia.GameVersion.ToString(), 0, -1, LegacyPlugin.ModVersion.ToString());

                this.artist = artist;
                this.creator = creator;
                this.song = song;
                this.beatmap = beatmap;

                Debug.LogError($"{DataManager.inst.className}Something went wrong with parsing metadata!");
            }
        }

        /// <summary>
        /// Writes the <see cref="MetaData"/> to a VG format JSON.
        /// </summary>
        /// <returns>Returns a JSON object representing the <see cref="MetaData"/>.</returns>
        public override JSONNode ToJSONVG()
        {
            var jn = Parser.NewJSONObject();

            jn["artist"] = artist.ToJSONVG();
            jn["creator"] = creator.ToJSONVG();
            jn["song"] = song.ToJSONVG();
            jn["beatmap"] = beatmap.ToJSONVG();

            return jn;
        }

        /// <summary>
        /// Writes the <see cref="MetaData"/> to a LS format JSON.
        /// </summary>
        /// <returns>Returns a JSON object representing the <see cref="MetaData"/>.</returns>
        public override JSONNode ToJSON()
        {
            var jn = Parser.NewJSONObject();

            jn["artist"] = artist.ToJSON();
            jn["creator"] = creator.ToJSON();
            jn["song"] = song.ToJSON();
            jn["beatmap"] = beatmap.ToJSON();

            if (!string.IsNullOrEmpty(serverID))
                jn["server_id"] = serverID;

            if (!string.IsNullOrEmpty(arcadeID))
                jn["arcade_id"] = arcadeID;

            if (!string.IsNullOrEmpty(collectionID))
                jn["collection_id"] = collectionID;

            if (!string.IsNullOrEmpty(prevID))
                jn["storyline"]["prev_level"] = prevID;

            if (!string.IsNullOrEmpty(nextID))
                jn["storyline"]["next_level"] = nextID;

            if (!string.IsNullOrEmpty(uploaderName))
                jn["uploader_name"] = uploaderName;
            
            if (!string.IsNullOrEmpty(uploaderID))
                jn["uploader_id"] = uploaderID;

            if (isHubLevel)
                jn["is_hub_level"] = isHubLevel.ToString();

            if (requireUnlock)
                jn["require_unlock"] = requireUnlock.ToString();

            if (!unlockAfterCompletion)
                jn["unlock_complete"] = unlockAfterCompletion.ToString();

            if (visibility != ServerVisibility.Public)
                jn["visibility"] = ((int)visibility).ToString();

            if (!string.IsNullOrEmpty(changelog))
                jn["changelog"] = changelog;

            if (requireVersion)
                jn["require_version"] = requireVersion.ToString();
            
            if (versionRange != DataManager.VersionComparison.EqualTo)
                jn["version_comparison"] = ((int)versionRange).ToString();

            if (customSayings != null)
            {
                foreach (var keyValuePair in customSayings)
                {
                    for (int i = 0; i < keyValuePair.Value.Length; i++)
                        jn["sayings"][keyValuePair.Key.Name.ToLower()][i] = keyValuePair.Value[i];
                }
            }

            return jn;
        }

        /// <summary>
        /// Saves the <see cref="MetaData"/> to a LS format file.
        /// </summary>
        /// <param name="path">The file to save to.</param>
        public void WriteToFile(string path) => RTFile.WriteToFile(path, ToJSON().ToString(3));

        /// <summary>
        /// Saves the <see cref="MetaData"/> to a VG format file.
        /// </summary>
        /// <param name="path">The file to save to.</param>
        public void WriteToFileVG(string path) => RTFile.WriteToFile(path, ToJSONVG().ToString(3));

        /// <summary>
        /// Checks if the levels' version is incompatible with the games' current version.
        /// </summary>
        /// <returns>Returns true if the level is playable in the games' current version, otherwise returns false.</returns>
        public bool IsIncompatible() => requireVersion && versionRange switch
        {
            DataManager.VersionComparison.EqualTo => ModVersion != LegacyPlugin.ModVersion,
            DataManager.VersionComparison.GreaterThan => ModVersion > LegacyPlugin.ModVersion,
            DataManager.VersionComparison.LessThan => ModVersion < LegacyPlugin.ModVersion,
            _ => false,
        };

        /// <summary>
        /// Gets a message for the levels' incompatibility.
        /// </summary>
        /// <returns>Returns a string representing the incompatibility of the level.</returns>
        public string GetIncompatibleMessage() => versionRange switch
        {
            DataManager.VersionComparison.EqualTo => $"Level is only playable in BetterLegacy version {ModVersion}",
            DataManager.VersionComparison.GreaterThan => $"Level is only playable after and in BetterLegacy version {ModVersion}",
            DataManager.VersionComparison.LessThan => $"Level is only playable before and in BetterLegacy version {ModVersion}",
            _ => string.Empty,
        };

        /// <summary>
        /// Verifies the ID is usable in BetterLegacy.
        /// </summary>
        public void VerifyID(string path)
        {
            if (string.IsNullOrEmpty(arcadeID) || arcadeID.Contains("-") /* < don't want negative IDs */ || arcadeID == "0")
            {
                arcadeID = LSFunctions.LSText.randomNumString(16);
                var jn = ToJSON();
                RTFile.WriteToFile(RTFile.CombinePaths(path, Level.Level.METADATA_LSB), jn.ToString(3));
            }
        }

        #endregion

        #region Fields

        public ArtistMetaData artist;
        public CreatorMetaData creator;
        public SongMetaData song;
        public BeatmapMetaData beatmap;

        public string collectionID;
        public int index;
        public string uploaderName;
        public string uploaderID;
        public string serverID;
        public string arcadeID;
        public string prevID;
        public string nextID;
        public bool isHubLevel;
        public bool requireUnlock;
        public bool unlockAfterCompletion = true;
        public ServerVisibility visibility;
        public string changelog;
        /// <summary>
        /// Marks the level as requiring a specific version. This means levels made in a specific version with specific features can only be playable on that version.
        /// </summary>
        public bool requireVersion;
        public DataManager.VersionComparison versionRange = DataManager.VersionComparison.EqualTo;

        public Dictionary<Rank, string[]> customSayings = new Dictionary<Rank, string[]>();

        #endregion

        #region Operators

        public override int GetHashCode() => CoreHelper.CombineHashCodes(arcadeID);

        public override bool Equals(object obj) => obj is MetaData && ID == (obj as MetaData).ID;

        public override string ToString() => $"{ID}: {artist.name} - {song.title}";

        #endregion
    }

    /// <summary>
    /// Artist section of <see cref="MetaData"/>.
    /// </summary>
    public class ArtistMetaData : PAObject<ArtistMetaData>
    {
        public ArtistMetaData() : this(string.Empty, 2, string.Empty) { }

        public ArtistMetaData(string name, int linkType, string link)
        {
            this.name = name;
            this.linkType = linkType;
            this.link = link;
        }

        #region Values

        /// <summary>
        /// URL to the artists' site.
        /// </summary>
        public string URL => AlephNetwork.GetURL(URLSource.Artist, linkType, link);

        public string name = "Artist";
        public int linkType = 2;
        public string link = string.Empty;

        #endregion

        #region Methods

        public override void CopyData(ArtistMetaData orig, bool newID = true)
        {
            name = orig.name;
            linkType = orig.linkType;
            link = orig.link;
        }

        public override void ReadJSONVG(JSONNode jn, Version version = default)
        {
            if (!string.IsNullOrEmpty(jn["artist"]["name"]))
                name = jn["artist"]["name"];
            if (!string.IsNullOrEmpty(jn["artist"]["link_type"]))
                linkType = jn["artist"]["link_type"].AsInt;
            if (!string.IsNullOrEmpty(jn["artist"]["link"]))
                link = jn["artist"]["link"];
        }

        public override void ReadJSON(JSONNode jn)
        {
            if (!string.IsNullOrEmpty(jn["artist"]["name"]))
                name = jn["artist"]["name"];
            if (!string.IsNullOrEmpty(jn["artist"]["linkType"]))
                linkType = jn["artist"]["linkType"].AsInt;
            if (!string.IsNullOrEmpty(jn["artist"]["link"]))
                link = jn["artist"]["link"];
        }

        public override JSONNode ToJSONVG()
        {
            var jn = Parser.NewJSONObject();

            jn["name"] = name ?? string.Empty;
            jn["link_type"] = linkType;
            jn["link"] = link ?? string.Empty;

            return jn;
        }

        public override JSONNode ToJSON()
        {
            var jn = Parser.NewJSONObject();

            jn["name"] = name;
            jn["linkType"] = !string.IsNullOrEmpty(link) ? linkType.ToString() : 2;
            jn["link"] = link ?? string.Empty;

            return jn;
        }

        public override string ToString() => name;

        #endregion
    }

    /// <summary>
    /// Creator section of <see cref="MetaData"/>.
    /// </summary>
    public class CreatorMetaData : PAObject<CreatorMetaData>
    {
        public CreatorMetaData() => name = "Unknown User";

        public CreatorMetaData(string name, int steamID, string link, int linkType)
        {
            this.name = name;
            this.steamID = steamID;
            this.linkType = linkType;
            this.link = link;
        }

        #region Values

        /// <summary>
        /// URL to the creators' site.
        /// </summary>
        public string URL => AlephNetwork.GetURL(URLSource.Creator, linkType, link);

        public string name = "Unknown User";
        public long steamID = -1;
        public int linkType;
        public string link;

        #endregion

        #region Methods

        public override void CopyData(CreatorMetaData orig, bool newID = true)
        {
            steamID = orig.steamID;
            name = orig.name;
            linkType = orig.linkType;
            link = orig.link;
        }

        public override void ReadJSONVG(JSONNode jn, Version version = default)
        {
            if (!string.IsNullOrEmpty(jn["creator"]["steam_name"]))
                name = jn["creator"]["steam_name"];
            if (jn["creator"]["steam_id"] != null)
                steamID = jn["creator"]["steam_id"].AsLong;
        }

        public override void ReadJSON(JSONNode jn)
        {
            if (!string.IsNullOrEmpty(jn["creator"]["steam_name"]))
                name = jn["creator"]["steam_name"];
            if (!string.IsNullOrEmpty(jn["creator"]["steam_id"]))
                steamID = jn["creator"]["steam_id"].AsLong;
            if (jn["creator"]["linkType"] != null)
                linkType = jn["creator"]["linkType"].AsInt;
            if (jn["creator"]["link_type"] != null)
                linkType = jn["creator"]["link_type"].AsInt;
            if (!string.IsNullOrEmpty(jn["creator"]["link"]))
                link = jn["creator"]["link"];
        }

        public override JSONNode ToJSONVG()
        {
            var jn = Parser.NewJSONObject();

            jn["steam_name"] = name ?? string.Empty;
            jn["steam_id"] = steamID;

            return jn;
        }

        public override JSONNode ToJSON()
        {
            var jn = Parser.NewJSONObject();

            jn["steam_name"] = name ?? string.Empty;
            jn["steam_id"] = steamID.ToString();

            if (!string.IsNullOrEmpty(link))
            {
                jn["link"] = link;
                jn["link_type"] = linkType.ToString();
            }

            return jn;
        }

        public override string ToString() => name;

        #endregion
    }

    /// <summary>
    /// Song section of <see cref="MetaData"/>.
    /// </summary>
    public class SongMetaData : PAObject<SongMetaData>
    {
        public SongMetaData() { }

        public SongMetaData(string title, int difficulty, string description, float bpm, float time, float previewStart, float previewLength, string[] tags, int linkType, string link)
        {
            this.title = title;
            this.difficulty = difficulty;
            this.description = description;
            this.bpm = bpm;
            this.time = time;
            this.previewLength = previewLength;
            this.previewStart = previewStart;

            this.tags = tags;
            this.linkType = linkType;
            this.link = link;
        }

        #region Values

        public int linkType = 2;
        public string link;
        public string[] tags;

        public DifficultyType DifficultyType { get => difficulty; set => difficulty = value; }

        public string title = "Song Title Placeholder";

        public int difficulty = 2;

        public string description = "This is the default description!";

        public float bpm = 140f;

        public float time = 60f;

        public float previewStart = -1f;

        public float previewLength = -1f;

        #endregion

        #region Methods

        public override void CopyData(SongMetaData orig, bool newID = true)
        {
            title = orig.title;
            difficulty = orig.difficulty;
            description = orig.description;
            bpm = orig.bpm;
            time = orig.time;
            previewLength = orig.previewLength;
            previewStart = orig.previewStart;

            tags = orig.tags?.Copy() ?? new string[] { };
            linkType = orig.linkType;
            link = orig.link;
        }

        public override void ReadJSONVG(JSONNode jn, Version version = default)
        {
            if (!string.IsNullOrEmpty(jn["song"]["title"]))
                title = jn["song"]["title"];
            if (!string.IsNullOrEmpty(jn["song"]["difficulty"]))
                difficulty = jn["song"]["difficulty"].AsInt;
            if (!string.IsNullOrEmpty(jn["song"]["description"]))
                description = jn["song"]["description"];
            if (!string.IsNullOrEmpty(jn["song"]["bpm"]))
                bpm = jn["song"]["bpm"].AsFloat;
            if (!string.IsNullOrEmpty(jn["song"]["time"]))
                time = jn["song"]["time"].AsFloat;
            if (!string.IsNullOrEmpty(jn["song"]["preview_start"]))
                previewStart = jn["song"]["preview_start"].AsFloat;
            if (!string.IsNullOrEmpty(jn["song"]["preview_length"]))
                previewLength = jn["song"]["preview_length"].AsFloat;
            tags = new string[] { };
        }

        public override void ReadJSON(JSONNode jn)
        {
            if (!string.IsNullOrEmpty(jn["song"]["title"]))
                title = jn["song"]["title"];
            if (!string.IsNullOrEmpty(jn["song"]["difficulty"]))
                difficulty = jn["song"]["difficulty"].AsInt;
            if (!string.IsNullOrEmpty(jn["song"]["description"]))
                description = jn["song"]["description"];
            if (!string.IsNullOrEmpty(jn["song"]["link"]))
                link = jn["song"]["link"];
            if (jn["song"]["linkType"] != null)
                linkType = jn["song"]["linkType"].AsInt;
            if (jn["song"]["link_type"] != null)
                linkType = jn["song"]["link_type"].AsInt;
            if (!string.IsNullOrEmpty(jn["song"]["bpm"]))
                bpm = jn["song"]["bpm"].AsFloat;
            if (!string.IsNullOrEmpty(jn["song"]["t"]))
                time = jn["song"]["t"].AsFloat;
            if (!string.IsNullOrEmpty(jn["song"]["preview_start"]))
                previewStart = jn["song"]["preview_start"].AsFloat;
            if (!string.IsNullOrEmpty(jn["song"]["preview_length"]))
                previewLength = jn["song"]["preview_length"].AsFloat;

            if (jn["song"]["tags"] != null)
            {
                tags = new string[jn["song"]["tags"].Count];
                for (int i = 0; i < jn["song"]["tags"].Count; i++)
                {
                    tags[i] = jn["song"]["tags"][i].Value.Replace(" ", "_");
                }
            }
            else
                tags = new string[] { };
        }

        public override JSONNode ToJSONVG()
        {
            var jn = Parser.NewJSONObject();

            jn["title"] = title ?? string.Empty;
            jn["difficulty"] = difficulty;
            jn["description"] = description ?? string.Empty;
            jn["bpm"] = bpm;
            jn["time"] = time;
            jn["preview_start"] = previewStart;
            jn["preview_length"] = previewLength;

            return jn;
        }

        public override JSONNode ToJSON()
        {
            var jn = Parser.NewJSONObject();

            jn["title"] = title ?? string.Empty;
            jn["difficulty"] = difficulty.ToString();
            jn["description"] = description ?? string.Empty;

            if (!string.IsNullOrEmpty(link))
            {
                jn["link"] = link;
                jn["link_type"] = linkType.ToString();
            }

            jn["bpm"] = bpm.ToString();
            jn["t"] = time.ToString();
            jn["preview_start"] = previewStart.ToString();
            jn["preview_length"] = previewLength.ToString();

            if (tags != null)
                for (int i = 0; i < tags.Length; i++)
                    jn["tags"][i] = tags[i] ?? string.Empty;

            return jn;
        }

        public override string ToString() => title;

        #endregion
    }

    /// <summary>
    /// Beatmap section of <see cref="MetaData"/>.
    /// </summary>
    public class BeatmapMetaData : PAObject<BeatmapMetaData>
    {
        public BeatmapMetaData()
        {
            name = "Level Name";
            gameVersion = ProjectArrhythmia.GameVersion.ToString();
            dateCreated = DateTime.Now.ToString("yyyy-MM-dd_HH.mm.ss");
            modVersion = LegacyPlugin.ModVersion.ToString();
        }

        public BeatmapMetaData(string name, string dateEdited, string dateCreated, string datePublished, string gameVersion, int versionNumber, long workshopID, string modVersion)
        {
            this.name = name;
            this.dateEdited = dateEdited;
            this.dateCreated = dateCreated;
            this.datePublished = datePublished;

            this.workshopID = workshopID;
            this.versionNumber = versionNumber;

            this.gameVersion = gameVersion;
            this.modVersion = modVersion;
        }

        #region Values

        public string name;

        public string dateEdited = DateTime.Now.ToString("yyyy-MM-dd_HH.mm.ss");
        public string dateCreated = string.Empty;
        public string datePublished = string.Empty;

        public long workshopID = -1;
        public int versionNumber;

        public string gameVersion = ProjectArrhythmia.GAME_VERSION;
        public string modVersion;

        public PreferredPlayerCount preferredPlayerCount;

        public enum PreferredPlayerCount
        {
            Any,
            One,
            Two,
            Three,
            Four,
            MoreThanFour
        }

        #endregion

        #region Methods

        public override void CopyData(BeatmapMetaData orig, bool newID = true)
        {
            name = orig.name;
            dateEdited = orig.dateEdited;
            dateCreated = orig.dateCreated;
            datePublished = orig.datePublished;
            versionNumber = orig.versionNumber;
            workshopID = orig.workshopID;

            gameVersion = orig.gameVersion;
            modVersion = orig.modVersion;

            preferredPlayerCount = orig.preferredPlayerCount;
        }

        public override void ReadJSONVG(JSONNode jn, Version version = default)
        {
            if (!string.IsNullOrEmpty(jn["song"]["title"]))
                name = jn["song"]["title"];
            if (!string.IsNullOrEmpty(jn["beatmap"]["game_version"]))
                gameVersion = jn["beatmap"]["game_version"];
            if (!string.IsNullOrEmpty(jn["beatmap"]["mod_version"]))
                modVersion = jn["beatmap"]["mod_version"];
            if (!string.IsNullOrEmpty(jn["beatmap"]["date_edited"]))
                dateEdited = jn["beatmap"]["date_edited"];
            if (!string.IsNullOrEmpty(jn["beatmap"]["date_created"]))
                dateCreated = jn["beatmap"]["date_created"];
            if (jn["beatmap"]["version_number"] != null)
                versionNumber = jn["beatmap"]["version_number"].AsInt;
            if (jn["beatmap"]["workshop_id"] != null)
                workshopID = jn["beatmap"]["workshop_id"].AsLong;
        }

        public override void ReadJSON(JSONNode jn)
        {
            if (!string.IsNullOrEmpty(jn["beatmap"]["name"]))
                name = jn["beatmap"]["name"];
            else
                name = jn["song"]["title"];

            if (!string.IsNullOrEmpty(jn["beatmap"]["game_version"]))
                gameVersion = jn["beatmap"]["game_version"];
            if (!string.IsNullOrEmpty(jn["beatmap"]["mod_version"]))
                modVersion = jn["beatmap"]["mod_version"];
            if (!string.IsNullOrEmpty(jn["beatmap"]["date_edited"]))
                dateEdited = jn["beatmap"]["date_edited"];
            if (!string.IsNullOrEmpty(jn["beatmap"]["date_created"]))
                dateCreated = jn["beatmap"]["date_created"];
            if (!string.IsNullOrEmpty(jn["beatmap"]["date_published"]))
                datePublished = jn["beatmap"]["date_published"];
            if (jn["beatmap"]["version_number"] != null)
                versionNumber = jn["beatmap"]["version_number"].AsInt;
            if (jn["beatmap"]["workshop_id"] != null)
                workshopID = jn["beatmap"]["workshop_id"].AsLong;
            if (jn["beatmap"]["preferred_players"] != null)
                preferredPlayerCount = (PreferredPlayerCount)jn["beatmap"]["preferred_players"].AsInt;
        }

        public override JSONNode ToJSONVG()
        {
            var jn = Parser.NewJSONObject();

            jn["date_edited"] = DateTime.Now.ToString("yyyy-MM-dd_HH.mm.ss");
            jn["game_version"] = "24.9.2";

            if (workshopID != -1)
                jn["workshop_id"] = workshopID;

            return jn;
        }

        public override JSONNode ToJSON()
        {
            var jn = Parser.NewJSONObject();

            if (!string.IsNullOrEmpty(name))
                jn["name"] = name;
            jn["date_created"] = dateCreated;

            if (!string.IsNullOrEmpty(datePublished))
                jn["date_published"] = datePublished;
            jn["date_edited"] = DateTime.Now.ToString("yyyy-MM-dd_HH.mm.ss");
            jn["version_number"] = versionNumber.ToString();
            jn["game_version"] = gameVersion;
            jn["mod_version"] = modVersion;
            if (workshopID != -1)
                jn["workshop_id"] = workshopID;

            if (preferredPlayerCount != PreferredPlayerCount.Any)
                jn["preferred_players"] = ((int)preferredPlayerCount).ToString();

            return jn;
        }

        public bool PlayersCanjoin(int count) => preferredPlayerCount switch
        {
            PreferredPlayerCount.One => count == 1,
            PreferredPlayerCount.Two => count == 2,
            PreferredPlayerCount.Three => count == 3,
            PreferredPlayerCount.Four => count == 4,
            PreferredPlayerCount.MoreThanFour => count > 4,
            _ => true,
        };

        public override string ToString() => workshopID.ToString();

        #endregion
    }
}
