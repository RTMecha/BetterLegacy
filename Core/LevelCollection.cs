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

                    var level = new Level(levelFolder) { fromCollection = true };

                    if (level.metadata)
                    {
                        level.metadata = MetaData.DeepCopy(level.metadata);
                        level.metadata.arcadeID = jn["levels"][i]["id"];
                        if (jn["levels"][i]["require_unlock"] != null)
                            level.metadata.requireUnlock = jn["levels"][i]["require_unlock"];
                    }
                    level.id = jn["levels"][i]["id"];

                    if (LevelManager.Saves.TryFind(x => x.ID == level.id, out LevelManager.PlayerData playerData))
                        level.playerData = playerData;

                    collection.levels.Add(level);
                }
                else if (jn["levels"][i]["arcade_id"] != null && LevelManager.Levels.TryFind(x => x.id == jn["levels"][i]["arcade_id"], out Level arcadeLevel))
                {
                    arcadeLevel = new Level(arcadeLevel.path);
                    if (arcadeLevel.metadata)
                    {
                        arcadeLevel.metadata = MetaData.DeepCopy(arcadeLevel.metadata);
                        arcadeLevel.metadata.arcadeID = jn["levels"][i]["id"];
                        if (jn["levels"][i]["require_unlock"] != null)
                            arcadeLevel.metadata.requireUnlock = jn["levels"][i]["require_unlock"];
                    }
                    arcadeLevel.id = jn["levels"][i]["id"];
                    collection.levels.Add(arcadeLevel);
                }
                else if (jn["levels"][i]["workshop_id"] != null && SteamWorkshopManager.inst.Levels.TryFind(x => x.metadata.beatmap.beatmap_id == jn["levels"][i]["workshop_id"], out Level steamLevel))
                {
                    steamLevel = new Level(steamLevel.path);
                    if (steamLevel.metadata)
                    {
                        steamLevel.metadata = MetaData.DeepCopy(steamLevel.metadata);
                        steamLevel.metadata.arcadeID = jn["levels"][i]["id"];
                        if (jn["levels"][i]["require_unlock"] != null)
                            steamLevel.metadata.requireUnlock = jn["levels"][i]["require_unlock"];
                    }
                    steamLevel.id = jn["levels"][i]["id"];
                    collection.levels.Add(steamLevel);
                }
                else
                {
                    collection.levels.Add(null);
                    collection.nullLevels.Add(new NullLevel
                    {
                        index = i,
                        id = jn["levels"][i]["id"],
                        path = jn["levels"][i]["path"],
                        name = jn["levels"][i]["name"],
                        creator = jn["levels"][i]["creator"],
                        songTitle = jn["levels"][i]["song_title"],
                        arcadeID = jn["levels"][i]["arcade_id"],
                        serverID = jn["levels"][i]["server_id"],
                        workshopID = jn["levels"][i]["workshop_id"],
                    }); // somehow this needs to be used to notify the users that the level is null.
                }
            }

            collection.icon = RTFile.FileExists($"{path}icon.png") ? SpriteHelper.LoadSprite($"{path}icon.png") : SpriteHelper.LoadSprite($"{path}icon.jpg");
            collection.banner = RTFile.FileExists($"{path}banner.png") ? SpriteHelper.LoadSprite($"{path}banner.png") : SpriteHelper.LoadSprite($"{path}banner.jpg");

            return collection;
        }

        /// <summary>
        /// A list of levels that exist in the level collection file, but do not exist in neither the direct folder of the collection nor the level list.
        /// </summary>
        public List<NullLevel> nullLevels = new List<NullLevel>();

        public class NullLevel
        {
            public int index;
            public string id;
            public string path;
            public string songTitle;
            public string name;
            public string creator;
            public string arcadeID;
            public string serverID;
            public string workshopID;
        }

        public static void Test()
        {
            var collection = new LevelCollection();
            var path = $"{RTFile.ApplicationDirectory}beatmaps/arcade/Collection Test";

            if (!RTFile.DirectoryExists(path))
                Directory.CreateDirectory(path);

            collection.path = $"{path}/";

            collection.AddLevel(new Level($"{RTFile.ApplicationDirectory}beatmaps/arcade/2581783822/"));

            collection.Save();
        }

        public Level EntryLevel => EntryLevelIndex >= 0 ? this[EntryLevelIndex] : this[0];

        public int EntryLevelIndex => levels.FindIndex(x => x.metadata.isHubLevel && (!x.metadata.requireUnlock || x.playerData != null && x.playerData.Unlocked));

        public void Move(string id, int moveTo) => levels.Move(x => x.id == id, moveTo);

        public void Save()
        {
            var jn = JSON.Parse("{}");

            jn["id"] = id;
            jn["name"] = name;
            jn["creator"] = creator;
            jn["desc"] = name;
            if (tags != null)
            {
                for (int i = 0; i < tags.Length; i++)
                    jn["tags"][i] = tags[i];
            }

            for (int i = 0; i < Count; i++)
                jn["levels"][i] = System.IO.Path.GetFileName(System.IO.Path.GetDirectoryName(this[i].path));

            if (icon)
                SpriteHelper.SaveSprite(icon, $"{path}icon.png");
            if (banner)
                SpriteHelper.SaveSprite(banner, $"{path}banner.png");
            RTFile.WriteToFile($"{path}collection.lsco", jn.ToString(3));
        }

        public void AddLevel(Level level)
        {
            if (levels.Any(x => x.id == level.id)) // don't want to have duplicate levels
                return;

            var path = System.IO.Path.GetDirectoryName(level.path);
            var folderName = System.IO.Path.GetFileName(path);

            var files = Directory.GetFiles(level.path, "*", SearchOption.AllDirectories);
            for (int i = 0; i < files.Length; i++)
            {
                var file = files[i];
                var copyToPath = file.Replace("\\", "/").Replace(level.path, $"{this.path}{folderName}/");
                if (!RTFile.DirectoryExists(System.IO.Path.GetDirectoryName(copyToPath)))
                    Directory.CreateDirectory(System.IO.Path.GetDirectoryName(copyToPath));

                File.Copy(file, copyToPath, RTFile.FileExists(copyToPath));
            }

            var actualLevel = new Level($"{this.path}{folderName}/");

            levels.Add(actualLevel);
        }

        public void RemoveLevel(Level level)
        {
            if (!levels.Any(x => x.id == level.id))
                return;

            var actualLevel = levels.Find(x => x.id == level.id);

            Directory.Delete(System.IO.Path.GetDirectoryName(actualLevel.path), true);

            levels.RemoveAll(x => x.id == level.id);
        }
    }
}
