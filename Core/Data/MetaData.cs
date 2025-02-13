using SimpleJSON;
using System;
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

        public MetaData(LevelArtist artist, LevelCreator creator, LevelSong song, LevelBeatmap beatmap)
        {
            this.artist = artist;
            this.creator = creator;
            this.song = song;
            this.beatmap = beatmap;
        }

        #region Properties

        #region Instance

        /// <summary>
        /// Checks if the current MetaData is of the correct type.
        /// </summary>
        public static bool IsValid => DataManager.inst.metaData is MetaData;

        /// <summary>
        /// The current MetaData that is being used by the game.
        /// </summary>
        public static MetaData Current { get => DataManager.inst.metaData as MetaData; set => DataManager.inst.metaData = value; }

        /// <summary>
        /// Gets the prioritised ID. Arcade ID (if empty) > Server ID (if empty) > Steam Workshop ID (if empty) > -1
        /// </summary>
        public string ID =>
                !string.IsNullOrEmpty(arcadeID) && arcadeID != "-1" ?
                    arcadeID : !string.IsNullOrEmpty(serverID) && serverID != "-1" ?
                    serverID : !string.IsNullOrEmpty(beatmap.beatmap_id) && beatmap.beatmap_id != "-1" ?
                    beatmap.beatmap_id : "-1";

        #endregion

        /// <summary>
        /// Gets the game version of the metadata.
        /// </summary>
        public Version Version => new Version(beatmap.game_version);

        /// <summary>
        /// Gets the mod version of the metadata.
        /// </summary>
        public Version ModVersion => new Version(beatmap.mod_version);

        /// <summary>
        /// Formats the song URL into a correct link format, in cases where the artist name is included in the song link somewhere.
        /// </summary>
        public string SongURL => string.IsNullOrEmpty(song.link) || string.IsNullOrEmpty(artist.Link) ? null : AlephNetwork.GetURL(URLSource.Song, song.linkType, song.linkType == 2 ? artist.Link + "," + song.link : song.link);

        #endregion

        #region Methods

        /// <summary>
        /// Creates a copy of a <see cref="MetaData"/>.
        /// </summary>
        /// <param name="orig">Original to copy.</param>
        /// <returns>Returns a copied <see cref="MetaData"/>.</returns>
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
                beatmap_id = orig.beatmap.beatmap_id,
                date_created = orig.beatmap.date_created,
                date_published = orig.beatmap.date_published,
                mod_version = orig.beatmap.mod_version,
                name = orig.beatmap.name,
            },
            creator = new LevelCreator
            {
                steam_id = orig.creator.steam_id,
                steam_name = orig.creator.steam_name,
                link = orig.creator.link,
                linkType = orig.creator.linkType
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
                tags = orig.song.tags,
                link = orig.song.link,
                linkType = orig.song.linkType,
            },
            serverID = orig.serverID,
            uploaderName = orig.uploaderName,
            uploaderID = orig.uploaderID,
            index = orig.index,
            collectionID = orig.collectionID,
            isHubLevel = orig.isHubLevel,
            requireUnlock = orig.requireUnlock,
            unlockAfterCompletion = orig.unlockAfterCompletion,
            arcadeID = orig.arcadeID,
            nextID = orig.nextID,
            prevID = orig.prevID,
            visibility = orig.visibility,
            changelog = orig.changelog,
            requireVersion = orig.requireVersion,
        };

        /// <summary>
        /// Parses a levels' metadata from a file.
        /// </summary>
        /// <param name="path">File to parse.</param>
        /// <param name="fileType">The type of Project Arrhythmia the file is from.</param>
        /// <param name="setDefaultValues">If defaults should be set when a value is null.</param>
        /// <returns>Returns a parsed <see cref="MetaData"/>.</returns>
        public static MetaData ReadFromFile(string path, ArrhythmiaType fileType, bool setDefaultValues = true) => fileType switch
        {
            ArrhythmiaType.LS => Parse(JSON.Parse(RTFile.ReadFromFile(path)), setDefaultValues),
            ArrhythmiaType.VG => ParseVG(JSON.Parse(RTFile.ReadFromFile(path))),
            _ => null,
        };

        /// <summary>
        /// Parses a levels' metadata from JSON in the VG format.
        /// </summary>
        /// <param name="jn">VG JSON to parse.</param>
        /// <returns>Returns a parsed <see cref="MetaData"/>.</returns>
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
                string creatorLink = null;
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

                string[] tags = new string[] { };

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

        /// <summary>
        /// Parses a levels' metadata from JSON in the LS format.
        /// </summary>
        /// <param name="jn">LS JSON to parse.</param>
        /// <returns>Returns a parsed <see cref="MetaData"/>.</returns>
        public static MetaData Parse(JSONNode jn, bool setDefaultValues = true)
        {
            MetaData result;
            try
            {
                string name = "Artist Name";
                int linkType = 0;
                string link = !setDefaultValues ? "" : "kaixomusic";
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
                string songLink = !setDefaultValues ? null : "album/full-devoid";
                int songLinkType = 2;
                float bpm = 120f;
                float time = 60f;
                float previewStart = 0f;
                float previewLength = 30f;

                string[] tags = new string[] { };

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
                            tags[i] = jn["song"]["tags"][i].Value.Replace(" ", "_");
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
                LevelBeatmap.PreferredPlayerCount preferredPlayerCount = LevelBeatmap.PreferredPlayerCount.Any;

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
                    if (!string.IsNullOrEmpty(jn["beatmap"]["preferred_players"]))
                        preferredPlayerCount = (LevelBeatmap.PreferredPlayerCount)jn["beatmap"]["preferred_players"].AsInt;
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Beatmap Error: {ex}");
                }

                var beatmap = new LevelBeatmap(levelName, dateEdited, dateCreated, datePublished, gameVersion, num, workshopID, modVersion);
                beatmap.preferredPlayerCount = preferredPlayerCount;

                result = new MetaData(artist, creator, song, beatmap);
                if (!string.IsNullOrEmpty(jn["server_id"]))
                    result.serverID = jn["server_id"];
                if (!string.IsNullOrEmpty(jn["arcade_id"]))
                    result.arcadeID = jn["arcade_id"];

                if (!string.IsNullOrEmpty(jn["storyline"]["prev_level"]))
                    result.prevID = jn["storyline"]["prev_level"];

                if (!string.IsNullOrEmpty(jn["storyline"]["next_level"]))
                    result.nextID = jn["storyline"]["next_level"];

                if (!string.IsNullOrEmpty(jn["uploader_name"]))
                    result.uploaderName = jn["uploader_name"];
                else
                    result.uploaderName = creator.steam_name;

                if (!string.IsNullOrEmpty(jn["uploader_id"]))
                    result.uploaderID = jn["uploader_id"];

                if (!string.IsNullOrEmpty(jn["is_hub_level"]))
                    result.isHubLevel = jn["is_hub_level"].AsBool;

                if (!string.IsNullOrEmpty(jn["require_unlock"]))
                    result.requireUnlock = jn["require_unlock"].AsBool;

                if (!string.IsNullOrEmpty(jn["unlock_complete"]))
                    result.unlockAfterCompletion = jn["unlock_complete"].AsBool;

                if (!string.IsNullOrEmpty(jn["visibility"]))
                    result.visibility = (ServerVisibility)jn["visibility"].AsInt;

                if (!string.IsNullOrEmpty(jn["changelog"]))
                    result.changelog = jn["changelog"];

                if (!string.IsNullOrEmpty(jn["require_version"]))
                    result.requireVersion = jn["require_version"].AsBool;

                if (!string.IsNullOrEmpty(jn["version_comparison"]))
                    result.versionRange = (DataManager.VersionComparison)jn["version_comparison"].AsInt;
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

        /// <summary>
        /// Writes the <see cref="MetaData"/> to a VG format JSON.
        /// </summary>
        /// <returns>Returns a JSON object representing the <see cref="MetaData"/>.</returns>
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
            jn["beatmap"]["game_version"] = "24.9.2";

            if (!string.IsNullOrEmpty(beatmap.beatmap_id) && beatmap.beatmap_id != "-1")
                jn["beatmap"]["workshop_id"] = beatmap.beatmap_id;

            return jn;
        }

        /// <summary>
        /// Writes the <see cref="MetaData"/> to a LS format JSON.
        /// </summary>
        /// <returns>Returns a JSON object representing the <see cref="MetaData"/>.</returns>
        public JSONNode ToJSON()
        {
            var jn = JSON.Parse("{}");

            jn["artist"]["name"] = artist.Name;
            jn["artist"]["link"] = !string.IsNullOrEmpty(artist.Link) ? artist.Link : "kaixo";
            jn["artist"]["linkType"] = !string.IsNullOrEmpty(artist.Link) ? artist.LinkType.ToString() : 2;

            jn["creator"]["steam_name"] = creator.steam_name;
            jn["creator"]["steam_id"] = creator.steam_id.ToString();
            if (!string.IsNullOrEmpty(creator.link))
            {
                jn["creator"]["link"] = creator.link;
                jn["creator"]["linkType"] = creator.linkType.ToString();
            }

            jn["song"]["title"] = song.title;
            jn["song"]["difficulty"] = song.difficulty.ToString();
            jn["song"]["description"] = song.description;

            if (!string.IsNullOrEmpty(song.link))
            {
                jn["song"]["link"] = song.link;
                jn["song"]["linkType"] = song.linkType.ToString();
            }
            jn["song"]["bpm"] = song.BPM.ToString();
            jn["song"]["t"] = song.time.ToString();
            jn["song"]["preview_start"] = song.previewStart.ToString();
            jn["song"]["preview_length"] = song.previewLength.ToString();

            if (song.tags != null)
                for (int i = 0; i < song.tags.Length; i++)
                    jn["song"]["tags"][i] = song.tags[i];

            jn["beatmap"]["name"] = !string.IsNullOrEmpty(beatmap.name) ? beatmap.name : song.title;
            jn["beatmap"]["date_created"] = beatmap.date_created;

            if (!string.IsNullOrEmpty(beatmap.date_published))
                jn["beatmap"]["date_published"] = beatmap.date_published;
            jn["beatmap"]["date_edited"] = DateTime.Now.ToString("yyyy-MM-dd_HH.mm.ss");
            jn["beatmap"]["version_number"] = beatmap.version_number.ToString();
            jn["beatmap"]["game_version"] = beatmap.game_version;
            jn["beatmap"]["mod_version"] = beatmap.mod_version;
            jn["beatmap"]["workshop_id"] = !string.IsNullOrEmpty(beatmap.beatmap_id) ? beatmap.beatmap_id : "-1";

            if (beatmap.preferredPlayerCount != LevelBeatmap.PreferredPlayerCount.Any)
                jn["beatmap"]["preferred_players"] = ((int)beatmap.preferredPlayerCount).ToString();

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

        #endregion

        #region Fields

        public new LevelArtist artist;
        public new LevelCreator creator;
        public new LevelSong song;
        public new LevelBeatmap beatmap;

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

        #endregion

        #region Operators

        public static implicit operator bool(MetaData exists) => exists != null;

        public override bool Equals(object obj) => obj is MetaData && ID == (obj as MetaData).ID;

        public override string ToString() => $"{ID}: {artist.Name} - {song.title}";

        #endregion
    }

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

        public string URL => AlephNetwork.GetURL(URLSource.Creator, linkType, link);


        public int linkType;
        public string link;

        #region Operators

        public static implicit operator bool(LevelCreator exists) => exists != null;

        public override string ToString() => steam_name;

        #endregion
    }

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

        public int linkType = 2;
        public string link;
        public string[] tags;

        public LevelDifficulty LevelDifficulty => (LevelDifficulty)(difficulty + 1);

        #region Operators

        public static implicit operator bool(LevelSong exists) => exists != null;

        public override string ToString() => title;

        #endregion
    }

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

        #region Operators

        public static implicit operator bool(LevelBeatmap exists) => exists != null;

        public override string ToString() => beatmap_id;

        #endregion
    }
}
