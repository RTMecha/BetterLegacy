﻿using BetterLegacy.Core.Managers;
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
            collection.path = path;

            for (int i = 0; i < jn["levels"].Count; i++)
            {
                var levelFolder = $"{path}{jn["levels"][i].Value}/";

                if (RTFile.FileExists($"{levelFolder}level.lsb") || RTFile.FileExists($"{levelFolder}level.vgd"))
                    collection.levels.Add(new Level(levelFolder) { fromCollection = true });
            }

            collection.icon = RTFile.FileExists($"{path}icon.png") ? SpriteHelper.LoadSprite($"{path}icon.png") : SpriteHelper.LoadSprite($"{path}icon.jpg");
            collection.banner = RTFile.FileExists($"{path}banner.png") ? SpriteHelper.LoadSprite($"{path}banner.png") : SpriteHelper.LoadSprite($"{path}banner.jpg");

            return collection;
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
