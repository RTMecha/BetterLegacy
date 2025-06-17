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
        #region Player

        public static bool playerCollide(Modifier<BeatmapObject> modifier, Dictionary<string, string> variables)
        {
            var runtimeObject = modifier.reference.runtimeObject;
            if (runtimeObject && runtimeObject.visualObject && runtimeObject.visualObject.collider)
            {
                var collider = runtimeObject.visualObject.collider;

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
        
        public static bool playerHealthEquals<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            var health = modifier.GetInt(0, 0, variables);
            return !InputDataManager.inst.players.IsEmpty() && InputDataManager.inst.players.Any(x => x.health == health);
        }
        
        public static bool playerHealthLesserEquals<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            var health = modifier.GetInt(0, 0, variables);
            return !InputDataManager.inst.players.IsEmpty() && InputDataManager.inst.players.Any(x => x.health <= health);
        }
        
        public static bool playerHealthGreaterEquals<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            var health = modifier.GetInt(0, 0, variables);
            return !InputDataManager.inst.players.IsEmpty() && InputDataManager.inst.players.Any(x => x.health >= health);
        }
        
        public static bool playerHealthLesser<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            var health = modifier.GetInt(0, 0, variables);
            return !InputDataManager.inst.players.IsEmpty() && InputDataManager.inst.players.Any(x => x.health < health);
        }
        
        public static bool playerHealthGreater<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            var health = modifier.GetInt(0, 0, variables);
            return !InputDataManager.inst.players.IsEmpty() && InputDataManager.inst.players.Any(x => x.health > health);
        }
        
        public static bool playerMoving<T>(Modifier<T> modifier, Dictionary<string, string> variables)
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
        
        public static bool playerBoosting(Modifier<BeatmapObject> modifier, Dictionary<string, string> variables)
        {
            var levelObject = modifier.reference.runtimeObject;
            if (levelObject && levelObject.visualObject && levelObject.visualObject.gameObject)
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
        }
        
        public static bool playerAlive(Modifier<BeatmapObject> modifier, Dictionary<string, string> variables)
        {
            var levelObject = modifier.reference.runtimeObject;
            if (int.TryParse(modifier.value, out int index) && levelObject && levelObject.visualObject && levelObject.visualObject.gameObject)
            {
                if (PlayerManager.Players.Count > index)
                {
                    var closest = PlayerManager.Players[index];

                    return closest.Player && closest.Player.Alive;
                }
            }

            return false;
        }
        
        public static bool playerDeathsEquals<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            return RTBeatmap.Current.deaths.Count == modifier.GetInt(0, 0, variables);
        }
        
        public static bool playerDeathsLesserEquals<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            return RTBeatmap.Current.deaths.Count <= modifier.GetInt(0, 0, variables);
        }
        
        public static bool playerDeathsGreaterEquals<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            return RTBeatmap.Current.deaths.Count >= modifier.GetInt(0, 0, variables);
        }
        
        public static bool playerDeathsLesser<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            return RTBeatmap.Current.deaths.Count < modifier.GetInt(0, 0, variables);
        }
        
        public static bool playerDeathsGreater<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            return RTBeatmap.Current.deaths.Count > modifier.GetInt(0, 0, variables);
        }
        
        public static bool playerDistanceGreater<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            if (modifier.reference is not ITransformable transformable)
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
        
        public static bool playerDistanceLesser<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            if (modifier.reference is not ITransformable transformable)
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
        
        public static bool playerCountEquals<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            return InputDataManager.inst.players.Count == modifier.GetInt(0, 0, variables);
        }
        
        public static bool playerCountLesserEquals<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            return InputDataManager.inst.players.Count <= modifier.GetInt(0, 0, variables);
        }
        
        public static bool playerCountGreaterEquals<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            return InputDataManager.inst.players.Count >= modifier.GetInt(0, 0, variables);
        }
        
        public static bool playerCountLesser<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            return InputDataManager.inst.players.Count < modifier.GetInt(0, 0, variables);
        }
        
        public static bool playerCountGreater<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            return InputDataManager.inst.players.Count > modifier.GetInt(0, 0, variables);
        }
        
        public static bool onPlayerHit<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            return RTBeatmap.Current.playerHit;
        }
        
        public static bool onPlayerDeath<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            return RTBeatmap.Current.playerDied;
        }
        
        public static bool playerBoostEquals<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            return RTBeatmap.Current.boosts.Count == modifier.GetInt(0, 0, variables);
        }
        
        public static bool playerBoostLesserEquals<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            return RTBeatmap.Current.boosts.Count <= modifier.GetInt(0, 0, variables);
        }
        
        public static bool playerBoostGreaterEquals<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            return RTBeatmap.Current.boosts.Count >= modifier.GetInt(0, 0, variables);
        }
        
        public static bool playerBoostLesser<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            return RTBeatmap.Current.boosts.Count < modifier.GetInt(0, 0, variables);
        }
        
        public static bool playerBoostGreater<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            return RTBeatmap.Current.boosts.Count > modifier.GetInt(0, 0, variables);
        }

        #endregion

        #region Controls

        public static bool keyPressDown<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            return Input.GetKeyDown((KeyCode)modifier.GetInt(0, 0, variables));
        }

        public static bool keyPress<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            return Input.GetKey((KeyCode)modifier.GetInt(0, 0, variables));
        }

        public static bool keyPressUp<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            return Input.GetKeyUp((KeyCode)modifier.GetInt(0, 0, variables));
        }

        public static bool mouseButtonDown<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            return Input.GetMouseButtonDown(modifier.GetInt(0, 0, variables));
        }

        public static bool mouseButton<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            return Input.GetMouseButton(modifier.GetInt(0, 0, variables));
        }

        public static bool mouseButtonUp<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            return Input.GetMouseButtonUp(modifier.GetInt(0, 0, variables));
        }

        public static bool mouseOver(Modifier<BeatmapObject> modifier, Dictionary<string, string> variables)
        {
            if (modifier.reference.runtimeObject && modifier.reference.runtimeObject.visualObject && modifier.reference.runtimeObject.visualObject.gameObject)
            {
                if (!modifier.reference.detector)
                {
                    var gameObject = modifier.reference.runtimeObject.visualObject.gameObject;
                    var op = gameObject.GetOrAddComponent<Detector>();
                    op.beatmapObject = modifier.reference;
                    modifier.reference.detector = op;
                }

                if (modifier.reference.detector)
                    return modifier.reference.detector.hovered;
            }

            return false;
        }

        public static bool mouseOverSignalModifier(Modifier<BeatmapObject> modifier, Dictionary<string, string> variables)
        {
            var delay = modifier.GetFloat(0, 0f, variables);
            var list = GameData.Current.FindObjectsWithTag(modifier, modifier.GetValue(1, variables));
            if (modifier.reference.runtimeObject && modifier.reference.runtimeObject.visualObject && modifier.reference.runtimeObject.visualObject.gameObject)
            {
                if (!modifier.reference.detector)
                {
                    var gameObject = modifier.reference.runtimeObject.visualObject.gameObject;
                    var op = gameObject.GetOrAddComponent<Detector>();
                    op.beatmapObject = modifier.reference;
                    modifier.reference.detector = op;
                }

                if (modifier.reference.detector)
                {
                    if (modifier.reference.detector.hovered && !list.IsEmpty())
                    {
                        foreach (var bm in list)
                            CoroutineHelper.StartCoroutine(ModifiersHelper.ActivateModifier(bm, delay));
                    }

                    if (modifier.reference.detector.hovered)
                        return true;
                }
            }

            return false;
        }

        public static bool controlPressDown<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            var type = modifier.GetInt(0, 0, variables);

            if (modifier.reference is not ITransformable transformable)
                return false;

            var player = PlayerManager.GetClosestPlayer(transformable.GetFullPosition());
            var device = player?.device ?? InControl.InputManager.ActiveDevice;

            if (device == null)
                return false;

            return Enum.TryParse(((PlayerInputControlType)type).ToString(), out InControl.InputControlType inputControlType) && device.GetControl(inputControlType).WasPressed;
        }

        public static bool controlPress<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            var type = modifier.GetInt(0, 0, variables);

            if (modifier.reference is not ITransformable transformable)
                return false;

            var player = PlayerManager.GetClosestPlayer(transformable.GetFullPosition());
            var device = player?.device ?? InControl.InputManager.ActiveDevice;

            if (device == null)
                return false;

            return Enum.TryParse(((PlayerInputControlType)type).ToString(), out InControl.InputControlType inputControlType) && device.GetControl(inputControlType).IsPressed;
        }

        public static bool controlPressUp<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            var type = modifier.GetInt(0, 0, variables);

            if (modifier.reference is not ITransformable transformable)
                return false;

            var player = PlayerManager.GetClosestPlayer(transformable.GetFullPosition());
            var device = player?.device ?? InControl.InputManager.ActiveDevice;

            if (device == null)
                return false;

            return Enum.TryParse(((PlayerInputControlType)type).ToString(), out InControl.InputControlType inputControlType) && device.GetControl(inputControlType).WasReleased;
        }

        #endregion

        #region Collide

        public static bool bulletCollide(Modifier<BeatmapObject> modifier, Dictionary<string, string> variables)
        {
            var runtimeObject = modifier.reference.runtimeObject;
            if (!runtimeObject || !runtimeObject.visualObject || !runtimeObject.visualObject.gameObject)
                return false;

            if (!modifier.reference.detector)
            {
                var op = runtimeObject.visualObject.gameObject.GetOrAddComponent<Detector>();
                op.beatmapObject = modifier.reference;
                modifier.reference.detector = op;
            }

            if (modifier.reference.detector)
                return modifier.reference.detector.bulletOver;

            return false;
        }

        public static bool objectCollide(Modifier<BeatmapObject> modifier, Dictionary<string, string> variables)
        {
            var runtimeObject = modifier.reference.runtimeObject;
            if (!runtimeObject || !runtimeObject.visualObject || !runtimeObject.visualObject.collider)
                return false;

            var list = GameData.Current.FindObjectsWithTag(modifier, modifier.GetValue(0)).FindAll(x => x.runtimeObject.visualObject && x.runtimeObject.visualObject.collider);
            return !list.IsEmpty() && list.Any(x => x.runtimeObject.visualObject.collider.IsTouching(runtimeObject.visualObject.collider));
        }

        #endregion

        #region JSON

        public static bool loadEquals<T>(Modifier<T> modifier, Dictionary<string, string> variables)
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

        public static bool loadLesserEquals<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            if (RTFile.TryReadFromFile(ModifiersHelper.GetSaveFile(modifier.GetValue(1, variables)), out string json))
            {
                var jn = JSON.Parse(json);

                var fjn = jn[modifier.GetValue(2, variables)][modifier.GetValue(3, variables)]["float"];

                return !string.IsNullOrEmpty(fjn) && fjn.AsFloat <= modifier.GetFloat(0, 0f, variables);
            }

            return false;
        }

        public static bool loadGreaterEquals<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            if (RTFile.TryReadFromFile(ModifiersHelper.GetSaveFile(modifier.GetValue(1, variables)), out string json))
            {
                var jn = JSON.Parse(json);

                var fjn = jn[modifier.GetValue(2, variables)][modifier.GetValue(3, variables)]["float"];

                return !string.IsNullOrEmpty(fjn) && fjn.AsFloat >= modifier.GetFloat(0, 0f, variables);
            }

            return false;
        }

        public static bool loadLesser<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            if (RTFile.TryReadFromFile(ModifiersHelper.GetSaveFile(modifier.GetValue(1, variables)), out string json))
            {
                var jn = JSON.Parse(json);

                var fjn = jn[modifier.GetValue(2, variables)][modifier.GetValue(3, variables)]["float"];

                return !string.IsNullOrEmpty(fjn) && fjn.AsFloat < modifier.GetFloat(0, 0f, variables);
            }

            return false;
        }

        public static bool loadGreater<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            if (RTFile.TryReadFromFile(ModifiersHelper.GetSaveFile(modifier.GetValue(1, variables)), out string json))
            {
                var jn = JSON.Parse(json);

                var fjn = jn[modifier.GetValue(2, variables)][modifier.GetValue(3, variables)]["float"];

                return !string.IsNullOrEmpty(fjn) && fjn.AsFloat > modifier.GetFloat(0, 0f, variables);
            }

            return false;
        }

        public static bool loadExists<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            return RTFile.TryReadFromFile(ModifiersHelper.GetSaveFile(modifier.GetValue(1, variables)), out string json) && !string.IsNullOrEmpty(JSON.Parse(json)[modifier.GetValue(2, variables)][modifier.GetValue(3, variables)]);
        }

        #endregion

        #region Variable

        public static bool localVariableEquals<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            return variables.TryGetValue(modifier.GetValue(0), out string result) && result == modifier.GetValue(1, variables);
        }
        
        public static bool localVariableLesserEquals<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            return variables.TryGetValue(modifier.GetValue(0), out string result) && (float.TryParse(result, out float num) ? num : Parser.TryParse(result, 0)) <= modifier.GetFloat(1, 0f, variables);
        }
        
        public static bool localVariableGreaterEquals<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            return variables.TryGetValue(modifier.GetValue(0), out string result) && (float.TryParse(result, out float num) ? num : Parser.TryParse(result, 0)) >= modifier.GetFloat(1, 0f, variables);
        }
        
        public static bool localVariableLesser<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            return variables.TryGetValue(modifier.GetValue(0), out string result) && (float.TryParse(result, out float num) ? num : Parser.TryParse(result, 0)) < modifier.GetFloat(1, 0f, variables);
        }

        public static bool localVariableGreater<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            return variables.TryGetValue(modifier.GetValue(0), out string result) && (float.TryParse(result, out float num) ? num : Parser.TryParse(result, 0)) > modifier.GetFloat(1, 0f, variables);
        }

        public static bool localVariableContains<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            return variables.TryGetValue(modifier.GetValue(0), out string result) && result.Contains(modifier.GetValue(1, variables));
        }

        public static bool localVariableStartsWith<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            return variables.TryGetValue(modifier.GetValue(0), out string result) && result.StartsWith(modifier.GetValue(1, variables));
        }

        public static bool localVariableEndsWith<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            return variables.TryGetValue(modifier.GetValue(0), out string result) && result.EndsWith(modifier.GetValue(1, variables));
        }

        public static bool localVariableExists<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            return variables.ContainsKey(modifier.GetValue(0));
        }
        
        public static bool variableEquals<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            return modifier.reference is IModifyable<T> modifyable && modifyable.IntVariable == modifier.GetInt(0, 0, variables);
        }

        public static bool variableLesserEquals<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            return modifier.reference is IModifyable<T> modifyable && modifyable.IntVariable <= modifier.GetInt(0, 0, variables);
        }
        
        public static bool variableGreaterEquals<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            return modifier.reference is IModifyable<T> modifyable && modifyable.IntVariable >= modifier.GetInt(0, 0, variables);
        }
        
        public static bool variableLesser<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            return modifier.reference is IModifyable<T> modifyable && modifyable.IntVariable < modifier.GetInt(0, 0, variables);
        }
        
        public static bool variableGreater<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            return modifier.reference is IModifyable<T> modifyable && modifyable.IntVariable > modifier.GetInt(0, 0, variables);
        }
        
        public static bool variableOtherEquals(Modifier<BeatmapObject> modifier, Dictionary<string, string> variables)
        {
            var beatmapObjects = GameData.Current.FindObjectsWithTag(modifier, modifier.GetValue(1, variables));

            int num = modifier.GetInt(0, 0, variables);

            return !beatmapObjects.IsEmpty() && beatmapObjects.Any(x => x.integerVariable == num);
        }
        
        public static bool variableOtherLesserEquals(Modifier<BeatmapObject> modifier, Dictionary<string, string> variables)
        {
            var beatmapObjects = GameData.Current.FindObjectsWithTag(modifier, modifier.GetValue(1, variables));

            int num = modifier.GetInt(0, 0, variables);

            return !beatmapObjects.IsEmpty() && beatmapObjects.Any(x => x.integerVariable <= num);
        }
        
        public static bool variableOtherGreaterEquals(Modifier<BeatmapObject> modifier, Dictionary<string, string> variables)
        {
            var beatmapObjects = GameData.Current.FindObjectsWithTag(modifier, modifier.GetValue(1, variables));

            int num = modifier.GetInt(0, 0, variables);

            return !beatmapObjects.IsEmpty() && beatmapObjects.Any(x => x.integerVariable >= num);
        }
        
        public static bool variableOtherLesser(Modifier<BeatmapObject> modifier, Dictionary<string, string> variables)
        {
            var beatmapObjects = GameData.Current.FindObjectsWithTag(modifier, modifier.GetValue(1, variables));

            int num = modifier.GetInt(0, 0, variables);

            return !beatmapObjects.IsEmpty() && beatmapObjects.Any(x => x.integerVariable < num);
        }
        
        public static bool variableOtherGreater(Modifier<BeatmapObject> modifier, Dictionary<string, string> variables)
        {
            var beatmapObjects = GameData.Current.FindObjectsWithTag(modifier, modifier.GetValue(1, variables));

            int num = modifier.GetInt(0, 0, variables);

            return !beatmapObjects.IsEmpty() && beatmapObjects.Any(x => x.integerVariable > num);
        }

        #endregion

        #region Audio

        public static bool pitchEquals<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            return AudioManager.inst.pitch == modifier.GetFloat(0, 0f, variables);
        }
        
        public static bool pitchLesserEquals<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            return AudioManager.inst.pitch <= modifier.GetFloat(0, 0f, variables);
        }
        
        public static bool pitchGreaterEquals<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            return AudioManager.inst.pitch >= modifier.GetFloat(0, 0f, variables);
        }
        
        public static bool pitchLesser<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            return AudioManager.inst.pitch < modifier.GetFloat(0, 0f, variables);
        }
        
        public static bool pitchGreater<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            return AudioManager.inst.pitch > modifier.GetFloat(0, 0f, variables);
        }
        
        public static bool musicTimeGreater<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            return AudioManager.inst.CurrentAudioSource.time - (modifier.GetBool(1, false, variables) && modifier.reference is ILifetime<AutoKillType> lifetime ? lifetime.StartTime : 0f) > modifier.GetFloat(0, 0f, variables);
        }
        
        public static bool musicTimeLesser<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            return AudioManager.inst.CurrentAudioSource.time - (modifier.GetBool(1, false, variables) && modifier.reference is ILifetime<AutoKillType> lifetime ? lifetime.StartTime : 0f) < modifier.GetFloat(0, 0f, variables);
        }

        public static bool musicPlaying<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            return AudioManager.inst.CurrentAudioSource.isPlaying;
        }

        #endregion

        #region Challenge Mode

        public static bool inZenMode<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            return RTBeatmap.Current.Invincible;
        }
        
        public static bool inNormal<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            return RTBeatmap.Current.IsNormal;
        }
        
        public static bool in1Life<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            return RTBeatmap.Current.Is1Life;
        }
        
        public static bool inNoHit<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            return RTBeatmap.Current.IsNoHit;
        }
        
        public static bool inPractice<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            return RTBeatmap.Current.IsPractice;
        }

        #endregion

        #region Random

        public static bool randomEquals<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            if (!modifier.HasResult())
                modifier.Result = UnityEngine.Random.Range(modifier.GetInt(1, 0, variables), modifier.GetInt(2, 0, variables)) == modifier.GetInt(0, 0, variables);

            return modifier.HasResult() && modifier.GetResult<bool>();
        }
        
        public static bool randomLesser<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            if (!modifier.HasResult())
                modifier.Result = UnityEngine.Random.Range(modifier.GetInt(1, 0, variables), modifier.GetInt(2, 0, variables)) < modifier.GetInt(0, 0, variables);

            return modifier.HasResult() && modifier.GetResult<bool>();
        }
        
        public static bool randomGreater<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            if (!modifier.HasResult())
                modifier.Result = UnityEngine.Random.Range(modifier.GetInt(1, 0, variables), modifier.GetInt(2, 0, variables)) > modifier.GetInt(0, 0, variables);

            return modifier.HasResult() && modifier.GetResult<bool>();
        }

        #endregion

        #region Math

        public static bool mathEquals<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            if (modifier.reference is not IEvaluatable evaluatable)
                return false;

            var numberVariables = evaluatable.GetObjectVariables();
            ModifiersHelper.SetVariables(variables, numberVariables);
            var functions = evaluatable.GetObjectFunctions();

            return RTMath.Parse(modifier.GetValue(0, variables), numberVariables, functions) == RTMath.Parse(modifier.GetValue(1, variables), numberVariables, functions);
        }

        public static bool mathLesserEquals<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            if (modifier.reference is not IEvaluatable evaluatable)
                return false;

            var numberVariables = evaluatable.GetObjectVariables();
            ModifiersHelper.SetVariables(variables, numberVariables);
            var functions = evaluatable.GetObjectFunctions();

            return RTMath.Parse(modifier.GetValue(0, variables), numberVariables, functions) <= RTMath.Parse(modifier.GetValue(1, variables), numberVariables, functions);
        }

        public static bool mathGreaterEquals<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            if (modifier.reference is not IEvaluatable evaluatable)
                return false;

            var numberVariables = evaluatable.GetObjectVariables();
            ModifiersHelper.SetVariables(variables, numberVariables);
            var functions = evaluatable.GetObjectFunctions();

            return RTMath.Parse(modifier.GetValue(0, variables), numberVariables, functions) >= RTMath.Parse(modifier.GetValue(1, variables), numberVariables, functions);
        }
        
        public static bool mathLesser<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            if (modifier.reference is not IEvaluatable evaluatable)
                return false;

            var numberVariables = evaluatable.GetObjectVariables();
            ModifiersHelper.SetVariables(variables, numberVariables);
            var functions = evaluatable.GetObjectFunctions();

            return RTMath.Parse(modifier.GetValue(0, variables), numberVariables, functions) < RTMath.Parse(modifier.GetValue(1, variables), numberVariables, functions);
        }
        
        public static bool mathGreater<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            if (modifier.reference is not IEvaluatable evaluatable)
                return false;

            var numberVariables = evaluatable.GetObjectVariables();
            ModifiersHelper.SetVariables(variables, numberVariables);
            var functions = evaluatable.GetObjectFunctions();

            return RTMath.Parse(modifier.GetValue(0, variables), numberVariables, functions) > RTMath.Parse(modifier.GetValue(1, variables), numberVariables, functions);
        }

        #endregion

        #region Axis

        public static bool axisEquals<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            if (modifier.reference is not IPrefabable prefabable)
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

            if (!GameData.Current.TryFindObjectWithTag(modifier.prefabInstanceOnly, modifier.groupAlive, prefabable, modifier.GetValue(0, variables), out BeatmapObject bm))
                return false;

            fromType = Mathf.Clamp(fromType, 0, bm.events.Count);
            fromAxis = Mathf.Clamp(fromAxis, 0, bm.events[fromType][0].values.Length);


            return fromType >= 0 && fromType <= 2 && ModifiersHelper.GetAnimation(bm, fromType, fromAxis, min, max, offset, multiply, delay, loop, visual) == equals;
        }
        
        public static bool axisLesserEquals<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            if (modifier.reference is not IPrefabable prefabable)
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

            if (!GameData.Current.TryFindObjectWithTag(modifier.prefabInstanceOnly, modifier.groupAlive, prefabable, modifier.GetValue(0, variables), out BeatmapObject bm))
                return false;

            fromType = Mathf.Clamp(fromType, 0, bm.events.Count);
            fromAxis = Mathf.Clamp(fromAxis, 0, bm.events[fromType][0].values.Length);


            return fromType >= 0 && fromType <= 2 && ModifiersHelper.GetAnimation(bm, fromType, fromAxis, min, max, offset, multiply, delay, loop, visual) <= equals;
        }
        
        public static bool axisGreaterEquals<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            if (modifier.reference is not IPrefabable prefabable)
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

            if (!GameData.Current.TryFindObjectWithTag(modifier.prefabInstanceOnly, modifier.groupAlive, prefabable, modifier.GetValue(0, variables), out BeatmapObject bm))
                return false;

            fromType = Mathf.Clamp(fromType, 0, bm.events.Count);
            fromAxis = Mathf.Clamp(fromAxis, 0, bm.events[fromType][0].values.Length);


            return fromType >= 0 && fromType <= 2 && ModifiersHelper.GetAnimation(bm, fromType, fromAxis, min, max, offset, multiply, delay, loop, visual) >= equals;
        }
        
        public static bool axisLesser<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            if (modifier.reference is not IPrefabable prefabable)
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

            if (!GameData.Current.TryFindObjectWithTag(modifier.prefabInstanceOnly, modifier.groupAlive, prefabable, modifier.GetValue(0, variables), out BeatmapObject bm))
                return false;

            fromType = Mathf.Clamp(fromType, 0, bm.events.Count);
            fromAxis = Mathf.Clamp(fromAxis, 0, bm.events[fromType][0].values.Length);


            return fromType >= 0 && fromType <= 2 && ModifiersHelper.GetAnimation(bm, fromType, fromAxis, min, max, offset, multiply, delay, loop, visual) < equals;
        }
        
        public static bool axisGreater<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            if (modifier.reference is not IPrefabable prefabable)
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

            if (!GameData.Current.TryFindObjectWithTag(modifier.prefabInstanceOnly, modifier.groupAlive, prefabable, modifier.GetValue(0, variables), out BeatmapObject bm))
                return false;

            fromType = Mathf.Clamp(fromType, 0, bm.events.Count);
            fromAxis = Mathf.Clamp(fromAxis, 0, bm.events[fromType][0].values.Length);


            return fromType >= 0 && fromType <= 2 && ModifiersHelper.GetAnimation(bm, fromType, fromAxis, min, max, offset, multiply, delay, loop, visual) > equals;
        }

        #endregion

        #region Level Rank

        public static bool levelRankEquals<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            return ModifiersHelper.GetLevelRank(LevelManager.CurrentLevel, out int levelRankIndex) && levelRankIndex == modifier.GetInt(0, 0, variables);
        }
        
        public static bool levelRankLesserEquals<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            return ModifiersHelper.GetLevelRank(LevelManager.CurrentLevel, out int levelRankIndex) && levelRankIndex <= modifier.GetInt(0, 0, variables);
        }
        
        public static bool levelRankGreaterEquals<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            return ModifiersHelper.GetLevelRank(LevelManager.CurrentLevel, out int levelRankIndex) && levelRankIndex >= modifier.GetInt(0, 0, variables);
        }
        
        public static bool levelRankLesser<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            return ModifiersHelper.GetLevelRank(LevelManager.CurrentLevel, out int levelRankIndex) && levelRankIndex < modifier.GetInt(0, 0, variables);
        }
        
        public static bool levelRankGreater<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            return ModifiersHelper.GetLevelRank(LevelManager.CurrentLevel, out int levelRankIndex) && levelRankIndex > modifier.GetInt(0, 0, variables);
        }
        
        public static bool levelRankOtherEquals<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            var id = modifier.GetValue(1, variables);
            return LevelManager.Levels.TryFind(x => x.id == id, out Level level) && ModifiersHelper.GetLevelRank(level, out int levelRankIndex) && levelRankIndex == modifier.GetInt(0, 0, variables);
        }
        
        public static bool levelRankOtherLesserEquals<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            var id = modifier.GetValue(1, variables);
            return LevelManager.Levels.TryFind(x => x.id == id, out Level level) && ModifiersHelper.GetLevelRank(level, out int levelRankIndex) && levelRankIndex <= modifier.GetInt(0, 0, variables);
        }
        
        public static bool levelRankOtherGreaterEquals<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            var id = modifier.GetValue(1, variables);
            return LevelManager.Levels.TryFind(x => x.id == id, out Level level) && ModifiersHelper.GetLevelRank(level, out int levelRankIndex) && levelRankIndex >= modifier.GetInt(0, 0, variables);
        }
        
        public static bool levelRankOtherLesser<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            var id = modifier.GetValue(1, variables);
            return LevelManager.Levels.TryFind(x => x.id == id, out Level level) && ModifiersHelper.GetLevelRank(level, out int levelRankIndex) && levelRankIndex < modifier.GetInt(0, 0, variables);
        }
        
        public static bool levelRankOtherGreater<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            var id = modifier.GetValue(1, variables);
            return LevelManager.Levels.TryFind(x => x.id == id, out Level level) && ModifiersHelper.GetLevelRank(level, out int levelRankIndex) && levelRankIndex > modifier.GetInt(0, 0, variables);
        }
        
        public static bool levelRankCurrentEquals<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            return LevelManager.GetLevelRank(RTBeatmap.Current.hits) == modifier.GetInt(0, 0, variables);
        }
        
        public static bool levelRankCurrentLesserEquals<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            return LevelManager.GetLevelRank(RTBeatmap.Current.hits) <= modifier.GetInt(0, 0, variables);
        }
        
        public static bool levelRankCurrentGreaterEquals<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            return LevelManager.GetLevelRank(RTBeatmap.Current.hits) >= modifier.GetInt(0, 0, variables);
        }
        
        public static bool levelRankCurrentLesser<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            return LevelManager.GetLevelRank(RTBeatmap.Current.hits) < modifier.GetInt(0, 0, variables);
        }
        
        public static bool levelRankCurrentGreater<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            return LevelManager.GetLevelRank(RTBeatmap.Current.hits) > modifier.GetInt(0, 0, variables);
        }

        #endregion

        #region Level

        public static bool levelUnlocked<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            var id = modifier.GetValue(0, variables);
            return LevelManager.Levels.TryFind(x => x.id == id, out Level level) && !level.Locked;
        }

        public static bool levelCompleted<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            return CoreHelper.InEditor || LevelManager.CurrentLevel && LevelManager.CurrentLevel.saveData && LevelManager.CurrentLevel.saveData.Completed;
        }

        public static bool levelCompletedOther<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            var id = modifier.GetValue(0, variables);
            return CoreHelper.InEditor || LevelManager.Levels.TryFind(x => x.id == id, out Level level) && level.saveData && level.saveData.Completed;
        }

        public static bool levelExists<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            var id = modifier.GetValue(0, variables);
            return LevelManager.Levels.Has(x => x.id == id);
        }

        public static bool levelPathExists<T>(Modifier<T> modifier, Dictionary<string, string> variables)
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
        public static bool realTimeSecondEquals<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            return Parser.TryParse(DateTime.Now.ToString("ss"), 0) == modifier.GetInt(0, 0, variables);
        }
        public static bool realTimeSecondLesserEquals<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            return Parser.TryParse(DateTime.Now.ToString("ss"), 0) <= modifier.GetInt(0, 0, variables);
        }
        public static bool realTimeSecondGreaterEquals<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            return Parser.TryParse(DateTime.Now.ToString("ss"), 0) >= modifier.GetInt(0, 0, variables);
        }
        public static bool realTimeSecondLesser<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            return Parser.TryParse(DateTime.Now.ToString("ss"), 0) < modifier.GetInt(0, 0, variables);
        }
        public static bool realTimeSecondGreater<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            return Parser.TryParse(DateTime.Now.ToString("ss"), 0) > modifier.GetInt(0, 0, variables);
        }

        // minutes
        public static bool realTimeMinuteEquals<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            return Parser.TryParse(DateTime.Now.ToString("mm"), 0) == modifier.GetInt(0, 0, variables);
        }
        public static bool realTimeMinuteLesserEquals<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            return Parser.TryParse(DateTime.Now.ToString("mm"), 0) <= modifier.GetInt(0, 0, variables);
        }
        public static bool realTimeMinuteGreaterEquals<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            return Parser.TryParse(DateTime.Now.ToString("mm"), 0) >= modifier.GetInt(0, 0, variables);
        }
        public static bool realTimeMinuteLesser<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            return Parser.TryParse(DateTime.Now.ToString("mm"), 0) < modifier.GetInt(0, 0, variables);
        }
        public static bool realTimeMinuteGreater<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            return Parser.TryParse(DateTime.Now.ToString("mm"), 0) > modifier.GetInt(0, 0, variables);
        }

        // 24 hours
        public static bool realTime24HourEquals<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            return Parser.TryParse(DateTime.Now.ToString("HH"), 0) == modifier.GetInt(0, 0, variables);
        }
        public static bool realTime24HourLesserEquals<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            return Parser.TryParse(DateTime.Now.ToString("HH"), 0) <= modifier.GetInt(0, 0, variables);
        }
        public static bool realTime24HourGreaterEquals<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            return Parser.TryParse(DateTime.Now.ToString("HH"), 0) >= modifier.GetInt(0, 0, variables);
        }
        public static bool realTime24HourLesser<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            return Parser.TryParse(DateTime.Now.ToString("HH"), 0) < modifier.GetInt(0, 0, variables);
        }
        public static bool realTime24HourGreater<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            return Parser.TryParse(DateTime.Now.ToString("HH"), 0) > modifier.GetInt(0, 0, variables);
        }

        // 12 hours
        public static bool realTime12HourEquals<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            return Parser.TryParse(DateTime.Now.ToString("hh"), 0) == modifier.GetInt(0, 0, variables);
        }
        public static bool realTime12HourLesserEquals<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            return Parser.TryParse(DateTime.Now.ToString("hh"), 0) <= modifier.GetInt(0, 0, variables);
        }
        public static bool realTime12HourGreaterEquals<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            return Parser.TryParse(DateTime.Now.ToString("hh"), 0) >= modifier.GetInt(0, 0, variables);
        }
        public static bool realTime12HourLesser<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            return Parser.TryParse(DateTime.Now.ToString("hh"), 0) < modifier.GetInt(0, 0, variables);
        }
        public static bool realTime12HourGreater<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            return Parser.TryParse(DateTime.Now.ToString("hh"), 0) > modifier.GetInt(0, 0, variables);
        }

        // days
        public static bool realTimeDayEquals<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            return Parser.TryParse(DateTime.Now.ToString("dd"), 0) == modifier.GetInt(0, 0, variables);
        }
        public static bool realTimeDayLesserEquals<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            return Parser.TryParse(DateTime.Now.ToString("dd"), 0) <= modifier.GetInt(0, 0, variables);
        }
        public static bool realTimeDayGreaterEquals<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            return Parser.TryParse(DateTime.Now.ToString("dd"), 0) >= modifier.GetInt(0, 0, variables);
        }
        public static bool realTimeDayLesser<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            return Parser.TryParse(DateTime.Now.ToString("dd"), 0) < modifier.GetInt(0, 0, variables);
        }
        public static bool realTimeDayGreater<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            return Parser.TryParse(DateTime.Now.ToString("dd"), 0) > modifier.GetInt(0, 0, variables);
        }
        
        public static bool realTimeDayWeekEquals<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            return DateTime.Now.ToString("dddd") == modifier.GetValue(0, variables);
        }

        // months
        public static bool realTimeMonthEquals<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            return Parser.TryParse(DateTime.Now.ToString("MM"), 0) == modifier.GetInt(0, 0, variables);
        }
        public static bool realTimeMonthLesserEquals<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            return Parser.TryParse(DateTime.Now.ToString("MM"), 0) <= modifier.GetInt(0, 0, variables);
        }
        public static bool realTimeMonthGreaterEquals<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            return Parser.TryParse(DateTime.Now.ToString("MM"), 0) >= modifier.GetInt(0, 0, variables);
        }
        public static bool realTimeMonthLesser<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            return Parser.TryParse(DateTime.Now.ToString("MM"), 0) < modifier.GetInt(0, 0, variables);
        }
        public static bool realTimeMonthGreater<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            return Parser.TryParse(DateTime.Now.ToString("MM"), 0) > modifier.GetInt(0, 0, variables);
        }

        // years
        public static bool realTimeYearEquals<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            return Parser.TryParse(DateTime.Now.ToString("yyyy"), 0) == modifier.GetInt(0, 0, variables);
        }
        public static bool realTimeYearLesserEquals<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            return Parser.TryParse(DateTime.Now.ToString("yyyy"), 0) <= modifier.GetInt(0, 0, variables);
        }
        public static bool realTimeYearGreaterEquals<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            return Parser.TryParse(DateTime.Now.ToString("yyyy"), 0) >= modifier.GetInt(0, 0, variables);
        }
        public static bool realTimeYearLesser<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            return Parser.TryParse(DateTime.Now.ToString("yyyy"), 0) < modifier.GetInt(0, 0, variables);
        }
        public static bool realTimeYearGreater<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            return Parser.TryParse(DateTime.Now.ToString("yyyy"), 0) > modifier.GetInt(0, 0, variables);
        }

        #endregion

        #region Config

        public static bool usernameEquals<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            return CoreConfig.Instance.DisplayName.Value == modifier.GetValue(0, variables);
        }
        
        public static bool languageEquals<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            return CoreConfig.Instance.Language.Value == (Language)modifier.GetInt(0, 0, variables);
        }
        
        public static bool configLDM<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            return CoreConfig.Instance.LDM.Value;
        }

        public static bool configShowEffects<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            return EventsConfig.Instance.ShowFX.Value;
        }

        public static bool configShowPlayerGUI<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            return EventsConfig.Instance.ShowGUI.Value;
        }
        
        public static bool configShowIntro<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            return EventsConfig.Instance.ShowIntro.Value;
        }

        #endregion

        #region Misc

        public static bool await<T>(Modifier<T> modifier, Dictionary<string, string> variables)
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
                time = RTLevel.Current.FixedTime;

            return time > modifier.GetFloat(0, 0f, variables);
        }

        public static bool containsTag<T>(Modifier<T> modifier, Dictionary<string, string> variables) => modifier.reference is IModifyable<T> modifyable && modifyable.Tags.Contains(modifier.GetValue(0, variables));

        public static bool inEditor<T>(Modifier<T> modifier, Dictionary<string, string> variables) => CoreHelper.InEditor;
        
        public static bool isEditing<T>(Modifier<T> modifier, Dictionary<string, string> variables) => CoreHelper.IsEditing;

        public static bool requireSignal<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            return modifier.HasResult();
        }
        
        public static bool isFullscreen<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            return Screen.fullScreen;
        }
        
        public static bool objectAlive(Modifier<BeatmapObject> modifier, Dictionary<string, string> variables)
        {
            var list = GameData.Current.FindObjectsWithTag(modifier, modifier.GetValue(0, variables));
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].Alive)
                    return true;
            }
            return false;
        }
        
        public static bool objectSpawned(Modifier<BeatmapObject> modifier, Dictionary<string, string> variables)
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
        }

        #endregion

        #region Dev Only

        #endregion

        public static class PlayerTriggers
        {
            public static bool keyPressDown(Modifier<CustomPlayer> modifier, Dictionary<string, string> variables)
            {
                return Input.GetKeyDown((KeyCode)modifier.GetInt(0, 0, variables));
            }

            public static bool keyPress(Modifier<CustomPlayer> modifier, Dictionary<string, string> variables)
            {
                return Input.GetKey((KeyCode)modifier.GetInt(0, 0, variables));
            }

            public static bool keyPressUp(Modifier<CustomPlayer> modifier, Dictionary<string, string> variables)
            {
                return Input.GetKeyUp((KeyCode)modifier.GetInt(0, 0, variables));
            }

            public static bool mouseButtonDown(Modifier<CustomPlayer> modifier, Dictionary<string, string> variables)
            {
                return Input.GetMouseButtonDown(modifier.GetInt(0, 0, variables));
            }

            public static bool mouseButton(Modifier<CustomPlayer> modifier, Dictionary<string, string> variables)
            {
                return Input.GetMouseButton(modifier.GetInt(0, 0, variables));
            }

            public static bool mouseButtonUp(Modifier<CustomPlayer> modifier, Dictionary<string, string> variables)
            {
                return Input.GetMouseButtonUp(modifier.GetInt(0, 0, variables));
            }

            public static bool controlPressDown(Modifier<CustomPlayer> modifier, Dictionary<string, string> variables)
            {
                var type = modifier.GetInt(0, 0, variables);
                var device = modifier.reference.device;

                return Enum.TryParse(((PlayerInputControlType)type).ToString(), out InControl.InputControlType inputControlType) && device.GetControl(inputControlType).WasPressed;
            }

            public static bool controlPress(Modifier<CustomPlayer> modifier, Dictionary<string, string> variables)
            {
                var type = modifier.GetInt(0, 0, variables);
                var device = modifier.reference.device;

                return Enum.TryParse(((PlayerInputControlType)type).ToString(), out InControl.InputControlType inputControlType) && device.GetControl(inputControlType).IsPressed;
            }

            public static bool controlPressUp(Modifier<CustomPlayer> modifier, Dictionary<string, string> variables)
            {
                var type = modifier.GetInt(0, 0, variables);
                var device = modifier.reference.device;

                return Enum.TryParse(((PlayerInputControlType)type).ToString(), out InControl.InputControlType inputControlType) && device.GetControl(inputControlType).WasReleased;
            }

            public static bool healthEquals(Modifier<CustomPlayer> modifier, Dictionary<string, string> variables)
            {
                return modifier.reference.Health == modifier.GetInt(0, 3, variables);
            }

            public static bool healthGreaterEquals(Modifier<CustomPlayer> modifier, Dictionary<string, string> variables)
            {
                return modifier.reference.Health >= modifier.GetInt(0, 3, variables);
            }

            public static bool healthLesserEquals(Modifier<CustomPlayer> modifier, Dictionary<string, string> variables)
            {
                return modifier.reference.Health <= modifier.GetInt(0, 3, variables);
            }

            public static bool healthGreater(Modifier<CustomPlayer> modifier, Dictionary<string, string> variables)
            {
                return modifier.reference.Health > modifier.GetInt(0, 3, variables);
            }

            public static bool healthLesser(Modifier<CustomPlayer> modifier, Dictionary<string, string> variables)
            {
                return modifier.reference.Health < modifier.GetInt(0, 3, variables);
            }

            public static bool healthPerEquals(Modifier<CustomPlayer> modifier, Dictionary<string, string> variables)
            {
                var health = ((float)modifier.reference.Health / modifier.reference.PlayerModel.basePart.health) * 100f;

                return health == modifier.GetFloat(0, 50f, variables);
            }

            public static bool healthPerGreaterEquals(Modifier<CustomPlayer> modifier, Dictionary<string, string> variables)
            {
                var health = ((float)modifier.reference.Health / modifier.reference.PlayerModel.basePart.health) * 100f;

                return health >= modifier.GetFloat(0, 50f, variables);
            }

            public static bool healthPerLesserEquals(Modifier<CustomPlayer> modifier, Dictionary<string, string> variables)
            {
                var health = ((float)modifier.reference.Health / modifier.reference.PlayerModel.basePart.health) * 100f;

                return health <= modifier.GetFloat(0, 50f, variables);
            }

            public static bool healthPerGreater(Modifier<CustomPlayer> modifier, Dictionary<string, string> variables)
            {
                var health = ((float)modifier.reference.Health / modifier.reference.PlayerModel.basePart.health) * 100f;

                return health > modifier.GetFloat(0, 50f, variables);
            }

            public static bool healthPerLesser(Modifier<CustomPlayer> modifier, Dictionary<string, string> variables)
            {
                var health = ((float)modifier.reference.Health / modifier.reference.PlayerModel.basePart.health) * 100f;

                return health < modifier.GetFloat(0, 50f, variables);
            }

            public static bool isDead(Modifier<CustomPlayer> modifier, Dictionary<string, string> variables)
            {
                return modifier.reference.Player && modifier.reference.Player.isDead;
            }

            public static bool isBoosting(Modifier<CustomPlayer> modifier, Dictionary<string, string> variables)
            {
                return modifier.reference.Player && modifier.reference.Player.isBoosting;
            }

            public static bool isColliding(Modifier<CustomPlayer> modifier, Dictionary<string, string> variables)
            {
                return modifier.reference.Player && modifier.reference.Player.triggerColliding;
            }

            public static bool isSolidColliding(Modifier<CustomPlayer> modifier, Dictionary<string, string> variables)
            {
                return modifier.reference.Player && modifier.reference.Player.colliding;
            }
        }
    }
}

#pragma warning restore IDE1006 // Naming Styles