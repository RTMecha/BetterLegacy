using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BetterLegacy.Core.Managers;
using LSFunctions;
using SimpleJSON;
using UnityEngine;

namespace BetterLegacy.Core
{
    public class LevelCollection : Exists
    {
        public LevelCollection()
        {
            ID = LSText.randomNumString(16);
        }

        /// <summary>
        /// Identification number of the collection.
        /// </summary>
        public string ID { get; set; }

        /// <summary>
        /// Name of the collection.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Full path of the collection. Must end with a "/".
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// Icon of the collection.
        /// </summary>
        public Sprite Icon { get; set; }

        /// <summary>
        /// Banner of the collection. To be used for full level screen.
        /// </summary>
        public Sprite Banner { get; set; }

        /// <summary>
        /// All levels the collection contains.
        /// </summary>
        public List<Level> levels = new List<Level>();

        public static LevelCollection Parse(string path, JSONNode jn)
        {
            var collection = new LevelCollection();
            collection.ID = jn["id"];
            collection.Name = jn["name"];

            for (int i = 0; i < jn["levels"].Count; i++)
            {
                var levelFolder = $"{path}{jn["levels"][i]}/";

                if (!RTFile.FileExists($"{levelFolder}level.lsb") && !RTFile.FileExists($"{levelFolder}level.vgd"))
                    continue;

                collection.levels.Add(new Level(levelFolder));
            }

            collection.Icon = RTFile.FileExists($"{path}icon.png") ? SpriteManager.LoadSprite($"{path}icon.png") : SpriteManager.LoadSprite($"{path}icon.jpg");
            collection.Banner = RTFile.FileExists($"{path}banner.png") ? SpriteManager.LoadSprite($"{path}banner.png") : SpriteManager.LoadSprite($"{path}banner.jpg");

            return collection;
        }

        public static void Test()
        {
            var collection = new LevelCollection();
            var path = $"{RTFile.ApplicationDirectory}beatmaps/arcade/Collection Test";

            if (!RTFile.DirectoryExists(path))
                Directory.CreateDirectory(path);

            collection.Path = $"{path}/";

            collection.AddLevel(new Level($"{RTFile.ApplicationDirectory}beatmaps/arcade/2581783822/"));

            collection.Save();
        }

        public Level EntryLevel
        {
            get
            {
                if (levels.TryFind(x => x.metadata.isHubLevel && (!x.metadata.requireUnlock || x.playerData != null && x.playerData.Unlocked), out Level level))
                    return level;

                return levels[0];
            }
        }

        public void Move(string id, int moveTo)
        {
            var levelIndex = levels.FindIndex(x => x.id == id);

            if (levelIndex < 0)
                return;

            var level = levels[levelIndex];
            levels.RemoveAt(levelIndex);
            levels.Insert(Mathf.Clamp(moveTo, 0, levels.Count), level);
        }

        public void Save()
        {
            var jn = JSON.Parse("{}");

            jn["id"] = ID;
            jn["name"] = Name;

            for (int i = 0; i < levels.Count; i++)
                jn["levels"][i] = System.IO.Path.GetFileName(System.IO.Path.GetDirectoryName(levels[i].path));

            if (Icon)
                SpriteManager.SaveSprite(Icon, $"{Path}icon.png");
            if (Banner)
                SpriteManager.SaveSprite(Banner, $"{Path}banner.png");
            RTFile.WriteToFile($"{Path}collection.lsco", jn.ToString(3));
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
                var copyToPath = file.Replace("\\", "/").Replace(level.path, $"{Path}{folderName}/");
                if (!RTFile.DirectoryExists(System.IO.Path.GetDirectoryName(copyToPath)))
                    Directory.CreateDirectory(System.IO.Path.GetDirectoryName(copyToPath));

                File.Copy(file, copyToPath, RTFile.FileExists(copyToPath));
            }

            var actuaLevel = new Level($"{Path}{folderName}/");
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
