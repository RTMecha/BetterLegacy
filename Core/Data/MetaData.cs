using BetterLegacy.Core.Helpers;
using SimpleJSON;
using System;
using System.Collections.Generic;
using UnityEngine;
using BaseArtist = DataManager.MetaData.Artist;
using BaseBeatmap = DataManager.MetaData.Beatmap;
using BaseCreator = DataManager.MetaData.Creator;
using BaseMetaData = DataManager.MetaData;
using BaseSong = DataManager.MetaData.Song;

namespace BetterLegacy.Core.Data
{
    public class MetaData : BaseMetaData
    {
        public MetaData()
        {
            artist = new LevelArtist();
            creator = new LevelCreator();
            song = new LevelSong();
            beatmap = new LevelBeatmap();
        }

        public MetaData(LevelArtist artist, LevelCreator creator, LevelSong song, LevelBeatmap beatmap) : base(artist, creator, song, beatmap)
        {

        }

        /// <summary>
        /// Checks if the current metadata is of the correct type.
        /// </summary>
        public static bool IsValid => DataManager.inst.metaData is MetaData;

        /// <summary>
        /// The current metadata.
        /// </summary>
        public static MetaData Current => (MetaData)DataManager.inst.metaData;

        public string collectionID;
        public int index;
        public string serverID;
        public string arcadeID;
        public string prevID;
        public string nextID;

        /// <summary>
        /// Gets the game version of the metadata.
        /// </summary>
        public Version Version => new Version(beatmap.game_version);

        /// <summary>
        /// Gets the mod version of the metadata.
        /// </summary>
        public Version ModVersion => new Version(LevelBeatmap.mod_version);

        /// <summary>
        /// Gets the prioritised ID. Arcade ID (if empty) > Server ID (if empty) > Steam Workshop ID (if empty) > -1
        /// </summary>
        public string ID =>
                !string.IsNullOrEmpty(arcadeID) && arcadeID != "-1" ?
                    arcadeID : !string.IsNullOrEmpty(serverID) && serverID != "-1" ?
                    serverID : !string.IsNullOrEmpty(LevelBeatmap.beatmap_id) && LevelBeatmap.beatmap_id != "-1" ?
                    LevelBeatmap.beatmap_id : "-1";

        #region Methods

        public static MetaData DeepCopy(MetaData orig) => new MetaData
        {
            artist = new LevelArtist
            {
                Link = orig.artist.Link,
                LinkType = orig.artist.LinkType,
                Name = orig.artist.Name
            },
            beatmap = new LevelBeatmap
            {
                date_edited = orig.beatmap.date_edited,
                game_version = orig.beatmap.game_version,
                version_number = orig.beatmap.version_number,
                workshop_id = orig.beatmap.workshop_id,
                beatmap_id = orig.LevelBeatmap.beatmap_id,
                date_created = orig.LevelBeatmap.date_created,
                date_published = orig.LevelBeatmap.date_published,
                mod_version = orig.LevelBeatmap.mod_version,
                name = orig.LevelBeatmap.name,
            },
            creator = new LevelCreator
            {
                steam_id = orig.creator.steam_id,
                steam_name = orig.creator.steam_name,
                link = orig.LevelCreator.link,
                linkType = orig.LevelCreator.linkType
            },
            song = new LevelSong
            {
                BPM = orig.song.BPM,
                description = orig.song.description,
                difficulty = orig.song.difficulty,
                previewLength = orig.song.previewLength,
                previewStart = orig.song.previewStart,
                time = orig.song.time,
                title = orig.song.title,
                tags = orig.LevelSong.tags,
                link = orig.LevelSong.link,
                linkType = orig.LevelSong.linkType,
            },
            serverID = orig.serverID,
            index = orig.index,
            collectionID = orig.collectionID,
        };

        public static MetaData ParseVG(JSONNode jn)
        {
            MetaData result;
            try
            {
                string artistName = "Artist Name";
                int linkType = 0;
                string link = "kaixomusic";
                try
                {
                    if (!string.IsNullOrEmpty(jn["artist"]["name"]))
                        artistName = jn["artist"]["name"];
                    if (!string.IsNullOrEmpty(jn["artist"]["link_type"]))
                        linkType = jn["artist"]["link_type"].AsInt;
                    if (!string.IsNullOrEmpty(jn["artist"]["link"]))
                        link = jn["artist"]["link"];
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Artist Error: {ex}");
                }

                var artist = new LevelArtist(artistName, linkType, link);

                string steam_name = "Mecha";
                int steam_id = -1;
                string creatorLink = "";
                int creatorLinkType = 0;

                try
                {
                    if (!string.IsNullOrEmpty(jn["creator"]["steam_name"]))
                        steam_name = jn["creator"]["steam_name"];
                    if (!string.IsNullOrEmpty(jn["creator"]["steam_id"]))
                        steam_id = jn["creator"]["steam_id"].AsInt;
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Creator Error: {ex}");
                }

                var creator = new LevelCreator(steam_name, steam_id, creatorLink, creatorLinkType);

                string title = "Pyrolysis";
                int difficulty = 2;
                string description = "This is the default description!";
                float bpm = 120f;
                float time = 60f;
                float previewStart = 0f;
                float previewLength = 30f;

                string[] tags = new string[]
                {
                };

                try
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
                catch (Exception ex)
                {
                    Debug.LogError($"Song Error: {ex}");
                }


                var song = new LevelSong(title, difficulty, description, bpm, time, previewStart, previewLength, tags, 0, "");

                string gameVersion = ProjectArrhythmia.GameVersion.ToString();
                string dateEdited = DateTime.Now.ToString("yyyy-MM-dd_HH.mm.ss");
                string dateCreated = DateTime.Now.ToString("yyyy-MM-dd_HH.mm.ss");
                string workshopID = "-1";
                int num = 0;
                var modVersion = LegacyPlugin.ModVersion.ToString();

                try
                {
                    if (!string.IsNullOrEmpty(jn["beatmap"]["game_version"]))
                        gameVersion = jn["beatmap"]["game_version"];
                    if (!string.IsNullOrEmpty(jn["beatmap"]["mod_version"]))
                        modVersion = jn["beatmap"]["mod_version"];
                    if (!string.IsNullOrEmpty(jn["beatmap"]["date_edited"]))
                        dateEdited = jn["beatmap"]["date_edited"];
                    if (!string.IsNullOrEmpty(jn["beatmap"]["date_created"]))
                        dateCreated = jn["beatmap"]["date_created"];
                    if (!string.IsNullOrEmpty(jn["beatmap"]["version_number"]))
                        num = jn["beatmap"]["version_number"].AsInt;
                    if (!string.IsNullOrEmpty(jn["beatmap"]["workshop_id"]))
                        workshopID = jn["beatmap"]["workshop_id"];
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Beatmap Error: {ex}");
                }

                var beatmap = new LevelBeatmap(title, dateEdited, dateCreated, "", gameVersion, num, workshopID.ToString(), modVersion);

                result = new MetaData(artist, creator, song, beatmap);
            }
            catch (Exception ex)
            {
                var artist2 = new LevelArtist("Corrupted", 0, "");
                var creator2 = new LevelCreator(SteamWrapper.inst.user.displayName, SteamWrapper.inst.user.id, "", 0);
                var song2 = new LevelSong("Corrupt Metadata", 0, "", 140f, 100f, -1f, -1f, new string[] { "Corrupted" }, 0, "");
                var beatmap2 = new LevelBeatmap("Corrupted Level", "", "", "", ProjectArrhythmia.GameVersion.ToString(), 0, "-1", LegacyPlugin.ModVersion.ToString());
                result = new MetaData(artist2, creator2, song2, beatmap2);
                Debug.LogError($"{DataManager.inst.className}Something went wrong with parsing metadata!\n{ex}");
            }
            return result;
        }

        public static MetaData Parse(JSONNode jn)
        {
            MetaData result;
            try
            {
                string name = "Artist Name";
                int linkType = 0;
                string link = "kaixomusic";
                try
                {
                    if (!string.IsNullOrEmpty(jn["artist"]["name"]))
                        name = jn["artist"]["name"];
                    if (!string.IsNullOrEmpty(jn["artist"]["linkType"]))
                        linkType = jn["artist"]["linkType"].AsInt;
                    if (!string.IsNullOrEmpty(jn["artist"]["link"]))
                        link = jn["artist"]["link"];
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Artist Error: {ex}");
                }

                var artist = new LevelArtist(name, linkType, link);

                string steam_name = "RTMecha";
                int steam_id = -1;
                string creatorLink = "";
                int creatorLinkType = 0;

                try
                {
                    if (!string.IsNullOrEmpty(jn["creator"]["steam_name"]))
                        steam_name = jn["creator"]["steam_name"];
                    if (!string.IsNullOrEmpty(jn["creator"]["steam_id"]))
                        steam_id = jn["creator"]["steam_id"].AsInt;
                    if (!string.IsNullOrEmpty(jn["creator"]["link"]))
                        creatorLink = jn["creator"]["link"];
                    if (!string.IsNullOrEmpty(jn["creator"]["linkType"]))
                        creatorLinkType = jn["creator"]["linkType"].AsInt;
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Creator Error: {ex}");
                }

                var creator = new LevelCreator(steam_name, steam_id, creatorLink, creatorLinkType);

                string title = "Intertia";
                int difficulty = 2;
                string description = "This is the default description!";
                string songLink = "album/full-devoid";
                int songLinkType = 2;
                float bpm = 120f;
                float time = 60f;
                float previewStart = 0f;
                float previewLength = 30f;

                string[] tags = new string[]
                {
                };

                try
                {
                    if (!string.IsNullOrEmpty(jn["song"]["title"]))
                        title = jn["song"]["title"];
                    if (!string.IsNullOrEmpty(jn["song"]["difficulty"]))
                        difficulty = jn["song"]["difficulty"].AsInt;
                    if (!string.IsNullOrEmpty(jn["song"]["description"]))
                        description = jn["song"]["description"];
                    if (!string.IsNullOrEmpty(jn["song"]["link"]))
                        songLink = jn["song"]["link"];
                    if (!string.IsNullOrEmpty(jn["song"]["linkType"]))
                        songLinkType = jn["song"]["linkType"].AsInt;
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
                            tags[i] = jn["song"]["tags"][i];
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Song Error: {ex}");
                }

                var song = new LevelSong(title, difficulty, description, bpm, time, previewStart, previewLength, tags, songLinkType, songLink);

                string levelName = "Level Name";
                string gameVersion = ProjectArrhythmia.GameVersion.ToString();
                string dateEdited = DateTime.Now.ToString("yyyy-MM-dd_HH.mm.ss");
                string dateCreated = DateTime.Now.ToString("yyyy-MM-dd_HH.mm.ss");
                string datePublished = "";
                string workshopID = "-1";
                int num = 0;
                var modVersion = LegacyPlugin.ModVersion.ToString();

                try
                {
                    if (!string.IsNullOrEmpty(jn["beatmap"]["name"]))
                        levelName = jn["beatmap"]["name"];
                    else
                        levelName = title;
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
                    if (!string.IsNullOrEmpty(jn["beatmap"]["version_number"]))
                        num = jn["beatmap"]["version_number"].AsInt;
                    if (!string.IsNullOrEmpty(jn["beatmap"]["workshop_id"]))
                        workshopID = jn["beatmap"]["workshop_id"];
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Beatmap Error: {ex}");
                }

                var beatmap = new LevelBeatmap(levelName, dateEdited, dateCreated, datePublished, gameVersion, num, workshopID, modVersion);

                result = new MetaData(artist, creator, song, beatmap);
                if (!string.IsNullOrEmpty(jn["server_id"]))
                    result.serverID = jn["server_id"];
                if (!string.IsNullOrEmpty(jn["arcade_id"]))
                    result.arcadeID = jn["arcade_id"];

                if (!string.IsNullOrEmpty(jn["storyline"]["prev_level"]))
                    result.prevID = jn["storyline"]["prev_level"];

                if (!string.IsNullOrEmpty(jn["storyline"]["next_level"]))
                    result.nextID = jn["storyline"]["next_level"];
            }
            catch
            {
                var artist2 = new LevelArtist("Corrupted", 0, "");
                var creator2 = new LevelCreator(SteamWrapper.inst.user.displayName, SteamWrapper.inst.user.id, "", 0);
                var song2 = new LevelSong("Corrupt Metadata", 0, "", 140f, 100f, -1f, -1f, new string[] { "Corrupted" }, 2, "album/full-devoid");
                var beatmap2 = new LevelBeatmap("Level Name", "", "", "", ProjectArrhythmia.GameVersion.ToString(), 0, "-1", LegacyPlugin.ModVersion.ToString());
                result = new MetaData(artist2, creator2, song2, beatmap2);
                Debug.LogError($"{DataManager.inst.className}Something went wrong with parsing metadata!");
            }
            return result;
        }

        public JSONNode ToJSONVG()
        {
            var jn = JSON.Parse("{}");

            jn["artist"]["name"] = artist.Name;
            jn["artist"]["link"] = artist.Link;
            jn["artist"]["link_type"] = artist.LinkType;

            jn["creator"]["steam_name"] = creator.steam_name;
            jn["creator"]["steam_id"] = creator.steam_id;

            jn["song"]["title"] = song.title;
            jn["song"]["difficulty"] = song.difficulty;
            jn["song"]["description"] = song.description;
            jn["song"]["bpm"] = song.BPM;
            jn["song"]["time"] = song.time;
            jn["song"]["preview_start"] = song.previewStart;
            jn["song"]["preview_length"] = song.previewLength;

            jn["beatmap"]["date_edited"] = DateTime.Now.ToString("yyyy-MM-dd_HH.mm.ss");
            jn["beatmap"]["game_version"] = "24.1.7";
            jn["beatmap"]["workshop_id"] = LevelBeatmap.beatmap_id;

            return jn;
        }

        public JSONNode ToJSON()
        {
            var jn = JSON.Parse("{}");

            jn["artist"]["name"] = artist.Name;
            jn["artist"]["link"] = artist.Link;
            jn["artist"]["linkType"] = artist.LinkType.ToString();

            jn["creator"]["steam_name"] = creator.steam_name;
            jn["creator"]["steam_id"] = creator.steam_id.ToString();
            jn["creator"]["link"] = LevelCreator.link;
            jn["creator"]["linkType"] = LevelCreator.linkType.ToString();

            jn["song"]["title"] = song.title;
            jn["song"]["difficulty"] = song.difficulty.ToString();
            jn["song"]["description"] = song.description;
            jn["song"]["link"] = LevelSong.link;
            jn["song"]["linkType"] = LevelSong.linkType.ToString();
            jn["song"]["bpm"] = song.BPM.ToString();
            jn["song"]["t"] = song.time.ToString();
            jn["song"]["preview_start"] = song.previewStart.ToString();
            jn["song"]["preview_length"] = song.previewLength.ToString();
            for (int i = 0; i < LevelSong.tags.Length; i++)
                jn["song"]["tags"][i] = LevelSong.tags[i];

            jn["beatmap"]["name"] = LevelBeatmap.name;
            jn["beatmap"]["date_created"] = LevelBeatmap.date_created;
            jn["beatmap"]["date_edited"] = DateTime.Now.ToString("yyyy-MM-dd_HH.mm.ss");
            jn["beatmap"]["version_number"] = beatmap.version_number.ToString();
            jn["beatmap"]["game_version"] = beatmap.game_version;
            jn["beatmap"]["mod_version"] = LevelBeatmap.mod_version;
            jn["beatmap"]["workshop_id"] = LevelBeatmap.beatmap_id;

            if (!string.IsNullOrEmpty(serverID))
                jn["server_id"] = serverID;

            if (!string.IsNullOrEmpty(arcadeID))
                jn["arcade_id"] = arcadeID;

            if (!string.IsNullOrEmpty(prevID))
                jn["storyline"]["prev_level"] = prevID;

            if (!string.IsNullOrEmpty(nextID))
                jn["storyline"]["next_level"] = nextID;

            return jn;
        }

        #endregion

        /// <summary>
        /// For future "more credits" section of an arcade level.
        /// </summary>
        public List<LevelArtist> Artists { get; set; } = new List<LevelArtist>();

        /// <summary>
        /// For future "more credits" section of an arcade level.
        /// </summary>
        public List<LevelCreator> Creators { get; set; } = new List<LevelCreator>();

        public LevelArtist LevelArtist => (LevelArtist)artist;
        public LevelCreator LevelCreator => (LevelCreator)creator;
        public LevelSong LevelSong => (LevelSong)song;
        public LevelBeatmap LevelBeatmap => (LevelBeatmap)beatmap;

        /// <summary>
        /// Formats the song URL into a correct link format, in cases where the artist name is included in the song link somewhere.
        /// </summary>
        public string SongURL => CoreHelper.GetURL(0, LevelSong.linkType, LevelSong.linkType == 2 ? artist.Name + "," + LevelSong.link : LevelSong.link);

        #region Operators

        public static implicit operator bool(MetaData exists) => exists != null;

        public override bool Equals(object obj) => obj is MetaData && ID == (obj as MetaData).ID;

        public override string ToString() => $"{ID}: {artist.Name} - {song.title}";

        #endregion
    }

    [Serializable]
    public class LevelArtist : BaseArtist
    {
        public LevelArtist() : base()
        {
            Name = "Kaixo";
            LinkType = 2;
            Link = "kaixo";
        }

        public LevelArtist(string name, int linkType, string link) : base(name, linkType, link)
        {

        }

        public string URL
            => LinkType < 0 || LinkType > DataManager.inst.linkTypes.Count - 1 || Link.Contains("http://") || Link.Contains("https://") ? null : string.Format(DataManager.inst.linkTypes[LinkType].linkFormat, Link);

        #region Operators

        public static implicit operator bool(LevelArtist exists) => exists != null;

        public override string ToString() => Name;

        #endregion
    }

    [Serializable]
    public class LevelCreator : BaseCreator
    {
        public LevelCreator() : base()
        {
            steam_name = "Unknown User";
        }

        public LevelCreator(string steam_name, int steam_id, string link, int linkType) : base(steam_name, steam_id)
        {
            this.link = link;
            this.linkType = linkType;
        }

        public string URL
            => linkType < 0 || linkType > creatorLinkTypes.Count - 1 || link.Contains("http://") || link.Contains("https://") ? null : string.Format(creatorLinkTypes[linkType].linkFormat, link);


        public int linkType;
        public string link;

        public static List<DataManager.LinkType> creatorLinkTypes = new List<DataManager.LinkType>
        {
            new DataManager.LinkType("YouTube", "https://www.youtube.com/c/{0}"),
            new DataManager.LinkType("Newgrounds", "https://{0}.newgrounds.com/"),
            new DataManager.LinkType("Discord", "https://discord.gg/{0}"),
            new DataManager.LinkType("Patreon", "https://patreon.com/{0}"),
            new DataManager.LinkType("Twitter", "https://twitter.com/{0}"),
        };

        #region Operators

        public static implicit operator bool(LevelCreator exists) => exists != null;

        public override string ToString() => steam_name;

        #endregion
    }

    [Serializable]
    public class LevelSong : BaseSong
    {
        public LevelSong() : base()
        {
            title = "Intertia";
        }

        public LevelSong(string title, int difficulty, string description, float BPM, float time, float previewStart, float previewLength, string[] tags, int linkType, string link) : base(title, difficulty, description, BPM, time, previewStart, previewLength)
        {
            this.tags = tags;
            this.linkType = linkType;
            this.link = link;
        }

        public string URL => CoreHelper.GetURL(0, linkType, link);
        public int linkType = 2;
        public string link = "album/full-devoid";
        public string[] tags;

        #region Operators

        public static implicit operator bool(LevelSong exists) => exists != null;

        public override string ToString() => title;

        #endregion
    }

    [Serializable]
    public class LevelBeatmap : BaseBeatmap
    {
        public LevelBeatmap() : base()
        {
            name = "Level Name";
            beatmap_id = workshop_id.ToString();
            game_version = ProjectArrhythmia.GameVersion.ToString();
            date_created = DateTime.Now.ToString("yyyy-MM-dd_HH.mm.ss");
            mod_version = LegacyPlugin.ModVersion.ToString();
        }

        public LevelBeatmap(string name, string dateEdited, string dateCreated, string datePublished, string gameVersion, int versionNumber, string beatmapID, string modVersion)
        {
            this.name = name;
            date_edited = dateEdited;
            game_version = gameVersion;
            version_number = versionNumber;

            beatmap_id = beatmapID;
            date_created = dateCreated;
            date_published = datePublished;
            mod_version = modVersion;
        }

        public string name;
        public string beatmap_id;
        public string date_created;
        public string date_published;
        public string mod_version;

        #region Operators

        public static implicit operator bool(LevelBeatmap exists) => exists != null;

        public override string ToString() => beatmap_id;

        #endregion
    }
}
