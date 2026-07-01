using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

using UnityEngine;

using LSFunctions;

using SimpleJSON;

using BetterLegacy.Companion.Entity;
using BetterLegacy.Configs;
using BetterLegacy.Core;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Data.Level;
using BetterLegacy.Core.Data.Modifiers;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using BetterLegacy.Core.Runtime;
using BetterLegacy.Editor.Data;
using BetterLegacy.Editor.Data.Dialogs;
using BetterLegacy.Editor.Data.Elements;
using BetterLegacy.Editor.Data.Planners;
using BetterLegacy.Editor.Data.Timeline;
using BetterLegacy.Editor.Managers;
using BetterLegacy.Menus;
using BetterLegacy.Story;

namespace BetterLegacy.Companion.Data
{
    /// <summary>
    /// Represents a way of communicating with Example.
    /// </summary>
    public abstract class ExampleCommand : Exists
    {
        /*
         examples include:
        - create marker 10 add_time current_time 0.1 name "test"
        - create background_object 10 start_time current_time add_editor_bin 0
        - hello buddy
        - i love you
        - evaluate 1 + 1
        - select objects layer current_layer time lesser_equals 1 name "Object Name"
        - select external_prefabs name_regex "RT (.*?)Mecha(.*?)" -> log import
        - select levels name "commands" union name \"Shockwave\" -> log notify_count combine "TEST COMBINE COMMAND"
        - select objects name "NAME" select modifiers -> log notify_count edit prefab_group_only true
        - select objects name "NAME" -> mirror horizontal
        - select objects name "keyframer" select object_keyframes event_coord 0 0 -> log edit value 0 set 10 select objects name "keyframer" -> update keyframes
        - select markers select annotations color equals 0
        - select markers selected -> draw_box 0 0 10 10 0 0 - 1 4 false
        - select markers selected -> draw_shape 32 0 0 10 10 0 0 - 1 4 false
        - create beatmap_object 24 start_time current_time add_editor_bin 0 1 pos_kf [time set 5 value 0 set evaluate (cos((index*15)/radToDeg)*50) value 1 set evaluate (sin((index*15)/radToDeg)*50) value 2 set 0 relative false] rot_kf [time set 0 value 0 set evaluate ((index*15)) relative false]
        - create prefab_object 8 "Mouth Controller" start_time current_time add_editor_bin 0 1 pos evaluate (cos((index*45)/radToDeg)*20) evaluate (sin((index*45)/radToDeg)*20) rot evaluate((index*45))
        - select objects selected edit beatmap_object parent [select objects name "TESTPARENT"]
        - select objects selected -> set_name value_list [0 test1 test2]
        - load_level story chapter 0 level 0
        - load_level arcade_id 523682385
         */

        #region Values

        /// <summary>
        /// Name of the command.
        /// </summary>
        public abstract string Name { get; }

        /// <summary>
        /// If the command is currently usable.
        /// </summary>
        public virtual bool Usable => true;

        /// <summary>
        /// If the command should display in the commands autocomplete.
        /// </summary>
        public virtual bool Autocomplete => true;

        /// <summary>
        /// Command line pattern hint.
        /// </summary>
        public virtual string Pattern => Name;

        /// <summary>
        /// Description of the command.
        /// </summary>
        public abstract string Description { get; }

        /// <summary>
        /// Text to add to the autocomplete when selected.
        /// </summary>
        public virtual string AddToAutocomplete => Pattern;

        /// <summary>
        /// List of global variables.
        /// </summary>
        public static List<VariableParameter> variables = new List<VariableParameter>
        {
            new EvaluateParameter(),
            new ValueListParameter(),
        };

        /// <summary>
        /// List of registered commands.
        /// </summary>
        public static List<ExampleCommand> commands = new List<ExampleCommand>
        {
            #region Example Interaction Commands
            
            new HelloCommand(),
            new HeyCommand(),
            new HiCommand(),
            new MyselfCommand(),
            new EvaluateCommand(),
            new DanceCommand(),

            #endregion

            #region Core Commands
            
            new SetSceneCommand(),
            new PlayerCommand(),
            new LoadLevelCommand(),
            new HideInterfaceCommand(),
            new ShowPlayerGUICommand(),
            new HideTimelineCommand(),

            #endregion

            #region Editor Commands

            new CreateCommand(),
            new SelectCommand(),

            new SetRuntimeVariable(),
            new RemoveRuntimeVariable(),
            new ClearRuntimeVariables(),

            #endregion

            #region Misc
            
            new ReadFileCommand(),

            new SaveCustomCommand(),
            new RemoveCustomCommand(),
            new ClearCustomCommands(),
            new EditCustomCommand(),

            new CustomCommand(
                name: "prefab_group_only_on",
                description: "Enables the prefab group only toggle for all modifiers in all selected objects.",
                commandLine: "select objects selected select modifiers edit prefab_group_only true"),
            new CustomCommand(
                name: "prefab_group_only_off",
                description: "Disables the prefab group only toggle for all modifiers in all selected objects.",
                commandLine: "select objects selected select modifiers edit prefab_group_only false"),

            #endregion
        };

        #endregion

        #region Functions

        /// <summary>
        /// Gets a collection of currently available parameters.
        /// </summary>
        /// <returns>Returns a collection of currently available parameters.</returns>
        public virtual IEnumerable<ParameterBase> GetParameters() => null;

        /// <summary>
        /// Parses multiple lines into commands and runs the commands.
        /// </summary>
        /// <param name="inputs">Inputs to parse.</param>
        public static void Run(params string[] inputs)
        {
            foreach (var line in inputs)
                Run(line);
        }

        /// <summary>
        /// Parses an input into a command and runs the command.
        /// </summary>
        /// <param name="input">Input to parse.</param>
        public static void Run(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                CoreHelper.LogError("Please provide a proper input!");
                return;
            }

            var split = input.Split(' ');
            var name = split[0];
            if (commands.TryFind(x => x.Name == name, out ExampleCommand command))
            {
                if (command.Usable)
                {
                    command.ConsumeInput(input, split);
                    command.Response(input);
                }
                else
                    CoreHelper.LogError($"Could not run command {name} since it is not currently usable.");
            }
        }

        /// <summary>
        /// Runs the action based on the input.
        /// </summary>
        /// <param name="input">Input for the command.</param>
        /// <param name="split">Array of split words.</param>
        public abstract void ConsumeInput(string input, string[] split);

        /// <summary>
        /// How Example should respond to the command.
        /// </summary>
        /// <param name="input">Input of the command.</param>
        public virtual void Response(string input) => Example.Current?.chatBubble?.Say($"Ran {Name} function!");

        /// <summary>
        /// Gets a quote parameter and iterates an index.
        /// </summary>
        /// <param name="split">Split string parameters.</param>
        /// <param name="index">Reference index.</param>
        /// <returns>Returns the parameter.</returns>
        public static string GetQuoteParameter(string[] split, ref int index)
        {
            var result = split[index];
            if (result.StartsWith("\""))
            {
                result = result.TrimStart('"');
                index++;
                if (!result.EndsWith("\""))
                    while (index < split.Length)
                    {
                        result += " " + split[index];
                        if (split[index].EndsWith("\""))
                            break;
                        index++;
                    }
                result = result.TrimEnd('"');
            }
            else
                index++;
            return result;
        }

        #endregion

        #region Sub Classes

        #region Example Interaction Commands

        /// <summary>
        /// Represents a command for saying hello.
        /// </summary>
        public class HelloCommand : ExampleCommand
        {
            public override string Name => "hello";

            public override bool Autocomplete => false;

            public override string Description => "Say hello to Example!";

            public override void ConsumeInput(string input, string[] split)
            {
                if (split.Length > 1)
                {
                    var next = split[1];
                    switch (next.ToLower())
                    {
                        case "buddy": {
                                Example.Current?.brain?.SetAttribute("HAPPINESS", 4.0, MathOperation.Addition);
                                break;
                            }
                        case "friend": {
                                Example.Current?.brain?.SetAttribute("HAPPINESS", 5.0, MathOperation.Addition);
                                break;
                            }
                    }
                }
            }

            public override void Response(string input) => Example.Current?.chatBubble?.SayDialogue(ExampleChatBubble.Dialogues.GREETING);
        }
        
        /// <summary>
        /// Represents a command for saying hey.
        /// </summary>
        public class HeyCommand : HelloCommand
        {
            public override string Name => "hey";
        }
        
        /// <summary>
        /// Represents a command for saying hi.
        /// </summary>
        public class HiCommand : HelloCommand
        {
            public override string Name => "hi";
        }

        /// <summary>
        /// Represents a command for saying something about yourself.
        /// </summary>
        public class MyselfCommand : ExampleCommand
        {
            public override string Name => "i";

            public override bool Autocomplete => false;

            public override string Description => "Say something about yourself";

            public List<Phrase> modifiers = new List<Phrase>
            {
                new Phrase("love", 10.0),
                new Phrase("like", 5.0),
                new Phrase("dislike", -5.0),
                new Phrase("hate", -10.0),
            };

            public List<Phrase> contexts = new List<Phrase>
            {
                new Phrase("you", 3.0),
                new Phrase("myself", 2.0),
            };

            public override void ConsumeInput(string input, string[] split)
            {
                if (split.TryGetAt(1, out string modifierName) && modifiers.TryFind(x => x.name == modifierName, out Phrase modifierPhrase) &&
                    split.TryGetAt(2, out string contextName) && contexts.TryFind(x => x.name == contextName, out Phrase contextPhrase))
                    Example.Current?.brain?.SetAttribute("HAPPINESS", modifierPhrase.score * contextPhrase.score, MathOperation.Addition);
            }

            public override void Response(string input) { }

            public class Phrase
            {
                public Phrase(string name, double score)
                {
                    this.name = name;
                    this.score = score;
                }

                public string name;
                public double score;
            }
        }

        /// <summary>
        /// Represents a command for evaluating math.
        /// </summary>
        public class EvaluateCommand : ExampleCommand
        {
            public override string Name => "evaluate";

            public override string Pattern => "evaluate 1 + 1";

            public override string Description => "Evaluate a math function";

            public override void ConsumeInput(string input, string[] split)
            {
                var response = string.Empty;
                for (int i = 1; i < split.Length; i++)
                {
                    response += split[i];
                    if (i != split.Length - 1)
                        response += " ";
                }

                try
                {
                    CompanionManager.Log($"Response: {response}");
                    var r = RTMath.Parse(response).ToString();
                    Example.Current?.chatBubble?.Say(r);
                    LSText.CopyToClipboard(r);
                }
                catch
                {
                    Example.Current?.chatBubble?.Say("Couldn't calculate that, sorry...");
                }
            }

            public override void Response(string input) { }
        }

        /// <summary>
        /// Represents a command for dancing.
        /// </summary>
        public class DanceCommand : ExampleCommand
        {
            public override string Name => "dance";

            public override bool Autocomplete => false;

            public override string Description => "Dance!";

            public override void ConsumeInput(string input, string[] split) => Example.Current?.brain?.ForceRunAction(Example.Current?.brain?.GetAction(ExampleBrain.Actions.DANCING));

            public override void Response(string input) { }
        }

        #endregion

        #region Core Commands

        /// <summary>
        /// Represents a command capable of setting the current Unity scene.
        /// </summary>
        public class SetSceneCommand : ExampleCommand
        {
            #region Values

            public override string Name => "setscene";

            public override string Pattern => "setscene Scene_Name";

            public override string Description => "Loads a Unity scene.";

            #endregion

            #region Functions

            public override void ConsumeInput(string input, string[] split) => SceneHelper.LoadScene(Parser.TryParse(split[1], true, SceneName.Main_Menu));

            public override IEnumerable<ParameterBase> GetParameters()
            {
                var values = EnumHelper.GetValues<SceneName>();
                foreach (var value in values)
                    yield return new TypeEnumParameter<SceneName>(value, value switch
                    {
                        SceneName.Main_Menu => "Main menu interface scene.",
                        SceneName.Editor => "Editor scene.",
                        SceneName.Arcade_Select => "Arcade menu scene.",
                        SceneName.Game => "Playing a level scene.",
                        SceneName.Input_Select => "Input select interface scene.",
                        SceneName.Interface => "Story interface scene.",
                        SceneName.post_level => "Unused story scene.",
                        _ => "Scene to load.",
                    });
            }

            #endregion
        }

        /// <summary>
        /// Represents a command capable of controlling players.
        /// </summary>
        public class PlayerCommand : ExampleCommand
        {
            #region Values

            public override string Name => "player";

            public override bool Usable => ProjectArrhythmia.State.InEditor;

            public override string Pattern => "player action";

            public override string Description => "Makes a player do something. Only available in the editor.";

            public static List<PlayerActionParameter> playerActionParameters = new List<PlayerActionParameter>
            {
                new MoveParameter(),
                new BoostParameter(),
            };

            #endregion

            #region Functions

            public override void ConsumeInput(string input, string[] split)
            {
                for (int i = 1; i < split.Length; i++)
                {
                    var s = split[i];
                    if (playerActionParameters.TryFind(x => x.Name == s, out PlayerActionParameter parameter))
                        parameter.Run(parameter.GetParameters(split, ref i));
                }
            }

            public override IEnumerable<ParameterBase> GetParameters() => playerActionParameters;

            #endregion

            #region Sub Classes

            public abstract class PlayerActionParameter : ParameterBase
            {
                public abstract void Run(string[] parameters);
            }

            public class MoveParameter : PlayerActionParameter
            {
                public override string Name => "move";

                public override int ParameterCount => 2;

                public override string Description => "Moves all players to an area.";

                public override string AddToAutocomplete => "move 0 0";

                public override void Run(string[] parameters)
                {
                    var pos = new Vector2(Parser.TryParse(parameters[0], 0f), Parser.TryParse(parameters[1], 0f));
                    foreach (var player in PlayerManager.Players)
                    {
                        if (player.RuntimePlayer && player.RuntimePlayer.rb)
                            player.RuntimePlayer.rb.position = pos;
                    }
                }
            }

            public class BoostParameter : PlayerActionParameter
            {
                public override string Name => "boost";

                public override string Description => "Forces all players to boost.";

                public override void Run(string[] parameters)
                {
                    foreach (var player in PlayerManager.Players)
                        player.RuntimePlayer?.Boost();
                }
            }

            #endregion
        }

        /// <summary>
        /// Represents a command capable of loading a level.
        /// </summary>
        public class LoadLevelCommand : ExampleCommand
        {
            #region Values

            public override string Name => "load_level";

            public override string Pattern => "load_level type [path or selection]";

            public override string Description => "Loads a level from the arcade, editor, path or story.";

            public static SelectType CurrentType { get; set; }

            public List<StorySelectParameter> storySelectParameters = new List<StorySelectParameter>
            {
                new ChapterSelectParameter(),
                new LevelSelectParameter(),
                new CutsceneSelectParameter(),
                new BonusSelectParameter(),
                new SkipCutscenesParameter(),
            };

            #endregion

            #region Functions

            public override void ConsumeInput(string input, string[] split)
            {
                CurrentType = Parser.TryParse(split[1].Remove("_"), true, SelectType.RelativePath);
                switch (CurrentType)
                {
                    case SelectType.RelativePath: {
                            int index = 2;
                            LoadLevel(new Level(RTFile.CombinePaths(RTFile.ApplicationDirectory, GetQuoteParameter(split, ref index))));
                            break;
                        }
                    case SelectType.FullPath: {
                            int index = 2;
                            LoadLevel(new Level(GetQuoteParameter(split, ref index)));
                            break;
                        }
                    case SelectType.ArcadeID: {
                            if (ProjectArrhythmia.State.InEditor)
                            {
                                if (EditorLevelManager.inst.LevelPanels.TryFind(x => x && x.Item && x.Item.id == split[2], out LevelPanel levelPanel))
                                    LoadLevel(levelPanel.Item);
                            }
                            else
                            {
                                if (LevelManager.Levels.TryFind(x => x && x.id == split[2], out Level level))
                                    LoadLevel(level);
                            }
                            break;
                        }
                    case SelectType.Story: {
                            var storySelection = new StorySelection();
                            for (int i = 2; i < split.Length; i++)
                            {
                                var s = split[i];
                                if (storySelectParameters.TryFind(x => x.Name == s, out StorySelectParameter parameter))
                                    storySelection = parameter.Select(storySelection, parameter.GetParameters(split, ref i));
                            }
                            StoryManager.inst.Play(storySelection);
                            break;
                        }
                }
            }

            public override IEnumerable<ParameterBase> GetParameters()
            {
                foreach (var parameter in storySelectParameters)
                    yield return parameter;
                var values = EnumHelper.GetValues<SelectType>();
                foreach (var value in values)
                    yield return new TypeEnumParameter<SelectType>(value, "Location type of the level to load.", value switch
                    {
                        SelectType.RelativePath => "relative_path \"beatmaps/arcade/Level Name\"",
                        SelectType.FullPath => $"full_path {RTFile.ApplicationDirectory}\"beatmaps/arcade/Level Name\"",
                        SelectType.ArcadeID => "arcade_id 0",
                        _ => null,
                    });
            }

            void LoadLevel(Level level)
            {
                if (ProjectArrhythmia.State.InEditor)
                    EditorLevelManager.inst.LoadLevel(level);
                else
                    LevelManager.Play(level);
            }

            #endregion

            #region Sub Classes

            public enum SelectType
            {
                RelativePath,
                FullPath,
                ArcadeID,
                Story,
            }

            public abstract class StorySelectParameter : ParameterBase
            {
                public abstract StorySelection Select(StorySelection storySelection, string[] parameters);
            }

            public class ChapterSelectParameter : StorySelectParameter
            {
                public override string Name => "chapter";

                public override int ParameterCount => 1;

                public override string Description => "Chapter of the story mode.";

                public override string AddToAutocomplete => "chapter 0";

                public override StorySelection Select(StorySelection storySelection, string[] parameters)
                {
                    var chapter = parameters[0];
                    if (int.TryParse(chapter, out int num))
                        storySelection.chapter = num;
                    else if (StoryMode.Instance.chapters.TryFindIndex(x => x.name == chapter, out int chapterIndex))
                        storySelection.chapter = chapterIndex;
                    return storySelection;
                }
            }

            public class LevelSelectParameter : StorySelectParameter
            {
                public override string Name => "level";

                public override int ParameterCount => 1;

                public override string Description => "A levels index within a chapter of the story mode.";

                public override string AddToAutocomplete => "level 0";

                public override StorySelection Select(StorySelection storySelection, string[] parameters)
                {
                    var level = parameters[0];
                    if (int.TryParse(level, out int num))
                        storySelection.level = num;
                    else if (StoryMode.Instance.chapters[storySelection.chapter].levels.TryFindIndex(x => x.name == level || System.IO.Path.GetFileName(x.filePath) == level, out int levelIndex))
                        storySelection.level = levelIndex;
                    return storySelection;
                }
            }

            public class CutsceneSelectParameter : StorySelectParameter
            {
                public override string Name => "cutscene";

                public override int ParameterCount => 1;

                public override string Description => "The index of a cutscene in a level in the story mode.";

                public override string AddToAutocomplete => "cutscene 0";

                public override StorySelection Select(StorySelection storySelection, string[] parameters)
                {
                    var cutscene = parameters[0];
                    if (int.TryParse(cutscene, out int num))
                        storySelection.cutsceneIndex = num;
                    return storySelection;
                }
            }

            public class BonusSelectParameter : StorySelectParameter
            {
                public override string Name => "bonus";

                public override string Description => "If bonus chapters should be selected.";

                public override StorySelection Select(StorySelection storySelection, string[] parameters)
                {
                    storySelection.bonus = true;
                    return storySelection;
                }
            }

            public class SkipCutscenesParameter : StorySelectParameter
            {
                public override string Name => "skip_cutscenes";

                public override string Description => "If cutscenes should be skipped.";

                public override StorySelection Select(StorySelection storySelection, string[] parameters)
                {
                    storySelection.skipCutscenes = true;
                    return storySelection;
                }
            }

            #endregion
        }

        /// <summary>
        /// Represents a command capable of setting the state of the current interface.
        /// </summary>
        public class HideInterfaceCommand : ExampleCommand
        {
            #region Values

            public override string Name => "hide_interface";

            public override bool Usable => ProjectArrhythmia.State.InGame;

            public override string Pattern => "hide_interface [true/false]";

            public override string Description => "Hides the interface.";

            #endregion

            #region Functions

            public override void ConsumeInput(string input, string[] split)
            {
                if (!InterfaceManager.inst || !InterfaceManager.inst.CurrentInterface)
                    return;

                var value = split[1];
                if (value == "swap")
                {
                    InterfaceManager.inst.CurrentInterface.SetActive(!InterfaceManager.inst.CurrentInterface.UIActive);
                    return;
                }

                InterfaceManager.inst.CurrentInterface.SetActive(!Parser.TryParse(split[1], false));
            }

            public override IEnumerable<ParameterBase> GetParameters()
            {
                yield return new GenericParameter("true", "Hides the interface.");
                yield return new GenericParameter("false", "Shows the interface.");
                yield return new GenericParameter("swap", "Toggles the interface.");
            }

            #endregion
        }

        /// <summary>
        /// Represents a command capable of setting the state of <see cref="EventsConfig.ShowGUI"/>.
        /// </summary>
        public class ShowPlayerGUICommand : ExampleCommand
        {
            #region Values

            public override string Name => "show_player_gui";

            public override bool Usable => ProjectArrhythmia.State.InGame;

            public override string Pattern => "show_player_gui [true/false]";

            public override string Description => "Sets the active state of players and the player GUI.";

            #endregion

            #region Functions

            public override void ConsumeInput(string input, string[] split) => EventsConfig.Instance.ShowGUI.Value = split[1] == "swap" ? !EventsConfig.Instance.ShowGUI.Value : Parser.TryParse(split[1], true);

            public override IEnumerable<ParameterBase> GetParameters()
            {
                yield return new GenericParameter("true", "Shows the players and GUI.");
                yield return new GenericParameter("false", "Hides the players and GUI.");
                yield return new GenericParameter("swap", "Toggles the players and GUI.");
            }

            #endregion
        }

        /// <summary>
        /// Represents a command capable of setting the state of <see cref="EventsConfig.HideTimeline"/>.
        /// </summary>
        public class HideTimelineCommand : ExampleCommand
        {
            #region Values

            public override string Name => "hide_timeline";

            public override bool Usable => ProjectArrhythmia.State.InGame;

            public override string Pattern => "hide_timeline [true/false]";

            public override string Description => "Hides the timeline.";

            #endregion

            #region Functions

            public override void ConsumeInput(string input, string[] split) => EventsConfig.Instance.HideTimeline.Value = split[1] == "swap" ? !EventsConfig.Instance.HideTimeline.Value : Parser.TryParse(split[1], true);

            public override IEnumerable<ParameterBase> GetParameters()
            {
                yield return new GenericParameter("true", "Hides the timeline.");
                yield return new GenericParameter("false", "Shows the timeline.");
                yield return new GenericParameter("swap", "Toggles the timeline.");
            }

            #endregion
        }

        #endregion

        #region Editor Commands

        /// <summary>
        /// Represents a command capable of editing objects.
        /// </summary>
        public class EditCommand : ExampleCommand
        {
            #region Values

            public override string Name => "edit";

            public override bool Usable => false;

            public override string Pattern => "create category [values]";

            public override string Description => "Edits an objects' values. Only available in the editor.";

            /// <summary>
            /// Current index per selectable item.
            /// </summary>
            public static int index;

            /// <summary>
            /// Current amount of selectables.
            /// </summary>
            public static int count;

            /// <summary>
            /// The current category.
            /// </summary>
            public static CategoryType CurrentCategory { get; set; }

            /// <summary>
            /// List of editor parameters for <see cref="BeatmapObject"/>.
            /// </summary>
            public static List<EditParameter<BeatmapObject>> beatmapObjectParameters = new List<EditParameter<BeatmapObject>>
            {
                new BeatmapObjectNameParameter(),
                new ObjectStartTimeParameter<BeatmapObject>(),
                new ObjectAddStartTimeParameter<BeatmapObject>(),
                new BeatmapObjectAutoKillTypeParameter(),
                new BeatmapObjectAutoKillOffsetParameter(),
                new LayerParameter<BeatmapObject>(),
                new BinParameter<BeatmapObject>(),
                new AddBinParameter<BeatmapObject>(),
                new ShapeParameter<BeatmapObject>(),
                new ShapeTextParameter<BeatmapObject>(),
                new ParentParameter<BeatmapObject>(),
                new BeatmapObjectGradientTypeParameter(),
                new BeatmapObjectRenderDepthParameter(),
                new BeatmapObjectPositionKeyframeParameter(),
                new BeatmapObjectScaleKeyframeParameter(),
                new BeatmapObjectRotationKeyframeParameter(),
                new BeatmapObjectColorKeyframeParameter(),
            };

            /// <summary>
            /// List of editor parameters for <see cref="BackgroundObject"/>.
            /// </summary>
            public static List<EditParameter<BackgroundObject>> backgroundObjectParameters = new List<EditParameter<BackgroundObject>>
            {
                new BackgroundObjectNameParameter(),
                new ObjectStartTimeParameter<BackgroundObject>(),
                new ObjectAddStartTimeParameter<BackgroundObject>(),
                new BackgroundObjectAutoKillTypeParameter(),
                new BackgroundObjectAutoKillOffsetParameter(),
                new LayerParameter<BackgroundObject>(),
                new BinParameter<BackgroundObject>(),
                new AddBinParameter<BackgroundObject>(),
                new BackgroundObjectPositionParameter(),
                new BackgroundObjectScaleParameter(),
                new BackgroundObjectRotationParameter(),
                new BackgroundObjectColorParameter(),
                new BackgroundObjectFadeColorParameter(),
                new ShapeParameter<BackgroundObject>(),
            };

            /// <summary>
            /// List of editor parameters for <see cref="PrefabObject"/>.
            /// </summary>
            public static List<EditParameter<PrefabObject>> prefabObjectParameters = new List<EditParameter<PrefabObject>>
            {
                new ObjectStartTimeParameter<PrefabObject>(),
                new ObjectAddStartTimeParameter<PrefabObject>(),
                new PrefabObjectAutoKillTypeParameter(),
                new PrefabObjectAutoKillOffsetParameter(),
                new LayerParameter<PrefabObject>(),
                new BinParameter<PrefabObject>(),
                new AddBinParameter<PrefabObject>(),
                new ParentParameter<PrefabObject>(),
                new PrefabObjectPositionParameter(),
                new PrefabObjectScaleParameter(),
                new PrefabObjectRotationParameter(),
                new PrefabObjectDepthParameter(),
            };

            /// <summary>
            /// List of editor parameters for <see cref="Marker"/>.
            /// </summary>
            public static List<EditParameter<Marker>> markerParameters = new List<EditParameter<Marker>>
            {
                new MarkerNameParameter(),
                new MarkerTimeParameter(),
                new MarkerAddTimeParameter(),
                new MarkerDescriptionParameter(),
                new MarkerColorParameter(),
            };

            /// <summary>
            /// List of editor parameters for <see cref="Checkpoint"/>.
            /// </summary>
            public static List<EditParameter<Checkpoint>> checkpointParameters = new List<EditParameter<Checkpoint>>
            {
                new CheckpointNameParameter(),
                new CheckpointTimeParameter(),
                new CheckpointAddTimeParameter(),
            };

            /// <summary>
            /// List of editor parameters for <see cref="EventKeyframe"/>.
            /// </summary>
            public static List<EditParameter<EventKeyframe>> eventKeyframeParameters = new List<EditParameter<EventKeyframe>>
            {
                new EventKeframeTimeParameter(),
                new EventKeyframeValueParameter(),
                new EventKeyframeRandomValueParameter(),
                new EventKeyframeStringValueParameter(),
                new EventKeyframeEasingParameter(),
                new EventKeyframeRandomTypeParameter(),
                new EventKeyframeRelativeParameter(),
            };

            /// <summary>
            /// List of editor parameters for <see cref="Modifier"/>.
            /// </summary>
            public static List<EditParameter<Modifier>> modifierParameters = new List<EditParameter<Modifier>>
            {
                new ModifierValueParameter(),
                new ModifierConstantParameter(),
                new ModifierRunCountParameter(),
                new ModifierPrefabGroupOnlyParameter(),
                new ModifierGroupAliveParameter(),
                new ModifierNotParameter(),
                new ModifierElseIfParameter(),
            };

            /// <summary>
            /// List of editor parameters for <see cref="Annotation"/>.
            /// </summary>
            public static List<EditParameter<Annotation>> annotationParameters = new List<EditParameter<Annotation>>
            {
                new AnnotationColorParameter(),
                new AnnotationHexColorParameter(),
                new AnnotationOpacityParameter(),
                new AnnotationThicknessParameter(),
                new AnnotationFixedCameraParameter(),
                new AnnotationHiddenParameter(),
                new AnnotationMoveParameter(),
                new AnnotationScaleParameter(),
                new AnnotationRotateParameter(),
            };

            #endregion

            #region Functions

            public override void ConsumeInput(string input, string[] split) => throw new NotImplementedException();

            public void ConsumeInput(string input, string[] parameters, SelectCommand.SelectableType selectableType, IEnumerable<ISelectable> selectables)
            {
                (int, int) command = (0, 0);
                switch (selectableType)
                {
                    case SelectCommand.SelectableType.Objects: {
                            CurrentCategory = Parser.TryParse(parameters[0].ToLower().Remove("_"), true, CategoryType.Null);
                            TimelineObject.TimelineReferenceType referenceType = CurrentCategory switch
                            {
                                CategoryType.BeatmapObject => TimelineObject.TimelineReferenceType.BeatmapObject,
                                CategoryType.BackgroundObject => TimelineObject.TimelineReferenceType.BackgroundObject,
                                CategoryType.PrefabObject => TimelineObject.TimelineReferenceType.PrefabObject,
                                _ => TimelineObject.TimelineReferenceType.Null,
                            };
                            index = 0;
                            count = selectables.Count(x => x is TimelineObject timelineObject && timelineObject.TimelineReference == referenceType);
                            foreach (var selectable in selectables)
                            {
                                if (selectable is not TimelineObject timelineObject || timelineObject.TimelineReference != referenceType)
                                    continue;
                                switch (referenceType)
                                {
                                    case TimelineObject.TimelineReferenceType.BeatmapObject: {
                                            var beatmapObject = timelineObject.GetData<BeatmapObject>();
                                            CurrentEdit.beatmapObject = beatmapObject;
                                            command = Apply(1, beatmapObject, parameters, beatmapObjectParameters);
                                            RTLevel.Current?.UpdateObject(beatmapObject);
                                            break;
                                        }
                                    case TimelineObject.TimelineReferenceType.BackgroundObject: {
                                            var backgroundObject = timelineObject.GetData<BackgroundObject>();
                                            CurrentEdit.backgroundObject = backgroundObject;
                                            command = Apply(1, backgroundObject, parameters, backgroundObjectParameters);
                                            RTLevel.Current?.UpdateBackgroundObject(backgroundObject);
                                            break;
                                        }
                                    case TimelineObject.TimelineReferenceType.PrefabObject: {
                                            var prefabObject = timelineObject.GetData<PrefabObject>();
                                            CurrentEdit.prefabObject = prefabObject;
                                            command = Apply(1, prefabObject, parameters, prefabObjectParameters);
                                            RTLevel.Current?.UpdatePrefab(prefabObject);
                                            break;
                                        }
                                }
                                timelineObject.Render();
                                index++;
                            }
                            break;
                        }
                    case SelectCommand.SelectableType.Keyframes: {
                            index = 0;
                            count = selectables.Count(x => x is TimelineKeyframe);
                            foreach (var selectable in selectables)
                            {
                                if (selectable is not TimelineKeyframe timelineKeyframe)
                                    continue;
                                CurrentEdit.eventKeyframe = timelineKeyframe.eventKeyframe;
                                command = Apply(0, timelineKeyframe.eventKeyframe, parameters, eventKeyframeParameters);
                                timelineKeyframe.Render();
                                index++;
                            }
                            break;
                        }
                    case SelectCommand.SelectableType.ObjectKeyframes: {
                            index = 0;
                            count = selectables.Count(x => x is TimelineKeyframe);
                            foreach (var selectable in selectables)
                            {
                                if (selectable is not TimelineKeyframe timelineKeyframe)
                                    continue;
                                var randomType = timelineKeyframe.eventKeyframe.RandomType;
                                CurrentEdit.eventKeyframe = timelineKeyframe.eventKeyframe;
                                command = Apply(0, timelineKeyframe.eventKeyframe, parameters, eventKeyframeParameters);
                                timelineKeyframe.Render();

                                if (timelineKeyframe.Type == 3 && randomType != timelineKeyframe.eventKeyframe.RandomType)
                                {
                                    if (timelineKeyframe.eventKeyframe.RandomType == RandomType.None)
                                        timelineKeyframe.eventKeyframe.SetStringValues();
                                    if (timelineKeyframe.eventKeyframe.RandomType == RandomType.Normal)
                                        timelineKeyframe.eventKeyframe.SetStringValues(RTColors.WHITE_HEX_CODE, RTColors.WHITE_HEX_CODE);
                                }

                                index++;
                            }
                            EditorTimeline.inst.CurrentSelection?.Render();
                            if (ObjectEditor.inst.Dialog.IsCurrent && EditorTimeline.inst.CurrentSelection && EditorTimeline.inst.CurrentSelection.isBeatmapObject)
                                ObjectEditor.inst.RenderDialog(EditorTimeline.inst.CurrentSelection.GetData<BeatmapObject>());
                            break;
                        }
                    case SelectCommand.SelectableType.Markers: {
                            index = 0;
                            count = selectables.Count(x => x is TimelineMarker);
                            foreach (var selectable in selectables)
                            {
                                if (selectable is not TimelineMarker timelineMarker)
                                    continue;
                                CurrentEdit.marker = timelineMarker.Marker;
                                command = Apply(0, timelineMarker.Marker, parameters, markerParameters);
                                timelineMarker.Render();
                                index++;
                            }
                            break;
                        }
                    case SelectCommand.SelectableType.Checkpoints: {
                            index = 0;
                            count = selectables.Count(x => x is TimelineCheckpoint);
                            foreach (var selectable in selectables)
                            {
                                if (selectable is not TimelineCheckpoint timelineCheckpoint)
                                    continue;
                                CurrentEdit.checkpoint = timelineCheckpoint.Checkpoint;
                                command = Apply(0, timelineCheckpoint.Checkpoint, parameters, checkpointParameters);
                                timelineCheckpoint.Render();
                                index++;
                            }
                            break;
                        }
                    case SelectCommand.SelectableType.Modifiers: {
                            index = 0;
                            count = selectables.Count(x => x is ModifierCard);
                            foreach (var selectable in selectables)
                            {
                                if (selectable is not ModifierCard modifierCard)
                                    continue;
                                CurrentEdit.modifier = modifierCard.Modifier;
                                command = Apply(0, modifierCard.Modifier, parameters, modifierParameters);
                                index++;
                            }
                            if (ModifiersEditorDialog.Current)
                                CoroutineHelper.StartCoroutine(ModifiersEditorDialog.Current.RenderModifiers(ModifiersEditorDialog.Current.CurrentObject));
                            break;
                        }
                    case SelectCommand.SelectableType.Annotations: {
                            index = 0;
                            count = selectables.Count(x => x is Annotation);
                            foreach (var selectable in selectables)
                            {
                                if (selectable is not Annotation annotation)
                                    continue;
                                CurrentEdit.annotation = annotation;
                                command = Apply(0, annotation, parameters, annotationParameters);
                                index++;
                            }
                            break;
                        }
                }
                SwitchMode(command.Item1, command.Item2, input, parameters, selectables);
                CurrentEdit.Reset();
            }

            /// <summary>
            /// Applies a set of parameters to an object.
            /// </summary>
            /// <typeparam name="T">Type of the object.</typeparam>
            /// <param name="startIndex">Start index for the parameters array.</param>
            /// <param name="obj">Object to edit.</param>
            /// <param name="split">Array of parameters.</param>
            /// <param name="parameters">List of edit parameters.</param>
            /// <returns>Returns the command type and the parameter index the function returned on.</returns>
            public (int, int) Apply<T>(int startIndex, T obj, string[] split, List<EditParameter<T>> parameters)
            {
                for (int i = startIndex; i < split.Length; i++)
                {
                    var s = split[i];
                    if (s == "select")
                        return (1, i + 1);
                    if (s == "->")
                        return (2, i + 1);

                    if (parameters.TryFind(x => x.Name == s, out EditParameter<T> parameter))
                        parameter.Apply(obj, parameter.GetParameters(split, ref i));
                }
                return (0, 0);
            }

            /// <summary>
            /// Switches to select or action mode.
            /// </summary>
            /// <param name="commandType">Command type to switch to.<br/>
            /// 0 = none<br/>
            /// 1 = select<br/>
            /// 2 = action
            /// </param>
            /// <param name="index">Index of the parameter to start from.</param>
            /// <param name="input">Input string.</param>
            /// <param name="split">Parameters array.</param>
            /// <param name="selectables">Collection of selectables.</param>
            public void SwitchMode(int commandType, int index, string input, string[] split, IEnumerable<ISelectable> selectables = null)
            {
                switch (commandType)
                {
                    case 1: {
                            var selectCommand = new SelectCommand();
                            selectCommand.DoSelection(selectCommand.GetSelectablesFromInput(input, split.Range(index, split.Length - 1).ToArray(), false, selectables));
                            break;
                        }
                    case 2: {
                            var selectCommand = new SelectCommand();
                            selectCommand.DoSelection(selectCommand.GetSelectablesFromInput(input, split.Range(index, split.Length - 1).ToArray(), true, selectables));
                            break;
                        }
                }
            }

            public override IEnumerable<ParameterBase> GetParameters()
            {
                foreach (var parameter in beatmapObjectParameters)
                    yield return parameter;
                foreach (var parameter in backgroundObjectParameters)
                    yield return parameter;
                foreach (var parameter in prefabObjectParameters)
                    yield return parameter;
                foreach (var parameter in markerParameters)
                    yield return parameter;
                foreach (var parameter in checkpointParameters)
                    yield return parameter;
                foreach (var parameter in eventKeyframeParameters)
                    yield return parameter;
                foreach (var parameter in modifierParameters)
                    yield return parameter;
                var values = EnumHelper.GetValues<CategoryType>();
                foreach (var value in values)
                {
                    if (value != CategoryType.Null)
                        yield return new TypeEnumParameter<CategoryType>(value, "Type of the object to edit.");
                }
            }

            #endregion

            #region Sub Classes

            /// <summary>
            /// The base edit parameter.
            /// </summary>
            /// <typeparam name="T">Type of the object the parameter can edit.</typeparam>
            public abstract class EditParameter<T> : ParameterBase
            {
                /// <summary>
                /// Applies the edit parameter to an object.
                /// </summary>
                /// <param name="obj">Object to apply to.</param>
                /// <param name="parameters">Array of parameters.</param>
                public abstract void Apply(T obj, string[] parameters);

                public override string GetVariable(string name) => name switch
                {
                    "index" => index.ToString(),
                    "count" => count.ToString(),
                    _ => base.GetVariable(name),
                };

                public override IEnumerable<(string, string)> GetVariables()
                {
                    yield return ("index", index.ToString());
                    yield return ("count", count.ToString());
                    foreach (var variable in base.GetVariables())
                        yield return variable;
                }
            }

            /// <summary>
            /// Categories of object types that can be edited.
            /// </summary>
            public enum CategoryType
            {
                /// <summary>
                /// No object specified.
                /// </summary>
                Null,
                /// <summary>
                /// Edits <see cref="Core.Data.Beatmap.BeatmapObject"/> values.
                /// </summary>
                BeatmapObject,
                /// <summary>
                /// Edits <see cref="Core.Data.Beatmap.BackgroundObject"/> values.
                /// </summary>
                BackgroundObject,
                /// <summary>
                /// Edits <see cref="Core.Data.Beatmap.PrefabObject"/> values.
                /// </summary>
                PrefabObject,
                /// <summary>
                /// Edits <see cref="Core.Data.Beatmap.Marker"/> values.
                /// </summary>
                Marker,
                /// <summary>
                /// Edits <see cref="Core.Data.Beatmap.Checkpoint"/> values.
                /// </summary>
                Checkpoint,
                /// <summary>
                /// Edits <see cref="Core.Data.Modifiers.Modifier"/> values.
                /// </summary>
                Modifier,
            }

            /// <summary>
            /// The current edit cache.
            /// </summary>
            public static class CurrentEdit
            {
                #region Values

                /// <summary>
                /// The current <see cref="BeatmapObject"/>.
                /// </summary>
                public static BeatmapObject beatmapObject;

                /// <summary>
                /// The current <see cref="BackgroundObject"/>.
                /// </summary>
                public static BackgroundObject backgroundObject;

                /// <summary>
                /// The current <see cref="PrefabObject"/>.
                /// </summary>
                public static PrefabObject prefabObject;

                /// <summary>
                /// The current <see cref="Marker"/>.
                /// </summary>
                public static Marker marker;

                /// <summary>
                /// The current <see cref="Checkpoint"/>.
                /// </summary>
                public static Checkpoint checkpoint;

                /// <summary>
                /// The current <see cref="Modifier"/>.
                /// </summary>
                public static Modifier modifier;

                /// <summary>
                /// The current <see cref="EventKeyframe"/>.
                /// </summary>
                public static EventKeyframe eventKeyframe;

                /// <summary>
                /// The current <see cref="Annotation"/>.
                /// </summary>
                public static Annotation annotation;

                #endregion

                #region Functions

                /// <summary>
                /// Resets the current edit cache.
                /// </summary>
                public static void Reset()
                {
                    beatmapObject = null;
                    backgroundObject = null;
                    prefabObject = null;
                    marker = null;
                    checkpoint = null;
                    modifier = null;
                    eventKeyframe = null;
                    annotation = null;
                }

                #endregion
            }

            #region General

            /// <summary>
            /// Edits the layer of an object.
            /// </summary>
            /// <typeparam name="T">Type of the object to edit. Must be an <see cref="IEditable"/>.</typeparam>
            public class LayerParameter<T> : EditParameter<T> where T : IEditable
            {
                public override string Name => "editor_layer";

                public override int ParameterCount => 1;

                public override string Description => "Editor layer of the object.";

                public override string AddToAutocomplete => "editor_layer 0";

                public override void Apply(T obj, string[] parameters) => obj.EditorData.Layer = Parser.TryParse(parameters[0], 0);
            }

            /// <summary>
            /// Edits the bin of an object.
            /// </summary>
            /// <typeparam name="T">Type of the object to edit. Must be an <see cref="IEditable"/>.</typeparam>
            public class BinParameter<T> : EditParameter<T> where T : IEditable
            {
                public override string Name => "editor_bin";

                public override int ParameterCount => 1;

                public override string Description => "Editor bin of the object.";

                public override string AddToAutocomplete => "editor_bin max_bin";

                public override void Apply(T obj, string[] parameters) => obj.EditorData.Bin = Parser.TryParse(parameters[0], 0);
            }

            /// <summary>
            /// Edits the bin of an object.
            /// </summary>
            /// <typeparam name="T">Type of the object to edit. Must be an <see cref="IEditable"/>.</typeparam>
            public class AddBinParameter<T> : EditParameter<T> where T : IEditable
            {
                public override string Name => "add_editor_bin";

                public override int ParameterCount => 2;

                public override string Description => "Editor bin of the object.";

                public override string AddToAutocomplete => "add_editor_bin max_bin 1";

                public override void Apply(T obj, string[] parameters) => obj.EditorData.Bin = Parser.TryParse(parameters[0], 0) + (Parser.TryParse(parameters[1], 0) * index);
            }

            /// <summary>
            /// Edits the start time of an object.
            /// </summary>
            /// <typeparam name="T">Type of the object to edit. Must be an <see cref="ILifetime"/>.</typeparam>
            public class ObjectStartTimeParameter<T> : EditParameter<T> where T : ILifetime
            {
                public override string Name => "start_time";

                public override int ParameterCount => 1;

                public override string Description => "Start time of the object.";

                public override string AddToAutocomplete => "start_time 0";

                public override void Apply(T obj, string[] parameters) => obj.StartTime = Parser.TryParse(parameters[0], 0f);
            }

            /// <summary>
            /// Edits the start time of an object.
            /// </summary>
            /// <typeparam name="T">Type of the object to edit. Must be an <see cref="ILifetime"/>.</typeparam>
            public class ObjectAddStartTimeParameter<T> : EditParameter<T> where T : ILifetime
            {
                public override string Name => "add_start_time";

                public override int ParameterCount => 1;

                public override string Description => "Start time of the object.";

                public override string AddToAutocomplete => "add_start_time 0.1";

                public override void Apply(T obj, string[] parameters) => obj.StartTime = Parser.TryParse(parameters[0], 0f) + (Parser.TryParse(parameters[1], 0f) * index);
            }

            /// <summary>
            /// Edits the shape of an object.
            /// </summary>
            /// <typeparam name="T">Type of the object to edit. Must be an <see cref="IShapeable"/>.</typeparam>
            public class ShapeParameter<T> : EditParameter<T> where T : IShapeable
            {
                public override string Name => "shape";

                public override int ParameterCount => 2;

                public override string Description => "Shape of the object.";

                public override string AddToAutocomplete => "shape 0 0";

                public override void Apply(T obj, string[] parameters)
                {
                    if (Enum.TryParse(parameters[0].Remove("_"), true, out ShapeType shapeType))
                        obj.ShapeType = shapeType;
                    else
                        obj.Shape = Parser.TryParse(parameters[0], 0);
                    obj.ShapeOption = Parser.TryParse(parameters[1], 0);
                }
            }

            /// <summary>
            /// Edits the text of an object.
            /// </summary>
            /// <typeparam name="T">Type of the object to edit. Must be an <see cref="IShapeable"/>.</typeparam>
            public class ShapeTextParameter<T> : EditParameter<T> where T : IShapeable
            {
                public override string Name => "text";

                public override int ParameterCount => 1;

                public override string Description => "Text of the object.";

                public override string AddToAutocomplete => "text \"Some text\"";

                public override void Apply(T obj, string[] parameters) => obj.Text = parameters[0];
            }

            /// <summary>
            /// Edits the parent of an object.
            /// </summary>
            /// <typeparam name="T">Type of the object to edit. Must be an <see cref="IParentable"/>.</typeparam>
            public class ParentParameter<T> : EditParameter<T> where T : IParentable
            {
                public override string Name => "parent";

                public override int BracketsType => 2;

                public override string Description => "Parent of the object.";

                public override string AddToAutocomplete => "parent [select objects]";

                public override void Apply(T obj, string[] parameters)
                {
                    var input = GetJoinedSquareBracketsParameter(parameters);
                    var split = input.Split(' ');
                    if (split[0] == "select")
                    {
                        var selectables = new SelectCommand().GetSelectablesFromInput(input, split);
                        var first = selectables.FirstOrDefault();
                        if (first is TimelineObject timelineObject && timelineObject.TryGetData(out BeatmapObject beatmapObject))
                            obj.SetParent(beatmapObject);
                    }
                }
            }

            #endregion

            #region Beatmap Object

            /// <summary>
            /// Edits the name of an object.
            /// </summary>
            public class BeatmapObjectNameParameter : EditParameter<BeatmapObject>
            {
                public override string Name => "name";

                public override int ParameterCount => 1;

                public override string Description => "Name of the object.";

                public override string AddToAutocomplete => "name \"Object Name\"";

                public override void Apply(BeatmapObject obj, string[] parameters) => obj.name = parameters[0];
            }

            /// <summary>
            /// Edits the autokill type of an object.
            /// </summary>
            public class BeatmapObjectAutoKillTypeParameter : EditParameter<BeatmapObject>
            {
                public override string Name => "autokill_type";

                public override int ParameterCount => 1;

                public override string Description => "Despawn type of the object.";

                public override string AddToAutocomplete => "autokill_type no_autokill";

                public override void Apply(BeatmapObject obj, string[] parameters)
                {
                    if (Enum.TryParse(parameters[0].Remove("_"), true, out AutoKillType autoKillType))
                        obj.autoKillType = autoKillType;
                    else
                        obj.autoKillType = (AutoKillType)Parser.TryParse(parameters[0], 0);
                }
            }

            /// <summary>
            /// Edits the autokill offset of an object.
            /// </summary>
            public class BeatmapObjectAutoKillOffsetParameter : EditParameter<BeatmapObject>
            {
                public override string Name => "autokill_offset";

                public override int ParameterCount => 2;

                public override string Description => "Despawn time of the object.";

                public override string AddToAutocomplete => "autokill_offset set 0";

                public override void Apply(BeatmapObject obj, string[] parameters) => RTMath.Operation(ref obj.autoKillOffset, Parser.TryParse(parameters[1], 0f), RTMath.GetOperation(parameters[0], MathOperation.Set));
            }

            /// <summary>
            /// Edits the gradient type of an object.
            /// </summary>
            public class BeatmapObjectGradientTypeParameter : EditParameter<BeatmapObject>
            {
                public override string Name => "gradient_type";

                public override int ParameterCount => 1;

                public override string Description => "Gradient type of the object.";

                public override string AddToAutocomplete => "gradient_type linear";

                public override void Apply(BeatmapObject obj, string[] parameters)
                {
                    if (obj.IsSpecialShape)
                        return;

                    if (Enum.TryParse(parameters[0].Remove("_"), true, out GradientType gradientType))
                        obj.gradientType = gradientType;
                    else
                        obj.gradientType = (GradientType)Parser.TryParse(parameters[0], 0);
                }
            }

            /// <summary>
            /// Edits the render depth of an object.
            /// </summary>
            public class BeatmapObjectRenderDepthParameter : EditParameter<BeatmapObject>
            {
                public override string Name => "depth";

                public override int ParameterCount => 2;

                public override string Description => "Render depth of the object.";

                public override string AddToAutocomplete => "depth set 15";

                public override void Apply(BeatmapObject obj, string[] parameters) => RTMath.Operation(ref obj.depth, Parser.TryParse(parameters[1], 0f), RTMath.GetOperation(parameters[0], MathOperation.Set));
            }

            /// <summary>
            /// Edits a position keyframe of an object.
            /// </summary>
            public class BeatmapObjectPositionKeyframeParameter : EditParameter<BeatmapObject>
            {
                public override string Name => "pos_kf";

                public override int BracketsType => 2;

                public override string Description => "Position keyframe of the object. Creates a new keyframe if no keyframe is found with the same keyframe time.";

                public override string AddToAutocomplete => "pos_kf [time 0 value 0 set 10]";

                public override void Apply(BeatmapObject obj, string[] parameters)
                {
                    var eventKeyframe = EventKeyframe.DefaultPositionKeyframe;
                    eventKeyframe.timelineKeyframe = new TimelineKeyframe(eventKeyframe);
                    eventKeyframe.timelineKeyframe.SetCoord(new KeyframeCoord(0, obj.events.Count));
                    for (int i = 0; i < parameters.Length; i++)
                    {
                        var p = parameters[i];
                        if (i == 0)
                            p = p.TrimStart('[');
                        if (i == parameters.Length - 1)
                            p = p.TrimEnd(']');
                        if (eventKeyframeParameters.TryFind(x => x.Name == p, out EditParameter<EventKeyframe> parameter))
                            parameter.Apply(eventKeyframe, parameter.GetParameters(parameters, ref i));
                    }
                    if (obj.events[0].TryFind(x => x.time == eventKeyframe.time, out EventKeyframe existingKeyframe))
                        existingKeyframe.CopyData(eventKeyframe);
                    else if (eventKeyframe.time > 0f)
                        obj.events[0].Add(eventKeyframe);
                }
            }

            /// <summary>
            /// Edits a scale keyframe of an object.
            /// </summary>
            public class BeatmapObjectScaleKeyframeParameter : EditParameter<BeatmapObject>
            {
                public override string Name => "sca_kf";

                public override int BracketsType => 2;

                public override string Description => "Scale keyframe of the object. Creates a new keyframe if no keyframe is found with the same keyframe time.";

                public override string AddToAutocomplete => "sca_kf [time 0 value 0 set 10]";

                public override void Apply(BeatmapObject obj, string[] parameters)
                {
                    var eventKeyframe = EventKeyframe.DefaultScaleKeyframe;
                    eventKeyframe.timelineKeyframe = new TimelineKeyframe(eventKeyframe);
                    eventKeyframe.timelineKeyframe.SetCoord(new KeyframeCoord(1, obj.events.Count));
                    for (int i = 0; i < parameters.Length; i++)
                    {
                        var p = parameters[i];
                        if (i == 0)
                            p = p.TrimStart('[');
                        if (i == parameters.Length - 1)
                            p = p.TrimEnd(']');
                        if (eventKeyframeParameters.TryFind(x => x.Name == p, out EditParameter<EventKeyframe> parameter))
                            parameter.Apply(eventKeyframe, parameter.GetParameters(parameters, ref i));
                    }
                    if (obj.events[1].TryFind(x => x.time == eventKeyframe.time, out EventKeyframe existingKeyframe))
                        existingKeyframe.CopyData(eventKeyframe);
                    else if (eventKeyframe.time > 0f)
                        obj.events[1].Add(eventKeyframe);
                }
            }

            /// <summary>
            /// Edits a rotation keyframe of an object.
            /// </summary>
            public class BeatmapObjectRotationKeyframeParameter : EditParameter<BeatmapObject>
            {
                public override string Name => "rot_kf";

                public override int BracketsType => 2;

                public override string Description => "Rotation keyframe of the object. Creates a new keyframe if no keyframe is found with the same keyframe time.";

                public override string AddToAutocomplete => "rot_kf [time 0 value 0 set 10]";

                public override void Apply(BeatmapObject obj, string[] parameters)
                {
                    var eventKeyframe = EventKeyframe.DefaultRotationKeyframe;
                    eventKeyframe.timelineKeyframe = new TimelineKeyframe(eventKeyframe);
                    eventKeyframe.timelineKeyframe.SetCoord(new KeyframeCoord(2, obj.events.Count));
                    for (int i = 0; i < parameters.Length; i++)
                    {
                        var p = parameters[i];
                        if (i == 0)
                            p = p.TrimStart('[');
                        if (i == parameters.Length - 1)
                            p = p.TrimEnd(']');
                        if (eventKeyframeParameters.TryFind(x => x.Name == p, out EditParameter<EventKeyframe> parameter))
                            parameter.Apply(eventKeyframe, parameter.GetParameters(parameters, ref i));
                    }
                    if (obj.events[2].TryFind(x => x.time == eventKeyframe.time, out EventKeyframe existingKeyframe))
                        existingKeyframe.CopyData(eventKeyframe);
                    else if (eventKeyframe.time > 0f)
                        obj.events[2].Add(eventKeyframe);
                }
            }

            /// <summary>
            /// Edits a color keyframe of an object.
            /// </summary>
            public class BeatmapObjectColorKeyframeParameter : EditParameter<BeatmapObject>
            {
                public override string Name => "col_kf";

                public override int BracketsType => 2;

                public override string Description => "Color keyframe of the object. Creates a new keyframe if no keyframe is found with the same keyframe time.";

                public override string AddToAutocomplete => "col_kf [time 0 value 0 set 5]";

                public override void Apply(BeatmapObject obj, string[] parameters)
                {
                    var eventKeyframe = EventKeyframe.DefaultColorKeyframe;
                    eventKeyframe.timelineKeyframe = new TimelineKeyframe(eventKeyframe);
                    eventKeyframe.timelineKeyframe.SetCoord(new KeyframeCoord(3, obj.events.Count));
                    for (int i = 0; i < parameters.Length; i++)
                    {
                        var p = parameters[i];
                        if (i == 0)
                            p = p.TrimStart('[');
                        if (i == parameters.Length - 1)
                            p = p.TrimEnd(']');
                        if (eventKeyframeParameters.TryFind(x => x.Name == p, out EditParameter<EventKeyframe> parameter))
                            parameter.Apply(eventKeyframe, parameter.GetParameters(parameters, ref i));
                    }
                    eventKeyframe.relative = false;
                    if (obj.events[3].TryFind(x => x.time == eventKeyframe.time, out EventKeyframe existingKeyframe))
                        existingKeyframe.CopyData(eventKeyframe);
                    else if (eventKeyframe.time > 0f)
                        obj.events[3].Add(eventKeyframe);
                }
            }

            #endregion

            #region Background Object

            /// <summary>
            /// Edits the name of an object.
            /// </summary>
            public class BackgroundObjectNameParameter : EditParameter<BackgroundObject>
            {
                public override string Name => "name";

                public override int ParameterCount => 1;

                public override string Description => "Name of the object.";

                public override string AddToAutocomplete => "name \"Object Name\"";

                public override void Apply(BackgroundObject obj, string[] parameters) => obj.name = parameters[0];
            }

            /// <summary>
            /// Edits the autokill type of an object.
            /// </summary>
            public class BackgroundObjectAutoKillTypeParameter : EditParameter<BackgroundObject>
            {
                public override string Name => "autokill_type";

                public override int ParameterCount => 1;

                public override string Description => "Despawn type of the object.";

                public override string AddToAutocomplete => "autokill_type no_autokill";

                public override void Apply(BackgroundObject obj, string[] parameters)
                {
                    if (Enum.TryParse(parameters[0].Remove("_"), true, out AutoKillType autoKillType))
                        obj.autoKillType = autoKillType;
                    else
                        obj.autoKillType = (AutoKillType)Parser.TryParse(parameters[0], 0);
                }
            }

            /// <summary>
            /// Edits the autokill offset of an object.
            /// </summary>
            public class BackgroundObjectAutoKillOffsetParameter : EditParameter<BackgroundObject>
            {
                public override string Name => "autokill_offset";

                public override int ParameterCount => 2;

                public override string Description => "Despawn time of the object.";

                public override string AddToAutocomplete => "autokill_offset set 0";

                public override void Apply(BackgroundObject obj, string[] parameters) => RTMath.Operation(ref obj.autoKillOffset, Parser.TryParse(parameters[1], 0f), RTMath.GetOperation(parameters[0], MathOperation.Set));
            }

            /// <summary>
            /// Edits the position of an object.
            /// </summary>
            public class BackgroundObjectPositionParameter : EditParameter<BackgroundObject>
            {
                public override string Name => "pos";

                public override int ParameterCount => 3;

                public override string Description => "Position of the object.";

                public override string AddToAutocomplete => "pos set 0 0";

                public override void Apply(BackgroundObject obj, string[] parameters) => RTMath.Operation(ref obj.pos, new Vector2(Parser.TryParse(parameters[1], 0f), Parser.TryParse(parameters[2], 0f)), RTMath.GetOperation(parameters[0], MathOperation.Set));
            }

            /// <summary>
            /// Edits the scale of an object.
            /// </summary>
            public class BackgroundObjectScaleParameter : EditParameter<BackgroundObject>
            {
                public override string Name => "sca";

                public override int ParameterCount => 3;

                public override string Description => "Scale of the object.";

                public override string AddToAutocomplete => "sca set 0 0";

                public override void Apply(BackgroundObject obj, string[] parameters) => RTMath.Operation(ref obj.scale, new Vector2(Parser.TryParse(parameters[1], 0f), Parser.TryParse(parameters[2], 0f)), RTMath.GetOperation(parameters[0], MathOperation.Set));
            }

            /// <summary>
            /// Edits the rotation of an object.
            /// </summary>
            public class BackgroundObjectRotationParameter : EditParameter<BackgroundObject>
            {
                public override string Name => "rot";

                public override int ParameterCount => 2;

                public override string Description => "Rotation of the object.";

                public override string AddToAutocomplete => "rot set 0";

                public override void Apply(BackgroundObject obj, string[] parameters) => RTMath.Operation(ref obj.rot, Parser.TryParse(parameters[1], 0f), RTMath.GetOperation(parameters[0], MathOperation.Set));
            }

            /// <summary>
            /// Edits the color of an object.
            /// </summary>
            public class BackgroundObjectColorParameter : EditParameter<BackgroundObject>
            {
                public override string Name => "color";

                public override int ParameterCount => 2;

                public override string Description => "Color of the object.";

                public override string AddToAutocomplete => "color set 0";

                public override void Apply(BackgroundObject obj, string[] parameters) => RTMath.Operation(ref obj.color, Parser.TryParse(parameters[1], 0), RTMath.GetOperation(parameters[0], MathOperation.Set));
            }

            /// <summary>
            /// Edits the fade color of an object.
            /// </summary>
            public class BackgroundObjectFadeColorParameter : EditParameter<BackgroundObject>
            {
                public override string Name => "fade_color";

                public override int ParameterCount => 2;

                public override string Description => "Fade color of the object.";

                public override string AddToAutocomplete => "fade_color set 0";

                public override void Apply(BackgroundObject obj, string[] parameters) => RTMath.Operation(ref obj.fadeColor, Parser.TryParse(parameters[1], 0), RTMath.GetOperation(parameters[0], MathOperation.Set));
            }

            #endregion

            #region Prefab Object

            /// <summary>
            /// Edits the autokill type of an object.
            /// </summary>
            public class PrefabObjectAutoKillTypeParameter : EditParameter<PrefabObject>
            {
                public override string Name => "autokill_type";

                public override int ParameterCount => 1;

                public override string Description => "Despawn type of the object.";

                public override string AddToAutocomplete => "autokill_type no_autokill";

                public override void Apply(PrefabObject obj, string[] parameters)
                {
                    if (Enum.TryParse(parameters[0].Remove("_"), true, out PrefabAutoKillType autoKillType))
                        obj.autoKillType = autoKillType;
                    else
                        obj.autoKillType = (PrefabAutoKillType)Parser.TryParse(parameters[0], 0);
                }
            }

            /// <summary>
            /// Edits the autokill offset of an object.
            /// </summary>
            public class PrefabObjectAutoKillOffsetParameter : EditParameter<PrefabObject>
            {
                public override string Name => "autokill_offset";

                public override int ParameterCount => 2;

                public override string Description => "Despawn time of the object.";

                public override string AddToAutocomplete => "autokill_offset set 0";

                public override void Apply(PrefabObject obj, string[] parameters) => RTMath.Operation(ref obj.autoKillOffset, Parser.TryParse(parameters[1], 0f), RTMath.GetOperation(parameters[0], MathOperation.Set));
            }

            /// <summary>
            /// Edits the position of an object.
            /// </summary>
            public class PrefabObjectPositionParameter : EditParameter<PrefabObject>
            {
                public override string Name => "pos";

                public override int ParameterCount => 3;

                public override string Description => "Position of the object.";

                public override string AddToAutocomplete => "pos set 0 0";

                public override void Apply(PrefabObject obj, string[] parameters)
                {
                    var x = obj.events[0].values.GetAtOrDefault(0, 0f);
                    var y = obj.events[0].values.GetAtOrDefault(1, 0f);
                    RTMath.Operation(ref x, Parser.TryParse(parameters[1], 0f), RTMath.GetOperation(parameters[0], MathOperation.Set));
                    RTMath.Operation(ref y, Parser.TryParse(parameters[2], 0f), RTMath.GetOperation(parameters[0], MathOperation.Set));
                    obj.events[0].SetValues(x, y);
                }
            }

            /// <summary>
            /// Edits the scale of an object.
            /// </summary>
            public class PrefabObjectScaleParameter : EditParameter<PrefabObject>
            {
                public override string Name => "sca";

                public override int ParameterCount => 3;

                public override string Description => "Scale of the object.";

                public override string AddToAutocomplete => "sca set 0 0";

                public override void Apply(PrefabObject obj, string[] parameters)
                {
                    var x = obj.events[1].values.GetAtOrDefault(0, 0f);
                    var y = obj.events[1].values.GetAtOrDefault(1, 0f);
                    RTMath.Operation(ref x, Parser.TryParse(parameters[1], 0f), RTMath.GetOperation(parameters[0], MathOperation.Set));
                    RTMath.Operation(ref y, Parser.TryParse(parameters[2], 0f), RTMath.GetOperation(parameters[0], MathOperation.Set));
                    obj.events[1].SetValues(x, y);
                }
            }

            /// <summary>
            /// Edits the rotation of an object.
            /// </summary>
            public class PrefabObjectRotationParameter : EditParameter<PrefabObject>
            {
                public override string Name => "rot";

                public override int ParameterCount => 2;

                public override string Description => "Rotation of the object.";

                public override string AddToAutocomplete => "rot set 0";

                public override void Apply(PrefabObject obj, string[] parameters) => RTMath.Operation(ref obj.events[2].values[0], Parser.TryParse(parameters[1], 0f), RTMath.GetOperation(parameters[0], MathOperation.Set));
            }

            /// <summary>
            /// Edits the depth of an object.
            /// </summary>
            public class PrefabObjectDepthParameter : EditParameter<PrefabObject>
            {
                public override string Name => "depth";

                public override int ParameterCount => 2;

                public override string Description => "Render depth of the object.";

                public override string AddToAutocomplete => "depth set 0";

                public override void Apply(PrefabObject obj, string[] parameters) => RTMath.Operation(ref obj.depth, Parser.TryParse(parameters[1], 0f), RTMath.GetOperation(parameters[0], MathOperation.Set));
            }

            #endregion

            #region Event Keyframe

            /// <summary>
            /// Edits the time of a keyframe.
            /// </summary>
            public class EventKeframeTimeParameter : EditParameter<EventKeyframe>
            {
                public override string Name => "time";

                public override int ParameterCount => 2;

                public override string Description => "Time of the keyframe.";

                public override string AddToAutocomplete => "time set 10";

                // set 10
                public override void Apply(EventKeyframe obj, string[] parameters)
                {
                    if (obj.timelineKeyframe && obj.timelineKeyframe.Index == 0)
                        return;

                    var operation = RTMath.GetOperation(parameters[0], MathOperation.Set);
                    var value = Parser.TryParse(parameters[1], 0f);
                    RTMath.Operation(ref obj.time, value, operation);
                }
            }

            /// <summary>
            /// Edits the value of a keyframe.
            /// </summary>
            public class EventKeyframeValueParameter : EditParameter<EventKeyframe>
            {
                public override string Name => "value";

                public override int ParameterCount => 3;

                public override string Description => "A value of the keyframe.";

                public override string AddToAutocomplete => "value 0 set 10";

                public override void Apply(EventKeyframe obj, string[] parameters)
                {
                    var valueIndex = Mathf.Clamp(Parser.TryParse(parameters[0], 0), 0, obj.values.Length - 1);
                    var operation = RTMath.GetOperation(parameters[1], MathOperation.Set);
                    var value = Parser.TryParse(parameters[2], 0f);
                    RTMath.Operation(ref obj.values[valueIndex], value, operation);
                }
            }

            /// <summary>
            /// Edits the random value of a keyframe.
            /// </summary>
            public class EventKeyframeRandomValueParameter : EditParameter<EventKeyframe>
            {
                public override string Name => "random_value";

                public override int ParameterCount => 3;

                public override string Description => "A value of the keyframe.";

                public override string AddToAutocomplete => "random_value 0 set 10";

                public override void Apply(EventKeyframe obj, string[] parameters)
                {
                    var valueIndex = Mathf.Clamp(Parser.TryParse(parameters[0], 0), 0, obj.randomValues.Length - 1);
                    var operation = RTMath.GetOperation(parameters[1], MathOperation.Set);
                    var value = Parser.TryParse(parameters[2], 0f);
                    RTMath.Operation(ref obj.randomValues[valueIndex], value, operation);
                }
            }

            /// <summary>
            /// Edits the string value of a keyframe.
            /// </summary>
            public class EventKeyframeStringValueParameter : EditParameter<EventKeyframe>
            {
                public override string Name => "string_value";

                public override int ParameterCount => 3;

                public override string Description => "A value of the keyframe.";

                public override string AddToAutocomplete => "string_value 0 set FFFFFF";

                public override void Apply(EventKeyframe obj, string[] parameters)
                {
                    if (obj.stringValues == null)
                        return;

                    var valueIndex = Mathf.Clamp(Parser.TryParse(parameters[0], 0), 0, obj.stringValues.Length - 1);
                    if (valueIndex < 0)
                        return;

                    var operation = parameters[1];
                    var value = parameters[2];
                    switch (operation)
                    {
                        case "set": {
                                obj.stringValues[valueIndex] = value;
                                break;
                            }
                        case "add": {
                                obj.stringValues[valueIndex] += value;
                                break;
                            }
                        case "sub": {
                                obj.stringValues[valueIndex] = obj.stringValues[valueIndex].Remove(value);
                                break;
                            }
                    }
                }
            }

            /// <summary>
            /// Edits the easing of a keyframe.
            /// </summary>
            public class EventKeyframeEasingParameter : EditParameter<EventKeyframe>
            {
                public override string Name => "easing";

                public override int ParameterCount => 1;

                public override string Description => "Ease / Curve Type of the keyframe.";

                public override string AddToAutocomplete => "easing linear";

                public override void Apply(EventKeyframe obj, string[] parameters) => obj.curve = Parser.TryParse(parameters[0], true, Easing.Linear);
            }

            /// <summary>
            /// Edits the random type of a keyframe.
            /// </summary>
            public class EventKeyframeRandomTypeParameter : EditParameter<EventKeyframe>
            {
                public override string Name => "random_type";

                public override int ParameterCount => 3;

                public override string Description => "The random type of the keyframe.";

                public override string AddToAutocomplete => "random_type set 1";

                public override void Apply(EventKeyframe obj, string[] parameters)
                {
                    var operation = RTMath.GetOperation(parameters[0], MathOperation.Set);
                    var value = Parser.TryParse(parameters[1], 0);
                    RTMath.Operation(ref obj.random, value, operation);
                }
            }

            /// <summary>
            /// Edits the relative of a keyframe.
            /// </summary>
            public class EventKeyframeRelativeParameter : EditParameter<EventKeyframe>
            {
                public override string Name => "relative";

                public override int ParameterCount => 1;

                public override string Description => "Relative / additive value of the keyframe.";

                public override string AddToAutocomplete => "relative true";

                public override void Apply(EventKeyframe obj, string[] parameters) => obj.relative = Parser.TryParse(parameters[0], false);
            }

            #endregion

            #region Marker

            /// <summary>
            /// Edits the name of a marker.
            /// </summary>
            public class MarkerNameParameter : EditParameter<Marker>
            {
                public override string Name => "name";

                public override int ParameterCount => 1;

                public override string Description => "Name of the marker.";

                public override string AddToAutocomplete => "name \"Marker Name\"";

                public override void Apply(Marker obj, string[] parameters) => obj.name = parameters[0];
            }

            /// <summary>
            /// Edits the time of a marker.
            /// </summary>
            public class MarkerTimeParameter : EditParameter<Marker>
            {
                public override string Name => "time";

                public override int ParameterCount => 1;

                public override string Description => "Time of the marker.";

                public override string AddToAutocomplete => "time 10";

                public override void Apply(Marker obj, string[] parameters) => obj.time = Parser.TryParse(parameters[0], 0f);
            }

            /// <summary>
            /// Edits the time of a marker.
            /// </summary>
            public class MarkerAddTimeParameter : EditParameter<Marker>
            {
                public override string Name => "add_time";

                public override int ParameterCount => 1;

                public override string Description => "Time of the marker.";

                public override string AddToAutocomplete => "add_time current_time 0.5";

                public override void Apply(Marker obj, string[] parameters) => obj.time = Parser.TryParse(parameters[0], 0f) + (Parser.TryParse(parameters[1], 0f) * index);
            }

            /// <summary>
            /// Edits the description of a marker.
            /// </summary>
            public class MarkerDescriptionParameter : EditParameter<Marker>
            {
                public override string Name => "desc";

                public override int ParameterCount => 1;

                public override string Description => "Description of the marker.";

                public override string AddToAutocomplete => "desc \"This is the default description!\"";

                public override void Apply(Marker obj, string[] parameters) => obj.desc = parameters[0];
            }

            /// <summary>
            /// Edits the color of a marker.
            /// </summary>
            public class MarkerColorParameter : EditParameter<Marker>
            {
                public override string Name => "color";

                public override int ParameterCount => 2;

                public override string Description => "Color of the marker.";

                public override string AddToAutocomplete => "color set 0";

                public override void Apply(Marker obj, string[] parameters) => RTMath.Operation(ref obj.color, Parser.TryParse(parameters[1], 0), RTMath.GetOperation(parameters[0], MathOperation.Set));
            }

            #endregion

            #region Checkpoint

            /// <summary>
            /// Edits the name of a checkpoint.
            /// </summary>
            public class CheckpointNameParameter : EditParameter<Checkpoint>
            {
                public override string Name => "name";

                public override int ParameterCount => 1;

                public override string Description => "Name of the checkpoint.";

                public override string AddToAutocomplete => "name \"Checkpoint Name\"";

                public override void Apply(Checkpoint obj, string[] parameters) => obj.name = parameters[0];
            }

            /// <summary>
            /// Edits the time of a checkpoint.
            /// </summary>
            public class CheckpointTimeParameter : EditParameter<Checkpoint>
            {
                public override string Name => "time";

                public override int ParameterCount => 1;

                public override string Description => "Time of the checkpoint.";

                public override string AddToAutocomplete => "time 10";

                public override void Apply(Checkpoint obj, string[] parameters) => obj.time = Parser.TryParse(parameters[0], 0f);
            }

            /// <summary>
            /// Edits the time of a checkpoint.
            /// </summary>
            public class CheckpointAddTimeParameter : EditParameter<Checkpoint>
            {
                public override string Name => "add_time";

                public override int ParameterCount => 1;

                public override string Description => "Time of the checkpoint.";

                public override string AddToAutocomplete => "add_time current_time 10";

                public override void Apply(Checkpoint obj, string[] parameters) => obj.time = Parser.TryParse(parameters[0], 0f) + (Parser.TryParse(parameters[1], 0f) * index);
            }

            #endregion

            #region Modifier

            /// <summary>
            /// Edits a value of a modifier.
            /// </summary>
            public class ModifierValueParameter : EditParameter<Modifier>
            {
                public override string Name => "value";

                public override int ParameterCount => 2;

                public override string Description => "A value of the modifier.";

                public override string AddToAutocomplete => "value 0 test";

                public override void Apply(Modifier obj, string[] parameters) => obj.SetValue(Parser.TryParse(parameters[0], 0), parameters[1]);
            }

            /// <summary>
            /// Edits the constant state of a modifier.
            /// </summary>
            public class ModifierConstantParameter : EditParameter<Modifier>
            {
                public override string Name => "constant";

                public override int ParameterCount => 1;

                public override string Description => "Constant state of the modifier.";

                public override string AddToAutocomplete => "constant true";

                public override void Apply(Modifier obj, string[] parameters) => obj.constant = Parser.TryParse(parameters[0], false);
            }

            /// <summary>
            /// Edits the run count of a modifier.
            /// </summary>
            public class ModifierRunCountParameter : EditParameter<Modifier>
            {
                public override string Name => "run_count";

                public override int ParameterCount => 2;

                public override string Description => "Amount of times the modifier should run.";

                public override string AddToAutocomplete => "run_count set 0";

                // run_count set 0
                // run_count add 1
                public override void Apply(Modifier obj, string[] parameters) => RTMath.Operation(ref obj.triggerCount, Parser.TryParse(parameters[1], 0), RTMath.GetOperation(parameters[0], MathOperation.Set));
            }

            /// <summary>
            /// Edits the prefab group only state of a modifier.
            /// </summary>
            public class ModifierPrefabGroupOnlyParameter : EditParameter<Modifier>
            {
                public override string Name => "prefab_group_only";

                public override int ParameterCount => 1;

                public override string Description => "If the modifier should only check for objects with the same prefab instance.";

                public override string AddToAutocomplete => "prefab_group_only true";

                public override void Apply(Modifier obj, string[] parameters)
                {
                    if (ModifiersHelper.IsGroupModifier(obj.Name))
                        obj.prefabInstanceOnly = Parser.TryParse(parameters[0], false);
                }
            }

            /// <summary>
            /// Edits the group alive state of a modifier.
            /// </summary>
            public class ModifierGroupAliveParameter : EditParameter<Modifier>
            {
                public override string Name => "group_alive";

                public override int ParameterCount => 1;

                public override string Description => "If the modifier should only check for alive objects.";

                public override string AddToAutocomplete => "group_alive true";

                public override void Apply(Modifier obj, string[] parameters)
                {
                    if (ModifiersHelper.IsGroupModifier(obj.Name))
                        obj.groupAlive = Parser.TryParse(parameters[0], false);
                }
            }

            /// <summary>
            /// Edits the not state of a modifier.
            /// </summary>
            public class ModifierNotParameter : EditParameter<Modifier>
            {
                public override string Name => "not";

                public override int ParameterCount => 1;

                public override string Description => "If the trigger check is inverted.";

                public override string AddToAutocomplete => "not true";

                public override void Apply(Modifier obj, string[] parameters)
                {
                    if (obj.type == Modifier.Type.Trigger)
                        obj.not = Parser.TryParse(parameters[0], false);
                }
            }

            /// <summary>
            /// Edits the else if state of a modifier.
            /// </summary>
            public class ModifierElseIfParameter : EditParameter<Modifier>
            {
                public override string Name => "else_if";

                public override int ParameterCount => 1;

                public override string Description => "If the trigger check can run if the previous triggers weren't active.";

                public override string AddToAutocomplete => "else_if true";

                public override void Apply(Modifier obj, string[] parameters)
                {
                    if (obj.type == Modifier.Type.Trigger)
                        obj.elseIf = Parser.TryParse(parameters[0], false);
                }
            }

            #endregion

            #region Annotation

            /// <summary>
            /// Edits the color of an annotation.
            /// </summary>
            public class AnnotationColorParameter : EditParameter<Annotation>
            {
                public override string Name => "color";

                public override int ParameterCount => 1;

                public override string Description => "Color of the annotation.";

                public override string AddToAutocomplete => "color 0";

                public override void Apply(Annotation obj, string[] parameters) => obj.color = Parser.TryParse(parameters[0], 0);
            }

            /// <summary>
            /// Edits the hex color of an annotation.
            /// </summary>
            public class AnnotationHexColorParameter : EditParameter<Annotation>
            {
                public override string Name => "hex_color";

                public override int ParameterCount => 1;

                public override string Description => "Color of the annotation.";

                public override string AddToAutocomplete => "hex_color ffffff";

                public override void Apply(Annotation obj, string[] parameters) => obj.hexColor = parameters[0];
            }

            /// <summary>
            /// Edits the opacity of an annotation.
            /// </summary>
            public class AnnotationOpacityParameter : EditParameter<Annotation>
            {
                public override string Name => "opacity";

                public override int ParameterCount => 1;

                public override string Description => "Opacity of the annotation.";

                public override string AddToAutocomplete => "opacity 1";

                public override void Apply(Annotation obj, string[] parameters) => obj.opacity = Parser.TryParse(parameters[0], 4f);
            }

            /// <summary>
            /// Edits the thickness of an annotation.
            /// </summary>
            public class AnnotationThicknessParameter : EditParameter<Annotation>
            {
                public override string Name => "thickness";

                public override int ParameterCount => 1;

                public override string Description => "Thickness of the annotation.";

                public override string AddToAutocomplete => "thickness 4";

                public override void Apply(Annotation obj, string[] parameters) => obj.thickness = Parser.TryParse(parameters[0], 4f);
            }

            /// <summary>
            /// Edits the fixed camera state of an annotation.
            /// </summary>
            public class AnnotationFixedCameraParameter : EditParameter<Annotation>
            {
                public override string Name => "fixed_camera";

                public override int ParameterCount => 1;

                public override string Description => "Fixed Camera state of the annotation.";

                public override string AddToAutocomplete => "fixed_camera true";

                public override void Apply(Annotation obj, string[] parameters) => obj.fixedCamera = Parser.TryParse(parameters[0], true);
            }

            /// <summary>
            /// Edits the hidden state of an annotation.
            /// </summary>
            public class AnnotationHiddenParameter : EditParameter<Annotation>
            {
                public override string Name => "hidden";

                public override int ParameterCount => 1;

                public override string Description => "Hidden state of the annotation.";

                public override string AddToAutocomplete => "hidden true";

                public override void Apply(Annotation obj, string[] parameters) => obj.hidden = Parser.TryParse(parameters[0], true);
            }

            /// <summary>
            /// Moves an annotation.
            /// </summary>
            public class AnnotationMoveParameter : EditParameter<Annotation>
            {
                public override string Name => "move";

                public override int ParameterCount => 2;

                public override string Description => "Moves the points of the annotation.";

                public override string AddToAutocomplete => "move 0 0";

                public override void Apply(Annotation obj, string[] parameters)
                {
                    var pos = new Vector2(Parser.TryParse(parameters[0], 0f), Parser.TryParse(parameters[1], 0f));
                    for (int i = 0; i < obj.points.Count; i++)
                        obj.points[i] += pos;
                }
            }

            /// <summary>
            /// Scales an annotation.
            /// </summary>
            public class AnnotationScaleParameter : EditParameter<Annotation>
            {
                public override string Name => "scale";

                public override int ParameterCount => 2;

                public override string Description => "Scales the points of the annotation.";

                public override string AddToAutocomplete => "scale 1 1";

                public override void Apply(Annotation obj, string[] parameters)
                {
                    var sca = new Vector2(Parser.TryParse(parameters[0], 0f), Parser.TryParse(parameters[1], 0f));
                    for (int i = 0; i < obj.points.Count; i++)
                        obj.points[i] = RTMath.Scale(obj.points[i], sca);
                }
            }

            /// <summary>
            /// Rotates an annotation.
            /// </summary>
            public class AnnotationRotateParameter : EditParameter<Annotation>
            {
                public override string Name => "rotate";

                public override int ParameterCount => 1;

                public override string Description => "Rotates the points of the annotation.";

                public override string AddToAutocomplete => "rotate 0";

                public override void Apply(Annotation obj, string[] parameters)
                {
                    var rot = Parser.TryParse(parameters[0], 0f);
                    for (int i = 0; i < obj.points.Count; i++)
                        obj.points[i] = RTMath.Rotate(obj.points[i], rot);
                }
            }

            #endregion

            #endregion
        }

        /// <summary>
        /// Represents a command capable of creating objects.
        /// </summary>
        public class CreateCommand : EditCommand
        {
            #region Values

            public override string Name => "create";

            public override bool Usable => ProjectArrhythmia.State.InEditor;

            public override string Pattern => "create category [values]";

            public override string Description => "Creates an object. Only available in the editor.";

            #endregion

            #region Functions

            public override void ConsumeInput(string input, string[] split)
            {
                CurrentCategory = Parser.TryParse(split[1].ToLower().Remove("_"), true, CategoryType.Null);
                count = Parser.TryParse(split[2], 1);
                if (count <= 0)
                    return;
                (int, int) command = (0, 0);
                List<ISelectable> selectables = new List<ISelectable>();
                switch (CurrentCategory)
                {
                    case CategoryType.BeatmapObject: {
                            for (index = 0; index < count; index++)
                            {
                                var beatmapObject = new BeatmapObject();
                                beatmapObject.InitDefaultEvents();
                                command = Apply(3, beatmapObject, split, beatmapObjectParameters);
                                GameData.Current.beatmapObjects.Add(beatmapObject);
                                EditorTimeline.inst.RenderTimelineObject(EditorTimeline.inst.GetTimelineObject(beatmapObject));
                                RTLevel.Current?.UpdateObject(beatmapObject);
                                selectables.Add(beatmapObject.timelineObject);
                            }
                            break;
                        }
                    case CategoryType.BackgroundObject: {
                            for (index = 0; index < count; index++)
                            {
                                var backgroundObject = new BackgroundObject();
                                command = Apply(3, backgroundObject, split, backgroundObjectParameters);
                                GameData.Current.backgroundObjects.Add(backgroundObject);
                                EditorTimeline.inst.RenderTimelineObject(EditorTimeline.inst.GetTimelineObject(backgroundObject));
                                selectables.Add(backgroundObject.timelineObject);
                            }
                            RTLevel.Current?.UpdateBackgroundObjects();
                            break;
                        }
                    case CategoryType.PrefabObject: {
                            int splitIndex = 3;
                            var prefabName = GetQuoteParameter(split, ref splitIndex);
                            if (!GameData.Current.prefabs.TryFind(x => x.name == prefabName, out Prefab prefab))
                                break;
                            for (index = 0; index < count; index++)
                            {
                                var prefabObject = new PrefabObject();
                                prefabObject.SetDefaultTransformOffsets();
                                prefabObject.prefabID = prefab.id;
                                command = Apply(splitIndex, prefabObject, split, prefabObjectParameters);
                                GameData.Current.prefabObjects.Add(prefabObject);
                                EditorTimeline.inst.RenderTimelineObject(EditorTimeline.inst.GetTimelineObject(prefabObject));
                                RTLevel.Current?.UpdatePrefab(prefabObject);
                                selectables.Add(prefabObject.timelineObject);
                            }
                            break;
                        }
                    case CategoryType.Marker: {
                            var markers = new List<Marker>();
                            for (index = 0; index < count; index++)
                            {
                                var marker = new Marker();
                                command = Apply(3, marker, split, markerParameters);
                                GameData.Current.data.markers.Add(marker);
                                markers.Add(marker);
                            }
                            RTMarkerEditor.inst.CreateMarkers();
                            selectables.AddRange(markers.Select(x => x.timelineMarker));
                            break;
                        }
                    case CategoryType.Checkpoint: {
                            var checkpoints = new List<Checkpoint>();
                            for (index = 0; index < count; index++)
                            {
                                var checkpoint = new Checkpoint();
                                command = Apply(3, checkpoint, split, checkpointParameters);
                                if (checkpoint.time <= 0f)
                                    continue;
                                GameData.Current.data.checkpoints.Add(checkpoint);
                                checkpoints.Add(checkpoint);
                            }
                            RTCheckpointEditor.inst.UpdateCheckpointTimeline();
                            selectables.AddRange(checkpoints.Select(x => x.timelineCheckpoint));
                            break;
                        }
                }
                SwitchMode(command.Item1, command.Item2, input, split, selectables);
            }

            #endregion
        }

        /// <summary>
        /// Represents a command for selecting objects.
        /// </summary>
        public class SelectCommand : ExampleCommand
        {
            #region Values

            public override string Name => "select";

            public override bool Usable => ProjectArrhythmia.State.InEditor;

            public override string Pattern => "select type [predicates]";

            public override string Description => "Provides selecting of many different types of objects. Only available in the editor.";

            /// <summary>
            /// The current selectable type.
            /// </summary>
            public static SelectableType CurrentType { get; set; }

            /// <summary>
            /// Collection of current selectables.
            /// </summary>
            public static IEnumerable<ISelectable> selectables;

            /// <summary>
            /// List of get parameters.
            /// </summary>
            public List<GetSelectableParameter> parameters = new List<GetSelectableParameter>
            {
                #region General

                new DefaultSelectablesParameter(),
                new SelectedParameter(),
                new TimeComparisonParameter(),
                new IndexComparisonParameter(),
                new LockedParameter(),
                new NameEqualsParameter(),
                new NameContainsParameter(),
                new NameRegexParameter(),
                new DescriptionRegexParameter(),
                new ContainsTagParameter(),

                #endregion

                #region Timeline Object

                new CurrentLayerParameter(),
                new SamePrefabGroupParameter(),
                new EditorGroupEqualsParameter(),
                new ReferenceTypeEqualsParameter(),
                new LayerComparisonParameter(),
                new BinComparisonParameter(),
                new CollapsedParameter(),
                new HiddenParameter(),
                new ObjectTypeParameter(),
                new AutokillTypeParameter(),

                #endregion

                #region Timeline Marker

                new ColorEqualsParameter(),
                new MarkerHasAnnotationsParameter(),

                #endregion

                #region Timeline Keyframe

                new EventTypeComparisonParameter(),
                new EventCoordParameter(),
                new EventValueComparisonParameter(),
                new EaseTypeEqualsParameter(),

                #endregion

                #region Timeline Checkpoint

                new CheckpointRespawnParameter(),
                new CheckpointHealParameter(),
                new CheckpointSetTimeParameter(),
                new CheckpointReverseParameter(),
                new CheckpointAutoTriggerableParameter(),

                #endregion

                #region Level

                new MetaDataValueEquals(),

                #endregion

                #region Modifier

                new ContainsModifierValue(),

                #endregion

                #region Annotations

                new AnnotationColorComparisonParameter(),
                new AnnotationHexColorEqualsParameter(),
                new AnnotationOpacityComparisonParameter(),
                new AnnotationThicknessComparisonParameter(),
                new AnnotationHiddenParameter(),
                new AnnotationFixedCameraParameter(),

                #endregion
            };

            /// <summary>
            /// List of action parameters.
            /// </summary>
            public List<ActionParameter> actionParameters = new List<ActionParameter>
            {
                #region General

                new LogSelectedParameter(),
                new NotifyCountParameter(),
                new SetNameParameter(),
                new ReplaceNameParameter(),

                #endregion

                #region Timeline Object
                
                new MirrorObjectParameter(),
                new SetCollapseParameter(),
                new SwapCollapseParameter(),
                new SetHiddenParameter(),
                new SwapHiddenParameter(),

                new CopyTimelineObjectParameter(),
                new DuplicateTimelineObjectParameter(),
                new DeleteTimelineObjectParameter(),
                new UpdateParameter(),

                #endregion

                #region Timeline Marker

                new SetMarkerColorParameter(),

                new SetMarkerLayerParameter(),
                new RemoveMarkerLayerParameter(),
                new ClearMarkerLayersParameter(),

                new MoveMarkerAnnotations(),
                new ScaleMarkerAnnotations(),
                new RotateMarkerAnnotations(),
                new ClearMarkerAnnotations(),
                new DrawBoxMarkerAnnotation(),
                new DrawShapeMarkerAnnotation(),
                new DrawLineMarkerAnnotation(),

                #endregion

                #region Beatmap Object
                
                new ShowObjectListParameter(),

                #endregion

                #region Level

                new OpenFirstParameter(),
                new OpenLastParameter(),
                new CombineLevelParameter(),
                new CreateLevelCollectionParameter(),

                #endregion

                #region Prefab

                new ImportPrefabParameter(),
                new ImportPrefabOnceParameter(),
                new ImportPrefabUpdateParameter(),
                new CreatePrefabParameter(),

                #endregion

                #region Modifier

                new SetPrefabGroupOnlyParameter(),
                new SwapPrefabGroupOnlyParameter(),

                #endregion
            };

            /// <summary>
            /// List of orderby parameters.
            /// </summary>
            public List<GetSelectableParameter> orderByParameters = new List<GetSelectableParameter>
            {
                new OrderByParameter<string>("name", "Orders by object name.", selectable => GetName(selectable)),
            };

            #endregion

            #region Functions

            public override void ConsumeInput(string input, string[] split) => DoSelection(GetSelectablesFromInput(input, split));

            /// <summary>
            /// Selects the objects and renders the editor.
            /// </summary>
            /// <param name="selectables">Collection of selectables.</param>
            public void DoSelection(IEnumerable<ISelectable> selectables)
            {
                if (selectables == null)
                    return;

                // Clear the current selectables.
                var s = GetSelectables(CurrentType);
                if (s != null)
                    foreach (var selectable in s)
                        selectable.Selected = false;
                // Select the new selectables.
                foreach (var selectable in selectables)
                    selectable.Selected = true;
                // Render the editor.
                switch (CurrentType)
                {
                    case SelectableType.Objects: {
                            EditorTimeline.inst.HandleSelection(EditorTimeline.inst.SelectedObjects);
                            break;
                        }
                    case SelectableType.Keyframes: {
                            RTEventEditor.inst.RenderDialog();
                            break;
                        }
                    case SelectableType.ObjectKeyframes: {
                            ObjectEditor.inst.Dialog.Timeline.RenderDialog(EditorTimeline.inst.CurrentSelection.GetData<BeatmapObject>());
                            break;
                        }
                    case SelectableType.Markers: {
                            RTMarkerEditor.inst.RenderDialog();
                            break;
                        }
                    case SelectableType.Checkpoints: {
                            RTCheckpointEditor.inst.RenderDialog();
                            break;
                        }
                }
            }

            /// <summary>
            /// Gets a collection of selectables based on the input command. Allows for switching to edit mode with "edit" or action mode with "->".
            /// </summary>
            /// <param name="input">Input command.</param>
            /// <param name="split">Array of parameters.</param>
            /// <param name="actionMode">The default action mode.</param>
            /// <param name="baseSelectables">Collection of default selectables.</param>
            /// <returns>Returns a collection of selectables.</returns>
            public IEnumerable<ISelectable> GetSelectablesFromInput(string input, string[] split, bool actionMode = false, IEnumerable<ISelectable> baseSelectables = null)
            {
                CurrentType = Parser.TryParse(split[1].ToLower().Remove("_"), true, SelectableType.Null);
                if (CurrentType != SelectableType.Null)
                    selectables = GetSelectables(CurrentType);
                else
                    selectables = baseSelectables;
                if (selectables == null)
                    return baseSelectables ?? selectables;
                for (int i = 2; i < split.Length; i++)
                {
                    var s = split[i];
                    bool not = false;
                    if (s.StartsWith("!"))
                    {
                        not = true;
                        s = s.TrimStart('!');
                    }

                    // switch to action mode.
                    if (s == "->")
                    {
                        // convert rest of parameters to action parameters
                        actionMode = true;
                        continue;
                    }

                    // switch back to selectable mode.
                    if (s == "if")
                    {
                        actionMode = false;
                        continue;
                    }

                    // gets a new selection.
                    if (s == "select")
                    {
                        actionMode = false;
                        i++;
                        if (i < split.Length)
                        {
                            var previousType = CurrentType;
                            CurrentType = Parser.TryParse(split[i].ToLower().Remove("_"), true, SelectableType.Null);
                            if ((previousType == SelectableType.ExternalPrefabs || previousType == SelectableType.InternalPrefabs) && CurrentType == SelectableType.Objects)
                                selectables = GetPrefabPackageObjects(selectables);
                            else if (CurrentType == SelectableType.Modifiers)
                                selectables = GetModifiers(selectables);
                            else if (CurrentType == SelectableType.ObjectKeyframes)
                                selectables = GetObjectKeyframes(selectables);
                            else if (CurrentType == SelectableType.Annotations)
                                selectables = GetMarkerAnnotations(selectables);
                            else
                                selectables = GetSelectables(CurrentType);
                        }
                        continue;
                    }

                    // combines the current selection with another selection.
                    if (s == "union")
                    {
                        actionMode = false;
                        i++;
                        if (i >= split.Length)
                            break;
                        s = split[i];
                        var nextSelectables = GetSelectables(CurrentType);
                        if (parameters.TryFind(x => x.Name == s && (x.RequiredSelectionType == SelectableType.Null || x.RequiredSelectionType == CurrentType), out GetSelectableParameter parameter))
                            nextSelectables = parameter.GetSelectables(nextSelectables, parameter.GetParameters(split, ref i), not);
                        selectables = selectables.Union(nextSelectables);
                        continue;
                    }

                    // switch to edit mode.
                    if (s == "edit")
                    {
                        new EditCommand().ConsumeInput(input, split.Range(i + 1, split.Length - 1).ToArray(), CurrentType, selectables);
                        selectables = null;
                        return selectables;
                    }

                    // orders the selection.
                    if (s == "orderby")
                    {
                        i++;
                        if (i < split.Length && orderByParameters.TryFind(x => x.Name == split[i], out GetSelectableParameter parameter))
                            selectables = parameter.GetSelectables(selectables, parameter.GetParameters(split, ref i), not);
                        continue;
                    }

                    //if (!actionMode && s == "or" && previousSelectables != null && !previousSelectables.IsEmpty())
                    //{
                    //    i++;
                    //    if (i >= split.Length)
                    //        break;
                    //    s = split[i];
                    //    if (parameters.TryFind(x => x.Name == s && (x.RequiredSelectionType == SelectableType.Null || x.RequiredSelectionType == CurrentType), out GetSelectableParameter parameter))
                    //        selectables = parameter.GetSelectables(selectables, parameter.GetParameters(split, ref i));
                    //    continue;
                    //}

                    if (actionMode)
                    {
                        if (actionParameters.TryFind(x => x.Name == s && (x.RequiredSelectionType == SelectableType.Null || x.RequiredSelectionType == CurrentType), out ActionParameter actionParameter))
                            actionParameter.Run(selectables, actionParameter.GetParameters(split, ref i));
                    }
                    else if (parameters.TryFind(x => x.Name == s && (x.RequiredSelectionType == SelectableType.Null || x.RequiredSelectionType == CurrentType), out GetSelectableParameter parameter))
                        selectables = parameter.GetSelectables(selectables, parameter.GetParameters(split, ref i), not);
                }
                if (actionMode) // since we're performing an action on specific objects, we don't select them.
                    return null;
                return selectables;
            }

            public override IEnumerable<ParameterBase> GetParameters()
            {
                foreach (var parameter in parameters)
                    yield return parameter;
                var values = EnumHelper.GetValues<SelectableType>();
                foreach (var value in values)
                {
                    if (value != SelectableType.Null)
                        yield return new TypeEnumParameter<SelectableType>(value, "Selection type to filter.");
                }
            }

            /// <summary>
            /// Gets the default selectable collection.
            /// </summary>
            /// <param name="type">Type of the selectable to get.</param>
            /// <returns>Returns the default selectable collection.</returns>
            public static IEnumerable<ISelectable> GetSelectables(SelectableType type) => type switch
            {
                SelectableType.Objects => EditorTimeline.inst.timelineObjects,
                SelectableType.Keyframes => EditorTimeline.inst.timelineKeyframes,
                SelectableType.ObjectKeyframes => EditorTimeline.inst.CurrentSelection.TryGetData(out BeatmapObject beatmapObject) ? beatmapObject.TimelineKeyframes : null,
                SelectableType.Markers => RTMarkerEditor.inst.timelineMarkers,
                SelectableType.Checkpoints => RTCheckpointEditor.inst.timelineCheckpoints,
                SelectableType.Levels => EditorLevelManager.inst.LevelPanels,
                SelectableType.LevelCollections => EditorLevelManager.inst.LevelCollectionPanels,
                SelectableType.ExternalPrefabs => RTPrefabEditor.inst.PrefabPanels.Where(x => x.IsExternal),
                SelectableType.InternalPrefabs => RTPrefabEditor.inst.PrefabPanels.Where(x => !x.IsExternal),
                SelectableType.ExternalThemes => RTThemeEditor.inst.ExternalThemePanels,
                SelectableType.InternalThemes => RTThemeEditor.inst.InternalThemePanels,
                SelectableType.PlayerModels => PlayerEditor.inst.ModelPanels,
                SelectableType.FolderPlanners => ProjectPlanner.inst.folders,
                SelectableType.DocumentPlanners => ProjectPlanner.inst.documents,
                SelectableType.CharacterPlanners => ProjectPlanner.inst.characters,
                SelectableType.TimelinePlanners => ProjectPlanner.inst.timelines,
                SelectableType.SchedulePlanners => ProjectPlanner.inst.schedules,
                SelectableType.NotePlanners => ProjectPlanner.inst.notes,
                SelectableType.OSTPlanners => ProjectPlanner.inst.osts,
                SelectableType.Modifiers => ModifiersEditorDialog.Current.modifierCards,
                SelectableType.Annotations => RTMarkerEditor.inst.timelineMarkers.Where(x => x.Selected).Select(x => x.Marker.annotations).Select(x => x as ISelectable),
                _ => null,
            };

            /// <summary>
            /// Gets objects inside of a prefab.
            /// </summary>
            /// <param name="selectables">Collection of selectables. Must be <see cref="PrefabPanel"/>.</param>
            /// <returns>Returns a collection of selectables from within prefabs.</returns>
            public static IEnumerable<ISelectable> GetPrefabPackageObjects(IEnumerable<ISelectable> selectables)
            {
                foreach (var selectable in selectables)
                {
                    if (selectable is PrefabPanel prefabPanel && prefabPanel.Item)
                    {
                        foreach (var beatmapObject in prefabPanel.Item.beatmapObjects)
                            yield return new TimelineObject(beatmapObject);
                        foreach (var backgroundObject in prefabPanel.Item.backgroundObjects)
                            yield return new TimelineObject(backgroundObject);
                        foreach (var prefabObject in prefabPanel.Item.prefabObjects)
                            yield return new TimelineObject(prefabObject);
                    }
                }
            }

            /// <summary>
            /// Gets modifiers.
            /// </summary>
            /// <param name="selectables">Collection of selectables. Must be <see cref="TimelineObject"/> and <see cref="IModifyable"/>.</param>
            /// <returns>Returns a collection of modifiers.</returns>
            public static IEnumerable<ISelectable> GetModifiers(IEnumerable<ISelectable> selectables)
            {
                foreach (var selectable in selectables)
                {
                    if (selectable is TimelineObject timelineObject && timelineObject.TryGetData(out IModifyable modifyable))
                        foreach (var modifier in modifyable.Modifiers)
                        {
                            if (!modifier.card)
                                modifier.card = new ModifierCard(modifier);
                            yield return modifier.card;
                        }
                }
            }

            /// <summary>
            /// Gets object keyframes.
            /// </summary>
            /// <param name="selectables">Collection of selectables. Must be <see cref="TimelineObject"/> and <see cref="BeatmapObject"/>.</param>
            /// <returns>Returns a collection of object keyframes.</returns>
            public static IEnumerable<ISelectable> GetObjectKeyframes(IEnumerable<ISelectable> selectables)
            {
                foreach (var selectable in selectables)
                {
                    if (selectable is not TimelineObject timelineObject || !timelineObject.TryGetData(out BeatmapObject beatmapObject))
                        continue;

                    for (int type = 0; type < beatmapObject.events.Count; type++)
                    {
                        for (int index = 0; index < beatmapObject.events[type].Count; index++)
                        {
                            var eventKeyframe = beatmapObject.events[type][index];
                            if (!eventKeyframe.timelineKeyframe)
                            {
                                eventKeyframe.timelineKeyframe = new TimelineKeyframe(eventKeyframe);
                                eventKeyframe.timelineKeyframe.SetCoord(new KeyframeCoord(type, index));
                            }
                            yield return eventKeyframe.timelineKeyframe;
                        }
                    }
                }
            }

            /// <summary>
            /// Gets annotations.
            /// </summary>
            /// <param name="selectables">Collection of selectables. Must be <see cref="TimelineMarker"/>.</param>
            /// <returns>Returns a collection of annotations.</returns>
            public static IEnumerable<ISelectable> GetMarkerAnnotations(IEnumerable<ISelectable> selectables)
            {
                foreach (var selectable in selectables)
                {
                    if (selectable is not TimelineMarker timelineMarker)
                        continue;

                    for (int i = 0; i < timelineMarker.Marker.annotations.Count; i++)
                        yield return timelineMarker.Marker.annotations[i];
                }
            }

            /// <summary>
            /// Gets the name of a selectable object.
            /// </summary>
            /// <param name="selectable">Selectable object reference. Must have a name value.</param>
            /// <returns>Returns the name of a selectable object.</returns>
            public static string GetName(ISelectable selectable)
            {
                if (selectable is TimelineObject timelineObject)
                    return timelineObject.Name;
                if (selectable is TimelineMarker timelineMarker)
                    return timelineMarker.Name;
                if (selectable is TimelineCheckpoint timelineCheckpoint)
                    return timelineCheckpoint.Name;
                if (selectable is LevelPanel levelPanel)
                    return levelPanel.isFolder ? levelPanel.Name : levelPanel.Item?.metadata?.beatmap?.name;
                if (selectable is LevelCollectionPanel levelCollectionPanel)
                    return levelCollectionPanel.isFolder ? levelCollectionPanel.Name : levelCollectionPanel.Item.name;
                if (selectable is PrefabPanel prefabPanel)
                    return prefabPanel.isFolder ? prefabPanel.Name : prefabPanel.Item.name;
                if (selectable is ThemePanel themePanel)
                    return themePanel.isFolder ? themePanel.Name : themePanel.Item.name;
                if (selectable is PlayerModelPanel playerModelPanel)
                    return playerModelPanel.isFolder ? playerModelPanel.Name : playerModelPanel.Item.basePart.name;
                if (selectable is FolderPlanner folderPlanner)
                    return folderPlanner.Name;
                if (selectable is DocumentPlanner documentPlanner)
                    return documentPlanner.Name;
                if (selectable is CharacterPlanner characterPlanner)
                    return characterPlanner.Name;
                if (selectable is TimelinePlanner timelinePlanner)
                    return timelinePlanner.Name;
                if (selectable is NotePlanner notePlanner)
                    return notePlanner.Name;
                if (selectable is OSTPlanner ostPlanner)
                    return ostPlanner.Name;
                if (selectable is ModifierCard modifierCard && modifierCard.Modifier)
                    return modifierCard.Modifier.Name;
                return null;
            }

            /// <summary>
            /// Sets the name of a selectable object.
            /// </summary>
            /// <param name="selectable">Selectable object reference. Must have a name value.</param>
            /// <param name="name">Name to set.</param>
            public static void SetName(ISelectable selectable, string name)
            {
                if (selectable is TimelineObject timelineObject)
                {
                    timelineObject.SetName(name);
                    return;
                }
                if (selectable is TimelineMarker timelineMarker)
                {
                    timelineMarker.Name = name;
                    return;
                }
                if (selectable is TimelineCheckpoint timelineCheckpoint)
                {
                    timelineCheckpoint.Name = name;
                    return;
                }
                if (selectable is LevelPanel levelPanel && levelPanel.Item && levelPanel.Item.metadata)
                {
                    levelPanel.Item.metadata.beatmap.name = name;
                    levelPanel.Render();
                    return;
                }
                if (selectable is LevelCollectionPanel levelCollectionPanel && levelCollectionPanel.Item)
                {
                    levelCollectionPanel.Item.name = name;
                    levelCollectionPanel.Render();
                    return;
                }
                if (selectable is PrefabPanel prefabPanel && prefabPanel.Item && !prefabPanel.IsExternal)
                {
                    prefabPanel.Item.name = name;
                    RTPrefabEditor.inst.ValidateDuplicateName(prefabPanel.Item);
                    prefabPanel.Render();
                    return;
                }
                if (selectable is ThemePanel themePanel && themePanel.Item && themePanel.Source == ObjectSource.Internal)
                {
                    themePanel.Item.name = name;
                    themePanel.Render();
                    return;
                }
                if (selectable is PlayerModelPanel playerModelPanel && playerModelPanel.Item)
                {
                    playerModelPanel.Item.basePart.name = name;
                    playerModelPanel.Render();
                    return;
                }
                if (selectable is FolderPlanner folderPlanner)
                {
                    folderPlanner.Name = name;
                    folderPlanner.Render();
                    return;
                }
                if (selectable is DocumentPlanner documentPlanner)
                {
                    documentPlanner.Name = name;
                    documentPlanner.Render();
                    return;
                }
                if (selectable is CharacterPlanner characterPlanner)
                {
                    characterPlanner.Name = name;
                    characterPlanner.Render();
                    return;
                }
                if (selectable is TimelinePlanner timelinePlanner)
                {
                    timelinePlanner.Name = name;
                    timelinePlanner.Render();
                    return;
                }
                if (selectable is NotePlanner notePlanner)
                {
                    notePlanner.Name = name;
                    notePlanner.Render();
                    return;
                }
                if (selectable is OSTPlanner ostPlanner)
                {
                    ostPlanner.Name = name;
                    ostPlanner.Render();
                    return;
                }
            }

            /// <summary>
            /// Gets the description of a selectable object.
            /// </summary>
            /// <param name="selectable">Selectable object reference. Must have a description value.</param>
            /// <returns>Returns the description of a selectable object.</returns>
            public static string GetDescription(ISelectable selectable)
            {
                if (selectable is TimelineMarker timelineMarker)
                    return timelineMarker.Description;
                if (selectable is LevelPanel levelPanel)
                    return levelPanel.Item?.metadata?.song?.description;
                if (selectable is LevelCollectionPanel levelCollectionPanel)
                    return levelCollectionPanel.Item?.description;
                if (selectable is PrefabPanel prefabPanel)
                    return prefabPanel.Item?.description;
                if (selectable is PlayerModelPanel playerModelPanel)
                    return playerModelPanel.Item?.basePart?.name;
                if (selectable is DocumentPlanner documentPlanner)
                    return documentPlanner.Text;
                if (selectable is CharacterPlanner characterPlanner)
                    return characterPlanner.Description;
                if (selectable is NotePlanner notePlanner)
                    return notePlanner.Text;
                if (selectable is ModifierCard modifierCard && modifierCard.Modifier)
                    return modifierCard.Modifier.description;
                return null;
            }

            #endregion

            #region Sub Classes

            /// <summary>
            /// Type of a selectable.
            /// </summary>
            public enum SelectableType
            {
                /// <summary>
                /// Invalid type.
                /// </summary>
                Null,
                /// <summary>
                /// Represents <see cref="TimelineObject"/>.
                /// </summary>
                Objects,
                /// <summary>
                /// Represents <see cref="EventKeyframe"/> in the main timeline.
                /// </summary>
                Keyframes,
                /// <summary>
                /// Represents <see cref="EventKeyframe"/> in an object.
                /// </summary>
                ObjectKeyframes,
                /// <summary>
                /// Represents <see cref="Marker"/>.
                /// </summary>
                Markers,
                /// <summary>
                /// Represents <see cref="Checkpoint"/>.
                /// </summary>
                Checkpoints,
                /// <summary>
                /// Represents <see cref="Level"/>.
                /// </summary>
                Levels,
                /// <summary>
                /// Represents <see cref="LevelCollection"/>.
                /// </summary>
                LevelCollections,
                /// <summary>
                /// Represents an external <see cref="Prefab"/>.
                /// </summary>
                ExternalPrefabs,
                /// <summary>
                /// Represents an internal <see cref="Prefab"/>.
                /// </summary>
                InternalPrefabs,
                /// <summary>
                /// Represents an external <see cref="BeatmapTheme"/>.
                /// </summary>
                ExternalThemes,
                /// <summary>
                /// Represents an internal <see cref="BeatmapTheme"/>.
                /// </summary>
                InternalThemes,
                /// <summary>
                /// Represents <see cref="Core.Data.Player.PlayerModel"/>.
                /// </summary>
                PlayerModels,
                /// <summary>
                /// Represents <see cref="FolderPlanner"/>.
                /// </summary>
                FolderPlanners,
                /// <summary>
                /// Represents <see cref="DocumentPlanner"/>.
                /// </summary>
                DocumentPlanners,
                /// <summary>
                /// Represents <see cref="TODOPlanner"/>.
                /// </summary>
                TODOPlanners,
                /// <summary>
                /// Represents <see cref="CharacterPlanner"/>.
                /// </summary>
                CharacterPlanners,
                /// <summary>
                /// Represents <see cref="TimelinePlanner"/>.
                /// </summary>
                TimelinePlanners,
                /// <summary>
                /// Represents <see cref="SchedulePlanner"/>.
                /// </summary>
                SchedulePlanners,
                /// <summary>
                /// Represents <see cref="NotePlanner"/>.
                /// </summary>
                NotePlanners,
                /// <summary>
                /// Represents <see cref="OSTPlanner"/>.
                /// </summary>
                OSTPlanners,
                /// <summary>
                /// Represents <see cref="Modifier"/>.
                /// </summary>
                Modifiers,
                /// <summary>
                /// Represents <see cref="Annotation"/>.
                /// </summary>
                Annotations,
            }

            /// <summary>
            /// Represents a selectable filter.
            /// </summary>
            public abstract class GetSelectableParameter : ParameterBase
            {
                /// <summary>
                /// Required selection type. If it's <see cref="SelectableType.Null"/> then the parameter can be used for any type.
                /// </summary>
                public virtual SelectableType RequiredSelectionType => SelectableType.Null;

                /// <summary>
                /// Filters the collection of selectables.
                /// </summary>
                /// <param name="selectables">Current collection of selectables.</param>
                /// <param name="parameters">Array of parameters.</param>
                /// <param name="not">If the filter should be a not gate.</param>
                /// <returns>Returns the filtered collection of selectables.</returns>
                public abstract IEnumerable<ISelectable> GetSelectables(IEnumerable<ISelectable> selectables, string[] parameters, bool not);

                /// <summary>
                /// Inverts a check if <paramref name="not"/> is true.
                /// </summary>
                /// <param name="not">If the check should be a not gate.</param>
                /// <param name="value">Check.</param>
                /// <returns>Returns <paramref name="value"/>.</returns>
                public bool NotCheck(bool not, bool value) => not ? !value : value;
            }

            /// <summary>
            /// Represents a function that can be run on a collection of selectables.
            /// </summary>
            public abstract class ActionParameter : ParameterBase
            {
                /// <summary>
                /// Required selection type. If it's <see cref="SelectableType.Null"/> then the parameter can be used for any type.
                /// </summary>
                public virtual SelectableType RequiredSelectionType => SelectableType.Null;

                /// <summary>
                /// Runs an action on a collection of selectables.
                /// </summary>
                /// <param name="selectables">Collection of selectables.</param>
                /// <param name="parameters">Array of parameters.</param>
                public abstract void Run(IEnumerable<ISelectable> selectables, string[] parameters);
            }

            /// <summary>
            /// Represents a way to order a collection of selectables.
            /// </summary>
            /// <typeparam name="TKey">Type of the selectable to order.</typeparam>
            public class OrderByParameter<TKey> : GetSelectableParameter
            {
                #region Constructors

                public OrderByParameter() { }

                public OrderByParameter(string name, string description, Func<ISelectable, TKey> keySelector)
                {
                    this.name = name;
                    this.description = description;
                    this.keySelector = keySelector;
                }
                
                public OrderByParameter(string name, string description, string addToAutocomplete, Func<ISelectable, TKey> keySelector)
                {
                    this.name = name;
                    this.description = description;
                    this.addToAutocomplete = addToAutocomplete;
                    this.keySelector = keySelector;
                }

                #endregion

                #region Values

                public override string Name => name;

                public override string Description => description;

                public override string AddToAutocomplete => addToAutocomplete ?? base.AddToAutocomplete;

                string name;

                string description;

                string addToAutocomplete;

                Func<ISelectable, TKey> keySelector;

                #endregion

                #region Functions

                public override IEnumerable<ISelectable> GetSelectables(IEnumerable<ISelectable> selectables, string[] parameters, bool not) => Parser.TryParse(parameters[0], true) ? selectables.OrderBy(keySelector) : selectables.OrderByDescending(keySelector);

                #endregion
            }

            #region Get

            #region General

            /// <summary>
            /// Gets the default collection of selectables.
            /// </summary>
            public class DefaultSelectablesParameter : GetSelectableParameter
            {
                public override string Name => "select";

                public override int ParameterCount => 1;

                public override string Description => "Resets to the default selectables.";

                public override string AddToAutocomplete => "select objects";

                public override IEnumerable<ISelectable> GetSelectables(IEnumerable<ISelectable> selectables, string[] parameters, bool not) => SelectCommand.GetSelectables(Parser.TryParse(parameters[0].ToLower().Remove("_"), true, SelectableType.Null));
            }

            /// <summary>
            /// Compares the selected state of selectables.
            /// </summary>
            public class SelectedParameter : GetSelectableParameter
            {
                public override string Name => "selected";

                public override string Description => "Selects already selected objects.";

                public override IEnumerable<ISelectable> GetSelectables(IEnumerable<ISelectable> selectables, string[] parameters, bool not) => selectables.Where(x => NotCheck(not, x.Selected));
            }
            
            /// <summary>
            /// Compares the time of selectables.
            /// </summary>
            public class TimeComparisonParameter : GetSelectableParameter
            {
                public override string Name => "time";

                public override int ParameterCount => 2;

                public override string Description => "Compares the time of the object.";

                public override string AddToAutocomplete => "time equals 0";

                public override IEnumerable<ISelectable> GetSelectables(IEnumerable<ISelectable> selectables, string[] parameters, bool not)
                {
                    var comparison = Parser.TryParse(parameters[0].Remove("_"), true, NumberComparison.Equals);
                    var time = Parser.TryParse(parameters[1], 0f);

                    foreach (var selectable in selectables)
                    {
                        if (selectable is TimelineObject timelineObject && NotCheck(not, comparison.Compare(timelineObject.Time, time)))
                            yield return selectable;
                        if (selectable is TimelineKeyframe timelineKeyframe && NotCheck(not, comparison.Compare(timelineKeyframe.Time, time)))
                            yield return selectable;
                        if (selectable is TimelineMarker timelineMarker && NotCheck(not, comparison.Compare(timelineMarker.Time, time)))
                            yield return selectable;
                        if (selectable is TimelineCheckpoint timelineCheckpoint && NotCheck(not, comparison.Compare(timelineCheckpoint.Time, time)))
                            yield return selectable;
                    }
                }
            }

            /// <summary>
            /// Compares the index of selectables.
            /// </summary>
            public class IndexComparisonParameter : GetSelectableParameter
            {
                public override string Name => "index";

                public override int ParameterCount => 1;

                public override string Description => "Compares the index of the object.";

                public override string AddToAutocomplete => "index 0";

                public override IEnumerable<ISelectable> GetSelectables(IEnumerable<ISelectable> selectables, string[] parameters, bool not)
                {
                    var comparison = Parser.TryParse(parameters[0].Remove("_"), true, NumberComparison.Equals);
                    var index = Parser.TryParse(parameters[1], 0);

                    foreach (var selectable in selectables)
                    {
                        if (selectable is TimelineObject timelineObject && NotCheck(not, comparison.Compare(timelineObject.Index, index)))
                            yield return selectable;
                        if (selectable is TimelineKeyframe timelineKeyframe && NotCheck(not, comparison.Compare(timelineKeyframe.Index, index)))
                            yield return selectable;
                        if (selectable is TimelineMarker timelineMarker && NotCheck(not, comparison.Compare(timelineMarker.Index, index)))
                            yield return timelineMarker;
                        if (selectable is TimelineCheckpoint timelineCheckpoint && NotCheck(not, comparison.Compare(timelineCheckpoint.Index, index)))
                            yield return selectable;
                    }
                }
            }

            /// <summary>
            /// Compares the locked state of selectables.
            /// </summary>
            public class LockedParameter : GetSelectableParameter
            {
                public override string Name => "locked";

                public override string Description => "Checks if the object is locked.";

                public override IEnumerable<ISelectable> GetSelectables(IEnumerable<ISelectable> selectables, string[] parameters, bool not)
                {
                    foreach (var selectable in selectables)
                    {
                        if (selectable is TimelineObject timelineObject && NotCheck(not, timelineObject.Locked))
                            yield return selectable;
                        if (selectable is TimelineKeyframe timelineKeyframe && NotCheck(not, timelineKeyframe.Locked))
                            yield return selectable;
                    }
                }
            }
            
            /// <summary>
            /// Compares the name of selectables.
            /// </summary>
            public class NameEqualsParameter : GetSelectableParameter
            {
                public override string Name => "name";

                public override int ParameterCount => 1;

                public override string Description => "Checks the objects name.";

                public override string AddToAutocomplete => "name \"Object Name\"";

                public override IEnumerable<ISelectable> GetSelectables(IEnumerable<ISelectable> selectables, string[] parameters, bool not)
                {
                    var name = parameters[0];
                    foreach (var selectable in selectables)
                    {
                        if (NotCheck(not, GetName(selectable) == name))
                            yield return selectable;
                    }
                }
            }

            /// <summary>
            /// Compares the name of selectables.
            /// </summary>
            public class NameContainsParameter : GetSelectableParameter
            {
                public override string Name => "name_contains";

                public override bool RequireQuotes => true;

                public override string Description => "Checks if an objects name contains a value.";

                public override string AddToAutocomplete => "name_contains \"Word\"";

                public override IEnumerable<ISelectable> GetSelectables(IEnumerable<ISelectable> selectables, string[] parameters, bool not)
                {
                    var name = parameters[0];
                    foreach (var selectable in selectables)
                    {
                        var n = GetName(selectable);
                        if (NotCheck(not, !string.IsNullOrEmpty(n) && n.Contains(name)))
                            yield return selectable;
                    }
                }
            }

            /// <summary>
            /// Compares the name of selectables.
            /// </summary>
            public class NameRegexParameter : GetSelectableParameter
            {
                public override string Name => "name_regex";

                public override bool RequireQuotes => true;

                public override string Description => "Checks an objects name using Regular Expression. Some knowledge of regex is required for this.";

                public override string AddToAutocomplete => "name_regex \"match\"";

                public override IEnumerable<ISelectable> GetSelectables(IEnumerable<ISelectable> selectables, string[] parameters, bool not)
                {
                    var name = parameters[0];
                    foreach (var selectable in selectables)
                    {
                        if (NotCheck(not, Regex.IsMatch(GetName(selectable), name)))
                            yield return selectable;
                    }
                }
            }

            /// <summary>
            /// Compares the description of selectables.
            /// </summary>
            public class DescriptionRegexParameter : GetSelectableParameter
            {
                public override string Name => "desc_regex";

                public override bool RequireQuotes => true;

                public override string Description => "Checks an objects description.";

                public override string AddToAutocomplete => "desc_regex match";

                public override IEnumerable<ISelectable> GetSelectables(IEnumerable<ISelectable> selectables, string[] parameters, bool not)
                {
                    var name = parameters[0];
                    foreach (var selectable in selectables)
                    {
                        if (NotCheck(not, Regex.IsMatch(GetDescription(selectable), name)))
                            yield return selectable;
                    }
                }
            }

            /// <summary>
            /// Compares the tag of selectables.
            /// </summary>
            public class ContainsTagParameter : GetSelectableParameter
            {
                public override string Name => "tag";

                public override int ParameterCount => 1;

                public override string Description => "Checks if a modifyable object contains a tag.";

                public override string AddToAutocomplete => "tag \"Object Group\"";

                public override IEnumerable<ISelectable> GetSelectables(IEnumerable<ISelectable> selectables, string[] parameters, bool not)
                {
                    var tag = parameters[0];
                    foreach (var selectable in selectables)
                        if (selectable is TimelineObject timelineObject && timelineObject.TryGetData(out IModifyable modifyable) && modifyable.Tags != null && NotCheck(not, modifyable.Tags.Contains(tag)))
                            yield return selectable;
                }
            }

            #endregion

            #region Timeline Object

            /// <summary>
            /// Checks if selectables are on the current editor layer.
            /// </summary>
            public class CurrentLayerParameter : GetSelectableParameter
            {
                public override string Name => "current_layer";

                public override string Description => "Gets objects on the current editor layer.";

                public override IEnumerable<ISelectable> GetSelectables(IEnumerable<ISelectable> selectables, string[] parameters, bool not) => selectables.Where(x => x is TimelineObject timelineObject && NotCheck(not, timelineObject.IsCurrentLayer) || x is TimelineMarker timelineMarker && NotCheck(not, timelineMarker.IsCurrentLayer));
            }

            /// <summary>
            /// Checks if selectables are in the same group as the first selectable.
            /// </summary>
            public class SamePrefabGroupParameter : GetSelectableParameter
            {
                public override string Name => "same_prefab_group";

                public override string Description => "Gets objects with the same prefab instance.";

                public override IEnumerable<ISelectable> GetSelectables(IEnumerable<ISelectable> selectables, string[] parameters, bool not)
                {
                    var f = selectables.FirstOrDefault();
                    if (f == null || f is not TimelineObject firstObject || !firstObject.TryGetPrefabable(out IPrefabable main))
                        yield break;

                    foreach (var selectable in selectables)
                    {
                        if (selectable is TimelineObject timelineObject && timelineObject.TryGetPrefabable(out IPrefabable prefabable) && NotCheck(not, main.SamePrefabInstance(prefabable)))
                            yield return selectable;
                    }
                }
            }

            /// <summary>
            /// Compares the editor group of selectables.
            /// </summary>
            public class EditorGroupEqualsParameter : GetSelectableParameter
            {
                public override string Name => "editor_group";

                public override int ParameterCount => 1;

                public override string Description => "Checks the objects editor group.";

                public override string AddToAutocomplete => "editor_group \"Editor Group\"";

                public override IEnumerable<ISelectable> GetSelectables(IEnumerable<ISelectable> selectables, string[] parameters, bool not)
                {
                    var group = parameters[0];
                    foreach (var selectable in selectables)
                    {
                        if (selectable is TimelineObject timelineObject && NotCheck(not, timelineObject.Group == group))
                            yield return selectable;
                    }
                }
            }

            /// <summary>
            /// Compares the reference type of selectables.
            /// </summary>
            public class ReferenceTypeEqualsParameter : GetSelectableParameter
            {
                public override string Name => "reference_type";

                public override int ParameterCount => 1;

                public override string Description => "Gets objects with the same timeline reference type (BeatmapObject, BackgroundObject, PrefabObject).";

                public override string AddToAutocomplete => "reference_type beatmap_object";

                public override IEnumerable<ISelectable> GetSelectables(IEnumerable<ISelectable> selectables, string[] parameters, bool not)
                {
                    var type = Parser.TryParse(parameters[0].Remove("_"), true, TimelineObject.TimelineReferenceType.Null);
                    foreach (var selectable in selectables)
                        if (selectable is TimelineObject timelineObject && NotCheck(not, timelineObject.TimelineReference == type))
                            yield return selectable;
                }
            }

            /// <summary>
            /// Compares the layer of selectables.
            /// </summary>
            public class LayerComparisonParameter : GetSelectableParameter
            {
                public override string Name => "layer";

                public override int ParameterCount => 2;

                public override string Description => "Compares the objects layer.";

                public override string AddToAutocomplete => "layer equals current_layer";

                public override IEnumerable<ISelectable> GetSelectables(IEnumerable<ISelectable> selectables, string[] parameters, bool not)
                {
                    var comparison = Parser.TryParse(parameters[0].Remove("_"), true, NumberComparison.Equals);
                    var layer = Parser.TryParse(parameters[1], 0);

                    foreach (var selectable in selectables)
                    {
                        if (selectable is TimelineObject timelineObject && NotCheck(not, comparison.Compare(timelineObject.Layer, layer)))
                            yield return selectable;
                        if (selectable is TimelineMarker timelineMarker && timelineMarker.Marker && NotCheck(not, timelineMarker.Marker.layers.Any(x => comparison.Compare(x, layer))))
                            yield return selectable;
                    }
                }
            }

            /// <summary>
            /// Compares the bin of selectables.
            /// </summary>
            public class BinComparisonParameter : GetSelectableParameter
            {
                public override string Name => "bin";

                public override int ParameterCount => 2;

                public override string Description => "Compares the objects bin (row).";

                public override string AddToAutocomplete => "bin equals max_bin";

                public override IEnumerable<ISelectable> GetSelectables(IEnumerable<ISelectable> selectables, string[] parameters, bool not)
                {
                    var comparison = Parser.TryParse(parameters[0].Remove("_"), true, NumberComparison.Equals);
                    var bin = Parser.TryParse(parameters[1], 0);

                    foreach (var selectable in selectables)
                        if (selectable is TimelineObject timelineObject && NotCheck(not, comparison.Compare(timelineObject.Bin, bin)))
                            yield return selectable;
                }
            }

            /// <summary>
            /// Compares the collapsed state of selectables.
            /// </summary>
            public class CollapsedParameter : GetSelectableParameter
            {
                public override string Name => "collapsed";

                public override string Description => "Checks if the object is collapsed.";

                public override IEnumerable<ISelectable> GetSelectables(IEnumerable<ISelectable> selectables, string[] parameters, bool not) => selectables.Where(x => x is TimelineObject timelineObject && NotCheck(not, timelineObject.Collapse));
            }

            /// <summary>
            /// Compares the hidden state of selectables.
            /// </summary>
            public class HiddenParameter : GetSelectableParameter
            {
                public override string Name => "hidden";

                public override string Description => "Checks if an object is hidden.";

                public override IEnumerable<ISelectable> GetSelectables(IEnumerable<ISelectable> selectables, string[] parameters, bool not) => selectables.Where(x => x is TimelineObject timelineObject && NotCheck(not, timelineObject.Hidden));
            }

            /// <summary>
            /// Compares the object type of selectables.
            /// </summary>
            public class ObjectTypeParameter : GetSelectableParameter
            {
                public override string Name => "object_type";

                public override int ParameterCount => 1;

                public override string Description => "Checks an objects' object type.";

                public override string AddToAutocomplete => "object_type normal";

                public override IEnumerable<ISelectable> GetSelectables(IEnumerable<ISelectable> selectables, string[] parameters, bool not)
                {
                    var objectType = Parser.TryParse(parameters[0].Remove("_"), true, BeatmapObject.ObjectType.Normal);
                    foreach (var selectable in selectables)
                        if (selectable is TimelineObject timelineObject && timelineObject.isBeatmapObject && NotCheck(not, timelineObject.GetData<BeatmapObject>().objectType == objectType))
                            yield return selectable;
                }
            }

            /// <summary>
            /// Compares the autokill type of selectables.
            /// </summary>
            public class AutokillTypeParameter : GetSelectableParameter
            {
                public override string Name => "autokill_type";

                public override int ParameterCount => 1;

                public override string Description => "Checks an objects' autokill type.";

                public override string AddToAutocomplete => "autokill_type no_autokill";

                public override IEnumerable<ISelectable> GetSelectables(IEnumerable<ISelectable> selectables, string[] parameters, bool not)
                {
                    var autoKillType = Parser.TryParse(parameters[0].Remove("_"), true, AutoKillType.NoAutokill);
                    foreach (var selectable in selectables)
                        if (selectable is TimelineObject timelineObject && timelineObject.isBeatmapObject && NotCheck(not, timelineObject.GetData<BeatmapObject>().autoKillType == autoKillType))
                            yield return selectable;
                }
            }

            #endregion

            #region Timeline Keyframe

            /// <summary>
            /// Compares the event type of event keyframes.
            /// </summary>
            public class EventTypeComparisonParameter : GetSelectableParameter
            {
                public override string Name => "event_type";

                public override int ParameterCount => 2;

                public override string Description => "Compares a keyframes event type.";

                public override string AddToAutocomplete => "event_type equals 0";

                public override IEnumerable<ISelectable> GetSelectables(IEnumerable<ISelectable> selectables, string[] parameters, bool not)
                {
                    var comparison = Parser.TryParse(parameters[0].Remove("_"), true, NumberComparison.Equals);
                    var typeStr = parameters[1];
                    var type = 0;
                    if (!int.TryParse(typeStr, out type))
                        type = typeStr switch
                        {
                            "pos" => 0,
                            "sca" => 1,
                            "rot" => 2,
                            "col" => 3,
                            _ => EventLibrary.displayNames.FindIndex(x => x == typeStr),
                        };
                    if (type < 0)
                        yield break;
                    foreach (var selectable in selectables)
                    {
                        if (selectable is TimelineKeyframe timelineKeyframe && NotCheck(not, comparison.Compare(timelineKeyframe.Type, type)))
                            yield return selectable;
                    }
                }
            }

            /// <summary>
            /// Compares the event coordinates of event keyframes.
            /// </summary>
            public class EventCoordParameter : GetSelectableParameter
            {
                public override string Name => "event_coord";

                public override int ParameterCount => 2;

                public override string Description => "Checks if a keyframe matches the keyframe coordinate.";

                public override string AddToAutocomplete => "event_coord 0 0";

                public override IEnumerable<ISelectable> GetSelectables(IEnumerable<ISelectable> selectables, string[] parameters, bool not)
                {
                    var type = Parser.TryParse(parameters[0], 0);
                    var index = Parser.TryParse(parameters[1], 0);
                    foreach (var selectable in selectables)
                    {
                        if (selectable is TimelineKeyframe timelineKeyframe && NotCheck(not, timelineKeyframe.Type == type && timelineKeyframe.Index == index))
                            yield return selectable;
                    }
                }
            }

            /// <summary>
            /// Compares a value of event keyframes.
            /// </summary>
            public class EventValueComparisonParameter : GetSelectableParameter
            {
                public override string Name => "event_value";

                public override int ParameterCount => 3;

                public override string Description => "Checks the value of a keyframe.";

                public override string AddToAutocomplete => "event_value 0 equals 10";

                public override IEnumerable<ISelectable> GetSelectables(IEnumerable<ISelectable> selectables, string[] parameters, bool not)
                {
                    var valueIndex = Parser.TryParse(parameters[0], 0);
                    var comparison = Parser.TryParse(parameters[1].Remove("_"), true, NumberComparison.Equals);
                    var value = Parser.TryParse(parameters[2], 0f);
                    foreach (var selectable in selectables)
                    {
                        if (selectable is TimelineKeyframe timelineKeyframe && timelineKeyframe.eventKeyframe && NotCheck(not, comparison.Compare(timelineKeyframe.eventKeyframe.GetValue(valueIndex), value)))
                            yield return selectable;
                    }
                }
            }

            /// <summary>
            /// Compares the easing of event keyframes.
            /// </summary>
            public class EaseTypeEqualsParameter : GetSelectableParameter
            {
                public override string Name => "easing";

                public override int ParameterCount => 2;

                public override string Description => "Checks the easing of a keyframe.";

                public override string AddToAutocomplete => "easing linear";

                public override IEnumerable<ISelectable> GetSelectables(IEnumerable<ISelectable> selectables, string[] parameters, bool not)
                {
                    var easing = Parser.TryParse(parameters[0], true, Easing.Linear);
                    foreach (var selectable in selectables)
                    {
                        if (selectable is TimelineKeyframe timelineKeyframe && timelineKeyframe.eventKeyframe && NotCheck(not, timelineKeyframe.eventKeyframe.curve == easing))
                            yield return selectable;
                    }
                }
            }

            #endregion

            #region Timeline Marker

            /// <summary>
            /// Compares the color of markers.
            /// </summary>
            public class ColorEqualsParameter : GetSelectableParameter
            {
                public override string Name => "color";

                public override int ParameterCount => 2;

                public override SelectableType RequiredSelectionType => SelectableType.Markers;

                public override string Description => "Compares a markers color slot.";

                public override string AddToAutocomplete => "color equals 0";

                public override IEnumerable<ISelectable> GetSelectables(IEnumerable<ISelectable> selectables, string[] parameters, bool not)
                {
                    var comparison = Parser.TryParse(parameters[0].Remove("_"), true, NumberComparison.Equals);
                    var color = Parser.TryParse(parameters[1], 0);
                    foreach (var selectable in selectables)
                        if (selectable is TimelineMarker timelineMarker && NotCheck(not, comparison.Compare(timelineMarker.ColorSlot, color)))
                            yield return selectable;
                }
            }

            /// <summary>
            /// Checks if markers have annotations.
            /// </summary>
            public class MarkerHasAnnotationsParameter : GetSelectableParameter
            {
                public override string Name => "has_annotations";

                public override SelectableType RequiredSelectionType => SelectableType.Markers;

                public override string Description => "Checks if a marker has annotations.";

                public override IEnumerable<ISelectable> GetSelectables(IEnumerable<ISelectable> selectables, string[] parameters, bool not)
                {
                    foreach (var selectable in selectables)
                        if (selectable is TimelineMarker timelineMarker && NotCheck(not, timelineMarker.Marker.annotations.IsEmpty()))
                            yield return selectable;
                }
            }

            #endregion

            #region Timeline Checkpoint

            /// <summary>
            /// Compares the respawn of checkpoints.
            /// </summary>
            public class CheckpointRespawnParameter : GetSelectableParameter
            {
                public override string Name => "respawn";

                public override SelectableType RequiredSelectionType => SelectableType.Checkpoints;

                public override string Description => "Checks a checkpoints respawn value.";

                public override IEnumerable<ISelectable> GetSelectables(IEnumerable<ISelectable> selectables, string[] parameters, bool not)
                {
                    foreach (var selectable in selectables)
                    {
                        if (selectable is TimelineCheckpoint timelineCheckpoint && timelineCheckpoint.Checkpoint && NotCheck(not, timelineCheckpoint.Checkpoint.respawn))
                            yield return selectable;
                    }
                }
            }

            /// <summary>
            /// Compares the heal of checkpoints.
            /// </summary>
            public class CheckpointHealParameter : GetSelectableParameter
            {
                public override string Name => "heal";

                public override SelectableType RequiredSelectionType => SelectableType.Checkpoints;

                public override string Description => "Checks a checkpoints heal value.";

                public override IEnumerable<ISelectable> GetSelectables(IEnumerable<ISelectable> selectables, string[] parameters, bool not)
                {
                    foreach (var selectable in selectables)
                    {
                        if (selectable is TimelineCheckpoint timelineCheckpoint && timelineCheckpoint.Checkpoint && NotCheck(not, timelineCheckpoint.Checkpoint.heal))
                            yield return selectable;
                    }
                }
            }

            /// <summary>
            /// Compares the set time of checkpoints.
            /// </summary>
            public class CheckpointSetTimeParameter : GetSelectableParameter
            {
                public override string Name => "set_time";

                public override SelectableType RequiredSelectionType => SelectableType.Checkpoints;

                public override string Description => "Checks a checkpoints set time value.";

                public override IEnumerable<ISelectable> GetSelectables(IEnumerable<ISelectable> selectables, string[] parameters, bool not)
                {
                    foreach (var selectable in selectables)
                    {
                        if (selectable is TimelineCheckpoint timelineCheckpoint && timelineCheckpoint.Checkpoint && NotCheck(not, timelineCheckpoint.Checkpoint.setTime))
                            yield return selectable;
                    }
                }
            }

            /// <summary>
            /// Compares the reverse of checkpoints.
            /// </summary>
            public class CheckpointReverseParameter : GetSelectableParameter
            {
                public override string Name => "reverse";

                public override SelectableType RequiredSelectionType => SelectableType.Checkpoints;

                public override string Description => "Checks a checkpoints reverse value.";

                public override IEnumerable<ISelectable> GetSelectables(IEnumerable<ISelectable> selectables, string[] parameters, bool not)
                {
                    foreach (var selectable in selectables)
                    {
                        if (selectable is TimelineCheckpoint timelineCheckpoint && timelineCheckpoint.Checkpoint && NotCheck(not, timelineCheckpoint.Checkpoint.reverse))
                            yield return selectable;
                    }
                }
            }

            /// <summary>
            /// Compares the auto triggerable of checkpoints.
            /// </summary>
            public class CheckpointAutoTriggerableParameter : GetSelectableParameter
            {
                public override string Name => "auto_triggerable";

                public override SelectableType RequiredSelectionType => SelectableType.Checkpoints;

                public override string Description => "Checks a checkpoints auto triggerable value.";

                public override IEnumerable<ISelectable> GetSelectables(IEnumerable<ISelectable> selectables, string[] parameters, bool not)
                {
                    foreach (var selectable in selectables)
                    {
                        if (selectable is TimelineCheckpoint timelineCheckpoint && timelineCheckpoint.Checkpoint && NotCheck(not, timelineCheckpoint.Checkpoint.autoTriggerable))
                            yield return selectable;
                    }
                }
            }

            #endregion

            #region Level

            /// <summary>
            /// Compares a value of level metadata.
            /// </summary>
            public class MetaDataValueEquals : GetSelectableParameter
            {
                public override string Name => "metadata";

                public override int ParameterCount => 2;

                public override SelectableType RequiredSelectionType => SelectableType.Levels;

                public override string Description => "Checks a levels metadata for its values.";

                public override string AddToAutocomplete => "metadata artist_name Kaixo";

                public override IEnumerable<ISelectable> GetSelectables(IEnumerable<ISelectable> selectables, string[] parameters, bool not)
                {
                    var type = parameters[0];
                    var value = parameters[1];

                    foreach (var selectable in selectables)
                    {
                        if (selectable is not LevelPanel levelPanel || !levelPanel.Item || !levelPanel.Item.metadata)
                            continue;

                        var metadata = levelPanel.Item.metadata;
                        var accept = NotCheck(not, type switch
                        {
                            "arcade_id" => metadata.arcadeID == value,
                            "artist_name" => metadata.artist.name == value,
                            "creator_name" => metadata.creator.name == value,
                            "level_name" => metadata.beatmap.name == value,
                            "song_title" => metadata.song.title == value,
                            "difficulty" => int.TryParse(value, out int num) ? metadata.song.difficulty == num : metadata.song.Difficulty == value,
                            _ => false,
                        });
                        if (accept)
                            yield return selectable;
                    }
                }
            }

            #endregion

            #region Modifier

            /// <summary>
            /// Compares a value of modifiers.
            /// </summary>
            public class ContainsModifierValue : GetSelectableParameter
            {
                public override string Name => "contains_value";

                public override int ParameterCount => 1;

                public override SelectableType RequiredSelectionType => SelectableType.Modifiers;

                public override string Description => "Checks if a modifier contains a value.";

                public override string AddToAutocomplete => "contains_value value";

                public override IEnumerable<ISelectable> GetSelectables(IEnumerable<ISelectable> selectables, string[] parameters, bool not)
                {
                    var value = parameters[0];
                    foreach (var selectable in selectables)
                    {
                        if (selectable is ModifierCard modifierCard && modifierCard.Modifier && NotCheck(not, modifierCard.Modifier.values.Contains(value)))
                            yield return selectable;
                    }
                }
            }

            #endregion

            #region Annotations

            /// <summary>
            /// Compares the color of annotations.
            /// </summary>
            public class AnnotationColorComparisonParameter : GetSelectableParameter
            {
                public override string Name => "color";

                public override int ParameterCount => 2;

                public override SelectableType RequiredSelectionType => SelectableType.Annotations;

                public override string Description => "Compares the annotation color.";

                public override string AddToAutocomplete => "color equals 0";

                public override IEnumerable<ISelectable> GetSelectables(IEnumerable<ISelectable> selectables, string[] parameters, bool not)
                {
                    var comparison = Parser.TryParse(parameters[0].Remove("_"), true, NumberComparison.Equals);
                    var color = Parser.TryParse(parameters[1], 0);

                    foreach (var selectable in selectables)
                    {
                        if (selectable is Annotation annotation && NotCheck(not, comparison.Compare(annotation.color, color)))
                            yield return annotation;
                    }
                }
            }

            /// <summary>
            /// Compares the hex color of annotations.
            /// </summary>
            public class AnnotationHexColorEqualsParameter : GetSelectableParameter
            {
                public override string Name => "hex_color";

                public override int ParameterCount => 2;

                public override SelectableType RequiredSelectionType => SelectableType.Annotations;

                public override string Description => "Compares the annotation custom hex color.";

                public override string AddToAutocomplete => "hex_color ffffff";

                public override IEnumerable<ISelectable> GetSelectables(IEnumerable<ISelectable> selectables, string[] parameters, bool not)
                {
                    var hexColor = parameters[0];

                    foreach (var selectable in selectables)
                    {
                        if (selectable is Annotation annotation && NotCheck(not, annotation.hexColor == hexColor))
                            yield return annotation;
                    }
                }
            }

            /// <summary>
            /// Compares the opacity of annotations.
            /// </summary>
            public class AnnotationOpacityComparisonParameter : GetSelectableParameter
            {
                public override string Name => "opacity";

                public override int ParameterCount => 2;

                public override SelectableType RequiredSelectionType => SelectableType.Annotations;

                public override string Description => "Compares the annotation opacity.";

                public override string AddToAutocomplete => "opacity equals 1";

                public override IEnumerable<ISelectable> GetSelectables(IEnumerable<ISelectable> selectables, string[] parameters, bool not)
                {
                    var comparison = Parser.TryParse(parameters[0].Remove("_"), true, NumberComparison.Equals);
                    var opacity = Parser.TryParse(parameters[1], 0f);

                    foreach (var selectable in selectables)
                    {
                        if (selectable is Annotation annotation && NotCheck(not, comparison.Compare(annotation.opacity, opacity)))
                            yield return annotation;
                    }
                }
            }

            /// <summary>
            /// Compares the thickness of annotations.
            /// </summary>
            public class AnnotationThicknessComparisonParameter : GetSelectableParameter
            {
                public override string Name => "thickness";

                public override int ParameterCount => 2;

                public override SelectableType RequiredSelectionType => SelectableType.Annotations;

                public override string Description => "Compares the annotation thickness.";

                public override string AddToAutocomplete => "thickness equals 0";

                public override IEnumerable<ISelectable> GetSelectables(IEnumerable<ISelectable> selectables, string[] parameters, bool not)
                {
                    var comparison = Parser.TryParse(parameters[0].Remove("_"), true, NumberComparison.Equals);
                    var thickness = Parser.TryParse(parameters[1], 0f);

                    foreach (var selectable in selectables)
                    {
                        if (selectable is Annotation annotation && NotCheck(not, comparison.Compare(annotation.thickness, thickness)))
                            yield return annotation;
                    }
                }
            }

            /// <summary>
            /// Compares the hidden state of annotations.
            /// </summary>
            public class AnnotationHiddenParameter : GetSelectableParameter
            {
                public override string Name => "hidden";

                public override SelectableType RequiredSelectionType => SelectableType.Annotations;

                public override string Description => "Compares the annotation hidden state.";

                public override IEnumerable<ISelectable> GetSelectables(IEnumerable<ISelectable> selectables, string[] parameters, bool not)
                {
                    foreach (var selectable in selectables)
                    {
                        if (selectable is Annotation annotation && NotCheck(not, annotation.hidden))
                            yield return annotation;
                    }
                }
            }

            /// <summary>
            /// Compares the fixed camera state of annotations.
            /// </summary>
            public class AnnotationFixedCameraParameter : GetSelectableParameter
            {
                public override string Name => "fixed_camera";

                public override SelectableType RequiredSelectionType => SelectableType.Annotations;

                public override string Description => "Compares the annotation fixed camera state.";

                public override IEnumerable<ISelectable> GetSelectables(IEnumerable<ISelectable> selectables, string[] parameters, bool not)
                {
                    foreach (var selectable in selectables)
                    {
                        if (selectable is Annotation annotation && NotCheck(not, annotation.fixedCamera))
                            yield return annotation;
                    }
                }
            }

            #endregion

            #endregion

            #region Action

            #region General

            /// <summary>
            /// Logs all selected objects to the console.
            /// </summary>
            public class LogSelectedParameter : ActionParameter
            {
                public override string Name => "log";

                public override string Description => "Logs all selected objects to the console.";

                public override void Run(IEnumerable<ISelectable> selectables, string[] parameters)
                {
                    var sb = new StringBuilder();
                    sb.AppendLine("Selected objects:");
                    int count = 0;
                    foreach (var selectable in selectables)
                    {
                        sb.AppendLine(selectable.ToString());
                        count++;
                    }
                    sb.AppendLine($"Count: {count}");
                    CoreHelper.Log(sb.ToString());
                }
            }

            /// <summary>
            /// Notifies the amount of objects that are selected.
            /// </summary>
            public class NotifyCountParameter : ActionParameter
            {
                public override string Name => "notify_count";

                public override string Description => "Notifies the amount of objects that are selected.";

                public override void Run(IEnumerable<ISelectable> selectables, string[] parameters) => EditorManager.inst.DisplayNotification($"Selected items: {selectables.Count()}", 2f, EditorManager.NotificationType.Success);
            }

            /// <summary>
            /// Sets the name of all selected objects.
            /// </summary>
            public class SetNameParameter : ActionParameter
            {
                public override string Name => "set_name";

                public override int ParameterCount => 1;

                public override string Description => "Sets the name of all selected objects.";

                public override string AddToAutocomplete => "set_name \"New Name\"";

                public override void Run(IEnumerable<ISelectable> selectables, string[] parameters)
                {
                    var name = parameters[0];
                    foreach (var selectable in selectables)
                        SetName(selectable, name);
                }
            }

            /// <summary>
            /// Sets the name of all selected objects.
            /// </summary>
            public class ReplaceNameParameter : ActionParameter
            {
                public override string Name => "replace_name";

                public override int ParameterCount => 2;

                public override string Description => "Sets the name of all selected objects.";

                public override string AddToAutocomplete => "replace_name \"old name\" new_name";

                public override void Run(IEnumerable<ISelectable> selectables, string[] parameters)
                {
                    var oldValue = parameters[0];
                    var newValue = parameters[1];
                    foreach (var selectable in selectables)
                        SetName(selectable, GetName(selectable).Replace(oldValue, newValue));
                }
            }

            #endregion

            #region Timeline Object

            /// <summary>
            /// Mirrors all selected objects in a specified direction.
            /// </summary>
            public class MirrorObjectParameter : ActionParameter
            {
                public override string Name => "mirror";

                public override int ParameterCount => 1;

                public override SelectableType RequiredSelectionType => SelectableType.Objects;

                public override string Description => "Mirrors all selected objects in a specified direction.";

                public override string AddToAutocomplete => "mirror horizontal";

                public override void Run(IEnumerable<ISelectable> selectables, string[] parameters)
                {
                    var direction = Parser.TryParse(parameters[0], true, Direction.Horizontal);
                    int axis = direction == Direction.Horizontal ? 0 : 1;
                    foreach (var selectable in selectables)
                    {
                        if (selectable is not TimelineObject timelineObject)
                            continue;
                        
                        switch (timelineObject.TimelineReference)
                        {
                            case TimelineObject.TimelineReferenceType.BeatmapObject: {
                                    var beatmapObject = timelineObject.GetData<BeatmapObject>();
                                    for (int i = 0; i < 3; i++)
                                    {
                                        if (direction == Direction.Vertical && i == 2)
                                            break;

                                        for (int j = 0; j < beatmapObject.events[i].Count; j++)
                                        {
                                            beatmapObject.events[i][j].values[axis] = -beatmapObject.events[i][j].values[axis];
                                            beatmapObject.events[i][j].randomValues[axis] = -beatmapObject.events[i][j].randomValues[axis];
                                        }
                                    }

                                    beatmapObject.GetParentRuntime()?.UpdateObject(beatmapObject, ObjectContext.KEYFRAMES);
                                    break;
                                }
                            case TimelineObject.TimelineReferenceType.PrefabObject: {
                                    var prefabObject = timelineObject.GetData<PrefabObject>();
                                    prefabObject.events[0].values[axis] = -prefabObject.events[0].values[axis];
                                    prefabObject.events[1].values[axis] = -prefabObject.events[1].values[axis];
                                    if (direction == Direction.Horizontal)
                                        prefabObject.events[2].values[0] = -prefabObject.events[2].values[0];
                                    prefabObject.GetParentRuntime()?.UpdatePrefab(prefabObject, PrefabObjectContext.TRANSFORM_OFFSET);
                                    break;
                                }
                            case TimelineObject.TimelineReferenceType.BackgroundObject: {
                                    var backgroundObject = timelineObject.GetData<BackgroundObject>();
                                    if (direction == Direction.Horizontal)
                                    {
                                        backgroundObject.pos.x = -backgroundObject.pos.x;
                                        backgroundObject.scale.x = -backgroundObject.scale.x;
                                        backgroundObject.rot = -backgroundObject.rot;
                                    }
                                    else
                                    {
                                        backgroundObject.pos.y = -backgroundObject.pos.y;
                                        backgroundObject.scale.y = -backgroundObject.scale.y;
                                    }
                                    break;
                                }
                        }
                    }
                }
            }

            /// <summary>
            /// Sets the collapse state of all selected objects.
            /// </summary>
            public class SetCollapseParameter : ActionParameter
            {
                public override string Name => "collapse";

                public override int ParameterCount => 1;

                public override string Description => "Sets the collapse state of all selected objects.";

                public override string AddToAutocomplete => "collapse true";

                public override void Run(IEnumerable<ISelectable> selectables, string[] parameters)
                {
                    var collapse = Parser.TryParse(parameters[0], false);
                    foreach (var selectable in selectables)
                        if (selectable is TimelineObject timelineObject)
                            timelineObject.Collapse = collapse;
                }
            }

            /// <summary>
            /// Swaps the collapse state of all selected objects.
            /// </summary>
            public class SwapCollapseParameter : ActionParameter
            {
                public override string Name => "swap_collapse";

                public override string Description => "Swaps the collapse state of all selected objects.";

                public override void Run(IEnumerable<ISelectable> selectables, string[] parameters)
                {
                    foreach (var selectable in selectables)
                        if (selectable is TimelineObject timelineObject)
                            timelineObject.Collapse = !timelineObject.Collapse;
                }
            }

            /// <summary>
            /// Sets the hidden state of all selected objects.
            /// </summary>
            public class SetHiddenParameter : ActionParameter
            {
                public override string Name => "hidden";

                public override int ParameterCount => 1;

                public override string Description => "Sets the hidden state of all selected objects.";

                public override string AddToAutocomplete => "hidden true";

                public override void Run(IEnumerable<ISelectable> selectables, string[] parameters)
                {
                    var hidden = Parser.TryParse(parameters[0], false);
                    foreach (var selectable in selectables)
                        if (selectable is TimelineObject timelineObject)
                        {
                            timelineObject.Hidden = hidden;
                            switch (timelineObject.TimelineReference)
                            {
                                case TimelineObject.TimelineReferenceType.BeatmapObject: {
                                        var beatmapObject = timelineObject.GetData<BeatmapObject>();
                                        beatmapObject.GetParentRuntime()?.UpdateObject(beatmapObject, ObjectContext.HIDE);
                                        break;
                                    }
                                case TimelineObject.TimelineReferenceType.BackgroundObject: {
                                        var backgroundObject = timelineObject.GetData<BackgroundObject>();
                                        backgroundObject.GetParentRuntime()?.UpdateBackgroundObject(backgroundObject, BackgroundObjectContext.HIDE);
                                        break;
                                    }
                                case TimelineObject.TimelineReferenceType.PrefabObject: {
                                        var prefabObject = timelineObject.GetData<PrefabObject>();
                                        prefabObject.GetParentRuntime()?.UpdatePrefab(prefabObject, PrefabObjectContext.HIDE);
                                        break;
                                    }
                            }
                        }
                }
            }

            /// <summary>
            /// Swaps the hidden state of all selected objects.
            /// </summary>
            public class SwapHiddenParameter : ActionParameter
            {
                public override string Name => "swap_hidden";

                public override string Description => "Swaps the hidden state of all selected objects.";

                public override void Run(IEnumerable<ISelectable> selectables, string[] parameters)
                {
                    foreach (var selectable in selectables)
                        if (selectable is TimelineObject timelineObject)
                        {
                            timelineObject.Hidden = !timelineObject.Hidden;
                            switch (timelineObject.TimelineReference)
                            {
                                case TimelineObject.TimelineReferenceType.BeatmapObject: {
                                        var beatmapObject = timelineObject.GetData<BeatmapObject>();
                                        beatmapObject.GetParentRuntime()?.UpdateObject(beatmapObject, ObjectContext.HIDE);
                                        break;
                                    }
                                case TimelineObject.TimelineReferenceType.BackgroundObject: {
                                        var backgroundObject = timelineObject.GetData<BackgroundObject>();
                                        backgroundObject.GetParentRuntime()?.UpdateBackgroundObject(backgroundObject, BackgroundObjectContext.HIDE);
                                        break;
                                    }
                                case TimelineObject.TimelineReferenceType.PrefabObject: {
                                        var prefabObject = timelineObject.GetData<PrefabObject>();
                                        prefabObject.GetParentRuntime()?.UpdatePrefab(prefabObject, PrefabObjectContext.HIDE);
                                        break;
                                    }
                            }
                        }
                }
            }

            /// <summary>
            /// Copies the selected objects.
            /// </summary>
            public class CopyTimelineObjectParameter : ActionParameter
            {
                public override string Name => "copy";

                public override SelectableType RequiredSelectionType => SelectableType.Objects;

                public override string Description => "Copies the selected objects.";

                public override void Run(IEnumerable<ISelectable> selectables, string[] parameters)
                {
                    var selected = selectables.Where(x => x is TimelineObject).Select(x => x as TimelineObject);
                    if (selected.IsEmpty())
                        return;

                    float start = 0f;
                    if (EditorConfig.Instance.PasteOffset.Value)
                        start = -AudioManager.inst.CurrentAudioSource.time + selected.Min(x => x.Time);

                    var beatmapObjects = selected.Where(x => x.isBeatmapObject).Select(x => x.GetData<BeatmapObject>());
                    var prefabObjects = selected.Where(x => x.isPrefabObject).Select(x => x.GetData<PrefabObject>());
                    var backgroundObjects = selected.Where(x => x.isBackgroundObject).Select(x => x.GetData<BackgroundObject>());

                    var prefabIDs = new Dictionary<string, Prefab>();
                    foreach (var prefabObject in prefabObjects)
                    {
                        if (string.IsNullOrEmpty(prefabObject.prefabID))
                            continue;
                        var prefab = prefabObject.GetPrefab();
                        if (prefab)
                            prefabIDs[prefabObject.prefabID] = prefab;
                    }

                    var copy = new Prefab("copied prefab", 0, start,
                        beatmapObjects.ToList(),
                        prefabObjects.ToList(),
                        null,
                        backgroundObjects.ToList(),
                        !prefabIDs.IsEmpty() ? prefabIDs.Values.ToList() : null);

                    copy.description = "Take me wherever you go!";
                    ObjectEditor.inst.copy = copy;
                    ObjEditor.inst.hasCopiedObject = true;

                    if (EditorConfig.Instance.CopyPasteGlobal.Value && RTFile.DirectoryExists(Application.persistentDataPath))
                        RTFile.WriteToFile(RTFile.CombinePaths(Application.persistentDataPath, $"copied_objects{FileFormat.LSP.Dot()}"), copy.ToJSON().ToString());
                }
            }

            /// <summary>
            /// Duplicates the selected objects.
            /// </summary>
            public class DuplicateTimelineObjectParameter : ActionParameter
            {
                public override string Name => "duplicate";

                public override SelectableType RequiredSelectionType => SelectableType.Objects;

                public override string Description => "Duplicates the selected objects.";

                public override void Run(IEnumerable<ISelectable> selectables, string[] parameters)
                {
                    var selected = selectables.Where(x => x is TimelineObject).Select(x => x as TimelineObject);
                    if (selected.IsEmpty())
                        return;

                    float start = 0f;
                    if (EditorConfig.Instance.PasteOffset.Value)
                        start = -AudioManager.inst.CurrentAudioSource.time + selected.Min(x => x.Time);

                    var beatmapObjects = selected.Where(x => x.isBeatmapObject).Select(x => x.GetData<BeatmapObject>());
                    var prefabObjects = selected.Where(x => x.isPrefabObject).Select(x => x.GetData<PrefabObject>());
                    var backgroundObjects = selected.Where(x => x.isBackgroundObject).Select(x => x.GetData<BackgroundObject>());

                    var prefabIDs = new Dictionary<string, Prefab>();
                    foreach (var prefabObject in prefabObjects)
                    {
                        if (string.IsNullOrEmpty(prefabObject.prefabID))
                            continue;
                        var prefab = prefabObject.GetPrefab();
                        if (prefab)
                            prefabIDs[prefabObject.prefabID] = prefab;
                    }

                    var copy = new Prefab("copied prefab", 0, start,
                        beatmapObjects.ToList(),
                        prefabObjects.ToList(),
                        null,
                        backgroundObjects.ToList(),
                        !prefabIDs.IsEmpty() ? prefabIDs.Values.ToList() : null);

                    copy.description = "Take me wherever you go!";
                    ObjectEditor.inst.copy = copy;
                    ObjEditor.inst.hasCopiedObject = true;

                    if (EditorConfig.Instance.CopyPasteGlobal.Value && RTFile.DirectoryExists(Application.persistentDataPath))
                        RTFile.WriteToFile(RTFile.CombinePaths(Application.persistentDataPath, $"copied_objects{FileFormat.LSP.Dot()}"), copy.ToJSON().ToString());

                    ObjectEditor.inst.PasteObject(0f, true, false);
                }
            }

            /// <summary>
            /// Deletes the selected objects.
            /// </summary>
            public class DeleteTimelineObjectParameter : ActionParameter
            {
                public override string Name => "delete";

                public override SelectableType RequiredSelectionType => SelectableType.Objects;

                public override string Description => "Deletes the selected objects.";

                public override void Run(IEnumerable<ISelectable> selectables, string[] parameters)
                {
                    var list = selectables.Where(x => x is TimelineObject).Select(x => x as TimelineObject);
                    if (list.IsEmpty())
                        return;

                    Prefab prefab = null;
                    float t = 0f;
                    List<IDPair> children = null;

                    EditorManager.inst.history.Add(new History.Command("Delete Objects",
                        () =>
                        {
                            EditorDialog.CurrentDialog?.Close();

                            prefab = new Prefab("deleted objects", 0, 0f,
                               list.Where(x => x.isBeatmapObject).Select(x => x.GetData<BeatmapObject>()).ToList(),
                               list.Where(x => x.isPrefabObject).Select(x => x.GetData<PrefabObject>()).ToList(),
                               null,
                               list.Where(x => x.isBackgroundObject).Select(x => x.GetData<BackgroundObject>()).ToList());

                            t = list.Min(x => x.Time);

                            children = new List<IDPair>();
                            foreach (var parent in prefab.beatmapObjects)
                            {
                                for (int i = 0; i < GameData.Current.beatmapObjects.Count; i++)
                                {
                                    var beatmapObject = GameData.Current.beatmapObjects[i];
                                    if (beatmapObject.parent == parent.id)
                                        children.Add(new IDPair(parent.id, beatmapObject.id)); // old = parent new = id
                                        }
                            }

                            EditorTimeline.inst.DeleteObjects();
                        },
                        () =>
                        {
                            EditorTimeline.inst.DeselectAllObjects();
                            new PrefabExpander(prefab).Select().RetainID().Offset(t).Expand();

                            if (children != null)
                            {
                                foreach (var pair in children)
                                {
                                    if (GameData.Current.beatmapObjects.TryFind(x => x.id == pair.newID, out BeatmapObject beatmapObject))
                                    {
                                        beatmapObject.Parent = pair.oldID;
                                        RTLevel.Current.UpdateObject(beatmapObject, ObjectContext.PARENT_CHAIN);
                                    }
                                }
                            }

                            list = EditorTimeline.inst.SelectedObjects;
                        }), true);
                }
            }

            /// <summary>
            /// Updates all selected objects with a specific context.
            /// </summary>
            public class UpdateParameter : ActionParameter
            {
                public override string Name => "update";

                public override int ParameterCount => 1;

                public override SelectableType RequiredSelectionType => SelectableType.Objects;

                public override string Description => "Updates all selected objects with a specific context.";

                public override string AddToAutocomplete => "update keyframes";

                public override void Run(IEnumerable<ISelectable> selectables, string[] parameters)
                {
                    var context = parameters[0];
                    foreach (var selectable in selectables)
                    {
                        if (selectable is TimelineObject timelineObject)
                            timelineObject.UpdateObject(context);
                    }
                }
            }

            #endregion

            #region Timeline Marker

            /// <summary>
            /// Sets the color of all selected markers.
            /// </summary>
            public class SetMarkerColorParameter : ActionParameter
            {
                public override string Name => "set_color";

                public override int ParameterCount => 1;

                public override SelectableType RequiredSelectionType => SelectableType.Markers;

                public override string Description => "Sets the color of all selected markers.";

                public override string AddToAutocomplete => "set_color 0";

                public override void Run(IEnumerable<ISelectable> selectables, string[] parameters)
                {
                    var color = Parser.TryParse(parameters[0], 0);
                    foreach (var selectable in selectables)
                    {
                        if (selectable is TimelineMarker timelineMarker)
                            timelineMarker.ColorSlot = color;
                    }
                }
            }

            /// <summary>
            /// Sets the layer of all selected markers. If the layer already exists in a marker, then it is skipped.
            /// </summary>
            public class SetMarkerLayerParameter : ActionParameter
            {
                public override string Name => "set_layer";

                public override int ParameterCount => 1;

                public override SelectableType RequiredSelectionType => SelectableType.Markers;

                public override string Description => "Sets the layer of all selected markers. If the layer already exists in a marker, then it is skipped.";

                public override string AddToAutocomplete => "set_layer 0";

                public override void Run(IEnumerable<ISelectable> selectables, string[] parameters)
                {
                    var layer = Parser.TryParse(parameters[0], 0);
                    foreach (var selectable in selectables)
                    {
                        if (selectable is TimelineMarker timelineMarker && timelineMarker.Marker && !timelineMarker.Marker.layers.Contains(layer))
                            timelineMarker.Marker.layers.Add(layer);
                    }
                }
            }

            /// <summary>
            /// Removes the layer of all selected markers.
            /// </summary>
            public class RemoveMarkerLayerParameter : ActionParameter
            {
                public override string Name => "remove_layer";

                public override int ParameterCount => 1;

                public override SelectableType RequiredSelectionType => SelectableType.Markers;

                public override string Description => "Removes the layer of all selected markers.";

                public override string AddToAutocomplete => "remove_layer 0";

                public override void Run(IEnumerable<ISelectable> selectables, string[] parameters)
                {
                    var layer = Parser.TryParse(parameters[0], 0);
                    foreach (var selectable in selectables)
                    {
                        if (selectable is TimelineMarker timelineMarker && timelineMarker.Marker)
                            timelineMarker.Marker.layers.Remove(layer);
                    }
                }
            }

            /// <summary>
            /// Clears the layers of all selected markers.
            /// </summary>
            public class ClearMarkerLayersParameter : ActionParameter
            {
                public override string Name => "clear_layers";

                public override SelectableType RequiredSelectionType => SelectableType.Markers;

                public override string Description => "Clears the layers of all selected markers.";

                public override void Run(IEnumerable<ISelectable> selectables, string[] parameters)
                {
                    foreach (var selectable in selectables)
                    {
                        if (selectable is TimelineMarker timelineMarker && timelineMarker.Marker)
                            timelineMarker.Marker.layers.Clear();
                    }
                }
            }

            /// <summary>
            /// Moves the annotations of all selected markers.
            /// </summary>
            public class MoveMarkerAnnotations : ActionParameter
            {
                public override string Name => "move_annotations";

                public override int ParameterCount => 2;

                public override SelectableType RequiredSelectionType => SelectableType.Markers;

                public override string Description => "Moves the annotations of all selected markers.";

                public override string AddToAutocomplete => "move_annotations 0 0";

                public override void Run(IEnumerable<ISelectable> selectables, string[] parameters)
                {
                    var pos = new Vector2(Parser.TryParse(parameters[0], 0f), Parser.TryParse(parameters[1], 0f));
                    foreach (var selectable in selectables)
                    {
                        if (selectable is TimelineMarker timelineMarker && timelineMarker.Marker)
                        {
                            for (int i = 0; i < timelineMarker.Marker.annotations.Count; i++)
                            {
                                var annotation = timelineMarker.Marker.annotations[i];
                                for (int j = 0; j < annotation.points.Count; j++)
                                    annotation.points[j] += pos;
                            }
                        }
                    }
                }
            }

            /// <summary>
            /// Scales the annotations of all selected markers.
            /// </summary>
            public class ScaleMarkerAnnotations : ActionParameter
            {
                public override string Name => "scale_annotations";

                public override int ParameterCount => 2;

                public override SelectableType RequiredSelectionType => SelectableType.Markers;

                public override string Description => "Scales the annotations of all selected markers.";

                public override string AddToAutocomplete => "scale_annotations 0 0";

                public override void Run(IEnumerable<ISelectable> selectables, string[] parameters)
                {
                    var sca = new Vector2(Parser.TryParse(parameters[0], 0f), Parser.TryParse(parameters[1], 0f));
                    foreach (var selectable in selectables)
                    {
                        if (selectable is TimelineMarker timelineMarker && timelineMarker.Marker)
                        {
                            for (int i = 0; i < timelineMarker.Marker.annotations.Count; i++)
                            {
                                var annotation = timelineMarker.Marker.annotations[i];
                                for (int j = 0; j < annotation.points.Count; j++)
                                    annotation.points[j] = RTMath.Scale(annotation.points[j], sca);
                            }
                        }
                    }
                }
            }

            /// <summary>
            /// Rotates the annotations of all selected markers.
            /// </summary>
            public class RotateMarkerAnnotations : ActionParameter
            {
                public override string Name => "rotate_annotations";

                public override int ParameterCount => 1;

                public override SelectableType RequiredSelectionType => SelectableType.Markers;

                public override string Description => "Rotates the annotations of all selected markers.";

                public override string AddToAutocomplete => "rotate_annotations 0";

                public override void Run(IEnumerable<ISelectable> selectables, string[] parameters)
                {
                    var rot = Parser.TryParse(parameters[0], 0f);
                    foreach (var selectable in selectables)
                    {
                        if (selectable is TimelineMarker timelineMarker && timelineMarker.Marker)
                        {
                            for (int i = 0; i < timelineMarker.Marker.annotations.Count; i++)
                            {
                                var annotation = timelineMarker.Marker.annotations[i];
                                for (int j = 0; j < annotation.points.Count; j++)
                                    annotation.points[j] = RTMath.Rotate(annotation.points[j], rot);
                            }
                        }
                    }
                }
            }

            /// <summary>
            /// Clears the annotations of all selected markers.
            /// </summary>
            public class ClearMarkerAnnotations : ActionParameter
            {
                public override string Name => "clear_annotations";

                public override SelectableType RequiredSelectionType => SelectableType.Markers;

                public override string Description => "Clears the annotations of all selected markers.";

                public override void Run(IEnumerable<ISelectable> selectables, string[] parameters)
                {
                    foreach (var selectable in selectables)
                    {
                        if (selectable is TimelineMarker timelineMarker && timelineMarker.Marker)
                            timelineMarker.Marker.annotations.Clear();
                    }
                }
            }

            /// <summary>
            /// Draws a box in the annotations of all selected markers.
            /// </summary>
            public class DrawBoxMarkerAnnotation : ActionParameter
            {
                public override string Name => "draw_box";

                public override int ParameterCount => 10;

                public override SelectableType RequiredSelectionType => SelectableType.Markers;

                public override string Description => "Draws a box in the annotations of all selected markers.";

                public override string AddToAutocomplete => "draw_box 0 0 10 10 0 0 - 1 4 false";
                public override void Run(IEnumerable<ISelectable> selectables, string[] parameters)
                {
                    var pos = new Vector2(Parser.TryParse(parameters[0], 0f), Parser.TryParse(parameters[1], 0f));
                    var sca = new Vector2(Parser.TryParse(parameters[2], 1f), Parser.TryParse(parameters[3], 1f));
                    var rot = Parser.TryParse(parameters[4], 0f);
                    var col = Parser.TryParse(parameters[5], 0);
                    var hexCol = parameters[6] == "-" ? string.Empty : parameters[6];
                    var opacity = Parser.TryParse(parameters[7], 1f);
                    var thickness = Parser.TryParse(parameters[8], 1f);
                    var fixedCamera = Parser.TryParse(parameters[9], false);

                    foreach (var selectable in selectables)
                    {
                        if (selectable is not TimelineMarker timelineMarker || !timelineMarker.Marker)
                            continue;

                        var annotation = new Annotation();
                        annotation.color = col;
                        annotation.hexColor = hexCol;
                        annotation.opacity = opacity;
                        annotation.thickness = thickness;
                        annotation.fixedCamera = fixedCamera;
                        annotation.DrawBox(pos, sca, rot);
                        timelineMarker.Marker.annotations.Add(annotation);
                    }
                }
            }

            /// <summary>
            /// Draws a polygon in the annotations of all selected markers.
            /// </summary>
            public class DrawShapeMarkerAnnotation : ActionParameter
            {
                public override string Name => "draw_shape";

                public override int ParameterCount => 11;

                public override SelectableType RequiredSelectionType => SelectableType.Markers;

                public override string Description => "Draws a polygon in the annotations of all selected markers.";

                public override string AddToAutocomplete => "draw_shape 32 0 0 10 10 0 0 - 1 4 false";

                public override void Run(IEnumerable<ISelectable> selectables, string[] parameters)
                {
                    var count = Parser.TryParse(parameters[0], 32);
                    var pos = new Vector2(Parser.TryParse(parameters[1], 0f), Parser.TryParse(parameters[2], 0f));
                    var sca = new Vector2(Parser.TryParse(parameters[3], 1f), Parser.TryParse(parameters[4], 1f));
                    var rot = Parser.TryParse(parameters[5], 0f);
                    var col = Parser.TryParse(parameters[6], 0);
                    var hexCol = parameters[7] == "-" ? string.Empty : parameters[7];
                    var opacity = Parser.TryParse(parameters[8], 1f);
                    var thickness = Parser.TryParse(parameters[9], 1f);
                    var fixedCamera = Parser.TryParse(parameters[10], false);

                    foreach (var selectable in selectables)
                    {
                        if (selectable is not TimelineMarker timelineMarker || !timelineMarker.Marker)
                            continue;

                        var annotation = new Annotation();
                        annotation.color = col;
                        annotation.hexColor = hexCol;
                        annotation.opacity = opacity;
                        annotation.thickness = thickness;
                        annotation.fixedCamera = fixedCamera;
                        annotation.DrawShape(count, pos, sca, rot);
                        timelineMarker.Marker.annotations.Add(annotation);
                    }
                }
            }

            /// <summary>
            /// Draws a line in the annotations of all selected markers.
            /// </summary>
            public class DrawLineMarkerAnnotation : ActionParameter
            {
                public override string Name => "draw_line";

                public override int ParameterCount => 9;

                public override SelectableType RequiredSelectionType => SelectableType.Markers;

                public override string Description => "Draws a line in the annotations of all selected markers.";

                public override string AddToAutocomplete => "draw_line 0 0 10 0 0 - 1 4 false";

                public override void Run(IEnumerable<ISelectable> selectables, string[] parameters)
                {
                    var startPos = new Vector2(Parser.TryParse(parameters[0], 0f), Parser.TryParse(parameters[1], 0f));
                    var endPos = new Vector2(Parser.TryParse(parameters[2], 0f), Parser.TryParse(parameters[3], 0f));
                    var col = Parser.TryParse(parameters[4], 0);
                    var hexCol = parameters[5] == "-" ? string.Empty : parameters[5];
                    var opacity = Parser.TryParse(parameters[6], 1f);
                    var thickness = Parser.TryParse(parameters[7], 1f);
                    var fixedCamera = Parser.TryParse(parameters[8], false);

                    foreach (var selectable in selectables)
                    {
                        if (selectable is not TimelineMarker timelineMarker || !timelineMarker.Marker)
                            continue;

                        var annotation = new Annotation();
                        annotation.color = col;
                        annotation.hexColor = hexCol;
                        annotation.opacity = opacity;
                        annotation.thickness = thickness;
                        annotation.fixedCamera = fixedCamera;
                        annotation.DrawLine(startPos, endPos);
                        timelineMarker.Marker.annotations.Add(annotation);
                    }
                }
            }

            #endregion

            #region Beatmap Object

            /// <summary>
            /// Opens the object search popup and displays all selected objects in the list.
            /// </summary>
            public class ShowObjectListParameter : ActionParameter
            {
                public override string Name => "show_object_list";

                public override SelectableType RequiredSelectionType => SelectableType.Objects;

                public override string Description => "Opens the object search popup and displays all selected objects in the list.";

                public override void Run(IEnumerable<ISelectable> selectables, string[] parameters)
                {
                    if (CurrentType == SelectableType.Objects)
                        ObjectEditor.inst.ShowObjectSearch(x => EditorTimeline.inst.SetCurrentObject(EditorTimeline.inst.GetTimelineObject(x), Input.GetKey(KeyCode.LeftControl)),
                            beatmapObjects: selectables.Where(x => x is TimelineObject timelineObject && timelineObject.isBeatmapObject).Select(x => ((TimelineObject)x).GetData<BeatmapObject>()).ToList());
                }
            }

            #endregion

            #region Level

            /// <summary>
            /// Opens the first selected level.
            /// </summary>
            public class OpenFirstParameter : ActionParameter
            {
                public override string Name => "open_first";

                public override SelectableType RequiredSelectionType => SelectableType.Levels;

                public override string Description => "Opens the first selected level.";

                public override void Run(IEnumerable<ISelectable> selectables, string[] parameters)
                {
                    foreach (var selectable in selectables)
                    {
                        if (selectable is LevelPanel levelPanel && levelPanel.Item)
                        {
                            EditorLevelManager.inst.LoadLevel(levelPanel);
                            return;
                        }
                    }
                }
            }

            /// <summary>
            /// Opens the last selected level.
            /// </summary>
            public class OpenLastParameter : ActionParameter
            {
                public override string Name => "open_last";

                public override SelectableType RequiredSelectionType => SelectableType.Levels;

                public override string Description => "Opens the last selected level.";

                public override void Run(IEnumerable<ISelectable> selectables, string[] parameters)
                {
                    LevelPanel toOpen = null;
                    foreach (var selectable in selectables)
                    {
                        if (selectable is LevelPanel levelPanel && levelPanel.Item)
                            toOpen = levelPanel;
                    }
                    if (toOpen)
                        EditorLevelManager.inst.LoadLevel(toOpen);
                }
            }

            /// <summary>
            /// Combines all selected levels.
            /// </summary>
            public class CombineLevelParameter : ActionParameter
            {
                public override string Name => "combine";

                public override int ParameterCount => 1;

                public override SelectableType RequiredSelectionType => SelectableType.Levels;

                public override string Description => "Combines all selected levels.";

                public override void Run(IEnumerable<ISelectable> selectables, string[] parameters)
                {
                    var name = parameters[0];
                    if (!string.IsNullOrEmpty(name))
                        EditorLevelManager.inst.Combine(RTFile.ValidateDirectory(name), selectables.SelectWhere(x => x is LevelPanel, x => x as LevelPanel), EditorLevelManager.inst.LoadLevels);
                }
            }

            /// <summary>
            /// Creates a level collection based on all selected levels.
            /// </summary>
            public class CreateLevelCollectionParameter : ActionParameter
            {
                public override string Name => "create_level_collection";

                public override int ParameterCount => 1;

                public override SelectableType RequiredSelectionType => SelectableType.Levels;

                public override string Description => "Creates a level collection based on all selected levels.";

                public override void Run(IEnumerable<ISelectable> selectables, string[] parameters)
                {
                    var name = parameters[0];
                    if (string.IsNullOrEmpty(name))
                        return;

                    var selectedLevels = selectables.SelectWhere(x => x is LevelPanel levelPanel && levelPanel.Item, x => ((LevelPanel)x).Item).ToList();
                    if (selectedLevels.IsEmpty())
                        return;

                    var collection = EditorLevelManager.inst.CreateNewLevelCollection(name, selectedLevels);
                    collection.Save();
                    EditorLevelManager.inst.LoadLevelCollections();
                }
            }

            #endregion

            #region Prefab

            /// <summary>
            /// Imports all selected prefabs into the level.
            /// </summary>
            public class ImportPrefabParameter : ActionParameter
            {
                public override string Name => "import";

                public override SelectableType RequiredSelectionType => SelectableType.ExternalPrefabs;

                public override string Description => "Imports all selected prefabs into the level.";

                public override void Run(IEnumerable<ISelectable> selectables, string[] parameters)
                {
                    int count = 0;
                    foreach (var selectable in selectables)
                    {
                        if (selectable is PrefabPanel prefabPanel && prefabPanel.Item && prefabPanel.IsExternal)
                        {
                            var newPrefab = prefabPanel.Item.Copy();
                            RTPrefabEditor.inst.ValidateDuplicateName(newPrefab);
                            GameData.Current.prefabs.Add(newPrefab);
                            count++;
                        }
                    }
                    if (count > 0)
                        RTPrefabEditor.inst.RefreshInternalPrefabs();
                }
            }

            /// <summary>
            /// Imports all selected prefabs into the level, but doesn't allow for duplicates.
            /// </summary>
            public class ImportPrefabOnceParameter : ActionParameter
            {
                public override string Name => "import_once";

                public override SelectableType RequiredSelectionType => SelectableType.ExternalPrefabs;

                public override string Description => "Imports all selected prefabs into the level, but doesn't allow for duplicates.";

                public override void Run(IEnumerable<ISelectable> selectables, string[] parameters)
                {
                    int count = 0;
                    foreach (var selectable in selectables)
                    {
                        if (selectable is PrefabPanel prefabPanel && prefabPanel.Item && prefabPanel.IsExternal)
                        {
                            var newPrefab = prefabPanel.Item.Copy();
                            if (GameData.Current.prefabs.Has(x => x.name == newPrefab.name))
                                continue;
                            GameData.Current.prefabs.Add(newPrefab);
                            count++;
                        }
                    }
                    if (count > 0)
                        RTPrefabEditor.inst.RefreshInternalPrefabs();
                }
            }

            /// <summary>
            /// Updates all associated prefabs.
            /// </summary>
            public class ImportPrefabUpdateParameter : ActionParameter
            {
                public override string Name => "import_update";

                public override SelectableType RequiredSelectionType => SelectableType.ExternalPrefabs;

                public override string Description => "Updates all associated prefabs.";

                public override void Run(IEnumerable<ISelectable> selectables, string[] parameters)
                {
                    int count = 0;
                    foreach (var selectable in selectables)
                    {
                        if (selectable is PrefabPanel prefabPanel && prefabPanel.Item && prefabPanel.IsExternal)
                        {
                            RTPrefabEditor.inst.UpdateLevelPrefab(prefabPanel.Item.Copy());
                            count++;
                        }
                    }
                    if (count > 0)
                        RTPrefabEditor.inst.RefreshInternalPrefabs();
                }
            }

            /// <summary>
            /// Creates a new prefab based on all selected objects.
            /// </summary>
            public class CreatePrefabParameter : ActionParameter
            {
                public override string Name => "create_prefab";

                public override int ParameterCount => 4;

                public override SelectableType RequiredSelectionType => SelectableType.Objects;

                public override string Description => "Creates a new prefab based on all selected objects.";

                public override string AddToAutocomplete => "create_prefab internal \"New Prefab\" \"Bombs\" 0";

                // select objects name \"prefab me\" -> log notify_count create_prefab internal \"New Prefab\" \"Bombs\" 0
                public override void Run(IEnumerable<ISelectable> selectables, string[] parameters)
                {
                    var timelineObjects = selectables.SelectWhere(x => x is TimelineObject, x => x as TimelineObject);

                    var selectedBeatmapObjects = timelineObjects.Where(x => x.isBeatmapObject).ToList();
                    var selectedBackgroundObjects = timelineObjects.Where(x => x.isBackgroundObject).ToList();
                    var selectedPrefabObjects = timelineObjects.Where(x => x.isPrefabObject).ToList();
                    if (selectedBeatmapObjects.IsEmpty() && selectedBackgroundObjects.IsEmpty() && selectedPrefabObjects.IsEmpty())
                    {
                        EditorManager.inst.DisplayNotification("Can't save prefab without any objects in it!", 2f, EditorManager.NotificationType.Error);
                        return;
                    }

                    var createInternal = Parser.TryParse(parameters[0], true, ObjectSource.External) == ObjectSource.Internal;
                    var name = parameters[1];
                    if (string.IsNullOrEmpty(name))
                    {
                        EditorManager.inst.DisplayNotification("Can't save prefab without a name!", 2f, EditorManager.NotificationType.Error);
                        return;
                    }

                    var typeID = parameters[2];
                    if (RTPrefabEditor.inst.prefabTypes.TryFind(x => x.name == typeID, out PrefabType prefabType))
                        typeID = prefabType.id;
                    var offset = Parser.TryParse(parameters[3], 0f);

                    var beatmapObjects = selectedBeatmapObjects.Select(x => x.GetData<BeatmapObject>()).ToList();
                    var prefabObjects = selectedPrefabObjects.Select(x => x.GetData<PrefabObject>()).ToList();
                    var backgroundObjects = selectedBackgroundObjects.Select(x => x.GetData<BackgroundObject>()).ToList();

                    var prefab = new Prefab(
                        name,
                        0,
                        offset,
                        beatmapObjects,
                        prefabObjects,
                        null,
                        backgroundObjects);

                    foreach (var prefabObject in prefabObjects)
                    {
                        var otherPefab = prefabObject.GetPrefab();
                        if (otherPefab && !prefab.prefabs.Has(x => x.id == otherPefab.id))
                            prefab.prefabs.Add(otherPefab.Copy(false));
                    }

                    prefab.creator = CoreConfig.Instance.DisplayName.Value;
                    prefab.description = string.Empty;
                    prefab.typeID = typeID;
                    prefab.beatmapThemes = new List<BeatmapTheme>();
                    prefab.modifierBlocks = new List<ModifierBlock>();
                    prefab.assets.sprites = new List<SpriteAsset>();

                    foreach (var beatmapObject in prefab.beatmapObjects)
                    {
                        if (beatmapObject.shape == 6 && !string.IsNullOrEmpty(beatmapObject.text) && GameData.Current.assets.sprites.TryFind(x => x.name == beatmapObject.text, out SpriteAsset spriteAsset) && !prefab.assets.sprites.Has(x => x.name == spriteAsset.name))
                            prefab.assets.sprites.Add(spriteAsset.Copy());
                    }

                    foreach (var backgroundObject in prefab.backgroundObjects)
                    {
                        if (backgroundObject.shape == 6 && !string.IsNullOrEmpty(backgroundObject.text) && GameData.Current.assets.sprites.TryFind(x => x.name == backgroundObject.text, out SpriteAsset spriteAsset) && !prefab.assets.sprites.Has(x => x.name == spriteAsset.name))
                            prefab.assets.sprites.Add(spriteAsset.Copy());
                    }

                    if (createInternal)
                    {
                        EditorManager.inst.DisplayNotification($"Saving Internal Prefab [{prefab.name}] to level...", 1.5f, EditorManager.NotificationType.Warning);
                        RTPrefabEditor.inst.ImportPrefabIntoLevel(prefab);
                        EditorManager.inst.DisplayNotification($"Saved Internal Prefab [{prefab.name}]!", 2f, EditorManager.NotificationType.Success);
                    }
                    else
                        RTPrefabEditor.inst.SavePrefab(prefab);

                    RTPrefabEditor.inst.PrefabCreatorDialog.Close();
                    RTPrefabEditor.inst.OpenPopup();

                    if (prefab.prefabPanel)
                        RTPrefabEditor.inst.OpenPrefabEditorDialog(prefab.prefabPanel);
                }
            }

            #endregion

            #region Modifier

            /// <summary>
            /// Sets the prefab group only state of all modifiers in all selected objects.
            /// </summary>
            public class SetPrefabGroupOnlyParameter : ActionParameter
            {
                public override string Name => "prefab_group_only";

                public override int ParameterCount => 1;

                public override SelectableType RequiredSelectionType => SelectableType.Objects;

                public override string Description => "Sets the prefab group only state of all modifiers in all selected objects.";

                public override void Run(IEnumerable<ISelectable> selectables, string[] parameters)
                {
                    var prefabGroupOnly = Parser.TryParse(parameters[0], false);
                    foreach (var selectable in selectables)
                        if (selectable is TimelineObject timelineObject && timelineObject.TryGetData(out IModifyable modifyable))
                            modifyable.Modifiers.ForLoop(modifier =>
                            {
                                if (ModifiersHelper.IsGroupModifier(modifier.Name))
                                    modifier.prefabInstanceOnly = prefabGroupOnly;
                            });
                }
            }

            /// <summary>
            /// Swaps the prefab group only state of all modifiers in all selected objects.
            /// </summary>
            public class SwapPrefabGroupOnlyParameter : ActionParameter
            {
                public override string Name => "swap_prefab_group_only";

                public override SelectableType RequiredSelectionType => SelectableType.Objects;

                public override string Description => "Swaps the prefab group only state of all modifiers in all selected objects.";

                public override void Run(IEnumerable<ISelectable> selectables, string[] parameters)
                {
                    foreach (var selectable in selectables)
                        if (selectable is TimelineObject timelineObject && timelineObject.TryGetData(out IModifyable modifyable))
                            modifyable.Modifiers.ForLoop(modifier =>
                            {
                                if (ModifiersHelper.IsGroupModifier(modifier.Name))
                                    modifier.prefabInstanceOnly = !modifier.prefabInstanceOnly;
                            });
                }
            }

            #endregion
            
            #endregion

            #endregion
        }

        /// <summary>
        /// Sets a global runtime variable.
        /// </summary>
        public class SetRuntimeVariable : ExampleCommand
        {
            public override string Name => "set_runtime_variable";

            public override bool Usable => ProjectArrhythmia.State.InEditor;

            public override string Pattern => "set_runtime_variable \"var_name\" \"10\"";

            public override string Description => "Sets a global runtime variable.";

            public override void ConsumeInput(string input, string[] split)
            {
                var index = 1;
                var key = GetQuoteParameter(split, ref index);
                var value = GetQuoteParameter(split, ref index);
                if (!string.IsNullOrEmpty(key))
                    RTLevel.Current.variables[key] = value;
            }
        }

        /// <summary>
        /// Removes a global runtime variable.
        /// </summary>
        public class RemoveRuntimeVariable : ExampleCommand
        {
            public override string Name => "remove_runtime_variable";

            public override bool Usable => ProjectArrhythmia.State.InEditor;

            public override string Pattern => "remove_runtime_variable \"var_name\"";

            public override string Description => "Removes a global runtime variable.";

            public override void ConsumeInput(string input, string[] split)
            {
                var index = 1;
                var key = GetQuoteParameter(split, ref index);
                if (!string.IsNullOrEmpty(key))
                    RTLevel.Current.variables.Remove(key);
            }
        }

        /// <summary>
        /// Clears all global runtime variables.
        /// </summary>
        public class ClearRuntimeVariables : ExampleCommand
        {
            public override string Name => "clear_runtime_variables";

            public override bool Usable => ProjectArrhythmia.State.InEditor;

            public override string Pattern => "clear_runtime_variables";

            public override string Description => "Clears all global runtime variables.";

            public override void ConsumeInput(string input, string[] split) => RTLevel.Current.variables.Clear();
        }

        #endregion

        #region Misc

        /// <summary>
        /// Reads an array of commands from a text file. Must be from an asset pack.
        /// </summary>
        public class ReadFileCommand : ExampleCommand
        {
            public override string Name => "read_file";

            public override string Pattern => "read_file \"core/func/multi.txt\"";

            public override string Description => "Reads an array of commands from a text file. Must be from an asset pack.";

            public override void ConsumeInput(string input, string[] split)
            {
                int index = 1;
                if (AssetPack.TryReadFromFile(GetQuoteParameter(split, ref index), out string file))
                    Run(RTString.GetLines(file));
            }
        }

        /// <summary>
        /// Saves a custom command line.
        /// </summary>
        public class SaveCustomCommand : ExampleCommand
        {
            public override string Name => "save_custom_command";

            public override string Pattern => "save_custom_command name description command";

            public override string Description => "Saves a custom command line.";

            public override void ConsumeInput(string input, string[] split)
            {
                int index = 1;
                var name = GetQuoteParameter(split, ref index);
                var description = GetQuoteParameter(split, ref index);
                var commandLine = string.Empty;
                for (int i = index; i < split.Length; i++)
                {
                    commandLine += split[i];
                    if (i != split.Length - 1)
                        commandLine += " ";
                }
                commands.Add(new CustomCommand(name, description, commandLine));
                Example.Current?.commands?.SetupCommandsAutocomplete();
                Example.Current?.brain?.SaveMemory();
            }
        }

        /// <summary>
        /// Removes a custom command from the commands list.
        /// </summary>
        public class RemoveCustomCommand : ExampleCommand
        {
            public override string Name => "remove_custom_command";

            public override string Pattern => "remove_custom_command name";

            public override string Description => "Removes a custom command from the commands list.";

            public override void ConsumeInput(string input, string[] split)
            {
                var name = split[1];
                commands.RemoveAll(x => x is CustomCommand customCommand && customCommand.Name == name);
                Example.Current?.commands?.SetupCommandsAutocomplete();
                Example.Current?.brain?.SaveMemory();
            }
        }

        /// <summary>
        /// Clears all custom commands from the commands list.
        /// </summary>
        public class ClearCustomCommands : ExampleCommand
        {
            public override string Name => "clear_custom_commands";

            public override string Pattern => "clear_custom_commands";

            public override string Description => "Clears all custom commands from the commands list.";

            public override void ConsumeInput(string input, string[] split)
            {
                commands.RemoveAll(x => x is CustomCommand customCommand);
                Example.Current?.commands?.SetupCommandsAutocomplete();
                Example.Current?.brain?.SaveMemory();
            }
        }

        /// <summary>
        /// Edits an existing custom command line.
        /// </summary>
        public class EditCustomCommand : ExampleCommand
        {
            public override string Name => "edit_custom_command";

            public override string Pattern => "edit_custom_command name description command";

            public override string Description => "Edits an existing custom command line.";

            public override void ConsumeInput(string input, string[] split)
            {
                int index = 1;
                var name = GetQuoteParameter(split, ref index);
                var description = GetQuoteParameter(split, ref index);
                var commandLine = string.Empty;
                for (int i = index; i < split.Length; i++)
                {
                    commandLine += split[i];
                    if (i != split.Length - 1)
                        commandLine += " ";
                }
                if (commands.TryFind(x => x is CustomCommand customCommand && customCommand.Name == name, out ExampleCommand command))
                {
                    var customCommand = (CustomCommand)command;
                    customCommand.description = description;
                    customCommand.CommandLine = commandLine;
                }
                Example.Current?.commands?.SetupCommandsAutocomplete();
                Example.Current?.brain?.SaveMemory();
            }
        }

        /// <summary>
        /// Represents a custom command line.
        /// </summary>
        public class CustomCommand : ExampleCommand, IJSON
        {
            #region Constructors

            public CustomCommand() { }

            public CustomCommand(string name, string description, string commandLine)
            {
                this.name = name;
                this.description = description;
                CommandLine = commandLine;
            }

            #endregion

            #region Values

            public override string Name => name;

            public override string Description => description;

            public override string AddToAutocomplete => InputDataManager.inst.editorActions.MultiSelect.IsPressed ? CommandLine : base.AddToAutocomplete;

            /// <summary>
            /// Command to add to the input.
            /// </summary>
            public string CommandLine { get; set; }

            /// <summary>
            /// Name of the parameter.
            /// </summary>
            public string name;

            /// <summary>
            /// Description of the parameter.
            /// </summary>
            public string description;

            public bool ShouldSerialize => !string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(CommandLine);

            #endregion

            #region Functions

            public override void ConsumeInput(string input, string[] split) => Run(CommandLine);

            public void ReadJSON(JSONNode jn)
            {
                name = jn["name"];
                description = jn["desc"];
                CommandLine = jn["command"];
            }

            public JSONNode ToJSON()
            {
                var jn = Parser.NewJSONObject();

                jn["name"] = name;
                if (!string.IsNullOrEmpty(description))
                    jn["desc"] = description;
                jn["command"] = CommandLine;

                return jn;
            }

            #endregion
        }

        #endregion

        #region Parameters

        /// <summary>
        /// The base parameters.
        /// </summary>
        public abstract class ParameterBase
        {
            #region Values

            /// <summary>
            /// Name of the parameter.
            /// </summary>
            public abstract string Name { get; }

            /// <summary>
            /// Amount of parameters.
            /// </summary>
            public virtual int ParameterCount => 0;

            /// <summary>
            /// If quotes are required.
            /// </summary>
            public virtual bool RequireQuotes => false;

            /// <summary>
            /// If brackets are required around the parameters.
            /// </summary>
            public virtual int BracketsType => 0;

            /// <summary>
            /// Description of the parameter.
            /// </summary>
            public abstract string Description { get; }

            /// <summary>
            /// Text to add to the autocomplete when selected.
            /// </summary>
            public virtual string AddToAutocomplete => Name;

            #endregion

            #region Functions

            /// <summary>
            /// Gets the parameters from the base parameters.
            /// </summary>
            /// <param name="split">Base parameters.</param>
            /// <param name="i">Reference index.</param>
            /// <returns>Returns an array of parameters for a function.</returns>
            public string[] GetParameters(string[] split, ref int i)
            {
                string[] parameters = null;
                // handle quotes (aka several phrase parameters that count as one)
                if (RequireQuotes)
                {
                    var quoteCount = 0;
                    var result = new List<string>();
                    var list = new List<string>();
                    i++;
                    while (i < split.Length)
                    {
                        list.Add(split[i]);
                        if (split[i].EndsWith('"'))
                        {
                            result.Add(GetJoinedQuoteParameter(list.ToArray()));
                            list.Clear();
                            if (ParameterCount == 0 || quoteCount >= ParameterCount - 1)
                                break;
                            quoteCount++;
                        }
                        i++;
                    }
                    parameters = result.ToArray();
                }
                else if (BracketsType > 0)
                {
                    var list = new List<string>();
                    i++;
                    while (i < split.Length)
                    {
                        var s = split[i];
                        var varName = s.TrimStart('[').TrimEnd(']');
                        if (variables.TryFind(x => x.Name == varName, out VariableParameter parameter))
                        {
                            parameter.variables = GetDictionary();
                            list.Add(parameter.Get(parameter.GetParameters(split, ref i)));
                        }
                        else
                        {
                            var v = GetVariable(varName);
                            if (s.StartsWith("["))
                                v = "[" + v;
                            if (s.EndsWith(']'))
                                v += "]";

                            list.Add(v);
                            if (s.EndsWith(BracketsType == 1 ? ')' : ']'))
                                break;
                        }
                        i++;
                    }
                    parameters = list.ToArray();
                }
                else if (ParameterCount > 0)
                {
                    var list = new List<string>();
                    i++;
                    int paramIndex = 0;
                    while (paramIndex < ParameterCount && i < split.Length)
                    {
                        var s = split[i];
                        // means a long string is being specified.
                        if (s.StartsWith("\""))
                        {
                            var join = new List<string>();
                            while (i < split.Length)
                            {
                                join.Add(split[i]);
                                if (split[i].EndsWith('"'))
                                {
                                    list.Add(GetJoinedQuoteParameter(join.ToArray()));
                                    join.Clear();
                                    break;
                                }
                                i++;
                            }
                            // in case we reached the end with no quotes
                            if (!join.IsEmpty())
                                list.Add(GetJoinedQuoteParameter(join.ToArray()));
                            paramIndex++;
                            if (paramIndex < ParameterCount)
                                i++;
                            continue;
                        }

                        if (variables.TryFind(x => x.Name == s, out VariableParameter parameter))
                        {
                            parameter.variables = GetDictionary();
                            list.Add(parameter.Get(parameter.GetParameters(split, ref i)));
                        }
                        else
                            list.Add(GetVariable(s));
                        paramIndex++;
                        if (paramIndex < ParameterCount)
                            i++;
                    }
                    parameters = list.ToArray();
                }
                return parameters;
            }

            /// <summary>
            /// Gets a joined parameter where a parameter has spaces.
            /// </summary>
            /// <param name="parameters">Paramaters to join.</param>
            /// <returns>Returns a joined parameters.</returns>
            public string GetJoinedQuoteParameter(string[] parameters) => GetVariable(string.Join(" ", parameters).TrimStart('"').TrimEnd('"'));

            /// <summary>
            /// Gets a joined parameter where a parameter has spaces.
            /// </summary>
            /// <param name="parameters">Paramaters to join.</param>
            /// <returns>Returns a joined parameters.</returns>
            public string GetJoinedBracketsParameter(string[] parameters) => GetVariable(string.Join(" ", parameters).TrimStart('(').TrimEnd(')'));

            /// <summary>
            /// Gets a joined parameter where a parameter has spaces.
            /// </summary>
            /// <param name="parameters">Paramaters to join.</param>
            /// <returns>Returns a joined parameters.</returns>
            public string GetJoinedSquareBracketsParameter(string[] parameters) => GetVariable(string.Join(" ", parameters).TrimStart('[').TrimEnd(']'));

            /// <summary>
            /// Gets a global variable.
            /// </summary>
            /// <param name="name">Name of the variable.</param>
            /// <returns>Returns the variable.</returns>
            public virtual string GetVariable(string name) => name switch
            {
                "current_time" => AudioManager.inst.CurrentAudioSource.time.ToString(),
                "current_layer" => EditorTimeline.inst?.Layer.ToString(),
                "max_bin" => EditorTimeline.MAX_BINS.ToString(),
                "default_bin" => EditorTimeline.DEFAULT_BIN_COUNT.ToString(),
                "default_object_name" => BeatmapObject.DEFAULT_OBJECT_NAME,
                _ => Format(name),
            };

            string Format(string input)
            {
                foreach (var variable in GetVariables())
                    input = input.Replace("{" + variable.Item1 + "}", variable.Item2);
                return input;
            }

            /// <summary>
            /// Gets the array of global variables.
            /// </summary>
            /// <returns>Returns the array of global variables.</returns>
            public virtual IEnumerable<(string, string)> GetVariables()
            {
                yield return ("current_time", AudioManager.inst.CurrentAudioSource.time.ToString());
                if (ProjectArrhythmia.State.InEditor)
                {
                    yield return ("current_layer", EditorTimeline.inst.Layer.ToString());
                    yield return ("max_bin", EditorTimeline.MAX_BINS.ToString());
                    yield return ("default_bin", EditorTimeline.DEFAULT_BIN_COUNT.ToString());
                    yield return ("default_object_name", BeatmapObject.DEFAULT_OBJECT_NAME);
                }
            }

            /// <summary>
            /// Converts <see cref="GetVariables"/> to a dictionary.
            /// </summary>
            /// <returns>Dictionary of variables.</returns>
            public Dictionary<string, string> GetDictionary()
            {
                var dictionary = new Dictionary<string, string>();
                foreach (var variable in GetVariables())
                    dictionary.TryAdd(variable.Item1, variable.Item2);
                return dictionary;
            }

            #endregion
        }

        /// <summary>
        /// Represents a variable.
        /// </summary>
        public abstract class VariableParameter : ParameterBase
        {
            /// <summary>
            /// Dictionary of possible variables.
            /// </summary>
            public Dictionary<string, string> variables = new Dictionary<string, string>();

            public override IEnumerable<(string, string)> GetVariables()
            {
                foreach (var variable in variables)
                    yield return (variable.Key, variable.Value);
                foreach (var variable in base.GetVariables())
                    yield return variable;
            }

            /// <summary>
            /// Gets a variable from the array of parameters.
            /// </summary>
            /// <param name="parameters">Array of parameters.</param>
            /// <returns>Returns a variable from the array of parameters.</returns>
            public abstract string Get(string[] parameters);
        }

        /// <summary>
        /// Evaluates a math function.
        /// </summary>
        public class EvaluateParameter : VariableParameter
        {
            public override string Name => "evaluate";

            public override int BracketsType => 1;

            public override string Description => "Evaluates a math function.";

            public override string Get(string[] parameters)
            {
                var numberVariables = new Dictionary<string, float>();
                foreach (var variable in GetVariables())
                {
                    if (float.TryParse(variable.Item2, out float num))
                        numberVariables[variable.Item1] = num;
                }
                var input = GetJoinedBracketsParameter(parameters);
                return RTMath.Parse(input, numberVariables, new Dictionary<string, ILMath.MathFunction>
                {
                    { "snapBPM", parameters => parameters.Length > 3 ? RTEditor.SnapToBPM((float)parameters[0], (float)parameters[1], (float)parameters[2], (float)parameters[3]) : RTEditor.SnapToBPM((float)parameters[0]) },
                }).ToString();
            }
        }

        /// <summary>
        /// Sets a list of possible values.
        /// </summary>
        public class ValueListParameter : VariableParameter
        {
            public override string Name => "value_list";

            public override int BracketsType => 2;

            public override string Description => "Gets a value from a list.";

            public override string AddToAutocomplete => "value_list [0 value1 value2]";

            public override string Get(string[] parameters)
            {
                var index = Parser.TryParse(parameters[0].TrimStart('['), 0);
                if (!parameters.TryGetAt(index + 1, out string value))
                    return string.Empty;
                if (index + 1 == parameters.Length - 1)
                    value = value.TrimEnd(']');
                return value;
            }
        }

        /// <summary>
        /// Parameter that represents an enum value.
        /// </summary>
        /// <typeparam name="T">Type of the parameter.</typeparam>
        public class TypeEnumParameter<T> : ParameterBase where T : Enum
        {
            public TypeEnumParameter(T type, string description, string addToAutocomplete = null)
            {
                this.type = type;
                this.description = description;
                this.addToAutocomplete = addToAutocomplete;
            }

            public override string Name => RTString.SplitWords(type.ToString()).ToLower().Replace(" ", "_");

            public override string Description => description;

            public override string AddToAutocomplete => addToAutocomplete ?? base.AddToAutocomplete;

            /// <summary>
            /// Enum value.
            /// </summary>
            public T type;

            string description;
            string addToAutocomplete;
        }

        /// <summary>
        /// Represents a generic parameter to display.
        /// </summary>
        public class GenericParameter : ParameterBase
        {
            public GenericParameter(string name, string description, string addToAutocomplete = null)
            {
                this.name = name;
                this.description = description;
                this.addToAutocomplete = addToAutocomplete;
            }

            public override string Name => name;

            public override string Description => description;

            public override string AddToAutocomplete => addToAutocomplete ?? base.AddToAutocomplete;

            string name;

            string description;

            string addToAutocomplete;
        }

        #endregion

        #endregion
    }
}
