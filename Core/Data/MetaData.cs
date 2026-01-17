using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

using SimpleJSON;

using BetterLegacy.Core.Data.Network;
using BetterLegacy.Core.Helpers;

namespace BetterLegacy.Core.Data
{
    /// <summary>
    /// User readable information of the level.
    /// </summary>
    public class MetaData : PAObject<MetaData>, IPacket, IUploadable
    {
        public MetaData()
        {
            artist = new ArtistMetaData();
            creator = new CreatorMetaData();
            song = new SongMetaData();
            beatmap = new BeatmapMetaData();
        }

        #region Values

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

        public ArtistMetaData artist;
        public List<ArtistMetaData> artists;
        public CreatorMetaData creator;
        public List<CreatorMetaData> creators;
        public SongMetaData song;
        public BeatmapMetaData beatmap;

        /// <summary>
        /// Required asset packs for the level.
        /// </summary>
        public List<RequiredAssetPackData> requiredAssetPacks = new List<RequiredAssetPackData>();

        #region Server

        public string serverID;
        public string ServerID { get => serverID; set => serverID = value; }

        public string uploaderName;
        public string UploaderName { get => uploaderName; set => uploaderName = value; }

        public string uploaderID;
        public string UploaderID { get => uploaderID; set => uploaderID = value; }

        public List<ServerUser> uploaders = new List<ServerUser>();
        public List<ServerUser> Uploaders { get => uploaders; set => uploaders = value; }

        public ServerVisibility visibility;
        public ServerVisibility Visibility { get => visibility; set => visibility = value; }

        public string changelog;
        public string Changelog { get => changelog; set => changelog = value; }

        public List<string> tags = new List<string>();
        public List<string> ArcadeTags { get => tags; set => tags = value; }

        public string ObjectVersion { get; set; }

        public string DatePublished { get => beatmap.datePublished; set => beatmap.datePublished = value; }

        public int VersionNumber { get => beatmap.versionNumber; set => beatmap.versionNumber = value; }

        #endregion

        #region Arcade

        public string arcadeID;
        public string prevID;
        public string nextID;
        public bool isHubLevel;
        public bool requireUnlock;
        public bool unlockAfterCompletion = true;

        #endregion

        /// <summary>
        /// Marks the level as requiring a specific version. This means levels made in a specific version with specific features can only be playable on that version.
        /// </summary>
        public bool requireVersion;
        public DataManager.VersionComparison versionRange = DataManager.VersionComparison.EqualTo;

        public Dictionary<Rank, string[]> customSayings = new Dictionary<Rank, string[]>();

        #endregion

        #region Functions

        public override void CopyData(MetaData orig, bool newID = true)
        {
            artist.CopyData(orig.artist, newID);
            creator.CopyData(orig.creator, newID);
            song.CopyData(orig.song, newID);
            beatmap.CopyData(orig.beatmap, newID);
            requiredAssetPacks = new List<RequiredAssetPackData>(orig.requiredAssetPacks.Select(x => x.Copy(false)));

            arcadeID = orig.arcadeID;
            nextID = orig.nextID;
            prevID = orig.prevID;
            isHubLevel = orig.isHubLevel;
            requireUnlock = orig.requireUnlock;
            unlockAfterCompletion = orig.unlockAfterCompletion;
            requireVersion = orig.requireVersion;

            this.CopyUploadableData(orig);
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

                if (jn["artists"] != null)
                    artists = Parser.ParseObjectList<ArtistMetaData>(jn["artists"]);
                if (jn["creators"] != null)
                    creators = Parser.ParseObjectList<CreatorMetaData>(jn["creators"]);

                if (jn["asset_packs"] != null)
                    requiredAssetPacks = Parser.ParseObjectList<RequiredAssetPackData>(jn["asset_packs"]);

                if (!string.IsNullOrEmpty(jn["arcade_id"]))
                    arcadeID = jn["arcade_id"];

                if (!string.IsNullOrEmpty(jn["storyline"]["prev_level"]))
                    prevID = jn["storyline"]["prev_level"];

                if (!string.IsNullOrEmpty(jn["storyline"]["next_level"]))
                    nextID = jn["storyline"]["next_level"];

                this.ReadUploadableJSON(jn);

                if (!string.IsNullOrEmpty(jn["is_hub_level"]))
                    isHubLevel = jn["is_hub_level"].AsBool;

                if (!string.IsNullOrEmpty(jn["require_unlock"]))
                    requireUnlock = jn["require_unlock"].AsBool;

                if (!string.IsNullOrEmpty(jn["unlock_complete"]))
                    unlockAfterCompletion = jn["unlock_complete"].AsBool;

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

                if (jn["song"] != null && jn["song"]["tags"] != null)
                    for (int i = 0; i < jn["song"]["tags"].Count; i++)
                        tags.Add(jn["song"]["tags"][i].Value.Replace(" ", "_"));
            }
            catch
            {
                var artist = new ArtistMetaData("Corrupted", 0, string.Empty);
                var creator = new CreatorMetaData(SteamWrapper.inst.user.displayName, SteamWrapper.inst.user.id, string.Empty, 0);
                var song = new SongMetaData("Corrupt Metadata", 0, string.Empty, 140f, 100f, -1f, -1f, 2, string.Empty);
                var beatmap = new BeatmapMetaData("Level Name", string.Empty, string.Empty, string.Empty, ProjectArrhythmia.GameVersion.ToString(), 0, -1, LegacyPlugin.ModVersion.ToString());

                this.artist = artist;
                this.creator = creator;
                this.song = song;
                this.beatmap = beatmap;

                Debug.LogError($"{DataManager.inst.className}Something went wrong with parsing metadata!");
            }
        }

        public override JSONNode ToJSONVG()
        {
            var jn = Parser.NewJSONObject();

            jn["artist"] = artist.ToJSONVG();
            jn["creator"] = creator.ToJSONVG();
            jn["song"] = song.ToJSONVG();
            jn["beatmap"] = beatmap.ToJSONVG();

            return jn;
        }

        public override JSONNode ToJSON()
        {
            var jn = Parser.NewJSONObject();

            jn["artist"] = artist.ToJSON();
            jn["creator"] = creator.ToJSON();
            jn["song"] = song.ToJSON();
            jn["beatmap"] = beatmap.ToJSON();
            for (int i = 0; i < artists.Count; i++)
                jn["artists"][i] = artists[i].ToJSON();
            for (int i = 0; i < creators.Count; i++)
                jn["creators"][i] = creators[i].ToJSON();

            for (int i = 0; i < requiredAssetPacks.Count; i++)
                jn["asset_packs"][i] = requiredAssetPacks[i].ToJSON();

            if (!string.IsNullOrEmpty(arcadeID))
                jn["arcade_id"] = arcadeID;

            if (!string.IsNullOrEmpty(prevID))
                jn["storyline"]["prev_level"] = prevID;

            if (!string.IsNullOrEmpty(nextID))
                jn["storyline"]["next_level"] = nextID;

            this.WriteUploadableJSON(jn);

            if (isHubLevel)
                jn["is_hub_level"] = isHubLevel.ToString();

            if (requireUnlock)
                jn["require_unlock"] = requireUnlock.ToString();

            if (!unlockAfterCompletion)
                jn["unlock_complete"] = unlockAfterCompletion.ToString();

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

        public void ReadPacket(NetworkReader reader)
        {
            #region Interface

            this.ReadUploadablePacket(reader);

            #endregion

            artist = Packet.CreateFromPacket<ArtistMetaData>(reader);
            creator = Packet.CreateFromPacket<CreatorMetaData>(reader);
            song = Packet.CreateFromPacket<SongMetaData>(reader);
            beatmap = Packet.CreateFromPacket<BeatmapMetaData>(reader);

            Packet.ReadPacketList(artists, reader);
            Packet.ReadPacketList(creators, reader);
            Packet.ReadPacketList(requiredAssetPacks, reader);

            arcadeID = reader.ReadString();
            prevID = reader.ReadString();
            nextID = reader.ReadString();

            isHubLevel = reader.ReadBoolean();
            requireUnlock = reader.ReadBoolean();
            unlockAfterCompletion = reader.ReadBoolean();
            requireVersion = reader.ReadBoolean();
            versionRange = (DataManager.VersionComparison)reader.ReadByte();
            customSayings = reader.ReadDictionary(() => (Rank)reader.ReadByte(), () => reader.ReadStringArray());
        }

        public void WritePacket(NetworkWriter writer)
        {
            #region Interface

            this.WriteUploadablePacket(writer);

            #endregion

            artist.WritePacket(writer);
            creator.WritePacket(writer);
            song.WritePacket(writer);
            beatmap.WritePacket(writer);

            Packet.WritePacketList(artists, writer);
            Packet.WritePacketList(creators, writer);
            Packet.WritePacketList(requiredAssetPacks, writer);

            writer.Write(arcadeID);
            writer.Write(prevID);
            writer.Write(nextID);

            writer.Write(isHubLevel);
            writer.Write(requireUnlock);
            writer.Write(unlockAfterCompletion);
            writer.Write(requireVersion);
            writer.Write((byte)versionRange);
            writer.Write(customSayings,
                writeKey: key => writer.Write((byte)key.Ordinal),
                writeValue: value => writer.Write(value));
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

        public override int GetHashCode() => CoreHelper.CombineHashCodes(arcadeID);

        public override bool Equals(object obj) => obj is MetaData metaData && ID == metaData.ID;

        public override string ToString() => $"{ID}: {artist.name} - {song.title}";

        #endregion
    }

    /// <summary>
    /// Artist section of <see cref="MetaData"/>.
    /// </summary>
    public class ArtistMetaData : PAObject<ArtistMetaData>, IPacket
    {
        #region Constructors

        public ArtistMetaData() : this(string.Empty, 2, string.Empty) { }

        public ArtistMetaData(string name, int linkType, string link)
        {
            this.name = name;
            this.linkType = linkType;
            this.link = link;
        }

        #endregion

        #region Values

        /// <summary>
        /// URL to the artists' site.
        /// </summary>
        public string URL => AlephNetwork.GetURL(URLSource.Artist, linkType, link);

        public string name = "Artist";
        public int linkType = 2;
        public string link = string.Empty;

        #endregion

        #region Functions

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
            if (jn["artist"]["linkType"] != null)
                linkType = jn["artist"]["linkType"].AsInt;
            if (jn["artist"]["link_type"] != null)
                linkType = jn["artist"]["link_type"].AsInt;
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
            jn["link_type"] = !string.IsNullOrEmpty(link) ? linkType : 2;
            jn["link"] = link ?? string.Empty;

            return jn;
        }

        public void ReadPacket(NetworkReader reader)
        {
            name = reader.ReadString();
            linkType = reader.ReadInt32();
            link = reader.ReadString();
        }

        public void WritePacket(NetworkWriter writer)
        {
            writer.Write(name);
            writer.Write(linkType);
            writer.Write(link);
        }

        public override string ToString() => name;

        #endregion
    }

    /// <summary>
    /// Creator section of <see cref="MetaData"/>.
    /// </summary>
    public class CreatorMetaData : PAObject<CreatorMetaData>, IPacket
    {
        #region Constructors

        public CreatorMetaData() => name = "Unknown User";

        public CreatorMetaData(string name, int steamID, string link, int linkType)
        {
            this.name = name;
            this.steamID = steamID;
            this.linkType = linkType;
            this.link = link;
        }

        #endregion

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

        #region Functions

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
            if (jn["creator"]["steam_id"] != null)
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
            if (steamID != -1)
                jn["steam_id"] = steamID;

            if (!string.IsNullOrEmpty(link))
            {
                jn["link_type"] = linkType;
                jn["link"] = link;
            }

            return jn;
        }

        public void ReadPacket(NetworkReader reader)
        {
            name = reader.ReadString();
            steamID = reader.ReadInt64();
            linkType = reader.ReadInt32();
            link = reader.ReadString();
        }

        public void WritePacket(NetworkWriter writer)
        {
            writer.Write(name);
            writer.Write(steamID);
            writer.Write(linkType);
            writer.Write(link);
        }

        public override string ToString() => name;

        #endregion
    }

    /// <summary>
    /// Song section of <see cref="MetaData"/>.
    /// </summary>
    public class SongMetaData : PAObject<SongMetaData>, IPacket
    {
        #region Constructors

        public SongMetaData() { }

        public SongMetaData(string title, int difficulty, string description, float bpm, float time, float previewStart, float previewLength, int linkType, string link)
        {
            this.title = title;
            this.difficulty = difficulty;
            this.description = description;
            this.bpm = bpm;
            this.time = time;
            this.previewLength = previewLength;
            this.previewStart = previewStart;

            this.linkType = linkType;
            this.link = link;
        }

        #endregion

        #region Values

        public int linkType = 2;
        public string link;
        public List<string> tags = new List<string>();

        public DifficultyType Difficulty { get => difficulty; set => difficulty = value; }

        public string title = "Song Title Placeholder";

        public int difficulty = 2;

        public string description = "This is the default description!";

        public float bpm = 140f;

        public float time = 60f;

        public float previewStart = -1f;

        public float previewLength = -1f;

        #endregion

        #region Functions

        public override void CopyData(SongMetaData orig, bool newID = true)
        {
            title = orig.title;
            difficulty = orig.difficulty;
            description = orig.description;
            bpm = orig.bpm;
            time = orig.time;
            previewLength = orig.previewLength;
            previewStart = orig.previewStart;

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
        }

        public override void ReadJSON(JSONNode jn)
        {
            if (!string.IsNullOrEmpty(jn["song"]["title"]))
                title = jn["song"]["title"];
            if (jn["song"]["difficulty"] != null)
                difficulty = jn["song"]["difficulty"].AsInt;
            if (!string.IsNullOrEmpty(jn["song"]["description"]))
                description = jn["song"]["description"];
            if (!string.IsNullOrEmpty(jn["song"]["link"]))
                link = jn["song"]["link"];
            if (jn["song"]["linkType"] != null)
                linkType = jn["song"]["linkType"].AsInt;
            if (jn["song"]["link_type"] != null)
                linkType = jn["song"]["link_type"].AsInt;
            if (jn["song"]["bpm"] != null)
                bpm = jn["song"]["bpm"].AsFloat;
            if (jn["song"]["t"] != null)
                time = jn["song"]["t"].AsFloat;
            if (jn["song"]["preview_start"] != null)
                previewStart = jn["song"]["preview_start"].AsFloat;
            if (jn["song"]["preview_length"] != null)
                previewLength = jn["song"]["preview_length"].AsFloat;
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
            jn["difficulty"] = difficulty;
            jn["description"] = description ?? string.Empty;

            if (!string.IsNullOrEmpty(link))
            {
                jn["link_type"] = linkType;
                jn["link"] = link;
            }

            jn["bpm"] = bpm;
            jn["t"] = time;
            jn["preview_start"] = previewStart;
            jn["preview_length"] = previewLength;

            return jn;
        }

        public void ReadPacket(NetworkReader reader)
        {
            title = reader.ReadString();
            difficulty = reader.ReadInt32();
            description = reader.ReadString();

            linkType = reader.ReadInt32();
            link = reader.ReadString();

            bpm = reader.ReadSingle();
            time = reader.ReadSingle();
            previewStart = reader.ReadSingle();
            previewLength = reader.ReadSingle();
        }

        public void WritePacket(NetworkWriter writer)
        {
            writer.Write(title);
            writer.Write(difficulty);
            writer.Write(description);

            writer.Write(linkType);
            writer.Write(link);

            writer.Write(bpm);
            writer.Write(time);
            writer.Write(previewStart);
            writer.Write(previewLength);
        }

        public override string ToString() => title;

        #endregion
    }

    /// <summary>
    /// Beatmap section of <see cref="MetaData"/>.
    /// </summary>
    public class BeatmapMetaData : PAObject<BeatmapMetaData>, IPacket
    {
        #region Constructors

        public BeatmapMetaData()
        {
            name = "Level Name";
            gameVersion = ProjectArrhythmia.GameVersion.ToString();
            dateCreated = DateTime.Now.ToString(LegacyPlugin.DATE_TIME_FORMAT);
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

        #endregion

        #region Values

        public string name;

        public string dateEdited = DateTime.Now.ToString(LegacyPlugin.DATE_TIME_FORMAT);
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

        public PreferredControlType preferredControlType;

        public enum PreferredControlType
        {
            AnyDevice,
            KeyboardOnly,
            KeyboardExtraOnly,
            MouseOnly,
            KeyboardMouseOnly,
            ControllerOnly
        }

        /// <summary>
        /// URL to the videos' site.
        /// </summary>
        public string VideoURL => AlephNetwork.GetURL(URLSource.Video, videoLinkType, videoLink);

        public int videoLinkType;
        public string videoLink;

        #endregion

        #region Functions

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
            preferredControlType = orig.preferredControlType;

            videoLinkType = orig.videoLinkType;
            videoLink = orig.videoLink;
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
            if (jn["beatmap"]["preferred_control"] != null)
                preferredControlType = (PreferredControlType)jn["beatmap"]["preferred_control"].AsInt;
        }

        public override JSONNode ToJSONVG()
        {
            var jn = Parser.NewJSONObject();

            jn["date_edited"] = DateTime.Now.ToString(LegacyPlugin.DATE_TIME_FORMAT);
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
            jn["date_edited"] = DateTime.Now.ToString(LegacyPlugin.DATE_TIME_FORMAT);
            if (versionNumber != 0)
                jn["version_number"] = versionNumber;
            jn["game_version"] = gameVersion;
            jn["mod_version"] = modVersion;
            if (workshopID != -1)
                jn["workshop_id"] = workshopID;

            if (preferredPlayerCount != PreferredPlayerCount.Any)
                jn["preferred_players"] = (int)preferredPlayerCount;
            if (preferredControlType != PreferredControlType.AnyDevice)
                jn["preferred_control"] = (int)preferredControlType;

            if (!string.IsNullOrEmpty(videoLink))
            {
                jn["video_link_type"] = videoLinkType;
                jn["video_link"] = videoLink;
            }

            return jn;
        }

        public void ReadPacket(NetworkReader reader)
        {
            name = reader.ReadString();
            workshopID = reader.ReadInt64();

            dateCreated = reader.ReadString();
            dateEdited = reader.ReadString();

            gameVersion = reader.ReadString();
            modVersion = reader.ReadString();

            preferredPlayerCount = (PreferredPlayerCount)reader.ReadByte();
            preferredControlType = (PreferredControlType)reader.ReadByte();

            videoLinkType = reader.ReadInt32();
            videoLink = reader.ReadString();
        }

        public void WritePacket(NetworkWriter writer)
        {
            writer.Write(name);
            writer.Write(workshopID);

            writer.Write(dateCreated);
            writer.Write(dateEdited);

            writer.Write(gameVersion);
            writer.Write(modVersion);

            writer.Write((byte)preferredPlayerCount);
            writer.Write((byte)preferredControlType);

            writer.Write(videoLinkType);
            writer.Write(videoLink);
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

    /// <summary>
    /// Indicates an asset pack that is required to play the level. Useful for fully custom functions.
    /// </summary>
    public class RequiredAssetPackData : PAObject<RequiredAssetPackData>, IPacket
    {
        public RequiredAssetPackData() { }

        #region Values

        /// <summary>
        /// Asset Pack ID reference.
        /// </summary>
        public string packID;
        /// <summary>
        /// Asset Pack Name.
        /// </summary>
        public string packName;
        /// <summary>
        /// ID of the Asset Pack on the server.
        /// </summary>
        public string serverID;

        #endregion

        #region Functions

        public override void CopyData(RequiredAssetPackData orig, bool newID = true)
        {
            packID = orig.packID;
            packName = orig.packName;
            serverID = orig.serverID;
        }

        public override void ReadJSON(JSONNode jn)
        {
            if (!string.IsNullOrEmpty(jn["pack_id"]))
                packID = jn["pack_id"];
            if (!string.IsNullOrEmpty(jn["pack_name"]))
                packName = jn["pack_name"];
            if (!string.IsNullOrEmpty(jn["server_id"]))
                serverID = jn["server_id"];
        }

        public override JSONNode ToJSON()
        {
            var jn = Parser.NewJSONObject();

            if (!string.IsNullOrEmpty(jn["pack_id"]))
                jn["pack_id"] = packID;
            if (!string.IsNullOrEmpty(jn["pack_name"]))
                jn["pack_name"] = packName;
            if (!string.IsNullOrEmpty(jn["server_id"]))
                jn["server_id"] = serverID;

            return jn;
        }

        public void ReadPacket(NetworkReader reader)
        {
            packID = reader.ReadString();
            packName = reader.ReadString();
            serverID = reader.ReadString();
        }

        public void WritePacket(NetworkWriter writer)
        {
            writer.Write(packID);
            writer.Write(packName);
            writer.Write(serverID);
        }

        #endregion
    }
}
