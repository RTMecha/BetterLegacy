using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

using UnityEngine;

using LSFunctions;

using SimpleJSON;

using BetterLegacy.Configs;
using BetterLegacy.Core.Animation;
using BetterLegacy.Core.Animation.Keyframe;
using BetterLegacy.Core.Components.Player;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using BetterLegacy.Core.Runtime;

namespace BetterLegacy.Core.Data.Beatmap
{
    /// <summary>
    /// Represents a Project Arrhythmia level.
    /// </summary>
    public class GameData : Exists
    {
        public GameData() { }

        #region Properties

        #region Instance

        /// <summary>
        /// The current GameData that is being used by the game.
        /// </summary>
        public static GameData Current { get; set; }

        #endregion

        #region Verifying

        public bool Modded => BeatmapObjectsModded || EventKeyframesModded || PrefabObjectsModded;

        bool BeatmapObjectsModded => beatmapObjects.Any(x => x.modifiers.Count > 0
                    || x.objectType == BeatmapObject.ObjectType.Solid
                    || x.desync
                    || x.renderLayerType != BeatmapObject.RenderLayerType.Foreground
                    || x.LDM
                    || x.parallaxSettings.Any(y => y != 1f)
                    || x.parentAdditive != "000"
                    || x.shape > UnmoddedShapeOptions.Length - 1
                    || x.shapeOption >= UnmoddedShapeOptions[Mathf.Clamp(x.shape, 0, UnmoddedShapeOptions.Length - 1)]
                    || x.events[0].Any(x => x.random > 4 || x.values.Length > 2 && x.values[2] != 0f || x.relative)
                    || x.events[1].Any(x => x.relative)
                    || x.events[2].Any(x => x.random > 4 || !x.relative)
                    || x.events[3].Any(x => x.random > 4 || x.values[0] > 8f || x.values[2] != 0f || x.values[3] != 0f || x.values[4] != 0f));

        bool EventKeyframesModded
        {
            get
            {
                bool eventKeyframesModded = false;

                for (int i = 0; i < events.Count; i++)
                {
                    for (int j = 0; j < events[i].Count; j++)
                    {
                        var eventKeyframe = events[i][j];

                        for (int k = 0; k < eventKeyframe.values.Length; k++)
                        {
                            if ((DefaultUnmoddedEventKeyframes.Length <= i || DefaultUnmoddedEventKeyframes[i] <= k) && DefaultKeyframes[i].values[k] != eventKeyframe.values[k])
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

        bool PrefabObjectsModded => prefabObjects.Any(x => x.RepeatCount > 0 || x.Speed != 1f || !string.IsNullOrEmpty(x.parent) || x.autoKillType != PrefabObject.AutoKillType.Regular);

        #endregion

        #region Events

        /// <summary>
        /// The total amount of event keyframes.
        /// </summary>
        public static int EventCount => DefaultKeyframes.Count;

        /// <summary>
        /// If opacity should be saved to themes.
        /// </summary>
        public static bool SaveOpacityToThemes { get; set; } = false;

        /// <summary>
        /// The vanilla event keyframe value counts.
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

        /// <summary>
        /// The JSON names of all the events in BetterLegacy.
        /// </summary>
        public static string[] EventTypes => new string[]
        {
            #region Vanilla

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

            #endregion

            #region Modded

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

            #endregion
        };

        /// <summary>
        /// The default events in BetterLegacy.
        /// </summary>
        public static List<EventKeyframe> DefaultKeyframes => new List<EventKeyframe>
        {
            #region Vanilla

            new EventKeyframe
            {
                time = 0f,
                values = new float[2],
                id = LSText.randomNumString(8),
            }, // Move
			new EventKeyframe
            {
                time = 0f,
                values = new float[1]
                { 20f },
                id = LSText.randomNumString(8),
            }, // Zoom
			new EventKeyframe
            {
                time = 0f,
                values = new float[1],
                id = LSText.randomNumString(8),
            }, // Rotate
			new EventKeyframe
            {
                time = 0f,
                values = new float[5]
                {
                    0f, // Shake Intensity
					1f, // Shake X
					1f, // Shake Y
					0f, // Shake Interpolation
					1f, // Shake Speed
                },
                id = LSText.randomNumString(8),
            }, // Shake
			new EventKeyframe
            {
                time = 0f,
                values = new float[1],
                id = LSText.randomNumString(8),
            }, // Theme
			new EventKeyframe
            {
                time = 0f,
                values = new float[1],
                id = LSText.randomNumString(8),
            }, // Chroma
			new EventKeyframe
            {
                time = 0f,
                values = new float[8]
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
			new EventKeyframe
            {
                time = 0f,
                values = new float[10]
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
			new EventKeyframe
            {
                time = 0f,
                values = new float[6]
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
			new EventKeyframe
            {
                time = 0f,
                values = new float[3],
                id = LSText.randomNumString(8),
            }, // Grain

            #endregion

            #region Modded

            new EventKeyframe
            {
                time = 0f,
                values = new float[9],
                id = LSText.randomNumString(8),
            }, // ColorGrading
			new EventKeyframe
            {
                time = 0f,
                values = new float[6]
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
			new EventKeyframe
            {
                time = 0f,
                values = new float[2]
                {
                    0f,
                    6f
                },
                id = LSText.randomNumString(8),
            }, // RadialBlur
			new EventKeyframe
            {
                time = 0f,
                values = new float[2],
                id = LSText.randomNumString(8),
            }, // ColorSplit
			new EventKeyframe
            {
                time = 0f,
                values = new float[2],
                id = LSText.randomNumString(8),
            }, // Offset
			new EventKeyframe
            {
                time = 0f,
                values = new float[13]
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
			new EventKeyframe
            {
                time = 0f,
                values = new float[2],
                id = LSText.randomNumString(8),
            }, // DoubleVision
			new EventKeyframe
            {
                time = 0f,
                values = new float[3],
                id = LSText.randomNumString(8),
            }, // ScanLines
			new EventKeyframe
            {
                time = 0f,
                values = new float[2]
                {
                    0f,
                    6f
                },
                id = LSText.randomNumString(8),
            }, // Blur
			new EventKeyframe
            {
                time = 0f,
                values = new float[1],
                id = LSText.randomNumString(8),
            }, // Pixelize
			new EventKeyframe
            {
                time = 0f,
                values = new float[5]
                {
                    18f, // Color
					0f, // Active
					0f, // Hue
					0f, // Sat
					0f, // Val
				},
                id = LSText.randomNumString(8),
            }, // BG
			new EventKeyframe
            {
                time = 0f,
                values = new float[1],
                id = LSText.randomNumString(8),
            }, // Invert
			new EventKeyframe
            {
                time = 0f,
                values = new float[11]
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
			new EventKeyframe
            {
                time = 0f,
                values = new float[6],
                id = LSText.randomNumString(8),
            }, // Player
			new EventKeyframe
            {
                time = 0f,
                values = new float[10]
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
			new EventKeyframe
            {
                time = 0f,
                values = new float[2]
                {
                    1f,
                    1f
                },
                id = LSText.randomNumString(8),
            }, // Audio
			new EventKeyframe
            {
                time = 0f,
                values = new float[9]
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
			new EventKeyframe
            {
                time = 0f,
                values = new float[10]
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
			new EventKeyframe
            {
                time = 0f,
                values = new float[1]
                {
                    0f, // Sharpen Amount
                },
                id = LSText.randomNumString(8),
            }, // Sharpen
			new EventKeyframe
            {
                time = 0f,
                values = new float[2]
                {
                    0f, // Amount
					0f, // Mode
                },
                id = LSText.randomNumString(8),
            }, // Bars
			new EventKeyframe
            {
                time = 0f,
                values = new float[7]
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
			new EventKeyframe
            {
                time = 0f,
                values = new float[2]
                {
                    0f, // X
					0f, // Y
                },
                id = LSText.randomNumString(8),
            }, // 3D Rotation
			new EventKeyframe
            {
                time = 0f,
                values = new float[4]
                {
                    -10f, // Depth
					0f, // Zoom
					0f, // Global Position
					1f, // Near Clip Plane Align
                },
                id = LSText.randomNumString(8),
            }, // Camera Depth
			new EventKeyframe
            {
                time = 0f,
                values = new float[4]
                {
                    0f, // Force Resolution (1 = true, includes position)
					1280f, // X
					720f, // Y
					0f, // Allow Position
                },
                id = LSText.randomNumString(8),
            }, // Window Base
			new EventKeyframe
            {
                time = 0f,
                values = new float[1]
                {
                    0f, // Position X
                },
                id = LSText.randomNumString(8),
            }, // Window Position X
			new EventKeyframe
            {
                time = 0f,
                values = new float[1]
                {
                    0f, // Position Y
                },
                id = LSText.randomNumString(8),
            }, // Window Position Y
			new EventKeyframe
            {
                time = 0f,
                values = new float[2]
                {
                    0f, // Player Force X
					0f, // Player Force Y
                },
                id = LSText.randomNumString(8),
            }, // Player Force
			new EventKeyframe
            {
                time = 0f,
                values = new float[1]
                {
                    0f, // Intensity
                },
                id = LSText.randomNumString(8),
            }, // Mosaic
			new EventKeyframe
            {
                time = 0f,
                values = new float[5]
                {
                    0f, // Enabled
                    0f, // ColorDrift
                    0f, // HorizontalShake
                    0f, // ScanLineJitter
                    0f, // VerticalJump
                },
                id = LSText.randomNumString(8),
            }, // Analog Glitch
			new EventKeyframe
            {
                time = 0f,
                values = new float[1]
                {
                    0f, // Intensity
                },
                id = LSText.randomNumString(8),
            }, // Digital Glitch

            #endregion
        };

        #endregion

        /// <summary>
        /// The vanilla shape options.
        /// </summary>
        public static int[] UnmoddedShapeOptions => new int[]
        {
            3,
            9,
            4,
            2,
            1,
            6
        };

        // debug
        //public static JSONNode LastParsedJSON { get; set; }
        //public static GameData ConvertedGameData { get; set; }

        #endregion

        #region Methods

        /// <summary>
        /// Creates a copy of a <see cref="GameData"/>.
        /// </summary>
        /// <param name="orig">Original to copy.</param>
        /// <returns>Returns a copied <see cref="GameData"/>.</returns>
        public static GameData DeepCopy(GameData orig)
        {
            if (orig.beatmapObjects == null)
                orig.beatmapObjects = new List<BeatmapObject>();
            if (orig.events == null)
                orig.events = new List<List<EventKeyframe>>();
            if (orig.backgroundObjects == null)
                orig.backgroundObjects = new List<BackgroundObject>();

            var gameData = new GameData();
            var beatmapData = new LevelBeatmapData();
            beatmapData.editor = new LevelEditorData
            {
                timelinePos = orig.data.editor.timelinePos,
                mainTimelineZoom = orig.data.editor.mainTimelineZoom
            };
            beatmapData.level = new LevelData
            {
                levelVersion = orig.data.level.levelVersion,
                backgroundColor = orig.data.level.backgroundColor,
                followPlayer = orig.data.level.followPlayer,
                showIntro = orig.data.level.showIntro
            };
            beatmapData.checkpoints = orig.data.checkpoints.Select(x => x.Copy()).ToList();
            beatmapData.markers = orig.data.markers.Select(x => x.Copy()).ToList();

            gameData.data = beatmapData;
            gameData.beatmapObjects = new List<BeatmapObject>((from obj in orig.beatmapObjects
                                                                   select obj.Copy(false)));
            gameData.backgroundLayers = new List<BackgroundLayer>((from obj in orig.backgroundLayers
                                                                   select obj.Copy(false)));
            gameData.backgroundObjects = new List<BackgroundObject>((from obj in orig.backgroundObjects
                                                                         select obj.Copy(false)));
            for (int i = 0; i < orig.events.Count; i++)
                gameData.events.Add(orig.events[i].Select(x => x.Copy()).ToList());
            return gameData;
        }

        /// <summary>
        /// Parses a level from a file.
        /// </summary>
        /// <param name="path">File to parse.</param>
        /// <param name="fileType">The type of Project Arrhythmia the file is from.</param>
        /// <param name="parseThemes">If the levels' themes should overwrite the current global list of themes.</param>
        /// <param name="version">The exact version the level is from.</param>
        /// <returns>Returns a parsed <see cref="GameData"/>.</returns>
        public static GameData ReadFromFile(string path, ArrhythmiaType fileType, Version version = default) => fileType switch
        {
            ArrhythmiaType.LS => Parse(JSON.Parse(RTFile.ReadFromFile(path))),
            ArrhythmiaType.VG => ParseVG(JSON.Parse(RTFile.ReadFromFile(path)), version),
            _ => null,
        };

        /// <summary>
        /// Parses a level from JSON in the VG format.
        /// </summary>
        /// <param name="jn">VG JSON to parse.</param>
        /// <param name="parseThemes">If the levels' themes should overwrite the current global list of themes.</param>
        /// <param name="version">The exact version the level is from.</param>
        /// <returns>Returns a parsed <see cref="GameData"/>.</returns>
        public static GameData ParseVG(JSONNode jn, Version version = default)
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
            gameData.data = LevelBeatmapData.ParseVG(jn);

            CoreHelper.Log($"Parsing Objects");
            for (int i = 0; i < jn["objects"].Count; i++)
                gameData.beatmapObjects.Add(BeatmapObject.ParseVG(jn["objects"][i], version));

            if (parseOptimizations)
                for (int i = 0; i < gameData.beatmapObjects.Count; i++)
                    gameData.beatmapObjects[i].SetAutokillToScale(gameData.beatmapObjects);

            CoreHelper.Log($"Parsing Prefab Objects");
            for (int i = 0; i < jn["prefab_objects"].Count; i++)
                gameData.prefabObjects.Add(PrefabObject.ParseVG(jn["prefab_objects"][i]));

            CoreHelper.Log($"Parsing Prefabs");
            for (int i = 0; i < jn["prefabs"].Count; i++)
                gameData.prefabs.Add(Prefab.ParseVG(jn["prefabs"][i], version));

            Dictionary<string, string> idConversion = new Dictionary<string, string>();

            if (jn["themes"] != null)
            {
                CoreHelper.Log($"Parsing Beatmap Themes");

                for (int i = 0; i < jn["themes"].Count; i++)
                {
                    var beatmapTheme = BeatmapTheme.ParseVG(jn["themes"][i]);

                    if (!string.IsNullOrEmpty(beatmapTheme.VGID) && !idConversion.ContainsKey(beatmapTheme.VGID))
                        idConversion.Add(beatmapTheme.VGID, beatmapTheme.id);

                    gameData.beatmapThemes.Add(beatmapTheme);

                    beatmapTheme = null;
                }

                ThemeManager.inst.UpdateAllThemes();
            }

            if (jn["parallax_settings"] != null)
                for (int i = 0; i < jn["parallax_settings"]["l"].Count; i++)
                {
                    var jnLayer = jn["parallax_settings"]["l"][i];
                    var backgroundLayer = BackgroundLayer.ParseVG(jnLayer, version);
                    gameData.backgroundLayers.Add(backgroundLayer);
                    for (int j = 0; j < jnLayer["o"].Count; j++)
                    {
                        var bg = BackgroundObject.ParseVG(jnLayer["o"][j], version);
                        bg.layer = backgroundLayer.id;
                        gameData.backgroundObjects.Add(bg);
                    }
                }

            gameData.events = new List<List<EventKeyframe>>();

            string breakContext = "";
            try
            {
                CoreHelper.Log($"Parsing VG Event Keyframes");
                // Move
                breakContext = "Move";
                gameData.events.Add(new List<EventKeyframe>());
                for (int i = 0; i < jn["events"][0].Count; i++)
                {
                    var eventKeyframe = new EventKeyframe();
                    var kfjn = jn["events"][0][i];

                    eventKeyframe.id = LSText.randomNumString(8);

                    if (kfjn["ct"] != null)
                        eventKeyframe.curve = Parser.TryParse(kfjn["ct"], Easing.Linear);

                    eventKeyframe.time = kfjn["t"].AsFloat;
                    eventKeyframe.SetEventValues(kfjn["ev"][0].AsFloat, kfjn["ev"][1].AsFloat);

                    gameData.events[0].Add(eventKeyframe);
                }

                // Zoom
                breakContext = "Zoom";
                gameData.events.Add(new List<EventKeyframe>());
                for (int i = 0; i < jn["events"][1].Count; i++)
                {
                    var eventKeyframe = new EventKeyframe();
                    var kfjn = jn["events"][1][i];

                    eventKeyframe.id = LSText.randomNumString(8);

                    if (kfjn["ct"] != null)
                        eventKeyframe.curve = Parser.TryParse(kfjn["ct"], Easing.Linear);

                    eventKeyframe.time = kfjn["t"].AsFloat;
                    eventKeyframe.SetEventValues(kfjn["ev"][0].AsFloat);

                    gameData.events[1].Add(eventKeyframe);
                }

                // Rotate
                breakContext = "Rotate";
                gameData.events.Add(new List<EventKeyframe>());
                for (int i = 0; i < jn["events"][2].Count; i++)
                {
                    var eventKeyframe = new EventKeyframe();
                    var kfjn = jn["events"][2][i];

                    eventKeyframe.id = LSText.randomNumString(8);

                    if (kfjn["ct"] != null)
                        eventKeyframe.curve = Parser.TryParse(kfjn["ct"], Easing.Linear);

                    eventKeyframe.time = kfjn["t"].AsFloat;
                    eventKeyframe.SetEventValues(kfjn["ev"][0].AsFloat);

                    gameData.events[2].Add(eventKeyframe);
                }

                // Shake
                breakContext = "Shake";
                gameData.events.Add(new List<EventKeyframe>());
                for (int i = 0; i < jn["events"][3].Count; i++)
                {
                    var eventKeyframe = new EventKeyframe();
                    var kfjn = jn["events"][3][i];

                    eventKeyframe.id = LSText.randomNumString(8);

                    if (kfjn["ct"] != null)
                        eventKeyframe.curve = Parser.TryParse(kfjn["ct"], Easing.Linear);

                    eventKeyframe.time = kfjn["t"].AsFloat;

                    if (kfjn["ev"].Count > 3)
                        eventKeyframe.SetEventValues(kfjn["ev"][0].AsFloat, kfjn["ev"][1].AsFloat, kfjn["ev"][2].AsFloat);
                    else
                        eventKeyframe.SetEventValues(kfjn["ev"][0].AsFloat, 1f, 1f);

                    gameData.events[3].Add(eventKeyframe);
                }

                // Theme
                breakContext = "Theme";
                gameData.events.Add(new List<EventKeyframe>());
                for (int i = 0; i < jn["events"][4].Count; i++)
                {
                    var eventKeyframe = new EventKeyframe();
                    var kfjn = jn["events"][4][i];

                    eventKeyframe.id = LSText.randomNumString(8);

                    if (kfjn["ct"] != null)
                        eventKeyframe.curve = Parser.TryParse(kfjn["ct"], Easing.Linear);

                    eventKeyframe.time = kfjn["t"].AsFloat;
                    // Since theme keyframes use random string IDs as their value instead of numbers (wtf), we have to convert the new IDs to numbers.
                    if (!string.IsNullOrEmpty(kfjn["evs"][0]) && idConversion.TryGetValue(kfjn["evs"][0], out string themeID))
                        eventKeyframe.SetEventValues(Parser.TryParse(themeID, 0f));
                    else
                        eventKeyframe.SetEventValues(0f);

                    gameData.events[4].Add(eventKeyframe);
                }

                // Chroma
                breakContext = "Chroma";
                gameData.events.Add(new List<EventKeyframe>());
                for (int i = 0; i < jn["events"][5].Count; i++)
                {
                    var eventKeyframe = new EventKeyframe();
                    var kfjn = jn["events"][5][i];

                    eventKeyframe.id = LSText.randomNumString(8);

                    if (kfjn["ct"] != null)
                        eventKeyframe.curve = Parser.TryParse(kfjn["ct"], Easing.Linear);

                    eventKeyframe.time = kfjn["t"].AsFloat;
                    eventKeyframe.SetEventValues(kfjn["ev"][0].AsFloat);

                    gameData.events[5].Add(eventKeyframe);
                }

                // Bloom
                breakContext = "Bloom";
                gameData.events.Add(new List<EventKeyframe>());
                for (int i = 0; i < jn["events"][6].Count; i++)
                {
                    var eventKeyframe = new EventKeyframe();
                    var kfjn = jn["events"][6][i];

                    eventKeyframe.id = LSText.randomNumString(8);

                    if (kfjn["ct"] != null)
                        eventKeyframe.curve = Parser.TryParse(kfjn["ct"], Easing.Linear);

                    eventKeyframe.time = kfjn["t"].AsFloat;
                    eventKeyframe.SetEventValues(
                        kfjn["ev"][0].AsFloat,
                        kfjn["ev"][1].AsFloat,
                        1f,
                        0f,
                        kfjn["ev"][2].AsFloat == 9f ? 18f : kfjn["ev"][2].AsFloat);

                    gameData.events[6].Add(eventKeyframe);
                }

                // Vignette
                breakContext = "Vignette";
                gameData.events.Add(new List<EventKeyframe>());
                for (int i = 0; i < jn["events"][7].Count; i++)
                {
                    var eventKeyframe = new EventKeyframe();
                    var kfjn = jn["events"][7][i];

                    eventKeyframe.id = LSText.randomNumString(8);

                    if (kfjn["ct"] != null)
                        eventKeyframe.curve = Parser.TryParse(kfjn["ct"], Easing.Linear);

                    eventKeyframe.time = kfjn["t"].AsFloat;
                    eventKeyframe.SetEventValues(
                        kfjn["ev"][0].AsFloat,
                        kfjn["ev"][1].AsFloat,
                        kfjn["ev"][2].AsFloat,
                        1f,
                        kfjn["ev"][4].AsFloat,
                        kfjn["ev"][5].AsFloat,
                        kfjn["ev"][6].AsFloat == 9f ? 18f : kfjn["ev"][6].AsFloat);

                    gameData.events[7].Add(eventKeyframe);
                }

                // Lens
                breakContext = "Lens";
                gameData.events.Add(new List<EventKeyframe>());
                for (int i = 0; i < jn["events"][8].Count; i++)
                {
                    var eventKeyframe = new EventKeyframe();
                    var kfjn = jn["events"][8][i];

                    eventKeyframe.id = LSText.randomNumString(8);

                    if (kfjn["ct"] != null)
                        eventKeyframe.curve = Parser.TryParse(kfjn["ct"], Easing.Linear);

                    eventKeyframe.time = kfjn["t"].AsFloat;
                    eventKeyframe.SetEventValues(
                        kfjn["ev"][0].AsFloat,
                        kfjn["ev"][1].AsFloat,
                        kfjn["ev"][2].AsFloat,
                        1f,
                        1f,
                        1f);

                    gameData.events[8].Add(eventKeyframe);
                }

                // Grain
                breakContext = "Grain";
                gameData.events.Add(new List<EventKeyframe>());
                for (int i = 0; i < jn["events"][9].Count; i++)
                {
                    var eventKeyframe = new EventKeyframe();
                    var kfjn = jn["events"][9][i];

                    eventKeyframe.id = LSText.randomNumString(8);

                    if (kfjn["ct"] != null)
                        eventKeyframe.curve = Parser.TryParse(kfjn["ct"], Easing.Linear);

                    eventKeyframe.time = kfjn["t"].AsFloat;
                    eventKeyframe.SetEventValues(
                        kfjn["ev"][0].AsFloat,
                        kfjn["ev"][1].AsFloat,
                        kfjn["ev"][2].AsFloat);

                    gameData.events[9].Add(eventKeyframe);
                }

                // Hue
                breakContext = "Hue";
                gameData.events.Add(new List<EventKeyframe>());
                for (int i = 0; i < jn["events"][12].Count; i++)
                {
                    var eventKeyframe = new EventKeyframe();
                    var kfjn = jn["events"][12][i];

                    eventKeyframe.id = LSText.randomNumString(8);

                    if (kfjn["ct"] != null)
                        eventKeyframe.curve = Parser.TryParse(kfjn["ct"], Easing.Linear);

                    eventKeyframe.time = kfjn["t"].AsFloat;
                    eventKeyframe.SetEventValues(
                        kfjn["ev"][0].AsFloat);

                    gameData.events[10].Add(eventKeyframe);
                }

                gameData.events.Add(new List<EventKeyframe>());
                gameData.events[11].Add(DefaultKeyframes[11].Copy());
                gameData.events.Add(new List<EventKeyframe>());
                gameData.events[12].Add(DefaultKeyframes[12].Copy());
                gameData.events.Add(new List<EventKeyframe>());
                gameData.events[13].Add(DefaultKeyframes[13].Copy());
                gameData.events.Add(new List<EventKeyframe>());
                gameData.events[14].Add(DefaultKeyframes[14].Copy());

                // Gradient
                breakContext = "Gradient";
                gameData.events.Add(new List<EventKeyframe>());
                for (int i = 0; i < jn["events"][10].Count; i++)
                {
                    var eventKeyframe = new EventKeyframe();
                    var kfjn = jn["events"][10][i];

                    eventKeyframe.id = LSText.randomNumString(8);

                    if (kfjn["ct"] != null)
                        eventKeyframe.curve = Parser.TryParse(kfjn["ct"], Easing.Linear);

                    eventKeyframe.time = kfjn["t"].AsFloat;
                    eventKeyframe.SetEventValues(
                        kfjn["ev"][0].AsFloat,
                        kfjn["ev"][1].AsFloat,
                        kfjn["ev"][2].AsFloat == 9f ? 19f : kfjn["ev"][2].AsFloat,
                        kfjn["ev"][3].AsFloat == 9f ? 19f : kfjn["ev"][3].AsFloat,
                        kfjn["ev"].Count > 4 ? kfjn["ev"][4].AsFloat : 0f);

                    gameData.events[15].Add(eventKeyframe);
                }

                for (int i = 16; i < DefaultKeyframes.Count; i++)
                {
                    gameData.events.Add(new List<EventKeyframe>());
                    gameData.events[i].Add(DefaultKeyframes[i].Copy());
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
            ClampEventListValues(gameData.events);

            if (jn["events"].Count > 13 && jn["events"][13] != null && gameData.events.Count > 36)
            {
                var playerForce = gameData.events[36];
                var firstKF = playerForce[0];

                firstKF.id = LSText.randomNumString(8);
                firstKF.time = 0f;
                firstKF.SetEventValues(jn["events"][13][0]["ev"][0].AsFloat, jn["events"][13][0]["ev"][1].AsFloat);

                for (int i = 1; i < jn["events"][13].Count; i++)
                {
                    var eventKeyframe = new EventKeyframe();
                    var kfjn = jn["events"][13][i];

                    eventKeyframe.id = LSText.randomNumString(8);

                    if (kfjn["ct"] != null)
                        eventKeyframe.curve = Parser.TryParse(kfjn["ct"], Easing.Linear);

                    eventKeyframe.time = kfjn["t"].AsFloat;
                    eventKeyframe.SetEventValues(
                        kfjn["ev"][0].AsFloat,
                        kfjn["ev"][1].AsFloat);

                    gameData.events[36].Add(eventKeyframe);
                }
            }

            //ConvertedGameData = gameData;

            return gameData;
        }

        /// <summary>
        /// Parses a level from JSON in the LS format.
        /// </summary>
        /// <param name="jn">LS JSON to parse.</param>
        /// <param name="parseThemes">If the levels' themes should overwrite the current global list of themes.</param>
        /// <returns>Returns a parsed <see cref="GameData"/>.</returns>
        public static GameData Parse(JSONNode jn)
        {
            var gameData = new GameData();

            //LastParsedJSON = jn;
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

            gameData.data = LevelBeatmapData.Parse(jn);

            for (int i = 0; i < jn["prefabs"].Count; i++)
            {
                var prefab = Prefab.Parse(jn["prefabs"][i]);
                if (gameData.prefabs.Find(x => x.id == prefab.id) == null)
                    gameData.prefabs.Add(prefab);
            }

            for (int i = 0; i < jn["prefab_objects"].Count; i++)
            {
                var prefab = PrefabObject.Parse(jn["prefab_objects"][i]);
                if (gameData.prefabObjects.Find(x => x.id == prefab.id) == null)
                    gameData.prefabObjects.Add(prefab);
            }

            foreach (var theme in ThemeManager.inst.DefaultThemes)
                gameData.beatmapThemes.Add(theme);

            for (int i = 0; i < jn["themes"].Count; i++)
            {
                if (string.IsNullOrEmpty(jn["themes"][i]["id"]))
                    continue;

                gameData.beatmapThemes.Add(BeatmapTheme.Parse(jn["themes"][i]));
            }

            for (int i = 0; i < jn["beatmap_objects"].Count; i++)
            {
                var beatmapObject = BeatmapObject.Parse(jn["beatmap_objects"][i]);

                // remove objects with duplicate ID's due to a stupid dev branch bug
                if (gameData.beatmapObjects.TryFindIndex(x => x.id == beatmapObject.id, out int index))
                    gameData.beatmapObjects.RemoveAt(index);

                gameData.beatmapObjects.Add(beatmapObject);
            }

            if (parseOptimizations)
                for (int i = 0; i < gameData.beatmapObjects.Count; i++)
                    gameData.beatmapObjects[i].SetAutokillToScale(gameData.beatmapObjects);
            
            gameData.assets.Clear();
            if (jn["assets"] != null)
                gameData.assets.ReadJSON(jn["assets"]);

            if (jn["bg_layers"] != null)
                for (int i = 0; i < jn["bg_layers"].Count; i++)
                    gameData.backgroundLayers.Add(BackgroundLayer.Parse(jn["bg_layers"][i]));

            for (int i = 0; i < jn["bg_objects"].Count; i++)
                gameData.backgroundObjects.Add(BackgroundObject.Parse(jn["bg_objects"][i]));

            gameData.events = ParseEventkeyframes(jn["events"], false);

            // Fix for some levels having a Y value in shake, resulting in a shake with a 0 x direction value.
            var shakeIsBroke = false;
            if (jn["events"]["shake"] != null)
                for (int i = 0; i < jn["events"]["shake"].Count; i++)
                {
                    if (jn["events"]["shake"][i]["y"] != null && jn["events"]["shake"][i]["z"] == null)
                        shakeIsBroke = true;
                }

            if (shakeIsBroke)
                for (int i = 0; i < gameData.events[3].Count; i++)
                    gameData.events[3][i].SetValue(1, 1f);

            try
            {
                if (gameData.data is LevelBeatmapData levelBeatmapData &&
                    levelBeatmapData.level is LevelData modLevelData &&
                    Version.TryParse(modLevelData.modVersion, out Version modVersion) && modVersion < new Version(1, 3, 4))
                {
                    for (int i = 0; i < gameData.events[3].Count; i++)
                        gameData.events[3][i].SetValue(3, 0f);
                }
            }
            catch (Exception ex)
            {
                CoreHelper.LogException(ex);
            }

            ClampEventListValues(gameData.events);

            return gameData;
        }

        /// <summary>
        /// Writes the <see cref="GameData"/> to a VG format JSON.
        /// </summary>
        /// <returns>Returns a JSON object representing the <see cref="GameData"/>.</returns>
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

            int numLayer = 1;
            for (int i = 0; i < Mathf.Clamp(backgroundLayers.Count, 0, 5); i++)
            {
                var layerID = backgroundLayers[i].id;
                var jnLayer = Parser.NewJSONObject();

                int numBG = 0;
                for (int j = 0; j < backgroundObjects.Count; j++)
                {
                    var bg = backgroundObjects[j];
                    if (bg.layer != layerID)
                        continue;

                    jnLayer["o"][numBG] = bg.ToJSONVG();
                    numBG++;
                }

                jn["parallax_settings"]["l"][i] = jnLayer;
                numLayer++;
            }

            // other layers are required.
            while (numLayer < 6)
            {
                jn["parallax_settings"]["l"][numLayer - 1]["d"] = 100 * numLayer;
                jn["parallax_settings"]["l"][numLayer - 1]["c"] = 1 * numLayer;
                numLayer++;
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

            for (int i = 0; i < data.checkpoints.Count; i++)
                jn["checkpoints"][i] = data.checkpoints[i].ToJSONVG();

            for (int i = 0; i < beatmapObjects.Count; i++)
                jn["objects"][i] = beatmapObjects[i].ToJSONVG();

            if (!prefabObjects.IsEmpty())
                for (int i = 0; i < prefabObjects.Count; i++)
                    jn["prefab_objects"][i] = prefabObjects[i].ToJSONVG();
            else
                jn["prefab_objects"] = new JSONArray();

            if (!prefabs.IsEmpty())
                for (int i = 0; i < prefabs.Count; i++)
                    jn["prefabs"][i] = prefabs[i].ToJSONVG();
            else
                jn["prefabs"] = new JSONArray();

            var idsConverter = new Dictionary<string, string>();

            int themeIndex = 0;
            var themes = ThemeManager.inst.CustomThemes.Where(x => events[4].Has(y => int.TryParse(x.id, out int id) && id == y.values[0]));
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

            if (!data.markers.IsEmpty())
                for (int i = 0; i < data.markers.Count; i++)
                    jn["markers"][i] = data.markers[i].ToJSONVG();
            else
                jn["markers"] = new JSONArray();

            // Event Handlers
            {
                // Move
                for (int i = 0; i < events[0].Count; i++)
                {
                    var eventKeyframe = events[0][i];
                    jn["events"][0][i]["ct"] = eventKeyframe.curve.ToString();
                    jn["events"][0][i]["t"] = eventKeyframe.time;
                    jn["events"][0][i]["ev"][0] = eventKeyframe.values[0];
                    jn["events"][0][i]["ev"][1] = eventKeyframe.values[1];
                }

                // Zoom
                for (int i = 0; i < events[1].Count; i++)
                {
                    var eventKeyframe = events[1][i];
                    jn["events"][1][i]["ct"] = eventKeyframe.curve.ToString();
                    jn["events"][1][i]["t"] = eventKeyframe.time;
                    jn["events"][1][i]["ev"][0] = eventKeyframe.values[0];
                }

                // Rotate
                for (int i = 0; i < events[2].Count; i++)
                {
                    var eventKeyframe = events[2][i];
                    jn["events"][2][i]["ct"] = eventKeyframe.curve.ToString();
                    jn["events"][2][i]["t"] = eventKeyframe.time;
                    jn["events"][2][i]["ev"][0] = eventKeyframe.values[0];
                }

                // Shake
                for (int i = 0; i < events[3].Count; i++)
                {
                    var eventKeyframe = events[3][i];
                    jn["events"][3][i]["ct"] = eventKeyframe.curve.ToString();
                    jn["events"][3][i]["t"] = eventKeyframe.time;
                    jn["events"][3][i]["ev"][0] = eventKeyframe.values[0];
                    jn["events"][3][i]["ev"][1] = eventKeyframe.values[1];
                    jn["events"][3][i]["ev"][2] = eventKeyframe.values[2];
                }

                // Themes
                for (int i = 0; i < events[4].Count; i++)
                {
                    var eventKeyframe = events[4][i];
                    jn["events"][4][i]["ct"] = eventKeyframe.curve.ToString();
                    jn["events"][4][i]["t"] = eventKeyframe.time;
                    jn["events"][4][i]["evs"][0] = idsConverter.TryGetValue(eventKeyframe.values[0].ToString(), out string themeID) ? themeID : eventKeyframe.values[0].ToString();
                }

                // Chroma
                for (int i = 0; i < events[5].Count; i++)
                {
                    var eventKeyframe = events[5][i];
                    jn["events"][5][i]["ct"] = eventKeyframe.curve.ToString();
                    jn["events"][5][i]["t"] = eventKeyframe.time;
                    jn["events"][5][i]["ev"][0] = eventKeyframe.values[0];
                }

                // Bloom
                for (int i = 0; i < events[6].Count; i++)
                {
                    var eventKeyframe = events[6][i];
                    jn["events"][6][i]["ct"] = eventKeyframe.curve.ToString();
                    jn["events"][6][i]["t"] = eventKeyframe.time;
                    jn["events"][6][i]["ev"][0] = eventKeyframe.values[0];
                    jn["events"][6][i]["ev"][1] = eventKeyframe.values[1];
                    jn["events"][6][i]["ev"][2] = Mathf.Clamp(eventKeyframe.values[4], 0f, 9f);
                }

                // Vignette
                for (int i = 0; i < events[7].Count; i++)
                {
                    var eventKeyframe = events[7][i];
                    jn["events"][7][i]["ct"] = eventKeyframe.curve.ToString();
                    jn["events"][7][i]["t"] = eventKeyframe.time;
                    jn["events"][7][i]["ev"][0] = eventKeyframe.values[0];
                    jn["events"][7][i]["ev"][1] = eventKeyframe.values[1];
                    jn["events"][7][i]["ev"][2] = eventKeyframe.values[2];
                    jn["events"][7][i]["ev"][3] = eventKeyframe.values[3];
                    jn["events"][7][i]["ev"][4] = eventKeyframe.values[4];
                    jn["events"][7][i]["ev"][5] = eventKeyframe.values[5];
                    jn["events"][7][i]["ev"][6] = Mathf.Clamp(eventKeyframe.values[6], 0f, 9f);
                }

                // Lens
                for (int i = 0; i < events[8].Count; i++)
                {
                    var eventKeyframe = events[8][i];
                    jn["events"][8][i]["ct"] = eventKeyframe.curve.ToString();
                    jn["events"][8][i]["t"] = eventKeyframe.time;
                    jn["events"][8][i]["ev"][0] = eventKeyframe.values[0];
                    jn["events"][8][i]["ev"][1] = eventKeyframe.values[1];
                    jn["events"][8][i]["ev"][2] = eventKeyframe.values[2];
                }

                // Grain
                for (int i = 0; i < events[9].Count; i++)
                {
                    var eventKeyframe = events[9][i];
                    jn["events"][9][i]["ct"] = eventKeyframe.curve.ToString();
                    jn["events"][9][i]["t"] = eventKeyframe.time;
                    jn["events"][9][i]["ev"][0] = eventKeyframe.values[0];
                    jn["events"][9][i]["ev"][1] = eventKeyframe.values[1];
                    jn["events"][9][i]["ev"][2] = eventKeyframe.values[2];
                    jn["events"][9][i]["ev"][3] = 1f;
                }

                // Gradient
                for (int i = 0; i < events[15].Count; i++)
                {
                    var eventKeyframe = events[15][i];
                    jn["events"][10][i]["ct"] = eventKeyframe.curve.ToString();
                    jn["events"][10][i]["t"] = eventKeyframe.time;
                    jn["events"][10][i]["ev"][0] = eventKeyframe.values[0];
                    jn["events"][10][i]["ev"][1] = eventKeyframe.values[1];
                    jn["events"][10][i]["ev"][2] = Mathf.Clamp(eventKeyframe.values[2], 0f, 9f);
                    jn["events"][10][i]["ev"][3] = Mathf.Clamp(eventKeyframe.values[3], 0f, 9f);
                    jn["events"][10][i]["ev"][4] = eventKeyframe.values[4];
                }

                jn["events"][11][0]["ct"] = "Linear";
                jn["events"][11][0]["t"] = 0f;
                jn["events"][11][0]["ev"][0] = 0f;
                jn["events"][11][0]["ev"][1] = 0f;
                jn["events"][11][0]["ev"][2] = 0f;

                // Hueshift
                for (int i = 0; i < events[10].Count; i++)
                {
                    var eventKeyframe = events[10][i];
                    jn["events"][12][i]["ct"] = eventKeyframe.curve.ToString();
                    jn["events"][12][i]["t"] = eventKeyframe.time;
                    jn["events"][12][i]["ev"][0] = eventKeyframe.values[0];
                }

                // Player
                for (int i = 0; i < events[36].Count; i++)
                {
                    var eventKeyframe = events[36][i];
                    jn["events"][13][i]["ct"] = eventKeyframe.curve.ToString();
                    jn["events"][13][i]["t"] = eventKeyframe.time;
                    jn["events"][13][i]["ev"][0] = eventKeyframe.values[0];
                    jn["events"][13][i]["ev"][1] = eventKeyframe.values[1];
                    jn["events"][13][i]["ev"][2] = 0f;
                }
            }

            return jn;
        }

        /// <summary>
        /// Writes the <see cref="GameData"/> to an LS format JSON.
        /// </summary>
        /// <param name="saveGameDataThemes">If the levels' themes should be written to the JSON.</param>
        /// <returns>Returns a JSON object representing the <see cref="GameData"/>.</returns>
        public JSONNode ToJSON(bool saveGameDataThemes = false)
        {
            CoreHelper.Log("Saving Beatmap");
            var jn = JSON.Parse("{}");

            CoreHelper.Log("Saving Editor Data");
            jn["ed"]["timeline_pos"] = "0";

            CoreHelper.Log("Saving Markers");
            for (int i = 0; i < data.markers.Count; i++)
                jn["ed"]["markers"][i] = data.markers[i].ToJSON();

            for (int i = 0; i < levelModifiers.Count; i++)
            {
                var levelModifier = levelModifiers[i];

                jn["modifiers"][i]["action"] = levelModifier.ActionModifier.ToJSON();
                jn["modifiers"][i]["trigger"] = levelModifier.TriggerModifier.ToJSON();
                jn["modifiers"][i]["retrigger"] = levelModifier.retriggerAmount;
            }

            if (assets && !assets.IsEmpty())
                jn["assets"] = assets.ToJSON();

            CoreHelper.Log("Saving Object Prefabs");
            var prefabObjects = this.prefabObjects.FindAll(x => !x.fromModifier);
            for (int i = 0; i < prefabObjects.Count; i++)
                jn["prefab_objects"][i] = prefabObjects[i].ToJSON();

            CoreHelper.Log("Saving Level Data");
            jn["level_data"] = data.level.ToJSON();

            CoreHelper.Log("Saving prefabs");
            if (prefabs != null)
                for (int i = 0; i < prefabs.Count; i++)
                    jn["prefabs"][i] = prefabs[i].ToJSON();

            CoreHelper.Log($"Saving themes");
            var levelThemes =
                saveGameDataThemes ?
                    beatmapThemes.Where(x => Parser.TryParse(x.id, 0) != 0 && events[4].Has(y => y.values[0] == Parser.TryParse(x.id, 0))).ToList() :
                    ThemeManager.inst.CustomThemes.Where(x => Parser.TryParse(x.id, 0) != 0 && events[4].Has(y => y.values[0] == Parser.TryParse(x.id, 0))).ToList();

            for (int i = 0; i < levelThemes.Count; i++)
            {
                CoreHelper.Log($"Saving {levelThemes[i].id} - {levelThemes[i].name} to level!");
                jn["themes"][i] = levelThemes[i].ToJSON();
            }

            CoreHelper.Log("Saving Checkpoints");
            for (int i = 0; i < data.checkpoints.Count; i++)
                jn["checkpoints"][i] = data.checkpoints[i].ToJSON();

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

            CoreHelper.Log("Saving Background Layers");
            for (int i = 0; i < backgroundLayers.Count; i++)
                jn["bg_layers"][i] = backgroundLayers[i].ToJSON();

            CoreHelper.Log("Saving Background Objects");
            for (int i = 0; i < backgroundObjects.Count; i++)
                jn["bg_objects"][i] = backgroundObjects[i].ToJSON();

            CoreHelper.Log("Saving Event Objects");
            for (int i = 0; i < events.Count; i++)
                for (int j = 0; j < events[i].Count; j++)
                    if (EventTypes.Length > i)
                        jn["events"][EventTypes[i]][j] = events[i][j].ToJSON();

            return jn;
        }

        /// <summary>
        /// Saves the <see cref="GameData"/> to a LS format file.
        /// </summary>
        /// <param name="path">The file to save to.</param>
        /// <param name="onSave">Function to run when saving is complete.</param>
        /// <param name="saveGameDataThemes">If the levels' themes should be written to the JSON.</param>
        public void SaveData(string path, Action onSave = null, bool saveGameDataThemes = false)
        {
            if (EditorConfig.Instance.SaveAsync.Value)
                CoroutineHelper.StartCoroutineAsync(ISaveData(path, onSave, saveGameDataThemes));
            else
                CoroutineHelper.StartCoroutine(ISaveData(path, onSave, saveGameDataThemes));
        }

        /// <summary>
        /// Saves the <see cref="GameData"/> to a VG format file.
        /// </summary>
        /// <param name="path">The file to save to.</param>
        /// <param name="onSave">Function to run when saving is complete.</param>
        public void SaveDataVG(string path, Action onSave = null)
        {
            if (EditorConfig.Instance.SaveAsync.Value)
                CoroutineHelper.StartCoroutineAsync(ISaveDataVG(path, onSave));
            else
                CoroutineHelper.StartCoroutine(ISaveDataVG(path, onSave));
        }

        /// <summary>
        /// Saves the <see cref="GameData"/> to a LS format file.
        /// </summary>
        /// <param name="path">The file to save to.</param>
        /// <param name="onSave">Function to run when saving is complete.</param>
        /// <paramref name="saveGameDataThemes">If the levels' themes should be written to the JSON or not.</paramref>
        public IEnumerator ISaveData(string path, Action onSave = null, bool saveGameDataThemes = false)
        {
            var jn = ToJSON(saveGameDataThemes);
            CoreHelper.Log($"Saving Entire Beatmap to {path}");
            RTFile.WriteToFile(path, jn.ToString());

            yield return CielaSpike.Ninja.JumpToUnity;
            onSave?.Invoke();

            yield break;
        }

        /// <summary>
        /// Saves the <see cref="GameData"/> to a VG format file.
        /// </summary>
        /// <param name="path">The file to save to.</param>
        /// <param name="onSave">Function to run when saving is complete.</param>
        public IEnumerator ISaveDataVG(string path, Action onSave = null)
        {
            var jn = ToJSONVG();
            CoreHelper.Log($"Saving Entire Beatmap to {path}");
            RTFile.WriteToFile(path, jn.ToString());

            yield return CielaSpike.Ninja.JumpToUnity;
            onSave?.Invoke();

            yield break;
        }

        /// <summary>
        /// Parses all events from a level.
        /// </summary>
        /// <param name="jn">The LS JSON to parse.</param>
        /// <param name="clamp">If event keyframes list should be verified.</param>
        /// <returns>Returns a parsed list of event keyframes.</returns>
        public static List<List<EventKeyframe>> ParseEventkeyframes(JSONNode jn, bool clamp = true)
        {
            var allEvents = new List<List<EventKeyframe>>();

            // here we iterate through the default event types and check if the JSON exists. This is so we don't need to have a ton of repeating code.
            for (int i = 0; i < EventCount; i++)
            {
                allEvents.Add(new List<EventKeyframe>());
                if (jn[EventTypes[i]] != null)
                    for (int j = 0; j < jn[EventTypes[i]].Count; j++)
                        allEvents[i].Add(EventKeyframe.Parse(jn[EventTypes[i]][j], i, DefaultKeyframes[i].values.Length));
            }

            if (clamp)
                ClampEventListValues(allEvents);

            for (int i = 0; i < allEvents.Count; i++)
                allEvents[i] = allEvents[i].OrderBy(x => x.time).ToList(); // ensures the event keyframes are ordered correctly.

            return allEvents;
        }

        /// <summary>
        /// Verifies that the list of event keyframes is of the correct length.
        /// </summary>
        /// <param name="eventKeyframes">List of event keyframes to check.</param>
        public static void ClampEventListValues(List<List<EventKeyframe>> eventKeyframes)
        {
            int totalTypes = EventCount;

            // first, check if event keyframes count is higher than normal.
            while (eventKeyframes.Count > totalTypes)
                eventKeyframes.RemoveAt(eventKeyframes.Count - 1);

            for (int type = 0; type < totalTypes; type++)
            {
                // add to the event types if no event exists.
                if (eventKeyframes.Count < type + 1)
                    eventKeyframes.Add(new List<EventKeyframe>());

                // add an event if the list contains none.
                if (eventKeyframes[type].Count < 1)
                    eventKeyframes[type].Add(DefaultKeyframes[type].Copy());

                // verify the event value lengths are correct.
                for (int index = 0; index < eventKeyframes[type].Count; index++)
                {
                    var array = eventKeyframes[type][index].values;
                    if (array.Length != DefaultKeyframes[type].values.Length)
                    {
                        array = new float[DefaultKeyframes[type].values.Length];
                        for (int i = 0; i < DefaultKeyframes[type].values.Length; i++)
                            array[i] = i < eventKeyframes[type][index].values.Length ? eventKeyframes[type][index].values[i] : DefaultKeyframes[type].values[i];
                    }
                    eventKeyframes[type][index].values = array;
                }
            }
        }

        #region Helpers

        public void Clear()
        {
            for (int i = 0; i < beatmapObjects.Count; i++)
            {
                var beatmapObject = beatmapObjects[i];
                for (int j = 0; j < 0; j++)
                {
                    var modifier = beatmapObject.modifiers[j];
                    modifier.Action = null;
                    modifier.Trigger = null;
                    modifier.Inactive = null;
                    modifier.Result = null;
                }
            }
        }

        public List<BeatmapTheme> GetUsedThemes() => GetUsedThemes(beatmapThemes);

        public List<BeatmapTheme> GetUsedThemes(List<BeatmapTheme> beatmapThemes) => events == null || events.Count <= 4 ? new List<BeatmapTheme>() : beatmapThemes.Where(x => Parser.TryParse(x.id, 0) != 0 && events[4].Has(y => y.values[0] == Parser.TryParse(x.id, 0))).ToList();

        public void UpdateUsedThemes() => beatmapThemes = GetUsedThemes(ThemeManager.inst.CustomThemes);

        /// <summary>
        /// Gets closest event keyframe to current time.
        /// </summary>
        /// <param name="_type">Event Keyframe Type</param>
        /// <returns>Event Keyframe Index</returns>
        public int ClosestEventKeyframe(int type)
        {
            float time = AudioManager.inst.CurrentAudioSource.time;
            if (events[type].TryFindIndex(x => x.time > time, out int nextKF))
            {
                var prevKF = nextKF - 1;

                if (nextKF == 0)
                    prevKF = 0;
                else
                {
                    var v1 = new Vector2(events[type][prevKF].time, 0f);
                    var v2 = new Vector2(events[type][nextKF].time, 0f);

                    float dis = Vector2.Distance(v1, v2) / 2f;

                    bool prevClose = time > dis + events[type][prevKF].time;
                    bool nextClose = time < events[type][nextKF].time - dis;

                    if (!prevClose)
                        return prevKF;
                    if (!nextClose)
                        return nextKF;
                }
            }
            return 0;
        }

        /// <summary>
        /// Tries to get an object with a modifier's tag group.
        /// </summary>
        /// <param name="modifier">Modifier reference.</param>
        /// <param name="tag">Tag group.</param>
        /// <param name="result">Object result.</param>
        /// <returns>Returns true if an object was found, otherwise returns false.</returns>
        public bool TryFindObjectWithTag(Modifier<BeatmapObject> modifier, string tag, out BeatmapObject result)
        {
            result = FindObjectWithTag(modifier, tag);
            return result;
        }

        /// <summary>
        /// Gets an object with a tag group.
        /// </summary>
        /// <param name="tag">Tag group.</param>
        /// <returns>Returns the found object.</returns>
        public BeatmapObject FindObjectWithTag(string tag) => FindObjectWithTag(beatmapObjects, tag);

        /// <summary>
        /// Gets an object with a modifier's tag group.
        /// </summary>
        /// <param name="modifier">Modifier reference.</param>
        /// <param name="tag">Tag group.</param>
        /// <returns>Returns the found object.</returns>
        public BeatmapObject FindObjectWithTag(Modifier<BeatmapObject> modifier, string tag) => modifier.prefabInstanceOnly && !string.IsNullOrEmpty(modifier.reference.prefabInstanceID) ?
                beatmapObjects.Find(x => (!modifier.groupAlive || x.Alive) && x.tags.Contains(tag) && x.prefabID == modifier.reference.prefabID && x.prefabInstanceID == modifier.reference.prefabInstanceID) :
                beatmapObjects.Find(x => (!modifier.groupAlive || x.Alive) && x.tags.Contains(tag));

        /// <summary>
        /// Gets an object with a tag group.
        /// </summary>
        /// <param name="beatmapObjects">Objects list to search.</param>
        /// <param name="tag">Tag group.</param>
        /// <returns>Returns the found object.</returns>
        public BeatmapObject FindObjectWithTag(List<BeatmapObject> beatmapObjects, string tag) => beatmapObjects.Find(x => x.tags.Contains(tag));

        /// <summary>
        /// Gets a list of objects with a tag group.
        /// </summary>
        /// <param name="tag">Tag group.</param>
        /// <returns>Returns a list of found objects.</returns>
        public List<BeatmapObject> FindObjectsWithTag(string tag) => FindObjectsWithTag(beatmapObjects, tag);

        /// <summary>
        /// Returns a list of objects with a modifier's tag group.
        /// </summary>
        /// <param name="modifier">Modifier reference.</param>
        /// <param name="tag">Tag group.</param>
        /// <returns>Returns a list of found objects.</returns>
        public List<BeatmapObject> FindObjectsWithTag(Modifier<BeatmapObject> modifier, string tag) => modifier.prefabInstanceOnly && !string.IsNullOrEmpty(modifier.reference.prefabInstanceID) ?
                beatmapObjects.FindAll(x => (!modifier.groupAlive || x.Alive) && x.tags.Contains(tag) && x.prefabID == modifier.reference.prefabID && x.prefabInstanceID == modifier.reference.prefabInstanceID) :
                beatmapObjects.FindAll(x => (!modifier.groupAlive || x.Alive) && x.tags.Contains(tag));

        /// <summary>
        /// Gets a list of objects with a tag group.
        /// </summary>
        /// <param name="beatmapObjects">Objects list to search.</param>
        /// <param name="tag">Tag group.</param>
        /// <returns>Returns a list of found objects.</returns>
        public List<BeatmapObject> FindObjectsWithTag(List<BeatmapObject> beatmapObjects, string tag) => beatmapObjects.FindAll(x => x.tags.Contains(tag));

        public static float InterpolateFloatKeyframes(List<EventKeyframe> eventKeyframes, float time, int valueIndex, bool isLerper = true)
        {
            var list = eventKeyframes.OrderBy(x => x.time).ToList();

            var nextKFIndex = list.FindIndex(x => x.time > time);

            if (nextKFIndex < 0)
                nextKFIndex = list.Count - 1;

            var prevKFIndex = nextKFIndex - 1;
            if (prevKFIndex < 0)
                prevKFIndex = 0;

            var nextKF = list[nextKFIndex];
            var prevKF = list[prevKFIndex];

            if (prevKF.values.Length <= valueIndex)
                return 0f;

            var total = 0f;
            var prevtotal = 0f;
            for (int k = 0; k < nextKFIndex; k++)
            {
                if (list[k + 1].relative)
                    total += list[k].values[valueIndex];
                else
                    total = 0f;

                if (list[k].relative)
                    prevtotal += list[k].values[valueIndex];
                else
                    prevtotal = 0f;
            }

            var next = nextKF.relative ? total + nextKF.values[valueIndex] : nextKF.values[valueIndex];
            var prev = prevKF.relative || nextKF.relative ? prevtotal : prevKF.values[valueIndex];

            if (float.IsNaN(prev) || !isLerper)
                prev = 0f;

            if (float.IsNaN(next))
                next = 0f;

            if (!isLerper)
                next = 1f;

            if (prevKFIndex == nextKFIndex)
                return next;

            var x = RTMath.Lerp(prev, next, Ease.GetEaseFunction(nextKF.curve.ToString())(RTMath.InverseLerp(prevKF.time, nextKF.time, Mathf.Clamp(time, 0f, nextKF.time))));

            if (prevKFIndex == nextKFIndex)
                x = next;

            if (float.IsNaN(x) || float.IsInfinity(x))
                x = next;

            return x;
        }

        public static Vector2 InterpolateVector2Keyframes(List<EventKeyframe> eventKeyframes, float time)
        {
            var list = eventKeyframes.OrderBy(x => x.time).ToList();

            var nextKFIndex = list.FindIndex(x => x.time > time);

            if (nextKFIndex < 0)
                nextKFIndex = list.Count - 1;

            var prevKFIndex = nextKFIndex - 1;
            if (prevKFIndex < 0)
                prevKFIndex = 0;

            var nextKF = list[nextKFIndex];
            var prevKF = list[prevKFIndex];

            if (prevKF.values.Length <= 0)
                return Vector2.zero;

            Vector2 total = Vector3.zero;
            Vector2 prevtotal = Vector3.zero;
            for (int k = 0; k < nextKFIndex; k++)
            {
                if (list[k + 1].relative)
                    total += new Vector2(list[k].values[0], list[k].values[1]);
                else
                    total = Vector3.zero;

                if (list[k].relative)
                    prevtotal += new Vector2(list[k].values[0], list[k].values[1]);
                else
                    prevtotal = Vector2.zero;
            }

            var next = nextKF.relative ? total + new Vector2(nextKF.values[0], nextKF.values[1]) : new Vector2(nextKF.values[0], nextKF.values[1]);
            var prev = prevKF.relative || nextKF.relative ? prevtotal : new Vector2(prevKF.values[0], prevKF.values[1]);

            if (float.IsNaN(prev.x) || float.IsNaN(prev.y))
                prev = Vector2.zero;

            if (float.IsNaN(prev.x) || float.IsNaN(prev.y))
                next = Vector2.zero;

            if (prevKFIndex == nextKFIndex)
                return next;

            var x = RTMath.Lerp(prev, next, Ease.GetEaseFunction(nextKF.curve.ToString())(RTMath.InverseLerp(prevKF.time, nextKF.time, Mathf.Clamp(time, 0f, nextKF.time))));

            if (prevKFIndex == nextKFIndex)
                x = next;

            if (float.IsNaN(x.x) || float.IsNaN(x.y) || float.IsInfinity(x.x) || float.IsInfinity(x.y))
                x = next;

            return x;
        }

        public static Vector3 InterpolateVector3Keyframes(List<EventKeyframe> eventKeyframes, float time)
        {
            var list = eventKeyframes.OrderBy(x => x.time).ToList();

            var nextKFIndex = list.FindIndex(x => x.time > time);

            if (nextKFIndex < 0)
                nextKFIndex = list.Count - 1;

            var prevKFIndex = nextKFIndex - 1;
            if (prevKFIndex < 0)
                prevKFIndex = 0;

            var nextKF = list[nextKFIndex];
            var prevKF = list[prevKFIndex];

            if (prevKF.values.Length <= 0)
                return Vector3.zero;

            Vector3 total = Vector3.zero;
            Vector3 prevtotal = Vector3.zero;
            for (int k = 0; k < nextKFIndex; k++)
            {
                if (list[k + 1].relative)
                    total += new Vector3(list[k].values[0], list[k].values[1], list[k].values[2]);
                else
                    total = Vector3.zero;

                if (list[k].relative)
                    prevtotal += new Vector3(list[k].values[0], list[k].values[1], list[k].values[2]);
                else
                    prevtotal = Vector3.zero;
            }

            var next = nextKF.relative ? total + new Vector3(nextKF.values[0], nextKF.values[1], nextKF.values[2]) : new Vector3(nextKF.values[0], nextKF.values[1], nextKF.values[2]);
            var prev = prevKF.relative || nextKF.relative ? prevtotal : new Vector3(prevKF.values[0], prevKF.values[1], prevKF.values[2]);

            if (float.IsNaN(prev.x) || float.IsNaN(prev.y) || float.IsNaN(prev.z))
                prev = Vector3.zero;

            if (float.IsNaN(prev.x) || float.IsNaN(prev.y) || float.IsNaN(prev.z))
                next = Vector3.zero;

            if (prevKFIndex == nextKFIndex)
                return next;

            var x = RTMath.Lerp(prev, next, Ease.GetEaseFunction(nextKF.curve.ToString())(RTMath.InverseLerp(prevKF.time, nextKF.time, Mathf.Clamp(time, 0f, nextKF.time))));

            if (prevKFIndex == nextKFIndex)
                x = next;

            if (float.IsNaN(x.x) || float.IsNaN(x.y) || float.IsNaN(x.z) || float.IsInfinity(x.x) || float.IsInfinity(x.y) || float.IsInfinity(x.z))
                x = next;

            return x;
        }

        #endregion

        #endregion

        #region Fields

        public LevelBeatmapData data;

        public Assets assets = new Assets();

        public List<BeatmapObject> beatmapObjects = new List<BeatmapObject>();

        public List<PrefabObject> prefabObjects = new List<PrefabObject>();

        public List<Prefab> prefabs = new List<Prefab>();

        public List<BackgroundLayer> backgroundLayers = new List<BackgroundLayer>();

        public List<BackgroundObject> backgroundObjects = new List<BackgroundObject>();

        public List<LevelModifier> levelModifiers = new List<LevelModifier>();

        public List<BeatmapTheme> beatmapThemes = new List<BeatmapTheme>();

        public List<List<EventKeyframe>> events = new List<List<EventKeyframe>>();

        #endregion

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static GameData operator +(GameData a, GameData b) => Combiner.Combine(a, b);

        // todo: rework this to be more consistent with other modifier sets.
        // theory:
        // when converting to VG, a singular action and trigger are saved per modifier block.
        // the first trigger in a range is saved.
        // the first action in a range is saved.
        // example:
        // modifier (trigger) 0 = saved because it's the first trigger
        // modifier (trigger) 1 = not saved
        // modifier (trigger) 2 = not saved
        // modifier (action) 3 = saved because it's the first action
        // modifier (action) 4 = not saved
        // modifier (trigger) 5 = saved because it's the first trigger
        // modifier (action) 6 = saved because it's the first action
        // modifier (trigger) 7 = not saved because there are no accompanying actions
        /// <summary>
        /// Class for alpha EventTrigger support.
        /// </summary>
        public class LevelModifier
        {
            public Modifier<GameData> TriggerModifier { get; set; }
            public Modifier<GameData> ActionModifier { get; set; }

            public int retriggerAmount = -1;
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

                var time = RTLevel.Current?.CurrentTime;

                switch (modiifer.Name.ToLower())
                {
                    case "none": {
                            return true;
                        }
                    case "time": {
                            return modiifer.commands.Count > 2 && float.TryParse(modiifer.commands[1], out float min) && float.TryParse(modiifer.commands[2], out float max)
                                && time >= min - 0.01f && time <= max + 0.1f;
                        }
                    case "playerhit":  {
                            return PlayerManager.Players.Any(x => x.Player != null && x.Player.isTakingHit);
                        }
                    case "playerdeath": {
                            return PlayerManager.Players.Any(x => x.Player != null && x.Player.isDead);
                        }
                    case "levelstart": {
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

                switch (modifier.Name.ToLower().Replace(" ", "").Replace("_", ""))
                {
                    case "playerlocation": {
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
                    case "playerboostlock": {
                            if (modifier.commands.Count > 3 && !string.IsNullOrEmpty(modifier.commands[1]) && bool.TryParse(modifier.commands[1], out bool lockBoost))
                                RTPlayer.LockBoost = lockBoost;

                            break;
                        }

                }
            }

            public static Modifier<GameData>[] DefaultTriggers => new Modifier<GameData>[]
            {
                new Modifier<GameData>("time")
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
				new Modifier<GameData>("playerHit")
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
				new Modifier<GameData>("playerDeath")
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
				new Modifier<GameData>("levelStart")
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
                new Modifier<GameData>("vnInk")
                {
                    type = ModifierBase.Type.Action,
                    constant = false,
                    commands = new List<string>
                    {
                        "vnInk"
                    },
                    value = "",
                }, // vnInk
				new Modifier<GameData>("vnTimeline")
                {
                    type = ModifierBase.Type.Action,
                    constant = false,
                    commands = new List<string>
                    {
                        "vnTimeline"
                    },
                    value = "",
                }, // vnTimeline
				new Modifier<GameData>("playerBubble")
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
				new Modifier<GameData>("playerLocation")
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
				new Modifier<GameData>("playerBoostLock")
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
                    data = new LevelBeatmapData
                    {
                        editor = new LevelEditorData(),
                        level = new LevelData(),
                    },
                };

                if (gameDatas != null && gameDatas.Length > 0)
                    for (int i = 0; i < gameDatas.Length; i++)
                    {
                        if (gameDatas[i].data != null && baseData.data != null)
                        {
                            if (baseData.data.checkpoints == null)
                                baseData.data.checkpoints = new List<Checkpoint>();
                            if (baseData.data.markers == null)
                                baseData.data.markers = new List<Marker>();

                            baseData.data.checkpoints.AddRange(gameDatas[i].data.checkpoints.FindAll(x => !baseData.data.checkpoints.Has(y => y.time == x.time)));
                            baseData.data.markers.AddRange(gameDatas[i].data.markers.FindAll(x => !baseData.data.markers.Has(y => y.time == x.time)));
                        }

                        if (baseData.beatmapObjects == null)
                            baseData.beatmapObjects = new List<BeatmapObject>();

                        baseData.beatmapObjects.AddRange(gameDatas[i].beatmapObjects.FindAll(x => !baseData.beatmapObjects.Has(y => y.id == x.id)));

                        if (baseData.prefabObjects == null)
                            baseData.prefabObjects = new List<PrefabObject>();

                        baseData.prefabObjects.AddRange(gameDatas[i].prefabObjects.Where(x => !baseData.prefabObjects.Has(y => y.id == x.id)));

                        if (baseData.prefabs == null)
                            baseData.prefabs = new List<Prefab>();

                        baseData.prefabs.AddRange(gameDatas[i].prefabs.FindAll(x => !baseData.prefabs.Has(y => y.id == x.id)));

                        baseData.backgroundObjects.AddRange(gameDatas[i].backgroundObjects.Where(x => !baseData.backgroundObjects.Has(y =>
                        {
                            return y.active == x.active &&
                                    y.color == x.color &&
                                    y.iterations == x.iterations &&
                                    y.drawFade == x.drawFade &&
                                    y.fadeColor == x.fadeColor &&
                                    y.depth == x.depth &&
                                    y.name == x.name &&
                                    y.pos == x.pos &&
                                    y.reactiveType == x.reactiveType &&
                                    y.reactiveCol == x.reactiveCol &&
                                    y.reactiveColIntensity == x.reactiveColIntensity &&
                                    y.reactiveColSample == x.reactiveColSample &&
                                    y.reactivePosIntensity == x.reactivePosIntensity &&
                                    y.reactivePosSamples == x.reactivePosSamples &&
                                    y.reactiveRotIntensity == x.reactiveRotIntensity &&
                                    y.reactiveRotSample == x.reactiveRotSample &&
                                    y.reactiveScaIntensity == x.reactiveScaIntensity &&
                                    y.reactiveScale == x.reactiveScale &&
                                    y.reactiveScaSamples == x.reactiveScaSamples &&
                                    y.reactiveSize == x.reactiveSize &&
                                    y.reactiveZIntensity == x.reactiveZIntensity &&
                                    y.reactiveZSample == x.reactiveZSample &&
                                    y.rot == x.rot &&
                                    y.rotation == x.rotation &&
                                    y.scale == x.scale &&
                                    y.text == x.text &&
                                    y.zscale == x.zscale;
                        })));

                        if (baseData.events == null)
                            baseData.events = new List<List<EventKeyframe>>();

                        for (int j = 0; j < gameDatas[i].events.Count; j++)
                        {
                            if (baseData.events.Count <= j)
                                baseData.events.Add(new List<EventKeyframe>());

                            baseData.events[j].AddRange(gameDatas[i].events[j].Where(x => !baseData.events[j].Has(y => y.time == x.time)));
                        }

                        foreach (var beatmapTheme in gameDatas[i].beatmapThemes)
                        {
                            if (!baseData.beatmapThemes.Has(x => x.id == beatmapTheme.id))
                                baseData.beatmapThemes.Add(beatmapTheme);
                        }

                        // Clearing
                        {
                            for (int j = 0; j < gameDatas[i].data.checkpoints.Count; j++)
                                gameDatas[i].data.checkpoints[j] = null;
                            gameDatas[i].data.checkpoints.Clear();

                            for (int j = 0; j < gameDatas[i].data.markers.Count; j++)
                                gameDatas[i].data.markers[j] = null;
                            gameDatas[i].data.markers.Clear();

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

                            for (int j = 0; j < gameDatas[i].events.Count; j++)
                                gameDatas[i].events[j] = null;
                            gameDatas[i].events.Clear();

                            gameDatas[i] = null;
                        }
                    }

                gameDatas = null;

                return baseData;
            }
        }
    }
}