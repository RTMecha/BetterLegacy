using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

using UnityEngine;

using LSFunctions;

using DG.Tweening;
using SimpleJSON;

using BetterLegacy.Arcade.Managers;
using BetterLegacy.Configs;
using BetterLegacy.Core.Animation;
using BetterLegacy.Core.Animation.Keyframe;
using BetterLegacy.Core.Components;
using BetterLegacy.Core.Components.Player;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Data.Level;
using BetterLegacy.Core.Data.Player;
using BetterLegacy.Core.Managers;
using BetterLegacy.Core.Runtime;
using BetterLegacy.Core.Runtime.Objects;
using BetterLegacy.Core.Runtime.Objects.Visual;
using BetterLegacy.Editor.Data;
using BetterLegacy.Editor.Managers;
using BetterLegacy.Menus;
using BetterLegacy.Menus.UI.Interfaces;

using Ease = BetterLegacy.Core.Animation.Ease;

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
                        AssignModifierAction(modifier as Modifier<CustomPlayer>, PlayerAction, PlayerTrigger, PlayerInactive);
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

        public static bool VerifyModifier<T>(Modifier<T> modifier, List<Modifier<T>> modifiers)
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

            "playerCollide" => ModifierTriggers.playerCollide,
            "playerHealthEquals" => ModifierTriggers.playerHealthEquals,
            "playerHealthLesserEquals" => ModifierTriggers.playerHealthLesserEquals,
            "playerHealthGreaterEquals" => ModifierTriggers.playerHealthGreaterEquals,
            "playerHealthLesser" => ModifierTriggers.playerHealthLesser,
            "playerHealthGreater" => ModifierTriggers.playerHealthGreater,
            "playerMoving" => ModifierTriggers.playerMoving,
            "playerBoosting" => ModifierTriggers.playerBoosting,
            "playerAlive" => ModifierTriggers.playerAlive,
            "playerDeathsEquals" => ModifierTriggers.playerDeathsEquals,
            "playerDeathsLesserEquals" => ModifierTriggers.playerDeathsLesserEquals,
            "playerDeathsGreaterEquals" => ModifierTriggers.playerDeathsGreaterEquals,
            "playerDeathsLesser" => ModifierTriggers.playerDeathsLesser,
            "playerDeathsGreater" => ModifierTriggers.playerDeathsGreater,
            "playerDistanceGreater" => ModifierTriggers.playerDistanceGreater,
            "playerDistanceLesser" => ModifierTriggers.playerDistanceLesser,
            "playerCountEquals" => ModifierTriggers.playerCountEquals,
            "playerCountLesserEquals" => ModifierTriggers.playerCountLesserEquals,
            "playerCountGreaterEquals" => ModifierTriggers.playerCountGreaterEquals,
            "playerCountLesser" => ModifierTriggers.playerCountLesser,
            "playerCountGreater" => ModifierTriggers.playerCountGreater,
            "onPlayerHit" => ModifierTriggers.onPlayerHit,
            "onPlayerDeath" => ModifierTriggers.onPlayerDeath,
            "playerBoostEquals" => ModifierTriggers.playerBoostEquals,
            "playerBoostLesserEquals" => ModifierTriggers.playerBoostLesserEquals,
            "playerBoostGreaterEquals" => ModifierTriggers.playerBoostGreaterEquals,
            "playerBoostLesser" => ModifierTriggers.playerBoostLesser,
            "playerBoostGreater" => ModifierTriggers.playerBoostGreater,

            #endregion

            #region Controls

            "keyPressDown" => ModifierTriggers.keyPressDown,
            "keyPress" => ModifierTriggers.keyPress,
            "keyPressUp" => ModifierTriggers.keyPressUp,
            "mouseButtonDown" => ModifierTriggers.mouseButtonDown,
            "mouseButton" => ModifierTriggers.mouseButton,
            "mouseButtonUp" => ModifierTriggers.mouseButtonUp,
            "mouseOver" => ModifierTriggers.mouseOver,
            "mouseOverSignalModifier" => ModifierTriggers.mouseOverSignalModifier,
            "controlPressDown" => ModifierTriggers.controlPressDown,
            "controlPress" => ModifierTriggers.controlPress,
            "controlPressUp" => ModifierTriggers.controlPressUp,

            #endregion

            #region Collide

            "bulletCollide" => ModifierTriggers.bulletCollide,
            "objectCollide" => ModifierTriggers.objectCollide,

            #endregion

            #region JSON

            "loadEquals" => ModifierTriggers.loadEquals,
            "loadLesserEquals" => ModifierTriggers.loadLesserEquals,
            "loadGreaterEquals" => ModifierTriggers.loadGreaterEquals,
            "loadLesser" => ModifierTriggers.loadLesser,
            "loadGreater" => ModifierTriggers.loadGreater,
            "loadExists" => ModifierTriggers.loadExists,

            #endregion

            #region Variable

            "localVariableEquals" => (modifier, variables) =>
            {
                return variables.TryGetValue(modifier.GetValue(0), out string result) && result == modifier.GetValue(1, variables);
            },
            "localVariableLesserEquals" => (modifier, variables) =>
            {
                return variables.TryGetValue(modifier.GetValue(0), out string result) && (float.TryParse(result, out float num) ? num : Parser.TryParse(result, 0)) <= modifier.GetFloat(1, 0f, variables);
            },
            "localVariableGreaterEquals" => (modifier, variables) =>
            {
                return variables.TryGetValue(modifier.GetValue(0), out string result) && (float.TryParse(result, out float num) ? num : Parser.TryParse(result, 0)) >= modifier.GetFloat(1, 0f, variables);
            },
            "localVariableLesser" => (modifier, variables) =>
            {
                return variables.TryGetValue(modifier.GetValue(0), out string result) && (float.TryParse(result, out float num) ? num : Parser.TryParse(result, 0)) < modifier.GetFloat(1, 0f, variables);
            },
            "localVariableGreater" => (modifier, variables) =>
            {
                return variables.TryGetValue(modifier.GetValue(0), out string result) && (float.TryParse(result, out float num) ? num : Parser.TryParse(result, 0)) > modifier.GetFloat(1, 0f, variables);
            },
            "localVariableContains" => (modifier, variables) =>
            {
                return variables.TryGetValue(modifier.GetValue(0), out string result) && result.Contains(modifier.GetValue(1, variables));
            },
            "localVariableStartsWith" => (modifier, variables) =>
            {
                return variables.TryGetValue(modifier.GetValue(0), out string result) && result.StartsWith(modifier.GetValue(1, variables));
            },
            "localVariableEndsWith" => (modifier, variables) =>
            {
                return variables.TryGetValue(modifier.GetValue(0), out string result) && result.EndsWith(modifier.GetValue(1, variables));
            },
            "localVariableExists" => (modifier, variables) =>
            {
                return variables.ContainsKey(modifier.GetValue(0));
            },

            // self
            "variableEquals" => (modifier, variables) =>
            {
                return modifier.reference && modifier.reference.integerVariable == modifier.GetInt(0, 0, variables);
            },
            "variableLesserEquals" => (modifier, variables) =>
            {
                return modifier.reference && modifier.reference.integerVariable <= modifier.GetInt(0, 0, variables);
            },
            "variableGreaterEquals" => (modifier, variables) =>
            {
                return modifier.reference && modifier.reference.integerVariable >= modifier.GetInt(0, 0, variables);
            },
            "variableLesser" => (modifier, variables) =>
            {
                return modifier.reference && modifier.reference.integerVariable < modifier.GetInt(0, 0, variables);
            },
            "variableGreater" => (modifier, variables) =>
            {
                return modifier.reference && modifier.reference.integerVariable > modifier.GetInt(0, 0, variables);
            },

            // other
            "variableOtherEquals" => (modifier, variables) =>
            {
                var beatmapObjects = GameData.Current.FindObjectsWithTag(modifier, modifier.GetValue(1, variables));

                int num = modifier.GetInt(0, 0, variables);

                return !beatmapObjects.IsEmpty() && beatmapObjects.Any(x => x.integerVariable == num);
            },
            "variableOtherLesserEquals" => (modifier, variables) =>
            {
                var beatmapObjects = GameData.Current.FindObjectsWithTag(modifier, modifier.GetValue(1, variables));

                int num = modifier.GetInt(0, 0, variables);

                return !beatmapObjects.IsEmpty() && beatmapObjects.Any(x => x.integerVariable <= num);
            },
            "variableOtherGreaterEquals" => (modifier, variables) =>
            {
                var beatmapObjects = GameData.Current.FindObjectsWithTag(modifier, modifier.GetValue(1, variables));

                int num = modifier.GetInt(0, 0, variables);

                return !beatmapObjects.IsEmpty() && beatmapObjects.Any(x => x.integerVariable >= num);
            },
            "variableOtherLesser" => (modifier, variables) =>
            {
                var beatmapObjects = GameData.Current.FindObjectsWithTag(modifier, modifier.GetValue(1, variables));

                int num = modifier.GetInt(0, 0, variables);

                return !beatmapObjects.IsEmpty() && beatmapObjects.Any(x => x.integerVariable < num);
            },
            "variableOtherGreater" => (modifier, variables) =>
            {
                var beatmapObjects = GameData.Current.FindObjectsWithTag(modifier, modifier.GetValue(1, variables));

                int num = modifier.GetInt(0, 0, variables);

                return !beatmapObjects.IsEmpty() && beatmapObjects.Any(x => x.integerVariable > num);
            },

            #endregion

            #region Audio

            "pitchEquals" => (modifier, variables) =>
            {
                return AudioManager.inst.pitch == modifier.GetFloat(0, 0f, variables);
            },
            "pitchLesserEquals" => (modifier, variables) =>
            {
                return AudioManager.inst.pitch <= modifier.GetFloat(0, 0f, variables);
            },
            "pitchGreaterEquals" => (modifier, variables) =>
            {
                return AudioManager.inst.pitch >= modifier.GetFloat(0, 0f, variables);
            },
            "pitchLesser" => (modifier, variables) =>
            {
                return AudioManager.inst.pitch < modifier.GetFloat(0, 0f, variables);
            },
            "pitchGreater" => (modifier, variables) =>
            {
                return AudioManager.inst.pitch > modifier.GetFloat(0, 0f, variables);
            },
            "musicTimeGreater" => (modifier, variables) =>
            {
                return AudioManager.inst.CurrentAudioSource.time - (modifier.GetBool(1, false, variables) ? modifier.reference.StartTime : 0f) > modifier.GetFloat(0, 0f, variables);
            },
            "musicTimeLesser" => (modifier, variables) =>
            {
                return AudioManager.inst.CurrentAudioSource.time - (modifier.GetBool(1, false, variables) ? modifier.reference.StartTime : 0f) < modifier.GetFloat(0, 0f, variables);
            },
            "musicPlaying" => (modifier, variables) =>
            {
                return AudioManager.inst.CurrentAudioSource.isPlaying;
            },

            #endregion

            #region Challenge Mode

            "inZenMode" => (modifier, variables) => PlayerManager.Invincible,
            "inNormal" => (modifier, variables) => PlayerManager.IsNormal,
            "in1Life" => (modifier, variables) => PlayerManager.Is1Life,
            "inNoHit" => (modifier, variables) => PlayerManager.IsNoHit,
            "inPractice" => (modifier, variables) => PlayerManager.IsPractice,

            #endregion

            #region Random

            "randomEquals" => (modifier, variables) =>
            {
                if (!modifier.HasResult())
                    modifier.Result = UnityEngine.Random.Range(modifier.GetInt(1, 0, variables), modifier.GetInt(2, 0, variables)) == modifier.GetInt(0, 0, variables);

                return modifier.HasResult() && modifier.GetResult<bool>();
            },
            "randomLesser" => (modifier, variables) =>
            {
                if (!modifier.HasResult())
                    modifier.Result = UnityEngine.Random.Range(modifier.GetInt(1, 0, variables), modifier.GetInt(2, 0, variables)) < modifier.GetInt(0, 0, variables);

                return modifier.HasResult() && modifier.GetResult<bool>();
            },
            "randomGreater" => (modifier, variables) =>
            {
                if (!modifier.HasResult())
                    modifier.Result = UnityEngine.Random.Range(modifier.GetInt(1, 0, variables), modifier.GetInt(2, 0, variables)) > modifier.GetInt(0, 0, variables);

                return modifier.HasResult() && modifier.GetResult<bool>();
            },

            #endregion

            #region Math

            "mathEquals" => (modifier, variables) =>
            {
                var numberVariables = modifier.reference.GetObjectVariables();
                SetVariables(variables, numberVariables);

                return RTMath.Parse(modifier.GetValue(0, variables), numberVariables) == RTMath.Parse(modifier.GetValue(1, variables), numberVariables);
            },
            "mathLesserEquals" => (modifier, variables) =>
            {
                var numberVariables = modifier.reference.GetObjectVariables();
                SetVariables(variables, numberVariables);

                return RTMath.Parse(modifier.GetValue(0, variables), numberVariables) <= RTMath.Parse(modifier.GetValue(1, variables), numberVariables);
            },
            "mathGreaterEquals" => (modifier, variables) =>
            {
                var numberVariables = modifier.reference.GetObjectVariables();
                SetVariables(variables, numberVariables);

                return RTMath.Parse(modifier.GetValue(0, variables), numberVariables) >= RTMath.Parse(modifier.GetValue(1, variables), numberVariables);
            },
            "mathLesser" => (modifier, variables) =>
            {
                var numberVariables = modifier.reference.GetObjectVariables();
                SetVariables(variables, numberVariables);

                return RTMath.Parse(modifier.GetValue(0, variables), numberVariables) < RTMath.Parse(modifier.GetValue(1, variables), numberVariables);
            },
            "mathGreater" => (modifier, variables) =>
            {
                var numberVariables = modifier.reference.GetObjectVariables();
                SetVariables(variables, numberVariables);

                return RTMath.Parse(modifier.GetValue(0, variables), numberVariables) > RTMath.Parse(modifier.GetValue(1, variables), numberVariables);
            },

            #endregion

            #region Axis

            "axisEquals" => (modifier, variables) =>
            {
                int fromType = modifier.GetInt(1, 0, variables);
                int fromAxis = modifier.GetInt(2, 0, variables);

                float delay = modifier.GetFloat(3, 0f, variables);
                float multiply = modifier.GetFloat(4, 0f, variables);
                float offset = modifier.GetFloat(5, 0f, variables);
                float min = modifier.GetFloat(6, -9999f, variables);
                float max = modifier.GetFloat(7, 9999f, variables);
                float equals = modifier.GetFloat(8, 0f, variables);
                bool visual = modifier.GetBool(9, false, variables);
                float loop = modifier.GetFloat(10, 9999f, variables);

                if (GameData.Current.TryFindObjectWithTag(modifier, modifier.GetValue(0, variables), out BeatmapObject bm))
                {
                    fromType = Mathf.Clamp(fromType, 0, bm.events.Count);
                    fromAxis = Mathf.Clamp(fromAxis, 0, bm.events[fromType][0].values.Length);

                    if (fromType >= 0 && fromType <= 2)
                        return GetAnimation(bm, fromType, fromAxis, min, max, offset, multiply, delay, loop, visual) == equals;
                }

                return false;
            },
            "axisLesserEquals" => (modifier, variables) =>
            {
                int fromType = modifier.GetInt(1, 0, variables);
                int fromAxis = modifier.GetInt(2, 0, variables);

                float delay = modifier.GetFloat(3, 0f, variables);
                float multiply = modifier.GetFloat(4, 0f, variables);
                float offset = modifier.GetFloat(5, 0f, variables);
                float min = modifier.GetFloat(6, -9999f, variables);
                float max = modifier.GetFloat(7, 9999f, variables);
                float equals = modifier.GetFloat(8, 0f, variables);
                bool visual = modifier.GetBool(9, false, variables);
                float loop = modifier.GetFloat(10, 9999f, variables);

                if (GameData.Current.TryFindObjectWithTag(modifier, modifier.GetValue(0, variables), out BeatmapObject bm))
                {
                    fromType = Mathf.Clamp(fromType, 0, bm.events.Count);
                    fromAxis = Mathf.Clamp(fromAxis, 0, bm.events[fromType][0].values.Length);

                    if (fromType >= 0 && fromType <= 2)
                        return GetAnimation(bm, fromType, fromAxis, min, max, offset, multiply, delay, loop, visual) <= equals;
                }

                return false;
            },
            "axisGreaterEquals" => (modifier, variables) =>
            {
                int fromType = modifier.GetInt(1, 0, variables);
                int fromAxis = modifier.GetInt(2, 0, variables);

                float delay = modifier.GetFloat(3, 0f, variables);
                float multiply = modifier.GetFloat(4, 0f, variables);
                float offset = modifier.GetFloat(5, 0f, variables);
                float min = modifier.GetFloat(6, -9999f, variables);
                float max = modifier.GetFloat(7, 9999f, variables);
                float equals = modifier.GetFloat(8, 0f, variables);
                bool visual = modifier.GetBool(9, false, variables);
                float loop = modifier.GetFloat(10, 9999f, variables);

                if (GameData.Current.TryFindObjectWithTag(modifier, modifier.GetValue(0, variables), out BeatmapObject bm))
                {
                    fromType = Mathf.Clamp(fromType, 0, bm.events.Count);
                    fromAxis = Mathf.Clamp(fromAxis, 0, bm.events[fromType][0].values.Length);

                    if (fromType >= 0 && fromType <= 2)
                        return GetAnimation(bm, fromType, fromAxis, min, max, offset, multiply, delay, loop, visual) >= equals;
                }

                return false;
            },
            "axisLesser" => (modifier, variables) =>
            {
                int fromType = modifier.GetInt(1, 0, variables);
                int fromAxis = modifier.GetInt(2, 0, variables);

                float delay = modifier.GetFloat(3, 0f, variables);
                float multiply = modifier.GetFloat(4, 0f, variables);
                float offset = modifier.GetFloat(5, 0f, variables);
                float min = modifier.GetFloat(6, -9999f, variables);
                float max = modifier.GetFloat(7, 9999f, variables);
                float equals = modifier.GetFloat(8, 0f, variables);
                bool visual = modifier.GetBool(9, false, variables);
                float loop = modifier.GetFloat(10, 9999f, variables);

                if (GameData.Current.TryFindObjectWithTag(modifier, modifier.GetValue(0, variables), out BeatmapObject bm))
                {
                    fromType = Mathf.Clamp(fromType, 0, bm.events.Count);
                    fromAxis = Mathf.Clamp(fromAxis, 0, bm.events[fromType][0].values.Length);

                    if (fromType >= 0 && fromType <= 2)
                        return GetAnimation(bm, fromType, fromAxis, min, max, offset, multiply, delay, loop, visual) < equals;
                }

                return false;
            },
            "axisGreater" => (modifier, variables) =>
            {
                int fromType = modifier.GetInt(1, 0, variables);
                int fromAxis = modifier.GetInt(2, 0, variables);

                float delay = modifier.GetFloat(3, 0f, variables);
                float multiply = modifier.GetFloat(4, 0f, variables);
                float offset = modifier.GetFloat(5, 0f, variables);
                float min = modifier.GetFloat(6, -9999f, variables);
                float max = modifier.GetFloat(7, 9999f, variables);
                float equals = modifier.GetFloat(8, 0f, variables);
                bool visual = modifier.GetBool(9, false, variables);
                float loop = modifier.GetFloat(10, 9999f, variables);

                if (GameData.Current.TryFindObjectWithTag(modifier, modifier.GetValue(0, variables), out BeatmapObject bm))
                {
                    fromType = Mathf.Clamp(fromType, 0, bm.events.Count);
                    fromAxis = Mathf.Clamp(fromAxis, 0, bm.events[fromType][0].values.Length);

                    if (fromType >= 0 && fromType <= 2)
                        return GetAnimation(bm, fromType, fromAxis, min, max, offset, multiply, delay, loop, visual) > equals;
                }

                return false;
            },

            #endregion

            #region Level Rank

            // self
            "levelRankEquals" => (modifier, variables) =>
            {
                return GetLevelRank(LevelManager.CurrentLevel, out int levelRankIndex) && levelRankIndex == modifier.GetInt(0, 0, variables);
            },
            "levelRankLesserEquals" => (modifier, variables) =>
            {
                return GetLevelRank(LevelManager.CurrentLevel, out int levelRankIndex) && levelRankIndex <= modifier.GetInt(0, 0, variables);
            },
            "levelRankGreaterEquals" => (modifier, variables) =>
            {
                return GetLevelRank(LevelManager.CurrentLevel, out int levelRankIndex) && levelRankIndex >= modifier.GetInt(0, 0, variables);
            },
            "levelRankLesser" => (modifier, variables) =>
            {
                return GetLevelRank(LevelManager.CurrentLevel, out int levelRankIndex) && levelRankIndex < modifier.GetInt(0, 0, variables);
            },
            "levelRankGreater" => (modifier, variables) =>
            {
                return GetLevelRank(LevelManager.CurrentLevel, out int levelRankIndex) && levelRankIndex > modifier.GetInt(0, 0, variables);
            },

            // other
            "levelRankOtherEquals" => (modifier, variables) =>
            {
                var id = modifier.GetValue(1, variables);
                return LevelManager.Levels.TryFind(x => x.id == id, out Level level) && GetLevelRank(level, out int levelRankIndex) && levelRankIndex == modifier.GetInt(0, 0, variables);
            },
            "levelRankOtherLesserEquals" => (modifier, variables) =>
            {
                var id = modifier.GetValue(1, variables);
                return LevelManager.Levels.TryFind(x => x.id == id, out Level level) && GetLevelRank(level, out int levelRankIndex) && levelRankIndex <= modifier.GetInt(0, 0, variables);
            },
            "levelRankOtherGreaterEquals" => (modifier, variables) =>
            {
                var id = modifier.GetValue(1, variables);
                return LevelManager.Levels.TryFind(x => x.id == id, out Level level) && GetLevelRank(level, out int levelRankIndex) && levelRankIndex >= modifier.GetInt(0, 0, variables);
            },
            "levelRankOtherLesser" => (modifier, variables) =>
            {
                var id = modifier.GetValue(1, variables);
                return LevelManager.Levels.TryFind(x => x.id == id, out Level level) && GetLevelRank(level, out int levelRankIndex) && levelRankIndex < modifier.GetInt(0, 0, variables);
            },
            "levelRankOtherGreater" => (modifier, variables) =>
            {
                var id = modifier.GetValue(1, variables);
                return LevelManager.Levels.TryFind(x => x.id == id, out Level level) && GetLevelRank(level, out int levelRankIndex) && levelRankIndex > modifier.GetInt(0, 0, variables);
            },

            // current
            "levelRankCurrentEquals" => (modifier, variables) =>
            {
                return LevelManager.levelRankIndexes[LevelManager.GetLevelRank(GameManager.inst.hits).name] == modifier.GetInt(0, 0, variables);
            },
            "levelRankCurrentLesserEquals" => (modifier, variables) =>
            {
                return LevelManager.levelRankIndexes[LevelManager.GetLevelRank(GameManager.inst.hits).name] <= modifier.GetInt(0, 0, variables);
            },
            "levelRankCurrentGreaterEquals" => (modifier, variables) =>
            {
                return LevelManager.levelRankIndexes[LevelManager.GetLevelRank(GameManager.inst.hits).name] >= modifier.GetInt(0, 0, variables);
            },
            "levelRankCurrentLesser" => (modifier, variables) =>
            {
                return LevelManager.levelRankIndexes[LevelManager.GetLevelRank(GameManager.inst.hits).name] < modifier.GetInt(0, 0, variables);
            },
            "levelRankCurrentGreater" => (modifier, variables) =>
            {
                return LevelManager.levelRankIndexes[LevelManager.GetLevelRank(GameManager.inst.hits).name] > modifier.GetInt(0, 0, variables);
            },

            #endregion

            #region Level

            "levelUnlocked" => (modifier, variables) =>
            {
                var id = modifier.GetValue(0, variables);
                return LevelManager.Levels.TryFind(x => x.id == id, out Level level) && !level.Locked;
            },
            "levelCompleted" => (modifier, variables) =>
            {
                return CoreHelper.InEditor || LevelManager.CurrentLevel && LevelManager.CurrentLevel.saveData && LevelManager.CurrentLevel.saveData.Completed;
            },
            "levelCompletedOther" => (modifier, variables) =>
            {
                var id = modifier.GetValue(0, variables);
                return CoreHelper.InEditor || LevelManager.Levels.TryFind(x => x.id == id, out Level level) && level.saveData && level.saveData.Completed;
            },
            "levelExists" => (modifier, variables) =>
            {
                var id = modifier.GetValue(0, variables);
                return LevelManager.Levels.Has(x => x.id == id);
            },
            "levelPathExists" => (modifier, variables) =>
            {
                var basePath = RTFile.CombinePaths(RTFile.ApplicationDirectory, LevelManager.ListSlash, modifier.GetValue(0, variables));

                return
                    RTFile.FileExists(RTFile.CombinePaths(basePath, Level.LEVEL_LSB)) ||
                    RTFile.FileExists(RTFile.CombinePaths(basePath, Level.LEVEL_VGD)) ||
                    RTFile.FileExists(basePath + FileFormat.ASSET.Dot());
            },

            #endregion

            #region Real Time

            // seconds
            "realTimeSecondEquals" => (modifier, variables) =>
            {
                return Parser.TryParse(DateTime.Now.ToString("ss"), 0) == modifier.GetInt(0, 0, variables);
            },
            "realTimeSecondLesserEquals" => (modifier, variables) =>
            {
                return Parser.TryParse(DateTime.Now.ToString("ss"), 0) <= modifier.GetInt(0, 0, variables);
            },
            "realTimeSecondGreaterEquals" => (modifier, variables) =>
            {
                return Parser.TryParse(DateTime.Now.ToString("ss"), 0) >= modifier.GetInt(0, 0, variables);
            },
            "realTimeSecondLesser" => (modifier, variables) =>
            {
                return Parser.TryParse(DateTime.Now.ToString("ss"), 0) < modifier.GetInt(0, 0, variables);
            },
            "realTimeSecondGreater" => (modifier, variables) =>
            {
                return Parser.TryParse(DateTime.Now.ToString("ss"), 0) > modifier.GetInt(0, 0, variables);
            },

            // minutes
            "realTimeMinuteEquals" => (modifier, variables) =>
            {
                return Parser.TryParse(DateTime.Now.ToString("mm"), 0) == modifier.GetInt(0, 0, variables);
            },
            "realTimeMinuteLesserEquals" => (modifier, variables) =>
            {
                return Parser.TryParse(DateTime.Now.ToString("mm"), 0) <= modifier.GetInt(0, 0, variables);
            },
            "realTimeMinuteGreaterEquals" => (modifier, variables) =>
            {
                return Parser.TryParse(DateTime.Now.ToString("mm"), 0) >= modifier.GetInt(0, 0, variables);
            },
            "realTimeMinuteLesser" => (modifier, variables) =>
            {
                return Parser.TryParse(DateTime.Now.ToString("mm"), 0) < modifier.GetInt(0, 0, variables);
            },
            "realTimeMinuteGreater" => (modifier, variables) =>
            {
                return Parser.TryParse(DateTime.Now.ToString("mm"), 0) > modifier.GetInt(0, 0, variables);
            },

            // 24 hours
            "realTime24HourEquals" => (modifier, variables) =>
            {
                return Parser.TryParse(DateTime.Now.ToString("HH"), 0) == modifier.GetInt(0, 0, variables);
            },
            "realTime24HourLesserEquals" => (modifier, variables) =>
            {
                return Parser.TryParse(DateTime.Now.ToString("HH"), 0) <= modifier.GetInt(0, 0, variables);
            },
            "realTime24HourGreaterEquals" => (modifier, variables) =>
            {
                return Parser.TryParse(DateTime.Now.ToString("HH"), 0) >= modifier.GetInt(0, 0, variables);
            },
            "realTime24HourLesser" => (modifier, variables) =>
            {
                return Parser.TryParse(DateTime.Now.ToString("HH"), 0) < modifier.GetInt(0, 0, variables);
            },
            "realTime24HourGreater" => (modifier, variables) =>
            {
                return Parser.TryParse(DateTime.Now.ToString("HH"), 0) > modifier.GetInt(0, 0, variables);
            },

            // 12 hours
            "realTime12HourEquals" => (modifier, variables) =>
            {
                return Parser.TryParse(DateTime.Now.ToString("hh"), 0) == modifier.GetInt(0, 0, variables);
            },
            "realTime12HourLesserEquals" => (modifier, variables) =>
            {
                return Parser.TryParse(DateTime.Now.ToString("hh"), 0) <= modifier.GetInt(0, 0, variables);
            },
            "realTime12HourGreaterEquals" => (modifier, variables) =>
            {
                return Parser.TryParse(DateTime.Now.ToString("hh"), 0) >= modifier.GetInt(0, 0, variables);
            },
            "realTime12HourLesser" => (modifier, variables) =>
            {
                return Parser.TryParse(DateTime.Now.ToString("hh"), 0) < modifier.GetInt(0, 0, variables);
            },
            "realTime12HourGreater" => (modifier, variables) =>
            {
                return Parser.TryParse(DateTime.Now.ToString("hh"), 0) > modifier.GetInt(0, 0, variables);
            },

            // days
            "realTimeDayEquals" => (modifier, variables) =>
            {
                return Parser.TryParse(DateTime.Now.ToString("dd"), 0) == modifier.GetInt(0, 0, variables);
            },
            "realTimeDayLesserEquals" => (modifier, variables) =>
            {
                return Parser.TryParse(DateTime.Now.ToString("dd"), 0) <= modifier.GetInt(0, 0, variables);
            },
            "realTimeDayGreaterEquals" => (modifier, variables) =>
            {
                return Parser.TryParse(DateTime.Now.ToString("dd"), 0) >= modifier.GetInt(0, 0, variables);
            },
            "realTimeDayLesser" => (modifier, variables) =>
            {
                return Parser.TryParse(DateTime.Now.ToString("dd"), 0) < modifier.GetInt(0, 0, variables);
            },
            "realTimeDayGreater" => (modifier, variables) =>
            {
                return Parser.TryParse(DateTime.Now.ToString("dd"), 0) > modifier.GetInt(0, 0, variables);
            },

            // months
            "realTimeMonthEquals" => (modifier, variables) =>
            {
                return Parser.TryParse(DateTime.Now.ToString("MM"), 0) == modifier.GetInt(0, 0, variables);
            },
            "realTimeMonthLesserEquals" => (modifier, variables) =>
            {
                return Parser.TryParse(DateTime.Now.ToString("MM"), 0) <= modifier.GetInt(0, 0, variables);
            },
            "realTimeMonthGreaterEquals" => (modifier, variables) =>
            {
                return Parser.TryParse(DateTime.Now.ToString("MM"), 0) >= modifier.GetInt(0, 0, variables);
            },
            "realTimeMonthLesser" => (modifier, variables) =>
            {
                return Parser.TryParse(DateTime.Now.ToString("MM"), 0) < modifier.GetInt(0, 0, variables);
            },
            "realTimeMonthGreater" => (modifier, variables) =>
            {
                return Parser.TryParse(DateTime.Now.ToString("MM"), 0) > modifier.GetInt(0, 0, variables);
            },

            // years
            "realTimeYearEquals" => (modifier, variables) =>
            {
                return Parser.TryParse(DateTime.Now.ToString("yyyy"), 0) == modifier.GetInt(0, 0, variables);
            },
            "realTimeYearLesserEquals" => (modifier, variables) =>
            {
                return Parser.TryParse(DateTime.Now.ToString("yyyy"), 0) <= modifier.GetInt(0, 0, variables);
            },
            "realTimeYearGreaterEquals" => (modifier, variables) =>
            {
                return Parser.TryParse(DateTime.Now.ToString("yyyy"), 0) >= modifier.GetInt(0, 0, variables);
            },
            "realTimeYearLesser" => (modifier, variables) =>
            {
                return Parser.TryParse(DateTime.Now.ToString("yyyy"), 0) < modifier.GetInt(0, 0, variables);
            },
            "realTimeYearGreater" => (modifier, variables) =>
            {
                return Parser.TryParse(DateTime.Now.ToString("yyyy"), 0) > modifier.GetInt(0, 0, variables);
            },

            #endregion

            #region Config

            // main
            "usernameEquals" => (modifier, variables) =>
            {
                return CoreConfig.Instance.DisplayName.Value == modifier.GetValue(0, variables);
            },
            "languageEquals" => (modifier, variables) =>
            {
                return CoreConfig.Instance.Language.Value == (Language)modifier.GetInt(0, 0, variables);
            },

            // misc
            "configLDM" => (modifier, variables) =>
            {
                return CoreConfig.Instance.LDM.Value;
            },
            "configShowEffects" => (modifier, variables) =>
            {
                return EventsConfig.Instance.ShowFX.Value;
            },
            "configShowPlayerGUI" => (modifier, variables) =>
            {
                return EventsConfig.Instance.ShowGUI.Value;
            },
            "configShowIntro" => (modifier, variables) =>
            {
                return EventsConfig.Instance.ShowIntro.Value;
            },

            #endregion

            #region Misc

            "inEditor" => (modifier, variables) => CoreHelper.InEditor,
            "requireSignal" => (modifier, variables) => modifier.HasResult(),
            "isFullscreen" => (modifier, variables) => Screen.fullScreen,
            "objectAlive" => (modifier, variables) =>
            {
                var list = GameData.Current.FindObjectsWithTag(modifier, modifier.GetValue(0, variables));
                for (int i = 0; i < list.Count; i++)
                {
                    if (list[i].Alive)
                        return true;
                }
                return false;
            },
            "objectSpawned" => (modifier, variables) =>
            {
                if (!modifier.HasResult())
                    modifier.Result = new List<string>();

                var ids = modifier.GetResult<List<string>>();

                var list = GameData.Current.FindObjectsWithTag(modifier, modifier.GetValue(0, variables));
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
            },

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
            "setPitch" => ModifierActions.setPitch,
            "addPitch" => ModifierActions.addPitch,
            "setPitchMath" => ModifierActions.setPitchMath,
            "addPitchMath" => ModifierActions.addPitchMath,

            // music playing states
            "setMusicTime" => ModifierActions.setMusicTime,
            "setMusicTimeMath" => ModifierActions.setMusicTimeMath,
            "setMusicTimeStartTime" => ModifierActions.setMusicTimeStartTime,
            "setMusicTimeAutokill" => ModifierActions.setMusicTimeAutokill,
            "setMusicPlaying" => ModifierActions.setMusicPlaying,

            // play sound
            "playSound" => ModifierActions.playSound,
            "playSoundOnline" => ModifierActions.playSoundOnline,
            "playDefaultSound" => ModifierActions.playDefaultSound,
            "audioSource" => ModifierActions.audioSource,

            #endregion

            #region Level

            "loadLevel" => ModifierActions.loadLevel,
            "loadLevelID" => ModifierActions.loadLevelID,
            "loadLevelInternal" => ModifierActions.loadLevelInternal,
            "loadLevelPrevious" => ModifierActions.loadLevelPrevious,
            "loadLevelHub" => ModifierActions.loadLevelHub,
            "loadLevelInCollection" => ModifierActions.loadLevelInCollection,
            "downloadLevel" => ModifierActions.downloadLevel,
            "endLevel" => ModifierActions.endLevel,
            "setAudioTransition" => ModifierActions.setAudioTransition,
            "setIntroFade" => ModifierActions.setIntroFade,
            "setLevelEndFunc" => ModifierActions.setLevelEndFunc,

            #endregion

            #region Component

            "blur" => ModifierActions.blur,
            "blurOther" => ModifierActions.blurOther,
            "blurVariable" => ModifierActions.blurVariable,
            "blurVariableOther" => ModifierActions.blurVariableOther,
            "blurColored" => ModifierActions.blurColored,
            "blurColoredOther" => ModifierActions.blurColoredOther,
            "doubleSided" => ModifierActions.doubleSided,
            "particleSystem" => ModifierActions.particleSystem,
            "trailRenderer" => ModifierActions.trailRenderer,
            "rigidbody" => ModifierActions.rigidbody,
            "rigidbodyOther" => ModifierActions.rigidbodyOther,

            #endregion

            #region Player

            // hit
            "playerHit" => ModifierActions.playerHit,
            "playerHitIndex" => ModifierActions.playerHitIndex,
            "playerHitAll" => ModifierActions.playerHitAll,

            // heal
            "playerHeal" => ModifierActions.playerHeal,
            "playerHealIndex" => ModifierActions.playerHealIndex,
            "playerHealAll" => ModifierActions.playerHealAll,

            // kill
            "playerKill" => ModifierActions.playerKill,
            "playerKillIndex" => ModifierActions.playerKillIndex,
            "playerKillAll" => ModifierActions.playerKillAll,

            // respawn
            "playerRespawn" => ModifierActions.playerRespawn,
            "playerRespawnIndex" => ModifierActions.playerRespawnIndex,
            "playerRespawnAll" => ModifierActions.playerRespawnAll,

            // player move
            "playerMove" => ModifierActions.playerMove,
            "playerMoveIndex" => ModifierActions.playerMoveIndex,
            "playerMoveAll" => ModifierActions.playerMoveAll,
            "playerMoveX" => ModifierActions.playerMoveX,
            "playerMoveXIndex" => ModifierActions.playerMoveXIndex,
            "playerMoveXAll" => ModifierActions.playerMoveXAll,
            "playerMoveY" => ModifierActions.playerMoveY,
            "playerMoveYIndex" => ModifierActions.playerMoveYIndex,
            "playerMoveYAll" => ModifierActions.playerMoveYAll,
            "playerRotate" => ModifierActions.playerRotate,
            "playerRotateIndex" => ModifierActions.playerRotateIndex,
            "playerRotateAll" => ModifierActions.playerRotateAll,

            // move to object
            "playerMoveToObject" => ModifierActions.playerMoveToObject,
            "playerMoveIndexToObject" => ModifierActions.playerMoveIndexToObject,
            "playerMoveAllToObject" => ModifierActions.playerMoveAllToObject,
            "playerMoveXToObject" => ModifierActions.playerMoveXToObject,
            "playerMoveXIndexToObject" => ModifierActions.playerMoveXIndexToObject,
            "playerMoveXAllToObject" => ModifierActions.playerMoveXAllToObject,
            "playerMoveYToObject" => ModifierActions.playerMoveYToObject,
            "playerMoveYIndexToObject" => ModifierActions.playerMoveYIndexToObject,
            "playerMoveYAllToObject" => ModifierActions.playerMoveYAllToObject,
            "playerRotateToObject" => ModifierActions.playerRotateToObject,
            "playerRotateIndexToObject" => ModifierActions.playerRotateIndexToObject,
            "playerRotateAllToObject" => ModifierActions.playerRotateAllToObject,

            // actions
            "playerBoost" => ModifierActions.playerBoost,
            "playerBoostIndex" => ModifierActions.playerBoostIndex,
            "playerBoostAll" => ModifierActions.playerBoostAll,
            "playerDisableBoost" => ModifierActions.playerDisableBoost,
            "playerDisableBoostIndex" => ModifierActions.playerDisableBoostIndex,
            "playerDisableBoostAll" => ModifierActions.playerDisableBoostAll,
            "playerEnableBoost" => ModifierActions.playerEnableBoost,
            "playerEnableBoostIndex" => ModifierActions.playerEnableBoostIndex,
            "playerEnableBoostAll" => ModifierActions.playerEnableBoostAll,

            // speed
            "playerSpeed" => ModifierActions.playerSpeed,
            "playerVelocityAll" => ModifierActions.playerVelocityAll,
            "playerVelocityXAll" => ModifierActions.playerVelocityXAll,
            "playerVelocityYAll" => ModifierActions.playerVelocityYAll,

            "setPlayerModel" => ModifierActions.setPlayerModel,
            "setGameMode" => ModifierActions.setGameMode,
            "gameMode" => ModifierActions.gameMode,

            "blackHole" => ModifierActions.blackHole,

            #endregion

            #region Mouse Cursor

            "showMouse" => ModifierActions.showMouse,
            "hideMouse" => ModifierActions.hideMouse,
            "setMousePosition" => ModifierActions.setMousePosition,
            "followMousePosition" => ModifierActions.followMousePosition,

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
            "getAxis" => ModifierActions.getAxis,
            "getMath" => ModifierActions.getMath,
            nameof(ModifierActions.getNearestPlayer) => ModifierActions.getNearestPlayer,
            nameof(ModifierActions.getCollidingPlayers) => ModifierActions.getCollidingPlayers,
            "getPlayerHealth" => ModifierActions.getPlayerHealth,
            "getPlayerPosX" => ModifierActions.getPlayerPosX,
            "getPlayerPosY" => ModifierActions.getPlayerPosY,
            "getPlayerRot" => ModifierActions.getPlayerRot,
            "getEventValue" => ModifierActions.getEventValue,
            "getSample" => ModifierActions.getSample,
            "getText" => ModifierActions.getText,
            "getTextOther" => ModifierActions.getTextOther,
            "getCurrentKey" => ModifierActions.getCurrentKey,
            "getColorSlotHexCode" => ModifierActions.getColorSlotHexCode,
            "getFloatFromHexCode" => ModifierActions.getFloatFromHexCode,
            "getHexCodeFromFloat" => ModifierActions.getHexCodeFromFloat,
            "getJSONString" => ModifierActions.getJSONString,
            "getJSONFloat" => ModifierActions.getJSONFloat,
            "getJSON" => ModifierActions.getJSON,
            "getSubString" => ModifierActions.getSubString,
            nameof(ModifierActions.getSplitString) => ModifierActions.getSplitString,
            nameof(ModifierActions.getSplitStringAt) => ModifierActions.getSplitStringAt,
            nameof(ModifierActions.getSplitStringCount) => ModifierActions.getSplitStringCount,
            "getStringLength" => ModifierActions.getStringLength,
            "getParsedString" => ModifierActions.getParsedString,
            "getRegex" => ModifierActions.getRegex,
            "getFormatVariable" => ModifierActions.getFormatVariable,
            "getComparison" => ModifierActions.getComparison,
            "getComparisonMath" => ModifierActions.getComparisonMath,
            "getMixedColors" => ModifierActions.getMixedColors,
            "getSignaledVariables" => ModifierActions.getSignaledVariables,
            "signalLocalVariables" => ModifierActions.signalLocalVariables,
            "clearLocalVariables" => ModifierActions.clearLocalVariables,

            "addVariable" => ModifierActions.addVariable,
            "addVariableOther" => ModifierActions.addVariableOther,
            "subVariable" => ModifierActions.subVariable,
            "subVariableOther" => ModifierActions.subVariableOther,
            "setVariable" => ModifierActions.setVariable,
            "setVariableOther" => ModifierActions.setVariableOther,
            "setVariableRandom" => ModifierActions.setVariableRandom,
            "setVariableRandomOther" => ModifierActions.setVariableRandomOther,
            "animateVariableOther" => ModifierActions.animateVariableOther,
            "clampVariable" => ModifierActions.clampVariable,
            "clampVariableOther" => ModifierActions.clampVariableOther,

            #endregion

            #region Enable / Disable

            // enable
            "enableObject" => ModifierActions.enableObject,
            "enableObjectTree" => ModifierActions.enableObjectTree,
            "enableObjectOther" => ModifierActions.enableObjectOther,
            "enableObjectTreeOther" => ModifierActions.enableObjectTreeOther,
            "enableObjectGroup" => ModifierActions.enableObjectGroup,

            // disable
            "disableObject" => ModifierActions.disableObject,
            "disableObjectTree" => ModifierActions.disableObjectTree,
            "disableObjectOther" => ModifierActions.disableObjectOther,
            "disableObjectTreeOther" => ModifierActions.disableObjectTreeOther,

            #endregion

            #region JSON

            "saveFloat" => ModifierActions.saveFloat,
            "saveString" => ModifierActions.saveString,
            "saveText" => ModifierActions.saveText,
            "saveVariable" => ModifierActions.saveVariable,
            "loadVariable" => ModifierActions.loadVariable,
            "loadVariableOther" => ModifierActions.loadVariableOther,

            #endregion

            #region Reactive

            // single
            "reactivePos" => ModifierActions.reactivePos,
            "reactiveSca" => ModifierActions.reactiveSca,
            "reactiveRot" => ModifierActions.reactiveRot,
            "reactiveCol" => ModifierActions.reactiveCol,
            "reactiveColLerp" => ModifierActions.reactiveColLerp,

            // chain
            "reactivePosChain" => ModifierActions.reactivePosChain,
            "reactiveScaChain" => ModifierActions.reactiveScaChain,
            "reactiveRotChain" => ModifierActions.reactiveRotChain,

            #endregion

            #region Events

            "eventOffset" => ModifierActions.eventOffset,
            "eventOffsetVariable" => ModifierActions.eventOffsetVariable,
            "eventOffsetMath" => ModifierActions.eventOffsetMath,
            "eventOffsetAnimate" => ModifierActions.eventOffsetAnimate,
            "eventOffsetCopyAxis" => ModifierActions.eventOffsetCopyAxis,
            "vignetteTracksPlayer" => ModifierActions.vignetteTracksPlayer,
            "lensTracksPlayer" => ModifierActions.lensTracksPlayer,

            #endregion

            // todo: implement gradients and different color controls
            #region Color

            // color
            "addColor" => ModifierActions.addColor,
            "addColorOther" => ModifierActions.addColorOther,
            "lerpColor" => ModifierActions.lerpColor,
            "lerpColorOther" => ModifierActions.lerpColorOther,
            "addColorPlayerDistance" => ModifierActions.addColorPlayerDistance,
            "lerpColorPlayerDistance" => ModifierActions.lerpColorPlayerDistance,

            // opacity
            "setAlpha" => ModifierActions.setOpacity,
            "setOpacity" => ModifierActions.setOpacity,
            "setAlphaOther" => ModifierActions.setOpacityOther,
            "setOpacityOther" => ModifierActions.setOpacityOther,

            // copy
            "copyColor" => ModifierActions.copyColor,
            "copyColorOther" => ModifierActions.copyColorOther,
            "applyColorGroup" => ModifierActions.applyColorGroup,

            // hex code
            "setColorHex" => ModifierActions.setColorHex,
            "setColorHexOther" => ModifierActions.setColorHexOther,

            // rgba
            "setColorRGBA" => ModifierActions.setColorRGBA,
            "setColorRGBAOther" => ModifierActions.setColorRGBAOther,

            #endregion

            // todo: figure out how to get actorFrameTexture to work
            // todo: add polygon modifier stuff
            #region Shape

            "actorFrameTexture" => ModifierActions.actorFrameTexture,

            // image
            "setImage" => ModifierActions.setImage,
            "setImageOther" => ModifierActions.setImageOther,

            // text (pain)
            "setText" => ModifierActions.setText,
            "setTextOther" => ModifierActions.setTextOther,
            "addText" => ModifierActions.addText,
            "addTextOther" => ModifierActions.addTextOther,
            "removeText" => ModifierActions.removeText,
            "removeTextOther" => ModifierActions.removeTextOther,
            "removeTextAt" => ModifierActions.removeTextAt,
            "removeTextOtherAt" => ModifierActions.removeTextOtherAt,
            "formatText" => ModifierActions.formatText,
            "textSequence" => ModifierActions.textSequence,

            // modify shape
            "backgroundShape" => ModifierActions.backgroundShape,
            "sphereShape" => ModifierActions.sphereShape,
            "translateShape" => ModifierActions.translateShape,

            #endregion

            #region Animation

            "animateObject" => ModifierActions.animateObject,
            "animateObjectOther" => ModifierActions.animateObjectOther,
            "animateSignal" => ModifierActions.animateSignal,
            "animateSignalOther" => ModifierActions.animateSignalOther,

            "animateObjectMath" => ModifierActions.animateObjectMath,
            "animateObjectMathOther" => ModifierActions.animateObjectMathOther,
            "animateSignalMath" => ModifierActions.animateSignalMath,
            "animateSignalMathOther" => ModifierActions.animateSignalMathOther,

            "gravity" => ModifierActions.gravity,
            "gravityOther" => ModifierActions.gravityOther,

            "copyAxis" => ModifierActions.copyAxis,
            "copyAxisMath" => ModifierActions.copyAxisMath,
            "copyAxisGroup" => ModifierActions.copyAxisGroup,
            "copyPlayerAxis" => ModifierActions.copyPlayerAxis,
            "legacyTail" => ModifierActions.legacyTail,

            "applyAnimation" => ModifierActions.applyAnimation,
            "applyAnimationFrom" => ModifierActions.applyAnimationFrom,
            "applyAnimationTo" => ModifierActions.applyAnimationTo,
            "applyAnimationMath" => ModifierActions.applyAnimationMath,
            "applyAnimationFromMath" => ModifierActions.applyAnimationFromMath,
            "applyAnimationToMath" => ModifierActions.applyAnimationToMath,

            #endregion

            #region Prefab

            "spawnPrefab" => ModifierActions.spawnPrefab,
            "spawnPrefabOffset" => ModifierActions.spawnPrefabOffset,
            "spawnPrefabOffsetOther" => ModifierActions.spawnPrefabOffsetOther,
            "spawnMultiPrefab" => ModifierActions.spawnMultiPrefab,
            "spawnMultiPrefabOffset" => ModifierActions.spawnMultiPrefabOffset,
            "spawnMultiPrefabOffsetOther" => ModifierActions.spawnMultiPrefabOffsetOther,
            "clearSpawnedPrefabs" => ModifierActions.clearSpawnedPrefabs,

            #endregion

            #region Ranking

            "saveLevelRank" => ModifierActions.saveLevelRank,

            "clearHits" => ModifierActions.clearHits,
            "addHit" => ModifierActions.addHit,
            "subHit" => ModifierActions.subHit,
            "clearDeaths" => ModifierActions.clearDeaths,
            "addDeath" => ModifierActions.addDeath,
            "subDeath" => ModifierActions.subDeath,

            #endregion

            #region Updates

            // update
            "updateObjects" => ModifierActions.updateObjects,
            "updateObject" => ModifierActions.updateObject,

            // parent
            "setParent" => ModifierActions.setParent,
            "setParentOther" => ModifierActions.setParentOther,
            "detachParent" => ModifierActions.detachParent,
            "detachParentOther" => ModifierActions.detachParentOther,

            #endregion

            #region Physics

            // collision
            "setCollision" => ModifierActions.setCollision,
            "setCollisionOther" => ModifierActions.setCollisionOther,

            #endregion

            #region Checkpoints

            nameof(ModifierActions.createCheckpoint) => ModifierActions.createCheckpoint,
            nameof(ModifierActions.resetCheckpoint) => ModifierActions.resetCheckpoint,

            #endregion

            #region Interfaces

            "loadInterface" => ModifierActions.loadInterface,
            "quitToMenu" => ModifierActions.quitToMenu,
            "quitToArcade" => ModifierActions.quitToArcade,
            "pauseLevel" => ModifierActions.pauseLevel,

            #endregion

            #region Misc

            "setBGActive" => ModifierActions.setBGActive,

            // activation
            "signalModifier" => ModifierActions.signalModifier,
            "activateModifier" => ModifierActions.activateModifier,

            "editorNotify" => ModifierActions.editorNotify,

            // external
            "setWindowTitle" => ModifierActions.setWindowTitle,
            "setDiscordStatus" => ModifierActions.setDiscordStatus,

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
                            if (modifier.constant && modifier.reference.shape == 4 && modifier.reference.runtimeObject && modifier.reference.runtimeObject.visualObject != null &&
                                modifier.reference.runtimeObject.visualObject is TextObject textObject)
                                textObject.text = modifier.reference.text;
                            break;
                        }
                    case "setTextOther": {
                            var list = GameData.Current.FindObjectsWithTag(modifier, modifier.GetValue(1));

                            if (modifier.constant && !list.IsEmpty())
                                foreach (var bm in list)
                                    if (bm.shape == 4 && bm.runtimeObject && bm.runtimeObject.visualObject != null &&
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
            _ => (modifier, variables) => false,
        };

        public static Action<Modifier<BackgroundObject>, Dictionary<string, string>> GetBGAction(string key) => key switch
        {
            "setActive" => ModifierActions.setActive,
            "setActiveOther" => ModifierActions.setActiveOther,

            #region Audio

            // pitch
            "setPitch" => ModifierActions.setPitch,
            "addPitch" => ModifierActions.addPitch,
            "setPitchMath" => ModifierActions.setPitchMath,
            "addPitchMath" => ModifierActions.addPitchMath,

            // music playing states
            "setMusicTime" => ModifierActions.setMusicTime,
            "setMusicTimeMath" => ModifierActions.setMusicTimeMath,
            "setMusicTimeStartTime" => ModifierActions.setMusicTimeStartTime,
            "setMusicTimeAutokill" => ModifierActions.setMusicTimeAutokill,
            "setMusicPlaying" => ModifierActions.setMusicPlaying,

            // play sound
            "playSound" => ModifierActions.playSound,
            "playSoundOnline" => ModifierActions.playSoundOnline,
            "playDefaultSound" => ModifierActions.playDefaultSound,
            //"audioSource" => ModifierActions.audioSource,

            #endregion

            #region Level

            "loadLevel" => ModifierActions.loadLevel,
            "loadLevelID" => ModifierActions.loadLevelID,
            "loadLevelInternal" => ModifierActions.loadLevelInternal,
            "loadLevelPrevious" => ModifierActions.loadLevelPrevious,
            "loadLevelHub" => ModifierActions.loadLevelHub,
            "loadLevelInCollection" => ModifierActions.loadLevelInCollection,
            "downloadLevel" => ModifierActions.downloadLevel,
            "endLevel" => ModifierActions.endLevel,
            "setAudioTransition" => ModifierActions.setAudioTransition,
            "setIntroFade" => ModifierActions.setIntroFade,
            "setLevelEndFunc" => ModifierActions.setLevelEndFunc,

            #endregion

            // Component

            #region Player

            // hit
            //"playerHit" => ModifierActions.playerHit,
            "playerHitIndex" => ModifierActions.playerHitIndex,
            "playerHitAll" => ModifierActions.playerHitAll,

            // heal
            //"playerHeal" => ModifierActions.playerHeal,
            "playerHealIndex" => ModifierActions.playerHealIndex,
            "playerHealAll" => ModifierActions.playerHealAll,

            // kill
            //"playerKill" => ModifierActions.playerKill,
            "playerKillIndex" => ModifierActions.playerKillIndex,
            "playerKillAll" => ModifierActions.playerKillAll,

            // respawn
            //"playerRespawn" => ModifierActions.playerRespawn,
            "playerRespawnIndex" => ModifierActions.playerRespawnIndex,
            "playerRespawnAll" => ModifierActions.playerRespawnAll,

            // player move
            //"playerMove" => ModifierActions.playerMove,
            "playerMoveIndex" => ModifierActions.playerMoveIndex,
            "playerMoveAll" => ModifierActions.playerMoveAll,
            //"playerMoveX" => ModifierActions.playerMoveX,
            "playerMoveXIndex" => ModifierActions.playerMoveXIndex,
            "playerMoveXAll" => ModifierActions.playerMoveXAll,
            //"playerMoveY" => ModifierActions.playerMoveY,
            "playerMoveYIndex" => ModifierActions.playerMoveYIndex,
            "playerMoveYAll" => ModifierActions.playerMoveYAll,
            //"playerRotate" => ModifierActions.playerRotate,
            "playerRotateIndex" => ModifierActions.playerRotateIndex,
            "playerRotateAll" => ModifierActions.playerRotateAll,

            // move to object
            //"playerMoveToObject" => ModifierActions.playerMoveToObject,
            //"playerMoveIndexToObject" => ModifierActions.playerMoveIndexToObject,
            //"playerMoveAllToObject" => ModifierActions.playerMoveAllToObject,
            //"playerMoveXToObject" => ModifierActions.playerMoveXToObject,
            //"playerMoveXIndexToObject" => ModifierActions.playerMoveXIndexToObject,
            //"playerMoveXAllToObject" => ModifierActions.playerMoveXAllToObject,
            //"playerMoveYToObject" => ModifierActions.playerMoveYToObject,
            //"playerMoveYIndexToObject" => ModifierActions.playerMoveYIndexToObject,
            //"playerMoveYAllToObject" => ModifierActions.playerMoveYAllToObject,
            //"playerRotateToObject" => ModifierActions.playerRotateToObject,
            //"playerRotateIndexToObject" => ModifierActions.playerRotateIndexToObject,
            //"playerRotateAllToObject" => ModifierActions.playerRotateAllToObject,

            // actions
            //"playerBoost" => ModifierActions.playerBoost,
            "playerBoostIndex" => ModifierActions.playerBoostIndex,
            "playerBoostAll" => ModifierActions.playerBoostAll,
            //"playerDisableBoost" => ModifierActions.playerDisableBoost,
            "playerDisableBoostIndex" => ModifierActions.playerDisableBoostIndex,
            "playerDisableBoostAll" => ModifierActions.playerDisableBoostAll,
            //"playerEnableBoost" => ModifierActions.playerEnableBoost,
            "playerEnableBoostIndex" => ModifierActions.playerEnableBoostIndex,
            "playerEnableBoostAll" => ModifierActions.playerEnableBoostAll,

            // speed
            "playerSpeed" => ModifierActions.playerSpeed,
            "playerVelocityAll" => ModifierActions.playerVelocityAll,
            "playerVelocityXAll" => ModifierActions.playerVelocityXAll,
            "playerVelocityYAll" => ModifierActions.playerVelocityYAll,

            "setPlayerModel" => ModifierActions.setPlayerModel,
            "gameMode" => ModifierActions.gameMode,

            //"blackHole" => ModifierActions.blackHole,

            #endregion

            #region Mouse Cursor

            "showMouse" => ModifierActions.showMouse,
            "hideMouse" => ModifierActions.hideMouse,
            "setMousePosition" => ModifierActions.setMousePosition,
            "followMousePosition" => ModifierActions.followMousePosition,

            #endregion

            #region Variable

            "getToggle" => ModifierActions.getToggle,
            "getFloat" => ModifierActions.getFloat,
            "getInt" => ModifierActions.getInt,
            "getString" => ModifierActions.getString,
            "getStringLower" => ModifierActions.getStringLower,
            "getStringUpper" => ModifierActions.getStringUpper,
            "getColor" => ModifierActions.getColor,
            "getEnum" => ModifierActions.getEnum,
            "getPitch" => ModifierActions.getPitch,
            "getMusicTime" => ModifierActions.getMusicTime,
            //"getAxis" => ModifierActions.getAxis,
            "getMath" => ModifierActions.getMath,
            "getNearestPlayer" => ModifierActions.getNearestPlayer,
            "getPlayerHealth" => ModifierActions.getPlayerHealth,
            "getPlayerPosX" => ModifierActions.getPlayerPosX,
            "getPlayerPosY" => ModifierActions.getPlayerPosY,
            "getPlayerRot" => ModifierActions.getPlayerRot,
            "getEventValue" => ModifierActions.getEventValue,
            "getSample" => ModifierActions.getSample,
            //"getText" => ModifierActions.getText,
            //"getTextOther" => ModifierActions.getTextOther,
            "getCurrentKey" => ModifierActions.getCurrentKey,
            "getColorSlotHexCode" => ModifierActions.getColorSlotHexCode,
            "getFloatFromHexCode" => ModifierActions.getFloatFromHexCode,
            "getHexCodeFromFloat" => ModifierActions.getHexCodeFromFloat,
            "getJSONString" => ModifierActions.getJSONString,
            "getJSONFloat" => ModifierActions.getJSONFloat,
            "getJSON" => ModifierActions.getJSON,
            "getSubString" => ModifierActions.getSubString,
            nameof(ModifierActions.getSplitString) => ModifierActions.getSplitString,
            nameof(ModifierActions.getSplitStringAt) => ModifierActions.getSplitStringAt,
            nameof(ModifierActions.getSplitStringCount) => ModifierActions.getSplitStringCount,
            "getParsedString" => ModifierActions.getParsedString,
            "getStringLength" => ModifierActions.getStringLength,
            "getRegex" => ModifierActions.getRegex,
            "getFormatVariable" => ModifierActions.getFormatVariable,
            "getComparison" => ModifierActions.getComparison,
            "getComparisonMath" => ModifierActions.getComparisonMath,
            "getMixedColors" => ModifierActions.getMixedColors,
            "getSignaledVariables" => ModifierActions.getSignaledVariables,
            "signalLocalVariables" => ModifierActions.signalLocalVariables,
            "clearLocalVariables" => ModifierActions.clearLocalVariables,

            "addVariable" => ModifierActions.addVariable,
            "addVariableOther" => ModifierActions.addVariableOther,
            "subVariable" => ModifierActions.subVariable,
            "subVariableOther" => ModifierActions.subVariableOther,
            "setVariable" => ModifierActions.setVariable,
            "setVariableOther" => ModifierActions.setVariableOther,
            "setVariableRandom" => ModifierActions.setVariableRandom,
            "setVariableRandomOther" => ModifierActions.setVariableRandomOther,
            "animateVariableOther" => ModifierActions.animateVariableOther,
            "clampVariable" => ModifierActions.clampVariable,
            "clampVariableOther" => ModifierActions.clampVariableOther,

            #endregion

            #region Animation

            "animateObject" => ModifierActions.animateObject,
            "animateObjectOther" => ModifierActions.animateObjectOther,
            "animateSignal" => ModifierActions.animateSignal,
            "animateSignalOther" => ModifierActions.animateSignalOther,

            "animateObjectMath" => ModifierActions.animateObjectMath,
            "animateObjectMathOther" => ModifierActions.animateObjectMathOther,
            "animateSignalMath" => ModifierActions.animateSignalMath,
            "animateSignalMathOther" => ModifierActions.animateSignalMathOther,

            "gravity" => ModifierActions.gravity,
            "gravityOther" => ModifierActions.gravityOther,

            "copyAxis" => ModifierActions.copyAxis,
            "copyAxisMath" => ModifierActions.copyAxisMath,
            "copyAxisGroup" => ModifierActions.copyAxisGroup,
            "copyPlayerAxis" => ModifierActions.copyPlayerAxis,
            //"legacyTail" => ModifierActions.legacyTail,

            //"applyAnimation" => ModifierActions.applyAnimation,
            //"applyAnimationFrom" => ModifierActions.applyAnimationFrom,
            //"applyAnimationTo" => ModifierActions.applyAnimationTo,
            //"applyAnimationMath" => ModifierActions.applyAnimationMath,
            //"applyAnimationFromMath" => ModifierActions.applyAnimationFromMath,
            //"applyAnimationToMath" => ModifierActions.applyAnimationToMath,

            #endregion

            #region Checkpoints

            nameof(ModifierActions.createCheckpoint) => ModifierActions.createCheckpoint,
            nameof(ModifierActions.resetCheckpoint) => ModifierActions.resetCheckpoint,

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
        }

        #endregion

        #region Player

        /// <summary>
        /// The function to run when a <see cref="ModifierBase.Type.Trigger"/> modifier is running and has a reference of <see cref="CustomPlayer"/>.
        /// </summary>
        /// <param name="modifier">Modifier to run.</param>
        /// <returns>Returns true if the modifier was triggered, otherwise returns false.</returns>
        public static bool PlayerTrigger(Modifier<CustomPlayer> modifier, Dictionary<string, string> variables = null)
        {
            if (!modifier.verified)
            {
                modifier.verified = true;
                modifier.VerifyModifier(null);
            }
            
            if (modifier.commands.IsEmpty() || !modifier.reference)
                return false;

            modifier.hasChanged = false;

            switch (modifier.Name)
            {
                case "keyPressDown": {
                        return int.TryParse(modifier.value, out int num) && Input.GetKeyDown((KeyCode)num);
                    }
                case "keyPress": {
                        return int.TryParse(modifier.value, out int num) && Input.GetKey((KeyCode)num);
                    }
                case "keyPressUp": {
                        return int.TryParse(modifier.value, out int num) && Input.GetKeyUp((KeyCode)num);
                    }
                case "mouseButtonDown": {
                        return int.TryParse(modifier.value, out int num) && Input.GetMouseButtonDown(num);
                    }
                case "mouseButton": {
                        return int.TryParse(modifier.value, out int num) && Input.GetMouseButton(num);
                    }
                case "mouseButtonUp": {
                        return int.TryParse(modifier.value, out int num) && Input.GetMouseButtonUp(num);
                    }
                case "controlPressDown": {
                        var type = modifier.GetInt(0, 0);
                        var device = modifier.reference.device;

                        return Enum.TryParse(((PlayerInputControlType)type).ToString(), out InControl.InputControlType inputControlType) && device.GetControl(inputControlType).WasPressed;
                    }
                case "controlPress": {
                        var type = modifier.GetInt(0, 0);
                        var device = modifier.reference.device;

                        return Enum.TryParse(((PlayerInputControlType)type).ToString(), out InControl.InputControlType inputControlType) && device.GetControl(inputControlType).IsPressed;
                    }
                case "controlPressUp": {
                        var type = modifier.GetInt(0, 0);
                        var device = modifier.reference.device;

                        return Enum.TryParse(((PlayerInputControlType)type).ToString(), out InControl.InputControlType inputControlType) && device.GetControl(inputControlType).WasReleased;
                    }
                case "healthEquals": {
                        return modifier.reference.Health == modifier.GetInt(0, 3);
                    }
                case "healthGreaterEquals": {
                        return modifier.reference.Health >= modifier.GetInt(0, 3);
                    }
                case "healthLesserEquals": {
                        return modifier.reference.Health <= modifier.GetInt(0, 3);
                    }
                case "healthGreater": {
                        return modifier.reference.Health > modifier.GetInt(0, 3);
                    }
                case "healthLesser": {
                        return modifier.reference.Health < modifier.GetInt(0, 3);
                    }
                case "healthPerEquals": {
                        var health = ((float)modifier.reference.Health / modifier.reference.PlayerModel.basePart.health) * 100f;

                        return health == Parser.TryParse(modifier.value, 50f);
                    }
                case "healthPerGreaterEquals": {
                        var health = ((float)modifier.reference.Health / modifier.reference.PlayerModel.basePart.health) * 100f;

                        return health >= Parser.TryParse(modifier.value, 50f);
                    }
                case "healthPerLesserEquals": {
                        var health = ((float)modifier.reference.Health / modifier.reference.PlayerModel.basePart.health) * 100f;

                        return health <= Parser.TryParse(modifier.value, 50f);
                    }
                case "healthPerGreater": {
                        var health = ((float)modifier.reference.Health / modifier.reference.PlayerModel.basePart.health) * 100f;

                        return health > Parser.TryParse(modifier.value, 50f);
                    }
                case "healthPerLesser": {
                        var health = ((float)modifier.reference.Health / modifier.reference.PlayerModel.basePart.health) * 100f;

                        return health < Parser.TryParse(modifier.value, 50f);
                    }
                case "isDead": {
                        return modifier.reference.Player && modifier.reference.Player.isDead;
                    }
                case "isBoosting": {
                        return modifier.reference.Player && modifier.reference.Player.isBoosting;
                    }
                case "isColliding": {
                        return modifier.reference.Player && modifier.reference.Player.triggerColliding;
                    }
                case "isSolidColliding": {
                        return modifier.reference.Player && modifier.reference.Player.colliding;
                    }
            }

            return false;
        }

        /// <summary>
        /// The function to run when a <see cref="ModifierBase.Type.Action"/> modifier is running and has a reference of <see cref="CustomPlayer"/>.
        /// </summary>
        /// <param name="modifier">Modifier to run.</param>
        public static void PlayerAction(Modifier<CustomPlayer> modifier, Dictionary<string, string> variables = null)
        {
            if (!modifier.verified)
            {
                modifier.verified = true;
                modifier.VerifyModifier(ModifiersManager.defaultPlayerModifiers);
            }

            if (modifier.commands.IsEmpty() || !modifier.reference)
                return;

            modifier.hasChanged = false;

            switch (modifier.Name)
            {
                case "setCustomActive": {
                        if (modifier.reference.Player && modifier.reference.Player.customObjects.TryFind(x => x.id == modifier.GetValue(1), out RTPlayer.CustomObject customObject))
                            customObject.active = Parser.TryParse(modifier.value, false);

                        break;
                    }
                case "kill": {
                        modifier.reference.Health = 0;
                        break;
                    }
                case "hit": {
                        if (modifier.reference.Player)
                            modifier.reference.Player.Hit();
                        break;
                    }
                case "boost": {
                        if (modifier.reference.Player)
                            modifier.reference.Player.Boost();
                        break;
                    }
                case "shoot": {
                        if (modifier.reference.Player)
                            modifier.reference.Player.Shoot();
                        break;
                    }
                case "pulse": {
                        if (modifier.reference.Player)
                            modifier.reference.Player.Pulse();
                        break;
                    }
                case "jump": {
                        if (modifier.reference.Player)
                            modifier.reference.Player.Jump();
                        break;
                    }
                case "signalModifier": {
                        var list = GameData.Current.FindObjectsWithTag(modifier.GetValue(1));

                        foreach (var bm in list)
                            CoroutineHelper.StartCoroutine(ActivateModifier(bm, Parser.TryParse(modifier.value, 0f)));

                        break;
                    }
                case "playAnimation": {
                        if (modifier.reference.Player && modifier.reference.Player.customObjects.TryFind(x => x.id == modifier.GetValue(0), out RTPlayer.CustomObject customObject) && customObject.reference && customObject.reference.animations.TryFind(x => x.ReferenceID == modifier.GetValue(1), out PAAnimation animation))
                        {
                            var runtimeAnimation = new RTAnimation("Custom Animation");
                            modifier.reference.Player.ApplyAnimation(runtimeAnimation, animation, customObject);
                            modifier.reference.Player.animationController.Play(runtimeAnimation);
                        }

                        break;
                    }
                case "setIdleAnimation": {
                        if (modifier.reference.Player && modifier.reference.Player.customObjects.TryFind(x => x.id == modifier.GetValue(0), out RTPlayer.CustomObject customObject) && customObject.reference && customObject.reference.animations.TryFind(x => x.ReferenceID == modifier.GetValue(1), out PAAnimation animation))
                            customObject.currentIdleAnimation = animation.ReferenceID;

                        break;
                    }
                case "playDefaultSound": {
                        if (!float.TryParse(modifier.commands[1], out float pitch) || !float.TryParse(modifier.commands[2], out float vol) || !bool.TryParse(modifier.commands[3], out bool loop) || !AudioManager.inst.library.soundClips.TryGetValue(modifier.value, out AudioClip[] audioClips))
                            break;

                        var clip = audioClips[UnityEngine.Random.Range(0, audioClips.Length)];
                        var audioSource = Camera.main.gameObject.AddComponent<AudioSource>();
                        audioSource.clip = clip;
                        audioSource.playOnAwake = true;
                        audioSource.loop = loop;
                        audioSource.pitch = pitch * AudioManager.inst.CurrentAudioSource.pitch;
                        audioSource.volume = vol * AudioManager.inst.sfxVol;
                        audioSource.Play();

                        float x = pitch * AudioManager.inst.CurrentAudioSource.pitch;
                        if (x == 0f)
                            x = 1f;
                        if (x < 0f)
                            x = -x;

                        CoroutineHelper.StartCoroutine(AudioManager.inst.DestroyWithDelay(audioSource, clip.length / x));

                        break;
                    }
                case "animateObject": {
                        if (int.TryParse(modifier.GetValue(1), out int type)
                            && float.TryParse(modifier.GetValue(2), out float x) && float.TryParse(modifier.GetValue(3), out float y) && float.TryParse(modifier.GetValue(4), out float z)
                            && bool.TryParse(modifier.GetValue(5), out bool relative) && float.TryParse(modifier.GetValue(0), out float time)
                            && modifier.reference.Player && modifier.reference.Player.customObjects.TryFind(x => x.id == modifier.GetValue(7), out RTPlayer.CustomObject customObject))
                        {
                            string easing = modifier.GetValue(6);
                            if (int.TryParse(easing, out int e) && e >= 0 && e < DataManager.inst.AnimationList.Count)
                                easing = DataManager.inst.AnimationList[e].Name;

                            Vector3 vector = customObject.GetTransformOffset(type);

                            var setVector = new Vector3(x, y, z) + (relative ? vector : Vector3.zero);

                            if (!modifier.constant)
                            {
                                var animation = new RTAnimation("Animate Object Offset");

                                animation.animationHandlers = new List<AnimationHandlerBase>
                                    {
                                        new AnimationHandler<Vector3>(new List<IKeyframe<Vector3>>
                                        {
                                            new Vector3Keyframe(0f, vector, Ease.Linear),
                                            new Vector3Keyframe(Mathf.Clamp(time, 0f, 9999f), setVector, Ease.HasEaseFunction(easing) ? Ease.GetEaseFunction(easing) : Ease.Linear),
                                        }, vector3 => customObject.SetTransform(type, vector3), interpolateOnComplete: true),
                                    };
                                animation.onComplete = () => AnimationManager.inst.Remove(animation.id);
                                AnimationManager.inst.Play(animation);
                                break;
                            }

                            customObject.SetTransform(type, setVector);
                        }

                        break;
                    }
            }
        }

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
