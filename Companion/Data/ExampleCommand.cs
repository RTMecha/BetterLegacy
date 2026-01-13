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
using BetterLegacy.Core.Data.Modifiers;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Runtime;
using BetterLegacy.Editor.Data;
using BetterLegacy.Editor.Data.Dialogs;
using BetterLegacy.Editor.Data.Elements;
using BetterLegacy.Editor.Data.Planners;
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
        - create marker 10 add_time 0.1 name "test"
        - create background_object 10 start_time current_time add_editor_bin 0
        - hello buddy
        - i love you
        - evaluate 1 + 1
        - select objects layer current_layer time lesser_equals 1 name "Object Name"
        - select external_prefabs name_regex \"RT (.*?)Mecha(.*?)\" -> log import
        - select levels name \"commands\" union name \"Shockwave\" -> log notify_count combine \"TEST COMBINE COMMAND\"
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

        public static List<VariableParameter> variables = new List<VariableParameter>
        {
            new EvaluateParameter(),
        };

        #endregion

        #region Functions

        /// <summary>
        /// Gets the list of registered commands.
        /// </summary>
        /// <returns>Returns the list of registered commands.</returns>
        public static List<ExampleCommandBase> GetComands() => new List<ExampleCommandBase>
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
            if (GetComands().TryFind(x => x.Name == name, out ExampleCommandBase command))
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

        public class EditCommand : ExampleCommandBase
        {
            public override string Name => "edit";

            public override bool Usable => false;

            public override string Pattern => "create category [values]";

            public static int index;

            public static int count;

            public static CategoryType CurrentCategory { get; set; }

            public static List<EditParameter<BeatmapObject>> beatmapObjectParameters = new List<EditParameter<BeatmapObject>>
            {
                new BeatmapObjectNameParameter(),
                new ObjectStartTimeParameter<BeatmapObject>(),
                new ObjectAddStartTimeParameter<BeatmapObject>(),
                new LayerParameter<BeatmapObject>(),
                new BinParameter<BeatmapObject>(),
                new AddBinParameter<BeatmapObject>(),
                new ShapeParameter<BeatmapObject>(),
                new ShapeTextParameter<BeatmapObject>(),
                new BeatmapObjectGradientTypeParameter(),
                new BeatmapObjectRenderDepthParameter(),
                new BeatmapObjectPositionKeyframeParameter(),
                new BeatmapObjectScaleKeyframeParameter(),
                new BeatmapObjectRotationKeyframeParameter(),
                new BeatmapObjectColorKeyframeParameter(),
            };

            public static List<EditParameter<BackgroundObject>> backgroundObjectParameters = new List<EditParameter<BackgroundObject>>
            {
                new BackgroundObjectNameParameter(),
                new ObjectStartTimeParameter<BackgroundObject>(),
                new ObjectAddStartTimeParameter<BackgroundObject>(),
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

            public static List<EditParameter<PrefabObject>> prefabObjectParameters = new List<EditParameter<PrefabObject>>
            {
                new ObjectStartTimeParameter<PrefabObject>(),
                new ObjectAddStartTimeParameter<PrefabObject>(),
                new LayerParameter<PrefabObject>(),
                new BinParameter<PrefabObject>(),
                new AddBinParameter<PrefabObject>(),
                new PrefabObjectPositionParameter(),
                new PrefabObjectScaleParameter(),
                new PrefabObjectRotationParameter(),
            };

            public static List<EditParameter<Marker>> markerParameters = new List<EditParameter<Marker>>
            {
                new MarkerNameParameter(),
                new MarkerTimeParameter(),
                new MarkerAddTimeParameter(),
                new MarkerDescriptionParameter(),
                new MarkerColorParameter(),
            };

            public static List<EditParameter<Checkpoint>> checkpointParameters = new List<EditParameter<Checkpoint>>
            {
                new CheckpointNameParameter(),
                new CheckpointTimeParameter(),
                new CheckpointAddTimeParameter(),
            };

            public static List<EditParameter<EventKeyframe>> eventKeyframeParameters = new List<EditParameter<EventKeyframe>>
            {
                new EventKeframeTimeParameter(),
                new EventKeyframeValueParameter(),
                new EventKeyframeEasingParameter(),
                new EventKeyframeRelativeParameter(),
            };

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

            public override void ConsumeInput(string input, string[] split) => throw new NotImplementedException();

            public void ConsumeInput(string[] parameters, SelectCommand.SelectableType selectableType, IEnumerable<ISelectable> selectables)
            {
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
                                            Apply(1, beatmapObject, parameters, beatmapObjectParameters);
                                            RTLevel.Current?.UpdateObject(beatmapObject);
                                            break;
                                        }
                                    case TimelineObject.TimelineReferenceType.BackgroundObject: {
                                            var backgroundObject = timelineObject.GetData<BackgroundObject>();
                                            Apply(1, backgroundObject, parameters, backgroundObjectParameters);
                                            RTLevel.Current?.UpdateBackgroundObject(backgroundObject);
                                            break;
                                        }
                                    case TimelineObject.TimelineReferenceType.PrefabObject: {
                                            var prefabObject = timelineObject.GetData<PrefabObject>();
                                            Apply(1, prefabObject, parameters, prefabObjectParameters);
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
                                Apply(0, timelineKeyframe.eventKeyframe, parameters, eventKeyframeParameters);
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
                                Apply(0, timelineKeyframe.eventKeyframe, parameters, eventKeyframeParameters);
                                timelineKeyframe.Render();
                                index++;
                            }
                            EditorTimeline.inst.CurrentSelection?.Render();
                            ObjectEditor.inst.Dialog.Timeline.ResizeKeyframeTimeline(ObjectEditor.inst.Dialog.Timeline.CurrentObject);
                            break;
                        }
                    case SelectCommand.SelectableType.Markers: {
                            index = 0;
                            count = selectables.Count(x => x is TimelineMarker);
                            foreach (var selectable in selectables)
                            {
                                if (selectable is not TimelineMarker timelineMarker)
                                    continue;
                                Apply(0, timelineMarker.Marker, parameters, markerParameters);
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
                                Apply(0, timelineCheckpoint.Checkpoint, parameters, checkpointParameters);
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
                                Apply(0, modifierCard.Modifier, parameters, modifierParameters);
                                index++;
                            }
                            if (ModifiersEditorDialog.Current)
                                CoroutineHelper.StartCoroutine(ModifiersEditorDialog.Current.RenderModifiers(ModifiersEditorDialog.Current.CurrentObject));
                            break;
                        }
                }
            }

            public void Apply<T>(int startIndex, T obj, string[] split, List<EditParameter<T>> parameters)
            {
                for (int i = startIndex; i < split.Length; i++)
                {
                    var s = split[i];
                    if (parameters.TryFind(x => x.Name == s, out EditParameter<T> parameter))
                        parameter.Apply(obj, parameter.GetParameters(split, ref i));
                }
            }

            public abstract class EditParameter<T> : ParameterBase
            {
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

            public enum CategoryType
            {
                Null,
                BeatmapObject,
                BackgroundObject,
                PrefabObject,
                Marker,
                Checkpoint,
                Modifier,
            }

            #region General

            public class LayerParameter<T> : EditParameter<T> where T : IEditable
            {
                public override string Name => "editor_layer";

                public override int ParameterCount => 1;

                public override void Apply(T obj, string[] parameters) => obj.EditorData.Layer = Parser.TryParse(parameters[0], 0);
            }

            public class BinParameter<T> : EditParameter<T> where T : IEditable
            {
                public override string Name => "editor_bin";

                public override int ParameterCount => 1;

                public override void Apply(T obj, string[] parameters) => obj.EditorData.Bin = Parser.TryParse(parameters[0], 0);
            }

            public class AddBinParameter<T> : EditParameter<T> where T : IEditable
            {
                public override string Name => "add_editor_bin";

                public override int ParameterCount => 1;

                public override void Apply(T obj, string[] parameters) => obj.EditorData.Bin = Parser.TryParse(parameters[0], 0) + index;
            }

            public class ObjectStartTimeParameter<T> : EditParameter<T> where T : ILifetime
            {
                public override string Name => "start_time";

                public override int ParameterCount => 1;

                public override void Apply(T obj, string[] parameters) => obj.StartTime = Parser.TryParse(parameters[0], 0f);
            }

            public class ObjectAddStartTimeParameter<T> : EditParameter<T> where T : ILifetime
            {
                public override string Name => "add_start_time";

                public override int ParameterCount => 1;

                public override void Apply(T obj, string[] parameters) => obj.StartTime = AudioManager.inst.CurrentAudioSource.time + (Parser.TryParse(parameters[0], 0f) * index);
            }

            public class ShapeParameter<T> : EditParameter<T> where T : IShapeable
            {
                public override string Name => "shape";

                public override int ParameterCount => 2;

                public override void Apply(T obj, string[] parameters)
                {
                    obj.Shape = Parser.TryParse(parameters[0], 0);
                    obj.ShapeOption = Parser.TryParse(parameters[1], 0);
                }
            }

            public class ShapeTextParameter<T> : EditParameter<T> where T : IShapeable
            {
                public override string Name => "text";

                public override int ParameterCount => 1;

                public override void Apply(T obj, string[] parameters) => obj.Text = parameters[0];
            }

            #endregion

            #region Beatmap Object

            public class BeatmapObjectNameParameter : EditParameter<BeatmapObject>
            {
                public override string Name => "name";

                public override bool RequireQuotes => true;

                public override void Apply(BeatmapObject obj, string[] parameters) => obj.name = parameters[0];
            }

            public class BeatmapObjectGradientTypeParameter : EditParameter<BeatmapObject>
            {
                public override string Name => "gradient_type";

                public override int ParameterCount => 1;

                public override void Apply(BeatmapObject obj, string[] parameters) => obj.gradientType = Parser.TryParse(parameters[0], true, GradientType.Normal);
            }

            public class BeatmapObjectRenderDepthParameter : EditParameter<BeatmapObject>
            {
                public override string Name => "depth";

                public override int ParameterCount => 1;

                public override void Apply(BeatmapObject obj, string[] parameters) => obj.Depth = Parser.TryParse(parameters[0], 0f);
            }

            public class BeatmapObjectPositionKeyframeParameter : EditParameter<BeatmapObject>
            {
                public override string Name => "pos_kf";

                public override int BracketsType => 2;

                public override void Apply(BeatmapObject obj, string[] parameters)
                {
                    var eventKeyframe = EventKeyframe.DefaultPositionKeyframe;
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

            public class BeatmapObjectScaleKeyframeParameter : EditParameter<BeatmapObject>
            {
                public override string Name => "sca_kf";

                public override int BracketsType => 2;

                public override void Apply(BeatmapObject obj, string[] parameters)
                {
                    var eventKeyframe = EventKeyframe.DefaultScaleKeyframe;
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
            
            public class BeatmapObjectRotationKeyframeParameter : EditParameter<BeatmapObject>
            {
                public override string Name => "rot_kf";

                public override int BracketsType => 2;

                public override void Apply(BeatmapObject obj, string[] parameters)
                {
                    var eventKeyframe = EventKeyframe.DefaultRotationKeyframe;
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

            public class BeatmapObjectColorKeyframeParameter : EditParameter<BeatmapObject>
            {
                public override string Name => "col_kf";

                public override int BracketsType => 2;

                public override void Apply(BeatmapObject obj, string[] parameters)
                {
                    var eventKeyframe = EventKeyframe.DefaultColorKeyframe;
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

            public class BackgroundObjectNameParameter : EditParameter<BackgroundObject>
            {
                public override string Name => "name";

                public override bool RequireQuotes => true;

                public override void Apply(BackgroundObject obj, string[] parameters) => obj.name = parameters[0];
            }

            public class BackgroundObjectPositionParameter : EditParameter<BackgroundObject>
            {
                public override string Name => "pos";

                public override int ParameterCount => 2;

                public override void Apply(BackgroundObject obj, string[] parameters) => obj.pos = new Vector2(Parser.TryParse(parameters[0], 0f), Parser.TryParse(parameters[1], 0f));
            }

            public class BackgroundObjectScaleParameter : EditParameter<BackgroundObject>
            {
                public override string Name => "sca";

                public override int ParameterCount => 2;

                public override void Apply(BackgroundObject obj, string[] parameters) => obj.scale = new Vector2(Parser.TryParse(parameters[0], 0f), Parser.TryParse(parameters[1], 0f));
            }

            public class BackgroundObjectRotationParameter : EditParameter<BackgroundObject>
            {
                public override string Name => "rot";

                public override int ParameterCount => 1;

                public override void Apply(BackgroundObject obj, string[] parameters) => obj.rot = Parser.TryParse(parameters[0], 0f);
            }

            public class BackgroundObjectColorParameter : EditParameter<BackgroundObject>
            {
                public override string Name => "color";

                public override int ParameterCount => 1;

                public override void Apply(BackgroundObject obj, string[] parameters) => obj.color = Parser.TryParse(parameters[0], 0);
            }

            public class BackgroundObjectFadeColorParameter : EditParameter<BackgroundObject>
            {
                public override string Name => "fade_color";

                public override int ParameterCount => 1;

                public override void Apply(BackgroundObject obj, string[] parameters) => obj.fadeColor = Parser.TryParse(parameters[0], 0);
            }

            #endregion

            #region Prefab Object

            public class PrefabObjectPositionParameter : EditParameter<PrefabObject>
            {
                public override string Name => "pos";

                public override int ParameterCount => 2;

                public override void Apply(PrefabObject obj, string[] parameters) => obj.events[0].SetValues(Parser.TryParse(parameters[0], 0f), Parser.TryParse(parameters[1], 0f));
            }

            public class PrefabObjectScaleParameter : EditParameter<PrefabObject>
            {
                public override string Name => "sca";

                public override int ParameterCount => 2;

                public override void Apply(PrefabObject obj, string[] parameters) => obj.events[1].SetValues(Parser.TryParse(parameters[0], 0f), Parser.TryParse(parameters[1], 0f));
            }

            public class PrefabObjectRotationParameter : EditParameter<PrefabObject>
            {
                public override string Name => "rot";

                public override int ParameterCount => 1;

                public override void Apply(PrefabObject obj, string[] parameters) => obj.events[2].SetValues(Parser.TryParse(parameters[0], 0f));
            }

            #endregion

            #region Event Keyframe

            public class EventKeframeTimeParameter : EditParameter<EventKeyframe>
            {
                public override string Name => "time";

                public override int ParameterCount => 1;

                public override void Apply(EventKeyframe obj, string[] parameters) => obj.time = Parser.TryParse(parameters[0], 0f);
            }

            public class EventKeyframeValueParameter : EditParameter<EventKeyframe>
            {
                public override string Name => "value";

                public override int ParameterCount => 2;

                public override void Apply(EventKeyframe obj, string[] parameters) => obj.SetValue(Mathf.Clamp(Parser.TryParse(parameters[0], 0), 0, obj.values.Length - 1), Parser.TryParse(parameters[1], 0f));
            }

            public class EventKeyframeEasingParameter : EditParameter<EventKeyframe>
            {
                public override string Name => "easing";

                public override int ParameterCount => 1;

                public override void Apply(EventKeyframe obj, string[] parameters) => obj.curve = Parser.TryParse(parameters[0], Easing.Linear);
            }

            public class EventKeyframeRelativeParameter : EditParameter<EventKeyframe>
            {
                public override string Name => "relative";

                public override int ParameterCount => 1;

                public override void Apply(EventKeyframe obj, string[] parameters) => obj.relative = Parser.TryParse(parameters[0], false);
            }

            #endregion

            #region Marker

            public class MarkerNameParameter : EditParameter<Marker>
            {
                public override string Name => "name";

                public override bool RequireQuotes => true;

                public override void Apply(Marker obj, string[] parameters) => obj.name = parameters[0];
            }

            public class MarkerTimeParameter : EditParameter<Marker>
            {
                public override string Name => "time";

                public override int ParameterCount => 1;

                public override void Apply(Marker obj, string[] parameters) => obj.time = Parser.TryParse(parameters[0], 0f);
            }

            public class MarkerAddTimeParameter : EditParameter<Marker>
            {
                public override string Name => "add_time";

                public override int ParameterCount => 1;

                public override void Apply(Marker obj, string[] parameters) => obj.time = AudioManager.inst.CurrentAudioSource.time + (Parser.TryParse(parameters[0], 0f) * index);
            }

            public class MarkerDescriptionParameter : EditParameter<Marker>
            {
                public override string Name => "desc";

                public override bool RequireQuotes => true;

                public override void Apply(Marker obj, string[] parameters) => obj.desc = parameters[0];
            }

            public class MarkerColorParameter : EditParameter<Marker>
            {
                public override string Name => "color";

                public override int ParameterCount => 1;

                public override void Apply(Marker obj, string[] parameters) => obj.color = Parser.TryParse(parameters[0], 0);
            }

            #endregion

            #region Checkpoint

            public class CheckpointNameParameter : EditParameter<Checkpoint>
            {
                public override string Name => "name";

                public override bool RequireQuotes => true;

                public override void Apply(Checkpoint obj, string[] parameters) => obj.name = parameters[0];
            }

            public class CheckpointTimeParameter : EditParameter<Checkpoint>
            {
                public override string Name => "time";

                public override int ParameterCount => 1;

                public override void Apply(Checkpoint obj, string[] parameters) => obj.time = Parser.TryParse(parameters[0], 0f);
            }

            public class CheckpointAddTimeParameter : EditParameter<Checkpoint>
            {
                public override string Name => "add_time";

                public override int ParameterCount => 1;

                public override void Apply(Checkpoint obj, string[] parameters) => obj.time = AudioManager.inst.CurrentAudioSource.time + (Parser.TryParse(parameters[0], 0f) * index);
            }

            #endregion

            #region Modifier

            public class ModifierValueParameter : EditParameter<Modifier>
            {
                public override string Name => "value";

                public override int ParameterCount => 2;

                public override void Apply(Modifier obj, string[] parameters) => obj.SetValue(Parser.TryParse(parameters[0], 0), parameters[1]);
            }

            public class ModifierConstantParameter : EditParameter<Modifier>
            {
                public override string Name => "constant";

                public override int ParameterCount => 1;

                public override void Apply(Modifier obj, string[] parameters) => obj.constant = Parser.TryParse(parameters[0], false);
            }

            public class ModifierRunCountParameter : EditParameter<Modifier>
            {
                public override string Name => "run_count";

                public override int ParameterCount => 2;

                // run_count set 0
                // run_count add 1
                public override void Apply(Modifier obj, string[] parameters) => RTMath.Operation(ref obj.runCount, Parser.TryParse(parameters[1], 0), RTMath.GetOperation(parameters[0], MathOperation.Set));
            }

            public class ModifierPrefabGroupOnlyParameter : EditParameter<Modifier>
            {
                public override string Name => "prefab_group_only";

                public override int ParameterCount => 1;

                public override void Apply(Modifier obj, string[] parameters)
                {
                    if (ModifiersHelper.IsGroupModifier(obj.Name))
                        obj.prefabInstanceOnly = Parser.TryParse(parameters[0], false);
                }
            }

            public class ModifierGroupAliveParameter : EditParameter<Modifier>
            {
                public override string Name => "group_alive";

                public override int ParameterCount => 1;

                public override void Apply(Modifier obj, string[] parameters)
                {
                    if (ModifiersHelper.IsGroupModifier(obj.Name))
                        obj.groupAlive = Parser.TryParse(parameters[0], false);
                }
            }

            public class ModifierNotParameter : EditParameter<Modifier>
            {
                public override string Name => "not";

                public override int ParameterCount => 1;

                public override void Apply(Modifier obj, string[] parameters)
                {
                    if (obj.type == Modifier.Type.Trigger)
                        obj.not = Parser.TryParse(parameters[0], false);
                }
            }

            public class ModifierElseIfParameter : EditParameter<Modifier>
            {
                public override string Name => "else_if";

                public override int ParameterCount => 1;

                public override void Apply(Modifier obj, string[] parameters)
                {
                    if (obj.type == Modifier.Type.Trigger)
                        obj.elseIf = Parser.TryParse(parameters[0], false);
                }
            }

            #endregion
        }

        /// <summary>
        /// Represents a command capable of creating objects.
        /// </summary>
        public class CreateCommand : EditCommand
        {
            #region Values

            public override string Name => "create";

            public override bool Usable => CoreHelper.InEditor;

            public override string Pattern => "create category [values]";

            #endregion

            #region Functions

            public override void ConsumeInput(string input, string[] split)
            {
                CurrentCategory = Parser.TryParse(split[1].ToLower().Remove("_"), true, CategoryType.Null);
                count = Parser.TryParse(split[2], 1);
                if (count <= 0)
                    return;
                switch (CurrentCategory)
                {
                    case CategoryType.BeatmapObject: {
                            for (index = 0; index < count; index++)
                            {
                                var beatmapObject = new BeatmapObject();
                                beatmapObject.InitDefaultEvents();
                                Apply(3, beatmapObject, split, beatmapObjectParameters);
                                GameData.Current.beatmapObjects.Add(beatmapObject);
                                EditorTimeline.inst.RenderTimelineObject(EditorTimeline.inst.GetTimelineObject(beatmapObject));
                                RTLevel.Current?.UpdateObject(beatmapObject);
                            }
                            break;
                        }
                    case CategoryType.BackgroundObject: {
                            for (index = 0; index < count; index++)
                            {
                                var backgroundObject = new BackgroundObject();
                                Apply(3, backgroundObject, split, backgroundObjectParameters);
                                GameData.Current.backgroundObjects.Add(backgroundObject);
                                EditorTimeline.inst.RenderTimelineObject(EditorTimeline.inst.GetTimelineObject(backgroundObject));
                            }
                            RTLevel.Current?.UpdateBackgroundObjects();
                            break;
                        }
                    case CategoryType.PrefabObject: {
                            var prefabName = split[3];
                            if (!GameData.Current.prefabs.TryFind(x => x.name == prefabName, out Prefab prefab))
                                break;
                            for (index = 0; index < count; index++)
                            {
                                var prefabObject = new PrefabObject();
                                prefabObject.SetDefaultTransformOffsets();
                                prefabObject.prefabID = prefab.id;
                                Apply(4, prefabObject, split, prefabObjectParameters);
                                GameData.Current.prefabObjects.Add(prefabObject);
                                EditorTimeline.inst.RenderTimelineObject(EditorTimeline.inst.GetTimelineObject(prefabObject));
                                RTLevel.Current?.UpdatePrefab(prefabObject);
                            }
                            break;
                        }
                    case CategoryType.Marker: {
                            for (index = 0; index < count; index++)
                            {
                                var marker = new Marker();
                                Apply(3, marker, split, markerParameters);
                                GameData.Current.data.markers.Add(marker);
                            }
                            RTMarkerEditor.inst.CreateMarkers();
                            break;
                        }
                    case CategoryType.Checkpoint: {
                            for (index = 0; index < count; index++)
                            {
                                var checkpoint = new Checkpoint();
                                Apply(3, checkpoint, split, checkpointParameters);
                                if (checkpoint.time > 0f)
                                    GameData.Current.data.checkpoints.Add(checkpoint);
                            }
                            RTCheckpointEditor.inst.UpdateCheckpointTimeline();
                            break;
                        }
                }
            }

            #endregion
        }

        /// <summary>
        /// Represents a command for selecting objects.
        /// </summary>
        public class SelectCommand : ExampleCommandBase
        {
            #region Values

            public override string Name => "select";

            public override bool Usable => CoreHelper.InEditor;

            public override string Pattern => "select type [predicates]";

            /// <summary>
            /// The current selectable type.
            /// </summary>
            public static SelectableType CurrentType { get; set; }

            /// <summary>
            /// List of get parameters.
            /// </summary>
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
                new NameRegexParameter(),
                new DescriptionRegexParameter(),
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

                #endregion

                #region Timeline Marker

                new SetMarkerColorParameter(),

                #endregion

                #region Beatmap Object
                
                new ShowObjectListParameter(),

                #endregion

                #region Level

                new CombineLevelParameter(),
                new CreateLevelCollectionParameter(),

                #endregion

                #region Prefab

                new ImportPrefabParameter(),
                new CreatePrefabParameter(),

                #endregion

                #region Modifier

                new SetPrefabGroupOnlyParameter(),
                new SwapPrefabGroupOnlyParameter(),

                #endregion
            };

            #endregion

            #region Functions

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
                    {
                        actionMode = false;
                        i++;
                        if (i < split.Length)
                        {
                            CurrentType = Parser.TryParse(split[i].ToLower().Remove("_"), true, SelectableType.Null);
                            if (CurrentType == SelectableType.Modifiers)
                                selectables = GetModifiers(selectables);
                            else
                                selectables = GetSelectables(CurrentType);
                        }
                        continue;
                    }

                    if (s == "union")
                    {
                        actionMode = false;
                        i++;
                        if (i >= split.Length)
                            break;
                        s = split[i];
                        var nextSelectables = GetSelectables(CurrentType);
                        if (parameters.TryFind(x => x.Name == s && (x.RequiredSelectionType == SelectableType.Null || x.RequiredSelectionType == CurrentType), out GetSelectableParameter parameter))
                            nextSelectables = parameter.GetSelectables(nextSelectables, parameter.GetParameters(split, ref i));
                        selectables = selectables.Union(nextSelectables);
                        continue;
                    }

                    if (s == "edit")
                    {
                        new EditCommand().ConsumeInput(split.Range(i + 1, split.Length - 1).ToArray(), CurrentType, selectables);
                        return;
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
                SelectableType.PlayerModels => PlayerEditor.inst.ModelPanels,
                SelectableType.FolderPlanners => ProjectPlanner.inst.folders,
                SelectableType.DocumentPlanners => ProjectPlanner.inst.documents,
                SelectableType.CharacterPlanners => ProjectPlanner.inst.characters,
                SelectableType.TimelinePlanners => ProjectPlanner.inst.timelines,
                SelectableType.SchedulePlanners => ProjectPlanner.inst.schedules,
                SelectableType.NotePlanners => ProjectPlanner.inst.notes,
                SelectableType.OSTPlanners => ProjectPlanner.inst.osts,
                SelectableType.Modifiers => ModifiersEditorDialog.Current.modifierCards,
                _ => null,
            };

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

            public static void SetName(ISelectable selectable, string name)
            {
                if (selectable is TimelineObject timelineObject)
                    timelineObject.SetName(name);
                if (selectable is TimelineMarker timelineMarker)
                    timelineMarker.Name = name;
                if (selectable is TimelineCheckpoint timelineCheckpoint)
                    timelineCheckpoint.Name = name;
                if (selectable is LevelPanel levelPanel && levelPanel.Item && levelPanel.Item.metadata)
                {
                    levelPanel.Item.metadata.beatmap.name = name;
                    levelPanel.Render();
                }
                if (selectable is LevelCollectionPanel levelCollectionPanel && levelCollectionPanel.Item)
                {
                    levelCollectionPanel.Item.name = name;
                    levelCollectionPanel.Render();
                }
                if (selectable is PrefabPanel prefabPanel && prefabPanel.Item && !prefabPanel.IsExternal)
                {
                    prefabPanel.Item.name = name;
                    RTPrefabEditor.inst.ValidateDuplicateName(prefabPanel.Item);
                    prefabPanel.Render();
                }
                if (selectable is ThemePanel themePanel && themePanel.Item && themePanel.Source == ObjectSource.Internal)
                {
                    themePanel.Item.name = name;
                    themePanel.Render();
                }
                if (selectable is PlayerModelPanel playerModelPanel && playerModelPanel.Item)
                {
                    playerModelPanel.Item.basePart.name = name;
                    playerModelPanel.Render();
                }
                if (selectable is FolderPlanner folderPlanner)
                {
                    folderPlanner.Name = name;
                    folderPlanner.Render();
                }
                if (selectable is DocumentPlanner documentPlanner)
                {
                    documentPlanner.Name = name;
                    documentPlanner.Render();
                }
                if (selectable is CharacterPlanner characterPlanner)
                {
                    characterPlanner.Name = name;
                    characterPlanner.Render();
                }
                if (selectable is TimelinePlanner timelinePlanner)
                {
                    timelinePlanner.Name = name;
                    timelinePlanner.Render();
                }
                if (selectable is NotePlanner notePlanner)
                {
                    notePlanner.Name = name;
                    notePlanner.Render();
                }
                if (selectable is OSTPlanner ostPlanner)
                {
                    ostPlanner.Name = name;
                    ostPlanner.Render();
                }
            }

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
                PlayerModels,
                FolderPlanners,
                DocumentPlanners,
                TODOPlanners,
                CharacterPlanners,
                TimelinePlanners,
                SchedulePlanners,
                NotePlanners,
                OSTPlanners,
                Modifiers,
            }

            public abstract class GetSelectableParameter : ParameterBase
            {
                public virtual SelectableType RequiredSelectionType => SelectableType.Null;

                public abstract IEnumerable<ISelectable> GetSelectables(IEnumerable<ISelectable> selectables, string[] parameters);
            }

            public abstract class ActionParameter : ParameterBase
            {
                public virtual SelectableType RequiredSelectionType => SelectableType.Null;

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

                public override bool RequireQuotes => true;

                public override IEnumerable<ISelectable> GetSelectables(IEnumerable<ISelectable> selectables, string[] parameters)
                {
                    var name = parameters[0];
                    foreach (var selectable in selectables)
                    {
                        if (GetName(selectable) == name)
                            yield return selectable;
                    }
                }
            }

            public class NameRegexParameter : GetSelectableParameter
            {
                public override string Name => "name_regex";

                public override bool RequireQuotes => true;

                public override IEnumerable<ISelectable> GetSelectables(IEnumerable<ISelectable> selectables, string[] parameters)
                {
                    var name = parameters[0];
                    foreach (var selectable in selectables)
                    {
                        if (Regex.IsMatch(GetName(selectable), name))
                            yield return selectable;
                    }
                }
            }

            public class DescriptionRegexParameter : GetSelectableParameter
            {
                public override string Name => "desc_regex";

                public override bool RequireQuotes => true;

                public override IEnumerable<ISelectable> GetSelectables(IEnumerable<ISelectable> selectables, string[] parameters)
                {
                    var name = parameters[0];
                    foreach (var selectable in selectables)
                    {
                        if (Regex.IsMatch(GetDescription(selectable), name))
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
                    var tag = parameters[0];
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
                    var layer = Parser.TryParse(parameters[1], 0);

                    foreach (var selectable in selectables)
                    {
                        if (selectable is TimelineObject timelineObject && comparison.Compare(timelineObject.Layer, layer))
                            yield return selectable;
                        if (selectable is TimelineMarker timelineMarker && timelineMarker.Marker && timelineMarker.Marker.layers.Any(x => comparison.Compare(x, layer)))
                            yield return selectable;
                    }
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

            #region Timeline Marker

            public class ColorEqualsParameter : GetSelectableParameter
            {
                public override string Name => "color";

                public override int ParameterCount => 2;

                public override IEnumerable<ISelectable> GetSelectables(IEnumerable<ISelectable> selectables, string[] parameters)
                {
                    var comparison = Parser.TryParse(parameters[0].Remove("_"), true, NumberComparison.Equals);
                    var color = Parser.TryParse(parameters[1], 0);
                    foreach (var selectable in selectables)
                        if (selectable is TimelineMarker timelineMarker && comparison.Compare(timelineMarker.ColorSlot, color))
                            yield return selectable;
                }
            }

            #endregion

            #region Timeline Checkpoint

            #endregion

            #region Level

            #endregion

            #region Modifier

            #endregion

            #endregion

            #region Action

            #region General

            public class LogSelectedParameter : ActionParameter
            {
                public override string Name => "log";

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

            public class NotifyCountParameter : ActionParameter
            {
                public override string Name => "notify_count";

                public override void Run(IEnumerable<ISelectable> selectables, string[] parameters) => EditorManager.inst.DisplayNotification($"Selected items: {selectables.Count()}", 2f, EditorManager.NotificationType.Success);
            }

            public class SetNameParameter : ActionParameter
            {
                public override string Name => "set_name";

                public override bool RequireQuotes => true;

                public override void Run(IEnumerable<ISelectable> selectables, string[] parameters)
                {
                    var name = parameters[0];
                    foreach (var selectable in selectables)
                        SetName(selectable, name);
                }
            }

            public class ReplaceNameParameter : ActionParameter
            {
                public override string Name => "replace_name";

                public override int ParameterCount => 2;

                public override bool RequireQuotes => true;

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

            public class MirrorObjectParameter : ActionParameter
            {
                public override string Name => "mirror";

                public override int ParameterCount => 1;

                public override SelectableType RequiredSelectionType => SelectableType.Objects;

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

            public class SetCollapseParameter : ActionParameter
            {
                public override string Name => "collapse";

                public override int ParameterCount => 1;

                public override void Run(IEnumerable<ISelectable> selectables, string[] parameters)
                {
                    var collapse = Parser.TryParse(parameters[0], false);
                    foreach (var selectable in selectables)
                        if (selectable is TimelineObject timelineObject)
                            timelineObject.Collapse = collapse;
                }
            }

            public class SwapCollapseParameter : ActionParameter
            {
                public override string Name => "swap_collapse";

                public override void Run(IEnumerable<ISelectable> selectables, string[] parameters)
                {
                    foreach (var selectable in selectables)
                        if (selectable is TimelineObject timelineObject)
                            timelineObject.Collapse = !timelineObject.Collapse;
                }
            }

            #endregion

            #region Timeline Marker

            public class SetMarkerColorParameter : ActionParameter
            {
                public override string Name => "set_color";

                public override int ParameterCount => 1;

                public override SelectableType RequiredSelectionType => SelectableType.Markers;

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

            #endregion

            #region Beatmap Object

            public class ShowObjectListParameter : ActionParameter
            {
                public override string Name => "show_object_list";

                public override SelectableType RequiredSelectionType => SelectableType.Objects;

                public override void Run(IEnumerable<ISelectable> selectables, string[] parameters)
                {
                    if (CurrentType == SelectableType.Objects)
                        ObjectEditor.inst.ShowObjectSearch(x => EditorTimeline.inst.SetCurrentObject(EditorTimeline.inst.GetTimelineObject(x), Input.GetKey(KeyCode.LeftControl)),
                            beatmapObjects: selectables.Where(x => x is TimelineObject timelineObject && timelineObject.isBeatmapObject).Select(x => ((TimelineObject)x).GetData<BeatmapObject>()).ToList());
                }
            }

            #endregion

            #region Level

            public class CombineLevelParameter : ActionParameter
            {
                public override string Name => "combine";

                public override bool RequireQuotes => true;

                public override void Run(IEnumerable<ISelectable> selectables, string[] parameters)
                {
                    var name = parameters[0];
                    if (!string.IsNullOrEmpty(name))
                        EditorLevelManager.inst.Combine(RTFile.ValidateDirectory(name), selectables.SelectWhere(x => x is LevelPanel, x => x as LevelPanel), EditorLevelManager.inst.LoadLevels);
                }
            }

            public class CreateLevelCollectionParameter : ActionParameter
            {
                public override string Name => "create_level_collection";

                public override bool RequireQuotes => true;

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

            public class ImportPrefabParameter : ActionParameter
            {
                public override string Name => "import";

                public override SelectableType RequiredSelectionType => SelectableType.ExternalPrefabs;

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
                        CoroutineHelper.StartCoroutine(RTPrefabEditor.inst.RefreshInternalPrefabs());
                }
            }

            public class CreatePrefabParameter : ActionParameter
            {
                public override string Name => "create_prefab";

                public override int ParameterCount => 4;

                public override SelectableType RequiredSelectionType => SelectableType.Objects;

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

            public class SetPrefabGroupOnlyParameter : ActionParameter
            {
                public override string Name => "prefab_group_only";

                public override int ParameterCount => 1;

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

            public class SwapPrefabGroupOnlyParameter : ActionParameter
            {
                public override string Name => "swap_prefab_group_only";

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
        }

        #endregion

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
                        list.Add(split[i]);
                        if (split[i].EndsWith(BracketsType == 1 ? ')' : ']'))
                            break;
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
                            parameter.variables = new Dictionary<string, string>(GetVariables().Select(x => new KeyValuePair<string, string>(x.Item1, x.Item2)));
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
                "current_layer" => EditorTimeline.inst.Layer.ToString(),
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
            public virtual IEnumerable<(string, string)> GetVariables() => new (string, string)[]
            {
                ("current_time", AudioManager.inst.CurrentAudioSource.time.ToString()),
                ("current_layer", EditorTimeline.inst.Layer.ToString()),
                ("max_bin", EditorTimeline.MAX_BINS.ToString()),
                ("default_bin", EditorTimeline.DEFAULT_BIN_COUNT.ToString()),
                ("default_object_name", BeatmapObject.DEFAULT_OBJECT_NAME),
            };

            #endregion
        }

        public abstract class VariableParameter : ParameterBase
        {
            public Dictionary<string, string> variables = new Dictionary<string, string>();

            public override IEnumerable<(string, string)> GetVariables()
            {
                foreach (var variable in variables)
                    yield return (variable.Key, variable.Value);
                foreach (var variable in base.GetVariables())
                    yield return variable;
            }

            public abstract string Get(string[] parameters);
        }

        public class EvaluateParameter : VariableParameter
        {
            public override string Name => "evaluate";

            public override int BracketsType => 1;

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

        #endregion
    }
}
