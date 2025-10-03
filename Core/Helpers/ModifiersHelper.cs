using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

using UnityEngine;

using LSFunctions;

using SimpleJSON;

using BetterLegacy.Arcade.Managers;
using BetterLegacy.Configs;
using BetterLegacy.Core.Animation;
using BetterLegacy.Core.Animation.Keyframe;
using BetterLegacy.Core.Components;
using BetterLegacy.Core.Components.Player;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Data.Modifiers;
using BetterLegacy.Core.Data.Level;
using BetterLegacy.Core.Data.Player;
using BetterLegacy.Core.Managers;
using BetterLegacy.Core.Runtime;
using BetterLegacy.Core.Runtime.Objects;
using BetterLegacy.Core.Runtime.Objects.Visual;
using BetterLegacy.Editor.Data.Elements;
using BetterLegacy.Editor.Managers;
using BetterLegacy.Menus;
using BetterLegacy.Menus.UI.Interfaces;

// ignore naming styles since modifiers are named like this.
#pragma warning disable IDE1006 // Naming Styles

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
        public static void AssignModifierFunctions(Modifier modifier, ModifierReferenceType referenceType)
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
            new ModifierTrigger("break", ModifierFunctions.breakModifier),
            new ModifierTrigger(nameof(ModifierFunctions.disableModifier), (modifier, reference, variables) => false),

            #region Player

            new ModifierTrigger(nameof(ModifierFunctions.playerCollide), ModifierFunctions.playerCollide, ModifierCompatibility.BeatmapObjectCompatible),
            new ModifierTrigger(nameof(ModifierFunctions.playerCollideIndex), ModifierFunctions.playerCollideIndex, ModifierCompatibility.BeatmapObjectCompatible),
            new ModifierTrigger(nameof(ModifierFunctions.playerCollideOther), ModifierFunctions.playerCollideOther, ModifierCompatibility.FullBeatmapCompatible),
            new ModifierTrigger(nameof(ModifierFunctions.playerCollideIndexOther), ModifierFunctions.playerCollideIndexOther, ModifierCompatibility.FullBeatmapCompatible),
            new ModifierTrigger(nameof(ModifierFunctions.playerHealthEquals), ModifierFunctions.playerHealthEquals),
            new ModifierTrigger(nameof(ModifierFunctions.playerHealthLesserEquals), ModifierFunctions.playerHealthLesserEquals),
            new ModifierTrigger(nameof(ModifierFunctions.playerHealthGreaterEquals), ModifierFunctions.playerHealthGreaterEquals),
            new ModifierTrigger(nameof(ModifierFunctions.playerHealthLesser), ModifierFunctions.playerHealthLesser),
            new ModifierTrigger(nameof(ModifierFunctions.playerHealthGreater), ModifierFunctions.playerHealthGreater),
            new ModifierTrigger(nameof(ModifierFunctions.playerMoving), ModifierFunctions.playerMoving),
            new ModifierTrigger(nameof(ModifierFunctions.playerBoosting), ModifierFunctions.playerBoosting, ModifierCompatibility.BeatmapObjectCompatible),
            new ModifierTrigger(nameof(ModifierFunctions.playerAlive), ModifierFunctions.playerAlive),
            new ModifierTrigger(nameof(ModifierFunctions.playerDeathsEquals), ModifierFunctions.playerDeathsEquals),
            new ModifierTrigger(nameof(ModifierFunctions.playerDeathsLesserEquals), ModifierFunctions.playerDeathsLesserEquals),
            new ModifierTrigger(nameof(ModifierFunctions.playerDeathsGreaterEquals), ModifierFunctions.playerDeathsGreaterEquals),
            new ModifierTrigger(nameof(ModifierFunctions.playerDeathsLesser), ModifierFunctions.playerDeathsLesser),
            new ModifierTrigger(nameof(ModifierFunctions.playerDeathsGreater), ModifierFunctions.playerDeathsGreater),
            new ModifierTrigger(nameof(ModifierFunctions.playerDistanceGreater), ModifierFunctions.playerDistanceGreater),
            new ModifierTrigger(nameof(ModifierFunctions.playerDistanceLesser), ModifierFunctions.playerDistanceLesser),
            new ModifierTrigger(nameof(ModifierFunctions.playerCountEquals), ModifierFunctions.playerCountEquals),
            new ModifierTrigger(nameof(ModifierFunctions.playerCountLesserEquals), ModifierFunctions.playerCountLesserEquals),
            new ModifierTrigger(nameof(ModifierFunctions.playerCountGreaterEquals), ModifierFunctions.playerCountGreaterEquals),
            new ModifierTrigger(nameof(ModifierFunctions.playerCountLesser), ModifierFunctions.playerCountLesser),
            new ModifierTrigger(nameof(ModifierFunctions.playerCountGreater), ModifierFunctions.playerCountGreater),
            new ModifierTrigger(nameof(ModifierFunctions.onPlayerHit), ModifierFunctions.onPlayerHit),
            new ModifierTrigger(nameof(ModifierFunctions.onPlayerDeath), ModifierFunctions.onPlayerDeath),
            new ModifierTrigger(nameof(ModifierFunctions.playerBoostEquals), ModifierFunctions.playerBoostEquals),
            new ModifierTrigger(nameof(ModifierFunctions.playerBoostLesserEquals), ModifierFunctions.playerBoostLesserEquals),
            new ModifierTrigger(nameof(ModifierFunctions.playerBoostGreaterEquals), ModifierFunctions.playerBoostGreaterEquals),
            new ModifierTrigger(nameof(ModifierFunctions.playerBoostLesser), ModifierFunctions.playerBoostLesser),
            new ModifierTrigger(nameof(ModifierFunctions.playerBoostGreater), ModifierFunctions.playerBoostGreater),

            #endregion
            
            #region Collide

            new ModifierTrigger(nameof(ModifierFunctions.bulletCollide), ModifierFunctions.bulletCollide, ModifierCompatibility.BeatmapObjectCompatible),
            new ModifierTrigger(nameof(ModifierFunctions.objectCollide), ModifierFunctions.objectCollide, ModifierCompatibility.BeatmapObjectCompatible),

            #endregion

            #region Controls

            new ModifierTrigger(nameof(ModifierFunctions.keyPressDown), ModifierFunctions.keyPressDown),
            new ModifierTrigger(nameof(ModifierFunctions.keyPress), ModifierFunctions.keyPress),
            new ModifierTrigger(nameof(ModifierFunctions.keyPressUp), ModifierFunctions.keyPressUp),
            new ModifierTrigger(nameof(ModifierFunctions.controlPressDown), ModifierFunctions.controlPressDown),
            new ModifierTrigger(nameof(ModifierFunctions.controlPress), ModifierFunctions.controlPress),
            new ModifierTrigger(nameof(ModifierFunctions.controlPressUp), ModifierFunctions.controlPressUp),
            new ModifierTrigger(nameof(ModifierFunctions.mouseButtonDown), ModifierFunctions.mouseButtonDown),
            new ModifierTrigger(nameof(ModifierFunctions.mouseButton), ModifierFunctions.mouseButton),
            new ModifierTrigger(nameof(ModifierFunctions.mouseButtonUp), ModifierFunctions.mouseButtonUp),
            new ModifierTrigger(nameof(ModifierFunctions.mouseOver), ModifierFunctions.mouseOver, ModifierCompatibility.BeatmapObjectCompatible),
            new ModifierTrigger(nameof(ModifierFunctions.mouseOverSignalModifier), ModifierFunctions.mouseOverSignalModifier, ModifierCompatibility.BeatmapObjectCompatible),

            #endregion

            #region JSON

            new ModifierTrigger(nameof(ModifierFunctions.loadEquals), ModifierFunctions.loadEquals),
            new ModifierTrigger(nameof(ModifierFunctions.loadLesserEquals), ModifierFunctions.loadLesserEquals),
            new ModifierTrigger(nameof(ModifierFunctions.loadGreaterEquals), ModifierFunctions.loadGreaterEquals),
            new ModifierTrigger(nameof(ModifierFunctions.loadLesser), ModifierFunctions.loadLesser),
            new ModifierTrigger(nameof(ModifierFunctions.loadGreater), ModifierFunctions.loadGreater),
            new ModifierTrigger(nameof(ModifierFunctions.loadExists), ModifierFunctions.loadExists),

            #endregion

            #region Variable

            new ModifierTrigger(nameof(ModifierFunctions.localVariableEquals), ModifierFunctions.localVariableEquals),
            new ModifierTrigger(nameof(ModifierFunctions.localVariableLesserEquals), ModifierFunctions.localVariableLesserEquals),
            new ModifierTrigger(nameof(ModifierFunctions.localVariableGreaterEquals), ModifierFunctions.localVariableGreaterEquals),
            new ModifierTrigger(nameof(ModifierFunctions.localVariableLesser), ModifierFunctions.localVariableLesser),
            new ModifierTrigger(nameof(ModifierFunctions.localVariableGreater), ModifierFunctions.localVariableGreater),
            new ModifierTrigger(nameof(ModifierFunctions.localVariableContains), ModifierFunctions.localVariableContains),
            new ModifierTrigger(nameof(ModifierFunctions.localVariableStartsWith), ModifierFunctions.localVariableStartsWith),
            new ModifierTrigger(nameof(ModifierFunctions.localVariableEndsWith), ModifierFunctions.localVariableEndsWith),
            new ModifierTrigger(nameof(ModifierFunctions.localVariableExists), ModifierFunctions.localVariableExists),

            // self
            new ModifierTrigger(nameof(ModifierFunctions.variableEquals), ModifierFunctions.variableEquals),
            new ModifierTrigger(nameof(ModifierFunctions.variableLesserEquals), ModifierFunctions.variableLesserEquals),
            new ModifierTrigger(nameof(ModifierFunctions.variableGreaterEquals), ModifierFunctions.variableGreaterEquals),
            new ModifierTrigger(nameof(ModifierFunctions.variableLesser), ModifierFunctions.variableLesser),
            new ModifierTrigger(nameof(ModifierFunctions.variableGreater), ModifierFunctions.variableGreater),

            // other
            new ModifierTrigger(nameof(ModifierFunctions.variableOtherEquals), ModifierFunctions.variableOtherEquals, ModifierCompatibility.BeatmapObjectCompatible),
            new ModifierTrigger(nameof(ModifierFunctions.variableOtherLesserEquals), ModifierFunctions.variableOtherLesserEquals, ModifierCompatibility.BeatmapObjectCompatible),
            new ModifierTrigger(nameof(ModifierFunctions.variableOtherGreaterEquals), ModifierFunctions.variableOtherGreaterEquals, ModifierCompatibility.BeatmapObjectCompatible),
            new ModifierTrigger(nameof(ModifierFunctions.variableOtherLesser), ModifierFunctions.variableOtherLesser, ModifierCompatibility.BeatmapObjectCompatible),
            new ModifierTrigger(nameof(ModifierFunctions.variableOtherGreater), ModifierFunctions.variableOtherGreater, ModifierCompatibility.BeatmapObjectCompatible),

            #endregion

            #region Audio

            new ModifierTrigger(nameof(ModifierFunctions.pitchEquals), ModifierFunctions.pitchEquals),
            new ModifierTrigger(nameof(ModifierFunctions.pitchLesserEquals), ModifierFunctions.pitchLesserEquals),
            new ModifierTrigger(nameof(ModifierFunctions.pitchGreaterEquals), ModifierFunctions.pitchGreaterEquals),
            new ModifierTrigger(nameof(ModifierFunctions.pitchLesser), ModifierFunctions.pitchLesser),
            new ModifierTrigger(nameof(ModifierFunctions.pitchGreater), ModifierFunctions.pitchGreater),
            new ModifierTrigger(nameof(ModifierFunctions.musicTimeGreater), ModifierFunctions.musicTimeGreater),
            new ModifierTrigger(nameof(ModifierFunctions.musicTimeLesser), ModifierFunctions.musicTimeLesser),
            new ModifierTrigger(nameof(ModifierFunctions.musicTimeInRange), ModifierFunctions.musicTimeInRange),
            new ModifierTrigger(nameof(ModifierFunctions.musicPlaying), ModifierFunctions.musicPlaying),

            new ModifierTrigger("timeLesserEquals", (modifier, reference, variables) => AudioManager.inst.CurrentAudioSource.time <= modifier.GetFloat(0, 0f, variables)),
            new ModifierTrigger("timeGreaterEquals", (modifier, reference, variables) => AudioManager.inst.CurrentAudioSource.time >= modifier.GetFloat(0, 0f, variables)),
            new ModifierTrigger("timeLesser", (modifier, reference, variables)  => AudioManager.inst.CurrentAudioSource.time<modifier.GetFloat(0, 0f, variables)),
            new ModifierTrigger("timeGreater", (modifier, reference, variables)  => AudioManager.inst.CurrentAudioSource.time > modifier.GetFloat(0, 0f, variables)),

            #endregion

            #region Game State

            new ModifierTrigger(nameof(ModifierFunctions.inZenMode), ModifierFunctions.inZenMode),
            new ModifierTrigger(nameof(ModifierFunctions.inNormal), ModifierFunctions.inNormal),
            new ModifierTrigger(nameof(ModifierFunctions.in1Life), ModifierFunctions.in1Life),
            new ModifierTrigger(nameof(ModifierFunctions.inNoHit), ModifierFunctions.inNoHit),
            new ModifierTrigger(nameof(ModifierFunctions.inPractice), ModifierFunctions.inPractice),
            new ModifierTrigger(nameof(ModifierFunctions.inEditor), ModifierFunctions.inEditor),
            new ModifierTrigger(nameof(ModifierFunctions.isEditing), ModifierFunctions.isEditing),

            #endregion

            #region Random

            new ModifierTrigger(nameof(ModifierFunctions.randomEquals), ModifierFunctions.randomEquals),
            new ModifierTrigger(nameof(ModifierFunctions.randomLesser), ModifierFunctions.randomLesser),
            new ModifierTrigger(nameof(ModifierFunctions.randomGreater), ModifierFunctions.randomGreater),

            #endregion

            #region Math

            new ModifierTrigger(nameof(ModifierFunctions.mathEquals), ModifierFunctions.mathEquals),
            new ModifierTrigger(nameof(ModifierFunctions.mathLesserEquals), ModifierFunctions.mathLesserEquals),
            new ModifierTrigger(nameof(ModifierFunctions.mathGreaterEquals), ModifierFunctions.mathGreaterEquals),
            new ModifierTrigger(nameof(ModifierFunctions.mathLesser), ModifierFunctions.mathLesser),
            new ModifierTrigger(nameof(ModifierFunctions.mathGreater), ModifierFunctions.mathGreater),

            #endregion

            #region Animation

            new ModifierTrigger(nameof(ModifierFunctions.axisEquals), ModifierFunctions.axisEquals, ModifierCompatibility.BeatmapObjectCompatible),
            new ModifierTrigger(nameof(ModifierFunctions.axisLesserEquals), ModifierFunctions.axisLesserEquals, ModifierCompatibility.BeatmapObjectCompatible),
            new ModifierTrigger(nameof(ModifierFunctions.axisGreaterEquals), ModifierFunctions.axisGreaterEquals, ModifierCompatibility.BeatmapObjectCompatible),
            new ModifierTrigger(nameof(ModifierFunctions.axisLesser), ModifierFunctions.axisLesser, ModifierCompatibility.BeatmapObjectCompatible),
            new ModifierTrigger(nameof(ModifierFunctions.axisGreater), ModifierFunctions.axisGreater, ModifierCompatibility.BeatmapObjectCompatible),
            
            new ModifierTrigger(nameof(ModifierFunctions.eventEquals), ModifierFunctions.eventEquals),
            new ModifierTrigger(nameof(ModifierFunctions.eventLesserEquals), ModifierFunctions.eventLesserEquals),
            new ModifierTrigger(nameof(ModifierFunctions.eventGreaterEquals), ModifierFunctions.eventGreaterEquals),
            new ModifierTrigger(nameof(ModifierFunctions.eventLesser), ModifierFunctions.eventLesser),
            new ModifierTrigger(nameof(ModifierFunctions.eventGreater), ModifierFunctions.eventGreater),

            #endregion

            #region Level
            
            // self
            new ModifierTrigger(nameof(ModifierFunctions.levelRankEquals), ModifierFunctions.levelRankEquals),
            new ModifierTrigger(nameof(ModifierFunctions.levelRankLesserEquals), ModifierFunctions.levelRankLesserEquals),
            new ModifierTrigger(nameof(ModifierFunctions.levelRankGreaterEquals), ModifierFunctions.levelRankGreaterEquals),
            new ModifierTrigger(nameof(ModifierFunctions.levelRankLesser), ModifierFunctions.levelRankLesser),
            new ModifierTrigger(nameof(ModifierFunctions.levelRankGreater), ModifierFunctions.levelRankGreater),

            // other
            new ModifierTrigger(nameof(ModifierFunctions.levelRankOtherEquals), ModifierFunctions.levelRankOtherEquals),
            new ModifierTrigger(nameof(ModifierFunctions.levelRankOtherLesserEquals), ModifierFunctions.levelRankOtherLesserEquals),
            new ModifierTrigger(nameof(ModifierFunctions.levelRankOtherGreaterEquals), ModifierFunctions.levelRankOtherGreaterEquals),
            new ModifierTrigger(nameof(ModifierFunctions.levelRankOtherLesser), ModifierFunctions.levelRankOtherLesser),
            new ModifierTrigger(nameof(ModifierFunctions.levelRankOtherGreater), ModifierFunctions.levelRankOtherGreater),

            // current
            new ModifierTrigger(nameof(ModifierFunctions.levelRankCurrentEquals), ModifierFunctions.levelRankCurrentEquals),
            new ModifierTrigger(nameof(ModifierFunctions.levelRankCurrentLesserEquals), ModifierFunctions.levelRankCurrentLesserEquals),
            new ModifierTrigger(nameof(ModifierFunctions.levelRankCurrentGreaterEquals), ModifierFunctions.levelRankCurrentGreaterEquals),
            new ModifierTrigger(nameof(ModifierFunctions.levelRankCurrentLesser), ModifierFunctions.levelRankCurrentLesser),
            new ModifierTrigger(nameof(ModifierFunctions.levelRankCurrentGreater), ModifierFunctions.levelRankCurrentGreater),

            // level state
            new ModifierTrigger(nameof(ModifierFunctions.onLevelStart), ModifierFunctions.onLevelStart),
            new ModifierTrigger(nameof(ModifierFunctions.onLevelRestart), ModifierFunctions.onLevelRestart),
            new ModifierTrigger(nameof(ModifierFunctions.onLevelRewind), ModifierFunctions.onLevelRestart),
            new ModifierTrigger(nameof(ModifierFunctions.levelUnlocked), ModifierFunctions.levelUnlocked),
            new ModifierTrigger(nameof(ModifierFunctions.levelCompleted), ModifierFunctions.levelCompleted),
            new ModifierTrigger(nameof(ModifierFunctions.levelCompletedOther), ModifierFunctions.levelCompletedOther),
            new ModifierTrigger(nameof(ModifierFunctions.levelExists), ModifierFunctions.levelExists),
            new ModifierTrigger(nameof(ModifierFunctions.levelPathExists), ModifierFunctions.levelPathExists),

            new ModifierTrigger(nameof(ModifierFunctions.achievementUnlocked), ModifierFunctions.achievementUnlocked, ModifierCompatibility.LevelControlCompatible),

            #endregion

            #region Real Time

            // custom
            new ModifierTrigger(nameof(ModifierFunctions.realTimeEquals), ModifierFunctions.realTimeEquals),
            new ModifierTrigger(nameof(ModifierFunctions.realTimeLesserEquals), ModifierFunctions.realTimeLesserEquals),
            new ModifierTrigger(nameof(ModifierFunctions.realTimeGreaterEquals), ModifierFunctions.realTimeGreaterEquals),
            new ModifierTrigger(nameof(ModifierFunctions.realTimeLesser), ModifierFunctions.realTimeLesser),
            new ModifierTrigger(nameof(ModifierFunctions.realTimeGreater), ModifierFunctions.realTimeGreater),

            // seconds
            new ModifierTrigger(nameof(ModifierFunctions.realTimeSecondEquals), ModifierFunctions.realTimeSecondEquals),
            new ModifierTrigger(nameof(ModifierFunctions.realTimeSecondLesserEquals), ModifierFunctions.realTimeSecondLesserEquals),
            new ModifierTrigger(nameof(ModifierFunctions.realTimeSecondGreaterEquals), ModifierFunctions.realTimeSecondGreaterEquals),
            new ModifierTrigger(nameof(ModifierFunctions.realTimeSecondLesser), ModifierFunctions.realTimeSecondLesser),
            new ModifierTrigger(nameof(ModifierFunctions.realTimeSecondGreater), ModifierFunctions.realTimeSecondGreater),

            // minutes
            new ModifierTrigger(nameof(ModifierFunctions.realTimeMinuteEquals), ModifierFunctions.realTimeMinuteEquals),
            new ModifierTrigger(nameof(ModifierFunctions.realTimeMinuteLesserEquals), ModifierFunctions.realTimeMinuteLesserEquals),
            new ModifierTrigger(nameof(ModifierFunctions.realTimeMinuteGreaterEquals), ModifierFunctions.realTimeMinuteGreaterEquals),
            new ModifierTrigger(nameof(ModifierFunctions.realTimeMinuteLesser), ModifierFunctions.realTimeMinuteLesser),
            new ModifierTrigger(nameof(ModifierFunctions.realTimeMinuteGreater), ModifierFunctions.realTimeMinuteGreater),

            // 24 hours
            new ModifierTrigger(nameof(ModifierFunctions.realTime24HourEquals), ModifierFunctions.realTime24HourEquals),
            new ModifierTrigger(nameof(ModifierFunctions.realTime24HourLesserEquals), ModifierFunctions.realTime24HourLesserEquals),
            new ModifierTrigger(nameof(ModifierFunctions.realTime24HourGreaterEquals), ModifierFunctions.realTime24HourGreaterEquals),
            new ModifierTrigger(nameof(ModifierFunctions.realTime24HourLesser), ModifierFunctions.realTime24HourLesser),
            new ModifierTrigger(nameof(ModifierFunctions.realTime24HourGreater), ModifierFunctions.realTime24HourGreater),

            // 12 hours
            new ModifierTrigger(nameof(ModifierFunctions.realTime12HourEquals), ModifierFunctions.realTime12HourEquals),
            new ModifierTrigger(nameof(ModifierFunctions.realTime12HourLesserEquals), ModifierFunctions.realTime12HourLesserEquals),
            new ModifierTrigger(nameof(ModifierFunctions.realTime12HourGreaterEquals), ModifierFunctions.realTime12HourGreaterEquals),
            new ModifierTrigger(nameof(ModifierFunctions.realTime12HourLesser), ModifierFunctions.realTime12HourLesser),
            new ModifierTrigger(nameof(ModifierFunctions.realTime12HourGreater), ModifierFunctions.realTime12HourGreater),

            // days
            new ModifierTrigger(nameof(ModifierFunctions.realTimeDayEquals), ModifierFunctions.realTimeDayEquals),
            new ModifierTrigger(nameof(ModifierFunctions.realTimeDayLesserEquals), ModifierFunctions.realTimeDayLesserEquals),
            new ModifierTrigger(nameof(ModifierFunctions.realTimeDayGreaterEquals), ModifierFunctions.realTimeDayGreaterEquals),
            new ModifierTrigger(nameof(ModifierFunctions.realTimeDayLesser), ModifierFunctions.realTimeDayLesser),
            new ModifierTrigger(nameof(ModifierFunctions.realTimeDayGreater), ModifierFunctions.realTimeDayGreater),
            new ModifierTrigger(nameof(ModifierFunctions.realTimeDayWeekEquals), ModifierFunctions.realTimeDayWeekEquals),

            // months
            new ModifierTrigger(nameof(ModifierFunctions.realTimeMonthEquals), ModifierFunctions.realTimeMonthEquals),
            new ModifierTrigger(nameof(ModifierFunctions.realTimeMonthLesserEquals), ModifierFunctions.realTimeMonthLesserEquals),
            new ModifierTrigger(nameof(ModifierFunctions.realTimeMonthGreaterEquals), ModifierFunctions.realTimeMonthGreaterEquals),
            new ModifierTrigger(nameof(ModifierFunctions.realTimeMonthLesser), ModifierFunctions.realTimeMonthLesser),
            new ModifierTrigger(nameof(ModifierFunctions.realTimeMonthGreater), ModifierFunctions.realTimeMonthGreater),

            // years
            new ModifierTrigger(nameof(ModifierFunctions.realTimeYearEquals), ModifierFunctions.realTimeYearEquals),
            new ModifierTrigger(nameof(ModifierFunctions.realTimeYearLesserEquals), ModifierFunctions.realTimeYearLesserEquals),
            new ModifierTrigger(nameof(ModifierFunctions.realTimeYearGreaterEquals), ModifierFunctions.realTimeYearGreaterEquals),
            new ModifierTrigger(nameof(ModifierFunctions.realTimeYearLesser), ModifierFunctions.realTimeYearLesser),
            new ModifierTrigger(nameof(ModifierFunctions.realTimeYearGreater), ModifierFunctions.realTimeYearGreater),

            #endregion

            #region Config

            // main
            new ModifierTrigger(nameof(ModifierFunctions.usernameEquals), ModifierFunctions.usernameEquals),
            new ModifierTrigger(nameof(ModifierFunctions.languageEquals), ModifierFunctions.languageEquals),

            // misc
            new ModifierTrigger(nameof(ModifierFunctions.configLDM), ModifierFunctions.configLDM),
            new ModifierTrigger(nameof(ModifierFunctions.configShowEffects), ModifierFunctions.configShowEffects),
            new ModifierTrigger(nameof(ModifierFunctions.configShowPlayerGUI), ModifierFunctions.configShowPlayerGUI),
            new ModifierTrigger(nameof(ModifierFunctions.configShowIntro), ModifierFunctions.configShowIntro),

            #endregion

            #region Misc

            new ModifierTrigger(nameof(ModifierFunctions.await), ModifierFunctions.await),
            new ModifierTrigger(nameof(ModifierFunctions.awaitCounter), ModifierFunctions.awaitCounter),
            new ModifierTrigger(nameof(ModifierFunctions.containsTag), ModifierFunctions.containsTag),
            new ModifierTrigger(nameof(ModifierFunctions.requireSignal), ModifierFunctions.requireSignal),
            new ModifierTrigger(nameof(ModifierFunctions.isFocused), ModifierFunctions.isFocused),
            new ModifierTrigger(nameof(ModifierFunctions.isFullscreen), ModifierFunctions.isFullscreen),
            new ModifierTrigger(nameof(ModifierFunctions.objectActive), ModifierFunctions.objectActive),
            new ModifierTrigger(nameof(ModifierFunctions.objectCustomActive), ModifierFunctions.objectCustomActive),
            new ModifierTrigger(nameof(ModifierFunctions.objectAlive), ModifierFunctions.objectAlive, ModifierCompatibility.BeatmapObjectCompatible),
            new ModifierTrigger(nameof(ModifierFunctions.objectSpawned), ModifierFunctions.objectSpawned, ModifierCompatibility.BeatmapObjectCompatible),
            new ModifierTrigger(nameof(ModifierFunctions.onMarker), ModifierFunctions.onMarker),
            new ModifierTrigger(nameof(ModifierFunctions.onCheckpoint), ModifierFunctions.onCheckpoint),
            new ModifierTrigger(nameof(ModifierFunctions.fromPrefab), ModifierFunctions.fromPrefab),
            new ModifierTrigger(nameof(ModifierFunctions.callModifierBlockTrigger), ModifierFunctions.callModifierBlockTrigger, ModifierCompatibility.LevelControlCompatible),

            #endregion

            #region Player Only
            
            new ModifierTrigger(nameof(ModifierFunctions.healthEquals), ModifierFunctions.healthEquals, ModifierCompatibility.PAPlayerCompatible),
            new ModifierTrigger(nameof(ModifierFunctions.healthGreaterEquals), ModifierFunctions.healthGreaterEquals, ModifierCompatibility.PAPlayerCompatible),
            new ModifierTrigger(nameof(ModifierFunctions.healthLesserEquals), ModifierFunctions.healthLesserEquals, ModifierCompatibility.PAPlayerCompatible),
            new ModifierTrigger(nameof(ModifierFunctions.healthGreater), ModifierFunctions.healthGreater, ModifierCompatibility.PAPlayerCompatible),
            new ModifierTrigger(nameof(ModifierFunctions.healthLesser), ModifierFunctions.healthLesser, ModifierCompatibility.PAPlayerCompatible),
            new ModifierTrigger(nameof(ModifierFunctions.isDead), ModifierFunctions.isDead, ModifierCompatibility.PAPlayerCompatible),
            new ModifierTrigger(nameof(ModifierFunctions.isBoosting), ModifierFunctions.isBoosting, ModifierCompatibility.PAPlayerCompatible),
            new ModifierTrigger(nameof(ModifierFunctions.isColliding), ModifierFunctions.isColliding, ModifierCompatibility.PAPlayerCompatible),
            new ModifierTrigger(nameof(ModifierFunctions.isSolidColliding), ModifierFunctions.isSolidColliding, ModifierCompatibility.PAPlayerCompatible),

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
                return Story.StoryManager.inst.CurrentSave && Story.StoryManager.inst.CurrentSave.LoadBool(modifier.GetValue(0, variables), modifier.GetBool(1, false, variables));
            }),

            #endregion
        };

        public static ModifierAction[] actions = new ModifierAction[]
        {
            #region Audio

            // pitch
            new ModifierAction(nameof(ModifierFunctions.setPitch),  ModifierFunctions.setPitch, ModifierCompatibility.LevelControlCompatible),
            new ModifierAction(nameof(ModifierFunctions.addPitch),  ModifierFunctions.addPitch, ModifierCompatibility.LevelControlCompatible),
            new ModifierAction(nameof(ModifierFunctions.setPitchMath),  ModifierFunctions.setPitchMath, ModifierCompatibility.LevelControlCompatible),
            new ModifierAction(nameof(ModifierFunctions.addPitchMath),  ModifierFunctions.addPitchMath, ModifierCompatibility.LevelControlCompatible),

            // music playing states
            new ModifierAction(nameof(ModifierFunctions.setMusicTime),  ModifierFunctions.setMusicTime, ModifierCompatibility.LevelControlCompatible),
            new ModifierAction(nameof(ModifierFunctions.setMusicTimeMath),  ModifierFunctions.setMusicTimeMath, ModifierCompatibility.LevelControlCompatible),
            new ModifierAction(nameof(ModifierFunctions.setMusicTimeStartTime),  ModifierFunctions.setMusicTimeStartTime, ModifierCompatibility.LevelControlCompatible),
            new ModifierAction(nameof(ModifierFunctions.setMusicTimeAutokill),  ModifierFunctions.setMusicTimeAutokill, ModifierCompatibility.LevelControlCompatible),
            new ModifierAction(nameof(ModifierFunctions.setMusicPlaying),  ModifierFunctions.setMusicPlaying, ModifierCompatibility.LevelControlCompatible),

            // play sound
            new ModifierAction(nameof(ModifierFunctions.playSound),  ModifierFunctions.playSound),
            new ModifierAction(nameof(ModifierFunctions.playSoundOnline),  ModifierFunctions.playSoundOnline),
            new ModifierAction(nameof(ModifierFunctions.playOnlineSound),  ModifierFunctions.playOnlineSound),
            new ModifierAction(nameof(ModifierFunctions.playDefaultSound),  ModifierFunctions.playDefaultSound),
            new ModifierAction(nameof(ModifierFunctions.audioSource),  ModifierFunctions.audioSource, ModifierCompatibility.BeatmapObjectCompatible),
            new ModifierAction(nameof(ModifierFunctions.loadSoundAsset),  ModifierFunctions.loadSoundAsset, ModifierCompatibility.BeatmapObjectCompatible),

            #endregion

            #region Level

            new ModifierAction(nameof(ModifierFunctions.loadLevel),  ModifierFunctions.loadLevel, ModifierCompatibility.LevelControlCompatible),
            new ModifierAction(nameof(ModifierFunctions.loadLevelID),  ModifierFunctions.loadLevelID, ModifierCompatibility.LevelControlCompatible),
            new ModifierAction(nameof(ModifierFunctions.loadLevelInternal),  ModifierFunctions.loadLevelInternal, ModifierCompatibility.LevelControlCompatible),
            new ModifierAction(nameof(ModifierFunctions.loadLevelPrevious),  ModifierFunctions.loadLevelPrevious, ModifierCompatibility.LevelControlCompatible),
            new ModifierAction(nameof(ModifierFunctions.loadLevelHub),  ModifierFunctions.loadLevelHub, ModifierCompatibility.LevelControlCompatible),
            new ModifierAction(nameof(ModifierFunctions.loadLevelInCollection),  ModifierFunctions.loadLevelInCollection, ModifierCompatibility.LevelControlCompatible),
            new ModifierAction(nameof(ModifierFunctions.loadLevelCollection),  ModifierFunctions.loadLevelCollection, ModifierCompatibility.LevelControlCompatible),
            new ModifierAction(nameof(ModifierFunctions.downloadLevel),  ModifierFunctions.downloadLevel, ModifierCompatibility.LevelControlCompatible),
            new ModifierAction(nameof(ModifierFunctions.endLevel),  ModifierFunctions.endLevel, ModifierCompatibility.LevelControlCompatible),
            new ModifierAction(nameof(ModifierFunctions.setAudioTransition),  ModifierFunctions.setAudioTransition, ModifierCompatibility.LevelControlCompatible),
            new ModifierAction(nameof(ModifierFunctions.setIntroFade),  ModifierFunctions.setIntroFade, ModifierCompatibility.LevelControlCompatible),
            new ModifierAction(nameof(ModifierFunctions.setLevelEndFunc),  ModifierFunctions.setLevelEndFunc, ModifierCompatibility.LevelControlCompatible),
            new ModifierAction(nameof(ModifierFunctions.getCurrentLevelID),  ModifierFunctions.getCurrentLevelID),
            new ModifierAction(nameof(ModifierFunctions.getCurrentLevelName),  ModifierFunctions.getCurrentLevelName),
            new ModifierAction(nameof(ModifierFunctions.getCurrentLevelRank),  ModifierFunctions.getCurrentLevelRank),

            #endregion

            #region Component

            new ModifierAction(nameof(ModifierFunctions.blur),  ModifierFunctions.blur, ModifierCompatibility.BeatmapObjectCompatible),
            new ModifierAction(nameof(ModifierFunctions.blurOther),  ModifierFunctions.blurOther, ModifierCompatibility.BeatmapObjectCompatible),
            new ModifierAction(nameof(ModifierFunctions.blurVariable),  ModifierFunctions.blurVariable, ModifierCompatibility.BeatmapObjectCompatible),
            new ModifierAction(nameof(ModifierFunctions.blurVariableOther),  ModifierFunctions.blurVariableOther, ModifierCompatibility.BeatmapObjectCompatible),
            new ModifierAction(nameof(ModifierFunctions.blurColored),  ModifierFunctions.blurColored, ModifierCompatibility.BeatmapObjectCompatible),
            new ModifierAction(nameof(ModifierFunctions.blurColoredOther),  ModifierFunctions.blurColoredOther, ModifierCompatibility.BeatmapObjectCompatible),
            new ModifierAction(nameof(ModifierFunctions.doubleSided),  ModifierFunctions.doubleSided, ModifierCompatibility.BeatmapObjectCompatible),
            new ModifierAction(nameof(ModifierFunctions.particleSystem),  ModifierFunctions.particleSystem, ModifierCompatibility.BeatmapObjectCompatible),
            new ModifierAction(nameof(ModifierFunctions.particleSystemHex),  ModifierFunctions.particleSystemHex, ModifierCompatibility.BeatmapObjectCompatible),
            new ModifierAction(nameof(ModifierFunctions.trailRenderer),  ModifierFunctions.trailRenderer, ModifierCompatibility.BeatmapObjectCompatible),
            new ModifierAction(nameof(ModifierFunctions.trailRendererHex),  ModifierFunctions.trailRendererHex, ModifierCompatibility.BeatmapObjectCompatible),
            new ModifierAction(nameof(ModifierFunctions.rigidbody),  ModifierFunctions.rigidbody, ModifierCompatibility.BeatmapObjectCompatible),
            new ModifierAction(nameof(ModifierFunctions.rigidbodyOther),  ModifierFunctions.rigidbodyOther, ModifierCompatibility.BeatmapObjectCompatible),
            new ModifierAction(nameof(ModifierFunctions.setRenderType),  ModifierFunctions.setRenderType, ModifierCompatibility.BeatmapObjectCompatible),
            new ModifierAction(nameof(ModifierFunctions.setRenderTypeOther),  ModifierFunctions.setRenderTypeOther, ModifierCompatibility.BeatmapObjectCompatible),
            new ModifierAction(nameof(ModifierFunctions.setRendering),  ModifierFunctions.setRendering, ModifierCompatibility.BeatmapObjectCompatible),
            new ModifierAction(nameof(ModifierFunctions.setOutline),  ModifierFunctions.setOutline, ModifierCompatibility.BeatmapObjectCompatible),
            new ModifierAction(nameof(ModifierFunctions.setOutlineOther),  ModifierFunctions.setOutlineOther, ModifierCompatibility.FullBeatmapCompatible),
            new ModifierAction(nameof(ModifierFunctions.setOutlineHex),  ModifierFunctions.setOutlineHex, ModifierCompatibility.BeatmapObjectCompatible),
            new ModifierAction(nameof(ModifierFunctions.setOutlineHexOther),  ModifierFunctions.setOutlineHexOther, ModifierCompatibility.FullBeatmapCompatible),

            #endregion

            #region Player

            // hit
            new ModifierAction(nameof(ModifierFunctions.playerHit),  ModifierFunctions.playerHit, ModifierCompatibility.BeatmapObjectCompatible),
            new ModifierAction(nameof(ModifierFunctions.playerHitIndex),  ModifierFunctions.playerHitIndex, ModifierCompatibility.LevelControlCompatible),
            new ModifierAction(nameof(ModifierFunctions.playerHitAll),  ModifierFunctions.playerHitAll, ModifierCompatibility.LevelControlCompatible),

            // heal
            new ModifierAction(nameof(ModifierFunctions.playerHeal),  ModifierFunctions.playerHeal, ModifierCompatibility.BeatmapObjectCompatible),
            new ModifierAction(nameof(ModifierFunctions.playerHealIndex),  ModifierFunctions.playerHealIndex, ModifierCompatibility.LevelControlCompatible),
            new ModifierAction(nameof(ModifierFunctions.playerHealAll),  ModifierFunctions.playerHealAll, ModifierCompatibility.LevelControlCompatible),

            // kill
            new ModifierAction(nameof(ModifierFunctions.playerKill),  ModifierFunctions.playerKill, ModifierCompatibility.BeatmapObjectCompatible),
            new ModifierAction(nameof(ModifierFunctions.playerKillIndex),  ModifierFunctions.playerKillIndex, ModifierCompatibility.LevelControlCompatible),
            new ModifierAction(nameof(ModifierFunctions.playerKillAll),  ModifierFunctions.playerKillAll, ModifierCompatibility.LevelControlCompatible),

            // respawn
            new ModifierAction(nameof(ModifierFunctions.playerRespawn),  ModifierFunctions.playerRespawn, ModifierCompatibility.BeatmapObjectCompatible),
            new ModifierAction(nameof(ModifierFunctions.playerRespawnIndex),  ModifierFunctions.playerRespawnIndex, ModifierCompatibility.LevelControlCompatible),
            new ModifierAction(nameof(ModifierFunctions.playerRespawnAll),  ModifierFunctions.playerRespawnAll, ModifierCompatibility.LevelControlCompatible),

            // lock
            new ModifierAction(nameof(ModifierFunctions.playerLockX), ModifierFunctions.playerLockX, ModifierCompatibility.LevelControlCompatible),
            new ModifierAction(nameof(ModifierFunctions.playerLockXIndex), ModifierFunctions.playerLockXIndex, ModifierCompatibility.LevelControlCompatible),
            new ModifierAction(nameof(ModifierFunctions.playerLockXAll), ModifierFunctions.playerLockXAll, ModifierCompatibility.LevelControlCompatible),
            new ModifierAction(nameof(ModifierFunctions.playerLockY), ModifierFunctions.playerLockY, ModifierCompatibility.LevelControlCompatible),
            new ModifierAction(nameof(ModifierFunctions.playerLockYIndex), ModifierFunctions.playerLockYIndex, ModifierCompatibility.LevelControlCompatible),
            new ModifierAction(nameof(ModifierFunctions.playerLockYAll), ModifierFunctions.playerLockYAll, ModifierCompatibility.LevelControlCompatible),
            new ModifierAction(nameof(ModifierFunctions.playerLockBoostAll),  ModifierFunctions.playerLockBoostAll, ModifierCompatibility.LevelControlCompatible),

            new ModifierAction(nameof(ModifierFunctions.playerEnable),  ModifierFunctions.playerEnable, ModifierCompatibility.LevelControlCompatible),
            new ModifierAction(nameof(ModifierFunctions.playerEnableIndex),  ModifierFunctions.playerEnableIndex, ModifierCompatibility.LevelControlCompatible),
            new ModifierAction(nameof(ModifierFunctions.playerEnableAll),  ModifierFunctions.playerEnableAll, ModifierCompatibility.LevelControlCompatible),

            // player move
            new ModifierAction(nameof(ModifierFunctions.playerMove),  ModifierFunctions.playerMove, ModifierCompatibility.BeatmapObjectCompatible),
            new ModifierAction(nameof(ModifierFunctions.playerMoveIndex),  ModifierFunctions.playerMoveIndex, ModifierCompatibility.LevelControlCompatible),
            new ModifierAction(nameof(ModifierFunctions.playerMoveAll),  ModifierFunctions.playerMoveAll, ModifierCompatibility.LevelControlCompatible),
            new ModifierAction(nameof(ModifierFunctions.playerMoveX),  ModifierFunctions.playerMoveX, ModifierCompatibility.BeatmapObjectCompatible),
            new ModifierAction(nameof(ModifierFunctions.playerMoveXIndex),  ModifierFunctions.playerMoveXIndex, ModifierCompatibility.LevelControlCompatible),
            new ModifierAction(nameof(ModifierFunctions.playerMoveXAll),  ModifierFunctions.playerMoveXAll, ModifierCompatibility.LevelControlCompatible),
            new ModifierAction(nameof(ModifierFunctions.playerMoveY),  ModifierFunctions.playerMoveY, ModifierCompatibility.BeatmapObjectCompatible),
            new ModifierAction(nameof(ModifierFunctions.playerMoveYIndex),  ModifierFunctions.playerMoveYIndex, ModifierCompatibility.LevelControlCompatible),
            new ModifierAction(nameof(ModifierFunctions.playerMoveYAll),  ModifierFunctions.playerMoveYAll, ModifierCompatibility.LevelControlCompatible),
            new ModifierAction(nameof(ModifierFunctions.playerRotate),  ModifierFunctions.playerRotate, ModifierCompatibility.BeatmapObjectCompatible),
            new ModifierAction(nameof(ModifierFunctions.playerRotateIndex),  ModifierFunctions.playerRotateIndex, ModifierCompatibility.LevelControlCompatible),
            new ModifierAction(nameof(ModifierFunctions.playerRotateAll),  ModifierFunctions.playerRotateAll, ModifierCompatibility.LevelControlCompatible),

            // move to object
            new ModifierAction(nameof(ModifierFunctions.playerMoveToObject),  ModifierFunctions.playerMoveToObject, ModifierCompatibility.BeatmapObjectCompatible),
            new ModifierAction(nameof(ModifierFunctions.playerMoveIndexToObject),  ModifierFunctions.playerMoveIndexToObject, ModifierCompatibility.BeatmapObjectCompatible),
            new ModifierAction(nameof(ModifierFunctions.playerMoveAllToObject),  ModifierFunctions.playerMoveAllToObject, ModifierCompatibility.BeatmapObjectCompatible),
            new ModifierAction(nameof(ModifierFunctions.playerMoveXToObject),  ModifierFunctions.playerMoveXToObject, ModifierCompatibility.BeatmapObjectCompatible),
            new ModifierAction(nameof(ModifierFunctions.playerMoveXIndexToObject),  ModifierFunctions.playerMoveXIndexToObject, ModifierCompatibility.BeatmapObjectCompatible),
            new ModifierAction(nameof(ModifierFunctions.playerMoveXAllToObject),  ModifierFunctions.playerMoveXAllToObject, ModifierCompatibility.BeatmapObjectCompatible),
            new ModifierAction(nameof(ModifierFunctions.playerMoveYToObject),  ModifierFunctions.playerMoveYToObject, ModifierCompatibility.BeatmapObjectCompatible),
            new ModifierAction(nameof(ModifierFunctions.playerMoveYIndexToObject),  ModifierFunctions.playerMoveYIndexToObject, ModifierCompatibility.BeatmapObjectCompatible),
            new ModifierAction(nameof(ModifierFunctions.playerMoveYAllToObject),  ModifierFunctions.playerMoveYAllToObject, ModifierCompatibility.BeatmapObjectCompatible),
            new ModifierAction(nameof(ModifierFunctions.playerRotateToObject),  ModifierFunctions.playerRotateToObject, ModifierCompatibility.BeatmapObjectCompatible),
            new ModifierAction(nameof(ModifierFunctions.playerRotateIndexToObject),  ModifierFunctions.playerRotateIndexToObject, ModifierCompatibility.BeatmapObjectCompatible),
            new ModifierAction(nameof(ModifierFunctions.playerRotateAllToObject),  ModifierFunctions.playerRotateAllToObject, ModifierCompatibility.BeatmapObjectCompatible),
            new ModifierAction(nameof(ModifierFunctions.playerDrag),  ModifierFunctions.playerDrag, ModifierCompatibility.BeatmapObjectCompatible),

            // actions
            new ModifierAction(nameof(ModifierFunctions.playerBoost),  ModifierFunctions.playerBoost, ModifierCompatibility.BeatmapObjectCompatible),
            new ModifierAction(nameof(ModifierFunctions.playerBoostIndex),  ModifierFunctions.playerBoostIndex, ModifierCompatibility.LevelControlCompatible),
            new ModifierAction(nameof(ModifierFunctions.playerBoostAll),  ModifierFunctions.playerBoostAll, ModifierCompatibility.LevelControlCompatible),
            new ModifierAction(nameof(ModifierFunctions.playerCancelBoost),  ModifierFunctions.playerCancelBoost, ModifierCompatibility.BeatmapObjectCompatible),
            new ModifierAction(nameof(ModifierFunctions.playerCancelBoostIndex),  ModifierFunctions.playerCancelBoostIndex, ModifierCompatibility.LevelControlCompatible),
            new ModifierAction(nameof(ModifierFunctions.playerCancelBoostAll),  ModifierFunctions.playerCancelBoostAll, ModifierCompatibility.LevelControlCompatible),
            new ModifierAction(nameof(ModifierFunctions.playerDisableBoost),  ModifierFunctions.playerDisableBoost, ModifierCompatibility.BeatmapObjectCompatible),
            new ModifierAction(nameof(ModifierFunctions.playerDisableBoostIndex),  ModifierFunctions.playerDisableBoostIndex, ModifierCompatibility.LevelControlCompatible),
            new ModifierAction(nameof(ModifierFunctions.playerDisableBoostAll),  ModifierFunctions.playerDisableBoostAll, ModifierCompatibility.LevelControlCompatible),
            new ModifierAction(nameof(ModifierFunctions.playerEnableBoost),  ModifierFunctions.playerEnableBoost, ModifierCompatibility.BeatmapObjectCompatible),
            new ModifierAction(nameof(ModifierFunctions.playerEnableBoostIndex),  ModifierFunctions.playerEnableBoostIndex, ModifierCompatibility.LevelControlCompatible),
            new ModifierAction(nameof(ModifierFunctions.playerEnableBoostAll),  ModifierFunctions.playerEnableBoostAll, ModifierCompatibility.LevelControlCompatible),
            new ModifierAction(nameof(ModifierFunctions.playerEnableMove),  ModifierFunctions.playerEnableMove, ModifierCompatibility.BeatmapObjectCompatible),
            new ModifierAction(nameof(ModifierFunctions.playerEnableMoveIndex),  ModifierFunctions.playerEnableMoveIndex, ModifierCompatibility.LevelControlCompatible),
            new ModifierAction(nameof(ModifierFunctions.playerEnableMoveAll),  ModifierFunctions.playerEnableMoveAll, ModifierCompatibility.LevelControlCompatible),

            // speed
            new ModifierAction(nameof(ModifierFunctions.playerSpeed),  ModifierFunctions.playerSpeed, ModifierCompatibility.LevelControlCompatible),
            new ModifierAction(nameof(ModifierFunctions.playerVelocity),  ModifierFunctions.playerVelocity, ModifierCompatibility.BeatmapObjectCompatible),
            new ModifierAction(nameof(ModifierFunctions.playerVelocityIndex),  ModifierFunctions.playerVelocityIndex, ModifierCompatibility.LevelControlCompatible),
            new ModifierAction(nameof(ModifierFunctions.playerVelocityAll),  ModifierFunctions.playerVelocityAll, ModifierCompatibility.LevelControlCompatible),
            new ModifierAction(nameof(ModifierFunctions.playerVelocityX),  ModifierFunctions.playerVelocityX, ModifierCompatibility.BeatmapObjectCompatible),
            new ModifierAction(nameof(ModifierFunctions.playerVelocityXIndex),  ModifierFunctions.playerVelocityXIndex, ModifierCompatibility.LevelControlCompatible),
            new ModifierAction(nameof(ModifierFunctions.playerVelocityXAll),  ModifierFunctions.playerVelocityXAll, ModifierCompatibility.LevelControlCompatible),
            new ModifierAction(nameof(ModifierFunctions.playerVelocityY),  ModifierFunctions.playerVelocityY, ModifierCompatibility.BeatmapObjectCompatible),
            new ModifierAction(nameof(ModifierFunctions.playerVelocityYIndex),  ModifierFunctions.playerVelocityYIndex, ModifierCompatibility.LevelControlCompatible),
            new ModifierAction(nameof(ModifierFunctions.playerVelocityYAll),  ModifierFunctions.playerVelocityYAll, ModifierCompatibility.LevelControlCompatible),

            new ModifierAction(nameof(ModifierFunctions.playerEnableDamage),  ModifierFunctions.playerEnableDamage, ModifierCompatibility.LevelControlCompatible),
            new ModifierAction(nameof(ModifierFunctions.playerEnableDamageIndex),  ModifierFunctions.playerEnableDamageIndex, ModifierCompatibility.LevelControlCompatible),
            new ModifierAction(nameof(ModifierFunctions.playerEnableDamageAll),  ModifierFunctions.playerEnableDamageAll, ModifierCompatibility.LevelControlCompatible),
            new ModifierAction(nameof(ModifierFunctions.setPlayerModel),  ModifierFunctions.setPlayerModel, ModifierCompatibility.LevelControlCompatible),
            new ModifierAction(nameof(ModifierFunctions.setGameMode),  ModifierFunctions.setGameMode, ModifierCompatibility.LevelControlCompatible),
            new ModifierAction(nameof(ModifierFunctions.gameMode),  ModifierFunctions.setGameMode, ModifierCompatibility.LevelControlCompatible),

            new ModifierAction(nameof(ModifierFunctions.blackHole),  ModifierFunctions.blackHole, ModifierCompatibility.BeatmapObjectCompatible),
            new ModifierAction(nameof(ModifierFunctions.blackHoleIndex),  ModifierFunctions.blackHoleIndex, ModifierCompatibility.BeatmapObjectCompatible),
            new ModifierAction(nameof(ModifierFunctions.blackHoleAll),  ModifierFunctions.blackHoleAll, ModifierCompatibility.BeatmapObjectCompatible),
            new ModifierAction(nameof(ModifierFunctions.whiteHole),  ModifierFunctions.whiteHole, ModifierCompatibility.BeatmapObjectCompatible),
            new ModifierAction(nameof(ModifierFunctions.whiteHoleIndex),  ModifierFunctions.whiteHoleIndex, ModifierCompatibility.BeatmapObjectCompatible),
            new ModifierAction(nameof(ModifierFunctions.whiteHoleAll),  ModifierFunctions.whiteHoleAll, ModifierCompatibility.BeatmapObjectCompatible),

            #endregion

            #region Mouse Cursor

            new ModifierAction(nameof(ModifierFunctions.showMouse),  ModifierFunctions.showMouse, ModifierCompatibility.LevelControlCompatible),
            new ModifierAction(nameof(ModifierFunctions.hideMouse),  ModifierFunctions.hideMouse, ModifierCompatibility.LevelControlCompatible),
            new ModifierAction(nameof(ModifierFunctions.setMousePosition),  ModifierFunctions.setMousePosition, ModifierCompatibility.LevelControlCompatible),
            new ModifierAction(nameof(ModifierFunctions.followMousePosition),  ModifierFunctions.followMousePosition, ModifierCompatibility.FullBeatmapCompatible),

            #endregion

            #region Variable

            new ModifierAction(nameof(ModifierFunctions.getToggle),  ModifierFunctions.getToggle),
            new ModifierAction(nameof(ModifierFunctions.getFloat),  ModifierFunctions.getFloat),
            new ModifierAction(nameof(ModifierFunctions.getInt),  ModifierFunctions.getInt),
            new ModifierAction(nameof(ModifierFunctions.getString),  ModifierFunctions.getString),
            new ModifierAction(nameof(ModifierFunctions.getStringLower),  ModifierFunctions.getStringLower),
            new ModifierAction(nameof(ModifierFunctions.getStringUpper),  ModifierFunctions.getStringUpper),
            new ModifierAction(nameof(ModifierFunctions.getColor),  ModifierFunctions.getColor),
            new ModifierAction(nameof(ModifierFunctions.getEnum),  ModifierFunctions.getEnum),
            new ModifierAction(nameof(ModifierFunctions.getTag),  ModifierFunctions.getTag),
            new ModifierAction(nameof(ModifierFunctions.getPitch),  ModifierFunctions.getPitch),
            new ModifierAction(nameof(ModifierFunctions.getMusicTime),  ModifierFunctions.getMusicTime),
            new ModifierAction(nameof(ModifierFunctions.getAxis),  ModifierFunctions.getAxis),
            new ModifierAction(nameof(ModifierFunctions.getAxisMath),  ModifierFunctions.getAxisMath),
            new ModifierAction(nameof(ModifierFunctions.getMath),  ModifierFunctions.getMath),
            new ModifierAction(nameof(ModifierFunctions.getNearestPlayer),  ModifierFunctions.getNearestPlayer, ModifierCompatibility.BeatmapObjectCompatible),
            new ModifierAction(nameof(ModifierFunctions.getCollidingPlayers),  ModifierFunctions.getCollidingPlayers, ModifierCompatibility.BeatmapObjectCompatible),
            new ModifierAction(nameof(ModifierFunctions.getPlayerHealth),  ModifierFunctions.getPlayerHealth),
            new ModifierAction(nameof(ModifierFunctions.getPlayerLives),  ModifierFunctions.getPlayerLives),
            new ModifierAction(nameof(ModifierFunctions.getPlayerPosX),  ModifierFunctions.getPlayerPosX),
            new ModifierAction(nameof(ModifierFunctions.getPlayerPosY),  ModifierFunctions.getPlayerPosY),
            new ModifierAction(nameof(ModifierFunctions.getPlayerRot),  ModifierFunctions.getPlayerRot),
            new ModifierAction(nameof(ModifierFunctions.getEventValue),  ModifierFunctions.getEventValue),
            new ModifierAction(nameof(ModifierFunctions.getSample),  ModifierFunctions.getSample),
            new ModifierAction(nameof(ModifierFunctions.getText),  ModifierFunctions.getText, ModifierCompatibility.BeatmapObjectCompatible),
            new ModifierAction(nameof(ModifierFunctions.getTextOther),  ModifierFunctions.getTextOther, ModifierCompatibility.BeatmapObjectCompatible),
            new ModifierAction(nameof(ModifierFunctions.getCurrentKey),  ModifierFunctions.getCurrentKey),
            new ModifierAction(nameof(ModifierFunctions.getColorSlotHexCode),  ModifierFunctions.getColorSlotHexCode),
            new ModifierAction(nameof(ModifierFunctions.getFloatFromHexCode),  ModifierFunctions.getFloatFromHexCode),
            new ModifierAction(nameof(ModifierFunctions.getHexCodeFromFloat),  ModifierFunctions.getHexCodeFromFloat),
            new ModifierAction(nameof(ModifierFunctions.getModifiedColor),  ModifierFunctions.getModifiedColor),
            new ModifierAction(nameof(ModifierFunctions.getMixedColors),  ModifierFunctions.getMixedColors),
            new ModifierAction(nameof(ModifierFunctions.getLerpColor),  ModifierFunctions.getLerpColor),
            new ModifierAction(nameof(ModifierFunctions.getAddColor),  ModifierFunctions.getAddColor),
            new ModifierAction(nameof(ModifierFunctions.getVisualColor),  ModifierFunctions.getVisualColor),
            new ModifierAction(nameof(ModifierFunctions.getVisualColorRGBA),  ModifierFunctions.getVisualColorRGBA),
            new ModifierAction(nameof(ModifierFunctions.getVisualOpacity),  ModifierFunctions.getVisualOpacity),
            new ModifierAction(nameof(ModifierFunctions.getJSONString),  ModifierFunctions.getJSONString),
            new ModifierAction(nameof(ModifierFunctions.getJSONFloat),  ModifierFunctions.getJSONFloat),
            new ModifierAction(nameof(ModifierFunctions.getJSON),  ModifierFunctions.getJSON),
            new ModifierAction(nameof(ModifierFunctions.getSubString),  ModifierFunctions.getSubString),
            new ModifierAction(nameof(ModifierFunctions.getSplitString),  ModifierFunctions.getSplitString),
            new ModifierAction(nameof(ModifierFunctions.getSplitStringAt),  ModifierFunctions.getSplitStringAt),
            new ModifierAction(nameof(ModifierFunctions.getSplitStringCount),  ModifierFunctions.getSplitStringCount),
            new ModifierAction(nameof(ModifierFunctions.getStringLength),  ModifierFunctions.getStringLength),
            new ModifierAction(nameof(ModifierFunctions.getParsedString),  ModifierFunctions.getParsedString),
            new ModifierAction(nameof(ModifierFunctions.getRegex),  ModifierFunctions.getRegex),
            new ModifierAction(nameof(ModifierFunctions.getFormatVariable),  ModifierFunctions.getFormatVariable),
            new ModifierAction(nameof(ModifierFunctions.getComparison),  ModifierFunctions.getComparison),
            new ModifierAction(nameof(ModifierFunctions.getComparisonMath),  ModifierFunctions.getComparisonMath),
            new ModifierAction(nameof(ModifierFunctions.getEditorBin),  ModifierFunctions.getEditorBin),
            new ModifierAction(nameof(ModifierFunctions.getEditorLayer),  ModifierFunctions.getEditorLayer),
            new ModifierAction(nameof(ModifierFunctions.getObjectName),  ModifierFunctions.getObjectName),
            new ModifierAction(nameof(ModifierFunctions.getSignaledVariables),  ModifierFunctions.getSignaledVariables),
            new ModifierAction(nameof(ModifierFunctions.signalLocalVariables),  ModifierFunctions.signalLocalVariables),
            new ModifierAction(nameof(ModifierFunctions.clearLocalVariables),  ModifierFunctions.clearLocalVariables),
            new ModifierAction(nameof(ModifierFunctions.storeLocalVariables),  ModifierFunctions.storeLocalVariables),

            new ModifierAction(nameof(ModifierFunctions.addVariable),  ModifierFunctions.addVariable),
            new ModifierAction(nameof(ModifierFunctions.addVariableOther),  ModifierFunctions.addVariableOther, ModifierCompatibility.LevelControlCompatible),
            new ModifierAction(nameof(ModifierFunctions.subVariable),  ModifierFunctions.subVariable),
            new ModifierAction(nameof(ModifierFunctions.subVariableOther),  ModifierFunctions.subVariableOther, ModifierCompatibility.LevelControlCompatible),
            new ModifierAction(nameof(ModifierFunctions.setVariable),  ModifierFunctions.setVariable),
            new ModifierAction(nameof(ModifierFunctions.setVariableOther),  ModifierFunctions.setVariableOther, ModifierCompatibility.LevelControlCompatible),
            new ModifierAction(nameof(ModifierFunctions.setVariableRandom),  ModifierFunctions.setVariableRandom),
            new ModifierAction(nameof(ModifierFunctions.setVariableRandomOther),  ModifierFunctions.setVariableRandomOther, ModifierCompatibility.LevelControlCompatible),
            new ModifierAction(nameof(ModifierFunctions.animateVariableOther),  ModifierFunctions.animateVariableOther, ModifierCompatibility.LevelControlCompatible),
            new ModifierAction(nameof(ModifierFunctions.clampVariable),  ModifierFunctions.clampVariable),
            new ModifierAction(nameof(ModifierFunctions.clampVariableOther),  ModifierFunctions.clampVariableOther, ModifierCompatibility.LevelControlCompatible),

            #endregion

            #region Enable

            // old bg modifiers
            new ModifierAction(nameof(ModifierFunctions.setActive), ModifierFunctions.setActive, ModifierCompatibility.BackgroundObjectCompatible),
            new ModifierAction(nameof(ModifierFunctions.setActiveOther), ModifierFunctions.setActiveOther, ModifierCompatibility.BackgroundObjectCompatible),

            // enable
            new ModifierAction(nameof(ModifierFunctions.enableObject),  ModifierFunctions.enableObject, ModifierCompatibility.FullBeatmapCompatible.WithPAPlayer()),
            new ModifierAction(nameof(ModifierFunctions.enableObjectTree),  ModifierFunctions.enableObjectTree, ModifierCompatibility.BeatmapObjectCompatible),
            new ModifierAction(nameof(ModifierFunctions.enableObjectOther),  ModifierFunctions.enableObjectOther, ModifierCompatibility.FullBeatmapCompatible),
            new ModifierAction(nameof(ModifierFunctions.enableObjectTreeOther),  ModifierFunctions.enableObjectTreeOther, ModifierCompatibility.BeatmapObjectCompatible),
            new ModifierAction(nameof(ModifierFunctions.enableObjectGroup),  ModifierFunctions.enableObjectGroup, ModifierCompatibility.FullBeatmapCompatible),

            // disable
            new ModifierAction(nameof(ModifierFunctions.disableObject),  ModifierFunctions.disableObject, ModifierCompatibility.BeatmapObjectCompatible),
            new ModifierAction(nameof(ModifierFunctions.disableObjectTree),  ModifierFunctions.disableObjectTree, ModifierCompatibility.BeatmapObjectCompatible),
            new ModifierAction(nameof(ModifierFunctions.disableObjectOther),  ModifierFunctions.disableObjectOther, ModifierCompatibility.BeatmapObjectCompatible),
            new ModifierAction(nameof(ModifierFunctions.disableObjectTreeOther),  ModifierFunctions.disableObjectTreeOther, ModifierCompatibility.BeatmapObjectCompatible),

            #endregion

            #region JSON

            new ModifierAction(nameof(ModifierFunctions.saveFloat),  ModifierFunctions.saveFloat, ModifierCompatibility.LevelControlCompatible),
            new ModifierAction(nameof(ModifierFunctions.saveString),  ModifierFunctions.saveString, ModifierCompatibility.LevelControlCompatible),
            new ModifierAction(nameof(ModifierFunctions.saveText),  ModifierFunctions.saveText, ModifierCompatibility.LevelControlCompatible),
            new ModifierAction(nameof(ModifierFunctions.saveVariable),  ModifierFunctions.saveVariable, ModifierCompatibility.LevelControlCompatible),
            new ModifierAction(nameof(ModifierFunctions.loadVariable),  ModifierFunctions.loadVariable, ModifierCompatibility.LevelControlCompatible),
            new ModifierAction(nameof(ModifierFunctions.loadVariableOther),  ModifierFunctions.loadVariableOther, ModifierCompatibility.LevelControlCompatible),

            #endregion

            #region Reactive

            // single
            new ModifierAction(nameof(ModifierFunctions.reactivePos),  ModifierFunctions.reactivePos, ModifierCompatibility.BeatmapObjectCompatible),
            new ModifierAction(nameof(ModifierFunctions.reactiveSca),  ModifierFunctions.reactiveSca, ModifierCompatibility.BeatmapObjectCompatible),
            new ModifierAction(nameof(ModifierFunctions.reactiveRot),  ModifierFunctions.reactiveRot, ModifierCompatibility.BeatmapObjectCompatible),
            new ModifierAction(nameof(ModifierFunctions.reactiveCol),  ModifierFunctions.reactiveCol, ModifierCompatibility.BeatmapObjectCompatible),
            new ModifierAction(nameof(ModifierFunctions.reactiveColLerp),  ModifierFunctions.reactiveColLerp, ModifierCompatibility.BeatmapObjectCompatible),

            // chain
            new ModifierAction(nameof(ModifierFunctions.reactivePosChain),  ModifierFunctions.reactivePosChain, ModifierCompatibility.BeatmapObjectCompatible),
            new ModifierAction(nameof(ModifierFunctions.reactiveScaChain),  ModifierFunctions.reactiveScaChain, ModifierCompatibility.BeatmapObjectCompatible),
            new ModifierAction(nameof(ModifierFunctions.reactiveRotChain),  ModifierFunctions.reactiveRotChain, ModifierCompatibility.BeatmapObjectCompatible),

            #endregion

            #region Events

            new ModifierAction(nameof(ModifierFunctions.eventOffset),  ModifierFunctions.eventOffset, ModifierCompatibility.LevelControlCompatible),
            new ModifierAction(nameof(ModifierFunctions.eventOffsetVariable),  ModifierFunctions.eventOffsetVariable, ModifierCompatibility.LevelControlCompatible),
            new ModifierAction(nameof(ModifierFunctions.eventOffsetMath),  ModifierFunctions.eventOffsetMath, ModifierCompatibility.LevelControlCompatible),
            new ModifierAction(nameof(ModifierFunctions.eventOffsetAnimate),  ModifierFunctions.eventOffsetAnimate, ModifierCompatibility.LevelControlCompatible),
            new ModifierAction(nameof(ModifierFunctions.eventOffsetCopyAxis),  ModifierFunctions.eventOffsetCopyAxis, ModifierCompatibility.BeatmapObjectCompatible),
            new ModifierAction(nameof(ModifierFunctions.vignetteTracksPlayer),  ModifierFunctions.vignetteTracksPlayer, ModifierCompatibility.LevelControlCompatible),
            new ModifierAction(nameof(ModifierFunctions.lensTracksPlayer),  ModifierFunctions.lensTracksPlayer, ModifierCompatibility.LevelControlCompatible),

            #endregion

            // todo: implement gradients and different color controls
            #region Color
            
            new ModifierAction(nameof(ModifierFunctions.actorFrameTexture),  ModifierFunctions.actorFrameTexture, ModifierCompatibility.BeatmapObjectCompatible),

            new ModifierAction(nameof(ModifierFunctions.setTheme),  ModifierFunctions.setTheme, ModifierCompatibility.LevelControlCompatible),
            new ModifierAction(nameof(ModifierFunctions.lerpTheme),  ModifierFunctions.lerpTheme, ModifierCompatibility.LevelControlCompatible),

            // color
            new ModifierAction(nameof(ModifierFunctions.addColor),  ModifierFunctions.addColor, ModifierCompatibility.BeatmapObjectCompatible),
            new ModifierAction(nameof(ModifierFunctions.addColorOther),  ModifierFunctions.addColorOther, ModifierCompatibility.FullBeatmapCompatible),
            new ModifierAction(nameof(ModifierFunctions.lerpColor),  ModifierFunctions.lerpColor, ModifierCompatibility.BeatmapObjectCompatible),
            new ModifierAction(nameof(ModifierFunctions.lerpColorOther),  ModifierFunctions.lerpColorOther, ModifierCompatibility.FullBeatmapCompatible),
            new ModifierAction(nameof(ModifierFunctions.addColorPlayerDistance),  ModifierFunctions.addColorPlayerDistance, ModifierCompatibility.BeatmapObjectCompatible),
            new ModifierAction(nameof(ModifierFunctions.lerpColorPlayerDistance),  ModifierFunctions.lerpColorPlayerDistance, ModifierCompatibility.BeatmapObjectCompatible),

            // opacity
            new ModifierAction("setAlpha",  ModifierFunctions.setOpacity, ModifierCompatibility.BeatmapObjectCompatible),
            new ModifierAction(nameof(ModifierFunctions.setOpacity),  ModifierFunctions.setOpacity, ModifierCompatibility.BeatmapObjectCompatible),
            new ModifierAction("setAlphaOther",  ModifierFunctions.setOpacityOther, ModifierCompatibility.BeatmapObjectCompatible),
            new ModifierAction(nameof(ModifierFunctions.setOpacityOther),  ModifierFunctions.setOpacityOther, ModifierCompatibility.FullBeatmapCompatible),

            // copy
            new ModifierAction(nameof(ModifierFunctions.copyColor),  ModifierFunctions.copyColor, ModifierCompatibility.BeatmapObjectCompatible),
            new ModifierAction(nameof(ModifierFunctions.copyColorOther),  ModifierFunctions.copyColorOther, ModifierCompatibility.FullBeatmapCompatible),
            new ModifierAction(nameof(ModifierFunctions.applyColorGroup),  ModifierFunctions.applyColorGroup, ModifierCompatibility.FullBeatmapCompatible),

            // hex code
            new ModifierAction(nameof(ModifierFunctions.setColorHex),  ModifierFunctions.setColorHex, ModifierCompatibility.BeatmapObjectCompatible),
            new ModifierAction(nameof(ModifierFunctions.setColorHexOther),  ModifierFunctions.setColorHexOther, ModifierCompatibility.FullBeatmapCompatible),

            // rgba
            new ModifierAction(nameof(ModifierFunctions.setColorRGBA),  ModifierFunctions.setColorRGBA, ModifierCompatibility.BeatmapObjectCompatible),
            new ModifierAction(nameof(ModifierFunctions.setColorRGBAOther),  ModifierFunctions.setColorRGBAOther, ModifierCompatibility.FullBeatmapCompatible),

            new ModifierAction(nameof(ModifierFunctions.animateColorKF),  ModifierFunctions.animateColorKF, ModifierCompatibility.BeatmapObjectCompatible.WithBackgroundObject()),
            new ModifierAction(nameof(ModifierFunctions.animateColorKFHex),  ModifierFunctions.animateColorKFHex, ModifierCompatibility.BeatmapObjectCompatible.WithBackgroundObject()),

            #endregion

            #region Shape
            
            new ModifierAction(nameof(ModifierFunctions.translateShape),  ModifierFunctions.translateShape, ModifierCompatibility.BeatmapObjectCompatible),
            new ModifierAction(nameof(ModifierFunctions.setShape),  ModifierFunctions.setShape, ModifierCompatibility.BeatmapObjectCompatible.WithBackgroundObject()),
            new ModifierAction(nameof(ModifierFunctions.setPolygonShape),  ModifierFunctions.setPolygonShape, ModifierCompatibility.BeatmapObjectCompatible),
            new ModifierAction(nameof(ModifierFunctions.setPolygonShapeOther),  ModifierFunctions.setPolygonShapeOther, ModifierCompatibility.BeatmapObjectCompatible),

            // image
            new ModifierAction(nameof(ModifierFunctions.setImage),  ModifierFunctions.setImage, ModifierCompatibility.BeatmapObjectCompatible),
            new ModifierAction(nameof(ModifierFunctions.setImageOther),  ModifierFunctions.setImageOther, ModifierCompatibility.BeatmapObjectCompatible),

            // text (pain)
            new ModifierAction(nameof(ModifierFunctions.setText),  ModifierFunctions.setText, ModifierCompatibility.BeatmapObjectCompatible),
            new ModifierAction(nameof(ModifierFunctions.setTextOther),  ModifierFunctions.setTextOther, ModifierCompatibility.BeatmapObjectCompatible),
            new ModifierAction(nameof(ModifierFunctions.addText),  ModifierFunctions.addText, ModifierCompatibility.BeatmapObjectCompatible),
            new ModifierAction(nameof(ModifierFunctions.addTextOther),  ModifierFunctions.addTextOther, ModifierCompatibility.BeatmapObjectCompatible),
            new ModifierAction(nameof(ModifierFunctions.removeText),  ModifierFunctions.removeText, ModifierCompatibility.BeatmapObjectCompatible),
            new ModifierAction(nameof(ModifierFunctions.removeTextOther),  ModifierFunctions.removeTextOther, ModifierCompatibility.BeatmapObjectCompatible),
            new ModifierAction(nameof(ModifierFunctions.removeTextAt),  ModifierFunctions.removeTextAt, ModifierCompatibility.BeatmapObjectCompatible),
            new ModifierAction(nameof(ModifierFunctions.removeTextOtherAt),  ModifierFunctions.removeTextOtherAt, ModifierCompatibility.BeatmapObjectCompatible),
            new ModifierAction(nameof(ModifierFunctions.formatText),  ModifierFunctions.formatText, ModifierCompatibility.BeatmapObjectCompatible),
            new ModifierAction(nameof(ModifierFunctions.textSequence),  ModifierFunctions.textSequence, ModifierCompatibility.BeatmapObjectCompatible),

            // modify shape
            new ModifierAction(nameof(ModifierFunctions.backgroundShape),  ModifierFunctions.backgroundShape, ModifierCompatibility.BeatmapObjectCompatible),
            new ModifierAction(nameof(ModifierFunctions.sphereShape),  ModifierFunctions.sphereShape, ModifierCompatibility.BeatmapObjectCompatible),

            #endregion

            #region Animation

            new ModifierAction(nameof(ModifierFunctions.animateObject),  ModifierFunctions.animateObject),
            new ModifierAction(nameof(ModifierFunctions.animateObjectOther),  ModifierFunctions.animateObjectOther, ModifierCompatibility.LevelControlCompatible),
            new ModifierAction(nameof(ModifierFunctions.animateObjectKF),  ModifierFunctions.animateObjectKF),
            new ModifierAction(nameof(ModifierFunctions.animateSignal),  ModifierFunctions.animateSignal, ModifierCompatibility.LevelControlCompatible),
            new ModifierAction(nameof(ModifierFunctions.animateSignalOther),  ModifierFunctions.animateSignalOther, ModifierCompatibility.LevelControlCompatible),

            new ModifierAction(nameof(ModifierFunctions.animateObjectMath),  ModifierFunctions.animateObjectMath),
            new ModifierAction(nameof(ModifierFunctions.animateObjectMathOther),  ModifierFunctions.animateObjectMathOther, ModifierCompatibility.LevelControlCompatible),
            new ModifierAction(nameof(ModifierFunctions.animateSignalMath),  ModifierFunctions.animateSignalMath, ModifierCompatibility.LevelControlCompatible),
            new ModifierAction(nameof(ModifierFunctions.animateSignalMathOther),  ModifierFunctions.animateSignalMathOther, ModifierCompatibility.LevelControlCompatible),

            new ModifierAction(nameof(ModifierFunctions.applyAnimation),  ModifierFunctions.applyAnimation, ModifierCompatibility.BeatmapObjectCompatible),
            new ModifierAction(nameof(ModifierFunctions.applyAnimationFrom),  ModifierFunctions.applyAnimationFrom, ModifierCompatibility.BeatmapObjectCompatible),
            new ModifierAction(nameof(ModifierFunctions.applyAnimationTo),  ModifierFunctions.applyAnimationTo, ModifierCompatibility.BeatmapObjectCompatible),
            new ModifierAction(nameof(ModifierFunctions.applyAnimationMath),  ModifierFunctions.applyAnimationMath, ModifierCompatibility.BeatmapObjectCompatible),
            new ModifierAction(nameof(ModifierFunctions.applyAnimationFromMath),  ModifierFunctions.applyAnimationFromMath, ModifierCompatibility.BeatmapObjectCompatible),
            new ModifierAction(nameof(ModifierFunctions.applyAnimationToMath),  ModifierFunctions.applyAnimationToMath, ModifierCompatibility.BeatmapObjectCompatible),

            new ModifierAction(nameof(ModifierFunctions.copyAxis),  ModifierFunctions.copyAxis),
            new ModifierAction(nameof(ModifierFunctions.copyAxisMath),  ModifierFunctions.copyAxisMath),
            new ModifierAction(nameof(ModifierFunctions.copyAxisGroup),  ModifierFunctions.copyAxisGroup),
            new ModifierAction(nameof(ModifierFunctions.copyPlayerAxis),  ModifierFunctions.copyPlayerAxis),

            new ModifierAction(nameof(ModifierFunctions.legacyTail),  ModifierFunctions.legacyTail, ModifierCompatibility.BeatmapObjectCompatible),

            new ModifierAction(nameof(ModifierFunctions.gravity),  ModifierFunctions.gravity),
            new ModifierAction(nameof(ModifierFunctions.gravityOther),  ModifierFunctions.gravityOther),

            #endregion

            #region Prefab

            new ModifierAction(nameof(ModifierFunctions.spawnPrefab),  ModifierFunctions.spawnPrefab, ModifierCompatibility.LevelControlCompatible),
            new ModifierAction(nameof(ModifierFunctions.spawnPrefabOffset),  ModifierFunctions.spawnPrefabOffset, ModifierCompatibility.LevelControlCompatible),
            new ModifierAction(nameof(ModifierFunctions.spawnPrefabOffsetOther),  ModifierFunctions.spawnPrefabOffsetOther, ModifierCompatibility.LevelControlCompatible),
            new ModifierAction(nameof(ModifierFunctions.spawnPrefabCopy),  ModifierFunctions.spawnPrefabCopy, ModifierCompatibility.LevelControlCompatible),
            new ModifierAction(nameof(ModifierFunctions.spawnMultiPrefab),  ModifierFunctions.spawnMultiPrefab, ModifierCompatibility.LevelControlCompatible),
            new ModifierAction(nameof(ModifierFunctions.spawnMultiPrefabOffset),  ModifierFunctions.spawnMultiPrefabOffset, ModifierCompatibility.LevelControlCompatible),
            new ModifierAction(nameof(ModifierFunctions.spawnMultiPrefabOffsetOther),  ModifierFunctions.spawnMultiPrefabOffsetOther, ModifierCompatibility.LevelControlCompatible),
            new ModifierAction(nameof(ModifierFunctions.spawnMultiPrefabCopy),  ModifierFunctions.spawnMultiPrefabCopy, ModifierCompatibility.LevelControlCompatible),
            new ModifierAction(nameof(ModifierFunctions.clearSpawnedPrefabs),  ModifierFunctions.clearSpawnedPrefabs, ModifierCompatibility.LevelControlCompatible),
            new ModifierAction(nameof(ModifierFunctions.setPrefabTime),  ModifierFunctions.setPrefabTime, ModifierCompatibility.PrefabObjectCompatible),
            new ModifierAction(nameof(ModifierFunctions.enablePrefab),  ModifierFunctions.enablePrefab, ModifierCompatibility.FullBeatmapCompatible),
            new ModifierAction(nameof(ModifierFunctions.updatePrefab),  ModifierFunctions.updatePrefab, ModifierCompatibility.FullBeatmapCompatible),

            #endregion

            #region Ranking

            new ModifierAction(nameof(ModifierFunctions.saveLevelRank), ModifierFunctions.saveLevelRank, ModifierCompatibility.LevelControlCompatible),
            new ModifierAction(nameof(ModifierFunctions.unlockAchievement), ModifierFunctions.unlockAchievement, ModifierCompatibility.LevelControlCompatible),
            new ModifierAction(nameof(ModifierFunctions.lockAchievement), ModifierFunctions.lockAchievement, ModifierCompatibility.LevelControlCompatible),
            new ModifierAction(nameof(ModifierFunctions.getAchievementUnlocked), ModifierFunctions.getAchievementUnlocked),

            new ModifierAction(nameof(ModifierFunctions.clearHits), ModifierFunctions.clearHits, ModifierCompatibility.LevelControlCompatible),
            new ModifierAction(nameof(ModifierFunctions.addHit), ModifierFunctions.addHit, ModifierCompatibility.LevelControlCompatible),
            new ModifierAction(nameof(ModifierFunctions.subHit), ModifierFunctions.subHit, ModifierCompatibility.LevelControlCompatible),
            new ModifierAction(nameof(ModifierFunctions.clearDeaths), ModifierFunctions.clearDeaths, ModifierCompatibility.LevelControlCompatible),
            new ModifierAction(nameof(ModifierFunctions.addDeath), ModifierFunctions.addDeath, ModifierCompatibility.LevelControlCompatible),
            new ModifierAction(nameof(ModifierFunctions.subDeath), ModifierFunctions.subDeath, ModifierCompatibility.LevelControlCompatible),

            new ModifierAction(nameof(ModifierFunctions.getHitCount), ModifierFunctions.getHitCount),
            new ModifierAction(nameof(ModifierFunctions.getDeathCount), ModifierFunctions.getDeathCount),

            #endregion

            #region Updates

            // update
            new ModifierAction(nameof(ModifierFunctions.reinitLevel),  ModifierFunctions.reinitLevel, ModifierCompatibility.LevelControlCompatible),
            new ModifierAction(nameof(ModifierFunctions.updateObjects),  ModifierFunctions.updateObjects, ModifierCompatibility.LevelControlCompatible),
            new ModifierAction(nameof(ModifierFunctions.updateObject),  ModifierFunctions.updateObject, ModifierCompatibility.FullBeatmapCompatible),
            new ModifierAction(nameof(ModifierFunctions.updateObjectOther),  ModifierFunctions.updateObjectOther, ModifierCompatibility.FullBeatmapCompatible),

            // parent
            new ModifierAction(nameof(ModifierFunctions.setParent),  ModifierFunctions.setParent, ModifierCompatibility.BeatmapObjectCompatible.WithPrefabObject(true)),
            new ModifierAction(nameof(ModifierFunctions.setParentOther),  ModifierFunctions.setParentOther, ModifierCompatibility.BeatmapObjectCompatible.WithPrefabObject(true)),
            new ModifierAction(nameof(ModifierFunctions.detachParent),  ModifierFunctions.detachParent, ModifierCompatibility.BeatmapObjectCompatible.WithPrefabObject(true)),
            new ModifierAction(nameof(ModifierFunctions.detachParentOther),  ModifierFunctions.detachParentOther, ModifierCompatibility.BeatmapObjectCompatible.WithPrefabObject(true)),

            new ModifierAction(nameof(ModifierFunctions.setSeed),  ModifierFunctions.setSeed, ModifierCompatibility.BeatmapObjectCompatible.WithPrefabObject(true)),

            #endregion

            #region Physics

            // collision
            new ModifierAction(nameof(ModifierFunctions.setCollision),  ModifierFunctions.setCollision, ModifierCompatibility.BeatmapObjectCompatible),
            new ModifierAction(nameof(ModifierFunctions.setCollisionOther),  ModifierFunctions.setCollisionOther, ModifierCompatibility.FullBeatmapCompatible),

            #endregion

            #region Checkpoints

            new ModifierAction(nameof(ModifierFunctions.createCheckpoint),  ModifierFunctions.createCheckpoint, ModifierCompatibility.LevelControlCompatible),
            new ModifierAction(nameof(ModifierFunctions.resetCheckpoint),  ModifierFunctions.resetCheckpoint, ModifierCompatibility.LevelControlCompatible),

            #endregion

            #region Interfaces

            new ModifierAction(nameof(ModifierFunctions.loadInterface),  ModifierFunctions.loadInterface, ModifierCompatibility.LevelControlCompatible),
            new ModifierAction(nameof(ModifierFunctions.exitInterface),  ModifierFunctions.exitInterface, ModifierCompatibility.LevelControlCompatible),
            new ModifierAction(nameof(ModifierFunctions.quitToMenu),  ModifierFunctions.quitToMenu, ModifierCompatibility.LevelControlCompatible),
            new ModifierAction(nameof(ModifierFunctions.quitToArcade),  ModifierFunctions.quitToArcade, ModifierCompatibility.LevelControlCompatible),
            new ModifierAction(nameof(ModifierFunctions.pauseLevel),  ModifierFunctions.pauseLevel, ModifierCompatibility.LevelControlCompatible),

            #endregion

            #region Misc

            new ModifierAction(nameof(ModifierFunctions.setBGActive),  ModifierFunctions.setBGActive, ModifierCompatibility.LevelControlCompatible),

            // activation
            new ModifierAction(nameof(ModifierFunctions.signalModifier),  ModifierFunctions.signalModifier, ModifierCompatibility.LevelControlCompatible),
            new ModifierAction(nameof(ModifierFunctions.activateModifier),  ModifierFunctions.activateModifier, ModifierCompatibility.LevelControlCompatible),

            new ModifierAction(nameof(ModifierFunctions.editorNotify),  ModifierFunctions.editorNotify, ModifierCompatibility.LevelControlCompatible),

            // external
            new ModifierAction(nameof(ModifierFunctions.setWindowTitle),  ModifierFunctions.setWindowTitle, ModifierCompatibility.LevelControlCompatible),
            new ModifierAction(nameof(ModifierFunctions.setDiscordStatus),  ModifierFunctions.setDiscordStatus, ModifierCompatibility.LevelControlCompatible),

            new ModifierAction(nameof(ModifierFunctions.callModifierBlock),  ModifierFunctions.callModifierBlock, ModifierCompatibility.LevelControlCompatible),

            #endregion

            #region Player Only
            
            new ModifierAction(nameof(ModifierFunctions.setCustomObjectActive),  ModifierFunctions.setCustomObjectActive, ModifierCompatibility.FullPlayerCompatible),
            new ModifierAction(nameof(ModifierFunctions.setCustomObjectIdle),  ModifierFunctions.setCustomObjectIdle, ModifierCompatibility.FullPlayerCompatible),
            new ModifierAction(nameof(ModifierFunctions.setIdleAnimation),  ModifierFunctions.setIdleAnimation, ModifierCompatibility.FullPlayerCompatible),
            new ModifierAction(nameof(ModifierFunctions.playAnimation),  ModifierFunctions.playAnimation, ModifierCompatibility.FullPlayerCompatible),
            new ModifierAction(nameof(ModifierFunctions.kill),  ModifierFunctions.kill, ModifierCompatibility.PAPlayerCompatible),
            new ModifierAction(nameof(ModifierFunctions.hit),  ModifierFunctions.hit, ModifierCompatibility.PAPlayerCompatible),
            new ModifierAction(nameof(ModifierFunctions.boost),  ModifierFunctions.boost, ModifierCompatibility.PAPlayerCompatible),
            new ModifierAction(nameof(ModifierFunctions.shoot),  ModifierFunctions.shoot, ModifierCompatibility.PAPlayerCompatible),
            new ModifierAction(nameof(ModifierFunctions.pulse),  ModifierFunctions.pulse, ModifierCompatibility.PAPlayerCompatible),
            new ModifierAction(nameof(ModifierFunctions.jump),  ModifierFunctions.jump, ModifierCompatibility.PAPlayerCompatible),
            new ModifierAction(nameof(ModifierFunctions.getHealth),  ModifierFunctions.getHealth, ModifierCompatibility.FullPlayerCompatible),
            new ModifierAction(nameof(ModifierFunctions.getLives),  ModifierFunctions.getLives, ModifierCompatibility.FullPlayerCompatible),
            new ModifierAction(nameof(ModifierFunctions.getMaxHealth),  ModifierFunctions.getMaxHealth, ModifierCompatibility.FullPlayerCompatible),
            new ModifierAction(nameof(ModifierFunctions.getMaxLives),  ModifierFunctions.getMaxLives, ModifierCompatibility.FullPlayerCompatible),
            new ModifierAction(nameof(ModifierFunctions.getIndex),  ModifierFunctions.getIndex, ModifierCompatibility.FullPlayerCompatible),
            new ModifierAction(nameof(ModifierFunctions.getMove),  ModifierFunctions.getMove, ModifierCompatibility.FullPlayerCompatible),
            new ModifierAction(nameof(ModifierFunctions.getMoveX),  ModifierFunctions.getMoveX, ModifierCompatibility.FullPlayerCompatible),
            new ModifierAction(nameof(ModifierFunctions.getMoveY),  ModifierFunctions.getMoveY, ModifierCompatibility.FullPlayerCompatible),

            #endregion

            // dev only (story mode)
            #region Dev Only

            new ModifierAction(nameof(ModifierFunctions.loadSceneDEVONLY),  ModifierFunctions.loadSceneDEVONLY, ModifierCompatibility.LevelControlCompatible.WithStoryOnly()),
            new ModifierAction(nameof(ModifierFunctions.loadStoryLevelDEVONLY),  ModifierFunctions.loadStoryLevelDEVONLY, ModifierCompatibility.LevelControlCompatible.WithStoryOnly()),
            new ModifierAction(nameof(ModifierFunctions.storySaveBoolDEVONLY),  ModifierFunctions.storySaveBoolDEVONLY, ModifierCompatibility.LevelControlCompatible.WithStoryOnly()),
            new ModifierAction(nameof(ModifierFunctions.storySaveIntDEVONLY),  ModifierFunctions.storySaveIntDEVONLY, ModifierCompatibility.LevelControlCompatible.WithStoryOnly()),
            new ModifierAction(nameof(ModifierFunctions.storySaveFloatDEVONLY),  ModifierFunctions.storySaveFloatDEVONLY, ModifierCompatibility.LevelControlCompatible.WithStoryOnly()),
            new ModifierAction(nameof(ModifierFunctions.storySaveStringDEVONLY),  ModifierFunctions.storySaveStringDEVONLY, ModifierCompatibility.LevelControlCompatible.WithStoryOnly()),
            new ModifierAction(nameof(ModifierFunctions.storySaveIntVariableDEVONLY),  ModifierFunctions.storySaveIntVariableDEVONLY, ModifierCompatibility.LevelControlCompatible.WithStoryOnly()),
            new ModifierAction(nameof(ModifierFunctions.getStorySaveBoolDEVONLY),  ModifierFunctions.getStorySaveBoolDEVONLY),
            new ModifierAction(nameof(ModifierFunctions.getStorySaveIntDEVONLY),  ModifierFunctions.getStorySaveIntDEVONLY),
            new ModifierAction(nameof(ModifierFunctions.getStorySaveFloatDEVONLY),  ModifierFunctions.getStorySaveFloatDEVONLY),
            new ModifierAction(nameof(ModifierFunctions.getStorySaveStringDEVONLY),  ModifierFunctions.getStorySaveStringDEVONLY),
            new ModifierAction(nameof(ModifierFunctions.exampleEnableDEVONLY),  ModifierFunctions.exampleEnableDEVONLY, ModifierCompatibility.LevelControlCompatible.WithStoryOnly()),
            new ModifierAction(nameof(ModifierFunctions.exampleSayDEVONLY),  ModifierFunctions.exampleSayDEVONLY, ModifierCompatibility.LevelControlCompatible.WithStoryOnly()),

            #endregion
        };

        public static ModifierInactive[] inactives = new ModifierInactive[]
        {
            #region Actions

            #region Audio

            new ModifierInactive(nameof(ModifierFunctions.playSound),
                (modifier, reference, variables) =>
                {
                    if (!modifier.constant || !modifier.TryGetResult(out AudioSource cache) || !cache)
                        return;

                    cache.Pause();
                }),
            new ModifierInactive(nameof(ModifierFunctions.playOnlineSound),
                (modifier, reference, variables) =>
                {
                    if (!modifier.constant || !modifier.TryGetResult(out AudioSource cache) || !cache)
                        return;

                    cache.Pause();
                }),
            new ModifierInactive(nameof(ModifierFunctions.playDefaultSound),
                (modifier, reference, variables) =>
                {
                    if (!modifier.constant || !modifier.TryGetResult(out AudioSource cache) || !cache)
                        return;

                    cache.Pause();
                }),
            new ModifierInactive(nameof(ModifierFunctions.loadSoundAsset),
                (modifier, reference, variables) =>
                {
                    if (!modifier.constant || !modifier.TryGetResult(out AudioSource cache) || !cache)
                        return;

                    cache.Pause();
                }),

            #endregion

            #region Component

            new ModifierInactive(nameof(ModifierFunctions.blur),
                (modifier, reference, variables) =>
                {
                    if (reference is BeatmapObject beatmapObject && beatmapObject.objectType != BeatmapObject.ObjectType.Empty &&
                        beatmapObject.runtimeObject is RTBeatmapObject runtimeObject && runtimeObject.visualObject.renderer && runtimeObject.visualObject is SolidObject solidObject &&
                        modifier.GetBool(2, false, variables))
                    {
                        runtimeObject.visualObject.renderer.material = LegacyResources.objectMaterial;
                        solidObject.material = runtimeObject.visualObject.renderer.material;
                        modifier.Result = default;
                    }
                }, ModifierCompatibility.BeatmapObjectCompatible
            ),
            new ModifierInactive(nameof(ModifierFunctions.blurOther),
                (modifier, reference, variables) =>
                {
                    if (reference is BeatmapObject beatmapObject && beatmapObject.objectType != BeatmapObject.ObjectType.Empty &&
                        beatmapObject.runtimeObject is RTBeatmapObject runtimeObject && runtimeObject.visualObject.renderer && runtimeObject.visualObject is SolidObject solidObject &&
                        modifier.GetBool(2, false, variables))
                    {
                        runtimeObject.visualObject.renderer.material = LegacyResources.objectMaterial;
                        solidObject.material = runtimeObject.visualObject.renderer.material;
                        modifier.Result = default;
                    }
                }, ModifierCompatibility.BeatmapObjectCompatible
            ),
            new ModifierInactive(nameof(ModifierFunctions.blurVariable),
                (modifier, reference, variables) =>
                {
                    if (reference is BeatmapObject beatmapObject && beatmapObject.objectType != BeatmapObject.ObjectType.Empty &&
                        beatmapObject.runtimeObject is RTBeatmapObject runtimeObject && runtimeObject.visualObject.renderer && runtimeObject.visualObject is SolidObject solidObject &&
                        modifier.GetBool(1, false, variables))
                    {
                        runtimeObject.visualObject.renderer.material = LegacyResources.objectMaterial;
                        solidObject.material = runtimeObject.visualObject.renderer.material;
                        modifier.Result = default;
                    }
                }, ModifierCompatibility.BeatmapObjectCompatible
            ),
            new ModifierInactive(nameof(ModifierFunctions.blurVariableOther),
                (modifier, reference, variables) =>
                {
                    if (reference is BeatmapObject beatmapObject && beatmapObject.objectType != BeatmapObject.ObjectType.Empty &&
                        beatmapObject.runtimeObject is RTBeatmapObject runtimeObject && runtimeObject.visualObject.renderer && runtimeObject.visualObject is SolidObject solidObject &&
                        modifier.GetBool(2, false, variables))
                    {
                        runtimeObject.visualObject.renderer.material = LegacyResources.objectMaterial;
                        solidObject.material = runtimeObject.visualObject.renderer.material;
                        modifier.Result = default;
                    }
                }, ModifierCompatibility.BeatmapObjectCompatible
            ),
            new ModifierInactive(nameof(ModifierFunctions.blurColored),
                (modifier, reference, variables) =>
                {
                    if (reference is BeatmapObject beatmapObject && beatmapObject.objectType != BeatmapObject.ObjectType.Empty &&
                        beatmapObject.runtimeObject is RTBeatmapObject runtimeObject && runtimeObject.visualObject.renderer && runtimeObject.visualObject is SolidObject solidObject &&
                        modifier.GetBool(2, false, variables))
                    {
                        runtimeObject.visualObject.renderer.material = LegacyResources.objectMaterial;
                        solidObject.material = runtimeObject.visualObject.renderer.material;
                        modifier.Result = default;
                    }
                }, ModifierCompatibility.BeatmapObjectCompatible
            ),
            new ModifierInactive(nameof(ModifierFunctions.blurColoredOther),
                (modifier, reference, variables) =>
                {
                    if (reference is BeatmapObject beatmapObject && beatmapObject.objectType != BeatmapObject.ObjectType.Empty &&
                        beatmapObject.runtimeObject is RTBeatmapObject runtimeObject && runtimeObject.visualObject.renderer && runtimeObject.visualObject is SolidObject solidObject &&
                        modifier.GetBool(2, false, variables))
                    {
                        runtimeObject.visualObject.renderer.material = LegacyResources.objectMaterial;
                        solidObject.material = runtimeObject.visualObject.renderer.material;
                        modifier.Result = default;
                    }
                }, ModifierCompatibility.BeatmapObjectCompatible
            ),
            new ModifierInactive(nameof(ModifierFunctions.particleSystem),
                (modifier, reference, variables) =>
                {
                    if (modifier.Result is ParticleSystem particleSystem && particleSystem)
                    {
                        var emission = particleSystem.emission;
                        emission.enabled = false;
                    }
                }, ModifierCompatibility.BeatmapObjectCompatible
            ),
            new ModifierInactive(nameof(ModifierFunctions.particleSystemHex),
                (modifier, reference, variables) =>
                {
                    if (modifier.Result is ParticleSystem particleSystem && particleSystem)
                    {
                        var emission = particleSystem.emission;
                        emission.enabled = false;
                    }
                }, ModifierCompatibility.BeatmapObjectCompatible
            ),
            new ModifierInactive(nameof(ModifierFunctions.trailRenderer),
                (modifier, reference, variables) =>
                {
                    if (reference is BeatmapObject beatmapObject && beatmapObject.trailRenderer)
                        beatmapObject.trailRenderer.emitting = false;
                }, ModifierCompatibility.BeatmapObjectCompatible
            ),
            new ModifierInactive(nameof(ModifierFunctions.trailRendererHex),
                (modifier, reference, variables) =>
                {
                    if (reference is BeatmapObject beatmapObject && beatmapObject.trailRenderer)
                        beatmapObject.trailRenderer.emitting = false;
                }, ModifierCompatibility.BeatmapObjectCompatible
            ),

            #endregion

            #region Variables
            
            new ModifierInactive(nameof(ModifierFunctions.storeLocalVariables),
                (modifier, reference, variables) =>
                {
                    modifier.Result = null;
                }
            ),

            #endregion

            #region Enable

            new ModifierInactive(nameof(ModifierFunctions.enableObject),
                (modifier, reference, variables) =>
                {
                    if (!modifier.GetBool(1, false, variables))
                        return;

                    if (reference is not IPrefabable prefabable)
                        return;

                    if (prefabable.GetRuntimeObject() is ICustomActivatable activatable)
                        activatable.SetCustomActive(false);
                }, ModifierCompatibility.BeatmapObjectCompatible.WithPAPlayer()
            ),
            new ModifierInactive(nameof(ModifierFunctions.enableObjectOther),
                (modifier, reference, variables) =>
                {
                    if (!modifier.GetBool(1, false, variables))
                        return;

                    if (reference is not IPrefabable prefabable)
                        return;

                    var prefabables = GameData.Current.FindPrefabablesWithTag(modifier, prefabable, modifier.GetValue(0, variables));

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
            new ModifierInactive(nameof(ModifierFunctions.enableObjectTree),
                (modifier, reference, variables) =>
                {
                    if (modifier.GetValue(0) == "0")
                        modifier.SetValue(0, "False");

                    if (!modifier.GetBool(1, false, variables))
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
            new ModifierInactive(nameof(ModifierFunctions.enableObjectTreeOther),
                (modifier, reference, variables) =>
                {
                    if (!modifier.GetBool(2, false, variables))
                    {
                        modifier.Result = null;
                        return;
                    }

                    if (reference is not IPrefabable prefabable)
                        return;

                    if (modifier.Result == null)
                    {
                        var beatmapObjects = GameData.Current.FindObjectsWithTag(modifier, prefabable, modifier.GetValue(1, variables));

                        var resultList = new List<BeatmapObject>();
                        foreach (var bm in beatmapObjects)
                        {
                            var beatmapObject = modifier.GetBool(0, true, variables) ? bm : bm.GetParentChain().Last();
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

            new ModifierInactive(nameof(ModifierFunctions.disableObject),
                (modifier, reference, variables) =>
                {
                    if (!modifier.GetBool(1, false, variables))
                        return;

                    if (reference is not IPrefabable prefabable)
                        return;

                    if (prefabable.GetRuntimeObject() is ICustomActivatable activatable)
                        activatable.SetCustomActive(true);
                }, ModifierCompatibility.BeatmapObjectCompatible
            ),
            new ModifierInactive(nameof(ModifierFunctions.disableObjectOther),
                (modifier, reference, variables) =>
                {
                    if (!modifier.GetBool(1, false, variables))
                        return;

                    if (reference is not IPrefabable prefabable)
                        return;

                    var prefabables = GameData.Current.FindPrefabablesWithTag(modifier, prefabable, modifier.GetValue(0, variables));

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
            new ModifierInactive(nameof(ModifierFunctions.disableObjectTree),
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
            new ModifierInactive(nameof(ModifierFunctions.disableObjectTreeOther),
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
                        var beatmapObjects = GameData.Current.FindObjectsWithTag(modifier, prefabable, modifier.GetValue(1, variables));

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

            new ModifierInactive(nameof(ModifierFunctions.reactivePosChain),
                (modifier, reference, variables) =>
                {
                    if (reference is IReactive reactive)
                        reactive.ReactivePositionOffset = Vector3.zero;
                }
            ),
            new ModifierInactive(nameof(ModifierFunctions.reactiveScaChain),
                (modifier, reference, variables) =>
                {
                    if (reference is IReactive reactive)
                        reactive.ReactiveScaleOffset = Vector3.zero;
                }
            ),
            new ModifierInactive(nameof(ModifierFunctions.reactiveRotChain),
                (modifier, reference, variables) =>
                {
                    if (reference is IReactive reactive)
                        reactive.ReactiveRotationOffset = 0f;
                }
            ),

            #endregion

            #region Color

            new ModifierInactive(nameof(ModifierFunctions.animateColorKF),
                (modifier, reference, variables) =>
                {
                    modifier.Result = null;
                }, ModifierCompatibility.BeatmapObjectCompatible.WithBackgroundObject()
            ),
            new ModifierInactive(nameof(ModifierFunctions.animateColorKFHex),
                (modifier, reference, variables) =>
                {
                    modifier.Result = null;
                }, ModifierCompatibility.BeatmapObjectCompatible.WithBackgroundObject()
            ),

            #endregion

            #region Shape

            new ModifierInactive(nameof(ModifierFunctions.setText),
                (modifier, reference, variables) =>
                {
                    if (modifier.constant && reference is BeatmapObject beatmapObject && beatmapObject.ShapeType == ShapeType.Text && beatmapObject.runtimeObject && beatmapObject.runtimeObject.visualObject &&
                        beatmapObject.runtimeObject.visualObject is TextObject textObject)
                        textObject.text = beatmapObject.text;
                }, ModifierCompatibility.BeatmapObjectCompatible
            ),
            new ModifierInactive(nameof(ModifierFunctions.setTextOther),
                (modifier, reference, variables) =>
                {
                    if (reference is not IPrefabable prefabable)
                        return;

                    var list = GameData.Current.FindObjectsWithTag(modifier, prefabable, modifier.GetValue(1, variables));

                    if (modifier.constant && !list.IsEmpty())
                        foreach (var bm in list)
                            if (bm.ShapeType == ShapeType.Text && bm.runtimeObject && bm.runtimeObject.visualObject &&
                                bm.runtimeObject.visualObject is TextObject textObject)
                                textObject.text = bm.text;
                }, ModifierCompatibility.BeatmapObjectCompatible
            ),
            new ModifierInactive(nameof(ModifierFunctions.textSequence),
                (modifier, reference, variables) =>
                {
                    modifier.setTimer = false;
                }, ModifierCompatibility.BeatmapObjectCompatible
            ),

            #endregion

            #region Animation

            new ModifierInactive(nameof(ModifierFunctions.animateSignal),
                (modifier, reference, variables) =>
                {
                    if (modifier.constant || !modifier.GetBool(!modifier.Name.Contains("Other") ? 9 : 10, true, variables))
                        return;

                    if (reference is not IPrefabable prefabable)
                        return;

                    int groupIndex = !modifier.Name.Contains("Other") ? 7 : 8;
                    var modifyables = GameData.Current.FindModifyables(modifier, prefabable, modifier.GetValue(groupIndex, variables));

                    foreach (var modifyable in modifyables)
                    {
                        if (!modifyable.Modifiers.IsEmpty() && modifyable.Modifiers.TryFind(x => x.Name == "requireSignal" && x.type == Modifier.Type.Trigger, out Modifier m))
                            m.Result = null;
                    }
                }
            ),
            new ModifierInactive(nameof(ModifierFunctions.animateSignalOther),
                (modifier, reference, variables) =>
                {
                    if (modifier.constant || !modifier.GetBool(!modifier.Name.Contains("Other") ? 9 : 10, true, variables))
                        return;

                    if (reference is not IPrefabable prefabable)
                        return;

                    int groupIndex = !modifier.Name.Contains("Other") ? 7 : 8;
                    var modifyables = GameData.Current.FindModifyables(modifier, prefabable, modifier.GetValue(groupIndex, variables));

                    foreach (var modifyable in modifyables)
                    {
                        if (!modifyable.Modifiers.IsEmpty() && modifyable.Modifiers.TryFind(x => x.Name == "requireSignal" && x.type == Modifier.Type.Trigger, out Modifier m))
                            m.Result = null;
                    }
                }
            ),
            new ModifierInactive(nameof(ModifierFunctions.applyAnimation),
                (modifier, reference, variables) =>
                {
                    modifier.Result = null;
                }, ModifierCompatibility.BeatmapObjectCompatible
            ),
            new ModifierInactive(nameof(ModifierFunctions.applyAnimationFrom),
                (modifier, reference, variables) =>
                {
                    modifier.Result = null;
                }, ModifierCompatibility.BeatmapObjectCompatible
            ),
            new ModifierInactive(nameof(ModifierFunctions.applyAnimationTo),
                (modifier, reference, variables) =>
                {
                    modifier.Result = null;
                }, ModifierCompatibility.BeatmapObjectCompatible
            ),
            new ModifierInactive(nameof(ModifierFunctions.copyAxis),
                (modifier, reference, variables) =>
                {
                    modifier.Result = null;
                }
            ),
            new ModifierInactive(nameof(ModifierFunctions.copyAxisMath),
                (modifier, reference, variables) =>
                {
                    modifier.Result = null;
                }
            ),
            new ModifierInactive(nameof(ModifierFunctions.copyAxisGroup),
                (modifier, reference, variables) =>
                {
                    modifier.Result = null;
                }
            ),
            new ModifierInactive(nameof(ModifierFunctions.gravity),
                (modifier, reference, variables) =>
                {
                    modifier.Result = null;
                }
            ),
            new ModifierInactive(nameof(ModifierFunctions.gravityOther),
                (modifier, reference, variables) =>
                {
                    modifier.Result = null;
                }
            ),

            #endregion

            #region Prefab

            new ModifierInactive(nameof(ModifierFunctions.spawnPrefab),
                (modifier, reference, variables) =>
                {
                    // value 9 is permanent

                    if (modifier.Result is PrefabObject prefabObject && !modifier.GetBool(9, false, variables))
                    {
                        RTLevelBase runtimeLevel = reference is PrefabObject p && p.runtimeObject ? p.runtimeObject : reference.GetParentRuntime();
                        runtimeLevel?.UpdatePrefab(prefabObject, false);

                        GameData.Current.prefabObjects.RemoveAll(x => x.fromModifier && x.id == prefabObject.id);

                        modifier.Result = null;
                    }
                }
            ),
            new ModifierInactive(nameof(ModifierFunctions.spawnPrefabOffset),
                (modifier, reference, variables) =>
                {
                    // value 9 is permanent

                    if (modifier.Result is PrefabObject prefabObject && !modifier.GetBool(9, false, variables))
                    {
                        RTLevelBase runtimeLevel = reference is PrefabObject p && p.runtimeObject ? p.runtimeObject : reference.GetParentRuntime();
                        runtimeLevel?.UpdatePrefab(prefabObject, false);

                        GameData.Current.prefabObjects.RemoveAll(x => x.fromModifier && x.id == prefabObject.id);

                        modifier.Result = null;
                    }
                }
            ),
            new ModifierInactive(nameof(ModifierFunctions.spawnPrefabOffsetOther),
                (modifier, reference, variables) =>
                {
                    // value 9 is permanent

                    if (modifier.Result is PrefabObject prefabObject && !modifier.GetBool(9, false, variables))
                    {
                        RTLevelBase runtimeLevel = reference is PrefabObject p && p.runtimeObject ? p.runtimeObject : reference.GetParentRuntime();
                        runtimeLevel?.UpdatePrefab(prefabObject, false);

                        GameData.Current.prefabObjects.RemoveAll(x => x.fromModifier && x.id == prefabObject.id);

                        modifier.Result = null;
                    }
                }
            ),
            new ModifierInactive(nameof(ModifierFunctions.spawnPrefabCopy),
                (modifier, reference, variables) =>
                {
                    // value 5 is permanent

                    if (modifier.Result is PrefabObject prefabObject && !modifier.GetBool(5, false, variables))
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

            new ModifierInactive(nameof(ModifierFunctions.setCustomObjectActive),
                (modifier, reference, variables) =>
                {
                    if (modifier.GetBool(2, true, variables) && reference is PAPlayer player && player.RuntimePlayer.customObjects.TryFind(x => x.id == modifier.GetValue(1, variables), out RTPlayer.RTCustomPlayerObject customObject))
                        customObject.active = !modifier.GetBool(0, false, variables);
                }, ModifierCompatibility.PAPlayerCompatible),

            #endregion

            #region Misc

            new ModifierInactive(nameof(ModifierFunctions.signalModifier),
                (modifier, reference, variables) =>
                {
                    if (reference is not IPrefabable prefabable)
                        return;

                    var modifyables = GameData.Current.FindModifyables(modifier, prefabable, modifier.GetValue(1, variables));

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

            new ModifierInactive(nameof(ModifierFunctions.mouseOverSignalModifier),
                (modifier, reference, variables) =>
                {
                    if (reference is not IPrefabable prefabable)
                        return;

                    var modifyables = GameData.Current.FindModifyables(modifier, prefabable, modifier.GetValue(1, variables));

                    foreach (var modifyable in modifyables)
                    {
                        if (!modifyable.Modifiers.IsEmpty() && modifyable.Modifiers.TryFind(x => x.Name == "requireSignal" && x.type == Modifier.Type.Trigger, out Modifier m))
                            m.Result = null;
                    }
                }, ModifierCompatibility.BeatmapObjectCompatible
            ),

            #endregion

            #region Random
            
            new ModifierInactive(nameof(ModifierFunctions.randomEquals),
                (modifier, reference, variables) =>
                {
                    modifier.Result = null;
                }
            ),
            new ModifierInactive(nameof(ModifierFunctions.randomLesser),
                (modifier, reference, variables) =>
                {
                    modifier.Result = null;
                }
            ),
            new ModifierInactive(nameof(ModifierFunctions.randomGreater),
                (modifier, reference, variables) =>
                {
                    modifier.Result = null;
                }
            ),

            #endregion

            #region Misc
            
            new ModifierInactive(nameof(ModifierFunctions.await),
                (modifier, reference, variables) =>
                {
                    modifier.Result = default;
                }),
            new ModifierInactive(nameof(ModifierFunctions.awaitCounter),
                (modifier, reference, variables) =>
                {
                    modifier.Result = default;
                }),
            new ModifierInactive(nameof(ModifierFunctions.objectSpawned),
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
            nameof(ModifierFunctions.onPlayerHit) => 1,
            nameof(ModifierFunctions.onPlayerDeath) => 2,
            nameof(ModifierFunctions.onLevelStart) => 3,
            nameof(ModifierFunctions.onLevelRestart) => 4,
            nameof(ModifierFunctions.onLevelRewind) => 5,
            _ => -1,
        };
        
        public static int GetLevelActionType(string key) => key switch
        {
            "vnInk" => 0,
            "vnTimeline" => 1,
            "playerBubble" => 2,
            nameof(ModifierFunctions.playerMoveAll) => 3,
            nameof(ModifierFunctions.playerLockBoostAll) => 4,
            nameof(ModifierFunctions.playerLockXAll) => 5,
            nameof(ModifierFunctions.playerLockYAll) => 6,
            "bgSpin" => 7,
            "bgMove" => 8,
            nameof(ModifierFunctions.playerBoostAll) => 9,
            nameof(ModifierFunctions.setMusicTime) => 10,
            nameof(ModifierFunctions.setPitch) => 11,
            _ => -1,
        };

        public static string GetLevelTriggerName(int type) => type switch
        {
            0 => "timeInRange",
            1 => nameof(ModifierFunctions.onPlayerHit),
            2 => nameof(ModifierFunctions.onPlayerDeath),
            3 => nameof(ModifierFunctions.onLevelStart),
            4 => nameof(ModifierFunctions.onLevelRestart),
            5 => nameof(ModifierFunctions.onLevelRewind),
            _ => string.Empty,
        };

        public static string GetLevelActionName(int type) => type switch
        {
            0 => "vnInk",
            1 => "vnTimeline",
            2 => "playerBubble",
            3 => nameof(ModifierFunctions.playerMoveAll),
            4 => nameof(ModifierFunctions.playerLockBoostAll),
            5 => nameof(ModifierFunctions.playerLockXAll),
            6 => nameof(ModifierFunctions.playerLockYAll),
            7 => "bgSpin",
            8 => "bgMove",
            9 => nameof(ModifierFunctions.playerBoostAll),
            10 => nameof(ModifierFunctions.setMusicTime),
            11 => nameof(ModifierFunctions.setPitch),
            _ => string.Empty,
        };

        #endregion

        #endregion

        #region Internal Functions

        public static bool IsGroupModifier(string name) =>
            name == nameof(ModifierFunctions.setParent) ||
            name == nameof(ModifierFunctions.objectCollide) ||
            name == nameof(ModifierFunctions.axisEquals) ||
            name == nameof(ModifierFunctions.axisGreater) ||
            name == nameof(ModifierFunctions.axisGreaterEquals) ||
            name == nameof(ModifierFunctions.axisLesser) ||
            name == nameof(ModifierFunctions.axisLesserEquals) ||
            name == nameof(ModifierFunctions.getAxis) ||
            name == nameof(ModifierFunctions.getAxisMath) ||
            name == nameof(ModifierFunctions.activateModifier) ||
            name == nameof(ModifierFunctions.legacyTail) ||
            name.ToLower().Contains("signal") ||
            name.Contains("Other") ||
            name.Contains("copy") && name != nameof(ModifierFunctions.copyPlayerAxis) ||
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

        public static void GetSoundPath(string id, string path, bool fromSoundLibrary = false, float pitch = 1f, float volume = 1f, bool loop = false, float panStereo = 0f, Action<AudioSource> getAudioSource = null)
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
                CoroutineHelper.StartCoroutine(LoadMusicFileRaw(fullPath, audioClip =>
                {
                    var audioSource = PlaySound(id, audioClip, pitch, volume, loop, panStereo);
                    getAudioSource?.Invoke(audioSource);
                }));
            else
            {
                var audioSource = PlaySound(id, LSAudio.CreateAudioClipUsingMP3File(fullPath), pitch, volume, loop, panStereo);
                getAudioSource?.Invoke(audioSource);
            }
        }

        public static void DownloadSoundAndPlay(string id, string path, float pitch = 1f, float volume = 1f, bool loop = false, float panStereo = 0f, Action<AudioSource> getAudioSource = null)
        {
            try
            {
                var audioType = RTFile.GetAudioType(path);

                if (audioType != AudioType.UNKNOWN)
                    CoroutineHelper.StartCoroutine(AlephNetwork.DownloadAudioClip(
                        path: path, 
                        audioType: audioType,
                        callback: audioClip =>
                        {
                            var audioSource = PlaySound(id, audioClip, pitch, volume, loop, panStereo);
                            getAudioSource?.Invoke(audioSource);
                        },
                        onError: (string onError, long responseCode, string errorMsg) => CoreHelper.Log($"Error! Could not download audioclip.\n{onError}")));
            }
            catch
            {

            }
        }

        public static AudioSource PlaySound(string id, AudioClip clip, float pitch, float volume, bool loop, float panStereo = 0f)
        {
            var audioSource = SoundManager.inst.PlaySound(clip, volume, pitch * AudioManager.inst.CurrentAudioSource.pitch, loop, panStereo);
            if (loop)
                ModifiersManager.audioSources.TryAdd(id, audioSource);
            return audioSource;
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

        public static float GetAnimation(BeatmapObject reference, int fromType, int fromAxis, float delay, bool visual)
        {
            var time = GetTime(reference);
            var t = time - reference.StartTime - delay;

            if (!visual && reference.cachedSequences)
                return fromType switch
                {
                    0 => reference.cachedSequences.PositionSequence.GetValue(t).At(fromAxis),
                    1 => reference.cachedSequences.ScaleSequence.GetValue(t).At(fromAxis),
                    2 => reference.cachedSequences.RotationSequence.GetValue(t),
                    _ => 0f,
                };
            else if (visual && reference.runtimeObject is RTBeatmapObject runtimeObject && runtimeObject.visualObject && runtimeObject.visualObject.gameObject)
                return runtimeObject.visualObject.gameObject.transform.GetVector(fromType).At(fromAxis);

            return 0f;
        }

        public static float GetAnimation(BeatmapObject reference, int fromType, int fromAxis, float min, float max, float offset, float multiply, float delay, float loop, bool visual)
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

        public static string FormatStringVariables(string input, Dictionary<string, string> variables)
        {
            foreach (var variable in variables)
                input = input.Replace("{" + variable.Key + "}", variable.Value);
            return input;
        }

        public static void OnRemoveCache(Modifier modifier)
        {
            if (!modifier)
                return;

            switch (modifier.Name)
            {
                case nameof(ModifierFunctions.playSound):
                case nameof(ModifierFunctions.playSoundOnline):
                case nameof(ModifierFunctions.playOnlineSound):
                case nameof(ModifierFunctions.playDefaultSound): 
                case nameof(ModifierFunctions.loadSoundAsset): {
                        if (modifier.TryGetResult(out AudioSource cache) && cache)
                        {
                            CoreHelper.Destroy(cache);
                            return;
                        }

                        break;
                    }
                case nameof(ModifierFunctions.actorFrameTexture): {
                        if (modifier.TryGetResult(out MaskCache cache))
                            CoreHelper.Destroy(cache.renderTexture);
                        break;
                    }
                case nameof(ModifierFunctions.translateShape): {
                        if (modifier.TryGetResult(out TranslateShapeCache cache))
                        {
                            if (cache.meshFilter && cache.vertices != null)
                                cache.meshFilter.mesh.vertices = cache.vertices;
                            if (cache.collider2D && cache.points != null)
                                cache.collider2D.points = cache.points;
                        }
                        break;
                    }
            }
        }

        public static GradientColors GetColors(BeatmapObject beatmapObject) => GetColors(beatmapObject, beatmapObject.GetParentRuntime().CurrentTime - beatmapObject.StartTime);

        public static GradientColors GetColors(BeatmapObject beatmapObject, float time)
        {
            Color color;
            Color secondColor;
            {
                var prevKFIndex = beatmapObject.events[3].FindLastIndex(x => x.time < time);

                if (prevKFIndex < 0)
                    return new GradientColors(RTColors.errorColor, RTColors.errorColor);

                var prevKF = beatmapObject.events[3][prevKFIndex];
                var nextKF = beatmapObject.events[3][Mathf.Clamp(prevKFIndex + 1, 0, beatmapObject.events[3].Count - 1)];
                var easing = Ease.GetEaseFunction(nextKF.curve.ToString())(RTMath.InverseLerp(prevKF.time, nextKF.time, time));
                int prevcolor = (int)prevKF.values[0];
                int nextColor = (int)nextKF.values[0];
                var lerp = RTMath.Lerp(0f, 1f, easing);
                if (float.IsNaN(lerp) || float.IsInfinity(lerp))
                    lerp = 1f;

                color = Color.Lerp(
                    CoreHelper.CurrentBeatmapTheme.GetObjColor(prevcolor),
                    CoreHelper.CurrentBeatmapTheme.GetObjColor(nextColor),
                    lerp);

                lerp = RTMath.Lerp(prevKF.values[1], nextKF.values[1], easing);
                if (float.IsNaN(lerp) || float.IsInfinity(lerp))
                    lerp = 0f;

                color = RTColors.FadeColor(color, -(lerp - 1f));

                var lerpHue = RTMath.Lerp(prevKF.values[2], nextKF.values[2], easing);
                var lerpSat = RTMath.Lerp(prevKF.values[3], nextKF.values[3], easing);
                var lerpVal = RTMath.Lerp(prevKF.values[4], nextKF.values[4], easing);

                if (float.IsNaN(lerpHue))
                    lerpHue = nextKF.values[2];
                if (float.IsNaN(lerpSat))
                    lerpSat = nextKF.values[3];
                if (float.IsNaN(lerpVal))
                    lerpVal = nextKF.values[4];

                color = RTColors.ChangeColorHSV(color, lerpHue, lerpSat, lerpVal);

                prevcolor = (int)prevKF.values[5];
                nextColor = (int)nextKF.values[5];
                lerp = RTMath.Lerp(0f, 1f, easing);
                if (float.IsNaN(lerp) || float.IsInfinity(lerp))
                    lerp = 1f;

                secondColor = Color.Lerp(
                    CoreHelper.CurrentBeatmapTheme.GetObjColor(prevcolor),
                    CoreHelper.CurrentBeatmapTheme.GetObjColor(nextColor),
                    lerp);

                lerp = RTMath.Lerp(prevKF.values[6], nextKF.values[6], easing);
                if (float.IsNaN(lerp) || float.IsInfinity(lerp))
                    lerp = 0f;

                secondColor = RTColors.FadeColor(secondColor, -(lerp - 1f));

                lerpHue = RTMath.Lerp(prevKF.values[7], nextKF.values[7], easing);
                lerpSat = RTMath.Lerp(prevKF.values[8], nextKF.values[8], easing);
                lerpVal = RTMath.Lerp(prevKF.values[9], nextKF.values[9], easing);

                if (float.IsNaN(lerpHue))
                    lerpHue = nextKF.values[7];
                if (float.IsNaN(lerpSat))
                    lerpSat = nextKF.values[8];
                if (float.IsNaN(lerpVal))
                    lerpVal = nextKF.values[9];

                secondColor = RTColors.ChangeColorHSV(color, lerpHue, lerpSat, lerpVal);
            } // assign

            return new GradientColors(color, secondColor);
        }

        #endregion
    }

    public static class ModifierFunctions
    {
        public static bool breakModifier(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables) => true;
        public static bool disableModifier(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables) => false;
        
        #region Variable

        // local variables
        public static void getToggle(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            var value = modifier.GetBool(1, false, variables);

            if (modifier.GetBool(2, false, variables))
                value = !value;

            variables[modifier.GetValue(0)] = value.ToString();
        }

        public static void getFloat(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            variables[modifier.GetValue(0)] = modifier.GetFloat(1, 0f, variables).ToString();
        }

        public static void getInt(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            variables[modifier.GetValue(0)] = modifier.GetInt(1, 0, variables).ToString();
        }

        public static void getString(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            variables[ModifiersHelper.FormatStringVariables(modifier.GetValue(0), variables)] = ModifiersHelper.FormatStringVariables(modifier.GetValue(1, variables), variables);
        }

        public static void getStringLower(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            variables[ModifiersHelper.FormatStringVariables(modifier.GetValue(0), variables)] = ModifiersHelper.FormatStringVariables(modifier.GetValue(1, variables), variables).ToLower();
        }

        public static void getStringUpper(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            variables[ModifiersHelper.FormatStringVariables(modifier.GetValue(0), variables)] = ModifiersHelper.FormatStringVariables(modifier.GetValue(1, variables), variables).ToUpper();
        }

        public static void getColor(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            variables[modifier.GetValue(0)] = modifier.GetInt(1, 0, variables).ToString();
        }

        public static void getEnum(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            var index = (modifier.GetInt(1, 0, variables) * 2) + 4;
            if (modifier.values.Count > index)
                variables[modifier.GetValue(0)] = modifier.GetValue(index, variables).ToString();
        }

        public static void getTag(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            variables[modifier.GetValue(0)] = reference is IModifyable modifyable && modifyable.Tags.TryGetAt(modifier.GetInt(1, 0, variables), out string tag) ? tag : string.Empty;
        }

        public static void getPitch(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            variables[modifier.GetValue(0)] = AudioManager.inst.CurrentAudioSource.pitch.ToString();
        }

        public static void getMusicTime(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            variables[modifier.GetValue(0)] = AudioManager.inst.CurrentAudioSource.time.ToString();
        }

        public static void getAxis(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            var prefabable = reference.AsPrefabable();
            if (prefabable == null)
                return;

            int fromType = modifier.GetInt(1, 0, variables);
            int fromAxis = modifier.GetInt(2, 0, variables);

            float delay = modifier.GetFloat(3, 0f, variables);
            float multiply = modifier.GetFloat(4, 0f, variables);
            float offset = modifier.GetFloat(5, 0f, variables);
            float min = modifier.GetFloat(6, -9999f, variables);
            float max = modifier.GetFloat(7, 9999f, variables);
            bool useVisual = modifier.GetBool(8, false, variables);
            float loop = modifier.GetFloat(9, 9999f, variables);
            var tag = modifier.GetValue(10, variables);

            var cache = modifier.GetResultOrDefault(() => GroupBeatmapObjectCache.Get(modifier, prefabable, tag));
            if (cache.tag != tag)
            {
                cache.UpdateCache(modifier, prefabable, tag);
                modifier.Result = cache;
            }

            var beatmapObject = cache.obj;
            if (!beatmapObject)
                return;

            fromType = Mathf.Clamp(fromType, 0, beatmapObject.events.Count);
            if (!useVisual)
                fromAxis = Mathf.Clamp(fromAxis, 0, beatmapObject.events[fromType][0].values.Length);

            if (fromType < 0 || fromType > 2)
                return;

            variables[modifier.GetValue(0)] = ModifiersHelper.GetAnimation(beatmapObject, fromType, fromAxis, min, max, offset, multiply, delay, loop, useVisual).ToString();
        }

        public static void getAxisMath(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            var prefabable = reference.AsPrefabable();
            if (prefabable == null)
                return;

            if (reference is not IEvaluatable evaluatable)
                return;

            int fromType = modifier.GetInt(1, 0, variables);
            int fromAxis = modifier.GetInt(2, 0, variables);

            float delay = modifier.GetFloat(3, 0f, variables);
            bool useVisual = modifier.GetBool(4, false, variables);
            var tag = modifier.GetValue(5, variables);
            var evaluation = modifier.GetValue(6, variables);

            var cache = modifier.GetResultOrDefault(() => GroupBeatmapObjectCache.Get(modifier, prefabable, tag));
            if (cache.tag != tag)
            {
                cache.UpdateCache(modifier, prefabable, tag);
                modifier.Result = cache;
            }

            var beatmapObject = cache.obj;
            if (!beatmapObject)
                return;

            fromType = Mathf.Clamp(fromType, 0, beatmapObject.events.Count);
            if (!useVisual)
                fromAxis = Mathf.Clamp(fromAxis, 0, beatmapObject.events[fromType][0].values.Length);

            if (fromType < 0 || fromType > 2)
                return;

            var numberVariables = evaluatable.GetObjectVariables();
            ModifiersHelper.SetVariables(variables, numberVariables);

            numberVariables["axis"] = ModifiersHelper.GetAnimation(beatmapObject, fromType, fromAxis, delay, useVisual);
            beatmapObject.SetOtherObjectVariables(numberVariables);

            float value = RTMath.Parse(evaluation, numberVariables);

            variables[modifier.GetValue(0)] = value.ToString();
        }

        public static void getMath(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not IEvaluatable evaluatable)
            {
                var numberVariables = new Dictionary<string, float>();
                ModifiersHelper.SetVariables(variables, numberVariables);

                variables[modifier.GetValue(0)] = RTMath.Parse(modifier.GetValue(1, variables), numberVariables).ToString();
                return;
            }

            try
            {
                var numberVariables = evaluatable.GetObjectVariables();
                ModifiersHelper.SetVariables(variables, numberVariables);

                variables[modifier.GetValue(0)] = RTMath.Parse(modifier.GetValue(1, variables), numberVariables, evaluatable.GetObjectFunctions()).ToString();
            }
            catch { }
        }

        public static void getNearestPlayer(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not ITransformable transformable)
                return;

            var pos = transformable.GetFullPosition();
            variables[modifier.GetValue(0)] = PlayerManager.GetClosestPlayerIndex(pos).ToString();
        }

        public static void getCollidingPlayers(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not BeatmapObject beatmapObject)
                return;

            var runtimeObject = beatmapObject.runtimeObject;
            if (runtimeObject && runtimeObject.visualObject && runtimeObject.visualObject.collider)
            {
                var collider = runtimeObject.visualObject.collider;

                var players = PlayerManager.Players;
                for (int i = 0; i < players.Count; i++)
                {
                    var player = players[i];
                    variables[modifier.GetValue(0) + "_" + i] = (player.RuntimePlayer && player.RuntimePlayer.CurrentCollider && player.RuntimePlayer.CurrentCollider.IsTouching(collider)).ToString();
                }
            }
        }

        public static void getPlayerHealth(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (PlayerManager.Players.TryGetAt(modifier.GetInt(1, 0, variables), out PAPlayer player))
                variables[modifier.GetValue(0)] = player.Health.ToString();
        }

        public static void getPlayerLives(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (PlayerManager.Players.TryGetAt(modifier.GetInt(1, 0, variables), out PAPlayer player))
                variables[modifier.GetValue(0)] = player.lives.ToString();
        }

        public static void getPlayerPosX(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (PlayerManager.Players.TryGetAt(modifier.GetInt(1, 0, variables), out PAPlayer player) && player.RuntimePlayer && player.RuntimePlayer.rb)
                variables[modifier.GetValue(0)] = player.RuntimePlayer.rb.transform.position.x.ToString();
        }

        public static void getPlayerPosY(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (PlayerManager.Players.TryGetAt(modifier.GetInt(1, 0, variables), out PAPlayer player) && player.RuntimePlayer && player.RuntimePlayer.rb)
                variables[modifier.GetValue(0)] = player.RuntimePlayer.rb.transform.position.y.ToString();
        }

        public static void getPlayerRot(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (PlayerManager.Players.TryGetAt(modifier.GetInt(1, 0, variables), out PAPlayer player) && player.RuntimePlayer && player.RuntimePlayer.rb)
                variables[modifier.GetValue(0)] = player.RuntimePlayer.rb.transform.eulerAngles.z.ToString();
        }

        public static void getEventValue(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (!RTLevel.Current.eventEngine)
                return;

            float multiply = modifier.GetFloat(4, 0f, variables);
            float offset = modifier.GetFloat(5, 0f, variables);
            float min = modifier.GetFloat(6, -9999f, variables);
            float max = modifier.GetFloat(7, 9999f, variables);
            float loop = modifier.GetFloat(8, 9999f, variables);

            var value = RTLevel.Current.eventEngine.Interpolate(modifier.GetInt(1, 0, variables), modifier.GetInt(2, 0, variables), RTLevel.Current.CurrentTime - modifier.GetFloat(3, 0f, variables));

            value = Mathf.Clamp((value - offset) * multiply % loop, min, max);

            variables[modifier.GetValue(0)] = value.ToString();
        }

        public static void getSample(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            variables[modifier.GetValue(0)] = RTLevel.Current.GetSample(modifier.GetInt(1, 0, variables), modifier.GetFloat(2, 1f, variables)).ToString();
        }

        public static void getText(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not BeatmapObject beatmapObject)
                return;

            var useVisual = modifier.GetBool(1, false, variables);
            if (useVisual && beatmapObject.runtimeObject && beatmapObject.runtimeObject.visualObject is TextObject textObject)
                variables[modifier.GetValue(0)] = textObject.GetText();
            else
                variables[modifier.GetValue(0)] = beatmapObject.text;
        }

        public static void getTextOther(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not IPrefabable prefabable)
                return;

            if (!GameData.Current.TryFindObjectWithTag(modifier, prefabable, modifier.GetValue(2, variables), out BeatmapObject beatmapObject))
                return;

            var useVisual = modifier.GetBool(1, false, variables);
            if (useVisual && beatmapObject.runtimeObject && beatmapObject.runtimeObject.visualObject is TextObject textObject)
                variables[modifier.GetValue(0)] = textObject.GetText();
            else
                variables[modifier.GetValue(0)] = beatmapObject.text;
        }

        public static void getCurrentKey(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            variables[modifier.GetValue(0)] = CoreHelper.GetKeyCodeDown().ToString();
        }

        public static void getColorSlotHexCode(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            var color = ThemeManager.inst.Current.GetObjColor(modifier.GetInt(1, 0, variables));
            color = RTColors.FadeColor(color, modifier.GetFloat(2, 1f, variables));
            color = RTColors.ChangeColorHSV(color, modifier.GetFloat(3, 0f, variables), modifier.GetFloat(4, 0f, variables), modifier.GetFloat(5, 0f, variables));

            variables[modifier.GetValue(0)] = RTColors.ColorToHexOptional(color);
        }

        public static void getFloatFromHexCode(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            variables[modifier.GetValue(0)] = RTColors.HexToFloat(modifier.GetValue(1, variables)).ToString();
        }

        public static void getHexCodeFromFloat(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            variables[modifier.GetValue(0)] = RTColors.FloatToHex(modifier.GetFloat(1, 1f, variables));
        }

        public static void getJSONString(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (!RTFile.TryReadFromFile(ModifiersHelper.GetSaveFile(modifier.GetValue(1, variables)), out string json))
                return;

            var jn = JSON.Parse(json);

            var fjn = jn[modifier.GetValue(2, variables)][modifier.GetValue(3, variables)]["string"];

            variables[modifier.GetValue(0)] = fjn;
        }

        public static void getJSONFloat(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (!RTFile.TryReadFromFile(ModifiersHelper.GetSaveFile(modifier.GetValue(1, variables)), out string json))
                return;

            var jn = JSON.Parse(json);

            var fjn = jn[modifier.GetValue(2, variables)][modifier.GetValue(3, variables)]["float"];

            variables[modifier.GetValue(0)] = fjn;
        }

        public static void getJSON(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            try
            {
                var jn = JSON.Parse(modifier.GetValue(1, variables));
                var json1 = modifier.GetValue(2, variables);
                if (!string.IsNullOrEmpty(json1))
                    jn = jn[json1];

                variables[modifier.GetValue(0)] = jn;
            }
            catch { }
        }

        public static void getSubString(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            try
            {
                var str = modifier.GetValue(1, variables);
                var subString = str.Substring(Mathf.Clamp(modifier.GetInt(2, 0, variables), 0, str.Length), Mathf.Clamp(modifier.GetInt(3, 0, variables), 0, str.Length));
                variables[modifier.GetValue(0)] = subString;
            }
            catch { }
        }

        public static void getSplitString(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            var str = modifier.GetValue(0, variables);
            var ch = modifier.GetValue(1, variables);

            if (string.IsNullOrEmpty(str) || string.IsNullOrEmpty(ch))
                return;

            var split = str.Split(ch[0]);
            for (int i = 0; i < split.Length; i++)
            {
                var index = i + 2;
                if (modifier.values.InRange(index))
                    variables[modifier.GetValue(index)] = split[i];
            }
        }

        public static void getSplitStringAt(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            var str = modifier.GetValue(0, variables);
            var ch = modifier.GetValue(1, variables);

            if (string.IsNullOrEmpty(str) || string.IsNullOrEmpty(ch))
                return;

            var split = str.Split(ch[0]);
            variables[modifier.GetValue(2)] = split.GetAt(modifier.GetInt(3, 0, variables));
        }

        public static void getSplitStringCount(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            var str = modifier.GetValue(0, variables);
            var ch = modifier.GetValue(1, variables);

            if (string.IsNullOrEmpty(str) || string.IsNullOrEmpty(ch))
                return;

            var split = str.Split(ch[0]);
            variables[modifier.GetValue(2)] = split.Length.ToString();
        }

        public static void getStringLength(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            variables[modifier.GetValue(0)] = modifier.GetValue(1, variables).Length.ToString();
        }

        public static void getParsedString(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            variables[modifier.GetValue(0)] = RTString.ParseText(modifier.GetValue(1, variables));
        }

        public static void getRegex(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            var regex = new Regex(modifier.GetValue(0, variables));
            var match = regex.Match(modifier.GetValue(1, variables));

            if (!match.Success)
                return;

            for (int i = 0; i < match.Groups.Count; i++)
            {
                var index = i + 2;
                if (modifier.values.InRange(index))
                    variables[modifier.values[index]] = match.Groups[i].ToString();
            }
        }

        public static void getFormatVariable(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            try
            {
                object[] args = new object[modifier.values.Count - 2];
                for (int i = 2; i < modifier.values.Count; i++)
                    args[i - 2] = modifier.GetValue(i, variables);

                variables[modifier.GetValue(0)] = string.Format(modifier.GetValue(1, variables), args);
            }
            catch { }
        }

        public static void getComparison(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            variables[modifier.GetValue(0)] = (modifier.GetValue(1, variables) == modifier.GetValue(2, variables)).ToString();
        }

        public static void getComparisonMath(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not IEvaluatable evaluatable)
                return;

            try
            {
                var numberVariables = evaluatable.GetObjectVariables();
                var functions = evaluatable.GetObjectFunctions();

                variables[modifier.GetValue(0)] = (RTMath.Parse(modifier.GetValue(1, variables), numberVariables, functions) == RTMath.Parse(modifier.GetValue(2, variables), numberVariables, functions)).ToString();
            }
            catch { }
        }

        public static void getModifiedColor(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            var color = RTColors.HexToColor(ModifiersHelper.FormatStringVariables(modifier.GetValue(1, variables), variables));

            variables[modifier.GetValue(0)] = RTColors.ColorToHexOptional(RTColors.FadeColor(RTColors.ChangeColorHSV(color,
                    modifier.GetFloat(3, 0f, variables),
                    modifier.GetFloat(4, 0f, variables),
                    modifier.GetFloat(5, 0f, variables)), modifier.GetFloat(2, 1f, variables)));
        }

        public static void getMixedColors(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            var colors = new List<Color>();
            for (int i = 1; i < modifier.values.Count; i++)
                colors.Add(RTColors.HexToColor(ModifiersHelper.FormatStringVariables(modifier.GetValue(1, variables), variables)));

            variables[modifier.GetValue(0)] = RTColors.MixColors(colors).ToString();
        }

        public static void getLerpColor(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            var a = RTColors.HexToColor(modifier.GetValue(1, variables));
            var b = RTColors.HexToColor(modifier.GetValue(2, variables));
            variables[modifier.GetValue(0)] = RTColors.ColorToHexOptional(RTMath.Lerp(a, b, modifier.GetFloat(3, 1f, variables)));
        }

        public static void getAddColor(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            var a = RTColors.HexToColor(modifier.GetValue(1, variables));
            var b = RTColors.HexToColor(modifier.GetValue(2, variables));
            variables[modifier.GetValue(0)] = RTColors.ColorToHexOptional(a + b * modifier.GetFloat(3, 1f, variables));
        }

        public static void getVisualColor(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference.GetRuntimeObject() is RTBeatmapObject runtimeObject && runtimeObject.visualObject is SolidObject solidObject)
            {
                var colors = solidObject.GetColors();
                var startColorName = modifier.GetValue(0);
                var endColorName = modifier.GetValue(1);
                if (!string.IsNullOrEmpty(startColorName))
                    variables[startColorName] = RTColors.ColorToHexOptional(colors.startColor);
                if (!string.IsNullOrEmpty(endColorName))
                    variables[endColorName] = RTColors.ColorToHexOptional(colors.endColor);
            }
            else if (reference is BeatmapObject beatmapObject)
            {
                var colors = ModifiersHelper.GetColors(beatmapObject);
                var startColorName = modifier.GetValue(0);
                var endColorName = modifier.GetValue(1);
                if (!string.IsNullOrEmpty(startColorName))
                    variables[startColorName] = RTColors.ColorToHexOptional(colors.startColor);
                if (!string.IsNullOrEmpty(endColorName))
                    variables[endColorName] = RTColors.ColorToHexOptional(colors.endColor);
            }
        }

        public static void getVisualColorRGBA(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference.GetRuntimeObject() is RTBeatmapObject runtimeObject && runtimeObject.visualObject is SolidObject solidObject)
            {
                var colors = solidObject.GetColors();
                var startColorRName = modifier.GetValue(0);
                var startColorGName = modifier.GetValue(1);
                var startColorBName = modifier.GetValue(2);
                var startColorAName = modifier.GetValue(3);
                var endColorRName = modifier.GetValue(4);
                var endColorGName = modifier.GetValue(5);
                var endColorBName = modifier.GetValue(6);
                var endColorAName = modifier.GetValue(7);
                if (!string.IsNullOrEmpty(startColorRName))
                    variables[startColorRName] = colors.startColor.r.ToString();
                if (!string.IsNullOrEmpty(startColorGName))
                    variables[startColorGName] = colors.startColor.g.ToString();
                if (!string.IsNullOrEmpty(startColorBName))
                    variables[startColorBName] = colors.startColor.b.ToString();
                if (!string.IsNullOrEmpty(startColorAName))
                    variables[startColorAName] = colors.startColor.a.ToString();
                if (!string.IsNullOrEmpty(endColorRName))
                    variables[endColorRName] = colors.endColor.r.ToString();
                if (!string.IsNullOrEmpty(endColorGName))
                    variables[endColorGName] = colors.endColor.g.ToString();
                if (!string.IsNullOrEmpty(endColorBName))
                    variables[endColorBName] = colors.endColor.b.ToString();
                if (!string.IsNullOrEmpty(endColorAName))
                    variables[endColorAName] = colors.endColor.a.ToString();
            }
            else if (reference is BeatmapObject beatmapObject)
            {
                var colors = ModifiersHelper.GetColors(beatmapObject);
                var startColorRName = modifier.GetValue(0);
                var startColorGName = modifier.GetValue(1);
                var startColorBName = modifier.GetValue(2);
                var startColorAName = modifier.GetValue(3);
                var endColorRName = modifier.GetValue(4);
                var endColorGName = modifier.GetValue(5);
                var endColorBName = modifier.GetValue(6);
                var endColorAName = modifier.GetValue(7);
                if (!string.IsNullOrEmpty(startColorRName))
                    variables[startColorRName] = colors.startColor.r.ToString();
                if (!string.IsNullOrEmpty(startColorGName))
                    variables[startColorGName] = colors.startColor.g.ToString();
                if (!string.IsNullOrEmpty(startColorBName))
                    variables[startColorBName] = colors.startColor.b.ToString();
                if (!string.IsNullOrEmpty(startColorAName))
                    variables[startColorAName] = colors.startColor.a.ToString();
                if (!string.IsNullOrEmpty(endColorRName))
                    variables[endColorRName] = colors.endColor.r.ToString();
                if (!string.IsNullOrEmpty(endColorGName))
                    variables[endColorGName] = colors.endColor.g.ToString();
                if (!string.IsNullOrEmpty(endColorBName))
                    variables[endColorBName] = colors.endColor.b.ToString();
                if (!string.IsNullOrEmpty(endColorAName))
                    variables[endColorAName] = colors.endColor.a.ToString();
            }
        }

        public static void getVisualOpacity(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference.GetRuntimeObject() is RTBeatmapObject runtimeObject && runtimeObject.visualObject is SolidObject solidObject)
            {
                var colors = solidObject.GetColors();
                var startOpacityName = modifier.GetValue(0);
                var endOpacityName = modifier.GetValue(1);
                if (!string.IsNullOrEmpty(startOpacityName))
                    variables[startOpacityName] = colors.startColor.a.ToString();
                if (!string.IsNullOrEmpty(endOpacityName))
                    variables[endOpacityName] = colors.endColor.a.ToString();
            }
            else if (reference is BeatmapObject beatmapObject)
            {
                var startOpacityName = modifier.GetValue(0);
                var endOpacityName = modifier.GetValue(1);
                if (!string.IsNullOrEmpty(startOpacityName))
                    variables[startOpacityName] = (-(beatmapObject.Interpolate(3, 1) - 1f)).ToString();
                if (!string.IsNullOrEmpty(endOpacityName))
                    variables[endOpacityName] = (-(beatmapObject.Interpolate(3, 6) - 1f)).ToString();
            }
        }

        public static void getFloatAnimationKF(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            var audioTime = modifier.GetFloat(1, 0f, variables);
            var type = modifier.GetInt(2, 0, variables);

            Sequence<float> sequence;

            // get cache
            if (modifier.HasResult() && modifier.GetBool(3, true, variables))
                sequence = modifier.GetResult<Sequence<float>>();
            else
            {
                var value = modifier.GetFloat(4, 0f, variables);

                var currentTime = 0f;

                var keyframes = new List<IKeyframe<float>>();
                keyframes.Add(new FloatKeyframe(currentTime, value, Ease.Linear));
                for (int i = 5; i < modifier.values.Count; i += 4)
                {
                    var time = modifier.GetFloat(i, 0f, variables);
                    if (time < currentTime)
                        continue;

                    var x = modifier.GetFloat(i + 1, 0f, variables);
                    var relative = modifier.GetBool(i + 2, true, variables);

                    var easing = modifier.GetValue(i + 3, variables);
                    if (int.TryParse(easing, out int e) && e >= 0 && e < DataManager.inst.AnimationList.Count)
                        easing = DataManager.inst.AnimationList[e].Name;

                    var setvalue = x;
                    if (relative)
                        setvalue += value;

                    keyframes.Add(new FloatKeyframe(currentTime + time, setvalue, Ease.GetEaseFunction(easing, Ease.Linear)));

                    value = setvalue;
                    currentTime = time;
                }

                sequence = new Sequence<float>(keyframes);
                modifier.Result = sequence;
            }

            if (sequence != null)
                variables[modifier.GetValue(0)] = sequence.GetValue(audioTime).ToString();
        }

        public static void getEditorBin(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            ObjectEditorData editorData = null;
            var prefabable = reference.AsPrefabable();
            if (prefabable != null && prefabable.FromPrefab && modifier.GetBool(1, false, variables))
                editorData = prefabable.GetPrefabObject()?.EditorData;
            else if (reference is IEditable editable)
                editorData = editable.EditorData;

            variables[modifier.GetValue(0)] = editorData ? editorData.Bin.ToString() : 0.ToString();
        }

        public static void getEditorLayer(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            ObjectEditorData editorData = null;
            var prefabable = reference.AsPrefabable();
            if (prefabable != null && prefabable.FromPrefab && modifier.GetBool(1, false, variables))
                editorData = prefabable.GetPrefabObject()?.EditorData;
            else if (reference is IEditable editable)
                editorData = editable.EditorData;

            variables[modifier.GetValue(0)] = editorData ? editorData.Layer.ToString() : 0.ToString();
        }

        public static void getObjectName(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is BeatmapObject beatmapObject)
                variables[modifier.GetValue(0)] = beatmapObject.name;
            else if (reference is BackgroundObject backgroundObject)
                variables[modifier.GetValue(0)] = backgroundObject.name;
            else if (reference is PrefabObject prefabObject && prefabObject.GetPrefab() is Prefab prefab)
                variables[modifier.GetValue(0)] = prefab.name;
            else if (reference is RTPlayer.RTCustomPlayerObject customPlayerObject && customPlayerObject.reference)
                variables[modifier.GetValue(0)] = customPlayerObject.reference.name;
        }

        public static void getSignaledVariables(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (modifier.Result is Dictionary<string, string> otherVariables)
            {
                foreach (var variable in otherVariables)
                    variables[variable.Key] = variable.Value;

                if (!modifier.GetBool(0, true, variables)) // don't clear
                    return;

                otherVariables.Clear();
                modifier.Result = null;
            }
        }

        public static void signalLocalVariables(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not IPrefabable prefabable)
                return;

            var list = GameData.Current.FindObjectsWithTag(modifier, prefabable, modifier.GetValue(0, variables));

            if (list.IsEmpty())
                return;

            var sendVariables = new Dictionary<string, string>(variables);

            foreach (var beatmapObject in list)
            {
                beatmapObject.modifiers.FindAll(x => x.Name == nameof(getSignaledVariables)).ForLoop(modifier =>
                {
                    if (modifier.TryGetResult(out Dictionary<string, string> otherVariables))
                    {
                        otherVariables.InsertRange(variables);
                        return;
                    }

                    modifier.Result = sendVariables;
                });
            }
        }

        public static void clearLocalVariables(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables) => variables.Clear();

        public static void storeLocalVariables(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (modifier.TryGetResult(out Dictionary<string, string> storedVariables))
            {
                variables.InsertRange(storedVariables);
                return;
            }

            var storeVariables = new Dictionary<string, string>(variables);
            modifier.Result = storeVariables;
        }

        // object variable
        public static void addVariable(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (modifier.values.Count == 2)
            {
                if (reference is not IPrefabable prefabable)
                    return;

                var list = GameData.Current.FindObjectsWithTag(modifier, prefabable, modifier.GetValue(1, variables));
                if (list.IsEmpty())
                    return;

                int num = modifier.GetInt(0, 0, variables);

                foreach (var beatmapObject in list)
                    beatmapObject.integerVariable += num;
            }
            else
                reference.IntVariable += modifier.GetInt(0, 0, variables);
        }

        public static void addVariableOther(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not IPrefabable prefabable)
                return;

            var list = GameData.Current.FindObjectsWithTag(modifier, prefabable, modifier.GetValue(1, variables));
            if (list.IsEmpty())
                return;

            int num = modifier.GetInt(0, 0, variables);

            foreach (var beatmapObject in list)
                beatmapObject.integerVariable += num;
        }

        public static void subVariable(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (modifier.values.Count == 2)
            {
                if (reference is not IPrefabable prefabable)
                    return;

                var list = GameData.Current.FindObjectsWithTag(modifier, prefabable, modifier.GetValue(1, variables));
                if (list.IsEmpty())
                    return;

                int num = modifier.GetInt(0, 0, variables);

                foreach (var beatmapObject in list)
                    beatmapObject.integerVariable -= num;
            }
            else
                reference.IntVariable -= modifier.GetInt(0, 0, variables);
        }

        public static void subVariableOther(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not IPrefabable prefabable)
                return;

            var list = GameData.Current.FindObjectsWithTag(modifier, prefabable, modifier.GetValue(1, variables));
            if (list.IsEmpty())
                return;

            int num = modifier.GetInt(0, 0, variables);

            foreach (var beatmapObject in list)
                beatmapObject.integerVariable -= num;
        }

        public static void setVariable(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (modifier.values.Count == 2)
            {
                if (reference is not IPrefabable prefabable)
                    return;

                var list = GameData.Current.FindObjectsWithTag(modifier, prefabable, modifier.GetValue(1, variables));
                if (list.IsEmpty())
                    return;

                int num = modifier.GetInt(0, 0, variables);

                foreach (var beatmapObject in list)
                    beatmapObject.integerVariable = num;
            }
            else
                reference.IntVariable = modifier.GetInt(0, 0, variables);
        }

        public static void setVariableOther(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not IPrefabable prefabable)
                return;

            var list = GameData.Current.FindObjectsWithTag(modifier, prefabable, modifier.GetValue(1, variables));
            if (list.IsEmpty())
                return;

            int num = modifier.GetInt(0, 0, variables);

            foreach (var beatmapObject in list)
                beatmapObject.integerVariable = num;
        }

        public static void setVariableRandom(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (modifier.values.Count == 3)
            {
                if (reference is not IPrefabable prefabable)
                    return;

                var list = GameData.Current.FindObjectsWithTag(modifier, prefabable, modifier.GetValue(0, variables));
                if (list.IsEmpty())
                    return;

                int min = modifier.GetInt(1, 0, variables);
                int max = modifier.GetInt(2, 0, variables);

                foreach (var beatmapObject in list)
                    beatmapObject.integerVariable = UnityEngine.Random.Range(min, max < 0 ? max - 1 : max + 1);
            }
            else
            {
                var min = modifier.GetInt(0, 0, variables);
                var max = modifier.GetInt(1, 0, variables);
                reference.IntVariable = UnityEngine.Random.Range(min, max < 0 ? max - 1 : max + 1);
            }
        }

        public static void setVariableRandomOther(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not IPrefabable prefabable)
                return;

            var list = GameData.Current.FindObjectsWithTag(modifier, prefabable, modifier.GetValue(0, variables));
            if (list.IsEmpty())
                return;

            int min = modifier.GetInt(1, 0, variables);
            int max = modifier.GetInt(2, 0, variables);

            foreach (var beatmapObject in list)
                beatmapObject.integerVariable = UnityEngine.Random.Range(min, max < 0 ? max - 1 : max + 1);
        }

        public static void animateVariableOther(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not IPrefabable prefabable)
                return;

            var fromType = modifier.GetInt(1, 0, variables);
            var fromAxis = modifier.GetInt(2, 0, variables);
            var delay = modifier.GetFloat(3, 0, variables);
            var multiply = modifier.GetFloat(4, 0, variables);
            var offset = modifier.GetFloat(5, 0, variables);
            var min = modifier.GetFloat(6, -9999f, variables);
            var max = modifier.GetFloat(7, 9999f, variables);
            var loop = modifier.GetFloat(8, 9999f, variables);

            var list = GameData.Current.FindObjectsWithTag(modifier, prefabable, modifier.GetValue(0, variables));
            if (list.IsEmpty())
                return;

            for (int i = 0; i < list.Count; i++)
            {
                var beatmapObject = list[i];
                var cachedSequences = beatmapObject.cachedSequences;
                var time = AudioManager.inst.CurrentAudioSource.time;

                fromType = Mathf.Clamp(fromType, 0, beatmapObject.events.Count);
                fromAxis = Mathf.Clamp(fromAxis, 0, beatmapObject.events[fromType][0].values.Length);

                if (!cachedSequences)
                    continue;

                switch (fromType)
                {
                    // To Type Position
                    // To Axis X
                    // From Type Position
                    case 0: {
                            var sequence = cachedSequences.PositionSequence.GetValue(time - beatmapObject.StartTime - delay);

                            beatmapObject.integerVariable = (int)Mathf.Clamp((fromAxis == 0 ? sequence.x % loop : fromAxis == 1 ? sequence.y % loop : sequence.z % loop) * multiply - offset, min, max);
                            break;
                        }
                    // To Type Position
                    // To Axis X
                    // From Type Scale
                    case 1: {
                            var sequence = cachedSequences.ScaleSequence.GetValue(time - beatmapObject.StartTime - delay);

                            beatmapObject.integerVariable = (int)Mathf.Clamp((fromAxis == 0 ? sequence.x % loop : sequence.y % loop) * multiply - offset, min, max);
                            break;
                        }
                    // To Type Position
                    // To Axis X
                    // From Type Rotation
                    case 2: {
                            var sequence = cachedSequences.RotationSequence.GetValue(time - beatmapObject.StartTime - delay) * multiply;

                            beatmapObject.integerVariable = (int)Mathf.Clamp((sequence % loop) - offset, min, max);
                            break;
                        }
                }
            }
        }

        public static void clampVariable(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is IModifyable modifyable)
                modifyable.IntVariable = Mathf.Clamp(modifyable.IntVariable, modifier.GetInt(1, 0, variables), modifier.GetInt(2, 0, variables));
        }

        public static void clampVariableOther(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not IPrefabable prefabable)
                return;

            var list = GameData.Current.FindObjectsWithTag(modifier, prefabable, modifier.GetValue(0, variables));

            var min = modifier.GetInt(1, 0, variables);
            var max = modifier.GetInt(2, 0, variables);

            if (!list.IsEmpty())
                foreach (var bm in list)
                    bm.integerVariable = Mathf.Clamp(bm.integerVariable, min, max);
        }

        #endregion

        #region Audio

        public static void setPitch(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (RTLevel.Current.eventEngine)
                RTLevel.Current.eventEngine.pitchOffset = modifier.GetFloat(0, 0f, variables);
        }

        public static void addPitch(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (RTLevel.Current.eventEngine)
                RTLevel.Current.eventEngine.pitchOffset += modifier.GetFloat(0, 0f, variables);
        }

        public static void setPitchMath(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not IEvaluatable evaluatable)
                return;

            var numberVariables = evaluatable.GetObjectVariables();
            if (variables != null)
            {
                foreach (var variable in variables)
                {
                    if (float.TryParse(variable.Value, out float num))
                        numberVariables[variable.Key] = num;
                }
            }

            if (RTLevel.Current.eventEngine)
                RTLevel.Current.eventEngine.pitchOffset = RTMath.Parse(modifier.GetValue(0, variables), numberVariables);
        }

        public static void addPitchMath(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not IEvaluatable evaluatable)
                return;

            var numberVariables = evaluatable.GetObjectVariables();
            if (variables != null)
            {
                foreach (var variable in variables)
                {
                    if (float.TryParse(variable.Value, out float num))
                        numberVariables[variable.Key] = num;
                }
            }

            if (RTLevel.Current.eventEngine)
                RTLevel.Current.eventEngine.pitchOffset += RTMath.Parse(modifier.GetValue(0, variables), numberVariables);
        }

        public static void animatePitch(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            var time = modifier.GetFloat(0, 0f, variables);
            var pitch = modifier.GetFloat(1, 0f, variables);
            var relative = modifier.GetBool(2, true, variables);

            string easing = modifier.GetValue(3, variables);
            if (int.TryParse(easing, out int e) && e >= 0 && e < DataManager.inst.AnimationList.Count)
                easing = DataManager.inst.AnimationList[e].Name;

            var setPitch = pitch;
            if (relative)
            {
                if (modifier.constant)
                    setPitch *= CoreHelper.TimeFrame;

                setPitch += AudioManager.inst.CurrentAudioSource.pitch;
            }

            if (!modifier.constant)
            {
                var animation = new RTAnimation("Animate Object Offset");

                animation.animationHandlers = new List<AnimationHandlerBase>
                {
                    new AnimationHandler<float>(new List<IKeyframe<float>>
                    {
                        new FloatKeyframe(0f, AudioManager.inst.CurrentAudioSource.pitch, Ease.Linear),
                        new FloatKeyframe(Mathf.Clamp(time, 0f, 9999f), setPitch, Ease.GetEaseFunction(easing, Ease.Linear)),
                    }, x => RTLevel.Current.eventEngine.pitchOffset = x, interpolateOnComplete: true),
                };
                animation.SetDefaultOnComplete();
                AnimationManager.inst.Play(animation);
                return;
            }

            RTLevel.Current.eventEngine.pitchOffset = setPitch;
        }

        public static void setMusicTime(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables) => AudioManager.inst.SetMusicTime(modifier.GetFloat(0, 0f, variables));

        public static void setMusicTimeMath(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not IEvaluatable evaluatable)
                return;

            var numberVariables = evaluatable.GetObjectVariables();
            if (variables != null)
            {
                foreach (var variable in variables)
                {
                    if (float.TryParse(variable.Value, out float num))
                        numberVariables[variable.Key] = num;
                }
            }

            AudioManager.inst.SetMusicTime(RTMath.Parse(modifier.GetValue(0, variables), numberVariables));
        }

        public static void setMusicTimeStartTime(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is ILifetime lifeTime)
                AudioManager.inst.SetMusicTime(lifeTime.StartTime);
        }

        public static void setMusicTimeAutokill(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is ILifetime lifeTime)
                AudioManager.inst.SetMusicTime(lifeTime.StartTime + lifeTime.SpawnDuration);
        }

        public static void setMusicPlaying(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables) => SoundManager.inst.SetPlaying(modifier.GetBool(0, false, variables));

        public static void playSound(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (modifier.constant && modifier.TryGetResult(out AudioSource cache) && cache)
            {
                cache.UnPause();
                return;
            }

            var path = modifier.GetValue(0, variables);
            var global = modifier.GetBool(1, false, variables);
            var pitch = modifier.GetFloat(2, 1f, variables);
            var vol = modifier.GetFloat(3, 1f, variables);
            var loop = modifier.GetBool(4, false, variables);
            var panStereo = modifier.GetFloat(5, 0f, variables);

            var id = reference is PAObjectBase obj ? obj.id : reference is RTPlayer.RTPlayerObject playerObject ? playerObject.id : string.Empty;
            if (string.IsNullOrEmpty(id))
                loop = false;

            if (GameData.Current && GameData.Current.assets.sounds.TryFind(x => x.name == path, out SoundAsset soundAsset))
            {
                if (!soundAsset.audio)
                {
                    CoroutineHelper.StartCoroutine(soundAsset.LoadAudioClip(() =>
                    {
                        if (soundAsset.audio)
                            modifier.Result = ModifiersHelper.PlaySound(id, soundAsset.audio, pitch, vol, loop, panStereo);
                    }));
                    return;
                }

                modifier.Result = ModifiersHelper.PlaySound(id, soundAsset.audio, pitch, vol, loop, panStereo);
                return;
            }

            ModifiersHelper.GetSoundPath(id, path, global, pitch, vol, loop, panStereo, audioSource => modifier.Result = audioSource);
        }

        public static void playSoundOnline(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables) => playOnlineSound(modifier, reference, variables);

        public static void playOnlineSound(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (modifier.constant && modifier.TryGetResult(out AudioSource cache) && cache)
            {
                cache.UnPause();
                return;
            }

            var url = modifier.GetValue(0, variables);
            var pitch = modifier.GetFloat(1, 1f, variables);
            var vol = modifier.GetFloat(2, 1f, variables);
            var loop = modifier.GetBool(3, false, variables);
            var panStereo = modifier.GetFloat(4, 0f, variables);

            var id = reference is PAObjectBase obj ? obj.id : reference is RTPlayer.RTPlayerObject playerObject ? playerObject.id : string.Empty;
            if (string.IsNullOrEmpty(id))
                loop = false;

            if (!string.IsNullOrEmpty(url))
                ModifiersHelper.DownloadSoundAndPlay(id, url, pitch, vol, loop, panStereo, audioSource => modifier.Result = audioSource);
        }

        public static void playDefaultSound(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (modifier.constant && modifier.TryGetResult(out AudioSource cache) && cache)
            {
                cache.UnPause();
                return;
            }

            var pitch = modifier.GetFloat(1, 1f, variables);
            var vol = modifier.GetFloat(2, 1f, variables);
            var loop = modifier.GetBool(3, false, variables);
            var panStereo = modifier.GetFloat(4, 0f, variables);

            if (!LegacyResources.soundClips.TryFind(x => x.id == modifier.GetValue(0), out SoundGroup soundGroup))
                return;

            var clip = soundGroup.GetClip();
            var audioSource = Camera.main.gameObject.AddComponent<AudioSource>();
            audioSource.clip = clip;
            audioSource.playOnAwake = true;
            audioSource.loop = loop;
            audioSource.pitch = pitch * AudioManager.inst.CurrentAudioSource.pitch;
            audioSource.volume = vol * AudioManager.inst.sfxVol;
            audioSource.panStereo = panStereo;
            audioSource.Play();

            float x = pitch * AudioManager.inst.CurrentAudioSource.pitch;
            if (x == 0f)
                x = 1f;
            if (x < 0f)
                x = -x;

            var id = reference is PAObjectBase obj ? obj.id : reference is RTPlayer.RTPlayerObject playerObject ? playerObject.id : string.Empty;
            if (string.IsNullOrEmpty(id))
                loop = false;

            modifier.Result = audioSource;

            if (!loop)
                CoroutineHelper.StartCoroutine(AudioManager.inst.DestroyWithDelay(audioSource, clip.clip.length / x));
            else if (!ModifiersManager.audioSources.ContainsKey(id))
                ModifiersManager.audioSources.Add(id, audioSource);
        }

        public static void audioSource(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not BeatmapObject beatmapObject)
                return;

            var runtimeObject = beatmapObject.runtimeObject;
            if (!runtimeObject || !runtimeObject.visualObject || !runtimeObject.visualObject.gameObject)
                return;

            if (modifier.TryGetResult(out AudioModifier audioModifier))
            {
                audioModifier.pitch = modifier.GetFloat(2, 1f, variables);
                audioModifier.volume = modifier.GetFloat(3, 1f, variables);
                audioModifier.loop = modifier.GetBool(4, true, variables);
                audioModifier.timeOffset = modifier.GetBool(6, true, variables) ? AudioManager.inst.CurrentAudioSource.time + modifier.GetFloat(5, 0f, variables) : modifier.GetFloat(5, 0f, variables);
                audioModifier.lengthOffset = modifier.GetFloat(7, 0f, variables);
                audioModifier.playing = modifier.GetBool(8, true, variables);
                audioModifier.panStereo = modifier.GetFloat(9, 0f, variables);
                audioModifier.Tick();
                return;
            }

            var path = modifier.GetValue(0, variables);

            string fullPath =
                !bool.TryParse(modifier.GetValue(1, variables), out bool global) || !global ?
                RTFile.CombinePaths(RTFile.BasePath, path) :
                RTFile.CombinePaths(RTFile.ApplicationDirectory, ModifiersManager.SOUNDLIBRARY_PATH, path);

            var audioDotFormats = RTFile.AudioDotFormats;
            for (int i = 0; i < audioDotFormats.Length; i++)
            {
                var audioDotFormat = audioDotFormats[i];
                if (!path.EndsWith(audioDotFormat) && RTFile.FileExists(fullPath + audioDotFormat))
                    fullPath += audioDotFormat;
            }

            if (!RTFile.FileExists(fullPath))
            {
                CoreHelper.LogError($"File does not exist {fullPath}");
                return;
            }

            if (fullPath.EndsWith(FileFormat.MP3.Dot()))
            {
                modifier.Result = runtimeObject.visualObject.gameObject.AddComponent<AudioModifier>();
                ((AudioModifier)modifier.Result).Init(LSAudio.CreateAudioClipUsingMP3File(fullPath), beatmapObject, modifier);
                return;
            }

            CoroutineHelper.StartCoroutine(ModifiersHelper.LoadMusicFileRaw(fullPath, audioClip =>
            {
                if (!audioClip)
                {
                    CoreHelper.LogError($"Failed to load audio {fullPath}");
                    return;
                }

                audioClip.name = path;

                if (!runtimeObject.visualObject || !runtimeObject.visualObject.gameObject)
                    return;

                var audioModifier = runtimeObject.visualObject.gameObject.AddComponent<AudioModifier>();
                modifier.Result = audioModifier;
                audioModifier.Init(audioClip, beatmapObject, modifier);
                audioModifier.pitch = modifier.GetFloat(2, 1f, variables);
                audioModifier.volume = modifier.GetFloat(3, 1f, variables);
                audioModifier.loop = modifier.GetBool(4, true, variables);
                audioModifier.timeOffset = modifier.GetBool(6, true, variables) ? AudioManager.inst.CurrentAudioSource.time + modifier.GetFloat(5, 0f, variables) : modifier.GetFloat(5, 0f, variables);
                audioModifier.lengthOffset = modifier.GetFloat(7, 0f, variables);
                audioModifier.playing = modifier.GetBool(8, true, variables);
                audioModifier.panStereo = modifier.GetFloat(9, 0f, variables);
                audioModifier.Tick();
            }));
        }

        public static void loadSoundAsset(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (modifier.constant && modifier.TryGetResult(out AudioSource cache) && cache)
            {
                cache.UnPause();
                return;
            }

            var name = modifier.GetValue(0, variables);
            var soundAsset = GameData.Current.assets.sounds.Find(x => x.name == name);
            if (!soundAsset)
                return;

            if (modifier.GetBool(1, true, variables))
            {
                if (soundAsset.audio)
                    return;

                var play = modifier.GetBool(2, false, variables);
                var pitch = modifier.GetFloat(3, 1f, variables);
                var vol = modifier.GetFloat(4, 1f, variables);
                var loop = modifier.GetBool(5, false, variables);
                var panStereo = modifier.GetFloat(6, 0f, variables);

                CoroutineHelper.StartCoroutine(soundAsset.LoadAudioClip(() =>
                {
                    if (play)
                        modifier.Result = SoundManager.inst.PlaySound(soundAsset.audio, vol, pitch, loop, panStereo);
                }));
            }
            else
                soundAsset.UnloadAudioClip();
        }

        #endregion

        #region Level

        public static void loadLevel(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            var path = modifier.GetValue(0, variables);

            if (CoreHelper.IsEditing)
            {
                if (!EditorConfig.Instance.ModifiersCanLoadLevels.Value)
                    return;

                RTEditor.inst.ShowWarningPopup($"You are about to enter the level {path}, are you sure you want to continue? Any unsaved progress will be lost!", () =>
                {
                    string str = RTFile.BasePath;
                    if (EditorConfig.Instance.ModifiersSavesBackup.Value)
                    {
                        GameData.Current.SaveData(str + "level-modifier-backup.lsb", () =>
                        {
                            EditorManager.inst.DisplayNotification($"Saved backup to {System.IO.Path.GetFileName(RTFile.RemoveEndSlash(str))}", 2f, EditorManager.NotificationType.Success);
                        });
                    }

                    EditorLevelManager.inst.LoadLevel(new Level(RTFile.CombinePaths(RTEditor.inst.BeatmapsPath, RTEditor.inst.EditorPath, path)));
                }, RTEditor.inst.HideWarningPopup);

                return;
            }

            if (CoreHelper.InEditor)
                return;

            var levelPath = RTFile.CombinePaths(RTFile.ApplicationDirectory, LevelManager.ListSlash, $"{path}");
            if (RTFile.FileExists(RTFile.CombinePaths(levelPath, Level.LEVEL_LSB)) || RTFile.FileExists(RTFile.CombinePaths(levelPath, Level.LEVEL_VGD)) || RTFile.FileExists(levelPath + FileFormat.ASSET.Dot()))
                LevelManager.Load(levelPath);
            else
                SoundManager.inst.PlaySound(DefaultSounds.Block);
        }

        public static void loadLevelID(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            var id = modifier.GetValue(0, variables);
            if (string.IsNullOrEmpty(id) || id == "0" || id == "-1")
                return;

            if (!CoreHelper.InEditor)
            {
                if (LevelManager.Levels.TryFind(x => x.id == id, out Level level))
                    LevelManager.Play(level);
                else
                    SoundManager.inst.PlaySound(DefaultSounds.Block);

                return;
            }

            if (!CoreHelper.IsEditing)
                return;

            if (EditorLevelManager.inst.LevelPanels.TryFind(x => x.Item && x.Item.metadata is MetaData metaData && metaData.ID == id, out LevelPanel levelPanel))
            {
                if (!EditorConfig.Instance.ModifiersCanLoadLevels.Value)
                    return;

                var path = System.IO.Path.GetFileName(levelPanel.Path);

                RTEditor.inst.ShowWarningPopup($"You are about to enter the level {path}, are you sure you want to continue? Any unsaved progress will be lost!", () =>
                {
                    string str = RTFile.BasePath;
                    if (EditorConfig.Instance.ModifiersSavesBackup.Value)
                    {
                        GameData.Current.SaveData(str + "level-modifier-backup.lsb", () =>
                        {
                            EditorManager.inst.DisplayNotification($"Saved backup to {System.IO.Path.GetFileName(RTFile.RemoveEndSlash(str))}", 2f, EditorManager.NotificationType.Success);
                        });
                    }

                    EditorLevelManager.inst.LoadLevel(levelPanel.Item);
                }, RTEditor.inst.HideWarningPopup);
            }
            else
                SoundManager.inst.PlaySound(DefaultSounds.Block);
        }

        public static void loadLevelInternal(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            var path = modifier.GetValue(0, variables);

            if (!CoreHelper.InEditor)
            {
                var filePath = RTFile.CombinePaths(RTFile.BasePath, path);
                if (!CoreHelper.InEditor && (RTFile.FileExists(RTFile.CombinePaths(filePath, Level.LEVEL_LSB)) || RTFile.FileIsFormat(RTFile.CombinePaths(filePath, Level.LEVEL_VGD)) || RTFile.FileExists(filePath + FileFormat.ASSET.Dot())))
                    LevelManager.Load(filePath);
                else
                    SoundManager.inst.PlaySound(DefaultSounds.Block);

                return;
            }

            if (CoreHelper.IsEditing && RTFile.FileExists(RTFile.CombinePaths(RTFile.BasePath, EditorManager.inst.currentLoadedLevel, path, Level.LEVEL_LSB)))
            {
                if (!EditorConfig.Instance.ModifiersCanLoadLevels.Value)
                    return;

                RTEditor.inst.ShowWarningPopup($"You are about to enter the level {RTFile.CombinePaths(EditorManager.inst.currentLoadedLevel, path)}, are you sure you want to continue? Any unsaved progress will be lost!", () =>
                {
                    string str = RTFile.BasePath;
                    if (EditorConfig.Instance.ModifiersSavesBackup.Value)
                    {
                        GameData.Current.SaveData(RTFile.CombinePaths(str, "level-modifier-backup.lsb"), () =>
                        {
                            EditorManager.inst.DisplayNotification($"Saved backup to {System.IO.Path.GetFileName(RTFile.RemoveEndSlash(str))}", 2f, EditorManager.NotificationType.Success);
                        });
                    }

                    EditorLevelManager.inst.LoadLevel(new Level(RTFile.CombinePaths(EditorManager.inst.currentLoadedLevel, path)));
                }, RTEditor.inst.HideWarningPopup);
            }
        }

        public static void loadLevelPrevious(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (CoreHelper.InEditor)
                return;

            LevelManager.Play(LevelManager.PreviousLevel);
        }

        public static void loadLevelHub(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (CoreHelper.InEditor)
                return;

            LevelManager.Play(LevelManager.Hub);
        }

        public static void loadLevelInCollection(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            var id = modifier.GetValue(0, variables);
            if (!CoreHelper.InEditor && LevelManager.CurrentLevelCollection && LevelManager.CurrentLevelCollection.levels.TryFind(x => x.id == id, out Level level))
                LevelManager.Play(level);
        }

        public static void loadLevelCollection(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            var id = modifier.GetValue(0, variables);
            if (CoreHelper.InEditor || !LevelManager.LevelCollections.TryFind(x => x.id == id, out LevelCollection levelCollection))
                return;

            var levelID = modifier.GetValue(1, variables);

            var entryLevelIndex = levelCollection.EntryLevelIndex;
            if (!string.IsNullOrEmpty(levelID) && LevelManager.Levels.TryFindIndex(x => x && x.id == levelID, out int arcadeLevelIndex))
                entryLevelIndex = arcadeLevelIndex;
            if (!string.IsNullOrEmpty(levelID) && Core.Managers.Networking.SteamWorkshopManager.inst.Levels.TryFindIndex(x => x && x.id == levelID, out int steamLevelIndex))
                entryLevelIndex = steamLevelIndex;

            if (entryLevelIndex < 0)
                return;

            levelCollection.DownloadLevel(levelCollection.levelInformation[entryLevelIndex], LevelManager.Play);
        }

        public static void downloadLevel(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            var levelInfo = new LevelInfo(modifier.GetValue(0, variables), modifier.GetValue(0, variables), modifier.GetValue(1, variables), modifier.GetValue(2, variables), modifier.GetValue(3, variables), modifier.GetValue(4, variables));

            if (!CoreHelper.InEditor)
            {
                if (LevelManager.Levels.TryFind(x => x.id == levelInfo.arcadeID, out Level level))
                {
                    LevelManager.Play(level);
                    return;
                }
            }

            if (CoreHelper.IsEditing)
            {
                if (EditorLevelManager.inst.LevelPanels.TryFind(x => x.Item && x.Item.metadata is MetaData metaData && metaData.ID == levelInfo.arcadeID, out LevelPanel levelPanel))
                {
                    if (!EditorConfig.Instance.ModifiersCanLoadLevels.Value)
                        return;

                    var path = System.IO.Path.GetFileName(levelPanel.Path);

                    RTEditor.inst.ShowWarningPopup($"You are about to enter the level {path}, are you sure you want to continue? Any unsaved progress will be lost!", () =>
                    {
                        string str = RTFile.BasePath;
                        if (EditorConfig.Instance.ModifiersSavesBackup.Value)
                        {
                            GameData.Current.SaveData(str + "level-modifier-backup.lsb", () =>
                            {
                                EditorManager.inst.DisplayNotification($"Saved backup to {System.IO.Path.GetFileName(RTFile.RemoveEndSlash(str))}", 2f, EditorManager.NotificationType.Success);
                            });
                        }

                        EditorLevelManager.inst.LoadLevel(levelPanel.Item);
                    }, RTEditor.inst.HideWarningPopup);
                    return;
                }
                return;
            }

            LevelCollection.DownloadLevel(null, levelInfo, level =>
            {
                if (modifier.GetBool(5, true, variables))
                    LevelManager.Play(level);
                else
                    RTBeatmap.Current.Resume(); // in case of softlock
            });
        }

        public static void endLevel(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (CoreHelper.InEditor)
            {
                EditorManager.inst.DisplayNotification("End level func", 1f, EditorManager.NotificationType.Success);
                return;
            }

            var endLevelFunc = modifier.GetInt(0, 0, variables);

            if (endLevelFunc > 0)
            {
                RTBeatmap.Current.endLevelFunc = (EndLevelFunction)(endLevelFunc - 1);
                RTBeatmap.Current.endLevelData = modifier.GetValue(1, variables);
            }
            RTBeatmap.Current.endLevelUpdateProgress = modifier.GetBool(2, true, variables);

            LevelManager.EndLevel();
        }

        public static void setAudioTransition(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables) => LevelManager.songFadeTransition = modifier.GetFloat(0, 0.5f, variables);

        public static void setIntroFade(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables) => RTGameManager.doIntroFade = modifier.GetBool(0, true, variables);

        public static void setLevelEndFunc(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (CoreHelper.InEditor)
                return;

            var endLevelFunc = modifier.GetInt(0, 0, variables);

            if (endLevelFunc > 0)
            {
                RTBeatmap.Current.endLevelFunc = (EndLevelFunction)(endLevelFunc - 1);
                RTBeatmap.Current.endLevelData = modifier.GetValue(1, variables);
            }
            RTBeatmap.Current.endLevelUpdateProgress = modifier.GetBool(2, true, variables);
        }

        public static void getCurrentLevelID(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (LevelManager.CurrentLevel)
                variables[modifier.GetValue(0)] = LevelManager.CurrentLevel.id;
            if (CoreHelper.InEditor && EditorLevelManager.inst.CurrentLevel)
                variables[modifier.GetValue(0)] = EditorLevelManager.inst.CurrentLevel.id;
        }
        
        public static void getCurrentLevelName(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (LevelManager.CurrentLevel && LevelManager.CurrentLevel.metadata)
                variables[modifier.GetValue(0)] = LevelManager.CurrentLevel.metadata.beatmap.name;
            if (CoreHelper.InEditor && EditorLevelManager.inst.CurrentLevel && EditorLevelManager.inst.CurrentLevel.metadata)
                variables[modifier.GetValue(0)] = EditorLevelManager.inst.CurrentLevel.metadata.beatmap.name;
        }

        public static void getCurrentLevelRank(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            variables[modifier.GetValue(0)] = LevelManager.GetLevelRank(RTBeatmap.Current.hits).Ordinal.ToString();
        }

        #endregion

        #region Component

        public static void blur(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not BeatmapObject beatmapObject)
                return;

            if (beatmapObject.objectType == BeatmapObject.ObjectType.Empty)
                return;

            var runtimeObject = beatmapObject.runtimeObject;

            if (!runtimeObject || !runtimeObject.visualObject.renderer)
                return;

            var amount = modifier.GetFloat(0, 0f, variables);
            var renderer = runtimeObject.visualObject.renderer;

            if (!modifier.HasResult())
            {
                DestroyModifierResult.Init(runtimeObject.visualObject.gameObject, modifier);
                modifier.Result = runtimeObject.visualObject.gameObject;
                renderer.material = LegacyResources.blur;
            }

            if (modifier.GetBool(1, false, variables))
                renderer.material.SetFloat("_blurSizeXY", -(beatmapObject.Interpolate(3, 1) - 1f) * amount);
            else
                renderer.material.SetFloat("_blurSizeXY", amount);
        }

        public static void blurOther(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not IPrefabable prefabable)
                return;

            var list = GameData.Current.FindObjectsWithTag(modifier, prefabable, modifier.GetValue(1, variables));
            if (list.IsEmpty())
                return;

            var amount = modifier.GetFloat(0, 0f, variables);

            foreach (var beatmapObject in list)
            {
                var runtimeObject = beatmapObject.runtimeObject;
                if (beatmapObject.objectType == BeatmapObject.ObjectType.Empty || !runtimeObject || !runtimeObject.visualObject.renderer)
                    continue;

                var renderer = runtimeObject.visualObject.renderer;

                if (renderer.material != LegacyResources.blur)
                    renderer.material = LegacyResources.blur;
                renderer.material.SetFloat("_blurSizeXY", -(beatmapObject.Interpolate(3, 1) - 1f) * amount);
            }
        }

        public static void blurVariable(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not BeatmapObject beatmapObject)
                return;

            if (beatmapObject.objectType == BeatmapObject.ObjectType.Empty)
                return;

            var runtimeObject = beatmapObject.runtimeObject;

            if (!runtimeObject || !runtimeObject.visualObject.renderer)
                return;

            var amount = modifier.GetFloat(0, 0f, variables);
            var renderer = runtimeObject.visualObject.renderer;

            if (!modifier.HasResult())
            {
                var onDestroy = runtimeObject.visualObject.gameObject.AddComponent<DestroyModifierResult>();
                onDestroy.Modifier = modifier;
                modifier.Result = runtimeObject.visualObject.gameObject;
                renderer.material = LegacyResources.blur;
            }

            renderer.material.SetFloat("_blurSizeXY", beatmapObject.integerVariable * amount);
        }

        public static void blurVariableOther(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not IPrefabable prefabable)
                return;

            var list = GameData.Current.FindObjectsWithTag(modifier, prefabable, modifier.GetValue(1));
            if (list.IsEmpty())
                return;

            var amount = modifier.GetFloat(0, 0f, variables);

            foreach (var beatmapObject in list)
            {
                var runtimeObject = beatmapObject.runtimeObject;
                if (beatmapObject.objectType == BeatmapObject.ObjectType.Empty || !runtimeObject || !runtimeObject.visualObject.renderer)
                    continue;

                var renderer = runtimeObject.visualObject.renderer;

                if (renderer.material != LegacyResources.blur)
                    renderer.material = LegacyResources.blur;
                renderer.material.SetFloat("_blurSizeXY", beatmapObject.integerVariable * amount);
            }
        }

        public static void blurColored(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not BeatmapObject beatmapObject)
                return;

            if (beatmapObject.objectType == BeatmapObject.ObjectType.Empty)
                return;

            var runtimeObject = beatmapObject.runtimeObject;

            if (!runtimeObject || !runtimeObject.visualObject.renderer)
                return;

            var amount = modifier.GetFloat(0, 0f, variables);
            var renderer = runtimeObject.visualObject.renderer;

            if (!modifier.HasResult())
            {
                var onDestroy = runtimeObject.visualObject.gameObject.AddComponent<DestroyModifierResult>();
                onDestroy.Modifier = modifier;
                modifier.Result = runtimeObject.visualObject.gameObject;
                renderer.material.shader = LegacyResources.blurColored;
            }

            if (modifier.GetBool(1, false, variables))
                renderer.material.SetFloat("_Size", -(beatmapObject.Interpolate(3, 1) - 1f) * amount);
            else
                renderer.material.SetFloat("_Size", amount);
        }

        public static void blurColoredOther(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not IPrefabable prefabable)
                return;

            var list = GameData.Current.FindObjectsWithTag(modifier, prefabable, modifier.GetValue(1, variables));
            if (list.IsEmpty())
                return;

            var amount = modifier.GetFloat(0, 0f, variables);

            foreach (var beatmapObject in list)
            {
                var runtimeObject = beatmapObject.runtimeObject;
                if (beatmapObject.objectType == BeatmapObject.ObjectType.Empty || !runtimeObject || !runtimeObject.visualObject.renderer)
                    continue;

                var renderer = runtimeObject.visualObject.renderer;

                if (renderer.material != LegacyResources.blurColored)
                    renderer.material.shader = LegacyResources.blurColored;
                renderer.material.SetFloat("_Size", -(beatmapObject.Interpolate(3, 1) - 1f) * amount);
            }
        }

        public static void doubleSided(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not BeatmapObject beatmapObject)
                return;

            var runtimeObject = beatmapObject.runtimeObject;
            if (runtimeObject && runtimeObject.visualObject is SolidObject solidObject && solidObject.gameObject)
                solidObject.UpdateRendering((int)beatmapObject.gradientType, (int)beatmapObject.renderLayerType, true, beatmapObject.gradientScale, beatmapObject.gradientRotation, (int)beatmapObject.colorBlendMode);
        }

        public static void particleSystem(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not BeatmapObject beatmapObject)
                return;

            var runtimeObject = beatmapObject.runtimeObject;
            if (!runtimeObject || !runtimeObject.visualObject || !runtimeObject.visualObject.gameObject)
                return;

            var gameObject = runtimeObject.visualObject.gameObject;

            if (modifier.Result is not ParticleSystem a || !a)
            {
                //var solidObject = runtimeObject.visualObject as SolidObject;
                var ps = gameObject.GetOrAddComponent<ParticleSystem>();
                var psr = gameObject.GetComponent<ParticleSystemRenderer>();

                var s = modifier.GetInt(1, 0, variables);
                var so = modifier.GetInt(2, 0, variables);

                s = Mathf.Clamp(s, 0, ObjectManager.inst.objectPrefabs.Count - 1);
                so = Mathf.Clamp(so, 0, ObjectManager.inst.objectPrefabs[s].options.Count - 1);

                psr.mesh = ObjectManager.inst.objectPrefabs[s == 4 ? 0 : s == 6 ? 0 : s].options[so].GetComponentInChildren<MeshFilter>().mesh;

                psr.material = GameManager.inst.PlayerPrefabs[0].transform.GetChild(0).GetChild(0).GetComponent<TrailRenderer>().material;
                //psr.material = LegacyResources.GetObjectMaterial(solidObject && solidObject.doubleSided, solidObject?.gradientType ?? 0, solidObject?.colorBlendMode ?? 0);
                psr.material.color = Color.white;
                psr.trailMaterial = psr.material;
                psr.renderMode = ParticleSystemRenderMode.Mesh;

                var psMain = ps.main;

                psMain.simulationSpace = ParticleSystemSimulationSpace.World;

                var rotationOverLifetime = ps.rotationOverLifetime;
                rotationOverLifetime.enabled = true;
                rotationOverLifetime.separateAxes = true;
                rotationOverLifetime.xMultiplier = 0f;
                rotationOverLifetime.yMultiplier = 0f;

                var forceOverLifetime = ps.forceOverLifetime;
                forceOverLifetime.enabled = true;
                forceOverLifetime.space = ParticleSystemSimulationSpace.World;

                modifier.Result = ps;
                gameObject.AddComponent<DestroyModifierResult>().Modifier = modifier;
            }

            if (modifier.Result is ParticleSystem particleSystem && particleSystem)
            {
                var ps = particleSystem;

                var psMain = ps.main;
                var psEmission = ps.emission;

                psMain.startSpeed = modifier.GetFloat(9, 5f, variables);

                psMain.loop = modifier.constant;
                ps.emissionRate = modifier.GetFloat(10, 1f, variables);
                //psEmission.burstCount = modifier.GetInt(16, 1, variables);
                psMain.duration = modifier.GetFloat(11, 1f, variables);

                var rotationOverLifetime = ps.rotationOverLifetime;
                rotationOverLifetime.zMultiplier = modifier.GetFloat(8, 0f, variables);

                var forceOverLifetime = ps.forceOverLifetime;
                forceOverLifetime.xMultiplier = modifier.GetFloat(12, 0f, variables);
                forceOverLifetime.yMultiplier = modifier.GetFloat(13, 0f, variables);

                var particlesTrail = ps.trails;
                particlesTrail.enabled = modifier.GetBool(14, true, variables);

                var colorOverLifetime = ps.colorOverLifetime;
                colorOverLifetime.enabled = true;
                var psCol = colorOverLifetime.color;

                float alphaStart = modifier.GetFloat(4, 1f, variables);
                float alphaEnd = modifier.GetFloat(5, 0f, variables);

                psCol.gradient.alphaKeys = new GradientAlphaKey[2] { new GradientAlphaKey(alphaStart, 0f), new GradientAlphaKey(alphaEnd, 1f) };
                psCol.gradient.colorKeys = new GradientColorKey[2] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) };
                psCol.gradient.mode = GradientMode.Blend;

                colorOverLifetime.color = psCol;

                var sizeOverLifetime = ps.sizeOverLifetime;
                sizeOverLifetime.enabled = true;

                var ssss = sizeOverLifetime.size;

                var sizeStart = modifier.GetFloat(6, 0f, variables);
                var sizeEnd = modifier.GetFloat(7, 0f, variables);

                var curve = new AnimationCurve(new Keyframe[2] { new Keyframe(0f, sizeStart), new Keyframe(1f, sizeEnd) });

                ssss.curve = curve;

                sizeOverLifetime.size = ssss;

                psMain.startLifetime = modifier.GetFloat(0, 1f, variables);
                psEmission.enabled = !(gameObject.transform.lossyScale.x < 0.001f && gameObject.transform.lossyScale.x > -0.001f || gameObject.transform.lossyScale.y < 0.001f && gameObject.transform.lossyScale.y > -0.001f) && gameObject.activeSelf && gameObject.activeInHierarchy;

                psMain.startColor = CoreHelper.CurrentBeatmapTheme.GetObjColor(modifier.GetInt(3, 0, variables));

                var shape = ps.shape;
                shape.angle = modifier.GetFloat(15, 90f, variables);

                if (!modifier.constant)
                    RTLevel.Current.postTick.Enqueue(() => ps.Emit(modifier.GetInt(16, 1, variables)));
            }
        }

        public static void particleSystemHex(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not BeatmapObject beatmapObject)
                return;

            var runtimeObject = beatmapObject.runtimeObject;
            if (!runtimeObject || !runtimeObject.visualObject || !runtimeObject.visualObject.gameObject)
                return;

            var gameObject = runtimeObject.visualObject.gameObject;

            if (modifier.Result is not ParticleSystem a || !a)
            {
                //var solidObject = runtimeObject.visualObject as SolidObject;
                var ps = gameObject.GetOrAddComponent<ParticleSystem>();
                var psr = gameObject.GetComponent<ParticleSystemRenderer>();

                var s = modifier.GetInt(1, 0, variables);
                var so = modifier.GetInt(2, 0, variables);

                s = Mathf.Clamp(s, 0, ObjectManager.inst.objectPrefabs.Count - 1);
                so = Mathf.Clamp(so, 0, ObjectManager.inst.objectPrefabs[s].options.Count - 1);

                psr.mesh = ObjectManager.inst.objectPrefabs[s == 4 ? 0 : s == 6 ? 0 : s].options[so].GetComponentInChildren<MeshFilter>().mesh;

                psr.material = GameManager.inst.PlayerPrefabs[0].transform.GetChild(0).GetChild(0).GetComponent<TrailRenderer>().material;
                //psr.material = LegacyResources.GetObjectMaterial(solidObject && solidObject.doubleSided, solidObject?.gradientType ?? 0, solidObject?.colorBlendMode ?? 0);
                psr.material.color = Color.white;
                psr.trailMaterial = psr.material;
                psr.renderMode = ParticleSystemRenderMode.Mesh;

                var psMain = ps.main;

                psMain.simulationSpace = ParticleSystemSimulationSpace.World;

                var rotationOverLifetime = ps.rotationOverLifetime;
                rotationOverLifetime.enabled = true;
                rotationOverLifetime.separateAxes = true;
                rotationOverLifetime.xMultiplier = 0f;
                rotationOverLifetime.yMultiplier = 0f;

                var forceOverLifetime = ps.forceOverLifetime;
                forceOverLifetime.enabled = true;
                forceOverLifetime.space = ParticleSystemSimulationSpace.World;

                modifier.Result = ps;
                gameObject.AddComponent<DestroyModifierResult>().Modifier = modifier;
            }

            if (modifier.Result is ParticleSystem particleSystem && particleSystem)
            {
                var ps = particleSystem;

                var psMain = ps.main;
                var psEmission = ps.emission;

                psMain.startSpeed = modifier.GetFloat(9, 5f, variables);

                psMain.loop = modifier.constant;
                ps.emissionRate = modifier.GetFloat(10, 1f, variables);
                //psEmission.burstCount = modifier.GetInt(16, 1, variables);
                psMain.duration = modifier.GetFloat(11, 1f, variables);

                var rotationOverLifetime = ps.rotationOverLifetime;
                rotationOverLifetime.zMultiplier = modifier.GetFloat(8, 0f, variables);

                var forceOverLifetime = ps.forceOverLifetime;
                forceOverLifetime.xMultiplier = modifier.GetFloat(12, 0f, variables);
                forceOverLifetime.yMultiplier = modifier.GetFloat(13, 0f, variables);

                var particlesTrail = ps.trails;
                particlesTrail.enabled = modifier.GetBool(14, true, variables);

                var colorOverLifetime = ps.colorOverLifetime;
                colorOverLifetime.enabled = true;
                var psCol = colorOverLifetime.color;

                float alphaStart = modifier.GetFloat(4, 1f, variables);
                float alphaEnd = modifier.GetFloat(5, 0f, variables);

                psCol.gradient.alphaKeys = new GradientAlphaKey[2] { new GradientAlphaKey(alphaStart, 0f), new GradientAlphaKey(alphaEnd, 1f) };
                psCol.gradient.colorKeys = new GradientColorKey[2] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) };
                psCol.gradient.mode = GradientMode.Blend;

                colorOverLifetime.color = psCol;

                var sizeOverLifetime = ps.sizeOverLifetime;
                sizeOverLifetime.enabled = true;

                var ssss = sizeOverLifetime.size;

                var sizeStart = modifier.GetFloat(6, 0f, variables);
                var sizeEnd = modifier.GetFloat(7, 0f, variables);

                var curve = new AnimationCurve(new Keyframe[2] { new Keyframe(0f, sizeStart), new Keyframe(1f, sizeEnd) });

                ssss.curve = curve;

                sizeOverLifetime.size = ssss;

                psMain.startLifetime = modifier.GetFloat(0, 1f, variables);
                psEmission.enabled = !(gameObject.transform.lossyScale.x < 0.001f && gameObject.transform.lossyScale.x > -0.001f || gameObject.transform.lossyScale.y < 0.001f && gameObject.transform.lossyScale.y > -0.001f) && gameObject.activeSelf && gameObject.activeInHierarchy;

                psMain.startColor = RTColors.HexToColor(modifier.GetValue(3, variables));

                var shape = ps.shape;
                shape.angle = modifier.GetFloat(15, 90f, variables);

                if (!modifier.constant)
                    RTLevel.Current.postTick.Enqueue(() => ps.Emit(modifier.GetInt(16, 1, variables)));
            }
        }

        public static void trailRenderer(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not BeatmapObject beatmapObject)
                return;

            var runtimeObject = beatmapObject.runtimeObject;
            if (!runtimeObject || !runtimeObject.visualObject || !runtimeObject.visualObject.gameObject)
                return;

            var gameObject = runtimeObject.visualObject.gameObject;

            if (!beatmapObject.trailRenderer)
            {
                beatmapObject.trailRenderer = gameObject.GetOrAddComponent<TrailRenderer>();

                beatmapObject.trailRenderer.material = GameManager.inst.PlayerPrefabs[0].transform.GetChild(0).GetChild(0).GetComponent<TrailRenderer>().material;
                beatmapObject.trailRenderer.material.color = Color.white;
            }
            else
            {
                var tr = beatmapObject.trailRenderer;

                tr.time = modifier.GetFloat(0, 1f, variables);
                tr.emitting = !(gameObject.transform.lossyScale.x < 0.001f && gameObject.transform.lossyScale.x > -0.001f || gameObject.transform.lossyScale.y < 0.001f && gameObject.transform.lossyScale.y > -0.001f) && gameObject.activeSelf && gameObject.activeInHierarchy;

                var t = gameObject.transform.lossyScale.magnitude * 0.576635f;
                tr.startWidth = modifier.GetFloat(1, 1f, variables) * t;
                tr.endWidth = modifier.GetFloat(2, 1f, variables) * t;

                var beatmapTheme = CoreHelper.CurrentBeatmapTheme;

                tr.startColor = RTColors.FadeColor(beatmapTheme.GetObjColor(modifier.GetInt(3, 0, variables)), modifier.GetFloat(4, 1f, variables));
                tr.endColor = RTColors.FadeColor(beatmapTheme.GetObjColor(modifier.GetInt(5, 0, variables)), modifier.GetFloat(6, 1f, variables));
            }
        }

        public static void trailRendererHex(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not BeatmapObject beatmapObject)
                return;

            var runtimeObject = beatmapObject.runtimeObject;
            if (!runtimeObject || !runtimeObject.visualObject || !runtimeObject.visualObject.gameObject)
                return;

            var gameObject = runtimeObject.visualObject.gameObject;

            if (!beatmapObject.trailRenderer)
            {
                beatmapObject.trailRenderer = gameObject.GetOrAddComponent<TrailRenderer>();

                beatmapObject.trailRenderer.material = GameManager.inst.PlayerPrefabs[0].transform.GetChild(0).GetChild(0).GetComponent<TrailRenderer>().material;
                beatmapObject.trailRenderer.material.color = Color.white;
            }
            else
            {
                var tr = beatmapObject.trailRenderer;

                tr.time = modifier.GetFloat(0, 1f, variables);
                tr.emitting = !(gameObject.transform.lossyScale.x < 0.001f && gameObject.transform.lossyScale.x > -0.001f || gameObject.transform.lossyScale.y < 0.001f && gameObject.transform.lossyScale.y > -0.001f) && gameObject.activeSelf && gameObject.activeInHierarchy;

                var t = gameObject.transform.lossyScale.magnitude * 0.576635f;
                tr.startWidth = modifier.GetFloat(1, 1f, variables) * t;
                tr.endWidth = modifier.GetFloat(2, 1f, variables) * t;

                tr.startColor = RTColors.HexToColor(modifier.GetValue(3, variables));
                tr.endColor = RTColors.HexToColor(modifier.GetValue(4, variables));
            }
        }

        public static void rigidbody(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not BeatmapObject beatmapObject)
                return;

            var runtimeObject = beatmapObject.runtimeObject;
            if (!runtimeObject || !runtimeObject.visualObject || !runtimeObject.visualObject.gameObject)
                return;

            var gravity = modifier.GetFloat(1, 0f, variables);
            var collisionMode = modifier.GetInt(2, 0, variables);
            var drag = modifier.GetFloat(3, 0f, variables);
            var velocityX = modifier.GetFloat(4, 0f, variables);
            var velocityY = modifier.GetFloat(5, 0f, variables);
            var bodyType = modifier.GetInt(6, 0, variables);

            if (!beatmapObject.rigidbody)
                beatmapObject.rigidbody = runtimeObject.visualObject.gameObject.GetOrAddComponent<Rigidbody2D>();

            beatmapObject.rigidbody.gravityScale = gravity;
            beatmapObject.rigidbody.collisionDetectionMode = (CollisionDetectionMode2D)Mathf.Clamp(collisionMode, 0, 1);
            beatmapObject.rigidbody.drag = drag;

            beatmapObject.rigidbody.bodyType = (RigidbodyType2D)Mathf.Clamp(bodyType, 0, 2);

            beatmapObject.rigidbody.velocity += new Vector2(velocityX, velocityY);
        }

        public static void rigidbodyOther(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not IPrefabable prefabable)
                return;

            var list = GameData.Current.FindObjectsWithTag(modifier, prefabable, modifier.GetValue(0, variables));
            if (list.IsEmpty())
                return;

            var gravity = modifier.GetFloat(1, 0f, variables);
            var collisionMode = modifier.GetInt(2, 0, variables);
            var drag = modifier.GetFloat(3, 0f, variables);
            var velocityX = modifier.GetFloat(4, 0f, variables);
            var velocityY = modifier.GetFloat(5, 0f, variables);
            var bodyType = modifier.GetInt(6, 0, variables);

            foreach (var beatmapObject in list)
            {
                var runtimeObject = beatmapObject.runtimeObject;
                if (beatmapObject.objectType == BeatmapObject.ObjectType.Empty || !runtimeObject || !runtimeObject.visualObject.renderer)
                    continue;

                if (!beatmapObject.rigidbody)
                    beatmapObject.rigidbody = runtimeObject.visualObject.gameObject.GetOrAddComponent<Rigidbody2D>();

                beatmapObject.rigidbody.gravityScale = gravity;
                beatmapObject.rigidbody.collisionDetectionMode = (CollisionDetectionMode2D)Mathf.Clamp(collisionMode, 0, 1);
                beatmapObject.rigidbody.drag = drag;

                beatmapObject.rigidbody.bodyType = (RigidbodyType2D)Mathf.Clamp(bodyType, 0, 2);

                beatmapObject.rigidbody.velocity += new Vector2(velocityX, velocityY);
            }
        }

        public static void setRenderType(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is BeatmapObject beatmapObject && beatmapObject.runtimeObject && beatmapObject.runtimeObject.visualObject)
                beatmapObject.runtimeObject.visualObject.SetRenderType(modifier.GetInt(0, 0, variables));
        }

        public static void setRenderTypeOther(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not IPrefabable prefabable)
                return;

            var list = GameData.Current.FindObjectsWithTag(modifier, prefabable, modifier.GetValue(0, variables));
            if (list.IsEmpty())
                return;

            var renderType = modifier.GetInt(1, 0, variables);
            foreach (var beatmapObject in list)
            {
                if (beatmapObject.runtimeObject && beatmapObject.runtimeObject.visualObject)
                    beatmapObject.runtimeObject.visualObject.SetRenderType(renderType);
            }
        }

        public static void setRendering(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not BeatmapObject beatmapObject || !beatmapObject.runtimeObject || beatmapObject.runtimeObject.visualObject is not SolidObject solidObject || !solidObject.gameObject)
                return;

            var doubleSided = modifier.GetBool(0, false, variables);
            var gradientType = modifier.GetInt(1, 0, variables);
            var colorBlendMode = modifier.GetInt(2, 0, variables);
            var gradientScale = modifier.GetFloat(3, 1f, variables);
            var gradientRotation = modifier.GetFloat(4, 0f, variables);

            if (modifier.constant)
            {
                var cache = modifier.GetResultOrDefault(() =>
                {
                    var cache = new RenderingCache();
                    cache.UpdateCache(doubleSided, gradientType, colorBlendMode, gradientScale, gradientRotation);
                    cache.Apply(solidObject);
                    DestroyModifierResult.Init(solidObject.gameObject, modifier);
                    return cache;
                });

                if (!cache.Is(doubleSided, gradientType, colorBlendMode, gradientScale, gradientRotation))
                {
                    cache.UpdateCache(doubleSided, gradientType, colorBlendMode, gradientScale, gradientRotation);
                    cache.Apply(solidObject);
                }
            }
            else
            {
                solidObject.UpdateRendering(
                    gradientType: gradientType,
                    renderType: solidObject.gameObject.layer switch
                    {
                        RTLevel.FOREGROUND_LAYER => (int)BeatmapObject.RenderLayerType.Foreground,
                        RTLevel.BACKGROUND_LAYER => (int)BeatmapObject.RenderLayerType.Background,
                        RTLevel.UI_LAYER => (int)BeatmapObject.RenderLayerType.UI,
                        _ => 0,
                    },
                    doubleSided: doubleSided,
                    gradientScale: gradientScale,
                    gradientRotation: gradientRotation,
                    colorBlendMode: colorBlendMode);
            }
        }

        public static void setOutline(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not BeatmapObject beatmapObject || !beatmapObject.runtimeObject || beatmapObject.runtimeObject.visualObject is not SolidObject solidObject || !solidObject.gameObject)
                return;

            var enabled = modifier.GetBool(0, true, variables);
            var width = modifier.GetFloat(1, 0.5f, variables);
            var index = modifier.GetInt(2, 0, variables);
            var opacity = modifier.GetFloat(3, 0f, variables);
            var hue = modifier.GetFloat(4, 0f, variables);
            var sat = modifier.GetFloat(5, 0f, variables);
            var val = modifier.GetFloat(6, 0f, variables);

            if (enabled)
            {
                solidObject.AddOutline();
                solidObject.SetOutline(RTColors.FadeColor(RTColors.ChangeColorHSV(CoreHelper.CurrentBeatmapTheme.GetObjColor(index), hue, sat, val), opacity), width);
            }
            else
                solidObject.RemoveOutline();
        }

        public static void setOutlineOther(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not IPrefabable prefabable)
                return;

            List<BeatmapObject> list = modifier.GetResultOrDefault(() => GameData.Current.FindObjectsWithTag(modifier, prefabable, modifier.GetValue(0, variables)));

            if (list.IsEmpty())
                return;

            var enabled = modifier.GetBool(1, true, variables);
            var width = modifier.GetFloat(2, 0.5f, variables);
            var index = modifier.GetInt(3, 0, variables);
            var opacity = modifier.GetFloat(4, 0f, variables);
            var hue = modifier.GetFloat(5, 0f, variables);
            var sat = modifier.GetFloat(6, 0f, variables);
            var val = modifier.GetFloat(7, 0f, variables);

            foreach (var beatmapObject in list)
            {
                if (!beatmapObject.runtimeObject || beatmapObject.runtimeObject.visualObject is not SolidObject solidObject || !solidObject.gameObject)
                    continue;

                if (enabled)
                {
                    solidObject.AddOutline();
                    solidObject.SetOutline(RTColors.FadeColor(RTColors.ChangeColorHSV(CoreHelper.CurrentBeatmapTheme.GetObjColor(index), hue, sat, val), opacity), width);
                }
                else
                    solidObject.RemoveOutline();
            }
        }

        public static void setOutlineHex(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not BeatmapObject beatmapObject || !beatmapObject.runtimeObject || beatmapObject.runtimeObject.visualObject is not SolidObject solidObject || !solidObject.gameObject)
                return;

            var enabled = modifier.GetBool(0, true, variables);
            var width = modifier.GetFloat(1, 0.5f, variables);
            var hex = RTColors.HexToColor(modifier.GetValue(2, variables));

            if (enabled)
            {
                solidObject.AddOutline();
                solidObject.SetOutline(hex, width);
            }
            else
                solidObject.RemoveOutline();
        }

        public static void setOutlineHexOther(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not IPrefabable prefabable)
                return;

            List<BeatmapObject> list = modifier.GetResultOrDefault(() => GameData.Current.FindObjectsWithTag(modifier, prefabable, modifier.GetValue(0, variables)));

            if (list.IsEmpty())
                return;

            var enabled = modifier.GetBool(1, true, variables);
            var width = modifier.GetFloat(2, 0.5f, variables);
            var hex = RTColors.HexToColor(modifier.GetValue(3, variables));

            foreach (var beatmapObject in list)
            {
                if (!beatmapObject.runtimeObject || beatmapObject.runtimeObject.visualObject is not SolidObject solidObject || !solidObject.gameObject)
                    continue;

                if (enabled)
                {
                    solidObject.AddOutline();
                    solidObject.SetOutline(hex, width);
                }
                else
                    solidObject.RemoveOutline();
            }
        }

        #endregion

        #region Player

        public static void playerHit(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (RTBeatmap.Current.Invincible || modifier.constant || reference is not BeatmapObject beatmapObject)
                return;

            // queue post tick so the position of the object is accurate.
            RTLevel.Current.postTick.Enqueue(() =>
            {
                var pos = beatmapObject.GetFullPosition();
                var player = PlayerManager.GetClosestPlayer(pos);
                player?.RuntimePlayer?.Hit(Mathf.Clamp(modifier.GetInt(0, 1, variables), 0, int.MaxValue));
            });
        }

        public static void playerHitIndex(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (RTBeatmap.Current.Invincible || modifier.constant)
                return;

            var damage = Mathf.Clamp(modifier.GetInt(1, 1, variables), 0, int.MaxValue);
            if (PlayerManager.Players.TryGetAt(modifier.GetInt(0, 0, variables), out PAPlayer player))
                player.RuntimePlayer?.Hit(damage);
        }

        public static void playerHitAll(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (RTBeatmap.Current.Invincible || modifier.constant)
                return;

            var damage = Mathf.Clamp(modifier.GetInt(0, 1, variables), 0, int.MaxValue);
            foreach (var player in PlayerManager.Players)
                player.RuntimePlayer?.Hit(damage);
        }

        public static void playerHeal(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (RTBeatmap.Current.Invincible || modifier.constant || reference is not BeatmapObject beatmapObject)
                return;

            var heal = Mathf.Clamp(modifier.GetInt(0, 1, variables), 0, int.MaxValue);

            // queue post tick so the position of the object is accurate.
            RTLevel.Current.postTick.Enqueue(() =>
            {
                var pos = beatmapObject.GetFullPosition();
                var player = PlayerManager.GetClosestPlayer(pos);
                player?.RuntimePlayer?.Heal(heal);
            });
        }

        public static void playerHealIndex(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (RTBeatmap.Current.Invincible || modifier.constant)
                return;

            var health = Mathf.Clamp(modifier.GetInt(1, 1, variables), 0, int.MaxValue);
            if (PlayerManager.Players.TryGetAt(modifier.GetInt(0, 0, variables), out PAPlayer player) && player.RuntimePlayer)
                player.RuntimePlayer.Heal(health);
        }

        public static void playerHealAll(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (RTBeatmap.Current.Invincible || modifier.constant)
                return;

            var heal = Mathf.Clamp(modifier.GetInt(0, 1, variables), 0, int.MaxValue);
            bool healed = false;
            foreach (var player in PlayerManager.Players)
            {
                if (player.RuntimePlayer && player.RuntimePlayer.Heal(heal, false))
                    healed = true;
            }

            if (healed)
                SoundManager.inst.PlaySound(DefaultSounds.HealPlayer);
        }

        public static void playerKill(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (RTBeatmap.Current.Invincible || modifier.constant || reference is not BeatmapObject beatmapObject)
                return;

            // queue post tick so the position of the object is accurate.
            RTLevel.Current.postTick.Enqueue(() =>
            {
                var pos = beatmapObject.GetFullPosition();
                var player = PlayerManager.GetClosestPlayer(pos);

                if (player && player.RuntimePlayer)
                    player.RuntimePlayer.Kill();
            });
        }

        public static void playerKillIndex(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (RTBeatmap.Current.Invincible || modifier.constant)
                return;

            if (PlayerManager.Players.TryGetAt(modifier.GetInt(0, 0, variables), out PAPlayer player) && player.RuntimePlayer)
                player.RuntimePlayer.Kill();
        }

        public static void playerKillAll(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (RTBeatmap.Current.Invincible || modifier.constant)
                return;

            foreach (var player in PlayerManager.Players)
                if (player.RuntimePlayer)
                    player.RuntimePlayer.Kill();
        }

        public static void playerRespawn(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (modifier.constant || reference is not BeatmapObject beatmapObject)
                return;

            // queue post tick so the position of the object is accurate.
            RTLevel.Current.postTick.Enqueue(() =>
            {
                var pos = beatmapObject.GetFullPosition();
                var playerIndex = PlayerManager.GetClosestPlayerIndex(pos);

                if (playerIndex >= 0)
                    PlayerManager.RespawnPlayer(playerIndex);
            });
        }

        public static void playerRespawnIndex(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (!modifier.constant)
                PlayerManager.RespawnPlayer(modifier.GetInt(0, 0));
        }

        public static void playerRespawnAll(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (!modifier.constant)
                PlayerManager.RespawnPlayers();
        }

        public static void playerLockX(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not BeatmapObject beatmapObject)
                return;

            var locked = modifier.GetBool(0, true, variables);

            // queue post tick so the position of the object is accurate.
            RTLevel.Current.postTick.Enqueue(() =>
            {
                var pos = beatmapObject.GetFullPosition();
                var player = PlayerManager.GetClosestPlayer(pos);

                if (player && player.RuntimePlayer)
                    player.RuntimePlayer.LockXMovement = locked;
            });
        }

        public static void playerLockXIndex(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            var locked = modifier.GetBool(1, true, variables);

            if (PlayerManager.Players.TryGetAt(modifier.GetInt(0, 0, variables), out PAPlayer player) && player.RuntimePlayer)
                player.RuntimePlayer.LockXMovement = locked;
        }

        public static void playerLockXAll(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            var locked = modifier.GetBool(0, true, variables);
            PlayerManager.Players.ForLoop(player =>
            {
                if (player.RuntimePlayer)
                    player.RuntimePlayer.LockXMovement = locked;
            });
        }

        public static void playerLockY(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not BeatmapObject beatmapObject)
                return;

            var locked = modifier.GetBool(0, true, variables);

            // queue post tick so the position of the object is accurate.
            RTLevel.Current.postTick.Enqueue(() =>
            {
                var pos = beatmapObject.GetFullPosition();
                var player = PlayerManager.GetClosestPlayer(pos);

                if (player && player.RuntimePlayer)
                    player.RuntimePlayer.LockYMovement = locked;
            });
        }

        public static void playerLockYIndex(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            var locked = modifier.GetBool(1, true, variables);

            if (PlayerManager.Players.TryGetAt(modifier.GetInt(0, 0, variables), out PAPlayer player) && player.RuntimePlayer)
                player.RuntimePlayer.LockYMovement = locked;
        }

        public static void playerLockYAll(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            var locked = modifier.GetBool(0, true, variables);
            PlayerManager.Players.ForLoop(player =>
            {
                if (player.RuntimePlayer)
                    player.RuntimePlayer.LockYMovement = locked;
            });
        }

        public static void playerLockBoostAll(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (modifier.values.Count > 3 && !string.IsNullOrEmpty(modifier.GetValue(1)) && bool.TryParse(modifier.GetValue(0, variables), out bool lockBoost))
                RTPlayer.LockBoost = lockBoost;
        }

        public static void playerEnable(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not BeatmapObject beatmapObject)
                return;

            var enabled = modifier.GetBool(0, true, variables);

            // queue post tick so the position of the object is accurate.
            RTLevel.Current.postTick.Enqueue(() =>
            {
                var pos = beatmapObject.GetFullPosition();
                var player = PlayerManager.GetClosestPlayer(pos);

                if (player && player.RuntimePlayer)
                    player.SetCustomActive(enabled);
            });
        }

        public static void playerEnableIndex(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            var enabled = modifier.GetBool(1, true, variables);

            if (PlayerManager.Players.TryGetAt(modifier.GetInt(0, 0, variables), out PAPlayer player) && player.RuntimePlayer)
                player.SetCustomActive(enabled);
        }

        public static void playerEnableAll(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            var enabled = modifier.GetBool(0, true, variables);

            PlayerManager.Players.ForLoop(player =>
            {
                if (player.RuntimePlayer)
                    player.SetCustomActive(enabled);
            });
        }

        public static void playerMove(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not BeatmapObject beatmapObject)
                return;

            // queue post tick so the position of the object is accurate.
            RTLevel.Current.postTick.Enqueue(() =>
            {
                var pos = beatmapObject.GetFullPosition();
                var player = PlayerManager.GetClosestPlayer(pos);

                var value = modifier.GetValue(0);

                Vector2 vector;
                if (value.Contains(','))
                {
                    var axis = value.Split(',');
                    modifier.SetValue(0, axis[0]);
                    modifier.values.RemoveAt(modifier.values.Count - 1);
                    modifier.values.Insert(1, axis[1]);
                    vector = new Vector2(Parser.TryParse(axis[0], 0f), Parser.TryParse(axis[1], 0f));
                }
                else
                    vector = new Vector2(modifier.GetFloat(0, 0f, variables), modifier.GetFloat(1, 0f, variables));

                var duration = modifier.GetFloat(3, 0f, variables);
                bool relative = modifier.GetBool(4, false, variables);
                if (!player || !player.RuntimePlayer)
                    return;

                var tf = player.RuntimePlayer.rb.transform;
                if (duration == 0f || modifier.constant)
                {
                    if (relative)
                        tf.localPosition += (Vector3)vector;
                    else
                        tf.localPosition = vector;
                }
                else
                {
                    string easing = modifier.GetValue(3, variables);
                    if (int.TryParse(easing, out int e) && e >= 0 && e < DataManager.inst.AnimationList.Count)
                        easing = DataManager.inst.AnimationList[e].Name;

                    var animation = new RTAnimation("Player Move");
                    animation.animationHandlers = new List<AnimationHandlerBase>
                    {
                        new AnimationHandler<Vector2>(new List<IKeyframe<Vector2>>
                        {
                            new Vector2Keyframe(0f, tf.localPosition, Ease.Linear),
                            new Vector2Keyframe(modifier.GetFloat(2, 1f, variables), new Vector2(vector.x + (relative ? tf.localPosition.x : 0f), vector.y + (relative ? tf.localPosition.y : 0f)), Ease.GetEaseFunction(easing, Ease.Linear)),
                        }, vector2 => tf.localPosition = vector2, interpolateOnComplete: true),
                    };
                    animation.SetDefaultOnComplete();
                    AnimationManager.inst.Play(animation);
                }
            });
        }

        public static void playerMoveIndex(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            var vector = new Vector2(modifier.GetFloat(1, 0f, variables), modifier.GetFloat(2, 0f, variables));
            var duration = modifier.GetFloat(3, 0f, variables);

            string easing = modifier.GetValue(4, variables);
            if (int.TryParse(easing, out int e) && e >= 0 && e < DataManager.inst.AnimationList.Count)
                easing = DataManager.inst.AnimationList[e].Name;

            var relative = modifier.GetBool(5, false, variables);

            if (!PlayerManager.Players.TryGetAt(modifier.GetInt(0, 0, variables), out PAPlayer player) || !player.RuntimePlayer)
                return;

            var tf = player.RuntimePlayer.rb.transform;
            if (duration == 0f || modifier.constant)
            {
                if (relative)
                    tf.localPosition += (Vector3)vector;
                else
                    tf.localPosition = vector;
            }
            else
            {
                var animation = new RTAnimation("Player Move");
                animation.animationHandlers = new List<AnimationHandlerBase>
                {
                    new AnimationHandler<Vector2>(new List<IKeyframe<Vector2>>
                    {
                        new Vector2Keyframe(0f, tf.localPosition, Ease.Linear),
                        new Vector2Keyframe(duration, new Vector2(vector.x + (relative ? tf.localPosition.x : 0f), vector.y + (relative ? tf.position.y : 0f)), Ease.GetEaseFunction(easing, Ease.Linear)),
                    }, vector2 => tf.localPosition = vector2, interpolateOnComplete: true),
                };
                animation.SetDefaultOnComplete();
                AnimationManager.inst.Play(animation);
            }
        }

        public static void playerMoveAll(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            var value = modifier.GetValue(0, variables);

            Vector2 vector;
            if (value.Contains(','))
            {
                var axis = value.Split(',');
                modifier.SetValue(0, axis[0]);
                modifier.values.RemoveAt(modifier.values.Count - 1);
                modifier.values.Insert(1, axis[1]);
                vector = new Vector2(Parser.TryParse(axis[0], 0f), Parser.TryParse(axis[1], 0f));
            }
            else
                vector = new Vector2(modifier.GetFloat(0, 0f, variables), modifier.GetFloat(1, 0f, variables));

            var duration = modifier.GetFloat(2, 1f, variables);

            string easing = modifier.GetValue(3, variables);
            if (int.TryParse(easing, out int e) && e >= 0 && e < DataManager.inst.AnimationList.Count)
                easing = DataManager.inst.AnimationList[e].Name;

            bool relative = modifier.GetBool(4, false, variables);
            foreach (var player in PlayerManager.Players)
            {
                if (!player.RuntimePlayer)
                    continue;

                var tf = player.RuntimePlayer.rb.transform;
                if (duration == 0f || modifier.constant)
                {
                    if (relative)
                        tf.localPosition += (Vector3)vector;
                    else
                        tf.localPosition = vector;
                }
                else
                {
                    var animation = new RTAnimation("Player Move");
                    animation.animationHandlers = new List<AnimationHandlerBase>
                    {
                        new AnimationHandler<Vector2>(new List<IKeyframe<Vector2>>
                        {
                            new Vector2Keyframe(0f, tf.localPosition, Ease.Linear),
                            new Vector2Keyframe(duration, new Vector2(vector.x + (relative ? tf.localPosition.x : 0f), vector.y + (relative ? tf.position.y : 0f)), Ease.GetEaseFunction(easing, Ease.Linear)),
                        }, vector2 => tf.localPosition = vector2, interpolateOnComplete: true),
                    };
                    animation.SetDefaultOnComplete();
                    AnimationManager.inst.Play(animation);
                }
            }
        }

        public static void playerMoveX(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not BeatmapObject beatmapObject)
                return;

            var value = modifier.GetFloat(0, 0f, variables);
            var duration = modifier.GetFloat(1, 1f, variables);
            string easing = modifier.GetValue(2, variables);
            if (int.TryParse(easing, out int e) && e >= 0 && e < DataManager.inst.AnimationList.Count)
                easing = DataManager.inst.AnimationList[e].Name;

            bool relative = modifier.GetBool(3, false, variables);

            // queue post tick so the position of the object is accurate.
            RTLevel.Current.postTick.Enqueue(() =>
            {
                var pos = beatmapObject.GetFullPosition();
                var player = PlayerManager.GetClosestPlayer(pos);

                if (!player || !player.RuntimePlayer)
                    return;

                var tf = player.RuntimePlayer.rb.transform;
                if (modifier.constant)
                {
                    var v = tf.localPosition;
                    if (relative)
                        v.x += value;
                    else
                        v.x = value;
                    tf.localPosition = v;
                }
                else
                {
                    var animation = new RTAnimation("Player Move");
                    animation.animationHandlers = new List<AnimationHandlerBase>
                    {
                        new AnimationHandler<float>(new List<IKeyframe<float>>
                        {
                            new FloatKeyframe(0f, tf.localPosition.x, Ease.Linear),
                            new FloatKeyframe(duration, value + (relative ? tf.localPosition.x : 0f), Ease.GetEaseFunction(easing, Ease.Linear)),
                        }, tf.SetLocalPositionX, interpolateOnComplete: true),
                    };
                    animation.SetDefaultOnComplete();
                    AnimationManager.inst.Play(animation);
                }
            });
        }

        public static void playerMoveXIndex(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            var value = modifier.GetFloat(1, 0f, variables);
            var duration = modifier.GetFloat(2, 0f, variables);

            string easing = modifier.GetValue(3, variables);
            if (int.TryParse(easing, out int e) && e >= 0 && e < DataManager.inst.AnimationList.Count)
                easing = DataManager.inst.AnimationList[e].Name;

            var relative = modifier.GetBool(4, false, variables);

            if (!PlayerManager.Players.TryGetAt(modifier.GetInt(0, 0, variables), out PAPlayer player) || !player.RuntimePlayer)
                return;

            var tf = player.RuntimePlayer.rb.transform;
            if (modifier.constant)
            {
                var v = tf.localPosition;
                if (relative)
                    v.x += value;
                else
                    v.x = value;
                tf.localPosition = v;
            }
            else
            {
                var animation = new RTAnimation("Player Move");
                animation.animationHandlers = new List<AnimationHandlerBase>
                {
                    new AnimationHandler<float>(new List<IKeyframe<float>>
                    {
                        new FloatKeyframe(0f, tf.localPosition.x, Ease.Linear),
                        new FloatKeyframe(duration, value + (relative ? tf.localPosition.x : 0f), Ease.GetEaseFunction(easing, Ease.Linear)),
                    }, tf.SetLocalPositionX, interpolateOnComplete: true),
                };
                animation.SetDefaultOnComplete();
                AnimationManager.inst.Play(animation);
            }
        }

        public static void playerMoveXAll(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            var value = modifier.GetFloat(0, 0f, variables);
            var duration = modifier.GetFloat(1, 1f, variables);
            string easing = modifier.GetValue(2, variables);
            if (int.TryParse(easing, out int e) && e >= 0 && e < DataManager.inst.AnimationList.Count)
                easing = DataManager.inst.AnimationList[e].Name;

            bool relative = modifier.GetBool(3, false, variables);
            foreach (var player in PlayerManager.Players)
            {
                if (!player.RuntimePlayer)
                    continue;

                var tf = player.RuntimePlayer.rb.transform;
                if (modifier.constant)
                {
                    var v = tf.localPosition;
                    if (relative)
                        v.x += value;
                    else
                        v.x = value;
                    tf.localPosition = v;
                }
                else
                {
                    var animation = new RTAnimation("Player Move");
                    animation.animationHandlers = new List<AnimationHandlerBase>
                    {
                        new AnimationHandler<float>(new List<IKeyframe<float>>
                        {
                            new FloatKeyframe(0f, tf.localPosition.x, Ease.Linear),
                            new FloatKeyframe(duration, value + (relative ? tf.localPosition.x : 0f), Ease.GetEaseFunction(easing, Ease.Linear)),
                        }, tf.SetLocalPositionX, interpolateOnComplete: true),
                    };
                    animation.SetDefaultOnComplete();
                    AnimationManager.inst.Play(animation);
                }
            }
        }

        public static void playerMoveY(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not BeatmapObject beatmapObject)
                return;

            var value = modifier.GetFloat(0, 0f, variables);
            var duration = modifier.GetFloat(1, 1f, variables);
            string easing = modifier.GetValue(2, variables);
            if (int.TryParse(easing, out int e) && e >= 0 && e < DataManager.inst.AnimationList.Count)
                easing = DataManager.inst.AnimationList[e].Name;

            bool relative = modifier.GetBool(3, false, variables);

            // queue post tick so the position of the object is accurate.
            RTLevel.Current.postTick.Enqueue(() =>
            {
                var pos = beatmapObject.GetFullPosition();
                var player = PlayerManager.GetClosestPlayer(pos);

                if (!player || !player.RuntimePlayer)
                    return;

                var tf = player.RuntimePlayer.rb.transform;
                if (modifier.constant)
                {
                    var v = tf.localPosition;
                    if (relative)
                        v.y += value;
                    else
                        v.y = value;
                    tf.localPosition = v;
                }
                else
                {
                    var animation = new RTAnimation("Player Move");
                    animation.animationHandlers = new List<AnimationHandlerBase>
                    {
                        new AnimationHandler<float>(new List<IKeyframe<float>>
                        {
                            new FloatKeyframe(0f, tf.localPosition.y, Ease.Linear),
                            new FloatKeyframe(duration, value + (relative ? tf.localPosition.y : 0f), Ease.GetEaseFunction(easing, Ease.Linear)),
                        }, tf.SetLocalPositionY, interpolateOnComplete: true),
                    };
                    animation.SetDefaultOnComplete();
                    AnimationManager.inst.Play(animation);
                }
            });
        }

        public static void playerMoveYIndex(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            var value = modifier.GetFloat(1, 0f, variables);
            var duration = modifier.GetFloat(2, 0f, variables);

            string easing = modifier.GetValue(3, variables);
            if (int.TryParse(easing, out int e) && e >= 0 && e < DataManager.inst.AnimationList.Count)
                easing = DataManager.inst.AnimationList[e].Name;

            var relative = modifier.GetBool(4, false, variables);

            if (!PlayerManager.Players.TryGetAt(modifier.GetInt(0, 0, variables), out PAPlayer player) || !player.RuntimePlayer)
                return;

            var tf = player.RuntimePlayer.rb.transform;
            if (modifier.constant)
            {
                var v = tf.localPosition;
                if (relative)
                    v.y += value;
                else
                    v.y = value;
                tf.localPosition = v;
            }
            else
            {
                var animation = new RTAnimation("Player Move");
                animation.animationHandlers = new List<AnimationHandlerBase>
                {
                    new AnimationHandler<float>(new List<IKeyframe<float>>
                    {
                        new FloatKeyframe(0f, tf.localPosition.y, Ease.Linear),
                        new FloatKeyframe(duration, value + (relative ? tf.localPosition.y : 0f), Ease.GetEaseFunction(easing, Ease.Linear)),
                    }, tf.SetLocalPositionY, interpolateOnComplete: true),
                };
                animation.SetDefaultOnComplete();
                AnimationManager.inst.Play(animation);
            }
        }

        public static void playerMoveYAll(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            var value = modifier.GetFloat(0, 0f, variables);
            var duration = modifier.GetFloat(1, 1f, variables);
            string easing = modifier.GetValue(2, variables);
            if (int.TryParse(easing, out int e) && e >= 0 && e < DataManager.inst.AnimationList.Count)
                easing = DataManager.inst.AnimationList[e].Name;

            bool relative = modifier.GetBool(3, false, variables);
            foreach (var player in PlayerManager.Players)
            {
                if (!player.RuntimePlayer)
                    continue;

                var tf = player.RuntimePlayer.rb.transform;
                if (modifier.constant)
                {
                    var v = tf.localPosition;
                    if (relative)
                        v.y += value;
                    else
                        v.y = value;
                    tf.localPosition = v;
                }
                else
                {
                    var animation = new RTAnimation("Player Move");
                    animation.animationHandlers = new List<AnimationHandlerBase>
                    {
                        new AnimationHandler<float>(new List<IKeyframe<float>>
                        {
                            new FloatKeyframe(0f, tf.localPosition.y, Ease.Linear),
                            new FloatKeyframe(duration, value + (relative ? tf.localPosition.y : 0f), Ease.GetEaseFunction(easing, Ease.Linear)),
                        }, tf.SetLocalPositionY, interpolateOnComplete: true),
                    };
                    animation.SetDefaultOnComplete();
                    AnimationManager.inst.Play(animation);
                }
            }
        }

        public static void playerRotate(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not BeatmapObject beatmapObject)
                return;

            var value = modifier.GetFloat(0, 0f, variables);
            string easing = modifier.GetValue(2, variables);
            if (int.TryParse(easing, out int e) && e >= 0 && e < DataManager.inst.AnimationList.Count)
                easing = DataManager.inst.AnimationList[e].Name;
            var duration = modifier.GetFloat(1, 1f, variables);
            bool relative = modifier.GetBool(3, false, variables);

            // queue post tick so the position of the object is accurate.
            RTLevel.Current.postTick.Enqueue(() =>
            {
                var pos = beatmapObject.GetFullPosition();
                var player = PlayerManager.GetClosestPlayer(pos);

                if (!player || !player.RuntimePlayer)
                    return;

                var tf = player.RuntimePlayer.rb.transform;
                if (modifier.constant)
                {
                    var v = tf.localRotation.eulerAngles;
                    if (relative)
                        v.z += value;
                    else
                        v.z = value;
                    tf.localRotation = Quaternion.Euler(v);
                }
                else
                {
                    var animation = new RTAnimation("Player Move");
                    animation.animationHandlers = new List<AnimationHandlerBase>
                    {
                        new AnimationHandler<float>(new List<IKeyframe<float>>
                        {
                            new FloatKeyframe(0f, tf.localRotation.eulerAngles.z, Ease.Linear),
                            new FloatKeyframe(duration, value + (relative ? tf.localRotation.eulerAngles.z : 0f), Ease.GetEaseFunction(easing, Ease.Linear)),
                        }, tf.SetLocalRotationEulerZ, interpolateOnComplete: true),
                    };
                    animation.SetDefaultOnComplete();
                    AnimationManager.inst.Play(animation);
                }
            });
        }

        public static void playerRotateIndex(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            var value = modifier.GetFloat(1, 0f, variables);
            var duration = modifier.GetFloat(2, 0f, variables);

            string easing = modifier.GetValue(3, variables);
            if (int.TryParse(easing, out int e) && e >= 0 && e < DataManager.inst.AnimationList.Count)
                easing = DataManager.inst.AnimationList[e].Name;

            var relative = modifier.GetBool(4, false, variables);

            if (!PlayerManager.Players.TryGetAt(modifier.GetInt(0, 0, variables), out PAPlayer player) || !player.RuntimePlayer)
                return;

            var tf = player.RuntimePlayer.rb.transform;
            if (modifier.constant)
            {
                var v = tf.localRotation.eulerAngles;
                if (relative)
                    v.z += value;
                else
                    v.z = value;
                tf.localRotation = Quaternion.Euler(v);
            }
            else
            {
                var animation = new RTAnimation("Player Move");
                animation.animationHandlers = new List<AnimationHandlerBase>
                {
                    new AnimationHandler<float>(new List<IKeyframe<float>>
                    {
                        new FloatKeyframe(0f, tf.localRotation.eulerAngles.z, Ease.Linear),
                        new FloatKeyframe(duration, value + (relative ? tf.localRotation.eulerAngles.z : 0f), Ease.GetEaseFunction(easing, Ease.Linear)),
                    }, tf.SetLocalRotationEulerZ, interpolateOnComplete: true),
                };
                animation.SetDefaultOnComplete();
                AnimationManager.inst.Play(animation);
            }
        }

        public static void playerRotateAll(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            var value = modifier.GetFloat(0, 0f, variables);
            string easing = modifier.GetValue(2, variables);
            if (int.TryParse(easing, out int e) && e >= 0 && e < DataManager.inst.AnimationList.Count)
                easing = DataManager.inst.AnimationList[e].Name;
            var duration = modifier.GetFloat(1, 1f, variables);

            bool relative = modifier.GetBool(3, false, variables);
            foreach (var player in PlayerManager.Players)
            {
                if (!player.RuntimePlayer || !player.RuntimePlayer.rb)
                    continue;

                var tf = player.RuntimePlayer.rb.transform;
                if (modifier.constant)
                {
                    var v = tf.localRotation.eulerAngles;
                    if (relative)
                        v.z += value;
                    else
                        v.z = value;
                    tf.localRotation = Quaternion.Euler(v);
                }
                else
                {
                    var animation = new RTAnimation("Player Move");
                    animation.animationHandlers = new List<AnimationHandlerBase>
                    {
                        new AnimationHandler<float>(new List<IKeyframe<float>>
                        {
                            new FloatKeyframe(0f, tf.localRotation.eulerAngles.z, Ease.Linear),
                            new FloatKeyframe(duration, value + (relative ? tf.localRotation.eulerAngles.z : 0f), Ease.GetEaseFunction(easing, Ease.Linear)),
                        }, tf.SetLocalRotationEulerZ, interpolateOnComplete: true),
                    };
                    animation.SetDefaultOnComplete();
                    AnimationManager.inst.Play(animation);
                }
            }
        }

        public static void playerMoveToObject(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not BeatmapObject beatmapObject)
                return;

            // queue post tick so the position of the object is accurate.
            RTLevel.Current.postTick.Enqueue(() =>
            {
                var pos = beatmapObject.GetFullPosition();
                var player = PlayerManager.GetClosestPlayer(pos);

                if (!player || !player.RuntimePlayer || !player.RuntimePlayer.rb)
                    return;

                player.RuntimePlayer.rb.position = new Vector3(pos.x, pos.y, 0f);
            });
        }

        public static void playerMoveIndexToObject(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not BeatmapObject beatmapObject)
                return;

            var index = modifier.GetInt(0, 0, variables);
            // queue post tick so the position of the object is accurate.
            RTLevel.Current.postTick.Enqueue(() =>
            {
                var pos = beatmapObject.GetFullPosition();
                if (!PlayerManager.Players.TryGetAt(index, out PAPlayer player) || !player.RuntimePlayer || !player.RuntimePlayer.rb)
                    return;

                player.RuntimePlayer.rb.position = new Vector3(pos.x, pos.y, 0f);
            });
        }

        public static void playerMoveAllToObject(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not BeatmapObject beatmapObject)
                return;

            // queue post tick so the position of the object is accurate.
            RTLevel.Current.postTick.Enqueue(() =>
            {
                var pos = beatmapObject.GetFullPosition();
                var player = PlayerManager.GetClosestPlayer(pos);

                if (!player || !player.RuntimePlayer || !player.RuntimePlayer.rb)
                    return;

                var y = player.RuntimePlayer.rb.position.y;
                player.RuntimePlayer.rb.position = new Vector2(pos.x, y);
            });
        }

        public static void playerMoveXToObject(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not BeatmapObject beatmapObject)
                return;

            // queue post tick so the position of the object is accurate.
            RTLevel.Current.postTick.Enqueue(() =>
            {
                var pos = beatmapObject.GetFullPosition();
                var player = PlayerManager.GetClosestPlayer(pos);

                if (!player || !player.RuntimePlayer || !player.RuntimePlayer.rb)
                    return;

                var y = player.RuntimePlayer.rb.position.y;
                player.RuntimePlayer.rb.position = new Vector2(pos.x, y);
            });
        }

        public static void playerMoveXIndexToObject(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not BeatmapObject beatmapObject)
                return;

            var index = modifier.GetInt(0, 0, variables);
            // queue post tick so the position of the object is accurate.
            RTLevel.Current.postTick.Enqueue(() =>
            {
                var pos = beatmapObject.GetFullPosition();
                if (!PlayerManager.Players.TryGetAt(index, out PAPlayer player) || !player.RuntimePlayer || !player.RuntimePlayer.rb)
                    return;

                var y = player.RuntimePlayer.rb.position.y;
                player.RuntimePlayer.rb.position = new Vector2(pos.x, y);
            });
        }

        public static void playerMoveXAllToObject(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not BeatmapObject beatmapObject)
                return;

            // queue post tick so the position of the object is accurate.
            RTLevel.Current.postTick.Enqueue(() =>
            {
                var pos = beatmapObject.GetFullPosition();
                foreach (var player in PlayerManager.Players)
                {
                    if (!player.RuntimePlayer || !player.RuntimePlayer.rb)
                        continue;

                    var y = player.RuntimePlayer.rb.position.y;
                    player.RuntimePlayer.rb.position = new Vector2(pos.x, y);
                }
            });
        }

        public static void playerMoveYToObject(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not BeatmapObject beatmapObject)
                return;

            // queue post tick so the position of the object is accurate.
            RTLevel.Current.postTick.Enqueue(() =>
            {
                var pos = beatmapObject.GetFullPosition();
                var player = PlayerManager.GetClosestPlayer(pos);

                if (!player || !player.RuntimePlayer || !player.RuntimePlayer.rb)
                    return;

                var x = player.RuntimePlayer.rb.position.x;
                player.RuntimePlayer.rb.position = new Vector2(x, pos.y);
            });
        }

        public static void playerMoveYIndexToObject(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not BeatmapObject beatmapObject)
                return;

            var index = modifier.GetInt(0, 0, variables);
            // queue post tick so the position of the object is accurate.
            RTLevel.Current.postTick.Enqueue(() =>
            {
                var pos = beatmapObject.GetFullPosition();
                if (!PlayerManager.Players.TryGetAt(index, out PAPlayer player) || !player.RuntimePlayer || !player.RuntimePlayer.rb)
                    return;

                var x = player.RuntimePlayer.rb.position.x;
                player.RuntimePlayer.rb.position = new Vector2(x, pos.y);
            });
        }

        public static void playerMoveYAllToObject(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not BeatmapObject beatmapObject)
                return;

            // queue post tick so the position of the object is accurate.
            RTLevel.Current.postTick.Enqueue(() =>
            {
                var pos = beatmapObject.GetFullPosition();
                foreach (var player in PlayerManager.Players)
                {
                    if (!player.RuntimePlayer || !player.RuntimePlayer.rb)
                        continue;

                    var x = player.RuntimePlayer.rb.position.x;
                    player.RuntimePlayer.rb.position = new Vector2(x, pos.y);
                }
            });
        }

        public static void playerRotateToObject(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not BeatmapObject beatmapObject)
                return;

            // queue post tick so the position of the object is accurate.
            RTLevel.Current.postTick.Enqueue(() =>
            {
                var pos = beatmapObject.GetFullPosition();
                var player = PlayerManager.GetClosestPlayer(pos);

                if (!player || !player.RuntimePlayer || !player.RuntimePlayer.rb)
                    return;

                player.RuntimePlayer.rb.transform.SetLocalRotationEulerZ(beatmapObject.GetFullRotation(true).z);
            });
        }

        public static void playerRotateIndexToObject(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not BeatmapObject beatmapObject)
                return;

            var index = modifier.GetInt(0, 0, variables);
            // queue post tick so the position of the object is accurate.
            RTLevel.Current.postTick.Enqueue(() =>
            {
                if (!PlayerManager.Players.TryGetAt(index, out PAPlayer player) || !player.RuntimePlayer || !player.RuntimePlayer.rb)
                    return;

                player.RuntimePlayer.rb.transform.SetLocalRotationEulerZ(beatmapObject.GetFullRotation(true).z);
            });
        }

        public static void playerRotateAllToObject(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not BeatmapObject beatmapObject)
                return;

            // queue post tick so the position of the object is accurate.
            RTLevel.Current.postTick.Enqueue(() =>
            {
                var rot = beatmapObject.GetFullRotation(true).z;

                foreach (var player in PlayerManager.Players)
                {
                    if (!player.RuntimePlayer || !player.RuntimePlayer.rb)
                        continue;

                    player.RuntimePlayer.rb.transform.SetLocalRotationEulerZ(rot);
                }
            });
        }

        public static void playerDrag(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not BeatmapObject beatmapObject)
                return;

            var usePosition = modifier.GetBool(0, false, variables);
            var useScale = modifier.GetBool(1, false, variables);
            var useRotation = modifier.GetBool(2, false, variables);

            var prevPos = !usePosition ? Vector3.zero : beatmapObject.GetFullPosition();
            var prevSca = !useScale ? Vector3.zero : beatmapObject.GetFullScale();
            var prevRot = !useRotation ? Vector3.zero : beatmapObject.GetFullRotation(true);

            // queue post tick so the position of the object is accurate.
            RTLevel.Current.postTick.Enqueue(() =>
            {
                var pos = beatmapObject.GetFullPosition();

                var player = PlayerManager.GetClosestPlayer(pos);
                if (!player || !player.RuntimePlayer || !player.RuntimePlayer.rb)
                    return;

                var rb = player.RuntimePlayer.rb;

                Vector2 distance = Vector2.zero;
                if (usePosition)
                    distance = pos - prevPos;
                if (useScale)
                {
                    var playerDistance = Vector3.Distance(pos, rb.position);

                    var sca = beatmapObject.GetFullScale();
                    distance += (Vector2)(sca - prevSca) * playerDistance;
                }
                // idk why this rotates the player around the area next to the object instead of around it
                if (useRotation)
                {
                    var rot = beatmapObject.GetFullRotation(true);
                    //var rotationDistance = RTMath.Distance(rot.z, prevRot.z);

                    var amount = (Vector2)(RTMath.Rotate(distance + rb.position + (Vector2)pos, rot.z) - RTMath.Rotate(distance + rb.position + (Vector2)pos, prevRot.z));
                    //var a = (Vector2)RTMath.Rotate(rb.position + (Vector2)pos, rot.z);
                    //var b = (Vector2)RTMath.Rotate(rb.position + (Vector2)pos, prevRot.z);
                    //var amount = new Vector2(RTMath.Distance(a.x, b.x), RTMath.Distance(a.y, b.y));
                    //if (Input.GetKeyDown(KeyCode.U))
                    //    CoreHelper.Log($"Rot: {rot} Prev Rot: {prevRot} A: {a} B: {b} Amount: {amount}");
                    distance = amount;
                }

                rb.position += distance;
            });
        }

        public static void playerBoost(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (modifier.constant || reference is not BeatmapObject beatmapObject)
                return;

            var xStr = modifier.GetValue(0, variables);
            var yStr = modifier.GetValue(1, variables);
            var shouldBoostX = false;
            var shouldBoostY = false;
            var x = 0f;
            var y = 0f;

            if (!string.IsNullOrEmpty(xStr))
            {
                shouldBoostX = true;
                x = Parser.TryParse(xStr, 0f);
            }

            if (!string.IsNullOrEmpty(yStr))
            {
                shouldBoostY = true;
                y = Parser.TryParse(yStr, 0f);
            }

            // queue post tick so the position of the object is accurate.
            RTLevel.Current.postTick.Enqueue(() =>
            {
                var pos = beatmapObject.GetFullPosition();
                var player = PlayerManager.GetClosestPlayer(pos);

                if (!player || !player.RuntimePlayer)
                    return;

                if (shouldBoostX)
                    player.RuntimePlayer.lastMoveHorizontal = x;
                if (shouldBoostY)
                    player.RuntimePlayer.lastMoveVertical = y;
                player.RuntimePlayer.Boost();
            });
        }

        public static void playerBoostIndex(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            var xStr = modifier.GetValue(0, variables);
            var yStr = modifier.GetValue(1, variables);
            var shouldBoostX = false;
            var shouldBoostY = false;
            var x = 0f;
            var y = 0f;

            if (!string.IsNullOrEmpty(xStr))
            {
                shouldBoostX = true;
                x = Parser.TryParse(xStr, 0f);
            }

            if (!string.IsNullOrEmpty(yStr))
            {
                shouldBoostY = true;
                y = Parser.TryParse(yStr, 0f);
            }

            if (!modifier.constant && PlayerManager.Players.TryGetAt(modifier.GetInt(0, 0, variables), out PAPlayer player) && player.RuntimePlayer)
            {
                if (shouldBoostX)
                    player.RuntimePlayer.lastMoveHorizontal = x;
                if (shouldBoostY)
                    player.RuntimePlayer.lastMoveVertical = y;
                player.RuntimePlayer.Boost();
            }
        }

        public static void playerBoostAll(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            var xStr = modifier.GetValue(0, variables);
            var yStr = modifier.GetValue(1, variables);
            var shouldBoostX = false;
            var shouldBoostY = false;
            var x = 0f;
            var y = 0f;

            if (!string.IsNullOrEmpty(xStr))
            {
                shouldBoostX = true;
                x = Parser.TryParse(xStr, 0f);
            }

            if (!string.IsNullOrEmpty(yStr))
            {
                shouldBoostY = true;
                y = Parser.TryParse(yStr, 0f);
            }

            foreach (var player in PlayerManager.Players)
            {
                if (!player.RuntimePlayer)
                    continue;

                if (shouldBoostX)
                    player.RuntimePlayer.lastMoveHorizontal = x;
                if (shouldBoostY)
                    player.RuntimePlayer.lastMoveVertical = y;
                player.RuntimePlayer.Boost();
            }
        }

        public static void playerCancelBoost(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (modifier.constant || reference is not BeatmapObject beatmapObject)
                return;

            // queue post tick so the position of the object is accurate.
            RTLevel.Current.postTick.Enqueue(() =>
            {
                var pos = beatmapObject.GetFullPosition();
                var player = PlayerManager.GetClosestPlayer(pos);

                if (player && player.RuntimePlayer && player.RuntimePlayer.CanCancelBoosting)
                    player.RuntimePlayer.StopBoosting();
            });
        }

        public static void playerCancelBoostIndex(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (PlayerManager.Players.TryGetAt(modifier.GetInt(0, 0, variables), out PAPlayer player) && player.RuntimePlayer && player.RuntimePlayer.CanCancelBoosting)
                player.RuntimePlayer.StopBoosting();
        }

        public static void playerCancelBoostAll(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            foreach (var player in PlayerManager.Players)
            {
                if (player && player.RuntimePlayer && player.RuntimePlayer.CanCancelBoosting)
                    player.RuntimePlayer.StopBoosting();
            }
        }

        public static void playerDisableBoost(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not BeatmapObject beatmapObject)
                return;

            // queue post tick so the position of the object is accurate.
            RTLevel.Current.postTick.Enqueue(() =>
            {
                var pos = beatmapObject.GetFullPosition();
                var player = PlayerManager.GetClosestPlayer(pos);

                if (player && player.RuntimePlayer)
                    player.RuntimePlayer.CanBoost = false;
            });
        }

        public static void playerDisableBoostIndex(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (PlayerManager.Players.TryGetAt(modifier.GetInt(0, 0, variables), out PAPlayer player) && player.RuntimePlayer)
                player.RuntimePlayer.CanBoost = false;
        }

        public static void playerDisableBoostAll(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            foreach (var player in PlayerManager.Players)
            {
                if (player.RuntimePlayer)
                    player.RuntimePlayer.CanBoost = false;
            }
        }

        public static void playerEnableBoost(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not BeatmapObject beatmapObject)
                return;

            var enabled = modifier.GetBool(0, true, variables);

            // queue post tick so the position of the object is accurate.
            RTLevel.Current.postTick.Enqueue(() =>
            {
                var pos = beatmapObject.GetFullPosition();
                var player = PlayerManager.GetClosestPlayer(pos);

                if (player && player.RuntimePlayer)
                    player.RuntimePlayer.CanBoost = enabled;
            });
        }

        public static void playerEnableBoostIndex(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            var enabled = modifier.GetBool(1, true, variables);

            if (PlayerManager.Players.TryGetAt(modifier.GetInt(0, 0, variables), out PAPlayer player) && player.RuntimePlayer)
                player.RuntimePlayer.CanBoost = enabled;
        }

        public static void playerEnableBoostAll(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            var enabled = modifier.GetBool(0, true, variables);

            foreach (var player in PlayerManager.Players)
            {
                if (player.RuntimePlayer)
                    player.RuntimePlayer.CanBoost = enabled;
            }
        }

        public static void playerEnableMove(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not BeatmapObject beatmapObject)
                return;

            var enabled = modifier.GetBool(0, true, variables);
            var rotate = modifier.GetBool(1, true, variables);

            // queue post tick so the position of the object is accurate.
            RTLevel.Current.postTick.Enqueue(() =>
            {
                var pos = beatmapObject.GetFullPosition();
                var player = PlayerManager.GetClosestPlayer(pos);

                if (!player || !player.RuntimePlayer)
                    return;

                player.RuntimePlayer.CanMove = enabled;
                player.RuntimePlayer.CanRotate = rotate;
            });
        }

        public static void playerEnableMoveIndex(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            var enabled = modifier.GetBool(1, true, variables);
            var rotate = modifier.GetBool(2, true, variables);

            if (!PlayerManager.Players.TryGetAt(modifier.GetInt(0, 0, variables), out PAPlayer player) || !player.RuntimePlayer)
                return;

            player.RuntimePlayer.CanMove = enabled;
            player.RuntimePlayer.CanRotate = rotate;
        }

        public static void playerEnableMoveAll(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            var enabled = modifier.GetBool(0, true, variables);
            var rotate = modifier.GetBool(1, true, variables);

            foreach (var player in PlayerManager.Players)
            {
                if (!player.RuntimePlayer)
                    continue;

                player.RuntimePlayer.CanMove = enabled;
                player.RuntimePlayer.CanRotate = rotate;
            }
        }

        public static void playerSpeed(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables) => RTPlayer.SpeedMultiplier = modifier.GetFloat(0, 1f, variables);

        public static void playerVelocity(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not BeatmapObject beatmapObject)
                return;

            var x = modifier.GetFloat(0, 0f, variables);
            var y = modifier.GetFloat(1, 0f, variables);

            // queue post tick so the position of the object is accurate.
            RTLevel.Current.postTick.Enqueue(() =>
            {
                var pos = beatmapObject.GetFullPosition();
                var player = PlayerManager.GetClosestPlayer(pos);

                if (player && player.RuntimePlayer)
                    player.RuntimePlayer.rb.velocity = new Vector2(x, y);
            });
        }

        public static void playerVelocityIndex(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            var index = modifier.GetInt(0, 0, variables);
            var x = modifier.GetFloat(1, 0f, variables);
            var y = modifier.GetFloat(2, 0f, variables);

            if (PlayerManager.Players.TryGetAt(index, out PAPlayer player) && player.RuntimePlayer && player.RuntimePlayer.rb)
                player.RuntimePlayer.rb.velocity = new Vector2(x, y);
        }

        public static void playerVelocityAll(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            var x = modifier.GetFloat(0, 0f, variables);
            var y = modifier.GetFloat(1, 0f, variables);

            for (int i = 0; i < PlayerManager.Players.Count; i++)
            {
                var player = PlayerManager.Players[i];
                if (player.RuntimePlayer && player.RuntimePlayer.rb)
                    player.RuntimePlayer.rb.velocity = new Vector2(x, y);
            }
        }

        public static void playerVelocityX(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not BeatmapObject beatmapObject)
                return;

            var x = modifier.GetFloat(0, 0f, variables);

            // queue post tick so the position of the object is accurate.
            RTLevel.Current.postTick.Enqueue(() =>
            {
                var pos = beatmapObject.GetFullPosition();
                var player = PlayerManager.GetClosestPlayer(pos);

                if (!player || !player.RuntimePlayer)
                    return;

                var velocity = player.RuntimePlayer.rb.velocity;
                velocity.x = x;
                player.RuntimePlayer.rb.velocity = velocity;
            });
        }

        public static void playerVelocityXIndex(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            var index = modifier.GetInt(0, 0, variables);
            var x = modifier.GetFloat(1, 0f, variables);

            if (!PlayerManager.Players.TryGetAt(index, out PAPlayer player) || !player.RuntimePlayer || !player.RuntimePlayer.rb)
                return;

            var velocity = player.RuntimePlayer.rb.velocity;
            velocity.x = x;
            player.RuntimePlayer.rb.velocity = velocity;
        }

        public static void playerVelocityXAll(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            var x = modifier.GetFloat(0, 0f, variables);

            for (int i = 0; i < PlayerManager.Players.Count; i++)
            {
                var player = PlayerManager.Players[i];
                if (!player.RuntimePlayer || !player.RuntimePlayer.rb)
                    continue;

                var velocity = player.RuntimePlayer.rb.velocity;
                velocity.x = x;
                player.RuntimePlayer.rb.velocity = velocity;
            }
        }

        public static void playerVelocityY(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not BeatmapObject beatmapObject)
                return;

            var y = modifier.GetFloat(0, 0f, variables);

            // queue post tick so the position of the object is accurate.
            RTLevel.Current.postTick.Enqueue(() =>
            {
                var pos = beatmapObject.GetFullPosition();
                var player = PlayerManager.GetClosestPlayer(pos);

                if (!player || !player.RuntimePlayer)
                    return;

                var velocity = player.RuntimePlayer.rb.velocity;
                velocity.y = y;
                player.RuntimePlayer.rb.velocity = velocity;
            });
        }

        public static void playerVelocityYIndex(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            var index = modifier.GetInt(0, 0, variables);
            var y = modifier.GetFloat(1, 0f, variables);

            if (!PlayerManager.Players.TryGetAt(index, out PAPlayer player) || !player.RuntimePlayer || !player.RuntimePlayer.rb)
                return;

            var velocity = player.RuntimePlayer.rb.velocity;
            velocity.y = y;
            player.RuntimePlayer.rb.velocity = velocity;
        }

        public static void playerVelocityYAll(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            var y = modifier.GetFloat(0, 0f, variables);

            for (int i = 0; i < PlayerManager.Players.Count; i++)
            {
                var player = PlayerManager.Players[i];
                if (!player.RuntimePlayer || !player.RuntimePlayer.rb)
                    continue;

                var velocity = player.RuntimePlayer.rb.velocity;
                velocity.y = y;
                player.RuntimePlayer.rb.velocity = velocity;
            }
        }

        public static void playerEnableDamage(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not BeatmapObject beatmapObject)
                return;

            var enabled = modifier.GetBool(0, true, variables);

            // queue post tick so the position of the object is accurate.
            RTLevel.Current.postTick.Enqueue(() =>
            {
                var pos = beatmapObject.GetFullPosition();
                var player = PlayerManager.GetClosestPlayer(pos);

                if (player && player.RuntimePlayer)
                    player.RuntimePlayer.canTakeDamageModified = enabled;
            });
        }

        public static void playerEnableDamageIndex(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            var enabled = modifier.GetBool(1, true, variables);

            if (PlayerManager.Players.TryGetAt(modifier.GetInt(0, 0, variables), out PAPlayer player) && player.RuntimePlayer)
                player.RuntimePlayer.canTakeDamageModified = enabled;
        }

        public static void playerEnableDamageAll(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            var enabled = modifier.GetBool(0, true, variables);

            foreach (var player in PlayerManager.Players)
            {
                if (player.RuntimePlayer)
                    player.RuntimePlayer.canTakeDamageModified = enabled;
            }
        }

        public static void setPlayerModel(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (modifier.constant)
                return;

            var index = modifier.GetInt(1, 0, variables);

            if (!PlayersData.Current.playerModels.ContainsKey(modifier.GetValue(0, variables)))
                return;

            PlayersData.Current.SetPlayerModel(index, modifier.GetValue(0, variables));
            PlayerManager.AssignPlayerModels();

            if (!PlayerManager.Players.TryGetAt(index, out PAPlayer player) || !player.RuntimePlayer)
                return;

            player.UpdatePlayerModel();

            player.RuntimePlayer.playerNeedsUpdating = true;
            player.RuntimePlayer.UpdateModel();
        }

        public static void setGameMode(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables) => RTPlayer.GameMode = (GameMode)modifier.GetInt(0, 0);

        public static void gameMode(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables) => RTPlayer.GameMode = (GameMode)modifier.GetInt(0, 0);

        public static void blackHole(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not BeatmapObject beatmapObject)
                return;

            float p = Time.deltaTime * 60f * CoreHelper.ForwardPitch;

            float num = modifier.GetFloat(0, 0.01f, variables);

            if (modifier.GetBool(1, false, variables))
                num = -(beatmapObject.Interpolate(3, 1) - 1f) * num;

            if (num == 0f)
                return;

            float moveDelay = 1f - Mathf.Pow(1f - Mathf.Clamp(num, 0.001f, 1f), p);
            var players = PlayerManager.Players;

            // queue post tick so the position of the object is accurate.
            RTLevel.Current.postTick.Enqueue(() =>
            {
                var pos = beatmapObject.GetFullPosition();
                var player = PlayerManager.GetClosestPlayer(pos);

                if (!player || !player.RuntimePlayer)
                    return;

                var transform = player.RuntimePlayer.rb.transform;

                var vector = new Vector3(transform.position.x, transform.position.y, 0f);
                var target = new Vector3(pos.x, pos.y, 0f);

                transform.position += (target - vector) * moveDelay;
            });
        }

        public static void blackHoleIndex(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not BeatmapObject beatmapObject)
                return;

            float p = Time.deltaTime * 60f * CoreHelper.ForwardPitch;

            float num = modifier.GetFloat(1, 0.01f, variables);

            if (num == 0f)
                return;

            float moveDelay = 1f - Mathf.Pow(1f - Mathf.Clamp(num, 0.001f, 1f), p);
            var index = modifier.GetInt(0, 0, variables);
            if (!PlayerManager.Players.TryGetAt(index, out PAPlayer player) || !player.RuntimePlayer || !player.RuntimePlayer.rb)
                return;

            var pos = beatmapObject.GetFullPosition();

            if (!player || !player.RuntimePlayer)
                return;

            var transform = player.RuntimePlayer.rb.transform;

            var vector = new Vector3(transform.position.x, transform.position.y, 0f);
            var target = new Vector3(pos.x, pos.y, 0f);

            transform.position += (target - vector) * moveDelay;
        }

        public static void blackHoleAll(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not BeatmapObject beatmapObject)
                return;

            float p = Time.deltaTime * 60f * CoreHelper.ForwardPitch;

            float num = modifier.GetFloat(0, 0.01f, variables);

            if (modifier.GetBool(1, false, variables))
                num = -(beatmapObject.Interpolate(3, 1) - 1f) * num;

            if (num == 0f)
                return;

            float moveDelay = 1f - Mathf.Pow(1f - Mathf.Clamp(num, 0.001f, 1f), p);
            var players = PlayerManager.Players;

            // queue post tick so the position of the object is accurate.
            RTLevel.Current.postTick.Enqueue(() =>
            {
                var pos = beatmapObject.GetFullPosition();
                players.ForLoop(player =>
                {
                    if (!player.RuntimePlayer || !player.RuntimePlayer.rb)
                        return;

                    var transform = player.RuntimePlayer.rb.transform;

                    var vector = new Vector3(transform.position.x, transform.position.y, 0f);
                    var target = new Vector3(pos.x, pos.y, 0f);

                    transform.position += (target - vector) * moveDelay;
                });
            });
        }

        public static void whiteHole(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not BeatmapObject beatmapObject)
                return;

            float p = Time.deltaTime * 60f * CoreHelper.ForwardPitch;

            float num = modifier.GetFloat(0, 0.01f, variables);

            if (num == 0f)
                return;

            float moveDelay = 1f - Mathf.Pow(1f - Mathf.Clamp(num, 0.001f, 1f), p);
            var players = PlayerManager.Players;

            // queue post tick so the position of the object is accurate.
            RTLevel.Current.postTick.Enqueue(() =>
            {
                var pos = beatmapObject.GetFullPosition();
                var player = PlayerManager.GetClosestPlayer(pos);

                if (!player || !player.RuntimePlayer)
                    return;

                var transform = player.RuntimePlayer.rb.transform;

                var vector = new Vector3(transform.position.x, transform.position.y, 0f);
                var target = new Vector3(-pos.x, -pos.y, 0f);

                transform.position += (target + vector) * moveDelay;
            });
        }

        public static void whiteHoleIndex(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not BeatmapObject beatmapObject)
                return;

            float p = Time.deltaTime * 60f * CoreHelper.ForwardPitch;

            float num = modifier.GetFloat(1, 0.01f, variables);

            if (num == 0f)
                return;

            float moveDelay = 1f - Mathf.Pow(1f - Mathf.Clamp(num, 0.001f, 1f), p);
            var index = modifier.GetInt(0, 0, variables);
            if (!PlayerManager.Players.TryGetAt(index, out PAPlayer player) || !player.RuntimePlayer || !player.RuntimePlayer.rb)
                return;

            // queue post tick so the position of the object is accurate.
            RTLevel.Current.postTick.Enqueue(() =>
            {
                var pos = beatmapObject.GetFullPosition();

                if (!player || !player.RuntimePlayer)
                    return;

                var transform = player.RuntimePlayer.rb.transform;

                var vector = new Vector3(transform.position.x, transform.position.y, 0f);
                var target = new Vector3(-pos.x, -pos.y, 0f);

                transform.position += (target + vector) * moveDelay;
            });
        }

        public static void whiteHoleAll(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not BeatmapObject beatmapObject)
                return;

            float p = Time.deltaTime * 60f * CoreHelper.ForwardPitch;

            float num = modifier.GetFloat(0, 0.01f, variables);

            if (modifier.GetBool(1, false, variables))
                num = -(beatmapObject.Interpolate(3, 1) - 1f) * num;

            if (num == 0f)
                return;

            float moveDelay = 1f - Mathf.Pow(1f - Mathf.Clamp(num, 0.001f, 1f), p);
            var players = PlayerManager.Players;

            // queue post tick so the position of the object is accurate.
            RTLevel.Current.postTick.Enqueue(() =>
            {
                var pos = beatmapObject.GetFullPosition();
                players.ForLoop(player =>
                {
                    if (!player.RuntimePlayer || !player.RuntimePlayer.rb)
                        return;

                    var transform = player.RuntimePlayer.rb.transform;

                    var vector = new Vector3(transform.position.x, transform.position.y, 0f);
                    var target = new Vector3(-pos.x, -pos.y, 0f);

                    transform.position += (target + vector) * moveDelay;
                });
            });
        }

        #endregion

        #region Mouse Cursor

        public static void showMouse(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            var value = modifier.GetValue(0, variables);
            if (value == "0")
                value = "True";
            var enabled = Parser.TryParse(value, true);

            if (enabled)
                CursorManager.inst.ShowCursor();
            else if (CoreHelper.InEditorPreview)
                CursorManager.inst.HideCursor();
        }

        public static void hideMouse(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (CoreHelper.InEditorPreview)
                CursorManager.inst.HideCursor();
        }

        public static void setMousePosition(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (CoreHelper.IsEditing)
                return;

            var screenScale = Display.main.systemWidth / 1920f;
            float windowCenterX = (Display.main.systemWidth) / 2;
            float windowCenterY = (Display.main.systemHeight) / 2;

            var x = modifier.GetFloat(1, 0f, variables);
            var y = modifier.GetFloat(2, 0f, variables);

            CursorManager.inst.SetCursorPosition(new Vector2((x * screenScale) + windowCenterX, (y * screenScale) + windowCenterY));
        }

        public static void followMousePosition(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (modifier.GetValue(0) == "0")
                modifier.SetValue(0, "1");

            if (reference is not ITransformable transformable)
                return;

            Vector2 mousePosition = Input.mousePosition;
            mousePosition = Camera.main.ScreenToWorldPoint(mousePosition);

            float p = Time.deltaTime * 60f;
            float po = 1f - Mathf.Pow(1f - Mathf.Clamp(modifier.GetFloat(0, 1f, variables), 0.001f, 1f), p);
            float ro = 1f - Mathf.Pow(1f - Mathf.Clamp(modifier.GetFloat(1, 1f, variables), 0.001f, 1f), p);

            if (modifier.Result == null)
                modifier.Result = Vector2.zero;

            var dragPos = (Vector2)modifier.Result;

            var target = new Vector2(mousePosition.x, mousePosition.y);

            transformable.RotationOffset = new Vector3(0f, 0f, (target.x - dragPos.x) * ro);

            dragPos += (target - dragPos) * po;

            modifier.Result = dragPos;

            transformable.PositionOffset = dragPos;
        }

        #endregion

        #region Enable

        public static void enableObject(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            var value = modifier.GetValue(0, variables);
            if (value == "0")
                value = "True";

            if (reference is ICustomActivatable activatable)
            {
                activatable.SetCustomActive(Parser.TryParse(value, true));
                return;
            }

            if (reference is not IPrefabable prefabable)
                return;

            ModifiersHelper.SetObjectActive(prefabable, Parser.TryParse(value, true));
        }

        public static void enableObjectTree(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            var value = modifier.GetValue(0, variables);
            if (value == "0")
                value = "False";

            var enabled = modifier.GetBool(2, true, variables);

            var list = modifier.GetResultOrDefault(() =>
            {
                if (reference is not BeatmapObject beatmapObject)
                    return new List<BeatmapObject>();

                var root = Parser.TryParse(value, true) ? beatmapObject : beatmapObject.GetParentChain().Last();
                return root.GetChildTree();
            });

            for (int i = 0; i < list.Count; i++)
                list[i].runtimeObject?.SetCustomActive(enabled);
        }

        public static void enableObjectOther(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            var enabled = modifier.GetBool(2, true, variables);

            var prefabables = modifier.GetResultOrDefault(() =>
            {
                var prefabable = reference.AsPrefabable();
                if (prefabable == null)
                    return new List<IPrefabable>();

                return GameData.Current.FindPrefabablesWithTag(modifier, prefabable, modifier.GetValue(0, variables));
            });

            if (prefabables.IsEmpty())
                return;

            foreach (var other in prefabables)
                ModifiersHelper.SetObjectActive(other, enabled);
        }

        public static void enableObjectTreeOther(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            var enabled = modifier.GetBool(3, true, variables);

            var list = modifier.GetResultOrDefault(() =>
            {
                var resultList = new List<BeatmapObject>();

                var prefabable = reference.AsPrefabable();
                if (prefabable == null)
                    return resultList;

                var beatmapObjects = GameData.Current.FindObjectsWithTag(modifier, prefabable, modifier.GetValue(1, variables));
                var useSelf = modifier.GetBool(0, true, variables);

                foreach (var bm in beatmapObjects)
                {
                    var beatmapObject = useSelf ? bm : bm.GetParentChain().Last();
                    resultList.AddRange(beatmapObject.GetChildTree());
                }
                return resultList;
            });

            for (int i = 0; i < list.Count; i++)
                list[i].runtimeObject?.SetCustomActive(enabled);
        }

        // if this ever needs to be updated, add a "version" int number to modifiers that increment each time a major change was done to the modifier.
        public static void enableObjectGroup(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            var enabled = modifier.GetBool(0, true, variables);
            var state = modifier.GetInt(1, 0, variables);

            var enableObjectGroupCache = modifier.GetResultOrDefault(() =>
            {
                var cache = new EnableObjectGroupCache();
                var prefabable = reference.AsPrefabable();
                if (prefabable == null)
                    return cache;

                var groups = new List<List<IPrefabable>>();
                int count = 0;
                for (int i = 2; i < modifier.values.Count; i++)
                {
                    var tag = modifier.values[i];
                    if (string.IsNullOrEmpty(tag))
                        continue;

                    var list = GameData.Current.FindPrefabablesWithTag(modifier, prefabable, tag);
                    groups.Add(list);
                    cache.allObjects.AddRange(list);

                    count++;
                }
                cache.Init(groups.ToArray(), enabled);
                return cache;
            });
            enableObjectGroupCache?.SetGroupActive(enabled, state);
        }

        public static void disableObject(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is BeatmapObject beatmapObject)
                beatmapObject.runtimeObject?.SetCustomActive(false);
        }

        public static void disableObjectTree(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not BeatmapObject beatmapObject)
                return;

            var value = modifier.GetValue(0, variables);
            if (value == "0")
                value = "False";

            var list = modifier.GetResultOrDefault(() =>
            {
                var root = Parser.TryParse(value, true) ? beatmapObject : beatmapObject.GetParentChain().Last();
                return root.GetChildTree();
            });

            for (int i = 0; i < list.Count; i++)
                list[i].runtimeObject?.SetCustomActive(false);
        }

        public static void disableObjectOther(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            var list = modifier.GetResultOrDefault(() =>
            {
                var prefabable = reference.AsPrefabable();
                if (prefabable == null)
                    return new List<BeatmapObject>();

                return GameData.Current.FindObjectsWithTag(modifier, prefabable, modifier.GetValue(0, variables));
            });

            if (!list.IsEmpty())
                foreach (var beatmapObject in list)
                    beatmapObject.runtimeObject?.SetCustomActive(false);
        }

        public static void disableObjectTreeOther(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            var list = modifier.GetResultOrDefault(() =>
            {
                var resultList = new List<BeatmapObject>();

                var prefabable = reference.AsPrefabable();
                if (prefabable == null)
                    return resultList;

                var beatmapObjects = GameData.Current.FindObjectsWithTag(modifier, prefabable, modifier.GetValue(1, variables));
                var useSelf = modifier.GetBool(0, true, variables);

                foreach (var bm in beatmapObjects)
                {
                    var beatmapObject = useSelf ? bm : bm.GetParentChain().Last();
                    resultList.AddRange(beatmapObject.GetChildTree());
                }
                return resultList;
            });

            for (int i = 0; i < list.Count; i++)
                list[i].runtimeObject?.SetCustomActive(false);
        }

        public static void setActive(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is BackgroundObject backgroundObject)
                backgroundObject.Enabled = modifier.GetBool(0, false, variables);
        }

        public static void setActiveOther(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            var active = modifier.GetBool(0, false, variables);
            var tag = modifier.GetValue(1, variables);
            var list = GameData.Current.backgroundObjects.FindAll(x => x.tags.Contains(tag));
            if (!list.IsEmpty())
                for (int i = 0; i < list.Count; i++)
                    list[i].Enabled = active;
        }

        #endregion

        #region JSON

        public static void saveFloat(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            ModifiersHelper.SaveProgress(modifier.GetValue(1, variables), modifier.GetValue(2, variables), modifier.GetValue(3, variables), modifier.GetFloat(0, 0f, variables));
        }

        public static void saveString(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            ModifiersHelper.SaveProgress(modifier.GetValue(1, variables), modifier.GetValue(2, variables), modifier.GetValue(3, variables), modifier.GetValue(0, variables));
        }

        public static void saveText(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is BeatmapObject beatmapObject && beatmapObject.runtimeObject && beatmapObject.runtimeObject.visualObject is TextObject textObject)
                ModifiersHelper.SaveProgress(modifier.GetValue(1, variables), modifier.GetValue(2, variables), modifier.GetValue(3, variables), textObject.textMeshPro.text);
        }

        public static void saveVariable(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            ModifiersHelper.SaveProgress(modifier.GetValue(1, variables), modifier.GetValue(2, variables), modifier.GetValue(3, variables), reference.IntVariable);
        }

        public static void loadVariable(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            var path = RTFile.CombinePaths(RTFile.ApplicationDirectory, "profile", modifier.GetValue(1, variables) + FileFormat.SES.Dot());
            if (!RTFile.FileExists(path))
                return;

            string json = RTFile.ReadFromFile(path);

            if (string.IsNullOrEmpty(json))
                return;

            var jn = JSON.Parse(json);

            var fjn = jn[modifier.GetValue(2, variables)][modifier.GetValue(3, variables)]["float"];
            if (!string.IsNullOrEmpty(fjn) && float.TryParse(fjn, out float eq))
                reference.IntVariable = (int)eq;
        }

        public static void loadVariableOther(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not IPrefabable prefabable)
                return;

            var path = RTFile.CombinePaths(RTFile.ApplicationDirectory, "profile", modifier.GetValue(1, variables) + FileFormat.SES.Dot());
            if (!RTFile.FileExists(path))
                return;

            string json = RTFile.ReadFromFile(path);

            if (string.IsNullOrEmpty(json))
                return;

            var jn = JSON.Parse(json);
            var list = GameData.Current.FindObjectsWithTag(modifier, prefabable, modifier.GetValue(0, variables));
            var fjn = jn[modifier.GetValue(2, variables)][modifier.GetValue(3, variables)]["float"];

            if (list.Count > 0 && !string.IsNullOrEmpty(fjn) && float.TryParse(fjn, out float eq))
                foreach (var bm in list)
                    bm.integerVariable = (int)eq;
        }

        #endregion

        #region Reactive

        public static void reactivePos(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not BeatmapObject beatmapObject)
                return;

            var val = modifier.GetFloat(0, 0f, variables);
            var sampleX = modifier.GetInt(1, 0, variables);
            var sampleY = modifier.GetInt(2, 0, variables);
            var intensityX = modifier.GetFloat(3, 0f, variables);
            var intensityY = modifier.GetFloat(4, 0f, variables);

            beatmapObject.runtimeObject?.visualObject?.SetOrigin(new Vector3(
                beatmapObject.origin.x + RTLevel.Current.GetSample(sampleX, intensityX * val),
                beatmapObject.origin.y + RTLevel.Current.GetSample(sampleY, intensityY * val),
                beatmapObject.Depth * 0.1f));
        }

        public static void reactiveSca(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not BeatmapObject beatmapObject)
                return;

            var val = modifier.GetFloat(0, 0f, variables);
            var sampleX = modifier.GetInt(1, 0, variables);
            var sampleY = modifier.GetInt(2, 0, variables);
            var intensityX = modifier.GetFloat(3, 0f, variables);
            var intensityY = modifier.GetFloat(4, 0f, variables);

            beatmapObject.runtimeObject?.visualObject?.SetScaleOffset(new Vector2(
                1f + RTLevel.Current.GetSample(sampleX, intensityX * val),
                1f + RTLevel.Current.GetSample(sampleY, intensityY * val)));
        }

        public static void reactiveRot(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is BeatmapObject beatmapObject)
                beatmapObject.runtimeObject?.visualObject?.SetRotationOffset(RTLevel.Current.GetSample(modifier.GetInt(1, 0, variables), modifier.GetFloat(0, 0f, variables)));
        }

        public static void reactiveCol(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not BeatmapObject beatmapObject)
                return;

            var runtimeObject = beatmapObject.runtimeObject;
            if (runtimeObject && runtimeObject.visualObject && runtimeObject.visualObject.renderer)
                runtimeObject.visualObject.SetColor(runtimeObject.visualObject.GetPrimaryColor() + ThemeManager.inst.Current.GetObjColor(modifier.GetInt(2, 0, variables)) * RTLevel.Current.GetSample(modifier.GetInt(1, 0, variables), modifier.GetFloat(0, 0f, variables)));
        }

        public static void reactiveColLerp(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not BeatmapObject beatmapObject)
                return;

            var runtimeObject = beatmapObject.runtimeObject;
            if (runtimeObject && runtimeObject.visualObject && runtimeObject.visualObject.renderer)
                runtimeObject.visualObject.SetColor(RTMath.Lerp(runtimeObject.visualObject.GetPrimaryColor(), ThemeManager.inst.Current.GetObjColor(modifier.GetInt(2, 0, variables)), RTLevel.Current.GetSample(modifier.GetInt(1, 0, variables), modifier.GetFloat(0, 0f, variables))));
        }

        public static void reactivePosChain(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not IReactive reactive)
                return;

            var val = modifier.GetFloat(0, 0f, variables);
            var sampleX = modifier.GetInt(1, 0, variables);
            var sampleY = modifier.GetInt(2, 0, variables);
            var intensityX = modifier.GetFloat(3, 0f, variables);
            var intensityY = modifier.GetFloat(4, 0f, variables);

            float reactivePositionX = RTLevel.Current.GetSample(sampleX, intensityX * val);
            float reactivePositionY = RTLevel.Current.GetSample(sampleY, intensityY * val);

            reactive.ReactivePositionOffset = new Vector3(reactivePositionX, reactivePositionY);
        }

        public static void reactiveScaChain(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not IReactive reactive)
                return;

            var val = modifier.GetFloat(0, 0f, variables);
            var sampleX = modifier.GetInt(1, 0, variables);
            var sampleY = modifier.GetInt(2, 0, variables);
            var intensityX = modifier.GetFloat(3, 0f, variables);
            var intensityY = modifier.GetFloat(4, 0f, variables);

            float reactiveScaleX = RTLevel.Current.GetSample(sampleX, intensityX * val);
            float reactiveScaleY = RTLevel.Current.GetSample(sampleY, intensityY * val);

            reactive.ReactiveScaleOffset = new Vector3(reactiveScaleX, reactiveScaleY, 1f);
        }

        public static void reactiveRotChain(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is IReactive reactive)
                reactive.ReactiveRotationOffset = RTLevel.Current.GetSample(modifier.GetInt(1, 0, variables), modifier.GetFloat(0, 0f, variables));
        }

        #endregion

        #region Events

        public static void eventOffset(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (RTLevel.Current.eventEngine && RTLevel.Current.eventEngine.offsets != null)
                RTLevel.Current.eventEngine.SetOffset(modifier.GetInt(1, 0, variables), modifier.GetInt(2, 0, variables), modifier.GetFloat(0, 1f, variables));
        }

        public static void eventOffsetVariable(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (RTLevel.Current.eventEngine && RTLevel.Current.eventEngine.offsets != null && reference is IModifyable modifyable)
                RTLevel.Current.eventEngine.SetOffset(modifier.GetInt(1, 0, variables), modifier.GetInt(2, 0, variables), modifyable.IntVariable * modifier.GetFloat(0, 1f, variables));
        }

        public static void eventOffsetMath(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (RTLevel.Current.eventEngine && RTLevel.Current.eventEngine.offsets != null && reference is IEvaluatable evaluatable)
            {
                var numberVariables = evaluatable.GetObjectVariables();
                ModifiersHelper.SetVariables(variables, numberVariables);
                RTLevel.Current.eventEngine.SetOffset(modifier.GetInt(1, 0, variables), modifier.GetInt(2, 0, variables), RTMath.Parse(modifier.GetValue(0, variables), numberVariables, evaluatable.GetObjectFunctions()));
            }
        }

        public static void eventOffsetAnimate(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (modifier.constant || !RTLevel.Current.eventEngine || RTLevel.Current.eventEngine.offsets == null)
                return;

            string easing = modifier.GetValue(4, variables);
            if (int.TryParse(easing, out int e) && e >= 0 && e < DataManager.inst.AnimationList.Count)
                easing = DataManager.inst.AnimationList[e].Name;

            var list = RTLevel.Current.eventEngine.offsets;

            var eventType = modifier.GetInt(1, 0, variables);
            var indexValue = modifier.GetInt(2, 0, variables);

            if (eventType < list.Count && indexValue < list[eventType].Count)
            {
                var value = modifier.GetBool(5, false, variables) ? list[eventType][indexValue] + modifier.GetFloat(0, 0f, variables) : modifier.GetFloat(0, 0f, variables);

                var animation = new RTAnimation("Event Offset Animation");
                animation.animationHandlers = new List<AnimationHandlerBase>
                    {
                        new AnimationHandler<float>(new List<IKeyframe<float>>
                        {
                            new FloatKeyframe(0f, list[eventType][indexValue], Ease.Linear),
                            new FloatKeyframe(modifier.GetFloat(3, 1f, variables), value, Ease.GetEaseFunction(easing, Ease.Linear)),
                        }, x => RTLevel.Current.eventEngine.SetOffset(eventType, indexValue, x), interpolateOnComplete: true)
                    };
                animation.SetDefaultOnComplete();
                AnimationManager.inst.Play(animation);
            }
        }

        public static void eventOffsetCopyAxis(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (!RTLevel.Current.eventEngine || RTLevel.Current.eventEngine.offsets == null || reference is not BeatmapObject beatmapObject)
                return;

            var fromType = modifier.GetInt(1, 0, variables);
            var fromAxis = modifier.GetInt(2, 0, variables);
            var toType = modifier.GetInt(3, 0, variables);
            var toAxis = modifier.GetInt(4, 0, variables);
            var delay = modifier.GetFloat(5, 0f, variables);
            var multiply = modifier.GetFloat(6, 0f, variables);
            var offset = modifier.GetFloat(7, 0f, variables);
            var min = modifier.GetFloat(8, 0f, variables);
            var max = modifier.GetFloat(9, 0f, variables);
            var loop = modifier.GetFloat(10, 0f, variables);
            var useVisual = modifier.GetBool(11, false, variables);

            var time = AudioManager.inst.CurrentAudioSource.time;

            fromType = Mathf.Clamp(fromType, 0, beatmapObject.events.Count - 1);
            fromAxis = Mathf.Clamp(fromAxis, 0, beatmapObject.events[fromType][0].values.Length - 1);
            toType = Mathf.Clamp(toType, 0, RTLevel.Current.eventEngine.offsets.Count - 1);
            toAxis = Mathf.Clamp(toAxis, 0, RTLevel.Current.eventEngine.offsets[toType].Count - 1);

            if (!useVisual && beatmapObject.cachedSequences)
                RTLevel.Current.eventEngine.SetOffset(toType, toAxis, fromType switch
                {
                    0 => Mathf.Clamp((beatmapObject.cachedSequences.PositionSequence.GetValue(time - beatmapObject.StartTime - delay).At(fromAxis) - offset) * multiply % loop, min, max),
                    1 => Mathf.Clamp((beatmapObject.cachedSequences.ScaleSequence.GetValue(time - beatmapObject.StartTime - delay).At(fromAxis) - offset) * multiply % loop, min, max),
                    2 => Mathf.Clamp((beatmapObject.cachedSequences.RotationSequence.GetValue(time - beatmapObject.StartTime - delay) - offset) * multiply % loop, min, max),
                    _ => 0f,
                });
            else if (beatmapObject.runtimeObject is RTBeatmapObject runtimeObject && runtimeObject.visualObject && runtimeObject.visualObject.gameObject)
                RTLevel.Current.eventEngine.SetOffset(toType, toAxis, Mathf.Clamp((runtimeObject.visualObject.gameObject.transform.GetVector(fromType).At(fromAxis) - offset) * multiply % loop, min, max));
        }

        public static void vignetteTracksPlayer(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (!RTLevel.Current.eventEngine)
                return;

            var players = PlayerManager.Players;
            if (players.IsEmpty())
                return;

            var player = players[0].RuntimePlayer;

            if (!player || !player.rb)
                return;

            var cameraToViewportPoint = Camera.main.WorldToViewportPoint(player.rb.position);
            RTLevel.Current.eventEngine.SetOffset(7, 4, cameraToViewportPoint.x);
            RTLevel.Current.eventEngine.SetOffset(7, 5, cameraToViewportPoint.y);
        }

        public static void lensTracksPlayer(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (!RTLevel.Current.eventEngine)
                return;

            var players = PlayerManager.Players;
            if (players.IsEmpty())
                return;

            var player = players[0].RuntimePlayer;

            if (!player || !player.rb)
                return;

            var cameraToViewportPoint = Camera.main.WorldToViewportPoint(player.rb.position);
            RTLevel.Current.eventEngine.SetOffset(8, 1, cameraToViewportPoint.x - 0.5f);
            RTLevel.Current.eventEngine.SetOffset(8, 2, cameraToViewportPoint.y - 0.5f);
        }

        #endregion

        #region Color

        public static void setTheme(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            var themeID = modifier.GetValue(0, variables);
            if (string.IsNullOrEmpty(themeID))
            {
                if (RTLevel.Current && RTLevel.Current.eventEngine)
                    RTLevel.Current.eventEngine.CustomTheme = null;
                return;
            }

            var theme = ThemeManager.inst.GetTheme(themeID);
            if (theme && RTLevel.Current && RTLevel.Current.eventEngine)
                RTLevel.Current.eventEngine.CustomTheme = theme;
        }

        public static void lerpTheme(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            var firstID = modifier.GetValue(0, variables);
            var secondID = modifier.GetValue(1, variables);
            if (string.IsNullOrEmpty(firstID) || string.IsNullOrEmpty(secondID))
            {
                if (RTLevel.Current && RTLevel.Current.eventEngine)
                    RTLevel.Current.eventEngine.CustomTheme = null;
                return;
            }

            if (!RTLevel.Current || !RTLevel.Current.eventEngine)
                return;

            if (!RTLevel.Current.eventEngine.CustomTheme)
                RTLevel.Current.eventEngine.CustomTheme = ThemeManager.inst.Current.Copy();

            var first = ThemeManager.inst.GetTheme(firstID);
            var second = ThemeManager.inst.GetTheme(secondID);
            RTLevel.Current.eventEngine.CustomTheme.Lerp(first, second, modifier.GetFloat(2, 0f, variables));
        }

        public static void addColor(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not BeatmapObject beatmapObject || !beatmapObject.runtimeObject || !beatmapObject.runtimeObject.visualObject)
                return;

            var multiply = modifier.GetFloat(0, 1f, variables);
            var index = modifier.GetInt(1, 0, variables);
            var hue = modifier.GetFloat(2, 0f, variables);
            var sat = modifier.GetFloat(3, 0f, variables);
            var val = modifier.GetFloat(4, 0f, variables);

            // queue post tick so the color overrides the sequence color
            RTLevel.Current.postTick.Enqueue(() =>
            {
                beatmapObject.runtimeObject.visualObject.SetColor(beatmapObject.runtimeObject.visualObject.GetPrimaryColor() + RTColors.ChangeColorHSV(CoreHelper.CurrentBeatmapTheme.GetObjColor(index), hue, sat, val) * multiply);
            });
        }

        public static void addColorOther(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            var list = modifier.GetResultOrDefault(() =>
            {
                var prefabable = reference.AsPrefabable();
                if (prefabable == null)
                    return new List<BeatmapObject>();

                return GameData.Current.FindObjectsWithTag(modifier, prefabable, modifier.GetValue(1));
            });

            if (list.IsEmpty())
                return;

            var multiply = modifier.GetFloat(0, 0f, variables);
            var index = modifier.GetInt(2, 0, variables);
            var hue = modifier.GetFloat(3, 0f, variables);
            var sat = modifier.GetFloat(4, 0f, variables);
            var val = modifier.GetFloat(5, 0f, variables);

            // queue post tick so the color overrides the sequence color
            RTLevel.Current.postTick.Enqueue(() =>
            {
                foreach (var bm in list)
                {
                    if (bm.runtimeObject)
                        bm.runtimeObject.visualObject.SetColor(bm.runtimeObject.visualObject.GetPrimaryColor() + RTColors.ChangeColorHSV(ThemeManager.inst.Current.GetObjColor(index), hue, sat, val) * multiply);
                }
            });
        }

        public static void lerpColor(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not BeatmapObject beatmapObject || !beatmapObject.runtimeObject || !beatmapObject.runtimeObject.visualObject)
                return;

            var multiply = modifier.GetFloat(0, 0f, variables);
            var index = modifier.GetInt(1, 0, variables);
            var hue = modifier.GetFloat(2, 0f, variables);
            var sat = modifier.GetFloat(3, 0f, variables);
            var val = modifier.GetFloat(4, 0f, variables);

            // queue post tick so the color overrides the sequence color
            RTLevel.Current.postTick.Enqueue(() =>
            {
                beatmapObject.runtimeObject.visualObject.SetColor(RTMath.Lerp(beatmapObject.runtimeObject.visualObject.GetPrimaryColor(), RTColors.ChangeColorHSV(ThemeManager.inst.Current.GetObjColor(index), hue, sat, val), multiply));
            });
        }

        public static void lerpColorOther(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            var list = modifier.GetResultOrDefault(() =>
            {
                var prefabable = reference.AsPrefabable();
                if (prefabable == null)
                    return new List<BeatmapObject>();

                return GameData.Current.FindObjectsWithTag(modifier, prefabable, modifier.GetValue(1));
            });

            if (list.IsEmpty())
                return;

            var multiply = modifier.GetFloat(0, 0f, variables);
            var index = modifier.GetInt(2, 0, variables);
            var hue = modifier.GetFloat(3, 0f, variables);
            var sat = modifier.GetFloat(4, 0f, variables);
            var val = modifier.GetFloat(5, 0f, variables);

            // queue post tick so the color overrides the sequence color
            RTLevel.Current.postTick.Enqueue(() =>
            {
                var color = RTColors.ChangeColorHSV(ThemeManager.inst.Current.GetObjColor(index), hue, sat, val);
                for (int i = 0; i < list.Count; i++)
                {
                    var bm = list[i];
                    if (bm.runtimeObject && bm.runtimeObject.visualObject)
                        bm.runtimeObject.visualObject.SetColor(RTMath.Lerp(bm.runtimeObject.visualObject.GetPrimaryColor(), color, multiply));
                }
            });
        }

        public static void addColorPlayerDistance(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not BeatmapObject beatmapObject)
                return;

            var runtimeObject = beatmapObject.runtimeObject;
            if (!runtimeObject || !runtimeObject.visualObject.gameObject)
                return;

            var offset = modifier.GetFloat(0, 0f, variables);
            var index = modifier.GetInt(1, 0, variables);
            var multiply = modifier.GetFloat(2, 0, variables);

            // queue post tick so the color overrides the sequence color
            RTLevel.Current.postTick.Enqueue(() =>
            {
                var player = PlayerManager.GetClosestPlayer(runtimeObject.visualObject.gameObject.transform.position);

                if (!player.RuntimePlayer || !player.RuntimePlayer.rb)
                    return;

                var distance = Vector2.Distance(player.RuntimePlayer.rb.transform.position, runtimeObject.visualObject.gameObject.transform.position);

                runtimeObject.visualObject.SetColor(runtimeObject.visualObject.GetPrimaryColor() + ThemeManager.inst.Current.GetObjColor(index) * -(distance * multiply - offset));
            });
        }

        public static void lerpColorPlayerDistance(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not BeatmapObject beatmapObject)
                return;

            var runtimeObject = beatmapObject.runtimeObject;
            if (!runtimeObject || !runtimeObject.visualObject.gameObject)
                return;

            var offset = modifier.GetFloat(0, 0f, variables);
            var index = modifier.GetInt(1, 0, variables);
            var multiply = modifier.GetFloat(2, 0f, variables);
            var opacity = modifier.GetFloat(3, 0f, variables);
            var hue = modifier.GetFloat(4, 0f, variables);
            var sat = modifier.GetFloat(5, 0f, variables);
            var val = modifier.GetFloat(6, 0f, variables);

            // queue post tick so the color overrides the sequence color
            RTLevel.Current.postTick.Enqueue(() =>
            {
                var player = PlayerManager.GetClosestPlayer(runtimeObject.visualObject.gameObject.transform.position);

                if (!player.RuntimePlayer || !player.RuntimePlayer.rb)
                    return;

                var distance = Vector2.Distance(player.RuntimePlayer.rb.transform.position, runtimeObject.visualObject.gameObject.transform.position);

                runtimeObject.visualObject.SetColor(Color.Lerp(runtimeObject.visualObject.GetPrimaryColor(),
                                RTColors.FadeColor(RTColors.ChangeColorHSV(ThemeManager.inst.Current.GetObjColor(index), hue, sat, val), opacity),
                                -(distance * multiply - offset)));
            });
        }

        public static void setOpacity(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not BeatmapObject beatmapObject)
                return;

            var runtimeObject = beatmapObject.runtimeObject;
            if (!runtimeObject || !runtimeObject.visualObject.gameObject)
                return;

            var opacity = modifier.GetFloat(0, 1f, variables);

            // queue post tick so the color overrides the sequence color
            RTLevel.Current.postTick.Enqueue(() =>
            {
                runtimeObject.visualObject.SetColor(RTColors.FadeColor(runtimeObject.visualObject.GetPrimaryColor(), opacity));
            });
        }

        public static void setOpacityOther(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not IPrefabable prefabable)
                return;

            var opacity = modifier.GetFloat(0, 1f, variables);

            var list = modifier.GetResultOrDefault(() => GameData.Current.FindObjectsWithTag(modifier, prefabable, modifier.GetValue(1, variables)));

            if (list.IsEmpty())
                return;

            // queue post tick so the color overrides the sequence color
            RTLevel.Current.postTick.Enqueue(() =>
            {
                foreach (var bm in list)
                {
                    if (bm.runtimeObject && bm.runtimeObject.visualObject)
                        bm.runtimeObject.visualObject.SetColor(RTColors.FadeColor(bm.runtimeObject.visualObject.GetPrimaryColor(), opacity));
                }
            });
        }

        public static void copyColor(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not BeatmapObject beatmapObject || !beatmapObject.runtimeObject)
                return;

            var applyColor1 = modifier.GetBool(1, true, variables);
            var applyColor2 = modifier.GetBool(2, true, variables);

            var other = modifier.GetResultOrDefault(() => GameData.Current.FindObjectWithTag(modifier, beatmapObject, modifier.GetValue(0, variables)));

            if (!other || !other.runtimeObject)
                return;

            // queue post tick so the color overrides the sequence color
            RTLevel.Current.postTick.Enqueue(() => ModifiersHelper.CopyColor(beatmapObject.runtimeObject, other.runtimeObject, applyColor1, applyColor2));
        }

        public static void copyColorOther(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not BeatmapObject beatmapObject)
                return;

            var list = modifier.GetResultOrDefault(() => GameData.Current.FindObjectsWithTag(modifier, beatmapObject, modifier.GetValue(0, variables)));

            if (list.IsEmpty())
                return;

            var applyColor1 = modifier.GetBool(1, true, variables);
            var applyColor2 = modifier.GetBool(2, true, variables);

            var runtimeObject = beatmapObject.runtimeObject;
            if (!runtimeObject)
                return;

            // queue post tick so the color overrides the sequence color
            RTLevel.Current.postTick.Enqueue(() =>
            {
                foreach (var bm in list)
                {
                    var otherRuntimeObject = bm.runtimeObject;
                    if (!otherRuntimeObject)
                        continue;

                    ModifiersHelper.CopyColor(otherRuntimeObject, runtimeObject, applyColor1, applyColor2);
                }
            });
        }

        public static void applyColorGroup(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not BeatmapObject beatmapObject)
                return;

            var list = modifier.GetResultOrDefault(() => GameData.Current.FindObjectsWithTag(modifier, beatmapObject, modifier.GetValue(0, variables)));

            var cachedSequences = beatmapObject.cachedSequences;
            if (list.IsEmpty() || !cachedSequences)
                return;

            var type = modifier.GetInt(1, 0, variables);
            var axis = modifier.GetInt(2, 0, variables);

            // queue post tick so the color overrides the sequence color
            RTLevel.Current.postTick.Enqueue(() =>
            {
                var time = beatmapObject.GetParentRuntime().CurrentTime - beatmapObject.StartTime;
                var colors = ModifiersHelper.GetColors(beatmapObject, time);
                Color color = colors.startColor;
                Color secondColor = colors.endColor;

                var isEmpty = beatmapObject.objectType == BeatmapObject.ObjectType.Empty;

                float t = !isEmpty ? type switch
                {
                    0 => axis == 0 ? cachedSequences.PositionSequence.Value.x : axis == 1 ? cachedSequences.PositionSequence.Value.y : cachedSequences.PositionSequence.Value.z,
                    1 => axis == 0 ? cachedSequences.ScaleSequence.Value.x : cachedSequences.ScaleSequence.Value.y,
                    2 => cachedSequences.RotationSequence.Value,
                    _ => 0f
                } : type switch
                {
                    0 => axis == 0 ? cachedSequences.PositionSequence.GetValue(time).x : axis == 1 ? cachedSequences.PositionSequence.GetValue(time).y : cachedSequences.PositionSequence.GetValue(time).z,
                    1 => axis == 0 ? cachedSequences.ScaleSequence.GetValue(time).x : cachedSequences.ScaleSequence.GetValue(time).y,
                    2 => cachedSequences.RotationSequence.GetValue(time),
                    _ => 0f
                };

                foreach (var other in list)
                {
                    var otherRuntimeObject = other.runtimeObject;
                    if (!otherRuntimeObject)
                        continue;

                    if (!otherRuntimeObject.visualObject.isGradient)
                        otherRuntimeObject.visualObject.SetColor(Color.Lerp(otherRuntimeObject.visualObject.GetPrimaryColor(), color, t));
                    else if (otherRuntimeObject.visualObject is SolidObject solidObject)
                    {
                        var otherColors = solidObject.GetColors();
                        solidObject.SetColor(Color.Lerp(otherColors.startColor, color, t), Color.Lerp(otherColors.endColor, secondColor, t));
                    }
                }
            });
        }

        public static void setColorHex(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not BeatmapObject beatmapObject)
                return;

            var runtimeObject = beatmapObject.runtimeObject;
            if (!runtimeObject)
                return;

            var color1 = modifier.GetValue(0, variables);
            var color2 = modifier.GetValue(1, variables);

            // queue post tick so the color overrides the sequence color
            RTLevel.Current.postTick.Enqueue(() =>
            {
                if (!runtimeObject.visualObject.isGradient)
                {
                    var color = runtimeObject.visualObject.GetPrimaryColor();
                    runtimeObject.visualObject.SetColor(string.IsNullOrEmpty(color1) ? color : color1.Length == 8 ? RTColors.HexToColor(color1) : RTColors.FadeColor(RTColors.HexToColor(color1), color.a));
                }
                else if (runtimeObject.visualObject is SolidObject solidObject)
                {
                    var colors = solidObject.GetColors();
                    solidObject.SetColor(
                        string.IsNullOrEmpty(color1) ? colors.startColor : color1.Length == 8 ? RTColors.HexToColor(color1) : RTColors.FadeColor(RTColors.HexToColor(color1), colors.startColor.a),
                        string.IsNullOrEmpty(color2) ? colors.endColor : color2.Length == 8 ? RTColors.HexToColor(color2) : RTColors.FadeColor(RTColors.HexToColor(color2), colors.endColor.a));
                }
            });
        }

        public static void setColorHexOther(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not IPrefabable prefabable)
                return;

            List<BeatmapObject> list = modifier.GetResultOrDefault(() => GameData.Current.FindObjectsWithTag(modifier, prefabable, modifier.GetValue(1, variables)));

            if (list.IsEmpty())
                return;

            var color1 = modifier.GetValue(0, variables);
            var color2 = modifier.GetValue(2, variables);

            // queue post tick so the color overrides the sequence color
            RTLevel.Current.postTick.Enqueue(() =>
            {
                foreach (var bm in list)
                {
                    var runtimeObject = bm.runtimeObject;
                    if (!runtimeObject)
                        continue;

                    if (!runtimeObject.visualObject.isGradient)
                    {
                        var color = runtimeObject.visualObject.GetPrimaryColor();
                        runtimeObject.visualObject.SetColor(string.IsNullOrEmpty(color1) ? color : color1.Length == 8 ? RTColors.HexToColor(color1) : RTColors.FadeColor(LSColors.HexToColorAlpha(color1), color.a));
                    }
                    else if (runtimeObject.visualObject is SolidObject solidObject)
                    {
                        var colors = solidObject.GetColors();
                        solidObject.SetColor(
                            string.IsNullOrEmpty(color1) ? colors.startColor : color1.Length == 8 ? RTColors.HexToColor(color1) : RTColors.FadeColor(LSColors.HexToColorAlpha(color1), colors.startColor.a),
                            string.IsNullOrEmpty(color2) ? colors.endColor : color2.Length == 8 ? RTColors.HexToColor(color2) : RTColors.FadeColor(LSColors.HexToColorAlpha(color2), colors.endColor.a));
                    }
                }
            });
        }

        public static void setColorRGBA(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not BeatmapObject beatmapObject)
                return;

            var runtimeObject = beatmapObject.runtimeObject;
            if (!runtimeObject)
                return;

            var color1 = new Color(modifier.GetFloat(0, 1f, variables), modifier.GetFloat(1, 1f, variables), modifier.GetFloat(2, 1f, variables), modifier.GetFloat(3, 1f, variables));
            var color2 = new Color(modifier.GetFloat(4, 1f, variables), modifier.GetFloat(5, 1f, variables), modifier.GetFloat(6, 1f, variables), modifier.GetFloat(7, 1f, variables));

            // queue post tick so the color overrides the sequence color
            RTLevel.Current.postTick.Enqueue(() =>
            {
                if (!runtimeObject.visualObject.isGradient)
                    runtimeObject.visualObject.SetColor(color1);
                else if (runtimeObject.visualObject is SolidObject solidObject)
                    solidObject.SetColor(color1, color2);
            });
        }

        public static void setColorRGBAOther(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not IPrefabable prefabable)
                return;

            var list = GameData.Current.FindObjectsWithTag(modifier, prefabable, modifier.GetValue(8, variables));

            if (list.IsEmpty())
                return;

            var color1 = new Color(modifier.GetFloat(0, 1f, variables), modifier.GetFloat(1, 1f, variables), modifier.GetFloat(2, 1f, variables), modifier.GetFloat(3, 1f, variables));
            var color2 = new Color(modifier.GetFloat(4, 1f, variables), modifier.GetFloat(5, 1f, variables), modifier.GetFloat(6, 1f, variables), modifier.GetFloat(7, 1f, variables));

            // queue post tick so the color overrides the sequence color
            RTLevel.Current.postTick.Enqueue(() =>
            {
                foreach (var bm in list)
                {
                    var runtimeObject = bm.runtimeObject;
                    if (!runtimeObject)
                        continue;

                    if (!runtimeObject.visualObject.isGradient)
                        runtimeObject.visualObject.SetColor(color1);
                    else if (runtimeObject.visualObject is SolidObject solidObject)
                        solidObject.SetColor(color1, color2);
                }
            });
        }

        public static void animateColorKF(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not ILifetime lifetime)
                return;

            Sequence<Color> sequence1;
            Sequence<Color> sequence2;

            var audioTime = modifier.GetFloat(0, 0f, variables);
            var colorSource = modifier.GetInt(1, 0, variables);

            if (modifier.TryGetResult(out KeyValuePair<Sequence<Color>, Sequence<Color>> sequences))
            {
                sequence1 = sequences.Key;
                sequence2 = sequences.Value;
            }
            else
            {
                // custom start colors
                var colorSlot1Start = modifier.GetInt(2, 0, variables);
                var opacity1Start = modifier.GetFloat(3, 1f, variables);
                var hue1Start = modifier.GetFloat(4, 0f, variables);
                var saturation1Start = modifier.GetFloat(5, 0f, variables);
                var value1Start = modifier.GetFloat(6, 0f, variables);
                var colorSlot2Start = modifier.GetInt(7, 0, variables);
                var opacity2Start = modifier.GetFloat(8, 1f, variables);
                var hue2Start = modifier.GetFloat(9, 0f, variables);
                var saturation2Start = modifier.GetFloat(10, 0f, variables);
                var value2Start = modifier.GetFloat(11, 0f, variables);

                var currentTime = 0f;

                var keyframes1 = new List<IKeyframe<Color>>();
                keyframes1.Add(new CustomThemeKeyframe(currentTime, colorSource, colorSlot1Start, opacity1Start, hue1Start, saturation1Start, value1Start, Ease.Linear, false));
                var keyframes2 = new List<IKeyframe<Color>>();
                keyframes2.Add(new CustomThemeKeyframe(currentTime, colorSource, colorSlot2Start, opacity2Start, hue2Start, saturation2Start, value2Start, Ease.Linear, false));
                for (int i = 12; i < modifier.values.Count; i += 14)
                {
                    var time = modifier.GetFloat(i + 1, 0f, variables);
                    if (time < currentTime)
                        continue;

                    var colorSlot1 = modifier.GetInt(i + 2, 0, variables);
                    var opacity1 = modifier.GetFloat(i + 3, 1f, variables);
                    var hue1 = modifier.GetFloat(i + 4, 0f, variables);
                    var saturation1 = modifier.GetFloat(i + 5, 0f, variables);
                    var value1 = modifier.GetFloat(i + 6, 0f, variables);
                    var colorSlot2 = modifier.GetInt(i + 7, 0, variables);
                    var opacity2 = modifier.GetFloat(i + 8, 1f, variables);
                    var hue2 = modifier.GetFloat(i + 9, 0f, variables);
                    var saturation2 = modifier.GetFloat(i + 10, 0f, variables);
                    var value2 = modifier.GetFloat(i + 11, 0f, variables);
                    var relative = modifier.GetBool(i + 12, true, variables);

                    var easing = modifier.GetValue(i + 13, variables);
                    if (int.TryParse(easing, out int e) && e >= 0 && e < DataManager.inst.AnimationList.Count)
                        easing = DataManager.inst.AnimationList[e].Name;

                    var ease = Ease.GetEaseFunction(easing, Ease.Linear);
                    keyframes1.Add(new CustomThemeKeyframe(currentTime + time, colorSource, colorSlot1, opacity1, hue1, saturation1, value1, ease, false));
                    keyframes2.Add(new CustomThemeKeyframe(currentTime + time, colorSource, colorSlot2, opacity2, hue2, saturation2, value2, ease, false));

                    currentTime = time;
                }

                sequence1 = new Sequence<Color>(keyframes1);
                sequence2 = new Sequence<Color>(keyframes2);

                modifier.Result = new KeyValuePair<Sequence<Color>, Sequence<Color>>(sequence1, sequence2);
            }

            var beatmapObject = reference as BeatmapObject;
            var backgroundObject = reference as BackgroundObject;

            var startTime = lifetime.StartTime;

            RTLevel.Current.postTick.Enqueue(() =>
            {
                var primaryColor = Color.white;
                var secondaryColor = Color.white;

                primaryColor = sequence1.GetValue(audioTime - startTime);
                secondaryColor = sequence2.GetValue(audioTime - startTime);

                if (beatmapObject && beatmapObject.runtimeObject && beatmapObject.runtimeObject.visualObject is SolidObject solidObject)
                {
                    if (solidObject.isGradient)
                        solidObject.SetColor(primaryColor, secondaryColor);
                    else
                        solidObject.SetColor(primaryColor);
                }

                if (backgroundObject && backgroundObject.runtimeObject)
                    backgroundObject.runtimeObject.SetColor(primaryColor, secondaryColor);
            });
        }

        public static void animateColorKFHex(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not ILifetime lifetime)
                return;

            Sequence<Color> sequence1;
            Sequence<Color> sequence2;

            var audioTime = modifier.GetFloat(0, 0f, variables);

            if (modifier.TryGetResult(out KeyValuePair<Sequence<Color>, Sequence<Color>> sequences))
            {
                sequence1 = sequences.Key;
                sequence2 = sequences.Value;
            }
            else
            {
                // custom start colors
                var color1Start = modifier.GetValue(1, variables);
                var color2Start = modifier.GetValue(2, variables);

                var currentTime = 0f;

                var keyframes1 = new List<IKeyframe<Color>>();
                keyframes1.Add(new ColorKeyframe(currentTime, RTColors.HexToColor(color1Start), Ease.Linear));
                var keyframes2 = new List<IKeyframe<Color>>();
                keyframes2.Add(new ColorKeyframe(currentTime, RTColors.HexToColor(color2Start), Ease.Linear));
                for (int i = 3; i < modifier.values.Count; i += 6)
                {
                    var time = modifier.GetFloat(i + 1, 0f, variables);
                    if (time < currentTime)
                        continue;

                    var color1 = modifier.GetValue(i + 2, variables);
                    var color2 = modifier.GetValue(i + 3, variables);
                    var relative = modifier.GetBool(i + 4, true, variables);

                    var easing = modifier.GetValue(i + 5, variables);
                    if (int.TryParse(easing, out int e) && e >= 0 && e < DataManager.inst.AnimationList.Count)
                        easing = DataManager.inst.AnimationList[e].Name;

                    var ease = Ease.GetEaseFunction(easing, Ease.Linear);
                    keyframes1.Add(new ColorKeyframe(currentTime + time, RTColors.HexToColor(color1), ease));
                    keyframes2.Add(new ColorKeyframe(currentTime + time, RTColors.HexToColor(color2), ease));

                    currentTime = time;
                }

                sequence1 = new Sequence<Color>(keyframes1);
                sequence2 = new Sequence<Color>(keyframes2);

                modifier.Result = new KeyValuePair<Sequence<Color>, Sequence<Color>>(sequence1, sequence2);
            }

            var beatmapObject = reference as BeatmapObject;
            var backgroundObject = reference as BackgroundObject;

            var startTime = lifetime.StartTime;

            RTLevel.Current.postTick.Enqueue(() =>
            {
                var primaryColor = Color.white;
                var secondaryColor = Color.white;

                primaryColor = sequence1.GetValue(audioTime - startTime);
                primaryColor = sequence2.GetValue(audioTime - startTime);

                if (beatmapObject && beatmapObject.runtimeObject && beatmapObject.runtimeObject.visualObject is SolidObject solidObject)
                {
                    if (solidObject.isGradient)
                        solidObject.SetColor(primaryColor, secondaryColor);
                    else
                        solidObject.SetColor(primaryColor);
                }

                if (backgroundObject && backgroundObject.runtimeObject)
                    backgroundObject.runtimeObject.SetColor(primaryColor, secondaryColor);
            });
        }

        #endregion

        #region Shape

        public static void translateShape(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not BeatmapObject beatmapObject)
                return;

            var runtimeObject = beatmapObject.runtimeObject;
            if (!runtimeObject || !runtimeObject.visualObject.gameObject)
                return;

            var pos = new Vector2(modifier.GetFloat(1, 0f, variables), modifier.GetFloat(2, 0f, variables));
            var sca = new Vector2(modifier.GetFloat(3, 0f, variables), modifier.GetFloat(4, 0f, variables));
            var rot = modifier.GetFloat(5, 0f, variables);

            if (!modifier.HasResult())
            {
                var meshFilter = runtimeObject.visualObject.gameObject.GetComponent<MeshFilter>();
                var collider2D = runtimeObject.visualObject.collider as PolygonCollider2D;
                var mesh = meshFilter.mesh;

                var translateShapeCache = new TranslateShapeCache
                {
                    meshFilter = meshFilter,
                    collider2D = collider2D,
                    vertices = mesh?.vertices ?? null,
                    points = collider2D?.points ?? null,

                    pos = pos,
                    sca = sca,
                    rot = rot,
                };
                modifier.Result = translateShapeCache;
                // force translate for first frame
                translateShapeCache.Translate(pos, sca, rot, true);

                runtimeObject.visualObject.gameObject.AddComponent<DestroyModifierResult>().Modifier = modifier;
                return;
            }

            if (modifier.TryGetResult(out TranslateShapeCache shapeCache))
                shapeCache.Translate(pos, sca, rot);
        }

        public static void setShape(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not IShapeable shapeable)
                return;

            shapeable.SetCustomShape(modifier.GetInt(0, 0, variables), modifier.GetInt(1, 0, variables));
            if (shapeable is BeatmapObject beatmapObject)
                reference.GetParentRuntime()?.UpdateObject(beatmapObject, ObjectContext.SHAPE);
            else if (shapeable is BackgroundObject backgroundObject)
                backgroundObject.runtimeObject?.UpdateShape(backgroundObject.Shape, backgroundObject.ShapeOption);
        }

        public static void setPolygonShape(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not BeatmapObject beatmapObject || !beatmapObject.runtimeObject || beatmapObject.runtimeObject.visualObject is not PolygonObject polygonObject)
                return;

            var radius = RTMath.Clamp(modifier.GetFloat(0, 0.5f, variables), 0.1f, 10f);
            var sides = RTMath.Clamp(modifier.GetInt(1, 3, variables), 3, 32);
            var roundness = RTMath.Clamp(modifier.GetFloat(2, 0f, variables), 0f, 1f);
            var thickness = RTMath.Clamp(modifier.GetFloat(3, 1f, variables), 0f, 1f);
            var slices = RTMath.Clamp(modifier.GetInt(4, 3, variables), 0, sides);
            var thicknessOffset = new Vector2(modifier.GetFloat(5, 0f, variables), modifier.GetFloat(6, 0f, variables));
            var thicknessScale = new Vector2(modifier.GetFloat(7, 1f, variables), modifier.GetFloat(8, 1f, variables));
            var rotation = modifier.GetFloat(9, 0f, variables);

            var meshParams = new VGShapes.MeshParams
            {
                radius = radius,
                VertexCount = sides,
                cornerRoundness = roundness,
                thickness = thickness,
                SliceCount = slices,
                thicknessOffset = thicknessOffset,
                thicknessScale = thicknessScale,
                rotation = rotation,
            };

            if (modifier.TryGetResult(out VGShapes.MeshParams cache) && meshParams.Equals(cache))
                return;

            polygonObject.UpdatePolygon(radius, sides, roundness, thickness, slices, thicknessOffset, thicknessScale, rotation);
            modifier.Result = meshParams;
        }

        public static void setPolygonShapeOther(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not IPrefabable prefabable)
                return;

            var radius = RTMath.Clamp(modifier.GetFloat(1, 0.5f, variables), 0.1f, 10f);
            var sides = RTMath.Clamp(modifier.GetInt(2, 3, variables), 3, 32);
            var roundness = RTMath.Clamp(modifier.GetFloat(3, 0f, variables), 0f, 1f);
            var thickness = RTMath.Clamp(modifier.GetFloat(4, 1f, variables), 0f, 1f);
            var slices = RTMath.Clamp(modifier.GetInt(5, 3, variables), 0, sides);
            var thicknessOffset = new Vector2(modifier.GetFloat(6, 0f, variables), modifier.GetFloat(7, 0f, variables));
            var thicknessScale = new Vector2(modifier.GetFloat(8, 1f, variables), modifier.GetFloat(9, 1f, variables));
            var rotation = modifier.GetFloat(10, 0f, variables);

            var meshParams = new VGShapes.MeshParams
            {
                VertexCount = sides,
                cornerRoundness = roundness,
                thickness = thickness,
                SliceCount = slices,
                thicknessOffset = thicknessOffset,
                thicknessScale = thicknessScale,
                rotation = rotation,
            };

            if (modifier.TryGetResult(out VGShapes.MeshParams cache) && meshParams.Equals(cache))
                return;

            var list = GameData.Current.FindObjectsWithTag(modifier, prefabable, modifier.GetValue(0, variables));

            for (int i = 0; i < list.Count; i++)
            {
                var beatmapObject = list[i];
                if (beatmapObject.runtimeObject && beatmapObject.runtimeObject.visualObject is PolygonObject polygonObject)
                    polygonObject.UpdatePolygon(radius, sides, roundness, thickness, slices, thicknessOffset, thicknessScale, rotation);
            }

            modifier.Result = meshParams;
        }

        public static void actorFrameTexture(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not BeatmapObject beatmapObject || beatmapObject.ShapeType == ShapeType.Image)
                return;

            var runtimeObject = beatmapObject.runtimeObject;
            if (!runtimeObject || !runtimeObject.visualObject)
                return;

            var camera = modifier.GetInt(0, 0, variables) switch
            {
                1 => RTLevel.Cameras.BG,
                2 => RTLevel.Cameras.UI,
                _ => RTLevel.Cameras.FG,
            };
            var width = modifier.GetInt(1, 512, variables);
            var height = modifier.GetInt(2, 512, variables);
            var offsetX = modifier.GetFloat(3, 0f, variables);
            var offsetY = modifier.GetFloat(4, 0f, variables);
            var zoom = modifier.GetFloat(5, 1f, variables);
            var rotate = modifier.GetFloat(6, 0f, variables);

            var renderer = runtimeObject.visualObject.renderer;
            if (!renderer)
                return;

            if (runtimeObject.visualObject is not SolidObject solidObject)
                return;

            // Get render texture
            var result = modifier.GetResultOrDefault(() =>
            {
                var cache = new MaskCache()
                {
                    width = width,
                    height = height,
                    renderTexture = new RenderTexture(width, height, 24)
                    {
                        name = SpriteHelper.DEFAULT_TEXTURE_NAME,
                        wrapMode = TextureWrapMode.Clamp,
                        useDynamicScale = true,
                    },
                };
                renderer.material.SetTexture("_MainTex", cache.renderTexture);
                DestroyModifierResult.Init(solidObject.gameObject, modifier);
                return cache;
            });
            if (result.width != width || result.height != height)
            {
                CoreHelper.Destroy(result.renderTexture);

                result = new MaskCache()
                {
                    width = width,
                    height = height,
                    renderTexture = new RenderTexture(width, height, 24)
                    {
                        name = SpriteHelper.DEFAULT_TEXTURE_NAME,
                        wrapMode = TextureWrapMode.Clamp,
                        useDynamicScale = true,
                    },
                };
                renderer.material.SetTexture("_MainTex", result.renderTexture);
                modifier.Result = result;
            }

            camera.transform.localPosition = new Vector3(offsetX, offsetY);
            camera.transform.localEulerAngles = new Vector3(0f, 0f, rotate);

            RTLevel.Current.eventEngine.SetCameraPosition(Vector3.zero);
            RTLevel.Current.eventEngine.SetCameraRotation(0f);

            EventManager.inst.camParent.transform.localPosition = Vector2.zero;
            var trackerPos = RTEventManager.inst.delayTracker.transform.localPosition;
            RTEventManager.inst.delayTracker.transform.localPosition = Vector2.zero;

            var rect = RTLevel.Cameras.FG.rect;
            RTLevel.Cameras.FG.rect = new Rect(0f, 0f, 1f, 1f);
            var total = width + height;
            RTLevel.Current.eventEngine.SetZoom(total / 2 / 512f * 12.66f * zoom);

            //var playersActive = GameManager.inst.players.activeSelf;
            //if (hidePlayers)
            //    GameManager.inst.players.SetActive(false);

            var clearFlags = RTLevel.Cameras.FG.clearFlags;
            var bgColor = RTLevel.Cameras.FG.backgroundColor;
            RTLevel.Cameras.FG.clearFlags = CameraClearFlags.SolidColor;
            RTLevel.Cameras.FG.backgroundColor = RTLevel.Cameras.BG.backgroundColor;

            // create
            var renderTexture = result.renderTexture;

            var currentActiveRT = RenderTexture.active;
            RenderTexture.active = renderTexture;

            var enabled = renderer.enabled;
            renderer.enabled = false;
            // Assign render texture to camera and render the camera
            camera.targetTexture = renderTexture;
            camera.Render();
            renderTexture.Create();

            // Reset to defaults
            renderer.enabled = enabled;
            camera.targetTexture = null;
            RenderTexture.active = currentActiveRT;

            camera.transform.localPosition = Vector3.zero;
            camera.transform.localEulerAngles = Vector3.zero;

            var editorCam = EventsConfig.Instance.EditorCameraEnabled;

            RTLevel.Current.eventEngine.SetCameraRotation(editorCam ?
                new Vector3(RTLevel.Current.eventEngine.editorCamPerRotate.x, RTLevel.Current.eventEngine.editorCamPerRotate.y, RTLevel.Current.eventEngine.editorCamRotate) :
                new Vector3(RTLevel.Current.eventEngine.camRotOffset.x, RTLevel.Current.eventEngine.camRotOffset.y, EventManager.inst.camRot));

            RTLevel.Current.eventEngine.SetCameraPosition(editorCam ?
                RTLevel.Current.eventEngine.editorCamPosition :
                EventManager.inst.camPos);

            // fixes bg camera position being offset if rotated for some reason...
            RTLevel.Cameras.BG.transform.SetLocalPositionX(0f);
            RTLevel.Cameras.BG.transform.SetLocalPositionY(0f);

            RTLevel.Current.eventEngine.SetZoom(editorCam ?
                RTLevel.Current.eventEngine.editorCamZoom :
                EventManager.inst.camZoom);

            RTLevel.Current.eventEngine.UpdateShake();

            RTEventManager.inst.delayTracker.transform.localPosition = trackerPos;

            RTLevel.Cameras.FG.clearFlags = clearFlags;
            RTLevel.Cameras.FG.backgroundColor = bgColor;

            // disable and re-enable the glitch camera to ensure the glitch camera is ordered last.
            RTEventManager.inst.glitchCam.enabled = false;
            RTEventManager.inst.glitchCam.enabled = true;

            // disable and re-enable the UI camera to ensure the UI camera is ordered last.
            RTLevel.Cameras.UI.enabled = false;
            RTLevel.Cameras.UI.enabled = true;

            RTLevel.Cameras.FG.rect = rect;

            //if (hidePlayers)
            //    GameManager.inst.players.SetActive(true);

            //if (useCustomBGColor)
            //{
            //    RTLevel.Cameras.FG.clearFlags = clearFlags;
            //    RTLevel.Cameras.FG.backgroundColor = bgColor;
            //}
        }

        public static void setImage(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (modifier.constant || reference is not BeatmapObject beatmapObject || !beatmapObject.runtimeObject)
                return;

            var value = modifier.GetValue(0, variables);
            value = ModifiersHelper.FormatStringVariables(value, variables);
            var sprite = beatmapObject.GetSprite(value);

            if (beatmapObject.runtimeObject.visualObject is ImageObject imageObject)
            {
                if (sprite)
                {
                    imageObject.SetSprite(sprite);
                    return;
                }

                var path = RTFile.CombinePaths(RTFile.BasePath, value);

                if (!RTFile.FileExists(path))
                {
                    imageObject.SetDefaultSprite();
                    return;
                }

                CoroutineHelper.StartCoroutine(AlephNetwork.DownloadImageTexture("file://" + path, imageObject.SetTexture, imageObject.SetDefaultSprite));
            }
            else if (beatmapObject.runtimeObject.visualObject is SolidObject solidObject && solidObject.renderer)
            {
                var renderer = solidObject.renderer;
                if (!renderer)
                    return;

                if (sprite)
                {
                    renderer.material.SetTexture("_MainTex", sprite.texture);
                    return;
                }

                var assetPath = AssetPack.GetFile(value);
                var path = RTFile.FileExists(assetPath) ? assetPath : RTFile.CombinePaths(RTFile.BasePath, value);
                if (!RTFile.FileExists(path))
                    return;

                CoroutineHelper.StartCoroutine(AlephNetwork.DownloadImageTexture("file://" + path,
                    texture2D =>
                    {
                        if (!beatmapObject.runtimeObject || beatmapObject.runtimeObject.visualObject is not SolidObject solidObject)
                            return;

                        var renderer = solidObject.renderer;
                        if (renderer)
                            renderer.material.SetTexture("_MainTex", texture2D);
                    }));
            }
        }

        public static void setImageOther(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (modifier.constant)
                return;

            if (reference is not IPrefabable prefabable)
                return;

            var list = GameData.Current.FindObjectsWithTag(modifier, prefabable, modifier.GetValue(1, variables));

            if (list.IsEmpty())
                return;

            var value = modifier.GetValue(0, variables);
            value = ModifiersHelper.FormatStringVariables(value, variables);

            Sprite sprite = null;
            if (prefabable.FromPrefab && prefabable.GetPrefab() is Prefab prefab && prefab.assets.sprites.TryFind(x => x.name == value, out SpriteAsset spriteAsset))
                sprite = spriteAsset.sprite;
            else
                sprite = GameData.Current.assets.GetSprite(value);

            if (sprite)
            {
                foreach (var bm in list)
                {
                    if (bm.ShapeType == ShapeType.Image && bm.runtimeObject && bm.runtimeObject.visualObject is ImageObject imageObject)
                        imageObject.SetSprite(sprite);
                    else if (bm.runtimeObject && bm.runtimeObject.visualObject is SolidObject solidObject)
                        solidObject.renderer.material.SetTexture("_MainTex", sprite.texture);
                }
                return;
            }

            var assetPath = AssetPack.GetFile(value);
            var path = RTFile.FileExists(assetPath) ? assetPath : RTFile.CombinePaths(RTFile.BasePath, value);
            if (!RTFile.FileExists(path))
            {
                foreach (var bm in list)
                {
                    if (bm.ShapeType == ShapeType.Image && bm.runtimeObject && bm.runtimeObject.visualObject is ImageObject imageObject)
                        imageObject.SetDefaultSprite();
                }
                return;
            }

            CoroutineHelper.StartCoroutine(AlephNetwork.DownloadImageTexture("file://" + path,
                texture2D =>
                {
                    foreach (var bm in list)
                    {
                        if (bm.ShapeType == ShapeType.Image && bm.runtimeObject && bm.runtimeObject.visualObject is ImageObject imageObject)
                            imageObject.SetTexture(texture2D);
                        else if (bm.runtimeObject && bm.runtimeObject.visualObject is SolidObject solidObject)
                            solidObject.renderer.material.SetTexture("_MainTex", texture2D);
                    }
                },
                onError =>
                {
                    foreach (var bm in list)
                    {
                        if (bm.ShapeType == ShapeType.Image && bm.runtimeObject && bm.runtimeObject.visualObject is ImageObject imageObject)
                            imageObject.SetDefaultSprite();
                    }
                }));
        }

        public static void setText(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not BeatmapObject beatmapObject || beatmapObject.ShapeType != ShapeType.Text || !beatmapObject.runtimeObject || beatmapObject.runtimeObject.visualObject is not TextObject textObject)
                return;

            var text = modifier.GetValue(0, variables);
            text = ModifiersHelper.FormatStringVariables(text, variables);

            if (modifier.constant || !CoreConfig.Instance.AllowCustomTextFormatting.Value)
                textObject.SetText(text);
            else
                textObject.text = text;
        }

        public static void setTextOther(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not IPrefabable prefabable)
                return;

            var list = GameData.Current.FindObjectsWithTag(modifier, prefabable, modifier.GetValue(1, variables));

            if (list.IsEmpty())
                return;

            var text = modifier.GetValue(0, variables);
            text = ModifiersHelper.FormatStringVariables(text, variables);

            foreach (var bm in list)
            {
                if (bm.ShapeType != ShapeType.Text || !bm.runtimeObject || bm.runtimeObject.visualObject is not TextObject textObject)
                    continue;

                if (modifier.constant || !CoreConfig.Instance.AllowCustomTextFormatting.Value)
                    textObject.SetText(text);
                else
                    textObject.text = text;
            }
        }

        public static void addText(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not BeatmapObject beatmapObject || beatmapObject.ShapeType != ShapeType.Text || !beatmapObject.runtimeObject || beatmapObject.runtimeObject.visualObject is not TextObject textObject)
                return;

            var text = modifier.GetValue(0, variables);
            text = ModifiersHelper.FormatStringVariables(text, variables);

            if (modifier.constant || !CoreConfig.Instance.AllowCustomTextFormatting.Value)
                textObject.SetText(textObject.textMeshPro.text + text);
            else
                textObject.text += text;
        }

        public static void addTextOther(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not IPrefabable prefabable)
                return;

            var list = GameData.Current.FindObjectsWithTag(modifier, prefabable, modifier.GetValue(1, variables));

            if (list.IsEmpty())
                return;

            var text = modifier.GetValue(0, variables);
            text = ModifiersHelper.FormatStringVariables(text, variables);

            foreach (var bm in list)
            {
                if (bm.ShapeType != ShapeType.Text || !bm.runtimeObject || bm.runtimeObject.visualObject is not TextObject textObject)
                    continue;

                if (modifier.constant || !CoreConfig.Instance.AllowCustomTextFormatting.Value)
                    textObject.SetText(textObject.textMeshPro.text + text);
                else
                    textObject.text += text;
            }
        }

        public static void removeText(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not BeatmapObject beatmapObject || beatmapObject.ShapeType != ShapeType.Text || !beatmapObject.runtimeObject || beatmapObject.runtimeObject.visualObject is not TextObject textObject)
                return;

            string text = string.IsNullOrEmpty(textObject.textMeshPro.text) ? string.Empty :
                textObject.textMeshPro.text.Substring(0, textObject.textMeshPro.text.Length - Mathf.Clamp(modifier.GetInt(0, 1, variables), 0, textObject.textMeshPro.text.Length));

            if (modifier.constant || !CoreConfig.Instance.AllowCustomTextFormatting.Value)
                textObject.SetText(text);
            else
                textObject.text = text;
        }

        public static void removeTextOther(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not IPrefabable prefabable)
                return;

            var list = GameData.Current.FindObjectsWithTag(modifier, prefabable, modifier.GetValue(1, variables));

            if (list.IsEmpty())
                return;

            var remove = modifier.GetInt(0, 1, variables);

            foreach (var bm in list)
            {
                var levelObject = bm.runtimeObject;
                if (bm.ShapeType != ShapeType.Text || !levelObject || levelObject.visualObject is not TextObject textObject)
                    continue;

                string text = string.IsNullOrEmpty(textObject.textMeshPro.text) ? string.Empty :
                    textObject.textMeshPro.text.Substring(0, textObject.textMeshPro.text.Length - Mathf.Clamp(remove, 0, textObject.textMeshPro.text.Length));

                if (modifier.constant || !CoreConfig.Instance.AllowCustomTextFormatting.Value)
                    textObject.SetText(text);
                else
                    textObject.text = text;
            }
        }

        public static void removeTextAt(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not BeatmapObject beatmapObject || beatmapObject.ShapeType != ShapeType.Text || !beatmapObject.runtimeObject || beatmapObject.runtimeObject.visualObject is not TextObject textObject)
                return;

            var remove = modifier.GetInt(0, 1, variables);
            string text = string.IsNullOrEmpty(textObject.textMeshPro.text) ? string.Empty : textObject.textMeshPro.text.Length > remove ?
                textObject.textMeshPro.text.Remove(remove, 1) : string.Empty;

            if (modifier.constant || !CoreConfig.Instance.AllowCustomTextFormatting.Value)
                textObject.SetText(text);
            else
                textObject.text = text;
        }

        public static void removeTextOtherAt(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not IPrefabable prefabable)
                return;

            var list = GameData.Current.FindObjectsWithTag(modifier, prefabable, modifier.GetValue(1, variables));

            if (list.IsEmpty())
                return;

            var remove = modifier.GetInt(0, 1, variables);

            foreach (var bm in list)
            {
                if (bm.ShapeType != ShapeType.Text || !bm.runtimeObject || bm.runtimeObject.visualObject is not TextObject textObject)
                    continue;

                string text = string.IsNullOrEmpty(textObject.textMeshPro.text) ? string.Empty : textObject.textMeshPro.text.Length > remove ?
                    textObject.textMeshPro.text.Remove(remove, 1) : string.Empty;

                if (modifier.constant || !CoreConfig.Instance.AllowCustomTextFormatting.Value)
                    textObject.SetText(text);
                else
                    textObject.text = text;
            }
        }

        public static void formatText(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (!CoreConfig.Instance.AllowCustomTextFormatting.Value && reference is BeatmapObject beatmapObject && beatmapObject.ShapeType == ShapeType.Text &&
                beatmapObject.runtimeObject is RTBeatmapObject runtimeObject && runtimeObject.visualObject is TextObject textObject)
                textObject.SetText(RTString.FormatText(beatmapObject, textObject.text, variables));
        }

        public static void textSequence(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not BeatmapObject beatmapObject || beatmapObject.ShapeType != ShapeType.Text || !beatmapObject.runtimeObject || beatmapObject.runtimeObject.visualObject is not TextObject textObject)
                return;

            var value = modifier.GetValue(9, variables);
            var text = !string.IsNullOrEmpty(value) ? value : beatmapObject.text;
            text = ModifiersHelper.FormatStringVariables(text, variables);

            if (!modifier.setTimer)
            {
                modifier.setTimer = true;
                modifier.ResultTimer = AudioManager.inst.CurrentAudioSource.time;
            }

            var offsetTime = modifier.ResultTimer;
            if (!modifier.GetBool(11, false, variables))
                offsetTime = beatmapObject.StartTime;

            var time = AudioManager.inst.CurrentAudioSource.time - offsetTime + modifier.GetFloat(10, 0f, variables);
            var length = modifier.GetFloat(0, 1f, variables);
            var glitch = modifier.GetBool(1, true, variables);

            var p = time / length;

            var textWithoutFormatting = text;
            var tagLocations = new List<Vector2Int>();
            RTString.RegexMatches(text, new Regex(@"<(.*?)>"), match =>
            {
                textWithoutFormatting = textWithoutFormatting.Replace(match.Groups[0].ToString(), "");
                tagLocations.Add(new Vector2Int(match.Index, match.Length - 1));
            });

            var stringLength2 = (int)Mathf.Lerp(0, textWithoutFormatting.Length, p);
            textObject.textMeshPro.maxVisibleCharacters = stringLength2;

            if (glitch && (int)RTMath.Lerp(0, textWithoutFormatting.Length, p) <= textWithoutFormatting.Length)
            {
                int insert = Mathf.Clamp(stringLength2 - 1, 0, text.Length);
                for (int i = 0; i < tagLocations.Count; i++)
                {
                    var tagLocation = tagLocations[i];
                    if (insert >= tagLocation.x)
                        insert += tagLocation.y + 1;
                }

                text = text.Insert(insert, LSText.randomString(1));
            }

            if (modifier.constant || !CoreConfig.Instance.AllowCustomTextFormatting.Value)
                textObject.SetText(text);
            else
                textObject.text = text;

            if ((modifier.Result is not int result || result != stringLength2) && textWithoutFormatting[Mathf.Clamp(stringLength2 - 1, 0, textWithoutFormatting.Length - 1)] != ' ')
            {
                modifier.Result = stringLength2;
                float pitch = modifier.GetFloat(6, 1f, variables);
                float volume = modifier.GetFloat(7, 1f, variables);
                float pitchVary = modifier.GetFloat(8, 0f, variables);

                if (pitchVary != 0f)
                    pitch += UnityEngine.Random.Range(-pitchVary, pitchVary);

                // Don't play any sounds.
                if (!modifier.GetBool(2, true, variables))
                    return;

                // Don't play custom sound.
                if (!modifier.GetBool(3, false, variables))
                {
                    SoundManager.inst.PlaySound(DefaultSounds.Click, volume, volume);
                    return;
                }

                var soundName = modifier.GetValue(4, variables);
                if (GameData.Current.assets.sounds.TryFind(x => x.name == soundName, out SoundAsset soundAsset) && soundAsset.audio)
                    SoundManager.inst.PlaySound(soundAsset.audio, volume, pitch, panStereo: modifier.GetFloat(12, 0f, variables));
                else if (SoundManager.inst.TryGetSound(soundName, out AudioClip audioClip))
                    SoundManager.inst.PlaySound(audioClip, volume, pitch, panStereo: modifier.GetFloat(12, 0f, variables));
                else
                    ModifiersHelper.GetSoundPath(beatmapObject.id, soundName, modifier.GetBool(5, false, variables), pitch, volume, false, modifier.GetFloat(12, 0f, variables));
            }
        }

        // modify shape
        public static void backgroundShape(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not BeatmapObject beatmapObject)
                return;

            var runtimeObject = beatmapObject.runtimeObject;
            if (modifier.HasResult() || beatmapObject.IsSpecialShape || !runtimeObject || !runtimeObject.visualObject || !runtimeObject.visualObject.gameObject)
                return;

            if (ShapeManager.inst.Shapes3D.TryGetAt(beatmapObject.Shape, out ShapeGroup shapeGroup) && shapeGroup.TryGetShape(beatmapObject.ShapeOption, out Shape shape))
            {
                runtimeObject.visualObject.gameObject.GetComponent<MeshFilter>().mesh = shape.mesh;
                modifier.Result = "frick";
                runtimeObject.visualObject.gameObject.AddComponent<DestroyModifierResult>().Modifier = modifier;
            }
        }

        public static void sphereShape(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not BeatmapObject beatmapObject)
                return;

            var runtimeObject = beatmapObject.runtimeObject;
            if (modifier.HasResult() || beatmapObject.IsSpecialShape || !runtimeObject || !runtimeObject.visualObject || !runtimeObject.visualObject.gameObject)
                return;

            runtimeObject.visualObject.gameObject.GetComponent<MeshFilter>().mesh = GameManager.inst.PlayerPrefabs[1].GetComponentInChildren<MeshFilter>().mesh;
            modifier.Result = "frick";
            runtimeObject.visualObject.gameObject.AddComponent<DestroyModifierResult>().Modifier = modifier;
        }

        #endregion

        #region Animation

        public static void animateObject(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            var transformable = reference.AsTransformable();
            if (transformable == null)
                return;

            var time = modifier.GetFloat(0, 0f, variables);
            var type = modifier.GetInt(1, 0, variables);
            var x = modifier.GetFloat(2, 0f, variables);
            var y = modifier.GetFloat(3, 0f, variables);
            var z = modifier.GetFloat(4, 0f, variables);
            var relative = modifier.GetBool(5, true, variables);

            string easing = modifier.GetValue(6, variables);
            if (int.TryParse(easing, out int e) && e >= 0 && e < DataManager.inst.AnimationList.Count)
                easing = DataManager.inst.AnimationList[e].Name;

            var applyDeltaTime = modifier.GetBool(7, true, variables);

            Vector3 vector = transformable.GetTransformOffset(type);

            var setVector = new Vector3(x, y, z);
            if (relative)
            {
                if (modifier.constant && applyDeltaTime)
                    setVector *= CoreHelper.TimeFrame;

                setVector += vector;
            }

            if (!modifier.constant)
            {
                var animation = new RTAnimation("Animate Object Offset");

                animation.animationHandlers = new List<AnimationHandlerBase>
                {
                    new AnimationHandler<Vector3>(new List<IKeyframe<Vector3>>
                    {
                        new Vector3Keyframe(0f, vector, Ease.Linear),
                        new Vector3Keyframe(Mathf.Clamp(time, 0f, 9999f), setVector, Ease.GetEaseFunction(easing, Ease.Linear)),
                    }, vector3 => transformable.SetTransform(type, vector3), interpolateOnComplete: true),
                };
                animation.SetDefaultOnComplete();
                AnimationManager.inst.Play(animation);
                return;
            }

            transformable.SetTransform(type, setVector);
        }

        public static void animateObjectOther(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not IPrefabable prefabable)
                return;

            var transformables = GameData.Current.FindTransformablesWithTag(modifier, prefabable, modifier.GetValue(7, variables));

            var time = modifier.GetFloat(0, 0f, variables);
            var type = modifier.GetInt(1, 0, variables);
            var x = modifier.GetFloat(2, 0f, variables);
            var y = modifier.GetFloat(3, 0f, variables);
            var z = modifier.GetFloat(4, 0f, variables);
            var relative = modifier.GetBool(5, true, variables);

            string easing = modifier.GetValue(6, variables);
            if (int.TryParse(easing, out int e) && e >= 0 && e < DataManager.inst.AnimationList.Count)
                easing = DataManager.inst.AnimationList[e].Name;

            var applyDeltaTime = modifier.GetBool(8, true, variables);

            foreach (var transformable in transformables)
            {
                Vector3 vector = transformable.GetTransformOffset(type);

                var setVector = new Vector3(x, y, z);
                if (relative)
                {
                    if (modifier.constant && applyDeltaTime)
                        setVector *= CoreHelper.TimeFrame;

                    setVector += vector;
                }

                if (!modifier.constant)
                {
                    var animation = new RTAnimation("Animate Other Object Offset");

                    animation.animationHandlers = new List<AnimationHandlerBase>
                    {
                        new AnimationHandler<Vector3>(new List<IKeyframe<Vector3>>
                        {
                            new Vector3Keyframe(0f, vector, Ease.Linear),
                            new Vector3Keyframe(Mathf.Clamp(time, 0f, 9999f), setVector, Ease.GetEaseFunction(easing, Ease.Linear)),
                        }, vector3 => transformable.SetTransform(type, vector3), interpolateOnComplete: true),
                    };
                    animation.SetDefaultOnComplete();
                    AnimationManager.inst.Play(animation);
                    continue;
                }

                transformable.SetTransform(type, setVector);
            }
        }

        // tests modifier keyframing
        // todo: see if i can get homing to work via adding a keyframe depending on audio time
        public static void animateObjectKF(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not ITransformable transformable || reference is not ILifetime lifetime)
                return;

            var audioTime = modifier.GetFloat(0, 0f, variables);
            var type = modifier.GetInt(1, 0, variables);

            Sequence<Vector3> sequence;

            if (modifier.HasResult())
                sequence = modifier.GetResult<Sequence<Vector3>>();
            else
            {
                // get starting position.
                var vector = transformable.GetTransformOffset(type);

                // a custom start position can be registered if you want.
                var xStart = modifier.GetValue(2, variables);
                var yStart = modifier.GetValue(3, variables);
                var zStart = modifier.GetValue(4, variables);
                if (float.TryParse(xStart, out float xS))
                    vector.x = xS;
                if (float.TryParse(yStart, out float yS))
                    vector.y = yS;
                if (float.TryParse(zStart, out float zS))
                    vector.z = zS;

                var currentTime = 0f;

                var keyframes = new List<IKeyframe<Vector3>>();
                keyframes.Add(new Vector3Keyframe(currentTime, vector, Ease.Linear));
                for (int i = 5; i < modifier.values.Count; i += 6)
                {
                    var time = modifier.GetFloat(i, 0f, variables);
                    if (time < currentTime)
                        continue;

                    var x = modifier.GetFloat(i + 1, 0f, variables);
                    var y = modifier.GetFloat(i + 2, 0f, variables);
                    var z = modifier.GetFloat(i + 3, 0f, variables);
                    var relative = modifier.GetBool(i + 4, true, variables);

                    var easing = modifier.GetValue(i + 5, variables);
                    if (int.TryParse(easing, out int e) && e >= 0 && e < DataManager.inst.AnimationList.Count)
                        easing = DataManager.inst.AnimationList[e].Name;

                    var setVector = new Vector3(x, y, z);
                    if (relative)
                        setVector += vector;

                    keyframes.Add(new Vector3Keyframe(currentTime + time, setVector, Ease.GetEaseFunction(easing, Ease.Linear)));

                    vector = setVector;
                    currentTime = time;
                }

                sequence = new Sequence<Vector3>(keyframes);
            }

            if (sequence != null)
                transformable.SetTransform(type, sequence.GetValue(audioTime - lifetime.StartTime));
        }

        public static void animateSignal(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not IPrefabable prefabable || reference is not ITransformable transformable)
                return;

            var time = modifier.GetFloat(0, 0f, variables);
            var type = modifier.GetInt(1, 0, variables);
            var x = modifier.GetFloat(2, 0f, variables);
            var y = modifier.GetFloat(3, 0f, variables);
            var z = modifier.GetFloat(4, 0f, variables);
            var relative = modifier.GetBool(5, true, variables);
            var signalGroup = modifier.GetValue(7, variables);
            var delay = modifier.GetFloat(8, 0f, variables);

            if (!modifier.GetBool(9, true, variables))
            {
                var list = GameData.Current.FindObjectsWithTag(modifier, prefabable, signalGroup);

                foreach (var bm in list)
                {
                    if (!bm.modifiers.IsEmpty() && !bm.modifiers.FindAll(x => x.Name == "requireSignal" && x.type == Modifier.Type.Trigger).IsEmpty() &&
                        bm.modifiers.TryFind(x => x.Name == "requireSignal" && x.type == Modifier.Type.Trigger, out Modifier m))
                        m.Result = null;
                }
            }

            string easing = modifier.GetValue(6, variables);
            if (int.TryParse(easing, out int e) && e >= 0 && e < DataManager.inst.AnimationList.Count)
                easing = DataManager.inst.AnimationList[e].Name;

            var applyDeltaTime = modifier.GetBool(10, true, variables);

            Vector3 vector = transformable.GetTransformOffset(type);

            var setVector = new Vector3(x, y, z);
            if (relative)
            {
                if (modifier.constant && applyDeltaTime)
                    setVector *= CoreHelper.TimeFrame;

                setVector += vector;
            }

            if (!modifier.constant)
            {
                var animation = new RTAnimation("Animate Object Offset");

                animation.animationHandlers = new List<AnimationHandlerBase>
                {
                    new AnimationHandler<Vector3>(new List<IKeyframe<Vector3>>
                    {
                        new Vector3Keyframe(0f, vector, Ease.Linear),
                        new Vector3Keyframe(Mathf.Clamp(time, 0f, 9999f), setVector, Ease.HasEaseFunction(easing) ? Ease.GetEaseFunction(easing) : Ease.Linear),
                    }, vector3 => transformable.SetTransform(type, vector3), interpolateOnComplete: true),
                };
                animation.onComplete = () =>
                {
                    AnimationManager.inst.Remove(animation.id);

                    var list = GameData.Current.FindObjectsWithTag(modifier, prefabable, signalGroup);

                    foreach (var bm in list)
                        CoroutineHelper.StartCoroutine(ModifiersHelper.ActivateModifier(bm, delay));
                };
                AnimationManager.inst.Play(animation);
                return;
            }

            transformable.SetTransform(type, setVector);
        }

        public static void animateSignalOther(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not IPrefabable prefabable)
                return;

            var transformables = GameData.Current.FindTransformablesWithTag(modifier, prefabable, modifier.GetValue(7, variables));

            var time = modifier.GetFloat(0, 0f, variables);
            var type = modifier.GetInt(1, 0, variables);
            var x = modifier.GetFloat(2, 0f, variables);
            var y = modifier.GetFloat(3, 0f, variables);
            var z = modifier.GetFloat(4, 0f, variables);
            var relative = modifier.GetBool(5, true, variables);
            var signalGroup = modifier.GetValue(8, variables);
            var delay = modifier.GetFloat(9, 0f, variables);

            if (!modifier.GetBool(10, true, variables))
            {
                var list2 = GameData.Current.FindObjectsWithTag(modifier, prefabable, signalGroup);

                foreach (var bm in list2)
                {
                    if (!bm.modifiers.IsEmpty() && !bm.modifiers.FindAll(x => x.Name == "requireSignal" && x.type == Modifier.Type.Trigger).IsEmpty() &&
                        bm.modifiers.TryFind(x => x.Name == "requireSignal" && x.type == Modifier.Type.Trigger, out Modifier m))
                    {
                        m.Result = null;
                    }
                }
            }

            string easing = modifier.GetValue(6, variables);
            if (int.TryParse(easing, out int e) && e >= 0 && e < DataManager.inst.AnimationList.Count)
                easing = DataManager.inst.AnimationList[e].Name;

            var applyDeltaTime = modifier.GetBool(11, true, variables);

            foreach (var transformable in transformables)
            {
                Vector3 vector = transformable.GetTransformOffset(type);

                var setVector = new Vector3(x, y, z);
                if (relative)
                {
                    if (modifier.constant && applyDeltaTime)
                        setVector *= CoreHelper.TimeFrame;

                    setVector += vector;
                }

                if (!modifier.constant)
                {
                    var animation = new RTAnimation("Animate Other Object Offset");

                    animation.animationHandlers = new List<AnimationHandlerBase>
                    {
                        new AnimationHandler<Vector3>(new List<IKeyframe<Vector3>>
                        {
                            new Vector3Keyframe(0f, vector, Ease.Linear),
                            new Vector3Keyframe(Mathf.Clamp(time, 0f, 9999f), setVector, Ease.GetEaseFunction(easing, Ease.Linear)),
                        }, vector3 => transformable.SetTransform(type, vector3), interpolateOnComplete: true),
                    };
                    animation.onComplete = () =>
                    {
                        AnimationManager.inst.Remove(animation.id);

                        var list = GameData.Current.FindObjectsWithTag(modifier, prefabable, signalGroup);

                        foreach (var bm in list)
                            CoroutineHelper.StartCoroutine(ModifiersHelper.ActivateModifier(bm, delay));
                    };
                    AnimationManager.inst.Play(animation);
                    break;
                }

                transformable.SetTransform(type, setVector);
            }
        }

        public static void animateObjectMath(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            var transformable = reference.AsTransformable();
            if (transformable == null)
                return;

            if (reference is not IEvaluatable evaluatable)
                return;

            var numberVariables = evaluatable.GetObjectVariables();
            ModifiersHelper.SetVariables(variables, numberVariables);

            var functions = evaluatable.GetObjectFunctions();

            float time = (float)RTMath.Parse(modifier.GetValue(0, variables), numberVariables, functions);
            var type = modifier.GetInt(1, 0, variables);
            float x = (float)RTMath.Parse(modifier.GetValue(2, variables), numberVariables, functions);
            float y = (float)RTMath.Parse(modifier.GetValue(3, variables), numberVariables, functions);
            float z = (float)RTMath.Parse(modifier.GetValue(4, variables), numberVariables, functions);
            var relative = modifier.GetBool(5, true, variables);

            string easing = modifier.GetValue(6, variables);
            if (int.TryParse(easing, out int e) && e >= 0 && e < DataManager.inst.AnimationList.Count)
                easing = DataManager.inst.AnimationList[e].Name;

            var applyDeltaTime = modifier.GetBool(7, true, variables);

            Vector3 vector = transformable.GetTransformOffset(type);

            var setVector = new Vector3(x, y, z);
            if (relative)
            {
                if (modifier.constant && applyDeltaTime)
                    setVector *= CoreHelper.TimeFrame;

                setVector += vector;
            }

            if (!modifier.constant)
            {
                var animation = new RTAnimation("Animate Object Offset");

                animation.animationHandlers = new List<AnimationHandlerBase>
                {
                    new AnimationHandler<Vector3>(new List<IKeyframe<Vector3>>
                    {
                        new Vector3Keyframe(0f, vector, Ease.Linear),
                        new Vector3Keyframe(Mathf.Clamp(time, 0f, 9999f), setVector, Ease.GetEaseFunction(easing, Ease.Linear)),
                    }, vector3 => transformable.SetTransform(type, vector3), interpolateOnComplete: true),
                };
                animation.SetDefaultOnComplete();
                AnimationManager.inst.Play(animation);
                return;
            }

            transformable.SetTransform(type, setVector);
        }

        public static void animateObjectMathOther(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not IPrefabable prefabable || reference is not IEvaluatable evaluatable)
                return;

            var transformables = modifier.GetResultOrDefault(() => GameData.Current.FindTransformablesWithTag(modifier, prefabable, modifier.GetValue(7, variables)));

            var numberVariables = evaluatable.GetObjectVariables();
            ModifiersHelper.SetVariables(variables, numberVariables);

            var functions = evaluatable.GetObjectFunctions();

            // for optimization sake, we evaluate this outside of the foreach loop. normally I'd place this inside and replace "otherVar" with bm.integerVariable.ToString(), however I feel that would result in a worse experience so the tradeoff is not worth it.
            float time = (float)RTMath.Parse(modifier.GetValue(0, variables), numberVariables, functions);
            var type = modifier.GetInt(1, 0, variables);
            float x = (float)RTMath.Parse(modifier.GetValue(2, variables), numberVariables, functions);
            float y = (float)RTMath.Parse(modifier.GetValue(3, variables), numberVariables, functions);
            float z = (float)RTMath.Parse(modifier.GetValue(4, variables), numberVariables, functions);
            var relative = modifier.GetBool(5, true, variables);

            string easing = modifier.GetValue(6, variables);
            if (int.TryParse(easing, out int e) && e >= 0 && e < DataManager.inst.AnimationList.Count)
                easing = DataManager.inst.AnimationList[e].Name;

            var applyDeltaTime = modifier.GetBool(8, true, variables);

            foreach (var transformable in transformables)
            {
                Vector3 vector = transformable.GetTransformOffset(type);

                var setVector = new Vector3(x, y, z);
                if (relative)
                {
                    if (modifier.constant && applyDeltaTime)
                        setVector *= CoreHelper.TimeFrame;

                    setVector += vector;
                }

                if (!modifier.constant)
                {
                    var animation = new RTAnimation("Animate Other Object Offset");

                    animation.animationHandlers = new List<AnimationHandlerBase>
                    {
                        new AnimationHandler<Vector3>(new List<IKeyframe<Vector3>>
                        {
                            new Vector3Keyframe(0f, vector, Ease.Linear),
                            new Vector3Keyframe(Mathf.Clamp(time, 0f, 9999f), setVector, Ease.GetEaseFunction(easing, Ease.Linear)),
                        }, vector3 => transformable.SetTransform(type, vector3), interpolateOnComplete: true),
                    };
                    animation.SetDefaultOnComplete();
                    AnimationManager.inst.Play(animation);
                    continue;
                }

                transformable.SetTransform(type, setVector);
            }
        }

        public static void animateSignalMath(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not IEvaluatable evaluatable || reference is not IPrefabable prefabable || reference is not ITransformable transformable)
                return;

            var numberVariables = evaluatable.GetObjectVariables();
            ModifiersHelper.SetVariables(variables, numberVariables);

            var functions = evaluatable.GetObjectFunctions();

            float time = (float)RTMath.Parse(modifier.GetValue(0, variables), numberVariables, functions);
            var type = modifier.GetInt(1, 0, variables);
            float x = (float)RTMath.Parse(modifier.GetValue(2, variables), numberVariables, functions);
            float y = (float)RTMath.Parse(modifier.GetValue(3, variables), numberVariables, functions);
            float z = (float)RTMath.Parse(modifier.GetValue(4, variables), numberVariables, functions);
            var relative = modifier.GetBool(5, true, variables);
            var signalGroup = modifier.GetValue(7, variables);
            float signalTime = (float)RTMath.Parse(modifier.GetValue(8, variables), numberVariables, functions);

            if (!modifier.GetBool(9, true, variables))
            {
                var list = GameData.Current.FindObjectsWithTag(modifier, prefabable, signalGroup);

                foreach (var bm in list)
                {
                    if (!bm.modifiers.IsEmpty() && !bm.modifiers.FindAll(x => x.Name == "requireSignal" && x.type == Modifier.Type.Trigger).IsEmpty() &&
                        bm.modifiers.TryFind(x => x.Name == "requireSignal" && x.type == Modifier.Type.Trigger, out Modifier m))
                        m.Result = null;
                }
            }

            string easing = modifier.GetValue(6, variables);
            if (int.TryParse(easing, out int e) && e >= 0 && e < DataManager.inst.AnimationList.Count)
                easing = DataManager.inst.AnimationList[e].Name;

            var applyDeltaTime = modifier.GetBool(10, true, variables);

            Vector3 vector = transformable.GetTransformOffset(type);

            var setVector = new Vector3(x, y, z);
            if (relative)
            {
                if (modifier.constant && applyDeltaTime)
                    setVector *= CoreHelper.TimeFrame;

                setVector += vector;
            }

            if (!modifier.constant)
            {
                var animation = new RTAnimation("Animate Object Offset");

                animation.animationHandlers = new List<AnimationHandlerBase>
                {
                    new AnimationHandler<Vector3>(new List<IKeyframe<Vector3>>
                    {
                        new Vector3Keyframe(0f, vector, Ease.Linear),
                        new Vector3Keyframe(Mathf.Clamp(time, 0f, 9999f), setVector, Ease.GetEaseFunction(easing, Ease.Linear)),
                    }, vector3 => transformable.SetTransform(type, vector3), interpolateOnComplete: true),
                };
                animation.onComplete = () =>
                {
                    AnimationManager.inst.Remove(animation.id);

                    var list = GameData.Current.FindObjectsWithTag(modifier, prefabable, signalGroup);

                    foreach (var bm in list)
                        CoroutineHelper.StartCoroutine(ModifiersHelper.ActivateModifier(bm, signalTime));
                };
                AnimationManager.inst.Play(animation);
                return;
            }

            transformable.SetTransform(type, setVector);
        }

        public static void animateSignalMathOther(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not IPrefabable prefabable || reference is not IEvaluatable evaluatable)
                return;

            var transformables = GameData.Current.FindTransformablesWithTag(modifier, prefabable, modifier.GetValue(7, variables));

            var numberVariables = evaluatable.GetObjectVariables();
            ModifiersHelper.SetVariables(variables, numberVariables);

            var functions = evaluatable.GetObjectFunctions();

            var time = (float)RTMath.Parse(modifier.GetValue(0, variables), numberVariables, functions);
            var type = modifier.GetInt(1, 0, variables);
            var x = (float)RTMath.Parse(modifier.GetValue(2, variables), numberVariables, functions);
            var y = (float)RTMath.Parse(modifier.GetValue(3, variables), numberVariables, functions);
            var z = (float)RTMath.Parse(modifier.GetValue(4, variables), numberVariables, functions);
            var relative = modifier.GetBool(5, true, variables);
            var signalGroup = modifier.GetValue(8, variables);
            var signalTime = (float)RTMath.Parse(modifier.GetValue(9, variables), numberVariables);

            if (!modifier.GetBool(10, true, variables))
            {
                var list2 = GameData.Current.FindObjectsWithTag(modifier, prefabable, signalGroup);

                foreach (var bm in list2)
                {
                    if (!bm.modifiers.IsEmpty() && bm.modifiers.FindAll(x => x.Name == "requireSignal" && x.type == Modifier.Type.Trigger).Count > 0 &&
                        bm.modifiers.TryFind(x => x.Name == "requireSignal" && x.type == Modifier.Type.Trigger, out Modifier m))
                        m.Result = null;
                }
            }

            string easing = modifier.GetValue(6, variables);
            if (int.TryParse(easing, out int e) && e >= 0 && e < DataManager.inst.AnimationList.Count)
                easing = DataManager.inst.AnimationList[e].Name;

            var applyDeltaTime = modifier.GetBool(11, true, variables);

            foreach (var transformable in transformables)
            {
                Vector3 vector = transformable.GetTransformOffset(type);

                var setVector = new Vector3(x, y, z);
                if (relative)
                {
                    if (modifier.constant && applyDeltaTime)
                        setVector *= CoreHelper.TimeFrame;

                    setVector += vector;
                }

                if (!modifier.constant)
                {
                    var animation = new RTAnimation("Animate Other Object Offset");

                    animation.animationHandlers = new List<AnimationHandlerBase>
                    {
                        new AnimationHandler<Vector3>(new List<IKeyframe<Vector3>>
                        {
                            new Vector3Keyframe(0f, vector, Ease.Linear),
                            new Vector3Keyframe(Mathf.Clamp(time, 0f, 9999f), setVector, Ease.GetEaseFunction(easing, Ease.Linear)),
                        }, vector3 => transformable.SetTransform(type, vector3), interpolateOnComplete: true),
                    };
                    animation.onComplete = () =>
                    {
                        AnimationManager.inst.Remove(animation.id);

                        var list = GameData.Current.FindObjectsWithTag(modifier, prefabable, signalGroup);

                        foreach (var bm in list)
                            CoroutineHelper.StartCoroutine(ModifiersHelper.ActivateModifier(bm, signalTime));
                    };
                    AnimationManager.inst.Play(animation);
                    continue;
                }

                transformable.SetTransform(type, setVector);
            }
        }

        public static void applyAnimation(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not BeatmapObject beatmapObject)
                return;

            var cache = modifier.GetResultOrDefault(() =>
            {
                var applyAnimationCache = new ApplyAnimationCache();
                applyAnimationCache.from = GameData.Current.FindObjectWithTag(modifier, beatmapObject, modifier.GetValue(0, variables));
                applyAnimationCache.to = GameData.Current.FindObjectsWithTag(modifier, beatmapObject, modifier.GetValue(10, variables));
                applyAnimationCache.startTime = reference.GetParentRuntime()?.CurrentTime ?? 0f;
                return applyAnimationCache;
            });

            if (!cache.from)
                return;

            var from = cache.from;
            var list = cache.to;
            var time = cache.startTime;

            var animatePos = modifier.GetBool(1, true, variables);
            var animateSca = modifier.GetBool(2, true, variables);
            var animateRot = modifier.GetBool(3, true, variables);
            var delayPos = modifier.GetFloat(4, 0f, variables);
            var delaySca = modifier.GetFloat(5, 0f, variables);
            var delayRot = modifier.GetFloat(6, 0f, variables);
            var useVisual = modifier.GetBool(7, false, variables);
            var length = modifier.GetFloat(8, 1f, variables);
            var speed = modifier.GetFloat(9, 1f, variables);

            if (!modifier.constant)
                AnimationManager.inst.RemoveName("Apply Object Animation " + beatmapObject.id);

            for (int i = 0; i < list.Count; i++)
            {
                var bm = list[i];

                if (!modifier.constant)
                {
                    var animation = new RTAnimation("Apply Object Animation " + beatmapObject.id);
                    animation.animationHandlers = new List<AnimationHandlerBase>
                    {
                        new AnimationHandler<float>(new List<IKeyframe<float>>
                        {
                            new FloatKeyframe(0f, 0f, Ease.Linear),
                            new FloatKeyframe(Mathf.Clamp(length / speed, 0f, 100f), length, Ease.Linear),
                        }, x => ModifiersHelper.ApplyAnimationTo(bm, from, useVisual, 0f, x, animatePos, animateSca, animateRot, delayPos, delaySca, delayRot), interpolateOnComplete: true)
                    };
                    animation.onComplete = () =>
                    {
                        AnimationManager.inst.Remove(animation.id);
                        animation = null;
                        modifier.Result = null;
                    };
                    AnimationManager.inst.Play(animation);
                    continue;
                }

                ModifiersHelper.ApplyAnimationTo(bm, from, useVisual, time, reference.GetParentRuntime().CurrentTime, animatePos, animateSca, animateRot, delayPos, delaySca, delayRot);
            }
        }

        public static void applyAnimationFrom(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not BeatmapObject beatmapObject)
                return;

            var cache = modifier.GetResultOrDefault(() =>
            {
                var applyAnimationCache = new ApplyAnimationCache();
                applyAnimationCache.from = GameData.Current.FindObjectWithTag(modifier, beatmapObject, modifier.GetValue(0, variables));
                applyAnimationCache.startTime = reference.GetParentRuntime()?.CurrentTime ?? 0f;
                return applyAnimationCache;
            });

            if (!cache.from)
                return;

            var from = cache.from;
            var time = cache.startTime;

            var animatePos = modifier.GetBool(1, true, variables);
            var animateSca = modifier.GetBool(2, true, variables);
            var animateRot = modifier.GetBool(3, true, variables);
            var delayPos = modifier.GetFloat(4, 0f, variables);
            var delaySca = modifier.GetFloat(5, 0f, variables);
            var delayRot = modifier.GetFloat(6, 0f, variables);
            var useVisual = modifier.GetBool(7, false, variables);
            var length = modifier.GetFloat(8, 1f, variables);
            var speed = modifier.GetFloat(9, 1f, variables);

            if (!modifier.constant)
            {
                AnimationManager.inst.RemoveName("Apply Object Animation " + beatmapObject.id);

                var animation = new RTAnimation("Apply Object Animation " + beatmapObject.id);
                animation.animationHandlers = new List<AnimationHandlerBase>
                    {
                        new AnimationHandler<float>(new List<IKeyframe<float>>
                        {
                            new FloatKeyframe(0f, 0f, Ease.Linear),
                            new FloatKeyframe(Mathf.Clamp(length / speed, 0f, 100f), length, Ease.Linear),
                        }, x => ModifiersHelper.ApplyAnimationTo(beatmapObject, from, useVisual, 0f, x, animatePos, animateSca, animateRot, delayPos, delaySca, delayRot), interpolateOnComplete: true)
                    };
                animation.onComplete = () =>
                {
                    AnimationManager.inst.Remove(animation.id);
                    animation = null;
                    modifier.Result = null;
                };
                AnimationManager.inst.Play(animation);
                return;
            }

            ModifiersHelper.ApplyAnimationTo(beatmapObject, from, useVisual, time, reference.GetParentRuntime().CurrentTime, animatePos, animateSca, animateRot, delayPos, delaySca, delayRot);
        }

        public static void applyAnimationTo(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not BeatmapObject beatmapObject)
                return;

            var cache = modifier.GetResultOrDefault(() =>
            {
                var applyAnimationCache = new ApplyAnimationCache();
                applyAnimationCache.to = GameData.Current.FindObjectsWithTag(modifier, beatmapObject, modifier.GetValue(0, variables));
                applyAnimationCache.startTime = reference.GetParentRuntime()?.CurrentTime ?? 0f;
                return applyAnimationCache;
            });

            var list = cache.to;
            var time = cache.startTime;

            var animatePos = modifier.GetBool(1, true, variables);
            var animateSca = modifier.GetBool(2, true, variables);
            var animateRot = modifier.GetBool(3, true, variables);
            var delayPos = modifier.GetFloat(4, 0f, variables);
            var delaySca = modifier.GetFloat(5, 0f, variables);
            var delayRot = modifier.GetFloat(6, 0f, variables);
            var useVisual = modifier.GetBool(7, false, variables);
            var length = modifier.GetFloat(8, 1f, variables);
            var speed = modifier.GetFloat(9, 1f, variables);

            if (!modifier.constant)
                AnimationManager.inst.RemoveName("Apply Object Animation " + beatmapObject.id);

            for (int i = 0; i < list.Count; i++)
            {
                var bm = list[i];

                if (!modifier.constant)
                {
                    var animation = new RTAnimation("Apply Object Animation " + beatmapObject.id);
                    animation.animationHandlers = new List<AnimationHandlerBase>
                        {
                            new AnimationHandler<float>(new List<IKeyframe<float>>
                            {
                                new FloatKeyframe(0f, 0f, Ease.Linear),
                                new FloatKeyframe(Mathf.Clamp(length / speed, 0f, 100f), length, Ease.Linear),
                            }, x => ModifiersHelper.ApplyAnimationTo(bm, beatmapObject, useVisual, 0f, x, animatePos, animateSca, animateRot, delayPos, delaySca, delayRot), interpolateOnComplete: true)
                        };
                    animation.onComplete = () =>
                    {
                        AnimationManager.inst.Remove(animation.id);
                        animation = null;
                        modifier.Result = null;
                    };
                    AnimationManager.inst.Play(animation);
                    continue;
                }

                ModifiersHelper.ApplyAnimationTo(bm, beatmapObject, useVisual, time, reference.GetParentRuntime().CurrentTime, animatePos, animateSca, animateRot, delayPos, delaySca, delayRot);
            }
        }

        public static void applyAnimationMath(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not BeatmapObject beatmapObject)
                return;

            var cache = modifier.GetResultOrDefault(() =>
            {
                var applyAnimationCache = new ApplyAnimationCache();
                applyAnimationCache.from = GameData.Current.FindObjectWithTag(modifier, beatmapObject, modifier.GetValue(0, variables));
                applyAnimationCache.to = GameData.Current.FindObjectsWithTag(modifier, beatmapObject, modifier.GetValue(10, variables));
                applyAnimationCache.startTime = reference.GetParentRuntime()?.CurrentTime ?? 0f;
                return applyAnimationCache;
            });

            if (!cache.from)
                return;

            var from = cache.from;
            var list = cache.to;
            var time = cache.startTime;

            var numberVariables = beatmapObject.GetObjectVariables();
            ModifiersHelper.SetVariables(variables, numberVariables);
            var functions = beatmapObject.GetObjectFunctions();

            var animatePos = modifier.GetBool(1, true, variables);
            var animateSca = modifier.GetBool(2, true, variables);
            var animateRot = modifier.GetBool(3, true, variables);
            var delayPos = RTMath.Parse(modifier.GetValue(4, variables), numberVariables, functions);
            var delaySca = RTMath.Parse(modifier.GetValue(5, variables), numberVariables, functions);
            var delayRot = RTMath.Parse(modifier.GetValue(6, variables), numberVariables, functions);
            var useVisual = modifier.GetBool(7, false, variables);
            var length = RTMath.Parse(modifier.GetValue(8, variables), numberVariables, functions);
            var speed = RTMath.Parse(modifier.GetValue(9, variables), numberVariables, functions);
            var timeOffset = RTMath.Parse(modifier.GetValue(11, variables), numberVariables, functions);

            if (!modifier.constant)
                AnimationManager.inst.RemoveName("Apply Object Animation " + beatmapObject.id);

            for (int i = 0; i < list.Count; i++)
            {
                var bm = list[i];

                if (!modifier.constant)
                {
                    var animation = new RTAnimation("Apply Object Animation " + beatmapObject.id);
                    animation.animationHandlers = new List<AnimationHandlerBase>
                        {
                            new AnimationHandler<float>(new List<IKeyframe<float>>
                            {
                                new FloatKeyframe(0f, 0f, Ease.Linear),
                                new FloatKeyframe(Mathf.Clamp(length / speed, 0f, 100f), length, Ease.Linear),
                            }, x => ModifiersHelper.ApplyAnimationTo(bm, from, useVisual, 0f, x, animatePos, animateSca, animateRot, delayPos, delaySca, delayRot), interpolateOnComplete: true)
                        };
                    animation.onComplete = () =>
                    {
                        AnimationManager.inst.Remove(animation.id);
                        animation = null;
                        modifier.Result = null;
                    };
                    AnimationManager.inst.Play(animation);
                    return;
                }

                ModifiersHelper.ApplyAnimationTo(bm, from, useVisual, time, timeOffset, animatePos, animateSca, animateRot, delayPos, delaySca, delayRot);
            }
        }

        public static void applyAnimationFromMath(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not BeatmapObject beatmapObject)
                return;

            var cache = modifier.GetResultOrDefault(() =>
            {
                var applyAnimationCache = new ApplyAnimationCache();
                applyAnimationCache.from = GameData.Current.FindObjectWithTag(modifier, beatmapObject, modifier.GetValue(0, variables));
                applyAnimationCache.startTime = reference.GetParentRuntime()?.CurrentTime ?? 0f;
                return applyAnimationCache;
            });

            if (!cache.from)
                return;

            var from = cache.from;
            var time = cache.startTime;

            var numberVariables = beatmapObject.GetObjectVariables();
            ModifiersHelper.SetVariables(variables, numberVariables);
            var functions = beatmapObject.GetObjectFunctions();

            var animatePos = modifier.GetBool(1, true, variables);
            var animateSca = modifier.GetBool(2, true, variables);
            var animateRot = modifier.GetBool(3, true, variables);
            var delayPos = RTMath.Parse(modifier.GetValue(4, variables), numberVariables, functions);
            var delaySca = RTMath.Parse(modifier.GetValue(5, variables), numberVariables, functions);
            var delayRot = RTMath.Parse(modifier.GetValue(6, variables), numberVariables, functions);
            var useVisual = modifier.GetBool(7, false, variables);
            var length = RTMath.Parse(modifier.GetValue(8, variables), numberVariables, functions);
            var speed = RTMath.Parse(modifier.GetValue(9, variables), numberVariables, functions);
            var timeOffset = RTMath.Parse(modifier.GetValue(10, variables), numberVariables, functions);

            if (!modifier.constant)
            {
                AnimationManager.inst.RemoveName("Apply Object Animation " + beatmapObject.id);

                var animation = new RTAnimation("Apply Object Animation " + beatmapObject.id);
                animation.animationHandlers = new List<AnimationHandlerBase>
                    {
                        new AnimationHandler<float>(new List<IKeyframe<float>>
                        {
                            new FloatKeyframe(0f, 0f, Ease.Linear),
                            new FloatKeyframe(Mathf.Clamp(length / speed, 0f, 100f), length, Ease.Linear),
                        }, x => ModifiersHelper.ApplyAnimationTo(beatmapObject, from, useVisual, 0f, x, animatePos, animateSca, animateRot, delayPos, delaySca, delayRot), interpolateOnComplete: true)
                    };
                animation.onComplete = () =>
                {
                    AnimationManager.inst.Remove(animation.id);
                    animation = null;
                    modifier.Result = null;
                };
                AnimationManager.inst.Play(animation);
                return;
            }

            ModifiersHelper.ApplyAnimationTo(beatmapObject, from, useVisual, time, timeOffset, animatePos, animateSca, animateRot, delayPos, delaySca, delayRot);
        }

        public static void applyAnimationToMath(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not BeatmapObject beatmapObject)
                return;

            var cache = modifier.GetResultOrDefault(() =>
            {
                var applyAnimationCache = new ApplyAnimationCache();
                applyAnimationCache.to = GameData.Current.FindObjectsWithTag(modifier, beatmapObject, modifier.GetValue(0, variables));
                applyAnimationCache.startTime = reference.GetParentRuntime()?.CurrentTime ?? 0f;
                return applyAnimationCache;
            });

            var list = cache.to;
            var time = cache.startTime;

            var numberVariables = beatmapObject.GetObjectVariables();
            ModifiersHelper.SetVariables(variables, numberVariables);
            var functions = beatmapObject.GetObjectFunctions();

            var animatePos = modifier.GetBool(1, true, variables);
            var animateSca = modifier.GetBool(2, true, variables);
            var animateRot = modifier.GetBool(3, true, variables);
            var delayPos = RTMath.Parse(modifier.GetValue(4, variables), numberVariables, functions);
            var delaySca = RTMath.Parse(modifier.GetValue(5, variables), numberVariables, functions);
            var delayRot = RTMath.Parse(modifier.GetValue(6, variables), numberVariables, functions);
            var useVisual = modifier.GetBool(7, false, variables);
            var length = RTMath.Parse(modifier.GetValue(8, variables), numberVariables, functions);
            var speed = RTMath.Parse(modifier.GetValue(9, variables), numberVariables, functions);
            var timeOffset = RTMath.Parse(modifier.GetValue(10, variables), numberVariables, functions);

            if (!modifier.constant)
                AnimationManager.inst.RemoveName("Apply Object Animation " + beatmapObject.id);

            for (int i = 0; i < list.Count; i++)
            {
                var bm = list[i];

                if (!modifier.constant)
                {
                    var animation = new RTAnimation("Apply Object Animation " + beatmapObject.id);
                    animation.animationHandlers = new List<AnimationHandlerBase>
                        {
                            new AnimationHandler<float>(new List<IKeyframe<float>>
                            {
                                new FloatKeyframe(0f, 0f, Ease.Linear),
                                new FloatKeyframe(Mathf.Clamp(length / speed, 0f, 100f), length, Ease.Linear),
                            }, x => ModifiersHelper.ApplyAnimationTo(bm, beatmapObject, useVisual, 0f, x, animatePos, animateSca, animateRot, delayPos, delaySca, delayRot), interpolateOnComplete: true)
                        };
                    animation.onComplete = () =>
                    {
                        AnimationManager.inst.Remove(animation.id);
                        animation = null;
                        modifier.Result = null;
                    };
                    AnimationManager.inst.Play(animation);
                    continue;
                }

                ModifiersHelper.ApplyAnimationTo(bm, beatmapObject, useVisual, time, timeOffset, animatePos, animateSca, animateRot, delayPos, delaySca, delayRot);
            }
        }

        public static void copyAxis(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            var transformable = reference.AsTransformable();
            if (transformable == null)
                return;
            var prefabable = reference.AsPrefabable();
            if (prefabable == null)
                return;

            var tag = modifier.GetValue(0, variables);

            var fromType = modifier.GetInt(1, 0, variables);
            var fromAxis = modifier.GetInt(2, 0, variables);
            var toType = modifier.GetInt(3, 0, variables);
            var toAxis = modifier.GetInt(4, 0, variables);
            var delay = modifier.GetFloat(5, 0f, variables);
            var multiply = modifier.GetFloat(6, 0f, variables);
            var offset = modifier.GetFloat(7, 0f, variables);
            var min = modifier.GetFloat(8, -9999f, variables);
            var max = modifier.GetFloat(9, 9999f, variables);
            var loop = modifier.GetFloat(10, 9999f, variables);
            var useVisual = modifier.GetBool(11, false, variables);

            var cache = modifier.GetResultOrDefault(() => GroupBeatmapObjectCache.Get(modifier, prefabable, tag));
            if (cache.tag != tag)
            {
                cache.UpdateCache(modifier, prefabable, tag);
                modifier.Result = cache;
            }

            var bm = cache.obj;
            if (!bm)
                return;

            var time = ModifiersHelper.GetTime(bm);

            fromType = Mathf.Clamp(fromType, 0, bm.events.Count);
            if (!useVisual)
                fromAxis = Mathf.Clamp(fromAxis, 0, bm.events[fromType][0].values.Length);

            if (toType < 0 || toType > 3)
                return;

            if (!useVisual && bm.cachedSequences)
            {
                var t = time - bm.StartTime - delay;
                if (fromType == 3)
                {
                    if (toType == 3 && toAxis == 0 && bm.cachedSequences.ColorSequence != null && reference is BeatmapObject beatmapObject && beatmapObject.runtimeObject && beatmapObject.runtimeObject.visualObject)
                        RTLevel.Current.postTick.Enqueue(() =>
                        {
                            var sequence = bm.cachedSequences.ColorSequence.GetValue(t);
                            var visualObject = beatmapObject.runtimeObject.visualObject;
                            visualObject.SetColor(RTMath.Lerp(visualObject.GetPrimaryColor(), sequence, multiply));
                        });
                    return;
                }
                transformable.SetTransform(toType, toAxis, fromType switch
                {
                    0 => Mathf.Clamp((bm.cachedSequences.PositionSequence.GetValue(t).At(fromAxis) - offset) * multiply % loop, min, max),
                    1 => Mathf.Clamp((bm.cachedSequences.ScaleSequence.GetValue(t).At(fromAxis) - offset) * multiply % loop, min, max),
                    2 => Mathf.Clamp((bm.cachedSequences.RotationSequence.GetValue(t) - offset) * multiply % loop, min, max),
                    _ => 0f,
                });
            }
            else if (useVisual && bm.runtimeObject is RTBeatmapObject runtimeObject && runtimeObject.visualObject && runtimeObject.visualObject.gameObject)
                transformable.SetTransform(toType, toAxis, Mathf.Clamp((runtimeObject.visualObject.gameObject.transform.GetVector(fromType).At(fromAxis) - offset) * multiply % loop, min, max));
            else if (useVisual)
                transformable.SetTransform(toType, toAxis, Mathf.Clamp(fromType switch
                {
                    0 => bm.InterpolateChainPosition().At(fromAxis),
                    1 => bm.InterpolateChainScale().At(fromAxis),
                    2 => bm.InterpolateChainRotation(),
                    _ => 0f,
                }, min, max));
        }

        public static void copyAxisMath(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            var transformable = reference.AsTransformable();
            if (transformable == null)
                return;
            var prefabable = reference.AsPrefabable();
            if (prefabable == null)
                return;

            if (reference is not IEvaluatable evaluatable)
                return;

            try
            {
                var tag = modifier.GetValue(0, variables);

                var fromType = modifier.GetInt(1, 0, variables);
                var fromAxis = modifier.GetInt(2, 0, variables);
                var toType = modifier.GetInt(3, 0, variables);
                var toAxis = modifier.GetInt(4, 0, variables);
                var delay = modifier.GetFloat(5, 0f, variables);
                var min = modifier.GetFloat(6, -9999f, variables);
                var max = modifier.GetFloat(7, 9999f, variables);
                var evaluation = modifier.GetValue(8, variables);
                var useVisual = modifier.GetBool(9, false, variables);

                var cache = modifier.GetResultOrDefault(() => GroupBeatmapObjectCache.Get(modifier, prefabable, tag));
                if (cache.tag != tag)
                {
                    cache.UpdateCache(modifier, prefabable, tag);
                    modifier.Result = cache;
                }

                var bm = cache.obj;
                if (!bm)
                    return;

                var time = ModifiersHelper.GetTime(bm);

                fromType = Mathf.Clamp(fromType, 0, bm.events.Count);
                if (!useVisual)
                    fromAxis = Mathf.Clamp(fromAxis, 0, bm.events[fromType][0].values.Length);

                if (toType < 0 || toType > 3)
                    return;

                if (!useVisual && bm.cachedSequences)
                {
                    if (fromType == 3)
                    {
                        if (toType == 3 && toAxis == 0 && bm.cachedSequences.ColorSequence != null &&
                            reference is BeatmapObject beatmapObject && beatmapObject.runtimeObject && beatmapObject.runtimeObject.visualObject &&
                            beatmapObject.runtimeObject.visualObject.renderer)
                        {
                            // queue post tick so the color overrides the sequence color
                            RTLevel.Current.postTick.Enqueue(() =>
                            {
                                var sequence = bm.cachedSequences.ColorSequence.GetValue(time - bm.StartTime - delay);

                                var renderer = beatmapObject.runtimeObject.visualObject.renderer;

                                var numberVariables = beatmapObject.GetObjectVariables();
                                ModifiersHelper.SetVariables(variables, numberVariables);

                                numberVariables["colorR"] = sequence.r;
                                numberVariables["colorG"] = sequence.g;
                                numberVariables["colorB"] = sequence.b;
                                numberVariables["colorA"] = sequence.a;
                                bm.SetOtherObjectVariables(numberVariables);

                                float value = RTMath.Parse(evaluation, numberVariables);

                                renderer.material.color = RTMath.Lerp(renderer.material.color, sequence, Mathf.Clamp(value, min, max));
                            });
                        }
                    }
                    else
                    {
                        var numberVariables = evaluatable.GetObjectVariables();
                        ModifiersHelper.SetVariables(variables, numberVariables);

                        if (bm.cachedSequences)
                            numberVariables["axis"] = fromType switch
                            {
                                0 => bm.cachedSequences.PositionSequence.GetValue(time - bm.StartTime - delay).At(fromAxis),
                                1 => bm.cachedSequences.ScaleSequence.GetValue(time - bm.StartTime - delay).At(fromAxis),
                                2 => bm.cachedSequences.RotationSequence.GetValue(time - bm.StartTime - delay),
                                _ => 0f,
                            };
                        bm.SetOtherObjectVariables(numberVariables);

                        float value = RTMath.Parse(evaluation, numberVariables);

                        transformable.SetTransform(toType, toAxis, Mathf.Clamp(value, min, max));
                    }
                }
                else if (useVisual && bm.runtimeObject is RTBeatmapObject runtimeObject && runtimeObject.visualObject && runtimeObject.visualObject.gameObject)
                {
                    var axis = runtimeObject.visualObject.gameObject.transform.GetVector(fromType).At(fromAxis);

                    var numberVariables = evaluatable.GetObjectVariables();
                    ModifiersHelper.SetVariables(variables, numberVariables);

                    numberVariables["axis"] = axis;
                    bm.SetOtherObjectVariables(numberVariables);

                    float value = RTMath.Parse(evaluation, numberVariables);

                    transformable.SetTransform(toType, toAxis, Mathf.Clamp(value, min, max));
                }
                else if (useVisual)
                {
                    var numberVariables = evaluatable.GetObjectVariables();
                    ModifiersHelper.SetVariables(variables, numberVariables);

                    numberVariables["axis"] = fromType switch
                    {
                        0 => bm.InterpolateChainPosition().At(fromAxis),
                        1 => bm.InterpolateChainScale().At(fromAxis),
                        2 => bm.InterpolateChainRotation(),
                        _ => 0f,
                    };
                    bm.SetOtherObjectVariables(numberVariables);

                    float value = RTMath.Parse(evaluation, numberVariables);

                    transformable.SetTransform(toType, toAxis, Mathf.Clamp(value, min, max));
                }
            }
            catch
            {

            } // try catch for cases where the math is broken
        }

        public static void copyAxisGroup(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            var transformable = reference.AsTransformable();
            if (transformable == null)
                return;
            var prefabable = reference.AsPrefabable();
            if (prefabable == null)
                return;

            if (reference is not IEvaluatable evaluatable)
                return;

            var evaluation = modifier.GetValue(0, variables);

            var toType = modifier.GetInt(1, 0, variables);
            var toAxis = modifier.GetInt(2, 0, variables);

            if (toType < 0 || toType > 4)
                return;

            try
            {
                var beatmapObjects = GameData.Current.beatmapObjects;
                var prefabObjects = GameData.Current.prefabObjects;

                var time = reference.GetParentRuntime().CurrentTime;
                var numberVariables = evaluatable.GetObjectVariables();
                ModifiersHelper.SetVariables(variables, numberVariables);

                var list = modifier.GetResultOrDefault(() =>
                {
                    var result = new List<BeatmapObject>();

                    for (int i = 3; i < modifier.values.Count; i += 8)
                    {
                        var group = modifier.GetValue(i + 1);

                        if (GameData.Current.TryFindObjectWithTag(modifier, prefabable, group, out BeatmapObject beatmapObject))
                            result.Add(beatmapObject);
                    }

                    return result;
                });

                int groupIndex = 0;
                for (int i = 3; i < modifier.values.Count; i += 8)
                {
                    var name = modifier.GetValue(i, variables);
                    var group = modifier.GetValue(i + 1, variables);
                    var fromType = modifier.GetInt(i + 2, 0, variables);
                    var fromAxis = modifier.GetInt(i + 3, 0, variables);
                    var delay = modifier.GetFloat(i + 4, 0f, variables);
                    var min = modifier.GetFloat(i + 5, 0f, variables);
                    var max = modifier.GetFloat(i + 6, 0f, variables);
                    var useVisual = modifier.GetBool(i + 7, false, variables);

                    var beatmapObject = list.GetAtOrDefault(groupIndex, null);

                    if (!beatmapObject)
                    {
                        groupIndex++;
                        continue;
                    }

                    if (!useVisual && beatmapObject.cachedSequences)
                        numberVariables[name] = fromType switch
                        {
                            0 => Mathf.Clamp(beatmapObject.cachedSequences.PositionSequence.GetValue(time - beatmapObject.StartTime - delay).At(fromAxis), min, max),
                            1 => Mathf.Clamp(beatmapObject.cachedSequences.ScaleSequence.GetValue(time - beatmapObject.StartTime - delay).At(fromAxis), min, max),
                            2 => Mathf.Clamp(beatmapObject.cachedSequences.RotationSequence.GetValue(time - beatmapObject.StartTime - delay), min, max),
                            _ => 0f,
                        };
                    else if (useVisual && beatmapObject.runtimeObject is RTBeatmapObject levelObject && levelObject.visualObject && levelObject.visualObject.gameObject)
                        numberVariables[name] = Mathf.Clamp(levelObject.visualObject.gameObject.transform.GetVector(fromType).At(fromAxis), min, max);
                    else if (useVisual)
                        numberVariables[name] = fromType switch
                        {
                            0 => Mathf.Clamp(beatmapObject.InterpolateChainPosition().At(fromAxis), min, max),
                            1 => Mathf.Clamp(beatmapObject.InterpolateChainScale().At(fromAxis), min, max),
                            2 => Mathf.Clamp(beatmapObject.InterpolateChainRotation(), min, max),
                            _ => 0f,
                        };

                    if (fromType == 4)
                        numberVariables[name] = Mathf.Clamp(beatmapObject.integerVariable, min, max);

                    groupIndex++;
                }

                transformable.SetTransform(toType, toAxis, RTMath.Parse(evaluation, numberVariables));
            }
            catch (Exception ex)
            {
                CoreHelper.LogException(ex);
            }
        }

        public static void copyPlayerAxis(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            var transformable = reference.AsTransformable();
            if (transformable == null)
                return;

            var fromType = modifier.GetInt(1, 0, variables);
            var fromAxis = modifier.GetInt(2, 0, variables);

            var toType = modifier.GetInt(3, 0, variables);
            var toAxis = modifier.GetInt(4, 0, variables);

            var delay = modifier.GetFloat(5, 0f, variables);
            var multiply = modifier.GetFloat(6, 0f, variables);
            var offset = modifier.GetFloat(7, 0f, variables);
            var min = modifier.GetFloat(8, -9999f, variables);
            var max = modifier.GetFloat(9, 9999f, variables);

            var players = PlayerManager.Players;

            if (players.TryFind(x => x.RuntimePlayer && x.RuntimePlayer.rb, out PAPlayer player))
                transformable.SetTransform(toType, toAxis, Mathf.Clamp((player.RuntimePlayer.rb.transform.GetLocalVector(fromType).At(fromAxis) - offset) * multiply, min, max));
        }

        public static void legacyTail(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not BeatmapObject beatmapObject || modifier.values.IsEmpty() || !GameData.Current)
                return;

            var totalTime = modifier.GetFloat(0, 200f, variables);

            var list = modifier.Result is List<LegacyTracker> ? (List<LegacyTracker>)modifier.Result : new List<LegacyTracker>();

            if (!modifier.HasResult())
            {
                list.Add(new LegacyTracker(beatmapObject, Vector3.zero, Vector3.zero, Quaternion.identity, 0f, 0f));

                for (int i = 1; i < modifier.values.Count; i += 3)
                {
                    var group = GameData.Current.FindObjectsWithTag(modifier, beatmapObject, modifier.GetValue(i, variables));

                    if (modifier.values.Count <= i + 2 || group.Count < 1)
                        break;

                    var distance = modifier.GetFloat(i + 1, 2f, variables);
                    var time = modifier.GetFloat(i + 2, 12f, variables);

                    for (int j = 0; j < group.Count; j++)
                    {
                        var tail = group[j];
                        list.Add(new LegacyTracker(tail, tail.positionOffset, tail.positionOffset, Quaternion.Euler(tail.rotationOffset), distance, time));
                    }
                }

                modifier.Result = list;
            }

            var animationResult = beatmapObject.InterpolateChain();
            list[0].pos = animationResult.position;
            list[0].rot = Quaternion.Euler(0f, 0f, animationResult.rotation);

            float num = Time.deltaTime * totalTime;

            for (int i = 1; i < list.Count; i++)
            {
                var tracker = list[i];
                var prevTracker = list[i - 1];
                if (Vector3.Distance(tracker.pos, prevTracker.pos) > tracker.distance)
                {
                    var vector = Vector3.Lerp(tracker.pos, prevTracker.pos, Time.deltaTime * tracker.time);
                    var quaternion = Quaternion.Lerp(tracker.rot, prevTracker.rot, Time.deltaTime * tracker.time);
                    list[i].pos = vector;
                    list[i].rot = quaternion;
                }

                num *= Vector3.Distance(prevTracker.lastPos, tracker.pos);
                tracker.beatmapObject.positionOffset = Vector3.MoveTowards(prevTracker.lastPos, tracker.pos, num);
                prevTracker.lastPos = tracker.pos;
                tracker.beatmapObject.rotationOffset = tracker.rot.eulerAngles;
            }
        }

        public static void gravity(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not ITransformable transformable)
                return;

            var gravityX = modifier.GetFloat(1, 0f, variables);
            var gravityY = modifier.GetFloat(2, 0f, variables);
            var time = modifier.GetFloat(3, 1f, variables);
            var curve = modifier.GetInt(4, 2, variables);

            if (modifier.Result == null)
            {
                modifier.Result = Vector2.zero;
                modifier.ResultTimer = Time.time;
            }
            else
                modifier.Result = RTMath.Lerp(Vector2.zero, new Vector2(gravityX, gravityY), (RTMath.Recursive(Time.time - modifier.ResultTimer, curve)) * (time * CoreHelper.TimeFrame));

            var vector = modifier.GetResult<Vector2>();
            transformable.PositionOffset = RTMath.Rotate(vector, -transformable.GetFullRotation(false).z);
        }

        public static void gravityOther(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not IPrefabable prefabable)
                return;

            var transformables = GameData.Current.FindTransformablesWithTag(modifier, prefabable, modifier.GetValue(0, variables));

            var gravityX = modifier.GetFloat(1, 0f, variables);
            var gravityY = modifier.GetFloat(2, 0f, variables);
            var time = modifier.GetFloat(3, 1f, variables);
            var curve = modifier.GetInt(4, 2, variables);

            if (modifier.Result == null)
            {
                modifier.Result = Vector2.zero;
                modifier.ResultTimer = Time.time;
            }
            else
                modifier.Result = RTMath.Lerp(Vector2.zero, new Vector2(gravityX, gravityY), (RTMath.Recursive(Time.time - modifier.ResultTimer, curve)) * (time * CoreHelper.TimeFrame));

            var vector = modifier.GetResult<Vector2>();
            foreach (var transformable in transformables)
                transformable.PositionOffset = RTMath.Rotate(vector, -transformable.GetFullRotation(false).z);
        }

        #endregion

        #region Prefab

        public static void spawnPrefab(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (modifier.constant || modifier.HasResult())
                return;

            var prefab = GameData.Current.GetPrefab(modifier.GetInt(12, 0, variables), modifier.GetValue(0, variables));

            if (!prefab)
                return;

            var posX = modifier.GetFloat(1, 0f, variables);
            var posY = modifier.GetFloat(2, 0f, variables);
            var scaX = modifier.GetFloat(3, 0f, variables);
            var scaY = modifier.GetFloat(4, 0f, variables);
            var rot = modifier.GetFloat(5, 0f, variables);
            var repeatCount = modifier.GetInt(6, 0, variables);
            var repeatOffsetTime = modifier.GetFloat(7, 0f, variables);
            var speed = modifier.GetFloat(8, 0f, variables);

            var prefabObject = ModifiersHelper.AddPrefabObjectToLevel(prefab,
                modifier.GetBool(11, true, variables) ? AudioManager.inst.CurrentAudioSource.time + modifier.GetFloat(10, 0f, variables) : modifier.GetFloat(10, 0f, variables),
                new Vector2(posX, posY),
                new Vector2(scaX, scaY),
                rot, repeatCount, repeatOffsetTime, speed);

            modifier.Result = prefabObject;
            GameData.Current.prefabObjects.Add(prefabObject);
            RTLevel.Current.postTick.Enqueue(() =>
            {
                RTLevelBase runtimeLevel = reference is PrefabObject p && p.runtimeObject ? p.runtimeObject : reference.GetParentRuntime();
                runtimeLevel?.UpdatePrefab(prefabObject);

                var runtimePrefabObject = prefabObject.runtimeObject;
                if (runtimePrefabObject && modifier.GetBool(13, false, variables))
                    runtimePrefabObject.onActiveChanged = enabled =>
                    {
                        if (enabled)
                            return;

                        RTLevel.Current.postTick.Enqueue(() =>
                        {
                            RTLevelBase runtimeLevel = reference is PrefabObject p && p.runtimeObject ? p.runtimeObject : reference.GetParentRuntime();
                            runtimeLevel?.UpdatePrefab(prefabObject, false);

                            GameData.Current.prefabObjects.RemoveAll(x => x.fromModifier && x.id == prefabObject.id);

                            modifier.Result = null;
                        });
                    };
            });
        }

        public static void spawnPrefabOffset(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (modifier.constant || modifier.HasResult() || reference is not ITransformable transformable)
                return;

            var prefab = GameData.Current.GetPrefab(modifier.GetInt(12, 0, variables), modifier.GetValue(0, variables));

            if (!prefab)
                return;

            var animationResult = transformable.GetObjectTransform();

            var posX = modifier.GetFloat(1, 0f, variables);
            var posY = modifier.GetFloat(2, 0f, variables);
            var scaX = modifier.GetFloat(3, 0f, variables);
            var scaY = modifier.GetFloat(4, 0f, variables);
            var rot = modifier.GetFloat(5, 0f, variables);
            var repeatCount = modifier.GetInt(6, 0, variables);
            var repeatOffsetTime = modifier.GetFloat(7, 0f, variables);
            var speed = modifier.GetFloat(8, 0f, variables);

            var prefabObject = ModifiersHelper.AddPrefabObjectToLevel(prefab,
                modifier.GetBool(11, true) ? AudioManager.inst.CurrentAudioSource.time + modifier.GetFloat(10, 0f, variables) : modifier.GetFloat(10, 0f, variables),
                new Vector2(posX, posY) + (Vector2)animationResult.position,
                new Vector2(scaX, scaY) * animationResult.scale,
                rot + animationResult.rotation, repeatCount, repeatOffsetTime, speed);

            modifier.Result = prefabObject;
            GameData.Current.prefabObjects.Add(prefabObject);
            RTLevel.Current.postTick.Enqueue(() =>
            {
                RTLevelBase runtimeLevel = reference is PrefabObject p && p.runtimeObject ? p.runtimeObject : reference.GetParentRuntime();
                runtimeLevel?.UpdatePrefab(prefabObject);

                var runtimePrefabObject = prefabObject.runtimeObject;
                if (runtimePrefabObject && modifier.GetBool(13, false, variables))
                    runtimePrefabObject.onActiveChanged = enabled =>
                    {
                        if (enabled)
                            return;

                        RTLevel.Current.postTick.Enqueue(() =>
                        {
                            RTLevelBase runtimeLevel = reference is PrefabObject p && p.runtimeObject ? p.runtimeObject : reference.GetParentRuntime();
                            runtimeLevel?.UpdatePrefab(prefabObject, false);

                            GameData.Current.prefabObjects.RemoveAll(x => x.fromModifier && x.id == prefabObject.id);

                            modifier.Result = null;
                        });
                    };
            });
        }

        public static void spawnPrefabOffsetOther(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (modifier.constant || modifier.HasResult() || reference is not IPrefabable prefabable)
                return;

            var prefab = GameData.Current.GetPrefab(modifier.GetInt(13, 0, variables), modifier.GetValue(0, variables));

            if (!prefab)
                return;

            if (!GameData.Current.TryFindTransformableWithTag(modifier, prefabable, modifier.GetValue(10, variables), out ITransformable target))
                return;

            var animationResult = target.GetObjectTransform();

            var posX = modifier.GetFloat(1, 0f, variables);
            var posY = modifier.GetFloat(2, 0f, variables);
            var scaX = modifier.GetFloat(3, 0f, variables);
            var scaY = modifier.GetFloat(4, 0f, variables);
            var rot = modifier.GetFloat(5, 0f, variables);
            var repeatCount = modifier.GetInt(6, 0, variables);
            var repeatOffsetTime = modifier.GetFloat(7, 0f, variables);
            var speed = modifier.GetFloat(8, 0f, variables);

            var prefabObject = ModifiersHelper.AddPrefabObjectToLevel(prefab,
                modifier.GetBool(12, true, variables) ? AudioManager.inst.CurrentAudioSource.time + modifier.GetFloat(11, 0f, variables) : modifier.GetFloat(11, 0f, variables),
                new Vector2(posX, posY) + (Vector2)animationResult.position,
                new Vector2(scaX, scaY) * animationResult.scale,
                rot + animationResult.rotation, repeatCount, repeatOffsetTime, speed);

            modifier.Result = prefabObject;
            GameData.Current.prefabObjects.Add(prefabObject);
            RTLevel.Current.postTick.Enqueue(() =>
            {
                RTLevelBase runtimeLevel = reference is PrefabObject p && p.runtimeObject ? p.runtimeObject : reference.GetParentRuntime();
                runtimeLevel?.UpdatePrefab(prefabObject);

                var runtimePrefabObject = prefabObject.runtimeObject;
                if (runtimePrefabObject && modifier.GetBool(14, false, variables))
                    runtimePrefabObject.onActiveChanged = enabled =>
                    {
                        if (enabled)
                            return;

                        RTLevel.Current.postTick.Enqueue(() =>
                        {
                            RTLevelBase runtimeLevel = reference is PrefabObject p && p.runtimeObject ? p.runtimeObject : reference.GetParentRuntime();
                            runtimeLevel?.UpdatePrefab(prefabObject, false);

                            GameData.Current.prefabObjects.RemoveAll(x => x.fromModifier && x.id == prefabObject.id);

                            modifier.Result = null;
                        });
                    };
            });
        }

        public static void spawnPrefabCopy(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (modifier.constant || modifier.HasResult() || reference is not IPrefabable prefabable)
                return;

            var prefab = GameData.Current.GetPrefab(modifier.GetInt(4, 0, variables), modifier.GetValue(0, variables));

            if (!prefab)
                return;

            if (!GameData.Current.TryFindPrefabObjectWithTag(modifier, prefabable, modifier.GetValue(1), out PrefabObject orig))
                return;

            var prefabObject = new PrefabObject();
            prefabObject.id = LSText.randomString(16);
            prefabObject.prefabID = prefab.id;

            prefabObject.StartTime = modifier.GetBool(3, true, variables) ? AudioManager.inst.CurrentAudioSource.time + modifier.GetFloat(2, 0f, variables) : modifier.GetFloat(2, 0f, variables);

            prefabObject.PasteInstanceData(orig);

            prefabObject.fromModifier = true;

            modifier.Result = prefabObject;
            GameData.Current.prefabObjects.Add(prefabObject);
            RTLevel.Current.postTick.Enqueue(() =>
            {
                RTLevelBase runtimeLevel = reference is PrefabObject p && p.runtimeObject ? p.runtimeObject : reference.GetParentRuntime();
                runtimeLevel?.UpdatePrefab(prefabObject);

                var runtimePrefabObject = prefabObject.runtimeObject;
                if (runtimePrefabObject && modifier.GetBool(6, false, variables))
                    runtimePrefabObject.onActiveChanged = enabled =>
                    {
                        if (enabled)
                            return;

                        RTLevel.Current.postTick.Enqueue(() =>
                        {
                            RTLevelBase runtimeLevel = reference is PrefabObject p && p.runtimeObject ? p.runtimeObject : reference.GetParentRuntime();
                            runtimeLevel?.UpdatePrefab(prefabObject, false);

                            GameData.Current.prefabObjects.RemoveAll(x => x.fromModifier && x.id == prefabObject.id);

                            modifier.Result = null;
                        });
                    };
            });
        }

        public static void spawnMultiPrefab(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (modifier.constant)
                return;

            var prefab = GameData.Current.GetPrefab(modifier.GetInt(11, 0, variables), modifier.GetValue(0, variables));

            if (!prefab)
                return;

            var posX = modifier.GetFloat(1, 0f, variables);
            var posY = modifier.GetFloat(2, 0f, variables);
            var scaX = modifier.GetFloat(3, 0f, variables);
            var scaY = modifier.GetFloat(4, 0f, variables);
            var rot = modifier.GetFloat(5, 0f, variables);
            var repeatCount = modifier.GetInt(6, 0, variables);
            var repeatOffsetTime = modifier.GetFloat(7, 0f, variables);
            var speed = modifier.GetFloat(8, 0f, variables);

            if (!modifier.HasResult())
                modifier.Result = new List<PrefabObject>();

            var list = modifier.GetResult<List<PrefabObject>>();
            var prefabObject = ModifiersHelper.AddPrefabObjectToLevel(prefab,
                modifier.GetBool(10, true, variables) ? AudioManager.inst.CurrentAudioSource.time + modifier.GetFloat(9, 0f, variables) : modifier.GetFloat(9, 0f, variables),
                new Vector2(posX, posY),
                new Vector2(scaX, scaY),
                rot, repeatCount, repeatOffsetTime, speed);

            list.Add(prefabObject);
            modifier.Result = list;

            GameData.Current.prefabObjects.Add(prefabObject);
            RTLevel.Current.postTick.Enqueue(() =>
            {
                RTLevelBase runtimeLevel = reference is PrefabObject p && p.runtimeObject ? p.runtimeObject : reference.GetParentRuntime();
                runtimeLevel?.UpdatePrefab(prefabObject);

                var runtimePrefabObject = prefabObject.runtimeObject;
                if (runtimePrefabObject && modifier.GetBool(12, false, variables))
                    runtimePrefabObject.onActiveChanged = enabled =>
                    {
                        if (enabled)
                            return;

                        RTLevel.Current.postTick.Enqueue(() =>
                        {
                            RTLevelBase runtimeLevel = reference is PrefabObject p && p.runtimeObject ? p.runtimeObject : reference.GetParentRuntime();
                            runtimeLevel?.UpdatePrefab(prefabObject, false);

                            GameData.Current.prefabObjects.RemoveAll(x => x.fromModifier && x.id == prefabObject.id);
                        });
                    };
            });
        }

        public static void spawnMultiPrefabOffset(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (modifier.constant || reference is not ITransformable transformable)
                return;

            var prefab = GameData.Current.GetPrefab(modifier.GetInt(11, 0, variables), modifier.GetValue(0, variables));

            if (!prefab)
                return;

            var animationResult = transformable.GetObjectTransform();

            var posX = modifier.GetFloat(1, 0f, variables);
            var posY = modifier.GetFloat(2, 0f, variables);
            var scaX = modifier.GetFloat(3, 0f, variables);
            var scaY = modifier.GetFloat(4, 0f, variables);
            var rot = modifier.GetFloat(5, 0f, variables);
            var repeatCount = modifier.GetInt(6, 0, variables);
            var repeatOffsetTime = modifier.GetFloat(7, 0f, variables);
            var speed = modifier.GetFloat(8, 0f, variables);

            if (!modifier.HasResult())
                modifier.Result = new List<PrefabObject>();

            var list = modifier.GetResult<List<PrefabObject>>();
            var prefabObject = ModifiersHelper.AddPrefabObjectToLevel(prefab,
                modifier.GetBool(10, true, variables) ? AudioManager.inst.CurrentAudioSource.time + modifier.GetFloat(9, 0f, variables) : modifier.GetFloat(9, 0f, variables),
                new Vector2(posX, posY) + (Vector2)animationResult.position,
                new Vector2(scaX, scaY) * animationResult.scale,
                rot + animationResult.rotation, repeatCount, repeatOffsetTime, speed);

            list.Add(prefabObject);
            modifier.Result = list;

            GameData.Current.prefabObjects.Add(prefabObject);
            RTLevel.Current.postTick.Enqueue(() =>
            {
                RTLevelBase runtimeLevel = reference is PrefabObject p && p.runtimeObject ? p.runtimeObject : reference.GetParentRuntime();
                runtimeLevel?.UpdatePrefab(prefabObject);

                var runtimePrefabObject = prefabObject.runtimeObject;
                if (runtimePrefabObject && modifier.GetBool(12, false, variables))
                    runtimePrefabObject.onActiveChanged = enabled =>
                    {
                        if (enabled)
                            return;

                        RTLevel.Current.postTick.Enqueue(() =>
                        {
                            RTLevelBase runtimeLevel = reference is PrefabObject p && p.runtimeObject ? p.runtimeObject : reference.GetParentRuntime();
                            runtimeLevel?.UpdatePrefab(prefabObject, false);

                            GameData.Current.prefabObjects.RemoveAll(x => x.fromModifier && x.id == prefabObject.id);
                        });
                    };
            });
        }

        public static void spawnMultiPrefabOffsetOther(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (modifier.constant || reference is not IPrefabable prefabable)
                return;

            var prefab = GameData.Current.GetPrefab(modifier.GetInt(12, 0, variables), modifier.GetValue(0, variables));

            if (!prefab)
                return;

            if (!GameData.Current.TryFindTransformableWithTag(modifier, prefabable, modifier.GetValue(9, variables), out ITransformable target))
                return;

            var animationResult = target.GetObjectTransform();

            var posX = modifier.GetFloat(1, 0f, variables);
            var posY = modifier.GetFloat(2, 0f, variables);
            var scaX = modifier.GetFloat(3, 0f, variables);
            var scaY = modifier.GetFloat(4, 0f, variables);
            var rot = modifier.GetFloat(5, 0f, variables);
            var repeatCount = modifier.GetInt(6, 0, variables);
            var repeatOffsetTime = modifier.GetFloat(7, 0f, variables);
            var speed = modifier.GetFloat(8, 0f, variables);

            if (!modifier.HasResult())
                modifier.Result = new List<PrefabObject>();

            var list = modifier.GetResult<List<PrefabObject>>();
            var prefabObject = ModifiersHelper.AddPrefabObjectToLevel(prefab,
                modifier.GetBool(11, true, variables) ? AudioManager.inst.CurrentAudioSource.time + modifier.GetFloat(10, 0f, variables) : modifier.GetFloat(10, 0f, variables),
                new Vector2(posX, posY) + (Vector2)animationResult.position,
                new Vector2(scaX, scaY) * animationResult.scale,
                rot + animationResult.rotation, repeatCount, repeatOffsetTime, speed);

            list.Add(prefabObject);
            modifier.Result = list;

            GameData.Current.prefabObjects.Add(prefabObject);
            RTLevel.Current.postTick.Enqueue(() =>
            {
                RTLevelBase runtimeLevel = reference is PrefabObject p && p.runtimeObject ? p.runtimeObject : reference.GetParentRuntime();
                runtimeLevel?.UpdatePrefab(prefabObject);

                var runtimePrefabObject = prefabObject.runtimeObject;
                if (runtimePrefabObject && modifier.GetBool(13, false, variables))
                    runtimePrefabObject.onActiveChanged = enabled =>
                    {
                        if (enabled)
                            return;

                        RTLevel.Current.postTick.Enqueue(() =>
                        {
                            RTLevelBase runtimeLevel = reference is PrefabObject p && p.runtimeObject ? p.runtimeObject : reference.GetParentRuntime();
                            runtimeLevel?.UpdatePrefab(prefabObject, false);

                            GameData.Current.prefabObjects.RemoveAll(x => x.fromModifier && x.id == prefabObject.id);
                        });
                    };
            });
        }

        public static void spawnMultiPrefabCopy(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (modifier.constant || modifier.HasResult() || reference is not IPrefabable prefabable)
                return;

            var prefab = GameData.Current.GetPrefab(modifier.GetInt(4, 0, variables), modifier.GetValue(0, variables));

            if (!prefab)
                return;

            if (!GameData.Current.TryFindPrefabObjectWithTag(modifier, prefabable, modifier.GetValue(1), out PrefabObject orig))
                return;

            if (!modifier.HasResult())
                modifier.Result = new List<PrefabObject>();

            var list = modifier.GetResult<List<PrefabObject>>();
            var prefabObject = new PrefabObject();
            prefabObject.id = LSText.randomString(16);
            prefabObject.prefabID = prefab.id;

            prefabObject.StartTime = modifier.GetBool(3, true, variables) ? AudioManager.inst.CurrentAudioSource.time + modifier.GetFloat(2, 0f, variables) : modifier.GetFloat(2, 0f, variables);

            prefabObject.PasteInstanceData(orig);

            prefabObject.fromModifier = true;

            list.Add(prefabObject);
            modifier.Result = list;

            GameData.Current.prefabObjects.Add(prefabObject);
            RTLevel.Current.postTick.Enqueue(() =>
            {
                RTLevelBase runtimeLevel = reference is PrefabObject p && p.runtimeObject ? p.runtimeObject : reference.GetParentRuntime();
                runtimeLevel?.UpdatePrefab(prefabObject);

                var runtimePrefabObject = prefabObject.runtimeObject;
                if (runtimePrefabObject && modifier.GetBool(5, false, variables))
                    runtimePrefabObject.onActiveChanged = enabled =>
                    {
                        if (enabled)
                            return;

                        RTLevel.Current.postTick.Enqueue(() =>
                        {
                            RTLevelBase runtimeLevel = reference is PrefabObject p && p.runtimeObject ? p.runtimeObject : reference.GetParentRuntime();
                            runtimeLevel?.UpdatePrefab(prefabObject, false);

                            GameData.Current.prefabObjects.RemoveAll(x => x.fromModifier && x.id == prefabObject.id);
                        });
                    };
            });
        }

        public static void clearSpawnedPrefabs(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not IPrefabable prefabable)
                return;

            var modifyables = GameData.Current.FindModifyables(modifier, prefabable, modifier.GetValue(0, variables)).ToList();

            RTLevel.Current.postTick.Enqueue(() =>
            {
                RTLevelBase runtimeLevel = reference is PrefabObject p && p.runtimeObject ? p.runtimeObject : reference.GetParentRuntime();

                foreach (var modifyable in modifyables)
                {
                    for (int j = 0; j < modifyable.Modifiers.Count; j++)
                    {
                        var otherModifier = modifyable.Modifiers[j];

                        if (otherModifier.TryGetResult(out PrefabObject prefabObjectResult))
                        {
                            runtimeLevel?.UpdatePrefab(prefabObjectResult, false);

                            GameData.Current.prefabObjects.RemoveAll(x => x.fromModifier && x.id == prefabObjectResult.id);

                            otherModifier.Result = null;
                            continue;
                        }

                        if (!otherModifier.TryGetResult(out List<PrefabObject> result))
                            continue;

                        for (int k = 0; k < result.Count; k++)
                        {
                            var prefabObject = result[k];

                            runtimeLevel?.UpdatePrefab(prefabObject, false);
                            GameData.Current.prefabObjects.RemoveAll(x => x.fromModifier && x.id == prefabObject.id);
                        }

                        result.Clear();
                        otherModifier.Result = null;
                    }
                }
            });
        }

        public static void setPrefabTime(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is PrefabObject prefabObject && prefabObject.runtimeObject)
            {
                prefabObject.runtimeObject.CustomTime = modifier.GetFloat(0, 0f, variables);
                prefabObject.runtimeObject.UseCustomTime = modifier.GetBool(1, false, variables);
            }
        }

        public static void enablePrefab(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            var prefabable = reference.AsPrefabable();
            if (prefabable != null && prefabable.FromPrefab)
                prefabable.GetPrefabObject()?.runtimeObject?.SetCustomActive(modifier.GetBool(0, true, variables));
        }

        public static void updatePrefab(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (modifier.constant)
                return;

            var prefabable = reference.AsPrefabable();
            if (prefabable == null || !prefabable.FromPrefab)
                return;

            var reinsert = modifier.GetBool(0, true, variables);
            RTLevel.Current.postTick.Enqueue(() =>
            {
                var prefabObject = prefabable.GetPrefabObject();
                if (prefabObject)
                    prefabObject.GetParentRuntime()?.UpdatePrefab(prefabObject, reinsert: reinsert);
            });
        }

        #endregion

        #region Ranking

        public static void unlockAchievement(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            var id = modifier.GetValue(0, variables);
            if (CoreHelper.InEditor)
            {
                if (!EditorConfig.Instance.ModifiersDisplayAchievements.Value)
                    return;

                var achievement = AchievementEditor.inst.achievements.Find(x => x.id == id);
                AchievementManager.inst.ShowAchievement(achievement);
                return;
            }

            if (!LevelManager.CurrentLevel)
                return;

            if (!LevelManager.CurrentLevel.saveData)
                LevelManager.AssignSaveData(LevelManager.CurrentLevel);
            LevelManager.CurrentLevel.saveData.UnlockAchievement(id);
        }

        public static void lockAchievement(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (!LevelManager.CurrentLevel)
                return;

            if (!LevelManager.CurrentLevel.saveData)
                LevelManager.AssignSaveData(LevelManager.CurrentLevel);
            LevelManager.CurrentLevel.saveData.LockAchievement(modifier.GetValue(0, variables));
        }

        public static void getAchievementUnlocked(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (!LevelManager.CurrentLevel)
                return;

            if (!LevelManager.CurrentLevel.saveData)
                LevelManager.AssignSaveData(LevelManager.CurrentLevel);

            // global or local
            var unlocked = modifier.GetBool(2, false, variables) ?
                AchievementManager.unlockedCustomAchievements.TryGetValue(modifier.GetValue(1, variables), out bool global) && global :
                LevelManager.CurrentLevel && LevelManager.CurrentLevel.saveData && LevelManager.CurrentLevel.saveData.AchievementUnlocked(modifier.GetValue(1, variables));
            variables[modifier.GetValue(0)] = unlocked.ToString();
        }

        public static void saveLevelRank(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (CoreHelper.InEditor || modifier.constant || !LevelManager.CurrentLevel)
                return;

            LevelManager.UpdateCurrentLevelProgress();
        }

        public static void clearHits(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            RTBeatmap.Current.hits.Clear();
        }

        public static void addHit(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            var vector = Vector3.zero;
            if (reference is BeatmapObject beatmapObject)
            {
                if (modifier.GetBool(0, true, variables))
                    vector = beatmapObject.GetFullPosition();
                else
                {
                    var player = PlayerManager.GetClosestPlayer(beatmapObject.GetFullPosition());
                    if (player && player.RuntimePlayer)
                        vector = player.RuntimePlayer.rb.position;
                }
            }

            var timeValue = modifier.GetValue(1, variables);
            float time = AudioManager.inst.CurrentAudioSource.time;
            if (!string.IsNullOrEmpty(timeValue) && reference is IEvaluatable evaluatable)
            {
                var numberVariables = evaluatable.GetObjectVariables();
                ModifiersHelper.SetVariables(variables, numberVariables);

                time = RTMath.Parse(timeValue, numberVariables, evaluatable.GetObjectFunctions());
            }

            RTBeatmap.Current.hits.Add(new PlayerDataPoint(vector, time));
        }

        public static void subHit(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (!RTBeatmap.Current.hits.IsEmpty())
                RTBeatmap.Current.hits.RemoveAt(RTBeatmap.Current.hits.Count - 1);
        }

        public static void clearDeaths(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            RTBeatmap.Current.deaths.Clear();
        }

        public static void addDeath(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            var vector = Vector3.zero;
            if (reference is BeatmapObject beatmapObject)
            {
                if (modifier.GetBool(0, true, variables))
                    vector = beatmapObject.GetFullPosition();
                else
                {
                    var player = PlayerManager.GetClosestPlayer(beatmapObject.GetFullPosition());
                    if (player && player.RuntimePlayer)
                        vector = player.RuntimePlayer.rb.position;
                }
            }

            var timeValue = modifier.GetValue(1, variables);
            float time = AudioManager.inst.CurrentAudioSource.time;
            if (!string.IsNullOrEmpty(timeValue) && reference is IEvaluatable evaluatable)
            {
                var numberVariables = evaluatable.GetObjectVariables();
                ModifiersHelper.SetVariables(variables, numberVariables);

                time = RTMath.Parse(timeValue, numberVariables, evaluatable.GetObjectFunctions());
            }

            RTBeatmap.Current.deaths.Add(new PlayerDataPoint(vector, time));
        }

        public static void subDeath(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (!RTBeatmap.Current.deaths.IsEmpty())
                RTBeatmap.Current.deaths.RemoveAt(RTBeatmap.Current.deaths.Count - 1);
        }

        public static void getHitCount(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            variables[modifier.GetValue(0)] = RTBeatmap.Current.hits.Count.ToString();
        }

        public static void getDeathCount(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            variables[modifier.GetValue(0)] = RTBeatmap.Current.deaths.Count.ToString();
        }

        #endregion

        #region Updates

        public static void updateObjects(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (!modifier.constant)
                CoroutineHelper.StartCoroutine(RTLevel.IReinit());
        }

        public static void reinitLevel(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (!modifier.constant)
                CoroutineHelper.StartCoroutine(RTLevel.IReinit());
        }

        public static void updateObject(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (modifier.constant)
                return;

            var reinsert = modifier.GetBool(0, true, variables);
            RTLevel.Current.postTick.Enqueue(() =>
            {
                if (reference is BeatmapObject beatmapObject)
                    beatmapObject.GetParentRuntime()?.UpdateObject(beatmapObject, reinsert: reinsert);
                if (reference is BackgroundObject backgroundObject)
                    backgroundObject.GetParentRuntime()?.UpdateBackgroundObject(backgroundObject, reinsert: reinsert);
                if (reference is PrefabObject prefabObject)
                    prefabObject.GetParentRuntime()?.UpdatePrefab(prefabObject, reinsert: reinsert);
            });
        }

        public static void updateObjectOther(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (modifier.constant || reference is not IPrefabable prefabable)
                return;

            var prefabables = GameData.Current.FindPrefabablesWithTag(modifier, prefabable, modifier.GetValue(0));
            if (prefabables.IsEmpty())
                return;

            var reinsert = modifier.GetBool(1, true, variables);
            RTLevel.Current.postTick.Enqueue(() =>
            {
                foreach (var other in prefabables)
                {
                    if (other is BeatmapObject beatmapObject)
                        beatmapObject.GetParentRuntime()?.UpdateObject(beatmapObject, reinsert: reinsert, recalculate: false);
                    if (other is BackgroundObject backgroundObject)
                        backgroundObject.GetParentRuntime()?.UpdateBackgroundObject(backgroundObject, reinsert: reinsert, recalculate: false);
                    if (other is PrefabObject prefabObject)
                        prefabObject.GetParentRuntime()?.UpdatePrefab(prefabObject, reinsert: reinsert, recalculate: false);
                }
                RTLevel.Current?.RecalculateObjectStates();
            });
        }

        public static void setStartTime(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            var startTime = modifier.GetFloat(0, 0f, variables);
            var killTime = modifier.GetFloat(1, 0f, variables);

            RTLevel.Current.postTick.Enqueue(() =>
            {
                var runtimeObject = reference.GetRuntimeObject();
                if (runtimeObject == null)
                    return;

                var changed = runtimeObject.StartTime != startTime || runtimeObject.KillTime != killTime;

                runtimeObject.StartTime = startTime;
                runtimeObject.KillTime = killTime;

                if (changed)
                    RTLevel.Current.RecalculateObjectStates();
            });
        }

        public static void setParent(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (modifier.constant || reference is not IParentable child)
                return;

            var prefabable = reference.AsPrefabable();
            if (prefabable == null)
                return;

            var group = modifier.GetValue(0, variables);

            var result = modifier.GetResultOrDefault(() => GroupCache.FromSingle(modifier, prefabable, group));

            if (result.group != group)
            {
                result = GroupCache.FromSingle(modifier, prefabable, group);
                modifier.Result = result;
            }

            if (group == string.Empty)
                ModifiersHelper.SetParent(child, string.Empty);
            else if (result.target && child.CanParent(result.target))
                ModifiersHelper.SetParent(child, result.target);
            else
                CoreHelper.LogError($"CANNOT PARENT OBJECT!\nID: {child.ID}");
        }

        public static void setParentOther(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (modifier.constant)
                return;

            var prefabable = reference.AsPrefabable();
            if (prefabable == null)
                return;

            var group = modifier.GetValue(2, variables);

            var result = modifier.GetResultOrDefault(() => GroupCache.FromGroup(reference, modifier, prefabable, group, modifier.GetValue(0, variables)));

            if (result.group != group)
            {
                result = GroupCache.FromGroup(reference, modifier, prefabable, group, modifier.GetValue(0, variables));
                modifier.Result = result;
            }

            var isEmpty = modifier.GetBool(1, false, variables);

            bool failed = false;
            foreach (var parentable in result.parentables)
            {
                if (isEmpty)
                    ModifiersHelper.SetParent(parentable, string.Empty);
                else if (parentable.CanParent(result.target))
                    ModifiersHelper.SetParent(parentable, result.target);
                else
                    failed = true;
            }

            if (failed)
                CoreHelper.LogError($"CANNOT PARENT OBJECT {reference}");
        }

        public static void detachParent(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (modifier.constant || reference is not IParentable parentable)
                return;

            parentable.ParentDetatched = modifier.GetBool(0, true, variables);

            if (reference is not PrefabObject prefabObject || !prefabObject.runtimeObject)
                return;

            foreach (var beatmapObject in prefabObject.runtimeObject.Spawner.BeatmapObjects)
                beatmapObject.detatched = prefabObject.detatched;
        }

        public static void detachParentOther(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (modifier.constant || reference is not IPrefabable prefabable)
                return;

            var parentables = GameData.Current.FindParentables(modifier, prefabable, modifier.GetValue(1, variables));
            var detach = modifier.GetBool(0, true, variables);

            foreach (var other in parentables)
            {
                other.ParentDetatched = detach;

                if (other is not PrefabObject prefabObject || !prefabObject.runtimeObject)
                    continue;

                foreach (var beatmapObject in prefabObject.runtimeObject.Spawner.BeatmapObjects)
                    beatmapObject.detatched = prefabObject.detatched;
            }
        }

        public static void setSeed(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (!modifier.constant)
                RTLevel.Current?.InitSeed(modifier.GetValue(0, variables));
        }

        #endregion

        #region Physics

        public static void setCollision(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is BeatmapObject beatmapObject && beatmapObject.runtimeObject is RTBeatmapObject runtimeObject && runtimeObject.visualObject && runtimeObject.visualObject.collider)
                runtimeObject.visualObject.colliderEnabled = modifier.GetBool(0, false, variables);
        }

        public static void setCollisionOther(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not IPrefabable prefabable)
                return;

            var colliderEnabled = modifier.GetBool(0, false, variables);
            var list = GameData.Current.FindObjectsWithTag(modifier, prefabable, modifier.GetValue(1, variables));

            foreach (var beatmapObject in list)
            {
                if (beatmapObject.runtimeObject is RTBeatmapObject runtimeObject && runtimeObject.visualObject && runtimeObject.visualObject.collider)
                    runtimeObject.visualObject.colliderEnabled = colliderEnabled;
            }
        }

        #endregion

        #region Checkpoints

        public static void createCheckpoint(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            // if active checpoints matches the stored checkpoint, do not create a new checkpoint.
            if (modifier.TryGetResult(out Checkpoint prevCheckpoint) && prevCheckpoint.id == RTBeatmap.Current.ActiveCheckpoint.id)
                return;

            var checkpoint = new Checkpoint();
            checkpoint.time = modifier.GetBool(1, true, variables) ? reference.GetParentRuntime().FixedTime + modifier.GetFloat(0, 0f, variables) : modifier.GetFloat(0, 0f, variables);
            checkpoint.pos = new Vector2(modifier.GetFloat(2, 0f, variables), modifier.GetFloat(3, 0f, variables));
            checkpoint.heal = modifier.GetBool(4, false, variables);
            checkpoint.respawn = modifier.GetBool(5, true, variables);
            checkpoint.reverse = modifier.GetBool(6, true, variables);
            checkpoint.setTime = modifier.GetBool(7, true, variables);
            checkpoint.spawnType = (Checkpoint.SpawnPositionType)modifier.GetInt(8, 0, variables);
            for (int i = 9; i < modifier.values.Count; i += 2)
                checkpoint.positions.Add(new Vector2(modifier.GetFloat(i, 0f, variables), modifier.GetFloat(i + 1, 0f, variables)));

            RTBeatmap.Current.SetCheckpoint(checkpoint);
            modifier.Result = checkpoint;
        }

        public static void resetCheckpoint(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            RTBeatmap.Current.ResetCheckpoint(modifier.GetBool(0, false, variables));
        }

        #endregion

        #region Interfaces

        public static void loadInterface(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (CoreHelper.IsEditing) // don't want interfaces to load in editor
            {
                EditorManager.inst.DisplayNotification($"Cannot load interface in the editor!", 1f, EditorManager.NotificationType.Warning);
                return;
            }

            var value = modifier.GetValue(0, variables);
            var path = RTFile.CombinePaths(RTFile.BasePath, value + FileFormat.LSI.Dot());

            if (!RTFile.FileExists(path))
            {
                CoreHelper.LogError($"Interface with file name: \"{value}\" does not exist.");
                return;
            }

            Dictionary<string, JSONNode> customVariables = null;
            if (modifier.GetBool(2, false, variables))
            {
                customVariables = new Dictionary<string, JSONNode>();
                foreach (var variable in variables)
                    customVariables[variable.Key] = variable.Value;
            }

            InterfaceManager.inst.ParseInterface(path, customVariables: customVariables);

            InterfaceManager.inst.MainDirectory = RTFile.BasePath;

            if (modifier.GetBool(1, true, variables))
                RTBeatmap.Current.Pause();
            ArcadeHelper.endedLevel = false;
        }

        public static void exitInterface(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            InterfaceManager.inst.CloseMenus();
            if (CoreHelper.Paused)
                RTBeatmap.Current.Resume();
        }

        public static void pauseLevel(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (CoreHelper.InEditor)
            {
                EditorManager.inst.DisplayNotification("Cannot pause in the editor. This modifier only works in the Arcade.", 3f, EditorManager.NotificationType.Warning);
                return;
            }

            PauseMenu.Pause();
        }

        public static void quitToMenu(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (CoreHelper.InEditor && !EditorManager.inst.isEditing && EditorConfig.Instance.ModifiersCanLoadLevels.Value)
            {
                string str = RTFile.BasePath;
                if (EditorConfig.Instance.ModifiersSavesBackup.Value)
                {
                    GameData.Current.SaveData(RTFile.CombinePaths(str, $"level-modifier-backup{FileFormat.LSB.Dot()}"), () =>
                    {
                        EditorManager.inst.DisplayNotification($"Saved backup to {System.IO.Path.GetFileName(RTFile.RemoveEndSlash(str))}", 2f, EditorManager.NotificationType.Success);
                    });
                }

                EditorManager.inst.QuitToMenu();
            }

            if (!CoreHelper.InEditor)
                ArcadeHelper.QuitToMainMenu();
        }

        public static void quitToArcade(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (CoreHelper.InEditor && !EditorManager.inst.isEditing && EditorConfig.Instance.ModifiersCanLoadLevels.Value)
            {
                string str = RTFile.BasePath;
                if (EditorConfig.Instance.ModifiersSavesBackup.Value)
                {
                    GameData.Current.SaveData(RTFile.CombinePaths(str, $"level-modifier-backup{FileFormat.LSB.Dot()}"), () =>
                    {
                        EditorManager.inst.DisplayNotification($"Saved backup to {System.IO.Path.GetFileName(RTFile.RemoveEndSlash(str))}", 2f, EditorManager.NotificationType.Success);
                    });
                }

                GameManager.inst.QuitToArcade();

                return;
            }

            if (!CoreHelper.InEditor)
                ArcadeHelper.QuitToArcade();
        }

        #endregion

        #region Misc

        public static void setBGActive(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            var active = modifier.GetBool(0, false, variables);
            var tag = modifier.GetValue(1, variables);
            var list = GameData.Current.backgroundObjects.FindAll(x => x.tags.Contains(tag));
            if (!list.IsEmpty())
                for (int i = 0; i < list.Count; i++)
                    list[i].Enabled = active;
        }

        public static void signalModifier(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not IPrefabable prefabable)
                return;

            var list = GameData.Current.FindObjectsWithTag(modifier, prefabable, modifier.GetValue(1, variables));
            var delay = modifier.GetFloat(0, 0f, variables);

            foreach (var bm in list)
                CoroutineHelper.StartCoroutine(ModifiersHelper.ActivateModifier(bm, delay));
        }

        public static void activateModifier(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not IPrefabable prefabable)
                return;

            var list = GameData.Current.FindObjectsWithTag(modifier, prefabable, modifier.GetValue(0, variables));

            var doMultiple = modifier.GetBool(1, true, variables);
            var index = modifier.GetInt(2, -1, variables);

            // 3 is modifier names
            var modifierNames = new List<string>();
            for (int i = 3; i < modifier.values.Count; i++)
                modifierNames.Add(modifier.GetValue(i, variables));

            for (int i = 0; i < list.Count; i++)
            {
                if (doMultiple)
                {
                    var modifiers = list[i].modifiers.FindAll(x => x.type == Modifier.Type.Action && modifierNames.Contains(x.Name));

                    for (int j = 0; j < modifiers.Count; j++)
                    {
                        var otherModifier = modifiers[i];
                        otherModifier.Action?.Invoke(otherModifier, list[i], variables);
                    }
                    continue;
                }

                if (index >= 0 && index < list[i].modifiers.Count)
                {
                    var otherModifier = list[i].modifiers[index];
                    otherModifier.Action?.Invoke(otherModifier, list[i], variables);
                }
            }
        }

        public static void editorNotify(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            var text = ModifiersHelper.FormatStringVariables(modifier.GetValue(0, variables), variables);

            if (CoreHelper.InEditor)
                EditorManager.inst.DisplayNotification(
                    /*text: */ text,
                    /*time: */ modifier.GetFloat(1, 0.5f, variables),
                    /*type: */ (EditorManager.NotificationType)modifier.GetInt(2, 0, variables));
        }

        public static void setWindowTitle(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables) => ProjectArrhythmia.Window.SetTitle(modifier.GetValue(0, variables));

        public static void setDiscordStatus(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            var discordSubIcons = CoreHelper.discordSubIcons;
            var discordIcons = CoreHelper.discordIcons;

            var state = ModifiersHelper.FormatStringVariables(modifier.GetValue(0, variables), variables);
            var details = ModifiersHelper.FormatStringVariables(modifier.GetValue(1, variables), variables);
            var discordSubIcon = modifier.GetInt(2, 0, variables);
            var discordIcon = modifier.GetInt(3, 0, variables);

            try
            {
                CoreHelper.UpdateDiscordStatus(
                    string.Format(state, MetaData.Current.song.title, $"{(!CoreHelper.InEditor ? "Game" : "Editor")}", $"{(!CoreHelper.InEditor ? "Level" : "Editing")}", $"{(!CoreHelper.InEditor ? "Arcade" : "Editor")}"),
                    string.Format(details, MetaData.Current.song.title, $"{(!CoreHelper.InEditor ? "Game" : "Editor")}", $"{(!CoreHelper.InEditor ? "Level" : "Editing")}", $"{(!CoreHelper.InEditor ? "Arcade" : "Editor")}"),
                    discordSubIcons[Mathf.Clamp(discordSubIcon, 0, discordSubIcons.Length - 1)], discordIcons[Mathf.Clamp(discordIcon, 0, discordIcons.Length - 1)]);
            }
            catch
            {
                CoreHelper.UpdateDiscordStatus((CoreHelper.InEditor ? "Editing: " : "Level: ") + MetaData.Current.beatmap.name, CoreHelper.InEditor ? "In Editor" : "In Arcade", CoreHelper.InEditor ? "editor" : "arcade");
            }
        }

        public static void callModifierBlock(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            var name = modifier.GetValue(0, variables);

            var prefabable = reference.AsPrefabable();
            var prefab = prefabable?.GetPrefab();
            if (prefabable != null && prefab && prefab.modifierBlocks.TryFind(x => x.Name == name, out ModifierBlock prefabModifierBlock))
                prefabModifierBlock.Run(reference, variables);
            else if (GameData.Current.modifierBlocks.TryFind(x => x.Name == name, out ModifierBlock modifierBlock))
                modifierBlock.Run(reference, variables);
        }

        #endregion

        #region Player Only

        public static void setCustomObjectActive(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            var id = modifier.GetValue(1, variables);
            var player = reference is RTPlayer.RTCustomPlayerObject customPlayerObject ? customPlayerObject.Player.Core : reference as PAPlayer;

            if (player && player.RuntimePlayer && player.RuntimePlayer.customObjects.TryFind(x => x.id == id, out RTPlayer.RTCustomPlayerObject customObject))
                customObject.active = modifier.GetBool(0, false, variables);
        }

        public static void setCustomObjectIdle(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            var id = modifier.GetValue(0, variables);
            var idle = modifier.GetBool(1, true, variables);
            var customPlayerObject = reference as RTPlayer.RTCustomPlayerObject;
            var player = customPlayerObject ? customPlayerObject.Player.Core : reference as PAPlayer;

            if (!player || !player.RuntimePlayer)
                return;

            var customObject = string.IsNullOrEmpty(id) && customPlayerObject ? customPlayerObject : player.RuntimePlayer.customObjects.Find(x => x.id == id);

            if (customObject)
                customObject.idle = idle;
        }

        public static void setIdleAnimation(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            var id = modifier.GetValue(0, variables);
            var referenceID = modifier.GetValue(1, variables);
            var customPlayerObject = reference as RTPlayer.RTCustomPlayerObject;
            var player = customPlayerObject ? customPlayerObject.Player.Core : reference as PAPlayer;

            if (!player || !player.RuntimePlayer)
                return;

            var customObject = string.IsNullOrEmpty(id) && customPlayerObject ? customPlayerObject : player.RuntimePlayer.customObjects.Find(x => x.id == id);

            if (customObject && customObject.reference && customObject.reference.animations.TryFind(x => x.ReferenceID == referenceID, out PAAnimation animation))
                customObject.currentIdleAnimation = animation.ReferenceID;
        }

        public static void playAnimation(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            var id = modifier.GetValue(0, variables);
            var referenceID = modifier.GetValue(1, variables);
            var customPlayerObject = reference as RTPlayer.RTCustomPlayerObject;
            var player = customPlayerObject ? customPlayerObject.Player.Core : reference as PAPlayer;

            if (!player || !player.RuntimePlayer)
                return;

            var customObject = string.IsNullOrEmpty(id) && customPlayerObject ? customPlayerObject : player.RuntimePlayer.customObjects.Find(x => x.id == id);

            if (customObject && customObject.reference && customObject.reference.animations.TryFind(x => x.ReferenceID == referenceID, out PAAnimation animation))
            {
                var runtimeAnimation = new RTAnimation("Custom Animation");
                runtimeAnimation.SetDefaultOnComplete(player.RuntimePlayer.animationController);
                player.RuntimePlayer.ApplyAnimation(runtimeAnimation, animation, customObject);
                player.RuntimePlayer.animationController.Play(runtimeAnimation);
            }
        }

        public static void kill(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is PAPlayer player)
                player.Health = 0;
        }

        public static void hit(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not PAPlayer player)
                return;

            var damage = modifier.GetInt(0, 0, variables);
            if (damage <= 1)
                player.RuntimePlayer?.Hit();
            else
                player.RuntimePlayer?.Hit(damage);
        }

        public static void boost(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is PAPlayer player)
                player.RuntimePlayer?.Boost();
        }

        public static void shoot(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is PAPlayer player)
                player.RuntimePlayer?.Shoot();
        }

        public static void pulse(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is PAPlayer player)
                player.RuntimePlayer?.Pulse();
        }

        public static void jump(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is PAPlayer player)
                player.RuntimePlayer?.Jump();
        }

        public static void getHealth(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            var player = reference is RTPlayer.RTCustomPlayerObject customPlayerObject ? customPlayerObject.Player.Core : reference as PAPlayer;
            if (!player)
                return;

            variables[modifier.GetValue(0)] = player.Health.ToString();
        }

        public static void getLives(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            var player = reference is RTPlayer.RTCustomPlayerObject customPlayerObject ? customPlayerObject.Player.Core : reference as PAPlayer;
            if (!player)
                return;

            variables[modifier.GetValue(0)] = player.lives.ToString();
        }

        public static void getMaxHealth(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            var player = reference is RTPlayer.RTCustomPlayerObject customPlayerObject ? customPlayerObject.Player.Core : reference as PAPlayer;
            if (!player)
                return;

            variables[modifier.GetValue(0)] = player.GetMaxHealth().ToString();
        }

        public static void getMaxLives(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            var player = reference is RTPlayer.RTCustomPlayerObject customPlayerObject ? customPlayerObject.Player.Core : reference as PAPlayer;
            if (!player)
                return;

            variables[modifier.GetValue(0)] = player.GetMaxLives().ToString();
        }

        public static void getIndex(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            var player = reference is RTPlayer.RTCustomPlayerObject customPlayerObject ? customPlayerObject.Player.Core : reference as PAPlayer;
            if (!player)
                return;

            variables[modifier.GetValue(0)] = player.index.ToString();
        }

        public static void getMove(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            var player = reference is RTPlayer.RTCustomPlayerObject customPlayerObject ? customPlayerObject.Player.Core : reference as PAPlayer;
            if (!player)
                return;

            var move = player.RuntimePlayer.Actions.Move.Vector;
            if (move.magnitude > 1f && modifier.GetBool(2, true, variables))
                move = move.normalized;
            variables[modifier.GetValue(0)] = move.x.ToString();
            variables[modifier.GetValue(1)] = move.y.ToString();
        }

        public static void getMoveX(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            var player = reference is RTPlayer.RTCustomPlayerObject customPlayerObject ? customPlayerObject.Player.Core : reference as PAPlayer;
            if (!player)
                return;

            var move = player.RuntimePlayer.Actions.Move.Vector;
            if (move.magnitude > 1f && modifier.GetBool(1, true, variables))
                move = move.normalized;
            variables[modifier.GetValue(0)] = move.x.ToString();
        }

        public static void getMoveY(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            var player = reference is RTPlayer.RTCustomPlayerObject customPlayerObject ? customPlayerObject.Player.Core : reference as PAPlayer;
            if (!player)
                return;

            var move = player.RuntimePlayer.Actions.Move.Vector;
            if (move.magnitude > 1f && modifier.GetBool(1, true, variables))
                move = move.normalized;
            variables[modifier.GetValue(0)] = move.y.ToString();
        }

        #endregion

        #region DEVONLY

        public static void loadSceneDEVONLY(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (CoreHelper.InStory)
                SceneManager.inst.LoadScene(modifier.GetValue(0, variables), modifier.values.Count > 1 && modifier.GetBool(1, true, variables));
        }

        public static void loadStoryLevelDEVONLY(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (CoreHelper.InStory)
                Story.StoryManager.inst.Play(modifier.GetInt(1, 0, variables), modifier.GetInt(2, 0, variables), modifier.GetInt(4, 0, variables), modifier.GetBool(0, false, variables), modifier.GetBool(3, false, variables));
        }

        public static void storySaveBoolDEVONLY(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (CoreHelper.InStory)
                Story.StoryManager.inst.CurrentSave.SaveBool(modifier.GetValue(0, variables), modifier.GetBool(1, false, variables));
        }

        public static void storySaveIntDEVONLY(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (CoreHelper.InStory)
                Story.StoryManager.inst.CurrentSave.SaveInt(modifier.GetValue(0, variables), modifier.GetInt(1, 0, variables));
        }

        public static void storySaveFloatDEVONLY(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (CoreHelper.InStory)
                Story.StoryManager.inst.CurrentSave.SaveFloat(modifier.GetValue(0, variables), modifier.GetFloat(1, 0f, variables));
        }

        public static void storySaveStringDEVONLY(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (CoreHelper.InStory)
                Story.StoryManager.inst.CurrentSave.SaveString(modifier.GetValue(0, variables), modifier.GetValue(1, variables));
        }

        public static void storySaveIntVariableDEVONLY(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (CoreHelper.InStory && reference is IModifyable modifyable)
                Story.StoryManager.inst.CurrentSave.SaveInt(modifier.GetValue(0, variables), modifyable.IntVariable);
        }

        public static void getStorySaveBoolDEVONLY(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            variables[modifier.GetValue(0)] = !CoreHelper.InStory ? modifier.GetBool(2, false, variables).ToString() : Story.StoryManager.inst.CurrentSave.LoadBool(modifier.GetValue(1, variables), modifier.GetBool(2, false, variables)).ToString();
        }

        public static void getStorySaveIntDEVONLY(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            variables[modifier.GetValue(0)] = !CoreHelper.InStory ? modifier.GetInt(2, 0, variables).ToString() : Story.StoryManager.inst.CurrentSave.LoadInt(modifier.GetValue(1, variables), modifier.GetInt(2, 0, variables)).ToString();
        }

        public static void getStorySaveFloatDEVONLY(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            variables[modifier.GetValue(0)] = !CoreHelper.InStory ? modifier.GetFloat(2, 0f, variables).ToString() : Story.StoryManager.inst.CurrentSave.LoadFloat(modifier.GetValue(1, variables), modifier.GetFloat(2, 0f, variables)).ToString();
        }

        public static void getStorySaveStringDEVONLY(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            variables[modifier.GetValue(0)] = !CoreHelper.InStory ? modifier.GetValue(2, variables) : Story.StoryManager.inst.CurrentSave.LoadString(modifier.GetValue(1, variables), modifier.GetValue(2, variables)).ToString();
        }

        public static void exampleEnableDEVONLY(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (Companion.Entity.Example.Current && Companion.Entity.Example.Current.model)
                Companion.Entity.Example.Current.model.SetActive(modifier.GetBool(0, false, variables));
        }

        public static void exampleSayDEVONLY(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (Companion.Entity.Example.Current && Companion.Entity.Example.Current.chatBubble)
                Companion.Entity.Example.Current.chatBubble.Say(modifier.GetValue(0, variables));
        }

        #endregion

        #region Player

        public static bool playerCollide(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not BeatmapObject beatmapObject)
                return false;

            var runtimeObject = beatmapObject.runtimeObject;
            if (runtimeObject && runtimeObject.visualObject && runtimeObject.visualObject.collider)
            {
                var collider = runtimeObject.visualObject.collider;

                var players = PlayerManager.Players;
                for (int i = 0; i < players.Count; i++)
                {
                    var player = players[i];
                    if (!player.RuntimePlayer || !player.RuntimePlayer.CurrentCollider)
                        continue;

                    if (player.RuntimePlayer.CurrentCollider.IsTouching(collider))
                        return true;
                }
            }
            return false;
        }

        public static bool playerCollideIndex(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not BeatmapObject beatmapObject)
                return false;

            var runtimeObject = beatmapObject.runtimeObject;
            if (runtimeObject && runtimeObject.visualObject && runtimeObject.visualObject.collider)
            {
                var collider = runtimeObject.visualObject.collider;

                if (PlayerManager.Players.TryGetAt(modifier.GetInt(0, 0, variables), out PAPlayer player) && player.RuntimePlayer && player.RuntimePlayer.CurrentCollider)
                    return player.RuntimePlayer.CurrentCollider.IsTouching(collider);
            }
            return false;
        }

        public static bool playerCollideOther(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            var prefabable = reference.AsPrefabable();
            if (prefabable == null)
                return false;

            var tag = modifier.GetValue(0, variables);

            var cache = modifier.GetResultOrDefault(() => new GenericGroupCache<BeatmapObject>(tag, GameData.Current.FindObjectsWithTag(modifier, prefabable, tag)));
            if (cache.tag != tag)
                cache.UpdateCache(tag, GameData.Current.FindObjectsWithTag(modifier, prefabable, tag));

            for (int i = 0; i < cache.group.Count; i++)
            {
                var runtimeObject = cache.group[i]?.runtimeObject;
                if (!runtimeObject || !runtimeObject.visualObject || !runtimeObject.visualObject.collider)
                    continue;

                var collider = runtimeObject.visualObject.collider;

                var players = PlayerManager.Players;
                for (int j = 0; j < players.Count; j++)
                {
                    var player = players[j];
                    if (!player.RuntimePlayer || !player.RuntimePlayer.CurrentCollider)
                        continue;

                    if (player.RuntimePlayer.CurrentCollider.IsTouching(collider))
                        return true;
                }
            }

            return false;
        }

        public static bool playerCollideIndexOther(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            var prefabable = reference.AsPrefabable();
            if (prefabable == null)
                return false;

            var tag = modifier.GetValue(0, variables);
            var index = modifier.GetInt(1, 0, variables);

            var cache = modifier.GetResultOrDefault(() => new GenericGroupCache<BeatmapObject>(tag, GameData.Current.FindObjectsWithTag(modifier, prefabable, tag)));
            if (cache.tag != tag)
                cache.UpdateCache(tag, GameData.Current.FindObjectsWithTag(modifier, prefabable, tag));

            for (int i = 0; i < cache.group.Count; i++)
            {
                var runtimeObject = cache.group[i]?.runtimeObject;
                if (!runtimeObject || !runtimeObject.visualObject || !runtimeObject.visualObject.collider)
                    continue;

                var collider = runtimeObject.visualObject.collider;

                if (PlayerManager.Players.TryGetAt(index, out PAPlayer player) && player.RuntimePlayer && player.RuntimePlayer.CurrentCollider && player.RuntimePlayer.CurrentCollider.IsTouching(collider))
                    return true;
            }

            return false;
        }

        public static bool playerHealthEquals(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            var health = modifier.GetInt(0, 0, variables);
            return !PlayerManager.Players.IsEmpty() && PlayerManager.Players.Any(x => x.health == health);
        }

        public static bool playerHealthLesserEquals(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            var health = modifier.GetInt(0, 0, variables);
            return !PlayerManager.Players.IsEmpty() && PlayerManager.Players.Any(x => x.health <= health);
        }

        public static bool playerHealthGreaterEquals(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            var health = modifier.GetInt(0, 0, variables);
            return !PlayerManager.Players.IsEmpty() && PlayerManager.Players.Any(x => x.health >= health);
        }

        public static bool playerHealthLesser(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            var health = modifier.GetInt(0, 0, variables);
            return !PlayerManager.Players.IsEmpty() && PlayerManager.Players.Any(x => x.health < health);
        }

        public static bool playerHealthGreater(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            var health = modifier.GetInt(0, 0, variables);
            return !PlayerManager.Players.IsEmpty() && PlayerManager.Players.Any(x => x.health > health);
        }

        public static bool playerMoving(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            for (int i = 0; i < GameManager.inst.players.transform.childCount; i++)
            {
                if (GameManager.inst.players.transform.Find(string.Format("Player {0}", i + 1)))
                {
                    var player = GameManager.inst.players.transform.Find(string.Format("Player {0}/Player", i + 1));

                    if (!modifier.HasResult())
                        modifier.Result = player.position;

                    if (player.position != (Vector3)modifier.Result)
                    {
                        modifier.Result = player.position;
                        return true;
                    }
                }
            }

            return false;
        }

        public static bool playerBoosting(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not BeatmapObject beatmapObject)
                return false;

            var runtimeObject = beatmapObject.runtimeObject;
            if (runtimeObject && runtimeObject.visualObject && runtimeObject.visualObject.gameObject)
            {
                var player = PlayerManager.GetClosestPlayer(beatmapObject.GetFullPosition());

                if (player && player.RuntimePlayer)
                    return player.RuntimePlayer.isBoosting;
            }

            return false;
        }

        public static bool playerAlive(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            return PlayerManager.Players.TryGetAt(modifier.GetInt(0, 0, variables), out PAPlayer player) && player.RuntimePlayer && player.RuntimePlayer.Alive;
        }

        public static bool playerDeathsEquals(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            return RTBeatmap.Current.deaths.Count == modifier.GetInt(0, 0, variables);
        }

        public static bool playerDeathsLesserEquals(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            return RTBeatmap.Current.deaths.Count <= modifier.GetInt(0, 0, variables);
        }

        public static bool playerDeathsGreaterEquals(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            return RTBeatmap.Current.deaths.Count >= modifier.GetInt(0, 0, variables);
        }

        public static bool playerDeathsLesser(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            return RTBeatmap.Current.deaths.Count < modifier.GetInt(0, 0, variables);
        }

        public static bool playerDeathsGreater(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            return RTBeatmap.Current.deaths.Count > modifier.GetInt(0, 0, variables);
        }

        public static bool playerDistanceGreater(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not ITransformable transformable)
                return false;

            var pos = transformable.GetFullPosition();
            float num = modifier.GetFloat(0, 0f, variables);
            for (int i = 0; i < GameManager.inst.players.transform.childCount; i++)
            {
                if (GameManager.inst.players.transform.Find(string.Format("Player {0}", i + 1)))
                {
                    var player = GameManager.inst.players.transform.Find(string.Format("Player {0}/Player", i + 1));
                    if (Vector2.Distance(player.transform.position, pos) > num)
                        return true;
                }
            }

            return false;
        }

        public static bool playerDistanceLesser(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not ITransformable transformable)
                return false;

            var pos = transformable.GetFullPosition();
            float num = modifier.GetFloat(0, 0f, variables);
            for (int i = 0; i < GameManager.inst.players.transform.childCount; i++)
            {
                if (GameManager.inst.players.transform.Find(string.Format("Player {0}", i + 1)))
                {
                    var player = GameManager.inst.players.transform.Find(string.Format("Player {0}/Player", i + 1));
                    if (Vector2.Distance(player.transform.position, pos) < num)
                        return true;
                }
            }

            return false;
        }

        public static bool playerCountEquals(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            return PlayerManager.Players.Count == modifier.GetInt(0, 0, variables);
        }

        public static bool playerCountLesserEquals(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            return PlayerManager.Players.Count <= modifier.GetInt(0, 0, variables);
        }

        public static bool playerCountGreaterEquals(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            return PlayerManager.Players.Count >= modifier.GetInt(0, 0, variables);
        }

        public static bool playerCountLesser(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            return PlayerManager.Players.Count < modifier.GetInt(0, 0, variables);
        }

        public static bool playerCountGreater(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            return PlayerManager.Players.Count > modifier.GetInt(0, 0, variables);
        }

        public static bool onPlayerHit(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            return RTBeatmap.Current.playerHit;
        }

        public static bool onPlayerDeath(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            return RTBeatmap.Current.playerDied;
        }

        public static bool playerBoostEquals(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            return RTBeatmap.Current.boosts.Count == modifier.GetInt(0, 0, variables);
        }

        public static bool playerBoostLesserEquals(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            return RTBeatmap.Current.boosts.Count <= modifier.GetInt(0, 0, variables);
        }

        public static bool playerBoostGreaterEquals(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            return RTBeatmap.Current.boosts.Count >= modifier.GetInt(0, 0, variables);
        }

        public static bool playerBoostLesser(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            return RTBeatmap.Current.boosts.Count < modifier.GetInt(0, 0, variables);
        }

        public static bool playerBoostGreater(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            return RTBeatmap.Current.boosts.Count > modifier.GetInt(0, 0, variables);
        }

        #endregion

        #region Collide

        public static bool bulletCollide(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not BeatmapObject beatmapObject)
                return false;

            var runtimeObject = beatmapObject.runtimeObject;
            if (!runtimeObject || !runtimeObject.visualObject || !runtimeObject.visualObject.gameObject)
                return false;

            if (!beatmapObject.detector)
            {
                var op = runtimeObject.visualObject.gameObject.GetOrAddComponent<Detector>();
                op.beatmapObject = beatmapObject;
                beatmapObject.detector = op;
            }

            return beatmapObject.detector && beatmapObject.detector.bulletOver;
        }

        public static bool objectCollide(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not BeatmapObject beatmapObject)
                return false;

            var runtimeObject = beatmapObject.runtimeObject;
            if (!runtimeObject || !runtimeObject.visualObject || !runtimeObject.visualObject.collider)
                return false;

            var list = GameData.Current.FindObjectsWithTag(modifier, beatmapObject, modifier.GetValue(0)).FindAll(x => x.runtimeObject.visualObject && x.runtimeObject.visualObject.collider);
            return !list.IsEmpty() && list.Any(x => x.runtimeObject.visualObject.collider.IsTouching(runtimeObject.visualObject.collider));
        }

        #endregion

        #region Controls

        public static bool keyPressDown(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            return Input.GetKeyDown((KeyCode)modifier.GetInt(0, 0, variables));
        }

        public static bool keyPress(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            return Input.GetKey((KeyCode)modifier.GetInt(0, 0, variables));
        }

        public static bool keyPressUp(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            return Input.GetKeyUp((KeyCode)modifier.GetInt(0, 0, variables));
        }

        public static bool mouseButtonDown(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            return Input.GetMouseButtonDown(modifier.GetInt(0, 0, variables));
        }

        public static bool mouseButton(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            return Input.GetMouseButton(modifier.GetInt(0, 0, variables));
        }

        public static bool mouseButtonUp(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            return Input.GetMouseButtonUp(modifier.GetInt(0, 0, variables));
        }

        public static bool mouseOver(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is BeatmapObject beatmapObject && beatmapObject.runtimeObject && beatmapObject.runtimeObject.visualObject && beatmapObject.runtimeObject.visualObject.gameObject)
            {
                if (!beatmapObject.detector)
                {
                    var gameObject = beatmapObject.runtimeObject.visualObject.gameObject;
                    var op = gameObject.GetOrAddComponent<Detector>();
                    op.beatmapObject = beatmapObject;
                    beatmapObject.detector = op;
                }

                return beatmapObject.detector && beatmapObject.detector.hovered;
            }

            return false;
        }

        public static bool mouseOverSignalModifier(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not BeatmapObject beatmapObject)
                return false;

            var delay = modifier.GetFloat(0, 0f, variables);
            var list = GameData.Current.FindObjectsWithTag(modifier, beatmapObject, modifier.GetValue(1, variables));
            if (beatmapObject.runtimeObject && beatmapObject.runtimeObject.visualObject && beatmapObject.runtimeObject.visualObject.gameObject)
            {
                if (!beatmapObject.detector)
                {
                    var gameObject = beatmapObject.runtimeObject.visualObject.gameObject;
                    var op = gameObject.GetOrAddComponent<Detector>();
                    op.beatmapObject = beatmapObject;
                    beatmapObject.detector = op;
                }

                if (beatmapObject.detector)
                {
                    if (beatmapObject.detector.hovered && !list.IsEmpty())
                    {
                        foreach (var bm in list)
                            CoroutineHelper.StartCoroutine(ModifiersHelper.ActivateModifier(bm, delay));
                    }

                    if (beatmapObject.detector.hovered)
                        return true;
                }
            }

            return false;
        }

        public static bool controlPressDown(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            var type = modifier.GetInt(0, 0, variables);

            var transformable = reference.AsTransformable();
            var player = reference is PAPlayer p ? p : PlayerManager.GetClosestPlayer(transformable?.GetFullPosition() ?? Vector3.zero);
            var device = player?.device ?? InControl.InputManager.ActiveDevice;

            if (device == null)
                return false;

            return Enum.TryParse(((PlayerInputControlType)type).ToString(), out InControl.InputControlType inputControlType) && device.GetControl(inputControlType).WasPressed;
        }

        public static bool controlPress(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            var type = modifier.GetInt(0, 0, variables);

            var transformable = reference.AsTransformable();
            var player = reference is PAPlayer p ? p : PlayerManager.GetClosestPlayer(transformable?.GetFullPosition() ?? Vector3.zero);
            var device = player?.device ?? InControl.InputManager.ActiveDevice;

            if (device == null)
                return false;

            return Enum.TryParse(((PlayerInputControlType)type).ToString(), out InControl.InputControlType inputControlType) && device.GetControl(inputControlType).IsPressed;
        }

        public static bool controlPressUp(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            var type = modifier.GetInt(0, 0, variables);

            var transformable = reference.AsTransformable();
            var player = reference is PAPlayer p ? p : PlayerManager.GetClosestPlayer(transformable?.GetFullPosition() ?? Vector3.zero);
            var device = player?.device ?? InControl.InputManager.ActiveDevice;

            if (device == null)
                return false;

            return Enum.TryParse(((PlayerInputControlType)type).ToString(), out InControl.InputControlType inputControlType) && device.GetControl(inputControlType).WasReleased;
        }

        #endregion

        #region JSON

        public static bool loadEquals(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (RTFile.TryReadFromFile(ModifiersHelper.GetSaveFile(modifier.GetValue(1, variables)), out string json))
            {
                var type = modifier.GetInt(4, 0, variables);

                var jn = JSON.Parse(json);
                var jsonName1 = modifier.GetValue(2, variables);
                var jsonName2 = modifier.GetValue(3, variables);

                return
                    jn[jsonName1][jsonName2]["float"] != null &&
                        (type == 0 ?
                            float.TryParse(jn[jsonName1][jsonName2]["float"], out float eq) && eq == modifier.GetFloat(0, 0f, variables) :
                            jn[jsonName1][jsonName2]["string"] == modifier.GetValue(0, variables));
            }

            return false;
        }

        public static bool loadLesserEquals(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (RTFile.TryReadFromFile(ModifiersHelper.GetSaveFile(modifier.GetValue(1, variables)), out string json))
            {
                var jn = JSON.Parse(json);

                var fjn = jn[modifier.GetValue(2, variables)][modifier.GetValue(3, variables)]["float"];

                return !string.IsNullOrEmpty(fjn) && fjn.AsFloat <= modifier.GetFloat(0, 0f, variables);
            }

            return false;
        }

        public static bool loadGreaterEquals(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (RTFile.TryReadFromFile(ModifiersHelper.GetSaveFile(modifier.GetValue(1, variables)), out string json))
            {
                var jn = JSON.Parse(json);

                var fjn = jn[modifier.GetValue(2, variables)][modifier.GetValue(3, variables)]["float"];

                return !string.IsNullOrEmpty(fjn) && fjn.AsFloat >= modifier.GetFloat(0, 0f, variables);
            }

            return false;
        }

        public static bool loadLesser(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (RTFile.TryReadFromFile(ModifiersHelper.GetSaveFile(modifier.GetValue(1, variables)), out string json))
            {
                var jn = JSON.Parse(json);

                var fjn = jn[modifier.GetValue(2, variables)][modifier.GetValue(3, variables)]["float"];

                return !string.IsNullOrEmpty(fjn) && fjn.AsFloat < modifier.GetFloat(0, 0f, variables);
            }

            return false;
        }

        public static bool loadGreater(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (RTFile.TryReadFromFile(ModifiersHelper.GetSaveFile(modifier.GetValue(1, variables)), out string json))
            {
                var jn = JSON.Parse(json);

                var fjn = jn[modifier.GetValue(2, variables)][modifier.GetValue(3, variables)]["float"];

                return !string.IsNullOrEmpty(fjn) && fjn.AsFloat > modifier.GetFloat(0, 0f, variables);
            }

            return false;
        }

        public static bool loadExists(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            return RTFile.TryReadFromFile(ModifiersHelper.GetSaveFile(modifier.GetValue(1, variables)), out string json) && !string.IsNullOrEmpty(JSON.Parse(json)[modifier.GetValue(2, variables)][modifier.GetValue(3, variables)]);
        }

        #endregion

        #region Variable

        public static bool localVariableEquals(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            return variables.TryGetValue(modifier.GetValue(0), out string result) && result == modifier.GetValue(1, variables);
        }

        public static bool localVariableLesserEquals(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            return variables.TryGetValue(modifier.GetValue(0), out string result) && (float.TryParse(result, out float num) ? num : Parser.TryParse(result, 0)) <= modifier.GetFloat(1, 0f, variables);
        }

        public static bool localVariableGreaterEquals(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            return variables.TryGetValue(modifier.GetValue(0), out string result) && (float.TryParse(result, out float num) ? num : Parser.TryParse(result, 0)) >= modifier.GetFloat(1, 0f, variables);
        }

        public static bool localVariableLesser(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            return variables.TryGetValue(modifier.GetValue(0), out string result) && (float.TryParse(result, out float num) ? num : Parser.TryParse(result, 0)) < modifier.GetFloat(1, 0f, variables);
        }

        public static bool localVariableGreater(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            return variables.TryGetValue(modifier.GetValue(0), out string result) && (float.TryParse(result, out float num) ? num : Parser.TryParse(result, 0)) > modifier.GetFloat(1, 0f, variables);
        }

        public static bool localVariableContains(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            return variables.TryGetValue(modifier.GetValue(0), out string result) && result.Contains(modifier.GetValue(1, variables));
        }

        public static bool localVariableStartsWith(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            return variables.TryGetValue(modifier.GetValue(0), out string result) && result.StartsWith(modifier.GetValue(1, variables));
        }

        public static bool localVariableEndsWith(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            return variables.TryGetValue(modifier.GetValue(0), out string result) && result.EndsWith(modifier.GetValue(1, variables));
        }

        public static bool localVariableExists(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            return variables.ContainsKey(modifier.GetValue(0));
        }

        public static bool variableEquals(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            return reference is IModifyable modifyable && modifyable.IntVariable == modifier.GetInt(0, 0, variables);
        }

        public static bool variableLesserEquals(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            return reference is IModifyable modifyable && modifyable.IntVariable <= modifier.GetInt(0, 0, variables);
        }

        public static bool variableGreaterEquals(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            return reference is IModifyable modifyable && modifyable.IntVariable >= modifier.GetInt(0, 0, variables);
        }

        public static bool variableLesser(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            return reference is IModifyable modifyable && modifyable.IntVariable < modifier.GetInt(0, 0, variables);
        }

        public static bool variableGreater(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            return reference is IModifyable modifyable && modifyable.IntVariable > modifier.GetInt(0, 0, variables);
        }

        public static bool variableOtherEquals(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not IPrefabable prefabable)
                return false;

            var beatmapObjects = GameData.Current.FindObjectsWithTag(modifier, prefabable, modifier.GetValue(1, variables));

            int num = modifier.GetInt(0, 0, variables);

            return !beatmapObjects.IsEmpty() && beatmapObjects.Any(x => x.integerVariable == num);
        }

        public static bool variableOtherLesserEquals(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not IPrefabable prefabable)
                return false;

            var beatmapObjects = GameData.Current.FindObjectsWithTag(modifier, prefabable, modifier.GetValue(1, variables));

            int num = modifier.GetInt(0, 0, variables);

            return !beatmapObjects.IsEmpty() && beatmapObjects.Any(x => x.integerVariable <= num);
        }

        public static bool variableOtherGreaterEquals(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not IPrefabable prefabable)
                return false;

            var beatmapObjects = GameData.Current.FindObjectsWithTag(modifier, prefabable, modifier.GetValue(1, variables));

            int num = modifier.GetInt(0, 0, variables);

            return !beatmapObjects.IsEmpty() && beatmapObjects.Any(x => x.integerVariable >= num);
        }

        public static bool variableOtherLesser(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not IPrefabable prefabable)
                return false;

            var beatmapObjects = GameData.Current.FindObjectsWithTag(modifier, prefabable, modifier.GetValue(1, variables));

            int num = modifier.GetInt(0, 0, variables);

            return !beatmapObjects.IsEmpty() && beatmapObjects.Any(x => x.integerVariable < num);
        }

        public static bool variableOtherGreater(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not IPrefabable prefabable)
                return false;

            var beatmapObjects = GameData.Current.FindObjectsWithTag(modifier, prefabable, modifier.GetValue(1, variables));

            int num = modifier.GetInt(0, 0, variables);

            return !beatmapObjects.IsEmpty() && beatmapObjects.Any(x => x.integerVariable > num);
        }

        #endregion

        #region Audio

        public static bool pitchEquals(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            return AudioManager.inst.pitch == modifier.GetFloat(0, 0f, variables);
        }

        public static bool pitchLesserEquals(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            return AudioManager.inst.pitch <= modifier.GetFloat(0, 0f, variables);
        }

        public static bool pitchGreaterEquals(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            return AudioManager.inst.pitch >= modifier.GetFloat(0, 0f, variables);
        }

        public static bool pitchLesser(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            return AudioManager.inst.pitch < modifier.GetFloat(0, 0f, variables);
        }

        public static bool pitchGreater(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            return AudioManager.inst.pitch > modifier.GetFloat(0, 0f, variables);
        }

        public static bool musicTimeGreater(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            return AudioManager.inst.CurrentAudioSource.time - (modifier.GetBool(1, false, variables) && reference is ILifetime lifetime ? lifetime.StartTime : 0f) > modifier.GetFloat(0, 0f, variables);
        }

        public static bool musicTimeLesser(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            return AudioManager.inst.CurrentAudioSource.time - (modifier.GetBool(1, false, variables) && reference is ILifetime lifetime ? lifetime.StartTime : 0f) < modifier.GetFloat(0, 0f, variables);
        }

        public static bool musicTimeInRange(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            var time = reference.GetParentRuntime().FixedTime;
            return modifier.values.Count > 2 && time >= modifier.GetFloat(1, 0f, variables) - 0.01f && time <= modifier.GetFloat(2, 0f, variables) + 0.1f;
        }

        public static bool musicPlaying(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            return AudioManager.inst.CurrentAudioSource.isPlaying;
        }

        #endregion

        #region Game State

        public static bool inZenMode(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            return RTBeatmap.Current.Invincible;
        }

        public static bool inNormal(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            return RTBeatmap.Current.IsNormal;
        }

        public static bool in1Life(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            return RTBeatmap.Current.Is1Life;
        }

        public static bool inNoHit(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            return RTBeatmap.Current.IsNoHit;
        }

        public static bool inPractice(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            return RTBeatmap.Current.IsPractice;
        }

        #endregion

        #region Random

        public static bool randomEquals(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (!modifier.HasResult())
            {
                if (reference is PAObjectBase obj)
                    modifier.Result = RandomHelper.FromIDRange(RandomHelper.CurrentSeed, obj.id, modifier.GetInt(1, 0, variables), modifier.GetInt(2, 0, variables)) == modifier.GetInt(0, 0, variables);
                else
                    modifier.Result = UnityEngine.Random.Range(modifier.GetInt(1, 0, variables), modifier.GetInt(2, 0, variables)) == modifier.GetInt(0, 0, variables);
            }

            return modifier.HasResult() && modifier.GetResult<bool>();
        }

        public static bool randomLesser(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (!modifier.HasResult())
            {
                if (reference is PAObjectBase obj)
                    modifier.Result = RandomHelper.FromIDRange(RandomHelper.CurrentSeed, obj.id, modifier.GetInt(1, 0, variables), modifier.GetInt(2, 0, variables)) < modifier.GetInt(0, 0, variables);
                else
                    modifier.Result = UnityEngine.Random.Range(modifier.GetInt(1, 0, variables), modifier.GetInt(2, 0, variables)) < modifier.GetInt(0, 0, variables);
            }

            return modifier.HasResult() && modifier.GetResult<bool>();
        }

        public static bool randomGreater(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (!modifier.HasResult())
            {
                if (reference is PAObjectBase obj)
                    modifier.Result = RandomHelper.FromIDRange(RandomHelper.CurrentSeed, obj.id, modifier.GetInt(1, 0, variables), modifier.GetInt(2, 0, variables)) > modifier.GetInt(0, 0, variables);
                else
                    modifier.Result = UnityEngine.Random.Range(modifier.GetInt(1, 0, variables), modifier.GetInt(2, 0, variables)) > modifier.GetInt(0, 0, variables);
            }

            return modifier.HasResult() && modifier.GetResult<bool>();
        }

        #endregion

        #region Math

        public static bool mathEquals(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not IEvaluatable evaluatable)
                return false;

            var numberVariables = evaluatable.GetObjectVariables();
            ModifiersHelper.SetVariables(variables, numberVariables);
            var functions = evaluatable.GetObjectFunctions();

            return RTMath.Parse(modifier.GetValue(0, variables), numberVariables, functions) == RTMath.Parse(modifier.GetValue(1, variables), numberVariables, functions);
        }

        public static bool mathLesserEquals(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not IEvaluatable evaluatable)
                return false;

            var numberVariables = evaluatable.GetObjectVariables();
            ModifiersHelper.SetVariables(variables, numberVariables);
            var functions = evaluatable.GetObjectFunctions();

            return RTMath.Parse(modifier.GetValue(0, variables), numberVariables, functions) <= RTMath.Parse(modifier.GetValue(1, variables), numberVariables, functions);
        }

        public static bool mathGreaterEquals(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not IEvaluatable evaluatable)
                return false;

            var numberVariables = evaluatable.GetObjectVariables();
            ModifiersHelper.SetVariables(variables, numberVariables);
            var functions = evaluatable.GetObjectFunctions();

            return RTMath.Parse(modifier.GetValue(0, variables), numberVariables, functions) >= RTMath.Parse(modifier.GetValue(1, variables), numberVariables, functions);
        }

        public static bool mathLesser(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not IEvaluatable evaluatable)
                return false;

            var numberVariables = evaluatable.GetObjectVariables();
            ModifiersHelper.SetVariables(variables, numberVariables);
            var functions = evaluatable.GetObjectFunctions();

            return RTMath.Parse(modifier.GetValue(0, variables), numberVariables, functions) < RTMath.Parse(modifier.GetValue(1, variables), numberVariables, functions);
        }

        public static bool mathGreater(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not IEvaluatable evaluatable)
                return false;

            var numberVariables = evaluatable.GetObjectVariables();
            ModifiersHelper.SetVariables(variables, numberVariables);
            var functions = evaluatable.GetObjectFunctions();

            return RTMath.Parse(modifier.GetValue(0, variables), numberVariables, functions) > RTMath.Parse(modifier.GetValue(1, variables), numberVariables, functions);
        }

        #endregion

        #region Animation

        public static bool axisEquals(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            var prefabable = reference.AsPrefabable();
            if (prefabable == null)
                return false;

            var tag = modifier.GetValue(0, variables);

            int fromType = modifier.GetInt(1, 0, variables);
            int fromAxis = modifier.GetInt(2, 0, variables);

            float delay = modifier.GetFloat(3, 0f, variables);
            float multiply = modifier.GetFloat(4, 0f, variables);
            float offset = modifier.GetFloat(5, 0f, variables);
            float min = modifier.GetFloat(6, -9999f, variables);
            float max = modifier.GetFloat(7, 9999f, variables);
            float equals = modifier.GetFloat(8, 0f, variables);
            bool useVisual = modifier.GetBool(9, false, variables);
            float loop = modifier.GetFloat(10, 9999f, variables);

            var cache = modifier.GetResultOrDefault(() => GroupBeatmapObjectCache.Get(modifier, prefabable, tag));
            if (cache.tag != tag)
            {
                cache.UpdateCache(modifier, prefabable, tag);
                modifier.Result = cache;
            }
            var beatmapObject = cache.obj;
            if (!beatmapObject)
                return false;

            fromType = Mathf.Clamp(fromType, 0, beatmapObject.events.Count);
            if (!useVisual)
                fromAxis = Mathf.Clamp(fromAxis, 0, beatmapObject.events[fromType][0].values.Length);

            return fromType >= 0 && fromType <= 2 && ModifiersHelper.GetAnimation(beatmapObject, fromType, fromAxis, min, max, offset, multiply, delay, loop, useVisual) == equals;
        }

        public static bool axisLesserEquals(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            var prefabable = reference.AsPrefabable();
            if (prefabable == null)
                return false;

            var tag = modifier.GetValue(0, variables);

            int fromType = modifier.GetInt(1, 0, variables);
            int fromAxis = modifier.GetInt(2, 0, variables);

            float delay = modifier.GetFloat(3, 0f, variables);
            float multiply = modifier.GetFloat(4, 0f, variables);
            float offset = modifier.GetFloat(5, 0f, variables);
            float min = modifier.GetFloat(6, -9999f, variables);
            float max = modifier.GetFloat(7, 9999f, variables);
            float equals = modifier.GetFloat(8, 0f, variables);
            bool useVisual = modifier.GetBool(9, false, variables);
            float loop = modifier.GetFloat(10, 9999f, variables);

            var cache = modifier.GetResultOrDefault(() => GroupBeatmapObjectCache.Get(modifier, prefabable, tag));
            if (cache.tag != tag)
            {
                cache.UpdateCache(modifier, prefabable, tag);
                modifier.Result = cache;
            }
            var beatmapObject = cache.obj;
            if (!beatmapObject)
                return false;

            fromType = Mathf.Clamp(fromType, 0, beatmapObject.events.Count);
            if (!useVisual)
                fromAxis = Mathf.Clamp(fromAxis, 0, beatmapObject.events[fromType][0].values.Length);

            return fromType >= 0 && fromType <= 2 && ModifiersHelper.GetAnimation(beatmapObject, fromType, fromAxis, min, max, offset, multiply, delay, loop, useVisual) <= equals;
        }

        public static bool axisGreaterEquals(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            var prefabable = reference.AsPrefabable();
            if (prefabable == null)
                return false;

            var tag = modifier.GetValue(0, variables);

            int fromType = modifier.GetInt(1, 0, variables);
            int fromAxis = modifier.GetInt(2, 0, variables);

            float delay = modifier.GetFloat(3, 0f, variables);
            float multiply = modifier.GetFloat(4, 0f, variables);
            float offset = modifier.GetFloat(5, 0f, variables);
            float min = modifier.GetFloat(6, -9999f, variables);
            float max = modifier.GetFloat(7, 9999f, variables);
            float equals = modifier.GetFloat(8, 0f, variables);
            bool useVisual = modifier.GetBool(9, false, variables);
            float loop = modifier.GetFloat(10, 9999f, variables);

            var cache = modifier.GetResultOrDefault(() => GroupBeatmapObjectCache.Get(modifier, prefabable, tag));
            if (cache.tag != tag)
            {
                cache.UpdateCache(modifier, prefabable, tag);
                modifier.Result = cache;
            }
            var beatmapObject = cache.obj;
            if (!beatmapObject)
                return false;

            fromType = Mathf.Clamp(fromType, 0, beatmapObject.events.Count);
            if (!useVisual)
                fromAxis = Mathf.Clamp(fromAxis, 0, beatmapObject.events[fromType][0].values.Length);

            return fromType >= 0 && fromType <= 2 && ModifiersHelper.GetAnimation(beatmapObject, fromType, fromAxis, min, max, offset, multiply, delay, loop, useVisual) >= equals;
        }

        public static bool axisLesser(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            var prefabable = reference.AsPrefabable();
            if (prefabable == null)
                return false;

            var tag = modifier.GetValue(0, variables);

            int fromType = modifier.GetInt(1, 0, variables);
            int fromAxis = modifier.GetInt(2, 0, variables);

            float delay = modifier.GetFloat(3, 0f, variables);
            float multiply = modifier.GetFloat(4, 0f, variables);
            float offset = modifier.GetFloat(5, 0f, variables);
            float min = modifier.GetFloat(6, -9999f, variables);
            float max = modifier.GetFloat(7, 9999f, variables);
            float equals = modifier.GetFloat(8, 0f, variables);
            bool useVisual = modifier.GetBool(9, false, variables);
            float loop = modifier.GetFloat(10, 9999f, variables);

            var cache = modifier.GetResultOrDefault(() => GroupBeatmapObjectCache.Get(modifier, prefabable, tag));
            if (cache.tag != tag)
            {
                cache.UpdateCache(modifier, prefabable, tag);
                modifier.Result = cache;
            }
            var beatmapObject = cache.obj;
            if (!beatmapObject)
                return false;

            fromType = Mathf.Clamp(fromType, 0, beatmapObject.events.Count);
            if (!useVisual)
                fromAxis = Mathf.Clamp(fromAxis, 0, beatmapObject.events[fromType][0].values.Length);

            return fromType >= 0 && fromType <= 2 && ModifiersHelper.GetAnimation(beatmapObject, fromType, fromAxis, min, max, offset, multiply, delay, loop, useVisual) < equals;
        }

        public static bool axisGreater(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            var prefabable = reference.AsPrefabable();
            if (prefabable == null)
                return false;

            var tag = modifier.GetValue(0, variables);

            int fromType = modifier.GetInt(1, 0, variables);
            int fromAxis = modifier.GetInt(2, 0, variables);

            float delay = modifier.GetFloat(3, 0f, variables);
            float multiply = modifier.GetFloat(4, 0f, variables);
            float offset = modifier.GetFloat(5, 0f, variables);
            float min = modifier.GetFloat(6, -9999f, variables);
            float max = modifier.GetFloat(7, 9999f, variables);
            float equals = modifier.GetFloat(8, 0f, variables);
            bool useVisual = modifier.GetBool(9, false, variables);
            float loop = modifier.GetFloat(10, 9999f, variables);

            var cache = modifier.GetResultOrDefault(() => GroupBeatmapObjectCache.Get(modifier, prefabable, tag));
            if (cache.tag != tag)
            {
                cache.UpdateCache(modifier, prefabable, tag);
                modifier.Result = cache;
            }
            var beatmapObject = cache.obj;
            if (!beatmapObject)
                return false;

            fromType = Mathf.Clamp(fromType, 0, beatmapObject.events.Count);
            if (!useVisual)
                fromAxis = Mathf.Clamp(fromAxis, 0, beatmapObject.events[fromType][0].values.Length);

            return fromType >= 0 && fromType <= 2 && ModifiersHelper.GetAnimation(beatmapObject, fromType, fromAxis, min, max, offset, multiply, delay, loop, useVisual) > equals;
        }

        public static bool eventEquals(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            var str = modifier.GetValue(0, variables);
            var time = Parser.TryParse(str, RTLevel.Current.FixedTime);

            return RTLevel.Current && RTLevel.Current.eventEngine && RTLevel.Current.eventEngine.Interpolate(modifier.GetInt(1, 0, variables), modifier.GetInt(2, 0, variables), time) == modifier.GetFloat(3, 0f, variables);
        }

        public static bool eventLesserEquals(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            var str = modifier.GetValue(0, variables);
            var time = Parser.TryParse(str, RTLevel.Current.FixedTime);

            return RTLevel.Current && RTLevel.Current.eventEngine && RTLevel.Current.eventEngine.Interpolate(modifier.GetInt(1, 0, variables), modifier.GetInt(2, 0, variables), time) <= modifier.GetFloat(3, 0f, variables);
        }

        public static bool eventGreaterEquals(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            var str = modifier.GetValue(0, variables);
            var time = Parser.TryParse(str, RTLevel.Current.FixedTime);

            return RTLevel.Current && RTLevel.Current.eventEngine && RTLevel.Current.eventEngine.Interpolate(modifier.GetInt(1, 0, variables), modifier.GetInt(2, 0, variables), time) >= modifier.GetFloat(3, 0f, variables);
        }

        public static bool eventLesser(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            var str = modifier.GetValue(0, variables);
            var time = Parser.TryParse(str, RTLevel.Current.FixedTime);

            return RTLevel.Current && RTLevel.Current.eventEngine && RTLevel.Current.eventEngine.Interpolate(modifier.GetInt(1, 0, variables), modifier.GetInt(2, 0, variables), time) < modifier.GetFloat(3, 0f, variables);
        }

        public static bool eventGreater(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            var str = modifier.GetValue(0, variables);
            var time = Parser.TryParse(str, RTLevel.Current.FixedTime);

            return RTLevel.Current && RTLevel.Current.eventEngine && RTLevel.Current.eventEngine.Interpolate(modifier.GetInt(1, 0, variables), modifier.GetInt(2, 0, variables), time) > modifier.GetFloat(3, 0f, variables);
        }

        #endregion

        #region Level

        public static bool levelRankEquals(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            return ModifiersHelper.GetLevelRank(LevelManager.CurrentLevel, out int levelRankIndex) && levelRankIndex == modifier.GetInt(0, 0, variables);
        }

        public static bool levelRankLesserEquals(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            return ModifiersHelper.GetLevelRank(LevelManager.CurrentLevel, out int levelRankIndex) && levelRankIndex <= modifier.GetInt(0, 0, variables);
        }

        public static bool levelRankGreaterEquals(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            return ModifiersHelper.GetLevelRank(LevelManager.CurrentLevel, out int levelRankIndex) && levelRankIndex >= modifier.GetInt(0, 0, variables);
        }

        public static bool levelRankLesser(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            return ModifiersHelper.GetLevelRank(LevelManager.CurrentLevel, out int levelRankIndex) && levelRankIndex < modifier.GetInt(0, 0, variables);
        }

        public static bool levelRankGreater(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            return ModifiersHelper.GetLevelRank(LevelManager.CurrentLevel, out int levelRankIndex) && levelRankIndex > modifier.GetInt(0, 0, variables);
        }

        public static bool levelRankOtherEquals(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            var id = modifier.GetValue(1, variables);
            return LevelManager.Levels.TryFind(x => x.id == id, out Level level) && ModifiersHelper.GetLevelRank(level, out int levelRankIndex) && levelRankIndex == modifier.GetInt(0, 0, variables);
        }

        public static bool levelRankOtherLesserEquals(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            var id = modifier.GetValue(1, variables);
            return LevelManager.Levels.TryFind(x => x.id == id, out Level level) && ModifiersHelper.GetLevelRank(level, out int levelRankIndex) && levelRankIndex <= modifier.GetInt(0, 0, variables);
        }

        public static bool levelRankOtherGreaterEquals(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            var id = modifier.GetValue(1, variables);
            return LevelManager.Levels.TryFind(x => x.id == id, out Level level) && ModifiersHelper.GetLevelRank(level, out int levelRankIndex) && levelRankIndex >= modifier.GetInt(0, 0, variables);
        }

        public static bool levelRankOtherLesser(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            var id = modifier.GetValue(1, variables);
            return LevelManager.Levels.TryFind(x => x.id == id, out Level level) && ModifiersHelper.GetLevelRank(level, out int levelRankIndex) && levelRankIndex < modifier.GetInt(0, 0, variables);
        }

        public static bool levelRankOtherGreater(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            var id = modifier.GetValue(1, variables);
            return LevelManager.Levels.TryFind(x => x.id == id, out Level level) && ModifiersHelper.GetLevelRank(level, out int levelRankIndex) && levelRankIndex > modifier.GetInt(0, 0, variables);
        }

        public static bool levelRankCurrentEquals(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            return LevelManager.GetLevelRank(RTBeatmap.Current.hits) == modifier.GetInt(0, 0, variables);
        }

        public static bool levelRankCurrentLesserEquals(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            return LevelManager.GetLevelRank(RTBeatmap.Current.hits) <= modifier.GetInt(0, 0, variables);
        }

        public static bool levelRankCurrentGreaterEquals(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            return LevelManager.GetLevelRank(RTBeatmap.Current.hits) >= modifier.GetInt(0, 0, variables);
        }

        public static bool levelRankCurrentLesser(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            return LevelManager.GetLevelRank(RTBeatmap.Current.hits) < modifier.GetInt(0, 0, variables);
        }

        public static bool levelRankCurrentGreater(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            return LevelManager.GetLevelRank(RTBeatmap.Current.hits) > modifier.GetInt(0, 0, variables);
        }

        public static bool onLevelStart(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables) => RTBeatmap.Current && RTBeatmap.Current.LevelStarted;

        public static bool onLevelRestart(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables) => false;

        public static bool onLevelRewind(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables) => CoreHelper.Reversing;

        public static bool levelUnlocked(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            var id = modifier.GetValue(0, variables);
            return LevelManager.Levels.TryFind(x => x.id == id, out Level level) && !level.Locked;
        }

        public static bool levelCompleted(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            return CoreHelper.InEditor || LevelManager.CurrentLevel && LevelManager.CurrentLevel.saveData && LevelManager.CurrentLevel.saveData.Completed;
        }

        public static bool levelCompletedOther(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            var id = modifier.GetValue(0, variables);
            return CoreHelper.InEditor || LevelManager.Levels.TryFind(x => x.id == id, out Level level) && level.saveData && level.saveData.Completed;
        }

        public static bool levelExists(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            var id = modifier.GetValue(0, variables);
            return LevelManager.Levels.Has(x => x.id == id);
        }

        public static bool levelPathExists(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            var basePath = RTFile.CombinePaths(RTFile.ApplicationDirectory, LevelManager.ListSlash, modifier.GetValue(0, variables));

            return
                RTFile.FileExists(RTFile.CombinePaths(basePath, Level.LEVEL_LSB)) ||
                RTFile.FileExists(RTFile.CombinePaths(basePath, Level.LEVEL_VGD)) ||
                RTFile.FileExists(basePath + FileFormat.ASSET.Dot());
        }

        public static bool achievementUnlocked(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            // global or local
            return modifier.GetBool(1, false, variables) ?
                AchievementManager.unlockedCustomAchievements.TryGetValue(modifier.GetValue(0, variables), out bool global) && global :
                LevelManager.CurrentLevel && LevelManager.CurrentLevel.saveData && LevelManager.CurrentLevel.saveData.AchievementUnlocked(modifier.GetValue(0, variables));
        }

        #endregion

        #region Real Time

        public static bool realTimeEquals(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            return DateTime.Now.ToString(modifier.GetValue(0, variables)) == modifier.GetValue(1, variables);
        }

        public static bool realTimeLesserEquals(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            var dateTime = DateTime.Now;

            var type = modifier.GetInt(0, 0, variables);
            return type switch
            {
                0 => dateTime.Millisecond <= modifier.GetInt(1, 0, variables),
                1 => dateTime.Second <= modifier.GetInt(1, 0, variables),
                2 => dateTime.Minute <= modifier.GetInt(1, 0, variables),
                3 => dateTime.Hour % 12 <= modifier.GetInt(1, 0, variables),
                4 => dateTime.Hour <= modifier.GetInt(1, 0, variables),
                5 => dateTime.Day <= modifier.GetInt(1, 0, variables),
                6 => dateTime.Month <= modifier.GetInt(1, 0, variables),
                7 => dateTime.Year <= modifier.GetInt(1, 0, variables),
                8 => dateTime.DayOfWeek <= (DayOfWeek)modifier.GetInt(1, 0, variables),
                9 => dateTime.DayOfYear <= modifier.GetInt(1, 0, variables),
                10 => dateTime.Ticks <= modifier.GetInt(1, 0, variables),
                _ => false,
            };
        }

        public static bool realTimeGreaterEquals(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            var dateTime = DateTime.Now;

            var type = modifier.GetInt(0, 0, variables);
            return type switch
            {
                0 => dateTime.Millisecond >= modifier.GetInt(1, 0, variables),
                1 => dateTime.Second >= modifier.GetInt(1, 0, variables),
                2 => dateTime.Minute >= modifier.GetInt(1, 0, variables),
                3 => dateTime.Hour % 12 >= modifier.GetInt(1, 0, variables),
                4 => dateTime.Hour >= modifier.GetInt(1, 0, variables),
                5 => dateTime.Day >= modifier.GetInt(1, 0, variables),
                6 => dateTime.Month >= modifier.GetInt(1, 0, variables),
                7 => dateTime.Year >= modifier.GetInt(1, 0, variables),
                8 => dateTime.DayOfWeek >= (DayOfWeek)modifier.GetInt(1, 0, variables),
                9 => dateTime.DayOfYear >= modifier.GetInt(1, 0, variables),
                10 => dateTime.Ticks >= modifier.GetInt(1, 0, variables),
                _ => false,
            };
        }

        public static bool realTimeLesser(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            var dateTime = DateTime.Now;

            var type = modifier.GetInt(0, 0, variables);
            return type switch
            {
                0 => dateTime.Millisecond < modifier.GetInt(1, 0, variables),
                1 => dateTime.Second < modifier.GetInt(1, 0, variables),
                2 => dateTime.Minute < modifier.GetInt(1, 0, variables),
                3 => dateTime.Hour % 12 < modifier.GetInt(1, 0, variables),
                4 => dateTime.Hour < modifier.GetInt(1, 0, variables),
                5 => dateTime.Day < modifier.GetInt(1, 0, variables),
                6 => dateTime.Month < modifier.GetInt(1, 0, variables),
                7 => dateTime.Year < modifier.GetInt(1, 0, variables),
                8 => dateTime.DayOfWeek < (DayOfWeek)modifier.GetInt(1, 0, variables),
                9 => dateTime.DayOfYear < modifier.GetInt(1, 0, variables),
                10 => dateTime.Ticks < modifier.GetInt(1, 0, variables),
                _ => false,
            };
        }

        public static bool realTimeGreater(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            var dateTime = DateTime.Now;

            var type = modifier.GetInt(0, 0, variables);
            return type switch
            {
                0 => dateTime.Millisecond > modifier.GetInt(1, 0, variables),
                1 => dateTime.Second > modifier.GetInt(1, 0, variables),
                2 => dateTime.Minute > modifier.GetInt(1, 0, variables),
                3 => dateTime.Hour % 12 > modifier.GetInt(1, 0, variables),
                4 => dateTime.Hour > modifier.GetInt(1, 0, variables),
                5 => dateTime.Day > modifier.GetInt(1, 0, variables),
                6 => dateTime.Month > modifier.GetInt(1, 0, variables),
                7 => dateTime.Year > modifier.GetInt(1, 0, variables),
                8 => dateTime.DayOfWeek > (DayOfWeek)modifier.GetInt(1, 0, variables),
                9 => dateTime.DayOfYear > modifier.GetInt(1, 0, variables),
                10 => dateTime.Ticks > modifier.GetInt(1, 0, variables),
                _ => false,
            };
        }

        // seconds
        public static bool realTimeSecondEquals(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            return Parser.TryParse(DateTime.Now.ToString("ss"), 0) == modifier.GetInt(0, 0, variables);
        }
        public static bool realTimeSecondLesserEquals(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            return Parser.TryParse(DateTime.Now.ToString("ss"), 0) <= modifier.GetInt(0, 0, variables);
        }
        public static bool realTimeSecondGreaterEquals(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            return Parser.TryParse(DateTime.Now.ToString("ss"), 0) >= modifier.GetInt(0, 0, variables);
        }
        public static bool realTimeSecondLesser(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            return Parser.TryParse(DateTime.Now.ToString("ss"), 0) < modifier.GetInt(0, 0, variables);
        }
        public static bool realTimeSecondGreater(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            return Parser.TryParse(DateTime.Now.ToString("ss"), 0) > modifier.GetInt(0, 0, variables);
        }

        // minutes
        public static bool realTimeMinuteEquals(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            return Parser.TryParse(DateTime.Now.ToString("mm"), 0) == modifier.GetInt(0, 0, variables);
        }
        public static bool realTimeMinuteLesserEquals(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            return Parser.TryParse(DateTime.Now.ToString("mm"), 0) <= modifier.GetInt(0, 0, variables);
        }
        public static bool realTimeMinuteGreaterEquals(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            return Parser.TryParse(DateTime.Now.ToString("mm"), 0) >= modifier.GetInt(0, 0, variables);
        }
        public static bool realTimeMinuteLesser(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            return Parser.TryParse(DateTime.Now.ToString("mm"), 0) < modifier.GetInt(0, 0, variables);
        }
        public static bool realTimeMinuteGreater(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            return Parser.TryParse(DateTime.Now.ToString("mm"), 0) > modifier.GetInt(0, 0, variables);
        }

        // 24 hours
        public static bool realTime24HourEquals(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            return Parser.TryParse(DateTime.Now.ToString("HH"), 0) == modifier.GetInt(0, 0, variables);
        }
        public static bool realTime24HourLesserEquals(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            return Parser.TryParse(DateTime.Now.ToString("HH"), 0) <= modifier.GetInt(0, 0, variables);
        }
        public static bool realTime24HourGreaterEquals(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            return Parser.TryParse(DateTime.Now.ToString("HH"), 0) >= modifier.GetInt(0, 0, variables);
        }
        public static bool realTime24HourLesser(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            return Parser.TryParse(DateTime.Now.ToString("HH"), 0) < modifier.GetInt(0, 0, variables);
        }
        public static bool realTime24HourGreater(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            return Parser.TryParse(DateTime.Now.ToString("HH"), 0) > modifier.GetInt(0, 0, variables);
        }

        // 12 hours
        public static bool realTime12HourEquals(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            return Parser.TryParse(DateTime.Now.ToString("hh"), 0) == modifier.GetInt(0, 0, variables);
        }
        public static bool realTime12HourLesserEquals(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            return Parser.TryParse(DateTime.Now.ToString("hh"), 0) <= modifier.GetInt(0, 0, variables);
        }
        public static bool realTime12HourGreaterEquals(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            return Parser.TryParse(DateTime.Now.ToString("hh"), 0) >= modifier.GetInt(0, 0, variables);
        }
        public static bool realTime12HourLesser(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            return Parser.TryParse(DateTime.Now.ToString("hh"), 0) < modifier.GetInt(0, 0, variables);
        }
        public static bool realTime12HourGreater(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            return Parser.TryParse(DateTime.Now.ToString("hh"), 0) > modifier.GetInt(0, 0, variables);
        }

        // days
        public static bool realTimeDayEquals(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            return Parser.TryParse(DateTime.Now.ToString("dd"), 0) == modifier.GetInt(0, 0, variables);
        }
        public static bool realTimeDayLesserEquals(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            return Parser.TryParse(DateTime.Now.ToString("dd"), 0) <= modifier.GetInt(0, 0, variables);
        }
        public static bool realTimeDayGreaterEquals(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            return Parser.TryParse(DateTime.Now.ToString("dd"), 0) >= modifier.GetInt(0, 0, variables);
        }
        public static bool realTimeDayLesser(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            return Parser.TryParse(DateTime.Now.ToString("dd"), 0) < modifier.GetInt(0, 0, variables);
        }
        public static bool realTimeDayGreater(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            return Parser.TryParse(DateTime.Now.ToString("dd"), 0) > modifier.GetInt(0, 0, variables);
        }

        public static bool realTimeDayWeekEquals(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            return DateTime.Now.ToString("dddd") == modifier.GetValue(0, variables);
        }

        // months
        public static bool realTimeMonthEquals(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            return Parser.TryParse(DateTime.Now.ToString("MM"), 0) == modifier.GetInt(0, 0, variables);
        }
        public static bool realTimeMonthLesserEquals(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            return Parser.TryParse(DateTime.Now.ToString("MM"), 0) <= modifier.GetInt(0, 0, variables);
        }
        public static bool realTimeMonthGreaterEquals(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            return Parser.TryParse(DateTime.Now.ToString("MM"), 0) >= modifier.GetInt(0, 0, variables);
        }
        public static bool realTimeMonthLesser(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            return Parser.TryParse(DateTime.Now.ToString("MM"), 0) < modifier.GetInt(0, 0, variables);
        }
        public static bool realTimeMonthGreater(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            return Parser.TryParse(DateTime.Now.ToString("MM"), 0) > modifier.GetInt(0, 0, variables);
        }

        // years
        public static bool realTimeYearEquals(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            return Parser.TryParse(DateTime.Now.ToString("yyyy"), 0) == modifier.GetInt(0, 0, variables);
        }
        public static bool realTimeYearLesserEquals(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            return Parser.TryParse(DateTime.Now.ToString("yyyy"), 0) <= modifier.GetInt(0, 0, variables);
        }
        public static bool realTimeYearGreaterEquals(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            return Parser.TryParse(DateTime.Now.ToString("yyyy"), 0) >= modifier.GetInt(0, 0, variables);
        }
        public static bool realTimeYearLesser(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            return Parser.TryParse(DateTime.Now.ToString("yyyy"), 0) < modifier.GetInt(0, 0, variables);
        }
        public static bool realTimeYearGreater(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            return Parser.TryParse(DateTime.Now.ToString("yyyy"), 0) > modifier.GetInt(0, 0, variables);
        }

        #endregion

        #region Config

        public static bool usernameEquals(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            return CoreConfig.Instance.DisplayName.Value == modifier.GetValue(0, variables);
        }

        public static bool languageEquals(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            return CoreConfig.Instance.Language.Value == (Language)modifier.GetInt(0, 0, variables);
        }

        public static bool configLDM(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            return CoreConfig.Instance.LDM.Value;
        }

        public static bool configShowEffects(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            return EventsConfig.Instance.ShowFX.Value;
        }

        public static bool configShowPlayerGUI(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            return EventsConfig.Instance.ShowGUI.Value;
        }

        public static bool configShowIntro(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            return EventsConfig.Instance.ShowIntro.Value;
        }

        #endregion

        #region Misc

        public static bool await(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (!modifier.constant)
            {
                if (CoreHelper.InEditor)
                    EditorManager.inst.DisplayNotification($"Constant has to be on in order for await modifiers to work!", 4f, EditorManager.NotificationType.Error);
                return false;
            }

            var start = modifier.GetInt(0, 0, variables);
            var realTime = modifier.GetBool(2, true, variables);
            float time;
            if (realTime)
            {
                var timer = modifier.GetResultOrDefault(() =>
                {
                    var timer = new RTTimer();
                    timer.offset = start;
                    timer.Reset();
                    return timer;
                });
                timer.Update();
                time = timer.time;
            }
            else
                time = modifier.GetResultOrDefault(() => reference.GetParentRuntime().FixedTime + start) + reference.GetParentRuntime().FixedTime;

            return time >= modifier.GetFloat(1, 0f, variables);
        }

        public static bool awaitCounter(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            var start = modifier.GetInt(0, 0, variables);
            var end = modifier.GetInt(1, 10, variables);
            var num = modifier.GetResultOrDefault(() => start - 1);
            num += modifier.GetInt(2, 1, variables);
            modifier.Result = num;
            return num >= end;
        }

        public static bool containsTag(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            return reference is IPrefabable prefabable && prefabable.FromPrefab && prefabable.TryGetPrefabObject(out PrefabObject prefabObject) &&
                prefabObject.Tags.Contains(modifier.GetValue(0, variables)) || reference is IModifyable modifyable && modifyable.Tags.Contains(modifier.GetValue(0, variables));
        }

        public static bool inEditor(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables) => CoreHelper.InEditor;

        public static bool isEditing(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables) => CoreHelper.IsEditing;

        public static bool requireSignal(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            return modifier.HasResult();
        }

        public static bool isFocused(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables) => Application.isFocused;

        public static bool isFullscreen(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables) => Screen.fullScreen;

        public static bool objectActive(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            var runtimeObject = reference.GetRuntimeObject();
            return runtimeObject != null && runtimeObject.Active;
        }

        public static bool objectCustomActive(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            return reference.GetRuntimeObject() is ICustomActivatable customActivatable && customActivatable.CustomActive;
        }

        public static bool objectAlive(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not IPrefabable prefabable)
                return false;

            var list = GameData.Current.FindObjectsWithTag(modifier, prefabable, modifier.GetValue(0, variables));
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].Alive)
                    return true;
            }
            return false;
        }

        public static bool objectSpawned(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not IPrefabable prefabable)
                return false;

            if (!modifier.HasResult())
                modifier.Result = new List<string>();

            var ids = modifier.GetResult<List<string>>();

            var list = GameData.Current.FindObjectsWithTag(modifier, prefabable, modifier.GetValue(0, variables));
            for (int i = 0; i < list.Count; i++)
            {
                if (!ids.Contains(list[i].id) && list[i].Alive)
                {
                    ids.Add(list[i].id);
                    modifier.Result = ids;
                    return true;
                }
            }

            return false;
        }

        public static bool fromPrefab(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables) => reference is IPrefabable prefabable && prefabable.FromPrefab;

        public static bool onMarker(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            var forward = AudioManager.inst.CurrentAudioSource.pitch >= 0f;

            var name = modifier.GetValue(0, variables);
            var color = modifier.GetInt(1, -1, variables);
            var layer = modifier.GetInt(2, -1, variables);
            var index = modifier.GetResultOrDefault(() => GameData.Current.data.GetLastMarkerIndex(x => x.Matches(name, color, layer)));
            var newIndex = GameData.Current.data.GetLastMarkerIndex(x => x.Matches(name, color, layer));
            if (index != newIndex)
            {
                modifier.Result = newIndex;
                // if current pitch is forwards, check if new index is ahead, otherwise if pitch is backwards then check if new index is behind
                return newIndex > index;
            }
            return false;
        }

        public static bool onCheckpoint(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            var forward = AudioManager.inst.CurrentAudioSource.pitch >= 0f;

            var name = modifier.GetValue(0, variables);
            var index = modifier.GetResultOrDefault(() => GameData.Current.data.GetLastCheckpointIndex(x => string.IsNullOrEmpty(name) || x.name == name));
            var newIndex = GameData.Current.data.GetLastCheckpointIndex(x => string.IsNullOrEmpty(name) || x.name == name);
            if (index != newIndex)
            {
                modifier.Result = newIndex;
                return newIndex > index;
            }
            return false;
        }

        public static bool callModifierBlockTrigger(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            var name = modifier.GetValue(0, variables);
            var prefabable = reference.AsPrefabable();
            var prefab = prefabable?.GetPrefab();
            if (prefabable != null && prefab && prefab.modifierBlocks.TryFind(x => x.Name == name, out ModifierBlock prefabModifierBlock))
                return prefabModifierBlock.Run(reference, variables).result;
            else if (GameData.Current.modifierBlocks.TryFind(x => x.Name == name, out ModifierBlock modifierBlock))
                return modifierBlock.Run(reference, variables).result;
            return false;
        }

        #endregion

        #region Player Only

        public static bool healthEquals(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            return reference is PAPlayer player && player.Health == modifier.GetInt(0, 3, variables);
        }

        public static bool healthGreaterEquals(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            return reference is PAPlayer player && player.Health >= modifier.GetInt(0, 3, variables);
        }

        public static bool healthLesserEquals(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            return reference is PAPlayer player && player.Health <= modifier.GetInt(0, 3, variables);
        }

        public static bool healthGreater(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            return reference is PAPlayer player && player.Health > modifier.GetInt(0, 3, variables);
        }

        public static bool healthLesser(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            return reference is PAPlayer player && player.Health < modifier.GetInt(0, 3, variables);
        }

        public static bool isDead(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            return reference is PAPlayer player && player.RuntimePlayer && player.RuntimePlayer.isDead;
        }

        public static bool isBoosting(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            return reference is PAPlayer player && player.RuntimePlayer && player.RuntimePlayer.isBoosting;
        }

        public static bool isColliding(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            return reference is PAPlayer player && player.RuntimePlayer && player.RuntimePlayer.triggerColliding;
        }

        public static bool isSolidColliding(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            return reference is PAPlayer player && player.RuntimePlayer && player.RuntimePlayer.colliding;
        }

        #endregion

        #region Dev Only

        #endregion
    }

    #region Caches

    public class GenericGroupCache<T>
    {
        public GenericGroupCache() { }

        public GenericGroupCache(string tag, List<T> group) => UpdateCache(tag, group);

        public string tag;
        public List<T> group;

        public void UpdateCache(string tag, List<T> group)
        {
            this.tag = tag;
            this.group = group;
        }
    }

    /// <summary>
    /// Cache for <see cref="ModifierFunctions.translateShape(Modifier, IModifierReference, Dictionary{string, string})"/>.
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
    /// Cache for <see cref="ModifierFunctions.enableObjectGroup(Modifier, IModifierReference, Dictionary{string, string})"/>.
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

    public class GroupCache
    {
        public GroupCache() { }

        public static GroupCache FromSingle(Modifier modifier, IPrefabable prefabable, string group)
        {
            var cache = new GroupCache();
            cache.group = group;
            if (!string.IsNullOrEmpty(group) && GameData.Current.TryFindObjectWithTag(modifier, prefabable, group, out BeatmapObject target))
                cache.target = target;
            return cache;
        }

        public static GroupCache FromGroup(IModifierReference reference, Modifier modifier, IPrefabable prefabable, string group, string otherGroup)
        {
            var cache = new GroupCache();
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

    public class GroupBeatmapObjectCache
    {
        public GroupBeatmapObjectCache(string tag) => this.tag = tag;

        public static GroupBeatmapObjectCache Get(Modifier modifier, IPrefabable prefabable, string tag)
        {
            var cache = new GroupBeatmapObjectCache(tag);
            cache.UpdateCache(modifier, prefabable, tag);
            return cache;
        }

        public void UpdateCache(Modifier modifier, IPrefabable prefabable, string tag)
        {
            if (GameData.Current.TryFindObjectWithTag(modifier, prefabable, tag, out BeatmapObject target))
                obj = target;
        }

        public string tag;
        public BeatmapObject obj;
    }

    public class MaskCache
    {
        public int width;
        public int height;
        public RenderTexture renderTexture;
    }

    public class RenderingCache
    {
        public bool doubleSided;
        public int gradientType;
        public int colorBlendMode;
        public float gradientScale = 1f;
        public float gradientRotation;

        public void UpdateCache(bool doubleSided, int gradientType, int colorBlendMode, float gradientScale, float gradientRotation)
        {
            this.doubleSided = doubleSided;
            this.gradientType = gradientType;
            this.colorBlendMode = colorBlendMode;
            this.gradientScale = gradientScale;
            this.gradientRotation = gradientRotation;
        }

        public bool Is(bool doubleSided, int gradientType, int colorBlendMode, float gradientScale, float gradientRotation) =>
            this.doubleSided == doubleSided &&
            this.gradientType == gradientType &&
            this.colorBlendMode == colorBlendMode &&
            this.gradientScale == gradientScale &&
            this.gradientRotation == gradientRotation;

        public void Apply(SolidObject solidObject)
        {
            solidObject.UpdateRendering(
                gradientType: gradientType,
                renderType: solidObject.gameObject.layer switch
                {
                    RTLevel.FOREGROUND_LAYER => (int)BeatmapObject.RenderLayerType.Foreground,
                    RTLevel.BACKGROUND_LAYER => (int)BeatmapObject.RenderLayerType.Background,
                    RTLevel.UI_LAYER => (int)BeatmapObject.RenderLayerType.UI,
                    _ => 0,
                },
                doubleSided: doubleSided,
                gradientScale: gradientScale,
                gradientRotation: gradientRotation,
                colorBlendMode: colorBlendMode);
        }
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
}

#pragma warning restore IDE1006 // Naming Styles