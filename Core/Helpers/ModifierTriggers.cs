using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

using SimpleJSON;

using BetterLegacy.Configs;
using BetterLegacy.Core.Components;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Data.Level;
using BetterLegacy.Core.Data.Player;
using BetterLegacy.Core.Managers;
using BetterLegacy.Core.Runtime;

// ignore naming styles since modifiers are named like this.
#pragma warning disable IDE1006 // Naming Styles
namespace BetterLegacy.Core.Helpers
{
    /// <summary>
    /// Library of modifier triggers.
    /// </summary>
    public static class ModifierTriggers
    {
        public static bool breakModifier(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables) => true;
        public static bool disableModifier(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables) => false;

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

            if (reference is not ITransformable transformable)
                return false;

            var player = PlayerManager.GetClosestPlayer(transformable.GetFullPosition());
            var device = player?.device ?? InControl.InputManager.ActiveDevice;

            if (device == null)
                return false;

            return Enum.TryParse(((PlayerInputControlType)type).ToString(), out InControl.InputControlType inputControlType) && device.GetControl(inputControlType).WasPressed;
        }

        public static bool controlPress(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            var type = modifier.GetInt(0, 0, variables);

            if (reference is not ITransformable transformable)
                return false;

            var player = PlayerManager.GetClosestPlayer(transformable.GetFullPosition());
            var device = player?.device ?? InControl.InputManager.ActiveDevice;

            if (device == null)
                return false;

            return Enum.TryParse(((PlayerInputControlType)type).ToString(), out InControl.InputControlType inputControlType) && device.GetControl(inputControlType).IsPressed;
        }

        public static bool controlPressUp(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            var type = modifier.GetInt(0, 0, variables);

            if (reference is not ITransformable transformable)
                return false;

            var player = PlayerManager.GetClosestPlayer(transformable.GetFullPosition());
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
            return AudioManager.inst.CurrentAudioSource.time - (modifier.GetBool(1, false, variables) && reference is ILifetime<AutoKillType> lifetime ? lifetime.StartTime : 0f) > modifier.GetFloat(0, 0f, variables);
        }
        
        public static bool musicTimeLesser(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            return AudioManager.inst.CurrentAudioSource.time - (modifier.GetBool(1, false, variables) && reference is ILifetime<AutoKillType> lifetime ? lifetime.StartTime : 0f) < modifier.GetFloat(0, 0f, variables);
        }

        public static bool musicTimeInRange(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            var time = reference.GetParentRuntime().FixedTime;
            return modifier.commands.Count > 2 && time >= modifier.GetFloat(1, 0f, variables) - 0.01f && time <= modifier.GetFloat(2, 0f, variables) + 0.1f;
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
                modifier.Result = UnityEngine.Random.Range(modifier.GetInt(1, 0, variables), modifier.GetInt(2, 0, variables)) == modifier.GetInt(0, 0, variables);

            return modifier.HasResult() && modifier.GetResult<bool>();
        }
        
        public static bool randomLesser(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (!modifier.HasResult())
                modifier.Result = UnityEngine.Random.Range(modifier.GetInt(1, 0, variables), modifier.GetInt(2, 0, variables)) < modifier.GetInt(0, 0, variables);

            return modifier.HasResult() && modifier.GetResult<bool>();
        }
        
        public static bool randomGreater(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (!modifier.HasResult())
                modifier.Result = UnityEngine.Random.Range(modifier.GetInt(1, 0, variables), modifier.GetInt(2, 0, variables)) > modifier.GetInt(0, 0, variables);

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
            if (reference is not IPrefabable prefabable)
                return false;

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

            if (!GameData.Current.TryFindObjectWithTag(modifier, prefabable, modifier.GetValue(0, variables), out BeatmapObject bm))
                return false;

            fromType = Mathf.Clamp(fromType, 0, bm.events.Count);
            fromAxis = Mathf.Clamp(fromAxis, 0, bm.events[fromType][0].values.Length);


            return fromType >= 0 && fromType <= 2 && ModifiersHelper.GetAnimation(prefabable, bm, fromType, fromAxis, min, max, offset, multiply, delay, loop, visual) == equals;
        }
        
        public static bool axisLesserEquals(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not IPrefabable prefabable)
                return false;

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

            if (!GameData.Current.TryFindObjectWithTag(modifier, prefabable, modifier.GetValue(0, variables), out BeatmapObject bm))
                return false;

            fromType = Mathf.Clamp(fromType, 0, bm.events.Count);
            fromAxis = Mathf.Clamp(fromAxis, 0, bm.events[fromType][0].values.Length);


            return fromType >= 0 && fromType <= 2 && ModifiersHelper.GetAnimation(prefabable, bm, fromType, fromAxis, min, max, offset, multiply, delay, loop, visual) <= equals;
        }
        
        public static bool axisGreaterEquals(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not IPrefabable prefabable)
                return false;

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

            if (!GameData.Current.TryFindObjectWithTag(modifier, prefabable, modifier.GetValue(0, variables), out BeatmapObject bm))
                return false;

            fromType = Mathf.Clamp(fromType, 0, bm.events.Count);
            fromAxis = Mathf.Clamp(fromAxis, 0, bm.events[fromType][0].values.Length);


            return fromType >= 0 && fromType <= 2 && ModifiersHelper.GetAnimation(prefabable, bm, fromType, fromAxis, min, max, offset, multiply, delay, loop, visual) >= equals;
        }
        
        public static bool axisLesser(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not IPrefabable prefabable)
                return false;

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

            if (!GameData.Current.TryFindObjectWithTag(modifier, prefabable, modifier.GetValue(0, variables), out BeatmapObject bm))
                return false;

            fromType = Mathf.Clamp(fromType, 0, bm.events.Count);
            fromAxis = Mathf.Clamp(fromAxis, 0, bm.events[fromType][0].values.Length);


            return fromType >= 0 && fromType <= 2 && ModifiersHelper.GetAnimation(prefabable, bm, fromType, fromAxis, min, max, offset, multiply, delay, loop, visual) < equals;
        }
        
        public static bool axisGreater(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not IPrefabable prefabable)
                return false;

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

            if (!GameData.Current.TryFindObjectWithTag(modifier, prefabable, modifier.GetValue(0, variables), out BeatmapObject bm))
                return false;

            fromType = Mathf.Clamp(fromType, 0, bm.events.Count);
            fromAxis = Mathf.Clamp(fromAxis, 0, bm.events[fromType][0].values.Length);


            return fromType >= 0 && fromType <= 2 && ModifiersHelper.GetAnimation(prefabable, bm, fromType, fromAxis, min, max, offset, multiply, delay, loop, visual) > equals;
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

        #endregion

        #region Real Time
        
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

            var realTime = modifier.GetBool(1, true, variables);
            float time;
            if (realTime)
            {
                var hasResult = modifier.HasResult();
                var timer = hasResult ? modifier.GetResult<RTTimer>() : new RTTimer();
                if (!hasResult)
                    timer.Reset();
                timer.Update();
                time = timer.time;
            }
            else
                time = reference.GetParentRuntime().FixedTime;

            return time > modifier.GetFloat(0, 0f, variables);
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
        
        public static bool isFullscreen(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            return Screen.fullScreen;
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

        #endregion

        #region Dev Only

        #endregion

        public static class PlayerTriggers
        {
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

            public static bool controlPressDown(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
            {
                if (reference is not PAPlayer player)
                    return false;

                var type = modifier.GetInt(0, 0, variables);
                var device = player.device;

                return Enum.TryParse(((PlayerInputControlType)type).ToString(), out InControl.InputControlType inputControlType) && device.GetControl(inputControlType).WasPressed;
            }

            public static bool controlPress(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
            {
                if (reference is not PAPlayer player)
                    return false;

                var type = modifier.GetInt(0, 0, variables);
                var device = player.device;

                return Enum.TryParse(((PlayerInputControlType)type).ToString(), out InControl.InputControlType inputControlType) && device.GetControl(inputControlType).IsPressed;
            }

            public static bool controlPressUp(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
            {
                if (reference is not PAPlayer player)
                    return false;

                var type = modifier.GetInt(0, 0, variables);
                var device = player.device;

                return Enum.TryParse(((PlayerInputControlType)type).ToString(), out InControl.InputControlType inputControlType) && device.GetControl(inputControlType).WasReleased;
            }

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

            public static bool healthPerEquals(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
            {
                if (reference is not PAPlayer player)
                    return false;

                var health = ((float)player.Health / player.GetControl().Health) * 100f;

                return health == modifier.GetFloat(0, 50f, variables);
            }

            public static bool healthPerGreaterEquals(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
            {
                if (reference is not PAPlayer player)
                    return false;

                var health = ((float)player.Health / player.GetControl().Health) * 100f;

                return health >= modifier.GetFloat(0, 50f, variables);
            }

            public static bool healthPerLesserEquals(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
            {
                if (reference is not PAPlayer player)
                    return false;

                var health = ((float)player.Health / player.GetControl().Health) * 100f;

                return health <= modifier.GetFloat(0, 50f, variables);
            }

            public static bool healthPerGreater(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
            {
                if (reference is not PAPlayer player)
                    return false;

                var health = ((float)player.Health / player.GetControl().Health) * 100f;

                return health > modifier.GetFloat(0, 50f, variables);
            }

            public static bool healthPerLesser(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
            {
                if (reference is not PAPlayer player)
                    return false;

                var health = ((float)player.Health / player.GetControl().Health) * 100f;

                return health < modifier.GetFloat(0, 50f, variables);
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
        }
    }
}

#pragma warning restore IDE1006 // Naming Styles