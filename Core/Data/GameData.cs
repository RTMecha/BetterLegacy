﻿using BetterLegacy.Components.Player;
using BetterLegacy.Configs;
using BetterLegacy.Core.Animation;
using BetterLegacy.Core.Animation.Keyframe;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using BetterLegacy.Core.Optimization;
using LSFunctions;
using SimpleJSON;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using BaseBackgroundObject = DataManager.GameData.BackgroundObject;
using BaseBeatmapObject = DataManager.GameData.BeatmapObject;
using BaseBeatmapTheme = DataManager.BeatmapTheme;
using BaseEventKeyframe = DataManager.GameData.EventKeyframe;
using BaseGameData = DataManager.GameData;

namespace BetterLegacy.Core.Data
{
    public class GameData : BaseGameData
    {
        public GameData()
        {

        }

        public static bool IsValid => DataManager.inst.gameData is GameData;
        public static GameData Current { get => (GameData)DataManager.inst.gameData; set => DataManager.inst.gameData = value; }

        public Dictionary<string, BaseBeatmapTheme> beatmapThemes = new Dictionary<string, BaseBeatmapTheme>();

        public List<LevelModifier> levelModifiers = new List<LevelModifier>();

        /// <summary>
        /// Class for alpha EventTrigger support.
        /// </summary>
        public class LevelModifier
        {
            public Modifier<GameData> TriggerModifier { get; set; }
            public Modifier<GameData> ActionModifier { get; set; }

            public int retriggerAmount = -1;
            [NonSerialized]
            public int current;

            public void AssignModifier(ModifierBase.Type type, int i)
            {
                if (type == ModifierBase.Type.Trigger)
                    AssignTrigger(i);
                if (type == ModifierBase.Type.Action)
                    AssignAction(i);
            }

            public void AssignModifier(int action, int trigger)
            {
                AssignTrigger(trigger);
                AssignAction(action);
            }

            public void AssignTrigger(int i)
            {
                TriggerModifier = DefaultTriggers[Mathf.Clamp(i, 0, DefaultTriggers.Length - 1)];
            }

            public void AssignAction(int i)
            {
                ActionModifier = DefaultActions[Mathf.Clamp(i, 0, DefaultActions.Length - 1)];
            }

            public void Activate()
            {
                var trigger = Trigger();
                if ((trigger && !TriggerModifier.not || !trigger && TriggerModifier.not) && !TriggerModifier.active)
                {
                    Action();

                    if (!TriggerModifier.constant)
                        TriggerModifier.active = true;
                }
                else if (!(trigger && !TriggerModifier.not || !trigger && TriggerModifier.not))
                    TriggerModifier.active = false;
            }

            public bool Trigger()
            {
                var modiifer = TriggerModifier;

                if (modiifer == null || retriggerAmount != -1 && current > retriggerAmount)
                    return false;

                current++;

                var time = Updater.CurrentTime;

                switch (modiifer.commands[0].ToLower())
                {
                    case "none":
                        {
                            return true;
                        }
                    case "time":
                        {
                            return modiifer.commands.Count > 2 && float.TryParse(modiifer.commands[1], out float min) && float.TryParse(modiifer.commands[2], out float max)
                                && time >= min - 0.01f && time <= max + 0.1f;
                        }
                    case "playerhit":
                        {
                            return PlayerManager.Players.Any(x => x.Player != null && x.Player.isTakingHit);
                        }
                    case "playerdeath":
                        {
                            return PlayerManager.Players.Any(x => x.Player != null && x.Player.isDead);
                        }
                    case "levelstart":
                        {
                            return AudioManager.inst.CurrentAudioSource.time < 0.1f;
                        }
                }

                return false;
            }

            public void Action()
            {
                var modifier = ActionModifier;

                if (modifier == null)
                    return;

                switch (modifier.commands[0].ToLower().Replace(" ", "").Replace("_", ""))
                {
                    case "playerlocation":
                        {
                            float x = 0f;
                            float y = 0f;
                            float t = 0f;

                            if (modifier.commands.Count > 1 && float.TryParse(modifier.commands[1], out float xResult))
                                x = xResult;

                            if (modifier.commands.Count > 2 && float.TryParse(modifier.commands[2], out float yResult))
                                y = yResult;

                            if (modifier.commands.Count > 2 && float.TryParse(modifier.commands[2], out float tResult))
                                t = tResult;

                            foreach (var player in PlayerManager.Players)
                            {
                                if (!player.Player || !player.Player.rb)
                                    continue;

                                var tf = player.Player.rb.transform;

                                if (t == 0f)
                                {
                                    tf.localPosition = new Vector3(x, y, 0f);
                                    continue;
                                }

                                var animation = new RTAnimation("Player Move");

                                animation.animationHandlers = new List<AnimationHandlerBase>
                                {
                                    new AnimationHandler<Vector2>(new List<IKeyframe<Vector2>>
                                    {
                                        new Vector2Keyframe(0f, tf.localPosition, Ease.Linear),
                                        new Vector2Keyframe(t, new Vector2(x, y), Ease.Linear),
                                    }, x =>
                                    {
                                        if (!tf)
                                            return;

                                        tf.localPosition = x;
                                    }),
                                };

                                animation.onComplete = () =>
                                {
                                    AnimationManager.inst.Remove(animation.id);
                                    if (tf)
                                        tf.localPosition = new Vector2(x, y);
                                };

                                AnimationManager.inst.Play(animation);
                            }

                            break;
                        }
                    case "playerboostlock":
                        {
                            if (modifier.commands.Count > 3 && !string.IsNullOrEmpty(modifier.commands[1]) && bool.TryParse(modifier.commands[1], out bool lockBoost))
                                RTPlayer.LockBoost = lockBoost;

                            break;
                        }

                }
            }

            public static Modifier<GameData>[] DefaultTriggers => new Modifier<GameData>[]
            {
                new Modifier<GameData>
                {
                    type = ModifierBase.Type.Trigger,
                    constant = true,
                    commands = new List<string>
                    {
                        "time",
                        "0", // Activation Time Range Min
						"0", // Activation Time Range Max
					},
                    value = "",
                }, // time
				new Modifier<GameData>
                {
                    type = ModifierBase.Type.Trigger,
                    constant = false,
                    commands = new List<string>
                    {
                        "playerHit",
                        "0", // Activation Time Range Min
						"0", // Activation Time Range Max
					},
                    value = "",
                }, // playerHit
				new Modifier<GameData>
                {
                    type = ModifierBase.Type.Trigger,
                    constant = false,
                    commands = new List<string>
                    {
                        "playerDeath",
                        "0", // Activation Time Range Min
						"0", // Activation Time Range Max
					},
                    value = "",
                }, // playerDeath
				new Modifier<GameData>
                {
                    type = ModifierBase.Type.Trigger,
                    constant = false,
                    commands = new List<string>
                    {
                        "levelStart",
                        "0", // Activation Time Range Min
						"0", // Activation Time Range Max
					},
                    value = "",
                }, // levelStart
			};
            public static Modifier<GameData>[] DefaultActions => new Modifier<GameData>[]
            {
                new Modifier<GameData>
                {
                    type = ModifierBase.Type.Action,
                    constant = false,
                    commands = new List<string>
                    {
                        "vnInk"
                    },
                    value = "",
                }, // vnInk
				new Modifier<GameData>
                {
                    type = ModifierBase.Type.Action,
                    constant = false,
                    commands = new List<string>
                    {
                        "vnTimeline"
                    },
                    value = "",
                }, // vnTimeline
				new Modifier<GameData>
                {
                    type = ModifierBase.Type.Action,
                    constant = false,
                    commands = new List<string>
                    {
                        "playerBubble",
                        "Text", // Text
						"0", // Time
                    },
                    value = "",
                }, // playerBubble (Probably won't have support for this yet)
				new Modifier<GameData>
                {
                    type = ModifierBase.Type.Action,
                    constant = false,
                    commands = new List<string>
                    {
                        "playerLocation",
                        "0", // X
						"0", // Y
						"0", // Time
                    },
                    value = "",
                }, // playerLocation
				new Modifier<GameData>
                {
                    type = ModifierBase.Type.Action,
                    constant = false,
                    commands = new List<string>
                    {
                        "playerBoostLock",
                        "False", // Lock Enabled
						"", // Show Bubble
						"", // Bubble Time
                    },
                    value = "",
                }, // playerBoostLock
			};
        }

        #region Methods

        public static GameData DeepCopy(GameData orig)
        {
            if (orig.beatmapObjects == null)
                orig.beatmapObjects = new List<Data.BeatmapObject>();
            if (orig.eventObjects == null)
                orig.eventObjects = new EventObjects();
            if (orig.backgroundObjects == null)
                orig.backgroundObjects = new List<Data.BackgroundObject>();

            var gameData = new GameData();
            var beatmapData = new LevelBeatmapData();
            beatmapData.editorData = new BeatmapData.EditorData
            {
                timelinePos = orig.beatmapData.editorData.timelinePos,
                mainTimelineZoom = orig.beatmapData.editorData.mainTimelineZoom
            };
            beatmapData.levelData = new BeatmapData.LevelData
            {
                levelVersion = orig.beatmapData.levelData.levelVersion,
                backgroundColor = orig.beatmapData.levelData.backgroundColor,
                followPlayer = orig.beatmapData.levelData.followPlayer,
                showIntro = orig.beatmapData.levelData.showIntro
            };
            beatmapData.checkpoints = orig.beatmapData.checkpoints.Select(x => BeatmapData.Checkpoint.DeepCopy(x)).ToList();
            beatmapData.markers = new List<BeatmapData.Marker>();
            beatmapData.markers.AddRange(orig.beatmapData.markers.Select(x => new Marker(x.active, x.name, x.desc, x.color, x.time)));

            gameData.beatmapData = beatmapData;
            gameData.beatmapObjects = new List<Data.BeatmapObject>((from obj in orig.beatmapObjects
                                                                   select Data.BeatmapObject.DeepCopy(obj, false)).ToList());
            gameData.backgroundObjects = new List<Data.BackgroundObject>((from obj in orig.backgroundObjects
                                                                         select Data.BackgroundObject.DeepCopy(obj)).ToList());
            gameData.eventObjects = EventObjects.DeepCopy(orig.eventObjects);
            return gameData;
        }

        public static GameData ConvertedGameData { get; set; }

        public static GameData ReadFromFile(string path, FileType fileType, bool parseThemes = true, Version version = default) => fileType switch
        {
            FileType.LS => Parse(JSON.Parse(RTFile.ReadFromFile(path)), parseThemes),
            FileType.VG => ParseVG(JSON.Parse(RTFile.ReadFromFile(path)), parseThemes, version),
            _ => null,
        };

        public static GameData ParseVG(JSONNode jn, bool parseThemes = true, Version version = default)
        {
            var gameData = new GameData();
            var parseOptimizations = CoreConfig.Instance.ParseOptimizations.Value;

            CoreHelper.Log($"Parsing Version: {version}");

            for (int i = 0; i < jn["triggers"].Count; i++)
            {
                var levelModifier = new LevelModifier();

                levelModifier.retriggerAmount = jn["triggers"][i]["event_retrigger"];

                levelModifier.AssignModifier(jn["triggers"][i]["event_type"].AsInt, jn["triggers"][i]["event_trigger"].AsInt);

                for (int j = 0; j < jn["triggers"][i]["event_data"].Count; j++)
                {
                    var data = jn["triggers"][i]["event_data"][j];

                    if (levelModifier.ActionModifier.commands.Count - 2 < data.Count)
                    {
                        levelModifier.ActionModifier.commands.Add(data);
                    }
                    else
                    {
                        levelModifier.ActionModifier.commands[j + 1] = data;
                    }
                }

                levelModifier.TriggerModifier.commands[1] = jn["triggers"][i]["event_trigger_time"]["x"].AsFloat.ToString();
                levelModifier.TriggerModifier.commands[2] = jn["triggers"][i]["event_trigger_time"]["y"].AsFloat.ToString();

                gameData.levelModifiers.Add(levelModifier);
            }

            CoreHelper.Log($"Parsing BeatmapData");
            gameData.beatmapData = LevelBeatmapData.ParseVG(jn);

            gameData.beatmapData.markers = gameData.beatmapData.markers.OrderBy(x => x.time).ToList();

            CoreHelper.Log($"Parsing Checkpoints");
            for (int i = 0; i < jn["checkpoints"].Count; i++)
            {
                var name = jn["checkpoints"][i]["n"] == null ? "" : (string)jn["checkpoints"][i]["n"];
                var time = jn["checkpoints"][i]["t"] == null ? 0f : jn["checkpoints"][i]["t"].AsFloat;
                var pos = jn["checkpoints"][i]["p"] == null ? Vector2.zero : new Vector2(jn["checkpoints"][i]["p"]["x"] == null ? 0f : jn["checkpoints"][i]["p"]["x"].AsFloat, jn["checkpoints"][i]["p"]["y"] == null ? 0f : jn["checkpoints"][i]["p"]["y"].AsFloat);
                gameData.beatmapData.checkpoints.Add(new BeatmapData.Checkpoint(true, name, time, pos));
            }

            CoreHelper.Log($"Parsing Objects");
            for (int i = 0; i < jn["objects"].Count; i++)
                gameData.beatmapObjects.Add(Data.BeatmapObject.ParseVG(jn["objects"][i], version));

            if (parseOptimizations)
                for (int i = 0; i < gameData.beatmapObjects.Count; i++)
                    gameData.beatmapObjects[i].SetAutokillToScale(gameData.beatmapObjects);

            CoreHelper.Log($"Parsing Prefab Objects");
            for (int i = 0; i < jn["prefab_objects"].Count; i++)
                gameData.prefabObjects.Add(Data.PrefabObject.ParseVG(jn["prefab_objects"][i]));

            CoreHelper.Log($"Parsing Prefabs");
            for (int i = 0; i < jn["prefabs"].Count; i++)
                gameData.prefabs.Add(Data.Prefab.ParseVG(jn["prefabs"][i], version));

            Dictionary<string, string> idConversion = new Dictionary<string, string>();

            if (jn["themes"] != null)
            {
                CoreHelper.Log($"Parsing Beatmap Themes");

                if (parseThemes)
                {
                    DataManager.inst.CustomBeatmapThemes.Clear();
                    DataManager.inst.BeatmapThemeIndexToID.Clear();
                    DataManager.inst.BeatmapThemeIDToIndex.Clear();
                }

                for (int i = 0; i < jn["themes"].Count; i++)
                {
                    var beatmapTheme = BeatmapTheme.ParseVG(jn["themes"][i]);

                    if (!string.IsNullOrEmpty(beatmapTheme.VGID) && !idConversion.ContainsKey(beatmapTheme.VGID))
                        idConversion.Add(beatmapTheme.VGID, beatmapTheme.id);

                    if (!gameData.beatmapThemes.ContainsKey(beatmapTheme.id))
                        gameData.beatmapThemes.Add(beatmapTheme.id, beatmapTheme);

                    if (parseThemes)
                    {
                        DataManager.inst.CustomBeatmapThemes.Add(beatmapTheme);
                        if (DataManager.inst.BeatmapThemeIDToIndex.ContainsKey(int.Parse(beatmapTheme.id)))
                        {
                            var list = DataManager.inst.CustomBeatmapThemes.Where(x => x.id == beatmapTheme.id).ToList();
                            var str = "";
                            for (int j = 0; j < list.Count; j++)
                            {
                                str += list[j].name;
                                if (i != list.Count - 1)
                                    str += ", ";
                            }

                            if (CoreHelper.InEditor)
                                EditorManager.inst.DisplayNotification($"Unable to Load theme [{beatmapTheme.name}] due to conflicting themes: {str}", 2f, EditorManager.NotificationType.Error);
                        }
                        else
                        {
                            DataManager.inst.BeatmapThemeIndexToID.Add(DataManager.inst.AllThemes.Count - 1, int.Parse(beatmapTheme.id));
                            DataManager.inst.BeatmapThemeIDToIndex.Add(int.Parse(beatmapTheme.id), DataManager.inst.AllThemes.Count - 1);
                        }
                    }

                    beatmapTheme = null;
                }
            }

            gameData.backgroundObjects.Add(new Data.BackgroundObject
            {
                active = false,
                pos = new Vector2(9999f, 9999f),
            });

            gameData.eventObjects = new EventObjects();
            gameData.eventObjects.allEvents = new List<List<BaseEventKeyframe>>();

            string breakContext = "";
            try
            {
                CoreHelper.Log($"Parsing VG Event Keyframes");
                // Move
                breakContext = "Move";
                gameData.eventObjects.allEvents.Add(new List<BaseEventKeyframe>());
                for (int i = 0; i < jn["events"][0].Count; i++)
                {
                    var eventKeyframe = new Data.EventKeyframe();
                    var kfjn = jn["events"][0][i];

                    eventKeyframe.id = LSText.randomNumString(8);

                    if (kfjn["ct"] != null && DataManager.inst.AnimationListDictionaryStr.TryGetValue(kfjn["ct"], out DataManager.LSAnimation anim))
                        eventKeyframe.curveType = anim;

                    eventKeyframe.eventTime = kfjn["t"].AsFloat;
                    eventKeyframe.SetEventValues(kfjn["ev"][0].AsFloat, kfjn["ev"][1].AsFloat);

                    gameData.eventObjects.allEvents[0].Add(eventKeyframe);
                }

                // Zoom
                breakContext = "Zoom";
                gameData.eventObjects.allEvents.Add(new List<BaseEventKeyframe>());
                for (int i = 0; i < jn["events"][1].Count; i++)
                {
                    var eventKeyframe = new Data.EventKeyframe();
                    var kfjn = jn["events"][1][i];

                    eventKeyframe.id = LSText.randomNumString(8);

                    if (kfjn["ct"] != null && DataManager.inst.AnimationListDictionaryStr.TryGetValue(kfjn["ct"], out DataManager.LSAnimation anim))
                        eventKeyframe.curveType = anim;

                    eventKeyframe.eventTime = kfjn["t"].AsFloat;
                    eventKeyframe.SetEventValues(kfjn["ev"][0].AsFloat);

                    gameData.eventObjects.allEvents[1].Add(eventKeyframe);
                }

                // Rotate
                breakContext = "Rotate";
                gameData.eventObjects.allEvents.Add(new List<BaseEventKeyframe>());
                for (int i = 0; i < jn["events"][2].Count; i++)
                {
                    var eventKeyframe = new Data.EventKeyframe();
                    var kfjn = jn["events"][2][i];

                    eventKeyframe.id = LSText.randomNumString(8);

                    if (kfjn["ct"] != null && DataManager.inst.AnimationListDictionaryStr.TryGetValue(kfjn["ct"], out DataManager.LSAnimation anim))
                        eventKeyframe.curveType = anim;

                    eventKeyframe.eventTime = kfjn["t"].AsFloat;
                    eventKeyframe.SetEventValues(kfjn["ev"][0].AsFloat);

                    gameData.eventObjects.allEvents[2].Add(eventKeyframe);
                }

                // Shake
                breakContext = "Shake";
                gameData.eventObjects.allEvents.Add(new List<BaseEventKeyframe>());
                for (int i = 0; i < jn["events"][3].Count; i++)
                {
                    var eventKeyframe = new Data.EventKeyframe();
                    var kfjn = jn["events"][3][i];

                    eventKeyframe.id = LSText.randomNumString(8);

                    if (kfjn["ct"] != null && DataManager.inst.AnimationListDictionaryStr.TryGetValue(kfjn["ct"], out DataManager.LSAnimation anim))
                        eventKeyframe.curveType = anim;

                    eventKeyframe.eventTime = kfjn["t"].AsFloat;
                    eventKeyframe.SetEventValues(kfjn["ev"][0].AsFloat, 1f, 1f);

                    gameData.eventObjects.allEvents[3].Add(eventKeyframe);
                }

                // Theme
                breakContext = "Theme";
                gameData.eventObjects.allEvents.Add(new List<BaseEventKeyframe>());
                for (int i = 0; i < jn["events"][4].Count; i++)
                {
                    var eventKeyframe = new Data.EventKeyframe();
                    var kfjn = jn["events"][4][i];

                    eventKeyframe.id = LSText.randomNumString(8);

                    if (kfjn["ct"] != null && DataManager.inst.AnimationListDictionaryStr.TryGetValue(kfjn["ct"], out DataManager.LSAnimation anim))
                        eventKeyframe.curveType = anim;

                    eventKeyframe.eventTime = kfjn["t"].AsFloat;
                    // Since theme keyframes use random string IDs as their value instead of numbers (wtf), we have to convert the new IDs to numbers.
                    if (!string.IsNullOrEmpty(kfjn["evs"][0]) && idConversion.TryGetValue(kfjn["evs"][0], out string themeID))
                        eventKeyframe.SetEventValues(Parser.TryParse(themeID, 0f));
                    else
                        eventKeyframe.SetEventValues(0f);

                    gameData.eventObjects.allEvents[4].Add(eventKeyframe);
                }

                // Chroma
                breakContext = "Chroma";
                gameData.eventObjects.allEvents.Add(new List<BaseEventKeyframe>());
                for (int i = 0; i < jn["events"][5].Count; i++)
                {
                    var eventKeyframe = new Data.EventKeyframe();
                    var kfjn = jn["events"][5][i];

                    eventKeyframe.id = LSText.randomNumString(8);

                    if (kfjn["ct"] != null && DataManager.inst.AnimationListDictionaryStr.TryGetValue(kfjn["ct"], out DataManager.LSAnimation anim))
                        eventKeyframe.curveType = anim;

                    eventKeyframe.eventTime = kfjn["t"].AsFloat;
                    eventKeyframe.SetEventValues(kfjn["ev"][0].AsFloat);

                    gameData.eventObjects.allEvents[5].Add(eventKeyframe);
                }

                // Bloom
                breakContext = "Bloom";
                gameData.eventObjects.allEvents.Add(new List<BaseEventKeyframe>());
                for (int i = 0; i < jn["events"][6].Count; i++)
                {
                    var eventKeyframe = new Data.EventKeyframe();
                    var kfjn = jn["events"][6][i];

                    eventKeyframe.id = LSText.randomNumString(8);

                    if (kfjn["ct"] != null && DataManager.inst.AnimationListDictionaryStr.TryGetValue(kfjn["ct"], out DataManager.LSAnimation anim))
                        eventKeyframe.curveType = anim;

                    eventKeyframe.eventTime = kfjn["t"].AsFloat;
                    eventKeyframe.SetEventValues(
                        kfjn["ev"][0].AsFloat,
                        kfjn["ev"][1].AsFloat,
                        1f,
                        0f,
                        kfjn["ev"][2].AsFloat == 9f ? 18f : kfjn["ev"][2].AsFloat);

                    gameData.eventObjects.allEvents[6].Add(eventKeyframe);
                }

                // Vignette
                breakContext = "Vignette";
                gameData.eventObjects.allEvents.Add(new List<BaseEventKeyframe>());
                for (int i = 0; i < jn["events"][7].Count; i++)
                {
                    var eventKeyframe = new Data.EventKeyframe();
                    var kfjn = jn["events"][7][i];

                    eventKeyframe.id = LSText.randomNumString(8);

                    if (kfjn["ct"] != null && DataManager.inst.AnimationListDictionaryStr.TryGetValue(kfjn["ct"], out DataManager.LSAnimation anim))
                        eventKeyframe.curveType = anim;

                    eventKeyframe.eventTime = kfjn["t"].AsFloat;
                    eventKeyframe.SetEventValues(
                        kfjn["ev"][0].AsFloat,
                        kfjn["ev"][1].AsFloat,
                        kfjn["ev"][2].AsFloat,
                        1f,
                        kfjn["ev"][4].AsFloat,
                        kfjn["ev"][5].AsFloat,
                        kfjn["ev"][6].AsFloat == 9f ? 18f : kfjn["ev"][6].AsFloat);

                    gameData.eventObjects.allEvents[7].Add(eventKeyframe);
                }

                // Lens
                breakContext = "Lens";
                gameData.eventObjects.allEvents.Add(new List<BaseEventKeyframe>());
                for (int i = 0; i < jn["events"][8].Count; i++)
                {
                    var eventKeyframe = new Data.EventKeyframe();
                    var kfjn = jn["events"][8][i];

                    eventKeyframe.id = LSText.randomNumString(8);

                    if (kfjn["ct"] != null && DataManager.inst.AnimationListDictionaryStr.TryGetValue(kfjn["ct"], out DataManager.LSAnimation anim))
                        eventKeyframe.curveType = anim;

                    eventKeyframe.eventTime = kfjn["t"].AsFloat;
                    eventKeyframe.SetEventValues(
                        kfjn["ev"][0].AsFloat,
                        kfjn["ev"][1].AsFloat,
                        kfjn["ev"][2].AsFloat,
                        1f,
                        1f,
                        1f);

                    gameData.eventObjects.allEvents[8].Add(eventKeyframe);
                }

                // Grain
                breakContext = "Grain";
                gameData.eventObjects.allEvents.Add(new List<BaseEventKeyframe>());
                for (int i = 0; i < jn["events"][9].Count; i++)
                {
                    var eventKeyframe = new Data.EventKeyframe();
                    var kfjn = jn["events"][9][i];

                    eventKeyframe.id = LSText.randomNumString(8);

                    if (kfjn["ct"] != null && DataManager.inst.AnimationListDictionaryStr.TryGetValue(kfjn["ct"], out DataManager.LSAnimation anim))
                        eventKeyframe.curveType = anim;

                    eventKeyframe.eventTime = kfjn["t"].AsFloat;
                    eventKeyframe.SetEventValues(
                        kfjn["ev"][0].AsFloat,
                        kfjn["ev"][1].AsFloat,
                        kfjn["ev"][2].AsFloat);

                    gameData.eventObjects.allEvents[9].Add(eventKeyframe);
                }

                // Hue
                breakContext = "Hue";
                gameData.eventObjects.allEvents.Add(new List<BaseEventKeyframe>());
                for (int i = 0; i < jn["events"][12].Count; i++)
                {
                    var eventKeyframe = new Data.EventKeyframe();
                    var kfjn = jn["events"][12][i];

                    eventKeyframe.id = LSText.randomNumString(8);

                    if (kfjn["ct"] != null && DataManager.inst.AnimationListDictionaryStr.TryGetValue(kfjn["ct"], out DataManager.LSAnimation anim))
                        eventKeyframe.curveType = anim;

                    eventKeyframe.eventTime = kfjn["t"].AsFloat;
                    eventKeyframe.SetEventValues(
                        kfjn["ev"][0].AsFloat);

                    gameData.eventObjects.allEvents[10].Add(eventKeyframe);
                }

                gameData.eventObjects.allEvents.Add(new List<BaseEventKeyframe>());
                gameData.eventObjects.allEvents[11].Add(Data.EventKeyframe.DeepCopy((Data.EventKeyframe)DefaultKeyframes[11]));
                gameData.eventObjects.allEvents.Add(new List<BaseEventKeyframe>());
                gameData.eventObjects.allEvents[12].Add(Data.EventKeyframe.DeepCopy((Data.EventKeyframe)DefaultKeyframes[12]));
                gameData.eventObjects.allEvents.Add(new List<BaseEventKeyframe>());
                gameData.eventObjects.allEvents[13].Add(Data.EventKeyframe.DeepCopy((Data.EventKeyframe)DefaultKeyframes[13]));
                gameData.eventObjects.allEvents.Add(new List<BaseEventKeyframe>());
                gameData.eventObjects.allEvents[14].Add(Data.EventKeyframe.DeepCopy((Data.EventKeyframe)DefaultKeyframes[14]));

                // Gradient
                breakContext = "Gradient";
                gameData.eventObjects.allEvents.Add(new List<BaseEventKeyframe>());
                for (int i = 0; i < jn["events"][10].Count; i++)
                {
                    var eventKeyframe = new Data.EventKeyframe();
                    var kfjn = jn["events"][10][i];

                    eventKeyframe.id = LSText.randomNumString(8);

                    if (kfjn["ct"] != null && DataManager.inst.AnimationListDictionaryStr.TryGetValue(kfjn["ct"], out DataManager.LSAnimation anim))
                        eventKeyframe.curveType = anim;

                    eventKeyframe.eventTime = kfjn["t"].AsFloat;
                    eventKeyframe.SetEventValues(
                        kfjn["ev"][0].AsFloat,
                        kfjn["ev"][1].AsFloat,
                        kfjn["ev"][2].AsFloat == 9f ? 19f : kfjn["ev"][2].AsFloat,
                        kfjn["ev"][3].AsFloat == 9f ? 19f : kfjn["ev"][3].AsFloat,
                        kfjn["ev"].Count > 4 ? kfjn["ev"][4].AsFloat : 0f);

                    gameData.eventObjects.allEvents[15].Add(eventKeyframe);
                }

                for (int i = 16; i < DefaultKeyframes.Count; i++)
                {
                    gameData.eventObjects.allEvents.Add(new List<BaseEventKeyframe>());
                    gameData.eventObjects.allEvents[i].Add(Data.EventKeyframe.DeepCopy((Data.EventKeyframe)DefaultKeyframes[i]));
                }
            }
            catch (Exception ex)
            {
                if (CoreHelper.InEditor)
                    EditorManager.inst.DisplayNotification($"There was an error in parsing VG Event Keyframes. Parsing got caught at {breakContext}", 4f, EditorManager.NotificationType.Error);
                if (!CoreHelper.InEditor)
                    Debug.LogError($"There was an error in parsing VG Event Keyframes. Parsing got caught at {breakContext}.\n {ex}");
                else
                    Debug.LogError($"{ex}");
            }

            CoreHelper.Log($"Checking keyframe counts");
            ClampEventListValues(gameData.eventObjects.allEvents, EventCount);

            if (jn["events"].Count > 13 && jn["events"][13] != null && gameData.eventObjects.allEvents.Count > 36)
            {
                var playerForce = gameData.eventObjects.allEvents[36];
                var firstKF = (Data.EventKeyframe)playerForce[0];

                firstKF.id = LSText.randomNumString(8);
                firstKF.eventTime = 0f;
                firstKF.SetEventValues(jn["events"][13][0]["ev"][0].AsFloat, jn["events"][13][0]["ev"][1].AsFloat);

                for (int i = 1; i < jn["events"][13].Count; i++)
                {
                    var eventKeyframe = new Data.EventKeyframe();
                    var kfjn = jn["events"][13][i];

                    eventKeyframe.id = LSText.randomNumString(8);
                    
                    if (kfjn["ct"] != null && DataManager.inst.AnimationListDictionaryStr.TryGetValue(kfjn["ct"], out DataManager.LSAnimation anim))
                        eventKeyframe.curveType = anim;

                    eventKeyframe.eventTime = kfjn["t"].AsFloat;
                    eventKeyframe.SetEventValues(
                        kfjn["ev"][0].AsFloat,
                        kfjn["ev"][1].AsFloat);

                    gameData.eventObjects.allEvents[36].Add(eventKeyframe);
                }
            }

            ConvertedGameData = gameData;

            return gameData;
        }

        public static GameData Parse(JSONNode jn, bool parseThemes = true)
        {
            var gameData = new GameData();

            LastParsedJSON = jn;
            var parseOptimizations = CoreConfig.Instance.ParseOptimizations.Value;

            if (jn["modifiers"] != null)
                for (int i = 0; i < jn["modifiers"].Count; i++)
                {
                    var modifier = jn["modifiers"][i];

                    var levelModifier = new LevelModifier();

                    levelModifier.ActionModifier = Modifier<GameData>.Parse(modifier["action"], gameData);
                    levelModifier.TriggerModifier = Modifier<GameData>.Parse(modifier["trigger"], gameData);
                    levelModifier.retriggerAmount = modifier["retrigger"].AsInt;

                    gameData.levelModifiers.Add(levelModifier);
                }

            gameData.beatmapData = LevelBeatmapData.Parse(jn);

            gameData.beatmapData.markers = gameData.beatmapData.markers.OrderBy(x => x.time).ToList();

            for (int i = 0; i < jn["checkpoints"].Count; i++)
            {
                var jnCheckpoint = jn["checkpoints"][i];
                gameData.beatmapData.checkpoints.Add(new BeatmapData.Checkpoint(
                    jnCheckpoint["active"].AsBool,
                    jnCheckpoint["name"],
                    jnCheckpoint["t"].AsFloat,
                    new Vector2(
                        jnCheckpoint["pos"]["x"].AsFloat,
                        jnCheckpoint["pos"]["y"].AsFloat)));
            }

            gameData.beatmapData.checkpoints = gameData.beatmapData.checkpoints.OrderBy(x => x.time).ToList();

            for (int i = 0; i < jn["prefabs"].Count; i++)
            {
                var prefab = Data.Prefab.Parse(jn["prefabs"][i]);
                if (gameData.prefabs.Find(x => x.ID == prefab.ID) == null)
                    gameData.prefabs.Add(prefab);
            }

            for (int i = 0; i < jn["prefab_objects"].Count; i++)
            {
                var prefab = Data.PrefabObject.Parse(jn["prefab_objects"][i]);
                if (gameData.prefabObjects.Find(x => x.ID == prefab.ID) == null)
                    gameData.prefabObjects.Add(prefab);
            }

            foreach (var theme in DataManager.inst.BeatmapThemes)
                gameData.beatmapThemes.Add(theme.id, theme);

            if (parseThemes)
            {
                DataManager.inst.CustomBeatmapThemes.Clear();
                DataManager.inst.BeatmapThemeIndexToID.Clear();
                DataManager.inst.BeatmapThemeIDToIndex.Clear();
            }
            for (int i = 0; i < jn["themes"].Count; i++)
            {
                var beatmapTheme = BeatmapTheme.Parse(jn["themes"][i]);

                if (parseThemes)
                    DataManager.inst.CustomBeatmapThemes.Add(beatmapTheme);
                if (parseThemes && DataManager.inst.BeatmapThemeIDToIndex.ContainsKey(int.Parse(beatmapTheme.id)))
                {
                    var list = DataManager.inst.CustomBeatmapThemes.Where(x => x.id == beatmapTheme.id).ToList();
                    var str = "";
                    for (int j = 0; j < list.Count; j++)
                    {
                        str += list[j].name;
                        if (i != list.Count - 1)
                            str += ", ";
                    }

                    if (CoreHelper.InEditor)
                        EditorManager.inst.DisplayNotification($"Unable to Load theme [{beatmapTheme.name}] due to conflicting themes: {str}", 2f, EditorManager.NotificationType.Error);
                }
                else if (parseThemes)
                {
                    DataManager.inst.BeatmapThemeIndexToID.Add(DataManager.inst.AllThemes.Count - 1, int.Parse(beatmapTheme.id));
                    DataManager.inst.BeatmapThemeIDToIndex.Add(int.Parse(beatmapTheme.id), DataManager.inst.AllThemes.Count - 1);
                }

                if (!string.IsNullOrEmpty(jn["themes"][i]["id"]) && !gameData.beatmapThemes.ContainsKey(jn["themes"][i]["id"]))
                    gameData.beatmapThemes.Add(jn["themes"][i]["id"], beatmapTheme);
            }

            for (int i = 0; i < jn["beatmap_objects"].Count; i++)
            {
                var beatmapObject = Data.BeatmapObject.Parse(jn["beatmap_objects"][i]);

                if (gameData.beatmapObjects.TryFindIndex(x => x.id == beatmapObject.id, out int index))
                    gameData.beatmapObjects.RemoveAt(index);

                gameData.beatmapObjects.Add(beatmapObject);
                gameData.modifierCount += beatmapObject.modifiers.Count;
            }

            if (parseOptimizations)
                for (int i = 0; i < gameData.beatmapObjects.Count; i++)
                    ((Data.BeatmapObject)gameData.beatmapObjects[i]).SetAutokillToScale(gameData.beatmapObjects);
            
            AssetManager.SpriteAssets.Clear();
            if (jn["assets"] != null && jn["assets"]["spr"] != null)
            {
                for (int i = 0; i < jn["assets"]["spr"].Count; i++)
                {
                    var name = jn["assets"]["spr"][i]["n"];
                    var data = jn["assets"]["spr"][i]["d"];

                    if (!AssetManager.SpriteAssets.ContainsKey(name) && gameData.beatmapObjects.Has(x => x.text == name))
                    {
                        if (jn["assets"]["spr"][i]["i"] != null)
                        {
                            AssetManager.SpriteAssets.Add(name, SpriteHelper.StringToSprite(jn["assets"]["spr"][i]["i"]));
                            continue;
                        }

                        byte[] imageData = new byte[data.Count];
                        for (int j = 0; j < data.Count; j++)
                        {
                            imageData[j] = (byte)data[j].AsInt;
                        }

                        var texture2d = new Texture2D(2, 2, TextureFormat.ARGB32, false);
                        texture2d.LoadImage(imageData);

                        texture2d.wrapMode = TextureWrapMode.Clamp;
                        texture2d.filterMode = FilterMode.Point;
                        texture2d.Apply();

                        AssetManager.SpriteAssets.Add(name, SpriteHelper.CreateSprite(texture2d));
                    }
                }
            }

            for (int i = 0; i < jn["bg_objects"].Count; i++)
                gameData.backgroundObjects.Add(Data.BackgroundObject.Parse(jn["bg_objects"][i]));

            gameData.eventObjects.allEvents = ParseEventkeyframes(jn["events"], false);

            // Fix for some levels having a Y value in shake, resulting in a shake with a 0 x direction value.
            var shakeIsBroke = false;
            if (jn["events"]["shake"] != null)
                for (int i = 0; i < jn["events"]["shake"].Count; i++)
                {
                    if (jn["events"]["shake"][i]["y"] != null && jn["events"]["shake"][i]["z"] == null)
                        shakeIsBroke = true;
                }

            if (shakeIsBroke)
                for (int i = 0; i < gameData.eventObjects.allEvents[3].Count; i++)
                {
                    gameData.eventObjects.allEvents[3][i].eventValues[1] = 1f;
                }

            try
            {
                if (gameData.beatmapData is LevelBeatmapData levelBeatmapData &&
                    levelBeatmapData.levelData is LevelData modLevelData &&
                    Version.TryParse(modLevelData.modVersion, out Version modVersion) && modVersion < new Version(1, 3, 4))
                {
                    for (int i = 0; i < gameData.eventObjects.allEvents[3].Count; i++)
                    {
                        gameData.eventObjects.allEvents[3][i].eventValues[3] = 0f;
                    }
                }
            }
            catch (Exception ex)
            {
                CoreHelper.LogException(ex);
            }

            ClampEventListValues(gameData.eventObjects.allEvents, EventCount);

            return gameData;
        }

        /// <summary>
        /// Parsed JSON for debugging purposes.
        /// </summary>
        public static JSONNode LastParsedJSON { get; set; }

        public static int EventCount => DefaultKeyframes.Count;

        public JSONNode ToJSONVG()
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

            for (int i = 0; i < levelModifiers.Count; i++)
            {
                var levelModifier = levelModifiers[i];

                var triggerIndex = LevelModifier.DefaultTriggers.ToList().FindIndex(x => x.commands[0] == levelModifier.TriggerModifier.commands[0]);
                var actionIndex = LevelModifier.DefaultActions.ToList().FindIndex(x => x.commands[0] == levelModifier.ActionModifier.commands[0]);

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

            for (int i = 0; i < beatmapData.checkpoints.Count; i++)
            {
                var checkpoint = beatmapData.checkpoints[i];
                jn["checkpoints"][i]["n"] = checkpoint.name;
                jn["checkpoints"][i]["t"] = checkpoint.time;
                jn["checkpoints"][i]["p"]["X"] = checkpoint.pos.x;
                jn["checkpoints"][i]["p"]["y"] = checkpoint.pos.y;
            }

            for (int i = 0; i < beatmapObjects.Count; i++)
                jn["objects"][i] = beatmapObjects[i].ToJSONVG();

            if (prefabObjects.Count > 0)
                for (int i = 0; i < prefabObjects.Count; i++)
                    jn["prefab_objects"][i] = prefabObjects[i].ToJSONVG();
            else
                jn["prefab_objects"] = new JSONArray();

            if (prefabs.Count > 0)
                for (int i = 0; i < prefabs.Count; i++)
                    jn["prefabs"][i] = prefabs[i].ToJSONVG();
            else
                jn["prefabs"] = new JSONArray();

            var idsConverter = new Dictionary<string, string>();

            int themeIndex = 0;
            var themes = DataManager.inst.CustomBeatmapThemes.Select(x => x as BeatmapTheme).Where(x => eventObjects.allEvents[4].Has(y => int.TryParse(x.id, out int id) && id == y.eventValues[0]));
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

            if (beatmapData.markers.Count > 0)
                for (int i = 0; i < beatmapData.markers.Count; i++)
                    jn["markers"][i] = ((Marker)beatmapData.markers[i]).ToJSONVG();
            else
                jn["markers"] = new JSONArray();

            // Event Handlers
            {
                // Move
                for (int i = 0; i < eventObjects.allEvents[0].Count; i++)
                {
                    var eventKeyframe = eventObjects.allEvents[0][i];
                    jn["events"][0][i]["ct"] = eventKeyframe.curveType.Name;
                    jn["events"][0][i]["t"] = eventKeyframe.eventTime;
                    jn["events"][0][i]["ev"][0] = eventKeyframe.eventValues[0];
                    jn["events"][0][i]["ev"][1] = eventKeyframe.eventValues[1];
                }

                // Zoom
                for (int i = 0; i < eventObjects.allEvents[1].Count; i++)
                {
                    var eventKeyframe = eventObjects.allEvents[1][i];
                    jn["events"][1][i]["ct"] = eventKeyframe.curveType.Name;
                    jn["events"][1][i]["t"] = eventKeyframe.eventTime;
                    jn["events"][1][i]["ev"][0] = eventKeyframe.eventValues[0];
                }

                // Rotate
                for (int i = 0; i < eventObjects.allEvents[2].Count; i++)
                {
                    var eventKeyframe = eventObjects.allEvents[2][i];
                    jn["events"][2][i]["ct"] = eventKeyframe.curveType.Name;
                    jn["events"][2][i]["t"] = eventKeyframe.eventTime;
                    jn["events"][2][i]["ev"][0] = eventKeyframe.eventValues[0];
                }

                // Shake
                for (int i = 0; i < eventObjects.allEvents[3].Count; i++)
                {
                    var eventKeyframe = eventObjects.allEvents[3][i];
                    jn["events"][3][i]["ct"] = eventKeyframe.curveType.Name;
                    jn["events"][3][i]["t"] = eventKeyframe.eventTime;
                    jn["events"][3][i]["ev"][0] = eventKeyframe.eventValues[0];
                }

                // Themes
                for (int i = 0; i < eventObjects.allEvents[4].Count; i++)
                {
                    var eventKeyframe = eventObjects.allEvents[4][i];
                    jn["events"][4][i]["ct"] = eventKeyframe.curveType.Name;
                    jn["events"][4][i]["t"] = eventKeyframe.eventTime;
                    jn["events"][4][i]["evs"][0] = idsConverter.TryGetValue(eventKeyframe.eventValues[0].ToString(), out string themeID) ? themeID : eventKeyframe.eventValues[0].ToString();
                }

                // Chroma
                for (int i = 0; i < eventObjects.allEvents[5].Count; i++)
                {
                    var eventKeyframe = eventObjects.allEvents[5][i];
                    jn["events"][5][i]["ct"] = eventKeyframe.curveType.Name;
                    jn["events"][5][i]["t"] = eventKeyframe.eventTime;
                    jn["events"][5][i]["ev"][0] = eventKeyframe.eventValues[0];
                }

                // Bloom
                for (int i = 0; i < eventObjects.allEvents[6].Count; i++)
                {
                    var eventKeyframe = eventObjects.allEvents[6][i];
                    jn["events"][6][i]["ct"] = eventKeyframe.curveType.Name;
                    jn["events"][6][i]["t"] = eventKeyframe.eventTime;
                    jn["events"][6][i]["ev"][0] = eventKeyframe.eventValues[0];
                    jn["events"][6][i]["ev"][1] = eventKeyframe.eventValues[1];
                    jn["events"][6][i]["ev"][2] = Mathf.Clamp(eventKeyframe.eventValues[4], 0f, 9f);
                }

                // Vignette
                for (int i = 0; i < eventObjects.allEvents[7].Count; i++)
                {
                    var eventKeyframe = eventObjects.allEvents[7][i];
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
                for (int i = 0; i < eventObjects.allEvents[8].Count; i++)
                {
                    var eventKeyframe = eventObjects.allEvents[8][i];
                    jn["events"][8][i]["ct"] = eventKeyframe.curveType.Name;
                    jn["events"][8][i]["t"] = eventKeyframe.eventTime;
                    jn["events"][8][i]["ev"][0] = eventKeyframe.eventValues[0];
                    jn["events"][8][i]["ev"][1] = eventKeyframe.eventValues[1];
                    jn["events"][8][i]["ev"][2] = eventKeyframe.eventValues[2];
                }

                // Grain
                for (int i = 0; i < eventObjects.allEvents[9].Count; i++)
                {
                    var eventKeyframe = eventObjects.allEvents[9][i];
                    jn["events"][9][i]["ct"] = eventKeyframe.curveType.Name;
                    jn["events"][9][i]["t"] = eventKeyframe.eventTime;
                    jn["events"][9][i]["ev"][0] = eventKeyframe.eventValues[0];
                    jn["events"][9][i]["ev"][1] = eventKeyframe.eventValues[1];
                    jn["events"][9][i]["ev"][2] = eventKeyframe.eventValues[2];
                    jn["events"][9][i]["ev"][3] = 1f;
                }

                // Gradient
                for (int i = 0; i < eventObjects.allEvents[15].Count; i++)
                {
                    var eventKeyframe = eventObjects.allEvents[15][i];
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
                for (int i = 0; i < eventObjects.allEvents[10].Count; i++)
                {
                    var eventKeyframe = eventObjects.allEvents[10][i];
                    jn["events"][12][i]["ct"] = eventKeyframe.curveType.Name;
                    jn["events"][12][i]["t"] = eventKeyframe.eventTime;
                    jn["events"][12][i]["ev"][0] = eventKeyframe.eventValues[0];
                }

                // Player
                for (int i = 0; i < eventObjects.allEvents[36].Count; i++)
                {
                    var eventKeyframe = eventObjects.allEvents[36][i];
                    jn["events"][13][i]["ct"] = eventKeyframe.curveType.Name;
                    jn["events"][13][i]["t"] = eventKeyframe.eventTime;
                    jn["events"][13][i]["ev"][0] = eventKeyframe.eventValues[0];
                    jn["events"][13][i]["ev"][1] = eventKeyframe.eventValues[1];
                    jn["events"][13][i]["ev"][2] = 0f;
                }
            }

            return jn;
        }

        public JSONNode ToJSON(bool saveGameDataThemes = false)
        {
            CoreHelper.Log("Saving Beatmap");
            var jn = JSON.Parse("{}");

            CoreHelper.Log("Saving Editor Data");
            jn["ed"]["timeline_pos"] = "0";

            CoreHelper.Log("Saving Markers");
            for (int i = 0; i < beatmapData.markers.Count; i++)
                jn["ed"]["markers"][i] = ((Marker)beatmapData.markers[i]).ToJSON();

            for (int i = 0; i < levelModifiers.Count; i++)
            {
                var levelModifier = levelModifiers[i];

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
            var prefabObjects = this.prefabObjects.FindAll(x => !x.fromModifier);
            for (int i = 0; i < prefabObjects.Count; i++)
                jn["prefab_objects"][i] = prefabObjects[i].ToJSON();

            CoreHelper.Log("Saving Level Data");
            jn["level_data"] = beatmapData.ModLevelData.ToJSON();

            CoreHelper.Log("Saving prefabs");
            if (prefabs != null)
                for (int i = 0; i < prefabs.Count; i++)
                    jn["prefabs"][i] = prefabs[i].ToJSON();

            CoreHelper.Log($"Saving themes");
            var levelThemes =
                saveGameDataThemes ?
                    beatmapThemes.Where(x => Parser.TryParse(x.Value.id, 0) != 0 && eventObjects.allEvents[4].Has(y => y.eventValues[0] == Parser.TryParse(x.Value.id, 0))).Select(x => x.Value).ToList() :
                    DataManager.inst.CustomBeatmapThemes.Where(x => Parser.TryParse(x.id, 0) != 0 && eventObjects.allEvents[4].Has(y => y.eventValues[0] == Parser.TryParse(x.id, 0))).ToList();

            for (int i = 0; i < levelThemes.Count; i++)
            {
                CoreHelper.Log($"Saving {levelThemes[i].id} - {levelThemes[i].name} to level!");
                jn["themes"][i] = ((BeatmapTheme)levelThemes[i]).ToJSON();
            }

            CoreHelper.Log("Saving Checkpoints");
            for (int i = 0; i < beatmapData.checkpoints.Count; i++)
            {
                jn["checkpoints"][i]["active"] = "False";
                jn["checkpoints"][i]["name"] = beatmapData.checkpoints[i].name;
                jn["checkpoints"][i]["t"] = beatmapData.checkpoints[i].time.ToString();
                jn["checkpoints"][i]["pos"]["x"] = beatmapData.checkpoints[i].pos.x.ToString();
                jn["checkpoints"][i]["pos"]["y"] = beatmapData.checkpoints[i].pos.y.ToString();
            }

            CoreHelper.Log("Saving Beatmap Objects");
            if (beatmapObjects != null)
            {
                var list = beatmapObjects.FindAll(x => !x.fromPrefab);
                jn["beatmap_objects"] = new JSONArray();
                for (int i = 0; i < list.Count; i++)
                    jn["beatmap_objects"][i] = list[i].ToJSON();
            }
            else
            {
                CoreHelper.Log("skipping objects");
                jn["beatmap_objects"] = new JSONArray();
            }

            CoreHelper.Log("Saving Background Objects");
            for (int i = 0; i < backgroundObjects.Count; i++)
                jn["bg_objects"][i] = backgroundObjects[i].ToJSON();

            CoreHelper.Log("Saving Event Objects");
            for (int i = 0; i < eventObjects.allEvents.Count; i++)
                for (int j = 0; j < eventObjects.allEvents[i].Count; j++)
                    if (EventTypes.Length > i)
                        jn["events"][EventTypes[i]][j] = ((Data.EventKeyframe)eventObjects.allEvents[i][j]).ToJSON();

            return jn;
        }

        public void SaveData(string path, Action onSave = null, bool saveGameDataThemes = false)
        {
            if (EditorConfig.Instance.SaveAsync.Value)
                CoreHelper.StartCoroutineAsync(ISaveData(path, onSave, saveGameDataThemes));
            else
                CoreHelper.StartCoroutine(ISaveData(path, onSave, saveGameDataThemes));
        }
        
        public void SaveDataVG(string path, Action onSave = null)
        {
            if (EditorConfig.Instance.SaveAsync.Value)
                CoreHelper.StartCoroutineAsync(ISaveDataVG(path, onSave));
            else
                CoreHelper.StartCoroutine(ISaveDataVG(path, onSave));
        }

        public IEnumerator ISaveData(string path, Action onSave = null, bool saveGameDataThemes = false)
        {
            var jn = ToJSON(saveGameDataThemes);
            CoreHelper.Log($"Saving Entire Beatmap to {path}");
            RTFile.WriteToFile(path, jn.ToString());

            yield return CielaSpike.Ninja.JumpToUnity;
            onSave?.Invoke();

            yield break;
        }

        public IEnumerator ISaveDataVG(string path, Action onSave = null)
        {
            var jn = ToJSONVG();
            CoreHelper.Log($"Saving Entire Beatmap to {path}");
            RTFile.WriteToFile(path, jn.ToString());

            yield return CielaSpike.Ninja.JumpToUnity;
            onSave?.Invoke();

            yield break;
        }

        public static List<List<BaseEventKeyframe>> ParseEventkeyframes(JSONNode jn, bool clamp = true)
        {
            var allEvents = new List<List<BaseEventKeyframe>>();

            for (int i = 0; i < EventCount; i++)
            {
                allEvents.Add(new List<BaseEventKeyframe>());
                if (jn[EventTypes[i]] != null)
                    for (int j = 0; j < jn[EventTypes[i]].Count; j++)
                        allEvents[i].Add(Data.EventKeyframe.Parse(jn[EventTypes[i]][j], i, DefaultKeyframes[i].eventValues.Length));
            }

            if (clamp)
                ClampEventListValues(allEvents, EventCount);

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
                    eventKeyframes[type].Add(Data.EventKeyframe.DeepCopy((Data.EventKeyframe)DefaultKeyframes[type]));

                for (int index = 0; index < eventKeyframes[type].Count; index++)
                {
                    var array = eventKeyframes[type][index].eventValues;
                    if (array.Length != DefaultKeyframes[type].eventValues.Length)
                    {
                        array = new float[DefaultKeyframes[type].eventValues.Length];
                        for (int i = 0; i < DefaultKeyframes[type].eventValues.Length; i++)
                            array[i] = i < eventKeyframes[type][index].eventValues.Length ? eventKeyframes[type][index].eventValues[i] : DefaultKeyframes[type].eventValues[i];
                    }
                    eventKeyframes[type][index].eventValues = array;
                }
            }
        }

        #endregion

        public int modifierCount;

        #region Verifying

        public bool Modded => BeatmapObjectsModded || EventKeyframesModded || PrefabObjectsModded;

        bool BeatmapObjectsModded => beatmapObjects.Any(x => x.modifiers.Count > 0
                    || x.objectType == Data.BeatmapObject.ObjectType.Solid
                    || x.desync
                    || x.background
                    || x.LDM
                    || x.parallaxSettings.Any(y => y != 1f)
                    || x.parentAdditive != "000"
                    || x.shape > UnmoddedShapeOptions.Length - 1
                    || x.shapeOption >= UnmoddedShapeOptions[Mathf.Clamp(x.shape, 0, UnmoddedShapeOptions.Length - 1)]
                    || ArePositionKeyframesModded(x.events[0])
                    || AreScaleKeyframesModded(x.events[1])
                    || AreRotationKeyframesModded(x.events[2])
                    || AreColorKeyframesModded(x.events[3]));

        bool EventKeyframesModded
        {
            get
            {
                bool eventKeyframesModded = false;

                for (int i = 0; i < eventObjects.allEvents.Count; i++)
                {
                    for (int j = 0; j < eventObjects.allEvents[i].Count; j++)
                    {
                        var eventKeyframe = (Data.EventKeyframe)eventObjects.allEvents[i][j];

                        for (int k = 0; k < eventKeyframe.eventValues.Length; k++)
                        {
                            if ((DefaultUnmoddedEventKeyframes.Length <= i || DefaultUnmoddedEventKeyframes[i] <= k) && DefaultKeyframes[i].eventValues[k] != eventKeyframe.eventValues[k])
                            {
                                eventKeyframesModded = true;
                                break;
                            }
                        }

                        if (eventKeyframesModded)
                            break;
                    }

                    if (eventKeyframesModded)
                        break;
                }

                return eventKeyframesModded;
            }
        }

        bool PrefabObjectsModded => prefabObjects.Any(x => x.RepeatCount > 0 || x.speed != 1f || !string.IsNullOrEmpty(x.parent) || x.autoKillType != Data.PrefabObject.AutoKillType.Regular);

        static bool ArePositionKeyframesModded(List<BaseEventKeyframe> eventKeyframes)
            => eventKeyframes.Any(x => x.random > 4 || x.eventValues.Length > 2 && x.eventValues[2] != 0f || ((Data.EventKeyframe)x).relative);

        static bool AreScaleKeyframesModded(List<BaseEventKeyframe> eventKeyframes)
            => eventKeyframes.Any(x => ((Data.EventKeyframe)x).relative);

        static bool AreRotationKeyframesModded(List<BaseEventKeyframe> eventKeyframes)
            => eventKeyframes.Any(x => x.random > 4 || !((Data.EventKeyframe)x).relative);

        static bool AreColorKeyframesModded(List<BaseEventKeyframe> eventKeyframes)
            => eventKeyframes.Any(x => x.random > 4 || x.eventValues[0] > 8f || x.eventValues[2] != 0f || x.eventValues[3] != 0f || x.eventValues[4] != 0f);

        public static int[] UnmoddedShapeOptions => new int[]
        {
            3,
            9,
            4,
            2,
            1,
            6
        };

        /// <summary>
        /// For comparing modded values.
        /// </summary>
        public static int[] DefaultUnmoddedEventKeyframes => new int[]
        {
            2, // Move
            1, // Zoom
            1, // Rotate
            1, // Shake
            1, // Theme
            1, // Chroma
            1, // Bloom
            6, // Vignette
            1, // Lens
            3, // Grain
        };

        public static string[] EventTypes => new string[]
        {
            "pos", // 0
			"zoom", // 1
			"rot", // 2
			"shake", // 3
			"theme", // 4
			"chroma", // 5
			"bloom", // 6
			"vignette", // 7
			"lens", // 8
			"grain", // 9
			"cg", // 10
			"rip", // 11
			"rb", // 12
			"cs", // 13
			"offset", // 14
			"grd", // 15
			"dbv", // 16
			"scan", // 17
			"blur", // 18
			"pixel", // 19
			"bg", // 20
			"invert", // 21
			"timeline", // 22
			"player", // 23
			"follow_player", // 24
			"audio", // 25
			"vidbg_p", // 26
			"vidbg", // 27
			"sharp", // 28
			"bars", // 29
			"danger", // 30
			"xyrot", // 31
			"camdepth", // 32
			"winbase", // 33
			"winposx", // 34
			"winposy", // 35
			"playerforce", // 36
			"mosaic", // 37
			"analog_glitch", // 38
			"digital_glitch", // 39
		};

        public static List<BaseEventKeyframe> DefaultKeyframes = new List<BaseEventKeyframe>
        {
            new Data.EventKeyframe
            {
                eventTime = 0f,
                eventValues = new float[2],
                id = LSText.randomNumString(8),
            }, // Move
			new Data.EventKeyframe
            {
                eventTime = 0f,
                eventValues = new float[1]
                { 20f },
                id = LSText.randomNumString(8),
            }, // Zoom
			new Data.EventKeyframe
            {
                eventTime = 0f,
                eventValues = new float[1],
                id = LSText.randomNumString(8),
            }, // Rotate
			new Data.EventKeyframe
            {
                eventTime = 0f,
                eventValues = new float[5]
                {
                    0f, // Shake Intensity
					1f, // Shake X
					1f, // Shake Y
					0f, // Shake Interpolation
					1f, // Shake Speed
                },
                id = LSText.randomNumString(8),
            }, // Shake
			new Data.EventKeyframe
            {
                eventTime = 0f,
                eventValues = new float[1],
                id = LSText.randomNumString(8),
            }, // Theme
			new Data.EventKeyframe
            {
                eventTime = 0f,
                eventValues = new float[1],
                id = LSText.randomNumString(8),
            }, // Chroma
			new Data.EventKeyframe
            {
                eventTime = 0f,
                eventValues = new float[8]
                {
                    0f, // Bloom Intensity
					7f, // Bloom Diffusion
					1f, // Bloom Threshold
					0f, // Bloom Anamorphic Ratio
					18f, // Bloom Color
					0f, // Bloom Hue
					0f, // Bloom Sat
					0f, // Bloom Val
				},
                id = LSText.randomNumString(8),
            }, // Bloom
			new Data.EventKeyframe
            {
                eventTime = 0f,
                eventValues = new float[10]
                {
                    0f, // Vignette Intensity
					0f, // Vignette Smoothness
					0f, // Vignette Rounded
					0f, // Vignette Roundness
					0f, // Vignette Center X
					0f, // Vignette Center Y
					18f, // Vignette Color
					0f, // Vignette Hue
					0f, // Vignette Sat
					0f, // Vignette Val
                },
                id = LSText.randomNumString(8),
            }, // Vignette
			new Data.EventKeyframe
            {
                eventTime = 0f,
                eventValues = new float[6]
                {
                    0f,
                    0f,
                    0f,
                    1f,
                    1f,
                    1f
                },
                id = LSText.randomNumString(8),
            }, // Lens
			new Data.EventKeyframe
            {
                eventTime = 0f,
                eventValues = new float[3],
                id = LSText.randomNumString(8),
            }, // Grain
			new Data.EventKeyframe
            {
                eventTime = 0f,
                eventValues = new float[9],
                id = LSText.randomNumString(8),
            }, // ColorGrading
			new Data.EventKeyframe
            {
                eventTime = 0f,
                eventValues = new float[6]
                {
                    0f,
                    0f,
                    1f,
                    0f,
                    0f,
                    0f,
                },
                id = LSText.randomNumString(8),
            }, // Ripples
			new Data.EventKeyframe
            {
                eventTime = 0f,
                eventValues = new float[2]
                {
                    0f,
                    6f
                },
                id = LSText.randomNumString(8),
            }, // RadialBlur
			new Data.EventKeyframe
            {
                eventTime = 0f,
                eventValues = new float[2],
                id = LSText.randomNumString(8),
            }, // ColorSplit
			new Data.EventKeyframe
            {
                eventTime = 0f,
                eventValues = new float[2],
                id = LSText.randomNumString(8),
            }, // Offset
			new Data.EventKeyframe
            {
                eventTime = 0f,
                eventValues = new float[13]
                {
                    0f,
                    0f,
                    18f,
                    18f,
                    0f,
                    1f, // Top Opacity
					0f, // Top Hue
					0f, // Top Sat
					0f, // Top Val
					1f, // Bottom Opacity
					0f, // Bottom Hue
					0f, // Bottom Sat
					0f, // Bottom Val
				},
                id = LSText.randomNumString(8),
            }, // Gradient
			new Data.EventKeyframe
            {
                eventTime = 0f,
                eventValues = new float[2],
                id = LSText.randomNumString(8),
            }, // DoubleVision
			new Data.EventKeyframe
            {
                eventTime = 0f,
                eventValues = new float[3],
                id = LSText.randomNumString(8),
            }, // ScanLines
			new Data.EventKeyframe
            {
                eventTime = 0f,
                eventValues = new float[2]
                {
                    0f,
                    6f
                },
                id = LSText.randomNumString(8),
            }, // Blur
			new Data.EventKeyframe
            {
                eventTime = 0f,
                eventValues = new float[1],
                id = LSText.randomNumString(8),
            }, // Pixelize
			new Data.EventKeyframe
            {
                eventTime = 0f,
                eventValues = new float[5]
                {
                    18f, // Color
					0f, // Active
					0f, // Hue
					0f, // Sat
					0f, // Val
				},
                id = LSText.randomNumString(8),
            }, // BG
			new Data.EventKeyframe
            {
                eventTime = 0f,
                eventValues = new float[1],
                id = LSText.randomNumString(8),
            }, // Invert
			new Data.EventKeyframe
            {
                eventTime = 0f,
                eventValues = new float[11]
                {
                    0f,
                    0f,
                    -342f,
                    1f,
                    1f,
                    0f,
                    18f,
                    1f, // Opacity
					0f, // Hue
					0f, // Sat
					0f, // Val
				},
                id = LSText.randomNumString(8),
            }, // Timeline
			new Data.EventKeyframe
            {
                eventTime = 0f,
                eventValues = new float[6],
                id = LSText.randomNumString(8),
            }, // Player
			new Data.EventKeyframe
            {
                eventTime = 0f,
                eventValues = new float[10]
                {
                    0f, // Active
					0f, // Move
					0f, // Rotate
					0.5f,
                    0f,
                    9999f,
                    -9999f,
                    9999f,
                    -9999f,
                    1f,
                },
                id = LSText.randomNumString(8),
            }, // Follow Player
			new Data.EventKeyframe
            {
                eventTime = 0f,
                eventValues = new float[2]
                {
                    1f,
                    1f
                },
                id = LSText.randomNumString(8),
            }, // Audio
			new Data.EventKeyframe
            {
                eventTime = 0f,
                eventValues = new float[9]
                {
                    0f, // Position X
                    0f, // Position Y
                    0f, // Position Z
                    1f, // Scale X
                    1f, // Scale Y
                    1f, // Scale Z
                    0f, // Rotation X
                    0f, // Rotation Y
                    0f, // Rotation Z
                },
                id = LSText.randomNumString(8),
            }, // Video BG Parent
			new Data.EventKeyframe
            {
                eventTime = 0f,
                eventValues = new float[10]
                {
                    0f, // Position X
                    0f, // Position Y
                    120f, // Position Z
                    240f, // Scale X
                    135f, // Scale Y
                    1f, // Scale Z
                    0f, // Rotation X
                    0f, // Rotation Y
                    0f, // Rotation Z
                    0f, // Render Layer (Foreground / Background)
                },
                id = LSText.randomNumString(8),
            }, // Video BG
			new Data.EventKeyframe
            {
                eventTime = 0f,
                eventValues = new float[1]
                {
                    0f, // Sharpen Amount
                },
                id = LSText.randomNumString(8),
            }, // Sharpen
			new Data.EventKeyframe
            {
                eventTime = 0f,
                eventValues = new float[2]
                {
                    0f, // Amount
					0f, // Mode
                },
                id = LSText.randomNumString(8),
            }, // Bars
			new Data.EventKeyframe
            {
                eventTime = 0f,
                eventValues = new float[7]
                {
                    0f, // Intensity
					0f, // Size
					18f, // Color
					1f, // Opacity
					0f, // Hue
					0f, // Sat
					0f, // Val
                },
                id = LSText.randomNumString(8),
            }, // Danger
			new Data.EventKeyframe
            {
                eventTime = 0f,
                eventValues = new float[2]
                {
                    0f, // X
					0f, // Y
                },
                id = LSText.randomNumString(8),
            }, // 3D Rotation
			new Data.EventKeyframe
            {
                eventTime = 0f,
                eventValues = new float[3]
                {
                    -10f, // Depth
					0f, // Zoom
					0f, // Global Position
                },
                id = LSText.randomNumString(8),
            }, // Camera Depth
			new Data.EventKeyframe
            {
                eventTime = 0f,
                eventValues = new float[4]
                {
                    0f, // Force Resolution (1 = true, includes position)
					1280f, // X
					720f, // Y
					0f, // Allow Position
                },
                id = LSText.randomNumString(8),
            }, // Window Base
			new Data.EventKeyframe
            {
                eventTime = 0f,
                eventValues = new float[1]
                {
                    0f, // Position X
                },
                id = LSText.randomNumString(8),
            }, // Window Position X
			new Data.EventKeyframe
            {
                eventTime = 0f,
                eventValues = new float[1]
                {
                    0f, // Position Y
                },
                id = LSText.randomNumString(8),
            }, // Window Position Y
			new Data.EventKeyframe
            {
                eventTime = 0f,
                eventValues = new float[2]
                {
                    0f, // Player Force X
					0f, // Player Force Y
                },
                id = LSText.randomNumString(8),
            }, // Player Force
			new Data.EventKeyframe
            {
                eventTime = 0f,
                eventValues = new float[1]
                {
                    0f, // Intensity
                },
                id = LSText.randomNumString(8),
            }, // Mosaic
			new Data.EventKeyframe
            {
                eventTime = 0f,
                eventValues = new float[5]
                {
                    0f, // Enabled
                    0f, // ColorDrift
                    0f, // HorizontalShake
                    0f, // ScanLineJitter
                    0f, // VerticalJump
                },
                id = LSText.randomNumString(8),
            }, // Analog Glitch
			new Data.EventKeyframe
            {
                eventTime = 0f,
                eventValues = new float[1]
                {
                    0f, // Intensity
                },
                id = LSText.randomNumString(8),
            }, // Digital Glitch
		};

        public static bool SaveOpacityToThemes { get; set; } = false;

        #endregion

        public new LevelBeatmapData beatmapData;

        public new List<Data.BeatmapObject> beatmapObjects = new List<Data.BeatmapObject>();

        public new List<Data.PrefabObject> prefabObjects = new List<Data.PrefabObject>();

        public new List<Data.Prefab> prefabs = new List<Data.Prefab>();

        public new List<Data.BackgroundObject> backgroundObjects = new List<Data.BackgroundObject>();

        [NonSerialized]
        public new List<GameObject> backgroundGameObjects = new List<GameObject>();

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
                var baseData = new GameData
                {
                    beatmapData = new LevelBeatmapData
                    {
                        editorData = new LevelEditorData(),
                        levelData = new LevelData(),
                    },
                };

                if (gameDatas != null && gameDatas.Length > 0)
                    for (int i = 0; i < gameDatas.Length; i++)
                    {
                        if (gameDatas[i].beatmapData != null && baseData.beatmapData != null)
                        {
                            if (baseData.beatmapData.checkpoints == null)
                                baseData.beatmapData.checkpoints = new List<BeatmapData.Checkpoint>();
                            if (baseData.beatmapData.markers == null)
                                baseData.beatmapData.markers = new List<BeatmapData.Marker>();

                            baseData.beatmapData.checkpoints.AddRange(gameDatas[i].beatmapData.checkpoints.FindAll(x => !baseData.beatmapData.checkpoints.Has(y => y.time == x.time)));
                            baseData.beatmapData.markers.AddRange(gameDatas[i].beatmapData.markers.FindAll(x => !baseData.beatmapData.markers.Has(y => y.time == x.time)));
                        }

                        if (baseData.beatmapObjects == null)
                            baseData.beatmapObjects = new List<Data.BeatmapObject>();

                        baseData.beatmapObjects.AddRange(gameDatas[i].beatmapObjects.FindAll(x => !baseData.beatmapObjects.Has(y => y.id == x.id)));

                        if (baseData.prefabObjects == null)
                            baseData.prefabObjects = new List<Data.PrefabObject>();

                        baseData.prefabObjects.AddRange(gameDatas[i].prefabObjects.Where(x => !baseData.prefabObjects.Has(y => y.ID == x.ID)));

                        if (baseData.prefabs == null)
                            baseData.prefabs = new List<Data.Prefab>();

                        baseData.prefabs.AddRange(gameDatas[i].prefabs.FindAll(x => !baseData.prefabs.Has(y => y.ID == x.ID)));

                        baseData.backgroundObjects.AddRange(gameDatas[i].backgroundObjects.Where(x => !baseData.backgroundObjects.Has(y =>
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
                            baseData.eventObjects = new EventObjects();

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

    }
}