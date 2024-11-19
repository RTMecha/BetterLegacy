using BetterLegacy.Configs;
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
            if (CoreHelper.InStory)
                return;

            CoreHelper.Log($"Init {nameof(StoryMode)}");
            var path = $"{RTFile.ApplicationDirectory}{RTFile.BepInExAssetsPath}Story/{CoreConfig.Instance.StoryFile.Value}";
            if (RTFile.FileExists(path))
                Instance = Parse(JSON.Parse(RTFile.ReadFromFile(path)));
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
            public List<LevelSequence> levels = new List<LevelSequence>();

            /// <summary>
            /// Amount of levels in a chapter.
            /// </summary>
            public int Count => levels.Count;

            public LevelSequence this[int index]
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
                    chapter.levels.Add(LevelSequence.Parse(jn["levels"][i]));

                return chapter;
            }

            public override string ToString() => $"{name} - {Count}";
        }

        public class LevelSequence
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

            public string this[int index]
            {
                get
                {
                    if (index >= 0 && index < preCutscenes.Count)
                        return preCutscenes[index];
                    if (index > preCutscenes.Count && index - preCutscenes.Count - 1 < postCutscenes.Count)
                        return postCutscenes[index - preCutscenes.Count - 1];
                    return filePath;
                }
            }

            /// <summary>
            /// The amount of sub-levels (cutscenes + the level itself) in this level.
            /// </summary>
            public int Count => preCutscenes.Count + 1 + postCutscenes.Count;

            public List<string> preCutscenes = new List<string>();
            public List<string> postCutscenes = new List<string>();

            public string returnInterface;
            public bool returnReplayable;

            public static LevelSequence Parse(JSONNode jn)
            {
                var level = new LevelSequence
                {
                    id = jn["id"],
                    songTitle = jn["song_title"],
                    name = jn["name"],
                    filePath = RTFile.ParsePaths(jn["file"]),
                    bonus = jn["bonus"].AsBool,
                };

                if (jn["pre_cutscenes"] != null)
                    for (int i = 0; i < jn["pre_cutscenes"].Count; i++)
                        level.preCutscenes.Add(RTFile.ParsePaths(jn["pre_cutscenes"][i]));

                if (jn["post_cutscenes"] != null)
                    for (int i = 0; i < jn["post_cutscenes"].Count; i++)
                        level.postCutscenes.Add(RTFile.ParsePaths(jn["post_cutscenes"][i]));

                level.returnInterface = RTFile.ParsePaths(jn["return_interface"]);
                level.returnReplayable = jn["return_replayable"];

                return level;
            }

            public override string ToString() => $"{name} | {songTitle} - {Count}";
        }
    }
}
