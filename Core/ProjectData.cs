using BetterLegacy.Core.Data;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using LSFunctions;
using SimpleJSON;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using BaseBeatmapObject = DataManager.GameData.BeatmapObject;
using BaseBeatmapTheme = DataManager.BeatmapTheme;
using BaseCheckpoint = DataManager.GameData.BeatmapData.Checkpoint;
using BaseEventKeyframe = DataManager.GameData.EventKeyframe;
using BaseMarker = DataManager.GameData.BeatmapData.Marker;
using BasePrefab = DataManager.GameData.Prefab;

namespace BetterLegacy.Core
{
    public class ProjectData
    {
        public static class Combiner
        {
            #region Settings

            public static bool prioritizeFirstEvents = true;
            public static bool prioritizeFirstThemes = true;

            public static bool addFirstMarkers = true;
            public static bool addSecondMarkers = false;

            public static bool addFirstCheckpoints = true;
            public static bool addSecondCheckpoints = false;

            public static bool objectsWithMatchingIDAddKeyframes = false;

            #endregion

            /// <summary>
            /// Combines multiple GameDatas together.
            /// </summary>
            /// <param name="gameDatas">Array of GameData to combine together.</param>
            /// <returns>Combined GameData.</returns>
            public static GameData Combine(params GameData[] gameDatas)
            {
                var baseData = new GameData();
                baseData.beatmapData = new LevelBeatmapData();
                baseData.beatmapData.editorData = new LevelEditorData();
                baseData.beatmapData.levelData = new LevelData();

                if (gameDatas != null && gameDatas.Length > 0)
                    for (int i = 0; i < gameDatas.Length; i++)
                    {
                        if (gameDatas[i].beatmapData != null && baseData.beatmapData != null)
                        {
                            if (baseData.beatmapData.checkpoints == null)
                                baseData.beatmapData.checkpoints = new List<BaseCheckpoint>();
                            if (baseData.beatmapData.markers == null)
                                baseData.beatmapData.markers = new List<BaseMarker>();

                            baseData.beatmapData.checkpoints.AddRange(gameDatas[i].beatmapData.checkpoints.Where(x => !baseData.beatmapData.checkpoints.Has(y => y.time == x.time)));
                            baseData.beatmapData.markers.AddRange(gameDatas[i].beatmapData.markers.Where(x => !baseData.beatmapData.markers.Has(y => y.time == x.time)));
                        }

                        if (baseData.beatmapObjects == null)
                            baseData.beatmapObjects = new List<BaseBeatmapObject>();

                        baseData.beatmapObjects.AddRange(gameDatas[i].BeatmapObjects.Where(x => !baseData.BeatmapObjects.Has(y => y.id == x.id)));

                        if (baseData.prefabObjects == null)
                            baseData.prefabObjects = new List<DataManager.GameData.PrefabObject>();

                        baseData.prefabObjects.AddRange(gameDatas[i].prefabObjects.Where(x => !baseData.prefabObjects.Has(y => y.ID == x.ID)));

                        if (baseData.prefabs == null)
                            baseData.prefabs = new List<BasePrefab>();

                        baseData.prefabs.AddRange(gameDatas[i].prefabs.Where(x => !baseData.prefabs.Has(y => y.ID == x.ID)));

                        baseData.backgroundObjects.AddRange(gameDatas[i].BackgroundObjects.Where(x => !baseData.BackgroundObjects.Has(y =>
                        {
                            return y.active == x.active &&
                                    y.color == x.color &&
                                    y.depth == x.depth &&
                                    y.drawFade == x.drawFade &&
                                    y.FadeColor == x.FadeColor &&
                                    y.layer == x.layer &&
                                    y.name == x.name &&
                                    y.pos == x.pos &&
                                    y.reactive == x.reactive &&
                                    y.reactiveCol == x.reactiveCol &&
                                    y.reactiveColIntensity == x.reactiveColIntensity &&
                                    y.reactiveColSample == x.reactiveColSample &&
                                    y.reactiveIncludesZ == x.reactiveIncludesZ &&
                                    y.reactivePosIntensity == x.reactivePosIntensity &&
                                    y.reactivePosSamples == x.reactivePosSamples &&
                                    y.reactiveRotIntensity == x.reactiveRotIntensity &&
                                    y.reactiveRotSample == x.reactiveRotSample &&
                                    y.reactiveScaIntensity == x.reactiveScaIntensity &&
                                    y.reactiveScale == x.reactiveScale &&
                                    y.reactiveScaSamples == x.reactiveScaSamples &&
                                    y.reactiveSize == x.reactiveSize &&
                                    y.reactiveType == x.reactiveType &&
                                    y.reactiveZIntensity == x.reactiveZIntensity &&
                                    y.reactiveZSample == x.reactiveZSample &&
                                    y.rot == x.rot &&
                                    y.rotation == x.rotation &&
                                    y.scale == x.scale &&
                                    y.text == x.text &&
                                    y.zscale == x.zscale;
                        })));

                        if (baseData.eventObjects == null)
                            baseData.eventObjects = new DataManager.GameData.EventObjects();

                        for (int j = 0; j < gameDatas[i].eventObjects.allEvents.Count; j++)
                        {
                            if (baseData.eventObjects.allEvents.Count <= j)
                                baseData.eventObjects.allEvents.Add(new List<BaseEventKeyframe>());

                            baseData.eventObjects.allEvents[j].AddRange(gameDatas[i].eventObjects.allEvents[j].Where(x => !baseData.eventObjects.allEvents[j].Has(y => y.eventTime == x.eventTime)));
                        }

                        foreach (var beatmapTheme in gameDatas[i].beatmapThemes)
                        {
                            if (!baseData.beatmapThemes.ContainsKey(beatmapTheme.Key))
                                baseData.beatmapThemes.Add(beatmapTheme.Key, beatmapTheme.Value);
                        }

                        // Clearing
                        {
                            for (int j = 0; j < gameDatas[i].beatmapData.checkpoints.Count; j++)
                                gameDatas[i].beatmapData.checkpoints[j] = null;
                            gameDatas[i].beatmapData.checkpoints.Clear();

                            for (int j = 0; j < gameDatas[i].beatmapData.markers.Count; j++)
                                gameDatas[i].beatmapData.markers[j] = null;
                            gameDatas[i].beatmapData.markers.Clear();

                            for (int j = 0; j < gameDatas[i].beatmapObjects.Count; j++)
                                gameDatas[i].beatmapObjects[j] = null;
                            gameDatas[i].beatmapObjects.Clear();

                            for (int j = 0; j < gameDatas[i].backgroundObjects.Count; j++)
                                gameDatas[i].backgroundObjects[j] = null;
                            gameDatas[i].backgroundObjects.Clear();

                            for (int j = 0; j < gameDatas[i].prefabObjects.Count; j++)
                                gameDatas[i].prefabObjects[j] = null;
                            gameDatas[i].prefabObjects.Clear();

                            for (int j = 0; j < gameDatas[i].prefabs.Count; j++)
                                gameDatas[i].prefabs[j] = null;
                            gameDatas[i].prefabs.Clear();

                            gameDatas[i].beatmapThemes.Clear();

                            for (int j = 0; j < gameDatas[i].eventObjects.allEvents.Count; j++)
                                gameDatas[i].eventObjects.allEvents[j] = null;
                            gameDatas[i].eventObjects.allEvents.Clear();

                            gameDatas[i] = null;
                        }
                    }

                gameDatas = null;

                return baseData;
            }
        }

        public static class Reader
        {
            public static List<List<BaseEventKeyframe>> ParseEventkeyframes(JSONNode jn, bool clamp = true)
            {
                var allEvents = new List<List<BaseEventKeyframe>>();

                for (int i = 0; i < GameData.EventCount; i++)
                {
                    allEvents.Add(new List<BaseEventKeyframe>());
                    if (jn[GameData.EventTypes[i]] != null)
                        for (int j = 0; j < jn[GameData.EventTypes[i]].Count; j++)
                            allEvents[i].Add(EventKeyframe.Parse(jn[GameData.EventTypes[i]][j], i, GameData.DefaultKeyframes[i].eventValues.Length));
                }

                if (clamp)
                    ClampEventListValues(allEvents, GameData.EventCount);

                allEvents.ForEach(x => x = x.OrderBy(x => x.eventTime).ToList());

                return allEvents;
            }

            public static void ClampEventListValues(List<List<BaseEventKeyframe>> eventKeyframes, int totalTypes)
            {
                while (eventKeyframes.Count > totalTypes)
                    eventKeyframes.RemoveAt(eventKeyframes.Count - 1);

                for (int type = 0; type < totalTypes; type++)
                {
                    if (eventKeyframes.Count < type + 1)
                        eventKeyframes.Add(new List<BaseEventKeyframe>());

                    if (eventKeyframes[type].Count < 1)
                        eventKeyframes[type].Add(EventKeyframe.DeepCopy((EventKeyframe)GameData.DefaultKeyframes[type]));

                    for (int index = 0; index < eventKeyframes[type].Count; index++)
                    {
                        var array = eventKeyframes[type][index].eventValues;
                        if (array.Length != GameData.DefaultKeyframes[type].eventValues.Length)
                        {
                            array = new float[GameData.DefaultKeyframes[type].eventValues.Length];
                            for (int i = 0; i < GameData.DefaultKeyframes[type].eventValues.Length; i++)
                                array[i] = i < eventKeyframes[type][index].eventValues.Length ? eventKeyframes[type][index].eventValues[i] : GameData.DefaultKeyframes[type].eventValues[i];
                        }
                        eventKeyframes[type][index].eventValues = array;
                    }
                }
            }

            public static BaseCheckpoint ParseCheckpoint(JSONNode jn)
                => new BaseCheckpoint(jn["active"].AsBool, jn["name"], jn["t"].AsFloat, new Vector2(jn["pos"]["x"].AsFloat, jn["pos"]["y"].AsFloat));
        }

        public static class Writer
        {
            public static IEnumerator SaveData(string _path, GameData _data, Action onSave = null, bool saveGameDataThemes = false)
            {
                CoreHelper.Log("Saving Beatmap");
                var jn = JSON.Parse("{}");

                CoreHelper.Log("Saving Editor Data");
                jn["ed"]["timeline_pos"] = "0";

                CoreHelper.Log("Saving Markers");
                for (int i = 0; i < _data.beatmapData.markers.Count; i++)
                    jn["ed"]["markers"][i] = ((Marker)_data.beatmapData.markers[i]).ToJSON();

                for (int i = 0; i < _data.levelModifiers.Count; i++)
                {
                    var levelModifier = _data.levelModifiers[i];

                    jn["modifiers"][i]["action"] = levelModifier.ActionModifier.ToJSON();
                    jn["modifiers"][i]["trigger"] = levelModifier.TriggerModifier.ToJSON();
                    jn["modifiers"][i]["retrigger"] = levelModifier.retriggerAmount.ToString();
                }

                for (int i = 0; i < AssetManager.SpriteAssets.Count; i++)
                {
                    jn["assets"]["spr"][i]["n"] = AssetManager.SpriteAssets.ElementAt(i).Key;
                    jn["assets"]["spr"][i]["i"] = SpriteHelper.SpriteToString(AssetManager.SpriteAssets.ElementAt(i).Value);
                }

                CoreHelper.Log("Saving Object Prefabs");
                var prefabObjects = _data.prefabObjects.Where(x => x is PrefabObject prefabObject && !prefabObject.fromModifier).Select(x => x as PrefabObject).ToList();
                for (int i = 0; i < prefabObjects.Count; i++)
                    jn["prefab_objects"][i] = prefabObjects[i].ToJSON();

                CoreHelper.Log("Saving Level Data");
                {
                    jn["level_data"] = _data.LevelBeatmapData.ModLevelData.ToJSON();
                }

                CoreHelper.Log("Saving prefabs");
                if (_data.prefabs != null)
                {
                    for (int i = 0; i < _data.prefabs.Count; i++)
                    {
                        jn["prefabs"][i] = ((Prefab)_data.prefabs[i]).ToJSON();
                    }
                }
                CoreHelper.Log($"Saving themes");
                var levelThemes = saveGameDataThemes ? _data.beatmapThemes.Where(x => Parser.TryParse(x.Value.id, 0) != 0 && _data.eventObjects.allEvents[4].Has(y => y.eventValues[0] == Parser.TryParse(x.Value.id, 0))).Select(x => x.Value).ToList() : DataManager.inst.CustomBeatmapThemes.Where(x => Parser.TryParse(x.id, 0) != 0 && _data.eventObjects.allEvents[4].Has(y => y.eventValues[0] == Parser.TryParse(x.id, 0))).ToList();

                for (int i = 0; i < levelThemes.Count; i++)
                {
                    CoreHelper.Log($"Saving {levelThemes[i].id} - {levelThemes[i].name} to level!");
                    jn["themes"][i] = ((BeatmapTheme)levelThemes[i]).ToJSON();
                }

                CoreHelper.Log("Saving Checkpoints");
                for (int i = 0; i < _data.beatmapData.checkpoints.Count; i++)
                {
                    jn["checkpoints"][i]["active"] = "False";
                    jn["checkpoints"][i]["name"] = _data.beatmapData.checkpoints[i].name;
                    jn["checkpoints"][i]["t"] = _data.beatmapData.checkpoints[i].time.ToString();
                    jn["checkpoints"][i]["pos"]["x"] = _data.beatmapData.checkpoints[i].pos.x.ToString();
                    jn["checkpoints"][i]["pos"]["y"] = _data.beatmapData.checkpoints[i].pos.y.ToString();
                }

                CoreHelper.Log("Saving Beatmap Objects");
                if (_data.beatmapObjects != null)
                {
                    var list = _data.beatmapObjects.FindAll(x => !x.fromPrefab);
                    jn["beatmap_objects"] = new JSONArray();
                    for (int i = 0; i < list.Count; i++)
                    {
                        jn["beatmap_objects"][i] = ((BeatmapObject)list[i]).ToJSON();
                    }
                }
                else
                {
                    CoreHelper.Log("skipping objects");
                    jn["beatmap_objects"] = new JSONArray();
                }

                CoreHelper.Log("Saving Background Objects");
                for (int i = 0; i < _data.backgroundObjects.Count; i++)
                {
                    jn["bg_objects"][i] = ((BackgroundObject)_data.backgroundObjects[i]).ToJSON();
                }

                CoreHelper.Log("Saving Event Objects");
                {
                    for (int i = 0; i < _data.eventObjects.allEvents.Count; i++)
                    {
                        for (int j = 0; j < _data.eventObjects.allEvents[i].Count; j++)
                        {
                            if (GameData.EventTypes.Length > i)
                            {
                                jn["events"][GameData.EventTypes[i]][j] = ((EventKeyframe)_data.eventObjects.allEvents[i][j]).ToJSON();
                            }
                        }
                    }
                }

                CoreHelper.Log($"Saving Entire Beatmap to {_path}");
                RTFile.WriteToFile(_path, jn.ToString());

                onSave?.Invoke();

                yield break;
            }

            public static IEnumerator SaveDataVG(string _path, GameData _data, Action onSave = null)
            {
                var jn = JSON.Parse("{}");

                jn["editor"]["bpm"]["snap"]["objects"] = true;
                jn["editor"]["bpm"]["bpm_value"] = 140f;
                jn["editor"]["bpm"]["bpm_offset"] = 0f;
                jn["editor"]["bpm"]["BPMValue"] = 140f;
                jn["editor"]["grid"]["scale"]["x"] = 2f;
                jn["editor"]["grid"]["scale"]["y"] = 2f;
                jn["editor"]["general"]["complexity"] = 0;
                jn["editor"]["general"]["theme"] = 0;
                jn["editor"]["general"]["test_mode"] = 0;
                jn["editor"]["preview"]["cam_zoom_offset"] = 0f;
                jn["editor"]["preview"]["cam_zoom_offset_color"] = 0;

                for (int i = 0; i < 6; i++)
                    jn["editor_prefab_spawn"][i] = new JSONObject();

                for (int i = 1; i < 6; i++)
                {
                    jn["parallax_settings"]["l"][i - 1]["d"] = 100 * i;
                    jn["parallax_settings"]["l"][i - 1]["c"] = 1 * i;
                }

                for (int i = 0; i < _data.levelModifiers.Count; i++)
                {
                    var levelModifier = _data.levelModifiers[i];

                    var triggerIndex = GameData.LevelModifier.DefaultTriggers.ToList().FindIndex(x => x.commands[0] == levelModifier.TriggerModifier.commands[0]);
                    var actionIndex = GameData.LevelModifier.DefaultActions.ToList().FindIndex(x => x.commands[0] == levelModifier.ActionModifier.commands[0]);

                    if (triggerIndex < 0 || actionIndex < 0)
                        continue;

                    jn["triggers"][i]["event_trigger"] = triggerIndex;
                    jn["triggers"][i]["event_trigger_time"]["x"] = Parser.TryParse(levelModifier.TriggerModifier.commands[1], 0f);
                    jn["triggers"][i]["event_trigger_time"]["y"] = Parser.TryParse(levelModifier.TriggerModifier.commands[2], 0f);
                    jn["triggers"][i]["event_retrigger"] = levelModifier.retriggerAmount;

                    jn["triggers"][i]["event_type"] = actionIndex;

                    for (int j = 1; j < levelModifier.ActionModifier.commands.Count; j++)
                        jn["triggers"][i]["event_data"][j - 1] = levelModifier.ActionModifier.commands[j];
                }

                for (int i = 0; i < _data.beatmapData.checkpoints.Count; i++)
                {
                    var checkpoint = _data.beatmapData.checkpoints[i];
                    jn["checkpoints"][i]["n"] = checkpoint.name;
                    jn["checkpoints"][i]["t"] = checkpoint.time;
                    jn["checkpoints"][i]["p"]["X"] = checkpoint.pos.x;
                    jn["checkpoints"][i]["p"]["y"] = checkpoint.pos.y;
                }

                for (int i = 0; i < _data.beatmapObjects.Count; i++)
                {
                    jn["objects"][i] = ((Data.BeatmapObject)_data.beatmapObjects[i]).ToJSONVG();
                }

                if (_data.prefabObjects.Count > 0)
                    for (int i = 0; i < _data.prefabObjects.Count; i++)
                    {
                        jn["prefab_objects"][i] = ((Data.PrefabObject)_data.prefabObjects[i]).ToJSONVG();
                    }
                else
                    jn["prefab_objects"] = new JSONArray();

                if (_data.prefabs.Count > 0)
                    for (int i = 0; i < _data.prefabs.Count; i++)
                    {
                        jn["prefabs"][i] = ((Data.Prefab)_data.prefabs[i]).ToJSONVG();
                    }
                else
                    jn["prefabs"] = new JSONArray();

                Dictionary<string, string> idsConverter = new Dictionary<string, string>();

                int themeIndex = 0;
                var themes = DataManager.inst.CustomBeatmapThemes.Select(x => x as BeatmapTheme).Where(x => _data.eventObjects.allEvents[4].Has(y => int.TryParse(x.id, out int id) && id == y.eventValues[0]));
                if (themes.Count() > 0)
                    foreach (var beatmapTheme in themes)
                    {
                        beatmapTheme.VGID = LSText.randomString(16);

                        if (!idsConverter.ContainsKey(Parser.TryParse(beatmapTheme.id, 0f).ToString()))
                            idsConverter.Add(Parser.TryParse(beatmapTheme.id, 0f).ToString(), beatmapTheme.VGID);

                        jn["themes"][themeIndex] = beatmapTheme.ToJSONVG();
                        themeIndex++;
                    }
                else
                    jn["themes"] = new JSONArray();

                if (_data.beatmapData.markers.Count > 0)
                    for (int i = 0; i < _data.beatmapData.markers.Count; i++)
                    {
                        jn["markers"][i] = ((Marker)_data.beatmapData.markers[i]).ToJSONVG();
                    }
                else
                    jn["markers"] = new JSONArray();

                // Event Handlers
                {
                    // Move
                    for (int i = 0; i < _data.eventObjects.allEvents[0].Count; i++)
                    {
                        var eventKeyframe = _data.eventObjects.allEvents[0][i];
                        jn["events"][0][i]["ct"] = eventKeyframe.curveType.Name;
                        jn["events"][0][i]["t"] = eventKeyframe.eventTime;
                        jn["events"][0][i]["ev"][0] = eventKeyframe.eventValues[0];
                        jn["events"][0][i]["ev"][1] = eventKeyframe.eventValues[1];
                    }

                    // Zoom
                    for (int i = 0; i < _data.eventObjects.allEvents[1].Count; i++)
                    {
                        var eventKeyframe = _data.eventObjects.allEvents[1][i];
                        jn["events"][1][i]["ct"] = eventKeyframe.curveType.Name;
                        jn["events"][1][i]["t"] = eventKeyframe.eventTime;
                        jn["events"][1][i]["ev"][0] = eventKeyframe.eventValues[0];
                    }

                    // Rotate
                    for (int i = 0; i < _data.eventObjects.allEvents[2].Count; i++)
                    {
                        var eventKeyframe = _data.eventObjects.allEvents[2][i];
                        jn["events"][2][i]["ct"] = eventKeyframe.curveType.Name;
                        jn["events"][2][i]["t"] = eventKeyframe.eventTime;
                        jn["events"][2][i]["ev"][0] = eventKeyframe.eventValues[0];
                    }

                    // Shake
                    for (int i = 0; i < _data.eventObjects.allEvents[3].Count; i++)
                    {
                        var eventKeyframe = _data.eventObjects.allEvents[3][i];
                        jn["events"][3][i]["ct"] = eventKeyframe.curveType.Name;
                        jn["events"][3][i]["t"] = eventKeyframe.eventTime;
                        jn["events"][3][i]["ev"][0] = eventKeyframe.eventValues[0];
                    }

                    // Themes
                    for (int i = 0; i < _data.eventObjects.allEvents[4].Count; i++)
                    {
                        var eventKeyframe = _data.eventObjects.allEvents[4][i];
                        jn["events"][4][i]["ct"] = eventKeyframe.curveType.Name;
                        jn["events"][4][i]["t"] = eventKeyframe.eventTime;
                        jn["events"][4][i]["evs"][0] = idsConverter.TryGetValue(eventKeyframe.eventValues[0].ToString(), out string themeID) ? themeID : eventKeyframe.eventValues[0].ToString();
                    }

                    // Chroma
                    for (int i = 0; i < _data.eventObjects.allEvents[5].Count; i++)
                    {
                        var eventKeyframe = _data.eventObjects.allEvents[5][i];
                        jn["events"][5][i]["ct"] = eventKeyframe.curveType.Name;
                        jn["events"][5][i]["t"] = eventKeyframe.eventTime;
                        jn["events"][5][i]["ev"][0] = eventKeyframe.eventValues[0];
                    }

                    // Bloom
                    for (int i = 0; i < _data.eventObjects.allEvents[6].Count; i++)
                    {
                        var eventKeyframe = _data.eventObjects.allEvents[6][i];
                        jn["events"][6][i]["ct"] = eventKeyframe.curveType.Name;
                        jn["events"][6][i]["t"] = eventKeyframe.eventTime;
                        jn["events"][6][i]["ev"][0] = eventKeyframe.eventValues[0];
                        jn["events"][6][i]["ev"][1] = eventKeyframe.eventValues[1];
                        jn["events"][6][i]["ev"][2] = Mathf.Clamp(eventKeyframe.eventValues[4], 0f, 9f);
                    }

                    // Vignette
                    for (int i = 0; i < _data.eventObjects.allEvents[7].Count; i++)
                    {
                        var eventKeyframe = _data.eventObjects.allEvents[7][i];
                        jn["events"][7][i]["ct"] = eventKeyframe.curveType.Name;
                        jn["events"][7][i]["t"] = eventKeyframe.eventTime;
                        jn["events"][7][i]["ev"][0] = eventKeyframe.eventValues[0];
                        jn["events"][7][i]["ev"][1] = eventKeyframe.eventValues[1];
                        jn["events"][7][i]["ev"][2] = eventKeyframe.eventValues[2];
                        jn["events"][7][i]["ev"][3] = eventKeyframe.eventValues[3];
                        jn["events"][7][i]["ev"][4] = eventKeyframe.eventValues[4];
                        jn["events"][7][i]["ev"][5] = eventKeyframe.eventValues[5];
                        jn["events"][7][i]["ev"][6] = Mathf.Clamp(eventKeyframe.eventValues[6], 0f, 9f);
                    }

                    // Lens
                    for (int i = 0; i < _data.eventObjects.allEvents[8].Count; i++)
                    {
                        var eventKeyframe = _data.eventObjects.allEvents[8][i];
                        jn["events"][8][i]["ct"] = eventKeyframe.curveType.Name;
                        jn["events"][8][i]["t"] = eventKeyframe.eventTime;
                        jn["events"][8][i]["ev"][0] = eventKeyframe.eventValues[0];
                        jn["events"][8][i]["ev"][1] = eventKeyframe.eventValues[1];
                        jn["events"][8][i]["ev"][2] = eventKeyframe.eventValues[2];
                    }

                    // Grain
                    for (int i = 0; i < _data.eventObjects.allEvents[9].Count; i++)
                    {
                        var eventKeyframe = _data.eventObjects.allEvents[9][i];
                        jn["events"][9][i]["ct"] = eventKeyframe.curveType.Name;
                        jn["events"][9][i]["t"] = eventKeyframe.eventTime;
                        jn["events"][9][i]["ev"][0] = eventKeyframe.eventValues[0];
                        jn["events"][9][i]["ev"][1] = eventKeyframe.eventValues[1];
                        jn["events"][9][i]["ev"][2] = eventKeyframe.eventValues[2];
                        jn["events"][9][i]["ev"][3] = 1f;
                    }

                    // Gradient
                    for (int i = 0; i < _data.eventObjects.allEvents[15].Count; i++)
                    {
                        var eventKeyframe = _data.eventObjects.allEvents[15][i];
                        jn["events"][10][i]["ct"] = eventKeyframe.curveType.Name;
                        jn["events"][10][i]["t"] = eventKeyframe.eventTime;
                        jn["events"][10][i]["ev"][0] = eventKeyframe.eventValues[0];
                        jn["events"][10][i]["ev"][1] = eventKeyframe.eventValues[1];
                        jn["events"][10][i]["ev"][2] = Mathf.Clamp(eventKeyframe.eventValues[2], 0f, 9f);
                        jn["events"][10][i]["ev"][3] = Mathf.Clamp(eventKeyframe.eventValues[3], 0f, 9f);
                        jn["events"][10][i]["ev"][4] = eventKeyframe.eventValues[4];
                    }

                    jn["events"][11][0]["ct"] = "Linear";
                    jn["events"][11][0]["t"] = 0f;
                    jn["events"][11][0]["ev"][0] = 0f;
                    jn["events"][11][0]["ev"][1] = 0f;
                    jn["events"][11][0]["ev"][2] = 0f;

                    // Hueshift
                    for (int i = 0; i < _data.eventObjects.allEvents[10].Count; i++)
                    {
                        var eventKeyframe = _data.eventObjects.allEvents[10][i];
                        jn["events"][12][i]["ct"] = eventKeyframe.curveType.Name;
                        jn["events"][12][i]["t"] = eventKeyframe.eventTime;
                        jn["events"][12][i]["ev"][0] = eventKeyframe.eventValues[0];
                    }

                    // Player
                    for (int i = 0; i < _data.eventObjects.allEvents[36].Count; i++)
                    {
                        var eventKeyframe = _data.eventObjects.allEvents[36][i];
                        jn["events"][13][i]["ct"] = eventKeyframe.curveType.Name;
                        jn["events"][13][i]["t"] = eventKeyframe.eventTime;
                        jn["events"][13][i]["ev"][0] = eventKeyframe.eventValues[0];
                        jn["events"][13][i]["ev"][1] = eventKeyframe.eventValues[1];
                        jn["events"][13][i]["ev"][2] = 0f;
                    }
                }

                CoreHelper.Log($"Saving Entire Beatmap to {_path}");
                RTFile.WriteToFile(_path, jn.ToString());

                onSave?.Invoke();

                yield break;
            }
        }
    }
}
