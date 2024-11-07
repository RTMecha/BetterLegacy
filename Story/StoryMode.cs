using BetterLegacy.Core;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using SimpleJSON;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BetterLegacy.Story
{
    /// <summary>
    /// The main story data class.
    /// </summary>
    public class StoryMode
    {
        /// <summary>
        /// Where the story is located.
        /// </summary>
        public static StoryMode Instance { get; set; }

        /// <summary>
        /// Inits the Story Mode data.
        /// </summary>
        public static void Init()
        {
            CoreHelper.Log($"Init {nameof(StoryMode)}");
            Instance = Parse(JSON.Parse(RTFile.ReadFromFile($"{RTFile.ApplicationDirectory}{RTFile.BepInExAssetsPath}Story/story.json")));
        }

        /// <summary>
        /// Where the story begins.
        /// </summary>
        public string entryInterfacePath;

        /// <summary>
        /// All main story chapters.
        /// </summary>
        public List<Chapter> chapters = new List<Chapter>();

        /// <summary>
        /// Potential bonus chapters. (E.G. Melodical Escapism)
        /// </summary>
        public List<Chapter> bonusChapters = new List<Chapter>();

        public static StoryMode Parse(JSONNode jn)
        {
            var story = new StoryMode()
            {
                entryInterfacePath = RTFile.ParsePaths(jn["entry_interface"]),
            };

            for (int i = 0; i < jn["chapters"].Count; i++)
                story.chapters.Add(Chapter.Parse(jn["chapters"][i]));

            if (jn["bonus_chapters"] != null)
                for (int i = 0; i < jn["bonus_chapters"].Count; i++)
                    story.bonusChapters.Add(Chapter.Parse(jn["bonus_chapters"][i]));

            return story;
        }

        public class Chapter
        {
            /// <summary>
            /// Name of the chapter.
            /// </summary>
            public string name;

            /// <summary>
            /// Path to the interface that represents the chapter.
            /// </summary>
            public string interfacePath;

            /// <summary>
            /// All story levels within the chapter.
            /// </summary>
            public List<Level> levels = new List<Level>();

            /// <summary>
            /// Amount of levels in a chapter.
            /// </summary>
            public int Count => levels.Count;

            public Level this[int index]
            {
                get => levels[index];
                set => levels[index] = value;
            }

            public static Chapter Parse(JSONNode jn)
            {
                var chapter = new Chapter
                {
                    name = jn["name"],
                    interfacePath = RTFile.ParsePaths(jn["interface"])
                };

                for (int i = 0; i < jn["levels"].Count; i++)
                    chapter.levels.Add(Level.Parse(jn["levels"][i]));

                return chapter;
            }
        }

        public class Level
        {
            /// <summary>
            /// Identification number of the level.
            /// </summary>
            public string id;

            /// <summary>
            /// The title of the song the level uses.
            /// </summary>
            public string songTitle;

            /// <summary>
            /// Name of the level.
            /// </summary>
            public string name;

            /// <summary>
            /// File path to where the .asset or .lsb level is.
            /// </summary>
            public string filePath;

            /// <summary>
            /// If the level is a bonus level. Used for ChapterFullyRanked if function.
            /// </summary>
            public bool bonus;

            public static Level Parse(JSONNode jn) => new Level
            {
                id = jn["id"],
                songTitle = jn["song_title"],
                name = jn["name"],
                filePath = RTFile.ParsePaths(jn["file"]),
                bonus = jn["bonus"].AsBool,
            };
        }
    }
}
