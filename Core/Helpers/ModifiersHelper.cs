using BetterLegacy.Arcade.Managers;
using BetterLegacy.Configs;
using BetterLegacy.Core.Animation;
using BetterLegacy.Core.Animation.Keyframe;
using BetterLegacy.Core.Components;
using BetterLegacy.Core.Components.Player;
using BetterLegacy.Core.Data;
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
using DG.Tweening;
using LSFunctions;
using SimpleJSON;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
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
            if (modifier is Modifier<BeatmapObject> objectModifier)
                AssignModifierAction(objectModifier, ObjectAction, ObjectTrigger, ObjectInactive);
            else if (modifier is Modifier<BackgroundObject> backgroundModifier)
                AssignModifierAction(backgroundModifier, BGAction, BGTrigger, BGInactive);
            else if (modifier is Modifier<CustomPlayer> playerModifier)
                AssignModifierAction(playerModifier, PlayerAction, PlayerTrigger, PlayerInactive);
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
                    case ModifierBase.Type.Action:
                        {
                            if (modifier.Action == null || modifier.Inactive == null)
                                AssignModifierActions(modifier);

                            actions.Add(modifier);
                            break;
                        }
                    case ModifierBase.Type.Trigger:
                        {
                            if (modifier.Trigger == null || modifier.Inactive == null)
                                AssignModifierActions(modifier);

                            triggers.Add(modifier);
                            break;
                        }
                }
            });

            if (active)
            {
                if (triggers.Count > 0)
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

        #region BeatmapObject

        /// <summary>
        /// The function to run when a <see cref="ModifierBase.Type.Trigger"/> modifier is running and has a reference of <see cref="BeatmapObject"/>.
        /// </summary>
        /// <param name="modifier">Modifier to run.</param>
        /// <returns>Returns true if the modifier was triggered, otherwise returns false.</returns>
        public static bool ObjectTrigger(Modifier<BeatmapObject> modifier)
        {
            if (!modifier.verified)
            {
                modifier.verified = true;

                if (modifier.commands.Count > 0 && !modifier.Name.Contains("DEVONLY"))
                    modifier.VerifyModifier(ModifiersManager.defaultBeatmapObjectModifiers);
            }

            if (modifier.commands.Count <= 0)
                return false;

            switch (modifier.Name)
            {
                case "disableModifier": {
                        return false;
                    }

                #region Player

                case "playerCollide": {
                        if (Updater.TryGetObject(modifier.reference, out LevelObject levelObject) && levelObject.visualObject.Collider)
                        {
                            var collider = levelObject.visualObject.Collider;

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
                    }
                case "playerHealthEquals": {
                        return InputDataManager.inst.players.Count > 0 && int.TryParse(modifier.value, out int num) && InputDataManager.inst.players.Any(x => x.health == num);
                    }
                case "playerHealthLesserEquals": {
                        return InputDataManager.inst.players.Count > 0 && int.TryParse(modifier.value, out int num) && InputDataManager.inst.players.Any(x => x.health <= num);
                    }
                case "playerHealthGreaterEquals": {
                        return InputDataManager.inst.players.Count > 0 && int.TryParse(modifier.value, out int num) && InputDataManager.inst.players.Any(x => x.health >= num);
                    }
                case "playerHealthLesser": {
                        return InputDataManager.inst.players.Count > 0 && int.TryParse(modifier.value, out int num) && InputDataManager.inst.players.Any(x => x.health < num);
                    }
                case "playerHealthGreater": {
                        return InputDataManager.inst.players.Count > 0 && int.TryParse(modifier.value, out int num) && InputDataManager.inst.players.Any(x => x.health > num);
                    }
                case "playerMoving": {
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
                        break;
                    }
                case "playerBoosting": {
                        if (modifier.reference != null && Updater.TryGetObject(modifier.reference, out LevelObject levelObject) && levelObject.visualObject.GameObject)
                        {
                            var orderedList = PlayerManager.Players
                                .Where(x => x.Player && x.Player.rb)
                                .OrderBy(x => Vector2.Distance(x.Player.rb.position, levelObject.visualObject.GameObject.transform.position)).ToList();

                            if (orderedList.Count > 0)
                            {
                                var closest = orderedList[0];

                                return closest.Player.isBoosting;
                            }
                        }

                        break;
                    }
                case "playerAlive": {
                        if (int.TryParse(modifier.value, out int hit) && modifier.reference != null && Updater.TryGetObject(modifier.reference, out LevelObject levelObject) && levelObject.visualObject.GameObject)
                        {
                            if (PlayerManager.Players.Count > hit)
                            {
                                var closest = PlayerManager.Players[hit];

                                return closest.Player && closest.Player.Alive;
                            }
                        }

                        break;
                    }
                case "playerDeathsEquals": {
                        return int.TryParse(modifier.value, out int num) && GameManager.inst.deaths.Count == num;
                    }
                case "playerDeathsLesserEquals": {
                        return int.TryParse(modifier.value, out int num) && GameManager.inst.deaths.Count <= num;
                    }
                case "playerDeathsGreaterEquals": {
                        return int.TryParse(modifier.value, out int num) && GameManager.inst.deaths.Count >= num;
                    }
                case "playerDeathsLesser": {
                        return int.TryParse(modifier.value, out int num) && GameManager.inst.deaths.Count < num;
                    }
                case "playerDeathsGreater": {
                        return int.TryParse(modifier.value, out int num) && GameManager.inst.deaths.Count > num;
                    }
                case "playerDistanceGreater": {
                        for (int i = 0; i < GameManager.inst.players.transform.childCount; i++)
                        {
                            if (GameManager.inst.players.transform.Find(string.Format("Player {0}", i + 1)) && modifier.reference != null && Updater.TryGetObject(modifier.reference, out LevelObject levelObject) && levelObject.visualObject.GameObject && float.TryParse(modifier.value, out float num))
                            {
                                var player = GameManager.inst.players.transform.Find(string.Format("Player {0}/Player", i + 1));
                                if (Vector2.Distance(player.transform.position, levelObject.visualObject.GameObject.transform.position) > num)
                                    return true;
                            }
                        }

                        break;
                    }
                case "playerDistanceLesser": {
                        for (int i = 0; i < GameManager.inst.players.transform.childCount; i++)
                        {
                            if (GameManager.inst.players.transform.Find(string.Format("Player {0}", i + 1)) && modifier.reference != null && Updater.TryGetObject(modifier.reference, out LevelObject levelObject) && levelObject.visualObject.GameObject && float.TryParse(modifier.value, out float num))
                            {
                                var player = GameManager.inst.players.transform.Find(string.Format("Player {0}/Player", i + 1));
                                if (Vector2.Distance(player.transform.position, levelObject.visualObject.GameObject.transform.position) < num)
                                    return true;
                            }
                        }

                        break;
                    }
                case "playerCountEquals": {
                        return int.TryParse(modifier.value, out int num) && InputDataManager.inst.players.Count == num;
                    }
                case "playerCountLesserEquals": {
                        return int.TryParse(modifier.value, out int num) && InputDataManager.inst.players.Count <= num;
                    }
                case "playerCountGreaterEquals": {
                        return int.TryParse(modifier.value, out int num) && InputDataManager.inst.players.Count >= num;
                    }
                case "playerCountLesser": {
                        return int.TryParse(modifier.value, out int num) && InputDataManager.inst.players.Count < num;
                    }
                case "playerCountGreater": {
                        return int.TryParse(modifier.value, out int num) && InputDataManager.inst.players.Count > num;
                    }
                case "onPlayerHit": {
                        if (modifier.Result == null || modifier.Result is int count && count != GameManager.inst.hits.Count)
                        {
                            modifier.Result = GameManager.inst.hits.Count;
                            return true;
                        }

                        break;
                    }
                case "onPlayerDeath": {
                        if (modifier.Result == null || modifier.Result is int count && count != GameManager.inst.deaths.Count)
                        {
                            modifier.Result = GameManager.inst.deaths.Count;
                            return true;
                        }

                        break;
                    }
                case "playerBoostEquals": {
                        return int.TryParse(modifier.value, out int num) && LevelManager.BoostCount == num;
                    }
                case "playerBoostLesserEquals": {
                        return int.TryParse(modifier.value, out int num) && LevelManager.BoostCount <= num;
                    }
                case "playerBoostGreaterEquals": {
                        return int.TryParse(modifier.value, out int num) && LevelManager.BoostCount >= num;
                    }
                case "playerBoostLesser": {
                        return int.TryParse(modifier.value, out int num) && LevelManager.BoostCount < num;
                    }
                case "playerBoostGreater": {
                        return int.TryParse(modifier.value, out int num) && LevelManager.BoostCount > num;
                    }

                #endregion
                #region Controls

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
                case "mouseOver": {
                        if (modifier.reference.levelObject && modifier.reference.levelObject.visualObject != null && modifier.reference.levelObject.visualObject.GameObject)
                        {
                            if (!modifier.reference.detector)
                            {
                                var gameObject = modifier.reference.levelObject.visualObject.GameObject;
                                var op = gameObject.GetComponent<Detector>() ?? gameObject.AddComponent<Detector>();
                                op.beatmapObject = modifier.reference;
                                modifier.reference.detector = op;
                            }

                            if (modifier.reference.detector)
                                return modifier.reference.detector.hovered;
                        }
                        break;
                    }
                case "mouseOverSignalModifier": {
                        var list = !modifier.prefabInstanceOnly ? GameData.Current.FindObjectsWithTag(modifier.commands[1]) : GameData.Current.FindObjectsWithTag(modifier.reference, modifier.commands[1]);
                        if (modifier.reference.levelObject && modifier.reference.levelObject.visualObject != null && modifier.reference.levelObject.visualObject.GameObject)
                        {
                            if (!modifier.reference.detector)
                            {
                                var gameObject = modifier.reference.levelObject.visualObject.GameObject;
                                var op = gameObject.GetComponent<Detector>() ?? gameObject.AddComponent<Detector>();
                                op.beatmapObject = modifier.reference;
                                modifier.reference.detector = op;
                            }

                            if (modifier.reference.detector)
                            {
                                if (modifier.reference.detector.hovered && list.Count() > 0)
                                {
                                    foreach (var bm in list)
                                        CoreHelper.StartCoroutine(ModifiersManager.ActivateModifier((BeatmapObject)bm, Parser.TryParse(modifier.value, 0f)));
                                }

                                if (modifier.reference.detector.hovered)
                                    return true;
                            }
                        }
                        break;
                    }
                case "controlPress": {
                        var type = Parser.TryParse(modifier.value, 0);

                        if (!Updater.TryGetObject(modifier.reference, out LevelObject levelObject) || !levelObject.visualObject.GameObject)
                            break;

                        var player = PlayerManager.GetClosestPlayer(levelObject.visualObject.GameObject.transform.position);

                        if (!player || player.device == null && !CoreHelper.InEditor || InControl.InputManager.ActiveDevice == null)
                            break;

                        var device = player.device ?? InControl.InputManager.ActiveDevice;

                        return Enum.TryParse(((PlayerInputControlType)type).ToString(), out InControl.InputControlType inputControlType) && device.GetControl(inputControlType).IsPressed;
                    }
                case "controlPressDown": {
                        var type = Parser.TryParse(modifier.value, 0);

                        if (!Updater.TryGetObject(modifier.reference, out LevelObject levelObject) || !levelObject.visualObject.GameObject)
                            break;

                        var player = PlayerManager.GetClosestPlayer(levelObject.visualObject.GameObject.transform.position);

                        if (!player || player.device == null && !CoreHelper.InEditor || InControl.InputManager.ActiveDevice == null)
                            break;

                        var device = player.device ?? InControl.InputManager.ActiveDevice;

                        return Enum.TryParse(((PlayerInputControlType)type).ToString(), out InControl.InputControlType inputControlType) && device.GetControl(inputControlType).WasPressed;
                    }
                case "controlPressUp": {
                        var type = Parser.TryParse(modifier.value, 0);

                        if (!Updater.TryGetObject(modifier.reference, out LevelObject levelObject) || !levelObject.visualObject.GameObject)
                            break;

                        var player = PlayerManager.GetClosestPlayer(levelObject.visualObject.GameObject.transform.position);

                        if (!player || player.device == null && !CoreHelper.InEditor || InControl.InputManager.ActiveDevice == null)
                            break;

                        var device = player.device ?? InControl.InputManager.ActiveDevice;

                        return Enum.TryParse(((PlayerInputControlType)type).ToString(), out InControl.InputControlType inputControlType) && device.GetControl(inputControlType).WasReleased;
                    }

                #endregion
                #region Collide

                case "bulletCollide": {
                        if (modifier.reference != null && Updater.TryGetObject(modifier.reference, out LevelObject levelObject) && levelObject.visualObject != null && levelObject.visualObject.GameObject)
                        {
                            if (!modifier.reference.detector)
                            {
                                var op = levelObject.visualObject.GameObject.GetComponent<Detector>() ?? levelObject.visualObject.GameObject.AddComponent<Detector>();
                                op.beatmapObject = modifier.reference;
                                modifier.reference.detector = op;
                            }

                            if (modifier.reference.detector)
                                return modifier.reference.detector.bulletOver;
                        }
                        break;
                    }
                case "objectCollide": {
                        if (Updater.TryGetObject(modifier.reference, out LevelObject levelObject) && levelObject.visualObject != null && levelObject.visualObject.Collider)
                        {
                            var list = (!modifier.prefabInstanceOnly ? GameData.Current.FindObjectsWithTag(modifier.value) : GameData.Current.FindObjectsWithTag(modifier.reference, modifier.value)).FindAll(x => Updater.TryGetObject(x, out LevelObject levelObject1) && levelObject1.visualObject != null && levelObject1.visualObject.Collider);
                            return list.Count > 0 && list.Any(x => x.levelObject.visualObject.Collider.IsTouching(levelObject.visualObject.Collider));
                        }

                        break;
                    }

                #endregion
                #region JSON

                case "loadEquals": {
                        if (RTFile.FileExists(RTFile.ApplicationDirectory + "profile/" + modifier.commands[1] + ".ses"))
                        {
                            string json = RTFile.ReadFromFile(RTFile.ApplicationDirectory + "profile/" + modifier.commands[1] + ".ses");

                            if (modifier.commands.Count < 5)
                                modifier.commands.Add("0");

                            if (!string.IsNullOrEmpty(json) && int.TryParse(modifier.commands[4], out int type))
                            {
                                var jn = JSON.Parse(json);

                                return
                                    !string.IsNullOrEmpty(jn[modifier.commands[2]][modifier.commands[3]]["float"]) && (type == 0 &&
                                    float.TryParse(jn[modifier.commands[2]][modifier.commands[3]]["float"], out float eq) &&
                                    float.TryParse(modifier.value, out float num) && eq == num || type == 1 && jn[modifier.commands[2]][modifier.commands[3]]["string"] == modifier.value);
                            }
                        }
                        break;
                    }
                case "loadLesserEquals": {
                        if (RTFile.FileExists(RTFile.ApplicationDirectory + "profile/" + modifier.commands[1] + ".ses"))
                        {
                            string json = FileManager.inst.LoadJSONFile("profile/" + modifier.commands[1] + ".ses");

                            if (!string.IsNullOrEmpty(json))
                            {
                                var jn = JSON.Parse(json);

                                return
                                    !string.IsNullOrEmpty(jn[modifier.commands[2]][modifier.commands[3]]["float"]) &&
                                    float.TryParse(jn[modifier.commands[2]][modifier.commands[3]]["float"], out float eq) &&
                                    float.TryParse(modifier.value, out float num) && eq <= num;
                            }
                        }
                        break;
                    }
                case "loadGreaterEquals": {
                        if (RTFile.FileExists(RTFile.ApplicationDirectory + "profile/" + modifier.commands[1] + ".ses"))
                        {
                            string json = FileManager.inst.LoadJSONFile("profile/" + modifier.commands[1] + ".ses");

                            if (!string.IsNullOrEmpty(json))
                            {
                                var jn = JSON.Parse(json);

                                return
                                    !string.IsNullOrEmpty(jn[modifier.commands[2]][modifier.commands[3]]["float"]) &&
                                    float.TryParse(jn[modifier.commands[2]][modifier.commands[3]]["float"], out float eq) &&
                                    float.TryParse(modifier.value, out float num) && eq >= num;
                            }
                        }
                        break;
                    }
                case "loadLesser": {
                        if (RTFile.FileExists(RTFile.ApplicationDirectory + "profile/" + modifier.commands[1] + ".ses"))
                        {
                            string json = FileManager.inst.LoadJSONFile("profile/" + modifier.commands[1] + ".ses");

                            if (!string.IsNullOrEmpty(json))
                            {
                                var jn = JSON.Parse(json);

                                return
                                    !string.IsNullOrEmpty(jn[modifier.commands[2]][modifier.commands[3]]["float"]) &&
                                    float.TryParse(jn[modifier.commands[2]][modifier.commands[3]]["float"], out float eq) &&
                                    float.TryParse(modifier.value, out float num) && eq < num;
                            }
                        }
                        break;
                    }
                case "loadGreater": {
                        if (RTFile.FileExists(RTFile.ApplicationDirectory + "profile/" + modifier.commands[1] + ".ses"))
                        {
                            string json = FileManager.inst.LoadJSONFile("profile/" + modifier.commands[1] + ".ses");

                            if (!string.IsNullOrEmpty(json))
                            {
                                var jn = JSON.Parse(json);

                                return
                                    !string.IsNullOrEmpty(jn[modifier.commands[2]][modifier.commands[3]]["float"]) &&
                                    float.TryParse(jn[modifier.commands[2]][modifier.commands[3]]["float"], out float eq) &&
                                    float.TryParse(modifier.value, out float num) && eq > num;
                            }
                        }
                        break;
                    }
                case "loadExists": {
                        if (RTFile.FileExists(RTFile.ApplicationDirectory + "profile/" + modifier.commands[1] + ".ses"))
                        {
                            string json = FileManager.inst.LoadJSONFile("profile/" + modifier.commands[1] + ".ses");

                            if (!string.IsNullOrEmpty(json))
                            {
                                var jn = JSON.Parse(json);

                                return !string.IsNullOrEmpty(jn[modifier.commands[2]][modifier.commands[3]]);
                            }
                        }
                        break;
                    }

                #endregion
                #region Variable

                case "variableEquals": {
                        return int.TryParse(modifier.value, out int num) && modifier.reference && modifier.reference.integerVariable == num;
                    }
                case "variableLesserEquals": {
                        return int.TryParse(modifier.value, out int num) && modifier.reference && modifier.reference.integerVariable <= num;
                    }
                case "variableGreaterEquals": {
                        return int.TryParse(modifier.value, out int num) && modifier.reference && modifier.reference.integerVariable >= num;
                    }
                case "variableLesser": {
                        return int.TryParse(modifier.value, out int num) && modifier.reference && modifier.reference.integerVariable < num;
                    }
                case "variableGreater": {
                        return int.TryParse(modifier.value, out int num) && modifier.reference && modifier.reference.integerVariable > num;
                    }
                case "variableOtherEquals": {
                        var beatmapObjects = !modifier.prefabInstanceOnly ? GameData.Current.FindObjectsWithTag(modifier.commands[1]) : GameData.Current.FindObjectsWithTag(modifier.reference, modifier.commands[1]);

                        return
                            int.TryParse(modifier.value, out int num) &&
                            modifier.reference &&
                            beatmapObjects.Any(x => x.integerVariable == num);
                    }
                case "variableOtherLesserEquals": {
                        var beatmapObjects = !modifier.prefabInstanceOnly ? GameData.Current.FindObjectsWithTag(modifier.commands[1]) : GameData.Current.FindObjectsWithTag(modifier.reference, modifier.commands[1]);

                        return
                            int.TryParse(modifier.value, out int num) &&
                            modifier.reference &&
                            beatmapObjects.Any(x => x.integerVariable <= num);
                    }
                case "variableOtherGreaterEquals": {
                        var beatmapObjects = !modifier.prefabInstanceOnly ? GameData.Current.FindObjectsWithTag(modifier.commands[1]) : GameData.Current.FindObjectsWithTag(modifier.reference, modifier.commands[1]);

                        return
                            int.TryParse(modifier.value, out int num) &&
                            modifier.reference &&
                            beatmapObjects.Any(x => x.integerVariable >= num);
                    }
                case "variableOtherLesser": {
                        var beatmapObjects = !modifier.prefabInstanceOnly ? GameData.Current.FindObjectsWithTag(modifier.commands[1]) : GameData.Current.FindObjectsWithTag(modifier.reference, modifier.commands[1]);

                        return
                            int.TryParse(modifier.value, out int num) &&
                            modifier.reference &&
                            beatmapObjects.Any(x => x.integerVariable < num);
                    }
                case "variableOtherGreater": {
                        var beatmapObjects = !modifier.prefabInstanceOnly ? GameData.Current.FindObjectsWithTag(modifier.commands[1]) : GameData.Current.FindObjectsWithTag(modifier.reference, modifier.commands[1]);

                        return
                            int.TryParse(modifier.value, out int num) &&
                            modifier.reference &&
                            beatmapObjects.Any(x => x.integerVariable > num);
                    }

                #endregion
                #region Audio

                case "pitchEquals": {
                        return float.TryParse(modifier.value, out float num) && AudioManager.inst.pitch == num;
                    }
                case "pitchLesserEquals": {
                        return float.TryParse(modifier.value, out float num) && AudioManager.inst.pitch <= num;
                    }
                case "pitchGreaterEquals": {
                        return float.TryParse(modifier.value, out float num) && AudioManager.inst.pitch >= num;
                    }
                case "pitchLesser": {
                        return float.TryParse(modifier.value, out float num) && AudioManager.inst.pitch < num;
                    }
                case "pitchGreater": {
                        return float.TryParse(modifier.value, out float num) && AudioManager.inst.pitch > num;
                    }
                case "musicTimeGreater": {
                        return float.TryParse(modifier.value, out float x) && AudioManager.inst.CurrentAudioSource.time - (modifier.GetBool(1, false) ? modifier.reference.StartTime : 0f) > x;
                    }
                case "musicTimeLesser": {
                        return float.TryParse(modifier.value, out float x) && AudioManager.inst.CurrentAudioSource.time - (modifier.GetBool(1, false) ? modifier.reference.StartTime : 0f) < x;
                    }
                case "musicPlaying": {
                        return AudioManager.inst.CurrentAudioSource.isPlaying;
                    }

                #endregion
                #region Challenge Mode

                case "inZenMode": {
                        return PlayerManager.Invincible;
                    }
                case "inNormal": {
                        return PlayerManager.IsNormal;
                    }
                case "in1Life": {
                        return PlayerManager.Is1Life;
                    }
                case "inNoHit": {
                        return PlayerManager.IsNoHit;
                    }
                case "inPractice": {
                        return PlayerManager.IsPractice;
                    }

                #endregion
                #region Random

                case "randomGreater": {
                        if (modifier.Result == null)
                            modifier.Result = int.TryParse(modifier.commands[1], out int x) && int.TryParse(modifier.commands[2], out int y) && int.TryParse(modifier.value, out int z) && UnityEngine.Random.Range(x, y) > z;

                        return modifier.Result != null && (bool)modifier.Result;
                    }
                case "randomLesser": {
                        if (modifier.Result == null)
                            modifier.Result = int.TryParse(modifier.commands[1], out int x) && int.TryParse(modifier.commands[2], out int y) && int.TryParse(modifier.value, out int z) && UnityEngine.Random.Range(x, y) < z;

                        return modifier.Result != null && (bool)modifier.Result;
                    }
                case "randomEquals": {
                        if (modifier.Result == null)
                            modifier.Result = int.TryParse(modifier.commands[1], out int x) && int.TryParse(modifier.commands[2], out int y) && int.TryParse(modifier.value, out int z) && UnityEngine.Random.Range(x, y) == z;

                        return modifier.Result != null && (bool)modifier.Result;
                    }

                #endregion
                #region Math

                case "mathEquals": {
                        var variables = modifier.reference.GetObjectVariables();
                        return RTMath.Parse(modifier.value, variables) == RTMath.Parse(modifier.commands[1], variables);
                    }
                case "mathLesserEquals": {
                        var variables = modifier.reference.GetObjectVariables();
                        return RTMath.Parse(modifier.value, variables) <= RTMath.Parse(modifier.commands[1], variables);
                    }
                case "mathGreaterEquals": {
                        var variables = modifier.reference.GetObjectVariables();
                        return RTMath.Parse(modifier.value, variables) >= RTMath.Parse(modifier.commands[1], variables);
                    }
                case "mathLesser": {
                        var variables = modifier.reference.GetObjectVariables();
                        return RTMath.Parse(modifier.value, variables) < RTMath.Parse(modifier.commands[1], variables);
                    }
                case "mathGreater": {
                        var variables = modifier.reference.GetObjectVariables();
                        return RTMath.Parse(modifier.value, variables) > RTMath.Parse(modifier.commands[1], variables);
                    }

                #endregion
                #region Axis

                case "axisEquals": {
                        if (int.TryParse(modifier.commands[1], out int fromType) && int.TryParse(modifier.commands[2], out int fromAxis)
                            && float.TryParse(modifier.commands[3], out float delay) && float.TryParse(modifier.commands[4], out float multiply)
                            && float.TryParse(modifier.commands[5], out float offset) && float.TryParse(modifier.commands[6], out float min) && float.TryParse(modifier.commands[7], out float max)
                            && float.TryParse(modifier.commands[8], out float equals) && bool.TryParse(modifier.commands[9], out bool visual)
                            && float.TryParse(modifier.commands[10], out float loop)
                            && GameData.Current.TryFindObjectWithTag(modifier, modifier.value, out BeatmapObject bm))
                        {
                            var time = Updater.CurrentTime;

                            fromType = Mathf.Clamp(fromType, 0, bm.events.Count);
                            fromAxis = Mathf.Clamp(fromAxis, 0, bm.events[fromType][0].eventValues.Length);

                            if (fromType < 0 || fromType > 2)
                                break;

                            return GetAnimation(bm, fromType, fromAxis, min, max, offset, multiply, delay, loop, visual) == equals;
                        }

                        break;
                    }
                case "axisLesserEquals": {
                        if (int.TryParse(modifier.commands[1], out int fromType) && int.TryParse(modifier.commands[2], out int fromAxis)
                            && float.TryParse(modifier.commands[3], out float delay) && float.TryParse(modifier.commands[4], out float multiply)
                            && float.TryParse(modifier.commands[5], out float offset) && float.TryParse(modifier.commands[6], out float min) && float.TryParse(modifier.commands[7], out float max)
                            && float.TryParse(modifier.commands[8], out float equals) && bool.TryParse(modifier.commands[9], out bool visual)
                            && float.TryParse(modifier.commands[10], out float loop)
                            && GameData.Current.TryFindObjectWithTag(modifier, modifier.value, out BeatmapObject bm))
                        {
                            var time = Updater.CurrentTime;

                            fromType = Mathf.Clamp(fromType, 0, bm.events.Count);
                            fromAxis = Mathf.Clamp(fromAxis, 0, bm.events[fromType][0].eventValues.Length);

                            if (fromType < 0 || fromType > 2)
                                break;

                            return GetAnimation(bm, fromType, fromAxis, min, max, offset, multiply, delay, loop, visual) <= equals;
                        }

                        break;
                    }
                case "axisGreaterEquals": {
                        if (int.TryParse(modifier.commands[1], out int fromType) && int.TryParse(modifier.commands[2], out int fromAxis)
                            && float.TryParse(modifier.commands[3], out float delay) && float.TryParse(modifier.commands[4], out float multiply)
                            && float.TryParse(modifier.commands[5], out float offset) && float.TryParse(modifier.commands[6], out float min) && float.TryParse(modifier.commands[7], out float max)
                            && float.TryParse(modifier.commands[8], out float equals) && bool.TryParse(modifier.commands[9], out bool visual)
                            && float.TryParse(modifier.commands[10], out float loop)
                            && GameData.Current.TryFindObjectWithTag(modifier, modifier.value, out BeatmapObject bm))
                        {
                            var time = Updater.CurrentTime;

                            fromType = Mathf.Clamp(fromType, 0, bm.events.Count);
                            fromAxis = Mathf.Clamp(fromAxis, 0, bm.events[fromType][0].eventValues.Length);

                            if (fromType < 0 || fromType > 2)
                                break;

                            return GetAnimation(bm, fromType, fromAxis, min, max, offset, multiply, delay, loop, visual) >= equals;
                        }

                        break;
                    }
                case "axisLesser": {
                        if (int.TryParse(modifier.commands[1], out int fromType) && int.TryParse(modifier.commands[2], out int fromAxis)
                            && float.TryParse(modifier.commands[3], out float delay) && float.TryParse(modifier.commands[4], out float multiply)
                            && float.TryParse(modifier.commands[5], out float offset) && float.TryParse(modifier.commands[6], out float min) && float.TryParse(modifier.commands[7], out float max)
                            && float.TryParse(modifier.commands[8], out float equals) && bool.TryParse(modifier.commands[9], out bool visual)
                            && float.TryParse(modifier.commands[10], out float loop)
                            && GameData.Current.TryFindObjectWithTag(modifier, modifier.value, out BeatmapObject bm))
                        {
                            var time = Updater.CurrentTime;

                            fromType = Mathf.Clamp(fromType, 0, bm.events.Count);
                            fromAxis = Mathf.Clamp(fromAxis, 0, bm.events[fromType][0].eventValues.Length);

                            if (fromType < 0 || fromType > 2)
                                break;

                            return GetAnimation(bm, fromType, fromAxis, min, max, offset, multiply, delay, loop, visual) < equals;
                        }

                        break;
                    }
                case "axisGreater": {
                        if (int.TryParse(modifier.commands[1], out int fromType) && int.TryParse(modifier.commands[2], out int fromAxis)
                            && float.TryParse(modifier.commands[3], out float delay) && float.TryParse(modifier.commands[4], out float multiply)
                            && float.TryParse(modifier.commands[5], out float offset) && float.TryParse(modifier.commands[6], out float min) && float.TryParse(modifier.commands[7], out float max)
                            && float.TryParse(modifier.commands[8], out float equals) && bool.TryParse(modifier.commands[9], out bool visual)
                            && float.TryParse(modifier.commands[10], out float loop)
                            && GameData.Current.TryFindObjectWithTag(modifier, modifier.value, out BeatmapObject bm))
                        {
                            var time = Updater.CurrentTime;

                            fromType = Mathf.Clamp(fromType, 0, bm.events.Count);
                            fromAxis = Mathf.Clamp(fromAxis, 0, bm.events[fromType][0].eventValues.Length);

                            if (fromType < 0 || fromType > 2)
                                break;

                            return GetAnimation(bm, fromType, fromAxis, min, max, offset, multiply, delay, loop, visual) > equals;
                        }

                        break;
                    }

                #endregion
                #region Level Rank

                case "levelRankEquals": {
                        return !CoreHelper.InEditor && LevelManager.CurrentLevel != null && LevelManager.CurrentLevel.playerData != null && int.TryParse(modifier.value, out int num) && LevelManager.levelRankIndexes[LevelManager.GetLevelRank(LevelManager.CurrentLevel).name] == num;
                    }
                case "levelRankLesserEquals": {
                        return !CoreHelper.InEditor && LevelManager.CurrentLevel != null && LevelManager.CurrentLevel.playerData != null && int.TryParse(modifier.value, out int num) && LevelManager.levelRankIndexes[LevelManager.GetLevelRank(LevelManager.CurrentLevel).name] <= num;
                    }
                case "levelRankGreaterEquals": {
                        return !CoreHelper.InEditor && LevelManager.CurrentLevel != null && LevelManager.CurrentLevel.playerData != null && int.TryParse(modifier.value, out int num) && LevelManager.levelRankIndexes[LevelManager.GetLevelRank(LevelManager.CurrentLevel).name] >= num;
                    }
                case "levelRankLesser": {
                        return !CoreHelper.InEditor && LevelManager.CurrentLevel != null && LevelManager.CurrentLevel.playerData != null && int.TryParse(modifier.value, out int num) && LevelManager.levelRankIndexes[LevelManager.GetLevelRank(LevelManager.CurrentLevel).name] < num;
                    }
                case "levelRankGreater": {
                        return !CoreHelper.InEditor && LevelManager.CurrentLevel != null && LevelManager.CurrentLevel.playerData != null && int.TryParse(modifier.value, out int num) && LevelManager.levelRankIndexes[LevelManager.GetLevelRank(LevelManager.CurrentLevel).name] > num;
                    }
                case "levelRankOtherEquals": {
                        return LevelManager.Levels.TryFind(x => x.id == modifier.commands[1], out Level level) && level.playerData != null && int.TryParse(modifier.value, out int num) && LevelManager.levelRankIndexes[LevelManager.GetLevelRank(level).name] == num;
                    }
                case "levelRankOtherLesserEquals": {
                        return LevelManager.Levels.TryFind(x => x.id == modifier.commands[1], out Level level) && level.playerData != null && int.TryParse(modifier.value, out int num) && LevelManager.levelRankIndexes[LevelManager.GetLevelRank(level).name] <= num;
                    }
                case "levelRankOtherGreaterEquals": {
                        return LevelManager.Levels.TryFind(x => x.id == modifier.commands[1], out Level level) && level.playerData != null && int.TryParse(modifier.value, out int num) && LevelManager.levelRankIndexes[LevelManager.GetLevelRank(level).name] >= num;
                    }
                case "levelRankOtherLesser": {
                        return LevelManager.Levels.TryFind(x => x.id == modifier.commands[1], out Level level) && level.playerData != null && int.TryParse(modifier.value, out int num) && LevelManager.levelRankIndexes[LevelManager.GetLevelRank(level).name] < num;
                    }
                case "levelRankOtherGreater": {
                        return LevelManager.Levels.TryFind(x => x.id == modifier.commands[1], out Level level) && level.playerData != null && int.TryParse(modifier.value, out int num) && LevelManager.levelRankIndexes[LevelManager.GetLevelRank(level).name] > num;
                    }
                case "levelRankCurrentEquals": {
                        return int.TryParse(modifier.value, out int num) && LevelManager.levelRankIndexes[LevelManager.GetLevelRank(GameManager.inst.hits).name] == num;
                    }
                case "levelRankCurrentLesserEquals": {
                        return int.TryParse(modifier.value, out int num) && LevelManager.levelRankIndexes[LevelManager.GetLevelRank(GameManager.inst.hits).name] <= num;
                    }
                case "levelRankCurrentGreaterEquals": {
                        return int.TryParse(modifier.value, out int num) && LevelManager.levelRankIndexes[LevelManager.GetLevelRank(GameManager.inst.hits).name] >= num;
                    }
                case "levelRankCurrentLesser": {
                        return int.TryParse(modifier.value, out int num) && LevelManager.levelRankIndexes[LevelManager.GetLevelRank(GameManager.inst.hits).name] < num;
                    }
                case "levelRankCurrentGreater": {
                        return int.TryParse(modifier.value, out int num) && LevelManager.levelRankIndexes[LevelManager.GetLevelRank(GameManager.inst.hits).name] > num;
                    }

                #endregion
                #region Real Time

                case "realTimeSecondEquals": {
                        if (int.TryParse(modifier.value, out int num))
                            return Parser.TryParse(DateTime.Now.ToString("ss"), 0) == num;
                        break;
                    }
                case "realTimeSecondLesserEquals": {
                        if (int.TryParse(modifier.value, out int num))
                            return Parser.TryParse(DateTime.Now.ToString("ss"), 0) <= num;
                        break;
                    }
                case "realTimeSecondGreaterEquals": {
                        if (int.TryParse(modifier.value, out int num))
                            return Parser.TryParse(DateTime.Now.ToString("ss"), 0) >= num;
                        break;
                    }
                case "realTimeSecondLesser": {
                        if (int.TryParse(modifier.value, out int num))
                            return Parser.TryParse(DateTime.Now.ToString("ss"), 0) < num;
                        break;
                    }
                case "realTimeSecondGreater": {
                        if (int.TryParse(modifier.value, out int num))
                            return Parser.TryParse(DateTime.Now.ToString("ss"), 0) > num;
                        break;
                    }
                case "realTimeMinuteEquals": {
                        if (int.TryParse(modifier.value, out int num))
                            return Parser.TryParse(DateTime.Now.ToString("mm"), 0) == num;
                        break;
                    }
                case "realTimeMinuteLesserEquals": {
                        if (int.TryParse(modifier.value, out int num))
                            return Parser.TryParse(DateTime.Now.ToString("mm"), 0) <= num;
                        break;
                    }
                case "realTimeMinuteGreaterEquals": {
                        if (int.TryParse(modifier.value, out int num))
                            return Parser.TryParse(DateTime.Now.ToString("mm"), 0) >= num;
                        break;
                    }
                case "realTimeMinuteLesser": {
                        if (int.TryParse(modifier.value, out int num))
                            return Parser.TryParse(DateTime.Now.ToString("mm"), 0) < num;
                        break;
                    }
                case "realTimeMinuteGreater": {
                        if (int.TryParse(modifier.value, out int num))
                            return Parser.TryParse(DateTime.Now.ToString("mm"), 0) > num;
                        break;
                    }
                case "realTime24HourEquals": {
                        if (int.TryParse(modifier.value, out int num))
                            return Parser.TryParse(DateTime.Now.ToString("HH"), 0) == num;
                        break;
                    }
                case "realTime24HourLesserEquals": {
                        if (int.TryParse(modifier.value, out int num))
                            return Parser.TryParse(DateTime.Now.ToString("HH"), 0) <= num;
                        break;
                    }
                case "realTime24HourGreaterEquals": {
                        if (int.TryParse(modifier.value, out int num))
                            return Parser.TryParse(DateTime.Now.ToString("HH"), 0) >= num;
                        break;
                    }
                case "realTime24HourLesser": {
                        if (int.TryParse(modifier.value, out int num))
                            return Parser.TryParse(DateTime.Now.ToString("HH"), 0) < num;
                        break;
                    }
                case "realTime24HourGreater": {
                        if (int.TryParse(modifier.value, out int num))
                            return Parser.TryParse(DateTime.Now.ToString("HH"), 0) > num;
                        break;
                    }
                case "realTime12HourEquals": {
                        if (int.TryParse(modifier.value, out int num))
                            return Parser.TryParse(DateTime.Now.ToString("hh"), 0) == num;
                        break;
                    }
                case "realTime12HourLesserEquals": {
                        if (int.TryParse(modifier.value, out int num))
                            return Parser.TryParse(DateTime.Now.ToString("hh"), 0) <= num;
                        break;
                    }
                case "realTime12HourGreaterEquals": {
                        if (int.TryParse(modifier.value, out int num))
                            return Parser.TryParse(DateTime.Now.ToString("hh"), 0) >= num;
                        break;
                    }
                case "realTime12HourLesser": {
                        if (int.TryParse(modifier.value, out int num))
                            return Parser.TryParse(DateTime.Now.ToString("hh"), 0) < num;
                        break;
                    }
                case "realTime12HourGreater": {
                        if (int.TryParse(modifier.value, out int num))
                            return Parser.TryParse(DateTime.Now.ToString("hh"), 0) > num;
                        break;
                    }
                case "realTimeDayEquals": {
                        if (int.TryParse(modifier.value, out int num))
                            return Parser.TryParse(DateTime.Now.ToString("dd"), 0) == num;
                        break;
                    }
                case "realTimeDayLesserEquals": {
                        if (int.TryParse(modifier.value, out int num))
                            return Parser.TryParse(DateTime.Now.ToString("dd"), 0) <= num;
                        break;
                    }
                case "realTimeDayGreaterEquals": {
                        if (int.TryParse(modifier.value, out int num))
                            return Parser.TryParse(DateTime.Now.ToString("dd"), 0) >= num;
                        break;
                    }
                case "realTimeDayLesser": {
                        if (int.TryParse(modifier.value, out int num))
                            return Parser.TryParse(DateTime.Now.ToString("dd"), 0) < num;
                        break;
                    }
                case "realTimeDayGreater": {
                        if (int.TryParse(modifier.value, out int num))
                            return Parser.TryParse(DateTime.Now.ToString("dd"), 0) > num;
                        break;
                    }
                case "realTimeDayWeekEquals": {
                        return DateTime.Now.ToString("dddd") == modifier.value;
                    }
                case "realTimeMonthEquals": {
                        if (int.TryParse(modifier.value, out int num))
                            return Parser.TryParse(DateTime.Now.ToString("MM"), 0) == num;
                        break;
                    }
                case "realTimeMonthLesserEquals": {
                        if (int.TryParse(modifier.value, out int num))
                            return Parser.TryParse(DateTime.Now.ToString("MM"), 0) <= num;
                        break;
                    }
                case "realTimeMonthGreaterEquals": {
                        if (int.TryParse(modifier.value, out int num))
                            return Parser.TryParse(DateTime.Now.ToString("MM"), 0) >= num;
                        break;
                    }
                case "realTimeMonthLesser": {
                        if (int.TryParse(modifier.value, out int num))
                            return Parser.TryParse(DateTime.Now.ToString("MM"), 0) < num;
                        break;
                    }
                case "realTimeMonthGreater": {
                        if (int.TryParse(modifier.value, out int num))
                            return Parser.TryParse(DateTime.Now.ToString("MM"), 0) > num;
                        break;
                    }
                case "realTimeYearEquals": {
                        if (int.TryParse(modifier.value, out int num))
                            return Parser.TryParse(DateTime.Now.ToString("yyyy"), 0) == num;
                        break;
                    }
                case "realTimeYearLesserEquals": {
                        if (int.TryParse(modifier.value, out int num))
                            return Parser.TryParse(DateTime.Now.ToString("yyyy"), 0) <= num;
                        break;
                    }
                case "realTimeYearGreaterEquals": {
                        if (int.TryParse(modifier.value, out int num))
                            return Parser.TryParse(DateTime.Now.ToString("yyyy"), 0) >= num;
                        break;
                    }
                case "realTimeYearLesser": {
                        if (int.TryParse(modifier.value, out int num))
                            return Parser.TryParse(DateTime.Now.ToString("yyyy"), 0) < num;
                        break;
                    }
                case "realTimeYearGreater": {
                        if (int.TryParse(modifier.value, out int num))
                            return Parser.TryParse(DateTime.Now.ToString("yyyy"), 0) > num;
                        break;
                    }

                #endregion
                #region Level

                case "levelUnlocked": {
                        return LevelManager.Levels.TryFind(x => x.id == modifier.value, out Level level) && !level.Locked;
                    }
                case "levelCompleted": {
                        return CoreHelper.InEditor || LevelManager.CurrentLevel != null && LevelManager.CurrentLevel.playerData != null && LevelManager.CurrentLevel.playerData.Completed;
                    }
                case "levelCompletedOther": {
                        return CoreHelper.InEditor || LevelManager.Levels.TryFind(x => x.id == modifier.value, out Level level) && level.playerData != null && level.playerData.Completed;
                    }
                case "levelExists": {
                        return LevelManager.Levels.Has(x => x.id == modifier.value);
                    }
                case "levelPathExists": {
                        var basePath = RTFile.CombinePaths(RTFile.ApplicationDirectory, LevelManager.ListSlash, modifier.value);
                        return RTFile.FileExists(RTFile.CombinePaths(basePath, Level.LEVEL_LSB)) || RTFile.FileExists(RTFile.CombinePaths(basePath, Level.LEVEL_VGD)) || RTFile.FileExists(basePath + FileFormat.ASSET.Dot());
                    }

                #endregion
                #region Misc

                case "inEditor": {
                        return CoreHelper.InEditor;
                    }
                case "configLDM": {
                        return CoreConfig.Instance.LDM.Value;
                    }
                case "usernameEquals": {
                        return CoreConfig.Instance.DisplayName.Value == modifier.value;
                    }
                case "languageEquals": {
                        return CoreConfig.Instance.Language.Value == (Language)Parser.TryParse(modifier.value, 0);
                    }
                case "configShowEffects": {
                        return EventsConfig.Instance.ShowFX.Value;
                    }
                case "configShowPlayerGUI": {
                        return EventsConfig.Instance.ShowGUI.Value;
                    }
                case "configShowIntro": {
                        return EventsConfig.Instance.ShowIntro.Value;
                    }
                case "requireSignal": {
                        return modifier.Result != null;
                    }
                case "isFullscreen": {
                        return Screen.fullScreen;
                    }
                case "objectAlive": {
                        var list = !modifier.prefabInstanceOnly ? GameData.Current.FindObjectsWithTag(modifier.value) : GameData.Current.FindObjectsWithTag(modifier.reference, modifier.value);
                        for (int i = 0; i < list.Count; i++)
                        {
                            if (list[i].Alive)
                                return true;
                        }

                        break;
                    }
                case "objectSpawned": {
                        if (modifier.Result == null)
                            modifier.Result = new List<string>();

                        var ids = modifier.GetResult<List<string>>();

                        var list = !modifier.prefabInstanceOnly ? GameData.Current.FindObjectsWithTag(modifier.value) : GameData.Current.FindObjectsWithTag(modifier.reference, modifier.value);
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

                #endregion

                #region Dev Only

                case "storyLoadIntEqualsDEVONLY":
                        return Story.StoryManager.inst.LoadInt(modifier.GetValue(0), modifier.GetInt(1, 0)) == modifier.GetInt(2, 0);
                case "storyLoadIntLesserEqualsDEVONLY":
                        return Story.StoryManager.inst.LoadInt(modifier.GetValue(0), modifier.GetInt(1, 0)) <= modifier.GetInt(2, 0);
                case "storyLoadIntGreaterEqualsDEVONLY":
                        return Story.StoryManager.inst.LoadInt(modifier.GetValue(0), modifier.GetInt(1, 0)) >= modifier.GetInt(2, 0);
                case "storyLoadIntLesserDEVONLY":
                        return Story.StoryManager.inst.LoadInt(modifier.GetValue(0), modifier.GetInt(1, 0)) < modifier.GetInt(2, 0);
                case "storyLoadIntGreaterDEVONLY":
                        return Story.StoryManager.inst.LoadInt(modifier.GetValue(0), modifier.GetInt(1, 0)) > modifier.GetInt(2, 0);
                case "storyLoadBoolDEVONLY":
                    return Story.StoryManager.inst.LoadBool(modifier.GetValue(0), modifier.GetBool(1, false));

                    #endregion
            }

            //modifier.Inactive?.Invoke(modifier); // ?
            return false;
        }

        /// <summary>
        /// The function to run when a <see cref="ModifierBase.Type.Action"/> modifier is running and has a reference of <see cref="BeatmapObject"/>.
        /// </summary>
        /// <param name="modifier">Modifier to run.</param>
        public static void ObjectAction(Modifier<BeatmapObject> modifier)
        {
            modifier.hasChanged = false;

            if (!modifier.verified)
            {
                modifier.verified = true;

                if (modifier.commands.Count > 0 && !modifier.Name.Contains("DEVONLY"))
                    modifier.VerifyModifier(ModifiersManager.defaultBeatmapObjectModifiers);
            }

            if (modifier.commands.Count < 0)
                return;

            try
            {
                //System.Diagnostics.Stopwatch sw = null;
                //if (Input.GetKeyDown(KeyCode.G))
                //    sw = CoreHelper.StartNewStopwatch();

                switch (modifier.Name)
                {
                    #region Audio

                    case "setPitch": {
                            if (float.TryParse(modifier.value, out float num))
                                RTEventManager.inst.pitchOffset = num;

                            break;
                        }
                    case "addPitch": {
                            if (float.TryParse(modifier.value, out float num))
                                RTEventManager.inst.pitchOffset += num;

                            break;
                        }
                    case "setPitchMath": {
                            RTEventManager.inst.pitchOffset = RTMath.Parse(modifier.value, modifier.reference.GetObjectVariables());
                            break;
                        }
                    case "addPitchMath": {
                            RTEventManager.inst.pitchOffset += RTMath.Parse(modifier.value, modifier.reference.GetObjectVariables());
                            break;
                        }
                    case "setMusicTime": {
                            if (float.TryParse(modifier.value, out float num))
                                AudioManager.inst.SetMusicTime(num);
                            break;
                        }
                    case "setMusicTimeMath": {
                            AudioManager.inst.SetMusicTime(RTMath.Parse(modifier.value, modifier.reference.GetObjectVariables()));
                            break;
                        }
                    case "setMusicTimeStartTime": {
                            AudioManager.inst.SetMusicTime(modifier.reference.StartTime);
                            break;
                        }
                    case "setMusicTimeAutokill": {
                            AudioManager.inst.SetMusicTime(modifier.reference.StartTime + modifier.reference.SpawnDuration);
                            break;
                        }
                    case "playSound": {
                            if (bool.TryParse(modifier.commands[1], out bool global) && float.TryParse(modifier.commands[2], out float pitch) && float.TryParse(modifier.commands[3], out float vol) && bool.TryParse(modifier.commands[4], out bool loop))
                                ModifiersManager.GetSoundPath(modifier.reference.id, modifier.value, global, pitch, vol, loop);

                            break;
                        }
                    case "playSoundOnline": {
                            if (float.TryParse(modifier.commands[1], out float pitch) && float.TryParse(modifier.commands[2], out float vol) && bool.TryParse(modifier.commands[3], out bool loop) && !string.IsNullOrEmpty(modifier.value))
                                ModifiersManager.DownloadSoundAndPlay(modifier.reference.id, modifier.value, pitch, vol, loop);

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

                            if (!loop)
                                CoreHelper.StartCoroutine(AudioManager.inst.DestroyWithDelay(audioSource, clip.length / x));
                            else if (!ModifiersManager.audioSources.ContainsKey(modifier.reference.id))
                                ModifiersManager.audioSources.Add(modifier.reference.id, audioSource);

                            break;
                        }
                    case "audioSource": {
                            if (modifier.Result != null || !Updater.TryGetObject(modifier.reference, out LevelObject levelObject) || levelObject.visualObject == null ||
                                !levelObject.visualObject.GameObject)
                                break;

                            string fullPath =
                                !bool.TryParse(modifier.commands[1], out bool global) || !global ?
                                RTFile.CombinePaths(RTFile.BasePath, modifier.value) :
                                RTFile.CombinePaths(RTFile.ApplicationDirectory, ModifiersManager.SOUNDLIBRARY_PATH, modifier.value);

                            var audioDotFormats = RTFile.AudioDotFormats;
                            for (int i = 0; i < audioDotFormats.Length; i++)
                            {
                                var audioDotFormat = audioDotFormats[i];
                                if (!modifier.value.EndsWith(audioDotFormat) && RTFile.FileExists(fullPath + audioDotFormat))
                                    fullPath += audioDotFormat;
                            }

                            if (!RTFile.FileExists(fullPath))
                            {
                                CoreHelper.LogError($"File does not exist {fullPath}");
                                break;
                            }

                            if (fullPath.EndsWith(FileFormat.MP3.Dot()))
                            {
                                modifier.Result = levelObject.visualObject.GameObject.AddComponent<AudioModifier>();
                                ((AudioModifier)modifier.Result).Init(LSAudio.CreateAudioClipUsingMP3File(fullPath), modifier.reference, modifier);
                                break;
                            }

                            CoreHelper.StartCoroutine(ModifiersManager.LoadMusicFileRaw(fullPath, audioClip =>
                            {
                                if (!audioClip)
                                {
                                    CoreHelper.LogError($"Failed to load audio {fullPath}");
                                    return;
                                }

                                audioClip.name = modifier.value;

                                if (levelObject.visualObject == null || !levelObject.visualObject.GameObject)
                                    return;

                                modifier.Result = levelObject.visualObject.GameObject.AddComponent<AudioModifier>();
                                ((AudioModifier)modifier.Result).Init(audioClip, modifier.reference, modifier);
                            }));

                            break;
                        }
                    case "setMusicPlaying": {
                            SoundManager.inst.SetPlaying(modifier.GetBool(0, false));
                            break;
                        }

                    #endregion
                    #region Level

                    case "loadLevel": {
                            if (CoreHelper.IsEditing)
                            {
                                if (!ModifiersConfig.Instance.EditorLoadLevel.Value)
                                    break;

                                RTEditor.inst.ShowWarningPopup($"You are about to enter the level {modifier.value}, are you sure you want to continue? Any unsaved progress will be lost!", () =>
                                {
                                    string str = RTFile.BasePath;
                                    if (ModifiersConfig.Instance.EditorSavesBeforeLoad.Value)
                                    {
                                        GameData.Current.SaveData(str + "level-modifier-backup.lsb", () =>
                                        {
                                            EditorManager.inst.DisplayNotification($"Saved backup to {System.IO.Path.GetFileName(RTFile.RemoveEndSlash(str))}", 2f, EditorManager.NotificationType.Success);
                                        });
                                    }

                                    CoreHelper.StartCoroutine(RTEditor.inst.LoadLevel(new Level($"{RTFile.ApplicationDirectory}{RTEditor.editorListSlash}{modifier.value}")));
                                }, RTEditor.inst.HideWarningPopup);

                                break;
                            }

                            if (CoreHelper.InEditor)
                                break;

                            var levelPath = RTFile.CombinePaths(RTFile.ApplicationDirectory, LevelManager.ListSlash, $"{modifier.value}");
                            if (RTFile.FileExists(RTFile.CombinePaths(levelPath, Level.LEVEL_LSB)) || RTFile.FileExists(RTFile.CombinePaths(levelPath, Level.LEVEL_VGD)) || RTFile.FileExists(levelPath + FileFormat.ASSET.Dot()))
                                LevelManager.Load(levelPath);
                            else
                                SoundManager.inst.PlaySound(DefaultSounds.Block);

                            break;
                        }
                    case "loadLevelID": {
                            var id = modifier.GetValue(0);
                            if (string.IsNullOrEmpty(id) || id == "0" || id == "-1")
                                break;

                            if (!CoreHelper.InEditor)
                            {
                                if (LevelManager.Levels.TryFind(x => x.id == modifier.value, out Level level))
                                    LevelManager.Play(level);
                                else
                                    SoundManager.inst.PlaySound(DefaultSounds.Block);

                                break;
                            }

                            if (!CoreHelper.IsEditing)
                                break;

                            if (RTEditor.inst.LevelPanels.TryFind(x => x.Level && x.Level.metadata is MetaData metaData && metaData.ID == modifier.value, out LevelPanel editorWrapper))
                            {
                                if (!ModifiersConfig.Instance.EditorLoadLevel.Value)
                                    break;

                                var path = System.IO.Path.GetFileName(editorWrapper.FolderPath);

                                RTEditor.inst.ShowWarningPopup($"You are about to enter the level {path}, are you sure you want to continue? Any unsaved progress will be lost!", () =>
                                {
                                    string str = RTFile.BasePath;
                                    if (ModifiersConfig.Instance.EditorSavesBeforeLoad.Value)
                                    {
                                        GameData.Current.SaveData(str + "level-modifier-backup.lsb", () =>
                                        {
                                            EditorManager.inst.DisplayNotification($"Saved backup to {System.IO.Path.GetFileName(RTFile.RemoveEndSlash(str))}", 2f, EditorManager.NotificationType.Success);
                                        });
                                    }

                                    CoreHelper.StartCoroutine(RTEditor.inst.LoadLevel(editorWrapper.Level));
                                }, RTEditor.inst.HideWarningPopup);
                            }
                            else
                                SoundManager.inst.PlaySound(DefaultSounds.Block);

                            break;
                        }
                    case "loadLevelInternal": {
                            if (!CoreHelper.InEditor)
                            {
                                var filePath = RTFile.CombinePaths(RTFile.BasePath, modifier.value);
                                if (!CoreHelper.InEditor && (RTFile.FileExists(RTFile.CombinePaths(filePath, Level.LEVEL_LSB)) || RTFile.FileIsFormat(RTFile.CombinePaths(filePath, Level.LEVEL_VGD)) || RTFile.FileExists(filePath + FileFormat.ASSET.Dot())))
                                    LevelManager.Load(filePath);
                                else
                                    SoundManager.inst.PlaySound(DefaultSounds.Block);

                                break;
                            }

                            if (CoreHelper.IsEditing && RTFile.FileExists(RTFile.CombinePaths(RTFile.BasePath, EditorManager.inst.currentLoadedLevel, modifier.value, Level.LEVEL_LSB)))
                            {
                                if (!ModifiersConfig.Instance.EditorLoadLevel.Value)
                                    break;

                                RTEditor.inst.ShowWarningPopup($"You are about to enter the level {RTFile.CombinePaths(EditorManager.inst.currentLoadedLevel, modifier.value)}, are you sure you want to continue? Any unsaved progress will be lost!", () =>
                                {
                                    string str = RTFile.BasePath;
                                    if (ModifiersConfig.Instance.EditorSavesBeforeLoad.Value)
                                    {
                                        GameData.Current.SaveData(RTFile.CombinePaths(str, "level-modifier-backup.lsb"), () =>
                                        {
                                            EditorManager.inst.DisplayNotification($"Saved backup to {System.IO.Path.GetFileName(RTFile.RemoveEndSlash(str))}", 2f, EditorManager.NotificationType.Success);
                                        });
                                    }

                                    CoreHelper.StartCoroutine(RTEditor.inst.LoadLevel(new Level(RTFile.CombinePaths(EditorManager.inst.currentLoadedLevel, modifier.value))));
                                }, RTEditor.inst.HideWarningPopup);
                            }

                            break;
                        }
                    case "loadLevelPrevious": {
                            if (CoreHelper.InEditor)
                                return;

                            LevelManager.Play(LevelManager.PreviousLevel);

                            break;
                        }
                    case "loadLevelHub": {
                            if (CoreHelper.InEditor)
                                return;

                            LevelManager.Play(LevelManager.Hub);

                            break;
                        }
                    case "loadLevelInCollection": {
                            if (!CoreHelper.InEditor && LevelManager.CurrentLevelCollection && LevelManager.CurrentLevelCollection.levels.TryFind(x => x.id == modifier.value, out Level level))
                                LevelManager.Play(level);

                            break;
                        }
                    case "downloadLevel": {
                            var levelInfo = new LevelInfo(modifier.GetValue(0), modifier.GetValue(0), modifier.GetValue(1), modifier.GetValue(2), modifier.GetValue(3), modifier.GetValue(4));

                            LevelCollection.DownloadLevel(null, levelInfo, level =>
                            {
                                if (modifier.GetBool(5, true))
                                    LevelManager.Play(level);
                            });

                            break;
                        }
                    case "endLevel": {
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

                            break;
                        }
                    case "setAudioTransition": {
                            LevelManager.songFadeTransition = modifier.GetFloat(0, 0.5f);
                            break;
                        }
                    case "setIntroFade": {
                            RTGameManager.doIntroFade = modifier.GetBool(0, true);
                            break;
                        }
                    case "setLevelEndFunc": {
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

                            break;
                        }

                    #endregion
                    #region Component

                    case "blur": {
                            if (modifier.reference &&
                                modifier.reference.objectType != BeatmapObject.ObjectType.Empty &&
                                Updater.TryGetObject(modifier.reference, out LevelObject levelObject) &&
                                levelObject.visualObject.Renderer &&
                                float.TryParse(modifier.value, out float num))
                            {
                                var rend = levelObject.visualObject.Renderer;
                                if (modifier.Result == null)
                                {
                                    if (!levelObject.visualObject.GameObject.GetComponent<DestroyModifierResult>())
                                    {
                                        var onDestroy = levelObject.visualObject.GameObject.AddComponent<DestroyModifierResult>();
                                        onDestroy.Modifier = modifier;
                                    }

                                    modifier.Result = levelObject.visualObject.GameObject;
                                    rend.material = LegacyPlugin.blur;
                                }
                                if (modifier.commands.Count > 1 && bool.TryParse(modifier.commands[1], out bool r) && r)
                                    rend.material.SetFloat("_blurSizeXY", -(modifier.reference.Interpolate(3, 1) - 1f) * num);
                                else
                                    rend.material.SetFloat("_blurSizeXY", num);
                            }
                            break;
                        }
                    case "blurOther": {
                            var list = !modifier.prefabInstanceOnly ? GameData.Current.FindObjectsWithTag(modifier.commands[1]) : GameData.Current.FindObjectsWithTag(modifier.reference, modifier.commands[1]);
                            if (list.Count <= 0 || !float.TryParse(modifier.value, out float num))
                                break;

                            foreach (var beatmapObject in list)
                            {
                                if (beatmapObject.objectType == BeatmapObject.ObjectType.Empty ||
                                    !Updater.TryGetObject(beatmapObject, out LevelObject levelObject) ||
                                    !levelObject.visualObject.Renderer)
                                    continue;

                                var rend = levelObject.visualObject.Renderer;
                                if (modifier.Result == null)
                                {
                                    if (!levelObject.visualObject.GameObject.GetComponent<DestroyModifierResult>())
                                    {
                                        var onDestroy = levelObject.visualObject.GameObject.AddComponent<DestroyModifierResult>();
                                        onDestroy.Modifier = modifier;
                                    }

                                    modifier.Result = levelObject.visualObject.GameObject;
                                    rend.material = LegacyPlugin.blur;
                                }
                                rend.material.SetFloat("_blurSizeXY", -(beatmapObject.Interpolate(3, 1) - 1f) * num);
                            }

                            break;
                        }
                    case "blurVariable": {
                            if (modifier.reference &&
                                modifier.reference.objectType != BeatmapObject.ObjectType.Empty &&
                                Updater.TryGetObject(modifier.reference, out LevelObject levelObject) &&
                                levelObject.visualObject.Renderer &&
                                float.TryParse(modifier.value, out float num))
                            {
                                var rend = levelObject.visualObject.Renderer;
                                if (modifier.Result == null)
                                {
                                    if (!levelObject.visualObject.GameObject.GetComponent<DestroyModifierResult>())
                                    {
                                        var onDestroy = levelObject.visualObject.GameObject.AddComponent<DestroyModifierResult>();
                                        onDestroy.Modifier = modifier;
                                    }

                                    modifier.Result = levelObject.visualObject.GameObject;
                                    rend.material = LegacyPlugin.blur;
                                }
                                rend.material.SetFloat("_blurSizeXY", modifier.reference.integerVariable * num);
                            }
                            break;
                        }
                    case "blurVariableOther": {
                            var list = !modifier.prefabInstanceOnly ? GameData.Current.FindObjectsWithTag(modifier.commands[1]) : GameData.Current.FindObjectsWithTag(modifier.reference, modifier.commands[1]);
                            if (list.Count <= 0 || !float.TryParse(modifier.value, out float num))
                                break;

                            foreach (var beatmapObject in list)
                            {
                                if (beatmapObject.objectType == BeatmapObject.ObjectType.Empty ||
                                    !Updater.TryGetObject(beatmapObject, out LevelObject levelObject) ||
                                    !levelObject.visualObject.Renderer)
                                    continue;

                                var rend = levelObject.visualObject.Renderer;
                                if (modifier.Result == null)
                                {
                                    if (!levelObject.visualObject.GameObject.GetComponent<DestroyModifierResult>())
                                    {
                                        var onDestroy = levelObject.visualObject.GameObject.AddComponent<DestroyModifierResult>();
                                        onDestroy.Modifier = modifier;
                                    }

                                    modifier.Result = levelObject.visualObject.GameObject;
                                    rend.material = LegacyPlugin.blur;
                                }
                                rend.material.SetFloat("_blurSizeXY", beatmapObject.integerVariable * num);
                            }

                            break;
                        }
                    case "blurColored": {
                            if (modifier.reference &&
                                modifier.reference.objectType != BeatmapObject.ObjectType.Empty &&
                                Updater.TryGetObject(modifier.reference, out LevelObject levelObject) &&
                                levelObject.visualObject.Renderer &&
                                float.TryParse(modifier.value, out float num))
                            {
                                var rend = levelObject.visualObject.Renderer;
                                if (modifier.Result == null)
                                {
                                    if (!levelObject.visualObject.GameObject.GetComponent<DestroyModifierResult>())
                                    {
                                        var onDestroy = levelObject.visualObject.GameObject.AddComponent<DestroyModifierResult>();
                                        onDestroy.Modifier = modifier;
                                    }

                                    modifier.Result = levelObject.visualObject.GameObject;
                                    rend.material.shader = LegacyPlugin.blurColored;
                                }

                                if (modifier.commands.Count > 1 && bool.TryParse(modifier.commands[1], out bool r) && r)
                                    rend.material.SetFloat("_Size", -(modifier.reference.Interpolate(3, 1) - 1f) * num);
                                else
                                    rend.material.SetFloat("_Size", num);
                            }
                            break;
                        }
                    case "blurColoredOther": {
                            var list = !modifier.prefabInstanceOnly ? GameData.Current.FindObjectsWithTag(modifier.commands[1]) : GameData.Current.FindObjectsWithTag(modifier.reference, modifier.commands[1]);
                            if (list.Count <= 0 || !float.TryParse(modifier.value, out float num))
                                break;

                            foreach (var beatmapObject in list)
                            {
                                if (beatmapObject.objectType == BeatmapObject.ObjectType.Empty ||
                                    !Updater.TryGetObject(beatmapObject, out LevelObject levelObject) ||
                                    !levelObject.visualObject.Renderer)
                                    continue;

                                var rend = levelObject.visualObject.Renderer;
                                if (modifier.Result == null)
                                {
                                    if (!levelObject.visualObject.GameObject.GetComponent<DestroyModifierResult>())
                                    {
                                        var onDestroy = levelObject.visualObject.GameObject.AddComponent<DestroyModifierResult>();
                                        onDestroy.Modifier = modifier;
                                    }

                                    modifier.Result = levelObject.visualObject.GameObject;
                                    rend.material.shader = LegacyPlugin.blurColored;
                                }
                                rend.material.SetFloat("_Size", -(beatmapObject.Interpolate(3, 1) - 1f) * num);
                            }

                            break;
                        }
                    case "doubleSided": {
                            if (Updater.TryGetObject(modifier.reference, out LevelObject levelObject) && levelObject.visualObject is SolidObject solidObject && solidObject.GameObject)
                            {
                                solidObject.Renderer.material = ObjectManager.inst.norm;
                                solidObject.material = solidObject.Renderer.material;
                            }

                            break;
                        } // todo: implement gradient double sided somehow
                    case "particleSystem": {
                            if (!modifier.reference || !Updater.TryGetObject(modifier.reference, out LevelObject levelObject) || levelObject.visualObject == null || !levelObject.visualObject.GameObject)
                                break;

                            var gameObject = levelObject.visualObject.GameObject;

                            if (modifier.Result == null || modifier.Result is KeyValuePair<ParticleSystem, ParticleSystemRenderer> keyValuePair && (!keyValuePair.Key || !keyValuePair.Value))
                            {
                                var ps = gameObject.GetComponent<ParticleSystem>() ?? gameObject.AddComponent<ParticleSystem>();
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

                            break;
                        }
                    case "trailRenderer": {
                            if (!modifier.reference || !Updater.TryGetObject(modifier.reference, out LevelObject levelObject) || levelObject.visualObject == null || !levelObject.visualObject.GameObject)
                                break;

                            var mod = levelObject.visualObject.GameObject;

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

                            break;
                        }
                    case "rigidbody": {
                            if (Updater.TryGetObject(modifier.reference, out LevelObject levelObject) && levelObject.visualObject != null && levelObject.visualObject.GameObject
                                && float.TryParse(modifier.commands[1], out float gravity)
                                && int.TryParse(modifier.commands[2], out int collisionMode)
                                && float.TryParse(modifier.commands[3], out float drag)
                                && float.TryParse(modifier.commands[4], out float velocityX)
                                && float.TryParse(modifier.commands[5], out float velocityY))
                            {
                                modifier.reference.components.RemoveAll(x => !x);

                                if (!modifier.reference.components.TryFind(x => x is Rigidbody2D, out Component component))
                                {
                                    var gameObject = levelObject.visualObject.GameObject;

                                    var rigidbody2d = gameObject.GetComponent<Rigidbody2D>();

                                    if (!rigidbody2d)
                                        rigidbody2d = gameObject.AddComponent<Rigidbody2D>();

                                    modifier.reference.components.Add(rigidbody2d);

                                    rigidbody2d.gravityScale = gravity;
                                    rigidbody2d.collisionDetectionMode = (CollisionDetectionMode2D)Mathf.Clamp(collisionMode, 0, 1);
                                    rigidbody2d.drag = drag;

                                    rigidbody2d.bodyType = (RigidbodyType2D)Parser.TryParse(modifier.commands[6], 0);

                                    rigidbody2d.velocity += new Vector2(velocityX, velocityY);
                                }
                                else if (component is Rigidbody2D rigidbody)
                                {
                                    rigidbody.gravityScale = gravity;
                                    rigidbody.collisionDetectionMode = (CollisionDetectionMode2D)Mathf.Clamp(collisionMode, 0, 1);
                                    rigidbody.drag = drag;

                                    rigidbody.bodyType = (RigidbodyType2D)Parser.TryParse(modifier.commands[6], 0);

                                    rigidbody.velocity += new Vector2(velocityX, velocityY);
                                }
                            }

                            break;
                        }
                    case "rigidbodyOther": {
                            var list = !modifier.prefabInstanceOnly ? GameData.Current.FindObjectsWithTag(modifier.value) : GameData.Current.FindObjectsWithTag(modifier.reference, modifier.value);
                            if (list.Count > 0
                                        && float.TryParse(modifier.commands[1], out float gravity)
                                        && int.TryParse(modifier.commands[2], out int collisionMode)
                                        && float.TryParse(modifier.commands[3], out float drag)
                                        && float.TryParse(modifier.commands[4], out float velocityX)
                                        && float.TryParse(modifier.commands[5], out float velocityY))
                            {
                                foreach (var bm in list)
                                {
                                    if (Updater.TryGetObject(bm, out LevelObject levelObject) && levelObject.visualObject != null && levelObject.visualObject.GameObject)
                                    {
                                        bm.components.RemoveAll(x => !x);

                                        if (!bm.components.TryFind(x => x is Rigidbody2D, out Component component))
                                        {
                                            var gameObject = levelObject.visualObject.GameObject;

                                            var rigidbody = gameObject.GetComponent<Rigidbody2D>();

                                            if (!rigidbody)
                                                rigidbody = gameObject.AddComponent<Rigidbody2D>();

                                            bm.components.Add(rigidbody);

                                            rigidbody.gravityScale = gravity;
                                            rigidbody.collisionDetectionMode = (CollisionDetectionMode2D)Mathf.Clamp(collisionMode, 0, 1);
                                            rigidbody.drag = drag;

                                            rigidbody.bodyType = (RigidbodyType2D)Parser.TryParse(modifier.commands[6], 0);

                                            rigidbody.velocity += new Vector2(velocityX, velocityY);
                                        }
                                        else if (component is Rigidbody2D rigidbody)
                                        {
                                            rigidbody.gravityScale = gravity;
                                            rigidbody.collisionDetectionMode = (CollisionDetectionMode2D)Mathf.Clamp(collisionMode, 0, 1);
                                            rigidbody.drag = drag;

                                            rigidbody.bodyType = (RigidbodyType2D)Parser.TryParse(modifier.commands[6], 0);

                                            rigidbody.velocity += new Vector2(velocityX, velocityY);
                                        }
                                    }
                                }
                            }

                            break;
                        }

                    #endregion
                    #region Player

                        // todo: implement the player Index modifiers
                    case "playerHit": {
                            if (!modifier.reference || PlayerManager.Invincible || modifier.constant || !int.TryParse(modifier.GetValue(0), out int damage))
                                break;

                            var pos = Updater.TryGetObject(modifier.reference, out LevelObject levelObject) && levelObject.visualObject != null && levelObject.visualObject.GameObject ? levelObject.visualObject.GameObject.transform.position : modifier.reference.InterpolateChainPosition();

                            var player = PlayerManager.GetClosestPlayer(pos);

                            if (player && player.Player)
                                player.Player.Hit(damage);

                            break;
                        }
                    case "playerHitIndex": {
                            if (!modifier.reference || PlayerManager.Invincible || modifier.constant)
                                break;

                            var damage = Mathf.Clamp(modifier.GetInt(0, 1), 0, int.MaxValue);
                            if (PlayerManager.Players.TryGetAt(modifier.GetInt(0, 0), out CustomPlayer customPlayer) && customPlayer.Player)
                                customPlayer.Player.Hit(damage);

                            break;
                        }
                    case "playerHitAll": {
                            if (!PlayerManager.Invincible && !modifier.constant && int.TryParse(modifier.GetValue(0), out int damage))
                            {
                                damage = Mathf.Clamp(damage, 0, int.MaxValue);
                                foreach (var player in PlayerManager.Players.Where(x => x.Player))
                                    player.Player.Hit(damage);
                            }

                            break;
                        }

                    case "playerHeal": {
                            if (!modifier.reference || PlayerManager.Invincible || modifier.constant)
                                break;

                            var heal = Mathf.Clamp(modifier.GetInt(0, 1), 0, int.MaxValue);

                            var pos = Updater.TryGetObject(modifier.reference, out LevelObject levelObject) && levelObject.visualObject != null && levelObject.visualObject.GameObject ? levelObject.visualObject.GameObject.transform.position : modifier.reference.InterpolateChainPosition();

                            var player = PlayerManager.GetClosestPlayer(pos);

                            if (player && player.Player)
                                player.Player.Heal(heal);

                            break;
                        }
                    case "playerHealIndex": {
                            if (!modifier.reference || PlayerManager.Invincible || modifier.constant)
                                break;

                            var health = Mathf.Clamp(modifier.GetInt(0, 1), 0, int.MaxValue);
                            if (PlayerManager.Players.TryGetAt(modifier.GetInt(1, 0), out CustomPlayer customPlayer) && customPlayer.Player)
                                customPlayer.Player.Heal(health);

                            break;
                        }
                    case "playerHealAll": {
                            if (!PlayerManager.Invincible && !modifier.constant)
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
                            break;
                        }

                    case "playerKill": {
                            if (!modifier.reference || PlayerManager.Invincible || modifier.constant)
                                break;

                            var pos = Updater.TryGetObject(modifier.reference, out LevelObject levelObject) && levelObject.visualObject != null && levelObject.visualObject.GameObject ? levelObject.visualObject.GameObject.transform.position : modifier.reference.InterpolateChainPosition();

                            var player = PlayerManager.GetClosestPlayer(pos);

                            if (player && player.Player)
                                player.Player.Kill();

                            break;
                        }
                    case "playerKillIndex": {
                            if (!modifier.reference || PlayerManager.Invincible || modifier.constant)
                                break;

                            if (PlayerManager.Players.TryGetAt(modifier.GetInt(0, 0), out CustomPlayer customPlayer) && customPlayer.Player)
                                customPlayer.Player.Kill();

                            break;
                        }
                    case "playerKillAll": {
                            if (!PlayerManager.Invincible && !modifier.constant)
                                foreach (var player in PlayerManager.Players)
                                    if (player.Player)
                                        player.Player.Kill();

                            break;
                        }

                    case "playerRespawn": {
                            if (!modifier.reference || modifier.constant)
                                break;

                            var pos = Updater.TryGetObject(modifier.reference, out LevelObject levelObject) && levelObject.visualObject != null && levelObject.visualObject.GameObject ? levelObject.visualObject.GameObject.transform.position : modifier.reference.InterpolateChainPosition();

                            var playerIndex = PlayerManager.GetClosestPlayerIndex(pos);

                            if (playerIndex >= 0)
                                PlayerManager.RespawnPlayer(playerIndex);

                            break;
                        }
                    case "playerRespawnIndex": {
                            if (!modifier.reference || modifier.constant)
                                break;

                            var playerIndex = modifier.GetInt(0, 0);
                            if (playerIndex >= 0 && playerIndex < InputDataManager.inst.players.Count)
                                PlayerManager.RespawnPlayer(playerIndex);

                            break;
                        }
                    case "playerRespawnAll": {
                            if (!modifier.reference || modifier.constant)
                                break;

                            PlayerManager.RespawnPlayers();
                            break;
                        }

                        // todo: probably rework these
                    case "playerMove": {
                            if (!modifier.reference)
                                break;

                            var pos = Updater.TryGetObject(modifier.reference, out LevelObject levelObject) && levelObject.visualObject != null && levelObject.visualObject.GameObject ? levelObject.visualObject.GameObject.transform.position : modifier.reference.InterpolateChainPosition();

                            var player = PlayerManager.GetClosestPlayer(pos);

                            var vector = modifier.value.Split(new char[] { ',' });

                            bool relative = Parser.TryParse(modifier.commands[3], false);
                            if (!player)
                                break;

                            var tf = player.Player.rb.transform;
                            if (modifier.constant)
                                tf.localPosition = new Vector3(Parser.TryParse(vector[0], 0f), Parser.TryParse(vector[1], 0f), 0f);
                            else
                                tf
                                    .DOLocalMove(new Vector3(Parser.TryParse(vector[0], 0f) + (relative ? tf.position.x : 0f), Parser.TryParse(vector[1], 0f) + (relative ? tf.position.y : 0f), 0f), Parser.TryParse(modifier.commands[1], 1f))
                                    .SetEase(DataManager.inst.AnimationList[Parser.TryParse(modifier.commands[2], 0)].Animation);

                            break;
                        }
                    case "playerMoveAll": {
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

                            break;
                        }
                    case "playerMoveX": {
                            if (!modifier.reference)
                                break;

                            var pos = Updater.TryGetObject(modifier.reference, out LevelObject levelObject) && levelObject.visualObject != null && levelObject.visualObject.GameObject ? levelObject.visualObject.GameObject.transform.position : modifier.reference.InterpolateChainPosition();

                            var player = PlayerManager.GetClosestPlayer(pos);

                            bool relative = Parser.TryParse(modifier.commands[3], false);
                            if (!player)
                                break;

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

                            break;
                        }
                    case "playerMoveXAll": {
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

                            break;
                        }
                    case "playerMoveY": {
                            if (!modifier.reference)
                                break;

                            var pos = Updater.TryGetObject(modifier.reference, out LevelObject levelObject) && levelObject.visualObject != null && levelObject.visualObject.GameObject ? levelObject.visualObject.GameObject.transform.position : modifier.reference.InterpolateChainPosition();

                            var player = PlayerManager.GetClosestPlayer(pos);

                            bool relative = Parser.TryParse(modifier.commands[3], false);
                            if (!player)
                                break;

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

                            break;
                        }
                    case "playerMoveYAll": {
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

                            break;
                        }
                    case "playerRotate": {
                            if (!modifier.reference)
                                break;

                            var pos = Updater.TryGetObject(modifier.reference, out LevelObject levelObject) && levelObject.visualObject != null && levelObject.visualObject.GameObject ? levelObject.visualObject.GameObject.transform.position : modifier.reference.InterpolateChainPosition();

                            var player = PlayerManager.GetClosestPlayer(pos);

                            bool relative = Parser.TryParse(modifier.commands[3], false);
                            if (!player)
                                break;

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

                            break;
                        }
                    case "playerRotateAll": {
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

                            break;
                        }

                    case "playerMoveToObject": {
                            if (!modifier.reference)
                                break;

                            var pos = Updater.TryGetObject(modifier.reference, out LevelObject levelObject) && levelObject.visualObject != null && levelObject.visualObject.GameObject ? levelObject.visualObject.GameObject.transform.position : modifier.reference.InterpolateChainPosition();

                            var player = PlayerManager.GetClosestPlayer(pos);

                            if (!player || !player.Player || !player.Player.rb)
                                break;
                            
                            player.Player.rb.position = new Vector3(pos.x, pos.y, 0f);

                            break;
                        }
                    case "playerMoveAllToObject": {
                            if (!modifier.reference)
                                break;

                            var pos = Updater.TryGetObject(modifier.reference, out LevelObject levelObject) && levelObject.visualObject != null && levelObject.visualObject.GameObject ? levelObject.visualObject.GameObject.transform.position : modifier.reference.InterpolateChainPosition();

                            foreach (var player in PlayerManager.Players.Where(x => x.Player))
                            {
                                if (!player.Player.rb)
                                    break;

                                player.Player.rb.position = new Vector3(pos.x, pos.y, 0f);
                            }

                            break;
                        }
                    case "playerMoveXToObject": {
                            if (!modifier.reference)
                                break;

                            var pos = Updater.TryGetObject(modifier.reference, out LevelObject levelObject) && levelObject.visualObject != null && levelObject.visualObject.GameObject ? levelObject.visualObject.GameObject.transform.position : modifier.reference.InterpolateChainPosition();

                            var player = PlayerManager.GetClosestPlayer(pos);

                            if (!player || !player.Player || !player.Player.rb)
                                break;

                            var y = player.Player.rb.position.y;
                            player.Player.rb.position = new Vector2(pos.x, y);

                            break;
                        }
                    case "playerMoveXAllToObject": {
                            if (!modifier.reference)
                                break;

                            var pos = Updater.TryGetObject(modifier.reference, out LevelObject levelObject) && levelObject.visualObject != null && levelObject.visualObject.GameObject ? levelObject.visualObject.GameObject.transform.position : modifier.reference.InterpolateChainPosition();

                            foreach (var player in PlayerManager.Players.Where(x => x.Player))
                            {
                                if (!player.Player.rb)
                                    break;

                                var y = player.Player.rb.position.y;
                                player.Player.rb.position = new Vector2(pos.x, y);
                            }

                            break;
                        }
                    case "playerMoveYToObject": {
                            if (!modifier.reference)
                                break;

                            var pos = Updater.TryGetObject(modifier.reference, out LevelObject levelObject) && levelObject.visualObject != null && levelObject.visualObject.GameObject ? levelObject.visualObject.GameObject.transform.position : modifier.reference.InterpolateChainPosition();

                            var player = PlayerManager.GetClosestPlayer(pos);

                            if (!player || !player.Player || !player.Player.rb)
                                break;

                            var x = player.Player.rb.position.x;
                            player.Player.rb.position = new Vector2(x, pos.y);

                            break;
                        }
                    case "playerMoveYAllToObject": {
                            if (!modifier.reference)
                                break;

                            var pos = Updater.TryGetObject(modifier.reference, out LevelObject levelObject) && levelObject.visualObject != null && levelObject.visualObject.GameObject ? levelObject.visualObject.GameObject.transform.position : modifier.reference.InterpolateChainPosition();

                            foreach (var player in PlayerManager.Players.Where(x => x.Player))
                            {
                                if (!player.Player.rb)
                                    break;

                                var x = player.Player.rb.position.x;
                                player.Player.rb.position = new Vector2(x, pos.y);
                            }

                            break;
                        }
                    case "playerRotateToObject": {
                            if (!modifier.reference)
                                break;

                            var pos = Updater.TryGetObject(modifier.reference, out LevelObject levelObject) && levelObject.visualObject != null && levelObject.visualObject.GameObject ? levelObject.visualObject.GameObject.transform.position : modifier.reference.InterpolateChainPosition();

                            var player = PlayerManager.GetClosestPlayer(pos);

                            if (!player || !player.Player || !player.Player.rb)
                                break;

                            player.Player.rb.transform.SetLocalRotationEulerZ(levelObject.visualObject.GameObject.transform.localRotation.eulerAngles.z);

                            break;
                        }
                    case "playerRotateAllToObject": {
                            if (!modifier.reference)
                                break;

                            var rot = Updater.TryGetObject(modifier.reference, out LevelObject levelObject) && levelObject.visualObject != null && levelObject.visualObject.GameObject ? levelObject.visualObject.GameObject.transform.localRotation.eulerAngles.z : modifier.reference.InterpolateChainRotation();

                            foreach (var player in PlayerManager.Players.Where(x => x.Player))
                            {
                                if (!player.Player.rb)
                                    break;

                                player.Player.rb.transform.SetLocalRotationEulerZ(rot);
                            }

                            break;
                        }

                    case "playerBoost": {
                            if (!modifier.constant && modifier.reference && Updater.TryGetObject(modifier.reference, out LevelObject levelObject) && levelObject.visualObject.GameObject)
                                PlayerManager.GetClosestPlayer(levelObject.visualObject.GameObject.transform.position)?.Player?.StartBoost();

                            break;
                        }
                    case "playerBoostIndex": {
                            if (!modifier.constant && PlayerManager.Players.TryGetAt(modifier.GetInt(0, 0), out CustomPlayer customPlayer) && customPlayer.Player)
                                customPlayer.Player.StartBoost();

                            break;
                        }
                    case "playerBoostAll": {
                            foreach (var player in PlayerManager.Players.Where(x => x.Player))
                                player.Player.StartBoost();

                            break;
                        }

                    case "playerDisableBoost": {
                            if (modifier.reference && Updater.TryGetObject(modifier.reference, out LevelObject levelObject) && levelObject.visualObject.GameObject)
                            {
                                var player = PlayerManager.GetClosestPlayer(levelObject.visualObject.GameObject.transform.position);

                                if (player && player.Player)
                                    player.Player.CanBoost = false;
                            }

                            break;
                        }
                    case "playerDisableBoostIndex": {
                            if (modifier.reference && PlayerManager.Players.TryGetAt(modifier.GetInt(0, 0), out CustomPlayer customPlayer) && customPlayer.Player)
                                customPlayer.Player.CanBoost = false;

                            break;
                        }
                    case "playerDisableBoostAll": {
                            foreach (var player in PlayerManager.Players.Where(x => x.Player))
                                player.Player.CanBoost = false;

                            break;
                        }

                    case "playerEnableBoost": {
                            if (modifier.reference && Updater.TryGetObject(modifier.reference, out LevelObject levelObject) && levelObject.visualObject.GameObject)
                            {
                                var player = PlayerManager.GetClosestPlayer(levelObject.visualObject.GameObject.transform.position);

                                if (player && player.Player)
                                    player.Player.CanBoost = true;
                            }

                            break;
                        }
                    case "playerEnableBoostIndex": {
                            if (modifier.reference && PlayerManager.Players.TryGetAt(modifier.GetInt(0, 0), out CustomPlayer customPlayer) && customPlayer.Player)
                                customPlayer.Player.CanBoost = true;

                            break;
                        }
                    case "playerEnableBoostAll": {
                            foreach (var player in PlayerManager.Players.Where(x => x.Player))
                                player.Player.CanBoost = true;

                            break;
                        }

                    case "playerSpeed": {
                            if (float.TryParse(modifier.value, out float speed))
                                RTPlayer.SpeedMultiplier = speed;

                            break;
                        }
                    case "playerVelocityAll": {
                            if (!float.TryParse(modifier.commands[1], out float x) || !float.TryParse(modifier.commands[2], out float y))
                                break;

                            for (int i = 0; i < PlayerManager.Players.Count; i++)
                            {
                                var player = PlayerManager.Players[i];
                                if (player.Player && player.Player.rb)
                                    player.Player.rb.velocity = new Vector2(x, y);
                            }

                            break;
                        }
                    case "playerVelocityXAll": {
                            if (!float.TryParse(modifier.value, out float x))
                                break;

                            for (int i = 0; i < PlayerManager.Players.Count; i++)
                            {
                                var player = PlayerManager.Players[i];
                                if (!player.Player || !player.Player.rb)
                                    continue;

                                var velocity = player.Player.rb.velocity;
                                velocity.x = x;
                                player.Player.rb.velocity = velocity;
                            }

                            break;
                        }
                    case "playerVelocityYAll": {
                            if (!float.TryParse(modifier.value, out float x))
                                break;

                            for (int i = 0; i < PlayerManager.Players.Count; i++)
                            {
                                var player = PlayerManager.Players[i];
                                if (!player.Player || !player.Player.rb)
                                    continue;

                                var velocity = player.Player.rb.velocity;
                                velocity.y = x;
                                player.Player.rb.velocity = velocity;
                            }

                            break;
                        }
                    case "setPlayerModel": {
                            if (modifier.constant || !int.TryParse(modifier.commands[1], out int index) || !PlayersData.Current.playerModels.ContainsKey(modifier.value))
                                break;

                            PlayersData.Current.SetPlayerModel(index, modifier.value);
                            PlayerManager.AssignPlayerModels();

                            if (PlayerManager.Players.Count <= index || !PlayerManager.Players[index].Player)
                                break;

                            PlayerManager.Players[index].Player.playerNeedsUpdating = true;
                            PlayerManager.Players[index].Player.UpdateModel();

                            break;
                        }
                    case "gameMode": {
                            if (int.TryParse(modifier.GetValue(0), out int value))
                                RTPlayer.GameMode = (GameMode)value;

                            break;
                        }

                    #endregion
                    #region Mouse Cursor

                    case "showMouse": {
                            CursorManager.inst.ShowCursor();
                            break;
                        }
                    case "hideMouse": {
                            if (CoreHelper.InEditorPreview)
                                CursorManager.inst.HideCursor();
                            break;
                        }
                    case "setMousePosition": {
                            if (CoreHelper.IsEditing)
                                break;

                            var screenScale = Display.main.systemWidth / 1920f;
                            float windowCenterX = (Display.main.systemWidth) / 2;
                            float windowCenterY = (Display.main.systemHeight) / 2;

                            if (int.TryParse(modifier.commands[1], out int x) && int.TryParse(modifier.commands[2], out int y))
                                CursorManager.inst.SetCursorPosition(new Vector2(((x * screenScale) + windowCenterX), ((y * screenScale) + windowCenterY)));

                            break;
                        }
                    case "followMousePosition": {
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

                            break;
                        }

                    #endregion
                    #region Variable

                        // todo: consider reworking these to reference the objects' self instead of an object group and add "variable other" modifiers.
                    case "addVariable": {

                            if (modifier.commands.Count == 2)
                            {
                                var list = !modifier.prefabInstanceOnly ? GameData.Current.FindObjectsWithTag(modifier.commands[1]) : GameData.Current.FindObjectsWithTag(modifier.reference, modifier.commands[1]);
                                if (list.Count <= 0 || !int.TryParse(modifier.value, out int num))
                                    break;

                                foreach (var beatmapObject in list)
                                    beatmapObject.integerVariable += num;
                            }
                            else
                                modifier.reference.integerVariable += modifier.GetInt(0, 0);

                            break;
                        }
                    case "addVariableOther": {
                            var list = !modifier.prefabInstanceOnly ? GameData.Current.FindObjectsWithTag(modifier.commands[1]) : GameData.Current.FindObjectsWithTag(modifier.reference, modifier.commands[1]);
                            if (list.Count <= 0 || !int.TryParse(modifier.value, out int num))
                                break;

                            foreach (var beatmapObject in list)
                                beatmapObject.integerVariable += num;

                            break;
                        }
                    case "subVariable": {
                            if (modifier.commands.Count == 2)
                            {
                                var list = !modifier.prefabInstanceOnly ? GameData.Current.FindObjectsWithTag(modifier.commands[1]) : GameData.Current.FindObjectsWithTag(modifier.reference, modifier.commands[1]);
                                if (list.Count <= 0 || !int.TryParse(modifier.value, out int num))
                                    break;

                                foreach (var beatmapObject in list)
                                    beatmapObject.integerVariable -= num;
                            }
                            else
                                modifier.reference.integerVariable -= modifier.GetInt(0, 0);

                            break;
                        }
                    case "subVariableOther": {
                            var list = !modifier.prefabInstanceOnly ? GameData.Current.FindObjectsWithTag(modifier.commands[1]) : GameData.Current.FindObjectsWithTag(modifier.reference, modifier.commands[1]);
                            if (list.Count <= 0 || !int.TryParse(modifier.value, out int num))
                                break;

                            foreach (var beatmapObject in list)
                                beatmapObject.integerVariable -= num;

                            break;
                        }
                    case "setVariable": {
                            if (modifier.commands.Count == 2)
                            {
                                var list = !modifier.prefabInstanceOnly ? GameData.Current.FindObjectsWithTag(modifier.commands[1]) : GameData.Current.FindObjectsWithTag(modifier.reference, modifier.commands[1]);
                                if (list.Count <= 0 || !int.TryParse(modifier.value, out int num))
                                    break;

                                foreach (var beatmapObject in list)
                                    beatmapObject.integerVariable = num;
                            }
                            else
                                modifier.reference.integerVariable = modifier.GetInt(0, 0);

                            break;
                        }
                    case "setVariableOther": {
                            var list = !modifier.prefabInstanceOnly ? GameData.Current.FindObjectsWithTag(modifier.commands[1]) : GameData.Current.FindObjectsWithTag(modifier.reference, modifier.commands[1]);
                            if (list.Count <= 0 || !int.TryParse(modifier.value, out int num))
                                break;

                            foreach (var beatmapObject in list)
                                beatmapObject.integerVariable = num;

                            break;
                        }
                    case "setVariableRandom": {
                            if (modifier.commands.Count == 3)
                            {
                                var list = !modifier.prefabInstanceOnly ? GameData.Current.FindObjectsWithTag(modifier.value) : GameData.Current.FindObjectsWithTag(modifier.reference, modifier.value);
                                if (list.Count <= 0 || !int.TryParse(modifier.commands[1], out int min) || !int.TryParse(modifier.commands[2], out int max))
                                    break;

                                foreach (var beatmapObject in list)
                                    beatmapObject.integerVariable = UnityEngine.Random.Range(min, max < 0 ? max - 1 : max + 1);
                            }
                            else
                            {
                                var min = modifier.GetInt(0, 0);
                                var max = modifier.GetInt(1, 0);
                                modifier.reference.integerVariable = UnityEngine.Random.Range(min, max < 0 ? max - 1 : max + 1);
                            }

                            break;
                        }
                    case "setVariableRandomOther": {
                            var list = !modifier.prefabInstanceOnly ? GameData.Current.FindObjectsWithTag(modifier.value) : GameData.Current.FindObjectsWithTag(modifier.reference, modifier.value);
                            if (list.Count <= 0 || !int.TryParse(modifier.commands[1], out int min) || !int.TryParse(modifier.commands[2], out int max))
                                break;

                            foreach (var beatmapObject in list)
                                beatmapObject.integerVariable = UnityEngine.Random.Range(min, max < 0 ? max - 1 : max + 1);

                            break;
                        }
                    case "animateVariableOther": {
                            var list = !modifier.prefabInstanceOnly ? GameData.Current.FindObjectsWithTag(modifier.value) : GameData.Current.FindObjectsWithTag(modifier.reference, modifier.value);
                            if (list.Count <= 0 || !int.TryParse(modifier.commands[1], out int fromType) || !int.TryParse(modifier.commands[2], out int fromAxis) ||
                                !float.TryParse(modifier.commands[3], out float delay) || !float.TryParse(modifier.commands[4], out float multiply) ||
                                !float.TryParse(modifier.commands[5], out float offset) || !float.TryParse(modifier.commands[6], out float min) ||
                                !float.TryParse(modifier.commands[7], out float max) || !float.TryParse(modifier.commands[8], out float loop))
                                break;

                            for (int i = 0; i < list.Count; i++)
                            {
                                var beatmapObject = list[i];
                                var time = AudioManager.inst.CurrentAudioSource.time;

                                fromType = Mathf.Clamp(fromType, 0, beatmapObject.events.Count);
                                fromAxis = Mathf.Clamp(fromAxis, 0, beatmapObject.events[fromType][0].eventValues.Length);

                                if (!Updater.levelProcessor.converter.cachedSequences.TryGetValue(beatmapObject.id, out ObjectConverter.CachedSequences cachedSequence))
                                    continue;

                                switch (fromType)
                                {
                                    // To Type Position
                                    // To Axis X
                                    // From Type Position
                                    case 0:
                                        {
                                            var sequence = cachedSequence.Position3DSequence.Interpolate(time - beatmapObject.StartTime - delay);

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

                            break;
                        }
                    case "clampVariable": {
                            modifier.reference.integerVariable = Mathf.Clamp(modifier.reference.integerVariable, Parser.TryParse(modifier.commands.Count > 1 ? modifier.commands[1] : "1", 0), Parser.TryParse(modifier.commands.Count > 2 ? modifier.commands[2] : "1", 1));
                            break;
                        }
                    case "clampVariableOther": {
                            var list = !modifier.prefabInstanceOnly ? GameData.Current.FindObjectsWithTag(modifier.commands[1]) : GameData.Current.FindObjectsWithTag(modifier.reference, modifier.commands[1]);

                            if (list.Count > 0)
                                foreach (var bm in list)
                                    bm.integerVariable = Mathf.Clamp(bm.integerVariable, Parser.TryParse(modifier.commands.Count > 1 ? modifier.commands[1] : "1", 0), Parser.TryParse(modifier.commands.Count > 2 ? modifier.commands[2] : "1", 1));

                            break;
                        }

                    #endregion
                    #region Enable / Disable

                    case "enableObject": {
                            if (Updater.TryGetObject(modifier.reference, out LevelObject levelObject) && levelObject.top)
                                levelObject.top.gameObject.SetActive(true);

                            break;
                        }
                    case "enableObjectTree": {
                            if (modifier.GetValue(0) == "0")
                                modifier.SetValue(0, "False");

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
                    case "enableObjectOther": {
                            var list = !modifier.prefabInstanceOnly ? GameData.Current.FindObjectsWithTag(modifier.value) : GameData.Current.FindObjectsWithTag(modifier.reference, modifier.value);

                            if (list.Count > 0)
                                foreach (var beatmapObject in list)
                                    if (Updater.TryGetObject(beatmapObject, out LevelObject levelObject) && levelObject.top)
                                        levelObject.top.gameObject.SetActive(true);

                            break;
                        }
                    case "enableObjectTreeOther": {
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

                            break;
                        }
                    case "disableObject": {
                            if (Updater.TryGetObject(modifier.reference, out LevelObject levelObject) && levelObject.top)
                                levelObject.top.gameObject.SetActive(false);

                            break;
                        }
                    case "disableObjectTree": {
                            if (modifier.GetValue(0) == "0")
                                modifier.SetValue(0, "False");

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
                    case "disableObjectOther": {
                            var list = !modifier.prefabInstanceOnly ? GameData.Current.FindObjectsWithTag(modifier.value) : GameData.Current.FindObjectsWithTag(modifier.reference, modifier.value);

                            if (list.Count > 0)
                                foreach (var beatmapObject in list)
                                    if (Updater.TryGetObject(beatmapObject, out LevelObject levelObject) && levelObject.top)
                                        levelObject.top.gameObject.SetActive(false);

                            break;
                        }
                    case "disableObjectTreeOther": {
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

                            break;
                        }

                    #endregion
                    #region JSON

                    case "saveFloat": {
                            if (CoreHelper.InEditorPreview && float.TryParse(modifier.value, out float num))
                                ModifiersManager.SaveProgress(modifier.commands[1], modifier.commands[2], modifier.commands[3], num);

                            break;
                        }
                    case "saveString": {
                            if (CoreHelper.InEditorPreview)
                                ModifiersManager.SaveProgress(modifier.commands[1], modifier.commands[2], modifier.commands[3], modifier.value);

                            break;
                        }
                    case "saveText": {
                            if (CoreHelper.InEditorPreview && modifier.reference != null && Updater.TryGetObject(modifier.reference, out LevelObject levelObject)
                                && levelObject.visualObject is TextObject textObject)
                                ModifiersManager.SaveProgress(modifier.commands[1], modifier.commands[2], modifier.commands[3], textObject.textMeshPro.text);

                            break;
                        }
                    case "saveVariable": {
                            if (CoreHelper.InEditorPreview)
                                ModifiersManager.SaveProgress(modifier.commands[1], modifier.commands[2], modifier.commands[3], modifier.reference.integerVariable);

                            break;
                        }
                    case "loadVariable": {
                            var path = RTFile.CombinePaths(RTFile.ApplicationDirectory, "profile", modifier.GetValue(1) + FileFormat.SES.Dot());
                            if (!RTFile.FileExists(path))
                                break;

                            string json = RTFile.ReadFromFile(path);

                            if (string.IsNullOrEmpty(json))
                                break;

                            var jn = JSON.Parse(json);

                            if (!string.IsNullOrEmpty(jn[modifier.commands[2]][modifier.commands[3]]["float"]) &&
                                float.TryParse(jn[modifier.commands[2]][modifier.commands[3]]["float"], out float eq))
                                modifier.reference.integerVariable = (int)eq;

                            break;
                        }
                    case "loadVariableOther": {
                            var path = RTFile.CombinePaths(RTFile.ApplicationDirectory, "profile", modifier.GetValue(1) + FileFormat.SES.Dot());
                            if (!RTFile.FileExists(path))
                                break;

                            string json = RTFile.ReadFromFile(path);

                            if (string.IsNullOrEmpty(json))
                                break;

                            var jn = JSON.Parse(json);
                            
                            var list = !modifier.prefabInstanceOnly ? GameData.Current.FindObjectsWithTag(modifier.value) : GameData.Current.FindObjectsWithTag(modifier.reference, modifier.value);

                            if (list.Count > 0 && !string.IsNullOrEmpty(jn[modifier.commands[2]][modifier.commands[3]]["float"]) &&
                                float.TryParse(jn[modifier.commands[2]][modifier.commands[3]]["float"], out float eq))
                            {
                                foreach (var bm in list)
                                    bm.integerVariable = (int)eq;
                            }

                            break;
                        }

                    #endregion
                    #region Reactive

                    case "reactivePos": {
                            if (modifier.reference && Updater.TryGetObject(modifier.reference, out LevelObject levelObject)
                                && int.TryParse(modifier.commands[1], out int sampleX) && float.TryParse(modifier.commands[3], out float intensityX)
                                && int.TryParse(modifier.commands[2], out int sampleY) && float.TryParse(modifier.commands[4], out float intensityY)
                                && float.TryParse(modifier.value, out float val))
                            {
                                float reactivePositionX = Updater.GetSample(sampleX, intensityX * val);
                                float reactivePositionY = Updater.GetSample(sampleY, intensityY * val);

                                levelObject.visualObject.SetOrigin(new Vector3(modifier.reference.origin.x + reactivePositionX, modifier.reference.origin.y + reactivePositionY, modifier.reference.depth * 0.1f));
                            }
                            break;
                        }
                    case "reactiveSca": {
                            if (modifier.reference && Updater.TryGetObject(modifier.reference, out LevelObject levelObject) && levelObject.visualObject.GameObject
                                && int.TryParse(modifier.commands[1], out int sampleX) && float.TryParse(modifier.commands[3], out float intensityX)
                                && int.TryParse(modifier.commands[2], out int sampleY) && float.TryParse(modifier.commands[4], out float intensityY)
                                && float.TryParse(modifier.value, out float val))
                            {
                                float reactiveScaleX = Updater.GetSample(sampleX, intensityX * val);
                                float reactiveScaleY = Updater.GetSample(sampleY, intensityY * val);

                                levelObject.visualObject.SetScaleOffset(new Vector2(1f + reactiveScaleX, 1f + reactiveScaleY));
                            }
                            break;
                        }
                    case "reactiveRot": {
                            if (modifier.reference && Updater.TryGetObject(modifier.reference, out LevelObject levelObject) && levelObject.visualObject.GameObject
                                && int.TryParse(modifier.commands[1], out int sample) && float.TryParse(modifier.value, out float val))
                                levelObject.visualObject.SetRotationOffset(Updater.GetSample(sample, val));

                            break;
                        }
                    case "reactiveCol": {
                            if (modifier.reference && Updater.TryGetObject(modifier.reference, out LevelObject levelObject) && levelObject.visualObject.Renderer
                                && int.TryParse(modifier.commands[1], out int sample) && float.TryParse(modifier.value, out float val))
                            {
                                sample = Mathf.Clamp(sample, 0, 255);

                                float reactiveColor = Updater.GetSample(sample, val);

                                if (levelObject.visualObject.Renderer != null && int.TryParse(modifier.commands[2], out int col))
                                    levelObject.visualObject.Renderer.material.color += GameManager.inst.LiveTheme.objectColors[col] * reactiveColor;
                            }

                            break;
                        }
                    case "reactiveColLerp": {
                            if (modifier.reference && Updater.TryGetObject(modifier.reference, out LevelObject levelObject) && levelObject.visualObject.Renderer
                                && int.TryParse(modifier.commands[1], out int sample) && float.TryParse(modifier.value, out float val))
                            {
                                sample = Mathf.Clamp(sample, 0, 255);

                                float reactiveColor = Updater.GetSample(sample, val);

                                if (levelObject.visualObject.Renderer != null && int.TryParse(modifier.commands[2], out int col))
                                    levelObject.visualObject.Renderer.material.color = RTMath.Lerp(levelObject.visualObject.Renderer.material.color, GameManager.inst.LiveTheme.objectColors[col], reactiveColor);
                            }

                            break;
                        }
                    case "reactivePosChain": {
                            if (modifier.reference
                                && int.TryParse(modifier.commands[1], out int sampleX) && float.TryParse(modifier.commands[3], out float intensityX)
                                && int.TryParse(modifier.commands[2], out int sampleY) && float.TryParse(modifier.commands[4], out float intensityY)
                                && float.TryParse(modifier.value, out float val))
                            {
                                float reactivePositionX = Updater.GetSample(sampleX, intensityX * val);
                                float reactivePositionY = Updater.GetSample(sampleY, intensityY * val);

                                modifier.reference.reactivePositionOffset = new Vector3(reactivePositionX, reactivePositionY);
                            }

                            break;
                        }
                    case "reactiveScaChain": {
                            if (modifier.reference
                                && int.TryParse(modifier.commands[1], out int sampleX) && float.TryParse(modifier.commands[3], out float intensityX)
                                && int.TryParse(modifier.commands[2], out int sampleY) && float.TryParse(modifier.commands[4], out float intensityY)
                                && float.TryParse(modifier.value, out float val))
                            {
                                float reactiveScaleX = Updater.GetSample(sampleX, intensityX * val);
                                float reactiveScaleY = Updater.GetSample(sampleY, intensityY * val);

                                modifier.reference.reactiveScaleOffset = new Vector3(reactiveScaleX, reactiveScaleY, 1f);
                            }

                            break;
                        }
                    case "reactiveRotChain": {
                            if (modifier.reference && int.TryParse(modifier.commands[1], out int sample) && float.TryParse(modifier.value, out float val))
                            {
                                float reactiveRotation = Updater.GetSample(sample, val);

                                modifier.reference.reactiveRotationOffset = reactiveRotation;
                            }
                            break;
                        }

                    #endregion
                    #region Event Offset

                    case "eventOffset": {
                            if (RTEventManager.inst && RTEventManager.inst.offsets != null)
                                RTEventManager.inst.SetOffset(modifier.GetInt(1, 0), modifier.GetInt(2, 0), modifier.GetFloat(0, 1f));

                            break;
                        }
                    case "eventOffsetVariable": {
                            if (RTEventManager.inst && RTEventManager.inst.offsets != null)
                                RTEventManager.inst.SetOffset(modifier.GetInt(1, 0), modifier.GetInt(2, 0), modifier.reference.integerVariable * modifier.GetFloat(0, 1f));

                            break;
                        }
                    case "eventOffsetMath": {
                            if (RTEventManager.inst && RTEventManager.inst.offsets != null)
                                RTEventManager.inst.SetOffset(modifier.GetInt(1, 0), modifier.GetInt(2, 0), RTMath.Parse(modifier.GetValue(0), modifier.reference.GetObjectVariables()));

                            break;
                        }
                    case "eventOffsetAnimate": {
                            if (!modifier.constant && RTEventManager.inst && RTEventManager.inst.offsets != null)
                            {
                                string easing = modifier.commands[4];
                                if (int.TryParse(modifier.commands[4], out int e) && e >= 0 && e < DataManager.inst.AnimationList.Count)
                                    easing = DataManager.inst.AnimationList[e].Name;

                                var list = RTEventManager.inst.offsets;

                                var eventType = Parser.TryParse(modifier.commands[1], 0);
                                var indexValue = Parser.TryParse(modifier.commands[2], 0);

                                if (eventType < list.Count && indexValue < list[eventType].Count)
                                {
                                    var value = Parser.TryParse(modifier.commands[5], false) ? list[eventType][indexValue] + Parser.TryParse(modifier.value, 0f) : Parser.TryParse(modifier.value, 0f);

                                    var animation = new RTAnimation("Event Offset Animation");
                                    animation.animationHandlers = new List<AnimationHandlerBase>
                                    {
                                        new AnimationHandler<float>(new List<IKeyframe<float>>
                                        {
                                            new FloatKeyframe(0f, list[eventType][indexValue], Ease.Linear),
                                            new FloatKeyframe(Parser.TryParse(modifier.commands[3], 1f), value, Ease.HasEaseFunction(easing) ? Ease.GetEaseFunction(easing) : Ease.Linear),
                                        }, x => RTEventManager.inst.SetOffset(eventType, indexValue, x), interpolateOnComplete: true)
                                    };
                                    animation.onComplete = () => AnimationManager.inst.Remove(animation.id);
                                    AnimationManager.inst.Play(animation);
                                }
                            }
                            break;
                        }
                    case "eventOffsetCopyAxis": {
                            if (!RTEventManager.inst || RTEventManager.inst.offsets == null)
                                break;

                            if (int.TryParse(modifier.commands[1], out int fromType) && int.TryParse(modifier.commands[2], out int fromAxis)
                                && int.TryParse(modifier.commands[3], out int toType) && int.TryParse(modifier.commands[4], out int toAxis)
                                && float.TryParse(modifier.commands[5], out float delay) && float.TryParse(modifier.commands[6], out float multiply)
                                && float.TryParse(modifier.commands[7], out float offset) && float.TryParse(modifier.commands[8], out float min) && float.TryParse(modifier.commands[9], out float max)
                                && float.TryParse(modifier.commands[10], out float loop) && bool.TryParse(modifier.commands[11], out bool useVisual))
                            {
                                var time = AudioManager.inst.CurrentAudioSource.time;

                                fromType = Mathf.Clamp(fromType, 0, modifier.reference.events.Count - 1);
                                fromAxis = Mathf.Clamp(fromAxis, 0, modifier.reference.events[fromType][0].eventValues.Length - 1);
                                toType = Mathf.Clamp(toType, 0, RTEventManager.inst.offsets.Count - 1);
                                toAxis = Mathf.Clamp(toAxis, 0, RTEventManager.inst.offsets[toType].Count - 1);

                                if (!useVisual && Updater.levelProcessor.converter.cachedSequences.TryGetValue(modifier.reference.id, out ObjectConverter.CachedSequences cachedSequences))
                                    RTEventManager.inst.SetOffset(toType, toAxis, fromType switch
                                    {
                                        0 => Mathf.Clamp((cachedSequences.Position3DSequence.Interpolate(time - modifier.reference.StartTime - delay).At(fromAxis) - offset) * multiply % loop, min, max),
                                        1 => Mathf.Clamp((cachedSequences.ScaleSequence.Interpolate(time - modifier.reference.StartTime - delay).At(fromAxis) - offset) * multiply % loop, min, max),
                                        2 => Mathf.Clamp((cachedSequences.RotationSequence.Interpolate(time - modifier.reference.StartTime - delay) - offset) * multiply % loop, min, max),
                                        _ => 0f,
                                    });
                                else if (Updater.TryGetObject(modifier.reference, out LevelObject levelObject) && levelObject.visualObject != null && levelObject.visualObject.GameObject)
                                    RTEventManager.inst.SetOffset(toType, toAxis, Mathf.Clamp((levelObject.visualObject.GameObject.transform.GetVector(fromType).At(fromAxis) - offset) * multiply % loop, min, max));
                            }
                            break;
                        }
                    case "vignetteTracksPlayer": {
                            if (PlayerManager.Players.Count < 1)
                                break;

                            var player = PlayerManager.Players[0].Player;

                            if (!player || !player.rb)
                                break;

                            var cameraToViewportPoint = Camera.main.WorldToViewportPoint(player.rb.position);
                            RTEventManager.inst.SetOffset(7, 4, cameraToViewportPoint.x);
                            RTEventManager.inst.SetOffset(7, 5, cameraToViewportPoint.y);

                            break;
                        }
                    case "lensTracksPlayer": {
                            if (PlayerManager.Players.Count < 1)
                                break;

                            var player = PlayerManager.Players[0].Player;

                            if (!player || !player.rb)
                                break;

                            var cameraToViewportPoint = Camera.main.WorldToViewportPoint(player.rb.position);
                            RTEventManager.inst.SetOffset(8, 1, cameraToViewportPoint.x - 0.5f);
                            RTEventManager.inst.SetOffset(8, 2, cameraToViewportPoint.y - 0.5f);

                            break;
                        }

                    #endregion
                    #region Color

                    case "addColor": {
                            if (modifier.reference != null && Updater.TryGetObject(modifier.reference, out LevelObject levelObject) && levelObject.visualObject.Renderer &&
                                int.TryParse(modifier.commands[1], out int index) && float.TryParse(modifier.value, out float multiply) &&
                                float.TryParse(modifier.commands[2], out float hue) && float.TryParse(modifier.commands[3], out float sat) && float.TryParse(modifier.commands[4], out float val))
                            {
                                var color = CoreHelper.ChangeColorHSV(GameManager.inst.LiveTheme.GetObjColor(index), hue, sat, val) * multiply;
                                //if (levelObject.isGradient)
                                //    levelObject.gradientObject.SetColor(levelObject.gradientObject.GetPrimaryColor() + color, levelObject.gradientObject.GetSecondaryColor() + color);
                                //else
                                    levelObject.visualObject.SetColor(levelObject.visualObject.GetPrimaryColor() + color);
                            }

                            break;
                        }
                    case "addColorOther": {
                            var list = !modifier.prefabInstanceOnly ? GameData.Current.FindObjectsWithTag(modifier.commands[1]) : GameData.Current.FindObjectsWithTag(modifier.reference, modifier.commands[1]);

                            if (list.Count > 0 &&
                                int.TryParse(modifier.commands[2], out int index) && float.TryParse(modifier.value, out float multiply) &&
                                float.TryParse(modifier.commands[3], out float hue) && float.TryParse(modifier.commands[4], out float sat) && float.TryParse(modifier.commands[5], out float val))
                                foreach (var bm in list)
                                {
                                    if (!Updater.TryGetObject(bm, out LevelObject levelObject) || !levelObject.visualObject.Renderer)
                                        continue;

                                    var color = CoreHelper.ChangeColorHSV(GameManager.inst.LiveTheme.GetObjColor(index), hue, sat, val) * multiply;
                                    //if (levelObject.isGradient)
                                    //    levelObject.gradientObject.SetColor(levelObject.gradientObject.GetPrimaryColor() + color, levelObject.gradientObject.GetSecondaryColor() + color);
                                    //else
                                        levelObject.visualObject.SetColor(levelObject.visualObject.GetPrimaryColor() + color);
                                }

                            break;
                        }
                    case "lerpColor": {
                            if (modifier.reference != null && Updater.TryGetObject(modifier.reference, out LevelObject levelObject) && levelObject.visualObject.Renderer &&
                                int.TryParse(modifier.commands[1], out int index) && float.TryParse(modifier.value, out float multiply) &&
                                float.TryParse(modifier.commands[2], out float hue) && float.TryParse(modifier.commands[3], out float sat) && float.TryParse(modifier.commands[4], out float val))
                            {
                                index = Mathf.Clamp(index, 0, GameManager.inst.LiveTheme.objectColors.Count - 1);

                                if (levelObject.visualObject != null && levelObject.visualObject.Renderer)
                                    levelObject.visualObject.Renderer.material.color =
                                        RTMath.Lerp(levelObject.visualObject.Renderer.material.color, CoreHelper.ChangeColorHSV(GameManager.inst.LiveTheme.objectColors[index], hue, sat, val), multiply);
                            }

                            break;
                        }
                    case "lerpColorOther": {
                            //if (sw != null)
                            //    CoreHelper.Log($"Time taken: {sw.Elapsed}");

                            var list = !modifier.prefabInstanceOnly ? GameData.Current.FindObjectsWithTag(modifier.commands[1]) : GameData.Current.FindObjectsWithTag(modifier.reference, modifier.commands[1]);

                            //if (sw != null)
                            //    CoreHelper.Log($"Time taken: {sw.Elapsed}");

                            if (list.Count > 0 &&
                                        int.TryParse(modifier.commands[2], out int index) && float.TryParse(modifier.value, out float multiply) &&
                                        float.TryParse(modifier.commands[3], out float hue) && float.TryParse(modifier.commands[4], out float sat) && float.TryParse(modifier.commands[5], out float val))
                            {
                                var color = CoreHelper.ChangeColorHSV(GameManager.inst.LiveTheme.GetObjColor(index), hue, sat, val);
                                for (int i = 0; i < list.Count; i++)
                                {
                                    var bm = list[i];
                                    if (!Updater.TryGetObject(bm, out LevelObject levelObject) || levelObject.visualObject == null)
                                        continue;

                                    var renderer = levelObject.visualObject.Renderer;
                                    if (!renderer)
                                        continue;

                                    var material = renderer.material;
                                    material.color = RTMath.Lerp(material.color, color, multiply);
                                }
                            }

                            break;
                        }
                    case "addColorPlayerDistance": {
                            if (modifier.reference != null && Updater.TryGetObject(modifier.reference, out LevelObject levelObject) &&
                                levelObject.visualObject.GameObject && levelObject.visualObject.Renderer &&
                                int.TryParse(modifier.commands[1], out int index) && float.TryParse(modifier.value, out float offset) && float.TryParse(modifier.commands[2], out float multiply))
                            {
                                var player = PlayerManager.GetClosestPlayer(levelObject.visualObject.GameObject.transform.position);

                                if (!player.Player || !player.Player.rb)
                                    break;

                                var distance = Vector2.Distance(player.Player.rb.transform.position, levelObject.visualObject.GameObject.transform.position);

                                index = Mathf.Clamp(index, 0, GameManager.inst.LiveTheme.objectColors.Count - 1);

                                levelObject.visualObject.Renderer.material.color += GameManager.inst.LiveTheme.objectColors[index] * -(distance * multiply - offset);
                            }

                            break;
                        }
                    case "lerpColorPlayerDistance": {
                            if (modifier.reference != null && Updater.TryGetObject(modifier.reference, out LevelObject levelObject) &&
                                levelObject.visualObject.GameObject && levelObject.visualObject.Renderer &&
                                int.TryParse(modifier.commands[1], out int index) && float.TryParse(modifier.value, out float offset) && float.TryParse(modifier.commands[2], out float multiply) &&
                                float.TryParse(modifier.commands[3], out float opacity) &&
                                float.TryParse(modifier.commands[4], out float hue) && float.TryParse(modifier.commands[5], out float sat) && float.TryParse(modifier.commands[6], out float val))
                            {
                                var player = PlayerManager.GetClosestPlayer(levelObject.visualObject.GameObject.transform.position);

                                if (!player.Player || !player.Player.rb)
                                    break;

                                var distance = Vector2.Distance(player.Player.rb.transform.position, levelObject.visualObject.GameObject.transform.position);

                                index = Mathf.Clamp(index, 0, GameManager.inst.LiveTheme.objectColors.Count - 1);

                                levelObject.visualObject.Renderer.material.color =
                                    Color.Lerp(levelObject.visualObject.Renderer.material.color,
                                                LSColors.fadeColor(CoreHelper.ChangeColorHSV(GameManager.inst.LiveTheme.objectColors[index], hue, sat, val), opacity),
                                                -(distance * multiply - offset));
                            }

                            break;
                        }
                    case "setAlpha":
                    case "setOpacity": {
                            if (modifier.reference != null && Updater.TryGetObject(modifier.reference, out LevelObject levelObject) && levelObject.visualObject.Renderer && float.TryParse(modifier.value, out float num))
                            {
                                if (levelObject.visualObject is not TextObject)
                                    levelObject.visualObject.Renderer.material.color = LSColors.fadeColor(levelObject.visualObject.Renderer.material.color, num);
                                else
                                    ((TextObject)levelObject.visualObject).textMeshPro.color = LSColors.fadeColor(levelObject.visualObject.Renderer.material.color, num);
                            }

                            break;
                        }
                    case "setAlphaOther":
                    case "setOpacityOther": {
                            var list = !modifier.prefabInstanceOnly ? GameData.Current.FindObjectsWithTag(modifier.commands[1]) : GameData.Current.FindObjectsWithTag(modifier.reference, modifier.commands[1]);

                            if (list.Count > 0 && float.TryParse(modifier.value, out float num))
                                foreach (var bm in list)
                                {
                                    if (!Updater.TryGetObject(bm, out LevelObject levelObject) || !levelObject.visualObject.Renderer)
                                        continue;

                                    if (levelObject.visualObject is TextObject textObject)
                                        textObject.textMeshPro.color = LSColors.fadeColor(levelObject.visualObject.Renderer.material.color, num);
                                    else
                                        levelObject.visualObject.Renderer.material.color = LSColors.fadeColor(levelObject.visualObject.Renderer.material.color, num);
                                }

                            break;
                        }
                    case "copyColor": {
                            if (!GameData.Current.TryFindObjectWithTag(modifier, modifier.value, out BeatmapObject beatmapObject))
                                break;

                            if (!Updater.TryGetObject(beatmapObject, out LevelObject otherLevelObject) ||
                                !otherLevelObject.visualObject.Renderer ||
                                !Updater.TryGetObject(modifier.reference, out LevelObject levelObject) ||
                                !levelObject.visualObject.Renderer)
                                break;

                            var isFlipped = modifier.reference.gradientType == BeatmapObject.GradientType.RightLinear || modifier.reference.gradientType == BeatmapObject.GradientType.OutInRadial;
                            var otherIsFlipped = beatmapObject.gradientType == BeatmapObject.GradientType.RightLinear || beatmapObject.gradientType == BeatmapObject.GradientType.OutInRadial;
                            var otherMaterial = otherLevelObject.visualObject.Renderer.material;
                            if (!levelObject.isGradient)
                                levelObject.visualObject.Renderer.material.color =
                                    otherLevelObject.isGradient ?
                                        Parser.TryParse(modifier.commands[1], true) ? (!otherIsFlipped ? otherMaterial.GetColor("_Color") : otherMaterial.GetColor("_ColorSecondary")) :
                                            (!otherIsFlipped ? otherMaterial.GetColor("_ColorSecondary") : otherMaterial.GetColor("_Color")) :
                                    otherMaterial.color;
                            else
                            {
                                if (otherLevelObject.isGradient)
                                {
                                    var startColor = isFlipped ? otherMaterial.GetColor("_ColorSecondary") : otherMaterial.GetColor("_Color");
                                    var endColor = isFlipped ? otherMaterial.GetColor("_Color") : otherMaterial.GetColor("_ColorSecondary");
                                    levelObject.gradientObject.SetColor(startColor, endColor);
                                }
                                else
                                {
                                    var material = otherLevelObject.visualObject.Renderer.material;

                                    var startColor = isFlipped ? material.GetColor("_ColorSecondary") : material.GetColor("_Color");
                                    var endColor = isFlipped ? material.GetColor("_Color") : material.GetColor("_ColorSecondary");
                                    levelObject.gradientObject.SetColor(
                                        Parser.TryParse(modifier.commands[1], true) ? otherMaterial.color : startColor,
                                        Parser.TryParse(modifier.commands[2], true) ? otherMaterial.color : startColor);
                                }
                            }

                            break;
                        }
                    case "copyColorOther": {
                            var list = !modifier.prefabInstanceOnly ? GameData.Current.FindObjectsWithTag(modifier.value) : GameData.Current.FindObjectsWithTag(modifier.reference, modifier.value);

                            if (list.Count > 0 && Updater.TryGetObject(modifier.reference, out LevelObject levelObject) && levelObject.visualObject.Renderer)
                                foreach (var bm in list)
                                {
                                    if (!Updater.TryGetObject(bm, out LevelObject otherLevelObject) || !otherLevelObject.visualObject.Renderer)
                                        continue;

                                    var material = levelObject.visualObject.Renderer.material;

                                    var isFlipped = modifier.reference.gradientType == BeatmapObject.GradientType.RightLinear || modifier.reference.gradientType == BeatmapObject.GradientType.OutInRadial;

                                    if (!otherLevelObject.isGradient)
                                        otherLevelObject.visualObject.Renderer.material.color =
                                            levelObject.isGradient ?
                                                Parser.TryParse(modifier.commands[1], true) ? (!isFlipped ? material.GetColor("_Color") : material.GetColor("_ColorSecondary")) :
                                                    (!isFlipped ? material.GetColor("_ColorSecondary") : material.GetColor("_Color")) :
                                                material.color;
                                    else
                                    {
                                        if (levelObject.isGradient)
                                        {
                                            var otherIsFlipped = bm.gradientType == BeatmapObject.GradientType.RightLinear || bm.gradientType == BeatmapObject.GradientType.OutInRadial;

                                            var startColor = otherIsFlipped ? material.GetColor("_ColorSecondary") : material.GetColor("_Color");
                                            var endColor = otherIsFlipped ? material.GetColor("_Color") : material.GetColor("_ColorSecondary");
                                            otherLevelObject.gradientObject.SetColor(startColor, endColor);
                                        }
                                        else
                                        {
                                            var otherIsFlipped = bm.gradientType == BeatmapObject.GradientType.RightLinear || bm.gradientType == BeatmapObject.GradientType.OutInRadial;
                                            var otherMaterial = otherLevelObject.visualObject.Renderer.material;

                                            var startColor = otherIsFlipped ? otherMaterial.GetColor("_ColorSecondary") : otherMaterial.GetColor("_Color");
                                            var endColor = otherIsFlipped ? otherMaterial.GetColor("_Color") : otherMaterial.GetColor("_ColorSecondary");
                                            otherLevelObject.gradientObject.SetColor(
                                                Parser.TryParse(modifier.commands[1], true) ? material.color : startColor,
                                                Parser.TryParse(modifier.commands[2], true) ? material.color : startColor);
                                        }
                                    }
                                }

                            break;
                        }
                    case "applyColorGroup": {
                            var list = !modifier.prefabInstanceOnly ? GameData.Current.FindObjectsWithTag(modifier.value) : GameData.Current.FindObjectsWithTag(modifier.reference, modifier.value);

                            if (list.Count > 0 && Updater.levelProcessor.converter.cachedSequences.TryGetValue(modifier.reference.id, out ObjectConverter.CachedSequences cachedSequence))
                            {
                                var beatmapObject = modifier.reference;
                                var time = Updater.CurrentTime - beatmapObject.StartTime;
                                Color color;
                                Color secondColor;
                                {
                                    var prevKFIndex = beatmapObject.events[3].FindLastIndex(x => x.eventTime < time);

                                    if (prevKFIndex < 0)
                                        return;

                                    var prevKF = beatmapObject.events[3][prevKFIndex];
                                    var nextKF = beatmapObject.events[3][Mathf.Clamp(prevKFIndex + 1, 0, beatmapObject.events[3].Count - 1)];
                                    var easing = Ease.GetEaseFunction(nextKF.curveType.Name)(RTMath.InverseLerp(prevKF.eventTime, nextKF.eventTime, time));
                                    int prevcolor = (int)prevKF.eventValues[0];
                                    int nextColor = (int)nextKF.eventValues[0];
                                    var lerp = RTMath.Lerp(0f, 1f, easing);
                                    if (float.IsNaN(lerp) || float.IsInfinity(lerp))
                                        lerp = 1f;

                                    color = Color.Lerp(
                                        CoreHelper.CurrentBeatmapTheme.GetObjColor(prevcolor),
                                        CoreHelper.CurrentBeatmapTheme.GetObjColor(nextColor),
                                        lerp);

                                    lerp = RTMath.Lerp(prevKF.eventValues[1], nextKF.eventValues[1], easing);
                                    if (float.IsNaN(lerp) || float.IsInfinity(lerp))
                                        lerp = 0f;

                                    color = LSColors.fadeColor(color, -(lerp - 1f));

                                    var lerpHue = RTMath.Lerp(prevKF.eventValues[2], nextKF.eventValues[2], easing);
                                    var lerpSat = RTMath.Lerp(prevKF.eventValues[3], nextKF.eventValues[3], easing);
                                    var lerpVal = RTMath.Lerp(prevKF.eventValues[4], nextKF.eventValues[4], easing);

                                    if (float.IsNaN(lerpHue))
                                        lerpHue = nextKF.eventValues[2];
                                    if (float.IsNaN(lerpSat))
                                        lerpSat = nextKF.eventValues[3];
                                    if (float.IsNaN(lerpVal))
                                        lerpVal = nextKF.eventValues[4];

                                    color = CoreHelper.ChangeColorHSV(color, lerpHue, lerpSat, lerpVal);

                                    prevcolor = (int)prevKF.eventValues[5];
                                    nextColor = (int)nextKF.eventValues[5];
                                    lerp = RTMath.Lerp(0f, 1f, easing);
                                    if (float.IsNaN(lerp) || float.IsInfinity(lerp))
                                        lerp = 1f;

                                    secondColor = Color.Lerp(
                                        CoreHelper.CurrentBeatmapTheme.GetObjColor(prevcolor),
                                        CoreHelper.CurrentBeatmapTheme.GetObjColor(nextColor),
                                        lerp);

                                    lerp = RTMath.Lerp(prevKF.eventValues[6], nextKF.eventValues[6], easing);
                                    if (float.IsNaN(lerp) || float.IsInfinity(lerp))
                                        lerp = 0f;

                                    secondColor = LSColors.fadeColor(secondColor, -(lerp - 1f));

                                    lerpHue = RTMath.Lerp(prevKF.eventValues[7], nextKF.eventValues[7], easing);
                                    lerpSat = RTMath.Lerp(prevKF.eventValues[8], nextKF.eventValues[8], easing);
                                    lerpVal = RTMath.Lerp(prevKF.eventValues[9], nextKF.eventValues[9], easing);

                                    if (float.IsNaN(lerpHue))
                                        lerpHue = nextKF.eventValues[7];
                                    if (float.IsNaN(lerpSat))
                                        lerpSat = nextKF.eventValues[8];
                                    if (float.IsNaN(lerpVal))
                                        lerpVal = nextKF.eventValues[9];

                                    secondColor = CoreHelper.ChangeColorHSV(color, lerpHue, lerpSat, lerpVal);
                                }
                                var type = Parser.TryParse(modifier.commands[1], 0);
                                var axis = Parser.TryParse(modifier.commands[2], 0);

                                var isEmpty = modifier.reference.objectType == BeatmapObject.ObjectType.Empty;

                                float t = !isEmpty ? type switch
                                {
                                    0 => axis == 0 ? cachedSequence.Position3DSequence.Value.x : axis == 1 ? cachedSequence.Position3DSequence.Value.y : cachedSequence.Position3DSequence.Value.z,
                                    1 => axis == 0 ? cachedSequence.ScaleSequence.Value.x : cachedSequence.ScaleSequence.Value.y,
                                    2 => cachedSequence.RotationSequence.Value,
                                    _ => 0f
                                } : type switch
                                {
                                    0 => axis == 0 ? cachedSequence.Position3DSequence.Interpolate(time).x : axis == 1 ? cachedSequence.Position3DSequence.Interpolate(time).y : cachedSequence.Position3DSequence.Interpolate(time).z,
                                    1 => axis == 0 ? cachedSequence.ScaleSequence.Interpolate(time).x : cachedSequence.ScaleSequence.Interpolate(time).y,
                                    2 => cachedSequence.RotationSequence.Interpolate(time),
                                    _ => 0f
                                };

                                foreach (var bm in list)
                                {
                                    if (!Updater.TryGetObject(bm, out LevelObject otherLevelObject) || !otherLevelObject.visualObject.Renderer)
                                        return;

                                    var material = otherLevelObject.visualObject.Renderer.material;

                                    if (!otherLevelObject.isGradient)
                                    {
                                        material.color = Color.Lerp(otherLevelObject.visualObject.Renderer.material.color, color, t);
                                    }
                                    else
                                    {
                                        var isFlipped = bm.gradientType == BeatmapObject.GradientType.RightLinear || bm.gradientType == BeatmapObject.GradientType.OutInRadial;

                                        var startColor = isFlipped ? material.GetColor("_ColorSecondary") : material.GetColor("_Color");
                                        var endColor = isFlipped ? material.GetColor("_Color") : material.GetColor("_ColorSecondary");
                                        otherLevelObject.gradientObject.SetColor(Color.Lerp(startColor, color, t), Color.Lerp(endColor, secondColor, t));
                                    }
                                }
                            }

                            break;
                        }
                    case "setColorHex": {
                            if (modifier.reference != null && Updater.TryGetObject(modifier.reference, out LevelObject levelObject) && levelObject.visualObject.Renderer)
                            {
                                if (!levelObject.isGradient)
                                {
                                    var color = levelObject.visualObject.Renderer.material.color;
                                    levelObject.visualObject.Renderer.material.color =
                                        string.IsNullOrEmpty(modifier.value) ? color : LSColors.fadeColor(LSColors.HexToColorAlpha(modifier.value), color.a);
                                }
                                else
                                {
                                    var startColor = levelObject.visualObject.Renderer.material.GetColor("_Color");
                                    var endColor = levelObject.visualObject.Renderer.material.GetColor("_ColorSecondary");
                                    levelObject.gradientObject.SetColor(
                                        string.IsNullOrEmpty(modifier.value) ? startColor : LSColors.fadeColor(LSColors.HexToColorAlpha(modifier.value), startColor.a),
                                        string.IsNullOrEmpty(modifier.commands[1]) ? endColor : LSColors.fadeColor(LSColors.HexToColorAlpha(modifier.commands[1]), endColor.a));
                                }
                            }

                            break;
                        }
                    case "setColorHexOther": {
                            var list = !modifier.prefabInstanceOnly ? GameData.Current.FindObjectsWithTag(modifier.commands[1]) : GameData.Current.FindObjectsWithTag(modifier.reference, modifier.commands[1]);

                            if (list.Count > 0)
                                foreach (var bm in list)
                                {
                                    if (!Updater.TryGetObject(bm, out LevelObject levelObject) || !levelObject.visualObject.Renderer)
                                        continue;

                                    if (!levelObject.isGradient)
                                    {
                                        var color = levelObject.visualObject.Renderer.material.color;
                                        levelObject.visualObject.Renderer.material.color =
                                            string.IsNullOrEmpty(modifier.value) ? color : LSColors.fadeColor(LSColors.HexToColorAlpha(modifier.value), color.a);
                                    }
                                    else
                                    {
                                        var startColor = levelObject.visualObject.Renderer.material.GetColor("_Color");
                                        var endColor = levelObject.visualObject.Renderer.material.GetColor("_ColorSecondary");
                                        levelObject.gradientObject.SetColor(
                                            string.IsNullOrEmpty(modifier.value) ? startColor : LSColors.fadeColor(LSColors.HexToColorAlpha(modifier.value), startColor.a),
                                            string.IsNullOrEmpty(modifier.commands[2]) ? endColor : LSColors.fadeColor(LSColors.HexToColorAlpha(modifier.commands[2]), endColor.a));
                                    }
                                }

                            break;
                        }

                    #endregion
                    #region Shape

                        // todo: figure out how to get this to work
                    case "actorFrameTexture": {
                            if (modifier.reference.shape != 6 || !Updater.TryGetObject(modifier.reference, out LevelObject levelObject) || levelObject.visualObject is not ImageObject imageObject)
                                break;

                            var camera = Parser.TryParse(modifier.value, 0) == 0 ? EventManager.inst.cam : EventManager.inst.camPer;

                            var frame = SpriteHelper.CaptureFrame(camera, Parser.TryParse(modifier.commands[1], 512), Parser.TryParse(modifier.commands[2], 512), Parser.TryParse(modifier.commands[3], 0f), Parser.TryParse(modifier.commands[4], 0f));

                            ((SpriteRenderer)imageObject.Renderer).sprite = frame;

                            break;
                        }
                    case "setImage": {
                            if (modifier.reference.shape == 6 && modifier.reference.levelObject && modifier.reference.levelObject.visualObject != null &&
                                modifier.reference.levelObject.visualObject is ImageObject imageObject)
                            {
                                if (modifier.constant)
                                    break;

                                var path = RTFile.CombinePaths(RTFile.BasePath, modifier.value);

                                if (!RTFile.FileExists(path))
                                {
                                    imageObject.SetDefaultSprite();
                                    break;
                                }

                                CoreHelper.StartCoroutine(AlephNetwork.DownloadImageTexture("file://" + path, imageObject.SetTexture, imageObject.SetDefaultSprite));
                            }
                            break;
                        }
                    case "setImageOther": {
                            var list = !modifier.prefabInstanceOnly ? GameData.Current.FindObjectsWithTag(modifier.commands[1]) : GameData.Current.FindObjectsWithTag(modifier.reference, modifier.commands[1]);

                            if (list.Count <= 0 || modifier.constant)
                                break;

                            foreach (var bm in list)
                            {
                                if (bm.shape == 6 && bm.levelObject && bm.levelObject.visualObject != null &&
                                    bm.levelObject.visualObject is ImageObject imageObject)
                                {
                                    var path = RTFile.CombinePaths(RTFile.BasePath, modifier.value);

                                    if (!RTFile.FileExists(path))
                                    {
                                        imageObject.SetDefaultSprite();
                                        break;
                                    }

                                    CoreHelper.StartCoroutine(AlephNetwork.DownloadImageTexture("file://" + path, imageObject.SetTexture, imageObject.SetDefaultSprite));
                                }
                            }

                            break;
                        }
                    case "setText": {
                            if (modifier.reference.shape == 4 && modifier.reference.levelObject && modifier.reference.levelObject.visualObject != null &&
                                modifier.reference.levelObject.visualObject is TextObject textObject)
                            {
                                if (modifier.constant)
                                    textObject.SetText(modifier.GetValue(0));
                                else
                                    textObject.text = modifier.GetValue(0);
                            }
                            break;
                        }
                    case "setTextOther": {
                            var list = !modifier.prefabInstanceOnly ? GameData.Current.FindObjectsWithTag(modifier.commands[1]) : GameData.Current.FindObjectsWithTag(modifier.reference, modifier.commands[1]);

                            if (list.Count <= 0)
                                break;

                            foreach (var bm in list)
                            {
                                if (bm.shape == 4 && bm.levelObject && bm.levelObject.visualObject != null &&
                                    bm.levelObject.visualObject is TextObject textObject)
                                {
                                    if (modifier.constant)
                                        textObject.SetText(modifier.GetValue(0));
                                    else
                                        textObject.text = modifier.GetValue(0);
                                }
                            }

                            break;
                        }
                    case "addText": {
                            if (modifier.reference.shape == 4 && modifier.reference.levelObject && modifier.reference.levelObject.visualObject != null &&
                                modifier.reference.levelObject.visualObject is TextObject textObject)
                            {
                                if (modifier.constant)
                                    textObject.SetText(textObject.textMeshPro.text + modifier.GetValue(0));
                                else
                                    textObject.text += modifier.GetValue(0);
                            }
                            break;
                        }
                    case "addTextOther": {
                            var list = !modifier.prefabInstanceOnly ? GameData.Current.FindObjectsWithTag(modifier.commands[1]) : GameData.Current.FindObjectsWithTag(modifier.reference, modifier.commands[1]);

                            if (list.Count <= 0)
                                break;

                            foreach (var bm in list)
                            {
                                if (bm.shape == 4 && bm.levelObject && bm.levelObject.visualObject != null &&
                                    bm.levelObject.visualObject is TextObject textObject)
                                    if (modifier.constant)
                                        textObject.SetText(textObject.textMeshPro.text + modifier.GetValue(0));
                                    else
                                        textObject.text += modifier.GetValue(0);
                            }

                            break;
                        }
                    case "removeText": {
                            if (modifier.reference.shape == 4 && modifier.reference.levelObject && modifier.reference.levelObject.visualObject != null &&
                                modifier.reference.levelObject.visualObject is TextObject textObject)
                            {
                                string text = string.IsNullOrEmpty(textObject.textMeshPro.text) ? "" :
                                    textObject.textMeshPro.text.Substring(0, textObject.textMeshPro.text.Length - Mathf.Clamp(modifier.GetInt(0, 1), 0, textObject.textMeshPro.text.Length - 1));

                                if (modifier.constant)
                                    textObject.SetText(text);
                                else
                                    textObject.text = text;
                            }
                            break;
                        }
                    case "removeTextOther": {
                            var list = !modifier.prefabInstanceOnly ? GameData.Current.FindObjectsWithTag(modifier.commands[1]) : GameData.Current.FindObjectsWithTag(modifier.reference, modifier.commands[1]);

                            if (list.Count <= 0)
                                break;

                            var remove = modifier.GetInt(0, 1);

                            foreach (var bm in list)
                            {
                                if (bm.shape == 4 && bm.levelObject && bm.levelObject.visualObject != null &&
                                    bm.levelObject.visualObject is TextObject textObject)
                                {
                                    string text = string.IsNullOrEmpty(textObject.textMeshPro.text) ? "" :
                                        textObject.textMeshPro.text.Substring(0, textObject.textMeshPro.text.Length - Mathf.Clamp(remove, 0, textObject.textMeshPro.text.Length - 1));

                                    if (modifier.constant)
                                        textObject.SetText(text);
                                    else
                                        textObject.text = text;
                                }
                            }

                            break;
                        }
                    case "removeTextAt": {
                            if (modifier.reference.shape == 4 && modifier.reference.levelObject && modifier.reference.levelObject.visualObject != null &&
                                modifier.reference.levelObject.visualObject is TextObject textObject)
                            {
                                var remove = modifier.GetInt(0, 1);
                                string text = string.IsNullOrEmpty(textObject.textMeshPro.text) ? "" : textObject.textMeshPro.text.Length > remove ?
                                    textObject.textMeshPro.text.Remove(remove, 1) : "";

                                if (modifier.constant)
                                    textObject.SetText(text);
                                else
                                    textObject.text = text;
                            }
                            break;
                        }
                    case "removeTextOtherAt": {
                            var list = !modifier.prefabInstanceOnly ? GameData.Current.FindObjectsWithTag(modifier.commands[1]) : GameData.Current.FindObjectsWithTag(modifier.reference, modifier.commands[1]);

                            if (list.Count <= 0)
                                break;

                            var remove = modifier.GetInt(0, 1);

                            foreach (var bm in list)
                            {
                                if (bm.shape == 4 && bm.levelObject && bm.levelObject.visualObject != null &&
                                    bm.levelObject.visualObject is TextObject textObject)
                                {
                                    string text = string.IsNullOrEmpty(textObject.textMeshPro.text) ? "" : textObject.textMeshPro.text.Length > remove ?
                                        textObject.textMeshPro.text.Remove(remove, 1) : "";

                                    if (modifier.constant)
                                        textObject.SetText(text);
                                    else
                                        textObject.text = text;
                                }
                            }

                            break;
                        }
                    case "formatText": {
                            if (!CoreConfig.Instance.AllowCustomTextFormatting.Value && modifier.reference.shape == 4 && modifier.reference.levelObject && modifier.reference.levelObject.visualObject != null &&
                                modifier.reference.levelObject.visualObject is TextObject textObject)
                                textObject.SetText(RTString.FormatText(modifier.reference, textObject.text));

                            break;
                        }
                    case "textSequence": {
                            if (modifier.reference.shape != 4 || !modifier.reference.levelObject || modifier.reference.levelObject.visualObject is not TextObject textObject)
                                break;

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
                            var length = Parser.TryParse(modifier.value, 1f);
                            var glitch = Parser.TryParse(modifier.commands[1], true);

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
                                    break;

                                // Don't play custom sound.
                                if (!modifier.GetBool(3, false))
                                {
                                    SoundManager.inst.PlaySound(DefaultSounds.Click, volume, volume);
                                    break;
                                }

                                if (SoundManager.inst.TryGetSound(modifier.GetValue(4), out AudioClip audioClip))
                                    SoundManager.inst.PlaySound(audioClip, volume, pitch);
                                else
                                    ModifiersManager.GetSoundPath(modifier.reference.id, modifier.GetValue(4), modifier.GetBool(5, false), pitch, volume, false);

                                break;
                            }

                            break;
                        }
                    case "backgroundShape": {
                            if (modifier.reference.shape == 4 || modifier.reference.shape == 6 || modifier.reference.shape == 9 || modifier.Result != null)
                                break;

                            if (Updater.TryGetObject(modifier.reference, out LevelObject levelObject) && levelObject.visualObject != null && levelObject.visualObject.GameObject)
                            {
                                var shape = new Vector2Int(modifier.reference.shape, modifier.reference.shapeOption);
                                if (ShapeManager.inst.StoredShapes3D.TryGetValue(shape, out Shape value))
                                {
                                    levelObject.visualObject.GameObject.GetComponent<MeshFilter>().mesh = value.mesh;
                                    modifier.Result = "frick";
                                    levelObject.visualObject.GameObject.AddComponent<DestroyModifierResult>().Modifier = modifier;
                                }
                            }

                            break;
                        }
                    case "sphereShape": {
                            if (modifier.reference.shape == 4 || modifier.reference.shape == 6 || modifier.reference.shape == 9 || modifier.Result != null)
                                break;

                            if (Updater.TryGetObject(modifier.reference, out LevelObject levelObject) && levelObject.visualObject != null && levelObject.visualObject.GameObject)
                            {
                                levelObject.visualObject.GameObject.GetComponent<MeshFilter>().mesh = GameManager.inst.PlayerPrefabs[1].GetComponentInChildren<MeshFilter>().mesh;
                                modifier.Result = "frick";
                                levelObject.visualObject.GameObject.AddComponent<DestroyModifierResult>().Modifier = modifier;
                            }

                            break;
                        }
                    case "translateShape": {
                            if (!Updater.TryGetObject(modifier.reference, out LevelObject levelObject) || !levelObject.visualObject.GameObject)
                                return;

                            if (modifier.Result == null)
                            {
                                var meshFilter = levelObject.visualObject.GameObject.GetComponent<MeshFilter>();
                                var mesh = meshFilter.mesh;

                                modifier.Result = new KeyValuePair<MeshFilter, Vector3[]>(meshFilter, mesh.vertices);

                                levelObject.visualObject.GameObject.AddComponent<DestroyModifierResult>().Modifier = modifier;
                            }

                            if (modifier.Result is KeyValuePair<MeshFilter, Vector3[]> keyValuePair &&
                                float.TryParse(modifier.commands[1], out float posX) && float.TryParse(modifier.commands[2], out float posY) &&
                                float.TryParse(modifier.commands[3], out float scaleX) && float.TryParse(modifier.commands[4], out float scaleY)
                                && float.TryParse(modifier.commands[5], out float rot))
                            {
                                keyValuePair.Key.mesh.vertices =
                                    keyValuePair.Value.Select(x => RTMath.Move(RTMath.Rotate(RTMath.Scale(x, new Vector2(scaleX, scaleY)), rot), new Vector2(posX, posY))).ToArray();
                            }

                            break;
                        }

                    #endregion
                    #region Animation

                    case "animateObject": {
                            if (int.TryParse(modifier.commands[1], out int type)
                                && float.TryParse(modifier.commands[2], out float x) && float.TryParse(modifier.commands[3], out float y) && float.TryParse(modifier.commands[4], out float z)
                                && bool.TryParse(modifier.commands[5], out bool relative) && float.TryParse(modifier.value, out float time))
                            {
                                string easing = modifier.commands[6];
                                if (int.TryParse(modifier.commands[6], out int e) && e >= 0 && e < DataManager.inst.AnimationList.Count)
                                    easing = DataManager.inst.AnimationList[e].Name;

                                Vector3 vector = modifier.reference.GetTransformOffset(type);

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
                                        }, vector3 => modifier.reference.SetTransform(type, vector3), interpolateOnComplete: true),
                                    };
                                    animation.onComplete = () => AnimationManager.inst.Remove(animation.id);
                                    AnimationManager.inst.Play(animation);
                                    break;
                                }

                                modifier.reference.SetTransform(type, setVector);
                            }

                            break;
                        }
                    case "animateObjectOther": {
                            var list = !modifier.prefabInstanceOnly ? GameData.Current.FindObjectsWithTag(modifier.commands[7]) : GameData.Current.FindObjectsWithTag(modifier.reference, modifier.commands[7]);

                            if (list.Count > 0 && int.TryParse(modifier.commands[1], out int type)
                                && float.TryParse(modifier.commands[2], out float x) && float.TryParse(modifier.commands[3], out float y) && float.TryParse(modifier.commands[4], out float z)
                                && bool.TryParse(modifier.commands[5], out bool relative) && float.TryParse(modifier.value, out float time))
                            {
                                string easing = modifier.commands[6];
                                if (int.TryParse(modifier.commands[6], out int e) && e >= 0 && e < DataManager.inst.AnimationList.Count)
                                    easing = DataManager.inst.AnimationList[e].Name;

                                foreach (var bm in list)
                                {
                                    Vector3 vector = bm.GetTransformOffset(type);

                                    var setVector = new Vector3(x, y, z) + (relative ? vector : Vector3.zero);

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
                            }

                            break;
                        }
                    case "animateSignal": {
                            if (int.TryParse(modifier.commands[1], out int type)
                                && float.TryParse(modifier.commands[2], out float x) && float.TryParse(modifier.commands[3], out float y) && float.TryParse(modifier.commands[4], out float z)
                                && bool.TryParse(modifier.commands[5], out bool relative) && float.TryParse(modifier.value, out float time))
                            {
                                if (!Parser.TryParse(modifier.commands[9], true))
                                {
                                    var list = !modifier.prefabInstanceOnly ? GameData.Current.FindObjectsWithTag(modifier.commands[7]) : GameData.Current.FindObjectsWithTag(modifier.reference, modifier.commands[7]);

                                    foreach (var bm in list)
                                    {
                                        if (bm.modifiers.Count > 0 && bm.modifiers.Where(x => x.commands[0] == "requireSignal" && x.type == ModifierBase.Type.Trigger).Count() > 0 &&
                                            bm.modifiers.TryFind(x => x.commands[0] == "requireSignal" && x.type == ModifierBase.Type.Trigger, out Modifier<BeatmapObject> m))
                                            m.Result = null;
                                    }
                                }

                                string easing = modifier.commands[6];
                                if (int.TryParse(modifier.commands[6], out int e) && e >= 0 && e < DataManager.inst.AnimationList.Count)
                                    easing = DataManager.inst.AnimationList[e].Name;

                                Vector3 vector = modifier.reference.GetTransformOffset(type);

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
                                        }, vector3 => modifier.reference.SetTransform(type, vector3), interpolateOnComplete: true),
                                    };
                                    animation.onComplete = () =>
                                    {
                                        AnimationManager.inst.Remove(animation.id);

                                        var list = !modifier.prefabInstanceOnly ? GameData.Current.FindObjectsWithTag(modifier.commands[7]) : GameData.Current.FindObjectsWithTag(modifier.reference, modifier.commands[7]);

                                        foreach (var bm in list)
                                            CoreHelper.StartCoroutine(ModifiersManager.ActivateModifier(bm, Parser.TryParse(modifier.commands[8], 0f)));
                                    };
                                    AnimationManager.inst.Play(animation);
                                    break;
                                }

                                modifier.reference.SetTransform(type, setVector);
                            }

                            break;
                        }
                    case "animateSignalOther": {
                            var list = !modifier.prefabInstanceOnly ? GameData.Current.FindObjectsWithTag(modifier.commands[7]) : GameData.Current.FindObjectsWithTag(modifier.reference, modifier.commands[7]);

                            if (list.Count > 0 && int.TryParse(modifier.commands[1], out int type)
                                && float.TryParse(modifier.commands[2], out float x) && float.TryParse(modifier.commands[3], out float y) && float.TryParse(modifier.commands[4], out float z)
                                && bool.TryParse(modifier.commands[5], out bool relative) && float.TryParse(modifier.value, out float time))
                            {
                                if (!Parser.TryParse(modifier.commands[10], true))
                                {
                                    var list2 = !modifier.prefabInstanceOnly ? GameData.Current.FindObjectsWithTag(modifier.commands[8]) : GameData.Current.FindObjectsWithTag(modifier.reference, modifier.commands[8]);

                                    foreach (var bm in list2)
                                    {
                                        if (bm.modifiers.Count > 0 && bm.modifiers.FindAll(x => x.commands[0] == "requireSignal" && x.type == ModifierBase.Type.Trigger).Count > 0 &&
                                            bm.modifiers.TryFind(x => x.commands[0] == "requireSignal" && x.type == ModifierBase.Type.Trigger, out Modifier<BeatmapObject> m))
                                        {
                                            m.Result = null;
                                        }
                                    }
                                }

                                string easing = modifier.commands[6];
                                if (int.TryParse(modifier.commands[6], out int e) && e >= 0 && e < DataManager.inst.AnimationList.Count)
                                    easing = DataManager.inst.AnimationList[e].Name;

                                foreach (var bm in list)
                                {
                                    Vector3 vector = bm.GetTransformOffset(type);

                                    var setVector = new Vector3(x, y, z) + (relative ? vector : Vector3.zero);

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

                                            var list = !modifier.prefabInstanceOnly ? GameData.Current.FindObjectsWithTag(modifier.commands[8]) : GameData.Current.FindObjectsWithTag(modifier.reference, modifier.commands[8]);

                                            foreach (var bm in list)
                                                CoreHelper.StartCoroutine(ModifiersManager.ActivateModifier(bm, Parser.TryParse(modifier.commands[9], 0f)));
                                        };
                                        AnimationManager.inst.Play(animation);
                                        break;
                                    }

                                    bm.SetTransform(type, setVector);
                                }
                            }

                            break;
                        }

                    case "animateObjectMath": {
                            if (!int.TryParse(modifier.commands[1], out int type) || !bool.TryParse(modifier.commands[5], out bool relative))
                                break;

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
                                    }, vector3 => modifier.reference.SetTransform(type, vector3), interpolateOnComplete: true),
                                };
                                animation.onComplete = () => AnimationManager.inst.Remove(animation.id);
                                AnimationManager.inst.Play(animation);
                                break;
                            }

                            modifier.reference.SetTransform(type, setVector);

                            break;
                        }
                    case "animateObjectMathOther": {
                            var list = !modifier.prefabInstanceOnly ? GameData.Current.FindObjectsWithTag(modifier.commands[7]) : GameData.Current.FindObjectsWithTag(modifier.reference, modifier.commands[7]);

                            if (list.Count < 1 || !int.TryParse(modifier.commands[1], out int type) || !bool.TryParse(modifier.commands[5], out bool relative))
                                break;

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

                                var setVector = new Vector3(x, y, z) + (relative ? vector : Vector3.zero);

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

                            break;
                        }
                    case "animateSignalMath": {
                            if (!int.TryParse(modifier.commands[1], out int type) || !bool.TryParse(modifier.commands[5], out bool relative))
                                break;

                            if (!Parser.TryParse(modifier.commands[9], true))
                            {
                                var list = !modifier.prefabInstanceOnly ? GameData.Current.FindObjectsWithTag(modifier.commands[7]) : GameData.Current.FindObjectsWithTag(modifier.reference, modifier.commands[7]);

                                foreach (var bm in list)
                                {
                                    if (bm.modifiers.Count > 0 && bm.modifiers.Where(x => x.commands[0] == "requireSignal" && x.type == ModifierBase.Type.Trigger).Count() > 0 &&
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
                                    }, vector3 => modifier.reference.SetTransform(type, vector3), interpolateOnComplete: true),
                                };
                                animation.onComplete = () =>
                                {
                                    AnimationManager.inst.Remove(animation.id);

                                    var list = !modifier.prefabInstanceOnly ? GameData.Current.FindObjectsWithTag(modifier.commands[7]) : GameData.Current.FindObjectsWithTag(modifier.reference, modifier.commands[7]);

                                    foreach (var bm in list)
                                        CoreHelper.StartCoroutine(ModifiersManager.ActivateModifier(bm, signalTime));
                                };
                                AnimationManager.inst.Play(animation);
                                break;
                            }

                            modifier.reference.SetTransform(type, setVector);

                            break;
                        }
                    case "animateSignalMathOther": {
                            var list = !modifier.prefabInstanceOnly ? GameData.Current.FindObjectsWithTag(modifier.commands[7]) : GameData.Current.FindObjectsWithTag(modifier.reference, modifier.commands[7]);

                            if (list.Count < 1 || !int.TryParse(modifier.commands[1], out int type) || !bool.TryParse(modifier.commands[5], out bool relative))
                                break;

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

                                var setVector = new Vector3(x, y, z) + (relative ? vector : Vector3.zero);

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
                                            CoreHelper.StartCoroutine(ModifiersManager.ActivateModifier(bm, signalTime));
                                    };
                                    AnimationManager.inst.Play(animation);
                                    break;
                                }

                                modifier.reference.SetTransform(type, setVector);
                            }

                            break;
                        }

                    case "gravity": {
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
                                modifier.Result = RTMath.Lerp(Vector2.zero, new Vector2(gravityX, gravityY), RTMath.Recursive(Time.time - modifier.ResultTimer, curve) * time);

                            var vector = (Vector2)modifier.Result;

                            var rotation = modifier.reference.InterpolateChainRotation(includeSelf: false);

                            modifier.reference.positionOffset = RTMath.Rotate(vector, -rotation);

                            break;
                        }
                    case "gravityOther": {
                            var beatmapObjects = !modifier.prefabInstanceOnly ? GameData.Current.FindObjectsWithTag(modifier.value) : GameData.Current.FindObjectsWithTag(modifier.reference, modifier.value);

                            if (beatmapObjects.Count <= 0)
                                break;

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
                                modifier.Result = RTMath.Lerp(Vector2.zero, new Vector2(gravityX, gravityY), RTMath.Recursive(Time.time - modifier.ResultTimer, curve) * time);

                            var vector = (Vector2)modifier.Result;

                            foreach (var beatmapObject in beatmapObjects)
                            {
                                var rotation = beatmapObject.InterpolateChainRotation(includeSelf: false);

                                beatmapObject.positionOffset = RTMath.Rotate(vector, -rotation);
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
                                && float.TryParse(modifier.commands[10], out float loop) && bool.TryParse(modifier.commands[11], out bool useVisual)
                                && GameData.Current.TryFindObjectWithTag(modifier, modifier.value, out BeatmapObject bm))
                            {
                                var time = Updater.CurrentTime;

                                fromType = Mathf.Clamp(fromType, 0, bm.events.Count);
                                fromAxis = Mathf.Clamp(fromAxis, 0, bm.events[fromType][0].eventValues.Length);

                                if (toType < 0 || toType > 3)
                                    break;

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
                                        break;
                                    }
                                    modifier.reference.SetTransform(toType, toAxis, fromType switch
                                    {
                                        0 => Mathf.Clamp((cachedSequence.Position3DSequence.Interpolate(time - bm.StartTime - delay).At(fromAxis) - offset) * multiply % loop, min, max),
                                        1 => Mathf.Clamp((cachedSequence.ScaleSequence.Interpolate(time - bm.StartTime - delay).At(fromAxis) - offset) * multiply % loop, min, max),
                                        2 => Mathf.Clamp((cachedSequence.RotationSequence.Interpolate(time - bm.StartTime - delay) - offset) * multiply % loop, min, max),
                                        _ => 0f,
                                    });
                                }
                                else if (useVisual && Updater.TryGetObject(bm, out LevelObject levelObject) && levelObject.visualObject != null && levelObject.visualObject.GameObject)
                                    modifier.reference.SetTransform(toType, toAxis, Mathf.Clamp((levelObject.visualObject.GameObject.transform.GetVector(fromType).At(fromAxis) - offset) * multiply % loop, min, max));
                                else if (useVisual)
                                    modifier.reference.SetTransform(toType, toAxis, Mathf.Clamp(fromType switch
                                    {
                                        0 => bm.InterpolateChainPosition().At(fromAxis),
                                        1 => bm.InterpolateChainScale().At(fromAxis),
                                        2 => bm.InterpolateChainRotation(),
                                        _ => 0f,
                                    }, min, max));
                            }

                            break;
                        }
                    case "copyAxisMath": {
                            /*
                            From Type: (Pos / Sca / Rot)
                            From Axis: (X / Y / Z)
                            Object Group
                            To Type: (Pos / Sca / Rot)
                            To Axis: (X / Y / Z)
                            */

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
                                    fromAxis = Mathf.Clamp(fromAxis, 0, bm.events[fromType][0].eventValues.Length);

                                    if (toType < 0 || toType > 3)
                                        break;

                                    if (!useVisual && Updater.levelProcessor.converter.cachedSequences.TryGetValue(bm.id, out ObjectConverter.CachedSequences cachedSequence))
                                    {
                                        if (fromType == 3)
                                        {
                                            if (toType == 3 && toAxis == 0 && cachedSequence.ColorSequence != null &&
                                                modifier.reference.levelObject && modifier.reference.levelObject.visualObject != null &&
                                                modifier.reference.levelObject.visualObject.Renderer)
                                            {
                                                var sequence = cachedSequence.ColorSequence.Interpolate(time - bm.StartTime - delay);

                                                var renderer = modifier.reference.levelObject.visualObject.Renderer;

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
                                                0 => cachedSequence.Position3DSequence.Interpolate(time - bm.StartTime - delay).At(fromAxis),
                                                1 => cachedSequence.ScaleSequence.Interpolate(time - bm.StartTime - delay).At(fromAxis),
                                                2 => cachedSequence.RotationSequence.Interpolate(time - bm.StartTime - delay),
                                                _ => 0f,
                                            };
                                            bm.SetOtherObjectVariables(variables);

                                            float value = RTMath.Parse(modifier.GetValue(8), variables);

                                            modifier.reference.SetTransform(toType, toAxis, Mathf.Clamp(value, min, max));
                                        }
                                    }
                                    else if (useVisual && Updater.TryGetObject(bm, out LevelObject levelObject) && levelObject.visualObject != null && levelObject.visualObject.GameObject)
                                    {
                                        var axis = levelObject.visualObject.GameObject.transform.GetVector(fromType).At(fromAxis);

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

                            break;
                        }
                    case "copyAxisGroup": {
                            var evaluation = modifier.value;

                            var toType = Parser.TryParse(modifier.commands[1], 0);
                            var toAxis = Parser.TryParse(modifier.commands[2], 0);

                            if (toType < 0 || toType > 4)
                                break;

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

                                    var beatmapObject = !modifier.prefabInstanceOnly ? GameData.Current.FindObjectWithTag(group) : GameData.Current.FindObjectWithTag(beatmapObjects, prefabObjects, modifier.reference, group);

                                    if (!beatmapObject)
                                        continue;

                                    cachedSequences.TryGetValue(beatmapObject.id, out ObjectConverter.CachedSequences cachedSequence);

                                    if (!useVisual && cachedSequence != null)
                                        variables[name] = fromType switch
                                        {
                                            0 => Mathf.Clamp(cachedSequence.Position3DSequence.Interpolate(time - beatmapObject.StartTime - delay).At(fromAxis), min, max),
                                            1 => Mathf.Clamp(cachedSequence.ScaleSequence.Interpolate(time - beatmapObject.StartTime - delay).At(fromAxis), min, max),
                                            2 => Mathf.Clamp(cachedSequence.RotationSequence.Interpolate(time - beatmapObject.StartTime - delay), min, max),
                                            _ => 0f,
                                        };
                                    else if (useVisual && Updater.TryGetObject(beatmapObject, out LevelObject levelObject) && levelObject.visualObject != null && levelObject.visualObject.GameObject)
                                        variables[name] = Mathf.Clamp(levelObject.visualObject.GameObject.transform.GetVector(fromType).At(fromAxis), min, max);
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

                            break;
                        }
                    case "copyPlayerAxis": {
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
                                && InputDataManager.inst.players.TryFind(x => x is CustomPlayer customPlayer && customPlayer.Player && customPlayer.Player.rb, out InputDataManager.CustomPlayer p))
                                modifier.reference.SetTransform(toType, toAxis, Mathf.Clamp((((CustomPlayer)p).Player.rb.transform.GetLocalVector(fromType).At(fromAxis) - offset) * multiply, min, max));

                            break;
                        }
                    case "legacyTail": {
                            if (modifier.reference &&
                                modifier.commands.Count > 1 && GameData.IsValid)
                            {
                                var totalTime = Parser.TryParse(modifier.value, 200f);

                                var list = modifier.Result is List<LegacyTracker> ? (List<LegacyTracker>)modifier.Result : new List<LegacyTracker>();

                                if (modifier.Result == null)
                                {
                                    list.Add(new LegacyTracker(modifier.reference, Vector3.zero, Vector3.zero, Quaternion.identity, 0f, 0f));

                                    for (int i = 1; i < modifier.commands.Count; i += 3)
                                    {
                                        var group = !modifier.prefabInstanceOnly ? GameData.Current.FindObjectsWithTag(modifier.commands[i]) : GameData.Current.FindObjectsWithTag(modifier.reference, modifier.commands[i]);

                                        if (modifier.commands.Count <= i + 2 || group.Count() < 1)
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
                            }

                            break;
                        }

                    case "applyAnimation": {
                            if (GameData.Current.TryFindObjectWithTag(modifier, modifier.value, out BeatmapObject from))
                            {
                                var list = !modifier.prefabInstanceOnly ? GameData.Current.FindObjectsWithTag(modifier.commands[10]) : GameData.Current.FindObjectsWithTag(modifier.reference, modifier.commands[10]);

                                if (modifier.Result == null)
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
                                        return;
                                    }

                                    ApplyAnimationTo(bm, from, useVisual, time, Updater.CurrentTime, animatePos, animateSca, animateRot, delayPos, delaySca, delayRot);
                                }
                            }

                            break;
                        }
                    case "applyAnimationFrom": {
                            if (GameData.Current.TryFindObjectWithTag(modifier, modifier.value, out BeatmapObject bm))
                            {
                                if (modifier.Result == null)
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

                            break;
                        }
                    case "applyAnimationTo": {
                            var list = !modifier.prefabInstanceOnly ? GameData.Current.FindObjectsWithTag(modifier.value) : GameData.Current.FindObjectsWithTag(modifier.reference, modifier.value);

                            if (modifier.Result == null)
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
                                    return;
                                }

                                ApplyAnimationTo(bm, modifier.reference, useVisual, time, Updater.CurrentTime, animatePos, animateSca, animateRot, delayPos, delaySca, delayRot);
                            }

                            break;
                        }

                    case "applyAnimationMath": {
                            if (GameData.Current.TryFindObjectWithTag(modifier, modifier.value, out BeatmapObject from))
                            {
                                var list = !modifier.prefabInstanceOnly ? GameData.Current.FindObjectsWithTag(modifier.commands[10]) : GameData.Current.FindObjectsWithTag(modifier.reference, modifier.commands[10]);

                                if (modifier.Result == null)
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

                            break;
                        }
                    case "applyAnimationFromMath": {
                            if (GameData.Current.TryFindObjectWithTag(modifier, modifier.value, out BeatmapObject bm))
                            {
                                if (modifier.Result == null)
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

                            break;
                        }
                    case "applyAnimationToMath": {
                            var list = !modifier.prefabInstanceOnly ? GameData.Current.FindObjectsWithTag(modifier.value) : GameData.Current.FindObjectsWithTag(modifier.reference, modifier.value);

                            if (modifier.Result == null)
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
                                    return;
                                }

                                ApplyAnimationTo(bm, modifier.reference, useVisual, time, timeOffset, animatePos, animateSca, animateRot, delayPos, delaySca, delayRot);
                            }

                            break;
                        }

                    #endregion
                    #region BG Object

                    case "setBGActive": {
                            if (!bool.TryParse(modifier.value, out bool active))
                                break;

                            var list = GameData.Current.backgroundObjects.FindAll(x => x.tags.Contains(modifier.commands[1]));
                            if (list.Count > 0)
                                for (int i = 0; i < list.Count; i++)
                                    list[i].Enabled = active;

                            break;
                        }
                    case "animateBGObject": {
                            break;
                        }

                    #endregion
                    #region Prefab

                        // todo: change from index to ID.
                    case "spawnPrefab": {
                            if (!modifier.constant && modifier.Result == null && int.TryParse(modifier.value, out int num) && GameData.Current.prefabs.Count > num
                                && float.TryParse(modifier.commands[1], out float posX) && float.TryParse(modifier.commands[2], out float posY)
                                && float.TryParse(modifier.commands[3], out float scaX) && float.TryParse(modifier.commands[4], out float scaY) && float.TryParse(modifier.commands[5], out float rot)
                                && int.TryParse(modifier.commands[6], out int repeatCount) && float.TryParse(modifier.commands[7], out float repeatOffsetTime) && float.TryParse(modifier.commands[8], out float speed))
                            {
                                var prefabObject = ModifiersManager.AddPrefabObjectToLevel(GameData.Current.prefabs[num],
                                    modifier.GetBool(11, true) ? AudioManager.inst.CurrentAudioSource.time + modifier.GetFloat(10, 0f) : modifier.GetFloat(10, 0f),
                                    new Vector2(posX, posY),
                                    new Vector2(scaX, scaY),
                                    rot, repeatCount, repeatOffsetTime, speed);

                                modifier.Result = prefabObject;
                                GameData.Current.prefabObjects.Add(prefabObject);
                                Updater.AddPrefabToLevel(prefabObject);
                            }

                            break;
                        }
                    case "spawnPrefabOffset": {
                            if (!modifier.constant && modifier.Result == null && int.TryParse(modifier.value, out int num) && GameData.Current.prefabs.Count > num
                                && float.TryParse(modifier.commands[1], out float posX) && float.TryParse(modifier.commands[2], out float posY)
                                && float.TryParse(modifier.commands[3], out float scaX) && float.TryParse(modifier.commands[4], out float scaY) && float.TryParse(modifier.commands[5], out float rot)
                                && int.TryParse(modifier.commands[6], out int repeatCount) && float.TryParse(modifier.commands[7], out float repeatOffsetTime) && float.TryParse(modifier.commands[8], out float speed))
                            {
                                var animationResult = modifier.reference.InterpolateChain();

                                var prefabObject = ModifiersManager.AddPrefabObjectToLevel(GameData.Current.prefabs[num],
                                    modifier.GetBool(11, true) ? AudioManager.inst.CurrentAudioSource.time + modifier.GetFloat(10, 0f) : modifier.GetFloat(10, 0f),
                                    new Vector2(posX, posY) + (Vector2)animationResult.position,
                                    new Vector2(scaX, scaY) * animationResult.scale,
                                    rot + animationResult.rotation, repeatCount, repeatOffsetTime, speed);

                                modifier.Result = prefabObject;
                                GameData.Current.prefabObjects.Add(prefabObject);
                                Updater.AddPrefabToLevel(prefabObject);
                            }

                            break;
                        }
                    case "spawnPrefabOffsetOther": {
                            if (!modifier.constant && modifier.Result == null && int.TryParse(modifier.value, out int num) && GameData.Current.prefabs.Count > num
                                && float.TryParse(modifier.commands[1], out float posX) && float.TryParse(modifier.commands[2], out float posY)
                                && float.TryParse(modifier.commands[3], out float scaX) && float.TryParse(modifier.commands[4], out float scaY) && float.TryParse(modifier.commands[5], out float rot)
                                && int.TryParse(modifier.commands[6], out int repeatCount) && float.TryParse(modifier.commands[7], out float repeatOffsetTime) && float.TryParse(modifier.commands[8], out float speed)
                                && GameData.Current.TryFindObjectWithTag(modifier, modifier.commands[10], out BeatmapObject beatmapObject))
                            {
                                var animationResult = beatmapObject.InterpolateChain();

                                var prefabObject = ModifiersManager.AddPrefabObjectToLevel(GameData.Current.prefabs[num],
                                    modifier.GetBool(12, true) ? AudioManager.inst.CurrentAudioSource.time + modifier.GetFloat(11, 0f) : modifier.GetFloat(11, 0f),
                                    new Vector2(posX, posY) + (Vector2)animationResult.position,
                                    new Vector2(scaX, scaY) * animationResult.scale,
                                    rot + animationResult.rotation, repeatCount, repeatOffsetTime, speed);

                                modifier.Result = prefabObject;
                                GameData.Current.prefabObjects.Add(prefabObject);
                                Updater.AddPrefabToLevel(prefabObject);
                            }

                            break;
                        }
                    case "spawnMultiPrefab": {
                            if (!modifier.constant && int.TryParse(modifier.value, out int num) && GameData.Current.prefabs.Count > num
                                && float.TryParse(modifier.commands[1], out float posX) && float.TryParse(modifier.commands[2], out float posY)
                                && float.TryParse(modifier.commands[3], out float scaX) && float.TryParse(modifier.commands[4], out float scaY) && float.TryParse(modifier.commands[5], out float rot)
                                && int.TryParse(modifier.commands[6], out int repeatCount) && float.TryParse(modifier.commands[7], out float repeatOffsetTime) && float.TryParse(modifier.commands[8], out float speed))
                            {
                                if (modifier.Result == null)
                                    modifier.Result = new List<PrefabObject>();

                                var list = modifier.GetResult<List<PrefabObject>>();
                                var prefabObject = ModifiersManager.AddPrefabObjectToLevel(GameData.Current.prefabs[num],
                                    modifier.GetBool(10, true) ? AudioManager.inst.CurrentAudioSource.time + modifier.GetFloat(9, 0f) : modifier.GetFloat(9, 0f),
                                    new Vector2(posX, posY),
                                    new Vector2(scaX, scaY),
                                    rot, repeatCount, repeatOffsetTime, speed);

                                list.Add(prefabObject);
                                modifier.Result = list;

                                GameData.Current.prefabObjects.Add(prefabObject);
                                Updater.AddPrefabToLevel(prefabObject);
                            }

                            break;
                        }
                    case "spawnMultiPrefabOffset": {
                            if (!modifier.constant && int.TryParse(modifier.value, out int num) && GameData.Current.prefabs.Count > num
                                && float.TryParse(modifier.commands[1], out float posX) && float.TryParse(modifier.commands[2], out float posY)
                                && float.TryParse(modifier.commands[3], out float scaX) && float.TryParse(modifier.commands[4], out float scaY) && float.TryParse(modifier.commands[5], out float rot)
                                && int.TryParse(modifier.commands[6], out int repeatCount) && float.TryParse(modifier.commands[7], out float repeatOffsetTime) && float.TryParse(modifier.commands[8], out float speed))
                            {
                                var animationResult = modifier.reference.InterpolateChain();

                                if (modifier.Result == null)
                                    modifier.Result = new List<PrefabObject>();

                                var list = modifier.GetResult<List<PrefabObject>>();
                                var prefabObject = ModifiersManager.AddPrefabObjectToLevel(GameData.Current.prefabs[num],
                                    modifier.GetBool(10, true) ? AudioManager.inst.CurrentAudioSource.time + modifier.GetFloat(9, 0f) : modifier.GetFloat(9, 0f),
                                    new Vector2(posX, posY) + (Vector2)animationResult.position,
                                    new Vector2(scaX, scaY) * animationResult.scale,
                                    rot + animationResult.rotation, repeatCount, repeatOffsetTime, speed);

                                list.Add(prefabObject);
                                modifier.Result = list;

                                GameData.Current.prefabObjects.Add(prefabObject);
                                Updater.AddPrefabToLevel(prefabObject);
                            }

                            break;
                        }
                    case "spawnMultiPrefabOffsetOther": {
                            if (!modifier.constant && int.TryParse(modifier.value, out int num) && GameData.Current.prefabs.Count > num
                                && float.TryParse(modifier.commands[1], out float posX) && float.TryParse(modifier.commands[2], out float posY)
                                && float.TryParse(modifier.commands[3], out float scaX) && float.TryParse(modifier.commands[4], out float scaY) && float.TryParse(modifier.commands[5], out float rot)
                                && int.TryParse(modifier.commands[6], out int repeatCount) && float.TryParse(modifier.commands[7], out float repeatOffsetTime) && float.TryParse(modifier.commands[8], out float speed)
                                && GameData.Current.TryFindObjectWithTag(modifier, modifier.commands[9], out BeatmapObject beatmapObject))
                            {
                                var animationResult = beatmapObject.InterpolateChain();

                                if (modifier.Result == null)
                                    modifier.Result = new List<PrefabObject>();

                                var list = modifier.GetResult<List<PrefabObject>>();
                                var prefabObject = ModifiersManager.AddPrefabObjectToLevel(GameData.Current.prefabs[num],
                                    modifier.GetBool(11, true) ? AudioManager.inst.CurrentAudioSource.time + modifier.GetFloat(10, 0f) : modifier.GetFloat(10, 0f),
                                    new Vector2(posX, posY) + (Vector2)animationResult.position,
                                    new Vector2(scaX, scaY) * animationResult.scale,
                                    rot + animationResult.rotation, repeatCount, repeatOffsetTime, speed);

                                list.Add(prefabObject);
                                modifier.Result = list;

                                GameData.Current.prefabObjects.Add(prefabObject);
                                Updater.AddPrefabToLevel(prefabObject);
                            }

                            break;
                        }
                    case "clearSpawnedPrefabs": {
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

                                        GameData.Current.prefabObjects.RemoveAll(x => x.fromModifier && x.ID == prefabObjectResult.ID);

                                        otherModifier.Result = null;
                                        continue;
                                    }

                                    if (!otherModifier.TryGetResult(out List<PrefabObject> result))
                                        continue;

                                    for (int k = 0; k < result.Count; k++)
                                    {
                                        var prefabObject = result[k];

                                        Updater.UpdatePrefab(prefabObject, false);
                                        GameData.Current.prefabObjects.RemoveAll(x => x.fromModifier && x.ID == prefabObject.ID);
                                    }

                                    result.Clear();
                                    otherModifier.Result = null;
                                }
                            }

                            break;
                        }

                    #endregion
                    #region Ranking

                    case "saveLevelRank": {
                            if (CoreHelper.InEditor || modifier.constant || !LevelManager.CurrentLevel)
                                break;

                            LevelManager.UpdateCurrentLevelProgress();

                            break;
                        }
                    case "clearHits": {
                            if (!CoreHelper.InEditor) // hit and death counters are not supported in the editor yet.
                                GameManager.inst.hits.Clear();
                            break;
                        }
                    case "addHit": {
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
                            break;
                        }
                    case "subHit": {
                            if (!CoreHelper.InEditor && GameManager.inst.hits.Count > 0)
                                GameManager.inst.hits.RemoveAt(GameManager.inst.hits.Count - 1);
                            break;
                        }
                    case "clearDeaths": {
                            if (!CoreHelper.InEditor)
                                GameManager.inst.deaths.Clear();
                            break;
                        }
                    case "addDeath": {
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
                            break;
                        }
                    case "subDeath": {
                            if (!CoreHelper.InEditor && GameManager.inst.deaths.Count > 0)
                                GameManager.inst.deaths.RemoveAt(GameManager.inst.deaths.Count - 1);
                            break;
                        }

                    #endregion
                    #region Misc

                    case "quitToMenu": {
                            if (CoreHelper.InEditor && !EditorManager.inst.isEditing && ModifiersConfig.Instance.EditorLoadLevel.Value)
                            {
                                string str = RTFile.BasePath;
                                if (ModifiersConfig.Instance.EditorSavesBeforeLoad.Value)
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

                            break;
                        }
                    case "quitToArcade": {
                            if (CoreHelper.InEditor && !EditorManager.inst.isEditing && ModifiersConfig.Instance.EditorLoadLevel.Value)
                            {
                                string str = RTFile.BasePath;
                                if (ModifiersConfig.Instance.EditorSavesBeforeLoad.Value)
                                {
                                    GameData.Current.SaveData(RTFile.CombinePaths(str, $"level-modifier-backup{FileFormat.LSB.Dot()}"), () =>
                                    {
                                        EditorManager.inst.DisplayNotification($"Saved backup to {System.IO.Path.GetFileName(RTFile.RemoveEndSlash(str))}", 2f, EditorManager.NotificationType.Success);
                                    });
                                }

                                GameManager.inst.QuitToArcade();

                                break;
                            }

                            if (!CoreHelper.InEditor)
                                ArcadeHelper.QuitToArcade();

                            break;
                        }
                    case "blackHole": {
                            if (!modifier.reference)
                                break;

                            float p = Time.deltaTime * 60f * CoreHelper.ForwardPitch;

                            float num = Parser.TryParse(modifier.value, 0.01f);

                            if (Parser.TryParse(modifier.commands[1], false))
                                num = -(modifier.reference.Interpolate(3, 1) - 1f) * num;

                            if (num == 0f)
                                break;

                            float moveDelay = 1f - Mathf.Pow(1f - Mathf.Clamp(num, 0.001f, 1f), p);
                            var players = PlayerManager.Players;

                            if (!Updater.TryGetObject(modifier.reference, out LevelObject levelObject) || !levelObject.visualObject.GameObject)
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

                                break;
                            }

                            var gm = levelObject.visualObject.GameObject;

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

                            break;
                        }
                    case "setCollision": {
                            if (Updater.TryGetObject(modifier.reference, out LevelObject levelObject) && levelObject.visualObject != null && levelObject.visualObject.Collider)
                                levelObject.visualObject.ColliderEnabled = Parser.TryParse(modifier.value, false);

                            break;
                        }
                    case "setCollisionOther": {
                            var list = !modifier.prefabInstanceOnly ? GameData.Current.FindObjectsWithTag(modifier.commands[1]) : GameData.Current.FindObjectsWithTag(modifier.reference, modifier.commands[1]);

                            foreach (var beatmapObject in list)
                            {
                                if (Updater.TryGetObject(beatmapObject, out LevelObject levelObject) && levelObject.visualObject != null && levelObject.visualObject.Collider)
                                    levelObject.visualObject.ColliderEnabled = Parser.TryParse(modifier.value, false);
                            }

                            break;
                        }
                    case "updateObjects": {
                            if (!modifier.constant)
                                CoreHelper.StartCoroutine(Updater.IUpdateObjects(true));

                            break;
                        }
                    case "updateObject": {
                            var list = !modifier.prefabInstanceOnly ? GameData.Current.FindObjectsWithTag(modifier.value) : GameData.Current.FindObjectsWithTag(modifier.reference, modifier.value);

                            if (!modifier.constant && list.Count > 0)
                            {
                                foreach (var bm in list)
                                    Updater.UpdateObject(bm, recalculate: false);
                                Updater.RecalculateObjectStates();
                            }

                            break;
                        }
                    case "signalModifier": {
                            var list = !modifier.prefabInstanceOnly ? GameData.Current.FindObjectsWithTag(modifier.commands[1]) : GameData.Current.FindObjectsWithTag(modifier.reference, modifier.commands[1]);

                            foreach (var bm in list)
                                CoreHelper.StartCoroutine(ModifiersManager.ActivateModifier(bm, Parser.TryParse(modifier.value, 0f)));

                            break;
                        }
                    case "activateModifier": {
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

                            break;
                        }
                    case "editorNotify": {
                            EditorManager.inst?.DisplayNotification(modifier.value, Parser.TryParse(modifier.commands[1], 0.5f), (EditorManager.NotificationType)Parser.TryParse(modifier.commands[2], 0));
                            break;
                        }
                    case "setWindowTitle": {
                            WindowController.SetTitle(modifier.value);

                            break;
                        }
                    case "setDiscordStatus": {
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

                            break;
                        }
                    case "unlockAchievement": {

                            break;
                        } // todo: implement
                    case "videoPlayer": {
                            if (Updater.TryGetObject(modifier.reference, out LevelObject levelObject) && levelObject.visualObject is SolidObject solidObject && solidObject.GameObject)
                            {
                                var filePath = RTFile.CombinePaths(RTFile.BasePath, modifier.value);
                                if (!RTFile.FileExists(filePath))
                                    break;

                                if (modifier.Result == null || modifier.Result is RTVideoPlayer nullVideo && !nullVideo)
                                {
                                    var gameObject = levelObject.visualObject.GameObject;
                                    var videoPlayer = gameObject.GetComponent<RTVideoPlayer>() ?? gameObject.AddComponent<RTVideoPlayer>();

                                    solidObject.Renderer.material = GameObject.Find("ExtraBG").transform.GetChild(0).GetComponent<Renderer>().material;

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

                            break;
                        } // todo: dunno if this will ever be implemented as one video player might be enough
                    case "loadInterface": {
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

                            break;
                        }
                    case "pauseLevel": {
                            if (CoreHelper.InEditor)
                            {
                                EditorManager.inst.DisplayNotification("Cannot pause in the editor. This modifier only works in the Arcade.", 3f, EditorManager.NotificationType.Warning);
                                break;
                            }

                            PauseMenu.Pause();
                            break;
                        }

                    #endregion

                    #region Dev Only

                    // dev only (story mode)
                    case "loadSceneDEVONLY": {
                            if (!CoreHelper.InStory)
                                return;

                            SceneManager.inst.LoadScene(modifier.value, modifier.commands.Count > 1 && Parser.TryParse(modifier.commands[1], true));

                            break;
                        }
                    case "loadStoryLevelDEVONLY": {
                            if (!CoreHelper.InStory)
                                return;

                            Story.StoryManager.inst.Play(modifier.GetInt(1, 0), modifier.GetInt(2, 0), modifier.GetBool(0, false), modifier.GetBool(3, false));

                            break;
                        }
                    case "storySaveIntVariableDEVONLY": {
                            if (!CoreHelper.InStory)
                                return;

                            Story.StoryManager.inst.SaveInt(modifier.GetValue(0), modifier.reference.integerVariable);

                            break;
                        }
                    case "storySaveIntDEVONLY": {
                            if (!CoreHelper.InStory)
                                return;

                            Story.StoryManager.inst.SaveInt(modifier.GetValue(0), modifier.GetInt(1, 0));

                            break;
                        }
                    case "storySaveBoolDEVONLY": {
                            if (!CoreHelper.InStory)
                                return;

                            Story.StoryManager.inst.SaveBool(modifier.GetString(0, "null"), modifier.GetBool(1, false));

                            break;
                        }
                    case "enableExampleDEVONLY": {
                            if (Example.ExampleManager.inst)
                                Example.ExampleManager.inst.SetActive(modifier.GetBool(0, false));

                            break;
                        }

                        #endregion
                }

                //if (sw != null)
                //{
                //    CoreHelper.StopAndLogStopwatch(sw, $"Ran modifier: {modifier.commands[0]}\nObject ID: {modifier.reference.id}\nObject Name: {modifier.reference.name}\nTotal Time Taken: {timeTaken + sw.Elapsed.TotalSeconds}");
                //    timeTaken += (float)sw.Elapsed.TotalSeconds;
                //    sw = null;
                //}
            }
            catch (Exception ex)
            {
                CoreHelper.LogError($"Modifier ({modifier.commands[0]}) had an error. {ex}");
            }
        }

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

            if (modifier.commands.Count < 0)
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
                                levelObject.visualObject.Renderer && levelObject.visualObject is SolidObject &&
                                modifier.commands.Count > 2 && bool.TryParse(modifier.commands[2], out bool setNormal) && setNormal)
                            {
                                modifier.Result = null;

                                levelObject.visualObject.Renderer.material = ObjectManager.inst.norm;

                                ((SolidObject)levelObject.visualObject).material = levelObject.visualObject.Renderer.material;
                            }

                            break;
                        }
                    case "blurVariable": {
                            if (modifier.Result != null && modifier.reference &&
                                modifier.reference.objectType != BeatmapObject.ObjectType.Empty &&
                                Updater.TryGetObject(modifier.reference, out LevelObject levelObject) &&
                                levelObject.visualObject.Renderer && levelObject.visualObject is SolidObject &&
                                modifier.commands.Count > 1 && bool.TryParse(modifier.commands[1], out bool setNormal) && setNormal)
                            {
                                modifier.Result = null;

                                levelObject.visualObject.Renderer.material = ObjectManager.inst.norm;

                                ((SolidObject)levelObject.visualObject).material = levelObject.visualObject.Renderer.material;
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

                                GameData.Current.prefabObjects.RemoveAll(x => x.fromModifier && x.ID == prefabObject.ID);

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

                            if (list.Count > 0 && (modifier.commands.Count == 1 || Parser.TryParse(modifier.commands[1], true)))
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
                                return;

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
                                return;

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

                            if (list.Count > 0 && (modifier.commands.Count == 1 || Parser.TryParse(modifier.commands[1], true)))
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
                                return;

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
                                return;

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

                            if (list.Count > 0 && list.Any(x => x.modifiers.Any(y => y.Result != null)))
                                foreach (var bm in list)
                                {
                                    if (bm.modifiers.Count > 0 && bm.modifiers.Where(x => x.commands[0] == "requireSignal" && x.type == ModifierBase.Type.Trigger).Count() > 0 &&
                                        bm.modifiers.TryFind(x => x.commands[0] == "requireSignal" && x.type == ModifierBase.Type.Trigger, out Modifier<BeatmapObject> m))
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

                            if (list.Count > 0 && !modifier.constant)
                                foreach (var bm in list)
                                {
                                    if (bm.modifiers.Count > 0 && bm.modifiers.Where(x => x.commands[0] == "requireSignal" && x.type == ModifierBase.Type.Trigger).Count() > 0 &&
                                        bm.modifiers.TryFind(x => x.commands[0] == "requireSignal" && x.type == ModifierBase.Type.Trigger, out Modifier<BeatmapObject> m))
                                    {
                                        m.Result = null;
                                    }
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

                            if (modifier.constant && list.Count > 0)
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

            if (modifier.commands.Count < 0)
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

            if (modifier.commands.Count < 0)
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
                        if (list.Count > 0)
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

                            var setVector = new Vector3(x, y, z) + (relative ? vector : Vector3.zero);

                            if (!modifier.constant)
                            {
                                var animation = new RTAnimation("Animate BG Object Offset");

                                animation.animationHandlers = new List<AnimationHandlerBase>
                                {
                                    new AnimationHandler<Vector3>(new List<IKeyframe<Vector3>>
                                    {
                                        new Vector3Keyframe(0f, vector, Ease.Linear),
                                        new Vector3Keyframe(Mathf.Clamp(time, 0f, 9999f), setVector, Ease.HasEaseFunction(easing) ? Ease.GetEaseFunction(easing) : Ease.Linear),
                                    }, vector3 => { modifier.reference.SetTransform(type, vector3); }),
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

                                var setVector = new Vector3(x, y, z) + (relative ? vector : Vector3.zero);

                                if (!modifier.constant)
                                {
                                    var animation = new RTAnimation("Animate BG Object Offset");

                                    animation.animationHandlers = new List<AnimationHandlerBase>
                                    {
                                        new AnimationHandler<Vector3>(new List<IKeyframe<Vector3>>
                                        {
                                            new Vector3Keyframe(0f, vector, Ease.Linear),
                                            new Vector3Keyframe(Mathf.Clamp(time, 0f, 9999f), setVector, Ease.HasEaseFunction(easing) ? Ease.GetEaseFunction(easing) : Ease.Linear),
                                        }, vector3 => { bg.SetTransform(type, vector3); }),
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
                            fromAxis = Mathf.Clamp(fromAxis, 0, bm.events[fromType][0].eventValues.Length);

                            if (Updater.levelProcessor.converter.cachedSequences.TryGetValue(bm.id, out ObjectConverter.CachedSequences cachedSequence))
                            {
                                switch (fromType)
                                {
                                    case 0:
                                        {
                                            var sequence = cachedSequence.Position3DSequence.Interpolate(time - bm.StartTime - delay);
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

            if (modifier.commands.Count < 0)
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
            
            if (modifier.commands.Count < 0 || !modifier.reference)
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
                case "controlPress": {
                        var type = modifier.GetInt(0, 0);
                        var device = modifier.reference.device;

                        return Enum.TryParse(((PlayerInputControlType)type).ToString(), out InControl.InputControlType inputControlType) && device.GetControl(inputControlType).IsPressed;
                    }
                case "controlPressDown": {
                        var type = modifier.GetInt(0, 0);
                        var device = modifier.reference.device;

                        return Enum.TryParse(((PlayerInputControlType)type).ToString(), out InControl.InputControlType inputControlType) && device.GetControl(inputControlType).WasPressed;
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

            if (modifier.commands.Count < 0 || !modifier.reference)
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
                case "signalModifier": {
                        var list = GameData.Current.FindObjectsWithTag(modifier.commands[1]);

                        foreach (var bm in list)
                            CoreHelper.StartCoroutine(ModifiersManager.ActivateModifier(bm, Parser.TryParse(modifier.value, 0f)));

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

            if (modifier.commands.Count < 0 || modifier.reference == null)
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

        public static void ApplyAnimationTo(
            BeatmapObject applyTo, BeatmapObject takeFrom,
            bool useVisual, float time, float currentTime,
            bool animatePos, bool animateSca, bool animateRot,
            float delayPos, float delaySca, float delayRot)
        {
            if (!useVisual && Updater.levelProcessor.converter.cachedSequences.TryGetValue(takeFrom.id, out ObjectConverter.CachedSequences cachedSequences))
            {
                // Animate position
                if (animatePos)
                    applyTo.positionOffset = cachedSequences.Position3DSequence.Interpolate(currentTime - time - delayPos);

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
            else if (useVisual && Updater.TryGetObject(takeFrom, out LevelObject levelObject) && levelObject.visualObject != null && levelObject.visualObject.GameObject)
            {
                var transform = levelObject.visualObject.GameObject.transform;

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
            var time = Updater.CurrentTime;

            if (!visual && Updater.levelProcessor.converter.cachedSequences.TryGetValue(bm.id, out ObjectConverter.CachedSequences cachedSequence))
                return fromType switch
                {
                    0 => Mathf.Clamp((cachedSequence.Position3DSequence.Interpolate(time - bm.StartTime - delay).At(fromAxis) - offset) * multiply % loop, min, max),
                    1 => Mathf.Clamp((cachedSequence.ScaleSequence.Interpolate(time - bm.StartTime - delay).At(fromAxis) - offset) * multiply % loop, min, max),
                    2 => Mathf.Clamp((cachedSequence.RotationSequence.Interpolate(time - bm.StartTime - delay) - offset) * multiply % loop, min, max),
                    _ => 0f,
                };
            else if (visual && Updater.TryGetObject(bm, out LevelObject levelObject) && levelObject.visualObject != null && levelObject.visualObject.GameObject)
                return Mathf.Clamp((levelObject.visualObject.GameObject.transform.GetVector(fromType).At(fromAxis) - offset) * multiply % loop, min, max);

            return 0f;
        }
    }
}
