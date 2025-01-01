using BetterLegacy.Configs;
using BetterLegacy.Core;
using BetterLegacy.Core.Helpers;
using SimpleJSON;
using System.Collections.Generic;

namespace BetterLegacy.Story
{
    /// <summary>
    /// The main story data class.
    /// </summary>
    public class StoryMode
    {
        #region Init

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

        #endregion

        #region Values

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

        #endregion

        /// <summary>
        /// Parses the Story Mode from JSON.
        /// </summary>
        /// <param name="jn">JSON to parse.</param>
        /// <returns>Returns a parsed <see cref="StoryMode"/>.</returns>
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

        /// <summary>
        /// Represents a chapter in the BetterLegacy story mode.
        /// </summary>
        public class Chapter
        {
            #region Values

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

            public LevelSequence this[int index]
            {
                get => levels[index];
                set => levels[index] = value;
            }

            /// <summary>
            /// Amount of levels in a chapter.
            /// </summary>
            public int Count => levels.Count;

            /// <summary>
            /// The transition sequence to the next chapter.
            /// </summary>
            public ChapterTransition transition;

            #endregion

            /// <summary>
            /// Parses a <see cref="Chapter"/> from JSON.
            /// </summary>
            /// <param name="jn">JSON to parse.</param>
            /// <returns>Returns a parsed <see cref="Chapter"/> for the story mode.</returns>
            public static Chapter Parse(JSONNode jn)
            {
                var chapter = new Chapter
                {
                    name = jn["name"],
                    interfacePath = RTFile.ParsePaths(jn["interface"])
                };

                for (int i = 0; i < jn["levels"].Count; i++)
                    chapter.levels.Add(LevelSequence.Parse(jn["levels"][i]));

                if (jn["transition"] != null)
                    chapter.transition = ChapterTransition.Parse(jn["transition"]);

                return chapter;
            }

            public override string ToString() => $"{name} - {Count}";
        }

        /// <summary>
        /// Represents a level with cutscenes in the BetterLegacy story mode.
        /// </summary>
        public class LevelSequence
        {
            #region Values

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

            /// <summary>
            /// Cutscenes to play before the level starts.
            /// </summary>
            public List<string> preCutscenes = new List<string>();
            /// <summary>
            /// Cutscenes to play after the level is completed.
            /// </summary>
            public List<string> postCutscenes = new List<string>();

            /// <summary>
            /// Interface to return to when the level is completed.
            /// </summary>
            public string returnInterface;
            /// <summary>
            /// If the return interface is replayable after completion.
            /// </summary>
            public bool returnReplayable;
            /// <summary>
            /// Level to return to when the level is completed. Currently only used for custom stories.
            /// </summary>
            public string returnLevel;

            /// <summary>
            /// If the level is from the <see cref="ChapterTransition"/>.
            /// </summary>
            public bool isChapterTransition;

            #endregion

            /// <summary>
            /// Parses a <see cref="LevelSequence"/> from JSON.
            /// </summary>
            /// <param name="jn">JSON to parse.</param>
            /// <returns>Returns a parsed <see cref="LevelSequence"/> for the story mode.</returns>
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
                level.returnReplayable = jn["return_replayable"].AsBool;
                level.returnLevel = jn["return_level"];

                return level;
            }

            public override string ToString() => $"{name} | {songTitle} - {Count}";
        }

        /// <summary>
        /// Represents the transition from one chapter to the next.
        /// </summary>
        public class ChapterTransition
        {
            /// <summary>
            /// Path to the interface to load when moving onto the next chapter. If left empty, load the level sequence.
            /// </summary>
            public string interfacePath;

            /// <summary>
            /// Level transition between chapters. If left null, move onto the next chapter anyways.
            /// </summary>
            public LevelSequence levelSequence;

            /// <summary>
            /// Parses a Chapters' transition from JSON.
            /// </summary>
            /// <param name="jn">JSON to parse.</param>
            /// <returns>Returns a parsed <see cref="ChapterTransition"/> for the story mode.</returns>
            public static ChapterTransition Parse(JSONNode jn)
            {
                var transition = new ChapterTransition();
                if (!string.IsNullOrEmpty(jn["interface"]))
                    transition.interfacePath = jn["interface"];
                if (jn["level"] != null)
                {
                    transition.levelSequence = LevelSequence.Parse(jn["level"]);
                    transition.levelSequence.isChapterTransition = true;
                }
                return transition;
            }

            public override string ToString() => levelSequence?.ToString();
        }
    }
}
