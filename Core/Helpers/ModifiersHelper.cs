using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

using LSFunctions;

using SimpleJSON;

using BetterLegacy.Core.Components.Player;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Data.Level;
using BetterLegacy.Core.Data.Player;
using BetterLegacy.Core.Managers;
using BetterLegacy.Core.Runtime;
using BetterLegacy.Core.Runtime.Objects;
using BetterLegacy.Core.Runtime.Objects.Visual;
using BetterLegacy.Editor.Managers;

namespace BetterLegacy.Core.Helpers
{
    /// <summary>
    /// Helper class for modifier functions.
    /// </summary>
    public static class ModifiersHelper
    {
        /// <summary>
        /// If development only modifiers should be loaded.
        /// </summary>
        public static bool development = false;

        public const string DEPRECATED_MESSAGE = "Deprecated.";

        public static void ToggleDevelopment()
        {
            development = !development;
            if (ModifiersEditor.inst.DefaultModifiersPopup.IsOpen)
                ModifiersEditor.inst.DefaultModifiersPopup.SearchField.onValueChanged.Invoke(ModifiersEditor.inst.DefaultModifiersPopup.SearchTerm);
        }

        #region Running

        /// <summary>
        /// Checks if triggers return true.
        /// </summary>
        /// <typeparam name="T">The type of <see cref="Modifier"/>.</typeparam>
        /// <param name="triggers">Triggers to check.</param>
        /// <returns>Returns true if all modifiers are active or if some have else if on, otherwise returns false.</returns>
        public static bool CheckTriggers(List<Modifier> triggers, IModifierReference reference, Dictionary<string, string> variables)
        {
            bool result = true;
            triggers.ForLoop(trigger =>
            {
                if (trigger.compatibility.StoryOnly && !CoreHelper.InStory || trigger.active || trigger.triggerCount > 0 && trigger.runCount >= trigger.triggerCount)
                {
                    trigger.triggered = false;
                    result = false;
                    return;
                }

                var innerResult = trigger.not ? !trigger.RunTrigger(trigger, reference, variables) : trigger.RunTrigger(trigger, reference, variables);

                if (trigger.elseIf && !result && innerResult)
                    result = true;

                if (!trigger.elseIf && !innerResult)
                    result = false;

                trigger.triggered = innerResult;

                if (!trigger.running)
                    trigger.runCount++;
                if (!trigger.constant)
                    trigger.active = true;

                trigger.running = true;
            });
            return result;
        }

        /// <summary>
        /// Assigns the associated modifier functions to the modifier.
        /// </summary>
        /// <typeparam name="T">The type of <see cref="Modifier"/>.</typeparam>
        /// <param name="modifier">The modifier to assign to.</param>
        public static void AssignModifierActions(Modifier modifier, ModifierReferenceType referenceType)
        {
            var name = modifier.Name;

            if (modifier.type == Modifier.Type.Trigger && triggers.TryFind(x => x.name == name, out ModifierTrigger trigger) && trigger.compatibility.CompareType(referenceType))
                modifier.Trigger = trigger.function;

            if (modifier.type == Modifier.Type.Action && actions.TryFind(x => x.name == name, out ModifierAction action) && action.compatibility.CompareType(referenceType))
                modifier.Action = action.function;

            if (modifier.Inactive == null && inactives.TryFind(x => x.name == name, out ModifierInactive inactive) && inactive.compatibility.CompareType(referenceType))
                modifier.Inactive = inactive.function;
        }

        /// <summary>
        /// Assigns actions to a modifier.
        /// </summary>
        /// <typeparam name="T">The type of <see cref="Modifier{T}"/>.</typeparam>
        /// <param name="modifier">The modifier to assign to.</param>
        /// <param name="action">Action function to set if <see cref="Modifier.type"/> is <see cref="Modifier.Type.Action"/>.</param>
        /// <param name="trigger">Trigger function to set if <see cref="Modifier.type"/> is <see cref="Modifier.Type.Trigger"/>.</param>
        /// <param name="inactive">Inactive function to set.</param>
        public static void AssignModifierAction(Modifier modifier, Action<Modifier, IModifierReference, Dictionary<string, string>> action, Func<Modifier, IModifierReference, Dictionary<string, string>, bool> trigger, Action<Modifier, IModifierReference, Dictionary<string, string>> inactive)
        {
            // Only assign methods depending on modifier type.
            if (modifier.type == Modifier.Type.Action)
                modifier.Action = action;
            if (modifier.type == Modifier.Type.Trigger)
                modifier.Trigger = trigger;

            // Both action and trigger modifier types can be inactive.
            modifier.Inactive = inactive;
        }

        /// <summary>
        /// The original way modifiers run.
        /// </summary>
        /// <typeparam name="T">The type of <see cref="Modifier{T}"/>.</typeparam>
        /// <param name="modifiers">The list of modifiers to run.</param>
        /// <param name="active">If the object is active.</param>
        public static ModifierLoopResult RunModifiersAll(List<Modifier> modifiers, IModifierReference reference, Dictionary<string, string> variables = null) => RunModifiersAll(null, null, modifiers, reference, variables);

        /// <summary>
        /// The original way modifiers run.
        /// </summary>
        /// <typeparam name="T">The type of <see cref="Modifier{T}"/>.</typeparam>
        /// <param name="modifiers">The list of modifiers to run.</param>
        /// <param name="active">If the object is active.</param>
        public static ModifierLoopResult RunModifiersAll(List<Modifier> triggers, List<Modifier> actions, List<Modifier> modifiers, IModifierReference reference, Dictionary<string, string> variables = null)
        {
            if (triggers == null || actions == null)
            {
                triggers = new List<Modifier>();
                actions = new List<Modifier>();
                modifiers.ForLoop(modifier =>
                {
                    switch (modifier.type)
                    {
                        case Modifier.Type.Trigger: {
                                triggers.Add(modifier);
                                break;
                            }
                        case Modifier.Type.Action: {
                                actions.Add(modifier);
                                break;
                            }
                    }
                });
            }

            if (!triggers.IsEmpty())
            {
                // If all triggers are active
                if (CheckTriggers(triggers, reference, variables))
                {
                    bool returned = false;
                    actions.ForLoop(act =>
                    {
                        if (act.compatibility.StoryOnly && !CoreHelper.InStory || returned || act.active || act.triggerCount > 0 && act.runCount >= act.triggerCount) // Continue if modifier is not constant and was already activated
                            return;

                        if (!act.running)
                            act.runCount++;
                        if (!act.constant)
                            act.active = true;

                        act.running = true;
                        if (act.Action == null && TryGetAction(act, reference, out ModifierAction action))
                            act.Action = action.function;
                        act.RunAction(act, reference, variables);
                        if (act.Name == "return")
                            returned = true;
                    });
                    return new ModifierLoopResult(returned, true, Modifier.Type.Action, modifiers.Count);
                }

                // Deactivate both action and trigger modifiers
                modifiers.ForLoop(modifier =>
                {
                    if (modifier.compatibility.StoryOnly && !CoreHelper.InStory || !modifier.active && !modifier.running)
                        return;

                    modifier.active = false;
                    modifier.running = false;
                    if (modifier.Inactive == null && TryGetInactive(modifier, reference, out ModifierInactive action))
                        modifier.Inactive = action.function;
                    modifier.RunInactive(modifier, reference, variables);
                });
                return new ModifierLoopResult(false, false, Modifier.Type.Action, modifiers.Count);
            }

            actions.ForLoop(act =>
            {
                if (act.compatibility.StoryOnly && !CoreHelper.InStory || act.active || act.triggerCount > 0 && act.runCount >= act.triggerCount)
                    return;

                if (!act.running)
                    act.runCount++;
                if (!act.constant)
                    act.active = true;

                act.running = true;
                if (act.Action == null && TryGetAction(act, reference, out ModifierAction action))
                    act.Action = action.function;
                act.RunAction(act, reference, variables);
            });
            return new ModifierLoopResult(false, true, Modifier.Type.Action, modifiers.Count);
        }

        /// <summary>
        /// The advanced way modifiers run.
        /// </summary>
        /// <typeparam name="T">The type of <see cref="Modifier{T}"/>.</typeparam>
        /// <param name="modifiers">The list of modifiers to run.</param>
        /// <param name="active">If the object is active.</param>
        public static ModifierLoopResult RunModifiersLoop(List<Modifier> modifiers, IModifierReference reference, Dictionary<string, string> variables = null, int sequence = 0, int end = 0)
        {
            bool continued = false;
            bool returned = false;
            bool result = true; // Action modifiers at the start with no triggers before it should always run, so result is true.
            bool triggered = false; // If the first "or gate" argument is true, then ignore the rest.
            int triggerIndex = 0;
            Modifier.Type previousType = Modifier.Type.Action;
            int index = 0;
            while (index < modifiers.Count)
            {
                var modifier = modifiers[index];
                if (modifier.compatibility.StoryOnly && !CoreHelper.InStory)
                {
                    index++;
                    continue;
                }

                var name = modifier.Name;

                var isAction = modifier.type == Modifier.Type.Action;
                var isTrigger = modifier.type == Modifier.Type.Trigger;

                // Continue to the end of the modifier loop and set all modifiers to not running.
                if (continued)
                {
                    modifier.running = false;
                    index++;
                    continue;
                }

                if (name == "forLoop") // this modifier requires a specific function, so it's placed here.
                {
                    if (!modifier.running)
                        modifier.runCount++;

                    modifier.running = true;

                    var variable = modifier.GetValue(0);
                    var startIndex = modifier.GetInt(1, 0, variables);
                    var endCount = modifier.GetInt(2, 0, variables);
                    var increment = modifier.GetInt(3, 1, variables);

                    var endIndex = modifiers.FindLastIndex(x => x.Name == "return"); // return is treated as a break of the for loop
                    endIndex = endIndex < 0 ? modifiers.Count : endIndex + 1;

                    try
                    {
                        // if result is false, then skip the for loop sequence.
                        if (!(modifier.active || !result || modifier.triggerCount > 0 && modifier.runCount >= modifier.triggerCount))
                        {
                            var selectModifiers = modifiers.GetIndexRange(index + 1, endIndex);

                            for (int i = startIndex; i < endCount; i += increment)
                            {
                                variables[variable] = i.ToString();
                                RunModifiersLoop(selectModifiers, reference, variables, i, endCount);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        CoreHelper.LogError($"Had an exception with the forLoop modifier.\n" +
                            $"Index: {index}\n" +
                            $"End Index: {endIndex}\nException: {ex}");
                    }

                    // Only occur once
                    if (!modifier.constant && sequence + 1 >= end)
                        modifier.active = true;

                    index = endIndex; // exit for loop.
                    continue;
                }

                if (name == "resetLoop")
                {
                    var runCount = modifier.runCount;
                    if (!modifier.running)
                        runCount++;

                    modifier.running = true;

                    if (!(modifier.active || !result || modifier.triggerCount > 0 && runCount >= modifier.triggerCount))
                        modifiers.ForLoop(modifier =>
                        {
                            if (modifier.compatibility.StoryOnly && !CoreHelper.InStory || !modifier.active && !modifier.running)
                                return;

                            modifier.active = false;
                            modifier.running = false;
                            modifier.runCount = 0;
                            if (modifier.Inactive == null && TryGetInactive(modifier, reference, out ModifierInactive action))
                                modifier.Inactive = action.function;
                            modifier.RunInactive(modifier, reference, variables);
                        });

                    // Only occur once
                    if (!modifier.constant && sequence + 1 >= end)
                        modifier.active = true;

                    modifier.runCount = runCount;

                    index++;
                    continue;
                }

                if (isTrigger)
                {
                    if (previousType == Modifier.Type.Action) // If previous modifier was an action modifier, result should be considered true as we just started another modifier-block
                    {
                        if (name != "else")
                            result = true;
                        triggered = false;
                        triggerIndex = 0;
                    }

                    if (modifier.active || modifier.triggerCount > 0 && modifier.runCount >= modifier.triggerCount)
                    {
                        modifier.triggered = false;
                        result = false;
                    }
                    else if (name == "else") // else triggers inverse the previous trigger result
                    {
                        var innerResult = result;
                        result = !innerResult;
                        modifier.triggered = !innerResult;
                    }
                    else
                    {
                        var innerResult = modifier.not ? !modifier.RunTrigger(modifier, reference, variables) : modifier.RunTrigger(modifier, reference, variables);
                        var elseIf = triggerIndex > 0 && modifier.elseIf;

                        if (elseIf)
                        {
                            if (result) // If result is already active, set triggered to true
                                triggered = true;
                            else // Otherwise set the result to modifier trigger result
                                result = innerResult;
                        }
                        else if (!triggered && !innerResult)
                            result = false;

                        // Allow trigger to turn result to true again if "elseIf" is on
                        //if (modifier.elseIf && !result && innerResult)
                        //    result = true;

                        //if (!modifier.elseIf && !innerResult)
                        //    result = false;

                        modifier.triggered = innerResult;
                        previousType = modifier.type;
                    }

                    triggerIndex++;
                }

                if (name == "return" || name == "continue") // return stops the loop (any), continue moves it to the next loop (forLoop only)
                {
                    // Set modifier inactive state
                    if (!result && !(!modifier.active && !modifier.running))
                    {
                        modifier.active = false;
                        modifier.running = false;
                        result = false;
                    }

                    if (modifier.active || !result || modifier.triggerCount > 0 && modifier.runCount >= modifier.triggerCount) // don't return
                        result = false;

                    if (!modifier.running)
                        modifier.runCount++;

                    // Only occur once
                    if (!modifier.constant && sequence + 1 >= end)
                        modifier.active = true;

                    modifier.running = result;

                    if (result)
                    {
                        continued = true;
                        returned = name == "return";
                    }

                    result = true;

                    previousType = modifier.type;
                    index++;
                    continue;
                }

                // Set modifier inactive state
                if (!result && !(!modifier.active && !modifier.running))
                {
                    modifier.active = false;
                    modifier.running = false;
                    modifier.RunInactive(modifier, reference, variables);

                    previousType = modifier.type;
                    index++;
                    continue;
                }

                // Continue if modifier was already active with constant on
                if (modifier.active || !result || modifier.triggerCount > 0 && modifier.runCount >= modifier.triggerCount)
                {
                    previousType = modifier.type;
                    index++;
                    continue;
                }

                if (!modifier.running)
                    modifier.runCount++;

                // Only occur once
                if (!modifier.constant && sequence + 1 >= end)
                    modifier.active = true;

                modifier.running = true;

                if (isAction && result) // Only run modifier if result is true
                    modifier.RunAction(modifier, reference, variables);

                previousType = modifier.type;
                index++;
            }

            return new ModifierLoopResult(returned, result, previousType, index);
        }

        #endregion

        #region Functions

        public static ModifierReferenceType GetReferenceType<T>()
        {
            var type = typeof(T);
            if (type == typeof(BeatmapObject))
                return ModifierReferenceType.BeatmapObject;
            else if (type == typeof(BackgroundObject))
                return ModifierReferenceType.BackgroundObject;
            else if (type == typeof(PrefabObject))
                return ModifierReferenceType.PrefabObject;
            else if (type == typeof(PAPlayer))
                return ModifierReferenceType.PAPlayer;
            else if (type == typeof(PlayerModel))
                return ModifierReferenceType.PlayerModel;
            else if (type == typeof(PlayerObject))
                return ModifierReferenceType.PlayerObject;
            else if (type == typeof(GameData))
                return ModifierReferenceType.GameData;
            return ModifierReferenceType.Null;
        }

        public static bool VerifyModifier(Modifier modifier, List<Modifier> modifiers)
        {
            if (!modifier.verified)
            {
                modifier.verified = true;

                if (!modifier.Name.Contains("DEVONLY"))
                    modifier.VerifyModifier(modifiers);
            }

            return !string.IsNullOrEmpty(modifier.name);
        }

        public static ModifierTrigger[] triggers = new ModifierTrigger[]
        {
            new ModifierTrigger("break", ModifierTriggers.breakModifier),
            new ModifierTrigger(nameof(ModifierTriggers.disableModifier), (modifier, reference, variables) => false),

            #region Player

            new ModifierTrigger(nameof(ModifierTriggers.playerCollide), ModifierTriggers.playerCollide, ModifierCompatibility.BeatmapObjectCompatible),
            new ModifierTrigger(nameof(ModifierTriggers.playerCollideIndex), ModifierTriggers.playerCollideIndex, ModifierCompatibility.BeatmapObjectCompatible),
            new ModifierTrigger(nameof(ModifierTriggers.playerHealthEquals), ModifierTriggers.playerHealthEquals),
            new ModifierTrigger(nameof(ModifierTriggers.playerHealthLesserEquals), ModifierTriggers.playerHealthLesserEquals),
            new ModifierTrigger(nameof(ModifierTriggers.playerHealthGreaterEquals), ModifierTriggers.playerHealthGreaterEquals),
            new ModifierTrigger(nameof(ModifierTriggers.playerHealthLesser), ModifierTriggers.playerHealthLesser),
            new ModifierTrigger(nameof(ModifierTriggers.playerHealthGreater), ModifierTriggers.playerHealthGreater),
            new ModifierTrigger(nameof(ModifierTriggers.playerMoving), ModifierTriggers.playerMoving),
            new ModifierTrigger(nameof(ModifierTriggers.playerBoosting), ModifierTriggers.playerBoosting, ModifierCompatibility.BeatmapObjectCompatible),
            new ModifierTrigger(nameof(ModifierTriggers.playerAlive), ModifierTriggers.playerAlive),
            new ModifierTrigger(nameof(ModifierTriggers.playerDeathsEquals), ModifierTriggers.playerDeathsEquals),
            new ModifierTrigger(nameof(ModifierTriggers.playerDeathsLesserEquals), ModifierTriggers.playerDeathsLesserEquals),
            new ModifierTrigger(nameof(ModifierTriggers.playerDeathsGreaterEquals), ModifierTriggers.playerDeathsGreaterEquals),
            new ModifierTrigger(nameof(ModifierTriggers.playerDeathsLesser), ModifierTriggers.playerDeathsLesser),
            new ModifierTrigger(nameof(ModifierTriggers.playerDeathsGreater), ModifierTriggers.playerDeathsGreater),
            new ModifierTrigger(nameof(ModifierTriggers.playerDistanceGreater), ModifierTriggers.playerDistanceGreater),
            new ModifierTrigger(nameof(ModifierTriggers.playerDistanceLesser), ModifierTriggers.playerDistanceLesser),
            new ModifierTrigger(nameof(ModifierTriggers.playerCountEquals), ModifierTriggers.playerCountEquals),
            new ModifierTrigger(nameof(ModifierTriggers.playerCountLesserEquals), ModifierTriggers.playerCountLesserEquals),
            new ModifierTrigger(nameof(ModifierTriggers.playerCountGreaterEquals), ModifierTriggers.playerCountGreaterEquals),
            new ModifierTrigger(nameof(ModifierTriggers.playerCountLesser), ModifierTriggers.playerCountLesser),
            new ModifierTrigger(nameof(ModifierTriggers.playerCountGreater), ModifierTriggers.playerCountGreater),
            new ModifierTrigger(nameof(ModifierTriggers.onPlayerHit), ModifierTriggers.onPlayerHit),
            new ModifierTrigger(nameof(ModifierTriggers.onPlayerDeath), ModifierTriggers.onPlayerDeath),
            new ModifierTrigger(nameof(ModifierTriggers.playerBoostEquals), ModifierTriggers.playerBoostEquals),
            new ModifierTrigger(nameof(ModifierTriggers.playerBoostLesserEquals), ModifierTriggers.playerBoostLesserEquals),
            new ModifierTrigger(nameof(ModifierTriggers.playerBoostGreaterEquals), ModifierTriggers.playerBoostGreaterEquals),
            new ModifierTrigger(nameof(ModifierTriggers.playerBoostLesser), ModifierTriggers.playerBoostLesser),
            new ModifierTrigger(nameof(ModifierTriggers.playerBoostGreater), ModifierTriggers.playerBoostGreater),

            #endregion
            
            #region Collide

            new ModifierTrigger(nameof(ModifierTriggers.bulletCollide), ModifierTriggers.bulletCollide, ModifierCompatibility.BeatmapObjectCompatible),
            new ModifierTrigger(nameof(ModifierTriggers.objectCollide), ModifierTriggers.objectCollide, ModifierCompatibility.BeatmapObjectCompatible),

            #endregion

            #region Controls

            new ModifierTrigger(nameof(ModifierTriggers.keyPressDown), ModifierTriggers.keyPressDown),
            new ModifierTrigger(nameof(ModifierTriggers.keyPress), ModifierTriggers.keyPress),
            new ModifierTrigger(nameof(ModifierTriggers.keyPressUp), ModifierTriggers.keyPressUp),
            new ModifierTrigger(nameof(ModifierTriggers.controlPressDown), ModifierTriggers.controlPressDown),
            new ModifierTrigger(nameof(ModifierTriggers.controlPress), ModifierTriggers.controlPress),
            new ModifierTrigger(nameof(ModifierTriggers.controlPressUp), ModifierTriggers.controlPressUp),
            new ModifierTrigger(nameof(ModifierTriggers.mouseButtonDown), ModifierTriggers.mouseButtonDown),
            new ModifierTrigger(nameof(ModifierTriggers.mouseButton), ModifierTriggers.mouseButton),
            new ModifierTrigger(nameof(ModifierTriggers.mouseButtonUp), ModifierTriggers.mouseButtonUp),
            new ModifierTrigger(nameof(ModifierTriggers.mouseOver), ModifierTriggers.mouseOver, ModifierCompatibility.BeatmapObjectCompatible),
            new ModifierTrigger(nameof(ModifierTriggers.mouseOverSignalModifier), ModifierTriggers.mouseOverSignalModifier, ModifierCompatibility.BeatmapObjectCompatible),

            #endregion

            #region JSON

            new ModifierTrigger(nameof(ModifierTriggers.loadEquals), ModifierTriggers.loadEquals),
            new ModifierTrigger(nameof(ModifierTriggers.loadLesserEquals), ModifierTriggers.loadLesserEquals),
            new ModifierTrigger(nameof(ModifierTriggers.loadGreaterEquals), ModifierTriggers.loadGreaterEquals),
            new ModifierTrigger(nameof(ModifierTriggers.loadLesser), ModifierTriggers.loadLesser),
            new ModifierTrigger(nameof(ModifierTriggers.loadGreater), ModifierTriggers.loadGreater),
            new ModifierTrigger(nameof(ModifierTriggers.loadExists), ModifierTriggers.loadExists),

            #endregion

            #region Variable

            new ModifierTrigger(nameof(ModifierTriggers.localVariableEquals), ModifierTriggers.localVariableEquals),
            new ModifierTrigger(nameof(ModifierTriggers.localVariableLesserEquals), ModifierTriggers.localVariableLesserEquals),
            new ModifierTrigger(nameof(ModifierTriggers.localVariableGreaterEquals), ModifierTriggers.localVariableGreaterEquals),
            new ModifierTrigger(nameof(ModifierTriggers.localVariableLesser), ModifierTriggers.localVariableLesser),
            new ModifierTrigger(nameof(ModifierTriggers.localVariableGreater), ModifierTriggers.localVariableGreater),
            new ModifierTrigger(nameof(ModifierTriggers.localVariableContains), ModifierTriggers.localVariableContains),
            new ModifierTrigger(nameof(ModifierTriggers.localVariableStartsWith), ModifierTriggers.localVariableStartsWith),
            new ModifierTrigger(nameof(ModifierTriggers.localVariableEndsWith), ModifierTriggers.localVariableEndsWith),
            new ModifierTrigger(nameof(ModifierTriggers.localVariableExists), ModifierTriggers.localVariableExists),

            // self
            new ModifierTrigger(nameof(ModifierTriggers.variableEquals), ModifierTriggers.variableEquals),
            new ModifierTrigger(nameof(ModifierTriggers.variableLesserEquals), ModifierTriggers.variableLesserEquals),
            new ModifierTrigger(nameof(ModifierTriggers.variableGreaterEquals), ModifierTriggers.variableGreaterEquals),
            new ModifierTrigger(nameof(ModifierTriggers.variableLesser), ModifierTriggers.variableLesser),
            new ModifierTrigger(nameof(ModifierTriggers.variableGreater), ModifierTriggers.variableGreater),

            // other
            new ModifierTrigger(nameof(ModifierTriggers.variableOtherEquals), ModifierTriggers.variableOtherEquals, ModifierCompatibility.BeatmapObjectCompatible),
            new ModifierTrigger(nameof(ModifierTriggers.variableOtherLesserEquals), ModifierTriggers.variableOtherLesserEquals, ModifierCompatibility.BeatmapObjectCompatible),
            new ModifierTrigger(nameof(ModifierTriggers.variableOtherGreaterEquals), ModifierTriggers.variableOtherGreaterEquals, ModifierCompatibility.BeatmapObjectCompatible),
            new ModifierTrigger(nameof(ModifierTriggers.variableOtherLesser), ModifierTriggers.variableOtherLesser, ModifierCompatibility.BeatmapObjectCompatible),
            new ModifierTrigger(nameof(ModifierTriggers.variableOtherGreater), ModifierTriggers.variableOtherGreater, ModifierCompatibility.BeatmapObjectCompatible),

            #endregion

            #region Audio

            new ModifierTrigger(nameof(ModifierTriggers.pitchEquals), ModifierTriggers.pitchEquals),
            new ModifierTrigger(nameof(ModifierTriggers.pitchLesserEquals), ModifierTriggers.pitchLesserEquals),
            new ModifierTrigger(nameof(ModifierTriggers.pitchGreaterEquals), ModifierTriggers.pitchGreaterEquals),
            new ModifierTrigger(nameof(ModifierTriggers.pitchLesser), ModifierTriggers.pitchLesser),
            new ModifierTrigger(nameof(ModifierTriggers.pitchGreater), ModifierTriggers.pitchGreater),
            new ModifierTrigger(nameof(ModifierTriggers.musicTimeGreater), ModifierTriggers.musicTimeGreater),
            new ModifierTrigger(nameof(ModifierTriggers.musicTimeLesser), ModifierTriggers.musicTimeLesser),
            new ModifierTrigger(nameof(ModifierTriggers.musicTimeInRange), ModifierTriggers.musicTimeInRange),
            new ModifierTrigger(nameof(ModifierTriggers.musicPlaying), ModifierTriggers.musicPlaying),

            new ModifierTrigger("timeLesserEquals", (modifier, reference, variables) => AudioManager.inst.CurrentAudioSource.time <= modifier.GetFloat(0, 0f, variables)),
            new ModifierTrigger("timeGreaterEquals", (modifier, reference, variables) => AudioManager.inst.CurrentAudioSource.time >= modifier.GetFloat(0, 0f, variables)),
            new ModifierTrigger("timeLesser", (modifier, reference, variables)  => AudioManager.inst.CurrentAudioSource.time<modifier.GetFloat(0, 0f, variables)),
            new ModifierTrigger("timeGreater", (modifier, reference, variables)  => AudioManager.inst.CurrentAudioSource.time > modifier.GetFloat(0, 0f, variables)),

            #endregion

            #region Game State

            new ModifierTrigger(nameof(ModifierTriggers.inZenMode), ModifierTriggers.inZenMode),
            new ModifierTrigger(nameof(ModifierTriggers.inNormal), ModifierTriggers.inNormal),
            new ModifierTrigger(nameof(ModifierTriggers.in1Life), ModifierTriggers.in1Life),
            new ModifierTrigger(nameof(ModifierTriggers.inNoHit), ModifierTriggers.inNoHit),
            new ModifierTrigger(nameof(ModifierTriggers.inPractice), ModifierTriggers.inPractice),
            new ModifierTrigger(nameof(ModifierTriggers.inEditor), ModifierTriggers.inEditor),
            new ModifierTrigger(nameof(ModifierTriggers.isEditing), ModifierTriggers.isEditing),

            #endregion

            #region Random

            new ModifierTrigger(nameof(ModifierTriggers.randomEquals), ModifierTriggers.randomEquals),
            new ModifierTrigger(nameof(ModifierTriggers.randomLesser), ModifierTriggers.randomLesser),
            new ModifierTrigger(nameof(ModifierTriggers.randomGreater), ModifierTriggers.randomGreater),

            #endregion

            #region Math

            new ModifierTrigger(nameof(ModifierTriggers.mathEquals), ModifierTriggers.mathEquals),
            new ModifierTrigger(nameof(ModifierTriggers.mathLesserEquals), ModifierTriggers.mathLesserEquals),
            new ModifierTrigger(nameof(ModifierTriggers.mathGreaterEquals), ModifierTriggers.mathGreaterEquals),
            new ModifierTrigger(nameof(ModifierTriggers.mathLesser), ModifierTriggers.mathLesser),
            new ModifierTrigger(nameof(ModifierTriggers.mathGreater), ModifierTriggers.mathGreater),

            #endregion

            #region Animation

            new ModifierTrigger(nameof(ModifierTriggers.axisEquals), ModifierTriggers.axisEquals, ModifierCompatibility.BeatmapObjectCompatible),
            new ModifierTrigger(nameof(ModifierTriggers.axisLesserEquals), ModifierTriggers.axisLesserEquals, ModifierCompatibility.BeatmapObjectCompatible),
            new ModifierTrigger(nameof(ModifierTriggers.axisGreaterEquals), ModifierTriggers.axisGreaterEquals, ModifierCompatibility.BeatmapObjectCompatible),
            new ModifierTrigger(nameof(ModifierTriggers.axisLesser), ModifierTriggers.axisLesser, ModifierCompatibility.BeatmapObjectCompatible),
            new ModifierTrigger(nameof(ModifierTriggers.axisGreater), ModifierTriggers.axisGreater, ModifierCompatibility.BeatmapObjectCompatible),
            
            new ModifierTrigger(nameof(ModifierTriggers.eventEquals), ModifierTriggers.eventEquals),
            new ModifierTrigger(nameof(ModifierTriggers.eventLesserEquals), ModifierTriggers.eventLesserEquals),
            new ModifierTrigger(nameof(ModifierTriggers.eventGreaterEquals), ModifierTriggers.eventGreaterEquals),
            new ModifierTrigger(nameof(ModifierTriggers.eventLesser), ModifierTriggers.eventLesser),
            new ModifierTrigger(nameof(ModifierTriggers.eventGreater), ModifierTriggers.eventGreater),

            #endregion

            #region Level
            
            // self
            new ModifierTrigger(nameof(ModifierTriggers.levelRankEquals), ModifierTriggers.levelRankEquals),
            new ModifierTrigger(nameof(ModifierTriggers.levelRankLesserEquals), ModifierTriggers.levelRankLesserEquals),
            new ModifierTrigger(nameof(ModifierTriggers.levelRankGreaterEquals), ModifierTriggers.levelRankGreaterEquals),
            new ModifierTrigger(nameof(ModifierTriggers.levelRankLesser), ModifierTriggers.levelRankLesser),
            new ModifierTrigger(nameof(ModifierTriggers.levelRankGreater), ModifierTriggers.levelRankGreater),

            // other
            new ModifierTrigger(nameof(ModifierTriggers.levelRankOtherEquals), ModifierTriggers.levelRankOtherEquals),
            new ModifierTrigger(nameof(ModifierTriggers.levelRankOtherLesserEquals), ModifierTriggers.levelRankOtherLesserEquals),
            new ModifierTrigger(nameof(ModifierTriggers.levelRankOtherGreaterEquals), ModifierTriggers.levelRankOtherGreaterEquals),
            new ModifierTrigger(nameof(ModifierTriggers.levelRankOtherLesser), ModifierTriggers.levelRankOtherLesser),
            new ModifierTrigger(nameof(ModifierTriggers.levelRankOtherGreater), ModifierTriggers.levelRankOtherGreater),

            // current
            new ModifierTrigger(nameof(ModifierTriggers.levelRankCurrentEquals), ModifierTriggers.levelRankCurrentEquals),
            new ModifierTrigger(nameof(ModifierTriggers.levelRankCurrentLesserEquals), ModifierTriggers.levelRankCurrentLesserEquals),
            new ModifierTrigger(nameof(ModifierTriggers.levelRankCurrentGreaterEquals), ModifierTriggers.levelRankCurrentGreaterEquals),
            new ModifierTrigger(nameof(ModifierTriggers.levelRankCurrentLesser), ModifierTriggers.levelRankCurrentLesser),
            new ModifierTrigger(nameof(ModifierTriggers.levelRankCurrentGreater), ModifierTriggers.levelRankCurrentGreater),

            // level state
            new ModifierTrigger(nameof(ModifierTriggers.onLevelStart), ModifierTriggers.onLevelStart),
            new ModifierTrigger(nameof(ModifierTriggers.onLevelRestart), ModifierTriggers.onLevelRestart),
            new ModifierTrigger(nameof(ModifierTriggers.onLevelRewind), ModifierTriggers.onLevelRestart),
            new ModifierTrigger(nameof(ModifierTriggers.levelUnlocked), ModifierTriggers.levelUnlocked),
            new ModifierTrigger(nameof(ModifierTriggers.levelCompleted), ModifierTriggers.levelCompleted),
            new ModifierTrigger(nameof(ModifierTriggers.levelCompletedOther), ModifierTriggers.levelCompletedOther),
            new ModifierTrigger(nameof(ModifierTriggers.levelExists), ModifierTriggers.levelExists),
            new ModifierTrigger(nameof(ModifierTriggers.levelPathExists), ModifierTriggers.levelPathExists),

            new ModifierTrigger(nameof(ModifierTriggers.achievementUnlocked), ModifierTriggers.achievementUnlocked, ModifierCompatibility.LevelControlCompatible),

            #endregion

            #region Real Time

            // seconds
            new ModifierTrigger(nameof(ModifierTriggers.realTimeSecondEquals), ModifierTriggers.realTimeSecondEquals),
            new ModifierTrigger(nameof(ModifierTriggers.realTimeSecondLesserEquals), ModifierTriggers.realTimeSecondLesserEquals),
            new ModifierTrigger(nameof(ModifierTriggers.realTimeSecondGreaterEquals), ModifierTriggers.realTimeSecondGreaterEquals),
            new ModifierTrigger(nameof(ModifierTriggers.realTimeSecondLesser), ModifierTriggers.realTimeSecondLesser),
            new ModifierTrigger(nameof(ModifierTriggers.realTimeSecondGreater), ModifierTriggers.realTimeSecondGreater),

            // minutes
            new ModifierTrigger(nameof(ModifierTriggers.realTimeMinuteEquals), ModifierTriggers.realTimeMinuteEquals),
            new ModifierTrigger(nameof(ModifierTriggers.realTimeMinuteLesserEquals), ModifierTriggers.realTimeMinuteLesserEquals),
            new ModifierTrigger(nameof(ModifierTriggers.realTimeMinuteGreaterEquals), ModifierTriggers.realTimeMinuteGreaterEquals),
            new ModifierTrigger(nameof(ModifierTriggers.realTimeMinuteLesser), ModifierTriggers.realTimeMinuteLesser),
            new ModifierTrigger(nameof(ModifierTriggers.realTimeMinuteGreater), ModifierTriggers.realTimeMinuteGreater),

            // 24 hours
            new ModifierTrigger(nameof(ModifierTriggers.realTime24HourEquals), ModifierTriggers.realTime24HourEquals),
            new ModifierTrigger(nameof(ModifierTriggers.realTime24HourLesserEquals), ModifierTriggers.realTime24HourLesserEquals),
            new ModifierTrigger(nameof(ModifierTriggers.realTime24HourGreaterEquals), ModifierTriggers.realTime24HourGreaterEquals),
            new ModifierTrigger(nameof(ModifierTriggers.realTime24HourLesser), ModifierTriggers.realTime24HourLesser),
            new ModifierTrigger(nameof(ModifierTriggers.realTime24HourGreater), ModifierTriggers.realTime24HourGreater),

            // 12 hours
            new ModifierTrigger(nameof(ModifierTriggers.realTime12HourEquals), ModifierTriggers.realTime12HourEquals),
            new ModifierTrigger(nameof(ModifierTriggers.realTime12HourLesserEquals), ModifierTriggers.realTime12HourLesserEquals),
            new ModifierTrigger(nameof(ModifierTriggers.realTime12HourGreaterEquals), ModifierTriggers.realTime12HourGreaterEquals),
            new ModifierTrigger(nameof(ModifierTriggers.realTime12HourLesser), ModifierTriggers.realTime12HourLesser),
            new ModifierTrigger(nameof(ModifierTriggers.realTime12HourGreater), ModifierTriggers.realTime12HourGreater),

            // days
            new ModifierTrigger(nameof(ModifierTriggers.realTimeDayEquals), ModifierTriggers.realTimeDayEquals),
            new ModifierTrigger(nameof(ModifierTriggers.realTimeDayLesserEquals), ModifierTriggers.realTimeDayLesserEquals),
            new ModifierTrigger(nameof(ModifierTriggers.realTimeDayGreaterEquals), ModifierTriggers.realTimeDayGreaterEquals),
            new ModifierTrigger(nameof(ModifierTriggers.realTimeDayLesser), ModifierTriggers.realTimeDayLesser),
            new ModifierTrigger(nameof(ModifierTriggers.realTimeDayGreater), ModifierTriggers.realTimeDayGreater),
            new ModifierTrigger(nameof(ModifierTriggers.realTimeDayWeekEquals), ModifierTriggers.realTimeDayWeekEquals),

            // months
            new ModifierTrigger(nameof(ModifierTriggers.realTimeMonthEquals), ModifierTriggers.realTimeMonthEquals),
            new ModifierTrigger(nameof(ModifierTriggers.realTimeMonthLesserEquals), ModifierTriggers.realTimeMonthLesserEquals),
            new ModifierTrigger(nameof(ModifierTriggers.realTimeMonthGreaterEquals), ModifierTriggers.realTimeMonthGreaterEquals),
            new ModifierTrigger(nameof(ModifierTriggers.realTimeMonthLesser), ModifierTriggers.realTimeMonthLesser),
            new ModifierTrigger(nameof(ModifierTriggers.realTimeMonthGreater), ModifierTriggers.realTimeMonthGreater),

            // years
            new ModifierTrigger(nameof(ModifierTriggers.realTimeYearEquals), ModifierTriggers.realTimeYearEquals),
            new ModifierTrigger(nameof(ModifierTriggers.realTimeYearLesserEquals), ModifierTriggers.realTimeYearLesserEquals),
            new ModifierTrigger(nameof(ModifierTriggers.realTimeYearGreaterEquals), ModifierTriggers.realTimeYearGreaterEquals),
            new ModifierTrigger(nameof(ModifierTriggers.realTimeYearLesser), ModifierTriggers.realTimeYearLesser),
            new ModifierTrigger(nameof(ModifierTriggers.realTimeYearGreater), ModifierTriggers.realTimeYearGreater),

            #endregion

            #region Config

            // main
            new ModifierTrigger(nameof(ModifierTriggers.usernameEquals), ModifierTriggers.usernameEquals),
            new ModifierTrigger(nameof(ModifierTriggers.languageEquals), ModifierTriggers.languageEquals),

            // misc
            new ModifierTrigger(nameof(ModifierTriggers.configLDM), ModifierTriggers.configLDM),
            new ModifierTrigger(nameof(ModifierTriggers.configShowEffects), ModifierTriggers.configShowEffects),
            new ModifierTrigger(nameof(ModifierTriggers.configShowPlayerGUI), ModifierTriggers.configShowPlayerGUI),
            new ModifierTrigger(nameof(ModifierTriggers.configShowIntro), ModifierTriggers.configShowIntro),

            #endregion

            #region Misc

            new ModifierTrigger(nameof(ModifierTriggers.await), ModifierTriggers.await),
            new ModifierTrigger(nameof(ModifierTriggers.awaitCounter), ModifierTriggers.awaitCounter),
            new ModifierTrigger(nameof(ModifierTriggers.containsTag), ModifierTriggers.containsTag),
            new ModifierTrigger(nameof(ModifierTriggers.requireSignal), ModifierTriggers.requireSignal),
            new ModifierTrigger(nameof(ModifierTriggers.isFocused), ModifierTriggers.isFocused),
            new ModifierTrigger(nameof(ModifierTriggers.isFullscreen), ModifierTriggers.isFullscreen),
            new ModifierTrigger(nameof(ModifierTriggers.objectActive), ModifierTriggers.objectActive),
            new ModifierTrigger(nameof(ModifierTriggers.objectCustomActive), ModifierTriggers.objectCustomActive),
            new ModifierTrigger(nameof(ModifierTriggers.objectAlive), ModifierTriggers.objectAlive, ModifierCompatibility.BeatmapObjectCompatible),
            new ModifierTrigger(nameof(ModifierTriggers.objectSpawned), ModifierTriggers.objectSpawned, ModifierCompatibility.BeatmapObjectCompatible),
            new ModifierTrigger(nameof(ModifierTriggers.onMarker), ModifierTriggers.onMarker),
            new ModifierTrigger(nameof(ModifierTriggers.onCheckpoint), ModifierTriggers.onCheckpoint),
            new ModifierTrigger(nameof(ModifierTriggers.fromPrefab), ModifierTriggers.fromPrefab),
            new ModifierTrigger(nameof(ModifierTriggers.callModifierBlockTrigger), ModifierTriggers.callModifierBlockTrigger, ModifierCompatibility.LevelControlCompatible),

            #endregion

            #region Player Only
            
            new ModifierTrigger(nameof(ModifierTriggers.healthEquals), ModifierTriggers.healthEquals, ModifierCompatibility.PAPlayerCompatible),
            new ModifierTrigger(nameof(ModifierTriggers.healthGreaterEquals), ModifierTriggers.healthGreaterEquals, ModifierCompatibility.PAPlayerCompatible),
            new ModifierTrigger(nameof(ModifierTriggers.healthLesserEquals), ModifierTriggers.healthLesserEquals, ModifierCompatibility.PAPlayerCompatible),
            new ModifierTrigger(nameof(ModifierTriggers.healthGreater), ModifierTriggers.healthGreater, ModifierCompatibility.PAPlayerCompatible),
            new ModifierTrigger(nameof(ModifierTriggers.healthLesser), ModifierTriggers.healthLesser, ModifierCompatibility.PAPlayerCompatible),
            new ModifierTrigger(nameof(ModifierTriggers.isDead), ModifierTriggers.isDead, ModifierCompatibility.PAPlayerCompatible),
            new ModifierTrigger(nameof(ModifierTriggers.isBoosting), ModifierTriggers.isBoosting, ModifierCompatibility.PAPlayerCompatible),
            new ModifierTrigger(nameof(ModifierTriggers.isColliding), ModifierTriggers.isColliding, ModifierCompatibility.PAPlayerCompatible),
            new ModifierTrigger(nameof(ModifierTriggers.isSolidColliding), ModifierTriggers.isSolidColliding, ModifierCompatibility.PAPlayerCompatible),

            #endregion

            #region Dev Only

            new ModifierTrigger("storyLoadIntEqualsDEVONLY", (modifier, reference, variables) =>
            {
                return Story.StoryManager.inst.CurrentSave && Story.StoryManager.inst.CurrentSave.LoadInt(modifier.GetValue(0, variables), modifier.GetInt(1, 0, variables)) == modifier.GetInt(2, 0, variables);
            }),
            new ModifierTrigger("storyLoadIntLesserEqualsDEVONLY", (modifier, reference, variables) =>
            {
                return Story.StoryManager.inst.CurrentSave && Story.StoryManager.inst.CurrentSave.LoadInt(modifier.GetValue(0, variables), modifier.GetInt(1, 0, variables)) <= modifier.GetInt(2, 0, variables);
            }),
            new ModifierTrigger("storyLoadIntGreaterEqualsDEVONLY", (modifier, reference, variables) =>
            {
                return Story.StoryManager.inst.CurrentSave && Story.StoryManager.inst.CurrentSave.LoadInt(modifier.GetValue(0, variables), modifier.GetInt(1, 0, variables)) >= modifier.GetInt(2, 0, variables);
            }),
            new ModifierTrigger("storyLoadIntLesserDEVONLY", (modifier, reference, variables) =>
            {
                return Story.StoryManager.inst.CurrentSave && Story.StoryManager.inst.CurrentSave.LoadInt(modifier.GetValue(0, variables), modifier.GetInt(1, 0, variables)) < modifier.GetInt(2, 0, variables);
            }),
            new ModifierTrigger("storyLoadIntGreaterDEVONLY", (modifier, reference, variables) =>
            {
                return Story.StoryManager.inst.CurrentSave && Story.StoryManager.inst.CurrentSave.LoadInt(modifier.GetValue(0, variables), modifier.GetInt(1, 0, variables)) > modifier.GetInt(2, 0, variables);
            }),
            new ModifierTrigger("storyLoadBoolDEVONLY", (modifier, reference, variables) =>
            {
                return Story.StoryManager.inst.CurrentSave && Story.StoryManager.inst.CurrentSave.LoadBool(modifier.GetValue(0, variables), modifier.GetBool(1, false));
            }),

            #endregion
        };

        public static ModifierAction[] actions = new ModifierAction[]
        {
            #region Audio

            // pitch
            new ModifierAction(nameof(ModifierActions.setPitch),  ModifierActions.setPitch, ModifierCompatibility.LevelControlCompatible),
            new ModifierAction(nameof(ModifierActions.addPitch),  ModifierActions.addPitch, ModifierCompatibility.LevelControlCompatible),
            new ModifierAction(nameof(ModifierActions.setPitchMath),  ModifierActions.setPitchMath, ModifierCompatibility.LevelControlCompatible),
            new ModifierAction(nameof(ModifierActions.addPitchMath),  ModifierActions.addPitchMath, ModifierCompatibility.LevelControlCompatible),

            // music playing states
            new ModifierAction(nameof(ModifierActions.setMusicTime),  ModifierActions.setMusicTime, ModifierCompatibility.LevelControlCompatible),
            new ModifierAction(nameof(ModifierActions.setMusicTimeMath),  ModifierActions.setMusicTimeMath, ModifierCompatibility.LevelControlCompatible),
            new ModifierAction(nameof(ModifierActions.setMusicTimeStartTime),  ModifierActions.setMusicTimeStartTime, ModifierCompatibility.LevelControlCompatible),
            new ModifierAction(nameof(ModifierActions.setMusicTimeAutokill),  ModifierActions.setMusicTimeAutokill, ModifierCompatibility.LevelControlCompatible),
            new ModifierAction(nameof(ModifierActions.setMusicPlaying),  ModifierActions.setMusicPlaying, ModifierCompatibility.LevelControlCompatible),

            // play sound
            new ModifierAction(nameof(ModifierActions.playSound),  ModifierActions.playSound),
            new ModifierAction(nameof(ModifierActions.playSoundOnline),  ModifierActions.playSoundOnline),
            new ModifierAction(nameof(ModifierActions.playOnlineSound),  ModifierActions.playOnlineSound),
            new ModifierAction(nameof(ModifierActions.playDefaultSound),  ModifierActions.playDefaultSound),
            new ModifierAction(nameof(ModifierActions.audioSource),  ModifierActions.audioSource, ModifierCompatibility.BeatmapObjectCompatible),
            new ModifierAction(nameof(ModifierActions.loadSoundAsset),  ModifierActions.loadSoundAsset, ModifierCompatibility.BeatmapObjectCompatible),

            #endregion

            #region Level

            new ModifierAction(nameof(ModifierActions.loadLevel),  ModifierActions.loadLevel, ModifierCompatibility.LevelControlCompatible),
            new ModifierAction(nameof(ModifierActions.loadLevelID),  ModifierActions.loadLevelID, ModifierCompatibility.LevelControlCompatible),
            new ModifierAction(nameof(ModifierActions.loadLevelInternal),  ModifierActions.loadLevelInternal, ModifierCompatibility.LevelControlCompatible),
            new ModifierAction(nameof(ModifierActions.loadLevelPrevious),  ModifierActions.loadLevelPrevious, ModifierCompatibility.LevelControlCompatible),
            new ModifierAction(nameof(ModifierActions.loadLevelHub),  ModifierActions.loadLevelHub, ModifierCompatibility.LevelControlCompatible),
            new ModifierAction(nameof(ModifierActions.loadLevelInCollection),  ModifierActions.loadLevelInCollection, ModifierCompatibility.LevelControlCompatible),
            new ModifierAction(nameof(ModifierActions.loadLevelCollection),  ModifierActions.loadLevelCollection, ModifierCompatibility.LevelControlCompatible),
            new ModifierAction(nameof(ModifierActions.downloadLevel),  ModifierActions.downloadLevel, ModifierCompatibility.LevelControlCompatible),
            new ModifierAction(nameof(ModifierActions.endLevel),  ModifierActions.endLevel, ModifierCompatibility.LevelControlCompatible),
            new ModifierAction(nameof(ModifierActions.setAudioTransition),  ModifierActions.setAudioTransition, ModifierCompatibility.LevelControlCompatible),
            new ModifierAction(nameof(ModifierActions.setIntroFade),  ModifierActions.setIntroFade, ModifierCompatibility.LevelControlCompatible),
            new ModifierAction(nameof(ModifierActions.setLevelEndFunc),  ModifierActions.setLevelEndFunc, ModifierCompatibility.LevelControlCompatible),
            new ModifierAction(nameof(ModifierActions.getCurrentLevelID),  ModifierActions.getCurrentLevelID),
            new ModifierAction(nameof(ModifierActions.getCurrentLevelRank),  ModifierActions.getCurrentLevelRank),

            #endregion

            #region Component

            new ModifierAction(nameof(ModifierActions.blur),  ModifierActions.blur, ModifierCompatibility.BeatmapObjectCompatible),
            new ModifierAction(nameof(ModifierActions.blurOther),  ModifierActions.blurOther, ModifierCompatibility.BeatmapObjectCompatible),
            new ModifierAction(nameof(ModifierActions.blurVariable),  ModifierActions.blurVariable, ModifierCompatibility.BeatmapObjectCompatible),
            new ModifierAction(nameof(ModifierActions.blurVariableOther),  ModifierActions.blurVariableOther, ModifierCompatibility.BeatmapObjectCompatible),
            new ModifierAction(nameof(ModifierActions.blurColored),  ModifierActions.blurColored, ModifierCompatibility.BeatmapObjectCompatible),
            new ModifierAction(nameof(ModifierActions.blurColoredOther),  ModifierActions.blurColoredOther, ModifierCompatibility.BeatmapObjectCompatible),
            new ModifierAction(nameof(ModifierActions.doubleSided),  ModifierActions.doubleSided, ModifierCompatibility.BeatmapObjectCompatible),
            new ModifierAction(nameof(ModifierActions.particleSystem),  ModifierActions.particleSystem, ModifierCompatibility.BeatmapObjectCompatible),
            new ModifierAction(nameof(ModifierActions.trailRenderer),  ModifierActions.trailRenderer, ModifierCompatibility.BeatmapObjectCompatible),
            new ModifierAction(nameof(ModifierActions.trailRendererHex),  ModifierActions.trailRendererHex, ModifierCompatibility.BeatmapObjectCompatible),
            new ModifierAction(nameof(ModifierActions.rigidbody),  ModifierActions.rigidbody, ModifierCompatibility.BeatmapObjectCompatible),
            new ModifierAction(nameof(ModifierActions.rigidbodyOther),  ModifierActions.rigidbodyOther, ModifierCompatibility.BeatmapObjectCompatible),
            new ModifierAction(nameof(ModifierActions.setRenderType),  ModifierActions.setRenderType, ModifierCompatibility.BeatmapObjectCompatible),
            new ModifierAction(nameof(ModifierActions.setRenderTypeOther),  ModifierActions.setRenderTypeOther, ModifierCompatibility.BeatmapObjectCompatible),

            #endregion

            #region Player

            // hit
            new ModifierAction(nameof(ModifierActions.playerHit),  ModifierActions.playerHit, ModifierCompatibility.BeatmapObjectCompatible),
            new ModifierAction(nameof(ModifierActions.playerHitIndex),  ModifierActions.playerHitIndex, ModifierCompatibility.LevelControlCompatible),
            new ModifierAction(nameof(ModifierActions.playerHitAll),  ModifierActions.playerHitAll, ModifierCompatibility.LevelControlCompatible),

            // heal
            new ModifierAction(nameof(ModifierActions.playerHeal),  ModifierActions.playerHeal, ModifierCompatibility.BeatmapObjectCompatible),
            new ModifierAction(nameof(ModifierActions.playerHealIndex),  ModifierActions.playerHealIndex, ModifierCompatibility.LevelControlCompatible),
            new ModifierAction(nameof(ModifierActions.playerHealAll),  ModifierActions.playerHealAll, ModifierCompatibility.LevelControlCompatible),

            // kill
            new ModifierAction(nameof(ModifierActions.playerKill),  ModifierActions.playerKill, ModifierCompatibility.BeatmapObjectCompatible),
            new ModifierAction(nameof(ModifierActions.playerKillIndex),  ModifierActions.playerKillIndex, ModifierCompatibility.LevelControlCompatible),
            new ModifierAction(nameof(ModifierActions.playerKillAll),  ModifierActions.playerKillAll, ModifierCompatibility.LevelControlCompatible),

            // respawn
            new ModifierAction(nameof(ModifierActions.playerRespawn),  ModifierActions.playerRespawn, ModifierCompatibility.BeatmapObjectCompatible),
            new ModifierAction(nameof(ModifierActions.playerRespawnIndex),  ModifierActions.playerRespawnIndex, ModifierCompatibility.LevelControlCompatible),
            new ModifierAction(nameof(ModifierActions.playerRespawnAll),  ModifierActions.playerRespawnAll, ModifierCompatibility.LevelControlCompatible),

            // lock
            new ModifierAction(nameof(ModifierActions.playerLockX), ModifierActions.playerLockX, ModifierCompatibility.LevelControlCompatible),
            new ModifierAction(nameof(ModifierActions.playerLockXIndex), ModifierActions.playerLockXIndex, ModifierCompatibility.LevelControlCompatible),
            new ModifierAction(nameof(ModifierActions.playerLockXAll), ModifierActions.playerLockXAll, ModifierCompatibility.LevelControlCompatible),
            new ModifierAction(nameof(ModifierActions.playerLockY), ModifierActions.playerLockY, ModifierCompatibility.LevelControlCompatible),
            new ModifierAction(nameof(ModifierActions.playerLockYIndex), ModifierActions.playerLockYIndex, ModifierCompatibility.LevelControlCompatible),
            new ModifierAction(nameof(ModifierActions.playerLockYAll), ModifierActions.playerLockYAll, ModifierCompatibility.LevelControlCompatible),
            new ModifierAction(nameof(ModifierActions.playerLockBoostAll),  ModifierActions.playerLockBoostAll, ModifierCompatibility.LevelControlCompatible),

            new ModifierAction(nameof(ModifierActions.playerEnable),  ModifierActions.playerEnable, ModifierCompatibility.LevelControlCompatible),
            new ModifierAction(nameof(ModifierActions.playerEnableIndex),  ModifierActions.playerEnableIndex, ModifierCompatibility.LevelControlCompatible),
            new ModifierAction(nameof(ModifierActions.playerEnableAll),  ModifierActions.playerEnableAll, ModifierCompatibility.LevelControlCompatible),

            // player move
            new ModifierAction(nameof(ModifierActions.playerMove),  ModifierActions.playerMove, ModifierCompatibility.BeatmapObjectCompatible),
            new ModifierAction(nameof(ModifierActions.playerMoveIndex),  ModifierActions.playerMoveIndex, ModifierCompatibility.LevelControlCompatible),
            new ModifierAction(nameof(ModifierActions.playerMoveAll),  ModifierActions.playerMoveAll, ModifierCompatibility.LevelControlCompatible),
            new ModifierAction(nameof(ModifierActions.playerMoveX),  ModifierActions.playerMoveX, ModifierCompatibility.BeatmapObjectCompatible),
            new ModifierAction(nameof(ModifierActions.playerMoveXIndex),  ModifierActions.playerMoveXIndex, ModifierCompatibility.LevelControlCompatible),
            new ModifierAction(nameof(ModifierActions.playerMoveXAll),  ModifierActions.playerMoveXAll, ModifierCompatibility.LevelControlCompatible),
            new ModifierAction(nameof(ModifierActions.playerMoveY),  ModifierActions.playerMoveY, ModifierCompatibility.BeatmapObjectCompatible),
            new ModifierAction(nameof(ModifierActions.playerMoveYIndex),  ModifierActions.playerMoveYIndex, ModifierCompatibility.LevelControlCompatible),
            new ModifierAction(nameof(ModifierActions.playerMoveYAll),  ModifierActions.playerMoveYAll, ModifierCompatibility.LevelControlCompatible),
            new ModifierAction(nameof(ModifierActions.playerRotate),  ModifierActions.playerRotate, ModifierCompatibility.BeatmapObjectCompatible),
            new ModifierAction(nameof(ModifierActions.playerRotateIndex),  ModifierActions.playerRotateIndex, ModifierCompatibility.LevelControlCompatible),
            new ModifierAction(nameof(ModifierActions.playerRotateAll),  ModifierActions.playerRotateAll, ModifierCompatibility.LevelControlCompatible),

            // move to object
            new ModifierAction(nameof(ModifierActions.playerMoveToObject),  ModifierActions.playerMoveToObject, ModifierCompatibility.BeatmapObjectCompatible),
            new ModifierAction(nameof(ModifierActions.playerMoveIndexToObject),  ModifierActions.playerMoveIndexToObject, ModifierCompatibility.BeatmapObjectCompatible),
            new ModifierAction(nameof(ModifierActions.playerMoveAllToObject),  ModifierActions.playerMoveAllToObject, ModifierCompatibility.BeatmapObjectCompatible),
            new ModifierAction(nameof(ModifierActions.playerMoveXToObject),  ModifierActions.playerMoveXToObject, ModifierCompatibility.BeatmapObjectCompatible),
            new ModifierAction(nameof(ModifierActions.playerMoveXIndexToObject),  ModifierActions.playerMoveXIndexToObject, ModifierCompatibility.BeatmapObjectCompatible),
            new ModifierAction(nameof(ModifierActions.playerMoveXAllToObject),  ModifierActions.playerMoveXAllToObject, ModifierCompatibility.BeatmapObjectCompatible),
            new ModifierAction(nameof(ModifierActions.playerMoveYToObject),  ModifierActions.playerMoveYToObject, ModifierCompatibility.BeatmapObjectCompatible),
            new ModifierAction(nameof(ModifierActions.playerMoveYIndexToObject),  ModifierActions.playerMoveYIndexToObject, ModifierCompatibility.BeatmapObjectCompatible),
            new ModifierAction(nameof(ModifierActions.playerMoveYAllToObject),  ModifierActions.playerMoveYAllToObject, ModifierCompatibility.BeatmapObjectCompatible),
            new ModifierAction(nameof(ModifierActions.playerRotateToObject),  ModifierActions.playerRotateToObject, ModifierCompatibility.BeatmapObjectCompatible),
            new ModifierAction(nameof(ModifierActions.playerRotateIndexToObject),  ModifierActions.playerRotateIndexToObject, ModifierCompatibility.BeatmapObjectCompatible),
            new ModifierAction(nameof(ModifierActions.playerRotateAllToObject),  ModifierActions.playerRotateAllToObject, ModifierCompatibility.BeatmapObjectCompatible),
            new ModifierAction(nameof(ModifierActions.playerDrag),  ModifierActions.playerDrag, ModifierCompatibility.BeatmapObjectCompatible),

            // actions
            new ModifierAction(nameof(ModifierActions.playerBoost),  ModifierActions.playerBoost, ModifierCompatibility.BeatmapObjectCompatible),
            new ModifierAction(nameof(ModifierActions.playerBoostIndex),  ModifierActions.playerBoostIndex, ModifierCompatibility.LevelControlCompatible),
            new ModifierAction(nameof(ModifierActions.playerBoostAll),  ModifierActions.playerBoostAll, ModifierCompatibility.LevelControlCompatible),
            new ModifierAction(nameof(ModifierActions.playerCancelBoost),  ModifierActions.playerCancelBoost, ModifierCompatibility.BeatmapObjectCompatible),
            new ModifierAction(nameof(ModifierActions.playerCancelBoostIndex),  ModifierActions.playerCancelBoostIndex, ModifierCompatibility.LevelControlCompatible),
            new ModifierAction(nameof(ModifierActions.playerCancelBoostAll),  ModifierActions.playerCancelBoostAll, ModifierCompatibility.LevelControlCompatible),
            new ModifierAction(nameof(ModifierActions.playerDisableBoost),  ModifierActions.playerDisableBoost, ModifierCompatibility.BeatmapObjectCompatible),
            new ModifierAction(nameof(ModifierActions.playerDisableBoostIndex),  ModifierActions.playerDisableBoostIndex, ModifierCompatibility.LevelControlCompatible),
            new ModifierAction(nameof(ModifierActions.playerDisableBoostAll),  ModifierActions.playerDisableBoostAll, ModifierCompatibility.LevelControlCompatible),
            new ModifierAction(nameof(ModifierActions.playerEnableBoost),  ModifierActions.playerEnableBoost, ModifierCompatibility.BeatmapObjectCompatible),
            new ModifierAction(nameof(ModifierActions.playerEnableBoostIndex),  ModifierActions.playerEnableBoostIndex, ModifierCompatibility.LevelControlCompatible),
            new ModifierAction(nameof(ModifierActions.playerEnableBoostAll),  ModifierActions.playerEnableBoostAll, ModifierCompatibility.LevelControlCompatible),
            new ModifierAction(nameof(ModifierActions.playerEnableMove),  ModifierActions.playerEnableMove, ModifierCompatibility.BeatmapObjectCompatible),
            new ModifierAction(nameof(ModifierActions.playerEnableMoveIndex),  ModifierActions.playerEnableMoveIndex, ModifierCompatibility.LevelControlCompatible),
            new ModifierAction(nameof(ModifierActions.playerEnableMoveAll),  ModifierActions.playerEnableMoveAll, ModifierCompatibility.LevelControlCompatible),

            // speed
            new ModifierAction(nameof(ModifierActions.playerSpeed),  ModifierActions.playerSpeed, ModifierCompatibility.LevelControlCompatible),
            new ModifierAction(nameof(ModifierActions.playerVelocity),  ModifierActions.playerVelocity, ModifierCompatibility.BeatmapObjectCompatible),
            new ModifierAction(nameof(ModifierActions.playerVelocityIndex),  ModifierActions.playerVelocityIndex, ModifierCompatibility.LevelControlCompatible),
            new ModifierAction(nameof(ModifierActions.playerVelocityAll),  ModifierActions.playerVelocityAll, ModifierCompatibility.LevelControlCompatible),
            new ModifierAction(nameof(ModifierActions.playerVelocityX),  ModifierActions.playerVelocityX, ModifierCompatibility.BeatmapObjectCompatible),
            new ModifierAction(nameof(ModifierActions.playerVelocityXIndex),  ModifierActions.playerVelocityXIndex, ModifierCompatibility.LevelControlCompatible),
            new ModifierAction(nameof(ModifierActions.playerVelocityXAll),  ModifierActions.playerVelocityXAll, ModifierCompatibility.LevelControlCompatible),
            new ModifierAction(nameof(ModifierActions.playerVelocityY),  ModifierActions.playerVelocityY, ModifierCompatibility.BeatmapObjectCompatible),
            new ModifierAction(nameof(ModifierActions.playerVelocityYIndex),  ModifierActions.playerVelocityYIndex, ModifierCompatibility.LevelControlCompatible),
            new ModifierAction(nameof(ModifierActions.playerVelocityYAll),  ModifierActions.playerVelocityYAll, ModifierCompatibility.LevelControlCompatible),

            new ModifierAction(nameof(ModifierActions.playerEnableDamage),  ModifierActions.playerEnableDamage, ModifierCompatibility.LevelControlCompatible),
            new ModifierAction(nameof(ModifierActions.playerEnableDamageIndex),  ModifierActions.playerEnableDamageIndex, ModifierCompatibility.LevelControlCompatible),
            new ModifierAction(nameof(ModifierActions.playerEnableDamageAll),  ModifierActions.playerEnableDamageAll, ModifierCompatibility.LevelControlCompatible),
            new ModifierAction(nameof(ModifierActions.setPlayerModel),  ModifierActions.setPlayerModel, ModifierCompatibility.LevelControlCompatible),
            new ModifierAction(nameof(ModifierActions.setGameMode),  ModifierActions.setGameMode, ModifierCompatibility.LevelControlCompatible),
            new ModifierAction(nameof(ModifierActions.gameMode),  ModifierActions.setGameMode, ModifierCompatibility.LevelControlCompatible),

            new ModifierAction(nameof(ModifierActions.blackHole),  ModifierActions.blackHole, ModifierCompatibility.BeatmapObjectCompatible),
            new ModifierAction(nameof(ModifierActions.blackHoleIndex),  ModifierActions.blackHoleIndex, ModifierCompatibility.BeatmapObjectCompatible),
            new ModifierAction(nameof(ModifierActions.blackHoleAll),  ModifierActions.blackHoleAll, ModifierCompatibility.BeatmapObjectCompatible),
            new ModifierAction(nameof(ModifierActions.whiteHole),  ModifierActions.whiteHole, ModifierCompatibility.BeatmapObjectCompatible),
            new ModifierAction(nameof(ModifierActions.whiteHoleIndex),  ModifierActions.whiteHoleIndex, ModifierCompatibility.BeatmapObjectCompatible),
            new ModifierAction(nameof(ModifierActions.whiteHoleAll),  ModifierActions.whiteHoleAll, ModifierCompatibility.BeatmapObjectCompatible),

            #endregion

            #region Mouse Cursor

            new ModifierAction(nameof(ModifierActions.showMouse),  ModifierActions.showMouse, ModifierCompatibility.LevelControlCompatible),
            new ModifierAction(nameof(ModifierActions.hideMouse),  ModifierActions.hideMouse, ModifierCompatibility.LevelControlCompatible),
            new ModifierAction(nameof(ModifierActions.setMousePosition),  ModifierActions.setMousePosition, ModifierCompatibility.LevelControlCompatible),
            new ModifierAction(nameof(ModifierActions.followMousePosition),  ModifierActions.followMousePosition, ModifierCompatibility.FullBeatmapCompatible),

            #endregion

            #region Variable

            new ModifierAction(nameof(ModifierActions.getToggle),  ModifierActions.getToggle),
            new ModifierAction(nameof(ModifierActions.getFloat),  ModifierActions.getFloat),
            new ModifierAction(nameof(ModifierActions.getInt),  ModifierActions.getInt),
            new ModifierAction(nameof(ModifierActions.getString),  ModifierActions.getString),
            new ModifierAction(nameof(ModifierActions.getStringLower),  ModifierActions.getStringLower),
            new ModifierAction(nameof(ModifierActions.getStringUpper),  ModifierActions.getStringUpper),
            new ModifierAction(nameof(ModifierActions.getColor),  ModifierActions.getColor),
            new ModifierAction(nameof(ModifierActions.getEnum),  ModifierActions.getEnum),
            new ModifierAction(nameof(ModifierActions.getTag),  ModifierActions.getTag),
            new ModifierAction(nameof(ModifierActions.getPitch),  ModifierActions.getPitch),
            new ModifierAction(nameof(ModifierActions.getMusicTime),  ModifierActions.getMusicTime),
            new ModifierAction(nameof(ModifierActions.getAxis),  ModifierActions.getAxis),
            new ModifierAction(nameof(ModifierActions.getMath),  ModifierActions.getMath),
            new ModifierAction(nameof(ModifierActions.getNearestPlayer),  ModifierActions.getNearestPlayer, ModifierCompatibility.BeatmapObjectCompatible),
            new ModifierAction(nameof(ModifierActions.getCollidingPlayers),  ModifierActions.getCollidingPlayers, ModifierCompatibility.BeatmapObjectCompatible),
            new ModifierAction(nameof(ModifierActions.getPlayerHealth),  ModifierActions.getPlayerHealth),
            new ModifierAction(nameof(ModifierActions.getPlayerLives),  ModifierActions.getPlayerLives),
            new ModifierAction(nameof(ModifierActions.getPlayerPosX),  ModifierActions.getPlayerPosX),
            new ModifierAction(nameof(ModifierActions.getPlayerPosY),  ModifierActions.getPlayerPosY),
            new ModifierAction(nameof(ModifierActions.getPlayerRot),  ModifierActions.getPlayerRot),
            new ModifierAction(nameof(ModifierActions.getEventValue),  ModifierActions.getEventValue),
            new ModifierAction(nameof(ModifierActions.getSample),  ModifierActions.getSample),
            new ModifierAction(nameof(ModifierActions.getText),  ModifierActions.getText, ModifierCompatibility.BeatmapObjectCompatible),
            new ModifierAction(nameof(ModifierActions.getTextOther),  ModifierActions.getTextOther, ModifierCompatibility.BeatmapObjectCompatible),
            new ModifierAction(nameof(ModifierActions.getCurrentKey),  ModifierActions.getCurrentKey),
            new ModifierAction(nameof(ModifierActions.getColorSlotHexCode),  ModifierActions.getColorSlotHexCode),
            new ModifierAction(nameof(ModifierActions.getFloatFromHexCode),  ModifierActions.getFloatFromHexCode),
            new ModifierAction(nameof(ModifierActions.getHexCodeFromFloat),  ModifierActions.getHexCodeFromFloat),
            new ModifierAction(nameof(ModifierActions.getModifiedColor),  ModifierActions.getModifiedColor),
            new ModifierAction(nameof(ModifierActions.getMixedColors),  ModifierActions.getMixedColors),
            new ModifierAction(nameof(ModifierActions.getVisualColor),  ModifierActions.getVisualColor),
            new ModifierAction(nameof(ModifierActions.getVisualOpacity),  ModifierActions.getVisualOpacity),
            new ModifierAction(nameof(ModifierActions.getJSONString),  ModifierActions.getJSONString),
            new ModifierAction(nameof(ModifierActions.getJSONFloat),  ModifierActions.getJSONFloat),
            new ModifierAction(nameof(ModifierActions.getJSON),  ModifierActions.getJSON),
            new ModifierAction(nameof(ModifierActions.getSubString),  ModifierActions.getSubString),
            new ModifierAction(nameof(ModifierActions.getSplitString),  ModifierActions.getSplitString),
            new ModifierAction(nameof(ModifierActions.getSplitStringAt),  ModifierActions.getSplitStringAt),
            new ModifierAction(nameof(ModifierActions.getSplitStringCount),  ModifierActions.getSplitStringCount),
            new ModifierAction(nameof(ModifierActions.getStringLength),  ModifierActions.getStringLength),
            new ModifierAction(nameof(ModifierActions.getParsedString),  ModifierActions.getParsedString),
            new ModifierAction(nameof(ModifierActions.getRegex),  ModifierActions.getRegex),
            new ModifierAction(nameof(ModifierActions.getFormatVariable),  ModifierActions.getFormatVariable),
            new ModifierAction(nameof(ModifierActions.getComparison),  ModifierActions.getComparison),
            new ModifierAction(nameof(ModifierActions.getComparisonMath),  ModifierActions.getComparisonMath),
            new ModifierAction(nameof(ModifierActions.getEditorBin),  ModifierActions.getEditorBin),
            new ModifierAction(nameof(ModifierActions.getEditorLayer),  ModifierActions.getEditorLayer),
            new ModifierAction(nameof(ModifierActions.getObjectName),  ModifierActions.getObjectName),
            new ModifierAction(nameof(ModifierActions.getSignaledVariables),  ModifierActions.getSignaledVariables),
            new ModifierAction(nameof(ModifierActions.signalLocalVariables),  ModifierActions.signalLocalVariables),
            new ModifierAction(nameof(ModifierActions.clearLocalVariables),  ModifierActions.clearLocalVariables),
            new ModifierAction(nameof(ModifierActions.storeLocalVariables),  ModifierActions.storeLocalVariables),

            new ModifierAction(nameof(ModifierActions.addVariable),  ModifierActions.addVariable),
            new ModifierAction(nameof(ModifierActions.addVariableOther),  ModifierActions.addVariableOther, ModifierCompatibility.LevelControlCompatible),
            new ModifierAction(nameof(ModifierActions.subVariable),  ModifierActions.subVariable),
            new ModifierAction(nameof(ModifierActions.subVariableOther),  ModifierActions.subVariableOther, ModifierCompatibility.LevelControlCompatible),
            new ModifierAction(nameof(ModifierActions.setVariable),  ModifierActions.setVariable),
            new ModifierAction(nameof(ModifierActions.setVariableOther),  ModifierActions.setVariableOther, ModifierCompatibility.LevelControlCompatible),
            new ModifierAction(nameof(ModifierActions.setVariableRandom),  ModifierActions.setVariableRandom),
            new ModifierAction(nameof(ModifierActions.setVariableRandomOther),  ModifierActions.setVariableRandomOther, ModifierCompatibility.LevelControlCompatible),
            new ModifierAction(nameof(ModifierActions.animateVariableOther),  ModifierActions.animateVariableOther, ModifierCompatibility.LevelControlCompatible),
            new ModifierAction(nameof(ModifierActions.clampVariable),  ModifierActions.clampVariable),
            new ModifierAction(nameof(ModifierActions.clampVariableOther),  ModifierActions.clampVariableOther, ModifierCompatibility.LevelControlCompatible),

            #endregion

            #region Enable

            // old bg modifiers
            new ModifierAction(nameof(ModifierActions.setActive), ModifierActions.setActive, ModifierCompatibility.BackgroundObjectCompatible),
            new ModifierAction(nameof(ModifierActions.setActiveOther), ModifierActions.setActiveOther, ModifierCompatibility.BackgroundObjectCompatible),

            // enable
            new ModifierAction(nameof(ModifierActions.enableObject),  ModifierActions.enableObject, ModifierCompatibility.FullBeatmapCompatible.WithPAPlayer()),
            new ModifierAction(nameof(ModifierActions.enableObjectTree),  ModifierActions.enableObjectTree, ModifierCompatibility.BeatmapObjectCompatible),
            new ModifierAction(nameof(ModifierActions.enableObjectOther),  ModifierActions.enableObjectOther, ModifierCompatibility.FullBeatmapCompatible),
            new ModifierAction(nameof(ModifierActions.enableObjectTreeOther),  ModifierActions.enableObjectTreeOther, ModifierCompatibility.BeatmapObjectCompatible),
            new ModifierAction(nameof(ModifierActions.enableObjectGroup),  ModifierActions.enableObjectGroup, ModifierCompatibility.FullBeatmapCompatible),

            // disable
            new ModifierAction(nameof(ModifierActions.disableObject),  ModifierActions.disableObject, ModifierCompatibility.BeatmapObjectCompatible),
            new ModifierAction(nameof(ModifierActions.disableObjectTree),  ModifierActions.disableObjectTree, ModifierCompatibility.BeatmapObjectCompatible),
            new ModifierAction(nameof(ModifierActions.disableObjectOther),  ModifierActions.disableObjectOther, ModifierCompatibility.BeatmapObjectCompatible),
            new ModifierAction(nameof(ModifierActions.disableObjectTreeOther),  ModifierActions.disableObjectTreeOther, ModifierCompatibility.BeatmapObjectCompatible),

            #endregion

            #region JSON

            new ModifierAction(nameof(ModifierActions.saveFloat),  ModifierActions.saveFloat, ModifierCompatibility.LevelControlCompatible),
            new ModifierAction(nameof(ModifierActions.saveString),  ModifierActions.saveString, ModifierCompatibility.LevelControlCompatible),
            new ModifierAction(nameof(ModifierActions.saveText),  ModifierActions.saveText, ModifierCompatibility.LevelControlCompatible),
            new ModifierAction(nameof(ModifierActions.saveVariable),  ModifierActions.saveVariable, ModifierCompatibility.LevelControlCompatible),
            new ModifierAction(nameof(ModifierActions.loadVariable),  ModifierActions.loadVariable, ModifierCompatibility.LevelControlCompatible),
            new ModifierAction(nameof(ModifierActions.loadVariableOther),  ModifierActions.loadVariableOther, ModifierCompatibility.LevelControlCompatible),

            #endregion

            #region Reactive

            // single
            new ModifierAction(nameof(ModifierActions.reactivePos),  ModifierActions.reactivePos, ModifierCompatibility.BeatmapObjectCompatible),
            new ModifierAction(nameof(ModifierActions.reactiveSca),  ModifierActions.reactiveSca, ModifierCompatibility.BeatmapObjectCompatible),
            new ModifierAction(nameof(ModifierActions.reactiveRot),  ModifierActions.reactiveRot, ModifierCompatibility.BeatmapObjectCompatible),
            new ModifierAction(nameof(ModifierActions.reactiveCol),  ModifierActions.reactiveCol, ModifierCompatibility.BeatmapObjectCompatible),
            new ModifierAction(nameof(ModifierActions.reactiveColLerp),  ModifierActions.reactiveColLerp, ModifierCompatibility.BeatmapObjectCompatible),

            // chain
            new ModifierAction(nameof(ModifierActions.reactivePosChain),  ModifierActions.reactivePosChain, ModifierCompatibility.BeatmapObjectCompatible),
            new ModifierAction(nameof(ModifierActions.reactiveScaChain),  ModifierActions.reactiveScaChain, ModifierCompatibility.BeatmapObjectCompatible),
            new ModifierAction(nameof(ModifierActions.reactiveRotChain),  ModifierActions.reactiveRotChain, ModifierCompatibility.BeatmapObjectCompatible),

            #endregion

            #region Events

            new ModifierAction(nameof(ModifierActions.eventOffset),  ModifierActions.eventOffset, ModifierCompatibility.LevelControlCompatible),
            new ModifierAction(nameof(ModifierActions.eventOffsetVariable),  ModifierActions.eventOffsetVariable, ModifierCompatibility.LevelControlCompatible),
            new ModifierAction(nameof(ModifierActions.eventOffsetMath),  ModifierActions.eventOffsetMath, ModifierCompatibility.LevelControlCompatible),
            new ModifierAction(nameof(ModifierActions.eventOffsetAnimate),  ModifierActions.eventOffsetAnimate, ModifierCompatibility.LevelControlCompatible),
            new ModifierAction(nameof(ModifierActions.eventOffsetCopyAxis),  ModifierActions.eventOffsetCopyAxis, ModifierCompatibility.BeatmapObjectCompatible),
            new ModifierAction(nameof(ModifierActions.vignetteTracksPlayer),  ModifierActions.vignetteTracksPlayer, ModifierCompatibility.LevelControlCompatible),
            new ModifierAction(nameof(ModifierActions.lensTracksPlayer),  ModifierActions.lensTracksPlayer, ModifierCompatibility.LevelControlCompatible),

            #endregion

            // todo: implement gradients and different color controls
            #region Color
            
            new ModifierAction(nameof(ModifierActions.mask),  ModifierActions.mask, ModifierCompatibility.BeatmapObjectCompatible),

            // color
            new ModifierAction(nameof(ModifierActions.addColor),  ModifierActions.addColor, ModifierCompatibility.BeatmapObjectCompatible),
            new ModifierAction(nameof(ModifierActions.addColorOther),  ModifierActions.addColorOther, ModifierCompatibility.BeatmapObjectCompatible),
            new ModifierAction(nameof(ModifierActions.lerpColor),  ModifierActions.lerpColor, ModifierCompatibility.BeatmapObjectCompatible),
            new ModifierAction(nameof(ModifierActions.lerpColorOther),  ModifierActions.lerpColorOther, ModifierCompatibility.BeatmapObjectCompatible),
            new ModifierAction(nameof(ModifierActions.addColorPlayerDistance),  ModifierActions.addColorPlayerDistance, ModifierCompatibility.BeatmapObjectCompatible),
            new ModifierAction(nameof(ModifierActions.lerpColorPlayerDistance),  ModifierActions.lerpColorPlayerDistance, ModifierCompatibility.BeatmapObjectCompatible),

            // opacity
            new ModifierAction("setAlpha",  ModifierActions.setOpacity, ModifierCompatibility.BeatmapObjectCompatible),
            new ModifierAction(nameof(ModifierActions.setOpacity),  ModifierActions.setOpacity, ModifierCompatibility.BeatmapObjectCompatible),
            new ModifierAction("setAlphaOther",  ModifierActions.setOpacityOther, ModifierCompatibility.BeatmapObjectCompatible),
            new ModifierAction(nameof(ModifierActions.setOpacityOther),  ModifierActions.setOpacityOther, ModifierCompatibility.BeatmapObjectCompatible),

            // copy
            new ModifierAction(nameof(ModifierActions.copyColor),  ModifierActions.copyColor, ModifierCompatibility.BeatmapObjectCompatible),
            new ModifierAction(nameof(ModifierActions.copyColorOther),  ModifierActions.copyColorOther, ModifierCompatibility.BeatmapObjectCompatible),
            new ModifierAction(nameof(ModifierActions.applyColorGroup),  ModifierActions.applyColorGroup, ModifierCompatibility.BeatmapObjectCompatible),

            // hex code
            new ModifierAction(nameof(ModifierActions.setColorHex),  ModifierActions.setColorHex, ModifierCompatibility.BeatmapObjectCompatible),
            new ModifierAction(nameof(ModifierActions.setColorHexOther),  ModifierActions.setColorHexOther, ModifierCompatibility.BeatmapObjectCompatible),

            // rgba
            new ModifierAction(nameof(ModifierActions.setColorRGBA),  ModifierActions.setColorRGBA, ModifierCompatibility.BeatmapObjectCompatible),
            new ModifierAction(nameof(ModifierActions.setColorRGBAOther),  ModifierActions.setColorRGBAOther, ModifierCompatibility.BeatmapObjectCompatible),

            new ModifierAction(nameof(ModifierActions.animateColorKF),  ModifierActions.animateColorKF, ModifierCompatibility.BeatmapObjectCompatible.WithBackgroundObject()),
            new ModifierAction(nameof(ModifierActions.animateColorKFHex),  ModifierActions.animateColorKFHex, ModifierCompatibility.BeatmapObjectCompatible.WithBackgroundObject()),

            #endregion

            #region Shape
            
            new ModifierAction(nameof(ModifierActions.translateShape),  ModifierActions.translateShape, ModifierCompatibility.BeatmapObjectCompatible),
            new ModifierAction(nameof(ModifierActions.setShape),  ModifierActions.setShape, ModifierCompatibility.BeatmapObjectCompatible.WithBackgroundObject()),
            new ModifierAction(nameof(ModifierActions.setPolygonShape),  ModifierActions.setPolygonShape, ModifierCompatibility.BeatmapObjectCompatible),
            new ModifierAction(nameof(ModifierActions.setPolygonShapeOther),  ModifierActions.setPolygonShapeOther, ModifierCompatibility.BeatmapObjectCompatible),

            // image
            new ModifierAction(nameof(ModifierActions.setImage),  ModifierActions.setImage, ModifierCompatibility.BeatmapObjectCompatible),
            new ModifierAction(nameof(ModifierActions.setImageOther),  ModifierActions.setImageOther, ModifierCompatibility.BeatmapObjectCompatible),

            // text (pain)
            new ModifierAction(nameof(ModifierActions.setText),  ModifierActions.setText, ModifierCompatibility.BeatmapObjectCompatible),
            new ModifierAction(nameof(ModifierActions.setTextOther),  ModifierActions.setTextOther, ModifierCompatibility.BeatmapObjectCompatible),
            new ModifierAction(nameof(ModifierActions.addText),  ModifierActions.addText, ModifierCompatibility.BeatmapObjectCompatible),
            new ModifierAction(nameof(ModifierActions.addTextOther),  ModifierActions.addTextOther, ModifierCompatibility.BeatmapObjectCompatible),
            new ModifierAction(nameof(ModifierActions.removeText),  ModifierActions.removeText, ModifierCompatibility.BeatmapObjectCompatible),
            new ModifierAction(nameof(ModifierActions.removeTextOther),  ModifierActions.removeTextOther, ModifierCompatibility.BeatmapObjectCompatible),
            new ModifierAction(nameof(ModifierActions.removeTextAt),  ModifierActions.removeTextAt, ModifierCompatibility.BeatmapObjectCompatible),
            new ModifierAction(nameof(ModifierActions.removeTextOtherAt),  ModifierActions.removeTextOtherAt, ModifierCompatibility.BeatmapObjectCompatible),
            new ModifierAction(nameof(ModifierActions.formatText),  ModifierActions.formatText, ModifierCompatibility.BeatmapObjectCompatible),
            new ModifierAction(nameof(ModifierActions.textSequence),  ModifierActions.textSequence, ModifierCompatibility.BeatmapObjectCompatible),

            // modify shape
            new ModifierAction(nameof(ModifierActions.backgroundShape),  ModifierActions.backgroundShape, ModifierCompatibility.BeatmapObjectCompatible),
            new ModifierAction(nameof(ModifierActions.sphereShape),  ModifierActions.sphereShape, ModifierCompatibility.BeatmapObjectCompatible),

            #endregion

            #region Animation

            new ModifierAction(nameof(ModifierActions.animateObject),  ModifierActions.animateObject),
            new ModifierAction(nameof(ModifierActions.animateObjectOther),  ModifierActions.animateObjectOther, ModifierCompatibility.LevelControlCompatible),
            new ModifierAction(nameof(ModifierActions.animateObjectKF),  ModifierActions.animateObjectKF),
            new ModifierAction(nameof(ModifierActions.animateSignal),  ModifierActions.animateSignal, ModifierCompatibility.LevelControlCompatible),
            new ModifierAction(nameof(ModifierActions.animateSignalOther),  ModifierActions.animateSignalOther, ModifierCompatibility.LevelControlCompatible),

            new ModifierAction(nameof(ModifierActions.animateObjectMath),  ModifierActions.animateObjectMath),
            new ModifierAction(nameof(ModifierActions.animateObjectMathOther),  ModifierActions.animateObjectMathOther, ModifierCompatibility.LevelControlCompatible),
            new ModifierAction(nameof(ModifierActions.animateSignalMath),  ModifierActions.animateSignalMath, ModifierCompatibility.LevelControlCompatible),
            new ModifierAction(nameof(ModifierActions.animateSignalMathOther),  ModifierActions.animateSignalMathOther, ModifierCompatibility.LevelControlCompatible),

            new ModifierAction(nameof(ModifierActions.applyAnimation),  ModifierActions.applyAnimation, ModifierCompatibility.BeatmapObjectCompatible),
            new ModifierAction(nameof(ModifierActions.applyAnimationFrom),  ModifierActions.applyAnimationFrom, ModifierCompatibility.BeatmapObjectCompatible),
            new ModifierAction(nameof(ModifierActions.applyAnimationTo),  ModifierActions.applyAnimationTo, ModifierCompatibility.BeatmapObjectCompatible),
            new ModifierAction(nameof(ModifierActions.applyAnimationMath),  ModifierActions.applyAnimationMath, ModifierCompatibility.BeatmapObjectCompatible),
            new ModifierAction(nameof(ModifierActions.applyAnimationFromMath),  ModifierActions.applyAnimationFromMath, ModifierCompatibility.BeatmapObjectCompatible),
            new ModifierAction(nameof(ModifierActions.applyAnimationToMath),  ModifierActions.applyAnimationToMath, ModifierCompatibility.BeatmapObjectCompatible),

            new ModifierAction(nameof(ModifierActions.copyAxis),  ModifierActions.copyAxis),
            new ModifierAction(nameof(ModifierActions.copyAxisMath),  ModifierActions.copyAxisMath),
            new ModifierAction(nameof(ModifierActions.copyAxisGroup),  ModifierActions.copyAxisGroup),
            new ModifierAction(nameof(ModifierActions.copyPlayerAxis),  ModifierActions.copyPlayerAxis),

            new ModifierAction(nameof(ModifierActions.legacyTail),  ModifierActions.legacyTail, ModifierCompatibility.BeatmapObjectCompatible),

            new ModifierAction(nameof(ModifierActions.gravity),  ModifierActions.gravity),
            new ModifierAction(nameof(ModifierActions.gravityOther),  ModifierActions.gravityOther),

            #endregion

            #region Prefab

            new ModifierAction(nameof(ModifierActions.spawnPrefab),  ModifierActions.spawnPrefab, ModifierCompatibility.LevelControlCompatible),
            new ModifierAction(nameof(ModifierActions.spawnPrefabOffset),  ModifierActions.spawnPrefabOffset, ModifierCompatibility.LevelControlCompatible),
            new ModifierAction(nameof(ModifierActions.spawnPrefabOffsetOther),  ModifierActions.spawnPrefabOffsetOther, ModifierCompatibility.LevelControlCompatible),
            new ModifierAction(nameof(ModifierActions.spawnPrefabCopy),  ModifierActions.spawnPrefabCopy, ModifierCompatibility.LevelControlCompatible),
            new ModifierAction(nameof(ModifierActions.spawnMultiPrefab),  ModifierActions.spawnMultiPrefab, ModifierCompatibility.LevelControlCompatible),
            new ModifierAction(nameof(ModifierActions.spawnMultiPrefabOffset),  ModifierActions.spawnMultiPrefabOffset, ModifierCompatibility.LevelControlCompatible),
            new ModifierAction(nameof(ModifierActions.spawnMultiPrefabOffsetOther),  ModifierActions.spawnMultiPrefabOffsetOther, ModifierCompatibility.LevelControlCompatible),
            new ModifierAction(nameof(ModifierActions.spawnMultiPrefabCopy),  ModifierActions.spawnMultiPrefabCopy, ModifierCompatibility.LevelControlCompatible),
            new ModifierAction(nameof(ModifierActions.clearSpawnedPrefabs),  ModifierActions.clearSpawnedPrefabs, ModifierCompatibility.LevelControlCompatible),
            new ModifierAction(nameof(ModifierActions.setPrefabTime),  ModifierActions.setPrefabTime, ModifierCompatibility.PrefabObjectCompatible),

            #endregion

            #region Ranking

            new ModifierAction(nameof(ModifierActions.saveLevelRank), ModifierActions.saveLevelRank, ModifierCompatibility.LevelControlCompatible),
            new ModifierAction(nameof(ModifierActions.unlockAchievement), ModifierActions.unlockAchievement, ModifierCompatibility.LevelControlCompatible),
            new ModifierAction(nameof(ModifierActions.lockAchievement), ModifierActions.lockAchievement, ModifierCompatibility.LevelControlCompatible),
            new ModifierAction(nameof(ModifierActions.getAchievementUnlocked), ModifierActions.getAchievementUnlocked),

            new ModifierAction(nameof(ModifierActions.clearHits), ModifierActions.clearHits, ModifierCompatibility.LevelControlCompatible),
            new ModifierAction(nameof(ModifierActions.addHit), ModifierActions.addHit, ModifierCompatibility.LevelControlCompatible),
            new ModifierAction(nameof(ModifierActions.subHit), ModifierActions.subHit, ModifierCompatibility.LevelControlCompatible),
            new ModifierAction(nameof(ModifierActions.clearDeaths), ModifierActions.clearDeaths, ModifierCompatibility.LevelControlCompatible),
            new ModifierAction(nameof(ModifierActions.addDeath), ModifierActions.addDeath, ModifierCompatibility.LevelControlCompatible),
            new ModifierAction(nameof(ModifierActions.subDeath), ModifierActions.subDeath, ModifierCompatibility.LevelControlCompatible),

            new ModifierAction(nameof(ModifierActions.getHitCount), ModifierActions.getHitCount),
            new ModifierAction(nameof(ModifierActions.getDeathCount), ModifierActions.getDeathCount),

            #endregion

            #region Updates

            // update
            new ModifierAction(nameof(ModifierActions.reinitLevel),  ModifierActions.reinitLevel, ModifierCompatibility.LevelControlCompatible),
            new ModifierAction(nameof(ModifierActions.updateObjects),  ModifierActions.updateObjects, ModifierCompatibility.LevelControlCompatible),
            new ModifierAction(nameof(ModifierActions.updateObject),  ModifierActions.updateObject, ModifierCompatibility.FullBeatmapCompatible),
            new ModifierAction(nameof(ModifierActions.updateObjectOther),  ModifierActions.updateObjectOther, ModifierCompatibility.FullBeatmapCompatible),

            // parent
            new ModifierAction(nameof(ModifierActions.setParent),  ModifierActions.setParent, ModifierCompatibility.BeatmapObjectCompatible.WithPrefabObject(true)),
            new ModifierAction(nameof(ModifierActions.setParentOther),  ModifierActions.setParentOther, ModifierCompatibility.BeatmapObjectCompatible.WithPrefabObject(true)),
            new ModifierAction(nameof(ModifierActions.detachParent),  ModifierActions.detachParent, ModifierCompatibility.BeatmapObjectCompatible.WithPrefabObject(true)),
            new ModifierAction(nameof(ModifierActions.detachParentOther),  ModifierActions.detachParentOther, ModifierCompatibility.BeatmapObjectCompatible.WithPrefabObject(true)),

            new ModifierAction(nameof(ModifierActions.setSeed),  ModifierActions.setSeed, ModifierCompatibility.BeatmapObjectCompatible.WithPrefabObject(true)),

            #endregion

            #region Physics

            // collision
            new ModifierAction(nameof(ModifierActions.setCollision),  ModifierActions.setCollision, ModifierCompatibility.BeatmapObjectCompatible),
            new ModifierAction(nameof(ModifierActions.setCollisionOther),  ModifierActions.setCollisionOther, ModifierCompatibility.BeatmapObjectCompatible),

            #endregion

            #region Checkpoints

            new ModifierAction(nameof(ModifierActions.createCheckpoint),  ModifierActions.createCheckpoint, ModifierCompatibility.LevelControlCompatible),
            new ModifierAction(nameof(ModifierActions.resetCheckpoint),  ModifierActions.resetCheckpoint, ModifierCompatibility.LevelControlCompatible),

            #endregion

            #region Interfaces

            new ModifierAction(nameof(ModifierActions.loadInterface),  ModifierActions.loadInterface, ModifierCompatibility.LevelControlCompatible),
            new ModifierAction(nameof(ModifierActions.exitInterface),  ModifierActions.exitInterface, ModifierCompatibility.LevelControlCompatible),
            new ModifierAction(nameof(ModifierActions.quitToMenu),  ModifierActions.quitToMenu, ModifierCompatibility.LevelControlCompatible),
            new ModifierAction(nameof(ModifierActions.quitToArcade),  ModifierActions.quitToArcade, ModifierCompatibility.LevelControlCompatible),
            new ModifierAction(nameof(ModifierActions.pauseLevel),  ModifierActions.pauseLevel, ModifierCompatibility.LevelControlCompatible),

            #endregion

            #region Misc

            new ModifierAction(nameof(ModifierActions.setBGActive),  ModifierActions.setBGActive, ModifierCompatibility.LevelControlCompatible),

            // activation
            new ModifierAction(nameof(ModifierActions.signalModifier),  ModifierActions.signalModifier, ModifierCompatibility.LevelControlCompatible),
            new ModifierAction(nameof(ModifierActions.activateModifier),  ModifierActions.activateModifier, ModifierCompatibility.LevelControlCompatible),

            new ModifierAction(nameof(ModifierActions.editorNotify),  ModifierActions.editorNotify, ModifierCompatibility.LevelControlCompatible),

            // external
            new ModifierAction(nameof(ModifierActions.setWindowTitle),  ModifierActions.setWindowTitle, ModifierCompatibility.LevelControlCompatible),
            new ModifierAction(nameof(ModifierActions.setDiscordStatus),  ModifierActions.setDiscordStatus, ModifierCompatibility.LevelControlCompatible),

            new ModifierAction(nameof(ModifierActions.callModifierBlock),  ModifierActions.callModifierBlock, ModifierCompatibility.LevelControlCompatible),

            #endregion

            #region Player Only
            
            new ModifierAction(nameof(ModifierActions.setCustomObjectActive),  ModifierActions.setCustomObjectActive, ModifierCompatibility.FullPlayerCompatible),
            new ModifierAction(nameof(ModifierActions.setCustomObjectIdle),  ModifierActions.setCustomObjectIdle, ModifierCompatibility.FullPlayerCompatible),
            new ModifierAction(nameof(ModifierActions.setIdleAnimation),  ModifierActions.setIdleAnimation, ModifierCompatibility.FullPlayerCompatible),
            new ModifierAction(nameof(ModifierActions.playAnimation),  ModifierActions.playAnimation, ModifierCompatibility.FullPlayerCompatible),
            new ModifierAction(nameof(ModifierActions.kill),  ModifierActions.kill, ModifierCompatibility.PAPlayerCompatible),
            new ModifierAction(nameof(ModifierActions.hit),  ModifierActions.hit, ModifierCompatibility.PAPlayerCompatible),
            new ModifierAction(nameof(ModifierActions.boost),  ModifierActions.boost, ModifierCompatibility.PAPlayerCompatible),
            new ModifierAction(nameof(ModifierActions.shoot),  ModifierActions.shoot, ModifierCompatibility.PAPlayerCompatible),
            new ModifierAction(nameof(ModifierActions.pulse),  ModifierActions.pulse, ModifierCompatibility.PAPlayerCompatible),
            new ModifierAction(nameof(ModifierActions.jump),  ModifierActions.jump, ModifierCompatibility.PAPlayerCompatible),
            new ModifierAction(nameof(ModifierActions.getHealth),  ModifierActions.getHealth, ModifierCompatibility.FullPlayerCompatible),
            new ModifierAction(nameof(ModifierActions.getLives),  ModifierActions.getLives, ModifierCompatibility.FullPlayerCompatible),
            new ModifierAction(nameof(ModifierActions.getMaxHealth),  ModifierActions.getMaxHealth, ModifierCompatibility.FullPlayerCompatible),
            new ModifierAction(nameof(ModifierActions.getMaxLives),  ModifierActions.getMaxLives, ModifierCompatibility.FullPlayerCompatible),
            new ModifierAction(nameof(ModifierActions.getIndex),  ModifierActions.getIndex, ModifierCompatibility.FullPlayerCompatible),
            new ModifierAction(nameof(ModifierActions.getMove),  ModifierActions.getMove, ModifierCompatibility.FullPlayerCompatible),
            new ModifierAction(nameof(ModifierActions.getMoveX),  ModifierActions.getMoveX, ModifierCompatibility.FullPlayerCompatible),
            new ModifierAction(nameof(ModifierActions.getMoveY),  ModifierActions.getMoveY, ModifierCompatibility.FullPlayerCompatible),

            #endregion

            // dev only (story mode)
            #region Dev Only

            new ModifierAction(nameof(ModifierActions.loadSceneDEVONLY),  ModifierActions.loadSceneDEVONLY, ModifierCompatibility.LevelControlCompatible.WithStoryOnly()),
            new ModifierAction(nameof(ModifierActions.loadStoryLevelDEVONLY),  ModifierActions.loadStoryLevelDEVONLY, ModifierCompatibility.LevelControlCompatible.WithStoryOnly()),
            new ModifierAction(nameof(ModifierActions.storySaveBoolDEVONLY),  ModifierActions.storySaveBoolDEVONLY, ModifierCompatibility.LevelControlCompatible.WithStoryOnly()),
            new ModifierAction(nameof(ModifierActions.storySaveIntDEVONLY),  ModifierActions.storySaveIntDEVONLY, ModifierCompatibility.LevelControlCompatible.WithStoryOnly()),
            new ModifierAction(nameof(ModifierActions.storySaveFloatDEVONLY),  ModifierActions.storySaveFloatDEVONLY, ModifierCompatibility.LevelControlCompatible.WithStoryOnly()),
            new ModifierAction(nameof(ModifierActions.storySaveStringDEVONLY),  ModifierActions.storySaveStringDEVONLY, ModifierCompatibility.LevelControlCompatible.WithStoryOnly()),
            new ModifierAction(nameof(ModifierActions.storySaveIntVariableDEVONLY),  ModifierActions.storySaveIntVariableDEVONLY, ModifierCompatibility.LevelControlCompatible.WithStoryOnly()),
            new ModifierAction(nameof(ModifierActions.getStorySaveBoolDEVONLY),  ModifierActions.getStorySaveBoolDEVONLY),
            new ModifierAction(nameof(ModifierActions.getStorySaveIntDEVONLY),  ModifierActions.getStorySaveIntDEVONLY),
            new ModifierAction(nameof(ModifierActions.getStorySaveFloatDEVONLY),  ModifierActions.getStorySaveFloatDEVONLY),
            new ModifierAction(nameof(ModifierActions.getStorySaveStringDEVONLY),  ModifierActions.getStorySaveStringDEVONLY),
            new ModifierAction(nameof(ModifierActions.exampleEnableDEVONLY),  ModifierActions.exampleEnableDEVONLY, ModifierCompatibility.LevelControlCompatible.WithStoryOnly()),
            new ModifierAction(nameof(ModifierActions.exampleSayDEVONLY),  ModifierActions.exampleSayDEVONLY, ModifierCompatibility.LevelControlCompatible.WithStoryOnly()),

            #endregion
        };

        public static ModifierInactive[] inactives = new ModifierInactive[]
        {
            #region Actions

            #region Component

            new ModifierInactive(nameof(ModifierActions.blur),
                (modifier, reference, variables) =>
                {
                    if (reference is BeatmapObject beatmapObject && beatmapObject.objectType != BeatmapObject.ObjectType.Empty &&
                        beatmapObject.runtimeObject is RTBeatmapObject runtimeObject && runtimeObject.visualObject.renderer && runtimeObject.visualObject is SolidObject solidObject &&
                        modifier.GetBool(2, false))
                    {
                        runtimeObject.visualObject.renderer.material = LegacyResources.objectMaterial;
                        solidObject.material = runtimeObject.visualObject.renderer.material;
                    }
                }, ModifierCompatibility.BeatmapObjectCompatible
            ),
            new ModifierInactive(nameof(ModifierActions.blurOther),
                (modifier, reference, variables) =>
                {
                    if (reference is BeatmapObject beatmapObject && beatmapObject.objectType != BeatmapObject.ObjectType.Empty &&
                        beatmapObject.runtimeObject is RTBeatmapObject runtimeObject && runtimeObject.visualObject.renderer && runtimeObject.visualObject is SolidObject solidObject &&
                        modifier.GetBool(2, false))
                    {
                        runtimeObject.visualObject.renderer.material = LegacyResources.objectMaterial;
                        solidObject.material = runtimeObject.visualObject.renderer.material;
                    }
                }, ModifierCompatibility.BeatmapObjectCompatible
            ),
            new ModifierInactive(nameof(ModifierActions.blurVariableOther),
                (modifier, reference, variables) =>
                {
                    if (reference is BeatmapObject beatmapObject && beatmapObject.objectType != BeatmapObject.ObjectType.Empty &&
                        beatmapObject.runtimeObject is RTBeatmapObject runtimeObject && runtimeObject.visualObject.renderer && runtimeObject.visualObject is SolidObject solidObject &&
                        modifier.GetBool(2, false))
                    {
                        runtimeObject.visualObject.renderer.material = LegacyResources.objectMaterial;
                        solidObject.material = runtimeObject.visualObject.renderer.material;
                    }
                }, ModifierCompatibility.BeatmapObjectCompatible
            ),

            #endregion

            #region Variables
            
            new ModifierInactive(nameof(ModifierActions.storeLocalVariables),
                (modifier, reference, variables) =>
                {
                    modifier.Result = null;
                }
            ),

            #endregion

            #region Enable

            new ModifierInactive(nameof(ModifierActions.enableObject),
                (modifier, reference, variables) =>
                {
                    if (!modifier.GetBool(1, false))
                        return;

                    if (reference is not IPrefabable prefabable)
                        return;

                    if (prefabable.GetRuntimeObject() is ICustomActivatable activatable)
                        activatable.SetCustomActive(false);
                }, ModifierCompatibility.BeatmapObjectCompatible.WithPAPlayer()
            ),
            new ModifierInactive(nameof(ModifierActions.enableObjectOther),
                (modifier, reference, variables) =>
                {
                    if (!modifier.GetBool(1, false))
                        return;

                    if (reference is not IPrefabable prefabable)
                        return;

                    var prefabables = GameData.Current.FindPrefabablesWithTag(modifier, prefabable, modifier.GetValue(0));

                    if (prefabables.IsEmpty())
                        return;

                    foreach (var other in prefabables)
                    {
                        if (other.GetRuntimeObject() is ICustomActivatable activatable)
                            activatable.SetCustomActive(false);
                    }

                    modifier.Result = null;
                }, ModifierCompatibility.BeatmapObjectCompatible
            ),
            new ModifierInactive(nameof(ModifierActions.enableObjectTree),
                (modifier, reference, variables) =>
                {
                    if (modifier.GetValue(0) == "0")
                        modifier.SetValue(0, "False");

                    if (!modifier.GetBool(1, false))
                    {
                        modifier.Result = null;
                        return;
                    }

                    if (reference is not BeatmapObject beatmapObject)
                        return;

                    if (modifier.Result == null)
                        return;

                    var list = (List<BeatmapObject>)modifier.Result;

                    for (int i = 0; i < list.Count; i++)
                        list[i].runtimeObject?.SetCustomActive(false);

                    modifier.Result = null;
                }, ModifierCompatibility.BeatmapObjectCompatible
            ),
            new ModifierInactive(nameof(ModifierActions.enableObjectTreeOther),
                (modifier, reference, variables) =>
                {
                    if (!modifier.GetBool(2, false))
                    {
                        modifier.Result = null;
                        return;
                    }

                    if (reference is not IPrefabable prefabable)
                        return;

                    if (modifier.Result == null)
                    {
                        var beatmapObjects = GameData.Current.FindObjectsWithTag(modifier, prefabable, modifier.GetValue(1));

                        var resultList = new List<BeatmapObject>();
                        foreach (var bm in beatmapObjects)
                        {
                            var beatmapObject = modifier.GetBool(0, true) ? bm : bm.GetParentChain().Last();
                            resultList.AddRange(beatmapObject.GetChildTree());
                        }

                        modifier.Result = resultList;
                    }

                    var list = (List<BeatmapObject>)modifier.Result;

                    for (int i = 0; i < list.Count; i++)
                        list[i].runtimeObject?.SetCustomActive(false);

                    modifier.Result = null;
                }, ModifierCompatibility.BeatmapObjectCompatible
            ),

            new ModifierInactive(nameof(ModifierActions.disableObject),
                (modifier, reference, variables) =>
                {
                    if (!modifier.GetBool(1, false))
                        return;

                    if (reference is not IPrefabable prefabable)
                        return;

                    if (prefabable.GetRuntimeObject() is ICustomActivatable activatable)
                        activatable.SetCustomActive(true);
                }, ModifierCompatibility.BeatmapObjectCompatible
            ),
            new ModifierInactive(nameof(ModifierActions.disableObjectOther),
                (modifier, reference, variables) =>
                {
                    if (!modifier.GetBool(1, false))
                        return;

                    if (reference is not IPrefabable prefabable)
                        return;

                    var prefabables = GameData.Current.FindPrefabablesWithTag(modifier, prefabable, modifier.GetValue(0));

                    if (prefabables.IsEmpty())
                        return;

                    foreach (var other in prefabables)
                    {
                        if (other.GetRuntimeObject() is ICustomActivatable activatable)
                            activatable.SetCustomActive(true);
                    }

                    modifier.Result = null;
                }, ModifierCompatibility.BeatmapObjectCompatible
            ),
            new ModifierInactive(nameof(ModifierActions.disableObjectTree),
                (modifier, reference, variables) =>
                {
                    if (modifier.GetValue(0) == "0")
                        modifier.SetValue(0, "False");

                    if (!modifier.GetBool(1, false))
                    {
                        modifier.Result = null;
                        return;
                    }

                    if (reference is not BeatmapObject beatmapObject)
                        return;

                    if (modifier.Result == null)
                        return;

                    var list = (List<BeatmapObject>)modifier.Result;

                    for (int i = 0; i < list.Count; i++)
                        list[i].runtimeObject?.SetCustomActive(true);

                    modifier.Result = null;
                }, ModifierCompatibility.BeatmapObjectCompatible
            ),
            new ModifierInactive(nameof(ModifierActions.disableObjectTreeOther),
                (modifier, reference, variables) =>
                {
                    if (!modifier.GetBool(2, false))
                    {
                        modifier.Result = null;
                        return;
                    }

                    if (reference is not IPrefabable prefabable)
                        return;

                    if (modifier.Result == null)
                    {
                        var beatmapObjects = GameData.Current.FindObjectsWithTag(modifier, prefabable, modifier.GetValue(1));

                        var resultList = new List<BeatmapObject>();
                        foreach (var bm in beatmapObjects)
                        {
                            var beatmapObject = modifier.GetBool(0, true) ? bm : bm.GetParentChain().Last();
                            resultList.AddRange(beatmapObject.GetChildTree());
                        }

                        modifier.Result = resultList;
                    }

                    var list = (List<BeatmapObject>)modifier.Result;

                    for (int i = 0; i < list.Count; i++)
                        list[i].runtimeObject?.SetCustomActive(true);

                    modifier.Result = null;
                }, ModifierCompatibility.BeatmapObjectCompatible
            ),

            #endregion

            #region Reactive

            new ModifierInactive(nameof(ModifierActions.reactivePosChain),
                (modifier, reference, variables) =>
                {
                    if (reference is IReactive reactive)
                        reactive.ReactivePositionOffset = Vector3.zero;
                }
            ),
            new ModifierInactive(nameof(ModifierActions.reactiveScaChain),
                (modifier, reference, variables) =>
                {
                    if (reference is IReactive reactive)
                        reactive.ReactiveScaleOffset = Vector3.zero;
                }
            ),
            new ModifierInactive(nameof(ModifierActions.reactiveRotChain),
                (modifier, reference, variables) =>
                {
                    if (reference is IReactive reactive)
                        reactive.ReactiveRotationOffset = 0f;
                }
            ),

            #endregion

            #region Color

            new ModifierInactive(nameof(ModifierActions.animateColorKF),
                (modifier, reference, variables) =>
                {
                    modifier.Result = null;
                }, ModifierCompatibility.BeatmapObjectCompatible.WithBackgroundObject()
            ),
            new ModifierInactive(nameof(ModifierActions.animateColorKFHex),
                (modifier, reference, variables) =>
                {
                    modifier.Result = null;
                }, ModifierCompatibility.BeatmapObjectCompatible.WithBackgroundObject()
            ),

            #endregion

            #region Shape

            new ModifierInactive(nameof(ModifierActions.setText),
                (modifier, reference, variables) =>
                {
                    if (modifier.constant && reference is BeatmapObject beatmapObject && beatmapObject.ShapeType == ShapeType.Text && beatmapObject.runtimeObject && beatmapObject.runtimeObject.visualObject &&
                        beatmapObject.runtimeObject.visualObject is TextObject textObject)
                        textObject.text = beatmapObject.text;
                }, ModifierCompatibility.BeatmapObjectCompatible
            ),
            new ModifierInactive(nameof(ModifierActions.setTextOther),
                (modifier, reference, variables) =>
                {
                    if (reference is not IPrefabable prefabable)
                        return;

                    var list = GameData.Current.FindObjectsWithTag(modifier, prefabable, modifier.GetValue(1));

                    if (modifier.constant && !list.IsEmpty())
                        foreach (var bm in list)
                            if (bm.ShapeType == ShapeType.Text && bm.runtimeObject && bm.runtimeObject.visualObject &&
                                bm.runtimeObject.visualObject is TextObject textObject)
                                textObject.text = bm.text;
                }, ModifierCompatibility.BeatmapObjectCompatible
            ),
            new ModifierInactive(nameof(ModifierActions.textSequence),
                (modifier, reference, variables) =>
                {
                    modifier.setTimer = false;
                }, ModifierCompatibility.BeatmapObjectCompatible
            ),

            #endregion

            #region Animation

            new ModifierInactive(nameof(ModifierActions.animateSignal),
                (modifier, reference, variables) =>
                {
                    if (modifier.constant || !modifier.GetBool(!modifier.Name.Contains("Other") ? 9 : 10, true))
                        return;

                    if (reference is not IPrefabable prefabable)
                        return;

                    int groupIndex = !modifier.Name.Contains("Other") ? 7 : 8;
                    var modifyables = GameData.Current.FindModifyables(modifier, prefabable, modifier.GetValue(groupIndex));

                    foreach (var modifyable in modifyables)
                    {
                        if (!modifyable.Modifiers.IsEmpty() && modifyable.Modifiers.TryFind(x => x.Name == "requireSignal" && x.type == Modifier.Type.Trigger, out Modifier m))
                            m.Result = null;
                    }
                }
            ),
            new ModifierInactive(nameof(ModifierActions.animateSignalOther),
                (modifier, reference, variables) =>
                {
                    if (modifier.constant || !modifier.GetBool(!modifier.Name.Contains("Other") ? 9 : 10, true))
                        return;

                    if (reference is not IPrefabable prefabable)
                        return;

                    int groupIndex = !modifier.Name.Contains("Other") ? 7 : 8;
                    var modifyables = GameData.Current.FindModifyables(modifier, prefabable, modifier.GetValue(groupIndex));

                    foreach (var modifyable in modifyables)
                    {
                        if (!modifyable.Modifiers.IsEmpty() && modifyable.Modifiers.TryFind(x => x.Name == "requireSignal" && x.type == Modifier.Type.Trigger, out Modifier m))
                            m.Result = null;
                    }
                }
            ),
            new ModifierInactive(nameof(ModifierActions.applyAnimation),
                (modifier, reference, variables) =>
                {
                    modifier.Result = null;
                }, ModifierCompatibility.BeatmapObjectCompatible
            ),
            new ModifierInactive(nameof(ModifierActions.applyAnimationFrom),
                (modifier, reference, variables) =>
                {
                    modifier.Result = null;
                }, ModifierCompatibility.BeatmapObjectCompatible
            ),
            new ModifierInactive(nameof(ModifierActions.applyAnimationTo),
                (modifier, reference, variables) =>
                {
                    modifier.Result = null;
                }, ModifierCompatibility.BeatmapObjectCompatible
            ),
            new ModifierInactive(nameof(ModifierActions.copyAxis),
                (modifier, reference, variables) =>
                {
                    modifier.Result = null;
                }
            ),
            new ModifierInactive(nameof(ModifierActions.copyAxisMath),
                (modifier, reference, variables) =>
                {
                    modifier.Result = null;
                }
            ),
            new ModifierInactive(nameof(ModifierActions.copyAxisGroup),
                (modifier, reference, variables) =>
                {
                    modifier.Result = null;
                }
            ),
            new ModifierInactive(nameof(ModifierActions.gravity),
                (modifier, reference, variables) =>
                {
                    modifier.Result = null;
                }
            ),
            new ModifierInactive(nameof(ModifierActions.gravityOther),
                (modifier, reference, variables) =>
                {
                    modifier.Result = null;
                }
            ),

            #endregion

            #region Prefab

            new ModifierInactive(nameof(ModifierActions.spawnPrefab),
                (modifier, reference, variables) =>
                {
                    // value 9 is permanent

                    if (modifier.Result is PrefabObject prefabObject && !modifier.GetBool(9, false))
                    {
                        RTLevelBase runtimeLevel = reference is PrefabObject p && p.runtimeObject ? p.runtimeObject : reference.GetParentRuntime();
                        runtimeLevel?.UpdatePrefab(prefabObject, false);

                        GameData.Current.prefabObjects.RemoveAll(x => x.fromModifier && x.id == prefabObject.id);

                        modifier.Result = null;
                    }
                }
            ),
            new ModifierInactive(nameof(ModifierActions.spawnPrefabOffset),
                (modifier, reference, variables) =>
                {
                    // value 9 is permanent

                    if (modifier.Result is PrefabObject prefabObject && !modifier.GetBool(9, false))
                    {
                        RTLevelBase runtimeLevel = reference is PrefabObject p && p.runtimeObject ? p.runtimeObject : reference.GetParentRuntime();
                        runtimeLevel?.UpdatePrefab(prefabObject, false);

                        GameData.Current.prefabObjects.RemoveAll(x => x.fromModifier && x.id == prefabObject.id);

                        modifier.Result = null;
                    }
                }
            ),
            new ModifierInactive(nameof(ModifierActions.spawnPrefabOffsetOther),
                (modifier, reference, variables) =>
                {
                    // value 9 is permanent

                    if (modifier.Result is PrefabObject prefabObject && !modifier.GetBool(9, false))
                    {
                        RTLevelBase runtimeLevel = reference is PrefabObject p && p.runtimeObject ? p.runtimeObject : reference.GetParentRuntime();
                        runtimeLevel?.UpdatePrefab(prefabObject, false);

                        GameData.Current.prefabObjects.RemoveAll(x => x.fromModifier && x.id == prefabObject.id);

                        modifier.Result = null;
                    }
                }
            ),
            new ModifierInactive(nameof(ModifierActions.spawnPrefabCopy),
                (modifier, reference, variables) =>
                {
                    // value 5 is permanent

                    if (modifier.Result is PrefabObject prefabObject && !modifier.GetBool(5, false))
                    {
                        RTLevelBase runtimeLevel = reference is PrefabObject p && p.runtimeObject ? p.runtimeObject : reference.GetParentRuntime();
                        runtimeLevel?.UpdatePrefab(prefabObject, false);

                        GameData.Current.prefabObjects.RemoveAll(x => x.fromModifier && x.id == prefabObject.id);

                        modifier.Result = null;
                    }
                }
            ),

            #endregion

            #region Player Only

            new ModifierInactive(nameof(ModifierActions.setCustomObjectActive),
                (modifier, reference, variables) =>
                {
                    if (modifier.GetBool(2, true) && reference is PAPlayer player && player.RuntimePlayer.customObjects.TryFind(x => x.id == modifier.GetValue(1), out RTPlayer.RTCustomPlayerObject customObject))
                        customObject.active = !modifier.GetBool(0, false);
                }, ModifierCompatibility.PAPlayerCompatible),

            #endregion

            #region Misc

            new ModifierInactive(nameof(ModifierActions.signalModifier),
                (modifier, reference, variables) =>
                {
                    if (reference is not IPrefabable prefabable)
                        return;

                    var modifyables = GameData.Current.FindModifyables(modifier, prefabable, modifier.GetValue(1));

                    foreach (var modifyable in modifyables)
                    {
                        if (!modifyable.Modifiers.IsEmpty() && modifyable.Modifiers.TryFind(x => x.Name == "requireSignal" && x.type == Modifier.Type.Trigger, out Modifier m))
                            m.Result = null;
                    }
                }
            ),

            #endregion

            #endregion

            #region Triggers

            #region Controls

            new ModifierInactive(nameof(ModifierTriggers.mouseOverSignalModifier),
                (modifier, reference, variables) =>
                {
                    if (reference is not IPrefabable prefabable)
                        return;

                    var modifyables = GameData.Current.FindModifyables(modifier, prefabable, modifier.GetValue(1));

                    foreach (var modifyable in modifyables)
                    {
                        if (!modifyable.Modifiers.IsEmpty() && modifyable.Modifiers.TryFind(x => x.Name == "requireSignal" && x.type == Modifier.Type.Trigger, out Modifier m))
                            m.Result = null;
                    }
                }, ModifierCompatibility.BeatmapObjectCompatible
            ),

            #endregion

            #region Random
            
            new ModifierInactive(nameof(ModifierTriggers.randomEquals),
                (modifier, reference, variables) =>
                {
                    modifier.Result = null;
                }
            ),
            new ModifierInactive(nameof(ModifierTriggers.randomLesser),
                (modifier, reference, variables) =>
                {
                    modifier.Result = null;
                }
            ),
            new ModifierInactive(nameof(ModifierTriggers.randomGreater),
                (modifier, reference, variables) =>
                {
                    modifier.Result = null;
                }
            ),

            #endregion

            #region Misc
            
            new ModifierInactive(nameof(ModifierTriggers.await),
                (modifier, reference, variables) =>
                {
                    modifier.Result = default;
                }),
            new ModifierInactive(nameof(ModifierTriggers.awaitCounter),
                (modifier, reference, variables) =>
                {
                    modifier.Result = default;
                }),
            new ModifierInactive(nameof(ModifierTriggers.objectSpawned),
                (modifier, reference, variables) =>
                {
                    modifier.Result = null;
                }, ModifierCompatibility.BeatmapObjectCompatible
            ),

            #endregion

            #endregion
        };

        public static bool TryGetTrigger(Modifier modifier, IModifierReference reference, out ModifierTrigger trigger)
        {
            var name = modifier.Name;
            ModifierTrigger result = null;
            var check = modifier.type == Modifier.Type.Trigger && triggers.TryFind(x => x.name == name, out result) && result.compatibility.CompareType(reference.ReferenceType);
            trigger = result;
            return check;
        }

        public static bool TryGetAction(Modifier modifier, IModifierReference reference, out ModifierAction action)
        {
            var name = modifier.Name;
            ModifierAction result = null;
            var check = modifier.type == Modifier.Type.Action && actions.TryFind(x => x.name == name, out result) && result.compatibility.CompareType(reference.ReferenceType);
            action = result;
            return check;
        }
        
        public static bool TryGetInactive(Modifier modifier, IModifierReference reference, out ModifierInactive inactive)
        {
            var name = modifier.Name;
            var check = inactives.TryFind(x => x.name == name, out ModifierInactive result) && result.compatibility.CompareType(reference.ReferenceType);
            inactive = result;
            return check;
        }

        #region GameData

        public static int GetLevelTriggerType(string key) => key switch
        {
            "time" => 0,
            "timeInRange" => 0,
            nameof(ModifierTriggers.onPlayerHit) => 1,
            nameof(ModifierTriggers.onPlayerDeath) => 2,
            nameof(ModifierTriggers.onLevelStart) => 3,
            nameof(ModifierTriggers.onLevelRestart) => 4,
            nameof(ModifierTriggers.onLevelRewind) => 5,
            _ => -1,
        };
        
        public static int GetLevelActionType(string key) => key switch
        {
            "vnInk" => 0,
            "vnTimeline" => 1,
            "playerBubble" => 2,
            nameof(ModifierActions.playerMoveAll) => 3,
            nameof(ModifierActions.playerLockBoostAll) => 4,
            nameof(ModifierActions.playerLockXAll) => 5,
            nameof(ModifierActions.playerLockYAll) => 6,
            "bgSpin" => 7,
            "bgMove" => 8,
            nameof(ModifierActions.playerBoostAll) => 9,
            nameof(ModifierActions.setMusicTime) => 10,
            nameof(ModifierActions.setPitch) => 11,
            _ => -1,
        };

        public static string GetLevelTriggerName(int type) => type switch
        {
            0 => "timeInRange",
            1 => nameof(ModifierTriggers.onPlayerHit),
            2 => nameof(ModifierTriggers.onPlayerDeath),
            3 => nameof(ModifierTriggers.onLevelStart),
            4 => nameof(ModifierTriggers.onLevelRestart),
            5 => nameof(ModifierTriggers.onLevelRewind),
            _ => string.Empty,
        };

        public static string GetLevelActionName(int type) => type switch
        {
            0 => "vnInk",
            1 => "vnTimeline",
            2 => "playerBubble",
            3 => nameof(ModifierActions.playerMoveAll),
            4 => nameof(ModifierActions.playerLockBoostAll),
            5 => nameof(ModifierActions.playerLockXAll),
            6 => nameof(ModifierActions.playerLockYAll),
            7 => "bgSpin",
            8 => "bgMove",
            9 => nameof(ModifierActions.playerBoostAll),
            10 => nameof(ModifierActions.setMusicTime),
            11 => nameof(ModifierActions.setPitch),
            _ => string.Empty,
        };

        #endregion

        #endregion

        #region Internal Functions

        public static bool IsGroupModifier(string name) =>
            name == nameof(ModifierActions.setParent) ||
            name == nameof(ModifierTriggers.objectCollide) ||
            name == nameof(ModifierTriggers.axisEquals) ||
            name == nameof(ModifierTriggers.axisGreater) ||
            name == nameof(ModifierTriggers.axisGreaterEquals) ||
            name == nameof(ModifierTriggers.axisLesser) ||
            name == nameof(ModifierTriggers.axisLesserEquals) ||
            name == nameof(ModifierActions.getAxis) ||
            name == nameof(ModifierActions.activateModifier) ||
            name == nameof(ModifierActions.legacyTail) ||
            name.ToLower().Contains("signal") ||
            name.Contains("Other") ||
            name.Contains("copy") && name != nameof(ModifierActions.copyPlayerAxis) ||
            name.Contains("applyAnimation");

        public static void SetVariables(Dictionary<string, string> variables, Dictionary<string, float> numberVariables)
        {
            if (variables == null)
                return;

            foreach (var variable in variables)
            {
                if (float.TryParse(variable.Value, out float num))
                    numberVariables[variable.Key] = num;
            }
        }

        public static void GetSoundPath(string id, string path, bool fromSoundLibrary = false, float pitch = 1f, float volume = 1f, bool loop = false, float panStereo = 0f)
        {
            string fullPath = !fromSoundLibrary ? RTFile.CombinePaths(RTFile.BasePath, path) : RTFile.CombinePaths(RTFile.ApplicationDirectory, ModifiersManager.SOUNDLIBRARY_PATH, path);

            var audioDotFormats = RTFile.AudioDotFormats;
            for (int i = 0; i < audioDotFormats.Length; i++)
            {
                var audioDotFormat = audioDotFormats[i];
                if (!path.Contains(audioDotFormat) && RTFile.FileExists(fullPath + audioDotFormat))
                    fullPath += audioDotFormat;
            }

            if (!RTFile.FileExists(fullPath))
                return;

            if (!fullPath.EndsWith(FileFormat.MP3.Dot()))
                CoroutineHelper.StartCoroutine(LoadMusicFileRaw(fullPath, audioClip => PlaySound(id, audioClip, pitch, volume, loop, panStereo)));
            else
                PlaySound(id, LSAudio.CreateAudioClipUsingMP3File(fullPath), pitch, volume, loop, panStereo);
        }

        public static void DownloadSoundAndPlay(string id, string path, float pitch = 1f, float volume = 1f, bool loop = false, float panStereo = 0f)
        {
            try
            {
                var audioType = RTFile.GetAudioType(path);

                if (audioType != AudioType.UNKNOWN)
                    CoroutineHelper.StartCoroutine(AlephNetwork.DownloadAudioClip(path, audioType, audioClip => PlaySound(id, audioClip, pitch, volume, loop, panStereo), onError => CoreHelper.Log($"Error! Could not download audioclip.\n{onError}")));
            }
            catch
            {

            }
        }

        public static void PlaySound(string id, AudioClip clip, float pitch, float volume, bool loop, float panStereo = 0f)
        {
            var audioSource = SoundManager.inst.PlaySound(clip, volume, pitch * AudioManager.inst.CurrentAudioSource.pitch, loop, panStereo);
            if (loop && !ModifiersManager.audioSources.ContainsKey(id))
                ModifiersManager.audioSources.Add(id, audioSource);
        }

        public static IEnumerator LoadMusicFileRaw(string path, Action<AudioClip> callback)
        {
            if (!RTFile.FileExists(path))
            {
                CoreHelper.Log($"Could not load Music file [{path}]");
                yield break;
            }

            var www = new WWW("file://" + path);
            while (!www.isDone)
                yield return null;

            var beatmapAudio = www.GetAudioClip(false, false);
            while (beatmapAudio.loadState != AudioDataLoadState.Loaded)
                yield return null;
            callback?.Invoke(beatmapAudio);
            beatmapAudio = null;
            www = null;

            yield break;
        }

        public static PrefabObject AddPrefabObjectToLevel(Prefab prefab, float startTime, Vector2 pos, Vector2 sca, float rot, int repeatCount, float repeatOffsetTime, float speed)
        {
            var prefabObject = new PrefabObject();
            prefabObject.id = LSText.randomString(16);
            prefabObject.prefabID = prefab.id;

            prefabObject.StartTime = startTime;

            if (prefab.defaultInstanceData)
                prefabObject.PasteInstanceData(prefab.defaultInstanceData);

            prefabObject.events[0].values[0] = pos.x;
            prefabObject.events[0].values[1] = pos.y;
            prefabObject.events[1].values[0] = sca.x;
            prefabObject.events[1].values[1] = sca.y;
            prefabObject.events[2].values[0] = rot;

            prefabObject.RepeatCount = repeatCount;
            prefabObject.RepeatOffsetTime = repeatOffsetTime;
            prefabObject.Speed = speed;

            prefabObject.fromModifier = true;

            return prefabObject;
        }

        public static void SaveProgress(string path, string chapter, string level, float data)
        {
            if (path.Contains("\\") || path.Contains("/") || path.Contains(".."))
                return;

            var profile = RTFile.CombinePaths(RTFile.ApplicationDirectory, "profile");
            RTFile.CreateDirectory(profile);

            var file = RTFile.CombinePaths(profile, $"{path}{FileFormat.SES.Dot()}");
            var jn = JSON.Parse(RTFile.FileExists(file) ? RTFile.ReadFromFile(file) : "{}");

            jn[chapter][level]["float"] = data.ToString();

            RTFile.WriteToFile(file, jn.ToString(3));
        }

        public static void SaveProgress(string path, string chapter, string level, string data)
        {
            if (path.Contains("\\") || path.Contains("/") || path.Contains(".."))
                return;

            var profile = RTFile.CombinePaths(RTFile.ApplicationDirectory, "profile");
            RTFile.CreateDirectory(profile);

            var file = RTFile.CombinePaths(profile, $"{path}{FileFormat.SES.Dot()}");
            var jn = JSON.Parse(RTFile.FileExists(file) ? RTFile.ReadFromFile(file) : "{}");

            jn[chapter][level]["string"] = data.ToString();

            RTFile.WriteToFile(file, jn.ToString(3));
        }

        public static IEnumerator ActivateModifier(BeatmapObject beatmapObject, float delay)
        {
            if (delay != 0.0)
                yield return CoroutineHelper.Seconds(delay);

            if (beatmapObject.modifiers.TryFind(x => x.Name == "requireSignal" && x.type == Modifier.Type.Trigger, out Modifier modifier))
                modifier.Result = "death hd";
            yield break;
        }

        public static void ApplyAnimationTo(
            BeatmapObject applyTo, BeatmapObject takeFrom,
            bool useVisual, float time, float currentTime,
            bool animatePos, bool animateSca, bool animateRot,
            float delayPos, float delaySca, float delayRot)
        {
            if (!useVisual && takeFrom.cachedSequences)
            {
                // Animate position
                if (animatePos)
                    applyTo.positionOffset = takeFrom.cachedSequences.PositionSequence.GetValue(currentTime - time - delayPos);

                // Animate scale
                if (animateSca)
                {
                    var scaleSequence = takeFrom.cachedSequences.ScaleSequence.GetValue(currentTime - time - delaySca);
                    applyTo.scaleOffset = new Vector3(scaleSequence.x - 1f, scaleSequence.y - 1f, 0f);
                }

                // Animate rotation
                if (animateRot)
                    applyTo.rotationOffset = new Vector3(0f, 0f, takeFrom.cachedSequences.RotationSequence.GetValue(currentTime - time - delayRot));
            }
            else if (useVisual && takeFrom.runtimeObject is RTBeatmapObject levelObject && levelObject.visualObject != null && levelObject.visualObject.gameObject)
            {
                var transform = levelObject.visualObject.gameObject.transform;

                // Animate position
                if (animatePos)
                    applyTo.positionOffset = transform.position;

                // Animate scale
                if (animateSca)
                    applyTo.scaleOffset = transform.lossyScale;

                // Animate rotation
                if (animateRot)
                    applyTo.rotationOffset = transform.rotation.eulerAngles;
            }
            else if (useVisual)
            {
                // Animate position
                if (animatePos)
                    applyTo.positionOffset = takeFrom.InterpolateChainPosition(currentTime - time - delayPos);

                // Animate scale
                if (animateSca)
                {
                    var scaleSequence = takeFrom.InterpolateChainScale(currentTime - time - delaySca);
                    applyTo.scaleOffset = new Vector3(scaleSequence.x - 1f, scaleSequence.y - 1f, 0f);
                }

                // Animate rotation
                if (animateRot)
                    applyTo.rotationOffset = new Vector3(0f, 0f, takeFrom.InterpolateChainRotation(currentTime - time - delayRot));
            }
        }

        public static float GetAnimation(IPrefabable prefabable, BeatmapObject reference, int fromType, int fromAxis, float min, float max, float offset, float multiply, float delay, float loop, bool visual)
        {
            var time = GetTime(reference);
            var t = time - reference.StartTime - delay;

            if (!visual && reference.cachedSequences)
                return fromType switch
                {
                    0 => Mathf.Clamp((reference.cachedSequences.PositionSequence.GetValue(t).At(fromAxis) - offset) * multiply % loop, min, max),
                    1 => Mathf.Clamp((reference.cachedSequences.ScaleSequence.GetValue(t).At(fromAxis) - offset) * multiply % loop, min, max),
                    2 => Mathf.Clamp((reference.cachedSequences.RotationSequence.GetValue(t) - offset) * multiply % loop, min, max),
                    _ => 0f,
                };
            else if (visual && reference.runtimeObject is RTBeatmapObject runtimeObject && runtimeObject.visualObject && runtimeObject.visualObject.gameObject)
                return Mathf.Clamp((runtimeObject.visualObject.gameObject.transform.GetVector(fromType).At(fromAxis) - offset) * multiply % loop, min, max);

            return 0f;
        }

        public static float GetTime(BeatmapObject reference)
        {
            if (reference.FromPrefab)
            {
                var prefabObject = reference.GetPrefabObject();
                if (prefabObject && prefabObject.runtimeObject)
                    return prefabObject.runtimeObject.CurrentTime;
            }
            return reference.GetParentRuntime().CurrentTime;
        }

        public static void CopyColor(RTBeatmapObject applyTo, RTBeatmapObject takeFrom, bool applyColor1, bool applyColor2)
        {
            var applyToSolidObject = applyTo.visualObject as SolidObject;
            var takeFromSolidObject = takeFrom.visualObject as SolidObject;

            if (applyTo.visualObject.isGradient && applyToSolidObject && takeFrom.visualObject.isGradient && takeFromSolidObject) // both are gradients
            {
                var colors = takeFromSolidObject.GetColors();
                applyToSolidObject.SetColor(colors.startColor, colors.endColor);
            }

            if (applyTo.visualObject.isGradient && applyToSolidObject && !takeFrom.visualObject.isGradient) // only main object is a gradient
            {
                var color = takeFrom.visualObject.GetPrimaryColor();
                var colors = applyToSolidObject.GetColors();
                applyToSolidObject.SetColor(applyColor1 ? color : colors.startColor, applyColor2 ? color : colors.endColor);
            }

            if (!applyTo.visualObject.isGradient && takeFrom.visualObject.isGradient && takeFromSolidObject) // only copying object is a gradient
            {
                var colors = takeFromSolidObject.GetColors();
                applyTo.visualObject.SetColor(applyColor1 ? colors.startColor : applyColor2 ? colors.endColor : takeFromSolidObject.GetPrimaryColor());
            }

            if (!applyTo.visualObject.isGradient && !takeFrom.visualObject.isGradient) // neither are gradients
                applyTo.visualObject.SetColor(takeFrom.visualObject.GetPrimaryColor());
        }

        public static bool GetLevelRank(Level level, out int levelRankIndex)
        {
            var active = level && level.saveData;
            levelRankIndex = active ? LevelManager.GetLevelRank(level) : 0;
            return active;
        }

        public static string GetSaveFile(string file) => RTFile.CombinePaths(RTFile.ApplicationDirectory, "profile", file + FileFormat.SES.Dot());

        public static void SetParent(IParentable child, BeatmapObject parent) => SetParent(child, parent.id);

        public static void SetParent(IParentable child, string parent)
        {
            // don't update parent if the parent is already the same
            if (child.Parent == parent)
                return;

            child.CustomParent = parent;
            child.UpdateParentChain();

            if (ObjectEditor.inst && ObjectEditor.inst.Dialog && ObjectEditor.inst.Dialog.IsCurrent && EditorTimeline.inst.CurrentSelection.isBeatmapObject)
                ObjectEditor.inst.RenderParent(EditorTimeline.inst.CurrentSelection.GetData<BeatmapObject>());
            if (RTPrefabEditor.inst && RTPrefabEditor.inst.PrefabObjectEditor && RTPrefabEditor.inst.PrefabObjectEditor.IsCurrent && EditorTimeline.inst.CurrentSelection.isPrefabObject)
                RTPrefabEditor.inst.RenderPrefabObjectParent(EditorTimeline.inst.CurrentSelection.GetData<PrefabObject>());
        }

        public static void SetObjectActive(IPrefabable prefabable, bool active)
        {
            if (prefabable != null && prefabable.GetRuntimeObject() is ICustomActivatable customActivatable)
                customActivatable.SetCustomActive(active);
        }

        #endregion
    }

    #region Caches

    /// <summary>
    /// Cache for <see cref="ModifierActions.translateShape(Modifier, IModifierReference, Dictionary{string, string})"/>.
    /// </summary>
    public class TranslateShapeCache
    {
        /// <summary>
        /// Translates the mesh.
        /// </summary>
        /// <param name="pos">Position to translate to.</param>
        /// <param name="sca">Scale to translate to.</param>
        /// <param name="rot">Rotation to tranlsate to.</param>
        public void Translate(Vector2 pos, Vector2 sca, float rot, bool forceTranslate = false)
        {
            // don't translate if the cached values are the same as the parameters.
            if (Is(pos, sca, rot) && !forceTranslate)
            {
                Cache(pos, sca, rot);
                return;
            }

            Cache(pos, sca, rot);

            if (meshFilter && vertices != null)
                meshFilter.mesh.vertices = vertices.Select(x => RTMath.Move(RTMath.Rotate(RTMath.Scale(x, sca), rot), pos)).ToArray();
            if (collider2D && points != null)
                collider2D.points = points.Select(x => (Vector2)RTMath.Move(RTMath.Rotate(RTMath.Scale(x, sca), rot), pos)).ToArray();
        }

        #region Cache

        void Cache(Vector2 pos, Vector2 sca, float rot)
        {
            this.pos = pos;
            this.sca = sca;
            this.rot = rot;
        }

        /// <summary>
        /// Cached mesh filter.
        /// </summary>
        public MeshFilter meshFilter;
        /// <summary>
        /// Cached polygon collider.
        /// </summary>
        public PolygonCollider2D collider2D;
        /// <summary>
        /// Original vertices to translate.
        /// </summary>
        public Vector3[] vertices;
        /// <summary>
        /// Original collider points.
        /// </summary>
        public Vector2[] points;

        /// <summary>
        /// Cached position.
        /// </summary>
        public Vector2 pos;
        /// <summary>
        /// Cached scale.
        /// </summary>
        public Vector2 sca;
        /// <summary>
        /// Cached rotation.
        /// </summary>
        public float rot;

        #endregion

        #region Operators

        public override int GetHashCode() => CoreHelper.CombineHashCodes(pos.x, pos.y, sca.x, sca.y, rot);

        public override bool Equals(object obj) => obj is TranslateShapeCache shapeCache && Is(shapeCache.pos, shapeCache.sca, shapeCache.rot);

        /// <summary>
        /// Checks if the cached values are equal to the parameters.
        /// </summary>
        /// <param name="pos">Position.</param>
        /// <param name="sca">Scale.</param>
        /// <param name="rot">Rotation.</param>
        /// <returns>Returns true if the cached values are approximately the same as the passed parameters, otherwise returns false.</returns>
        public bool Is(Vector2 pos, Vector2 sca, float rot) =>
            Mathf.Approximately(pos.x, this.pos.x) && Mathf.Approximately(pos.y, this.pos.y) &&
            Mathf.Approximately(sca.x, this.sca.x) && Mathf.Approximately(sca.y, this.sca.y) &&
            Mathf.Approximately(rot, this.rot);

        #endregion
    }

    /// <summary>
    /// Cache for <see cref="ModifierActions.enableObjectGroup(Modifier, IModifierReference, Dictionary{string, string})"/>.
    /// </summary>
    public class EnableObjectGroupCache
    {
        public EnableObjectGroupCache() { }

        int currentState = -1;

        /// <summary>
        /// List of object groups.
        /// </summary>
        public List<IPrefabable>[] objects;
        /// <summary>
        /// List of all objects in the cache.
        /// </summary>
        public List<IPrefabable> allObjects = new List<IPrefabable>();

        readonly HashSet<IPrefabable> activeObjects = new HashSet<IPrefabable>();

        /// <summary>
        /// Initializes the cache.
        /// </summary>
        /// <param name="objects">List of object groups.</param>
        /// <param name="enabled">Enabled / disabled state.</param>
        public void Init(List<IPrefabable>[] objects, bool enabled)
        {
            this.objects = objects;
            foreach (var obj in allObjects)
                ModifiersHelper.SetObjectActive(obj, !enabled);
        }

        /// <summary>
        /// Recalculates the currently active objects.
        /// </summary>
        /// <param name="enabled">Enabled / disabled state.</param>
        /// <param name="state">Currently active group.</param>
        public void RecalculateActiveObjects(bool enabled, int state)
        {
            int groupIndex = 0;
            foreach (var obj in activeObjects)
            {
                ModifiersHelper.SetObjectActive(obj, !enabled);
                groupIndex++;
            }

            activeObjects.Clear();
            if (state == 0)
            {
                foreach (var obj in allObjects)
                    activeObjects.Add(obj);
                return;
            }

            var current = objects.GetAt(state - 1);
            foreach (var obj in current)
                activeObjects.Add(obj);
        }

        /// <summary>
        /// Sets a group active.
        /// </summary>
        /// <param name="enabled">Enabled / disabled state.</param>
        /// <param name="state">Currently active group.</param>
        public void SetGroupActive(bool enabled, int state)
        {
            if (currentState == state)
                return;

            RecalculateActiveObjects(enabled, state);

            foreach (var obj in activeObjects)
                ModifiersHelper.SetObjectActive(obj, enabled);

            currentState = state;
        }

        /// <summary>
        /// Gets the active state for an object group. If <paramref name="state"/> is 0, then all should have their active state the same as <paramref name="enabled"/>. Otherwise if the state equals the modifier group, set only that object group to <paramref name="enabled"/>.
        /// </summary>
        /// <param name="enabled">If the active group should be enabled / disabled.</param>
        /// <param name="state">The currently active group.</param>
        /// <param name="groupIndex">The group index.</param>
        /// <returns>Returns true if the group is active, otherwise returns false.</returns>
        public bool GetState(bool enabled, int state, int groupIndex)
        {
            // if state is 0, then all should be active / inactive. otherwise if state equals the modifier group, set only that object group active / inactive.
            var innerEnabled = state == 0 || state == groupIndex - 1;
            if (!enabled)
                innerEnabled = !innerEnabled;

            return innerEnabled;
        }
    }

    /// <summary>
    /// Cache for applyAnimation modifiers.
    /// </summary>
    public class ApplyAnimationCache
    {
        public BeatmapObject from;
        public List<BeatmapObject> to = new List<BeatmapObject>();
        public float startTime;
    }

    public class SetParentCache
    {
        public SetParentCache() { }

        public static SetParentCache FromSingle(Modifier modifier, IPrefabable prefabable, string group)
        {
            var cache = new SetParentCache();
            cache.group = group;
            if (!string.IsNullOrEmpty(group) && GameData.Current.TryFindObjectWithTag(modifier, prefabable, group, out BeatmapObject target))
                cache.target = target;
            return cache;
        }

        public static SetParentCache FromGroup(IModifierReference reference, Modifier modifier, IPrefabable prefabable, string group, string otherGroup)
        {
            var cache = new SetParentCache();
            cache.group = group;
            if (!string.IsNullOrEmpty(group) && GameData.Current.TryFindObjectWithTag(modifier, prefabable, group, out BeatmapObject target))
                cache.target = target;
            if (!cache.target && reference is BeatmapObject parent)
                cache.target = parent;
            cache.parentables = GameData.Current.FindParentablesWithTag(modifier, prefabable, otherGroup);
            return cache;
        }

        public string group;
        public BeatmapObject target;
        public List<IParentable> parentables;
    }

    public class MaskCache
    {
        public int width;
        public int height;
        public RenderTexture renderTexture;
    }

    #endregion

    public struct ModifierLoopResult
    {
        public ModifierLoopResult(bool returned, bool result, Modifier.Type previousType, int index)
        {
            this.returned = returned;
            this.result = result;
            this.previousType = previousType;
            this.index = index;
        }

        public bool returned;
        public bool result;
        public Modifier.Type previousType;
        public int index;
    }

    public class ModifierTrigger
    {
        public ModifierTrigger(string name, Func<Modifier, IModifierReference, Dictionary<string, string>, bool> function)
        {
            this.name = name;
            this.function = function;
        }

        public ModifierTrigger(string name, Func<Modifier, IModifierReference, Dictionary<string, string>, bool> function, ModifierCompatibility compatibility) : this(name, function)
        {
            this.compatibility = compatibility;
        }

        public ModifierCompatibility compatibility = ModifierCompatibility.AllCompatible;
        public string name;
        public Func<Modifier, IModifierReference, Dictionary<string, string>, bool> function;

        public static implicit operator ModifierTrigger(Func<Modifier, IModifierReference, Dictionary<string, string>, bool> function) => new ModifierTrigger(string.Empty, function);

        public static implicit operator ModifierTrigger(KeyValuePair<string, Func<Modifier, IModifierReference, Dictionary<string, string>, bool>> keyValuePair) => new ModifierTrigger(keyValuePair.Key, keyValuePair.Value);
    }

    public class ModifierAction
    {
        public ModifierAction(string name, Action<Modifier, IModifierReference, Dictionary<string, string>> function)
        {
            this.name = name;
            this.function = function;
        }
        
        public ModifierAction(string name, Action<Modifier, IModifierReference, Dictionary<string, string>> function, ModifierCompatibility compatibility) : this(name, function)
        {
            this.compatibility = compatibility;
        }

        public ModifierCompatibility compatibility = ModifierCompatibility.AllCompatible;
        public string name;
        public Action<Modifier, IModifierReference, Dictionary<string, string>> function;

        public static implicit operator ModifierAction(Action<Modifier, IModifierReference, Dictionary<string, string>> function) => new ModifierAction(string.Empty, function);

        public static implicit operator ModifierAction(KeyValuePair<string, Action<Modifier, IModifierReference, Dictionary<string, string>>> keyValuePair) => new ModifierAction(keyValuePair.Key, keyValuePair.Value);
    }

    public class ModifierInactive
    {
        public ModifierInactive(string name, Action<Modifier, IModifierReference, Dictionary<string, string>> function)
        {
            this.name = name;
            this.function = function;
        }

        public ModifierInactive(string name, Action<Modifier, IModifierReference, Dictionary<string, string>> function, ModifierCompatibility compatibility) : this(name, function)
        {
            this.compatibility = compatibility;
        }

        public ModifierCompatibility compatibility = ModifierCompatibility.AllCompatible;
        public string name;
        public Action<Modifier, IModifierReference, Dictionary<string, string>> function;

        public static implicit operator ModifierInactive(Action<Modifier, IModifierReference, Dictionary<string, string>> function) => new ModifierInactive(string.Empty, function);

        public static implicit operator ModifierInactive(KeyValuePair<string, Action<Modifier, IModifierReference, Dictionary<string, string>>> keyValuePair) => new ModifierInactive(keyValuePair.Key, keyValuePair.Value);
    }

    public class TriggerTest
    {
        public TriggerTest() { }
        public TriggerTest(bool active, bool elseIf)
        {
            this.active = active;
            this.elseIf = elseIf;
        }

        public bool active;
        public bool elseIf;
    }
}
