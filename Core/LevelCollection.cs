using BetterLegacy.Core.Data;
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
    public class LevelCollection : Exists
    {
        public LevelCollection()
        {
            id = LSText.randomNumString(16);
        }

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

        public int Count => levels.Count;

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

        public static LevelCollection Parse(string path, JSONNode jn)
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
                if (jn["levels"][i]["path"] != null && (RTFile.FileExists(RTFile.CombinePaths(path, $"{jn["levels"][i]["path"].Value}/", Level.LEVEL_LSB)) || RTFile.FileExists(RTFile.CombinePaths(path, $"{jn["levels"][i]["path"].Value}/", Level.LEVEL_VGD))))
                {
                    var levelFolder = RTFile.CombinePaths(path, $"{jn["levels"][i]["path"].Value}/");

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

                    collection.AddJSON(jn["levels"][i], new Level(levelFolder) { fromCollection = true });
                }
                else if (jn["levels"][i]["arcade_id"] != null && LevelManager.Levels.TryFind(x => x.id == jn["levels"][i]["arcade_id"], out Level arcadeLevel))
                    collection.AddJSON(jn["levels"][i], new Level(arcadeLevel.path) { fromCollection = true });
                else if (jn["levels"][i]["workshop_id"] != null && SteamWorkshopManager.inst.Levels.TryFind(x => x.id == jn["levels"][i]["workshop_id"], out Level steamLevel))
                    collection.AddJSON(jn["levels"][i], new Level(steamLevel.path) { fromCollection = true });
                else
                    collection.levels.Add(null);

                collection.levelInformation.Add(LevelInfo.Parse(jn["levels"][i], i));
            }

            collection.icon = RTFile.FileExists($"{path}icon.png") ? SpriteHelper.LoadSprite($"{path}icon.png") : SpriteHelper.LoadSprite($"{path}icon.jpg");
            collection.banner = RTFile.FileExists($"{path}banner.png") ? SpriteHelper.LoadSprite($"{path}banner.png") : SpriteHelper.LoadSprite($"{path}banner.jpg");

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
            level.id = jn["id"];

            if (LevelManager.Saves.TryFind(x => x.ID == level.id, out LevelManager.PlayerData playerData))
                level.playerData = playerData;

            levels.Add(level);
        }

        /// <summary>
        /// A list of levels that exist in the level collection file, regardless of whether a level was loaded or not.
        /// </summary>
        public List<LevelInfo> levelInformation = new List<LevelInfo>();

        public class LevelInfo
        {
            public int index;
            public string id;

            public string path;
            public string editorPath;

            public string songTitle;
            public string name;
            public string creator;

            public string arcadeID;
            public string serverID;
            public string workshopID;

            public bool hidden;
            public bool requireUnlock;

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
        }

        public static void Test()
        {
            //var collection = new LevelCollection();
            //var path = $"{RTFile.ApplicationDirectory}beatmaps/arcade/Collection Test";

            //if (!RTFile.DirectoryExists(path))
            //    Directory.CreateDirectory(path);

            //collection.path = $"{path}/";

            //collection.AddLevel(new Level($"{RTFile.ApplicationDirectory}beatmaps/arcade/2581783822/"));

            //collection.Save();
        }

        public Level EntryLevel => this[EntryLevelIndex];

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
        /// The collection file.
        /// </summary>
        public const string COLLECTION_LSCO = "collection.lsco";
        /// <summary>
        /// The collection preview audio file.
        /// </summary>
        public const string PREVIEW_OGG = "preview.ogg";

        public void Move(string id, int moveTo)
        {
            levels.Move(x => x.id == id, moveTo);
            levelInformation.Move(x => x.id == id, moveTo);
            levelInformation[moveTo].index = moveTo;
        }

        public void Save()
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

            if (icon)
                SpriteHelper.SaveSprite(icon, $"{path}icon.png");
            if (banner)
                SpriteHelper.SaveSprite(banner, $"{path}banner.png");
            RTFile.WriteToFile($"{path}collection.lsco", jn.ToString(3));
        }

        public void AddLevelToFolder(Level level)
        {
            if (levels.Any(x => x.id == level.id)) // don't want to have duplicate levels
                return;

            var path = Path.GetDirectoryName(level.path);
            var folderName = Path.GetFileName(path);

            var files = Directory.GetFiles(level.path, "*", SearchOption.AllDirectories);
            for (int i = 0; i < files.Length; i++)
            {
                var file = files[i];
                var copyToPath = file.Replace("\\", "/").Replace(level.path, $"{this.path}{folderName}/");
                if (!RTFile.DirectoryExists(Path.GetDirectoryName(copyToPath)))
                    Directory.CreateDirectory(Path.GetDirectoryName(copyToPath));

                File.Copy(file, copyToPath, RTFile.FileExists(copyToPath));
            }

            var actualLevel = new Level($"{this.path}{folderName}/");

            levels.Add(actualLevel);
            levelInformation.Add(LevelInfo.FromLevel(actualLevel));
        }

        public void RemoveLevelFromFolder(Level level)
        {
            if (!levels.Any(x => x.id == level.id))
                return;

            var actualLevel = levels.Find(x => x.id == level.id);

            Directory.Delete(Path.GetDirectoryName(actualLevel.path), true);

            levels.RemoveAll(x => x.id == level.id);
            levelInformation.RemoveAll(x => x.id == level.id);
        }
    }
}
