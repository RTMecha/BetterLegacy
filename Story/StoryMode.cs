using System.Collections.Generic;
using System.Linq;

using SimpleJSON;

using BetterLegacy.Configs;
using BetterLegacy.Core;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Data.Network;
using BetterLegacy.Core.Helpers;

namespace BetterLegacy.Story
{
    /// <summary>
    /// The main story data class.
    /// </summary>
    public class StoryMode : PAObject<StoryMode>, IPacket
    {
        #region Values

        /// <summary>
        /// The current story mode instance.
        /// </summary>
        public static StoryMode Instance { get; set; }

        /// <summary>
        /// Where the story begins.
        /// </summary>
        public string entryInterfacePath;
        
        /// <summary>
        /// Where the story begins.
        /// </summary>
        public string entryInterfacePathNoParse;

        /// <summary>
        /// All main story chapters.
        /// </summary>
        public List<Chapter> chapters = new List<Chapter>();

        /// <summary>
        /// Potential bonus chapters. (E.G. Melodical Escapism)
        /// </summary>
        public List<Chapter> bonusChapters = new List<Chapter>();

        #endregion

        #region Functions

        /// <summary>
        /// Inits the Story Mode data.
        /// </summary>
        public static void Init()
        {
            CoreHelper.Log($"Init {nameof(StoryMode)}");
            var path = AssetPack.GetFile($"story/{CoreConfig.Instance.StoryFile.Value}");
            if (RTFile.FileExists(path))
                Instance = Parse(JSON.Parse(RTFile.ReadFromFile(path)));
        }

        public override void CopyData(StoryMode orig, bool newID = true)
        {
            entryInterfacePathNoParse = orig.entryInterfacePathNoParse;
            entryInterfacePath = orig.entryInterfacePath;
            chapters = new List<Chapter>(orig.chapters.Select(x => x.Copy(false)));
            bonusChapters = new List<Chapter>(orig.bonusChapters.Select(x => x.Copy(false)));
        }

        public override void ReadJSON(JSONNode jn)
        {
            entryInterfacePathNoParse = jn["entry_interface"];
            entryInterfacePath = RTFile.ParsePaths(entryInterfacePathNoParse);

            for (int i = 0; i < jn["chapters"].Count; i++)
                chapters.Add(Chapter.Parse(jn["chapters"][i]));

            if (jn["bonus_chapters"] != null)
                for (int i = 0; i < jn["bonus_chapters"].Count; i++)
                    bonusChapters.Add(Chapter.Parse(jn["bonus_chapters"][i]));
        }

        public override JSONNode ToJSON()
        {
            var jn = Parser.NewJSONObject();

            jn["entry_interface"] = entryInterfacePathNoParse;
            for (int i = 0; i < chapters.Count; i++)
                jn["chapters"][i] = chapters[i].ToJSON();
            for (int i = 0; i < bonusChapters.Count; i++)
                jn["bonus_chapters"][i] = bonusChapters[i].ToJSON();

            return jn;
        }

        public void ReadPacket(NetworkReader reader)
        {
            entryInterfacePathNoParse = reader.ReadString();
            entryInterfacePath = RTFile.ParsePaths(entryInterfacePath);
            Packet.ReadPacketList(chapters, reader);
            Packet.ReadPacketList(bonusChapters, reader);
        }

        public void WritePacket(NetworkWriter writer)
        {
            writer.Write(entryInterfacePathNoParse);
            Packet.WritePacketList(chapters, writer);
            Packet.WritePacketList(bonusChapters, writer);
        }

        #endregion

        #region Sub Classes

        /// <summary>
        /// Represents a chapter in the BetterLegacy story mode.
        /// </summary>
        public class Chapter : PAObject<Chapter>, IPacket
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
            /// Path to the interface that represents the chapter.
            /// </summary>
            public string interfacePathNoParse;

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

            #region Functions

            public override void CopyData(Chapter orig, bool newID = true)
            {
                name = orig.name;
                interfacePath = orig.interfacePath;
                interfacePathNoParse = orig.interfacePathNoParse;
                levels = new List<LevelSequence>(orig.levels.Select(x => x.Copy(false)));
                transition = orig.transition ? orig.transition.Copy(false) : null;
            }

            public override void ReadJSON(JSONNode jn)
            {
                name = jn["name"];
                interfacePathNoParse = jn["interface"];
                interfacePath = RTFile.ParsePaths(interfacePathNoParse);

                for (int i = 0; i < jn["levels"].Count; i++)
                    levels.Add(LevelSequence.Parse(jn["levels"][i]));

                if (jn["transition"] != null)
                    transition = ChapterTransition.Parse(jn["transition"]);
            }

            public override JSONNode ToJSON()
            {
                var jn = Parser.NewJSONObject();

                jn["name"] = name ?? string.Empty;
                jn["interface"] = interfacePathNoParse;
                for (int i = 0; i < levels.Count; i++)
                    jn["levels"][i] = levels[i].ToJSON();
                if (transition)
                    jn["transition"] = transition.ToJSON();

                return jn;
            }

            public void ReadPacket(NetworkReader reader)
            {
                name = reader.ReadString();
                interfacePathNoParse = reader.ReadString();
                interfacePath = RTFile.ParsePaths(interfacePathNoParse);
                Packet.ReadPacketList(levels, reader);
                var hasTransition = reader.ReadBoolean();
                if (hasTransition)
                    transition = Packet.CreateFromPacket<ChapterTransition>(reader);
            }

            public void WritePacket(NetworkWriter writer)
            {
                writer.Write(name);
                writer.Write(interfacePathNoParse);
                Packet.WritePacketList(levels, writer);
                bool hasTransition = transition;
                writer.Write(hasTransition);
                if (hasTransition)
                    transition.WritePacket(writer);
            }

            /// <summary>
            /// Gets a story level at an index.
            /// </summary>
            /// <param name="level">Index of the story level.</param>
            /// <returns>Returns the level sequence that represents the story level.</returns>
            public LevelSequence GetLevel(int level) => level < Count ? levels[level] : transition.levelSequence;

            public override string ToString() => $"{name} - {Count}";

            #endregion
        }

        /// <summary>
        /// Represents a level with cutscenes in the BetterLegacy story mode.
        /// </summary>
        public class LevelSequence : PAObject<LevelSequence>, IPacket
        {
            #region Values

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
            public LevelPath filePath;

            /// <summary>
            /// If the level is a bonus level. Used for ChapterFullyRanked if function.
            /// </summary>
            public bool bonus;

            public LevelPath this[int index]
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
            public List<LevelPath> preCutscenes = new List<LevelPath>();

            /// <summary>
            /// Cutscenes to play after the level is completed.
            /// </summary>
            public List<LevelPath> postCutscenes = new List<LevelPath>();

            /// <summary>
            /// Interface to return to when the level is completed.
            /// </summary>
            public string returnInterface;

            /// <summary>
            /// Interface to return to when the level is completed.
            /// </summary>
            public string returnInterfaceNoParse;

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

            #region Functions

            public override void CopyData(LevelSequence orig, bool newID = true)
            {
                id = newID ? GetNumberID() : orig.id;
                songTitle = orig.songTitle;
                name = orig.name;
                filePath = orig.filePath.Copy(false);
                bonus = orig.bonus;
                preCutscenes = new List<LevelPath>(orig.preCutscenes.Select(x => x.Copy(false)));
                postCutscenes = new List<LevelPath>(orig.postCutscenes.Select(x => x.Copy(false)));
                returnInterfaceNoParse = orig.returnInterfaceNoParse;
                returnInterface = orig.returnInterface;
                returnReplayable = orig.returnReplayable;
                returnLevel = orig.returnLevel;
                isChapterTransition = orig.isChapterTransition;
            }

            public override void ReadJSON(JSONNode jn)
            {
                id = jn["id"];
                songTitle = jn["song_title"];
                name = jn["name"];
                filePath = LevelPath.Parse(jn["file"]);
                bonus = jn["bonus"].AsBool;

                if (jn["pre_cutscenes"] != null)
                    for (int i = 0; i < jn["pre_cutscenes"].Count; i++)
                        preCutscenes.Add(LevelPath.Parse(jn["pre_cutscenes"][i]));

                if (jn["post_cutscenes"] != null)
                    for (int i = 0; i < jn["post_cutscenes"].Count; i++)
                        postCutscenes.Add(LevelPath.Parse(jn["post_cutscenes"][i]));

                returnInterfaceNoParse = jn["return_interface"];
                returnInterface = RTFile.ParsePaths(returnInterfaceNoParse);
                returnReplayable = jn["return_replayable"].AsBool;
                returnLevel = jn["return_level"];
            }

            public override JSONNode ToJSON()
            {
                var jn = Parser.NewJSONObject();

                jn["id"] = id;
                jn["song_title"] = songTitle ?? string.Empty;
                jn["name"] = name ?? string.Empty;
                jn["file"] = filePath.ToJSON();
                jn["bonus"] = bonus;

                for (int i = 0; i < preCutscenes.Count; i++)
                    jn["pre_cutscenes"][i] = preCutscenes[i].ToJSON();
                for (int i = 0; i < postCutscenes.Count; i++)
                    jn["post_cutscenes"][i] = postCutscenes[i].ToJSON();

                jn["return_interface"] = returnInterfaceNoParse ?? string.Empty;
                jn["return_replayable"] = returnReplayable;
                jn["return_level"] = returnLevel ?? string.Empty;

                return jn;
            }

            public void ReadPacket(NetworkReader reader)
            {
                id = reader.ReadString();
                songTitle = reader.ReadString();
                name = reader.ReadString();
                filePath = Packet.CreateFromPacket<LevelPath>(reader);
                bonus = reader.ReadBoolean();

                Packet.ReadPacketList(preCutscenes, reader);
                Packet.ReadPacketList(postCutscenes, reader);

                returnInterfaceNoParse = reader.ReadString();
                returnInterface = RTFile.ParsePaths(returnInterfaceNoParse);
                returnReplayable = reader.ReadBoolean();
                returnLevel = reader.ReadString();
            }

            public void WritePacket(NetworkWriter writer)
            {
                writer.Write(id);
                writer.Write(songTitle);
                writer.Write(name);
                filePath.WritePacket(writer);
                writer.Write(bonus);

                Packet.WritePacketList(preCutscenes, writer);
                Packet.WritePacketList(postCutscenes, writer);

                writer.Write(returnInterfaceNoParse);
                writer.Write(returnReplayable);
                writer.Write(returnLevel);
            }

            /// <summary>
            /// Gets the levels based on their destination in a level sequence.
            /// </summary>
            /// <param name="cutsceneDestination">Cutscene destination.</param>
            /// <returns>Returns a list of levels (cutscene or otherwise).</returns>
            public List<LevelPath> GetPaths(CutsceneDestination cutsceneDestination) => cutsceneDestination switch
            {
                CutsceneDestination.Pre => preCutscenes,
                CutsceneDestination.Post => postCutscenes,
                _ => new List<LevelPath>() { filePath }
            };

            public override string ToString() => $"{name} | {songTitle} - {Count}";

            #endregion
        }

        /// <summary>
        /// Represents a path to a story level.
        /// </summary>
        public class LevelPath : PAObject<LevelPath>, IPacket
        {
            #region Constructors

            public LevelPath() { }

            public LevelPath(string filePath) => this.filePath = filePath;

            public LevelPath(string filePath, string songName) : this(filePath) => this.songName = songName;
            public LevelPath(string filePath, string editorFilePath, string songName) : this(filePath, songName) => this.editorFilePath = editorFilePath;

            #endregion

            #region Values

            /// <summary>
            /// Path to the level file.
            /// </summary>
            public string filePath;

            /// <summary>
            /// Path to the level file.
            /// </summary>
            public string filePathNoParse;

            /// <summary>
            /// Path to the level file in the editor. Good for quickly editing the level.
            /// </summary>
            public string editorFilePath;

            /// <summary>
            /// Path to the level file in the editor. Good for quickly editing the level.
            /// </summary>
            public string editorFilePathNoParse;

            /// <summary>
            /// Song to override to save on space.
            /// </summary>
            public string songName;

            #endregion

            #region Functions

            public override void CopyData(LevelPath orig, bool newID = true)
            {
                filePathNoParse = orig.filePathNoParse;
                filePath = orig.filePath;
                editorFilePathNoParse = orig.editorFilePathNoParse;
                editorFilePath = orig.editorFilePath;
                songName = orig.songName;
            }

            public override void ReadJSON(JSONNode jn)
            {
                if (jn.IsString)
                {
                    filePathNoParse = jn;
                    filePath = RTFile.ParsePaths(filePathNoParse);
                    return;
                }

                filePathNoParse = jn["path"];
                filePath = RTFile.ParsePaths(filePathNoParse);
                editorFilePathNoParse = jn["editor_path"];
                editorFilePath = RTFile.ParsePaths(editorFilePathNoParse);
                songName = jn["song"];
            }

            public override JSONNode ToJSON()
            {
                if (string.IsNullOrEmpty(editorFilePath) && string.IsNullOrEmpty(songName))
                    return filePath ?? string.Empty;

                var jn = Parser.NewJSONObject();

                if (!string.IsNullOrEmpty(filePathNoParse))
                    jn["path"] = filePathNoParse;
                if (!string.IsNullOrEmpty(editorFilePathNoParse))
                    jn["editor_path"] = editorFilePathNoParse;
                if (!string.IsNullOrEmpty(songName))
                    jn["song"] = songName;

                return jn;
            }

            public void ReadPacket(NetworkReader reader)
            {
                filePathNoParse = reader.ReadString();
                filePath = RTFile.ParsePaths(filePathNoParse);
                editorFilePathNoParse = reader.ReadString();
                editorFilePath = RTFile.ParsePaths(editorFilePathNoParse);
                songName = reader.ReadString();
            }

            public void WritePacket(NetworkWriter writer)
            {
                writer.Write(filePathNoParse);
                writer.Write(editorFilePathNoParse);
                writer.Write(songName);
            }

            public override string ToString() => System.IO.Path.GetFileName(filePath);

            #endregion

            #region Operators

            public static implicit operator string(LevelPath levelPath) => CoreConfig.Instance.StoryEditorMode.Value && RTFile.FileExists(levelPath.editorFilePath) ? levelPath.editorFilePath : levelPath.filePath;

            #endregion
        }

        /// <summary>
        /// Represents the transition from one chapter to the next.
        /// </summary>
        public class ChapterTransition : PAObject<ChapterTransition>, IPacket
        {
            #region Values

            /// <summary>
            /// Path to the interface to load when moving onto the next chapter. If left empty, load the level sequence.
            /// </summary>
            public string interfacePath;

            /// <summary>
            /// Level transition between chapters. If left null, move onto the next chapter anyways.
            /// </summary>
            public LevelSequence levelSequence;

            #endregion

            #region Functions

            public override void CopyData(ChapterTransition orig, bool newID = true)
            {
                interfacePath = orig.interfacePath;
                levelSequence = orig.levelSequence.Copy(false);
            }

            public override void ReadJSON(JSONNode jn)
            {
                if (!string.IsNullOrEmpty(jn["interface"]))
                    interfacePath = jn["interface"];
                if (jn["level"] != null)
                {
                    levelSequence = LevelSequence.Parse(jn["level"]);
                    levelSequence.isChapterTransition = true;
                }
            }

            public override JSONNode ToJSON()
            {
                var jn = Parser.NewJSONObject();

                if (!string.IsNullOrEmpty(interfacePath))
                    jn["interface"] = interfacePath;
                if (levelSequence)
                    jn["level"] = levelSequence.ToJSON();

                return jn;
            }

            public void ReadPacket(NetworkReader reader)
            {
                interfacePath = reader.ReadString();
                var hasLevelSequence = reader.ReadBoolean();
                if (hasLevelSequence)
                    levelSequence = Packet.CreateFromPacket<LevelSequence>(reader);
            }

            public void WritePacket(NetworkWriter writer)
            {
                writer.Write(interfacePath);
                bool hasLevelSequence = levelSequence;
                writer.Write(hasLevelSequence);
                if (hasLevelSequence)
                    levelSequence.WritePacket(writer);
            }

            public override string ToString() => levelSequence?.ToString();

            #endregion
        }

        #endregion
    }
}
