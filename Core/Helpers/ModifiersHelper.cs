using BetterLegacy.Components;
using BetterLegacy.Components.Player;
using BetterLegacy.Configs;
using BetterLegacy.Core.Animation;
using BetterLegacy.Core.Animation.Keyframe;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Managers;
using BetterLegacy.Core.Managers.Networking;
using BetterLegacy.Core.Optimization;
using BetterLegacy.Core.Optimization.Objects;
using BetterLegacy.Core.Optimization.Objects.Visual;
using BetterLegacy.Editor.Managers;
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
    public static class ModifiersHelper
    {
        public static bool Trigger(Modifier<BeatmapObject> modifier)
        {
            if (!modifier.verified)
            {
                modifier.verified = true;
                modifier.VerifyModifier(ModifiersManager.modifierTypes);
            }

            if (!modifier.IsValid(ModifiersManager.modifierTypes))
                return false;

            switch (modifier.commands[0])
            {
                case "disableModifier":
                    {
                        return false;
                    }
                case "playerCollide":
                    {
                        return modifier.reference.IsTouchingPlayer();
                    }
                case "playerHealthEquals":
                    {
                        return InputDataManager.inst.players.Count > 0 && int.TryParse(modifier.value, out int num) && InputDataManager.inst.players.Any(x => x.health == num);
                    }
                case "playerHealthLesserEquals":
                    {
                        return InputDataManager.inst.players.Count > 0 && int.TryParse(modifier.value, out int num) && InputDataManager.inst.players.Any(x => x.health <= num);
                    }
                case "playerHealthGreaterEquals":
                    {
                        return InputDataManager.inst.players.Count > 0 && int.TryParse(modifier.value, out int num) && InputDataManager.inst.players.Any(x => x.health >= num);
                    }
                case "playerHealthLesser":
                    {
                        return InputDataManager.inst.players.Count > 0 && int.TryParse(modifier.value, out int num) && InputDataManager.inst.players.Any(x => x.health < num);
                    }
                case "playerHealthGreater":
                    {
                        return InputDataManager.inst.players.Count > 0 && int.TryParse(modifier.value, out int num) && InputDataManager.inst.players.Any(x => x.health > num);
                    }
                case "playerMoving":
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
                        break;
                    }
                case "playerBoosting":
                    {
                        if (modifier.reference != null && Updater.TryGetObject(modifier.reference, out LevelObject levelObject) && levelObject.visualObject.GameObject)
                        {
                            var orderedList = PlayerManager.Players
                                .Where(x => x.Player)
                                .OrderBy(x => Vector2.Distance(x.Player.playerObjects["RB Parent"].gameObject.transform.position, levelObject.visualObject.GameObject.transform.position)).ToList();

                            if (orderedList.Count > 0)
                            {
                                var closest = orderedList[0];

                                return closest.Player.isBoosting;
                            }
                        }

                        break;
                    }
                case "playerAlive":
                    {
                        if (int.TryParse(modifier.value, out int hit) && modifier.reference != null && Updater.TryGetObject(modifier.reference, out LevelObject levelObject) && levelObject.visualObject.GameObject)
                        {
                            if (PlayerManager.Players.Count > hit)
                            {
                                var closest = PlayerManager.Players[hit];

                                return closest.Player && closest.Player.PlayerAlive;
                            }
                        }

                        break;
                    }
                case "playerDeathsEquals":
                    {
                        return InputDataManager.inst.players.Count > 0 && int.TryParse(modifier.value, out int num) && InputDataManager.inst.players.Any(x => x.PlayerDeaths.Count == num);
                    }
                case "playerDeathsLesserEquals":
                    {
                        return InputDataManager.inst.players.Count > 0 && int.TryParse(modifier.value, out int num) && InputDataManager.inst.players.Any(x => x.PlayerDeaths.Count <= num);
                    }
                case "playerDeathsGreaterEquals":
                    {
                        return InputDataManager.inst.players.Count > 0 && int.TryParse(modifier.value, out int num) && InputDataManager.inst.players.Any(x => x.PlayerDeaths.Count >= num);
                    }
                case "playerDeathsLesser":
                    {
                        return InputDataManager.inst.players.Count > 0 && int.TryParse(modifier.value, out int num) && InputDataManager.inst.players.Any(x => x.PlayerDeaths.Count < num);
                    }
                case "playerDeathsGreater":
                    {
                        return InputDataManager.inst.players.Count > 0 && int.TryParse(modifier.value, out int num) && InputDataManager.inst.players.Any(x => x.PlayerDeaths.Count > num);
                    }
                case "playerDistanceGreater":
                    {
                        for (int i = 0; i < GameManager.inst.players.transform.childCount; i++)
                        {
                            if (GameManager.inst.players.transform.Find(string.Format("Player {0}", i + 1)) && modifier.reference != null && Updater.TryGetObject(modifier.reference, out LevelObject levelObject) && levelObject.visualObject.GameObject && float.TryParse(modifier.value, out float num))
                            {
                                var player = GameManager.inst.players.transform.Find(string.Format("Player {0}/Player", i + 1));
                                return Vector2.Distance(player.transform.position, levelObject.visualObject.GameObject.transform.position) > num;
                            }
                        }

                        break;
                    }
                case "playerDistanceLesser":
                    {
                        for (int i = 0; i < GameManager.inst.players.transform.childCount; i++)
                        {
                            if (GameManager.inst.players.transform.Find(string.Format("Player {0}", i + 1)) && modifier.reference != null && Updater.TryGetObject(modifier.reference, out LevelObject levelObject) && levelObject.visualObject.GameObject && float.TryParse(modifier.value, out float num))
                            {
                                var player = GameManager.inst.players.transform.Find(string.Format("Player {0}/Player", i + 1));
                                return Vector2.Distance(player.transform.position, levelObject.visualObject.GameObject.transform.position) < num;
                            }
                        }

                        break;
                    }
                case "playerCountEquals":
                    {
                        return int.TryParse(modifier.value, out int num) && InputDataManager.inst.players.Count == num;
                    }
                case "playerCountLesserEquals":
                    {
                        return int.TryParse(modifier.value, out int num) && InputDataManager.inst.players.Count <= num;
                    }
                case "playerCountGreaterEquals":
                    {
                        return int.TryParse(modifier.value, out int num) && InputDataManager.inst.players.Count >= num;
                    }
                case "playerCountLesser":
                    {
                        return int.TryParse(modifier.value, out int num) && InputDataManager.inst.players.Count < num;
                    }
                case "playerCountGreater":
                    {
                        return int.TryParse(modifier.value, out int num) && InputDataManager.inst.players.Count > num;
                    }
                case "keyPressDown":
                    {
                        return int.TryParse(modifier.value, out int num) && Input.GetKeyDown((KeyCode)num);
                    }
                case "keyPress":
                    {
                        return int.TryParse(modifier.value, out int num) && Input.GetKey((KeyCode)num);
                    }
                case "keyPressUp":
                    {
                        return int.TryParse(modifier.value, out int num) && Input.GetKeyUp((KeyCode)num);
                    }
                case "mouseButtonDown":
                    {
                        return int.TryParse(modifier.value, out int num) && Input.GetMouseButtonDown(num);
                    }
                case "mouseButton":
                    {
                        return int.TryParse(modifier.value, out int num) && Input.GetMouseButton(num);
                    }
                case "mouseButtonUp":
                    {
                        return int.TryParse(modifier.value, out int num) && Input.GetMouseButtonUp(num);
                    }
                case "mouseOver":
                    {
                        if (modifier.reference != null && modifier.reference.levelObject && modifier.reference.levelObject.visualObject != null && modifier.reference.levelObject.visualObject.GameObject)
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
                case "mouseOverSignalModifier":
                    {
                        var list = DataManager.inst.gameData.beatmapObjects.Where(x => (x as BeatmapObject).tags.Contains(modifier.commands[1]));
                        if (modifier.reference != null && modifier.reference.levelObject && modifier.reference.levelObject.visualObject != null && modifier.reference.levelObject.visualObject.GameObject)
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
                                    {
                                        CoreHelper.StartCoroutine(ModifiersManager.ActivateModifier((BeatmapObject)bm, Parser.TryParse(modifier.value, 0f)));
                                    }
                                }

                                if (modifier.reference.detector.hovered)
                                    return true;
                            }
                        }
                        break;
                    }
                case "bulletCollide":
                    {
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
                case "objectCollide":
                    {
                        if (Updater.TryGetObject(modifier.reference, out LevelObject levelObject) && levelObject.visualObject != null && levelObject.visualObject.Collider)
                        {
                            var list = DataManager.inst.gameData.beatmapObjects.Where(x => ((BeatmapObject)x).tags.Contains(modifier.value) && ((BeatmapObject)x).levelObject && ((BeatmapObject)x).levelObject.visualObject != null && ((BeatmapObject)x).levelObject.visualObject.Collider);
                            return list.Count() > 0 && list.Any(x => ((BeatmapObject)x).levelObject.visualObject.Collider.IsTouching(levelObject.visualObject.Collider));
                        }

                        break;
                    }
                case "loadEquals":
                    {
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
                case "loadLesserEquals":
                    {
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
                case "loadGreaterEquals":
                    {
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
                case "loadLesser":
                    {
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
                case "loadGreater":
                    {
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
                case "loadExists":
                    {
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
                case "variableEquals":
                    {
                        return int.TryParse(modifier.value, out int num) && modifier.reference && modifier.reference.integerVariable == num;
                    }
                case "variableLesserEquals":
                    {
                        return int.TryParse(modifier.value, out int num) && modifier.reference && modifier.reference.integerVariable <= num;
                    }
                case "variableGreaterEquals":
                    {
                        return int.TryParse(modifier.value, out int num) && modifier.reference && modifier.reference.integerVariable >= num;
                    }
                case "variableLesser":
                    {
                        return int.TryParse(modifier.value, out int num) && modifier.reference && modifier.reference.integerVariable < num;
                    }
                case "variableGreater":
                    {
                        return int.TryParse(modifier.value, out int num) && modifier.reference && modifier.reference.integerVariable > num;
                    }
                case "variableOtherEquals":
                    {
                        return
                            int.TryParse(modifier.value, out int num) &&
                            modifier.reference &&
                            !string.IsNullOrEmpty(modifier.commands[1]) &&
                            DataManager.inst.gameData.beatmapObjects.Any(x => ((BeatmapObject)x).tags.Contains(modifier.commands[1]) && ((BeatmapObject)x).integerVariable == num);
                    }
                case "variableOtherLesserEquals":
                    {
                        return
                            int.TryParse(modifier.value, out int num) &&
                            modifier.reference &&
                            !string.IsNullOrEmpty(modifier.commands[1]) &&
                            DataManager.inst.gameData.beatmapObjects.Any(x => ((BeatmapObject)x).tags.Contains(modifier.commands[1]) && ((BeatmapObject)x).integerVariable <= num);
                    }
                case "variableOtherGreaterEquals":
                    {
                        return
                            int.TryParse(modifier.value, out int num) &&
                            modifier.reference &&
                            !string.IsNullOrEmpty(modifier.commands[1]) &&
                            DataManager.inst.gameData.beatmapObjects.Any(x => ((BeatmapObject)x).tags.Contains(modifier.commands[1]) && ((BeatmapObject)x).integerVariable >= num);
                    }
                case "variableOtherLesser":
                    {
                        return
                            int.TryParse(modifier.value, out int num) &&
                            modifier.reference &&
                            !string.IsNullOrEmpty(modifier.commands[1]) &&
                            DataManager.inst.gameData.beatmapObjects.Any(x => ((BeatmapObject)x).tags.Contains(modifier.commands[1]) && ((BeatmapObject)x).integerVariable < num);
                    }
                case "variableOtherGreater":
                    {
                        return
                            int.TryParse(modifier.value, out int num) &&
                            modifier.reference &&
                            !string.IsNullOrEmpty(modifier.commands[1]) &&
                            DataManager.inst.gameData.beatmapObjects.Any(x => ((BeatmapObject)x).tags.Contains(modifier.commands[1]) && ((BeatmapObject)x).integerVariable > num);
                    }
                case "pitchEquals":
                    {
                        return float.TryParse(modifier.value, out float num) && AudioManager.inst.pitch == num;
                    }
                case "pitchLesserEquals":
                    {
                        return float.TryParse(modifier.value, out float num) && AudioManager.inst.pitch <= num;
                    }
                case "pitchGreaterEquals":
                    {
                        return float.TryParse(modifier.value, out float num) && AudioManager.inst.pitch >= num;
                    }
                case "pitchLesser":
                    {
                        return float.TryParse(modifier.value, out float num) && AudioManager.inst.pitch < num;
                    }
                case "pitchGreater":
                    {
                        return float.TryParse(modifier.value, out float num) && AudioManager.inst.pitch > num;
                    }
                case "onPlayerHit":
                    {
                        if (modifier.Result == null)
                        {
                            modifier.Result = PlayerManager.Players.Select(x => x.Health).ToList();
                        }

                        if (modifier.Result is List<int>)
                            if (modifier.reference != null && Updater.TryGetObject(modifier.reference, out LevelObject levelObject) && levelObject.visualObject.GameObject)
                            {
                                var result = modifier.Result as List<int>;

                                var orderedList = PlayerManager.Players
                                    .Where(x => x.Player)
                                    .OrderBy(x => Vector2.Distance(x.Player.playerObjects["RB Parent"].gameObject.transform.position, levelObject.visualObject.GameObject.transform.position)).ToList();

                                if (orderedList.Count > 0)
                                {
                                    var closest = orderedList[0];

                                    var a = result.Count > closest.index && result[closest.index] > closest.Health;

                                    if (a)
                                    {
                                        result[closest.index] = closest.Health;
                                        modifier.Result = result;
                                    }

                                    return a;
                                }
                            }

                        break;
                    }
                case "inZenMode":
                    {
                        return PlayerManager.IsZenMode && (EditorManager.inst == null || RTPlayer.ZenModeInEditor);
                    }
                case "inNormal":
                    {
                        return PlayerManager.IsNormal;
                    }
                case "in1Life":
                    {
                        return PlayerManager.Is1Life;
                    }
                case "inNoHit":
                    {
                        return PlayerManager.IsNoHit;
                    }
                case "inPractice":
                    {
                        return PlayerManager.IsPractice;
                    }
                case "inEditor":
                    {
                        return EditorManager.inst != null;
                    }
                case "randomGreater":
                    {
                        if (modifier.Result == null)
                            modifier.Result = int.TryParse(modifier.commands[1], out int x) && int.TryParse(modifier.commands[2], out int y) && int.TryParse(modifier.value, out int z) && UnityEngine.Random.Range(x, y) > z;

                        return modifier.Result != null && (bool)modifier.Result;
                    }
                case "randomLesser":
                    {
                        if (modifier.Result == null)
                            modifier.Result = int.TryParse(modifier.commands[1], out int x) && int.TryParse(modifier.commands[2], out int y) && int.TryParse(modifier.value, out int z) && UnityEngine.Random.Range(x, y) < z;

                        return modifier.Result != null && (bool)modifier.Result;
                    }
                case "randomEquals":
                    {
                        if (modifier.Result == null)
                            modifier.Result = int.TryParse(modifier.commands[1], out int x) && int.TryParse(modifier.commands[2], out int y) && int.TryParse(modifier.value, out int z) && UnityEngine.Random.Range(x, y) == z;

                        return modifier.Result != null && (bool)modifier.Result;
                    }
                case "requireSignal":
                    {
                        return modifier.Result != null;
                    }
                case "musicTimeGreater":
                    {
                        return float.TryParse(modifier.value, out float x) && AudioManager.inst.CurrentAudioSource.time > x;
                    }
                case "musicTimeLesser":
                    {
                        return float.TryParse(modifier.value, out float x) && AudioManager.inst.CurrentAudioSource.time < x;
                    }
                case "musicPlaying":
                    {
                        return AudioManager.inst.CurrentAudioSource.isPlaying;
                    }
                case "axisEquals":
                    {
                        if (modifier.commands.Count < 11)
                        {
                            modifier.commands.Add("9999");
                        }

                        if (int.TryParse(modifier.commands[1], out int fromType) && int.TryParse(modifier.commands[2], out int fromAxis)
                            && float.TryParse(modifier.commands[3], out float delay) && float.TryParse(modifier.commands[4], out float multiply)
                            && float.TryParse(modifier.commands[5], out float offset) && float.TryParse(modifier.commands[6], out float min) && float.TryParse(modifier.commands[7], out float max)
                            && float.TryParse(modifier.commands[8], out float equals) && bool.TryParse(modifier.commands[9], out bool visual)
                            && float.TryParse(modifier.commands[10], out float loop)
                            && DataManager.inst.gameData.beatmapObjects.TryFind(x => (x as BeatmapObject).tags.Contains(modifier.value), out DataManager.GameData.BeatmapObject beatmapObject)
                            && beatmapObject != null)
                        {
                            var time = AudioManager.inst.CurrentAudioSource.time;

                            var bm = beatmapObject as BeatmapObject;

                            fromType = Mathf.Clamp(fromType, 0, bm.events.Count);
                            fromAxis = Mathf.Clamp(fromAxis, 0, bm.events[fromType][0].eventValues.Length);

                            if (!visual && Updater.levelProcessor.converter.cachedSequences.ContainsKey(bm.id))
                            {
                                // From Type Position
                                if (fromType == 0)
                                {
                                    var sequence = Updater.levelProcessor.converter.cachedSequences[bm.id].Position3DSequence.Interpolate(time - bm.StartTime - delay);
                                    float value = ((fromAxis == 0 ? sequence.x : fromAxis == 1 ? sequence.y : sequence.z) - offset) * multiply % loop;

                                    return Mathf.Clamp(value, min, max) == equals;
                                }

                                // From Type Scale
                                if (fromType == 1)
                                {
                                    var sequence = Updater.levelProcessor.converter.cachedSequences[bm.id].ScaleSequence.Interpolate(time - bm.StartTime - delay);
                                    float value = ((fromAxis == 0 ? sequence.x : sequence.y) - offset) * multiply % loop;

                                    return Mathf.Clamp(value, min, max) == equals;
                                }

                                // From Type Rotation
                                if (fromType == 2)
                                {
                                    var sequence = Updater.levelProcessor.converter.cachedSequences[bm.id].RotationSequence.Interpolate(time - bm.StartTime - delay);
                                    float value = (sequence - offset) * multiply % loop;

                                    return Mathf.Clamp(value, min, max) == equals;
                                }
                            }
                            else if (visual && Updater.TryGetObject(bm, out LevelObject levelObject) && levelObject.visualObject != null && levelObject.visualObject.GameObject)
                            {
                                var tf = levelObject.visualObject.GameObject.transform;

                                if (fromType == 0)
                                {
                                    return Mathf.Clamp(((fromAxis == 0 ? tf.position.x : fromAxis == 1 ? tf.position.y : tf.position.z) - offset) * multiply % loop, min, max) == equals;
                                }

                                if (fromType == 1)
                                {
                                    return Mathf.Clamp(((fromAxis == 0 ? tf.lossyScale.x : fromAxis == 1 ? tf.lossyScale.y : tf.lossyScale.z) - offset) * multiply % loop, min, max) == equals;
                                }

                                if (fromType == 2)
                                {
                                    return Mathf.Clamp(((fromAxis == 0 ? tf.rotation.eulerAngles.x : fromAxis == 1 ? tf.rotation.eulerAngles.y : tf.rotation.eulerAngles.z) - offset) * multiply % loop, min, max) == equals;
                                }
                            }
                        }

                        break;
                    }
                case "axisLesserEquals":
                    {
                        if (modifier.commands.Count < 11)
                        {
                            modifier.commands.Add("9999");
                        }

                        if (int.TryParse(modifier.commands[1], out int fromType) && int.TryParse(modifier.commands[2], out int fromAxis)
                            && float.TryParse(modifier.commands[3], out float delay) && float.TryParse(modifier.commands[4], out float multiply)
                            && float.TryParse(modifier.commands[5], out float offset) && float.TryParse(modifier.commands[6], out float min) && float.TryParse(modifier.commands[7], out float max)
                            && float.TryParse(modifier.commands[8], out float equals) && bool.TryParse(modifier.commands[9], out bool visual)
                            && float.TryParse(modifier.commands[10], out float loop)
                            && DataManager.inst.gameData.beatmapObjects.TryFind(x => (x as BeatmapObject).tags.Contains(modifier.value), out DataManager.GameData.BeatmapObject beatmapObject)
                            && beatmapObject != null)
                        {
                            var time = AudioManager.inst.CurrentAudioSource.time;

                            var bm = beatmapObject as BeatmapObject;

                            fromType = Mathf.Clamp(fromType, 0, bm.events.Count);
                            fromAxis = Mathf.Clamp(fromAxis, 0, bm.events[fromType][0].eventValues.Length);

                            if (!visual && Updater.levelProcessor.converter.cachedSequences.ContainsKey(bm.id))
                            {
                                // From Type Position
                                if (fromType == 0)
                                {
                                    var sequence = Updater.levelProcessor.converter.cachedSequences[bm.id].Position3DSequence.Interpolate(time - bm.StartTime - delay);
                                    float value = ((fromAxis == 0 ? sequence.x : fromAxis == 1 ? sequence.y : sequence.z) - offset) * multiply % loop;

                                    return Mathf.Clamp(value, min, max) <= equals;
                                }

                                // From Type Scale
                                if (fromType == 1)
                                {
                                    var sequence = Updater.levelProcessor.converter.cachedSequences[bm.id].ScaleSequence.Interpolate(time - bm.StartTime - delay);
                                    float value = ((fromAxis == 0 ? sequence.x : sequence.y) - offset) * multiply % loop;

                                    return Mathf.Clamp(value, min, max) <= equals;
                                }

                                // From Type Rotation
                                if (fromType == 2)
                                {
                                    var sequence = Updater.levelProcessor.converter.cachedSequences[bm.id].RotationSequence.Interpolate(time - bm.StartTime - delay);
                                    float value = (sequence - offset) * multiply % loop;

                                    return Mathf.Clamp(value, min, max) <= equals;
                                }
                            }
                            else if (visual && Updater.TryGetObject(bm, out LevelObject levelObject) && levelObject.visualObject != null && levelObject.visualObject.GameObject)
                            {
                                var tf = levelObject.visualObject.GameObject.transform;

                                if (fromType == 0)
                                {
                                    return Mathf.Clamp(((fromAxis == 0 ? tf.position.x : fromAxis == 1 ? tf.position.y : tf.position.z) - offset) * multiply % loop, min, max) <= equals;
                                }

                                if (fromType == 1)
                                {
                                    return Mathf.Clamp(((fromAxis == 0 ? tf.lossyScale.x : fromAxis == 1 ? tf.lossyScale.y : tf.lossyScale.z) - offset) * multiply % loop, min, max) <= equals;
                                }

                                if (fromType == 2)
                                {
                                    return Mathf.Clamp(((fromAxis == 0 ? tf.rotation.eulerAngles.x : fromAxis == 1 ? tf.rotation.eulerAngles.y : tf.rotation.eulerAngles.z) - offset) * multiply % loop, min, max) <= equals;
                                }
                            }
                        }

                        break;
                    }
                case "axisGreaterEquals":
                    {
                        if (modifier.commands.Count < 11)
                        {
                            modifier.commands.Add("9999");
                        }

                        if (int.TryParse(modifier.commands[1], out int fromType) && int.TryParse(modifier.commands[2], out int fromAxis)
                            && float.TryParse(modifier.commands[3], out float delay) && float.TryParse(modifier.commands[4], out float multiply)
                            && float.TryParse(modifier.commands[5], out float offset) && float.TryParse(modifier.commands[6], out float min) && float.TryParse(modifier.commands[7], out float max)
                            && float.TryParse(modifier.commands[8], out float equals) && bool.TryParse(modifier.commands[9], out bool visual)
                            && float.TryParse(modifier.commands[10], out float loop)
                            && DataManager.inst.gameData.beatmapObjects.TryFind(x => (x as BeatmapObject).tags.Contains(modifier.value), out DataManager.GameData.BeatmapObject beatmapObject)
                            && beatmapObject != null)
                        {
                            var time = AudioManager.inst.CurrentAudioSource.time;

                            var bm = beatmapObject as BeatmapObject;

                            fromType = Mathf.Clamp(fromType, 0, bm.events.Count);
                            fromAxis = Mathf.Clamp(fromAxis, 0, bm.events[fromType][0].eventValues.Length);
                            if (!visual && Updater.levelProcessor.converter.cachedSequences.ContainsKey(bm.id))
                            {
                                // From Type Position
                                if (fromType == 0)
                                {
                                    var sequence = Updater.levelProcessor.converter.cachedSequences[bm.id].Position3DSequence.Interpolate(time - bm.StartTime - delay);
                                    float value = ((fromAxis == 0 ? sequence.x : fromAxis == 1 ? sequence.y : sequence.z) - offset) * multiply % loop;

                                    return Mathf.Clamp(value, min, max) >= equals;
                                }

                                // From Type Scale
                                if (fromType == 1)
                                {
                                    var sequence = Updater.levelProcessor.converter.cachedSequences[bm.id].ScaleSequence.Interpolate(time - bm.StartTime - delay);
                                    float value = ((fromAxis == 0 ? sequence.x : sequence.y) - offset) * multiply % loop;

                                    return Mathf.Clamp(value, min, max) >= equals;
                                }

                                // From Type Rotation
                                if (fromType == 2)
                                {
                                    var sequence = Updater.levelProcessor.converter.cachedSequences[bm.id].RotationSequence.Interpolate(time - bm.StartTime - delay);
                                    float value = (sequence - offset) * multiply % loop;

                                    return Mathf.Clamp(value, min, max) >= equals;
                                }
                            }
                            else if (visual && Updater.TryGetObject(bm, out LevelObject levelObject) && levelObject.visualObject != null && levelObject.visualObject.GameObject)
                            {
                                var tf = levelObject.visualObject.GameObject.transform;

                                if (fromType == 0)
                                {
                                    return Mathf.Clamp(((fromAxis == 0 ? tf.position.x : fromAxis == 1 ? tf.position.y : tf.position.z) - offset) * multiply % loop, min, max) >= equals;
                                }

                                if (fromType == 1)
                                {
                                    return Mathf.Clamp(((fromAxis == 0 ? tf.lossyScale.x : fromAxis == 1 ? tf.lossyScale.y : tf.lossyScale.z) - offset) * multiply % loop, min, max) >= equals;
                                }

                                if (fromType == 2)
                                {
                                    return Mathf.Clamp(((fromAxis == 0 ? tf.rotation.eulerAngles.x : fromAxis == 1 ? tf.rotation.eulerAngles.y : tf.rotation.eulerAngles.z) - offset) * multiply % loop, min, max) >= equals;
                                }
                            }
                        }

                        break;
                    }
                case "axisLesser":
                    {
                        if (modifier.commands.Count < 11)
                        {
                            modifier.commands.Add("9999");
                        }

                        if (int.TryParse(modifier.commands[1], out int fromType) && int.TryParse(modifier.commands[2], out int fromAxis)
                            && float.TryParse(modifier.commands[3], out float delay) && float.TryParse(modifier.commands[4], out float multiply)
                            && float.TryParse(modifier.commands[5], out float offset) && float.TryParse(modifier.commands[6], out float min) && float.TryParse(modifier.commands[7], out float max)
                            && float.TryParse(modifier.commands[8], out float equals) && bool.TryParse(modifier.commands[9], out bool visual)
                            && float.TryParse(modifier.commands[10], out float loop)
                            && DataManager.inst.gameData.beatmapObjects.TryFind(x => (x as BeatmapObject).tags.Contains(modifier.value), out DataManager.GameData.BeatmapObject beatmapObject)
                            && beatmapObject != null)
                        {
                            var time = AudioManager.inst.CurrentAudioSource.time;

                            var bm = beatmapObject as BeatmapObject;

                            fromType = Mathf.Clamp(fromType, 0, bm.events.Count);
                            fromAxis = Mathf.Clamp(fromAxis, 0, bm.events[fromType][0].eventValues.Length);
                            if (!visual && Updater.levelProcessor.converter.cachedSequences.ContainsKey(bm.id))
                            {
                                // From Type Position
                                if (fromType == 0)
                                {
                                    var sequence = Updater.levelProcessor.converter.cachedSequences[bm.id].Position3DSequence.Interpolate(time - bm.StartTime - delay);
                                    float value = ((fromAxis == 0 ? sequence.x : fromAxis == 1 ? sequence.y : sequence.z) - offset) * multiply % loop;

                                    return Mathf.Clamp(value, min, max) < equals;
                                }

                                // From Type Scale
                                if (fromType == 1)
                                {
                                    var sequence = Updater.levelProcessor.converter.cachedSequences[bm.id].ScaleSequence.Interpolate(time - bm.StartTime - delay);
                                    float value = ((fromAxis == 0 ? sequence.x : sequence.y) - offset) * multiply % loop;

                                    return Mathf.Clamp(value, min, max) < equals;
                                }

                                // From Type Rotation
                                if (fromType == 2)
                                {
                                    var sequence = Updater.levelProcessor.converter.cachedSequences[bm.id].RotationSequence.Interpolate(time - bm.StartTime - delay);
                                    float value = (sequence - offset) * multiply % loop;

                                    return Mathf.Clamp(value, min, max) < equals;
                                }
                            }
                            else if (visual && Updater.TryGetObject(bm, out LevelObject levelObject) && levelObject.visualObject != null && levelObject.visualObject.GameObject)
                            {
                                var tf = levelObject.visualObject.GameObject.transform;

                                if (fromType == 0)
                                {
                                    return Mathf.Clamp(((fromAxis == 0 ? tf.position.x : fromAxis == 1 ? tf.position.y : tf.position.z) - offset) * multiply % loop, min, max) < equals;
                                }

                                if (fromType == 1)
                                {
                                    return Mathf.Clamp(((fromAxis == 0 ? tf.lossyScale.x : fromAxis == 1 ? tf.lossyScale.y : tf.lossyScale.z) - offset) * multiply % loop, min, max) < equals;
                                }

                                if (fromType == 2)
                                {
                                    return Mathf.Clamp(((fromAxis == 0 ? tf.rotation.eulerAngles.x : fromAxis == 1 ? tf.rotation.eulerAngles.y : tf.rotation.eulerAngles.z) - offset) * multiply % loop, min, max) < equals;
                                }
                            }
                        }

                        break;
                    }
                case "axisGreater":
                    {
                        if (modifier.commands.Count < 11)
                        {
                            modifier.commands.Add("9999");
                        }

                        if (int.TryParse(modifier.commands[1], out int fromType) && int.TryParse(modifier.commands[2], out int fromAxis)
                            && float.TryParse(modifier.commands[3], out float delay) && float.TryParse(modifier.commands[4], out float multiply)
                            && float.TryParse(modifier.commands[5], out float offset) && float.TryParse(modifier.commands[6], out float min) && float.TryParse(modifier.commands[7], out float max)
                            && float.TryParse(modifier.commands[8], out float equals) && bool.TryParse(modifier.commands[9], out bool visual)
                            && float.TryParse(modifier.commands[10], out float loop)
                            && DataManager.inst.gameData.beatmapObjects.TryFind(x => (x as BeatmapObject).tags.Contains(modifier.value), out DataManager.GameData.BeatmapObject beatmapObject)
                            && beatmapObject != null)
                        {
                            var time = AudioManager.inst.CurrentAudioSource.time;

                            var bm = beatmapObject as BeatmapObject;

                            fromType = Mathf.Clamp(fromType, 0, bm.events.Count);
                            fromAxis = Mathf.Clamp(fromAxis, 0, bm.events[fromType][0].eventValues.Length);
                            if (!visual && Updater.levelProcessor.converter.cachedSequences.ContainsKey(bm.id))
                            {
                                // From Type Position
                                if (fromType == 0)
                                {
                                    var sequence = Updater.levelProcessor.converter.cachedSequences[bm.id].Position3DSequence.Interpolate(time - bm.StartTime - delay);
                                    float value = ((fromAxis == 0 ? sequence.x : fromAxis == 1 ? sequence.y : sequence.z) - offset) * multiply % loop;

                                    return Mathf.Clamp(value, min, max) > equals;
                                }

                                // From Type Scale
                                if (fromType == 1)
                                {
                                    var sequence = Updater.levelProcessor.converter.cachedSequences[bm.id].ScaleSequence.Interpolate(time - bm.StartTime - delay);
                                    float value = ((fromAxis == 0 ? sequence.x : sequence.y) - offset) * multiply % loop;

                                    return Mathf.Clamp(value, min, max) > equals;
                                }

                                // From Type Rotation
                                if (fromType == 2)
                                {
                                    var sequence = Updater.levelProcessor.converter.cachedSequences[bm.id].RotationSequence.Interpolate(time - bm.StartTime - delay);
                                    float value = (sequence - offset) * multiply % loop;

                                    return Mathf.Clamp(value, min, max) > equals;
                                }
                            }
                            else if (visual && Updater.TryGetObject(bm, out LevelObject levelObject) && levelObject.visualObject != null && levelObject.visualObject.GameObject)
                            {
                                var tf = levelObject.visualObject.GameObject.transform;

                                if (fromType == 0)
                                {
                                    return Mathf.Clamp(((fromAxis == 0 ? tf.position.x : fromAxis == 1 ? tf.position.y : tf.position.z) - offset) * multiply % loop, min, max) > equals;
                                }

                                if (fromType == 1)
                                {
                                    return Mathf.Clamp(((fromAxis == 0 ? tf.lossyScale.x : fromAxis == 1 ? tf.lossyScale.y : tf.lossyScale.z) - offset) * multiply % loop, min, max) > equals;
                                }

                                if (fromType == 2)
                                {
                                    return Mathf.Clamp(((fromAxis == 0 ? tf.rotation.eulerAngles.x : fromAxis == 1 ? tf.rotation.eulerAngles.y : tf.rotation.eulerAngles.z) - offset) * multiply % loop, min, max) > equals;
                                }
                            }
                        }

                        break;
                    }
                case "playerBoostEquals":
                    {
                        return int.TryParse(modifier.value, out int num) && LevelManager.BoostCount == num;
                    }
                case "playerBoostLesserEquals":
                    {
                        return int.TryParse(modifier.value, out int num) && LevelManager.BoostCount <= num;
                    }
                case "playerBoostGreaterEquals":
                    {
                        return int.TryParse(modifier.value, out int num) && LevelManager.BoostCount >= num;
                    }
                case "playerBoostLesser":
                    {
                        return int.TryParse(modifier.value, out int num) && LevelManager.BoostCount < num;
                    }
                case "playerBoostGreater":
                    {
                        return int.TryParse(modifier.value, out int num) && LevelManager.BoostCount > num;
                    }
                case "levelRankEquals":
                    {
                        if (LevelManager.CurrentLevel != null && LevelManager.CurrentLevel.playerData != null && int.TryParse(modifier.value, out int num))
                        {
                            var levelRank = LevelManager.GetLevelRank(LevelManager.CurrentLevel);

                            var levelRankIndexes = new Dictionary<string, int>
                            {
                                { "-", 0 },
                                { "SS", 1 },
                                { "S", 2 },
                                { "A", 3 },
                                { "B", 4 },
                                { "C", 5 },
                                { "D", 6 },
                                { "F", 7 },
                            };

                            return levelRankIndexes[levelRank.name] == num;
                        }

                        break;
                    }
                case "levelRankLesserEquals":
                    {
                        if (LevelManager.CurrentLevel != null && LevelManager.CurrentLevel.playerData != null && int.TryParse(modifier.value, out int num))
                        {
                            var levelRank = LevelManager.GetLevelRank(LevelManager.CurrentLevel);

                            var levelRankIndexes = new Dictionary<string, int>
                            {
                                { "-", 0 },
                                { "SS", 1 },
                                { "S", 2 },
                                { "A", 3 },
                                { "B", 4 },
                                { "C", 5 },
                                { "D", 6 },
                                { "F", 7 },
                            };

                            return levelRankIndexes[levelRank.name] <= num;
                        }

                        break;
                    }
                case "levelRankGreaterEquals":
                    {
                        if (LevelManager.CurrentLevel != null && LevelManager.CurrentLevel.playerData != null && int.TryParse(modifier.value, out int num))
                        {
                            var levelRank = LevelManager.GetLevelRank(LevelManager.CurrentLevel);

                            var levelRankIndexes = new Dictionary<string, int>
                            {
                                { "-", 0 },
                                { "SS", 1 },
                                { "S", 2 },
                                { "A", 3 },
                                { "B", 4 },
                                { "C", 5 },
                                { "D", 6 },
                                { "F", 7 },
                            };

                            return levelRankIndexes[levelRank.name] >= num;
                        }

                        break;
                    }
                case "levelRankLesser":
                    {
                        if (LevelManager.CurrentLevel != null && LevelManager.CurrentLevel.playerData != null && int.TryParse(modifier.value, out int num))
                        {
                            var levelRank = LevelManager.GetLevelRank(LevelManager.CurrentLevel);

                            var levelRankIndexes = new Dictionary<string, int>
                            {
                                { "-", 0 },
                                { "SS", 1 },
                                { "S", 2 },
                                { "A", 3 },
                                { "B", 4 },
                                { "C", 5 },
                                { "D", 6 },
                                { "F", 7 },
                            };

                            return levelRankIndexes[levelRank.name] < num;
                        }

                        break;
                    }
                case "levelRankGreater":
                    {
                        if (LevelManager.CurrentLevel != null && LevelManager.CurrentLevel.playerData != null && int.TryParse(modifier.value, out int num))
                        {
                            var levelRank = LevelManager.GetLevelRank(LevelManager.CurrentLevel);

                            var levelRankIndexes = new Dictionary<string, int>
                            {
                                { "-", 0 },
                                { "SS", 1 },
                                { "S", 2 },
                                { "A", 3 },
                                { "B", 4 },
                                { "C", 5 },
                                { "D", 6 },
                                { "F", 7 },
                            };

                            return levelRankIndexes[levelRank.name] > num;
                        }

                        break;
                    }
                case "isFullscreen":
                    {
                        return Screen.fullScreen;
                    }
                case "realTimeSecondEquals":
                    {
                        if (int.TryParse(modifier.value, out int num))
                            return Parser.TryParse(DateTime.Now.ToString("ss"), 0) == num;
                        break;
                    }
                case "realTimeSecondLesserEquals":
                    {
                        if (int.TryParse(modifier.value, out int num))
                            return Parser.TryParse(DateTime.Now.ToString("ss"), 0) <= num;
                        break;
                    }
                case "realTimeSecondGreaterEquals":
                    {
                        if (int.TryParse(modifier.value, out int num))
                            return Parser.TryParse(DateTime.Now.ToString("ss"), 0) >= num;
                        break;
                    }
                case "realTimeSecondLesser":
                    {
                        if (int.TryParse(modifier.value, out int num))
                            return Parser.TryParse(DateTime.Now.ToString("ss"), 0) < num;
                        break;
                    }
                case "realTimeSecondGreater":
                    {
                        if (int.TryParse(modifier.value, out int num))
                            return Parser.TryParse(DateTime.Now.ToString("ss"), 0) > num;
                        break;
                    }
                case "realTimeMinuteEquals":
                    {
                        if (int.TryParse(modifier.value, out int num))
                            return Parser.TryParse(DateTime.Now.ToString("mm"), 0) == num;
                        break;
                    }
                case "realTimeMinuteLesserEquals":
                    {
                        if (int.TryParse(modifier.value, out int num))
                            return Parser.TryParse(DateTime.Now.ToString("mm"), 0) <= num;
                        break;
                    }
                case "realTimeMinuteGreaterEquals":
                    {
                        if (int.TryParse(modifier.value, out int num))
                            return Parser.TryParse(DateTime.Now.ToString("mm"), 0) >= num;
                        break;
                    }
                case "realTimeMinuteLesser":
                    {
                        if (int.TryParse(modifier.value, out int num))
                            return Parser.TryParse(DateTime.Now.ToString("mm"), 0) < num;
                        break;
                    }
                case "realTimeMinuteGreater":
                    {
                        if (int.TryParse(modifier.value, out int num))
                            return Parser.TryParse(DateTime.Now.ToString("mm"), 0) > num;
                        break;
                    }
                case "realTime24HourEquals":
                    {
                        if (int.TryParse(modifier.value, out int num))
                            return Parser.TryParse(DateTime.Now.ToString("HH"), 0) == num;
                        break;
                    }
                case "realTime24HourLesserEquals":
                    {
                        if (int.TryParse(modifier.value, out int num))
                            return Parser.TryParse(DateTime.Now.ToString("HH"), 0) <= num;
                        break;
                    }
                case "realTime24HourGreaterEquals":
                    {
                        if (int.TryParse(modifier.value, out int num))
                            return Parser.TryParse(DateTime.Now.ToString("HH"), 0) >= num;
                        break;
                    }
                case "realTime24HourLesser":
                    {
                        if (int.TryParse(modifier.value, out int num))
                            return Parser.TryParse(DateTime.Now.ToString("HH"), 0) < num;
                        break;
                    }
                case "realTime24HourGreater":
                    {
                        if (int.TryParse(modifier.value, out int num))
                            return Parser.TryParse(DateTime.Now.ToString("HH"), 0) > num;
                        break;
                    }
                case "realTime12HourEquals":
                    {
                        if (int.TryParse(modifier.value, out int num))
                            return Parser.TryParse(DateTime.Now.ToString("hh"), 0) == num;
                        break;
                    }
                case "realTime12HourLesserEquals":
                    {
                        if (int.TryParse(modifier.value, out int num))
                            return Parser.TryParse(DateTime.Now.ToString("hh"), 0) <= num;
                        break;
                    }
                case "realTime12HourGreaterEquals":
                    {
                        if (int.TryParse(modifier.value, out int num))
                            return Parser.TryParse(DateTime.Now.ToString("hh"), 0) >= num;
                        break;
                    }
                case "realTime12HourLesser":
                    {
                        if (int.TryParse(modifier.value, out int num))
                            return Parser.TryParse(DateTime.Now.ToString("hh"), 0) < num;
                        break;
                    }
                case "realTime12HourGreater":
                    {
                        if (int.TryParse(modifier.value, out int num))
                            return Parser.TryParse(DateTime.Now.ToString("hh"), 0) > num;
                        break;
                    }
                case "realTimeDayEquals":
                    {
                        if (int.TryParse(modifier.value, out int num))
                            return Parser.TryParse(DateTime.Now.ToString("dd"), 0) == num;
                        break;
                    }
                case "realTimeDayLesserEquals":
                    {
                        if (int.TryParse(modifier.value, out int num))
                            return Parser.TryParse(DateTime.Now.ToString("dd"), 0) <= num;
                        break;
                    }
                case "realTimeDayGreaterEquals":
                    {
                        if (int.TryParse(modifier.value, out int num))
                            return Parser.TryParse(DateTime.Now.ToString("dd"), 0) >= num;
                        break;
                    }
                case "realTimeDayLesser":
                    {
                        if (int.TryParse(modifier.value, out int num))
                            return Parser.TryParse(DateTime.Now.ToString("dd"), 0) < num;
                        break;
                    }
                case "realTimeDayGreater":
                    {
                        if (int.TryParse(modifier.value, out int num))
                            return Parser.TryParse(DateTime.Now.ToString("dd"), 0) > num;
                        break;
                    }
                case "realTimeDayWeekEquals":
                    {
                        return DateTime.Now.ToString("dddd") == modifier.value;
                    }
                case "realTimeMonthEquals":
                    {
                        if (int.TryParse(modifier.value, out int num))
                            return Parser.TryParse(DateTime.Now.ToString("MM"), 0) == num;
                        break;
                    }
                case "realTimeMonthLesserEquals":
                    {
                        if (int.TryParse(modifier.value, out int num))
                            return Parser.TryParse(DateTime.Now.ToString("MM"), 0) <= num;
                        break;
                    }
                case "realTimeMonthGreaterEquals":
                    {
                        if (int.TryParse(modifier.value, out int num))
                            return Parser.TryParse(DateTime.Now.ToString("MM"), 0) >= num;
                        break;
                    }
                case "realTimeMonthLesser":
                    {
                        if (int.TryParse(modifier.value, out int num))
                            return Parser.TryParse(DateTime.Now.ToString("MM"), 0) < num;
                        break;
                    }
                case "realTimeMonthGreater":
                    {
                        if (int.TryParse(modifier.value, out int num))
                            return Parser.TryParse(DateTime.Now.ToString("MM"), 0) > num;
                        break;
                    }
                case "realTimeYearEquals":
                    {
                        if (int.TryParse(modifier.value, out int num))
                            return Parser.TryParse(DateTime.Now.ToString("yyyy"), 0) == num;
                        break;
                    }
                case "realTimeYearLesserEquals":
                    {
                        if (int.TryParse(modifier.value, out int num))
                            return Parser.TryParse(DateTime.Now.ToString("yyyy"), 0) <= num;
                        break;
                    }
                case "realTimeYearGreaterEquals":
                    {
                        if (int.TryParse(modifier.value, out int num))
                            return Parser.TryParse(DateTime.Now.ToString("yyyy"), 0) >= num;
                        break;
                    }
                case "realTimeYearLesser":
                    {
                        if (int.TryParse(modifier.value, out int num))
                            return Parser.TryParse(DateTime.Now.ToString("yyyy"), 0) < num;
                        break;
                    }
                case "realTimeYearGreater":
                    {
                        if (int.TryParse(modifier.value, out int num))
                            return Parser.TryParse(DateTime.Now.ToString("yyyy"), 0) > num;
                        break;
                    }
            }

            modifier.Inactive?.Invoke(modifier);
            return false;
        }

        public static void Action(Modifier<BeatmapObject> modifier)
        {
            modifier.hasChanged = false;

            if (!modifier.verified)
            {
                modifier.verified = true;
                modifier.VerifyModifier(ModifiersManager.modifierTypes);
            }

            if (!modifier.IsValid(ModifiersManager.modifierTypes))
                return;

            switch (modifier.commands[0])
            {
                case "setPitch":
                    {
                        if (float.TryParse(modifier.value, out float num))
                        {
                            RTEventManager.inst.pitchOffset = num;
                        }

                        break;
                    }
                case "addPitch":
                    {
                        if (float.TryParse(modifier.value, out float num))
                        {
                            RTEventManager.inst.pitchOffset += num;
                        }

                        break;
                    }
                case "setMusicTime":
                    {
                        if (float.TryParse(modifier.value, out float num))
                            AudioManager.inst.SetMusicTime(num);
                        break;
                    }
                case "setMusicTimeStartTime":
                    {
                        AudioManager.inst.SetMusicTime(modifier.reference.StartTime);
                        break;
                    }
                case "setMusicTimeAutokill":
                    {
                        AudioManager.inst.SetMusicTime(modifier.reference.StartTime + modifier.reference.GetObjectLifeLength(_oldStyle: true));
                        break;
                    }
                case "playSound":
                    {
                        if (bool.TryParse(modifier.commands[1], out bool global) && float.TryParse(modifier.commands[2], out float pitch) && float.TryParse(modifier.commands[3], out float vol) && bool.TryParse(modifier.commands[4], out bool loop))
                            ModifiersManager.GetSoundPath(modifier.reference.id, modifier.value, global, pitch, vol, loop);

                        break;
                    }
                case "playSoundOnline":
                    {
                        if (bool.TryParse(modifier.commands[1], out bool global) && float.TryParse(modifier.commands[2], out float pitch) && float.TryParse(modifier.commands[3], out float vol) && bool.TryParse(modifier.commands[4], out bool loop) && !string.IsNullOrEmpty(modifier.value))
                            ModifiersManager.DownloadSoundAndPlay(modifier.reference.id, modifier.value, pitch, vol, loop);

                        break;
                    }
                case "audioSource":
                    {
                        if (Updater.TryGetObject(modifier.reference, out LevelObject levelObject) && levelObject.visualObject != null &&
                            levelObject.visualObject.GameObject != null && bool.TryParse(modifier.commands[1], out bool global))
                        {
                            if (modifier.Result != null)
                                break;

                            string text = RTFile.ApplicationDirectory + "beatmaps/soundlibrary/" + modifier.value;

                            if (!global)
                                text = RTFile.BasePath + modifier.value;

                            if (!modifier.value.Contains(".ogg") && RTFile.FileExists(text + ".ogg"))
                                text += ".ogg";

                            if (!modifier.value.Contains(".wav") && RTFile.FileExists(text + ".wav"))
                                text += ".wav";

                            if (!modifier.value.Contains(".mp3") && RTFile.FileExists(text + ".mp3"))
                                text += ".mp3";

                            if (!RTFile.FileExists(text))
                                break;

                            if (text.Contains(".mp3"))
                            {
                                modifier.Result = levelObject.visualObject.GameObject.AddComponent<AudioModifier>();
                                ((AudioModifier)modifier.Result).Init(LSAudio.CreateAudioClipUsingMP3File(text), modifier.reference, modifier);
                                break;
                            }

                            CoreHelper.StartCoroutine(ModifiersManager.LoadMusicFileRaw(text, delegate (AudioClip audioClip)
                            {
                                audioClip.name = modifier.value;
                                modifier.Result = levelObject.visualObject.GameObject.AddComponent<AudioModifier>();
                                ((AudioModifier)modifier.Result).Init(audioClip, modifier.reference, modifier);
                            }));
                        }

                        break;
                    }
                case "loadLevel":
                    {
                        if (EditorManager.inst && EditorManager.inst.isEditing)
                        {
                            if (ModifiersConfig.Instance.EditorLoadLevel.Value)
                            {
                                RTEditor.inst.ShowWarningPopup($"You are about to enter the level {modifier.value}, are you sure you want to continue? Any unsaved progress will be lost!", delegate ()
                                {
                                    string str = RTFile.BasePath;
                                    if (ModifiersConfig.Instance.EditorSavesBeforeLoad.Value)
                                    {
                                        CoreHelper.StartCoroutine(ProjectData.Writer.SaveData(str + "level-modifier-backup.lsb", GameData.Current, delegate ()
                                        {
                                            EditorManager.inst.DisplayNotification($"Saved backup to {System.IO.Path.GetFileName(System.IO.Path.GetDirectoryName(str))}", 2f, EditorManager.NotificationType.Success);
                                        }));
                                    }

                                    EditorManager.inst.StartCoroutine(EditorManager.inst.LoadLevel(modifier.value));
                                }, delegate ()
                                {
                                    EditorManager.inst.HideDialog("Warning Popup");
                                }, "Yes", "No");
                            }
                        }
                        else if (!EditorManager.inst)
                        {
                            if (RTFile.FileExists($"{RTFile.ApplicationDirectory}{LevelManager.ListSlash}{modifier.value}/level.lsb"))
                                LevelManager.Load($"{RTFile.ApplicationDirectory}{LevelManager.ListSlash}{modifier.value}/level.lsb");
                            if (RTFile.FileExists($"{RTFile.ApplicationDirectory}{LevelManager.ListSlash}{modifier.value}/level.vgd"))
                                LevelManager.Load($"{RTFile.ApplicationDirectory}{LevelManager.ListSlash}{modifier.value}/level.vgd");
                        }
                        break;
                    }
                case "loadLevelID":
                    {
                        if (modifier.value == "0" || modifier.value == "-1")
                            break;

                        if (EditorManager.inst && EditorManager.inst.isEditing && EditorManager.inst.loadedLevels.Has(x => x.metadata is MetaData metaData && metaData.ID == modifier.value))
                        {
                            if (ModifiersConfig.Instance.EditorLoadLevel.Value)
                            {
                                var path = System.IO.Path.GetFileName(EditorManager.inst.loadedLevels.Find(x => x.metadata is MetaData metaData && metaData.ID == modifier.value).folder);

                                RTEditor.inst.ShowWarningPopup($"You are about to enter the level {path}, are you sure you want to continue? Any unsaved progress will be lost!", delegate ()
                                    {
                                        string str = RTFile.BasePath;
                                        if (ModifiersConfig.Instance.EditorSavesBeforeLoad.Value)
                                        {
                                            CoreHelper.StartCoroutine(ProjectData.Writer.SaveData(str + "level-modifier-backup.lsb", GameData.Current, delegate ()
                                            {
                                                EditorManager.inst.DisplayNotification($"Saved backup to {System.IO.Path.GetFileName(System.IO.Path.GetDirectoryName(str))}", 2f, EditorManager.NotificationType.Success);
                                            }));
                                        }

                                        EditorManager.inst.StartCoroutine(EditorManager.inst.LoadLevel(path));
                                    }, delegate ()
                                    {
                                        EditorManager.inst.HideDialog("Warning Popup");
                                    }, "Yes", "No");
                            }
                        }
                        else if (!EditorManager.inst && LevelManager.Levels.Has(x => x.id == modifier.value))
                        {
                            var level = LevelManager.Levels.Find(x => x.id == modifier.value);

                            CoreHelper.StartCoroutine(LevelManager.Play(level));
                        }
                        break;
                    }
                case "loadLevelInternal":
                    {
                        if (EditorManager.inst && EditorManager.inst.isEditing && RTFile.FileExists($"{RTFile.BasePath}{EditorManager.inst.currentLoadedLevel}/{modifier.value}/level.lsb"))
                        {
                            if (ModifiersConfig.Instance.EditorLoadLevel.Value)
                            {
                                RTEditor.inst.ShowWarningPopup($"You are about to enter the level {EditorManager.inst.currentLoadedLevel}/{modifier.value}, are you sure you want to continue? Any unsaved progress will be lost!", delegate ()
                                    {
                                        string str = RTFile.BasePath;
                                        if (ModifiersConfig.Instance.EditorSavesBeforeLoad.Value)
                                        {
                                            CoreHelper.StartCoroutine(ProjectData.Writer.SaveData(str + "level-modifier-backup.lsb", GameData.Current, delegate ()
                                            {
                                                EditorManager.inst.DisplayNotification($"Saved backup to {System.IO.Path.GetFileName(System.IO.Path.GetDirectoryName(str))}", 2f, EditorManager.NotificationType.Success);
                                            }));
                                        }

                                        EditorManager.inst.StartCoroutine(EditorManager.inst.LoadLevel($"{EditorManager.inst.currentLoadedLevel}/{modifier.value}"));
                                    }, delegate ()
                                    {
                                        EditorManager.inst.HideDialog("Warning Popup");
                                    }, "Yes", "No");
                            }
                        }
                        else if (!EditorManager.inst && RTFile.FileExists($"{RTFile.ApplicationDirectory}{LevelManager.ListSlash}{System.IO.Path.GetFileName(GameManager.inst.basePath.Substring(0, GameManager.inst.basePath.Length - 1))}/{modifier.value}/level.lsb"))
                        {
                            LevelManager.Load($"{RTFile.ApplicationDirectory}{LevelManager.ListSlash}{System.IO.Path.GetFileName(GameManager.inst.basePath.Substring(0, GameManager.inst.basePath.Length - 1))}/{modifier.value}/level.lsb");
                        }
                        break;
                    }
                case "quitToMenu":
                    {
                        if (EditorManager.inst != null && !EditorManager.inst.isEditing)
                        {
                            if (ModifiersConfig.Instance.EditorLoadLevel.Value)
                            {
                                string str = RTFile.BasePath;
                                if (ModifiersConfig.Instance.EditorSavesBeforeLoad.Value)
                                {
                                    CoreHelper.StartCoroutine(ProjectData.Writer.SaveData(str + "level-modifier-backup.lsb", GameData.Current, delegate ()
                                    {
                                        EditorManager.inst.DisplayNotification($"Saved backup to {System.IO.Path.GetFileName(System.IO.Path.GetDirectoryName(str))}", 2f, EditorManager.NotificationType.Success);
                                    }));
                                }

                                EditorManager.inst.QuitToMenu();
                            }
                        }
                        else if (EditorManager.inst == null)
                        {
                            DOTween.KillAll();
                            DOTween.Clear(true);
                            DataManager.inst.gameData = null;
                            DataManager.inst.gameData = new GameData();
                            DiscordController.inst.OnIconChange("");
                            DiscordController.inst.OnStateChange("");
                            CoreHelper.Log($"Quit to Main Menu");
                            InputDataManager.inst.players.Clear();
                            SceneManager.inst.LoadScene("Main Menu");
                        }
                        break;
                    }
                case "quitToArcade":
                    {
                        if (EditorManager.inst != null && !EditorManager.inst.isEditing)
                        {
                            if (ModifiersConfig.Instance.EditorLoadLevel.Value)
                            {
                                string str = RTFile.BasePath;
                                if (ModifiersConfig.Instance.EditorSavesBeforeLoad.Value)
                                {
                                    CoreHelper.StartCoroutine(ProjectData.Writer.SaveData(str + "level-modifier-backup.lsb", GameData.Current, delegate ()
                                    {
                                        EditorManager.inst.DisplayNotification($"Saved backup to {System.IO.Path.GetFileName(System.IO.Path.GetDirectoryName(str))}", 2f, EditorManager.NotificationType.Success);
                                    }));
                                }

                                GameManager.inst.QuitToArcade();
                            }
                        }
                        else if (EditorManager.inst == null)
                        {
                            GameManager.inst.QuitToArcade();
                        }
                        break;
                    }
                case "blur":
                    {
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
                case "blurOther":
                    {
                        var list = GameData.Current.BeatmapObjects.Where(x => x.tags.Contains(modifier.commands[1]));

                        if (list.Count() > 0 && float.TryParse(modifier.value, out float num))
                        {
                            foreach (var beatmapObject in list)
                            {
                                if (beatmapObject.objectType != BeatmapObject.ObjectType.Empty &&
                                    Updater.TryGetObject(beatmapObject, out LevelObject levelObject) &&
                                    levelObject.visualObject.Renderer)
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
                                    rend.material.SetFloat("_blurSizeXY", -(beatmapObject.Interpolate(3, 1) - 1f) * num);
                                }
                            }
                        }

                        break;
                    }
                case "blurVariable":
                    {
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
                case "blurVariableOther":
                    {
                        var list = GameData.Current.BeatmapObjects.Where(x => x.tags.Contains(modifier.commands[1]));

                        if (list.Count() > 0 && float.TryParse(modifier.value, out float num))
                        {
                            foreach (var beatmapObject in list)
                            {
                                if (beatmapObject.objectType != BeatmapObject.ObjectType.Empty &&
                                    Updater.TryGetObject(beatmapObject, out LevelObject levelObject) &&
                                    levelObject.visualObject.Renderer)
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
                                    rend.material.SetFloat("_blurSizeXY", beatmapObject.integerVariable * num);
                                }
                            }
                        }
                        break;
                    }
                case "blurColored":
                    {
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
                case "blurColoredOther":
                    {
                        var list = GameData.Current.BeatmapObjects.Where(x => x.tags.Contains(modifier.commands[1]));

                        if (list.Count() > 0 && float.TryParse(modifier.value, out float num))
                        {
                            foreach (var beatmapObject in list)
                            {
                                if (beatmapObject.objectType != BeatmapObject.ObjectType.Empty &&
                                    Updater.TryGetObject(beatmapObject, out LevelObject levelObject) &&
                                    levelObject.visualObject.Renderer)
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
                                    rend.material.SetFloat("_Size", -(beatmapObject.Interpolate(3, 1) - 1f) * num);
                                }
                            }
                        }

                        break;
                    }
                case "particleSystem":
                    {
                        if (modifier.reference != null && Updater.TryGetObject(modifier.reference, out LevelObject levelObject) && levelObject.visualObject.GameObject)
                        {
                            var mod = levelObject.visualObject.GameObject;

                            if (!modifier.reference.particleSystem && !mod.GetComponent<ParticleSystem>())
                            {
                                modifier.reference.particleSystem = mod.AddComponent<ParticleSystem>();
                                var ps = modifier.reference.particleSystem;

                                var mat = mod.GetComponent<ParticleSystemRenderer>();
                                mat.material = GameManager.inst.PlayerPrefabs[0].transform.GetChild(0).GetChild(0).GetComponent<TrailRenderer>().material;
                                mat.material.color = Color.white;
                                mat.trailMaterial = mat.material;
                                mat.renderMode = ParticleSystemRenderMode.Mesh;

                                var s = int.Parse(modifier.commands[1]);
                                var so = int.Parse(modifier.commands[2]);

                                s = Mathf.Clamp(s, 0, ObjectManager.inst.objectPrefabs.Count - 1);
                                so = Mathf.Clamp(so, 0, ObjectManager.inst.objectPrefabs[s].options.Count - 1);

                                mat.mesh = ObjectManager.inst.objectPrefabs[s == 4 ? 0 : s == 6 ? 0 : s].options[so].GetComponentInChildren<MeshFilter>().mesh;

                                var psMain = ps.main;
                                var psEmission = ps.emission;

                                psMain.simulationSpace = ParticleSystemSimulationSpace.World;

                                psMain.startSpeed = float.Parse(modifier.commands[9]);

                                if (modifier.constant)
                                    ps.emissionRate = float.Parse(modifier.commands[10]);
                                else
                                {
                                    ps.emissionRate = 0f;
                                    psMain.loop = false;
                                    psEmission.burstCount = (int)float.Parse(modifier.commands[10]);
                                    psMain.duration = float.Parse(modifier.commands[11]);
                                }

                                var rotationOverLifetime = ps.rotationOverLifetime;
                                rotationOverLifetime.enabled = true;
                                rotationOverLifetime.separateAxes = true;
                                rotationOverLifetime.xMultiplier = 0f;
                                rotationOverLifetime.yMultiplier = 0f;
                                rotationOverLifetime.zMultiplier = float.Parse(modifier.commands[8]);

                                var forceOverLifetime = ps.forceOverLifetime;
                                forceOverLifetime.enabled = true;
                                forceOverLifetime.space = ParticleSystemSimulationSpace.World;
                                forceOverLifetime.xMultiplier = float.Parse(modifier.commands[12]);
                                forceOverLifetime.yMultiplier = float.Parse(modifier.commands[13]);

                                var particlesTrail = ps.trails;
                                particlesTrail.enabled = bool.Parse(modifier.commands[14]);

                                var colorOverLifetime = ps.colorOverLifetime;
                                colorOverLifetime.enabled = true;
                                var psCol = colorOverLifetime.color;

                                float alphaStart = float.Parse(modifier.commands[4]);
                                float alphaEnd = float.Parse(modifier.commands[5]);

                                var gradient = new Gradient();
                                gradient.alphaKeys = new GradientAlphaKey[2]
                                {
                                        new GradientAlphaKey(alphaStart, 0f),
                                        new GradientAlphaKey(alphaEnd, 1f)
                                };
                                gradient.colorKeys = new GradientColorKey[2]
                                {
                                        new GradientColorKey(Color.white, 0f),
                                        new GradientColorKey(Color.white, 1f)
                                };

                                psCol.gradient = gradient;

                                colorOverLifetime.color = psCol;

                                var sizeOverLifetime = ps.sizeOverLifetime;
                                sizeOverLifetime.enabled = true;

                                var ssss = sizeOverLifetime.size;

                                var sizeStart = float.Parse(modifier.commands[6]);
                                var sizeEnd = float.Parse(modifier.commands[7]);

                                var curve = new AnimationCurve(new Keyframe[2]
                                {
                                            new Keyframe(0f, sizeStart),
                                            new Keyframe(1f, sizeEnd)
                                });

                                ssss.curve = curve;

                                sizeOverLifetime.size = ssss;
                            }
                            else if (!modifier.reference.particleSystem)
                            {
                                modifier.reference.particleSystem = mod.GetComponent<ParticleSystem>();
                                var ps = modifier.reference.particleSystem;

                                var mat = mod.GetComponent<ParticleSystemRenderer>();
                                mat.material = GameManager.inst.PlayerPrefabs[0].transform.GetChild(0).GetChild(0).GetComponent<TrailRenderer>().material;
                                mat.material.color = Color.white;
                                mat.trailMaterial = mat.material;
                                mat.renderMode = ParticleSystemRenderMode.Mesh;

                                var s = int.Parse(modifier.commands[1]);
                                var so = int.Parse(modifier.commands[2]);

                                s = Mathf.Clamp(s, 0, ObjectManager.inst.objectPrefabs.Count - 1);
                                so = Mathf.Clamp(so, 0, ObjectManager.inst.objectPrefabs[s].options.Count - 1);

                                mat.mesh = ObjectManager.inst.objectPrefabs[s == 4 ? 0 : s == 6 ? 0 : s].options[so].GetComponentInChildren<MeshFilter>().mesh;

                                var psMain = ps.main;
                                var psEmission = ps.emission;

                                psMain.simulationSpace = ParticleSystemSimulationSpace.World;

                                psMain.startSpeed = float.Parse(modifier.commands[9]);

                                if (modifier.constant)
                                    ps.emissionRate = float.Parse(modifier.commands[10]);
                                else
                                {
                                    ps.emissionRate = 0f;
                                    psMain.loop = false;
                                    psEmission.burstCount = int.Parse(modifier.commands[10]);
                                    psMain.duration = float.Parse(modifier.commands[11]);
                                }

                                var rotationOverLifetime = ps.rotationOverLifetime;
                                rotationOverLifetime.enabled = true;
                                rotationOverLifetime.separateAxes = true;
                                rotationOverLifetime.xMultiplier = 0f;
                                rotationOverLifetime.yMultiplier = 0f;
                                rotationOverLifetime.zMultiplier = float.Parse(modifier.commands[8]);

                                var forceOverLifetime = ps.forceOverLifetime;
                                forceOverLifetime.enabled = true;
                                forceOverLifetime.space = ParticleSystemSimulationSpace.World;
                                forceOverLifetime.xMultiplier = float.Parse(modifier.commands[12]);
                                forceOverLifetime.yMultiplier = float.Parse(modifier.commands[13]);

                                var particlesTrail = ps.trails;
                                particlesTrail.enabled = bool.Parse(modifier.commands[14]);

                                var colorOverLifetime = ps.colorOverLifetime;
                                colorOverLifetime.enabled = true;
                                var psCol = colorOverLifetime.color;

                                float alphaStart = float.Parse(modifier.commands[4]);
                                float alphaEnd = float.Parse(modifier.commands[5]);

                                var gradient = new Gradient();
                                gradient.alphaKeys = new GradientAlphaKey[2]
                                {
                                    new GradientAlphaKey(alphaStart, 0f),
                                    new GradientAlphaKey(alphaEnd, 1f)
                                };
                                gradient.colorKeys = new GradientColorKey[2]
                                {
                                    new GradientColorKey(Color.white, 0f),
                                    new GradientColorKey(Color.white, 1f)
                                };

                                psCol.gradient = gradient;

                                colorOverLifetime.color = psCol;

                                var sizeOverLifetime = ps.sizeOverLifetime;
                                sizeOverLifetime.enabled = true;

                                var ssss = sizeOverLifetime.size;

                                var sizeStart = float.Parse(modifier.commands[6]);
                                var sizeEnd = float.Parse(modifier.commands[7]);

                                var curve = new AnimationCurve(new Keyframe[2]
                                {
                                    new Keyframe(0f, sizeStart),
                                    new Keyframe(1f, sizeEnd)
                                });

                                ssss.curve = curve;

                                sizeOverLifetime.size = ssss;
                            }

                            if (modifier.reference.particleSystem)
                            {
                                var ps = modifier.reference.particleSystem;
                                var psMain = ps.main;
                                var psEmission = ps.emission;

                                psMain.startLifetime = float.Parse(modifier.value);
                                psEmission.enabled = !(mod.transform.lossyScale.x < 0.001f && mod.transform.lossyScale.x > -0.001f || mod.transform.lossyScale.y < 0.001f && mod.transform.lossyScale.y > -0.001f) && mod.activeSelf && mod.activeInHierarchy;

                                psMain.startColor = CoreHelper.CurrentBeatmapTheme.GetObjColor(int.Parse(modifier.commands[3]));

                                if (!modifier.constant && !psMain.loop)
                                {
                                    ps.Play();
                                }
                            }
                        }

                        break;
                    }
                case "trailRenderer":
                    {
                        if (modifier.reference != null && Updater.TryGetObject(modifier.reference, out LevelObject levelObject) && levelObject.visualObject.GameObject)
                        {
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
                                    tr.startColor = LSFunctions.LSColors.fadeColor(beatmapTheme.GetObjColor(startColor), startOpacity);
                                if (int.TryParse(modifier.commands[5], out int endColor) && float.TryParse(modifier.commands[6], out float endOpacity))
                                    tr.endColor = LSFunctions.LSColors.fadeColor(beatmapTheme.GetObjColor(endColor), endOpacity);
                            }
                        }
                        break;
                    }
                case "spawnPrefab":
                    {
                        if (!modifier.constant && int.TryParse(modifier.value, out int num) && DataManager.inst.gameData.prefabs.Count > num
                            && float.TryParse(modifier.commands[1], out float posX) && float.TryParse(modifier.commands[2], out float posY)
                            && float.TryParse(modifier.commands[3], out float scaX) && float.TryParse(modifier.commands[4], out float scaY) && float.TryParse(modifier.commands[5], out float rot)
                            && int.TryParse(modifier.commands[6], out int repeatCount) && float.TryParse(modifier.commands[7], out float repeatOffsetTime) && float.TryParse(modifier.commands[8], out float speed))
                        {
                            modifier.Result = ModifiersManager.AddPrefabObjectToLevel(DataManager.inst.gameData.prefabs[num],
                                AudioManager.inst.CurrentAudioSource.time,
                                new Vector2(posX, posY),
                                new Vector2(scaX, scaY),
                                rot, repeatCount, repeatOffsetTime, speed);

                            DataManager.inst.gameData.prefabObjects.Add((PrefabObject)modifier.Result);

                            Updater.AddPrefabToLevel((PrefabObject)modifier.Result);
                        }

                        break;
                    }
                case "playerHit":
                    {
                        if ((EditorManager.inst == null && DataManager.inst.GetSettingEnum("ArcadeDifficulty", 1) != 0 || !EditorManager.inst.isEditing) && !modifier.constant)
                        {
                            if (Updater.TryGetObject(modifier.reference, out LevelObject levelObject) && levelObject.visualObject != null && levelObject.visualObject.GameObject && int.TryParse(modifier.value, out int hit))
                            {
                                var orderedList = PlayerManager.Players
                                    .Where(x => x.Player)
                                    .OrderBy(x => Vector2.Distance(x.Player.playerObjects["RB Parent"].gameObject.transform.position, levelObject.visualObject.GameObject.transform.position)).ToList();

                                if (orderedList.Count > 0)
                                {
                                    var closest = orderedList[0];

                                    closest?.Player?.PlayerHit();

                                    if (hit > 1 && closest)
                                        closest.Health -= hit;
                                }
                            }
                        }

                        break;
                    }
                case "playerHitAll":
                    {
                        if ((EditorManager.inst == null && DataManager.inst.GetSettingEnum("ArcadeDifficulty", 1) != 0 || !EditorManager.inst.isEditing) && !modifier.constant && int.TryParse(modifier.value, out int hit))
                            foreach (var player in PlayerManager.Players.Where(x => x.Player))
                            {
                                player.Player.PlayerHit();

                                if (hit > 1)
                                    player.Health -= hit;
                            }

                        break;
                    }
                case "playerHeal":
                    {
                        if ((EditorManager.inst == null && DataManager.inst.GetSettingEnum("ArcadeDifficulty", 1) != 0 || !EditorManager.inst.isEditing) && !modifier.constant)
                            if (modifier.reference != null && Updater.TryGetObject(modifier.reference, out LevelObject levelObject) && levelObject.visualObject.GameObject && int.TryParse(modifier.value, out int hit))
                            {
                                var orderedList = PlayerManager.Players
                                    .Where(x => x.Player)
                                    .OrderBy(x => Vector2.Distance(x.Player.playerObjects["RB Parent"].gameObject.transform.position, levelObject.visualObject.GameObject.transform.position)).ToList();

                                if (orderedList.Count > 0)
                                {
                                    var closest = orderedList[0];

                                    if (closest)
                                        closest.Health += hit;
                                }
                            }
                        break;
                    }
                case "playerHealAll":
                    {
                        if ((EditorManager.inst == null && DataManager.inst.GetSettingEnum("ArcadeDifficulty", 1) != 0 || !EditorManager.inst.isEditing) && !modifier.constant && int.TryParse(modifier.value, out int hit))
                            foreach (var player in PlayerManager.Players.Where(x => x.Player))
                            {
                                player.Health += hit;
                            }
                        break;
                    }
                case "playerKill":
                    {
                        if ((EditorManager.inst == null || !EditorManager.inst.isEditing) && DataManager.inst.GetSettingEnum("ArcadeDifficulty", 1) != 0 && !modifier.constant)
                            if (modifier.reference != null && Updater.TryGetObject(modifier.reference, out LevelObject levelObject) && levelObject.visualObject.GameObject)
                            {
                                var orderedList = PlayerManager.Players
                                    .Where(x => x.Player)
                                    .OrderBy(x => Vector2.Distance(x.Player.playerObjects["RB Parent"].gameObject.transform.position, levelObject.visualObject.GameObject.transform.position)).ToList();

                                if (orderedList.Count > 0)
                                {
                                    var closest = orderedList[0];

                                    if (closest)
                                        closest.Health = 0;
                                }
                            }

                        break;
                    }
                case "playerKillAll":
                    {
                        if ((EditorManager.inst == null || !EditorManager.inst.isEditing) && DataManager.inst.GetSettingEnum("ArcadeDifficulty", 1) != 0 && !modifier.constant)
                        {
                            foreach (var player in PlayerManager.Players.Where(x => x.Player))
                            {
                                player.Health = 0;
                            }
                        }
                        break;
                    }
                case "playerMove":
                    {
                        if (modifier.reference != null && Updater.TryGetObject(modifier.reference, out LevelObject levelObject) && levelObject.visualObject.GameObject)
                        {
                            var vector = modifier.value.Split(new char[] { ',' });

                            var orderedList = PlayerManager.Players
                                .Where(x => x.Player)
                                .OrderBy(x => Vector2.Distance(x.Player.playerObjects["RB Parent"].gameObject.transform.position, levelObject.visualObject.GameObject.transform.position)).ToList();

                            if (orderedList.Count > 0)
                            {
                                var closest = orderedList[0];

                                bool relative = Parser.TryParse(modifier.commands[3], false);
                                if (closest)
                                {
                                    var tf = closest.Player.playerObjects["RB Parent"].gameObject.transform;
                                    if (modifier.constant)
                                        tf.localPosition = new Vector3(Parser.TryParse(vector[0], 0f), Parser.TryParse(vector[1], 0f), 0f);
                                    else
                                        tf
                                            .DOLocalMove(new Vector3(Parser.TryParse(vector[0], 0f) + (relative ? tf.position.x : 0f), Parser.TryParse(vector[1], 0f) + (relative ? tf.position.y : 0f), 0f), Parser.TryParse(modifier.commands[1], 1f))
                                            .SetEase(DataManager.inst.AnimationList[Parser.TryParse(modifier.commands[2], 0)].Animation);
                                }
                            }
                        }

                        break;
                    }
                case "playerMoveAll":
                    {
                        var vector = modifier.value.Split(new char[] { ',' });

                        bool relative = Parser.TryParse(modifier.commands[3], false);
                        foreach (var player in PlayerManager.Players.Where(x => x.Player))
                        {
                            var tf = player.Player.playerObjects["RB Parent"].gameObject.transform;
                            if (modifier.constant)
                                tf.localPosition = new Vector3(Parser.TryParse(vector[0], 0f), Parser.TryParse(vector[1], 0f), 0f);
                            else
                                tf
                                    .DOLocalMove(new Vector3(Parser.TryParse(vector[0], 0f) + (relative ? tf.position.x : 0f), Parser.TryParse(vector[1], 0f) + (relative ? tf.position.y : 0f), 0f), Parser.TryParse(modifier.commands[1], 1f))
                                    .SetEase(DataManager.inst.AnimationList[Parser.TryParse(modifier.commands[2], 0)].Animation);
                        }

                        break;
                    }
                case "playerMoveX":
                    {
                        if (modifier.reference && Updater.TryGetObject(modifier.reference, out LevelObject levelObject) && levelObject.visualObject.GameObject)
                        {
                            var orderedList = PlayerManager.Players
                                .Where(x => x.Player)
                                .OrderBy(x => Vector2.Distance(x.Player.playerObjects["RB Parent"].gameObject.transform.position, levelObject.visualObject.GameObject.transform.position)).ToList();

                            if (orderedList.Count > 0)
                            {
                                var closest = orderedList[0];

                                bool relative = Parser.TryParse(modifier.commands[3], false);
                                if (closest)
                                {
                                    var tf = closest.Player.playerObjects["RB Parent"].gameObject.transform;
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
                            }
                        }

                        break;
                    }
                case "playerMoveXAll":
                    {
                        bool relative = Parser.TryParse(modifier.commands[3], false);
                        foreach (var player in PlayerManager.Players.Where(x => x.Player))
                        {
                            var tf = player.Player.playerObjects["RB Parent"].gameObject.transform;
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
                case "playerMoveY":
                    {
                        if (modifier.reference && Updater.TryGetObject(modifier.reference, out LevelObject levelObject) && levelObject.visualObject.GameObject)
                        {
                            var orderedList = PlayerManager.Players
                                .Where(x => x.Player)
                                .OrderBy(x => Vector2.Distance(x.Player.playerObjects["RB Parent"].gameObject.transform.position, levelObject.visualObject.GameObject.transform.position)).ToList();

                            if (orderedList.Count > 0)
                            {
                                var closest = orderedList[0];

                                bool relative = Parser.TryParse(modifier.commands[3], false);
                                if (closest)
                                {
                                    var tf = closest.Player.playerObjects["RB Parent"].gameObject.transform;
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
                            }
                        }

                        break;
                    }
                case "playerMoveYAll":
                    {
                        bool relative = Parser.TryParse(modifier.commands[3], false);
                        foreach (var player in PlayerManager.Players.Where(x => x.Player))
                        {
                            var tf = player.Player.playerObjects["RB Parent"].gameObject.transform;
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
                case "playerRotate":
                    {
                        if (modifier.reference && Updater.TryGetObject(modifier.reference, out LevelObject levelObject) && levelObject.visualObject.GameObject)
                        {
                            var orderedList = PlayerManager.Players
                                .Where(x => x.Player)
                                .OrderBy(x => Vector2.Distance(x.Player.playerObjects["RB Parent"].gameObject.transform.position, levelObject.visualObject.GameObject.transform.position)).ToList();

                            if (orderedList.Count > 0)
                            {
                                var closest = orderedList[0];

                                bool relative = Parser.TryParse(modifier.commands[3], false);
                                if (closest)
                                {
                                    if (modifier.constant)
                                    {
                                        var v = closest.Player.playerObjects["RB Parent"].gameObject.transform.localRotation.eulerAngles;
                                        v.z += Parser.TryParse(modifier.value, 1f);
                                        closest.Player.playerObjects["RB Parent"].gameObject.transform.localRotation = Quaternion.Euler(v);
                                    }
                                    else
                                        closest.Player.playerObjects["RB Parent"].gameObject.transform
                                            .DORotate(new Vector3(0f, 0f, Parser.TryParse(modifier.value, 0f)), Parser.TryParse(modifier.commands[1], 1f))
                                            .SetEase(DataManager.inst.AnimationList[Parser.TryParse(modifier.commands[2], 0)].Animation);
                                }
                            }
                        }

                        break;
                    }
                case "playerRotateAll":
                    {
                        bool relative = Parser.TryParse(modifier.commands[3], false);
                        foreach (var player in PlayerManager.Players.Where(x => x.Player))
                        {
                            if (modifier.constant)
                            {
                                var v = player.Player.playerObjects["RB Parent"].gameObject.transform.localRotation.eulerAngles;
                                v.z += Parser.TryParse(modifier.value, 1f);
                                player.Player.playerObjects["RB Parent"].gameObject.transform.localRotation = Quaternion.Euler(v);
                            }
                            else
                                player.Player.playerObjects["RB Parent"].gameObject.transform
                                    .DORotate(new Vector3(0f, 0f, Parser.TryParse(modifier.value, 0f)), Parser.TryParse(modifier.commands[1], 1f))
                                    .SetEase(DataManager.inst.AnimationList[Parser.TryParse(modifier.commands[2], 0)].Animation);
                        }

                        break;
                    }
                case "playerBoost":
                    {
                        if (modifier.reference && Updater.TryGetObject(modifier.reference, out LevelObject levelObject) && levelObject.visualObject.GameObject && !modifier.constant)
                        {
                            var orderedList = PlayerManager.Players
                                .Where(x => x.Player)
                                .OrderBy(x => Vector2.Distance(x.Player.playerObjects["RB Parent"].gameObject.transform.position, levelObject.visualObject.GameObject.transform.position)).ToList();

                            if (orderedList.Count > 0)
                            {
                                var closest = orderedList[0];

                                closest?.Player?.StartBoost();
                            }
                        }

                        break;
                    }
                case "playerBoostAll":
                    {
                        foreach (var player in PlayerManager.Players.Where(x => x.Player))
                        {
                            player.Player.StartBoost();
                        }

                        break;
                    }
                case "playerDisableBoost":
                    {
                        if (modifier.reference && Updater.TryGetObject(modifier.reference, out LevelObject levelObject) && levelObject.visualObject.GameObject)
                        {
                            var orderedList = PlayerManager.Players
                                .Where(x => x.Player)
                                .OrderBy(x => Vector2.Distance(x.Player.playerObjects["RB Parent"].gameObject.transform.position, levelObject.visualObject.GameObject.transform.position)).ToList();

                            if (orderedList.Count > 0)
                            {
                                var closest = orderedList[0];

                                if (closest)
                                    closest.Player.canBoost = false;
                            }
                        }

                        break;
                    }
                case "playerDisableBoostAll":
                    {
                        foreach (var player in PlayerManager.Players.Where(x => x.Player))
                        {
                            player.Player.canBoost = false;
                        }

                        break;
                    }
                case "playerEnableBoost":
                    {
                        if (modifier.reference && Updater.TryGetObject(modifier.reference, out LevelObject levelObject) && levelObject.visualObject.GameObject)
                        {
                            var orderedList = PlayerManager.Players
                                .Where(x => x.Player)
                                .OrderBy(x => Vector2.Distance(x.Player.playerObjects["RB Parent"].gameObject.transform.position, levelObject.visualObject.GameObject.transform.position)).ToList();

                            if (orderedList.Count > 0)
                            {
                                var closest = orderedList[0];

                                if (closest)
                                    closest.Player.canBoost = true;
                            }
                        }

                        break;
                    }
                case "playerEnableBoostAll":
                    {
                        foreach (var player in PlayerManager.Players.Where(x => x.Player))
                        {
                            player.Player.canBoost = true;
                        }

                        break;
                    }
                case "playerSpeed":
                    {
                        if (float.TryParse(modifier.value, out float speed))
                        {
                            RTPlayer.SpeedMultiplier = speed;
                        }

                        break;
                    }
                case "showMouse":
                    {
                        LSFunctions.LSHelpers.ShowCursor();
                        break;
                    }
                case "hideMouse":
                    {
                        if (EditorManager.inst == null || !EditorManager.inst.isEditing)
                            LSFunctions.LSHelpers.HideCursor();
                        break;
                    }
                case "addVariable":
                    {
                        var list = DataManager.inst.gameData.beatmapObjects.Where(x => (x as BeatmapObject).tags.Contains(modifier.commands[1]));

                        if (list.Count() > 0 && int.TryParse(modifier.value, out int num))
                        {
                            foreach (var bm in DataManager.inst.gameData.beatmapObjects.FindAll(x => x.name == modifier.commands[1]))
                            {
                                var beatmapObject = (BeatmapObject)bm;
                                beatmapObject.integerVariable += num;
                            }
                        }
                        break;
                    }
                case "subVariable":
                    {
                        var list = DataManager.inst.gameData.beatmapObjects.Where(x => (x as BeatmapObject).tags.Contains(modifier.commands[1]));

                        if (list.Count() > 0 && int.TryParse(modifier.value, out int num))
                        {
                            foreach (var bm in DataManager.inst.gameData.beatmapObjects.FindAll(x => x.name == modifier.commands[1]))
                            {
                                var beatmapObject = (BeatmapObject)bm;
                                beatmapObject.integerVariable -= num;
                            }
                        }
                        break;
                    }
                case "setVariable":
                    {
                        var list = DataManager.inst.gameData.beatmapObjects.Where(x => (x as BeatmapObject).tags.Contains(modifier.commands[1]));

                        if (list.Count() > 0 && int.TryParse(modifier.value, out int num))
                        {
                            foreach (var bm in list)
                            {
                                var beatmapObject = (BeatmapObject)bm;
                                beatmapObject.integerVariable = num;
                            }
                        }
                        break;
                    }
                case "setVariableRandom":
                    {
                        var list = DataManager.inst.gameData.beatmapObjects.Where(x => (x as BeatmapObject).tags.Contains(modifier.value));

                        if (list.Count() > 0 && int.TryParse(modifier.commands[1], out int min) && int.TryParse(modifier.commands[2], out int max))
                        {
                            foreach (var bm in list)
                            {
                                var beatmapObject = (BeatmapObject)bm;
                                beatmapObject.integerVariable = UnityEngine.Random.Range(min, max < 0 ? max - 1 : max + 1);
                            }
                        }
                        break;
                    }
                case "loadVariable":
                    {
                        if (RTFile.FileExists(RTFile.ApplicationDirectory + "profile/" + modifier.commands[1] + ".ses"))
                        {
                            string json = FileManager.inst.LoadJSONFile("profile/" + modifier.commands[1] + ".ses");

                            if (!string.IsNullOrEmpty(json))
                            {
                                var jn = JSON.Parse(json);

                                if (!string.IsNullOrEmpty(jn[modifier.commands[2]][modifier.commands[3]]["float"]) &&
                                    float.TryParse(jn[modifier.commands[2]][modifier.commands[3]]["float"], out float eq))
                                {
                                    modifier.reference.integerVariable = (int)eq;
                                }
                            }
                        }
                        break;
                    }
                case "loadVariableOther":
                    {
                        if (RTFile.FileExists(RTFile.ApplicationDirectory + "profile/" + modifier.commands[1] + ".ses"))
                        {
                            string json = FileManager.inst.LoadJSONFile("profile/" + modifier.commands[1] + ".ses");

                            if (!string.IsNullOrEmpty(json))
                            {
                                var jn = JSON.Parse(json);

                                if (!string.IsNullOrEmpty(jn[modifier.commands[2]][modifier.commands[3]]["float"]) &&
                                    float.TryParse(jn[modifier.commands[2]][modifier.commands[3]]["float"], out float eq))
                                {
                                    var list = DataManager.inst.gameData.beatmapObjects.Where(x => (x as BeatmapObject).tags.Contains(modifier.value));

                                    if (list.Count() > 0)
                                    {
                                        foreach (var bm in list)
                                            ((BeatmapObject)bm).integerVariable = (int)eq;
                                    }
                                }
                            }
                        }
                        break;
                    }
                case "animateVariableOther":
                    {
                        var list = DataManager.inst.gameData.beatmapObjects.Where(x => (x as BeatmapObject).tags.Contains(modifier.value));

                        if (list.Count() > 0 && int.TryParse(modifier.commands[1], out int fromType) && int.TryParse(modifier.commands[2], out int fromAxis)
                            && float.TryParse(modifier.commands[3], out float delay) && float.TryParse(modifier.commands[4], out float multiply)
                            && float.TryParse(modifier.commands[5], out float offset) && float.TryParse(modifier.commands[6], out float min) && float.TryParse(modifier.commands[7], out float max)
                            && float.TryParse(modifier.commands[8], out float loop))
                        {
                            foreach (var beatmapObject in list)
                            {
                                var bm = (BeatmapObject)beatmapObject;

                                var time = AudioManager.inst.CurrentAudioSource.time;

                                fromType = Mathf.Clamp(fromType, 0, bm.events.Count);
                                fromAxis = Mathf.Clamp(fromAxis, 0, bm.events[fromType][0].eventValues.Length);

                                if (Updater.levelProcessor.converter.cachedSequences.ContainsKey(bm.id))
                                {
                                    // To Type Position
                                    // To Axis X
                                    // From Type Position
                                    if (fromType == 0)
                                    {
                                        var sequence = Updater.levelProcessor.converter.cachedSequences[bm.id].Position3DSequence.Interpolate(time - bm.StartTime - delay);

                                        bm.integerVariable = (int)Mathf.Clamp((fromAxis == 0 ? sequence.x % loop : fromAxis == 1 ? sequence.y % loop : sequence.z % loop) * multiply - offset, min, max);
                                    }

                                    // To Type Position
                                    // To Axis X
                                    // From Type Scale
                                    if (fromType == 1)
                                    {
                                        var sequence = Updater.levelProcessor.converter.cachedSequences[bm.id].ScaleSequence.Interpolate(time - bm.StartTime - delay);

                                        bm.integerVariable = (int)Mathf.Clamp((fromAxis == 0 ? sequence.x % loop : sequence.y % loop) * multiply - offset, min, max);
                                    }

                                    // To Type Position
                                    // To Axis X
                                    // From Type Rotation
                                    if (fromType == 2)
                                    {
                                        var sequence = Updater.levelProcessor.converter.cachedSequences[bm.id].RotationSequence.Interpolate(time - bm.StartTime - delay) * multiply;

                                        bm.integerVariable = (int)Mathf.Clamp((sequence % loop) - offset, min, max);
                                    }
                                }
                            }
                        }


                        break;
                    }
                case "enableObject":
                    {
                        if (Updater.TryGetObject(modifier.reference, out LevelObject levelObject) && levelObject.transformChain != null && levelObject.transformChain.Count > 0 && levelObject.transformChain[0] != null)
                        {
                            levelObject.transformChain[0].gameObject.SetActive(true);
                        }
                        break;
                    }
                case "enableObjectTree":
                    {
                        if (modifier.value == "0")
                            modifier.value = "False";

                        if (modifier.Result == null)
                        {
                            var beatmapObject = Parser.TryParse(modifier.value, true) ? modifier.reference : modifier.reference.GetParentChain().Last();

                            modifier.Result = beatmapObject.GetChildChain();
                        }

                        var list = (List<List<DataManager.GameData.BeatmapObject>>)modifier.Result;

                        for (int i = 0; i < list.Count; i++)
                        {
                            var childList = list[i];
                            for (int j = 0; j < childList.Count; j++)
                            {
                                if (childList[j] != null && Updater.TryGetObject(childList[j], out LevelObject levelObject) && levelObject.transformChain != null && levelObject.transformChain.Count > 0 && levelObject.transformChain[0] != null)
                                {
                                    levelObject.transformChain[0].gameObject.SetActive(true);
                                }
                            }
                        }

                        break;
                    }
                case "enableObjectOther":
                    {
                        var list = DataManager.inst.gameData.beatmapObjects.Where(x => (x as BeatmapObject).tags.Contains(modifier.value));

                        if (list.Count() > 0)
                        {
                            foreach (var beatmapObject in list)
                            {
                                if (Updater.TryGetObject(beatmapObject, out LevelObject levelObject) && levelObject.transformChain != null && levelObject.transformChain.Count > 0 && levelObject.transformChain[0] != null)
                                {
                                    levelObject.transformChain[0].gameObject.SetActive(true);
                                }
                            }
                        }

                        break;
                    }
                case "enableObjectTreeOther":
                    {
                        if (modifier.Result == null)
                        {
                            var beatmapObjects = DataManager.inst.gameData.beatmapObjects.Where(x => (x as BeatmapObject).tags.Contains(modifier.commands[1]));

                            var resultList = new List<List<DataManager.GameData.BeatmapObject>>();
                            foreach (var bm in beatmapObjects)
                            {
                                var beatmapObject = Parser.TryParse(modifier.value, true) ? bm : bm.GetParentChain().Last();
                                resultList.AddRange(beatmapObject.GetChildChain());
                            }

                            modifier.Result = resultList;
                        }

                        var list = (List<List<DataManager.GameData.BeatmapObject>>)modifier.Result;

                        for (int i = 0; i < list.Count; i++)
                        {
                            var childList = list[i];
                            for (int j = 0; j < childList.Count; j++)
                            {
                                if (childList[j] != null && Updater.TryGetObject(childList[j], out LevelObject levelObject) && levelObject.transformChain != null && levelObject.transformChain.Count > 0 && levelObject.transformChain[0] != null)
                                {
                                    levelObject.transformChain[0].gameObject.SetActive(true);
                                }
                            }
                        }

                        break;
                    }
                case "disableObject":
                    {
                        if (Updater.TryGetObject(modifier.reference, out LevelObject levelObject) && levelObject.transformChain != null && levelObject.transformChain.Count > 0 && levelObject.transformChain[0] != null)
                        {
                            levelObject.transformChain[0].gameObject.SetActive(false);
                        }
                        break;
                    }
                case "disableObjectTree":
                    {
                        if (modifier.value == "0")
                            modifier.value = "False";

                        if (modifier.Result == null)
                        {
                            var beatmapObject = Parser.TryParse(modifier.value, true) ? modifier.reference : modifier.reference.GetParentChain().Last();

                            modifier.Result = beatmapObject.GetChildChain();
                        }

                        var list = (List<List<DataManager.GameData.BeatmapObject>>)modifier.Result;

                        for (int i = 0; i < list.Count; i++)
                        {
                            var childList = list[i];
                            for (int j = 0; j < childList.Count; j++)
                            {
                                if (childList[j] != null && Updater.TryGetObject(childList[j], out LevelObject levelObject) && levelObject.transformChain != null && levelObject.transformChain.Count > 0 && levelObject.transformChain[0] != null)
                                {
                                    levelObject.transformChain[0].gameObject.SetActive(false);
                                }
                            }
                        }

                        break;
                    }
                case "disableObjectOther":
                    {
                        var list = DataManager.inst.gameData.beatmapObjects.Where(x => (x as BeatmapObject).tags.Contains(modifier.value));

                        if (list.Count() > 0)
                        {
                            foreach (var beatmapObject in list)
                            {
                                if (Updater.TryGetObject(beatmapObject, out LevelObject levelObject) && levelObject.transformChain != null && levelObject.transformChain.Count > 0 && levelObject.transformChain[0] != null)
                                {
                                    levelObject.transformChain[0].gameObject.SetActive(false);
                                }
                            }
                        }

                        break;
                    }
                case "disableObjectTreeOther":
                    {
                        if (modifier.Result == null)
                        {
                            var beatmapObjects = DataManager.inst.gameData.beatmapObjects.Where(x => (x as BeatmapObject).tags.Contains(modifier.commands[1]));

                            var resultList = new List<List<DataManager.GameData.BeatmapObject>>();
                            foreach (var bm in beatmapObjects)
                            {
                                var beatmapObject = Parser.TryParse(modifier.value, true) ? bm : bm.GetParentChain().Last();
                                resultList.AddRange(beatmapObject.GetChildChain());
                            }

                            modifier.Result = resultList;
                        }

                        var list = (List<List<DataManager.GameData.BeatmapObject>>)modifier.Result;

                        for (int i = 0; i < list.Count; i++)
                        {
                            var childList = list[i];
                            for (int j = 0; j < childList.Count; j++)
                            {
                                if (childList[j] != null && Updater.TryGetObject(childList[j], out LevelObject levelObject) && levelObject.transformChain != null && levelObject.transformChain.Count > 0 && levelObject.transformChain[0] != null)
                                {
                                    levelObject.transformChain[0].gameObject.SetActive(false);
                                }
                            }
                        }

                        break;
                    }
                case "saveFloat":
                    {
                        if ((EditorManager.inst == null || !EditorManager.inst.isEditing) && float.TryParse(modifier.value, out float num))
                            ModifiersManager.SaveProgress(modifier.commands[1], modifier.commands[2], modifier.commands[3], num);

                        break;
                    }
                case "saveString":
                    {
                        if (EditorManager.inst == null || !EditorManager.inst.isEditing)
                            ModifiersManager.SaveProgress(modifier.commands[1], modifier.commands[2], modifier.commands[3], modifier.value);

                        break;
                    }
                case "saveText":
                    {
                        if ((EditorManager.inst == null || !EditorManager.inst.isEditing) && modifier.reference != null && Updater.TryGetObject(modifier.reference, out LevelObject levelObject)
                            && levelObject.visualObject is TextObject textObject)
                            ModifiersManager.SaveProgress(modifier.commands[1], modifier.commands[2], modifier.commands[3], textObject.Text);

                        break;
                    }
                case "saveVariable":
                    {
                        if (EditorManager.inst == null || !EditorManager.inst.isEditing)
                            ModifiersManager.SaveProgress(modifier.commands[1], modifier.commands[2], modifier.commands[3], modifier.reference.integerVariable);

                        break;
                    }
                case "reactivePos":
                    {
                        if (modifier.reference && Updater.TryGetObject(modifier.reference, out LevelObject levelObject) && levelObject.visualObject.GameObject
                            && int.TryParse(modifier.commands[1], out int sampleX) && float.TryParse(modifier.commands[3], out float intensityX)
                            && int.TryParse(modifier.commands[2], out int sampleY) && float.TryParse(modifier.commands[4], out float intensityY)
                            && float.TryParse(modifier.value, out float val))
                        {
                            float[] samples = new float[256];

                            AudioManager.inst.CurrentAudioSource.GetSpectrumData(samples, 0, FFTWindow.Rectangular);

                            sampleX = Mathf.Clamp(sampleX, 0, 255);
                            sampleY = Mathf.Clamp(sampleY, 0, 255);

                            float reactivePositionX = samples[sampleX] * intensityX * val;
                            float reactivePositionY = samples[sampleY] * intensityY * val;

                            var x = modifier.reference.origin.x;
                            var y = modifier.reference.origin.y;

                            levelObject.visualObject.GameObject.transform.localPosition = new Vector3(x + reactivePositionX, y + reactivePositionY, modifier.reference.depth * 0.1f);

                            samples = null;
                        }
                        break;
                    }
                case "reactiveSca":
                    {
                        if (modifier.reference && Updater.TryGetObject(modifier.reference, out LevelObject levelObject) && levelObject.visualObject.GameObject
                            && int.TryParse(modifier.commands[1], out int sampleX) && float.TryParse(modifier.commands[3], out float intensityX)
                            && int.TryParse(modifier.commands[2], out int sampleY) && float.TryParse(modifier.commands[4], out float intensityY)
                            && float.TryParse(modifier.value, out float val))
                        {
                            float[] samples = new float[256];

                            AudioManager.inst.CurrentAudioSource.GetSpectrumData(samples, 0, FFTWindow.Rectangular);

                            sampleX = Mathf.Clamp(sampleX, 0, 255);
                            sampleY = Mathf.Clamp(sampleY, 0, 255);

                            float reactiveScaleX = samples[sampleX] * intensityX * val;
                            float reactiveScaleY = samples[sampleY] * intensityY * val;

                            levelObject.visualObject.GameObject.transform.localScale = new Vector3(1f + reactiveScaleX, 1f + reactiveScaleY, 1f);

                            samples = null;
                        }
                        break;
                    }
                case "reactiveRot":
                    {
                        if (modifier.reference && Updater.TryGetObject(modifier.reference, out LevelObject levelObject) && levelObject.visualObject.GameObject
                            && int.TryParse(modifier.commands[1], out int sample) && float.TryParse(modifier.value, out float val))
                        {
                            float[] samples = new float[256];

                            AudioManager.inst.CurrentAudioSource.GetSpectrumData(samples, 0, FFTWindow.Rectangular);

                            sample = Mathf.Clamp(sample, 0, 255);

                            float reactiveRotation = samples[sample] * val;

                            levelObject.visualObject.GameObject.transform.localRotation = Quaternion.Euler(0f, 0f, reactiveRotation);

                            samples = null;
                        }
                        break;
                    }
                case "reactiveCol":
                    {
                        if (modifier.reference && Updater.TryGetObject(modifier.reference, out LevelObject levelObject) && levelObject.visualObject.Renderer
                            && int.TryParse(modifier.commands[1], out int sample) && float.TryParse(modifier.value, out float val))
                        {
                            float[] samples = new float[256];

                            AudioManager.inst.CurrentAudioSource.GetSpectrumData(samples, 0, FFTWindow.Rectangular);

                            sample = Mathf.Clamp(sample, 0, 255);

                            float reactiveColor = samples[sample] * val;

                            if (levelObject.visualObject.Renderer != null && int.TryParse(modifier.commands[2], out int col))
                                levelObject.visualObject.Renderer.material.color += GameManager.inst.LiveTheme.objectColors[col] * reactiveColor;

                            samples = null;
                        }
                        break;
                    }
                case "reactiveColLerp":
                    {
                        if (modifier.reference && Updater.TryGetObject(modifier.reference, out LevelObject levelObject) && levelObject.visualObject.Renderer
                            && int.TryParse(modifier.commands[1], out int sample) && float.TryParse(modifier.value, out float val))
                        {
                            float[] samples = new float[256];

                            AudioManager.inst.CurrentAudioSource.GetSpectrumData(samples, 0, FFTWindow.Rectangular);

                            sample = Mathf.Clamp(sample, 0, 255);

                            float reactiveColor = samples[sample] * val;

                            if (levelObject.visualObject.Renderer != null && int.TryParse(modifier.commands[2], out int col))
                                levelObject.visualObject.Renderer.material.color = RTMath.Lerp(levelObject.visualObject.Renderer.material.color, GameManager.inst.LiveTheme.objectColors[col], reactiveColor);

                            samples = null;
                        }
                        break;
                    }
                case "reactivePosChain":
                    {
                        if (modifier.reference
                            && int.TryParse(modifier.commands[1], out int sampleX) && float.TryParse(modifier.commands[3], out float intensityX)
                            && int.TryParse(modifier.commands[2], out int sampleY) && float.TryParse(modifier.commands[4], out float intensityY)
                            && float.TryParse(modifier.value, out float val))
                        {
                            float[] samples = new float[256];

                            AudioManager.inst.CurrentAudioSource.GetSpectrumData(samples, 0, FFTWindow.Rectangular);

                            sampleX = Mathf.Clamp(sampleX, 0, 255);
                            sampleY = Mathf.Clamp(sampleY, 0, 255);

                            float reactivePositionX = samples[sampleX] * intensityX * val;
                            float reactivePositionY = samples[sampleY] * intensityY * val;

                            modifier.reference.reactivePositionOffset = new Vector3(reactivePositionX, reactivePositionY);

                            samples = null;
                        }

                        break;
                    }
                case "reactiveScaChain":
                    {
                        if (modifier.reference
                            && int.TryParse(modifier.commands[1], out int sampleX) && float.TryParse(modifier.commands[3], out float intensityX)
                            && int.TryParse(modifier.commands[2], out int sampleY) && float.TryParse(modifier.commands[4], out float intensityY)
                            && float.TryParse(modifier.value, out float val))
                        {
                            float[] samples = new float[256];

                            AudioManager.inst.CurrentAudioSource.GetSpectrumData(samples, 0, FFTWindow.Rectangular);

                            sampleX = Mathf.Clamp(sampleX, 0, 255);
                            sampleY = Mathf.Clamp(sampleY, 0, 255);

                            float reactiveScaleX = samples[sampleX] * intensityX * val;
                            float reactiveScaleY = samples[sampleY] * intensityY * val;

                            modifier.reference.reactiveScaleOffset = new Vector3(reactiveScaleX, reactiveScaleY, 1f);

                            samples = null;
                        }

                        break;
                    }
                case "reactiveRotChain":
                    {
                        if (modifier.reference && int.TryParse(modifier.commands[1], out int sample) && float.TryParse(modifier.value, out float val))
                        {
                            float[] samples = new float[256];

                            AudioManager.inst.CurrentAudioSource.GetSpectrumData(samples, 0, FFTWindow.Rectangular);

                            sample = Mathf.Clamp(sample, 0, 255);

                            float reactiveRotation = samples[sample] * val;

                            modifier.reference.reactiveRotationOffset = reactiveRotation;

                            samples = null;
                        }
                        break;
                    }
                case "setPlayerModel":
                    {
                        if (!modifier.constant && int.TryParse(modifier.commands[1], out int result) && PlayerManager.PlayerModels.ContainsKey(modifier.value))
                        {
                            PlayerManager.SetPlayerModel(result, modifier.value);
                            PlayerManager.AssignPlayerModels();

                            if (PlayerManager.Players.Count > result && PlayerManager.Players[result].Player)
                            {
                                PlayerManager.Players[result].Player.playerNeedsUpdating = true;
                                PlayerManager.Players[result].Player.UpdatePlayer();
                            }
                        }
                        break;
                    }
                case "eventOffset":
                    {
                        if (ModCompatibility.sharedFunctions.ContainsKey("EventsCoreEventOffsets"))
                        {
                            var list = (List<List<float>>)ModCompatibility.sharedFunctions["EventsCoreEventOffsets"];

                            var indexArray = Parser.TryParse(modifier.commands[1], 0);
                            var indexValue = Parser.TryParse(modifier.commands[2], 0);

                            if (indexArray < list.Count && indexValue < list[indexArray].Count)
                                list[indexArray][indexValue] = Parser.TryParse(modifier.value, 0f);

                            ModCompatibility.sharedFunctions["EventsCoreEventOffsets"] = list;
                        }
                        break;
                    }
                case "eventOffsetVariable":
                    {
                        if (ModCompatibility.sharedFunctions.ContainsKey("EventsCoreEventOffsets"))
                        {
                            var list = (List<List<float>>)ModCompatibility.sharedFunctions["EventsCoreEventOffsets"];

                            var indexArray = Parser.TryParse(modifier.commands[1], 0);
                            var indexValue = Parser.TryParse(modifier.commands[2], 0);

                            if (indexArray < list.Count && indexValue < list[indexArray].Count)
                                list[indexArray][indexValue] = modifier.reference.integerVariable * Parser.TryParse(modifier.value, 1f);

                            ModCompatibility.sharedFunctions["EventsCoreEventOffsets"] = list;
                        }
                        break;
                    }
                case "eventOffsetAnimate":
                    {
                        if (!modifier.constant && ModCompatibility.sharedFunctions.ContainsKey("EventsCoreEventOffsets"))
                        {
                            string easing = modifier.commands[4];
                            if (int.TryParse(modifier.commands[4], out int e) && e >= 0 && e < DataManager.inst.AnimationList.Count)
                                easing = DataManager.inst.AnimationList[e].Name;

                            var list = (List<List<float>>)ModCompatibility.sharedFunctions["EventsCoreEventOffsets"];

                            var indexArray = Parser.TryParse(modifier.commands[1], 0);
                            var indexValue = Parser.TryParse(modifier.commands[2], 0);

                            if (modifier.commands.Count < 6)
                                modifier.commands.Add("False");

                            if (indexArray < list.Count && indexValue < list[indexArray].Count)
                            {
                                var value = Parser.TryParse(modifier.commands[5], false) ? list[indexArray][indexValue] + Parser.TryParse(modifier.value, 0f) : Parser.TryParse(modifier.value, 0f);

                                var animation = new RTAnimation("Event Offset Animation");
                                animation.animationHandlers = new List<AnimationHandlerBase>
                                {
                                    new AnimationHandler<float>(new List<IKeyframe<float>>
                                    {
                                        new FloatKeyframe(0f, list[indexArray][indexValue], Ease.Linear),
                                        new FloatKeyframe(Parser.TryParse(modifier.commands[3], 1f), value, Ease.HasEaseFunction(easing) ? Ease.GetEaseFunction(easing) : Ease.Linear),
                                        new FloatKeyframe(Parser.TryParse(modifier.commands[3], 1f) + 0.1f, value, Ease.Linear),
                                    }, delegate (float x)
                                    {
                                        list[indexArray][indexValue] = x;
                                        ModCompatibility.sharedFunctions["EventsCoreEventOffsets"] = list;
                                    })
                                };
                                animation.onComplete = delegate ()
                                {
                                    AnimationManager.inst.RemoveID(animation.id);
                                };

                                AnimationManager.inst.Play(animation);
                            }
                        }
                        break;
                    }
                case "eventOffsetCopyAxis":
                    {
                        if (!ModCompatibility.sharedFunctions.ContainsKey("EventsCoreEventOffsets"))
                            break;

                        var list = (List<List<float>>)ModCompatibility.sharedFunctions["EventsCoreEventOffsets"];

                        if (int.TryParse(modifier.commands[1], out int fromType) && int.TryParse(modifier.commands[2], out int fromAxis)
                            && int.TryParse(modifier.commands[3], out int toType) && int.TryParse(modifier.commands[4], out int toAxis)
                            && float.TryParse(modifier.commands[5], out float delay) && float.TryParse(modifier.commands[6], out float multiply)
                            && float.TryParse(modifier.commands[7], out float offset) && float.TryParse(modifier.commands[8], out float min) && float.TryParse(modifier.commands[9], out float max)
                            && float.TryParse(modifier.commands[10], out float loop) && bool.TryParse(modifier.commands[11], out bool useVisual))
                        {
                            var time = AudioManager.inst.CurrentAudioSource.time;

                            fromType = Mathf.Clamp(fromType, 0, modifier.reference.events.Count - 1);
                            fromAxis = Mathf.Clamp(fromAxis, 0, modifier.reference.events[fromType][0].eventValues.Length - 1);
                            toType = Mathf.Clamp(toType, 0, list.Count - 1);
                            toAxis = Mathf.Clamp(toAxis, 0, list[toType].Count - 1);

                            if (!useVisual)
                            {
                                if (fromType == 0)
                                {
                                    var sequence = Updater.levelProcessor.converter.cachedSequences[modifier.reference.id].Position3DSequence.Interpolate(time - modifier.reference.StartTime - delay);
                                    float value = ((fromAxis == 0 ? sequence.x : fromAxis == 1 ? sequence.y : sequence.z) - offset) * multiply % loop;

                                    list[toType][toAxis] = Mathf.Clamp(value, min, max);
                                    ModCompatibility.sharedFunctions["EventsCoreEventOffsets"] = list;
                                }

                                if (fromType == 1)
                                {
                                    var sequence = Updater.levelProcessor.converter.cachedSequences[modifier.reference.id].ScaleSequence.Interpolate(time - modifier.reference.StartTime - delay);
                                    float value = ((fromAxis == 0 ? sequence.x : sequence.y) - offset) * multiply % loop;

                                    list[toType][toAxis] = Mathf.Clamp(value, min, max);
                                    ModCompatibility.sharedFunctions["EventsCoreEventOffsets"] = list;
                                }

                                if (fromType == 2)
                                {
                                    var sequence = (Updater.levelProcessor.converter.cachedSequences[modifier.reference.id].RotationSequence.Interpolate(time - modifier.reference.StartTime - delay) - offset) * multiply % loop;

                                    list[toType][toAxis] = Mathf.Clamp(sequence, min, max);
                                    ModCompatibility.sharedFunctions["EventsCoreEventOffsets"] = list;
                                }
                            }
                            else if (Updater.TryGetObject(modifier.reference, out LevelObject levelObject) && levelObject.visualObject != null && levelObject.visualObject.GameObject)
                            {
                                var transform = levelObject.visualObject.GameObject.transform;

                                if (toType >= 0 && toType < 3 && fromType == 0)
                                {
                                    var sequence = transform.position;
                                    float value = ((fromAxis == 0 ? sequence.x : fromAxis == 1 ? sequence.y : sequence.z) - offset) * multiply % loop;

                                    list[toType][toAxis] = Mathf.Clamp(value, min, max);
                                    ModCompatibility.sharedFunctions["EventsCoreEventOffsets"] = list;
                                }

                                if (toType >= 0 && toType < 3 && fromType == 1)
                                {
                                    var sequence = transform.lossyScale;
                                    float value = ((fromAxis == 0 ? sequence.x : fromAxis == 1 ? sequence.y : sequence.z) - offset) * multiply % loop;

                                    list[toType][toAxis] = Mathf.Clamp(value, min, max);
                                    ModCompatibility.sharedFunctions["EventsCoreEventOffsets"] = list;
                                }

                                if (toType >= 0 && toType < 3 && fromType == 2)
                                {
                                    var sequence = transform.rotation.eulerAngles;
                                    float value = ((fromAxis == 0 ? sequence.x : fromAxis == 1 ? sequence.y : sequence.z) - offset) * multiply % loop;

                                    list[toType][toAxis] = Mathf.Clamp(value, min, max);
                                    ModCompatibility.sharedFunctions["EventsCoreEventOffsets"] = list;
                                }
                            }
                        }
                        break;
                    }
                case "vignetteTracksPlayer":
                    {
                        var player = PlayerManager.Players[0].Player;

                        var rb = player.playerObjects["RB Parent"].gameObject;

                        var cameraToViewportPoint = Camera.main.WorldToViewportPoint(rb.transform.position);

                        var list = (List<List<float>>)ModCompatibility.sharedFunctions["EventsCoreEventOffsets"];

                        var indexArray = 7;
                        var indexXValue = 4;
                        var indexYValue = 5;

                        if (indexArray < list.Count && indexXValue < list[indexArray].Count)
                            list[indexArray][indexXValue] = cameraToViewportPoint.x;
                        if (indexArray < list.Count && indexYValue < list[indexArray].Count)
                            list[indexArray][indexYValue] = cameraToViewportPoint.y;

                        ModCompatibility.sharedFunctions["EventsCoreEventOffsets"] = list;

                        break;
                    }
                case "lensTracksPlayer":
                    {
                        var player = PlayerManager.Players[0].Player;

                        var rb = player.playerObjects["RB Parent"].gameObject;

                        var cameraToViewportPoint = Camera.main.WorldToViewportPoint(rb.transform.position);

                        var list = (List<List<float>>)ModCompatibility.sharedFunctions["EventsCoreEventOffsets"];

                        var indexArray = 8;
                        var indexXValue = 1;
                        var indexYValue = 2;

                        if (indexArray < list.Count && indexXValue < list[indexArray].Count)
                            list[indexArray][indexXValue] = cameraToViewportPoint.x - 0.5f;
                        if (indexArray < list.Count && indexYValue < list[indexArray].Count)
                            list[indexArray][indexYValue] = cameraToViewportPoint.y - 0.5f;

                        ModCompatibility.sharedFunctions["EventsCoreEventOffsets"] = list;

                        break;
                    }
                case "legacyTail":
                    {
                        if (modifier.reference && Updater.TryGetObject(modifier.reference, out LevelObject levelObject) && levelObject.visualObject != null && levelObject.visualObject.GameObject &&
                            modifier.commands.Count > 1 && DataManager.inst.gameData is GameData gameData)
                        {
                            var totalTime = Parser.TryParse(modifier.value, 200f);

                            var list = modifier.Result is List<LegacyTracker> ? (List<LegacyTracker>)modifier.Result : new List<LegacyTracker>();

                            if (modifier.Result == null)
                            {
                                list.Add(new LegacyTracker(modifier.reference, Vector3.zero, Vector3.zero, Quaternion.identity, 0f, 0f));

                                for (int i = 1; i < modifier.commands.Count; i += 3)
                                {
                                    var group = gameData.BeatmapObjects.Where(x => x.tags.Contains(modifier.commands[i]));

                                    if (modifier.commands.Count <= i + 2 || group.Count() < 1)
                                        break;

                                    var distance = Parser.TryParse(modifier.commands[i + 1], 2f);
                                    var time = Parser.TryParse(modifier.commands[i + 2], 12f);

                                    for (int j = 0; j < group.Count(); j++)
                                    {
                                        var beatmapObject = group.ElementAt(j);
                                        list.Add(new LegacyTracker(beatmapObject, beatmapObject.positionOffset, beatmapObject.positionOffset, Quaternion.Euler(beatmapObject.rotationOffset), distance, time));
                                    }
                                }

                                var onDestroy = levelObject.visualObject.GameObject.AddComponent<DestroyModifierResult>();

                                onDestroy.Modifier = modifier;

                                modifier.Result = list;
                            }

                            list[0].pos = levelObject.visualObject.GameObject.transform.position;
                            list[0].rot = levelObject.visualObject.GameObject.transform.rotation;

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
                case "blackHole":
                    {
                        if (modifier.reference != null && Updater.TryGetObject(modifier.reference, out LevelObject levelObject) && levelObject.visualObject.GameObject)
                        {
                            var gm = levelObject.visualObject.GameObject;

                            for (int i = 0; i < InputDataManager.inst.players.Count; i++)
                            {
                                if (GameManager.inst.players.transform.Find("Player " + (i + 1).ToString()))
                                {
                                    var pl = GameManager.inst.players.transform.Find("Player " + (i + 1).ToString() + "/Player");

                                    float pitch = AudioManager.inst.CurrentAudioSource.pitch;

                                    if (pitch < 0f)
                                        pitch = -pitch;
                                    if (pitch == 0f)
                                        pitch = 0.001f;

                                    float p = Time.deltaTime * 60f * pitch;

                                    if (modifier.commands.Count < 2)
                                        modifier.commands.Add("false");

                                    float num = Parser.TryParse(modifier.value, 0.01f);

                                    if (modifier.commands.Count > 1 && bool.TryParse(modifier.commands[1], out bool r) && r)
                                        num = -(modifier.reference.Interpolate(3, 1) - 1f) * num;

                                    float moveDelay = 1f - Mathf.Pow(1f - Mathf.Clamp(num, 0.001f, 1f), p);

                                    var vector = new Vector3(pl.position.x, pl.position.y, 0f);
                                    var target = new Vector3(gm.transform.position.x, gm.transform.position.y, 0f);

                                    if (AudioManager.inst.CurrentAudioSource.isPlaying)
                                        pl.position += (target - vector) * moveDelay;
                                }
                            }
                        }
                        break;
                    }
                case "addColor":
                    {
                        if (modifier.reference != null && Updater.TryGetObject(modifier.reference, out LevelObject levelObject) && levelObject.visualObject.Renderer && int.TryParse(modifier.commands[1], out int index) && float.TryParse(modifier.value, out float num))
                        {
                            index = Mathf.Clamp(index, 0, GameManager.inst.LiveTheme.objectColors.Count - 1);

                            if (levelObject.visualObject.Renderer != null)
                                levelObject.visualObject.Renderer.material.color += GameManager.inst.LiveTheme.objectColors[index] * num;
                        }

                        break;
                    }
                case "addColorOther":
                    {
                        var list = DataManager.inst.gameData.beatmapObjects.Where(x => (x as BeatmapObject).tags.Contains(modifier.commands[1]));

                        if (list.Count() > 0)
                            foreach (var bm in list)
                            {
                                if (Updater.TryGetObject(bm, out LevelObject levelObject) && levelObject.visualObject.Renderer && int.TryParse(modifier.commands[2], out int index) && float.TryParse(modifier.value, out float num))
                                {
                                    index = Mathf.Clamp(index, 0, GameManager.inst.LiveTheme.objectColors.Count - 1);

                                    levelObject.visualObject.Renderer.material.color += GameManager.inst.LiveTheme.objectColors[index] * num;
                                }
                            }

                        break;
                    }
                case "lerpColor":
                    {
                        if (modifier.reference != null && Updater.TryGetObject(modifier.reference, out LevelObject levelObject) && levelObject.visualObject.Renderer && int.TryParse(modifier.commands[1], out int index) && float.TryParse(modifier.value, out float num))
                        {
                            index = Mathf.Clamp(index, 0, GameManager.inst.LiveTheme.objectColors.Count - 1);

                            if (levelObject.visualObject != null && levelObject.visualObject.Renderer)
                                levelObject.visualObject.Renderer.material.color = RTMath.Lerp(levelObject.visualObject.Renderer.material.color, GameManager.inst.LiveTheme.objectColors[index], num);
                        }

                        break;
                    }
                case "lerpColorOther":
                    {
                        var list = DataManager.inst.gameData.beatmapObjects.Where(x => (x as BeatmapObject).tags.Contains(modifier.commands[1]));

                        if (list.Count() > 0)
                            foreach (var bm in list)
                            {
                                if (bm != null && Updater.TryGetObject(bm, out LevelObject levelObject) && int.TryParse(modifier.commands[2], out int index) && float.TryParse(modifier.value, out float num))
                                {
                                    index = Mathf.Clamp(index, 0, GameManager.inst.LiveTheme.objectColors.Count - 1);

                                    if (levelObject.visualObject != null && levelObject.visualObject.Renderer)
                                        levelObject.visualObject.Renderer.material.color = RTMath.Lerp(levelObject.visualObject.Renderer.material.color, GameManager.inst.LiveTheme.objectColors[index], num);
                                }
                            }


                        break;
                    }
                case "addColorPlayerDistance":
                    {
                        if (modifier.reference != null && Updater.TryGetObject(modifier.reference, out LevelObject levelObject) && levelObject.visualObject.GameObject && levelObject.visualObject.Renderer && int.TryParse(modifier.commands[1], out int index) && float.TryParse(modifier.value, out float num))
                        {
                            var i = RTExtensions.ClosestPlayer(levelObject.visualObject.GameObject);

                            var player = GameManager.inst.players.transform.Find(string.Format("Player {0}/Player", i + 1));

                            var distance = Vector2.Distance(player.transform.position, levelObject.visualObject.GameObject.transform.position);

                            index = Mathf.Clamp(index, 0, GameManager.inst.LiveTheme.objectColors.Count - 1);

                            levelObject.visualObject.Renderer.material.color += GameManager.inst.LiveTheme.objectColors[index] * (distance - num);
                        }

                        break;
                    }
                case "setAlpha":
                    {
                        if (modifier.reference != null && Updater.TryGetObject(modifier.reference, out LevelObject levelObject) && levelObject.visualObject.Renderer && float.TryParse(modifier.value, out float num))
                        {
                            if (levelObject.visualObject is not TextObject)
                                levelObject.visualObject.Renderer.material.color = LSFunctions.LSColors.fadeColor(levelObject.visualObject.Renderer.material.color, num);
                            else
                                ((TextObject)levelObject.visualObject).TextMeshPro.color = LSFunctions.LSColors.fadeColor(levelObject.visualObject.Renderer.material.color, num);
                        }

                        break;
                    }
                case "setAlphaOther":
                    {
                        var list = DataManager.inst.gameData.beatmapObjects.Where(x => (x as BeatmapObject).tags.Contains(modifier.commands[1]));

                        if (list.Count() > 0)
                            foreach (var bm in list)
                            {
                                if (bm != null && Updater.TryGetObject(bm, out LevelObject levelObject) && levelObject.visualObject.Renderer && float.TryParse(modifier.value, out float num))
                                {
                                    if (levelObject.visualObject is not TextObject)
                                        levelObject.visualObject.Renderer.material.color = LSFunctions.LSColors.fadeColor(levelObject.visualObject.Renderer.material.color, num);
                                    else
                                        ((TextObject)levelObject.visualObject).TextMeshPro.color = LSFunctions.LSColors.fadeColor(levelObject.visualObject.Renderer.material.color, num);
                                }
                            }

                        break;
                    }
                case "copyColor":
                    {
                        Predicate<DataManager.GameData.BeatmapObject> predicate = x => (x as BeatmapObject).tags.Contains(modifier.value);

                        if (!DataManager.inst.gameData.beatmapObjects.Has(predicate))
                            break;

                        var beatmapObject = DataManager.inst.gameData.beatmapObjects.Find(predicate);

                        if (Updater.TryGetObject(beatmapObject, out LevelObject otherLevelObject) &&
                            otherLevelObject.visualObject.Renderer &&
                            Updater.TryGetObject(modifier.reference, out LevelObject levelObject) &&
                            levelObject.visualObject.Renderer)
                        {
                            levelObject.visualObject.Renderer.material.color = otherLevelObject.visualObject.Renderer.material.color;
                        }

                        break;
                    }
                case "copyColorOther":
                    {
                        var list = DataManager.inst.gameData.beatmapObjects.Where(x => (x as BeatmapObject).tags.Contains(modifier.value));

                        if (list.Count() > 0)
                            foreach (var bm in list)
                            {
                                if (Updater.TryGetObject(bm, out LevelObject otherLevelObject) &&
                                    otherLevelObject.visualObject.Renderer &&
                                    Updater.TryGetObject(modifier.reference, out LevelObject levelObject) &&
                                    levelObject.visualObject.Renderer)
                                {
                                    otherLevelObject.visualObject.Renderer.material.color = levelObject.visualObject.Renderer.material.color;
                                }
                            }

                        break;
                    }
                case "updateObjects":
                    {
                        if (!modifier.constant)
                            ObjectManager.inst.updateObjects();
                        break;
                    }
                case "updateObject":
                    {
                        var list = DataManager.inst.gameData.beatmapObjects.Where(x => (x as BeatmapObject).tags.Contains(modifier.value));

                        if (!modifier.constant && list.Count() > 0)
                        {
                            foreach (var bm in list)
                            {
                                Updater.UpdateProcessor(bm);
                            }
                        }
                        break;
                    }
                case "signalModifier":
                    {
                        var list = DataManager.inst.gameData.beatmapObjects.Where(x => (x as BeatmapObject).tags.Contains(modifier.commands[1]));

                        foreach (var bm in list)
                        {
                            CoreHelper.StartCoroutine(ModifiersManager.ActivateModifier((BeatmapObject)bm, Parser.TryParse(modifier.value, 0f)));
                        }

                        break;
                    }
                case "editorNotify":
                    {
                        EditorManager.inst?.DisplayNotification(modifier.value, Parser.TryParse(modifier.commands[1], 0.5f), (EditorManager.NotificationType)Parser.TryParse(modifier.commands[2], 0));
                        break;
                    }
                case "setImage":
                    {
                        if (modifier.reference.shape == 6 && modifier.reference.levelObject && modifier.reference.levelObject.visualObject != null &&
                            modifier.reference.levelObject.visualObject is ImageObject imageObject)
                        {
                            if (!modifier.constant)
                            {
                                var path = RTFile.BasePath + modifier.value;

                                var local = imageObject.GameObject.transform.localPosition;

                                if (RTFile.FileExists(path))
                                    CoreHelper.StartCoroutine(AlephNetworkManager.DownloadImageTexture("file://" + path, delegate (Texture2D x)
                                    {
                                        ((SpriteRenderer)imageObject.Renderer).sprite = SpriteManager.CreateSprite(x);
                                        imageObject.GameObject.transform.localPosition = local;
                                        imageObject.GameObject.transform.localPosition = local;
                                        imageObject.GameObject.transform.localPosition = local;
                                    }, delegate (string onError)
                                    {
                                        //((SpriteRenderer)imageObject.Renderer).sprite = ArcadeManager.inst.defaultImage;
                                    }));
                                //else ((SpriteRenderer)imageObject.Renderer).sprite = ArcadeManager.inst.defaultImage;
                            }
                        }
                        break;
                    }
                case "setImageOther":
                    {
                        var list = DataManager.inst.gameData.beatmapObjects.Where(x => (x as BeatmapObject).tags.Contains(modifier.commands[1]));

                        if (list.Count() > 0 && !modifier.constant)
                        {
                            foreach (var bm in list.Select(x => x as BeatmapObject))
                            {
                                if (bm.shape == 6 && bm.levelObject && bm.levelObject.visualObject != null &&
                                    bm.levelObject.visualObject is ImageObject imageObject)
                                {
                                    var path = RTFile.BasePath + modifier.value;

                                    var local = imageObject.GameObject.transform.localPosition;

                                    if (RTFile.FileExists(path))
                                        CoreHelper.StartCoroutine(AlephNetworkManager.DownloadImageTexture("file://" + path, delegate (Texture2D x)
                                        {
                                            ((SpriteRenderer)imageObject.Renderer).sprite = SpriteManager.CreateSprite(x);
                                            imageObject.GameObject.transform.localPosition = local;
                                            imageObject.GameObject.transform.localPosition = local;
                                            imageObject.GameObject.transform.localPosition = local;
                                        }, delegate (string onError)
                                        {
                                            //((SpriteRenderer)imageObject.Renderer).sprite = ArcadeManager.inst.defaultImage;
                                        }));
                                    //else ((SpriteRenderer)imageObject.Renderer).sprite = ArcadeManager.inst.defaultImage;
                                }
                            }
                        }

                        break;
                    }
                case "setText":
                    {
                        if (modifier.reference.shape == 4 && modifier.reference.levelObject && modifier.reference.levelObject.visualObject != null &&
                            modifier.reference.levelObject.visualObject is TextObject)
                        {
                            if (modifier.constant)
                                ((TextObject)modifier.reference.levelObject.visualObject).SetText(modifier.value);
                            else
                                ((TextObject)modifier.reference.levelObject.visualObject).Text = modifier.value;
                        }
                        break;
                    }
                case "setTextOther":
                    {
                        var list = DataManager.inst.gameData.beatmapObjects.Where(x => (x as BeatmapObject).tags.Contains(modifier.commands[1]));

                        if (list.Count() > 0)
                        {
                            foreach (var bm in list.Select(x => x as BeatmapObject))
                            {
                                if (bm.shape == 4 && bm.levelObject && bm.levelObject.visualObject != null &&
                                    bm.levelObject.visualObject is TextObject textObject)
                                {
                                    if (modifier.constant)
                                        textObject.SetText(modifier.value);
                                    else
                                        textObject.Text = modifier.value;
                                }
                            }
                        }

                        break;
                    }
                case "addText":
                    {
                        if (modifier.reference.shape == 4 && modifier.reference.levelObject && modifier.reference.levelObject.visualObject != null &&
                            modifier.reference.levelObject.visualObject is TextObject)
                        {
                            ((TextObject)modifier.reference.levelObject.visualObject).Text += modifier.value;
                        }
                        break;
                    }
                case "addTextOther":
                    {
                        var list = DataManager.inst.gameData.beatmapObjects.Where(x => (x as BeatmapObject).tags.Contains(modifier.commands[1]));

                        if (list.Count() > 0)
                        {
                            foreach (var bm in list.Select(x => x as BeatmapObject))
                            {
                                if (bm.shape == 4 && bm.levelObject && bm.levelObject.visualObject != null &&
                                    bm.levelObject.visualObject is TextObject textObject)
                                {
                                    textObject.Text += modifier.value;
                                }
                            }
                        }

                        break;
                    }
                case "removeText":
                    {
                        if (modifier.reference.shape == 4 && modifier.reference.levelObject && modifier.reference.levelObject.visualObject != null &&
                            modifier.reference.levelObject.visualObject is TextObject && int.TryParse(modifier.value, out int remove))
                        {
                            var visualObject = (TextObject)modifier.reference.levelObject.visualObject;
                            string text = string.IsNullOrEmpty(visualObject.TextMeshPro.text) ? "" :
                                visualObject.TextMeshPro.text.Substring(0, visualObject.TextMeshPro.text.Length - Mathf.Clamp(remove, 0, visualObject.TextMeshPro.text.Length - 1));

                            if (modifier.constant)
                                visualObject.SetText(text);
                            else
                                visualObject.Text = text;
                        }
                        break;
                    }
                case "removeTextOther":
                    {
                        var list = DataManager.inst.gameData.beatmapObjects.Where(x => (x as BeatmapObject).tags.Contains(modifier.commands[1]));

                        if (list.Count() > 0 && int.TryParse(modifier.value, out int remove))
                        {
                            foreach (var bm in list.Select(x => x as BeatmapObject))
                            {
                                if (bm.shape == 4 && bm.levelObject && bm.levelObject.visualObject != null &&
                                    bm.levelObject.visualObject is TextObject textObject)
                                {
                                    string text = string.IsNullOrEmpty(textObject.TextMeshPro.text) ? "" :
                                        textObject.TextMeshPro.text.Substring(0, textObject.TextMeshPro.text.Length - Mathf.Clamp(remove, 0, textObject.TextMeshPro.text.Length - 1));

                                    if (modifier.constant)
                                        textObject.SetText(text);
                                    else
                                        textObject.Text = text;
                                }
                            }
                        }
                        break;
                    }
                case "removeTextAt":
                    {
                        if (modifier.reference.shape == 4 && modifier.reference.levelObject && modifier.reference.levelObject.visualObject != null &&
                            modifier.reference.levelObject.visualObject is TextObject && int.TryParse(modifier.value, out int remove))
                        {
                            var visualObject = (TextObject)modifier.reference.levelObject.visualObject;
                            string text = string.IsNullOrEmpty(visualObject.TextMeshPro.text) ? "" : visualObject.TextMeshPro.text.Length > remove ?
                                visualObject.TextMeshPro.text.Remove(remove, 1) : "";

                            if (modifier.constant)
                                visualObject.SetText(text);
                            else
                                visualObject.Text = text;
                        }
                        break;
                    }
                case "removeTextOtherAt":
                    {
                        var list = DataManager.inst.gameData.beatmapObjects.Where(x => (x as BeatmapObject).tags.Contains(modifier.commands[1]));

                        if (list.Count() > 0 && int.TryParse(modifier.value, out int remove))
                        {
                            foreach (var bm in list.Select(x => x as BeatmapObject))
                            {
                                if (bm.shape == 4 && bm.levelObject && bm.levelObject.visualObject != null &&
                                    bm.levelObject.visualObject is TextObject textObject)
                                {
                                    string text = string.IsNullOrEmpty(textObject.TextMeshPro.text) ? "" : textObject.TextMeshPro.text.Length > remove ?
                                        textObject.TextMeshPro.text.Remove(remove, 1) : "";

                                    if (modifier.constant)
                                        textObject.SetText(text);
                                    else
                                        textObject.Text = text;
                                }
                            }
                        }
                        break;
                    }
                case "clampVariable":
                    {
                        modifier.reference.integerVariable = Mathf.Clamp(modifier.reference.integerVariable, Parser.TryParse(modifier.commands.Count > 1 ? modifier.commands[1] : "1", 0), Parser.TryParse(modifier.commands.Count > 2 ? modifier.commands[2] : "1", 1));
                        break;
                    }
                case "clampVariableOther":
                    {
                        var list = DataManager.inst.gameData.beatmapObjects.Where(x => (x as BeatmapObject).tags.Contains(modifier.commands[1]));

                        if (list.Count() > 0)
                        {
                            foreach (var bm in list.Select(x => x as BeatmapObject))
                            {
                                bm.integerVariable = Mathf.Clamp(bm.integerVariable, Parser.TryParse(modifier.commands.Count > 1 ? modifier.commands[1] : "1", 0), Parser.TryParse(modifier.commands.Count > 2 ? modifier.commands[2] : "1", 1));
                            }
                        }

                        break;
                    }
                case "animateObject":
                    {
                        if (int.TryParse(modifier.commands[1], out int type)
                            && float.TryParse(modifier.commands[2], out float x) && float.TryParse(modifier.commands[3], out float y) && float.TryParse(modifier.commands[4], out float z)
                            && bool.TryParse(modifier.commands[5], out bool relative) && float.TryParse(modifier.value, out float time))
                        {
                            string easing = modifier.commands[6];
                            if (int.TryParse(modifier.commands[6], out int e) && e >= 0 && e < DataManager.inst.AnimationList.Count)
                                easing = DataManager.inst.AnimationList[e].Name;

                            Vector3 vector;
                            if (type == 0)
                                vector = modifier.reference.positionOffset;
                            else if (type == 1)
                                vector = modifier.reference.scaleOffset;
                            else
                                vector = modifier.reference.rotationOffset;

                            var setVector = new Vector3(x, y, z) + (relative ? vector : Vector3.zero);

                            if (!modifier.constant)
                            {
                                var animation = new RTAnimation("Animate Object Offset");

                                animation.animationHandlers = new List<AnimationHandlerBase>
                                {
                                    new AnimationHandler<Vector3>(new List<IKeyframe<Vector3>>
                                    {
                                        new Vector3Keyframe(0f, vector, Ease.Linear),
                                        new Vector3Keyframe(Mathf.Clamp(time, 0f, 9999f), setVector,
                                        Ease.HasEaseFunction(easing) ? Ease.GetEaseFunction(easing) : Ease.Linear),
                                            new Vector3Keyframe(Mathf.Clamp(time, 0f, 9999f) + 0.1f, setVector, Ease.Linear),
                                    }, delegate (Vector3 vector3)
                                    {
                                        if (type == 0)
                                            modifier.reference.positionOffset = vector3;
                                        else if (type == 1)
                                            modifier.reference.scaleOffset = vector3;
                                        else
                                            modifier.reference.rotationOffset = vector3;
                                    }),
                                };
                                animation.onComplete = delegate ()
                                {
                                    AnimationManager.inst.RemoveID(animation.id);
                                };
                                AnimationManager.inst.Play(animation);
                            }
                            else
                            {
                                if (type == 0)
                                    modifier.reference.positionOffset = setVector;
                                else if (type == 1)
                                    modifier.reference.scaleOffset = setVector;
                                else
                                    modifier.reference.rotationOffset = setVector;
                            }
                        }

                        break;
                    }
                case "animateObjectOther":
                    {
                        var list = DataManager.inst.gameData.beatmapObjects.Where(x => (x as BeatmapObject).tags.Contains(modifier.commands[7]));

                        if (list.Count() > 0 && int.TryParse(modifier.commands[1], out int type)
                            && float.TryParse(modifier.commands[2], out float x) && float.TryParse(modifier.commands[3], out float y) && float.TryParse(modifier.commands[4], out float z)
                            && bool.TryParse(modifier.commands[5], out bool relative) && float.TryParse(modifier.value, out float time))
                        {
                            string easing = modifier.commands[6];
                            if (int.TryParse(modifier.commands[6], out int e) && e >= 0 && e < DataManager.inst.AnimationList.Count)
                                easing = DataManager.inst.AnimationList[e].Name;

                            foreach (var bm in list.Select(x => x as BeatmapObject))
                            {
                                Vector3 vector;
                                if (type == 0)
                                    vector = bm.positionOffset;
                                else if (type == 1)
                                    vector = bm.scaleOffset;
                                else
                                    vector = bm.rotationOffset;

                                var setVector = new Vector3(x, y, z) + (relative ? vector : Vector3.zero);

                                if (!modifier.constant)
                                {
                                    var animation = new RTAnimation("Animate Other Object Offset");

                                    animation.animationHandlers = new List<AnimationHandlerBase>
                                    {
                                        new AnimationHandler<Vector3>(new List<IKeyframe<Vector3>>
                                        {
                                            new Vector3Keyframe(0f, vector, Ease.Linear),
                                            new Vector3Keyframe(Mathf.Clamp(time, 0f, 9999f), setVector,
                                            Ease.HasEaseFunction(easing) ? Ease.GetEaseFunction(easing) : Ease.Linear),
                                            new Vector3Keyframe(Mathf.Clamp(time, 0f, 9999f) + 0.1f, setVector, Ease.Linear),
                                        }, delegate (Vector3 vector3)
                                        {
                                            if (type == 0)
                                                bm.positionOffset = vector3;
                                            else if (type == 1)
                                                bm.scaleOffset = vector3;
                                            else
                                                bm.rotationOffset = vector3;
                                        }),
                                    };
                                    animation.onComplete = delegate ()
                                    {
                                        AnimationManager.inst.RemoveID(animation.id);
                                    };
                                    AnimationManager.inst.Play(animation);
                                }
                                else
                                {
                                    if (type == 0)
                                        bm.positionOffset = setVector;
                                    else if (type == 1)
                                        bm.scaleOffset = setVector;
                                    else
                                        bm.rotationOffset = setVector;
                                }
                            }
                        }

                        break;
                    }
                case "animateSignal":
                    {
                        if (int.TryParse(modifier.commands[1], out int type)
                            && float.TryParse(modifier.commands[2], out float x) && float.TryParse(modifier.commands[3], out float y) && float.TryParse(modifier.commands[4], out float z)
                            && bool.TryParse(modifier.commands[5], out bool relative) && float.TryParse(modifier.value, out float time))
                        {
                            if (!Parser.TryParse(modifier.commands[9], true))
                            {
                                var list = DataManager.inst.gameData.beatmapObjects.Where(x => (x as BeatmapObject).tags.Contains(modifier.commands[7]));

                                foreach (var bm in list)
                                {
                                    if ((bm as BeatmapObject).modifiers.Count > 0 && (bm as BeatmapObject).modifiers.Where(x => x.commands[0] == "requireSignal" && x.type == ModifierBase.Type.Trigger).Count() > 0 &&
                                        (bm as BeatmapObject).modifiers.TryFind(x => x.commands[0] == "requireSignal" && x.type == ModifierBase.Type.Trigger, out Modifier<BeatmapObject> m))
                                    {
                                        m.Result = null;
                                    }
                                }
                            }

                            string easing = modifier.commands[6];
                            if (int.TryParse(modifier.commands[6], out int e) && e >= 0 && e < DataManager.inst.AnimationList.Count)
                                easing = DataManager.inst.AnimationList[e].Name;

                            Vector3 vector;
                            if (type == 0)
                                vector = modifier.reference.positionOffset;
                            else if (type == 1)
                                vector = modifier.reference.scaleOffset;
                            else
                                vector = modifier.reference.rotationOffset;

                            var setVector = new Vector3(x, y, z) + (relative ? vector : Vector3.zero);

                            if (!modifier.constant)
                            {
                                var animation = new RTAnimation("Animate Object Offset");

                                animation.animationHandlers = new List<AnimationHandlerBase>
                                {
                                    new AnimationHandler<Vector3>(new List<IKeyframe<Vector3>>
                                    {
                                        new Vector3Keyframe(0f, vector, Ease.Linear),
                                        new Vector3Keyframe(Mathf.Clamp(time, 0f, 9999f), setVector,
                                        Ease.HasEaseFunction(easing) ? Ease.GetEaseFunction(easing) : Ease.Linear),
                                            new Vector3Keyframe(Mathf.Clamp(time, 0f, 9999f) + 0.1f, setVector, Ease.Linear),
                                    }, delegate (Vector3 vector3)
                                    {
                                        if (type == 0)
                                            modifier.reference.positionOffset = vector3;
                                        else if (type == 1)
                                            modifier.reference.scaleOffset = vector3;
                                        else
                                            modifier.reference.rotationOffset = vector3;
                                    }),
                                };
                                animation.onComplete = delegate ()
                                {
                                    AnimationManager.inst.RemoveID(animation.id);

                                    var list = DataManager.inst.gameData.beatmapObjects.Where(x => (x as BeatmapObject).tags.Contains(modifier.commands[7]));

                                    foreach (var bm in list)
                                    {
                                        CoreHelper.StartCoroutine(ModifiersManager.ActivateModifier((BeatmapObject)bm, Parser.TryParse(modifier.commands[8], 0f)));
                                    }
                                };
                                AnimationManager.inst.Play(animation);
                            }
                            else
                            {
                                if (type == 0)
                                    modifier.reference.positionOffset = setVector;
                                else if (type == 1)
                                    modifier.reference.scaleOffset = setVector;
                                else
                                    modifier.reference.rotationOffset = setVector;
                            }
                        }

                        break;
                    }
                case "animateSignalOther":
                    {
                        var list = DataManager.inst.gameData.beatmapObjects.Where(x => (x as BeatmapObject).tags.Contains(modifier.commands[7]));

                        if (list.Count() > 0 && int.TryParse(modifier.commands[1], out int type)
                            && float.TryParse(modifier.commands[2], out float x) && float.TryParse(modifier.commands[3], out float y) && float.TryParse(modifier.commands[4], out float z)
                            && bool.TryParse(modifier.commands[5], out bool relative) && float.TryParse(modifier.value, out float time))
                        {
                            if (!Parser.TryParse(modifier.commands[10], true))
                            {
                                var list2 = DataManager.inst.gameData.beatmapObjects.Where(x => (x as BeatmapObject).tags.Contains(modifier.commands[8]));

                                foreach (var bm in list2)
                                {
                                    if ((bm as BeatmapObject).modifiers.Count > 0 && (bm as BeatmapObject).modifiers.Where(x => x.commands[0] == "requireSignal" && x.type == ModifierBase.Type.Trigger).Count() > 0 &&
                                        (bm as BeatmapObject).modifiers.TryFind(x => x.commands[0] == "requireSignal" && x.type == ModifierBase.Type.Trigger, out Modifier<BeatmapObject> m))
                                    {
                                        m.Result = null;
                                    }
                                }
                            }

                            string easing = modifier.commands[6];
                            if (int.TryParse(modifier.commands[6], out int e) && e >= 0 && e < DataManager.inst.AnimationList.Count)
                                easing = DataManager.inst.AnimationList[e].Name;

                            foreach (var bm in list.Select(x => x as BeatmapObject))
                            {
                                Vector3 vector;
                                if (type == 0)
                                    vector = bm.positionOffset;
                                else if (type == 1)
                                    vector = bm.scaleOffset;
                                else
                                    vector = bm.rotationOffset;

                                var setVector = new Vector3(x, y, z) + (relative ? vector : Vector3.zero);

                                if (!modifier.constant)
                                {
                                    var animation = new RTAnimation("Animate Other Object Offset");

                                    animation.animationHandlers = new List<AnimationHandlerBase>
                                    {
                                        new AnimationHandler<Vector3>(new List<IKeyframe<Vector3>>
                                        {
                                            new Vector3Keyframe(0f, vector, Ease.Linear),
                                            new Vector3Keyframe(Mathf.Clamp(time, 0f, 9999f), setVector,
                                            Ease.HasEaseFunction(easing) ? Ease.GetEaseFunction(easing) : Ease.Linear),
                                            new Vector3Keyframe(Mathf.Clamp(time, 0f, 9999f) + 0.1f, setVector, Ease.Linear),
                                        }, delegate (Vector3 vector3)
                                        {
                                            if (type == 0)
                                                bm.positionOffset = vector3;
                                            else if (type == 1)
                                                bm.scaleOffset = vector3;
                                            else
                                                bm.rotationOffset = vector3;
                                        }),
                                    };
                                    animation.onComplete = delegate ()
                                    {
                                        AnimationManager.inst.RemoveID(animation.id);

                                        var list = DataManager.inst.gameData.beatmapObjects.Where(x => (x as BeatmapObject).tags.Contains(modifier.commands[8]));

                                        foreach (var bm in list)
                                        {
                                            CoreHelper.StartCoroutine(ModifiersManager.ActivateModifier((BeatmapObject)bm, Parser.TryParse(modifier.commands[9], 0f)));
                                        }
                                    };
                                    AnimationManager.inst.Play(animation);
                                }
                                else
                                {
                                    if (type == 0)
                                        bm.positionOffset = setVector;
                                    else if (type == 1)
                                        bm.scaleOffset = setVector;
                                    else
                                        bm.rotationOffset = setVector;
                                }
                            }
                        }

                        break;
                    }
                case "rigidbody":
                    {
                        if (modifier.reference.levelObject && modifier.reference.levelObject.visualObject != null
                            && float.TryParse(modifier.commands[1], out float gravity)
                            && int.TryParse(modifier.commands[2], out int collisionMode)
                            && float.TryParse(modifier.commands[3], out float drag)
                            && float.TryParse(modifier.commands[4], out float velocityX)
                            && float.TryParse(modifier.commands[5], out float velocityY))
                        {
                            modifier.reference.components.RemoveAll(x => x == null);

                            if (!modifier.reference.components.Has(x => x is Rigidbody2D))
                            {
                                var rigidbody = modifier.reference.levelObject.visualObject.GameObject.GetComponent<Rigidbody2D>();

                                if (!rigidbody)
                                    rigidbody = modifier.reference.levelObject.visualObject.GameObject.AddComponent<Rigidbody2D>();

                                modifier.reference.components.Add(rigidbody);

                                rigidbody.gravityScale = gravity;
                                rigidbody.collisionDetectionMode = (CollisionDetectionMode2D)Mathf.Clamp(collisionMode, 0, 1);
                                rigidbody.drag = drag;

                                rigidbody.bodyType = (RigidbodyType2D)Parser.TryParse(modifier.commands[6], 0);

                                var velocity = rigidbody.velocity;
                                velocity.x += velocityX;
                                velocity.y += velocityY;
                                rigidbody.velocity = velocity;
                            }

                            if (!modifier.constant && modifier.reference.components.Has(x => x is Rigidbody2D))
                            {
                                var rigidbody = (Rigidbody2D)modifier.reference.components.Find(x => x is Rigidbody2D);

                                rigidbody.gravityScale = gravity;
                                rigidbody.collisionDetectionMode = (CollisionDetectionMode2D)Mathf.Clamp(collisionMode, 0, 1);
                                rigidbody.drag = drag;

                                rigidbody.bodyType = (RigidbodyType2D)Parser.TryParse(modifier.commands[6], 0);

                                rigidbody.velocity += new Vector2(velocityX, velocityY);
                            }
                        }

                        break;
                    }
                case "rigidbodyOther":
                    {
                        var list = DataManager.inst.gameData.beatmapObjects.Where(x => (x as BeatmapObject).tags.Contains(modifier.value));

                        if (list.Count() > 0
                                    && float.TryParse(modifier.commands[1], out float gravity)
                                    && int.TryParse(modifier.commands[2], out int collisionMode)
                                    && float.TryParse(modifier.commands[3], out float drag)
                                    && float.TryParse(modifier.commands[4], out float velocityX)
                                    && float.TryParse(modifier.commands[5], out float velocityY))
                        {
                            foreach (var bm in list.Select(x => x as BeatmapObject))
                            {
                                if (bm.levelObject && bm.levelObject.visualObject != null)
                                {
                                    if (bm.components.Has(x => x is Rigidbody2D))
                                    {
                                        var rigidbody = (Rigidbody2D)bm.components.Find(x => x is Rigidbody2D);

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
                case "gravity":
                    {
                        if (float.TryParse(modifier.commands[1], out float gravityX) && float.TryParse(modifier.commands[2], out float gravityY)
                            && modifier.reference && Updater.TryGetObject(modifier.reference, out LevelObject levelObject))
                        {
                            var list = levelObject.parentObjects;
                            float rotation = 0f;

                            for (int i = 1; i < list.Count; i++)
                                rotation += list[i].Transform.localRotation.eulerAngles.z;

                            if (modifier.Result == null)
                            {
                                modifier.Result = new Vector2(gravityX / 1000f, gravityY / 1000f);
                            }
                            else
                            {
                                var f = (Vector2)modifier.Result;

                                f *= new Vector2(gravityX, gravityY);

                                modifier.Result = f;
                            }

                            modifier.reference.positionOffset = RTMath.Rotate((Vector2)modifier.Result, (list[0].Transform.localRotation.eulerAngles.z - rotation));
                        }

                        break;
                    }
                case "gravityOther":
                    {
                        var beatmapObjects = DataManager.inst.gameData.beatmapObjects.Where(x => (x as BeatmapObject).tags.Contains(modifier.value));

                        if (beatmapObjects.Count() > 0 && float.TryParse(modifier.commands[1], out float gravityX) && float.TryParse(modifier.commands[2], out float gravityY))
                        {
                            foreach (var bm in beatmapObjects.Select(x => x as BeatmapObject))
                            {
                                if (Updater.TryGetObject(bm, out LevelObject levelObject))
                                {
                                    var list = levelObject.parentObjects;
                                    float rotation = 0f;

                                    for (int i = 1; i < list.Count; i++)
                                        rotation += list[i].Transform.localRotation.eulerAngles.z;

                                    if (modifier.Result == null)
                                    {
                                        modifier.Result = new Vector2(gravityX / 1000f, gravityY / 1000f);
                                    }
                                    else
                                    {
                                        var f = (Vector2)modifier.Result;

                                        f *= new Vector2(gravityX, gravityY);

                                        modifier.Result = f;
                                    }

                                    bm.positionOffset = RTMath.Rotate((Vector2)modifier.Result, (list[0].Transform.localRotation.eulerAngles.z - rotation));
                                }

                            }
                        }

                        break;
                    }
                case "copyAxis":
                    {
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
                            && DataManager.inst.gameData.beatmapObjects.TryFind(x => (x as BeatmapObject).tags.Contains(modifier.value), out DataManager.GameData.BeatmapObject beatmapObject)
                            && beatmapObject != null)
                        {
                            var time = AudioManager.inst.CurrentAudioSource.time;

                            var bm = beatmapObject as BeatmapObject;

                            fromType = Mathf.Clamp(fromType, 0, bm.events.Count);
                            fromAxis = Mathf.Clamp(fromAxis, 0, bm.events[fromType][0].eventValues.Length);

                            if (!useVisual && Updater.levelProcessor.converter.cachedSequences.ContainsKey(bm.id))
                            {
                                if (toType >= 0 && toType < 3 && fromType == 0)
                                {
                                    var sequence = Updater.levelProcessor.converter.cachedSequences[bm.id].Position3DSequence.Interpolate(time - bm.StartTime - delay);
                                    float value = ((fromAxis == 0 ? sequence.x : fromAxis == 1 ? sequence.y : sequence.z) - offset) * multiply % loop;

                                    modifier.reference.SetTransform(toType, toAxis, Mathf.Clamp(value, min, max));
                                }

                                if (toType >= 0 && toType < 3 && fromType == 1)
                                {
                                    var sequence = Updater.levelProcessor.converter.cachedSequences[bm.id].ScaleSequence.Interpolate(time - bm.StartTime - delay);
                                    float value = ((fromAxis == 0 ? sequence.x : sequence.y) - offset) * multiply % loop;

                                    modifier.reference.SetTransform(toType, toAxis, Mathf.Clamp(value, min, max));
                                }

                                if (toType >= 0 && toType < 3 && fromType == 2)
                                {
                                    var sequence = (Updater.levelProcessor.converter.cachedSequences[bm.id].RotationSequence.Interpolate(time - bm.StartTime - delay) - offset) * multiply % loop;

                                    modifier.reference.SetTransform(toType, toAxis, Mathf.Clamp(sequence, min, max));
                                }

                                if (toType == 3 && toAxis == 0 && fromType == 3 && Updater.levelProcessor.converter.cachedSequences[bm.id].ColorSequence != null &&
                                    modifier.reference.levelObject && modifier.reference.levelObject.visualObject != null &&
                                    modifier.reference.levelObject.visualObject.Renderer)
                                {
                                    var sequence = Updater.levelProcessor.converter.cachedSequences[bm.id].ColorSequence.Interpolate(time - bm.StartTime - delay);

                                    var renderer = modifier.reference.levelObject.visualObject.Renderer;

                                    renderer.material.color = RTMath.Lerp(renderer.material.color, sequence, multiply);
                                }
                            }
                            else if (useVisual && Updater.TryGetObject(bm, out LevelObject levelObject) && levelObject.visualObject != null && levelObject.visualObject.GameObject)
                            {
                                var transform = levelObject.visualObject.GameObject.transform;

                                if (toType >= 0 && toType < 3 && fromType == 0)
                                {
                                    var sequence = transform.position;
                                    float value = ((fromAxis == 0 ? sequence.x : fromAxis == 1 ? sequence.y : sequence.z) - offset) * multiply % loop;

                                    modifier.reference.SetTransform(toType, toAxis, Mathf.Clamp(value, min, max));
                                }

                                if (toType >= 0 && toType < 3 && fromType == 1)
                                {
                                    var sequence = transform.lossyScale;
                                    float value = ((fromAxis == 0 ? sequence.x : fromAxis == 1 ? sequence.y : sequence.z) - offset) * multiply % loop;

                                    modifier.reference.SetTransform(toType, toAxis, Mathf.Clamp(value, min, max));
                                }

                                if (toType >= 0 && toType < 3 && fromType == 2)
                                {
                                    var sequence = transform.rotation.eulerAngles;
                                    float value = ((fromAxis == 0 ? sequence.x : fromAxis == 1 ? sequence.y : sequence.z) - offset) * multiply % loop;

                                    modifier.reference.SetTransform(toType, toAxis, Mathf.Clamp(value, min, max));
                                }
                            }
                        }

                        break;
                    }
                case "copyPlayerAxis":
                    {
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
                            && PlayerManager.Players.Count > 0)
                        {
                            var time = AudioManager.inst.CurrentAudioSource.time;

                            var player = PlayerManager.Players[0];
                            var rb = player.Player.playerObjects["RB Parent"].gameObject.transform;

                            // From Type Position
                            if (fromType == 0)
                            {
                                var sequence = rb.localPosition;

                                modifier.reference.SetTransform(toType, toAxis, Mathf.Clamp(((fromAxis == 0 ? sequence.x : fromAxis == 1 ? sequence.y : sequence.z) - offset) * multiply, min, max));
                            }

                            // From Type Scale
                            if (fromType == 1)
                            {
                                var sequence = rb.localScale;

                                modifier.reference.SetTransform(toType, toAxis, Mathf.Clamp(((fromAxis == 0 ? sequence.x : fromAxis == 1 ? sequence.y : sequence.z) - offset) * multiply, min, max));
                            }

                            // From Type Rotation
                            if (fromType == 2)
                            {
                                var sequence = rb.localRotation.eulerAngles;

                                modifier.reference.SetTransform(toType, toAxis, Mathf.Clamp(((fromAxis == 0 ? sequence.x : fromAxis == 1 ? sequence.y : sequence.z) - offset) * multiply, min, max));
                            }
                        }

                        break;
                    }
                case "setWindowTitle":
                    {
                        WindowController.SetTitle(modifier.value);

                        break;
                    }
                case "setDiscordStatus":
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
                                string.Format(modifier.value, MetaData.Current.song.title, $"{(EditorManager.inst == null ? "Game" : "Editor")}", $"{(EditorManager.inst == null ? "Level" : "Editing")}", $"{(EditorManager.inst == null ? "Arcade" : "Editor")}"),
                                string.Format(modifier.commands[1], MetaData.Current.song.title, $"{(EditorManager.inst == null ? "Game" : "Editor")}", $"{(EditorManager.inst == null ? "Level" : "Editing")}", $"{(EditorManager.inst == null ? "Arcade" : "Editor")}"),
                                discordSubIcons[Mathf.Clamp(discordSubIcon, 0, discordSubIcons.Length - 1)], discordIcons[Mathf.Clamp(discordIcon, 0, discordIcons.Length - 1)]);

                        break;
                    }
                case "saveLevelRank":
                    {
                        if (EditorManager.inst || modifier.constant || !LevelManager.CurrentLevel)
                            break;

                        int prevHits = LevelManager.CurrentLevel.playerData != null ? LevelManager.CurrentLevel.playerData.Hits : -1;

                        if (LevelManager.Saves.Where(x => x.Completed).Count() >= 100)
                        {
                            SteamWrapper.inst.achievements.SetAchievement("GREAT_TESTER");
                        }

                        if (!PlayerManager.IsZenMode && !PlayerManager.IsPractice)
                        {
                            if (LevelManager.CurrentLevel.playerData == null)
                            {
                                LevelManager.CurrentLevel.playerData = new LevelManager.PlayerData
                                {
                                    ID = LevelManager.CurrentLevel.id,
                                };
                            }

                            if (LevelManager.CurrentLevel.playerData.Deaths < GameManager.inst.deaths.Count)
                                LevelManager.CurrentLevel.playerData.Deaths = GameManager.inst.deaths.Count;
                            if (LevelManager.CurrentLevel.playerData.Hits < GameManager.inst.hits.Count)
                                LevelManager.CurrentLevel.playerData.Hits = GameManager.inst.hits.Count;
                            LevelManager.CurrentLevel.playerData.Completed = true;
                            if (LevelManager.CurrentLevel.playerData.Boosts < LevelManager.BoostCount)
                                LevelManager.CurrentLevel.playerData.Boosts = LevelManager.BoostCount;

                            if (LevelManager.Saves.Has(x => x.ID == LevelManager.CurrentLevel.id))
                            {
                                var saveIndex = LevelManager.Saves.FindIndex(x => x.ID == LevelManager.CurrentLevel.id);
                                LevelManager.Saves[saveIndex] = LevelManager.CurrentLevel.playerData;
                            }
                            else
                                LevelManager.Saves.Add(LevelManager.CurrentLevel.playerData);
                        }

                        LevelManager.SaveProgress();

                        break;
                    }
                case "customCode":
                    {
                        //string code = "void Action() { Log(0f); Pause(); }";
                        var code = modifier.value;

                        var matchCollection = Regex.Matches(code, "{(.*?)}");

                        if (matchCollection.Count > 0)
                        {
                            foreach (var obj in matchCollection)
                            {
                                var match = (Match)obj;

                                var str = match.Groups[1].ToString();

                                var array = str.Split(new string[] { ";" }, StringSplitOptions.RemoveEmptyEntries);

                                if (array.Length - 1 > 0)
                                    for (int i = 0; i < array.Length - 1; i++)
                                    {
                                        var methodName = array[i].Substring(0, array[i].IndexOf('('));

                                        var parameters = Regex.Match(array[i], @"\((.*?)\)").Groups[1].ToString().Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);

                                        if (methodName.ToLower() == "log" && parameters.Length == 1)
                                            Debug.Log($"{parameters[0]}");
                                    }
                            }
                        }

                        break;
                    }
            }
        }

        public static void Inactive(Modifier<BeatmapObject> modifier)
        {
            if (!modifier.verified)
            {
                modifier.verified = true;
                modifier.VerifyModifier(ModifiersManager.modifierTypes);
            }

            if (!modifier.IsValid(ModifiersManager.modifierTypes))
                return;

            switch (modifier.commands[0])
            {
                case "blur":
                case "blurOther":
                case "blurVariableOther":
                    {
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
                case "blurVariable":
                    {
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
                    {
                        if (!modifier.constant && modifier.Result != null && modifier.Result is PrefabObject)
                        {
                            Updater.UpdatePrefab((PrefabObject)modifier.Result, false);

                            DataManager.inst.gameData.prefabObjects.RemoveAll(x => ((PrefabObject)x).fromModifier && x.ID == ((PrefabObject)modifier.Result).ID);

                            modifier.Result = null;
                        }
                        break;
                    }
                case "enableObject":
                    {
                        if (Updater.TryGetObject(modifier.reference, out LevelObject levelObject) && levelObject.transformChain != null && levelObject.transformChain.Count > 0 && levelObject.transformChain[0] != null)
                        {
                            levelObject.transformChain[0].gameObject.SetActive(false);
                        }
                        break;
                    }
                case "enableObjectOther":
                    {
                        var list = DataManager.inst.gameData.beatmapObjects.Where(x => (x as BeatmapObject).tags.Contains(modifier.value));

                        if (list.Count() > 0)
                        {
                            foreach (var beatmapObject in list)
                            {
                                if (Updater.TryGetObject(beatmapObject, out LevelObject levelObject) && levelObject.transformChain != null && levelObject.transformChain.Count > 0 && levelObject.transformChain[0] != null)
                                {
                                    levelObject.transformChain[0].gameObject.SetActive(false);
                                }
                            }
                        }

                        break;
                    }
                case "enableObjectTree":
                    {
                        if (modifier.value == "0")
                            modifier.value = "False";

                        if (modifier.Result == null)
                        {
                            var beatmapObject = Parser.TryParse(modifier.value, true) ? modifier.reference : modifier.reference.GetParentChain().Last();

                            modifier.Result = beatmapObject.GetChildChain();
                        }

                        var list = (List<List<DataManager.GameData.BeatmapObject>>)modifier.Result;

                        for (int i = 0; i < list.Count; i++)
                        {
                            var childList = list[i];
                            for (int j = 0; j < childList.Count; j++)
                            {
                                if (childList[j] != null && Updater.TryGetObject(childList[j], out LevelObject levelObject) && levelObject.transformChain != null && levelObject.transformChain.Count > 0 && levelObject.transformChain[0] != null)
                                {
                                    levelObject.transformChain[0].gameObject.SetActive(false);
                                }
                            }
                        }

                        break;
                    }
                case "enableObjectTreeOther":
                    {
                        if (modifier.Result == null)
                        {
                            var beatmapObjects = DataManager.inst.gameData.beatmapObjects.Where(x => (x as BeatmapObject).tags.Contains(modifier.commands[1]));

                            var resultList = new List<List<DataManager.GameData.BeatmapObject>>();
                            foreach (var bm in beatmapObjects)
                            {
                                var beatmapObject = Parser.TryParse(modifier.value, true) ? bm : bm.GetParentChain().Last();
                                resultList.AddRange(beatmapObject.GetChildChain());
                            }

                            modifier.Result = resultList;
                        }

                        var list = (List<List<DataManager.GameData.BeatmapObject>>)modifier.Result;

                        for (int i = 0; i < list.Count; i++)
                        {
                            var childList = list[i];
                            for (int j = 0; j < childList.Count; j++)
                            {
                                if (childList[j] != null && Updater.TryGetObject(childList[j], out LevelObject levelObject) && levelObject.transformChain != null && levelObject.transformChain.Count > 0 && levelObject.transformChain[0] != null)
                                {
                                    levelObject.transformChain[0].gameObject.SetActive(false);
                                }
                            }
                        }

                        modifier.Result = null;

                        break;
                    }
                case "disableObject":
                    {
                        if (!modifier.hasChanged && modifier.reference != null && Updater.TryGetObject(modifier.reference, out LevelObject levelObject) && levelObject.transformChain != null && levelObject.transformChain.Count > 0 && levelObject.transformChain[0] != null)
                        {
                            levelObject.transformChain[0].gameObject.SetActive(true);
                            modifier.hasChanged = true;
                        }

                        break;
                    }
                case "disableObjectOther":
                    {
                        var list = DataManager.inst.gameData.beatmapObjects.Where(x => (x as BeatmapObject).tags.Contains(modifier.value));

                        if (list.Count() > 0)
                        {
                            foreach (var beatmapObject in list)
                            {
                                if (Updater.TryGetObject(beatmapObject, out LevelObject levelObject) && levelObject.transformChain != null && levelObject.transformChain.Count > 0 && levelObject.transformChain[0] != null)
                                {
                                    levelObject.transformChain[0].gameObject.SetActive(true);
                                }
                            }
                        }

                        break;
                    }
                case "disableObjectTree":
                    {
                        if (modifier.value == "0")
                            modifier.value = "False";

                        if (Parser.TryParse(modifier.value, true))
                        {
                            foreach (var cc in modifier.reference.GetChildChain())
                            {
                                for (int o = 0; o < cc.Count; o++)
                                {
                                    if (cc[o] != null && Updater.TryGetObject(cc[o], out LevelObject levelObject) && levelObject.transformChain != null && levelObject.transformChain.Count > 0 && levelObject.transformChain[0] != null)
                                    {
                                        levelObject.transformChain[0].gameObject.SetActive(true);
                                    }
                                }
                            }

                            break;
                        }

                        var parentChain = modifier.reference.GetParentChain();

                        foreach (var cc in parentChain[parentChain.Count - 1].GetChildChain())
                        {
                            for (int o = 0; o < cc.Count; o++)
                            {
                                if (cc[o] != null && Updater.TryGetObject(cc[o], out LevelObject levelObject) && levelObject.transformChain != null && levelObject.transformChain.Count > 0 && levelObject.transformChain[0] != null)
                                    levelObject.transformChain[0].gameObject.SetActive(true);
                            }
                        }

                        break;
                    }
                case "disableObjectTreeOther":
                    {
                        if (modifier.Result == null)
                        {
                            var beatmapObjects = DataManager.inst.gameData.beatmapObjects.Where(x => (x as BeatmapObject).tags.Contains(modifier.commands[1]));

                            var resultList = new List<List<DataManager.GameData.BeatmapObject>>();
                            foreach (var bm in beatmapObjects)
                            {
                                var beatmapObject = Parser.TryParse(modifier.value, true) ? bm : bm.GetParentChain().Last();
                                resultList.AddRange(beatmapObject.GetChildChain());
                            }

                            modifier.Result = resultList;
                        }

                        var list = (List<List<DataManager.GameData.BeatmapObject>>)modifier.Result;

                        for (int i = 0; i < list.Count; i++)
                        {
                            var childList = list[i];
                            for (int j = 0; j < childList.Count; j++)
                            {
                                if (childList[j] != null && Updater.TryGetObject(childList[j], out LevelObject levelObject) && levelObject.transformChain != null && levelObject.transformChain.Count > 0 && levelObject.transformChain[0] != null)
                                {
                                    levelObject.transformChain[0].gameObject.SetActive(true);
                                }
                            }
                        }

                        modifier.Result = null;

                        break;
                    }
                case "reactivePosChain":
                    {
                        modifier.reference.reactivePositionOffset = Vector3.zero;

                        break;
                    }
                case "reactiveScaChain":
                    {
                        modifier.reference.reactiveScaleOffset = Vector3.zero;

                        break;
                    }
                case "reactiveRotChain":
                    {
                        modifier.reference.reactiveRotationOffset = 0f;

                        break;
                    }
                case "signalModifier":
                case "mouseOverSignalModifier":
                    {
                        var list = DataManager.inst.gameData.beatmapObjects.Where(x => x is BeatmapObject beatmapObject && beatmapObject.tags.Contains(modifier.commands[1]) && beatmapObject.modifiers.Any(y => y.Result != null));

                        if (list.Count() > 0)
                            foreach (var bm in list)
                            {
                                if ((bm as BeatmapObject).modifiers.Count > 0 && (bm as BeatmapObject).modifiers.Where(x => x.commands[0] == "requireSignal" && x.type == ModifierBase.Type.Trigger).Count() > 0 &&
                                    (bm as BeatmapObject).modifiers.TryFind(x => x.commands[0] == "requireSignal" && x.type == ModifierBase.Type.Trigger, out Modifier<BeatmapObject> m))
                                {
                                    m.Result = null;
                                }
                            }

                        break;
                    }
                case "animateSignal":
                case "animateSignalOther":
                    {
                        if (!Parser.TryParse(modifier.commands[!modifier.commands[0].Contains("Other") ? 9 : 10], true))
                            return;

                        var list = DataManager.inst.gameData.beatmapObjects.Where(x => (x as BeatmapObject).tags.Contains(modifier.commands[!modifier.commands[0].Contains("Other") ? 7 : 8]));

                        if (list.Count() > 0 && !modifier.constant)
                            foreach (var bm in list)
                            {
                                if ((bm as BeatmapObject).modifiers.Count > 0 && (bm as BeatmapObject).modifiers.Where(x => x.commands[0] == "requireSignal" && x.type == ModifierBase.Type.Trigger).Count() > 0 &&
                                    (bm as BeatmapObject).modifiers.TryFind(x => x.commands[0] == "requireSignal" && x.type == ModifierBase.Type.Trigger, out Modifier<BeatmapObject> m))
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
                case "gravityOther":
                    {
                        modifier.Result = null;
                        break;
                    }
                case "setText":
                    {
                        if (modifier.constant && modifier.reference.shape == 4 && modifier.reference.levelObject && modifier.reference.levelObject.visualObject != null &&
                            modifier.reference.levelObject.visualObject is TextObject textObject)
                            textObject.Text = modifier.reference.text;
                        break;
                    }
                case "setTextOther":
                    {
                        var list = DataManager.inst.gameData.beatmapObjects.Where(x => (x as BeatmapObject).tags.Contains(modifier.commands[1]));

                        if (modifier.constant && list.Count() > 0)
                        {
                            foreach (var bm in list.Select(x => x as BeatmapObject))
                            {
                                if (bm.shape == 4 && bm.levelObject && bm.levelObject.visualObject != null &&
                                    bm.levelObject.visualObject is TextObject textObject)
                                    textObject.Text = bm.text;
                            }
                        }
                        break;
                    }
            }
        }

        public static bool BGTrigger(Modifier<BackgroundObject> modifier)
        {
            if (!modifier.verified)
            {
                modifier.verified = true;
                modifier.VerifyModifier(ModifiersManager.bgModifierTypes);
            }

            if (!modifier.IsValid(ModifiersManager.bgModifierTypes))
                return false;

            switch (modifier.commands[0])
            {
                case "timeLesserEquals":
                    {
                        return float.TryParse(modifier.value, out float t) && AudioManager.inst.CurrentAudioSource.time <= t;
                    }
                case "timeGreaterEquals":
                    {
                        return float.TryParse(modifier.value, out float t) && AudioManager.inst.CurrentAudioSource.time >= t;
                    }
                case "timeLesser":
                    {
                        return float.TryParse(modifier.value, out float t) && AudioManager.inst.CurrentAudioSource.time < t;
                    }
                case "timeGreater":
                    {
                        return float.TryParse(modifier.value, out float t) && AudioManager.inst.CurrentAudioSource.time > t;
                    }
            }

            return false;
        }

        public static void BGAction(Modifier<BackgroundObject> modifier)
        {
            if (!modifier.verified)
            {
                modifier.verified = true;
                modifier.VerifyModifier(ModifiersManager.bgModifierTypes);
            }

            if (!modifier.IsValid(ModifiersManager.bgModifierTypes))
                return;

            modifier.hasChanged = false;
            switch (modifier.commands[0])
            {
                case "setActive":
                    {
                        if (bool.TryParse(modifier.value, out bool active))
                            modifier.reference.Enabled = active;

                        break;
                    }
                case "animateObject":
                    {
                        if (int.TryParse(modifier.commands[1], out int type)
                            && float.TryParse(modifier.commands[2], out float x) && float.TryParse(modifier.commands[3], out float y) && float.TryParse(modifier.commands[4], out float z)
                            && bool.TryParse(modifier.commands[5], out bool relative) && float.TryParse(modifier.value, out float time))
                        {
                            string easing = modifier.commands[6];
                            if (int.TryParse(modifier.commands[6], out int e) && e >= 0 && e < DataManager.inst.AnimationList.Count)
                                easing = DataManager.inst.AnimationList[e].Name;

                            Vector3 vector;
                            if (type == 0)
                                vector = modifier.reference.positionOffset;
                            else if (type == 1)
                                vector = modifier.reference.scaleOffset;
                            else
                                vector = modifier.reference.rotationOffset;

                            var setVector = new Vector3(x, y, z) + (relative ? vector : Vector3.zero);

                            if (!modifier.constant)
                            {
                                var animation = new RTAnimation("Animate Object Offset");

                                animation.animationHandlers = new List<AnimationHandlerBase>
                                {
                                    new AnimationHandler<Vector3>(new List<IKeyframe<Vector3>>
                                    {
                                        new Vector3Keyframe(0f, vector, Ease.Linear),
                                        new Vector3Keyframe(Mathf.Clamp(time, 0f, 9999f), setVector,
                                        Ease.HasEaseFunction(easing) ? Ease.GetEaseFunction(easing) : Ease.Linear),
                                            new Vector3Keyframe(Mathf.Clamp(time, 0f, 9999f) + 0.1f, setVector, Ease.Linear),
                                    }, delegate (Vector3 vector3)
                                    {
                                        if (type == 0)
                                            modifier.reference.positionOffset = vector3;
                                        else if (type == 1)
                                            modifier.reference.scaleOffset = vector3;
                                        else
                                            modifier.reference.rotationOffset = vector3;
                                    }),
                                };
                                animation.onComplete = delegate ()
                                {
                                    AnimationManager.inst.RemoveID(animation.id);
                                };
                                AnimationManager.inst.Play(animation);
                            }
                            else
                            {
                                if (type == 0)
                                    modifier.reference.positionOffset = setVector;
                                else if (type == 1)
                                    modifier.reference.scaleOffset = setVector;
                                else
                                    modifier.reference.rotationOffset = setVector;
                            }
                        }

                        break;
                    }
                case "copyAxis":
                    {
                        /*
                        From Type: (Pos / Sca / Rot)
                        From Axis: (X / Y / Z)
                        Object Group
                        To Type: (Pos / Sca / Rot)
                        To Axis: (X / Y / Z)
                        */

                        if (modifier.commands.Count < 6)
                            modifier.commands.Add("0");

                        if (modifier.commands.Count < 7)
                            modifier.commands.Add("1");

                        if (modifier.commands.Count < 8)
                            modifier.commands.Add("0");

                        if (modifier.commands.Count < 9)
                            modifier.commands.Add("-99999");

                        if (modifier.commands.Count < 10)
                            modifier.commands.Add("99999");

                        if (modifier.commands.Count < 11)
                            modifier.commands.Add("9999");

                        if (int.TryParse(modifier.commands[1], out int fromType) && int.TryParse(modifier.commands[2], out int fromAxis)
                            && int.TryParse(modifier.commands[3], out int toType) && int.TryParse(modifier.commands[4], out int toAxis)
                            && float.TryParse(modifier.commands[5], out float delay) && float.TryParse(modifier.commands[6], out float multiply)
                            && float.TryParse(modifier.commands[7], out float offset) && float.TryParse(modifier.commands[8], out float min) && float.TryParse(modifier.commands[9], out float max)
                            && float.TryParse(modifier.commands[10], out float loop)
                            && DataManager.inst.gameData.beatmapObjects.TryFind(x => (x as BeatmapObject).tags.Contains(modifier.value), out DataManager.GameData.BeatmapObject beatmapObject)
                            && beatmapObject != null)
                        {
                            var time = AudioManager.inst.CurrentAudioSource.time;

                            var bm = beatmapObject as BeatmapObject;

                            fromType = Mathf.Clamp(fromType, 0, bm.events.Count);
                            fromAxis = Mathf.Clamp(fromAxis, 0, bm.events[fromType][0].eventValues.Length);

                            if (Updater.levelProcessor.converter.cachedSequences.ContainsKey(bm.id))
                            {
                                // To Type Position
                                // To Axis X
                                // From Type Position
                                if (toType == 0 && toAxis == 0 && fromType == 0)
                                {
                                    var sequence = Updater.levelProcessor.converter.cachedSequences[bm.id].Position3DSequence.Interpolate(time - bm.StartTime - delay);

                                    modifier.reference.positionOffset.x = Mathf.Clamp((fromAxis == 0 ? sequence.x % loop : fromAxis == 1 ? sequence.y % loop : sequence.z % loop) * multiply - offset, min, max);
                                }

                                // To Type Position
                                // To Axis Y
                                // From Type Position
                                if (toType == 0 && toAxis == 1 && fromType == 0)
                                {
                                    var sequence = Updater.levelProcessor.converter.cachedSequences[bm.id].Position3DSequence.Interpolate(time - bm.StartTime - delay);

                                    modifier.reference.positionOffset.y = Mathf.Clamp((fromAxis == 0 ? sequence.x % loop : fromAxis == 1 ? sequence.y % loop : sequence.z % loop) * multiply - offset, min, max);
                                }

                                // To Type Position
                                // To Axis Z
                                // From Type Position
                                if (toType == 0 && toAxis == 2 && fromType == 0)
                                {
                                    var sequence = Updater.levelProcessor.converter.cachedSequences[bm.id].Position3DSequence.Interpolate(time - bm.StartTime - delay);

                                    modifier.reference.positionOffset.z = Mathf.Clamp((fromAxis == 0 ? sequence.x % loop : fromAxis == 1 ? sequence.y % loop : sequence.z % loop) * multiply - offset, min, max);
                                }

                                // To Type Position
                                // To Axis X
                                // From Type Scale
                                if (toType == 0 && toAxis == 0 && fromType == 1)
                                {
                                    var sequence = Updater.levelProcessor.converter.cachedSequences[bm.id].ScaleSequence.Interpolate(time - bm.StartTime - delay);

                                    modifier.reference.positionOffset.x = Mathf.Clamp((fromAxis == 0 ? sequence.x % loop : sequence.y % loop) * multiply - offset, min, max);
                                }

                                // To Type Position
                                // To Axis Y
                                // From Type Scale
                                if (toType == 0 && toAxis == 1 && fromType == 1)
                                {
                                    var sequence = Updater.levelProcessor.converter.cachedSequences[bm.id].ScaleSequence.Interpolate(time - bm.StartTime - delay);

                                    modifier.reference.positionOffset.y = Mathf.Clamp((fromAxis == 0 ? sequence.x % loop : sequence.y % loop) * multiply - offset, min, max);
                                }

                                // To Type Position
                                // To Axis Z
                                // From Type Scale
                                if (toType == 0 && toAxis == 2 && fromType == 1)
                                {
                                    var sequence = Updater.levelProcessor.converter.cachedSequences[bm.id].ScaleSequence.Interpolate(time - bm.StartTime - delay);

                                    modifier.reference.positionOffset.z = Mathf.Clamp((fromAxis == 0 ? sequence.x % loop : sequence.y % loop) * multiply - offset, min, max);
                                }

                                // To Type Position
                                // To Axis X
                                // From Type Rotation
                                if (toType == 0 && toAxis == 0 && fromType == 2)
                                {
                                    var sequence = Updater.levelProcessor.converter.cachedSequences[bm.id].RotationSequence.Interpolate(time - bm.StartTime - delay) * multiply;

                                    modifier.reference.positionOffset.x = Mathf.Clamp((sequence % loop) - offset, min, max);
                                }

                                // To Type Position
                                // To Axis Y
                                // From Type Rotation
                                if (toType == 0 && toAxis == 1 && fromType == 2)
                                {
                                    var sequence = Updater.levelProcessor.converter.cachedSequences[bm.id].RotationSequence.Interpolate(time - bm.StartTime - delay) * multiply;

                                    modifier.reference.positionOffset.y = Mathf.Clamp((sequence % loop) - offset, min, max);
                                }

                                // To Type Position
                                // To Axis Z
                                // From Type Rotation
                                if (toType == 0 && toAxis == 2 && fromType == 2)
                                {
                                    var sequence = Updater.levelProcessor.converter.cachedSequences[bm.id].RotationSequence.Interpolate(time - bm.StartTime - delay) * multiply;

                                    modifier.reference.positionOffset.z = Mathf.Clamp((sequence % loop) - offset, min, max);
                                }

                                // To Type Scale
                                // To Axis X
                                // From Type Position
                                if (toType == 1 && toAxis == 0 && fromType == 0)
                                {
                                    var sequence = Updater.levelProcessor.converter.cachedSequences[bm.id].Position3DSequence.Interpolate(time - bm.StartTime - delay);

                                    modifier.reference.scaleOffset.x = Mathf.Clamp((fromAxis == 0 ? sequence.x % loop : fromAxis == 1 ? sequence.y % loop : sequence.z % loop) * multiply - offset, min, max);
                                }

                                // To Type Scale
                                // To Axis Y
                                // From Type Position
                                if (toType == 1 && toAxis == 1 && fromType == 0)
                                {
                                    var sequence = Updater.levelProcessor.converter.cachedSequences[bm.id].Position3DSequence.Interpolate(time - bm.StartTime - delay);

                                    modifier.reference.scaleOffset.y = Mathf.Clamp((fromAxis == 0 ? sequence.x % loop : fromAxis == 1 ? sequence.y % loop : sequence.z % loop) * multiply - offset, min, max);
                                }

                                // To Type Scale
                                // To Axis Z
                                // From Type Position
                                if (toType == 1 && toAxis == 2 && fromType == 0)
                                {
                                    var sequence = Updater.levelProcessor.converter.cachedSequences[bm.id].Position3DSequence.Interpolate(time - bm.StartTime - delay);

                                    modifier.reference.scaleOffset.z = Mathf.Clamp((fromAxis == 0 ? sequence.x % loop : fromAxis == 1 ? sequence.y % loop : sequence.z % loop) * multiply - offset, min, max);
                                }

                                // To Type Scale
                                // To Axis X
                                // From Type Scale
                                if (toType == 1 && toAxis == 0 && fromType == 1)
                                {
                                    var sequence = Updater.levelProcessor.converter.cachedSequences[bm.id].ScaleSequence.Interpolate(time - bm.StartTime - delay);

                                    modifier.reference.scaleOffset.x = Mathf.Clamp((fromAxis == 0 ? sequence.x % loop : sequence.y % loop) * multiply - offset, min, max);
                                }

                                // To Type Scale
                                // To Axis Y
                                // From Type Scale
                                if (toType == 1 && toAxis == 1 && fromType == 1)
                                {
                                    var sequence = Updater.levelProcessor.converter.cachedSequences[bm.id].ScaleSequence.Interpolate(time - bm.StartTime - delay);

                                    modifier.reference.scaleOffset.y = Mathf.Clamp((fromAxis == 0 ? sequence.x % loop : sequence.y % loop) * multiply - offset, min, max);
                                }

                                // To Type Scale
                                // To Axis Z
                                // From Type Scale
                                if (toType == 1 && toAxis == 2 && fromType == 1)
                                {
                                    var sequence = Updater.levelProcessor.converter.cachedSequences[bm.id].ScaleSequence.Interpolate(time - bm.StartTime - delay);

                                    modifier.reference.scaleOffset.z = Mathf.Clamp((fromAxis == 0 ? sequence.x % loop : sequence.y % loop) * multiply - offset, min, max);
                                }

                                // To Type Scale
                                // To Axis X
                                // From Type Rotation
                                if (toType == 1 && toAxis == 0 && fromType == 2)
                                {
                                    var sequence = Updater.levelProcessor.converter.cachedSequences[bm.id].RotationSequence.Interpolate(time - bm.StartTime - delay);

                                    modifier.reference.scaleOffset.x = Mathf.Clamp((sequence % loop) * multiply - offset, min, max);
                                }

                                // To Type Scale
                                // To Axis Y
                                // From Type Rotation
                                if (toType == 1 && toAxis == 1 && fromType == 2)
                                {
                                    var sequence = Updater.levelProcessor.converter.cachedSequences[bm.id].RotationSequence.Interpolate(time - bm.StartTime - delay);

                                    modifier.reference.scaleOffset.y = Mathf.Clamp((sequence % loop) * multiply - offset, min, max);
                                }

                                // To Type Scale
                                // To Axis Z
                                // From Type Rotation
                                if (toType == 1 && toAxis == 2 && fromType == 2)
                                {
                                    var sequence = Updater.levelProcessor.converter.cachedSequences[bm.id].RotationSequence.Interpolate(time - bm.StartTime - delay);

                                    modifier.reference.scaleOffset.z = Mathf.Clamp((sequence % loop) * multiply - offset, min, max);
                                }

                                // To Type Rotation
                                // To Axis X
                                // From Type Position
                                if (toType == 2 && toAxis == 0 && fromType == 0)
                                {
                                    var sequence = Updater.levelProcessor.converter.cachedSequences[bm.id].Position3DSequence.Interpolate(time - bm.StartTime - delay);

                                    modifier.reference.rotationOffset.x = Mathf.Clamp((fromAxis == 0 ? sequence.x % loop : fromAxis == 1 ? sequence.y % loop : sequence.z % loop) * multiply - offset, min, max);
                                }

                                // To Type Rotation
                                // To Axis Y
                                // From Type Position
                                if (toType == 2 && toAxis == 1 && fromType == 0)
                                {
                                    var sequence = Updater.levelProcessor.converter.cachedSequences[bm.id].Position3DSequence.Interpolate(time - bm.StartTime - delay);

                                    modifier.reference.rotationOffset.y = Mathf.Clamp((fromAxis == 0 ? sequence.x % loop : fromAxis == 1 ? sequence.y % loop : sequence.z % loop) * multiply - offset, min, max);
                                }

                                // To Type Rotation
                                // To Axis Z
                                // From Type Position
                                if (toType == 2 && toAxis == 2 && fromType == 0)
                                {
                                    var sequence = Updater.levelProcessor.converter.cachedSequences[bm.id].Position3DSequence.Interpolate(time - bm.StartTime - delay);

                                    modifier.reference.rotationOffset.z = Mathf.Clamp((fromAxis == 0 ? sequence.x % loop : fromAxis == 1 ? sequence.y % loop : sequence.z % loop) * multiply - offset, min, max);
                                }

                                // To Type Rotation
                                // To Axis X
                                // From Type Scale
                                if (toType == 2 && toAxis == 0 && fromType == 1)
                                {
                                    var sequence = Updater.levelProcessor.converter.cachedSequences[bm.id].ScaleSequence.Interpolate(time - bm.StartTime - delay);

                                    modifier.reference.rotationOffset.x = Mathf.Clamp((fromAxis == 0 ? sequence.x % loop : sequence.y % loop) * multiply - offset, min, max);
                                }

                                // To Type Rotation
                                // To Axis Y
                                // From Type Scale
                                if (toType == 2 && toAxis == 1 && fromType == 1)
                                {
                                    var sequence = Updater.levelProcessor.converter.cachedSequences[bm.id].ScaleSequence.Interpolate(time - bm.StartTime - delay);

                                    modifier.reference.rotationOffset.y = Mathf.Clamp((fromAxis == 0 ? sequence.x % loop : sequence.y % loop) * multiply - offset, min, max);
                                }

                                // To Type Rotation
                                // To Axis Z
                                // From Type Scale
                                if (toType == 2 && toAxis == 2 && fromType == 1)
                                {
                                    var sequence = Updater.levelProcessor.converter.cachedSequences[bm.id].ScaleSequence.Interpolate(time - bm.StartTime - delay);

                                    modifier.reference.rotationOffset.z = Mathf.Clamp((fromAxis == 0 ? sequence.x % loop : sequence.y % loop) * multiply - offset, min, max);
                                }

                                // To Type Rotation
                                // To Axis X
                                // From Type Rotation
                                if (toType == 2 && toAxis == 0 && fromType == 2)
                                {
                                    var sequence = Updater.levelProcessor.converter.cachedSequences[bm.id].RotationSequence.Interpolate(time - bm.StartTime - delay);

                                    modifier.reference.rotationOffset.x = Mathf.Clamp((sequence % loop) * multiply - offset, min, max);
                                }

                                // To Type Rotation
                                // To Axis Y
                                // From Type Rotation
                                if (toType == 2 && toAxis == 1 && fromType == 2)
                                {
                                    var sequence = Updater.levelProcessor.converter.cachedSequences[bm.id].RotationSequence.Interpolate(time - bm.StartTime - delay);

                                    modifier.reference.rotationOffset.y = Mathf.Clamp((sequence % loop) * multiply - offset, min, max);
                                }

                                // To Type Rotation
                                // To Axis Z
                                // From Type Rotation
                                if (toType == 2 && toAxis == 2 && fromType == 2)
                                {
                                    var sequence = Updater.levelProcessor.converter.cachedSequences[bm.id].RotationSequence.Interpolate(time - bm.StartTime - delay);

                                    modifier.reference.rotationOffset.z = Mathf.Clamp((sequence % loop) * multiply - offset, min, max);
                                }
                            }
                        }

                        break;
                    }
            }
        }

        public static void BGInactive(Modifier<BackgroundObject> modifier)
        {
            if (!modifier.verified)
            {
                modifier.verified = true;
                modifier.VerifyModifier(ModifiersManager.bgModifierTypes);
            }

            if (!modifier.IsValid(ModifiersManager.bgModifierTypes))
                return;
        }
    }
}
