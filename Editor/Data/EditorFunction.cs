using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using SimpleJSON;

using BetterLegacy.Core;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Data.Modifiers;
using BetterLegacy.Editor.Data.Timeline;
using BetterLegacy.Editor.Managers;

namespace BetterLegacy.Editor.Data
{
    /// <summary>
    /// Editor predicate.
    /// </summary>
    public class EditorFunction : PAObject<EditorFunction>, IEditorFunction
    {
        public EditorFunction() : base() { }

        #region Values

        public string Name => name;
        public List<string> Values { get; set; } = new List<string>();

        public string name = string.Empty;

        public List<IEditorFunction> functions = new List<IEditorFunction>();

        public string returnVariable;

        #endregion

        #region Methods

        public override void CopyData(EditorFunction orig, bool newID = true)
        {
            name = orig.name;
            returnVariable = orig.returnVariable;

            for (int i = 0; i < orig.functions.Count; i++)
            {
                var origFunction = orig.functions[i];
                IEditorFunction function = origFunction.FunctionType switch
                {
                    Type.Function => origFunction.AsFunction()?.Copy(false),
                    Type.Trigger => origFunction.AsTrigger()?.Copy(false),
                    Type.Variable => origFunction.AsVariable()?.Copy(false),
                    Type.Action => origFunction.AsAction()?.Copy(false),
                    _ => null,
                };
                if (function != null)
                    functions.Add(function);
            }
        }

        public override void ReadJSON(JSONNode jn)
        {
            name = jn["name"] ?? string.Empty;

            for (int i = 0; i < jn["functions"].Count; i++)
            {
                var type = jn["functions"][i]["type"].AsInt;
                IEditorFunction function = (Type)type switch
                {
                    Type.Function => Parse(jn["functions"][i]),
                    Type.Trigger => Trigger.Parse(jn["functions"][i]),
                    Type.Variable => Variable.Parse(jn["functions"][i]),
                    Type.Action => Action.Parse(jn["functions"][i]),
                    _ => null,
                };
                if (function != null)
                    functions.Add(function);
            }
        }

        public override JSONNode ToJSON()
        {
            var jn = Parser.NewJSONObject();

            if (!string.IsNullOrEmpty(name))
                jn["name"] = name;

            for (int i = 0; i < functions.Count; i++)
                jn["functions"][i] = functions[i].ToJSON();

            return jn;
        }

        public object Run<T>(T obj)
        {
            Dictionary<string, object> cache = new Dictionary<string, object>();
            bool continued = false;
            bool result = true;
            bool triggered = false; // If the first "or gate" argument is true, then ignore the rest.
            int triggerIndex = 0;
            int index = 0;
            while (index < functions.Count)
            {
                var function = functions[index];
                var name = function.Name;

                // Continue to the end of the trigger loop and set all modifiers to not running.
                if (continued)
                {
                    index++;
                    continue;
                }

                switch (function.FunctionType)
                {
                    case Type.Function: {
                            var editorFunction = function.AsFunction();
                            if (!editorFunction)
                                break;

                            var variable = editorFunction.Run(obj);
                            if (variable != null && !string.IsNullOrEmpty(editorFunction.returnVariable))
                                cache[editorFunction.returnVariable] = variable;
                            break;
                        }
                    case Type.Trigger: {
                            if (name == "else") // else triggers inverse the previous trigger result
                            {
                                var innerResult = result;
                                result = !innerResult;
                            }
                            else
                            {
                                var trigger = function.AsTrigger();
                                if (!trigger)
                                    break;

                                var innerResult = trigger.not ? !trigger.RunTrigger(obj, cache) : trigger.RunTrigger(obj, cache);
                                var elseIf = triggerIndex > 0 && trigger.elseIf;

                                if (elseIf)
                                {
                                    if (result) // If result is already active, set triggered to true
                                        triggered = true;
                                    else // Otherwise set the result to trigger result
                                        result = innerResult;
                                }
                                else if (!triggered && !innerResult)
                                    result = false;
                            }
                            break;
                        }
                    case Type.Variable: {
                            function.AsVariable()?.RunVariable(obj, cache);
                            break;
                        }
                    case Type.Action: {
                            if (name == "return" || name == "continue") // return stops the loop (any), continue moves it to the next loop (forLoop only)
                            {
                                if (result)
                                    continued = true;

                                result = true;

                                index++;
                                continue;
                            }

                            function.AsAction()?.RunAction(obj, cache);
                            break;
                        }
                }

                triggerIndex++;
                index++;
            }

            return cache.TryGetValue(returnVariable, out object returnObj) ? returnObj : null;
        }

        #region Interface

        public EditorFunction AsFunction() => this;

        public Trigger AsTrigger() => null;

        public Variable AsVariable() => null;

        public Action AsAction() => null;

        public Type FunctionType => Type.Function;

        #endregion

        #endregion

        #region Trigger

        public class Trigger : PAObject<Trigger>, IEditorFunction
        {
            #region Constructors

            public Trigger() : base() { }

            public Trigger(string name) : this() => this.name = name;

            public Trigger(string name, Func<Trigger, Dictionary<string, object>, bool> predicate) : this(name) => this.predicate = predicate;

            public Trigger(string name, Func<Trigger, TimelineObject, Dictionary<string, object>, bool> timelineObjectPredicate) : this(name) => this.timelineObjectPredicate = timelineObjectPredicate;

            public Trigger(string name, Func<Trigger, TimelineKeyframe, Dictionary<string, object>, bool> timelineKeyframePredicate) : this(name) => this.timelineKeyframePredicate = timelineKeyframePredicate;

            public Trigger(string name, Func<Trigger, TimelineMarker, Dictionary<string, object>, bool> timelineMarkerPredicate) : this(name) => this.timelineMarkerPredicate = timelineMarkerPredicate;

            public Trigger(string name, Func<Trigger, TimelineCheckpoint, Dictionary<string, object>, bool> timelineCheckpointPredicate) : this(name) => this.timelineCheckpointPredicate = timelineCheckpointPredicate;

            #endregion

            #region Values

            public string Name => name;
            public List<string> Values { get; set; } = new List<string>();

            public string name = string.Empty;
            public bool not;
            public bool elseIf;

            public Func<Trigger, Dictionary<string, object>, bool> predicate;
            public Func<Trigger, TimelineObject, Dictionary<string, object>, bool> timelineObjectPredicate;
            public Func<Trigger, TimelineKeyframe, Dictionary<string, object>, bool> timelineKeyframePredicate;
            public Func<Trigger, TimelineMarker, Dictionary<string, object>, bool> timelineMarkerPredicate;
            public Func<Trigger, TimelineCheckpoint, Dictionary<string, object>, bool> timelineCheckpointPredicate;

            #endregion

            #region Methods

            public override void CopyData(Trigger orig, bool newID = true)
            {
                name = orig.name;
                not = orig.not;
                elseIf = orig.elseIf;

                CopyDelegates(orig);
            }

            public override void ReadJSON(JSONNode jn)
            {
                name = jn["n"];
                not = jn["not"].AsBool;
                elseIf = jn["else"].AsBool;

                Triggers.ApplyFunctions(this);
            }

            public override JSONNode ToJSON()
            {
                var jn = Parser.NewJSONObject();

                if (!string.IsNullOrEmpty(name))
                    jn["n"] = name;
                if (not)
                    jn["not"] = not;
                if (elseIf)
                    jn["else"] = elseIf;

                return jn;
            }

            public bool RunTrigger<T>(T obj, Dictionary<string, object> cache)
            {
                if (obj is TimelineObject timelineObject)
                    return timelineObjectPredicate?.Invoke(this, timelineObject, cache) == true;
                if (obj is TimelineKeyframe timelineKeyframe)
                    return timelineKeyframePredicate?.Invoke(this, timelineKeyframe, cache) == true;
                if (obj is TimelineMarker timelineMarker)
                    return timelineMarkerPredicate?.Invoke(this, timelineMarker, cache) == true;
                if (obj is TimelineCheckpoint timelineCheckpoint)
                    return timelineCheckpointPredicate?.Invoke(this, timelineCheckpoint, cache) == true;
                return false;
            }

            public void CopyDelegates(Trigger orig)
            {
                predicate = orig.predicate;
                timelineObjectPredicate = orig.timelineObjectPredicate;
                timelineKeyframePredicate = orig.timelineKeyframePredicate;
                timelineMarkerPredicate = orig.timelineMarkerPredicate;
                timelineCheckpointPredicate = orig.timelineCheckpointPredicate;
            }

            #region Interface

            public Type FunctionType => Type.Trigger;

            public EditorFunction AsFunction() => null;

            public Trigger AsTrigger() => this;

            public Variable AsVariable() => null;

            public Action AsAction() => null;

            #endregion

            #endregion
        }

        /// <summary>
        /// Library of triggers.
        /// </summary>
        public static class Triggers
        {
            public static bool ApplyFunctions(Trigger trigger)
            {
                if (string.IsNullOrEmpty(trigger.name))
                    return false;

                var triggers = GetTriggers();
                if (!triggers.TryFind(x => x.name == trigger.name, out Trigger defaultTrigger))
                    return false;

                trigger.CopyDelegates(defaultTrigger);
                return true;
            }
            public static List<Trigger> GetTriggers() => triggers;
            readonly static List<Trigger> triggers = new List<Trigger>
            {
                #region Global Triggers

                ObjectEquals,
                StringContains,
                RegexIsMatch,

                #endregion

                #region Timeline Object Triggers

                TimelineObjectSelected,
                TimelineObjectNameEquals,
                TimelineObjectNameContains,

                #endregion
                
                #region Timeline Checkpoint Triggers

                TimelineCheckpointNameEquals,
                TimelineCheckpointNameContains,
                
                #endregion
                
                #region Modifyable Triggers

                ModifyableContainsTag,
                ModifyableContainsModifier,
                
                #endregion
                
                #region Beatmap Object Triggers
                
                BeatmapObjectKeyframeIndexColorSlotEquals,
                BeatmapObjectKeyframeAllColorSlotEquals,
                BeatmapObjectKeyframeAnyColorSlotEquals,
                
                #endregion
            };

            #region Global Triggers

            public static Trigger ObjectEquals { get; } = new Trigger(nameof(ObjectEquals), (Trigger trigger, Dictionary<string, object> cache) =>
            {
                return cache.TryGetValue(trigger.GetValue(0), out object a) && cache.TryGetValue(trigger.GetValue(1), out object b) && a == b;
            });

            public static Trigger StringContains { get; } = new Trigger(nameof(StringContains), (Trigger trigger, Dictionary<string, object> cache) =>
            {
                return trigger.GetValue(0, cache).Contains(trigger.GetValue(1, cache));
            });

            public static Trigger RegexIsMatch { get; } = new Trigger(nameof(RegexIsMatch), (Trigger trigger, Dictionary<string, object> cache) =>
            {
                return Regex.IsMatch(trigger.GetValue(0, cache), trigger.GetValue(1, cache));
            });

            #endregion

            #region Timeline Object Triggers

            public static Trigger TimelineObjectSelected { get; } = new Trigger(nameof(TimelineObjectSelected), (Trigger trigger, TimelineObject timelineObject, Dictionary<string, object> cache) =>
            {
                return timelineObject.Selected;
            });

            public static Trigger TimelineObjectNameEquals { get; } = new Trigger(nameof(TimelineObjectNameEquals), (Trigger trigger, TimelineObject timelineObject, Dictionary<string, object> cache) =>
            {
                return timelineObject.Name == trigger.GetValue(0);
            });

            public static Trigger TimelineObjectNameContains { get; } = new Trigger(nameof(TimelineObjectNameContains), (Trigger trigger, TimelineObject timelineObject, Dictionary<string, object> cache) =>
            {
                return timelineObject.Name.Contains(trigger.GetValue(0));
            });

            #endregion

            #region Timeline Checkpoint Triggers

            public static Trigger TimelineCheckpointNameEquals { get; } = new Trigger(nameof(TimelineCheckpointNameEquals), (Trigger trigger, TimelineCheckpoint timelineCheckpoint, Dictionary<string, object> cache) =>
            {
                return timelineCheckpoint.Name == trigger.GetValue(0);
            });

            public static Trigger TimelineCheckpointNameContains { get; } = new Trigger(nameof(TimelineCheckpointNameContains), (Trigger trigger, TimelineCheckpoint timelineCheckpoint, Dictionary<string, object> cache) =>
            {
                return timelineCheckpoint.Name.Contains(trigger.GetValue(0));
            });

            #endregion

            #region Modifyable Triggers

            public static Trigger ModifyableContainsTag { get; } = new Trigger(nameof(ModifyableContainsTag), (Trigger trigger, TimelineObject timelineObject, Dictionary<string, object> cache) =>
            {
                var name = trigger.GetValue(0);
                var comparison = trigger.GetInt(1, 0);
                return timelineObject.TryGetData(out IModifyable modifyable) && modifyable.Tags != null && modifyable.Tags.Has(x => Compare(comparison, x, name));
            });
            
            public static Trigger ModifyableContainsModifier { get; } = new Trigger(nameof(ModifyableContainsModifier), (Trigger trigger, TimelineObject timelineObject, Dictionary<string, object> cache) =>
            {
                var name = trigger.GetValue(0);
                var comparison = trigger.GetInt(1, 0);
                return timelineObject.TryGetData(out IModifyable modifyable) && modifyable.Modifiers != null && modifyable.Modifiers.Has(x => Compare(comparison, x.Name, name));
            });

            #endregion

            #region Beatmap Object Triggers

            public static Trigger BeatmapObjectKeyframeIndexColorSlotEquals { get; } = new Trigger(nameof(BeatmapObjectKeyframeIndexColorSlotEquals), (Trigger trigger, TimelineObject timelineObject, Dictionary<string, object> cache) =>
            {
                if (!timelineObject.isBeatmapObject)
                    return false;

                var beatmapObject = timelineObject.GetData<BeatmapObject>();
                var type = trigger.GetInt(0, 0);
                var index = trigger.GetInt(1, 0);
                var value = trigger.GetInt(2, 0);
                return beatmapObject.events.InRange(3) && beatmapObject.events[3].InRange(index) && beatmapObject.events[3][index].values[type == 0 ? 0 : 5] == value;
            });
            
            public static Trigger BeatmapObjectKeyframeAllColorSlotEquals { get; } = new Trigger(nameof(BeatmapObjectKeyframeAllColorSlotEquals), (Trigger trigger, TimelineObject timelineObject, Dictionary<string, object> cache) =>
            {
                if (!timelineObject.isBeatmapObject)
                    return false;

                var beatmapObject = timelineObject.GetData<BeatmapObject>();
                var type = trigger.GetInt(0, 0);
                var value = trigger.GetInt(1, 0);
                return beatmapObject.events.InRange(3) && beatmapObject.events[3].All(x => x.values[type == 0 ? 0 : 5] == value);
            });
            
            public static Trigger BeatmapObjectKeyframeAnyColorSlotEquals { get; } = new Trigger(nameof(BeatmapObjectKeyframeAnyColorSlotEquals), (Trigger trigger, TimelineObject timelineObject, Dictionary<string, object> cache) =>
            {
                if (!timelineObject.isBeatmapObject)
                    return false;

                var beatmapObject = timelineObject.GetData<BeatmapObject>();
                var type = trigger.GetInt(0, 0);
                var value = trigger.GetInt(1, 0);
                return beatmapObject.events.InRange(3) && beatmapObject.events[3].Any(x => x.values[type == 0 ? 0 : 5] == value);
            });

            #endregion

            #region Functions

            static bool Compare(int comparison, string a, string b) => comparison switch
            {
                0 => a == b,
                1 => a?.Contains(b) == true,
                _ => false,
            };

            #endregion
        }

        #endregion

        #region Variable

        public class Variable : PAObject<Variable>, IEditorFunction
        {
            #region Constructors

            public Variable() : base() { }

            public Variable(string name) : this() => this.name = name;

            public Variable(string name, Action<Variable, Dictionary<string, object>> func) : this(name) => this.func = func;
            public Variable(string name, Action<Variable, TimelineObject, Dictionary<string, object>> timelineObjectFunc) : this(name) => this.timelineObjectFunc = timelineObjectFunc;
            public Variable(string name, Action<Variable, TimelineKeyframe, Dictionary<string, object>> timelineKeyframeFunc) : this(name) => this.timelineKeyframeFunc = timelineKeyframeFunc;
            public Variable(string name, Action<Variable, TimelineMarker, Dictionary<string, object>> timelineMarkerFunc) : this(name) => this.timelineMarkerFunc = timelineMarkerFunc;
            public Variable(string name, Action<Variable, TimelineCheckpoint, Dictionary<string, object>> timelineCheckpointFunc) : this(name) => this.timelineCheckpointFunc = timelineCheckpointFunc;

            #endregion

            #region Values

            public string Name => name;
            public List<string> Values { get; set; } = new List<string>();

            public string name = string.Empty;

            public Action<Variable, Dictionary<string, object>> func;
            public Action<Variable, TimelineObject, Dictionary<string, object>> timelineObjectFunc;
            public Action<Variable, TimelineKeyframe, Dictionary<string, object>> timelineKeyframeFunc;
            public Action<Variable, TimelineMarker, Dictionary<string, object>> timelineMarkerFunc;
            public Action<Variable, TimelineCheckpoint, Dictionary<string, object>> timelineCheckpointFunc;

            #endregion

            #region Methods

            public override void CopyData(Variable orig, bool newID = true)
            {


                CopyDelegates(orig);
            }

            public override void ReadJSON(JSONNode jn)
            {

            }

            public override JSONNode ToJSON()
            {
                var jn = Parser.NewJSONObject();

                return jn;
            }

            public void RunVariable<T>(T obj, Dictionary<string, object> cache)
            {
                if (func != null)
                {
                    func.Invoke(this, cache);
                    return;
                }

                if (obj is TimelineObject timelineObject)
                    timelineObjectFunc?.Invoke(this, timelineObject, cache);
                if (obj is TimelineKeyframe timelineKeyframe)
                    timelineKeyframeFunc?.Invoke(this, timelineKeyframe, cache);
                if (obj is TimelineMarker timelineMarker)
                    timelineMarkerFunc?.Invoke(this, timelineMarker, cache);
                if (obj is TimelineCheckpoint timelineCheckpoint)
                    timelineCheckpointFunc?.Invoke(this, timelineCheckpoint, cache);
            }

            public void CopyDelegates(Variable orig)
            {
                timelineObjectFunc = orig.timelineObjectFunc;
                timelineKeyframeFunc = orig.timelineKeyframeFunc;
                timelineMarkerFunc = orig.timelineMarkerFunc;
                timelineCheckpointFunc = orig.timelineCheckpointFunc;
            }

            #region Interface

            public Type FunctionType => Type.Variable;

            public EditorFunction AsFunction() => null;

            public Trigger AsTrigger() => null;

            public Variable AsVariable() => this;

            public Action AsAction() => null;

            #endregion

            #endregion
        }

        /// <summary>
        /// Library of variables.
        /// </summary>
        public static class Variables
        {
            public static bool ApplyFunctions(Variable variable)
            {
                if (string.IsNullOrEmpty(variable.name))
                    return false;

                var variables = GetVariables();
                if (!variables.TryFind(x => x.name == variable.name, out Variable defaultVariable))
                    return false;

                variable.CopyDelegates(defaultVariable);
                return true;
            }
            public static List<Variable> GetVariables() => variables;
            readonly static List<Variable> variables = new List<Variable>
            {
                #region Global Variables

                MathParse,

                GetTimelineObjects,
                GetSelectedTimelineObjects,
                GetBeatmapObjectTimelineObjects,
                GetPrefabObjectTimelineObjects,
                GetBackgroundObjectTimelineObjects,

                #endregion

                #region Timeline Object Variables

                GetTimelineObjectSelected,
                GetTimelineObjectName,

                #endregion

                #region Timeline Checkpoint Variables

                #endregion

                #region Modifyable Variables

                GetModifyableTagCount,
                GetModifyableModifierCount,

                #endregion
            };

            #region Global Variables

            public static Variable MathParse { get; } = new Variable(nameof(MathParse), (Variable variable, Dictionary<string, object> cache) =>
            {
                var variables = new Dictionary<string, float>();
                foreach (var entry in cache)
                {
                    if (entry.Value is int i)
                        variables[entry.Key] = i;
                    else if (entry.Value is float f)
                        variables[entry.Key] = f;
                }

                cache[variable.GetValue(0)] = RTMath.Parse(variable.GetValue(1), variables);
            });

            public static Variable GetTimelineObjects { get; } = new Variable(nameof(GetTimelineObjects), (Variable variable, Dictionary<string, object> cache) =>
            {
                cache[variable.GetValue(0)] = EditorTimeline.inst.timelineObjects;
            });
            
            public static Variable GetSelectedTimelineObjects { get; } = new Variable(nameof(GetSelectedTimelineObjects), (Variable variable, Dictionary<string, object> cache) =>
            {
                var timelineObjects = cache.TryGetValue(variable.GetValue(1), out object value) && value is List<TimelineObject> list ? list : EditorTimeline.inst.timelineObjects;

                cache[variable.GetValue(0)] = timelineObjects.FindAll(x => x.Selected);
            });
            
            public static Variable GetBeatmapObjectTimelineObjects { get; } = new Variable(nameof(GetSelectedTimelineObjects), (Variable variable, Dictionary<string, object> cache) =>
            {
                var timelineObjects = cache.TryGetValue(variable.GetValue(1), out object value) && value is List<TimelineObject> list ? list : EditorTimeline.inst.timelineObjects;

                cache[variable.GetValue(0)] = timelineObjects.FindAll(x => x.isBeatmapObject);
            });
            
            public static Variable GetPrefabObjectTimelineObjects { get; } = new Variable(nameof(GetSelectedTimelineObjects), (Variable variable, Dictionary<string, object> cache) =>
            {
                var timelineObjects = cache.TryGetValue(variable.GetValue(1), out object value) && value is List<TimelineObject> list ? list : EditorTimeline.inst.timelineObjects;

                cache[variable.GetValue(0)] = timelineObjects.FindAll(x => x.isPrefabObject);
            });
            
            public static Variable GetBackgroundObjectTimelineObjects { get; } = new Variable(nameof(GetSelectedTimelineObjects), (Variable variable, Dictionary<string, object> cache) =>
            {
                var timelineObjects = cache.TryGetValue(variable.GetValue(1), out object value) && value is List<TimelineObject> list ? list : EditorTimeline.inst.timelineObjects;

                cache[variable.GetValue(0)] = timelineObjects.FindAll(x => x.isBackgroundObject);
            });

            #endregion

            #region Timeline Object Variables

            public static Variable GetTimelineObjectSelected { get; } = new Variable(nameof(GetTimelineObjectSelected), (Variable variable, TimelineObject timelineObject, Dictionary<string, object> cache) =>
            {
                cache[variable.GetValue(0)] = timelineObject.SelectableInPreview;
            });

            public static Variable GetTimelineObjectName { get; } = new Variable(nameof(GetTimelineObjectName), (Variable variable, TimelineObject timelineObject, Dictionary<string, object> cache) =>
            {
                cache[variable.GetValue(0)] = timelineObject.Name;
            });

            #endregion

            #region Timeline Checkpoint Variables

            #endregion

            #region Modifyable Variables

            public static Variable GetModifyableTagCount { get; } = new Variable(nameof(GetModifyableTagCount), (Variable variable, TimelineObject timelineObject, Dictionary<string, object> cache) =>
            {
                cache[variable.GetValue(0)] = timelineObject.TryGetData(out IModifyable modifyable) && modifyable.Tags != null ? modifyable.Tags.Count : 0;
            });
            
            public static Variable GetModifyableModifierCount { get; } = new Variable(nameof(GetModifyableModifierCount), (Variable variable, TimelineObject timelineObject, Dictionary<string, object> cache) =>
            {
                cache[variable.GetValue(0)] = timelineObject.TryGetData(out IModifyable modifyable) && modifyable.Modifiers != null ? modifyable.Modifiers.Count : 0;
            });

            #endregion
        }

        #endregion

        #region Action

        public class Action : PAObject<Action>, IEditorFunction
        {
            #region Constructors

            #endregion

            #region Values

            public string Name => name;
            public List<string> Values { get; set; } = new List<string>();

            public string name = string.Empty;

            public Action<Action, TimelineObject, Dictionary<string, object>> timelineObjectAction;
            public Action<Action, TimelineKeyframe, Dictionary<string, object>> timelineKeyframeAction;
            public Action<Action, TimelineMarker, Dictionary<string, object>> timelineMarkerAction;
            public Action<Action, TimelineCheckpoint, Dictionary<string, object>> timelineCheckpointAction;

            #endregion

            #region Methods

            public override void CopyData(Action orig, bool newID = true)
            {


                CopyDelegates(orig);
            }

            public override void ReadJSON(JSONNode jn)
            {

            }

            public override JSONNode ToJSON()
            {
                var jn = Parser.NewJSONObject();

                return jn;
            }

            public void RunAction<T>(T obj, Dictionary<string, object> cache)
            {
                if (obj is TimelineObject timelineObject)
                    timelineObjectAction?.Invoke(this, timelineObject, cache);
                if (obj is TimelineKeyframe timelineKeyframe)
                    timelineKeyframeAction?.Invoke(this, timelineKeyframe, cache);
                if (obj is TimelineMarker timelineMarker)
                    timelineMarkerAction?.Invoke(this, timelineMarker, cache);
                if (obj is TimelineCheckpoint timelineCheckpoint)
                    timelineCheckpointAction?.Invoke(this, timelineCheckpoint, cache);
            }

            public void CopyDelegates(Action orig)
            {
                timelineObjectAction = orig.timelineObjectAction;
                timelineKeyframeAction = orig.timelineKeyframeAction;
                timelineMarkerAction = orig.timelineMarkerAction;
                timelineCheckpointAction = orig.timelineCheckpointAction;
            }

            #region Interface

            public Type FunctionType => Type.Action;

            public EditorFunction AsFunction() => null;

            public Trigger AsTrigger() => null;

            public Variable AsVariable() => null;

            public Action AsAction() => this;

            #endregion

            #endregion
        }

        /// <summary>
        /// Library of actions.
        /// </summary>
        public static class Actions
        {

        }

        #endregion

        public enum Type
        {
            Function,
            Trigger,
            Variable,
            Action,
        }
    }

    public interface IEditorFunction
    {
        public string Name { get; }

        public EditorFunction.Type FunctionType { get; }

        public List<string> Values { get; set; }

        public EditorFunction AsFunction();

        public EditorFunction.Trigger AsTrigger();

        public EditorFunction.Variable AsVariable();

        public EditorFunction.Action AsAction();

        public JSONNode ToJSON();
    }

    public static class EditorFunctionExtension
    {
        #region Values

        /// <summary>
        /// Gets a value of the function.
        /// </summary>
        /// <param name="index">Index of the value.</param>
        /// <returns>Returns a value.</returns>
        public static string GetValue(this IEditorFunction function, int index, Dictionary<string, object> variables = null)
        {
            var values = function.Values;
            if (index > 0 && !values.InRange(index))
                return string.Empty;

            var result = values[index];

            if (variables != null && variables.TryGetValue(result, out object variable))
                return variable?.ToString() ?? string.Empty;

            return result;
        }

        public static T GetValue<T>(this IEditorFunction function, int index, T defaultValue)
        {
            var type = typeof(T);
            if (type == typeof(bool))
                return (T)(object)function.GetBool(index, (bool)(object)defaultValue);
            if (type == typeof(float))
                return (T)(object)function.GetFloat(index, (float)(object)defaultValue);
            if (type == typeof(int))
                return (T)(object)function.GetInt(index, (int)(object)defaultValue);
            if (type == typeof(string))
                return (T)(object)function.GetString(index, (string)(object)defaultValue);
            return default;
        }

        public static bool GetBool(this IEditorFunction function, int index, bool defaultValue, Dictionary<string, object> variables = null)
        {
            var values = function.Values;
            if (!values.InRange(index))
                return defaultValue;

            return Parser.TryParse(function.GetValue(index, variables), defaultValue);
        }

        public static float GetFloat(this IEditorFunction function, int index, float defaultValue, Dictionary<string, object> variables = null)
        {
            var values = function.Values;
            if (!values.InRange(index))
                return defaultValue;

            return Parser.TryParse(function.GetValue(index, variables), defaultValue);
        }

        public static int GetInt(this IEditorFunction function, int index, int defaultValue, Dictionary<string, object> variables = null)
        {
            var values = function.Values;
            if (!values.InRange(index))
                return defaultValue;

            return Parser.TryParse(function.GetValue(index, variables), defaultValue);
        }

        public static string GetString(this IEditorFunction function, int index, string defaultValue, Dictionary<string, object> variables = null)
        {
            var values = function.Values;
            if (!values.InRange(index))
                return defaultValue;

            return function.GetValue(index, variables);
        }

        public static void SetValue(this IEditorFunction function, int index, string value)
        {
            var values = function.Values;
            if (index < values.Count)
                values[index] = value;
            else
                values.Add(value);
        }

        #endregion
    }
}
