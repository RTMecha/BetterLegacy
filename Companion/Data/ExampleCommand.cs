using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

using LSFunctions;

using SimpleJSON;

using BetterLegacy.Companion.Entity;
using BetterLegacy.Core;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Editor.Data;
using BetterLegacy.Editor.Data.Elements;
using BetterLegacy.Editor.Data.Timeline;
using BetterLegacy.Editor.Managers;

namespace BetterLegacy.Companion.Data
{
    /// <summary>
    /// Represents a way to communicate with Example.
    /// </summary>
    public class ExampleCommand
    {
        #region Constructors

        public ExampleCommand() { }

        public ExampleCommand(string name, string desc, bool autocomplete, Action<string> response)
        {
            this.name = name;
            this.desc = desc;
            this.autocomplete = autocomplete;
            this.response = response;
        }
        
        public ExampleCommand(string name, string desc, bool autocomplete, Action<string> response, List<Phrase> phrases) : this(name, desc, autocomplete, response)
        {
            this.phrases = phrases;
        }
        
        public ExampleCommand(string name, string desc, bool autocomplete, Action<string> response, bool requirePhrase, List<Phrase> phrases) : this(name, desc, autocomplete, response, phrases)
        {
            this.requirePhrase = requirePhrase;
        }

        #endregion

        #region Values

        /// <summary>
        /// Name of the command to display.
        /// </summary>
        public string name;

        /// <summary>
        /// Description of the command to display.
        /// </summary>
        public string desc;

        /// <summary>
        /// Function to respond to the input with.
        /// </summary>
        public Action<string> response;

        /// <summary>
        /// If the command should show up in autocomplete.
        /// </summary>
        public bool autocomplete = true;

        /// <summary>
        /// If phrases are required.
        /// </summary>
        public bool requirePhrase;

        /// <summary>
        /// List of things to say to prompt the response.
        /// </summary>
        public List<Phrase> phrases;

        #endregion

        #region Functions

        /// <summary>
        /// Checks the input for a response.
        /// </summary>
        /// <param name="input">Input text.</param>
        public void CheckResponse(string input)
        {
            if (!requirePhrase && input == name)
                response?.Invoke(input);
            else if (phrases != null && phrases.TryFind(x => x.CheckPhrase(input), out Phrase phrase))
            {
                if (phrase.isRegex)
                    response?.Invoke(input.Replace(phrase.match.Groups[0].ToString(), phrase.match.Groups[1].ToString()));
                else
                    response?.Invoke(phrase.text);
            }
        }

        /// <summary>
        /// Parses a command.
        /// </summary>
        /// <param name="jn">JSON to parse.</param>
        /// <returns>Returns a parsed command.</returns>
        public static ExampleCommand Parse(JSONNode jn)
        {
            var command = new ExampleCommand();

            if (jn["phrases"] != null)
            {
                command.phrases = new List<Phrase>();
                for (int i = 0; i < jn["phrases"].Count; i++)
                {
                    var phrase = new Phrase(jn["phrases"][i]["text"]);
                    phrase.isRegex = jn["phrases"][i]["regex"].AsBool;
                    command.phrases.Add(phrase);
                }
            }

            command.response = _val =>
            {
                if (jn["response"] != null)
                    RTCode.Evaluate($"var input = \"{_val}\";" + jn["response"]);
            };

            command.name = jn["name"];
            command.desc = jn["desc"];

            if (jn["autocomplete"] != null)
                command.autocomplete = jn["autocomplete"].AsBool;

            command.requirePhrase = jn["require_phrase"].AsBool;

            return command;
        }

        public override string ToString() => name;

        #endregion

        /// <summary>
        /// Represents a phrase prompt.
        /// </summary>
        public class Phrase
        {
            public Phrase(string text) => this.text = text;

            public Phrase(string text, bool isRegex) : this(text) => this.isRegex = isRegex;

            /// <summary>
            /// Text of the phrase.
            /// </summary>
            public string text;

            /// <summary>
            /// Does the phrase contain regex?
            /// </summary>
            public bool isRegex;

            /// <summary>
            /// Match of the phrase.
            /// </summary>
            public Match match;

            /// <summary>
            /// Checks if the phrase matches the input.
            /// </summary>
            /// <param name="input">Input text to check.</param>
            /// <returns>Returns true if the match was successful, otherwise returns false.</returns>
            public bool CheckPhrase(string input)
            {
                if (!isRegex)
                    return text.ToLower() == input.ToLower();

                var regex = new Regex(text);
                match = regex.Match(input);
                return match.Success;
            }
        }
    }

    /// <summary>
    /// Represents a new and upcoming way of communicating with Example.
    /// </summary>
    public abstract class ExampleCommandBase : Exists
    {
        /*
         examples include:
        - create marker 1 0.1
        - hello buddy
        - i love you
        - evaluate 1 + 1
        - select objects layer current_layer time lesser_equals 1 name "Object Name"
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
        /// List of registered commands.
        /// </summary>
        public static List<ExampleCommandBase> commands = new List<ExampleCommandBase>
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

            #endregion

            #region Editor Commands

            new CreateCommand(),
            new SelectCommand(),

            #endregion
        };

        #endregion

        #region Functions

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
            if (commands.TryFind(x => x.Name == name, out ExampleCommandBase command))
            {
                if (command.Usable)
                    command.ConsumeInput(input, split);
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

        #endregion

        #region Sub Classes

        #region Example Interaction Commands
        
        /// <summary>
        /// Represents a command for saying hello.
        /// </summary>
        public class HelloCommand : ExampleCommandBase
        {
            public override string Name => "hello";

            public override bool Autocomplete => false;

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
        public class MyselfCommand : ExampleCommandBase
        {
            public override string Name => "i";

            public override bool Autocomplete => false;

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
        public class EvaluateCommand : ExampleCommandBase
        {
            public override string Name => "evaluate";

            public override string Pattern => "evaluate 1 + 1";

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
        }

        /// <summary>
        /// Represents a command for dancing.
        /// </summary>
        public class DanceCommand : ExampleCommandBase
        {
            public override string Name => "dance";

            public override bool Autocomplete => false;

            public override void ConsumeInput(string input, string[] split) => Example.Current?.brain?.ForceRunAction(Example.Current?.brain?.GetAction(ExampleBrain.Actions.DANCING));
        }

        #endregion

        #region Core Commands

        public class SetSceneCommand : ExampleCommandBase
        {
            public override string Name => "setscene";

            public override string Pattern => "setscene Scene_Name";

            public override void ConsumeInput(string input, string[] split) => SceneHelper.LoadScene(Parser.TryParse(split[1], true, SceneName.Main_Menu));
        }

        #endregion

        #region Editor Commands

        /// <summary>
        /// Represents a command capable of creating objects.
        /// </summary>
        public class CreateCommand : ExampleCommandBase
        {
            public override string Name => "create";

            public override bool Usable => CoreHelper.InEditor;

            public override string Pattern => "create category [values]";

            public List<Category> categories = new List<Category>
            {
                new MarkerCategory(),
                new BeatmapObjectCategory(),
            };

            public override void ConsumeInput(string input, string[] split)
            {
                var categoryName = split[1];
                if (categories.TryFind(x => x.Name == categoryName, out Category category))
                    category.ConsumeInput(input, split);
            }

            public abstract class Category
            {
                public abstract string Name { get; }

                public abstract void ConsumeInput(string input, string[] split);
            }

            public class MarkerCategory : Category
            {
                public override string Name => "marker";

                public override void ConsumeInput(string input, string[] split)
                {
                    var count = Parser.TryParse(split.GetAtOrDefault(2, "1"), 1);
                    var distance = Parser.TryParse(split.GetAtOrDefault(3, "0.1"), 0.1f);
                    Core.Helpers.EditorHelper.CreateMarkers(count, distance);
                }
            }

            public class BeatmapObjectCategory : Category
            {
                public override string Name => "beatmap_object";

                public override void ConsumeInput(string input, string[] split)
                {

                }
            }
        }

        /// <summary>
        /// Represents a command for selecting objects.
        /// </summary>
        public class SelectCommand : ExampleCommandBase
        {
            public override string Name => "select";

            public override bool Usable => CoreHelper.InEditor;

            public override string Pattern => "select type [predicates]";

            public static SelectableType CurrentType { get; set; }

            public List<GetSelectableParameter> parameters = new List<GetSelectableParameter>
            {
                #region General

                new DefaultSelectablesParameter(),
                new SelectedParameter(),
                new UnselectedParameter(),
                new TimeComparisonParameter(),
                new IndexComparisonParameter(),
                new LockedParameter(),
                new UnlockedParameter(),
                new NameEqualsParameter(),
                new ContainsTagParameter(),

                #endregion

                #region Timeline Object

                new CurrentLayerParameter(),
                new SamePrefabGroupParameter(),
                new ReferenceTypeEqualsParameter(),
                new LayerComparisonParameter(),
                new BinComparisonParameter(),
                new CollapsedParameter(),
                new UncollapsedParameter(),

                #endregion

                #region Timeline Marker

                new ColorEqualsParameter(),

                #endregion

                #region Timeline Keyframe

                new EventTypeComparisonParameter(),

                #endregion
            };

            public List<ActionParameter> actionParameters = new List<ActionParameter>
            {
                new SetNameParameter(),
            };

            public override void ConsumeInput(string input, string[] split)
            {
                CurrentType = Parser.TryParse(split[1].ToLower().Remove("_"), true, SelectableType.Null);
                IEnumerable<ISelectable> selectables = GetSelectables(CurrentType);
                if (selectables == null)
                    return;
                bool actionMode = false;
                for (int i = 2; i < split.Length; i++)
                {
                    var s = split[i];
                    if (s == "->")
                    {
                        // convert rest of parameters to action parameters
                        actionMode = true;
                        continue;
                    }

                    if (s == "if")
                    {
                        actionMode = false;
                        continue;
                    }

                    if (s == "select")
                        actionMode = false;

                    if (actionMode)
                    {
                        if (actionParameters.TryFind(x => x.Name == s, out ActionParameter actionParameter))
                            actionParameter.Run(selectables, actionParameter.GetParameters(split, ref i));
                        continue;
                    }
                    if (parameters.TryFind(x => x.Name == s, out GetSelectableParameter parameter))
                        selectables = parameter.GetSelectables(selectables, parameter.GetParameters(split, ref i));
                }

                if (actionMode) // since we're performing an action on specific objects, we don't select them.
                    return;

                foreach (var selectable in GetSelectables(CurrentType))
                    selectable.Selected = false;
                foreach (var selectable in selectables)
                    selectable.Selected = true;
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
                _ => null,
            };

            public enum SelectableType
            {
                Null,
                Objects,
                Keyframes,
                ObjectKeyframes,
                Markers,
                Checkpoints,
                Levels,
                LevelCollections,
                ExternalPrefabs,
                InternalPrefabs,
                ExternalThemes,
                InternalThemes,
            }

            public abstract class GetSelectableParameter : ParameterBase
            {
                public abstract IEnumerable<ISelectable> GetSelectables(IEnumerable<ISelectable> selectables, string[] parameters);
            }

            public abstract class ActionParameter : ParameterBase
            {
                public abstract void Run(IEnumerable<ISelectable> selectables, string[] parameters);
            }

            #region Get

            #region General

            public class DefaultSelectablesParameter : GetSelectableParameter
            {
                public override string Name => "select";

                public override int ParameterCount => 1;

                public override IEnumerable<ISelectable> GetSelectables(IEnumerable<ISelectable> selectables, string[] parameters) => SelectCommand.GetSelectables(Parser.TryParse(parameters[0].ToLower().Remove("_"), true, SelectableType.Null));
            }

            public class SelectedParameter : GetSelectableParameter
            {
                public override string Name => "selected";

                public override IEnumerable<ISelectable> GetSelectables(IEnumerable<ISelectable> selectables, string[] parameters) => selectables.Where(x => x.Selected);
            }
            
            public class UnselectedParameter : GetSelectableParameter
            {
                public override string Name => "unselected";

                public override IEnumerable<ISelectable> GetSelectables(IEnumerable<ISelectable> selectables, string[] parameters) => selectables.Where(x => !x.Selected);
            }

            public class TimeComparisonParameter : GetSelectableParameter
            {
                public override string Name => "time";

                public override int ParameterCount => 2;

                public override IEnumerable<ISelectable> GetSelectables(IEnumerable<ISelectable> selectables, string[] parameters)
                {
                    var comparison = Parser.TryParse(parameters[0].Remove("_"), true, NumberComparison.Equals);
                    var time = Parser.TryParse(parameters[1], 0f);

                    foreach (var selectable in selectables)
                    {
                        if (selectable is TimelineObject timelineObject && comparison.Compare(timelineObject.Time, time))
                            yield return selectable;
                        if (selectable is TimelineKeyframe timelineKeyframe && comparison.Compare(timelineKeyframe.Time, time))
                            yield return selectable;
                        if (selectable is TimelineMarker timelineMarker && comparison.Compare(timelineMarker.Time, time))
                            yield return selectable;
                        if (selectable is TimelineCheckpoint timelineCheckpoint && comparison.Compare(timelineCheckpoint.Time, time))
                            yield return selectable;
                    }
                }
            }

            public class IndexComparisonParameter : GetSelectableParameter
            {
                public override string Name => "index";

                public override int ParameterCount => 1;

                public override IEnumerable<ISelectable> GetSelectables(IEnumerable<ISelectable> selectables, string[] parameters)
                {
                    var comparison = Parser.TryParse(parameters[0].Remove("_"), true, NumberComparison.Equals);
                    var index = Parser.TryParse(parameters[1], 0);

                    foreach (var selectable in selectables)
                    {
                        if (selectable is TimelineObject timelineObject && comparison.Compare(timelineObject.Index, index))
                            yield return selectable;
                        if (selectable is TimelineKeyframe timelineKeyframe && comparison.Compare(timelineKeyframe.Index, index))
                            yield return selectable;
                        if (selectable is TimelineMarker timelineMarker && comparison.Compare(timelineMarker.Index, index))
                            yield return timelineMarker;
                        if (selectable is TimelineCheckpoint timelineCheckpoint && comparison.Compare(timelineCheckpoint.Index, index))
                            yield return selectable;
                    }
                }
            }

            public class LockedParameter : GetSelectableParameter
            {
                public override string Name => "locked";

                public override IEnumerable<ISelectable> GetSelectables(IEnumerable<ISelectable> selectables, string[] parameters)
                {
                    foreach (var selectable in selectables)
                    {
                        if (selectable is TimelineObject timelineObject && timelineObject.Locked)
                            yield return selectable;
                        if (selectable is TimelineKeyframe timelineKeyframe && timelineKeyframe.Locked)
                            yield return selectable;
                    }
                }
            }
            
            public class UnlockedParameter : GetSelectableParameter
            {
                public override string Name => "unlocked";

                public override IEnumerable<ISelectable> GetSelectables(IEnumerable<ISelectable> selectables, string[] parameters)
                {
                    foreach (var selectable in selectables)
                    {
                        if (selectable is TimelineObject timelineObject && !timelineObject.Locked)
                            yield return selectable;
                        if (selectable is TimelineKeyframe timelineKeyframe && !timelineKeyframe.Locked)
                            yield return selectable;
                    }
                }
            }

            public class NameEqualsParameter : GetSelectableParameter
            {
                public override string Name => "name";

                public override int ParameterCount => 1;

                public override bool RequireQuotes => true;

                public override IEnumerable<ISelectable> GetSelectables(IEnumerable<ISelectable> selectables, string[] parameters)
                {
                    var name = GetJoinedQuoteParameter(parameters);
                    foreach (var selectable in selectables)
                    {
                        if (selectable is TimelineObject timelineObject && timelineObject.Name == name)
                            yield return selectable;
                        if (selectable is TimelineMarker timelineMarker && timelineMarker.Name == name)
                            yield return selectable;
                        if (selectable is TimelineCheckpoint timelineCheckpoint && timelineCheckpoint.Name == name)
                            yield return selectable;
                        if (selectable is LevelPanel levelPanel && (levelPanel.isFolder ? levelPanel.Name == name : levelPanel.Item?.metadata?.beatmap?.name == name))
                            yield return selectable;
                        if (selectable is LevelCollectionPanel levelCollectionPanel && (levelCollectionPanel.isFolder ? levelCollectionPanel.Name == name : levelCollectionPanel.Item.name == name))
                            yield return selectable;
                        if (selectable is PrefabPanel prefabPanel && (prefabPanel.isFolder ? prefabPanel.Name == name : prefabPanel.Item.name == name))
                            yield return selectable;
                        if (selectable is ThemePanel themePanel && (themePanel.isFolder ? themePanel.Name == name : themePanel.Item.name == name))
                            yield return selectable;
                    }
                }
            }

            public class ContainsTagParameter : GetSelectableParameter
            {
                public override string Name => "tag";

                public override bool RequireQuotes => true;

                public override IEnumerable<ISelectable> GetSelectables(IEnumerable<ISelectable> selectables, string[] parameters)
                {
                    var tag = GetJoinedQuoteParameter(parameters);
                    foreach (var selectable in selectables)
                        if (selectable is TimelineObject timelineObject && timelineObject.TryGetData(out Core.Data.Modifiers.IModifyable modifyable) && modifyable.Tags != null && modifyable.Tags.Contains(tag))
                            yield return selectable;
                }
            }

            #endregion

            #region Timeline Object

            public class CurrentLayerParameter : GetSelectableParameter
            {
                public override string Name => "current_layer";

                public override IEnumerable<ISelectable> GetSelectables(IEnumerable<ISelectable> selectables, string[] parameters) => selectables.Where(x => x is TimelineObject timelineObject && timelineObject.IsCurrentLayer);
            }

            public class SamePrefabGroupParameter : GetSelectableParameter
            {
                public override string Name => "same_prefab_group";

                public override IEnumerable<ISelectable> GetSelectables(IEnumerable<ISelectable> selectables, string[] parameters)
                {
                    var f = selectables.FirstOrDefault();
                    if (f == null || f is not TimelineObject firstObject || !firstObject.TryGetPrefabable(out IPrefabable main))
                        yield break;

                    foreach (var selectable in selectables)
                    {
                        if (selectable is TimelineObject timelineObject && timelineObject.TryGetPrefabable(out IPrefabable prefabable) && main.SamePrefabInstance(prefabable))
                            yield return selectable;
                    }
                }
            }

            public class ReferenceTypeEqualsParameter : GetSelectableParameter
            {
                public override string Name => "reference_type";

                public override int ParameterCount => 1;

                public override IEnumerable<ISelectable> GetSelectables(IEnumerable<ISelectable> selectables, string[] parameters)
                {
                    var type = Parser.TryParse(parameters[0], TimelineObject.TimelineReferenceType.Null);
                    foreach (var selectable in selectables)
                        if (selectable is TimelineObject timelineObject && timelineObject.TimelineReference == type)
                            yield return selectable;
                }
            }

            public class LayerComparisonParameter : GetSelectableParameter
            {
                public override string Name => "layer";

                public override int ParameterCount => 2;

                public override IEnumerable<ISelectable> GetSelectables(IEnumerable<ISelectable> selectables, string[] parameters)
                {
                    var comparison = Parser.TryParse(parameters[0].Remove("_"), true, NumberComparison.Equals);
                    var layer = parameters[1] == "current_layer" ? EditorTimeline.inst.Layer : Parser.TryParse(parameters[1], 0);

                    foreach (var selectable in selectables)
                        if (selectable is TimelineObject timelineObject && comparison.Compare(timelineObject.Layer, layer))
                            yield return selectable;
                }
            }

            public class BinComparisonParameter : GetSelectableParameter
            {
                public override string Name => "bin";

                public override int ParameterCount => 2;

                public override IEnumerable<ISelectable> GetSelectables(IEnumerable<ISelectable> selectables, string[] parameters)
                {
                    var comparison = Parser.TryParse(parameters[0].Remove("_"), true, NumberComparison.Equals);
                    var bin = Parser.TryParse(parameters[1], 0);

                    foreach (var selectable in selectables)
                        if (selectable is TimelineObject timelineObject && comparison.Compare(timelineObject.Bin, bin))
                            yield return selectable;
                }
            }

            public class CollapsedParameter : GetSelectableParameter
            {
                public override string Name => "collapsed";

                public override IEnumerable<ISelectable> GetSelectables(IEnumerable<ISelectable> selectables, string[] parameters) => selectables.Where(x => x is TimelineObject timelineObject && timelineObject.Collapse);
            }

            public class UncollapsedParameter : GetSelectableParameter
            {
                public override string Name => "uncollapsed";

                public override IEnumerable<ISelectable> GetSelectables(IEnumerable<ISelectable> selectables, string[] parameters) => selectables.Where(x => x is TimelineObject timelineObject && !timelineObject.Collapse);
            }

            #endregion

            #region Timeline Marker

            public class ColorEqualsParameter : GetSelectableParameter
            {
                public override string Name => "color";

                public override int ParameterCount => 1;

                public override IEnumerable<ISelectable> GetSelectables(IEnumerable<ISelectable> selectables, string[] parameters)
                {
                    var color = Parser.TryParse(parameters[0], 0);
                    foreach (var selectable in selectables)
                        if (selectable is TimelineMarker timelineMarker && timelineMarker.ColorSlot == color)
                            yield return selectable;
                }
            }

            #endregion

            #region Timeline Keyframe

            public class EventTypeComparisonParameter : GetSelectableParameter
            {
                public override string Name => "event_type";

                public override int ParameterCount => 2;

                public override IEnumerable<ISelectable> GetSelectables(IEnumerable<ISelectable> selectables, string[] parameters)
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
                        if (selectable is TimelineKeyframe timelineKeyframe && comparison.Compare(timelineKeyframe.Type, type))
                            yield return selectable;
                    }
                }
            }

            #endregion

            #endregion

            #region Action

            public class SetNameParameter : ActionParameter
            {
                public override string Name => "set_name";

                public override bool RequireQuotes => true;

                public override void Run(IEnumerable<ISelectable> selectables, string[] parameters)
                {
                    var name = GetJoinedQuoteParameter(parameters);
                    foreach (var selectable in selectables)
                    {
                        if (selectable is TimelineObject timelineObject)
                            timelineObject.SetName(name);
                        if (selectable is TimelineMarker timelineMarker)
                            timelineMarker.Name = name;
                        if (selectable is TimelineCheckpoint timelineCheckpoint)
                            timelineCheckpoint.Name = name;
                    }
                }
            }

            #endregion
        }

        #endregion

        public abstract class ParameterBase
        {
            public abstract string Name { get; }

            public virtual int ParameterCount => 0;

            public virtual bool RequireQuotes => false;

            public string[] GetParameters(string[] split, ref int i)
            {
                string[] parameters = null;
                // handle quotes (aka several phrase parameters that count as one)
                if (RequireQuotes)
                {
                    var list = new List<string>();
                    i++;
                    while (i < split.Length)
                    {
                        list.Add(split[i]);
                        if (split[i].EndsWith('"'))
                            break;
                        i++;
                    }
                    parameters = list.ToArray();
                }
                else if (ParameterCount > 0)
                {
                    parameters = new string[ParameterCount];
                    i++;
                    int paramIndex = 0;
                    while (paramIndex < ParameterCount)
                    {
                        parameters[paramIndex] = split[i];
                        i++;
                        paramIndex++;
                    }
                }
                return parameters;
            }

            public static string GetJoinedQuoteParameter(string[] parameters) => string.Join(" ", parameters).TrimStart('"').TrimEnd('"');
        }

        #endregion
    }
}
