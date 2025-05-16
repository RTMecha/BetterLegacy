using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

using LSFunctions;

using SimpleJSON;

using BetterLegacy.Core.Components.Player;
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

        #region Running

        /// <summary>
        /// Checks if triggers return true.
        /// </summary>
        /// <typeparam name="T">The type of <see cref="Modifier{T}"/>.</typeparam>
        /// <param name="triggers">Triggers to check.</param>
        /// <returns>Returns true if all modifiers are active or if some have else if on, otherwise returns false.</returns>
        public static bool CheckTriggers<T>(List<Modifier<T>> triggers, Dictionary<string, string> variables)
        {
            bool result = true;
            triggers.ForLoop(trigger =>
            {
                if (trigger.active || trigger.triggerCount > 0 && trigger.runCount >= trigger.triggerCount)
                {
                    trigger.triggered = false;
                    result = false;
                    return;
                }

                var innerResult = trigger.not ? !trigger.Trigger(trigger, variables) : trigger.Trigger(trigger, variables);

                if (trigger.elseIf && !result && innerResult)
                    result = true;

                if (!trigger.elseIf && !innerResult)
                    result = false;

                trigger.triggered = innerResult;
            });
            return result;
        }

        /// <summary>
        /// Assigns the associated modifier functions to the modifier.
        /// </summary>
        /// <typeparam name="T">The type of <see cref="Modifier{T}"/>.</typeparam>
        /// <param name="modifier">The modifier to assign to.</param>
        public static void AssignModifierActions<T>(Modifier<T> modifier)
        {
            switch (modifier.referenceType)
            {
                case ModifierReferenceType.BeatmapObject: {
                        var objectModifier = modifier as Modifier<BeatmapObject>;

                        var name = modifier.Name;

                        // Only assign methods depending on modifier type.
                        if (objectModifier.type == ModifierBase.Type.Action)
                            objectModifier.Action = GetObjectAction(name);
                        if (objectModifier.type == ModifierBase.Type.Trigger)
                            objectModifier.Trigger = GetObjectTrigger(name);

                        objectModifier.Inactive = ObjectInactive;

                        break;
                    }
                case ModifierReferenceType.BackgroundObject: {
                        var bgModifier = modifier as Modifier<BackgroundObject>;

                        var name = modifier.Name;
                        if (bgModifier.type == ModifierBase.Type.Action)
                            bgModifier.Action = GetBGAction(name);
                        if (bgModifier.type == ModifierBase.Type.Trigger)
                            bgModifier.Trigger = GetBGTrigger(name);

                        bgModifier.Inactive = BGInactive;

                        break;
                    }
                case ModifierReferenceType.CustomPlayer: {
                        var playerModifier = modifier as Modifier<CustomPlayer>;

                        var name = modifier.Name;
                        if (playerModifier.type == ModifierBase.Type.Action)
                            playerModifier.Action = GetPlayerAction(name);
                        if (playerModifier.type == ModifierBase.Type.Trigger)
                            playerModifier.Trigger = GetPlayerTrigger(name);

                        playerModifier.Inactive = PlayerInactive;

                        break;
                    }
                case ModifierReferenceType.GameData: {
                        var levelModifier = modifier as Modifier<GameData>;

                        var name = modifier.Name;

                        // Only assign methods depending on modifier type.
                        if (levelModifier.type == ModifierBase.Type.Action)
                            levelModifier.Action = GetLevelAction(name);
                        if (levelModifier.type == ModifierBase.Type.Trigger)
                            levelModifier.Trigger = GetLevelTrigger(name);

                        break;
                    }
            }
        }

        /// <summary>
        /// Assigns actions to a modifier.
        /// </summary>
        /// <typeparam name="T">The type of <see cref="Modifier{T}"/>.</typeparam>
        /// <param name="modifier">The modifier to assign to.</param>
        /// <param name="action">Action function to set if <see cref="ModifierBase.type"/> is <see cref="ModifierBase.Type.Action"/>.</param>
        /// <param name="trigger">Trigger function to set if <see cref="ModifierBase.type"/> is <see cref="ModifierBase.Type.Trigger"/>.</param>
        /// <param name="inactive">Inactive function to set.</param>
        public static void AssignModifierAction<T>(Modifier<T> modifier, Action<Modifier<T>, Dictionary<string, string>> action, Func<Modifier<T>, Dictionary<string, string>, bool> trigger, Action<Modifier<T>, Dictionary<string, string>> inactive)
        {
            // Only assign methods depending on modifier type.
            if (modifier.type == ModifierBase.Type.Action)
                modifier.Action = action;
            if (modifier.type == ModifierBase.Type.Trigger)
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
        public static void RunModifiersAll<T>(List<Modifier<T>> modifiers, Dictionary<string, string> variables = null)
        {
            var actions = new List<Modifier<T>>();
            var triggers = new List<Modifier<T>>();
            modifiers.ForLoop(modifier =>
            {
                switch (modifier.type)
                {
                    case ModifierBase.Type.Action: {
                            if (modifier.Action == null || modifier.Inactive == null)
                                AssignModifierActions(modifier);

                            actions.Add(modifier);
                            break;
                        }
                    case ModifierBase.Type.Trigger: {
                            if (modifier.Trigger == null || modifier.Inactive == null)
                                AssignModifierActions(modifier);

                            triggers.Add(modifier);
                            break;
                        }
                }
            });

            if (!triggers.IsEmpty())
            {
                // If all triggers are active
                if (CheckTriggers(triggers, variables))
                {
                    bool returned = false;
                    actions.ForLoop(act =>
                    {
                        if (returned || act.active || act.triggerCount > 0 && act.runCount >= act.triggerCount) // Continue if modifier is not constant and was already activated
                            return;

                        if (!act.running)
                            act.runCount++;
                        if (!act.constant)
                            act.active = true;

                        act.running = true;
                        act.Action?.Invoke(act, variables);
                        if (act.Name == "return")
                            returned = true;
                    });
                    triggers.ForLoop(trig =>
                    {
                        if (trig.triggerCount > 0 && trig.runCount >= trig.triggerCount)
                            return;

                        if (!trig.running)
                            trig.runCount++;
                        if (!trig.constant)
                            trig.active = true;

                        trig.running = true;
                    });
                    return;
                }

                // Deactivate both action and trigger modifiers
                modifiers.ForLoop(modifier =>
                {
                    if (!modifier.active && !modifier.running)
                        return;

                    modifier.active = false;
                    modifier.running = false;
                    modifier.Inactive?.Invoke(modifier, variables);
                });
                return;
            }

            actions.ForLoop(act =>
            {
                if (act.active || act.triggerCount > 0 && act.runCount >= act.triggerCount)
                    return;

                if (!act.running)
                    act.runCount++;
                if (!act.constant)
                    act.active = true;

                act.running = true;
                act.Action?.Invoke(act, variables);
            });
        }

        /// <summary>
        /// The advanced way modifiers run.
        /// </summary>
        /// <typeparam name="T">The type of <see cref="Modifier{T}"/>.</typeparam>
        /// <param name="modifiers">The list of modifiers to run.</param>
        /// <param name="active">If the object is active.</param>
        public static void RunModifiersLoop<T>(List<Modifier<T>> modifiers, bool active = true, Dictionary<string, string> variables = null, int sequence = 0, int end = 0)
        {
            if (active)
            {
                bool returned = false;
                bool result = true; // Action modifiers at the start with no triggers before it should always run, so result is true.
                ModifierBase.Type previousType = ModifierBase.Type.Action;
                int index = 0;
                while (index < modifiers.Count)
                {
                    var modifier = modifiers[index];
                    var name = modifier.Name;

                    var isAction = modifier.type == ModifierBase.Type.Action;
                    var isTrigger = modifier.type == ModifierBase.Type.Trigger;

                    if (isAction && modifier.Action == null || isTrigger && modifier.Trigger == null || modifier.Inactive == null)
                        AssignModifierActions(modifier);

                    if (returned)
                    {
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
                                    RunModifiersLoop(selectModifiers, true, variables, i, endCount);
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

                    if (isTrigger)
                    {
                        if (previousType == ModifierBase.Type.Action) // If previous modifier was an action modifier, result should be considered true as we just started another modifier-block
                            result = true;

                        if (modifier.active || modifier.triggerCount > 0 && modifier.runCount >= modifier.triggerCount)
                        {
                            modifier.triggered = false;
                            result = false;
                        }
                        else
                        {
                            var innerResult = modifier.not ? !modifier.Trigger(modifier, variables) : modifier.Trigger(modifier, variables);

                            // Allow trigger to turn result to true again if "elseIf" is on
                            if (modifier.elseIf && !result && innerResult)
                                result = true;

                            if (!modifier.elseIf && !innerResult)
                                result = false;

                            modifier.triggered = innerResult;
                            previousType = modifier.type;
                        }
                    }

                    // Set modifier inactive state
                    if (!result && !(!modifier.active && !modifier.running))
                    {
                        modifier.active = false;
                        modifier.running = false;
                        modifier.Inactive?.Invoke(modifier, variables);

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
                    {
                        modifier.Action?.Invoke(modifier, variables);

                        if (name == "return" || name == "continue") // return stops the loop, continue moves it to the next loop
                            returned = true;
                    }

                    previousType = modifier.type;
                    index++;
                }
            }
            else if (modifiers.TryFindAll(x => x.active || x.running, out List<Modifier<T>> findAll))
                findAll.ForLoop(modifier =>
                {
                    modifier.runCount = 0;
                    modifier.active = false;
                    modifier.running = false;
                    modifier.Inactive?.Invoke(modifier, variables);
                });
        }

        #endregion

        #region Functions

        public static bool VerifyModifier<T>(Modifier<T> modifier, List<ModifierBase> modifiers)
        {
            modifier.hasChanged = false;

            if (!modifier.verified)
            {
                modifier.verified = true;

                if (!modifier.commands.IsEmpty() && !modifier.Name.Contains("DEVONLY"))
                    modifier.VerifyModifier(modifiers);
            }

            return !modifier.commands.IsEmpty();
        }

        #region GameData

        public static int GetLevelTriggerType(string key) => key switch
        {
            "time" => 0,
            "timeInRange" => 0,
            "playerHit" => 1,
            "playerDeath" => 2,
            "levelStart" => 3,
            "levelRestart" => 4,
            "levelRewind" => 5,
            _ => -1,
        };
        
        public static int GetLevelActionType(string key) => key switch
        {
            "vnInk" => 0,
            "vnTimeline" => 1,
            "playerBubble" => 2,
            "playerMoveAll" => 3,
            "playerBoostLock" => 4,
            "playerXLock" => 5,
            "playerYLock" => 6,
            "bgSpin" => 7,
            "bgMove" => 8,
            "playerBoost" => 9,
            "setMusicTime" => 10,
            "setPitch" => 11,
            _ => -1,
        };

        public static string GetLevelTriggerName(int type) => type switch
        {
            0 => "timeInRange",
            1 => "playerHit",
            2 => "playerDeath",
            3 => "levelStart",
            4 => "levelRestart",
            5 => "levelRewind",
            _ => string.Empty,
        };

        public static string GetLevelActionName(int type) => type switch
        {
            0 => "vnInk",
            1 => "vnTimeline",
            2 => "playerBubble",
            3 => "playerMoveAll",
            4 => "playerBoostLock",
            5 => "playerXLock",
            6 => "playerYLock",
            7 => "bgSpin",
            8 => "bgMove",
            9 => "playerBoost",
            10 => "setMusicTime",
            11 => "setPitch",
            _ => string.Empty,
        };

        public static Func<Modifier<GameData>, Dictionary<string, string>, bool> GetLevelTrigger(string key) => key switch
        {
            "time" => (modifier, variables) =>
            {
                var time = RTLevel.Current.FixedTime;
                return modifier.commands.Count > 2 && time >= modifier.GetFloat(1, 0f, variables) - 0.01f && time <= modifier.GetFloat(2, 0f, variables) + 0.1f;
            },
            "timeInRange" => (modifier, variables) =>
            {
                var time = RTLevel.Current.FixedTime;
                return modifier.commands.Count > 2 && time >= modifier.GetFloat(1, 0f, variables) - 0.01f && time <= modifier.GetFloat(2, 0f, variables) + 0.1f;
            },
            "playerHit" => (modifier, variables) => PlayerManager.Players.Any(x => x.Player && x.Player.isTakingHit),
            "playerDeath" => (modifier, variables) => PlayerManager.Players.Any(x => x.Player && x.Player.isDead),

            "levelStart" => (modifier, variables) => AudioManager.inst.CurrentAudioSource.time < 0.1f,
            "levelRestart" => (modifier, variables) => false, // why is this here...?
            "levelRewind" => (modifier, variables) => CoreHelper.Reversing,
            _ => (modifier, variables) => false,
        };

        public static Action<Modifier<GameData>, Dictionary<string, string>> GetLevelAction(string key) => key switch
        {
            "playerMoveAll" => ModifierActions.playerMoveAll,
            "playerBoostLock" => (modifier, variables) =>
            {
                if (modifier.commands.Count > 3 && !string.IsNullOrEmpty(modifier.commands[1]) && bool.TryParse(modifier.GetValue(0, variables), out bool lockBoost))
                    RTPlayer.LockBoost = lockBoost;
            },
            "playerXLock" => (modifier, variables) =>
            {

            },
            "playerYLock" => (modifier, variables) =>
            {

            },
            "playerBoost" => ModifierActions.playerBoostAll,
            "setMusicTime" => ModifierActions.setMusicTime,
            "setPitch" => ModifierActions.setPitch,
            _ => (modifier, variables) => { },
        };

        #endregion

        #region BeatmapObject

        public static Func<Modifier<BeatmapObject>, Dictionary<string, string>, bool> GetObjectTrigger(string key) => key switch
        {
            #region Player

            nameof(ModifierTriggers.playerCollide) => ModifierTriggers.playerCollide,
            nameof(ModifierTriggers.playerHealthEquals) => ModifierTriggers.playerHealthEquals,
            nameof(ModifierTriggers.playerHealthLesserEquals) => ModifierTriggers.playerHealthLesserEquals,
            nameof(ModifierTriggers.playerHealthGreaterEquals) => ModifierTriggers.playerHealthGreaterEquals,
            nameof(ModifierTriggers.playerHealthLesser) => ModifierTriggers.playerHealthLesser,
            nameof(ModifierTriggers.playerHealthGreater) => ModifierTriggers.playerHealthGreater,
            nameof(ModifierTriggers.playerMoving) => ModifierTriggers.playerMoving,
            nameof(ModifierTriggers.playerBoosting) => ModifierTriggers.playerBoosting,
            nameof(ModifierTriggers.playerAlive) => ModifierTriggers.playerAlive,
            nameof(ModifierTriggers.playerDeathsEquals) => ModifierTriggers.playerDeathsEquals,
            nameof(ModifierTriggers.playerDeathsLesserEquals) => ModifierTriggers.playerDeathsLesserEquals,
            nameof(ModifierTriggers.playerDeathsGreaterEquals) => ModifierTriggers.playerDeathsGreaterEquals,
            nameof(ModifierTriggers.playerDeathsLesser) => ModifierTriggers.playerDeathsLesser,
            nameof(ModifierTriggers.playerDeathsGreater) => ModifierTriggers.playerDeathsGreater,
            nameof(ModifierTriggers.playerDistanceGreater) => ModifierTriggers.playerDistanceGreater,
            nameof(ModifierTriggers.playerDistanceLesser) => ModifierTriggers.playerDistanceLesser,
            nameof(ModifierTriggers.playerCountEquals) => ModifierTriggers.playerCountEquals,
            nameof(ModifierTriggers.playerCountLesserEquals) => ModifierTriggers.playerCountLesserEquals,
            nameof(ModifierTriggers.playerCountGreaterEquals) => ModifierTriggers.playerCountGreaterEquals,
            nameof(ModifierTriggers.playerCountLesser) => ModifierTriggers.playerCountLesser,
            nameof(ModifierTriggers.playerCountGreater) => ModifierTriggers.playerCountGreater,
            nameof(ModifierTriggers.onPlayerHit) => ModifierTriggers.onPlayerHit,
            nameof(ModifierTriggers.onPlayerDeath) => ModifierTriggers.onPlayerDeath,
            nameof(ModifierTriggers.playerBoostEquals) => ModifierTriggers.playerBoostEquals,
            nameof(ModifierTriggers.playerBoostLesserEquals) => ModifierTriggers.playerBoostLesserEquals,
            nameof(ModifierTriggers.playerBoostGreaterEquals) => ModifierTriggers.playerBoostGreaterEquals,
            nameof(ModifierTriggers.playerBoostLesser) => ModifierTriggers.playerBoostLesser,
            nameof(ModifierTriggers.playerBoostGreater) => ModifierTriggers.playerBoostGreater,

            #endregion

            #region Controls

            nameof(ModifierTriggers.keyPressDown) => ModifierTriggers.keyPressDown,
            nameof(ModifierTriggers.keyPress) => ModifierTriggers.keyPress,
            nameof(ModifierTriggers.keyPressUp) => ModifierTriggers.keyPressUp,
            nameof(ModifierTriggers.mouseButtonDown) => ModifierTriggers.mouseButtonDown,
            nameof(ModifierTriggers.mouseButton) => ModifierTriggers.mouseButton,
            nameof(ModifierTriggers.mouseButtonUp) => ModifierTriggers.mouseButtonUp,
            nameof(ModifierTriggers.mouseOver) => ModifierTriggers.mouseOver,
            nameof(ModifierTriggers.mouseOverSignalModifier) => ModifierTriggers.mouseOverSignalModifier,
            nameof(ModifierTriggers.controlPressDown) => ModifierTriggers.controlPressDown,
            nameof(ModifierTriggers.controlPress) => ModifierTriggers.controlPress,
            nameof(ModifierTriggers.controlPressUp) => ModifierTriggers.controlPressUp,

            #endregion

            #region Collide

            nameof(ModifierTriggers.bulletCollide) => ModifierTriggers.bulletCollide,
            nameof(ModifierTriggers.objectCollide) => ModifierTriggers.objectCollide,

            #endregion

            #region JSON

            nameof(ModifierTriggers.loadEquals) => ModifierTriggers.loadEquals,
            nameof(ModifierTriggers.loadLesserEquals) => ModifierTriggers.loadLesserEquals,
            nameof(ModifierTriggers.loadGreaterEquals) => ModifierTriggers.loadGreaterEquals,
            nameof(ModifierTriggers.loadLesser) => ModifierTriggers.loadLesser,
            nameof(ModifierTriggers.loadGreater) => ModifierTriggers.loadGreater,
            nameof(ModifierTriggers.loadExists) => ModifierTriggers.loadExists,

            #endregion

            #region Variable

            nameof(ModifierTriggers.localVariableEquals) => ModifierTriggers.localVariableEquals,
            nameof(ModifierTriggers.localVariableLesserEquals) => ModifierTriggers.localVariableLesserEquals,
            nameof(ModifierTriggers.localVariableGreaterEquals) => ModifierTriggers.localVariableGreaterEquals,
            nameof(ModifierTriggers.localVariableLesser) => ModifierTriggers.localVariableLesser,
            nameof(ModifierTriggers.localVariableGreater) => ModifierTriggers.localVariableGreater,
            nameof(ModifierTriggers.localVariableContains) => ModifierTriggers.localVariableContains,
            nameof(ModifierTriggers.localVariableStartsWith) => ModifierTriggers.localVariableStartsWith,
            nameof(ModifierTriggers.localVariableEndsWith) => ModifierTriggers.localVariableEndsWith,
            nameof(ModifierTriggers.localVariableExists) => ModifierTriggers.localVariableExists,

            // self
            nameof(ModifierTriggers.variableEquals) => ModifierTriggers.variableEquals,
            nameof(ModifierTriggers.variableLesserEquals) => ModifierTriggers.variableLesserEquals,
            nameof(ModifierTriggers.variableGreaterEquals) => ModifierTriggers.variableGreaterEquals,
            nameof(ModifierTriggers.variableLesser) => ModifierTriggers.variableLesser,
            nameof(ModifierTriggers.variableGreater) => ModifierTriggers.variableGreater,

            // other
            nameof(ModifierTriggers.variableOtherEquals) => ModifierTriggers.variableOtherEquals,
            nameof(ModifierTriggers.variableOtherLesserEquals) => ModifierTriggers.variableOtherLesserEquals,
            nameof(ModifierTriggers.variableOtherGreaterEquals) => ModifierTriggers.variableOtherGreaterEquals,
            nameof(ModifierTriggers.variableOtherLesser) => ModifierTriggers.variableOtherLesser,
            nameof(ModifierTriggers.variableOtherGreater) => ModifierTriggers.variableOtherGreater,

            #endregion

            #region Audio

            nameof(ModifierTriggers.pitchEquals) => ModifierTriggers.pitchEquals,
            nameof(ModifierTriggers.pitchLesserEquals) => ModifierTriggers.pitchLesserEquals,
            nameof(ModifierTriggers.pitchGreaterEquals) => ModifierTriggers.pitchGreaterEquals,
            nameof(ModifierTriggers.pitchLesser) => ModifierTriggers.pitchLesser,
            nameof(ModifierTriggers.pitchGreater) => ModifierTriggers.pitchGreater,
            nameof(ModifierTriggers.musicTimeGreater) => ModifierTriggers.musicTimeGreater,
            nameof(ModifierTriggers.musicTimeLesser) => ModifierTriggers.musicTimeLesser,
            nameof(ModifierTriggers.musicPlaying) => ModifierTriggers.musicPlaying,

            #endregion

            #region Challenge Mode

            nameof(ModifierTriggers.inZenMode) => ModifierTriggers.inZenMode,
            nameof(ModifierTriggers.inNormal) => ModifierTriggers.inNormal,
            nameof(ModifierTriggers.in1Life) => ModifierTriggers.in1Life,
            nameof(ModifierTriggers.inNoHit) => ModifierTriggers.inNoHit,
            nameof(ModifierTriggers.inPractice) => ModifierTriggers.inPractice,

            #endregion

            #region Random

            nameof(ModifierTriggers.randomEquals) => ModifierTriggers.randomEquals,
            nameof(ModifierTriggers.randomLesser) => ModifierTriggers.randomLesser,
            nameof(ModifierTriggers.randomGreater) => ModifierTriggers.randomGreater,

            #endregion

            #region Math

            nameof(ModifierTriggers.mathEquals) => ModifierTriggers.mathEquals,
            nameof(ModifierTriggers.mathLesserEquals) => ModifierTriggers.mathLesserEquals,
            nameof(ModifierTriggers.mathGreaterEquals) => ModifierTriggers.mathGreaterEquals,
            nameof(ModifierTriggers.mathLesser) => ModifierTriggers.mathLesser,
            nameof(ModifierTriggers.mathGreater) => ModifierTriggers.mathGreater,

            #endregion

            #region Axis

            nameof(ModifierTriggers.axisEquals) => ModifierTriggers.axisEquals,
            nameof(ModifierTriggers.axisLesserEquals) => ModifierTriggers.axisLesserEquals,
            nameof(ModifierTriggers.axisGreaterEquals) => ModifierTriggers.axisGreaterEquals,
            nameof(ModifierTriggers.axisLesser) => ModifierTriggers.axisLesser,
            nameof(ModifierTriggers.axisGreater) => ModifierTriggers.axisGreater,

            #endregion

            #region Level Rank

            // self
            nameof(ModifierTriggers.levelRankEquals) => ModifierTriggers.levelRankEquals,
            nameof(ModifierTriggers.levelRankLesserEquals) => ModifierTriggers.levelRankLesserEquals,
            nameof(ModifierTriggers.levelRankGreaterEquals) => ModifierTriggers.levelRankGreaterEquals,
            nameof(ModifierTriggers.levelRankLesser) => ModifierTriggers.levelRankLesser,
            nameof(ModifierTriggers.levelRankGreater) => ModifierTriggers.levelRankGreater,

            // other
            nameof(ModifierTriggers.levelRankOtherEquals) => ModifierTriggers.levelRankOtherEquals,
            nameof(ModifierTriggers.levelRankOtherLesserEquals) => ModifierTriggers.levelRankOtherLesserEquals,
            nameof(ModifierTriggers.levelRankOtherGreaterEquals) => ModifierTriggers.levelRankOtherGreaterEquals,
            nameof(ModifierTriggers.levelRankOtherLesser) => ModifierTriggers.levelRankOtherLesser,
            nameof(ModifierTriggers.levelRankOtherGreater) => ModifierTriggers.levelRankOtherGreater,

            // current
            nameof(ModifierTriggers.levelRankCurrentEquals) => ModifierTriggers.levelRankCurrentEquals,
            nameof(ModifierTriggers.levelRankCurrentLesserEquals) => ModifierTriggers.levelRankCurrentLesserEquals,
            nameof(ModifierTriggers.levelRankCurrentGreaterEquals) => ModifierTriggers.levelRankCurrentGreaterEquals,
            nameof(ModifierTriggers.levelRankCurrentLesser) => ModifierTriggers.levelRankCurrentLesser,
            nameof(ModifierTriggers.levelRankCurrentGreater) => ModifierTriggers.levelRankCurrentGreater,

            #endregion

            #region Level

            nameof(ModifierTriggers.levelUnlocked) => ModifierTriggers.levelUnlocked,
            nameof(ModifierTriggers.levelCompleted) => ModifierTriggers.levelCompleted,
            nameof(ModifierTriggers.levelCompletedOther) => ModifierTriggers.levelCompletedOther,
            nameof(ModifierTriggers.levelExists) => ModifierTriggers.levelExists,
            nameof(ModifierTriggers.levelPathExists) => ModifierTriggers.levelPathExists,

            #endregion

            #region Real Time

            // seconds
            nameof(ModifierTriggers.realTimeSecondEquals) => ModifierTriggers.realTimeSecondEquals,
            nameof(ModifierTriggers.realTimeSecondLesserEquals) => ModifierTriggers.realTimeSecondLesserEquals,
            nameof(ModifierTriggers.realTimeSecondGreaterEquals) => ModifierTriggers.realTimeSecondGreaterEquals,
            nameof(ModifierTriggers.realTimeSecondLesser) => ModifierTriggers.realTimeSecondLesser,
            nameof(ModifierTriggers.realTimeSecondGreater) => ModifierTriggers.realTimeSecondGreater,

            // minutes
            nameof(ModifierTriggers.realTimeMinuteEquals) => ModifierTriggers.realTimeMinuteEquals,
            nameof(ModifierTriggers.realTimeMinuteLesserEquals) => ModifierTriggers.realTimeMinuteLesserEquals,
            nameof(ModifierTriggers.realTimeMinuteGreaterEquals) => ModifierTriggers.realTimeMinuteGreaterEquals,
            nameof(ModifierTriggers.realTimeMinuteLesser) => ModifierTriggers.realTimeMinuteLesser,
            nameof(ModifierTriggers.realTimeMinuteGreater) => ModifierTriggers.realTimeMinuteGreater,

            // 24 hours
            nameof(ModifierTriggers.realTime24HourEquals) => ModifierTriggers.realTime24HourEquals,
            nameof(ModifierTriggers.realTime24HourLesserEquals) => ModifierTriggers.realTime24HourLesserEquals,
            nameof(ModifierTriggers.realTime24HourGreaterEquals) => ModifierTriggers.realTime24HourGreaterEquals,
            nameof(ModifierTriggers.realTime24HourLesser) => ModifierTriggers.realTime24HourLesser,
            nameof(ModifierTriggers.realTime24HourGreater) => ModifierTriggers.realTime24HourGreater,

            // 12 hours
            nameof(ModifierTriggers.realTime12HourEquals) => ModifierTriggers.realTime12HourEquals,
            nameof(ModifierTriggers.realTime12HourLesserEquals) => ModifierTriggers.realTime12HourLesserEquals,
            nameof(ModifierTriggers.realTime12HourGreaterEquals) => ModifierTriggers.realTime12HourGreaterEquals,
            nameof(ModifierTriggers.realTime12HourLesser) => ModifierTriggers.realTime12HourLesser,
            nameof(ModifierTriggers.realTime12HourGreater) => ModifierTriggers.realTime12HourGreater,

            // days
            nameof(ModifierTriggers.realTimeDayEquals) => ModifierTriggers.realTimeDayEquals,
            nameof(ModifierTriggers.realTimeDayLesserEquals) => ModifierTriggers.realTimeDayLesserEquals,
            nameof(ModifierTriggers.realTimeDayGreaterEquals) => ModifierTriggers.realTimeDayGreaterEquals,
            nameof(ModifierTriggers.realTimeDayLesser) => ModifierTriggers.realTimeDayLesser,
            nameof(ModifierTriggers.realTimeDayGreater) => ModifierTriggers.realTimeDayGreater,
            nameof(ModifierTriggers.realTimeDayWeekEquals) => ModifierTriggers.realTimeDayWeekEquals,

            // months
            nameof(ModifierTriggers.realTimeMonthEquals) => ModifierTriggers.realTimeMonthEquals,
            nameof(ModifierTriggers.realTimeMonthLesserEquals) => ModifierTriggers.realTimeMonthLesserEquals,
            nameof(ModifierTriggers.realTimeMonthGreaterEquals) => ModifierTriggers.realTimeMonthGreaterEquals,
            nameof(ModifierTriggers.realTimeMonthLesser) => ModifierTriggers.realTimeMonthLesser,
            nameof(ModifierTriggers.realTimeMonthGreater) => ModifierTriggers.realTimeMonthGreater,

            // years
            nameof(ModifierTriggers.realTimeYearEquals) => ModifierTriggers.realTimeYearEquals,
            nameof(ModifierTriggers.realTimeYearLesserEquals) => ModifierTriggers.realTimeYearLesserEquals,
            nameof(ModifierTriggers.realTimeYearGreaterEquals) => ModifierTriggers.realTimeYearGreaterEquals,
            nameof(ModifierTriggers.realTimeYearLesser) => ModifierTriggers.realTimeYearLesser,
            nameof(ModifierTriggers.realTimeYearGreater) => ModifierTriggers.realTimeYearGreater,

            #endregion

            #region Config

            // main
            nameof(ModifierTriggers.usernameEquals) => ModifierTriggers.usernameEquals,
            nameof(ModifierTriggers.languageEquals) => ModifierTriggers.languageEquals,

            // misc
            nameof(ModifierTriggers.configLDM) => ModifierTriggers.configLDM,
            nameof(ModifierTriggers.configShowEffects) => ModifierTriggers.configShowEffects,
            nameof(ModifierTriggers.configShowPlayerGUI) => ModifierTriggers.configShowPlayerGUI,
            nameof(ModifierTriggers.configShowIntro) => ModifierTriggers.configShowIntro,

            #endregion

            #region Misc

            nameof(ModifierTriggers.inEditor) => ModifierTriggers.inEditor,
            nameof(ModifierTriggers.requireSignal) => ModifierTriggers.requireSignal,
            nameof(ModifierTriggers.isFullscreen) => ModifierTriggers.isFullscreen,
            nameof(ModifierTriggers.objectAlive) => ModifierTriggers.objectAlive,
            nameof(ModifierTriggers.objectSpawned) => ModifierTriggers.objectSpawned,

            #endregion

            #region Dev Only

            "storyLoadIntEqualsDEVONLY" => (modifier, variables) =>
            {
                return Story.StoryManager.inst.LoadInt(modifier.GetValue(0, variables), modifier.GetInt(1, 0, variables)) == modifier.GetInt(2, 0, variables);
            },
            "storyLoadIntLesserEqualsDEVONLY" => (modifier, variables) =>
            {
                return Story.StoryManager.inst.LoadInt(modifier.GetValue(0, variables), modifier.GetInt(1, 0, variables)) <= modifier.GetInt(2, 0, variables);
            },
            "storyLoadIntGreaterEqualsDEVONLY" => (modifier, variables) =>
            {
                return Story.StoryManager.inst.LoadInt(modifier.GetValue(0, variables), modifier.GetInt(1, 0, variables)) >= modifier.GetInt(2, 0, variables);
            },
            "storyLoadIntLesserDEVONLY" => (modifier, variables) =>
            {
                return Story.StoryManager.inst.LoadInt(modifier.GetValue(0, variables), modifier.GetInt(1, 0, variables)) < modifier.GetInt(2, 0, variables);
            },
            "storyLoadIntGreaterDEVONLY" => (modifier, variables) =>
            {
                return Story.StoryManager.inst.LoadInt(modifier.GetValue(0, variables), modifier.GetInt(1, 0, variables)) > modifier.GetInt(2, 0, variables);
            },
            "storyLoadBoolDEVONLY" => (modifier, variables) =>
            {
                return Story.StoryManager.inst.LoadBool(modifier.GetValue(0, variables), modifier.GetBool(1, false));
            },

            #endregion

            "break" => (modifier, variables) => true,
            _ => (modifier, variables) => false,
        };

        public static Action<Modifier<BeatmapObject>, Dictionary<string, string>> GetObjectAction(string key) => key switch
        {
            #region Audio

            // pitch
            nameof(ModifierActions.setPitch) => ModifierActions.setPitch,
            nameof(ModifierActions.addPitch) => ModifierActions.addPitch,
            nameof(ModifierActions.setPitchMath) => ModifierActions.setPitchMath,
            nameof(ModifierActions.addPitchMath) => ModifierActions.addPitchMath,

            // music playing states
            nameof(ModifierActions.setMusicTime) => ModifierActions.setMusicTime,
            nameof(ModifierActions.setMusicTimeMath) => ModifierActions.setMusicTimeMath,
            nameof(ModifierActions.setMusicTimeStartTime) => ModifierActions.setMusicTimeStartTime,
            nameof(ModifierActions.setMusicTimeAutokill) => ModifierActions.setMusicTimeAutokill,
            nameof(ModifierActions.setMusicPlaying) => ModifierActions.setMusicPlaying,

            // play sound
            nameof(ModifierActions.playSound) => ModifierActions.playSound,
            nameof(ModifierActions.playSoundOnline) => ModifierActions.playSoundOnline,
            nameof(ModifierActions.playDefaultSound) => ModifierActions.playDefaultSound,
            nameof(ModifierActions.audioSource) => ModifierActions.audioSource,

            #endregion

            #region Level

            nameof(ModifierActions.loadLevel) => ModifierActions.loadLevel,
            nameof(ModifierActions.loadLevelID) => ModifierActions.loadLevelID,
            nameof(ModifierActions.loadLevelInternal) => ModifierActions.loadLevelInternal,
            nameof(ModifierActions.loadLevelPrevious) => ModifierActions.loadLevelPrevious,
            nameof(ModifierActions.loadLevelHub) => ModifierActions.loadLevelHub,
            nameof(ModifierActions.loadLevelInCollection) => ModifierActions.loadLevelInCollection,
            nameof(ModifierActions.downloadLevel) => ModifierActions.downloadLevel,
            nameof(ModifierActions.endLevel) => ModifierActions.endLevel,
            nameof(ModifierActions.setAudioTransition) => ModifierActions.setAudioTransition,
            nameof(ModifierActions.setIntroFade) => ModifierActions.setIntroFade,
            nameof(ModifierActions.setLevelEndFunc) => ModifierActions.setLevelEndFunc,

            #endregion

            #region Component

            nameof(ModifierActions.blur) => ModifierActions.blur,
            nameof(ModifierActions.blurOther) => ModifierActions.blurOther,
            nameof(ModifierActions.blurVariable) => ModifierActions.blurVariable,
            nameof(ModifierActions.blurVariableOther) => ModifierActions.blurVariableOther,
            nameof(ModifierActions.blurColored) => ModifierActions.blurColored,
            nameof(ModifierActions.blurColoredOther) => ModifierActions.blurColoredOther,
            nameof(ModifierActions.doubleSided) => ModifierActions.doubleSided,
            nameof(ModifierActions.particleSystem) => ModifierActions.particleSystem,
            nameof(ModifierActions.trailRenderer) => ModifierActions.trailRenderer,
            nameof(ModifierActions.trailRendererHex) => ModifierActions.trailRendererHex,
            nameof(ModifierActions.rigidbody) => ModifierActions.rigidbody,
            nameof(ModifierActions.rigidbodyOther) => ModifierActions.rigidbodyOther,

            #endregion

            #region Player

            // hit
            nameof(ModifierActions.playerHit) => ModifierActions.playerHit,
            nameof(ModifierActions.playerHitIndex) => ModifierActions.playerHitIndex,
            nameof(ModifierActions.playerHitAll) => ModifierActions.playerHitAll,

            // heal
            nameof(ModifierActions.playerHeal) => ModifierActions.playerHeal,
            nameof(ModifierActions.playerHealIndex) => ModifierActions.playerHealIndex,
            nameof(ModifierActions.playerHealAll) => ModifierActions.playerHealAll,

            // kill
            nameof(ModifierActions.playerKill) => ModifierActions.playerKill,
            nameof(ModifierActions.playerKillIndex) => ModifierActions.playerKillIndex,
            nameof(ModifierActions.playerKillAll) => ModifierActions.playerKillAll,

            // respawn
            nameof(ModifierActions.playerRespawn) => ModifierActions.playerRespawn,
            nameof(ModifierActions.playerRespawnIndex) => ModifierActions.playerRespawnIndex,
            nameof(ModifierActions.playerRespawnAll) => ModifierActions.playerRespawnAll,

            // player move
            nameof(ModifierActions.playerMove) => ModifierActions.playerMove,
            nameof(ModifierActions.playerMoveIndex) => ModifierActions.playerMoveIndex,
            nameof(ModifierActions.playerMoveAll) => ModifierActions.playerMoveAll,
            nameof(ModifierActions.playerMoveX) => ModifierActions.playerMoveX,
            nameof(ModifierActions.playerMoveXIndex) => ModifierActions.playerMoveXIndex,
            nameof(ModifierActions.playerMoveXAll) => ModifierActions.playerMoveXAll,
            nameof(ModifierActions.playerMoveY) => ModifierActions.playerMoveY,
            nameof(ModifierActions.playerMoveYIndex) => ModifierActions.playerMoveYIndex,
            nameof(ModifierActions.playerMoveYAll) => ModifierActions.playerMoveYAll,
            nameof(ModifierActions.playerRotate) => ModifierActions.playerRotate,
            nameof(ModifierActions.playerRotateIndex) => ModifierActions.playerRotateIndex,
            nameof(ModifierActions.playerRotateAll) => ModifierActions.playerRotateAll,

            // move to object
            nameof(ModifierActions.playerMoveToObject) => ModifierActions.playerMoveToObject,
            nameof(ModifierActions.playerMoveIndexToObject) => ModifierActions.playerMoveIndexToObject,
            nameof(ModifierActions.playerMoveAllToObject) => ModifierActions.playerMoveAllToObject,
            nameof(ModifierActions.playerMoveXToObject) => ModifierActions.playerMoveXToObject,
            nameof(ModifierActions.playerMoveXIndexToObject) => ModifierActions.playerMoveXIndexToObject,
            nameof(ModifierActions.playerMoveXAllToObject) => ModifierActions.playerMoveXAllToObject,
            nameof(ModifierActions.playerMoveYToObject) => ModifierActions.playerMoveYToObject,
            nameof(ModifierActions.playerMoveYIndexToObject) => ModifierActions.playerMoveYIndexToObject,
            nameof(ModifierActions.playerMoveYAllToObject) => ModifierActions.playerMoveYAllToObject,
            nameof(ModifierActions.playerRotateToObject) => ModifierActions.playerRotateToObject,
            nameof(ModifierActions.playerRotateIndexToObject) => ModifierActions.playerRotateIndexToObject,
            nameof(ModifierActions.playerRotateAllToObject) => ModifierActions.playerRotateAllToObject,

            // actions
            nameof(ModifierActions.playerBoost) => ModifierActions.playerBoost,
            nameof(ModifierActions.playerBoostIndex) => ModifierActions.playerBoostIndex,
            nameof(ModifierActions.playerBoostAll) => ModifierActions.playerBoostAll,
            nameof(ModifierActions.playerDisableBoost) => ModifierActions.playerDisableBoost,
            nameof(ModifierActions.playerDisableBoostIndex) => ModifierActions.playerDisableBoostIndex,
            nameof(ModifierActions.playerDisableBoostAll) => ModifierActions.playerDisableBoostAll,
            nameof(ModifierActions.playerEnableBoost) => ModifierActions.playerEnableBoost,
            nameof(ModifierActions.playerEnableBoostIndex) => ModifierActions.playerEnableBoostIndex,
            nameof(ModifierActions.playerEnableBoostAll) => ModifierActions.playerEnableBoostAll,

            // speed
            nameof(ModifierActions.playerSpeed) => ModifierActions.playerSpeed,
            nameof(ModifierActions.playerVelocityAll) => ModifierActions.playerVelocityAll,
            nameof(ModifierActions.playerVelocityXAll) => ModifierActions.playerVelocityXAll,
            nameof(ModifierActions.playerVelocityYAll) => ModifierActions.playerVelocityYAll,

            nameof(ModifierActions.setPlayerModel) => ModifierActions.setPlayerModel,
            nameof(ModifierActions.setGameMode) => ModifierActions.setGameMode,
            nameof(ModifierActions.gameMode) => ModifierActions.gameMode,

            nameof(ModifierActions.blackHole) => ModifierActions.blackHole,

            #endregion

            #region Mouse Cursor

            nameof(ModifierActions.showMouse) => ModifierActions.showMouse,
            nameof(ModifierActions.hideMouse) => ModifierActions.hideMouse,
            nameof(ModifierActions.setMousePosition) => ModifierActions.setMousePosition,
            nameof(ModifierActions.followMousePosition) => ModifierActions.followMousePosition,

            #endregion

            #region Variable

            nameof(ModifierActions.getToggle) => ModifierActions.getToggle,
            nameof(ModifierActions.getFloat) => ModifierActions.getFloat,
            nameof(ModifierActions.getInt) => ModifierActions.getInt,
            nameof(ModifierActions.getString) => ModifierActions.getString,
            nameof(ModifierActions.getStringLower) => ModifierActions.getStringLower,
            nameof(ModifierActions.getStringUpper) => ModifierActions.getStringUpper,
            nameof(ModifierActions.getColor) => ModifierActions.getColor,
            nameof(ModifierActions.getEnum) => ModifierActions.getEnum,
            nameof(ModifierActions.getPitch) => ModifierActions.getPitch,
            nameof(ModifierActions.getMusicTime) => ModifierActions.getMusicTime,
            nameof(ModifierActions.getAxis) => ModifierActions.getAxis,
            nameof(ModifierActions.getMath) => ModifierActions.getMath,
            nameof(ModifierActions.getNearestPlayer) => ModifierActions.getNearestPlayer,
            nameof(ModifierActions.getCollidingPlayers) => ModifierActions.getCollidingPlayers,
            nameof(ModifierActions.getPlayerHealth) => ModifierActions.getPlayerHealth,
            nameof(ModifierActions.getPlayerPosX) => ModifierActions.getPlayerPosX,
            nameof(ModifierActions.getPlayerPosY) => ModifierActions.getPlayerPosY,
            nameof(ModifierActions.getPlayerRot) => ModifierActions.getPlayerRot,
            nameof(ModifierActions.getEventValue) => ModifierActions.getEventValue,
            nameof(ModifierActions.getSample) => ModifierActions.getSample,
            nameof(ModifierActions.getText) => ModifierActions.getText,
            nameof(ModifierActions.getTextOther) => ModifierActions.getTextOther,
            nameof(ModifierActions.getCurrentKey) => ModifierActions.getCurrentKey,
            nameof(ModifierActions.getColorSlotHexCode) => ModifierActions.getColorSlotHexCode,
            nameof(ModifierActions.getFloatFromHexCode) => ModifierActions.getFloatFromHexCode,
            nameof(ModifierActions.getHexCodeFromFloat) => ModifierActions.getHexCodeFromFloat,
            nameof(ModifierActions.getJSONString) => ModifierActions.getJSONString,
            nameof(ModifierActions.getJSONFloat) => ModifierActions.getJSONFloat,
            nameof(ModifierActions.getJSON) => ModifierActions.getJSON,
            nameof(ModifierActions.getSubString) => ModifierActions.getSubString,
            nameof(ModifierActions.getSplitString) => ModifierActions.getSplitString,
            nameof(ModifierActions.getSplitStringAt) => ModifierActions.getSplitStringAt,
            nameof(ModifierActions.getSplitStringCount) => ModifierActions.getSplitStringCount,
            nameof(ModifierActions.getStringLength) => ModifierActions.getStringLength,
            nameof(ModifierActions.getParsedString) => ModifierActions.getParsedString,
            nameof(ModifierActions.getRegex) => ModifierActions.getRegex,
            nameof(ModifierActions.getFormatVariable) => ModifierActions.getFormatVariable,
            nameof(ModifierActions.getComparison) => ModifierActions.getComparison,
            nameof(ModifierActions.getComparisonMath) => ModifierActions.getComparisonMath,
            nameof(ModifierActions.getMixedColors) => ModifierActions.getMixedColors,
            nameof(ModifierActions.getSignaledVariables) => ModifierActions.getSignaledVariables,
            nameof(ModifierActions.signalLocalVariables) => ModifierActions.signalLocalVariables,
            nameof(ModifierActions.clearLocalVariables) => ModifierActions.clearLocalVariables,

            nameof(ModifierActions.addVariable) => ModifierActions.addVariable,
            nameof(ModifierActions.addVariableOther) => ModifierActions.addVariableOther,
            nameof(ModifierActions.subVariable) => ModifierActions.subVariable,
            nameof(ModifierActions.subVariableOther) => ModifierActions.subVariableOther,
            nameof(ModifierActions.setVariable) => ModifierActions.setVariable,
            nameof(ModifierActions.setVariableOther) => ModifierActions.setVariableOther,
            nameof(ModifierActions.setVariableRandom) => ModifierActions.setVariableRandom,
            nameof(ModifierActions.setVariableRandomOther) => ModifierActions.setVariableRandomOther,
            nameof(ModifierActions.animateVariableOther) => ModifierActions.animateVariableOther,
            nameof(ModifierActions.clampVariable) => ModifierActions.clampVariable,
            nameof(ModifierActions.clampVariableOther) => ModifierActions.clampVariableOther,

            #endregion

            #region Enable / Disable

            // enable
            nameof(ModifierActions.enableObject) => ModifierActions.enableObject,
            nameof(ModifierActions.enableObjectTree) => ModifierActions.enableObjectTree,
            nameof(ModifierActions.enableObjectOther) => ModifierActions.enableObjectOther,
            nameof(ModifierActions.enableObjectTreeOther) => ModifierActions.enableObjectTreeOther,
            nameof(ModifierActions.enableObjectGroup) => ModifierActions.enableObjectGroup,

            // disable
            nameof(ModifierActions.disableObject) => ModifierActions.disableObject,
            nameof(ModifierActions.disableObjectTree) => ModifierActions.disableObjectTree,
            nameof(ModifierActions.disableObjectOther) => ModifierActions.disableObjectOther,
            nameof(ModifierActions.disableObjectTreeOther) => ModifierActions.disableObjectTreeOther,

            #endregion

            #region JSON

            nameof(ModifierActions.saveFloat) => ModifierActions.saveFloat,
            nameof(ModifierActions.saveString) => ModifierActions.saveString,
            nameof(ModifierActions.saveText) => ModifierActions.saveText,
            nameof(ModifierActions.saveVariable) => ModifierActions.saveVariable,
            nameof(ModifierActions.loadVariable) => ModifierActions.loadVariable,
            nameof(ModifierActions.loadVariableOther) => ModifierActions.loadVariableOther,

            #endregion

            #region Reactive

            // single
            nameof(ModifierActions.reactivePos) => ModifierActions.reactivePos,
            nameof(ModifierActions.reactiveSca) => ModifierActions.reactiveSca,
            nameof(ModifierActions.reactiveRot) => ModifierActions.reactiveRot,
            nameof(ModifierActions.reactiveCol) => ModifierActions.reactiveCol,
            nameof(ModifierActions.reactiveColLerp) => ModifierActions.reactiveColLerp,

            // chain
            nameof(ModifierActions.reactivePosChain) => ModifierActions.reactivePosChain,
            nameof(ModifierActions.reactiveScaChain) => ModifierActions.reactiveScaChain,
            nameof(ModifierActions.reactiveRotChain) => ModifierActions.reactiveRotChain,

            #endregion

            #region Events

            nameof(ModifierActions.eventOffset) => ModifierActions.eventOffset,
            nameof(ModifierActions.eventOffsetVariable) => ModifierActions.eventOffsetVariable,
            nameof(ModifierActions.eventOffsetMath) => ModifierActions.eventOffsetMath,
            nameof(ModifierActions.eventOffsetAnimate) => ModifierActions.eventOffsetAnimate,
            nameof(ModifierActions.eventOffsetCopyAxis) => ModifierActions.eventOffsetCopyAxis,
            nameof(ModifierActions.vignetteTracksPlayer) => ModifierActions.vignetteTracksPlayer,
            nameof(ModifierActions.lensTracksPlayer) => ModifierActions.lensTracksPlayer,

            #endregion

            // todo: implement gradients and different color controls
            #region Color

            // color
            nameof(ModifierActions.addColor) => ModifierActions.addColor,
            nameof(ModifierActions.addColorOther) => ModifierActions.addColorOther,
            nameof(ModifierActions.lerpColor) => ModifierActions.lerpColor,
            nameof(ModifierActions.lerpColorOther) => ModifierActions.lerpColorOther,
            nameof(ModifierActions.addColorPlayerDistance) => ModifierActions.addColorPlayerDistance,
            nameof(ModifierActions.lerpColorPlayerDistance) => ModifierActions.lerpColorPlayerDistance,

            // opacity
            "setAlpha" => ModifierActions.setOpacity,
            nameof(ModifierActions.setOpacity) => ModifierActions.setOpacity,
            "setAlphaOther" => ModifierActions.setOpacityOther,
            nameof(ModifierActions.setOpacityOther) => ModifierActions.setOpacityOther,

            // copy
            nameof(ModifierActions.copyColor) => ModifierActions.copyColor,
            nameof(ModifierActions.copyColorOther) => ModifierActions.copyColorOther,
            nameof(ModifierActions.applyColorGroup) => ModifierActions.applyColorGroup,

            // hex code
            nameof(ModifierActions.setColorHex) => ModifierActions.setColorHex,
            nameof(ModifierActions.setColorHexOther) => ModifierActions.setColorHexOther,

            // rgba
            nameof(ModifierActions.setColorRGBA) => ModifierActions.setColorRGBA,
            nameof(ModifierActions.setColorRGBAOther) => ModifierActions.setColorRGBAOther,

            nameof(ModifierActions.animateColorKF) => ModifierActions.animateColorKF,
            nameof(ModifierActions.animateColorKFHex) => ModifierActions.animateColorKFHex,

            #endregion

            // todo: figure out how to get actorFrameTexture to work
            #region Shape

            nameof(ModifierActions.setShape) => ModifierActions.setShape,
            nameof(ModifierActions.setPolygonShape) => ModifierActions.setPolygonShape,
            nameof(ModifierActions.setPolygonShapeOther) => ModifierActions.setPolygonShapeOther,

            // image
            nameof(ModifierActions.actorFrameTexture) => ModifierActions.actorFrameTexture,
            nameof(ModifierActions.setImage) => ModifierActions.setImage,
            nameof(ModifierActions.setImageOther) => ModifierActions.setImageOther,

            // text (pain)
            nameof(ModifierActions.setText) => ModifierActions.setText,
            nameof(ModifierActions.setTextOther) => ModifierActions.setTextOther,
            nameof(ModifierActions.addText) => ModifierActions.addText,
            nameof(ModifierActions.addTextOther) => ModifierActions.addTextOther,
            nameof(ModifierActions.removeText) => ModifierActions.removeText,
            nameof(ModifierActions.removeTextOther) => ModifierActions.removeTextOther,
            nameof(ModifierActions.removeTextAt) => ModifierActions.removeTextAt,
            nameof(ModifierActions.removeTextOtherAt) => ModifierActions.removeTextOtherAt,
            nameof(ModifierActions.formatText) => ModifierActions.formatText,
            nameof(ModifierActions.textSequence) => ModifierActions.textSequence,

            // modify shape
            nameof(ModifierActions.backgroundShape) => ModifierActions.backgroundShape,
            nameof(ModifierActions.sphereShape) => ModifierActions.sphereShape,
            nameof(ModifierActions.translateShape) => ModifierActions.translateShape,

            #endregion

            #region Animation

            nameof(ModifierActions.animateObject) => ModifierActions.animateObject,
            nameof(ModifierActions.animateObjectOther) => ModifierActions.animateObjectOther,
            nameof(ModifierActions.animateObjectKF) => ModifierActions.animateObjectKF,
            nameof(ModifierActions.animateSignal) => ModifierActions.animateSignal,
            nameof(ModifierActions.animateSignalOther) => ModifierActions.animateSignalOther,

            nameof(ModifierActions.animateObjectMath) => ModifierActions.animateObjectMath,
            nameof(ModifierActions.animateObjectMathOther) => ModifierActions.animateObjectMathOther,
            nameof(ModifierActions.animateSignalMath) => ModifierActions.animateSignalMath,
            nameof(ModifierActions.animateSignalMathOther) => ModifierActions.animateSignalMathOther,

            nameof(ModifierActions.gravity) => ModifierActions.gravity,
            nameof(ModifierActions.gravityOther) => ModifierActions.gravityOther,

            nameof(ModifierActions.copyAxis) => ModifierActions.copyAxis,
            nameof(ModifierActions.copyAxisMath) => ModifierActions.copyAxisMath,
            nameof(ModifierActions.copyAxisGroup) => ModifierActions.copyAxisGroup,
            nameof(ModifierActions.copyPlayerAxis) => ModifierActions.copyPlayerAxis,
            nameof(ModifierActions.legacyTail) => ModifierActions.legacyTail,

            nameof(ModifierActions.applyAnimation) => ModifierActions.applyAnimation,
            nameof(ModifierActions.applyAnimationFrom) => ModifierActions.applyAnimationFrom,
            nameof(ModifierActions.applyAnimationTo) => ModifierActions.applyAnimationTo,
            nameof(ModifierActions.applyAnimationMath) => ModifierActions.applyAnimationMath,
            nameof(ModifierActions.applyAnimationFromMath) => ModifierActions.applyAnimationFromMath,
            nameof(ModifierActions.applyAnimationToMath) => ModifierActions.applyAnimationToMath,

            #endregion

            #region Prefab

            nameof(ModifierActions.spawnPrefab) => ModifierActions.spawnPrefab,
            nameof(ModifierActions.spawnPrefabOffset) => ModifierActions.spawnPrefabOffset,
            nameof(ModifierActions.spawnPrefabOffsetOther) => ModifierActions.spawnPrefabOffsetOther,
            nameof(ModifierActions.spawnMultiPrefab) => ModifierActions.spawnMultiPrefab,
            nameof(ModifierActions.spawnMultiPrefabOffset) => ModifierActions.spawnMultiPrefabOffset,
            nameof(ModifierActions.spawnMultiPrefabOffsetOther) => ModifierActions.spawnMultiPrefabOffsetOther,
            nameof(ModifierActions.clearSpawnedPrefabs) => ModifierActions.clearSpawnedPrefabs,

            #endregion

            #region Ranking

            nameof(ModifierActions.saveLevelRank) => ModifierActions.saveLevelRank,

            nameof(ModifierActions.clearHits) => ModifierActions.clearHits,
            nameof(ModifierActions.addHit) => ModifierActions.addHit,
            nameof(ModifierActions.subHit) => ModifierActions.subHit,
            nameof(ModifierActions.clearDeaths) => ModifierActions.clearDeaths,
            nameof(ModifierActions.addDeath) => ModifierActions.addDeath,
            nameof(ModifierActions.subDeath) => ModifierActions.subDeath,

            #endregion

            #region Updates

            // update
            nameof(ModifierActions.updateObjects) => ModifierActions.updateObjects,
            nameof(ModifierActions.updateObject) => ModifierActions.updateObject,

            // parent
            nameof(ModifierActions.setParent) => ModifierActions.setParent,
            nameof(ModifierActions.setParentOther) => ModifierActions.setParentOther,
            nameof(ModifierActions.detachParent) => ModifierActions.detachParent,
            nameof(ModifierActions.detachParentOther) => ModifierActions.detachParentOther,

            #endregion

            #region Physics

            // collision
            nameof(ModifierActions.setCollision) => ModifierActions.setCollision,
            nameof(ModifierActions.setCollisionOther) => ModifierActions.setCollisionOther,

            #endregion

            #region Checkpoints

            nameof(ModifierActions.createCheckpoint) => ModifierActions.createCheckpoint,
            nameof(ModifierActions.resetCheckpoint) => ModifierActions.resetCheckpoint,

            #endregion

            #region Interfaces

            nameof(ModifierActions.loadInterface) => ModifierActions.loadInterface,
            nameof(ModifierActions.quitToMenu) => ModifierActions.quitToMenu,
            nameof(ModifierActions.quitToArcade) => ModifierActions.quitToArcade,
            nameof(ModifierActions.pauseLevel) => ModifierActions.pauseLevel,

            #endregion

            #region Misc

            nameof(ModifierActions.setBGActive) => ModifierActions.setBGActive,

            // activation
            nameof(ModifierActions.signalModifier) => ModifierActions.signalModifier,
            nameof(ModifierActions.activateModifier) => ModifierActions.activateModifier,

            nameof(ModifierActions.editorNotify) => ModifierActions.editorNotify,

            // external
            nameof(ModifierActions.setWindowTitle) => ModifierActions.setWindowTitle,
            nameof(ModifierActions.setDiscordStatus) => ModifierActions.setDiscordStatus,

            #endregion

            // dev only (story mode)
            #region Dev Only

            "loadSceneDEVONLY" => (modifier, variables) =>
            {
                if (!CoreHelper.InStory)
                    return;

                SceneManager.inst.LoadScene(modifier.GetValue(0, variables), modifier.commands.Count > 1 && modifier.GetBool(1, true, variables));
            },
            "loadStoryLevelDEVONLY" => (modifier, variables) =>
            {
                if (!CoreHelper.InStory)
                    return;

                Story.StoryManager.inst.Play(modifier.GetInt(1, 0, variables), modifier.GetInt(2, 0, variables), modifier.GetBool(0, false, variables), modifier.GetBool(3, false, variables));
            },
            "storySaveIntVariableDEVONLY" => (modifier, variables) =>
            {
                if (!CoreHelper.InStory)
                    return;

                Story.StoryManager.inst.SaveInt(modifier.GetValue(0, variables), modifier.reference.integerVariable);
            },
            "storySaveIntDEVONLY" => (modifier, variables) =>
            {
                if (!CoreHelper.InStory)
                    return;

                Story.StoryManager.inst.SaveInt(modifier.GetValue(0, variables), modifier.GetInt(1, 0, variables));
            },
            "storySaveBoolDEVONLY" => (modifier, variables) =>
            {
                if (!CoreHelper.InStory)
                    return;

                Story.StoryManager.inst.SaveBool(modifier.GetValue(0, variables), modifier.GetBool(1, false, variables));
            },
            "exampleEnableDEVONLY" => (modifier, variables) =>
            {
                if (Companion.Entity.Example.Current && Companion.Entity.Example.Current.model)
                    Companion.Entity.Example.Current.model.SetActive(modifier.GetBool(0, false, variables));
            },
            "exampleSayDEVONLY" => (modifier, variables) =>
            {
                if (Companion.Entity.Example.Current && Companion.Entity.Example.Current.chatBubble)
                    Companion.Entity.Example.Current.chatBubble.Say(modifier.GetValue(0, variables));
            },

            #endregion

            _ => (modifier, variables) => { },
        };

        /// <summary>
        /// The function to run when a modifier is inactive and has a reference of <see cref="BeatmapObject"/>.
        /// </summary>
        /// <param name="modifier">Modifier to run the inactive function of.</param>
        public static void ObjectInactive(Modifier<BeatmapObject> modifier, Dictionary<string, string> variables = null)
        {
            if (!modifier.verified)
            {
                modifier.verified = true;
                modifier.VerifyModifier(ModifiersManager.defaultBeatmapObjectModifiers);
            }

            if (modifier.commands.IsEmpty())
                return;

            try
            {
                switch (modifier.Name)
                {
                    case nameof(ModifierActions.animateColorKF): {
                            modifier.Result = null;
                            break;
                        }
                    case nameof(ModifierActions.animateColorKFHex): {
                            modifier.Result = null;
                            break;
                        }

                    case "blur":
                    case "blurOther":
                    case "blurVariableOther": {
                            if (modifier.Result != null && modifier.reference &&
                                modifier.reference.objectType != BeatmapObject.ObjectType.Empty &&
                                modifier.reference.runtimeObject is RTBeatmapObject levelObject &&
                                levelObject.visualObject.renderer && levelObject.visualObject is SolidObject &&
                                modifier.commands.Count > 2 && bool.TryParse(modifier.commands[2], out bool setNormal) && setNormal)
                            {
                                modifier.Result = null;

                                levelObject.visualObject.renderer.material = ObjectManager.inst.norm;

                                ((SolidObject)levelObject.visualObject).material = levelObject.visualObject.renderer.material;
                            }

                            break;
                        }
                    case "blurVariable": {
                            if (modifier.Result != null && modifier.reference &&
                                modifier.reference.objectType != BeatmapObject.ObjectType.Empty &&
                                modifier.reference.runtimeObject is RTBeatmapObject levelObject &&
                                levelObject.visualObject.renderer && levelObject.visualObject is SolidObject &&
                                modifier.commands.Count > 1 && bool.TryParse(modifier.commands[1], out bool setNormal) && setNormal)
                            {
                                modifier.Result = null;

                                levelObject.visualObject.renderer.material = ObjectManager.inst.norm;

                                ((SolidObject)levelObject.visualObject).material = levelObject.visualObject.renderer.material;
                            }

                            break;
                        }

                    case "spawnPrefab":
                    case "spawnPrefabOffset":
                    case "spawnPrefabOffsetOther": {
                            // value 9 is permanent

                            if (!modifier.constant && modifier.Result is PrefabObject prefabObject && !Parser.TryParse(modifier.commands[9], false))
                            {
                                RTLevel.Current?.UpdatePrefab(prefabObject, false);

                                GameData.Current.prefabObjects.RemoveAll(x => x.fromModifier && x.id == prefabObject.id);

                                modifier.Result = null;
                            }
                            break;
                        }

                    case "enableObject": {
                            if (modifier.reference.runtimeObject is RTBeatmapObject levelObject && levelObject.top && (modifier.commands.Count == 1 || Parser.TryParse(modifier.commands[1], true)))
                                levelObject.top.gameObject.SetActive(false);

                            break;
                        }
                    case "enableObjectOther": {
                            var list = GameData.Current.FindObjectsWithTag(modifier, modifier.GetValue(0));

                            if (!list.IsEmpty() && (modifier.commands.Count == 1 || Parser.TryParse(modifier.commands[1], true)))
                            {
                                foreach (var beatmapObject in list)
                                {
                                    var levelObject = beatmapObject.runtimeObject;
                                    if (levelObject && levelObject.top)
                                        levelObject.top.gameObject.SetActive(false);
                                }
                            }

                            break;
                        }
                    case "enableObjectTree": {
                            if (modifier.GetValue(0) == "0")
                                modifier.SetValue(0, "False");

                            if (modifier.commands.Count > 1 && !Parser.TryParse(modifier.commands[1], true))
                            {
                                modifier.Result = null;
                                return;
                            }

                            if (modifier.Result == null)
                            {
                                var beatmapObject = Parser.TryParse(modifier.value, true) ? modifier.reference : modifier.reference.GetParentChain().Last();

                                modifier.Result = beatmapObject.GetChildTree();
                            }

                            var list = (List<BeatmapObject>)modifier.Result;

                            for (int i = 0; i < list.Count; i++)
                            {
                                var beatmapObject = list[i];
                                var levelObject = beatmapObject.runtimeObject;
                                if (levelObject && levelObject.top)
                                    levelObject.top.gameObject.SetActive(false);
                            }

                            break;
                        }
                    case "enableObjectTreeOther": {
                            if (modifier.commands.Count > 2 && !Parser.TryParse(modifier.commands[2], true))
                            {
                                modifier.Result = null;
                                return;
                            }

                            if (modifier.Result == null)
                            {
                                var beatmapObjects = GameData.Current.FindObjectsWithTag(modifier, modifier.GetValue(1));

                                var resultList = new List<BeatmapObject>();
                                foreach (var bm in beatmapObjects)
                                {
                                    var beatmapObject = Parser.TryParse(modifier.value, true) ? bm : bm.GetParentChain().Last();
                                    resultList.AddRange(beatmapObject.GetChildTree());
                                }

                                modifier.Result = resultList;
                            }

                            var list = (List<BeatmapObject>)modifier.Result;

                            for (int i = 0; i < list.Count; i++)
                            {
                                var beatmapObject = list[i];
                                var levelObject = beatmapObject.runtimeObject;
                                if (levelObject && levelObject.top)
                                    levelObject.top.gameObject.SetActive(false);
                            }

                            modifier.Result = null;

                            break;
                        }
                    case "disableObject": {
                            if (!modifier.hasChanged && modifier.reference != null && modifier.reference.runtimeObject is RTBeatmapObject levelObject && levelObject.top && (modifier.commands.Count == 1 || Parser.TryParse(modifier.commands[1], true)))
                            {
                                levelObject.top.gameObject.SetActive(true);
                                modifier.hasChanged = true;
                            }

                            break;
                        }
                    case "disableObjectOther": {
                            var list = GameData.Current.FindObjectsWithTag(modifier, modifier.GetValue(0));

                            if (!list.IsEmpty() && (modifier.commands.Count == 1 || Parser.TryParse(modifier.commands[1], true)))
                            {
                                foreach (var beatmapObject in list)
                                {
                                    var levelObject = beatmapObject.runtimeObject;
                                    if (levelObject && levelObject.top)
                                        levelObject.top.gameObject.SetActive(true);
                                }
                            }

                            break;
                        }
                    case "disableObjectTree": {
                            if (modifier.GetValue(0) == "0")
                                modifier.SetValue(0, "False");

                            if (modifier.commands.Count > 1 && !Parser.TryParse(modifier.commands[1], true))
                            {
                                modifier.Result = null;
                                return;
                            }

                            if (modifier.Result == null)
                            {
                                var beatmapObject = Parser.TryParse(modifier.value, true) ? modifier.reference : modifier.reference.GetParentChain().Last();

                                modifier.Result = beatmapObject.GetChildTree();
                            }

                            var list = (List<BeatmapObject>)modifier.Result;

                            for (int i = 0; i < list.Count; i++)
                            {
                                var beatmapObject = list[i];
                                var levelObject = beatmapObject.runtimeObject;
                                if (levelObject && levelObject.top)
                                    levelObject.top.gameObject.SetActive(true);
                            }

                            break;
                        }
                    case "disableObjectTreeOther": {
                            if (modifier.commands.Count > 2 && !Parser.TryParse(modifier.commands[2], true))
                            {
                                modifier.Result = null;
                                return;
                            }

                            if (modifier.Result == null)
                            {
                                var beatmapObjects = GameData.Current.FindObjectsWithTag(modifier, modifier.GetValue(1));

                                var resultList = new List<BeatmapObject>();
                                foreach (var bm in beatmapObjects)
                                {
                                    var beatmapObject = Parser.TryParse(modifier.value, true) ? bm : bm.GetParentChain().Last();
                                    resultList.AddRange(beatmapObject.GetChildTree());
                                }

                                modifier.Result = resultList;
                            }

                            var list = (List<BeatmapObject>)modifier.Result;

                            for (int i = 0; i < list.Count; i++)
                            {
                                var beatmapObject = list[i];
                                var levelObject = beatmapObject.runtimeObject;
                                if (levelObject && levelObject.top)
                                    levelObject.top.gameObject.SetActive(true);
                            }

                            modifier.Result = null;

                            break;
                        }

                    case "reactivePosChain": {
                            modifier.reference.reactivePositionOffset = Vector3.zero;

                            break;
                        }
                    case "reactiveScaChain": {
                            modifier.reference.reactiveScaleOffset = Vector3.zero;

                            break;
                        }
                    case "reactiveRotChain": {
                            modifier.reference.reactiveRotationOffset = 0f;

                            break;
                        }
                    case "signalModifier":
                    case "mouseOverSignalModifier": {
                            var list = GameData.Current.FindObjectsWithTag(modifier, modifier.GetValue(1));

                            if (!list.IsEmpty() && list.Any(x => x.modifiers.Any(y => y.Result != null)))
                                foreach (var bm in list)
                                {
                                    if (!bm.modifiers.IsEmpty() && bm.modifiers.TryFind(x => x.Name == "requireSignal" && x.type == ModifierBase.Type.Trigger, out Modifier<BeatmapObject> m))
                                        m.Result = null;
                                }

                            break;
                        }
                    case "animateSignal":
                    case "animateSignalOther": {
                            if (!Parser.TryParse(modifier.commands[!modifier.commands[0].Contains("Other") ? 9 : 10], true))
                                return;

                            int groupIndex = !modifier.commands[0].Contains("Other") ? 7 : 8;
                            var list = GameData.Current.FindObjectsWithTag(modifier, modifier.GetValue(groupIndex));

                            if (!list.IsEmpty() && !modifier.constant)
                                foreach (var bm in list)
                                {
                                    if (!bm.modifiers.IsEmpty() && bm.modifiers.TryFind(x => x.Name == "requireSignal" && x.type == ModifierBase.Type.Trigger, out Modifier<BeatmapObject> m))
                                        m.Result = null;
                                }

                            break;
                        }
                    case "randomGreater":
                    case "randomLesser":
                    case "randomEquals":
                    case "gravity":
                    case "gravityOther": {
                            modifier.Result = null;
                            break;
                        }
                    case "setText": {
                            if (modifier.constant && modifier.reference.ShapeType == ShapeType.Text && modifier.reference.runtimeObject && modifier.reference.runtimeObject.visualObject != null &&
                                modifier.reference.runtimeObject.visualObject is TextObject textObject)
                                textObject.text = modifier.reference.text;
                            break;
                        }
                    case "setTextOther": {
                            var list = GameData.Current.FindObjectsWithTag(modifier, modifier.GetValue(1));

                            if (modifier.constant && !list.IsEmpty())
                                foreach (var bm in list)
                                    if (bm.ShapeType == ShapeType.Text && bm.runtimeObject && bm.runtimeObject.visualObject != null &&
                                        bm.runtimeObject.visualObject is TextObject textObject)
                                        textObject.text = bm.text;
                            break;
                        }
                    case "textSequence": {
                            modifier.setTimer = false;
                            break;
                        }
                    case "copyAxis":
                    case "copyAxisMath":
                    case "copyAxisGroup":
                    case "objectSpawned": {
                            modifier.Result = null;
                            break;
                        }
                    case "applyAnimation":
                    case "applyAnimationFrom":
                    case "applyAnimationTo": {
                            if (modifier.constant)
                                modifier.Result = null;

                            break;
                        }
                }
            }
            catch (Exception ex)
            {
                CoreHelper.LogError($"Modifier ({modifier.commands[0]}) had an error. {ex}");
            }
        }

        #endregion

        #region BackgroundObject

        public static Func<Modifier<BackgroundObject>, Dictionary<string, string>, bool> GetBGTrigger(string key) => key switch
        {
            "timeLesserEquals" => (modifier, variables) => AudioManager.inst.CurrentAudioSource.time <= modifier.GetFloat(0, 0f, variables),
            "timeGreaterEquals" => (modifier, variables) => AudioManager.inst.CurrentAudioSource.time >= modifier.GetFloat(0, 0f, variables),
            "timeLesser" => (modifier, variables)  => AudioManager.inst.CurrentAudioSource.time < modifier.GetFloat(0, 0f, variables),
            "timeGreater" => (modifier, variables)  => AudioManager.inst.CurrentAudioSource.time > modifier.GetFloat(0, 0f, variables),

            #region Player

            //nameof(ModifierTriggers.playerCollide) => ModifierTriggers.playerCollide,
            nameof(ModifierTriggers.playerHealthEquals) => ModifierTriggers.playerHealthEquals,
            nameof(ModifierTriggers.playerHealthLesserEquals) => ModifierTriggers.playerHealthLesserEquals,
            nameof(ModifierTriggers.playerHealthGreaterEquals) => ModifierTriggers.playerHealthGreaterEquals,
            nameof(ModifierTriggers.playerHealthLesser) => ModifierTriggers.playerHealthLesser,
            nameof(ModifierTriggers.playerHealthGreater) => ModifierTriggers.playerHealthGreater,
            nameof(ModifierTriggers.playerMoving) => ModifierTriggers.playerMoving,
            //nameof(ModifierTriggers.playerBoosting) => ModifierTriggers.playerBoosting,
            //nameof(ModifierTriggers.playerAlive) => ModifierTriggers.playerAlive,
            nameof(ModifierTriggers.playerDeathsEquals) => ModifierTriggers.playerDeathsEquals,
            nameof(ModifierTriggers.playerDeathsLesserEquals) => ModifierTriggers.playerDeathsLesserEquals,
            nameof(ModifierTriggers.playerDeathsGreaterEquals) => ModifierTriggers.playerDeathsGreaterEquals,
            nameof(ModifierTriggers.playerDeathsLesser) => ModifierTriggers.playerDeathsLesser,
            nameof(ModifierTriggers.playerDeathsGreater) => ModifierTriggers.playerDeathsGreater,
            nameof(ModifierTriggers.playerDistanceGreater) => ModifierTriggers.playerDistanceGreater,
            nameof(ModifierTriggers.playerDistanceLesser) => ModifierTriggers.playerDistanceLesser,
            nameof(ModifierTriggers.playerCountEquals) => ModifierTriggers.playerCountEquals,
            nameof(ModifierTriggers.playerCountLesserEquals) => ModifierTriggers.playerCountLesserEquals,
            nameof(ModifierTriggers.playerCountGreaterEquals) => ModifierTriggers.playerCountGreaterEquals,
            nameof(ModifierTriggers.playerCountLesser) => ModifierTriggers.playerCountLesser,
            nameof(ModifierTriggers.playerCountGreater) => ModifierTriggers.playerCountGreater,
            nameof(ModifierTriggers.onPlayerHit) => ModifierTriggers.onPlayerHit,
            nameof(ModifierTriggers.onPlayerDeath) => ModifierTriggers.onPlayerDeath,
            nameof(ModifierTriggers.playerBoostEquals) => ModifierTriggers.playerBoostEquals,
            nameof(ModifierTriggers.playerBoostLesserEquals) => ModifierTriggers.playerBoostLesserEquals,
            nameof(ModifierTriggers.playerBoostGreaterEquals) => ModifierTriggers.playerBoostGreaterEquals,
            nameof(ModifierTriggers.playerBoostLesser) => ModifierTriggers.playerBoostLesser,
            nameof(ModifierTriggers.playerBoostGreater) => ModifierTriggers.playerBoostGreater,

            #endregion

            #region Controls

            nameof(ModifierTriggers.keyPressDown) => ModifierTriggers.keyPressDown,
            nameof(ModifierTriggers.keyPress) => ModifierTriggers.keyPress,
            nameof(ModifierTriggers.keyPressUp) => ModifierTriggers.keyPressUp,
            nameof(ModifierTriggers.mouseButtonDown) => ModifierTriggers.mouseButtonDown,
            nameof(ModifierTriggers.mouseButton) => ModifierTriggers.mouseButton,
            nameof(ModifierTriggers.mouseButtonUp) => ModifierTriggers.mouseButtonUp,
            //nameof(ModifierTriggers.mouseOver) => ModifierTriggers.mouseOver,
            //nameof(ModifierTriggers.mouseOverSignalModifier) => ModifierTriggers.mouseOverSignalModifier,
            nameof(ModifierTriggers.controlPressDown) => ModifierTriggers.controlPressDown,
            nameof(ModifierTriggers.controlPress) => ModifierTriggers.controlPress,
            nameof(ModifierTriggers.controlPressUp) => ModifierTriggers.controlPressUp,

            #endregion

            #region Collide

            //nameof(ModifierTriggers.bulletCollide) => ModifierTriggers.bulletCollide,
            //nameof(ModifierTriggers.objectCollide) => ModifierTriggers.objectCollide,

            #endregion

            #region JSON

            nameof(ModifierTriggers.loadEquals) => ModifierTriggers.loadEquals,
            nameof(ModifierTriggers.loadLesserEquals) => ModifierTriggers.loadLesserEquals,
            nameof(ModifierTriggers.loadGreaterEquals) => ModifierTriggers.loadGreaterEquals,
            nameof(ModifierTriggers.loadLesser) => ModifierTriggers.loadLesser,
            nameof(ModifierTriggers.loadGreater) => ModifierTriggers.loadGreater,
            nameof(ModifierTriggers.loadExists) => ModifierTriggers.loadExists,

            #endregion

            #region Variable

            nameof(ModifierTriggers.localVariableEquals) => ModifierTriggers.localVariableEquals,
            nameof(ModifierTriggers.localVariableLesserEquals) => ModifierTriggers.localVariableLesserEquals,
            nameof(ModifierTriggers.localVariableGreaterEquals) => ModifierTriggers.localVariableGreaterEquals,
            nameof(ModifierTriggers.localVariableLesser) => ModifierTriggers.localVariableLesser,
            nameof(ModifierTriggers.localVariableGreater) => ModifierTriggers.localVariableGreater,
            nameof(ModifierTriggers.localVariableContains) => ModifierTriggers.localVariableContains,
            nameof(ModifierTriggers.localVariableStartsWith) => ModifierTriggers.localVariableStartsWith,
            nameof(ModifierTriggers.localVariableEndsWith) => ModifierTriggers.localVariableEndsWith,
            nameof(ModifierTriggers.localVariableExists) => ModifierTriggers.localVariableExists,

            // self
            nameof(ModifierTriggers.variableEquals) => ModifierTriggers.variableEquals,
            nameof(ModifierTriggers.variableLesserEquals) => ModifierTriggers.variableLesserEquals,
            nameof(ModifierTriggers.variableGreaterEquals) => ModifierTriggers.variableGreaterEquals,
            nameof(ModifierTriggers.variableLesser) => ModifierTriggers.variableLesser,
            nameof(ModifierTriggers.variableGreater) => ModifierTriggers.variableGreater,

            // other
            //nameof(ModifierTriggers.variableOtherEquals) => ModifierTriggers.variableOtherEquals,
            //nameof(ModifierTriggers.variableOtherLesserEquals) => ModifierTriggers.variableOtherLesserEquals,
            //nameof(ModifierTriggers.variableOtherGreaterEquals) => ModifierTriggers.variableOtherGreaterEquals,
            //nameof(ModifierTriggers.variableOtherLesser) => ModifierTriggers.variableOtherLesser,
            //nameof(ModifierTriggers.variableOtherGreater) => ModifierTriggers.variableOtherGreater,

            #endregion

            #region Audio

            nameof(ModifierTriggers.pitchEquals) => ModifierTriggers.pitchEquals,
            nameof(ModifierTriggers.pitchLesserEquals) => ModifierTriggers.pitchLesserEquals,
            nameof(ModifierTriggers.pitchGreaterEquals) => ModifierTriggers.pitchGreaterEquals,
            nameof(ModifierTriggers.pitchLesser) => ModifierTriggers.pitchLesser,
            nameof(ModifierTriggers.pitchGreater) => ModifierTriggers.pitchGreater,
            nameof(ModifierTriggers.musicTimeGreater) => ModifierTriggers.musicTimeGreater,
            nameof(ModifierTriggers.musicTimeLesser) => ModifierTriggers.musicTimeLesser,
            nameof(ModifierTriggers.musicPlaying) => ModifierTriggers.musicPlaying,

            #endregion

            #region Challenge Mode

            nameof(ModifierTriggers.inZenMode) => ModifierTriggers.inZenMode,
            nameof(ModifierTriggers.inNormal) => ModifierTriggers.inNormal,
            nameof(ModifierTriggers.in1Life) => ModifierTriggers.in1Life,
            nameof(ModifierTriggers.inNoHit) => ModifierTriggers.inNoHit,
            nameof(ModifierTriggers.inPractice) => ModifierTriggers.inPractice,

            #endregion

            #region Random

            nameof(ModifierTriggers.randomEquals) => ModifierTriggers.randomEquals,
            nameof(ModifierTriggers.randomLesser) => ModifierTriggers.randomLesser,
            nameof(ModifierTriggers.randomGreater) => ModifierTriggers.randomGreater,

            #endregion

            #region Math

            nameof(ModifierTriggers.mathEquals) => ModifierTriggers.mathEquals,
            nameof(ModifierTriggers.mathLesserEquals) => ModifierTriggers.mathLesserEquals,
            nameof(ModifierTriggers.mathGreaterEquals) => ModifierTriggers.mathGreaterEquals,
            nameof(ModifierTriggers.mathLesser) => ModifierTriggers.mathLesser,
            nameof(ModifierTriggers.mathGreater) => ModifierTriggers.mathGreater,

            #endregion

            #region Axis

            nameof(ModifierTriggers.axisEquals) => ModifierTriggers.axisEquals,
            nameof(ModifierTriggers.axisLesserEquals) => ModifierTriggers.axisLesserEquals,
            nameof(ModifierTriggers.axisGreaterEquals) => ModifierTriggers.axisGreaterEquals,
            nameof(ModifierTriggers.axisLesser) => ModifierTriggers.axisLesser,
            nameof(ModifierTriggers.axisGreater) => ModifierTriggers.axisGreater,

            #endregion

            #region Level Rank

            // self
            nameof(ModifierTriggers.levelRankEquals) => ModifierTriggers.levelRankEquals,
            nameof(ModifierTriggers.levelRankLesserEquals) => ModifierTriggers.levelRankLesserEquals,
            nameof(ModifierTriggers.levelRankGreaterEquals) => ModifierTriggers.levelRankGreaterEquals,
            nameof(ModifierTriggers.levelRankLesser) => ModifierTriggers.levelRankLesser,
            nameof(ModifierTriggers.levelRankGreater) => ModifierTriggers.levelRankGreater,

            // other
            nameof(ModifierTriggers.levelRankOtherEquals) => ModifierTriggers.levelRankOtherEquals,
            nameof(ModifierTriggers.levelRankOtherLesserEquals) => ModifierTriggers.levelRankOtherLesserEquals,
            nameof(ModifierTriggers.levelRankOtherGreaterEquals) => ModifierTriggers.levelRankOtherGreaterEquals,
            nameof(ModifierTriggers.levelRankOtherLesser) => ModifierTriggers.levelRankOtherLesser,
            nameof(ModifierTriggers.levelRankOtherGreater) => ModifierTriggers.levelRankOtherGreater,

            // current
            nameof(ModifierTriggers.levelRankCurrentEquals) => ModifierTriggers.levelRankCurrentEquals,
            nameof(ModifierTriggers.levelRankCurrentLesserEquals) => ModifierTriggers.levelRankCurrentLesserEquals,
            nameof(ModifierTriggers.levelRankCurrentGreaterEquals) => ModifierTriggers.levelRankCurrentGreaterEquals,
            nameof(ModifierTriggers.levelRankCurrentLesser) => ModifierTriggers.levelRankCurrentLesser,
            nameof(ModifierTriggers.levelRankCurrentGreater) => ModifierTriggers.levelRankCurrentGreater,

            #endregion

            #region Level

            nameof(ModifierTriggers.levelUnlocked) => ModifierTriggers.levelUnlocked,
            nameof(ModifierTriggers.levelCompleted) => ModifierTriggers.levelCompleted,
            nameof(ModifierTriggers.levelCompletedOther) => ModifierTriggers.levelCompletedOther,
            nameof(ModifierTriggers.levelExists) => ModifierTriggers.levelExists,
            nameof(ModifierTriggers.levelPathExists) => ModifierTriggers.levelPathExists,

            #endregion

            #region Real Time

            // seconds
            nameof(ModifierTriggers.realTimeSecondEquals) => ModifierTriggers.realTimeSecondEquals,
            nameof(ModifierTriggers.realTimeSecondLesserEquals) => ModifierTriggers.realTimeSecondLesserEquals,
            nameof(ModifierTriggers.realTimeSecondGreaterEquals) => ModifierTriggers.realTimeSecondGreaterEquals,
            nameof(ModifierTriggers.realTimeSecondLesser) => ModifierTriggers.realTimeSecondLesser,
            nameof(ModifierTriggers.realTimeSecondGreater) => ModifierTriggers.realTimeSecondGreater,

            // minutes
            nameof(ModifierTriggers.realTimeMinuteEquals) => ModifierTriggers.realTimeMinuteEquals,
            nameof(ModifierTriggers.realTimeMinuteLesserEquals) => ModifierTriggers.realTimeMinuteLesserEquals,
            nameof(ModifierTriggers.realTimeMinuteGreaterEquals) => ModifierTriggers.realTimeMinuteGreaterEquals,
            nameof(ModifierTriggers.realTimeMinuteLesser) => ModifierTriggers.realTimeMinuteLesser,
            nameof(ModifierTriggers.realTimeMinuteGreater) => ModifierTriggers.realTimeMinuteGreater,

            // 24 hours
            nameof(ModifierTriggers.realTime24HourEquals) => ModifierTriggers.realTime24HourEquals,
            nameof(ModifierTriggers.realTime24HourLesserEquals) => ModifierTriggers.realTime24HourLesserEquals,
            nameof(ModifierTriggers.realTime24HourGreaterEquals) => ModifierTriggers.realTime24HourGreaterEquals,
            nameof(ModifierTriggers.realTime24HourLesser) => ModifierTriggers.realTime24HourLesser,
            nameof(ModifierTriggers.realTime24HourGreater) => ModifierTriggers.realTime24HourGreater,

            // 12 hours
            nameof(ModifierTriggers.realTime12HourEquals) => ModifierTriggers.realTime12HourEquals,
            nameof(ModifierTriggers.realTime12HourLesserEquals) => ModifierTriggers.realTime12HourLesserEquals,
            nameof(ModifierTriggers.realTime12HourGreaterEquals) => ModifierTriggers.realTime12HourGreaterEquals,
            nameof(ModifierTriggers.realTime12HourLesser) => ModifierTriggers.realTime12HourLesser,
            nameof(ModifierTriggers.realTime12HourGreater) => ModifierTriggers.realTime12HourGreater,

            // days
            nameof(ModifierTriggers.realTimeDayEquals) => ModifierTriggers.realTimeDayEquals,
            nameof(ModifierTriggers.realTimeDayLesserEquals) => ModifierTriggers.realTimeDayLesserEquals,
            nameof(ModifierTriggers.realTimeDayGreaterEquals) => ModifierTriggers.realTimeDayGreaterEquals,
            nameof(ModifierTriggers.realTimeDayLesser) => ModifierTriggers.realTimeDayLesser,
            nameof(ModifierTriggers.realTimeDayGreater) => ModifierTriggers.realTimeDayGreater,
            nameof(ModifierTriggers.realTimeDayWeekEquals) => ModifierTriggers.realTimeDayWeekEquals,

            // months
            nameof(ModifierTriggers.realTimeMonthEquals) => ModifierTriggers.realTimeMonthEquals,
            nameof(ModifierTriggers.realTimeMonthLesserEquals) => ModifierTriggers.realTimeMonthLesserEquals,
            nameof(ModifierTriggers.realTimeMonthGreaterEquals) => ModifierTriggers.realTimeMonthGreaterEquals,
            nameof(ModifierTriggers.realTimeMonthLesser) => ModifierTriggers.realTimeMonthLesser,
            nameof(ModifierTriggers.realTimeMonthGreater) => ModifierTriggers.realTimeMonthGreater,

            // years
            nameof(ModifierTriggers.realTimeYearEquals) => ModifierTriggers.realTimeYearEquals,
            nameof(ModifierTriggers.realTimeYearLesserEquals) => ModifierTriggers.realTimeYearLesserEquals,
            nameof(ModifierTriggers.realTimeYearGreaterEquals) => ModifierTriggers.realTimeYearGreaterEquals,
            nameof(ModifierTriggers.realTimeYearLesser) => ModifierTriggers.realTimeYearLesser,
            nameof(ModifierTriggers.realTimeYearGreater) => ModifierTriggers.realTimeYearGreater,

            #endregion

            #region Config

            // main
            nameof(ModifierTriggers.usernameEquals) => ModifierTriggers.usernameEquals,
            nameof(ModifierTriggers.languageEquals) => ModifierTriggers.languageEquals,

            // misc
            nameof(ModifierTriggers.configLDM) => ModifierTriggers.configLDM,
            nameof(ModifierTriggers.configShowEffects) => ModifierTriggers.configShowEffects,
            nameof(ModifierTriggers.configShowPlayerGUI) => ModifierTriggers.configShowPlayerGUI,
            nameof(ModifierTriggers.configShowIntro) => ModifierTriggers.configShowIntro,

            #endregion

            #region Misc

            nameof(ModifierTriggers.inEditor) => ModifierTriggers.inEditor,
            nameof(ModifierTriggers.requireSignal) => ModifierTriggers.requireSignal,
            nameof(ModifierTriggers.isFullscreen) => ModifierTriggers.isFullscreen,
            //nameof(ModifierTriggers.objectAlive) => ModifierTriggers.objectAlive,
            //nameof(ModifierTriggers.objectSpawned) => ModifierTriggers.objectSpawned,

            #endregion

            #region Dev Only

            "storyLoadIntEqualsDEVONLY" => (modifier, variables) =>
            {
                return Story.StoryManager.inst.LoadInt(modifier.GetValue(0, variables), modifier.GetInt(1, 0, variables)) == modifier.GetInt(2, 0, variables);
            },
            "storyLoadIntLesserEqualsDEVONLY" => (modifier, variables) =>
            {
                return Story.StoryManager.inst.LoadInt(modifier.GetValue(0, variables), modifier.GetInt(1, 0, variables)) <= modifier.GetInt(2, 0, variables);
            },
            "storyLoadIntGreaterEqualsDEVONLY" => (modifier, variables) =>
            {
                return Story.StoryManager.inst.LoadInt(modifier.GetValue(0, variables), modifier.GetInt(1, 0, variables)) >= modifier.GetInt(2, 0, variables);
            },
            "storyLoadIntLesserDEVONLY" => (modifier, variables) =>
            {
                return Story.StoryManager.inst.LoadInt(modifier.GetValue(0, variables), modifier.GetInt(1, 0, variables)) < modifier.GetInt(2, 0, variables);
            },
            "storyLoadIntGreaterDEVONLY" => (modifier, variables) =>
            {
                return Story.StoryManager.inst.LoadInt(modifier.GetValue(0, variables), modifier.GetInt(1, 0, variables)) > modifier.GetInt(2, 0, variables);
            },
            "storyLoadBoolDEVONLY" => (modifier, variables) =>
            {
                return Story.StoryManager.inst.LoadBool(modifier.GetValue(0, variables), modifier.GetBool(1, false));
            },

            #endregion

            "break" => (modifier, variables) => true,
            _ => (modifier, variables) => false,
        };

        public static Action<Modifier<BackgroundObject>, Dictionary<string, string>> GetBGAction(string key) => key switch
        {
            "setActive" => ModifierActions.setActive,
            "setActiveOther" => ModifierActions.setActiveOther,

            #region Audio

            // pitch
            nameof(ModifierActions.setPitch) => ModifierActions.setPitch,
            nameof(ModifierActions.addPitch) => ModifierActions.addPitch,
            nameof(ModifierActions.setPitchMath) => ModifierActions.setPitchMath,
            nameof(ModifierActions.addPitchMath) => ModifierActions.addPitchMath,

            // music playing states
            nameof(ModifierActions.setMusicTime) => ModifierActions.setMusicTime,
            nameof(ModifierActions.setMusicTimeMath) => ModifierActions.setMusicTimeMath,
            nameof(ModifierActions.setMusicTimeStartTime) => ModifierActions.setMusicTimeStartTime,
            nameof(ModifierActions.setMusicTimeAutokill) => ModifierActions.setMusicTimeAutokill,
            nameof(ModifierActions.setMusicPlaying) => ModifierActions.setMusicPlaying,

            // play sound
            nameof(ModifierActions.playSound) => ModifierActions.playSound,
            nameof(ModifierActions.playSoundOnline) => ModifierActions.playSoundOnline,
            nameof(ModifierActions.playDefaultSound) => ModifierActions.playDefaultSound,
            //nameof(ModifierActions.audioSource) => ModifierActions.audioSource,

            #endregion

            #region Level

            nameof(ModifierActions.loadLevel) => ModifierActions.loadLevel,
            nameof(ModifierActions.loadLevelID) => ModifierActions.loadLevelID,
            nameof(ModifierActions.loadLevelInternal) => ModifierActions.loadLevelInternal,
            nameof(ModifierActions.loadLevelPrevious) => ModifierActions.loadLevelPrevious,
            nameof(ModifierActions.loadLevelHub) => ModifierActions.loadLevelHub,
            nameof(ModifierActions.loadLevelInCollection) => ModifierActions.loadLevelInCollection,
            nameof(ModifierActions.downloadLevel) => ModifierActions.downloadLevel,
            nameof(ModifierActions.endLevel) => ModifierActions.endLevel,
            nameof(ModifierActions.setAudioTransition) => ModifierActions.setAudioTransition,
            nameof(ModifierActions.setIntroFade) => ModifierActions.setIntroFade,
            nameof(ModifierActions.setLevelEndFunc) => ModifierActions.setLevelEndFunc,

            #endregion

            #region Component

            //nameof(ModifierActions.blur) => ModifierActions.blur,
            //nameof(ModifierActions.blurOther) => ModifierActions.blurOther,
            //nameof(ModifierActions.blurVariable) => ModifierActions.blurVariable,
            //nameof(ModifierActions.blurVariableOther) => ModifierActions.blurVariableOther,
            //nameof(ModifierActions.blurColored) => ModifierActions.blurColored,
            //nameof(ModifierActions.blurColoredOther) => ModifierActions.blurColoredOther,
            //nameof(ModifierActions.doubleSided) => ModifierActions.doubleSided,
            //nameof(ModifierActions.particleSystem) => ModifierActions.particleSystem,
            //nameof(ModifierActions.trailRenderer) => ModifierActions.trailRenderer,
            //nameof(ModifierActions.rigidbody) => ModifierActions.rigidbody,
            //nameof(ModifierActions.rigidbodyOther) => ModifierActions.rigidbodyOther,

            #endregion

            #region Player

            // hit
            //nameof(ModifierActions.playerHit) => ModifierActions.playerHit,
            nameof(ModifierActions.playerHitIndex) => ModifierActions.playerHitIndex,
            nameof(ModifierActions.playerHitAll) => ModifierActions.playerHitAll,

            // heal
            //nameof(ModifierActions.playerHeal) => ModifierActions.playerHeal,
            nameof(ModifierActions.playerHealIndex) => ModifierActions.playerHealIndex,
            nameof(ModifierActions.playerHealAll) => ModifierActions.playerHealAll,

            // kill
            //nameof(ModifierActions.playerKill) => ModifierActions.playerKill,
            nameof(ModifierActions.playerKillIndex) => ModifierActions.playerKillIndex,
            nameof(ModifierActions.playerKillAll) => ModifierActions.playerKillAll,

            // respawn
            //nameof(ModifierActions.playerRespawn) => ModifierActions.playerRespawn,
            nameof(ModifierActions.playerRespawnIndex) => ModifierActions.playerRespawnIndex,
            nameof(ModifierActions.playerRespawnAll) => ModifierActions.playerRespawnAll,

            // player move
            //nameof(ModifierActions.playerMove) => ModifierActions.playerMove,
            nameof(ModifierActions.playerMoveIndex) => ModifierActions.playerMoveIndex,
            nameof(ModifierActions.playerMoveAll) => ModifierActions.playerMoveAll,
            //nameof(ModifierActions.playerMoveX) => ModifierActions.playerMoveX,
            nameof(ModifierActions.playerMoveXIndex) => ModifierActions.playerMoveXIndex,
            nameof(ModifierActions.playerMoveXAll) => ModifierActions.playerMoveXAll,
            //nameof(ModifierActions.playerMoveY) => ModifierActions.playerMoveY,
            nameof(ModifierActions.playerMoveYIndex) => ModifierActions.playerMoveYIndex,
            nameof(ModifierActions.playerMoveYAll) => ModifierActions.playerMoveYAll,
            //nameof(ModifierActions.playerRotate) => ModifierActions.playerRotate,
            nameof(ModifierActions.playerRotateIndex) => ModifierActions.playerRotateIndex,
            nameof(ModifierActions.playerRotateAll) => ModifierActions.playerRotateAll,

            // move to object
            //nameof(ModifierActions.playerMoveToObject) => ModifierActions.playerMoveToObject,
            //nameof(ModifierActions.playerMoveIndexToObject) => ModifierActions.playerMoveIndexToObject,
            //nameof(ModifierActions.playerMoveAllToObject) => ModifierActions.playerMoveAllToObject,
            //nameof(ModifierActions.playerMoveXToObject) => ModifierActions.playerMoveXToObject,
            //nameof(ModifierActions.playerMoveXIndexToObject) => ModifierActions.playerMoveXIndexToObject,
            //nameof(ModifierActions.playerMoveXAllToObject) => ModifierActions.playerMoveXAllToObject,
            //nameof(ModifierActions.playerMoveYToObject) => ModifierActions.playerMoveYToObject,
            //nameof(ModifierActions.playerMoveYIndexToObject) => ModifierActions.playerMoveYIndexToObject,
            //nameof(ModifierActions.playerMoveYAllToObject) => ModifierActions.playerMoveYAllToObject,
            //nameof(ModifierActions.playerRotateToObject) => ModifierActions.playerRotateToObject,
            //nameof(ModifierActions.playerRotateIndexToObject) => ModifierActions.playerRotateIndexToObject,
            //nameof(ModifierActions.playerRotateAllToObject) => ModifierActions.playerRotateAllToObject,

            // actions
            //nameof(ModifierActions.playerBoost) => ModifierActions.playerBoost,
            nameof(ModifierActions.playerBoostIndex) => ModifierActions.playerBoostIndex,
            nameof(ModifierActions.playerBoostAll) => ModifierActions.playerBoostAll,
            //nameof(ModifierActions.playerDisableBoost) => ModifierActions.playerDisableBoost,
            nameof(ModifierActions.playerDisableBoostIndex) => ModifierActions.playerDisableBoostIndex,
            nameof(ModifierActions.playerDisableBoostAll) => ModifierActions.playerDisableBoostAll,
            //nameof(ModifierActions.playerEnableBoost) => ModifierActions.playerEnableBoost,
            nameof(ModifierActions.playerEnableBoostIndex) => ModifierActions.playerEnableBoostIndex,
            nameof(ModifierActions.playerEnableBoostAll) => ModifierActions.playerEnableBoostAll,

            // speed
            nameof(ModifierActions.playerSpeed) => ModifierActions.playerSpeed,
            nameof(ModifierActions.playerVelocityAll) => ModifierActions.playerVelocityAll,
            nameof(ModifierActions.playerVelocityXAll) => ModifierActions.playerVelocityXAll,
            nameof(ModifierActions.playerVelocityYAll) => ModifierActions.playerVelocityYAll,

            nameof(ModifierActions.setPlayerModel) => ModifierActions.setPlayerModel,
            nameof(ModifierActions.setGameMode) => ModifierActions.setGameMode,
            nameof(ModifierActions.gameMode) => ModifierActions.gameMode,

            //nameof(ModifierActions.blackHole) => ModifierActions.blackHole,

            #endregion

            #region Mouse Cursor

            nameof(ModifierActions.showMouse) => ModifierActions.showMouse,
            nameof(ModifierActions.hideMouse) => ModifierActions.hideMouse,
            nameof(ModifierActions.setMousePosition) => ModifierActions.setMousePosition,
            nameof(ModifierActions.followMousePosition) => ModifierActions.followMousePosition,

            #endregion

            #region Variable

            nameof(ModifierActions.getToggle) => ModifierActions.getToggle,
            nameof(ModifierActions.getFloat) => ModifierActions.getFloat,
            nameof(ModifierActions.getInt) => ModifierActions.getInt,
            nameof(ModifierActions.getString) => ModifierActions.getString,
            nameof(ModifierActions.getStringLower) => ModifierActions.getStringLower,
            nameof(ModifierActions.getStringUpper) => ModifierActions.getStringUpper,
            nameof(ModifierActions.getColor) => ModifierActions.getColor,
            nameof(ModifierActions.getEnum) => ModifierActions.getEnum,
            nameof(ModifierActions.getPitch) => ModifierActions.getPitch,
            nameof(ModifierActions.getMusicTime) => ModifierActions.getMusicTime,
            //nameof(ModifierActions.getAxis) => ModifierActions.getAxis,
            nameof(ModifierActions.getMath) => ModifierActions.getMath,
            nameof(ModifierActions.getNearestPlayer) => ModifierActions.getNearestPlayer,
            //nameof(ModifierActions.getCollidingPlayers) => ModifierActions.getCollidingPlayers,
            nameof(ModifierActions.getPlayerHealth) => ModifierActions.getPlayerHealth,
            nameof(ModifierActions.getPlayerPosX) => ModifierActions.getPlayerPosX,
            nameof(ModifierActions.getPlayerPosY) => ModifierActions.getPlayerPosY,
            nameof(ModifierActions.getPlayerRot) => ModifierActions.getPlayerRot,
            nameof(ModifierActions.getEventValue) => ModifierActions.getEventValue,
            nameof(ModifierActions.getSample) => ModifierActions.getSample,
            //nameof(ModifierActions.getText) => ModifierActions.getText,
            //nameof(ModifierActions.getTextOther) => ModifierActions.getTextOther,
            nameof(ModifierActions.getCurrentKey) => ModifierActions.getCurrentKey,
            nameof(ModifierActions.getColorSlotHexCode) => ModifierActions.getColorSlotHexCode,
            nameof(ModifierActions.getFloatFromHexCode) => ModifierActions.getFloatFromHexCode,
            nameof(ModifierActions.getHexCodeFromFloat) => ModifierActions.getHexCodeFromFloat,
            nameof(ModifierActions.getJSONString) => ModifierActions.getJSONString,
            nameof(ModifierActions.getJSONFloat) => ModifierActions.getJSONFloat,
            nameof(ModifierActions.getJSON) => ModifierActions.getJSON,
            nameof(ModifierActions.getSubString) => ModifierActions.getSubString,
            nameof(ModifierActions.getSplitString) => ModifierActions.getSplitString,
            nameof(ModifierActions.getSplitStringAt) => ModifierActions.getSplitStringAt,
            nameof(ModifierActions.getSplitStringCount) => ModifierActions.getSplitStringCount,
            nameof(ModifierActions.getStringLength) => ModifierActions.getStringLength,
            nameof(ModifierActions.getParsedString) => ModifierActions.getParsedString,
            nameof(ModifierActions.getRegex) => ModifierActions.getRegex,
            nameof(ModifierActions.getFormatVariable) => ModifierActions.getFormatVariable,
            nameof(ModifierActions.getComparison) => ModifierActions.getComparison,
            nameof(ModifierActions.getComparisonMath) => ModifierActions.getComparisonMath,
            nameof(ModifierActions.getMixedColors) => ModifierActions.getMixedColors,
            nameof(ModifierActions.getSignaledVariables) => ModifierActions.getSignaledVariables,
            nameof(ModifierActions.signalLocalVariables) => ModifierActions.signalLocalVariables,
            nameof(ModifierActions.clearLocalVariables) => ModifierActions.clearLocalVariables,

            nameof(ModifierActions.addVariable) => ModifierActions.addVariable,
            nameof(ModifierActions.addVariableOther) => ModifierActions.addVariableOther,
            nameof(ModifierActions.subVariable) => ModifierActions.subVariable,
            nameof(ModifierActions.subVariableOther) => ModifierActions.subVariableOther,
            nameof(ModifierActions.setVariable) => ModifierActions.setVariable,
            nameof(ModifierActions.setVariableOther) => ModifierActions.setVariableOther,
            nameof(ModifierActions.setVariableRandom) => ModifierActions.setVariableRandom,
            nameof(ModifierActions.setVariableRandomOther) => ModifierActions.setVariableRandomOther,
            nameof(ModifierActions.animateVariableOther) => ModifierActions.animateVariableOther,
            nameof(ModifierActions.clampVariable) => ModifierActions.clampVariable,
            nameof(ModifierActions.clampVariableOther) => ModifierActions.clampVariableOther,

            #endregion

            #region Enable / Disable

            //// enable
            //nameof(ModifierActions.enableObject) => ModifierActions.enableObject,
            //nameof(ModifierActions.enableObjectTree) => ModifierActions.enableObjectTree,
            //nameof(ModifierActions.enableObjectOther) => ModifierActions.enableObjectOther,
            //nameof(ModifierActions.enableObjectTreeOther) => ModifierActions.enableObjectTreeOther,
            //nameof(ModifierActions.enableObjectGroup) => ModifierActions.enableObjectGroup,

            //// disable
            //nameof(ModifierActions.disableObject) => ModifierActions.disableObject,
            //nameof(ModifierActions.disableObjectTree) => ModifierActions.disableObjectTree,
            //nameof(ModifierActions.disableObjectOther) => ModifierActions.disableObjectOther,
            //nameof(ModifierActions.disableObjectTreeOther) => ModifierActions.disableObjectTreeOther,

            #endregion

            #region JSON

            nameof(ModifierActions.saveFloat) => ModifierActions.saveFloat,
            nameof(ModifierActions.saveString) => ModifierActions.saveString,
            //nameof(ModifierActions.saveText) => ModifierActions.saveText,
            //nameof(ModifierActions.saveVariable) => ModifierActions.saveVariable,
            //nameof(ModifierActions.loadVariable) => ModifierActions.loadVariable,
            //nameof(ModifierActions.loadVariableOther) => ModifierActions.loadVariableOther,

            #endregion

            #region Reactive

            //// single
            //nameof(ModifierActions.reactivePos) => ModifierActions.reactivePos,
            //nameof(ModifierActions.reactiveSca) => ModifierActions.reactiveSca,
            //nameof(ModifierActions.reactiveRot) => ModifierActions.reactiveRot,
            //nameof(ModifierActions.reactiveCol) => ModifierActions.reactiveCol,
            //nameof(ModifierActions.reactiveColLerp) => ModifierActions.reactiveColLerp,

            //// chain
            //nameof(ModifierActions.reactivePosChain) => ModifierActions.reactivePosChain,
            //nameof(ModifierActions.reactiveScaChain) => ModifierActions.reactiveScaChain,
            //nameof(ModifierActions.reactiveRotChain) => ModifierActions.reactiveRotChain,

            #endregion

            #region Events

            nameof(ModifierActions.eventOffset) => ModifierActions.eventOffset,
            nameof(ModifierActions.eventOffsetVariable) => ModifierActions.eventOffsetVariable,
            nameof(ModifierActions.eventOffsetMath) => ModifierActions.eventOffsetMath,
            nameof(ModifierActions.eventOffsetAnimate) => ModifierActions.eventOffsetAnimate,
            //nameof(ModifierActions.eventOffsetCopyAxis) => ModifierActions.eventOffsetCopyAxis,
            nameof(ModifierActions.vignetteTracksPlayer) => ModifierActions.vignetteTracksPlayer,
            nameof(ModifierActions.lensTracksPlayer) => ModifierActions.lensTracksPlayer,

            #endregion

            // todo: implement gradients and different color controls
            #region Color

            //// color
            //nameof(ModifierActions.addColor) => ModifierActions.addColor,
            //nameof(ModifierActions.addColorOther) => ModifierActions.addColorOther,
            //nameof(ModifierActions.lerpColor) => ModifierActions.lerpColor,
            //nameof(ModifierActions.lerpColorOther) => ModifierActions.lerpColorOther,
            //nameof(ModifierActions.addColorPlayerDistance) => ModifierActions.addColorPlayerDistance,
            //nameof(ModifierActions.lerpColorPlayerDistance) => ModifierActions.lerpColorPlayerDistance,

            //// opacity
            //nameof(ModifierActions.setAlpha) => ModifierActions.setOpacity,
            //nameof(ModifierActions.setOpacity) => ModifierActions.setOpacity,
            //nameof(ModifierActions.setAlphaOther) => ModifierActions.setOpacityOther,
            //nameof(ModifierActions.setOpacityOther) => ModifierActions.setOpacityOther,

            //// copy
            //nameof(ModifierActions.copyColor) => ModifierActions.copyColor,
            //nameof(ModifierActions.copyColorOther) => ModifierActions.copyColorOther,
            //nameof(ModifierActions.applyColorGroup) => ModifierActions.applyColorGroup,

            //// hex code
            //nameof(ModifierActions.setColorHex) => ModifierActions.setColorHex,
            //nameof(ModifierActions.setColorHexOther) => ModifierActions.setColorHexOther,

            //// rgba
            //nameof(ModifierActions.setColorRGBA) => ModifierActions.setColorRGBA,
            //nameof(ModifierActions.setColorRGBAOther) => ModifierActions.setColorRGBAOther,

            nameof(ModifierActions.animateColorKF) => ModifierActions.animateColorKF,
            nameof(ModifierActions.animateColorKFHex) => ModifierActions.animateColorKFHex,

            #endregion

            // todo: figure out how to get actorFrameTexture to work
            #region Shape

            nameof(ModifierActions.setShape) => ModifierActions.setShape,
            //nameof(ModifierActions.setPolygonShape) => ModifierActions.setPolygonShape,
            //nameof(ModifierActions.setPolygonShapeOther) => ModifierActions.setPolygonShapeOther,

            //nameof(ModifierActions.actorFrameTexture) => ModifierActions.actorFrameTexture,

            //// image
            //nameof(ModifierActions.setImage) => ModifierActions.setImage,
            //nameof(ModifierActions.setImageOther) => ModifierActions.setImageOther,

            //// text (pain)
            //nameof(ModifierActions.setText) => ModifierActions.setText,
            //nameof(ModifierActions.setTextOther) => ModifierActions.setTextOther,
            //nameof(ModifierActions.addText) => ModifierActions.addText,
            //nameof(ModifierActions.addTextOther) => ModifierActions.addTextOther,
            //nameof(ModifierActions.removeText) => ModifierActions.removeText,
            //nameof(ModifierActions.removeTextOther) => ModifierActions.removeTextOther,
            //nameof(ModifierActions.removeTextAt) => ModifierActions.removeTextAt,
            //nameof(ModifierActions.removeTextOtherAt) => ModifierActions.removeTextOtherAt,
            //nameof(ModifierActions.formatText) => ModifierActions.formatText,
            //nameof(ModifierActions.textSequence) => ModifierActions.textSequence,

            //// modify shape
            //nameof(ModifierActions.backgroundShape) => ModifierActions.backgroundShape,
            //nameof(ModifierActions.sphereShape) => ModifierActions.sphereShape,
            //nameof(ModifierActions.translateShape) => ModifierActions.translateShape,

            #endregion

            #region Animation

            nameof(ModifierActions.animateObject) => ModifierActions.animateObject,
            nameof(ModifierActions.animateObjectOther) => ModifierActions.animateObjectOther,
            nameof(ModifierActions.animateObjectKF) => ModifierActions.animateObjectKF,
            nameof(ModifierActions.animateSignal) => ModifierActions.animateSignal,
            nameof(ModifierActions.animateSignalOther) => ModifierActions.animateSignalOther,

            nameof(ModifierActions.animateObjectMath) => ModifierActions.animateObjectMath,
            nameof(ModifierActions.animateObjectMathOther) => ModifierActions.animateObjectMathOther,
            nameof(ModifierActions.animateSignalMath) => ModifierActions.animateSignalMath,
            nameof(ModifierActions.animateSignalMathOther) => ModifierActions.animateSignalMathOther,

            nameof(ModifierActions.gravity) => ModifierActions.gravity,
            nameof(ModifierActions.gravityOther) => ModifierActions.gravityOther,

            nameof(ModifierActions.copyAxis) => ModifierActions.copyAxis,
            nameof(ModifierActions.copyAxisMath) => ModifierActions.copyAxisMath,
            nameof(ModifierActions.copyAxisGroup) => ModifierActions.copyAxisGroup,
            nameof(ModifierActions.copyPlayerAxis) => ModifierActions.copyPlayerAxis,
            //nameof(ModifierActions.legacyTail) => ModifierActions.legacyTail,

            //nameof(ModifierActions.applyAnimation) => ModifierActions.applyAnimation,
            //nameof(ModifierActions.applyAnimationFrom) => ModifierActions.applyAnimationFrom,
            //nameof(ModifierActions.applyAnimationTo) => ModifierActions.applyAnimationTo,
            //nameof(ModifierActions.applyAnimationMath) => ModifierActions.applyAnimationMath,
            //nameof(ModifierActions.applyAnimationFromMath) => ModifierActions.applyAnimationFromMath,
            //nameof(ModifierActions.applyAnimationToMath) => ModifierActions.applyAnimationToMath,

            #endregion

            #region Prefab

            nameof(ModifierActions.spawnPrefab) => ModifierActions.spawnPrefab,
            //nameof(ModifierActions.spawnPrefabOffset) => ModifierActions.spawnPrefabOffset,
            //nameof(ModifierActions.spawnPrefabOffsetOther) => ModifierActions.spawnPrefabOffsetOther,
            nameof(ModifierActions.spawnMultiPrefab) => ModifierActions.spawnMultiPrefab,
            //nameof(ModifierActions.spawnMultiPrefabOffset) => ModifierActions.spawnMultiPrefabOffset,
            //nameof(ModifierActions.spawnMultiPrefabOffsetOther) => ModifierActions.spawnMultiPrefabOffsetOther,
            nameof(ModifierActions.clearSpawnedPrefabs) => ModifierActions.clearSpawnedPrefabs,

            #endregion

            #region Ranking

            nameof(ModifierActions.saveLevelRank) => ModifierActions.saveLevelRank,

            nameof(ModifierActions.clearHits) => ModifierActions.clearHits,
            nameof(ModifierActions.addHit) => ModifierActions.addHit,
            nameof(ModifierActions.subHit) => ModifierActions.subHit,
            nameof(ModifierActions.clearDeaths) => ModifierActions.clearDeaths,
            nameof(ModifierActions.addDeath) => ModifierActions.addDeath,
            nameof(ModifierActions.subDeath) => ModifierActions.subDeath,

            #endregion

            #region Updates

            // update
            nameof(ModifierActions.updateObjects) => ModifierActions.updateObjects,
            //nameof(ModifierActions.updateObject) => ModifierActions.updateObject,

            // parent
            //nameof(ModifierActions.setParent) => ModifierActions.setParent,
            //nameof(ModifierActions.setParentOther) => ModifierActions.setParentOther,
            //nameof(ModifierActions.detachParent) => ModifierActions.detachParent,
            //nameof(ModifierActions.detachParentOther) => ModifierActions.detachParentOther,

            #endregion

            #region Physics

            // collision
            //nameof(ModifierActions.setCollision) => ModifierActions.setCollision,
            //nameof(ModifierActions.setCollisionOther) => ModifierActions.setCollisionOther,

            #endregion

            #region Checkpoints

            nameof(ModifierActions.createCheckpoint) => ModifierActions.createCheckpoint,
            nameof(ModifierActions.resetCheckpoint) => ModifierActions.resetCheckpoint,

            #endregion

            #region Interfaces

            nameof(ModifierActions.loadInterface) => ModifierActions.loadInterface,
            nameof(ModifierActions.quitToMenu) => ModifierActions.quitToMenu,
            nameof(ModifierActions.quitToArcade) => ModifierActions.quitToArcade,
            nameof(ModifierActions.pauseLevel) => ModifierActions.pauseLevel,

            #endregion

            #region Misc

            nameof(ModifierActions.setBGActive) => ModifierActions.setBGActive,

            // activation
            nameof(ModifierActions.signalModifier) => ModifierActions.signalModifier,
            nameof(ModifierActions.activateModifier) => ModifierActions.activateModifier,

            nameof(ModifierActions.editorNotify) => ModifierActions.editorNotify,

            // external
            nameof(ModifierActions.setWindowTitle) => ModifierActions.setWindowTitle,
            nameof(ModifierActions.setDiscordStatus) => ModifierActions.setDiscordStatus,

            #endregion

            _ => (modifier, variables) => { },
        };

        /// <summary>
        /// The function to run when a modifier is inactive and has a reference of <see cref="BackgroundObject"/>.
        /// </summary>
        /// <param name="modifier">Modifier to run the inactive function of.</param>
        public static void BGInactive(Modifier<BackgroundObject> modifier, Dictionary<string, string> variables = null)
        {
            if (!modifier.verified)
            {
                modifier.verified = true;
                modifier.VerifyModifier(ModifiersManager.defaultBackgroundObjectModifiers);
            }

            if (modifier.commands.IsEmpty())
                return;

            try
            {
                switch (modifier.Name)
                {
                    case nameof(ModifierActions.animateColorKF): {
                            modifier.Result = null;
                            break;
                        }
                    case nameof(ModifierActions.animateColorKFHex): {
                            modifier.Result = null;
                            break;
                        }

                    case "spawnPrefab":
                    case "spawnPrefabOffset":
                    case "spawnPrefabOffsetOther": {
                            // value 9 is permanent

                            if (!modifier.constant && modifier.Result is PrefabObject prefabObject && !Parser.TryParse(modifier.commands[9], false))
                            {
                                RTLevel.Current?.UpdatePrefab(prefabObject, false);

                                GameData.Current.prefabObjects.RemoveAll(x => x.fromModifier && x.id == prefabObject.id);

                                modifier.Result = null;
                            }
                            break;
                        }

                    case "signalModifier":
                    case "mouseOverSignalModifier": {
                            var list = GameData.Current.FindObjectsWithTag(modifier.prefabInstanceOnly, modifier.groupAlive, modifier.reference, modifier.GetValue(1));

                            if (!list.IsEmpty() && list.Any(x => x.modifiers.Any(y => y.Result != null)))
                                foreach (var bm in list)
                                {
                                    if (!bm.modifiers.IsEmpty() && bm.modifiers.TryFind(x => x.Name == "requireSignal" && x.type == ModifierBase.Type.Trigger, out Modifier<BeatmapObject> m))
                                        m.Result = null;
                                }

                            break;
                        }
                    case "animateSignal":
                    case "animateSignalOther": {
                            if (!Parser.TryParse(modifier.commands[!modifier.commands[0].Contains("Other") ? 9 : 10], true))
                                return;

                            int groupIndex = !modifier.commands[0].Contains("Other") ? 7 : 8;
                            var list = GameData.Current.FindObjectsWithTag(modifier.prefabInstanceOnly, modifier.groupAlive, modifier.reference, modifier.GetValue(groupIndex));

                            if (!list.IsEmpty() && !modifier.constant)
                                foreach (var bm in list)
                                {
                                    if (!bm.modifiers.IsEmpty() && bm.modifiers.TryFind(x => x.Name == "requireSignal" && x.type == ModifierBase.Type.Trigger, out Modifier<BeatmapObject> m))
                                        m.Result = null;
                                }

                            break;
                        }
                    case "randomGreater":
                    case "randomLesser":
                    case "randomEquals":
                    case "gravity":
                    case "gravityOther": {
                            modifier.Result = null;
                            break;
                        }
                    case "copyAxis":
                    case "copyAxisMath":
                    case "copyAxisGroup":
                    case "objectSpawned": {
                            modifier.Result = null;
                            break;
                        }
                    case "applyAnimation":
                    case "applyAnimationFrom":
                    case "applyAnimationTo": {
                            if (modifier.constant)
                                modifier.Result = null;

                            break;
                        }
                }
            }
            catch { }
        }

        #endregion

        #region Player

        public static Func<Modifier<CustomPlayer>, Dictionary<string, string>, bool> GetPlayerTrigger(string key) => key switch
        {
            "keyPressDown" => ModifierTriggers.PlayerTriggers.keyPressDown,
            "keyPress" => ModifierTriggers.PlayerTriggers.keyPress,
            "keyPressUp" => ModifierTriggers.PlayerTriggers.keyPressUp,
            "mouseButtonDown" => ModifierTriggers.PlayerTriggers.mouseButtonDown,
            "mouseButton" => ModifierTriggers.PlayerTriggers.mouseButton,
            "mouseButtonUp" => ModifierTriggers.PlayerTriggers.mouseButtonUp,
            "controlPressDown" => ModifierTriggers.PlayerTriggers.controlPressDown,
            "controlPress" => ModifierTriggers.PlayerTriggers.controlPress,
            "controlPressUp" => ModifierTriggers.PlayerTriggers.controlPressUp,
            "healthEquals" => ModifierTriggers.PlayerTriggers.healthEquals,
            "healthGreaterEquals" => ModifierTriggers.PlayerTriggers.healthGreaterEquals,
            "healthLesserEquals" => ModifierTriggers.PlayerTriggers.healthLesserEquals,
            "healthGreater" => ModifierTriggers.PlayerTriggers.healthGreater,
            "healthLesser" => ModifierTriggers.PlayerTriggers.healthLesser,
            "healthPerEquals" => ModifierTriggers.PlayerTriggers.healthPerEquals,
            "healthPerGreaterEquals" => ModifierTriggers.PlayerTriggers.healthPerGreaterEquals,
            "healthPerLesserEquals" => ModifierTriggers.PlayerTriggers.healthPerLesserEquals,
            "healthPerGreater" => ModifierTriggers.PlayerTriggers.healthPerGreater,
            "healthPerLesser" => ModifierTriggers.PlayerTriggers.healthPerLesser,
            "isDead" => ModifierTriggers.PlayerTriggers.isDead,
            "isBoosting" => ModifierTriggers.PlayerTriggers.isBoosting,
            "isColliding" => ModifierTriggers.PlayerTriggers.isColliding,
            "isSolidColliding" => ModifierTriggers.PlayerTriggers.isSolidColliding,
            _ => (modifier, variables) => false,
        };

        public static Action<Modifier<CustomPlayer>, Dictionary<string, string>> GetPlayerAction(string key) => key switch
        {
            "setCustomActive" => ModifierActions.PlayerActions.setCustomActive,
            "kill" => ModifierActions.PlayerActions.kill,
            "hit" => ModifierActions.PlayerActions.hit,
            "boost" => ModifierActions.PlayerActions.boost,
            "shoot" => ModifierActions.PlayerActions.shoot,
            "pulse" => ModifierActions.PlayerActions.pulse,
            "jump" => ModifierActions.PlayerActions.jump,
            "signalModifier" => ModifierActions.PlayerActions.signalModifier,
            "playAnimation" => ModifierActions.PlayerActions.playAnimation,
            "setIdleAnimation" => ModifierActions.PlayerActions.setIdleAnimation,
            "playDefaultSound" => ModifierActions.playDefaultSound,
            "animateObject" => ModifierActions.animateObject,
            _ => (modifier, variables) => { },
        };

        /// <summary>
        /// The function to run when a modifier is inactive and has a reference of <see cref="CustomPlayer"/>.
        /// </summary>
        /// <param name="modifier">Modifier to run the inactive function of.</param>
        public static void PlayerInactive(Modifier<CustomPlayer> modifier, Dictionary<string, string> variables = null)
        {
            if (!modifier.verified)
            {
                modifier.verified = true;
                modifier.VerifyModifier(ModifiersManager.defaultPlayerModifiers);
            }

            if (modifier.commands.IsEmpty() || modifier.reference == null)
                return;

            switch (modifier.Name)
            {
                case "setCustomActive": {
                        if (modifier.GetBool(2, true) && modifier.reference.Player.customObjects.TryFind(x => x.id == modifier.GetValue(1), out RTPlayer.CustomObject customObject))
                            customObject.active = !Parser.TryParse(modifier.value, false);

                        break;
                    }
            }
        }

        #endregion

        #endregion

        #region Internal Functions

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

        public static void GetSoundPath(string id, string path, bool fromSoundLibrary = false, float pitch = 1f, float volume = 1f, bool loop = false)
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
                CoroutineHelper.StartCoroutine(LoadMusicFileRaw(fullPath, audioClip => PlaySound(id, audioClip, pitch, volume, loop)));
            else
                PlaySound(id, LSAudio.CreateAudioClipUsingMP3File(fullPath), pitch, volume, loop);
        }

        public static void DownloadSoundAndPlay(string id, string path, float pitch = 1f, float volume = 1f, bool loop = false)
        {
            try
            {
                var audioType = RTFile.GetAudioType(path);

                if (audioType != AudioType.UNKNOWN)
                    CoroutineHelper.StartCoroutine(AlephNetwork.DownloadAudioClip(path, audioType, audioClip => PlaySound(id, audioClip, pitch, volume, loop), onError => CoreHelper.Log($"Error! Could not download audioclip.\n{onError}")));
            }
            catch
            {

            }
        }

        public static void PlaySound(string id, AudioClip clip, float pitch, float volume, bool loop)
        {
            var audioSource = SoundManager.inst.PlaySound(clip, volume, pitch * AudioManager.inst.CurrentAudioSource.pitch, loop);
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

            if (beatmapObject.modifiers.TryFind(x => x.commands[0] == "requireSignal" && x.type == ModifierBase.Type.Trigger, out Modifier<BeatmapObject> modifier))
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
                    applyTo.positionOffset = takeFrom.cachedSequences.PositionSequence.Interpolate(currentTime - time - delayPos);

                // Animate scale
                if (animateSca)
                {
                    var scaleSequence = takeFrom.cachedSequences.ScaleSequence.Interpolate(currentTime - time - delaySca);
                    applyTo.scaleOffset = new Vector3(scaleSequence.x - 1f, scaleSequence.y - 1f, 0f);
                }

                // Animate rotation
                if (animateRot)
                    applyTo.rotationOffset = new Vector3(0f, 0f, takeFrom.cachedSequences.RotationSequence.Interpolate(currentTime - time - delayRot));
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

        public static float GetAnimation(BeatmapObject bm, int fromType, int fromAxis, float min, float max, float offset, float multiply, float delay, float loop, bool visual)
        {
            var time = RTLevel.Current.CurrentTime;

            if (!visual && bm.cachedSequences)
                return fromType switch
                {
                    0 => Mathf.Clamp((bm.cachedSequences.PositionSequence.Interpolate(time - bm.StartTime - delay).At(fromAxis) - offset) * multiply % loop, min, max),
                    1 => Mathf.Clamp((bm.cachedSequences.ScaleSequence.Interpolate(time - bm.StartTime - delay).At(fromAxis) - offset) * multiply % loop, min, max),
                    2 => Mathf.Clamp((bm.cachedSequences.RotationSequence.Interpolate(time - bm.StartTime - delay) - offset) * multiply % loop, min, max),
                    _ => 0f,
                };
            else if (visual && bm.runtimeObject is RTBeatmapObject levelObject && levelObject.visualObject && levelObject.visualObject.gameObject)
                return Mathf.Clamp((levelObject.visualObject.gameObject.transform.GetVector(fromType).At(fromAxis) - offset) * multiply % loop, min, max);

            return 0f;
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
            levelRankIndex = active ? LevelManager.levelRankIndexes[LevelManager.GetLevelRank(level).name] : 0;
            return active;
        }

        public static string GetSaveFile(string file) => RTFile.CombinePaths(RTFile.ApplicationDirectory, "profile", file + FileFormat.SES.Dot());

        public static Prefab GetPrefab(int findType, string reference) => findType switch
        {
            0 => GameData.Current.prefabs.GetAt(Parser.TryParse(reference, -1)),
            1 => GameData.Current.prefabs.Find(x => x.name == reference),
            2 => GameData.Current.prefabs.Find(x => x.id == reference),
            _ => null,
        };

        public static void SetParent(BeatmapObject child, string parent)
        {
            child.customParent = parent;
            RTLevel.Current?.UpdateObject(child, RTLevel.ObjectContext.PARENT_CHAIN);

            if (ObjectEditor.inst && ObjectEditor.inst.Dialog && ObjectEditor.inst.Dialog.IsCurrent && EditorTimeline.inst.CurrentSelection.isBeatmapObject)
                ObjectEditor.inst.RenderParent(EditorTimeline.inst.CurrentSelection.GetData<BeatmapObject>());
        }

        #endregion
    }
}
