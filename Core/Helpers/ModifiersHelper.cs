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
using BetterLegacy.Core.Optimization;
using BetterLegacy.Core.Optimization.Objects;
using BetterLegacy.Core.Optimization.Objects.Visual;
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

        #region Running

        /// <summary>
        /// Checks if triggers return true.
        /// </summary>
        /// <typeparam name="T">The type of <see cref="Modifier{T}"/>.</typeparam>
        /// <param name="triggers">Triggers to check.</param>
        /// <returns>Returns true if all modifiers are active or if some have else if on, otherwise returns false.</returns>
        public static bool CheckTriggers<T>(List<Modifier<T>> triggers)
        {
            bool result = true;
            triggers.ForLoop(trigger =>
            {
                var innerResult = !trigger.active && (trigger.not ? !trigger.Trigger(trigger) : trigger.Trigger(trigger));

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
                        AssignModifierAction(modifier as Modifier<BackgroundObject>, BGAction, BGTrigger, BGInactive);
                        break;
                    }
                case ModifierReferenceType.CustomPlayer: {
                        AssignModifierAction(modifier as Modifier<CustomPlayer>, PlayerAction, PlayerTrigger, PlayerInactive);
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
        public static void AssignModifierAction<T>(Modifier<T> modifier, Action<Modifier<T>> action, Predicate<Modifier<T>> trigger, Action<Modifier<T>> inactive)
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
        public static void RunModifiersAll<T>(List<Modifier<T>> modifiers, bool active)
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

            if (active)
            {
                if (!triggers.IsEmpty())
                {
                    // If all triggers are active
                    if (CheckTriggers(triggers))
                    {
                        actions.ForLoop(act =>
                        {
                            if (act.active) // Continue if modifier is not constant and was already activated
                                return;

                            if (!act.constant)
                                act.active = true;

                            act.running = true;
                            act.Action?.Invoke(act);
                        });
                        triggers.ForLoop(trig =>
                        {
                            if (!trig.constant)
                                trig.active = true;
                            trig.running = true;
                        });
                        return;
                    }

                    // Deactivate both action and trigger modifiers
                    modifiers.ForLoop(modifier =>
                    {
                        if (!modifier.active && (modifier.type == ModifierBase.Type.Trigger || !modifier.running))
                            return;

                        modifier.active = false;
                        modifier.running = false;
                        modifier.Inactive?.Invoke(modifier);
                    });
                    return;
                }

                actions.ForLoop(act =>
                {
                    if (act.active)
                        return;

                    if (!act.constant)
                        act.active = true;

                    act.running = true;
                    act.Action?.Invoke(act);
                });
            }
            else if (modifiers.TryFindAll(x => x.active || x.running, out List<Modifier<T>> findAll))
                findAll.ForLoop(act =>
                {
                    act.active = false;
                    act.running = false;
                    act.Inactive?.Invoke(act);
                });
        }

        /// <summary>
        /// The advanced way modifiers run.
        /// </summary>
        /// <typeparam name="T">The type of <see cref="Modifier{T}"/>.</typeparam>
        /// <param name="modifiers">The list of modifiers to run.</param>
        /// <param name="active">If the object is active.</param>
        public static void RunModifiersLoop<T>(List<Modifier<T>> modifiers, bool active)
        {
            if (active)
            {
                bool result = true; // Action modifiers at the start with no triggers before it should always run, so result is true.
                ModifierBase.Type previousType = ModifierBase.Type.Action;
                modifiers.ForLoop(modifier =>
                {
                    var isAction = modifier.type == ModifierBase.Type.Action;
                    var isTrigger = modifier.type == ModifierBase.Type.Trigger;

                    if (isAction && modifier.Action == null || isTrigger && modifier.Trigger == null || modifier.Inactive == null)
                        AssignModifierActions(modifier);

                    if (isTrigger)
                    {
                        if (previousType == ModifierBase.Type.Action) // If previous modifier was an action modifier, result should be considered true as we just started another modifier-block
                            result = true;

                        var innerResult = !modifier.active && (modifier.not ? !modifier.Trigger(modifier) : modifier.Trigger(modifier));

                        // Allow trigger to turn result to true again if "elseIf" is on
                        if (modifier.elseIf && !result && innerResult)
                            result = true;

                        if (!modifier.elseIf && !innerResult)
                            result = false;

                        modifier.triggered = innerResult;
                        previousType = modifier.type;
                    }

                    // Set modifier inactive state
                    if (!result && !(!modifier.active && !modifier.running))
                    {
                        modifier.active = false;
                        modifier.running = false;
                        modifier.Inactive?.Invoke(modifier);

                        previousType = modifier.type;
                        return;
                    }

                    // Continue if modifier was already active with constant on
                    if (modifier.active || !result)
                    {
                        previousType = modifier.type;
                        return;
                    }

                    // Only occur once
                    if (!modifier.constant)
                        modifier.active = true;

                    modifier.running = true;

                    if (isAction && result) // Only run modifier if result is true
                        modifier.Action?.Invoke(modifier);

                    previousType = modifier.type;
                });
            }
            else if (modifiers.TryFindAll(x => x.active || x.running, out List<Modifier<T>> findAll))
                findAll.ForLoop(modifier =>
                {
                    modifier.active = false;
                    modifier.running = false;
                    modifier.Inactive?.Invoke(modifier);
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

        #region BeatmapObject

        public static Predicate<Modifier<BeatmapObject>> GetObjectTrigger(string key) => key switch
        {
            #region Player

            "playerCollide" => modifier =>
            {
                if (Updater.TryGetObject(modifier.reference, out LevelObject levelObject) && levelObject.visualObject.collider)
                {
                    var collider = levelObject.visualObject.collider;

                    var players = PlayerManager.Players;
                    for (int i = 0; i < players.Count; i++)
                    {
                        var player = players[i];
                        if (!player.Player || !player.Player.CurrentCollider)
                            continue;

                        if (player.Player.CurrentCollider.IsTouching(collider))
                            return true;
                    }
                }
                return false;
            },
            "playerHealthEquals" => modifier =>
            {
                return !InputDataManager.inst.players.IsEmpty() && InputDataManager.inst.players.Any(x => x.health == modifier.GetInt(0, 0));
            },
            "playerHealthLesserEquals" => modifier =>
            {
                return !InputDataManager.inst.players.IsEmpty() && InputDataManager.inst.players.Any(x => x.health <= modifier.GetInt(0, 0));
            },
            "playerHealthGreaterEquals" => modifier =>
            {
                return !InputDataManager.inst.players.IsEmpty() && InputDataManager.inst.players.Any(x => x.health >= modifier.GetInt(0, 0));
            },
            "playerHealthLesser" => modifier =>
            {
                return !InputDataManager.inst.players.IsEmpty() && InputDataManager.inst.players.Any(x => x.health < modifier.GetInt(0, 0));
            },
            "playerHealthGreater" => modifier =>
            {
                return !InputDataManager.inst.players.IsEmpty() && InputDataManager.inst.players.Any(x => x.health > modifier.GetInt(0, 0));
            },
            "playerMoving" => modifier =>
            {
                for (int i = 0; i < GameManager.inst.players.transform.childCount; i++)
                {
                    if (GameManager.inst.players.transform.Find(string.Format("Player {0}", i + 1)))
                    {
                        var player = GameManager.inst.players.transform.Find(string.Format("Player {0}/Player", i + 1));

                        if (modifier.Result == null)
                            modifier.Result = player.position;

                        if (player.position != (Vector3)modifier.Result)
                        {
                            modifier.Result = player.position;
                            return true;
                        }
                    }
                }

                return false;
            },
            "playerBoosting" => modifier =>
            {
                if (modifier.reference != null && Updater.TryGetObject(modifier.reference, out LevelObject levelObject) && levelObject.visualObject.gameObject)
                {
                    var orderedList = PlayerManager.Players
                        .Where(x => x.Player && x.Player.rb)
                        .OrderBy(x => Vector2.Distance(x.Player.rb.position, levelObject.visualObject.gameObject.transform.position)).ToList();

                    if (!orderedList.IsEmpty())
                    {
                        var closest = orderedList[0];

                        return closest.Player.isBoosting;
                    }
                }

                return false;
            },
            "playerAlive" => modifier =>
            {
                if (int.TryParse(modifier.value, out int index) && modifier.reference != null && Updater.TryGetObject(modifier.reference, out LevelObject levelObject) && levelObject.visualObject.gameObject)
                {
                    if (PlayerManager.Players.Count > index)
                    {
                        var closest = PlayerManager.Players[index];

                        return closest.Player && closest.Player.Alive;
                    }
                }

                return false;
            },
            "playerDeathsEquals" => modifier =>
            {
                return GameManager.inst.deaths.Count == modifier.GetInt(0, 0);
            },
            "playerDeathsLesserEquals" => modifier =>
            {
                return GameManager.inst.deaths.Count <= modifier.GetInt(0, 0);
            },
            "playerDeathsGreaterEquals" => modifier =>
            {
                return GameManager.inst.deaths.Count >= modifier.GetInt(0, 0);
            },
            "playerDeathsLesser" => modifier =>
            {
                return GameManager.inst.deaths.Count < modifier.GetInt(0, 0);
            },
            "playerDeathsGreater" => modifier =>
            {
                return GameManager.inst.deaths.Count > modifier.GetInt(0, 0);
            },
            "playerDistanceGreater" => modifier =>
            {
                float num = modifier.GetFloat(0, 0f);
                for (int i = 0; i < GameManager.inst.players.transform.childCount; i++)
                {
                    if (GameManager.inst.players.transform.Find(string.Format("Player {0}", i + 1)) && modifier.reference != null && Updater.TryGetObject(modifier.reference, out LevelObject levelObject) && levelObject.visualObject.gameObject)
                    {
                        var player = GameManager.inst.players.transform.Find(string.Format("Player {0}/Player", i + 1));
                        if (Vector2.Distance(player.transform.position, levelObject.visualObject.gameObject.transform.position) > num)
                            return true;
                    }
                }

                return false;
            },
            "playerDistanceLesser" => modifier =>
            {
                float num = modifier.GetFloat(0, 0f);
                for (int i = 0; i < GameManager.inst.players.transform.childCount; i++)
                {
                    if (GameManager.inst.players.transform.Find(string.Format("Player {0}", i + 1)) && modifier.reference != null && Updater.TryGetObject(modifier.reference, out LevelObject levelObject) && levelObject.visualObject.gameObject)
                    {
                        var player = GameManager.inst.players.transform.Find(string.Format("Player {0}/Player", i + 1));
                        if (Vector2.Distance(player.transform.position, levelObject.visualObject.gameObject.transform.position) < num)
                            return true;
                    }
                }

                return false;
            },
            "playerCountEquals" => modifier =>
            {
                return InputDataManager.inst.players.Count == modifier.GetInt(0, 0);
            },
            "playerCountLesserEquals" => modifier =>
            {
                return InputDataManager.inst.players.Count <= modifier.GetInt(0, 0);
            },
            "playerCountGreaterEquals" => modifier =>
            {
                return InputDataManager.inst.players.Count >= modifier.GetInt(0, 0);
            },
            "playerCountLesser" => modifier =>
            {
                return InputDataManager.inst.players.Count < modifier.GetInt(0, 0);
            },
            "playerCountGreater" => modifier =>
            {
                return InputDataManager.inst.players.Count > modifier.GetInt(0, 0);
            },
            "onPlayerHit" => modifier =>
            {
                var hasResult = modifier.HasResult();

                if (!hasResult || modifier.Result is int count && count != GameManager.inst.hits.Count)
                {
                    modifier.Result = GameManager.inst.hits.Count;
                    if (hasResult)
                        return true;
                }

                return false;
            },
            "onPlayerDeath" => modifier =>
            {
                var hasResult = modifier.HasResult();

                if (!hasResult || modifier.Result is int count && count != GameManager.inst.deaths.Count)
                {
                    modifier.Result = GameManager.inst.deaths.Count;
                    if (hasResult)
                        return true;
                }

                return false;
            },
            "playerBoostEquals" => modifier =>
            {
                return LevelManager.BoostCount == modifier.GetInt(0, 0);
            },
            "playerBoostLesserEquals" => modifier =>
            {
                return LevelManager.BoostCount <= modifier.GetInt(0, 0);
            },
            "playerBoostGreaterEquals" => modifier =>
            {
                return LevelManager.BoostCount >= modifier.GetInt(0, 0);
            },
            "playerBoostLesser" => modifier =>
            {
                return LevelManager.BoostCount < modifier.GetInt(0, 0);
            },
            "playerBoostGreater" => modifier =>
            {
                return LevelManager.BoostCount > modifier.GetInt(0, 0);
            },

            #endregion

            #region Controls

            "keyPressDown" => modifier =>
            {
                return Input.GetKeyDown((KeyCode)modifier.GetInt(0, 0));
            },
            "keyPress" => modifier =>
            {
                return Input.GetKey((KeyCode)modifier.GetInt(0, 0));
            },
            "keyPressUp" => modifier =>
            {
                return Input.GetKeyUp((KeyCode)modifier.GetInt(0, 0));
            },
            "mouseButtonDown" => modifier =>
            {
                return Input.GetMouseButtonDown(modifier.GetInt(0, 0));
            },
            "mouseButton" => modifier =>
            {
                return Input.GetMouseButton(modifier.GetInt(0, 0));
            },
            "mouseButtonUp" => modifier =>
            {
                return Input.GetMouseButtonUp(modifier.GetInt(0, 0));
            },
            "mouseOver" => modifier =>
            {
                if (modifier.reference.levelObject && modifier.reference.levelObject.visualObject != null && modifier.reference.levelObject.visualObject.gameObject)
                {
                    if (!modifier.reference.detector)
                    {
                        var gameObject = modifier.reference.levelObject.visualObject.gameObject;
                        var op = gameObject.GetOrAddComponent<Detector>();
                        op.beatmapObject = modifier.reference;
                        modifier.reference.detector = op;
                    }

                    if (modifier.reference.detector)
                        return modifier.reference.detector.hovered;
                }

                return false;
            },
            "mouseOverSignalModifier" => modifier =>
            {
                var list = !modifier.prefabInstanceOnly ? GameData.Current.FindObjectsWithTag(modifier.GetValue(1)) : GameData.Current.FindObjectsWithTag(modifier.reference, modifier.GetValue(1));
                if (modifier.reference.levelObject && modifier.reference.levelObject.visualObject != null && modifier.reference.levelObject.visualObject.gameObject)
                {
                    if (!modifier.reference.detector)
                    {
                        var gameObject = modifier.reference.levelObject.visualObject.gameObject;
                        var op = gameObject.GetOrAddComponent<Detector>();
                        op.beatmapObject = modifier.reference;
                        modifier.reference.detector = op;
                    }

                    if (modifier.reference.detector)
                    {
                        if (modifier.reference.detector.hovered && !list.IsEmpty())
                        {
                            foreach (var bm in list)
                                CoroutineHelper.StartCoroutine(ActivateModifier(bm, modifier.GetFloat(0, 0f)));
                        }

                        if (modifier.reference.detector.hovered)
                            return true;
                    }
                }

                return false;
            },
            "controlPressDown" => modifier =>
            {
                var type = modifier.GetInt(0, 0);

                if (!Updater.TryGetObject(modifier.reference, out LevelObject levelObject) || !levelObject.visualObject.gameObject)
                    return false;

                var player = PlayerManager.GetClosestPlayer(levelObject.visualObject.gameObject.transform.position);

                if (!player || player.device == null && !CoreHelper.InEditor || InControl.InputManager.ActiveDevice == null)
                    return false;

                var device = player.device ?? InControl.InputManager.ActiveDevice;

                return Enum.TryParse(((PlayerInputControlType)type).ToString(), out InControl.InputControlType inputControlType) && device.GetControl(inputControlType).WasPressed;
            },
            "controlPress" => modifier =>
            {
                var type = modifier.GetInt(0, 0);

                if (!Updater.TryGetObject(modifier.reference, out LevelObject levelObject) || !levelObject.visualObject.gameObject)
                    return false;

                var player = PlayerManager.GetClosestPlayer(levelObject.visualObject.gameObject.transform.position);

                if (!player || player.device == null && !CoreHelper.InEditor || InControl.InputManager.ActiveDevice == null)
                    return false;

                var device = player.device ?? InControl.InputManager.ActiveDevice;

                return Enum.TryParse(((PlayerInputControlType)type).ToString(), out InControl.InputControlType inputControlType) && device.GetControl(inputControlType).IsPressed;
            },
            "controlPressUp" => modifier =>
            {
                var type = modifier.GetInt(0, 0);

                if (!Updater.TryGetObject(modifier.reference, out LevelObject levelObject) || !levelObject.visualObject.gameObject)
                    return false;

                var player = PlayerManager.GetClosestPlayer(levelObject.visualObject.gameObject.transform.position);

                if (!player || player.device == null && !CoreHelper.InEditor || InControl.InputManager.ActiveDevice == null)
                    return false;

                var device = player.device ?? InControl.InputManager.ActiveDevice;

                return Enum.TryParse(((PlayerInputControlType)type).ToString(), out InControl.InputControlType inputControlType) && device.GetControl(inputControlType).WasReleased;
            },

            #endregion

            #region Collide

            "bulletCollide" => modifier =>
            {
                if (Updater.TryGetObject(modifier.reference, out LevelObject levelObject) && levelObject.visualObject != null && levelObject.visualObject.gameObject)
                {
                    if (!modifier.reference.detector)
                    {
                        var op = levelObject.visualObject.gameObject.GetOrAddComponent<Detector>();
                        op.beatmapObject = modifier.reference;
                        modifier.reference.detector = op;
                    }

                    if (modifier.reference.detector)
                        return modifier.reference.detector.bulletOver;
                }

                return false;
            },
            "objectCollide" => modifier =>
            {
                if (Updater.TryGetObject(modifier.reference, out LevelObject levelObject) && levelObject.visualObject != null && levelObject.visualObject.collider)
                {
                    var list = (!modifier.prefabInstanceOnly ? GameData.Current.FindObjectsWithTag(modifier.value) : GameData.Current.FindObjectsWithTag(modifier.reference, modifier.value)).FindAll(x => Updater.TryGetObject(x, out LevelObject levelObject1) && levelObject1.visualObject != null && levelObject1.visualObject.collider);
                    return !list.IsEmpty() && list.Any(x => x.levelObject.visualObject.collider.IsTouching(levelObject.visualObject.collider));
                }

                return false;
            },

            #endregion

            #region JSON

            "loadEquals" => modifier =>
            {
                if (RTFile.TryReadFromFile(GetSaveFile(modifier.GetValue(1)), out string json) &&
                    int.TryParse(modifier.GetValue(4), out int type) && float.TryParse(modifier.GetValue(0), out float num))
                {
                    var jn = JSON.Parse(json);

                    return
                        !string.IsNullOrEmpty(jn[modifier.GetValue(2)][modifier.GetValue(3)]["float"]) &&
                            (type == 0 ?
                                float.TryParse(jn[modifier.GetValue(2)][modifier.GetValue(3)]["float"], out float eq) && eq == num :
                                jn[modifier.GetValue(2)][modifier.GetValue(3)]["string"] == modifier.GetValue(0));
                }

                return false;
            },
            "loadLesserEquals" => modifier =>
            {
                if (RTFile.TryReadFromFile(GetSaveFile(modifier.GetValue(1)), out string json))
                {
                    var jn = JSON.Parse(json);

                    var fjn = jn[modifier.GetValue(2)][modifier.GetValue(3)]["float"];

                    return !string.IsNullOrEmpty(fjn) && fjn.AsFloat <= modifier.GetFloat(0, 0f);
                }

                return false;
            },
            "loadGreaterEquals" => modifier =>
            {
                if (RTFile.TryReadFromFile(GetSaveFile(modifier.GetValue(1)), out string json))
                {
                    var jn = JSON.Parse(json);

                    var fjn = jn[modifier.GetValue(2)][modifier.GetValue(3)]["float"];

                    return !string.IsNullOrEmpty(fjn) && fjn.AsFloat >= modifier.GetFloat(0, 0f);
                }

                return false;
            },
            "loadLesser" => modifier =>
            {
                if (RTFile.TryReadFromFile(GetSaveFile(modifier.GetValue(1)), out string json))
                {
                    var jn = JSON.Parse(json);

                    var fjn = jn[modifier.GetValue(2)][modifier.GetValue(3)]["float"];

                    return !string.IsNullOrEmpty(fjn) && fjn.AsFloat < modifier.GetFloat(0, 0f);
                }

                return false;
            },
            "loadGreater" => modifier =>
            {
                if (RTFile.TryReadFromFile(GetSaveFile(modifier.GetValue(1)), out string json))
                {
                    var jn = JSON.Parse(json);

                    var fjn = jn[modifier.GetValue(2)][modifier.GetValue(3)]["float"];

                    return !string.IsNullOrEmpty(fjn) && fjn.AsFloat > modifier.GetFloat(0, 0f);
                }

                return false;
            },
            "loadExists" => modifier =>
            {
                return RTFile.TryReadFromFile(GetSaveFile(modifier.GetValue(1)), out string json) && !string.IsNullOrEmpty(JSON.Parse(json)[modifier.GetValue(2)][modifier.GetValue(3)]);
            },

            #endregion

            #region Variable

            // self
            "variableEquals" => modifier =>
            {
                return modifier.reference && modifier.reference.integerVariable == modifier.GetInt(0, 0);
            },
            "variableLesserEquals" => modifier =>
            {
                return modifier.reference && modifier.reference.integerVariable <= modifier.GetInt(0, 0);
            },
            "variableGreaterEquals" => modifier =>
            {
                return modifier.reference && modifier.reference.integerVariable >= modifier.GetInt(0, 0);
            },
            "variableLesser" => modifier =>
            {
                return modifier.reference && modifier.reference.integerVariable < modifier.GetInt(0, 0);
            },
            "variableGreater" => modifier =>
            {
                return modifier.reference && modifier.reference.integerVariable > modifier.GetInt(0, 0);
            },

            // other
            "variableOtherEquals" => modifier =>
            {
                var beatmapObjects = !modifier.prefabInstanceOnly ? GameData.Current.FindObjectsWithTag(modifier.GetValue(1)) : GameData.Current.FindObjectsWithTag(modifier.reference, modifier.GetValue(1));

                int num = modifier.GetInt(0, 0);

                return !beatmapObjects.IsEmpty() && beatmapObjects.Any(x => x.integerVariable == num);
            },
            "variableOtherLesserEquals" => modifier =>
            {
                var beatmapObjects = !modifier.prefabInstanceOnly ? GameData.Current.FindObjectsWithTag(modifier.GetValue(1)) : GameData.Current.FindObjectsWithTag(modifier.reference, modifier.GetValue(1));

                int num = modifier.GetInt(0, 0);

                return !beatmapObjects.IsEmpty() && beatmapObjects.Any(x => x.integerVariable <= num);
            },
            "variableOtherGreaterEquals" => modifier =>
            {
                var beatmapObjects = !modifier.prefabInstanceOnly ? GameData.Current.FindObjectsWithTag(modifier.GetValue(1)) : GameData.Current.FindObjectsWithTag(modifier.reference, modifier.GetValue(1));

                int num = modifier.GetInt(0, 0);

                return !beatmapObjects.IsEmpty() && beatmapObjects.Any(x => x.integerVariable >= num);
            },
            "variableOtherLesser" => modifier =>
            {
                var beatmapObjects = !modifier.prefabInstanceOnly ? GameData.Current.FindObjectsWithTag(modifier.GetValue(1)) : GameData.Current.FindObjectsWithTag(modifier.reference, modifier.GetValue(1));

                int num = modifier.GetInt(0, 0);

                return !beatmapObjects.IsEmpty() && beatmapObjects.Any(x => x.integerVariable < num);
            },
            "variableOtherGreater" => modifier =>
            {
                var beatmapObjects = !modifier.prefabInstanceOnly ? GameData.Current.FindObjectsWithTag(modifier.GetValue(1)) : GameData.Current.FindObjectsWithTag(modifier.reference, modifier.GetValue(1));

                int num = modifier.GetInt(0, 0);

                return !beatmapObjects.IsEmpty() && beatmapObjects.Any(x => x.integerVariable > num);
            },

            #endregion

            #region Audio

            "pitchEquals" => modifier =>
            {
                return AudioManager.inst.pitch == modifier.GetFloat(0, 0f);
            },
            "pitchLesserEquals" => modifier =>
            {
                return AudioManager.inst.pitch <= modifier.GetFloat(0, 0f);
            },
            "pitchGreaterEquals" => modifier =>
            {
                return AudioManager.inst.pitch >= modifier.GetFloat(0, 0f);
            },
            "pitchLesser" => modifier =>
            {
                return AudioManager.inst.pitch < modifier.GetFloat(0, 0f);
            },
            "pitchGreater" => modifier =>
            {
                return AudioManager.inst.pitch > modifier.GetFloat(0, 0f);
            },
            "musicTimeGreater" => modifier =>
            {
                return AudioManager.inst.CurrentAudioSource.time - (modifier.GetBool(1, false) ? modifier.reference.StartTime : 0f) > modifier.GetFloat(0, 0f);
            },
            "musicTimeLesser" => modifier =>
            {
                return AudioManager.inst.CurrentAudioSource.time - (modifier.GetBool(1, false) ? modifier.reference.StartTime : 0f) < modifier.GetFloat(0, 0f);
            },
            "musicPlaying" => modifier =>
            {
                return AudioManager.inst.CurrentAudioSource.isPlaying;
            },

            #endregion

            #region Challenge Mode

            "inZenMode" => modifier => PlayerManager.Invincible,
            "inNormal" => modifier => PlayerManager.IsNormal,
            "in1Life" => modifier => PlayerManager.Is1Life,
            "inNoHit" => modifier => PlayerManager.IsNoHit,
            "inPractice" => modifier => PlayerManager.IsPractice,

            #endregion

            #region Random

            "randomEquals" => modifier =>
            {
                if (!modifier.HasResult())
                    modifier.Result = UnityEngine.Random.Range(modifier.GetInt(1, 0), modifier.GetInt(2, 0)) == modifier.GetInt(0, 0);

                return modifier.HasResult() && modifier.GetResult<bool>();
            },
            "randomLesser" => modifier =>
            {
                if (!modifier.HasResult())
                    modifier.Result = UnityEngine.Random.Range(modifier.GetInt(1, 0), modifier.GetInt(2, 0)) < modifier.GetInt(0, 0);

                return modifier.HasResult() && modifier.GetResult<bool>();
            },
            "randomGreater" => modifier =>
            {
                if (!modifier.HasResult())
                    modifier.Result = UnityEngine.Random.Range(modifier.GetInt(1, 0), modifier.GetInt(2, 0)) > modifier.GetInt(0, 0);

                return modifier.HasResult() && modifier.GetResult<bool>();
            },

            #endregion

            #region Math

            "mathEquals" => modifier =>
            {
                var variables = modifier.reference.GetObjectVariables();
                return RTMath.Parse(modifier.GetValue(0), variables) == RTMath.Parse(modifier.GetValue(1), variables);
            },
            "mathLesserEquals" => modifier =>
            {
                var variables = modifier.reference.GetObjectVariables();
                return RTMath.Parse(modifier.GetValue(0), variables) <= RTMath.Parse(modifier.GetValue(1), variables);
            },
            "mathGreaterEquals" => modifier =>
            {
                var variables = modifier.reference.GetObjectVariables();
                return RTMath.Parse(modifier.GetValue(0), variables) >= RTMath.Parse(modifier.GetValue(1), variables);
            },
            "mathLesser" => modifier =>
            {
                var variables = modifier.reference.GetObjectVariables();
                return RTMath.Parse(modifier.GetValue(0), variables) < RTMath.Parse(modifier.GetValue(1), variables);
            },
            "mathGreater" => modifier =>
            {
                var variables = modifier.reference.GetObjectVariables();
                return RTMath.Parse(modifier.GetValue(0), variables) > RTMath.Parse(modifier.GetValue(1), variables);
            },

            #endregion

            #region Axis

            "axisEquals" => modifier =>
            {
                int fromType = modifier.GetInt(1, 0);
                int fromAxis = modifier.GetInt(2, 0);

                float delay = modifier.GetFloat(3, 0f);
                float multiply = modifier.GetFloat(4, 0f);
                float offset = modifier.GetFloat(5, 0f);
                float min = modifier.GetFloat(6, -9999f);
                float max = modifier.GetFloat(7, 9999f);
                float equals = modifier.GetFloat(8, 0f);
                bool visual = modifier.GetBool(9, false);
                float loop = modifier.GetFloat(10, 9999f);

                if (GameData.Current.TryFindObjectWithTag(modifier, modifier.GetValue(0), out BeatmapObject bm))
                {
                    var time = Updater.CurrentTime;

                    fromType = Mathf.Clamp(fromType, 0, bm.events.Count);
                    fromAxis = Mathf.Clamp(fromAxis, 0, bm.events[fromType][0].values.Length);

                    if (fromType >= 0 && fromType <= 2)
                        return GetAnimation(bm, fromType, fromAxis, min, max, offset, multiply, delay, loop, visual) == equals;
                }

                return false;
            },
            "axisLesserEquals" => modifier =>
            {
                int fromType = modifier.GetInt(1, 0);
                int fromAxis = modifier.GetInt(2, 0);

                float delay = modifier.GetFloat(3, 0f);
                float multiply = modifier.GetFloat(4, 0f);
                float offset = modifier.GetFloat(5, 0f);
                float min = modifier.GetFloat(6, -9999f);
                float max = modifier.GetFloat(7, 9999f);
                float equals = modifier.GetFloat(8, 0f);
                bool visual = modifier.GetBool(9, false);
                float loop = modifier.GetFloat(10, 9999f);

                if (GameData.Current.TryFindObjectWithTag(modifier, modifier.GetValue(0), out BeatmapObject bm))
                {
                    var time = Updater.CurrentTime;

                    fromType = Mathf.Clamp(fromType, 0, bm.events.Count);
                    fromAxis = Mathf.Clamp(fromAxis, 0, bm.events[fromType][0].values.Length);

                    if (fromType >= 0 && fromType <= 2)
                        return GetAnimation(bm, fromType, fromAxis, min, max, offset, multiply, delay, loop, visual) <= equals;
                }

                return false;
            },
            "axisGreaterEquals" => modifier =>
            {
                int fromType = modifier.GetInt(1, 0);
                int fromAxis = modifier.GetInt(2, 0);

                float delay = modifier.GetFloat(3, 0f);
                float multiply = modifier.GetFloat(4, 0f);
                float offset = modifier.GetFloat(5, 0f);
                float min = modifier.GetFloat(6, -9999f);
                float max = modifier.GetFloat(7, 9999f);
                float equals = modifier.GetFloat(8, 0f);
                bool visual = modifier.GetBool(9, false);
                float loop = modifier.GetFloat(10, 9999f);

                if (GameData.Current.TryFindObjectWithTag(modifier, modifier.GetValue(0), out BeatmapObject bm))
                {
                    var time = Updater.CurrentTime;

                    fromType = Mathf.Clamp(fromType, 0, bm.events.Count);
                    fromAxis = Mathf.Clamp(fromAxis, 0, bm.events[fromType][0].values.Length);

                    if (fromType >= 0 && fromType <= 2)
                        return GetAnimation(bm, fromType, fromAxis, min, max, offset, multiply, delay, loop, visual) >= equals;
                }

                return false;
            },
            "axisLesser" => modifier =>
            {
                int fromType = modifier.GetInt(1, 0);
                int fromAxis = modifier.GetInt(2, 0);

                float delay = modifier.GetFloat(3, 0f);
                float multiply = modifier.GetFloat(4, 0f);
                float offset = modifier.GetFloat(5, 0f);
                float min = modifier.GetFloat(6, -9999f);
                float max = modifier.GetFloat(7, 9999f);
                float equals = modifier.GetFloat(8, 0f);
                bool visual = modifier.GetBool(9, false);
                float loop = modifier.GetFloat(10, 9999f);

                if (GameData.Current.TryFindObjectWithTag(modifier, modifier.GetValue(0), out BeatmapObject bm))
                {
                    var time = Updater.CurrentTime;

                    fromType = Mathf.Clamp(fromType, 0, bm.events.Count);
                    fromAxis = Mathf.Clamp(fromAxis, 0, bm.events[fromType][0].values.Length);

                    if (fromType >= 0 && fromType <= 2)
                        return GetAnimation(bm, fromType, fromAxis, min, max, offset, multiply, delay, loop, visual) < equals;
                }

                return false;
            },
            "axisGreater" => modifier =>
            {
                int fromType = modifier.GetInt(1, 0);
                int fromAxis = modifier.GetInt(2, 0);

                float delay = modifier.GetFloat(3, 0f);
                float multiply = modifier.GetFloat(4, 0f);
                float offset = modifier.GetFloat(5, 0f);
                float min = modifier.GetFloat(6, -9999f);
                float max = modifier.GetFloat(7, 9999f);
                float equals = modifier.GetFloat(8, 0f);
                bool visual = modifier.GetBool(9, false);
                float loop = modifier.GetFloat(10, 9999f);

                if (GameData.Current.TryFindObjectWithTag(modifier, modifier.GetValue(0), out BeatmapObject bm))
                {
                    var time = Updater.CurrentTime;

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
            "levelRankEquals" => modifier =>
            {
                return GetLevelRank(LevelManager.CurrentLevel, out int levelRankIndex) && levelRankIndex == modifier.GetInt(0, 0);
            },
            "levelRankLesserEquals" => modifier =>
            {
                return GetLevelRank(LevelManager.CurrentLevel, out int levelRankIndex) && levelRankIndex <= modifier.GetInt(0, 0);
            },
            "levelRankGreaterEquals" => modifier =>
            {
                return GetLevelRank(LevelManager.CurrentLevel, out int levelRankIndex) && levelRankIndex >= modifier.GetInt(0, 0);
            },
            "levelRankLesser" => modifier =>
            {
                return GetLevelRank(LevelManager.CurrentLevel, out int levelRankIndex) && levelRankIndex < modifier.GetInt(0, 0);
            },
            "levelRankGreater" => modifier =>
            {
                return GetLevelRank(LevelManager.CurrentLevel, out int levelRankIndex) && levelRankIndex > modifier.GetInt(0, 0);
            },

            // other
            "levelRankOtherEquals" => modifier =>
            {
                return LevelManager.Levels.TryFind(x => x.id == modifier.GetValue(1), out Level level) && GetLevelRank(level, out int levelRankIndex) && levelRankIndex == modifier.GetInt(0, 0);
            },
            "levelRankOtherLesserEquals" => modifier =>
            {
                return LevelManager.Levels.TryFind(x => x.id == modifier.GetValue(1), out Level level) && GetLevelRank(level, out int levelRankIndex) && levelRankIndex <= modifier.GetInt(0, 0);
            },
            "levelRankOtherGreaterEquals" => modifier =>
            {
                return LevelManager.Levels.TryFind(x => x.id == modifier.GetValue(1), out Level level) && GetLevelRank(level, out int levelRankIndex) && levelRankIndex >= modifier.GetInt(0, 0);
            },
            "levelRankOtherLesser" => modifier =>
            {
                return LevelManager.Levels.TryFind(x => x.id == modifier.GetValue(1), out Level level) && GetLevelRank(level, out int levelRankIndex) && levelRankIndex < modifier.GetInt(0, 0);
            },
            "levelRankOtherGreater" => modifier =>
            {
                return LevelManager.Levels.TryFind(x => x.id == modifier.GetValue(1), out Level level) && GetLevelRank(level, out int levelRankIndex) && levelRankIndex > modifier.GetInt(0, 0);
            },

            // current
            "levelRankCurrentEquals" => modifier =>
            {
                return LevelManager.levelRankIndexes[LevelManager.GetLevelRank(GameManager.inst.hits).name] == modifier.GetInt(0, 0);
            },
            "levelRankCurrentLesserEquals" => modifier =>
            {
                return LevelManager.levelRankIndexes[LevelManager.GetLevelRank(GameManager.inst.hits).name] <= modifier.GetInt(0, 0);
            },
            "levelRankCurrentGreaterEquals" => modifier =>
            {
                return LevelManager.levelRankIndexes[LevelManager.GetLevelRank(GameManager.inst.hits).name] >= modifier.GetInt(0, 0);
            },
            "levelRankCurrentLesser" => modifier =>
            {
                return LevelManager.levelRankIndexes[LevelManager.GetLevelRank(GameManager.inst.hits).name] < modifier.GetInt(0, 0);
            },
            "levelRankCurrentGreater" => modifier =>
            {
                return LevelManager.levelRankIndexes[LevelManager.GetLevelRank(GameManager.inst.hits).name] > modifier.GetInt(0, 0);
            },

            #endregion

            #region Level

            "levelUnlocked" => modifier =>
            {
                return LevelManager.Levels.TryFind(x => x.id == modifier.GetValue(0), out Level level) && !level.Locked;
            },
            "levelCompleted" => modifier =>
            {
                return CoreHelper.InEditor || LevelManager.CurrentLevel != null && LevelManager.CurrentLevel.playerData != null && LevelManager.CurrentLevel.playerData.Completed;
            },
            "levelCompletedOther" => modifier =>
            {
                return CoreHelper.InEditor || LevelManager.Levels.TryFind(x => x.id == modifier.GetValue(0), out Level level) && level.playerData != null && level.playerData.Completed;
            },
            "levelExists" => modifier =>
            {
                return LevelManager.Levels.Has(x => x.id == modifier.GetValue(0));
            },
            "levelPathExists" => modifier =>
            {
                var basePath = RTFile.CombinePaths(RTFile.ApplicationDirectory, LevelManager.ListSlash, modifier.GetValue(0));

                return
                    RTFile.FileExists(RTFile.CombinePaths(basePath, Level.LEVEL_LSB)) ||
                    RTFile.FileExists(RTFile.CombinePaths(basePath, Level.LEVEL_VGD)) ||
                    RTFile.FileExists(basePath + FileFormat.ASSET.Dot());
            },

            #endregion

            #region Real Time

            // seconds
            "realTimeSecondEquals" => modifier =>
            {
                return Parser.TryParse(DateTime.Now.ToString("ss"), 0) == modifier.GetInt(0, 0);
            },
            "realTimeSecondLesserEquals" => modifier =>
            {
                return Parser.TryParse(DateTime.Now.ToString("ss"), 0) <= modifier.GetInt(0, 0);
            },
            "realTimeSecondGreaterEquals" => modifier =>
            {
                return Parser.TryParse(DateTime.Now.ToString("ss"), 0) >= modifier.GetInt(0, 0);
            },
            "realTimeSecondLesser" => modifier =>
            {
                return Parser.TryParse(DateTime.Now.ToString("ss"), 0) < modifier.GetInt(0, 0);
            },
            "realTimeSecondGreater" => modifier =>
            {
                return Parser.TryParse(DateTime.Now.ToString("ss"), 0) > modifier.GetInt(0, 0);
            },

            // minutes
            "realTimeMinuteEquals" => modifier =>
            {
                return Parser.TryParse(DateTime.Now.ToString("mm"), 0) == modifier.GetInt(0, 0);
            },
            "realTimeMinuteLesserEquals" => modifier =>
            {
                return Parser.TryParse(DateTime.Now.ToString("mm"), 0) <= modifier.GetInt(0, 0);
            },
            "realTimeMinuteGreaterEquals" => modifier =>
            {
                return Parser.TryParse(DateTime.Now.ToString("mm"), 0) >= modifier.GetInt(0, 0);
            },
            "realTimeMinuteLesser" => modifier =>
            {
                return Parser.TryParse(DateTime.Now.ToString("mm"), 0) < modifier.GetInt(0, 0);
            },
            "realTimeMinuteGreater" => modifier =>
            {
                return Parser.TryParse(DateTime.Now.ToString("mm"), 0) > modifier.GetInt(0, 0);
            },

            // 24 hours
            "realTime24HourEquals" => modifier =>
            {
                return Parser.TryParse(DateTime.Now.ToString("HH"), 0) == modifier.GetInt(0, 0);
            },
            "realTime24HourLesserEquals" => modifier =>
            {
                return Parser.TryParse(DateTime.Now.ToString("HH"), 0) <= modifier.GetInt(0, 0);
            },
            "realTime24HourGreaterEquals" => modifier =>
            {
                return Parser.TryParse(DateTime.Now.ToString("HH"), 0) >= modifier.GetInt(0, 0);
            },
            "realTime24HourLesser" => modifier =>
            {
                return Parser.TryParse(DateTime.Now.ToString("HH"), 0) < modifier.GetInt(0, 0);
            },
            "realTime24HourGreater" => modifier =>
            {
                return Parser.TryParse(DateTime.Now.ToString("HH"), 0) > modifier.GetInt(0, 0);
            },

            // 12 hours
            "realTime12HourEquals" => modifier =>
            {
                return Parser.TryParse(DateTime.Now.ToString("hh"), 0) == modifier.GetInt(0, 0);
            },
            "realTime12HourLesserEquals" => modifier =>
            {
                return Parser.TryParse(DateTime.Now.ToString("hh"), 0) <= modifier.GetInt(0, 0);
            },
            "realTime12HourGreaterEquals" => modifier =>
            {
                return Parser.TryParse(DateTime.Now.ToString("hh"), 0) >= modifier.GetInt(0, 0);
            },
            "realTime12HourLesser" => modifier =>
            {
                return Parser.TryParse(DateTime.Now.ToString("hh"), 0) < modifier.GetInt(0, 0);
            },
            "realTime12HourGreater" => modifier =>
            {
                return Parser.TryParse(DateTime.Now.ToString("hh"), 0) > modifier.GetInt(0, 0);
            },

            // days
            "realTimeDayEquals" => modifier =>
            {
                return Parser.TryParse(DateTime.Now.ToString("dd"), 0) == modifier.GetInt(0, 0);
            },
            "realTimeDayLesserEquals" => modifier =>
            {
                return Parser.TryParse(DateTime.Now.ToString("dd"), 0) <= modifier.GetInt(0, 0);
            },
            "realTimeDayGreaterEquals" => modifier =>
            {
                return Parser.TryParse(DateTime.Now.ToString("dd"), 0) >= modifier.GetInt(0, 0);
            },
            "realTimeDayLesser" => modifier =>
            {
                return Parser.TryParse(DateTime.Now.ToString("dd"), 0) < modifier.GetInt(0, 0);
            },
            "realTimeDayGreater" => modifier =>
            {
                return Parser.TryParse(DateTime.Now.ToString("dd"), 0) > modifier.GetInt(0, 0);
            },

            // months
            "realTimeMonthEquals" => modifier =>
            {
                return Parser.TryParse(DateTime.Now.ToString("MM"), 0) == modifier.GetInt(0, 0);
            },
            "realTimeMonthLesserEquals" => modifier =>
            {
                return Parser.TryParse(DateTime.Now.ToString("MM"), 0) <= modifier.GetInt(0, 0);
            },
            "realTimeMonthGreaterEquals" => modifier =>
            {
                return Parser.TryParse(DateTime.Now.ToString("MM"), 0) >= modifier.GetInt(0, 0);
            },
            "realTimeMonthLesser" => modifier =>
            {
                return Parser.TryParse(DateTime.Now.ToString("MM"), 0) < modifier.GetInt(0, 0);
            },
            "realTimeMonthGreater" => modifier =>
            {
                return Parser.TryParse(DateTime.Now.ToString("MM"), 0) > modifier.GetInt(0, 0);
            },

            // years
            "realTimeYearEquals" => modifier =>
            {
                return Parser.TryParse(DateTime.Now.ToString("yyyy"), 0) == modifier.GetInt(0, 0);
            },
            "realTimeYearLesserEquals" => modifier =>
            {
                return Parser.TryParse(DateTime.Now.ToString("yyyy"), 0) <= modifier.GetInt(0, 0);
            },
            "realTimeYearGreaterEquals" => modifier =>
            {
                return Parser.TryParse(DateTime.Now.ToString("yyyy"), 0) >= modifier.GetInt(0, 0);
            },
            "realTimeYearLesser" => modifier =>
            {
                return Parser.TryParse(DateTime.Now.ToString("yyyy"), 0) < modifier.GetInt(0, 0);
            },
            "realTimeYearGreater" => modifier =>
            {
                return Parser.TryParse(DateTime.Now.ToString("yyyy"), 0) > modifier.GetInt(0, 0);
            },

            #endregion

            #region Config

            // main
            "usernameEquals" => modifier =>
            {
                return CoreConfig.Instance.DisplayName.Value == modifier.GetValue(0);
            },
            "languageEquals" => modifier =>
            {
                return CoreConfig.Instance.Language.Value == (Language)modifier.GetInt(0, 0);
            },

            // misc
            "configLDM" => modifier =>
            {
                return CoreConfig.Instance.LDM.Value;
            },
            "configShowEffects" => modifier =>
            {
                return EventsConfig.Instance.ShowFX.Value;
            },
            "configShowPlayerGUI" => modifier =>
            {
                return EventsConfig.Instance.ShowGUI.Value;
            },
            "configShowIntro" => modifier =>
            {
                return EventsConfig.Instance.ShowIntro.Value;
            },

            #endregion

            #region Misc

            "inEditor" => modifier => CoreHelper.InEditor,
            "requireSignal" => modifier => modifier.HasResult(),
            "isFullscreen" => modifier => Screen.fullScreen,
            "objectAlive" => modifier =>
            {
                var list = !modifier.prefabInstanceOnly ? GameData.Current.FindObjectsWithTag(modifier.value) : GameData.Current.FindObjectsWithTag(modifier.reference, modifier.value);
                for (int i = 0; i < list.Count; i++)
                {
                    if (list[i].Alive)
                        return true;
                }
                return false;
            },
            "objectSpawned" => modifier =>
            {
                if (!modifier.HasResult())
                    modifier.Result = new List<string>();

                var ids = modifier.GetResult<List<string>>();

                var list = !modifier.prefabInstanceOnly ? GameData.Current.FindObjectsWithTag(modifier.GetValue(0)) : GameData.Current.FindObjectsWithTag(modifier.reference, modifier.GetValue(0));
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

            "storyLoadIntEqualsDEVONLY" => modifier =>
            {
                return Story.StoryManager.inst.LoadInt(modifier.GetValue(0), modifier.GetInt(1, 0)) == modifier.GetInt(2, 0);
            },
            "storyLoadIntLesserEqualsDEVONLY" => modifier =>
            {
                return Story.StoryManager.inst.LoadInt(modifier.GetValue(0), modifier.GetInt(1, 0)) <= modifier.GetInt(2, 0);
            },
            "storyLoadIntGreaterEqualsDEVONLY" => modifier =>
            {
                return Story.StoryManager.inst.LoadInt(modifier.GetValue(0), modifier.GetInt(1, 0)) >= modifier.GetInt(2, 0);
            },
            "storyLoadIntLesserDEVONLY" => modifier =>
            {
                return Story.StoryManager.inst.LoadInt(modifier.GetValue(0), modifier.GetInt(1, 0)) < modifier.GetInt(2, 0);
            },
            "storyLoadIntGreaterDEVONLY" => modifier =>
            {
                return Story.StoryManager.inst.LoadInt(modifier.GetValue(0), modifier.GetInt(1, 0)) > modifier.GetInt(2, 0);
            },
            "storyLoadBoolDEVONLY" => modifier =>
            {
                return Story.StoryManager.inst.LoadBool(modifier.GetValue(0), modifier.GetBool(1, false));
            },

            #endregion

            _ => modifier => false,
        };

        public static Action<Modifier<BeatmapObject>> GetObjectAction(string key) => key switch
        {
            #region Audio

            // pitch
            "setPitch" => modifier =>
            {
                RTEventManager.inst.pitchOffset = modifier.GetFloat(0, 0f);
            },
            "addPitch" => modifier =>
            {
                RTEventManager.inst.pitchOffset += modifier.GetFloat(0, 0f);
            },
            "setPitchMath" => modifier =>
            {
                RTEventManager.inst.pitchOffset = RTMath.Parse(modifier.GetValue(0), modifier.reference.GetObjectVariables());
            },
            "addPitchMath" => modifier =>
            {
                RTEventManager.inst.pitchOffset += RTMath.Parse(modifier.GetValue(0), modifier.reference.GetObjectVariables());
            },

            // music time
            "setMusicTime" => modifier =>
            {
                AudioManager.inst.SetMusicTime(modifier.GetFloat(0, 0f));
            },
            "setMusicTimeMath" => modifier =>
            {
                AudioManager.inst.SetMusicTime(RTMath.Parse(modifier.GetValue(0), modifier.reference.GetObjectVariables()));
            },
            "setMusicTimeStartTime" => modifier =>
            {
                AudioManager.inst.SetMusicTime(modifier.reference.StartTime);
            },
            "setMusicTimeAutokill" => modifier =>
            {
                AudioManager.inst.SetMusicTime(modifier.reference.StartTime + modifier.reference.SpawnDuration);
            },

            // play sound
            "playSound" => modifier =>
            {
                if (bool.TryParse(modifier.GetValue(1), out bool global) && float.TryParse(modifier.GetValue(2), out float pitch) && float.TryParse(modifier.GetValue(3), out float vol) && bool.TryParse(modifier.GetValue(4), out bool loop))
                    GetSoundPath(modifier.reference.id, modifier.GetValue(0), global, pitch, vol, loop);
            },
            "playSoundOnline" => modifier =>
            {
                if (float.TryParse(modifier.GetValue(1), out float pitch) && float.TryParse(modifier.GetValue(2), out float vol) && bool.TryParse(modifier.GetValue(3), out bool loop) && !string.IsNullOrEmpty(modifier.GetValue(0)))
                    DownloadSoundAndPlay(modifier.reference.id, modifier.GetValue(0), pitch, vol, loop);
            },
            "playDefaultSound" => modifier =>
            {
                if (!float.TryParse(modifier.GetValue(1), out float pitch) || !float.TryParse(modifier.GetValue(2), out float vol) || !bool.TryParse(modifier.GetValue(3), out bool loop) || !AudioManager.inst.library.soundClips.TryGetValue(modifier.GetValue(0), out AudioClip[] audioClips))
                    return;

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

                if (!loop)
                    CoroutineHelper.StartCoroutine(AudioManager.inst.DestroyWithDelay(audioSource, clip.length / x));
                else if (!ModifiersManager.audioSources.ContainsKey(modifier.reference.id))
                    ModifiersManager.audioSources.Add(modifier.reference.id, audioSource);
            },
            "audioSource" => modifier =>
            {
                if (modifier.HasResult() || !Updater.TryGetObject(modifier.reference, out LevelObject levelObject) || levelObject.visualObject == null ||
                    !levelObject.visualObject.gameObject)
                    return;

                string fullPath =
                    !bool.TryParse(modifier.GetValue(1), out bool global) || !global ?
                    RTFile.CombinePaths(RTFile.BasePath, modifier.GetValue(0)) :
                    RTFile.CombinePaths(RTFile.ApplicationDirectory, ModifiersManager.SOUNDLIBRARY_PATH, modifier.GetValue(0));

                var audioDotFormats = RTFile.AudioDotFormats;
                for (int i = 0; i < audioDotFormats.Length; i++)
                {
                    var audioDotFormat = audioDotFormats[i];
                    if (!modifier.GetValue(0).EndsWith(audioDotFormat) && RTFile.FileExists(fullPath + audioDotFormat))
                        fullPath += audioDotFormat;
                }

                if (!RTFile.FileExists(fullPath))
                {
                    CoreHelper.LogError($"File does not exist {fullPath}");
                    return;
                }

                if (fullPath.EndsWith(FileFormat.MP3.Dot()))
                {
                    modifier.Result = levelObject.visualObject.gameObject.AddComponent<AudioModifier>();
                    ((AudioModifier)modifier.Result).Init(LSAudio.CreateAudioClipUsingMP3File(fullPath), modifier.reference, modifier);
                    return;
                }

                CoroutineHelper.StartCoroutine(LoadMusicFileRaw(fullPath, audioClip =>
                {
                    if (!audioClip)
                    {
                        CoreHelper.LogError($"Failed to load audio {fullPath}");
                        return;
                    }

                    audioClip.name = modifier.GetValue(0);

                    if (levelObject.visualObject == null || !levelObject.visualObject.gameObject)
                        return;

                    modifier.Result = levelObject.visualObject.gameObject.AddComponent<AudioModifier>();
                    ((AudioModifier)modifier.Result).Init(audioClip, modifier.reference, modifier);
                }));
            },
            "setMusicPlaying" => modifier =>
            {
                SoundManager.inst.SetPlaying(modifier.GetBool(0, false));
            },

            #endregion

            #region Level

            "loadLevel" => modifier =>
            {
                if (CoreHelper.IsEditing)
                {
                    if (!EditorConfig.Instance.ModifiersCanLoadLevels.Value)
                        return;

                    RTEditor.inst.ShowWarningPopup($"You are about to enter the level {modifier.GetValue(0)}, are you sure you want to continue? Any unsaved progress will be lost!", () =>
                    {
                        string str = RTFile.BasePath;
                        if (EditorConfig.Instance.ModifiersSavesBackup.Value)
                        {
                            GameData.Current.SaveData(str + "level-modifier-backup.lsb", () =>
                            {
                                EditorManager.inst.DisplayNotification($"Saved backup to {System.IO.Path.GetFileName(RTFile.RemoveEndSlash(str))}", 2f, EditorManager.NotificationType.Success);
                            });
                        }

                        CoroutineHelper.StartCoroutine(RTEditor.inst.LoadLevel(new Level(RTFile.CombinePaths(RTEditor.inst.BeatmapsPath, RTEditor.inst.EditorPath, modifier.GetValue(0)))));
                    }, RTEditor.inst.HideWarningPopup);

                    return;
                }

                if (CoreHelper.InEditor)
                    return;

                var levelPath = RTFile.CombinePaths(RTFile.ApplicationDirectory, LevelManager.ListSlash, $"{modifier.GetValue(0)}");
                if (RTFile.FileExists(RTFile.CombinePaths(levelPath, Level.LEVEL_LSB)) || RTFile.FileExists(RTFile.CombinePaths(levelPath, Level.LEVEL_VGD)) || RTFile.FileExists(levelPath + FileFormat.ASSET.Dot()))
                    LevelManager.Load(levelPath);
                else
                    SoundManager.inst.PlaySound(DefaultSounds.Block);
            },
            "loadLevelID" => modifier =>
            {
                var id = modifier.GetValue(0);
                if (string.IsNullOrEmpty(id) || id == "0" || id == "-1")
                    return;

                if (!CoreHelper.InEditor)
                {
                    if (LevelManager.Levels.TryFind(x => x.id == modifier.value, out Level level))
                        LevelManager.Play(level);
                    else
                        SoundManager.inst.PlaySound(DefaultSounds.Block);

                    return;
                }

                if (!CoreHelper.IsEditing)
                    return;

                if (RTEditor.inst.LevelPanels.TryFind(x => x.Level && x.Level.metadata is MetaData metaData && metaData.ID == modifier.value, out LevelPanel editorWrapper))
                {
                    if (!EditorConfig.Instance.ModifiersCanLoadLevels.Value)
                        return;

                    var path = System.IO.Path.GetFileName(editorWrapper.FolderPath);

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

                        CoroutineHelper.StartCoroutine(RTEditor.inst.LoadLevel(editorWrapper.Level));
                    }, RTEditor.inst.HideWarningPopup);
                }
                else
                    SoundManager.inst.PlaySound(DefaultSounds.Block);
            },
            "loadLevelInternal" => modifier =>
            {
                if (!CoreHelper.InEditor)
                {
                    var filePath = RTFile.CombinePaths(RTFile.BasePath, modifier.GetValue(0));
                    if (!CoreHelper.InEditor && (RTFile.FileExists(RTFile.CombinePaths(filePath, Level.LEVEL_LSB)) || RTFile.FileIsFormat(RTFile.CombinePaths(filePath, Level.LEVEL_VGD)) || RTFile.FileExists(filePath + FileFormat.ASSET.Dot())))
                        LevelManager.Load(filePath);
                    else
                        SoundManager.inst.PlaySound(DefaultSounds.Block);

                    return;
                }

                if (CoreHelper.IsEditing && RTFile.FileExists(RTFile.CombinePaths(RTFile.BasePath, EditorManager.inst.currentLoadedLevel, modifier.GetValue(0), Level.LEVEL_LSB)))
                {
                    if (!EditorConfig.Instance.ModifiersCanLoadLevels.Value)
                        return;

                    RTEditor.inst.ShowWarningPopup($"You are about to enter the level {RTFile.CombinePaths(EditorManager.inst.currentLoadedLevel, modifier.GetValue(0))}, are you sure you want to continue? Any unsaved progress will be lost!", () =>
                    {
                        string str = RTFile.BasePath;
                        if (EditorConfig.Instance.ModifiersSavesBackup.Value)
                        {
                            GameData.Current.SaveData(RTFile.CombinePaths(str, "level-modifier-backup.lsb"), () =>
                            {
                                EditorManager.inst.DisplayNotification($"Saved backup to {System.IO.Path.GetFileName(RTFile.RemoveEndSlash(str))}", 2f, EditorManager.NotificationType.Success);
                            });
                        }

                        CoroutineHelper.StartCoroutine(RTEditor.inst.LoadLevel(new Level(RTFile.CombinePaths(EditorManager.inst.currentLoadedLevel, modifier.GetValue(0)))));
                    }, RTEditor.inst.HideWarningPopup);
                }
            },
            "loadLevelPrevious" => modifier =>
            {
                if (CoreHelper.InEditor)
                    return;

                LevelManager.Play(LevelManager.PreviousLevel);
            },
            "loadLevelHub" => modifier =>
            {
                if (CoreHelper.InEditor)
                    return;

                LevelManager.Play(LevelManager.Hub);
            },
            "loadLevelInCollection" => modifier =>
            {
                if (!CoreHelper.InEditor && LevelManager.CurrentLevelCollection && LevelManager.CurrentLevelCollection.levels.TryFind(x => x.id == modifier.GetValue(0), out Level level))
                    LevelManager.Play(level);
            },
            "downloadLevel" => modifier =>
            {
                var levelInfo = new LevelInfo(modifier.GetValue(0), modifier.GetValue(0), modifier.GetValue(1), modifier.GetValue(2), modifier.GetValue(3), modifier.GetValue(4));

                LevelCollection.DownloadLevel(null, levelInfo, level =>
                {
                    if (modifier.GetBool(5, true))
                        LevelManager.Play(level);
                });
            },
            "endLevel" => modifier =>
            {
                if (CoreHelper.InEditor)
                {
                    EditorManager.inst.DisplayNotification("End level func", 1f, EditorManager.NotificationType.Success);
                    return;
                }

                var endLevelFunc = modifier.GetInt(0, 0);

                if (endLevelFunc > 0)
                {
                    var endLevelUpdateProgress = modifier.GetBool(2, true);

                    ArcadeHelper.endLevelFunc = (EndLevelFunction)(endLevelFunc - 1);
                    ArcadeHelper.endLevelData = modifier.GetValue(1);
                }
                ArcadeHelper.endLevelUpdateProgress = modifier.GetBool(2, true);

                LevelManager.EndLevel();
            },
            "setAudioTransition" => modifier =>
            {
                LevelManager.songFadeTransition = modifier.GetFloat(0, 0.5f);
            },
            "setIntroFade" => modifier =>
            {
                RTGameManager.doIntroFade = modifier.GetBool(0, true);
            },
            "setLevelEndFunc" => modifier =>
            {
                if (CoreHelper.InEditor)
                    return;

                var endLevelFunc = modifier.GetInt(0, 0);

                if (endLevelFunc > 0)
                {
                    var endLevelUpdateProgress = modifier.GetBool(2, true);

                    ArcadeHelper.endLevelFunc = (EndLevelFunction)(endLevelFunc - 1);
                    ArcadeHelper.endLevelData = modifier.GetValue(1);
                }
                ArcadeHelper.endLevelUpdateProgress = modifier.GetBool(2, true);
            },

            #endregion

            #region Component

            "blur" => modifier =>
            {
                if (modifier.reference && modifier.reference.objectType != BeatmapObject.ObjectType.Empty &&
                    Updater.TryGetObject(modifier.reference, out LevelObject levelObject) && levelObject.visualObject.renderer)
                {
                    var num = modifier.GetFloat(0, 0f);
                    var rend = levelObject.visualObject.renderer;

                    if (!modifier.HasResult())
                    {
                        var onDestroy = levelObject.visualObject.gameObject.AddComponent<DestroyModifierResult>();
                        onDestroy.Modifier = modifier;
                        modifier.Result = levelObject.visualObject.gameObject;
                        rend.material = LegacyResources.blur;
                    }
                    if (modifier.commands.Count > 1 && modifier.GetBool(1, false))
                        rend.material.SetFloat("_blurSizeXY", -(modifier.reference.Interpolate(3, 1) - 1f) * num);
                    else
                        rend.material.SetFloat("_blurSizeXY", num);
                }
            },
            "blurOther" => modifier =>
            {
                var list = !modifier.prefabInstanceOnly ? GameData.Current.FindObjectsWithTag(modifier.GetValue(1)) : GameData.Current.FindObjectsWithTag(modifier.reference, modifier.GetValue(1));
                if (list.IsEmpty())
                    return;

                var num = modifier.GetFloat(0, 0f);

                foreach (var beatmapObject in list)
                {
                    if (beatmapObject.objectType == BeatmapObject.ObjectType.Empty || !Updater.TryGetObject(beatmapObject, out LevelObject levelObject) || !levelObject.visualObject.renderer)
                        continue;

                    var rend = levelObject.visualObject.renderer;
                    if (!modifier.HasResult())
                    {
                        var onDestroy = levelObject.visualObject.gameObject.AddComponent<DestroyModifierResult>();
                        onDestroy.Modifier = modifier;
                        modifier.Result = levelObject.visualObject.gameObject;
                        rend.material = LegacyResources.blur;
                    }
                    rend.material.SetFloat("_blurSizeXY", -(beatmapObject.Interpolate(3, 1) - 1f) * num);
                }
            },
            "blurVariable" => modifier =>
            {
                if (modifier.reference && modifier.reference.objectType != BeatmapObject.ObjectType.Empty &&
                    Updater.TryGetObject(modifier.reference, out LevelObject levelObject) && levelObject.visualObject.renderer)
                {
                    var num = modifier.GetFloat(0, 0f);
                    var rend = levelObject.visualObject.renderer;

                    if (!modifier.HasResult())
                    {
                        var onDestroy = levelObject.visualObject.gameObject.AddComponent<DestroyModifierResult>();
                        onDestroy.Modifier = modifier;
                        modifier.Result = levelObject.visualObject.gameObject;
                        rend.material = LegacyResources.blur;
                    }
                    rend.material.SetFloat("_blurSizeXY", modifier.reference.integerVariable * num);
                }
            },
            "blurVariableOther" => modifier =>
            {
                var list = !modifier.prefabInstanceOnly ? GameData.Current.FindObjectsWithTag(modifier.GetValue(1)) : GameData.Current.FindObjectsWithTag(modifier.reference, modifier.GetValue(1));
                if (list.IsEmpty())
                    return;

                var num = modifier.GetFloat(0, 0f);

                foreach (var beatmapObject in list)
                {
                    if (beatmapObject.objectType == BeatmapObject.ObjectType.Empty || !Updater.TryGetObject(beatmapObject, out LevelObject levelObject) || !levelObject.visualObject.renderer)
                        continue;

                    var rend = levelObject.visualObject.renderer;
                    if (!modifier.HasResult())
                    {
                        var onDestroy = levelObject.visualObject.gameObject.AddComponent<DestroyModifierResult>();
                        onDestroy.Modifier = modifier;
                        modifier.Result = levelObject.visualObject.gameObject;
                        rend.material = LegacyResources.blur;
                    }
                    rend.material.SetFloat("_blurSizeXY", beatmapObject.integerVariable * num);
                }
            },
            "blurColored" => modifier =>
            {
                if (modifier.reference && modifier.reference.objectType != BeatmapObject.ObjectType.Empty &&
                    Updater.TryGetObject(modifier.reference, out LevelObject levelObject) && levelObject.visualObject.renderer)
                {
                    var num = modifier.GetFloat(0, 0f);
                    var rend = levelObject.visualObject.renderer;

                    if (!modifier.HasResult())
                    {
                        var onDestroy = levelObject.visualObject.gameObject.AddComponent<DestroyModifierResult>();
                        onDestroy.Modifier = modifier;
                        modifier.Result = levelObject.visualObject.gameObject;
                        rend.material.shader = LegacyResources.blurColored;
                    }

                    if (modifier.commands.Count > 1 && modifier.GetBool(1, false))
                        rend.material.SetFloat("_Size", -(modifier.reference.Interpolate(3, 1) - 1f) * num);
                    else
                        rend.material.SetFloat("_Size", num);
                }
            },
            "blurColoredOther" => modifier =>
            {
                var list = !modifier.prefabInstanceOnly ? GameData.Current.FindObjectsWithTag(modifier.commands[1]) : GameData.Current.FindObjectsWithTag(modifier.reference, modifier.commands[1]);
                if (list.IsEmpty())
                    return;

                var num = modifier.GetFloat(0, 0f);

                foreach (var beatmapObject in list)
                {
                    if (beatmapObject.objectType == BeatmapObject.ObjectType.Empty || !Updater.TryGetObject(beatmapObject, out LevelObject levelObject) || !levelObject.visualObject.renderer)
                        continue;

                    var rend = levelObject.visualObject.renderer;
                    if (!modifier.HasResult())
                    {
                        var onDestroy = levelObject.visualObject.gameObject.AddComponent<DestroyModifierResult>();
                        onDestroy.Modifier = modifier;
                        modifier.Result = levelObject.visualObject.gameObject;
                        rend.material.shader = LegacyResources.blurColored;
                    }
                    rend.material.SetFloat("_Size", -(beatmapObject.Interpolate(3, 1) - 1f) * num);
                }
            },
            "doubleSided" => modifier =>
            {
                if (Updater.TryGetObject(modifier.reference, out LevelObject levelObject) && levelObject.visualObject is SolidObject solidObject && solidObject.gameObject)
                    solidObject.UpdateRendering((int)modifier.reference.gradientType, (int)modifier.reference.renderLayerType, true, modifier.reference.gradientScale, modifier.reference.gradientRotation);
            },
            "particleSystem" => modifier =>
            {
                if (!modifier.reference || !Updater.TryGetObject(modifier.reference, out LevelObject levelObject) || levelObject.visualObject == null || !levelObject.visualObject.gameObject)
                    return;

                var gameObject = levelObject.visualObject.gameObject;

                if (modifier.Result == null || modifier.Result is KeyValuePair<ParticleSystem, ParticleSystemRenderer> keyValuePair && (!keyValuePair.Key || !keyValuePair.Value))
                {
                    var ps = gameObject.GetOrAddComponent<ParticleSystem>();
                    var psr = gameObject.GetComponent<ParticleSystemRenderer>();

                    var s = Parser.TryParse(modifier.commands[1], 0);
                    var so = Parser.TryParse(modifier.commands[2], 0);

                    s = Mathf.Clamp(s, 0, ObjectManager.inst.objectPrefabs.Count - 1);
                    so = Mathf.Clamp(so, 0, ObjectManager.inst.objectPrefabs[s].options.Count - 1);

                    psr.mesh = ObjectManager.inst.objectPrefabs[s == 4 ? 0 : s == 6 ? 0 : s].options[so].GetComponentInChildren<MeshFilter>().mesh;

                    psr.material = GameManager.inst.PlayerPrefabs[0].transform.GetChild(0).GetChild(0).GetComponent<TrailRenderer>().material;
                    psr.material.color = Color.white;
                    psr.trailMaterial = psr.material;
                    psr.renderMode = ParticleSystemRenderMode.Mesh;

                    var psMain = ps.main;
                    var psEmission = ps.emission;

                    psMain.simulationSpace = ParticleSystemSimulationSpace.World;

                    var rotationOverLifetime = ps.rotationOverLifetime;
                    rotationOverLifetime.enabled = true;
                    rotationOverLifetime.separateAxes = true;
                    rotationOverLifetime.xMultiplier = 0f;
                    rotationOverLifetime.yMultiplier = 0f;

                    var forceOverLifetime = ps.forceOverLifetime;
                    forceOverLifetime.enabled = true;
                    forceOverLifetime.space = ParticleSystemSimulationSpace.World;

                    modifier.Result = new KeyValuePair<ParticleSystem, ParticleSystemRenderer>(ps, psr);
                    gameObject.AddComponent<DestroyModifierResult>().Modifier = modifier;
                }

                if (modifier.Result is KeyValuePair<ParticleSystem, ParticleSystemRenderer> particleSystems && particleSystems.Key && particleSystems.Value)
                {
                    var ps = particleSystems.Key;
                    var psr = particleSystems.Value;

                    var psMain = ps.main;
                    var psEmission = ps.emission;

                    psMain.startSpeed = Parser.TryParse(modifier.commands[9], 5f);

                    if (modifier.constant)
                        ps.emissionRate = Parser.TryParse(modifier.commands[10], 1f);
                    else
                    {
                        ps.emissionRate = 0f;
                        psMain.loop = false;
                        psEmission.burstCount = Parser.TryParse(modifier.commands[10], 1);
                        psMain.duration = Parser.TryParse(modifier.commands[11], 1f);
                    }

                    var rotationOverLifetime = ps.rotationOverLifetime;
                    rotationOverLifetime.zMultiplier = Parser.TryParse(modifier.commands[8], 0f);

                    var forceOverLifetime = ps.forceOverLifetime;
                    forceOverLifetime.xMultiplier = Parser.TryParse(modifier.commands[12], 0f);
                    forceOverLifetime.yMultiplier = Parser.TryParse(modifier.commands[13], 0f);

                    var particlesTrail = ps.trails;
                    particlesTrail.enabled = Parser.TryParse(modifier.commands[14], true);

                    var colorOverLifetime = ps.colorOverLifetime;
                    colorOverLifetime.enabled = true;
                    var psCol = colorOverLifetime.color;

                    float alphaStart = Parser.TryParse(modifier.commands[4], 1f);
                    float alphaEnd = Parser.TryParse(modifier.commands[5], 0f);

                    psCol.gradient.alphaKeys = new GradientAlphaKey[2] { new GradientAlphaKey(alphaStart, 0f), new GradientAlphaKey(alphaEnd, 1f) };
                    psCol.gradient.colorKeys = new GradientColorKey[2] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) };
                    psCol.gradient.mode = GradientMode.Blend;

                    colorOverLifetime.color = psCol;

                    var sizeOverLifetime = ps.sizeOverLifetime;
                    sizeOverLifetime.enabled = true;

                    var ssss = sizeOverLifetime.size;

                    var sizeStart = Parser.TryParse(modifier.commands[6], 0f);
                    var sizeEnd = Parser.TryParse(modifier.commands[7], 0f);

                    var curve = new AnimationCurve(new Keyframe[2] { new Keyframe(0f, sizeStart), new Keyframe(1f, sizeEnd) });

                    ssss.curve = curve;

                    sizeOverLifetime.size = ssss;

                    psMain.startLifetime = float.Parse(modifier.value);
                    psEmission.enabled = !(gameObject.transform.lossyScale.x < 0.001f && gameObject.transform.lossyScale.x > -0.001f || gameObject.transform.lossyScale.y < 0.001f && gameObject.transform.lossyScale.y > -0.001f) && gameObject.activeSelf && gameObject.activeInHierarchy;

                    psMain.startColor = CoreHelper.CurrentBeatmapTheme.GetObjColor(Parser.TryParse(modifier.commands[3], 0));

                    if (!modifier.constant)
                        ps.Play();

                    var shape = ps.shape;
                    shape.angle = Parser.TryParse(modifier.commands[15], 90f);
                }
            },
            "trailRenderer" => modifier =>
            {
                if (!modifier.reference || !Updater.TryGetObject(modifier.reference, out LevelObject levelObject) || levelObject.visualObject == null || !levelObject.visualObject.gameObject)
                    return;

                var mod = levelObject.visualObject.gameObject;

                if (!modifier.reference.trailRenderer && !mod.GetComponent<TrailRenderer>())
                {
                    modifier.reference.trailRenderer = mod.AddComponent<TrailRenderer>();

                    modifier.reference.trailRenderer.material = GameManager.inst.PlayerPrefabs[0].transform.GetChild(0).GetChild(0).GetComponent<TrailRenderer>().material;
                    modifier.reference.trailRenderer.material.color = Color.white;
                }
                else if (!modifier.reference.trailRenderer)
                {
                    modifier.reference.trailRenderer = mod.GetComponent<TrailRenderer>();

                    modifier.reference.trailRenderer.material = GameManager.inst.PlayerPrefabs[0].transform.GetChild(0).GetChild(0).GetComponent<TrailRenderer>().material;
                    modifier.reference.trailRenderer.material.color = Color.white;
                }
                else
                {
                    var tr = modifier.reference.trailRenderer;

                    if (float.TryParse(modifier.value, out float time))
                    {
                        tr.time = time;
                    }

                    tr.emitting = !(mod.transform.lossyScale.x < 0.001f && mod.transform.lossyScale.x > -0.001f || mod.transform.lossyScale.y < 0.001f && mod.transform.lossyScale.y > -0.001f) && mod.activeSelf && mod.activeInHierarchy;

                    if (float.TryParse(modifier.commands[1], out float startWidth) && float.TryParse(modifier.commands[2], out float endWidth))
                    {
                        var t = mod.transform.lossyScale.magnitude * 0.576635f;
                        tr.startWidth = startWidth * t;
                        tr.endWidth = endWidth * t;
                    }

                    var beatmapTheme = CoreHelper.CurrentBeatmapTheme;

                    if (int.TryParse(modifier.commands[3], out int startColor) && float.TryParse(modifier.commands[4], out float startOpacity))
                        tr.startColor = LSColors.fadeColor(beatmapTheme.GetObjColor(startColor), startOpacity);
                    if (int.TryParse(modifier.commands[5], out int endColor) && float.TryParse(modifier.commands[6], out float endOpacity))
                        tr.endColor = LSColors.fadeColor(beatmapTheme.GetObjColor(endColor), endOpacity);
                }
            },
            "rigidbody" => modifier =>
            {
                var gravity = modifier.GetFloat(1, 0f);
                var collisionMode = modifier.GetInt(2, 0);
                var drag = modifier.GetFloat(3, 0f);
                var velocityX = modifier.GetFloat(4, 0f);
                var velocityY = modifier.GetFloat(5, 0f);
                var bodyType = modifier.GetInt(6, 0);

                if (!Updater.TryGetObject(modifier.reference, out LevelObject levelObject) || !levelObject.visualObject || !levelObject.visualObject.gameObject)
                    return;

                if (!modifier.reference.rigidbody)
                    modifier.reference.rigidbody = levelObject.visualObject.gameObject.GetOrAddComponent<Rigidbody2D>();

                modifier.reference.rigidbody.gravityScale = gravity;
                modifier.reference.rigidbody.collisionDetectionMode = (CollisionDetectionMode2D)Mathf.Clamp(collisionMode, 0, 1);
                modifier.reference.rigidbody.drag = drag;

                modifier.reference.rigidbody.bodyType = (RigidbodyType2D)Mathf.Clamp(bodyType, 0, 2);

                modifier.reference.rigidbody.velocity += new Vector2(velocityX, velocityY);
            },
            "rigidbodyOther" => modifier =>
            {
                var gravity = modifier.GetFloat(1, 0f);
                var collisionMode = modifier.GetInt(2, 0);
                var drag = modifier.GetFloat(3, 0f);
                var velocityX = modifier.GetFloat(4, 0f);
                var velocityY = modifier.GetFloat(5, 0f);
                var bodyType = modifier.GetInt(6, 0);

                var list = !modifier.prefabInstanceOnly ? GameData.Current.FindObjectsWithTag(modifier.GetValue(0)) : GameData.Current.FindObjectsWithTag(modifier.reference, modifier.GetValue(0));
                if (list.IsEmpty())
                    return;

                foreach (var beatmapObject in list)
                {
                    if (!Updater.TryGetObject(beatmapObject, out LevelObject levelObject) || !levelObject.visualObject || !levelObject.visualObject.gameObject)
                        continue;

                    if (!beatmapObject.rigidbody)
                        beatmapObject.rigidbody = levelObject.visualObject.gameObject.GetOrAddComponent<Rigidbody2D>();

                    beatmapObject.rigidbody.gravityScale = gravity;
                    beatmapObject.rigidbody.collisionDetectionMode = (CollisionDetectionMode2D)Mathf.Clamp(collisionMode, 0, 1);
                    beatmapObject.rigidbody.drag = drag;

                    beatmapObject.rigidbody.bodyType = (RigidbodyType2D)Mathf.Clamp(bodyType, 0, 2);

                    beatmapObject.rigidbody.velocity += new Vector2(velocityX, velocityY);
                }
            },
            "videoPlayer" => modifier =>
            {
                if (Updater.TryGetObject(modifier.reference, out LevelObject levelObject) && levelObject.visualObject is SolidObject solidObject && solidObject.gameObject)
                {
                    var filePath = RTFile.CombinePaths(RTFile.BasePath, modifier.value);
                    if (!RTFile.FileExists(filePath))
                        return;

                    if (modifier.Result == null || modifier.Result is RTVideoPlayer nullVideo && !nullVideo)
                    {
                        var gameObject = levelObject.visualObject.gameObject;
                        var videoPlayer = gameObject.GetComponent<RTVideoPlayer>() ?? gameObject.AddComponent<RTVideoPlayer>();

                        solidObject.renderer.material = GameObject.Find("ExtraBG").transform.GetChild(0).GetComponent<Renderer>().material;

                        modifier.Result = videoPlayer;
                    }

                    if (modifier.Result is RTVideoPlayer videeoPlayer &&
                        float.TryParse(modifier.commands[1], out float timeOffset) &&
                        int.TryParse(modifier.commands[2], out int audioOutputType))
                    {
                        videeoPlayer.timeOffset = modifier.reference.StartTime + timeOffset;

                        if (videeoPlayer.didntPlay)
                            videeoPlayer.Play(videeoPlayer.gameObject, filePath, (UnityEngine.Video.VideoAudioOutputMode)audioOutputType);
                    }
                }
            }, // todo: dunno if this will ever be implemented as one video player might be enough

            #endregion

            // todo: implement index modifiers
            #region Player

            // hit
            "playerHit" => modifier =>
            {
                if (!modifier.reference || PlayerManager.Invincible || modifier.constant)
                    return;

                var pos = Updater.TryGetObject(modifier.reference, out LevelObject levelObject) && levelObject.visualObject != null && levelObject.visualObject.gameObject ? levelObject.visualObject.gameObject.transform.position : modifier.reference.InterpolateChainPosition();

                var player = PlayerManager.GetClosestPlayer(pos);

                if (player && player.Player)
                    player.Player.Hit(Mathf.Clamp(modifier.GetInt(0, 1), 0, int.MaxValue));
            },
            "playerHitIndex" => modifier =>
            {
                if (PlayerManager.Invincible || modifier.constant)
                    return;

                if (PlayerManager.Players.TryGetAt(modifier.GetInt(0, 0), out CustomPlayer customPlayer) && customPlayer.Player)
                    customPlayer.Player.Hit(Mathf.Clamp(modifier.GetInt(0, 1), 0, int.MaxValue));
            },
            "playerHitAll" => modifier =>
            {
                if (PlayerManager.Invincible || modifier.constant)
                    return;

                var damage = Mathf.Clamp(modifier.GetInt(0, 1), 0, int.MaxValue);
                foreach (var player in PlayerManager.Players.Where(x => x.Player))
                    player.Player.Hit(damage);
            },

            // heal
            "playerHeal" => modifier =>
            {
                if (!modifier.reference || PlayerManager.Invincible || modifier.constant)
                    return;

                var heal = Mathf.Clamp(modifier.GetInt(0, 1), 0, int.MaxValue);

                var pos = Updater.TryGetObject(modifier.reference, out LevelObject levelObject) && levelObject.visualObject != null && levelObject.visualObject.gameObject ? levelObject.visualObject.gameObject.transform.position : modifier.reference.InterpolateChainPosition();

                var player = PlayerManager.GetClosestPlayer(pos);

                if (player && player.Player)
                    player.Player.Heal(heal);
            },
            "playerHealIndex" => modifier =>
            {
                if (PlayerManager.Invincible || modifier.constant)
                    return;

                var health = Mathf.Clamp(modifier.GetInt(0, 1), 0, int.MaxValue);
                if (PlayerManager.Players.TryGetAt(modifier.GetInt(1, 0), out CustomPlayer customPlayer) && customPlayer.Player)
                    customPlayer.Player.Heal(health);
            },
            "playerHealAll" => modifier =>
            {
                if (PlayerManager.Invincible || modifier.constant)
                    return;
                {
                    var heal = Mathf.Clamp(modifier.GetInt(0, 1), 0, int.MaxValue);
                    bool healed = false;
                    foreach (var player in PlayerManager.Players)
                    {
                        if (player.Player)
                            if (player.Player.Heal(heal, false))
                                healed = true;
                    }

                    if (healed)
                        SoundManager.inst.PlaySound(DefaultSounds.HealPlayer);
                }
            },

            // kill
            "playerKill" => modifier =>
            {
                if (!modifier.reference || PlayerManager.Invincible || modifier.constant)
                    return;

                var pos = Updater.TryGetObject(modifier.reference, out LevelObject levelObject) && levelObject.visualObject != null && levelObject.visualObject.gameObject ? levelObject.visualObject.gameObject.transform.position : modifier.reference.InterpolateChainPosition();

                var player = PlayerManager.GetClosestPlayer(pos);

                if (player && player.Player)
                    player.Player.Kill();
            },
            "playerKillIndex" => modifier =>
            {
                if (PlayerManager.Invincible || modifier.constant)
                    return;

                if (PlayerManager.Players.TryGetAt(modifier.GetInt(0, 0), out CustomPlayer customPlayer) && customPlayer.Player)
                    customPlayer.Player.Kill();
            },
            "playerKillAll" => modifier =>
            {
                if (PlayerManager.Invincible || modifier.constant)
                    return;

                foreach (var player in PlayerManager.Players)
                    if (player.Player)
                        player.Player.Kill();
            },

            // respawn
            "playerRespawn" => modifier =>
            {
                if (!modifier.reference || modifier.constant)
                    return;

                var pos = Updater.TryGetObject(modifier.reference, out LevelObject levelObject) && levelObject.visualObject != null && levelObject.visualObject.gameObject ? levelObject.visualObject.gameObject.transform.position : modifier.reference.InterpolateChainPosition();

                var playerIndex = PlayerManager.GetClosestPlayerIndex(pos);

                if (playerIndex >= 0)
                    PlayerManager.RespawnPlayer(playerIndex);
            },
            "playerRespawnIndex" => modifier =>
            {
                if (modifier.constant)
                    return;

                var playerIndex = modifier.GetInt(0, 0);
                if (PlayerManager.Players.TryGetAt(modifier.GetInt(0, 0), out CustomPlayer customPlayer))
                    PlayerManager.RespawnPlayer(playerIndex);
            },
            "playerRespawnAll" => modifier =>
            {
                if (!modifier.constant)
                    PlayerManager.RespawnPlayers();
            },

            // player move TODO: (rework these)
            "playerMove" => modifier =>
            {
                if (!modifier.reference)
                    return;

                var pos = Updater.TryGetObject(modifier.reference, out LevelObject levelObject) && levelObject.visualObject != null && levelObject.visualObject.gameObject ? levelObject.visualObject.gameObject.transform.position : modifier.reference.InterpolateChainPosition();

                var player = PlayerManager.GetClosestPlayer(pos);

                var vector = modifier.value.Split(new char[] { ',' });

                bool relative = Parser.TryParse(modifier.commands[3], false);
                if (!player)
                    return;

                var tf = player.Player.rb.transform;
                if (modifier.constant)
                    tf.localPosition = new Vector3(Parser.TryParse(vector[0], 0f), Parser.TryParse(vector[1], 0f), 0f);
                else
                    tf
                        .DOLocalMove(new Vector3(Parser.TryParse(vector[0], 0f) + (relative ? tf.position.x : 0f), Parser.TryParse(vector[1], 0f) + (relative ? tf.position.y : 0f), 0f), Parser.TryParse(modifier.commands[1], 1f))
                        .SetEase(DataManager.inst.AnimationList[Parser.TryParse(modifier.commands[2], 0)].Animation);
            },
            "playerMoveAll" => modifier =>
            {
                var vector = modifier.value.Split(new char[] { ',' });

                bool relative = Parser.TryParse(modifier.commands[3], false);
                foreach (var player in PlayerManager.Players.Where(x => x.Player))
                {
                    var tf = player.Player.rb.transform;
                    if (modifier.constant)
                        tf.localPosition = new Vector3(Parser.TryParse(vector[0], 0f), Parser.TryParse(vector[1], 0f), 0f);
                    else
                        tf
                            .DOLocalMove(new Vector3(Parser.TryParse(vector[0], 0f) + (relative ? tf.position.x : 0f), Parser.TryParse(vector[1], 0f) + (relative ? tf.position.y : 0f), 0f), Parser.TryParse(modifier.commands[1], 1f))
                            .SetEase(DataManager.inst.AnimationList[Parser.TryParse(modifier.commands[2], 0)].Animation);
                }
            },
            "playerMoveX" => modifier =>
            {
                if (!modifier.reference)
                    return;

                var pos = Updater.TryGetObject(modifier.reference, out LevelObject levelObject) && levelObject.visualObject != null && levelObject.visualObject.gameObject ? levelObject.visualObject.gameObject.transform.position : modifier.reference.InterpolateChainPosition();

                var player = PlayerManager.GetClosestPlayer(pos);

                bool relative = Parser.TryParse(modifier.commands[3], false);
                if (!player)
                    return;

                var tf = player.Player.rb.transform;
                if (modifier.constant)
                {
                    var v = tf.localPosition;
                    v.x += Parser.TryParse(modifier.value, 1f);
                    tf.localPosition = v;
                }
                else
                    tf
                        .DOLocalMoveX(Parser.TryParse(modifier.value, 0f) + (relative ? tf.position.x : 0f), Parser.TryParse(modifier.commands[1], 1f))
                        .SetEase(DataManager.inst.AnimationList[Parser.TryParse(modifier.commands[2], 0)].Animation);
            },
            "playerMoveXAll" => modifier =>
            {
                bool relative = Parser.TryParse(modifier.commands[3], false);
                foreach (var player in PlayerManager.Players.Where(x => x.Player))
                {
                    var tf = player.Player.rb.transform;
                    if (modifier.constant)
                    {
                        var v = tf.localPosition;
                        v.x += Parser.TryParse(modifier.value, 1f);
                        tf.localPosition = v;
                    }
                    else
                        tf
                            .DOLocalMoveX(Parser.TryParse(modifier.value, 0f) + (relative ? tf.position.x : 0f), Parser.TryParse(modifier.commands[1], 1f))
                            .SetEase(DataManager.inst.AnimationList[Parser.TryParse(modifier.commands[2], 0)].Animation);
                }
            },
            "playerMoveY" => modifier =>
            {
                if (!modifier.reference)
                    return;

                var pos = Updater.TryGetObject(modifier.reference, out LevelObject levelObject) && levelObject.visualObject != null && levelObject.visualObject.gameObject ? levelObject.visualObject.gameObject.transform.position : modifier.reference.InterpolateChainPosition();

                var player = PlayerManager.GetClosestPlayer(pos);

                bool relative = Parser.TryParse(modifier.commands[3], false);
                if (!player)
                    return;

                var tf = player.Player.rb.transform;
                if (modifier.constant)
                {
                    var v = tf.localPosition;
                    v.y += Parser.TryParse(modifier.value, 1f);
                    tf.localPosition = v;
                }
                else
                    tf
                        .DOLocalMoveY(Parser.TryParse(modifier.value, 0f) + (relative ? tf.position.y : 0f), Parser.TryParse(modifier.commands[1], 1f))
                        .SetEase(DataManager.inst.AnimationList[Parser.TryParse(modifier.commands[2], 0)].Animation);
            },
            "playerMoveYAll" => modifier =>
            {
                bool relative = Parser.TryParse(modifier.commands[3], false);
                foreach (var player in PlayerManager.Players.Where(x => x.Player))
                {
                    var tf = player.Player.rb.transform;
                    if (modifier.constant)
                    {
                        var v = tf.localPosition;
                        v.y += Parser.TryParse(modifier.value, 1f);
                        tf.localPosition = v;
                    }
                    else
                        tf
                            .DOLocalMoveY(Parser.TryParse(modifier.value, 0f) + (relative ? tf.position.y : 0f), Parser.TryParse(modifier.commands[1], 1f))
                            .SetEase(DataManager.inst.AnimationList[Parser.TryParse(modifier.commands[2], 0)].Animation);
                }
            },
            "playerRotate" => modifier =>
            {
                if (!modifier.reference)
                    return;

                var pos = Updater.TryGetObject(modifier.reference, out LevelObject levelObject) && levelObject.visualObject != null && levelObject.visualObject.gameObject ? levelObject.visualObject.gameObject.transform.position : modifier.reference.InterpolateChainPosition();

                var player = PlayerManager.GetClosestPlayer(pos);

                bool relative = Parser.TryParse(modifier.commands[3], false);
                if (!player)
                    return;

                if (modifier.constant)
                {
                    var v = player.Player.rb.transform.localRotation.eulerAngles;
                    v.z += Parser.TryParse(modifier.value, 1f);
                    player.Player.rb.transform.localRotation = Quaternion.Euler(v);
                }
                else
                    player.Player.rb.transform
                        .DORotate(new Vector3(0f, 0f, Parser.TryParse(modifier.value, 0f)), Parser.TryParse(modifier.commands[1], 1f))
                        .SetEase(DataManager.inst.AnimationList[Parser.TryParse(modifier.commands[2], 0)].Animation);
            },
            "playerRotateAll" => modifier =>
            {
                bool relative = Parser.TryParse(modifier.commands[3], false);
                foreach (var player in PlayerManager.Players.Where(x => x.Player))
                {
                    if (modifier.constant)
                    {
                        var v = player.Player.rb.transform.localRotation.eulerAngles;
                        v.z += Parser.TryParse(modifier.value, 1f);
                        player.Player.rb.transform.localRotation = Quaternion.Euler(v);
                    }
                    else
                        player.Player.rb.transform
                            .DORotate(new Vector3(0f, 0f, Parser.TryParse(modifier.value, 0f)), Parser.TryParse(modifier.commands[1], 1f))
                            .SetEase(DataManager.inst.AnimationList[Parser.TryParse(modifier.commands[2], 0)].Animation);
                }
            },

            // move to object
            "playerMoveToObject" => modifier =>
            {
                if (!modifier.reference)
                    return;

                var pos = Updater.TryGetObject(modifier.reference, out LevelObject levelObject) && levelObject.visualObject != null && levelObject.visualObject.gameObject ? levelObject.visualObject.gameObject.transform.position : modifier.reference.InterpolateChainPosition();

                var player = PlayerManager.GetClosestPlayer(pos);

                if (!player || !player.Player || !player.Player.rb)
                    return;

                player.Player.rb.position = new Vector3(pos.x, pos.y, 0f);
            },
            "playerMoveAllToObject" => modifier =>
            {
                if (!modifier.reference)
                    return;

                var pos = Updater.TryGetObject(modifier.reference, out LevelObject levelObject) && levelObject.visualObject != null && levelObject.visualObject.gameObject ? levelObject.visualObject.gameObject.transform.position : modifier.reference.InterpolateChainPosition();

                var player = PlayerManager.GetClosestPlayer(pos);

                if (!player || !player.Player || !player.Player.rb)
                    return;

                var y = player.Player.rb.position.y;
                player.Player.rb.position = new Vector2(pos.x, y);
            },
            "playerMoveXToObject" => modifier =>
            {
                if (!modifier.reference)
                    return;

                var pos = Updater.TryGetObject(modifier.reference, out LevelObject levelObject) && levelObject.visualObject != null && levelObject.visualObject.gameObject ? levelObject.visualObject.gameObject.transform.position : modifier.reference.InterpolateChainPosition();

                var player = PlayerManager.GetClosestPlayer(pos);

                if (!player || !player.Player || !player.Player.rb)
                    return;

                var y = player.Player.rb.position.y;
                player.Player.rb.position = new Vector2(pos.x, y);
            },
            "playerMoveXAllToObject" => modifier =>
            {
                if (!modifier.reference)
                    return;

                var pos = Updater.TryGetObject(modifier.reference, out LevelObject levelObject) && levelObject.visualObject != null && levelObject.visualObject.gameObject ? levelObject.visualObject.gameObject.transform.position : modifier.reference.InterpolateChainPosition();

                foreach (var player in PlayerManager.Players)
                {
                    if (!player.Player || !player.Player.rb)
                        continue;

                    var y = player.Player.rb.position.y;
                    player.Player.rb.position = new Vector2(pos.x, y);
                }
            },
            "playerMoveYToObject" => modifier =>
            {
                if (!modifier.reference)
                    return;

                var pos = Updater.TryGetObject(modifier.reference, out LevelObject levelObject) && levelObject.visualObject != null && levelObject.visualObject.gameObject ? levelObject.visualObject.gameObject.transform.position : modifier.reference.InterpolateChainPosition();

                var player = PlayerManager.GetClosestPlayer(pos);

                if (!player || !player.Player || !player.Player.rb)
                    return;

                var x = player.Player.rb.position.x;
                player.Player.rb.position = new Vector2(x, pos.y);
            },
            "playerMoveYAllToObject" => modifier =>
            {
                if (!modifier.reference)
                    return;

                var pos = Updater.TryGetObject(modifier.reference, out LevelObject levelObject) && levelObject.visualObject != null && levelObject.visualObject.gameObject ? levelObject.visualObject.gameObject.transform.position : modifier.reference.InterpolateChainPosition();

                foreach (var player in PlayerManager.Players)
                {
                    if (!player.Player || !player.Player.rb)
                        continue;

                    var x = player.Player.rb.position.x;
                    player.Player.rb.position = new Vector2(x, pos.y);
                }
            },
            "playerRotateToObject" => modifier =>
            {
                if (!modifier.reference)
                    return;

                var pos = Updater.TryGetObject(modifier.reference, out LevelObject levelObject) && levelObject.visualObject != null && levelObject.visualObject.gameObject ? levelObject.visualObject.gameObject.transform.position : modifier.reference.InterpolateChainPosition();

                var player = PlayerManager.GetClosestPlayer(pos);

                if (!player || !player.Player || !player.Player.rb)
                    return;

                player.Player.rb.transform.SetLocalRotationEulerZ(levelObject.visualObject.gameObject.transform.localRotation.eulerAngles.z);
            },
            "playerRotateAllToObject" => modifier =>
            {
                if (!modifier.reference)
                    return;

                var rot = Updater.TryGetObject(modifier.reference, out LevelObject levelObject) && levelObject.visualObject != null && levelObject.visualObject.gameObject ? levelObject.visualObject.gameObject.transform.localRotation.eulerAngles.z : modifier.reference.InterpolateChainRotation();

                foreach (var player in PlayerManager.Players)
                {
                    if (!player.Player || !player.Player.rb)
                        continue;

                    player.Player.rb.transform.SetLocalRotationEulerZ(rot);
                }
            },

            // actions
            "playerBoost" => modifier =>
            {
                if (modifier.constant || !modifier.reference || !Updater.TryGetObject(modifier.reference, out LevelObject levelObject) || !levelObject.visualObject.gameObject)
                    return;

                var player = PlayerManager.GetClosestPlayer(levelObject.visualObject.gameObject.transform.position);

                if (!player || !player.Player)
                    return;

                player.Player.Boost();
            },
            "playerBoostIndex" => modifier =>
            {
                if (!modifier.constant && PlayerManager.Players.TryGetAt(modifier.GetInt(0, 0), out CustomPlayer customPlayer) && customPlayer.Player)
                    customPlayer.Player.Boost();
            },
            "playerBoostAll" => modifier =>
            {
                foreach (var player in PlayerManager.Players.Where(x => x.Player))
                    player.Player.Boost();
            },
            "playerDisableBoost" => modifier =>
            {
                if (modifier.reference && Updater.TryGetObject(modifier.reference, out LevelObject levelObject) && levelObject.visualObject.gameObject)
                {
                    var player = PlayerManager.GetClosestPlayer(levelObject.visualObject.gameObject.transform.position);

                    if (player && player.Player)
                        player.Player.CanBoost = false;
                }
            },
            "playerDisableBoostIndex" => modifier =>
            {
                if (modifier.reference && PlayerManager.Players.TryGetAt(modifier.GetInt(0, 0), out CustomPlayer customPlayer) && customPlayer.Player)
                    customPlayer.Player.CanBoost = false;
            },
            "playerDisableBoostAll" => modifier =>
            {
                foreach (var player in PlayerManager.Players.Where(x => x.Player))
                    player.Player.CanBoost = false;
            },
            "playerEnableBoost" => modifier =>
            {
                if (modifier.reference && Updater.TryGetObject(modifier.reference, out LevelObject levelObject) && levelObject.visualObject.gameObject)
                {
                    var player = PlayerManager.GetClosestPlayer(levelObject.visualObject.gameObject.transform.position);

                    if (player && player.Player)
                        player.Player.CanBoost = true;
                }
            },
            "playerEnableBoostIndex" => modifier =>
            {
                if (modifier.reference && PlayerManager.Players.TryGetAt(modifier.GetInt(0, 0), out CustomPlayer customPlayer) && customPlayer.Player)
                    customPlayer.Player.CanBoost = true;
            },
            "playerEnableBoostAll" => modifier =>
            {
                foreach (var player in PlayerManager.Players.Where(x => x.Player))
                    player.Player.CanBoost = true;
            },

            // speed
            "playerSpeed" => modifier =>
            {
                RTPlayer.SpeedMultiplier = modifier.GetFloat(0, 1f);
            },
            "playerVelocityAll" => modifier =>
            {
                var x = modifier.GetFloat(1, 0f);
                var y = modifier.GetFloat(2, 0f);

                for (int i = 0; i < PlayerManager.Players.Count; i++)
                {
                    var player = PlayerManager.Players[i];
                    if (player.Player && player.Player.rb)
                        player.Player.rb.velocity = new Vector2(x, y);
                }
            },
            "playerVelocityXAll" => modifier =>
            {
                var x = modifier.GetFloat(0, 0f);

                for (int i = 0; i < PlayerManager.Players.Count; i++)
                {
                    var player = PlayerManager.Players[i];
                    if (!player.Player || !player.Player.rb)
                        continue;

                    var velocity = player.Player.rb.velocity;
                    velocity.x = x;
                    player.Player.rb.velocity = velocity;
                }
            },
            "playerVelocityYAll" => modifier =>
            {
                var x = modifier.GetFloat(0, 0f);

                for (int i = 0; i < PlayerManager.Players.Count; i++)
                {
                    var player = PlayerManager.Players[i];
                    if (!player.Player || !player.Player.rb)
                        continue;

                    var velocity = player.Player.rb.velocity;
                    velocity.y = x;
                    player.Player.rb.velocity = velocity;
                }
            },

            "setPlayerModel" => modifier =>
            {
                if (modifier.constant)
                    return;

                var index = modifier.GetInt(1, 0);

                if (!PlayersData.Current.playerModels.ContainsKey(modifier.GetValue(0)))
                    return;

                PlayersData.Current.SetPlayerModel(index, modifier.GetValue(0));
                PlayerManager.AssignPlayerModels();

                if (PlayerManager.Players.TryGetAt(index, out CustomPlayer customPlayer) || !customPlayer.Player)
                    return;

                customPlayer.Player.playerNeedsUpdating = true;
                customPlayer.Player.UpdateModel();
            },
            "gameMode" => modifier =>
            {
                RTPlayer.GameMode = (GameMode)modifier.GetInt(0, 0);
            },

            #endregion

            #region Mouse Cursor

            "showMouse" => modifier =>
            {
                CursorManager.inst.ShowCursor();
            },
            "hideMouse" => modifier =>
            {
                if (CoreHelper.InEditorPreview)
                    CursorManager.inst.HideCursor();
            },
            "setMousePosition" => modifier =>
            {
                if (CoreHelper.IsEditing)
                    return;

                var screenScale = Display.main.systemWidth / 1920f;
                float windowCenterX = (Display.main.systemWidth) / 2;
                float windowCenterY = (Display.main.systemHeight) / 2;

                var x = modifier.GetFloat(1, 0f);
                var y = modifier.GetFloat(2, 0f);

                CursorManager.inst.SetCursorPosition(new Vector2(((x * screenScale) + windowCenterX), ((y * screenScale) + windowCenterY)));
            },
            "followMousePosition" => modifier =>
            {
                if (modifier.value == "0")
                    modifier.value = "1";

                Vector2 mousePosition = Input.mousePosition;
                mousePosition = Camera.main.ScreenToWorldPoint(mousePosition);

                float p = Time.deltaTime * 60f;
                float po = 1f - Mathf.Pow(1f - Mathf.Clamp(Parser.TryParse(modifier.value, 1f), 0.001f, 1f), p);
                float ro = 1f - Mathf.Pow(1f - Mathf.Clamp(Parser.TryParse(modifier.commands[1], 1f), 0.001f, 1f), p);

                if (modifier.Result == null)
                    modifier.Result = Vector2.zero;

                var dragPos = (Vector2)modifier.Result;

                var target = new Vector2(mousePosition.x, mousePosition.y);

                modifier.reference.rotationOffset = new Vector3(0f, 0f, (target.x - dragPos.x) * ro);

                dragPos += (target - dragPos) * po;

                modifier.Result = dragPos;

                modifier.reference.positionOffset = dragPos;
            },

            #endregion

            #region Variable

            "addVariable" => modifier =>
            {
                if (modifier.commands.Count == 2)
                {
                    var list = !modifier.prefabInstanceOnly ? GameData.Current.FindObjectsWithTag(modifier.commands[1]) : GameData.Current.FindObjectsWithTag(modifier.reference, modifier.commands[1]);
                    if (list.IsEmpty())
                        return;

                    int num = modifier.GetInt(0, 0);

                    foreach (var beatmapObject in list)
                        beatmapObject.integerVariable += num;
                }
                else
                    modifier.reference.integerVariable += modifier.GetInt(0, 0);
            },
            "addVariableOther" => modifier =>
            {
                var list = !modifier.prefabInstanceOnly ? GameData.Current.FindObjectsWithTag(modifier.GetValue(1)) : GameData.Current.FindObjectsWithTag(modifier.reference, modifier.GetValue(1));
                if (list.IsEmpty())
                    return;

                int num = modifier.GetInt(0, 0);

                foreach (var beatmapObject in list)
                    beatmapObject.integerVariable += num;
            },
            "subVariable" => modifier =>
            {
                if (modifier.commands.Count == 2)
                {
                    var list = !modifier.prefabInstanceOnly ? GameData.Current.FindObjectsWithTag(modifier.commands[1]) : GameData.Current.FindObjectsWithTag(modifier.reference, modifier.commands[1]);
                    if (list.IsEmpty())
                        return;

                    int num = modifier.GetInt(0, 0);

                    foreach (var beatmapObject in list)
                        beatmapObject.integerVariable -= num;
                }
                else
                    modifier.reference.integerVariable -= modifier.GetInt(0, 0);
            },
            "subVariableOther" => modifier =>
            {
                var list = !modifier.prefabInstanceOnly ? GameData.Current.FindObjectsWithTag(modifier.GetValue(1)) : GameData.Current.FindObjectsWithTag(modifier.reference, modifier.GetValue(1));
                if (list.IsEmpty())
                    return;

                int num = modifier.GetInt(0, 0);

                foreach (var beatmapObject in list)
                    beatmapObject.integerVariable -= num;
            },
            "setVariable" => modifier =>
            {
                if (modifier.commands.Count == 2)
                {
                    var list = !modifier.prefabInstanceOnly ? GameData.Current.FindObjectsWithTag(modifier.commands[1]) : GameData.Current.FindObjectsWithTag(modifier.reference, modifier.commands[1]);
                    if (list.IsEmpty())
                        return;

                    int num = modifier.GetInt(0, 0);

                    foreach (var beatmapObject in list)
                        beatmapObject.integerVariable = num;
                }
                else
                    modifier.reference.integerVariable = modifier.GetInt(0, 0);
            },
            "setVariableOther" => modifier =>
            {
                var list = !modifier.prefabInstanceOnly ? GameData.Current.FindObjectsWithTag(modifier.GetValue(1)) : GameData.Current.FindObjectsWithTag(modifier.reference, modifier.GetValue(1));
                if (list.IsEmpty())
                    return;

                int num = modifier.GetInt(0, 0);

                foreach (var beatmapObject in list)
                    beatmapObject.integerVariable = num;
            },
            "setVariableRandom" => modifier =>
            {
                if (modifier.commands.Count == 3)
                {
                    var list = !modifier.prefabInstanceOnly ? GameData.Current.FindObjectsWithTag(modifier.value) : GameData.Current.FindObjectsWithTag(modifier.reference, modifier.value);
                    if (list.IsEmpty())
                        return;

                    int min = modifier.GetInt(1, 0);
                    int max = modifier.GetInt(2, 0);

                    foreach (var beatmapObject in list)
                        beatmapObject.integerVariable = UnityEngine.Random.Range(min, max < 0 ? max - 1 : max + 1);
                }
                else
                {
                    var min = modifier.GetInt(0, 0);
                    var max = modifier.GetInt(1, 0);
                    modifier.reference.integerVariable = UnityEngine.Random.Range(min, max < 0 ? max - 1 : max + 1);
                }
            },
            "setVariableRandomOther" => modifier =>
            {
                var list = !modifier.prefabInstanceOnly ? GameData.Current.FindObjectsWithTag(modifier.value) : GameData.Current.FindObjectsWithTag(modifier.reference, modifier.value);
                if (list.IsEmpty())
                    return;

                int min = modifier.GetInt(1, 0);
                int max = modifier.GetInt(2, 0);

                foreach (var beatmapObject in list)
                    beatmapObject.integerVariable = UnityEngine.Random.Range(min, max < 0 ? max - 1 : max + 1);
            },
            "animateVariableOther" => modifier =>
            {
                var fromType = modifier.GetInt(1, 0);
                var fromAxis = modifier.GetInt(2, 0);
                var delay = modifier.GetFloat(3, 0);
                var multiply = modifier.GetFloat(4, 0);
                var offset = modifier.GetFloat(5, 0);
                var min = modifier.GetFloat(6, 0);
                var max = modifier.GetFloat(7, 0);
                var loop = modifier.GetFloat(8, 0);

                var list = !modifier.prefabInstanceOnly ? GameData.Current.FindObjectsWithTag(modifier.GetValue(0)) : GameData.Current.FindObjectsWithTag(modifier.reference, modifier.GetValue(0));
                if (list.IsEmpty())
                    return;

                for (int i = 0; i < list.Count; i++)
                {
                    var beatmapObject = list[i];
                    var time = AudioManager.inst.CurrentAudioSource.time;

                    fromType = Mathf.Clamp(fromType, 0, beatmapObject.events.Count);
                    fromAxis = Mathf.Clamp(fromAxis, 0, beatmapObject.events[fromType][0].values.Length);

                    if (!Updater.levelProcessor.converter.cachedSequences.TryGetValue(beatmapObject.id, out ObjectConverter.CachedSequences cachedSequence))
                        continue;

                    switch (fromType)
                    {
                        // To Type Position
                        // To Axis X
                        // From Type Position
                        case 0:
                            {
                                var sequence = cachedSequence.PositionSequence.Interpolate(time - beatmapObject.StartTime - delay);

                                beatmapObject.integerVariable = (int)Mathf.Clamp((fromAxis == 0 ? sequence.x % loop : fromAxis == 1 ? sequence.y % loop : sequence.z % loop) * multiply - offset, min, max);
                                break;
                            }
                        // To Type Position
                        // To Axis X
                        // From Type Scale
                        case 1:
                            {
                                var sequence = cachedSequence.ScaleSequence.Interpolate(time - beatmapObject.StartTime - delay);

                                beatmapObject.integerVariable = (int)Mathf.Clamp((fromAxis == 0 ? sequence.x % loop : sequence.y % loop) * multiply - offset, min, max);
                                break;
                            }
                        // To Type Position
                        // To Axis X
                        // From Type Rotation
                        case 2:
                            {
                                var sequence = cachedSequence.RotationSequence.Interpolate(time - beatmapObject.StartTime - delay) * multiply;

                                beatmapObject.integerVariable = (int)Mathf.Clamp((sequence % loop) - offset, min, max);
                                break;
                            }
                    }
                }
            },
            "clampVariable" => modifier =>
            {
                modifier.reference.integerVariable = Mathf.Clamp(modifier.reference.integerVariable, Parser.TryParse(modifier.commands.Count > 1 ? modifier.commands[1] : "1", 0), Parser.TryParse(modifier.commands.Count > 2 ? modifier.commands[2] : "1", 1));
            },
            "clampVariableOther" => modifier =>
            {
                var list = !modifier.prefabInstanceOnly ? GameData.Current.FindObjectsWithTag(modifier.commands[1]) : GameData.Current.FindObjectsWithTag(modifier.reference, modifier.commands[1]);

                var min = Parser.TryParse(modifier.commands.Count > 1 ? modifier.commands[1] : "1", 0);
                var max = Parser.TryParse(modifier.commands.Count > 2 ? modifier.commands[2] : "1", 1);

                if (!list.IsEmpty())
                    foreach (var bm in list)
                        bm.integerVariable = Mathf.Clamp(bm.integerVariable, min, max);
            },

            #endregion

            #region Enable / Disable

            // enable
            "enableObject" => modifier =>
            {
                if (Updater.TryGetObject(modifier.reference, out LevelObject levelObject) && levelObject.top)
                    levelObject.top.gameObject.SetActive(true);
            },
            "enableObjectTree" => modifier =>
            {
                if (modifier.GetValue(0) == "0")
                    modifier.SetValue(0, "False");

                if (!modifier.HasResult())
                {
                    var beatmapObject = Parser.TryParse(modifier.GetValue(0), true) ? modifier.reference : modifier.reference.GetParentChain().Last();

                    modifier.Result = beatmapObject.GetChildTree();
                }

                var list = modifier.GetResult<List<BeatmapObject>>();

                for (int i = 0; i < list.Count; i++)
                {
                    var beatmapObject = list[i];
                    if (Updater.TryGetObject(beatmapObject, out LevelObject levelObject) && levelObject.top)
                        levelObject.top.gameObject.SetActive(true);
                }
            },
            "enableObjectOther" => modifier =>
            {
                var list = !modifier.prefabInstanceOnly ? GameData.Current.FindObjectsWithTag(modifier.GetValue(0)) : GameData.Current.FindObjectsWithTag(modifier.reference, modifier.GetValue(0));

                if (!list.IsEmpty())
                    foreach (var beatmapObject in list)
                        if (Updater.TryGetObject(beatmapObject, out LevelObject levelObject) && levelObject.top)
                            levelObject.top.gameObject.SetActive(true);
            },
            "enableObjectTreeOther" => modifier =>
            {
                if (!modifier.HasResult())
                {
                    var beatmapObjects = !modifier.prefabInstanceOnly ? GameData.Current.FindObjectsWithTag(modifier.GetValue(1)) : GameData.Current.FindObjectsWithTag(modifier.reference, modifier.GetValue(1));

                    var resultList = new List<BeatmapObject>();
                    foreach (var bm in beatmapObjects)
                    {
                        var beatmapObject = Parser.TryParse(modifier.GetValue(0), true) ? bm : bm.GetParentChain().Last();
                        resultList.AddRange(beatmapObject.GetChildTree());
                    }

                    modifier.Result = resultList;
                }

                var list = modifier.GetResult<List<BeatmapObject>>();

                for (int i = 0; i < list.Count; i++)
                {
                    var beatmapObject = list[i];
                    if (Updater.TryGetObject(beatmapObject, out LevelObject levelObject) && levelObject.top)
                        levelObject.top.gameObject.SetActive(true);
                }
            },

            // disable
            "disableObject" => modifier =>
            {
                if (Updater.TryGetObject(modifier.reference, out LevelObject levelObject) && levelObject.top)
                    levelObject.top.gameObject.SetActive(false);
            },
            "disableObjectTree" => modifier =>
            {
                if (modifier.GetValue(0) == "0")
                    modifier.SetValue(0, "False");

                if (!modifier.HasResult())
                {
                    var beatmapObject = Parser.TryParse(modifier.GetValue(0), true) ? modifier.reference : modifier.reference.GetParentChain().Last();

                    modifier.Result = beatmapObject.GetChildTree();
                }

                var list = modifier.GetResult<List<BeatmapObject>>();

                for (int i = 0; i < list.Count; i++)
                {
                    var beatmapObject = list[i];
                    if (Updater.TryGetObject(beatmapObject, out LevelObject levelObject) && levelObject.top)
                        levelObject.top.gameObject.SetActive(false);
                }
            },
            "disableObjectOther" => modifier =>
            {
                var list = !modifier.prefabInstanceOnly ? GameData.Current.FindObjectsWithTag(modifier.GetValue(0)) : GameData.Current.FindObjectsWithTag(modifier.reference, modifier.GetValue(0));

                if (!list.IsEmpty())
                    foreach (var beatmapObject in list)
                        if (Updater.TryGetObject(beatmapObject, out LevelObject levelObject) && levelObject.top)
                            levelObject.top.gameObject.SetActive(false);
            },
            "disableObjectTreeOther" => modifier =>
            {
                if (!modifier.HasResult())
                {
                    var beatmapObjects = !modifier.prefabInstanceOnly ? GameData.Current.FindObjectsWithTag(modifier.GetValue(1)) : GameData.Current.FindObjectsWithTag(modifier.reference, modifier.GetValue(1));

                    var resultList = new List<BeatmapObject>();
                    foreach (var bm in beatmapObjects)
                    {
                        var beatmapObject = Parser.TryParse(modifier.GetValue(0), true) ? bm : bm.GetParentChain().Last();
                        resultList.AddRange(beatmapObject.GetChildTree());
                    }

                    modifier.Result = resultList;
                }

                var list = modifier.GetResult<List<BeatmapObject>>();

                for (int i = 0; i < list.Count; i++)
                {
                    var beatmapObject = list[i];
                    if (Updater.TryGetObject(beatmapObject, out LevelObject levelObject) && levelObject.top)
                        levelObject.top.gameObject.SetActive(false);
                }
            },

            #endregion

            #region JSON

            "saveFloat" => modifier =>
            {
                if (CoreHelper.InEditorPreview)
                    SaveProgress(modifier.GetValue(1), modifier.GetValue(2), modifier.GetValue(3), modifier.GetFloat(0, 0f));
            },
            "saveString" => modifier =>
            {
                if (CoreHelper.InEditorPreview)
                    SaveProgress(modifier.GetValue(1), modifier.GetValue(2), modifier.GetValue(3), modifier.GetValue(0));
            },
            "saveText" => modifier =>
            {
                if (CoreHelper.InEditorPreview && modifier.reference && Updater.TryGetObject(modifier.reference, out LevelObject levelObject) && levelObject.visualObject is TextObject textObject)
                    SaveProgress(modifier.GetValue(1), modifier.GetValue(2), modifier.GetValue(3), textObject.textMeshPro.text);
            },
            "saveVariable" => modifier =>
            {
                if (CoreHelper.InEditorPreview && modifier.reference)
                    SaveProgress(modifier.GetValue(1), modifier.GetValue(2), modifier.GetValue(3), modifier.reference.integerVariable);
            },
            "loadVariable" => modifier =>
            {
                var path = RTFile.CombinePaths(RTFile.ApplicationDirectory, "profile", modifier.GetValue(1) + FileFormat.SES.Dot());
                if (!RTFile.FileExists(path))
                    return;

                string json = RTFile.ReadFromFile(path);

                if (string.IsNullOrEmpty(json))
                    return;

                var jn = JSON.Parse(json);

                var fjn = jn[modifier.GetValue(2)][modifier.GetValue(3)]["float"];
                if (!string.IsNullOrEmpty(fjn) && float.TryParse(fjn, out float eq))
                    modifier.reference.integerVariable = (int)eq;
            },
            "loadVariableOther" => modifier =>
            {
                var path = RTFile.CombinePaths(RTFile.ApplicationDirectory, "profile", modifier.GetValue(1) + FileFormat.SES.Dot());
                if (!RTFile.FileExists(path))
                    return;

                string json = RTFile.ReadFromFile(path);

                if (string.IsNullOrEmpty(json))
                    return;

                var jn = JSON.Parse(json);
                var list = !modifier.prefabInstanceOnly ? GameData.Current.FindObjectsWithTag(modifier.value) : GameData.Current.FindObjectsWithTag(modifier.reference, modifier.value);
                var fjn = jn[modifier.GetValue(2)][modifier.GetValue(3)]["float"];

                if (list.Count > 0 && !string.IsNullOrEmpty(fjn) && float.TryParse(fjn, out float eq))
                    foreach (var bm in list)
                        bm.integerVariable = (int)eq;
            },

            #endregion

            #region Reactive

            "reactivePos" => modifier =>
            {
                var val = modifier.GetFloat(0, 0f);
                var sampleX = modifier.GetInt(1, 0);
                var sampleY = modifier.GetInt(2, 0);
                var intensityX = modifier.GetFloat(3, 0f);
                var intensityY = modifier.GetFloat(4, 0f);

                if (modifier.reference && Updater.TryGetObject(modifier.reference, out LevelObject levelObject))
                {
                    float reactivePositionX = Updater.GetSample(sampleX, intensityX * val);
                    float reactivePositionY = Updater.GetSample(sampleY, intensityY * val);

                    levelObject.visualObject.SetOrigin(new Vector3(modifier.reference.origin.x + reactivePositionX, modifier.reference.origin.y + reactivePositionY, modifier.reference.Depth * 0.1f));
                }
            },
            "reactiveSca" => modifier =>
            {
                var val = modifier.GetFloat(0, 0f);
                var sampleX = modifier.GetInt(1, 0);
                var sampleY = modifier.GetInt(2, 0);
                var intensityX = modifier.GetFloat(3, 0f);
                var intensityY = modifier.GetFloat(4, 0f);

                if (modifier.reference && Updater.TryGetObject(modifier.reference, out LevelObject levelObject))
                {
                    float reactiveScaleX = Updater.GetSample(sampleX, intensityX * val);
                    float reactiveScaleY = Updater.GetSample(sampleY, intensityY * val);

                    levelObject.visualObject.SetScaleOffset(new Vector2(1f + reactiveScaleX, 1f + reactiveScaleY));
                }
            },
            "reactiveRot" => modifier =>
            {
                if (modifier.reference && Updater.TryGetObject(modifier.reference, out LevelObject levelObject) && levelObject.visualObject.gameObject)
                    levelObject.visualObject.SetRotationOffset(Updater.GetSample(modifier.GetInt(1, 0), modifier.GetFloat(0, 0f)));
            },
            "reactiveCol" => modifier =>
            {
                if (modifier.reference && Updater.TryGetObject(modifier.reference, out LevelObject levelObject) && levelObject.visualObject.renderer)
                    levelObject.visualObject.SetColor(levelObject.visualObject.GetPrimaryColor() + ThemeManager.inst.Current.GetObjColor(modifier.GetInt(2, 0)) * Updater.GetSample(modifier.GetInt(1, 0), modifier.GetFloat(0, 0f)));
            },
            "reactiveColLerp" => modifier =>
            {
                if (modifier.reference && Updater.TryGetObject(modifier.reference, out LevelObject levelObject) && levelObject.visualObject.renderer)
                    levelObject.visualObject.SetColor(RTMath.Lerp(levelObject.visualObject.GetPrimaryColor(), ThemeManager.inst.Current.GetObjColor(modifier.GetInt(2, 0)), Updater.GetSample(modifier.GetInt(1, 0), modifier.GetFloat(0, 0f))));
            },
            "reactivePosChain" => modifier =>
            {
                if (!modifier.reference)
                    return;

                var val = modifier.GetFloat(0, 0f);
                var sampleX = modifier.GetInt(1, 0);
                var sampleY = modifier.GetInt(2, 0);
                var intensityX = modifier.GetFloat(3, 0f);
                var intensityY = modifier.GetFloat(4, 0f);

                float reactivePositionX = Updater.GetSample(sampleX, intensityX * val);
                float reactivePositionY = Updater.GetSample(sampleY, intensityY * val);

                modifier.reference.reactivePositionOffset = new Vector3(reactivePositionX, reactivePositionY);
            },
            "reactiveScaChain" => modifier =>
            {
                if (!modifier.reference)
                    return;

                var val = modifier.GetFloat(0, 0f);
                var sampleX = modifier.GetInt(1, 0);
                var sampleY = modifier.GetInt(2, 0);
                var intensityX = modifier.GetFloat(3, 0f);
                var intensityY = modifier.GetFloat(4, 0f);

                float reactiveScaleX = Updater.GetSample(sampleX, intensityX * val);
                float reactiveScaleY = Updater.GetSample(sampleY, intensityY * val);

                modifier.reference.reactiveScaleOffset = new Vector3(reactiveScaleX, reactiveScaleY, 1f);
            },
            "reactiveRotChain" => modifier =>
            {
                if (modifier.reference)
                    modifier.reference.reactiveRotationOffset = Updater.GetSample(modifier.GetInt(1, 0), modifier.GetFloat(0, 0f));
            },

            #endregion

            #region Events

            "eventOffset" => modifier =>
            {
                if (RTEventManager.inst && RTEventManager.inst.offsets != null)
                    RTEventManager.inst.SetOffset(modifier.GetInt(1, 0), modifier.GetInt(2, 0), modifier.GetFloat(0, 1f));
            },
            "eventOffsetVariable" => modifier =>
            {
                if (RTEventManager.inst && RTEventManager.inst.offsets != null)
                    RTEventManager.inst.SetOffset(modifier.GetInt(1, 0), modifier.GetInt(2, 0), modifier.reference.integerVariable * modifier.GetFloat(0, 1f));
            },
            "eventOffsetMath" => modifier =>
            {
                if (RTEventManager.inst && RTEventManager.inst.offsets != null)
                    RTEventManager.inst.SetOffset(modifier.GetInt(1, 0), modifier.GetInt(2, 0), RTMath.Parse(modifier.GetValue(0), modifier.reference.GetObjectVariables()));
            },
            "eventOffsetAnimate" => modifier =>
            {
                if (modifier.constant || !RTEventManager.inst || RTEventManager.inst.offsets == null)
                    return;

                string easing = modifier.GetValue(4);
                if (int.TryParse(modifier.GetValue(4), out int e) && e >= 0 && e < DataManager.inst.AnimationList.Count)
                    easing = DataManager.inst.AnimationList[e].Name;

                var list = RTEventManager.inst.offsets;

                var eventType = modifier.GetInt(1, 0);
                var indexValue = modifier.GetInt(2, 0);

                if (eventType < list.Count && indexValue < list[eventType].Count)
                {
                    var value = modifier.GetBool(5, false) ? list[eventType][indexValue] + modifier.GetFloat(0, 0f) : modifier.GetFloat(0, 0f);

                    var animation = new RTAnimation("Event Offset Animation");
                    animation.animationHandlers = new List<AnimationHandlerBase>
                    {
                        new AnimationHandler<float>(new List<IKeyframe<float>>
                        {
                            new FloatKeyframe(0f, list[eventType][indexValue], Ease.Linear),
                            new FloatKeyframe(modifier.GetFloat(3, 1f), value, Ease.HasEaseFunction(easing) ? Ease.GetEaseFunction(easing) : Ease.Linear),
                        }, x => RTEventManager.inst.SetOffset(eventType, indexValue, x), interpolateOnComplete: true)
                    };
                    animation.onComplete = () => AnimationManager.inst.Remove(animation.id);
                    AnimationManager.inst.Play(animation);
                }
            },
            "eventOffsetCopyAxis" => modifier =>
            {
                if (!RTEventManager.inst || RTEventManager.inst.offsets == null)
                    return;

                var fromType = modifier.GetInt(1, 0);
                var fromAxis = modifier.GetInt(2, 0);
                var toType = modifier.GetInt(3, 0);
                var toAxis = modifier.GetInt(4, 0);
                var delay = modifier.GetFloat(5, 0f);
                var multiply = modifier.GetFloat(6, 0f);
                var offset = modifier.GetFloat(7, 0f);
                var min = modifier.GetFloat(8, 0f);
                var max = modifier.GetFloat(9, 0f);
                var loop = modifier.GetFloat(10, 0f);
                var useVisual = modifier.GetBool(11, false);

                var time = AudioManager.inst.CurrentAudioSource.time;

                fromType = Mathf.Clamp(fromType, 0, modifier.reference.events.Count - 1);
                fromAxis = Mathf.Clamp(fromAxis, 0, modifier.reference.events[fromType][0].values.Length - 1);
                toType = Mathf.Clamp(toType, 0, RTEventManager.inst.offsets.Count - 1);
                toAxis = Mathf.Clamp(toAxis, 0, RTEventManager.inst.offsets[toType].Count - 1);

                if (!useVisual && Updater.levelProcessor.converter.cachedSequences.TryGetValue(modifier.reference.id, out ObjectConverter.CachedSequences cachedSequences))
                    RTEventManager.inst.SetOffset(toType, toAxis, fromType switch
                    {
                        0 => Mathf.Clamp((cachedSequences.PositionSequence.Interpolate(time - modifier.reference.StartTime - delay).At(fromAxis) - offset) * multiply % loop, min, max),
                        1 => Mathf.Clamp((cachedSequences.ScaleSequence.Interpolate(time - modifier.reference.StartTime - delay).At(fromAxis) - offset) * multiply % loop, min, max),
                        2 => Mathf.Clamp((cachedSequences.RotationSequence.Interpolate(time - modifier.reference.StartTime - delay) - offset) * multiply % loop, min, max),
                        _ => 0f,
                    });
                else if (Updater.TryGetObject(modifier.reference, out LevelObject levelObject) && levelObject.visualObject != null && levelObject.visualObject.gameObject)
                    RTEventManager.inst.SetOffset(toType, toAxis, Mathf.Clamp((levelObject.visualObject.gameObject.transform.GetVector(fromType).At(fromAxis) - offset) * multiply % loop, min, max));
            },
            "vignetteTracksPlayer" => modifier =>
            {
                var players = PlayerManager.Players;
                if (players.IsEmpty())
                    return;

                var player = players[0].Player;

                if (!player || !player.rb)
                    return;

                var cameraToViewportPoint = Camera.main.WorldToViewportPoint(player.rb.position);
                RTEventManager.inst.SetOffset(7, 4, cameraToViewportPoint.x);
                RTEventManager.inst.SetOffset(7, 5, cameraToViewportPoint.y);
            },
            "lensTracksPlayer" => modifier =>
            {
                var players = PlayerManager.Players;
                if (players.IsEmpty())
                    return;

                var player = players[0].Player;

                if (!player || !player.rb)
                    return;

                var cameraToViewportPoint = Camera.main.WorldToViewportPoint(player.rb.position);
                RTEventManager.inst.SetOffset(8, 1, cameraToViewportPoint.x - 0.5f);
                RTEventManager.inst.SetOffset(8, 2, cameraToViewportPoint.y - 0.5f);
            },

            #endregion

            #region Color

            // color
            "addColor" => modifier =>
            {
                if (!modifier.reference || !Updater.TryGetObject(modifier.reference, out LevelObject levelObject))
                    return;

                var multiply = modifier.GetFloat(0, 1f);
                var index = modifier.GetInt(1, 0);
                var hue = modifier.GetFloat(2, 0f);
                var sat = modifier.GetFloat(3, 0f);
                var val = modifier.GetFloat(4, 0f);

                var color = CoreHelper.ChangeColorHSV(ThemeManager.inst.Current.GetObjColor(index), hue, sat, val) * multiply;
                //if (levelObject.isGradient)
                //    levelObject.gradientObject.SetColor(levelObject.gradientObject.GetPrimaryColor() + color, levelObject.gradientObject.GetSecondaryColor() + color);
                //else
                levelObject.visualObject.SetColor(levelObject.visualObject.GetPrimaryColor() + color);
            },
            "addColorOther" => modifier =>
            {
                var list = !modifier.prefabInstanceOnly ? GameData.Current.FindObjectsWithTag(modifier.GetValue(1)) : GameData.Current.FindObjectsWithTag(modifier.reference, modifier.GetValue(1));

                if (list.IsEmpty())
                    return;

                var multiply = modifier.GetFloat(0, 0f);
                var index = modifier.GetInt(2, 0);
                var hue = modifier.GetFloat(3, 0f);
                var sat = modifier.GetFloat(4, 0f);
                var val = modifier.GetFloat(5, 0f);

                foreach (var bm in list)
                {
                    if (!Updater.TryGetObject(bm, out LevelObject levelObject))
                        continue;

                    var color = CoreHelper.ChangeColorHSV(ThemeManager.inst.Current.GetObjColor(index), hue, sat, val) * multiply;
                    //if (levelObject.isGradient)
                    //    levelObject.gradientObject.SetColor(levelObject.gradientObject.GetPrimaryColor() + color, levelObject.gradientObject.GetSecondaryColor() + color);
                    //else
                    levelObject.visualObject.SetColor(levelObject.visualObject.GetPrimaryColor() + color);
                }
            },
            "lerpColor" => modifier =>
            {
                if (!modifier.reference || !Updater.TryGetObject(modifier.reference, out LevelObject levelObject) || levelObject.visualObject == null)
                    return;

                var multiply = modifier.GetFloat(0, 0f);
                var index = modifier.GetInt(1, 0);
                var hue = modifier.GetFloat(2, 0f);
                var sat = modifier.GetFloat(3, 0f);
                var val = modifier.GetFloat(4, 0f);

                levelObject.visualObject.SetColor(RTMath.Lerp(levelObject.visualObject.GetPrimaryColor(), CoreHelper.ChangeColorHSV(ThemeManager.inst.Current.GetObjColor(index), hue, sat, val), multiply));
            },
            "lerpColorOther" => modifier =>
            {
                var list = !modifier.prefabInstanceOnly ? GameData.Current.FindObjectsWithTag(modifier.GetValue(1)) : GameData.Current.FindObjectsWithTag(modifier.reference, modifier.GetValue(1));

                if (list.IsEmpty())
                    return;

                var multiply = modifier.GetFloat(0, 0f);
                var index = modifier.GetInt(2, 0);
                var hue = modifier.GetFloat(3, 0f);
                var sat = modifier.GetFloat(4, 0f);
                var val = modifier.GetFloat(5, 0f);

                var color = CoreHelper.ChangeColorHSV(ThemeManager.inst.Current.GetObjColor(index), hue, sat, val);
                for (int i = 0; i < list.Count; i++)
                {
                    var bm = list[i];
                    if (!Updater.TryGetObject(bm, out LevelObject levelObject) || levelObject.visualObject == null)
                        continue;

                    levelObject.visualObject.SetColor(RTMath.Lerp(levelObject.visualObject.GetPrimaryColor(), color, multiply));
                }
            },
            "addColorPlayerDistance" => modifier =>
            {
                if (!modifier.reference || !Updater.TryGetObject(modifier.reference, out LevelObject levelObject) || !levelObject.visualObject.gameObject)
                    return;

                var player = PlayerManager.GetClosestPlayer(levelObject.visualObject.gameObject.transform.position);

                if (!player.Player || !player.Player.rb)
                    return;

                var offset = modifier.GetFloat(0, 0f);
                var index = modifier.GetInt(1, 0);
                var multiply = modifier.GetFloat(2, 0);

                var distance = Vector2.Distance(player.Player.rb.transform.position, levelObject.visualObject.gameObject.transform.position);

                levelObject.visualObject.SetColor(levelObject.visualObject.GetPrimaryColor() + ThemeManager.inst.Current.GetObjColor(index) * -(distance * multiply - offset));
            },
            "lerpColorPlayerDistance" => modifier =>
            {
                if (!modifier.reference || !Updater.TryGetObject(modifier.reference, out LevelObject levelObject) || !levelObject.visualObject.gameObject)
                    return;

                var player = PlayerManager.GetClosestPlayer(levelObject.visualObject.gameObject.transform.position);

                if (!player.Player || !player.Player.rb)
                    return;

                var offset = modifier.GetFloat(0, 0f);
                var index = modifier.GetInt(1, 0);
                var multiply = modifier.GetFloat(2, 0f);
                var opacity = modifier.GetFloat(3, 0f);
                var hue = modifier.GetFloat(4, 0f);
                var sat = modifier.GetFloat(5, 0f);
                var val = modifier.GetFloat(6, 0f);

                var distance = Vector2.Distance(player.Player.rb.transform.position, levelObject.visualObject.gameObject.transform.position);

                levelObject.visualObject.SetColor(Color.Lerp(levelObject.visualObject.GetPrimaryColor(),
                                LSColors.fadeColor(CoreHelper.ChangeColorHSV(ThemeManager.inst.Current.GetObjColor(index), hue, sat, val), opacity),
                                -(distance * multiply - offset)));
            },

            // opacity
            "setAlpha" => modifier =>
            {
                if (modifier.reference && Updater.TryGetObject(modifier.reference, out LevelObject levelObject))
                    levelObject.visualObject.SetColor(LSColors.fadeColor(levelObject.visualObject.GetPrimaryColor(), modifier.GetFloat(0, 1f)));
            },
            "setOpacity" => modifier =>
            {
                if (modifier.reference && Updater.TryGetObject(modifier.reference, out LevelObject levelObject))
                    levelObject.visualObject.SetColor(LSColors.fadeColor(levelObject.visualObject.GetPrimaryColor(), modifier.GetFloat(0, 1f)));
            },
            "setAlphaOther" => modifier =>
            {
                var list = !modifier.prefabInstanceOnly ? GameData.Current.FindObjectsWithTag(modifier.GetValue(1)) : GameData.Current.FindObjectsWithTag(modifier.reference, modifier.GetValue(1));

                if (list.IsEmpty())
                    return;

                var num = modifier.GetFloat(0, 1f);
                foreach (var bm in list)
                {
                    if (!Updater.TryGetObject(bm, out LevelObject levelObject))
                        continue;

                    levelObject.visualObject.SetColor(LSColors.fadeColor(levelObject.visualObject.GetPrimaryColor(), num));
                }
            },
            "setOpacityOther" => modifier =>
            {
                var list = !modifier.prefabInstanceOnly ? GameData.Current.FindObjectsWithTag(modifier.GetValue(1)) : GameData.Current.FindObjectsWithTag(modifier.reference, modifier.GetValue(1));

                if (list.IsEmpty())
                    return;

                var num = modifier.GetFloat(0, 1f);
                foreach (var bm in list)
                {
                    if (!Updater.TryGetObject(bm, out LevelObject levelObject))
                        continue;

                    levelObject.visualObject.SetColor(LSColors.fadeColor(levelObject.visualObject.GetPrimaryColor(), num));
                }
            },

            // copy
            "copyColor" => modifier =>
            {
                if (!GameData.Current.TryFindObjectWithTag(modifier, modifier.GetValue(0), out BeatmapObject beatmapObject))
                    return;

                if (!Updater.TryGetObject(beatmapObject, out LevelObject otherLevelObject) ||
                    !Updater.TryGetObject(modifier.reference, out LevelObject levelObject))
                    return;

                CopyColor(levelObject, otherLevelObject, modifier.GetBool(1, true), modifier.GetBool(2, true));
            },
            "copyColorOther" => modifier =>
            {
                var list = !modifier.prefabInstanceOnly ? GameData.Current.FindObjectsWithTag(modifier.GetValue(0)) : GameData.Current.FindObjectsWithTag(modifier.reference, modifier.GetValue(0));

                if (list.IsEmpty() || !Updater.TryGetObject(modifier.reference, out LevelObject levelObject))
                    return;

                var applyColor1 = modifier.GetBool(1, true);
                var applyColor2 = modifier.GetBool(2, true);

                foreach (var bm in list)
                {
                    if (!Updater.TryGetObject(bm, out LevelObject otherLevelObject))
                        continue;

                    CopyColor(otherLevelObject, levelObject, applyColor1, applyColor2);
                }
            },
            "applyColorGroup" => modifier =>
            {
                var list = !modifier.prefabInstanceOnly ? GameData.Current.FindObjectsWithTag(modifier.GetValue(0)) : GameData.Current.FindObjectsWithTag(modifier.reference, modifier.GetValue(0));

                if (list.IsEmpty() || !Updater.levelProcessor.converter.cachedSequences.TryGetValue(modifier.reference.id, out ObjectConverter.CachedSequences cachedSequence))
                    return;

                var beatmapObject = modifier.reference;
                var time = Updater.CurrentTime - beatmapObject.StartTime;
                Color color;
                Color secondColor;
                {
                    var prevKFIndex = beatmapObject.events[3].FindLastIndex(x => x.time < time);

                    if (prevKFIndex < 0)
                        return;

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

                    color = LSColors.fadeColor(color, -(lerp - 1f));

                    var lerpHue = RTMath.Lerp(prevKF.values[2], nextKF.values[2], easing);
                    var lerpSat = RTMath.Lerp(prevKF.values[3], nextKF.values[3], easing);
                    var lerpVal = RTMath.Lerp(prevKF.values[4], nextKF.values[4], easing);

                    if (float.IsNaN(lerpHue))
                        lerpHue = nextKF.values[2];
                    if (float.IsNaN(lerpSat))
                        lerpSat = nextKF.values[3];
                    if (float.IsNaN(lerpVal))
                        lerpVal = nextKF.values[4];

                    color = CoreHelper.ChangeColorHSV(color, lerpHue, lerpSat, lerpVal);

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

                    secondColor = LSColors.fadeColor(secondColor, -(lerp - 1f));

                    lerpHue = RTMath.Lerp(prevKF.values[7], nextKF.values[7], easing);
                    lerpSat = RTMath.Lerp(prevKF.values[8], nextKF.values[8], easing);
                    lerpVal = RTMath.Lerp(prevKF.values[9], nextKF.values[9], easing);

                    if (float.IsNaN(lerpHue))
                        lerpHue = nextKF.values[7];
                    if (float.IsNaN(lerpSat))
                        lerpSat = nextKF.values[8];
                    if (float.IsNaN(lerpVal))
                        lerpVal = nextKF.values[9];

                    secondColor = CoreHelper.ChangeColorHSV(color, lerpHue, lerpSat, lerpVal);
                } // assign

                var type = modifier.GetInt(1, 0);
                var axis = modifier.GetInt(2, 0);

                var isEmpty = modifier.reference.objectType == BeatmapObject.ObjectType.Empty;

                float t = !isEmpty ? type switch
                {
                    0 => axis == 0 ? cachedSequence.PositionSequence.Value.x : axis == 1 ? cachedSequence.PositionSequence.Value.y : cachedSequence.PositionSequence.Value.z,
                    1 => axis == 0 ? cachedSequence.ScaleSequence.Value.x : cachedSequence.ScaleSequence.Value.y,
                    2 => cachedSequence.RotationSequence.Value,
                    _ => 0f
                } : type switch
                {
                    0 => axis == 0 ? cachedSequence.PositionSequence.Interpolate(time).x : axis == 1 ? cachedSequence.PositionSequence.Interpolate(time).y : cachedSequence.PositionSequence.Interpolate(time).z,
                    1 => axis == 0 ? cachedSequence.ScaleSequence.Interpolate(time).x : cachedSequence.ScaleSequence.Interpolate(time).y,
                    2 => cachedSequence.RotationSequence.Interpolate(time),
                    _ => 0f
                };

                foreach (var bm in list)
                {
                    if (!Updater.TryGetObject(bm, out LevelObject otherLevelObject))
                        continue;

                    if (!otherLevelObject.visualObject.isGradient)
                        otherLevelObject.visualObject.SetColor(Color.Lerp(otherLevelObject.visualObject.GetPrimaryColor(), color, t));
                    else if (otherLevelObject.visualObject is SolidObject solidObject)
                    {
                        var colors = solidObject.GetColors();
                        solidObject.SetColor(Color.Lerp(colors.startColor, color, t), Color.Lerp(colors.endColor, secondColor, t));
                    }
                }
            },

            // hex code
            "setColorHex" => modifier =>
            {
                if (!modifier.reference || !Updater.TryGetObject(modifier.reference, out LevelObject levelObject))
                    return;

                if (!levelObject.visualObject.isGradient)
                {
                    var color = levelObject.visualObject.GetPrimaryColor();
                    levelObject.visualObject.SetColor(string.IsNullOrEmpty(modifier.value) ? color : LSColors.fadeColor(LSColors.HexToColorAlpha(modifier.GetValue(0)), color.a));
                }
                else if (levelObject.visualObject is SolidObject solidObject)
                {
                    var colors = solidObject.GetColors();
                    solidObject.SetColor(
                        string.IsNullOrEmpty(modifier.GetValue(0)) ? colors.startColor : LSColors.fadeColor(LSColors.HexToColorAlpha(modifier.GetValue(0)), colors.startColor.a),
                        string.IsNullOrEmpty(modifier.GetValue(1)) ? colors.endColor : LSColors.fadeColor(LSColors.HexToColorAlpha(modifier.GetValue(1)), colors.endColor.a));
                }
            },
            "setColorHexOther" => modifier =>
            {
                var list = !modifier.prefabInstanceOnly ? GameData.Current.FindObjectsWithTag(modifier.GetValue(1)) : GameData.Current.FindObjectsWithTag(modifier.reference, modifier.GetValue(1));

                if (list.IsEmpty())
                    return;

                foreach (var bm in list)
                {
                    if (!Updater.TryGetObject(bm, out LevelObject levelObject))
                        continue;

                    if (!levelObject.visualObject.isGradient)
                    {
                        var color = levelObject.visualObject.GetPrimaryColor();
                        levelObject.visualObject.SetColor(string.IsNullOrEmpty(modifier.value) ? color : LSColors.fadeColor(LSColors.HexToColorAlpha(modifier.GetValue(0)), color.a));
                    }
                    else if (levelObject.visualObject is SolidObject solidObject)
                    {
                        var colors = solidObject.GetColors();
                        solidObject.SetColor(
                            string.IsNullOrEmpty(modifier.GetValue(0)) ? colors.startColor : LSColors.fadeColor(LSColors.HexToColorAlpha(modifier.GetValue(0)), colors.startColor.a),
                            string.IsNullOrEmpty(modifier.GetValue(2)) ? colors.endColor : LSColors.fadeColor(LSColors.HexToColorAlpha(modifier.GetValue(2)), colors.endColor.a));
                    }
                }
            },

            #endregion

            // todo: figure out how to get actorFrameTexture to work
            // todo: add polygon modifier stuff
            #region Shape

            "actorFrameTexture" => modifier =>
            {
                if (modifier.reference.ShapeType != ShapeType.Image || !Updater.TryGetObject(modifier.reference, out LevelObject levelObject) || levelObject.visualObject is not ImageObject imageObject)
                    return;

                var camera = modifier.GetInt(0, 0) == 0 ? EventManager.inst.cam : EventManager.inst.camPer;

                var frame = SpriteHelper.CaptureFrame(camera, modifier.GetInt(1, 512), modifier.GetInt(2, 512), modifier.GetFloat(3, 0f), modifier.GetFloat(4, 0f));

                ((SpriteRenderer)imageObject.renderer).sprite = frame;
            },

            // image
            "setImage" => modifier =>
            {
                if (modifier.constant || modifier.reference.ShapeType != ShapeType.Image || !Updater.TryGetObject(modifier.reference, out LevelObject levelObject) || levelObject.visualObject is not ImageObject imageObject)
                    return;

                var path = RTFile.CombinePaths(RTFile.BasePath, modifier.GetValue(0));

                if (!RTFile.FileExists(path))
                {
                    imageObject.SetDefaultSprite();
                    return;
                }

                CoroutineHelper.StartCoroutine(AlephNetwork.DownloadImageTexture("file://" + path, imageObject.SetTexture, imageObject.SetDefaultSprite));
            },
            "setImageOther" => modifier =>
            {
                if (modifier.constant)
                    return;

                var list = !modifier.prefabInstanceOnly ? GameData.Current.FindObjectsWithTag(modifier.GetValue(1)) : GameData.Current.FindObjectsWithTag(modifier.reference, modifier.GetValue(1));

                if (list.IsEmpty())
                    return;

                var value = modifier.GetValue(0);

                foreach (var bm in list)
                {
                    if (bm.ShapeType != ShapeType.Image || !bm.levelObject || bm.levelObject.visualObject is not ImageObject imageObject)
                        continue;

                    var path = RTFile.CombinePaths(RTFile.BasePath, value);

                    if (!RTFile.FileExists(path))
                    {
                        imageObject.SetDefaultSprite();
                        continue;
                    }

                    CoroutineHelper.StartCoroutine(AlephNetwork.DownloadImageTexture("file://" + path, imageObject.SetTexture, imageObject.SetDefaultSprite));
                }
            },

            // text (pain)
            "setText" => modifier =>
            {
                if (modifier.reference.ShapeType != ShapeType.Text ||
                !Updater.TryGetObject(modifier.reference, out LevelObject levelObject) ||
                levelObject.visualObject is not TextObject textObject)
                    return;

                if (modifier.constant)
                    textObject.SetText(modifier.GetValue(0));
                else
                    textObject.text = modifier.GetValue(0);
            },
            "setTextOther" => modifier =>
            {
                var list = !modifier.prefabInstanceOnly ? GameData.Current.FindObjectsWithTag(modifier.GetValue(1)) : GameData.Current.FindObjectsWithTag(modifier.reference, modifier.GetValue(1));

                if (list.IsEmpty())
                    return;

                foreach (var bm in list)
                {
                    if (bm.ShapeType != ShapeType.Text || !Updater.TryGetObject(bm, out LevelObject levelObject) || levelObject.visualObject is not TextObject textObject)
                        continue;

                    if (modifier.constant)
                        textObject.SetText(modifier.GetValue(0));
                    else
                        textObject.text = modifier.GetValue(0);
                }
            },
            "addText" => modifier =>
            {
                if (modifier.reference.ShapeType != ShapeType.Text ||
                !Updater.TryGetObject(modifier.reference, out LevelObject levelObject) ||
                levelObject.visualObject is not TextObject textObject)
                    return;

                if (modifier.constant)
                    textObject.SetText(textObject.textMeshPro.text + modifier.GetValue(0));
                else
                    textObject.text += modifier.GetValue(0);
            },
            "addTextOther" => modifier =>
            {
                var list = !modifier.prefabInstanceOnly ? GameData.Current.FindObjectsWithTag(modifier.GetValue(1)) : GameData.Current.FindObjectsWithTag(modifier.reference, modifier.GetValue(1));

                if (list.IsEmpty())
                    return;

                foreach (var bm in list)
                {
                    if (bm.ShapeType != ShapeType.Text || !Updater.TryGetObject(bm, out LevelObject levelObject) || levelObject.visualObject is not TextObject textObject)
                        continue;

                    if (modifier.constant)
                        textObject.SetText(textObject.textMeshPro.text + modifier.GetValue(0));
                    else
                        textObject.text += modifier.GetValue(0);
                }
            },
            "removeText" => modifier =>
            {
                if (modifier.reference.ShapeType != ShapeType.Text ||
                !Updater.TryGetObject(modifier.reference, out LevelObject levelObject) ||
                levelObject.visualObject is not TextObject textObject)
                    return;

                string text = string.IsNullOrEmpty(textObject.textMeshPro.text) ? string.Empty :
                    textObject.textMeshPro.text.Substring(0, textObject.textMeshPro.text.Length - Mathf.Clamp(modifier.GetInt(0, 1), 0, textObject.textMeshPro.text.Length - 1));

                if (modifier.constant)
                    textObject.SetText(text);
                else
                    textObject.text = text;
            },
            "removeTextOther" => modifier =>
            {
                var list = !modifier.prefabInstanceOnly ? GameData.Current.FindObjectsWithTag(modifier.GetValue(1)) : GameData.Current.FindObjectsWithTag(modifier.reference, modifier.GetValue(1));

                if (list.IsEmpty())
                    return;

                var remove = modifier.GetInt(0, 1);

                foreach (var bm in list)
                {
                    if (bm.ShapeType != ShapeType.Text || !Updater.TryGetObject(bm, out LevelObject levelObject) || levelObject.visualObject is not TextObject textObject)
                        continue;

                    string text = string.IsNullOrEmpty(textObject.textMeshPro.text) ? "" :
                        textObject.textMeshPro.text.Substring(0, textObject.textMeshPro.text.Length - Mathf.Clamp(remove, 0, textObject.textMeshPro.text.Length - 1));

                    if (modifier.constant)
                        textObject.SetText(text);
                    else
                        textObject.text = text;
                }
            },
            "removeTextAt" => modifier =>
            {
                if (modifier.reference.ShapeType != ShapeType.Text ||
                !Updater.TryGetObject(modifier.reference, out LevelObject levelObject) ||
                levelObject.visualObject is not TextObject textObject)
                    return;

                var remove = modifier.GetInt(0, 1);
                string text = string.IsNullOrEmpty(textObject.textMeshPro.text) ? "" : textObject.textMeshPro.text.Length > remove ?
                    textObject.textMeshPro.text.Remove(remove, 1) : "";

                if (modifier.constant)
                    textObject.SetText(text);
                else
                    textObject.text = text;
            },
            "removeTextOtherAt" => modifier =>
            {
                var list = !modifier.prefabInstanceOnly ? GameData.Current.FindObjectsWithTag(modifier.GetValue(1)) : GameData.Current.FindObjectsWithTag(modifier.reference, modifier.GetValue(1));

                if (list.IsEmpty())
                    return;

                var remove = modifier.GetInt(0, 1);

                foreach (var bm in list)
                {
                    if (bm.ShapeType != ShapeType.Text || !Updater.TryGetObject(bm, out LevelObject levelObject) || levelObject.visualObject is not TextObject textObject)
                        continue;

                    string text = string.IsNullOrEmpty(textObject.textMeshPro.text) ? "" : textObject.textMeshPro.text.Length > remove ?
                        textObject.textMeshPro.text.Remove(remove, 1) : "";

                    if (modifier.constant)
                        textObject.SetText(text);
                    else
                        textObject.text = text;
                }
            },
            "formatText" => modifier =>
            {
                if (CoreConfig.Instance.AllowCustomTextFormatting.Value ||
                modifier.reference.ShapeType != ShapeType.Text ||
                !Updater.TryGetObject(modifier.reference, out LevelObject levelObject) ||
                levelObject.visualObject is not TextObject textObject)
                    return;

                textObject.SetText(RTString.FormatText(modifier.reference, textObject.text));
            },
            "textSequence" => modifier =>
            {
                if (modifier.reference.ShapeType != ShapeType.Text || !Updater.TryGetObject(modifier.reference, out LevelObject levelObject) || levelObject.visualObject is not TextObject textObject)
                    return;

                var text = !string.IsNullOrEmpty(modifier.GetValue(9)) ? modifier.GetValue(9) : modifier.reference.text;

                if (!modifier.setTimer)
                {
                    modifier.setTimer = true;
                    modifier.ResultTimer = AudioManager.inst.CurrentAudioSource.time;
                }

                var offsetTime = modifier.ResultTimer;
                if (!modifier.GetBool(11, false))
                    offsetTime = modifier.reference.StartTime;

                var time = AudioManager.inst.CurrentAudioSource.time - offsetTime + modifier.GetFloat(10, 0f);
                var length = modifier.GetFloat(0, 1f);
                var glitch = modifier.GetBool(1, true);

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

                    if (modifier.constant)
                        textObject.SetText(text);
                    else
                        textObject.text = text;
                }
                else
                {
                    if (modifier.constant)
                        textObject.SetText(text);
                    else
                        textObject.text = text;
                }

                if ((modifier.Result is not int result || result != stringLength2) && textWithoutFormatting[Mathf.Clamp(stringLength2 - 1, 0, textWithoutFormatting.Length - 1)] != ' ')
                {
                    modifier.Result = stringLength2;
                    float pitch = modifier.GetFloat(6, 1f);
                    float volume = modifier.GetFloat(7, 1f);
                    float pitchVary = modifier.GetFloat(8, 0f);

                    if (pitchVary != 0f)
                        pitch += UnityEngine.Random.Range(-pitchVary, pitchVary);

                    // Don't play any sounds.
                    if (!modifier.GetBool(2, true))
                        return;

                    // Don't play custom sound.
                    if (!modifier.GetBool(3, false))
                    {
                        SoundManager.inst.PlaySound(DefaultSounds.Click, volume, volume);
                        return;
                    }

                    if (SoundManager.inst.TryGetSound(modifier.GetValue(4), out AudioClip audioClip))
                        SoundManager.inst.PlaySound(audioClip, volume, pitch);
                    else
                        GetSoundPath(modifier.reference.id, modifier.GetValue(4), modifier.GetBool(5, false), pitch, volume, false);
                }
            },

            // modify shape
            "backgroundShape" => modifier =>
            {
                if (modifier.HasResult() || modifier.reference.IsSpecialShape || !Updater.TryGetObject(modifier.reference, out LevelObject levelObject) || !levelObject.visualObject.gameObject)
                    return;

                if (ShapeManager.inst.Shapes3D.TryGetAt(modifier.reference.shape, out ShapeGroup shapeGroup) && shapeGroup.TryGetShape(modifier.reference.shapeOption, out Shape shape))
                {
                    levelObject.visualObject.gameObject.GetComponent<MeshFilter>().mesh = shape.mesh;
                    modifier.Result = "frick";
                    levelObject.visualObject.gameObject.AddComponent<DestroyModifierResult>().Modifier = modifier;
                }
            },
            "sphereShape" => modifier =>
            {
                if (modifier.HasResult() || modifier.reference.IsSpecialShape || !Updater.TryGetObject(modifier.reference, out LevelObject levelObject) || !levelObject.visualObject.gameObject)
                    return;

                var shape = new Vector2Int(modifier.reference.shape, modifier.reference.shapeOption);

                levelObject.visualObject.gameObject.GetComponent<MeshFilter>().mesh = GameManager.inst.PlayerPrefabs[1].GetComponentInChildren<MeshFilter>().mesh;
                modifier.Result = "frick";
                levelObject.visualObject.gameObject.AddComponent<DestroyModifierResult>().Modifier = modifier;
            },
            "translateShape" => modifier =>
            {
                if (!Updater.TryGetObject(modifier.reference, out LevelObject levelObject) || !levelObject.visualObject.gameObject)
                    return;

                if (!modifier.HasResult())
                {
                    var meshFilter = levelObject.visualObject.gameObject.GetComponent<MeshFilter>();
                    var mesh = meshFilter.mesh;

                    modifier.Result = new KeyValuePair<MeshFilter, Vector3[]>(meshFilter, mesh.vertices);

                    levelObject.visualObject.gameObject.AddComponent<DestroyModifierResult>().Modifier = modifier;
                }

                var posX = modifier.GetFloat(1, 0f);
                var posY = modifier.GetFloat(2, 0f);
                var scaX = modifier.GetFloat(3, 0f);
                var scaY = modifier.GetFloat(4, 0f);
                var rot = modifier.GetFloat(5, 0f);

                if (modifier.TryGetResult(out KeyValuePair<MeshFilter, Vector3[]> keyValuePair))
                    keyValuePair.Key.mesh.vertices = keyValuePair.Value.Select(x => RTMath.Move(RTMath.Rotate(RTMath.Scale(x, new Vector2(scaX, scaY)), rot), new Vector2(posX, posY))).ToArray();
            },

            #endregion

            #region Animation

            "animateObject" => modifier =>
            {
                var time = modifier.GetFloat(0, 0f);
                var type = modifier.GetInt(1, 0);
                var x = modifier.GetFloat(2, 0f);
                var y = modifier.GetFloat(3, 0f);
                var z = modifier.GetFloat(4, 0f);
                var relative = modifier.GetBool(5, true);

                string easing = modifier.GetValue(6);
                if (int.TryParse(modifier.GetValue(6), out int e) && e >= 0 && e < DataManager.inst.AnimationList.Count)
                    easing = DataManager.inst.AnimationList[e].Name;

                Vector3 vector = modifier.reference.GetTransformOffset(type);

                var setVector = new Vector3(x, y, z);
                if (relative)
                {
                    if (modifier.constant)
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
                        }, vector3 => modifier.reference.SetTransform(type, vector3), interpolateOnComplete: true),
                    };
                    animation.onComplete = () => AnimationManager.inst.Remove(animation.id);
                    AnimationManager.inst.Play(animation);
                    return;
                }

                modifier.reference.SetTransform(type, setVector);
            },
            "animateObjectOther" => modifier =>
            {
                var list = !modifier.prefabInstanceOnly ? GameData.Current.FindObjectsWithTag(modifier.GetValue(7)) : GameData.Current.FindObjectsWithTag(modifier.reference, modifier.GetValue(7));

                if (list.IsEmpty())
                    return;

                var time = modifier.GetFloat(0, 0f);
                var type = modifier.GetInt(1, 0);
                var x = modifier.GetFloat(2, 0f);
                var y = modifier.GetFloat(3, 0f);
                var z = modifier.GetFloat(4, 0f);
                var relative = modifier.GetBool(5, true);

                string easing = modifier.GetValue(6);
                if (int.TryParse(modifier.GetValue(6), out int e) && e >= 0 && e < DataManager.inst.AnimationList.Count)
                    easing = DataManager.inst.AnimationList[e].Name;

                foreach (var bm in list)
                {
                    Vector3 vector = bm.GetTransformOffset(type);

                    var setVector = new Vector3(x, y, z);
                    if (relative)
                    {
                        if (modifier.constant)
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
                                new Vector3Keyframe(Mathf.Clamp(time, 0f, 9999f), setVector, Ease.HasEaseFunction(easing) ? Ease.GetEaseFunction(easing) : Ease.Linear),
                            }, vector3 => bm.SetTransform(type, vector3), interpolateOnComplete: true),
                        };
                        animation.onComplete = () => AnimationManager.inst.Remove(animation.id);
                        AnimationManager.inst.Play(animation);
                        break;
                    }

                    bm.SetTransform(type, setVector);
                }
            },
            "animateSignal" => modifier =>
            {
                var time = modifier.GetFloat(0, 0f);
                var type = modifier.GetInt(1, 0);
                var x = modifier.GetFloat(2, 0f);
                var y = modifier.GetFloat(3, 0f);
                var z = modifier.GetFloat(4, 0f);
                var relative = modifier.GetBool(5, true);

                if (!modifier.GetBool(9, true))
                {
                    var list = !modifier.prefabInstanceOnly ? GameData.Current.FindObjectsWithTag(modifier.GetValue(7)) : GameData.Current.FindObjectsWithTag(modifier.reference, modifier.GetValue(7));

                    foreach (var bm in list)
                    {
                        if (!bm.modifiers.IsEmpty() && !bm.modifiers.FindAll(x => x.Name == "requireSignal" && x.type == ModifierBase.Type.Trigger).IsEmpty() &&
                            bm.modifiers.TryFind(x => x.Name == "requireSignal" && x.type == ModifierBase.Type.Trigger, out Modifier<BeatmapObject> m))
                            m.Result = null;
                    }
                }

                string easing = modifier.GetValue(6);
                if (int.TryParse(modifier.GetValue(6), out int e) && e >= 0 && e < DataManager.inst.AnimationList.Count)
                    easing = DataManager.inst.AnimationList[e].Name;

                Vector3 vector = modifier.reference.GetTransformOffset(type);

                var setVector = new Vector3(x, y, z);
                if (relative)
                {
                    if (modifier.constant)
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
                        }, vector3 => modifier.reference.SetTransform(type, vector3), interpolateOnComplete: true),
                    };
                    animation.onComplete = () =>
                    {
                        AnimationManager.inst.Remove(animation.id);

                        var list = !modifier.prefabInstanceOnly ? GameData.Current.FindObjectsWithTag(modifier.GetValue(7)) : GameData.Current.FindObjectsWithTag(modifier.reference, modifier.GetValue(7));

                        foreach (var bm in list)
                            CoroutineHelper.StartCoroutine(ActivateModifier(bm, modifier.GetFloat(8, 0f)));
                    };
                    AnimationManager.inst.Play(animation);
                    return;
                }

                modifier.reference.SetTransform(type, setVector);
            },
            "animateSignalOther" => modifier =>
            {
                var list = !modifier.prefabInstanceOnly ? GameData.Current.FindObjectsWithTag(modifier.GetValue(7)) : GameData.Current.FindObjectsWithTag(modifier.reference, modifier.GetValue(7));

                if (list.IsEmpty())
                    return;

                var time = modifier.GetFloat(0, 0f);
                var type = modifier.GetInt(1, 0);
                var x = modifier.GetFloat(2, 0f);
                var y = modifier.GetFloat(3, 0f);
                var z = modifier.GetFloat(4, 0f);
                var relative = modifier.GetBool(5, true);

                if (!modifier.GetBool(10, true))
                {
                    var list2 = !modifier.prefabInstanceOnly ? GameData.Current.FindObjectsWithTag(modifier.GetValue(8)) : GameData.Current.FindObjectsWithTag(modifier.reference, modifier.GetValue(8));

                    foreach (var bm in list2)
                    {
                        if (!bm.modifiers.IsEmpty() && !bm.modifiers.FindAll(x => x.Name == "requireSignal" && x.type == ModifierBase.Type.Trigger).IsEmpty() &&
                            bm.modifiers.TryFind(x => x.Name == "requireSignal" && x.type == ModifierBase.Type.Trigger, out Modifier<BeatmapObject> m))
                        {
                            m.Result = null;
                        }
                    }
                }

                string easing = modifier.GetValue(6);
                if (int.TryParse(modifier.GetValue(6), out int e) && e >= 0 && e < DataManager.inst.AnimationList.Count)
                    easing = DataManager.inst.AnimationList[e].Name;

                foreach (var bm in list)
                {
                    Vector3 vector = bm.GetTransformOffset(type);

                    var setVector = new Vector3(x, y, z);
                    if (relative)
                    {
                        if (modifier.constant)
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
                                new Vector3Keyframe(Mathf.Clamp(time, 0f, 9999f), setVector, Ease.HasEaseFunction(easing) ? Ease.GetEaseFunction(easing) : Ease.Linear),
                            }, vector3 => bm.SetTransform(type, vector3), interpolateOnComplete: true),
                        };
                        animation.onComplete = () =>
                        {
                            AnimationManager.inst.Remove(animation.id);

                            var list = !modifier.prefabInstanceOnly ? GameData.Current.FindObjectsWithTag(modifier.GetValue(8)) : GameData.Current.FindObjectsWithTag(modifier.reference, modifier.GetValue(8));

                            foreach (var bm in list)
                                CoroutineHelper.StartCoroutine(ActivateModifier(bm, modifier.GetFloat(9, 0f)));
                        };
                        AnimationManager.inst.Play(animation);
                        break;
                    }

                    bm.SetTransform(type, setVector);
                }
            },

            // todo: continue cleaning up, have to take a break for my sanity
            "animateObjectMath" => modifier =>
            {
                if (!int.TryParse(modifier.commands[1], out int type) || !bool.TryParse(modifier.commands[5], out bool relative))
                    return;

                string easing = modifier.commands[6];
                if (int.TryParse(modifier.commands[6], out int e) && e >= 0 && e < DataManager.inst.AnimationList.Count)
                    easing = DataManager.inst.AnimationList[e].Name;

                var variables = modifier.reference.GetObjectVariables();
                var functions = modifier.reference.GetObjectFunctions();

                float time = (float)RTMath.Parse(modifier.value, variables, functions);
                float x = (float)RTMath.Parse(modifier.commands[2], variables, functions);
                float y = (float)RTMath.Parse(modifier.commands[3], variables, functions);
                float z = (float)RTMath.Parse(modifier.commands[4], variables, functions);

                Vector3 vector = modifier.reference.GetTransformOffset(type);

                var setVector = new Vector3(x, y, z);
                if (relative)
                {
                    if (modifier.constant)
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
                        }, vector3 => modifier.reference.SetTransform(type, vector3), interpolateOnComplete: true),
                    };
                    animation.onComplete = () => AnimationManager.inst.Remove(animation.id);
                    AnimationManager.inst.Play(animation);
                    return;
                }

                modifier.reference.SetTransform(type, setVector);
            },
            "animateObjectMathOther" => modifier =>
            {
                var list = !modifier.prefabInstanceOnly ? GameData.Current.FindObjectsWithTag(modifier.commands[7]) : GameData.Current.FindObjectsWithTag(modifier.reference, modifier.commands[7]);

                if (list.IsEmpty() || !int.TryParse(modifier.commands[1], out int type) || !bool.TryParse(modifier.commands[5], out bool relative))
                    return;

                string easing = modifier.commands[6];
                if (int.TryParse(modifier.commands[6], out int e) && e >= 0 && e < DataManager.inst.AnimationList.Count)
                    easing = DataManager.inst.AnimationList[e].Name;

                var variables = modifier.reference.GetObjectVariables();
                var functions = modifier.reference.GetObjectFunctions();

                // for optimization sake, we evaluate this outside of the foreach loop. normally I'd place this inside and replace "otherVar" with bm.integerVariable.ToString(), however I feel that would result in a worse experience so the tradeoff is not worth it.
                float time = (float)RTMath.Parse(modifier.value, variables, functions);
                float x = (float)RTMath.Parse(modifier.commands[2], variables, functions);
                float y = (float)RTMath.Parse(modifier.commands[3], variables, functions);
                float z = (float)RTMath.Parse(modifier.commands[4], variables, functions);

                foreach (var bm in list)
                {
                    Vector3 vector = bm.GetTransformOffset(type);

                    var setVector = new Vector3(x, y, z);
                    if (relative)
                    {
                        if (modifier.constant)
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
                                new Vector3Keyframe(Mathf.Clamp(time, 0f, 9999f), setVector, Ease.HasEaseFunction(easing) ? Ease.GetEaseFunction(easing) : Ease.Linear),
                            }, vector3 => bm.SetTransform(type, vector3), interpolateOnComplete: true),
                        };
                        animation.onComplete = () => AnimationManager.inst.Remove(animation.id);
                        AnimationManager.inst.Play(animation);
                        continue;
                    }

                    bm.SetTransform(type, setVector);
                }
            },
            "animateSignalMath" => modifier =>
            {
                if (!int.TryParse(modifier.commands[1], out int type) || !bool.TryParse(modifier.commands[5], out bool relative))
                    return;

                if (!Parser.TryParse(modifier.commands[9], true))
                {
                    var list = !modifier.prefabInstanceOnly ? GameData.Current.FindObjectsWithTag(modifier.commands[7]) : GameData.Current.FindObjectsWithTag(modifier.reference, modifier.commands[7]);

                    foreach (var bm in list)
                    {
                        if (!bm.modifiers.IsEmpty() && !bm.modifiers.FindAll(x => x.commands[0] == "requireSignal" && x.type == ModifierBase.Type.Trigger).IsEmpty() &&
                            bm.modifiers.TryFind(x => x.commands[0] == "requireSignal" && x.type == ModifierBase.Type.Trigger, out Modifier<BeatmapObject> m))
                            m.Result = null;
                    }
                }

                string easing = modifier.commands[6];
                if (int.TryParse(modifier.commands[6], out int e) && e >= 0 && e < DataManager.inst.AnimationList.Count)
                    easing = DataManager.inst.AnimationList[e].Name;

                var variables = modifier.reference.GetObjectVariables();
                var functions = modifier.reference.GetObjectFunctions();

                float time = (float)RTMath.Parse(modifier.value, variables, functions);
                float x = (float)RTMath.Parse(modifier.commands[2], variables, functions);
                float y = (float)RTMath.Parse(modifier.commands[3], variables, functions);
                float z = (float)RTMath.Parse(modifier.commands[4], variables, functions);
                float signalTime = (float)RTMath.Parse(modifier.commands[8], variables, functions);

                Vector3 vector = modifier.reference.GetTransformOffset(type);

                var setVector = new Vector3(x, y, z);
                if (relative)
                {
                    if (modifier.constant)
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
                        }, vector3 => modifier.reference.SetTransform(type, vector3), interpolateOnComplete: true),
                    };
                    animation.onComplete = () =>
                    {
                        AnimationManager.inst.Remove(animation.id);

                        var list = !modifier.prefabInstanceOnly ? GameData.Current.FindObjectsWithTag(modifier.commands[7]) : GameData.Current.FindObjectsWithTag(modifier.reference, modifier.commands[7]);

                        foreach (var bm in list)
                            CoroutineHelper.StartCoroutine(ActivateModifier(bm, signalTime));
                    };
                    AnimationManager.inst.Play(animation);
                    return;
                }

                modifier.reference.SetTransform(type, setVector);
            },
            "animateSignalMathOther" => modifier =>
            {
                var list = !modifier.prefabInstanceOnly ? GameData.Current.FindObjectsWithTag(modifier.commands[7]) : GameData.Current.FindObjectsWithTag(modifier.reference, modifier.commands[7]);

                if (list.IsEmpty() || !int.TryParse(modifier.commands[1], out int type) || !bool.TryParse(modifier.commands[5], out bool relative))
                    return;

                if (!Parser.TryParse(modifier.commands[10], true))
                {
                    var list2 = !modifier.prefabInstanceOnly ? GameData.Current.FindObjectsWithTag(modifier.commands[8]) : GameData.Current.FindObjectsWithTag(modifier.reference, modifier.commands[8]);

                    foreach (var bm in list2)
                    {
                        if (bm.modifiers.Count > 0 && bm.modifiers.FindAll(x => x.commands[0] == "requireSignal" && x.type == ModifierBase.Type.Trigger).Count > 0 &&
                            bm.modifiers.TryFind(x => x.commands[0] == "requireSignal" && x.type == ModifierBase.Type.Trigger, out Modifier<BeatmapObject> m))
                            m.Result = null;
                    }
                }

                string easing = modifier.commands[6];
                if (int.TryParse(modifier.commands[6], out int e) && e >= 0 && e < DataManager.inst.AnimationList.Count)
                    easing = DataManager.inst.AnimationList[e].Name;

                var variables = modifier.reference.GetObjectVariables();
                var functions = modifier.reference.GetObjectFunctions();

                float time = (float)RTMath.Parse(modifier.value, variables, functions);
                float x = (float)RTMath.Parse(modifier.commands[2], variables, functions);
                float y = (float)RTMath.Parse(modifier.commands[3], variables, functions);
                float z = (float)RTMath.Parse(modifier.commands[4], variables, functions);
                float signalTime = (float)RTMath.Parse(modifier.commands[8], variables);

                foreach (var bm in list)
                {
                    Vector3 vector = bm.GetTransformOffset(type);

                    var setVector = new Vector3(x, y, z);
                    if (relative)
                    {
                        if (modifier.constant)
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
                                new Vector3Keyframe(Mathf.Clamp(time, 0f, 9999f), setVector, Ease.HasEaseFunction(easing) ? Ease.GetEaseFunction(easing) : Ease.Linear),
                            }, vector3 => modifier.reference.SetTransform(type, vector3), interpolateOnComplete: true),
                        };
                        animation.onComplete = () =>
                        {
                            AnimationManager.inst.Remove(animation.id);

                            var list = !modifier.prefabInstanceOnly ? GameData.Current.FindObjectsWithTag(modifier.commands[8]) : GameData.Current.FindObjectsWithTag(modifier.reference, modifier.commands[8]);

                            foreach (var bm in list)
                                CoroutineHelper.StartCoroutine(ActivateModifier(bm, signalTime));
                        };
                        AnimationManager.inst.Play(animation);
                        continue;
                    }

                    modifier.reference.SetTransform(type, setVector);
                }
            },

            "gravity" => modifier =>
            {
                var gravityX = Parser.TryParse(modifier.commands[1], 0f);
                var gravityY = Parser.TryParse(modifier.commands[2], 0f);
                float time = Parser.TryParse(modifier.commands[3], 1f);
                int curve = Parser.TryParse(modifier.commands[4], 2);

                if (modifier.Result == null)
                {
                    modifier.Result = Vector2.zero;
                    modifier.ResultTimer = Time.time;
                }
                else
                    modifier.Result = RTMath.Lerp(Vector2.zero, new Vector2(gravityX, gravityY), (RTMath.Recursive(Time.time - modifier.ResultTimer, curve)) * (time * CoreHelper.TimeFrame));

                var vector = (Vector2)modifier.Result;

                var rotation = modifier.reference.InterpolateChainRotation(includeSelf: false);

                modifier.reference.positionOffset = RTMath.Rotate(vector, -rotation);
            },
            "gravityOther" => modifier =>
            {
                var beatmapObjects = !modifier.prefabInstanceOnly ? GameData.Current.FindObjectsWithTag(modifier.value) : GameData.Current.FindObjectsWithTag(modifier.reference, modifier.value);

                if (beatmapObjects.IsEmpty())
                    return;

                var gravityX = Parser.TryParse(modifier.commands[1], 0f);
                var gravityY = Parser.TryParse(modifier.commands[2], 0f);
                float time = Parser.TryParse(modifier.commands[3], 1f);
                int curve = Parser.TryParse(modifier.commands[4], 2);

                if (modifier.Result == null)
                {
                    modifier.Result = Vector2.zero;
                    modifier.ResultTimer = Time.time;
                }
                else
                    modifier.Result = RTMath.Lerp(Vector2.zero, new Vector2(gravityX, gravityY), (RTMath.Recursive(Time.time - modifier.ResultTimer, curve)) * (time * CoreHelper.TimeFrame));

                var vector = (Vector2)modifier.Result;

                foreach (var beatmapObject in beatmapObjects)
                {
                    var rotation = beatmapObject.InterpolateChainRotation(includeSelf: false);

                    beatmapObject.positionOffset = RTMath.Rotate(vector, -rotation);
                }
            },

            "copyAxis" => modifier =>
            {
                if (int.TryParse(modifier.commands[1], out int fromType) && int.TryParse(modifier.commands[2], out int fromAxis)
                    && int.TryParse(modifier.commands[3], out int toType) && int.TryParse(modifier.commands[4], out int toAxis)
                    && float.TryParse(modifier.commands[5], out float delay) && float.TryParse(modifier.commands[6], out float multiply)
                    && float.TryParse(modifier.commands[7], out float offset) && float.TryParse(modifier.commands[8], out float min) && float.TryParse(modifier.commands[9], out float max)
                    && float.TryParse(modifier.commands[10], out float loop) && bool.TryParse(modifier.commands[11], out bool useVisual)
                    && GameData.Current.TryFindObjectWithTag(modifier, modifier.value, out BeatmapObject bm))
                {
                    var time = Updater.CurrentTime;

                    fromType = Mathf.Clamp(fromType, 0, bm.events.Count);
                    fromAxis = Mathf.Clamp(fromAxis, 0, bm.events[fromType][0].values.Length);

                    if (toType < 0 || toType > 3)
                        return;

                    if (!useVisual && Updater.levelProcessor.converter.cachedSequences.TryGetValue(bm.id, out ObjectConverter.CachedSequences cachedSequence))
                    {
                        if (fromType == 3)
                        {
                            if (toType == 3 && toAxis == 0 && cachedSequence.ColorSequence != null &&
                                modifier.reference.levelObject && modifier.reference.levelObject.visualObject != null)
                            {
                                var sequence = cachedSequence.ColorSequence.Interpolate(time - bm.StartTime - delay);
                                var visualObject = modifier.reference.levelObject.visualObject;
                                visualObject.SetColor(RTMath.Lerp(visualObject.GetPrimaryColor(), sequence, multiply));
                            }
                            return;
                        }
                        modifier.reference.SetTransform(toType, toAxis, fromType switch
                        {
                            0 => Mathf.Clamp((cachedSequence.PositionSequence.Interpolate(time - bm.StartTime - delay).At(fromAxis) - offset) * multiply % loop, min, max),
                            1 => Mathf.Clamp((cachedSequence.ScaleSequence.Interpolate(time - bm.StartTime - delay).At(fromAxis) - offset) * multiply % loop, min, max),
                            2 => Mathf.Clamp((cachedSequence.RotationSequence.Interpolate(time - bm.StartTime - delay) - offset) * multiply % loop, min, max),
                            _ => 0f,
                        });
                    }
                    else if (useVisual && Updater.TryGetObject(bm, out LevelObject levelObject) && levelObject.visualObject != null && levelObject.visualObject.gameObject)
                        modifier.reference.SetTransform(toType, toAxis, Mathf.Clamp((levelObject.visualObject.gameObject.transform.GetVector(fromType).At(fromAxis) - offset) * multiply % loop, min, max));
                    else if (useVisual)
                        modifier.reference.SetTransform(toType, toAxis, Mathf.Clamp(fromType switch
                        {
                            0 => bm.InterpolateChainPosition().At(fromAxis),
                            1 => bm.InterpolateChainScale().At(fromAxis),
                            2 => bm.InterpolateChainRotation(),
                            _ => 0f,
                        }, min, max));
                }
            },
            "copyAxisMath" => modifier =>
            {
                try
                {
                    if (int.TryParse(modifier.commands[1], out int fromType) && int.TryParse(modifier.commands[2], out int fromAxis)
                        && int.TryParse(modifier.commands[3], out int toType) && int.TryParse(modifier.commands[4], out int toAxis)
                        && float.TryParse(modifier.commands[5], out float delay) && float.TryParse(modifier.commands[6], out float min)
                        && float.TryParse(modifier.commands[7], out float max) && bool.TryParse(modifier.commands[9], out bool useVisual)
                        && GameData.Current.TryFindObjectWithTag(modifier, modifier.value, out BeatmapObject bm))
                    {
                        var time = Updater.CurrentTime;

                        fromType = Mathf.Clamp(fromType, 0, bm.events.Count);
                        fromAxis = Mathf.Clamp(fromAxis, 0, bm.events[fromType][0].values.Length);

                        if (toType < 0 || toType > 3)
                            return;

                        if (!useVisual && Updater.levelProcessor.converter.cachedSequences.TryGetValue(bm.id, out ObjectConverter.CachedSequences cachedSequence))
                        {
                            if (fromType == 3)
                            {
                                if (toType == 3 && toAxis == 0 && cachedSequence.ColorSequence != null &&
                                    modifier.reference.levelObject && modifier.reference.levelObject.visualObject != null &&
                                    modifier.reference.levelObject.visualObject.renderer)
                                {
                                    var sequence = cachedSequence.ColorSequence.Interpolate(time - bm.StartTime - delay);

                                    var renderer = modifier.reference.levelObject.visualObject.renderer;

                                    var variables = modifier.reference.GetObjectVariables();
                                    variables["colorR"] = sequence.r;
                                    variables["colorG"] = sequence.g;
                                    variables["colorB"] = sequence.b;
                                    variables["colorA"] = sequence.a;
                                    bm.SetOtherObjectVariables(variables);

                                    float value = RTMath.Parse(modifier.commands[8], variables);

                                    renderer.material.color = RTMath.Lerp(renderer.material.color, sequence, Mathf.Clamp(value, min, max));
                                }
                            }
                            else
                            {
                                var variables = modifier.reference.GetObjectVariables();
                                variables["axis"] = fromType switch
                                {
                                    0 => cachedSequence.PositionSequence.Interpolate(time - bm.StartTime - delay).At(fromAxis),
                                    1 => cachedSequence.ScaleSequence.Interpolate(time - bm.StartTime - delay).At(fromAxis),
                                    2 => cachedSequence.RotationSequence.Interpolate(time - bm.StartTime - delay),
                                    _ => 0f,
                                };
                                bm.SetOtherObjectVariables(variables);

                                float value = RTMath.Parse(modifier.GetValue(8), variables);

                                modifier.reference.SetTransform(toType, toAxis, Mathf.Clamp(value, min, max));
                            }
                        }
                        else if (useVisual && Updater.TryGetObject(bm, out LevelObject levelObject) && levelObject.visualObject != null && levelObject.visualObject.gameObject)
                        {
                            var axis = levelObject.visualObject.gameObject.transform.GetVector(fromType).At(fromAxis);

                            var variables = modifier.reference.GetObjectVariables();
                            variables["axis"] = axis;
                            bm.SetOtherObjectVariables(variables);

                            float value = RTMath.Parse(modifier.GetValue(8), variables);

                            modifier.reference.SetTransform(toType, toAxis, Mathf.Clamp(value, min, max));
                        }
                        else if (useVisual)
                        {
                            var variables = modifier.reference.GetObjectVariables();
                            variables["axis"] = fromType switch
                            {
                                0 => bm.InterpolateChainPosition().At(fromAxis),
                                1 => bm.InterpolateChainScale().At(fromAxis),
                                2 => bm.InterpolateChainRotation(),
                                _ => 0f,
                            };
                            bm.SetOtherObjectVariables(variables);

                            float value = RTMath.Parse(modifier.GetValue(8), variables);

                            modifier.reference.SetTransform(toType, toAxis, Mathf.Clamp(value, min, max));
                        }
                    }
                }
                catch
                {

                } // try catch for cases where the math is broken
            },
            "copyAxisGroup" => modifier =>
            {
                var evaluation = modifier.GetValue(0);

                var toType = Parser.TryParse(modifier.commands[1], 0);
                var toAxis = Parser.TryParse(modifier.commands[2], 0);

                if (toType < 0 || toType > 4)
                    return;

                try
                {
                    var cachedSequences = Updater.levelProcessor.converter.cachedSequences;
                    var beatmapObjects = GameData.Current.beatmapObjects;
                    var prefabObjects = GameData.Current.prefabObjects;

                    var time = Updater.CurrentTime;
                    var variables = modifier.reference.GetObjectVariables();

                    for (int i = 3; i < modifier.commands.Count; i += 8)
                    {
                        var name = modifier.commands[i];
                        var group = modifier.commands[i + 1];
                        var fromType = Parser.TryParse(modifier.commands[i + 2], 0);
                        var fromAxis = Parser.TryParse(modifier.commands[i + 3], 0);
                        var delay = Parser.TryParse(modifier.commands[i + 4], 0f);
                        var min = Parser.TryParse(modifier.commands[i + 5], 0f);
                        var max = Parser.TryParse(modifier.commands[i + 6], 0f);
                        var useVisual = Parser.TryParse(modifier.commands[i + 7], false);

                        var beatmapObject = !modifier.prefabInstanceOnly ? GameData.Current.FindObjectWithTag(group) : GameData.Current.FindObjectWithTag(beatmapObjects, modifier.reference, group);

                        if (!beatmapObject)
                            continue;

                        cachedSequences.TryGetValue(beatmapObject.id, out ObjectConverter.CachedSequences cachedSequence);

                        if (!useVisual && cachedSequence != null)
                            variables[name] = fromType switch
                            {
                                0 => Mathf.Clamp(cachedSequence.PositionSequence.Interpolate(time - beatmapObject.StartTime - delay).At(fromAxis), min, max),
                                1 => Mathf.Clamp(cachedSequence.ScaleSequence.Interpolate(time - beatmapObject.StartTime - delay).At(fromAxis), min, max),
                                2 => Mathf.Clamp(cachedSequence.RotationSequence.Interpolate(time - beatmapObject.StartTime - delay), min, max),
                                _ => 0f,
                            };
                        else if (useVisual && Updater.TryGetObject(beatmapObject, out LevelObject levelObject) && levelObject.visualObject != null && levelObject.visualObject.gameObject)
                            variables[name] = Mathf.Clamp(levelObject.visualObject.gameObject.transform.GetVector(fromType).At(fromAxis), min, max);
                        else if (useVisual)
                            variables[name] = fromType switch
                            {
                                0 => Mathf.Clamp(beatmapObject.InterpolateChainPosition().At(fromAxis), min, max),
                                1 => Mathf.Clamp(beatmapObject.InterpolateChainScale().At(fromAxis), min, max),
                                2 => Mathf.Clamp(beatmapObject.InterpolateChainRotation(), min, max),
                                _ => 0f,
                            };

                        if (fromType == 4)
                            variables[name] = Mathf.Clamp(beatmapObject.integerVariable, min, max);
                    }

                    modifier.reference.SetTransform(toType, toAxis, RTMath.Parse(evaluation, variables));
                }
                catch (Exception ex)
                {
                    CoreHelper.LogException(ex);
                }
            },
            "copyPlayerAxis" => modifier =>
            {
                if (int.TryParse(modifier.commands[1], out int fromType) && int.TryParse(modifier.commands[2], out int fromAxis)
                    && int.TryParse(modifier.commands[3], out int toType) && int.TryParse(modifier.commands[4], out int toAxis)
                    && float.TryParse(modifier.commands[5], out float delay) && float.TryParse(modifier.commands[6], out float multiply)
                    && float.TryParse(modifier.commands[7], out float offset) && float.TryParse(modifier.commands[8], out float min) && float.TryParse(modifier.commands[9], out float max)
                    && InputDataManager.inst.players.TryFind(x => x is CustomPlayer customPlayer && customPlayer.Player && customPlayer.Player.rb, out InputDataManager.CustomPlayer p))
                    modifier.reference.SetTransform(toType, toAxis, Mathf.Clamp((((CustomPlayer)p).Player.rb.transform.GetLocalVector(fromType).At(fromAxis) - offset) * multiply, min, max));
            },
            "legacyTail" => modifier =>
            {
                if (!modifier.reference || modifier.commands.IsEmpty() || !GameData.Current)
                    return;

                var totalTime = Parser.TryParse(modifier.value, 200f);

                var list = modifier.Result is List<LegacyTracker> ? (List<LegacyTracker>)modifier.Result : new List<LegacyTracker>();

                if (!modifier.HasResult())
                {
                    list.Add(new LegacyTracker(modifier.reference, Vector3.zero, Vector3.zero, Quaternion.identity, 0f, 0f));

                    for (int i = 1; i < modifier.commands.Count; i += 3)
                    {
                        var group = !modifier.prefabInstanceOnly ? GameData.Current.FindObjectsWithTag(modifier.commands[i]) : GameData.Current.FindObjectsWithTag(modifier.reference, modifier.commands[i]);

                        if (modifier.commands.Count <= i + 2 || group.Count < 1)
                            break;

                        var distance = Parser.TryParse(modifier.commands[i + 1], 2f);
                        var time = Parser.TryParse(modifier.commands[i + 2], 12f);

                        for (int j = 0; j < group.Count; j++)
                        {
                            var beatmapObject = group[j];
                            list.Add(new LegacyTracker(beatmapObject, beatmapObject.positionOffset, beatmapObject.positionOffset, Quaternion.Euler(beatmapObject.rotationOffset), distance, time));
                        }
                    }

                    modifier.Result = list;
                }

                var animationResult = modifier.reference.InterpolateChain();
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
            },

            "applyAnimation" => modifier =>
            {
                if (GameData.Current.TryFindObjectWithTag(modifier, modifier.value, out BeatmapObject from))
                {
                    var list = !modifier.prefabInstanceOnly ? GameData.Current.FindObjectsWithTag(modifier.commands[10]) : GameData.Current.FindObjectsWithTag(modifier.reference, modifier.commands[10]);

                    if (!modifier.HasResult())
                        modifier.Result = Updater.CurrentTime;
                    var time = modifier.GetResult<float>();

                    var animatePos = modifier.GetBool(1, true);
                    var animateSca = modifier.GetBool(2, true);
                    var animateRot = modifier.GetBool(3, true);
                    var delayPos = modifier.GetFloat(4, 0f);
                    var delaySca = modifier.GetFloat(5, 0f);
                    var delayRot = modifier.GetFloat(6, 0f);
                    var useVisual = modifier.GetBool(7, false);
                    var length = modifier.GetFloat(8, 1f);
                    var speed = modifier.GetFloat(9, 1f);

                    if (!modifier.constant)
                        AnimationManager.inst.RemoveName("Apply Object Animation " + modifier.reference.id);

                    for (int i = 0; i < list.Count; i++)
                    {
                        var bm = list[i];

                        if (!modifier.constant)
                        {
                            var animation = new RTAnimation("Apply Object Animation " + modifier.reference.id);
                            animation.animationHandlers = new List<AnimationHandlerBase>
                            {
                                new AnimationHandler<float>(new List<IKeyframe<float>>
                                {
                                    new FloatKeyframe(0f, 0f, Ease.Linear),
                                    new FloatKeyframe(Mathf.Clamp(length / speed, 0f, 100f), length, Ease.Linear),
                                }, x => ApplyAnimationTo(bm, from, useVisual, 0f, x, animatePos, animateSca, animateRot, delayPos, delaySca, delayRot), interpolateOnComplete: true)
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

                        ApplyAnimationTo(bm, from, useVisual, time, Updater.CurrentTime, animatePos, animateSca, animateRot, delayPos, delaySca, delayRot);
                    }
                }
            },
            "applyAnimationFrom" => modifier =>
            {
                if (GameData.Current.TryFindObjectWithTag(modifier, modifier.value, out BeatmapObject bm))
                {
                    if (!modifier.HasResult())
                        modifier.Result = Updater.CurrentTime;
                    var time = modifier.GetResult<float>();

                    var animatePos = modifier.GetBool(1, true);
                    var animateSca = modifier.GetBool(2, true);
                    var animateRot = modifier.GetBool(3, true);
                    var delayPos = modifier.GetFloat(4, 0f);
                    var delaySca = modifier.GetFloat(5, 0f);
                    var delayRot = modifier.GetFloat(6, 0f);
                    var useVisual = modifier.GetBool(7, false);
                    var length = modifier.GetFloat(8, 1f);
                    var speed = modifier.GetFloat(9, 1f);

                    if (!modifier.constant)
                    {
                        AnimationManager.inst.RemoveName("Apply Object Animation " + modifier.reference.id);

                        var animation = new RTAnimation("Apply Object Animation " + modifier.reference.id);
                        animation.animationHandlers = new List<AnimationHandlerBase>
                        {
                            new AnimationHandler<float>(new List<IKeyframe<float>>
                            {
                                new FloatKeyframe(0f, 0f, Ease.Linear),
                                new FloatKeyframe(Mathf.Clamp(length / speed, 0f, 100f), length, Ease.Linear),
                            }, x => ApplyAnimationTo(modifier.reference, bm, useVisual, 0f, x, animatePos, animateSca, animateRot, delayPos, delaySca, delayRot), interpolateOnComplete: true)
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

                    ApplyAnimationTo(modifier.reference, bm, useVisual, time, Updater.CurrentTime, animatePos, animateSca, animateRot, delayPos, delaySca, delayRot);
                }
            },
            "applyAnimationTo" => modifier =>
            {
                var list = !modifier.prefabInstanceOnly ? GameData.Current.FindObjectsWithTag(modifier.value) : GameData.Current.FindObjectsWithTag(modifier.reference, modifier.value);

                if (!modifier.HasResult())
                    modifier.Result = Updater.CurrentTime;
                var time = modifier.GetResult<float>();

                var animatePos = modifier.GetBool(1, true);
                var animateSca = modifier.GetBool(2, true);
                var animateRot = modifier.GetBool(3, true);
                var delayPos = modifier.GetFloat(4, 0f);
                var delaySca = modifier.GetFloat(5, 0f);
                var delayRot = modifier.GetFloat(6, 0f);
                var useVisual = modifier.GetBool(7, false);
                var length = modifier.GetFloat(8, 1f);
                var speed = modifier.GetFloat(9, 1f);

                if (!modifier.constant)
                    AnimationManager.inst.RemoveName("Apply Object Animation " + modifier.reference.id);

                for (int i = 0; i < list.Count; i++)
                {
                    var bm = list[i];

                    if (!modifier.constant)
                    {
                        var animation = new RTAnimation("Apply Object Animation " + modifier.reference.id);
                        animation.animationHandlers = new List<AnimationHandlerBase>
                        {
                            new AnimationHandler<float>(new List<IKeyframe<float>>
                            {
                                new FloatKeyframe(0f, 0f, Ease.Linear),
                                new FloatKeyframe(Mathf.Clamp(length / speed, 0f, 100f), length, Ease.Linear),
                            }, x => ApplyAnimationTo(bm, modifier.reference, useVisual, 0f, x, animatePos, animateSca, animateRot, delayPos, delaySca, delayRot), interpolateOnComplete: true)
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

                    ApplyAnimationTo(bm, modifier.reference, useVisual, time, Updater.CurrentTime, animatePos, animateSca, animateRot, delayPos, delaySca, delayRot);
                }
            },
            "applyAnimationMath" => modifier =>
            {
                if (GameData.Current.TryFindObjectWithTag(modifier, modifier.value, out BeatmapObject from))
                {
                    var list = !modifier.prefabInstanceOnly ? GameData.Current.FindObjectsWithTag(modifier.commands[10]) : GameData.Current.FindObjectsWithTag(modifier.reference, modifier.commands[10]);

                    if (!modifier.HasResult())
                        modifier.Result = Updater.CurrentTime;
                    var time = modifier.GetResult<float>();

                    var variables = modifier.reference.GetObjectVariables();
                    var functions = modifier.reference.GetObjectFunctions();

                    var animatePos = modifier.GetBool(1, true);
                    var animateSca = modifier.GetBool(2, true);
                    var animateRot = modifier.GetBool(3, true);
                    var delayPos = RTMath.Parse(modifier.commands[4], variables, functions);
                    var delaySca = RTMath.Parse(modifier.commands[5], variables, functions);
                    var delayRot = RTMath.Parse(modifier.commands[6], variables, functions);
                    var useVisual = modifier.GetBool(7, false);
                    var length = RTMath.Parse(modifier.commands[8], variables, functions);
                    var speed = RTMath.Parse(modifier.commands[9], variables, functions);
                    var timeOffset = RTMath.Parse(modifier.commands[11], variables, functions);

                    if (!modifier.constant)
                        AnimationManager.inst.RemoveName("Apply Object Animation " + modifier.reference.id);

                    for (int i = 0; i < list.Count; i++)
                    {
                        var bm = list[i];

                        if (!modifier.constant)
                        {
                            var animation = new RTAnimation("Apply Object Animation " + modifier.reference.id);
                            animation.animationHandlers = new List<AnimationHandlerBase>
                            {
                                new AnimationHandler<float>(new List<IKeyframe<float>>
                                {
                                    new FloatKeyframe(0f, 0f, Ease.Linear),
                                    new FloatKeyframe(Mathf.Clamp(length / speed, 0f, 100f), length, Ease.Linear),
                                }, x => ApplyAnimationTo(bm, from, useVisual, 0f, x, animatePos, animateSca, animateRot, delayPos, delaySca, delayRot), interpolateOnComplete: true)
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

                        ApplyAnimationTo(bm, from, useVisual, time, timeOffset, animatePos, animateSca, animateRot, delayPos, delaySca, delayRot);
                    }
                }
            },
            "applyAnimationFromMath" => modifier =>
            {
                if (GameData.Current.TryFindObjectWithTag(modifier, modifier.value, out BeatmapObject bm))
                {
                    if (!modifier.HasResult())
                        modifier.Result = Updater.CurrentTime;
                    var time = modifier.GetResult<float>();

                    var variables = modifier.reference.GetObjectVariables();
                    var functions = modifier.reference.GetObjectFunctions();

                    var animatePos = modifier.GetBool(1, true);
                    var animateSca = modifier.GetBool(2, true);
                    var animateRot = modifier.GetBool(3, true);
                    var delayPos = RTMath.Parse(modifier.commands[4], variables, functions);
                    var delaySca = RTMath.Parse(modifier.commands[5], variables, functions);
                    var delayRot = RTMath.Parse(modifier.commands[6], variables, functions);
                    var useVisual = modifier.GetBool(7, false);
                    var length = RTMath.Parse(modifier.commands[8], variables, functions);
                    var speed = RTMath.Parse(modifier.commands[9], variables, functions);
                    var timeOffset = RTMath.Parse(modifier.commands[10], variables, functions);

                    if (!modifier.constant)
                    {
                        AnimationManager.inst.RemoveName("Apply Object Animation " + modifier.reference.id);

                        var animation = new RTAnimation("Apply Object Animation " + modifier.reference.id);
                        animation.animationHandlers = new List<AnimationHandlerBase>
                        {
                            new AnimationHandler<float>(new List<IKeyframe<float>>
                            {
                                new FloatKeyframe(0f, 0f, Ease.Linear),
                                new FloatKeyframe(Mathf.Clamp(length / speed, 0f, 100f), length, Ease.Linear),
                            }, x => ApplyAnimationTo(modifier.reference, bm, useVisual, 0f, x, animatePos, animateSca, animateRot, delayPos, delaySca, delayRot), interpolateOnComplete: true)
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

                    ApplyAnimationTo(modifier.reference, bm, useVisual, time, timeOffset, animatePos, animateSca, animateRot, delayPos, delaySca, delayRot);
                }
            },
            "applyAnimationToMath" => modifier =>
            {
                var list = !modifier.prefabInstanceOnly ? GameData.Current.FindObjectsWithTag(modifier.value) : GameData.Current.FindObjectsWithTag(modifier.reference, modifier.value);

                if (!modifier.HasResult())
                    modifier.Result = Updater.CurrentTime;
                var time = modifier.GetResult<float>();

                var variables = modifier.reference.GetObjectVariables();
                var functions = modifier.reference.GetObjectFunctions();

                var animatePos = modifier.GetBool(1, true);
                var animateSca = modifier.GetBool(2, true);
                var animateRot = modifier.GetBool(3, true);
                var delayPos = RTMath.Parse(modifier.commands[4], variables, functions);
                var delaySca = RTMath.Parse(modifier.commands[5], variables, functions);
                var delayRot = RTMath.Parse(modifier.commands[6], variables, functions);
                var useVisual = modifier.GetBool(7, false);
                var length = RTMath.Parse(modifier.commands[8], variables, functions);
                var speed = RTMath.Parse(modifier.commands[9], variables, functions);
                var timeOffset = RTMath.Parse(modifier.commands[10], variables, functions);

                if (!modifier.constant)
                    AnimationManager.inst.RemoveName("Apply Object Animation " + modifier.reference.id);

                for (int i = 0; i < list.Count; i++)
                {
                    var bm = list[i];

                    if (!modifier.constant)
                    {
                        var animation = new RTAnimation("Apply Object Animation " + modifier.reference.id);
                        animation.animationHandlers = new List<AnimationHandlerBase>
                        {
                            new AnimationHandler<float>(new List<IKeyframe<float>>
                            {
                                new FloatKeyframe(0f, 0f, Ease.Linear),
                                new FloatKeyframe(Mathf.Clamp(length / speed, 0f, 100f), length, Ease.Linear),
                            }, x => ApplyAnimationTo(bm, modifier.reference, useVisual, 0f, x, animatePos, animateSca, animateRot, delayPos, delaySca, delayRot), interpolateOnComplete: true)
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

                    ApplyAnimationTo(bm, modifier.reference, useVisual, time, timeOffset, animatePos, animateSca, animateRot, delayPos, delaySca, delayRot);
                }
            },

            #endregion

            #region BG Object

            "setBGActive" => modifier =>
            {
                if (!bool.TryParse(modifier.value, out bool active))
                    return;

                var list = GameData.Current.backgroundObjects.FindAll(x => x.tags.Contains(modifier.commands[1]));
                if (!list.IsEmpty())
                    for (int i = 0; i < list.Count; i++)
                        list[i].Enabled = active;
            },

            #endregion

            #region Prefab

            "spawnPrefab" => modifier =>
            {
                if (modifier.constant || modifier.HasResult())
                    return;

                var prefab = GetPrefab(modifier.GetInt(12, 0), modifier.GetValue(0));

                if (!prefab)
                    return;

                var posX = modifier.GetFloat(1, 0f);
                var posY = modifier.GetFloat(2, 0f);
                var scaX = modifier.GetFloat(3, 0f);
                var scaY = modifier.GetFloat(4, 0f);
                var rot = modifier.GetFloat(5, 0f);
                var repeatCount = modifier.GetInt(6, 0);
                var repeatOffsetTime = modifier.GetFloat(7, 0f);
                var speed = modifier.GetFloat(8, 0f);

                var prefabObject = AddPrefabObjectToLevel(prefab,
                    modifier.GetBool(11, true) ? AudioManager.inst.CurrentAudioSource.time + modifier.GetFloat(10, 0f) : modifier.GetFloat(10, 0f),
                    new Vector2(posX, posY),
                    new Vector2(scaX, scaY),
                    rot, repeatCount, repeatOffsetTime, speed);

                modifier.Result = prefabObject;
                GameData.Current.prefabObjects.Add(prefabObject);
                Updater.AddPrefabToLevel(prefabObject);
            },
            "spawnPrefabOffset" => modifier =>
            {
                if (modifier.constant || modifier.HasResult())
                    return;

                var prefab = GetPrefab(modifier.GetInt(12, 0), modifier.GetValue(0));

                if (!prefab)
                    return;

                var animationResult = modifier.reference.InterpolateChain();

                var posX = modifier.GetFloat(1, 0f);
                var posY = modifier.GetFloat(2, 0f);
                var scaX = modifier.GetFloat(3, 0f);
                var scaY = modifier.GetFloat(4, 0f);
                var rot = modifier.GetFloat(5, 0f);
                var repeatCount = modifier.GetInt(6, 0);
                var repeatOffsetTime = modifier.GetFloat(7, 0f);
                var speed = modifier.GetFloat(8, 0f);

                var prefabObject = AddPrefabObjectToLevel(prefab,
                    modifier.GetBool(11, true) ? AudioManager.inst.CurrentAudioSource.time + modifier.GetFloat(10, 0f) : modifier.GetFloat(10, 0f),
                    new Vector2(posX, posY) + (Vector2)animationResult.position,
                    new Vector2(scaX, scaY) * animationResult.scale,
                    rot + animationResult.rotation, repeatCount, repeatOffsetTime, speed);

                modifier.Result = prefabObject;
                GameData.Current.prefabObjects.Add(prefabObject);
                Updater.AddPrefabToLevel(prefabObject);
            },
            "spawnPrefabOffsetOther" => modifier =>
            {
                if (modifier.constant || modifier.HasResult())
                    return;

                var prefab = GetPrefab(modifier.GetInt(13, 0), modifier.GetValue(0));

                if (!prefab)
                    return;

                if (GameData.Current.TryFindObjectWithTag(modifier, modifier.GetValue(10), out BeatmapObject beatmapObject))
                {
                    var animationResult = beatmapObject.InterpolateChain();

                    var posX = modifier.GetFloat(1, 0f);
                    var posY = modifier.GetFloat(2, 0f);
                    var scaX = modifier.GetFloat(3, 0f);
                    var scaY = modifier.GetFloat(4, 0f);
                    var rot = modifier.GetFloat(5, 0f);
                    var repeatCount = modifier.GetInt(6, 0);
                    var repeatOffsetTime = modifier.GetFloat(7, 0f);
                    var speed = modifier.GetFloat(8, 0f);

                    var prefabObject = AddPrefabObjectToLevel(prefab,
                        modifier.GetBool(12, true) ? AudioManager.inst.CurrentAudioSource.time + modifier.GetFloat(11, 0f) : modifier.GetFloat(11, 0f),
                        new Vector2(posX, posY) + (Vector2)animationResult.position,
                        new Vector2(scaX, scaY) * animationResult.scale,
                        rot + animationResult.rotation, repeatCount, repeatOffsetTime, speed);

                    modifier.Result = prefabObject;
                    GameData.Current.prefabObjects.Add(prefabObject);
                    Updater.AddPrefabToLevel(prefabObject);
                }
            },
            "spawnMultiPrefab" => modifier =>
            {
                if (modifier.constant)
                    return;

                var prefab = GetPrefab(modifier.GetInt(11, 0), modifier.GetValue(0));

                if (!prefab)
                    return;

                var posX = modifier.GetFloat(1, 0f);
                var posY = modifier.GetFloat(2, 0f);
                var scaX = modifier.GetFloat(3, 0f);
                var scaY = modifier.GetFloat(4, 0f);
                var rot = modifier.GetFloat(5, 0f);
                var repeatCount = modifier.GetInt(6, 0);
                var repeatOffsetTime = modifier.GetFloat(7, 0f);
                var speed = modifier.GetFloat(8, 0f);

                if (!modifier.HasResult())
                    modifier.Result = new List<PrefabObject>();

                var list = modifier.GetResult<List<PrefabObject>>();
                var prefabObject = AddPrefabObjectToLevel(prefab,
                    modifier.GetBool(10, true) ? AudioManager.inst.CurrentAudioSource.time + modifier.GetFloat(9, 0f) : modifier.GetFloat(9, 0f),
                    new Vector2(posX, posY),
                    new Vector2(scaX, scaY),
                    rot, repeatCount, repeatOffsetTime, speed);

                list.Add(prefabObject);
                modifier.Result = list;

                GameData.Current.prefabObjects.Add(prefabObject);
                Updater.AddPrefabToLevel(prefabObject);
            },
            "spawnMultiPrefabOffset" => modifier =>
            {
                if (modifier.constant)
                    return;

                var prefab = GetPrefab(modifier.GetInt(11, 0), modifier.GetValue(0));

                if (!prefab)
                    return;

                var animationResult = modifier.reference.InterpolateChain();

                var posX = modifier.GetFloat(1, 0f);
                var posY = modifier.GetFloat(2, 0f);
                var scaX = modifier.GetFloat(3, 0f);
                var scaY = modifier.GetFloat(4, 0f);
                var rot = modifier.GetFloat(5, 0f);
                var repeatCount = modifier.GetInt(6, 0);
                var repeatOffsetTime = modifier.GetFloat(7, 0f);
                var speed = modifier.GetFloat(8, 0f);

                if (!modifier.HasResult())
                    modifier.Result = new List<PrefabObject>();

                var list = modifier.GetResult<List<PrefabObject>>();
                var prefabObject = AddPrefabObjectToLevel(prefab,
                    modifier.GetBool(10, true) ? AudioManager.inst.CurrentAudioSource.time + modifier.GetFloat(9, 0f) : modifier.GetFloat(9, 0f),
                    new Vector2(posX, posY) + (Vector2)animationResult.position,
                    new Vector2(scaX, scaY) * animationResult.scale,
                    rot + animationResult.rotation, repeatCount, repeatOffsetTime, speed);

                list.Add(prefabObject);
                modifier.Result = list;

                GameData.Current.prefabObjects.Add(prefabObject);
                Updater.AddPrefabToLevel(prefabObject);
            },
            "spawnMultiPrefabOffsetOther" => modifier =>
            {
                if (modifier.constant)
                    return;

                var prefab = GetPrefab(modifier.GetInt(12, 0), modifier.GetValue(0));

                if (!prefab)
                    return;

                if (GameData.Current.TryFindObjectWithTag(modifier, modifier.commands[9], out BeatmapObject beatmapObject))
                {
                    var animationResult = beatmapObject.InterpolateChain();

                    var posX = modifier.GetFloat(1, 0f);
                    var posY = modifier.GetFloat(2, 0f);
                    var scaX = modifier.GetFloat(3, 0f);
                    var scaY = modifier.GetFloat(4, 0f);
                    var rot = modifier.GetFloat(5, 0f);
                    var repeatCount = modifier.GetInt(6, 0);
                    var repeatOffsetTime = modifier.GetFloat(7, 0f);
                    var speed = modifier.GetFloat(8, 0f);

                    if (!modifier.HasResult())
                        modifier.Result = new List<PrefabObject>();

                    var list = modifier.GetResult<List<PrefabObject>>();
                    var prefabObject = AddPrefabObjectToLevel(prefab,
                        modifier.GetBool(11, true) ? AudioManager.inst.CurrentAudioSource.time + modifier.GetFloat(10, 0f) : modifier.GetFloat(10, 0f),
                        new Vector2(posX, posY) + (Vector2)animationResult.position,
                        new Vector2(scaX, scaY) * animationResult.scale,
                        rot + animationResult.rotation, repeatCount, repeatOffsetTime, speed);

                    list.Add(prefabObject);
                    modifier.Result = list;

                    GameData.Current.prefabObjects.Add(prefabObject);
                    Updater.AddPrefabToLevel(prefabObject);
                }
            },
            "clearSpawnedPrefabs" => modifier =>
            {
                var list = !modifier.prefabInstanceOnly ? GameData.Current.FindObjectsWithTag(modifier.value) : GameData.Current.FindObjectsWithTag(modifier.reference, modifier.value);

                for (int i = 0; i < list.Count; i++)
                {
                    var beatmapObject = list[i];

                    for (int j = 0; j < beatmapObject.modifiers.Count; j++)
                    {
                        var otherModifier = beatmapObject.modifiers[j];

                        if (otherModifier.TryGetResult(out PrefabObject prefabObjectResult))
                        {
                            Updater.UpdatePrefab(prefabObjectResult, false);

                            GameData.Current.prefabObjects.RemoveAll(x => x.fromModifier && x.id == prefabObjectResult.id);

                            otherModifier.Result = null;
                            continue;
                        }

                        if (!otherModifier.TryGetResult(out List<PrefabObject> result))
                            continue;

                        for (int k = 0; k < result.Count; k++)
                        {
                            var prefabObject = result[k];

                            Updater.UpdatePrefab(prefabObject, false);
                            GameData.Current.prefabObjects.RemoveAll(x => x.fromModifier && x.id == prefabObject.id);
                        }

                        result.Clear();
                        otherModifier.Result = null;
                    }
                }
            },

            #endregion

            #region Ranking

            "saveLevelRank" => modifier =>
            {
                if (CoreHelper.InEditor || modifier.constant || !LevelManager.CurrentLevel)
                    return;

                LevelManager.UpdateCurrentLevelProgress();
            },

            "clearHits" => modifier =>
            {
                if (!CoreHelper.InEditor) // hit and death counters are not supported in the editor yet.
                    GameManager.inst.hits.Clear();
            },
            "addHit" => modifier =>
            {
                if (CoreHelper.InEditor)
                    return;

                var vector = Vector3.zero;
                if (modifier.GetBool(0, true))
                    vector = modifier.reference.InterpolateChainPosition();
                else
                {
                    var player = PlayerManager.GetClosestPlayer(modifier.reference.InterpolateChainPosition());
                    if (player && player.Player)
                        vector = player.Player.rb.position;
                }

                float time = AudioManager.inst.CurrentAudioSource.time;
                if (!string.IsNullOrEmpty(modifier.GetValue(1)))
                    time = RTMath.Parse(modifier.GetValue(1), modifier.reference.GetObjectVariables(), modifier.reference.GetObjectFunctions());

                GameManager.inst.hits.Add(new SaveManager.SaveGroup.Save.PlayerDataPoint(vector, GameManager.inst.UpcomingCheckpointIndex, time));
            },
            "subHit" => modifier =>
            {
                if (!CoreHelper.InEditor && !GameManager.inst.hits.IsEmpty())
                    GameManager.inst.hits.RemoveAt(GameManager.inst.hits.Count - 1);
            },
            "clearDeaths" => modifier =>
            {
                if (!CoreHelper.InEditor)
                    GameManager.inst.deaths.Clear();
            },
            "addDeath" => modifier =>
            {
                if (CoreHelper.InEditor)
                    return;

                var vector = Vector3.zero;
                if (modifier.GetBool(0, true))
                    vector = modifier.reference.InterpolateChainPosition();
                else
                {
                    var player = PlayerManager.GetClosestPlayer(modifier.reference.InterpolateChainPosition());
                    if (player && player.Player)
                        vector = player.Player.rb.position;
                }

                float time = AudioManager.inst.CurrentAudioSource.time;
                if (!string.IsNullOrEmpty(modifier.GetValue(1)))
                    time = RTMath.Parse(modifier.GetValue(1), modifier.reference.GetObjectVariables(), modifier.reference.GetObjectFunctions());

                GameManager.inst.deaths.Add(new SaveManager.SaveGroup.Save.PlayerDataPoint(vector, GameManager.inst.UpcomingCheckpointIndex, time));
            },
            "subDeath" => modifier =>
            {
                if (!CoreHelper.InEditor && !GameManager.inst.deaths.IsEmpty())
                    GameManager.inst.deaths.RemoveAt(GameManager.inst.deaths.Count - 1);
            },

            #endregion

            #region Updates

            // update
            "updateObjects" => modifier =>
            {
                if (!modifier.constant)
                    CoroutineHelper.StartCoroutine(Updater.IUpdateObjects(true));
            },
            "updateObject" => modifier =>
            {
                var list = !modifier.prefabInstanceOnly ? GameData.Current.FindObjectsWithTag(modifier.value) : GameData.Current.FindObjectsWithTag(modifier.reference, modifier.value);

                if (!modifier.constant && !list.IsEmpty())
                {
                    foreach (var bm in list)
                        Updater.UpdateObject(bm, recalculate: false);
                    Updater.RecalculateObjectStates();
                }
            },

            // parent
            "setParent" => modifier =>
            {
                if (modifier.constant)
                    return;

                if (modifier.GetValue(0) == string.Empty)
                    SetParent(modifier.reference, string.Empty);
                else if (GameData.Current.TryFindObjectWithTag(modifier, modifier.GetValue(0), out BeatmapObject beatmapObject) && modifier.reference.CanParent(beatmapObject))
                    SetParent(modifier.reference, beatmapObject.id);
                else
                    CoreHelper.LogError($"CANNOT PARENT OBJECT!\nName: {modifier.reference.name}\nID: {modifier.reference.id}");
            },
            "setParentOther" => modifier =>
            {
                if (modifier.constant)
                    return;

                var reference = modifier.reference;

                if (!string.IsNullOrEmpty(modifier.GetValue(2)) && GameData.Current.TryFindObjectWithTag(modifier, modifier.GetValue(2), out BeatmapObject beatmapObject))
                    reference = beatmapObject;

                var list = !modifier.prefabInstanceOnly ? GameData.Current.FindObjectsWithTag(modifier.GetValue(0)) : GameData.Current.FindObjectsWithTag(modifier.reference, modifier.GetValue(0));

                var isEmpty = modifier.GetBool(1, false);

                bool failed = false;
                list.ForLoop(beatmapObject =>
                {
                    if (isEmpty)
                        SetParent(beatmapObject, string.Empty);
                    else if (beatmapObject.CanParent(reference))
                        SetParent(beatmapObject, reference.id);
                    else
                        failed = true;
                });

                if (failed)
                    CoreHelper.LogError($"CANNOT PARENT OBJECT!\nName: {modifier.reference.name}\nID: {modifier.reference.id}");
            }
            ,

            #endregion

            #region Misc

            "quitToMenu" => modifier =>
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
            },
            "quitToArcade" => modifier =>
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
            },
            "blackHole" => modifier =>
            {
                if (!modifier.reference)
                    return;

                float p = Time.deltaTime * 60f * CoreHelper.ForwardPitch;

                float num = Parser.TryParse(modifier.value, 0.01f);

                if (Parser.TryParse(modifier.commands[1], false))
                    num = -(modifier.reference.Interpolate(3, 1) - 1f) * num;

                if (num == 0f)
                    return;

                float moveDelay = 1f - Mathf.Pow(1f - Mathf.Clamp(num, 0.001f, 1f), p);
                var players = PlayerManager.Players;

                if (!Updater.TryGetObject(modifier.reference, out LevelObject levelObject) || !levelObject.visualObject.gameObject)
                {
                    var position = modifier.reference.InterpolateChainPosition();

                    for (int i = 0; i < players.Count; i++)
                    {
                        var player = players[i];
                        if (!player.Player || !player.Player.rb)
                            continue;

                        var transform = player.Player.rb.transform;

                        var vector = new Vector3(transform.position.x, transform.position.y, 0f);
                        var target = new Vector3(position.x, position.y, 0f);

                        transform.position += (target - vector) * moveDelay;
                    }

                    return;
                }

                var gm = levelObject.visualObject.gameObject;

                for (int i = 0; i < players.Count; i++)
                {
                    var player = players[i];
                    if (!player.Player || !player.Player.rb)
                        continue;

                    var transform = player.Player.rb.transform;

                    var vector = new Vector3(transform.position.x, transform.position.y, 0f);
                    var target = new Vector3(gm.transform.position.x, gm.transform.position.y, 0f);

                    transform.position += (target - vector) * moveDelay;
                }
            },

            // collision
            "setCollision" => modifier =>
            {
                if (Updater.TryGetObject(modifier.reference, out LevelObject levelObject) && levelObject.visualObject != null && levelObject.visualObject.collider)
                    levelObject.visualObject.colliderEnabled = Parser.TryParse(modifier.value, false);
            },
            "setCollisionOther" => modifier =>
            {
                var list = !modifier.prefabInstanceOnly ? GameData.Current.FindObjectsWithTag(modifier.commands[1]) : GameData.Current.FindObjectsWithTag(modifier.reference, modifier.commands[1]);

                foreach (var beatmapObject in list)
                {
                    if (Updater.TryGetObject(beatmapObject, out LevelObject levelObject) && levelObject.visualObject != null && levelObject.visualObject.collider)
                        levelObject.visualObject.colliderEnabled = Parser.TryParse(modifier.value, false);
                }
            },

            // activation
            "signalModifier" => modifier =>
            {
                var list = !modifier.prefabInstanceOnly ? GameData.Current.FindObjectsWithTag(modifier.commands[1]) : GameData.Current.FindObjectsWithTag(modifier.reference, modifier.commands[1]);

                foreach (var bm in list)
                    CoroutineHelper.StartCoroutine(ActivateModifier(bm, Parser.TryParse(modifier.value, 0f)));
            },
            "activateModifier" => modifier =>
            {
                var list = !modifier.prefabInstanceOnly ? GameData.Current.FindObjectsWithTag(modifier.value) : GameData.Current.FindObjectsWithTag(modifier.reference, modifier.value);

                var doMultiple = Parser.TryParse(modifier.commands[1], true);
                var index = Parser.TryParse(modifier.commands[2], -1);

                // 3 is modifier names
                var modifierNames = new List<string>();
                for (int i = 3; i < modifier.commands.Count; i++)
                    modifierNames.Add(modifier.commands[i]);

                for (int i = 0; i < list.Count; i++)
                {
                    if (doMultiple)
                    {
                        var modifiers = list[i].modifiers.FindAll(x => x.type == ModifierBase.Type.Action && modifierNames.Contains(x.commands[0]));

                        for (int j = 0; j < modifiers.Count; j++)
                        {
                            var otherModifier = modifiers[i];
                            otherModifier.Action?.Invoke(otherModifier);
                        }
                        continue;
                    }

                    if (index >= 0 && index < list[i].modifiers.Count)
                    {
                        var otherModifier = list[i].modifiers[index];
                        otherModifier.Action?.Invoke(otherModifier);
                    }
                }
            },

            "editorNotify" => modifier =>
            {
                if (CoreHelper.InEditor)
                    EditorManager.inst.DisplayNotification(modifier.value, Parser.TryParse(modifier.commands[1], 0.5f), (EditorManager.NotificationType)Parser.TryParse(modifier.commands[2], 0));
            },

            // external
            "setWindowTitle" => modifier =>
            {
                WindowController.SetTitle(modifier.value);
            },
            "setDiscordStatus" => modifier =>
            {
                string[] discordSubIcons = new string[]
                {
                    "arcade",
                    "editor",
                    "play",
                    "menu",
                };

                string[] discordIcons = new string[]
                {
                    "pa_logo_white",
                    "pa_logo_black",
                };

                if (int.TryParse(modifier.commands[2], out int discordSubIcon) && int.TryParse(modifier.commands[3], out int discordIcon))
                    CoreHelper.UpdateDiscordStatus(
                        string.Format(modifier.value, MetaData.Current.song.title, $"{(!CoreHelper.InEditor ? "Game" : "Editor")}", $"{(!CoreHelper.InEditor ? "Level" : "Editing")}", $"{(!CoreHelper.InEditor ? "Arcade" : "Editor")}"),
                        string.Format(modifier.commands[1], MetaData.Current.song.title, $"{(!CoreHelper.InEditor ? "Game" : "Editor")}", $"{(!CoreHelper.InEditor ? "Level" : "Editing")}", $"{(!CoreHelper.InEditor ? "Arcade" : "Editor")}"),
                        discordSubIcons[Mathf.Clamp(discordSubIcon, 0, discordSubIcons.Length - 1)], discordIcons[Mathf.Clamp(discordIcon, 0, discordIcons.Length - 1)]);
            },
            "loadInterface" => modifier =>
            {
                if (CoreHelper.IsEditing) // don't want interfaces to load in editor
                {
                    EditorManager.inst.DisplayNotification($"Cannot load interface in the editor!", 1f, EditorManager.NotificationType.Warning);
                    return;
                }

                var path = RTFile.CombinePaths(RTFile.BasePath, modifier.value + FileFormat.LSI.Dot());

                if (!RTFile.FileExists(path))
                {
                    CoreHelper.LogError($"Interface with file name: \"{modifier.value}\" does not exist.");
                    return;
                }

                var menu = CustomMenu.Parse(JSON.Parse(RTFile.ReadFromFile(path)));

                menu.filePath = path;

                if (string.IsNullOrEmpty(menu.id) || menu.id == "0")
                {
                    CoreHelper.LogError($"Menu ID cannot be empty nor 0.");
                    return;
                }

                InterfaceManager.inst.MainDirectory = RTFile.BasePath;

                AudioManager.inst.CurrentAudioSource.Pause();
                InputDataManager.inst.SetAllControllerRumble(0f);
                GameManager.inst.gameState = GameManager.State.Paused;
                ArcadeHelper.endedLevel = false;

                if (InterfaceManager.inst.interfaces.TryFind(x => x.id == menu.id, out MenuBase otherMenu))
                {
                    InterfaceManager.inst.SetCurrentInterface(otherMenu);
                    menu = null;
                    return;
                }

                InterfaceManager.inst.interfaces.Add(menu);
                InterfaceManager.inst.SetCurrentInterface(menu);
            },

            "unlockAchievement" => modifier =>
            {

            }, // todo: implement

            "pauseLevel" => modifier =>
            {
                if (CoreHelper.InEditor)
                {
                    EditorManager.inst.DisplayNotification("Cannot pause in the editor. This modifier only works in the Arcade.", 3f, EditorManager.NotificationType.Warning);
                    return;
                }

                PauseMenu.Pause();
            },

            #endregion

            // dev only (story mode)
            #region Dev Only

            "loadSceneDEVONLY" => modifier =>
            {
                if (!CoreHelper.InStory)
                    return;

                SceneManager.inst.LoadScene(modifier.value, modifier.commands.Count > 1 && Parser.TryParse(modifier.commands[1], true));
            },
            "loadStoryLevelDEVONLY" => modifier =>
            {
                if (!CoreHelper.InStory)
                    return;

                Story.StoryManager.inst.Play(modifier.GetInt(1, 0), modifier.GetInt(2, 0), modifier.GetBool(0, false), modifier.GetBool(3, false));
            },
            "storySaveIntVariableDEVONLY" => modifier =>
            {
                if (!CoreHelper.InStory)
                    return;

                Story.StoryManager.inst.SaveInt(modifier.GetValue(0), modifier.reference.integerVariable);
            },
            "storySaveIntDEVONLY" => modifier =>
            {
                if (!CoreHelper.InStory)
                    return;

                Story.StoryManager.inst.SaveInt(modifier.GetValue(0), modifier.GetInt(1, 0));
            },
            "storySaveBoolDEVONLY" => modifier =>
            {
                if (!CoreHelper.InStory)
                    return;

                Story.StoryManager.inst.SaveBool(modifier.GetValue(0), modifier.GetBool(1, false));
            },
            "exampleEnableDEVONLY" => modifier =>
            {
                if (Companion.Entity.Example.Current && Companion.Entity.Example.Current.model)
                    Companion.Entity.Example.Current.model.SetActive(modifier.GetBool(0, false));
            },
            "exampleSayDEVONLY" => modifier =>
            {
                if (Companion.Entity.Example.Current && Companion.Entity.Example.Current.chatBubble)
                    Companion.Entity.Example.Current.chatBubble.Say(modifier.GetValue(0));
            },

            #endregion
        };

        /// <summary>
        /// The function to run when a modifier is inactive and has a reference of <see cref="BeatmapObject"/>.
        /// </summary>
        /// <param name="modifier">Modifier to run the inactive function of.</param>
        public static void ObjectInactive(Modifier<BeatmapObject> modifier)
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
                                Updater.TryGetObject(modifier.reference, out LevelObject levelObject) &&
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
                                Updater.TryGetObject(modifier.reference, out LevelObject levelObject) &&
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
                                Updater.UpdatePrefab(prefabObject, false);

                                GameData.Current.prefabObjects.RemoveAll(x => x.fromModifier && x.id == prefabObject.id);

                                modifier.Result = null;
                            }
                            break;
                        }

                    case "enableObject": {
                            if (Updater.TryGetObject(modifier.reference, out LevelObject levelObject) && levelObject.top && (modifier.commands.Count == 1 || Parser.TryParse(modifier.commands[1], true)))
                                levelObject.top.gameObject.SetActive(false);

                            break;
                        }
                    case "enableObjectOther": {
                            var list = !modifier.prefabInstanceOnly ? GameData.Current.FindObjectsWithTag(modifier.value) : GameData.Current.FindObjectsWithTag(modifier.reference, modifier.value);

                            if (!list.IsEmpty() && (modifier.commands.Count == 1 || Parser.TryParse(modifier.commands[1], true)))
                            {
                                foreach (var beatmapObject in list)
                                {
                                    if (Updater.TryGetObject(beatmapObject, out LevelObject levelObject) && levelObject.top)
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
                                if (Updater.TryGetObject(beatmapObject, out LevelObject levelObject) && levelObject.top)
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
                                var beatmapObjects = !modifier.prefabInstanceOnly ? GameData.Current.FindObjectsWithTag(modifier.commands[1]) : GameData.Current.FindObjectsWithTag(modifier.reference, modifier.commands[1]);

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
                                if (Updater.TryGetObject(beatmapObject, out LevelObject levelObject) && levelObject.top)
                                    levelObject.top.gameObject.SetActive(false);
                            }

                            modifier.Result = null;

                            break;
                        }
                    case "disableObject": {
                            if (!modifier.hasChanged && modifier.reference != null && Updater.TryGetObject(modifier.reference, out LevelObject levelObject) && levelObject.top && (modifier.commands.Count == 1 || Parser.TryParse(modifier.commands[1], true)))
                            {
                                levelObject.top.gameObject.SetActive(true);
                                modifier.hasChanged = true;
                            }

                            break;
                        }
                    case "disableObjectOther": {
                            var list = !modifier.prefabInstanceOnly ? GameData.Current.FindObjectsWithTag(modifier.value) : GameData.Current.FindObjectsWithTag(modifier.reference, modifier.value);

                            if (!list.IsEmpty() && (modifier.commands.Count == 1 || Parser.TryParse(modifier.commands[1], true)))
                            {
                                foreach (var beatmapObject in list)
                                {
                                    if (Updater.TryGetObject(beatmapObject, out LevelObject levelObject) && levelObject.top)
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
                                if (Updater.TryGetObject(beatmapObject, out LevelObject levelObject) && levelObject.top)
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
                                var beatmapObjects = !modifier.prefabInstanceOnly ? GameData.Current.FindObjectsWithTag(modifier.commands[1]) : GameData.Current.FindObjectsWithTag(modifier.reference, modifier.commands[1]);

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
                                if (Updater.TryGetObject(beatmapObject, out LevelObject levelObject) && levelObject.top)
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
                            var list = !modifier.prefabInstanceOnly ? GameData.Current.FindObjectsWithTag(modifier.commands[1]) : GameData.Current.FindObjectsWithTag(modifier.reference, modifier.commands[1]);

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
                            var list = !modifier.prefabInstanceOnly ? GameData.Current.FindObjectsWithTag(modifier.commands[groupIndex]) : GameData.Current.FindObjectsWithTag(modifier.reference, modifier.commands[groupIndex]);

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
                            if (modifier.constant && modifier.reference.shape == 4 && modifier.reference.levelObject && modifier.reference.levelObject.visualObject != null &&
                                modifier.reference.levelObject.visualObject is TextObject textObject)
                                textObject.text = modifier.reference.text;
                            break;
                        }
                    case "setTextOther": {
                            var list = !modifier.prefabInstanceOnly ? GameData.Current.FindObjectsWithTag(modifier.commands[1]) : GameData.Current.FindObjectsWithTag(modifier.reference, modifier.commands[1]);

                            if (modifier.constant && !list.IsEmpty())
                                foreach (var bm in list)
                                    if (bm.shape == 4 && bm.levelObject && bm.levelObject.visualObject != null &&
                                        bm.levelObject.visualObject is TextObject textObject)
                                        textObject.text = bm.text;
                            break;
                        }
                    case "textSequence": {
                            modifier.setTimer = false;
                            break;
                        }
                    case "copyAxis":
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

        /// <summary>
        /// The function to run when a <see cref="ModifierBase.Type.Trigger"/> modifier is running and has a reference of <see cref="BackgroundObject"/>.
        /// </summary>
        /// <param name="modifier">Modifier to run.</param>
        /// <returns>Returns true if the modifier was triggered, otherwise returns false.</returns>
        public static bool BGTrigger(Modifier<BackgroundObject> modifier)
        {
            if (!modifier.verified)
            {
                modifier.verified = true;
                modifier.VerifyModifier(ModifiersManager.defaultBackgroundObjectModifiers);
            }

            if (modifier.commands.IsEmpty())
                return false;

            switch (modifier.Name)
            {
                case "timeLesserEquals": {
                        return float.TryParse(modifier.value, out float t) && AudioManager.inst.CurrentAudioSource.time <= t;
                    }
                case "timeGreaterEquals": {
                        return float.TryParse(modifier.value, out float t) && AudioManager.inst.CurrentAudioSource.time >= t;
                    }
                case "timeLesser": {
                        return float.TryParse(modifier.value, out float t) && AudioManager.inst.CurrentAudioSource.time < t;
                    }
                case "timeGreater": {
                        return float.TryParse(modifier.value, out float t) && AudioManager.inst.CurrentAudioSource.time > t;
                    }
            }

            return false;
        }

        /// <summary>
        /// The function to run when a <see cref="ModifierBase.Type.Action"/> modifier is running and has a reference of <see cref="BackgroundObject"/>.
        /// </summary>
        /// <param name="modifier">Modifier to run.</param>
        public static void BGAction(Modifier<BackgroundObject> modifier)
        {
            if (!modifier.verified)
            {
                modifier.verified = true;
                modifier.VerifyModifier(ModifiersManager.defaultBackgroundObjectModifiers);
            }

            if (modifier.commands.IsEmpty())
                return;

            modifier.hasChanged = false;
            switch (modifier.Name)
            {
                case "setActive": {
                        if (bool.TryParse(modifier.value, out bool active))
                            modifier.reference.Enabled = active;

                        break;
                    }
                case "setActiveOther": {
                        if (!bool.TryParse(modifier.value, out bool active))
                            break;

                        var list = GameData.Current.backgroundObjects.FindAll(x => x.tags.Contains(modifier.commands[1]));
                        if (!list.IsEmpty())
                            for (int i = 0; i < list.Count; i++)
                                list[i].Enabled = active;

                        break;
                    }
                case "animateObject": {
                        if (int.TryParse(modifier.commands[1], out int type)
                            && float.TryParse(modifier.commands[2], out float x) && float.TryParse(modifier.commands[3], out float y) && float.TryParse(modifier.commands[4], out float z)
                            && bool.TryParse(modifier.commands[5], out bool relative) && float.TryParse(modifier.value, out float time))
                        {
                            string easing = modifier.commands[6];
                            if (int.TryParse(modifier.commands[6], out int e) && e >= 0 && e < DataManager.inst.AnimationList.Count)
                                easing = DataManager.inst.AnimationList[e].Name;

                            Vector3 vector = type switch
                            {
                                0 => modifier.reference.positionOffset,
                                1 => modifier.reference.scaleOffset,
                                _ => modifier.reference.rotationOffset,
                            };

                            var setVector = new Vector3(x, y, z);
                            if (relative)
                            {
                                if (modifier.constant)
                                    setVector *= CoreHelper.TimeFrame;

                                setVector += vector;
                            }

                            if (!modifier.constant)
                            {
                                var animation = new RTAnimation("Animate BG Object Offset");

                                animation.animationHandlers = new List<AnimationHandlerBase>
                                {
                                    new AnimationHandler<Vector3>(new List<IKeyframe<Vector3>>
                                    {
                                        new Vector3Keyframe(0f, vector, Ease.Linear),
                                        new Vector3Keyframe(Mathf.Clamp(time, 0f, 9999f), setVector, Ease.HasEaseFunction(easing) ? Ease.GetEaseFunction(easing) : Ease.Linear),
                                    }, vector3 => modifier.reference.SetTransform(type, vector3)),
                                };
                                animation.onComplete = () =>
                                {
                                    AnimationManager.inst.Remove(animation.id);
                                    modifier.reference.SetTransform(type, setVector);
                                };
                                AnimationManager.inst.Play(animation);
                            }
                            else
                                modifier.reference.SetTransform(type, setVector);
                        }

                        break;
                    }
                case "animateObjectOther": {
                        if (int.TryParse(modifier.commands[1], out int type)
                            && float.TryParse(modifier.commands[2], out float x) && float.TryParse(modifier.commands[3], out float y) && float.TryParse(modifier.commands[4], out float z)
                            && bool.TryParse(modifier.commands[5], out bool relative) && float.TryParse(modifier.value, out float time))
                        {
                            string easing = modifier.commands[6];
                            if (int.TryParse(modifier.commands[6], out int e) && e >= 0 && e < DataManager.inst.AnimationList.Count)
                                easing = DataManager.inst.AnimationList[e].Name;

                            var list = GameData.Current.backgroundObjects.FindAll(x => x.tags.Contains(modifier.commands[7]));

                            if (list.Count <= 0)
                                break;

                            for (int i = 0; i < list.Count; i++)
                            {
                                var bg = list[i];

                                Vector3 vector = type switch
                                {
                                    0 => bg.positionOffset,
                                    1 => bg.scaleOffset,
                                    _ => bg.rotationOffset,
                                };

                                var setVector = new Vector3(x, y, z);
                                if (relative)
                                {
                                    if (modifier.constant)
                                        setVector *= CoreHelper.TimeFrame;

                                    setVector += vector;
                                }

                                if (!modifier.constant)
                                {
                                    var animation = new RTAnimation("Animate BG Object Offset");

                                    animation.animationHandlers = new List<AnimationHandlerBase>
                                    {
                                        new AnimationHandler<Vector3>(new List<IKeyframe<Vector3>>
                                        {
                                            new Vector3Keyframe(0f, vector, Ease.Linear),
                                            new Vector3Keyframe(Mathf.Clamp(time, 0f, 9999f), setVector, Ease.HasEaseFunction(easing) ? Ease.GetEaseFunction(easing) : Ease.Linear),
                                        }, vector3 => bg.SetTransform(type, vector3)),
                                    };
                                    animation.onComplete = () =>
                                    {
                                        AnimationManager.inst.Remove(animation.id);
                                        bg.SetTransform(type, setVector);
                                    };
                                    AnimationManager.inst.Play(animation);
                                }
                                else
                                    bg.SetTransform(type, setVector);
                            }
                        }

                        break;
                    }
                case "copyAxis": {
                        /*
                        From Type: (Pos / Sca / Rot)
                        From Axis: (X / Y / Z)
                        Object Group
                        To Type: (Pos / Sca / Rot)
                        To Axis: (X / Y / Z)
                        */

                        if (int.TryParse(modifier.commands[1], out int fromType) && int.TryParse(modifier.commands[2], out int fromAxis)
                            && int.TryParse(modifier.commands[3], out int toType) && int.TryParse(modifier.commands[4], out int toAxis)
                            && float.TryParse(modifier.commands[5], out float delay) && float.TryParse(modifier.commands[6], out float multiply)
                            && float.TryParse(modifier.commands[7], out float offset) && float.TryParse(modifier.commands[8], out float min) && float.TryParse(modifier.commands[9], out float max)
                            && float.TryParse(modifier.commands[10], out float loop)
                            && GameData.Current.beatmapObjects.TryFind(x => x.tags.Contains(modifier.value), out BeatmapObject bm))
                        {
                            var time = Updater.CurrentTime;

                            fromType = Mathf.Clamp(fromType, 0, bm.events.Count);
                            fromAxis = Mathf.Clamp(fromAxis, 0, bm.events[fromType][0].values.Length);

                            if (Updater.levelProcessor.converter.cachedSequences.TryGetValue(bm.id, out ObjectConverter.CachedSequences cachedSequence))
                            {
                                switch (fromType)
                                {
                                    case 0:
                                        {
                                            var sequence = cachedSequence.PositionSequence.Interpolate(time - bm.StartTime - delay);
                                            float value = ((fromAxis == 0 ? sequence.x : fromAxis == 1 ? sequence.y : sequence.z) - offset) * multiply % loop;

                                            modifier.reference.SetTransform(toType, toAxis, Mathf.Clamp(value, min, max));
                                            break;
                                        }
                                    case 1:
                                        {
                                            var sequence = cachedSequence.ScaleSequence.Interpolate(time - bm.StartTime - delay);
                                            float value = ((fromAxis == 0 ? sequence.x : sequence.y) - offset) * multiply % loop;

                                            modifier.reference.SetTransform(toType, toAxis, Mathf.Clamp(value, min, max));
                                            break;
                                        }
                                    case 2:
                                        {
                                            var sequence = (cachedSequence.RotationSequence.Interpolate(time - bm.StartTime - delay) - offset) * multiply % loop;

                                            modifier.reference.SetTransform(toType, toAxis, Mathf.Clamp(sequence, min, max));
                                            break;
                                        }
                                }
                            }
                        }

                        break;
                    }
            }
        }

        /// <summary>
        /// The function to run when a modifier is inactive and has a reference of <see cref="BackgroundObject"/>.
        /// </summary>
        /// <param name="modifier">Modifier to run the inactive function of.</param>
        public static void BGInactive(Modifier<BackgroundObject> modifier)
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
        public static bool PlayerTrigger(Modifier<CustomPlayer> modifier)
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
        public static void PlayerAction(Modifier<CustomPlayer> modifier)
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
                        var list = GameData.Current.FindObjectsWithTag(modifier.commands[1]);

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
        public static void PlayerInactive(Modifier<CustomPlayer> modifier)
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

        static void GetSoundPath(string id, string path, bool fromSoundLibrary = false, float pitch = 1f, float volume = 1f, bool loop = false)
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

        static void DownloadSoundAndPlay(string id, string path, float pitch = 1f, float volume = 1f, bool loop = false)
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

        static void PlaySound(string id, AudioClip clip, float pitch, float volume, bool loop)
        {
            var audioSource = SoundManager.inst.PlaySound(clip, volume, pitch * AudioManager.inst.CurrentAudioSource.pitch, loop);
            if (loop && !ModifiersManager.audioSources.ContainsKey(id))
                ModifiersManager.audioSources.Add(id, audioSource);
        }

        static IEnumerator LoadMusicFileRaw(string path, Action<AudioClip> callback)
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

        static PrefabObject AddPrefabObjectToLevel(Prefab prefab, float startTime, Vector2 pos, Vector2 sca, float rot, int repeatCount, float repeatOffsetTime, float speed)
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

        static void SaveProgress(string path, string chapter, string level, float data)
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

        static void SaveProgress(string path, string chapter, string level, string data)
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

        static IEnumerator ActivateModifier(BeatmapObject beatmapObject, float delay)
        {
            if (delay != 0.0)
                yield return CoroutineHelper.Seconds(delay);

            if (beatmapObject.modifiers.TryFind(x => x.commands[0] == "requireSignal" && x.type == ModifierBase.Type.Trigger, out Modifier<BeatmapObject> modifier))
                modifier.Result = "death hd";
            yield break;
        }

        static void ApplyAnimationTo(
            BeatmapObject applyTo, BeatmapObject takeFrom,
            bool useVisual, float time, float currentTime,
            bool animatePos, bool animateSca, bool animateRot,
            float delayPos, float delaySca, float delayRot)
        {
            if (!useVisual && Updater.levelProcessor.converter.cachedSequences.TryGetValue(takeFrom.id, out ObjectConverter.CachedSequences cachedSequences))
            {
                // Animate position
                if (animatePos)
                    applyTo.positionOffset = cachedSequences.PositionSequence.Interpolate(currentTime - time - delayPos);

                // Animate scale
                if (animateSca)
                {
                    var scaleSequence = cachedSequences.ScaleSequence.Interpolate(currentTime - time - delaySca);
                    applyTo.scaleOffset = new Vector3(scaleSequence.x - 1f, scaleSequence.y - 1f, 0f);
                }

                // Animate rotation
                if (animateRot)
                    applyTo.rotationOffset = new Vector3(0f, 0f, cachedSequences.RotationSequence.Interpolate(currentTime - time - delayRot));
            }
            else if (useVisual && Updater.TryGetObject(takeFrom, out LevelObject levelObject) && levelObject.visualObject != null && levelObject.visualObject.gameObject)
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

        static float GetAnimation(BeatmapObject bm, int fromType, int fromAxis, float min, float max, float offset, float multiply, float delay, float loop, bool visual)
        {
            var time = Updater.CurrentTime;

            if (!visual && Updater.levelProcessor.converter.cachedSequences.TryGetValue(bm.id, out ObjectConverter.CachedSequences cachedSequence))
                return fromType switch
                {
                    0 => Mathf.Clamp((cachedSequence.PositionSequence.Interpolate(time - bm.StartTime - delay).At(fromAxis) - offset) * multiply % loop, min, max),
                    1 => Mathf.Clamp((cachedSequence.ScaleSequence.Interpolate(time - bm.StartTime - delay).At(fromAxis) - offset) * multiply % loop, min, max),
                    2 => Mathf.Clamp((cachedSequence.RotationSequence.Interpolate(time - bm.StartTime - delay) - offset) * multiply % loop, min, max),
                    _ => 0f,
                };
            else if (visual && Updater.TryGetObject(bm, out LevelObject levelObject) && levelObject.visualObject != null && levelObject.visualObject.gameObject)
                return Mathf.Clamp((levelObject.visualObject.gameObject.transform.GetVector(fromType).At(fromAxis) - offset) * multiply % loop, min, max);

            return 0f;
        }

        static void CopyColor(LevelObject applyTo, LevelObject takeFrom, bool applyColor1, bool applyColor2)
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

        static bool GetLevelRank(Level level, out int levelRankIndex)
        {
            var active = level && level.playerData;
            levelRankIndex = active ? LevelManager.levelRankIndexes[LevelManager.GetLevelRank(level).name] : 0;
            return active;
        }

        static string GetSaveFile(string file) => RTFile.CombinePaths(RTFile.ApplicationDirectory, "profile", file + FileFormat.SES.Dot());

        static Prefab GetPrefab(int findType, string reference) => findType switch
        {
            0 => GameData.Current.prefabs.GetAt(Parser.TryParse(reference, -1)),
            1 => GameData.Current.prefabs.Find(x => x.name == reference),
            2 => GameData.Current.prefabs.Find(x => x.id == reference),
            _ => null,
        };

        static void SetParent(BeatmapObject child, string parent)
        {
            child.customParent = parent;
            Updater.UpdateObject(child);

            if (ObjectEditor.inst && ObjectEditor.inst.Dialog && ObjectEditor.inst.Dialog.IsCurrent && EditorTimeline.inst.CurrentSelection.isBeatmapObject)
                ObjectEditor.inst.RenderParent(EditorTimeline.inst.CurrentSelection.GetData<BeatmapObject>());
        }

        #endregion
    }
}
