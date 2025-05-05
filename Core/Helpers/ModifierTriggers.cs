using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using UnityEngine;

using LSFunctions;

using SimpleJSON;

using BetterLegacy.Arcade.Managers;
using BetterLegacy.Configs;
using BetterLegacy.Core;
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
            return GameManager.inst.deaths.Count == modifier.GetInt(0, 0, variables);
        }
        
        public static bool playerDeathsLesserEquals<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            return GameManager.inst.deaths.Count <= modifier.GetInt(0, 0, variables);
        }
        
        public static bool playerDeathsGreaterEquals<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            return GameManager.inst.deaths.Count >= modifier.GetInt(0, 0, variables);
        }
        
        public static bool playerDeathsLesser<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            return GameManager.inst.deaths.Count < modifier.GetInt(0, 0, variables);
        }
        
        public static bool playerDeathsGreater<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            return GameManager.inst.deaths.Count > modifier.GetInt(0, 0, variables);
        }
        
        public static bool playerDistanceGreater(Modifier<BeatmapObject> modifier, Dictionary<string, string> variables)
        {
            float num = modifier.GetFloat(0, 0f, variables);
            var levelObject = modifier.reference.runtimeObject;
            for (int i = 0; i < GameManager.inst.players.transform.childCount; i++)
            {
                if (GameManager.inst.players.transform.Find(string.Format("Player {0}", i + 1)) && levelObject && levelObject.visualObject && levelObject.visualObject.gameObject)
                {
                    var player = GameManager.inst.players.transform.Find(string.Format("Player {0}/Player", i + 1));
                    if (Vector2.Distance(player.transform.position, levelObject.visualObject.gameObject.transform.position) > num)
                        return true;
                }
            }

            return false;
        }
        
        public static bool playerDistanceLesser(Modifier<BeatmapObject> modifier, Dictionary<string, string> variables)
        {
            float num = modifier.GetFloat(0, 0f, variables);
            var levelObject = modifier.reference.runtimeObject;
            for (int i = 0; i < GameManager.inst.players.transform.childCount; i++)
            {
                if (GameManager.inst.players.transform.Find(string.Format("Player {0}", i + 1)) && levelObject && levelObject.visualObject && levelObject.visualObject.gameObject)
                {
                    var player = GameManager.inst.players.transform.Find(string.Format("Player {0}/Player", i + 1));
                    if (Vector2.Distance(player.transform.position, levelObject.visualObject.gameObject.transform.position) < num)
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
            var hasResult = modifier.HasResult();

            if (!hasResult || modifier.Result is int count && count != GameManager.inst.hits.Count)
            {
                modifier.Result = GameManager.inst.hits.Count;
                if (hasResult)
                    return true;
            }

            return false;
        }
        
        public static bool onPlayerDeath<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            var hasResult = modifier.HasResult();

            if (!hasResult || modifier.Result is int count && count != GameManager.inst.deaths.Count)
            {
                modifier.Result = GameManager.inst.deaths.Count;
                if (hasResult)
                    return true;
            }

            return false;
        }
        
        public static bool playerBoostEquals<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            return LevelManager.BoostCount == modifier.GetInt(0, 0, variables);
        }
        
        public static bool playerBoostLesserEquals<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            return LevelManager.BoostCount <= modifier.GetInt(0, 0, variables);
        }
        
        public static bool playerBoostGreaterEquals<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            return LevelManager.BoostCount >= modifier.GetInt(0, 0, variables);
        }
        
        public static bool playerBoostLesser<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            return LevelManager.BoostCount < modifier.GetInt(0, 0, variables);
        }
        
        public static bool playerBoostGreater<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            return LevelManager.BoostCount > modifier.GetInt(0, 0, variables);
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

        public static bool controlPressDown(Modifier<BeatmapObject> modifier, Dictionary<string, string> variables)
        {
            var type = modifier.GetInt(0, 0, variables);

            var levelObject = modifier.reference.runtimeObject;
            if (!levelObject || !levelObject.visualObject || !levelObject.visualObject.gameObject)
                return false;

            var player = PlayerManager.GetClosestPlayer(levelObject.visualObject.gameObject.transform.position);

            if (!player || player.device == null && !CoreHelper.InEditor || InControl.InputManager.ActiveDevice == null)
                return false;

            var device = player.device ?? InControl.InputManager.ActiveDevice;

            return Enum.TryParse(((PlayerInputControlType)type).ToString(), out InControl.InputControlType inputControlType) && device.GetControl(inputControlType).WasPressed;
        }

        public static bool controlPress(Modifier<BeatmapObject> modifier, Dictionary<string, string> variables)
        {
            var type = modifier.GetInt(0, 0, variables);

            var levelObject = modifier.reference.runtimeObject;
            if (!levelObject || !levelObject.visualObject || !levelObject.visualObject.gameObject)
                return false;

            var player = PlayerManager.GetClosestPlayer(levelObject.visualObject.gameObject.transform.position);

            if (!player || player.device == null && !CoreHelper.InEditor || InControl.InputManager.ActiveDevice == null)
                return false;

            var device = player.device ?? InControl.InputManager.ActiveDevice;

            return Enum.TryParse(((PlayerInputControlType)type).ToString(), out InControl.InputControlType inputControlType) && device.GetControl(inputControlType).IsPressed;
        }

        public static bool controlPressUp(Modifier<BeatmapObject> modifier, Dictionary<string, string> variables)
        {
            var type = modifier.GetInt(0, 0, variables);

            var levelObject = modifier.reference.runtimeObject;
            if (!levelObject || !levelObject.visualObject || !levelObject.visualObject.gameObject)
                return false;

            var player = PlayerManager.GetClosestPlayer(levelObject.visualObject.gameObject.transform.position);

            if (!player || player.device == null && !CoreHelper.InEditor || InControl.InputManager.ActiveDevice == null)
                return false;

            var device = player.device ?? InControl.InputManager.ActiveDevice;

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

        public static bool dummy<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            return false;
        }

        #endregion

        #region Audio

        #endregion

        #region Challenge Mode

        #endregion

        #region Random

        #endregion

        #region Math

        #endregion

        #region Axis

        #endregion

        #region Level Rank

        #endregion

        #region Level

        #endregion

        #region Real Time

        #endregion

        #region Config

        #endregion

        #region Misc

        #endregion

        #region Dev Only

        #endregion
    }
}

#pragma warning restore IDE1006 // Naming Styles